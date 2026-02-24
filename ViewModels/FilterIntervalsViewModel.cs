using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using TDX;
using System.Configuration;
using static System.Net.WebRequestMethods;

namespace wpfTDX
{

    public class BoolToStringConverter : IValueConverter
    {
        public string TrueText { get; set; } = "True";
        public string FalseText { get; set; } = "False";
        public string NullText { get; set; } = "";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool? b = value as bool?;
            if (!b.HasValue) return NullText;
            return b.Value ? TrueText : FalseText;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = (value ?? "").ToString().Trim();
            if (string.Equals(s, TrueText, StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(s, FalseText, StringComparison.OrdinalIgnoreCase)) return false;
            return null;
        }
    }

    public class IsZeroConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return false;
            try
            {
                var d = System.Convert.ToDouble(value, culture);
                return Math.Abs(d) < 1e-9;
            }
            catch { return false; }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    public class NullableDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is double d ? d.ToString(culture) : string.Empty;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as string;
            if (string.IsNullOrWhiteSpace(s)) return null;              // <<< key line
            if (double.TryParse(s, NumberStyles.Any, culture, out var d)) return d;
            return Binding.DoNothing;                                   // keep old value if invalid
        }
    }
    public class NullablePercentInputConverter : IValueConverter
    {
        // Model -> UI (edit box). Show raw fraction, e.g. 0.75
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;

            if (value is double)                      // no pattern variable
                return ((double)value).ToString(culture);

            // be generous for other numeric types
            var formattable = value as IFormattable;
            return formattable != null
                ? formattable.ToString(null, culture)
                : value.ToString();
        }

        // UI -> Model. Accept "75", "75%", "0.75", "".
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as string;
            if (string.IsNullOrWhiteSpace(s)) return null;

            s = s.Trim();
            var pct = culture.NumberFormat.PercentSymbol;
            var hadPercent = s.EndsWith(pct, StringComparison.Ordinal);
            if (hadPercent) s = s.Substring(0, s.Length - pct.Length).Trim();

            double x;
            if (!double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, culture, out x))
                return Binding.DoNothing;

            if (hadPercent || x >= 1.0) x /= 100.0;   // "75" or "75%" => 0.75
            return x;
        }
    }

    public class NullablePercentDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Pass the numeric value through; StringFormat on the binding will handle "{0:P1}"
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value?.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(s))
                return null;

            // strip percent sign
            bool hadPercent = s.EndsWith("%", StringComparison.Ordinal);
            if (hadPercent) s = s.Substring(0, s.Length - 1).Trim();

            // accept leading or trailing dot
            if (s.StartsWith(".")) s = "0" + s;   // ".25" -> "0.25"
            if (s.EndsWith(".")) s = s + "0";    // "1."  -> "1.0"

            // parse with invariant (or your desired) culture
            if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                return Binding.DoNothing; // or throw to trigger validation

            // If user typed "25%" treat as 0.25; if they typed 0.25 or .25, keep as 0.25
            if (hadPercent)
                v = v / 100.0;

            // your scale is 0..3; converter returns the underlying fraction (e.g., 0.25)
            return v;
        }

    }

    public sealed class CompareRoundedConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return 0;

            double a, b;
            if (!TryToDouble(values[0], out a) || !TryToDouble(values[1], out b)) return 0;

            int decimals = 1;     // default P1
            double scale = 100.0; // percent space
            var p = parameter as string;
            if (!string.IsNullOrEmpty(p) && (p[0] == 'P' || p[0] == 'p'))
            {
                int d;
                if (int.TryParse(p.Substring(1), out d)) decimals = d;
            }

            double ra = Math.Round(a * scale, decimals, MidpointRounding.AwayFromZero);
            double rb = Math.Round(b * scale, decimals, MidpointRounding.AwayFromZero);

            if (ra < rb) return -1;
            if (ra > rb) return 1;
            return 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static bool TryToDouble(object v, out double d)
        {
            d = 0;
            if (v == null || v == DependencyProperty.UnsetValue) return false;

            try
            {
                if (v is double) { d = (double)v; return true; }
                if (v is float) { d = (float)v; return true; }
                if (v is decimal) { d = (double)(decimal)v; return true; }
                if (v is int) { d = (int)v; return true; }
                if (v is long) { d = (long)v; return true; }
                if (v is string) { return double.TryParse((string)v, NumberStyles.Any, CultureInfo.InvariantCulture, out d); }

                d = System.Convert.ToDouble(v, CultureInfo.InvariantCulture);
                return true;
            }
            catch { return false; }
        }
    }


    public sealed class IsZeroRoundedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double v;
            if (!TryToDouble(value, out v)) return false;

            int decimals = 1;
            double scale = 100.0;
            var p = parameter as string;
            if (!string.IsNullOrEmpty(p) && (p[0] == 'P' || p[0] == 'p'))
            {
                int d;
                if (int.TryParse(p.Substring(1), out d)) decimals = d;
            }

            double r = Math.Round(v * scale, decimals, MidpointRounding.AwayFromZero);
            return r == 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static bool TryToDouble(object v, out double d)
        {
            d = 0;
            if (v == null || v == DependencyProperty.UnsetValue) return false;

            try
            {
                if (v is double) { d = (double)v; return true; }
                if (v is float) { d = (float)v; return true; }
                if (v is decimal) { d = (double)(decimal)v; return true; }
                if (v is int) { d = (int)v; return true; }
                if (v is long) { d = (long)v; return true; }
                if (v is string) { return double.TryParse((string)v, NumberStyles.Any, CultureInfo.InvariantCulture, out d); }

                d = System.Convert.ToDouble(v, CultureInfo.InvariantCulture);
                return true;
            }
            catch { return false; }
        }
    }

    public sealed class FlagTextOrBlankConverter : IMultiValueConverter
    {
        public string TrueText { get; set; } = "True";
        public string FalseText { get; set; } = "False";

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] = flag (bool?)
            // values[1] = position (float?/double?)
            if (values == null || values.Length < 2) return "";

            bool? flag = values[0] as bool?;
            object posObj = values[1];

            // treat "no position" as blank
            if (posObj == null || posObj == DependencyProperty.UnsetValue)
                return "";

            // if position exists but flag is null, also blank
            if (!flag.HasValue) return "";

            return flag.Value ? TrueText : FalseText;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => null;
    }

    public sealed class NotEqualConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return false;
            var a = values[0];
            var b = values[1];
            if (a == DependencyProperty.UnsetValue || b == DependencyProperty.UnsetValue) return false;
            // treat null vs null as equal
            if (a == null && b == null) return false;
            return !Equals(a, b);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }


    // Returns -1 if a<b, 0 if equal/unknown, 1 if a>b
    public class CompareDoubleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return 0;
            bool okA = TryToDouble(values[0], culture, out var a);
            bool okB = TryToDouble(values[1], culture, out var b);
            if (!okA || !okB) return 0;
            if (a < b) return -1;
            if (a > b) return 1;
            return 0;
        }

        static bool TryToDouble(object v, CultureInfo c, out double d)
        {
            try { d = System.Convert.ToDouble(v, c); return true; }
            catch { d = 0; return false; }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => null;
    }

    public class FilterIntervalsViewModel :INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        private ObservableCollection<FilterIntervalsDataModel> _filterIntervalsData;
        
        public ObservableCollection<FilterIntervalsDataModel> FilterIntervalsData
        {
            get { return _filterIntervalsData; }
            set
            {
                if (_filterIntervalsData != value)
                { 
                    _filterIntervalsData = value;
                    OnPropertyChanged(nameof(FilterIntervalsData));
                }
            }
        }
        private ObservableCollection<ScaledPositionsDataModel> _scaledPositionsData
            = new ObservableCollection<ScaledPositionsDataModel>();
        public ObservableCollection<ScaledPositionsDataModel> ScaledPositionsData
        {
            get => _scaledPositionsData;
            set
            {
                if (!ReferenceEquals(_scaledPositionsData, value))
                {
                    _scaledPositionsData = value ?? new ObservableCollection<ScaledPositionsDataModel>();
                    OnPropertyChanged(nameof(ScaledPositionsData));
                }
            }
        }


        public ObservableCollection<MergedTickerRow> MergedRows { get; } = new ObservableCollection<MergedTickerRow>();


        private ObservableCollection<TadPositionsDataModel> _tadPositionsData;
        
        public ObservableCollection<TadPositionsDataModel> TadPositionsData
        {
            get { return _tadPositionsData; }
            set
            {
                if (_tadPositionsData != value)
                {
                    _tadPositionsData = value;
                    OnPropertyChanged(nameof(TadPositionsData));
                }
            }
        }

        private ObservableCollection<StrategiesOverrideDataModel> _StrategiesOverrideData;
        public ObservableCollection<StrategiesOverrideDataModel> StrategiesOverrideData
        {
            get { return _StrategiesOverrideData; }
            set { 
                if(_StrategiesOverrideData != value)
                {
                    _StrategiesOverrideData = value; ;
                    OnPropertyChanged(nameof(StrategiesOverrideData));
                }
            }
        }

        // strategies list for the ComboBox
        public ObservableCollection<string> StrategyNames { get; } = new ObservableCollection<string>();

        // manual-freeze definition rows (error_code + freeze_level) pulled from "errors" (manual=1)
        private sealed class ManualFreezeDef
        {
            public int ErrorCode;
            public string FreezeLevel;
            public string Reason;
        }
        private List<ManualFreezeDef> _manualDefs = new List<ManualFreezeDef>();

        // unresolved manual freezes we’ll match against rows
        private sealed class ActiveFreezeRow
        {
            public DateTime? RunTime;
            public string Tickername;
            public string Fundname;       // "*" or concrete fund
            public string Fundgroupname;  // "*" or concrete group
            public int ErrorCode;
            public string FreezeLevel;    // e.g. "tickername,fundname"
        }


        public sealed class FilterIntervalsDeleteRow
        {
            [JsonProperty("tickername")]
            public string Tickername { get; set; }

            [JsonProperty("fundgroupname")]
            public string FundGroup { get; set; }

            [JsonProperty("fundname")]
            public string FundName { get; set; }

            // optional if you need it server-side
            [JsonProperty("hostenv")]
            public string HostEnv { get; set; }
        }

        private List<ActiveFreezeRow> _activeManualFreezes = new List<ActiveFreezeRow>();

        // latest override by ticker (store the full model, not just the name)
        private Dictionary<string, StrategiesOverrideDataModel> _overrideByTicker
            = new Dictionary<string, StrategiesOverrideDataModel>(StringComparer.OrdinalIgnoreCase);

        // host environment (for strategies_override inserts)
        private string _hostEnv = "dev";
        public string HostEnv
        {
            get => _hostEnv;
            set
            {
                if (_hostEnv != value)
                {
                    _hostEnv = value;
                    OnPropertyChanged(nameof(HostEnv));
                }
            }
        }


        // cached universe for the Add-Ticker dialog
        public List<TickerRow> TickerUniverse { get; private set; } = new List<TickerRow>();

        private bool _isExecuting;
        
        public bool IsExecuting
        {
            get => _isExecuting;
            set
            {
                if (_isExecuting != value)
                {
                    _isExecuting = value;
                    OnPropertyChanged(); // raises PropertyChanged(nameof(IsExecuting))
                    OnPropertyChanged(nameof(IsSaveEnabled));
                }
            }
        }


        public ICollectionView MergedRowsView { get; private set; }

        private string _tickerFilter;
        public string TickerFilter
        {
            get { return _tickerFilter; }
            set
            {
                if (_tickerFilter != value)
                {
                    _tickerFilter = value;
                    OnPropertyChanged(nameof(TickerFilter));
                    RestartTimer();
                }
            }
        }

        private string _fundGroupFilter;
        public string FundGroupFilter
        {
            get { return _fundGroupFilter; }
            set
            {
                if (_fundGroupFilter != value)
                {
                    _fundGroupFilter = value;
                    OnPropertyChanged(nameof(FundGroupFilter));
                    RestartTimer();
                }
            }
        }
        private bool _defaultRescale = true;
        public bool DefaultRescale
        {
            get { return _defaultRescale; }
            set
            {
                if (_defaultRescale != value)
                {
                    _defaultRescale = value;
                    OnPropertyChanged(nameof(DefaultRescale));
                }
            }
        }
        private bool _defaultLongOnly = false;
        public bool DefaultLongOnly
        {
            get { return _defaultLongOnly; }
            set
            {
                if (_defaultLongOnly != value)
                {
                    _defaultLongOnly = value;
                    OnPropertyChanged(nameof(DefaultLongOnly));
                }
            }
        }
        private bool _defaultShortOnly = false;
        public bool DefaultShortOnly
        {
            get { return _defaultShortOnly; }
            set
            {
                if (_defaultShortOnly != value)
                {
                    _defaultShortOnly = value;
                    OnPropertyChanged(nameof(DefaultShortOnly));
                }
            }
        }
        private bool _defaultBuyOnly = false;
        public bool DefaultBuyOnly
        {
            get { return _defaultBuyOnly; }
            set
            {
                if (_defaultBuyOnly != value)
                {
                    _defaultBuyOnly = value;
                    OnPropertyChanged(nameof(DefaultBuyOnly));
                }
            }
        }
        private bool _defaultSellOnly = false;
        public bool DefaultSellOnly
        {
            get { return _defaultSellOnly; }
            set
            {
                if (_defaultSellOnly != value)
                {
                    _defaultSellOnly = value;
                    OnPropertyChanged(nameof(DefaultSellOnly));
                }
            }
        }
        private bool _defaultAllIntervals = false;
        public bool DefaultAllIntervals
        {
            get { return _defaultAllIntervals; }
            set
            {
                if (_defaultAllIntervals != value)
                {
                    _defaultAllIntervals = value;
                    OnPropertyChanged(nameof(DefaultAllIntervals));
                }
            }
        }

        private readonly DispatcherTimer _filterTimer =
            new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };


        private Func<string, bool> _tickerPredicate = _ => true;
        private Func<string, bool> _groupPredicate = _ => true;

        private static string apiKey = ConfigurationManager.AppSettings["TradingApiKey"];
        private static string baseUrl = ConfigurationManager.AppSettings["TradingApiBaseUrl"];
        private static readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(60),
            DefaultRequestHeaders =
            {
                { "X-Api-Key", apiKey }
            }
        };

        SqlConnection SqlConn;


        public FilterIntervalsViewModel(SqlConnection sqlconn)
        {
            SqlConn = sqlconn;
            FilterIntervalsData = new ObservableCollection<FilterIntervalsDataModel>();
            TadPositionsData = new ObservableCollection<TadPositionsDataModel>();
            MergedRowsView = CollectionViewSource.GetDefaultView(MergedRows);
            if (MergedRowsView != null)
                MergedRowsView.Filter = RowFilter;


            _filterTimer.Tick += delegate (object sender, EventArgs e)
            {
                _filterTimer.Stop();
                RebuildPredicates();          // build once per typing burst
                if (MergedRowsView != null) MergedRowsView.Refresh();
            };


        }

        public FilterIntervalsDeleteRow ToDeleteRow(MergedTickerRow r) => new FilterIntervalsDeleteRow
        {
            Tickername = r.Tickername?.Trim(),
            FundGroup = string.IsNullOrWhiteSpace(r.FundGroup) ? "*" : r.FundGroup.Trim(),
            FundName = string.IsNullOrWhiteSpace(r.FundName) ? "*" : r.FundName.Trim(),
            HostEnv = this.HostEnv
        };

        private static string JStr(JToken t, string name)
        {
            if (t == null) return null;
            var o = t as JObject;
            return o != null ? (o.Value<string>(name) ?? "").Trim() : null;
        }

        private sealed class ManualChangeDto
        {
            public string action;       // "insert" | "resolve"
            public string tickername;
            public string fundname;     // "*" allowed
            public string fundgroupname;// "*" allowed
            public int error_code;      // taken from _manualDefs
            public string freeze_level; // "tickername", "tickername,fundname", etc.
            public int resolved;        // 0 (insert), 1 (resolve)
            public string hostenv;      // _hostEnv (you already load this)
        }

        private static string DetermineFreezeLevel(MergedTickerRow r)
        {
            bool hasFund = !string.IsNullOrWhiteSpace(r.FundName) && r.FundName != "*";
            bool hasGroup = !string.IsNullOrWhiteSpace(r.FundGroup) && r.FundGroup != "*";

            if (hasFund && hasGroup) return "tickername,fundname,fundgroupname";
            if (hasFund) return "tickername,fundname";
            if (hasGroup) return "tickername,fundgroupname";
            return "tickername";
        }
        private ManualFreezeDef GetManualErrorDefForLevel(string freezeLevel)
        {
            // exact match preferred
            var def = _manualDefs.FirstOrDefault(d =>
                string.Equals(d.FreezeLevel, freezeLevel, StringComparison.OrdinalIgnoreCase));

            // fallback: any available
            return def ?? _manualDefs.FirstOrDefault();
        }



        public async Task ApplyManualChangesAsync_UsingTickerFreezer()
        {
            if (SqlConn == null) throw new InvalidOperationException("SqlConn is null on FilterIntervalsViewModel.");

            await Task.Run(() =>
            {
                var freezer = new TickerFreezer(SqlConn);
                string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); // your DB likes string runtimes

                foreach (var r in MergedRows)
                {

                    if (!r.ManualHasChanged) continue;

                    string level = DetermineFreezeLevel(r);
                    var def = GetManualErrorDefForLevel(level);
                    int errorCode = def?.ErrorCode ?? 9999;
                    string errorReason = string.IsNullOrWhiteSpace(def?.Reason)
                        ? $"Manual trading freeze ({level})"
                        : def.Reason;

                    // Normalise wildcards to "*"
                    string fund = string.IsNullOrWhiteSpace(r.FundName) ? "*" : r.FundName.Trim();
                    string group = string.IsNullOrWhiteSpace(r.FundGroup) ? "*" : r.FundGroup.Trim();
                    string tkr = r.Tickername?.Trim() ?? "*";

                    if (r.Manual == true && r.OriginalManual != true)
                    {

                        // INSERT unresolved manual freeze (resolved=0 inside Freeze())
                        freezer.Freeze(
                            runtime: now,
                            error_code: errorCode,
                            error_string: errorReason,
                            freeze_expiration: null,
                            _override: 0,
                            override_expiration: null,
                            notes: "FilterIntervals UI",
                            runtime_resolved: null,
                            ems: "*",
                            broker_code_exec: "*",
                            fundname: fund,
                            subaccountname: "*",
                            execaccountname: "*",
                            tad_id: "*",
                            tickername: tkr,
                            benchmarkname: "*",
                            fundgroupname: group
                        );
                    }
                    else if (r.Manual == false && r.OriginalManual == true)
                    {
                        // Find the active manual freeze that matches this row
                        var match = _activeManualFreezes.FirstOrDefault(f => FreezeMatchesRow(f, r));

                        // Fallback to "now" only if we couldn't find a matching unresolved freeze
                        string thawRuntime = (match?.RunTime?.ToString("yyyy-MM-dd HH:mm:ss"))
                                             ?? now;
                        // Resolve existing manual freeze entry
                        //MessageBox.Show($"Resolving manual freeze for {tkr} ({group}/{fund}/{errorCode}) at {thawRuntime}.");
                        freezer.Thaw(
                            runtime: thawRuntime,
                            ems: "*",
                            broker_code_exec: "*",
                            fundgroupname: group,
                            fundname: fund,
                            subaccountname: "*",
                            execaccountname: "*",
                            tad_id: "*",
                            tickername: tkr,
                            error_code: errorCode.ToString(),
                            benchmarkname: "*",
                            notes: "FilterIntervals UI",
                            gbl_conn: SqlConn
                        );
                    }

                    // Accept just the Manual change locally (do not disturb other dirty flags)
                    r.AcceptManualAsOriginal();
                }
            });

            // Optional: if your UI relies on _activeManualFreezes for highlighting, refresh it:
            // await LoadActiveManualFreezesAsync();
        }



        private static bool WildEq(string a, string b)
        {
            // treat "*" as wildcard; otherwise case-insensitive equals
            if (string.Equals(a, "*", StringComparison.Ordinal)) return true;
            return string.Equals(a ?? "", b ?? "", StringComparison.OrdinalIgnoreCase);
        }

        private static DateTime? JDate(JObject o, string name)
        {
            if (o == null) return null;
            DateTime dt;
            var s = o.Value<string>(name);
            if (DateTime.TryParse(s, out dt)) return dt;
            var tn = o[name];
            if (tn != null && tn.Type == JTokenType.Date) return tn.Value<DateTime>();
            return null;
        }

        //private List<StrategiesOverrideDataModel> BuildStrategyOverrideRows()
        //{
        //    // Safety: if the backing collection wasn't loaded, just treat as empty
        //    var prevByTicker = (StrategiesOverrideData ?? new ObservableCollection<StrategiesOverrideDataModel>())
        //        .GroupBy(s => s.TickerName, StringComparer.OrdinalIgnoreCase)
        //        // pick the latest by runtime if multiples exist
        //        .ToDictionary(
        //            g => g.Key,
        //            g => g.OrderByDescending(x => x.RunTime).First(),
        //            StringComparer.OrdinalIgnoreCase
        //        );

        //    var rows = new List<StrategiesOverrideDataModel>();

        //    foreach (var r in MergedRows)
        //    {
        //        if (!r.StrategyNameHasChanged) continue;

        //        // Try to get a previous override for this ticker to copy optional flags
        //        prevByTicker.TryGetValue(r.Tickername ?? string.Empty, out var prev);

        //        var item = new StrategiesOverrideDataModel
        //        {
        //            // required identifiers / meta
        //            RunTime = DateTime.UtcNow,     // stamp now; DB can also handle server-side if you prefer
        //            TickerName = r.Tickername?.Trim(),
        //            Env = _hostEnv,            // you already load this via LoadHostEnvAsync()

        //            // carry forward what we can from previous override (if present)
        //            SortKey = prev?.SortKey ?? 0,
        //            Enable = prev?.Enable ?? true,
        //            StrategyName = r.StrategyName,     // <-- the newly selected strategy

        //            // keep any existing base names if you use them; otherwise null is fine
        //            StrategyNameBase = prev?.StrategyNameBase,
        //            StrategyNameBaseDaily = prev?.StrategyNameBaseDaily,

        //            Watchlist = prev?.Watchlist,
        //            KeepUpdated = prev?.KeepUpdated,
        //            CalcTrades = prev?.CalcTrades,
        //            TakePosition = prev?.TakePosition
        //        };

        //        // Apply the min_* rule based on the selected strategy
        //        bool isDefault = string.Equals(r.StrategyName, "default", StringComparison.OrdinalIgnoreCase);
        //        item.MinUpdateFreq = isDefault ? "default" : "y1";
        //        item.MinChartFreq = isDefault ? "default" : "y1";
        //        item.MinPosFreq = isDefault ? "default" : "y1";

        //        rows.Add(item);
        //    }

        //    return rows;
        //}


        public async Task LoadStrategyNamesAsync()
        {
            StrategyNames.Clear();

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                var resp = await client.PostAsync($"{baseUrl}/get_strategynames",
                                                  new StringContent("{}", Encoding.UTF8, "application/json"));
                resp.EnsureSuccessStatusCode();
                var body = await resp.Content.ReadAsStringAsync();

                var tok = JToken.Parse(body);
                if (tok.Type == JTokenType.Array)
                {
                    foreach (var item in (JArray)tok)
                    {
                        string name = null;
                        if (item.Type == JTokenType.String)
                            name = (string)item;
                        else if (item.Type == JTokenType.Object)
                            name = ((JObject)item).Value<string>("strategyname");

                        if (!string.IsNullOrWhiteSpace(name))
                            StrategyNames.Add(name.Trim());
                    }
                }
            }
        }

        public async Task LoadStrategiesOverrideAsync()
        {
            _overrideByTicker.Clear();

            // If you already loaded env in LoadHostEnvAsync(), prefer it here
            // (leave null to accept all envs)
            string envFilter = _hostEnv; // e.g., "prod", "dev", etc.

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                var resp = await client.PostAsync(
                     $"{baseUrl}/get_strategies_override",
                    new StringContent("{}", Encoding.UTF8, "application/json"));
                resp.EnsureSuccessStatusCode();

                var body = await resp.Content.ReadAsStringAsync();
                var arr = JArray.Parse(body);

                // Keep latest row by (ticker, env) → latest runtime wins
                // If envFilter != null, we only consider that env
                var latestByTicker = new Dictionary<string, StrategiesOverrideDataModel>(StringComparer.OrdinalIgnoreCase);

                foreach (var token in arr)
                {
                    var o = token as JObject; if (o == null) continue;

                    var ticker = (o.Value<string>("tickername") ?? "").Trim();
                    if (ticker.Length == 0) continue;

                    var env = (o.Value<string>("env") ?? "").Trim();
                    if (!string.IsNullOrEmpty(envFilter) &&
                        !string.Equals(env, envFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        continue; // skip mismatched env
                    }

                    // you used JDate(...) previously; keep it if you have it available:
                    DateTime? rt = JDate(o, "runtime"); // falls back to null if parsing fails
                    if (!rt.HasValue) continue;

                    // Map into your StrategiesOverrideDataModel
                    var so = new StrategiesOverrideDataModel
                    {
                        RunTime = rt.Value,
                        TickerName = ticker,
                        Env = env,
                        SortKey = o.Value<int?>("sort_key") ?? 0,
                        Enable = o["enable"]?.ToObject<bool?>(),

                        StrategyName = (o.Value<string>("strategyname") ?? "").Trim(),
                        StrategyNameBase = (o.Value<string>("strategyname_base") ?? "").Trim(),
                        StrategyNameBaseDaily = (o.Value<string>("strategyname_base_daily") ?? "").Trim(),

                        Watchlist = o["watchlist"]?.ToObject<bool?>(),
                        KeepUpdated = o["keep_updated"]?.ToObject<bool?>(),
                        CalcTrades = o["calc_trades"]?.ToObject<bool?>(),
                        TakePosition = o["take_position"]?.ToObject<bool?>(),

                        MinUpdateFreq = (o.Value<string>("min_update_freq") ?? "").Trim(),
                        MinChartFreq = (o.Value<string>("min_chart_freq") ?? "").Trim(),
                        MinPosFreq = (o.Value<string>("min_pos_freq") ?? "").Trim(),
                    };

                    if (!latestByTicker.TryGetValue(ticker, out var existing)
                        || so.RunTime > existing.RunTime)
                    {
                        latestByTicker[ticker] = so;
                    }
                }

                // Commit to the backing dictionary
                foreach (var kv in latestByTicker)
                    _overrideByTicker[kv.Key] = kv.Value;
            }
        }

        public async Task LoadManualFreezeDefsAsync()
        {
            _manualDefs = new List<ManualFreezeDef>();

            var req = new
            {
                //table_name = "errors",
                //where_dict = new Dictionary<string, object> { { "manual", 1 } }
            };

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                var content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");
                var resp = await client.PostAsync($"{baseUrl}/get_manual_trading_errors", content);
                resp.EnsureSuccessStatusCode();
                var json = await resp.Content.ReadAsStringAsync();

                var rows = JArray.Parse(json);
                foreach (var t in rows)
                {
                    var o = t as JObject;
                    if (o == null) continue;

                    var fl = (o.Value<string>("freeze_level") ?? "").Trim().ToLowerInvariant();
                    var ec = o.Value<int?>("error_code") ?? 0;
                    var rs = (o.Value<string>("reason") ?? "").Trim();
                    if (ec != 0 && fl.Length > 0)
                    {
                        var def = new ManualFreezeDef { ErrorCode = ec, FreezeLevel = fl,Reason =rs };
                        _manualDefs.Add(def);
                    }
                }
            }
        }

        public async Task LoadActiveManualFreezesAsync()
        {
            _activeManualFreezes = new List<ActiveFreezeRow>();
            var manualSet = new HashSet<int>(_manualDefs.Select(m => m.ErrorCode));

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                var resp = await client.PostAsync($"{baseUrl}/get_unresolved_tickerfreezer",
                                                  new StringContent("{}", Encoding.UTF8, "application/json"));
                resp.EnsureSuccessStatusCode();
                var body = await resp.Content.ReadAsStringAsync();
                var arr = JArray.Parse(body);

                foreach (var t in arr)
                {
                    var o = t as JObject;
                    if (o == null) continue;

                    var ec = o.Value<int?>("error_code") ?? 0;
                    if (!manualSet.Contains(ec)) continue;

                    var r = new ActiveFreezeRow();
                    r.RunTime = JDate(o, "runtime"); // you already have JDate()
                    r.Tickername = (o.Value<string>("tickername") ?? "").Trim();
                    r.Fundname = (o.Value<string>("fundname") ?? "").Trim();
                    r.Fundgroupname = (o.Value<string>("fundgroupname") ?? "").Trim();
                    r.ErrorCode = ec;
                    r.FreezeLevel = (o.Value<string>("freeze_level") ?? "").Trim().ToLowerInvariant();

                    if (r.Tickername.Length > 0)
                        _activeManualFreezes.Add(r);
                }
            }
        }


        public async Task LoadHostEnvAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                    var resp = await client.PostAsync(
                         $"{baseUrl}/get_hostenv",
                        new StringContent("{}", Encoding.UTF8, "application/json")
                    );

                    if (resp.IsSuccessStatusCode)
                    {
                        var json = await resp.Content.ReadAsStringAsync();
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            try
                            {
                                var jo = JObject.Parse(json);
                                var value = (string)jo["hostenv"];
                                if (!string.IsNullOrWhiteSpace(value))
                                    _hostEnv = value.Trim();
                            }
                            catch
                            {
                                // fallback: if not valid JSON, keep current
                            }
                        }
                    }
                }
            }
            catch
            {
                // keep default "prod" if endpoint is missing or fails
            }
        }

        private bool FreezeMatchesRow(ActiveFreezeRow f, FilterIntervalsViewModel.MergedTickerRow r)
        {
            if (f == null || r == null) return false;

            var level = (f.FreezeLevel ?? "").Replace(" ", "").ToLowerInvariant();

            if (level == "tickername")
            {
                return string.Equals(f.Tickername, r.Tickername, StringComparison.OrdinalIgnoreCase);
            }
            else if (level == "tickername,fundname")
            {
                return string.Equals(f.Tickername, r.Tickername, StringComparison.OrdinalIgnoreCase)
                    && WildEq(f.Fundname, r.FundName);
            }
            else if (level == "tickername,fundgroupname")
            {
                return string.Equals(f.Tickername, r.Tickername, StringComparison.OrdinalIgnoreCase)
                    && WildEq(f.Fundgroupname, r.FundGroup);
            }
            else if (level == "tickername,fundname,fundgroupname")
            {
                return string.Equals(f.Tickername, r.Tickername, StringComparison.OrdinalIgnoreCase)
                    && WildEq(f.Fundname, r.FundName)
                    && WildEq(f.Fundgroupname, r.FundGroup);
            }
            else
            {
                // unknown schema → fall back to ticker-only
                return string.Equals(f.Tickername, r.Tickername, StringComparison.OrdinalIgnoreCase);
            }
        }



        //END NEW

        private void RebuildPredicates()
        {
            _tickerPredicate = BuildPredicate(TickerFilter);
            _groupPredicate = BuildPredicate(FundGroupFilter);
        }

        private static Func<string, bool> BuildPredicate(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return s => true;

            input = input.Trim();

            // No wildcards => fast case-insensitive "contains"
            bool hasWildcards = (input.IndexOf('*') >= 0) || (input.IndexOf('?') >= 0);
            if (!hasWildcards)
            {
                return s => !string.IsNullOrEmpty(s) &&
                            s.IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            // Wildcards present => compile regex ONCE
            string pattern = "^" + Regex.Escape(input)
                                      .Replace(@"\*", ".*")
                                      .Replace(@"\?", ".") + "$";
            var rx = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            return s => !string.IsNullOrEmpty(s) && rx.IsMatch(s);
        }


        private void RestartTimer() { _filterTimer.Stop(); _filterTimer.Start(); }
        private bool RowFilter(object obj)
        {
            var row = obj as MergedTickerRow;
            if (row == null) return false;
            if (row.IsDeleted)
                return false;
            return _tickerPredicate(row.Tickername) &&
                   _groupPredicate(row.FundGroup);
        }

        public async Task LoadFilterIntervalsDataAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                var requestData = new { }; // No payload needed if your endpoint accepts empty POST
                string jsonRequest = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync($"{baseUrl}/filtered_intervals", content);
                response.EnsureSuccessStatusCode();

                string jsonResponse = await response.Content.ReadAsStringAsync();

                var fullResponse = JObject.Parse(jsonResponse);
                var filterIntervalsJson = fullResponse["filter_intervals"];

                // Deserialize into a Dictionary<string, FilterIntervalsDataModel>
                var dict = filterIntervalsJson.ToObject<Dictionary<string, FilterIntervalsDataModel>>();

                FilterIntervalsData.Clear();
                foreach (var kvp in dict)
                {
                    var item = kvp.Value;
                    item.TickerName = kvp.Key; // Add index (key) as ticker if needed
                    FilterIntervalsData.Add(item);
                }
            }
        }


        public async Task LoadScaledPositionsAsync()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{baseUrl}/get_scaled_positions", content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var root = JObject.Parse(json);
        }


        public async Task LoadTadPositionsDataAsync()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            var response = await client.PostAsync( $"{baseUrl}/filtered_intervals", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var root = JObject.Parse(json);

            var positionsJson = (JObject)root["positions"];
            var viewPositionsJson = (JObject)root["viewpositions"];
            var filterPositionsJson = (JObject)root["filterpositions"];
            // Accept either "scaledpositions" or "scaled_positions"
            JToken scaledPositionsJson = root["scaledpositions"] ?? root["scaled_positions"];

            // Work dict keyed by ticker (change to (ticker,fundgroup) later if needed)
            var merged = new Dictionary<string, TadPositionsDataModel>(StringComparer.OrdinalIgnoreCase);

            // helper local function to get-or-create
            TadPositionsDataModel GetOrCreate(string ticker)
            {
                if (!merged.TryGetValue(ticker, out var m))
                {
                    m = new TadPositionsDataModel { Tickername = ticker };
                    merged[ticker] = m;
                }
                return m;
            }

            // 1) positions
            if (positionsJson != null)
            {
                foreach (var kv in positionsJson) // kv.Key = ticker, kv.Value = row object
                {
                    var ticker = kv.Key;
                    var row = (JObject)kv.Value;
                    var model = GetOrCreate(ticker);

                    // Map fields you expect (null-safe conversions)
                    model.PositionBaseY1 = row.Value<float?>("position_base_y1");
                    model.PositionBaseH1 = row.Value<float?>("position_base_h1");
                    model.PositionBaseD1 = row.Value<float?>("position_base_d1");
                    model.PositionY1 = row.Value<float?>("position_y1");
                    model.PositionY2 = row.Value<float?>("position_y2");
                    model.PositionY3 = row.Value<float?>("position_y3");
                    model.PositionH2 = row.Value<float?>("position_h2");
                    model.PositionH3 = row.Value<float?>("position_h3");
                    model.PositionH4 = row.Value<float?>("position_h4");
                    //model.PositionH5 = row.Value<float?>("position_h5");
                    model.PositionH6 = row.Value<float?>("position_h6");
                    model.PositionH12 = row.Value<float?>("position_h12");
                    model.PositionH16 = row.Value<float?>("position_h16");
                    model.PositionD1 = row.Value<float?>("position_D1");
                    model.PositionH36 = row.Value<float?>("position_h36");
                    model.PositionD2 = row.Value<float?>("position_D2");
                    model.PositionD3 = row.Value<float?>("position_D3");
                    model.PositionD4 = row.Value<float?>("position_D4");
                    model.PositionW1 = row.Value<float?>("position_W1");
                    model.PositionD8 = row.Value<float?>("position_D8");
                    model.PositionW2 = row.Value<float?>("position_W2");
                    model.PositionDeployment = row.Value<float?>("deployment");
                    model.PositionNumTrades = row.Value<float?>("num_trades") ;
                    model.PositionNumIntervals = row.Value<float?>("num_posintervals") ;
                }
            }

            // 2) viewpositions
            if (viewPositionsJson != null)
            {
                foreach (var kv in viewPositionsJson)
                {
                    var ticker = kv.Key;
                    var row = (JObject)kv.Value;
                    var model = GetOrCreate(ticker);

                    model.NumViewTrades = row.Value<float?>("num_view_trades") ?? 0;
                }
            }

            // 3) filterpositions
            if (filterPositionsJson != null)
            {
                foreach (var kv in filterPositionsJson)
                {
                    var ticker = kv.Key;
                    var row = (JObject)kv.Value;
                    var model = GetOrCreate(ticker);

                    model.NumFilteredTrades = row.Value<float?>("num_filtered_trades") ?? 0;
                    model.NumFiltIntervals = row.Value<int?>("num_filtintervals") ?? 0;
                    model.FilteredDeployment = row.Value<float?>("filtered_deployment") ?? 0;
                }
            }

            if (scaledPositionsJson != null &&
                scaledPositionsJson.Type != JTokenType.Null &&
                scaledPositionsJson.Type != JTokenType.Undefined)
            {
                var list = new List<ScaledPositionsDataModel>();

                if (scaledPositionsJson.Type == JTokenType.Array)
                {
                    foreach (JToken t in (JArray)scaledPositionsJson)
                    {
                        var item = t.ToObject<ScaledPositionsDataModel>() ?? new ScaledPositionsDataModel();

                        // Map scale_factor from backend: scaled_position → ScaledPercent
                        if (item.ScaledPercent == null)
                            item.ScaledPercent = t.Value<double?>("scaled_position");
                        if (item.ScaledTarget == null)
                            item.ScaledTarget = t.Value<double?>("scaled_position");

                        list.Add(item);
                    }
                }
                else if (scaledPositionsJson.Type == JTokenType.Object)
                {
                    foreach (var kv in (JObject)scaledPositionsJson)
                    {
                        var t = (JObject)kv.Value;
                        var item = t.ToObject<ScaledPositionsDataModel>() ?? new ScaledPositionsDataModel();

                        if (string.IsNullOrWhiteSpace(item.TickerName))
                            item.TickerName = kv.Key;

                        if (item.ScaledPercent == null)
                            item.ScaledPercent = t.Value<double?>("scaled_position");
                        if (item.ScaledTarget == null)
                            item.ScaledTarget = t.Value<double?>("scaled_position");
                        list.Add(item);
                    }
                }

                // Optional trims
                foreach (var r in list)
                {
                    r.FundGroupName = r.FundGroupName?.Trim();
                    r.FundName = r.FundName?.Trim();
                    r.TickerName = r.TickerName?.Trim();
                }

                ScaledPositionsData = new ObservableCollection<ScaledPositionsDataModel>(
                    list.OrderBy(x => x.TickerName, StringComparer.OrdinalIgnoreCase)
                );
            }
            else
            {
                // empty/missing → just clear
                ScaledPositionsData = new ObservableCollection<ScaledPositionsDataModel>();
            }


            // Push into your ObservableCollection
            TadPositionsData.Clear();
            foreach (var m in merged.Values.OrderBy(x => x.Tickername))
                TadPositionsData.Add(m);
        }
        private static string NormalizeLevel(string s)
        {
            return (s ?? "").Replace(" ", "").ToLowerInvariant();
        }
        //unfreeze 
        public void RemoveRow(MergedTickerRow row)
        {
            if (row == null) return;
            //first figure out the freeze level

            var freezer = new TickerFreezer(SqlConn);
            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); // your DB likes string runtimes


            var r = row as MergedTickerRow;
            string level = DetermineFreezeLevel(r);
            var def = GetManualErrorDefForLevel(level);
            int errorCode = def?.ErrorCode ?? 9999;
            string errorReason = string.IsNullOrWhiteSpace(def?.Reason)
                ? $"Manual trading freeze ({level})"
                : def.Reason;

            // Normalise wildcards to "*"
            string fund = string.IsNullOrWhiteSpace(r.FundName) ? "*" : r.FundName.Trim();
            string group = string.IsNullOrWhiteSpace(r.FundGroup) ? "*" : r.FundGroup.Trim();
            string tkr = r.Tickername?.Trim() ?? "*";

            var match = _activeManualFreezes.FirstOrDefault(f => FreezeMatchesRow(f, r));

            // Fallback to "now" only if we couldn't find a matching unresolved freeze
            string thawRuntime = (match?.RunTime?.ToString("yyyy-MM-dd HH:mm:ss"))
                                      ?? now;
            // Resolve existing manual freeze entry
            freezer.Thaw(
                runtime: thawRuntime,
                ems: "*",
                broker_code_exec: "*",
                fundgroupname: group,
                fundname: fund,
                subaccountname: "*",
                execaccountname: "*",
                tad_id: "*",
                tickername: tkr,
                error_code: errorCode.ToString(),
                benchmarkname: "*",
                notes: "FilterIntervals UI",
                gbl_conn: SqlConn
            );
            // Remove from MergedRows
            MergedRows.Remove(r);

        }
        private bool ManualAppliesToRowByRowLevel(MergedTickerRow r)
        {
            if (r == null || _activeManualFreezes == null || _activeManualFreezes.Count == 0)
                return false;

            // Use the row’s current level
            var level = DetermineFreezeLevel(r);                 // e.g. "tickername,fundname"
            var nlevel = NormalizeLevel(level);

            // Treat null/blank as "*" for comparison with WildEq
            string rowFund = string.IsNullOrWhiteSpace(r.FundName) ? "*" : r.FundName.Trim();
            string rowGroup = string.IsNullOrWhiteSpace(r.FundGroup) ? "*" : r.FundGroup.Trim();

            foreach (var f in _activeManualFreezes)
            {
                if (!string.Equals(NormalizeLevel(f.FreezeLevel), nlevel, StringComparison.Ordinal))
                    continue;

                if (!string.Equals(f.Tickername, r.Tickername, StringComparison.OrdinalIgnoreCase))
                    continue;

                // The row-level dictates which fields must match (with "*" allowed on either side)
                if (nlevel == "tickername")
                    return true;

                if (nlevel == "tickername,fundname")
                    return WildEq(f.Fundname, rowFund);

                if (nlevel == "tickername,fundgroupname")
                    return WildEq(f.Fundgroupname, rowGroup);

                if (nlevel == "tickername,fundname,fundgroupname")
                    return WildEq(f.Fundname, rowFund) && WildEq(f.Fundgroupname, rowGroup);
            }

            // Also allow a broader freeze ("tickername") to apply to more specific row levels.
            foreach (var f in _activeManualFreezes)
            {
                if (NormalizeLevel(f.FreezeLevel) == "tickername" &&
                    string.Equals(f.Tickername, r.Tickername, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

       

        public async Task RebuildMerged(bool preserveUserFiFlags = false)
        {
            // 1) Ensure defs and active freezes are loaded (and in the right order).
            if (_manualDefs == null || _manualDefs.Count == 0)
                await LoadManualFreezeDefsAsync();

            if (_activeManualFreezes == null || _activeManualFreezes.Count == 0)
                await LoadActiveManualFreezesAsync();

            string MakeKey(string t, string fg, string fn)
            {
                return $"{(t ?? "").Trim().ToUpperInvariant()}|{(fg ?? "").Trim().ToUpperInvariant()}|{(fn ?? "").Trim().ToUpperInvariant()}";
            }

            var scaledByComposite = (ScaledPositionsData ?? new ObservableCollection<ScaledPositionsDataModel>())
                .GroupBy(s => MakeKey(s.TickerName, s.FundGroupName, s.FundName))
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            // capture current rows (for preserving user edits/baselines)
            Dictionary<string, MergedTickerRow> previous = null;
            if (preserveUserFiFlags)
                previous = MergedRows.ToDictionary(r => r.Tickername, r => r, StringComparer.OrdinalIgnoreCase);

            MergedRows.Clear();

            var fiByTicker = (FilterIntervalsData ?? new ObservableCollection<FilterIntervalsDataModel>())
                .GroupBy(x => x.TickerName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            // ---- MAIN LOOP: TAD positions as left side ----
            foreach (var tp in TadPositionsData)
            {
                fiByTicker.TryGetValue(tp.Tickername, out var fi);

                var row = new MergedTickerRow
                {
                    Tickername = tp.Tickername,

                    // start from server FI values…
                    Runtime = fi?.Runtime ?? default,
                    FundGroup = fi?.FundGroup,
                    FundName = fi?.FundName,
                    Rescale = fi?.Rescale,
                    LongOnly = fi?.LongOnly,
                    ShortOnly = fi?.ShortOnly,
                    BuyOnly = fi?.BuyOnly,
                    SellOnly = fi?.SellOnly,
                    AllIntervals = fi?.AllIntervals,

                    BaseY1 = fi?.BaseY1,
                    BaseH1 = fi?.BaseH1,
                    BaseD1 = fi?.BaseD1,
                    y1 = fi?.Y1,
                    y2 = fi?.Y2,
                    y3 = fi?.Y3,
                    H2 = fi?.H2,
                    H3 = fi?.H3,
                    H4 = fi?.H4,
                    //H5   = fi?.H5,
                    H6 = fi?.H6,
                    H12 = fi?.H12,
                    H16 = fi?.H16,
                    D1 = fi?.D1,
                    H36 = fi?.H36,
                    D2 = fi?.D2,
                    D3 = fi?.D3,
                    D4 = fi?.D4,
                    W1 = fi?.W1,
                    D8 = fi?.D8,
                    W2 = fi?.W2,

                    // positions side
                    PositionBaseY1 = tp.PositionBaseY1,
                    PositionBaseH1 = tp.PositionBaseH1,
                    PositionBaseD1 = tp.PositionBaseD1,
                    PositionY1 = tp.PositionY1,
                    PositionY2 = tp.PositionY2,
                    PositionY3 = tp.PositionY3,
                    PositionH2 = tp.PositionH2,
                    PositionH3 = tp.PositionH3,
                    PositionH4 = tp.PositionH4,
                    //PositionH5 = tp.PositionH5,
                    PositionH6 = tp.PositionH6,
                    PositionH12 = tp.PositionH12,
                    PositionH16 = tp.PositionH16,
                    PositionD1 = tp.PositionD1,
                    PositionH36 = tp.PositionH36,
                    PositionD2 = tp.PositionD2,
                    PositionD3 = tp.PositionD3,
                    PositionD4 = tp.PositionD4,
                    PositionW1 = tp.PositionW1,
                    PositionD8 = tp.PositionD8,
                    PositionW2 = tp.PositionW2,

                    NumFilteredTrades = tp.NumFilteredTrades,
                    NumFiltIntervals = tp.NumFiltIntervals,
                    FilteredDeployment = tp.FilteredDeployment,
                    NumTrades = tp.PositionNumTrades,
                    NumPositionIntervals = tp.PositionNumIntervals,
                    ViewDeployment = tp.ViewDeployment,
                    PositionDeployment = tp.PositionDeployment,
                };

                // scaled positions join by composite key
                if (scaledByComposite.TryGetValue(MakeKey(tp.Tickername, fi?.FundGroup, fi?.FundName), out var sp))
                {
                    row.ScaleFactor = sp?.ScaledTarget;
                    row.ScaledPercent = sp?.ScaledPercent;
                }

                // Manual flag via your row-level matcher
                row.Manual = ManualAppliesToRowByRowLevel(row);

                // Strategy override: attach the full model if available
                if (_overrideByTicker.TryGetValue(row.Tickername, out var soModel))
                    row.AttachStrategyOverride(soModel);
                else
                    row.AttachStrategyOverride(null); // will set StrategyName to null/"default" per your method

                // Preserve current user edits (and original baselines) if requested
                if (preserveUserFiFlags && previous != null && previous.TryGetValue(tp.Tickername, out var old))
                {
                    // overwrite with user's CURRENT flags
                    row.Rescale = old.Rescale;
                    row.LongOnly = old.LongOnly;
                    row.ShortOnly = old.ShortOnly;
                    row.BuyOnly = old.BuyOnly;
                    row.SellOnly = old.SellOnly;
                    row.AllIntervals = old.AllIntervals;

                    row.BaseY1 = old.BaseY1;
                    row.BaseH1 = old.BaseH1;
                    row.BaseD1 = old.BaseD1;
                    row.y1 = old.y1;
                    row.y2 = old.y2;
                    row.y3 = old.y3;
                    row.H2 = old.H2;
                    row.H3 = old.H3;
                    row.H4 = old.H4;
                    //row.H5 = old.H5;
                    row.H6 = old.H6;
                    row.H12 = old.H12;
                    row.H16 = old.H16;
                    row.D1 = old.D1;
                    row.H36 = old.H36;
                    row.D2 = old.D2;
                    row.D3 = old.D3;
                    row.D4 = old.D4;
                    row.W1 = old.W1;
                    row.D8 = old.D8;
                    row.W2 = old.W2;

                    // preserve manual & strategy state from the previous snapshot
                    row.Manual = old.Manual;
                    // if your Attach method also sets OriginalStrategyName, keep that consistent:
                    if (old.StrategyOverride != null) row.AttachStrategyOverride(old.StrategyOverride);
                    else row.StrategyName = old.StrategyName;

                    // keep ORIGINALS so HasChanged continues to compare to the same baseline
                    row.RestoreOriginalsFrom(old);
                }
                else
                {
                    // fresh baseline from server values (and attached strategy)
                    row.SnapshotOriginals();
                }

                row.RecalcNewTrades();
                row.RecalcRescaledIntervals();
                row.RecalcNewDeployment();
                row.ReCalcScaledDeployment();

                MergedRows.Add(row);
            }

            // ---- SECOND LOOP: FI-only rows (no TAD position) ----
            foreach (var fiOnly in fiByTicker.Values
                         .Where(fi => !TadPositionsData.Any(tp =>
                                string.Equals(tp.Tickername, fi.TickerName, StringComparison.OrdinalIgnoreCase))))
            {
                var row = new MergedTickerRow
                {
                    Tickername = fiOnly.TickerName,
                    Runtime = fiOnly.Runtime,
                    FundGroup = fiOnly.FundGroup,
                    FundName = fiOnly.FundName,
                    Rescale = fiOnly.Rescale,
                    LongOnly = fiOnly.LongOnly,
                    ShortOnly = fiOnly.ShortOnly,
                    BuyOnly = fiOnly.BuyOnly,
                    SellOnly = fiOnly.SellOnly,
                    AllIntervals = fiOnly.AllIntervals,

                    BaseY1 = fiOnly.BaseY1,
                    BaseH1 = fiOnly.BaseH1,
                    BaseD1 = fiOnly.BaseD1,
                    y1 = fiOnly.Y1,
                    y2 = fiOnly.Y2,
                    y3 = fiOnly.Y3,
                    H2 = fiOnly.H2,
                    H3 = fiOnly.H3,
                    H4 = fiOnly.H4,
                    H6 = fiOnly.H6,
                    H12 = fiOnly.H12,
                    H16 = fiOnly.H16,
                    D1 = fiOnly.D1,
                    H36 = fiOnly.H36,
                    D2 = fiOnly.D2,
                    D3 = fiOnly.D3,
                    D4 = fiOnly.D4,
                    W1 = fiOnly.W1,
                    D8 = fiOnly.D8,
                    W2 = fiOnly.W2,
                };

                // join scaled positions for FI-only row as well
                if (scaledByComposite.TryGetValue(MakeKey(row.Tickername, row.FundGroup, row.FundName), out var sp2))
                {
                    row.ScaleFactor = sp2?.ScaledTarget;
                    row.ScaledPercent = sp2?.ScaledPercent;
                }

                if (preserveUserFiFlags && previous != null && previous.TryGetValue(fiOnly.TickerName, out var oldFi))
                {
                    row.Rescale = oldFi.Rescale;
                    row.LongOnly = oldFi.LongOnly;
                    row.ShortOnly = oldFi.ShortOnly;
                    row.BuyOnly = oldFi.BuyOnly;
                    row.SellOnly = oldFi.SellOnly;
                    row.AllIntervals = oldFi.AllIntervals;

                    // also preserve manual & strategy from previous row
                    row.Manual = oldFi.Manual;
                    if (oldFi.StrategyOverride != null) row.AttachStrategyOverride(oldFi.StrategyOverride);
                    else row.StrategyName = oldFi.StrategyName;

                    row.RestoreOriginalsFrom(oldFi);
                }
                else
                {
                    // compute manual for FI-only row
                    row.Manual = ManualAppliesToRowByRowLevel(row);

                    // attach latest strategy override if present
                    if (_overrideByTicker.TryGetValue(row.Tickername, out var so2))
                        row.AttachStrategyOverride(so2);
                    else
                        row.AttachStrategyOverride(null);

                    row.SnapshotOriginals();
                }

                row.RecalcNewTrades();
                row.RecalcRescaledIntervals();
                row.RecalcNewDeployment();
                row.ReCalcScaledDeployment();

                MergedRows.Add(row);
            }

            if (MergedRowsView != null)
            {
                // 🔹 Sort by FundGroup, then FundName, then Tickername
                MergedRowsView.SortDescriptions.Clear();
                MergedRowsView.SortDescriptions.Add(
                    new SortDescription(nameof(MergedTickerRow.FundGroup), ListSortDirection.Ascending));
                MergedRowsView.SortDescriptions.Add(
                    new SortDescription(nameof(MergedTickerRow.FundName), ListSortDirection.Ascending));
                MergedRowsView.SortDescriptions.Add(
                    new SortDescription(nameof(MergedTickerRow.Tickername), ListSortDirection.Ascending));

                MergedRowsView.Refresh();
            }

        }

        // inside FilterIntervalsViewModel
        public static FilterIntervalsUpsertRow ToUpsertRow(MergedTickerRow r) => new FilterIntervalsUpsertRow
        {
            FundGroup = r.FundGroup?.Trim(), 
            FundName = r.FundName?.Trim(),
            Tickername = r.Tickername?.Trim(),
            Rescale = r.Rescale,
            LongOnly = r.LongOnly,
            ShortOnly = r.ShortOnly,
            BuyOnly = r.BuyOnly,
            SellOnly = r.SellOnly,
            AllIntervals = r.AllIntervals,
            BaseY1 = r.BaseY1,
            BaseH1 = r.BaseH1,
            BaseD1 = r.BaseD1,
            Y1 = r.y1,
            Y2 = r.y2,
            Y3 = r.y3,
            H2 = r.H2,
            H3 = r.H3,
            H4 = r.H4,
            //H5 = r.H5,
            H6 = r.H6,
            H12 = r.H12,
            H16 = r.H16,
            D1 = r.D1,
            H36 = r.H36,
            D2 = r.D2,
            D3 = r.D3,
            D4 = r.D4,
            W1 = r.W1,
            D8 = r.D8,
            W2 = r.W2
        };


        // DTO for status
        public sealed class UpsertJobStatus
        {
            [Newtonsoft.Json.JsonProperty("job_id")]
            public string JobId { get; set; }

            [Newtonsoft.Json.JsonProperty("status")]
            public string Status { get; set; }   // queued | running | done | failed

            [Newtonsoft.Json.JsonProperty("message")]
            public string Message { get; set; }

            [Newtonsoft.Json.JsonProperty("progress")]
            public int? Progress { get; set; }   // coarse 0..100

            [Newtonsoft.Json.JsonProperty("processed")]
            public int? Processed { get; set; }

            [Newtonsoft.Json.JsonProperty("total")]
            public int? Total { get; set; }

            [Newtonsoft.Json.JsonProperty("percent")]
            public int? Percent { get; set; }    // preferred % if total>0 on server

            [Newtonsoft.Json.JsonProperty("started_at")]
            public string StartedAt { get; set; }

            [Newtonsoft.Json.JsonProperty("updated_at")]
            public string UpdatedAt { get; set; }

            [Newtonsoft.Json.JsonIgnore]
            public int EffectivePercent =>
                Percent
                ?? Progress
                ?? (Processed.HasValue && Total.HasValue && Total.Value > 0
                        ? (int)Math.Round(100.0 * Processed.Value / Total.Value)
                        : (string.Equals(Status, "done", StringComparison.OrdinalIgnoreCase) ? 100 : 0));


            [Newtonsoft.Json.JsonIgnore]
            public bool IsTerminal => string.Equals(Status, "done", StringComparison.OrdinalIgnoreCase)
                                   || string.Equals(Status, "failed", StringComparison.OrdinalIgnoreCase);
        }


        public async Task<string> UpsertFilterIntervalsStartAsync(
            IEnumerable<FilterIntervalsUpsertRow> rows,
            IList<string> tickersToProcess = null,
            IList<object> strategyOverrideRows = null)   //fixes for  <—
        {
            // Clean up tickers if any
            List<string> cleanTickers = null;
            if (tickersToProcess != null && tickersToProcess.Count > 0)
            {
                cleanTickers = tickersToProcess
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Select(t => t.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            // Build payload
            var payloadDict = new Dictionary<string, object>
            {
                ["rows"] = rows.ToList()
            };

            if (cleanTickers != null && cleanTickers.Count > 0)
                payloadDict["tickernames"] = cleanTickers;

            if (strategyOverrideRows != null && strategyOverrideRows.Count > 0)
                payloadDict["strategy_override_rows"] = strategyOverrideRows; // Json.NET will use your [JsonProperty]s

            var json = JsonConvert.SerializeObject(payloadDict);

            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            using (var resp = await _http.PostAsync("/upsert_filter_intervals/start", content))
            {
                resp.EnsureSuccessStatusCode();
                var body = await resp.Content.ReadAsStringAsync();
                try
                {
                    var jo = JObject.Parse(body);
                    if (jo["job_id"] != null) return (string)jo["job_id"];
                }
                catch { /* fall-through */ }
                return body.Trim('"', ' ', '\n', '\r');
            }
        }

        // in FilterIntervalsViewModel
        public async Task<string> InsertScaledPositionsStartAsync(IEnumerable<ScaledPositionsInsertRow> rows)
        {
            var payload = new { rows = rows };
            var json = JsonConvert.SerializeObject(payload);

            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            using (var resp = await _http.PostAsync("/insert_scaled_positions/start", content).ConfigureAwait(false))
            {
                resp.EnsureSuccessStatusCode();

                var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                var jo = JObject.Parse(body);
                return (string)jo["job_id"];
            }
        }

        public async Task<UpsertJobStatus> GetInsertScaledPositionsStatusAsync(string jobId)
        {
            var url = "/insert_scaled_positions/status/" + Uri.EscapeDataString(jobId ?? string.Empty);

            using (var resp = await _http.GetAsync(url).ConfigureAwait(false))
            {
                // Optional back-compat shim only if you may still have the old route live somewhere:
                if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    using (var fallback = await _http.GetAsync("/scaled_positions/status/" + Uri.EscapeDataString(jobId ?? string.Empty)).ConfigureAwait(false))
                    {
                        fallback.EnsureSuccessStatusCode();
                        var fb = await fallback.Content.ReadAsStringAsync().ConfigureAwait(false);
                        return JsonConvert.DeserializeObject<UpsertJobStatus>(fb);
                    }
                }

                resp.EnsureSuccessStatusCode();

                var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonConvert.DeserializeObject<UpsertJobStatus>(body);
            }
        }


        // Poll status
        public async Task<UpsertJobStatus> GetUpsertFilterIntervalsStatusAsync(string jobId, System.Threading.CancellationToken ct = default)
        {
            var url = "/upsert_filter_intervals/status/" + Uri.EscapeDataString(jobId);
            using (var resp = await _http.GetAsync(url, ct))
            {
                if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return new UpsertJobStatus { JobId = jobId, Status = "failed", Message = "Unknown job" };

                resp.EnsureSuccessStatusCode();
                var body = await resp.Content.ReadAsStringAsync();
                var status = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertJobStatus>(body);
                return status ?? new UpsertJobStatus { JobId = jobId, Status = "failed", Message = "Empty response" };
            }
        }

        private UpsertJobStatus _jobStatus;
        public UpsertJobStatus JobStatus
        {
            get => _jobStatus;
            private set
            {
                if (!Equals(_jobStatus, value))
                {
                    _jobStatus = value;
                    OnPropertyChanged(nameof(JobStatus));
                    OnPropertyChanged(nameof(IsUpsertRunning));
                    OnPropertyChanged(nameof(ProgressPercent));
                    OnPropertyChanged(nameof(StatusMessage));
                    OnPropertyChanged(nameof(IsSaveEnabled));
                }
            }
        }
        public void ApplyJobStatus(UpsertJobStatus status) => JobStatus = status;
        public void ClearJobStatus() => JobStatus = null;

        // ---- client-side visual boost ----
        private int _uiBoostPercent = 0;

        public void ResetBoost()
        {
            _uiBoostPercent = 0;
            OnPropertyChanged(nameof(ProgressPercent));
        }

        public void IncreaseBoost(int delta)
        {
            int old = _uiBoostPercent;
            _uiBoostPercent = Math.Min(90, _uiBoostPercent + delta);
            if (_uiBoostPercent != old)
                OnPropertyChanged(nameof(ProgressPercent));
        }



        public bool IsUpsertRunning =>
            JobStatus != null && !JobStatus.IsTerminal;


        public int ProgressPercent
        {
            get
            {
                int server = JobStatus?.EffectivePercent ?? 0;

                // When the job is terminal, show the real result (100 on done)
                if (JobStatus?.IsTerminal == true)
                {
                    return string.Equals(JobStatus.Status, "done", StringComparison.OrdinalIgnoreCase) ? 100 : server;
                }

                // While running: blend server with client boost, but never exceed 90%
                return Math.Min(90, Math.Max(server, _uiBoostPercent));
            }
        }


        public string StatusMessage => JobStatus?.Message ?? string.Empty;

        // Handy single flag for the Save button
        public bool IsSaveEnabled => !IsExecuting && !IsUpsertRunning;

        public class MergedTickerRow : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string p = null)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
            private bool _isInitialized;
            public string Tickername { get; set; }
            public string FundGroup { get; set; }
            public string FundName { get; set; }

            public bool? OriginalManual { get; private set; }
            private bool? _manual;
            public bool? Manual
            {
                get => _manual;
                set
                {
                    if (_manual != value)
                    {
                        _manual = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(ManualHasChanged));
                    }
                }
            }
            public bool ManualHasChanged => _isInitialized && _manual != OriginalManual;

            // --- NEW: StrategyName (join from strategies_override) ---
            public string OriginalStrategyName { get; private set; }
            private string _strategyName;
            public string StrategyName
            {
                get => _strategyName;
                set
                {
                    if (_strategyName != value)
                    {
                        _strategyName = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(StrategyNameHasChanged));
                    }
                }
            }
            public bool StrategyNameHasChanged => _isInitialized && !string.Equals(_strategyName, OriginalStrategyName, StringComparison.Ordinal);

            private float? _scaledDeployment { get; set; }
            public float? ScaledDeployment
            {
                get => _scaledDeployment;
                set
                {
                    if (_scaledDeployment != value)
                    {
                        _scaledDeployment = value;
                        OnPropertyChanged();
                    }
                }
            }
            
            public double? OriginalNewRescaleFactor { get; set; }
            private double? _newScaleFactor;
            public double? NewScaleFactor
            {
                get => _newScaleFactor;
                set
                {
                    if (_newScaleFactor != value)
                    {
                        _newScaleFactor = value;
                        ReCalcScaledDeployment();
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(NewScaleFactorHasChanged));
                    }
                }
            }
            public bool NewScaleFactorHasChanged => _isInitialized && _newScaleFactor != OriginalNewRescaleFactor;

            private double? _scaledPercent;
            public double? ScaledPercent
            {
                get => _scaledPercent;
                set
                {
                    if (_scaledPercent != value)
                    {
                        _scaledPercent = value;
                        OnPropertyChanged();
                    }
                }
            }

            
            private double? _scaleFactor;
            public double? ScaleFactor
            {
                get => _scaleFactor;
                set
                {
                    if (_scaleFactor != value)
                    {
                        _scaleFactor = value;
                        OnPropertyChanged();
                    }
                }
            }
            public bool? OriginalRescale { get; set; }
            private bool? _rescale;
            public bool? Rescale 
            { get => _rescale; 
              set
              {
                  if (_rescale != value)
                  {
                      _rescale = value;
                      OnPropertyChanged(nameof(Rescale));
                        OnPropertyChanged(nameof(RescaleHasChanged));
                        RecalcRescaledIntervals();
                    }
              } 
            
            }
            public bool RescaleHasChanged => _isInitialized && _rescale != OriginalRescale;
            public bool? OriginalLongOnly { get; private set; }
            private bool? _longOnly;
            public bool? LongOnly
            {
                get => _longOnly;
                set
                {
                    if (_longOnly != value)
                    {
                        _longOnly = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(LongOnlyHasChanged));
                        RecalcNewTrades();
                        RecalcNewDeployment();
                    }
                }
            }
            public bool LongOnlyHasChanged => _isInitialized && _longOnly != OriginalLongOnly;

            public bool? OriginalShortOnly { get; private set; }
            private bool? _shortOnly;
            public bool? ShortOnly
            {
                get => _shortOnly;
                set
                {
                    if (_shortOnly != value)
                    {
                        _shortOnly = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(ShortOnlyHasChanged));
                        RecalcNewTrades();
                        RecalcNewDeployment();
                    }
                }
            }
            public bool ShortOnlyHasChanged => _isInitialized && _shortOnly != OriginalShortOnly;

            public bool? OriginalBuyOnly { get; private set; }
            private bool? _buyOnly;
            public bool? BuyOnly
            {
                get => _buyOnly;
                set
                {
                    if (_buyOnly != value)
                    {
                        _buyOnly = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(BuyOnlyHasChanged));
                        RecalcNewTrades();
                    }
                }
            }
            public bool BuyOnlyHasChanged => _isInitialized && _buyOnly != OriginalBuyOnly;

            public bool? OriginalSellOnly { get; private set; }
            private bool? _sellOnly;
            public bool? SellOnly
            {
                get => _sellOnly;
                set
                {
                    if (_sellOnly != value)
                    {
                        _sellOnly = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(SellOnlyHasChanged));
                        RecalcNewTrades();
                    }
                }
            }
            public bool SellOnlyHasChanged => _isInitialized && _sellOnly != OriginalSellOnly;

            public bool? OriginalAllIntervals { get; private set; }
            private bool? _allIntervals;
            public bool? AllIntervals
            {
                get => _allIntervals;
                set
                {
                    if (_allIntervals != value)
                    {
                        _allIntervals = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(AllIntervalsHasChanged));
                        RecalcNewTrades();
                    }
                }
            }
            public bool AllIntervalsHasChanged => _isInitialized && _allIntervals != OriginalAllIntervals;

            public bool? OriginalBaseY1 { get; private set; }
            private bool? _baseY1;
            public bool? BaseY1
            {
                get => _baseY1;
                set
                {
                    if (_baseY1 != value)
                    {
                        _baseY1 = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(BaseY1HasChanged));
                        OnPropertyChanged(nameof(BaseY1Brush));
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool BaseY1HasChanged => _isInitialized && _baseY1 != OriginalBaseY1;
            public float? PositionBaseY1
            {
                get => _positionBaseY1;
                set
                {
                    if (_positionBaseY1 != value)
                    {
                        _positionBaseY1 = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(BaseY1Brush));       // <—
                    }
                }
            }
            private float? _positionBaseY1;

            public bool? OriginalBaseH1 { get; private set; }
            private bool? _baseH1;
            public bool? BaseH1
            {
                get => _baseH1;
                set
                {
                    if (_baseH1 != value)
                    {
                        _baseH1 = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(BaseH1HasChanged));
                        OnPropertyChanged(nameof(BaseH1Brush));
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool BaseH1HasChanged => _isInitialized && _baseH1 != OriginalBaseH1;
            private float? _positionBaseH1;
            public float? PositionBaseH1
            {
                get => _positionBaseH1; 
                set
                {
                    if (_positionBaseH1 != value)
                    {
                        _positionBaseH1 = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(BaseH1Brush));       // <—
                    }
                }
            }

            public bool? OriginalBaseD1 { get; private set; }
            private bool? _baseD1;
            public bool? BaseD1
                {
                get => _baseD1;
                set
                {
                    if (_baseD1 != value)
                    {
                        _baseD1 = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(BaseD1HasChanged));
                        OnPropertyChanged(nameof(BaseD1Brush));
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool BaseD1HasChanged => _isInitialized && _baseD1 != OriginalBaseD1;
            private float? _positionBaseD1;
            public float? PositionBaseD1
            {
                get => _positionBaseD1;
                set
                {
                    if (_positionBaseD1 != value)
                    {
                        _positionBaseD1 = value;
                        OnPropertyChanged(nameof(PositionBaseD1));
                        OnPropertyChanged(nameof(BaseD1Brush));       // <—
                    }
                }
            }
            

            public bool? OriginalY1 { get; private set; }
            private bool? _y1;
            public bool? y1
                {
                get => _y1;
                set
                {
                    if (_y1 != value)
                    {
                        _y1 = value;
                        OnPropertyChanged(nameof(y1));
                        OnPropertyChanged(nameof(Y1HasChanged));
                        OnPropertyChanged(nameof(Y1Brush));       // <—
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool Y1HasChanged => _isInitialized && _y1 != OriginalY1;
            private float? _positionY1;
            public float? PositionY1
            {
                get => _positionY1;
                set
                {
                    if (_positionY1 != value)
                    {
                        _positionY1 = value;
                        OnPropertyChanged(nameof(PositionY1));
                        OnPropertyChanged(nameof(Y1Brush));       // <—
                    }
                }
            }

            public bool? OriginalY2 { get; private set; }
            private bool? _y2;
            public bool? y2
            {
                get => _y2;
                set
                {
                    if (_y2 != value)
                    {
                        _y2 = value;
                        OnPropertyChanged(nameof(y2));
                        OnPropertyChanged(nameof(Y2HasChanged));
                        OnPropertyChanged(nameof(Y2Brush));       // <—
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool Y2HasChanged => _isInitialized && _y2 != OriginalY2;
            private float? _positionY2;
            public float? PositionY2
            {
                get => _positionY2;
                set
                {
                    if (_positionY2 != value)
                    {
                        _positionY2 = value;
                        OnPropertyChanged(nameof(PositionY2));
                        OnPropertyChanged(nameof(Y2Brush));       // <—
                    }
                }
            }

            public bool? OriginalY3 { get; private set; }
            private bool? _y3;
            public bool? y3
            {
                get => _y3;
                set
                {
                    if (_y3 != value)
                    {
                        _y3 = value;
                        OnPropertyChanged(nameof(y3));
                        OnPropertyChanged(nameof(Y3HasChanged));
                        OnPropertyChanged(nameof(Y3Brush));       // <—
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool Y3HasChanged => _isInitialized && _y3 != OriginalY3;
            private float? _positionY3;
            public float? PositionY3
            {
                get => _positionY3;
                set
                {
                    if (_positionY3 != value)
                    {
                        _positionY3 = value;
                        OnPropertyChanged(nameof(PositionY3));
                        OnPropertyChanged(nameof(Y3Brush));       // <—
                    }
                }
            }

            public bool? OriginalH2 { get; private set; }
            private bool? _h2;
            public bool? H2
                {
                get => _h2;
                set
                {
                    if (_h2 != value)
                    {
                        _h2 = value;
                        OnPropertyChanged(nameof(H2));
                        OnPropertyChanged(nameof(H2HasChanged));
                        OnPropertyChanged(nameof(H2Brush));       // <—
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool H2HasChanged => _isInitialized && _h2 != OriginalH2;
            private float? _positionH2;
            public float? PositionH2
            {
                get => _positionH2;
                set
                {
                    if (_positionH2 != value)
                    {
                        _positionH2 = value;
                        OnPropertyChanged(nameof(PositionH2));
                        OnPropertyChanged(nameof(H2Brush));       // <—
                    }
                }
            }

            public bool? OriginalH3 { get; private set; }
            private bool? _h3;
            public bool? H3
            {
                get => _h3;
                set
                {
                    if (_h3 != value)
                    {
                        _h3 = value;
                        OnPropertyChanged(nameof(H3));
                        OnPropertyChanged(nameof(H3HasChanged));
                        OnPropertyChanged(nameof(H3Brush));
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool H3HasChanged => _isInitialized && _h3 != OriginalH3;
            private float? _positionH3;
            public float? PositionH3
            {
                get => _positionH3;
                set
                {
                    if (_positionH3 != value)
                    {
                        _positionH3 = value;
                        OnPropertyChanged(nameof(PositionH3));
                        OnPropertyChanged(nameof(H3Brush));       // <—
                    }
                }
            }

            public bool? OriginalH4 { get; private set; }
            private bool? _h4;
            public bool? H4
            {
                get => _h4;
                set
                {
                    if (_h4 != value)
                    {
                        _h4 = value;
                        OnPropertyChanged(nameof(H4));
                        OnPropertyChanged(nameof(H4HasChanged));
                        OnPropertyChanged(nameof(H4Brush));       // <—
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool H4HasChanged => _isInitialized && _h4 != OriginalH4;
            private float? _positionH4;
            public float? PositionH4
            {
                get => _positionH4;
                set
                {
                    if (_positionH4 != value)
                    {
                        _positionH4 = value;
                        OnPropertyChanged(nameof(PositionH4));
                        OnPropertyChanged(nameof(H4Brush));       // <—
                    }
                }
            }


            public bool? OriginalH6 { get; private set; }
            private bool? _h6;
            public bool? H6
            {
                get => _h6;
                set
                {
                    if (_h6 != value)
                    {
                        _h6 = value;
                        OnPropertyChanged(nameof(H6));
                        OnPropertyChanged(nameof(H6HasChanged));
                        OnPropertyChanged(nameof(H6Brush));       // <—
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool H6HasChanged => _isInitialized && _h6 != OriginalH6;
            private float? _positionH6;
            public float? PositionH6
            {
                get => _positionH6;
                set
                {
                    if (_positionH6 != value)
                    {
                        _positionH6 = value;
                        OnPropertyChanged(nameof(PositionH6));
                        OnPropertyChanged(nameof(H6Brush));       // <—
                    }
                }
            }

            public bool? OriginalH12 { get; private set; }
            private bool? _h12;
            public bool? H12
            {
                get => _h12;
                set
                {
                    if (_h12 != value)
                    {
                        _h12 = value;
                        OnPropertyChanged(nameof(H12));
                        OnPropertyChanged(nameof(H12HasChanged));
                        OnPropertyChanged(nameof(H12Brush));       // <—
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool H12HasChanged => _isInitialized && _h12 != OriginalH12;
            private float? _positionH12;
            public float? PositionH12
                {
                get => _positionH12;
                set
                {
                    if (_positionH12 != value)
                    {
                        _positionH12 = value;
                        OnPropertyChanged(nameof(PositionH12));
                        OnPropertyChanged(nameof(H12Brush));       // <—
                    }
                }
            }


            public bool? OriginalH16 { get; private set; }
            private bool? _h16;
            public bool? H16
            {
                get => _h16;
                set
                {
                    if (_h16 != value)
                    {
                        _h16 = value;
                        OnPropertyChanged(nameof(H16));
                        OnPropertyChanged(nameof(H16HasChanged));
                        OnPropertyChanged(nameof(H16Brush));       // <—
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool H16HasChanged => _isInitialized && _h16 != OriginalH16;
            private float? _positionH16;
            public float? PositionH16
            {
                get => _positionH16;
                set
                {
                    if (_positionH16 != value)
                    {
                        _positionH16 = value;
                        OnPropertyChanged(nameof(PositionH16));
                        OnPropertyChanged(nameof(H16Brush));       // <—
                    }
                }
            }

            public bool? OriginalD1 { get; private set; }    
            private bool? _D1;
            public bool? D1
            {
                get => _D1;
                set
                {
                    if (_D1 != value)
                    {
                        _D1 = value;
                        OnPropertyChanged(nameof(D1));
                        OnPropertyChanged(nameof(D1HasChanged));
                        OnPropertyChanged(nameof(D1Brush));       // <—
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool D1HasChanged => _isInitialized && _D1 != OriginalD1;
            private float? _positionD1;
            public float? PositionD1
            {
                get => _positionD1;
                set
                {
                    if (_positionD1 != value)
                    {
                        _positionD1 = value;
                        OnPropertyChanged(nameof(PositionD1));
                        OnPropertyChanged(nameof(D1Brush));       // <—
                    }
                }
            }


            public bool? Originalh36 { get; private set; }
            private bool? _h36;
            public bool? H36
            {                 get => _h36;
                set
                {
                    if (_h36 != value)
                    {
                        _h36 = value;
                        OnPropertyChanged(nameof(H36));
                        OnPropertyChanged(nameof(H36HasChanged));
                        OnPropertyChanged(nameof(H36Brush));       // <—
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool H36HasChanged => _isInitialized && _h36 != Originalh36;
            private float? _positionH36;
            public float? PositionH36
            {
                get => _positionH36;
                set
                {
                    if (_positionH36 != value)
                    {
                        _positionH36 = value;
                        OnPropertyChanged(nameof(PositionH36));
                        OnPropertyChanged(nameof(H36Brush));       // <—
                    }
                }
            }

            public bool? OriginalD2 { get; private set; }
            private bool? _D2;
            public bool? D2
            {
                get => _D2;
                set
                {
                    if (_D2 != value)
                    {
                        _D2 = value;
                        OnPropertyChanged(nameof(D2));
                        OnPropertyChanged(nameof(D2HasChanged));
                        OnPropertyChanged(nameof(D2Brush));       // <—
                        RecalcNewTrades();
                    }
                }
            }
            public bool D2HasChanged => _isInitialized && _D2 != OriginalD2;
            private float? _positionD2;
            public float? PositionD2
            {
                get => _positionD2;
                set
                {
                    if (_positionD2 != value)
                    {
                        _positionD2 = value;
                        OnPropertyChanged(nameof(PositionD2));
                        OnPropertyChanged(nameof(D2Brush));       // <—
                    }
                }
            }

            public bool? OriginalD3 { get; private set; }
            private bool? _D3;
            public bool? D3
            {
                get => _D3;
                set
                {
                    if (_D3 != value)
                    {
                        _D3 = value;
                        OnPropertyChanged(nameof(D3));
                        OnPropertyChanged(nameof(D3HasChanged));
                        OnPropertyChanged(nameof(D3Brush));       // <—
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool D3HasChanged => _isInitialized && _D3 != OriginalD3;
            private float? _positionD3;
            public float? PositionD3
            {
                get => _positionD3;
                set
                {
                    if (_positionD3 != value)
                    {
                        _positionD3 = value;
                        OnPropertyChanged(nameof(PositionD3));
                        OnPropertyChanged(nameof(D3Brush));       // <—
                    }
                }
            }


            public bool? OriginalD4 { get; private set; }
            private bool? _D4;
            public bool? D4
            {
                get => _D4;
                set
                {
                    if (_D4 != value)
                    {
                        _D4 = value;
                        OnPropertyChanged(nameof(D4));
                        OnPropertyChanged(nameof(D4HasChanged));
                        OnPropertyChanged(nameof(D4Brush));       // <—
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }   
            public bool D4HasChanged => _isInitialized && _D4 != OriginalD4;
            private float? _positionD4;
            public float? PositionD4
            {
                get => _positionD4;
                set
                {
                    if (_positionD4 != value)
                    {
                        _positionD4 = value;
                        OnPropertyChanged(nameof(PositionD4));
                        OnPropertyChanged(nameof(D4Brush));       // <—
                    }
                }
            }

            public bool? OriginalW1 { get; private set; }
            private bool? _W1;
            public bool? W1
            {
                get => _W1;
                set
                {
                    if (_W1 != value)
                    {
                        _W1 = value;
                        OnPropertyChanged(nameof(W1));
                        OnPropertyChanged(nameof(W1HasChanged));
                        OnPropertyChanged(nameof(W1Brush));       // <—
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool W1HasChanged => _isInitialized && _W1 != OriginalW1;
            private float? _positionW1; 
            public float? PositionW1
            {
                get => _positionW1;
                set
                {
                    if (_positionW1 != value)
                    {
                        _positionW1 = value;
                        OnPropertyChanged(nameof(PositionW1));
                        OnPropertyChanged(nameof(W1Brush));       // <—
                    }
                }
            }


            public bool? OriginalD8 { get; private set; }
            private bool? _D8;
            public bool? D8
            {
                get => _D8;
                set
                {
                    if (_D8 != value)
                    {
                        _D8 = value;
                        OnPropertyChanged(nameof(D8));
                        OnPropertyChanged(nameof(D8HasChanged));
                        OnPropertyChanged(nameof(D8Brush));       // <—
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool D8HasChanged => _isInitialized && _D8 != OriginalD8;
            private float? _positionD8;
            public float? PositionD8
            {
                get => _positionD8;
                set
                {
                    if (_positionD8 != value)
                    {
                        _positionD8 = value;
                        OnPropertyChanged(nameof(PositionD8));
                        OnPropertyChanged(nameof(D8Brush));       // <—
                    }
                }
            }

            public bool? OriginalW2 { get; private set; }
            private bool? _W2;
            public bool? W2
            {
                get => _W2;
                set
                {
                    if (_W2 != value)
                    {
                        _W2 = value;
                        OnPropertyChanged(nameof(W2));
                        OnPropertyChanged(nameof(W2HasChanged));
                        OnPropertyChanged(nameof(W2Brush));       // <—
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool W2HasChanged => _isInitialized && _W2 != OriginalW2;
            private float? _positionW2;
            public float? PositionW2
            {
                get => _positionW2;
                set
                {
                    if (_positionW2 != value)
                    {
                        _positionW2 = value;
                        OnPropertyChanged(nameof(PositionW2));
                        OnPropertyChanged(nameof(W2Brush));       // <—
                    }
                }
            }

            // from FilterIntervalsDataModel (rename types/props to yours)
            public DateTime Runtime { get; set; }
            
            
            //// from TadPositionsDataModel
            public float? NumTrades { get; set; }
            public float? NumPositionIntervals { get; set; }
            public float? NumFiltIntervals { get; set; } 
            public float? NumFilteredTrades { get; set; }
            public float? FilteredDeployment { get; set; }
            public float? ViewDeployment { get; set; }
            public float? PositionDeployment { get; set; }


            private float? _newTrades;
            public float? NewTrades
            {
                get => _newTrades;
                private set
                {
                    if (_newTrades != value)
                    {
                        _newTrades = value;
                        OnPropertyChanged(nameof(NewTrades));
                        OnPropertyChanged(nameof(NewTradesBrush));
                    }
                }
            }

            private int? _rescaledIntervals;
            public int? RescaledIntervals
            {
                get => _rescaledIntervals;
                private set
                {
                    if (_rescaledIntervals != value)
                    {
                        _rescaledIntervals = value;
                        OnPropertyChanged(nameof(RescaledIntervals));
                        OnPropertyChanged(nameof(RescaledIntervalsBrush));
                    }
                }
            }

            private double? _newDeployment;
            public double? NewDeployment
            {
                get => _newDeployment;
                private set
                {
                    // remember previous (rounded) value for delta
                    var oldRounded = _newDeployment.HasValue ? Math.Round(_newDeployment.Value, 1) : (double?)null;

                    if (_newDeployment != value)
                    {
                        _prevNewDeploymentForBrush = oldRounded;
                        _newDeployment = value;

                        // after the first assignment, stop treating zeros as "initial grey"
                        _firstNewDeployment = false;

                        OnPropertyChanged(nameof(NewDeployment));
                        ReCalcScaledDeployment();
                        OnPropertyChanged(nameof(NewDeploymentBrush));
                    }
                }
            }

            private double? _prevNewDeploymentForBrush;   // last rounded value for delta coloring
            private bool _firstNewDeployment = true;      // only for "initial zero shows grey"

            //setting to detect changes for tickerprocessing further downstream
            // convenience aliases (optional)
            public bool HasIntervalEdits => HasAnyEdits; // your existing property

            public bool HasStrategyEdit => _isInitialized && StrategyNameHasChanged;

            // If you ever want to branch on manual work:
            public bool HasManualChange => _isInitialized && ManualHasChanged;

            // If you ever need to branch on pure scaling UI (usually doesn't require portfolio processing):
            public bool HasScaleFactorEdit => _isInitialized && NewScaleFactorHasChanged;

            private bool _isDeleted;
            public bool IsDeleted
            {
                get => _isDeleted;
                set
                {
                    if (_isDeleted != value)
                    {
                        _isDeleted = value;
                        OnPropertyChanged(nameof(IsDeleted));
                    }
                }
            }

            //for setting colours
            // Reuse ONE set of static, frozen brushes
            private static readonly Brush BgGrey = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            private static readonly Brush BgPaleGreen = Brushes.PaleGreen;
            private static readonly Brush BgMistyRose = Brushes.MistyRose;
            private static readonly Brush BgIndianRed = Brushes.IndianRed;
            private static readonly Brush BgMediumSeaGreen = Brushes.MediumSeaGreen;
            private static readonly Brush BgTransparent = Brushes.Transparent;

            public Brush BaseY1Brush => ComputeFlagPosBrush(BaseY1, PositionBaseY1);
            public Brush BaseH1Brush => ComputeFlagPosBrush(BaseH1, PositionBaseH1);
            public Brush BaseD1Brush => ComputeFlagPosBrush(BaseD1, PositionBaseD1);
            public Brush Y1Brush => ComputeFlagPosBrush(y1, PositionY1);
            public Brush Y2Brush => ComputeFlagPosBrush(y2, PositionY2);
            public Brush Y3Brush => ComputeFlagPosBrush(y3, PositionY3);
            public Brush H2Brush => ComputeFlagPosBrush(H2, PositionH2);
            public Brush H3Brush => ComputeFlagPosBrush(H3, PositionH3);
            public Brush H4Brush => ComputeFlagPosBrush(H4, PositionH4);
            //public Brush H5Brush => ComputeFlagPosBrush(H5, PositionH5);
            public Brush H6Brush => ComputeFlagPosBrush(H6, PositionH6);
            public Brush H12Brush => ComputeFlagPosBrush(H12, PositionH12);
            public Brush H16Brush => ComputeFlagPosBrush(H16, PositionH16);
            public Brush D1Brush => ComputeFlagPosBrush(D1, PositionD1);
            public Brush H36Brush => ComputeFlagPosBrush(H36, PositionH36);
            public Brush D2Brush => ComputeFlagPosBrush(D2, PositionD2);
            public Brush D3Brush => ComputeFlagPosBrush(D3, PositionD3);
            public Brush D4Brush => ComputeFlagPosBrush(D4, PositionD4);
            public Brush W1Brush => ComputeFlagPosBrush(W1, PositionW1);
            public Brush D8Brush => ComputeFlagPosBrush(D8, PositionD8);
            public Brush W2Brush => ComputeFlagPosBrush(W2, PositionW2);


            // same logic your XAML triggers implement
            private static Brush ComputeFlagPosBrush(bool? flag, float? pos)
            {
                if (!pos.HasValue) return BgGrey;
                if (flag == true && pos == 1) return BgPaleGreen;
                if (flag == true && pos == -1) return BgMistyRose;
                if (flag == false && pos == -1) return BgIndianRed;
                if (flag == false && pos == 1) return BgMediumSeaGreen;
                return BgTransparent;
            }


            public Brush NewTradesBrush
            {
                get
                {
                    if (NewTrades == null) return BgGrey;
                    if (NumFilteredTrades == null) return BgTransparent;
                    if (Math.Abs(NewTrades.Value) < 1e-9) return BgGrey;
                    if (NewTrades < NumFilteredTrades) return BgMistyRose;
                    if (NewTrades > NumFilteredTrades) return BgPaleGreen;
                    return BgTransparent;
                }
            }

            public Brush RescaledIntervalsBrush
            {
                get
                {
                    if (RescaledIntervals == null) return BgGrey;
                    if (NumFiltIntervals == null) return BgTransparent;
                    if (RescaledIntervals < NumFiltIntervals) return BgMistyRose;
                    if (RescaledIntervals > NumFiltIntervals) return BgPaleGreen;
                    return BgTransparent;
                }
            }

            public Brush NewDeploymentBrush
            {
                get
                {
                    if (NewDeployment == null) return BgGrey;

                    var a = Math.Round(NewDeployment.Value, 1);
                    var b = Math.Round(FilteredDeployment ?? 0f, 1);

                    // Primary: compare to baseline (FilteredDeployment)
                    if (a < b) return BgMistyRose;   // worse
                    if (a > b) return BgPaleGreen;   // better

                    if((FilteredDeployment == NewDeployment)&&(FilteredDeployment == 0))
                        {
                        return BgGrey;
                    }
                    return BgTransparent;
                }
            }


            public void AcceptManualAsOriginal()
            {
                // private setter, so do it here
                OriginalManual = _manual;
                OnPropertyChanged(nameof(ManualHasChanged));
            }




            // Keep the full override row for this ticker (may be null if none exists yet)
            public StrategiesOverrideDataModel StrategyOverride { get; private set; }

            public void AttachStrategyOverride(StrategiesOverrideDataModel so)
            {
                StrategyOverride = so;

                // Use the override's strategy as the "original" baseline (fall back to "default")
                var baseline = so?.StrategyName ?? "default";
                OriginalStrategyName = baseline;
                StrategyName = baseline;         // this is what your UI binds to
                OnPropertyChanged(nameof(OriginalStrategyName));
                OnPropertyChanged(nameof(StrategyName));
                OnPropertyChanged(nameof(StrategyNameHasChanged));
            }

            // Build the payload row for /strategies_override insert when StrategyName changed
            public object ToStrategyOverrideInsertModel(DateTime runtimeUtc, string envDefault)
            {
                // choose env from current override else fall back to a VM-wide default
                var env = StrategyOverride?.Env ?? envDefault;

                // business rule: if strategyname != "default" then min_* = "y1", else "default"
                bool isNonDefault = !string.Equals(StrategyName, "default", StringComparison.OrdinalIgnoreCase);
                string minVal = isNonDefault ? "y1" : "default";

                return new
                {
                    tickername = this.Tickername,
                    env = env,
                    runtime = runtimeUtc,                 // UTC, seconds-rounded
                    strategyname = this.StrategyName,

                    // carry other fields from existing override row if present (so we don’t null them out),
                    // or pick sane defaults if we are creating a first row
                    enable = StrategyOverride?.Enable ?? true,
                    sort_key = StrategyOverride?.SortKey ?? 0,
                    watchlist = StrategyOverride?.Watchlist,
                    keep_updated = StrategyOverride?.KeepUpdated ?? true,
                    calc_trades = StrategyOverride?.CalcTrades ?? true,
                    take_position = StrategyOverride?.TakePosition ?? true,
                    strategyname_base = StrategyOverride?.StrategyNameBase ?? "default",
                    strategyname_base_daily = StrategyOverride?.StrategyNameBaseDaily ?? "default",

                    // your frequency rule
                    min_update_freq = minVal,
                    min_chart_freq = minVal,
                    min_pos_freq = minVal,
                };
            }


            public void RestoreOriginalsFrom(MergedTickerRow src)
            {
                OriginalRescale = src.OriginalRescale;
                OriginalLongOnly = src.OriginalLongOnly;
                OriginalShortOnly = src.OriginalShortOnly;
                OriginalBuyOnly = src.OriginalBuyOnly;
                OriginalSellOnly = src.OriginalSellOnly;
                OriginalAllIntervals = src.OriginalAllIntervals;
                OriginalNewRescaleFactor = src.OriginalNewRescaleFactor;

                OriginalBaseY1 = src.OriginalBaseY1;
                OriginalBaseH1 = src.OriginalBaseH1;
                OriginalBaseD1 = src.OriginalBaseD1;

                OriginalY1 = src.OriginalY1;
                OriginalY2 = src.OriginalY2;
                OriginalY3 = src.OriginalY3;

                OriginalH2 = src.OriginalH2;
                OriginalH3 = src.OriginalH3;
                OriginalH4 = src.OriginalH4;
                //OriginalH5 = src.OriginalH5;
                OriginalH6 = src.OriginalH6;
                OriginalH12 = src.OriginalH12;
                OriginalH16 = src.OriginalH16;

                OriginalD1 = src.OriginalD1;
                Originalh36 = src.Originalh36; // note the lowercase h in your model
                OriginalD2 = src.OriginalD2;
                OriginalD3 = src.OriginalD3;
                OriginalD4 = src.OriginalD4;
                OriginalW1 = src.OriginalW1;
                OriginalD8 = src.OriginalD8;
                OriginalW2 = src.OriginalW2;
                OriginalManual = src.OriginalManual;
                OriginalStrategyName = src.OriginalStrategyName;
                _isInitialized = true;
                _isInitialized = true;

                // refresh HasChanged bindings
                OnPropertyChanged(nameof(RescaleHasChanged));
                OnPropertyChanged(nameof(LongOnlyHasChanged));
                OnPropertyChanged(nameof(ShortOnlyHasChanged));
                OnPropertyChanged(nameof(BuyOnlyHasChanged));
                OnPropertyChanged(nameof(SellOnlyHasChanged));
                OnPropertyChanged(nameof(AllIntervalsHasChanged));
                OnPropertyChanged(nameof(BaseY1HasChanged));
                OnPropertyChanged(nameof(BaseH1HasChanged));
                OnPropertyChanged(nameof(BaseD1HasChanged));
                OnPropertyChanged(nameof(Y1HasChanged));
                OnPropertyChanged(nameof(Y2HasChanged));
                OnPropertyChanged(nameof(Y3HasChanged));
                OnPropertyChanged(nameof(H2HasChanged));
                OnPropertyChanged(nameof(H3HasChanged));
                OnPropertyChanged(nameof(H4HasChanged));
                //OnPropertyChanged(nameof(H5HasChanged));
                OnPropertyChanged(nameof(H6HasChanged));
                OnPropertyChanged(nameof(H12HasChanged));
                OnPropertyChanged(nameof(H16HasChanged));
                OnPropertyChanged(nameof(D1HasChanged));
                OnPropertyChanged(nameof(H36HasChanged));
                OnPropertyChanged(nameof(D2HasChanged));
                OnPropertyChanged(nameof(D3HasChanged));
                OnPropertyChanged(nameof(D4HasChanged));
                OnPropertyChanged(nameof(W1HasChanged));
                OnPropertyChanged(nameof(D8HasChanged));
                OnPropertyChanged(nameof(W2HasChanged));
                OnPropertyChanged(nameof(ManualHasChanged));
                OnPropertyChanged(nameof(StrategyNameHasChanged));
            }


            // Count how many intervals are ACTIVE in the *current* row based on flags only
            // (flag == false means "included/active"). Ignores TadPositions entirely.
            private int CountFiOnlyCurrent()
            {
                int c = 0;
                if (BaseY1 == true) c++;
                if (BaseH1 == true) c++;
                if (BaseD1 == true) c++;

                if (y1 == true) c++;
                if (y2 == true) c++;
                if (y3 == true) c++;

                if (H2 == true) c++;
                if (H3 == true) c++;
                if (H4 == true) c++;
                if (H6 == true) c++;
                if (H12 == true) c++;
                if (H16 == true) c++;

                if (D1 == true) c++;
                if (H36 == true) c++;
                if (D2 == true) c++;
                if (D3 == true) c++;
                if (D4 == true) c++;
                if (W1 == true) c++;
                if (D8 == true) c++;
                if (W2 == true) c++;
                return c;
            }

            // Count how many intervals were ACTIVE in the *baseline* (original) flags only
            // (again, flag == false). Ignores TadPositions entirely.
            private int CountFiOnlyBaseline()
            {
                int c = 0;
                if (OriginalBaseY1 == false) c++;
                if (OriginalBaseH1 == false) c++;
                if (OriginalBaseD1 == false) c++;

                if (OriginalY1 == false) c++;
                if (OriginalY2 == false) c++;
                if (OriginalY3 == false) c++;

                if (OriginalH2 == false) c++;
                if (OriginalH3 == false) c++;
                if (OriginalH4 == false) c++;
                if (OriginalH6 == false) c++;
                if (OriginalH12 == false) c++;
                if (OriginalH16 == false) c++;

                if (OriginalD1 == false) c++;
                if (Originalh36 == false) c++; // note: your model uses lowercase h in Originalh36
                if (OriginalD2 == false) c++;
                if (OriginalD3 == false) c++;
                if (OriginalD4 == false) c++;
                if (OriginalW1 == false) c++;
                if (OriginalD8 == false) c++;
                if (OriginalW2 == false) c++;
                return c;
            }

            static void CountIfFiltered(bool? flag, float? pos, ref int acc, bool excludeZeroPositions)
            {
                if (flag == true && pos.HasValue &&
                    (!excludeZeroPositions || Math.Abs(pos.Value) > 1e-9))
                {
                    acc++;
                }
            }

            int CountCurrentFiltered(bool excludeZeroPositions)
            {
                int c = 0;
                CountIfFiltered(BaseY1, PositionBaseY1, ref c, excludeZeroPositions);
                CountIfFiltered(BaseH1, PositionBaseH1, ref c, excludeZeroPositions);
                CountIfFiltered(BaseD1, PositionBaseD1, ref c, excludeZeroPositions);
                CountIfFiltered(y1, PositionY1, ref c, excludeZeroPositions);
                CountIfFiltered(y2, PositionY2, ref c, excludeZeroPositions);
                CountIfFiltered(y3, PositionY3, ref c, excludeZeroPositions);
                CountIfFiltered(H2, PositionH2, ref c, excludeZeroPositions);
                CountIfFiltered(H3, PositionH3, ref c, excludeZeroPositions);
                CountIfFiltered(H4, PositionH4, ref c, excludeZeroPositions);
                // H5 intentionally not used
                CountIfFiltered(H6, PositionH6, ref c, excludeZeroPositions);
                CountIfFiltered(H12, PositionH12, ref c, excludeZeroPositions);
                CountIfFiltered(H16, PositionH16, ref c, excludeZeroPositions);
                CountIfFiltered(H36, PositionH36, ref c, excludeZeroPositions);
                CountIfFiltered(D1, PositionD1, ref c, excludeZeroPositions);
                CountIfFiltered(D2, PositionD2, ref c, excludeZeroPositions);
                CountIfFiltered(D3, PositionD3, ref c, excludeZeroPositions);
                CountIfFiltered(D4, PositionD4, ref c, excludeZeroPositions);
                CountIfFiltered(D8, PositionD8, ref c, excludeZeroPositions);
                CountIfFiltered(W1, PositionW1, ref c, excludeZeroPositions);
                CountIfFiltered(W2, PositionW2, ref c, excludeZeroPositions);
                return c;
            }

            int CountBaselineFiltered(bool excludeZeroPositions)
            {
                int c = 0;
                CountIfFiltered(OriginalBaseY1, PositionBaseY1, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalBaseH1, PositionBaseH1, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalBaseD1, PositionBaseD1, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalY1, PositionY1, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalY2, PositionY2, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalY3, PositionY3, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalH2, PositionH2, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalH3, PositionH3, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalH4, PositionH4, ref c, excludeZeroPositions);
                // H5 intentionally not used
                CountIfFiltered(OriginalH6, PositionH6, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalH12, PositionH12, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalH16, PositionH16, ref c, excludeZeroPositions);
                CountIfFiltered(Originalh36, PositionH36, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalD1, PositionD1, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalD2, PositionD2, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalD3, PositionD3, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalD4, PositionD4, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalD8, PositionD8, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalW1, PositionW1, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalW2, PositionW2, ref c, excludeZeroPositions);
                return c;
            }



            public void RecalcRescaledIntervals(bool excludeZeroPositions = false)
            {
                // AH2: cap (# of position intervals)
                int ah2 = NumPositionIntervals.HasValue ? (int)Math.Round(NumPositionIntervals.Value) : 0;
                if (ah2 <= 0) { RescaledIntervals = null; return; }

                int x;
                if (Rescale == true)
                {
                    // Count how many intervals are currently FILTERED (flag == true)
                    // Active (unfiltered) = total available - filtered
                    int curFiltered = CountCurrentFiltered(excludeZeroPositions);
                    int activeNow = ah2 - curFiltered;

                    // Clamp to [1, AH2]
                    x = Math.Min(ah2, Math.Max(1, activeNow));
                }
                else
                {
                    // No rescale → use the full positions cap
                    x = ah2;
                }

                RescaledIntervals = x;
                RecalcNewDeployment();
                OnPropertyChanged(nameof(NewDeploymentBrush));

            }

            public bool HasAnyEdits =>
                RescaleHasChanged
                || LongOnlyHasChanged || ShortOnlyHasChanged || BuyOnlyHasChanged || SellOnlyHasChanged || AllIntervalsHasChanged
                || BaseY1HasChanged || BaseH1HasChanged || BaseD1HasChanged
                || Y1HasChanged || Y2HasChanged || Y3HasChanged
                || H2HasChanged || H3HasChanged || H4HasChanged /* no H5 on purpose */
                || H6HasChanged || H12HasChanged || H16HasChanged || H36HasChanged
                || D1HasChanged || D2HasChanged || D3HasChanged || D4HasChanged
                || D8HasChanged || W1HasChanged || W2HasChanged;


            // Helper: include pos when flag == false
            static void AddIfNotFiltered(bool? flag, float? pos, bool? buyOnly,bool? shortOnly, ref double acc)
            {
                // flag == false => NOT filtered => include
                if (flag == false && pos.HasValue)
                {
                    if (pos.Value < 0 && buyOnly == true) return; // skip if BuyOnly and position is negative
                    else if (pos.Value > 0 && shortOnly == true) return; // skip if ShortOnly and position is positive
                    else
                    {
                        acc += pos.Value;
                    }
                }
                // flag true or null => skip
            }
            
            public void RecalcNewTrades()
            {
                if (AllIntervals != false) { 
                    NewTrades = 0; 
                    RecalcNewDeployment();
                    OnPropertyChanged(nameof(NewDeploymentBrush));
                    return; 
                
                }

                double sum = 0;
                // include here whichever intervals you want in the total
                AddIfNotFiltered(BaseY1, PositionBaseY1,BuyOnly,SellOnly, ref sum); // if you expose PositionBaseH1 as H1's "H1" bucket (optional)
                AddIfNotFiltered(BaseH1, PositionBaseH1, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(BaseD1, PositionBaseD1, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(y1, PositionY1, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(y2, PositionY2, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(y3, PositionY3, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(H2, PositionH2, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(H3, PositionH3, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(H4, PositionH4, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(H6, PositionH6, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(H12, PositionH12, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(H16, PositionH16, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(D1, PositionD1, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(H36, PositionH36, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(D2, PositionD2, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(D3, PositionD3, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(D4, PositionD4, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(W1, PositionW1, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(D8, PositionD8, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(W2, PositionW2, BuyOnly, SellOnly, ref sum);

                NewTrades = (float)sum;

                if((NewTrades > 0) && (ShortOnly==true))
                {
                    NewTrades = 0;
                }
                else if ((NewTrades < 0) && (LongOnly == true))
                {
                    NewTrades = 0;
                }
                RecalcNewDeployment();
                OnPropertyChanged(nameof(NewDeploymentBrush));

            }

            public void RecalcNewDeployment()
            {
                float? nd = null;

                if (NewTrades.HasValue && RescaledIntervals.HasValue && RescaledIntervals.Value != 0f)
                {
                    float r = NewTrades.Value / RescaledIntervals.Value;

                    if (LongOnly == true) r = Math.Max(r, 0f);   // float overload
                    else if (ShortOnly == true) r = Math.Min(r, 0f);

                    if (!float.IsNaN(r) && !float.IsInfinity(r))
                        nd = r;
                }

                if (NewDeployment != nd)
                    NewDeployment = nd; // make sure setter raises OnPropertyChanged
                OnPropertyChanged(nameof(NewDeploymentBrush));

            }

            public void ReCalcScaledDeployment()
            {
                float? sd = null;
                float sf = 1;
                if (NewScaleFactor.HasValue)
                {
                    sf = (float)(NewScaleFactor.Value);
                }
                else
                {
                    if(ScaleFactor.HasValue)
                        sf = (float)(ScaleFactor.Value);
                    else
                        sf = 1;
                }
                if (NewDeployment.HasValue)
                {
                    float r = (float)(NewDeployment.Value) * sf;

                    if (!float.IsNaN(r) && !float.IsInfinity(r))   // works on all TFMs
                        sd = r;
                }

                if (ScaledDeployment != sd)
                    ScaledDeployment = sd;  // assume setter raises PropertyChanged
            }

            public void SnapshotOriginals()
            {
                OriginalRescale = _rescale;
                OriginalNewRescaleFactor = _newScaleFactor;
                OriginalLongOnly = _longOnly;
                OriginalShortOnly = _shortOnly;
                OriginalBuyOnly = _buyOnly;
                OriginalSellOnly = _sellOnly;
                OriginalAllIntervals = _allIntervals;
                OriginalBaseY1 = _baseY1;
                OriginalD1 = _D1;
                OriginalBaseH1 = _baseH1;
                OriginalBaseD1 = _baseD1;
                OriginalY1 = _y1;
                OriginalY2 = _y2;
                OriginalY3 = _y3;
                OriginalH2 = _h2;
                OriginalH3 = _h3;
                OriginalH4 = _h4;
                //OriginalH5 = _h5;
                OriginalH6 = _h6;
                OriginalH12 = _h12;
                OriginalH16 = _h16;
                Originalh36 = _h36;
                OriginalD2 = _D2;
                OriginalD3 = _D3;
                OriginalD4 = _D4;
                OriginalW1 = _W1;
                OriginalW2 = _W2;
                OriginalD8 = _D8;

                OriginalManual = _manual;
                OriginalStrategyName = _strategyName;
                _isInitialized = true;

                // (repeat for other tracked fields)

                _isInitialized = true;

                // Notify HasChanged props so the UI refreshes to "not bold on load"
                OnPropertyChanged(nameof(RescaleHasChanged));
                OnPropertyChanged(nameof(NewScaleFactor));
                OnPropertyChanged(nameof(LongOnlyHasChanged));
                OnPropertyChanged(nameof(ShortOnlyHasChanged));
                OnPropertyChanged(nameof(BuyOnlyHasChanged));
                OnPropertyChanged(nameof(SellOnlyHasChanged));
                OnPropertyChanged(nameof(AllIntervalsHasChanged));
                OnPropertyChanged(nameof(BaseY1HasChanged));
                OnPropertyChanged(nameof(D1HasChanged));
                OnPropertyChanged(nameof(BaseH1HasChanged));
                OnPropertyChanged(nameof(BaseD1HasChanged));
                OnPropertyChanged(nameof(Y1HasChanged));
                OnPropertyChanged(nameof(Y2HasChanged));
                OnPropertyChanged(nameof(Y3HasChanged));
                OnPropertyChanged(nameof(H2HasChanged));
                OnPropertyChanged(nameof(H3HasChanged));
                OnPropertyChanged(nameof(H4HasChanged));
                //OnPropertyChanged(nameof(H5HasChanged));
                OnPropertyChanged(nameof(H6HasChanged));
                OnPropertyChanged(nameof(H12HasChanged));
                OnPropertyChanged(nameof(H16HasChanged));
                OnPropertyChanged(nameof(H36HasChanged));
                OnPropertyChanged(nameof(D2HasChanged));
                OnPropertyChanged(nameof(D3HasChanged));
                OnPropertyChanged(nameof(D4HasChanged));
                OnPropertyChanged(nameof(W1HasChanged));
                OnPropertyChanged(nameof(D8HasChanged));
                OnPropertyChanged(nameof(W2HasChanged));
                OnPropertyChanged(nameof(ManualHasChanged));
                OnPropertyChanged(nameof(StrategyNameHasChanged));

                // (repeat for others)
            }

            private static bool PickForSave(bool? current, bool? original, bool hasChanged, bool defaultValue = false)
            {
                var chosen = hasChanged ? current : original;   // if not changed, stick to original
                return chosen ?? defaultValue;                  // coalesce to a deterministic bool for Python
            }

            public ScaledPositionsInsertRow ToScaledPositionInsertRow()
            {
                return new ScaledPositionsInsertRow
                {
                    TickerName = this.Tickername,
                    FundGroupName = this.FundGroup,
                    FundName = this.FundName,
                    ScaledPercent = this.ScaledPercent ,
                    ScaledStepSize = Math.Abs((this.ScaleFactor ?? 1) - (this.NewScaleFactor ?? 1)), // default to 0.0 if null
                    ScaledTarget = this.NewScaleFactor,
                    ScaledTimeStep = 5,
                    ScaledType = "filtered"
                };
            }

            public FilterIntervalsUpsertRow ToUpsertRow()
            {
                return new FilterIntervalsUpsertRow
                {
                    Tickername = this.Tickername,
                    FundGroup = this.FundGroup,
                    FundName = this.FundName,

                    Rescale = PickForSave(this.Rescale, this.OriginalRescale, this.RescaleHasChanged, true),
                    LongOnly = PickForSave(this.LongOnly, this.OriginalLongOnly, this.LongOnlyHasChanged),
                    ShortOnly = PickForSave(this.ShortOnly, this.OriginalShortOnly, this.ShortOnlyHasChanged),
                    BuyOnly = PickForSave(this.BuyOnly, this.OriginalBuyOnly, this.BuyOnlyHasChanged),
                    SellOnly = PickForSave(this.SellOnly, this.OriginalSellOnly, this.SellOnlyHasChanged),
                    AllIntervals = PickForSave(this.AllIntervals, this.OriginalAllIntervals, this.AllIntervalsHasChanged),

                    BaseY1 = PickForSave(this.BaseY1, this.OriginalBaseY1, this.BaseY1HasChanged),
                    BaseH1 = PickForSave(this.BaseH1, this.OriginalBaseH1, this.BaseH1HasChanged),
                    BaseD1 = PickForSave(this.BaseD1, this.OriginalBaseD1, this.BaseD1HasChanged),

                    Y1 = PickForSave(this.y1, this.OriginalY1, this.Y1HasChanged),
                    Y2 = PickForSave(this.y2, this.OriginalY2, this.Y2HasChanged),
                    Y3 = PickForSave(this.y3, this.OriginalY3, this.Y3HasChanged),

                    H2 = PickForSave(this.H2, this.OriginalH2, this.H2HasChanged),
                    H3 = PickForSave(this.H3, this.OriginalH3, this.H3HasChanged),
                    H4 = PickForSave(this.H4, this.OriginalH4, this.H4HasChanged),
                    H6 = PickForSave(this.H6, this.OriginalH6, this.H6HasChanged),
                    H12 = PickForSave(this.H12, this.OriginalH12, this.H12HasChanged),
                    H16 = PickForSave(this.H16, this.OriginalH16, this.H16HasChanged),
                    H36 = PickForSave(this.H36, this.Originalh36, this.H36HasChanged),

                    D1 = PickForSave(this.D1, this.OriginalD1, this.D1HasChanged),
                    D2 = PickForSave(this.D2, this.OriginalD2, this.D2HasChanged),
                    D3 = PickForSave(this.D3, this.OriginalD3, this.D3HasChanged),
                    D4 = PickForSave(this.D4, this.OriginalD4, this.D4HasChanged),
                    D8 = PickForSave(this.D8, this.OriginalD8, this.D8HasChanged),

                    W1 = PickForSave(this.W1, this.OriginalW1, this.W1HasChanged),
                    W2 = PickForSave(this.W2, this.OriginalW2, this.W2HasChanged),
                };
            }


        }


        public sealed class TickerRow
        {
            [JsonProperty("index")]
            public int Index { get; set; }

            [JsonProperty("tickername")]
            public string TickerName { get; set; }

            // adjust to your table’s schema
            [JsonProperty("description")]
            public string Description { get; set; }

            [JsonProperty("fundgroupname")]
            public string FundGroupName { get; set; }

            [JsonProperty("fundname")]
            public string FundName { get; set; }
        }

        public async Task LoadTickerUniverseAsync()
        {
            try
            {
                IsExecuting = true;

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                    var request = new
                    {
                        table_name = "tickers",
                        where_dict = new Dictionary<string, object> { { "valid_tickername", 1 } }
                    };

                    var content = new StringContent(
                        JsonConvert.SerializeObject(request),
                        Encoding.UTF8, "application/json");

                    var resp = await client.PostAsync($"{baseUrl}/select_table", content);
                    resp.EnsureSuccessStatusCode();

                    var json = await resp.Content.ReadAsStringAsync();
                    var rows = JsonConvert.DeserializeObject<List<TickerRow>>(json) ?? new List<TickerRow>();

                    foreach (var r in rows)
                    {
                        r.TickerName = r.TickerName?.Trim();
                        r.Description = r.Description?.Trim();
                    }

                    TickerUniverse = rows
                        .OrderBy(r => r.TickerName, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    OnPropertyChanged(nameof(TickerUniverse));
                }
            }
            finally { IsExecuting = false; }
        }




        //public FilterIntervalsViewModel.MergedTickerRow CreateDefaultRow(string ticker,string fundgroupname,string fundname)
        //{
        //    var row = new FilterIntervalsViewModel.MergedTickerRow
        //    {
        //        Tickername = ticker,
        //        FundGroup = fundgroupname,
        //        FundName = fundname,    
        //        // safe defaults — tweak if you prefer different starting flags
        //        Rescale = DefaultRescale,
        //        LongOnly = DefaultLongOnly,
        //        ShortOnly = DefaultShortOnly,
        //        BuyOnly = DefaultBuyOnly,
        //        SellOnly = DefaultSellOnly,
        //        AllIntervals = DefaultAllIntervals,
        //        y1 = false,
        //        y2 = false,
        //        y3 = false,
        //        H2 = false,
        //        H3 = false,
        //        H4 = false,
        //        H6 = false,
        //        H12 = false,
        //        H16 = false,
        //        H36 = false,
        //        D1 = false,
        //        D2 = false,
        //        D3 = false,
        //        D4 = false,
        //        D8 = false,
        //        W1 = false,
        //        W2 = false,
        //        // make intervals editable-looking (NOT grey) for *new* tickers:
        //        PositionY1 = 0f,
        //        PositionY2 = 0f,
        //        PositionY3 = 0f,
        //        PositionH2 = 0f,
        //        PositionH3 = 0f,
        //        PositionH4 = 0f,
        //        // PositionH5 intentionally NOT set (you said H5 isn’t in the DB)
        //        PositionH6 = 0f,
        //        PositionH12 = 0f,
        //        PositionH16 = 0f,
        //        PositionH36 = 0f,
        //        PositionD1 = 0f,
        //        PositionD2 = 0f,
        //        PositionD3 = 0f,
        //        PositionD4 = 0f,
        //        PositionD8 = 0f,
        //        PositionW1 = 0f,
        //        PositionW2 = 0f,

        //        // positions/metrics start empty
        //        NumTrades = 0,
        //        NumPositionIntervals = 0,
        //        NumFiltIntervals = 0,
        //        NumFilteredTrades = 0,
        //        FilteredDeployment = 0,
        //        ViewDeployment = 0,
        //        PositionDeployment = 0
        //    };

        //    row.SnapshotOriginals();
        //    row.RecalcNewTrades();
        //    row.RecalcRescaledIntervals();
        //    row.RecalcNewDeployment();
        //    return row;
        //}
        public FilterIntervalsViewModel.MergedTickerRow CreateDefaultRow(string ticker, string fundgroupname, string fundname)
        {
            var row = new FilterIntervalsViewModel.MergedTickerRow
            {
                Tickername = ticker,
                FundGroup = fundgroupname,
                FundName = fundname,

                Rescale = DefaultRescale,
                LongOnly = DefaultLongOnly,
                ShortOnly = DefaultShortOnly,
                BuyOnly = DefaultBuyOnly,
                SellOnly = DefaultSellOnly,

                // IMPORTANT: baseline should remain false
                AllIntervals = false,

                y1 = false,
                y2 = false,
                y3 = false,
                H2 = false,
                H3 = false,
                H4 = false,
                H6 = false,
                H12 = false,
                H16 = false,
                H36 = false,
                D1 = false,
                D2 = false,
                D3 = false,
                D4 = false,
                D8 = false,
                W1 = false,
                W2 = false,

                PositionY1 = 0f,
                PositionY2 = 0f,
                PositionY3 = 0f,
                PositionH2 = 0f,
                PositionH3 = 0f,
                PositionH4 = 0f,
                PositionH6 = 0f,
                PositionH12 = 0f,
                PositionH16 = 0f,
                PositionH36 = 0f,
                PositionD1 = 0f,
                PositionD2 = 0f,
                PositionD3 = 0f,
                PositionD4 = 0f,
                PositionD8 = 0f,
                PositionW1 = 0f,
                PositionW2 = 0f,

                NumTrades = 0,
                NumPositionIntervals = 0,
                NumFiltIntervals = 0,
                NumFilteredTrades = 0,
                FilteredDeployment = 0,
                ViewDeployment = 0,
                PositionDeployment = 0
            };
            // attach existing override if we have it cached (keeps watchlist etc.)
            if (_overrideByTicker.TryGetValue(ticker, out var so))
                row.AttachStrategyOverride(so);
            else
                row.AttachStrategyOverride(null);

            // Snapshot the "original" state (AllIntervals=false)
            row.SnapshotOriginals();

            // NOW force the new ticker to look like an edit
            row.AllIntervals = true;

            row.RecalcNewTrades();
            row.RecalcRescaledIntervals();
            row.RecalcNewDeployment();
            return row;
        }

    }


}
