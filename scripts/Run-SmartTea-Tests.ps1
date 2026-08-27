[CmdletBinding()]
param(
    [string]$WebBaseUrl = "http://localhost:5255",
    [string]$AiBaseUrl = "http://127.0.0.1:8000",
    [switch]$CollectCoverage
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Net.Http
$root = Split-Path -Parent $PSScriptRoot
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$evidenceDir = Join-Path $root "TestEvidence\Automated-$timestamp"
New-Item -ItemType Directory -Path $evidenceDir -Force | Out-Null

$failures = New-Object System.Collections.Generic.List[string]
$passes = New-Object System.Collections.Generic.List[string]

function Invoke-DotnetStep {
    param(
        [string]$Name,
        [string[]]$Arguments,
        [string]$LogName
    )

    Write-Host "`n=== $Name ===" -ForegroundColor Cyan
    # Windows PowerShell can promote native stderr output (including normal xUnit
    # progress messages) to a terminating error when ErrorActionPreference is
    # Stop. Temporarily allow the native process to finish, then trust its exit
    # code and the TRX file as the authoritative result.
    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    & dotnet @Arguments 2>&1 | Tee-Object -FilePath (Join-Path $evidenceDir $LogName)
    $exitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousPreference
    if ($exitCode -eq 0) {
        $passes.Add($Name)
    }
    else {
        $failures.Add("$Name (exit code $exitCode)")
    }
}

function Save-Json {
    param([object]$Value, [string]$FileName)
    $Value | ConvertTo-Json -Depth 20 | Set-Content -Path (Join-Path $evidenceDir $FileName) -Encoding UTF8
}

function Invoke-JsonPost {
    param([string]$Path, [object]$Body, [string]$EvidenceFile)
    $client = New-Object System.Net.Http.HttpClient
    $client.Timeout = [TimeSpan]::FromSeconds(60)
    $content = New-Object System.Net.Http.StringContent(
        ($Body | ConvertTo-Json -Depth 10),
        [System.Text.Encoding]::UTF8,
        "application/json")
    try {
        $httpResponse = $client.PostAsync("$AiBaseUrl$Path", $content).GetAwaiter().GetResult()
        $bytes = $httpResponse.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
        $json = [System.Text.Encoding]::UTF8.GetString($bytes)
        if (-not $httpResponse.IsSuccessStatusCode) {
            throw "AI request $Path returned HTTP $([int]$httpResponse.StatusCode): $json"
        }
        $response = $json | ConvertFrom-Json
        Save-Json $response $EvidenceFile
        return $response
    }
    finally {
        $content.Dispose()
        $client.Dispose()
    }
}

function Invoke-Utf8JsonGet {
    param([string]$Uri)
    $client = New-Object System.Net.Http.HttpClient
    $client.Timeout = [TimeSpan]::FromSeconds(15)
    try {
        $httpResponse = $client.GetAsync($Uri).GetAwaiter().GetResult()
        $bytes = $httpResponse.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
        $json = [System.Text.Encoding]::UTF8.GetString($bytes)
        if (-not $httpResponse.IsSuccessStatusCode) {
            throw "GET $Uri returned HTTP $([int]$httpResponse.StatusCode): $json"
        }
        return $json | ConvertFrom-Json
    }
    finally {
        $client.Dispose()
    }
}

Push-Location $root
try {
    Write-Host "`n=== Python dependency integrity ===" -ForegroundColor Cyan
    $pythonEnvironment = if ([string]::IsNullOrWhiteSpace($env:SMARTTEA_PYTHON_ENV)) {
        Join-Path $env:USERPROFILE ".smarttea\python311"
    }
    else {
        $env:SMARTTEA_PYTHON_ENV
    }
    $pythonPath = Join-Path $pythonEnvironment "Scripts\python.exe"
    if (-not (Test-Path $pythonPath)) {
        "Python environment not found at $pythonPath" |
            Set-Content (Join-Path $evidenceDir "00-python-environment.log") -Encoding UTF8
        $failures.Add("Python dependency integrity: environment not found")
    }
    else {
        & $pythonPath -m pip check 2>&1 |
            Tee-Object -FilePath (Join-Path $evidenceDir "00-python-environment.log")
        if ($LASTEXITCODE -eq 0) {
            $passes.Add("Python dependency integrity")
        }
        else {
            $failures.Add("Python dependency integrity (exit code $LASTEXITCODE)")
        }
    }

    Invoke-DotnetStep "NuGet restore" @(
        "restore", "TeaOnlineShopSn.sln", "-v:minimal"
    ) "00b-restore.log"

    Invoke-DotnetStep "Release build" @(
        "build", "TeaOnlineShopSn.sln", "-c", "Release", "--no-restore", "-v:minimal"
    ) "01-build.log"

    Write-Host "`n=== Isolated clean-install verification ===" -ForegroundColor Cyan
    & powershell.exe -NoProfile -ExecutionPolicy Bypass `
        -File (Join-Path $root "scripts\Test-SmartTea-CleanInstall.ps1") 2>&1 |
        Tee-Object -FilePath (Join-Path $evidenceDir "01b-clean-install.log")
    if ($LASTEXITCODE -eq 0) {
        $passes.Add("Isolated clean-install verification")
    }
    else {
        $failures.Add("Isolated clean-install verification (exit code $LASTEXITCODE)")
    }

    $testArguments = @(
        "test", "TeaOnlineShop.Tests\TeaOnlineShop.Tests.csproj", "-c", "Release", "--no-restore",
        "--logger", "trx;LogFileName=SmartTea.Tests.trx",
        "--results-directory", $evidenceDir
    )
    if ($CollectCoverage) {
        $testArguments += "--collect:XPlat Code Coverage"
    }
    Invoke-DotnetStep "Automated xUnit tests" $testArguments "02-xunit.log"

    Invoke-DotnetStep "Transactional integration checks" @(
        "run", "--project", "TeaOnlineShop.IntegrationChecks\TeaOnlineShop.IntegrationChecks.csproj",
        "-c", "Release", "--no-restore"
    ) "03-integration.log"

    Write-Host "`n=== Live web smoke tests ===" -ForegroundColor Cyan
    try {
        $homeResponse = Invoke-WebRequest -UseBasicParsing -Uri $WebBaseUrl -TimeoutSec 15
        $login = Invoke-WebRequest -UseBasicParsing -Uri "$WebBaseUrl/UserAccount/Login" -TimeoutSec 15
        if ($homeResponse.StatusCode -ne 200 -or $login.StatusCode -ne 200) {
            throw "Unexpected HTTP status. Home=$($homeResponse.StatusCode), Login=$($login.StatusCode)."
        }

        $handler = New-Object System.Net.Http.HttpClientHandler
        $handler.AllowAutoRedirect = $false
        $client = New-Object System.Net.Http.HttpClient($handler)
        try {
            $adminResponse = $client.GetAsync("$WebBaseUrl/Admin").GetAwaiter().GetResult()
            $adminCode = [int]$adminResponse.StatusCode
            if ($adminCode -notin @(302, 401, 403)) {
                throw "Anonymous Admin request returned HTTP $adminCode; expected redirect or denial."
            }
            @{
                HomeStatus = $homeResponse.StatusCode
                LoginStatus = $login.StatusCode
                AnonymousAdminStatus = $adminCode
                AnonymousAdminLocation = if ($adminResponse.Headers.Location) { $adminResponse.Headers.Location.ToString() } else { $null }
                CheckedAt = (Get-Date).ToString("o")
            } | ConvertTo-Json | Set-Content (Join-Path $evidenceDir "04-web-smoke.json") -Encoding UTF8
        }
        finally {
            $client.Dispose()
            $handler.Dispose()
        }
        $passes.Add("Live web smoke tests")
    }
    catch {
        $_ | Out-String | Set-Content (Join-Path $evidenceDir "04-web-smoke-error.log") -Encoding UTF8
        $failures.Add("Live web smoke tests: $($_.Exception.Message)")
    }

    Write-Host "`n=== Live AI smoke tests ===" -ForegroundColor Cyan
    try {
        $health = Invoke-Utf8JsonGet "$AiBaseUrl/health"
        Save-Json $health "05-ai-health.json"
        if ($health.status -ne "healthy") {
            throw "AI health status was '$($health.status)' instead of 'healthy'."
        }

        $demandValues = @(1..60 | ForEach-Object { 280 + ($_ % 7) })
        $demand = Invoke-JsonPost "/predict/demand" @{
            grade = "BOP"
            last_60_days_demand = $demandValues
            horizon_days = 30
        } "06-ai-demand.json"
        if (@($demand.predictions).Count -ne 30) {
            throw "Demand endpoint returned $(@($demand.predictions).Count) predictions instead of 30."
        }

        $priceBody = [ordered]@{
            current_price = 105.0; price_lag1 = 104.5; price_lag2 = 104.0; price_lag3 = 103.5
            price_lag7 = 103.0; price_lag14 = 101.5; rolling_mean7 = 104.5; rolling_mean30 = 104.0
            rolling_std7 = 2.5; price_change_pct = 0.4785; quantity_kg = 4500.0; qty_rolling7 = 4300.0
            firewood_kg = 1200.0; firewood_cost = 18.5; total_cost = 472500.0; temperature = 18.0
            rainfall_mm = 5.0; heavy_rain = 0; supplier_delivered = 1; supplier_qty = 700.0
            promotion = 0; month_start = 0; month = 7; day_of_week = 3; quarter = 3
            is_weekend = 0; day_of_year = 203; day = 22
        }
        $price = Invoke-JsonPost "/predict/price" $priceBody "07-ai-price-tomorrow.json"
        if ($null -eq $price.predicted_price) {
            throw "Tomorrow-price endpoint did not return predicted_price."
        }

        $multiPriceBody = [ordered]@{}
        foreach ($key in $priceBody.Keys) { $multiPriceBody[$key] = $priceBody[$key] }
        $multiPriceBody["horizon_days"] = 7
        $multi = Invoke-JsonPost "/predict/price/multistep" $multiPriceBody "08-ai-price-7-day.json"
        if (@($multi.forecast).Count -ne 7) {
            throw "Multi-step price endpoint returned $(@($multi.forecast).Count) rows instead of 7."
        }

        $normal = Invoke-JsonPost "/predict/anomaly" @{
            grade = "BOP"; demand_kg = 320; stock_level_kg = 5000; price_per_kg = 105
            day_of_week = 3; month = 7; is_weekend = 0
        } "09-ai-anomaly-normal.json"
        $critical = Invoke-JsonPost "/predict/anomaly" @{
            grade = "BOP"; demand_kg = 320; stock_level_kg = 5; price_per_kg = 105
            day_of_week = 3; month = 7; is_weekend = 0
        } "10-ai-anomaly-critical.json"
        if ($normal.severity -ne "NORMAL") {
            throw "Normal anomaly scenario returned '$($normal.severity)'."
        }
        if ($critical.severity -ne "CRITICAL") {
            throw "Critical anomaly scenario returned '$($critical.severity)'."
        }
        $passes.Add("Live AI smoke tests")
    }
    catch {
        $_ | Out-String | Set-Content (Join-Path $evidenceDir "05-ai-smoke-error.log") -Encoding UTF8
        $failures.Add("Live AI smoke tests: $($_.Exception.Message)")
    }

    $lineCoverage = if ($CollectCoverage) { "Not available" } else { "Not collected on this machine" }
    $branchCoverage = if ($CollectCoverage) { "Not available" } else { "Not collected on this machine" }
    $coverageFile = Get-ChildItem $evidenceDir -Recurse -Filter "coverage.cobertura.xml" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($coverageFile) {
        [xml]$coverage = Get-Content $coverageFile.FullName -Raw
        $lineCoverage = "{0:P2}" -f [double]$coverage.coverage.'line-rate'
        $branchCoverage = "{0:P2}" -f [double]$coverage.coverage.'branch-rate'
    }

    $overall = if ($failures.Count -eq 0) { "PASS" } else { "FAIL" }
    $passText = if ($passes.Count -eq 0) { "- None" } else { ($passes | ForEach-Object { "- $_" }) -join "`r`n" }
    $failureText = if ($failures.Count -eq 0) { "- None" } else { ($failures | ForEach-Object { "- $_" }) -join "`r`n" }
    $summary = @"
# SmartTea automated test execution

- Executed: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz")
- Machine: $env:COMPUTERNAME
- Overall result: **$overall**
- Line coverage reported by coverlet: **$lineCoverage**
- Branch coverage reported by coverlet: **$branchCoverage**

## Passed stages

$passText

## Failed stages

$failureText

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
"@
    $summary | Set-Content (Join-Path $evidenceDir "TEST-SUMMARY.md") -Encoding UTF8

    Write-Host "`nEvidence created at:" -ForegroundColor Green
    Write-Host $evidenceDir
    Write-Host "Overall result: $overall" -ForegroundColor $(if ($overall -eq "PASS") { "Green" } else { "Red" })
    if ($failures.Count -gt 0) {
        Write-Host ($failures -join "`n") -ForegroundColor Red
        exit 1
    }
}
finally {
    Pop-Location
}
