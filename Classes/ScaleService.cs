using System;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace wpfTDX
{
    /// <summary>
    /// Thin client for inserting scaled_positions rows via the /insert_scaled_positions API,
    /// which routes each row through the server-side Scale class (hoover/scale.py) — that's where
    /// validation, stepping and overlap checks live. Prefer this over the direct-SQL
    /// Position.scale_position for new scaling paths (e.g. manual scaling from the scaled-positions
    /// screen): server-side validation, no client SqlConnection, consistent with the rest of the app.
    /// </summary>
    public sealed class ScaleService
    {
        private static readonly string ApiKey = ConfigurationManager.AppSettings["TradingApiKey"];
        private static readonly string BaseUrl = ConfigurationManager.AppSettings["TradingApiBaseUrl"];

        private static readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(60),
            DefaultRequestHeaders = { { "X-Api-Key", ApiKey } }
        };

        public sealed class Result
        {
            public bool Ok { get; set; }
            public string Message { get; set; }
        }

        /// <summary>Insert one scaled_positions row and wait for the background job to finish.</summary>
        public async Task<Result> InsertAndWaitAsync(ScaledPositionsInsertRow row, CancellationToken ct = default)
        {
            var json = JsonConvert.SerializeObject(new { rows = new[] { row } });

            string jobId;
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            using (var resp = await _http.PostAsync("/insert_scaled_positions/start", content).ConfigureAwait(false))
            {
                resp.EnsureSuccessStatusCode();
                var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                jobId = (string)JObject.Parse(body)["job_id"];
            }

            if (string.IsNullOrEmpty(jobId))
                return new Result { Ok = false, Message = "No job id returned by the server." };

            // Poll status until done/failed (~60s ceiling at 500ms intervals).
            for (int i = 0; i < 120; i++)
            {
                await Task.Delay(500, ct).ConfigureAwait(false);
                using (var resp = await _http.GetAsync(
                    "/insert_scaled_positions/status/" + Uri.EscapeDataString(jobId), ct).ConfigureAwait(false))
                {
                    resp.EnsureSuccessStatusCode();
                    var jo = JObject.Parse(await resp.Content.ReadAsStringAsync().ConfigureAwait(false));
                    var status = (string)jo["status"];
                    if (string.Equals(status, "done", StringComparison.OrdinalIgnoreCase))
                        return new Result { Ok = true, Message = (string)jo["message"] ?? "Done" };
                    if (string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase))
                        return new Result { Ok = false, Message = (string)jo["message"] ?? "Scaling failed." };
                }
            }
            return new Result { Ok = false, Message = "Timed out waiting for the scaling job." };
        }
    }
}
