using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Newtonsoft.Json.Linq;

namespace wpfTDX
{
    /// <summary>
    /// Live RTU per-ticker status. Polls the tapi /ticker_process_status route
    /// (populated by run_ticker_update via trading.util.ticker_status) and shows
    /// each ticker's queued/running/done/failed state, timings and last-processed
    /// time. Read-only; reads via the API, not direct SQL (per repo convention).
    /// </summary>
    public partial class winTickerStatusMonitor : Window
    {
        private readonly string _apiKey = ConfigurationManager.AppSettings["TradingApiKey"];
        private readonly string _baseUrl = ConfigurationManager.AppSettings["TradingApiBaseUrl"];

        private readonly ObservableCollection<TickerProcessStatusModel> _rows =
            new ObservableCollection<TickerProcessStatusModel>();

        // Full unfiltered fetch; _rows is this filtered by the Show dropdown.
        private List<TickerProcessStatusModel> _all = new List<TickerProcessStatusModel>();

        private readonly DispatcherTimer _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(10)
        };

        private bool _loaded;

        public winTickerStatusMonitor()
        {
            InitializeComponent();
            grid.ItemsSource = _rows;
            _timer.Tick += async (_, __) => await LoadAsync();
            Loaded += async (_, __) => { _loaded = true; await LoadAsync(); };
            Closed += (_, __) => _timer.Stop();
        }

        private string SelectedStatus()
        {
            var item = cboStatus.SelectedItem as System.Windows.Controls.ComboBoxItem;
            var val = item?.Content?.ToString();
            return (string.IsNullOrEmpty(val) || val == "(all)") ? null : val;
        }

        private async Task LoadAsync()
        {
            try
            {
                cmdRefresh.IsEnabled = false;
                txtStatus.Text = "Loading…";

                JObject root;
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);
                    // no process_type filter — pull all tickers for this env; the
                    // Show dropdown filters by status client-side.
                    var content = new StringContent("{}", Encoding.UTF8, "application/json");
                    var resp = await client.PostAsync($"{_baseUrl}/ticker_process_status", content);
                    resp.EnsureSuccessStatusCode();
                    root = JObject.Parse(await resp.Content.ReadAsStringAsync());
                }

                var tickers = root["tickers"] as JObject;
                var models = new List<TickerProcessStatusModel>();
                if (tickers != null)
                {
                    foreach (var prop in tickers.Properties())
                    {
                        var m = prop.Value.ToObject<TickerProcessStatusModel>();
                        if (m != null)
                        {
                            m.Tickername = prop.Name;
                            models.Add(m);
                        }
                    }
                }

                _all = models;
                txtSummary.Text = BuildSummary(root);
                ApplyFilter();
                txtStatus.Text = $"Updated {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                txtStatus.Text = "Error: " + ex.Message;
            }
            finally
            {
                cmdRefresh.IsEnabled = true;
            }
        }

        /// <summary>Re-populate the grid from _all, filtered by the Show dropdown. No re-fetch.</summary>
        private void ApplyFilter()
        {
            var want = SelectedStatus();
            var view = _all
                .Where(m => want == null || string.Equals(m.Status, want, StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => StatusRank(m.Status))
                .ThenBy(m => m.Tickername, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _rows.Clear();
            foreach (var m in view) _rows.Add(m);
        }

        private static int StatusRank(string status)
        {
            switch (status)
            {
                case "running": return 0;
                case "queued": return 1;
                case "failed": return 2;
                case "timeout": return 3;
                case "done": return 4;
                default: return 5;
            }
        }

        private static string BuildSummary(JObject root)
        {
            var sb = new StringBuilder();
            var env = root["env"]?.ToString();
            var pt = root["process_type"]?.ToString();
            sb.Append($"env={env}");
            if (!string.IsNullOrEmpty(pt)) sb.Append($"  type={pt}");

            var counts = root["counts"] as JObject;
            if (counts != null && counts.HasValues)
            {
                var parts = counts.Properties().Select(p => $"{p.Name}={p.Value}");
                sb.Append("   " + string.Join("  ", parts));
            }
            return sb.ToString();
        }

        private async void cmdRefresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

        private void cboStatus_SelectionChanged(object sender,
            System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // filter the already-fetched data; no server round-trip needed
            if (_loaded) ApplyFilter();
        }

        private void chkAuto_Changed(object sender, RoutedEventArgs e)
        {
            if (chkAuto.IsChecked == true) _timer.Start();
            else _timer.Stop();
        }
    }
}
