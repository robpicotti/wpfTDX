using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Windows.Input;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows;
using System.Configuration;
namespace wpfTDX
{
    public class MetadataTableChangesViewModel :INotifyPropertyChanged
    {
        private DataTable _tablesmetadataTable;
        private DataTable _filtersTable;
        private DataTable _weightsTable;
        private DataTable _allocationsTable;
        private DataTable _benchmarksTable;
        private DataTable _fundsTable;
        private DataTable _subaccountsTable;
        private DataTable _strategiesTable;
        private DataTable _modelsTable;
        private DataTable _regressionsTable;
        private DataTable _tadRollLogicTable;
        private DataTable _optRatioWaveTable;
        public ICommand RefreshCommand { get; } // Command for the Refresh button
        private readonly string apiKey = ConfigurationManager.AppSettings["TradingApiKey"];
        private readonly string baseUrl = ConfigurationManager.AppSettings["TradingApiBaseUrl"];

        public DataTable AllocationsTable
        {
            get => _allocationsTable;
            set
            {
                _allocationsTable = value;
                OnPropertyChanged(nameof(AllocationsTable));
            }
        }
        public DataTable FiltersTable
        {
            get => _filtersTable;
            set
            {
                _filtersTable = value;
                OnPropertyChanged(nameof(FiltersTable));
            }
        }
        public DataTable TablesMetadataTable
        {
            get => _tablesmetadataTable;
            set
            {
                _tablesmetadataTable = value;
                OnPropertyChanged(nameof(TablesMetadataTable));
            }
        }
        public DataTable WeightsTable
        {
            get => _weightsTable;
            set
            {
                _weightsTable = value;
                OnPropertyChanged(nameof(WeightsTable));
            }
        }
        public DataTable BenchmarksTable
        {
            get => _benchmarksTable;
            set
            {
                _benchmarksTable = value;
                OnPropertyChanged(nameof(BenchmarksTable));
            }
        }
        public DataTable FundsTable
        {
            get => _fundsTable;
            set
            {
                _fundsTable = value;
                OnPropertyChanged(nameof(FundsTable));
            }
        }
        public DataTable SubaccountsTable
        {
            get => _subaccountsTable;
            set
            {
                _subaccountsTable = value;
                OnPropertyChanged(nameof(SubaccountsTable));
            }
        }
        public DataTable StrategiesTable
        {
            get => _strategiesTable;
            set
            {
                _strategiesTable = value;
                OnPropertyChanged(nameof(StrategiesTable));
            }
        }
        public DataTable ModelsTable
        {
            get => _modelsTable;
            set
            {
                _modelsTable = value;
                OnPropertyChanged(nameof(ModelsTable));
            }
        }
        public DataTable RegressionsTable
        {
            get => _regressionsTable;
            set
            {
                _regressionsTable = value;
                OnPropertyChanged(nameof(RegressionsTable));
            }
        }
        public DataTable TADRollLogicTable
        {
            get => _tadRollLogicTable;
            set
            {
                _tadRollLogicTable = value;
                OnPropertyChanged(nameof(TADRollLogicTable));
            }
        }
        public DataTable OptRatioWaveTable
        {
            get => _optRatioWaveTable;
            set
            {
                _optRatioWaveTable = value;
                OnPropertyChanged(nameof(OptRatioWaveTable));
            }
        }
        public MetadataTableChangesViewModel()
        {
            RefreshCommand = new RefreshCommand(ExecuteRefreshCommand); // Initialize the command
        }

        // Implement INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        // Method to be called by the command
        public async void ExecuteRefreshCommand(object parameter)
        {
           await GetMetaDataTableChanges();
            MessageBox.Show(
                "Get metadata changes completed. If no data is visible, no metadata was changed in the last two days",
                "Metadata changes",MessageBoxButton.OK,MessageBoxImage.Information);

        }

        private async Task ProcessMetaDataChanges()
        {
            string jsonResponse = await GetMetadataChanges();
            try
            {
                var tablesData = JObject.Parse(jsonResponse);
                TablesMetadataTable = ConvertToDataTable(tablesData["metadata"].ToObject<List<Dictionary<string, object>>>());
                FiltersTable = ConvertToDataTable(tablesData["filters"].ToObject<List<Dictionary<string, object>>>());
                WeightsTable = ConvertToDataTable(tablesData["weights"].ToObject<List<Dictionary<string, object>>>());
                AllocationsTable = ConvertToDataTable(tablesData["allocations"].ToObject<List<Dictionary<string, object>>>());
                BenchmarksTable = ConvertToDataTable(tablesData["benchmarks"].ToObject<List<Dictionary<string, object>>>());
                FundsTable = ConvertToDataTable(tablesData["funds"].ToObject<List<Dictionary<string, object>>>());
                SubaccountsTable = ConvertToDataTable(tablesData["subaccounts"].ToObject<List<Dictionary<string, object>>>());
                StrategiesTable = ConvertToDataTable(tablesData["strategies"].ToObject<List<Dictionary<string, object>>>());
                ModelsTable = ConvertToDataTable(tablesData["models"].ToObject<List<Dictionary<string, object>>>());
                RegressionsTable = ConvertToDataTable(tablesData["regressions"].ToObject<List<Dictionary<string, object>>>());
                TADRollLogicTable = ConvertToDataTable(tablesData["tad_roll_logic"].ToObject<List<Dictionary<string, object>>>());
                OptRatioWaveTable = ConvertToDataTable(tablesData["opt_ratiowave"].ToObject<List<Dictionary<string, object>>>());
            }
            catch(Exception ex)
            {
                string message = "GetMetaDataChanges() error: " + ex.Message;
                throw new Exception(message);
            }
        }
        // Method to convert JSON table data to DataTable
        private DataTable ConvertToDataTable(List<Dictionary<string, object>> tableData)
        {
            var dataTable = new DataTable();

            if (tableData.Count == 0) return dataTable; // Return empty if no data

            var orderedColumns = tableData[0].Keys.ToList();
            // Add columns
            foreach (var columnName in orderedColumns)
            {
                dataTable.Columns.Add(columnName, typeof(string));
            }
            // Populate rows
            foreach (var row in tableData)
            {
                var dataRow = dataTable.NewRow();
                foreach (var columnName in orderedColumns)
                {
                    dataRow[columnName] = row[columnName] ?? DBNull.Value; // Handle nulls
                }
                dataTable.Rows.Add(dataRow);
            }

            return dataTable;
        }
        public async Task GetMetaDataTableChanges()
        {
            await ProcessMetaDataChanges();
        }

        private async Task<string> GetMetadataChanges()
        {
            string url = $"{baseUrl}/metadata_table_changes";
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

    }

    public class DataTableNotEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DataTable dataTable)
            {
                return dataTable.Rows.Count > 0; // Returns true if the DataTable has rows
            }
            return false; // Returns false if the DataTable is null or empty
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    // RelayCommand class for creating commands
    public class RefreshCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RefreshCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public class DataTableNotEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DataTable dataTable)
            {
                // Return Visible if the DataTable has rows, otherwise Collapsed
                return dataTable.Rows.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
