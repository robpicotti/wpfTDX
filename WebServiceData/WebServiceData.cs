using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Configuration;

namespace wpfTDX
{
    public class WebServiceData
    {
        public readonly string apiKey = ConfigurationManager.AppSettings["TradingApiKey"];
        public readonly string baseUrl = ConfigurationManager.AppSettings["TradingApiBaseUrl"];
        private static readonly HttpClient _client = new HttpClient();


        public WebServiceData()
        {
            if (_client.BaseAddress == null)
            {
                _client.BaseAddress = new Uri(baseUrl);
                _client.Timeout = TimeSpan.FromMinutes(10);
                _client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
            }
        }


        public async Task<string> GetFundsDataASync()
        {
            string fundsurl =  $"{baseUrl}/get_funds";


            string jsonResponse = "";
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                HttpResponseMessage response = await client.PostAsync(fundsurl, null);
                response.EnsureSuccessStatusCode(); // Ensures that the response was successful

                jsonResponse = await response.Content.ReadAsStringAsync();
            }
            return jsonResponse;
        }

        public async Task<List<TickerDataModel>> GetTickersAsync(Dictionary<string, object> where = null)
        {
            if (where == null) where = new Dictionary<string, object>();
            // Uses your existing private SelectTableAsync<T>
            return await SelectTableAsync<TickerDataModel>("tickers", where).ConfigureAwait(false);
        }

        private async Task<List<T>> SelectTableAsync<T>(string tableName, Dictionary<string, object> where)
        {
            var request = new
            {
                table_name = tableName,
                where_dict = where
            };

            string body = JsonConvert.SerializeObject(request);
            using (var payload = new StringContent(body, Encoding.UTF8, "application/json"))
            using (HttpResponseMessage resp = await _client.PostAsync("select_table", payload).ConfigureAwait(false))
            {
                resp.EnsureSuccessStatusCode();
                string json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                var list = JsonConvert.DeserializeObject<List<T>>(json);
                return list ?? new List<T>();
            }
        }
        public async Task SavePortfolioWeightsAsync(IEnumerable<PortfolioWeightsDataModel> rows, CancellationToken ct = default(CancellationToken))
        {
            if (rows == null) return;
            DateTime now = DateTime.UtcNow;
            var runtimeStr = now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            // Shape the "data" array to match your Flask insert_table expectations
            var dataArray = rows.Select(r => new
            {
                portfolioname = r.PortfolioName,
                // Send ISO8601; adjust if your API expects local/naive
                runtime = runtimeStr,              // Json.NET will serialize to ISO by default
                tickername = r.TickerName,
                weight = r.Weight                 // null => JSON null (soft remove)
            }).ToList();

            var payload = new
            {
                table_name = "portfolio_weights",
                data = dataArray
            };

            var json = JsonConvert.SerializeObject(
                payload,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Include,
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc // ensure UTC if that’s what your DB expects
                });

            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            using (var resp = await _client.PostAsync("insert_portfolio_weights", content, ct).ConfigureAwait(false))
            {
                resp.EnsureSuccessStatusCode();

                // Optional: check {"success": true}
                var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                // You can parse & assert here if you want:
                // var ok = (Newtonsoft.Json.Linq.JObject.Parse(body)["success"]?.Value<bool>() ?? false);
                // if (!ok) throw new System.Exception("insert_table returned success=false");
            }
        }

        /// <summary>
        /// Fetches the current documentation string for a given error_code.
        /// Calls the /get_error_documentation API endpoint.
        /// </summary>
        public async Task<string> GetErrorDocumentationAsync(int errorCode, CancellationToken ct = default(CancellationToken))
        {
            var request = new { error_code = errorCode };
            string body = JsonConvert.SerializeObject(request);
            using (var content = new StringContent(body, Encoding.UTF8, "application/json"))
            using (var resp = await _client.PostAsync("get_error_documentation", content, ct).ConfigureAwait(false))
            {
                resp.EnsureSuccessStatusCode();
                string json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                var obj = JObject.Parse(json);
                return obj["documentation"]?.Type == JTokenType.Null ? null : obj["documentation"]?.ToString();
            }
        }

        /// <summary>
        /// Updates the documentation field on the errors table for a given error_code.
        /// Calls the /update_error_documentation API endpoint.
        /// </summary>
        public async Task UpdateErrorDocumentationAsync(int errorCode, string documentation, CancellationToken ct = default(CancellationToken))
        {
            var request = new
            {
                error_code = errorCode,
                documentation = documentation
            };

            string body = JsonConvert.SerializeObject(request);
            using (var content = new StringContent(body, Encoding.UTF8, "application/json"))
            using (var resp = await _client.PostAsync("update_error_documentation", content, ct).ConfigureAwait(false))
            {
                resp.EnsureSuccessStatusCode();
            }
        }

        /// <summary>
        /// Checks if a ticker is in the benchmark for the given fund selection.
        /// Calls the /is_ticker_in_benchmark API endpoint.
        /// </summary>
        public async Task<BenchmarkCheckResult> IsTickerInBenchmarkAsync(
            string tickername, string fundgroupname = "*", string fundname = "*")
        {
            var request = new
            {
                tickername,
                fundgroupname = fundgroupname ?? "*",
                fundname = fundname ?? "*",
            };

            string body = JsonConvert.SerializeObject(request);
            using (var content = new StringContent(body, Encoding.UTF8, "application/json"))
            using (var resp = await _client.PostAsync("is_ticker_in_benchmark", content).ConfigureAwait(false))
            {
                resp.EnsureSuccessStatusCode();
                string json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonConvert.DeserializeObject<BenchmarkCheckResult>(json);
            }
        }
    }

    /// <summary>
    /// Response from /is_ticker_in_benchmark endpoint.
    /// </summary>
    public class BenchmarkCheckResult
    {
        [JsonProperty("in_benchmark")] public bool InBenchmark { get; set; }
        [JsonProperty("portfolionames")] public List<string> PortfolioNames { get; set; }
        [JsonProperty("funds")] public List<BenchmarkFundInfo> Funds { get; set; }
        [JsonProperty("detail")] public string Detail { get; set; }
    }

    public class BenchmarkFundInfo
    {
        [JsonProperty("portfolioname")] public string PortfolioName { get; set; }
        [JsonProperty("fundgroupname")] public string FundGroupName { get; set; }
        [JsonProperty("fundname")] public string FundName { get; set; }
    }
}
