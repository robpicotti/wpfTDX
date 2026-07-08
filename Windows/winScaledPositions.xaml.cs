using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace wpfTDX
{
    public partial class winScaledPositions : Window
    {
        private readonly ScaledPositionsViewModel _vm = new ScaledPositionsViewModel();

        public winScaledPositions()
        {
            InitializeComponent();
            DataContext = _vm;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await _vm.LoadAsync();
        }

        private async void Reload_Click(object sender, RoutedEventArgs e)
        {
            await _vm.LoadAsync();
        }

        // Right-click selects the row so the context menu acts on it.
        private void Row_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var row = sender as DataGridRow;
            if (row != null) row.IsSelected = true;
        }

        // "Edit scale…" edits the right-clicked row (ANY scale_type). Enabled whenever a row is
        // selected; adding new is manual-only, but editing an existing scale is not.
        private void Grid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            miScaleIn.IsEnabled = Grid.SelectedItem is ScaledPositionsDataModel;
        }

        // Right-click "Edit scale…" on an existing row — preserves the row's scale_type.
        private void ScaleInManual_Click(object sender, RoutedEventArgs e)
        {
            var r = Grid.SelectedItem as ScaledPositionsDataModel;
            if (r == null) return;
            OpenScale(r.FundGroupName, r.FundName, r.TickerName, r.ScaledPercent, r.ScaleType, r.ScaledTimeStep);
        }

        // "Add new…" — pick a ticker/fund via the shared Add-Ticker picker, then scale it manually.
        private async void AddNew_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Fetching the ticker universe can take a moment (only the first time — it's cached),
                // so show the progress bar + "Loading tickers…" until it returns.
                System.Collections.Generic.List<FilterIntervalsViewModel.TickerRow> universe;
                _vm.BeginBusy("Loading tickers…");
                try { universe = await _vm.GetTickerUniverseAsync(); }
                finally { _vm.EndBusy(); }

                var dlg = new winAddTicker(universe) { Owner = this };
                if (dlg.ShowDialog() != true || dlg.SelectedTickers == null || dlg.SelectedTickers.Count == 0)
                    return;

                var tr = dlg.SelectedTickers[0];   // scale the first selected ticker
                var fundGroup = string.IsNullOrWhiteSpace(tr.FundGroupName) ? "*" : tr.FundGroupName.Trim();
                var fundName = string.IsNullOrWhiteSpace(tr.FundName) ? "*" : tr.FundName.Trim();
                // Adding new is manual-only.
                OpenScale(fundGroup, fundName, (tr.TickerName ?? "").Trim(), 1.0, "manual", null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Add new", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Opens the shared scaling popup for the given scale_type (inserts via the API, no
        // SqlConnection), then reloads on success. Adding new always passes "manual"; editing
        // passes the existing row's scale_type so it is preserved.
        private async void OpenScale(string fundGroup, string fundName, string ticker,
                                     double? scaledPercent, string scaleType, double? scaledTimeStep)
        {
            if (string.IsNullOrWhiteSpace(ticker)) return;

            await ScaleLimits.EnsureLoadedAsync();   // so winScale validates against the server cap

            var type = string.IsNullOrWhiteSpace(scaleType) ? "manual" : scaleType.Trim();
            var win = new winScale(null, fundName, "", ticker, type,
                                   scaledPercent, scaledPercent, null, fundGroup) { Owner = this };
            // Route every scaled-positions insert through the API (this screen has no SqlConnection),
            // preserve the row's cadence, and give the popup the current rows so it can warn whether
            // this scale will apply for the fund or be superseded by an existing one.
            win.UseApiInsert = true;
            win.ExistingTimeStep = scaledTimeStep;
            win.ExistingScales = new System.Collections.Generic.List<ScaledPositionsDataModel>(_vm.Rows);
            if (win.ShowDialog() == true)
                await _vm.LoadAsync();
        }
    }
}
