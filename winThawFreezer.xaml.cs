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
using TDX;
using System.Data.SqlClient;
namespace wpfTDX
{
    /// <summary>
    /// Interaction logic for winThawFreezer.xaml
    /// </summary>
    public partial class winThawFreezer : Window
    {
        TDX.db _db = new TDX.db();
        SqlConnection gbl_conn;
        string FUNDNAME = "";
        string EXECACCOUNTNAME = "";
        string SUBACCOUNTNAME = "";
        string TAD_ID = "";
        string TICKERNAME = "";
        string BENCHMARKNAME = "";
        string BROKER_CODE_EXEC = "";
        string EMSNAME = "";
        string ERROR_CODE = "";
        string RUN_TIME = "";
        ucTickerFreezer MAIN_FORM;
        TickerFreezer TFR;
        public winThawFreezer(string runtime,string fundname,string execaccountname,
            string subaccountname,string tad_id,string tickername,
            string benchmarkname, string broker_code_exec,
            string emsname,string error_code,
            ucTickerFreezer mainForm, SqlConnection conn)
        {
            InitializeComponent();

            gbl_conn = conn;
            FUNDNAME = fundname;
            EXECACCOUNTNAME = execaccountname;
            SUBACCOUNTNAME = subaccountname;
            TAD_ID = tad_id;
            TICKERNAME = tickername;
            BENCHMARKNAME = benchmarkname;
            BROKER_CODE_EXEC = broker_code_exec;
            EMSNAME = emsname;
            ERROR_CODE = error_code;
            MAIN_FORM = mainForm;
            RUN_TIME = runtime;
            TFR = new TickerFreezer(gbl_conn);
            LoadForm();
        }
        private void LoadForm()
        {
            string message = "Are you sure you want to thaw an item from the freezer with the following details?";
            message += "\r\n";
            message += "\r\n";
            message += "fund name: " + FUNDNAME;
            message += "\r\n";
            message += "execution account name: " + EXECACCOUNTNAME;
            message += "\r\n";
            message += "subaccount name: " + SUBACCOUNTNAME;
            message += "\r\n";
            message += "tad_id: " + TAD_ID;
            message += "\r\n";
            message += "tickername: " + TICKERNAME;
            message += "\r\n";
            message += "benchmark name: " + BENCHMARKNAME;
            message += "\r\n";
            txtDetails.Text = message;
        }

        private void cmdYes_Click(object sender, RoutedEventArgs e)
        {
            if (cmdYes.Content.ToString() == "Thaw")
            {
                Thaw();
            }
            else
            {
                cmdYes.Content = "Thaw";
                txtReason.Visibility = Visibility.Visible;
                cmdNo.Visibility = Visibility.Hidden;
                this.Height = this.Height + 250;
            }
        }
        /// <summary>
        /// thawing an asset
        /// </summary>
        private void Thaw()
        {
            try
            {
                string sql_text = TFR.Thaw(RUN_TIME, EMSNAME, BROKER_CODE_EXEC, FUNDNAME, SUBACCOUNTNAME, EXECACCOUNTNAME, TAD_ID, TICKERNAME, ERROR_CODE, BENCHMARKNAME, txtReason.Text, gbl_conn);
                MessageBox.Show("Item thawed successfully. Update statement: " + sql_text, "Ticker Freezer", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                string message = "Thawing failed with error: " + ex.Message;
                MessageBox.Show(message);
            }
            finally
            {
                MAIN_FORM.Refresh();
                this.Close();
            }
        }

        private void cmdNo_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        

    }
}
