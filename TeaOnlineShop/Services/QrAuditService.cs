using System.Security.Claims;
using TeaOnlineShop.Models.Dbase;

namespace TeaOnlineShop.Services;

public sealed class QrAuditService
{
    private readonly TeaOnlineShopContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public QrAuditService(TeaOnlineShopContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task RecordAsync(
        string code,
        string entityType,
        int? entityId,
        bool successful,
        string result,
        string action,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var http = _httpContextAccessor.HttpContext;
        var user = http?.User;
        var actorId = int.TryParse(user?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        _context.QRCodeScans.Add(new QRCodeScan
        {
            QRCodeData = (code ?? string.Empty).Trim(),
            ScannedById = actorId,
            ScannedByName = user?.Identity?.Name ?? "Unknown staff user",
            ScanDateTime = DateTime.Now,
            ScanResult = result,
            ActionTaken = action,
            Notes = notes,
            EntityType = entityType,
            EntityId = entityId,
            WasSuccessful = successful,
            IpAddress = http?.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            UserAgent = http?.Request.Headers.UserAgent.ToString() ?? string.Empty,
            CorrelationId = Guid.NewGuid()
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
