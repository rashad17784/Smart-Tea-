namespace TeaOnlineShop.Models.Dbase;

public sealed class InventoryImportBatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ImportType { get; set; } = "OpeningBalance";
    public string FileName { get; set; } = string.Empty;
    public string FileSha256 { get; set; } = string.Empty;
    public string Status { get; set; } = "PendingApproval";
    public int SubmittedByUserId { get; set; }
    public string SubmittedByName { get; set; } = string.Empty;
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    public int? ApprovedByUserId { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int RejectedRows { get; set; }
    public string? Notes { get; set; }

    public ICollection<InventoryImportRow> Rows { get; set; } = new List<InventoryImportRow>();
    public ICollection<InventoryImportRowError> Errors { get; set; } = new List<InventoryImportRowError>();
}
