using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
            _viewModel = new FilterIntervalsViewModel();
            DataContext = _viewModel;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.LoadTickerUniverseAsync();
            await _viewModel.LoadFilterIntervalsDataAsync();
            await _viewModel.LoadTadPositionsDataAsync();
            await _viewModel.RebuildMerged();
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
                case "lo_only": flagProp = "LongOnly"; return true;
                case "so_only": flagProp = "ShortOnly"; return true;
                case "buy_only": flagProp = "BuyOnly"; return true;
                case "sell_only": flagProp = "SellOnly"; return true;
                case "filt_intvls": flagProp = "AllIntervals"; return true;

                // base_* timeframes
                case "base_y1": flagProp = "BaseY1"; posProp = "PositionBaseY1"; return true;
                case "base_h1": flagProp = "BaseH1"; posProp = "PositionBaseH1"; return true;
                case "base_d1": flagProp = "BaseD1"; posProp = "PositionBaseD1"; return true;

                // y* timeframes
                case "y1": flagProp = "y1"; posProp = "PositionY1"; return true;
                case "y2": flagProp = "y2"; posProp = "PositionY2"; return true;
                case "y3": flagProp = "y3"; posProp = "PositionY3"; return true;

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
            await RunWithBusy(async () =>
            {
                await _viewModel.LoadTadPositionsDataAsync();
                await _viewModel.RebuildMerged(preserveUserFiFlags: true);
            });
        }

        private async void ReloadFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            await RunWithBusy(async () =>
            {
                await _viewModel.LoadFilterIntervalsDataAsync();
                await _viewModel.RebuildMerged(preserveUserFiFlags: false);
            });
        }

        private async void ReloadBothButton_Click(object sender, RoutedEventArgs e)
        {
            await RunWithBusy(async () =>
            {
                await Task.WhenAll(
                    _viewModel.LoadFilterIntervalsDataAsync(),
                    _viewModel.LoadTadPositionsDataAsync()
                );
                await _viewModel.RebuildMerged();
            });
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
            _viewModel.MergedRows.Remove(row);
        }

        private async void AddTickerMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as FilterIntervalsViewModel;
            if (vm == null) return;

            if (vm.TickerUniverse == null || vm.TickerUniverse.Count == 0)
                await vm.LoadTickerUniverseAsync();

            var dlg = new winAddTicker(vm.TickerUniverse);
            if (dlg.ShowDialog() == true)
            {
                foreach (var tr in dlg.SelectedTickers)
                {
                    var row = vm.CreateDefaultRow(tr.TickerName,tr.FundGroupName,tr.FundName);

                    // Apply the fund group chosen per row (user picked it in the grid)
                    // "ALL" can mean no specific group if you prefer null
                    row.FundGroup = string.IsNullOrWhiteSpace(tr.FundGroupName)
                        ? null
                        : tr.FundGroupName.Trim();

                    //need to change this
                    row.FundName = "*";
                    vm.MergedRows.Add(row);
                }
            }

        }
        private async void ReloadUniverseButton_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as FilterIntervalsViewModel;
            if (vm == null) return;
            await vm.LoadTickerUniverseAsync();
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
                var intervalRows = vm.MergedRows.Select(r => r.ToUpsertRow()).ToList();

                bool Changed(double? oldV, double? newV)
                    => newV.HasValue && (!oldV.HasValue || Math.Abs(newV.Value - oldV.Value) > 1e-9);

                var scaledRows = vm.MergedRows
                    .Where(r => Changed(r.ScaleFactor, r.NewScaleFactor))
                    .Select(r => r.ToScaledPositionInsertRow())
                    .ToList();

                // --- Phase 1: intervals job ---
                string jobId = await vm.UpsertFilterIntervalsStartAsync(intervalRows);



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
                                var okMsg = inScaledPhase
                                    ? "Saved filter intervals and scaled positions."
                                    : "Saved filter intervals.";
                                MessageBox.Show(this, okMsg, "Save",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
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


        //private async void Button_Click(object sender, RoutedEventArgs e)
        //{
        //    var vm = DataContext as FilterIntervalsViewModel;
        //    if (vm == null) return;

        //    try
        //    {
        //        // Stop any previous poll/boost
        //        if (_pollTimer != null) { _pollTimer.Stop(); _pollTimer = null; }
        //        if (_boostTimer != null) { _boostTimer.Stop(); _boostTimer = null; }

        //        vm.IsExecuting = true;
        //        vm.ResetBoost(); // <— start boost from 0
        //        StatusTextBlock.Text = "Save started…";

        //        var rows = vm.MergedRows.Select(r => r.ToUpsertRow()).ToList();
        //        // Changed-scale rows (treat nulls carefully; only send when NewScaleFactor has a value and differs)
        //        bool Changed(double? a, double? b)
        //            => b.HasValue && (!a.HasValue || Math.Abs(b.Value - a.Value) > 1e-9);

        //        var scaledRows = vm.MergedRows
        //            .Where(r => Changed(r.ScaleFactor, r.NewScaleFactor))
        //            .Select(r => r.ToScaledPositionInsertRow())  // implement this similar to ToUpsertRow()
        //            .ToList();

        //        string jobId = await vm.UpsertFilterIntervalsStartAsync(rows);

        //        // Start the +10%/minute visual boost (capped at 90 while running)
        //        _boostTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        //        _boostTimer.Tick += (s2, e2) =>
        //        {
        //            if (vm.IsUpsertRunning) vm.IncreaseBoost(10);
        //        };
        //        _boostTimer.Start();

        //        // Poll every 3 seconds (unchanged cadence)
        //        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        //        _pollTimer.Tick += async (s, args) =>
        //        {
        //            try
        //            {
        //                var st = await vm.GetUpsertFilterIntervalsStatusAsync(jobId);

        //                // Keep VM in sync so ProgressPercent (and bar) blend server+boost
        //                vm.ApplyJobStatus(st); // <— IMPORTANT

        //                var status = st?.Status ?? "running";
        //                var msg = st?.Message ?? "";

        //                // Show the displayed (blended) percent, not raw server pct
        //                var displayPct = vm.ProgressPercent;
        //                StatusTextBlock.Text = $"{status} {displayPct}% – {msg}";

        //                if (string.Equals(status, "done", StringComparison.OrdinalIgnoreCase) ||
        //                    string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase))
        //                {
        //                    _pollTimer.Stop(); _pollTimer = null;
        //                    _boostTimer.Stop(); _boostTimer = null;

        //                    vm.IsExecuting = false;

        //                    if (string.Equals(status, "done", StringComparison.OrdinalIgnoreCase))
        //                    {
        //                        // vm.ProgressPercent will now be 100 via EffectivePercent
        //                        MessageBox.Show(this, "Saved filter intervals.", "Save",
        //                            MessageBoxButton.OK, MessageBoxImage.Information);
        //                    }
        //                    else
        //                    {
        //                        MessageBox.Show(this, "Save failed:\n" + (msg ?? "unknown error"), "Error",
        //                            MessageBoxButton.OK, MessageBoxImage.Error);
        //                    }
        //                }
        //            }
        //            catch (Exception pollEx)
        //            {
        //                _pollTimer?.Stop(); _pollTimer = null;
        //                _boostTimer?.Stop(); _boostTimer = null;

        //                vm.IsExecuting = false;
        //                StatusTextBlock.Text = "failed – " + pollEx.Message;

        //                MessageBox.Show(this, "Save status check failed:\n" + pollEx.Message, "Error",
        //                    MessageBoxButton.OK, MessageBoxImage.Error);
        //            }
        //        };
        //        _pollTimer.Start();
        //    }
        //    catch (Exception ex)
        //    {
        //        _pollTimer?.Stop(); _pollTimer = null;
        //        _boostTimer?.Stop(); _boostTimer = null;

        //        vm.IsExecuting = false;
        //        StatusTextBlock.Text = "failed – " + ex.Message;

        //        MessageBox.Show(this, "Save failed:\n" + ex.Message, "Error",
        //            MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}

        private void ScalePosition_Click(object sender, RoutedEventArgs e)
        {
            var row = FilterGrid.SelectedItem as FilterIntervalsViewModel.MergedTickerRow;

            winScale windowScale = new winScale(null, row.FundName, "", row.Tickername, "filtered",row.ScaleFactor,row.ScaledPercent, this.gbl_conn);
            bool? result = windowScale.ShowDialog();
        }

        private void ViewScaledPositions_Click(object sender, RoutedEventArgs e)
        {
            new winScaledPositions().Show(); // or ShowDialog()

        }
    }
}
