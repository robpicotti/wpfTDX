using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;   
using System.Windows.Data;
using System.Windows.Input;
using System.Configuration;

namespace wpfTDX
{
    public class PortfolioWeightsViewModel : INotifyPropertyChanged
    {
        private readonly string apiKey = ConfigurationManager.AppSettings["TradingApiKey"];
        private readonly string baseUrl = ConfigurationManager.AppSettings["TradingApiBaseUrl"];
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            var h = PropertyChanged; if (h != null) h(this, new PropertyChangedEventArgs(name));
        }

        public ICommand GetPortfolioDataCommand { get; }
        public ICommand GetTickerDataCommand { get; }

        private TickerFreezerRelayCommand _saveChangesCommand;
        private TickerFreezerRelayCommand<IList> _addSelectedTickersCommand;
        private TickerFreezerRelayCommand<IList> _removeSelectedWeightsCommand;

        public ICommand SaveChangesCommand { get { return _saveChangesCommand; } }
        public ICommand AddSelectedTickersCommand { get { return _addSelectedTickersCommand; } }
        public ICommand RemoveSelectedWeightsCommand { get { return _removeSelectedWeightsCommand; } }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged(nameof(IsBusy));
                    OnPropertyChanged(nameof(IsNotBusy)); // keep in sync
                    RaiseCommandCanExecutes();
                }
            }
        }

        public bool IsNotBusy => !IsBusy;

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged(nameof(StatusMessage));
                }
            }
        }

        public PortfolioWeightsViewModel()
        {
            GetPortfolioDataCommand = new TickerFreezerRelayCommand(async () => await GetPortfolioData());

            PortfolioData = new ObservableCollection<PortfolioDataModel>();
            PortfolioWeights = new ObservableCollection<PortfolioWeightsDataModel>();
            //_saveChangesCommand = new TickerFreezerRelayCommand(
            //    async () => await SaveChangesAsync(),
            //    () => !string.IsNullOrWhiteSpace(SelectedPortfolioName)
            //);
            _saveChangesCommand = new TickerFreezerRelayCommand(
                    async () => await SaveChangesAsync(),
                    () => IsNotBusy && !string.IsNullOrWhiteSpace(SelectedPortfolioName)
                );

            _addSelectedTickersCommand = new TickerFreezerRelayCommand<IList>(
                            items => { var _ = AddSelectedTickersAsync(items); },   // fire-and-forget wrapper
                            items => !string.IsNullOrWhiteSpace(SelectedPortfolioName) && items != null && items.Count > 0
                        );

                        _removeSelectedWeightsCommand = new TickerFreezerRelayCommand<IList>(
                            rows => { var _ = RemoveSelectedWeightsAsync(rows); },   // fire-and-forget wrapper
                            rows => rows != null && rows.Count > 0
                        );

        }
        // Original state (loaded from DB) for the currently selected portfolio
        private readonly HashSet<string> _originalTickers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, double?> _originalWeights = new Dictionary<string, double?>(StringComparer.OrdinalIgnoreCase);

        // User actions during this session
        private readonly HashSet<string> _newlyAddedTickers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _editedTickers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // (_editedTickers includes both “value changed” and “soft-removed” when weight becomes null)

        private void RaiseCommandCanExecutes()
        {
            _saveChangesCommand.RaiseCanExecuteChanged();
            _addSelectedTickersCommand.RaiseCanExecuteChanged();
            _removeSelectedWeightsCommand.RaiseCanExecuteChanged();
        }


        public string SelectedPortfolioName
        {
            get { return _selectedPortfolioName; }
            set
            {
                if (string.Equals(_selectedPortfolioName, value, StringComparison.Ordinal)) return;
                _selectedPortfolioName = value;
                OnPropertyChanged();
                RaiseCommandCanExecutes();                      // update buttons’ enabled state
                var _ = LoadPortfolioWeightsDataAsync(_selectedPortfolioName);
            }
        }




        private readonly WebServiceData _wsd = new WebServiceData();

        // NEW: raw tickers and a view for sorting/filtering
        private ObservableCollection<TickerDataModel> _tickers = new ObservableCollection<TickerDataModel>();
        public ObservableCollection<TickerDataModel> Tickers
        {
            get { return _tickers; }
            set { if (!object.ReferenceEquals(_tickers, value)) { _tickers = value; OnPropertyChanged(); } }
        }
        private ICollectionView _filteredTickers;
        public ICollectionView FilteredTickers
        {
            get { return _filteredTickers; }
            private set { if (!object.ReferenceEquals(_filteredTickers, value)) { _filteredTickers = value; OnPropertyChanged(); } }
        }

        private string _tickerFilter;
        public string TickerFilter
        {
            get { return _tickerFilter; }
            set
            {
                if (_tickerFilter == value) return;
                _tickerFilter = value;
                OnPropertyChanged();
                if (FilteredTickers != null) FilteredTickers.Refresh();
            }
        }
        // -------- Portfolios list (for ComboBox) --------
        private ObservableCollection<PortfolioDataModel> _portfolioData;
        public ObservableCollection<PortfolioDataModel> PortfolioData
        {
            get { return _portfolioData; }
            set { if (!object.ReferenceEquals(_portfolioData, value)) { _portfolioData = value; OnPropertyChanged(); } }
        }

        // -------- Selected portfolio NAME (string) --------
        private string _selectedPortfolioName;
        

        // -------- DataGrid source --------
        private ObservableCollection<PortfolioWeightsDataModel> _portfolioWeights;
        public ObservableCollection<PortfolioWeightsDataModel> PortfolioWeights
        {
            get { return _portfolioWeights; }
            set { if (!object.ReferenceEquals(_portfolioWeights, value)) { _portfolioWeights = value; OnPropertyChanged(); } }
        }



        private readonly HashSet<string> _removedOriginalTickers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private async Task AddSelectedTickersAsync(System.Collections.IList selected)
        {
            if (string.IsNullOrWhiteSpace(SelectedPortfolioName)) return;
            if (selected == null || selected.Count == 0) return;

            foreach (var obj in selected)
            {
                var t = obj as TickerDataModel;
                if (t == null || string.IsNullOrWhiteSpace(t.Tickername)) continue;

                // Check if this ticker already exists in the right-hand grid
                var existingRow = PortfolioWeights.FirstOrDefault(r =>
                    string.Equals(r.TickerName, t.Tickername, StringComparison.OrdinalIgnoreCase));

                if (existingRow != null)
                {
                    if (existingRow.IsMarkedForRemoval)
                    {
                        // Restore it — undo the soft remove
                        existingRow.IsMarkedForRemoval = false;
                        existingRow.IsEdited = false;

                        // Restore original weight
                        double? originalWeight;
                        if (_originalWeights.TryGetValue(existingRow.TickerName, out originalWeight))
                            existingRow.Weight = originalWeight;

                        _editedTickers.Remove(existingRow.TickerName);
                    }
                    // else it's a genuine duplicate — skip
                    continue;
                }

                // Brand new row
                var row = new PortfolioWeightsDataModel
                {
                    PortfolioName = SelectedPortfolioName,
                    TickerName = t.Tickername,
                    Weight = 1,
                    RunTime = DateTime.UtcNow
                };
                row.IsOriginal = false;
                row.IsNew = true;
                row.IsEdited = true;

                PortfolioWeights.Insert(0, row);
                _newlyAddedTickers.Add(t.Tickername);
                _originalWeights[t.Tickername] = null;

                var npc = row as INotifyPropertyChanged;
                if (npc != null) npc.PropertyChanged += OnWeightRowPropertyChanged;
            }
        }

        private async Task RemoveSelectedWeightsAsync(System.Collections.IList selectedRows)
        {
            if (selectedRows == null || selectedRows.Count == 0) return;

            var toRemove = new List<PortfolioWeightsDataModel>();

            foreach (var obj in selectedRows)
            {
                var row = obj as PortfolioWeightsDataModel;
                if (row == null) continue;

                var key = row.TickerName ?? "";

                if (_newlyAddedTickers.Contains(key))
                {
                    // Added this session and not yet saved — remove entirely
                    toRemove.Add(row);
                    _newlyAddedTickers.Remove(key);
                    _editedTickers.Remove(key);
                    _originalWeights.Remove(key);
                    continue;
                }

                if (_originalTickers.Contains(key))
                {
                    // Original DB row — soft remove: flag it visually, set weight null
                    row.Weight = null;
                    row.IsMarkedForRemoval = true;
                }
            }

            foreach (var r in toRemove)
            {
                var npc = r as INotifyPropertyChanged;
                if (npc != null) npc.PropertyChanged -= OnWeightRowPropertyChanged;
                PortfolioWeights.Remove(r);
            }
        }
        private async Task SaveChangesAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedPortfolioName)) return;

            // Payload = ALL rows with a valid (non-null, non-NaN) weight
            // This means: existing tickers kept + new tickers added, minus anything marked for removal
            var payload = PortfolioWeights
                .Where(r => r != null && !string.IsNullOrWhiteSpace(r.TickerName))
                .Where(r => (r.Weight.HasValue && !double.IsNaN(r.Weight.Value)) // valid weight
                         || r.IsMarkedForRemoval)                                 // OR flagged for removal (sends null)
                .Select(r => new PortfolioWeightsDataModel
                {
                    PortfolioName = SelectedPortfolioName,
                    RunTime = DateTime.UtcNow,
                    TickerName = r.TickerName,
                    Weight = r.IsMarkedForRemoval ? null : r.Weight        // explicit null for removed
                })
                .ToList();


            if (payload.Count == 0)
            {
                StatusMessage = "No valid weights to save.";
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = "Saving portfolio weights...";

                await _wsd.SavePortfolioWeightsAsync(payload);

                // Clear tracking sets
                _originalTickers.Clear();
                _originalWeights.Clear();
                _newlyAddedTickers.Clear();
                _editedTickers.Clear();

                // Remove soft-deleted rows from UI, reset flags on kept rows
                foreach (var row in PortfolioWeights.ToList())
                {
                    if (row == null) continue;

                    bool isValid = row.Weight.HasValue && !double.IsNaN(row.Weight.Value);

                    if (!isValid || row.IsMarkedForRemoval)
                    {
                        var npc = row as INotifyPropertyChanged;
                        if (npc != null) npc.PropertyChanged -= OnWeightRowPropertyChanged;
                        PortfolioWeights.Remove(row);
                        continue;
                    }

                    // Commit this row as the new original state
                    row.IsNew = false;
                    row.IsEdited = false;
                    row.IsMarkedForRemoval = false;
                    row.IsOriginal = true;

                    _originalTickers.Add(row.TickerName);
                    _originalWeights[row.TickerName] = row.Weight;
                }

                StatusMessage = "Portfolio weights saved successfully.";
                Application.Current.Dispatcher.Invoke(() =>
                    MessageBox.Show("Portfolio weights saved successfully.", "Saved",
                                    MessageBoxButton.OK, MessageBoxImage.Information));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Save failed: " + ex.Message);
                StatusMessage = "Error saving portfolio weights: " + ex.Message;
                Application.Current.Dispatcher.Invoke(() =>
                    MessageBox.Show("Save failed:\n" + ex.Message, "Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        //private async Task SaveChangesAsync()
        //{
        //    if (string.IsNullOrWhiteSpace(SelectedPortfolioName)) return;

        //    // Build payload: NEW + EDITED (including soft-removed where Weight == null)
        //    var payload = new List<PortfolioWeightsDataModel>();

        //    // New rows
        //    foreach (var key in _newlyAddedTickers)
        //    {
        //        var row = PortfolioWeights.FirstOrDefault(r =>
        //            string.Equals(r.TickerName, key, StringComparison.OrdinalIgnoreCase));
        //        if (row == null) continue;

        //        payload.Add(new PortfolioWeightsDataModel
        //        {
        //            PortfolioName = SelectedPortfolioName,
        //            RunTime = DateTime.UtcNow,
        //            TickerName = row.TickerName,
        //            Weight = row.Weight, // null or value
        //        });
        //    }

        //    // Edited rows (includes soft-removed = null)
        //    foreach (var key in _editedTickers)
        //    {
        //        var row = PortfolioWeights.FirstOrDefault(r =>
        //            string.Equals(r.TickerName, key, StringComparison.OrdinalIgnoreCase));
        //        if (row == null) continue;

        //        // If it’s also new, we already included it above
        //        if (_newlyAddedTickers.Contains(key)) continue;

        //        payload.Add(new PortfolioWeightsDataModel
        //        {
        //            PortfolioName = SelectedPortfolioName,
        //            RunTime = DateTime.UtcNow,
        //            TickerName = row.TickerName,
        //            Weight = row.Weight, // could be null (soft remove)
        //        });
        //    }

        //    if (payload.Count == 0)
        //    {
        //        StatusMessage = "No changes to save.";
        //        return;
        //    }

        //    try
        //    {
        //        IsBusy = true;
        //        StatusMessage = "Saving portfolio weights...";

        //        await _wsd.SavePortfolioWeightsAsync(payload);  // 🔁 this calls your Python webservice

        //        // On success, “commit” the new/edited sets into originals
        //        foreach (var r in payload)
        //        {
        //            if (!_originalTickers.Contains(r.TickerName))
        //                _originalTickers.Add(r.TickerName);

        //            _originalWeights[r.TickerName] = r.Weight;
        //            _newlyAddedTickers.Remove(r.TickerName);
        //            _editedTickers.Remove(r.TickerName);

        //            // If you want to clear flags:
        //            var row = PortfolioWeights.FirstOrDefault(x =>
        //                string.Equals(x.TickerName, r.TickerName, StringComparison.OrdinalIgnoreCase));

        //            if (row != null)
        //            {
        //                row.IsNew = false;
        //                row.IsEdited = false;
        //                row.IsOriginal = true;
        //            }
        //        }

        //        StatusMessage = "Portfolio weights saved successfully.";

        //        Application.Current.Dispatcher.Invoke(() =>
        //            MessageBox.Show("Portfolio weights saved successfully.", "Saved",
        //                            MessageBoxButton.OK, MessageBoxImage.Information));
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.WriteLine("Save failed: " + ex.Message);
        //        StatusMessage = "Error saving portfolio weights: " + ex.Message;

        //        Application.Current.Dispatcher.Invoke(() =>
        //            MessageBox.Show("Save failed:\n" + ex.Message, "Error",
        //                            MessageBoxButton.OK, MessageBoxImage.Error));
        //    }
        //    finally
        //    {
        //        IsBusy = false;  // ✅ Progress bar will hide, Save re-enables
        //    }
        //}

        // === Load portfolios (same simple pattern) ===
        public async Task GetPortfolioData()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
                var resp = await client.PostAsync($"{baseUrl}/get_custom_portfolios", content);
                resp.EnsureSuccessStatusCode();

                var json = await resp.Content.ReadAsStringAsync();
                var arr = JArray.Parse(json); // must be array

                PortfolioData.Clear();
                foreach (var t in arr)
                {
                    var item = t.ToObject<PortfolioDataModel>();
                    if (item != null) PortfolioData.Add(item);
                }
            }
            var view = CollectionViewSource.GetDefaultView(PortfolioData);
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription(nameof(PortfolioDataModel.BenchmarkName),
                                                          ListSortDirection.Ascending));
            view.Refresh();
            // no selection on load
            SelectedPortfolioName = null;
            PortfolioWeights.Clear();
            await LoadTickersAsync();
        }

        // === Load weights (STRICT records array) ===
        public async Task LoadPortfolioWeightsDataAsync(string portfolioName)
        {
            if (string.IsNullOrWhiteSpace(portfolioName))
            {
                PortfolioWeights.Clear();
                return;
            }

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                var payload = new { portfolioname = portfolioName };
                var content = new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json");

                var resp = await client.PostAsync($"{baseUrl}/get_portfolio_weights", content);
                resp.EnsureSuccessStatusCode();

                var jsonResponse = await resp.Content.ReadAsStringAsync();

                // DEBUG: see raw payload once
                System.Diagnostics.Debug.WriteLine("weights json: " + jsonResponse);

                var token = JToken.Parse(jsonResponse);

                // If your Python is truly orient="records", token MUST be JArray.
                // Force it, so we catch issues immediately.
                var arr = token as JArray;
                if (arr == null)
                {
                    // If this trips, your server is NOT sending a records array.
                    System.Diagnostics.Debug.WriteLine("weights json is NOT an array. Type: " + token.Type);
                    PortfolioWeights.Clear();
                    return;
                }

                // DEBUG: prove row count we parsed
                System.Diagnostics.Debug.WriteLine("weights array count: " + arr.Count);

                // Clear + Add (same as your FilterIntervals pattern)
                PortfolioWeights.Clear();
                foreach (var row in arr)
                {
                    var item = row.ToObject<PortfolioWeightsDataModel>();
                    if (item != null) PortfolioWeights.Add(item);
                    item.IsOriginal = true;
                    item.IsNew = false;
                    item.IsEdited = false;

                }
                // After you fill PortfolioWeights from the server:
                _originalTickers.Clear();
                _originalWeights.Clear();
                _newlyAddedTickers.Clear();
                _editedTickers.Clear();

                // Subscribe to item changes (to track edits)
                foreach (var row in PortfolioWeights)
                {
                    if (row == null) continue;
                    if (!_originalTickers.Contains(row.TickerName ?? "") && !string.IsNullOrEmpty(row.TickerName))
                    {
                        _originalTickers.Add(row.TickerName);
                    }
                    _originalWeights[row.TickerName ?? ""] = row.Weight;

                    var npc = row as INotifyPropertyChanged;
                    if (npc != null)
                        npc.PropertyChanged += OnWeightRowPropertyChanged;
                }

                // DEBUG: confirm collection count
                System.Diagnostics.Debug.WriteLine("PortfolioWeights items in collection: " + PortfolioWeights.Count);
            }
        }
        

        private void OnWeightRowPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "Weight") return;
            var row = sender as PortfolioWeightsDataModel;
            if (row == null) return;

            var key = row.TickerName ?? "";
            double? original;
            _originalWeights.TryGetValue(key, out original);

            // Normalize NaN ⇒ null
            double? current = row.Weight;
            if (current.HasValue && double.IsNaN(current.Value)) current = null;
            if (original.HasValue && double.IsNaN(original.Value)) original = null;

            bool changed = (original.HasValue != current.HasValue) ||
                           (original.HasValue && current.HasValue && original.Value != current.Value);

            if (changed)
                _editedTickers.Add(key);
            else
                _editedTickers.Remove(key);
            row.IsEdited = changed;
        }

        public async Task LoadTickersAsync()
        {
            // pull every ticker (adjust 'where' if you want a subset)
            var rows = await _wsd.GetTickersAsync();

            // repopulate the observable collection
            Tickers.Clear();
            if (rows != null)
            {
                foreach (var t in rows)
                {
                    // guard against null names just in case
                    if (t != null && !string.IsNullOrWhiteSpace(t.Tickername))
                        Tickers.Add(t);
                }
            }

            // build the view (first-time) or update its sort/filter
            if (FilteredTickers == null)
            {
                FilteredTickers = CollectionViewSource.GetDefaultView(Tickers);
                // alphabetical sort by TickerName
                FilteredTickers.SortDescriptions.Clear();
                FilteredTickers.SortDescriptions.Add(
                    new SortDescription(nameof(TickerDataModel.Tickername), ListSortDirection.Ascending));

                // case-insensitive contains filter based on TickerFilter
                FilteredTickers.Filter = delegate (object item)
                {
                    var row = item as TickerDataModel;
                    if (row == null) return false;
                    if (string.IsNullOrWhiteSpace(TickerFilter)) return true;

                    return row.Tickername != null &&
                           row.Tickername.IndexOf(TickerFilter, StringComparison.InvariantCultureIgnoreCase) >= 0;
                };
            }
            else
            {
                // ensure sort in case it was cleared, then refresh filter
                var view = FilteredTickers;
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(
                    new SortDescription(nameof(TickerDataModel.Tickername), ListSortDirection.Ascending));
                view.Refresh();
            }
        }

    }
}
