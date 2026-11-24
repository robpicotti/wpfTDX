using System.Windows;

namespace wpfTDX
{
    public partial class winSystemTradeReview : Window
    {
        private readonly SystemTradeReviewViewModel _viewModel;

        public winSystemTradeReview(SystemTradeReviewViewModel viewModel)
        {
            _viewModel = viewModel;
            InitializeComponent();
            DataContext = viewModel;
            Closing += (_, e) => { if (_viewModel.IsBusy) e.Cancel = true; };
        }

        private void ExitNowClick(object sender, RoutedEventArgs e) => _viewModel.UseExitTimeNow();

        private async void SaveClick(object sender, RoutedEventArgs e)
        {
            if (await _viewModel.SaveAsync()) DialogResult = true;
        }

        private async void ReloadClick(object sender, RoutedEventArgs e)
            => await _viewModel.ReloadAsync();

        private void CancelClick(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
