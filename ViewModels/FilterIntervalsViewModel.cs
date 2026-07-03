using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
    /// <summary>
    /// ObservableCollection that can be refilled in one shot, raising a single Reset
    /// notification instead of one Add per item — so a bound DataGrid regenerates and
    /// lays out once rather than per row.
    /// </summary>
    public class RangeObservableCollection<T> : ObservableCollection<T>
    {
        public RangeObservableCollection() { }
        public RangeObservableCollection(IEnumerable<T> items) : base(items) { }

        public void ReplaceAll(IEnumerable<T> items)
        {
            Items.Clear();
            foreach (var it in items)
                Items.Add(it);

            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

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

    /// <summary>
    /// DISPLAY-ONLY relabelling of interval column headers by their actual duration:
    /// intervals ≤ 90 min show the minute count (t1→"1" … y3→"90"); the hour-range intervals
    /// show hours (y4→"h2" … y72→"h36"); days/weeks and every non-interval header pass through
    /// unchanged. The column's real Header (the interval code) is untouched, so ApplyColumnOrder,
    /// the collapser groups, IntervalOrder and column-visibility all still key off the code.
    /// </summary>
    public sealed class IntervalHeaderLabelConverter : IValueConverter
    {
        private static readonly Dictionary<string, string> Map = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // ≤ 90 minutes → minute count
            { "t1", "1" }, { "t2", "2" }, { "t3", "3" }, { "t4", "4" }, { "t5", "5" }, { "t8", "8" },
            { "v2", "10" }, { "v3", "15" }, { "v4", "20" }, { "v6", "30" }, { "v8", "40" },
            { "y2", "60" }, { "y3", "90" },
            // hours (> 90 min, below days) → hN
            { "y4", "h2" }, { "y6", "h3" }, { "y8", "h4" }, { "y12", "h6" },
            { "y24", "h12" }, { "y32", "h16" }, { "y72", "h36" },
            // D*, W*, base (b_*) and all other headers pass through unchanged
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as string;
            if (s != null && Map.TryGetValue(s, out var label)) return label;
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value;
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


        public RangeObservableCollection<MergedTickerRow> MergedRows { get; } = new RangeObservableCollection<MergedTickerRow>();


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
        public ObservableCollection<string> StrategyNamesBase { get; } = new ObservableCollection<string>();

        // min_intvl options for the dropdown — "default" + all trading intervals from t1 upwards
        public ObservableCollection<string> MinIntvlOptions { get; } = new ObservableCollection<string>(new[]
        {
            "default",
            "t1", "t2", "t3", "t4", "t5", "t8",
            "v2", "v3",
            "n2", "n3", "n4",
            "y2", "y3",
            "h2", "h3", "h4", "h6", "h12", "h16",
            "D1", "h36", "D2", "D3", "D4", "W1", "D8", "W2"
        });

        // p_int options for the dropdown — "default" + all trading intervals
        public ObservableCollection<string> PosIntvlOptions { get; } = new ObservableCollection<string>(new[]
        {
            "default",
            "t1", "t2", "t3", "t4", "t5", "t8",
            "v2", "v3",
            "n2", "n3", "n4",
            "y2", "y3",
            "h2", "h3", "h4", "h6", "h12", "h16",
            "D1", "h36", "D2", "D3", "D4", "W1", "D8", "W2"
        });

        // ordered list of interval column headers for visibility calculation
        public static readonly string[] IntervalOrder = new[]
        {
            "t1", "t2", "t3", "t4", "t5", "t8",
            "v2", "v3", "v4", "v6", "v8",
            "y2", "y3", "y4", "y6", "y8", "y12", "y24", "y32",
            "D1", "y72", "D2", "D3", "D4", "W1", "D8", "W2"
        };

        /// <summary>
        /// Returns the minimum visible interval based on the lowest p_int across all rows.
        /// If all are "default", returns "n3".
        /// </summary>
        public string GetMinVisibleInterval()
        {
            string minInterval = null;
            int minIndex = int.MaxValue;

            foreach (var row in MergedRows)
            {
                var val = row.PosIntvl;
                if (string.IsNullOrEmpty(val) || string.Equals(val, "default", StringComparison.OrdinalIgnoreCase))
                    continue;

                int idx = Array.FindIndex(IntervalOrder, i => string.Equals(i, val, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0 && idx < minIndex)
                {
                    minIndex = idx;
                    minInterval = IntervalOrder[idx];
                }
            }

            // if no non-default p_int found, default minimum is n3
            if (minInterval == null)
            {
                int defIdx = Array.FindIndex(IntervalOrder, i => i == "v6");
                return IntervalOrder[defIdx];
            }
            return minInterval;
        }

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
                    // Reset load progress whenever we transition into a busy state so
                    // the determinate progress bar starts from 0 each time.
                    if (_isExecuting) LoadProgress = 0;
                    OnPropertyChanged(); // raises PropertyChanged(nameof(IsExecuting))
                    OnPropertyChanged(nameof(IsSaveEnabled));
                }
            }
        }

        // Determinate load progress bar value (0–100). Bumped from the window
        // code-behind as each load stage starts. Driven this way (discrete steps)
        // instead of an indeterminate animation so it works even at WPF render
        // tier 0/1 (software rendering, e.g. over RDP), where indeterminate
        // animations freeze whenever the UI thread is doing sync work.
        private int _loadProgress;
        public int LoadProgress
        {
            get => _loadProgress;
            set
            {
                if (_loadProgress != value)
                {
                    _loadProgress = value;
                    OnPropertyChanged();
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
                // Exclude base strategies — those belong to the strategy_b dropdown.
                var resp = await client.PostAsync($"{baseUrl}/get_strategynames",
                                                  new StringContent("{\"exclude_interval_type\":\"base\"}", Encoding.UTF8, "application/json"));
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

        public async Task LoadStrategyNamesBaseAsync()
        {
            StrategyNamesBase.Clear();

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                var resp = await client.PostAsync($"{baseUrl}/get_strategynames",
                                                  new StringContent("{\"interval_type\":\"base\"}", Encoding.UTF8, "application/json"));
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
                            StrategyNamesBase.Add(name.Trim());
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
                        ContinuousUpdate = o["continuous_update"]?.ToObject<bool?>(),
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
                PopulateFilterIntervalsFromJson(fullResponse);
            }
        }

        private void PopulateFilterIntervalsFromJson(JObject fullResponse)
        {
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

        /// <summary>
        /// Single-fetch combined load: POSTs once to /filtered_intervals and populates both
        /// FilterIntervalsData and TadPositionsData (and ScaledPositionsData) from the same
        /// response. Use this on initial window load to avoid hitting the endpoint twice.
        /// </summary>
        public async Task LoadFilterIntervalsAndPositionsAsync()
        {
            // Fetch + JSON parse on a background thread so the UI thread stays free
            // and the indeterminate progress bar keeps animating. Populators run on
            // UI thread (ObservableCollection updates require it).
            JObject root = await Task.Run(async () =>
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                    var content = new StringContent("{}", Encoding.UTF8, "application/json");
                    var response = await client.PostAsync($"{baseUrl}/filtered_intervals", content).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return JObject.Parse(json);
                }
            });

            PopulateFilterIntervalsFromJson(root);
            PopulateTadPositionsFromJson(root);
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

            PopulateTadPositionsFromJson(root);
        }

        private void PopulateTadPositionsFromJson(JObject root)
        {
            var positionsJson = (JObject)root["positions"];
            var viewPositionsJson = (JObject)root["viewpositions"];
            var filterPositionsJson = (JObject)root["filterpositions"];
            var positionLimitsJson = root["positionlimits"] as JObject;
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
                    model.PositionBaseD1 = row.Value<float?>("position_base_D1");
                    model.PositionBaseT1 = row.Value<float?>("position_base_t1");
                    model.PositionBaseV1 = row.Value<float?>("position_base_v1");
                    model.PositionBaseN1 = row.Value<float?>("position_base_n1");
                    model.PositionT1 = row.Value<float?>("position_t1");
                    model.PositionT2 = row.Value<float?>("position_t2");
                    model.PositionT3 = row.Value<float?>("position_t3");
                    model.PositionT4 = row.Value<float?>("position_t4");
                    model.PositionT5 = row.Value<float?>("position_t5");
                    model.PositionT8 = row.Value<float?>("position_t8");
                    model.PositionV2 = row.Value<float?>("position_v2");
                    model.PositionV3 = row.Value<float?>("position_v3");
                    model.PositionV4 = row.Value<float?>("position_v4");
                    model.PositionV6 = row.Value<float?>("position_v6");
                    model.PositionV8 = row.Value<float?>("position_v8");
                    model.PositionY4 = row.Value<float?>("position_y4");
                    model.PositionY6 = row.Value<float?>("position_y6");
                    model.PositionY8 = row.Value<float?>("position_y8");
                    model.PositionY12 = row.Value<float?>("position_y12");
                    model.PositionY24 = row.Value<float?>("position_y24");
                    model.PositionY32 = row.Value<float?>("position_y32");
                    model.PositionY72 = row.Value<float?>("position_y72");
                    model.PositionN2 = row.Value<float?>("position_n2");
                    model.PositionN3 = row.Value<float?>("position_n3");
                    model.PositionN4 = row.Value<float?>("position_n4");
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

            // 3b) position limits (from target_positions, merged on tickername+fundname)
            if (positionLimitsJson != null)
            {
                foreach (var kv in positionLimitsJson)
                {
                    var ticker = kv.Key;
                    var row = (JObject)kv.Value;
                    var model = GetOrCreate(ticker);

                    model.PositionLimit = row.Value<float?>("position_limit");
                    model.ScaledPositionLimit = row.Value<float?>("scaled_position_limit");
                    model.PositionTarget = row.Value<float?>("position_target");
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

            string Norm(string s) => (s ?? "").Trim().ToUpperInvariant();

            // Group scaled positions by ticker so we can wildcard-match on
            // (fundgroupname, fundname). A scaled row may carry "*" for fundname and/or
            // fundgroupname (a blanket rule), which must still apply to a concrete
            // fund/group filter row — mirrors Scale.load()'s fund IN ('*', fund) semantics.
            var scaledByTicker = (ScaledPositionsData ?? new ObservableCollection<ScaledPositionsDataModel>())
                .Where(s => s != null)
                .GroupBy(s => Norm(s.TickerName))
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            // Find the best scaled row for a (ticker, fundgroup, fundname) filter row.
            // "*" on the scaled side matches anything; we prefer the most specific match
            // (exact fund > exact group > exact ticker) so a fund-specific override wins
            // over a blanket wildcard when both exist.
            ScaledPositionsDataModel FindScaled(string ticker, string fundgroup, string fundname)
            {
                string t = Norm(ticker), g = Norm(fundgroup), f = Norm(fundname);

                IEnumerable<ScaledPositionsDataModel> candidates = Enumerable.Empty<ScaledPositionsDataModel>();
                if (scaledByTicker.TryGetValue(t, out var exactT)) candidates = candidates.Concat(exactT);
                if (t != "*" && scaledByTicker.TryGetValue("*", out var wildT)) candidates = candidates.Concat(wildT);

                ScaledPositionsDataModel best = null;
                int bestScore = -1;
                foreach (var s in candidates)
                {
                    string sg = Norm(s.FundGroupName), sf = Norm(s.FundName), st = Norm(s.TickerName);
                    bool groupOk = sg == g || sg == "*";
                    bool fundOk = sf == f || sf == "*";
                    bool tickOk = st == t || st == "*";
                    if (!(groupOk && fundOk && tickOk)) continue;

                    int score = (sf == "*" ? 0 : 4) + (sg == "*" ? 0 : 2) + (st == "*" ? 0 : 1);
                    if (score > bestScore) { bestScore = score; best = s; }
                }
                return best;
            }

            // capture current rows (for preserving user edits/baselines)
            Dictionary<string, MergedTickerRow> previous = null;
            if (preserveUserFiFlags)
                previous = MergedRows.ToDictionary(r => r.Tickername, r => r, StringComparer.OrdinalIgnoreCase);

            // Build into a temporary list to avoid per-row UI updates
            var tempRows = new List<MergedTickerRow>();

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
                    BaseT1 = fi?.BaseT1,
                    BaseV1 = fi?.BaseV1,
                    BaseN1 = fi?.BaseN1,
                    T1 = fi?.T1,
                    T2 = fi?.T2,
                    T3 = fi?.T3,
                    T4 = fi?.T4,
                    T5 = fi?.T5,
                    T8 = fi?.T8,
                    V2 = fi?.V2,
                    V3 = fi?.V3,
                    V4 = fi?.V4,
                    V6 = fi?.V6,
                    V8 = fi?.V8,
                    y4 = fi?.Y4,
                    y6 = fi?.Y6,
                    y8 = fi?.Y8,
                    y12 = fi?.Y12,
                    y24 = fi?.Y24,
                    y32 = fi?.Y32,
                    y72 = fi?.Y72,
                    N2 = fi?.N2,
                    N3 = fi?.N3,
                    N4 = fi?.N4,
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
                    PositionBaseT1 = tp.PositionBaseT1,
                    PositionBaseV1 = tp.PositionBaseV1,
                    PositionBaseN1 = tp.PositionBaseN1,
                    PositionT1 = tp.PositionT1,
                    PositionT2 = tp.PositionT2,
                    PositionT3 = tp.PositionT3,
                    PositionT4 = tp.PositionT4,
                    PositionT5 = tp.PositionT5,
                    PositionT8 = tp.PositionT8,
                    PositionV2 = tp.PositionV2,
                    PositionV3 = tp.PositionV3,
                    PositionV4 = tp.PositionV4,
                    PositionV6 = tp.PositionV6,
                    PositionV8 = tp.PositionV8,
                    PositionY4 = tp.PositionY4,
                    PositionY6 = tp.PositionY6,
                    PositionY8 = tp.PositionY8,
                    PositionY12 = tp.PositionY12,
                    PositionY24 = tp.PositionY24,
                    PositionY32 = tp.PositionY32,
                    PositionY72 = tp.PositionY72,
                    PositionN2 = tp.PositionN2,
                    PositionN3 = tp.PositionN3,
                    PositionN4 = tp.PositionN4,
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
                    PositionLimit = tp.PositionLimit,
                    ScaledPositionLimit = tp.ScaledPositionLimit,
                    PositionTarget = tp.PositionTarget,
                };

                // scaled positions join (wildcard-aware on fundgroup/fundname)
                var sp = FindScaled(tp.Tickername, fi?.FundGroup, fi?.FundName);
                if (sp != null)
                {
                    row.ScaleFactor = sp.ScaledTarget;
                    row.ScaledPercent = sp.ScaledPercent;
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
                    row.BaseT1 = old.BaseT1;
                    row.BaseV1 = old.BaseV1;
                    row.BaseN1 = old.BaseN1;
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
                    else { row.StrategyName = old.StrategyName; row.StrategyNameBase = old.StrategyNameBase; }

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

                tempRows.Add(row);
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
                    BaseT1 = fiOnly.BaseT1,
                    BaseV1 = fiOnly.BaseV1,
                    BaseN1 = fiOnly.BaseN1,
                    T1 = fiOnly.T1,
                    T2 = fiOnly.T2,
                    T3 = fiOnly.T3,
                    T4 = fiOnly.T4,
                    T5 = fiOnly.T5,
                    T8 = fiOnly.T8,
                    V2 = fiOnly.V2,
                    V3 = fiOnly.V3,
                    V4 = fiOnly.V4,
                    V6 = fiOnly.V6,
                    V8 = fiOnly.V8,
                    y4 = fiOnly.Y4,
                    y6 = fiOnly.Y6,
                    y8 = fiOnly.Y8,
                    y12 = fiOnly.Y12,
                    y24 = fiOnly.Y24,
                    y32 = fiOnly.Y32,
                    y72 = fiOnly.Y72,
                    N2 = fiOnly.N2,
                    N3 = fiOnly.N3,
                    N4 = fiOnly.N4,
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

                // join scaled positions for FI-only row as well (wildcard-aware)
                var sp2 = FindScaled(row.Tickername, row.FundGroup, row.FundName);
                if (sp2 != null)
                {
                    row.ScaleFactor = sp2.ScaledTarget;
                    row.ScaledPercent = sp2.ScaledPercent;
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
                    else { row.StrategyName = oldFi.StrategyName; row.StrategyNameBase = oldFi.StrategyNameBase; }

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

                tempRows.Add(row);
            }

            // Attach ticker "category" from the already-loaded ticker universe (no extra query),
            // then sort by category, then tickername.
            if (TickerUniverse != null && TickerUniverse.Count > 0)
            {
                var categoryByTicker = TickerUniverse
                    .Where(t => t != null && !string.IsNullOrWhiteSpace(t.TickerName))
                    .GroupBy(t => t.TickerName.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First().Category, StringComparer.OrdinalIgnoreCase);

                foreach (var r in tempRows)
                {
                    if (!string.IsNullOrWhiteSpace(r.Tickername) &&
                        categoryByTicker.TryGetValue(r.Tickername.Trim(), out var cat))
                        r.Category = cat;
                }
            }

            // Sort in memory first, then populate collection in one batch:
            // fundgroup, fundname, then category (grouped before ticker), then tickername.
            tempRows.Sort((a, b) =>
            {
                int c = string.Compare(a.FundGroup, b.FundGroup, StringComparison.OrdinalIgnoreCase);
                if (c != 0) return c;
                c = string.Compare(a.FundName, b.FundName, StringComparison.OrdinalIgnoreCase);
                if (c != 0) return c;
                c = string.Compare(a.Category, b.Category, StringComparison.OrdinalIgnoreCase);
                if (c != 0) return c;
                return string.Compare(a.Tickername, b.Tickername, StringComparison.OrdinalIgnoreCase);
            });

            // Bulk-replace in one shot: a single Reset notification instead of N Adds, so the
            // DataGrid regenerates containers and lays out once rather than per row.
            MergedRows.ReplaceAll(tempRows);

            if (MergedRowsView != null)
            {
                MergedRowsView.SortDescriptions.Clear();
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
            public string Category { get; set; }   // ticker metadata (from tickers table), display + sort only

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

            // --- NEW: StrategyNameBase (base strategy, also from strategies_override) ---
            public string OriginalStrategyNameBase { get; private set; }
            private string _strategyNameBase;
            public string StrategyNameBase
            {
                get => _strategyNameBase;
                set
                {
                    if (_strategyNameBase != value)
                    {
                        _strategyNameBase = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(StrategyNameBaseHasChanged));
                    }
                }
            }
            public bool StrategyNameBaseHasChanged => _isInitialized && !string.Equals(_strategyNameBase, OriginalStrategyNameBase, StringComparison.Ordinal);

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
                        OnPropertyChanged(nameof(ScaleFactorBrush));
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

            // ── base intervals shown on the grid as independent columns ──
            //   base_t1, base_v1, base_n1, base_y1, base_h1, base_d1 each behave
            //   like every other interval column (independent toggle + counts).
            public bool? OriginalBaseT1 { get; private set; }
            private bool? _baseT1;
            public bool? BaseT1
            {
                get => _baseT1;
                set
                {
                    if (_baseT1 != value)
                    {
                        _baseT1 = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(BaseT1HasChanged));
                        OnPropertyChanged(nameof(BaseT1Brush));
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool BaseT1HasChanged => _isInitialized && _baseT1 != OriginalBaseT1;
            private float? _positionBaseT1;
            public float? PositionBaseT1
            {
                get => _positionBaseT1;
                set
                {
                    if (_positionBaseT1 != value)
                    {
                        _positionBaseT1 = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(BaseT1Brush));
                    }
                }
            }

            public bool? OriginalBaseV1 { get; private set; }
            private bool? _baseV1;
            public bool? BaseV1
            {
                get => _baseV1;
                set
                {
                    if (_baseV1 != value)
                    {
                        _baseV1 = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(BaseV1HasChanged));
                        OnPropertyChanged(nameof(BaseV1Brush));
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool BaseV1HasChanged => _isInitialized && _baseV1 != OriginalBaseV1;
            private float? _positionBaseV1;
            public float? PositionBaseV1
            {
                get => _positionBaseV1;
                set
                {
                    if (_positionBaseV1 != value)
                    {
                        _positionBaseV1 = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(BaseV1Brush));
                    }
                }
            }

            public bool? OriginalBaseN1 { get; private set; }
            private bool? _baseN1;
            public bool? BaseN1
            {
                get => _baseN1;
                set
                {
                    if (_baseN1 != value)
                    {
                        _baseN1 = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(BaseN1HasChanged));
                        OnPropertyChanged(nameof(BaseN1Brush));
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool BaseN1HasChanged => _isInitialized && _baseN1 != OriginalBaseN1;
            private float? _positionBaseN1;
            public float? PositionBaseN1
            {
                get => _positionBaseN1;
                set
                {
                    if (_positionBaseN1 != value)
                    {
                        _positionBaseN1 = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(BaseN1Brush));
                    }
                }
            }

            // ── new short-term trading intervals ──
            public bool? OriginalT1 { get; private set; }
            private bool? _t1;
            public bool? T1
            {
                get => _t1;
                set
                {
                    if (_t1 != value)
                    {
                        _t1 = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(T1HasChanged));
                        OnPropertyChanged(nameof(T1Brush));
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool T1HasChanged => _isInitialized && _t1 != OriginalT1;
            private float? _positionT1;
            public float? PositionT1
            {
                get => _positionT1;
                set { if (_positionT1 != value) { _positionT1 = value; OnPropertyChanged(); OnPropertyChanged(nameof(T1Brush)); } }
            }

            public bool? OriginalT2 { get; private set; }
            private bool? _t2;
            public bool? T2
            {
                get => _t2;
                set
                {
                    if (_t2 != value)
                    {
                        _t2 = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(T2HasChanged));
                        OnPropertyChanged(nameof(T2Brush));
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool T2HasChanged => _isInitialized && _t2 != OriginalT2;
            private float? _positionT2;
            public float? PositionT2
            {
                get => _positionT2;
                set { if (_positionT2 != value) { _positionT2 = value; OnPropertyChanged(); OnPropertyChanged(nameof(T2Brush)); } }
            }

            public bool? OriginalT3 { get; private set; }
            private bool? _t3;
            public bool? T3
            {
                get => _t3;
                set
                {
                    if (_t3 != value)
                    {
                        _t3 = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(T3HasChanged));
                        OnPropertyChanged(nameof(T3Brush));
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool T3HasChanged => _isInitialized && _t3 != OriginalT3;
            private float? _positionT3;
            public float? PositionT3
            {
                get => _positionT3;
                set { if (_positionT3 != value) { _positionT3 = value; OnPropertyChanged(); OnPropertyChanged(nameof(T3Brush)); } }
            }

            public bool? OriginalT4 { get; private set; }
            private bool? _t4;
            public bool? T4
            {
                get => _t4;
                set
                {
                    if (_t4 != value)
                    {
                        _t4 = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(T4HasChanged));
                        OnPropertyChanged(nameof(T4Brush));
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool T4HasChanged => _isInitialized && _t4 != OriginalT4;
            private float? _positionT4;
            public float? PositionT4
            {
                get => _positionT4;
                set { if (_positionT4 != value) { _positionT4 = value; OnPropertyChanged(); OnPropertyChanged(nameof(T4Brush)); } }
            }

            public bool? OriginalT5 { get; private set; }
            private bool? _t5;
            public bool? T5
            {
                get => _t5;
                set
                {
                    if (_t5 != value)
                    {
                        _t5 = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(T5HasChanged));
                        OnPropertyChanged(nameof(T5Brush));
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool T5HasChanged => _isInitialized && _t5 != OriginalT5;
            private float? _positionT5;
            public float? PositionT5
            {
                get => _positionT5;
                set { if (_positionT5 != value) { _positionT5 = value; OnPropertyChanged(); OnPropertyChanged(nameof(T5Brush)); } }
            }

            public bool? OriginalT8 { get; private set; }
            private bool? _t8;
            public bool? T8
            {
                get => _t8;
                set
                {
                    if (_t8 != value)
                    {
                        _t8 = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(T8HasChanged));
                        OnPropertyChanged(nameof(T8Brush));
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool T8HasChanged => _isInitialized && _t8 != OriginalT8;
            private float? _positionT8;
            public float? PositionT8
            {
                get => _positionT8;
                set { if (_positionT8 != value) { _positionT8 = value; OnPropertyChanged(); OnPropertyChanged(nameof(T8Brush)); } }
            }

            public bool? OriginalV2 { get; private set; }
            private bool? _v2;
            public bool? V2
            {
                get => _v2;
                set
                {
                    if (_v2 != value)
                    {
                        _v2 = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(V2HasChanged));
                        OnPropertyChanged(nameof(V2Brush));
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool V2HasChanged => _isInitialized && _v2 != OriginalV2;
            private float? _positionV2;
            public float? PositionV2
            {
                get => _positionV2;
                set { if (_positionV2 != value) { _positionV2 = value; OnPropertyChanged(); OnPropertyChanged(nameof(V2Brush)); } }
            }

            public bool? OriginalV3 { get; private set; }
            private bool? _v3;
            public bool? V3
            {
                get => _v3;
                set
                {
                    if (_v3 != value)
                    {
                        _v3 = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(V3HasChanged));
                        OnPropertyChanged(nameof(V3Brush));
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool V3HasChanged => _isInitialized && _v3 != OriginalV3;
            private float? _positionV3;
            public float? PositionV3
            {
                get => _positionV3;
                set { if (_positionV3 != value) { _positionV3 = value; OnPropertyChanged(); OnPropertyChanged(nameof(V3Brush)); } }
            }

            public bool? OriginalV4 { get; private set; }
            private bool? _v4;
            public bool? V4
            {
                get => _v4;
                set
                {
                    if (_v4 != value)
                    {
                        _v4 = value;
                        OnPropertyChanged(nameof(V4));
                        OnPropertyChanged(nameof(V4HasChanged));
                        OnPropertyChanged(nameof(V4Brush));
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool V4HasChanged => _isInitialized && _v4 != OriginalV4;
            private float? _positionV4;
            public float? PositionV4
            {
                get => _positionV4;
                set { if (_positionV4 != value) { _positionV4 = value; OnPropertyChanged(nameof(PositionV4)); OnPropertyChanged(nameof(V4Brush)); } }
            }

            public bool? OriginalV6 { get; private set; }
            private bool? _v6;
            public bool? V6
            {
                get => _v6;
                set
                {
                    if (_v6 != value)
                    {
                        _v6 = value;
                        OnPropertyChanged(nameof(V6));
                        OnPropertyChanged(nameof(V6HasChanged));
                        OnPropertyChanged(nameof(V6Brush));
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool V6HasChanged => _isInitialized && _v6 != OriginalV6;
            private float? _positionV6;
            public float? PositionV6
            {
                get => _positionV6;
                set { if (_positionV6 != value) { _positionV6 = value; OnPropertyChanged(nameof(PositionV6)); OnPropertyChanged(nameof(V6Brush)); } }
            }

            public bool? OriginalV8 { get; private set; }
            private bool? _v8;
            public bool? V8
            {
                get => _v8;
                set
                {
                    if (_v8 != value)
                    {
                        _v8 = value;
                        OnPropertyChanged(nameof(V8));
                        OnPropertyChanged(nameof(V8HasChanged));
                        OnPropertyChanged(nameof(V8Brush));
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool V8HasChanged => _isInitialized && _v8 != OriginalV8;
            private float? _positionV8;
            public float? PositionV8
            {
                get => _positionV8;
                set { if (_positionV8 != value) { _positionV8 = value; OnPropertyChanged(nameof(PositionV8)); OnPropertyChanged(nameof(V8Brush)); } }
            }

            public bool? OriginalY4 { get; private set; }
            private bool? _y4;
            public bool? y4
            {
                get => _y4;
                set
                {
                    if (_y4 != value)
                    {
                        _y4 = value;
                        OnPropertyChanged(nameof(y4));
                        OnPropertyChanged(nameof(Y4HasChanged));
                        OnPropertyChanged(nameof(Y4Brush));
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool Y4HasChanged => _isInitialized && _y4 != OriginalY4;
            private float? _positionY4;
            public float? PositionY4
            {
                get => _positionY4;
                set { if (_positionY4 != value) { _positionY4 = value; OnPropertyChanged(nameof(PositionY4)); OnPropertyChanged(nameof(Y4Brush)); } }
            }

            public bool? OriginalY6 { get; private set; }
            private bool? _y6;
            public bool? y6
            {
                get => _y6;
                set
                {
                    if (_y6 != value)
                    {
                        _y6 = value;
                        OnPropertyChanged(nameof(y6));
                        OnPropertyChanged(nameof(Y6HasChanged));
                        OnPropertyChanged(nameof(Y6Brush));
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool Y6HasChanged => _isInitialized && _y6 != OriginalY6;
            private float? _positionY6;
            public float? PositionY6
            {
                get => _positionY6;
                set { if (_positionY6 != value) { _positionY6 = value; OnPropertyChanged(nameof(PositionY6)); OnPropertyChanged(nameof(Y6Brush)); } }
            }

            public bool? OriginalY8 { get; private set; }
            private bool? _y8;
            public bool? y8
            {
                get => _y8;
                set
                {
                    if (_y8 != value)
                    {
                        _y8 = value;
                        OnPropertyChanged(nameof(y8));
                        OnPropertyChanged(nameof(Y8HasChanged));
                        OnPropertyChanged(nameof(Y8Brush));
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool Y8HasChanged => _isInitialized && _y8 != OriginalY8;
            private float? _positionY8;
            public float? PositionY8
            {
                get => _positionY8;
                set { if (_positionY8 != value) { _positionY8 = value; OnPropertyChanged(nameof(PositionY8)); OnPropertyChanged(nameof(Y8Brush)); } }
            }

            public bool? OriginalY12 { get; private set; }
            private bool? _y12;
            public bool? y12
            {
                get => _y12;
                set
                {
                    if (_y12 != value)
                    {
                        _y12 = value;
                        OnPropertyChanged(nameof(y12));
                        OnPropertyChanged(nameof(Y12HasChanged));
                        OnPropertyChanged(nameof(Y12Brush));
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool Y12HasChanged => _isInitialized && _y12 != OriginalY12;
            private float? _positionY12;
            public float? PositionY12
            {
                get => _positionY12;
                set { if (_positionY12 != value) { _positionY12 = value; OnPropertyChanged(nameof(PositionY12)); OnPropertyChanged(nameof(Y12Brush)); } }
            }

            public bool? OriginalY24 { get; private set; }
            private bool? _y24;
            public bool? y24
            {
                get => _y24;
                set
                {
                    if (_y24 != value)
                    {
                        _y24 = value;
                        OnPropertyChanged(nameof(y24));
                        OnPropertyChanged(nameof(Y24HasChanged));
                        OnPropertyChanged(nameof(Y24Brush));
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool Y24HasChanged => _isInitialized && _y24 != OriginalY24;
            private float? _positionY24;
            public float? PositionY24
            {
                get => _positionY24;
                set { if (_positionY24 != value) { _positionY24 = value; OnPropertyChanged(nameof(PositionY24)); OnPropertyChanged(nameof(Y24Brush)); } }
            }

            public bool? OriginalY32 { get; private set; }
            private bool? _y32;
            public bool? y32
            {
                get => _y32;
                set
                {
                    if (_y32 != value)
                    {
                        _y32 = value;
                        OnPropertyChanged(nameof(y32));
                        OnPropertyChanged(nameof(Y32HasChanged));
                        OnPropertyChanged(nameof(Y32Brush));
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool Y32HasChanged => _isInitialized && _y32 != OriginalY32;
            private float? _positionY32;
            public float? PositionY32
            {
                get => _positionY32;
                set { if (_positionY32 != value) { _positionY32 = value; OnPropertyChanged(nameof(PositionY32)); OnPropertyChanged(nameof(Y32Brush)); } }
            }

            public bool? OriginalY72 { get; private set; }
            private bool? _y72;
            public bool? y72
            {
                get => _y72;
                set
                {
                    if (_y72 != value)
                    {
                        _y72 = value;
                        OnPropertyChanged(nameof(y72));
                        OnPropertyChanged(nameof(Y72HasChanged));
                        OnPropertyChanged(nameof(Y72Brush));
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool Y72HasChanged => _isInitialized && _y72 != OriginalY72;
            private float? _positionY72;
            public float? PositionY72
            {
                get => _positionY72;
                set { if (_positionY72 != value) { _positionY72 = value; OnPropertyChanged(nameof(PositionY72)); OnPropertyChanged(nameof(Y72Brush)); } }
            }

            public bool? OriginalN2 { get; private set; }
            private bool? _n2;
            public bool? N2
            {
                get => _n2;
                set
                {
                    if (_n2 != value)
                    {
                        _n2 = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(N2HasChanged));
                        OnPropertyChanged(nameof(N2Brush));
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool N2HasChanged => _isInitialized && _n2 != OriginalN2;
            private float? _positionN2;
            public float? PositionN2
            {
                get => _positionN2;
                set { if (_positionN2 != value) { _positionN2 = value; OnPropertyChanged(); OnPropertyChanged(nameof(N2Brush)); } }
            }

            public bool? OriginalN3 { get; private set; }
            private bool? _n3;
            public bool? N3
            {
                get => _n3;
                set
                {
                    if (_n3 != value)
                    {
                        _n3 = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(N3HasChanged));
                        OnPropertyChanged(nameof(N3Brush));
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool N3HasChanged => _isInitialized && _n3 != OriginalN3;
            private float? _positionN3;
            public float? PositionN3
            {
                get => _positionN3;
                set { if (_positionN3 != value) { _positionN3 = value; OnPropertyChanged(); OnPropertyChanged(nameof(N3Brush)); } }
            }

            public bool? OriginalN4 { get; private set; }
            private bool? _n4;
            public bool? N4
            {
                get => _n4;
                set
                {
                    if (_n4 != value)
                    {
                        _n4 = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(N4HasChanged));
                        OnPropertyChanged(nameof(N4Brush));
                        RecalcNewTrades();
                        RecalcRescaledIntervals();
                    }
                }
            }
            public bool N4HasChanged => _isInitialized && _n4 != OriginalN4;
            private float? _positionN4;
            public float? PositionN4
            {
                get => _positionN4;
                set { if (_positionN4 != value) { _positionN4 = value; OnPropertyChanged(); OnPropertyChanged(nameof(N4Brush)); } }
            }

            // ── min_intvl (from strategies_override.min_update_freq) ──
            public string OriginalMinIntvl { get; private set; }
            private string _minIntvl = "default";
            public string MinIntvl
            {
                get => _minIntvl;
                set
                {
                    if (_minIntvl != value)
                    {
                        _minIntvl = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(MinIntvlHasChanged));
                    }
                }
            }
            public bool MinIntvlHasChanged => _isInitialized && _minIntvl != OriginalMinIntvl;

            // ── cont_upd (from strategies_override.continuous_update) ──
            public bool? OriginalContUpd { get; private set; }
            private bool? _contUpd = false;
            public bool? ContUpd
            {
                get => _contUpd;
                set
                {
                    if (_contUpd != value)
                    {
                        _contUpd = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(ContUpdHasChanged));
                    }
                }
            }
            public bool ContUpdHasChanged => _isInitialized && _contUpd != OriginalContUpd;

            // ── pos_intvl (from strategies_override.min_pos_freq) ──
            public string OriginalPosIntvl { get; private set; }
            private string _posIntvl = "default";
            public string PosIntvl
            {
                get => _posIntvl;
                set
                {
                    if (_posIntvl != value)
                    {
                        _posIntvl = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(PosIntvlHasChanged));
                    }
                }
            }
            public bool PosIntvlHasChanged => _isInitialized && _posIntvl != OriginalPosIntvl;

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

            // position limits (read-only; from target_positions merged on tickername+fundname)
            public float? PositionLimit { get; set; }
            public float? ScaledPositionLimit { get; set; }
            public float? PositionTarget { get; set; }


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
                        OnPropertyChanged(nameof(ProposedChangeBrush));
                    }
                }
            }

            private double? _prevNewDeploymentForBrush;   // last rounded value for delta coloring
            private bool _firstNewDeployment = true;      // only for "initial zero shows grey"

            //setting to detect changes for tickerprocessing further downstream
            // convenience aliases (optional)
            public bool HasIntervalEdits => HasAnyEdits; // your existing property

            public bool HasStrategyEdit => _isInitialized && (StrategyNameHasChanged || StrategyNameBaseHasChanged);

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

            /// <summary>
            /// True when this is a new ticker with no existing strategies_override row.
            /// Forces a default override row to be written on save.
            /// </summary>
            public bool NeedsNewOverride { get; set; }

            //for setting colours
            // Reuse ONE set of static, frozen brushes
            private static readonly Brush BgGrey = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            private static readonly Brush BgPaleGreen = Brushes.PaleGreen;
            private static readonly Brush BgMistyRose = Brushes.MistyRose;
            private static readonly Brush BgIndianRed = Brushes.IndianRed;
            private static readonly Brush BgMediumSeaGreen = Brushes.MediumSeaGreen;
            private static readonly Brush BgTransparent = Brushes.Transparent;
            private static readonly Brush BgLightOrange = new SolidColorBrush(Color.FromRgb(0xFF, 0xE8, 0xCC));  // scale_f ≠ 1 (distinct from manual)

            public Brush BaseY1Brush => ComputeFlagPosBrush(BaseY1, PositionBaseY1);
            public Brush BaseH1Brush => ComputeFlagPosBrush(BaseH1, PositionBaseH1);
            public Brush BaseD1Brush => ComputeFlagPosBrush(BaseD1, PositionBaseD1);
            public Brush BaseT1Brush => ComputeFlagPosBrush(BaseT1, PositionBaseT1);
            public Brush BaseV1Brush => ComputeFlagPosBrush(BaseV1, PositionBaseV1);
            public Brush BaseN1Brush => ComputeFlagPosBrush(BaseN1, PositionBaseN1);
            public Brush T1Brush => ComputeFlagPosBrush(T1, PositionT1);
            public Brush T2Brush => ComputeFlagPosBrush(T2, PositionT2);
            public Brush T3Brush => ComputeFlagPosBrush(T3, PositionT3);
            public Brush T4Brush => ComputeFlagPosBrush(T4, PositionT4);
            public Brush T5Brush => ComputeFlagPosBrush(T5, PositionT5);
            public Brush T8Brush => ComputeFlagPosBrush(T8, PositionT8);
            public Brush V2Brush => ComputeFlagPosBrush(V2, PositionV2);
            public Brush V3Brush => ComputeFlagPosBrush(V3, PositionV3);
            public Brush V4Brush => ComputeFlagPosBrush(V4, PositionV4);
            public Brush V6Brush => ComputeFlagPosBrush(V6, PositionV6);
            public Brush V8Brush => ComputeFlagPosBrush(V8, PositionV8);
            public Brush Y4Brush => ComputeFlagPosBrush(y4, PositionY4);
            public Brush Y6Brush => ComputeFlagPosBrush(y6, PositionY6);
            public Brush Y8Brush => ComputeFlagPosBrush(y8, PositionY8);
            public Brush Y12Brush => ComputeFlagPosBrush(y12, PositionY12);
            public Brush Y24Brush => ComputeFlagPosBrush(y24, PositionY24);
            public Brush Y32Brush => ComputeFlagPosBrush(y32, PositionY32);
            public Brush Y72Brush => ComputeFlagPosBrush(y72, PositionY72);
            public Brush N2Brush => ComputeFlagPosBrush(N2, PositionN2);
            public Brush N3Brush => ComputeFlagPosBrush(N3, PositionN3);
            public Brush N4Brush => ComputeFlagPosBrush(N4, PositionN4);
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

            // Diverging cell colour by deployment: dark red (-100%) → white (0) → dark green
            // (+100%), intensity scaling with |deployment|. Used for the live (dep) and filtered
            // (f_dep) triplets — all three cells of a triplet share the one deployment value.
            private static Color DivergeColor(double dep)
            {
                double d = Math.Max(-1.0, Math.Min(1.0, dep));
                double t = Math.Abs(d);
                byte tr = d >= 0 ? (byte)0 : (byte)139;    // endpoint: dark green vs dark red
                byte tg = d >= 0 ? (byte)100 : (byte)0;
                byte tb = 0;
                byte r = (byte)Math.Round(255 + (tr - 255) * t);
                byte g = (byte)Math.Round(255 + (tg - 255) * t);
                byte b = (byte)Math.Round(255 + (tb - 255) * t);
                return Color.FromRgb(r, g, b);
            }
            private static Brush DeploymentDivergeBrush(float? dep)
            {
                if (!dep.HasValue) return BgGrey;   // no data
                var br = new SolidColorBrush(DivergeColor(dep.Value));
                br.Freeze();
                return br;
            }
            private static Brush DeploymentForeground(float? dep)
            {
                if (!dep.HasValue) return Brushes.Black;
                var c = DivergeColor(dep.Value);
                double lum = 0.299 * c.R + 0.587 * c.G + 0.114 * c.B;   // white text on dark bg
                return lum < 140 ? Brushes.White : Brushes.Black;
            }

            public Brush LiveDeploymentBrush => DeploymentDivergeBrush(PositionDeployment);
            public Brush LiveDeploymentForeground => DeploymentForeground(PositionDeployment);
            public Brush FilteredDeploymentBrush => DeploymentDivergeBrush(FilteredDeployment);
            public Brush FilteredDeploymentForeground => DeploymentForeground(FilteredDeployment);

            // Proposed triplet (new_t / new_n / n_dep): highlight only when the proposed deployment
            // differs from the saved filtered deployment — light green if more bullish (higher),
            // light red if more bearish (lower). One shared signal so all three cells agree
            // (the previous per-cell brushes compared different metrics and disagreed).
            public Brush ProposedChangeBrush
            {
                get
                {
                    if (!NewDeployment.HasValue || !FilteredDeployment.HasValue) return BgTransparent;
                    // Compare at the displayed precision (P1 = 0.1%). NewDeployment is a double and
                    // FilteredDeployment is a float, so a raw compare shows spurious float↔double
                    // noise (~1e-8) as a "change" even when the shown values are identical.
                    double a = Math.Round(NewDeployment.Value, 3);
                    double b = Math.Round((double)FilteredDeployment.Value, 3);
                    if (a == b) return BgTransparent;             // no proposed change
                    return a > b ? BgPaleGreen : BgMistyRose;     // more bullish : more bearish
                }
            }

            // scale_f highlight: very light orange when a scale factor is set and not 1 (i.e. the
            // ticker is actually being scaled). Blank otherwise. Distinct from the manual column.
            public Brush ScaleFactorBrush =>
                (ScaleFactor.HasValue && Math.Abs(ScaleFactor.Value - 1.0) > 1e-9) ? BgLightOrange : BgTransparent;


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
                if (so == null) NeedsNewOverride = true;

                // Use the override's strategy as the "original" baseline (fall back to "default")
                var baseline = so?.StrategyName ?? "default";
                OriginalStrategyName = baseline;
                StrategyName = baseline;         // this is what your UI binds to
                OnPropertyChanged(nameof(OriginalStrategyName));
                OnPropertyChanged(nameof(StrategyName));
                OnPropertyChanged(nameof(StrategyNameHasChanged));

                // base strategy baseline (fall back to "default")
                var baselineBase = so?.StrategyNameBase ?? "default";
                OriginalStrategyNameBase = baselineBase;
                StrategyNameBase = baselineBase;
                OnPropertyChanged(nameof(OriginalStrategyNameBase));
                OnPropertyChanged(nameof(StrategyNameBase));
                OnPropertyChanged(nameof(StrategyNameBaseHasChanged));

                // min_intvl from min_update_freq
                var minIntvlBaseline = so?.MinUpdateFreq ?? "default";
                OriginalMinIntvl = minIntvlBaseline;
                MinIntvl = minIntvlBaseline;
                OnPropertyChanged(nameof(MinIntvl));
                OnPropertyChanged(nameof(MinIntvlHasChanged));

                // cont_upd from continuous_update
                var contUpdBaseline = so?.ContinuousUpdate ?? false;
                OriginalContUpd = contUpdBaseline;
                ContUpd = contUpdBaseline;
                OnPropertyChanged(nameof(ContUpd));
                OnPropertyChanged(nameof(ContUpdHasChanged));

                // pos_intvl from min_pos_freq
                var posIntvlBaseline = so?.MinPosFreq ?? "default";
                OriginalPosIntvl = posIntvlBaseline;
                PosIntvl = posIntvlBaseline;
                OnPropertyChanged(nameof(PosIntvl));
                OnPropertyChanged(nameof(PosIntvlHasChanged));
            }

            // Build the payload row for /strategies_override insert when StrategyName changed
            public object ToStrategyOverrideInsertModel(DateTime runtimeUtc, string envDefault)
            {
                // choose env from current override else fall back to a VM-wide default
                var env = StrategyOverride?.Env ?? envDefault;

                string minVal = this.MinIntvl ?? "default";
                string minPosVal = this.PosIntvl ?? "default";

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
                    continuous_update = this.ContUpd ?? StrategyOverride?.ContinuousUpdate ?? false,
                    keep_updated = StrategyOverride?.KeepUpdated ?? true,
                    calc_trades = StrategyOverride?.CalcTrades ?? true,
                    take_position = StrategyOverride?.TakePosition ?? true,
                    strategyname_base = this.StrategyNameBase ?? "default",
                    strategyname_base_daily = StrategyOverride?.StrategyNameBaseDaily ?? "default",

                    min_update_freq = minVal,
                    min_chart_freq = minVal,
                    min_pos_freq = minPosVal,
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
                OriginalBaseT1 = src.OriginalBaseT1;
                OriginalBaseV1 = src.OriginalBaseV1;
                OriginalBaseN1 = src.OriginalBaseN1;
                OriginalT1 = src.OriginalT1;
                OriginalT2 = src.OriginalT2;
                OriginalT3 = src.OriginalT3;
                OriginalT4 = src.OriginalT4;
                OriginalT5 = src.OriginalT5;
                OriginalT8 = src.OriginalT8;
                OriginalV2 = src.OriginalV2;
                OriginalV3 = src.OriginalV3;
                OriginalV4 = src.OriginalV4;
                OriginalV6 = src.OriginalV6;
                OriginalV8 = src.OriginalV8;
                OriginalY4 = src.OriginalY4;
                OriginalY6 = src.OriginalY6;
                OriginalY8 = src.OriginalY8;
                OriginalY12 = src.OriginalY12;
                OriginalY24 = src.OriginalY24;
                OriginalY32 = src.OriginalY32;
                OriginalY72 = src.OriginalY72;
                OriginalN2 = src.OriginalN2;
                OriginalN3 = src.OriginalN3;
                OriginalN4 = src.OriginalN4;
                OriginalMinIntvl = src.OriginalMinIntvl;
                OriginalContUpd = src.OriginalContUpd;
                OriginalPosIntvl = src.OriginalPosIntvl;

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
                OriginalStrategyNameBase = src.OriginalStrategyNameBase;
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
                OnPropertyChanged(nameof(BaseT1HasChanged));
                OnPropertyChanged(nameof(BaseV1HasChanged));
                OnPropertyChanged(nameof(BaseN1HasChanged));
                OnPropertyChanged(nameof(T1HasChanged));
                OnPropertyChanged(nameof(T2HasChanged));
                OnPropertyChanged(nameof(T3HasChanged));
                OnPropertyChanged(nameof(T4HasChanged));
                OnPropertyChanged(nameof(T5HasChanged));
                OnPropertyChanged(nameof(T8HasChanged));
                OnPropertyChanged(nameof(V2HasChanged));
                OnPropertyChanged(nameof(V3HasChanged));
                OnPropertyChanged(nameof(N2HasChanged));
                OnPropertyChanged(nameof(N3HasChanged));
                OnPropertyChanged(nameof(N4HasChanged));
                OnPropertyChanged(nameof(MinIntvlHasChanged));
                OnPropertyChanged(nameof(ContUpdHasChanged));
                OnPropertyChanged(nameof(PosIntvlHasChanged));
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
                OnPropertyChanged(nameof(V4HasChanged));
                OnPropertyChanged(nameof(V6HasChanged));
                OnPropertyChanged(nameof(V8HasChanged));
                OnPropertyChanged(nameof(Y4HasChanged));
                OnPropertyChanged(nameof(Y6HasChanged));
                OnPropertyChanged(nameof(Y8HasChanged));
                OnPropertyChanged(nameof(Y12HasChanged));
                OnPropertyChanged(nameof(Y24HasChanged));
                OnPropertyChanged(nameof(Y32HasChanged));
                OnPropertyChanged(nameof(Y72HasChanged));
                OnPropertyChanged(nameof(ManualHasChanged));
                OnPropertyChanged(nameof(StrategyNameHasChanged));
                OnPropertyChanged(nameof(StrategyNameBaseHasChanged));
            }


            // Count how many intervals are ACTIVE in the *current* row based on flags only
            // (flag == false means "included/active"). Ignores TadPositions entirely.
            private int CountFiOnlyCurrent()
            {
                int c = 0;
                if (BaseY1 == true) c++;
                if (BaseH1 == true) c++;
                if (BaseD1 == true) c++;
                if (BaseT1 == true) c++;
                if (BaseV1 == true) c++;
                if (BaseN1 == true) c++;

                if (T1 == true) c++;
                if (T2 == true) c++;
                if (T3 == true) c++;
                if (T4 == true) c++;
                if (T5 == true) c++;
                if (T8 == true) c++;
                if (V2 == true) c++;
                if (V3 == true) c++;
                if (V4 == true) c++;
                if (V6 == true) c++;
                if (V8 == true) c++;
                if (y4 == true) c++;
                if (y6 == true) c++;
                if (y8 == true) c++;
                if (y12 == true) c++;
                if (y24 == true) c++;
                if (y32 == true) c++;
                if (y72 == true) c++;
                if (N2 == true) c++;
                if (N3 == true) c++;
                if (N4 == true) c++;

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
                if (OriginalBaseT1 == false) c++;
                if (OriginalBaseV1 == false) c++;
                if (OriginalBaseN1 == false) c++;

                if (OriginalT1 == false) c++;
                if (OriginalT2 == false) c++;
                if (OriginalT3 == false) c++;
                if (OriginalT4 == false) c++;
                if (OriginalT5 == false) c++;
                if (OriginalT8 == false) c++;
                if (OriginalV2 == false) c++;
                if (OriginalV3 == false) c++;
                if (OriginalV4 == false) c++;
                if (OriginalV6 == false) c++;
                if (OriginalV8 == false) c++;
                if (OriginalY4 == false) c++;
                if (OriginalY6 == false) c++;
                if (OriginalY8 == false) c++;
                if (OriginalY12 == false) c++;
                if (OriginalY24 == false) c++;
                if (OriginalY32 == false) c++;
                if (OriginalY72 == false) c++;
                if (OriginalN2 == false) c++;
                if (OriginalN3 == false) c++;
                if (OriginalN4 == false) c++;

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
                CountIfFiltered(BaseT1, PositionBaseT1, ref c, excludeZeroPositions);
                CountIfFiltered(BaseV1, PositionBaseV1, ref c, excludeZeroPositions);
                CountIfFiltered(BaseN1, PositionBaseN1, ref c, excludeZeroPositions);
                CountIfFiltered(T1, PositionT1, ref c, excludeZeroPositions);
                CountIfFiltered(T2, PositionT2, ref c, excludeZeroPositions);
                CountIfFiltered(T3, PositionT3, ref c, excludeZeroPositions);
                CountIfFiltered(T4, PositionT4, ref c, excludeZeroPositions);
                CountIfFiltered(T5, PositionT5, ref c, excludeZeroPositions);
                CountIfFiltered(T8, PositionT8, ref c, excludeZeroPositions);
                CountIfFiltered(V2, PositionV2, ref c, excludeZeroPositions);
                CountIfFiltered(V3, PositionV3, ref c, excludeZeroPositions);
                CountIfFiltered(V4, PositionV4, ref c, excludeZeroPositions);
                CountIfFiltered(V6, PositionV6, ref c, excludeZeroPositions);
                CountIfFiltered(V8, PositionV8, ref c, excludeZeroPositions);
                CountIfFiltered(y4, PositionY4, ref c, excludeZeroPositions);
                CountIfFiltered(y6, PositionY6, ref c, excludeZeroPositions);
                CountIfFiltered(y8, PositionY8, ref c, excludeZeroPositions);
                CountIfFiltered(y12, PositionY12, ref c, excludeZeroPositions);
                CountIfFiltered(y24, PositionY24, ref c, excludeZeroPositions);
                CountIfFiltered(y32, PositionY32, ref c, excludeZeroPositions);
                CountIfFiltered(y72, PositionY72, ref c, excludeZeroPositions);
                CountIfFiltered(N2, PositionN2, ref c, excludeZeroPositions);
                CountIfFiltered(N3, PositionN3, ref c, excludeZeroPositions);
                CountIfFiltered(N4, PositionN4, ref c, excludeZeroPositions);
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
                CountIfFiltered(OriginalBaseT1, PositionBaseT1, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalBaseV1, PositionBaseV1, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalBaseN1, PositionBaseN1, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalT1, PositionT1, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalT2, PositionT2, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalT3, PositionT3, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalT4, PositionT4, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalT5, PositionT5, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalT8, PositionT8, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalV2, PositionV2, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalV3, PositionV3, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalV4, PositionV4, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalV6, PositionV6, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalV8, PositionV8, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalY4, PositionY4, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalY6, PositionY6, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalY8, PositionY8, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalY12, PositionY12, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalY24, PositionY24, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalY32, PositionY32, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalY72, PositionY72, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalN2, PositionN2, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalN3, PositionN3, ref c, excludeZeroPositions);
                CountIfFiltered(OriginalN4, PositionN4, ref c, excludeZeroPositions);
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



            // Tally one flag/position pair into a group's totals: counts a position interval
            // (position present, optionally non-zero) and whether it's active (flag == false).
            private static void Tally(bool? flag, float? pos, bool excludeZero, ref int total, ref int active)
            {
                if (!pos.HasValue) return;
                if (excludeZero && Math.Abs(pos.Value) <= 1e-9) return;
                total++;
                if (flag == false) active++;   // flag == false ⇒ not filtered ⇒ active
            }

            private void CountBaseGroup(bool excludeZero, out int total, out int active)
            {
                total = 0; active = 0;
                Tally(BaseY1, PositionBaseY1, excludeZero, ref total, ref active);
                Tally(BaseH1, PositionBaseH1, excludeZero, ref total, ref active);
                Tally(BaseD1, PositionBaseD1, excludeZero, ref total, ref active);
                Tally(BaseT1, PositionBaseT1, excludeZero, ref total, ref active);
                Tally(BaseV1, PositionBaseV1, excludeZero, ref total, ref active);
                Tally(BaseN1, PositionBaseN1, excludeZero, ref total, ref active);
            }

            private void CountNonBaseGroup(bool excludeZero, out int total, out int active)
            {
                total = 0; active = 0;
                Tally(T1, PositionT1, excludeZero, ref total, ref active);
                Tally(T2, PositionT2, excludeZero, ref total, ref active);
                Tally(T3, PositionT3, excludeZero, ref total, ref active);
                Tally(T4, PositionT4, excludeZero, ref total, ref active);
                Tally(T5, PositionT5, excludeZero, ref total, ref active);
                Tally(T8, PositionT8, excludeZero, ref total, ref active);
                Tally(V2, PositionV2, excludeZero, ref total, ref active);
                Tally(V3, PositionV3, excludeZero, ref total, ref active);
                Tally(V4, PositionV4, excludeZero, ref total, ref active);
                Tally(V6, PositionV6, excludeZero, ref total, ref active);
                Tally(V8, PositionV8, excludeZero, ref total, ref active);
                Tally(N2, PositionN2, excludeZero, ref total, ref active);
                Tally(N3, PositionN3, excludeZero, ref total, ref active);
                Tally(N4, PositionN4, excludeZero, ref total, ref active);
                Tally(y1, PositionY1, excludeZero, ref total, ref active);
                Tally(y2, PositionY2, excludeZero, ref total, ref active);
                Tally(y3, PositionY3, excludeZero, ref total, ref active);
                Tally(y4, PositionY4, excludeZero, ref total, ref active);
                Tally(y6, PositionY6, excludeZero, ref total, ref active);
                Tally(y8, PositionY8, excludeZero, ref total, ref active);
                Tally(y12, PositionY12, excludeZero, ref total, ref active);
                Tally(y24, PositionY24, excludeZero, ref total, ref active);
                Tally(y32, PositionY32, excludeZero, ref total, ref active);
                Tally(y72, PositionY72, excludeZero, ref total, ref active);
                Tally(H2, PositionH2, excludeZero, ref total, ref active);
                Tally(H3, PositionH3, excludeZero, ref total, ref active);
                Tally(H4, PositionH4, excludeZero, ref total, ref active);
                Tally(H6, PositionH6, excludeZero, ref total, ref active);
                Tally(H12, PositionH12, excludeZero, ref total, ref active);
                Tally(H16, PositionH16, excludeZero, ref total, ref active);
                Tally(H36, PositionH36, excludeZero, ref total, ref active);
                Tally(D1, PositionD1, excludeZero, ref total, ref active);
                Tally(D2, PositionD2, excludeZero, ref total, ref active);
                Tally(D3, PositionD3, excludeZero, ref total, ref active);
                Tally(D4, PositionD4, excludeZero, ref total, ref active);
                Tally(D8, PositionD8, excludeZero, ref total, ref active);
                Tally(W1, PositionW1, excludeZero, ref total, ref active);
                Tally(W2, PositionW2, excludeZero, ref total, ref active);
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

                    // Allow 0 for DISPLAY (e.g. every position interval filtered out); clamp to
                    // [0, AH2]. The divide-by-zero-safe divisor (max(new_n, 1)) is applied only in
                    // RecalcNewDeployment — new_n itself is shown as the real value.
                    x = Math.Min(ah2, Math.Max(0, activeNow));
                }
                else
                {
                    // No rescale: deployment spreads over the FULL position-interval count of each
                    // group (base, non-base) that still has ≥1 active (unfiltered) interval; a group
                    // with none active contributes 0. So n = baseTotal (iff any base active)
                    // + nonBaseTotal (iff any non-base active).
                    CountBaseGroup(excludeZeroPositions, out int baseTotal, out int baseActive);
                    CountNonBaseGroup(excludeZeroPositions, out int nonBaseTotal, out int nonBaseActive);
                    x = (baseActive > 0 ? baseTotal : 0) + (nonBaseActive > 0 ? nonBaseTotal : 0);
                }

                RescaledIntervals = x;
                RecalcNewDeployment();
                OnPropertyChanged(nameof(NewDeploymentBrush));

            }

            // Right-click bulk actions (this row): "off" = filtered = flag true (matching the
            // ON/blank display where flag==false shows ON). Only the visible interval set is
            // touched. Each setter already recalcs trades/rescaled/deployment; ReCalcScaledDeployment
            // is called once at the end so s_dep updates too.
            public void TurnOffBaseIntervals()
            {
                BaseT1 = true; BaseV1 = true; BaseY1 = true; BaseD1 = true;
                ReCalcScaledDeployment();
            }

            public void TurnOffNonBaseIntervals()
            {
                T1 = true; T2 = true; T3 = true; T4 = true; T5 = true; T8 = true;
                V2 = true; V3 = true; V4 = true; V6 = true; V8 = true;
                y2 = true; y3 = true; y4 = true; y6 = true; y8 = true; y12 = true; y24 = true; y32 = true;
                D1 = true; y72 = true; D2 = true; D3 = true; D4 = true; W1 = true; D8 = true; W2 = true;
                ReCalcScaledDeployment();
            }

            public void TurnOffAllIntervals()
            {
                TurnOffBaseIntervals();
                TurnOffNonBaseIntervals();
            }

            public bool HasAnyEdits =>
                RescaleHasChanged
                || LongOnlyHasChanged || ShortOnlyHasChanged || BuyOnlyHasChanged || SellOnlyHasChanged || AllIntervalsHasChanged
                || BaseY1HasChanged || BaseH1HasChanged || BaseD1HasChanged
                || BaseT1HasChanged || BaseV1HasChanged || BaseN1HasChanged
                || T1HasChanged || T2HasChanged || T3HasChanged || T4HasChanged || T5HasChanged || T8HasChanged
                || V2HasChanged || V3HasChanged || V4HasChanged || V6HasChanged || V8HasChanged || N2HasChanged || N3HasChanged || N4HasChanged
                || Y1HasChanged || Y2HasChanged || Y3HasChanged || Y4HasChanged || Y6HasChanged || Y8HasChanged || Y12HasChanged || Y24HasChanged || Y32HasChanged || Y72HasChanged
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
                AddIfNotFiltered(BaseT1, PositionBaseT1, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(BaseV1, PositionBaseV1, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(BaseN1, PositionBaseN1, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(T1, PositionT1, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(T2, PositionT2, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(T3, PositionT3, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(T4, PositionT4, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(T5, PositionT5, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(T8, PositionT8, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(V2, PositionV2, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(V3, PositionV3, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(V4, PositionV4, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(V6, PositionV6, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(V8, PositionV8, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(y4, PositionY4, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(y6, PositionY6, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(y8, PositionY8, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(y12, PositionY12, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(y24, PositionY24, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(y32, PositionY32, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(y72, PositionY72, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(N2, PositionN2, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(N3, PositionN3, BuyOnly, SellOnly, ref sum);
                AddIfNotFiltered(N4, PositionN4, BuyOnly, SellOnly, ref sum);
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

                if (NewTrades.HasValue && RescaledIntervals.HasValue)
                {
                    // new_n is displayed as-is (may be 0), but clamp the divisor to >= 1 so an
                    // all-filtered row (new_n = 0) doesn't divide by zero.
                    float divisor = Math.Max(1f, (float)RescaledIntervals.Value);
                    float r = NewTrades.Value / divisor;

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
                OriginalBaseT1 = _baseT1;
                OriginalBaseV1 = _baseV1;
                OriginalBaseN1 = _baseN1;
                OriginalT1 = _t1;
                OriginalT2 = _t2;
                OriginalT3 = _t3;
                OriginalT4 = _t4;
                OriginalT5 = _t5;
                OriginalT8 = _t8;
                OriginalV2 = _v2;
                OriginalV3 = _v3;
                OriginalV4 = _v4;
                OriginalV6 = _v6;
                OriginalV8 = _v8;
                OriginalY4 = _y4;
                OriginalY6 = _y6;
                OriginalY8 = _y8;
                OriginalY12 = _y12;
                OriginalY24 = _y24;
                OriginalY32 = _y32;
                OriginalY72 = _y72;
                OriginalN2 = _n2;
                OriginalN3 = _n3;
                OriginalN4 = _n4;
                OriginalMinIntvl = _minIntvl;
                OriginalContUpd = _contUpd;
                OriginalPosIntvl = _posIntvl;
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
                OriginalStrategyNameBase = _strategyNameBase;
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
                OnPropertyChanged(nameof(BaseT1HasChanged));
                OnPropertyChanged(nameof(BaseV1HasChanged));
                OnPropertyChanged(nameof(BaseN1HasChanged));
                OnPropertyChanged(nameof(T1HasChanged));
                OnPropertyChanged(nameof(T2HasChanged));
                OnPropertyChanged(nameof(T3HasChanged));
                OnPropertyChanged(nameof(T4HasChanged));
                OnPropertyChanged(nameof(T5HasChanged));
                OnPropertyChanged(nameof(T8HasChanged));
                OnPropertyChanged(nameof(V2HasChanged));
                OnPropertyChanged(nameof(V3HasChanged));
                OnPropertyChanged(nameof(N2HasChanged));
                OnPropertyChanged(nameof(N3HasChanged));
                OnPropertyChanged(nameof(N4HasChanged));
                OnPropertyChanged(nameof(MinIntvlHasChanged));
                OnPropertyChanged(nameof(ContUpdHasChanged));
                OnPropertyChanged(nameof(PosIntvlHasChanged));
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
                OnPropertyChanged(nameof(V4HasChanged));
                OnPropertyChanged(nameof(V6HasChanged));
                OnPropertyChanged(nameof(V8HasChanged));
                OnPropertyChanged(nameof(Y4HasChanged));
                OnPropertyChanged(nameof(Y6HasChanged));
                OnPropertyChanged(nameof(Y8HasChanged));
                OnPropertyChanged(nameof(Y12HasChanged));
                OnPropertyChanged(nameof(Y24HasChanged));
                OnPropertyChanged(nameof(Y32HasChanged));
                OnPropertyChanged(nameof(Y72HasChanged));
                OnPropertyChanged(nameof(ManualHasChanged));
                OnPropertyChanged(nameof(StrategyNameHasChanged));
                OnPropertyChanged(nameof(StrategyNameBaseHasChanged));
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
                    BaseT1 = PickForSave(this.BaseT1, this.OriginalBaseT1, this.BaseT1HasChanged),
                    BaseV1 = PickForSave(this.BaseV1, this.OriginalBaseV1, this.BaseV1HasChanged),
                    BaseN1 = PickForSave(this.BaseN1, this.OriginalBaseN1, this.BaseN1HasChanged),

                    T1 = PickForSave(this.T1, this.OriginalT1, this.T1HasChanged),
                    T2 = PickForSave(this.T2, this.OriginalT2, this.T2HasChanged),
                    T3 = PickForSave(this.T3, this.OriginalT3, this.T3HasChanged),
                    T4 = PickForSave(this.T4, this.OriginalT4, this.T4HasChanged),
                    T5 = PickForSave(this.T5, this.OriginalT5, this.T5HasChanged),
                    T8 = PickForSave(this.T8, this.OriginalT8, this.T8HasChanged),
                    V2 = PickForSave(this.V2, this.OriginalV2, this.V2HasChanged),
                    V3 = PickForSave(this.V3, this.OriginalV3, this.V3HasChanged),
                    V4 = PickForSave(this.V4, this.OriginalV4, this.V4HasChanged),
                    V6 = PickForSave(this.V6, this.OriginalV6, this.V6HasChanged),
                    V8 = PickForSave(this.V8, this.OriginalV8, this.V8HasChanged),
                    Y4 = PickForSave(this.y4, this.OriginalY4, this.Y4HasChanged),
                    Y6 = PickForSave(this.y6, this.OriginalY6, this.Y6HasChanged),
                    Y8 = PickForSave(this.y8, this.OriginalY8, this.Y8HasChanged),
                    Y12 = PickForSave(this.y12, this.OriginalY12, this.Y12HasChanged),
                    Y24 = PickForSave(this.y24, this.OriginalY24, this.Y24HasChanged),
                    Y32 = PickForSave(this.y32, this.OriginalY32, this.Y32HasChanged),
                    Y72 = PickForSave(this.y72, this.OriginalY72, this.Y72HasChanged),
                    N2 = PickForSave(this.N2, this.OriginalN2, this.N2HasChanged),
                    N3 = PickForSave(this.N3, this.OriginalN3, this.N3HasChanged),
                    N4 = PickForSave(this.N4, this.OriginalN4, this.N4HasChanged),

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

            [JsonProperty("category")]
            public string Category { get; set; }

            [JsonProperty("fundgroupname")]
            public string FundGroupName { get; set; }

            [JsonProperty("fundname")]
            public string FundName { get; set; }
        }

        public async Task LoadTickerUniverseAsync()
        {
            // Note: this method does NOT manage IsExecuting itself — outer callers
            // (RunWithBusy in the window code-behind) own that state. If we set
            // IsExecuting=true/false in here, the finally block stomps the outer
            // state mid-load and the progress bar disappears partway through.
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

                // short-term intervals default to true for new tickers
                T1 = true,
                T2 = true,
                T3 = true,
                T4 = true,
                T5 = true,
                T8 = true,
                V2 = true,
                V3 = true,
                N2 = true,
                N3 = true,
                N4 = true,

                y1 = false,
                y2 = false,
                y3 = false,
                V4 = false,
                V6 = false,
                V8 = false,
                y4 = false,
                y6 = false,
                y8 = false,
                y12 = false,
                y24 = false,
                y32 = false,
                y72 = false,
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

                // cont_upd defaults to false for new tickers
                ContUpd = false,
                // min_intvl defaults to "default"
                MinIntvl = "default",
                PosIntvl = "default",

                PositionT1 = 0f,
                PositionT2 = 0f,
                PositionT3 = 0f,
                PositionT4 = 0f,
                PositionT5 = 0f,
                PositionT8 = 0f,
                PositionV2 = 0f,
                PositionV3 = 0f,
                PositionN2 = 0f,
                PositionN3 = 0f,
                PositionN4 = 0f,
                PositionY1 = 0f,
                PositionY2 = 0f,
                PositionY3 = 0f,
                PositionV4 = 0f,
                PositionV6 = 0f,
                PositionV8 = 0f,
                PositionY4 = 0f,
                PositionY6 = 0f,
                PositionY8 = 0f,
                PositionY12 = 0f,
                PositionY24 = 0f,
                PositionY32 = 0f,
                PositionY72 = 0f,
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
