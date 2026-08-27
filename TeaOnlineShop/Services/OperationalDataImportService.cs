using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using TeaOnlineShop.Models.Dbase;
using TeaOnlineShop.Models.ViewModels;

namespace TeaOnlineShop.Services;

public sealed record OperationalImportActor(int UserId, string DisplayName);

public sealed class OperationalDataImportService
{
    private readonly TeaOnlineShopContext _context;

    public OperationalDataImportService(TeaOnlineShopContext context) => _context = context;

    public async Task<OperationalDataImportBatch> StageAsync(
        OperationalDataImportUploadViewModel manifest,
        OperationalImportActor actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest.File);
        if (manifest.SourcePeriodStart.Date > manifest.SourcePeriodEnd.Date)
            throw new InvalidOperationException("The source period start cannot be after the end.");
        if (manifest.SourcePeriodEnd.Date > DateTime.UtcNow.Date)
            throw new InvalidOperationException("The source period cannot end in the future.");
        if (manifest.File.Length <= 0 || manifest.File.Length > OperationalDataImportRules.MaximumFileBytes)
            throw new InvalidOperationException("The CSV must be between 1 byte and 10 MB.");
        if (!string.Equals(Path.GetExtension(manifest.File.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only CSV factory exports are accepted.");
        var safeFileName = Path.GetFileName(manifest.File.FileName);
        if (safeFileName.Length > 260 || safeFileName.Any(char.IsControl))
            throw new InvalidOperationException("The source file name is invalid or exceeds 260 characters.");
        if (OperationalDataImportRules.IsClearlyNonOperationalSource(
                manifest.SourceSystem, manifest.SourceDocumentReference, safeFileName, manifest.Notes))
        {
            throw new InvalidOperationException(OperationalDataImportRules.NonOperationalProvenanceMessage);
        }
        if (!manifest.ConfirmedGenuineSource)
            throw new InvalidOperationException("The genuine-source certification is required.");

        byte[] fileBytes;
        await using (var stream = new MemoryStream())
        {
            await manifest.File.CopyToAsync(stream, cancellationToken);
            fileBytes = stream.ToArray();
        }

        var fileHash = Convert.ToHexString(SHA256.HashData(fileBytes));
        if (await _context.OperationalDataImportBatches.AsNoTracking()
                .AnyAsync(x => x.FileSha256 == fileHash, cancellationToken))
            throw new InvalidOperationException("This exact source file has already been staged. Open the existing batch instead of importing it again.");

        var now = DateTime.UtcNow;
        var batch = new OperationalDataImportBatch
        {
            Id = Guid.NewGuid(),
            BatchNumber = $"ODI-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..32].ToUpperInvariant(),
            SourceSystem = manifest.SourceSystem.Trim().ToUpperInvariant(),
            SourceDocumentReference = manifest.SourceDocumentReference.Trim(),
            SourcePeriodStartUtc = DateTime.SpecifyKind(manifest.SourcePeriodStart.Date, DateTimeKind.Utc),
            SourcePeriodEndUtc = DateTime.SpecifyKind(manifest.SourcePeriodEnd.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc),
            FileName = safeFileName,
            ContentType = "text/csv",
            FileSha256 = fileHash,
            OriginalFile = fileBytes,
            Status = "Validating",
            SubmittedByUserId = actor.UserId,
            SubmittedByName = actor.DisplayName,
            SubmittedAtUtc = now,
            ExpectedRowCount = manifest.ExpectedRowCount,
            ExpectedInboundKg = manifest.ExpectedInboundKg,
            ExpectedOutboundKg = manifest.ExpectedOutboundKg,
            SourceAuthenticityCertified = manifest.ConfirmedGenuineSource,
            Notes = string.IsNullOrWhiteSpace(manifest.Notes) ? null : manifest.Notes.Trim()
        };

        ParseFile(batch, fileBytes);
        if (batch.Rows.Any(x => OperationalDataImportRules.IsClearlyNonOperationalSource(x.SourceSystem)))
        {
            throw new InvalidOperationException(OperationalDataImportRules.NonOperationalProvenanceMessage);
        }
        await ValidateRowsAsync(batch, cancellationToken);
        Reconcile(batch);

        batch.AuditEvents.Add(new OperationalDataImportAuditEvent
        {
            Action = "SubmittedAndValidated",
            FromStatus = "New",
            ToStatus = batch.Status,
            ActorUserId = actor.UserId,
            ActorName = actor.DisplayName,
            OccurredAtUtc = now,
            Details = JsonSerializer.Serialize(new
            {
                batch.SourceSystem,
                batch.SourceDocumentReference,
                batch.FileName,
                batch.FileSha256,
                batch.SourcePeriodStartUtc,
                batch.SourcePeriodEndUtc,
                batch.ExpectedRowCount,
                batch.ExpectedInboundKg,
                batch.ExpectedOutboundKg,
                batch.ParsedRowCount,
                batch.ValidRowCount,
                batch.RejectedRowCount,
                batch.DuplicateRowCount,
                batch.CalculatedInboundKg,
                batch.CalculatedOutboundKg,
                batch.ReconciliationStatus,
                batch.SourceAuthenticityCertified
            })
        });

        _context.OperationalDataImportBatches.Add(batch);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraint(ex))
        {
            throw new InvalidOperationException("The same file or source identity was staged concurrently. Open the existing batch instead.", ex);
        }
        return batch;
    }

    public async Task ApproveAsync(Guid batchId, OperationalImportActor actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var batch = await _context.OperationalDataImportBatches
            .Include(x => x.Rows)
            .Include(x => x.Errors)
            .SingleOrDefaultAsync(x => x.Id == batchId, cancellationToken)
            ?? throw new KeyNotFoundException("Operational import batch was not found.");

        if (batch.Status != "PendingApproval")
            throw new InvalidOperationException("Only fully validated and reconciled batches can be approved.");
        if (batch.SubmittedByUserId == actor.UserId)
            throw new InvalidOperationException("Dual control is required: the submitter cannot approve the same batch.");
        if (!OperationalDataImportRules.IsCertifiedOperationalSource(
                batch.SourceAuthenticityCertified,
                batch.SourceSystem,
                batch.SourceDocumentReference,
                batch.FileName,
                batch.Notes) ||
            batch.Rows.Any(x => OperationalDataImportRules.IsClearlyNonOperationalSource(x.SourceSystem)) ||
            batch.Errors.Count != 0 ||
            batch.ValidRowCount != batch.ParsedRowCount || batch.ReconciliationStatus != "Matched")
            throw new InvalidOperationException("The batch no longer satisfies publication controls.");

        var sourceIds = batch.Rows.Select(x => x.SourceRecordId).ToArray();
        if (await PublishedSourceIdsExistAsync(batch.SourceSystem, sourceIds, cancellationToken))
            throw new InvalidOperationException("A source record in this batch was published by another import. Re-stage a clean export.");

        var itemIds = batch.Rows.Select(x => x.InventoryItemId!.Value).Distinct().ToArray();
        var activeItemCount = await _context.TeaInventoryItems.AsNoTracking()
            .CountAsync(x => itemIds.Contains(x.Id) && x.Status == "Active", cancellationToken);
        if (activeItemCount != itemIds.Length)
            throw new InvalidOperationException("One or more mapped tea items are no longer active.");

        var approvedAt = DateTime.UtcNow;
        foreach (var row in batch.Rows.OrderBy(x => x.RowNumber))
        {
            _context.OperationalInventoryEvents.Add(new OperationalInventoryEvent
            {
                PublicId = Guid.NewGuid(),
                BatchId = batch.Id,
                ImportRowId = row.Id,
                SourceSystem = row.SourceSystem,
                SourceRecordId = row.SourceRecordId,
                SourceOccurredAtUtc = row.OriginalTransactionAtUtc,
                ImportedAtUtc = approvedAt,
                TeaGrade = row.TeaGrade,
                ItemCode = row.ItemCode,
                InventoryItemId = row.InventoryItemId!.Value,
                QuantityKg = row.QuantityKg,
                QuantityChangeKg = row.QuantityChangeKg,
                TransactionType = row.TransactionType,
                IsDemand = row.IsDemand,
                SourceReferenceNumber = row.SourceReferenceNumber,
                SupplierOrProductionReference = row.SupplierOrProductionReference,
                WarehouseCode = row.WarehouseCode,
                BinCode = row.BinCode,
                UnitCost = row.UnitCost,
                Reason = row.Reason,
                CanonicalSha256 = row.CanonicalSha256,
                ImportedByUserId = actor.UserId,
                ImportedByName = actor.DisplayName
            });
            row.Status = "Published";
        }

        batch.Status = "Approved";
        batch.ApprovedByUserId = actor.UserId;
        batch.ApprovedByName = actor.DisplayName;
        batch.ApprovedAtUtc = approvedAt;
        batch.AuditEvents.Add(new OperationalDataImportAuditEvent
        {
            Action = "ApprovedAndPublished",
            FromStatus = "PendingApproval",
            ToStatus = "Approved",
            ActorUserId = actor.UserId,
            ActorName = actor.DisplayName,
            OccurredAtUtc = approvedAt,
            Details = JsonSerializer.Serialize(new
            {
                PublishedRows = batch.Rows.Count,
                batch.CalculatedInboundKg,
                batch.CalculatedOutboundKg,
                Control = "Independent approval; atomic immutable event publication; live stock unchanged"
            })
        });

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraint(ex))
        {
            throw new InvalidOperationException("Publication was stopped because a source record was published concurrently by another batch.", ex);
        }
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RejectAsync(Guid batchId, OperationalImportActor actor, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 10 || reason.Trim().Length > 1000)
            throw new InvalidOperationException("A specific rejection reason between 10 and 1,000 characters is required.");

        var batch = await _context.OperationalDataImportBatches.SingleOrDefaultAsync(x => x.Id == batchId, cancellationToken)
            ?? throw new KeyNotFoundException("Operational import batch was not found.");
        if (batch.Status is "Approved" or "Rejected")
            throw new InvalidOperationException("This batch is already final and cannot be changed.");

        var previous = batch.Status;
        batch.Status = "Rejected";
        batch.RejectedByUserId = actor.UserId;
        batch.RejectedByName = actor.DisplayName;
        batch.RejectedAtUtc = DateTime.UtcNow;
        batch.RejectionReason = reason.Trim();
        batch.AuditEvents.Add(new OperationalDataImportAuditEvent
        {
            Action = "Rejected",
            FromStatus = previous,
            ToStatus = "Rejected",
            ActorUserId = actor.UserId,
            ActorName = actor.DisplayName,
            Details = reason.Trim()
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static void ParseFile(OperationalDataImportBatch batch, byte[] fileBytes)
    {
        using var stream = new MemoryStream(fileBytes, writable: false);
        using var parser = new TextFieldParser(stream, Encoding.UTF8, detectEncoding: true)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = false
        };
        parser.SetDelimiters(",");

        if (parser.EndOfData)
        {
            AddError(batch, 0, "File", "EMPTY_FILE", "The factory export contains no header row.");
            return;
        }

        string[]? header;
        try { header = parser.ReadFields(); }
        catch (MalformedLineException ex)
        {
            AddError(batch, 0, "Header", "MALFORMED_CSV", ex.Message);
            return;
        }

        if (header is null || !header.SequenceEqual(OperationalDataImportRules.Headers, StringComparer.Ordinal))
        {
            AddError(batch, 0, "Header", "INVALID_HEADER",
                "Header order must exactly match the controlled template: " + string.Join(',', OperationalDataImportRules.Headers));
            return;
        }

        var dataRow = 0;
        while (!parser.EndOfData)
        {
            string[]? fields;
            try { fields = parser.ReadFields(); }
            catch (MalformedLineException ex)
            {
                dataRow++;
                AddError(batch, dataRow, "Row", "MALFORMED_CSV", ex.Message);
                continue;
            }
            if (fields is null || fields.All(string.IsNullOrWhiteSpace)) continue;

            dataRow++;
            if (dataRow > OperationalDataImportRules.MaximumRows)
            {
                AddError(batch, 0, "File", "ROW_LIMIT_EXCEEDED", $"A batch cannot exceed {OperationalDataImportRules.MaximumRows:N0} rows.");
                break;
            }
            if (fields.Length != OperationalDataImportRules.Headers.Length)
            {
                AddError(batch, dataRow, "Row", "COLUMN_COUNT",
                    $"Expected {OperationalDataImportRules.Headers.Length} columns but found {fields.Length}.");
                continue;
            }

            var row = new OperationalDataImportRow
            {
                BatchId = batch.Id,
                RowNumber = dataRow,
                SourceSystem = Fit(fields[0], 80).ToUpperInvariant(),
                SourceRecordId = Fit(fields[1], 120),
                TeaGrade = Fit(fields[3], 20).ToUpperInvariant(),
                ItemCode = Fit(fields[4], 50).ToUpperInvariant(),
                OriginalUnit = Fit(fields[6], 20),
                TransactionType = Fit(fields[7], 40),
                SourceReferenceNumber = Fit(fields[8], 120),
                SupplierOrProductionReference = Fit(fields[9], 120),
                WarehouseCode = Fit(fields[10], 30).ToUpperInvariant(),
                BinCode = Fit(fields[11], 30).ToUpperInvariant(),
                Reason = Fit(fields[13], 500),
                RawData = JsonSerializer.Serialize(fields),
                Status = "Staged"
            };
            batch.Rows.Add(row);

            AddLengthError(batch, dataRow, "SourceSystem", fields[0], 80);
            AddLengthError(batch, dataRow, "SourceRecordId", fields[1], 120);
            AddLengthError(batch, dataRow, "TeaGrade", fields[3], 20);
            AddLengthError(batch, dataRow, "ItemCode", fields[4], 50);
            AddLengthError(batch, dataRow, "Unit", fields[6], 20);
            AddLengthError(batch, dataRow, "TransactionType", fields[7], 40);
            AddLengthError(batch, dataRow, "SourceReferenceNumber", fields[8], 120);
            AddLengthError(batch, dataRow, "SupplierOrProductionReference", fields[9], 120);
            AddLengthError(batch, dataRow, "WarehouseCode", fields[10], 30);
            AddLengthError(batch, dataRow, "BinCode", fields[11], 30);
            AddLengthError(batch, dataRow, "Reason", fields[13], 500);
            foreach (var index in new[] { 0, 1, 3, 4, 6, 7, 8, 9, 10, 11, 13 })
            {
                if (OperationalDataImportRules.IsSpreadsheetFormula(fields[index]))
                    AddError(batch, dataRow, OperationalDataImportRules.Headers[index], "SPREADSHEET_FORMULA",
                        "Spreadsheet formulas are prohibited in source text fields. Correct the source system and re-export.");
            }

            if (!OperationalDataImportRules.HasExplicitTimeZone(fields[2]) ||
                !DateTimeOffset.TryParse(fields[2], CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out var sourceDate))
                AddError(batch, dataRow, "OriginalTransactionDate", "INVALID_SOURCE_DATE",
                    "Use an ISO-8601 timestamp with an explicit Z or UTC offset, for example 2026-07-14T08:30:00+05:30.");
            else
                row.OriginalTransactionAtUtc = sourceDate.UtcDateTime;

            if (!OperationalDataImportRules.IsAllowedGrade(fields[3], out var grade))
                AddError(batch, dataRow, "TeaGrade", "INVALID_GRADE", "Allowed grades are BOP, BOPF, DUST, FNGS and OP.");
            else row.TeaGrade = grade;

            if (!decimal.TryParse(fields[5], NumberStyles.Number, CultureInfo.InvariantCulture, out var quantity) || quantity <= 0)
                AddError(batch, dataRow, "Quantity", "INVALID_QUANTITY", "Quantity must be a positive invariant decimal number.");
            else if (!OperationalDataImportRules.TryNormalizeQuantity(quantity, fields[6], out var kg))
                AddError(batch, dataRow, "Unit", "INVALID_UNIT", "Unit must be kg, g or metric tonne (or a documented equivalent). ");
            else row.QuantityKg = kg;

            if (!OperationalDataImportRules.TryGetMovement(fields[7], out var movement))
                AddError(batch, dataRow, "TransactionType", "INVALID_TRANSACTION_TYPE",
                    "Use SupplierReceipt, ProductionReceipt, CustomerReturn, StockIn, OpeningBalance, CustomerOrder, ProductionUsage, Damage or StockOut.");
            else
            {
                row.TransactionType = movement.CanonicalType;
                row.IsDemand = movement.IsDemand;
                if (row.QuantityKg > 0) row.QuantityChangeKg = row.QuantityKg * movement.Direction;
            }

            if (string.IsNullOrWhiteSpace(fields[12])) row.UnitCost = null;
            else if (!decimal.TryParse(fields[12], NumberStyles.Number, CultureInfo.InvariantCulture, out var unitCost) || unitCost < 0)
                AddError(batch, dataRow, "UnitCost", "INVALID_UNIT_COST", "Unit cost must be blank or a non-negative invariant decimal number.");
            else row.UnitCost = unitCost;
        }
    }

    private async Task ValidateRowsAsync(OperationalDataImportBatch batch, CancellationToken cancellationToken)
    {
        batch.ParsedRowCount = batch.Rows.Count;
        if (batch.Rows.Count == 0)
            AddError(batch, 0, "File", "NO_DATA_ROWS", "The export contains no usable data rows.");

        foreach (var row in batch.Rows)
        {
            if (!string.Equals(row.SourceSystem, batch.SourceSystem, StringComparison.OrdinalIgnoreCase))
                AddError(batch, row.RowNumber, "SourceSystem", "SOURCE_SYSTEM_MISMATCH", "Row source system must match the certified manifest source system.");
            if (!OperationalDataImportRules.IsValidSourceRecordId(row.SourceRecordId))
                AddError(batch, row.RowNumber, "SourceRecordId", "INVALID_SOURCE_ID", "A stable source record ID using safe characters is required.");
            if (row.OriginalTransactionAtUtc != default)
            {
                if (row.OriginalTransactionAtUtc > DateTime.UtcNow.AddMinutes(5))
                    AddError(batch, row.RowNumber, "OriginalTransactionDate", "FUTURE_DATE", "The source transaction timestamp cannot be in the future.");
                if (row.OriginalTransactionAtUtc < batch.SourcePeriodStartUtc || row.OriginalTransactionAtUtc > batch.SourcePeriodEndUtc)
                    AddError(batch, row.RowNumber, "OriginalTransactionDate", "OUTSIDE_SOURCE_PERIOD", "The transaction timestamp is outside the certified source period.");
            }
            Require(row.ItemCode, 50, batch, row.RowNumber, "ItemCode");
            Require(row.SourceReferenceNumber, 120, batch, row.RowNumber, "SourceReferenceNumber");
            Require(row.SupplierOrProductionReference, 120, batch, row.RowNumber, "SupplierOrProductionReference");
            Require(row.WarehouseCode, 30, batch, row.RowNumber, "WarehouseCode");
            Require(row.BinCode, 30, batch, row.RowNumber, "BinCode");
        }

        foreach (var duplicateGroup in batch.Rows
                     .Where(x => !string.IsNullOrWhiteSpace(x.SourceRecordId))
                     .GroupBy(x => $"{x.SourceSystem}\u001f{x.SourceRecordId}", StringComparer.OrdinalIgnoreCase)
                     .Where(g => g.Count() > 1))
        {
            foreach (var row in duplicateGroup)
                AddError(batch, row.RowNumber, "SourceRecordId", "DUPLICATE_IN_FILE", "The same source-system record ID occurs more than once in this file.");
        }

        var items = await _context.TeaInventoryItems.AsNoTracking()
            .ToDictionaryAsync(x => x.ItemCode.ToUpper(), StringComparer.OrdinalIgnoreCase, cancellationToken);
        var warehouses = await _context.Warehouses.AsNoTracking().Include(x => x.Bins)
            .ToDictionaryAsync(x => x.Code.ToUpper(), StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var row in batch.Rows)
        {
            if (!items.TryGetValue(row.ItemCode, out var item))
                AddError(batch, row.RowNumber, "ItemCode", "ITEM_NOT_FOUND", "No tea inventory master item matches this code.");
            else
            {
                row.InventoryItemId = item.Id;
                if (!string.Equals(item.Status, "Active", StringComparison.OrdinalIgnoreCase))
                    AddError(batch, row.RowNumber, "ItemCode", "ITEM_INACTIVE", "The mapped tea inventory item is not active.");
                if (!string.Equals(item.Grade.Trim(), row.TeaGrade, StringComparison.OrdinalIgnoreCase))
                    AddError(batch, row.RowNumber, "TeaGrade", "GRADE_ITEM_MISMATCH", $"The item master grade is {item.Grade}, not {row.TeaGrade}.");
                if (!OperationalDataImportRules.TryNormalizeQuantity(1, item.Unit, out _))
                    AddError(batch, row.RowNumber, "ItemCode", "ITEM_UOM_NOT_MASS", "The item master unit is not a supported mass unit.");
            }

            if (!warehouses.TryGetValue(row.WarehouseCode, out var warehouse) || !warehouse.IsActive)
                AddError(batch, row.RowNumber, "WarehouseCode", "WAREHOUSE_NOT_FOUND", "No active warehouse matches this code.");
            else if (!warehouse.Bins.Any(x => x.IsActive && string.Equals(x.Code, row.BinCode, StringComparison.OrdinalIgnoreCase)))
                AddError(batch, row.RowNumber, "BinCode", "BIN_NOT_FOUND", "No active bin with this code belongs to the selected warehouse.");
        }

        var sourceIds = batch.Rows.Where(x => OperationalDataImportRules.IsValidSourceRecordId(x.SourceRecordId))
            .Select(x => x.SourceRecordId).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var publishedIds = await GetPublishedSourceIdsAsync(batch.SourceSystem, sourceIds, cancellationToken);
        foreach (var row in batch.Rows.Where(x => publishedIds.Contains(x.SourceRecordId)))
            AddError(batch, row.RowNumber, "SourceRecordId", "ALREADY_PUBLISHED", "This source record was already published by an approved batch.");

        foreach (var row in batch.Rows)
        {
            row.CanonicalSha256 = OperationalDataImportRules.CanonicalHash(
                row.RawData, row.SourceSystem, row.SourceRecordId,
                row.OriginalTransactionAtUtc == default ? string.Empty : OperationalDataImportRules.CanonicalDate(row.OriginalTransactionAtUtc),
                row.TeaGrade, row.ItemCode, OperationalDataImportRules.CanonicalDecimal(row.QuantityKg), row.OriginalUnit,
                row.TransactionType, row.SourceReferenceNumber, row.SupplierOrProductionReference,
                row.WarehouseCode, row.BinCode,
                row.UnitCost?.ToString("0.####", CultureInfo.InvariantCulture), row.Reason);
            row.Status = batch.Errors.Any(x => x.RowNumber == row.RowNumber) ? "Rejected" : "Validated";
        }
    }

    private static void Reconcile(OperationalDataImportBatch batch)
    {
        batch.ValidRowCount = batch.Rows.Count(x => x.Status == "Validated");
        batch.RejectedRowCount = batch.Rows.Count - batch.ValidRowCount;
        batch.DuplicateRowCount = batch.Errors
            .Where(x => x.ErrorCode is "DUPLICATE_IN_FILE" or "ALREADY_PUBLISHED")
            .Select(x => x.RowNumber).Distinct().Count();
        batch.CalculatedInboundKg = batch.Rows.Where(x => x.QuantityChangeKg > 0).Sum(x => x.QuantityChangeKg);
        batch.CalculatedOutboundKg = Math.Abs(batch.Rows.Where(x => x.QuantityChangeKg < 0).Sum(x => x.QuantityChangeKg));

        if (batch.ExpectedRowCount != batch.ParsedRowCount)
            AddError(batch, 0, "ExpectedRowCount", "ROW_COUNT_MISMATCH", $"Manifest expects {batch.ExpectedRowCount:N0} rows but the export contains {batch.ParsedRowCount:N0}.");
        if (!OperationalDataImportRules.TotalsMatch(batch.ExpectedInboundKg, batch.CalculatedInboundKg))
            AddError(batch, 0, "ExpectedInboundKg", "INBOUND_TOTAL_MISMATCH", $"Manifest expects {batch.ExpectedInboundKg:N4} kg; calculated total is {batch.CalculatedInboundKg:N4} kg.");
        if (!OperationalDataImportRules.TotalsMatch(batch.ExpectedOutboundKg, batch.CalculatedOutboundKg))
            AddError(batch, 0, "ExpectedOutboundKg", "OUTBOUND_TOTAL_MISMATCH", $"Manifest expects {batch.ExpectedOutboundKg:N4} kg; calculated total is {batch.CalculatedOutboundKg:N4} kg.");

        var matched = batch.Errors.Count == 0 && batch.ValidRowCount == batch.ParsedRowCount;
        batch.ReconciliationStatus = matched ? "Matched" : "Failed";
        batch.Status = matched ? "PendingApproval" : "ValidationFailed";
    }

    private async Task<HashSet<string>> GetPublishedSourceIdsAsync(string sourceSystem, string[] sourceIds, CancellationToken cancellationToken)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var chunk in sourceIds.Chunk(500))
        {
            var values = await _context.OperationalInventoryEvents.AsNoTracking()
                .Where(x => x.SourceSystem == sourceSystem && chunk.Contains(x.SourceRecordId))
                .Select(x => x.SourceRecordId).ToListAsync(cancellationToken);
            result.UnionWith(values);
        }
        return result;
    }

    private async Task<bool> PublishedSourceIdsExistAsync(string sourceSystem, string[] sourceIds, CancellationToken cancellationToken) =>
        (await GetPublishedSourceIdsAsync(sourceSystem, sourceIds, cancellationToken)).Count > 0;

    private static void Require(string value, int maxLength, OperationalDataImportBatch batch, int row, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) AddError(batch, row, field, "REQUIRED", $"{field} is required.");
        else if (value.Length > maxLength) AddError(batch, row, field, "VALUE_TOO_LONG", $"{field} cannot exceed {maxLength} characters.");
    }

    private static string Fit(string? value, int maximumLength)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length <= maximumLength ? trimmed : trimmed[..maximumLength];
    }

    private static void AddLengthError(OperationalDataImportBatch batch, int row, string field, string? raw, int maximumLength)
    {
        if ((raw ?? string.Empty).Trim().Length > maximumLength)
            AddError(batch, row, field, "VALUE_TOO_LONG", $"{field} cannot exceed {maximumLength} characters. The full original value remains in RawData.");
    }

    private static bool IsUniqueConstraint(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };

    private static void AddError(OperationalDataImportBatch batch, int row, string field, string code, string message) =>
        batch.Errors.Add(new OperationalDataImportRowError
        {
            BatchId = batch.Id,
            RowNumber = row,
            FieldName = Fit(field, 80),
            ErrorCode = Fit(code, 50),
            Message = Fit(message, 500)
        });
}
