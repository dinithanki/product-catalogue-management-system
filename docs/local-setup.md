# Local setup guide

This guide takes you from a fresh checkout to the Keells Product Catalogue running on your computer. It covers the SQL Server database, backend API, frontend, local secrets, and first administrator account. The commands below use **Windows PowerShell** and should be run from the repository root unless a step says otherwise.

## 1. Install the required tools

Install these before starting:

- Git, to clone the repository.
- The .NET 10 SDK. The backend targets `net10.0`.
- Node.js 20.19 or newer (or Node.js 22.12 or newer) and npm, for the React frontend dependencies in the lockfile.
- Microsoft SQL Server. SQL Server Express or LocalDB is suitable for a local development database. You can use SQL Server Management Studio (SSMS) to create the database and run the checked-in `.sql` files.
- An ExchangeRate-API account and API key, for product price conversion to LKR.

Open a new PowerShell window after installing the tools and check that they are available:

```powershell
git --version
dotnet --version
node --version
npm --version
```

The .NET version should be 10.x. If `sqlcmd` is installed, you can also check it with `sqlcmd -?`; using SSMS for the database steps below does not require `sqlcmd`.

## 2. Get the project

If you do not already have a checkout, clone the repository and enter its folder:

```powershell
git clone YOUR_REPOSITORY_URL
cd keells-product-catalogue
```

Replace `YOUR_REPOSITORY_URL` with the repository's Git URL. If you already opened this project in your IDE, open a terminal at the folder containing `README.md` and continue there.

## 3. Create the local SQL Server database

Start your SQL Server instance and connect to it in SSMS. Create a database named `KeellsProductCatalogue`:

```sql
CREATE DATABASE KeellsProductCatalogue;
GO
```

In SSMS, open and execute these files **in this order**, connected to the same SQL Server instance. The database must already exist because the scripts select it with `USE KeellsProductCatalogue`.

1. `database/schema/001_create_tables.sql`
2. `database/procedures/001_authentication.sql`
3. `database/procedures/002_user_management.sql`
4. `database/procedures/003_supplier_management.sql`
5. `database/procedures/004_product_management.sql`
6. `database/procedures/005_bootstrap_first_admin.sql`
7. `database/procedures/006_get_user_by_id.sql`
8. `database/procedures/007_change_password.sql`
9. `database/seed/001_seed_data.sql`

The last script adds sample suppliers and products. It is safe to run again; it skips rows that already exist. The database will initially have no users. You will create the first administrator in step 6.

## 4. Configure backend secrets

The backend reads local configuration from .NET User Secrets, which keeps credentials out of tracked files. The project already has a User Secrets ID configured. Run these commands from the repository root.

First, set the SQL Server connection string. Choose the example matching your SQL Server setup and replace the value if your instance or authentication differs:

```powershell
# Default SQL Server instance using Windows Authentication
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=KeellsProductCatalogue;Trusted_Connection=True;TrustServerCertificate=True;" --project backend

# SQL Server Express example (use this instead if your instance is SQLEXPRESS)
# dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.\SQLEXPRESS;Database=KeellsProductCatalogue;Trusted_Connection=True;TrustServerCertificate=True;" --project backend

# LocalDB example (use this instead if you installed LocalDB)
# dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\MSSQLLocalDB;Database=KeellsProductCatalogue;Trusted_Connection=True;TrustServerCertificate=True;" --project backend
```

For SQL Server Authentication, use your SQL login instead of `Trusted_Connection=True`, for example `User ID=YOUR_SQL_LOGIN;Password=YOUR_SQL_PASSWORD;TrustServerCertificate=True;`. Keep the actual password in User Secrets and never commit it.

Generate a private 64-byte JWT signing key and save it as a local secret. This value is created for your development environment; there is no shared project key to look up:

```powershell
$jwtSecret = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
dotnet user-secrets set "Jwt:Secret" $jwtSecret --project backend
Remove-Variable jwtSecret
```

The JWT issuer and audience are non-secret identifiers and have local defaults in `backend/appsettings.example.json`. Set them explicitly only if you want custom local values:

```powershell
dotnet user-secrets set "Jwt:Issuer" "KeellsProductCatalogue" --project backend
dotnet user-secrets set "Jwt:Audience" "KeellsProductCatalogueClient" --project backend
```

Get the ExchangeRate-API key from your ExchangeRate-API account dashboard and save it locally:

```powershell
dotnet user-secrets set "ExchangeRateApi:ApiKey" "PASTE_YOUR_OWN_API_KEY_HERE" --project backend
```

Replace the placeholder with your own key before running the command. If you do not configure this key, the API can start, but retrieving non-empty product results that need currency conversion will fail.

You can review which setting names are present with:

```powershell
dotnet user-secrets list --project backend
```

This prints secret values in your terminal, so do not paste its output into a public issue, chat, screenshot, or commit. User Secrets are for development only and are stored outside the repository.

## 5. Install frontend packages

In a PowerShell terminal at the repository root, install the exact dependency versions recorded in the lockfile:

```powershell
Set-Location frontend
npm ci
```

The checked-in `frontend/.env.example` points the frontend to `http://localhost:5131/api`, which is the backend's configured local HTTP address. If `frontend/.env` does not exist yet, create it from the example:

```powershell
Copy-Item .env.example .env
```

If it already exists, check that `VITE_API_BASE_URL` is set to:

```text
VITE_API_BASE_URL=http://localhost:5131/api
```

The frontend URL is configuration, not a secret. Keep API keys and passwords out of frontend environment files because values prefixed with `VITE_` are included in browser code.

## 6. Create your first administrator

Return to the repository root, then run the one-time setup tool:

```powershell
Set-Location ..
dotnet run --project tools/AdminSetup/AdminSetup.csproj
```

Enter an administrator username and a password of at least 12 characters when prompted. The password input is hidden while typing. The tool hashes the password and creates the initial Admin account only when the Users table is empty. Keep these login details safe. On the first login, the application requires you to change the password.

If setup reports that a user already exists, an administrator has already been created in that database; the bootstrap tool will not create another one.

## 7. Start the backend and frontend

Use two PowerShell terminals, both opened at the repository root.

In terminal 1, start the backend:

```powershell
dotnet run --project backend/backend.csproj --launch-profile http
```

The API listens at `http://localhost:5131`. Open Swagger at [http://localhost:5131/swagger](http://localhost:5131/swagger). The basic API health endpoint is [http://localhost:5131/api/health](http://localhost:5131/api/health), and the database connection check is [http://localhost:5131/api/health/database](http://localhost:5131/api/health/database).

In terminal 2, start the frontend:

```powershell
Set-Location frontend
npm run dev
```

Vite prints the frontend address, normally `http://localhost:5173`. Open that address in your browser, sign in using the administrator credentials, and follow the prompt to change the initial password.

## 8. Try the application

After signing in and changing the initial password, the seeded products and suppliers should appear. The product list converts prices to LKR using ExchangeRate-API. To try authenticated endpoints in Swagger, sign in through the frontend or call `POST /api/auth/login`, copy the returned access token, choose **Authorize** in Swagger, and enter the token as a Bearer token.

The seeded sample records are for local development. You can create additional users, suppliers, and products from the app according to the permissions shown by its interface and API.

## Troubleshooting

| Symptom | What to check |
|---|---|
| Backend says JWT configuration is missing | Set `Jwt:Secret`, `Jwt:Issuer`, and `Jwt:Audience` with `dotnet user-secrets` as above. |
| Backend says database connection string is missing | Set `ConnectionStrings:DefaultConnection` for the `backend` project. |
| Database health endpoint returns 503 | Confirm SQL Server is running, the server/instance in the connection string is correct, the database exists, and the selected authentication mode can connect. Confirm all schema and procedure scripts ran successfully. |
| Admin setup cannot find its connection string | Run it from the repository root and configure User Secrets for `backend` as described above. |
| Admin setup says a user already exists | The database already has a user; bootstrap is deliberately limited to an empty Users table. |
| Frontend cannot reach the API | Confirm the backend is listening on port 5131 and `frontend/.env` contains `VITE_API_BASE_URL=http://localhost:5131/api`. Restart Vite after changing `.env`. |
| Browser reports a CORS or origin error | Run the frontend at `http://localhost:5173`; that origin is allowed by the local defaults. If you change its port, add the new origin under `Cors:FrontendOrigins` in backend User Secrets. |
| Product list fails while loading converted prices | Configure a valid `ExchangeRateApi:ApiKey` and verify your account/key can access the provider endpoint. |

To add a different allowed frontend origin to local backend configuration, for example port 5174, run:

```powershell
dotnet user-secrets set "Cors:FrontendOrigins:0" "http://localhost:5174" --project backend
```

The frontend must also use the API base URL appropriate to the backend address. Restart the backend after changing its configuration.

## Stop or reset local development

Stop either running server with `Ctrl+C` in its terminal. To remove local User Secrets, run `dotnet user-secrets clear --project backend`; this removes all backend secrets for this project on your machine. To reset the sample data and accounts, use a separate empty development database and reapply the setup scripts. Do not delete or reset a database containing data you need to keep.

## Useful commands

Run these from the repository root unless noted otherwise:

```powershell
# Restore and build the backend
dotnet build backend/backend.csproj

# Run backend unit tests
dotnet test backend.Tests/backend.Tests.csproj

# Build the frontend (in the frontend directory)
npm run build

# Lint the frontend (in the frontend directory)
npm run lint
```
