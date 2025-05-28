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
using System.Data;

namespace wpfTDX
{
    public class MtmViewModel: INotifyPropertyChanged
    {
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
                    (RunMtmCommand as TickerFreezerRelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }
        private string _displayRunStatus;
        public string DisplayRunStatus
        {
            get { return _displayRunStatus; }
            set
            {
                if (_displayRunStatus != value)
                {
                    _displayRunStatus = value;
                    OnPropertyChanged(nameof(DisplayRunStatus));
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
                    (RunMtmCommand as TickerFreezerRelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }


        private MtmDataModel _mtmdatamodel;
        private ObservableCollection<MtmDataModel> _buyAndHoldData;
        private ObservableCollection<MtmDataModel> _newDealsData;

        public ObservableCollection<MtmDataModel> BuyAndHoldData
        {
            get => _buyAndHoldData;
            set
            {
                if (_buyAndHoldData != value)
                {
                    _buyAndHoldData = value;
                    OnPropertyChanged(nameof(BuyAndHoldData));
                }
            }
        }

        public ObservableCollection<MtmDataModel> NewDealsData
        {
            get => _newDealsData;
            set
            {
                if (_newDealsData != value)
                {
                    _newDealsData = value;
                    OnPropertyChanged();
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

        private double? _totalBuyAndHold;

        private double? _totalNewDeals;

        private double? _totalCommissions;

        public FundsDataModel SelectedFund
        {
            get => _selectedFund;
            set
            {
                if (_selectedFund != value)
                {
                    _selectedFund = value;
                    OnPropertyChanged(nameof(SelectedFund));
                    OnPropertyChanged(nameof(BaseCurrency)); // Notify that BaseCurrency has changed
                }
            }
        }

        public string BaseCurrency
        {
            get => SelectedFund?.BaseCurrency ?? string.Empty; // Ensure this never returns null
        }

        public double? TotalBuyAndHold
        {
            get => _totalBuyAndHold;
            set
            {
                if (_totalBuyAndHold != value)
                {
                    _totalBuyAndHold = value;
                    OnPropertyChanged(nameof(TotalBuyAndHold));
                    OnPropertyChanged(nameof(TotalPnl));
                }
            }
        }

        public double? TotalPnl
        {
            get
            {
                // Ensure that we return a sum of the three properties, handling null values
                return (_totalBuyAndHold ?? 0) + (TotalNewDeals ?? 0) + (TotalCommissions ?? 0);
            }
        }

        public double? TotalCommissions
        {
            get => _totalCommissions;
            set
            {
                if (_totalCommissions != value)
                {
                    _totalCommissions = value;
                    OnPropertyChanged(nameof(TotalCommissions));
                    OnPropertyChanged(nameof(TotalPnl));
                }
            }
        }

        public double? TotalNewDeals
        {
            get => _totalNewDeals;
            set
            {
                if (_totalNewDeals != value)
                {
                    _totalNewDeals = value;
                    OnPropertyChanged(nameof(TotalNewDeals));
                    OnPropertyChanged(nameof(TotalPnl));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string FundName { get; set; }
        private DateTime _datet;
        public DateTime Date_t
        {
            get => _datet;
            set
            {
                _datet = value;
                OnPropertyChanged(nameof(Date_t));
                OnPropertyChanged(nameof(IsMultiDay));
            }
        }
        private DateTime _datetminus1;
        public DateTime Date_tminus1 {
            get => _datetminus1;
            set
            {
                _datetminus1 = value;
                OnPropertyChanged(nameof(Date_tminus1));
                OnPropertyChanged(nameof(IsMultiDay));
            }
        }
        public ICommand RunMtmCommand { get; }

        private bool _showMultiDay;
        public bool ShowMultiDay
        {
            get => _showMultiDay;
            set
            {
                if (_showMultiDay != value)
                {
                    _showMultiDay = value;
                    OnPropertyChanged(nameof(ShowMultiDay));
                }
            }
        }


        public MtmViewModel()
        {
            BuyAndHoldData = new ObservableCollection<MtmDataModel>();
            NewDealsData = new ObservableCollection<MtmDataModel>();
            Funds = new ObservableCollection<FundsDataModel>();
            Date_t = DateTime.Today;
            Date_tminus1 = Date_t;
            InitializePreviousBusinessDay();
            RunMtmCommand = new TickerFreezerRelayCommand(
                async () => await GetPnl(),
                () => !IsRunning);
        }
        public bool IsMultiDay
        {
            get
            {
                return (Date_t - Date_tminus1).TotalDays > 1;
            }
        }
        private bool _isDataLoaded;
        public bool IsDataLoaded
        {
            get => _isDataLoaded;
            set
            {
                if (_isDataLoaded != value)
                {
                    _isDataLoaded = value;
                    OnPropertyChanged(nameof(IsDataLoaded));
                }
            }
        }

        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        //public async Task GetPnl()
        public async Task GetPnl()
        {

            if (IsRunning) return;

            try
            {
                if (SelectedFund != null)
                {
                    int datediff = (Date_t - Date_tminus1).Days;
                    if (datediff < 0)
                    {
                        throw new ArgumentException("Start date cannot be greater than end date.");
                    }
                    IsRunning = true;
                    RunStatus = "Running..";
                    DisplayRunStatus = "";
                    await Task.Yield();
                    string base_currency = SelectedFund.BaseCurrency.Substring(0, 3);
                    await ProcessPnlDataASync(SelectedFund.FundName, Date_t, Date_tminus1, base_currency);
                    IsDataLoaded = true;
                    RunStatus = "Run";
                    // Update ShowMultiDay here
                    ShowMultiDay = datediff > 1;
                }
            }
            catch(ArgumentException ax)
            {
                DisplayRunStatus = "Invalid Input: " + ax.Message;
            }
            catch (Exception ex)
            {
                RunStatus = "Error";
                DisplayRunStatus = ex.Message;
            }
            finally
            {
                IsRunning = false;
            }
        }
        public async Task<string> GetPreviousBusinessDay(DateTime date)
        {
            string url = "http://localhost:5001/previous_business_day"; // Ensure correct API URL

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Prepare JSON request body
                    var requestBody = new
                    {
                        date = date.ToString()  // Send date if provided, otherwise send null (default)
                    };

                    // Convert to JSON and send POST request
                    var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync(url, content);
                    response.EnsureSuccessStatusCode(); // Ensure HTTP response is successful

                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var json = JObject.Parse(jsonResponse);

                    // Parse and return the previous business day
                    string previousBusinessDay = json["previous_business_day"].ToString();
                    return previousBusinessDay;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching previous business day: " + ex.Message);
                return null;
            }
        }


        private async void InitializePreviousBusinessDay()
        {
            string previousBusinessDayString = await GetPreviousBusinessDay(Date_t);

            if (DateTime.TryParse(previousBusinessDayString, out DateTime result))
            {
                Date_tminus1 = result;
                OnPropertyChanged(nameof(Date_tminus1)); // Notify UI if using MVVM
            }
            else
            {
                Console.WriteLine("Error: Could not convert previous business day to DateTime.");
            }
        }


        public async Task GetFunds()
        {
            await ProcessFundsDataSync();
        }

        private async Task<string> GetPnlDataAsync(string _fundname, DateTime _date_t, DateTime _date_tminus1, string _base_currency)
        {
            using (HttpClient client = new HttpClient())
            {
                var requestData = new
                {
                    fundname = _fundname,
                    date_t = _date_t.ToString("yyyy-MM-dd"),
                    date_tminus1 = _date_tminus1.ToString("yyyy-MM-dd"),
                    base_currency = _base_currency
                };
                string jsonRequest = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync("http://localhost:5001/pnl", content);
                response.EnsureSuccessStatusCode();

                string jsonResponse = await response.Content.ReadAsStringAsync();
                return jsonResponse;
            }
        }

        private async Task ProcessPnlData(string _fundname, DateTime _date_t, DateTime _date_tminus1, string _base_currency)
        {
            string jsonResponse = await GetPnlDataAsync(_fundname, _date_t, _date_tminus1, _base_currency);

            try
            {
                JObject pnlData = JObject.Parse(jsonResponse);

                // Deserialize "dfbuyandhold" into a list of MtmDataModel
                var buyAndHoldList = JsonConvert.DeserializeObject<List<MtmDataModel>>(pnlData["dfbuyandhold"].ToString());
                var newDealsList = JsonConvert.DeserializeObject<List<MtmDataModel>>(pnlData["dfnewdeals"].ToString());

                // Clear existing data (if any) and add new data to BuyAndHoldData collection
                if (BuyAndHoldData == null)
                {
                    BuyAndHoldData = new ObservableCollection<MtmDataModel>();
                }
                else
                {
                    BuyAndHoldData.Clear();
                }
                foreach (var item in buyAndHoldList)
                {
                    BuyAndHoldData.Add(item);
                }

                // Clear existing data (if any) and add new data to NewDealsData collection
                if (NewDealsData == null)
                {
                    NewDealsData = new ObservableCollection<MtmDataModel>();
                }
                else
                {
                    NewDealsData.Clear();
                }
                foreach (var item in newDealsList)
                {
                    NewDealsData.Add(item);
                }

                // Notify the UI that BuyAndHoldData has changed
                OnPropertyChanged(nameof(BuyAndHoldData));
                // Notify the UI that BuyAndHoldData has changed
                OnPropertyChanged(nameof(NewDealsData));
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private async Task ProcessPnlDataASync(string _fundname, DateTime _date_t, DateTime _date_tminus1, string _base_currency)
        {
            string jsonResponse = await GetPnlDataASync(_fundname, _date_t, _date_tminus1, _base_currency);

            try
            {
                JObject pnlData = JObject.Parse(jsonResponse);

                // Deserialize "dfbuyandhold" into a list of MtmDataModel
                var buyAndHoldList = JsonConvert.DeserializeObject<List<MtmDataModel>>(pnlData["dfbuyandhold"].ToString());

                var newDealsList = JsonConvert.DeserializeObject<List<MtmDataModel>>(pnlData["dfnewdeals"].ToString());
                TotalBuyAndHold = pnlData["buyandhold"].Value<double?>();
                TotalNewDeals= pnlData["newdeals"].Value<double?>();
                TotalCommissions = pnlData["commissions"].Value<double?>();

                // Clear existing data (if any) and add new data to BuyAndHoldData collection
                if (BuyAndHoldData == null)
                {
                    BuyAndHoldData = new ObservableCollection<MtmDataModel>();
                }
                else
                {
                    BuyAndHoldData.Clear();
                }

                foreach (var item in buyAndHoldList)
                {
                    BuyAndHoldData.Add(item);
                }

                // Clear existing data (if any) and add new data to NewDealsData collection
                if (NewDealsData == null)
                {
                    NewDealsData = new ObservableCollection<MtmDataModel>();
                }
                else
                {
                    NewDealsData.Clear();
                }
                foreach (var item in newDealsList)
                {
                    NewDealsData.Add(item);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private async Task ProcessFundsDataSync()
        {
            string jsonResponse =  await GetFundsDataSync();

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
                throw new Exception("Error in ProcessFundsDataSync: "+ ex.Message);
            }
        }

        /// <summary>
        /// get all the meta data from funds table
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetFundsDataSync()
        {
            string url = "http://localhost:5001/get_funds";


            string jsonResponse = "";
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.PostAsync(url, null);
                response.EnsureSuccessStatusCode(); // Ensures that the response was successful

                jsonResponse = await response.Content.ReadAsStringAsync();
            }
            return jsonResponse;

        }

        private async Task<string> GetPnlDataASync(string _fundname, DateTime _date_t, DateTime _date_tminus1, string _base_currency)
        {
            using (HttpClient client = new HttpClient())
            {
                var requestData = new
                {
                    fundname = _fundname,
                    date_t = _date_t.ToString("yyyy-MM-dd"),
                    date_tminus1 = _date_tminus1.ToString("yyyy-MM-dd"),
                    base_currency = _base_currency,
                    filter_benchmark_tickers = true
                };
                string jsonRequest = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

                // Make a synchronous HTTP POST request
                HttpResponseMessage response = await client.PostAsync("http://localhost:5001/pnl", content);
                response.EnsureSuccessStatusCode();

                string jsonResponse = response.Content.ReadAsStringAsync().Result;
                return jsonResponse;
            }
        }
    }
}
