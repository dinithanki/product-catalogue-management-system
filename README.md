# Keells Product Catalogue

A full-stack supplier product catalogue developed as part of the Keells Software Engineer Technical Assignment.

## Deployment and Architecture

The system is deployed as a cloud-hosted, three-tier application. The frontend, backend API, and database are hosted separately to keep their responsibilities independent and simplify deployment and maintenance.

| Component | Technology | Hosting platform |
|---|---|---|
| Frontend | React, TypeScript | Vercel |
| Backend API | ASP.NET Core Web API | Render (Docker container) |
| Database | Microsoft SQL Server | MonsterASP |
| Containerization | Docker | Render |

The React frontend provides the product catalogue interface and communicates with the ASP.NET Core API over HTTP. The API handles business logic, request validation, authentication, and database operations, connecting to SQL Server through its configured connection string.

Deployment configuration is supplied through environment variables and each hosting platform's settings. Configure the backend's database connection string, JWT authentication settings, and other required application settings in Render; configure the frontend API base URL to point to the deployed backend. Database credentials, JWT secrets, and other sensitive values must never be committed to source control.

## Live Demo

- **Frontend:** [Product Catalogue](https://product-catalogue-management-system-three.vercel.app/products)
- **API documentation:** [Swagger UI](https://product-catalogue-management-system-q8xd.onrender.com/swagger/index.html)

## Technology Stack

- **Frontend:** React with TypeScript
- **Backend:** ASP.NET Core Web API (.NET 8 or newer)
- **Data Access:** Dapper
- **Database:** Microsoft SQL Server
- **Authentication:** JWT access tokens with role-based authorization
- **Password Security:** Secure password hashing using ASP.NET Core Identity's password hasher
- **API Documentation:** Swagger / OpenAPI
- **Currency Conversion:** ExchangeRate-API

## Project Structure

```text
keells-product-catalogue/
├── backend/
├── frontend/
├── database/
│   ├── schema/
│   ├── procedures/
│   └── seed/
└── README.md
```

## Documentation

- [Local setup guide](docs/local-setup.md) — step-by-step instructions to configure secrets, create the database and administrator, and run the application locally.
- [Architecture](docs/architecture.md) — application layers, request flow, authentication, and currency conversion.
- [Database design](docs/database-design.md) — schema, constraints, indexes, and stored procedures.

## Implemented Features

### Authentication and Security

- [x] JWT-based authentication
- [x] Login endpoint
- [x] Password hashing and verification
- [x] Authenticated password-change endpoint
- [x] First-login password-change enforcement
- [x] Middleware checks the user's current password-change status in the database
- [x] Role-based authorization support
- [x] SQL Server stored procedures for authentication and user operations
- [x] Swagger configuration for Bearer token authentication

### Database

- [x] SQL Server database configuration
- [x] Database schema and stored procedures
- [x] Initial administrator bootstrap procedure
- [x] Refresh-token storage support

## Project Status

- [x] GitHub repository and project structure
- [x] Backend API setup
- [x] Database schema and stored procedures
- [x] JWT authentication and authorization setup
- [x] First-login password-change enforcement
- [x] User-management, product and supplier APIs
- [x] Currency conversion integration
- [x] React frontend implementation and API integration
- [x] Backend unit tests
- [ ] End-to-end verification against the target SQL Server and ExchangeRate-API account

## Prerequisites

- .NET SDK compatible with the backend target framework
- Microsoft SQL Server
- Node.js and npm for the frontend
- Git

## Configuration

Configure `ConnectionStrings:DefaultConnection`, `Jwt:Secret`, `Jwt:Issuer`,
`Jwt:Audience`, and `ExchangeRateApi:ApiKey` using .NET User Secrets or
environment variables. Configure `Cors:FrontendOrigins` for the deployed
frontend origin.

Do not commit passwords, JWT secrets, API keys, or other credentials to Git.

## Setup

Apply the scripts in `database/schema`, `database/procedures`, and
`database/seed` to the target SQL Server. Run `tools/AdminSetup` once to create
the first administrator. Start the backend and frontend from their respective
directories with `dotnet run` and `npm run dev`.

## Testing

Run `dotnet test backend.Tests/backend.Tests.csproj` for backend tests, and
`npm run build` plus `npm run lint` from `frontend` for frontend checks. Full
login, refresh-token, product, supplier, and currency-conversion flows should
be verified against the configured target services before delivery.
