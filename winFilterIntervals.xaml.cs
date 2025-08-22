using System;
using System.Collections.Generic;
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

        public winFilterIntervals()
        {
            InitializeComponent();

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
                    var row = vm.CreateDefaultRow(tr.TickerName,tr.FundGroupName);

                    // Apply the fund group chosen per row (user picked it in the grid)
                    // "ALL" can mean no specific group if you prefer null
                    row.FundGroup = string.Equals(tr.FundGroupName, "ALL", StringComparison.OrdinalIgnoreCase)
                                    ? null
                                    : tr.FundGroupName;

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

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as FilterIntervalsViewModel;
            if (vm == null) return;

            try
            {
                // Stop any previous poller
                if (_pollTimer != null)
                {
                    _pollTimer.Stop();
                    _pollTimer = null;
                }

                vm.IsExecuting = true;
                StatusTextBlock.Text = "Save started…";

                // Prepare payload
                var rows = vm.MergedRows.Select(r => r.ToUpsertRow()).ToList();

                // Kick off long-running server job -> returns jobId
                string jobId = await vm.UpsertFilterIntervalsStartAsync(rows);

                // Immediately reflect "queued" in the VM so the bound bar/message show up
                vm.ApplyJobStatus(new FilterIntervalsViewModel.UpsertJobStatus
                {
                    JobId = jobId,
                    Status = "queued",
                    Progress = 0,
                    Message = "Queued"
                });

                // Poll every 3 seconds
                _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                _pollTimer.Tick += async (s, args) =>
                {
                    try
                    {
                        var st = await vm.GetUpsertFilterIntervalsStatusAsync(jobId);

                        // Update the VM so XAML bindings refresh (ProgressPercent/StatusMessage/IsUpsertRunning)
                        vm.ApplyJobStatus(st);

                        // Optional extra text in the top StatusBar
                        StatusTextBlock.Text = $"{st.Status} {st.EffectivePercent}% – {st.Message}";

                        if (st.IsTerminal)
                        {
                            _pollTimer.Stop();
                            _pollTimer = null;

                            vm.IsExecuting = false; // re-enable Save button now

                            if (string.Equals(st.Status, "done", StringComparison.OrdinalIgnoreCase))
                            {
                                MessageBox.Show(this, "Saved filter intervals.", "Save",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            else
                            {
                                MessageBox.Show(this, "Save failed:\n" + (st.Message ?? "unknown error"), "Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            }

                            // Optional: hide the bound progress bar after completion
                            vm.ClearJobStatus();
                        }
                    }
                    catch (Exception pollEx)
                    {
                        _pollTimer.Stop();
                        _pollTimer = null;

                        vm.IsExecuting = false;
                        StatusTextBlock.Text = "failed – " + pollEx.Message;

                        // Reflect failure in the VM so the bound UI updates too
                        vm.ApplyJobStatus(new FilterIntervalsViewModel.UpsertJobStatus
                        {
                            JobId = jobId,
                            Status = "failed",
                            Message = pollEx.Message,
                            Progress = 0
                        });

                        MessageBox.Show(this, "Save status check failed:\n" + pollEx.Message, "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };
                _pollTimer.Start();
            }
            catch (Exception ex)
            {
                vm.IsExecuting = false;
                StatusTextBlock.Text = "failed – " + ex.Message;

                // Reflect failure
                vm.ApplyJobStatus(new FilterIntervalsViewModel.UpsertJobStatus
                {
                    Status = "failed",
                    Message = ex.Message,
                    Progress = 0
                });

                MessageBox.Show(this, "Save failed:\n" + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // NOTE: no finally resetting IsExecuting; we flip it off when the job ends.
        }

    }
}
