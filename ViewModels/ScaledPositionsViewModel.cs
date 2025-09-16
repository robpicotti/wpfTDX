using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace wpfTDX
{
    public sealed class ScaledPositionsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string p = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(p));
        }

        public ObservableCollection<ScaledPositionsDataModel> Rows { get; } =
            new ObservableCollection<ScaledPositionsDataModel>();

        public ICollectionView RowsView { get; }

        public ScaledPositionsViewModel()
        {
            RowsView = CollectionViewSource.GetDefaultView(Rows);
            if (RowsView != null) RowsView.Filter = FilterRow;
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get { return _isLoading; }
            private set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        public string StatusText
        {
            get { return IsLoading ? "Loading…" : "Rows: " + Rows.Count; }
        }

        private string _fundGroupFilter;
        public string FundGroupFilter
        {
            get { return _fundGroupFilter; }
            set
            {
                if (_fundGroupFilter != value)
                {
                    _fundGroupFilter = value;
                    if (RowsView != null) RowsView.Refresh();
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        private string _fundNameFilter;
        public string FundNameFilter
        {
            get { return _fundNameFilter; }
            set
            {
                if (_fundNameFilter != value)
                {
                    _fundNameFilter = value;
                    if (RowsView != null) RowsView.Refresh();
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        private string _tickerFilter;
        public string TickerFilter
        {
            get { return _tickerFilter; }
            set
            {
                if (_tickerFilter != value)
                {
                    _tickerFilter = value;
                    if (RowsView != null) RowsView.Refresh();
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        private static bool Contains(string hay, string needle)
        {
            if (string.IsNullOrWhiteSpace(needle)) return true;
            return (hay ?? string.Empty).IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool FilterRow(object o)
        {
            var r = o as ScaledPositionsDataModel;
            if (r == null) return false;

            return Contains(r.FundGroupName, FundGroupFilter)
                && Contains(r.FundName, FundNameFilter)
                && Contains(r.TickerName, TickerFilter);
        }

        public async Task LoadAsync()
        {
            try
            {
                IsLoading = true;
                var list = await FetchScaledPositionsAsync();

                Rows.Clear();
                foreach (var row in list.OrderBy(x => x.FundGroupName)
                                        .ThenBy(x => x.FundName)
                                        .ThenBy(x => x.TickerName))
                {
                    Rows.Add(row);
                }

                if (RowsView != null) RowsView.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Load Scaled Positions", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private static async Task<ScaledPositionsDataModel[]> FetchScaledPositionsAsync()
        {
            var client = new HttpClient();
            try
            {
                var content = new StringContent("{}", Encoding.UTF8, "application/json");
                var response = await client.PostAsync("http://localhost:5001/get_scaled_positions", content);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var token = JToken.Parse(json); // root IS the scaled positions payload

                var bag = new List<ScaledPositionsDataModel>();

                if (token.Type == JTokenType.Array)
                {
                    foreach (var t in (JArray)token)
                    {
                        var obj = t as JObject;
                        if (obj == null) continue;

                        var it = obj.ToObject<ScaledPositionsDataModel>();
                        if (it == null) it = new ScaledPositionsDataModel();

                        ApplyFallbacks(it, obj, null);
                        bag.Add(it);
                    }
                }
                else if (token.Type == JTokenType.Object)
                {
                    foreach (var kv in (JObject)token)
                    {
                        var obj = kv.Value as JObject;
                        if (obj == null) continue;

                        var it = obj.ToObject<ScaledPositionsDataModel>();
                        if (it == null) it = new ScaledPositionsDataModel();

                        ApplyFallbacks(it, obj, kv.Key);
                        bag.Add(it);
                    }
                }

                return bag.ToArray();
            }
            finally
            {
                client.Dispose();
            }
        }


        // moved out of local function; no '??='; trim via 7.3-safe code
        private static void ApplyFallbacks(ScaledPositionsDataModel it, JObject src, string tickerKeyIfAny)
        {
            if (string.IsNullOrWhiteSpace(it.TickerName))
            {
                var tk = tickerKeyIfAny ?? src.Value<string>("tickername");
                it.TickerName = tk;
            }

            if (string.IsNullOrEmpty(it.FundGroupName))
                it.FundGroupName = src.Value<string>("fundgroupname");

            if (string.IsNullOrEmpty(it.FundName))
                it.FundName = src.Value<string>("fundname");

            if (string.IsNullOrEmpty(it.ScaleType))
                it.ScaleType = src.Value<string>("scale_type") ?? src.Value<string>("scaled_type");

            if (!it.ScaledPercent.HasValue)
            {
                var sp = src.Value<double?>("scaled_position");
                if (!sp.HasValue) sp = src.Value<double?>("scale_factor"); // accept either name
                it.ScaledPercent = sp;
            }

            if (!it.ScaledTarget.HasValue)
                it.ScaledTarget = src.Value<double?>("scaled_target");

            it.FundGroupName = it.FundGroupName != null ? it.FundGroupName.Trim() : null;
            it.FundName = it.FundName != null ? it.FundName.Trim() : null;
            it.TickerName = it.TickerName != null ? it.TickerName.Trim() : null;
        }
    }
}
