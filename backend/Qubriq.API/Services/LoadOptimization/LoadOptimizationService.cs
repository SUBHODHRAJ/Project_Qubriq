using Qubriq.API.DTOs.LoadOptimization;

namespace Qubriq.API.Services.LoadOptimization;

public interface ILoadOptimizationService
{
    LoadOptimizationResponse Optimize(LoadOptimizationRequest request);
}

public sealed class LoadOptimizationService : ILoadOptimizationService
{
    private const decimal SupportThreshold = 0.80m;
    private const decimal Epsilon = 0.0001m;

    private sealed class Box
    {
        public string PackageId { get; init; } = "";
        public string? Description { get; init; }
        public int UnitNumber { get; init; }
        public decimal X { get; init; }
        public decimal Y { get; init; }
        public decimal Z { get; init; }
        public decimal Length { get; init; }
        public decimal Width { get; init; }
        public decimal Height { get; init; }
        public decimal Weight { get; init; }
        public bool IsFragile { get; init; }

        public decimal Right => X + Length;
        public decimal Back => Y + Width;
        public decimal Top => Z + Height;
        public decimal Volume => Length * Width * Height;
    }

    private sealed class Unit
    {
        public PackageInputDto Package { get; init; } = new();
        public int UnitNumber { get; init; }
    }

    private readonly record struct Point(decimal X, decimal Y, decimal Z);

    private sealed class Orientation
    {
        public decimal Length { get; init; }
        public decimal Width { get; init; }
        public decimal Height { get; init; }
        public string Name { get; init; } = "";
    }

    public LoadOptimizationResponse Optimize(LoadOptimizationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateRequest(request);

        var units = request.Packages
            .SelectMany(package =>
                Enumerable.Range(1, package.Quantity)
                    .Select(number => new Unit
                    {
                        Package = package,
                        UnitNumber = number
                    }))
            .OrderByDescending(x =>
                x.Package.LengthCm * x.Package.WidthCm * x.Package.HeightCm)
            .ThenByDescending(x => x.Package.WeightKg)
            .ToList();

        var totalWeight = units.Sum(x => x.Package.WeightKg);
        var vehicleVolume =
            request.VehicleLengthCm *
            request.VehicleWidthCm *
            request.VehicleHeightCm;

        var placements = new List<Box>();
        var unplaced = new List<UnplacedPackageDto>();
        var points = new List<Point> { new(0, 0, 0) };

        decimal placedWeight = 0;

        foreach (var unit in units)
        {
            var package = unit.Package;

            if (package.WeightKg > request.MaxPayloadKg)
            {
                AddUnplaced(
                    unplaced,
                    unit,
                    "Package weight exceeds the vehicle's maximum payload.");

                continue;
            }

            if (placedWeight + package.WeightKg > request.MaxPayloadKg)
            {
                AddUnplaced(
                    unplaced,
                    unit,
                    "Insufficient remaining vehicle payload capacity.");

                continue;
            }

            var orientations = GetOrientations(package);
            Box? selected = null;
            Point selectedPoint = default;
            Orientation? selectedOrientation = null;

            var orderedPoints = points
                .Distinct()
                .OrderBy(p => p.Z)
                .ThenBy(p => p.Y)
                .ThenBy(p => p.X)
                .ToList();

            foreach (var point in orderedPoints)
            {
                foreach (var orientation in orientations)
                {
                    var candidate = new Box
                    {
                        PackageId = package.PackageId,
                        Description = package.Description,
                        UnitNumber = unit.UnitNumber,
                        X = point.X,
                        Y = point.Y,
                        Z = point.Z,
                        Length = orientation.Length,
                        Width = orientation.Width,
                        Height = orientation.Height,
                        Weight = package.WeightKg,
                        IsFragile = package.IsFragile
                    };

                    if (!InsideVehicle(candidate, request))
                        continue;

                    if (placements.Any(existing =>
                        Intersects(candidate, existing)))
                        continue;

                    if (!HasStableSupport(candidate, placements))
                        continue;

                    if (WouldLoadOnFragile(candidate, placements))
                        continue;

                    selected = candidate;
                    selectedPoint = point;
                    selectedOrientation = orientation;
                    break;
                }

                if (selected != null)
                    break;
            }

            if (selected == null || selectedOrientation == null)
            {
                AddUnplaced(
                    unplaced,
                    unit,
                    "No valid position found within the vehicle space and placement constraints.");

                continue;
            }

            placements.Add(selected);
            placedWeight += selected.Weight;

            AddPoint(points, new Point(
                selected.Right, selected.Y, selected.Z));

            AddPoint(points, new Point(
                selected.X, selected.Back, selected.Z));

            AddPoint(points, new Point(
                selected.X, selected.Y, selected.Top));
        }

        var usedVolume = placements.Sum(x => x.Volume);

        var volumeUtilization = vehicleVolume > 0
            ? usedVolume / vehicleVolume * 100m
            : 0m;

        var payloadUtilization = request.MaxPayloadKg > 0
            ? placedWeight / request.MaxPayloadKg * 100m
            : 0m;

        var isFeasible = unplaced.Count == 0;

        var response = new LoadOptimizationResponse
        {
            IsFeasible = isFeasible,
            Status = isFeasible
                ? "All packages placed"
                : placements.Count == 0
                    ? "No packages could be placed"
                    : "Partial loading",

            Vehicle = new VehicleSpaceDto
            {
                LengthCm = request.VehicleLengthCm,
                WidthCm = request.VehicleWidthCm,
                HeightCm = request.VehicleHeightCm,
                MaxPayloadKg = request.MaxPayloadKg,
                UsedVolumeCm3 = Round(usedVolume),
                TotalVolumeCm3 = Round(vehicleVolume)
            },

            TotalPackages = units.Count,
            PlacedPackages = placements.Count,
            UnplacedPackages = unplaced.Count,

            TotalCargoWeightKg = Round(totalWeight),
            PlacedCargoWeightKg = Round(placedWeight),
            RemainingPayloadKg = Round(
                request.MaxPayloadKg - placedWeight),

            VolumeUtilizationPercent = Round(volumeUtilization),
            PayloadUtilizationPercent = Round(payloadUtilization),

            Placements = placements.Select(box =>
            {
                var orientation = GetOrientations(
                    units.First(x =>
                        x.Package.PackageId == box.PackageId &&
                        x.UnitNumber == box.UnitNumber).Package)
                    .First(x =>
                        x.Length == box.Length &&
                        x.Width == box.Width &&
                        x.Height == box.Height);

                return new PackagePlacementDto
                {
                    PackageId = box.PackageId,
                    Description = box.Description,
                    UnitNumber = box.UnitNumber,
                    X = Round(box.X),
                    Y = Round(box.Y),
                    Z = Round(box.Z),
                    LengthCm = Round(box.Length),
                    WidthCm = Round(box.Width),
                    HeightCm = Round(box.Height),
                    WeightKg = Round(box.Weight),
                    IsFragile = box.IsFragile,
                    Orientation = orientation.Name
                };
            }).ToList(),

            UnplacedItems = unplaced,

            Warnings = new List<string>
            {
                "This is a heuristic packing result, not a guaranteed global optimum.",
                "Package coordinates use centimetres. X = vehicle length, Y = vehicle width, Z = vertical height.",
                "The support rule requires at least 80% of a raised package's bottom footprint to be supported.",
                "This initial version does not model axle loads, vehicle suspension, tie-downs or unloading sequence."
            }
        };

        return response;
    }

    private static void ValidateRequest(LoadOptimizationRequest request)
    {
        if (request.VehicleLengthCm <= 0 ||
            request.VehicleWidthCm <= 0 ||
            request.VehicleHeightCm <= 0 ||
            request.MaxPayloadKg <= 0)
        {
            throw new ArgumentException(
                "Vehicle dimensions and maximum payload must be positive.");
        }

        if (request.Packages == null || request.Packages.Count == 0)
        {
            throw new ArgumentException(
                "At least one package is required.");
        }

        var totalUnits = 0;
        var packageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var package in request.Packages)
        {
            if (string.IsNullOrWhiteSpace(package.PackageId))
                throw new ArgumentException("Every package requires a PackageId.");

            if (!packageIds.Add(package.PackageId.Trim()))
                throw new ArgumentException(
                    $"Duplicate PackageId '{package.PackageId}'. Each package group must have a unique ID.");

            if (package.LengthCm <= 0 ||
                package.WidthCm <= 0 ||
                package.HeightCm <= 0 ||
                package.WeightKg <= 0)
            {
                throw new ArgumentException(
                    $"Package '{package.PackageId}' has invalid dimensions or weight.");
            }

            if (package.Quantity < 1 || package.Quantity > 200)
                throw new ArgumentException(
                    $"Package '{package.PackageId}' has an invalid quantity.");

            totalUnits += package.Quantity;
        }

        if (totalUnits > 1000)
        {
            throw new ArgumentException(
                "A maximum of 1000 package units is allowed per optimization request.");
        }
    }

    private static List<Orientation> GetOrientations(
        PackageInputDto package)
    {
        var dimensions = new[]
        {
            (package.LengthCm, package.WidthCm, package.HeightCm, "L-W-H"),
            (package.LengthCm, package.HeightCm, package.WidthCm, "L-H-W"),
            (package.WidthCm, package.LengthCm, package.HeightCm, "W-L-H"),
            (package.WidthCm, package.HeightCm, package.LengthCm, "W-H-L"),
            (package.HeightCm, package.LengthCm, package.WidthCm, "H-L-W"),
            (package.HeightCm, package.WidthCm, package.LengthCm, "H-W-L")
        };

        return dimensions
            .Select(x => new Orientation
            {
                Length = x.Item1,
                Width = x.Item2,
                Height = x.Item3,
                Name = x.Item4
            })
            .GroupBy(x => (x.Length, x.Width, x.Height))
            .Select(x => x.First())
            .ToList();
    }

    private static bool InsideVehicle(
        Box box,
        LoadOptimizationRequest request)
    {
        return box.X >= 0 &&
               box.Y >= 0 &&
               box.Z >= 0 &&
               box.Right <= request.VehicleLengthCm + Epsilon &&
               box.Back <= request.VehicleWidthCm + Epsilon &&
               box.Top <= request.VehicleHeightCm + Epsilon;
    }

    private static bool Intersects(Box a, Box b)
    {
        return a.X < b.Right - Epsilon &&
               a.Right > b.X + Epsilon &&
               a.Y < b.Back - Epsilon &&
               a.Back > b.Y + Epsilon &&
               a.Z < b.Top - Epsilon &&
               a.Top > b.Z + Epsilon;
    }

    private static bool HasStableSupport(
        Box candidate,
        List<Box> placements)
    {
        if (candidate.Z <= Epsilon)
            return true;

        var footprintArea = candidate.Length * candidate.Width;
        if (footprintArea <= 0)
            return false;

        decimal supportedArea = 0;

        foreach (var support in placements)
        {
            if (Math.Abs(support.Top - candidate.Z) > Epsilon)
                continue;

            var overlapLength = Math.Max(
                0,
                Math.Min(candidate.Right, support.Right) -
                Math.Max(candidate.X, support.X));

            var overlapWidth = Math.Max(
                0,
                Math.Min(candidate.Back, support.Back) -
                Math.Max(candidate.Y, support.Y));

            supportedArea += overlapLength * overlapWidth;
        }

        return supportedArea / footprintArea >= SupportThreshold;
    }

    private static bool WouldLoadOnFragile(
        Box candidate,
        List<Box> placements)
    {
        return placements.Any(existing =>
            existing.IsFragile &&
            Math.Abs(existing.Top - candidate.Z) <= Epsilon &&
            candidate.X < existing.Right - Epsilon &&
            candidate.Right > existing.X + Epsilon &&
            candidate.Y < existing.Back - Epsilon &&
            candidate.Back > existing.Y + Epsilon);
    }

    private static void AddPoint(List<Point> points, Point point)
    {
        if (point.X < 0 || point.Y < 0 || point.Z < 0)
            return;

        if (!points.Contains(point))
            points.Add(point);
    }

    private static void AddUnplaced(
        List<UnplacedPackageDto> unplaced,
        Unit unit,
        string reason)
    {
        unplaced.Add(new UnplacedPackageDto
        {
            PackageId = unit.Package.PackageId,
            Description = unit.Package.Description,
            UnitNumber = unit.UnitNumber,
            WeightKg = unit.Package.WeightKg,
            Reason = reason
        });
    }

    private static decimal Round(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}

