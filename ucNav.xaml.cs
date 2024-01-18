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
    /// Interaction logic for ucNav.xaml
    /// </summary>
    public partial class ucNav : UserControl
    {
        SqlConnection gbl_conn;
        db _db = new db();
        DateTime calEnd;
        DateTime calStart;
        DataTable dtFunds;
        List<string> lstNumericColumns = new List<string>();
        public event EventHandler RemoveControlRequested;
        public ucNav(SqlConnection conn)
        {
            InitializeComponent();
            gbl_conn = conn;
            LoadForm();
        }
        /// <summary>
        /// initialize the form methods
        /// </summary>
        public void LoadForm()
        {
            PopulateControls();
            BuildNumericColumnLists();
            dgNav.AutoGeneratingColumn += datagrid_AutoGeneratingColumn;
        }
        /// <summary>
        /// populate all the user controls on form
        /// </summary>
        public void PopulateControls()
        {
            dtFunds = _db.get_funds(gbl_conn);
            cboFundname.Items.Clear();
            foreach (DataRow row in dtFunds.Rows)
            {
                cboFundname.Items.Add(row["fundname"].ToString());
            }
            cboFundname.SelectedIndex = 0;
        }

        /// <summary>
        /// run the nav
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdRun_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                GetNav();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Nav", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        /// <summary>
        /// get the nav
        /// </summary>
        private void GetNav()
        {
            string fund_name = cboFundname.SelectedItem.ToString();
            string nav_date = dtPickerPostionFrom.SelectedDate.Value.ToString("yyyy-MM-dd HH:mm:ss");
            string nav_date_end = dtPickerPostionFrom.SelectedDate.Value.AddHours(23).AddMinutes(59).AddSeconds(59).ToString("yyyy-MM-dd HH:mm:ss");
            string where_clause = "WHERE fundname='" + fund_name + "' AND navDate BETWEEN '" + nav_date + "' AND '"
                        + nav_date_end + "'";
            NAV nv = new NAV(fund_name, gbl_conn, where_clause);
            DataTable dt = nv.all_data;
            DataTable groupedTable = new DataTable();
            groupedTable.Columns.Add("navDate", typeof(DateTime));
            groupedTable.Columns.Add("nav", typeof(int));
            groupedTable.Columns.Add("equities_liquidation", typeof(int));
            groupedTable.Columns.Add("futures_liquidation", typeof(int));
            groupedTable.Columns.Add("deposits", typeof(int));
            groupedTable.Columns.Add("cash", typeof(int));
            groupedTable.Columns.Add("cash_legs", typeof(int));
            groupedTable.Columns.Add("commission", typeof(int));
            groupedTable.Columns.Add("withdrawals", typeof(int));
            groupedTable.Columns.Add("other_fees", typeof(int));
            groupedTable.Columns.Add("payments", typeof(int));
            groupedTable.Columns.Add("receivables", typeof(int));
            groupedTable.Columns.Add("comm_fee_adj", typeof(int));
            groupedTable.Columns.Add("comm_fee", typeof(int));

            var groupedData = from row in dt.AsEnumerable()
                group row by row.Field<DateTime>("navDate") into dateGroup
                select new
                {
                    navDate = dateGroup.Key,
                    equities_liquidation = dateGroup.Sum(equities_liquidation => equities_liquidation.Field<double?>("equities_liquidation") ?? 0.0),
                    futures_liquidation = dateGroup.Sum(futures_liquidation => futures_liquidation.Field<double?>("futures_liquidation") ?? 0.0),
                    deposits = dateGroup.Sum(deposits => deposits.Field<double?>("deposits") ?? 0.0),
                    cash = dateGroup.Sum(cash => cash.Field<double?>("cash") ?? 0.0),
                    cash_legs = dateGroup.Sum(cash_legs => cash_legs.Field<double?>("cash_legs") ?? 0.0),
                    commission = dateGroup.Sum(commission => commission.Field<double?>("commission") ?? 0.0),
                    withdrawals = dateGroup.Sum(withdrawls => withdrawls.Field<double?>("withdrawals") ?? 0.0),
                    other_fees = dateGroup.Sum(other_fees => other_fees.Field<double?>("other_fees") ?? 0.0),
                    payments = dateGroup.Sum(payments => payments.Field<double?>("payments") ?? 0.0),
                    receivables = dateGroup.Sum(receivables => receivables.Field<double?>("receivables") ?? 0.0),
                    comm_fee_adj = dateGroup.Sum(comm_fee_adj => comm_fee_adj.Field<double?>("comm_fee_adj") ?? 0.0),
                    comm_fee = dateGroup.Sum(comm_fee => comm_fee.Field<double?>("comm_fee") ?? 0.0),
                };
            foreach (var group in groupedData)
            {
                DataRow newRow = groupedTable.NewRow();
                newRow["navDate"] = group.navDate;
                newRow["equities_liquidation"] = group.equities_liquidation;
                newRow["futures_liquidation"] = group.futures_liquidation;
                newRow["deposits"] = group.deposits;
                newRow["cash"] = group.cash;
                newRow["cash_legs"] = group.cash_legs;
                newRow["commission"] = group.commission;
                newRow["withdrawals"] = group.withdrawals;
                newRow["other_fees"] = group.other_fees;
                newRow["payments"] = group.payments;
                newRow["receivables"] = group.receivables;
                newRow["comm_fee_adj"] = group.comm_fee_adj;
                newRow["comm_fee"] = group.comm_fee;
                newRow["nav"] = group.equities_liquidation + group.futures_liquidation + group.deposits + group.cash + group.cash_legs - group.commission +
                    group.withdrawals - group.other_fees + group.payments + group.receivables - group.comm_fee_adj - group.comm_fee;
                groupedTable.Rows.Add(newRow);
            }
            dgNav.ItemsSource = groupedTable.DefaultView;
        }

        /// <summary>
        /// remove the user control from main window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdClose_Click(object sender, RoutedEventArgs e)
        {
            RemoveControlRequested?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// build a list of all the numeric columns
        /// </summary>
        private void BuildNumericColumnLists()
        {
            lstNumericColumns.Add("nav");
            lstNumericColumns.Add("equities_liquidation");
            lstNumericColumns.Add("futures_liquidation");
            lstNumericColumns.Add("deposits");
            lstNumericColumns.Add("cash");
            lstNumericColumns.Add("cash_legs");
            lstNumericColumns.Add("commission");
            lstNumericColumns.Add("withdrawls");
            lstNumericColumns.Add("other_fees");
            lstNumericColumns.Add("payments");
            lstNumericColumns.Add("receivables");
            lstNumericColumns.Add("comm_fee_adj");
            lstNumericColumns.Add("comm_fee");
        }

        private void datagrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (lstNumericColumns.Contains(e.PropertyName))
            {
                var textColumn = e.Column as DataGridTextColumn;
                if (textColumn != null)
                {
                    // Set the StringFormat to "N" for thousand separator
                    textColumn.Binding = new Binding(e.PropertyName) { StringFormat = "N" };
                }
            }
        }
    }
}
