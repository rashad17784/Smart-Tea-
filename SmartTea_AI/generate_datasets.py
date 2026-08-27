# ============================================================
# SmartTea AI — Synthetic Dataset Generator
# Generates 2 realistic CSV datasets for your project
# ============================================================

import pandas as pd
import numpy as np
import os

# Set random seed for reproducibility
np.random.seed(42)

# Create folders if they don't exist
os.makedirs('data', exist_ok=True)
os.makedirs('plots', exist_ok=True)
print("✅ Folders ready!")

# ============================================================
#   DATASET 1: tea_demand_timeseries.csv
#   For: SARIMA vs LSTM (Time Series Forecasting)
# ============================================================

print("\n🔄 Generating Dataset 1: Tea Demand Time Series...")

dates   = pd.date_range(start='2021-01-01', end='2023-12-31', freq='D')
n_days  = len(dates)
print(f"   Total days: {n_days}")

tea_grades = ['BOP', 'BOPF', 'FNGS', 'DUST', 'OP']

grade_base_demand = {
    'BOP':  320,
    'BOPF': 280,
    'FNGS': 180,
    'DUST': 240,
    'OP':   120
}

grade_price = {
    'BOP':  1850,
    'BOPF': 1620,
    'FNGS': 1400,
    'DUST': 1550,
    'OP':   2800
}

all_records = []

for grade in tea_grades:
    base  = grade_base_demand[grade]
    price = grade_price[grade]

    trend = np.linspace(0, base * 0.15, n_days)

    day_of_week = np.array([d.weekday() for d in dates])
    weekly_pattern = {
        0: 1.05, 1: 1.10, 2: 1.08,
        3: 1.06, 4: 0.95, 5: 0.70, 6: 0.55
    }
    weekly = np.array([base * (weekly_pattern[d] - 1) for d in day_of_week])

    day_of_year = np.array([d.timetuple().tm_yday for d in dates])
    annual = (
        20 * np.sin(2 * np.pi * day_of_year / 365)
        + 12 * np.sin(4 * np.pi * day_of_year / 365)
        + 8  * np.cos(6 * np.pi * day_of_year / 365)
    )

    holiday_effect = np.zeros(n_days)
    for i, d in enumerate(dates):
        if d.month == 4 and d.day in [13, 14]:
            holiday_effect[i] = -base * 0.40
        elif d.month == 4 and d.day in [11, 12, 15, 16]:
            holiday_effect[i] = -base * 0.20
        elif d.month == 12 and 20 <= d.day <= 31:
            holiday_effect[i] =  base * 0.15
        elif d.month == 1 and d.day <= 7:
            holiday_effect[i] =  base * 0.10
        elif d.month == 5 and d.day in [5, 6]:
            holiday_effect[i] = -base * 0.30

    monsoon_effect = np.zeros(n_days)
    for i, d in enumerate(dates):
        if d.month in [5, 6, 7]:
            monsoon_effect[i] = -base * 0.08
        elif d.month in [11, 12, 1]:
            monsoon_effect[i] =  base * 0.05

    noise  = np.random.normal(0, base * 0.06, n_days)
    demand = base + trend + weekly + annual + holiday_effect + monsoon_effect + noise
    demand = np.clip(demand, base * 0.3, base * 1.8)
    demand = np.round(demand, 1)

    stock = []
    current_stock = base * 30
    for i in range(n_days):
        production_today = demand[i] * np.random.uniform(0.85, 1.25)
        current_stock    = current_stock + production_today - demand[i]
        current_stock    = max(current_stock, 0)
        stock.append(round(current_stock, 1))

    low_stock_threshold = base * 10
    low_stock_flag      = [1 if s < low_stock_threshold else 0 for s in stock]

    price_noise  = np.random.normal(0, price * 0.03, n_days)
    price_trend  = np.linspace(0, price * 0.08, n_days)
    daily_price  = np.round(price + price_trend + price_noise, 2)
    daily_price  = np.clip(daily_price, price * 0.85, price * 1.20)

    delivery_flag = np.array([1 if d.weekday() in [0, 2, 4] else 0 for d in dates])

    for i in range(n_days):
        all_records.append({
            'Date':          dates[i].strftime('%Y-%m-%d'),
            'TeaGrade':      grade,
            'DemandKg':      demand[i],
            'StockLevelKg':  stock[i],
            'LowStockFlag':  low_stock_flag[i],
            'PricePerKgLKR': daily_price[i],
            'DeliveryDay':   delivery_flag[i],
            'Month':         dates[i].month,
            'DayOfWeek':     dates[i].weekday(),
            'DayOfYear':     dates[i].timetuple().tm_yday,
            'Quarter':       dates[i].quarter,
            'IsWeekend':     1 if dates[i].weekday() >= 5 else 0,
            'Warehouse':     'Nuwara Eliya Main Warehouse'
        })

df_timeseries = pd.DataFrame(all_records)
df_timeseries = df_timeseries.sort_values(['Date','TeaGrade']).reset_index(drop=True)
df_timeseries.to_csv('data/tea_demand_timeseries.csv', index=False)

print(f"   ✅ Dataset 1 created!")
print(f"   Total rows  : {len(df_timeseries):,}")
print(f"   Date range  : {df_timeseries['Date'].min()} to {df_timeseries['Date'].max()}")
print(f"   Tea grades  : {df_timeseries['TeaGrade'].unique().tolist()}")
print(f"   Saved to    : data/tea_demand_timeseries.csv")
print(f"\n   First 5 rows:")
print(df_timeseries.head())


# ============================================================
#   DATASET 2: tea_price_regression.csv
#   For: Linear Regression vs XGBoost
# ============================================================

print("\n🔄 Generating Dataset 2: Tea Price Regression Dataset...")

suppliers = {
    'SUP-001': {'name': 'Perera Tea Estates',   'reliability': 0.95, 'base_qty': 850},
    'SUP-002': {'name': 'Silva Green Leaves',   'reliability': 0.88, 'base_qty': 620},
    'SUP-003': {'name': 'Nuwara Estates Ltd',   'reliability': 0.92, 'base_qty': 730},
    'SUP-004': {'name': 'Highlands Tea Farm',   'reliability': 0.79, 'base_qty': 510},
    'SUP-005': {'name': 'Kandyan Leaf Growers', 'reliability': 0.85, 'base_qty': 680}
}
supplier_ids       = list(suppliers.keys())
BASE_LEAF_PRICE    = 95.0
regression_records = []

for i, date in enumerate(dates):
    day_of_year = date.timetuple().tm_yday
    month       = date.month
    weekday     = date.weekday()

    price_trend_effect = i * 0.008
    price_seasonal     = (
        8.0 * np.sin(2 * np.pi * day_of_year / 365 - 1.5)
        + 4.0 * np.sin(4 * np.pi * day_of_year / 365)
    )
    price_shock = 0
    if date.year == 2022 and month in [6, 7, 8]:
        price_shock = np.random.uniform(8, 20)
    if date.year == 2023 and month in [1, 2]:
        price_shock = np.random.uniform(5, 15)

    weekly_price = {0:1.0,1:2.5,2:2.0,3:1.5,4:0.5,5:-1.0,6:-2.0}
    price_weekly = weekly_price[weekday]
    price_noise  = np.random.normal(0, 3.5)

    green_leaf_price = (BASE_LEAF_PRICE + price_trend_effect
                        + price_seasonal + price_shock
                        + price_weekly   + price_noise)
    green_leaf_price = max(round(green_leaf_price, 2), 65.0)

    base_qty         = 4500
    price_qty_effect = -15 * (green_leaf_price - BASE_LEAF_PRICE) / BASE_LEAF_PRICE
    qty_seasonal     = 200 * np.sin(2 * np.pi * day_of_year / 365)
    qty_shock        = 0
    if date.year == 2022 and month in [6, 7, 8]:
        qty_shock = -np.random.uniform(300, 700)
    if date.year == 2023 and month in [1, 2]:
        qty_shock = -np.random.uniform(200, 500)
    qty_weekend      = -1200 if weekday >= 5 else 0
    qty_noise        = np.random.normal(0, 180)

    green_leaf_qty   = (base_qty + price_qty_effect + qty_seasonal
                        + qty_shock + qty_weekend + qty_noise)
    green_leaf_qty   = max(round(green_leaf_qty, 1), 0)

    base_firewood    = 1200
    fw_seasonal      = 80 * np.cos(2 * np.pi * day_of_year / 365)
    fw_weekend       = -600 if weekday >= 5 else 0
    fw_noise         = np.random.normal(0, 75)
    firewood_qty     = max(round(base_firewood + fw_seasonal + fw_weekend + fw_noise, 1), 0)

    fw_price         = max(round(18.0 + i * 0.002 + np.random.normal(0, 1.2), 2), 12.0)

    base_temp        = 16.0
    temp_seasonal    = 4 * np.sin(2 * np.pi * day_of_year / 365 + 1)
    temperature      = round(np.clip(base_temp + temp_seasonal + np.random.normal(0,1.5), 8, 26), 1)

    if month in [5,6,7]:
        rainfall = max(round(np.random.exponential(18), 1), 0)
    elif month in [10,11,12]:
        rainfall = max(round(np.random.exponential(12), 1), 0)
    else:
        rainfall = max(round(np.random.exponential(5), 1), 0)

    heavy_rain   = 1 if rainfall > 30 else 0
    supplier_id  = np.random.choice(supplier_ids, p=[0.28,0.20,0.22,0.15,0.15])
    sup_info     = suppliers[supplier_id]
    delivered    = 1 if (np.random.random() < sup_info['reliability'] and weekday < 6) else 0
    sup_qty      = max(round(sup_info['base_qty'] + np.random.normal(0,60),1),0) if delivered else 0
    promotion    = 1 if np.random.random() < 0.08 else 0
    month_start  = 1 if date.day <= 5 else 0
    total_cost   = round((green_leaf_price * green_leaf_qty) + (fw_price * firewood_qty), 2)

    regression_records.append({
        'Date':                   date.strftime('%Y-%m-%d'),
        'Year':                   date.year,
        'Month':                  month,
        'Day':                    date.day,
        'DayOfWeek':              weekday,
        'Quarter':                date.quarter,
        'IsWeekend':              1 if weekday >= 5 else 0,
        'DayOfYear':              day_of_year,
        'GreenLeafPricePerKgLKR': green_leaf_price,
        'GreenLeafQuantityKg':    green_leaf_qty,
        'FirewoodCollectedKg':    firewood_qty,
        'FirewoodCostPerKgLKR':   fw_price,
        'TemperatureCelsius':     temperature,
        'RainfallMM':             rainfall,
        'HeavyRainFlag':          heavy_rain,
        'SupplierID':             supplier_id,
        'SupplierName':           sup_info['name'],
        'SupplierDelivered':      delivered,
        'SupplierQuantityKg':     sup_qty,
        'PromotionActive':        promotion,
        'MonthStartBonus':        month_start,
        'TotalDailyCostLKR':      total_cost
    })

df_reg = pd.DataFrame(regression_records)

df_reg['NextDayGreenLeafPrice'] = df_reg['GreenLeafPricePerKgLKR'].shift(-1)
df_reg['PriceLag1']             = df_reg['GreenLeafPricePerKgLKR'].shift(1)
df_reg['PriceLag2']             = df_reg['GreenLeafPricePerKgLKR'].shift(2)
df_reg['PriceLag3']             = df_reg['GreenLeafPricePerKgLKR'].shift(3)
df_reg['PriceLag7']             = df_reg['GreenLeafPricePerKgLKR'].shift(7)
df_reg['PriceLag14']            = df_reg['GreenLeafPricePerKgLKR'].shift(14)
df_reg['PriceRollingMean7']     = df_reg['GreenLeafPricePerKgLKR'].rolling(7).mean().round(2)
df_reg['PriceRollingMean30']    = df_reg['GreenLeafPricePerKgLKR'].rolling(30).mean().round(2)
df_reg['PriceRollingStd7']      = df_reg['GreenLeafPricePerKgLKR'].rolling(7).std().round(2)
df_reg['QtyRollingMean7']       = df_reg['GreenLeafQuantityKg'].rolling(7).mean().round(1)
df_reg['PriceChangePct']        = (
    (df_reg['GreenLeafPricePerKgLKR'] - df_reg['PriceLag1'])
    / df_reg['PriceLag1'] * 100
).round(4)

df_reg = df_reg.dropna().reset_index(drop=True)
df_reg.to_csv('data/tea_price_regression.csv', index=False)

print(f"   ✅ Dataset 2 created!")
print(f"   Total rows  : {len(df_reg):,}")
print(f"   Date range  : {df_reg['Date'].min()} to {df_reg['Date'].max()}")
print(f"   Columns     : {len(df_reg.columns)}")
print(f"   Saved to    : data/tea_price_regression.csv")
print(f"\n   First 5 rows:")
print(df_reg[['Date','GreenLeafPricePerKgLKR','GreenLeafQuantityKg',
              'FirewoodCollectedKg','TemperatureCelsius',
              'RainfallMM','NextDayGreenLeafPrice']].head())


# ============================================================
#   FINAL SUMMARY
# ============================================================
print("\n" + "="*60)
print("        ✅ ALL DATASETS GENERATED SUCCESSFULLY!")
print("="*60)
print(f"""
📁 FILES CREATED:

   📊 data/tea_demand_timeseries.csv
      Rows    : {len(df_timeseries):,}
      Purpose : SARIMA vs LSTM time series forecasting
      Period  : 2021-01-01 to 2023-12-31
      Grades  : BOP, BOPF, FNGS, DUST, OP

   📊 data/tea_price_regression.csv
      Rows    : {len(df_reg):,}
      Purpose : Linear Regression vs XGBoost
      Period  : 2021-01-01 to 2023-12-31
      Target  : NextDayGreenLeafPrice

🎯 NEXT STEP:
   Run:  python verify_data.py
""")
print("="*60)