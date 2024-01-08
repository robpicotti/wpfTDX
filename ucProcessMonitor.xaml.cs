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
using System.Data;
using System.Data.SqlClient;
using TDX;

namespace wpfTDX
{
    /// <summary>
    /// Interaction logic for ucProcessMonitor.xaml
    /// </summary>
    ///
    public partial class ucProcessMonitor : UserControl
    {
        public SqlConnection gbl_conn;
        public ucProcessMonitor(SqlConnection conn)
        {
            gbl_conn = conn;
            InitializeComponent();
            LoadForm();
        }
        private void LoadForm()
        {
            HeartBeats heartbeats = new HeartBeats(gbl_conn);
            foreach (ProcessBeat beat in heartbeats.ListHeartBeats)
            {
                Color itemColor = (Convert.ToInt32(beat.minutes_delta) > beat.age_limit_minutes) ? Colors.Red : Colors.Green;
                ucProcess proc = new ucProcess(beat.process_name, itemColor, gbl_conn);
                proc.Margin = new Thickness(5);

                Border userControlBorder = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(2),
                    Child = proc
                };

                processWrapPanel.Children.Add(userControlBorder); // Add the bordered user control
            }

        }
    }
}
