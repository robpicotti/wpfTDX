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
        List<string> lstNumericColumns = new List<string>();
        public DataTable dtEquityPosnPnl;
        public DataTable dtEquityNewDealsPnl;
        public DataTable dtFuturesPosnPnl;
        public DataTable dtFuturesNewDealsPnl;

        public ucMtm(SqlConnection conn, string default_fund)
        {
            InitializeComponent();
            gbl_conn = conn;
            LoadForm();  
        }
        private void LoadForm()
        {
            dgEquities.AutoGeneratingColumn += dgEquities_AutoGeneratingColumn;
            dgEquitiesNewDeals.AutoGeneratingColumn += dgEquities_AutoGeneratingColumn;
            dgFutures.AutoGeneratingColumn += dgEquities_AutoGeneratingColumn;
            dgFuturesNewDeals.AutoGeneratingColumn += dgEquities_AutoGeneratingColumn;
            dtAccounts = _db.get_funds(gbl_conn);
            cboFundname.Items.Clear();
            foreach (DataRow row in dtAccounts.Rows)
            {
                cboFundname.Items.Add(row["fundname"].ToString());
            }
            cboFundname.SelectedItem = DEFAULT_FUND;
            BuildNumericColumnLists();

        }
        /// <summary>
        /// populates the list of numeric columns that need to have thousand seperator
        /// </summary>
        private void BuildNumericColumnLists()
        {
            lstNumericColumns.Add("total_pnl");
            lstNumericColumns.Add("mtm");
            lstNumericColumns.Add("mtm_t1");
            lstNumericColumns.Add("posn_pnl");
            lstNumericColumns.Add("posn_new_deals");
            lstNumericColumns.Add("new_deals_pnl");
            lstNumericColumns.Add("newdeal_pnl");
            lstNumericColumns.Add("new_deals");
            lstNumericColumns.Add("new_deals_exchcurr");
            lstNumericColumns.Add("commission");
            lstNumericColumns.Add("consideration");
            lstNumericColumns.Add("new_deal_pnl_exclComm");
        }

        private void dgEquities_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (lstNumericColumns.Contains( e.PropertyName) )
            {
                var textColumn = e.Column as DataGridTextColumn;
                if (textColumn != null)
                {
                    // Set the StringFormat to "N" for thousand separator
                    textColumn.Binding = new Binding(e.PropertyName) { StringFormat = "N" };
                }
            }
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
                posn_date_t1 = dtFrom.SelectedDate.Value.ToString("yyyy-MM-dd");
            }
            else
            {
                posn_date_t1 = DateTime.Today.ToString("yyyy-MM-dd");
            }
            if (dtTo.SelectedDate != null)
            {
                posn_date = dtTo.SelectedDate.Value.ToString("yyyy-MM-dd");
            }
            else
            {
                posn_date = DateTime.Today.ToString("yyyy-MM-dd");
            }

            DataSet ds = _db.get_futures_pnl(fundName, posn_date, posn_date_t1, gbl_conn);

            if (ds.Tables.Count > 1)
            {
                DataView dataView = ds.Tables[1].DefaultView;
                dgFutures.ItemsSource = dataView;
                dgFuturesNewDeals.ItemsSource = ds.Tables[0].DefaultView;
                dtFuturesPosnPnl = ds.Tables[1];
                dtFuturesNewDealsPnl = ds.Tables[0];
            }
        }
        private void GetEquitiesPnl()
        {
            string posn_date = "";
            string posn_date_t1 = "";
            string fundName = cboFundname.Text;
            if (dtFrom.SelectedDate != null)
            {
                posn_date_t1 = dtFrom.SelectedDate.Value.ToString("yyyy-MM-dd");
            }
            else
            {
                posn_date_t1 = DateTime.Today.ToString("yyyy-MM-dd");
            }
            if (dtTo.SelectedDate != null)
            {
                posn_date = dtTo.SelectedDate.Value.ToString("yyyy-MM-dd");
            }
            else
            {
                posn_date = DateTime.Today.ToString("yyyy-MM-dd");
            }
            DataSet ds = _db.get_equities_pnl(fundName, posn_date, posn_date_t1, gbl_conn);
            if (ds.Tables.Count > 1)
            {
                DataView dataView = ds.Tables[1].DefaultView;
                dgEquities.ItemsSource = dataView;
                dgEquitiesNewDeals.ItemsSource = ds.Tables[0].DefaultView;
                dtEquityPosnPnl = ds.Tables[1];
                dtEquityNewDealsPnl = ds.Tables[0];
            }
            decimal equity_posn_pnl = 0;
            decimal equity_new_deals = 0;
            decimal equity_total_pnl = 0;

            if (dtEquityPosnPnl.Rows.Count > 0)
            {
                equity_posn_pnl = Convert.ToDecimal(dtEquityPosnPnl.Compute("SUM(posn_pnl)", String.Empty));
                equity_new_deals = Convert.ToDecimal(dtEquityPosnPnl.Compute("SUM(new_deals_pnl)", String.Empty));
                equity_total_pnl = Convert.ToDecimal(dtEquityPosnPnl.Compute("SUM(total_pnl)", String.Empty));
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
