namespace TeaOnlineShop.Services;

public sealed class AiServiceOptions
{
    public string BaseUrl { get; set; } = "http://localhost:8000";
    public int TimeoutSeconds { get; set; } = 60;
    public string ResearchDemandDatasetPath { get; set; } = "../SmartTea_AI/data/tea_demand_timeseries.csv";
    public string[] DemandMovementTypes { get; set; } = { "CustomerOrder", "ProductionUsage" };
}
