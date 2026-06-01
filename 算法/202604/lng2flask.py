# -*- coding: utf-8 -*-
"""
lng2flask.py
LNG调度系统Flask服务
依赖: numpy, pandas, openpyxl
"""
import os
import math
import copy
import json
import numpy as np
import pandas as pd
from datetime import datetime
import warnings
from flask import Flask, request, jsonify

warnings.filterwarnings("ignore")
np.random.seed(42)  # 固定随机种子，保证结果可复现
app = Flask(__name__)

OPTIMIZATION_PROFILES = {
    'speed': {'particles': 20, 'iters': 10},
    'balanced': {'particles': 40, 'iters': 20},
    'quality': {'particles': 60, 'iters': 40},
}


# -----------------------
# Params：LNG仿真参数类，类似MATLAB的Params结构体
# -----------------------
REQUIRED_ALGORITHM_PARAM_KEYS = [
    'T_Max', 'Delta_T', 'Total_Output_Target',
    'Tank_Pressure_Max', 'Tank_Pressure_Min', 'Tank_Level_Max', 'Tank_Level_Min',
    'Num_Tanks', 'Tank_Volume', 'S_Tan', 'Initial_Liquid',
    'Rho_LNG', 'Rho_SW', 'C_SW', 'DT_SW', 'L_LNG',
    'Num_LP_Pumps', 'Num_HP_Pumps', 'Num_SW_Big', 'Num_SW_Small', 'Num_ORV', 'Num_Compressors',
    'LP_PQ_Coeffs', 'LP_Flow_Min', 'LP_Flow_Max', 'LP_Power_Max',
    'HP_PQ_Coeffs', 'HP_Flow_Min', 'HP_Flow_Max', 'HP_Power_Max',
    'SW_Big_Flow', 'SW_Big_Power', 'SW_Small_Flow', 'SW_Small_Power',
    'Comp_Levels', 'Comp_Power_Levels',
    'LP_Target_Pressure', 'HP_Target_Pressure',
    # BOG相关参数（后端必须提供）
    'T_lng', 'U_insulation', 'pump_heat_fraction', 'flash_fraction',
    'rho_gas', 'k_atm_p', 'k_dp_dt'
]


class Params:
    def __init__(self, algorithm_params=None):
        # 批量从后端参数更新所有属性（类似前端 Object.assign）
        algorithm_params = require_algorithm_params(algorithm_params)
        print(f'[Params] 后端传入参数数量: {len(algorithm_params)}')
        for key, value in algorithm_params.items():
            print(f'[Params] {key} = {value}')
            setattr(self, key, value)
        # 电价和需求曲线（与MATLAB版本一致）
        self.Elec_Price = np.array([0.45] * 7 + [0.60] * 3 + [0.85] * 8 + [0.60] * 4 + [0.45] * 2)
        self.Demand = [956.0, 917.8, 975.1, 1051.6, 1147.2, 1434.0, 2294.4, 3441.6, 3824.0, 3250.4, 2485.6, 2390.0,
                       2581.2, 2447.3, 2332.6, 2256.1, 2485.6, 4206.4, 4800.7, 4588.8, 3824.0, 3059.2, 1912.0, 1338.4]

        # 别名字段映射
        self.Level_Max = self.Tank_Level_Max
        self.Level_Min = self.Tank_Level_Min


def require_algorithm_params(params_override):
    if not isinstance(params_override, dict):
        raise ValueError('Missing algorithmParams from backend request.')

    missing_keys = [key for key in REQUIRED_ALGORITHM_PARAM_KEYS if key not in params_override or params_override[key] is None]
    if missing_keys:
        missing_text = ', '.join(missing_keys)
        print(f'[simulate] Missing required algorithm params: {missing_text}')
        raise ValueError(f'Missing required algorithm params: {missing_text}')

    return params_override


# -----------------------
# BOG_Predict：BOG预测函数，机理模型+残差修正，对应MATLAB同名函数
# 输入：t_vec-时间向量，weather_data-天气数据，unload_plan-卸船计划，par-参数对象
# 输出：bog_pred-修正后BOG预测值，bog_mech-机理模型BOG值
# -----------------------
def BOG_Predict(t_vec, weather_data, unload_plan, par: Params):
    T = len(t_vec)  # 总时长
    bog_mech = np.zeros(T)  # 机理模型BOG数组
    bog_pred = np.zeros(T)  # 最终预测BOG数组

    # BOG参数从后端参数获取
    T_lng = par.T_lng
    U_insulation = par.U_insulation
    pump_heat_fraction = par.pump_heat_fraction
    pump_baseline_count = min(3, par.Num_LP_Pumps)
    flash_fraction = par.flash_fraction
    rho_gas = par.rho_gas
    k_atm_p = par.k_atm_p
    k_dp_dt = par.k_dp_dt

    # 计算储罐表面积
    A_base = max(par.S_Tan, 1.0)  # 单罐底面积
    r = math.sqrt(A_base / math.pi)  # 储罐半径
    h = max(par.Tank_Volume / A_base, 1.0)  # 储罐高度
    A_surface_single = 0.2 * A_base + 2 * math.pi * r * h  # 单罐总表面积

    # 计算大气压变化率
    Pvec = np.asarray(weather_data.get('Pressure', np.ones(T) * 101.3))
    dPdt = np.concatenate(([0.0], np.diff(Pvec)))  # 每小时压力变化值

    # 逐时刻计算机理BOG
    for i in range(T):
        Tamb = float(np.asarray(weather_data.get('Temp'))[i])  # 环境温度
        Patm = float(Pvec[i])  # 环境压力
        Qun = max(0.0, float(unload_plan[i]))  # 卸船流量

        # 因素1：静态热泄露（罐壁传热）
        deltaT = Tamb - T_lng  # 温差
        Qdot_W_single = U_insulation * A_surface_single * deltaT  # 单罐传热功率(W)
        Qdot_kJ_h_single = Qdot_W_single * 3600.0 / 1000.0  # 转换为kJ/h
        m_static_single = Qdot_kJ_h_single / par.L_LNG  # 单罐热泄露BOG
        m_static_total = max(m_static_single * par.Num_Tanks, 0.0)  # 全场总热泄露BOG

        # 因素2：泵送热输入产生的BOG
        P_pump_total_kW = pump_baseline_count * par.LP_Power_Max  # 泵总功率
        Q_pump_kJ_h = P_pump_total_kW * pump_heat_fraction * 3600.0  # 泵输入热量
        m_pump = Q_pump_kJ_h / par.L_LNG  # 泵送热产生BOG

        # 因素3：活塞效应（卸船气相置换）
        m_piston = Qun * rho_gas if Qun > 0 else 0.0

        # 因素4：卸船闪蒸产生的BOG
        m_flash = flash_fraction * Qun * par.Rho_LNG if Qun > 0 else 0.0

        # 因素5：大气压与压力变化率修正
        atm_factor = 1.0 + k_atm_p * (101.3 - Patm) + k_dp_dt * dPdt[i]
        atm_factor = max(0.7, min(1.3, atm_factor))  # 限制修正系数范围

        # 综合机理BOG
        bog_mech[i] = (m_static_total + m_pump + m_piston + m_flash) * atm_factor

    # 模拟LSTM残差修正（无模型时用正弦波动替代）
    amp = max(200.0, np.mean(bog_mech) * 0.05)  # 残差幅度
    hours = np.arange(T)
    residual = amp * np.sin(2.0 * np.pi * (hours / 24.0)) * (0.5 + 0.5 * np.random.rand(T))
    bog_pred = np.maximum(bog_mech + residual, 0.0)  # 保证BOG非负

    return bog_pred, bog_mech


# -----------------------
# select_seawater_pumps：海水泵优选函数，满足流量下功率最小，对应MATLAB同名函数
# 输入：required_flow-所需海水流量，par-参数对象
# 输出：最优大海水泵、小海水泵开启台数
# -----------------------
def select_seawater_pumps(required_flow, par: Params):
    best = (0, 0, float('inf'))  # 初始化最优解（大泵数，小泵数，最小功率）
    # 遍历所有海水泵组合
    for i in range(par.Num_SW_Big + 1):
        for j in range(par.Num_SW_Small + 1):
            flow = i * par.SW_Big_Flow + j * par.SW_Small_Flow  # 当前组合总流量
            if flow + 1e-9 >= required_flow:  # 满足流量需求
                power = i * par.SW_Big_Power + j * par.SW_Small_Power  # 当前组合功率
                if power < best[2]:  # 更新最优解
                    best = (i, j, power)
    return best[0], best[1]


# -----------------------
# calculate_ideal_pressures：计算泵理想出口压力，考虑管路损失
# 输入：sched-调度方案，par-参数对象
# 输出：低压泵、高压泵理想压力(MPa)
# -----------------------
def calculate_ideal_pressures(sched: dict, par: Params):
    # 计算低压泵理想压力
    if sched.get('n_LP', 0) > 0:
        q_lp = sched.get('LP_Flow_PerPump', 0.0)
        lp_pipe_loss = 0.05 * (q_lp / 3000.0) ** 2  # 管路损失
        ideal_lp = par.LP_Target_Pressure + lp_pipe_loss
        ideal_lp = float(np.clip(ideal_lp, 1.15, 1.25))  # 压力限幅
    else:
        ideal_lp = 0.0

    # 计算高压泵理想压力
    if sched.get('n_HP', 0) > 0:
        q_hp_total = sched.get('HP_Flow_Total', 0.0)
        hp_pipe_loss = 0.2 * (q_hp_total / 2000.0) ** 2  # 管路损失
        ideal_hp = par.HP_Target_Pressure + hp_pipe_loss
        ideal_hp = float(np.clip(ideal_hp, 9.0, 13.5))  # 压力限幅
    else:
        ideal_hp = 0.0

    return round(ideal_lp, 2), round(ideal_hp, 2)


# -----------------------
# decode_position：PSO粒子解码函数，将0-1向量转为设备调度指令，对应MATLAB同名函数
# 输入：x_vector-粒子向量，t_idx-时刻，par-参数对象
# 输出：sched-当前时刻完整调度方案
# -----------------------
def decode_position(x_vector, t_idx, par: Params):
    vec = np.asarray(x_vector).flatten()
    # 向量长度不足时用均值填充
    if vec.size < 7:
        x_mean = float(np.mean(vec))
        vec = np.array([x_mean] * 7)

    sched = {}
    # 1. 解码低压泵开启台数
    sched['n_LP'] = int(np.clip(round(vec[0] * par.Num_LP_Pumps), 1, par.Num_LP_Pumps))
    # 2. 解码高压泵开启台数
    sched['n_HP'] = int(np.clip(round(vec[1] * par.Num_HP_Pumps), 1, par.Num_HP_Pumps))

    # 计算高压泵总流量（基于日外输目标弹性分配）
    # base_hp_flow = par.Total_Output_Target / par.T_Max
    # x_elastic = float(np.mean(vec))
    # hp_flow_total = base_hp_flow * (0.8 + 0.4 * x_elastic)

    hp_flow_total = par.Demand[t_idx]

    # 3. 解码低压泵单台流量
    if sched['n_HP'] > 0:
        hp_per = hp_flow_total / sched['n_HP']
        hp_per = float(np.clip(hp_per, par.HP_Flow_Min, par.HP_Flow_Max))
        sched['HP_Flow_PerPump'] = hp_per
        sched['HP_Flow_Total'] = hp_per * sched['n_HP']
    else:
        sched['HP_Flow_PerPump'] = 0.0
        sched['HP_Flow_Total'] = 0.0

    # 计算低压泵流量需求（含裕量）
    required_lp_flow = sched['HP_Flow_Total'] + 200.0
    if sched['n_LP'] <= 0:
        sched['n_LP'] = 1

    # 调整低压泵台数并分配流量
    lp_per = required_lp_flow / sched['n_LP']
    if lp_per > par.LP_Flow_Max:
        sched['n_LP'] = int(math.ceil(required_lp_flow / par.LP_Flow_Max))
        sched['n_LP'] = max(1, min(sched['n_LP'], par.Num_LP_Pumps))
        lp_per = required_lp_flow / sched['n_LP']
    lp_per = float(np.clip(lp_per, par.LP_Flow_Min, par.LP_Flow_Max))
    sched['LP_Flow_PerPump'] = lp_per
    sched['LP_Flow_Total'] = lp_per * sched['n_LP']

    # 4. 解码海水泵开启台数
    sw_big_on = int(np.clip(round(vec[2] * par.Num_SW_Big), 0, par.Num_SW_Big))
    sw_small_on = int(np.clip(round(vec[3] * par.Num_SW_Small), 0, par.Num_SW_Small))
    # 5. 解码ORV开启台数
    orv_count = int(np.clip(round(vec[6] * par.Num_ORV), 1, par.Num_ORV))

    sched['SW_n_big'] = sw_big_on
    sched['SW_n_small'] = sw_small_on
    sched['ORV_Count'] = orv_count
    sched['SW_Flow_Total'] = sw_big_on * par.SW_Big_Flow + sw_small_on * par.SW_Small_Flow

    # 6. 解码两台压缩机档位
    idx1 = int(np.clip(round(vec[4] * 4), 0, 4))
    idx2 = int(np.clip(round(vec[5] * 4), 0, 4))
    sched['Comp1_Level'] = par.Comp_Levels[idx1]
    sched['Comp2_Level'] = par.Comp_Levels[idx2]
    sched['Comp1_Power'] = par.Comp_Power_Levels[idx1]
    sched['Comp2_Power'] = par.Comp_Power_Levels[idx2]

    # 计算海水泵流量需求（含裕量）
    heat_required = sched['HP_Flow_Total'] * par.Rho_LNG * par.L_LNG
    sw_flow_req = heat_required / (par.Rho_SW * par.C_SW * par.DT_SW + 1e-9)
    n_big, n_small = select_seawater_pumps(sw_flow_req, par)
    if n_big + n_small > 0:
        sched['SW_n_big'] = int(n_big)
        sched['SW_n_small'] = int(n_small)
        sched['SW_Flow_Total'] = n_big * par.SW_Big_Flow + n_small * par.SW_Small_Flow

    # 计算泵理想压力
    sched['Ideal_LP_Pressure'], sched['Ideal_HP_Pressure'] = calculate_ideal_pressures(sched, par)
    return sched


# -----------------------
# fitness：适应度函数，评估PSO粒子优劣
# 输入：X_matrix-粒子矩阵，bog_data-BOG数据，unload_plan-卸船计划，par-参数对象
# 输出：适应度值，总电费，惩罚项，调度方案列表
# -----------------------
def fitness(X_matrix, bog_data, unload_plan, par: Params):
    total_energy_cost = 0.0  # 总电费
    penalty_sum = 0.0  # 总惩罚值
    schedules = []  # 全时段调度方案
    current_level = par.Initial_Liquid  # 当前储罐液位
    outflows = []  # 外输流量记录
    # 惩罚系数定义
    penalty_per_full_comp = 50000.0
    penalty_imbalance_coeff = 5000.0

    # 遍历每个时刻
    for t in range(par.T_Max):
        x = X_matrix[t, :]
        sched = decode_position(x, t, par)

        # 计算低压泵功率
        a, b, c = par.LP_PQ_Coeffs
        q_lp = sched.get('LP_Flow_PerPump', 0.0)
        p_lp_per = a * q_lp ** 2 + b * q_lp + c
        power_lp = p_lp_per * sched.get('n_LP', 0)

        # 计算高压泵功率
        a2, b2, c2 = par.HP_PQ_Coeffs
        q_hp = sched.get('HP_Flow_PerPump', 0.0)
        p_hp_per = a2 * q_hp ** 2 + b2 * q_hp + c2
        power_hp = p_hp_per * sched.get('n_HP', 0)

        # 计算海水泵功率
        power_sw = sched.get('SW_n_big', 0) * par.SW_Big_Power + sched.get('SW_n_small', 0) * par.SW_Small_Power

        # 计算压缩机功率
        power_comp = sched.get('Comp1_Power', 0.0) + sched.get('Comp2_Power', 0.0)

        # 计算总功率
        hourly_power = power_lp + power_hp + power_sw + power_comp
        hourly_cost = hourly_power * par.Elec_Price[t]
        total_energy_cost += hourly_cost

        # 记录调度方案
        sched_hour = copy.deepcopy(sched)
        sched_hour['Hourly_Power'] = hourly_power
        sched_hour['Hourly_Cost'] = hourly_cost

        # 惩罚项1：压缩机满载
        if unload_plan[t] > 0 and (sched.get('Comp1_Level', 0.0) < 0.5 and sched.get('Comp2_Level', 0.0) < 0.5):
            penalty_sum += 50000.0

        # 惩罚项2：液位超限
        inflow = float(unload_plan[t])
        outflow = float(sched.get('HP_Flow_Total', 0.0))
        delta_h = (inflow - outflow) / (par.S_Tan * par.Num_Tanks + 1e-9)
        current_level += delta_h
        sched_hour['Tank_Level'] = current_level
        if current_level < par.Level_Min or current_level > par.Level_Max:
            penalty_sum += 1e6

        # 惩罚项3：BOG超限
        condense_cap = sched.get('LP_Flow_Total', 0.0) * par.Rho_LNG * 0.1
        if bog_data[t] > condense_cap + 2000.0:
            penalty_sum += 20000.0

        # 惩罚项4：ORV不足
        orv_load_kg = sched.get('HP_Flow_Total', 0.0) * par.Rho_LNG
        heat_duty = orv_load_kg * par.L_LNG
        sw_capacity = (sched.get('SW_n_big', 0) * par.SW_Big_Flow + sched.get('SW_n_small', 0) * par.SW_Small_Flow) * par.Rho_SW * par.C_SW * par.DT_SW
        if sw_capacity + 1e-9 < heat_duty:
            penalty_sum += 100000.0

        # 惩罚项5：压缩机不平衡
        comp1_level = sched.get('Comp1_Level', 0.0)
        comp2_level = sched.get('Comp2_Level', 0.0)
        if abs(comp1_level - 1.0) < 1e-9:
            penalty_sum += penalty_per_full_comp
        if abs(comp2_level - 1.0) < 1e-9:
            penalty_sum += penalty_per_full_comp
        imbalance = abs(comp1_level - comp2_level)
        penalty_sum += penalty_imbalance_coeff * imbalance

        # 记录调度方案
        schedules.append(sched_hour)
        outflows.append(outflow)

        # 惩罚项6：LNG外输量不达标
        if outflow < 0.95 * par.Demand[t] or outflow > 1.05 * par.Demand[t]:
            penalty_sum += 20000.0

    # 惩罚项7：总外输量不达标
    total_output = sum(outflows)
    if total_output < par.Total_Output_Target * 0.95 or total_output > par.Total_Output_Target * 1.05:
        penalty_sum += 500000.0 * abs(total_output - par.Total_Output_Target) / par.Total_Output_Target

    # 计算适应度
    fitness_val = total_energy_cost + penalty_sum
    return fitness_val, total_energy_cost, penalty_sum, schedules


# -----------------------
# pso_optimizer：PSO优化函数
# 输入：dim_per_step-每时刻维度，n_steps-总时长，n_particles-粒子数，max_iter-迭代次数，bog_data-BOG数据，unload_plan-卸船计划，par-参数对象
# 输出：最优粒子，调度方案列表，迭代历史，总电费
# -----------------------
def pso_optimizer(dim_per_step, n_steps, n_particles, max_iter, bog_data, unload_plan, par: Params):
    total_dim = dim_per_step * n_steps  # 总粒子维度
    # 初始化粒子位置和速度
    X = np.random.rand(n_particles, total_dim)
    V = 0.01 * (np.random.rand(n_particles, total_dim) - 0.5)

    # 初始化个体最优和全局最优
    pbest = X.copy()
    pbest_val = np.ones(n_particles) * float('inf')
    gbest = np.zeros(total_dim)
    gbest_val = float('inf')
    gbest_schedules = None
    history = []  # 迭代收敛历史

    # PSO基础参数
    c1, c2 = 1.49, 1.49  # 学习因子
    w_max, w_min = 0.9, 0.4  # 惯性权重上下限

    # 迭代优化
    for it in range(max_iter):
        w = w_max - (w_max - w_min) * (it / max_iter)  # 线性递减惯性权重

        # 遍历每个粒子计算适应度
        for i in range(n_particles):
            Xm = X[i, :].reshape((n_steps, dim_per_step))
            fit_val, cost, penalty, scheds = fitness(Xm, bog_data, unload_plan, par)

            # 更新个体最优
            if fit_val < pbest_val[i]:
                pbest_val[i] = fit_val
                pbest[i, :] = X[i, :].copy()

            # 更新全局最优
            if fit_val < gbest_val:
                gbest_val = fit_val
                gbest = X[i, :].copy()
                gbest_schedules = copy.deepcopy(scheds)

        # 更新粒子速度和位置
        r1 = np.random.rand(n_particles, total_dim)
        r2 = np.random.rand(n_particles, total_dim)
        V = w * V + c1 * r1 * (pbest - X) + c2 * r2 * (gbest - X)
        V = np.clip(V, -0.2, 0.2)  # 速度限幅
        X = np.clip(X + V, 0.0, 1.0)  # 位置限幅（0-1）

        history.append(gbest_val)
        # 打印迭代信息
        if (it + 1) % 10 == 0 or it == max_iter - 1:
            print(f"PSO iter {it + 1}/{max_iter} - best fitness {gbest_val:.2f}")

    # 计算最优方案总电费
    final_cost = 0.0
    if gbest_schedules is not None:
        final_cost = sum([s.get('Hourly_Cost', 0.0) for s in gbest_schedules])
    return gbest, gbest_schedules, history, final_cost


# -----------------------
# unitize_schedules：调度方案标准化函数
# 输入：scheds-全时段调度，par-参数对象
# 输出：标准化调度方案
# -----------------------
def unitize_schedules(scheds, par: Params):
    T = len(scheds)
    # 初始化标准化矩阵
    lp_mat = np.zeros((T, par.Num_LP_Pumps), dtype=int)
    hp_mat = np.zeros((T, par.Num_HP_Pumps), dtype=int)
    sw_big_mat = np.zeros((T, par.Num_SW_Big), dtype=int)
    sw_small_mat = np.zeros((T, par.Num_SW_Small), dtype=int)
    orv_mat = np.zeros((T, par.Num_ORV), dtype=int)
    comp_mat = np.zeros((T, 2), dtype=float)

    # 遍历每个时刻
    for t, s in enumerate(scheds):
        # 分配低压泵
        n_lp = int(s.get('n_LP', 0))
        if par.Num_LP_Pumps > 0 and n_lp > 0:
            start = t % par.Num_LP_Pumps
            for k in range(n_lp):
                idx = (start + k) % par.Num_LP_Pumps
                lp_mat[t, idx] = 1

        # 分配高压泵
        n_hp = int(s.get('n_HP', 0))
        if par.Num_HP_Pumps > 0 and n_hp > 0:
            start_hp = t % par.Num_HP_Pumps
            for k in range(n_hp):
                idx = (start_hp + k) % par.Num_HP_Pumps
                hp_mat[t, idx] = 1

        # 海水泵启停
        n_big = int(s.get('SW_n_big', 0))
        n_small = int(s.get('SW_n_small', 0))
        for i in range(min(n_big, par.Num_SW_Big)):
            sw_big_mat[t, i] = 1
        for j in range(min(n_small, par.Num_SW_Small)):
            sw_small_mat[t, j] = 1

        # ORV启停
        hp_flow = float(s.get('HP_Flow_Total', 0.0))
        n_orv = min(par.Num_ORV, int(math.ceil(hp_flow / 2000.0)) if hp_flow > 0 else 0)
        for k in range(n_orv):
            orv_mat[t, k] = 1

        # 压缩机档位
        comp_mat[t, 0] = float(s.get('Comp1_Level', 0.0))
        comp_mat[t, 1] = float(s.get('Comp2_Level', 0.0))

    return {'LP': lp_mat, 'HP': hp_mat, 'SW_Big': sw_big_mat, 'SW_Small': sw_small_mat, 'ORV': orv_mat,
            'Comp': comp_mat}


# -----------------------



# -----------------------
# run_simulation：主仿真函数，整合全流程调度、计算、可视化、输出
# 输入：save_dir-输出目录，run_pso-是否运行PSO，pso_particles-粒子数，pso_iters-迭代次数
# 输出：完整仿真结果
# -----------------------
def apply_params_override(par: Params, params_override):
    if not isinstance(params_override, dict):
        return

    for key, value in params_override.items():
        # 跳过内部固定参数，只允许修改设备参数
        if key in {'Elec_Price', 'Demand'}:
            continue

        if not hasattr(par, key):
            continue

        attr_value = getattr(par, key)
        if isinstance(value, list):
            if isinstance(attr_value, np.ndarray):
                setattr(par, key, np.asarray(value, dtype=float))
            else:
                setattr(par, key, copy.deepcopy(value))
        else:
            setattr(par, key, value)

    if hasattr(par, 'sync_alias_fields'):
        par.sync_alias_fields()


def run_simulation(save_dir=None, run_pso=False, pso_particles=30, pso_iters=60,
                   weather_data=None, unload_plan=None, params_override=None):
    if save_dir is not None:
        os.makedirs(save_dir, exist_ok=True)  # 创建输出目录
    par = Params(params_override)
    t_vec = np.arange(par.T_Max)  # 时间向量

    # 
    if weather_data is None:
        weather_data = {'Temp': 25.0 + 5.0 * np.sin(2.0 * np.pi * t_vec / 24.0),
                        'Pressure': np.ones(par.T_Max) * 101.3}
    else:
        weather_data = {
            'Temp': np.asarray(weather_data.get('Temp', 25.0 + 5.0 * np.sin(2.0 * np.pi * t_vec / 24.0))),
            'Pressure': np.asarray(weather_data.get('Pressure', np.ones(par.T_Max) * 101.3))
        }

    # 
    if unload_plan is None:
        unload_plan = np.zeros(par.T_Max)
        unload_plan[10:14] = 12000.0
        unload_plan[14:17] = 12000.0
    else:
        unload_plan = np.asarray(unload_plan, dtype=float)
        if unload_plan.size != par.T_Max:
            raise ValueError(f'unload_plan must have length {par.T_Max}')

    # BOG预测
    bog_pred, bog_mech = BOG_Predict(t_vec, weather_data, unload_plan, par)
    dim_per_step = 7  # 每时刻决策变量数

    # 运行PSO优化或默认调度
    best_scheds = None
    history = []
    if run_pso:
        best_x, best_scheds, history, final_cost = pso_optimizer(dim_per_step, par.T_Max, pso_particles, pso_iters,
                                                                 bog_pred, unload_plan, par)
    else:
        X = np.tile(np.linspace(0.2, 0.8, par.T_Max).reshape(-1, 1), (1, dim_per_step))
        _, _, _, best_scheds = fitness(X, bog_pred, unload_plan, par)
        history = []

    if best_scheds is None:
        best_scheds = []

    # 生成小时级调度报表
    Hour = np.arange(1, par.T_Max + 1)
    records = []
    for t in range(par.T_Max):
        s = best_scheds[t] if t < len(best_scheds) else {}
        rec = {
            'Hour': int(t),
            'Unload_m3h': float(unload_plan[t]),
            'BOG_pred_kgph': float(bog_pred[t]),
            'LP_Num': int(s.get('n_LP', 0)),
            'HP_Num': int(s.get('n_HP', 0)),
            'Actual_LNG_Output_m3h': float(s.get('HP_Flow_Total', 0.0)),
            'LP_Flow_perPump_m3h': float(s.get('LP_Flow_PerPump', 0.0)),
            'HP_Flow_perPump_m3h': float(s.get('HP_Flow_PerPump', 0.0)),
            'Ideal_LP_Pressure_MPa': float(s.get('Ideal_LP_Pressure', 0.0)),
            'Ideal_HP_Pressure_MPa': float(s.get('Ideal_HP_Pressure', 0.0)),
            'SW_Big': int(s.get('SW_n_big', 0)),
            'SW_Small': int(s.get('SW_n_small', 0)),
            'ORV_Count': int(s.get('ORV_Count', 0)),
            'Comp1_Level': float(s.get('Comp1_Level', 0.0)),
            'Comp2_Level': float(s.get('Comp2_Level', 0.0)),
            'Hourly_Power_kW': float(s.get('Hourly_Power', 0.0)),
            'Hourly_Cost_CNY': float(s.get('Hourly_Cost', 0.0)),
            'Tank_Level_m': float(s.get('Tank_Level', par.Initial_Liquid)),
            'Elec_Price': float(par.Elec_Price[t])
        }
        records.append(rec)
    df_hours = pd.DataFrame(records)

    # 生成设备启停标准化矩阵
    unitized = unitize_schedules(best_scheds, par)
    df_lp_on = pd.DataFrame(unitized['LP'], columns=[f'LP#{i + 1}' for i in range(par.Num_LP_Pumps)])
    df_hp_on = pd.DataFrame(unitized['HP'], columns=[f'HP#{i + 1}' for i in range(par.Num_HP_Pumps)])
    df_swbig_on = pd.DataFrame(unitized['SW_Big'], columns=[f'SWBig#{i + 1}' for i in range(par.Num_SW_Big)])
    df_swsmall_on = pd.DataFrame(unitized['SW_Small'], columns=[f'SWSmall#{i + 1}' for i in range(par.Num_SW_Small)])
    df_orv_on = pd.DataFrame(unitized['ORV'], columns=[f'ORV#{i + 1}' for i in range(par.Num_ORV)])
    df_comp_levels = pd.DataFrame(unitized['Comp'], columns=['Comp1_Level', 'Comp2_Level'])

    excel_path = None
    saved_images = {}

    summary = {
        'excel': excel_path,
        'images': saved_images,
        'timestamp': datetime.now().isoformat(),
        'estimated_total_electric_cost': float(df_hours['Hourly_Cost_CNY'].sum()),
        'run_pso': run_pso,
        'pso_particles': pso_particles,
        'pso_iters': pso_iters
    }

    return {
        'summary': summary,
        'params': par,
        'df_hours': df_hours,
        'unitized': unitized,
        'bog': {
            'hours': Hour.tolist(),
            'bog_mech_kgph': bog_mech.tolist(),
            'bog_pred_kgph': bog_pred.tolist()
        },
        'images': saved_images,
        'history': history
    }


def serialize_simulation_result(result):
    return {
        'summary': result['summary'],
        'hourly': json.loads(result['df_hours'].to_json(orient='records')),
        'unitized': {k: v.tolist() for k, v in result['unitized'].items()},
        'bog': result['bog'],
        'run_history': [float(x) for x in result.get('history', [])]
    }


@app.route('/health', methods=['GET'])
def health_check():
    return jsonify({'status': 'ok', 'message': 'LNG simulation service is running'})


@app.route('/simulate', methods=['POST'])
def simulate():
    data = request.get_json(silent=True) or {}
    optimization_mode = str(data.get('optimization_mode', 'balanced')).strip().lower()
    profile = OPTIMIZATION_PROFILES.get(optimization_mode, OPTIMIZATION_PROFILES['balanced'])
    run_pso = bool(data.get('run_pso', True))
    pso_particles = int(data.get('pso_particles', profile['particles']))
    pso_iters = int(data.get('pso_iters', profile['iters']))
    save_dir = data.get('save_dir')
    weather_data = data.get('weather_data')
    unload_plan = data.get('unload_plan')
    params_override = data.get('algorithmParams') or data.get('algorithm_params') or data.get('params')

    result = run_simulation(save_dir=save_dir, run_pso=run_pso, pso_particles=pso_particles,
                            pso_iters=pso_iters, weather_data=weather_data,
                            unload_plan=unload_plan, params_override=params_override)
    return jsonify(serialize_simulation_result(result))


if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)
