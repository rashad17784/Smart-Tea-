namespace TeaOnlineShop.Models.ViewModels
{
    public class AiDashboardOverviewViewModel
    {
        public bool AiAvailable { get; set; }
        public string AiMessage { get; set; } = string.Empty;
        public DateTimeOffset GeneratedAtUtc { get; set; } = DateTimeOffset.UtcNow;
        public AiDemandForecastHistoryRecord? LatestDemandForecast { get; set; }
        public AiPriceForecastHistoryRecord? LatestPriceForecast { get; set; }
        public int AnomalyAlertCount { get; set; }
        public int WarningAlertCount { get; set; }
        public int CriticalAlertCount { get; set; }
        public List<AiAlertHistoryRecord> RecentAlerts { get; set; } = new();
        public List<AiDemandForecastHistoryRecord> DemandHistory { get; set; } = new();
        public List<AiPriceForecastHistoryRecord> PriceHistory { get; set; } = new();
        public List<string> Insights { get; set; } = new();
    }

    public class AiDemandForecastPageViewModel
    {
        public bool AiAvailable { get; set; }
        public string AiMessage { get; set; } = string.Empty;
        public List<string> TeaGrades { get; set; } =
            new() { "BOP", "BOPF", "DUST", "FNGS", "OP" };
        public List<int> ForecastHorizons { get; set; } =
            new() { 30, 45, 60 };
        public List<AiDemandForecastHistoryRecord> History { get; set; } = new();
    }

    public class AiPriceForecastPageViewModel
    {
        public bool AiAvailable { get; set; }
        public string AiMessage { get; set; } = string.Empty;
        public List<int> ForecastHorizons { get; set; } =
            new() { 7, 14, 30 };
        public List<AiPriceForecastHistoryRecord> History { get; set; } = new();
    }

    public class AiAnomalyPageViewModel
    {
        public bool AiAvailable { get; set; }
        public string AiMessage { get; set; } = string.Empty;
        public List<string> TeaGrades { get; set; } =
            new() { "BOP", "BOPF", "DUST", "FNGS", "OP" };
        public List<AiAlertHistoryRecord> RecentAlerts { get; set; } = new();
    }

    public class AiAlertsHistoryViewModel
    {
        public List<AiAlertHistoryRecord> Alerts { get; set; } = new();
        public string SeverityFilter { get; set; } = string.Empty;
        public string GradeFilter { get; set; } = string.Empty;
        public List<string> TeaGrades { get; set; } =
            new() { "BOP", "BOPF", "DUST", "FNGS", "OP" };
        public int TotalAlerts { get; set; }
        public int WarningAlerts { get; set; }
        public int CriticalAlerts { get; set; }
    }

    public class AiDemandForecastHistoryRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;
        public string UserName { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public int HorizonDays { get; set; }
        public List<double> Last60DaysDemand { get; set; } = new();
        public List<double> Predictions { get; set; } = new();
        public string Model { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = string.Empty;
        public string Strategy { get; set; } = string.Empty;
        public double ExpectedMape { get; set; }
        public string DataSource { get; set; } = string.Empty;
        public string SourceLabel { get; set; } = string.Empty;
        public string SourceNote { get; set; } = string.Empty;
        public DateOnly? SourceStartDate { get; set; }
        public DateOnly? SourceEndDate { get; set; }
    }

    public class AiPriceForecastHistoryRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;
        public string UserName { get; set; } = string.Empty;
        public string ForecastType { get; set; } = string.Empty;
        public int HorizonDays { get; set; }

        public double CurrentPrice { get; set; }
        public double PriceLag1 { get; set; }
        public double PriceLag2 { get; set; }
        public double PriceLag3 { get; set; }
        public double PriceLag7 { get; set; }
        public double PriceLag14 { get; set; }
        public double RollingMean7 { get; set; }
        public double RollingMean30 { get; set; }
        public double RollingStd7 { get; set; }
        public double PriceChangePct { get; set; }
        public double QuantityKg { get; set; }
        public double QtyRolling7 { get; set; }
        public double FirewoodKg { get; set; }
        public double FirewoodCost { get; set; }
        public double TotalCost { get; set; }
        public double Temperature { get; set; }
        public double RainfallMm { get; set; }
        public int HeavyRain { get; set; }
        public int SupplierDelivered { get; set; }
        public double SupplierQty { get; set; }
        public int Promotion { get; set; }
        public int MonthStart { get; set; }
        public int Month { get; set; }
        public int DayOfWeek { get; set; }
        public int Quarter { get; set; }
        public int IsWeekend { get; set; }
        public int DayOfYear { get; set; }
        public int Day { get; set; }

        public double? PredictedPrice { get; set; }
        public double ChangePct { get; set; }
        public string Trend { get; set; } = string.Empty;
        public List<AiPriceForecastPoint> Forecast { get; set; } = new();
        public AiPriceForecastSummary? Summary { get; set; }
        public string Model { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = string.Empty;
        public string Strategy { get; set; } = string.Empty;
        public double ExpectedMape { get; set; }
    }

    public class AiPriceForecastPoint
    {
        public string Day { get; set; } = string.Empty;
        public int DayNumber { get; set; }
        public double Predicted { get; set; }
        public double ChangePct { get; set; }
        public string Trend { get; set; } = string.Empty;
        public double ExpectedMape { get; set; }
    }

    public class AiPriceForecastSummary
    {
        public double AvgPrice { get; set; }
        public double MinPrice { get; set; }
        public double MaxPrice { get; set; }
        public string OverallTrend { get; set; } = string.Empty;
        public double OverallChangePct { get; set; }
    }

    public class AiAlertHistoryRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;
        public string UserName { get; set; } = string.Empty;

        public string Grade { get; set; } = string.Empty;
        public double DemandKg { get; set; }
        public double StockLevelKg { get; set; }
        public double PricePerKg { get; set; }
        public int DayOfWeek { get; set; }
        public int Month { get; set; }
        public int IsWeekend { get; set; }

        public bool IsAnomaly { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public double Score { get; set; }
        public bool ModelTriggered { get; set; }
        public List<AiAlertRuleHistory> RulesTriggered { get; set; } = new();
        public string Model { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = string.Empty;
    }

    public class AiAlertRuleHistory
    {
        public string Rule { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class DemandForecastEvaluationViewModel
    {
        public Guid PredictionId { get; set; }
        public string Grade { get; set; } = string.Empty;
        public int HorizonDays { get; set; }
        public DateTime RequestedAtUtc { get; set; }
        public string Model { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = string.Empty;
        public decimal? ExpectedMape { get; set; }
        public string DataSource { get; set; } = string.Empty;
        public string SourceLabel { get; set; } = string.Empty;
        public string SourceNote { get; set; } = string.Empty;
        public int CompletedDays { get; set; }
        public double? ActualMape { get; set; }
        public List<DemandForecastEvaluationRow> Rows { get; set; } = new();
    }

    public sealed class DemandForecastEvaluationRow
    {
        public int DayNumber { get; set; }
        public DateOnly Date { get; set; }
        public double PredictedKg { get; set; }
        public double? ActualKg { get; set; }
        public double? AbsolutePercentageError { get; set; }
    }
}
