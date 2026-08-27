using System;
using System.Collections.Generic;

namespace TeaOnlineShop.Models.Dbase;

public partial class QRCodeScan
{
    public int Id { get; set; }
    public string QRCodeData { get; set; } = null!;
    public int ScannedById { get; set; }
    public string ScannedByName { get; set; } = string.Empty;
    public DateTime ScanDateTime { get; set; } = DateTime.Now;
    public string? ScanResult { get; set; }
    public string? ActionTaken { get; set; }
    public string? Notes { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public bool WasSuccessful { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public Guid CorrelationId { get; set; } = Guid.NewGuid();

} 
