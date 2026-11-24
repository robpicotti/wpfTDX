using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace wpfTDX
{
    public partial class winSystemTrades : Window
    {
        private readonly SystemTradesViewModel _viewModel;
        private readonly DispatcherTimer _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(15)
        };
        private bool _started, _closed, _reviewOpen;

        public winSystemTrades() : this(new SystemTradesViewModel()) { }

        public winSystemTrades(SystemTradesViewModel viewModel)
        {
            _viewModel = viewModel;
            InitializeComponent();
            DataContext = _viewModel;
            _refreshTimer.Tick += async (_, __) =>
            {
                if (!_closed && !_reviewOpen) await _viewModel.RefreshAsync();
            };
            Loaded += async (_, __) =>
            {
                if (_started) return;
                _started = true;
                await _viewModel.RefreshAsync();
                if (!_closed && !_reviewOpen) _refreshTimer.Start();
            };
            Closed += (_, __) =>
            {
                _closed = true;
                _refreshTimer.Stop();
                _viewModel.Dispose();
            };
        }

        private async void ReviewClick(object sender, RoutedEventArgs e)
            => await OpenReviewAsync();

        private async void TradeRowDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var row = sender as DataGridRow;
            var trade = row?.Item as SystemTradeDataModel;
            if (e.ChangedButton != MouseButton.Left || trade == null || !_viewModel.CanReview) return;
            e.Handled = true;
            _viewModel.SelectedTrade = trade;
            await OpenReviewAsync();
        }

        private async Task OpenReviewAsync()
        {
            if (_closed || _reviewOpen || !_viewModel.CanReview) return;
            _reviewOpen = true;
            _refreshTimer.Stop();
            try
            {
                using (var review = await _viewModel.BeginReviewAsync())
                {
                    if (review == null || _closed || !IsLoaded) return;
                    var dialog = new winSystemTradeReview(review) { Owner = this };
                    dialog.ShowDialog();
                    _viewModel.ApplyReviewResult(review);
                }
            }
            finally
            {
                _reviewOpen = false;
                if (!_closed) _refreshTimer.Start();
            }
        }

        private async void FilterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_started || _closed || _reviewOpen) return;
            var picker = sender as ComboBox;
            var value = picker?.SelectedItem as string;
            if (value == null) return;
            if ((string)picker.Tag == "Review") _viewModel.ReviewFilter = value;
            else _viewModel.TradeStatusFilter = value;
            await _viewModel.ApplyFiltersAsync();
        }

        private async void FilterKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || _reviewOpen || _closed) return;
            e.Handled = true;
            await _viewModel.ApplyFiltersAsync();
        }
    }
}
