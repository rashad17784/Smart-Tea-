# SmartTeaShop test evidence

**Project author and tester:** Mohamed Rashad

This directory contains the evidence used to support the final testing claims. Raw machine output and screenshots are retained so an examiner can inspect the result rather than relying only on a written statement.

## Authoritative automated run

`Automated-20260819-174215/` contains:

- pinned Python dependency check;
- Release restore/build output;
- isolated clean-database and first-Administrator bootstrap check;
- xUnit output and TRX (34/34 passed);
- rollback-safe SQL integration output;
- public/anonymous web smoke results;
- live AI health, demand, price and anomaly responses;
- the generated test summary.

## Manual evidence

- `Manual-20260722/` — broad system execution: 42 PASS, 0 FAIL and 2 BLOCKED out of 44 scenarios;
- `Manual-20260819-FinalRetest/` — final high-risk regression: 6/6 PASS.

Blocked manual scenarios are reported openly. They required qualifying operational supplier-delivery/QR records that were not available during that execution.

## Evidence integrity

Logs, TRX, JSON, screenshots and exported CSV files are original artefacts. They should not be edited for presentation. Documentation explains them, but does not replace them.

No password, authenticator secret or recovery code should be stored here. The final ZIP checksum is provided beside the submission archive.

See `../docs/TESTING_GUIDE.md` for the strategy, traceability and coverage limitation.
