using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using TeaOnlineShop.Models.Dbase;

namespace TeaOnlineShop.Models.ViewModels;

public sealed class OperationalDataImportUploadViewModel
{
    [Required]
    [Display(Name = "Factory CSV export")]
    public IFormFile? File { get; set; }

    [Required, StringLength(80)]
    [RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9._-]{1,79}$",
        ErrorMessage = "Use a stable system code containing letters, numbers, dot, underscore or hyphen.")]
    [Display(Name = "Source system")]
    public string SourceSystem { get; set; } = string.Empty;

    [Required, StringLength(120)]
    [Display(Name = "Source document / export reference")]
    public string SourceDocumentReference { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Source period start")]
    public DateTime SourcePeriodStart { get; set; } = DateTime.UtcNow.Date.AddDays(-90);

    [DataType(DataType.Date)]
    [Display(Name = "Source period end")]
    public DateTime SourcePeriodEnd { get; set; } = DateTime.UtcNow.Date;

    [Range(1, 10000)]
    [Display(Name = "Expected data rows")]
    public int ExpectedRowCount { get; set; }

    [Range(typeof(decimal), "0", "1000000000")]
    [Display(Name = "Expected inbound total (kg)")]
    public decimal ExpectedInboundKg { get; set; }

    [Range(typeof(decimal), "0", "1000000000")]
    [Display(Name = "Expected outbound total (kg)")]
    public decimal ExpectedOutboundKg { get; set; }

    [Display(Name = "I certify this is an unmodified export from the named operational source; dates were not manually backdated.")]
    public bool ConfirmedGenuineSource { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}

public sealed class OperationalDataImportDetailsViewModel
{
    public required OperationalDataImportBatch Batch { get; init; }
    public IReadOnlyList<OperationalDataGradeSummary> GradeSummaries { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public bool SubmittedByCurrentUser { get; init; }
    public bool CanApprove { get; init; }
    public bool CanReject { get; init; }
}

public sealed class OperationalDataImportListItem
{
    public Guid Id { get; init; }
    public string BatchNumber { get; init; } = string.Empty;
    public string SourceSystem { get; init; } = string.Empty;
    public DateTime SourcePeriodStartUtc { get; init; }
    public DateTime SourcePeriodEndUtc { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string ReconciliationStatus { get; init; } = string.Empty;
    public int ValidRowCount { get; init; }
    public int RejectedRowCount { get; init; }
    public decimal CalculatedInboundKg { get; init; }
    public decimal CalculatedOutboundKg { get; init; }
    public DateTime SubmittedAtUtc { get; init; }
    public string SubmittedByName { get; init; } = string.Empty;
}

public sealed record OperationalDataGradeSummary(
    string Grade,
    int Rows,
    int DemandRows,
    decimal InboundKg,
    decimal OutboundKg,
    DateTime FirstEventUtc,
    DateTime LastEventUtc,
    int DistinctEventDates);
