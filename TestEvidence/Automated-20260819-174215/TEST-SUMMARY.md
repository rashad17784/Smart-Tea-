# SmartTea automated test execution

- Executed: 2026-08-19 17:43:01 +05:30
- Machine: MR
- Overall result: **PASS**
- Line coverage reported by coverlet: **Not collected on this machine**
- Branch coverage reported by coverlet: **Not collected on this machine**

## Passed stages

- Python dependency integrity
- NuGet restore
- Release build
- Isolated clean-install verification
- Automated xUnit tests
- Transactional integration checks
- Live web smoke tests
- Live AI smoke tests

## Failed stages

- None

## Evidence files

- 00-python-environment.log: installed Python package consistency check.
- 01-build.log: Release compilation result.
- 01b-clean-install.log: isolated base schema, EF migration and first-Administrator bootstrap verification.
- 02-xunit.log and SmartTea.Tests.trx: automated unit/policy test results.
- coverage.cobertura.xml: machine-readable code coverage when the optional -CollectCoverage switch is supported by the host security policy.
- 03-integration.log: rollback-safe SQL integration checks.
- 04-web-smoke.json: public route and anonymous Admin protection checks.
- 05-ai-health.json through 10-ai-anomaly-critical.json: live AI service results.

This Windows host uses Smart App Control, which can block coverlet's temporarily instrumented application DLL. Coverage is therefore optional and must not be obtained by weakening endpoint security. The normal TRX test run, integration checks, traceability matrix and manual evidence remain authoritative. Automated coverage is one indicator, not proof that every workflow works.
