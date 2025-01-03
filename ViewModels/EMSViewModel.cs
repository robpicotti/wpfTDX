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
using System.Data.SqlClient;
using System.Data;

namespace wpfTDX
{
    public class EMSViewModel: INotifyPropertyChanged
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
        private ObservableCollection<ExecutionsDataModel> _executions;
        public ObservableCollection<ExecutionsDataModel> Executions
        {
            get => _executions;
            set
            {
                if(_executions != value)
                {
                    _executions = value;
                    OnPropertyChanged(nameof(Executions));
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
        private DateTime _fromdate;
        public DateTime FromDate
        {
            get => _fromdate;
            set
            {
                if(_fromdate != value)
                {
                    _fromdate = value;
                    OnPropertyChanged(nameof(FromDate));
                }
            }
        }
        private DateTime _todate;
        public DateTime ToDate
        {
            get => _todate;
            set
            {
                if(_todate != value)
                {
                    _todate = value;
                    OnPropertyChanged(nameof(ToDate));
                }
            }
        }
        private SqlConnection _sqlconn;
        public SqlConnection  SQLConn
        {
            get => _sqlconn;
            set
            {
                if(_sqlconn != value)
                {
                    _sqlconn = value;
                    OnPropertyChanged();
                }
            }
        }
        TDX.db _db = new TDX.db();
        private int _numexecutions;
        public int NumExecutions
        {
            get => _numexecutions;
            set
            {
                if(_numexecutions != value)
                {
                    _numexecutions = value;
                    OnPropertyChanged(nameof(NumExecutions));
                    OnPropertyChanged(nameof(ExecutionsTabHeader));
                }
            }
        }
        public string ExecutionsTabHeader
        {
            get => $"Executions - {NumExecutions}";
        }
        public EMSViewModel()
        {

        }
        public async Task GetExecutions()
        {
            await ProcessExecutionsData();
        }
        public async Task GetFunds()
        {
            await ProcessFundsDataSync();
        }
        private async Task ProcessTransactionsData()
        {
            DateTime enddate = this.ToDate.AddHours(23).AddMinutes(59).AddSeconds(59);
            DataTable tblTransactions = _db.get_account_transactions(SelectedFund.FundName, this.FromDate.ToString(), enddate.ToString(), this.SQLConn);
            string json = JsonConvert.SerializeObject(tblTransactions);
            //var transactionsList = JsonConvert.DeserializeObject<List<T>>
        }
        private async Task ProcessExecutionsData()
        {
            try 
            { 
                DateTime enddate = this.ToDate.AddHours(23).AddMinutes(59).AddSeconds(59);
                DataTable tblExecutions = new DataTable();
                tblExecutions = _db.get_fund_executions(SelectedFund.FundName, this.FromDate.ToString(), enddate.ToString(), this.SQLConn);
                string json = JsonConvert.SerializeObject(tblExecutions);
                var executionsList = JsonConvert.DeserializeObject<List<ExecutionsDataModel>>(json);
                if (Executions == null)
                {
                    Executions = new ObservableCollection<ExecutionsDataModel>();
                }
                else
                {
                    Executions.Clear(); // Clear existing items
                }
                foreach (var item in executionsList)
                {
                    Executions.Add(item);
                }
                NumExecutions = Executions.Count;
                OnPropertyChanged(nameof(Executions)); // Notify UI that Executions has been updated
                OnPropertyChanged(nameof(NumExecutions));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in GetExecutions: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
    }
}
