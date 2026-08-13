using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using static wpfTDX.FilterIntervalsViewModel;


namespace wpfTDX
{
    /// <summary>
    /// Interaction logic for winFilterIntervals.xaml
    /// </summary>
    public partial class winFilterIntervals : Window
    {

        private FilterIntervalsViewModel _viewModel;
        private SqlConnection gbl_conn;

        public winFilterIntervals(SqlConnection conn)
        {
            InitializeComponent();
            this.gbl_conn = conn;
            _viewModel = new FilterIntervalsViewModel(this.gbl_conn);
            DataContext = _viewModel;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Initial window load uses the same flow as the Reload Both button so
            // the progress bar and status messages behave identically in both cases.
            await DoFullReloadAsync();
        }

        /// <summary>
        /// Shared full-reload flow used by both the initial Window_Loaded and the
        /// Reload Both button — guarantees identical progress bar + status messaging.
        /// </summary>
        private async Task DoFullReloadAsync()
        {
            try
            {
                _viewModel.ClearJobStatus();
                await RunWithBusy(LoadAllAsync);
                StatusTextBlock.Text = $"Reloaded {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Reload failed:\n" + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async Task LoadAllAsync()
        {
            BeginLoad(totalSteps: 8);

            await SetStatus("Loading host env...");
            await _viewModel.LoadHostEnvAsync();
            await ScaleLimits.EnsureLoadedAsync();   // server-driven scale cap for new_f validation

            await SetStatus("Loading freeze definitions...");
            await _viewModel.LoadManualFreezeDefsAsync();

            await SetStatus("Loading active freezes...");
            await _viewModel.LoadActiveManualFreezesAsync();

            await SetStatus("Loading strategy overrides...");
            await _viewModel.LoadStrategiesOverrideAsync();

            await SetStatus("Loading strategy names...");
            await _viewModel.LoadStrategyNamesAsync();
            await _viewModel.LoadStrategyNamesBaseAsync();

            await SetStatus("Loading ticker universe...");
            await _viewModel.LoadTickerUniverseAsync();

            await SetStatus("Loading filter intervals + positions from server...");
            // Single fetch: /filtered_intervals returns both filter intervals and positions data
            await _viewModel.LoadFilterIntervalsAndPositionsAsync();

            await SetStatus("Rendering data on UI...");
            await _viewModel.RebuildMerged();
            UpdateIntervalColumnVisibility();
            SizeStrategyColumns();

            _viewModel.LoadProgress = 100;
        }

        /// <summary>
        /// Event-importance toggle changed: re-highlight rows via the lightweight
        /// /event_affected endpoint (no full reload). Ignores the selection set during
        /// initial load; only reacts once the window is loaded and rows exist.
        /// </summary>
        private async void EventImportanceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || _viewModel == null) return;
            if (_viewModel.MergedRows == null || _viewModel.MergedRows.Count == 0) return;
            try
            {
                await _viewModel.RefreshEventAffectedAsync();
            }
            catch
            {
                // highlight refresh is best-effort; never disrupt the screen
            }
        }

        /// <summary>
        /// Sizes the strategy / strategy_b columns to fit the widest name in their dropdown list
        /// (StrategyNames / StrategyNamesBase) plus the header, so whatever the user selects fits
        /// without clipping. (Sizing to only the currently-shown values clips once a longer name
        /// is chosen.)
        /// </summary>
        private void SizeStrategyColumns()
        {
            if (FilterGrid == null || _viewModel == null) return;

            double pixelsPerDip = 1.0;
            try { pixelsPerDip = System.Windows.Media.VisualTreeHelper.GetDpi(this).PixelsPerDip; } catch { }
            var tf = new System.Windows.Media.Typeface(FilterGrid.FontFamily, FilterGrid.FontStyle,
                                                       FilterGrid.FontWeight, FilterGrid.FontStretch);
            var tfHeader = new System.Windows.Media.Typeface(FilterGrid.FontFamily, FilterGrid.FontStyle,
                                                       System.Windows.FontWeights.Bold, FilterGrid.FontStretch);
            double fontSize = FilterGrid.FontSize;

            double Widest(System.Collections.Generic.IEnumerable<string> items, string header)
            {
                double max = 0;
                foreach (var s in (items ?? System.Linq.Enumerable.Empty<string>()))
                {
                    if (string.IsNullOrEmpty(s)) continue;
                    var ft = new System.Windows.Media.FormattedText(
                        s, System.Globalization.CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight,
                        tf, fontSize, System.Windows.Media.Brushes.Black, pixelsPerDip);
                    if (ft.Width > max) max = ft.Width;
                }
                // also fit the (bold) header text
                var hft = new System.Windows.Media.FormattedText(
                    header ?? "", System.Globalization.CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight,
                    tfHeader, fontSize, System.Windows.Media.Brushes.Black, pixelsPerDip);
                if (hft.Width > max) max = hft.Width;
                return max;
            }

            void SetWidth(string header, double contentWidth)
            {
                var col = ColumnByHeader(header);
                if (col == null) return;
                // small padding — the cell shows plain text almost always (the edit ComboBox
                // chevron may sit near the longest value, but the popup still shows it in full).
                col.Width = new DataGridLength(Math.Ceiling(contentWidth) + 12);
            }

            // Measure the widest name in each dropdown list, so any value the user can pick fits
            // (sizing to only the currently-shown values clips once a longer one is selected).
            SetWidth("strategy", Widest(_viewModel.StrategyNames, "strategy"));
            SetWidth("strategy_b", Widest(_viewModel.StrategyNamesBase, "strategy_b"));
        }

        // Step counter for the determinate load progress bar — set by BeginLoad,
        // incremented by SetStatus.
        private int _loadStepCurrent;
        private int _loadStepTotal;

        /// <summary>
        /// Resets the load progress counter at the start of a multi-stage load so
        /// SetStatus can compute LoadProgress as a percentage of total stages.
        /// </summary>
        private void BeginLoad(int totalSteps)
        {
            _loadStepCurrent = 0;
            _loadStepTotal = totalSteps;
            _viewModel.LoadProgress = 0;
        }

        /// <summary>
        /// Updates the status bar text, ticks the determinate load progress bar
        /// forward by one step, and yields at Background priority so WPF gets a
        /// chance to repaint before the next async call begins. The Background
        /// priority is critical: Task.Yield resumes at Normal priority which is
        /// higher than Render, so without the explicit yield priority the new
        /// text + progress value never make it to the screen.
        /// </summary>
        private async Task SetStatus(string text)
        {
            StatusTextBlock.Text = text;

            if (_loadStepTotal > 0)
            {
                _loadStepCurrent++;
                int pct = (int)Math.Round(100.0 * _loadStepCurrent / _loadStepTotal);
                _viewModel.LoadProgress = Math.Min(100, Math.Max(0, pct));
            }

            await Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);
        }

        /// <summary>
        /// Hides interval columns that are below the minimum p_int across all rows.
        /// </summary>
        private void UpdateIntervalColumnVisibility()
        {
            var minInterval = _viewModel.GetMinVisibleInterval();
            var order = FilterIntervalsViewModel.IntervalOrder;
            int minIdx = Array.FindIndex(order, i => string.Equals(i, minInterval, StringComparison.OrdinalIgnoreCase));
            if (minIdx < 0) minIdx = 0;

            foreach (var col in FilterGrid.Columns)
            {
                var header = col.Header as string;
                if (header == null) continue;

                // find this column in the interval order
                int colIdx = Array.FindIndex(order, i => string.Equals(i, header, StringComparison.OrdinalIgnoreCase));
                if (colIdx < 0) continue; // not an interval column, leave visible

                col.Visibility = colIdx >= minIdx
                    ? System.Windows.Visibility.Visible
                    : System.Windows.Visibility.Collapsed;
            }

            // Keep any user-collapsed groups collapsed after this visibility pass.
            ReapplyCollapsedGroups();
        }
        // ── Collapsible interval column groups (Excel-style banded headers) ──
        private sealed class ColumnGroup
        {
            public string Label;
            public string[] Headers;
            public Border Band;                  // the banded header in GroupHeaderCanvas
            public ToggleButton Toggle;
            public TextBlock LabelText;
            public Border BracketLine, TickLeft, TickRight;
            public bool Expanded = true;
            public bool LabelOnly;               // non-collapsible label band (no toggle)
            public string First { get { return Headers[0]; } }
            public string Last { get { return Headers[Headers.Length - 1]; } }
        }

        private readonly List<ColumnGroup> _columnGroups = new List<ColumnGroup>();
        private ScrollViewer _gridScrollViewer;
        private DataGridColumnHeadersPresenter _headersPresenter;

        private static readonly Brush BandBracketBrush =
            new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x5A, 0x6B, 0x8C));
        private static readonly Brush BandChipBrush =
            new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF0, 0xF0, 0xF0));

        /// <summary>
        /// Once the grid's visual tree exists, build the group bands and keep them aligned
        /// over their columns on horizontal scroll and on every layout pass (column resize).
        /// </summary>
        private void FilterGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (_headersPresenter == null)
            {
                _headersPresenter = FindVisualChild<DataGridColumnHeadersPresenter>(FilterGrid);
                _gridScrollViewer = FindVisualChild<ScrollViewer>(FilterGrid);
                if (_gridScrollViewer != null)
                    _gridScrollViewer.ScrollChanged += (s, a) => QueuePositionGroupBands();
                FilterGrid.LayoutUpdated += (s, a) => QueuePositionGroupBands();
                ApplyColumnOrder();
                BuildColumnGroups();
            }
            PositionGroupBands();
        }

        // LayoutUpdated fires on virtually every layout pass (and each row Add during a
        // reload triggers one), so running PositionGroupBands() — a visual-tree walk —
        // synchronously on each was the load/reload bottleneck. Coalesce a burst of
        // layout/scroll events into a single positioning pass per render frame.
        private bool _bandsUpdateQueued;
        private void QueuePositionGroupBands()
        {
            if (_bandsUpdateQueued) return;
            _bandsUpdateQueued = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _bandsUpdateQueued = false;
                PositionGroupBands();
            }), System.Windows.Threading.DispatcherPriority.Render);
        }

        // Bill's column order for the filter intervals screen (left → right). ticker leads
        // (and is frozen via DataGrid.FrozenColumnCount=1 so it stays visible when scrolling
        // horizontally), then category and the other identity columns; intervals (base +
        // trading) sit at the far right.
        private static readonly string[] DesiredColumnOrder =
        {
            "ticker", "category", "fundgrp", "fund", "strategy", "strategy_b", "m__int", "p__int",
            "c__upd", "manual", "rescale", "LO", "SO", "byO", "slO", "filt_all",
            "trd", "n", "dep", "filt_t", "filt_n", "f_dep", "new_t", "new_n", "n_dep",
            "scale_f", "new_f", "pos_lim", "s_pos_lim", "pos_tgt",
            "b_t1", "b_v1", "b_y1", "b_d1", "b_w1",
            "t1", "t2", "t3", "t4", "t5", "t8", "v2", "v3", "v4", "v6", "v8",
            "y2", "y3", "y4", "y6", "y8", "y12", "y24", "y32",
            "D1", "y72", "D2", "D3", "D4", "W1", "D8", "W2",
        };

        /// <summary>
        /// Sets each column's DisplayIndex to match DesiredColumnOrder. Assigning in
        /// increasing target order keeps WPF's auto-shift from disturbing already-placed
        /// columns. Any header not present is skipped.
        /// </summary>
        private void ApplyColumnOrder()
        {
            int di = 0;
            foreach (var header in DesiredColumnOrder)
            {
                foreach (var col in FilterGrid.Columns)
                {
                    if (string.Equals(col.Header as string, header, StringComparison.Ordinal))
                    {
                        col.DisplayIndex = di++;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Defines each interval family and creates its banded header (bracket + −/+ chip)
        /// in GroupHeaderCanvas. No placeholder columns — a collapsed column takes zero
        /// width so the grid closes up, and the collapsed "+" marker floats at the boundary.
        /// </summary>
        private void BuildColumnGroups()
        {
            if (_columnGroups.Count > 0) return;

            AddGroup("base", new[] { "b_t1", "b_v1", "b_y1", "b_d1", "b_w1" });
            // Intraday block: t1 → y12 (all sub-daily trading intervals). Contiguous, sits
            // between the base group and the daily/weekly group.
            AddGroup("intraday", new[] {
                "t1", "t2", "t3", "t4", "t5", "t8",
                "v2", "v3", "v4", "v6", "v8",
                "y2", "y3", "y4", "y6", "y8", "y12" });
            // Daily/weekly block: starts at y24 → W2 (y72 = 36h sits between D1 and D2; W1 before D8).
            AddGroup("daily/weekly", new[] {
                "y24", "y32", "D1", "y72", "D2", "D3", "D4", "W1", "D8", "W2" });

            // Non-collapsible label bands over the three trd/n/dep stat triplets. The columns
            // keep distinct Header codes (trd/n/dep, filt_t/filt_n/f_dep, new_t/new_n/n_dep) —
            // the band tells you which model each triplet is; their display text is relabelled
            // to trd/n/dep in XAML.
            AddLabelBand("live", new[] { "trd", "n", "dep" });
            AddLabelBand("filtered", new[] { "filt_t", "filt_n", "f_dep" });
            AddLabelBand("proposed", new[] { "new_t", "new_n", "n_dep" });
        }

        private void AddGroup(string label, string[] headers)
        {
            if (IndexOfColumn(headers[0]) < 0) return;   // group not present on this grid

            var g = new ColumnGroup { Label = label, Headers = headers };

            // Outline bracket: a line with end-ticks.
            g.BracketLine = new Border { Height = 2, VerticalAlignment = VerticalAlignment.Center,
                Background = BandBracketBrush, Margin = new Thickness(9, 0, 9, 0) };
            g.TickLeft = new Border { Width = 2, Height = 8, HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(9, 0, 0, 0), Background = BandBracketBrush };
            g.TickRight = new Border { Width = 2, Height = 8, HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 9, 0), Background = BandBracketBrush };

            // −/+ chip + label, with a background that "breaks" the bracket line.
            g.Toggle = new ToggleButton { Content = "−", IsChecked = true, Width = 16, Height = 16,
                Padding = new Thickness(0), FontWeight = FontWeights.Bold,
                ToolTip = "Collapse/expand the " + label + " interval columns" };
            var captured = g;
            g.Toggle.Click += (s, e) => ToggleGroup(captured);

            g.LabelText = new TextBlock { Text = label, FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 3, 0) };

            var chip = new StackPanel { Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                Background = BandChipBrush };
            chip.Children.Add(g.Toggle);
            chip.Children.Add(g.LabelText);

            var grid = new Grid();
            grid.Children.Add(g.BracketLine);
            grid.Children.Add(g.TickLeft);
            grid.Children.Add(g.TickRight);
            grid.Children.Add(chip);

            // Background = null (not Transparent) so the band's empty area is NOT hit-testable
            // — otherwise an expanded band would swallow clicks meant for an overlapping
            // collapsed "+" marker. Only the bracket/chip/+ button are clickable.
            g.Band = new Border { Height = 22, Background = null,
                Visibility = System.Windows.Visibility.Collapsed, Child = grid };
            GroupHeaderCanvas.Children.Add(g.Band);

            _columnGroups.Add(g);
        }

        /// <summary>
        /// A non-collapsible band: bracket + centred label above a contiguous column group
        /// (e.g. live / filtered / proposed over the trd/n/dep triplets). No toggle button —
        /// it's always shown (Expanded), positioned by PositionExpandedBand like any other band.
        /// </summary>
        private void AddLabelBand(string label, string[] headers)
        {
            if (IndexOfColumn(headers[0]) < 0) return;   // group not present on this grid

            var g = new ColumnGroup { Label = label, Headers = headers, LabelOnly = true, Expanded = true };

            // Zero side-margins so the bracket + end-ticks sit exactly on the group's column
            // span (first column's left edge → last column's right edge), i.e. trd → dep.
            g.BracketLine = new Border { Height = 2, VerticalAlignment = VerticalAlignment.Center,
                Background = BandBracketBrush, Margin = new Thickness(0, 0, 0, 0) };
            g.TickLeft = new Border { Width = 2, Height = 8, HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 0), Background = BandBracketBrush };
            g.TickRight = new Border { Width = 2, Height = 8, HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 0), Background = BandBracketBrush };

            g.LabelText = new TextBlock { Text = label, FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 4, 0) };

            var chip = new StackPanel { Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                Background = BandChipBrush };
            chip.Children.Add(g.LabelText);   // no toggle — this band never collapses

            var grid = new Grid();
            grid.Children.Add(g.BracketLine);
            grid.Children.Add(g.TickLeft);
            grid.Children.Add(g.TickRight);
            grid.Children.Add(chip);

            g.Band = new Border { Height = 22, Background = null,
                Visibility = System.Windows.Visibility.Collapsed, Child = grid };
            GroupHeaderCanvas.Children.Add(g.Band);

            _columnGroups.Add(g);
        }

        private int IndexOfColumn(string header)
        {
            for (int i = 0; i < FilterGrid.Columns.Count; i++)
                if ((FilterGrid.Columns[i].Header as string) == header) return i;
            return -1;
        }

        private DataGridColumn ColumnByHeader(string header)
        {
            for (int i = 0; i < FilterGrid.Columns.Count; i++)
                if ((FilterGrid.Columns[i].Header as string) == header) return FilterGrid.Columns[i];
            return null;
        }

        /// <summary>
        /// Collapse/expand one interval group: hides its member columns (they take zero width,
        /// so the grid closes up) and switches its band to the bracket (expanded) or a small
        /// floating "+" marker (collapsed).
        /// </summary>
        private void ToggleGroup(ColumnGroup g)
        {
            g.Expanded = g.Toggle.IsChecked == true;
            SetColumnsVisible(g.Headers, g.Expanded);
            // Expanding shows the group's columns; re-apply the min-p_int baseline so we
            // don't force-show interval columns the grid normally hides. (Base columns
            // aren't ranked, so they just stay shown.)
            if (g.Expanded)
                UpdateIntervalColumnVisibility();
            g.Toggle.Content = g.Expanded ? "−" : "+";
            var expandedVis = g.Expanded ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            g.BracketLine.Visibility = expandedVis;
            g.TickLeft.Visibility = expandedVis;
            g.TickRight.Visibility = expandedVis;
            g.LabelText.Visibility = expandedVis;   // collapsed marker is just the "+"

            Dispatcher.BeginInvoke(new Action(PositionGroupBands),
                System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// Re-applies collapsed groups after the min-interval visibility pass, so a reload
        /// doesn't re-show columns belonging to a group the user has collapsed.
        /// </summary>
        private void ReapplyCollapsedGroups()
        {
            foreach (var g in _columnGroups)
            {
                if (g.Expanded) continue;
                SetColumnsVisible(g.Headers, false);
            }
            PositionGroupBands();
        }

        /// <summary>
        /// Positions every group band: expanded bands span their columns; collapsed groups
        /// show a small "+" marker floated at the group's boundary, packed left-to-right so
        /// adjacent collapsed markers don't overlap.
        /// </summary>
        private void PositionGroupBands()
        {
            if (GroupHeaderCanvas == null || FilterGrid == null || _columnGroups.Count == 0) return;

            var byText = new Dictionary<string, DataGridColumnHeader>(StringComparer.Ordinal);
            var scope = (DependencyObject)_headersPresenter ?? FilterGrid;
            foreach (var h in FindVisualChildren<DataGridColumnHeader>(scope))
            {
                if (h.Column == null) continue;
                var txt = h.Column.Header as string;
                if (txt != null) byText[txt] = h;
            }

            double canvasW = GroupHeaderCanvas.ActualWidth;
            double canvasX = GroupHeaderCanvas.TransformToVisual(this).Transform(new System.Windows.Point(0, 0)).X;

            double lastCollapsedRight = double.NegativeInfinity;
            foreach (var g in _columnGroups)
            {
                if (g.Expanded)
                    PositionExpandedBand(g, byText, canvasX, canvasW);
                else
                    lastCollapsedRight = PositionCollapsedBand(g, byText, canvasX, canvasW, lastCollapsedRight);

                // Keep collapsed "+" markers above expanded brackets so they stay clickable
                // where the two overlap at a boundary.
                System.Windows.Controls.Panel.SetZIndex(g.Band, g.Expanded ? 0 : 1);
            }
        }

        private void PositionExpandedBand(ColumnGroup g,
            Dictionary<string, DataGridColumnHeader> byText, double canvasX, double canvasW)
        {
            // Span the first-visible to last-visible column of the group, so the bracket
            // appears whenever ANY of its columns are on screen (the min-p_int logic may
            // hide part of a group).
            DataGridColumnHeader left = null, right = null;
            foreach (var hdr in g.Headers)
            {
                DataGridColumnHeader hh;
                if (byText.TryGetValue(hdr, out hh) && hh.ActualWidth > 0)
                {
                    if (left == null) left = hh;
                    right = hh;
                }
            }

            if (left == null || right == null)
            {
                g.Band.Visibility = System.Windows.Visibility.Collapsed;
                return;
            }

            try
            {
                double leftX = left.TransformToVisual(this).Transform(new System.Windows.Point(0, 0)).X;
                double rightX = right.TransformToVisual(this).Transform(new System.Windows.Point(right.ActualWidth, 0)).X;

                double x = leftX - canvasX;
                double w = rightX - leftX;
                if (x < 0) { w += x; x = 0; }
                if (x + w > canvasW) w = canvasW - x;
                if (w <= 0) { g.Band.Visibility = System.Windows.Visibility.Collapsed; return; }

                Canvas.SetLeft(g.Band, x);
                g.Band.Width = w;
                g.Band.Visibility = System.Windows.Visibility.Visible;
            }
            catch
            {
                g.Band.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private double PositionCollapsedBand(ColumnGroup g,
            Dictionary<string, DataGridColumnHeader> byText, double canvasX, double canvasW, double lastRight)
        {
            const double w = 18;   // small "+" marker

            // Boundary is computed in DISPLAY order (DisplayIndex), NOT the Columns-collection
            // order. ApplyColumnOrder() reorders columns via DisplayIndex, so collection order
            // != on-screen order (e.g. the stat columns are declared after the intervals in XAML
            // but shown to their left). Walking the collection would find the wrong neighbour and
            // place the "+" on the wrong side of the group. So: find the group's member
            // DisplayIndex range, then the nearest VISIBLE neighbour on each side by DisplayIndex.
            double boundaryX = double.NaN;

            int firstDi = int.MaxValue, lastDi = int.MinValue;
            foreach (var h in g.Headers)
            {
                var mc = ColumnByHeader(h);
                if (mc == null) continue;
                if (mc.DisplayIndex < firstDi) firstDi = mc.DisplayIndex;
                if (mc.DisplayIndex > lastDi) lastDi = mc.DisplayIndex;
            }

            if (lastDi >= 0)
            {
                // nearest visible column to the RIGHT = smallest DisplayIndex > lastDi
                DataGridColumnHeader rightHdr = null;
                int bestRight = int.MaxValue;
                foreach (var col in FilterGrid.Columns)
                {
                    var hdr = col.Header as string;
                    if (hdr == null || col.Visibility != System.Windows.Visibility.Visible) continue;
                    DataGridColumnHeader hh;
                    if (!byText.TryGetValue(hdr, out hh)) continue;
                    if (col.DisplayIndex > lastDi && col.DisplayIndex < bestRight) { bestRight = col.DisplayIndex; rightHdr = hh; }
                }
                if (rightHdr != null)
                    boundaryX = rightHdr.TransformToVisual(this).Transform(new System.Windows.Point(0, 0)).X;
            }
            if (double.IsNaN(boundaryX) && firstDi != int.MaxValue)
            {
                // nearest visible column to the LEFT = largest DisplayIndex < firstDi
                DataGridColumnHeader leftHdr = null;
                int bestLeft = int.MinValue;
                foreach (var col in FilterGrid.Columns)
                {
                    var hdr = col.Header as string;
                    if (hdr == null || col.Visibility != System.Windows.Visibility.Visible) continue;
                    DataGridColumnHeader hh;
                    if (!byText.TryGetValue(hdr, out hh)) continue;
                    if (col.DisplayIndex < firstDi && col.DisplayIndex > bestLeft) { bestLeft = col.DisplayIndex; leftHdr = hh; }
                }
                if (leftHdr != null)
                    boundaryX = leftHdr.TransformToVisual(this).Transform(new System.Windows.Point(leftHdr.ActualWidth, 0)).X;
            }
            if (double.IsNaN(boundaryX))
            {
                g.Band.Visibility = System.Windows.Visibility.Collapsed;
                return lastRight;
            }

            double x = boundaryX - canvasX - w / 2.0;   // centre the marker on the boundary
            if (x < lastRight + 1) x = lastRight + 1;    // pack against the previous collapsed marker
            if (x < 0) x = 0;
            if (x + w > canvasW)
            {
                g.Band.Visibility = System.Windows.Visibility.Collapsed;
                return lastRight;
            }

            Canvas.SetLeft(g.Band, x);
            g.Band.Width = w;
            g.Band.Visibility = System.Windows.Visibility.Visible;
            return x + w;
        }

        /// <summary>
        /// Shows or hides the grid columns whose headers are in <paramref name="headers"/>.
        /// </summary>
        private void SetColumnsVisible(string[] headers, bool visible)
        {
            var set = new HashSet<string>(headers, StringComparer.OrdinalIgnoreCase);
            var vis = visible ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            foreach (var col in FilterGrid.Columns)
            {
                var h = col.Header as string;
                if (h != null && set.Contains(h))
                    col.Visibility = vis;
            }
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            foreach (var c in FindVisualChildren<T>(parent)) return c;
            return null;
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;
            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T t) yield return t;
                foreach (var d in FindVisualChildren<T>(child))
                    yield return d;
            }
        }

        private async Task RunWithBusy(Func<Task> work)
        {
            try
            {
                _viewModel.IsExecuting = true;
                await work();
            }
            finally
            {
                _viewModel.IsExecuting = false;
            }
        }


        // map columns -> property names (fill out the rest of your columns)
        private bool TryMap(DataGridColumn col, out string flagProp, out string posProp)
        {
            flagProp = null; posProp = null;
            if (col == null) return false;
            var header = col.Header as string;
            if (string.IsNullOrEmpty(header)) return false;

            switch (header)
            {
                case "base_y1": flagProp = "BaseY1"; posProp = "PositionBaseY1"; return true;
                case "base_h1": flagProp = "BaseH1"; posProp = "PositionBaseH1"; return true;
                case "base_d1": flagProp = "BaseD1"; posProp = "PositionBaseD1"; return true;
                case "y1": flagProp = "y1"; posProp = "PositionY1"; return true;
                case "y2": flagProp = "y2"; posProp = "PositionY2"; return true;
                case "y3": flagProp = "y3"; posProp = "PositionY3"; return true;
                case "h2": flagProp = "H2"; posProp = "PositionH2"; return true;
                case "h3": flagProp = "H3"; posProp = "PositionH3"; return true;
                case "h4": flagProp = "H4"; posProp = "PositionH4"; return true;
                case "h5": flagProp = "H5"; posProp = "PositionH5"; return true;
                case "h6": flagProp = "H6"; posProp = "PositionH6"; return true;
                case "h12": flagProp = "H12"; posProp = "PositionH12"; return true;
                case "h16": flagProp = "H16"; posProp = "PositionH16"; return true;
                case "h36": flagProp = "H36"; posProp = "PositionH36"; return true;
                case "D1": flagProp = "D1"; posProp = "PositionD1"; return true;
                case "D2": flagProp = "D2"; posProp = "PositionD2"; return true;
                case "D3": flagProp = "D3"; posProp = "PositionD3"; return true;
                case "D4": flagProp = "D4"; posProp = "PositionD4"; return true;
                case "D8": flagProp = "D8"; posProp = "PositionD8"; return true;
                case "W1": flagProp = "W1"; posProp = "PositionW1"; return true;
                case "W2": flagProp = "W2"; posProp = "PositionW2"; return true;
                default: return false;
            }
        }

        static T FindAncestor<T>(DependencyObject d) where T : DependencyObject
        {
            while (d != null && !(d is T)) d = System.Windows.Media.VisualTreeHelper.GetParent(d);
            return d as T;
        }


        // Map a column header to (flagProp, posProp). posProp is null for top-level booleans.
        private bool TryMapToggleTarget(DataGridColumn col, out string flagProp, out string posProp)
        {
            flagProp = null; posProp = null;
            if (col == null) return false;

            var header = col.Header as string;
            if (string.IsNullOrEmpty(header)) return false;

            // normalize to be robust
            string h = header.Trim().ToLowerInvariant();

            switch (h)
            {
                // top-level booleans (no Position* guard)
                case "rescale": flagProp = "Rescale"; return true;
                case "manual": flagProp = "Manual"; return true;
                case "lo_only": case "lo": flagProp = "LongOnly"; return true;
                case "so_only": case "so": flagProp = "ShortOnly"; return true;
                case "buy_only": case "byo": flagProp = "BuyOnly"; return true;
                case "sell_only": case "slo": flagProp = "SellOnly"; return true;
                case "filt_intvls": case "filt_all": flagProp = "AllIntervals"; return true;

                // cont_upd toggle (no Position* guard)
                case "c_upd": case "c__upd": flagProp = "ContUpd"; return true;

                // base intervals — independent toggles, one column per filter_intervals base flag
                case "b_t1": flagProp = "BaseT1"; posProp = "PositionBaseT1"; return true;
                case "b_v1": flagProp = "BaseV1"; posProp = "PositionBaseV1"; return true;
                case "b_n1": flagProp = "BaseN1"; posProp = "PositionBaseN1"; return true;
                case "b_y1": flagProp = "BaseY1"; posProp = "PositionBaseY1"; return true;
                case "b_h1": flagProp = "BaseH1"; posProp = "PositionBaseH1"; return true;
                case "b_d1": flagProp = "BaseD1"; posProp = "PositionBaseD1"; return true;
                case "b_w1": flagProp = "BaseW1"; posProp = "PositionBaseW1"; return true;

                // short-term intervals
                case "t1": flagProp = "T1"; posProp = "PositionT1"; return true;
                case "t2": flagProp = "T2"; posProp = "PositionT2"; return true;
                case "t3": flagProp = "T3"; posProp = "PositionT3"; return true;
                case "t4": flagProp = "T4"; posProp = "PositionT4"; return true;
                case "t5": flagProp = "T5"; posProp = "PositionT5"; return true;
                case "t8": flagProp = "T8"; posProp = "PositionT8"; return true;
                case "v2": flagProp = "V2"; posProp = "PositionV2"; return true;
                case "v3": flagProp = "V3"; posProp = "PositionV3"; return true;
                case "v4": flagProp = "V4"; posProp = "PositionV4"; return true;
                case "v6": flagProp = "V6"; posProp = "PositionV6"; return true;
                case "v8": flagProp = "V8"; posProp = "PositionV8"; return true;
                case "n2": flagProp = "N2"; posProp = "PositionN2"; return true;
                case "n3": flagProp = "N3"; posProp = "PositionN3"; return true;
                case "n4": flagProp = "N4"; posProp = "PositionN4"; return true;

                // y* timeframes
                case "y1": flagProp = "y1"; posProp = "PositionY1"; return true;
                case "y2": flagProp = "y2"; posProp = "PositionY2"; return true;
                case "y3": flagProp = "y3"; posProp = "PositionY3"; return true;
                case "y4": flagProp = "y4"; posProp = "PositionY4"; return true;
                case "y6": flagProp = "y6"; posProp = "PositionY6"; return true;
                case "y8": flagProp = "y8"; posProp = "PositionY8"; return true;
                case "y12": flagProp = "y12"; posProp = "PositionY12"; return true;
                case "y24": flagProp = "y24"; posProp = "PositionY24"; return true;
                case "y32": flagProp = "y32"; posProp = "PositionY32"; return true;
                case "y72": flagProp = "y72"; posProp = "PositionY72"; return true;

                // h* timeframes
                case "h2": flagProp = "H2"; posProp = "PositionH2"; return true;
                case "h3": flagProp = "H3"; posProp = "PositionH3"; return true;
                case "h4": flagProp = "H4"; posProp = "PositionH4"; return true;
                case "h5": flagProp = "H5"; posProp = "PositionH5"; return true;   // keep if you add H5 back
                case "h6": flagProp = "H6"; posProp = "PositionH6"; return true;
                case "h12": flagProp = "H12"; posProp = "PositionH12"; return true;
                case "h16": flagProp = "H16"; posProp = "PositionH16"; return true;
                case "h36": flagProp = "H36"; posProp = "PositionH36"; return true;

                // daily/weekly
                case "d1": flagProp = "D1"; posProp = "PositionD1"; return true;
                case "d2": flagProp = "D2"; posProp = "PositionD2"; return true;
                case "d3": flagProp = "D3"; posProp = "PositionD3"; return true;
                case "d4": flagProp = "D4"; posProp = "PositionD4"; return true;
                case "d8": flagProp = "D8"; posProp = "PositionD8"; return true;
                case "w1": flagProp = "W1"; posProp = "PositionW1"; return true;
                case "w2": flagProp = "W2"; posProp = "PositionW2"; return true;

                default: return false;
            }
        }



        // Make the method name match your XAML: DataGrid_PreviewMouseDoubleClick
        private void DataGrid_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dep = e.OriginalSource as DependencyObject;

            // only act on data cells
            var cell = FindAncestor<DataGridCell>(dep);
            if (cell == null) return;
            var row = FindAncestor<DataGridRow>(cell);
            if (row == null) return;

            var item = row.Item as FilterIntervalsViewModel.MergedTickerRow;
            if (item == null) return;

            string flagProp, posProp;
            if (!TryMapToggleTarget(cell.Column, out flagProp, out posProp)) return;

            var t = item.GetType();

            // Guard timeframe columns by Position* being non-null
            if (!string.IsNullOrEmpty(posProp))
            {
                var posObj = t.GetProperty(posProp).GetValue(item, null);
                float? posVal = (posObj == null) ? (float?)null : (float?)posObj;
                if (!posVal.HasValue)
                {
                    // disabled (grey) — do nothing
                    e.Handled = true;
                    return;
                }
            }

            // Toggle the bool? (null -> true, true -> false, false -> true)
            var curObj = t.GetProperty(flagProp).GetValue(item, null);
            bool? cur = (curObj == null) ? (bool?)null : (bool?)curObj;
            bool? next = (cur == true) ? (bool?)false
                       : (cur == false) ? (bool?)true
                       : (bool?)true;

            t.GetProperty(flagProp).SetValue(item, next, null);

            e.Handled = true; // swallow the double-click
        }

        private async void ReloadPositionsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.ClearJobStatus();
                await RunWithBusy(async () =>
                {
                    BeginLoad(totalSteps: 2);

                    await SetStatus("Loading positions from server...");
                    await _viewModel.LoadTadPositionsDataAsync();

                    await SetStatus("Rendering data on UI...");
                    await _viewModel.RebuildMerged(preserveUserFiFlags: true);
                    UpdateIntervalColumnVisibility();

                    _viewModel.LoadProgress = 100;
                });
                StatusTextBlock.Text = $"Reloaded {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Load positions failed:\n" + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ReloadFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.ClearJobStatus();
                await RunWithBusy(async () =>
                {
                    BeginLoad(totalSteps: 2);

                    await SetStatus("Loading filters from server...");
                    await _viewModel.LoadFilterIntervalsDataAsync();

                    await SetStatus("Rendering data on UI...");
                    await _viewModel.RebuildMerged(preserveUserFiFlags: false);
                    UpdateIntervalColumnVisibility();

                    _viewModel.LoadProgress = 100;
                });
                StatusTextBlock.Text = $"Reloaded {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Load filters failed:\n" + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ReloadBothButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.ClearJobStatus();
                await RunWithBusy(LoadAllAsync);
                StatusTextBlock.Text = $"Reloaded {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Reload failed:\n" + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DataGridRow_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var row = sender as DataGridRow;
            if (row != null)
                row.IsSelected = true;
        }

        private void DeleteTickerMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var row = FilterGrid.SelectedItem as FilterIntervalsViewModel.MergedTickerRow;
            if (row == null) return;

            // End any in-progress edit before we mutate + Refresh(). Deleting a freshly-added,
            // still-unsaved row leaves the grid mid-EditItem transaction on that row, and
            // ICollectionView.Refresh() throws "not allowed during an AddNew or EditItem
            // transaction" while that's open. Commit the cell/row (cancel if commit is rejected).
            FilterGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            if (!FilterGrid.CommitEdit(DataGridEditingUnit.Row, true))
                FilterGrid.CancelEdit(DataGridEditingUnit.Row);

            // Thaw it
            row.Manual = false;

            // Soft delete
            row.IsDeleted = true;

            //set strategies override to defaults
            row.StrategyName = "default";
            row.ContUpd = false;
            row.MinIntvl = "default";
            row.PosIntvl = "default";

            // Refresh UI to hide it immediately
            _viewModel.MergedRowsView?.Refresh();
        }

        // Right-click bulk "turn intervals off" for the selected row — sets the chosen interval
        // flags to filtered (true), i.e. any "ON" cells go blank. Cells are read-only, so this
        // is the way to bulk-filter; edits highlight and persist on save like any other change.
        private void TurnOffBaseIntervals_Click(object sender, RoutedEventArgs e)
        {
            (FilterGrid.SelectedItem as FilterIntervalsViewModel.MergedTickerRow)?.TurnOffBaseIntervals();
        }

        private void TurnOffNonBaseIntervals_Click(object sender, RoutedEventArgs e)
        {
            (FilterGrid.SelectedItem as FilterIntervalsViewModel.MergedTickerRow)?.TurnOffNonBaseIntervals();
        }

        private void TurnOffAllIntervals_Click(object sender, RoutedEventArgs e)
        {
            (FilterGrid.SelectedItem as FilterIntervalsViewModel.MergedTickerRow)?.TurnOffAllIntervals();
        }

        // Changing strategy / strategy_b offers to turn off that side's intervals. Captured on
        // BeginningEdit and compared on CellEditEnding so it only fires on a genuine user change
        // (not load/scroll/virtualization). strategy → non-base intervals; strategy_b → base.
        private string _editOldStrategy;
        private string _editOldStrategyBase;

        private void FilterGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            var row = e.Row?.Item as FilterIntervalsViewModel.MergedTickerRow;
            if (row == null) return;
            switch (e.Column?.Header as string)
            {
                case "strategy": _editOldStrategy = row.StrategyName; break;
                case "strategy_b": _editOldStrategyBase = row.StrategyNameBase; break;
            }
        }

        private void FilterGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;
            var row = e.Row?.Item as FilterIntervalsViewModel.MergedTickerRow;
            if (row == null) return;

            // strategy → non-base intervals; strategy_b → base intervals.
            bool? baseSide = null;
            switch (e.Column?.Header as string)
            {
                case "strategy":
                    if (!string.Equals(row.StrategyName, _editOldStrategy, StringComparison.Ordinal))
                        baseSide = false;
                    break;
                case "strategy_b":
                    if (!string.Equals(row.StrategyNameBase, _editOldStrategyBase, StringComparison.Ordinal))
                        baseSide = true;
                    break;
            }
            if (baseSide == null) return;   // not a strategy column, or value unchanged

            bool isBase = baseSide.Value;
            // Defer the dialog until after the edit fully commits (showing a modal mid-commit is
            // reentrant/focus-unsafe).
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var which = isBase ? "base" : "non-base";
                var result = MessageBox.Show(this,
                    $"Strategy changed for {row.Tickername}.\n\nTurn off all {which} intervals?",
                    "Turn intervals off?", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;
                if (isBase) row.TurnOffBaseIntervals();
                else row.TurnOffNonBaseIntervals();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }


        //private async void AddTickerMenuItem_Click(object sender, RoutedEventArgs e)
        //{
        //    var vm = DataContext as FilterIntervalsViewModel;
        //    if (vm == null) return;

        //    if (vm.TickerUniverse == null || vm.TickerUniverse.Count == 0)
        //        await vm.LoadTickerUniverseAsync();

        //    var dlg = new winAddTicker(vm.TickerUniverse);
        //    if (dlg.ShowDialog() == true)
        //    {
        //        foreach (var tr in dlg.SelectedTickers)
        //        {
        //            var row = vm.CreateDefaultRow(tr.TickerName,tr.FundGroupName,tr.FundName);

        //            // Apply the fund group chosen per row (user picked it in the grid)
        //            // "ALL" can mean no specific group if you prefer null
        //            row.FundGroup = string.IsNullOrWhiteSpace(tr.FundGroupName)
        //                ? null
        //                : tr.FundGroupName.Trim();

        //            //need to change this
        //            row.FundName = "*";
        //            vm.MergedRows.Add(row);
        //        }
        //    }

        //}
        private async void AddTickerMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as FilterIntervalsViewModel;
            if (vm == null) return;

            if (vm.TickerUniverse == null || vm.TickerUniverse.Count == 0)
            {
                await RunWithBusy(async () =>
                {
                    BeginLoad(totalSteps: 1);
                    await SetStatus("Loading ticker universe...");
                    await vm.LoadTickerUniverseAsync();
                    vm.LoadProgress = 100;
                });
            }
            //MessageBox.Show(this, "New ticker test",
            //    "Select Tickers", MessageBoxButton.OK, MessageBoxImage.Information);
            var dlg = new winAddTicker(vm.TickerUniverse);
            if (dlg.ShowDialog() != true) return;

            string Norm(string s) => (s ?? "").Trim();
            bool EqTicker(string a, string b) =>
                string.Equals(Norm(a), Norm(b), StringComparison.OrdinalIgnoreCase);

            foreach (var tr in dlg.SelectedTickers)
            {
                var ticker = Norm(tr.TickerName);
                var fundGroupCheck = string.IsNullOrWhiteSpace(tr.FundGroupName) ? "*" : Norm(tr.FundGroupName);
                var fundNameCheck = string.IsNullOrWhiteSpace(tr.FundName) ? "*" : Norm(tr.FundName);

                // Check if ticker is in the benchmark for the selected fund/fundgroup
                try
                {
                    var ws = new WebServiceData();
                    var check = await ws.IsTickerInBenchmarkAsync(ticker, fundGroupCheck, fundNameCheck);

                    if (!check.InBenchmark)
                    {
                        MessageBox.Show(this,
                            $"Cannot add ticker '{ticker}'.\n\n{check.Detail}",
                            "Not in benchmark", MessageBoxButton.OK, MessageBoxImage.Warning);
                        continue; // skip this ticker
                    }
                }
                catch (Exception ex)
                {
                    var result = MessageBox.Show(this,
                        $"Could not verify benchmark for '{ticker}':\n{ex.Message}\n\nAdd anyway?",
                        "Benchmark check failed", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result != MessageBoxResult.Yes)
                        continue;
                }

                // Find ANY existing rows with same ticker (primary key = ticker only)
                var existingForTicker = vm.MergedRows
                    .Where(r => EqTicker(r.Tickername, ticker))
                    .ToList();

                if (existingForTicker.Count > 0)
                {
                    var msg =
                        $"A row for ticker '{ticker}' already exists " +
                        $"({existingForTicker.Count} entr{(existingForTicker.Count == 1 ? "y" : "ies")}).\n\n" +
                        $"Adding a new one will overwrite the existing entr{(existingForTicker.Count == 1 ? "y" : "ies")}" +
                        $" when saved.\n\n" +
                        $"Do you want to continue?";
                    var result = MessageBox.Show(this, msg, "Overwrite existing?",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (result != MessageBoxResult.Yes)
                        continue; // skip this ticker
                }

                // Build the new row
                var fundGroup = string.IsNullOrWhiteSpace(tr.FundGroupName) ? null : Norm(tr.FundGroupName);
                var fundName = string.IsNullOrWhiteSpace(tr.FundName) ? "*" : tr.FundName;
                var manual = true;
                var newRow = vm.CreateDefaultRow(ticker, fundGroup, fundName);
                newRow.FundGroup = fundGroup;
                newRow.FundName = fundName;
                newRow.Manual = manual; // default to manual

                // Overwrite: remove all existing rows for this ticker, then add the new row
                if (existingForTicker.Count > 0)
                {
                    // Remove in reverse index order to avoid reindexing issues
                    for (int i = vm.MergedRows.Count - 1; i >= 0; i--)
                    {
                        if (EqTicker(vm.MergedRows[i].Tickername, ticker))
                            vm.MergedRows.RemoveAt(i);
                    }
                }

                vm.MergedRows.Add(newRow);
            }

            vm.MergedRowsView?.Refresh();
        }

        private async void ReloadUniverseButton_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as FilterIntervalsViewModel;
            if (vm == null) return;
            await RunWithBusy(async () =>
            {
                BeginLoad(totalSteps: 1);
                await SetStatus("Loading ticker universe...");
                await vm.LoadTickerUniverseAsync();
                vm.LoadProgress = 100;
            });
        }
        /// <summary>
        /// saves data to filters interval table
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private DispatcherTimer _pollTimer;
        private DispatcherTimer _boostTimer;



        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as FilterIntervalsViewModel;
            if (vm == null) return;

            try
            {
                // Stop any previous poll/boost
                _pollTimer?.Stop(); _pollTimer = null;
                _boostTimer?.Stop(); _boostTimer = null;

                vm.IsExecuting = true;
                vm.ResetBoost();
                StatusTextBlock.Text = "Save started…";

                // --- payloads ---
                //var intervalRows = vm.MergedRows.Select(r => r.ToUpsertRow()).ToList();
                var intervalRows = vm.MergedRows
                .Where(r => !r.IsDeleted)   // <-- exclude deleted rows
                .Select(r => r.ToUpsertRow())
                .ToList();


                //for deleting tickers
                var deleteRows = vm.MergedRows
                    .Where(r => r.IsDeleted)     // or a stricter condition if you add an "OriginalIsDeleted"
                    .Select(r => vm.ToDeleteRow(r))
                    .ToList();

                var affectedIntervalTickers = vm.MergedRows
                .Where(r => r.HasAnyEdits)                       // interval-affecting edits only
                .Select(r => r.Tickername)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

                var affectedStrategyTickers = vm.MergedRows
                    .Where(r => r.StrategyNameHasChanged || r.StrategyNameBaseHasChanged || r.MinIntvlHasChanged || r.ContUpdHasChanged || r.PosIntvlHasChanged || r.NeedsNewOverride)
                    .Select(r => r.Tickername)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var tickersToProcess = affectedIntervalTickers
                    .Union(affectedStrategyTickers, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var deletedTickers = vm.MergedRows
                    .Where(r => r.IsDeleted)
                    .Select(r => (r.Tickername ?? "").Trim())
                    .Where(t => t.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                tickersToProcess = tickersToProcess
                    .Union(deletedTickers, StringComparer.OrdinalIgnoreCase)
                    .ToList();



                bool Changed(double? oldV, double? newV)
                    => newV.HasValue && (!oldV.HasValue || Math.Abs(newV.Value - oldV.Value) > 1e-9);

                //var scaledRows = vm.MergedRows
                //    .Where(r => Changed(r.ScaleFactor, r.NewScaleFactor))
                //    .Select(r => r.ToScaledPositionInsertRow())
                //    .ToList();
                var scaledRows = vm.MergedRows
                .Where(r => !r.IsDeleted)
                .Where(r => Changed(r.ScaleFactor, r.NewScaleFactor))
                .Select(r => r.ToScaledPositionInsertRow())
                .ToList();


                //apply any changes to the manual flag
                await vm.ApplyManualChangesAsync_UsingTickerFreezer();


                var nowUtc = DateTime.UtcNow;

                //make sure deleted rows reset strategies_override to defaults. fail safe
                foreach (var r in vm.MergedRows.Where(r => r.IsDeleted))
                {
                    r.StrategyName = "default";
                    r.ContUpd = false;
                    r.MinIntvl = "default";
                    r.PosIntvl = "default";
                }



                var strategyOverridesPayload = vm.MergedRows
                    .Where(r => r.StrategyNameHasChanged || r.StrategyNameBaseHasChanged || r.MinIntvlHasChanged || r.ContUpdHasChanged || r.PosIntvlHasChanged || r.NeedsNewOverride)
                    .Select(r => r.ToStrategyOverrideInsertModel(nowUtc,vm.HostEnv))  // your helper
                    .ToList();

                // Phase 1
                string jobId = await vm.UpsertFilterIntervalsStartAsync(
                    intervalRows,
                    tickersToProcess,
                    strategyOverridesPayload
                );



                // AFTER (wrap in lambda to satisfy the delegate)
                Func<string, Task<FilterIntervalsViewModel.UpsertJobStatus>> pollFunc =
                    id => vm.GetUpsertFilterIntervalsStatusAsync(id);


                bool inScaledPhase = false;

                // client-side boost every minute (unchanged)
                _boostTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
                _boostTimer.Tick += (s2, e2) => { if (vm.IsUpsertRunning) vm.IncreaseBoost(10); };
                _boostTimer.Start();

                // poll every 3 seconds
                _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                _pollTimer.Tick += async (s, args) =>
                {
                    try
                    {
                        var st = await pollFunc(jobId);
                        vm.ApplyJobStatus(st); // keeps ProgressPercent blended with boost

                        var status = st?.Status ?? "running";
                        var msg = st?.Message ?? "";
                        var pct = vm.ProgressPercent;
                        StatusTextBlock.Text = $"{status} {pct}% – {msg}";

                        bool isDone = status.Equals("done", StringComparison.OrdinalIgnoreCase);
                        bool isFailed = status.Equals("failed", StringComparison.OrdinalIgnoreCase);

                        if (isDone || isFailed)
                        {
                            // If phase 1 succeeded and we have changes, start phase 2
                            if (!inScaledPhase && isDone && scaledRows.Count > 0)
                            {
                                inScaledPhase = true;
                                vm.ResetBoost();
                                StatusTextBlock.Text = "Saving scaled positions…";

                                jobId = await vm.InsertScaledPositionsStartAsync(scaledRows);
                                pollFunc = vm.GetInsertScaledPositionsStatusAsync; // swap to scaled-status endpoint
                                return; // keep polling with the same timer
                            }

                            // Otherwise we are finished (either no scaled rows, or phase 2 finished)
                            _pollTimer.Stop(); _pollTimer = null;
                            _boostTimer.Stop(); _boostTimer = null;
                            vm.IsExecuting = false;

                            if (isDone)
                            {
                                // No success modal — feedback comes from the progress bar
                                // + final status text. Failures still get a modal below.
                                await DoFullReloadAsync();
                                StatusTextBlock.Text = $"Saved + Reloaded {DateTime.Now:HH:mm:ss}";
                            }
                            else
                            {
                                MessageBox.Show(this, "Save failed:\n" + (msg ?? "unknown error"), "Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                    catch (Exception pollEx)
                    {
                        _pollTimer?.Stop(); _pollTimer = null;
                        _boostTimer?.Stop(); _boostTimer = null;

                        vm.IsExecuting = false;
                        StatusTextBlock.Text = "failed – " + pollEx.Message;

                        MessageBox.Show(this, "Save status check failed:\n" + pollEx.Message, "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };
                _pollTimer.Start();
            }
            catch (Exception ex)
            {
                _pollTimer?.Stop(); _pollTimer = null;
                _boostTimer?.Stop(); _boostTimer = null;

                vm.IsExecuting = false;
                StatusTextBlock.Text = "failed – " + ex.Message;

                MessageBox.Show(this, "Save failed:\n" + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void ScalePosition_Click(object sender, RoutedEventArgs e)
        {
            var row = FilterGrid.SelectedItem as FilterIntervalsViewModel.MergedTickerRow;

            winScale windowScale = new winScale(null, row.FundName, "", row.Tickername, "filtered",row.ScaleFactor,row.ScaledPercent, this.gbl_conn,
                row.FundGroup);
            bool? result = windowScale.ShowDialog();
        }

        private void ViewScaledPositions_Click(object sender, RoutedEventArgs e)
        {
            new winScaledPositions().Show(); // or ShowDialog()

        }
    }
}
