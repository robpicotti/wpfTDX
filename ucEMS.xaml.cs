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
    /// Interaction logic for ucEMS.xaml
    /// </summary>
    public partial class ucEMS : UserControl
    {
        public event EventHandler RemoveControlRequested;
        DataTable dtFunds;
        SqlConnection gbl_conn;
        TDX.db _db = new TDX.db();
        string DEFAULT_FUND = "";
        DataTable dtExecutions;
        DataTable dtTransactions;
        DataTable dtOrders;
        DataTable dtOpenOrders;
        public ucEMS(SqlConnection conn,string default_fund)
        {
            InitializeComponent();
            gbl_conn = conn;
            DEFAULT_FUND = default_fund;
            LoadForm();
        }
        private void LoadForm()
        {
            //subscribe to the formatting event
            //dgCashPosition.AutoGeneratingColumn += AutoGeneratingColumn;
            dtFunds = _db.get_funds(gbl_conn);
            cboFundname.Items.Clear();
            foreach (DataRow row in dtFunds.Rows)
            {
                cboFundname.Items.Add(row["fundname"].ToString());
            }
            cboFundname.SelectedValue = DEFAULT_FUND;
            dtPickerFrom.SelectedDate = DateTime.Today;
            dtPickerTo.SelectedDate = DateTime.Today;
        }
        private void cmdClose_Click(object sender, RoutedEventArgs e)
        {
            RemoveControlRequested?.Invoke(this, EventArgs.Empty);
        }

        private void cmdRun_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            try
            {
                GetExecutions();
                GetTransactions();
                GetOrders();
                MessageBox.Show("ems data retrieved", "ems data", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "EMS query", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }
        private void GetExecutions()
        {
            DateTime endDate = (DateTime)dtPickerTo.SelectedDate;
            endDate = endDate.AddHours(23).AddMinutes(59).AddSeconds(59);
            dtExecutions = _db.get_fund_executions(
                cboFundname.Text, dtPickerFrom.SelectedDate.ToString(),
                endDate.ToString(), gbl_conn);
            dgExecutions.ItemsSource = dtExecutions.DefaultView;
            tabiExecutions.Header = "Executions - " + dgExecutions.Items.Count.ToString();
        }
        private void GetTransactions()
        {
            DateTime endDate = (DateTime)dtPickerTo.SelectedDate;
            endDate = endDate.AddHours(23).AddMinutes(59).AddSeconds(59);
            dtTransactions = _db.get_account_transactions(
                cboFundname.Text, dtPickerFrom.SelectedDate.ToString(),
                endDate.ToString(), gbl_conn);
            dgTransactions.ItemsSource = dtTransactions.DefaultView;
            tabiTransactions.Header = "Transactions - " + dgTransactions.Items.Count.ToString();
        }

        private void GetOrders()
        {
            DateTime endDate = (DateTime)dtPickerTo.SelectedDate;
            endDate = endDate.AddHours(23).AddMinutes(59).AddSeconds(59);
            DateTime startDate = (DateTime)dtPickerFrom.SelectedDate;
            Order ord = new Order(cboFundname.Text, startDate , endDate, gbl_conn);
            ord.Get("fundname");
            dtOrders = ord.dtOrders;
            dgOrders.ItemsSource = dtOrders.DefaultView;
            dtOpenOrders = ord.dtOpenOrders;
            dgOpenOrders.ItemsSource = dtOpenOrders.DefaultView;
            if(dgOpenOrders.Items.Count > 0) 
            { btnApprove.Visibility = Visibility.Visible; }
            else { btnApprove.Visibility = Visibility.Hidden; }
            tabiOpenOrders.Header = "Open Orders - " + dgOpenOrders.Items.Count.ToString();
            tabiOrders.Header = "Filled Orders - " + dgOrders.Items.Count.ToString();
        }

        private void btnApprove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach(DataRowView rowView in dgOpenOrders.Items)
                {
                    DataRow row = rowView.Row;
                    bool approved = (bool)row["Approved"];
                    if(approved)
                    {
                        string tad_order_id = row["tad_order_id"].ToString();
                        string orders_key = row["orders_key"].ToString();
                        string status = "APPROVED";
                        DateTime endDate = (DateTime)dtPickerTo.SelectedDate;
                        endDate = endDate.AddHours(23).AddMinutes(59).AddSeconds(59);
                        DateTime startDate = (DateTime)dtPickerFrom.SelectedDate;
                        Order ord = new Order(cboFundname.Text, startDate, endDate, gbl_conn);
                        ord.UpdateOrderStatus(tad_order_id, status, orders_key);

                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Approve orders", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
