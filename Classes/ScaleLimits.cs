using System;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace wpfTDX
{
    /// <summary>
    /// Cached copy of the server-side Scale bounds (Scale.max_scale / min_scale), fetched from
    /// /get_scale_limits. Single source of truth for scale-factor validation across screens — if
    /// Scale.max_scale changes server-side, clients pick it up on next load with no code change.
    /// Falls back to 0..2 (today's server values) if the fetch fails.
    /// </summary>
    public static class ScaleLimits
    {
        public static double Min { get; private set; } = 0.0;
        public static double Max { get; private set; } = 2.0;   // fallback until fetched

        private static bool _loaded;
        private static readonly string ApiKey = ConfigurationManager.AppSettings["TradingApiKey"];
        private static readonly string BaseUrl = ConfigurationManager.AppSettings["TradingApiBaseUrl"];

        /// <summary>Fetch the bounds once and cache them. Safe to call repeatedly.</summary>
        public static async Task EnsureLoadedAsync()
        {
            if (_loaded) return;
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-API-Key", ApiKey);
                    var content = new StringContent("{}", Encoding.UTF8, "application/json");
                    var resp = await client.PostAsync($"{BaseUrl}/get_scale_limits", content).ConfigureAwait(false);
                    resp.EnsureSuccessStatusCode();
                    var jo = JObject.Parse(await resp.Content.ReadAsStringAsync().ConfigureAwait(false));
                    var mx = (double?)jo["max_scale"];
                    var mn = (double?)jo["min_scale"];
                    if (mx.HasValue) Max = mx.Value;
                    if (mn.HasValue) Min = mn.Value;
                    _loaded = true;
                }
            }
            catch
            {
                // keep the fallback defaults; validation still works, just at the last-known bound
            }
        }
    }
}
