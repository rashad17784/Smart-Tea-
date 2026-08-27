using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace TeaOnlineShop.Services;

public sealed record OperationalMovementRule(string CanonicalType, int Direction, bool IsDemand);

public static partial class OperationalDataImportRules
{
    public const int MaximumRows = 10000;
    public const long MaximumFileBytes = 10 * 1024 * 1024;
    public const decimal ReconciliationToleranceKg = 0.0001m;
    public const string NonOperationalProvenanceMessage =
        "NON-OPERATIONAL PROVENANCE: Synthetic, research, demo, sample, mock or toy data " +
        "cannot be certified or published as operational history. Use the explicitly labelled " +
        "Research dataset option on the AI Demand Forecast page instead.";

    public static readonly string[] Headers =
    [
        "SourceSystem", "SourceRecordId", "OriginalTransactionDate", "TeaGrade",
        "ItemCode", "Quantity", "Unit", "TransactionType", "SourceReferenceNumber",
        "SupplierOrProductionReference", "WarehouseCode", "BinCode", "UnitCost", "Reason"
    ];

    private static readonly HashSet<string> Grades =
        new(["BOP", "BOPF", "DUST", "FNGS", "OP"], StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> NonOperationalProvenanceMarkers =
        new(["SYNTHETIC", "RESEARCH", "DEMO", "SAMPLE", "MOCK", "TOY"],
            StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, OperationalMovementRule> Movements =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["SupplierReceipt"] = new("SupplierReceipt", 1, false),
            ["ProductionReceipt"] = new("ProductionReceipt", 1, false),
            ["CustomerReturn"] = new("CustomerReturn", 1, false),
            ["StockIn"] = new("StockIn", 1, false),
            ["OpeningBalance"] = new("OpeningBalance", 1, false),
            ["CustomerOrder"] = new("CustomerOrder", -1, true),
            ["ProductionUsage"] = new("ProductionUsage", -1, true),
            ["Damage"] = new("Damage", -1, false),
            ["StockOut"] = new("StockOut", -1, false)
        };

    public static bool IsAllowedGrade(string? value, out string grade)
    {
        grade = (value ?? string.Empty).Trim().ToUpperInvariant();
        return Grades.Contains(grade);
    }

    public static bool TryGetMovement(string? value, out OperationalMovementRule rule)
        => Movements.TryGetValue((value ?? string.Empty).Trim(), out rule!);

    public static bool TryNormalizeQuantity(decimal quantity, string? unit, out decimal quantityKg)
    {
        quantityKg = 0;
        if (quantity <= 0) return false;

        var normalized = (unit ?? string.Empty).Trim().ToLowerInvariant();
        var multiplier = normalized switch
        {
            "kg" or "kgs" or "kilogram" or "kilograms" => 1m,
            "g" or "gram" or "grams" => 0.001m,
            "t" or "tonne" or "tonnes" or "metrictonne" => 1000m,
            _ => 0m
        };

        if (multiplier == 0) return false;
        quantityKg = decimal.Round(quantity * multiplier, 4, MidpointRounding.AwayFromZero);
        return quantityKg > 0;
    }

    public static bool HasExplicitTimeZone(string? value) =>
        !string.IsNullOrWhiteSpace(value) && ExplicitOffsetRegex().IsMatch(value.Trim());

    public static bool IsValidSourceRecordId(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Trim().Length <= 120 &&
        SourceRecordIdRegex().IsMatch(value.Trim());

    public static bool IsSpreadsheetFormula(string? value)
    {
        var candidate = (value ?? string.Empty).TrimStart();
        return candidate.Length > 0 && candidate[0] is '=' or '+' or '-' or '@';
    }

    public static bool IsClearlyNonOperationalSource(params string?[] values)
    {
        foreach (var value in values.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            var tokens = Regex.Split(value!.ToUpperInvariant(), @"[^A-Z0-9]+")
                .Where(x => x.Length > 0);
            if (tokens.Any(NonOperationalProvenanceMarkers.Contains)) return true;
        }

        return false;
    }

    public static bool IsCertifiedOperationalSource(bool authenticityCertified, params string?[] values) =>
        authenticityCertified && !IsClearlyNonOperationalSource(values);

    public static bool TotalsMatch(decimal expected, decimal calculated) =>
        Math.Abs(expected - calculated) <= ReconciliationToleranceKg;

    public static string CanonicalHash(params string?[] values)
    {
        var canonical = string.Join('|', values.Select(v => (v ?? string.Empty).Trim().ToUpperInvariant()));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    public static string CanonicalDecimal(decimal value) => value.ToString("0.####", CultureInfo.InvariantCulture);
    public static string CanonicalDate(DateTime value) => value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    [GeneratedRegex(@"(?:Z|[+-]\d{2}:\d{2})$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ExplicitOffsetRegex();

    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9._:/-]{0,119}$", RegexOptions.CultureInvariant)]
    private static partial Regex SourceRecordIdRegex();
}
