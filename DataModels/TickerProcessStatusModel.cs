using System;
using Newtonsoft.Json;

namespace wpfTDX
{
    /// <summary>
    /// One ticker's live RTU processing status, as returned by the tapi
    /// /ticker_process_status route (the "tickers" map). Tickername is the map key
    /// and is assigned when the row is built, not part of the value object.
    /// </summary>
    public class TickerProcessStatusModel
    {
        public string Tickername { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }          // queued | running | done | failed | timeout

        [JsonProperty("run_id")]
        public DateTime? RunId { get; set; }

        [JsonProperty("start_utc")]
        public DateTime? StartUtc { get; set; }

        [JsonProperty("end_utc")]
        public DateTime? EndUtc { get; set; }

        [JsonProperty("duration_s")]
        public double? DurationS { get; set; }

        [JsonProperty("attempt")]
        public int? Attempt { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("hostname")]
        public string Hostname { get; set; }

        [JsonProperty("pid")]
        public int? Pid { get; set; }

        [JsonProperty("updated_utc")]
        public DateTime? UpdatedUtc { get; set; }

        [JsonProperty("last_processed")]
        public DateTime? LastProcessed { get; set; }

        // Convenience for display: minutes since this ticker last successfully
        // processed (from tad_positions), or null if never.
        public double? StaleMinutes =>
            LastProcessed.HasValue
                ? Math.Round((DateTime.UtcNow - LastProcessed.Value).TotalMinutes, 1)
                : (double?)null;
    }
}
