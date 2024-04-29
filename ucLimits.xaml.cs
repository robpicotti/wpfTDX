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
        string FundName;
        TickerLimits tickerLimits;
        bool blnValueChanged = false; 
        bool blnTickerLimitChanged = false;
        TDX.db _db = new TDX.db();

        public ucLimits(SqlConnection conn)
        {
            InitializeComponent();
            this.gbl_conn = conn;
            LoadForm();
        }
        private void LoadForm()
        {
            dtFunds = _db.get_funds(gbl_conn);
            cboFundName.Items.Clear();
            foreach (DataRow row in dtFunds.Rows)
            {
                cboFundName.Items.Add(row["fundname"].ToString());
            }


        }
        private void cboFundName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshFundLimits();
            RefreshTickerLimits();
        }
        private void RefreshFundLimits()
        {
            this.blnValueChanged = false;
           
            btnUpdateCustom.Visibility = Visibility.Hidden;
            this.FundName = cboFundName.SelectedValue.ToString();
            //set up the fund limits
            this.limits = new Limits(this.FundName, this.gbl_conn);
            DataTable dtAll = new DataTable();
            dtAll.Columns.Add("metric");
            dtAll.Columns.Add("default");
            dtAll.Columns.Add("custom");
            dtAll.Columns.Add("action");
            dtAll.Columns.Add("live");
            for (int i = 0; i < limits.lstFundLimits.Count; i++)
            {
                DataRow row = dtAll.NewRow();
                string limit = limits.lstFundLimits[i];
                row["metric"] = limit;
                switch (limit)
                {
                    case "weight_limit":
                        row["default"] = limits.fundLimits.weight_limit.defaultValue;
                        row["custom"] = limits.fundLimits.weight_limit.customValue;
                        row["action"] = limits.fundLimits.weight_limit.action;
                        break;
                    case "stk_notional_pct_limit":
                        row["default"] = limits.fundLimits.stk_notional_pct_limit.defaultValue;
                        row["custom"] = limits.fundLimits.stk_notional_pct_limit.customValue;
                        row["action"] = limits.fundLimits.stk_notional_pct_limit.action;
                        break;
                    case "fut_notional_pct_limit":
                        row["default"] = limits.fundLimits.fut_notional_pct_limit.defaultValue;
                        row["custom"] = limits.fundLimits.fut_notional_pct_limit.customValue;
                        row["action"] = limits.fundLimits.fut_notional_pct_limit.action;
                        break;
                    case "liquidity_limit":
                        row["default"] = limits.fundLimits.liquidity_limit.defaultValue;
                        row["custom"] = limits.fundLimits.liquidity_limit.customValue;
                        row["action"] = limits.fundLimits.liquidity_limit.action;
                        break;
                    case "stk_leverage_limit":
                        row["default"] = limits.fundLimits.stk_leverage_limit.defaultValue;
                        row["custom"] = limits.fundLimits.stk_leverage_limit.customValue;
                        row["action"] = limits.fundLimits.stk_leverage_limit.action;
                        row["live"] = limits.fundLimits.stk_leverage_limit.liveValue;
                        break;
                    case "fut_leverage_limit":
                        row["default"] = limits.fundLimits.fut_leverage_limit.defaultValue;
                        row["custom"] = limits.fundLimits.fut_leverage_limit.customValue;
                        row["action"] = limits.fundLimits.fut_leverage_limit.action;
                        row["live"] = limits.fundLimits.fut_leverage_limit.liveValue;
                        break;
                    case "leverage_limit":
                        row["default"] = limits.fundLimits.leverage_limit.defaultValue;
                        row["custom"] = limits.fundLimits.leverage_limit.customValue;
                        row["action"] = limits.fundLimits.leverage_limit.action;
                        row["live"] = limits.fundLimits.leverage_limit.liveValue;
                        break;
                    case "var_limit_factor":
                        row["default"] = limits.fundLimits.var_limit_factor.defaultValue;
                        row["custom"] = limits.fundLimits.var_limit_factor.customValue;
                        row["action"] = limits.fundLimits.var_limit_factor.action;
                        row["live"] = limits.fundLimits.var_limit_factor.liveValue;
                        break;
                    case "stress_limit_factor":
                        row["default"] = limits.fundLimits.stress_limit_factor.defaultValue;
                        row["custom"] = limits.fundLimits.stress_limit_factor.customValue;
                        row["action"] = limits.fundLimits.stress_limit_factor.action;
                        row["live"] = limits.fundLimits.stress_limit_factor.liveValue;
                        break;
                    case "drawdown_limit":
                        row["default"] = limits.fundLimits.drawdown_limit.defaultValue;
                        row["custom"] = limits.fundLimits.drawdown_limit.customValue;
                        row["action"] = limits.fundLimits.drawdown_limit.action;
                        row["live"] = limits.fundLimits.drawdown_limit.liveValue;
                        break;
                }
                dtAll.Rows.Add(row);
            }
            dgFundLimits.ItemsSource = dtAll.DefaultView;

        }
       
        private void RefreshTickerLimits()
        {
            this.blnTickerLimitChanged = false;
            btnUpdateTickerLimits.Visibility = Visibility.Hidden;
            DataTable dtNotional = new DataTable();
            DataTable dtLiquidity = new DataTable();
            DataTable dtWeights = new DataTable();
            this.FundName = cboFundName.SelectedValue.ToString();
            //set up the ticker limits based on the fund
            this.tickerLimits = new TickerLimits(this.FundName,this.limits.fundLimits, this.gbl_conn);
            dtNotional = populateNotionalTickerLimits();
            dtLiquidity = populateTickerLiquidityLimits();
            dtWeights = populateTickerWeightLimits();
            //dtMergedData = MergeDataTables(dtNotional, dtLiquidity, dtWeights);
            //dgTickerLimits.ItemsSource = dtMergedData.DefaultView;
            dgTickerLimits.ItemsSource = dtNotional.DefaultView;
            dgLiquidityLimits.ItemsSource = dtLiquidity.DefaultView;
            dgWeightLimits.ItemsSource = dtWeights.DefaultView;
        }
        
        private DataTable populateTickerWeightLimits()
        {
            try
            {
                DataTable dtWeight = new DataTable();
                dtWeight.Columns.Add("benchmarkname");
                dtWeight.Columns.Add("tickername");
                dtWeight.Columns.Add("Default");
                dtWeight.Columns.Add("Custom");
                dtWeight.Columns.Add("Live");
                dtWeight.Columns.Add("Action");
                foreach (var kvp in this.tickerLimits.dictOfTickers)
                {
                    string tickername = kvp.Key.ToString();
                    string instrument = kvp.Value.ToString();
                    DataRow row = dtWeight.NewRow();
                    // Check if the tickername exists in the list of TickerNotionalLimits
                    TickerWeightLimits tickerLimits = this.tickerLimits.TickerWeightLimitsList.FirstOrDefault(tnl => tnl.tickerName == tickername);
                    row["benchmarkname"] = this.tickerLimits.benchmarkName;
                    row["tickername"] = tickername;
                    if (tickerLimits != null)
                    {
                        row["Custom"] = tickerLimits.customLimit;
                        row["Live"] = tickerLimits.liveValue;
                        row["Action"] = tickerLimits.action;
                    }
                    else
                    {
                        row["Default"] = this.tickerLimits.fundLimit.liquidity_limit.defaultValue;
                    }
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
                dtLiquidity.Columns.Add("benchmarkname");
                dtLiquidity.Columns.Add("tickername");
                dtLiquidity.Columns.Add("Default");
                dtLiquidity.Columns.Add("Custom");
                dtLiquidity.Columns.Add("Live");
                dtLiquidity.Columns.Add("Action");
                foreach (var kvp in this.tickerLimits.dictOfTickers)
                {
                    string tickername = kvp.Key.ToString();
                    string instrument = kvp.Value.ToString();
                    DataRow row = dtLiquidity.NewRow();
                    // Check if the tickername exists in the list of TickerNotionalLimits
                    TickerLiquidityLimits tickerLimits = this.tickerLimits.TickerLiquidityLimitsList.FirstOrDefault(tnl => tnl.tickerName == tickername);
                    row["benchmarkname"] = this.tickerLimits.benchmarkName;
                    row["tickername"] = tickername;
                    if (tickerLimits != null)
                    {
                        row["Custom"] = tickerLimits.customLimit;
                        row["Live"] = tickerLimits.liveValue;
                        row["Action"] = tickerLimits.action;
                    }
                    else
                    {
                        row["Default"] = this.tickerLimits.fundLimit.liquidity_limit.defaultValue;
                    }
                    dtLiquidity.Rows.Add(row);

                }
                return dtLiquidity;
            }
            catch (Exception ex)
            {
                throw new Exception("populateLiquidityTickerLimits error: " + ex.Message);
            }
        }
        
        private DataTable populateNotionalTickerLimits()
        {
            try
            {
                DataTable dtNotional = new DataTable();
                dtNotional.Columns.Add("benchmarkname");
                dtNotional.Columns.Add("tickername");
                dtNotional.Columns.Add("Default");
                dtNotional.Columns.Add("Custom");
                dtNotional.Columns.Add("Live");
                dtNotional.Columns.Add("Action");
                //loop over all tickers in the benchmark
                foreach(var kvp in this.tickerLimits.dictOfTickers)
                {
                    string tickername = kvp.Key.ToString();
                    string instrument = kvp.Value.ToString();
                    DataRow row = dtNotional.NewRow();
                    // Check if the tickername exists in the list of TickerNotionalLimits
                    TickerNotionalLimits tickerLimits = this.tickerLimits.TickerNotionalLimitsList.FirstOrDefault(tnl => tnl.tickerName == tickername);
                    row["benchmarkname"] = this.tickerLimits.benchmarkName;
                    row["tickername"] = tickername;
                    if (tickerLimits != null)
                    {
                        row["Custom"] = tickerLimits.customLimit;
                        row["Live"] = tickerLimits.liveValue;
                        row["action"] = tickerLimits.action;
                    }
                    else
                    {
                        if(instrument=="equity" || instrument=="etf")
                        {
                            row["Default"] = this.tickerLimits.fundLimit.stk_notional_pct_limit.defaultValue;
                        }
                        else if(instrument == "future" )
                        {
                            row["Default"] = this.tickerLimits.fundLimit.fut_notional_pct_limit.defaultValue;
                        }
                    }
                    dtNotional.Rows.Add(row);

                }

                return dtNotional;
            }
            catch(Exception ex)
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

        private void btnUpdateCustom_Click(object sender, RoutedEventArgs e)
        {
            if (this.blnValueChanged)
            {
                try
                {
                    this.limits.fundLimits.UpdateFundLimits();
                    RefreshFundLimits();
                    MessageBox.Show("fund_limits updated", "fund limits", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "update fund limits", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void dgFundLimits_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            try
            {
                if (e.EditAction == DataGridEditAction.Commit)
                {
                    this.blnValueChanged = true;
                    btnUpdateCustom.Visibility = Visibility.Visible;
                    // Get the edited cell's row index
                    int rowIndex = e.Row.GetIndex();

                    // Get the corresponding data item
                    if (dgFundLimits.Items[rowIndex] is DataRowView rowView)
                    {
                        // Access the first column's value
                        var valueOfMetric = rowView[0]; // not editable
                        var valueOfDefault = rowView[1];
                        var valueOfCustom = rowView[2];
                        var valueOfLive = rowView[4];//not editable 
                        var valueOfAction = rowView[3];
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
                        string stringValueOfMetric = valueOfMetric.ToString();
                        string stringValueOfDefault = valueOfDefault.ToString();
                        string stringValueOfCustom = (editedValueAfter.ToString() !="" ? editedValueAfter.ToString() : "NULL") ;
                        string stringValueOfAction = editedValueAfter.ToString();
                        if (e.Column.DisplayIndex == 2)
                        {
                            double? parsedValue;
                            if (double.TryParse(stringValueOfCustom, out double result))
                            {
                                parsedValue = result;
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
                                    break;
                                case "stk_notional_pct_limit":
                                    this.limits.fundLimits.stk_notional_pct_limit.customValue = parsedValue;
                                    this.limits.fundLimits.stk_notional_pct_limit.updated = true;
                                    break;
                                case "fut_notional_pct_limit":
                                    this.limits.fundLimits.fut_notional_pct_limit.customValue = parsedValue;
                                    this.limits.fundLimits.fut_notional_pct_limit.updated = true;
                                    break;
                                case "liquidity_limit":
                                    this.limits.fundLimits.liquidity_limit.customValue = parsedValue;
                                    this.limits.fundLimits.liquidity_limit.updated = true;
                                    break;
                                case "stk_leverage_limit":
                                    this.limits.fundLimits.stk_leverage_limit.customValue = parsedValue;
                                    this.limits.fundLimits.stk_leverage_limit.updated = true;
                                    break;
                                case "fut_leverage_limit":
                                    this.limits.fundLimits.fut_leverage_limit.customValue = parsedValue;
                                    this.limits.fundLimits.fut_leverage_limit.updated = true;
                                    break;
                                case "leverage_limit":
                                    this.limits.fundLimits.leverage_limit.customValue = parsedValue;
                                    this.limits.fundLimits.leverage_limit.updated = true;
                                    break;
                                case "var_limit_factor":
                                    this.limits.fundLimits.var_limit_factor.customValue = parsedValue;
                                    this.limits.fundLimits.var_limit_factor.updated = true;
                                    break;
                                case "stress_limit_factor":
                                    this.limits.fundLimits.stress_limit_factor.customValue = parsedValue;
                                    this.limits.fundLimits.stress_limit_factor.updated = true;
                                    break;
                                case "drawdown_limit":
                                    this.limits.fundLimits.drawdown_limit.customValue = parsedValue;
                                    this.limits.fundLimits.drawdown_limit.updated = true;
                                    break;
                            }
                        }
                        else if(e.Column.DisplayIndex ==3)
                        {
                            ActionType actionType = this.limits.fundLimits.GetActionType(stringValueOfAction);
                            switch (stringValueOfMetric)
                            {
                                case "weight_limit":
                                    this.limits.fundLimits.weight_limit.action = actionType;
                                    this.limits.fundLimits.weight_limit.updated = true;
                                    break;
                                case "stk_notional_pct_limit":
                                    this.limits.fundLimits.stk_notional_pct_limit.action = actionType;
                                    this.limits.fundLimits.stk_notional_pct_limit.updated = true;
                                    break;
                                case "fut_notional_pct_limit":
                                    this.limits.fundLimits.fut_notional_pct_limit.action = actionType;
                                    this.limits.fundLimits.fut_notional_pct_limit.updated = true;
                                    break;
                                case "liquidity_limit":
                                    this.limits.fundLimits.liquidity_limit.action = actionType;
                                    this.limits.fundLimits.liquidity_limit.updated = true;
                                    break;
                                case "stk_leverage_limit":
                                    this.limits.fundLimits.stk_leverage_limit.action = actionType;
                                    this.limits.fundLimits.stk_leverage_limit.updated = true;
                                    break;
                                case "fut_leverage_limit":
                                    this.limits.fundLimits.fut_leverage_limit.action = actionType;
                                    this.limits.fundLimits.fut_leverage_limit.updated = true;
                                    break;
                                case "leverage_limit":
                                    this.limits.fundLimits.leverage_limit.action = actionType;
                                    this.limits.fundLimits.leverage_limit.updated = true;
                                    break;
                                case "var_limit_factor":
                                    this.limits.fundLimits.var_limit_factor.action = actionType;
                                    this.limits.fundLimits.var_limit_factor.updated = true;
                                    break;
                                case "stress_limit_factor":
                                    this.limits.fundLimits.stress_limit_factor.action = actionType;
                                    this.limits.fundLimits.stress_limit_factor.updated = true;
                                    break;
                                case "drawdown_limit":
                                    this.limits.fundLimits.drawdown_limit.action = actionType;
                                    this.limits.fundLimits.drawdown_limit.updated = true;
                                    break;
                            }
                        }
                        
                        else if (e.Column.DisplayIndex == 0)
                        {
                            ((TextBox)e.EditingElement).Text = stringValueOfMetric;
                            e.Cancel = true;
                            MessageBox.Show("You can only edit the custom value field", "edit fund limits", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else if (e.Column.DisplayIndex == 1)
                        {
                            ((TextBox)e.EditingElement).Text = stringValueOfDefault;
                            e.Cancel = true;
                            MessageBox.Show("You can only edit the custom value field", "edit fund limits", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        //else if (e.Column.DisplayIndex == 3)
                        //{
                        //    ((TextBox)e.EditingElement).Text = stringValueOfFourthColumn;
                        //    e.Cancel = true;
                        //    MessageBox.Show("You can only edit the custom value field", "edit fund limits", MessageBoxButton.OK, MessageBoxImage.Error);
                        //}
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "update fund limits", MessageBoxButton.OK, MessageBoxImage.Error);
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
                if (e.EditAction == DataGridEditAction.Commit)
                {
                    // Get the column and row indices of the cell being edited
                    int columnIndex = e.Column.DisplayIndex;
                    int rowIndex = e.Row.GetIndex();
                    string tickername = "";
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
                    UpdateTickerLimits("Notional",tickername,columnName,rowView,editedValueAfter);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Ticker limits editing error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void UpdateTickerLimits(string limitType,string tickername,string columnName,DataRowView rowView, string afterEditValue)
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
                action = rowView["Action"].ToString();
            }
            else if (columnName == "Action")
            {
                action = afterEditValue;
                customValue = rowView["Custom"];
            }
            switch(limitType)
            {
                case "Notional":
                    insertSQL += "notional_limits ";
                    break;
                case "Weight":
                    insertSQL += "weight_limits ";
                    break;
                case "Liquidity":
                    insertSQL += "liquidity_limits ";
                    break;
            }
            if (customValue != null)
            {
                double parsedOut;
                if (Double.TryParse(customValue.ToString(), out parsedOut))
                {
                    insertSQL += " VALUES('" + runtime + "','" + this.FundName + "','" + tickername + "'," + parsedOut.ToString() + ",'" + action + "')";
                    this._db.execSQL_noresults(insertSQL, this.gbl_conn);
                    RefreshTickerLimits();
                }
            }
            else
            {
                MessageBox.Show("No custom value was set", "update limits error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                if (e.EditAction == DataGridEditAction.Commit)
                {
                    // Get the column and row indices of the cell being edited
                    int columnIndex = e.Column.DisplayIndex;
                    int rowIndex = e.Row.GetIndex();
                    string tickername = "";
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
                    UpdateTickerLimits("Liquidity", tickername, columnName, rowView, editedValueAfter);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ticker limits editing error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dgWeightLimits_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            try
            {
                if (e.EditAction == DataGridEditAction.Commit)
                {
                    // Get the column and row indices of the cell being edited
                    int columnIndex = e.Column.DisplayIndex;
                    int rowIndex = e.Row.GetIndex();
                    string tickername = "";
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
                    UpdateTickerLimits("Weight", tickername, columnName, rowView, editedValueAfter);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ticker limits editing error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ComboBox_SelectionChanged_4(object sender, SelectionChangedEventArgs e)
        {

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
