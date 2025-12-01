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
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace wpfTDX
{
    /// <summary>
    /// Interaction logic for ucProcessMonitor.xaml
    /// </summary>
    ///
    public partial class ucProcessMonitor : UserControl
    {
        public SqlConnection gbl_conn;

        private string _hostenvironment;
        public string HostEnvironment
        {
            get => _hostenvironment;
            set
            {
                if(this._hostenvironment != value)
                {
                    this._hostenvironment = value;
                }
            }
        }
        public ucProcessMonitor(SqlConnection conn)
        {
            gbl_conn = conn;
            InitializeComponent();
            this.Loaded += UcProcessMonitor_Loaded;


        }
        private void LoadForm()
        {
            HeartBeats heartbeats = new HeartBeats(gbl_conn,this.HostEnvironment);
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
        private async Task<string> GetHostEnvironment()
        {
            try
            {
                string url = "http://localhost:5001/get_hostenv";
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.PostAsync(url, null);
                    response.EnsureSuccessStatusCode();

                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    JObject json = JObject.Parse(jsonResponse);
                    return json["hostenv"]?.ToString();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error getting host environment: " + ex.Message);
                return "";
            }

        }
        private async Task InitializeAsync()
        {
            this.HostEnvironment = await GetHostEnvironment();
            LoadForm();
        }
        private async void UcProcessMonitor_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= UcProcessMonitor_Loaded; // prevent double execution
            await InitializeAsync();
        }

    }

}
