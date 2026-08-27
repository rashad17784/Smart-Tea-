# Manual system test execution record

**Project:** SmartTeaShop  
**Tester:** Mohamed Rashad  
**Environment:** Windows, SQL Server Express, ASP.NET Core and Python AI API  
**Execution period:** 22–26 July 2026

## Result summary

| Result | Count |
|---|---:|
| PASS | 42 |
| FAIL | 0 |
| BLOCKED | 2 |
| NOT RUN | 0 |
| **Total** | **44** |

`PASS` means the expected behaviour was observed. `BLOCKED` means the test could not be completed because its required operational data/setup was not available; it is not reported as a pass or hidden as a failure.

## Environment and responsive UI

| ID | Scenario | Result | Evidence |
|---|---|---|---|
| MT-ENV-01 | Start SQL Server, AI API and web application; verify service availability. | PASS | `MT-ENV-01-1.png`, `MT-ENV-01-2.png` |
| MT-UI-01 | Public home/shop layout at desktop width. | PASS | `MT-UI-01.png` |
| MT-UI-02 | Customer pages at mobile/tablet width. | PASS | `MT-UI-02.png` |
| MT-UI-03 | Admin navigation and content at desktop width. | PASS | `MT-UI-03.png` |
| MT-UI-04 | Admin layout at reduced width without loss of core controls. | PASS | `MT-UI-04.png` |

## Authentication and authorisation

| ID | Scenario | Result | Evidence |
|---|---|---|---|
| MT-AUTH-01 | Register a customer; verify the public form cannot select a staff role. | PASS | `MT-AUTH-01.png` |
| MT-AUTH-02 | Confirm a customer email using the generated development confirmation link. | PASS | `MT-AUTH-02.png` |
| MT-AUTH-03 | Sign in with a confirmed active customer account. | PASS | `MT-AUTH-03.png` |
| MT-AUTH-04 | Reject invalid credentials without revealing whether an account exists. | PASS | `MT-AUTH-04.png` |
| MT-AUTH-05 | Start and complete password reset using a protected token. | PASS | `MT-AUTH-05.png` |
| MT-AUTH-06 | Enrol staff MFA and store one-time recovery codes. | PASS | `MT-AUTH-06-1.png`, `MT-AUTH-06-2.png` |
| MT-AUTH-07 | Administrator creates a least-privilege staff account. | PASS | `MT-AUTH-07.png` |
| MT-AUTH-08 | Warehouse Staff sees only authorised operational navigation/actions. | PASS | `MT-AUTH-08.png` |
| MT-AUTH-09 | Role/status administration and session-revocation controls are visible and auditable. | PASS | `MT-AUTH-09.png` |

## Customer order lifecycle

| ID | Scenario | Result | Evidence |
|---|---|---|---|
| MT-ORD-01 | Browse products and open product details. | PASS | `MT-ORD-01.png` |
| MT-ORD-02 | Add an available product to the cart. | PASS | `MT-ORD-02.png` |
| MT-ORD-03 | Prevent cart quantity from exceeding current availability. | PASS | `MT-ORD-03.png` |
| MT-ORD-04 | Complete checkout using cash on delivery. | PASS | `MT-ORD-04.png` |
| MT-ORD-05 | Customer order list and details show the submitted immutable order. | PASS | `MT-ORD-05-1.png`, `MT-ORD-05-2.png` |
| MT-ORD-06 | Warehouse Staff dispatches the order with carrier, tracking and verification evidence. | PASS | `MT-ORD-06.png` |
| MT-ORD-07 | Factory Manager records the controlled COD settlement and audit history. | PASS | `MT-ORD-07.png` |

## Warehouse and inventory

| ID | Scenario | Result | Evidence |
|---|---|---|---|
| MT-WH-01 | View tea inventory and stock details. | PASS | `MT-WH-01.png` |
| MT-WH-02 | Review low-stock information. | PASS | `MT-WH-02.png` |
| MT-WH-03 | Perform a permitted reasoned stock operation. | PASS | `MT-WH-03.png` |
| MT-WH-04 | Export an inventory/transaction report. | PASS | `MT-WH-04.png`, `Exports/MT-WH-04.csv.csv` |
| MT-WH-05 | Review the transaction log. | PASS | `MT-WH-05.png` |
| MT-WH-06 | Review the immutable stock ledger. | PASS | `MT-WH-06.png` |
| MT-WH-07 | Run reconciliation/integrity checks and review the result. | PASS | `MT-WH-07-1.png`, `MT-WH-07-2.png` |
| MT-WH-08 | Receive a genuine supplier delivery into a configured warehouse/bin. | BLOCKED | `MT-WH-08.png` (supplier/receipt precondition unavailable) |
| MT-WH-09 | Verify valid and invalid item/supplier QR events in the scan audit. | BLOCKED | `MT-WH-09.png` (no qualifying scan records available) |

## AI functions

| ID | Scenario | Result | Evidence |
|---|---|---|---|
| MT-AI-01 | AI overview loads and reports API health. | PASS | `MT-AI-01.png` |
| MT-AI-02 | Generate a 30-day demand forecast. | PASS | `MT-AI-02.png` |
| MT-AI-03 | Generate an alternative supported demand period. | PASS | `MT-AI-03.png` |
| MT-AI-04 | Review demand chart/table and export evidence. | PASS | `MT-AI-04-1.png`, `MT-AI-04-2.png` |
| MT-AI-05 | Generate tomorrow and multi-day green-leaf price forecasts. | PASS | `MT-AI-05-1.png`, `MT-AI-05-2.png` |
| MT-AI-06 | Review price summary, trend and forecast table. | PASS | `MT-AI-06.png` |
| MT-AI-07 | Detect a normal operational case. | PASS | `MT-AI-07.png` |
| MT-AI-08 | Detect a warning case. | PASS | `MT-AI-08.png` |
| MT-AI-09 | Detect a critical case with visible reasons. | PASS | `MT-AI-09.png` |
| MT-AI-10 | Review retained anomaly alert history. | PASS | `MT-AI-10.png` |
| MT-AI-11 | Handle an AI-service error without exposing an unhandled application failure. | PASS | `MT-AI-11.png` |

## Operational-data controls

| ID | Scenario | Result | Evidence |
|---|---|---|---|
| MT-DATA-01 | Reject a research file from verified operational history. | PASS | `MT-DATA-01.png` |
| MT-DATA-02 | Show field-level validation/reconciliation findings for a failed batch. | PASS | `MT-DATA-02.png` |
| MT-DATA-03 | Preserve failed-batch audit evidence and prevent publication/approval. | PASS | `MT-DATA-03-1.png`, `MT-DATA-03-2.png` |

## Assessment conclusion

No executed scenario produced an unresolved software failure. Two warehouse scenarios remained blocked by missing qualifying operational records and are reported honestly. A separate final-change regression run on 19 August 2026 passed all six selected high-risk checks; see `../Manual-20260819-FinalRetest/FINAL_CHANGE_MANUAL_RETEST_RESULTS.md`.
