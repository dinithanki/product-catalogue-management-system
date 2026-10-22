# Keells Product Catalogue

A full-stack supplier product catalogue developed as part of the Keells Software Engineer Technical Assignment.

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
- [ ] Complete user-management API
- [ ] Product and supplier APIs verification
- [ ] Currency conversion integration
- [ ] React frontend implementation and integration
- [ ] Complete testing and final documentation

## Prerequisites

- .NET SDK compatible with the backend target framework
- Microsoft SQL Server
- Node.js and npm for the frontend
- Git

## Configuration

Configure the database connection string and JWT settings using .NET User Secrets or environment variables.

Do not commit passwords, JWT secrets, API keys, or other credentials to Git.

## Setup

Detailed database, backend, and frontend setup instructions will be added as the implementation progresses.

## Testing

The authentication flow has been tested using Swagger, including login, blocking protected requests when a password change is required, and allowing access after the password is changed.
