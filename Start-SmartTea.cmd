@echo off
setlocal

set "ROOT_DIR=%~dp0"
set "PROJECT_DIR=%~dp0TeaOnlineShop"

where dotnet >nul 2>&1
if errorlevel 1 (
    echo ERROR: The .NET SDK was not found on PATH.
    echo Install .NET 8 SDK, reopen the terminal, and try again.
    exit /b 1
)

cd /d "%PROJECT_DIR%"
if errorlevel 1 (
    echo ERROR: Could not open the TeaOnlineShop project directory.
    exit /b 1
)

call "%ROOT_DIR%Start-SmartTea-Dependencies.cmd"
if errorlevel 1 (
    echo.
    echo SmartTeaShop was not started because a required service is unavailable.
    exit /b 1
)

echo.
echo [3/3] Starting SmartTeaShop in the supported Release configuration...
echo Open http://localhost:5255 after the application reports that it is listening.
echo Press Ctrl+C to stop the application.
echo.

dotnet run --configuration Release --launch-profile http
set "APP_EXIT_CODE=%ERRORLEVEL%"

if not "%APP_EXIT_CODE%"=="0" (
    echo.
    echo SmartTeaShop stopped with exit code %APP_EXIT_CODE%.
)

exit /b %APP_EXIT_CODE%
