namespace TeaOnlineShop.Models.Dbase;

public sealed class OperationalDataImportAuditEvent
{
    public long Id { get; set; }
    public Guid BatchId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public int? ActorUserId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public string Details { get; set; } = string.Empty;

    public OperationalDataImportBatch Batch { get; set; } = null!;
}
