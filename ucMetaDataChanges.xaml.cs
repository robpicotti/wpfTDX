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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace wpfTDX
{
    /// <summary>
    /// Interaction logic for ucMetaDataChanges.xaml
    /// </summary>
    public partial class ucMetaDataChanges : UserControl
    {
        public MetadataTableChangesViewModel ViewModel { get; set; }
        public event EventHandler RemoveControlRequested;
        public ucMetaDataChanges()
        {
            InitializeComponent();
            this.ViewModel = new MetadataTableChangesViewModel();
            this.DataContext = this.ViewModel;
        }
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await ViewModel.GetMetaDataTableChanges();
                MessageBox.Show("Get metadata changes completed. If no data is visible, no metadata was changed in the last two days", "Load Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load data: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            RemoveControlRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
