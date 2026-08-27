@echo off
setlocal
cd /d "%~dp0"
echo SmartTea automated verification
echo ==============================
echo.
echo Keep Start-SmartTea.cmd running before starting this test.
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Run-SmartTea-Tests.ps1"
set "RESULT=%ERRORLEVEL%"
echo.
if "%RESULT%"=="0" (
    echo TEST RESULT: PASS
) else (
    echo TEST RESULT: FAIL - review the newest TestEvidence folder.
)
echo.
pause
exit /b %RESULT%

