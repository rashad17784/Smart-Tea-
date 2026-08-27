# Final-change manual regression results

**Project:** SmartTeaShop  
**Project author:** Mohamed Rashad  
**Tester:** Mohamed Rashad  
**Execution date:** 19 August 2026  
**Environment:** Local Windows / SQL Server Express / ASP.NET Core / Python AI API

This focused regression was completed after the final installation, security, provenance and submission-hardening changes. It supplements the automated evidence with direct observation of authenticated pages.

| ID | Result | Evidence | Observation |
|---|---|---|---|
| FRT-01 | PASS | `FRT-01-startup.png` | The complete launcher started SQL connectivity, AI API and ASP.NET Core while preserving the existing database. |
| FRT-02 | PASS | `FRT-02-demand-page.png` | An authorised user opened Demand Forecast and saw the expected grade, period and generation controls. |
| FRT-03 | PASS | `FRT-03a-demand-30.png`, `FRT-03b-demand-45.png`, `FRT-03c-demand-60.png` | BOP forecasts completed for 30, 45 and 60 days with the selected output lengths. |
| FRT-04 | PASS | `FRT-04-research-rejected.png` | A clearly identified research file was stopped from entering verified operational history. |
| FRT-05 | PASS | `FRT-05-import-audit.png`, `FRT-05-errors.csv` | The failed batch retained control totals, validation codes, reasons and downloadable error evidence; no records were published. |
| FRT-06 | PASS | `FRT-06-inventory-integrity.png` | The integrity check reported zero issues across the checked ledger, balances and product mappings. |

## Overall result

**PASS — 6 of 6 tests passed.**

No passwords, MFA secrets or recovery codes are included in this evidence folder.
