using System.Text;
using System.Text.Json;
using TeaOnlineShop.Models.ViewModels;

namespace TeaOnlineShop.Services
{
    public sealed class AiDashboardHistoryService
    {
        private const int DefaultReadCount = 100;
        private const int MaximumReadCount = 1000;

        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        private readonly string _historyDirectory;
        private readonly string _demandFilePath;
        private readonly string _priceFilePath;
        private readonly string _alertsFilePath;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly SemaphoreSlim _demandLock = new(1, 1);
        private readonly SemaphoreSlim _priceLock = new(1, 1);
        private readonly SemaphoreSlim _alertsLock = new(1, 1);

        public AiDashboardHistoryService(IWebHostEnvironment environment)
        {
            ArgumentNullException.ThrowIfNull(environment);

            _historyDirectory = Path.Combine(
                environment.ContentRootPath,
                "App_Data",
                "ai-history");

            _demandFilePath = Path.Combine(_historyDirectory, "demand.jsonl");
            _priceFilePath = Path.Combine(_historyDirectory, "price.jsonl");
            _alertsFilePath = Path.Combine(_historyDirectory, "alerts.jsonl");

            _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = false
            };
        }

        public string HistoryDirectory => _historyDirectory;

        public Task AppendDemandAsync(
            AiDemandForecastHistoryRecord record,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(record);
            Prepare(record);

            return AppendAsync(
                _demandFilePath,
                record,
                _demandLock,
                cancellationToken);
        }

        public Task AppendPriceAsync(
            AiPriceForecastHistoryRecord record,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(record);
            Prepare(record);

            return AppendAsync(
                _priceFilePath,
                record,
                _priceLock,
                cancellationToken);
        }

        public Task AppendAlertAsync(
            AiAlertHistoryRecord record,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(record);
            Prepare(record);

            return AppendAsync(
                _alertsFilePath,
                record,
                _alertsLock,
                cancellationToken);
        }

        public Task<List<AiDemandForecastHistoryRecord>> GetDemandAsync(
            int max = DefaultReadCount,
            CancellationToken cancellationToken = default) =>
            ReadLatestAsync<AiDemandForecastHistoryRecord>(
                _demandFilePath,
                _demandLock,
                max,
                cancellationToken);

        public Task<List<AiPriceForecastHistoryRecord>> GetPriceAsync(
            int max = DefaultReadCount,
            CancellationToken cancellationToken = default) =>
            ReadLatestAsync<AiPriceForecastHistoryRecord>(
                _priceFilePath,
                _priceLock,
                max,
                cancellationToken);

        public Task<List<AiAlertHistoryRecord>> GetAlertsAsync(
            int max = DefaultReadCount,
            CancellationToken cancellationToken = default) =>
            ReadLatestAsync<AiAlertHistoryRecord>(
                _alertsFilePath,
                _alertsLock,
                max,
                cancellationToken);

        private async Task AppendAsync<T>(
            string filePath,
            T record,
            SemaphoreSlim fileLock,
            CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(record, _jsonOptions);

            await fileLock.WaitAsync(cancellationToken);
            try
            {
                Directory.CreateDirectory(_historyDirectory);

                await using var stream = new FileStream(
                    filePath,
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.Read,
                    bufferSize: 4096,
                    useAsync: true);
                await using var writer = new StreamWriter(
                    stream,
                    Utf8WithoutBom,
                    bufferSize: 4096,
                    leaveOpen: false);

                await writer.WriteLineAsync(json.AsMemory(), cancellationToken);
                await writer.FlushAsync(cancellationToken);
            }
            finally
            {
                fileLock.Release();
            }
        }

        private async Task<List<T>> ReadLatestAsync<T>(
            string filePath,
            SemaphoreSlim fileLock,
            int max,
            CancellationToken cancellationToken)
        {
            var readCount = Math.Clamp(max, 0, MaximumReadCount);
            if (readCount == 0)
            {
                return new List<T>();
            }

            await fileLock.WaitAsync(cancellationToken);
            try
            {
                if (!File.Exists(filePath))
                {
                    return new List<T>();
                }

                var latest = new Queue<T>(readCount);

                await using var stream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 4096,
                    useAsync: true);
                using var reader = new StreamReader(
                    stream,
                    Utf8WithoutBom,
                    detectEncodingFromByteOrderMarks: true,
                    bufferSize: 4096,
                    leaveOpen: false);

                while (await reader.ReadLineAsync(cancellationToken) is { } line)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    T? record;
                    try
                    {
                        record = JsonSerializer.Deserialize<T>(line, _jsonOptions);
                    }
                    catch (JsonException)
                    {
                        continue;
                    }
                    catch (NotSupportedException)
                    {
                        continue;
                    }

                    if (record is null)
                    {
                        continue;
                    }

                    if (latest.Count == readCount)
                    {
                        latest.Dequeue();
                    }

                    latest.Enqueue(record);
                }

                var records = latest.ToList();
                records.Reverse();
                return records;
            }
            catch (FileNotFoundException)
            {
                return new List<T>();
            }
            catch (DirectoryNotFoundException)
            {
                return new List<T>();
            }
            finally
            {
                fileLock.Release();
            }
        }

        private static void Prepare(AiDemandForecastHistoryRecord record)
        {
            record.Id = record.Id == Guid.Empty ? Guid.NewGuid() : record.Id;
            record.TimestampUtc = ToUtc(record.TimestampUtc);
            record.Last60DaysDemand ??= new List<double>();
            record.Predictions ??= new List<double>();
        }

        private static void Prepare(AiPriceForecastHistoryRecord record)
        {
            record.Id = record.Id == Guid.Empty ? Guid.NewGuid() : record.Id;
            record.TimestampUtc = ToUtc(record.TimestampUtc);
            record.Forecast ??= new List<AiPriceForecastPoint>();
        }

        private static void Prepare(AiAlertHistoryRecord record)
        {
            record.Id = record.Id == Guid.Empty ? Guid.NewGuid() : record.Id;
            record.TimestampUtc = ToUtc(record.TimestampUtc);
            record.RulesTriggered ??= new List<AiAlertRuleHistory>();
        }

        private static DateTimeOffset ToUtc(DateTimeOffset timestamp)
        {
            return timestamp == default
                ? DateTimeOffset.UtcNow
                : timestamp.ToUniversalTime();
        }
    }
}
