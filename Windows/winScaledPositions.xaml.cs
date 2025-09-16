using System.Windows;

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
    }
}
