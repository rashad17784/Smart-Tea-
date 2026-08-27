using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TeaOnlineShop.Models.Dbase;

namespace TeaOnlineShop.Services;

public sealed class DemandHistoryResult
{
    public bool Sufficient { get; init; }
    public string Grade { get; init; } = string.Empty;
    public int DaysAvailable { get; init; }
    public int DaysRequired { get; init; }
    public string DataSource { get; init; } = string.Empty;
    public string SourceLabel { get; init; } = string.Empty;
    public string SourceNote { get; init; } = string.Empty;
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public IReadOnlyList<string> Dates { get; init; } = Array.Empty<string>();
    public IReadOnlyList<double> History { get; init; } = Array.Empty<double>();
    public string Message { get; init; } = string.Empty;
}

public sealed record DemandActualObservation(DateOnly Date, double? ActualDemandKg);

public sealed class DemandHistoryService
{
    private readonly TeaOnlineShopContext _context;
    private readonly AiServiceOptions _options;
    private readonly string _contentRoot;
    private readonly ILogger<DemandHistoryService> _logger;

    public DemandHistoryService(
        TeaOnlineShopContext context,
        IOptions<AiServiceOptions> options,
        IWebHostEnvironment environment,
        ILogger<DemandHistoryService> logger)
    {
        _context = context;
        _options = options.Value;
        _contentRoot = environment.ContentRootPath;
        _logger = logger;
    }

    public async Task<DemandHistoryResult> GetOperationalAsync(
        string grade,
        int days,
        CancellationToken cancellationToken = default)
    {
        grade = grade.Trim().ToUpperInvariant();
        days = Math.Clamp(days, 1, 365);
        // Use closed calendar days only; today's still-changing total must never enter a daily forecast input.
        var end = DateTime.UtcNow.Date.AddDays(-1);
        var start = end.AddDays(-(days - 1));
        var verifiedSource = await FindVerifiedImportSourceAsync(grade, start, end, cancellationToken);
        if (verifiedSource is not null)
        {
            var importedDaily = await _context.OperationalInventoryEvents.AsNoTracking()
                .Where(x => x.SourceSystem == verifiedSource && x.TeaGrade == grade && x.IsDemand &&
                            x.SourceOccurredAtUtc >= start && x.SourceOccurredAtUtc < end.AddDays(1))
                .GroupBy(x => x.SourceOccurredAtUtc.Date)
                .Select(group => new { Date = group.Key, Demand = group.Sum(x => x.QuantityKg) })
                .ToDictionaryAsync(x => x.Date, x => x.Demand, cancellationToken);

            var importedValues = new List<double>(days);
            var importedDates = new List<string>(days);
            for (var date = start; date <= end; date = date.AddDays(1))
            {
                importedDates.Add(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                importedValues.Add(Convert.ToDouble(importedDaily.GetValueOrDefault(date)));
            }

            return new DemandHistoryResult
            {
                Sufficient = true,
                Grade = grade,
                DaysAvailable = days,
                DaysRequired = days,
                DataSource = $"verified_import:{verifiedSource}",
                SourceLabel = $"Approved factory export ({verifiedSource})",
                SourceNote = "Daily customer-order and production-usage demand from independently approved, reconciled, immutable operational import events. Source occurrence time is preserved separately from import time.",
                StartDate = DateOnly.FromDateTime(start),
                EndDate = DateOnly.FromDateTime(end),
                Dates = importedDates,
                History = importedValues,
                Message = $"Loaded {days} days of verified {grade} demand from approved {verifiedSource} factory exports."
            };
        }

        var movementTypes = _options.DemandMovementTypes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var query = _context.StockLedgerEntries.AsNoTracking()
            .Where(x => x.InventoryItemId != null &&
                        x.InventoryItem!.Grade == grade &&
                        x.QuantityChange < 0 &&
                        movementTypes.Contains(x.MovementType));

        var oldest = await query
            .Select(x => (DateTime?)x.OccurredAtUtc)
            .MinAsync(cancellationToken);

        var daily = await query
            .Where(x => x.OccurredAtUtc >= start && x.OccurredAtUtc < end.AddDays(1))
            .GroupBy(x => x.OccurredAtUtc.Date)
            .Select(group => new
            {
                Date = group.Key,
                Demand = -group.Sum(x => x.QuantityChange)
            })
            .ToDictionaryAsync(x => x.Date, x => x.Demand, cancellationToken);

        var values = new List<double>(days);
        var dates = new List<string>(days);
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            dates.Add(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            values.Add(Convert.ToDouble(daily.GetValueOrDefault(date)));
        }

        var daysAvailable = AiForecastRules.CalculateCoverageDays(oldest, end, days);
        var sufficient = daysAvailable >= days;
        var movementDescription = movementTypes.Length == 0
            ? "no configured movement types"
            : string.Join(", ", movementTypes);

        return new DemandHistoryResult
        {
            Sufficient = sufficient,
            Grade = grade,
            DaysAvailable = daysAvailable,
            DaysRequired = days,
            DataSource = "operational_sql",
            SourceLabel = "Verified SQL stock ledger",
            SourceNote = $"Daily outbound demand from immutable ledger movements: {movementDescription}.",
            StartDate = DateOnly.FromDateTime(start),
            EndDate = DateOnly.FromDateTime(end),
            Dates = dates,
            History = values,
            Message = sufficient
                ? $"Loaded {days} days of verified {grade} demand from SQL Server."
                : $"Only {daysAvailable} of {days} required days are available for {grade}. Record genuine operational demand over time; the system will not invent missing history."
        };
    }

    public async Task<DemandHistoryResult> GetResearchAsync(
        string grade,
        int days,
        CancellationToken cancellationToken = default)
    {
        grade = grade.Trim().ToUpperInvariant();
        days = Math.Clamp(days, 1, 365);
        var path = GetResearchDatasetPath();

        if (!File.Exists(path))
        {
            _logger.LogWarning("Configured research demand dataset was not found at {DatasetPath}", path);
            return new DemandHistoryResult
            {
                Grade = grade,
                DaysRequired = days,
                DataSource = "synthetic_research_dataset",
                SourceLabel = "Research dataset unavailable",
                SourceNote = "The configured research CSV could not be found.",
                Message = "The configured research demand dataset is unavailable."
            };
        }

        var observations = await ReadResearchObservationsAsync(grade, cancellationToken);
        var ordered = observations.OrderBy(x => x.Date).ToList();
        var inputPool = ordered.Take(Math.Max(0, ordered.Count - 60));
        var selected = inputPool.TakeLast(days).ToList();
        var sufficient = selected.Count == days;
        return new DemandHistoryResult
        {
            Sufficient = sufficient,
            Grade = grade,
            DaysAvailable = selected.Count,
            DaysRequired = days,
            DataSource = "synthetic_research_dataset",
            SourceLabel = "Research dataset",
            SourceNote = "Research dataset used by the trained demand forecasting model. The final 60 observations are reserved for visible holdout comparison.",
            StartDate = selected.Count == 0 ? null : selected[0].Date,
            EndDate = selected.Count == 0 ? null : selected[^1].Date,
            Dates = selected.Select(x => x.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).ToList(),
            History = selected.Select(x => x.Demand).ToList(),
            Message = sufficient
                ? $"Loaded {days} {grade} observations from the research dataset."
                : $"Only {selected.Count} research observations are available; {days} are required."
        };
    }

    public async Task<IReadOnlyList<DemandActualObservation>> GetActualsAsync(
        string grade,
        string dataSource,
        DateOnly startDate,
        int days,
        CancellationToken cancellationToken = default)
    {
        grade = grade.Trim().ToUpperInvariant();
        days = Math.Clamp(days, 1, 365);
        var dates = Enumerable.Range(0, days).Select(startDate.AddDays).ToList();

        if (string.Equals(dataSource, "synthetic_research_dataset", StringComparison.OrdinalIgnoreCase))
        {
            var research = (await ReadResearchObservationsAsync(grade, cancellationToken))
                .ToDictionary(x => x.Date, x => x.Demand);
            return dates.Select(date => new DemandActualObservation(
                date,
                research.TryGetValue(date, out var value) ? value : null)).ToList();
        }

        const string verifiedPrefix = "verified_import:";
        if (dataSource.StartsWith(verifiedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var sourceSystem = dataSource[verifiedPrefix.Length..];
            var importedStartUtc = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var importedEndUtc = importedStartUtc.AddDays(days);
            var importedActuals = await _context.OperationalInventoryEvents.AsNoTracking()
                .Where(x => x.SourceSystem == sourceSystem && x.TeaGrade == grade && x.IsDemand &&
                            x.SourceOccurredAtUtc >= importedStartUtc && x.SourceOccurredAtUtc < importedEndUtc)
                .GroupBy(x => x.SourceOccurredAtUtc.Date)
                .Select(group => new { Date = group.Key, Demand = group.Sum(x => x.QuantityKg) })
                .ToDictionaryAsync(x => DateOnly.FromDateTime(x.Date), x => Convert.ToDouble(x.Demand), cancellationToken);
            var importedToday = DateOnly.FromDateTime(DateTime.UtcNow);
            return dates.Select(date => new DemandActualObservation(
                date, date <= importedToday ? importedActuals.GetValueOrDefault(date) : null)).ToList();
        }

        if (!string.Equals(dataSource, "operational_sql", StringComparison.OrdinalIgnoreCase))
            return dates.Select(date => new DemandActualObservation(date, null)).ToList();

        var movementTypes = _options.DemandMovementTypes
            .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();
        var startUtc = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endUtc = startUtc.AddDays(days);
        var actuals = await _context.StockLedgerEntries.AsNoTracking()
            .Where(x => x.InventoryItemId != null &&
                        x.InventoryItem!.Grade == grade &&
                        x.QuantityChange < 0 &&
                        movementTypes.Contains(x.MovementType) &&
                        x.OccurredAtUtc >= startUtc && x.OccurredAtUtc < endUtc)
            .GroupBy(x => x.OccurredAtUtc.Date)
            .Select(group => new { Date = group.Key, Demand = -group.Sum(x => x.QuantityChange) })
            .ToDictionaryAsync(x => DateOnly.FromDateTime(x.Date), x => Convert.ToDouble(x.Demand), cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return dates.Select(date => new DemandActualObservation(
            date,
            date <= today ? actuals.GetValueOrDefault(date) : null)).ToList();
    }

    private async Task<string?> FindVerifiedImportSourceAsync(
        string grade,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken)
    {
        var intervals = await _context.OperationalDataImportBatches.AsNoTracking()
            .Where(x => x.Status == "Approved" &&
                        x.PublishedEvents.Any(e => e.TeaGrade == grade))
            .Select(x => new
            {
                x.SourceSystem,
                Start = x.SourcePeriodStartUtc,
                End = x.SourcePeriodEndUtc,
                Approved = x.ApprovedAtUtc ?? x.SubmittedAtUtc
            })
            .ToListAsync(cancellationToken);

        return intervals.GroupBy(x => x.SourceSystem, StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                Source = group.Key,
                LatestApproval = group.Max(x => x.Approved),
                Complete = Enumerable.Range(0, (endUtc.Date - startUtc.Date).Days + 1)
                    .Select(offset => startUtc.Date.AddDays(offset))
                    .All(date => group.Any(interval =>
                        interval.Start.Date <= date && interval.End.Date >= date))
            })
            .Where(x => x.Complete)
            .OrderByDescending(x => x.LatestApproval)
            .Select(x => x.Source)
            .FirstOrDefault();
    }

    private string GetResearchDatasetPath()
    {
        var configuredPath = _options.ResearchDemandDatasetPath;
        return Path.GetFullPath(Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(_contentRoot, configuredPath));
    }

    private async Task<List<(DateOnly Date, double Demand)>> ReadResearchObservationsAsync(
        string grade,
        CancellationToken cancellationToken)
    {
        var path = GetResearchDatasetPath();
        if (!File.Exists(path))
            return new List<(DateOnly, double)>();

        var observations = new List<(DateOnly Date, double Demand)>();
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        using var reader = new StreamReader(stream);
        var header = await reader.ReadLineAsync(cancellationToken);
        if (header is null)
            throw new InvalidDataException("The research demand CSV is empty.");

        var columns = header.Split(',');
        var dateIndex = Array.FindIndex(columns, x => x.Equals("Date", StringComparison.OrdinalIgnoreCase));
        var gradeIndex = Array.FindIndex(columns, x => x.Equals("TeaGrade", StringComparison.OrdinalIgnoreCase));
        var demandIndex = Array.FindIndex(columns, x => x.Equals("DemandKg", StringComparison.OrdinalIgnoreCase));
        if (dateIndex < 0 || gradeIndex < 0 || demandIndex < 0)
            throw new InvalidDataException("Research demand CSV must contain Date, TeaGrade and DemandKg columns.");

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            var fields = line.Split(',');
            if (fields.Length <= Math.Max(dateIndex, Math.Max(gradeIndex, demandIndex)) ||
                !fields[gradeIndex].Equals(grade, StringComparison.OrdinalIgnoreCase) ||
                !DateOnly.TryParseExact(fields[dateIndex], "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var date) ||
                !double.TryParse(fields[demandIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out var demand))
                continue;
            observations.Add((date, demand));
        }
        return observations;
    }
}
