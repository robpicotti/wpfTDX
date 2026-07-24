using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TDX;

namespace wpfTDX
{
    public class ProcessMonitorViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private readonly SqlConnection _sqlConn;
        private readonly TDX.db _db = new TDX.db();

        // Keyed by process_name from the heartbeats table
        public Dictionary<string, ObservableCollection<ProcessRunDataModel>> ProcessData { get; }
            = new Dictionary<string, ObservableCollection<ProcessRunDataModel>>();

        public List<string> MonitoredProcesses { get; private set; } = new List<string>();

        private DateTime _dateFrom;
        public DateTime DateFrom
        {
            get => _dateFrom;
            set { if (_dateFrom != value) { _dateFrom = value; OnPropertyChanged(); } }
        }

        private DateTime _dateTo;
        public DateTime DateTo
        {
            get => _dateTo;
            set { if (_dateTo != value) { _dateTo = value; OnPropertyChanged(); } }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { if (_isLoading != value) { _isLoading = value; OnPropertyChanged(); } }
        }

        public ProcessMonitorViewModel(SqlConnection conn)
        {
            _sqlConn = conn;
            DateTo   = DateTime.Today;
            DateFrom = SubtractBusinessDays(DateTime.Today, 7);
        }

        private static DateTime SubtractBusinessDays(DateTime date, int days)
        {
            int remaining = days;
            while (remaining > 0)
            {
                date = date.AddDays(-1);
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                    remaining--;
            }
            return date;
        }

        public async Task LoadDataAsync(string hostEnvironment)
        {
            IsLoading = true;
            try
            {
                // Get monitored process names from the heartbeats table for this environment
                var heartbeats = new TDX.HeartBeats(_sqlConn, hostEnvironment);
                var allProcesses = heartbeats.ListHeartBeats
                    .Select(b => b.process_name)
                    .ToList();

                var prefixes = (ConfigurationManager.AppSettings["ProcessMonitorPrefixes"] ?? "")
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .Where(p => p.Length > 0)
                    .ToList();

                MonitoredProcesses = prefixes.Count > 0
                    ? allProcesses.Where(n => prefixes.Any(p =>
                        n.StartsWith(p, StringComparison.OrdinalIgnoreCase))).ToList()
                    : allProcesses;

                string startStr = DateFrom.Date.ToString("yyyy-MM-dd");
                string endStr   = DateTo.Date.AddDays(1).ToString("yyyy-MM-dd");

                var results = new Dictionary<string, List<ProcessRunDataModel>>();
                foreach (var name in MonitoredProcesses)
                    results[name] = null;

                await Task.Run(() =>
                {
                    foreach (var name in MonitoredProcesses)
                        results[name] = QueryProcess(name, startStr, endStr);
                });

                foreach (var name in MonitoredProcesses)
                {
                    if (!ProcessData.ContainsKey(name))
                        ProcessData[name] = new ObservableCollection<ProcessRunDataModel>();
                    Populate(ProcessData[name], results[name]);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private static void Populate(ObservableCollection<ProcessRunDataModel> col, List<ProcessRunDataModel> rows)
        {
            col.Clear();
            if (rows != null)
                foreach (var r in rows) col.Add(r);
        }

        /// <summary>
        /// Returns a diagnostic string showing sample log text for the given process.
        /// </summary>
        public string DiagnoseAsync(string processName)
        {
            string sql = $@"
SELECT TOP 3 runtime, process_id, created_by,
       LEFT(log, 120) AS log_sample, heartbeat
FROM   error_log
WHERE  created_by = '{processName}'
  AND  heartbeat  = 1
ORDER  BY runtime DESC";

            try
            {
                DataTable dt = _db.execSQL(sql, _sqlConn);
                if (dt.Rows.Count == 0)
                    return $"[{processName}] NO rows found in error_log for this process/heartbeat";

                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"[{processName}] {dt.Rows.Count} sample row(s):");
                foreach (DataRow r in dt.Rows)
                    sb.AppendLine($"  {r["runtime"]}  pid={r["process_id"]}  log={r["log_sample"]}");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"[{processName}] DIAG ERROR: {ex.Message}";
            }
        }

        private List<ProcessRunDataModel> QueryProcess(string processName, string startStr, string endStr)
        {
            // Parse duration and optional avg-per-ticker from the Stats: section of the Ended log row.
            // Log format: "** Ended <name>  Stats: ... took D days HH:MM:SS.ffffff ...
            //               [with an average of X per ticker]"
            string sql = $@"
WITH raw AS (
    SELECT
        runtime                                            AS end_time,
        process_id,
        created_by,
        CHARINDEX('took ', log)                           AS took_pos,
        CHARINDEX(' days ', log, CHARINDEX('took ', log)) AS days_pos,
        CHARINDEX('with an average of ', log)             AS avg_pos,
        log
    FROM   error_log
    WHERE  created_by = '{processName}'
      AND  log LIKE '%** Ended%'
      AND  log LIKE '%Stats:%'
      AND  heartbeat = 1
      AND  runtime >= '{startStr}'
      AND  runtime <  '{endStr}'
),
dur AS (
    SELECT
        end_time,
        process_id,
        created_by,
        CAST(SUBSTRING(log, took_pos + 5, days_pos - took_pos - 5) AS INT) AS d,
        CAST(SUBSTRING(log, days_pos + 6, 2) AS INT)                        AS h,
        CAST(SUBSTRING(log, days_pos + 9, 2) AS INT)                        AS m,
        CAST(SUBSTRING(log, days_pos + 12, 2) AS INT)                       AS s,
        CASE
            WHEN avg_pos > 0
                 AND CHARINDEX(' per ticker', log, avg_pos) > avg_pos + 19
            THEN
                -- TRY_CAST returns NULL (not an error) for any non-numeric token
                -- the log can carry here: 'None' (no duration data), 'nan',
                -- 'inf', '-inf', etc. A plain CAST would abort the whole query.
                TRY_CAST(SUBSTRING(log, avg_pos + 19,
                    CHARINDEX(' per ticker', log, avg_pos) - avg_pos - 19) AS FLOAT)
            ELSE NULL
        END AS avg_per_ticker
    FROM raw
    WHERE took_pos > 0 AND days_pos > 0
)
SELECT
    DATEADD(SECOND, -(d * 86400 + h * 3600 + m * 60 + s), end_time) AS start_time,
    end_time,
    d * 1440.0 + h * 60.0 + m + s / 60.0                            AS duration_minutes,
    avg_per_ticker,
    process_id,
    created_by
FROM   dur
ORDER BY start_time";

            var list = new List<ProcessRunDataModel>();
            DataTable dt = _db.execSQL(sql, _sqlConn);
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new ProcessRunDataModel
                {
                    StartTime       = Convert.ToDateTime(row["start_time"]),
                    EndTime         = Convert.ToDateTime(row["end_time"]),
                    DurationMinutes = Convert.ToDouble(row["duration_minutes"]),
                    AvgPerTicker    = row["avg_per_ticker"] == DBNull.Value
                                        ? (double?)null
                                        : Convert.ToDouble(row["avg_per_ticker"]),
                    ProcessId       = Convert.ToInt32(row["process_id"]),
                    ProcessName     = row["created_by"].ToString()
                });
            }
            return list;
        }
    }
}
