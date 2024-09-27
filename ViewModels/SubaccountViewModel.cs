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
    public class SubaccountViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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
        private SubaccountDataModel _subaccountdatamodel;
        public ObservableCollection<SubaccountDataModel> Subaccounts;

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

        public SubaccountViewModel()
        {
            Funds = new ObservableCollection<FundsDataModel>();
            Subaccounts = new ObservableCollection<SubaccountDataModel>();
        }
        public void GetSubaccounts()
        {
            
        }
        private void ProcessSubaccountData()
        {
            string jsonResponse = GetSubaccountData();
            JObject subaccountsdata = JObject.Parse(jsonResponse);

            // Deserialize "dfbuyandhold" into a list of MtmDataModel
            var subaccounts = JsonConvert.DeserializeObject<List<SubaccountDataModel>>(subaccountsdata["df"].ToString());
            foreach (var item in subaccounts)
            {
                if (item.FundName == this.SelectedFund.FundName)
                {
                    Subaccounts.Add(item);
                }
            }
        }
        private string GetSubaccountData()
        {
            using (HttpClient client = new HttpClient())
            {
                var requestData = new
                {
                    table_name = "subaccounts",   
                };
                string jsonRequest = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

                // Make a synchronous HTTP POST request
                HttpResponseMessage response = client.PostAsync("http://localhost:5000/table_select", content).Result;
                response.EnsureSuccessStatusCode();

                string jsonResponse = response.Content.ReadAsStringAsync().Result;
                return jsonResponse;
            }
        }

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
                HttpResponseMessage response = client.PostAsync("http://localhost:5000/select_table", content).Result;
                response.EnsureSuccessStatusCode();

                string jsonResponse = response.Content.ReadAsStringAsync().Result;
                return jsonResponse;
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
                throw new Exception("Error in ProcessFundsDataSync: " + ex.Message);
            }
        }
    }

}
