using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;


namespace wpfTDX
{
    public class TickerFreezerViewModel :INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<MessageEventArgs> ShowMessage;
        public event EventHandler OpenFreezeTadIdDialog;
        public event EventHandler<ThawFreezerEventArgs> OpenThawFreezerDialog;
        protected void OnOpenThawFreezerDialog(ThawFreezerEventArgs args)
        {
            OpenThawFreezerDialog?.Invoke(this, args);
        }
        public event EventHandler<ErrorInfoEventArgs> OpenErrorInfoDialog;
        protected void OnOpenErrorInfoDialog(ErrorInfoEventArgs args)
        {
            OpenErrorInfoDialog?.Invoke(this, args);
        }
        protected void OnShowMessage(string message, string title)
        {
            ShowMessage?.Invoke(this, new MessageEventArgs { Message = message, Title = title });
        }

        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        private ObservableCollection<TickerFreezerDataModel> _tickerfreezer { get; set; }
        public ObservableCollection<TickerFreezerDataModel> TickerFreezer
        {
            get => _tickerfreezer;
            set
            {
                if (_tickerfreezer != value)
                {
                    _tickerfreezer = value;
                    OnPropertyChanged(nameof(TickerFreezer));
                    // rebuild the filtered view whenever the collection changes
                    _tickerFreezerView = CollectionViewSource.GetDefaultView(_tickerfreezer);
                    if (_tickerFreezerView != null)
                        _tickerFreezerView.Filter = TickerFreezerFilter;
                    OnPropertyChanged(nameof(TickerFreezerView));
                }
            }
        }

        private ICollectionView _tickerFreezerView;
        public ICollectionView TickerFreezerView => _tickerFreezerView;

        private bool _showManual = false;
        public bool ShowManual
        {
            get => _showManual;
            set
            {
                if (_showManual != value)
                {
                    _showManual = value;
                    OnPropertyChanged();
                    _tickerFreezerView?.Refresh();
                }
            }
        }

        private bool TickerFreezerFilter(object item)
        {
            if (_showManual) return true;
            var row = item as TickerFreezerDataModel;
            return row == null || row.Errorcode != 2009;
        }

        private ObservableCollection<FundsDataModel> _fundsData { get; set; }
        public ObservableCollection<FundsDataModel> FundsData
        {
            get => _fundsData;
            set
            {
                if(_fundsData != value)
                {
                    _fundsData = value;
                    OnPropertyChanged(nameof(FundsData));
                }
            }
        }

        private FundsDataModel _selectedFund;
        public FundsDataModel SelectedFund
        {
            get => _selectedFund;
            set
            {
                if(_selectedFund != value)
                {
                    _selectedFund = value;
                    OnPropertyChanged(nameof(SelectedFund));
                }
            }
        }
        private string _lastRunTime;
        public string LastRunTime
        {
            get => _lastRunTime;
            set
            {
                if (_lastRunTime != value)
                {
                    _lastRunTime = value;
                    OnPropertyChanged(nameof(LastRunTime));
                }
            }
        }
        private TickerFreezerDataModel _selectedTicker;
        public TickerFreezerDataModel SelectedTicker
        {
            get => _selectedTicker;
            set
            {
                if (_selectedTicker != value)
                {
                    _selectedTicker = value;
                    OnPropertyChanged();

                    // Notify commands to re-evaluate their CanExecute status
                    (ThawTadIdCommand as TickerFreezerRelayCommand)?.RaiseCanExecuteChanged();
                    (RemoveOverrideCommand as TickerFreezerRelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private WebServiceData WSD; 

        public ICommand ThawTadIdCommand { get; }
        public ICommand OverrideTadIdAfterCloseCommand { get; }
        public ICommand OverrideTadIdNeverCommand { get; }
        public ICommand RemoveOverrideCommand { get; }
        public ICommand SearchTadIdCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SelectAllCommand { get; }
        public ICommand BatchOverrideCommand { get; }
        public ICommand SelectAllThawCommand { get; }
        public ICommand BatchThawCommand { get; }
        public ICommand GetFundDataCommand { get; }
        public ICommand ToggleFreezeFundCommand { get; }
        public ICommand ShowErrorInfoCommand { get; }

        private bool _isSelectAllChecked;
        public bool IsSelectAllChecked
        {
            get => _isSelectAllChecked;
            set
            {
                if (_isSelectAllChecked != value)
                {
                    _isSelectAllChecked = value;
                    OnPropertyChanged();

                    // Trigger SelectAllCommand logic when the checkbox value changes
                    SelectAll(value);
                }
            }
        }
        private bool _isSelectAllThawChecked;
        public bool IsSelectAllThawChecked
        {
            get => _isSelectAllThawChecked;
            set
            {
                if(_isSelectAllThawChecked !=value)
                {
                    _isSelectAllThawChecked = value;
                    OnPropertyChanged();
                    SelectAllThaw(value);
                }
            }
        }

        public TickerFreezerViewModel()
        {
            WSD  = new WebServiceData();
            ThawTadIdCommand = new TickerFreezerRelayCommand(
                async () => await ExecuteOpenThawFreezerAsync(),
                () => CanExecuteOpenThawFreezer());


            OverrideTadIdAfterCloseCommand = new TickerFreezerRelayCommand(
                async () => await OverrideTadId("After the close"),
                () => SelectedTicker != null);

            OverrideTadIdNeverCommand = new TickerFreezerRelayCommand(
                    async () => await OverrideTadId("Never"),
                    () => SelectedTicker != null);

            RemoveOverrideCommand = new TickerFreezerRelayCommand(
                async () => await RemoveOverride(),
                () => SelectedTicker != null);

            SearchTadIdCommand = new TickerFreezerRelayCommand(
                async () => await ExecuteFreezeTadId());

            RefreshCommand = new TickerFreezerRelayCommand(async () => await Refresh());

            SelectAllCommand = new TickerFreezerRelayCommand<bool>(SelectAll);
            BatchOverrideCommand = new TickerFreezerRelayCommand(async () => await ExecuteBatchOverride());
            BatchThawCommand = new TickerFreezerRelayCommand(async () => await ExecuteBatchThaw());
            GetFundDataCommand = new TickerFreezerRelayCommand(async () => await GetFundsData());
            ToggleFreezeFundCommand = new TickerFreezerRelayCommand(async () => await FreezeFund());
            ShowErrorInfoCommand = new TickerFreezerRelayCommand(
                async () => await ExecuteOpenErrorInfoAsync(),
                () => SelectedTicker != null);
        }

        private async Task ExecuteOpenErrorInfoAsync()
        {
            if (SelectedTicker == null) return;

            var args = new ErrorInfoEventArgs
            {
                Ticker = SelectedTicker,
                WebServiceData = WSD,
            };

            await Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    OnOpenErrorInfoDialog(args);
                });
            });
        }
        
        private void SelectAllThaw(bool isChecked)
        {
            if (TickerFreezer != null)
            {
                // Temporarily hold the items in a new collection
                var tempCollection = new ObservableCollection<TickerFreezerDataModel>(TickerFreezer);

                // Clear the original collection
                TickerFreezer.Clear();

                // Update the Thaw property for each item
                foreach (var item in tempCollection)
                {
                    item.Thaw = isChecked;
                }

                // Add all items back to the original collection
                foreach (var item in tempCollection)
                {
                    TickerFreezer.Add(item);
                }

                // Notify the UI about the changes
                OnPropertyChanged(nameof(TickerFreezer));
            }
        }

        private void SelectAll(bool isChecked)
        {
            if (TickerFreezer != null)
            {
                // Temporarily hold the items in a new collection
                var tempCollection = new ObservableCollection<TickerFreezerDataModel>(TickerFreezer);

                // Clear the original collection
                TickerFreezer.Clear();

                // Update the Thaw property for each item
                foreach (var item in tempCollection)
                {
                    if (!item.Override)
                    {
                        item.ForceOverride = isChecked;
                    }
                }

                // Add all items back to the original collection
                foreach (var item in tempCollection)
                {
                    TickerFreezer.Add(item);
                }

                // Notify the UI about the changes
                OnPropertyChanged(nameof(TickerFreezer));
            }
        }

        private static string apiKey = ConfigurationManager.AppSettings["TradingApiKey"];
        private static string baseUrl = ConfigurationManager.AppSettings["TradingApiBaseUrl"];


        public async Task FreezeFund()
        {
            if (SelectedFund == null) return;

            // Store the original state of the Freeze property
            bool originalFreezeState = SelectedFund.Freeze;
            // Temporarily toggle the Freeze state to reflect the checkbox state
            SelectedFund.Freeze = !SelectedFund.Freeze;
            // Show confirmation dialog
            var result = MessageBox.Show(
                    $"Are you sure you want to {(SelectedFund.Freeze ? "Freeze" : "Thaw")} the fund {SelectedFund.FundName}?",
                    "Confirm Action",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                string tsql = "";
                if (!originalFreezeState)
                {
                    tsql = GenerateFreezeSQL(
                        "*", "*", SelectedFund.FundName, "*", "*", "*", "*", 2001, "Manual Freeze on Fund", 0, "", 0, "", "*", "*", DBNull.Value.ToString(),
                        SelectedFund.FundGroupName);
   
                }
                else
                {
                    tsql = generate_thaw_fund_sql(SelectedFund.FundName);
                }
                execute_sql(tsql);
                await Refresh();
                // Additional logic here if necessary
            }
            else
            {
                // Reset the Freeze state to its original value
                SelectedFund.Freeze = originalFreezeState;
                OnPropertyChanged(nameof(FundsData));
            }
        

        }

        private async Task ExecuteBatchOverride()
        {
            if (TickerFreezer == null) return;

            foreach (var item in TickerFreezer.Where(t => t.ForceOverride))
            {
                try
                {
                    SelectedTicker = item; // Set the SelectedTicker for SQL generation
                    string sql = generate_override_freezer_sql(
                        SelectedTicker.Runtime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        SelectedTicker.Emsname,
                        SelectedTicker.BrokerCodeExec,
                        SelectedTicker.FundGroupName,
                        SelectedTicker.Fundname,
                        SelectedTicker.Execaccountname,
                        SelectedTicker.Subaccountname,
                        SelectedTicker.Tadid,
                        SelectedTicker.Errorcode.ToString(),
                        true,
                        "After the close"
                        );
                    execute_sql(sql); // Assuming execute_sql is already async
                }
                catch (Exception ex)
                {
                    // Handle any errors
                    OnShowMessage(ex.Message, "Batch Thaw Error");
                }
                finally
                {
                    Refresh();
                }
            }
        }

        private async Task ExecuteBatchThaw()
        {
            if (TickerFreezer == null) return;

            foreach (var item in TickerFreezer.Where(t => t.Thaw))
            {
                try
                {
                    SelectedTicker = item; // Set the SelectedTicker for SQL generation
                    string sql = generate_thaw_sql("PART OF BATCH OVERRIDE PROCESS");
                    execute_sql(sql); // Assuming execute_sql is already async
                }
                catch (Exception ex)
                {
                    // Handle any errors
                    OnShowMessage(ex.Message, "Batch Thaw Error");
                }
                finally
                {
                    Refresh();
                }
            }
        }
        private async Task ExecuteFreezeTadId()
        {
            try
            {
                // Notify the View to open the dialog
                OpenFreezeTadIdDialog?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                // Notify the View about the error
                OnShowMessage(ex.Message, "Freeze any tad_id");
            }
        }

        private async Task OverrideTadId(string expiration)
        {
            try
            {
                string tsql = generate_override_freezer_sql(SelectedTicker.Runtime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    SelectedTicker.Emsname, SelectedTicker.BrokerCodeExec, SelectedTicker.FundGroupName, SelectedTicker.Fundname, SelectedTicker.Execaccountname,
                    SelectedTicker.Subaccountname, SelectedTicker.Tadid, SelectedTicker.Errorcode.ToString(), true, expiration
                    );
                execute_sql(tsql);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                await Refresh();
            }
        }

        private string GenerateFreezeSQL(string emsname,string broker_code_exec,string fundname,
            string subaccountname,string execaccountname,string tad_id,string tickername,int error_code,
            string error_string,int resolved,string expiration,int _override, string override_expire,
            string benchmarkname,string notes,string runtime_resolved,string fundgroupname)
        {
            string _runtime_resolved = runtime_resolved;
            if (_runtime_resolved == "") { _runtime_resolved = "NULL"; } else { _runtime_resolved = "'" + _runtime_resolved + "'"; }
            if (expiration == "") { expiration = "NULL"; } else { expiration = "'" + expiration + "'"; }
            if (override_expire == "") { override_expire = "NULL"; } else { override_expire = "'" + override_expire + "'"; }
 
            string tsql = "INSERT ticker_freezer VALUES('" + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff") + "','";
            tsql += emsname + "','" + broker_code_exec + "','" + fundgroupname + "','" + fundname + "','";
            tsql += subaccountname + "','" + execaccountname + "','" + tad_id + "','";
            tsql += tickername + "'," + error_code.ToString() + ",'" + error_string + "',";
            tsql += resolved.ToString() + "," + expiration + "," + _override.ToString() + "," + override_expire;
            tsql += ",'" + benchmarkname + "','" + notes + "'," + _runtime_resolved + ")";

            return tsql;
        }


        private async Task RemoveOverride()
        {
            try
            {
                string tsql = generate_override_freezer_sql(SelectedTicker.Runtime.ToString("yyyy-MM-dd HH:mm:ss.fff"), SelectedTicker.Emsname,
                    SelectedTicker.BrokerCodeExec, SelectedTicker.FundGroupName,
                    SelectedTicker.Fundname, SelectedTicker.Execaccountname, SelectedTicker.Subaccountname,
                    SelectedTicker.Tadid, SelectedTicker.Errorcode.ToString(), false, null);
                execute_sql(tsql);
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                await Refresh();
            }
        }
        private string generate_override_freezer_sql(string runtime, string ems, string brokercode_exec, string fundgroupname,
            string fundname, string execaccountname, string subaccountname,
             string tad_id, string error_code, bool add_override, string expiration)
        {
            string _override = "1";
            if (!add_override) { _override = "0"; } //remove the override
            string t_sql = "UPDATE ticker_freezer" ;

            t_sql += " SET override = " + _override;
            if (expiration == "After the close")
            {
                string override_expiration = DateTime.Now.Date.AddDays(1).ToString("yyyy-MM-dd HH:mm:ss");
                t_sql += ",  override_expiration='" + override_expiration + "'";
            }
            t_sql += " WHERE runtime='" + runtime + "' AND broker_code_exec ='" + brokercode_exec + "' AND fundname='" + fundname + "' AND subaccountname='" + subaccountname + "'";
            t_sql += " AND execaccountname = '" + execaccountname + "'";
            t_sql += " AND tad_id='" + tad_id + "' AND error_code=" + error_code;
            t_sql += " AND fundgroupname='" + fundgroupname + "'";


            return t_sql;
        }
        private async Task ExecuteOpenThawFreezerAsync()
        {
            if (SelectedTicker == null) return;

            await Task.Run(() =>
            {
                var thawEventArgs = new ThawFreezerEventArgs
                {
                    RunTime = SelectedTicker.Runtime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    FundGroupName = SelectedTicker.FundGroupName,
                    FundName = SelectedTicker.Fundname,
                    ExecAccountName = SelectedTicker.Execaccountname,
                    SubAccountName = SelectedTicker.Subaccountname,
                    TadId = SelectedTicker.Tadid,
                    TickerName = SelectedTicker.Tickername,
                    BenchmarkName = SelectedTicker.Benchmarkname,
                    BrokerCodeExec = SelectedTicker.BrokerCodeExec,
                    EmsName = SelectedTicker.Emsname,
                    ErrorCode = SelectedTicker.Errorcode.ToString()
                };

                // Ensure raising the event happens on the UI thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    OnOpenThawFreezerDialog(thawEventArgs);
                });
            });
        }

        private string generate_thaw_fund_sql(string fundname)
        {
            string sql_text = "UPDATE ticker_freezer SET resolved=1 WHERE fundname='" + fundname + "' AND error_code=2001 AND resolved=0";
            return sql_text;
        }

        private string generate_thaw_sql(string note)
        {
            
            string sql_text = "UPDATE ticker_freezer SET resolved=1,notes='" + note + "', runtime_resolved='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'";
            sql_text += " WHERE runtime ='" + SelectedTicker.Runtime.ToString("yyyy-MM-dd HH:mm:ss.fff") + "' AND emsname ='" + 
                SelectedTicker.Emsname + "' AND broker_code_exec='" + SelectedTicker.BrokerCodeExec + "' AND fundname='";
            sql_text += SelectedTicker.Fundname + "' and subaccountname='" + SelectedTicker.Subaccountname + "' and tad_id = '" + SelectedTicker.Tadid + "'" +
                " AND tickername='" + SelectedTicker.Tickername + "' AND error_code=" + SelectedTicker.Errorcode.ToString();
            sql_text += " AND benchmarkname='" + SelectedTicker.Benchmarkname + "'";
            sql_text += " AND fundgroupname='" + SelectedTicker.FundGroupName + "'";
            return sql_text;
        }


        private bool CanExecuteOpenThawFreezer()
        {
            return SelectedTicker != null; // Only allow execution when a row is selected
        }

        public async Task Refresh()
        {
            await ProcessTickerFreezer();
        }
    
        public async Task GetFundsData()
        {
            await ProcessFundsData();
        }
        private async Task ProcessFundsData()
        {
            try
            {
                string jsonResponse = await WSD.GetFundsDataASync();
                JArray fundsdata = JArray.Parse(jsonResponse);
                var fundsdataList = JsonConvert.DeserializeObject<List<FundsDataModel>>(fundsdata.ToString());
                if (FundsData == null)
                {
                    FundsData = new ObservableCollection<FundsDataModel>();
                }
                else
                {
                    FundsData.Clear();
                }
                foreach (var item in fundsdataList)
                {
                    FundsData.Add(item);
                }
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        private string execute_sql(string tsql)
        {

            string url = $"{baseUrl}/exec_sql";
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                var requestData = new
                {
                    sqltext = tsql
                };
                string jsonRequest = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

                // Make a synchronous HTTP POST request
                HttpResponseMessage response = client.PostAsync(url, content).Result;
                response.EnsureSuccessStatusCode();

                string jsonResponse = response.Content.ReadAsStringAsync().Result;
                return jsonResponse;
            }

        }
        
        private async Task<string> GetTickerFreezer()
        {
            string url = $"{baseUrl}/get_unresolved_tickerfreezer";
            string jsonResponse = "";
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                HttpResponseMessage response = await client.PostAsync(url, null);
                response.EnsureSuccessStatusCode(); // Ensures that the response was successful

                jsonResponse = await response.Content.ReadAsStringAsync();
            }
            return jsonResponse;
        }
        
        private async Task ProcessTickerFreezer()
        {
           
            try
            {
                DateTime now = DateTime.UtcNow;
                LastRunTime = now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string jsonResponse = await GetTickerFreezer();
                // Parse the response as a JArray since it's a list of dictionaries
                JArray tickerfreezer = JArray.Parse(jsonResponse);

                // Deserialize the list of dictionaries directly into your DataModel
                var tickerfreezerList = JsonConvert.DeserializeObject<List<TickerFreezerDataModel>>(tickerfreezer.ToString());

                if (TickerFreezer == null)
                {
                    TickerFreezer = new ObservableCollection<TickerFreezerDataModel>();
                }
                else
                {
                    TickerFreezer.Clear();
                }

                // Add each job to the ObservableCollection
                foreach (var item in tickerfreezerList)
                {
                    TickerFreezer.Add(item);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
    /// <summary>
    /// non-generic relay command
    /// </summary>
    public class TickerFreezerRelayCommand : ICommand
    {
        private readonly Func<Task> _executeAsync;
        private readonly Func<bool> _canExecute;

        public TickerFreezerRelayCommand(Func<Task> executeAsync, Func<bool> canExecute = null)
        {
            _executeAsync = executeAsync;
            _canExecute = canExecute;
        }

       

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }


        public async void Execute(object parameter)
        {
            if (_executeAsync != null)
                await _executeAsync();
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>
    /// generic relay command
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TickerFreezerRelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public TickerFreezerRelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            // Safely handle null/non-T parameters
            if (_canExecute == null) return true;
            if (parameter == null && default(T) == null) return _canExecute(default(T));
            if (!(parameter is T)) return _canExecute(default(T));
            return _canExecute((T)parameter);
        }

        public void Execute(object parameter)
        {
            if (parameter == null && default(T) == null) { _execute(default(T)); return; }
            _execute(parameter is T t ? t : default(T));
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }




    public class MessageEventArgs : EventArgs
    {
        public string Message { get; set; }
        public string Title { get; set; }
    }

    public class ThawFreezerEventArgs : EventArgs
    {
        public string RunTime { get; set; }
        public string FundGroupName { get; set; }
        public string FundName { get; set; }
        public string ExecAccountName { get; set; }
        public string SubAccountName { get; set; }
        public string TadId { get; set; }
        public string TickerName { get; set; }
        public string BenchmarkName { get; set; }
        public string BrokerCodeExec { get; set; }
        public string EmsName { get; set; }
        public string ErrorCode { get; set; }
    }

    public class ErrorInfoEventArgs : EventArgs
    {
        public TickerFreezerDataModel Ticker { get; set; }
        public WebServiceData WebServiceData { get; set; }
    }
}
