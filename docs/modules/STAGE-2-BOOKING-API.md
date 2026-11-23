# Stage 2 - Booking Management API

## 1. Overview

The Booking Management API provides backend operations for retrieving
and creating shipment bookings in Qubriq.

It uses the existing Shipment entity and extends it with booking and
cargo information.

This document describes the backend API implementation. It does not
claim that a complete booking frontend has been implemented.

## 2. Technology

- C#
- ASP.NET Core Web API
- Entity Framework Core
- LINQ
- MySQL 8
- Database: qubriq_dev

## 3. Implementation Files

| File | Responsibility |
|---|---|
| Controllers/BookingsController.cs | Booking API endpoints |
| DTOs/CreateBookingRequest.cs | Booking request validation |
| Models/Shipment.cs | Booking and shipment entity |
| Data/QubriqDbContext.cs | Entity configuration |
| Migrations/20261123182313_AddBookingDetails.cs | Booking schema migration |

## 4. API Endpoints

| Method | Endpoint | Purpose |
|---|---|---|
| GET | /api/bookings | Retrieve bookings |
| GET | /api/bookings?status=Confirmed | Filter by status |
| GET | /api/bookings?search=value | Search tracking number or customer |
| GET | /api/bookings/customers | Retrieve active customers |
| POST | /api/bookings | Create a booking |

## 5. Booking List Response

The list endpoint projects shipment information into a booking response
containing:

- Id
- BookingId (tracking number)
- Customer
- Pickup location
- Delivery location
- Cargo type and cargo name
- Weight and package count
- Priority
- Booking date
- Pickup date
- Expected delivery date
- Status

Results are ordered by creation date, newest first.

## 6. Active Customer Selection

The endpoint GET /api/bookings/customers retrieves active customers.

Inactive customers are excluded from the selection list.

The create operation also checks that the selected customer exists
and is active.

## 7. Booking Creation

POST /api/bookings accepts CreateBookingRequest.

The request includes:

### Customer and route
- CustomerId
- Origin
- Destination

### Cargo information
- CargoName
- CargoCategory
- PackageCount
- TotalWeightKg
- LengthCm
- WidthCm
- HeightCm
- IsFragile
- SpecialHandlingRequirements

### Transport information
- Priority
- PickupDateUtc
- ExpectedDeliveryUtc
- DeliveryTimeWindow
- AdditionalInstructions

### Booking status
- Status: Draft or Confirmed

## 8. Validation Rules

- CustomerId must be greater than zero.
- Origin and Destination are required and limited to 250 characters.
- CargoName is required and limited to 200 characters.
- PackageCount must be at least one.
- TotalWeightKg must be greater than zero.
- Optional dimensions must be greater than zero when supplied.
- Priority must be Low, Normal, High or Urgent.
- Pickup date is required.
- Expected delivery cannot be earlier than pickup.
- Status must be Draft or Confirmed.
- Text fields enforce their DTO maximum lengths.

## 9. Tracking Number Generation

The controller generates a tracking number using the following format:

QBR-yyyyMMdd-XXXXXXXX

The final eight characters are derived from a GUID.

## 10. Database Changes

The Shipment entity was extended with nullable booking fields to
preserve compatibility with existing shipment records.

The AddBookingDetails migration adds the booking-related columns.

The EF Core model snapshot was updated with the corresponding model
configuration.

## 11. Data Access

The controller uses Entity Framework Core asynchronous operations.

- AsNoTracking is used for read queries.
- Include loads the associated Customer for booking listings.
- LINQ filters support status and search.
- SaveChangesAsync persists new bookings.

## 12. Verification Status

| Item | Status |
|---|---|
| Booking controller implementation | Implemented |
| Booking request DTO | Implemented |
| Shipment model extension | Implemented |
| EF Core migration | Created and previously applied |
| Booking API automated tests | Not run |
| Stage 2 cumulative tests | Not run |

## 13. Current Scope and Limitations

- Booking listing and creation endpoints are implemented.
- Booking update and cancellation endpoints are not included.
- Booking frontend workflow is not documented as complete.
- Authentication and authorization are deferred.
- Automated verification has not been performed for this stage.

## 14. Completion Note

This document records the Booking Management backend implementation.
It does not represent automated test completion or formal Stage 2
sign-off.
