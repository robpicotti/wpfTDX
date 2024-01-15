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
        public winThawFreezer(string fundname,string execaccountname,
            string subaccountname,string tad_id,string tickername,
            string benchmarkname, SqlConnection conn)
        {
            InitializeComponent();

            gbl_conn = conn;
            FUNDNAME = fundname;
            EXECACCOUNTNAME = execaccountname;
            SUBACCOUNTNAME = subaccountname;
            TAD_ID = tad_id;
            TICKERNAME = tickername;
            BENCHMARKNAME = benchmarkname;
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

    }
}
