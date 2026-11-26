START TRANSACTION;

-- ==========================================
-- 1. FOUR PRIMARY ROLES
-- ==========================================

INSERT INTO Roles (Name, Description)
VALUES
('Admin', 'Full platform administration and system control'),
('OperationsDispatcher', 'Logistics operations and dispatch management'),
('BookingCustomer', 'Customer booking and own shipment access'),
('ShipmentRecipient', 'Restricted shipment recipient access')
ON DUPLICATE KEY UPDATE
Description = VALUES(Description);

-- ==========================================
-- 2. PERMISSION CATALOGUE
-- ==========================================

INSERT INTO Permissions (Code, Module, Action, Description)
VALUES
('dashboard.view', 'Dashboard', 'View', 'View permitted dashboard information'),

('users.read', 'Users', 'Read', 'View user accounts'),
('users.create', 'Users', 'Create', 'Create user accounts'),
('users.update', 'Users', 'Update', 'Update user accounts'),
('users.deactivate', 'Users', 'Deactivate', 'Deactivate user accounts'),
('users.assign_role', 'Users', 'AssignRole', 'Assign roles to users'),

('roles.manage', 'AccessControl', 'ManageRoles', 'Manage platform roles'),
('permissions.manage', 'AccessControl', 'ManagePermissions', 'Manage permission definitions'),

('settings.manage', 'Settings', 'Manage', 'Manage system settings'),
('finance.read', 'Finance', 'Read', 'View financial information'),
('finance.manage', 'Finance', 'Manage', 'Manage financial operations'),
('audit.read', 'Audit', 'Read', 'View audit logs'),

('bookings.create', 'Bookings', 'Create', 'Create bookings'),
('bookings.read_all', 'Bookings', 'ReadAll', 'View all bookings'),
('bookings.read_own', 'Bookings', 'ReadOwn', 'View own bookings'),
('bookings.update', 'Bookings', 'Update', 'Update bookings'),
('bookings.cancel', 'Bookings', 'Cancel', 'Cancel bookings'),

('shipments.create', 'Shipments', 'Create', 'Create shipments'),
('shipments.read_all', 'Shipments', 'ReadAll', 'View all shipments'),
('shipments.read_own', 'Shipments', 'ReadOwn', 'View own shipments'),
('shipments.update', 'Shipments', 'Update', 'Update shipment information'),
('shipments.status.update', 'Shipments', 'UpdateStatus', 'Update shipment status'),

('cargo.manage', 'Cargo', 'Manage', 'Manage cargo information and loading'),

('fleet.vehicles.manage', 'Fleet', 'ManageVehicles', 'Manage vehicles'),
('fleet.drivers.manage', 'Fleet', 'ManageDrivers', 'Manage drivers'),
('fleet.assignments.manage', 'Fleet', 'ManageAssignments', 'Assign shipments, drivers and vehicles'),
('fleet.load_optimization.run', 'Fleet', 'RunLoadOptimization', 'Run 3D cargo load optimization'),

('routes.plan', 'Routes', 'Plan', 'Plan shipment routes'),

('compliance.ewaybill.manage', 'Compliance', 'ManageEWayBill', 'Manage E-Way Bill compliance'),

('tracking.view_all', 'Tracking', 'ViewAll', 'View all shipment tracking information'),
('tracking.view_own', 'Tracking', 'ViewOwn', 'View permitted shipment tracking information'),

('exceptions.read', 'Exceptions', 'Read', 'View shipment exceptions'),
('exceptions.manage', 'Exceptions', 'Manage', 'Manage shipment exceptions'),

('delivery.assignments.manage', 'Delivery', 'ManageAssignments', 'Manage delivery assignments'),
('delivery.assigned.read', 'Delivery', 'ReadAssigned', 'View assigned deliveries'),
('delivery.status.update', 'Delivery', 'UpdateStatus', 'Update assigned delivery status'),
('delivery.proof.submit', 'Delivery', 'SubmitProof', 'Submit delivery proof'),
('delivery.incoming.read', 'Delivery', 'ReadIncoming', 'View incoming shipments'),
('delivery.receipt.confirm', 'Delivery', 'ConfirmReceipt', 'Confirm shipment receipt')

ON DUPLICATE KEY UPDATE
Module = VALUES(Module),
Action = VALUES(Action),
Description = VALUES(Description);

-- ==========================================
-- 3. ADMIN: ALL PERMISSIONS
-- ==========================================

INSERT INTO RolePermissions
(RoleId, PermissionId, IsAllowed, AssignedAtUtc)
SELECT
    r.Id,
    p.Id,
    TRUE,
    UTC_TIMESTAMP(6)
FROM Roles r
CROSS JOIN Permissions p
WHERE r.Name = 'Admin'
ON DUPLICATE KEY UPDATE
IsAllowed = TRUE;

-- ==========================================
-- 4. OPERATIONS & DISPATCHER
-- ==========================================

INSERT INTO RolePermissions
(RoleId, PermissionId, IsAllowed, AssignedAtUtc)
SELECT
    r.Id,
    p.Id,
    TRUE,
    UTC_TIMESTAMP(6)
FROM Roles r
JOIN Permissions p
    ON p.Code IN (
        'dashboard.view',
        'bookings.create',
        'bookings.read_all',
        'bookings.update',
        'bookings.cancel',
        'shipments.create',
        'shipments.read_all',
        'shipments.update',
        'shipments.status.update',
        'cargo.manage',
        'fleet.vehicles.manage',
        'fleet.drivers.manage',
        'fleet.assignments.manage',
        'fleet.load_optimization.run',
        'routes.plan',
        'compliance.ewaybill.manage',
        'tracking.view_all',
        'exceptions.read',
        'exceptions.manage',
        'delivery.assignments.manage'
    )
WHERE r.Name = 'OperationsDispatcher'
ON DUPLICATE KEY UPDATE
IsAllowed = TRUE;

-- ==========================================
-- 5. BOOKING CUSTOMER
-- ==========================================

INSERT INTO RolePermissions
(RoleId, PermissionId, IsAllowed, AssignedAtUtc)
SELECT
    r.Id,
    p.Id,
    TRUE,
    UTC_TIMESTAMP(6)
FROM Roles r
JOIN Permissions p
    ON p.Code IN (
        'dashboard.view',
        'bookings.create',
        'bookings.read_own',
        'shipments.read_own',
        'tracking.view_own'
    )
WHERE r.Name = 'BookingCustomer'
ON DUPLICATE KEY UPDATE
IsAllowed = TRUE;

-- ==========================================
-- 6. SHIPMENT RECIPIENT
-- ==========================================

INSERT INTO RolePermissions
(RoleId, PermissionId, IsAllowed, AssignedAtUtc)
SELECT
    r.Id,
    p.Id,
    TRUE,
    UTC_TIMESTAMP(6)
FROM Roles r
JOIN Permissions p
    ON p.Code IN (
        'dashboard.view',
        'delivery.assigned.read',
        'delivery.status.update',
        'delivery.proof.submit',
        'delivery.incoming.read',
        'delivery.receipt.confirm',
        'tracking.view_own'
    )
WHERE r.Name = 'ShipmentRecipient'
ON DUPLICATE KEY UPDATE
IsAllowed = TRUE;

COMMIT;

