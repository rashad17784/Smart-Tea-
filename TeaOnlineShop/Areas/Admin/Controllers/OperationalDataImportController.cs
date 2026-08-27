using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeaOnlineShop.Authorization;
using TeaOnlineShop.Models.Dbase;
using TeaOnlineShop.Models.ViewModels;
using TeaOnlineShop.Services;

namespace TeaOnlineShop.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AppPermissions.OperationalDataImportSubmit)]
public sealed class OperationalDataImportController : AdminBaseController
{
    private const int PageSize = 100;
    private readonly TeaOnlineShopContext _context;
    private readonly OperationalDataImportService _service;

    public OperationalDataImportController(TeaOnlineShopContext context, OperationalDataImportService service)
    {
        _context = context;
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? status, CancellationToken cancellationToken)
    {
        var query = _context.OperationalDataImportBatches.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        var batches = await query.OrderByDescending(x => x.SubmittedAtUtc).Take(200)
            .Select(x => new OperationalDataImportListItem
            {
                Id = x.Id,
                BatchNumber = x.BatchNumber,
                SourceSystem = x.SourceSystem,
                SourcePeriodStartUtc = x.SourcePeriodStartUtc,
                SourcePeriodEndUtc = x.SourcePeriodEndUtc,
                FileName = x.FileName,
                Status = x.Status,
                ReconciliationStatus = x.ReconciliationStatus,
                ValidRowCount = x.ValidRowCount,
                RejectedRowCount = x.RejectedRowCount,
                CalculatedInboundKg = x.CalculatedInboundKg,
                CalculatedOutboundKg = x.CalculatedOutboundKg,
                SubmittedAtUtc = x.SubmittedAtUtc,
                SubmittedByName = x.SubmittedByName
            }).ToListAsync(cancellationToken);
        ViewBag.Status = status;
        return View(batches);
    }

    [HttpGet]
    public IActionResult Upload() => View(new OperationalDataImportUploadViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(OperationalDataImportRules.MaximumFileBytes + 1024 * 1024)]
    public async Task<IActionResult> Upload(OperationalDataImportUploadViewModel model, CancellationToken cancellationToken)
    {
        var safeFileName = model.File is null ? null : Path.GetFileName(model.File.FileName);
        if (OperationalDataImportRules.IsClearlyNonOperationalSource(
                model.SourceSystem, model.SourceDocumentReference, safeFileName, model.Notes))
        {
            ModelState.AddModelError(string.Empty, OperationalDataImportRules.NonOperationalProvenanceMessage);
        }
        if (!model.ConfirmedGenuineSource)
            ModelState.AddModelError(nameof(model.ConfirmedGenuineSource), "You must certify the genuine source before staging.");
        if (model.File is null || model.File.Length == 0)
            ModelState.AddModelError(nameof(model.File), "Select a non-empty factory CSV export.");
        if (!ModelState.IsValid) return View(model);

        try
        {
            var batch = await _service.StageAsync(model, GetActor(), cancellationToken);
            TempData["Success"] = batch.Status == "PendingApproval"
                ? "The export passed validation and reconciliation. Independent approval is now required."
                : "The export was staged but failed one or more controls. Review the validation report.";
            return RedirectToAction(nameof(Details), new { id = batch.Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, int page = 1, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        var batch = await _context.OperationalDataImportBatches.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (batch is null) return NotFound();

        var pageRows = await _context.OperationalDataImportRows.AsNoTracking()
            .Where(x => x.BatchId == id).OrderBy(x => x.RowNumber)
            .Skip((page - 1) * PageSize).Take(PageSize).ToListAsync(cancellationToken);
        var pageRowNumbers = pageRows.Select(x => x.RowNumber).ToArray();
        var visibleErrors = await _context.OperationalDataImportRowErrors.AsNoTracking()
            .Where(x => x.BatchId == id && (x.RowNumber == 0 || pageRowNumbers.Contains(x.RowNumber)))
            .OrderBy(x => x.RowNumber).ThenBy(x => x.FieldName).ToListAsync(cancellationToken);
        var audit = await _context.OperationalDataImportAuditEvents.AsNoTracking()
            .Where(x => x.BatchId == id).OrderByDescending(x => x.OccurredAtUtc).ToListAsync(cancellationToken);
        batch.Rows = pageRows;
        batch.Errors = visibleErrors;
        batch.AuditEvents = audit;

        var summarySource = await _context.OperationalDataImportRows.AsNoTracking()
            .Where(x => x.BatchId == id && x.OriginalTransactionAtUtc != default)
            .Select(x => new
            {
                x.TeaGrade,
                x.IsDemand,
                x.QuantityChangeKg,
                x.OriginalTransactionAtUtc
            }).ToListAsync(cancellationToken);
        var summaries = summarySource.GroupBy(x => x.TeaGrade).OrderBy(x => x.Key)
            .Select(g => new OperationalDataGradeSummary(
                g.Key, g.Count(), g.Count(x => x.IsDemand),
                g.Where(x => x.QuantityChangeKg > 0).Sum(x => x.QuantityChangeKg),
                Math.Abs(g.Where(x => x.QuantityChangeKg < 0).Sum(x => x.QuantityChangeKg)),
                g.Min(x => x.OriginalTransactionAtUtc), g.Max(x => x.OriginalTransactionAtUtc),
                g.Select(x => x.OriginalTransactionAtUtc.Date).Distinct().Count()))
            .ToList();

        var submittedByCurrentUser = batch.SubmittedByUserId == GetActor().UserId;
        var canApprove = User.HasClaim(AppPermissions.ClaimType, AppPermissions.OperationalDataImportApprove) &&
                         batch.Status == "PendingApproval" && !submittedByCurrentUser;
        return View(new OperationalDataImportDetailsViewModel
        {
            Batch = batch,
            GradeSummaries = summaries,
            Page = page,
            PageSize = PageSize,
            TotalPages = Math.Max(1, (int)Math.Ceiling(batch.ParsedRowCount / (double)PageSize)),
            SubmittedByCurrentUser = submittedByCurrentUser,
            CanApprove = canApprove,
            CanReject = User.HasClaim(AppPermissions.ClaimType, AppPermissions.OperationalDataImportApprove) &&
                        batch.Status is not ("Approved" or "Rejected")
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPermissions.OperationalDataImportApprove)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _service.ApproveAsync(id, GetActor(), cancellationToken);
            TempData["Success"] = "The independently approved batch was atomically published to verified operational history. Live stock was not changed.";
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPermissions.OperationalDataImportApprove)]
    public async Task<IActionResult> Reject(Guid id, string reason, CancellationToken cancellationToken)
    {
        try
        {
            await _service.RejectAsync(id, GetActor(), reason, cancellationToken);
            TempData["Success"] = "The batch was rejected and retained as audit evidence.";
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public IActionResult Template()
    {
        var csv = string.Join(',', OperationalDataImportRules.Headers) + Environment.NewLine;
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", "smarttea-operational-history-template.csv");
    }

    [HttpGet]
    [Authorize(Policy = AppPermissions.OperationalDataImportApprove)]
    public async Task<IActionResult> Original(Guid id, CancellationToken cancellationToken)
    {
        var batch = await _context.OperationalDataImportBatches.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.OriginalFile, x.ContentType, x.FileName })
            .SingleOrDefaultAsync(cancellationToken);
        return batch is null ? NotFound() : File(batch.OriginalFile, batch.ContentType, batch.FileName);
    }

    [HttpGet]
    public async Task<IActionResult> ValidationErrors(Guid id, CancellationToken cancellationToken)
    {
        var batch = await _context.OperationalDataImportBatches.AsNoTracking()
            .Where(x => x.Id == id).Select(x => new { x.BatchNumber }).SingleOrDefaultAsync(cancellationToken);
        if (batch is null) return NotFound();
        var errors = await _context.OperationalDataImportRowErrors.AsNoTracking()
            .Where(x => x.BatchId == id).OrderBy(x => x.RowNumber).ThenBy(x => x.FieldName).ToListAsync(cancellationToken);
        var csv = new StringBuilder("RowNumber,FieldName,ErrorCode,Message\r\n");
        foreach (var error in errors)
            csv.Append(error.RowNumber).Append(',').Append(Csv(error.FieldName)).Append(',')
                .Append(Csv(error.ErrorCode)).Append(',').Append(Csv(error.Message)).Append("\r\n");
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"{batch.BatchNumber}-validation-errors.csv");
    }

    private OperationalImportActor GetActor()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var id))
            throw new InvalidOperationException("The authenticated user identifier is unavailable.");
        return new OperationalImportActor(id, User.Identity?.Name ?? "Unknown user");
    }

    private static string Csv(string value) => '"' + value.Replace("\"", "\"\"") + '"';
}
