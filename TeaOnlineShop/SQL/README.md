# SQL utilities

These scripts support a local assessment installation of SmartTeaShop.

## Optional catalogue seed

`QuickSeedTeaOnlineShop.sql` inserts non-sensitive catalogue, navigation and banner records. It does not create users and contains no password. Staff accounts must be created through **Admin > Users & Access** so password-change, MFA and session controls are applied.

```powershell
sqlcmd -S "localhost\SQLEXPRESS" -d TeaOnlineShop -E -C -b `
  -i ".\TeaOnlineShop\SQL\QuickSeedTeaOnlineShop.sql"
```

## Database creation

The root `DBSCRIPT.sql` is the supported clean-database entry point. It creates `TeaOnlineShop` only when missing and refuses to overwrite an existing database. Entity Framework migrations create and upgrade the application schema when ASP.NET Core starts.

## Safety rules

- back up important data before administrative SQL work;
- never add passwords, MFA keys or personal data to seed scripts;
- do not use `DROP DATABASE` as the installation method;
- use Windows authentication locally and managed secrets in production;
- use application workflows for operational records so validation and audit rules run.

See the repository `README.md` for the verified installation procedure.
