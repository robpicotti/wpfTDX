using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using Newtonsoft.Json.Linq;
using wpfTDX;

internal static class FundRiskClientChecks
{
    static int checks;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
    static object Field(object window, string name) { return window.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(window); }
    static void Call(object window, string name) { window.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Invoke(window, null); }
    static void Set(object window, string name, object value) { window.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(window, value); }
    static Task Load(object window) { return (Task)window.GetType().GetMethod("LoadAsync", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(window, null); }
    static void Finish(Task task)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!task.IsCompleted && DateTime.UtcNow < deadline) Pump();
        Check(task.IsCompleted, "Load must finish promptly");
        task.GetAwaiter().GetResult();
    }
    sealed class DelayedApi : HttpMessageHandler
    {
        public bool Missing;
        public int Calls;
        public readonly TaskCompletionSource<bool> Latest = new TaskCompletionSource<bool>();
        public readonly TaskCompletionSource<bool> History = new TaskCompletionSource<bool>();
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            if (request.RequestUri.AbsolutePath == "/fund_risk_history")
                return Reply(HttpStatusCode.OK, "{\"rows\":[{\"valuationDate\":\"2026-11-09\",\"Dates\":\"2026-11-09T21:00:00\",\"var\":0.01,\"mtm_day1\":100000},{\"valuationDate\":\"2026-11-10\",\"Dates\":\"2026-11-10T21:00:00\",\"var\":0.01,\"mtm_day1\":100000}]}");
            Calls++;
            if (Missing) return Reply(HttpStatusCode.NotFound, "{\"error\":\"Not found\"}");
            var body = JObject.Parse(await request.Content.ReadAsStringAsync());
            var gate = body["date"].ToString() == "2026-11-10" ? Latest : History;
            await Task.WhenAny(gate.Task, Task.Delay(Timeout.Infinite, token));
            token.ThrowIfCancellationRequested();
            return Reply(HttpStatusCode.OK, "{\"net_pnl\":125,\"currency\":\"USD\",\"details\":[]}");
        }
        static HttpResponseMessage Reply(HttpStatusCode code, string body)
        { return new HttpResponseMessage(code) { Content = new StringContent(body) }; }
    }
    static winFundRisk LoadingWindow(DelayedApi handler)
    {
        var window = new winFundRisk();
        ((HttpClient)Field(window, "_client")).Dispose();
        Set(window, "_client", new HttpClient(handler));
        var fund = (ComboBox)Field(window, "FundPicker");
        fund.ItemsSource = new[] { "synthetic" }; fund.SelectedIndex = 0;
        ((DatePicker)Field(window, "FromPicker")).SelectedDate = new DateTime(2026, 11, 9);
        ((DatePicker)Field(window, "ToPicker")).SelectedDate = new DateTime(2026, 11, 10);
        Set(window, "_ready", true);
        return window;
    }
    static void LoadingChecks()
    {
        var api = new DelayedApi();
        var window = LoadingWindow(api);
        var task = Load(window);
        Pump();
        Check(!task.IsCompleted, "Delayed MTM must remain pending");
        Check(((List<JObject>)Field(window, "_rows")).Count == 2 && ((Panel)Field(window, "Cards")).Children.Count == 5,
            "Risk cards render before delayed MTM completes");
        Check(((Panel)Field(window, "Filters")).IsEnabled, "Filters remain usable during MTM");
        Check(((TextBlock)Field(window, "Status")).Text.Contains("Risk is ready"), "Progress explains the pending MTM calculation");
        api.Latest.SetResult(true);
        Pump();
        Check(!task.IsCompleted && ((Dictionary<DateTime, JObject>)Field(window, "_pnl")).Count == 1,
            "Latest P&L appears while historical days are still pending");
        api.History.SetResult(true); Finish(task); window.Close();

        api = new DelayedApi { Missing = true }; window = LoadingWindow(api);
        Finish(Load(window));
        Check(api.Calls == 1, "Missing endpoint fails once instead of repeating for all dates");
        Check(((TextBlock)Field(window, "Status")).Text.Contains("Restart TAPI"), "Missing endpoint gives actionable restart message");
        Check(((List<JObject>)Field(window, "_rows")).Count == 2, "Missing endpoint retains recorded risk");
        window.Close();

        api = new DelayedApi(); window = LoadingWindow(api);
        task = Load(window);
        var replacement = Load(window); // Same action as changing a filter or pressing Refresh.
        Finish(task);
        Check(!replacement.IsCompleted && (bool)Field(window, "_busy"), "Cancelled prior load cannot clear the replacement load state");
        window.Close(); Finish(replacement);
        Check(!(bool)Field(window, "_busy"), "Closing cancels pending MTM work");
    }
    static void Pump()
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        timer.Tick += (s, e) => { timer.Stop(); frame.Continue = false; };
        timer.Start();
        Dispatcher.PushFrame(frame);
    }
    static T[] Children<T>(DependencyObject root) where T : DependencyObject
    {
        var found = new List<T>();
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T) found.Add((T)child);
            found.AddRange(Children<T>(child));
        }
        return found.ToArray();
    }
    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-GB");
            var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
            LoadingChecks();
            var window = new winFundRisk();
            var rows = (List<JObject>)Field(window, "_rows");
            var pnl = (Dictionary<DateTime, JObject>)Field(window, "_pnl");
            var categories = new[] { "ag", "chemicals", "crypto", "energy", "euenergy", "freight", "fx", "industry", "indices", "metal", "rates", "stock", "volatility" };
            for (int i = 0; i < 4; i++)
            {
                DateTime day = new DateTime(2026, 11, 9).AddDays(i);
                var row = new JObject { ["valuationDate"] = day, ["Dates"] = day.AddHours(21), ["runtime"] = day.AddHours(21),
                    ["var"] = .01, ["mtm_day1"] = 100000, ["mtm"] = 100500, ["pnl_curday"] = 500,
                    ["cum_return_curday"] = .05 + .001*i, ["dd_return_curday"] = -.01, ["dd_limit"] = .15 };
                for (int c = 0; c < categories.Length; c++) row["gross_var_" + categories[c]] = .001*(c+1);
                rows.Add(row);
                if (i == 2) continue; // An unavailable day must remain a visible gap.
                double amount = i == 0 ? 800 : i == 1 ? -1400 : 0;
                pnl[day] = new JObject { ["net_pnl"] = amount, ["currency"] = "USD", ["buyandhold"] = amount + 5,
                    ["newdeals"] = 0, ["commissions"] = -5, ["date_t"] = day.AddHours(21), ["date_tminus1"] = day.AddDays(-1).AddHours(21),
                    ["details"] = new JArray(new JObject { ["tickername"] = "ES1", ["tad_id"] = "ESZ6", ["buyandhold"] = amount + 5,
                        ["newdeals"] = 0, ["commissions"] = -5, ["net_pnl"] = amount }) };
            }
            Call(window, "Render");
            Check((double)rows[0]["mtm_pnl"] == 800, "Daily chart uses MTM net, not saved NAV estimate");
            Check(rows[2]["mtm_pnl"].Type == JTokenType.Null, "Missing MTM is not zero");
            Check((double)rows[0]["var_upper"] == 1000 && (double)rows[0]["var_lower"] == -1000, "Symmetric currency VaR");
            var host = (ContentControl)Field(window, "PnlChart");
            var border = (Border)host.Content;
            var panel = (StackPanel)border.Child;
            var chart = panel.Children.OfType<CartesianChart>().Single();
            Check(chart.Series.OfType<ColumnSeries>().Count() == 1 && chart.Series.OfType<LineSeries>().Count() == 2, "P&L columns with the positive and negative one-VaR envelope");
            Check(chart.Series.OfType<ColumnSeries>().Single().Values.Cast<ObservablePoint>().Any(p => double.IsNaN(p.Y)), "Missing day keeps chart gap");
            Check(chart.AxisY[0].MinValue < -1400 && chart.AxisY[0].MaxValue > 1000 && chart.AxisY[0].MaxValue < 2000, "Chart fits P&L and one-VaR envelope without two-VaR padding");
            var picker = (ComboBox)Field(window, "PnlDatePicker");
            picker.SelectedIndex = 1;
            var grid = (DataGrid)Field(window, "PnlDetails");
            Check(grid.Items.Count == 1 && (double)((JObject)grid.Items[0])["net_pnl"] == -1400, "Date selector changes ticker detail");
            Check(((TextBlock)Field(window, "PnlNote")).Text.Contains("MTM minus estimate -1,900.00"), "Reconciliation exposes NAV discrepancy");
            picker.SelectedIndex = 2;
            Check(grid.Items.Count == 0 && ((TextBlock)Field(window, "PnlNote")).Text.Contains("No MTM result"), "Missing result clears old detail");
            picker.SelectedIndex = 1;
            var categoryHost = (ContentControl)Field(window, "CategoryChart");
            var category = (CartesianChart)categoryHost.Content;
            Check(category.AxisY[0].Separator.Step == 1 && category.AxisY[0].LabelFormatter(-1) == "" && category.AxisY[0].LabelFormatter(13) == "", "One category label per row without wrapped padding labels");
            Check(category.AxisY[0].LabelFormatter(0) == "Agriculture" && category.AxisY[0].LabelFormatter(4) == "EU energy", "Readable category labels");
            Check(category.Height >= 13 * 29, "Category chart has space for every row");
            // Render chart artifacts without starting the window's API-loading lifecycle.
            host.Content = null; categoryHost.Content = null;
            ((Panel)grid.Parent).Children.Remove(grid);
            foreach (var entry in new[] { Tuple.Create("pnl", (FrameworkElement)border), Tuple.Create("categories", (FrameworkElement)category), Tuple.Create("detail", (FrameworkElement)grid) })
            {
                double height = entry.Item1 == "pnl" ? 400 : entry.Item1 == "detail" ? 285 : category.Height;
                var renderWindow = new Window { Content = entry.Item2, Width = 1320, Height = height,
                    Left = -20000, Top = -20000, ShowInTaskbar = false, ShowActivated = false,
                    WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize, Background = Brushes.White };
                renderWindow.Show();
                for (int i = 0; i < 5; i++) Pump();
                entry.Item2.UpdateLayout();
                var bitmap = new RenderTargetBitmap(1320, (int)height, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(renderWindow);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using (var file = File.Create(Path.Combine(args[0], entry.Item1 + ".png"))) encoder.Save(file);
                if (entry.Item1 == "detail") Check(Children<TextBlock>(grid).Any(t => t.Text == "-1,400.00"), "P&L detail renders numeric amounts to two decimals");
                renderWindow.Content = null; renderWindow.Close();
            }
            // Empty/all-zero and intraday modes must still render safely.
            foreach (var row in rows) { row["var"] = 0; row["mtm_day1"] = 0; }
            foreach (var value in pnl.Values) value["net_pnl"] = 0;
            Call(window, "Render");
            ((ComboBox)Field(window, "ModePicker")).SelectedIndex = 1;
            Call(window, "Render");
            Check(((TextBlock)Field(window, "PnlNote")).Text.Contains("Select Daily"), "Intraday does not imply MTM reconciliation");
            rows.Clear(); Call(window, "Render");
            Check(((DataGrid)Field(window, "PnlDetails")).Items.Count == 0, "Empty history clears details");
            window.Close();
            Console.WriteLine("PASS: " + checks + " Fund Risk client checks.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}

