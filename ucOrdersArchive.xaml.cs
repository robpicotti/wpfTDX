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
using TDX;
using System.Data.SqlClient;
using System.Data;

namespace wpfTDX
{
    /// <summary>
    /// Interaction logic for ucOrdersArchive.xaml
    /// </summary>
    public partial class ucOrdersArchive : UserControl
    {
        public event EventHandler RemoveControlRequested;
        SqlConnection gbl_conn;
        DataTable dtFunds;
        db _db = new db();
        public ucOrdersArchive(SqlConnection conn)
        {
            InitializeComponent();
            gbl_conn = conn;
            LoadForm();
        }
        private void LoadForm()
        {
            PopuldateControls();
        }
        private void PopuldateControls()
        {
            //populate funds
            dtFunds = _db.get_funds(gbl_conn);
            cboFundName.Items.Clear();
            foreach (DataRow row in dtFunds.Rows)
            {
                cboFundName.Items.Add(row["fundname"].ToString());
            }
            //set datetime picker dates
            dtpFromDate.SelectedDate = DateTime.Today;
            dtpEndDate.SelectedDate = DateTime.Today;

        }
        private void cboFundName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cboSubaccountName.Items.Clear();
            cboSubaccountName.Text = null;
            Account account = new Account(cboFundName.SelectedValue.ToString(), gbl_conn);
            foreach (Subaccount item in account.subaccounts_list)
            {
                cboSubaccountName.Items.Add(item.subaccount_name);
            }
        }

        private void rbViewOrders_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                DateTime startDate = (DateTime)dtpFromDate.SelectedDate;
                DateTime endDate = (DateTime)dtpEndDate.SelectedDate;
                dgOrders.ItemsSource = GetOrders(cboSubaccountName.Text,startDate,endDate).DefaultView;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "get oroders", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }
        /// <summary>
        /// returns all the orders where the subaccountname is in the subaccounts list in orders table
        /// </summary>
        /// <param name="subaccountname"></param>
        /// <returns></returns>
        private DataTable GetOrders(string subaccountname, DateTime startDate, DateTime endDate)
        {
            DataTable dtOrders = new DataTable();
            string tSQL = "DECLARE @subaccountname varchar(100) = '" + subaccountname + "'";
            tSQL += " SELECT * FROM orders ord where @subaccountname IN (SELECT value FROM string_split(ord.subaccounts,','))";
            tSQL += " AND order_submit_time BETWEEN '" + startDate.ToString("dd-MMM-yyyy") + "' ";
            tSQL += " AND '" + endDate.ToString("dd-MMM-yyyy") + " 23:59:59'";
            dtOrders =  _db.execSQL(tSQL, gbl_conn);
            return dtOrders;
        }

        private void rbViewOrdersArchive_Click(object sender, RoutedEventArgs e)
        {

        }

        private void cmdProcess_Click(object sender, RoutedEventArgs e)
        {

        }

        private void cmdClose_Click(object sender, RoutedEventArgs e)
        {
            RemoveControlRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
