using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TeaOnlineShop.Authorization;
using TeaOnlineShop.Models.Dbase;
using TeaOnlineShop.Models.ViewModels;
using TeaOnlineShop.Services;

namespace TeaOnlineShop.Areas.Admin.Controllers
{
    public class AiDashboardController : AdminBaseController
    {
        private static readonly string[] TeaGrades = { "BOP", "BOPF", "DUST", "FNGS", "OP" };

        private readonly AiPredictionService _ai;
        private readonly AiDashboardHistoryService _history;
        private readonly DemandHistoryService _demandHistory;
        private readonly AiPredictionHistoryService _predictionHistory;
        private readonly PdfService _pdf;
        private readonly ILogger<AiDashboardController> _logger;

        public AiDashboardController(
            AiPredictionService ai,
            AiDashboardHistoryService history,
            DemandHistoryService demandHistory,
            AiPredictionHistoryService predictionHistory,
            PdfService pdf,
            ILogger<AiDashboardController> logger)
        {
            _ai = ai;
            _history = history;
            _demandHistory = demandHistory;
            _predictionHistory = predictionHistory;
            _pdf = pdf;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Policy = AppPermissions.DashboardFinancialView)]
        public async Task<IActionResult> Index()
        {
            var statusTask = GetAiStatusAsync();
            var demandTask = _history.GetDemandAsync(12);
            var priceTask = _history.GetPriceAsync(12);
            var alertsTask = _history.GetAlertsAsync(1000);

            await Task.WhenAll(statusTask, demandTask, priceTask, alertsTask);

            var status = await statusTask;
            var demandHistory = await demandTask;
            var priceHistory = await priceTask;
            var alerts = await alertsTask;

            var model = new AiDashboardOverviewViewModel
            {
                AiAvailable = status.Available,
                AiMessage = status.Message,
                GeneratedAtUtc = DateTimeOffset.UtcNow,
                LatestDemandForecast = demandHistory.FirstOrDefault(),
                LatestPriceForecast = priceHistory.FirstOrDefault(),
                AnomalyAlertCount = alerts.Count,
                WarningAlertCount = alerts.Count(a =>
                    string.Equals(a.Severity, "WARNING", StringComparison.OrdinalIgnoreCase)),
                CriticalAlertCount = alerts.Count(a =>
                    string.Equals(a.Severity, "CRITICAL", StringComparison.OrdinalIgnoreCase)),
                RecentAlerts = alerts.Take(5).ToList(),
                DemandHistory = demandHistory,
                PriceHistory = priceHistory
            };

            model.Insights = BuildInsights(model);
            return View(model);
        }

        [HttpGet]
        [Authorize(Policy = AppPermissions.AiOperationalView)]
        public async Task<IActionResult> DemandForecast()
        {
            var status = await GetAiStatusAsync();
            return View(new AiDemandForecastPageViewModel
            {
                AiAvailable = status.Available,
                AiMessage = status.Message,
                TeaGrades = TeaGrades.ToList(),
                ForecastHorizons = new List<int> { 30, 45, 60 },
                History = await _history.GetDemandAsync(50)
            });
        }

        [HttpGet]
        [Authorize(Policy = AppPermissions.DashboardFinancialView)]
        public async Task<IActionResult> GreenLeafPriceForecast()
        {
            var status = await GetAiStatusAsync();
            return View(new AiPriceForecastPageViewModel
            {
                AiAvailable = status.Available,
                AiMessage = status.Message,
                ForecastHorizons = new List<int> { 7, 14, 30 },
                History = await _history.GetPriceAsync(25)
            });
        }

        [HttpGet]
        [Authorize(Policy = AppPermissions.AiOperationalView)]
        public async Task<IActionResult> AnomalyDetection()
        {
            var status = await GetAiStatusAsync();
            return View(new AiAnomalyPageViewModel
            {
                AiAvailable = status.Available,
                AiMessage = status.Message,
                TeaGrades = TeaGrades.ToList(),
                RecentAlerts = (await _history.GetAlertsAsync(5)).ToList()
            });
        }

        [HttpGet]
        [Authorize(Policy = AppPermissions.AiOperationalView)]
        public async Task<IActionResult> AlertsHistory(string? severity = null, string? grade = null)
        {
            var allAlerts = await _history.GetAlertsAsync(1000);
            var filtered = allAlerts.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(severity))
            {
                filtered = filtered.Where(a =>
                    string.Equals(a.Severity, severity, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(grade))
            {
                filtered = filtered.Where(a =>
                    string.Equals(a.Grade, grade, StringComparison.OrdinalIgnoreCase));
            }

            return View(new AiAlertsHistoryViewModel
            {
                Alerts = filtered.ToList(),
                SeverityFilter = severity ?? string.Empty,
                GradeFilter = grade ?? string.Empty,
                TeaGrades = TeaGrades.ToList(),
                TotalAlerts = allAlerts.Count,
                WarningAlerts = allAlerts.Count(a =>
                    string.Equals(a.Severity, "WARNING", StringComparison.OrdinalIgnoreCase)),
                CriticalAlerts = allAlerts.Count(a =>
                    string.Equals(a.Severity, "CRITICAL", StringComparison.OrdinalIgnoreCase))
            });
        }

        [HttpGet]
        [Authorize(Policy = AppPermissions.AiOperationalView)]
        public async Task<IActionResult> ExportDemandPdf(Guid id)
        {
            var record = (await _history.GetDemandAsync(1000)).FirstOrDefault(x => x.Id == id);
            if (record == null)
            {
                return NotFound("Demand forecast history record not found.");
            }

            var bytes = await _pdf.GenerateDemandForecastReport(record);
            return File(bytes, "application/pdf",
                $"Demand_Forecast_{record.Grade}_{record.TimestampUtc:yyyyMMdd_HHmm}.pdf");
        }

        [HttpGet]
        [Authorize(Policy = AppPermissions.AiOperationalView)]
        public async Task<IActionResult> ExportAlertsCsv(string? severity = null, string? grade = null)
        {
            var alerts = await _history.GetAlertsAsync(1000);
            var filtered = alerts.Where(a =>
                    string.IsNullOrWhiteSpace(severity) ||
                    string.Equals(a.Severity, severity, StringComparison.OrdinalIgnoreCase))
                .Where(a =>
                    string.IsNullOrWhiteSpace(grade) ||
                    string.Equals(a.Grade, grade, StringComparison.OrdinalIgnoreCase));

            var csv = new StringBuilder();
            csv.AppendLine("Detected,Grade,Severity,Demand (kg),Stock (kg),Price (LKR/kg),Score,ML Flagged,Message,User");
            foreach (var alert in filtered)
            {
                csv.AppendLine(string.Join(",", new[]
                {
                    Csv(alert.TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")),
                    Csv(alert.Grade),
                    Csv(alert.Severity),
                    alert.DemandKg.ToString("0.##"),
                    alert.StockLevelKg.ToString("0.##"),
                    alert.PricePerKg.ToString("0.##"),
                    alert.Score.ToString("0.####"),
                    alert.ModelTriggered.ToString(),
                    Csv(alert.Message),
                    Csv(alert.UserName)
                }));
            }

            return File(new UTF8Encoding(true).GetBytes(csv.ToString()), "text/csv",
                $"AI_Alerts_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }

        [HttpPost]
        [Authorize(Policy = AppPermissions.AiRunPredictions)]
        public async Task<IActionResult> GetDemandForecast([FromBody] DemandForecastRequest req)
        {
            try
            {
                if (req == null || string.IsNullOrWhiteSpace(req.Grade))
                {
                    return Json(new { success = false, message = "Please provide a tea grade." });
                }

                if (!TeaGrades.Contains(req.Grade, StringComparer.OrdinalIgnoreCase))
                {
                    return Json(new { success = false, message = "Unsupported tea grade." });
                }

                // Demand forecasting currently uses the trained research dataset.
                // Resolve it on the server so provenance and input values cannot be
                // changed by a modified browser request.
                var trustedHistory = await _demandHistory.GetResearchAsync(
                    req.Grade,
                    AiForecastRules.DemandInputWindowDays,
                    HttpContext.RequestAborted);

                if (!trustedHistory.Sufficient || trustedHistory.History.Count != AiForecastRules.DemandInputWindowDays)
                {
                    return Json(new
                    {
                        success = false,
                        message = trustedHistory.Message
                    });
                }

                // Never trust client-provided values or provenance labels for the audit trail.
                req.Last60DaysDemand = trustedHistory.History.ToList();
                req.DataSource = trustedHistory.DataSource;
                req.SourceLabel = trustedHistory.SourceLabel;
                req.SourceNote = trustedHistory.SourceNote;
                req.SourceStartDate = trustedHistory.StartDate?.ToString("yyyy-MM-dd") ?? string.Empty;
                req.SourceEndDate = trustedHistory.EndDate?.ToString("yyyy-MM-dd") ?? string.Empty;

                if (!AiForecastRules.IsSupportedDemandHorizon(req.HorizonDays))
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Horizon must be 30, 45, or 60. Got: {req.HorizonDays}"
                    });
                }

                var result = await _ai.GetDemandForecast(
                    req.Grade, req.Last60DaysDemand, req.HorizonDays);

                if (result == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "AI server unavailable. Please check Python API is running."
                    });
                }

                var record = new AiDemandForecastHistoryRecord
                {
                    UserName = CurrentUserName(),
                    Grade = result.Grade,
                    HorizonDays = result.HorizonDays > 0 ? result.HorizonDays : req.HorizonDays,
                    Last60DaysDemand = req.Last60DaysDemand.ToList(),
                    Predictions = result.Predictions?.ToList() ?? new List<double>(),
                    Model = result.Model,
                    ModelVersion = result.ModelVersion,
                    Strategy = result.Strategy,
                    ExpectedMape = result.ExpectedMape,
                    DataSource = req.DataSource,
                    SourceLabel = req.SourceLabel,
                    SourceNote = req.SourceNote,
                    SourceStartDate = DateOnly.TryParse(req.SourceStartDate, out var sourceStart) ? sourceStart : null,
                    SourceEndDate = DateOnly.TryParse(req.SourceEndDate, out var sourceEnd) ? sourceEnd : null
                };
                await TryAppendAsync(() => _history.AppendDemandAsync(record), "demand forecast");
                await TryAppendPredictionAsync(new AiPredictionHistory
                {
                    PublicId = record.Id,
                    PredictionType = "Demand",
                    Grade = record.Grade,
                    HorizonDays = record.HorizonDays,
                    RequestedByUserId = CurrentUserId(),
                    RequestedByName = CurrentUserName(),
                    RequestedAtUtc = record.TimestampUtc.UtcDateTime,
                    Model = record.Model,
                    ModelVersion = record.ModelVersion,
                    Strategy = record.Strategy,
                    ExpectedMape = Convert.ToDecimal(record.ExpectedMape),
                    DataSource = string.IsNullOrWhiteSpace(record.DataSource) ? "unknown" : record.DataSource,
                    SourceLabel = string.IsNullOrWhiteSpace(record.SourceLabel) ? "Unspecified source" : record.SourceLabel,
                    SourceNote = record.SourceNote,
                    SourceStartDateUtc = record.SourceStartDate?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                    SourceEndDateUtc = record.SourceEndDate?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                    InputSummary = $"60 daily {record.Grade} observations; min {record.Last60DaysDemand.Min():0.##} kg; max {record.Last60DaysDemand.Max():0.##} kg; average {record.Last60DaysDemand.Average():0.##} kg.",
                    ResultJson = JsonSerializer.Serialize(new { predictions = record.Predictions }),
                    Status = "Succeeded"
                });

                return Json(new
                {
                    success = true,
                    grade = result.Grade,
                    horizonDays = record.HorizonDays,
                    predictions = result.Predictions,
                    model = result.Model,
                    modelVersion = result.ModelVersion,
                    strategy = result.Strategy,
                    mape = result.ExpectedMape,
                    inputHistory = record.Last60DaysDemand,
                    dataSource = record.DataSource,
                    sourceLabel = record.SourceLabel,
                    sourceNote = record.SourceNote,
                    sourceStartDate = record.SourceStartDate?.ToString("yyyy-MM-dd"),
                    sourceEndDate = record.SourceEndDate?.ToString("yyyy-MM-dd"),
                    historyId = record.Id,
                    generatedAt = record.TimestampUtc
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Demand forecast failed");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Policy = AppPermissions.AiRunPredictions)]
        public async Task<IActionResult> GetDemandHistory(
            string grade,
            int days = 60,
            CancellationToken cancellationToken = default)
        {
            if (!TeaGrades.Contains(grade, StringComparer.OrdinalIgnoreCase))
                return BadRequest(new { sufficient = false, message = "Unsupported tea grade." });

            var result = await _demandHistory.GetResearchAsync(grade, days, cancellationToken);

            return Json(new
            {
                sufficient = result.Sufficient,
                grade = result.Grade,
                daysAvailable = result.DaysAvailable,
                daysRequired = result.DaysRequired,
                dataSource = result.DataSource,
                sourceLabel = result.SourceLabel,
                sourceNote = result.SourceNote,
                startDate = result.StartDate?.ToString("yyyy-MM-dd"),
                endDate = result.EndDate?.ToString("yyyy-MM-dd"),
                dates = result.Dates,
                history = result.History,
                message = result.Message
            });
        }

        [HttpGet]
        [Authorize(Policy = AppPermissions.AiOperationalView)]
        public async Task<IActionResult> GetModelInfo()
        {
            var modelInfo = await _ai.GetModelInfo();
            return modelInfo.HasValue
                ? Json(new { success = true, models = modelInfo.Value })
                : Json(new { success = false, message = "AI model metadata is unavailable." });
        }

        [HttpGet]
        [Authorize(Policy = AppPermissions.AiOperationalView)]
        public async Task<IActionResult> DemandEvaluation(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var prediction = await _predictionHistory.FindAsync(id, cancellationToken);
            if (prediction is null || !string.Equals(prediction.PredictionType, "Demand", StringComparison.Ordinal))
                return NotFound("Demand prediction history record not found.");
            if (!prediction.SourceEndDateUtc.HasValue)
                return BadRequest("This prediction does not contain a source cutoff date.");

            using var resultDocument = JsonDocument.Parse(prediction.ResultJson);
            if (!resultDocument.RootElement.TryGetProperty("predictions", out var predictionElement))
                return BadRequest("The stored prediction result is invalid.");
            var predicted = predictionElement.EnumerateArray().Select(x => x.GetDouble()).ToList();
            var forecastStart = DateOnly.FromDateTime(prediction.SourceEndDateUtc.Value).AddDays(1);
            var actuals = await _demandHistory.GetActualsAsync(
                prediction.Grade,
                prediction.DataSource,
                forecastStart,
                predicted.Count,
                cancellationToken);

            var model = new DemandForecastEvaluationViewModel
            {
                PredictionId = prediction.PublicId,
                Grade = prediction.Grade,
                HorizonDays = prediction.HorizonDays,
                RequestedAtUtc = prediction.RequestedAtUtc,
                Model = prediction.Model,
                ModelVersion = prediction.ModelVersion,
                ExpectedMape = prediction.ExpectedMape,
                DataSource = prediction.DataSource,
                SourceLabel = string.Equals(
                    prediction.DataSource,
                    "synthetic_research_dataset",
                    StringComparison.OrdinalIgnoreCase)
                    ? "Research dataset"
                    : prediction.SourceLabel,
                SourceNote = string.Equals(
                    prediction.DataSource,
                    "synthetic_research_dataset",
                    StringComparison.OrdinalIgnoreCase)
                    ? "Research dataset used by the trained demand forecasting model."
                    : prediction.SourceNote
            };

            for (var index = 0; index < predicted.Count; index++)
            {
                var actual = actuals[index].ActualDemandKg;
                double? error = actual.HasValue && actual.Value != 0
                    ? Math.Abs((actual.Value - predicted[index]) / actual.Value) * 100d
                    : null;
                model.Rows.Add(new DemandForecastEvaluationRow
                {
                    DayNumber = index + 1,
                    Date = actuals[index].Date,
                    PredictedKg = predicted[index],
                    ActualKg = actual,
                    AbsolutePercentageError = error
                });
            }

            model.CompletedDays = model.Rows.Count(x => x.ActualKg.HasValue);
            model.ActualMape = ForecastMetrics.MeanAbsolutePercentageError(
                model.Rows.Select(x => (x.PredictedKg, x.ActualKg)));
            return View(model);
        }

        [HttpPost]
        [Authorize(Policy = AppPermissions.AiRunPredictions)]
        public async Task<IActionResult> GetTomorrowPrice([FromBody] PricePredictRequest req)
        {
            try
            {
                var result = await _ai.GetTomorrowPrice(req);
                if (result == null)
                {
                    return Json(new { success = false, message = "AI server unavailable." });
                }

                var record = CreatePriceHistoryRecord(req);
                record.ForecastType = "Tomorrow";
                record.HorizonDays = 1;
                record.PredictedPrice = result.PredictedPrice;
                record.ChangePct = result.ChangePct;
                record.Trend = result.Trend;
                record.Model = result.Model;
                record.ExpectedMape = result.ExpectedMape;
                await TryAppendAsync(() => _history.AppendPriceAsync(record), "tomorrow price forecast");
                await TryAppendPredictionAsync(CreatePriceAudit(
                    record,
                    JsonSerializer.Serialize(new
                    {
                        predictedPrice = result.PredictedPrice,
                        changePct = result.ChangePct,
                        trend = result.Trend
                    })));

                return Json(new
                {
                    success = true,
                    predictedPrice = result.PredictedPrice,
                    currentPrice = result.CurrentPrice,
                    changePct = result.ChangePct,
                    trend = result.Trend,
                    model = result.Model,
                    mape = result.ExpectedMape,
                    historyId = record.Id,
                    generatedAt = record.TimestampUtc
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tomorrow price forecast failed");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Policy = AppPermissions.AiRunPredictions)]
        public async Task<IActionResult> GetMultiStepForecast([FromBody] PricePredictRequest req)
        {
            try
            {
                var allowed = new[] { 7, 14, 30 };
                if (!allowed.Contains(req.HorizonDays))
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Horizon must be 7, 14, or 30. Got: {req.HorizonDays}"
                    });
                }

                var result = await _ai.GetMultiStepForecast(req, req.HorizonDays);
                if (result == null)
                {
                    return Json(new { success = false, message = "AI server unavailable." });
                }

                var record = CreatePriceHistoryRecord(req);
                record.ForecastType = "MultiDay";
                record.HorizonDays = result.HorizonDays > 0 ? result.HorizonDays : req.HorizonDays;
                record.Forecast = (result.Forecast ?? new List<MultiStepItem>()).Select(item =>
                    new AiPriceForecastPoint
                    {
                        Day = item.Day,
                        DayNumber = item.DayNumber,
                        Predicted = item.Predicted,
                        ChangePct = item.ChangePct,
                        Trend = item.Trend,
                        ExpectedMape = item.ExpectedMape
                    }).ToList();
                if (result.Summary != null)
                {
                    record.Summary = new AiPriceForecastSummary
                    {
                        AvgPrice = result.Summary.AvgPrice,
                        MinPrice = result.Summary.MinPrice,
                        MaxPrice = result.Summary.MaxPrice,
                        OverallTrend = result.Summary.OverallTrend,
                        OverallChangePct = result.Summary.OverallChangePct
                    };
                }
                record.Model = result.Model;
                record.ModelVersion = result.ModelVersion;
                record.Strategy = result.Strategy;
                record.ExpectedMape = result.ExpectedMape;
                await TryAppendAsync(() => _history.AppendPriceAsync(record), "multi-day price forecast");
                await TryAppendPredictionAsync(CreatePriceAudit(
                    record,
                    JsonSerializer.Serialize(new { forecast = record.Forecast, summary = record.Summary })));

                return Json(new
                {
                    success = true,
                    currentPrice = result.CurrentPrice,
                    horizonDays = result.HorizonDays,
                    forecast = result.Forecast,
                    summary = result.Summary,
                    model = result.Model,
                    modelVersion = result.ModelVersion,
                    expectedMape = result.ExpectedMape,
                    historyId = record.Id,
                    generatedAt = record.TimestampUtc
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Multi-day price forecast failed");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Policy = AppPermissions.AiRunPredictions)]
        public async Task<IActionResult> Get7DayForecast([FromBody] PricePredictRequest req)
        {
            req.HorizonDays = 7;
            return await GetMultiStepForecast(req);
        }

        [HttpPost]
        [Authorize(Policy = AppPermissions.AiRunPredictions)]
        public async Task<IActionResult> CheckAnomaly([FromBody] AnomalyCheckRequest req)
        {
            try
            {
                var result = await _ai.CheckAnomaly(req);
                if (result == null)
                {
                    return Json(new { success = false, message = "AI server unavailable." });
                }

                AiAlertHistoryRecord? record = null;
                if (result.IsAnomaly)
                {
                    record = new AiAlertHistoryRecord
                    {
                        UserName = CurrentUserName(),
                        Grade = result.Grade ?? req.Grade,
                        DemandKg = req.DemandKg,
                        StockLevelKg = req.StockLevelKg,
                        PricePerKg = req.PricePerKg,
                        DayOfWeek = req.DayOfWeek,
                        Month = req.Month,
                        IsWeekend = req.IsWeekend,
                        IsAnomaly = true,
                        Severity = result.Severity,
                        Message = result.Message,
                        Color = result.Color,
                        Score = result.Score,
                        ModelTriggered = result.ModelTriggered,
                        RulesTriggered = MapRules(result.RulesTriggered),
                        Model = "Isolation Forest + Business Rules",
                        ModelVersion = "2.0"
                    };
                    await TryAppendAsync(() => _history.AppendAlertAsync(record), "anomaly alert");
                }

                await TryAppendPredictionAsync(new AiPredictionHistory
                {
                    PublicId = record?.Id ?? Guid.NewGuid(),
                    PredictionType = "Anomaly",
                    Grade = result.Grade ?? req.Grade,
                    HorizonDays = 0,
                    RequestedByUserId = CurrentUserId(),
                    RequestedByName = CurrentUserName(),
                    Model = "Isolation Forest + Business Rules",
                    ModelVersion = "2.0",
                    Strategy = "hybrid_ml_rules",
                    DataSource = "operator_input",
                    SourceLabel = "Operator-entered operational check",
                    SourceNote = "Values entered on the anomaly detection page.",
                    InputSummary = $"Demand {req.DemandKg:0.##} kg; stock {req.StockLevelKg:0.##} kg; price LKR {req.PricePerKg:0.##}.",
                    ResultJson = JsonSerializer.Serialize(new
                    {
                        result.IsAnomaly,
                        result.Severity,
                        result.Message,
                        result.Score,
                        result.ModelTriggered,
                        result.RulesTriggered
                    }),
                    Status = "Succeeded"
                });

                return Json(new
                {
                    success = true,
                    isAnomaly = result.IsAnomaly,
                    severity = result.Severity,
                    message = result.Message,
                    color = result.Color,
                    score = result.Score,
                    modelTriggered = result.ModelTriggered,
                    rulesTriggered = result.RulesTriggered,
                    alertId = record?.Id,
                    detectedAt = record?.TimestampUtc
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Anomaly check failed");
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task<(bool Available, string Message)> GetAiStatusAsync()
        {
            var available = await _ai.IsAvailable();
            return available
                ? (true, string.Empty)
                : (false, "AI Server is not running. Start the Python API to enable AI features.");
        }

        private string CurrentUserName() => User.Identity?.Name ?? "Admin";

        private int? CurrentUserId() =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        private async Task TryAppendAsync(Func<Task> append, string recordType)
        {
            try
            {
                await append();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI prediction succeeded but {RecordType} history could not be saved", recordType);
            }
        }

        private async Task TryAppendPredictionAsync(AiPredictionHistory record)
        {
            try
            {
                await _predictionHistory.AppendAsync(record);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "AI prediction succeeded but the immutable SQL audit record could not be saved for {PredictionType}",
                    record.PredictionType);
            }
        }

        private AiPredictionHistory CreatePriceAudit(
            AiPriceForecastHistoryRecord record,
            string resultJson) => new()
        {
            PublicId = record.Id,
            PredictionType = "Price",
            HorizonDays = record.HorizonDays,
            RequestedByUserId = CurrentUserId(),
            RequestedByName = CurrentUserName(),
            RequestedAtUtc = record.TimestampUtc.UtcDateTime,
            Model = record.Model,
            ModelVersion = record.ModelVersion,
            Strategy = record.Strategy,
            ExpectedMape = Convert.ToDecimal(record.ExpectedMape),
            DataSource = "operator_scenario",
            SourceLabel = "Dashboard price scenario inputs",
            SourceNote = "Current price, recent price, weather and dashboard-engineered scenario features.",
            InputSummary = $"Current LKR {record.CurrentPrice:0.##}; previous LKR {record.PriceLag1:0.##}; rainfall {record.RainfallMm:0.##} mm; temperature {record.Temperature:0.##} C.",
            ResultJson = resultJson,
            Status = "Succeeded"
        };

        private AiPriceForecastHistoryRecord CreatePriceHistoryRecord(PricePredictRequest req)
        {
            return new AiPriceForecastHistoryRecord
            {
                UserName = CurrentUserName(),
                CurrentPrice = req.CurrentPrice,
                PriceLag1 = req.PriceLag1,
                PriceLag2 = req.PriceLag2,
                PriceLag3 = req.PriceLag3,
                PriceLag7 = req.PriceLag7,
                PriceLag14 = req.PriceLag14,
                RollingMean7 = req.RollingMean7,
                RollingMean30 = req.RollingMean30,
                RollingStd7 = req.RollingStd7,
                PriceChangePct = req.PriceChangePct,
                QuantityKg = req.QuantityKg,
                QtyRolling7 = req.QtyRolling7,
                FirewoodKg = req.FirewoodKg,
                FirewoodCost = req.FirewoodCost,
                TotalCost = req.TotalCost,
                Temperature = req.Temperature,
                RainfallMm = req.RainfallMm,
                HeavyRain = req.HeavyRain,
                SupplierDelivered = req.SupplierDelivered,
                SupplierQty = req.SupplierQty,
                Promotion = req.Promotion,
                MonthStart = req.MonthStart,
                Month = req.Month,
                DayOfWeek = req.DayOfWeek,
                Quarter = req.Quarter,
                IsWeekend = req.IsWeekend,
                DayOfYear = req.DayOfYear,
                Day = req.Day
            };
        }

        private static List<AiAlertRuleHistory> MapRules(IEnumerable<object>? source)
        {
            var mapped = new List<AiAlertRuleHistory>();
            if (source == null)
            {
                return mapped;
            }

            foreach (var item in source)
            {
                if (item is JsonElement element && element.ValueKind == JsonValueKind.Object)
                {
                    mapped.Add(new AiAlertRuleHistory
                    {
                        Rule = GetJsonString(element, "rule"),
                        Message = GetJsonString(element, "message")
                    });
                }
                else
                {
                    mapped.Add(new AiAlertRuleHistory { Message = item?.ToString() ?? string.Empty });
                }
            }

            return mapped;
        }

        private static string GetJsonString(JsonElement element, string name)
        {
            return element.TryGetProperty(name, out var property)
                ? property.ToString()
                : string.Empty;
        }

        private static List<string> BuildInsights(AiDashboardOverviewViewModel model)
        {
            var insights = new List<string>();
            insights.Add(model.AiAvailable
                ? "All AI prediction services are reachable."
                : "AI predictions are temporarily unavailable until the Python service is started.");

            var demand = model.LatestDemandForecast;
            if (demand?.Predictions.Count > 0)
            {
                var average = demand.Predictions.Average();
                var peak = demand.Predictions.Max();
                var peakDay = demand.Predictions.IndexOf(peak) + 1;
                insights.Add($"{demand.Grade} demand averages {average:0.0} kg/day and peaks at {peak:0.0} kg on day {peakDay}.");
            }
            else
            {
                insights.Add("Generate a demand forecast to establish the first demand insight.");
            }

            var price = model.LatestPriceForecast;
            if (price != null)
            {
                var trend = price.Summary?.OverallTrend ?? price.Trend;
                var priceValue = price.Summary?.AvgPrice ?? price.PredictedPrice;
                insights.Add(priceValue.HasValue
                    ? $"Latest green leaf price outlook is {trend.ToLowerInvariant()} at about LKR {priceValue.Value:0.00}/kg."
                    : $"Latest green leaf price outlook is {trend.ToLowerInvariant()}.");
            }
            else
            {
                insights.Add("Generate a green leaf price forecast to establish the first price insight.");
            }

            insights.Add(model.CriticalAlertCount > 0
                ? $"{model.CriticalAlertCount} critical anomaly alert(s) require attention."
                : "There are no recorded critical anomaly alerts.");

            return insights;
        }

        private static string Csv(string? value) =>
            $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
    }
}
