START TRANSACTION;
ALTER TABLE `Vehicles` ADD `CargoHeightCm` decimal(12,2) NULL;

ALTER TABLE `Vehicles` ADD `CargoLengthCm` decimal(12,2) NULL;

ALTER TABLE `Vehicles` ADD `CargoWidthCm` decimal(12,2) NULL;

CREATE TABLE `OptimizationRuns` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `ShipmentId` bigint NOT NULL,
    `VehicleId` bigint NOT NULL,
    `CreatedByUserId` bigint NOT NULL,
    `Algorithm` varchar(100) NOT NULL,
    `Status` varchar(40) NOT NULL,
    `IsFeasible` tinyint(1) NOT NULL,
    `VehicleLengthCm` decimal(12,2) NOT NULL,
    `VehicleWidthCm` decimal(12,2) NOT NULL,
    `VehicleHeightCm` decimal(12,2) NOT NULL,
    `VehicleCapacityKg` decimal(12,2) NOT NULL,
    `TotalPackages` int NOT NULL,
    `PlacedPackages` int NOT NULL,
    `UnplacedPackages` int NOT NULL,
    `TotalWeightKg` decimal(12,2) NOT NULL,
    `UsedWeightKg` decimal(12,2) NOT NULL,
    `WeightUtilizationPercent` decimal(8,2) NOT NULL,
    `VolumeUtilizationPercent` decimal(8,2) NOT NULL,
    `Warnings` longtext NULL,
    `CreatedAtUtc` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_OptimizationRuns_Shipments_ShipmentId` FOREIGN KEY (`ShipmentId`) REFERENCES `Shipments` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_OptimizationRuns_Users_CreatedByUserId` FOREIGN KEY (`CreatedByUserId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_OptimizationRuns_Vehicles_VehicleId` FOREIGN KEY (`VehicleId`) REFERENCES `Vehicles` (`Id`) ON DELETE RESTRICT
);

CREATE TABLE `ShipmentPackages` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `ShipmentId` bigint NOT NULL,
    `Description` varchar(250) NOT NULL,
    `PackageCode` varchar(80) NULL,
    `Category` varchar(100) NULL,
    `Quantity` int NOT NULL,
    `LengthCm` decimal(12,2) NOT NULL,
    `WidthCm` decimal(12,2) NOT NULL,
    `HeightCm` decimal(12,2) NOT NULL,
    `WeightKg` decimal(12,2) NOT NULL,
    `IsFragile` tinyint(1) NOT NULL,
    `SpecialHandlingRequirements` varchar(500) NULL,
    `CreatedAtUtc` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ShipmentPackages_Shipments_ShipmentId` FOREIGN KEY (`ShipmentId`) REFERENCES `Shipments` (`Id`) ON DELETE RESTRICT
);

CREATE TABLE `LoadingPlanApprovals` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `OptimizationRunId` bigint NOT NULL,
    `ReviewedByUserId` bigint NOT NULL,
    `Decision` varchar(20) NOT NULL,
    `Comments` varchar(1000) NULL,
    `ReviewedAtUtc` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_LoadingPlanApprovals_OptimizationRuns_OptimizationRunId` FOREIGN KEY (`OptimizationRunId`) REFERENCES `OptimizationRuns` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_LoadingPlanApprovals_Users_ReviewedByUserId` FOREIGN KEY (`ReviewedByUserId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT
);

CREATE TABLE `PackagePlacements` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `OptimizationRunId` bigint NOT NULL,
    `ShipmentPackageId` bigint NOT NULL,
    `UnitNumber` int NOT NULL,
    `PackageDescription` varchar(250) NOT NULL,
    `LengthCm` decimal(12,2) NOT NULL,
    `WidthCm` decimal(12,2) NOT NULL,
    `HeightCm` decimal(12,2) NOT NULL,
    `WeightKg` decimal(12,2) NOT NULL,
    `IsFragile` tinyint(1) NOT NULL,
    `PlacementStatus` varchar(20) NOT NULL,
    `X` decimal(12,2) NULL,
    `Y` decimal(12,2) NULL,
    `Z` decimal(12,2) NULL,
    `Orientation` varchar(60) NULL,
    `UnplacedReason` varchar(500) NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_PackagePlacements_OptimizationRuns_OptimizationRunId` FOREIGN KEY (`OptimizationRunId`) REFERENCES `OptimizationRuns` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_PackagePlacements_ShipmentPackages_ShipmentPackageId` FOREIGN KEY (`ShipmentPackageId`) REFERENCES `ShipmentPackages` (`Id`) ON DELETE RESTRICT
);

CREATE INDEX `IX_LoadingPlanApprovals_OptimizationRunId` ON `LoadingPlanApprovals` (`OptimizationRunId`);

CREATE INDEX `IX_LoadingPlanApprovals_ReviewedAtUtc` ON `LoadingPlanApprovals` (`ReviewedAtUtc`);

CREATE INDEX `IX_LoadingPlanApprovals_ReviewedByUserId` ON `LoadingPlanApprovals` (`ReviewedByUserId`);

CREATE INDEX `IX_OptimizationRuns_CreatedAtUtc` ON `OptimizationRuns` (`CreatedAtUtc`);

CREATE INDEX `IX_OptimizationRuns_CreatedByUserId` ON `OptimizationRuns` (`CreatedByUserId`);

CREATE INDEX `IX_OptimizationRuns_ShipmentId` ON `OptimizationRuns` (`ShipmentId`);

CREATE INDEX `IX_OptimizationRuns_VehicleId` ON `OptimizationRuns` (`VehicleId`);

CREATE INDEX `IX_PackagePlacements_OptimizationRunId` ON `PackagePlacements` (`OptimizationRunId`);

CREATE UNIQUE INDEX `IX_PackagePlacements_OptimizationRunId_ShipmentPackageId_UnitNu~` ON `PackagePlacements` (`OptimizationRunId`, `ShipmentPackageId`, `UnitNumber`);

CREATE INDEX `IX_PackagePlacements_ShipmentPackageId` ON `PackagePlacements` (`ShipmentPackageId`);

CREATE INDEX `IX_ShipmentPackages_ShipmentId` ON `ShipmentPackages` (`ShipmentId`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261124173536_AddLoadOptimizationPersistence', '10.0.12');

COMMIT;

