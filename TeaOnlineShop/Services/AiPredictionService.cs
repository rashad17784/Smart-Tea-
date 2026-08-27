// ============================================================
// AiPredictionService.cs
// Connects SmartTea to Python AI API
// Version 2.3 — supports demand 7/14/30/90, price 7/14/30
// File location: TeaOnlineShop\Services\AiPredictionService.cs
// ============================================================

using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace TeaOnlineShop.Services
{
    // ============================================================
    // REQUEST MODELS
    // Data sent TO Python AI
    // ============================================================

    public class DemandForecastRequest
    {
        [JsonPropertyName("grade")]
        public string Grade { get; set; } = string.Empty;

        // Changed from last_30_days_demand to last_60_days_demand
        // New LSTM v2 needs 60 days of history as input
        [JsonPropertyName("last_60_days_demand")]
        public List<double> Last60DaysDemand { get; set; } = new();

        // Supported demand horizons: 30, 45, or 60
        [JsonPropertyName("horizon_days")]
        public int HorizonDays { get; set; } = 30;

        [JsonPropertyName("data_source")]
        public string DataSource { get; set; } = string.Empty;

        [JsonPropertyName("source_label")]
        public string SourceLabel { get; set; } = string.Empty;

        [JsonPropertyName("source_note")]
        public string SourceNote { get; set; } = string.Empty;

        [JsonPropertyName("source_start_date")]
        public string SourceStartDate { get; set; } = string.Empty;

        [JsonPropertyName("source_end_date")]
        public string SourceEndDate { get; set; } = string.Empty;
    }

    public class PricePredictRequest
    {
        [JsonPropertyName("current_price")]
        public double CurrentPrice { get; set; }

        [JsonPropertyName("price_lag1")]
        public double PriceLag1 { get; set; }

        [JsonPropertyName("price_lag2")]
        public double PriceLag2 { get; set; }

        [JsonPropertyName("price_lag3")]
        public double PriceLag3 { get; set; }

        [JsonPropertyName("price_lag7")]
        public double PriceLag7 { get; set; }

        [JsonPropertyName("price_lag14")]
        public double PriceLag14 { get; set; }

        [JsonPropertyName("rolling_mean7")]
        public double RollingMean7 { get; set; }

        [JsonPropertyName("rolling_mean30")]
        public double RollingMean30 { get; set; }

        [JsonPropertyName("rolling_std7")]
        public double RollingStd7 { get; set; }

        [JsonPropertyName("price_change_pct")]
        public double PriceChangePct { get; set; }

        [JsonPropertyName("quantity_kg")]
        public double QuantityKg { get; set; }

        [JsonPropertyName("qty_rolling7")]
        public double QtyRolling7 { get; set; }

        [JsonPropertyName("firewood_kg")]
        public double FirewoodKg { get; set; }

        [JsonPropertyName("firewood_cost")]
        public double FirewoodCost { get; set; }

        [JsonPropertyName("total_cost")]
        public double TotalCost { get; set; }

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("rainfall_mm")]
        public double RainfallMm { get; set; }

        [JsonPropertyName("heavy_rain")]
        public int HeavyRain { get; set; }

        [JsonPropertyName("supplier_delivered")]
        public int SupplierDelivered { get; set; }

        [JsonPropertyName("supplier_qty")]
        public double SupplierQty { get; set; }

        [JsonPropertyName("promotion")]
        public int Promotion { get; set; }

        [JsonPropertyName("month_start")]
        public int MonthStart { get; set; }

        [JsonPropertyName("month")]
        public int Month { get; set; }

        [JsonPropertyName("day_of_week")]
        public int DayOfWeek { get; set; }

        [JsonPropertyName("quarter")]
        public int Quarter { get; set; }

        [JsonPropertyName("is_weekend")]
        public int IsWeekend { get; set; }

        [JsonPropertyName("day_of_year")]
        public int DayOfYear { get; set; }

        [JsonPropertyName("day")]
        public int Day { get; set; }

        // New: 7, 14, or 30
        [JsonPropertyName("horizon_days")]
        public int HorizonDays { get; set; } = 7;
    }

    public class AnomalyCheckRequest
    {
        [JsonPropertyName("grade")]
        public string Grade { get; set; }

        [JsonPropertyName("demand_kg")]
        public double DemandKg { get; set; }

        [JsonPropertyName("stock_level_kg")]
        public double StockLevelKg { get; set; }

        [JsonPropertyName("price_per_kg")]
        public double PricePerKg { get; set; }

        [JsonPropertyName("day_of_week")]
        public int DayOfWeek { get; set; }

        [JsonPropertyName("month")]
        public int Month { get; set; }

        [JsonPropertyName("is_weekend")]
        public int IsWeekend { get; set; }
    }


    // ============================================================
    // RESPONSE MODELS
    // Data received FROM Python AI
    // ============================================================

    public class DemandForecastResponse
    {
        [JsonPropertyName("grade")]
        public string Grade { get; set; }

        [JsonPropertyName("horizon_days")]
        public int HorizonDays { get; set; }

        [JsonPropertyName("forecast_days")]
        public int ForecastDays { get; set; }

        [JsonPropertyName("predictions")]
        public List<double> Predictions { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("model_version")]
        public string ModelVersion { get; set; }

        [JsonPropertyName("strategy")]
        public string Strategy { get; set; }

        [JsonPropertyName("expected_mape")]
        public double ExpectedMape { get; set; }

        [JsonPropertyName("no_error_compounding")]
        public bool NoErrorCompounding { get; set; }

        [JsonPropertyName("input_days_used")]
        public int InputDaysUsed { get; set; }
    }

    public class PricePredictResponse
    {
        [JsonPropertyName("predicted_price")]
        public double PredictedPrice { get; set; }

        [JsonPropertyName("current_price")]
        public double CurrentPrice { get; set; }

        [JsonPropertyName("change_pct")]
        public double ChangePct { get; set; }

        [JsonPropertyName("trend")]
        public string Trend { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("expected_mape")]
        public double ExpectedMape { get; set; }
    }

    // Updated: new fields from API v2.2
    public class MultiStepItem
    {
        [JsonPropertyName("day")]
        public string Day { get; set; }

        [JsonPropertyName("day_number")]
        public int DayNumber { get; set; }

        [JsonPropertyName("predicted")]
        public double Predicted { get; set; }

        [JsonPropertyName("change_pct")]
        public double ChangePct { get; set; }

        [JsonPropertyName("trend")]
        public string Trend { get; set; }

        [JsonPropertyName("expected_mape")]
        public double ExpectedMape { get; set; }
    }

    // New: summary block returned by multistep API
    public class MultiStepSummary
    {
        [JsonPropertyName("avg_price")]
        public double AvgPrice { get; set; }

        [JsonPropertyName("min_price")]
        public double MinPrice { get; set; }

        [JsonPropertyName("max_price")]
        public double MaxPrice { get; set; }

        [JsonPropertyName("overall_trend")]
        public string OverallTrend { get; set; }

        [JsonPropertyName("overall_change_pct")]
        public double OverallChangePct { get; set; }
    }

    // Updated: new fields from API v2.2
    public class MultiStepResponse
    {
        [JsonPropertyName("current_price")]
        public double CurrentPrice { get; set; }

        [JsonPropertyName("horizon_days")]
        public int HorizonDays { get; set; }

        [JsonPropertyName("forecast")]
        public List<MultiStepItem> Forecast { get; set; }

        [JsonPropertyName("summary")]
        public MultiStepSummary Summary { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("model_version")]
        public string ModelVersion { get; set; }

        [JsonPropertyName("strategy")]
        public string Strategy { get; set; }

        [JsonPropertyName("expected_mape")]
        public double ExpectedMape { get; set; }
    }

    public class AnomalyResponse
    {
        [JsonPropertyName("grade")]
        public string Grade { get; set; }

        [JsonPropertyName("is_anomaly")]
        public bool IsAnomaly { get; set; }

        [JsonPropertyName("severity")]
        public string Severity { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("color")]
        public string Color { get; set; }

        [JsonPropertyName("score")]
        public double Score { get; set; }

        [JsonPropertyName("model_triggered")]
        public bool ModelTriggered { get; set; }

        [JsonPropertyName("rules_triggered")]
        public List<object> RulesTriggered { get; set; }
    }


    // ============================================================
    // THE MAIN SERVICE CLASS
    // ============================================================

    public class AiPredictionService
    {
        private readonly HttpClient _http;
        private readonly string _apiUrl;
        private readonly ILogger<AiPredictionService> _logger;

        public AiPredictionService(
            HttpClient http,
            IOptions<AiServiceOptions> options,
            ILogger<AiPredictionService> logger)
        {
            _http = http;
            _logger = logger;
            var configured = options.Value;
            _apiUrl = configured.BaseUrl.TrimEnd('/');
            _http.Timeout = TimeSpan.FromSeconds(Math.Clamp(configured.TimeoutSeconds, 5, 300));
        }

        // ---- Helper: POST JSON and return typed response ----
        private async Task<T?> PostAsync<T>(
            string endpoint, object data)
        {
            try
            {
                var timer = Stopwatch.StartNew();
                _logger.LogInformation("AI request started: {Endpoint}", endpoint);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                };
                var json    = JsonSerializer.Serialize(data, options);
                var content = new StringContent(
                    json, Encoding.UTF8, "application/json");
                var resp    = await _http.PostAsync(
                    $"{_apiUrl}{endpoint}", content);
                var body    = await resp.Content
                    .ReadAsStringAsync();
                timer.Stop();
                _logger.LogInformation(
                    "AI response received: {Endpoint}; status {StatusCode}; elapsed {ElapsedMs} ms",
                    endpoint, (int)resp.StatusCode, timer.ElapsedMilliseconds);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "AI request rejected: {Endpoint}; status {StatusCode}; response {ResponseBody}",
                        endpoint, (int)resp.StatusCode, body.Length > 1000 ? body[..1000] : body);
                    return default;
                }

                return JsonSerializer.Deserialize<T>(body,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI request failed: {Endpoint}", endpoint);
                return default;
            }
        }

        // ---- Check if Python AI server is running ----
        public async Task<bool> IsAvailable()
        {
            try
            {
                var r = await _http.GetAsync($"{_apiUrl}/health");
                return r.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "AI health check failed for {BaseUrl}", _apiUrl);
                return false;
            }
        }

        public async Task<JsonElement?> GetModelInfo()
        {
            try
            {
                var response = await _http.GetAsync($"{_apiUrl}/models/info");
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogInformation(
                    "AI model metadata response: status {StatusCode}", (int)response.StatusCode);
                if (!response.IsSuccessStatusCode)
                    return null;
                using var document = JsonDocument.Parse(body);
                return document.RootElement.Clone();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI model metadata request failed");
                return null;
            }
        }

        // ---- FEATURE 1: Demand forecast (30/45/60 days) ----
        // Now takes 60 days of history and a horizon choice
        public async Task<DemandForecastResponse?>
            GetDemandForecast(
                string grade,
                List<double> last60Days,
                int horizonDays = 30)
        {
            return await PostAsync<DemandForecastResponse>(
                "/predict/demand",
                new DemandForecastRequest
                {
                    Grade             = grade,
                    Last60DaysDemand  = last60Days,
                    HorizonDays       = horizonDays
                });
        }

        // ---- FEATURE 2: Tomorrow's price (unchanged) ----
        public async Task<PricePredictResponse?>
            GetTomorrowPrice(PricePredictRequest req)
        {
            return await PostAsync<PricePredictResponse>(
                "/predict/price", req);
        }

        // ---- FEATURE 3: Multi-step price (7/14/30 days) ----
        // Renamed from Get7DayForecast
        // Now accepts horizonDays parameter
        public async Task<MultiStepResponse?>
            GetMultiStepForecast(
                PricePredictRequest req,
                int horizonDays = 7)
        {
            req.HorizonDays = horizonDays;
            return await PostAsync<MultiStepResponse>(
                "/predict/price/multistep", req);
        }

        // ---- FEATURE 4: Anomaly detection (unchanged) ----
        public async Task<AnomalyResponse?>
            CheckAnomaly(AnomalyCheckRequest req)
        {
            return await PostAsync<AnomalyResponse>(
                "/predict/anomaly", req);
        }
    }
}
