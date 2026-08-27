namespace TeaOnlineShop.Identity;

public sealed class SecurityAuditEvent
{
    public long Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public int ActorUserId { get; set; }
    public string ActorEmail { get; set; } = string.Empty;
    public int TargetUserId { get; set; }
    public string TargetEmail { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public string? IpAddress { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
