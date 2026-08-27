using System.ComponentModel.DataAnnotations;

namespace TeaOnlineShop.Models.Dbase;

public sealed class AiPredictionHistory
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();

    [Required, StringLength(30)]
    public string PredictionType { get; set; } = string.Empty;

    [StringLength(20)]
    public string Grade { get; set; } = string.Empty;

    public int HorizonDays { get; set; }
    public int? RequestedByUserId { get; set; }

    [Required, StringLength(120)]
    public string RequestedByName { get; set; } = string.Empty;

    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;

    [Required, StringLength(120)]
    public string Model { get; set; } = string.Empty;

    [StringLength(40)]
    public string ModelVersion { get; set; } = string.Empty;

    [StringLength(80)]
    public string Strategy { get; set; } = string.Empty;

    public decimal? ExpectedMape { get; set; }

    [Required, StringLength(50)]
    public string DataSource { get; set; } = string.Empty;

    [Required, StringLength(160)]
    public string SourceLabel { get; set; } = string.Empty;

    [StringLength(1000)]
    public string SourceNote { get; set; } = string.Empty;

    public DateTime? SourceStartDateUtc { get; set; }
    public DateTime? SourceEndDateUtc { get; set; }

    [Required, StringLength(1000)]
    public string InputSummary { get; set; } = string.Empty;

    [Required]
    public string ResultJson { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string Status { get; set; } = "Succeeded";
}
