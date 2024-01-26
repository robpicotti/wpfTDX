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
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using TDX;


namespace wpfTDX
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string filepath = @"\\ad01-har.10dynamics.com\share\Files\TDX\tdx.txt";
        public string sqlServer = "";
        public string sqlInstance = "";
        public string DEFAULT_INSTANCE = "SQLEXPRESS";
        public string sqlServerInstance = "";
        public string sqlDb = "";
        public string sqlUser = "";
        public string sqlPwd = "";
        public string svrTag = "";
        public string fixed_pricing;
        public string default_fund = "";
        public string HEARTBEAT_MINUTES;
        public string RUN_RAWDATA_MINUTES;
        public string RUN_TICKER_UPDATE_MINUTES;
        public string RUN_BENCH_MINUTES;
        public string RTL_MINUTES;
        public string HEARTBEAT_STRING;
        public string RUN_RAWDATA_STRING;
        public string RUN_TICKER_UPDATE_STRING;
        public string RUN_BENCH_STRING;
        public string RTL_STRING;
        public string TIMER_MILLISECONDS;
        public string SERVERS;
        public string INSTANCES;
        public List<string> lstSERVERS;
        public List<string> lstDATABASES;
        public List<string> lstINSTANCES;
        public SqlConnection sql_conn;
        public SqlConnection sql_conn_monitor;
        //private readonly ImageList statusImageList;
        private bool stopMonitoring;
        //DataTable dtHeartBeatsMonitored;
        List<string> lstProcesses = new List<string>();
        private readonly object lockObject = new object();
        Thread monitoringThread;
        public string VERSION = "TDX version 1.8.5";
        
        private Dictionary<TextBlock, UserControl> userControlDictionary = new Dictionary<TextBlock, UserControl>();
        /// <summary>
        /// this is the number of columns for tiling effect
        /// </summary>
        private const int NumberOfColumns = 3;
        double Window_Width;
        public MainWindow()
        {
            InitializeComponent();
            LoadForm();
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                // Handle unobserved exceptions here
                Exception exception = (Exception)args.ExceptionObject;
                MessageBox.Show(exception.Message, "Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                // Handle unobserved task exceptions here
                Exception exception = args.Exception;
                MessageBox.Show(exception.Message, "Unobserved Task Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            }; 
        }

        private async void cboServer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                await HandleCboServernameSelectedIndexChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Server name change error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        public void LoadForm()
        {
            WindowState = WindowState.Maximized;
            Window_Width = this.Width;
            Dictionary<string, string> dict_sql = getConnectionParams();
            sqlPwd = dict_sql["@pwd"];
            fixed_pricing = dict_sql["@fixed_pricing"];
            default_fund = dict_sql["@default_fund"];
            string[] servernames = dict_sql["@server_names"].Split(',');
            string[] dbnames = "Oris,Oris_dev,Oris_stage".Split(',');
            string[] instance_names = dict_sql["@instance_names"].Split(',');
            //load the server and database list
            lstSERVERS = new List<string>(servernames);
            lstDATABASES = new List<string>(dbnames);
            lstINSTANCES = new List<string>(instance_names);
            for (int i = 0; i < lstSERVERS.Count; i++)
            {
                string server = lstSERVERS[i].ToString();
                string fullservername = "";
                if (server == "(local)")
                {
                    fullservername = server;//+ @"\sqlexpress";
                }
                else
                {
                    if (server.ToLower() != Environment.MachineName.ToLower())
                    {
                        server = server + ".10dynamics.com";
                        fullservername = server;// + @"\sqlexpress";
                    }
                }

                if (fullservername != "")
                {
                    cboServer.Items.Add(fullservername);
                }
            }
            //load the database list
            for (int i = 0; i < lstSERVERS.Count; i++)
            {
                string dbname = lstDATABASES[i].ToString();
                cboDatabase.Items.Add(dbname);
            }
            //load instance names
            for (int i = 0; i < lstINSTANCES.Count; i++)
            {
                string instance = lstINSTANCES[i].ToString();
                cboInstance.Items.Add(instance);
            }
            cboInstance.SelectedIndex = 0;
        }
        
        public Dictionary<string, string> getConnectionParams()
            {
                Dictionary<string, string> dictConn = new Dictionary<string, string>();
                if (File.Exists(filepath))
                {
                    StreamReader SR = new StreamReader(filepath);
                    string strFileText = SR.ReadToEnd();
                    SR.Close();
                    SR.Dispose();
                    string rgxServer = "<SERVER:(.*?)>";
                    Regex rgSrv = new Regex(rgxServer, RegexOptions.Compiled);
                    Match mServer = rgSrv.Match(strFileText);
                    if (mServer.Groups.Count > 0)
                    {
                        sqlServer = mServer.Groups[1].Value.ToString();
                        dictConn.Add("@server", sqlServer);
                    }
                    string rgxDB = "<DB:(.*?)>";
                    Regex rgDB = new Regex(rgxDB, RegexOptions.Compiled);
                    Match mDB = rgDB.Match(strFileText);
                    if (mDB.Groups.Count > 0)
                    {
                        sqlDb = mDB.Groups[1].Value.ToString();
                        dictConn.Add("@database", sqlDb);
                    }
                    string rgxUname = "<UNAME:(.*?)>";
                    Regex rgUname = new Regex(rgxUname, RegexOptions.Compiled);
                    Match mUname = rgUname.Match(strFileText);
                    if (mUname.Groups.Count > 0)
                    {
                        sqlUser = mUname.Groups[1].Value.ToString();
                        dictConn.Add("@user", sqlUser);
                    }
                    string rgxPwd = "<PWD:(.*?)>";
                    Regex rgPwd = new Regex(rgxPwd, RegexOptions.Compiled);
                    Match mPwd = rgPwd.Match(strFileText);
                    if (mPwd.Groups.Count > 0)
                    {
                        sqlPwd = mPwd.Groups[1].Value.ToString();
                        dictConn.Add("@pwd", sqlPwd);
                    }
                    string rgxTag = "<TAG:(.*?)>";
                    Regex rgTag = new Regex(rgxTag, RegexOptions.Compiled);
                    Match mTag = rgTag.Match(strFileText);
                    if (mTag.Groups.Count > 0)
                    {
                        svrTag = mTag.Groups[1].Value.ToString();
                        dictConn.Add("@tag", svrTag);
                    }
                    string rgxFixed_pricing = "<FIXED_PRICING:(.*?)>";
                    Regex rgFP = new Regex(rgxFixed_pricing, RegexOptions.Compiled);
                    Match mFP = rgFP.Match(strFileText);
                    if (mFP.Groups.Count > 0)
                    {
                        fixed_pricing = mFP.Groups[1].Value.ToString();
                        dictConn.Add("@fixed_pricing", fixed_pricing);
                    }
                    string rgxdefault_account = "<DEFAULT_FUND:(.*?)>";
                    Regex rgDA = new Regex(rgxdefault_account, RegexOptions.Compiled);
                    Match mDA = rgDA.Match(strFileText);
                    if (mDA.Groups.Count > 0)
                    {
                        default_fund = mDA.Groups[1].Value.ToString();
                        dictConn.Add("@default_fund", default_fund);
                    }
                    string rgxheartbeat = "<HEARTBEAT_MINUTES:(.*?)>";
                    Regex rgHB = new Regex(rgxheartbeat, RegexOptions.Compiled);
                    Match mHB = rgHB.Match(strFileText);
                    if (mHB.Groups.Count > 0)
                    {
                        HEARTBEAT_MINUTES = mHB.Groups[1].Value.ToString();
                        dictConn.Add("@heartbeat_minutes", HEARTBEAT_MINUTES);
                    }
                    string rgxheartbeattext = "<HEARTBEAT_STRING:(.*?)>";
                    Regex rgHBtext = new Regex(rgxheartbeattext, RegexOptions.Compiled);
                    Match mHBtext = rgHBtext.Match(strFileText);
                    if (mHBtext.Groups.Count > 0)
                    {
                        HEARTBEAT_STRING = mHBtext.Groups[1].Value.ToString();
                    }

                    string rgxrunupdate = "<RUN_RAWDATA_MINUTES:(.*?)>";
                    Regex rgUP = new Regex(rgxrunupdate, RegexOptions.Compiled);
                    Match mUP = rgUP.Match(strFileText);
                    if (mUP.Groups.Count > 0)
                    {
                        RUN_RAWDATA_MINUTES = mUP.Groups[1].Value.ToString();
                    }

                    string rgxrunupdatetext = "<RUN_RAWDATA_STRING:(.*?)>";
                    Regex rgUPtext = new Regex(rgxrunupdatetext, RegexOptions.Compiled);
                    Match mUPtext = rgUPtext.Match(strFileText);
                    if (mUPtext.Groups.Count > 0)
                    {
                        RUN_RAWDATA_STRING = mUPtext.Groups[1].Value.ToString();
                    }
                    string rgxrtl = "<RTL_MINUTES:(.*?)>";
                    Regex rgRTL = new Regex(rgxrtl, RegexOptions.Compiled);
                    Match mRTL = rgRTL.Match(strFileText);
                    if (mRTL.Groups.Count > 0)
                    {
                        RTL_MINUTES = mRTL.Groups[1].Value.ToString();
                    }

                    string rgxruntickerupdatetext = "<RUN_TICKER_STRING:(.*?)>";
                    Regex rgTickUPtext = new Regex(rgxruntickerupdatetext, RegexOptions.Compiled);
                    Match mTickUPtext = rgTickUPtext.Match(strFileText);
                    if (mTickUPtext.Groups.Count > 0)
                    {
                        RUN_TICKER_UPDATE_STRING = mUPtext.Groups[1].Value.ToString();
                    }
                    string rgxtickerupdate = "<RUN_TICKER_MINUTES:(.*?)>";
                    Regex rgTickupdate = new Regex(rgxtickerupdate, RegexOptions.Compiled);
                    Match mTickupdate = rgTickupdate.Match(strFileText);
                    if (mTickupdate.Groups.Count > 0)
                    {
                        RUN_TICKER_UPDATE_MINUTES = mTickupdate.Groups[1].Value.ToString();
                    }


                    string rgxrtltext = "<RTL_STRING:(.*?)>";
                    Regex rgRTLtext = new Regex(rgxrtltext, RegexOptions.Compiled);
                    Match mRTLtext = rgRTLtext.Match(strFileText);
                    if (mRTLtext.Groups.Count > 0)
                    {
                        RTL_STRING = mRTLtext.Groups[1].Value.ToString();
                    }

                    string rgxbench = "<RUN_BENCH_MINUTES:(.*?)>";
                    Regex rgBench = new Regex(rgxbench, RegexOptions.Compiled);
                    Match mBench = rgBench.Match(strFileText);
                    if (mBench.Groups.Count > 0)
                    {
                        RUN_BENCH_MINUTES = mBench.Groups[1].Value.ToString();
                    }
                    string rgxbenchtext = "<RUN_BENCH_STRING:(.*?)>";
                    Regex rgBenchtext = new Regex(rgxbenchtext, RegexOptions.Compiled);
                    Match mBenchtext = rgBenchtext.Match(strFileText);
                    if (mBenchtext.Groups.Count > 0)
                    {
                        RUN_BENCH_STRING = mBenchtext.Groups[1].Value.ToString();
                    }

                    string rgxTimer = "<TIMER_MILLISECONDS:(.*?)>";
                    Regex rgTimer = new Regex(rgxTimer, RegexOptions.Compiled);
                    Match mTimer = rgTimer.Match(strFileText);
                    if (mTimer.Groups.Count > 0)
                    {
                        TIMER_MILLISECONDS = mTimer.Groups[1].Value.ToString();
                    }

                    string rgxServers = "<SERVER_NAMES:(.*?)>";
                    Regex rgServers = new Regex(rgxServers, RegexOptions.Compiled);
                    Match mServers = rgServers.Match(strFileText);
                    if (mServers.Groups.Count > 0)
                    {
                        SERVERS = mServers.Groups[1].Value.ToString();
                        dictConn.Add("@server_names", SERVERS);
                    }

                    string rgxInstances = "<INSTANCE_NAMES:(.*?)>";
                    Regex rgInstances = new Regex(rgxInstances, RegexOptions.Compiled);
                    Match mInstance = rgInstances.Match(strFileText);
                    if (mInstance.Groups.Count > 0)
                    {
                        INSTANCES = mInstance.Groups[1].Value.ToString();
                        dictConn.Add("@instance_names", INSTANCES);
                    }
                }
                return dictConn;
            }

        private async void cboDatabase_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                await HandleCboDatabaseSelectedIndexChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Database change error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void ChangeCommandButtonConnectionStatus(bool connected)
        {
            if (!connected)
            {
                txtStatus.Background = Brushes.Red;
                txtStatus.Text = "Not connected";
                //lblConnectedTo.Text = "No Sql connection";
            }
            else
            {
                txtStatus.Background = Brushes.Green;
                txtStatus.Text = "Connected";
                //lblConnectedTo.Text = "connected to: server= " + sqlServerInstance + "; database=" + sqlDb;
            }
        }
        
        private void DisconnectDatabase()
        {
            sql_conn = null;
            sql_conn_monitor = null;
            ChangeCommandButtonConnectionStatus(false);
            StopMonitoring();

        }
        
        public SqlConnection connect_database()
        {
            sqlServer = cboServer.SelectedItem.ToString();
            sqlDb = cboDatabase.SelectedItem.ToString();
            sqlInstance = cboInstance.SelectedItem.ToString();
            sqlServerInstance = sqlServer + @"\" + sqlInstance;
            StopMonitoring();
            if (monitoringThread != null)
            {
                monitoringThread.Join(1);
            }
            if ((sqlServer != "") && (sqlDb != "") & (sqlInstance != ""))
            {
                string str_conn = "Data Source=" + sqlServerInstance + ";" +
                "Initial Catalog=" + sqlDb + ";" +
                "User id=" + sqlUser + ";" +
                "Password=" + sqlPwd + ";" +
                "Persist Security Info = true;";
                sql_conn = new SqlConnection(str_conn);
                sql_conn_monitor = new SqlConnection(str_conn);
                if ((sql_conn.DataSource != "") && (sql_conn.Database != "") && (db.IsValidConnection(sql_conn)))
                {
                    ChangeCommandButtonConnectionStatus(true);
                }
                else
                {
                    ChangeCommandButtonConnectionStatus(false);
                }
            }
            return sql_conn;
        }
        
        private void processChecker()
        {
            try
            {
                if (sql_conn_monitor != null)
                {
                    if (sql_conn_monitor.State != ConnectionState.Open)
                    { sql_conn_monitor.Open(); }
                    {
                        //StartMonitoring();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Process checker", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void StartMonitoring()
        {
            try
            {
                if(sql_conn != null)
                {
                    ucProcessMonitor procMon = new ucProcessMonitor(sql_conn);
                    procMon.HorizontalAlignment = HorizontalAlignment.Stretch;
                    // Set margin to create spacing between user controls
                    procMon.Margin = new Thickness(5); // Adjust the thickness as needed

                    // Wrap the user control in a Border with a black border color and thickness
                    //Border userControlBorder = new Border
                    //{
                    //    BorderBrush = Brushes.Black,
                    //    BorderThickness = new Thickness(2),
                    //    Child = procMon
                    //};
                    //userControlBorder.HorizontalAlignment = HorizontalAlignment.Stretch;
                    // Add the bordered user control to the WrapPanel
                    //userControlsWrapPanel.Children.Add(userControlBorder);
                    //processCheckerPanel.Children.Add(userControlBorder);
                    processCheckerPanel.Children.Add(procMon);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message,"error",MessageBoxButton.OK,MessageBoxImage.Error);
            }
        }

        private void StopMonitoring()
        {
            lock (lockObject)
            {
                stopMonitoring = true;
            }
            userControlsWrapPanel.Children.Clear();
        }
        
        private async Task HandleCboDatabaseSelectedIndexChanged()
        {

            try
            {
                if (cboDatabase.SelectedItem != null)
                {
                    sqlDb = cboDatabase.SelectedItem.ToString();
                    if (!string.IsNullOrEmpty(sqlDb))
                    {
                        connect_database();
                        //start the process monitor
                        StartMonitoring();
                        //we want the Mtm screen on startup
                        //AddMtM("MTM");
                    }
                    else
                    {
                        StopMonitoring();
                        monitoringThread.Join(1);
                        ChangeCommandButtonConnectionStatus(false);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private async Task ProcessCheckerInBackground()
        {
            try
            {
                // Perform non-UI-related tasks in the background
                processChecker();

                // Update the UI-related parts using Invoke
                if (this.IsInitialized)
                {
                    await Task.Run(() =>
                    {
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            // Call UI-related methods or update controls here
                            // Example: UpdateListView(heartbeats.ListHeartBeats);
                        }));
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ProcessCheckerInBackground Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task HandleCboServernameSelectedIndexChanged()
        {
            try
            {
                sqlServer = cboServer.SelectedItem.ToString();
                sqlDb = null;
                sqlInstance = DEFAULT_INSTANCE; //set to this as default
                cboInstance.SelectedItem = DEFAULT_INSTANCE;
                sqlServerInstance = sqlServer + @"\" + sqlInstance;
                cboDatabase.Text = null;
                DisconnectDatabase();


                // Run processChecker asynchronously
                await Task.Run(() => ProcessCheckerInBackground());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Server change error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private  void cboInstance_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                HandlecboInstanceSelectedIndexChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "cboInstance error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private  void HandlecboInstanceSelectedIndexChanged()
        {
            try
            {
                sqlInstance = cboInstance.SelectedItem.ToString();
                cboDatabase.SelectedItem = "";
                cboDatabase.Text = "";
                DisconnectDatabase();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private UserControl CreateNewUserControl(string text)
        {
            double extra_width = 80;
                switch (text)
                {
                    case "MTM":
                    ucMtm mtm = new ucMtm(sql_conn, default_fund);
                    mtm.Width = Window_Width + extra_width;
                    return mtm;
                    case "Positions":
                    ucPostions position = new ucPostions(sql_conn, default);
                    position.Width = Window_Width + extra_width;
                    return position;
                    // Add other cases for different user controls if needed
                    case "EMS":
                        ucEMS ems = new ucEMS(sql_conn,default_fund);
                        ems.Width = Window_Width + 100;
                        return ems;
                case "Ticker Freezer":
                    ucTickerFreezer tickerFreeze = new ucTickerFreezer(sql_conn, default_fund);
                    tickerFreeze.Width = Window_Width + extra_width;
                    return tickerFreeze;
                case "Order Rejections":
                    ucOrderRejections ordRejections = new ucOrderRejections(sql_conn);
                    ordRejections.Width = Window_Width + extra_width;
                    return ordRejections;
                case "NAV":
                    ucNav Nav = new ucNav(sql_conn);
                    Nav.Width = Window_Width + extra_width;
                    return Nav;
                case "Slippage":
                    ucSlippage slipp = new ucSlippage(sql_conn);
                    slipp.Width = Window_Width + extra_width;
                    return slipp;
                case "Deposits":
                    ucDeposits depo = new ucDeposits(sql_conn);
                    depo.Width = 1000;
                    return depo;
                case "Payments":
                    ucPayments payment = new ucPayments(sql_conn);
                    payment.Width = 1000;
                    return payment;
                case "Comm fee adjustments":
                    ucCommFeeAdj cfa = new ucCommFeeAdj(sql_conn);
                    cfa.Width = 1000;
                    return cfa;
                case "Corporate actions":
                    ucCorpActions corp = new ucCorpActions(sql_conn);
                    corp.Width = 1875;
                    return corp;
                default:
                        return null;
                }
        }
        /// <summary>
        /// positions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBlock_MouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
        {
            if (sql_conn != null)
            {
                if (sender is TextBlock textBlock)
                {
                    // Create a new instance of the user control
                    ucPostions newUserControl = CreateNewUserControl(textBlock.Text) as ucPostions;

                    if (newUserControl != null)
                    {
                        newUserControl.RemoveControlRequested += YourUserControl_RemoveControlRequested;  // Subscribe to the event here

                        // Set margin to create spacing between user controls
                        newUserControl.Margin = new Thickness(5); // Adjust the thickness as needed

                        // Wrap the user control in a Border with a black border color and thickness
                        Border userControlBorder = new Border
                        {
                            BorderBrush = Brushes.Black,
                            BorderThickness = new Thickness(2),
                            Child = newUserControl
                        };

                        // Add the bordered user control to the WrapPanel
                        userControlsWrapPanel.Children.Add(userControlBorder);
                    }
                }
            }
            else
            {
                MessageBox.Show("No database connection was set up", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void AddMtM(string textblockText)
        {
            // Create a new instance of the user control
            ucMtm newUserControl = CreateNewUserControl(textblockText) as ucMtm;

            if (newUserControl != null)
            {
                newUserControl.RemoveControlRequested += YourUserControl_RemoveControlRequested;  // Subscribe to the event here

                // Set margin to create spacing between user controls
                newUserControl.Margin = new Thickness(5); // Adjust the thickness as needed

                // Wrap the user control in a Border with a black border color and thickness
                Border userControlBorder = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(2),
                    Child = newUserControl
                };

                // Add the bordered user control to the WrapPanel
                userControlsWrapPanel.Children.Add(userControlBorder);
            }
        }
        /// <summary>
        /// MtM
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sql_conn != null)
            {
                if (sender is TextBlock textBlock)
                {
                    AddMtM(textBlock.Text);
                    // Create a new instance of the user control
                    //ucMtm newUserControl = CreateNewUserControl(textBlock.Text) as ucMtm;

                    //if (newUserControl != null)
                    //{
                    //    newUserControl.RemoveControlRequested += YourUserControl_RemoveControlRequested;  // Subscribe to the event here

                    //    // Set margin to create spacing between user controls
                    //    newUserControl.Margin = new Thickness(5); // Adjust the thickness as needed

                    //    // Wrap the user control in a Border with a black border color and thickness
                    //    Border userControlBorder = new Border
                    //    {
                    //        BorderBrush = Brushes.Black,
                    //        BorderThickness = new Thickness(2),
                    //        Child = newUserControl
                    //    };

                    //    // Add the bordered user control to the WrapPanel
                    //    userControlsWrapPanel.Children.Add(userControlBorder);
                    //}
                }
            }
            else
            {
                MessageBox.Show("No database connection was set up", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void YourUserControl_RemoveControlRequested(object sender, EventArgs e)
        {
            if (sender is ucMtm mtm)
            {
                // Find the Border that wraps the specified ucMtm in the WrapPanel
                Border userControlBorder = userControlsWrapPanel.Children.OfType<Border>()
                                                       .FirstOrDefault(border => border.Child == mtm);

                // Remove the Border from the WrapPanel
                if (userControlBorder != null)
                {
                    userControlsWrapPanel.Children.Remove(userControlBorder);
                }
            }
            else if (sender is ucPostions position)
            {
                // Find the Border that wraps the specified ucPostions in the WrapPanel
                Border userControlBorder = userControlsWrapPanel.Children.OfType<Border>()
                                                       .FirstOrDefault(border => border.Child == position);

                // Remove the Border from the WrapPanel
                if (userControlBorder != null)
                {
                    userControlsWrapPanel.Children.Remove(userControlBorder);
                }
            }
            else if(sender is ucEMS ems)
            {
                // Find the Border that wraps the specified ucPostions in the WrapPanel
                Border userControlBorder = userControlsWrapPanel.Children.OfType<Border>()
                                                       .FirstOrDefault(border => border.Child == ems);

                // Remove the Border from the WrapPanel
                if (userControlBorder != null)
                {
                    userControlsWrapPanel.Children.Remove(userControlBorder);
                }
            }
            else if (sender is ucTickerFreezer tickerFreezer)
            {
                // Find the Border that wraps the specified ucPostions in the WrapPanel
                Border userControlBorder = userControlsWrapPanel.Children.OfType<Border>()
                                                       .FirstOrDefault(border => border.Child == tickerFreezer);

                // Remove the Border from the WrapPanel
                if (userControlBorder != null)
                {
                    userControlsWrapPanel.Children.Remove(userControlBorder);
                }
            }
            else if (sender is ucOrderRejections ordRejection)
            {
                // Find the Border that wraps the specified ucPostions in the WrapPanel
                Border userControlBorder = userControlsWrapPanel.Children.OfType<Border>()
                                                       .FirstOrDefault(border => border.Child == ordRejection);

                // Remove the Border from the WrapPanel
                if (userControlBorder != null)
                {
                    userControlsWrapPanel.Children.Remove(userControlBorder);
                }
            }
            else if (sender is ucNav Nav)
            {
                // Find the Border that wraps the specified ucPostions in the WrapPanel
                Border userControlBorder = userControlsWrapPanel.Children.OfType<Border>()
                                                       .FirstOrDefault(border => border.Child == Nav);

                // Remove the Border from the WrapPanel
                if (userControlBorder != null)
                {
                    userControlsWrapPanel.Children.Remove(userControlBorder);
                }
            }
            else if (sender is ucSlippage slipp)
            {
                // Find the Border that wraps the specified ucPostions in the WrapPanel
                Border userControlBorder = userControlsWrapPanel.Children.OfType<Border>()
                                                       .FirstOrDefault(border => border.Child == slipp);

                // Remove the Border from the WrapPanel
                if (userControlBorder != null)
                {
                    userControlsWrapPanel.Children.Remove(userControlBorder);
                }
            }
            else if (sender is ucDeposits depo)
            {
                // Find the Border that wraps the specified ucPostions in the WrapPanel
                Border userControlBorder = userControlsWrapPanel.Children.OfType<Border>()
                                                       .FirstOrDefault(border => border.Child == depo);

                // Remove the Border from the WrapPanel
                if (userControlBorder != null)
                {
                    userControlsWrapPanel.Children.Remove(userControlBorder);
                }
            }
            else if (sender is ucPayments payment)
            {
                // Find the Border that wraps the specified ucPostions in the WrapPanel
                Border userControlBorder = userControlsWrapPanel.Children.OfType<Border>()
                                                       .FirstOrDefault(border => border.Child == payment);

                // Remove the Border from the WrapPanel
                if (userControlBorder != null)
                {
                    userControlsWrapPanel.Children.Remove(userControlBorder);
                }
            }
            else if (sender is ucCommFeeAdj cfa)
            {
                // Find the Border that wraps the specified ucPostions in the WrapPanel
                Border userControlBorder = userControlsWrapPanel.Children.OfType<Border>()
                                                       .FirstOrDefault(border => border.Child == cfa);

                // Remove the Border from the WrapPanel
                if (userControlBorder != null)
                {
                    userControlsWrapPanel.Children.Remove(userControlBorder);
                }
            }
            else if (sender is ucCorpActions corp)
            {
                // Find the Border that wraps the specified ucPostions in the WrapPanel
                Border userControlBorder = userControlsWrapPanel.Children.OfType<Border>()
                                                       .FirstOrDefault(border => border.Child == corp);

                // Remove the Border from the WrapPanel
                if (userControlBorder != null)
                {
                    userControlsWrapPanel.Children.Remove(userControlBorder);
                }
            }
        }
        /// <summary>
        /// ems
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBlock_MouseLeftButtonDown_2(object sender, MouseButtonEventArgs e)
        {
            if (sql_conn != null)
            {
                if (sender is TextBlock textBlock)
                {
                    // Create a new instance of the user control
                    ucEMS newUserControl = CreateNewUserControl(textBlock.Text) as ucEMS;

                    if (newUserControl != null)
                    {
                        newUserControl.RemoveControlRequested += YourUserControl_RemoveControlRequested;  // Subscribe to the event here

                        // Set margin to create spacing between user controls
                        newUserControl.Margin = new Thickness(5); // Adjust the thickness as needed

                        // Wrap the user control in a Border with a black border color and thickness
                        Border userControlBorder = new Border
                        {
                            BorderBrush = Brushes.Black,
                            BorderThickness = new Thickness(2),
                            Child = newUserControl
                        };

                        // Add the bordered user control to the WrapPanel
                        userControlsWrapPanel.Children.Add(userControlBorder);
                    }
                }
            }
            else
            {
                MessageBox.Show("No database connection was set up", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// ticker freezer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBlock_MouseLeftButtonDown_3(object sender, MouseButtonEventArgs e)
        {
            if (sql_conn != null)
            {
                if (sender is TextBlock textBlock)
                {
                    // Create a new instance of the user control
                    ucTickerFreezer newUserControl = CreateNewUserControl(textBlock.Text) as ucTickerFreezer;

                    if (newUserControl != null)
                    {
                        newUserControl.RemoveControlRequested += YourUserControl_RemoveControlRequested;  // Subscribe to the event here

                        // Set margin to create spacing between user controls
                        newUserControl.Margin = new Thickness(5); // Adjust the thickness as needed

                        // Wrap the user control in a Border with a black border color and thickness
                        Border userControlBorder = new Border
                        {
                            BorderBrush = Brushes.Black,
                            BorderThickness = new Thickness(2),
                            Child = newUserControl
                        };

                        // Add the bordered user control to the WrapPanel
                        userControlsWrapPanel.Children.Add(userControlBorder);
                    }
                }
            }
            else
            {
                MessageBox.Show("No database connection was set up", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// order rejections
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBlock_MouseLeftButtonDown_4(object sender, MouseButtonEventArgs e)
        {
            if (sql_conn != null)
            {
                if (sender is TextBlock textBlock)
                {
                    // Create a new instance of the user control
                    ucOrderRejections newUserControl = CreateNewUserControl(textBlock.Text) as ucOrderRejections;

                    if (newUserControl != null)
                    {
                        newUserControl.RemoveControlRequested += YourUserControl_RemoveControlRequested;  // Subscribe to the event here

                        // Set margin to create spacing between user controls
                        newUserControl.Margin = new Thickness(5); // Adjust the thickness as needed

                        // Wrap the user control in a Border with a black border color and thickness
                        Border userControlBorder = new Border
                        {
                            BorderBrush = Brushes.Black,
                            BorderThickness = new Thickness(2),
                            Child = newUserControl
                        };

                        // Add the bordered user control to the WrapPanel
                        userControlsWrapPanel.Children.Add(userControlBorder);
                    }
                }
            }
            else
            {
                MessageBox.Show("No database connection was set up", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// nav
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBlock_MouseLeftButtonDown_5(object sender, MouseButtonEventArgs e)
        {
            if (sql_conn != null)
            {
                if (sender is TextBlock textBlock)
                {
                    // Create a new instance of the user control
                    ucNav newUserControl = CreateNewUserControl(textBlock.Text) as ucNav;

                    if (newUserControl != null)
                    {
                        newUserControl.RemoveControlRequested += YourUserControl_RemoveControlRequested;  // Subscribe to the event here

                        // Set margin to create spacing between user controls
                        newUserControl.Margin = new Thickness(5); // Adjust the thickness as needed

                        // Wrap the user control in a Border with a black border color and thickness
                        Border userControlBorder = new Border
                        {
                            BorderBrush = Brushes.Black,
                            BorderThickness = new Thickness(2),
                            Child = newUserControl
                        };

                        // Add the bordered user control to the WrapPanel
                        userControlsWrapPanel.Children.Add(userControlBorder);
                    }
                }
            }
            else
            {
                MessageBox.Show("No database connection was set up", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// slippage
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBlock_MouseLeftButtonDown_6(object sender, MouseButtonEventArgs e)
        {
            if (sql_conn != null)
            {
                if (sender is TextBlock textBlock)
                {
                    // Create a new instance of the user control
                    ucSlippage newUserControl = CreateNewUserControl(textBlock.Text) as ucSlippage;

                    if (newUserControl != null)
                    {
                        newUserControl.RemoveControlRequested += YourUserControl_RemoveControlRequested;  // Subscribe to the event here

                        // Set margin to create spacing between user controls
                        newUserControl.Margin = new Thickness(5); // Adjust the thickness as needed

                        // Wrap the user control in a Border with a black border color and thickness
                        Border userControlBorder = new Border
                        {
                            BorderBrush = Brushes.Black,
                            BorderThickness = new Thickness(2),
                            Child = newUserControl
                        };

                        // Add the bordered user control to the WrapPanel
                        userControlsWrapPanel.Children.Add(userControlBorder);
                    }
                }
            }
            else
            {
                MessageBox.Show("No database connection was set up", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// deposits
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBlock_MouseLeftButtonDown_7(object sender, MouseButtonEventArgs e)
        {
            if (sql_conn != null)
            {
                if (sender is TextBlock textBlock)
                {
                    // Create a new instance of the user control
                    ucDeposits newUserControl = CreateNewUserControl(textBlock.Text) as ucDeposits;

                    if (newUserControl != null)
                    {
                        newUserControl.RemoveControlRequested += YourUserControl_RemoveControlRequested;  // Subscribe to the event here

                        // Set margin to create spacing between user controls
                        newUserControl.Margin = new Thickness(5); // Adjust the thickness as needed

                        // Wrap the user control in a Border with a black border color and thickness
                        Border userControlBorder = new Border
                        {
                            BorderBrush = Brushes.Black,
                            BorderThickness = new Thickness(2),
                            Child = newUserControl
                        };

                        // Add the bordered user control to the WrapPanel
                        userControlsWrapPanel.Children.Add(userControlBorder);
                    }
                }
            }
            else
            {
                MessageBox.Show("No database connection was set up", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// payments
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBlock_MouseLeftButtonDown_8(object sender, MouseButtonEventArgs e)
        {
            if (sql_conn != null)
            {
                if (sender is TextBlock textBlock)
                {
                    // Create a new instance of the user control
                    ucPayments newUserControl = CreateNewUserControl(textBlock.Text) as ucPayments;

                    if (newUserControl != null)
                    {
                        newUserControl.RemoveControlRequested += YourUserControl_RemoveControlRequested;  // Subscribe to the event here

                        // Set margin to create spacing between user controls
                        newUserControl.Margin = new Thickness(5); // Adjust the thickness as needed

                        // Wrap the user control in a Border with a black border color and thickness
                        Border userControlBorder = new Border
                        {
                            BorderBrush = Brushes.Black,
                            BorderThickness = new Thickness(2),
                            Child = newUserControl
                        };

                        // Add the bordered user control to the WrapPanel
                        userControlsWrapPanel.Children.Add(userControlBorder);
                    }
                }
            }
            else
            {
                MessageBox.Show("No database connection was set up", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// comm fee adjustments
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBlock_MouseLeftButtonDown_9(object sender, MouseButtonEventArgs e)
        {
            if (sql_conn != null)
            {
                if (sender is TextBlock textBlock)
                {
                    // Create a new instance of the user control
                    ucCommFeeAdj newUserControl = CreateNewUserControl(textBlock.Text) as ucCommFeeAdj;

                    if (newUserControl != null)
                    {
                        newUserControl.RemoveControlRequested += YourUserControl_RemoveControlRequested;  // Subscribe to the event here

                        // Set margin to create spacing between user controls
                        newUserControl.Margin = new Thickness(5); // Adjust the thickness as needed

                        // Wrap the user control in a Border with a black border color and thickness
                        Border userControlBorder = new Border
                        {
                            BorderBrush = Brushes.Black,
                            BorderThickness = new Thickness(2),
                            Child = newUserControl
                        };

                        // Add the bordered user control to the WrapPanel
                        userControlsWrapPanel.Children.Add(userControlBorder);
                    }
                }
            }
            else
            {
                MessageBox.Show("No database connection was set up", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TextBlock_MouseLeftButtonDown_10(object sender, MouseButtonEventArgs e)
        {
            if (sql_conn != null)
            {
                if (sender is TextBlock textBlock)
                {
                    // Create a new instance of the user control
                    ucCorpActions newUserControl = CreateNewUserControl(textBlock.Text) as ucCorpActions;

                    if (newUserControl != null)
                    {
                        newUserControl.RemoveControlRequested += YourUserControl_RemoveControlRequested;  // Subscribe to the event here

                        // Set margin to create spacing between user controls
                        newUserControl.Margin = new Thickness(5); // Adjust the thickness as needed

                        // Wrap the user control in a Border with a black border color and thickness
                        Border userControlBorder = new Border
                        {
                            BorderBrush = Brushes.Black,
                            BorderThickness = new Thickness(2),
                            Child = newUserControl
                        };

                        // Add the bordered user control to the WrapPanel
                        userControlsWrapPanel.Children.Add(userControlBorder);
                    }
                }
            }
            else
            {
                MessageBox.Show("No database connection was set up", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
