using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpfTDX
{
    public sealed class FilterIntervalsUpsertRow
    {
        [JsonProperty("tickername")] public string Tickername { get; set; }
        [JsonProperty("fundgroups")] public string FundGroup { get; set; }
        [JsonProperty("rescale")] public bool? Rescale { get; set; }
        [JsonProperty("long_only")] public bool? LongOnly { get; set; }
        [JsonProperty("short_only")] public bool? ShortOnly { get; set; }
        [JsonProperty("buy_only")] public bool? BuyOnly { get; set; }
        [JsonProperty("sell_only")] public bool? SellOnly { get; set; }
        [JsonProperty("all_intervals")] public bool? AllIntervals { get; set; }
        [JsonProperty("base_y1")] public bool? BaseY1 { get; set; }
        [JsonProperty("base_h1")] public bool? BaseH1 { get; set; }
        [JsonProperty("base_D1")] public bool? BaseD1 { get; set; }
        [JsonProperty("y1")] public bool? Y1 { get; set; }
        [JsonProperty("y2")] public bool? Y2 { get; set; }
        [JsonProperty("y3")] public bool? Y3 { get; set; }
        [JsonProperty("h2")] public bool? H2 { get; set; }
        [JsonProperty("h3")] public bool? H3 { get; set; }
        [JsonProperty("h4")] public bool? H4 { get; set; }
        [JsonProperty("h5")] public bool? H5 { get; set; }
        [JsonProperty("h6")] public bool? H6 { get; set; }
        [JsonProperty("h12")] public bool? H12 { get; set; }
        [JsonProperty("h16")] public bool? H16 { get; set; }
        [JsonProperty("D1")] public bool? D1 { get; set; }
        [JsonProperty("h36")] public bool? H36 { get; set; }
        [JsonProperty("D2")] public bool? D2 { get; set; }
        [JsonProperty("D3")] public bool? D3 { get; set; }
        [JsonProperty("D4")] public bool? D4 { get; set; }
        [JsonProperty("W1")] public bool? W1 { get; set; }
        [JsonProperty("D8")] public bool? D8 { get; set; }
        [JsonProperty("W2")] public bool? W2 { get; set; }
    }

}
