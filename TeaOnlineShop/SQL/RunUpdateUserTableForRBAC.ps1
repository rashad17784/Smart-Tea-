# Path to the SQL script
$sqlScriptPath = ".\UpdateUserTableForRBAC.sql"

# Server name (local for localhost)
$serverName = "DESKTOP-MFUBHE8\SQLEXPRESS"

# Database name
$databaseName = "TeaOnlineShop"

# Use Integrated Security (Windows Authentication)
$trustedConnection = $true

# Construct the arguments
$arguments = @(
    "-S", $serverName,
    "-d", $databaseName,
    "-i", $sqlScriptPath
)

# If using Integrated Security, add the -E flag
if ($trustedConnection) {
    $arguments += "-E"
}

# Execute the script using sqlcmd
& sqlcmd $arguments

Write-Host "SQL script executed." 