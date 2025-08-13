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

namespace wpfTDX
{
    public class FilterIntervalsViewModel
    {

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        private ObservableCollection<FilterIntervalsDataModel> _filterIntervalsData;
        public ObservableCollection<FilterIntervalsDataModel> FilterIntervalsData
        {
            get { return _filterIntervalsData; }
            set
            {
                if (_filterIntervalsData != value)
                { 
                    _filterIntervalsData = value;
                    OnPropertyChanged(nameof(FilterIntervalsData));
                }
            }
        }

        public ObservableCollection<MergedTickerRow> MergedRows { get; } = new ObservableCollection<MergedTickerRow>();


        private ObservableCollection<TadPositionsDataModel> _tadPositionsData;
        public ObservableCollection<TadPositionsDataModel> TadPositionsData
        {
            get { return _tadPositionsData; }
            set
            {
                if (_tadPositionsData != value)
                {
                    _tadPositionsData = value;
                    OnPropertyChanged(nameof(TadPositionsData));
                }
            }
        }
        public FilterIntervalsViewModel()
        {
            FilterIntervalsData = new ObservableCollection<FilterIntervalsDataModel>();
            TadPositionsData = new ObservableCollection<TadPositionsDataModel>();
        }
        public async Task LoadFilterIntervalsDataAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                var requestData = new { }; // No payload needed if your endpoint accepts empty POST
                string jsonRequest = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync("http://localhost:5001/filtered_intervals", content);
                response.EnsureSuccessStatusCode();

                string jsonResponse = await response.Content.ReadAsStringAsync();

                var fullResponse = JObject.Parse(jsonResponse);
                var filterIntervalsJson = fullResponse["filter_intervals"];

                // Deserialize into a Dictionary<string, FilterIntervalsDataModel>
                var dict = filterIntervalsJson.ToObject<Dictionary<string, FilterIntervalsDataModel>>();

                FilterIntervalsData.Clear();
                foreach (var kvp in dict)
                {
                    var item = kvp.Value;
                    item.TickerName = kvp.Key; // Add index (key) as ticker if needed
                    FilterIntervalsData.Add(item);
                }
            }
        }

        public async Task LoadTadPositionsDataAsync()
        {
            var client = new HttpClient();

            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            var response = await client.PostAsync("http://localhost:5001/filtered_intervals", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var root = JObject.Parse(json);

            var positionsJson = (JObject)root["positions"];
            var viewPositionsJson = (JObject)root["viewpositions"];
            var filterPositionsJson = (JObject)root["filterpositions"];

            // Work dict keyed by ticker (change to (ticker,fundgroup) later if needed)
            var merged = new Dictionary<string, TadPositionsDataModel>(StringComparer.OrdinalIgnoreCase);

            // helper local function to get-or-create
            TadPositionsDataModel GetOrCreate(string ticker)
            {
                if (!merged.TryGetValue(ticker, out var m))
                {
                    m = new TadPositionsDataModel { Tickername = ticker };
                    merged[ticker] = m;
                }
                return m;
            }

            // 1) positions
            if (positionsJson != null)
            {
                foreach (var kv in positionsJson) // kv.Key = ticker, kv.Value = row object
                {
                    var ticker = kv.Key;
                    var row = (JObject)kv.Value;
                    var model = GetOrCreate(ticker);

                    // Map fields you expect (null-safe conversions)
                    model.PositionBaseY1 = row.Value<float?>("position_base_y1");
                    model.PositionD1 = row.Value<float?>("position_D1");
                    model.PositionDeployment = row.Value<float?>("deployment") ?? 0;
                    model.PositionNumTrades = row.Value<float?>("num_trades") ?? 0;
                    model.PositionNumIntervals = row.Value<float?>("num_posintervals") ?? 0;
                }
            }

            // 2) viewpositions
            if (viewPositionsJson != null)
            {
                foreach (var kv in viewPositionsJson)
                {
                    var ticker = kv.Key;
                    var row = (JObject)kv.Value;
                    var model = GetOrCreate(ticker);

                    model.NumViewTrades = row.Value<float?>("num_view_trades") ?? 0;
                }
            }

            // 3) filterpositions
            if (filterPositionsJson != null)
            {
                foreach (var kv in filterPositionsJson)
                {
                    var ticker = kv.Key;
                    var row = (JObject)kv.Value;
                    var model = GetOrCreate(ticker);

                    model.NumFilteredTrades = row.Value<float?>("num_filtered_trades") ?? 0;
                    model.NumFiltIntervals = row.Value<int?>("num_filtintervals") ?? 0;
                    model.FilteredDeployment = row.Value<float?>("filtered_deployment") ?? 0;
                }
            }

            // Push into your ObservableCollection
            TadPositionsData.Clear();
            foreach (var m in merged.Values.OrderBy(x => x.Tickername))
                TadPositionsData.Add(m);
        }
        public async Task RebuildMerged()
        {
            MergedRows.Clear();

            var fiByTicker = (FilterIntervalsData ?? new ObservableCollection<FilterIntervalsDataModel>())
                .GroupBy(x => x.TickerName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var tp in TadPositionsData) // left-join: every tad row shows up
            {
                fiByTicker.TryGetValue(tp.Tickername, out var fi);

                var row = new MergedTickerRow
                {
                    Tickername = tp.Tickername,
                    // FilterIntervals fields (null if missing)
                    Runtime = fi.Runtime,
                    FundGroup = fi?.FundGroup,
                    Rescale = fi?.Rescale,
                    LongOnly = fi?.LongOnly,
                    ShortOnly = fi?.ShortOnly,
                    BuyOnly = fi?.BuyOnly,
                    SellOnly = fi?.SellOnly,
                    AllIntervals = fi?.AllIntervals,
                    BaseY1 = fi?.BaseY1,
                    PositionBaseY1 = tp.PositionBaseY1, // from TadPositionsDataModel
                    BaseH1 = fi?.BaseH1,
                    PositionBaseH1 = tp.PositionBaseH1, // from TadPositionsDataModel
                    PositionD1 = tp.PositionD1, // from TadPositionsDataModel
                    BaseD1 = fi?.BaseD1,
                    PositionBaseD1 = tp.PositionBaseD1, // from TadPositionsDataModel
                    y1 = fi?.Y1,
                    PositionY1 = tp.PositionBaseY1, // from TadPositionsDataModel
                    y2 = fi?.Y2,
                    y3 = fi?.Y3,
                    h1 = fi?.H1,
                    h2 = fi?.H2,
                    h3 = fi?.H3,
                    h4 = fi?.H4,
                    h5 = fi?.H5,
                    h6 = fi?.H6,
                    h12 = fi?.H12,
                    h16 = fi?.H16,
                    D1 = fi?.D1,
                    h36 = fi?.H36,
                    D2 = fi?.D2,
                    D3 = fi?.D3,
                    D4 = fi?.D4,
                    W1 = fi?.W1,
                    D8 = fi?.D8,
                    W2 = fi?.W2,

                    // TadPositions fields
                    NumFilteredTrades = tp.NumFilteredTrades,
                    NumFiltIntervals = tp.NumFiltIntervals,
                    FilteredDeployment = tp.FilteredDeployment,
                    NumTrades = tp.PositionNumTrades,
                    NumPositionIntervals = tp.PositionNumIntervals,
                    ViewDeployment = tp.ViewDeployment,
                    PositionDeployment = tp.PositionDeployment,
                };

                // 🔸 take the baseline after assigning server values
                row.SnapshotOriginals();

                MergedRows.Add(row);
            }

            // (optional) include tickers that exist only in FilterIntervals
            foreach (var fiOnly in fiByTicker.Values
                         .Where(fi => !TadPositionsData.Any(tp =>
                                string.Equals(tp.Tickername, fi.TickerName, StringComparison.OrdinalIgnoreCase))))
            {
                var row = new MergedTickerRow
                {
                    Tickername = fiOnly.TickerName,
                    Runtime = fiOnly.Runtime,
                    Rescale = fiOnly.Rescale,
                    LongOnly = fiOnly.LongOnly,
                    // …any other FI-only fields you want
                };

                row.SnapshotOriginals();
                MergedRows.Add(row);
            }
        }

        public class MergedTickerRow : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string p = null)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
            private bool _isInitialized;
            public string Tickername { get; set; }
            public string FundGroup { get; set; }
            public bool? OriginalRescale { get; set; }
            private bool? _rescale;
            public bool? Rescale 
            { get => _rescale; 
              set
              {
                  if (_rescale != value)
                  {
                      _rescale = value;
                      OnPropertyChanged(nameof(Rescale));
                        OnPropertyChanged(nameof(RescaleHasChanged));
                    }
              } 
            
            }
            public bool RescaleHasChanged => _rescale != OriginalRescale;
            public bool? OriginalLongOnly { get; private set; }
            private bool? _longOnly;
            public bool? LongOnly
            {
                get => _longOnly;
                set
                {
                    if (_longOnly != value)
                    {
                        _longOnly = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(LongOnlyHasChanged));
                    }
                }
            }
            public bool LongOnlyHasChanged => _isInitialized && _longOnly != OriginalLongOnly;

            public bool? OriginalShortOnly { get; private set; }
            private bool? _shortOnly;
            public bool? ShortOnly
            {
                get => _shortOnly;
                set
                {
                    if (_shortOnly != value)
                    {
                        _shortOnly = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(ShortOnlyHasChanged));
                    }
                }
            }
            public bool ShortOnlyHasChanged => _isInitialized && _shortOnly != OriginalShortOnly;

            public bool? OriginalBuyOnly { get; private set; }
            private bool? _buyOnly;
            public bool? BuyOnly
            {
                get => _buyOnly;
                set
                {
                    if (_buyOnly != value)
                    {
                        _buyOnly = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(BuyOnlyHasChanged));
                    }
                }
            }
            public bool BuyOnlyHasChanged => _isInitialized && _buyOnly != OriginalBuyOnly;

            public bool? OriginalSellOnly { get; private set; }
            private bool? _sellOnly;
            public bool? SellOnly
            {
                get => _sellOnly;
                set
                {
                    if (_sellOnly != value)
                    {
                        _sellOnly = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(SellOnlyHasChanged));
                    }
                }
            }
            public bool SellOnlyHasChanged => _isInitialized && _sellOnly != OriginalSellOnly;

            public bool? OriginalAllIntervals { get; private set; }
            private bool? _allIntervals;
            public bool? AllIntervals
            {
                get => _allIntervals;
                set
                {
                    if (_allIntervals != value)
                    {
                        _allIntervals = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(AllIntervalsHasChanged));
                    }
                }
            }
            public bool AllIntervalsHasChanged => _isInitialized && _allIntervals != OriginalAllIntervals;

            public bool? OriginalBaseY1 { get; private set; }
            private bool? _baseY1;
            public bool? BaseY1
            {
                get => _baseY1;
                set
                {
                    if (_baseY1 != value)
                    {
                        _baseY1 = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(BaseY1HasChanged));
                    }
                }
            }
            public bool BaseY1HasChanged => _isInitialized && _baseY1 != OriginalBaseY1;
            public float? PositionBaseY1 { get; set; } // from TadPositionsDataModel

            public bool? OriginalBaseH1 { get; private set; }
            private bool? _baseH1;
            public bool? BaseH1
            {
                get => _baseH1;
                set
                {
                    if (_baseH1 != value)
                    {
                        _baseH1 = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(BaseH1HasChanged));
                    }
                }
            }
            public bool BaseH1HasChanged => _isInitialized && _baseH1 != OriginalBaseH1;
            public float? PositionBaseH1 { get; set; } // from TadPositionsDataModel

            public bool? OriginalBaseD1 { get; private set; }
            private bool? _baseD1;
            public bool? BaseD1
                {
                get => _baseD1;
                set
                {
                    if (_baseD1 != value)
                    {
                        _baseD1 = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(BaseD1HasChanged));
                    }
                }
            }
            public bool BaseD1HasChanged => _isInitialized && _baseD1 != OriginalBaseD1;
            public float? PositionBaseD1 { get; set; } // from TadPositionsDataModel

            public bool? OriginalY1 { get; private set; }
            private bool? _y1;
            public bool? y1
                {
                get => _y1;
                set
                {
                    if (_y1 != value)
                    {
                        _y1 = value;
                        OnPropertyChanged(nameof(y1));
                        OnPropertyChanged(nameof(Y1HasChanged));
                    }
                }
            }
            public bool Y1HasChanged => _isInitialized && _y1 != OriginalY1;
            public float? PositionY1 { get; set; } // from TadPositionsDataModel

            public bool? OriginalY2 { get; private set; }
            private bool? _y2;
            public bool? y2
            {
                get => _y2;
                set
                {
                    if (_y2 != value)
                    {
                        _y2 = value;
                        OnPropertyChanged(nameof(y2));
                        OnPropertyChanged(nameof(Y2HasChanged));
                    }
                }
            }
            public bool Y2HasChanged => _isInitialized && _y2 != OriginalY2;
            public float? PositionY2 { get; set; } // from TadPositionsDataModel

            public bool? y3 { get; set; }
            public bool? h1 { get; set; }
            public bool? h2 { get; set; }
            public bool? h3 { get; set; }
            public  bool? h4 { get; set; }
            public bool? h5 { get; set; }
            public bool? h6 { get; set; }
            public bool? h12 { get; set; }
            public bool? h16 { get; set; }
            public float? PositionD1 { get; set; } // from TadPositionsDataModel

            public bool? OrginalD1 { get; private set; }    
            private bool? _D1;
            public bool? D1
            {
                get => _D1;
                set
                {
                    if (_D1 != value)
                    {
                        _D1 = value;
                        OnPropertyChanged(nameof(D1));
                        OnPropertyChanged(nameof(D1HasChanged));
                    }
                }
            }
            public bool D1HasChanged => _isInitialized && _D1 != OrginalD1;


            public bool? h36 { get; set; }
            public bool? D2 { get; set; }
            public bool? D3 { get; set; }
            public bool? D4 { get; set; }
            public bool? W1 { get; set; }
            public bool? D8 { get; set; }
            public bool? W2 { get; set; }
            // from FilterIntervalsDataModel (rename types/props to yours)
            public DateTime Runtime { get; set; }
            //public int? NumViews { get; set; }           

            //// from TadPositionsDataModel
            public float? NumTrades { get; set; }
            public float? NumPositionIntervals { get; set; }
            public float? NumFiltIntervals { get; set; } 
            public float? NumFilteredTrades { get; set; }
            public float? FilteredDeployment { get; set; }
            public float? ViewDeployment { get; set; }
            public float? PositionDeployment { get; set; }


            public void SnapshotOriginals()
            {
                OriginalRescale = _rescale;
                OriginalLongOnly = _longOnly;
                OriginalShortOnly = _shortOnly;
                OriginalBuyOnly = _buyOnly;
                OriginalSellOnly = _sellOnly;
                OriginalAllIntervals = _allIntervals;
                OriginalBaseY1 = _baseY1;
                OrginalD1 = _D1;
                OriginalBaseH1 = _baseH1;
                OriginalBaseD1 = _baseD1;
                OriginalY1 = _y1;
                OriginalY2 = _y2;
                // (repeat for other tracked fields)

                _isInitialized = true;

                // Notify HasChanged props so the UI refreshes to "not bold on load"
                OnPropertyChanged(nameof(RescaleHasChanged));
                OnPropertyChanged(nameof(LongOnlyHasChanged));
                OnPropertyChanged(nameof(ShortOnlyHasChanged));
                OnPropertyChanged(nameof(BuyOnlyHasChanged));
                OnPropertyChanged(nameof(SellOnlyHasChanged));
                OnPropertyChanged(nameof(AllIntervalsHasChanged));
                OnPropertyChanged(nameof(BaseY1HasChanged));
                OnPropertyChanged(nameof(D1HasChanged));
                OnPropertyChanged(nameof(BaseH1HasChanged));
                OnPropertyChanged(nameof(BaseD1HasChanged));
                OnPropertyChanged(nameof(Y1HasChanged));
                OnPropertyChanged(nameof(Y2HasChanged));
                // (repeat for others)
            }
        }

    }
}
