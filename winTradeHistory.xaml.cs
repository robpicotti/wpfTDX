using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
using System.Windows;
using LiveCharts;
using LiveCharts.Wpf;
using TDX;

namespace wpfTDX
{
    public partial class winTradeHistory : Window
    {
        private readonly SqlConnection gbl_conn;
        private readonly TradeHistoryViewModel _vm;

        // -------------------------------------------------------
        // Edit colours and thickness here
        // -------------------------------------------------------
        private class SeriesStyle
        {
            public System.Windows.Media.Brush Stroke { get; set; }
            public double StrokeThickness { get; set; }
        }

        private static readonly Dictionary<string, SeriesStyle> _seriesStyles =
            new Dictionary<string, SeriesStyle>(StringComparer.OrdinalIgnoreCase)
        {
            { "position_live",       new SeriesStyle { Stroke = System.Windows.Media.Brushes.Black,       StrokeThickness = 6 } },
            { "position_limit",      new SeriesStyle { Stroke = System.Windows.Media.Brushes.Red,         StrokeThickness = 2 } },
            { "position_target",     new SeriesStyle { Stroke = System.Windows.Media.Brushes.LimeGreen,   StrokeThickness = 2 } },
            { "deployment",          new SeriesStyle { Stroke = System.Windows.Media.Brushes.DodgerBlue,  StrokeThickness = 2 } },
            { "deployment_tad",      new SeriesStyle { Stroke = System.Windows.Media.Brushes.DeepSkyBlue, StrokeThickness = 2 } },
            { "position_target_raw", new SeriesStyle { Stroke = System.Windows.Media.Brushes.Orange,      StrokeThickness = 2 } },
            { "scaled_percent",      new SeriesStyle { Stroke = System.Windows.Media.Brushes.Magenta,     StrokeThickness = 2 } },

        };

        private static readonly SeriesStyle _defaultStyle =
            new SeriesStyle { Stroke = System.Windows.Media.Brushes.Gray, StrokeThickness = 2 };
        // -------------------------------------------------------

        private static readonly HashSet<string> _percentMetrics = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "scaled_percent", "deployment", "deployment_tad"
        };

        private static readonly HashSet<string> _limitMetrics = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "position_limit"
        };

        public winTradeHistory(string fundname, string tad_id, string tickername, DateTime positiondate, SqlConnection conn)
        {
            InitializeComponent();

            gbl_conn = conn;
            _vm = new TradeHistoryViewModel(conn);
            DataContext = _vm;

            _vm.FundName = fundname;
            _vm.TadId = tad_id;
            _vm.TickerName = tickername;
            _vm.PositionDateFrom = positiondate;
            _vm.PositionDateTo = positiondate;

            loadForm();
        }

        private async void loadForm()
        {
            try
            {
                txtFundName.Text = _vm.FundName;

                await _vm.LoadTadIdsAsync();

                if (!string.IsNullOrWhiteSpace(_vm.TadId))
                {
                    TadIdItem selected = _vm.TadIds.FirstOrDefault(x =>
                        string.Equals(x.TadId, _vm.TadId, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(x.TickerName, _vm.TickerName, StringComparison.OrdinalIgnoreCase));

                    if (selected == null)
                        selected = _vm.TadIds.FirstOrDefault(x =>
                            string.Equals(x.TadId, _vm.TadId, StringComparison.OrdinalIgnoreCase));

                    if (selected != null)
                    {
                        _vm.SelectedTadItem = selected;
                        cboTadId.SelectedItem = selected;
                    }
                }

                await _vm.LoadAllDataAsync();
                RebuildChart();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Trade history load", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void cmdRun_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _vm.LoadAllDataAsync();
                RebuildChart();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Trade history", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void cboTadId_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (!IsLoaded) return;
                if (_vm == null) return;
                if (_vm.SelectedTadItem == null) return;

                await _vm.LoadAllDataAsync();
                RebuildChart();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "TadId change error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChartMetricCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            RebuildChart();
        }

        private string BuildLabel(ChartPoint point, List<LivePositionsDataModel> src)
        {
            int idx = (int)Math.Round(point.X);
            bool? valid = idx >= 0 && idx < src.Count ? src[idx].Valid : null;
            string validStr = valid == null ? "Unknown" : valid.Value ? "Valid" : "Invalid";
            return $"{point.Y:N2}  |  {validStr}";
        }

        private void RebuildChart()
        {
            if (MyChart == null) return;

            MyChart.Series = new SeriesCollection();
            MyChart.AxisX = new AxesCollection();
            MyChart.AxisY = new AxesCollection();

            MyChart.DisableAnimations = true;
            MyChart.AnimationsSpeed = TimeSpan.Zero;
            MyChart.Hoverable = true;

            var source = _vm.LivePositions
                .Where(x => x.RunTime != default(DateTime))
                .OrderBy(x => x.RunTime)
                .ToList();


            var labels = source
                .Select(x => x.RunTime.ToString("dd-MMM-yy") + Environment.NewLine + x.RunTime.ToString("HH:mm:ss"))
                .ToList();

            var axisX = new LiveCharts.Wpf.Axis
            {
                Title = "Runtime",
                LabelsRotation = 0,
                Labels = labels,
                FontSize = 14,
                Foreground = System.Windows.Media.Brushes.Black,
                LabelFormatter = val =>
                {
                    int i = (int)Math.Round(val);
                    return i >= 0 && i < labels.Count ? labels[i] : "";
                }
            };


            var axisYValue = new LiveCharts.Wpf.Axis
            {
                Title = "Value",
                FontSize = 14,
                Foreground = System.Windows.Media.Brushes.Black,
                Sections = new SectionsCollection(),
                Position = AxisPosition.LeftBottom
            };

            var axisYPercent = new LiveCharts.Wpf.Axis
            {
                Title = "%",
                Sections = new SectionsCollection(),
                Position = AxisPosition.RightTop,
                LabelFormatter = val => val.ToString("N2")
            };

            // Validity shading
            axisX.Sections = new SectionsCollection();
            for (int i = 0; i < source.Count; i++)
            {
                bool? valid = source[i].Valid;
                if (valid == true) continue;

                System.Windows.Media.Color bandColor = valid == false
                    ? System.Windows.Media.Color.FromArgb(60, 220, 50, 50)
                    : System.Windows.Media.Color.FromArgb(60, 220, 150, 0);

                axisX.Sections.Add(new AxisSection
                {
                    Value = i - 0.5,
                    SectionWidth = 1,
                    Fill = new System.Windows.Media.SolidColorBrush(bandColor)
                });
            }

            MyChart.AxisX.Add(axisX);
            MyChart.AxisY.Add(axisYValue);

            MyChart.DataTooltip = new DefaultTooltip { SelectionMode = TooltipSelectionMode.SharedXValues };
            MyChart.DataTooltip.Background = System.Windows.Media.Brushes.Transparent;

            if (source.Count == 0)
            {
                axisYValue.MinValue = 0;
                axisYValue.MaxValue = 1;
                return;
            }

            var checkedMetrics = _vm.CheckedChartMetrics.ToList();
            if (checkedMetrics.Count == 0) return;

            bool hasPercent = checkedMetrics.Any(m => _percentMetrics.Contains(m.Key));
            if (hasPercent)
                MyChart.AxisY.Add(axisYPercent);

            var valueAxisData = new List<double>();
            var percentAxisData = new List<double>();
            var capturedSource = source;

            foreach (var metric in checkedMetrics)
            {
                bool isPercent = _percentMetrics.Contains(metric.Key);
                bool isLimit = _limitMetrics.Contains(metric.Key);
                var values = new ChartValues<double>();

                SeriesStyle style = _seriesStyles.ContainsKey(metric.Key)
                    ? _seriesStyles[metric.Key]
                    : _defaultStyle;

                // Forward-fill nulls
                double lastKnown = 0.0;
                foreach (var row in source)
                {
                    double? raw = GetMetricValue(row, metric.Key);
                    if (raw.HasValue) lastKnown = raw.Value;
                    double v = raw ?? lastKnown;
                    values.Add(v);
                    if (isPercent) percentAxisData.Add(v);
                    else valueAxisData.Add(v);
                }

                System.Windows.Media.DoubleCollection dash =
                    isPercent ? new System.Windows.Media.DoubleCollection { 4, 2 } :
                    isLimit ? new System.Windows.Media.DoubleCollection { 12, 4 } :
                                null;

                // All series use LineSeries + LineSmoothness=0 — StepLineSeries applies
                // directional colouring to up/down segments which overrides the Stroke we set
                MyChart.Series.Add(new LineSeries
                {
                    Title = metric.DisplayName,
                    Values = values,
                    PointGeometry = null,
                    Stroke = style.Stroke,
                    StrokeThickness = style.StrokeThickness,
                    LineSmoothness = 0,
                    Fill = System.Windows.Media.Brushes.Transparent,
                    ScalesYAt = (isPercent && hasPercent) ? 1 : 0,
                    StrokeDashArray = dash,
                    LabelPoint = lp => BuildLabel(lp, capturedSource)
                });

                // Auto-mirror position_limit as short
                if (metric.Key == "position_limit")
                {
                    var shortValues = new ChartValues<double>(values.Select(v => -v));
                    valueAxisData.AddRange(shortValues);

                    MyChart.Series.Add(new LineSeries
                    {
                        Title = "Position Limit (Short)",
                        Values = shortValues,
                        PointGeometry = null,
                        Stroke = style.Stroke,
                        StrokeThickness = style.StrokeThickness,
                        LineSmoothness = 0,
                        Fill = System.Windows.Media.Brushes.Transparent,
                        ScalesYAt = 0,
                        StrokeDashArray = dash,
                        LabelPoint = lp => BuildLabel(lp, capturedSource)
                    });
                }
            }

            ConfigureAxis(axisYValue, valueAxisData);
            if (hasPercent)
                ConfigureAxis(axisYPercent, percentAxisData);
        }

        private void ConfigureAxis(LiveCharts.Wpf.Axis axis, List<double> values)
        {
            if (values.Count == 0)
            {
                axis.MinValue = 0;
                axis.MaxValue = 1;
                return;
            }

            double minVal = values.Min();
            double maxVal = values.Max();

            double padding = (maxVal - minVal) * 0.1;
            if (Math.Abs(padding) < 0.0001) padding = 1;

            minVal -= padding;
            maxVal += padding;

            if (minVal > 0) minVal = 0;
            if (maxVal < 0) maxVal = 0;

            axis.MinValue = minVal;
            axis.MaxValue = maxVal;
            axis.Separator = new LiveCharts.Wpf.Separator
            {
                Step = CalculateStepSize(maxVal - minVal),
                StrokeThickness = 1,
                StrokeDashArray = new System.Windows.Media.DoubleCollection { 2, 2 },
                Stroke = System.Windows.Media.Brushes.Gray
            };

            axis.Sections.Add(new AxisSection
            {
                Value = 0,
                Stroke = System.Windows.Media.Brushes.Black,
                Label = "0",
                StrokeThickness = 2
            });
        }

        private double? GetMetricValue(LivePositionsDataModel row, string key)
        {
            if (row == null) return null;

            switch (key)
            {
                case "position_live": return row.PositionLive;
                case "position_limit": return row.PositionLimit;
                case "position_target": return row.PositionTarget;
                case "deployment": return row.Deployment;
                case "deployment_tad": return row.DeploymentTad;
                case "position_target_raw": return row.PositionTargetRaw;
                case "scaled_percent": return row.ScaledPercent;
                default: return null;
            }
        }

        private double CalculateStepSize(double range)
        {
            if (range <= 0) return 1;

            double step = Math.Pow(10, Math.Floor(Math.Log10(range)));
            if (range / step > 10) step *= 2;
            if (range / step > 10) step *= 5;
            return step;
        }
    }
}
