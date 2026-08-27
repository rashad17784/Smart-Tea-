using Microsoft.EntityFrameworkCore;
using TeaOnlineShop.Models.Dbase;

namespace TeaOnlineShop.Services;

public sealed class AiPredictionHistoryService
{
    private readonly TeaOnlineShopContext _context;

    public AiPredictionHistoryService(TeaOnlineShopContext context)
    {
        _context = context;
    }

    public async Task AppendAsync(
        AiPredictionHistory record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        _context.AiPredictionHistories.Add(record);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<AiPredictionHistory?> FindAsync(
        Guid publicId,
        CancellationToken cancellationToken = default) =>
        _context.AiPredictionHistories.AsNoTracking()
            .SingleOrDefaultAsync(x => x.PublicId == publicId, cancellationToken);

    public Task<List<AiPredictionHistory>> GetRecentAsync(
        string? type = null,
        int maximum = 100,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AiPredictionHistories.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(x => x.PredictionType == type);
        return query.OrderByDescending(x => x.RequestedAtUtc)
            .Take(Math.Clamp(maximum, 1, 1000))
            .ToListAsync(cancellationToken);
    }
}
