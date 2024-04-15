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
            this.blnValueChanged = false;
            btnUpdateCustom.Visibility = Visibility.Hidden;
            this.FundName = cboFundName.SelectedValue.ToString();
            this.limits = new Limits(this.FundName, this.gbl_conn);
            DataTable dtAll = new DataTable();
            dtAll.Columns.Add("metric");
            dtAll.Columns.Add("default");
            dtAll.Columns.Add("custom");
            dtAll.Columns.Add("live");
            for(int i=0;i<limits.lstFundLimits.Count;i++)
            {
                DataRow row = dtAll.NewRow();
                string limit = limits.lstFundLimits[i];
                row["metric"] = limit;
                switch (limit)
                {
                    case "weight_limit":
                        row["default"] = limits.fundLimits.weight_limit.defaultValue;
                        row["custom"] = limits.fundLimits.weight_limit.customValue;
                        break;
                    case "stk_notional_pct_limit":
                        row["default"] = limits.fundLimits.stk_notional_pct_limit.defaultValue;
                        row["custom"] = limits.fundLimits.stk_notional_pct_limit.customValue;
                        break;
                    case "fut_notional_pct_limit":
                        row["default"] = limits.fundLimits.fut_notional_pct_limit.defaultValue;
                        row["custom"] = limits.fundLimits.fut_notional_pct_limit.customValue;
                        break;
                    case "liquidity_limit":
                        row["default"] = limits.fundLimits.liquidity_limit.defaultValue;
                        row["custom"] = limits.fundLimits.liquidity_limit.customValue;
                        break;
                    case "stk_leverage_limit":
                        row["default"] = limits.fundLimits.stk_leverage_limit.defaultValue;
                        row["custom"] = limits.fundLimits.stk_leverage_limit.customValue;
                        break;
                    case "fut_leverage_limit":
                        row["default"] = limits.fundLimits.fut_leverage_limit.defaultValue;
                        row["custom"] = limits.fundLimits.fut_leverage_limit.customValue;
                        break;
                    case "leverage_limit":
                        row["default"] = limits.fundLimits.leverage_limit.defaultValue;
                        row["custom"] = limits.fundLimits.leverage_limit.customValue;
                        break;
                    case "var_limit_factor":
                        row["default"] = limits.fundLimits.var_limit_factor.defaultValue;
                        row["custom"] = limits.fundLimits.var_limit_factor.customValue;
                        break;
                    case "stress_limit_factor":
                        row["default"] = limits.fundLimits.stress_limit_factor.defaultValue;
                        row["custom"] = limits.fundLimits.stress_limit_factor.customValue;
                        break;
                    case "drawdown_limit":
                        row["default"] = limits.fundLimits.drawdown_limit.defaultValue;
                        row["custom"] = limits.fundLimits.drawdown_limit.customValue;
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
            string action = (cboAction.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (this.blnValueChanged && action !="")
            {
                try
                {
                    this.limits.fundLimits.UpdateFundLimits(action);
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
                        var valueOfFirstColumn = rowView[0]; // Assuming the first column's index is 0
                        var valueOfSecondColumn = rowView[1];
                        var editedValueAfter = (e.EditingElement as TextBox).Text;
                        var valueOfFourthColumn = rowView[3];
                        // Convert to string if necessary
                        string stringValueOfFirstColumn = valueOfFirstColumn.ToString();
                        string stringValueOfSecondColumn = valueOfSecondColumn.ToString();
                        string stringValueOfThirdColumn = (editedValueAfter.ToString() !="" ? editedValueAfter.ToString() : "NULL") ;
                        string stringValueOfFourthColumn = valueOfFourthColumn.ToString();
                        if (e.Column.DisplayIndex == 2)
                        {
                            double? parsedValue;
                            if (double.TryParse(stringValueOfThirdColumn, out double result))
                            {
                                parsedValue = result;
                            }
                            else
                            {
                                parsedValue =null; // or any other default value you prefer
                            }
                            switch (stringValueOfFirstColumn)
                            {
                                case "weight_limit":
                                    this.limits.fundLimits.weight_limit.customValue = parsedValue;
                                    break;
                                case "stk_notional_pct_limit":
                                    this.limits.fundLimits.stk_notional_pct_limit.customValue = parsedValue;
                                    break;
                                case "fut_notional_pct_limit":
                                    this.limits.fundLimits.fut_notional_pct_limit.customValue = parsedValue;
                                    break;
                                case "liquidity_limit":
                                    this.limits.fundLimits.liquidity_limit.customValue = parsedValue;
                                    break;
                                case "stk_leverage_limit":
                                    this.limits.fundLimits.stk_leverage_limit.customValue = parsedValue;
                                    break;
                                case "fut_leverage_limit":
                                    this.limits.fundLimits.fut_leverage_limit.customValue = parsedValue;
                                    break;
                                case "leverage_limit":
                                    this.limits.fundLimits.leverage_limit.customValue = parsedValue;
                                    break;
                                case "var_limit_factor":
                                    this.limits.fundLimits.var_limit_factor.customValue = parsedValue;
                                    break;
                                case "stress_limit_factor":
                                    this.limits.fundLimits.stress_limit_factor.customValue = parsedValue;
                                    break;
                                case "drawdown_limit":
                                    this.limits.fundLimits.drawdown_limit.customValue = parsedValue;
                                    break;
                            }
                        }
                        else if (e.Column.DisplayIndex == 0)
                        {
                            ((TextBox)e.EditingElement).Text = stringValueOfFirstColumn;
                            e.Cancel = true;
                            MessageBox.Show("You can only edit the custom value field", "edit fund limits", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else if (e.Column.DisplayIndex == 1)
                        {
                            ((TextBox)e.EditingElement).Text = stringValueOfSecondColumn;
                            e.Cancel = true;
                            MessageBox.Show("You can only edit the custom value field", "edit fund limits", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else if (e.Column.DisplayIndex == 3)
                        {
                            ((TextBox)e.EditingElement).Text = stringValueOfFourthColumn;
                            e.Cancel = true;
                            MessageBox.Show("You can only edit the custom value field", "edit fund limits", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "update fund limits", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
    }
}
