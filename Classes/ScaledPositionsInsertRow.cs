using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpfTDX
{
    public sealed class ScaledPositionsInsertRow
    {
        [JsonProperty("fundgroupname")] public string FundGroupName { get; set; }
        [JsonProperty("fundname")] public string FundName { get; set; }
        [JsonProperty("tickername")] public string TickerName { get; set; }
        [JsonProperty("scaled_stepsize")] public double? ScaledStepSize { get; set; }
        [JsonProperty("scaled_percent")] public double? ScaledPercent { get; set; }
        [JsonProperty("scaled_target")] public double? ScaledTarget { get; set; }
        [JsonProperty("scaled_timestep")] public double? ScaledTimeStep { get; set; }
        [JsonProperty("scaled_type")] public string ScaledType { get; set; }
    }
}
