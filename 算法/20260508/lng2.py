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
import matplotlib.pyplot as plt
import matplotlib.colors as mcolors

warnings.filterwarnings("ignore")
plt.rcParams["font.sans-serif"] = ["Microsoft YaHei", "SimHei", "Arial Unicode MS", "DejaVu Sans"]
plt.rcParams["axes.unicode_minus"] = False


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
        self.start_time = datetime(2026, 5, 6, 0, 0)
        self.delta_minutes = 15
        self.delta_hours = self.delta_minutes / 60.0
        self.horizon_hours = 24
        self.n_steps = int(self.horizon_hours / self.delta_hours)
        self.timestamps = [self.start_time + timedelta(minutes=self.delta_minutes * i) for i in range(self.n_steps)]

        self.total_output_target_m3_day = 16000.0
        self.allowed_delivery_bias = 0.05
        self.demand_charge_price = 46.75
        self.line_alarm_kw = {"L1": 14000.0, "L2": 11600.0}

        self.rho_lng = 450.0
        self.rho_sw = 1025.0
        self.cp_sw = 4.0
        self.delta_t_sw = 10.0
        self.latent_heat_kjkg = 500.0
        self.bog_density_kgm3 = 0.6784
        self.t_lng = -162.0
        self.tank_daily_bor = 0.00045
        self.pipe_heat_area_m2 = 5454.0
        self.pipe_heat_u = 0.225
        self.flash_fraction = 0.0048
        self.pump_heat_fraction = 0.18
        self.recondenser_factor = 0.18
        self.pipe_bog_share = 0.35
        self.atm_ref_kpa = 101.3
        self.k_atm = 0.0015
        self.k_dpatm = 0.02

        self.lp_pq_coeffs = [-0.0005, 0.5, 50.0]
        self.hp_pq_coeffs = [0.002, 2.5, 800.0]
        self.orv_unit_capacity_m3h = 420.0

        self.start_penalty = {"HP": 260.0, "LP": 40.0, "SW": 220.0, "COMP": 180.0}
        self.recirc_penalty = {"HP": 3.5, "LP": 1.0}
        self.line_balance_weight = 0.06
        self.runtime_weight = 0.002
        self.switch_penalty = 30.0
        self.unload_comp_min_level = 0.25

        self.energy_price_hourly = self._build_energy_price_hourly()
        hourly_cycles = int(math.ceil(self.horizon_hours / 24.0))
        self.energy_price = np.repeat(np.tile(self.energy_price_hourly, hourly_cycles), int(60 / self.delta_minutes))[: self.n_steps]
        self.weather = self._build_weather_profile()
        self.sendout_profile = self._build_sendout_profile()
        self.unload_profile, self.unload_phase = self._build_unload_profile()

        self.tanks = self._build_tanks()
        self.devices = self._build_devices()
        self.devices_by_category = self._group_devices_by_category()
        self.device_index = {d.name: d for d in self.devices}
        self.total_output_target_horizon = float(np.sum(self.sendout_profile) * self.delta_hours)

    def _build_energy_price_hourly(self) -> np.ndarray:
        hourly = np.array([
            0.28165, 0.28165, 0.28165, 0.28165, 0.28165, 0.28165,
            0.28165, 0.28165, 0.49200, 0.49200, 0.49200, 0.49200,
            0.49200, 0.49200, 0.49200, 0.49200, 0.49200, 0.49200,
            0.49200, 0.70235, 0.70235, 0.70235, 0.70235, 0.28165,
        ], dtype=float)
        return hourly

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

    def _build_sendout_profile(self) -> np.ndarray:
        hourly_base = np.array([
            956.0, 917.8, 975.1, 1051.6, 1147.2, 1434.0,
            2294.4, 3441.6, 3824.0, 3250.4, 2485.6, 2390.0,
            2581.2, 2447.3, 2332.6, 2256.1, 2485.6, 4206.4,
            4800.7, 4588.8, 3824.0, 3059.2, 1912.0, 1338.4,
        ], dtype=float)
        hourly_base = hourly_base / hourly_base.sum() * self.total_output_target_m3_day
        horizon_hourly = np.tile(hourly_base, int(math.ceil(self.horizon_hours / 24.0)))[: self.horizon_hours]
        profile = np.repeat(horizon_hourly, int(60 / self.delta_minutes))
        return profile[: self.n_steps]

    def _build_unload_profile(self) -> Tuple[np.ndarray, List[str]]:
        profile = np.zeros(self.n_steps, dtype=float)
        phases = ["待机"] * self.n_steps
        schedule = [
            (datetime(2026, 5, 6, 11, 0), datetime(2026, 5, 6, 12, 30), 0.0, "引航靠泊"),
            (datetime(2026, 5, 6, 12, 30), datetime(2026, 5, 6, 14, 30), 0.0, "系统建立"),
            (datetime(2026, 5, 6, 14, 50), datetime(2026, 5, 6, 15, 50), 0.0, "CIQ与MSA联检"),
            (datetime(2026, 5, 6, 15, 50), datetime(2026, 5, 6, 16, 50), 0.0, "安全检查及卸前"),
            (datetime(2026, 5, 6, 16, 50), datetime(2026, 5, 6, 17, 20), 0.0, "吹扫置换"),
            (datetime(2026, 5, 6, 17, 20), datetime(2026, 5, 6, 17, 40), 0.0, "初始计量"),
            (datetime(2026, 5, 6, 17, 40), datetime(2026, 5, 6, 18, 0), 0.0, "热态ESD测试"),
            (datetime(2026, 5, 6, 18, 0), datetime(2026, 5, 6, 19, 30), 2000.0, "卸料臂预冷"),
            (datetime(2026, 5, 6, 19, 30), datetime(2026, 5, 6, 19, 50), 0.0, "冷态ESD测试"),
            (datetime(2026, 5, 6, 19, 50), datetime(2026, 5, 6, 20, 50), 6000.0, "卸料升速"),
            (datetime(2026, 5, 6, 20, 50), datetime(2026, 5, 7, 10, 30), 12000.0, "卸料全速"),
            (datetime(2026, 5, 7, 10, 30), datetime(2026, 5, 7, 11, 30), 4000.0, "卸料降速"),
            (datetime(2026, 5, 7, 11, 30), datetime(2026, 5, 7, 12, 20), 1200.0, "岸侧排凝"),
            (datetime(2026, 5, 7, 12, 20), datetime(2026, 5, 7, 12, 50), 600.0, "船侧排凝"),
            (datetime(2026, 5, 7, 12, 50), datetime(2026, 5, 7, 13, 10), 0.0, "吹扫置换"),
            (datetime(2026, 5, 7, 13, 10), datetime(2026, 5, 7, 13, 30), 0.0, "末次计量"),
            (datetime(2026, 5, 7, 13, 30), datetime(2026, 5, 7, 14, 30), 0.0, "拆臂"),
            (datetime(2026, 5, 7, 14, 30), datetime(2026, 5, 7, 15, 0), 0.0, "卸后会议"),
        ]
        for i, ts in enumerate(self.timestamps):
            for start, end, flow, phase in schedule:
                if start <= ts < end:
                    profile[i] = flow
                    phases[i] = phase
                    break
        return profile, phases

    def _build_tanks(self) -> List[Tank]:
        area一期 = math.pi * (80.0 / 2.0) ** 2
        area二期 = math.pi * (84.2 / 2.0) ** 2
        tanks = [
            Tank("TK-01", "一期", area一期, 2.04, 2.16, 35.311, 20.0),
            Tank("TK-02", "一期", area一期, 2.04, 2.16, 35.311, 20.0),
            Tank("TK-03", "一期", area一期, 2.04, 2.16, 35.311, 20.0),
            Tank("TK-04", "一期", area一期, 2.04, 2.16, 35.311, 20.0),
            Tank("TK-05", "二期", area二期, 1.20, 1.50, 38.30, 20.0),
            Tank("TK-06", "二期", area二期, 1.20, 1.50, 38.30, 20.0),
        ]
        return tanks

    def _build_devices(self) -> List[Device]:
        lp_layout = {
            "TK-01": [("A", "L1"), ("B", "L2"), ("C", "L1"), ("D", "L2")],
            "TK-02": [("A", "L1"), ("B", "L2"), ("C", "L2"), ("D", "L1")],
            "TK-03": [("A", "L1"), ("B", "L2"), ("C", "L1"), ("D", "L2")],
            "TK-04": [("A", "L1"), ("B", "L2"), ("C", "L1"), ("D", "L1")],
            "TK-05": [("A", "L1"), ("B", "L2"), ("C", "L2"), ("D", "L1")],
            "TK-06": [("A", "L1"), ("B", "L2"), ("C", "L2"), ("D", "L1")],
        }
        devices: List[Device] = []
        for tank_idx, tank_name in enumerate(lp_layout.keys(), start=1):
            phase = "一期" if tank_idx <= 4 else "二期"
            for suffix, line in lp_layout[tank_name]:
                device_id = f"0331-P-{tank_idx:02d}{suffix}"
                reactive = 127.0
                devices.append(Device(
                    name=f"罐内泵{device_id}",
                    category="LP",
                    line=line,
                    rated_power_kw=220.0,
                    reactive_kvar=reactive,
                    min_flow=180.0,
                    max_flow=430.0,
                    tank=tank_name,
                    phase=phase,
                ))

        hp_specs = [
            ("高压外输泵0330-P-01A", "L1", 2170.0, 1230.0, "一期"),
            ("高压外输泵0330-P-01B", "L2", 2170.0, 1230.0, "一期"),
            ("高压外输泵0330-P-01C", "L1", 2170.0, 1230.0, "一期"),
            ("高压外输泵0330-P-01D", "L2", 2170.0, 1230.0, "一期"),
            ("高压外输泵0330-P-01E", "L1", 2170.0, 1230.0, "一期"),
            ("高压外输泵0330-P-01F", "L1", 2170.0, 1230.0, "一期"),
            ("高压外输泵0330-P-01G", "L2", 2200.0, 1250.0, "二期"),
        ]
        for name, line, power, kvar, phase in hp_specs:
            devices.append(Device(
                name=name,
                category="HP",
                line=line,
                rated_power_kw=power,
                reactive_kvar=kvar,
                min_flow=200.0,
                max_flow=460.0 if not name.endswith("01G") else 470.0,
                phase=phase,
            ))

        sw_specs = [
            ("海水泵P-01A", "L1", 900.0, 650.0, 7300.0, "一期"),
            ("海水泵P-01B", "L1", 900.0, 650.0, 7300.0, "一期"),
            ("海水泵P-01C", "L2", 900.0, 650.0, 7300.0, "一期"),
            ("海水泵P-01D", "L2", 900.0, 650.0, 7300.0, "一期"),
            ("海水泵P-01E", "L1", 1800.0, 1350.0, 13400.0, "二期"),
            ("海水泵P-01F", "L2", 1800.0, 1350.0, 13400.0, "二期"),
        ]
        for name, line, power, kvar, flow, phase in sw_specs:
            devices.append(Device(
                name=name,
                category="SW",
                line=line,
                rated_power_kw=power,
                reactive_kvar=kvar,
                max_flow=flow,
                phase=phase,
            ))

        comp_specs = [
            ("BOG压缩机0330-C-01A", "L1", 1100.0, 940.0, 3500.0, "一期"),
            ("BOG压缩机0330-C-01B", "L2", 1100.0, 940.0, 3500.0, "一期"),
            ("BOG压缩机0330-C-01C", "L1", 1120.0, 1018.0, 3600.0, "二期"),
        ]
        for name, line, power, kvar, cap, phase in comp_specs:
            devices.append(Device(
                name=name,
                category="COMP",
                line=line,
                rated_power_kw=power,
                reactive_kvar=kvar,
                capacity_kgph=cap,
                phase=phase,
            ))

        for i in range(1, 8):
            devices.append(Device(
                name=f"ORV#{i}",
                category="ORV",
                line=None,
                rated_power_kw=0.0,
                max_flow=self.orv_unit_capacity_m3h,
            ))
        return devices

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
    levels = [0.0, 0.25, 0.50, 0.75, 1.0]
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
            power = level * dev.rated_power_kw
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


def save_csv_tables(tables: Dict[str, pd.DataFrame], save_dir: str) -> Dict[str, str]:
    csv_paths = {}
    for name, df in tables.items():
        path = os.path.join(save_dir, f"{name}.csv")
        df.to_csv(path, index=False, encoding="utf-8-sig")
        csv_paths[name] = path
    return csv_paths


def plot_profile(summary: pd.DataFrame, save_dir: str) -> str:
    path = os.path.join(save_dir, "plot_sendout_and_unload.png")
    x = np.arange(len(summary))
    fig, ax1 = plt.subplots(figsize=(16, 5))
    ax1.plot(x, summary["demand_target_m3h"], label="外输需求", color="#1f77b4", linewidth=2)
    ax1.plot(x, summary["actual_output_m3h"], label="实际外输", color="#ff7f0e", linewidth=2, linestyle="--")
    ax1.set_ylabel("外输量 (m3/h)")
    ax1.set_xlabel("15分钟步")
    ax1.grid(True, linestyle="--", alpha=0.3)
    ax2 = ax1.twinx()
    ax2.fill_between(x, summary["unload_inflow_m3h"], color="#2ca02c", alpha=0.25, label="卸船入站")
    ax2.set_ylabel("卸船量 (m3/h)")
    lines1, labels1 = ax1.get_legend_handles_labels()
    lines2, labels2 = ax2.get_legend_handles_labels()
    ax1.legend(lines1 + lines2, labels1 + labels2, loc="upper left")
    plt.title("外输需求/实际外输/卸船曲线")
    plt.tight_layout()
    plt.savefig(path, dpi=250)
    plt.close()
    return path


def plot_bog(bog_df: pd.DataFrame, save_dir: str) -> str:
    path = os.path.join(save_dir, "plot_bog_components.png")
    x = np.arange(len(bog_df))
    plt.figure(figsize=(16, 5))
    plt.stackplot(
        x,
        bog_df["static_bor_kgph"],
        bog_df["wall_heat_bog_kgph"],
        bog_df["pump_heat_bog_kgph"],
        bog_df["piston_bog_kgph"],
        bog_df["flash_bog_kgph"],
        labels=["静态蒸发", "壁面漏热", "泵热输入", "活塞效应", "闪蒸"],
        alpha=0.75,
    )
    plt.plot(x, bog_df["bog_pred_kgph"], color="black", linewidth=2.0, label="BOG预测")
    plt.xlabel("15分钟步")
    plt.ylabel("BOG (kg/h)")
    plt.title("BOG五机理分解与预测结果")
    plt.grid(True, linestyle="--", alpha=0.3)
    plt.legend(ncol=3)
    plt.tight_layout()
    plt.savefig(path, dpi=250)
    plt.close()
    return path


def plot_tank_levels(tank_df: pd.DataFrame, par: Params, save_dir: str) -> str:
    path = os.path.join(save_dir, "plot_tank_levels.png")
    pivot = tank_df.pivot(index="timestamp", columns="tank", values="level_m")
    plt.figure(figsize=(16, 6))
    for col in pivot.columns:
        plt.plot(pivot.index, pivot[col], linewidth=2, label=col)
    for tank in par.tanks:
        plt.axhline(tank.level_max_m, color="#cccccc", linestyle="--", linewidth=0.8)
        plt.axhline(tank.level_min_m, color="#eeeeee", linestyle=":", linewidth=0.8)
    plt.ylabel("液位 (m)")
    plt.xlabel("时间")
    plt.title("6座储罐液位变化")
    plt.grid(True, linestyle="--", alpha=0.3)
    plt.legend(ncol=3)
    plt.tight_layout()
    plt.savefig(path, dpi=250)
    plt.close()
    return path


def plot_line_power(summary: pd.DataFrame, save_dir: str) -> str:
    path = os.path.join(save_dir, "plot_line_power_and_peak.png")
    x = np.arange(len(summary))
    plt.figure(figsize=(16, 5))
    plt.plot(x, summary["line1_kw"], label="线路1负荷", color="#d62728", linewidth=2)
    plt.plot(x, summary["line2_kw"], label="线路2负荷", color="#9467bd", linewidth=2)
    plt.axhline(summary["line1_peak_kw"].max(), color="#d62728", linestyle="--", alpha=0.7, label="线路1最大需量")
    plt.axhline(summary["line2_peak_kw"].max(), color="#9467bd", linestyle="--", alpha=0.7, label="线路2最大需量")
    plt.xlabel("15分钟步")
    plt.ylabel("有功功率 (kW)")
    plt.title("双供电线路负荷与最大需量")
    plt.grid(True, linestyle="--", alpha=0.3)
    plt.legend(ncol=2)
    plt.tight_layout()
    plt.savefig(path, dpi=250)
    plt.close()
    return path


def plot_cost(summary: pd.DataFrame, par: Params, save_dir: str) -> str:
    path = os.path.join(save_dir, "plot_cost_breakdown.png")
    energy = summary["step_energy_cost_cny"].sum()
    demand_charge = summary["line1_peak_kw"].max() * par.demand_charge_price + summary["line2_peak_kw"].max() * par.demand_charge_price
    plt.figure(figsize=(8, 5))
    plt.bar(["电度电费", "基本电费"], [energy, demand_charge], color=["#1f77b4", "#ff7f0e"])
    plt.ylabel("费用 (元)")
    plt.title("电费构成")
    plt.tight_layout()
    plt.savefig(path, dpi=250)
    plt.close()
    return path


def plot_heatmap(matrix: pd.DataFrame, title: str, save_path: str, is_level: bool = False) -> str:
    values = matrix.iloc[:, 1:].to_numpy(dtype=float).T
    labels = list(matrix.columns[1:])
    fig, ax = plt.subplots(figsize=(max(12, matrix.shape[0] * 0.12), max(4, len(labels) * 0.35)))
    cmap = "viridis" if is_level else mcolors.ListedColormap(["#ffffff", "#1f77b4"])
    vmin = 0.0
    vmax = 1.0 if not is_level else max(1.0, np.nanmax(values))
    im = ax.imshow(values, aspect="auto", interpolation="nearest", cmap=cmap, vmin=vmin, vmax=vmax)
    ax.set_yticks(np.arange(len(labels)))
    ax.set_yticklabels(labels, fontsize=9)
    ax.set_xticks(np.arange(matrix.shape[0]))
    ax.set_xticklabels([pd.to_datetime(v).strftime("%m-%d\n%H:%M") for v in matrix.iloc[:, 0]], fontsize=7)
    ax.set_title(title)
    ax.set_xlabel("时间")
    ax.set_ylabel("设备")
    for x in np.arange(-0.5, matrix.shape[0], 1):
        ax.axvline(x, color="#efefef", linewidth=0.4)
    plt.colorbar(im, ax=ax, fraction=0.015, pad=0.01)
    plt.tight_layout()
    plt.savefig(save_path, dpi=250)
    plt.close()
    return save_path


def save_plots(tables: Dict[str, pd.DataFrame], par: Params, save_dir: str) -> Dict[str, str]:
    plots = {}
    plots["sendout_unload"] = plot_profile(tables["summary_timeseries"], save_dir)
    plots["bog"] = plot_bog(tables["bog_components"], save_dir)
    plots["tank_levels"] = plot_tank_levels(tables["tank_timeseries"], par, save_dir)
    plots["line_power"] = plot_line_power(tables["summary_timeseries"], save_dir)
    plots["cost_breakdown"] = plot_cost(tables["summary_timeseries"], par, save_dir)
    plots["lp_heatmap"] = plot_heatmap(tables["lp_onoff"], "低压泵启停热力图", os.path.join(save_dir, "plot_lp_heatmap.png"))
    plots["hp_heatmap"] = plot_heatmap(tables["hp_onoff"], "高压泵启停热力图", os.path.join(save_dir, "plot_hp_heatmap.png"))
    plots["sw_heatmap"] = plot_heatmap(tables["sw_onoff"], "海水泵启停热力图", os.path.join(save_dir, "plot_sw_heatmap.png"))
    plots["orv_heatmap"] = plot_heatmap(tables["orv_onoff"], "ORV启停热力图", os.path.join(save_dir, "plot_orv_heatmap.png"))
    plots["comp_heatmap"] = plot_heatmap(tables["compressor_levels"], "BOG压缩机负荷热力图", os.path.join(save_dir, "plot_compressor_heatmap.png"), is_level=True)
    return plots


def run_simulation(save_dir: str = "engineering_output") -> Dict[str, object]:
    save_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), save_dir)
    os.makedirs(save_dir, exist_ok=True)
    par = Params()

    inventory_profile = build_projected_inventory_profile(par)
    bog_pass1 = predict_bog_profile(par, inventory_profile)
    results_pass1 = dispatch_terminal(par, bog_pass1)
    lp_power_trace = results_pass1["device"].query("category == 'LP'").groupby("timestamp")["power_kw"].sum().reindex(par.timestamps, fill_value=0.0).to_numpy()
    total_inventory_trace = results_pass1["tank"].groupby("timestamp")["volume_m3"].sum().reindex(par.timestamps, fill_value=0.0).to_numpy()
    bog_final = predict_bog_profile(par, total_inventory_trace, lp_power_trace_kw=lp_power_trace)
    results = dispatch_terminal(par, bog_final)
    tables = build_output_tables(results, bog_final, par)
    csv_paths = save_csv_tables(tables, save_dir)
    plot_paths = save_plots(tables, par, save_dir)

    summary = tables["summary_timeseries"]
    run_summary = {
        "save_dir": save_dir,
        "csv_files": csv_paths,
        "png_files": plot_paths,
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
    summary_path = os.path.join(save_dir, "run_summary.json")
    with open(summary_path, "w", encoding="utf-8") as f:
        json.dump(run_summary, f, ensure_ascii=False, indent=2)

    return {
        "params": par,
        "tables": tables,
        "csv_files": csv_paths,
        "png_files": plot_paths,
        "summary": run_summary,
    }


if __name__ == "__main__":
    result = run_simulation()
    print("工程版调度计算完成。输出目录：", result["summary"]["save_dir"])
