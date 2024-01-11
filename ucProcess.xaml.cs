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
using TDX;
using System.Data.SqlClient;
using System.Windows.Threading;

namespace wpfTDX
{
    /// <summary>
    /// Interaction logic for ucProcess.xaml
    /// </summary>
    public partial class ucProcess : UserControl
    {
        string PROCESS_NAME;
        Color STATUS_COLOUR;
        SqlConnection GBL_CONN;
        private DispatcherTimer timer;
        private int i = 1;
        public ucProcess(string processName,Color colour,SqlConnection conn )
        {
            InitializeComponent();
            STATUS_COLOUR = colour;
            PROCESS_NAME = processName;
            GBL_CONN = conn;
            LoadForm();

            // Initialize and start the timer
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMinutes(1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Timer tick event, execute the Refresh method
            Refresh();
        }
        private void LoadForm()
        {
            SolidColorBrush brush = new SolidColorBrush(STATUS_COLOUR);
            txtStatus.Text = PROCESS_NAME + "\n" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
            txtStatus.Background = brush;
        }

        private void Refresh()
        {
            HeartBeats heartbeat = new HeartBeats(GBL_CONN);
            foreach(ProcessBeat beat in heartbeat.ListHeartBeats)
            {
                if(beat.process_name==PROCESS_NAME)
                {
                    STATUS_COLOUR = (Convert.ToInt32(beat.minutes_delta) > beat.age_limit_minutes) ? Colors.Red : Colors.Green;
                    SolidColorBrush brush = new SolidColorBrush(STATUS_COLOUR);
                    txtStatus.Background = brush;
                    txtStatus.Text = PROCESS_NAME  +"\n" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
                }
            }
        }
    }
}
