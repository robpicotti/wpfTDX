using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace wpfTDX
{
    public sealed class SystemTradesOptions
    {
        [JsonProperty("capture_enabled")] public bool CaptureEnabled { get; set; }
        [JsonProperty("decisions")] public List<string> Decisions { get; set; }
        [JsonProperty("rejection_reasons")] public List<SystemTradeRejectionReason> RejectionReasons { get; set; }
        [JsonProperty("custom_rejection_code")] public string CustomRejectionCode { get; set; }
    }

    public sealed class SystemTradeRejectionReason
    {
        [JsonProperty("code")] public string Code { get; set; }
        [JsonProperty("label")] public string Label { get; set; }
        public override string ToString() => Label;
    }

    public sealed class SystemTradeReviewChoice
    {
        public string Code { get; set; }
        public string Label => Code == null ? "Unreviewed (clear decision)" :
            CultureInfo.InvariantCulture.TextInfo.ToTitleCase(Code);
        public override string ToString() => Label;
    }

    /// <summary>A recorded model trade, including its immutable admission evidence.</summary>
    public sealed class SystemTradeDataModel
    {
        [JsonProperty("system_trade_id")] public string SystemTradeId { get; set; }
        [JsonProperty("fundname")] public string Fundname { get; set; }
        [JsonProperty("tickername")] public string Tickername { get; set; }
        [JsonProperty("intervalname")] public string Intervalname { get; set; }
        [JsonProperty("groupname")] public string Groupname { get; set; }
        [JsonProperty("strategyname")] public string Strategyname { get; set; }
        [JsonProperty("trade_id")] public string TradeId { get; set; }
        [JsonProperty("direction")] public string Direction { get; set; }
        [JsonProperty("entry_time")] public DateTimeOffset? EntryTime { get; set; }
        [JsonProperty("entry_price")] public double? EntryPrice { get; set; }
        [JsonProperty("entry_var")] public double? EntryVar { get; set; }
        [JsonProperty("detected_at")] public DateTimeOffset? DetectedAt { get; set; }
        [JsonProperty("criteria_version")] public string CriteriaVersion { get; set; }
        [JsonProperty("criteria_snapshot")] public string CriteriaSnapshot { get; set; }
        [JsonProperty("trade_status")] public string TradeStatus { get; set; }
        [JsonProperty("exit_time")] public DateTimeOffset? ExitTime { get; set; }
        [JsonProperty("exit_price")] public double? ExitPrice { get; set; }
        [JsonProperty("source_updated_at")] public DateTimeOffset? SourceUpdatedAt { get; set; }
        [JsonProperty("decision")] public string Decision { get; set; }
        [JsonProperty("rejection_code")] public string RejectionCode { get; set; }
        [JsonProperty("rejection_reason")] public string RejectionReason { get; set; }
        [JsonProperty("decision_by")] public string DecisionBy { get; set; }
        [JsonProperty("decision_at")] public DateTimeOffset? DecisionAt { get; set; }
        [JsonProperty("decision_version")] public int DecisionVersion { get; set; }

        [JsonProperty("exit_decision")] public string ExitDecision { get; set; }
        [JsonProperty("discretionary_exit_reason")] public string DiscretionaryExitReason { get; set; }
        [JsonProperty("discretionary_exit_price")] public double? DiscretionaryExitPrice { get; set; }
        [JsonProperty("discretionary_exit_time")] public DateTimeOffset? DiscretionaryExitTime { get; set; }
        public DateTime? DiscretionaryExitUtc => DiscretionaryExitTime?.UtcDateTime;
        public string ExitDecisionLabel => ExitDecision == "discretionary" ? "Discretionary"
            : (ExitDecision == "system" || Decision == "accepted" ? "System" : "");

        public string ReviewLabel => string.IsNullOrEmpty(Decision) ? "Unreviewed" : Decision;
        public string SelectionTitle => Tickername + " | " + Direction + " | " + Intervalname;
        public DateTime? EntryUtc => EntryTime?.UtcDateTime;
        public DateTime? ExitUtc => ExitTime?.UtcDateTime;
        public DateTime? DecisionUtc => DecisionAt?.UtcDateTime;
        public DateTime? SourceUpdatedUtc => SourceUpdatedAt?.UtcDateTime;
        public string ReviewTimeLabel => DecisionUtc.HasValue
            ? "Reviewed: " + DecisionUtc.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC"
            : "Reviewed: Unavailable";

        public string QualificationText
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CriteriaSnapshot)) return "No qualification evidence recorded.";
                try
                {
                    var evidence = JObject.Parse(CriteriaSnapshot);
                    var conditions = evidence["conditions"] as JArray;
                    if (conditions == null || conditions.Count == 0)
                        return "No qualification conditions recorded.";
                    var lines = conditions.OfType<JObject>().Select(c => string.Format(
                        CultureInfo.InvariantCulture, "{0}: {1} {2} {3}",
                        c["column"], c["actual"], Operator((string)c["op"]), c["value"]));
                    return "All conditions below passed at signal entry:\n" + string.Join("\n", lines);
                }
                catch (JsonException) { return "Qualification evidence could not be read.\n" + CriteriaSnapshot; }
            }
        }

        private static string Operator(string op)
        {
            switch (op)
            {
                case "eq": return "=";
                case "ge": return ">=";
                case "gt": return ">";
                case "le": return "<=";
                case "lt": return "<";
                default: return op;
            }
        }
    }
}
