using GalaSoft.MvvmLight.Command;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace wpfTDX
{
    /// <summary>
    /// Rebuilt scheduled-jobs screen VM. Async, explicit-save, talks to the typed
    /// tapi endpoints (/get_schedule_jobs, /create_schedule_job, /update_schedule_job,
    /// /delete_schedule_job) — no /exec_sql, no client-side id generation.
    /// </summary>
    public class ScheduleJobViewModel : INotifyPropertyChanged
    {
        private readonly string apiKey = ConfigurationManager.AppSettings["TradingApiKey"];
        private readonly string baseUrl = ConfigurationManager.AppSettings["TradingApiBaseUrl"];

        public ObservableCollection<JobRow> Jobs { get; } = new ObservableCollection<JobRow>();
        public ObservableCollection<FreqOption> Frequencies { get; } = new ObservableCollection<FreqOption>();
        public ObservableCollection<DayOption> Days { get; } = new ObservableCollection<DayOption>();
        // half-hourly UTC clock times (00:00 .. 23:30) for the calendar-mode run-time picker
        public ObservableCollection<string> HalfHourTimes { get; } = new ObservableCollection<string>();
        // day-mode options for the picker
        public ObservableCollection<string> DayModes { get; } =
            new ObservableCollection<string> { "business", "calendar" };

        // buffered editor for a new job + its parameter rows (flat: name/value/environment)
        public JobRow NewJob { get; private set; } = JobRow.Blank();
        public ObservableCollection<ParamRow> NewParams { get; } = new ObservableCollection<ParamRow>();

        private DateTime? _newJobStartFrom = DateTime.UtcNow.Date;
        public DateTime? NewJobStartFrom
        {
            get => _newJobStartFrom;
            set { _newJobStartFrom = value; OnPropertyChanged(); }
        }

        // optional for_date for a force-run of the selected job (null = run now)
        private DateTime? _forceRunDate;
        public DateTime? ForceRunDate
        {
            get => _forceRunDate;
            set { _forceRunDate = value; OnPropertyChanged(); }
        }

        private JobRow _selectedJob;
        public JobRow SelectedJob
        {
            get => _selectedJob;
            set { _selectedJob = value; OnPropertyChanged(); }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        private string _status;
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public ICommand RefreshCommand { get; }
        public ICommand CreateCommand { get; }
        public ICommand UpdateCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ClearNewCommand { get; }
        public ICommand ForceRunCommand { get; }

        public ScheduleJobViewModel()
        {
            for (int m = 0; m < 24 * 60; m += 30)
                HalfHourTimes.Add($"{m / 60:D2}:{m % 60:D2}");

            RefreshCommand = new RelayCommand(async () => await LoadAsync());
            CreateCommand = new RelayCommand(async () => await CreateAsync());
            UpdateCommand = new RelayCommand(async () => await UpdateAsync());
            DeleteCommand = new RelayCommand(async () => await DeleteAsync());
            ClearNewCommand = new RelayCommand(ClearNew);
            ForceRunCommand = new RelayCommand(async () => await ForceRunAsync());
        }

        private HttpClient Client()
        {
            var c = new HttpClient();
            c.DefaultRequestHeaders.Add("X-API-Key", apiKey);
            return c;
        }

        private StringContent Json(object o) =>
            new StringContent(JsonConvert.SerializeObject(o), Encoding.UTF8, "application/json");

        public async Task LoadAsync()
        {
            try
            {
                IsBusy = true;
                Status = "Loading jobs...";
                string json;
                using (var c = Client())
                {
                    var resp = await c.PostAsync($"{baseUrl}/get_schedule_jobs", Json(new { }));
                    resp.EnsureSuccessStatusCode();
                    json = await resp.Content.ReadAsStringAsync();
                }
                var root = JObject.Parse(json);

                Jobs.Clear();
                foreach (var j in (root["jobs"] as JArray) ?? new JArray())
                    Jobs.Add(JobRow.FromJson((JObject)j));

                Frequencies.Clear();
                foreach (var f in (root["frequencies"] as JArray) ?? new JArray())
                    Frequencies.Add(new FreqOption
                    {
                        FreqId = f.Value<int>("freq_id"),
                        FreqName = f.Value<string>("freq_name")
                    });

                Days.Clear();
                foreach (var d in (root["days"] as JArray) ?? new JArray())
                    Days.Add(new DayOption
                    {
                        DayId = d.Value<int>("day_id"),
                        DayName = d.Value<string>("day_name")
                    });

                Status = $"Loaded {Jobs.Count} job(s).";
            }
            catch (Exception ex)
            {
                Status = "Load failed: " + ex.Message;
                MessageBox.Show(ex.Message, "Load jobs failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsBusy = false; }
        }

        private async Task CreateAsync()
        {
            if (string.IsNullOrWhiteSpace(NewJob.JobName) ||
                string.IsNullOrWhiteSpace(NewJob.PythonModule) ||
                string.IsNullOrWhiteSpace(NewJob.PythonClass) ||
                string.IsNullOrWhiteSpace(NewJob.ExecuteMethod) ||
                NewJob.FreqId == null || NewJob.DayId == null)
            {
                MessageBox.Show("Fill in name, module, class, method, frequency and day.",
                    "Missing fields", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.Equals(NewJob.DayMode, "calendar", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(NewJob.RunTimeUtc))
            {
                MessageBox.Show("Calendar day-mode needs a Run time (UTC).",
                    "Missing run time", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var parameters = NewParams
                .Where(p => !string.IsNullOrWhiteSpace(p.ParamName))
                .GroupBy(p => p.ParamName.Trim())
                .Select(g => new
                {
                    param_name = g.Key,
                    values = g.Where(x => !string.IsNullOrWhiteSpace(x.ParamValue))
                              .Select(x => new { param_value = x.ParamValue, environment = x.Environment })
                              .ToList()
                }).ToList();

            var body = new
            {
                job = new
                {
                    job_name = NewJob.JobName,
                    python_module = NewJob.PythonModule,
                    python_class = NewJob.PythonClass,
                    execute_method = NewJob.ExecuteMethod,
                    day_id = NewJob.DayId,
                    freq_id = NewJob.FreqId,
                    global_closing_delta_minutes = NewJob.GlobalClosingDeltaMinutes,
                    backfill = NewJob.Backfill,
                    enabled = NewJob.Enabled,
                    day_mode = NewJob.DayMode,
                    run_time_utc = NewJob.RunTimeUtc
                },
                parameters,
                start_from = NewJobStartFrom?.ToString("yyyy-MM-dd")
            };

            await PostAndRefresh("/create_schedule_job", body, "Created job.");
            ClearNew();
        }

        private async Task UpdateAsync()
        {
            if (SelectedJob == null) { Status = "Select a job first."; return; }
            var body = new
            {
                job = new
                {
                    job_id = SelectedJob.JobId,
                    job_name = SelectedJob.JobName,
                    python_module = SelectedJob.PythonModule,
                    python_class = SelectedJob.PythonClass,
                    execute_method = SelectedJob.ExecuteMethod,
                    day_id = SelectedJob.DayId,
                    freq_id = SelectedJob.FreqId,
                    global_closing_delta_minutes = SelectedJob.GlobalClosingDeltaMinutes,
                    backfill = SelectedJob.Backfill,
                    enabled = SelectedJob.Enabled,
                    day_mode = SelectedJob.DayMode,
                    run_time_utc = SelectedJob.RunTimeUtc
                }
            };
            await PostAndRefresh("/update_schedule_job", body, "Updated job.");
        }

        private async Task DeleteAsync()
        {
            if (SelectedJob == null) { Status = "Select a job first."; return; }
            if (MessageBox.Show($"Delete job '{SelectedJob.JobName}' (id {SelectedJob.JobId})?",
                    "Confirm delete", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            await PostAndRefresh("/delete_schedule_job", new { job_id = SelectedJob.JobId }, "Deleted job.");
        }

        private async Task ForceRunAsync()
        {
            if (SelectedJob == null) { Status = "Select a job first."; return; }
            var body = new
            {
                job_name = SelectedJob.JobName,
                for_dates = ForceRunDate.HasValue
                    ? new[] { ForceRunDate.Value.ToString("yyyy-MM-dd") } : null
            };
            try
            {
                IsBusy = true;
                Status = $"Running '{SelectedJob.JobName}'...";
                string json;
                System.Net.Http.HttpResponseMessage resp;
                using (var c = Client())
                {
                    c.Timeout = TimeSpan.FromMinutes(20);   // force-run blocks until the job finishes
                    resp = await c.PostAsync($"{baseUrl}/run_schedule_job", Json(body));
                    json = await resp.Content.ReadAsStringAsync();
                }
                if (!resp.IsSuccessStatusCode)
                {
                    var err = TryError(json) ?? resp.ReasonPhrase;
                    Status = "Force run failed: " + err;
                    MessageBox.Show(err, "Force run failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                var jo = JObject.Parse(json);
                int ran = jo.Value<int?>("ran") ?? 0;
                double secs = jo.Value<double?>("duration_seconds") ?? 0;
                Status = ran > 0
                    ? $"'{SelectedJob.JobName}' completed: {ran} run(s) in {secs}s."
                    : $"'{SelectedJob.JobName}' produced 0 runs (env filter / no jobdetails for this env?).";
                await LoadAsync();
            }
            catch (Exception ex)
            {
                Status = "Force run error: " + ex.Message;
                MessageBox.Show(ex.Message, "Force run error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsBusy = false; }
        }

        private async Task PostAndRefresh(string route, object body, string okMsg)
        {
            try
            {
                IsBusy = true;
                string json;
                using (var c = Client())
                {
                    var resp = await c.PostAsync($"{baseUrl}{route}", Json(body));
                    json = await resp.Content.ReadAsStringAsync();
                    if (!resp.IsSuccessStatusCode)
                    {
                        var err = TryError(json) ?? resp.ReasonPhrase;
                        Status = $"{route} failed: {err}";
                        MessageBox.Show(err, "Request failed", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                Status = okMsg;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                Status = $"{route} error: {ex.Message}";
                MessageBox.Show(ex.Message, "Request error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsBusy = false; }
        }

        private static string TryError(string json)
        {
            try { return JObject.Parse(json)["error"]?.ToString(); }
            catch { return null; }
        }

        private void ClearNew()
        {
            NewJob = JobRow.Blank();
            OnPropertyChanged(nameof(NewJob));
            NewParams.Clear();
            NewJobStartFrom = DateTime.UtcNow.Date;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class JobRow
    {
        public int JobId { get; set; }
        public string JobName { get; set; }
        public string PythonModule { get; set; }
        public string PythonClass { get; set; }
        public string ExecuteMethod { get; set; }
        public int? DayId { get; set; }
        public int? FreqId { get; set; }
        public int? GlobalClosingDeltaMinutes { get; set; }
        public bool Backfill { get; set; }
        public bool Enabled { get; set; } = true;
        // day_mode: "business" (default, close+GC-delta, business-day gated) or
        // "calendar" (absolute RunTimeUtc, runs every calendar day incl. weekends).
        public string DayMode { get; set; } = "business";
        public string RunTimeUtc { get; set; }   // "HH:MM" UTC, used in calendar mode
        // read-only display (from schedule_jobs_log / lookups)
        public string FreqName { get; set; }
        public string DayName { get; set; }
        public string NextRuntime { get; set; }
        public string LastRuntime { get; set; }
        public string LastStatus { get; set; }

        public static JobRow Blank() => new JobRow { Enabled = true, Backfill = false, DayMode = "business" };

        public static JobRow FromJson(JObject j) => new JobRow
        {
            JobId = j.Value<int?>("job_id") ?? 0,
            JobName = j.Value<string>("job_name"),
            PythonModule = j.Value<string>("python_module"),
            PythonClass = j.Value<string>("python_class"),
            ExecuteMethod = j.Value<string>("execute_method"),
            DayId = j.Value<int?>("day_id"),
            FreqId = j.Value<int?>("freq_id"),
            GlobalClosingDeltaMinutes = j.Value<int?>("global_closing_delta_minutes"),
            Backfill = j.Value<bool?>("backfill") ?? false,
            Enabled = j.Value<bool?>("enabled") ?? true,
            DayMode = string.IsNullOrWhiteSpace(j.Value<string>("day_mode")) ? "business" : j.Value<string>("day_mode"),
            RunTimeUtc = j.Value<string>("run_time_utc"),
            FreqName = j.Value<string>("freq_name"),
            DayName = j.Value<string>("day_name"),
            NextRuntime = j.Value<string>("next_runtime"),
            LastRuntime = j.Value<string>("last_runtime"),
            LastStatus = j.Value<string>("last_status"),
        };
    }

    public class FreqOption { public int FreqId { get; set; } public string FreqName { get; set; } }
    public class DayOption { public int DayId { get; set; } public string DayName { get; set; } }
    public class ParamRow
    {
        public string ParamName { get; set; }
        public string ParamValue { get; set; }
        public string Environment { get; set; }
    }
}
