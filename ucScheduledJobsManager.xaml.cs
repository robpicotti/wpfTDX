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
    /// Interaction logic for ucScheduledJobsManager.xaml
    /// </summary>
    public partial class ucScheduledJobsManager : UserControl
    {
        public ScheduleJobViewModel ViewModel { get; set; }
        public event EventHandler RemoveControlRequested;
        public ucScheduledJobsManager()
        {
            InitializeComponent();
            this.ViewModel = new ScheduleJobViewModel();
            this.DataContext = ViewModel;
            if(ViewModel!=null)
            {
                ViewModel.SaveCompleted += OnSaveCompleted;
                ViewModel.BeginProcess += OnBeginProcess;

            }
        }

        private void cmdClose_Click(object sender, RoutedEventArgs e)
        {
            RemoveControlRequested?.Invoke(this, EventArgs.Empty);
        }
        private void OnSaveCompleted()
        {
            txtProcess.Visibility = Visibility.Hidden;
            // Show the message box
            MessageBox.Show("Jobs saved successfully!", "Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void OnBeginProcess()
        {
            txtProcess.Visibility = Visibility.Visible;
            txtProcess.Text = "Processing";
            txtProcess.Background = new SolidColorBrush(Colors.Red);
        }
    }
}
