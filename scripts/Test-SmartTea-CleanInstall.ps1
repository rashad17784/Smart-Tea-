[CmdletBinding()]
param(
    [string]$SqlInstance = "localhost\SQLEXPRESS",
    [int]$Port = 5266
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$databaseName = "SmartTeaInstallCheck_$(Get-Date -Format 'yyyyMMddHHmmss')_$([Guid]::NewGuid().ToString('N').Substring(0, 6))"
$temporarySql = Join-Path $env:TEMP "$databaseName.sql"
$temporaryStdOut = Join-Path $env:TEMP "$databaseName.stdout.log"
$temporaryStdErr = Join-Path $env:TEMP "$databaseName.stderr.log"
$process = $null

if ($databaseName -notmatch '^SmartTeaInstallCheck_[0-9]{14}_[a-f0-9]{6}$') {
    throw "Refusing to use an unexpected temporary database name."
}

try {
    Write-Host "Creating isolated clean-install database $databaseName..."
    $baseScript = Get-Content (Join-Path $root "DBSCRIPT.sql") -Raw
    $isolatedScript = $baseScript.Replace("TeaOnlineShop", $databaseName)
    [System.IO.File]::WriteAllText(
        $temporarySql,
        $isolatedScript,
        [System.Text.UTF8Encoding]::new($true))

    & sqlcmd.exe -S $SqlInstance -d master -E -C -l 30 -b -i $temporarySql | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "Base database initialization failed with exit code $LASTEXITCODE."
    }

    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:Logging__LogLevel__Default = "Warning"
    $env:Logging__LogLevel__Microsoft_AspNetCore = "Warning"
    $env:ConnectionStrings__DefaultConnection =
        "Server=$SqlInstance;Database=$databaseName;Trusted_Connection=True;TrustServerCertificate=true"
    $env:BootstrapAdmin__Email = "install.check@smarttea.invalid"
    $env:BootstrapAdmin__FullName = "Clean Install Verification"
    $env:BootstrapAdmin__Password = "Tmp!$([Guid]::NewGuid().ToString('N'))aA9"

    $process = Start-Process -FilePath dotnet `
        -ArgumentList "bin\Release\net8.0\TeaOnlineShop.dll --urls http://127.0.0.1:$Port" `
        -WorkingDirectory (Join-Path $root "TeaOnlineShop") `
        -WindowStyle Hidden `
        -RedirectStandardOutput $temporaryStdOut `
        -RedirectStandardError $temporaryStdErr `
        -PassThru

    $ready = $false
    foreach ($attempt in 1..60) {
        if ($process.HasExited) {
            throw "The isolated ASP.NET process exited with code $($process.ExitCode)."
        }

        try {
            $response = Invoke-WebRequest -UseBasicParsing `
                -Uri "http://127.0.0.1:$Port/UserAccount/Login" -TimeoutSec 2
            if ($response.StatusCode -eq 200) {
                $ready = $true
                break
            }
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    }

    if (-not $ready) {
        throw "The isolated clean-install site did not become ready."
    }

    $verificationQuery = @"
SET NOCOUNT ON;
SELECT COUNT(*)
FROM dbo.AspNetUsers AS u
INNER JOIN dbo.AspNetUserRoles AS ur ON ur.UserId = u.Id
INNER JOIN dbo.AspNetRoles AS r ON r.Id = ur.RoleId
WHERE u.Email = N'install.check@smarttea.invalid'
  AND u.EmailConfirmed = 1
  AND u.IsActive = 1
  AND u.RequiresPasswordChange = 1
  AND r.Name = N'Administrator';
"@
    $administratorCount = (& sqlcmd.exe -S $SqlInstance -d $databaseName -E -C -h -1 -W -Q $verificationQuery |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Select-Object -Last 1).Trim()

    if ($administratorCount -ne "1") {
        throw "Secure first-Administrator bootstrap verification failed."
    }

    Write-Host "PASS: base schema, EF migrations and secure first-Administrator bootstrap verified." -ForegroundColor Green
}
finally {
    if ($process -and -not $process.HasExited) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        $process.WaitForExit(10000) | Out-Null
    }

    Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue
    Remove-Item Env:Logging__LogLevel__Default -ErrorAction SilentlyContinue
    Remove-Item Env:Logging__LogLevel__Microsoft_AspNetCore -ErrorAction SilentlyContinue
    Remove-Item Env:ConnectionStrings__DefaultConnection -ErrorAction SilentlyContinue
    Remove-Item Env:BootstrapAdmin__Email -ErrorAction SilentlyContinue
    Remove-Item Env:BootstrapAdmin__FullName -ErrorAction SilentlyContinue
    Remove-Item Env:BootstrapAdmin__Password -ErrorAction SilentlyContinue

    Remove-Item -LiteralPath $temporarySql, $temporaryStdOut, $temporaryStdErr `
        -Force -ErrorAction SilentlyContinue

    if ($databaseName -match '^SmartTeaInstallCheck_[0-9]{14}_[a-f0-9]{6}$') {
        $cleanup = "IF DB_ID(N'$databaseName') IS NOT NULL BEGIN ALTER DATABASE [$databaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$databaseName]; END;"
        & sqlcmd.exe -S $SqlInstance -d master -E -C -l 30 -b -Q $cleanup | Out-Null
    }
}
