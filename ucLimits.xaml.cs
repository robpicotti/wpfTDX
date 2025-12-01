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
using System.Windows.Markup;
using System.Globalization;

namespace wpfTDX
{
    /// <summary>
    /// Interaction logic for ucLimits.xaml
    /// </summary>
    public partial class ucLimits : UserControl
    {
        public event EventHandler RemoveControlRequested;
        public SqlConnection gbl_conn;
        DataTable dtFunds;
        DataTable dtMergedData;
        Limits limits;
        List<string> FundsList = new List<string>(); //this will contain the list of funds you want to query
        TickerLimits tickerLimits;
        bool blnValueChanged = false; 
        bool blnTickerLimitChanged = false;
        string ALL_FUNDS = "<* ALL FUNDS *>";
        TDX.db _db = new TDX.db();
        public List<string> ListOfFundNames =  new List<string>(); //contains full fund names list

        public ucLimits(SqlConnection conn)
        {
            InitializeComponent();
            this.gbl_conn = conn;
            LoadForm();
        }
        
        private void LoadForm()
        {
            dtFunds = _db.get_funds(gbl_conn);

            // Filter out the "ALL_FUNDS" row
            var filteredRows = dtFunds.AsEnumerable()
                .Where(row => row.Field<string>("fundname") != ALL_FUNDS);

            // Project fund names into a list
            this.ListOfFundNames = filteredRows.Select(row => row.Field<string>("fundname")).ToList();

            // add "all funds" to the dtFunds table
            DataRow newrow = dtFunds.NewRow();
            newrow["fundname"] = ALL_FUNDS ;
            dtFunds.Rows.Add(newrow);
            //order the datatable
            var orderedRows = dtFunds.AsEnumerable()
                             .OrderBy(row => row.Field<string>("fundname"));
            dtFunds = orderedRows.CopyToDataTable();

            cboFundName.Items.Clear();
            foreach (DataRow row in dtFunds.Rows)
            {
                cboFundName.Items.Add(row["fundname"].ToString());
            }
        }
        
        private void cboFundName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

                Cursor = Cursors.Wait;
                this.FundsList.Clear();
                try
                {
                    RefreshFundLimits();
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "Refresh fund limits", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    Cursor = Cursors.Arrow;
                }
                try
                {
                    RefreshTickerLimits();
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "Refresh ticker limits", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    Cursor = Cursors.Arrow;
                }


        }
        
        private void RefreshFundLimits()
        {
            this.blnValueChanged = false;
            this.FundsList.Clear();
            //btnUpdateCustom.Visibility = Visibility.Hidden;
            string FundName = cboFundName.SelectedValue.ToString();
            if(FundName != ALL_FUNDS)
            {
                this.FundsList.Add(FundName);
            }
            else
            {
                this.FundsList = new List<string>(this.ListOfFundNames);
            }

            DataTable dtAll = new DataTable();
            dtAll.Columns.Add("fundname");
            dtAll.Columns.Add("metric");
            dtAll.Columns.Add("default");
            dtAll.Columns.Add("custom",typeof(double));
            dtAll.Columns.Add("action");
            dtAll.Columns.Add("live");
            dtAll.Columns.Add("Flagged",typeof(bool));
            dtAll.Columns.Add("Color");

            for (int x = 0; x < this.FundsList.Count; x++)
            {
                //set up the fund limits
                string _fundName = this.FundsList[x].ToString();
                this.limits = new Limits(_fundName, this.gbl_conn);
                for (int i = 0; i < limits.lstFundLimits.Count; i++)
                {
                    DataRow row = dtAll.NewRow();
                    string limit = limits.lstFundLimits[i];
                    row["metric"] = limit;
                    row["fundname"] = _fundName;
                    switch (limit)
                    {
                        case "weight_limit":
                            row["default"] = limits.fundLimits.weight_limit.defaultValue;
                            if (limits.fundLimits.weight_limit.customValue != null)
                                row["custom"] = limits.fundLimits.weight_limit.customValue;
                            else
                                row["custom"] = DBNull.Value;
                            row["action"] = limits.fundLimits.weight_limit.action;
                            break;
                        case "stk_notional_pct_limit":
                            row["default"] = limits.fundLimits.stk_notional_pct_limit.defaultValue;
                            if (limits.fundLimits.stk_notional_pct_limit.customValue != null)
                                row["custom"] = limits.fundLimits.stk_notional_pct_limit.customValue;
                            else
                                row["custom"] = DBNull.Value;
                            row["action"] = limits.fundLimits.stk_notional_pct_limit.action;
                            break;
                        case "fut_notional_pct_limit":
                            row["default"] = limits.fundLimits.fut_notional_pct_limit.defaultValue;
                            if(limits.fundLimits.fut_notional_pct_limit.customValue!=null)
                                row["custom"] = limits.fundLimits.fut_notional_pct_limit.customValue;
                            else
                                row["custom"] = DBNull.Value;
                            row["action"] = limits.fundLimits.fut_notional_pct_limit.action;
                            break;
                        case "liquidity_limit":
                            row["default"] = limits.fundLimits.liquidity_limit.defaultValue;
                            if(limits.fundLimits.liquidity_limit.customValue!=null)
                                row["custom"] = limits.fundLimits.liquidity_limit.customValue;
                            else
                                row["custom"] = DBNull.Value;
                            row["action"] = limits.fundLimits.liquidity_limit.action;
                            break;
                        case "stk_leverage_limit":
                            row["default"] = limits.fundLimits.stk_leverage_limit.defaultValue;
                            if(limits.fundLimits.stk_leverage_limit.customValue!=null)
                                row["custom"] = limits.fundLimits.stk_leverage_limit.customValue;
                            else
                                row["custom"] = DBNull.Value;
                            row["action"] = limits.fundLimits.stk_leverage_limit.action;
                            row["live"] = limits.fundLimits.stk_leverage_limit.liveValue;
                            break;
                        case "fut_leverage_limit":
                            row["default"] = limits.fundLimits.fut_leverage_limit.defaultValue;
                            if(limits.fundLimits.fut_leverage_limit.customValue!=null)
                                row["custom"] = limits.fundLimits.fut_leverage_limit.customValue;
                            else
                                row["custom"] = DBNull.Value;
                            row["action"] = limits.fundLimits.fut_leverage_limit.action;
                            row["live"] = limits.fundLimits.fut_leverage_limit.liveValue;
                            break;
                        case "leverage_limit":
                            row["default"] = limits.fundLimits.leverage_limit.defaultValue;
                            if(limits.fundLimits.leverage_limit.customValue!=null)
                                row["custom"] = limits.fundLimits.leverage_limit.customValue;
                            else
                                row["custom"] = DBNull.Value;
                            row["action"] = limits.fundLimits.leverage_limit.action;
                            row["live"] = limits.fundLimits.leverage_limit.liveValue;
                            break;
                        case "var_limit_factor":
                            row["default"] = limits.fundLimits.var_limit_factor.defaultValue;
                            if(limits.fundLimits.var_limit_factor.customValue!=null)
                                row["custom"] = limits.fundLimits.var_limit_factor.customValue;
                            else
                                row["custom"] = DBNull.Value;
                            row["action"] = limits.fundLimits.var_limit_factor.action;
                            row["live"] = limits.fundLimits.var_limit_factor.liveValue;
                            break;
                        case "stress_limit_factor":
                            row["default"] = limits.fundLimits.stress_limit_factor.defaultValue;
                            if(limits.fundLimits.stress_limit_factor.customValue !=null)
                                row["custom"] = limits.fundLimits.stress_limit_factor.customValue;
                            else
                                row["custom"] = DBNull.Value;
                            row["action"] = limits.fundLimits.stress_limit_factor.action;
                            row["live"] = limits.fundLimits.stress_limit_factor.liveValue;
                            break;
                        case "drawdown_limit":
                            row["default"] = limits.fundLimits.drawdown_limit.defaultValue;
                            if(limits.fundLimits.drawdown_limit.customValue !=null)
                                row["custom"] = limits.fundLimits.drawdown_limit.customValue;
                            else
                                row["custom"] = DBNull.Value;
                            row["action"] = limits.fundLimits.drawdown_limit.action;
                            row["live"] = limits.fundLimits.drawdown_limit.liveValue;
                            break;
                    }
                    double? live = null;
                    double? custom = null;
                    double? _default = null;
                    if (double.TryParse(row["live"].ToString(), out double result_live))
                    {
                        live = result_live;
                    }
                    if (double.TryParse(row["custom"].ToString(), out double result_custom))
                    {
                        custom = result_custom;
                    }
                    if (double.TryParse(row["default"].ToString(), out double result_default))
                    {
                        _default = result_default;
                    }
                    bool flagged = TickerLimits.GetFlaggedStatus(live, custom, _default);
                    row["Flagged"] = flagged;
                    row["Color"] = SetColor(flagged, null,custom);
                    dtAll.Rows.Add(row);
                }
            }
            if (dtAll.Rows.Count > 0)
            {
                var sortedRows_weight = dtAll.AsEnumerable()
                .OrderByDescending(r => r.Field<bool>("Flagged"))
                .ThenBy(r => r.Field<string>("fundname"))
                .ThenBy(r => r.Field<string>("metric"))
                .CopyToDataTable();
                dtAll = sortedRows_weight.Copy();
            }
            dtAll.AcceptChanges(); //addded
            dgFundLimits.ItemsSource = dtAll.DefaultView;

        }
       
        private void RefreshTickerLimits()
        {
            this.blnTickerLimitChanged = false;
            //btnUpdateTickerLimits.Visibility = Visibility.Hidden;
            DataTable dtNotional = new DataTable();
            dtNotional.Columns.Add("fundname");
            dtNotional.Columns.Add("benchmarkname");
            dtNotional.Columns.Add("tickername");
            dtNotional.Columns.Add("Default");
            dtNotional.Columns.Add("Custom",typeof(double));
            dtNotional.Columns.Add("Live");
            dtNotional.Columns.Add("Action");
            dtNotional.Columns.Add("Frozen");
            dtNotional.Columns.Add("Flagged", typeof(bool));
            dtNotional.Columns.Add("Color");
            DataTable dtLiquidity = new DataTable();
            DataTable dtWeights = new DataTable();

            for (int i = 0; i < this.FundsList.Count; i++)
            {
                //set up the ticker limits based on the fund
                string _fundName = this.FundsList[i].ToString();
                try
                {
                    this.limits = new Limits(_fundName, this.gbl_conn);
                }
                catch(Exception ex)
                {
                   MessageBox.Show("Error setting up fund limits for fund: " + _fundName + "\r\n" + ex.Message);
                }
                try
                {
                    this.tickerLimits = new TickerLimits(_fundName, this.limits.fundLimits, this.gbl_conn);
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Error setting up ticker limits for fund: " + _fundName + "\r\n" + ex.Message);
                }
                try
                {
                    dtNotional.Merge(populateNotionalTickerLimits());
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Error populating notional ticker limits for fund: " + _fundName + "\r\n" + ex.Message);
                }
                try
                {
                    dtLiquidity.Merge(populateTickerLiquidityLimits());
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Error populating liquidity ticker limits for fund: " + _fundName + "\r\n" + ex.Message);
                }
                try
                {
                    dtWeights.Merge(populateTickerWeightLimits());
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Error populating weight ticker limits for fund: " + _fundName + "\r\n" + ex.Message);
                }
                // Hide the "color" & flagged column
                var columnsToHide = new List<string> { "Color", "Flagged" };
                // Hide specified columns
                foreach (var columnName in columnsToHide)
                {
                    var column = dgLiquidityLimits.Columns.FirstOrDefault(c => c.Header.ToString() == columnName);
                    if (column != null)
                    {
                        column.Visibility = Visibility.Collapsed;
                    }
                    column = dgWeightLimits.Columns.FirstOrDefault(c => c.Header.ToString() == columnName);
                    if (column != null)
                    {
                        column.Visibility = Visibility.Collapsed;
                    }
                    column = dgTickerLimits.Columns.FirstOrDefault(c => c.Header.ToString() == columnName);
                    if (column != null)
                    {
                        column.Visibility = Visibility.Collapsed;
                    }
                }
            }
            if (dtNotional.Rows.Count > 0)
            {
                var sortedRows = dtNotional.AsEnumerable()
                   .OrderByDescending(r => r.Field<bool>("Flagged"))
                   .ThenByDescending(r => r.IsNull("Custom") ? double.MinValue : r.Field<double>("Custom"))
                   .ThenBy(r => r.Field<string>("fundname"))
                   .ThenBy(r => r.Field<string>("tickername"))
                   .CopyToDataTable();
                dtNotional = sortedRows.Copy();
            }
            if(dtLiquidity.Rows.Count >0)
            {
                var sortedRows_Liq = dtLiquidity.AsEnumerable()
                    .OrderByDescending(r => r.Field<bool>("Flagged"))
                    .ThenByDescending(r => r.IsNull("Custom") ? double.MinValue : r.Field<double>("Custom"))
                    .ThenBy(r => r.Field<string>("fundname"))
                    .ThenBy(r => r.Field<string>("tickername"))
                    .CopyToDataTable();
                dtLiquidity = sortedRows_Liq.Copy();
            }
            if(dtWeights.Rows.Count >0)
            {
                var sortedRows_weight = dtWeights.AsEnumerable()
                .OrderByDescending(r => r.Field<bool>("Flagged"))
                .ThenByDescending(r => r.IsNull("Custom") ? double.MinValue : r.Field<double>("Custom"))
                .ThenBy(r => r.Field<string>("fundname"))
                .ThenBy(r => r.Field<string>("tickername"))
                .CopyToDataTable();
                dtWeights = sortedRows_weight.Copy();
            }
            Util.DataTableToCSV(dtNotional, "dtLiquidity.csv");
            dtNotional.AcceptChanges();   // <<< add
            dtLiquidity.AcceptChanges();  // <<< add
            dtWeights.AcceptChanges();    // <<< add
            dgTickerLimits.ItemsSource = dtNotional.DefaultView;
            dgLiquidityLimits.ItemsSource = dtLiquidity.DefaultView;
            dgWeightLimits.ItemsSource = dtWeights.DefaultView;
        }
        
        private DataTable populateTickerWeightLimits()
        {
            try
            {
                DataTable dtWeight = new DataTable();
                dtWeight.Columns.Add("fundname");
                dtWeight.Columns.Add("benchmarkname");
                dtWeight.Columns.Add("tickername");
                dtWeight.Columns.Add("Default");
                dtWeight.Columns.Add("Custom",typeof(double));
                dtWeight.Columns.Add("Live");
                dtWeight.Columns.Add("Action");
                dtWeight.Columns.Add("Frozen");
                dtWeight.Columns.Add("Flagged", typeof(bool));
                dtWeight.Columns.Add("Color");
                foreach (var kvp in this.tickerLimits.dictOfTickers)
                {
                    string tickername = kvp.Key.ToString();
                    string instrument = kvp.Value.ToString();
                    DataRow row = dtWeight.NewRow();
                    // Check if the tickername exists in the list of TickerNotionalLimits
                    TickerWeightLimits tickerLimits = this.tickerLimits.TickerWeightLimitsList.FirstOrDefault(tnl => tnl.tickerName == tickername);
                    row["fundname"] = this.tickerLimits.fundName;
                    row["benchmarkname"] = this.tickerLimits.benchmarkName;
                    row["tickername"] = tickername;
                    bool? frozen = null;
                    if (tickerLimits != null && (tickerLimits.customLimit != null || tickerLimits.liveValue!=null || tickerLimits.valid != null))
                    {
                        if (tickerLimits.customLimit != null)
                            row["Custom"] = tickerLimits.customLimit;
                        else
                            row["Custom"] = DBNull.Value;
                        if (tickerLimits.customLimit != null)
                            row["Live"] = tickerLimits.liveValue;
                        else
                            row["Live"] = DBNull.Value;
                        row["Action"] = tickerLimits.action;
                        switch (tickerLimits.valid)
                        {
                            case true:
                                frozen = false;
                                break;
                            case false:
                                frozen = true;
                                break;
                            default:
                                frozen = null;
                                break;
                        }
                        row["Frozen"] = frozen;
                    }
                    else
                    {
                        // If tickerLimits is null, make sure Custom and Live are set to DBNull
                        row["Custom"] = DBNull.Value;
                        row["Live"] = DBNull.Value;
                        row["Action"] = DBNull.Value;
                        row["Frozen"] = DBNull.Value;
                    }
                    row["Default"] = this.tickerLimits.fundLimit.liquidity_limit.defaultValue;
                    bool flagged = TickerLimits.GetFlaggedStatus(tickerLimits.liveValue, tickerLimits.customLimit, this.tickerLimits.fundLimit.liquidity_limit.defaultValue);
                    row["Flagged"] = flagged;
                    double? custom = null;
                    if (double.TryParse(row["custom"].ToString(), out double result_custom))
                    {
                        custom = result_custom;
                    }
                    row["Color"] = SetColor(flagged, frozen, custom);
                    dtWeight.Rows.Add(row);

                }
                return dtWeight;
            }
            catch (Exception ex)
            {
                throw new Exception("populateNotionalTickerLimits error: " + ex.Message);
            }
        }
        
        private DataTable populateTickerLiquidityLimits()
        {
            try
            {
                DataTable dtLiquidity = new DataTable();
                dtLiquidity.Columns.Add("fundname");
                dtLiquidity.Columns.Add("benchmarkname");
                dtLiquidity.Columns.Add("tickername");
                dtLiquidity.Columns.Add("Default");
                dtLiquidity.Columns.Add("Custom",typeof(double));
                dtLiquidity.Columns.Add("Live");
                dtLiquidity.Columns.Add("Action");
                dtLiquidity.Columns.Add("Frozen");
                dtLiquidity.Columns.Add("Flagged",typeof(bool));
                dtLiquidity.Columns.Add("Color");
                foreach (var kvp in this.tickerLimits.dictOfTickers)
                {
                    string tickername = kvp.Key.ToString();
                    string instrument = kvp.Value.ToString();
                    DataRow row = dtLiquidity.NewRow();
                    // Check if the tickername exists in the list of TickerNotionalLimits
                    TickerLiquidityLimits tickerLimits = this.tickerLimits.TickerLiquidityLimitsList.FirstOrDefault(tnl => tnl.tickerName == tickername);
                    row["fundname"] = this.tickerLimits.fundName;
                    row["benchmarkname"] = this.tickerLimits.benchmarkName;
                    row["tickername"] = tickername;
                    bool? frozen = null ;
                    if (tickerLimits != null && (tickerLimits.customLimit != null || tickerLimits.liveValue !=null || tickerLimits.valid !=null))
                    {
                        if (tickerLimits.customLimit != null)
                            row["Custom"] = tickerLimits.customLimit;
                        else
                            row["Custom"] = DBNull.Value;

                        if (tickerLimits.liveValue != null)
                            row["Live"] = tickerLimits.liveValue;
                        else
                            row["Live"] = DBNull.Value;
                        row["Action"] = tickerLimits.action;
                        switch (tickerLimits.valid)
                        {
                            case true:
                                frozen = false;
                                break;
                            case false:
                                frozen = true;
                                break;
                            default:
                                frozen = null;
                                break;
                        }
                        row["Frozen"] = frozen;
                    }
                    else
                    {
                        // If tickerLimits is null, make sure Custom and Live are set to DBNull
                        row["Custom"] = DBNull.Value;
                        row["Live"] = DBNull.Value;
                        row["Action"] = DBNull.Value;
                        row["Frozen"] = DBNull.Value;
                    }

                    row["Default"] = this.tickerLimits.fundLimit.liquidity_limit.defaultValue;
                    bool flagged = TickerLimits.GetFlaggedStatus(tickerLimits.liveValue, tickerLimits.customLimit, this.tickerLimits.fundLimit.liquidity_limit.defaultValue);
                    row["Flagged"] = flagged;
                    double? custom = null;
                    if (double.TryParse(row["custom"].ToString(), out double result_custom))
                    {
                        custom = result_custom;
                    }
                    row["Color"] = SetColor(flagged, frozen, custom);
                    dtLiquidity.Rows.Add(row);

                }
                return dtLiquidity;
            }
            catch (Exception ex)
            {
                throw new Exception("populateLiquidityTickerLimits error: " + ex.Message);
            }
        }
        
        private string SetColor(bool flagged, bool? frozen, double? customValue)
        {
            string strOut = "";
            switch(flagged)
            {
                case true:
                    switch(frozen)
                    {
                        case null:
                            strOut = "R";
                            break;
                        case true:
                            strOut = "R";
                            break;
                        case false:
                            strOut = "Y";
                            break;
                    }
                    break;
                default:
                    strOut = "";
                    break;
            }
            if(strOut=="")
            {
                if(customValue!= null)
                {
                    strOut = "G";
                }
            }
            return strOut;
        }

        private DataTable populateNotionalTickerLimits()
        {
            try
            {
                DataTable dtNotional = new DataTable();
                dtNotional.Columns.Add("fundname");
                dtNotional.Columns.Add("benchmarkname");
                dtNotional.Columns.Add("tickername");
                dtNotional.Columns.Add("Default");
                dtNotional.Columns.Add("Custom", typeof(double)); // Ensure it's double type
                dtNotional.Columns.Add("Live");
                dtNotional.Columns.Add("Action");
                dtNotional.Columns.Add("Frozen");
                dtNotional.Columns.Add("Flagged", typeof(bool));
                dtNotional.Columns.Add("Color");

                // Loop over all tickers in the benchmark
                foreach (var kvp in this.tickerLimits.dictOfTickers)
                {
                    string tickername = kvp.Key.ToString();
                    string instrument = kvp.Value.ToString();
                    DataRow row = dtNotional.NewRow();

                    // Check if the tickername exists in the list of TickerNotionalLimits
                    TickerNotionalLimits tickerLimits = this.tickerLimits.TickerNotionalLimitsList.FirstOrDefault(tnl => tnl.tickerName == tickername);
                    row["fundname"] = this.tickerLimits.fundName;
                    row["benchmarkname"] = this.tickerLimits.benchmarkName;
                    row["tickername"] = tickername;

                    bool? frozen = null;
                    if (tickerLimits != null && (tickerLimits.customLimit != null || tickerLimits.liveValue != null || tickerLimits.valid != null))
                    {
                        // Ensure Custom and Live columns handle null values properly
                        if (tickerLimits.customLimit != null)
                            row["Custom"] = tickerLimits.customLimit; // No need to assign DBNull.Value here
                        else
                            row["Custom"] = DBNull.Value;

                        if (tickerLimits.liveValue != null)
                            row["Live"] = tickerLimits.liveValue; // No need to assign DBNull.Value here
                        else
                            row["Live"] = DBNull.Value;

                        row["Action"] = tickerLimits.action;

                        switch (tickerLimits.valid)
                        {
                            case true:
                                frozen = false;
                                break;
                            case false:
                                frozen = true;
                                break;
                            default:
                                frozen = null;
                                break;
                        }
                        row["Frozen"] = frozen;
                    }
                    else
                    {
                        // If tickerLimits is null, make sure Custom and Live are set to DBNull
                        row["Custom"] = DBNull.Value;
                        row["Live"] = DBNull.Value;
                        row["Action"] = DBNull.Value;
                        row["Frozen"] = DBNull.Value;
                    }

                    double? defaultValue = null;
                    if (instrument == "equity" || instrument == "etf")
                    {
                        defaultValue = this.tickerLimits.fundLimit.stk_notional_pct_limit.defaultValue;
                    }
                    else if (instrument == "future" || instrument == "spread" || instrument == "swap")
                    {
                        defaultValue = this.tickerLimits.fundLimit.fut_notional_pct_limit.defaultValue;
                    }
                    row["Default"] = defaultValue.HasValue ? (object)defaultValue.Value : DBNull.Value; // Ensure Default handles null values

                    bool flagged = TickerLimits.GetFlaggedStatus(tickerLimits?.liveValue, tickerLimits?.customLimit, defaultValue);
                    row["Flagged"] = flagged;
                    double? custom = null;
                    if (double.TryParse(row["custom"].ToString(), out double result_custom))
                    {
                        custom = result_custom;
                    }
                    row["Color"] = SetColor(flagged, frozen,custom);

                    dtNotional.Rows.Add(row);
                }
                return dtNotional;
            }
            catch (Exception ex)
            {
                throw new Exception("populateNotionalTickerLimits error: " + ex.Message);
            }
        }

        /// <summary>
        /// merges multiple datatables into one
        /// </summary>
        /// <param name="tables"></param>
        /// <returns></returns>

        public DataTable MergeDataTables(params DataTable[] tables)
        {
            // Create a new DataTable to hold the merged data
            DataTable mergedTable = new DataTable();

            // Add columns to the merged table
            mergedTable.Columns.Add("benchmarkname");
            mergedTable.Columns.Add("tickername");

            // Add columns from each DataTable in the array
            foreach (var table in tables)
            {
                foreach (DataColumn column in table.Columns)
                {
                    if (!mergedTable.Columns.Contains(column.ColumnName))
                    {
                        mergedTable.Columns.Add(column.ColumnName);
                    }
                }
            }

            // Create a dictionary to store column indices
            var columnIndices = new Dictionary<string, int>();
            for (int i = 0; i < mergedTable.Columns.Count; i++)
            {
                columnIndices.Add(mergedTable.Columns[i].ColumnName, i);
            }

            // Merge the data using full outer join
            var mergedRows = tables.SelectMany(table => table.AsEnumerable())
                                   .GroupBy(row => new { benchmarkname = row["benchmarkname"], tickername = row["tickername"] })
                                   .Select(group => new
                                   {
                                       benchmarkname = group.Key.benchmarkname,
                                       tickername = group.Key.tickername,
                                       rowData = group.SelectMany(row => row.ItemArray),
                                       columns = group.SelectMany(row => row.Table.Columns.Cast<DataColumn>().Select(col => col.ColumnName))
                                   });

            foreach (var mergedRow in mergedRows)
            {
                DataRow newRow = mergedTable.NewRow();

                // Set benchmarkname and tickername values
                newRow["benchmarkname"] = mergedRow.benchmarkname;
                newRow["tickername"] = mergedRow.tickername;

                // Map data from the merged row to the new DataRow
                var dataEnumerator = mergedRow.rowData.GetEnumerator();
                var columnsEnumerator = mergedRow.columns.GetEnumerator();
                while (dataEnumerator.MoveNext() && columnsEnumerator.MoveNext())
                {
                    string columnName = columnsEnumerator.Current.ToString();
                    if (columnIndices.ContainsKey(columnName))
                    {
                        newRow[columnIndices[columnName]] = dataEnumerator.Current;
                    }
                }

                mergedTable.Rows.Add(newRow);
            }

            return mergedTable;
        }

        private void cmdClose_Click(object sender, RoutedEventArgs e)
        {
            RemoveControlRequested?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateFundLimits()
        {
            try
            {
                this.limits.fundLimits.UpdateFundLimits(this.limits.fundLimits.fundname);
                RefreshFundLimits();
                MessageBox.Show("fund_limits updated", "fund limits", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "update fund limits", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void dgFundLimits_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                if (e.EditAction == DataGridEditAction.Commit)
                {
                    this.blnValueChanged = true;
                    //btnUpdateCustom.Visibility = Visibility.Visible;
                    // Get the edited cell's row index
                    int rowIndex = e.Row.GetIndex();

                    // Get the corresponding data item
                    if (dgFundLimits.Items[rowIndex] is DataRowView rowView)
                    {
                        // Access the first column's value
                        var valueOfFund = rowView[0];
                        var valueOfMetric = rowView[1]; // not editable
                        var valueOfDefault = rowView[2];
                        var valueOfCustom = rowView[3];
                        var valueOfLive = rowView[5];//not editable 
                        var valueOfAction = rowView[4];
                        var editedValueAfter = "";
                        //check what the action value was after selection
                        if (e.EditingElement is ComboBox)
                        {
                            if (e.EditingElement != null)
                            {
                                editedValueAfter = (e.EditingElement as ComboBox).SelectedItem.ToString();
                            }
                        }
                        
                        else if (e.EditingElement is TextBox)
                        {
                            editedValueAfter = (e.EditingElement as TextBox).Text;
                        }

                        // Convert to string if necessary
                        string stringValueOfFund = valueOfFund.ToString();
                        string stringValueOfMetric = valueOfMetric.ToString();
                        string stringValueOfDefault = valueOfDefault.ToString();
                        string stringValueOfCustom = (editedValueAfter.ToString() !="" ? editedValueAfter.ToString() : "NULL") ;
                        string stringValueOfAction = editedValueAfter.ToString();
                        //create a new instance of the fundlimit object seeing that you are updating it
                        this.limits.fundLimits = new FundLimits(stringValueOfFund, this.gbl_conn);

                        if (e.Column.DisplayIndex == 3)
                        {
                            double? parsedValue;
                            if (double.TryParse(stringValueOfCustom, out double result))
                            {
                                parsedValue = result;
                                if(valueOfAction.ToString()=="Default")
                                {
                                    stringValueOfAction = "Hard";
                                }
                                else
                                {
                                    stringValueOfAction = valueOfAction.ToString();
                                }
                            }
                            else
                            {
                                parsedValue =null; // or any other default value you prefer
                            }
                            switch (stringValueOfMetric)
                            {
                                case "weight_limit":
                                    this.limits.fundLimits.weight_limit.customValue = parsedValue;
                                    this.limits.fundLimits.weight_limit.updated = true;
                                    this.limits.fundLimits.weight_limit.action = this.limits.fundLimits.GetActionType(stringValueOfAction);
                                    break;
                                case "stk_notional_pct_limit":
                                    this.limits.fundLimits.stk_notional_pct_limit.customValue = parsedValue;
                                    this.limits.fundLimits.stk_notional_pct_limit.updated = true;
                                    this.limits.fundLimits.stk_notional_pct_limit.action = this.limits.fundLimits.GetActionType(stringValueOfAction);
                                    break;
                                case "fut_notional_pct_limit":
                                    this.limits.fundLimits.fut_notional_pct_limit.customValue = parsedValue;
                                    this.limits.fundLimits.fut_notional_pct_limit.updated = true;
                                    this.limits.fundLimits.fut_notional_pct_limit.action = this.limits.fundLimits.GetActionType(stringValueOfAction);
                                    break;
                                case "liquidity_limit":
                                    this.limits.fundLimits.liquidity_limit.customValue = parsedValue;
                                    this.limits.fundLimits.liquidity_limit.updated = true;
                                    this.limits.fundLimits.liquidity_limit.action = this.limits.fundLimits.GetActionType(stringValueOfAction);
                                    break;
                                case "stk_leverage_limit":
                                    this.limits.fundLimits.stk_leverage_limit.customValue = parsedValue;
                                    this.limits.fundLimits.stk_leverage_limit.updated = true;
                                    this.limits.fundLimits.stk_leverage_limit.action = this.limits.fundLimits.GetActionType(stringValueOfAction);
                                    break;
                                case "fut_leverage_limit":
                                    this.limits.fundLimits.fut_leverage_limit.customValue = parsedValue;
                                    this.limits.fundLimits.fut_leverage_limit.updated = true;
                                    this.limits.fundLimits.fut_leverage_limit.action = this.limits.fundLimits.GetActionType(stringValueOfAction);
                                    break;
                                case "leverage_limit":
                                    this.limits.fundLimits.leverage_limit.customValue = parsedValue;
                                    this.limits.fundLimits.leverage_limit.updated = true;
                                    this.limits.fundLimits.leverage_limit.action = this.limits.fundLimits.GetActionType(stringValueOfAction);
                                    break;
                                case "var_limit_factor":
                                    this.limits.fundLimits.var_limit_factor.customValue = parsedValue;
                                    this.limits.fundLimits.var_limit_factor.updated = true;
                                    this.limits.fundLimits.var_limit_factor.action = this.limits.fundLimits.GetActionType(stringValueOfAction);
                                    break;
                                case "stress_limit_factor":
                                    this.limits.fundLimits.stress_limit_factor.customValue = parsedValue;
                                    this.limits.fundLimits.stress_limit_factor.updated = true;
                                    this.limits.fundLimits.stress_limit_factor.action = this.limits.fundLimits.GetActionType(stringValueOfAction);
                                    break;
                                case "drawdown_limit":
                                    this.limits.fundLimits.drawdown_limit.customValue = parsedValue;
                                    this.limits.fundLimits.drawdown_limit.updated = true;
                                    this.limits.fundLimits.drawdown_limit.action = this.limits.fundLimits.GetActionType(stringValueOfAction);
                                    break;
                            }
                        }
                        
                        else if(e.Column.DisplayIndex ==4)
                        {
                            ActionType actionType = this.limits.fundLimits.GetActionType(stringValueOfAction);
                            double? parsedCustomValue = null;
                            if (double.TryParse(valueOfCustom.ToString(), out double result))
                            {
                                if(actionType != ActionType.Default)
                                {
                                    parsedCustomValue = result;
                                }
                            }

                            switch (stringValueOfMetric)
                            {
                                case "weight_limit":
                                    this.limits.fundLimits.weight_limit.action = actionType;
                                    this.limits.fundLimits.weight_limit.updated = true;
                                    this.limits.fundLimits.weight_limit.customValue = parsedCustomValue;
                                    break;
                                case "stk_notional_pct_limit":
                                    this.limits.fundLimits.stk_notional_pct_limit.action = actionType;
                                    this.limits.fundLimits.stk_notional_pct_limit.updated = true;
                                    this.limits.fundLimits.stk_notional_pct_limit.customValue = parsedCustomValue;
                                    break;
                                case "fut_notional_pct_limit":
                                    this.limits.fundLimits.fut_notional_pct_limit.action = actionType;
                                    this.limits.fundLimits.fut_notional_pct_limit.updated = true;
                                    this.limits.fundLimits.fut_notional_pct_limit.customValue = parsedCustomValue;
                                    break;
                                case "liquidity_limit":
                                    this.limits.fundLimits.liquidity_limit.action = actionType;
                                    this.limits.fundLimits.liquidity_limit.updated = true;
                                    this.limits.fundLimits.liquidity_limit.customValue = parsedCustomValue;
                                    break;
                                case "stk_leverage_limit":
                                    this.limits.fundLimits.stk_leverage_limit.action = actionType;
                                    this.limits.fundLimits.stk_leverage_limit.updated = true;
                                    this.limits.fundLimits.stk_leverage_limit.customValue = parsedCustomValue;
                                    break;
                                case "fut_leverage_limit":
                                    this.limits.fundLimits.fut_leverage_limit.action = actionType;
                                    this.limits.fundLimits.fut_leverage_limit.updated = true;
                                    this.limits.fundLimits.fut_leverage_limit.customValue = parsedCustomValue;
                                    break;
                                case "leverage_limit":
                                    this.limits.fundLimits.leverage_limit.action = actionType;
                                    this.limits.fundLimits.leverage_limit.updated = true;
                                    this.limits.fundLimits.leverage_limit.customValue = parsedCustomValue;
                                    break;
                                case "var_limit_factor":
                                    this.limits.fundLimits.var_limit_factor.action = actionType;
                                    this.limits.fundLimits.var_limit_factor.updated = true;
                                    this.limits.fundLimits.var_limit_factor.customValue = parsedCustomValue;
                                    break;
                                case "stress_limit_factor":
                                    this.limits.fundLimits.stress_limit_factor.action = actionType;
                                    this.limits.fundLimits.stress_limit_factor.updated = true;
                                    this.limits.fundLimits.stress_limit_factor.customValue = parsedCustomValue;
                                    break;
                                case "drawdown_limit":
                                    this.limits.fundLimits.drawdown_limit.action = actionType;
                                    this.limits.fundLimits.drawdown_limit.updated = true;
                                    this.limits.fundLimits.drawdown_limit.customValue = parsedCustomValue;
                                    break;
                            }
                        }
                        
                        else if (e.Column.DisplayIndex == 1)
                        {
                            ((TextBox)e.EditingElement).Text = stringValueOfMetric;
                            e.Cancel = true;
                            MessageBox.Show("You can only edit the custom value field", "edit fund limits", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        
                        else if (e.Column.DisplayIndex == 2)
                        {
                            ((TextBox)e.EditingElement).Text = stringValueOfDefault;
                            e.Cancel = true;
                            MessageBox.Show("You can only edit the custom value field", "edit fund limits", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        
                        else if (e.Column.DisplayIndex == 0)
                        {
                            ((TextBox)e.EditingElement).Text = stringValueOfFund;
                            e.Cancel = true;
                            MessageBox.Show("You can only edit the fundname value field", "edit fund limits", MessageBoxButton.OK, MessageBoxImage.Error);
                        }

                        UpdateFundLimits();
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "update fund limits", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }

        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void btnUpdateTickerLimits_Click(object sender, RoutedEventArgs e)
        {

        }
        /// <summary>
        /// this updates the notional limits
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgTickerLimits_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                if (e.EditAction == DataGridEditAction.Commit)
                {
                    // Get the column and row indices of the cell being edited
                    int columnIndex = e.Column.DisplayIndex;
                    int rowIndex = e.Row.GetIndex();
                    string tickername = "";
                    string fundname = "";
                    DataRowView rowView = null;
   
                    if (dgTickerLimits.Items[rowIndex] is DataRowView)
                    {
                        rowView = (DataRowView)dgTickerLimits.Items[rowIndex]; // Assign the DataRowView object to rowView
                    }
                    var editedValueAfter = "";
                    if (e.EditingElement is ComboBox)
                    {
                        if (e.EditingElement != null)
                        {
                            editedValueAfter = (e.EditingElement as ComboBox).SelectedItem.ToString();
                        }
                    }
                    else if (e.EditingElement is TextBox)
                    {
                        editedValueAfter = (e.EditingElement as TextBox).Text;
                    }
                    // Get the column name
                    string columnName = e.Column.Header.ToString();
                    tickername = rowView["tickername"].ToString();
                    fundname = rowView["fundname"].ToString();
                    UpdateTickerLimits(fundname,"Notional",tickername,columnName,rowView,editedValueAfter);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Ticker limits editing error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }
        



        private void UpdateTickerLimits(string fundname,string limitType,string tickername,string columnName,DataRowView rowView, string afterEditValue)
        {
            //if you are at this point you arent updating the action field.
            //so you need to get the value of the action field for the data insert
            string action = "";
            object customValue = null;
            string insertSQL = "INSERT ";
            string runtime = DateTime.UtcNow.ToString("dd-MMM-yyyy HH:mm:ss");

            if (columnName == "Custom")
            {
                customValue = afterEditValue;
                if (customValue == null || customValue.ToString() == "")
                {
                    action = "Default";
                }
                else
                {
                    action = "Hard"; //defaults to hard if you change a custom value
                }
            }
            else if (columnName == "Action")
            {
                action = afterEditValue;
                if (action == "Default")
                {
                    customValue = null;
                }
                else
                {
                    customValue = rowView["Custom"];
                }
            }
            switch(limitType)
            {
                case "Notional":
                    insertSQL += "notional_pct_limits ";
                    break;
                case "Weight":
                    insertSQL += "weight_limits ";
                    break;
                case "Liquidity":
                    insertSQL += "liquidity_limits ";
                    break;
            }
            string stringParsedCustomValue = "";
            if(customValue == null || customValue.ToString() == "")
            {
                stringParsedCustomValue = "NULL";
            }
            else
            {
                double parsedOut;
                if (Double.TryParse(customValue.ToString(), out parsedOut))
                {
                    stringParsedCustomValue = parsedOut.ToString();
                }
                else
                {
                    MessageBox.Show("No custom value was set", "update limits error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            if(stringParsedCustomValue !="")
            {
                insertSQL += " VALUES('" + runtime + "','" + fundname + "','" + tickername + "'," + stringParsedCustomValue + ",'" + action + "')";
                this._db.execSQL_noresults(insertSQL, this.gbl_conn);
                RefreshTickerLimits();
            }
        }
       
        private void UpdateTickerLimits(string tickername, string columnName, string columnValue,DataRowView rowView)
        {

            RefreshTickerLimits();
        }

        private void ComboBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ComboBox_SelectionChanged_2(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ComboBox_SelectionChanged_3(object sender, SelectionChangedEventArgs e)
        {

        }

        private void dgLiquidityLimits_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                if (e.EditAction == DataGridEditAction.Commit)
                {
                    // Get the column and row indices of the cell being edited
                    int columnIndex = e.Column.DisplayIndex;
                    int rowIndex = e.Row.GetIndex();
                    string tickername = "";
                    string fundname = "";
                    DataRowView rowView = null;

                    if (dgLiquidityLimits.Items[rowIndex] is DataRowView)
                    {
                        rowView = (DataRowView)dgLiquidityLimits.Items[rowIndex]; // Assign the DataRowView object to rowView
                    }
                    var editedValueAfter = "";
                    if (e.EditingElement is ComboBox)
                    {
                        if (e.EditingElement != null)
                        {
                            editedValueAfter = (e.EditingElement as ComboBox).SelectedItem.ToString();
                        }
                    }
                    else if (e.EditingElement is TextBox)
                    {
                        editedValueAfter = (e.EditingElement as TextBox).Text;
                    }
                    // Get the column name
                    string columnName = e.Column.Header.ToString();
                    tickername = rowView["tickername"].ToString();
                    fundname = rowView["fundname"].ToString();
                    UpdateTickerLimits(fundname,"Liquidity", tickername, columnName, rowView, editedValueAfter);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ticker limits editing error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private void dgWeightLimits_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                if (e.EditAction == DataGridEditAction.Commit)
                {
                    // Get the column and row indices of the cell being edited
                    int columnIndex = e.Column.DisplayIndex;
                    int rowIndex = e.Row.GetIndex();
                    string tickername = "";
                    string fundname = "";
                    DataRowView rowView = null;

                    if (dgWeightLimits.Items[rowIndex] is DataRowView)
                    {
                        rowView = (DataRowView)dgWeightLimits.Items[rowIndex]; // Assign the DataRowView object to rowView
                    }
                    var editedValueAfter = "";
                    if (e.EditingElement is ComboBox)
                    {
                        if (e.EditingElement != null)
                        {
                            editedValueAfter = (e.EditingElement as ComboBox).SelectedItem.ToString();
                        }
                    }
                    else if (e.EditingElement is TextBox)
                    {
                        editedValueAfter = (e.EditingElement as TextBox).Text;
                    }
                    // Get the column name
                    string columnName = e.Column.Header.ToString();
                    tickername = rowView["tickername"].ToString();
                    fundname = rowView["fundname"].ToString();
                    UpdateTickerLimits(fundname, "Weight", tickername, columnName, rowView, editedValueAfter);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ticker limits editing error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private void ComboBox_SelectionChanged_4(object sender, SelectionChangedEventArgs e)
        {

        }

        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null)
            {
                var parent = VisualTreeHelper.GetParent(child);
                if (parent is T t) return t;
                child = parent;
            }
            return null;
        }

        private static bool ColumnChanged(DataRow row, string col)
        {
            // if we never called AcceptChanges(), nothing to compare to
            if (!row.HasVersion(DataRowVersion.Original)) return true;

            object cur = row[col];
            object orig = row[col, DataRowVersion.Original];

            if (cur == DBNull.Value) cur = null;
            if (orig == DBNull.Value) orig = null;
            return !Equals(cur, orig);
        }

        private static double? ToNullableDouble(object v)
        {
            if (v == null || v == DBNull.Value) return null;
            if (double.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture),
                                NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                return d;
            return null;
        }

        // ---------- one click handler for all 4 grids ----------
        private void RowSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;

                var btn = (Button)sender;
                var grid = FindParent<DataGrid>(btn);
                // make sure current edit is committed
                grid.CommitEdit(DataGridEditingUnit.Cell, true);
                grid.CommitEdit(DataGridEditingUnit.Row, true);

                var drv = btn.DataContext as DataRowView;
                if (drv == null) return;
                var row = drv.Row;

                var tag = (btn.Tag as string) ?? "";

                if (string.Equals(tag, "Fund", StringComparison.OrdinalIgnoreCase))
                {
                    SaveFundLimitsRow(row);
                }
                else if (string.Equals(tag, "Notional", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(tag, "Liquidity", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(tag, "Weight", StringComparison.OrdinalIgnoreCase))
                {
                    SaveTickerLimitRow(tag, row);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Save", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Cursor = Cursors.Arrow;
                UpdateFundLimits();
            }
        }

        // ---------- save logic per table ----------
        private void SaveFundLimitsRow(DataRow row)
        {
            // run only if something relevant changed
            bool customChanged = ColumnChanged(row, "custom");
            bool actionChanged = ColumnChanged(row, "action");
            if (!customChanged && !actionChanged)
            {
                // optional: tell user nothing changed
                // MessageBox.Show("No changes on this row.");
                return;
            }

            string fund = Convert.ToString(row["fundname"]);
            string metric = Convert.ToString(row["metric"]);
            string action = Convert.ToString(row["action"]) ?? "Default";
            double? custom = ToNullableDouble(row["custom"]);

            if (string.Equals(action, "Default", StringComparison.OrdinalIgnoreCase))
                custom = null;
            else if (!custom.HasValue && !string.Equals(action, "Default", StringComparison.OrdinalIgnoreCase))
                return;


                // Refresh the FundLimits instance used for writing:
                this.limits.fundLimits = new FundLimits(fund, this.gbl_conn);

            ActionType at = this.limits.fundLimits.GetActionType(action);

            // Map to your single metric property (mirrors your old switch)
            switch (metric)
            {
                case "weight_limit":
                    this.limits.fundLimits.weight_limit.customValue = custom;
                    this.limits.fundLimits.weight_limit.action = at;
                    this.limits.fundLimits.weight_limit.updated = true;
                    break;

                case "stk_notional_pct_limit":
                    this.limits.fundLimits.stk_notional_pct_limit.customValue = custom;
                    this.limits.fundLimits.stk_notional_pct_limit.action = at;
                    this.limits.fundLimits.stk_notional_pct_limit.updated = true;
                    break;

                case "fut_notional_pct_limit":
                    this.limits.fundLimits.fut_notional_pct_limit.customValue = custom;
                    this.limits.fundLimits.fut_notional_pct_limit.action = at;
                    this.limits.fundLimits.fut_notional_pct_limit.updated = true;
                    break;

                case "liquidity_limit":
                    this.limits.fundLimits.liquidity_limit.customValue = custom;
                    this.limits.fundLimits.liquidity_limit.action = at;
                    this.limits.fundLimits.liquidity_limit.updated = true;
                    break;

                case "stk_leverage_limit":
                    this.limits.fundLimits.stk_leverage_limit.customValue = custom;
                    this.limits.fundLimits.stk_leverage_limit.action = at;
                    this.limits.fundLimits.stk_leverage_limit.updated = true;
                    break;

                case "fut_leverage_limit":
                    this.limits.fundLimits.fut_leverage_limit.customValue = custom;
                    this.limits.fundLimits.fut_leverage_limit.action = at;
                    this.limits.fundLimits.fut_leverage_limit.updated = true;
                    break;

                case "leverage_limit":
                    this.limits.fundLimits.leverage_limit.customValue = custom;
                    this.limits.fundLimits.leverage_limit.action = at;
                    this.limits.fundLimits.leverage_limit.updated = true;
                    break;

                case "var_limit_factor":
                    this.limits.fundLimits.var_limit_factor.customValue = custom;
                    this.limits.fundLimits.var_limit_factor.action = at;
                    this.limits.fundLimits.var_limit_factor.updated = true;
                    break;

                case "stress_limit_factor":
                    this.limits.fundLimits.stress_limit_factor.customValue = custom;
                    this.limits.fundLimits.stress_limit_factor.action = at;
                    this.limits.fundLimits.stress_limit_factor.updated = true;
                    break;

                case "drawdown_limit":
                    this.limits.fundLimits.drawdown_limit.customValue = custom;
                    this.limits.fundLimits.drawdown_limit.action = at;
                    this.limits.fundLimits.drawdown_limit.updated = true;
                    break;

                default:
                    MessageBox.Show("Unknown metric: " + metric, "Save", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
            }

            // write & refresh
            //UpdateFundLimits();                 // reuses your existing method
                                                // row.AcceptChanges();             // optional; RefreshFundLimits() reloads anyway
        }

        private void SaveTickerLimitRow(string limitType, DataRow row)
        {
            // run only if relevant changed
            bool customChanged = ColumnChanged(row, "Custom");
            bool actionChanged = ColumnChanged(row, "Action");
            if (!customChanged && !actionChanged) return;

            string fund = Convert.ToString(row["fundname"]);
            string ticker = Convert.ToString(row["tickername"]);
            string action = Convert.ToString(row["Action"]) ?? "Default";
            double? custom = ToNullableDouble(row["Custom"]);

            if (string.Equals(action, "Default", StringComparison.OrdinalIgnoreCase))
                custom = null;
            else if (!custom.HasValue && !string.Equals(action, "Default", StringComparison.OrdinalIgnoreCase))
                return;

            InsertTickerLimitRow(fund, limitType, ticker, custom, action);
            RefreshTickerLimits();
        }

        private void InsertTickerLimitRow(string fundname, string limitType, string tickername, double? custom, string action)
        {
            string table;
            if (string.Equals(limitType, "Notional", StringComparison.OrdinalIgnoreCase))
                table = "notional_pct_limits";
            else if (string.Equals(limitType, "Liquidity", StringComparison.OrdinalIgnoreCase))
                table = "liquidity_limits";
            else if (string.Equals(limitType, "Weight", StringComparison.OrdinalIgnoreCase))
                table = "weight_limits";
            else
                throw new InvalidOperationException("Unknown limit type: " + limitType);

            string runtime = DateTime.UtcNow.ToString("dd-MMM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            string customSql = custom.HasValue
                ? custom.Value.ToString(CultureInfo.InvariantCulture)
                : "NULL";

            string sql = "INSERT " + table + " VALUES('" + runtime + "','" + fundname + "','" + tickername + "'," + customSql + ",'" + action + "')";
            this._db.execSQL_noresults(sql, this.gbl_conn);
        }

    }
    /// <summary>
    /// this class allows you to create the static 
    /// </summary>
    public class ActionItemsConverter : MarkupExtension, IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var staticItems = new string[] { "Scale", "Hard","Default" };
            var dynamicItems = values[0] as IEnumerable<string>;

            var combinedItems = new List<string>(staticItems);
            if (dynamicItems != null)
                combinedItems.AddRange(dynamicItems);

            return combinedItems;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }



}
