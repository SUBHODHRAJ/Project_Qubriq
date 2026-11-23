# Stage 2 - Customer Management

## 1. Overview

Customer Management is the first implemented frontend module in Stage 2
of the Qubriq logistics platform.

It provides a customer account interface connected to the ASP.NET Core
Web API and MySQL database.

## 2. Technology

### Frontend
- React
- JavaScript
- Vite
- React Router
- CSS

### Backend
- C#
- ASP.NET Core Web API
- Entity Framework Core
- LINQ

### Database
- MySQL 8
- Database: qubriq_dev
- Table: Customers

## 3. Frontend Files

| File | Responsibility |
|---|---|
| src/pages/CustomersPage.jsx | Customer list, form, editing and status controls |
| src/services/customerService.js | HTTP communication with customer API |
| src/styles/customers.css | Customer module and modal styling |
| src/routes/AppRoutes.jsx | Customer page route |
| src/layouts/DashboardLayout.jsx | Dashboard navigation entry |

## 4. Backend Files

| File | Responsibility |
|---|---|
| Controllers/CustomersController.cs | Customer API endpoints |
| Models/Customer.cs | Customer database entity |
| DTOs/CreateCustomerRequest.cs | Customer creation validation |
| DTOs/UpdateCustomerRequest.cs | Customer update validation |
| DTOs/UpdateCustomerStatusRequest.cs | Customer status request |
| Data/QubriqDbContext.cs | Entity Framework customer configuration |

## 5. API Endpoints

| Method | Endpoint | Purpose |
|---|---|---|
| GET | /api/customers | List customers |
| GET | /api/customers?search=value | Search customers |
| GET | /api/customers?isActive=true | Active customer filter |
| GET | /api/customers?isActive=false | Inactive customer filter |
| GET | /api/customers/{id} | Retrieve customer |
| POST | /api/customers | Create customer |
| PUT | /api/customers/{id} | Update customer |
| PATCH | /api/customers/{id}/status | Activate or deactivate |

## 6. Customer Fields

| Field | Validation |
|---|---|
| CompanyName | Required, 2-200 characters |
| ContactName | Optional, maximum 150 |
| Email | Optional, valid email, maximum 254 |
| Phone | Optional, maximum 30 |
| Address | Optional, maximum 500 |
| IsActive | Boolean status |
| CreatedAtUtc | UTC creation timestamp |

Email values are normalized to lowercase and whitespace is trimmed.
The controller checks for an existing email before creating or updating
a customer.

## 7. Implemented Frontend Features

- Customer listing from the actual API.
- Search by customer information.
- Active and inactive status filtering.
- Add Customer modal.
- Edit Customer modal populated with existing details.
- Activate and deactivate actions with confirmation.
- Loading, empty and error states.
- Success and error feedback.
- Responsive modal layout.

## 8. Data Handling

Customer records are retrieved from the MySQL database through the API.

The frontend does not use fabricated customer records.

No DELETE customer endpoint is currently implemented.
Customer status is managed through activation and deactivation.

## 9. Development Configuration

Frontend:
http://127.0.0.1:5173

Backend:
http://127.0.0.1:5080

Frontend API requests use the Vite /api proxy.

## 10. Verification Status

| Verification | Status |
|---|---|
| Frontend production build | Passed |
| Customer API connection | Manually verified |
| Customer listing | Manually verified |
| Add Customer | Manually verified |
| Edit Customer | Manually verified after handler-scope correction |
| Activate/Deactivate | Not independently confirmed |
| Automated Customer API tests | Not run |
| Cumulative test suite | Not run for this stage |

A successful frontend build does not establish that all customer
operations have passed automated tests.

## 11. Current Limitations

- Automated Customer Management tests have not been executed.
- Customer deletion is not available.
- Authentication and role-based authorization are deferred.
- Duplicate email checks are application-level checks; database-level
  unique email enforcement has not been established.

## 12. Completion Note

The Customer Management frontend and API integration have been
implemented.

This document records implementation progress, not automated
verification or formal Stage 2 sign-off.
