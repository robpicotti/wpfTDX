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
                    model.PositionBaseH1 = row.Value<float?>("position_base_h1");
                    model.PositionBaseD1 = row.Value<float?>("position_base_d1");
                    model.PositionY1 = row.Value<float?>("position_y1");
                    model.PositionY2 = row.Value<float?>("position_y2");
                    model.PositionY3 = row.Value<float?>("position_y3");
                    model.PositionH2 = row.Value<float?>("position_h2");
                    model.PositionH3 = row.Value<float?>("position_h3");
                    model.PositionH4 = row.Value<float?>("position_h4");
                    model.PositionH5 = row.Value<float?>("position_h5");
                    model.PositionH6 = row.Value<float?>("position_h6");
                    model.PositionH12 = row.Value<float?>("position_h12");
                    model.PositionH16 = row.Value<float?>("position_h16");
                    model.PositionD1 = row.Value<float?>("position_D1");
                    model.PositionH36 = row.Value<float?>("position_h36");
                    model.PositionD2 = row.Value<float?>("position_D2");
                    model.PositionD3 = row.Value<float?>("position_D3");
                    model.PositionD4 = row.Value<float?>("position_D4");
                    model.PositionW1 = row.Value<float?>("position_W1");
                    model.PositionD8 = row.Value<float?>("position_D8");
                    model.PositionW2 = row.Value<float?>("position_W2");
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
                    PositionY1 = tp.PositionY1, // from TadPositionsDataModel
                    y2 = fi?.Y2,
                    PositionY2 = tp.PositionY2, // from TadPositionsDataModel
                    y3 = fi?.Y3,
                    PositionY3 = tp.PositionY3,
   
                    H2 = fi?.H2,
                    PositionH2 = tp.PositionH2, // from TadPositionsDataModel

                    H3 = fi?.H3,
                    PositionH3 = tp.PositionH3, // from TadPositionsDataModel
                    H4 = fi?.H4,
                    PositionH4 = tp.PositionH4, // from TadPositionsDataModel
                    H5 = fi?.H5,
                    PositionH5 = tp.PositionH5, // from TadPositionsDataModel
                    H6 = fi?.H6,
                    PositionH6 = tp.PositionH6, // from TadPositionsDataModel
                    H12 = fi?.H12,
                    PositionH12 = tp.PositionH12, // from TadPositionsDataModel
                    H16 = fi?.H16,
                    PositionH16 = tp.PositionH16, // from TadPositionsDataModel
                    D1 = fi?.D1,
                    H36 = fi?.H36,
                    PositionH36 = tp.PositionH36, // from TadPositionsDataModel
                    D2 = fi?.D2,
                    PositionD2 = tp.PositionD2, // from TadPositionsDataModel
                    D3 = fi?.D3,
                    PositionD3 = tp.PositionD3, // from TadPositionsDataModel
                    D4 = fi?.D4,
                    PositionD4 = tp.PositionD4, // from TadPositionsDataModel
                    W1 = fi?.W1,
                    PositionW1 = tp.PositionW1, // from TadPositionsDataModel
                    D8 = fi?.D8,
                    PositionD8 = tp.PositionD8, // from TadPositionsDataModel
                    W2 = fi?.W2,
                    PositionW2 = tp.PositionW2, // from TadPositionsDataModel

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

            public bool? OriginalY3 { get; private set; }
            private bool? _y3;
            public bool? y3
            {
                get => _y3;
                set
                {
                    if (_y3 != value)
                    {
                        _y3 = value;
                        OnPropertyChanged(nameof(y3));
                        OnPropertyChanged(nameof(Y3HasChanged));
                    }
                }
            }
            public bool Y3HasChanged => _isInitialized && _y3 != OriginalY3;
            public float? PositionY3 { get; set; } // from TadPositionsDataModel

            public bool? OriginalH2 { get; private set; }
            private bool? _h2;
            public bool? H2
                {
                get => _h2;
                set
                {
                    if (_h2 != value)
                    {
                        _h2 = value;
                        OnPropertyChanged(nameof(H2));
                        OnPropertyChanged(nameof(H2HasChanged));
                    }
                }
            }
            public bool H2HasChanged => _isInitialized && _h2 != OriginalH2;
            public float? PositionH2 { get; set; } // from TadPositionsDataModel

            public bool? OriginalH3 { get; private set; }
            private bool? _h3;
            public bool? H3
            {
                get => _h3;
                set
                {
                    if (_h3 != value)
                    {
                        _h3 = value;
                        OnPropertyChanged(nameof(H3));
                        OnPropertyChanged(nameof(H3HasChanged));
                    }
                }
            }
            public bool H3HasChanged => _isInitialized && _h3 != OriginalH3;
            public float? PositionH3 { get; set; } // from TadPositionsDataModel

            public bool? OriginalH4 { get; private set; }
            private bool? _h4;
            public bool? H4
            {
                get => _h4;
                set
                {
                    if (_h4 != value)
                    {
                        _h4 = value;
                        OnPropertyChanged(nameof(H4));
                        OnPropertyChanged(nameof(H4HasChanged));
                    }
                }
            }
            public bool H4HasChanged => _isInitialized && _h4 != OriginalH4;
            public float? PositionH4 { get; set; } // from TadPositionsDataModel

            public bool? OriginalH5 { get; private set; }
            private bool? _h5;
            public bool? H5
                {
                get => _h5;
                set
                {
                    if (_h5 != value)
                    {
                        _h5 = value;
                        OnPropertyChanged(nameof(H5));
                        OnPropertyChanged(nameof(H5HasChanged));
                    }
                }
            }
            public bool H5HasChanged => _isInitialized && _h5 != OriginalH5;
            public float? PositionH5 { get; set; } // from TadPositionsDataModel


            public bool? OriginalH6 { get; private set; }
            private bool? _h6;
            public bool? H6
            {
                get => _h6;
                set
                {
                    if (_h6 != value)
                    {
                        _h6 = value;
                        OnPropertyChanged(nameof(H6));
                        OnPropertyChanged(nameof(H6HasChanged));
                    }
                }
            }
            public bool H6HasChanged => _isInitialized && _h6 != OriginalH6;
            public float? PositionH6 { get; set; } // from TadPositionsDataModel

            public bool? OriginalH12 { get; private set; }
            private bool? _h12;
            public bool? H12
            {
                get => _h12;
                set
                {
                    if (_h12 != value)
                    {
                        _h12 = value;
                        OnPropertyChanged(nameof(H12));
                        OnPropertyChanged(nameof(H12HasChanged));
                    }
                }
            }
            public bool H12HasChanged => _isInitialized && _h12 != OriginalH12;
            public float? PositionH12 { get; set; } // from TadPositionsDataModel


            public bool? OriginalH16 { get; private set; }
            private bool? _h16;
            public bool? H16
            {
                get => _h16;
                set
                {
                    if (_h16 != value)
                    {
                        _h16 = value;
                        OnPropertyChanged(nameof(H16));
                        OnPropertyChanged(nameof(H16HasChanged));
                    }
                }
            }
            public bool H16HasChanged => _isInitialized && _h16 != OriginalH16;
            public float? PositionH16 { get; set; } // from TadPositionsDataModel


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


            public bool? Originalh36 { get; private set; }
            private bool? _h36;
            public bool? H36
            {                 get => _h36;
                set
                {
                    if (_h36 != value)
                    {
                        _h36 = value;
                        OnPropertyChanged(nameof(H36));
                        OnPropertyChanged(nameof(H36HasChanged));
                    }
                }
            }
            public bool H36HasChanged => _isInitialized && _h36 != Originalh36;
            public float? PositionH36 { get; set; } // from TadPositionsDataModel

            public bool? OriginalD2 { get; private set; }
            private bool? _D2;
            public bool? D2
            {
                get => _D2;
                set
                {
                    if (_D2 != value)
                    {
                        _D2 = value;
                        OnPropertyChanged(nameof(D2));
                        OnPropertyChanged(nameof(D2HasChanged));
                    }
                }
            }
            public bool D2HasChanged => _isInitialized && _D2 != OriginalD2;
            public float? PositionD2 { get; set; } // from TadPositionsDataModel

            public bool? OriginalD3 { get; private set; }
            private bool? _D3;
            public bool? D3
            {
                get => _D3;
                set
                {
                    if (_D3 != value)
                    {
                        _D3 = value;
                        OnPropertyChanged(nameof(D3));
                        OnPropertyChanged(nameof(D3HasChanged));
                    }
                }
            }
            public bool D3HasChanged => _isInitialized && _D3 != OriginalD3;
            public float? PositionD3 { get; set; } // from TadPositionsDataModel


            public bool? OriginalD4 { get; private set; }
            private bool? _D4;
            public bool? D4
            {
                get => _D4;
                set
                {
                    if (_D4 != value)
                    {
                        _D4 = value;
                        OnPropertyChanged(nameof(D4));
                        OnPropertyChanged(nameof(D4HasChanged));
                    }
                }
            }   
            public bool D4HasChanged => _isInitialized && _D4 != OriginalD4;
            public float? PositionD4 { get; set; } // from TadPositionsDataModel

            public bool? OriginalW1 { get; private set; }
            private bool? _W1;
            public bool? W1
            {
                get => _W1;
                set
                {
                    if (_W1 != value)
                    {
                        _W1 = value;
                        OnPropertyChanged(nameof(W1));
                        OnPropertyChanged(nameof(W1HasChanged));
                    }
                }
            }
            public bool W1HasChanged => _isInitialized && _W1 != OriginalW1;
            public float? PositionW1 { get; set; } // from TadPositionsDataModel


            public bool? OriginalD8 { get; private set; }
            private bool? _D8;
            public bool? D8
            {
                get => _D8;
                set
                {
                    if (_D8 != value)
                    {
                        _D8 = value;
                        OnPropertyChanged(nameof(D8));
                        OnPropertyChanged(nameof(D8HasChanged));
                    }
                }
            }
            public bool D8HasChanged => _isInitialized && _D8 != OriginalD8;
            public float? PositionD8 { get; set; } // from TadPositionsDataModel

            public bool? OriginalW2 { get; private set; }
            private bool? _W2;
            public bool? W2
            {
                get => _W2;
                set
                {
                    if (_W2 != value)
                    {
                        _W2 = value;
                        OnPropertyChanged(nameof(W2));
                        OnPropertyChanged(nameof(W2HasChanged));
                    }
                }
            }
            public bool W2HasChanged => _isInitialized && _W2 != OriginalW2;
            public float? PositionW2 { get; set; } // from TadPositionsDataModel

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
                OriginalY3 = _y3;
                OriginalH2 = _h2;
                OriginalH3 = _h3;
                OriginalH4 = _h4;
                OriginalH5 = _h5;
                OriginalH6 = _h6;
                OriginalH12 = _h12;
                OriginalH16 = _h16;
                Originalh36 = _h36;
                OriginalD2 = _D2;
                OriginalD3 = _D3;
                OriginalD4 = _D4;
                OriginalW1 = _W1;
                OriginalW2 = _W2;
                OriginalD8 = _D8;



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
                OnPropertyChanged(nameof(Y3HasChanged));
                OnPropertyChanged(nameof(H2HasChanged));
                OnPropertyChanged(nameof(H3HasChanged));
                OnPropertyChanged(nameof(H4HasChanged));
                OnPropertyChanged(nameof(H5HasChanged));
                OnPropertyChanged(nameof(H6HasChanged));
                OnPropertyChanged(nameof(H12HasChanged));
                OnPropertyChanged(nameof(H16HasChanged));
                OnPropertyChanged(nameof(H36HasChanged));
                OnPropertyChanged(nameof(D2HasChanged));
                OnPropertyChanged(nameof(D3HasChanged));
                OnPropertyChanged(nameof(D4HasChanged));
                OnPropertyChanged(nameof(W1HasChanged));
                OnPropertyChanged(nameof(D8HasChanged));
                OnPropertyChanged(nameof(W2HasChanged));

                // (repeat for others)
            }
        }

    }
}
