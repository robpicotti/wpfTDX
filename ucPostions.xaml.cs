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
using System.Collections;

namespace wpfTDX
{
    /// <summary>
    /// Interaction logic for ucPostions.xaml
    /// </summary>
    public partial class ucPostions : UserControl
    {
        public DataTable dtFunds;
        public SqlConnection gbl_conn;
        TDX.db _db = new TDX.db();
        public string DEFAULT_FUND = "";
        public event EventHandler RemoveControlRequested;
        List<string> lstNumericColumns = new List<string>();
        string SUBACCOUNT = "";
        string TICKERNAME = "";
        string TAD_ID = "";
        Position POSN;
        TickerFreezer TickerFreezer;
        public ucPostions(SqlConnection conn, string default_fund)
        {
            InitializeComponent();
            gbl_conn = conn;
            DEFAULT_FUND = default_fund;
            LoadForm();
            SetControlValues();
        }
        private void LoadForm()
        {
            //subscribe to the formatting event
            dgCashPosition.AutoGeneratingColumn += AutoGeneratingColumn;
            dtFunds = _db.get_funds(gbl_conn);
            cboFundname.Items.Clear();
            foreach (DataRow row in dtFunds.Rows)
            {
                cboFundname.Items.Add(row["fundname"].ToString());
            }
            cboFundname.SelectedItem = DEFAULT_FUND;
            BuildNumericColumnLists();
            TickerFreezer = new TickerFreezer(gbl_conn);
        }
        private void BuildNumericColumnLists()
        {
            lstNumericColumns.Add("position_live");
        }
        private void SetControlValues()
        {
            dtPickerPostionFrom.SelectedDate = DateTime.Today;
        }
        private void cmdClose_Click(object sender, RoutedEventArgs e)
        {
            RemoveControlRequested?.Invoke(this, EventArgs.Empty);
        }

        private void cmdRun_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                string fundname = cboFundname.SelectedItem.ToString();
                if (fundname != null || fundname != "")
                {
                    POSN = new Position(fundname, gbl_conn);
                    POSN.position_date = dtPickerPostionFrom.SelectedDate.Value;
                    POSN.positions();
                    if (POSN.position != null)
                    {
                        dgPosition.ItemsSource = POSN.position.DefaultView;
                        dgCashPosition.ItemsSource = POSN.cash_position.DefaultView;
                        MessageBox.Show("positions complete", "positions", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("POSN.position is null. Check get_position sproc",
                            "Position", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Positions error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { Mouse.OverrideCursor = null; }
        }
        private void AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
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

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            winScale scalePositions = new winScale(POSN,cboFundname.Text,SUBACCOUNT,TICKERNAME);

            // Show the edit window as a dialog (blocks user interaction with the parent window)
            bool? result = scalePositions.ShowDialog();
        }
        /// <summary>
        /// freeze fund
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            string question = "Are you sure you want to freeze the fund: " + cboFundname.Text;
            MessageBoxResult result = MessageBox.Show(question, "fund freeze", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if(result == MessageBoxResult.Yes)
            {
                TickerFreezer.Freeze(
                    runtime: DateTime.UtcNow.ToString("dd-MMM-yyyy HH:mm:ss"), error_code: 2001, error_string: "Manual freeze on fund",
                    freeze_expiration: null, _override: 0, override_expiration: null, notes: "", runtime_resolved: null, fundname: cboFundname.Text);
                MessageBox.Show("Manual freeze completed");
            }
        }

        private void dgPosition_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            DataGridCell cell = GetClickedCell(e);

            if (cell != null)
            {
                // Get the corresponding DataGridRow
                DataGridRow row = (DataGridRow)dgPosition.ItemContainerGenerator.ContainerFromItem(cell.DataContext);

                if (row != null)
                {
                    // Get the data item associated with the row
                    DataRowView rowDataView = (DataRowView)row.Item;

                    // Now you have access to all column values for the clicked row
                    IList columns = dgPosition.Columns;

                    foreach (DataGridColumn column in columns)
                    {
                        // Access column values using reflection
                        object subAccount = rowDataView["subaccount"];
                        SUBACCOUNT = subAccount.ToString();
                        object tickerName = rowDataView["tickername"];
                        TICKERNAME = tickerName.ToString();
                        object tadid = rowDataView["tad_id"];
                        TAD_ID = tadid.ToString();
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
        /// <summary>
        /// freeze tad_id
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            string question = "Are you sure you want to freeze the tad_id: " + TAD_ID;
            MessageBoxResult result = MessageBox.Show(question, "tad_id freeze",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                TickerFreezer.Freeze(
                        runtime: DateTime.UtcNow.ToString("dd-MMM-yyyy HH:mm:ss"), error_code: 2002, error_string: "Manual freeze on tad_id",
                        freeze_expiration: null, _override: 0, override_expiration: null, notes: "", runtime_resolved: null, tad_id: TAD_ID);
                MessageBox.Show("Manual freeze completed","Ticker freezer");
            }
        }

        /// <summary>
        /// freeze fund-tad_id
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            string question = "Are you sure you want to freeze the fund tad_id?: fund = " 
                + cboFundname.Text + " tad_id =  " + TAD_ID;
            MessageBoxResult result = MessageBox.Show(question, "tad_id freeze",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                TickerFreezer.Freeze(
                        runtime: DateTime.UtcNow.ToString("dd-MMM-yyyy HH:mm:ss"),
                        error_code: 2003, error_string: "Manual freeze on tad_id-fund",
                        freeze_expiration: null, _override: 0, override_expiration: null,
                        notes: "", runtime_resolved: null, tad_id: TAD_ID,
                        fundname: cboFundname.Text);
                MessageBox.Show("Manual freeze completed", "Ticker freezer");
            }
        }
        /// <summary>
        /// freeze tickername
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click_4(object sender, RoutedEventArgs e)
        {
            string question = "Are you sure you want to freeze tickername: "
                   + TICKERNAME;
            MessageBoxResult result = MessageBox.Show(question, "tickername freeze",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                TickerFreezer.Freeze(
                runtime: DateTime.UtcNow.ToString("dd-MMM-yyyy HH:mm:ss"), error_code: 2004, error_string: "Manual freeze on tickername",
                freeze_expiration: null, _override: 0, override_expiration: null, notes: "", runtime_resolved: null, tickername: TICKERNAME);
                MessageBox.Show("Manual freeze completed");
            }
        }
        /// <summary>
        /// freeze fund- tickername 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click_5(object sender, RoutedEventArgs e)
        {
            string question = "Are you sure you want to freeze fund-tickername: fund =  " 
                + cboFundname.Text + " tickername = " 
                   + TICKERNAME;
            MessageBoxResult result = MessageBox.Show(question, "tickername freeze",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                TickerFreezer.Freeze(
                        runtime: DateTime.UtcNow.ToString("dd-MMM-yyyy HH:mm:ss"), 
                        error_code: 2005, error_string: "Manual freeze on tickername-fund",
                        freeze_expiration: null, _override: 0, override_expiration: null, notes: "",
                        runtime_resolved: null, tickername: TICKERNAME, fundname: cboFundname.Text);
                MessageBox.Show("Manual freeze completed");
            }
        }
    }
}

