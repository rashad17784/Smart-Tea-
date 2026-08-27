namespace TeaOnlineShop.Models.Dbase;

public sealed class InventoryImportRowError
{
    public long Id { get; set; }
    public Guid BatchId { get; set; }
    public int RowNumber { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public InventoryImportBatch Batch { get; set; } = null!;
}
