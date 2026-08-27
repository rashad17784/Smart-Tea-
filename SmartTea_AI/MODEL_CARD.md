# SmartTea AI model card

**Author:** Mohamed Rashad  
**API version:** 2.2.0  
**Purpose:** Decision support for tea demand, green-leaf price and operational anomaly review

## Intended use

The models support an authorised user reviewing production demand, price movement or unusual stock activity. They do not automatically place orders, change prices or adjust inventory. A human remains responsible for operational decisions.

## Demand forecasting

- **Approach:** grade-specific LSTM, version 2.1;
- **Grades:** BOP, BOPF, DUST, FNGS and OP;
- **Input:** 60 ordered daily demand values;
- **Output:** a direct 60-step forecast displayed as 30, 45 or 60 days;
- **Data:** reproducible project research dataset created with a fixed seed;
- **Saved research MAPE:** approximately 6.368% to 10.847%, depending on grade and evaluation run.

These figures describe research evaluation, not proven factory performance. Genuine factory history must be collected, quality-checked, divided chronologically and used for retraining/evaluation before a factory accuracy claim is made.

## Green-leaf price forecasting

- **Approach:** direct linear regression;
- **Outputs:** tomorrow and 7-, 14- or 30-day estimates;
- **Inputs:** current/lagged price and derived rolling, quantity, weather, supplier and calendar features;
- **Selection:** compared with alternative tree-based models, with the simpler deployed model retained for the runtime;
- **External context:** FRED and World Bank monthly tea-price series supported the validation research.

The web interface receives current price, previous price, rainfall and temperature from the operator and derives the remaining runtime features. There is no live market or weather feed.

## Anomaly detection

- **Approach:** Isolation Forest with grade-aware scaling and deterministic operational rules;
- **Inputs:** grade, demand, stock, price and calendar information;
- **Outputs:** NORMAL, WARNING or CRITICAL, plus score, message and triggered rules;
- **False-positive control:** an ML-only warning requires a score below `-0.65`;
- **Safety rules:** important operational conditions can raise CRITICAL even when the statistical signal is uncertain.

Thresholds and contamination assumptions require calibration against genuine factory behaviour.

## Runtime and traceability

Models and preprocessing artefacts are stored under `saved_models`. FastAPI loads them at startup. ASP.NET Core calls the JSON endpoints and records successful dashboard operations with model, metric and source metadata where applicable.

## Limitations and required validation

- research metrics are not factory-calibrated metrics;
- 60 observations form an inference window but do not prove seasonal accuracy;
- drift, missing records and process changes can reduce model quality;
- anomaly thresholds may produce false positives or false negatives;
- all outputs are decision support, not autonomous control.

Before production use, obtain approved operational history, perform validation/control totals, reserve a chronological test period, retrain and compare baselines, report MAE/RMSE/MAPE by grade, calibrate anomaly thresholds and approve a monitoring/retraining process.

Detailed factory data requirements are in `../docs/DATASET_REQUIREMENT_INFO.md`.
