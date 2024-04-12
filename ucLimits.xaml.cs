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
        string FundName;
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
            this.FundName = cboFundName.SelectedValue.ToString();
            Limits limits = new Limits(this.FundName, this.gbl_conn);
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
    }
}
