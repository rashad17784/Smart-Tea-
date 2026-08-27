# ============================================================
# smarttea_ai_api.py
# SmartTea Python AI Server
# Version 2.2 — Multi-Output LSTM + 30-Day Price Forecast
# ============================================================

from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from typing import List, Optional
import numpy as np
import joblib
import os

os.environ['TF_CPP_MIN_LOG_LEVEL'] = '3'
from tensorflow.keras.models import load_model

# ---- Create FastAPI app ----
app = FastAPI(
    title="SmartTea AI API",
    description="AI predictions for SmartTea system",
    version="2.2.0"
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"]
)

# ============================================================
# Load all models when server starts
# ============================================================
print("="*55)
print("   Loading SmartTea AI models v2.2...")
print("="*55)

MODELS_DIR = "saved_models"
GRADES     = ['BOP', 'BOPF', 'DUST', 'FNGS', 'OP']

# ── LSTM v2: one model + one scaler per grade ─────────────
# Direct multi-output: 30 / 45 / 60 day forecasting
# No recursive loop — no error compounding
lstm_v2_models  = {}
lstm_v2_scalers = {}

for grade in GRADES:
    g = grade.lower()
    lstm_v2_models[grade] = load_model(
        f'{MODELS_DIR}/lstm_v2/lstm_{g}.keras'
    )
    lstm_v2_scalers[grade] = joblib.load(
        f'{MODELS_DIR}/lstm_v2/scaler_{g}.pkl'
    )

lstm_v2_meta = joblib.load(
    f'{MODELS_DIR}/lstm_v2/metadata.pkl'
)
print("✅ LSTM v2 loaded (30/45/60 day direct multi-output)")
print(f"   Grades loaded: {GRADES}")

# ── LSTM v1: keep as fallback (DO NOT REMOVE) ─────────────
lstm_v1_model  = load_model(
    f'{MODELS_DIR}/lstm/lstm_demand_model.keras'
)
lstm_v1_scaler = joblib.load(
    f'{MODELS_DIR}/lstm/lstm_scaler.pkl'
)
print("✅ LSTM v1 loaded (fallback, original 30-day recursive)")

# ── Price: Linear Regression (single step) ────────────────
lr_model     = joblib.load(
    f'{MODELS_DIR}/linear_regression/lr_model.pkl'
)
lr_scaler    = joblib.load(
    f'{MODELS_DIR}/linear_regression/lr_scaler.pkl'
)
feature_cols = joblib.load(
    f'{MODELS_DIR}/linear_regression/feature_cols.pkl'
)
print("✅ Linear Regression loaded (price single-step)")

# ── Price: Multi-step LR (30 horizons) ────────────────────
# Day+1 to Day+7  : original models (untouched)
# Day+8 to Day+30 : new extended models
lr_ms = {}
for h in range(1, 31):
    lr_ms[h] = joblib.load(
        f'{MODELS_DIR}/multistep/lr_day{h}.pkl'
    )
ms_scaler = joblib.load(
    f'{MODELS_DIR}/multistep/scaler.pkl'
)
ms_meta = joblib.load(
    f'{MODELS_DIR}/multistep/metadata_v2.pkl'
)
print("✅ Multi-step LR loaded (30 horizons)")
print(f"   Avg MAPE  7d: {ms_meta['avg_mape_7d']}%")
print(f"   Avg MAPE 14d: {ms_meta['avg_mape_14d']}%")
print(f"   Avg MAPE 30d: {ms_meta['avg_mape_30d']}%")

# ── Anomaly: Isolation Forest ─────────────────────────────
iso_models = {}
sc_demand  = {}
for grade in GRADES:
    iso_models[grade] = joblib.load(
        f'{MODELS_DIR}/anomaly/iso_demand_{grade}.pkl'
    )
    sc_demand[grade] = joblib.load(
        f'{MODELS_DIR}/anomaly/sc_demand_{grade}.pkl'
    )
iso_price = joblib.load(
    f'{MODELS_DIR}/anomaly/iso_price.pkl'
)
sc_price = joblib.load(
    f'{MODELS_DIR}/anomaly/sc_price.pkl'
)
print("✅ Anomaly models loaded (Isolation Forest + rules)")

print()
print("="*55)
print("   SmartTea AI API v2.2 READY!")
print("   http://localhost:8000")
print("="*55)


# ============================================================
# Request data classes
# ============================================================

class DemandRequest(BaseModel):
    grade:               str
    last_60_days_demand: List[float]  # exactly 60 values
    horizon_days:        int = 30     # 30, 45, or 60


class PriceRequest(BaseModel):
    current_price:      float
    price_lag1:         float
    price_lag2:         float
    price_lag3:         float
    price_lag7:         float
    price_lag14:        float
    rolling_mean7:      float
    rolling_mean30:     float
    rolling_std7:       float
    price_change_pct:   float
    quantity_kg:        float
    qty_rolling7:       float
    firewood_kg:        float
    firewood_cost:      float
    total_cost:         float
    temperature:        float
    rainfall_mm:        float
    heavy_rain:         int
    supplier_delivered: int
    supplier_qty:       float
    promotion:          int
    month_start:        int
    month:              int
    day_of_week:        int
    quarter:            int
    is_weekend:         int
    day_of_year:        int
    day:                int


class PriceMultiStepRequest(BaseModel):
    # Same fields as PriceRequest
    # Only difference: horizon_days added at the bottom
    current_price:      float
    price_lag1:         float
    price_lag2:         float
    price_lag3:         float
    price_lag7:         float
    price_lag14:        float
    rolling_mean7:      float
    rolling_mean30:     float
    rolling_std7:       float
    price_change_pct:   float
    quantity_kg:        float
    qty_rolling7:       float
    firewood_kg:        float
    firewood_cost:      float
    total_cost:         float
    temperature:        float
    rainfall_mm:        float
    heavy_rain:         int
    supplier_delivered: int
    supplier_qty:       float
    promotion:          int
    month_start:        int
    month:              int
    day_of_week:        int
    quarter:            int
    is_weekend:         int
    day_of_year:        int
    day:                int
    horizon_days:       int = 7    # 7, 14, or 30


class AnomalyRequest(BaseModel):
    grade:          str
    demand_kg:      float
    stock_level_kg: float
    price_per_kg:   float
    day_of_week:    int
    month:          int
    is_weekend:     int


# ============================================================
# Endpoints
# ============================================================

@app.get("/")
def home():
    return {
        "status":          "SmartTea AI API Running",
        "version":         "2.2.0",
        "demand_forecast": "supports 30 / 45 / 60 days",
        "price_forecast":  "supports 7 / 14 / 30 days",
        "docs":            "http://localhost:8000/docs"
    }


@app.get("/health")
def health():
    return {
        "status":      "healthy",
        "lstm_v2":     "loaded (30/45/60d direct multi-output)",
        "lstm_v1":     "loaded (fallback)",
        "lr":          "loaded (price single-step)",
        "multistep":   "loaded (price 7/14/30 days)",
        "anomaly":     "loaded (model + business rules)",
        "api_version": "2.2.0"
    }


@app.get("/models/info")
def model_info():
    """
    Returns full info about all loaded models.
    Dashboard uses this to show model versions and accuracy.
    """
    grade_metrics = {}
    for grade in GRADES:
        gm = lstm_v2_meta['per_grade'][grade]
        grade_metrics[grade] = {
            'mape_30d': gm['mape_30'],
            'mape_45d': gm['mape_45'],
            'mape_60d': gm['mape_60'],
        }

    return {
        "demand_lstm_v2": {
            "version":            lstm_v2_meta['version'],
            "strategy":           lstm_v2_meta['strategy'],
            "look_back":          lstm_v2_meta['look_back'],
            "max_horizon":        60,
            "supported_horizons": [30, 45, 60],
            "grades":             lstm_v2_meta['grades'],
            "grade_metrics":      grade_metrics,
        },
        "price_lr": {
            "horizon":  1,
            "strategy": "single_step",
            "mape":     2.994
        },
        "price_multistep_lr": {
            "max_horizon":        ms_meta['max_horizon'],
            "supported_horizons": ms_meta['supported_horizons'],
            "strategy":           ms_meta['strategy'],
            "avg_mape_7d":        ms_meta['avg_mape_7d'],
            "avg_mape_14d":       ms_meta['avg_mape_14d'],
            "avg_mape_30d":       ms_meta['avg_mape_30d'],
        },
        "anomaly": {
            "model":    "Isolation Forest + Business Rules",
            "grades":   GRADES,
            "version":  "2.0"
        }
    }


# ── DEMAND FORECAST ────────────────────────────────────────
@app.post("/predict/demand")
def predict_demand(req: DemandRequest):
    """
    Predict tea demand for 30, 45, or 60 days ahead.

    Strategy: Direct multi-output LSTM.
    One model per grade.
    Input: last 60 days of demand for that grade.
    Output: next N days predicted all at once (no loop).
    """
    try:
        # Validate grade
        grade = req.grade.upper()
        if grade not in lstm_v2_models:
            raise HTTPException(
                status_code=400,
                detail=(
                    f"Grade '{grade}' not supported. "
                    f"Must be one of: {GRADES}"
                )
            )

        # Validate horizon
        allowed_horizons = [30, 45, 60]
        if req.horizon_days not in allowed_horizons:
            raise HTTPException(
                status_code=400,
                detail=(
                    f"horizon_days must be 30, 45, or 60. "
                    f"Got: {req.horizon_days}"
                )
            )

        # Validate input length
        if len(req.last_60_days_demand) != 60:
            raise HTTPException(
                status_code=400,
                detail=(
                    f"last_60_days_demand must have "
                    f"exactly 60 values. "
                    f"Got: {len(req.last_60_days_demand)}"
                )
            )

        # Load grade-specific model and scaler
        model  = lstm_v2_models[grade]
        scaler = lstm_v2_scalers[grade]

        # Scale input: (60,) → (60, 1)
        data   = np.array(
            req.last_60_days_demand
        ).reshape(-1, 1)
        scaled = scaler.transform(data)

        # Reshape for LSTM: (1, 60, 1)
        x_in = scaled.reshape(1, 60, 1)

        # Direct forward pass produces the next 60 days.
        pred_scaled = model.predict(x_in, verbose=0)[0]
        pred_h = pred_scaled[:req.horizon_days]
        strategy = "direct_multioutput"

        # Inverse transform → real kg values
        pred_kg = scaler.inverse_transform(
            pred_h.reshape(-1, 1)
        ).flatten()

        # Get MAPE for this grade + horizon
        gm = lstm_v2_meta['per_grade'][grade]
        mape_map = {
            30: gm['mape_30'],
            45: gm['mape_45'],
            60: gm['mape_60'],
        }

        predictions = [
            round(float(v), 2) for v in pred_kg
        ]

        return {
            "grade":                grade,
            "horizon_days":         req.horizon_days,
            "forecast_days":        req.horizon_days,
            "predictions":          predictions,
            "model":                "LSTM v2 (Direct Multi-Output)",
            "model_version":        lstm_v2_meta['version'],
            "strategy":             strategy,
            "expected_mape":        mape_map[req.horizon_days],
            "no_error_compounding": True,
            "input_days_used":      60
        }

    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


# ── PRICE FORECAST — single step ──────────────────────────
@app.post("/predict/price")
def predict_price(req: PriceRequest):
    """
    Predict tomorrow's green leaf price.
    Uses single-step Linear Regression.
    """
    try:
        features = np.array([[
            req.month,          req.day_of_week,
            req.quarter,        req.is_weekend,
            req.day_of_year,    req.day,
            req.current_price,  req.price_lag1,
            req.price_lag2,     req.price_lag3,
            req.price_lag7,     req.price_lag14,
            req.rolling_mean7,  req.rolling_mean30,
            req.rolling_std7,   req.price_change_pct,
            req.quantity_kg,    req.qty_rolling7,
            req.firewood_kg,    req.firewood_cost,
            req.total_cost,     req.temperature,
            req.rainfall_mm,    req.heavy_rain,
            req.supplier_delivered, req.supplier_qty,
            req.promotion,      req.month_start
        ]])

        features_sc = lr_scaler.transform(features)
        prediction  = lr_model.predict(features_sc)[0]
        change_pct  = (
            (prediction - req.current_price)
            / req.current_price * 100
        )

        if abs(change_pct) < 2:
            trend = "STABLE"
        elif change_pct > 0:
            trend = "RISING"
        else:
            trend = "FALLING"

        return {
            "predicted_price": round(float(prediction), 2),
            "current_price":   req.current_price,
            "change_pct":      round(float(change_pct), 2),
            "trend":           trend,
            "model":           "Linear Regression",
            "expected_mape":   2.994
        }

    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


# ── PRICE FORECAST — multi-step 7 / 14 / 30 days ──────────
@app.post("/predict/price/multistep")
def predict_multistep(req: PriceMultiStepRequest):
    """
    Predict green leaf price for 7, 14, or 30 days ahead.

    Strategy: Direct multi-horizon Linear Regression.
    One trained model per day horizon (lr_day1 to lr_day30).
    No recursive loop — no error compounding.
    Avg MAPE stays under 3.8% across all 30 days.
    """
    try:
        # Validate horizon
        allowed = [7, 14, 30]
        if req.horizon_days not in allowed:
            raise HTTPException(
                status_code=400,
                detail=(
                    f"horizon_days must be 7, 14, or 30. "
                    f"Got: {req.horizon_days}"
                )
            )

        # Build feature array (same order as training)
        features = np.array([[
            req.month,          req.day_of_week,
            req.quarter,        req.is_weekend,
            req.day_of_year,    req.day,
            req.current_price,  req.price_lag1,
            req.price_lag2,     req.price_lag3,
            req.price_lag7,     req.price_lag14,
            req.rolling_mean7,  req.rolling_mean30,
            req.rolling_std7,   req.price_change_pct,
            req.quantity_kg,    req.qty_rolling7,
            req.firewood_kg,    req.firewood_cost,
            req.total_cost,     req.temperature,
            req.rainfall_mm,    req.heavy_rain,
            req.supplier_delivered, req.supplier_qty,
            req.promotion,      req.month_start
        ]])

        # Scale features
        features_sc = ms_scaler.transform(features)

        # Predict for each day up to requested horizon
        results = []

        for h in range(1, req.horizon_days + 1):

            pred = lr_ms[h].predict(features_sc)[0]

            # Get actual MAPE for this day from metadata
            mape = ms_meta['mape_by_day'][h]

            # Trend vs current price
            change_pct = (
                (pred - req.current_price)
                / req.current_price * 100
            )

            if abs(change_pct) < 2:
                trend = "STABLE"
            elif change_pct > 0:
                trend = "RISING"
            else:
                trend = "FALLING"

            results.append({
                "day":           f"Day+{h}",
                "day_number":    h,
                "predicted":     round(float(pred), 2),
                "change_pct":    round(float(change_pct), 2),
                "trend":         trend,
                "expected_mape": round(mape, 3),
            })

        # Summary stats across forecast period
        predicted_prices = [r['predicted'] for r in results]
        avg_price        = round(np.mean(predicted_prices), 2)
        min_price        = round(np.min(predicted_prices),  2)
        max_price        = round(np.max(predicted_prices),  2)

        # Overall trend: current → last predicted day
        last_price  = results[-1]['predicted']
        overall_chg = (
            (last_price - req.current_price)
            / req.current_price * 100
        )

        if abs(overall_chg) < 2:
            overall_trend = "STABLE"
        elif overall_chg > 0:
            overall_trend = "RISING"
        else:
            overall_trend = "FALLING"

        # Pick correct avg MAPE for selected horizon
        horizon_mape_map = {
            7:  ms_meta['avg_mape_7d'],
            14: ms_meta['avg_mape_14d'],
            30: ms_meta['avg_mape_30d'],
        }

        return {
            "current_price": req.current_price,
            "horizon_days":  req.horizon_days,
            "forecast":      results,
            "summary": {
                "avg_price":          float(avg_price),
                "min_price":          float(min_price),
                "max_price":          float(max_price),
                "overall_trend":      overall_trend,
                "overall_change_pct": round(float(overall_chg), 2),
            },
            "model":          "Linear Regression (Direct)",
            "model_version":  ms_meta['version'],
            "strategy":       ms_meta['strategy'],
            "expected_mape":  round(
                horizon_mape_map[req.horizon_days], 3),
        }

    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


# ── ANOMALY DETECTION ──────────────────────────────────────
@app.post("/predict/anomaly")
def detect_anomaly(req: AnomalyRequest):
    """
    Detect anomalies in stock, demand, and price.

    Two-layer detection:
    Layer 1: Isolation Forest (statistical ML model)
    Layer 2: Business rules (grade-specific thresholds)

    Result is the WORST of the two layers.
    """
    try:
        grade = req.grade.upper()

        if grade not in iso_models:
            raise HTTPException(
                status_code=400,
                detail=f"Grade '{grade}' not supported."
            )

        # ── Layer 1: Isolation Forest ──────────────────────
        features = np.array([[
            req.demand_kg,    req.stock_level_kg,
            req.price_per_kg, req.day_of_week,
            req.month,        req.is_weekend
        ]])
        features_sc     = sc_demand[grade].transform(features)
        pred            = iso_models[grade].predict(features_sc)[0]
        score           = iso_models[grade].score_samples(
            features_sc)[0]
        is_anomaly_model = bool(pred == -1)

        # ── Layer 2: Business rules ────────────────────────
        GRADE_RULES = {
            'BOP': {
                'min_stock':       200,
                'critical_stock':   80,
                'max_demand':      600,
                'critical_demand': 800,
                'min_price':        70,
                'max_price':       160,
            },
            'BOPF': {
                'min_stock':       150,
                'critical_stock':   60,
                'max_demand':      400,
                'critical_demand': 600,
                'min_price':        70,
                'max_price':       160,
            },
            'DUST': {
                'min_stock':       120,
                'critical_stock':   50,
                'max_demand':      300,
                'critical_demand': 450,
                'min_price':        70,
                'max_price':       160,
            },
            'FNGS': {
                'min_stock':       100,
                'critical_stock':   40,
                'max_demand':      250,
                'critical_demand': 380,
                'min_price':        70,
                'max_price':       160,
            },
            'OP': {
                'min_stock':       180,
                'critical_stock':   70,
                'max_demand':      500,
                'critical_demand': 700,
                'min_price':        70,
                'max_price':       160,
            },
        }

        rules     = GRADE_RULES[grade]
        triggered = []

        # Stock check
        if req.stock_level_kg <= rules['critical_stock']:
            triggered.append({
                'rule':    'CRITICAL_LOW_STOCK',
                'message': (
                    f"Stock critically low: "
                    f"{req.stock_level_kg} kg "
                    f"(threshold: {rules['critical_stock']} kg)"
                )
            })
        elif req.stock_level_kg <= rules['min_stock']:
            triggered.append({
                'rule':    'LOW_STOCK',
                'message': (
                    f"Stock below minimum: "
                    f"{req.stock_level_kg} kg "
                    f"(threshold: {rules['min_stock']} kg)"
                )
            })

        # Demand check
        if req.demand_kg >= rules['critical_demand']:
            triggered.append({
                'rule':    'CRITICAL_HIGH_DEMAND',
                'message': (
                    f"Demand critically high: "
                    f"{req.demand_kg} kg "
                    f"(threshold: {rules['critical_demand']} kg)"
                )
            })
        elif req.demand_kg >= rules['max_demand']:
            triggered.append({
                'rule':    'HIGH_DEMAND',
                'message': (
                    f"Demand unusually high: "
                    f"{req.demand_kg} kg "
                    f"(threshold: {rules['max_demand']} kg)"
                )
            })

        # Price check
        if req.price_per_kg >= rules['max_price']:
            triggered.append({
                'rule':    'HIGH_PRICE',
                'message': (
                    f"Price unusually high: "
                    f"LKR {req.price_per_kg} "
                    f"(threshold: LKR {rules['max_price']})"
                )
            })
        elif req.price_per_kg <= rules['min_price']:
            triggered.append({
                'rule':    'LOW_PRICE',
                'message': (
                    f"Price unusually low: "
                    f"LKR {req.price_per_kg} "
                    f"(threshold: LKR {rules['min_price']})"
                )
            })

        # ── Combine both layers ────────────────────────────
        has_critical_rule = any(
            'CRITICAL' in t['rule'] for t in triggered
        )
        has_warning_rule = any(
            'CRITICAL' not in t['rule'] for t in triggered
        )

        if has_critical_rule:
            severity   = "CRITICAL"
            is_anomaly = True
            color      = "red"
            reasons    = [
                t['message'] for t in triggered
                if 'CRITICAL' in t['rule']
            ]
            message = (
                f"🚨 CRITICAL anomaly for {grade}! "
                + " | ".join(reasons)
            )

        elif has_warning_rule or (is_anomaly_model and score < -0.65):
            # Only trigger ML-only WARNING if score is strongly anomalous
            # This prevents false positives for normal operational values
            severity   = "WARNING"
            is_anomaly = True
            color      = "orange"
            if triggered:
                reasons = [t['message'] for t in triggered]
                message = (
                    f"⚠️ Unusual activity for {grade}: "
                    + " | ".join(reasons)
                )
            else:
                message = (
                    f"⚠️ Unusual statistical pattern "
                    f"detected for {grade}. "
                    f"Please verify."
                )

        else:
            severity   = "NORMAL"
            is_anomaly = False
            color      = "green"
            message    = f"✅ {grade} levels are normal."

        return {
            "grade":           grade,
            "is_anomaly":      is_anomaly,
            "severity":        severity,
            "message":         message,
            "color":           color,
            "score":           round(float(score), 4),
            "model_triggered": is_anomaly_model,
            "rules_triggered": triggered,
            "checks": {
                "stock_level_kg":  req.stock_level_kg,
                "demand_kg":       req.demand_kg,
                "price_per_kg":    req.price_per_kg,
                "min_stock_rule":  rules['min_stock'],
                "max_demand_rule": rules['max_demand'],
            }
        }

    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


# ── Start server ───────────────────────────────────────────
if __name__ == "__main__":
    import uvicorn
    port = int(os.environ.get("PORT", 8000))
    uvicorn.run(app, host="0.0.0.0", port=port)
