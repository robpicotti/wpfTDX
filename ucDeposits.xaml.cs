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
using System.Data.SqlClient;
using System.Data;
using TDX;

namespace wpfTDX
{
    /// <summary>
    /// Interaction logic for ucDeposits.xaml
    /// </summary>
    public partial class ucDeposits : UserControl
    {
        public event EventHandler RemoveControlRequested;
        TDX.db _db = new TDX.db();
        SqlConnection gbl_conn;
        DataTable dtFunds;
        DataTable dtTxTypedetails;
        DataTable dtCurrencies;
        DataTable dtTadidsMaster;
        private Account _ACC;
        private Subaccount _SUBACC;
        string TX_TYPE = "cash_depo_pay";
        public ucDeposits(SqlConnection conn)
        {
            InitializeComponent();
            gbl_conn = conn;
            LoadForm();

        }
        /// <summary>
        /// custom form initializer
        /// </summary>
        private void LoadForm()
        {
            PopulateControls();
        }
        /// <summary>
        /// populates all form user controls with data
        /// </summary>
        private void PopulateControls()
        {
            //populate funds
            dtFunds = _db.get_funds(gbl_conn);
            if (dtFunds.Rows.Count > 0)
            {
                cboFundname.Items.Clear();
                foreach (DataRow row in dtFunds.Rows)
                {
                    cboFundname.Items.Add(row["fundname"].ToString());
                }
            }
            //populate tx types
            dtTxTypedetails = _db.get_txtype_details("BUY", gbl_conn);
            cboTxtypeDetail.Items.Clear();
            foreach (DataRow row in dtTxTypedetails.Rows)
            {
                cboTxtypeDetail.Items.Add(row["detail"].ToString());
            }
            //populate currencies
            dtCurrencies = _db.get_cash_currencies(gbl_conn);
            cboCurrency.Items.Clear();
            foreach (DataRow row in dtCurrencies.Rows)
            {
                cboCurrency.Items.Add(row["tickername"].ToString());
            }
            //populdate tad_ids_master
            dtTadidsMaster = _db.get_tad_ids_master(gbl_conn);
            //set current date on datetime picker
            dtpTxDate.SelectedDate = (DateTime)DateTime.Today;
        }
        private void cboFundname_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                cboSubaccount.Items.Clear();
                cboExecBrokerCode.Items.Clear();
                cboBrokerCode.Items.Clear();
                Account acc = new Account(cboFundname.SelectedValue.ToString(), gbl_conn);
                _ACC = acc;
                for (int i = 0; i < acc.subaccounts_list.Count; i++)
                {
                    cboSubaccount.Items.Add(acc.subaccounts_list[i].subaccount_name.ToString());
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Funds", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cboSubaccount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                string subaccount_name = cboSubaccount.SelectedValue.ToString();
                _SUBACC = new Subaccount(_ACC, subaccount_name, gbl_conn);
                cboBrokerCode.Items.Clear();
                cboExecBrokerCode.Items.Clear();
                if (subaccount_name != "")
                {
                    if (this._ACC != null)
                    {
                        for (int i = 0; i < this._ACC.subaccounts_list.Count; i++)
                        {
                            string subaccont = this._ACC.subaccounts_list[i].subaccount_name;
                            if (subaccont == subaccount_name)
                            {
                                string broker_code = this._ACC.subaccounts_list[i].broker_code;
                                cboBrokerCode.Items.Add(broker_code);
                                cboBrokerCode.Text = broker_code;
                                cboExecBrokerCode.Items.Add(_SUBACC.broker_code_exec);
                                cboExecBrokerCode.Text = _SUBACC.broker_code_exec;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "subaccounts", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cboCurrency_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string tickername = cboCurrency.SelectedItem.ToString();
            DataTable dtSeltadid = dtTadidsMaster.Copy();
            dtSeltadid = dtSeltadid.Select("tickername='" + tickername + "'").CopyToDataTable();
            txtTadId.Text = dtSeltadid.Rows[0]["tad_id"].ToString();
        }

        private void cmdAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                if ((cboBrokerCode.SelectedItem != null) &&
                    (cboExecBrokerCode.SelectedItem != null) &&
                    (cboFundname.SelectedItem != null) &&
                    (cboCurrency.Text != "") &&
                    (txtAmount.Text != "") &&
                    (cboTxtypeDetail.Text != ""))
                {
                    string broker_code = cboBrokerCode.SelectedItem.ToString();
                    Broker Brok = new Broker(broker_code, gbl_conn);
                    int broker_id = Brok.broker_id;
                    //get executiing broker
                    string broker_code_exec = cboExecBrokerCode.SelectedItem.ToString();
                    Broker Broker_exec = new Broker(broker_code_exec, gbl_conn);
                    int broker_id_exec = Broker_exec.broker_id;
                    string fund = cboFundname.SelectedItem.ToString();
                    string subaccount = cboSubaccount.SelectedItem.ToString();
                    string broker = Brok.brokername;
                    string currency = cboCurrency.Text;
                    string amount = txtAmount.Text;
                    string txtype_detail = cboTxtypeDetail.Text;
                    bool isnumeric = double.TryParse(amount, out _);
                    string tad_id = txtTadId.Text;
                    DateTime dtmTx_date = (DateTime)dtpTxDate.SelectedDate;
                    string tx_date = dtmTx_date.ToString("yyyy-MM-dd HH:mm:ss");
                    if ((subaccount != "") && (broker != "") && (currency != "") && (txtype_detail != "") && (isnumeric))
                    {
                        addDepoCash(currency, Brok, fund, subaccount, tad_id, amount, "BUY", tx_date, Broker_exec, txtype_detail);
                        MessageBox.Show("Deposit added", "Add deposit", MessageBoxButton.OK, MessageBoxImage.Information);
                        ClearControls();
                    }
                }
                else
                {
                    MessageBox.Show(
                        "Please select all fields",
                        "Deposits",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Add deposit", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }
        /// <summary>
        /// clear all the controls once you've added a payment
        /// </summary>
        private void ClearControls()
        {
            UnsubscribeSelectionChangedEvents();
            cboTxtypeDetail.SelectedIndex = -1;
            cboFundname.SelectedIndex = -1;
            cboSubaccount.SelectedIndex = -1;
            cboBrokerCode.SelectedIndex = -1;
            cboExecBrokerCode.SelectedIndex = -1;
            cboCurrency.SelectedIndex = -1; 
            txtTadId.Text = "";
            txtAmount.Text = "";
            SubscribeSelectionChangedEvents();
        }
        /// <summary>
        /// unsubscribes controls from their change events
        /// </summary>
        private void UnsubscribeSelectionChangedEvents()
        {
            cboFundname.SelectionChanged -= cboFundname_SelectionChanged;
            cboSubaccount.SelectionChanged -= cboSubaccount_SelectionChanged;
            cboCurrency.SelectionChanged -= cboCurrency_SelectionChanged;
        }
        /// <summary>
        /// subscribe to the change events
        /// </summary>
        private void SubscribeSelectionChangedEvents()
        {
            cboFundname.SelectionChanged += cboFundname_SelectionChanged;
            cboSubaccount.SelectionChanged += cboSubaccount_SelectionChanged;
            cboCurrency.SelectionChanged += cboCurrency_SelectionChanged; 
        }
        private void cmdClose_Click(object sender, RoutedEventArgs e)
        {
            RemoveControlRequested?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// method which adds rows to transaction table for deposit
        /// </summary>
        /// <param name="currency"></param>
        /// <param name="broker"></param>
        /// <param name="fund"></param>
        /// <param name="subaccount"></param>
        /// <param name="tad_id"></param>
        /// <param name="amount"></param>
        /// <param name="_action"></param>
        /// <param name="tx_date"></param>
        /// <param name="broker_exec"></param>
        /// <param name="txtype_detail"></param>
        private void addDepoCash(string currency, Broker broker, string fund, string subaccount, string tad_id,
            string amount, string _action, string tx_date, Broker broker_exec, string txtype_detail)
        {
            string transaction_id = _db.transaction_id(currency, broker.brokername, subaccount);
            string strategy_id = "0";
            string action = _action;
            string price = "0.0";
            string executed_qty = amount;
            string runtime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string tickername = currency;
            string sql_text = "INSERT transactions (runtime,transaction_id,broker_id,fund,subaccount,strategy_id,action,tad_id,tickername,price,executed_qty,execution_time,settlement_date," +
                "effective_date,tx_type,broker_id_exec,tx_type_detail,sub_tickername,exch_currency,instrument) VALUES('" + runtime + "','" + transaction_id + "'," +
                broker.broker_id.ToString() + ",'" + fund + "','" + subaccount + "'," + strategy_id + ",'" + action + "','" + tad_id + "','" +
                tickername + "'," + price + "," + executed_qty + ",'" + tx_date + "','" + tx_date + "','" + tx_date + "','" + TX_TYPE + "'," + broker_exec.broker_id.ToString() + ",'" +
                txtype_detail +  "','" + tickername +  "','" + currency.Substring(0,3) + "','cash"   + "')";
            _db.execSQL_noresults(sql_text, gbl_conn);
        }
    }
}
