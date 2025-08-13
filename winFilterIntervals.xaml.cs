using System;
using System.Collections.Generic;
using System.Linq;
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
            await _viewModel.LoadFilterIntervalsDataAsync();
            await _viewModel.LoadTadPositionsDataAsync();
            await _viewModel.RebuildMerged();
        }

    }
}
