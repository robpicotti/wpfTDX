using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows.Input;
using System.Windows;


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
                }
            }
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

        //private void SelectAll(bool isChecked)
        //{
        //    if (TickerFreezer == null) return;

        //    foreach (var item in TickerFreezer)
        //    {
        //        if (isChecked)
        //        {
        //            // Only force override if not already overridden
        //            if (!item.Override)
        //            {
        //                item.Override = true;
        //                item.ForceOverride = true;
        //            }
        //        }
        //        else
        //        {
        //            // Uncheck only the ones that were force-overridden
        //            if (item.ForceOverride)
        //            {
        //                item.Override = false;
        //                item.ForceOverride = false;
        //            }
        //        }
        //    }
        //}


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
                        "*", "*", SelectedFund.FundName, "*", "*", "*", "*", 2001, "Manual Freeze on Fund", 0, "", 0, "", "*", "*", DBNull.Value.ToString());
   
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
                //if (SelectedFund.Freeze)
                //{
                //    SelectedFund.Freeze = false;
                //}
                //else
                //{
                //    SelectedFund.Freeze = true;
                //}
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
                    SelectedTicker.Emsname, SelectedTicker.BrokerCodeExec, SelectedTicker.Fundname, SelectedTicker.Execaccountname,
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
            string benchmarkname,string notes,string runtime_resolved)
        {
            string _runtime_resolved = runtime_resolved;
            if (_runtime_resolved == "") { _runtime_resolved = "NULL"; } else { _runtime_resolved = "'" + _runtime_resolved + "'"; }
            if (expiration == "") { expiration = "NULL"; } else { expiration = "'" + expiration + "'"; }
            if (override_expire == "") { override_expire = "NULL"; } else { override_expire = "'" + override_expire + "'"; }
 
            string tsql = "INSERT ticker_freezer VALUES('" + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff") + "','";
            tsql += emsname + "','" + broker_code_exec + "','" + fundname + "','";
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
                    SelectedTicker.BrokerCodeExec, SelectedTicker.Fundname, SelectedTicker.Execaccountname, SelectedTicker.Subaccountname,
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
        private string generate_override_freezer_sql(string runtime, string ems, string brokercode_exec, string fundname, string execaccountname, string subaccountname,
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
            string url = "http://localhost:5001/exec_sql";


            using (HttpClient client = new HttpClient())
            {
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
            string url = "http://localhost:5001/get_unresolved_tickerfreezer";
            string jsonResponse = "";
            using (HttpClient client = new HttpClient())
            {
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

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public async void Execute(object parameter)
        {
            if (_executeAsync != null)
            {
                await _executeAsync();
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
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

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute((T)parameter);
        }

        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    //public class TickerFreezerRelayCommand : ICommand
    //{
    //    private readonly Func<Task> _executeAsync;
    //    private readonly Func<bool> _canExecute;

    //    public TickerFreezerRelayCommand(Func<Task> executeAsync, Func<bool> canExecute = null)
    //    {
    //        _executeAsync = executeAsync;
    //        _canExecute = canExecute;
    //    }

    //    public event EventHandler CanExecuteChanged;

    //    public bool CanExecute(object parameter)
    //    {
    //        return _canExecute == null || _canExecute();
    //    }

    //    public async void Execute(object parameter)
    //    {
    //        if (_executeAsync != null)
    //        {
    //            await _executeAsync();
    //        }
    //    }

    //    public void RaiseCanExecuteChanged()
    //    {
    //        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    //    }
    //}



    public class MessageEventArgs : EventArgs
    {
        public string Message { get; set; }
        public string Title { get; set; }
    }

    public class ThawFreezerEventArgs : EventArgs
    {
        public string RunTime { get; set; }
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
}
