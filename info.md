# SmartTeaShop system handbook

**Project author:** Mohamed Rashad  
**Project type:** Final-year software engineering project  
**Purpose:** Tea e-commerce, warehouse traceability and AI-assisted operational decision support

## 1. Project overview

SmartTeaShop brings the main activities of a small tea business into one controlled application. Customers can browse and order products, warehouse staff can receive and dispatch stock, managers can review operational information, and authorised users can use the AI dashboard for forecasting and anomaly checks.

The system is split into three cooperating parts:

1. **ASP.NET Core application** — user interface, identity, permissions and business workflows;
2. **SQL Server database** — users, products, orders, stock movements, audit history and AI result history;
3. **Python FastAPI service** — demand, price and anomaly models.

The supported launcher starts all three parts and checks their health.

## 2. Users and access control

SmartTeaShop uses ASP.NET Core Identity rather than storing plain-text passwords. Identity provides salted password hashing, security stamps, secure sign-in cookies, lockout, password reset tokens and email-confirmation tokens.

The implemented assessment roles are:

- **Administrator** — system governance, staff accounts, configuration, reports, operational imports and full AI access;
- **Factory Manager** — operational oversight, authorised management actions and controlled payment verification;
- **Warehouse Staff** — receiving, stock lookup, small reasoned adjustments, QR lookup, fulfilment and non-sensitive reports;
- **Customer** — public shopping, profile management and access to their own orders only.

Additional permission definitions support future least-privilege roles such as Procurement Officer, Read-only Auditor and AI/System Administrator. The exact matrix is recorded in [docs/ACCESS_CONTROL_MATRIX.md](docs/ACCESS_CONTROL_MATRIX.md).

Security rules include:

- public registration always creates a Customer;
- a Customer cannot select an internal role;
- staff accounts are created by an Administrator;
- staff must change the temporary password and enrol MFA;
- role/status changes revoke active sessions;
- administrative controllers use authorisation policies;
- sign-in and security administration events are auditable.

## 3. Customer and order workflow

A customer can register, confirm their email, sign in, manage their profile, browse products, add available quantities to the cart, check out and review their own order history.

Stock validation is performed on the server during checkout. Browser limits improve usability, but they are not treated as the security control. If availability changes or a request is altered, the server rejects an invalid quantity rather than allowing overselling.

After an order is accepted, inventory is committed. Warehouse Staff can verify the immutable order lines, delivery address and dispatch information. Dispatch records the carrier, tracking reference, verification note, user and time. The Factory Manager can record the controlled cash-on-delivery settlement. Status and payment transitions remain visible in their audit histories.

## 4. Warehouse and inventory workflow

The warehouse module supports inventory master items, warehouses and bins, supplier/delivery lookup, stock receipt, reasoned stock operations, committed stock, low-stock review, QR lookup, transaction history, an immutable ledger, reconciliation and cross-system integrity checks.

The integrity page compares product availability, aggregate item stock and warehouse-location balances. The final manual retest recorded four ledger entries, three location balances, three product mappings and zero reported issues.

Historical factory import is deliberately separate from today's stock balance. An approved historical batch supplies verified observations for analytics and AI; it does not silently rewrite current stock. The process is documented in [docs/OPERATIONAL_DATA_IMPORT.md](docs/OPERATIONAL_DATA_IMPORT.md).

## 5. AI decision-support features

### Demand forecasting

The demand module forecasts BOP, BOPF, DUST, FNGS and OP demand. The deployed LSTM uses 60 ordered daily observations and produces a direct multi-step forecast. The interface exposes 30-, 45- and 60-day periods, charts, tables, comparison information, history and PDF export.

The included model is evaluated on the project research dataset. Its research metrics must not be presented as proven factory performance. A factory claim requires genuine operational history, chronological testing and retraining. See [docs/DATASET_REQUIREMENT_INFO.md](docs/DATASET_REQUIREMENT_INFO.md).

### Green-leaf price forecasting

The price module accepts current and previous price, rainfall, temperature and a forecast period. It provides tomorrow and multi-day results, trend direction, summary values and a forecast table. The dashboard uses operator-entered and derived features; it is not connected to a live auction or weather feed.

### Anomaly detection

Anomaly detection combines an Isolation Forest with visible operational rules. It returns NORMAL, WARNING or CRITICAL, together with a score, message and triggered reasons. A model-only warning requires a sufficiently strong score, reducing weak statistical false positives. Results are retained in alert history. Thresholds still require factory calibration.

### AI integration

ASP.NET Core sends JSON requests to FastAPI and converts the responses into typed dashboard data. Health status and controlled service errors are shown to the user. Successful operations are stored with model/source metadata where applicable.

## 6. Operational-data governance

Factory-history import follows an evidence-first process: upload the controlled CSV, declare source/period/control totals, preserve original bytes and SHA-256, validate dates/codes/units/references, reject duplicates, reconcile totals, require independent approval and publish only an approved batch.

Files identified as research, sample, demonstration or mock data are rejected from verified operational history. This protects the distinction between model research and real factory evidence.

## 7. Testing and evidence

The project uses xUnit tests, rollback-safe SQL integration checks, web security smoke checks, live AI calls, manual system tests and a final regression retest.

The latest authoritative automated run passed all stages and 34 of 34 xUnit tests. The broad manual execution recorded 42 passed scenarios and two blocked external-data scenarios; a later six-test final regression suite passed all six tests. Blocked tests are not disguised as passes. Details are in [docs/TESTING_GUIDE.md](docs/TESTING_GUIDE.md) and `TestEvidence`.

## 8. Current limitations

- real SMTP and a production payment gateway are not configured;
- procurement purchase orders and supplier risk scoring are not complete end-to-end modules;
- production batch/yield management is not a full manufacturing execution system;
- AI models require factory-specific retraining, calibration and drift monitoring;
- production hosting requires HTTPS, managed secrets, central logging, backups and monitoring;
- code coverage was not collected because endpoint protection blocked instrumentation; security was not weakened to manufacture a percentage.

These limits are recorded in [docs/FEATURE_READINESS.md](docs/FEATURE_READINESS.md).

## 9. Start and verify

```powershell
.\Start-SmartTea.cmd
```

With the system running, use a second terminal for automated verification:

```powershell
.\Run-SmartTea-Tests.cmd
```

Clean installation and secure Administrator bootstrap are explained in [README.md](README.md).
