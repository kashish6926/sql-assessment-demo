# SQL Assessment Demo

A small .NET 8 order application with **real SQL Server data access**. Unlike a
schema-only skeleton, it opens SQL connections, lists products, calls a
transactional stored procedure to place orders, updates inventory and reads a
reporting view. All seed data is synthetic.

This repository includes the application and database initialization script. It does
**not** include SQL Server, .NET, Modernize, credentials or fabricated assessment
results. Supply your own SQL Server instance. Windows authentication is used;
no SQL password or connection string is stored in this project.

Clone the sample before following the steps below:

```powershell
git clone https://github.com/kashish6926/sql-assessment-demo.git
Set-Location .\sql-assessment-demo
```

## 1. Install and prepare SQL Server

Use a Windows development machine. Do not use a production/shared instance for
this demo.

1. Download **SQL Server 2022 Developer** (free for development/testing) or
   **SQL Server Express** from the
   [official SQL Server downloads page](https://www.microsoft.com/en-us/sql-server/sql-server-downloads).
2. Run the installer, select **Database Engine Services**, and create a local
   named instance such as `SQL2022` (Developer) or `SQLEXPRESS` (Express).
   Record the actual instance name.
3. Choose **Windows authentication** and add your Windows user as SQL Server
   administrator for this dedicated development instance. Initialization needs
   permission to create a database. The assessment tool may require broader
   instance-level read access; follow its documented permission requirements.
4. Verify the SQL Server service is running. Optionally install
   [SQL Server Management Studio](https://learn.microsoft.com/en-us/ssms/install/install)
   and connect with Windows authentication.
5. For a local named instance, `localhost\SQL2022` or `localhost\SQLEXPRESS`
   is sufficient. For a remote instance, configure TCP/IP and its fixed port,
   firewall and Windows identity access before running the app. Do not expose
   SQL Server publicly.

The app creates only the database `SqlAssessmentDemo`; it does not drop existing
databases. Re-running initialization preserves existing demo orders and stock.
Use a fresh development instance/database to reproduce the seed state.

The demo accepts the development server's self-signed certificate while
encrypting the connection. Production applications should validate a trusted
server certificate instead.

## 2. Build and run the application

Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
or a later SDK that supports targeting .NET 8. The .NET 8 runtime is also needed
to run the app. Open PowerShell in the cloned project directory:

```powershell
$env:SQL_SERVER = "$env:COMPUTERNAME\SQL2022" # Replace with your actual instance.
dotnet build .\SqlAssessmentDemo.csproj
if ($LASTEXITCODE -ne 0) { throw "Sample build failed." }
dotnet run --no-build -- init
if ($LASTEXITCODE -ne 0) { throw "Database initialization failed." }
dotnet run --no-build -- products
dotnet run --no-build -- order 1 2
dotnet run --no-build -- orders
```

On a fresh database the first product has stock 50. Ordering two desk lamps
creates an order totaling 59.90 and reduces its stock to 48. A request exceeding
available stock fails without creating an order or changing inventory. Use
`dotnet run -- --help` for commands.

Schema: `Customers`, `Products`, `Orders`, `OrderLines`, the `OrderSummary` view
and the `PlaceOrder` stored procedure. Setup is in [Database/Setup.sql](Database/Setup.sql).
The tiny build-property files keep this project independent if extracted inside
another .NET repository.

## 3. Prepare assessment prerequisites separately

For the current offline SQL assessment skill, run Modernize on Windows and
prepare these prerequisites **before** assessment:

- Modernize with the contributed offline SQL assessment skill.
- PowerShell 7 and Azure CLI 2.75.0 or later.
- The Azure CLI `datamigration` extension.
- The SQL assessment package/executable already installed at
  `%LOCALAPPDATA%\Microsoft\SqlAssessment\Downloads\SqlAssessment.Console.csproj\SqlAssessment.exe`.
- A reachable SQL Server instance and an authorized Windows identity.
- Modernize's normal application-assessment prerequisites and Copilot access.

See [Azure CLI installation](https://aka.ms/installazurecli) and the
[SQL assessment package](https://aka.ms/sqlassessmentpackage). The assessment
skill must not install tools, sign in or request credentials.

## 4. Assess the application AND database

First initialize and exercise the app against the SQL instance. Then, from the
cloned project directory:

```powershell
$source = (Get-Location).Path
$config = Join-Path $source 'assessment-config.yaml'
$output = Join-Path (Split-Path $source -Parent) 'SqlAssessmentDemo-assessment'
$instance = $env:SQL_SERVER
if ([string]::IsNullOrWhiteSpace($instance)) { throw "Set SQL_SERVER first." }

modernize assess `
  --source $source `
  --assess-config $config `
  --output-path $output `
  --format html `
  --prompt "Assess this .NET order application and migration readiness for its local/on-premises SQL Server instance. Use sourceType=Local, instanceName=$instance and authMode=Windows. Include the application assessment and offline SQL database readiness assessment."
```

This uses full analysis; it does not disable the built-in application checks.
The source directory must contain the app, not just the SQL script. Never put a
password or connection string in the prompt. The offline assessment is
**instance-scoped**, so other user databases on that instance can also appear.
Use a dedicated demo instance when you need a report containing only this app's
database.

### Expected output and verification

Open the generated versioned aggregate `index.html` under the selected output
path. Open this application's report. It should contain:

- Application assessment findings/facts for the real .NET application.
- An offline SQL assessment tab linking to the tool's native HTML report,
  copied without custom HTML conversion.
- `SqlAssessmentDemo` in the SQL report's assessed database list.

The database results are linked within the app report; SQL issue counts are not
merged into the application's aggregate issue totals. Only the format-matching
SQL report entry point is copied into the final report output; companion native
JSON/HTML files remain in the assessment working directory.

Check both the application results and the SQL tab: an overall successful app
assessment alone does **not** prove the SQL assessment ran. Selection errors,
missing prerequisites, connection/permission problems or unusable SQL output
can leave the SQL tab absent. Resolve the diagnostic and rerun; do not call that
a completed app-plus-database assessment.

No assessment is pre-generated in this repository: a live SQL instance and a successful
Modernize run are required. This synthetic workload is suitable for readiness
testing, not representative performance/SKU sizing.

## Sharing and cleanup

Share the repository URL, not your working folder after running assessments.
Do not include `bin`, `obj`, `.github/modernize`, generated reports, protected
configuration or any credentials. Recipients install their own instance and
replace the example endpoint.

No cleanup runs automatically. If you want to remove demo data, confirm the
endpoint and database name in SSMS and delete **only** `SqlAssessmentDemo`
manually. Never remove other databases or uninstall a shared SQL instance.
