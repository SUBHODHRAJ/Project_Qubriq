# QUBRIQ - Team 3

Unified Logistics Operating System.

## Technology Stack

Frontend:
- React 19.3.1
- JavaScript
- Vite 8.3.8
- Three.js 0.187.0

Backend:
- ASP.NET Core Web API
- C# / .NET 10
- Entity Framework Core 10.0.12
- Oracle MySQL Connector/NET 26.10.0
- MySql.EntityFrameworkCore 10.0.12

Database:
- MySQL

## Development URLs

Frontend: http://127.0.0.1:5173
Backend:  http://127.0.0.1:5066
Health:   http://127.0.0.1:5066/api/health
OpenAPI:  http://127.0.0.1:5066/openapi/v1.json

## Start Frontend

cd frontend
npm run dev

## Start Backend

cd backend/Qubriq.API
dotnet run --urls http://127.0.0.1:5066

## Run Automated Tests

powershell -ExecutionPolicy Bypass -File tests/Run-Qubriq-Tests.ps1

## Development Policy

- Business modules are implemented incrementally.
- MySQL schema is introduced in Stage 1.
- LINQ and Entity Framework Core are used for data access.
- JWT authentication is deferred to a later phase.
- Run regression tests after every module.
