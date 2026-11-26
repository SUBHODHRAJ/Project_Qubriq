-- Qubriq: add permission for 3D cargo load optimization.
-- Review before execution. This script changes RBAC records only.

START TRANSACTION;

INSERT INTO Permissions (Code, Module, Action, Description)
VALUES (
    'fleet.load_optimization.run',
    'Fleet',
    'RunLoadOptimization',
    'Run 3D cargo load optimization'
)
ON DUPLICATE KEY UPDATE
    Module = VALUES(Module),
    Action = VALUES(Action),
    Description = VALUES(Description);

INSERT INTO RolePermissions
    (RoleId, PermissionId, IsAllowed, AssignedAtUtc)
SELECT
    r.Id,
    p.Id,
    TRUE,
    UTC_TIMESTAMP(6)
FROM Roles r
CROSS JOIN Permissions p
WHERE r.Name IN ('Admin', 'OperationsDispatcher')
  AND p.Code = 'fleet.load_optimization.run'
ON DUPLICATE KEY UPDATE
    IsAllowed = TRUE;

COMMIT;
