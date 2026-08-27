@echo off
setlocal

set "ROOT_DIR=%~dp0"
set "AI_DIR=%ROOT_DIR%SmartTea_AI"
if defined SMARTTEA_PYTHON_ENV (
    set "AI_ENV=%SMARTTEA_PYTHON_ENV%"
) else (
    set "AI_ENV=%USERPROFILE%\.smarttea\python311"
)
set "AI_PYTHON=%AI_ENV%\Scripts\python.exe"
set "AI_SCRIPT=%AI_DIR%\smarttea_ai_api.py"
set "AI_REQUIREMENTS=%AI_DIR%\requirements.txt"
set "DB_SCRIPT=%ROOT_DIR%DBSCRIPT.sql"

echo [1/2] Checking SQL Server and the TeaOnlineShop database...
where sqlcmd.exe >nul 2>&1
if errorlevel 1 (
    echo ERROR: sqlcmd.exe is not installed or is not available on PATH.
    exit /b 1
)

sc.exe query "MSSQL$SQLEXPRESS" | findstr /C:"RUNNING" >nul 2>&1
if errorlevel 1 (
    echo SQL Server Express is stopped. Attempting to start it...
    net.exe start "MSSQL$SQLEXPRESS" >nul 2>&1
    if errorlevel 1 (
        echo ERROR: SQL Server Express could not be started.
        echo Open an Administrator terminal and run:
        echo     Start-Service 'MSSQL$SQLEXPRESS'
        exit /b 1
    )
)

sqlcmd.exe -S "localhost\SQLEXPRESS" -d TeaOnlineShop -E -C -l 5 -b -Q "SET NOCOUNT ON; SELECT 1;" >nul 2>&1
if errorlevel 1 (
    echo       TeaOnlineShop database was not found. Creating a clean database...
    if not exist "%DB_SCRIPT%" (
        echo ERROR: The safe database initialization script was not found:
        echo        %DB_SCRIPT%
        exit /b 1
    )
    sqlcmd.exe -S "localhost\SQLEXPRESS" -d master -E -C -l 30 -b -i "%DB_SCRIPT%"
    if errorlevel 1 (
        echo ERROR: The TeaOnlineShop database could not be initialized.
        echo        No existing database was deleted or overwritten.
        exit /b 1
    )
    sqlcmd.exe -S "localhost\SQLEXPRESS" -d TeaOnlineShop -E -C -l 5 -b -Q "SET NOCOUNT ON; SELECT 1;" >nul 2>&1
    if errorlevel 1 (
        echo ERROR: Database initialization completed but TeaOnlineShop cannot be opened.
        exit /b 1
    )
)
echo       SQL Server and database: READY

echo [2/2] Checking the SmartTea AI API...
curl.exe --silent --fail --max-time 3 http://localhost:8000/health >nul 2>&1
if not errorlevel 1 (
    echo       AI API: READY ^(already running^)
    exit /b 0
)

if not exist "%AI_SCRIPT%" (
    echo ERROR: The SmartTea AI API entrypoint was not found:
    echo        %AI_SCRIPT%
    exit /b 1
)

if not exist "%AI_REQUIREMENTS%" (
    echo ERROR: The pinned Python requirements file was not found:
    echo        %AI_REQUIREMENTS%
    exit /b 1
)

if not exist "%AI_PYTHON%" (
    echo       Creating an isolated Python environment at:
    echo       %AI_ENV%
    echo       This short user-scoped path avoids the Windows package path-length limit.
    if not exist "%AI_ENV%" mkdir "%AI_ENV%"
    if errorlevel 1 (
        echo ERROR: The Python environment directory could not be created.
        exit /b 1
    )
    where py.exe >nul 2>&1
    if not errorlevel 1 (
        py.exe -3.11 -m venv "%AI_ENV%"
    ) else (
        where python.exe >nul 2>&1
        if errorlevel 1 (
            echo ERROR: Python 3.11 is not installed or is not available on PATH.
            exit /b 1
        )
        python.exe -m venv "%AI_ENV%"
    )
    if errorlevel 1 (
        echo ERROR: The isolated SmartTea Python environment could not be created.
        exit /b 1
    )
)

"%AI_PYTHON%" -c "import sys; assert sys.version_info[:2] == (3, 11), 'SmartTea requires Python 3.11'" >nul 2>&1
if errorlevel 1 (
    echo ERROR: The SmartTea environment must use Python 3.11.
    echo        Delete "%AI_ENV%" and rerun this launcher after installing Python 3.11.
    exit /b 1
)

echo       Verifying pinned Python dependencies...
"%AI_PYTHON%" -m pip install --disable-pip-version-check --requirement "%AI_REQUIREMENTS%"
if errorlevel 1 (
    echo ERROR: Python dependencies could not be installed from requirements.txt.
    exit /b 1
)

echo       Starting AI API in a minimized service window...
pushd "%AI_DIR%"
start "SmartTea AI API" /min "%AI_PYTHON%" "%AI_SCRIPT%"
popd

set /a AI_WAIT_SECONDS=0
:WAIT_FOR_AI
curl.exe --silent --fail --max-time 3 http://localhost:8000/health >nul 2>&1
if not errorlevel 1 goto AI_READY

set /a AI_WAIT_SECONDS+=1
if %AI_WAIT_SECONDS% GEQ 90 goto AI_TIMEOUT
timeout.exe /t 1 /nobreak >nul
goto WAIT_FOR_AI

:AI_READY
echo       AI API: READY
exit /b 0

:AI_TIMEOUT
echo ERROR: The AI API did not become healthy within 90 seconds.
echo Review the minimized "SmartTea AI API" window for the Python error.
exit /b 1
