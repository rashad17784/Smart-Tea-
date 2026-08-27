# SmartTeaShop

SmartTeaShop is my final-year software project for managing an online tea shop, warehouse operations and AI-assisted planning in one system. I designed and implemented the project to demonstrate secure access control, traceable stock movement, customer order fulfilment and practical machine-learning integration.

**Author:** Mohamed Rashad  
**Technology:** ASP.NET Core 8, SQL Server Express, Python 3.11 and FastAPI  
**Local web address:** `http://localhost:5255`  
**AI API address:** `http://localhost:8000`

## What the system includes

- a responsive public tea shop, customer registration, email confirmation, cart, checkout and order history;
- ASP.NET Core Identity with password hashing, lockout, password reset, role controls, session revocation and mandatory MFA for staff;
- Administrator, Factory Manager, Warehouse Staff and Customer journeys;
- products, suppliers, deliveries, warehouse locations, stock balances, immutable stock transactions and reconciliation checks;
- controlled order dispatch and cash-on-delivery settlement with audit history;
- demand forecasting, green-leaf price forecasting and anomaly detection through a Python API;
- controlled factory-history import with validation, control totals, duplicate detection, independent approval and provenance protection;
- automated tests, transactional integration checks, live smoke tests and documented manual system testing.

The detailed system description is in [info.md](info.md). Test scope and evidence are explained in [docs/TESTING_GUIDE.md](docs/TESTING_GUIDE.md).

## Requirements

Install these tools on a 64-bit Windows computer:

1. .NET 8 SDK;
2. Python 3.11, including `py.exe`;
3. SQL Server 2022 Express using the instance name `SQLEXPRESS`;
4. SQL Server command-line utilities, including `sqlcmd.exe`;
5. a current web browser.

Check the tools in PowerShell:

```powershell
dotnet --version
py -3.11 --version
sqlcmd -?
```

## First installation

### 1. Open PowerShell in the project folder

```powershell
cd "path\to\SmartTeaShop-Submission"
```

### 2. Provide the first Administrator securely

This step is used only when a clean database has no active Administrator. Choose a private email address and a strong temporary password containing at least 12 characters, uppercase and lowercase letters, a number and a symbol.

```powershell
$env:BootstrapAdmin__Email = "first.admin@example.org"
$env:BootstrapAdmin__FullName = "First System Administrator"
$env:BootstrapAdmin__Password = [System.Net.NetworkCredential]::new(
    "", (Read-Host "Temporary administrator password" -AsSecureString)).Password
```

The password stays in the current PowerShell process. It is not stored in the source code, SQL scripts, configuration files or logs.

### 3. Start the complete system

```powershell
.\Start-SmartTea.cmd
```

The launcher performs the following work:

1. checks SQL Server Express and starts it when permission allows;
2. creates `TeaOnlineShop` only if it does not already exist;
3. lets ASP.NET Core apply the Entity Framework/Identity migrations;
4. creates a Python 3.11 environment under `%USERPROFILE%\.smarttea\python311` when required;
5. installs the pinned packages from `SmartTea_AI\requirements.txt`;
6. starts and checks the AI API;
7. starts the ASP.NET Core application in Release mode.

`DBSCRIPT.sql` is deliberately non-destructive. It refuses to overwrite an existing `TeaOnlineShop` database.

### 4. Finish Administrator enrolment

Open `http://localhost:5255/UserAccount/Login` and sign in with the temporary credentials. The Administrator must then:

1. create a new private password;
2. enrol an authenticator application;
3. verify the six-digit MFA code;
4. store the one-time recovery codes offline.

After this, remove the temporary variables if the PowerShell window is still open:

```powershell
Remove-Item Env:BootstrapAdmin__Email
Remove-Item Env:BootstrapAdmin__FullName
Remove-Item Env:BootstrapAdmin__Password
```

### 5. Optional catalogue setup

On a new database, catalogue records may be entered through the Admin interface. The following optional script inserts only non-sensitive catalogue/navigation data; it does not create a user or contain a password:

```powershell
sqlcmd -S "localhost\SQLEXPRESS" -d TeaOnlineShop -E -C -b `
  -i ".\TeaOnlineShop\SQL\QuickSeedTeaOnlineShop.sql"
```

## Normal startup

After installation, double-click `Start-SmartTea.cmd` or run:

```powershell
.\Start-SmartTea.cmd
```

The launcher reuses the existing database and healthy dependencies. Do not run the web project by itself if the AI functions are required, because the complete launcher also starts the Python API.

## Running the tests

Start the system first. In a second PowerShell window, run:

```powershell
.\Run-SmartTea-Tests.cmd
```

The test runner checks the pinned Python environment, restores and builds the solution, verifies a clean installation, runs xUnit tests, performs rollback-safe SQL checks and calls the live web and AI services. It writes timestamped evidence under `TestEvidence`.

The final verified execution passed 34 of 34 xUnit tests and all integration and smoke-test stages. Code coverage was not collected because Windows Smart App Control blocked temporary instrumentation; endpoint protection was not weakened to obtain a percentage. This limitation is recorded transparently in the evidence.

## Configuration and security

- Local SQL access uses Windows authentication and does not require a committed database password.
- Real SMTP, production database credentials and other secrets must be supplied through environment variables, user secrets or a managed secret store.
- Never submit MFA QR images, authenticator keys, recovery codes, real passwords, development email files or a database backup containing personal data.
- Public registration creates Customer accounts only. Staff accounts are provisioned by an authorised Administrator and require MFA.

## Submission note

The folder is source code and assessment evidence, not a claim that every enterprise integration is already deployed. A production rollout would still require approved infrastructure, HTTPS termination, managed secrets, backups, monitoring, email delivery, operational ownership and factory-specific AI evaluation. These boundaries are documented in [docs/FEATURE_READINESS.md](docs/FEATURE_READINESS.md).
