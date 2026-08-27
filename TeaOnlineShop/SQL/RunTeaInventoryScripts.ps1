# PowerShell script to run Tea Inventory SQL scripts
# Run this script as an administrator with appropriate database access

param(
    [string]$ServerName = "DESKTOP-MFUBHE8\SQLEXPRESS",
    [string]$DatabaseName = "TeaOnlineShop",
    [string]$Username,
    [string]$Password,
    [switch]$UseWindowsAuth = $true
)

Write-Host "======================================================"
Write-Host "=           Tea Inventory SQL Script Runner          ="
Write-Host "======================================================"
Write-Host ""

$ErrorActionPreference = "Stop"
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition

# List of scripts to run in order
$scripts = @(
    "CreateTeaInventoryTables.sql"
)

try {
    # Import SqlServer module if needed
    if (-not (Get-Module -ListAvailable -Name SqlServer)) {
        Write-Host "SqlServer module not found. Attempting to install..."
        Install-Module -Name SqlServer -Scope CurrentUser -Force
    }
    Import-Module SqlServer

    # Authentication setup
    $params = @{
        ServerInstance = $ServerName
        Database = $DatabaseName
        QueryTimeout = 120
    }

    if (-not $UseWindowsAuth) {
        if (-not $Username -or -not $Password) {
            Write-Host "SQL Server authentication selected but username or password is missing." -ForegroundColor Red
            exit 1
        }
        $params.Username = $Username
        $params.Password = $Password
    }

    # Confirm database connection
    Write-Host "Connecting to database $DatabaseName on $ServerName..."
    try {
        $testQuery = "SELECT @@VERSION AS ServerVersion"
        $result = Invoke-Sqlcmd @params -Query $testQuery
        Write-Host "Connected successfully to:" -ForegroundColor Green
        Write-Host $result.ServerVersion
    }
    catch {
        Write-Host "Failed to connect to database: $_" -ForegroundColor Red
        exit 1
    }

    # Execute scripts
    Write-Host "`nPreparing to run scripts..." -ForegroundColor Cyan
    foreach ($script in $scripts) {
        $scriptFile = Join-Path $scriptPath $script
        
        if (Test-Path $scriptFile) {
            Write-Host "`nRunning script: $script" -ForegroundColor Yellow
            try {
                Invoke-Sqlcmd @params -InputFile $scriptFile
                Write-Host "Script executed successfully." -ForegroundColor Green
            }
            catch {
                Write-Host "Error executing script $script" -ForegroundColor Red
                Write-Host $_.Exception.Message
                exit 1
            }
        }
        else {
            Write-Host "Script file not found: $scriptFile" -ForegroundColor Red
        }
    }

    Write-Host "`nAll scripts executed successfully!" -ForegroundColor Green
}
catch {
    Write-Host "An error occurred: $_" -ForegroundColor Red
}

Write-Host "`nPress any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") 