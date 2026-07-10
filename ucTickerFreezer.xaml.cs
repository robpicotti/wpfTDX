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
using System.Configuration;
using System.Windows.Threading;

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
        string BROKER_CODE_EXEC = "";
        string EMS_NAME = "";
        string ERROR_CODE = "";
        string RUN_TIME = "";
        string EXPIRATION_DATETIME;
        TickerFreezer TFR;
        TickerFreezerViewModel viewmodel { get; set; }

        // Auto-refresh timer (its own dedicated timer; the process monitor has none).
        private DispatcherTimer _autoRefreshTimer;
        private bool _isAutoRefreshing;

        /// <summary>Raised after each refresh (initial load, auto-refresh tick, manual)
        /// so a host (e.g. MainWindow's announcements banner) can update in lockstep.</summary>
        public event EventHandler Refreshed;
        private void RaiseRefreshed()
        {
            try { Refreshed?.Invoke(this, EventArgs.Empty); } catch { }
        }
        public ucTickerFreezer(SqlConnection conn,string default_fund)
        {
            InitializeComponent();
            gbl_conn = conn;
            this.viewmodel = new TickerFreezerViewModel();
            this.DataContext = this.viewmodel;
            try
            {
                TFR = new TickerFreezer(gbl_conn);
                Refresh();
                this.Loaded += OnLoaded;
                this.viewmodel.OpenFreezeTadIdDialog += Viewmodel_OpenFreezeTadIdDialog;
                this.viewmodel.ShowMessage += ViewModel_ShowMessage;
                this.viewmodel.OpenThawFreezerDialog += ViewModel_OpenThawFreezerDialog;
                this.viewmodel.OpenErrorInfoDialog += ViewModel_OpenErrorInfoDialog;
                this.Unloaded += OnUnloaded;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Ticker Freezer", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Viewmodel_OpenFreezeTadIdDialog(object sender, EventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;

                // Open the dialog window
                winFreezeTadId winFreeze = new winFreezeTadId(gbl_conn,this);
                winFreeze.ShowDialog();
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }
        private void ViewModel_ShowMessage(object sender, MessageEventArgs e)
        {
            MessageBox.Show(e.Message, e.Title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ViewModel_OpenErrorInfoDialog(object sender, ErrorInfoEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                winErrorInfo info = new winErrorInfo(e.Ticker, e.WebServiceData);
                info.Owner = Window.GetWindow(this);
                info.ShowDialog();
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private void ViewModel_OpenThawFreezerDialog(object sender, ThawFreezerEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;

                // Pass the required parameters to the dialog
                winThawFreezer thaw = new winThawFreezer(
                    e.RunTime,
                    e.FundGroupName,
                    e.FundName,
                    e.ExecAccountName,
                    e.SubAccountName,
                    e.TadId,
                    e.TickerName,
                    e.BenchmarkName,
                    e.BrokerCodeExec,
                    e.EmsName,
                    e.ErrorCode,
                    this, // Pass the parent window
                    gbl_conn // Pass the connection
                );

                thaw.ShowDialog();
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                TFR = new TickerFreezer(gbl_conn);
                await this.viewmodel.Refresh();
                await this.viewmodel.GetFundsData();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ticker Freezer", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Unsubscribe to prevent the method from being called multiple times
                this.Loaded -= OnLoaded;

                // Begin the background auto-refresh once the initial load is done.
                StartAutoRefresh();
                RaiseRefreshed();
            }
        }
        public void Refresh()
        {
             this.viewmodel?.Refresh();
             RaiseRefreshed();
        }

        /// <summary>
        /// Starts a dedicated timer that periodically refreshes the Ticker Freezer
        /// in the background. Interval comes from the "TickerFreezerRefreshSeconds"
        /// app setting (defaults to 60s; 0 disables it).
        /// </summary>
        private void StartAutoRefresh()
        {
            if (_autoRefreshTimer != null) return; // already running

            int seconds = 60;
            string cfg = ConfigurationManager.AppSettings["TickerFreezerRefreshSeconds"];
            if (!string.IsNullOrWhiteSpace(cfg) && int.TryParse(cfg, out int parsed))
                seconds = parsed;

            if (seconds <= 0) return; // auto-refresh disabled via config

            _autoRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(seconds)
            };
            _autoRefreshTimer.Tick += AutoRefreshTimer_Tick;
            _autoRefreshTimer.Start();
        }

        /// <summary>
        /// Fires on the UI DispatcherTimer, but the work in viewmodel.Refresh()
        /// awaits async HTTP I/O — the network wait runs off the UI thread so the
        /// screen (and everything else) stays responsive. The bound collection is
        /// updated on the UI thread, as WPF requires. A re-entrancy guard skips a
        /// tick while a previous refresh is still running, and errors are swallowed
        /// so a transient network blip doesn't pop a dialog every tick (the manual
        /// Refresh button still surfaces errors).
        /// </summary>
        private async void AutoRefreshTimer_Tick(object sender, EventArgs e)
        {
            if (_isAutoRefreshing || this.viewmodel == null) return;

            _isAutoRefreshing = true;
            try
            {
                await this.viewmodel.Refresh();
            }
            catch
            {
                // ignore transient auto-refresh errors
            }
            finally
            {
                _isAutoRefreshing = false;
                RaiseRefreshed();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_autoRefreshTimer != null)
            {
                _autoRefreshTimer.Stop();
                _autoRefreshTimer.Tick -= AutoRefreshTimer_Tick;
                _autoRefreshTimer = null;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            try
            {
                Refresh();
                MessageBox.Show("Ticker Freezer data refreshed", "Ticker Freezer", MessageBoxButton.OK, MessageBoxImage.Information);
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

        //private void MenuItem_Click(object sender, RoutedEventArgs e)
        //{
        //    winThawFreezer thaw = new winThawFreezer(
        //        RUN_TIME,
        //        FUND_NAME, EXEC_ACCOUNT_NAME, SUBACCOUNT_NAME, TAD_ID,
        //        TICKERNAME, BENCHMARK_NAME,BROKER_CODE_EXEC,EMS_NAME,
        //        ERROR_CODE ,this,gbl_conn);
        //    thaw.ShowDialog();
        //}

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {

        }

        //private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        //{
        //    OverrideTadId("After the close");
        //}

        //private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        //{
        //    OverrideTadId("Never");
        //}

        //private void MenuItem_Click_4(object sender, RoutedEventArgs e)
        //{
        //    RemoveOverride();
        //}

        private void dgTickerFreezer_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var element = e.OriginalSource as DependencyObject;
            while (element != null && !(element is DataGridRow))
            {
                element = VisualTreeHelper.GetParent(element);
            }

            if (element is DataGridRow row)
            {
                dgTickerFreezer.SelectedItem = row.Item;
            }
            //DataGridCell cell = GetClickedCell(e);

            //if (cell != null)
            //{
            //    // Get the corresponding DataGridRow
            //    DataGridRow row = (DataGridRow)dgTickerFreezer.ItemContainerGenerator.ContainerFromItem(cell.DataContext);

            //    if (row != null)
            //    {
            //        // Get the data item associated with the row
            //        DataRowView rowDataView = (DataRowView)row.Item;

            //        // Now you have access to all column values for the clicked row
            //        IList columns = dgTickerFreezer.Columns;

            //        foreach (DataGridColumn column in columns)
            //        {
            //            // Access column values using reflection
            //            object runTime = rowDataView["runtime"];
            //            DateTime dtRun = (DateTime)runTime;
            //            string strRunTime = dtRun.ToString("yyyy-MM-dd HH:mm:ss.fff");
            //            RUN_TIME = strRunTime;
            //            object fundName = rowDataView["fundname"];
            //            FUND_NAME = fundName.ToString();
            //            object subAccount = rowDataView["subaccountname"];
            //            SUBACCOUNT_NAME = subAccount.ToString();
            //            object tickerName = rowDataView["tickername"];
            //            TICKERNAME = tickerName.ToString();
            //            object tadid = rowDataView["tad_id"];
            //            TAD_ID = tadid.ToString();
            //            object execaccountname = rowDataView["execaccountname"];
            //            EXEC_ACCOUNT_NAME = execaccountname.ToString();
            //            object benchmarkname = rowDataView["benchmarkname"];
            //            BENCHMARK_NAME = benchmarkname.ToString();
            //            object brokercode_exec = rowDataView["broker_code_exec"];
            //            BROKER_CODE_EXEC = brokercode_exec.ToString();
            //            object emsname = rowDataView["emsname"];
            //            EMS_NAME = emsname.ToString();
            //            object error_code = rowDataView["error_code"];
            //            ERROR_CODE = error_code.ToString();
            //        }
            //    }
            //}
        }
        
        //private DataGridCell GetClickedCell(MouseButtonEventArgs e)
        //{
        //    // Find the visual element that was clicked
        //    DependencyObject dep = (DependencyObject)e.OriginalSource;

        //    // Traverse the visual tree to find the DataGridCell
        //    while (dep != null && !(dep is DataGridCell))
        //    {
        //        dep = VisualTreeHelper.GetParent(dep);
        //    }

        //    return dep as DataGridCell;
        //}

        //private void OverrideTadId(string expiration)
        //{
        //    try
        //    {
        //        TFR.Override_freezer(RUN_TIME, EMS_NAME, BROKER_CODE_EXEC, 
        //            FUND_NAME, EXEC_ACCOUNT_NAME, SUBACCOUNT_NAME, TAD_ID, 
        //            ERROR_CODE, true, expiration, gbl_conn);
        //        MessageBox.Show("tad_id: " + TAD_ID + " has been overridden.", 
        //            "ticker_freezer override", MessageBoxButton.OK, 
        //            MessageBoxImage.Information);
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message);
        //    }
        //    finally
        //    {
        //        Refresh();
        //    }
        //}
        
        //private void RemoveOverride()
        //{
        //    try
        //    {
        //        TFR.Override_freezer(RUN_TIME, EMS_NAME, BROKER_CODE_EXEC, FUND_NAME,
        //            EXEC_ACCOUNT_NAME, SUBACCOUNT_NAME, TAD_ID, ERROR_CODE, false, 
        //            EXPIRATION_DATETIME, gbl_conn);
        //        MessageBox.Show("tad_id: " + TAD_ID + " has been removed.", "remove ticker_freezer override", 
        //            MessageBoxButton.OK, MessageBoxImage.Information);
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message);
        //    }
        //    finally
        //    {
        //        Refresh();
        //    }
        //}

        private void cmdClose_Click(object sender, RoutedEventArgs e)
        {
            RemoveControlRequested?.Invoke(this, EventArgs.Empty);
        }

        private void MenuItem_Click_5(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                winFreezeTadId winFreeze = new winFreezeTadId(gbl_conn, this);
                winFreeze.ShowDialog();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "freeze any tad_id", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private void dgFunds_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            // Check if the property being generated is "Freeze"
            if (e.PropertyName == "Freeze")
            {
                // Cancel the auto-generated column for "Freeze"
                e.Cancel = true;

                // Create a custom DataGridTemplateColumn
                var templateColumn = new DataGridTemplateColumn
                {
                    Header = "Freeze"
                };

                // Set the CellTemplate for the CheckBox
                templateColumn.CellTemplate = (DataTemplate)FindResource("FreezeTemplate");

                // Add the custom column to the DataGrid
                ((DataGrid)sender).Columns.Add(templateColumn);
            }
        }



 
    }
}
