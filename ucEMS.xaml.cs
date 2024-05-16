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
        DataTable dtOpenOrdersProposed;
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
                RefreshOrdersData();
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
        
        private void RefreshOrdersData()
        {
            GetOrders();
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
            //get filled orders
            dtOrders = ord.dtOrders;
            dgOrders.ItemsSource = dtOrders.DefaultView;
            //get open orders all
            dtOpenOrders = ord.dtOpenOrders;
            //add the selected column
            DataColumn selectedColumn = new DataColumn("Selected", typeof(bool));
            selectedColumn.DefaultValue = false;
            dtOpenOrders.Columns.Add(selectedColumn);
            selectedColumn.SetOrdinal(0);
            dgOpenOrders.ItemsSource = dtOpenOrders.DefaultView;
            if(dgOpenOrders.Items.Count >0)
            {
                btnSelectAll.Visibility = Visibility.Visible;
                btnFreeze.Visibility = Visibility.Visible;
                btnCancel.Visibility = Visibility.Visible;
            }
            else
            {
                btnSelectAll.Visibility = Visibility.Hidden;
                btnFreeze.Visibility = Visibility.Hidden;
                btnCancel.Visibility = Visibility.Hidden;
            }
            // get only proposed open orders
            dtOpenOrdersProposed = ord.dtOpenOrdersProposed;
            DataColumn approvedColumn = new DataColumn("Approved", typeof(bool));
            approvedColumn.DefaultValue = false;
            // Add the "Approved" column as the first column
            dtOpenOrdersProposed.Columns.Add(approvedColumn);
            approvedColumn.SetOrdinal(0);
            dgOpenOrdersProposed.ItemsSource = dtOpenOrdersProposed.DefaultView;
            if (dgOpenOrdersProposed.Items.Count > 0)
            {
                btnApprove.Visibility = Visibility.Visible;
                btnApproveAll.Visibility = Visibility.Visible;
            }
            else { btnApprove.Visibility = Visibility.Hidden; btnApproveAll.Visibility = Visibility.Hidden; }
            tabiOpenOrdersProposed.Header = "Proposed Open Orders - " + dgOpenOrdersProposed.Items.Count.ToString();
            tabiOrders.Header = "Filled Orders - " + dgOrders.Items.Count.ToString();
            tabiOpenOrders.Header = "Open Orders - " + dgOpenOrders.Items.Count.ToString();
        }

        private void btnApprove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime endDate = (DateTime)dtPickerTo.SelectedDate;
                endDate = endDate.AddHours(23).AddMinutes(59).AddSeconds(59);
                DateTime startDate = (DateTime)dtPickerFrom.SelectedDate;
                Order ord = new Order(cboFundname.Text, startDate, endDate, gbl_conn);

                bool atLeastOneChecked = dgOpenOrdersProposed.Items.Cast<DataRowView>().Any(item => (bool)item.Row.ItemArray[0]);
                if (atLeastOneChecked)
                {
                    string question = "Are you sure you want to approve the selected trades?";
                    MessageBoxResult result = MessageBox.Show(question, "approve trades",
                        MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        foreach (DataRowView rowView in dgOpenOrdersProposed.Items)
                        {
                            DataRow row = rowView.Row;
                            bool approved = (bool)row["Approved"];
                            if (approved)
                            {
                                string tad_order_id = row["tad_order_id"].ToString();
                                string orders_key = row["orders_key"].ToString();
                                string status = "APPROVED";

                                ord.UpdateOrderStatus(tad_order_id, status, orders_key);

                            }
                        }
                        MessageBox.Show("Trades approved", "EMS", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("No trades approved because none were checked", "EMS", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Approve orders", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                RefreshOrdersData();
            }
        }

        private void btnApproveAll_Click(object sender, RoutedEventArgs e)
        {
            bool setMode = false ;
            try
            {
                if (dgOpenOrdersProposed.Items.Count > 0)
                {
                    switch (btnApproveAll.Content)
                    {
                        case "Select All":
                            setMode = true;
                            break;
                        case "Unselect All":
                            setMode = false;
                            break;
                    }
                    foreach (DataRowView rowView in dgOpenOrdersProposed.Items)
                    {
                        DataRow row = rowView.Row;

                        row["approved"] = setMode;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Approve all error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (dgOpenOrdersProposed.Items.Count > 0)
                {
                    switch (setMode)
                    {
                        case true:
                            btnApproveAll.Content = "Unselect All";
                            btnApproveAll.Background = Brushes.Red;
                            break;
                        case false:
                            btnApproveAll.Content = "Select All";
                            btnApproveAll.Background = Brushes.LightGreen;
                            break;
                    }
                }
            }
        }

        private void btnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            bool setMode = false;
            try
            {
                if (dgOpenOrders.Items.Count > 0)
                {
                    switch (btnSelectAll.Content)
                    {
                        case "Select All":
                            setMode = true;
                            break;
                        case "Unselect All":
                            setMode = false;
                            break;
                    }
                    foreach (DataRowView rowView in dgOpenOrders.Items)
                    {
                        DataRow row = rowView.Row;

                        row["Selected"] = setMode;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Select all error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (dgOpenOrders.Items.Count > 0)
                {
                    switch (setMode)
                    {
                        case true:
                            btnSelectAll.Content = "Unselect All";
                            btnSelectAll.Background = Brushes.Red;
                            break;
                        case false:
                            btnSelectAll.Content = "Select All";
                            btnSelectAll.Background = Brushes.LightGreen;
                            break;
                    }
                }
            }
        }

        private void btnFreeze_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TickerFreezer tfreezer = new TickerFreezer(this.gbl_conn);
                string question = "Are you sure you want to freeze the selected tad_id-execaccountname combinations?";
                MessageBoxResult result = MessageBox.Show(question, "freeze tad-id-execaccoutname",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {

                    foreach (DataRowView rowView in dgOpenOrders.Items)
                    {
                        DataRow row = rowView.Row;
                        string tad_id = row["tad_id"].ToString();
                        string execaccountname = row["account"].ToString();
                        bool blnSelected = false;
                        blnSelected = (bool)row["selected"];
                        if(blnSelected)
                        {
                            tfreezer.Freeze(runtime: DateTime.UtcNow.ToString("dd-MMM-yyyy HH:mm:ss"), error_code: 2006, 
                                error_string: "Manual freeze on tad_id and execaccountname",freeze_expiration:null,_override:0,
                                override_expiration: null,runtime_resolved:null,
                                notes: "manual freeze from TDX", execaccountname:execaccountname,tad_id: tad_id);
                        }
                    }
                    string message = "selected tad_id-execaccoutname combinations have been frozen";
                    MessageBox.Show(message, "EMS freeze", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "EMS freeze", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                RefreshOrdersData();
            }
        }

        //for now can only cancel TAD broker trades
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //List<DataRow> listOfDataRows = new List<DataRow>();
                DateTime startDate = (DateTime)dtPickerFrom.SelectedDate;
                startDate = startDate.AddHours(23).AddMinutes(59).AddSeconds(59);
                DateTime endDate = (DateTime)dtPickerTo.SelectedDate;
                endDate = endDate.AddHours(23).AddMinutes(59).AddSeconds(59);
                Order ord = new Order(cboFundname.SelectedValue.ToString(), startDate, endDate, this.gbl_conn);
                bool blnNonTadBroker = false;
                foreach(DataRowView rowView in dgOpenOrders.Items)
                {
                    DataRow row = rowView.Row;
                    bool blnSelected = (bool)row["selected"];
                    string broker = row["broker"].ToString();
                    string status = row["status"].ToString();
                    if(blnSelected)
                    {
                        // can only cancel tad broker trades and 
                        if (broker == "TAD"  )
                        {
                            // trades that havent already been flagged as CANCEL TDX
                            if (status != "CANCEL TDX")
                            {
                                string tad_order_id = row["tad_order_id"].ToString();
                                string orders_key = row["orders_key"].ToString();
                                ord.UpdateOrderStatus(tad_order_id, "CANCEL TDX", orders_key);
                            }
                        }
                        else
                        {
                            if(!blnNonTadBroker)
                            {
                                blnNonTadBroker = true;
                            }
                        }
                    }
                }
                if (blnNonTadBroker)
                {
                    string msg = "Non TAD execution brokers cannot be cancelled at this point. You selected some orders to cancel that were not executed by TAD";
                    MessageBox.Show(msg, "cancel order auto", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "cancel selected orders", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                RefreshOrdersData();
            }
        }
    
    }
}
