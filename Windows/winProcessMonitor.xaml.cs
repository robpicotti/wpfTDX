using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Data.SqlClient;
using LiveCharts;
using LiveCharts.Wpf;
using Newtonsoft.Json.Linq;

namespace wpfTDX
{
    public partial class winProcessMonitor : Window
    {
        private readonly SqlConnection _conn;
        private readonly ProcessMonitorViewModel _vm;

        private readonly string _apiKey  = ConfigurationManager.AppSettings["TradingApiKey"];
        private readonly string _baseUrl = ConfigurationManager.AppSettings["TradingApiBaseUrl"];

        // Colour palette — cycles if more than 4 processes
        private static readonly Brush[] SeriesBrushes =
        {
            Brushes.DodgerBlue,
            Brushes.LimeGreen,
            Brushes.Orange,
            Brushes.Magenta,
            Brushes.Cyan,
            Brushes.Yellow,
        };

        public winProcessMonitor(SqlConnection conn)
        {
            InitializeComponent();
            _conn = conn;
            _vm   = new ProcessMonitorViewModel(conn);

            dpFrom.SelectedDate = _vm.DateFrom;
            dpTo.SelectedDate   = _vm.DateTo;

            Loaded += async (_, __) => await LoadAndBuild();
        }

        private async Task<string> GetHostEnvironmentAsync()
        {
            try
            {
                string url = $"{_baseUrl}/get_hostenv?apiKey={_apiKey}";
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);
                    var response = await client.PostAsync(url, null);
                    response.EnsureSuccessStatusCode();
                    string json = await response.Content.ReadAsStringAsync();
                    return JObject.Parse(json)["hostenv"]?.ToString() ?? "";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error getting host environment: " + ex.Message,
                    "Process Monitor", MessageBoxButton.OK, MessageBoxImage.Warning);
                return "";
            }
        }

        private async void cmdRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadAndBuild();
        }

        private void cmdDiagnose_Click(object sender, RoutedEventArgs e)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Date range: {_vm.DateFrom:yyyy-MM-dd} to {_vm.DateTo:yyyy-MM-dd}");
            sb.AppendLine();

            if (_vm.MonitoredProcesses.Count == 0)
            {
                sb.AppendLine("No monitored processes loaded yet — click Refresh first.");
            }
            else
            {
                foreach (var name in _vm.MonitoredProcesses)
                    sb.AppendLine(_vm.DiagnoseAsync(name));
            }

            MessageBox.Show(sb.ToString(), "Process Monitor — Diagnostics",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async System.Threading.Tasks.Task LoadAndBuild()
        {
            try
            {
                _vm.DateFrom = dpFrom.SelectedDate ?? _vm.DateFrom;
                _vm.DateTo   = dpTo.SelectedDate   ?? _vm.DateTo;

                txtStatus.Text       = "Loading…";
                cmdRefresh.IsEnabled = false;

                string hostEnv = await GetHostEnvironmentAsync();
                await _vm.LoadDataAsync(hostEnv);

                BuildAllCharts();

                int total = 0;
                foreach (var name in _vm.MonitoredProcesses)
                    total += _vm.ProcessData.ContainsKey(name) ? _vm.ProcessData[name].Count : 0;

                txtStatus.Text = $"Loaded {DateTime.Now:HH:mm:ss} — {_vm.MonitoredProcesses.Count} process(es), {total} run(s) total";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Process Monitor", MessageBoxButton.OK, MessageBoxImage.Error);
                txtStatus.Text = "Error loading data.";
            }
            finally
            {
                cmdRefresh.IsEnabled = true;
            }
        }

        private void BuildAllCharts()
        {
            chartsContainer.Children.Clear();
            chartsContainer.RowDefinitions.Clear();

            int colourIdx = 0;
            int rowIdx    = 0;
            foreach (var name in _vm.MonitoredProcesses)
            {
                var data  = _vm.ProcessData.ContainsKey(name)
                    ? _vm.ProcessData[name]
                    : new ObservableCollection<ProcessRunDataModel>();
                var brush = SeriesBrushes[colourIdx % SeriesBrushes.Length];
                colourIdx++;

                bool hasAvg = false;
                foreach (var r in data) if (r.AvgPerTicker.HasValue) { hasAvg = true; break; }

                // Add a * row so all processes share available height equally
                chartsContainer.RowDefinitions.Add(
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                // Inner 2-column grid: duration left, avg/ticker right (or empty placeholder)
                var row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var durationPanel = WrapChart(name + " — Duration", BuildDurationChart(data, brush));
                Grid.SetColumn(durationPanel, 0);
                row.Children.Add(durationPanel);

                UIElement rightPanel = hasAvg
                    ? (UIElement)WrapChart(name + " — Avg sec/ticker", BuildAvgTickerChart(data, brush))
                    : new Border();
                Grid.SetColumn(rightPanel, 1);
                row.Children.Add(rightPanel);

                Grid.SetRow(row, rowIdx++);
                chartsContainer.Children.Add(row);
            }
        }

        private static UIElement WrapChart(string title, CartesianChart chart)
        {
            var titleBlock = new TextBlock
            {
                Text       = title,
                FontSize   = 13,
                FontWeight = FontWeights.SemiBold,
                Margin     = new Thickness(6, 4, 0, 2)
            };

            var inner = new Grid();
            inner.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            inner.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(titleBlock, 0);
            Grid.SetRow(chart, 1);
            inner.Children.Add(titleBlock);
            inner.Children.Add(chart);

            return new Border
            {
                BorderBrush     = Brushes.Black,
                BorderThickness = new Thickness(1),
                Margin          = new Thickness(2),
                Child           = inner
            };
        }

        private CartesianChart BuildDurationChart(
            ObservableCollection<ProcessRunDataModel> data,
            Brush seriesColor)
        {
            var chart = CreateBaseChart();
            var source = new System.Collections.Generic.List<ProcessRunDataModel>(data);

            var labels = source.ConvertAll(x =>
                x.StartTime.ToString("dd-MMM-yy") + Environment.NewLine + x.StartTime.ToString("HH:mm"));

            var axisX = BuildAxisX(labels, source.Count);
            var axisY = new Axis
            {
                Title          = "Duration (min)",
                FontSize       = 11,
                Foreground     = Brushes.Black,
                Sections       = new SectionsCollection(),
                LabelFormatter = val => val.ToString("N2")
            };

            chart.AxisX.Add(axisX);
            chart.AxisY.Add(axisY);

            if (source.Count == 0) { axisY.MinValue = 0; axisY.MaxValue = 1; return chart; }

            var values = new ChartValues<double>(source.ConvertAll(r => r.DurationMinutes));

            chart.Series.Add(new LineSeries
            {
                Title             = "Duration (min)",
                Values            = values,
                PointGeometry     = DefaultGeometries.Circle,
                PointGeometrySize = 6,
                Stroke            = seriesColor,
                StrokeThickness   = 2,
                LineSmoothness    = 0,
                Fill              = Brushes.Transparent,
                LabelPoint        = lp =>
                {
                    int idx = (int)Math.Round(lp.X);
                    if (idx < 0 || idx >= source.Count) return $"{lp.Y:N2} min";
                    var r = source[idx];
                    return $"{lp.Y:N2} min" +
                           $"\nStart:  {r.StartTime:dd-MMM-yy HH:mm:ss}" +
                           $"\nEnd:    {r.EndTime:dd-MMM-yy HH:mm:ss}" +
                           $"\nPID:    {r.ProcessId}";
                }
            });

            double avg = 0;
            foreach (var r in source) avg += r.DurationMinutes;
            avg /= source.Count;
            axisY.Sections.Add(new AxisSection
            {
                Value           = avg,
                Stroke          = Brushes.Red,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 6, 3 },
                Label           = $"Avg {avg:N2}"
            });

            ConfigureAxisY(axisY, new System.Collections.Generic.List<double>(values));
            return chart;
        }

        private CartesianChart BuildAvgTickerChart(
            ObservableCollection<ProcessRunDataModel> data,
            Brush seriesColor)
        {
            var chart = CreateBaseChart();
            var source = new System.Collections.Generic.List<ProcessRunDataModel>();
            foreach (var r in data) if (r.AvgPerTicker.HasValue) source.Add(r);

            var labels = source.ConvertAll(x =>
                x.StartTime.ToString("dd-MMM-yy") + Environment.NewLine + x.StartTime.ToString("HH:mm"));

            var axisX = BuildAxisX(labels, source.Count);
            var axisY = new Axis
            {
                Title          = "Avg sec/ticker",
                FontSize       = 11,
                Foreground     = Brushes.Black,
                Sections       = new SectionsCollection(),
                LabelFormatter = val => val.ToString("N1")
            };

            chart.AxisX.Add(axisX);
            chart.AxisY.Add(axisY);

            if (source.Count == 0) { axisY.MinValue = 0; axisY.MaxValue = 1; return chart; }

            var values = new ChartValues<double>(source.ConvertAll(r => r.AvgPerTicker.Value));

            chart.Series.Add(new LineSeries
            {
                Title             = "Avg sec/ticker",
                Values            = values,
                PointGeometry     = DefaultGeometries.Diamond,
                PointGeometrySize = 6,
                Stroke            = seriesColor,
                StrokeThickness   = 2,
                LineSmoothness    = 0,
                Fill              = Brushes.Transparent,
                LabelPoint        = lp =>
                {
                    int idx = (int)Math.Round(lp.X);
                    if (idx < 0 || idx >= source.Count) return $"{lp.Y:N1} s/ticker";
                    var r = source[idx];
                    return $"{lp.Y:N1} s/ticker" +
                           $"\nStart:  {r.StartTime:dd-MMM-yy HH:mm:ss}" +
                           $"\nPID:    {r.ProcessId}";
                }
            });

            double avg = 0;
            foreach (var r in source) avg += r.AvgPerTicker.Value;
            avg /= source.Count;
            axisY.Sections.Add(new AxisSection
            {
                Value           = avg,
                Stroke          = Brushes.Red,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 6, 3 },
                Label           = $"Avg {avg:N1}"
            });

            ConfigureAxisY(axisY, new System.Collections.Generic.List<double>(values));
            return chart;
        }

        private static CartesianChart CreateBaseChart()
        {
            return new CartesianChart
            {
                Series           = new SeriesCollection(),
                AxisX            = new AxesCollection(),
                AxisY            = new AxesCollection(),
                DisableAnimations = true,
                AnimationsSpeed   = TimeSpan.Zero,
                Hoverable         = true,
                Margin            = new Thickness(4),
                DataTooltip       = new DefaultTooltip
                {
                    SelectionMode = TooltipSelectionMode.OnlySender,
                    Background    = Brushes.White
                }
            };
        }

        private static Axis BuildAxisX(System.Collections.Generic.List<string> labels, int count)
        {
            return new Axis
            {
                Title          = "Run Start",
                Labels         = labels,
                LabelsRotation = 0,
                FontSize       = 11,
                Foreground     = Brushes.Black,
                Separator      = new LiveCharts.Wpf.Separator
                {
                    Step            = Math.Max(1, count / 10),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 2, 2 },
                    Stroke          = Brushes.LightGray
                }
            };
        }

        private static void ConfigureAxisY(Axis axis, System.Collections.Generic.List<double> values)
        {
            if (values.Count == 0) { axis.MinValue = 0; axis.MaxValue = 1; return; }

            double minVal  = double.MaxValue, maxVal = double.MinValue;
            foreach (var v in values) { if (v < minVal) minVal = v; if (v > maxVal) maxVal = v; }

            double padding = (maxVal - minVal) * 0.15;
            if (padding < 0.01) padding = 0.5;

            double axisMin = Math.Max(0, minVal - padding);
            double axisMax = maxVal + padding;
            axis.MinValue = axisMin;
            axis.MaxValue = axisMax;

            double range = axisMax - axisMin;
            axis.Separator = new LiveCharts.Wpf.Separator
            {
                Step            = CalculateStep(range),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 2, 2 },
                Stroke          = Brushes.LightGray
            };
        }

        private static double CalculateStep(double range)
        {
            if (range <= 0) return 1;
            double step = Math.Pow(10, Math.Floor(Math.Log10(range)));
            if (range / step > 10) step *= 2;
            if (range / step > 10) step *= 5;
            return step;
        }
    }
}
