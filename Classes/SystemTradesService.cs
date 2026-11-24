using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace wpfTDX
{
    public sealed class SystemTradesApiException : InvalidOperationException
    {
        public int StatusCode { get; }
        public SystemTradesApiException(int statusCode, string message) : base(message)
            { StatusCode = statusCode; }
    }

    /// <summary>TAPI client for recorded model trades and versioned operator reviews.</summary>
    public sealed class SystemTradesService : IDisposable
    {
        private readonly HttpClient _client;
        private readonly string _baseUrl;

        public SystemTradesService() : this(
            ConfigurationManager.AppSettings["TradingApiBaseUrl"],
            ConfigurationManager.AppSettings["TradingApiKey"]) { }

        public SystemTradesService(string baseUrl, string apiKey)
        {
            _baseUrl = baseUrl;
            _client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            if (!string.IsNullOrWhiteSpace(apiKey)) _client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        }

        public async Task<bool> CaptureEnabledAsync(CancellationToken token)
        {
            var result = await PostAsync("system_trades_options", new JObject(), token).ConfigureAwait(false);
            if (result["capture_enabled"]?.Type != JTokenType.Boolean)
                throw new InvalidOperationException("The API returned an invalid capture status.");
            return (bool)result["capture_enabled"];
        }

        public async Task<SystemTradesOptions> OptionsAsync(CancellationToken token)
        {
            var result = await PostAsync("system_trades_options", new JObject(), token).ConfigureAwait(false);
            var options = result.ToObject<SystemTradesOptions>();
            if (result["capture_enabled"]?.Type != JTokenType.Boolean ||
                options?.Decisions == null || options.Decisions.Count != 3 ||
                !options.Decisions.Contains(null) || !options.Decisions.Contains("accepted") ||
                !options.Decisions.Contains("rejected") || options.RejectionReasons == null ||
                string.IsNullOrWhiteSpace(options.CustomRejectionCode) ||
                options.RejectionReasons.Exists(r => r == null || string.IsNullOrWhiteSpace(r.Code) ||
                    string.IsNullOrWhiteSpace(r.Label)))
                throw new InvalidOperationException("The API returned invalid review options. Refresh to retry.");
            return options;
        }

        public async Task<SystemTradeDataModel> FindAsync(SystemTradeDataModel trade, CancellationToken token)
        {
            var rows = await LoadAsync(trade.Fundname, trade.Tickername, "All", "All", token).ConfigureAwait(false);
            return rows.Find(r => r.SystemTradeId == trade.SystemTradeId);
        }

        public async Task<SystemTradeDataModel> SaveReviewAsync(SystemTradeDataModel trade,
            string decision, string rejectionCode, string rejectionReason, string reviewer,
            CancellationToken token, string exitDecision = null, string exitReason = null,
            double? exitPrice = null, DateTimeOffset? exitTime = null)
        {
            var payload = new JObject {
                ["system_trade_id"] = trade.SystemTradeId,
                ["expected_version"] = trade.DecisionVersion,
                ["decision"] = decision == null ? JValue.CreateNull() : new JValue(decision),
                ["decision_by"] = reviewer.Trim()
            };
            if (decision == "rejected")
            {
                payload["rejection_code"] = rejectionCode;
                if (rejectionReason != null) payload["rejection_reason"] = rejectionReason.Trim();
            }
            if (decision == "accepted" && exitDecision != null)
            {
                payload["exit_decision"] = exitDecision;
                if (exitDecision == "discretionary")
                {
                    payload["discretionary_exit_reason"] = exitReason?.Trim();
                    payload["discretionary_exit_price"] = exitPrice.HasValue ? new JValue(exitPrice.Value) : JValue.CreateNull();
                    payload["discretionary_exit_time"] = exitTime?.ToUniversalTime().ToString("o", System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            var result = await PostAsync("system_trades_decision", payload, token).ConfigureAwait(false);
            var row = result["row"]?.ToObject<SystemTradeDataModel>();
            if (row == null || row.SystemTradeId != trade.SystemTradeId ||
                row.DecisionVersion != trade.DecisionVersion + 1)
                throw new InvalidOperationException("The save response could not be verified.");
            return row;
        }

        public async Task<List<SystemTradeDataModel>> LoadAsync(string fund, string ticker,
            string review, string status, CancellationToken token)
        {
            const int pageSize = 500;
            var rows = new List<SystemTradeDataModel>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var serializer = JsonSerializer.Create(new JsonSerializerSettings {
                DateParseHandling = DateParseHandling.DateTimeOffset
            });
            for (int offset = 0; ; offset = checked(offset + pageSize))
            {
                token.ThrowIfCancellationRequested();
                var payload = new JObject { ["limit"] = pageSize, ["offset"] = offset };
                if (!string.IsNullOrWhiteSpace(fund)) payload["fundname"] = fund.Trim();
                if (!string.IsNullOrWhiteSpace(ticker)) payload["tickername"] = ticker.Trim();
                if (review == "Unreviewed") payload["decision"] = JValue.CreateNull();
                else if (review != "All") payload["decision"] = review.ToLowerInvariant();
                if (status != "All") payload["trade_status"] = status.ToLowerInvariant();
                var result = await PostAsync("system_trades", payload, token).ConfigureAwait(false);
                var page = result["rows"] as JArray;
                if (page == null) throw new InvalidOperationException("The API returned no trade list.");
                foreach (var item in page)
                {
                    var row = item.ToObject<SystemTradeDataModel>(serializer);
                    if (row == null || string.IsNullOrEmpty(row.SystemTradeId))
                        throw new InvalidOperationException("The API returned a trade without an ID.");
                    if (seen.Add(row.SystemTradeId)) rows.Add(row);
                }
                if (page.Count < pageSize) break;
            }
            return rows;
        }

        private async Task<JObject> PostAsync(string route, JObject payload, CancellationToken token)
        {
            if (!Uri.TryCreate(_baseUrl, UriKind.Absolute, out var address) ||
                (address.Scheme != "http" && address.Scheme != "https"))
                throw new InvalidOperationException("Configure TradingApiBaseUrl before loading system trades.");
            using (var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json"))
            using (var response = await _client.PostAsync(_baseUrl.TrimEnd('/') + "/" + route,
                content, token).ConfigureAwait(false))
            {
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    string error = null;
                    try { error = (string)JObject.Parse(body)["error"]; } catch (JsonException) { }
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                        error = "TAPI rejected the API key. Check the TDX API configuration.";
                    if (response.StatusCode == HttpStatusCode.NotFound && route != "system_trades_decision")
                        error = "System Trades is unavailable. Deploy/restart TAPI with the System Trades routes.";
                    throw new SystemTradesApiException((int)response.StatusCode,
                        error ?? "TAPI request failed (HTTP " + (int)response.StatusCode + "). Check the service and table setup.");
                }
                using (var reader = new JsonTextReader(new System.IO.StringReader(body)))
                {
                    reader.DateParseHandling = DateParseHandling.DateTimeOffset;
                    return JObject.Load(reader);
                }
            }
        }

        public void Dispose() => _client.Dispose();
    }
}
