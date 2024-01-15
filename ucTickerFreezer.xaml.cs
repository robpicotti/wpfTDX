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
using System.Collections;

namespace wpfTDX
{
    /// <summary>
    /// Interaction logic for ucTickerFreezer.xaml
    /// </summary>
    public partial class ucTickerFreezer : UserControl
    {
        public event EventHandler RemoveControlRequested;
        public SqlConnection gbl_conn;
        TDX.db _db = new TDX.db();
        DataTable dtTickerFreezer;
        string FUND_NAME = "";
        string SUBACCOUNT_NAME = "";
        string TAD_ID = "";
        string TICKERNAME = "";
        string EXEC_ACCOUNT_NAME = "";
        string BENCHMARK_NAME = "";
        public ucTickerFreezer(SqlConnection conn,string default_fund)
        {
            InitializeComponent();
            gbl_conn = conn;
            Refresh();
        }
        public void Refresh()
        {
            Cursor = Cursors.Wait;
            try
            {
                dtTickerFreezer = _db.ticker_freezer("", gbl_conn);
                dgTickerFreezer.ItemsSource = dtTickerFreezer.DefaultView;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Ticker Freezer", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }


        private void cmdClose_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            RemoveControlRequested?.Invoke(this, EventArgs.Empty);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            winThawFreezer thaw = new winThawFreezer(
                FUND_NAME, EXEC_ACCOUNT_NAME, SUBACCOUNT_NAME, TAD_ID,
                TICKERNAME, BENCHMARK_NAME, gbl_conn);
            thaw.ShowDialog();
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {

        }

        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {

        }

        private void MenuItem_Click_4(object sender, RoutedEventArgs e)
        {

        }

        private void dgTickerFreezer_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            DataGridCell cell = GetClickedCell(e);

            if (cell != null)
            {
                // Get the corresponding DataGridRow
                DataGridRow row = (DataGridRow)dgTickerFreezer.ItemContainerGenerator.ContainerFromItem(cell.DataContext);

                if (row != null)
                {
                    // Get the data item associated with the row
                    DataRowView rowDataView = (DataRowView)row.Item;

                    // Now you have access to all column values for the clicked row
                    IList columns = dgTickerFreezer.Columns;

                    foreach (DataGridColumn column in columns)
                    {
                        // Access column values using reflection
                        object fundName = rowDataView["fundname"];
                        FUND_NAME = fundName.ToString();
                        object subAccount = rowDataView["subaccountname"];
                        SUBACCOUNT_NAME = subAccount.ToString();
                        object tickerName = rowDataView["tickername"];
                        TICKERNAME = tickerName.ToString();
                        object tadid = rowDataView["tad_id"];
                        TAD_ID = tadid.ToString();
                        object execaccountname = rowDataView["execaccountname"];
                        EXEC_ACCOUNT_NAME = execaccountname.ToString();
                        object benchmarkname = rowDataView["benchmarkname"];
                        BENCHMARK_NAME = benchmarkname.ToString();
                    }
                }
            }
        }
        private DataGridCell GetClickedCell(MouseButtonEventArgs e)
        {
            // Find the visual element that was clicked
            DependencyObject dep = (DependencyObject)e.OriginalSource;

            // Traverse the visual tree to find the DataGridCell
            while (dep != null && !(dep is DataGridCell))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            return dep as DataGridCell;
        }
    }
}
