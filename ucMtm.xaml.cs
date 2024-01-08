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
    /// Interaction logic for ucMtm.xaml
    /// </summary>
    public partial class ucMtm : UserControl
    {
        public SqlConnection gbl_conn;
        public DataTable dtAccounts;
        public string DEFAULT_FUND = "";
        TDX.db _db = new TDX.db();
        public event EventHandler RemoveControlRequested;
        public ucMtm(SqlConnection conn, string default_fund)
        {
            InitializeComponent();
            gbl_conn = conn;
            LoadForm();
        }
        private void LoadForm()
        {
            dtAccounts = _db.get_funds(gbl_conn);
            cboFundname.Items.Clear();
            foreach (DataRow row in dtAccounts.Rows)
            {
                cboFundname.Items.Add(row["fundname"].ToString());
            }
            cboFundname.SelectedItem = DEFAULT_FUND;
        }

        private void cmdClose_Click(object sender, RoutedEventArgs e)
        {
            RemoveControlRequested?.Invoke(this, EventArgs.Empty);
        }


        private void GetFuturesPnl()
        {
            string posn_date = "";
            string posn_date_t1 = "";
            string fundName = cboFundname.Text;
            if (dtFrom.SelectedDate != null)
            {
                posn_date = dtFrom.SelectedDate.Value.ToString("yyyy-MM-dd");
            }
            else
            {
                posn_date = DateTime.Today.ToString("yyyy-MM-dd");
            }
            if (dtTo.SelectedDate != null)
            {
                posn_date_t1 = dtTo.SelectedDate.Value.ToString("yyyy-MM-dd");
            }
            else
            {
                posn_date_t1 = DateTime.Today.ToString("yyyy-MM-dd");
            }

            DataSet ds = _db.get_futures_pnl(fundName, posn_date, posn_date_t1, gbl_conn);

            if (ds.Tables.Count > 1)
            {
                DataView dataView = ds.Tables[1].DefaultView;
                dgFutures.ItemsSource = dataView;
            }
        }
        private void GetEquitiesPnl()
        {
            string posn_date = "";
            string posn_date_t1 = "";
            string fundName = cboFundname.Text;
            if (dtFrom.SelectedDate != null)
            {
                posn_date = dtFrom.SelectedDate.Value.ToString("yyyy-MM-dd");
            }
            else
            {
                posn_date = DateTime.Today.ToString("yyyy-MM-dd");
            }
            if (dtTo.SelectedDate != null)
            {
                posn_date_t1 = dtTo.SelectedDate.Value.ToString("yyyy-MM-dd");
            }
            else
            {
                posn_date_t1 = DateTime.Today.ToString("yyyy-MM-dd");
            }
            DataSet ds = _db.get_equities_pnl(fundName, posn_date, posn_date_t1, gbl_conn);
            if (ds.Tables.Count > 1)
            {
                DataView dataView = ds.Tables[1].DefaultView;
                dgEquities.ItemsSource = dataView;
                dgEquitiesNewDeals.ItemsSource = ds.Tables[0].DefaultView;
            }
        }

        private void cmdRun_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            GetEquitiesPnl();
            GetFuturesPnl();
            Cursor = Cursors.Arrow;
        }
    }
}
