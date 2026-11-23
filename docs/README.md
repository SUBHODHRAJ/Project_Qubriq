# QUBRIQ - Project Documentation

Unified Logistics Operating System.

## Project

Qubriq is being developed as a modular logistics operating platform.
Development is organized into stages, with each stage documented and
verified before formal completion.

## Technology Stack

### Frontend
- React 19.3.1
- JavaScript
- Vite 8.3.8
- Three.js 0.187.0

### Backend
- ASP.NET Core Web API
- C# / .NET 10
- Entity Framework Core 10.0.12
- LINQ
- Oracle MySQL Connector/NET 26.10.0
- MySql.EntityFrameworkCore 10.0.12

### Database
- MySQL 8.0
- Database name: qubriq_dev

## Development Environment

- Project root: S:\Project_Qubriq
- Backend: backend\Qubriq.API
- Frontend: frontend
- Tests: tests
- Documentation: docs

## Stage Progress

| Stage | Description | Status |
|---|---|---|
| Stage 0 | Initial project foundation and API setup | Completed |
| Stage 1 | MySQL database foundation and EF Core schema | Completed; 8/8 cumulative tests passed |
| Stage 2 | Booking API and Customer Management | Implemented; automated tests not run |

## Module Documentation

- [Stage 1 - Database Foundation](modules/STAGE-1-DATABASE-FOUNDATION.md)
- [Stage 2 - Customer Management](modules/STAGE-2-CUSTOMER-MANAGEMENT.md)
- [Stage 2 - Booking Management API](modules/STAGE-2-BOOKING-API.md)

## Development and Testing

Run the cumulative test suite from the project root:

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\Run-Qubriq-Tests.ps1
```

Default API test address: http://127.0.0.1:5080

The existing cumulative runner covers the previously completed
foundation stages. Stage 2 Customer Management automated tests
have not yet been added or executed.

## Security and Configuration

- Database credentials are stored in .NET User Secrets.
- Do not commit secrets or passwords to Git.
- JWT authentication is deferred to a later development stage.
- Local development is the current deployment target.
- AWS deployment is planned for a later stage.

## Version References

See development/VERSIONS.txt for the locked dependency versions.
