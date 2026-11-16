using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using Separator = LiveCharts.Wpf.Separator;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace wpfTDX
{
    /// <summary>Read-only fund dashboard. All observations and limits come through TAPI.</summary>
    public partial class winFundRisk : Window
    {
        private readonly HttpClient _client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        private readonly DispatcherTimer _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
        private readonly Dictionary<string, DateTime> _latestDates = new Dictionary<string, DateTime>();
        private List<JObject> _rows = new List<JObject>();
        private bool _ready, _busy, _closed;
        private readonly string _baseUrl = ConfigurationManager.AppSettings["TradingApiBaseUrl"];
        private static readonly Brush Ink = BrushOf("#17253D");
        private static readonly Brush Blue = BrushOf("#3168C6");
        private static readonly Brush Red = BrushOf("#C64E59");
        private static readonly Brush Muted = BrushOf("#607089");
        private bool Intraday => ModePicker.SelectedIndex == 1;

        public winFundRisk()
        {
            InitializeComponent();
            _client.DefaultRequestHeaders.Add("X-Api-Key", ConfigurationManager.AppSettings["TradingApiKey"]);
            Loaded += async (_, __) => await InitialiseAsync();
            _timer.Tick += async (_, __) => { if (AutoRefresh.IsChecked == true) await LoadAsync(); };
            Closed += (_, __) => { _closed = true; _timer.Stop(); _client.Dispose(); };
        }

        private static Brush BrushOf(string hex) => (Brush)new BrushConverter().ConvertFromString(hex);
        private static double? Number(JObject row, string key)
        {
            double value;
            return row != null && double.TryParse(row[key]?.ToString(), NumberStyles.Float,
                CultureInfo.InvariantCulture, out value) && !double.IsNaN(value) && !double.IsInfinity(value)
                ? (double?)value : null;
        }
        private static string Format(double? value, string format = "P2") => value.HasValue
            ? value.Value.ToString(format, CultureInfo.InvariantCulture) : "Unavailable";
        private static DateTime? Timestamp(JObject row, string key)
        {
            var token = row?[key];
            // Json.NET parses ISO strings into DateTime tokens. Reading those through
            // ToString() applies the Windows culture before invariant parsing, which
            // rejects UK days above 12 and swaps month/day for the remaining dates.
            if (token?.Type == JTokenType.Date) return token.Value<DateTime>();
            DateTime value;
            // Stored timestamps are UTC-naive; do not convert valuation-date labels to local dates.
            return DateTime.TryParse(token?.ToString(), CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out value) ? (DateTime?)value : null;
        }

        private async Task<JObject> PostAsync(string route, JObject payload)
        {
            using (var body = new StringContent(payload.ToString(), Encoding.UTF8, "application/json"))
            using (var response = await _client.PostAsync(_baseUrl.TrimEnd('/') + route, body))
            {
                string text = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    string error = null;
                    try { error = JObject.Parse(text)["error"]?.ToString(); } catch { }
                    throw new InvalidOperationException(error ?? $"API returned {(int)response.StatusCode}. Check the TAPI service.");
                }
                return JObject.Parse(text);
            }
        }

        private async Task InitialiseAsync()
        {
            try
            {
                Filters.IsEnabled = false;
                var result = await PostAsync("/fund_risk_funds", new JObject());
                var environment = await PostAsync("/get_hostenv", new JObject());
                if (_closed) return;
                _latestDates.Clear();
                foreach (JObject fund in (JArray)result["funds"])
                {
                    var date = Timestamp(fund, "latest_date");
                    if (date.HasValue) _latestDates[fund["fundname"].ToString()] = date.Value.Date;
                }
                FundPicker.ItemsSource = _latestDates.Keys.ToList();
                var preferredFund = string.Equals(environment["hostenv"]?.ToString(), "prod", StringComparison.OrdinalIgnoreCase)
                    ? _latestDates.Keys.FirstOrDefault(f => string.Equals(f, "TenDSelect", StringComparison.OrdinalIgnoreCase)) : null;
                if (preferredFund != null) FundPicker.SelectedItem = preferredFund;
                else FundPicker.SelectedIndex = 0;
                SetFundDates();
                _ready = true;
                _timer.Start();
            }
            catch (Exception ex) { Status.Text = "Unable to load funds: " + ex.Message + " Use Refresh to retry."; }
            finally { Filters.IsEnabled = true; }
            if (_ready) await LoadAsync();
        }

        private void SetFundDates()
        {
            var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time")).Date;
            ToPicker.SelectedDate = today;
            FromPicker.SelectedDate = new DateTime(today.Year, today.Month, 1);
        }
        private async void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_ready || _busy) return;
            _ready = false;
            FromPicker.Visibility = FromLabel.Visibility = Intraday ? Visibility.Collapsed : Visibility.Visible;
            _ready = true;
            await LoadAsync();
        }
        private async void DateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_ready && !_busy) await LoadAsync();
        }
        private async void RefreshClick(object sender, RoutedEventArgs e)
        {
            if (!_ready) await InitialiseAsync(); else await LoadAsync();
        }

        private async Task LoadAsync()
        {
            if (_busy || !_ready || _closed) return;
            if (FundPicker.SelectedItem == null) { Status.Text = "No funds have dated risk observations."; Render(); return; }
            _busy = true;
            Filters.IsEnabled = false;
            _rows.Clear();
            Render();
            Status.Text = "Loading recorded observations...";
            try
            {
                if (!ToPicker.SelectedDate.HasValue || (!Intraday && !FromPicker.SelectedDate.HasValue))
                    throw new InvalidOperationException("Select the date range.");
                var payload = new JObject {
                    ["fundname"] = FundPicker.SelectedItem.ToString(),
                    ["mode"] = Intraday ? "intraday" : "daily",
                    ["from_date"] = (Intraday ? ToPicker.SelectedDate.Value : FromPicker.SelectedDate.Value).ToString("yyyy-MM-dd"),
                    ["to_date"] = ToPicker.SelectedDate.Value.ToString("yyyy-MM-dd")
                };
                var result = await PostAsync("/fund_risk_history", payload);
                if (_closed) return;
                _rows = ((JArray)result["rows"]).OfType<JObject>().ToList();
                Render();
                if (_rows.Count == 0) { Status.Text = "No risk observations in this period. Try another date range."; return; }
                var last = _rows.Last();
                var runtime = Timestamp(last, "runtime");
                var observed = Timestamp(last, "Dates");
                var tradingDate = Timestamp(last, "valuationDate");
                var age = runtime.HasValue ? Math.Max(0, (DateTime.UtcNow - DateTime.SpecifyKind(runtime.Value, DateTimeKind.Utc)).TotalMinutes) : (double?)null;
                Status.Text = $"{_rows.Count} observations | Latest displayed: {observed:yyyy-MM-dd HH:mm} UTC | Runtime: {runtime:yyyy-MM-dd HH:mm} UTC | Age: {Format(age, "N0")} min. " +
                    (tradingDate.HasValue && tradingDate.Value.Date >= DateTime.UtcNow.Date ? "Current trading date is provisional. " : "") +
                    "Snapshots are recorded conditionally; refresh does not recalculate risk.";
            }
            catch (Exception ex) { if (!_closed) Status.Text = "Unable to load risk: " + ex.Message; }
            finally { _busy = false; if (!_closed) Filters.IsEnabled = true; }
        }

        private void AddCard(string title, double? value, double? limit, string format = "P2", string extra = null, string amount = null)
        {
            double? usage = value.HasValue && limit.HasValue && Math.Abs(limit.Value) > 0
                ? Math.Abs(value.Value) / Math.Abs(limit.Value) : (double?)null;
            var color = usage >= 1 ? Red : usage >= .8 ? BrushOf("#AE761A") : Ink;
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = title, Foreground = Muted, FontSize = 12 });
            panel.Children.Add(new TextBlock { Text = Format(value, format), Foreground = color, FontSize = 28, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 6) });
            if (amount != null)
                panel.Children.Add(new TextBlock { Text = amount, Foreground = Ink, FontSize = 16,
                    FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6),
                    ToolTip = "Amount in fund accounting currency, calculated using prior-day MTM." });
            panel.Children.Add(new TextBlock { Text = limit.HasValue ? "Limit " + Format(limit, format) : extra ?? "Limit unavailable", Foreground = Muted });
            if (usage.HasValue)
            {
                panel.Children.Add(new ProgressBar { Minimum = 0, Maximum = 1, Value = Math.Min(1, usage.Value), Height = 4, Foreground = color, Margin = new Thickness(0, 10, 0, 5) });
                panel.Children.Add(new TextBlock { Text = Format(usage, "P0") + " of limit" + (usage >= 1 ? " — at / above limit" : ""), Foreground = color, FontSize = 11 });
            }
            Cards.Children.Add(new Border { Background = Brushes.White, CornerRadius = new CornerRadius(6), Padding = new Thickness(15), Margin = new Thickness(5), Child = panel });
        }

        private void Render()
        {
            Cards.Children.Clear(); Charts.Children.Clear(); CategoryChart.Content = null;
            var last = _rows.LastOrDefault();
            AddCard("PORTFOLIO VAR", Number(last, "var"), Number(last, "var_limit"),
                amount: Format(Number(last, "var") * Number(last, "mtm_day1"), "N0") + " (fund ccy)");
            AddCard("STRESS LOSS", Number(last, "stress"), Number(last, "stress_limit"),
                amount: Format(Number(last, "stress") * Number(last, "mtm_day1"), "N0") + " (fund ccy)");
            AddCard("DRAWDOWN", Number(last, "dd_return_curday"), Number(last, "dd_limit"));
            AddCard("GROSS LEVERAGE", Number(last, "leverage_total"), Number(last, "leverage_limit_total"), "0.00'x'");
            AddCard("DAILY P&L", Number(last, "pnl_curday"), null, "N0", "Daily return " + Format(Number(last, "return_curday")));
            PerformanceStrip.Text = "Latest displayed snapshot  |  MTM " + Format(Number(last, "mtm"), "N0") +
                "  |  Cumulative return " + Format(Number(last, "cum_return_curday")) +
                "  |  VaR target " + Format(Number(last, "var_target")) +
                "  |  Monetary values in fund accounting units";
            AddChart("Portfolio risk", "VaR and stress loss as % of prior-day MTM", "P1",
                new[] { "var", "stress", "var_limit", "stress_limit", "var_target" },
                new[] { "VaR", "Stress", "VaR limit", "Stress limit", "VaR target" });
            AddChart("Cumulative return", "Recorded cumulative return", "P1",
                new[] { "cum_return_curday" }, new[] { "Cumulative return" });
            AddChart("Drawdown", "Recorded drawdown and historical limit", "P1",
                new[] { "dd_return_curday", "dd_limit" }, new[] { "Drawdown", "Drawdown limit" }, drawdown: true);
            AddChart(Intraday ? "Running day P&L" : "Daily P&L", "Capital-movement-adjusted P&L in fund accounting units", "N0",
                new[] { "pnl_curday" }, new[] { "P&L" }, bars: !Intraday);
            RenderCategories(last);
            Details.Text = "Latest displayed snapshot\n" +
                "Liquidity (position-limit / daily-volume usage): average " + Format(Number(last, "avg_liquidity")) +
                ", maximum " + Format(Number(last, "max_liquidity")) + ", limit " + Format(Number(last, "liquidity_limit")) + "\n" +
                "Equity / futures leverage: " + Format(Number(last, "leverage_equities"), "0.00'x'") + " / " + Format(Number(last, "leverage_futures"), "0.00'x'") +
                "; limits " + Format(Number(last, "leverage_limit_equities"), "0.00'x'") + " / " + Format(Number(last, "leverage_limit_futures"), "0.00'x'") + "\n" +
                "Max equity / futures concentration: " + Format(Number(last, "max_notional_equities")) + " / " + Format(Number(last, "max_notional_futures")) +
                "; limits " + Format(Number(last, "max_notional_limit_equities")) + " / " + Format(Number(last, "max_notional_limit_futures")) + "\n" +
                "Allocated risk " + Format(Number(last, "var_allocated")) + "; limit " + Format(Number(last, "var_allocated_limit")) + "\n" +
                "MTM change since prior day " + Format(Number(last, "mtm") - Number(last, "mtm_day1"), "N0") +
                "; net capital movement " + Format(Number(last, "notional") - Number(last, "notional_day1"), "N0") +
                ". Missing values remain unavailable. Amber indicates 80% of a stored limit; red indicates at / above limit.";
            var table = new DataTable();
            var keys = _rows.SelectMany(r => r.Properties().Select(p => p.Name)).Distinct().ToList();
            foreach (var key in keys) table.Columns.Add(key, typeof(string));
            foreach (var row in _rows) table.Rows.Add(keys.Select(k => (object)(row[k]?.ToString() ?? "")).ToArray());
            Observations.ItemsSource = table.DefaultView;
        }

        // A weekday-only calendar retains missing weekdays while closing weekend gaps.
        private static double WeekdayIndex(DateTime date)
        {
            int days = (date.Date - new DateTime(2000, 1, 3)).Days; // Monday
            int weeks = (int)Math.Floor(days / 7.0);
            return weeks * 5 + Math.Min(days - weeks * 7, 4);
        }

        private static DateTime WeekdayDate(double index)
        {
            int days = (int)Math.Round(index);
            int weeks = (int)Math.Floor(days / 5.0);
            return new DateTime(2000, 1, 3).AddDays(weeks * 7 + days - weeks * 5);
        }

        private void AddChart(string title, string subtitle, string format, string[] keys, string[] names, bool drawdown = false, bool bars = false)
        {
            var chart = new CartesianChart { Height = 230, DisableAnimations = true,
                LegendLocation = bars ? LegendLocation.None : LegendLocation.Bottom, Hoverable = true };
            var colors = new[] { Blue, Red, Blue, Red, Muted };
            var series = new SeriesCollection();
            var finiteY = new List<double>();
            var finiteX = new List<double>();
            DateTime origin = ToPicker.SelectedDate ?? Timestamp(_rows.FirstOrDefault(), "Dates")?.Date ?? DateTime.UtcNow.Date;
            for (int n = 0; n < keys.Length; n++)
            {
                var values = new ChartValues<ObservablePoint>();
                bool hasValues = false;
                foreach (var row in _rows)
                {
                    var when = Timestamp(row, Intraday ? "Dates" : "valuationDate");
                    var value = Number(row, keys[n]);
                    if (!when.HasValue) continue;
                    if (!Intraday && (when.Value.DayOfWeek == DayOfWeek.Saturday || when.Value.DayOfWeek == DayOfWeek.Sunday)) continue;
                    if (drawdown && keys[n] == "dd_limit" && value.HasValue) value = -Math.Abs(value.Value);
                    // NaN creates a visible gap rather than presenting missing risk as zero.
                    double x = Intraday ? (when.Value - origin).TotalHours : WeekdayIndex(when.Value);
                    values.Add(new ObservablePoint(x, value ?? double.NaN));
                    if (value.HasValue) { hasValues = true; finiteY.Add(value.Value); finiteX.Add(x); }
                }
                if (!hasValues) continue;
                if (bars)
                {
                    series.Add(new ColumnSeries { Title = names[n], Values = values, Fill = Blue, MaxColumnWidth = 22 });
                    // Zero-height bars and one-day bars across sparse histories can
                    // disappear. Mark every actual observation without filling gaps.
                    series.Add(new ScatterSeries { Title = "Recorded P&L",
                        Values = new ChartValues<ScatterPoint>(values.Where(p => !double.IsNaN(p.Y))
                            .Select(p => new ScatterPoint(p.X, p.Y, 1))),
                        Fill = Blue, Stroke = Blue, MinPointShapeDiameter = 5, MaxPointShapeDiameter = 5 });
                }
                else series.Add(new LineSeries { Title = names[n], Values = values, Stroke = drawdown ? Red : colors[n],
                    Fill = Brushes.Transparent, PointGeometrySize = values.Count < 3 || values.Any(p => double.IsNaN(p.Y)) ? 7 : 0, StrokeThickness = 2,
                    LineSmoothness = 0, StrokeDashArray = keys[n].Contains("limit") || keys[n].Contains("target") ? new DoubleCollection { 4, 3 } : null });
            }
            chart.Series = series;
            double unit = Intraday ? 1.0 / 60 : 1;
            double xPadding = bars && finiteX.Count > 0
                ? Math.Max(unit / 2, (finiteX.Max() - finiteX.Min()) * .02) : unit / 2;
            var dateAxis = new Axis { Unit = unit, BarUnit = unit, LabelsRotation = -30, FontSize = 11,
                MinValue = finiteX.Count == 0 ? 0 : finiteX.Min() - xPadding,
                MaxValue = finiteX.Count == 0 ? 1 : finiteX.Max() + xPadding,
                LabelFormatter = x => Intraday ? origin.AddMinutes(Math.Round(x * 60)).ToString("HH:mm") : WeekdayDate(x).ToString("dd MMM"),
                Separator = new Separator { IsEnabled = false }, Foreground = Muted };
            chart.AxisX.Add(dateAxis);
            bool intradayAxis = Intraday;
            Action updateDateSpacing = () =>
            {
                double span = dateAxis.MaxValue - dateAxis.MinValue;
                double labels = Math.Max(4, Math.Min(14, Math.Floor(chart.ActualWidth / 65)));
                // Keep roughly one date per 65 pixels, with whole-day/minute steps.
                dateAxis.Separator.Step = intradayAxis
                    ? Math.Max(1, Math.Ceiling(span * 60 / labels)) / 60
                    : Math.Max(1, Math.Ceiling(span / labels));
            };
            chart.SizeChanged += (_, __) => updateDateSpacing();
            updateDateSpacing();
            double lower = finiteY.Count == 0 ? 0 : Math.Min(0, finiteY.Min());
            double upper = finiteY.Count == 0 ? 0 : Math.Max(0, finiteY.Max());
            if (bars && lower == 0 && upper == 0) { lower = -1; upper = 1; }
            else if (lower == upper) upper = format == "N0" ? 1 : .01;
            var valueAxis = new Axis { MinValue = lower < 0 ? lower * 1.08 : 0,
                MaxValue = upper > 0 ? upper * 1.08 : 0,
                LabelFormatter = y => y.ToString(format, CultureInfo.InvariantCulture), Foreground = Muted };
            if (keys.Contains("pnl_curday"))
                valueAxis.Sections.Add(new AxisSection { Value = 0, SectionWidth = 0,
                    Stroke = Ink, StrokeThickness = 1.5, Fill = Brushes.Transparent });
            chart.AxisY.Add(valueAxis);
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = title, FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = Ink });
            panel.Children.Add(new TextBlock { Text = subtitle, Foreground = Muted, Margin = new Thickness(0, 4, 0, 8) });
            if (finiteY.Count > 0) panel.Children.Add(chart);
            else panel.Children.Add(new TextBlock { Text = "No recorded values in this period", Height = 230,
                Foreground = Muted, VerticalAlignment = VerticalAlignment.Center });
            Charts.Children.Add(new Border { Background = Brushes.White, CornerRadius = new CornerRadius(6), Padding = new Thickness(14), Margin = new Thickness(5), Child = panel });
        }

        private void RenderCategories(JObject last)
        {
            var categories = new[] { "ag", "chemicals", "crypto", "energy", "euenergy", "freight", "fx", "industry", "indices", "metal", "rates", "stock", "volatility" };
            var available = categories.Where(c => Number(last, "gross_var_" + c).HasValue)
                .OrderBy(c => Number(last, "gross_var_" + c)).ToList();
            var chart = new CartesianChart { DisableAnimations = true, LegendLocation = LegendLocation.None };
            chart.Series = new SeriesCollection { new RowSeries { Title = "Gross risk", Fill = Blue,
                Values = new ChartValues<double>(available.Select(c => Number(last, "gross_var_" + c).Value)), DataLabels = true,
                LabelPoint = p => p.X.ToString("P2", CultureInfo.InvariantCulture) } };
            chart.AxisY.Add(new Axis { Labels = available, MinValue = -.5, MaxValue = Math.Max(.5, available.Count - .5), Separator = new Separator { IsEnabled = false } });
            chart.AxisX.Add(new Axis { LabelFormatter = x => x.ToString("P1", CultureInfo.InvariantCulture), MinValue = 0,
                MaxValue = Math.Max(.001, available.Select(c => Number(last, "gross_var_" + c).Value).DefaultIfEmpty(0).Max() * 1.25) });
            CategoryChart.Content = available.Count == 0 ? null : chart;
            CategoryNote.Text = available.Count == 0 ? "No category risk recorded." :
                $"{available.Count} of {categories.Length} categories recorded; missing categories are omitted. Gross total: {Format(Number(last, "gross_var"))}.";
        }

        private void ExportClick(object sender, RoutedEventArgs e)
        {
            if (_rows.Count == 0) return;
            var dialog = new SaveFileDialog { Filter = "CSV files|*.csv", FileName = "fund-risk.csv" };
            if (dialog.ShowDialog(this) != true) return;
            try
            {
                var keys = _rows.SelectMany(r => r.Properties().Select(p => p.Name)).Distinct().ToList();
                Func<string, string> quote = text => "\"" + (text ?? "").Replace("\"", "\"\"") + "\"";
                var lines = new[] { string.Join(",", keys.Select(quote)) }.Concat(_rows.Select(row =>
                    string.Join(",", keys.Select(k => quote(row[k]?.ToString())))));
                File.WriteAllLines(dialog.FileName, lines, Encoding.UTF8);
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Export failed"); }
        }
    }
}
