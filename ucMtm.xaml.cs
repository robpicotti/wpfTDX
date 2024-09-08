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
using System.Globalization;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        public decimal futures_posn_pnl = 0;
        public decimal futures_new_deals = 0;
        public decimal futures_total_pnl = 0;
        public decimal futures_commission = 0;

        public decimal equity_posn_pnl = 0;
        public decimal equity_new_deals = 0;
        public decimal equity_total_pnl = 0;
        public decimal equity_commission = 0;

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
            SetControlValues();

        }
        /// <summary>
        /// set some control values e.g. datetime picker dates
        /// </summary>
        private void SetControlValues()
        {
            dtFrom.SelectedDate = DateTime.Today;
            dtTo.SelectedDate = DateTime.Today;
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
            lstNumericColumns.Add("position_pnl");
            lstNumericColumns.Add("posn_new_deals");
            lstNumericColumns.Add("new_deals_pnl");
            lstNumericColumns.Add("newdeal_pnl");
            lstNumericColumns.Add("new_deals");
            lstNumericColumns.Add("new_deals_exchcurr");
            lstNumericColumns.Add("commission");
            lstNumericColumns.Add("consideration");
            lstNumericColumns.Add("new_deal_pnl_exclComm");
            lstNumericColumns.Add("contract_size");
            lstNumericColumns.Add("_effective_posn");
            lstNumericColumns.Add("_actual_posn");
        }

        private void dgEquities_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (lstNumericColumns.Contains(e.PropertyName.ToString().ToLower()))
            {
                var textColumn = e.Column as DataGridTextColumn;
                if (textColumn != null)
                {
                    // Set the StringFormat to "N0" for thousand separator and no decimal places
                    textColumn.Binding = new Binding(e.PropertyName) { StringFormat = "N0" };
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

            //DataSet ds = _db.get_futures_pnl(fundName, posn_date, posn_date_t1, gbl_conn);

            //if (ds.Tables.Count > 0)
            //{
            //    DataView dataView = ds.Tables[1].DefaultView;
            //    dgFutures.ItemsSource = dataView;
            //    dgFuturesNewDeals.ItemsSource = ds.Tables[0].DefaultView;
            //    dtFuturesPosnPnl = ds.Tables[1];
            //    dtFuturesNewDealsPnl = ds.Tables[0];
            //}
            //decimal futuresCommission = 0;
            //decimal futuresNewDeals = 0;
            //decimal futuresPosnPnl = 0;
            //object futuresNewDealsObject = DBNull.Value;
            //if ((dtFuturesNewDealsPnl != null) && dtFuturesNewDealsPnl.Rows.Count > 0)
            //{
            //    futuresNewDealsObject = Math.Round(Convert.ToDecimal(dtFuturesNewDealsPnl.Compute("SUM(new_deals)", String.Empty)), 0);
            //    if (futuresNewDealsObject != DBNull.Value)
            //    {
            //        futuresNewDeals = Math.Round(Convert.ToDecimal(futuresNewDealsObject), 0);
            //    }
            //}
            //futures_posn_pnl = Math.Round(Convert.ToDecimal(dtFuturesPosnPnl.Compute("SUM(_position_pnl)", String.Empty)), 0);

            //futures_total_pnl = Math.Round(Convert.ToDecimal(dtFuturesPosnPnl.Compute("SUM(total_pnl)", String.Empty)), 0);

            //futures_new_deals = futuresNewDeals;
            //txtPosnPnlFutures.Text = futures_posn_pnl.ToString("N0");
            //txtNewDealsPnlFutures.Text = futures_new_deals.ToString("N0");
            //txtTotalPnlFutures.Text = futures_total_pnl.ToString("N0");
            //txtCommissionFutures.Text = futures_commission.ToString("N0");
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
            if (ds.Tables.Count > 0)
            {
                DataView dataView = ds.Tables[1].DefaultView;
                dgEquities.ItemsSource = dataView;
                dgEquitiesNewDeals.ItemsSource = ds.Tables[0].DefaultView;
                dtEquityPosnPnl = ds.Tables[1];
                dtEquityNewDealsPnl = ds.Tables[0];
            }


            if (dtEquityPosnPnl.Rows.Count > 0)
            {
                decimal equityCommission = 0;
                equity_posn_pnl = Math.Round(Convert.ToDecimal(dtEquityPosnPnl.Compute("SUM(posn_pnl)", String.Empty)),0);
                equity_new_deals = Math.Round(Convert.ToDecimal(dtEquityPosnPnl.Compute("SUM(new_deals_pnl)", String.Empty)),0);
                equity_total_pnl = Math.Round(Convert.ToDecimal(dtEquityPosnPnl.Compute("SUM(total_pnl)", String.Empty)),0);
                object equityCommissionObject = dtEquityNewDealsPnl.Compute("SUM(commission)", String.Empty);
                if (equityCommissionObject != DBNull.Value)
                {
                    equityCommission = Math.Round(Convert.ToDecimal(equityCommissionObject), 0);
                }

                equity_commission = equityCommission;
                txtPosnPnl.Text = equity_posn_pnl.ToString("N0");
                txtNewDealsPnl.Text = equity_new_deals.ToString("N0");
                txtTotalPnl.Text = equity_total_pnl.ToString("N0");
                txtCommission.Text = equity_commission.ToString("N0");
            }
        }
        private void GetTotalPnl()
        {
            decimal total_posn_pnl = 0;
            decimal total_newdeals_pnl = 0;
            decimal total_pnl = 0;
            string fundname = cboFundname.Text;
            total_posn_pnl = futures_posn_pnl + equity_posn_pnl;
            total_newdeals_pnl = futures_new_deals + equity_new_deals;
            total_pnl = equity_total_pnl + futures_total_pnl;

            DataTable dtTotal = new DataTable();
            dtTotal.Columns.Add("fundname");
            dtTotal.Columns.Add("type");
            dtTotal.Columns.Add("posn_pnl");
            dtTotal.Columns.Add("new_deals_pnl");
            dtTotal.Columns.Add("total_pnl");

            DataRow dr1 = dtTotal.NewRow();
            dr1["fundname"] = fundname ;
            dr1["type"] = "equities";
            dr1["posn_pnl"] = equity_posn_pnl.ToString("N", CultureInfo.GetCultureInfo("en-US"));
            dr1["new_deals_pnl"] = equity_new_deals.ToString("N", CultureInfo.GetCultureInfo("en-US"));
            dr1["total_pnl"] = equity_total_pnl.ToString("N", CultureInfo.GetCultureInfo("en-US"));
            dtTotal.Rows.Add(dr1);

            DataRow dr2 = dtTotal.NewRow();
            dr2["fundname"] = fundname;
            dr2["type"] = "futures";
            dr2["posn_pnl"] = futures_posn_pnl.ToString("N", CultureInfo.GetCultureInfo("en-US"));
            dr2["new_deals_pnl"] = futures_new_deals.ToString("N", CultureInfo.GetCultureInfo("en-US"));
            dr2["total_pnl"] = futures_total_pnl.ToString("N", CultureInfo.GetCultureInfo("en-US"));
            dtTotal.Rows.Add(dr2);

            DataRow dr3 = dtTotal.NewRow();
            dr3["fundname"] = fundname;
            dr3["type"] = "";
            dr3["posn_pnl"] = String.Empty;
            dr3["new_deals_pnl"] = String.Empty;
            dr3["total_pnl"] = String.Empty;
            dtTotal.Rows.Add(dr3);

            DataRow dr4 = dtTotal.NewRow();
            dr4["fundname"] = fundname;
            dr4["type"] = "Total";
            dr4["posn_pnl"] = (futures_posn_pnl + equity_posn_pnl).ToString("N", CultureInfo.GetCultureInfo("en-US"));
            dr4["new_deals_pnl"] = (futures_new_deals + equity_new_deals).ToString("N", CultureInfo.GetCultureInfo("en-US"));
            dr4["total_pnl"] = (futures_total_pnl + equity_total_pnl).ToString("N", CultureInfo.GetCultureInfo("en-US"));
            dtTotal.Rows.Add(dr4);
            dgTotal.ItemsSource = dtTotal.DefaultView;

        }
        private async void cmdRun_Click(object sender, RoutedEventArgs e)
        {
 
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                string fundname = cboFundname.Text;
                DateTime date_t = dtTo.SelectedDate.Value;
                DateTime date_tminus1 = dtFrom.SelectedDate.Value;
                string base_currency = "EUR";
                await ProcessPnlData(fundname, date_t, date_tminus1, base_currency);
                MessageBox.Show("Pnl calculation complete", "Pnl", MessageBoxButton.OK, MessageBoxImage.Information);
                //GetEquitiesPnl();
                //GetFuturesPnl();
                //GetTotalPnl();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Run pnl error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { Mouse.OverrideCursor = null; }
            
        }

        private async void cmdTest_Click(object sender, RoutedEventArgs e)
        {
            //string table_name = "funds";
            //DataTable dtResults = await GetDataFromApiAsync(table_name);
            //if (dtResults != null)
            //{
            //    dgTest.ItemsSource = dtResults.DefaultView;
            //}

            string fundname = cboFundname.Text;
            DateTime date_t = dtTo.SelectedDate.Value;
            DateTime date_tminus1 = dtFrom.SelectedDate.Value;
            string base_currency = "EUR";
            ProcessPnlData(fundname, date_t, date_tminus1, base_currency);
        }


        public DataTable ConvertJsonArrayToDataTable(string jsonArray)
        {
            DataTable dataTable = new DataTable();
            JArray array = JArray.Parse(jsonArray);

            if (array.Count == 0)
                return dataTable;

            // Create columns based on the first row
            foreach (var column in array[0].ToObject<JObject>())
            {
                dataTable.Columns.Add(column.Key);
            }

            // Add rows
            foreach (JObject row in array)
            {
                DataRow dataRow = dataTable.NewRow();
                foreach (var column in row)
                {
                    dataRow[column.Key] = column.Value.ToString(); // Use column.Key and column.Value
                }
                dataTable.Rows.Add(dataRow);
            }

            return dataTable;
        }

        public DataTable ReorderColumns(DataTable originalTable, string[] desiredColumnOrder)
        {
            // Create a new DataTable to store the reordered columns
            DataTable reorderedTable = new DataTable();

            // Add the columns in the desired order
            foreach (string columnName in desiredColumnOrder)
            {
                if (originalTable.Columns.Contains(columnName))
                {
                    reorderedTable.Columns.Add(columnName, originalTable.Columns[columnName].DataType);
                }
            }

            // Now, add the rows from the original DataTable into the new DataTable
            foreach (DataRow row in originalTable.Rows)
            {
                DataRow newRow = reorderedTable.NewRow();
                foreach (string columnName in desiredColumnOrder)
                {
                    if (originalTable.Columns.Contains(columnName))
                    {
                        newRow[columnName] = row[columnName];
                    }
                }
                reorderedTable.Rows.Add(newRow);
            }

            return reorderedTable;
        }



        public async Task ProcessPnlData(string _subaccount, DateTime _date_t, DateTime _date_tminus1, string _base_currency)
        {
            string jsonResponse = await GetPnlDataAsync(_subaccount,_date_t,_date_tminus1,_base_currency);

            try
            {
                JObject pnlData = JObject.Parse(jsonResponse);
                // Convert each dataframe part of the response into a DataTable
                DataTable dfbuyandhold = ConvertJsonArrayToDataTable(pnlData["dfbuyandhold"].ToString());
                string[] buyAndHoldcolumnOrder = new string[] { 
                    "tad_id", "tickername", "instrument", "exch_currency" ,"multiplier",
                    "executed_quantity","closing_price_t","closing_price_tminus1",
                    "cash_t","cash_tminus1","mtm_t","mtm_tminus1",
                    "adjusted_mtm_tminus1","adjusted_cash_tminus1","commission_" + _base_currency,"pnl_" + _base_currency
                };  // Replace with your desired column names
                DataTable reorderedDfbuyandhold = ReorderColumns(dfbuyandhold, buyAndHoldcolumnOrder);
                DataTable dfnewdeals = ConvertJsonArrayToDataTable(pnlData["dfnewdeals"].ToString());
                dgFutures.ItemsSource = reorderedDfbuyandhold.DefaultView;
                dgFuturesNewDeals.ItemsSource = dfnewdeals.DefaultView;
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }


        public async Task<string> GetPnlDataAsync(string _fundname, DateTime _date_t, DateTime _date_tminus1, string _base_currency)
        {
            using (HttpClient client = new HttpClient())
            {
                var requestData = new
                {
                    fundname = _fundname,
                    date_t = _date_t.ToString("yyyy-MM-dd"),
                    date_tminus1 = _date_tminus1.ToString("yyyy-MM-dd"),
                    base_currency = _base_currency
                };
                string jsonRequest = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync("http://localhost:5000/pnl", content);
                response.EnsureSuccessStatusCode();

                string jsonResponse = await response.Content.ReadAsStringAsync();
                return jsonResponse;
            }
        }
        
        
        public async Task<DataTable> GetDataFromApiAsync(string tableName)
        {
            using (var client = new HttpClient())
            {
                // Set the URL to your API endpoint
                string url = "http://localhost:5000/select_table";

                // Create the request body as a JSON object
                var requestData = new
                {
                    table_name = tableName
                };

                // Serialize the request data to JSON
                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Send the POST request
                var response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    // Read the response content as a string
                    var result = await response.Content.ReadAsStringAsync();

                    // Deserialize the result into a list of dictionaries
                    var rows = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(result);

                    // Convert the list of dictionaries to a DataTable
                    DataTable dataTable = new DataTable();

                    if (rows.Count > 0)
                    {
                        // Create columns based on the dictionary keys
                        foreach (var key in rows[0].Keys)
                        {
                            dataTable.Columns.Add(key);
                        }

                        // Add rows to the DataTable
                        foreach (var row in rows)
                        {
                            DataRow dataRow = dataTable.NewRow();
                            foreach (var key in row.Keys)
                            {
                                dataRow[key] = row[key];
                            }
                            dataTable.Rows.Add(dataRow);
                        }
                    }

                    return dataTable;
                }
                else
                {
                    // Handle errors
                    Console.WriteLine("Error: " + response.StatusCode);
                    return null;
                }
            }
        }
    }
}
