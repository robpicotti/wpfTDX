using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TDX;

namespace wpfTDX
{
    public class TradeHistoryViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly SqlConnection _sqlConn;
        private readonly TDX.db _db = new TDX.db();

        public TradeHistoryViewModel(SqlConnection conn)
        {
            _sqlConn = conn;

            TargetPositions = new ObservableCollection<TargetPositionsDataModel>();
            LivePositions = new ObservableCollection<LivePositionsDataModel>();
            TadIds = new ObservableCollection<TadIdItem>();
            ChartMetrics = new ObservableCollection<ChartMetricItem>();

            BuildDefaultChartMetrics();
        }

        private void BuildDefaultChartMetrics()
        {
            ChartMetrics.Clear();

            ChartMetrics.Add(new ChartMetricItem("position_live", "Position Live", true));
            ChartMetrics.Add(new ChartMetricItem("position_limit", "Position Limit", false));
            ChartMetrics.Add(new ChartMetricItem("position_target", "Position Target", false));
            ChartMetrics.Add(new ChartMetricItem("deployment", "Deployment", false));
            ChartMetrics.Add(new ChartMetricItem("deployment_tad", "Deployment TAD", false));
            ChartMetrics.Add(new ChartMetricItem("position_target_raw", "Position Target Raw", false));
            ChartMetrics.Add(new ChartMetricItem("scaled_percent", "Scaled %", false));

            for (int i = 0; i < ChartMetrics.Count; i++)
            {
                ChartMetrics[i].PropertyChanged += ChartMetricItem_PropertyChanged;
            }
        }

        private void ChartMetricItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsChecked")
            {
                OnPropertyChanged(nameof(CheckedChartMetrics));
            }
        }

        public IEnumerable<ChartMetricItem> CheckedChartMetrics
        {
            get { return ChartMetrics.Where(x => x.IsChecked); }
        }

        private string _fundName;
        public string FundName
        {
            get { return _fundName; }
            set
            {
                if (_fundName != value)
                {
                    _fundName = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _tadId;
        public string TadId
        {
            get { return _tadId; }
            set
            {
                if (_tadId != value)
                {
                    _tadId = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<TadIdItem> TadIds { get; private set; }

        private TadIdItem _selectedTadItem;
        public TadIdItem SelectedTadItem
        {
            get { return _selectedTadItem; }
            set
            {
                if (_selectedTadItem != value)
                {
                    _selectedTadItem = value;

                    if (_selectedTadItem != null)
                    {
                        TadId = _selectedTadItem.TadId;
                        TickerName = _selectedTadItem.TickerName;
                    }

                    OnPropertyChanged();
                }
            }
        }

        private string _tickerName;
        public string TickerName
        {
            get { return _tickerName; }
            set
            {
                if (_tickerName != value)
                {
                    _tickerName = value;
                    OnPropertyChanged();
                }
            }
        }

        private DateTime _positionDateFrom;
        public DateTime PositionDateFrom
        {
            get { return _positionDateFrom; }
            set
            {
                if (_positionDateFrom != value)
                {
                    _positionDateFrom = value;
                    OnPropertyChanged();
                }
            }
        }

        private DateTime _positionDateTo;
        public DateTime PositionDateTo
        {
            get { return _positionDateTo; }
            set
            {
                if (_positionDateTo != value)
                {
                    _positionDateTo = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _lastRunTime;
        public string LastRunTime
        {
            get { return _lastRunTime; }
            set
            {
                if (_lastRunTime != value)
                {
                    _lastRunTime = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isExecuting;
        public bool IsExecuting
        {
            get { return _isExecuting; }
            set
            {
                if (_isExecuting != value)
                {
                    _isExecuting = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<TargetPositionsDataModel> TargetPositions { get; private set; }
        public ObservableCollection<LivePositionsDataModel> LivePositions { get; private set; }
        public ObservableCollection<ChartMetricItem> ChartMetrics { get; private set; }

        private TargetPositionsDataModel _selectedTargetPosition;
        public TargetPositionsDataModel SelectedTargetPosition
        {
            get { return _selectedTargetPosition; }
            set
            {
                if (_selectedTargetPosition != value)
                {
                    _selectedTargetPosition = value;
                    OnPropertyChanged();
                }
            }
        }

        private LivePositionsDataModel _selectedLivePosition;
        public LivePositionsDataModel SelectedLivePosition
        {
            get { return _selectedLivePosition; }
            set
            {
                if (_selectedLivePosition != value)
                {
                    _selectedLivePosition = value;
                    OnPropertyChanged();
                }
            }
        }

        public async Task LoadTadIdsAsync()
        {
            if (string.IsNullOrWhiteSpace(FundName))
                return;

            IsExecuting = true;
            try
            {
                List<TadIdItem> tadIds = await Task.Run(() =>
                {
                    string sql =
                        "select distinct tad_id, tickername " +
                        "from live_positions " +
                        "where fundname = '" + EscapeSql(FundName) + "' " +
                        "and tad_id is not null " +
                        "order by tad_id";

                    DataTable dt = _db.execSQL(sql, _sqlConn);

                    List<TadIdItem> outList = new List<TadIdItem>();
                    foreach (DataRow row in dt.Rows)
                    {
                        string tadId = SafeString(row, "tad_id");
                        string tickerName = SafeString(row, "tickername");

                        if (!string.IsNullOrWhiteSpace(tadId))
                        {
                            outList.Add(new TadIdItem
                            {
                                TadId = tadId,
                                TickerName = tickerName
                            });
                        }
                    }

                    return outList;
                });

                TadIds.Clear();
                for (int i = 0; i < tadIds.Count; i++)
                    TadIds.Add(tadIds[i]);

                if (!string.IsNullOrWhiteSpace(TadId))
                {
                    // Match on both TadId + TickerName first
                    TadIdItem existing = TadIds.FirstOrDefault(x =>
                        string.Equals(x.TadId, TadId, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(x.TickerName, TickerName, StringComparison.OrdinalIgnoreCase));

                    // Fallback to TadId only
                    if (existing == null)
                        existing = TadIds.FirstOrDefault(x =>
                            string.Equals(x.TadId, TadId, StringComparison.OrdinalIgnoreCase));

                    if (existing != null)
                        SelectedTadItem = existing;
                }
                else if (TadIds.Count > 0)
                {
                    SelectedTadItem = TadIds[0];
                }
            }
            finally
            {
                IsExecuting = false;
            }
        }

        public async Task LoadAllDataAsync()
        {
            if (string.IsNullOrWhiteSpace(FundName))
                throw new Exception("FundName is empty.");

            if (string.IsNullOrWhiteSpace(TadId))
                throw new Exception("TadId is empty.");

            IsExecuting = true;
            try
            {
                List<TargetPositionsDataModel> targetRows = null;
                List<LivePositionsDataModel> liveRows = null;

                await Task.Run(() =>
                {
                    targetRows = GetLatestTargetPositions(FundName, TadId);
                    liveRows = GetLatestLivePositions(FundName, TadId);
                });

                TargetPositions.Clear();
                for (int i = 0; i < targetRows.Count; i++)
                    TargetPositions.Add(targetRows[i]);

                LivePositions.Clear();
                for (int i = 0; i < liveRows.Count; i++)
                    LivePositions.Add(liveRows[i]);

                LastRunTime = DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss");
            }
            finally
            {
                IsExecuting = false;
            }
        }

        private List<TargetPositionsDataModel> GetLatestTargetPositions(string fundName, string tadId)
        {
            List<TargetPositionsDataModel> list = new List<TargetPositionsDataModel>();

            string sql = @"
                WITH cte AS
                (
                    SELECT
                        runtime,
                        fundname,
                        subaccountname,
                        lastprice,
                        position_limit,
                        position_target,
                        deployment,
                        deployment_tad,
                        newposition_id,
                        roll_percent,
                        scaled_target,
                        scaled_percent,
                        var_daily,
                        ptval_constant,
                        spot_rate,
                        var,
                        scale,
                        weight,
                        capital,
                        var_target,
                        var_allocated,
                        sub_ticker_wt,
                        var_limit,
                        position_limit_raw,
                        position_target_raw,
                        tad_id,
                        tickername,
                        sub_tickername,
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY
                                fundname,
                                tickername,
                                subaccountname,
                                lastprice,
                                position_limit,
                                position_target,
                                deployment,
                                deployment_tad,
                                newposition_id,
                                roll_percent,
                                scaled_target,
                                scaled_percent,
                                var_daily,
                                ptval_constant,
                                spot_rate,
                                var,
                                scale,
                                weight,
                                capital,
                                var_target,
                                var_allocated,
                                sub_ticker_wt,
                                var_limit,
                                position_limit_raw,
                                position_target_raw,
                                tad_id,
                                sub_tickername
                            ORDER BY runtime DESC
                        ) AS rn
                    FROM target_positions
                    WHERE fundname = '" + EscapeSql(fundName) + @"'
                      AND tad_id = '" + EscapeSql(tadId) + @"'
                      AND tickername = '" + EscapeSql(TickerName) + @"' 
                      AND sub_tickername = 'NONE'
                )
                SELECT *
                FROM cte
                WHERE rn = 1
                ORDER BY runtime DESC;";

            DataTable dt = _db.execSQL(sql, _sqlConn);

            foreach (DataRow row in dt.Rows)
            {
                TargetPositionsDataModel item = new TargetPositionsDataModel();

                item.Runtime = SafeDateTime(row, "runtime");
                item.FundName = FirstNonEmpty(row, "fundname", "fund_name");
                item.TadId = FirstNonEmpty(row, "tad_id");
                item.TickerName = FirstNonEmpty(row, "tickername");
                item.SubaccountName = SafeString(row, "subaccountname");
                item.LastPrice = SafeNullableDouble(row, "lastprice");
                item.PositionLimit = SafeNullableDouble(row, "position_limit");
                item.PositionTarget = SafeNullableDouble(row, "position_target");
                item.Deployment = SafeNullableDouble(row, "deployment");
                item.DeploymentTad = SafeNullableDouble(row, "deployment_tad");
                item.NewpositionId = SafeString(row, "newposition_id");
                item.RollPercent = SafeNullableDouble(row, "roll_percent");
                item.ScaledTarget = SafeNullableDouble(row, "scaled_target");
                item.ScaledPercent = SafeNullableDouble(row, "scaled_percent");
                item.VarDaily = SafeNullableDouble(row, "var_daily");
                item.PtvalConstant = SafeNullableDouble(row, "ptval_constant");
                item.SpotRate = SafeNullableDouble(row, "spot_rate");
                item.Var = SafeNullableDouble(row, "var");
                item.Scale = SafeNullableDouble(row, "scale");
                item.Weight = SafeNullableDouble(row, "weight");
                item.Capital = SafeNullableDouble(row, "capital");
                item.VarTarget = SafeNullableDouble(row, "var_target");
                item.VarAllocated = SafeNullableDouble(row, "var_allocated");
                item.SubTickerWt = SafeNullableDouble(row, "sub_ticker_wt");
                item.VarLimit = SafeNullableDouble(row, "var_limit");
                item.PositionLimitRaw = SafeNullableDouble(row, "position_limit_raw");
                item.PositionTargetRaw = SafeNullableDouble(row, "position_target_raw");

                list.Add(item);
            }

            return list;
        }

        private List<LivePositionsDataModel> GetLatestLivePositions(string fundName, string tadId)
        {
            List<LivePositionsDataModel> list = new List<LivePositionsDataModel>();

            string sql = @"
                WITH cte AS
                (
                    SELECT
                        runtime,
                        fundname,
                        subaccountname,
                        tad_id,
                        tickername,
                        sub_tickername,
                        broker_code_exec,
                        market_pitopen,
                        lastprice,
                        position_limit,
                        position_live,
                        position_target,
                        deployment,
                        deployment_tad,
                        scaled_percent,
                        position_target_raw,
                        valid,
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY
                                fundname,
                                subaccountname,
                                tad_id,
                                tickername,
                                sub_tickername,
                                broker_code_exec,
                                market_pitopen,
                                lastprice,
                                position_limit,
                                position_live,
                                position_target,
                                deployment,
                                deployment_tad,
                                scaled_percent,
                                position_target_raw,
                                valid
                            ORDER BY runtime DESC
                        ) AS rn
                    FROM live_positions
                    WHERE fundname = '" + EscapeSql(fundName) + @"'
                      AND tad_id = '" + EscapeSql(tadId) + @"'
                      AND tickername = '" + EscapeSql(TickerName) + @"'     
     
                )
                SELECT *
                FROM cte
                WHERE rn = 1
                ORDER BY runtime DESC;";

            DataTable dt = _db.execSQL(sql, _sqlConn);

            foreach (DataRow row in dt.Rows)
            {
                LivePositionsDataModel item = new LivePositionsDataModel();

                item.RunTime = SafeDateTime(row, "runtime");
                item.FundName = SafeString(row, "fundname");
                item.SubaccountName = SafeString(row, "subaccountname");
                item.TadId = SafeString(row, "tad_id");
                item.TickerName = SafeString(row, "tickername");
                item.SubTickerName = SafeString(row, "sub_tickername");
                item.BrokerCodeExec = SafeString(row, "broker_code_exec");
                item.MarketpitOpen = SafeDateTime(row, "market_pitopen");
                item.LastPrice = SafeNullableDouble(row, "lastprice");
                item.PositionLimit = SafeNullableDouble(row, "position_limit");
                item.PositionLive = SafeNullableDouble(row, "position_live");
                item.PositionTarget = SafeNullableDouble(row, "position_target");
                item.Deployment = SafeNullableDouble(row, "deployment");
                item.DeploymentTad = SafeNullableDouble(row, "deployment_tad");

                item.ScaledPercent = SafeNullableDouble(row, "scaled_percent");
                item.PositionTargetRaw = SafeNullableDouble(row, "position_target_raw");
                item.Valid = SafeNullableBool(row, "valid");

                list.Add(item);
            }

            return list;
        }

        private static string SafeString(DataRow row, string columnName)
        {
            if (row == null || !row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
                return null;

            return Convert.ToString(row[columnName]);
        }

        private static string FirstNonEmpty(DataRow row, params string[] columnNames)
        {
            for (int i = 0; i < columnNames.Length; i++)
            {
                string value = SafeString(row, columnNames[i]);
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
            return null;
        }

        private static double? SafeNullableDouble(DataRow row, string columnName)
        {
            if (row == null || !row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
                return null;

            double d;
            if (double.TryParse(Convert.ToString(row[columnName]), NumberStyles.Any, CultureInfo.InvariantCulture, out d))
                return d;

            if (double.TryParse(Convert.ToString(row[columnName]), NumberStyles.Any, CultureInfo.CurrentCulture, out d))
                return d;

            return null;
        }
        private static bool? SafeNullableBool(DataRow row, string columnName)
        {
            if (row == null || !row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
                return null;

            // Handles actual bool/bit columns (SQL Server bit comes back as bool)
            if (row[columnName] is bool b)
                return b;

            string val = Convert.ToString(row[columnName]).Trim();

            if (bool.TryParse(val, out bool parsed))
                return parsed;

            // Handle 1/0 from numeric bit columns
            if (val == "1") return true;
            if (val == "0") return false;

            return null;
        }

        private static DateTime SafeDateTime(DataRow row, string columnName)
        {
            if (row == null || !row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
                return default(DateTime);

            DateTime dt;
            if (DateTime.TryParse(Convert.ToString(row[columnName]), out dt))
                return dt;

            return default(DateTime);
        }

        private static string EscapeSql(string value)
        {
            return (value ?? "").Replace("'", "''");
        }
    }

    public class TadIdItem
    {
        public string TadId { get; set; }
        public string TickerName { get; set; }

        public string DisplayText => $"{TadId}  —  {TickerName}";

        public override string ToString()
        {
            return DisplayText;
        }
    }


    public class ChartMetricItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _key;
        public string Key
        {
            get { return _key; }
            set
            {
                if (_key != value)
                {
                    _key = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _displayName;
        public string DisplayName
        {
            get { return _displayName; }
            set
            {
                if (_displayName != value)
                {
                    _displayName = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isChecked;
        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged();
                }
            }
        }

        public ChartMetricItem()
        {
        }

        public ChartMetricItem(string key, string displayName, bool isChecked)
        {
            _key = key;
            _displayName = displayName;
            _isChecked = isChecked;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}