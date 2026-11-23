# Stage 1 - MySQL Database Foundation

## 1. Objective

Stage 1 establishes the initial relational database foundation
for the Qubriq logistics platform.

It introduces the initial domain entities, Entity Framework Core
DbContext, MySQL provider configuration, database migration,
and API database connectivity endpoint.

Project root:

S:\Project_Qubriq

Backend:

backend\Qubriq.API

Database:

qubriq_dev

## 2. Technology and Dependencies

- .NET 10
- Entity Framework Core 10.0.12
- MySql.EntityFrameworkCore 10.0.12
- MySql.Data 26.10.0
- MySQL Server 8.0
- dotnet-ef 10.0.12

The versions are recorded in:

docs/development/VERSIONS.txt

## 3. Architecture

The database access flow is:

ASP.NET Core API
    |
    v
Dependency Injection
    |
    v
QubriqDbContext
    |
    v
Entity Framework Core
    |
    v
MySQL Provider
    |
    v
MySQL - qubriq_dev

The API reads the connection string from application configuration.
The development database credential is supplied through .NET User Secrets.

No database password should be stored in appsettings.json or committed
to the Git repository.

## 4. Database Configuration

The project uses the connection string key:

ConnectionStrings:QubriqDatabase

The User Secrets identifier is configured in Qubriq.API.csproj.

The database account used by the application is:

qubriq_app

The database is:

qubriq_dev

The connection configuration is registered in Program.cs using:

GetConnectionString("QubriqDatabase")

The DbContext is registered through dependency injection using
the MySQL Entity Framework provider.

## 5. Database Entities

Eight initial application entities were introduced.

### 5.1 User

Purpose: Represents a Qubriq application user.

Fields:
- Id
- FullName
- Email
- PasswordHash
- IsActive
- CreatedAtUtc
- UpdatedAtUtc

Constraint:
- Email has a unique index.

PasswordHash is a storage field. This stage does not implement
the complete authentication workflow.

### 5.2 Role

Purpose: Represents an application role.

Fields:
- Id
- Name
- Description

Constraint:
- Name has a unique index.

### 5.3 UserRole

Purpose: Connects users and roles using a many-to-many relationship.

Fields:
- UserId
- RoleId
- AssignedAtUtc

Primary key:
- UserId + RoleId

Relationships:
- UserRole references Users.
- UserRole references Roles.

### 5.4 Customer

Purpose: Represents a customer organization.

Fields:
- Id
- CompanyName
- ContactName
- Email
- Phone
- Address
- IsActive
- CreatedAtUtc

### 5.5 Shipment

Purpose: Represents a shipment handled by the logistics platform.

Fields:
- Id
- TrackingNumber
- CustomerId
- Origin
- Destination
- Status
- CreatedAtUtc
- ExpectedDeliveryUtc

Relationships:
- Each shipment belongs to a customer.

Constraints:
- TrackingNumber has a unique index.
- CustomerId has an index.
- Customer deletion is restricted when referenced by shipments.

### 5.6 Vehicle

Purpose: Represents a logistics vehicle.

Fields:
- Id
- RegistrationNumber
- VehicleType
- CapacityKg
- Status

Constraints:
- RegistrationNumber has a unique index.
- CapacityKg uses decimal(12,2).

### 5.7 Driver

Purpose: Represents a logistics driver.

Fields:
- Id
- FullName
- Phone
- LicenseNumber
- Status

Constraint:
- LicenseNumber has a unique index.

### 5.8 ShipmentAssignment

Purpose: Records the assignment of a shipment to a vehicle and driver.

Fields:
- Id
- ShipmentId
- VehicleId
- DriverId
- AssignedAtUtc
- ReleasedAtUtc
- Status

Relationships:
- ShipmentAssignment references Shipment.
- ShipmentAssignment references Vehicle.
- ShipmentAssignment references Driver.

Indexes:
- ShipmentId
- VehicleId
- DriverId

The ReleasedAtUtc field supports recording assignment history.

## 6. Database Relationship Overview

User <-> UserRole <-> Role

Customer
    |
    | one-to-many
    v
Shipment
    |
    | one-to-many
    v
ShipmentAssignment
    |                  |
    v                  v
Vehicle              Driver

ShipmentAssignment also references Shipment directly.

Foreign-key delete behavior is configured explicitly in
QubriqDbContext.cs. The shipment, vehicle, driver, customer,
and role relationships use restrictive deletion where configured.
UserRole has cascading deletion from User and restrictive deletion
from Role.

## 7. Important Backend Files

| File | Responsibility |
|---|---|
| Program.cs | API configuration, dependency injection, endpoints |
| Data/QubriqDbContext.cs | DbSets and entity relationship configuration |
| Models/User.cs | User entity |
| Models/Role.cs | Role entity |
| Models/UserRole.cs | User-role relationship |
| Models/Customer.cs | Customer entity |
| Models/Shipment.cs | Shipment entity |
| Models/Vehicle.cs | Vehicle entity |
| Models/Driver.cs | Driver entity |
| Models/ShipmentAssignment.cs | Assignment entity |

## 8. Migration Files

Migration name:

InitialDatabaseSchema

Migration ID:

20261123172349_InitialDatabaseSchema

Files:
- Migrations/20261123172349_InitialDatabaseSchema.cs
- Migrations/20261123172349_InitialDatabaseSchema.Designer.cs
- Migrations/QubriqDbContextModelSnapshot.cs

The migration was successfully applied to qubriq_dev.

## 9. Created Database Tables

Application tables:

1. Customers
2. Drivers
3. Roles
4. Users
5. Vehicles
6. Shipments
7. UserRoles
8. ShipmentAssignments

EF Core migration tracking table:

9. __EFMigrationsHistory

The migration history contains:

20261123172349_InitialDatabaseSchema

## 10. API Endpoints

### GET /

Purpose:
Returns basic application information.

Verified response fields:
- application
- status
- version
- stage

### GET /api/health

Purpose:
Returns application health information.

Expected:
- status: Healthy
- application: Qubriq API
- stage: 1

### GET /api/database/health

Purpose:
Checks whether the API can connect to MySQL.

Successful response:

```json
{
  "database": "MySQL",
  "status": "Connected",
  "databaseName": "qubriq_dev"
}
```

On connection failure, the endpoint returns HTTP 503.

### GET /openapi/v1.json

Purpose:
Provides the OpenAPI document in the Development environment.

## 11. Setup and Migration Commands

Open PowerShell and navigate to:

```powershell
Set-Location "S:\Project_Qubriq\backend\Qubriq.API"
```

Restore packages:

```powershell
dotnet restore
```

Build:

```powershell
dotnet build
```

Inspect the DbContext:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef dbcontext info
```

Create a migration when the model changes:

```powershell
dotnet ef migrations add MigrationName
```

Apply pending migrations:

```powershell
dotnet ef database update
```

Do not create another initial migration after this migration
has already been applied. Future schema changes should use
new migration names.

## 12. Run the Backend

```powershell
Set-Location "S:\Project_Qubriq\backend\Qubriq.API"

$env:ASPNETCORE_ENVIRONMENT = "Development"

dotnet run --urls "http://127.0.0.1:5080"
```

## 13. Database Verification

Verify application connectivity:

```powershell
Invoke-RestMethod "http://127.0.0.1:5080/api/database/health"
```

Verify tables and migration history using the MySQL client:

```powershell
mysql -u qubriq_app -p -D qubriq_dev -e "SHOW TABLES; SELECT * FROM __EFMigrationsHistory;"
```

Enter the database password locally when prompted.

Never put the password in source code, documentation, command
history, or Git commits.

## 14. Testing

The cumulative test runner is:

tests/Run-Qubriq-Tests.ps1

It retains Stage 0 checks and adds Stage 1 API database connectivity
verification.

Reports are written under:

tests/logs/

Files include:
- latest-test-report.json
- latest-test-report.csv
- frontend_build.log
- backend_build.log
- api_stdout.log
- api_stderr.log

A successful test suite is required before Stage 1 is marked complete.

## 15. Security Scope and Limitations

Implemented:
- MySQL application account
- User Secrets connection configuration
- Entity Framework Core database access
- Initial relational schema
- Foreign-key constraints and indexes

Not implemented in this stage:
- JWT authentication
- Login and registration workflows
- Password hashing and verification services
- Role-based authorization policies
- Business CRUD APIs
- Production deployment
- AWS configuration
- Database seed data

The PasswordHash field alone does not constitute a completed
authentication system.

## 16. Stage 1 Completion Criteria

- [x] Eight entity classes created.
- [x] QubriqDbContext configured.
- [x] MySQL provider configured.
- [x] Connection string stored in User Secrets.
- [x] Initial EF Core migration generated.
- [x] Migration applied successfully.
- [x] Eight application tables verified.
- [x] Migration history verified.
- [x] API root endpoint verified.
- [x] API database health endpoint verified.
- [x] Cumulative automated test suite passed (8/8).
- [x] Final documentation created; Git review pending.

Stage 1 implementation and regression testing are complete. Final Git review and commit remain pending.
Stage 2 requires explicit project-owner approval.
