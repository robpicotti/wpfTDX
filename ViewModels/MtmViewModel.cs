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
using System.Data;

namespace wpfTDX
{
    public class MtmViewModel: INotifyPropertyChanged
    {
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
        public DateTime Date_t { get; set; }
        public DateTime Date_tminus1 { get; set; }
        public MtmViewModel()
        {
            BuyAndHoldData = new ObservableCollection<MtmDataModel>();
            NewDealsData = new ObservableCollection<MtmDataModel>();
            Funds = new ObservableCollection<FundsDataModel>();
            //TotalBuyAndHold = 0;
            //TotalNewDeals = 0;
            //TotalCommissions = 0;
        }
        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        //public async Task GetPnl()
        public void GetPnl()
        {
            string base_currency = SelectedFund.BaseCurrency.Substring(0, 3);
            //await ProcessPnlData("ENBW-testing", this.Date_t, this.Date_tminus1, "EUR");
            ProcessPnlDataSync(this.SelectedFund.FundName, this.Date_t, this.Date_tminus1, base_currency);
        }

        public void GetFunds()
        {
            ProcessFundsDataSync();
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

        private void ProcessPnlDataSync(string _fundname, DateTime _date_t, DateTime _date_tminus1, string _base_currency)
        {
            string jsonResponse = GetPnlDataSync(_fundname, _date_t, _date_tminus1, _base_currency);

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

        private void ProcessFundsDataSync()
        {
            string jsonResponse = GetFundsDataSync();

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
        private string GetFundsDataSync()
        {
            using (HttpClient client = new HttpClient())
            {
                var requestData = new
                {
                    table_name = "funds",
   
                };
                string jsonRequest = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

                // Make a synchronous HTTP POST request
                HttpResponseMessage response = client.PostAsync("http://localhost:5001/select_table", content).Result;
                response.EnsureSuccessStatusCode();

                string jsonResponse = response.Content.ReadAsStringAsync().Result;
                return jsonResponse;
            }
        }

        private string GetPnlDataSync(string _fundname, DateTime _date_t, DateTime _date_tminus1, string _base_currency)
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

                // Make a synchronous HTTP POST request
                HttpResponseMessage response = client.PostAsync("http://localhost:5001/pnl", content).Result;
                response.EnsureSuccessStatusCode();

                string jsonResponse = response.Content.ReadAsStringAsync().Result;
                return jsonResponse;
            }
        }
    }
}
