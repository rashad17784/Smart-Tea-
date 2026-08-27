[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Destination,

    [string]$AutomatedEvidenceFolder
)

$ErrorActionPreference = "Stop"
$sourceRoot = [System.IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$destinationRoot = [System.IO.Path]::GetFullPath($Destination)
$separator = [System.IO.Path]::DirectorySeparatorChar

if (Test-Path -LiteralPath $destinationRoot) {
    throw "Destination already exists. Refusing to overwrite: $destinationRoot"
}

if ($destinationRoot.StartsWith($sourceRoot + $separator, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "The submission destination must be outside the development workspace."
}

Write-Host "Creating sanitized SmartTea submission copy..." -ForegroundColor Cyan
Write-Host "Source:      $sourceRoot"
Write-Host "Destination: $destinationRoot"

New-Item -ItemType Directory -Path $destinationRoot | Out-Null

$excludedDirectories = @(
    (Join-Path $sourceRoot ".git"),
    (Join-Path $sourceRoot ".github"),
    (Join-Path $sourceRoot ".vs"),
    (Join-Path $sourceRoot "TestEvidence"),
    (Join-Path $sourceRoot "TeaOnlineShop\App_Data"),
    (Join-Path $sourceRoot "SmartTea_AI\smarttea_env"),
    (Join-Path $sourceRoot "SmartTea_AI\data\real_validation"),
    "real_validation",
    "bin",
    "obj",
    "TestResults",
    "__pycache__",
    ".ipynb_checkpoints"
)

$excludedFiles = @(
    "*.pyc", "*.pyo", "*.tmp", "*.bak", "*.log", "*.zip",
    "*.mdf", "*.ldf", "*.ndf", "*.pfx", "*.p12", "*.pem", "*.key",
    "*.user", "*.suo", ".env", ".env.*", "*~",
    "home_snapshot.html", "run_stderr.txt", "run_stdout.txt",
    "run2_stderr.txt", "run2_stdout.txt", "seed data.rtf",
    "synthetic_research_import_90days.csv",
    "synthetic_research_import_FIXED.csv",
    "Train_LSTM_MultiOutput.ipynb"
)

$robocopyArguments = @(
    $sourceRoot,
    $destinationRoot,
    "*",
    "/E",
    "/COPY:DAT",
    "/DCOPY:DAT",
    "/R:1",
    "/W:1",
    "/NFL",
    "/NDL",
    "/NJH",
    "/NJS",
    "/NP",
    "/XD"
) + $excludedDirectories + @("/XF") + $excludedFiles

& robocopy.exe @robocopyArguments | Out-Null
$robocopyExitCode = $LASTEXITCODE
if ($robocopyExitCode -ge 8) {
    throw "Robocopy failed with exit code $robocopyExitCode."
}

$sourceEvidenceRoot = Join-Path $sourceRoot "TestEvidence"
$destinationEvidenceRoot = Join-Path $destinationRoot "TestEvidence"
New-Item -ItemType Directory -Path $destinationEvidenceRoot -Force | Out-Null

if ([string]::IsNullOrWhiteSpace($AutomatedEvidenceFolder)) {
    $selectedAutomatedEvidence = Get-ChildItem -LiteralPath $sourceEvidenceRoot -Directory -Filter "Automated-*" |
        Where-Object {
            $summaryPath = Join-Path $_.FullName "TEST-SUMMARY.md"
            (Test-Path -LiteralPath $summaryPath) -and
            ((Get-Content -LiteralPath $summaryPath -Raw) -match 'Overall result:\s*\*\*PASS\*\*')
        } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
}
else {
    $selectedAutomatedEvidence = Get-Item -LiteralPath (
        Join-Path $sourceEvidenceRoot $AutomatedEvidenceFolder)
}

if (-not $selectedAutomatedEvidence) {
    throw "No passing automated-test evidence folder was found."
}

$selectedSummary = Join-Path $selectedAutomatedEvidence.FullName "TEST-SUMMARY.md"
if ((Get-Content -LiteralPath $selectedSummary -Raw) -notmatch 'Overall result:\s*\*\*PASS\*\*') {
    throw "Selected automated evidence is not marked PASS: $($selectedAutomatedEvidence.Name)"
}

Copy-Item -LiteralPath $selectedAutomatedEvidence.FullName `
    -Destination $destinationEvidenceRoot -Recurse

foreach ($manualFolderName in @("Manual-20260722", "Manual-20260819-FinalRetest")) {
    $manualFolder = Join-Path $sourceEvidenceRoot $manualFolderName
    if (Test-Path -LiteralPath $manualFolder) {
        Copy-Item -LiteralPath $manualFolder -Destination $destinationEvidenceRoot -Recurse
    }
}

$evidenceReadme = Join-Path $sourceEvidenceRoot "README.md"
if (Test-Path -LiteralPath $evidenceReadme) {
    Copy-Item -LiteralPath $evidenceReadme -Destination $destinationEvidenceRoot
}

Write-Host "Auditing exclusions and configuration..." -ForegroundColor Cyan

$forbiddenDirectoryNames = @(
    ".git", ".github", ".vs", "bin", "obj", "TestResults",
    "smarttea_env", "__pycache__", ".ipynb_checkpoints", "development-mail",
    "real_validation"
)
$forbiddenDirectories = Get-ChildItem -LiteralPath $destinationRoot -Directory -Recurse -Force |
    Where-Object { $_.Name -in $forbiddenDirectoryNames }
if ($forbiddenDirectories) {
    throw "Forbidden directories remain: $($forbiddenDirectories.FullName -join ', ')"
}

$forbiddenFilePatterns = @(
    "*.pyc", "*.pyo", "*.tmp", "*.bak", "*.mdf", "*.ldf", "*.ndf",
    "*.pfx", "*.p12", "*.pem", "*.key", "*.user", "*.suo", ".env", ".env.*"
)
$forbiddenFiles = foreach ($pattern in $forbiddenFilePatterns) {
    Get-ChildItem -LiteralPath $destinationRoot -File -Recurse -Force -Filter $pattern
}
if ($forbiddenFiles) {
    throw "Forbidden files remain: $($forbiddenFiles.FullName -join ', ')"
}

$appSettingsFiles = Get-ChildItem -LiteralPath (Join-Path $destinationRoot "TeaOnlineShop") `
    -File -Filter "appsettings*.json"
foreach ($settingsFile in $appSettingsFiles) {
    $settingsText = Get-Content -LiteralPath $settingsFile.FullName -Raw
    if ($settingsText -match '(?i)(Password|Pwd|User Id|Uid)\s*=') {
        throw "A database credential was found in $($settingsFile.FullName)."
    }

    $settings = $settingsText | ConvertFrom-Json
    if ($settings.Email -and -not [string]::IsNullOrWhiteSpace([string]$settings.Email.Password)) {
        throw "An email password was found in $($settingsFile.FullName)."
    }
}

$textExtensions = @(
    ".cmd", ".config", ".cs", ".cshtml", ".ipynb", ".json", ".md",
    ".ps1", ".py", ".sql", ".txt", ".xml", ".yaml", ".yml"
)
$sensitiveMatches = Get-ChildItem -LiteralPath $destinationRoot -File -Recurse |
    Where-Object { $_.Extension -in $textExtensions } |
    Select-String -Pattern 'otpauth://[^\s"''<>]*[?&]secret=[A-Z2-7]{16,}|-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----' -List
if ($sensitiveMatches) {
    throw "MFA provisioning data or a private key remains: $($sensitiveMatches.Path -join ', ')"
}

$manifestPath = Join-Path $destinationRoot "SUBMISSION-SHA256.txt"
$manifestLines = Get-ChildItem -LiteralPath $destinationRoot -File -Recurse |
    Where-Object { $_.FullName -ne $manifestPath } |
    Sort-Object FullName |
    ForEach-Object {
        $relativePath = $_.FullName.Substring($destinationRoot.Length + 1).Replace('\', '/')
        $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
        "$hash *$relativePath"
    }
$manifestLines | Set-Content -LiteralPath $manifestPath -Encoding UTF8

$fileCount = (Get-ChildItem -LiteralPath $destinationRoot -File -Recurse).Count
$sizeBytes = (Get-ChildItem -LiteralPath $destinationRoot -File -Recurse |
    Measure-Object -Property Length -Sum).Sum

Write-Host "PASS: clean submission copy created and audited." -ForegroundColor Green
Write-Host "Automated evidence: $($selectedAutomatedEvidence.Name)"
Write-Host "Files: $fileCount"
Write-Host ("Size: {0:N2} MB" -f ($sizeBytes / 1MB))
Write-Host "SHA-256 manifest: $manifestPath"
