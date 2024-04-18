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
        Limits limits;
        string FundName;
        bool blnValueChanged = false;
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
        }
        private void RefreshFundLimits()
        {
            this.blnValueChanged = false;
            btnUpdateCustom.Visibility = Visibility.Hidden;
            this.FundName = cboFundName.SelectedValue.ToString();
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
                        break;
                    case "fut_leverage_limit":
                        row["default"] = limits.fundLimits.fut_leverage_limit.defaultValue;
                        row["custom"] = limits.fundLimits.fut_leverage_limit.customValue;
                        row["action"] = limits.fundLimits.fut_leverage_limit.action;
                        break;
                    case "leverage_limit":
                        row["default"] = limits.fundLimits.leverage_limit.defaultValue;
                        row["custom"] = limits.fundLimits.leverage_limit.customValue;
                        row["action"] = limits.fundLimits.leverage_limit.action;
                        break;
                    case "var_limit_factor":
                        row["default"] = limits.fundLimits.var_limit_factor.defaultValue;
                        row["custom"] = limits.fundLimits.var_limit_factor.customValue;
                        row["action"] = limits.fundLimits.var_limit_factor.action;
                        break;
                    case "stress_limit_factor":
                        row["default"] = limits.fundLimits.stress_limit_factor.defaultValue;
                        row["custom"] = limits.fundLimits.stress_limit_factor.customValue;
                        row["action"] = limits.fundLimits.stress_limit_factor.action;
                        break;
                    case "drawdown_limit":
                        row["default"] = limits.fundLimits.drawdown_limit.defaultValue;
                        row["custom"] = limits.fundLimits.drawdown_limit.customValue;
                        row["action"] = limits.fundLimits.drawdown_limit.action;
                        break;
                }
                dtAll.Rows.Add(row);
            }
            dgFundLimits.ItemsSource = dtAll.DefaultView;
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
