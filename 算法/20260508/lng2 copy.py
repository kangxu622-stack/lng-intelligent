# -*- coding: utf-8 -*-
import os
import math
import json
import warnings
from dataclasses import dataclass
from datetime import datetime, timedelta
from itertools import combinations, product
from typing import Dict, List, Optional, Tuple

import numpy as np
import pandas as pd
from flask import Flask, jsonify, request

warnings.filterwarnings("ignore")

app = Flask(__name__)

FRONTEND_PARAM_KEYS = {
    "plan_name",
    "condition_name",
    "remark",
    "total_output_target_m3_day",
    "lp_target_pressure",
    "hp_target_pressure",
    "initial_liquid",
    "start_time",
    "daily_demand_curve",
    "calculation_mode",
}


def _json_safe(value: object) -> object:
    if isinstance(value, np.ndarray):
        return value.tolist()
    if isinstance(value, (np.floating, np.integer)):
        return value.item()
    if isinstance(value, datetime):
        return value.strftime("%Y-%m-%d %H:%M:%S")
    if isinstance(value, list):
        return [_json_safe(item) for item in value]
    if isinstance(value, tuple):
        return [_json_safe(item) for item in value]
    if isinstance(value, dict):
        return {str(key): _json_safe(item) for key, item in value.items()}
    return value


def debug_print_algorithm_params(params: Dict[str, object]) -> None:
    frontend_params = {key: params[key] for key in sorted(params.keys()) if key in FRONTEND_PARAM_KEYS}
    backend_params = {key: params[key] for key in sorted(params.keys()) if key not in FRONTEND_PARAM_KEYS}

    print("\n=== Frontend params via backend ===")
    print(json.dumps(_json_safe(frontend_params), ensure_ascii=False, indent=2))
    print("=== Backend DB assembled params ===")
    print(json.dumps(_json_safe(backend_params), ensure_ascii=False, indent=2))
    print("=== End algorithm params ===\n")


@dataclass
class Tank:
    name: str
    phase: str
    area_m2: float
    level_min_m: float
    level_pump_start_m: float
    level_max_m: float
    level_init_m: float


@dataclass
class Device:
    name: str
    category: str
    line: Optional[str]
    rated_power_kw: float
    reactive_kvar: float = 0.0
    min_flow: float = 0.0
    max_flow: float = 0.0
    tank: Optional[str] = None
    capacity_kgph: float = 0.0
    phase: str = "一期"


class Params:
    def __init__(self, base_dir: Optional[str] = None):
        self.base_dir = base_dir or os.path.dirname(os.path.abspath(__file__))
        self.random_seed = 42
        self.start_time = self._default_start_time()
        self.delta_minutes = 15
        self.delta_hours = self.delta_minutes / 60.0
        self.horizon_hours = 24
        self.n_steps = int(self.horizon_hours / self.delta_hours)
        self.timestamps = [self.start_time + timedelta(minutes=self.delta_minutes * i) for i in range(self.n_steps)]

        self.allowed_delivery_bias = 0.05

        self.start_penalty = {"HP": 260.0, "LP": 40.0, "SW": 220.0, "COMP": 180.0}
        self.recirc_penalty = {"HP": 3.5, "LP": 1.0}
        self.line_balance_weight = 0.06
        self.runtime_weight = 0.002
        self.switch_penalty = 30.0

    def _build_weather_profile(self) -> pd.DataFrame:
        hours = np.arange(self.n_steps) * self.delta_hours
        tod = np.array([ts.hour + ts.minute / 60.0 for ts in self.timestamps])
        temp = 26.0 + 4.5 * np.sin(2.0 * np.pi * (tod - 7.0) / 24.0)
        pressure = 101.2 + 0.6 * np.cos(2.0 * np.pi * (tod - 4.0) / 24.0)
        humidity = 0.80 - 0.10 * np.sin(2.0 * np.pi * (tod - 6.0) / 24.0)
        return pd.DataFrame({
            "timestamp": self.timestamps,
            "temp_c": temp,
            "pressure_kpa": pressure,
            "humidity": humidity,
            "sim_hour": hours,
        })

    @staticmethod
    def _default_start_time() -> datetime:
        now = datetime.now()
        return now.replace(hour=0, minute=0, second=0, microsecond=0)

    def _build_sendout_profile(self) -> np.ndarray:
        if not hasattr(self, "daily_demand_curve"):
            raise ValueError("daily_demand_curve must be provided by backend parameter assembly.")

        hourly_base = np.array(self.daily_demand_curve, dtype=float)
        if hourly_base.size != 24:
            raise ValueError(f"daily_demand_curve must have 24 hourly points, got {hourly_base.size}")
        if np.sum(hourly_base) <= 0:
            raise ValueError("daily_demand_curve total must be greater than 0")
        hourly_base = hourly_base / hourly_base.sum() * self.total_output_target_m3_day
        horizon_hourly = np.tile(hourly_base, int(math.ceil(self.horizon_hours / 24.0)))[: self.horizon_hours]
        profile = np.repeat(horizon_hourly, int(60 / self.delta_minutes))
        return profile[: self.n_steps]

    def _build_unload_profile(self) -> Tuple[np.ndarray, List[str]]:
        profile = np.zeros(self.n_steps, dtype=float)
        phases = ["待机"] * self.n_steps
        schedule = [
            (11, 0, 0, 12, 30, 0.0, "引航靠泊"),
            (12, 30, 0, 14, 30, 0.0, "系统建立"),
            (14, 50, 0, 15, 50, 0.0, "CIQ与MSA联检"),
            (15, 50, 0, 16, 50, 0.0, "安全检查及卸前"),
            (16, 50, 0, 17, 20, 0.0, "吹扫置换"),
            (17, 20, 0, 17, 40, 0.0, "初始计量"),
            (17, 40, 0, 18, 0, 0.0, "热态ESD测试"),
            (18, 0, 0, 19, 30, 2000.0, "卸料臂预冷"),
            (19, 30, 0, 19, 50, 0.0, "冷态ESD测试"),
            (19, 50, 0, 20, 50, 6000.0, "卸料升速"),
            (20, 50, 1, 10, 30, 12000.0, "卸料全速"),
            (10, 30, 1, 11, 30, 4000.0, "卸料降速"),
            (11, 30, 1, 12, 20, 1200.0, "岸侧排凝"),
            (12, 20, 1, 12, 50, 600.0, "船侧排凝"),
            (12, 50, 1, 13, 10, 0.0, "吹扫置换"),
            (13, 10, 1, 13, 30, 0.0, "末次计量"),
            (13, 30, 1, 14, 30, 0.0, "拆臂"),
            (14, 30, 1, 15, 0, 0.0, "卸后会议"),
        ]
        base_day = self.start_time.replace(hour=0, minute=0, second=0, microsecond=0)
        for i, ts in enumerate(self.timestamps):
            for start_hour, start_minute, day_offset, end_hour, end_minute, flow, phase in schedule:
                start = base_day + timedelta(days=day_offset, hours=start_hour, minutes=start_minute)
                end = base_day + timedelta(days=day_offset, hours=end_hour, minutes=end_minute)
                if end <= start:
                    end += timedelta(days=1)
                if start <= ts < end:
                    profile[i] = flow
                    phases[i] = phase
                    break
        return profile, phases

    def _build_tanks(self) -> List[Tank]:
        raise ValueError("Tank master data must be provided by backend parameter assembly.")

    def _build_devices(self) -> List[Device]:
        raise ValueError("Device master data must be provided by backend parameter assembly.")

    def _group_devices_by_category(self) -> Dict[str, List[Device]]:
        grouped: Dict[str, List[Device]] = {}
        for d in self.devices:
            grouped.setdefault(d.category, []).append(d)
        return grouped


def calc_quadratic_power(coeffs: List[float], flow: float, rated_power: float) -> float:
    if flow <= 0:
        return 0.0
    power = coeffs[0] * flow ** 2 + coeffs[1] * flow + coeffs[2]
    return float(np.clip(power, 0.0, rated_power))


def incremental_peak_cost(line_after: Dict[str, float], current_peak: Dict[str, float], demand_charge_price: float) -> float:
    inc = 0.0
    for line in ["L1", "L2"]:
        if line_after[line] > current_peak[line]:
            inc += (line_after[line] - current_peak[line]) * demand_charge_price
    return inc


def choose_hp_pumps(
    demand_m3h: float,
    devices: List[Device],
    prev_on: Dict[str, int],
    runtime_steps: Dict[str, int],
    line_loads: Dict[str, float],
    current_peak: Dict[str, float],
    par: Params,
    price: float,
) -> Tuple[List[str], Dict[str, float], Dict[str, float], float, float]:
    if demand_m3h <= 1e-9:
        return [], {}, {"L1": 0.0, "L2": 0.0}, 0.0, 0.0

    for d in devices:
        if d.line not in {"L1", "L2"}:
            raise ValueError(f"HP device {d.name} has invalid line: {d.line!r}")

    best = None
    for r in range(1, len(devices) + 1):
        for subset in combinations(devices, r):
            min_required = max(d.min_flow for d in subset)
            max_allowed = min(d.max_flow for d in subset)
            flow_per_pump = max(demand_m3h / r, min_required)
            if flow_per_pump > max_allowed + 1e-9:
                continue
            power_per_pump = calc_quadratic_power(par.hp_pq_coeffs, flow_per_pump, max(d.rated_power_kw for d in subset))
            power_by_line = {"L1": 0.0, "L2": 0.0}
            for d in subset:
                power_by_line[d.line] += power_per_pump
            line_after = {
                "L1": line_loads["L1"] + power_by_line["L1"],
                "L2": line_loads["L2"] + power_by_line["L2"],
            }
            start_cost = par.start_penalty["HP"] * sum(1 for d in subset if prev_on.get(d.name, 0) == 0)
            recirc = max(0.0, flow_per_pump * r - demand_m3h)
            recirc_cost = par.recirc_penalty["HP"] * recirc
            peak_cost = incremental_peak_cost(line_after, current_peak, par.demand_charge_price)
            balance_cost = par.line_balance_weight * abs(line_after["L1"] - line_after["L2"])
            runtime_cost = par.runtime_weight * sum(runtime_steps.get(d.name, 0) for d in subset)
            energy_cost = power_per_pump * r * price * par.delta_hours
            total_cost = energy_cost + start_cost + recirc_cost + peak_cost + balance_cost + runtime_cost
            candidate = (total_cost, [d.name for d in subset], flow_per_pump, power_per_pump, power_by_line, recirc)
            if best is None or candidate[0] < best[0]:
                best = candidate

    if best is None:
        subset = [max(devices, key=lambda d: d.max_flow)]
        flow_per_pump = min(subset[0].max_flow, max(demand_m3h, subset[0].min_flow))
        power_per_pump = calc_quadratic_power(par.hp_pq_coeffs, flow_per_pump, subset[0].rated_power_kw)
        power_by_line = {"L1": 0.0, "L2": 0.0}
        power_by_line[subset[0].line] = power_per_pump
        recirc = max(0.0, flow_per_pump - demand_m3h)
        best = (0.0, [subset[0].name], flow_per_pump, power_per_pump, power_by_line, recirc)

    selected_names = best[1]
    flow_map = {name: best[2] for name in selected_names}
    power_map = {name: best[3] for name in selected_names}
    return selected_names, flow_map, best[4], sum(power_map.values()), best[5]


def choose_lp_pumps(
    required_lp_flow_m3h: float,
    devices: List[Device],
    prev_on: Dict[str, int],
    runtime_steps: Dict[str, int],
    line_loads: Dict[str, float],
    current_peak: Dict[str, float],
    tank_levels: Dict[str, float],
    receiving_tanks: List[str],
    par: Params,
    price: float,
) -> Tuple[List[str], Dict[str, float], Dict[str, float], float, float]:
    for d in devices:
        if d.line not in {"L1", "L2"}:
            raise ValueError(f"LP device {d.name} has invalid line: {d.line!r}")

    available = []
    for d in devices:
        if tank_levels[d.tank] < next(t.level_pump_start_m for t in par.tanks if t.name == d.tank):
            continue
        available.append(d)
    if not available:
        return [], {}, {"L1": 0.0, "L2": 0.0}, 0.0, 0.0

    available = sorted(
        available,
        key=lambda d: (
            1 if d.tank in receiving_tanks else 0,
            -tank_levels[d.tank],
            0 if prev_on.get(d.name, 0) == 1 else 1,
            runtime_steps.get(d.name, 0),
            d.name,
        ),
    )
    pool = available[: min(12, len(available))]
    n_low = max(1, int(math.ceil(required_lp_flow_m3h / max(d.max_flow for d in pool))))
    n_high = min(len(pool), max(n_low, int(math.ceil(required_lp_flow_m3h / min(d.min_flow for d in pool))) + 1))

    best = None
    for n in range(n_low, min(n_high, n_low + 3) + 1):
        for subset in combinations(pool, n):
            min_required = max(d.min_flow for d in subset)
            max_allowed = min(d.max_flow for d in subset)
            flow_per_pump = max(required_lp_flow_m3h / n, min_required)
            if flow_per_pump > max_allowed + 1e-9:
                continue
            power_per_pump = calc_quadratic_power(par.lp_pq_coeffs, flow_per_pump, max(d.rated_power_kw for d in subset))
            power_by_line = {"L1": 0.0, "L2": 0.0}
            for d in subset:
                power_by_line[d.line] += power_per_pump
            line_after = {
                "L1": line_loads["L1"] + power_by_line["L1"],
                "L2": line_loads["L2"] + power_by_line["L2"],
            }
            receive_penalty = 900.0 * sum(1 for d in subset if d.tank in receiving_tanks)
            start_cost = par.start_penalty["LP"] * sum(1 for d in subset if prev_on.get(d.name, 0) == 0)
            recirc = max(0.0, flow_per_pump * n - required_lp_flow_m3h)
            recirc_cost = par.recirc_penalty["LP"] * recirc
            peak_cost = incremental_peak_cost(line_after, current_peak, par.demand_charge_price)
            balance_cost = par.line_balance_weight * abs(line_after["L1"] - line_after["L2"])
            runtime_cost = par.runtime_weight * sum(runtime_steps.get(d.name, 0) for d in subset)
            tank_spread_cost = 25.0 * max(0, len({d.tank for d in subset}) - 1)
            energy_cost = power_per_pump * n * price * par.delta_hours
            total_cost = energy_cost + receive_penalty + start_cost + recirc_cost + peak_cost + balance_cost + runtime_cost + tank_spread_cost
            candidate = (total_cost, [d.name for d in subset], flow_per_pump, power_per_pump, power_by_line, recirc)
            if best is None or candidate[0] < best[0]:
                best = candidate

    if best is None:
        subset = pool[:n_low]
        flow_per_pump = max(required_lp_flow_m3h / max(1, len(subset)), max(d.min_flow for d in subset))
        power_per_pump = calc_quadratic_power(par.lp_pq_coeffs, flow_per_pump, max(d.rated_power_kw for d in subset))
        power_by_line = {"L1": 0.0, "L2": 0.0}
        for d in subset:
            power_by_line[d.line] += power_per_pump
        best = (0.0, [d.name for d in subset], flow_per_pump, power_per_pump, power_by_line, max(0.0, flow_per_pump * len(subset) - required_lp_flow_m3h))

    selected_names = best[1]
    flow_map = {name: best[2] for name in selected_names}
    power_map = {name: best[3] for name in selected_names}
    return selected_names, flow_map, best[4], sum(power_map.values()), best[5]


def choose_sw_pumps(
    required_sw_flow_m3h: float,
    devices: List[Device],
    prev_on: Dict[str, int],
    runtime_steps: Dict[str, int],
    line_loads: Dict[str, float],
    current_peak: Dict[str, float],
    par: Params,
    price: float,
) -> Tuple[List[str], Dict[str, float], Dict[str, float], float]:
    if required_sw_flow_m3h <= 1e-9:
        return [], {}, {"L1": 0.0, "L2": 0.0}, 0.0

    for d in devices:
        if d.line not in {"L1", "L2"}:
            raise ValueError(f"SW device {d.name} has invalid line: {d.line!r}")

    best = None
    for r in range(1, len(devices) + 1):
        for subset in combinations(devices, r):
            total_flow = sum(d.max_flow for d in subset)
            if total_flow + 1e-9 < required_sw_flow_m3h:
                continue
            power_by_line = {"L1": 0.0, "L2": 0.0}
            total_power = 0.0
            for d in subset:
                power_by_line[d.line] += d.rated_power_kw
                total_power += d.rated_power_kw
            line_after = {
                "L1": line_loads["L1"] + power_by_line["L1"],
                "L2": line_loads["L2"] + power_by_line["L2"],
            }
            start_cost = par.start_penalty["SW"] * sum(1 for d in subset if prev_on.get(d.name, 0) == 0)
            peak_cost = incremental_peak_cost(line_after, current_peak, par.demand_charge_price)
            balance_cost = par.line_balance_weight * abs(line_after["L1"] - line_after["L2"])
            runtime_cost = par.runtime_weight * sum(runtime_steps.get(d.name, 0) for d in subset)
            oversupply_cost = 0.006 * (total_flow - required_sw_flow_m3h)
            energy_cost = total_power * price * par.delta_hours
            total_cost = energy_cost + start_cost + peak_cost + balance_cost + runtime_cost + oversupply_cost
            candidate = (total_cost, [d.name for d in subset], power_by_line, total_power)
            if best is None or candidate[0] < best[0]:
                best = candidate

    if best is None:
        best = (0.0, [], {"L1": 0.0, "L2": 0.0}, 0.0)
    selected_names = best[1]
    power_map = {name: next(d.rated_power_kw for d in devices if d.name == name) for name in selected_names}
    return selected_names, power_map, best[2], best[3]


def choose_compressors(
    required_bog_kgph: float,
    unload_active: bool,
    devices: List[Device],
    prev_level: Dict[str, float],
    runtime_steps: Dict[str, int],
    line_loads: Dict[str, float],
    current_peak: Dict[str, float],
    par: Params,
    price: float,
) -> Tuple[Dict[str, float], Dict[str, float], Dict[str, float], float, float]:
    for d in devices:
        if d.line not in {"L1", "L2"}:
            raise ValueError(f"COMP device {d.name} has invalid line: {d.line!r}")

    levels = list(par.comp_levels)
    power_by_level = {}
    for idx, level in enumerate(levels):
        if idx < len(par.comp_power_levels):
            power_by_level[level] = par.comp_power_levels[idx]
        else:
            power_by_level[level] = level * devices[0].rated_power_kw
    best = None
    for combo in product(levels, repeat=len(devices)):
        if unload_active and max(combo) < par.unload_comp_min_level:
            continue
        cap = 0.0
        total_power = 0.0
        power_by_line = {"L1": 0.0, "L2": 0.0}
        level_map = {}
        power_map = {}
        switch_cost = 0.0
        for dev, level in zip(devices, combo):
            level_map[dev.name] = level
            cap += level * dev.capacity_kgph
            power = power_by_level.get(level, level * dev.rated_power_kw)
            power_map[dev.name] = power
            total_power += power
            power_by_line[dev.line] += power
            switch_cost += par.switch_penalty * abs(level - prev_level.get(dev.name, 0.0))
        shortage = max(0.0, required_bog_kgph - cap)
        shortage_penalty = 8.0 * shortage
        line_after = {
            "L1": line_loads["L1"] + power_by_line["L1"],
            "L2": line_loads["L2"] + power_by_line["L2"],
        }
        peak_cost = incremental_peak_cost(line_after, current_peak, par.demand_charge_price)
        balance_cost = par.line_balance_weight * abs(line_after["L1"] - line_after["L2"])
        runtime_cost = par.runtime_weight * sum(runtime_steps.get(dev.name, 0) * combo[idx] for idx, dev in enumerate(devices))
        energy_cost = total_power * price * par.delta_hours
        total_cost = energy_cost + switch_cost + peak_cost + balance_cost + runtime_cost + shortage_penalty + 0.02 * max(0.0, cap - required_bog_kgph)
        candidate = (total_cost, level_map, power_map, power_by_line, total_power, cap)
        if best is None or candidate[0] < best[0]:
            best = candidate

    if best is None:
        level_map = {dev.name: 0.0 for dev in devices}
        power_map = {dev.name: 0.0 for dev in devices}
        return level_map, power_map, {"L1": 0.0, "L2": 0.0}, 0.0, 0.0
    return best[1], best[2], best[3], best[4], best[5]


def choose_orv_units(required_count: int, devices: List[Device], prev_on: Dict[str, int], runtime_steps: Dict[str, int]) -> List[str]:
    if required_count <= 0:
        return []
    ordered = sorted(devices, key=lambda d: (0 if prev_on.get(d.name, 0) == 1 else 1, runtime_steps.get(d.name, 0), d.name))
    return [d.name for d in ordered[:required_count]]


def plan_receive_allocation(tank_volumes: Dict[str, float], unload_m3h: float, par: Params) -> Dict[str, float]:
    allocation = {t.name: 0.0 for t in par.tanks}
    if unload_m3h <= 1e-9:
        return allocation
    volume_to_place = unload_m3h * par.delta_hours
    candidates = []
    for tank in par.tanks:
        max_volume = tank.level_max_m * tank.area_m2
        free_volume = max(0.0, max_volume - tank_volumes[tank.name])
        candidates.append((free_volume, tank.name))
    candidates.sort(reverse=True)
    for free_volume, tank_name in candidates:
        if volume_to_place <= 1e-9:
            break
        take = min(free_volume, volume_to_place)
        allocation[tank_name] += take
        volume_to_place -= take
    return allocation


def distribute_lp_outflow(selected_lp: List[str], flow_map: Dict[str, float], device_index: Dict[str, Device], par: Params) -> Dict[str, float]:
    outflow = {}
    for name in selected_lp:
        tank = device_index[name].tank
        outflow[tank] = outflow.get(tank, 0.0) + flow_map[name] * par.delta_hours
    return outflow


def build_projected_inventory_profile(par: Params) -> np.ndarray:
    total_volume = sum(t.level_init_m * t.area_m2 for t in par.tanks)
    profile = []
    total_capacity = sum(t.level_max_m * t.area_m2 for t in par.tanks)
    for i in range(par.n_steps):
        inflow = par.unload_profile[i] * par.delta_hours
        outflow = par.sendout_profile[i] * par.delta_hours
        total_volume = float(np.clip(total_volume + inflow - outflow, 0.0, total_capacity))
        profile.append(total_volume)
    return np.array(profile)


def predict_bog_profile(
    par: Params,
    projected_inventory_m3: np.ndarray,
    lp_power_trace_kw: Optional[np.ndarray] = None,
) -> pd.DataFrame:
    lp_power_trace_kw = lp_power_trace_kw if lp_power_trace_kw is not None else np.maximum(440.0, par.sendout_profile * 0.45)
    temp = par.weather["temp_c"].to_numpy()
    pressure = par.weather["pressure_kpa"].to_numpy()
    dpressure = np.concatenate(([0.0], np.diff(pressure)))
    unload = par.unload_profile.copy()
    demand = par.sendout_profile.copy()

    static_bor = projected_inventory_m3 * par.rho_lng * par.tank_daily_bor / 24.0
    pipe_heat_kw = par.pipe_heat_u * par.pipe_heat_area_m2 * np.maximum(temp - par.t_lng, 0.0) / 1000.0
    wall_heat_bog = pipe_heat_kw * 3600.0 / par.latent_heat_kjkg * par.pipe_bog_share
    pump_heat_bog = lp_power_trace_kw * par.pump_heat_fraction * 3600.0 / par.latent_heat_kjkg
    piston_bog = unload * par.bog_density_kgm3
    flash_bog = unload * par.rho_lng * par.flash_fraction

    bog_base = static_bor + wall_heat_bog + pump_heat_bog + piston_bog + flash_bog
    atm_factor = 1.0 + par.k_atm * (par.atm_ref_kpa - pressure) + par.k_dpatm * dpressure
    atm_factor = np.clip(atm_factor, 0.82, 1.20)
    bog_mech = bog_base * atm_factor

    level_ratio = projected_inventory_m3 / max(1.0, sum(t.level_max_m * t.area_m2 for t in par.tanks))
    demand_ratio = demand / max(1.0, np.max(demand))
    unload_ratio = unload / max(1.0, np.max(unload) if np.max(unload) > 0 else 1.0)
    data_correction = 1.0 + 0.06 * demand_ratio + 0.11 * unload_ratio + 0.04 * (temp - temp.mean()) / max(1.0, temp.std()) + 0.05 * level_ratio
    bog_pred = np.maximum(bog_mech * data_correction, 0.0)

    return pd.DataFrame({
        "timestamp": par.timestamps,
        "static_bor_kgph": static_bor,
        "wall_heat_bog_kgph": wall_heat_bog,
        "pump_heat_bog_kgph": pump_heat_bog,
        "piston_bog_kgph": piston_bog,
        "flash_bog_kgph": flash_bog,
        "atm_factor": atm_factor,
        "bog_mech_kgph": bog_mech,
        "bog_pred_kgph": bog_pred,
    })


def build_event_notes(prev_state: Dict[str, float], curr_state: Dict[str, float], curr_phase: str, prev_phase: str) -> str:
    notes = []
    if curr_phase != prev_phase:
        notes.append(f"卸船阶段={curr_phase}")
    for name in sorted(curr_state.keys()):
        before = prev_state.get(name, 0.0)
        after = curr_state.get(name, 0.0)
        if abs(before - after) < 1e-9:
            continue
        if before == 0 and after > 0:
            notes.append(f"{name}启动")
        elif before > 0 and after == 0:
            notes.append(f"{name}停运")
        else:
            notes.append(f"{name}调整至{after:.2f}")
    return "；".join(notes)


def dispatch_terminal(par: Params, bog_df: pd.DataFrame) -> Dict[str, pd.DataFrame]:
    hp_devices = par.devices_by_category["HP"]
    lp_devices = par.devices_by_category["LP"]
    sw_devices = par.devices_by_category["SW"]
    comp_devices = par.devices_by_category["COMP"]
    orv_devices = par.devices_by_category["ORV"]
    device_index = par.device_index

    tank_volumes = {t.name: t.level_init_m * t.area_m2 for t in par.tanks}
    current_peak = {"L1": 0.0, "L2": 0.0}
    cumulative_energy_cost = 0.0
    cumulative_peak_cost = 0.0

    prev_on = {d.name: 0 for d in par.devices}
    prev_comp_level = {d.name: 0.0 for d in comp_devices}
    runtime_steps = {d.name: 0 for d in par.devices}
    prev_phase = "待机"

    summary_records = []
    tank_records = []
    device_records = []
    line_records = []

    for step, ts in enumerate(par.timestamps):
        price = float(par.energy_price[step])
        demand = float(par.sendout_profile[step])
        bog = float(bog_df.loc[step, "bog_pred_kgph"])
        unload = float(par.unload_profile[step])
        phase = par.unload_phase[step]
        tank_levels = {t.name: tank_volumes[t.name] / t.area_m2 for t in par.tanks}

        receive_allocation = plan_receive_allocation(tank_volumes, unload, par)
        receiving_tanks = [k for k, v in receive_allocation.items() if v > 0]

        line_loads = {"L1": 0.0, "L2": 0.0}
        line_reactive = {"L1": 0.0, "L2": 0.0}

        hp_sel, hp_flow_map, hp_line_power, hp_total_power, hp_recirc = choose_hp_pumps(
            demand, hp_devices, prev_on, runtime_steps, line_loads, current_peak, par, price
        )
        line_loads["L1"] += hp_line_power["L1"]
        line_loads["L2"] += hp_line_power["L2"]
        for name in hp_sel:
            dev = device_index[name]
            line_reactive[dev.line] += dev.reactive_kvar * (hp_total_power / max(1.0, len(hp_sel))) / dev.rated_power_kw
        hp_internal_total = sum(hp_flow_map.values())
        actual_output = demand

        recondenser_target_m3h = bog / max(par.rho_lng * par.recondenser_factor, 1e-6)
        lp_required = max(hp_internal_total * 1.08 + 40.0, actual_output + recondenser_target_m3h)
        lp_sel, lp_flow_map, lp_line_power, lp_total_power, lp_recirc = choose_lp_pumps(
            lp_required, lp_devices, prev_on, runtime_steps, line_loads, current_peak, tank_levels, receiving_tanks, par, price
        )
        line_loads["L1"] += lp_line_power["L1"]
        line_loads["L2"] += lp_line_power["L2"]
        for name in lp_sel:
            dev = device_index[name]
            line_reactive[dev.line] += dev.reactive_kvar * (lp_total_power / max(1.0, len(lp_sel))) / dev.rated_power_kw
        lp_internal_total = sum(lp_flow_map.values())

        recondenser_cap = max(0.0, lp_internal_total - actual_output) * par.rho_lng * par.recondenser_factor
        comp_required = max(0.0, bog - recondenser_cap)
        comp_level_map, comp_power_map, comp_line_power, _, comp_capacity = choose_compressors(
            comp_required, unload > 0.0, comp_devices, prev_comp_level, runtime_steps, line_loads, current_peak, par, price
        )
        line_loads["L1"] += comp_line_power["L1"]
        line_loads["L2"] += comp_line_power["L2"]
        for dev in comp_devices:
            if comp_power_map[dev.name] > 0:
                line_reactive[dev.line] += dev.reactive_kvar * comp_power_map[dev.name] / dev.rated_power_kw

        sw_required = actual_output * par.rho_lng * par.latent_heat_kjkg / (par.rho_sw * par.cp_sw * par.delta_t_sw)
        sw_sel, sw_power_map, sw_line_power, _ = choose_sw_pumps(
            sw_required, sw_devices, prev_on, runtime_steps, line_loads, current_peak, par, price
        )
        line_loads["L1"] += sw_line_power["L1"]
        line_loads["L2"] += sw_line_power["L2"]
        for name in sw_sel:
            dev = device_index[name]
            line_reactive[dev.line] += dev.reactive_kvar

        orv_required = int(math.ceil(actual_output / par.orv_unit_capacity_m3h)) if actual_output > 1e-9 else 0
        orv_required = min(orv_required, len(orv_devices))
        orv_sel = choose_orv_units(orv_required, orv_devices, prev_on, runtime_steps)

        lp_outflow_by_tank = distribute_lp_outflow(lp_sel, lp_flow_map, device_index, par)
        curr_state_numeric = {d.name: 0.0 for d in par.devices}
        for name in hp_sel:
            curr_state_numeric[name] = 1.0
        for name in lp_sel:
            curr_state_numeric[name] = 1.0
        for name in sw_sel:
            curr_state_numeric[name] = 1.0
        for name, level in comp_level_map.items():
            curr_state_numeric[name] = level
        for name in orv_sel:
            curr_state_numeric[name] = 1.0
        event_note = build_event_notes(prev_comp_level | {k: float(prev_on.get(k, 0)) for k in prev_on}, curr_state_numeric, phase, prev_phase)

        tank_status = {}
        tank_in_m3 = {}
        tank_out_m3 = {}
        for tank in par.tanks:
            inflow_m3 = receive_allocation[tank.name]
            outflow_m3 = lp_outflow_by_tank.get(tank.name, 0.0)
            tank_volumes[tank.name] += inflow_m3 - outflow_m3
            tank_volumes[tank.name] = float(np.clip(tank_volumes[tank.name], 0.0, tank.level_max_m * tank.area_m2))
            level = tank_volumes[tank.name] / tank.area_m2
            flags = []
            if inflow_m3 > 1e-9:
                flags.append("接卸")
            if outflow_m3 > 1e-9:
                flags.append("外输")
            if level <= tank.level_min_m + 1e-6:
                flags.append("低液位")
            if level >= tank.level_max_m - 1e-6:
                flags.append("高液位")
            tank_status[tank.name] = "+".join(flags) if flags else "备用"
            tank_in_m3[tank.name] = inflow_m3
            tank_out_m3[tank.name] = outflow_m3
            tank_records.append({
                "timestamp": ts,
                "tank": tank.name,
                "phase": tank.phase,
                "level_m": level,
                "volume_m3": tank_volumes[tank.name],
                "status": tank_status[tank.name],
                "inflow_m3": inflow_m3,
                "outflow_m3": outflow_m3,
            })

        step_peak_cost = incremental_peak_cost(line_loads, current_peak, par.demand_charge_price)
        current_peak["L1"] = max(current_peak["L1"], line_loads["L1"])
        current_peak["L2"] = max(current_peak["L2"], line_loads["L2"])
        step_energy_cost = (line_loads["L1"] + line_loads["L2"]) * price * par.delta_hours
        cumulative_energy_cost += step_energy_cost
        cumulative_peak_cost += step_peak_cost

        line1_apparent = math.sqrt(line_loads["L1"] ** 2 + line_reactive["L1"] ** 2)
        line2_apparent = math.sqrt(line_loads["L2"] ** 2 + line_reactive["L2"] ** 2)
        line1_pf = line_loads["L1"] / line1_apparent if line1_apparent > 1e-9 else 1.0
        line2_pf = line_loads["L2"] / line2_apparent if line2_apparent > 1e-9 else 1.0

        for dev in par.devices:
            state = curr_state_numeric.get(dev.name, 0.0)
            power = 0.0
            flow = 0.0
            unit = ""
            if dev.category == "HP" and dev.name in hp_flow_map:
                power = calc_quadratic_power(par.hp_pq_coeffs, hp_flow_map[dev.name], dev.rated_power_kw)
                flow = hp_flow_map[dev.name]
                unit = "m3/h"
            elif dev.category == "LP" and dev.name in lp_flow_map:
                power = calc_quadratic_power(par.lp_pq_coeffs, lp_flow_map[dev.name], dev.rated_power_kw)
                flow = lp_flow_map[dev.name]
                unit = "m3/h"
            elif dev.category == "SW" and dev.name in sw_power_map:
                power = sw_power_map[dev.name]
                flow = dev.max_flow
                unit = "m3/h"
            elif dev.category == "COMP":
                power = comp_power_map.get(dev.name, 0.0)
                flow = comp_level_map.get(dev.name, 0.0) * dev.capacity_kgph
                unit = "kg/h"
            elif dev.category == "ORV" and dev.name in orv_sel:
                power = 0.0
                flow = dev.max_flow
                unit = "m3/h"
            device_records.append({
                "timestamp": ts,
                "device": dev.name,
                "category": dev.category,
                "line": dev.line or "",
                "phase": dev.phase,
                "status_value": state,
                "power_kw": power,
                "load_or_flow": flow,
                "flow_unit": unit,
                "tank": dev.tank or "",
            })
            if state > 0:
                runtime_steps[dev.name] += 1

        alarm_line1 = int(line_loads["L1"] >= par.line_alarm_kw["L1"])
        alarm_line2 = int(line_loads["L2"] >= par.line_alarm_kw["L2"])
        summary_records.append({
            "timestamp": ts,
            "date": ts.strftime("%Y-%m-%d"),
            "time": ts.strftime("%H:%M"),
            "unload_phase": phase,
            "demand_target_m3h": demand,
            "actual_output_m3h": actual_output,
            "unload_inflow_m3h": unload,
            "bog_pred_kgph": bog,
            "bog_to_recondenser_kgph": min(bog, recondenser_cap),
            "bog_to_compressor_kgph": max(0.0, bog - min(bog, recondenser_cap)),
            "compressor_capacity_kgph": comp_capacity,
            "lp_internal_flow_m3h": lp_internal_total,
            "hp_internal_flow_m3h": hp_internal_total,
            "hp_recirculation_m3h": hp_recirc,
            "lp_recirculation_m3h": lp_recirc,
            "orv_running": len(orv_sel),
            "hp_running": len(hp_sel),
            "lp_running": len(lp_sel),
            "sw_running": len(sw_sel),
            "line1_kw": line_loads["L1"],
            "line2_kw": line_loads["L2"],
            "line1_kvar": line_reactive["L1"],
            "line2_kvar": line_reactive["L2"],
            "line1_pf": line1_pf,
            "line2_pf": line2_pf,
            "line1_peak_kw": current_peak["L1"],
            "line2_peak_kw": current_peak["L2"],
            "step_energy_cost_cny": step_energy_cost,
            "step_peak_increment_cny": step_peak_cost,
            "cumulative_energy_cost_cny": cumulative_energy_cost,
            "cumulative_peak_cost_cny": cumulative_peak_cost,
            "electricity_price_cnykwh": price,
            "line1_alarm": alarm_line1,
            "line2_alarm": alarm_line2,
            "event_note": event_note,
        })
        line_records.append({
            "timestamp": ts,
            "line": "L1",
            "active_kw": line_loads["L1"],
            "reactive_kvar": line_reactive["L1"],
            "apparent_kva": line1_apparent,
            "power_factor": line1_pf,
            "peak_kw": current_peak["L1"],
        })
        line_records.append({
            "timestamp": ts,
            "line": "L2",
            "active_kw": line_loads["L2"],
            "reactive_kvar": line_reactive["L2"],
            "apparent_kva": line2_apparent,
            "power_factor": line2_pf,
            "peak_kw": current_peak["L2"],
        })

        prev_on = {k: int(v > 0) for k, v in curr_state_numeric.items()}
        prev_comp_level = {name: comp_level_map.get(name, 0.0) for name in prev_comp_level}
        prev_phase = phase

    return {
        "summary": pd.DataFrame(summary_records),
        "tank": pd.DataFrame(tank_records),
        "device": pd.DataFrame(device_records),
        "line": pd.DataFrame(line_records),
    }


def build_output_tables(results: Dict[str, pd.DataFrame], bog_df: pd.DataFrame, par: Params) -> Dict[str, pd.DataFrame]:
    summary = results["summary"].copy()
    tank_df = results["tank"].copy()
    device_df = results["device"].copy()
    line_df = results["line"].copy()

    pivot_lp = device_df[device_df["category"] == "LP"].pivot(index="timestamp", columns="device", values="status_value").fillna(0)
    pivot_hp = device_df[device_df["category"] == "HP"].pivot(index="timestamp", columns="device", values="status_value").fillna(0)
    pivot_sw = device_df[device_df["category"] == "SW"].pivot(index="timestamp", columns="device", values="status_value").fillna(0)
    pivot_orv = device_df[device_df["category"] == "ORV"].pivot(index="timestamp", columns="device", values="status_value").fillna(0)
    pivot_comp = device_df[device_df["category"] == "COMP"].pivot(index="timestamp", columns="device", values="status_value").fillna(0)
    tank_status = tank_df.pivot(index="timestamp", columns="tank", values="status")
    tank_level = tank_df.pivot(index="timestamp", columns="tank", values="level_m")

    two_hour_log = summary.iloc[:: int(120 / par.delta_minutes), :][[
        "timestamp", "line1_kw", "line2_kw", "line1_peak_kw", "line2_peak_kw", "event_note", "unload_phase"
    ]].copy()

    kpi = pd.DataFrame([
        {"metric": "仿真起点", "value": par.start_time.strftime("%Y-%m-%d %H:%M")},
        {"metric": "仿真步长(min)", "value": par.delta_minutes},
        {"metric": "仿真时长(h)", "value": par.horizon_hours},
        {"metric": "日外输目标(m3/day)", "value": par.total_output_target_m3_day},
        {"metric": "仿真期外输总量(m3)", "value": round(summary["actual_output_m3h"].sum() * par.delta_hours, 2)},
        {"metric": "仿真期卸船总量(m3)", "value": round(summary["unload_inflow_m3h"].sum() * par.delta_hours, 2)},
        {"metric": "线1最大需量(kW)", "value": round(summary["line1_peak_kw"].max(), 2)},
        {"metric": "线2最大需量(kW)", "value": round(summary["line2_peak_kw"].max(), 2)},
        {"metric": "估算电度电费(元)", "value": round(summary["step_energy_cost_cny"].sum(), 2)},
        {"metric": "估算基本电费(元)", "value": round(summary["line1_peak_kw"].max() * par.demand_charge_price + summary["line2_peak_kw"].max() * par.demand_charge_price, 2)},
        {"metric": "估算总电费(元)", "value": round(summary["step_energy_cost_cny"].sum() + summary["line1_peak_kw"].max() * par.demand_charge_price + summary["line2_peak_kw"].max() * par.demand_charge_price, 2)},
        {"metric": "最大BOG(kg/h)", "value": round(bog_df["bog_pred_kgph"].max(), 2)},
    ])

    return {
        "summary_timeseries": summary,
        "bog_components": bog_df,
        "tank_timeseries": tank_df,
        "device_status_long": device_df,
        "line_loads": line_df,
        "lp_onoff": pivot_lp.reset_index(),
        "hp_onoff": pivot_hp.reset_index(),
        "sw_onoff": pivot_sw.reset_index(),
        "orv_onoff": pivot_orv.reset_index(),
        "compressor_levels": pivot_comp.reset_index(),
        "tank_status_wide": tank_status.reset_index(),
        "tank_level_wide": tank_level.reset_index(),
        "line_power_log_2h": two_hour_log,
        "kpi_summary": kpi,
    }


def run_engineering_simulation() -> Dict[str, object]:
    par = Params()

    inventory_profile = build_projected_inventory_profile(par)
    bog_pass1 = predict_bog_profile(par, inventory_profile)
    results_pass1 = dispatch_terminal(par, bog_pass1)
    lp_power_trace = results_pass1["device"].query("category == 'LP'").groupby("timestamp")["power_kw"].sum().reindex(par.timestamps, fill_value=0.0).to_numpy()
    total_inventory_trace = results_pass1["tank"].groupby("timestamp")["volume_m3"].sum().reindex(par.timestamps, fill_value=0.0).to_numpy()
    bog_final = predict_bog_profile(par, total_inventory_trace, lp_power_trace_kw=lp_power_trace)
    results = dispatch_terminal(par, bog_final)
    tables = build_output_tables(results, bog_final, par)

    summary = tables["summary_timeseries"]
    run_summary = {
        "start_time": par.start_time.strftime("%Y-%m-%d %H:%M"),
        "horizon_hours": par.horizon_hours,
        "line1_peak_kw": float(summary["line1_peak_kw"].max()),
        "line2_peak_kw": float(summary["line2_peak_kw"].max()),
        "energy_cost_cny": float(summary["step_energy_cost_cny"].sum()),
        "demand_charge_cny": float(summary["line1_peak_kw"].max() * par.demand_charge_price + summary["line2_peak_kw"].max() * par.demand_charge_price),
        "total_cost_cny": float(summary["step_energy_cost_cny"].sum() + summary["line1_peak_kw"].max() * par.demand_charge_price + summary["line2_peak_kw"].max() * par.demand_charge_price),
        "total_output_m3": float(summary["actual_output_m3h"].sum() * par.delta_hours),
        "total_unload_m3": float(summary["unload_inflow_m3h"].sum() * par.delta_hours),
    }

    return {
        "params": par,
        "tables": tables,
        "summary": run_summary,
    }



class CompatParams(Params):
    pending_algorithm_params: Optional[Dict[str, object]] = None
    pending_weather_data: Optional[Dict[str, object]] = None
    pending_unload_plan: Optional[List[float]] = None
    required_algorithm_keys = [
        "total_output_target_m3_day",
        "tank_level_max",
        "tank_level_min",
        "num_tanks",
        "s_tan",
        "initial_liquid",
        "rho_lng",
        "rho_sw",
        "cp_sw",
        "delta_t_sw",
        "latent_heat_kjkg",
        "num_lp_pumps",
        "num_hp_pumps",
        "num_sw_big",
        "num_sw_small",
        "num_orv",
        "num_compressors",
        "lp_pq_coeffs",
        "lp_flow_min",
        "lp_flow_max",
        "lp_power_max",
        "hp_pq_coeffs",
        "hp_flow_min",
        "hp_flow_max",
        "hp_power_max",
        "sw_big_flow",
        "sw_big_power",
        "sw_small_flow",
        "sw_small_power",
        "comp_levels",
        "comp_power_levels",
        "comp_capacity_kgph",
        "orv_unit_capacity_m3h",
        "lp_target_pressure",
        "hp_target_pressure",
        "start_time",
        "peak_elec_price",
        "flat_elec_price",
        "valley_elec_price",
        "t_lng",
        "pipe_heat_u",
        "pump_heat_fraction",
        "flash_fraction",
        "bog_density_kgm3",
        "k_atm",
        "k_dpatm",
        "demand_charge_price",
        "line_alarm_kw",
        "tank_daily_bor",
        "pipe_heat_area_m2",
        "recondenser_factor",
        "pipe_bog_share",
        "atm_ref_kpa",
        "unload_comp_min_level",
        "daily_demand_curve",
        "tank_master_data",
        "device_master_data",
    ]

    def __init__(
        self,
        base_dir: Optional[str] = None,
        algorithm_params: Optional[Dict[str, object]] = None,
        weather_data: Optional[Dict[str, object]] = None,
        unload_plan: Optional[List[float]] = None,
    ):
        if algorithm_params is None:
            algorithm_params = self.__class__.pending_algorithm_params
        if weather_data is None:
            weather_data = self.__class__.pending_weather_data
        if unload_plan is None:
            unload_plan = self.__class__.pending_unload_plan
        params = algorithm_params or {}
        self._ensure_required_algorithm_params(params)
        self.base_dir = base_dir or os.path.dirname(os.path.abspath(__file__))
        self.random_seed = 42
        self.start_time = self._parse_start_time(params["start_time"])
        self.delta_minutes = 15
        self.delta_hours = self.delta_minutes / 60.0
        self.horizon_hours = 24
        self.n_steps = int(self.horizon_hours / self.delta_hours)
        self.timestamps = [self.start_time + timedelta(minutes=self.delta_minutes * i) for i in range(self.n_steps)]
        self._initialize_local_defaults()
        self._apply_legacy_algorithm_params(params)
        self.weather = self._build_weather_profile()
        self.unload_profile, self.unload_phase = self._build_unload_profile()
        self._apply_weather_override(weather_data)
        self._apply_unload_override(unload_plan)
        self.sync_derived_fields()

    def _initialize_local_defaults(self) -> None:
        self.allowed_delivery_bias = 0.05
        self.start_penalty = {"HP": 260.0, "LP": 40.0, "SW": 220.0, "COMP": 180.0}
        self.recirc_penalty = {"HP": 3.5, "LP": 1.0}
        self.line_balance_weight = 0.06
        self.runtime_weight = 0.002
        self.switch_penalty = 30.0

    def _apply_legacy_algorithm_params(self, params: Dict[str, object]) -> None:
        self.total_output_target_m3_day = self._coerce_float(params["total_output_target_m3_day"])
        self.demand_charge_price = self._coerce_float(params["demand_charge_price"])
        self.line_alarm_kw = self._coerce_keyed_float_map(params["line_alarm_kw"])
        self.tank_daily_bor = self._coerce_float(params["tank_daily_bor"])
        self.pipe_heat_area_m2 = self._coerce_float(params["pipe_heat_area_m2"])
        self.recondenser_factor = self._coerce_float(params["recondenser_factor"])
        self.pipe_bog_share = self._coerce_float(params["pipe_bog_share"])
        self.atm_ref_kpa = self._coerce_float(params["atm_ref_kpa"])
        self.unload_comp_min_level = self._coerce_float(params["unload_comp_min_level"])
        self.rho_lng = self._coerce_float(params["rho_lng"])
        self.rho_sw = self._coerce_float(params["rho_sw"])
        self.cp_sw = self._coerce_float(params["cp_sw"])
        self.delta_t_sw = self._coerce_float(params["delta_t_sw"])
        self.latent_heat_kjkg = self._coerce_float(params["latent_heat_kjkg"])
        self.t_lng = self._coerce_float(params["t_lng"])
        self.pipe_heat_u = self._coerce_float(params["pipe_heat_u"])
        self.bog_density_kgm3 = self._coerce_float(params["bog_density_kgm3"])
        self.lp_pq_coeffs = self._coerce_float_list(params["lp_pq_coeffs"])
        self.hp_pq_coeffs = self._coerce_float_list(params["hp_pq_coeffs"])
        self.pump_heat_fraction = self._coerce_float(params["pump_heat_fraction"])
        self.flash_fraction = self._coerce_float(params["flash_fraction"])
        self.k_atm = self._coerce_float(params["k_atm"])
        self.k_dpatm = self._coerce_float(params["k_dpatm"])
        self.orv_unit_capacity_m3h = self._coerce_float(params["orv_unit_capacity_m3h"])
        self.lp_target_pressure = self._coerce_float(params["lp_target_pressure"])
        self.hp_target_pressure = self._coerce_float(params["hp_target_pressure"])
        self.comp_levels = self._coerce_float_list(params["comp_levels"])
        self.comp_power_levels = self._coerce_float_list(params["comp_power_levels"])
        self.daily_demand_curve = self._coerce_float_list(params["daily_demand_curve"])
        self.energy_price_hourly = self._build_energy_price_hourly_from_params(params)
        hourly_cycles = int(math.ceil(self.horizon_hours / 24.0))
        self.energy_price = np.repeat(np.tile(self.energy_price_hourly, hourly_cycles), int(60 / self.delta_minutes))[: self.n_steps]
        self.sendout_profile = self._build_sendout_profile()
        self.tanks = self._build_tanks_from_backend_params(params)
        self.devices = self._build_devices_from_backend_params(params)
        self.devices_by_category = self._group_devices_by_category()
        self.device_index = {d.name: d for d in self.devices}
        self.total_output_target_horizon = float(np.sum(self.sendout_profile) * self.delta_hours)

    def _ensure_required_algorithm_params(self, params: Dict[str, object]) -> None:
        missing = []
        for key in self.required_algorithm_keys:
            value = params.get(key)
            if value is None:
                missing.append(key)
                continue
            if isinstance(value, (list, tuple, np.ndarray)) and len(value) == 0:
                missing.append(key)
        if missing:
            raise ValueError(f"Missing required algorithmParams keys: {', '.join(missing)}")

    @staticmethod
    def _coerce_float(value: object) -> float:
        return float(value)

    @staticmethod
    def _coerce_int(value: object) -> int:
        return int(round(float(value)))

    @staticmethod
    def _coerce_float_list(value: object) -> List[float]:
        return [float(item) for item in value]

    @staticmethod
    def _coerce_keyed_float_map(value: object) -> Dict[str, float]:
        if not isinstance(value, dict):
            raise ValueError(f"Expected object mapping, got: {value}")
        return {str(key): float(item) for key, item in value.items()}

    @staticmethod
    def _coerce_mapping_list(value: object, field_name: str) -> List[Dict[str, object]]:
        if not isinstance(value, list):
            raise ValueError(f"{field_name} must be a list, got: {value}")
        result: List[Dict[str, object]] = []
        for item in value:
            if not isinstance(item, dict):
                raise ValueError(f"{field_name} entries must be objects, got: {item}")
            result.append({str(key): item[key] for key in item})
        return result

    @staticmethod
    def _parse_start_time(value: object) -> datetime:
        if isinstance(value, datetime):
            return value
        text = str(value).strip()
        for fmt in ("%Y-%m-%d %H:%M:%S", "%Y-%m-%d %H:%M", "%Y-%m-%dT%H:%M:%S", "%Y-%m-%dT%H:%M"):
            try:
                return datetime.strptime(text, fmt)
            except ValueError:
                continue
        try:
            return datetime.fromisoformat(text)
        except ValueError as exc:
            raise ValueError(f"Invalid start_time value: {value}") from exc

    def _build_energy_price_hourly_from_params(self, params: Dict[str, object]) -> np.ndarray:
        valley = self._coerce_float(params["valley_elec_price"])
        flat = self._coerce_float(params["flat_elec_price"])
        peak = self._coerce_float(params["peak_elec_price"])
        return np.array([
            flat, flat, flat, flat, flat, flat,
            flat, flat, valley, valley, valley, valley,
            valley, valley, valley, valley, valley, valley,
            valley, peak, peak, peak, peak, flat,
        ], dtype=float)

    def _build_tanks_from_backend_params(self, params: Dict[str, object]) -> List[Tank]:
        tank_rows = self._coerce_mapping_list(params["tank_master_data"], "tank_master_data")
        tanks: List[Tank] = []
        for item in tank_rows:
            tanks.append(Tank(
                name=str(item["name"]),
                phase=str(item["phase"]),
                area_m2=self._coerce_float(item["area_m2"]),
                level_min_m=self._coerce_float(item["level_min_m"]),
                level_pump_start_m=self._coerce_float(item["level_pump_start_m"]),
                level_max_m=self._coerce_float(item["level_max_m"]),
                level_init_m=self._coerce_float(item["level_init_m"]),
            ))
        if not tanks:
            raise ValueError("tank_master_data is empty.")
        return tanks

    def _build_devices_from_backend_params(self, params: Dict[str, object]) -> List[Device]:
        device_rows = self._coerce_mapping_list(params["device_master_data"], "device_master_data")
        devices: List[Device] = []
        valid_tank_names = {tank.name for tank in self.tanks}
        categories_requiring_line = {"HP", "LP", "SW", "COMP"}
        for item in device_rows:
            name = str(item["name"])
            category = str(item["category"])
            tank_name_raw = item.get("tank")
            tank_name = None if tank_name_raw in (None, "") else str(tank_name_raw)
            if category == "LP" and tank_name not in valid_tank_names:
                raise ValueError(f"LP device {name} references unknown tank: {tank_name}")
            line_raw = item.get("line")
            line = None if line_raw in (None, "") else str(line_raw)
            if category in categories_requiring_line and line not in {"L1", "L2"}:
                raise ValueError(
                    f"Device {name} (category={category}) requires line to be 'L1' or 'L2', got: {line_raw!r}"
                )
            devices.append(Device(
                name=name,
                category=category,
                line=line,
                rated_power_kw=self._coerce_float(item["rated_power_kw"]),
                reactive_kvar=self._coerce_float(item["reactive_kvar"]),
                min_flow=self._coerce_float(item["min_flow"]),
                max_flow=self._coerce_float(item["max_flow"]),
                tank=tank_name,
                capacity_kgph=self._coerce_float(item["capacity_kgph"]),
                phase=str(item["phase"]),
            ))
        if not devices:
            raise ValueError("device_master_data is empty.")
        return devices

    def _apply_weather_override(self, weather_data: Optional[Dict[str, object]]) -> None:
        if not weather_data:
            return
        temp_default = self.weather["temp_c"].to_numpy(dtype=float)
        pressure_default = self.weather["pressure_kpa"].to_numpy(dtype=float)
        humidity_default = self.weather["humidity"].to_numpy(dtype=float)
        temp = np.asarray(weather_data.get("Temp", temp_default), dtype=float)
        pressure = np.asarray(weather_data.get("Pressure", pressure_default), dtype=float)
        humidity = np.asarray(weather_data.get("Humidity", humidity_default), dtype=float)
        if temp.size != self.n_steps or pressure.size != self.n_steps:
            raise ValueError(f"weather_data must have length {self.n_steps}")
        if humidity.size != self.n_steps:
            humidity = np.resize(humidity, self.n_steps)
        self.weather = pd.DataFrame({
            "timestamp": self.timestamps,
            "temp_c": temp,
            "pressure_kpa": pressure,
            "humidity": humidity,
            "sim_hour": np.arange(self.n_steps) * self.delta_hours,
        })

    def _apply_unload_override(self, unload_plan: Optional[List[float]]) -> None:
        if unload_plan is None:
            return
        values = np.asarray(unload_plan, dtype=float)
        if values.size != self.n_steps:
            raise ValueError(f"unload_plan must have length {self.n_steps}")
        self.unload_profile = values
        self.unload_phase = ["external_input"] * self.n_steps

    def sync_derived_fields(self) -> None:
        self.level_max_m = max(t.level_max_m for t in self.tanks)
        self.level_min_m = min(t.level_min_m for t in self.tanks)
        self.hourly_demand_profile = self.sendout_profile.reshape(-1, int(60 / self.delta_minutes)).mean(axis=1).tolist()


def _extract_unitized_from_tables(tables: Dict[str, pd.DataFrame], par: CompatParams) -> Dict[str, List[List[float]]]:
    def frame_to_rows(df: pd.DataFrame) -> List[List[float]]:
        if df.empty:
            return []
        return df.iloc[:, 1:].to_numpy(dtype=float).tolist()

    sw_df = tables["sw_onoff"]
    sw_devices = {d.name: d for d in par.devices if d.category == "SW"}
    sw_small_cols = [c for c in sw_df.columns[1:] if sw_devices.get(c) and sw_devices[c].rated_power_kw < 1000]
    sw_big_cols = [c for c in sw_df.columns[1:] if sw_devices.get(c) and sw_devices[c].rated_power_kw >= 1000]
    sw_small = sw_df[["timestamp"] + sw_small_cols] if sw_small_cols else pd.DataFrame({"timestamp": sw_df["timestamp"]})
    sw_big = sw_df[["timestamp"] + sw_big_cols] if sw_big_cols else pd.DataFrame({"timestamp": sw_df["timestamp"]})

    return {
        "LP": frame_to_rows(tables["lp_onoff"]),
        "HP": frame_to_rows(tables["hp_onoff"]),
        "SW_Big": frame_to_rows(sw_big),
        "SW_Small": frame_to_rows(sw_small),
        "ORV": frame_to_rows(tables["orv_onoff"]),
        "Comp": frame_to_rows(tables["compressor_levels"]),
    }


def _extract_labels_from_tables(tables: Dict[str, pd.DataFrame], par: CompatParams) -> Dict[str, List[str]]:
    sw_df = tables["sw_onoff"]
    sw_devices = {d.name: d for d in par.devices if d.category == "SW"}
    sw_small_cols = [c for c in sw_df.columns[1:] if sw_devices.get(c) and sw_devices[c].rated_power_kw < 1000]
    sw_big_cols = [c for c in sw_df.columns[1:] if sw_devices.get(c) and sw_devices[c].rated_power_kw >= 1000]

    labels = {
        "LP": tables["lp_onoff"].columns[1:].tolist(),
        "HP": tables["hp_onoff"].columns[1:].tolist(),
        "SW_Big": sw_big_cols,
        "SW_Small": sw_small_cols,
        "ORV": tables["orv_onoff"].columns[1:].tolist(),
        "Comp": tables["compressor_levels"].columns[1:].tolist(),
        "Tank": tables["tank_level_wide"].columns[1:].tolist() if "tank_level_wide" in tables else [],
    }

    # Keep legacy front-end aligned with the original lng2.py defaults.
    default_comp_labels = [
        "BOG压缩机0330-C-01A",
        "BOG压缩机0330-C-01B",
        "BOG压缩机0330-C-01C",
    ]
    if len(labels["Comp"]) < len(default_comp_labels):
        labels["Comp"] = default_comp_labels

    return labels


def _build_tank_level_payload(tables: Dict[str, pd.DataFrame]) -> Dict[str, object]:
    if "tank_level_wide" not in tables or tables["tank_level_wide"].empty:
        return {"hours": [], "series": []}

    tank_wide = tables["tank_level_wide"].copy()
    tank_wide["hour"] = pd.to_datetime(tank_wide["timestamp"]).dt.hour
    agg_map = {col: "mean" for col in tank_wide.columns if col not in {"timestamp"}}
    tank_hourly = tank_wide.groupby("hour", as_index=False).agg(agg_map)
    hours = tank_hourly["hour"].astype(int).tolist()
    series = []
    for col in tank_wide.columns[1:-1]:
        series.append({
            "name": col,
            "values": tank_hourly[col].astype(float).tolist(),
        })
    return {"hours": hours, "series": series}


def _build_legacy_hourly(result: Dict[str, object]) -> List[Dict[str, object]]:
    par: CompatParams = result["params"]
    tables: Dict[str, pd.DataFrame] = result["tables"]
    summary = tables["summary_timeseries"].copy()
    bog = tables["bog_components"].copy()
    tank = tables["tank_timeseries"].copy()
    unitized = _extract_unitized_from_tables(tables, par)

    summary["hour"] = pd.to_datetime(summary["timestamp"]).dt.hour
    bog["hour"] = pd.to_datetime(bog["timestamp"]).dt.hour
    tank["hour"] = pd.to_datetime(tank["timestamp"]).dt.hour

    summary_hourly = summary.groupby("hour", as_index=False).agg({
        "unload_inflow_m3h": "mean",
        "actual_output_m3h": "mean",
        "line1_kw": "mean",
        "line2_kw": "mean",
        "line1_peak_kw": "max",
        "line2_peak_kw": "max",
        "step_energy_cost_cny": "sum",
        "step_peak_increment_cny": "sum",
        "cumulative_energy_cost_cny": "max",
        "cumulative_peak_cost_cny": "max",
    })
    bog_hourly = bog.groupby("hour", as_index=False).agg({
        "bog_pred_kgph": "mean",
        "bog_mech_kgph": "mean",
    })
    tank_hourly = tank.groupby("hour", as_index=False).agg({"level_m": "mean"})

    steps_per_hour = int(60 / par.delta_minutes)

    def hourly_matrix(rows: List[List[float]]) -> np.ndarray:
        if not rows:
            return np.zeros((par.horizon_hours, steps_per_hour, 0))
        return np.asarray(rows, dtype=float).reshape(par.horizon_hours, steps_per_hour, len(rows[0]))

    def hourly_comp_levels(matrix: np.ndarray, hour: int) -> np.ndarray:
        if matrix.shape[-1] == 0:
            return np.array([])
        # Compressor levels are discrete gear positions. Use the maximum level reached
        # within the hour so the hourly trend stays on valid level values instead of
        # showing fractional averages like 0.13 from 15-minute averaging.
        return matrix[hour].max(axis=0)

    lp_hourly = hourly_matrix(unitized["LP"])
    hp_hourly = hourly_matrix(unitized["HP"])
    sw_big_hourly = hourly_matrix(unitized["SW_Big"])
    sw_small_hourly = hourly_matrix(unitized["SW_Small"])
    orv_hourly = hourly_matrix(unitized["ORV"])
    comp_hourly = hourly_matrix(unitized["Comp"])

    records: List[Dict[str, object]] = []
    for hour in range(par.horizon_hours):
        row_summary = summary_hourly.loc[summary_hourly["hour"] == hour]
        row_bog = bog_hourly.loc[bog_hourly["hour"] == hour]
        row_tank = tank_hourly.loc[tank_hourly["hour"] == hour]
        line1 = float(row_summary["line1_kw"].iloc[0]) if not row_summary.empty else 0.0
        line2 = float(row_summary["line2_kw"].iloc[0]) if not row_summary.empty else 0.0
        comp_levels = hourly_comp_levels(comp_hourly, hour)
        records.append({
            "Hour": hour,
            "Unload_m3h": float(row_summary["unload_inflow_m3h"].iloc[0]) if not row_summary.empty else 0.0,
            "BOG_pred_kgph": float(row_bog["bog_pred_kgph"].iloc[0]) if not row_bog.empty else 0.0,
            "LP_Num": int(round(lp_hourly[hour].mean(axis=0).sum())) if lp_hourly.shape[-1] else 0,
            "HP_Num": int(round(hp_hourly[hour].mean(axis=0).sum())) if hp_hourly.shape[-1] else 0,
            "Actual_LNG_Output_m3h": float(row_summary["actual_output_m3h"].iloc[0]) if not row_summary.empty else 0.0,
            "LP_Flow_perPump_m3h": 0.0,
            "HP_Flow_perPump_m3h": 0.0,
            "Ideal_LP_Pressure_MPa": float(getattr(par, "lp_target_pressure", 0.0)),
            "Ideal_HP_Pressure_MPa": float(getattr(par, "hp_target_pressure", 0.0)),
            "SW_Big": int(round(sw_big_hourly[hour].mean(axis=0).sum())) if sw_big_hourly.shape[-1] else 0,
            "SW_Small": int(round(sw_small_hourly[hour].mean(axis=0).sum())) if sw_small_hourly.shape[-1] else 0,
            "ORV_Count": int(round(orv_hourly[hour].mean(axis=0).sum())) if orv_hourly.shape[-1] else 0,
            "Comp1_Level": float(comp_levels[0]) if comp_levels.size >= 1 else 0.0,
            "Comp2_Level": float(comp_levels[1]) if comp_levels.size >= 2 else 0.0,
            "Comp3_Level": float(comp_levels[2]) if comp_levels.size >= 3 else 0.0,
            "Hourly_Power_kW": line1 + line2,
            "Line1_kW": line1,
            "Line2_kW": line2,
            "Line1_Peak_kW": float(row_summary["line1_peak_kw"].iloc[0]) if not row_summary.empty else 0.0,
            "Line2_Peak_kW": float(row_summary["line2_peak_kw"].iloc[0]) if not row_summary.empty else 0.0,
            "Hourly_Cost_CNY": float(row_summary["step_energy_cost_cny"].iloc[0]) if not row_summary.empty else 0.0,
            "Step_Peak_Increment_CNY": float(row_summary["step_peak_increment_cny"].iloc[0]) if not row_summary.empty else 0.0,
            "Cumulative_Energy_Cost_CNY": float(row_summary["cumulative_energy_cost_cny"].iloc[0]) if not row_summary.empty else 0.0,
            "Cumulative_Peak_Cost_CNY": float(row_summary["cumulative_peak_cost_cny"].iloc[0]) if not row_summary.empty else 0.0,
            "Tank_Level_m": float(row_tank["level_m"].iloc[0]) if not row_tank.empty else float(par.level_min_m),
            "Elec_Price": float(par.energy_price_hourly[hour]) if hour < len(par.energy_price_hourly) else 0.0,
        })
    return records


def serialize_simulation_result(result: Dict[str, object]) -> Dict[str, object]:
    par: CompatParams = result["params"]
    tables: Dict[str, pd.DataFrame] = result["tables"]
    summary = dict(result["summary"])
    unitized = _extract_unitized_from_tables(tables, par)
    labels = _extract_labels_from_tables(tables, par)
    hourly = _build_legacy_hourly(result)
    tank_levels = _build_tank_level_payload(tables)

    bog_hourly = tables["bog_components"].copy()
    bog_hourly["hour"] = pd.to_datetime(bog_hourly["timestamp"]).dt.hour
    bog_agg = bog_hourly.groupby("hour", as_index=False).agg({
        "bog_mech_kgph": "mean",
        "bog_pred_kgph": "mean",
        "static_bor_kgph": "mean",
        "wall_heat_bog_kgph": "mean",
        "pump_heat_bog_kgph": "mean",
        "piston_bog_kgph": "mean",
        "flash_bog_kgph": "mean",
    })

    summary.update({
        "estimated_total_electric_cost": summary.get("total_cost_cny", 0.0),
        "timestamp": datetime.now().isoformat(),
    })

    return {
        "summary": summary,
        "hourly": hourly,
        "unitized": unitized,
        "labels": labels,
        "tank_levels": tank_levels,
        "bog": {
            "hours": bog_agg["hour"].astype(int).tolist(),
            "bog_mech_kgph": bog_agg["bog_mech_kgph"].astype(float).tolist(),
            "bog_pred_kgph": bog_agg["bog_pred_kgph"].astype(float).tolist(),
            "static_bor_kgph": bog_agg["static_bor_kgph"].astype(float).tolist(),
            "wall_heat_bog_kgph": bog_agg["wall_heat_bog_kgph"].astype(float).tolist(),
            "pump_heat_bog_kgph": bog_agg["pump_heat_bog_kgph"].astype(float).tolist(),
            "piston_bog_kgph": bog_agg["piston_bog_kgph"].astype(float).tolist(),
            "flash_bog_kgph": bog_agg["flash_bog_kgph"].astype(float).tolist(),
        },
        "run_history": [],
    }


def run_simulation(
    weather_data: Optional[Dict[str, object]] = None,
    unload_plan: Optional[List[float]] = None,
    params_override: Optional[Dict[str, object]] = None,
) -> Dict[str, object]:
    current_dir = os.path.dirname(os.path.abspath(__file__))
    if params_override is not None:
        debug_print_algorithm_params(params_override)

    global Params
    original_params = Params
    try:
        CompatParams.pending_algorithm_params = params_override
        CompatParams.pending_weather_data = weather_data
        CompatParams.pending_unload_plan = unload_plan
        Params = CompatParams
        result = run_engineering_simulation()
    finally:
        CompatParams.pending_algorithm_params = None
        CompatParams.pending_weather_data = None
        CompatParams.pending_unload_plan = None
        Params = original_params

    result["params"] = CompatParams(
        base_dir=current_dir,
        algorithm_params=params_override,
        weather_data=weather_data,
        unload_plan=unload_plan,
    )
    return result


@app.route("/health", methods=["GET"])
def health_check():
    return jsonify({"status": "ok", "message": "LNG simulation service is running"})


@app.route("/simulate", methods=["POST"])
def simulate():
    data = request.get_json(silent=True) or {}
    result = run_simulation(
        weather_data=data.get("weather_data"),
        unload_plan=data.get("unload_plan"),
        params_override=data.get("algorithmParams") or data.get("algorithm_params") or data.get("params"),
    )
    return jsonify(serialize_simulation_result(result))


if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5000)
