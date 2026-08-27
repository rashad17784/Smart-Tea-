namespace TeaOnlineShop.Models.Dbase;

public sealed class OperationalDataImportBatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BatchNumber { get; set; } = string.Empty;
    public string SourceSystem { get; set; } = string.Empty;
    public string SourceDocumentReference { get; set; } = string.Empty;
    public DateTime SourcePeriodStartUtc { get; set; }
    public DateTime SourcePeriodEndUtc { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text/csv";
    public string FileSha256 { get; set; } = string.Empty;
    public byte[] OriginalFile { get; set; } = Array.Empty<byte>();
    public string Status { get; set; } = "Validating";
    public int SubmittedByUserId { get; set; }
    public string SubmittedByName { get; set; } = string.Empty;
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    public int? ApprovedByUserId { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public int? RejectedByUserId { get; set; }
    public string? RejectedByName { get; set; }
    public DateTime? RejectedAtUtc { get; set; }
    public string? RejectionReason { get; set; }
    public bool SourceAuthenticityCertified { get; set; }
    public int ExpectedRowCount { get; set; }
    public decimal ExpectedInboundKg { get; set; }
    public decimal ExpectedOutboundKg { get; set; }
    public int ParsedRowCount { get; set; }
    public int ValidRowCount { get; set; }
    public int RejectedRowCount { get; set; }
    public int DuplicateRowCount { get; set; }
    public decimal CalculatedInboundKg { get; set; }
    public decimal CalculatedOutboundKg { get; set; }
    public string ReconciliationStatus { get; set; } = "Pending";
    public string? Notes { get; set; }

    public ICollection<OperationalDataImportRow> Rows { get; set; } = new List<OperationalDataImportRow>();
    public ICollection<OperationalDataImportRowError> Errors { get; set; } = new List<OperationalDataImportRowError>();
    public ICollection<OperationalDataImportAuditEvent> AuditEvents { get; set; } = new List<OperationalDataImportAuditEvent>();
    public ICollection<OperationalInventoryEvent> PublishedEvents { get; set; } = new List<OperationalInventoryEvent>();
}
