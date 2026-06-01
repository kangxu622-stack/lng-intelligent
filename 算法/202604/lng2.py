# -*- coding: utf-8 -*-
"""
lng_dispatch_full_separate_plots.py
完整脚本（与之前版本功能一致），但所有结果单独保存为图片（每个图单独文件）。
运行前确保安装依赖: numpy, pandas, matplotlib, openpyxl
"""
import os
import math
import copy
import numpy as np
import pandas as pd
import matplotlib.pyplot as plt
from datetime import datetime
import warnings

warnings.filterwarnings("ignore")
np.random.seed(42)  # 固定随机种子，保证结果可复现


# -----------------------
# Params：LNG接收站全局参数配置类，对应MATLAB的Params类
# -----------------------
class Params:
    def __init__(self):
        # 1. 时间与仿真参数
        self.T_Max = 24  # 调度周期 24小时
        self.Delta_T = 1  # 时间步长 (小时)

        # 2. 生产目标与约束
        self.Total_Output_Target = 60000.0  # 日外输目标 (m3/day)
        self.Tank_Pressure_Max = 24.5  # 储罐压力上限 (kPaG)
        self.Tank_Pressure_Min = 10.0  # 储罐压力下限 (kPaG)
        self.Tank_Level_Max = 35.311  # 高高液位联锁值 (m)
        self.Tank_Level_Min = 3.0  # 低低液位保护值 (m)
        self.Level_Max = self.Tank_Level_Max  # 液位上限别名
        self.Level_Min = self.Tank_Level_Min  # 液位下限别名

        # 储罐几何与初始状态
        self.Num_Tanks = 4  # 储罐数量
        self.Tank_Volume = 160000.0  # 单罐有效容积 (m3)
        self.S_Tan = 5000.0  # 单罐等效截面积 (m2)
        self.Initial_Liquid = 20.0  # 初始液位 (m)

        # 3. 物性参数
        self.Rho_LNG = 450.0  # LNG密度 (kg/m3)
        self.Rho_SW = 1025.0  # 海水密度 (kg/m3)
        self.C_SW = 4.0  # 海水比热 (kJ/kg.K)
        self.DT_SW = 10.0  # 海水温升允许值 (K)
        self.L_LNG = 510.0  # LNG汽化潜热 (kJ/kg)

        # 4. 设备数量
        self.Num_LP_Pumps = 16  # 低压泵数量
        self.Num_HP_Pumps = 8  # 高压泵数量
        self.Num_SW_Big = 2  # 大海水泵数量
        self.Num_SW_Small = 4  # 小海水泵数量
        self.Num_ORV = 7  # ORV数量
        self.Num_Compressors = 2  # 压缩机数量

        # 5. 设备性能曲线/额定参数
        # 低压泵P-Q曲线系数 P[kW] = a*Q^2 + b*Q + c
        self.LP_PQ_Coeffs = [-0.0005, 0.5, 50.0]
        self.LP_Flow_Min = 180.0  # 单台低压泵最小流量 (m3/h)
        self.LP_Flow_Max = 430.0  # 单台低压泵最大流量 (m3/h)
        self.LP_Power_Max = 220.0  # 单台低压泵额定功率 (kW)

        # 高压泵P-Q曲线系数
        self.HP_PQ_Coeffs = [0.002, 2.5, 800.0]
        self.HP_Flow_Min = 260.0  # 单台高压泵最小流量 (m3/h)
        self.HP_Flow_Max = 460.0  # 单台高压泵最大流量 (m3/h)
        self.HP_Power_Max = 2000.0  # 单台高压泵额定功率 (kW)

        # 海水泵额定参数
        self.SW_Big_Flow = 13400.0  # 大海水泵单台额定流量 (m3/h)
        self.SW_Big_Power = 1328.7  # 大海水泵单台额定功率 (kW)
        self.SW_Small_Flow = 7300.0  # 小海水泵单台额定流量 (m3/h)
        self.SW_Small_Power = 900.0  # 小海水泵单台额定功率 (kW)

        # 压缩机档位与对应功率
        self.Comp_Levels = [0.0, 0.25, 0.5, 0.75, 1.0]  # 压缩机档位百分比
        self.Comp_Power_Levels = [0.0, 250.0, 500.0, 750.0, 1000.0]  # 各档位功率(kW)

        # 6. 分时电价 (24小时) 单位：元/kWh
        self.Elec_Price = np.array([0.45] * 7 + [0.60] * 3 + [0.85] * 8 + [0.60] * 4 + [0.45] * 2)

        # 泵目标压力
        self.LP_Target_Pressure = 1.20  # 低压泵目标压力(MPa)
        self.HP_Target_Pressure = 12.00  # 高压泵目标压力(MPa)

        self.Demand = [956.0, 917.8, 975.1, 1051.6, 1147.2, 1434.0, 2294.4, 3441.6, 3824.0, 3250.4, 2485.6, 2390.0,
                       2581.2, 2447.3, 2332.6, 2256.1, 2485.6, 4206.4, 4800.7, 4588.8, 3824.0, 3059.2, 1912.0, 1338.4]


# -----------------------
# BOG_Predict：BOG预测函数，机理模型+残差修正，对应MATLAB同名函数
# 输入：t_vec-时间向量，weather_data-天气数据，unload_plan-卸船计划，par-参数对象
# 输出：bog_pred-修正后BOG预测值，bog_mech-机理模型BOG值
# -----------------------
def BOG_Predict(t_vec, weather_data, unload_plan, par: Params):
    T = len(t_vec)  # 总时长
    bog_mech = np.zeros(T)  # 机理模型BOG数组
    bog_pred = np.zeros(T)  # 最终预测BOG数组

    # 工程常数定义
    T_lng = -162.0  # 罐内LNG温度(°C)
    U_insulation = 0.02  # 总体传热系数 W/(m^2·K)
    pump_heat_fraction = 0.2  # 泵电功转入流体的热比例
    pump_baseline_count = min(3, par.Num_LP_Pumps)  # 基准运行泵数量
    flash_fraction = 0.005  # 卸船闪蒸质量占比
    rho_gas = 20  # 卸船气相近似密度 kg/m3
    k_atm_p = 0.001  # 大气压修正系数
    k_dp_dt = 0.01  # 压力变化率修正系数

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
    #x_elastic = float(np.mean(vec))
    #hp_flow_total = base_hp_flow * (0.8 + 0.4 * x_elastic)

    hp_flow_total = par.Demand[t_idx]

    # 分配高压泵单台流量
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

    # 3. 解码海水泵开启台数
    sw_big_on = int(np.clip(round(vec[2] * par.Num_SW_Big), 0, par.Num_SW_Big))
    sw_small_on = int(np.clip(round(vec[3] * par.Num_SW_Small), 0, par.Num_SW_Small))
    # 4. 解码ORV开启台数
    orv_count = int(np.clip(round(vec[6] * par.Num_ORV), 1, par.Num_ORV))

    sched['SW_n_big'] = sw_big_on
    sched['SW_n_small'] = sw_small_on
    sched['ORV_Count'] = orv_count
    sched['SW_Flow_Total'] = sw_big_on * par.SW_Big_Flow + sw_small_on * par.SW_Small_Flow

    # 5. 解码两台压缩机档位
    idx1 = int(np.clip(round(vec[4] * 4), 0, 4))
    idx2 = int(np.clip(round(vec[5] * 4), 0, 4))
    sched['Comp1_Level'] = par.Comp_Levels[idx1]
    sched['Comp2_Level'] = par.Comp_Levels[idx2]
    sched['Comp1_Power'] = par.Comp_Power_Levels[idx1]
    sched['Comp2_Power'] = par.Comp_Power_Levels[idx2]

    # 基于热负荷优选海水泵组合
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
# fitness：适应度函数，电费最小化+约束惩罚，对应MATLAB同名函数
# 输入：X_matrix-粒子矩阵，bog_data-BOG数据，unload_plan-卸船计划，par-参数对象
# 输出：适应度值，总电费，总惩罚，全时段调度方案
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

    # 逐时刻计算能耗与约束
    for t in range(par.T_Max):
        x = X_matrix[t, :]
        sched = decode_position(x, t, par)

        # 计算低压泵能耗
        a, b, c = par.LP_PQ_Coeffs
        q_lp = sched.get('LP_Flow_PerPump', 0.0)
        p_lp_per = a * q_lp ** 2 + b * q_lp + c
        power_lp = p_lp_per * sched.get('n_LP', 0)

        # 计算高压泵能耗
        a2, b2, c2 = par.HP_PQ_Coeffs
        q_hp = sched.get('HP_Flow_PerPump', 0.0)
        p_hp_per = a2 * q_hp ** 2 + b2 * q_hp + c2
        power_hp = p_hp_per * sched.get('n_HP', 0)

        # 计算海水泵能耗
        power_sw = sched.get('SW_n_big', 0) * par.SW_Big_Power + sched.get('SW_n_small', 0) * par.SW_Small_Power

        # 计算压缩机能耗
        power_comp = sched.get('Comp1_Power', 0.0) + sched.get('Comp2_Power', 0.0)

        # 计算小时总功率与电费
        hourly_power = power_lp + power_hp + power_sw + power_comp
        hourly_cost = hourly_power * par.Elec_Price[t]
        total_energy_cost += hourly_cost

        # 保存当前时刻调度信息
        sched_hour = copy.deepcopy(sched)
        sched_hour['Hourly_Power'] = hourly_power
        sched_hour['Hourly_Cost'] = hourly_cost

        # 约束1：卸船期间压缩机运行约束
        if unload_plan[t] > 0 and (sched.get('Comp1_Level', 0.0) < 0.5 and sched.get('Comp2_Level', 0.0) < 0.5):
            penalty_sum += 50000.0

        # 约束2：储罐液位约束（质量守恒）
        inflow = float(unload_plan[t])
        outflow = float(sched.get('HP_Flow_Total', 0.0))
        delta_h = (inflow - outflow) / (par.S_Tan * par.Num_Tanks + 1e-9)
        current_level += delta_h
        sched_hour['Tank_Level'] = current_level
        if current_level < par.Level_Min or current_level > par.Level_Max:
            penalty_sum += 1e6

        # 约束3：BOG再冷凝能力约束
        condense_cap = sched.get('LP_Flow_Total', 0.0) * par.Rho_LNG * 0.1
        if bog_data[t] > condense_cap + 2000.0:
            penalty_sum += 20000.0

        # 约束4：ORV热平衡约束
        orv_load_kg = sched.get('HP_Flow_Total', 0.0) * par.Rho_LNG
        heat_duty = orv_load_kg * par.L_LNG
        sw_capacity = (sched.get('SW_n_big', 0) * par.SW_Big_Flow + sched.get('SW_n_small', 0) * par.SW_Small_Flow) * par.Rho_SW * par.C_SW * par.DT_SW
        if sw_capacity + 1e-9 < heat_duty:
            penalty_sum += 100000.0

        # 约束5：压缩机满负荷与均衡约束
        comp1_level = sched.get('Comp1_Level', 0.0)
        comp2_level = sched.get('Comp2_Level', 0.0)
        if abs(comp1_level - 1.0) < 1e-9:
            penalty_sum += penalty_per_full_comp
        if abs(comp2_level - 1.0) < 1e-9:
            penalty_sum += penalty_per_full_comp
        imbalance = abs(comp1_level - comp2_level)
        penalty_sum += penalty_imbalance_coeff * imbalance

        schedules.append(sched_hour)
        outflows.append(outflow)

        # 添加约束：各小时的外输量约束
        if outflow < 0.95 * par.Demand[t] or outflow > 1.05 * par.Demand[t]:
            penalty_sum += 20000.0


    # 约束6：日外输总量约束
    total_output = sum(outflows)
    if total_output < par.Total_Output_Target * 0.95 or total_output > par.Total_Output_Target * 1.05:
        penalty_sum += 500000.0 * abs(total_output - par.Total_Output_Target) / par.Total_Output_Target

    # 总适应度=电费+惩罚
    fitness_val = total_energy_cost + penalty_sum
    return fitness_val, total_energy_cost, penalty_sum, schedules


# -----------------------
# pso_optimizer：粒子群优化算法，对应MATLAB的pso函数
# 输入：dim_per_step-每时刻维度，n_steps-总时长，n_particles-粒子数，max_iter-迭代次数，bog_data-BOG数据，unload_plan-卸船计划，par-参数对象
# 输出：最优粒子，最优调度，迭代历史，总电费
# -----------------------
def pso_optimizer(dim_per_step, n_steps, n_particles, max_iter, bog_data, unload_plan, par: Params):
    total_dim = dim_per_step * n_steps  # 总粒子维度
    # 初始化粒子位置与速度
    X = np.random.rand(n_particles, total_dim)
    V = 0.01 * (np.random.rand(n_particles, total_dim) - 0.5)

    # 初始化个体最优与全局最优
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

        # 更新粒子速度与位置
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
# unitize_schedules：调度方案标准化，生成设备启停矩阵
# 输入：scheds-全时段调度，par-参数对象
# 输出：各设备启停/档位矩阵
# -----------------------
def unitize_schedules(scheds, par: Params):
    T = len(scheds)
    # 初始化设备启停矩阵
    lp_mat = np.zeros((T, par.Num_LP_Pumps), dtype=int)
    hp_mat = np.zeros((T, par.Num_HP_Pumps), dtype=int)
    sw_big_mat = np.zeros((T, par.Num_SW_Big), dtype=int)
    sw_small_mat = np.zeros((T, par.Num_SW_Small), dtype=int)
    orv_mat = np.zeros((T, par.Num_ORV), dtype=int)
    comp_mat = np.zeros((T, 2), dtype=float)

    # 逐时刻生成启停矩阵
    for t, s in enumerate(scheds):
        # 低压泵启停（循环分配）
        n_lp = int(s.get('n_LP', 0))
        if par.Num_LP_Pumps > 0 and n_lp > 0:
            start = t % par.Num_LP_Pumps
            for k in range(n_lp):
                idx = (start + k) % par.Num_LP_Pumps
                lp_mat[t, idx] = 1

        # 高压泵启停（循环分配）
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

        # ORV启停（按流量需求分配）
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
# 可视化函数：生成并保存各维度图表，用于结果展示
# -----------------------
def plot_and_save_bog(bog_mech, bog_pred, t_vec, save_dir):
    """绘制BOG预测对比图并保存"""
    path = os.path.join(save_dir, 'plot_bog_pred.png')
    plt.figure(figsize=(10, 4))
    plt.plot(t_vec, bog_mech, '--', linewidth=1.5, label='BOG mech (kg/h)')
    plt.plot(t_vec, bog_pred, '-', linewidth=2, label='BOG pred (kg/h)')
    plt.title('BOG: Mechanistic vs Predicted')
    plt.xlabel('Hour')
    plt.ylabel('BOG (kg/h)')
    plt.grid(True, linestyle='--')
    plt.legend()
    plt.tight_layout()
    plt.savefig(path, dpi=200)
    plt.close()
    return path


def plot_and_save_comp_levels(comp_levels, save_dir):
    """绘制压缩机档位变化图（增加横向淡灰色参考线）"""
    path = os.path.join(save_dir, 'plot_comp_levels.png')
    T = comp_levels.shape[0]
    hours = np.arange(T)

    fig, (ax1, ax2) = plt.subplots(2, 1, figsize=(12, 6), sharex=True)
    fig.suptitle('Compressor Operating Levels', fontsize=14, fontweight='bold', y=0.96)

    colors = ['#2E86AB', '#A23B72']

    for i, ax in enumerate([ax1, ax2]):
        # width=1.0 实现无间隙；zorder=3 确保柱状图在网格线上方
        ax.bar(hours, comp_levels[:, i], width=1.0, color=colors[i],
               edgecolor='white', linewidth=0.5, zorder=3)

        ax.set_title(f'Compressor {i + 1}', fontsize=12, loc='left', pad=8)
        ax.set_ylabel('Level', fontsize=11)

        # Y轴设置
        y_ticks = [0.0, 0.25, 0.5, 0.75, 1.0]
        ax.set_yticks(y_ticks)
        ax.set_ylim(0, 1.05)

        # --- 新增：横向淡灰色线 ---
        # 使用 grid 函数只开启 y 轴方向的网格线
        ax.grid(axis='y', color='#E0E0E0', linestyle='-', linewidth=0.8, zorder=0)

        # 隐藏多余边框
        ax.spines['top'].set_visible(False)
        ax.spines['right'].set_visible(False)
        ax.spines['left'].set_visible(False)  # 隐藏左边框，靠网格线引导
        ax.spines['bottom'].set_color('#CCCCCC')

        # --- 纵向小时分割线（保持参考图风格） ---
        for x in np.arange(-0.5, 24, 1):
            ax.axvline(x=x, color='#EEEEEE', linestyle='--', linewidth=0.8, zorder=1)

    # 底部 X 轴设置
    ax2.set_xlabel('Hour', fontsize=11)
    ax2.set_xticks(np.arange(0, 24, 1))
    ax2.set_xlim(-0.5, 23.5)

    # 移除刻度小触角
    ax1.tick_params(axis='both', which='both', length=0)
    ax2.tick_params(axis='both', which='both', length=0)

    plt.tight_layout(rect=[0, 0.03, 1, 0.95])
    plt.savefig(path, dpi=300, bbox_inches='tight')
    plt.close()
    return path


def plot_and_save_pressure(df_hours, save_dir):
    """绘制泵理想压力图并保存"""
    path_lp = os.path.join(save_dir, 'plot_ideal_lp_pressure.png')
    path_hp = os.path.join(save_dir, 'plot_ideal_hp_pressure.png')
    hours = df_hours['Hour'].values

    # 低压泵压力
    plt.figure(figsize=(10, 4))
    plt.plot(hours, df_hours['Ideal_LP_Pressure_MPa'], '-s', label='LP Ideal Pressure (MPa)')
    plt.title('LP Ideal Pressure')
    plt.xlabel('Hour')
    plt.ylabel('Pressure (MPa)')
    plt.grid(True)
    plt.tight_layout()
    plt.savefig(path_lp, dpi=200)
    plt.close()

    # 高压泵压力
    plt.figure(figsize=(10, 4))
    plt.plot(hours, df_hours['Ideal_HP_Pressure_MPa'], '-^', label='HP Ideal Pressure (MPa)')
    plt.title('HP Ideal Pressure')
    plt.xlabel('Hour')
    plt.ylabel('Pressure (MPa)')
    plt.grid(True)
    plt.tight_layout()
    plt.savefig(path_hp, dpi=200)
    plt.close()
    return path_lp, path_hp


def plot_and_save_tank_level(df_hours, save_dir):
    """绘制储罐液位变化图并保存"""
    path = os.path.join(save_dir, 'plot_tank_level.png')
    hours = df_hours['Hour'].values
    plt.figure(figsize=(10, 4))
    plt.plot(hours, df_hours['Tank_Level_m'], '-o', linewidth=2, label='Tank level (m)')
    plt.axhline(y=Params().Level_Max, color='r', linestyle='--', label='High-High Level')
    plt.axhline(y=Params().Level_Min, color='k', linestyle='--', label='Low-Low Level')
    plt.title('Tank Level (m)')
    plt.xlabel('Hour')
    plt.ylabel('Level (m)')
    plt.grid(True)
    plt.legend()
    plt.tight_layout()
    plt.savefig(path, dpi=200)
    plt.close()
    return path


def plot_and_save_price_and_hpcount(df_hours, par, save_dir):
    """绘制高压泵台数与电价对比图并保存"""
    path = os.path.join(save_dir, 'plot_hpcount_vs_price.png')
    hours = df_hours['Hour'].values
    plt.figure(figsize=(10, 4))
    plt.bar(hours, df_hours['HP_Num'], alpha=0.7, label='HP Count')
    plt.ylabel('HP Count')
    ax2 = plt.twinx()
    ax2.plot(hours, par.Elec_Price, 'r-o', label='Electricity Price (CNY/kWh)')
    ax2.set_ylabel('Price (CNY/kWh)')
    plt.xlabel('Hour')
    plt.title('HP Count vs Electricity Price')
    plt.grid(True)
    plt.tight_layout()
    plt.savefig(path, dpi=200)
    plt.close()
    return path


import numpy as np
import matplotlib.pyplot as plt
import matplotlib.colors as mcolors


def plot_and_save_onoff_heatmap(matrix, title, row_labels, col_labels, save_path):
    """
    绘制设备启停热力图（修正版：绝对保证相邻颜色不同）
    """
    n_devices, n_hours = matrix.shape
    DEVICE_ROWS = 2  # 设备占2行
    GAP_ROWS = 1  # 间隙占1行 (0.5倍高度)
    total_height = n_devices * DEVICE_ROWS + (n_devices - 1) * GAP_ROWS

    # --------------------------
    # 学术高区分度配色库 (20色)
    # --------------------------
    academic_colors = [
        "#1f77b4", "#ff7f0e", "#2ca02c", "#d62728", "#9467bd",
        "#8c564b", "#e377c2", "#7f7f7f", "#bcbd22", "#17becf",
        "#aec7e8", "#ffbb78", "#98df8a", "#ff9896", "#c5b0d5",
        "#c49c94", "#f7b6d2", "#c7c7c7", "#dbdb8d", "#9edae5"
    ]
    num_palette = len(academic_colors)

    # --------------------------
    # 构建可视化矩阵
    # 0: 间隙(透明), 1: 未激活(白色), 2-21: 激活状态(对应色板)
    # --------------------------
    vis_matrix = np.zeros((total_height, n_hours))  # 默认为0 (间隙)

    current_row = 0
    for i in range(n_devices):
        # 计算当前设备的激活颜色ID: (i % 20) + 2
        # matrix[i] == 1 时赋值为颜色ID，为 0 时赋值为 1 (白色)
        active_id = (i % num_palette) + 2
        row_values = np.where(matrix[i] == 1, active_id, 1)

        vis_matrix[current_row: current_row + DEVICE_ROWS, :] = row_values
        current_row += DEVICE_ROWS + GAP_ROWS

    # --------------------------
    # 颜色映射设置
    # --------------------------
    # 索引0: 透明(RGBA), 索引1: 白色, 索引2-21: 学术配色
    color_list = [(1, 1, 1, 0), (1, 1, 1, 1)] + [mcolors.to_rgba(c) for c in academic_colors]
    cmap = mcolors.ListedColormap(color_list)

    # 定义边界：[0, 1, 2, ..., 22] 对应上述 color_list 的索引
    bounds = np.arange(len(color_list) + 1)
    norm = mcolors.BoundaryNorm(bounds, cmap.N)

    # --------------------------
    # 绘图
    # --------------------------
    plt.rcParams['font.sans-serif'] = ['Arial']
    plt.rcParams['axes.unicode_minus'] = False
    fig, ax = plt.subplots(figsize=(max(8, n_hours * 0.35), max(4, total_height * 0.2)))

    # 绘制热力图
    im = ax.imshow(vis_matrix, aspect='auto', interpolation='none', cmap=cmap, norm=norm)

    # --------------------------
    # 坐标轴与辅助线
    # --------------------------
    ax.set_xticks(np.arange(n_hours))
    ax.set_xticklabels(col_labels, fontsize=9, ha='center')

    # 小时之间绘制纵向虚线
    for x in np.arange(0.5, n_hours, 1):
        ax.axvline(x=x, color='gray', linestyle='--', alpha=0.3, lw=0.5)

    # Y轴设备标签居中
    y_ticks = [DEVICE_ROWS / 2 - 0.5 + i * (DEVICE_ROWS + GAP_ROWS) for i in range(n_devices)]
    ax.set_yticks(y_ticks)
    ax.set_yticklabels(row_labels, fontsize=9)

    # --------------------------
    # 学术样式修饰
    # --------------------------
    for spine in ax.spines.values():
        spine.set_visible(False)
    ax.grid(False)
    ax.tick_params(length=0, pad=3)
    ax.set_title(title, fontsize=11, weight='bold')
    ax.set_xlabel('Hour', fontsize=10)
    ax.set_ylabel('Device', fontsize=10)

    # 保存
    plt.tight_layout()
    plt.savefig(save_path, dpi=300, bbox_inches='tight', facecolor='white')
    plt.close()
    return save_path


# -----------------------
# run_simulation：主仿真函数，整合全流程调度、计算、可视化、输出
# 输入：save_dir-输出目录，run_pso-是否运行PSO，pso_particles-粒子数，pso_iters-迭代次数
# 输出：完整仿真结果
# -----------------------
def run_simulation(save_dir='output', run_pso=False, pso_particles=30, pso_iters=60):
    os.makedirs(save_dir, exist_ok=True)  # 创建输出目录
    par = Params()
    t_vec = np.arange(par.T_Max)  # 时间向量

    # 模拟天气数据
    weather_data = {'Temp': 25.0 + 5.0 * np.sin(2.0 * np.pi * t_vec / 24.0), 'Pressure': np.ones(par.T_Max) * 101.3}
    # 模拟卸船计划
    unload_plan = np.zeros(par.T_Max)
    unload_plan[10:14] = 12000.0
    unload_plan[14:17] = 12000.0

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
            'Hour': t,
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
            'SW_Small': int(s.get('SW_small', 0)),
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

    # 保存结果到Excel
    excel_path = os.path.join(save_dir, 'LNG_Schedule_Report.xlsx')
    with pd.ExcelWriter(excel_path, engine='openpyxl') as writer:
        df_hours.to_excel(writer, sheet_name='Hourly_Summary', index=False)
        df_lp_on.to_excel(writer, sheet_name='LP_OnOff', index=False)
        df_hp_on.to_excel(writer, sheet_name='HP_OnOff', index=False)
        df_swbig_on.to_excel(writer, sheet_name='SWBig_OnOff', index=False)
        df_swsmall_on.to_excel(writer, sheet_name='SWSmall_OnOff', index=False)
        df_orv_on.to_excel(writer, sheet_name='ORV_OnOff', index=False)
        df_comp_levels.to_excel(writer, sheet_name='Comp_Levels', index=False)
        pd.DataFrame({'Hour': Hour, 'BOG_mech_kgph': bog_mech, 'BOG_pred_kgph': bog_pred}).to_excel(writer,
                                                                                                    sheet_name='BOG',
                                                                                                    index=False)
    print(f"Excel saved: {excel_path}")

    # 保存所有图表
    saved_images = {}
    saved_images['bog'] = plot_and_save_bog(bog_mech, bog_pred, t_vec, save_dir)
    saved_images['comp_levels'] = plot_and_save_comp_levels(unitized['Comp'], save_dir)
    lp_p, hp_p = plot_and_save_pressure(df_hours, save_dir)
    saved_images['lp_pressure'] = lp_p
    saved_images['hp_pressure'] = hp_p
    saved_images['tank_level'] = plot_and_save_tank_level(df_hours, save_dir)
    saved_images['hpcount_price'] = plot_and_save_price_and_hpcount(df_hours, par, save_dir)

    # 设备启停热力图
    hours_labels = [str(h) for h in range(par.T_Max)]
    lp_row_labels = [f'LP#{i + 1}' for i in range(par.Num_LP_Pumps)]
    hp_row_labels = [f'HP#{i + 1}' for i in range(par.Num_HP_Pumps)]
    swbig_row_labels = [f'SWBig#{i + 1}' for i in range(par.Num_SW_Big)]
    swsmall_row_labels = [f'SWSmall#{i + 1}' for i in range(par.Num_SW_Small)]
    orv_row_labels = [f'ORV#{i + 1}' for i in range(par.Num_ORV)]

    saved_images['lp_onoff'] = plot_and_save_onoff_heatmap(unitized['LP'].T, 'LP Pumps On/Off', lp_row_labels,
                                                           hours_labels, os.path.join(save_dir, 'plot_lp_onoff.png'))
    saved_images['hp_onoff'] = plot_and_save_onoff_heatmap(unitized['HP'].T, 'HP Pumps On/Off', hp_row_labels,
                                                           hours_labels, os.path.join(save_dir, 'plot_hp_onoff.png'))
    saved_images['swbig_onoff'] = plot_and_save_onoff_heatmap(unitized['SW_Big'].T, 'SW Big Pumps On/Off',
                                                              swbig_row_labels, hours_labels,
                                                              os.path.join(save_dir, 'plot_swbig_onoff.png'))
    saved_images['swsmall_onoff'] = plot_and_save_onoff_heatmap(unitized['SW_Small'].T, 'SW Small Pumps On/Off',
                                                                swsmall_row_labels, hours_labels,
                                                                os.path.join(save_dir, 'plot_swsmall_onoff.png'))
    saved_images['orv_onoff'] = plot_and_save_onoff_heatmap(unitized['ORV'].T, 'ORV On/Off', orv_row_labels,
                                                            hours_labels, os.path.join(save_dir, 'plot_orv_onoff.png'))

    # 压缩机档位热力图
    comp_img_path = os.path.join(save_dir, 'plot_comp_levels_heatmap.png')
    plt.figure(figsize=(8, 3))
    plt.imshow(unitized['Comp'].T, aspect='auto', interpolation='nearest', cmap='viridis', vmin=0, vmax=1)
    plt.colorbar(label='Comp Level (fraction)')
    plt.title('Compressor Levels Heatmap')
    plt.xlabel('Hour')
    plt.ylabel('Compressor')
    plt.xticks(np.arange(par.T_Max), hours_labels, rotation=0)
    plt.yticks([0, 1], ['Comp1', 'Comp2'])
    plt.tight_layout()
    plt.savefig(comp_img_path, dpi=200)
    plt.close()
    saved_images['comp_levels_heatmap'] = comp_img_path

    # 打印保存的图片路径
    print("Saved images:")
    for k, v in saved_images.items():
        print(f" - {k}: {v}")

    # 保存运行总结
    summary = {
        'excel': excel_path,
        'images': saved_images,
        'timestamp': datetime.now().isoformat(),
        'estimated_total_electric_cost': float(df_hours['Hourly_Cost_CNY'].sum())
    }
    summary_path = os.path.join(save_dir, 'run_summary.json')
    pd.Series(summary).to_json(summary_path)
    print(f"Summary saved: {summary_path}")

    return {
        'params': par,
        'df_hours': df_hours,
        'unitized': unitized,
        'images': saved_images,
        'summary': summary
    }


if __name__ == '__main__':
    # 运行主程序，开启PSO优化
    res = run_simulation(save_dir='output', run_pso=True, pso_particles=40, pso_iters=20)
    print("Done. Results in ./output/")