using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace wpfTDX
{
    public class PositionViewModel :INotifyPropertyChanged
    {
        private readonly string apiKey = ConfigurationManager.AppSettings["TradingApiKey"];
        private readonly string baseUrl = ConfigurationManager.AppSettings["TradingApiBaseUrl"];
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private ObservableCollection<PositionsDataModel> _positionsDataModel;
        public ObservableCollection<PositionsDataModel> PositionsData
        {
            get => _positionsDataModel;
            set
            {
                if (_positionsDataModel != value)
                {
                    _positionsDataModel = value;
                    OnPropertyChanged(nameof(PositionsData));
                }
            }
        }
        private PositionsDataModel _selectedposition;
        public PositionsDataModel SelectedPosition
        {
            get => _selectedposition;
            set
            {
                if (_selectedposition != value)
                {
                    _selectedposition = value;
                    OnPropertyChanged(nameof(SelectedPosition));
                }
            }
        }
        private SqlConnection _conn;
        public SqlConnection Conn
        {
            get => _conn;
            set
            {
                if(_conn!=value)
                {
                    _conn = value;
                    OnPropertyChanged(nameof(Conn));
                }
            }
        }
        private TDX.Position _tdxposition;
        public TDX.Position TdxPosition
        {
            get => _tdxposition;
            set
            {
                if(_tdxposition != value)
                {
                    _tdxposition = value;
                    OnPropertyChanged(nameof(TdxPosition));
                }
            }
        }
        private ObservableCollection<PositionsDataModel> _cashposition;
        public ObservableCollection<PositionsDataModel> CashPosition
        {
            get => _cashposition;
            set
            {
                if(_cashposition != value)
                {
                    _cashposition = value;
                    OnPropertyChanged(nameof(CashPosition));
                }
            }
        }
        private string _runStatus = "Run";
        public string RunStatus
        {
            get => _runStatus;
            set
            {
                if (_runStatus != value)
                {
                    _runStatus = value;
                    OnPropertyChanged(nameof(RunStatus));
                    (RunPositionCommand as TickerFreezerRelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }
        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (_isRunning != value)
                {
                    _isRunning = value;
                    OnPropertyChanged(nameof(IsRunning));
                    (RunPositionCommand as TickerFreezerRelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }
        private ObservableCollection<FundsDataModel> _funds;
        public ObservableCollection<FundsDataModel> Funds
        {
            get => _funds;
            set
            {
                if (_funds != value)
                {
                    _funds = value;
                    OnPropertyChanged(nameof(Funds));
                }
            }
        }
        private FundsDataModel _selectedFund;
        public FundsDataModel SelectedFund
        {
            get => _selectedFund;
            set
            {
                if (_selectedFund != value)
                {
                    _selectedFund = value;
                    OnPropertyChanged(nameof(SelectedFund));
                }
            }
        }
        public DateTime Date_t { get; set; }
        public ICommand RunPositionCommand { get; }
        public PositionViewModel()
        {
            PositionsData = new ObservableCollection<PositionsDataModel>();
            RunPositionCommand = new TickerFreezerRelayCommand(
                async () => await GetPosition(),
                () => !IsRunning);
        }
        public async Task GetPosition()
        {
            if (IsRunning) return;

            try
            {
                IsRunning = true;
                RunStatus = "Running..";
                this.TdxPosition = new TDX.Position(SelectedFund.FundName, Conn);
                await Task.Yield();
                await ProcessPositionDataASync(SelectedFund.FundName, Date_t);
                RunStatus = "Run";
            }
            catch (Exception ex)
            {
                RunStatus = "Error: " + ex.Message;
            }
            finally
            {
                IsRunning = false;
            }
        }

        public async Task GetFunds()
        {
            await ProcessFundsDataSync();
        }

        /// <summary>
        /// get all the meta data from funds table
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetFundsDataSync()
        {
            string url = $"{baseUrl}/get_funds";


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
        private async Task ProcessFundsDataSync()
        {
            string jsonResponse = await GetFundsDataSync();

            try
            {
                // Deserialize the JSON array directly into a list of FundDataModel
                var fundsList = JsonConvert.DeserializeObject<List<FundsDataModel>>(jsonResponse);

                // Initialize or clear the existing Funds collection
                if (Funds == null)
                {
                    Funds = new ObservableCollection<FundsDataModel>();
                }
                else
                {
                    Funds.Clear();
                }

                // Add the deserialized list to the ObservableCollection
                foreach (var fund in fundsList)
                {
                    Funds.Add(fund);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in ProcessFundsDataSync: " + ex.Message);
            }
        }
        private async Task<string> GetPositionDataASync(string _fundname, DateTime _date_t)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                var requestData = new
                {
                    fundname = _fundname,
                    posn_date = _date_t.ToString("yyyy-MM-dd"),
                };
                string jsonRequest = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

                // Make a synchronous HTTP POST request
                HttpResponseMessage response = await client.PostAsync($"{baseUrl}/get_position", content);
                response.EnsureSuccessStatusCode();

                string jsonResponse = response.Content.ReadAsStringAsync().Result;
                return jsonResponse;
            }
        }
        private async Task ProcessPositionDataASync(string _fundname, DateTime _date_t)
        {
            string jsonResponse = await GetPositionDataASync(_fundname, _date_t);

            try
            {
                JObject posnData = JObject.Parse(jsonResponse);

                // Deserialize "dfbuyandhold" into a list of MtmDataModel
                var positionList = JsonConvert.DeserializeObject<List<PositionsDataModel>>(posnData["dfposition"].ToString());
                var cashpositionList = JsonConvert.DeserializeObject<List<PositionsDataModel>>(posnData["dfcash"].ToString());


                // Clear existing data (if any) and add new data to BuyAndHoldData collection
                if (PositionsData == null)
                {
                    PositionsData = new ObservableCollection<PositionsDataModel>();
                }
                else
                {
                    PositionsData.Clear();
                }

                foreach (var item in positionList)
                {
                    PositionsData.Add(item);
                }
                //cash positions
                if(CashPosition == null)
                {
                    CashPosition = new ObservableCollection<PositionsDataModel>();
                }
                else
                {
                    CashPosition.Clear();
                }
                foreach(var item in cashpositionList)
                {
                    CashPosition.Add(item);
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public  async Task<bool> CloseoutExpiredAsync(
            string broker, string broker_id, string broker_id_exec, string fundname,
            string subaccountname, string action, string tad_id, string tickername,
            double price, double executed_qty, double multiplier, string exch_currency,DateTime ? bbg_lasttrade_date,
            DateTime ? roll_date,string instrument,string report_category,string category,double contract_increment,
            string bbg_symbol_exp,string sub_tickername)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                var requestData = new
                {
                    broker,
                    broker_id,
                    broker_id_exec,
                    fundname,
                    subaccountname,
                    action,
                    tad_id,
                    tickername,
                    price,
                    executed_qty,
                    multiplier,
                    exch_currency,
                    bbg_lasttrade_date,
                    roll_date,
                    instrument,
                    report_category,
                    category,
                    contract_increment,
                    bbg_symbol_exp,
                    sub_tickername
                };

                string jsonRequest = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

                try
                {
                    HttpResponseMessage response = await client.PostAsync($"{baseUrl}/closeout_expired", content);
                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    var responseDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonResponse);

                    if (responseDict.ContainsKey("success") && (bool)responseDict["success"] == true)
                    {
                        return true; 
                    }
                    else
                    {
                        Console.WriteLine($"[ERROR] API responded with an error: {jsonResponse}");
                        return false; 
                    }
                }
                catch (HttpRequestException httpEx)
                {
                    Console.WriteLine($"[ERROR] HTTP Request failed: {httpEx.Message}");
                    return false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Unexpected error: {ex.Message}");
                    return false;
                }
            }
        }


    }
}
