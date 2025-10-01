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
        public PositionViewModel ViewModel { get; set; }
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
            this.ViewModel = new PositionViewModel();
            this.DataContext = ViewModel;
            this.ViewModel?.GetFunds();
            this.ViewModel.Conn = gbl_conn;
            //subscribe to the formatting event
            dgCashPosition.AutoGeneratingColumn += AutoGeneratingColumn;

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
            winScale scalePositions = new winScale(this.ViewModel.TdxPosition,this.ViewModel.SelectedFund.FundName,SUBACCOUNT,TICKERNAME,"out", null,null,this.gbl_conn);

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
            if (this.ViewModel?.SelectedPosition != null)
            {
                SUBACCOUNT = this.ViewModel?.SelectedPosition.SubaccountName;
                TICKERNAME = this.ViewModel?.SelectedPosition.TickerName;
                TAD_ID = this.ViewModel?.SelectedPosition.TadId;
            }
            else
            {
                string mesg = "Please select a row before right clicking";
                MessageBox.Show(mesg, "selected position error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void MenuItem_Click_6(object sender, RoutedEventArgs e)
        {
            winTradeHistory winTH = new winTradeHistory(cboFundname.Text,TAD_ID,TICKERNAME, dtPickerPostionFrom.SelectedDate.Value,this.gbl_conn);
            winTH.ShowDialog();
        }

        private void dtPickerPostionFrom_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dtPickerPostionFrom.SelectedDate == null)
            {
                ViewModel.Date_t = DateTime.Today;
            }
            else
            {
                ViewModel.Date_t = (DateTime)dtPickerPostionFrom.SelectedDate;
            }
        }

        private async void MenuItem_Click_7(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime today = DateTime.UtcNow;
                if ((ViewModel.SelectedPosition.BbgLastTradeDate > today.Date))
                {
                    MessageBox.Show("This position has not expired yet. You cannot use this function to closeout the position",
                        "closeout expired", MessageBoxButton.OK, MessageBoxImage.Error);

                }
                else
                {
                    if (ViewModel.Date_t != today.Date)
                    {
                        string messg = "You can only closeout an expired contract when the position date is LIVE. Please select todays date";
                        MessageBox.Show(messg, "closeout expired positions error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        string message = "Are you sure you want to proceed closing out this tad_id " + ViewModel.SelectedPosition.TadId + "?";
                        MessageBoxResult result = MessageBox.Show(
                                message,
                                "Confirmation",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            double posnlive = ViewModel.SelectedPosition.PositionLive;
                            string action = "";
                            if (posnlive > 0)
                            {
                                action = "SELL";
                            }
                            else if (posnlive < 0)
                            {
                                action = "BUY";
                            }
                            bool isSuccess = await ViewModel.CloseoutExpiredAsync("TAD", "2", "12", ViewModel.SelectedPosition.FundName,
                                ViewModel.SelectedPosition.SubaccountName, action, ViewModel.SelectedPosition.TadId, ViewModel.SelectedPosition.TickerName,
                                ViewModel.SelectedPosition.PriceLive, Math.Abs(posnlive),
                                ViewModel.SelectedPosition.Multiplier, ViewModel.SelectedPosition.ExchangeCurrency, ViewModel.SelectedPosition.BbgLastTradeDate,
                                ViewModel.SelectedPosition.RollDate,ViewModel.SelectedPosition.Instrument,ViewModel.SelectedPosition.ReportCategory,
                                ViewModel.SelectedPosition.Category,ViewModel.SelectedPosition.ContractIncrement,ViewModel.SelectedPosition.BbgSymbolExp,
                                ViewModel.SelectedPosition.SubTickerName);
                            if (isSuccess)
                            {
                                string msg = "Closeout Expired successfully completed!";
                                msg += "VERY IMPORTANT: broker_id = 2 has been used for this transaction, which is TAD.";
                                msg += "Find this transaction in the transactions table and change the broker_id to the relevant broker_id for this subaccount";
                                MessageBox.Show("Closeout Expired successfully completed!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                                ViewModel.PositionsData.Remove(ViewModel.SelectedPosition);
                                //ViewModel.GetPosition();
                            }
                            else
                            {
                                MessageBox.Show("Closeout Expired failed!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "closeout expired position error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuItem_Click_8(object sender, RoutedEventArgs e)
        {
            new winScaledPositions().Show();
        }
    }
}

