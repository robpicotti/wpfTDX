
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
using OfficeOpenXml;
//using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using System.Collections.Specialized;


namespace wpfTDX
{
    public class ScheduleJobViewModel : INotifyPropertyChanged
    {

        private string _excelpath = @"\\ad01-har.10dynamics.com\Ray_Share\Files\files_backup\";
        public ICommand SaveCommand { get; private set; }
        public ICommand AddParameterCommand { get; private set; }
        public ICommand AddParameterValueCommand { get;private set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action SaveCompleted; //action for subscribing from UI for pop up msg
        public event Action BeginProcess; //action for subscribing to starting to process

        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private ObservableCollection<ScheduledJobDataModel> _scheduledjobs;
        
        public ObservableCollection<ScheduledJobDataModel> ScheduledJobs
        {
            get => _scheduledjobs;
            set
            {
                if (_scheduledjobs != value)
                {
                    _scheduledjobs = value;
                    OnPropertyChanged(nameof(ScheduledJobs));
                }
            }
        }

        public ObservableCollection<ScheduledJobDataModel> NewJobs { get; set; }

        public ObservableCollection<JobParametersDataModel> NewParameters { get; set; }

        public ObservableCollection<JobParameterValueDataModel> NewParameterValues { get; set; }

        private ObservableCollection<ScheduleDaysDataModel> _scheduledays;

        public ObservableCollection<ScheduleDaysDataModel> ScheduleDays
        {
            get => _scheduledays;
            set
            {
                if (_scheduledays != value)
                {
                    _scheduledays = value;
                    OnPropertyChanged(nameof(ScheduleDays));
                }
            }
        }

        private ObservableCollection<ScheduleFrequenciesDataModel> _schedulefrequencies;

        public ObservableCollection<ScheduleFrequenciesDataModel> ScheduleFrequencies
        {
            get => _schedulefrequencies;
            set
            {
                if (_schedulefrequencies != value)
                {
                    _schedulefrequencies = value;
                    OnPropertyChanged(nameof(ScheduleFrequencies));
                }
            }
        }

        public ObservableCollection<JobParametersDataModel> JobParameters { get; set; } = new ObservableCollection<JobParametersDataModel>();

        private ObservableCollection<JobParameterValueDataModel> _jobparametervalues;
        
        public ObservableCollection<JobParameterValueDataModel> JobParameterValues
        {
            get => _jobparametervalues;
            set
            {
                if (_jobparametervalues != value)
                {
                    _jobparametervalues = value;
                    OnPropertyChanged(nameof(JobParameterValues));
                }
            }
        }

        public ObservableCollection<bool> TrueFalseOptions { get; set; } = new ObservableCollection<bool> { true, false };

        private bool _isBackFill;
        
        public bool IsBackFill
        {
            get => _isBackFill;
            set
            {
                if (_isBackFill != value)
                {
                    _isBackFill = value;
                    OnPropertyChanged(nameof(IsBackFill));
                }
            }
        }

        private ScheduledJobDataModel _selectedJob;
        
        public ScheduledJobDataModel SelectedJob
        {
            get => _selectedJob;
            set
            {
                if (_selectedJob != value)
                {
                    _selectedJob = value;
                    OnPropertyChanged(nameof(SelectedJob));
                    //unsubscribe from newparameters
                    JobParameters.CollectionChanged -= JobParameters_CollectionChanged;
                    JobParameterValues.CollectionChanged -= JobParameterValues_CollectionChanged;
                    // Clear job parameters and values when a new job is selected
                    JobParameters.Clear();
                    JobParameterValues.Clear();

                    // Load job parameters for the selected job
                    if (_selectedJob != null)
                    {
                        // Fetch and load parameters specific to the selected job
                        var jobParams = GetJobParameters(_selectedJob.JobId);

                        foreach (var param in jobParams)
                        {
                            param.PropertyChanged += JobParameters_PropertyChanged;
                            JobParameters.Add(param);
                        }
                    }
                    JobParameters.CollectionChanged += JobParameters_CollectionChanged;
                    JobParameterValues.CollectionChanged += JobParameterValues_CollectionChanged;
                }
            }
        }

        // Event handler to track new job additions
        private void ScheduledJobs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (ScheduledJobDataModel newJob in e.NewItems)
                {
                    // Add the new job to the NewJobs collection
                    NewJobs.Add(newJob);
                }
            }
        }
        // Event handler to track new parameters additions
        private void JobParameters_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (JobParametersDataModel newparameter in e.NewItems)
                {
                    NewParameters.Add(newparameter);
                }
                NewParameterValues.Clear();
            }

        }

        //event handler for new paramater values
        private void JobParameterValues_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (JobParameterValueDataModel newparametervalue in e.NewItems)
                {
                    NewParameterValues.Add(newparametervalue);
                }
            }
        }


        private void JobParameters_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var jobparam = sender as JobParametersDataModel;
            string column_name = "";
            if (jobparam != null)
            {
                if (e.PropertyName == nameof(JobParametersDataModel.ParameterName))
                {
                    // Handle frequency changes here
                    column_name = "param_name";

                }
                
                if (column_name != "")
                {
                    UpdateJobParametersTable(jobparam, column_name);
                    //the updated values from the db
                    GetJobParameters();
                    //export to excel (all job parameters not just the udpated one
                    ObservableCollection<JobParametersDataModel> _ocallparams = new ObservableCollection<JobParametersDataModel>(allJobParameters);
                    ExportJobParametersToExcel(_ocallparams);
                }
            }
        }

        private void JobParameterValues_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var jobparamvalue = sender as JobParameterValueDataModel;
            string column_name = "";
            if (jobparamvalue != null)
            {
                if (e.PropertyName == nameof(JobParameterValueDataModel.ParameterValue))
                {
                    // Handle frequency changes here
                    column_name = "param_value";

                }
                else if(e.PropertyName == nameof(JobParameterValueDataModel.Environment))
                {
                    column_name = "environment";
                }
                if (column_name != "")
                {
                    UpdateJobParameterValuesTable(jobparamvalue, column_name);
                    ////the updated values from the db
                    GetJobParameterValues();
                    ////export to excel (all job parameters not just the udpated one
                    ObservableCollection<JobParameterValueDataModel> _ocallparamvalues = new ObservableCollection<JobParameterValueDataModel>(allParameterValues);
                    ExportJobParameterValuesToExcel(_ocallparamvalues);
                }
            }
        }

        private void UpdateJobParameterValuesTable(JobParameterValueDataModel jobparamvalue, string table_column_to_update)
        {
            string sqltext = "UPDATE schedule_jobs_param_values SET ";
            try
            {
                switch (table_column_to_update)
                {
                    case "param_value":
                        sqltext += " param_value='" + jobparamvalue.ParameterValue + "'";
                        break;
                    case "environment":
                        sqltext += " environment='" + jobparamvalue.Environment + "'";
                        break;
                }
                sqltext += " WHERE param_value_id=" + jobparamvalue.ParameterValueId + " AND param_id=" + jobparamvalue.ParameterId;
                ExecSQL(sqltext);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private void UpdateJobParametersTable(JobParametersDataModel jobparam, string table_column_to_update)
        {
            string sqltext = "UPDATE schedule_jobs_parameters SET ";
            try
            {
                switch (table_column_to_update)
                {
                    case "param_name":
                        sqltext += " param_name='" + jobparam.ParameterName + "'";
                        break;

                }
                sqltext += " WHERE param_id=" + jobparam.ParameterId + " AND job_id=" + jobparam.JobId;
                ExecSQL(sqltext);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private void ScheduledJob_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var job = sender as ScheduledJobDataModel;
            string column_name = "";
            if (job != null)
            {
                int jobid = job.JobId;
                if (e.PropertyName == nameof(ScheduledJobDataModel.FrequencyId))
                {
                    // Handle frequency changes here
                     column_name = "freq_id";
                    
                }
                else if (e.PropertyName == nameof(ScheduledJobDataModel.PythonModule))
                {
                    column_name = "python_module";
                }
                else if (e.PropertyName == nameof(ScheduledJobDataModel.PythonClass))
                {
                    column_name = "python_class";
                }
                else if (e.PropertyName == nameof(ScheduledJobDataModel.ExecuteMethod))
                {
                    column_name = "execute_method";
                }
                else if (e.PropertyName == nameof(ScheduledJobDataModel.DayId))
                {
                    column_name = "day_id";
                }
                else if (e.PropertyName == nameof(ScheduledJobDataModel.GlobalClosingDeltaMinutes))
                {
                    column_name = "global_closing_delta_minutes";
                }
                else if (e.PropertyName == nameof(ScheduledJobDataModel.BackFill))
                {
                    column_name = "backfill";
                }
                else if (e.PropertyName == nameof(ScheduledJobDataModel.JobName))
                {
                    column_name = "job_name";
                }
                if(column_name != "")
                {
                    UpdateScheduleJobsTable(job, column_name);
                    ExportScheduleJobsToExcel(ScheduledJobs);
                }
            }
        }

        private void UpdateScheduleJobsTable(ScheduledJobDataModel job,string table_column_to_update)
        {
            string sqltext = "UPDATE schedule_jobs2 SET ";
            try
            {
                switch(table_column_to_update)
                {
                    case "job_name":
                        sqltext += " job_name='" + job.JobName + "'";
                        break;
                    case "freq_id":
                        sqltext += " freq_id=" + job.FrequencyId;
                        break;
                    case "python_module":
                        sqltext += "python_module='" + job.PythonModule + "'";
                        break;
                    case "python_class":
                        sqltext += "python_class='" + job.PythonClass + "'";
                        break;
                    case "execute_method":
                        sqltext += "execute_method='" + job.ExecuteMethod + "'";
                        break;
                    case "day_id":
                        sqltext += "day_id=" + job.DayId;
                        break;
                    case "backfill":
                        int _backfill = 0;
                        if(job.BackFill)
                        {
                            _backfill = 1;
                        }
                        sqltext += "backfill=" + _backfill;
                        break;
                    case "global_closing_delta_minutes":
                        sqltext += "global_closing_delta_minutes=" + job.GlobalClosingDeltaMinutes;
                        break;

                }
                sqltext += " WHERE job_id=" + job.JobId; 
                ExecSQL(sqltext);
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private string ExecSQL(string sqltext)
        {
            using (HttpClient client = new HttpClient())
            {
                var requestData = new
                {
                    sqltext = sqltext                
                };
                string jsonRequest = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

                // Make a synchronous HTTP POST request
                HttpResponseMessage response = client.PostAsync("http://localhost:5001/exec_sql", content).Result;
                response.EnsureSuccessStatusCode();

                string jsonResponse = response.Content.ReadAsStringAsync().Result;
                return jsonResponse;
            }
        }
        // Holds all job parameters for all jobs
        private List<JobParametersDataModel> allJobParameters;
        
        private List<JobParameterValueDataModel> allParameterValues;

        private JobParametersDataModel _selectedjobparameter;
        
        public JobParametersDataModel SelectedJobParameter
        {
            get => _selectedjobparameter;
            set
            {
                if (_selectedjobparameter != value)
                {
                    _selectedjobparameter = value;
                    OnPropertyChanged(nameof(SelectedJobParameter));

                    // Clear existing parameter values
                    JobParameterValues.Clear();

                    // Load parameter values for the selected parameter
                    if (_selectedjobparameter != null)
                    {
                        // Call the method to load parameter values
                        LoadParameterValuesForSelectedJobParameter(_selectedjobparameter.ParameterId);
                    }
                }
            }
        }

        private List<JobParametersDataModel> GetJobParameters(int jobId)
        {
            return allJobParameters?.Where(p => p.JobId == jobId).ToList() ?? new List<JobParametersDataModel>();
        }

        private List<JobParameterValueDataModel> GetJobParameterValues(int parameterId)
        {
            return allParameterValues?.Where(pv => pv.ParameterId == parameterId).ToList() ?? new List<JobParameterValueDataModel>();
        }

        private JobParameterValueDataModel _selectedjobparametervalue;

        public JobParameterValueDataModel SelectedJobParameterValue
        {
            get => _selectedjobparametervalue;
            set
            {
                if (_selectedjobparametervalue != value)
                {
                    _selectedjobparametervalue = value;
                    OnPropertyChanged(nameof(SelectedJobParameterValue));
                }
            }
        }

        // This is a computed property that maps the DayID to the DayName
        public string SelectedJobDayName
        {
            get
            {
                if (SelectedJob != null && ScheduleDays != null)
                {
                    var day = ScheduleDays.FirstOrDefault(d => d.DayId == SelectedJob.DayId);
                    return day?.DayName ?? "Unknown";
                }
                return "Unknown";
            }
        }

        // This is a computed property that maps the FrequencyID to the frequencyname
        public string SelectedJobFrequencyName
        {
            get
            {
                if (SelectedJob != null && ScheduleFrequencies != null)
                {
                    var frequency = ScheduleFrequencies.FirstOrDefault(d => d.FrequencyId == SelectedJob.FrequencyId);
                    return frequency?.FrequencyName ?? "Unknown";
                }
                return "Unknown";
            }
        }

        public ScheduleJobViewModel()
        {
            SaveCommand = new RelayCommand(SaveAllJobs);
            AddParameterCommand = new RelayCommand(SaveParameters);
            AddParameterValueCommand = new RelayCommand(SaveParameterValues);
            ScheduledJobs = new ObservableCollection<ScheduledJobDataModel>();

            ScheduleDays = new ObservableCollection<ScheduleDaysDataModel>();
            ScheduleFrequencies = new ObservableCollection<ScheduleFrequenciesDataModel>();
            JobParameterValues = new ObservableCollection<JobParameterValueDataModel>(); // Initialize here
            NewJobs = new ObservableCollection<ScheduledJobDataModel>();
            NewParameters = new ObservableCollection<JobParametersDataModel>();
            NewParameterValues = new ObservableCollection<JobParameterValueDataModel>();
     

            GetScheduledJobs();
            AddScheduleJobEventHandlers();
            // Hook into collection change event after schedulejobs populated
            ScheduledJobs.CollectionChanged += ScheduledJobs_CollectionChanged;
            JobParameterValues.CollectionChanged += JobParameterValues_CollectionChanged;

            GetScheduleDays();
            GetScheduleFrequencies();
            GetJobParameters();
            //JobParameters.CollectionChanged += JobParameters_CollectionChanged;
            GetJobParameterValues();
        }
        
        private void AddScheduleJobEventHandlers()
        {
            foreach (var job in ScheduledJobs)
            {
                job.PropertyChanged += ScheduledJob_PropertyChanged;
            }
        }

        private void SaveAllJobs()
        {
            try
            {
                BeginProcess?.Invoke();
                //save new jobs to database
                CreateNewJobs(NewJobs);
                // Logic to save all jobs to Excel
                ExportScheduleJobsToExcel(ScheduledJobs);
                // Raise the SaveCompleted event
                SaveCompleted?.Invoke();
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private void SaveParameters()
        {
            BeginProcess?.Invoke();
            CreateNewJobParameters(NewParameters);
            //refresh all the job parameters
            GetJobParameters();
            //then export them
            ObservableCollection<JobParametersDataModel> _ocallparams = new ObservableCollection<JobParametersDataModel>(allJobParameters);
            ExportJobParametersToExcel(_ocallparams);
            SaveCompleted?.Invoke();
        }

        private void SaveParameterValues()
        {
            BeginProcess?.Invoke();
            CreateNewParameterValues(NewParameterValues);
            GetJobParameterValues();
            ObservableCollection<JobParameterValueDataModel> _ocallparamvalues = new ObservableCollection<JobParameterValueDataModel>(allParameterValues);
            ExportJobParameterValuesToExcel(_ocallparamvalues);
            SaveCompleted?.Invoke();
        }

        private void CreateNewJobParameters(ObservableCollection<JobParametersDataModel> newparams)
        {
            var jobParameterList = new List<Dictionary<string, object>>();
            for (int i = 0; i < newparams.Count;i++)
            {
                var param = newparams[i];
                param.JobId = SelectedJob.JobId;
                if(param.JobId !=0)
                {
                    param.ParameterId = GetNewParameterId();
                }
                var paramData = new Dictionary<string, object>
                {
                    { "param_id", param.ParameterId },
                    {"job_id",param.JobId },
                    {"param_name",param.ParameterName }
                };
                jobParameterList.Add(paramData);
                // Prepare the payload for the web service (table name + job data)
                var requestData = new
                {
                    table_name = "schedule_jobs_parameters",  // Replace with your table name
                    data = jobParameterList           // This is the list of job rows
                };
                // Convert to JSON
                var jsonPayload = JsonConvert.SerializeObject(requestData);

                // Send the JSON payload to the Python web service
                using (var client = new HttpClient())
                {
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = client.PostAsync("http://localhost:5001/insert_table", content).Result;
                    response.EnsureSuccessStatusCode();

                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = response.Content.ReadAsStringAsync();
                        Console.WriteLine("Success: " + responseString);
                    }
                    else
                    {
                        Console.WriteLine("Error: " + response.StatusCode);
                    }
                }
            }
            //clear the collection once we've added all the new jobs
            NewParameters.Clear();
        }
        /// <summary>
        /// inserts data into the job_parameter_values table
        /// </summary>
        /// <param name="newparamvalues"></param>
        private void CreateNewParameterValues(ObservableCollection<JobParameterValueDataModel> newparamvalues)
        {
            var jobParameterValueList = new List<Dictionary<string, object>>();
            for(int i=0; i < newparamvalues.Count;i++)
            {
                var newparam = newparamvalues[i];
                newparam.ParameterId = SelectedJobParameter.ParameterId;
                if (newparam.ParameterId != 0)
                {
                    newparam.ParameterValueId = GetNewParameterValueId();
                }
                var paramValueData = new Dictionary<string, object>
                {
                    { "param_value_id", newparam.ParameterValueId },
                    {"param_id",newparam.ParameterId },
                    {"param_value",newparam.ParameterValue },
                    {"environment", newparam.Environment}
                };
                jobParameterValueList.Add(paramValueData);
                var requestData = new
                {
                    table_name = "schedule_jobs_param_values",  // Replace with your table name
                    data = jobParameterValueList           // This is the list of job rows
                };
                var jsonPayload = JsonConvert.SerializeObject(requestData);
                // Send the JSON payload to the Python web service
                using (var client = new HttpClient())
                {
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = client.PostAsync("http://localhost:5001/insert_table", content).Result;
                    response.EnsureSuccessStatusCode();

                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = response.Content.ReadAsStringAsync();
                        Console.WriteLine("Success: " + responseString);
                    }
                    else
                    {
                        Console.WriteLine("Error: " + response.StatusCode);
                    }
                }
            }
            NewParameterValues.Clear();
        }

        private void CreateNewJobs(ObservableCollection<ScheduledJobDataModel> newjobs)
        {
            try
            {
                var jobDataList = new List<Dictionary<string, object>>();
                for (int i= 0; i < newjobs.Count;i++)
                {
                    var job = newjobs[i];
                    if(job.JobId==0)
                    {
                        //this is where i will need to create some sort of json to pass to webservice
                        job.JobId = GetNewJobId();
                    }
                    var jobData = new Dictionary<string, object>
                    {
                        { "runtime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},
                        {"job_id", job.JobId },
                        {"job_name", job.JobName },
                        {"python_module",job.PythonModule },
                        {"python_class",job.PythonClass },
                        {"execute_method",job.ExecuteMethod },
                        {"day_id", job.DayId },
                        { "freq_id",job.FrequencyId},
                        { "global_closing_delta_minutes",job.GlobalClosingDeltaMinutes},
                        {"backfill",job.BackFill }
                    };
                    jobDataList.Add(jobData);
                }
                if (jobDataList.Any())
                {
                    // Prepare the payload for the web service (table name + job data)
                    var requestData = new
                    {
                        table_name = "schedule_jobs2",  // Replace with your table name
                        data = jobDataList           // This is the list of job rows
                    };

                    // Convert to JSON
                    var jsonPayload = JsonConvert.SerializeObject(requestData);

                    // Send the JSON payload to the Python web service
                    using (var client = new HttpClient())
                    {
                        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                        HttpResponseMessage response = client.PostAsync("http://localhost:5001/insert_table", content).Result;
                        response.EnsureSuccessStatusCode();

                        if (response.IsSuccessStatusCode)
                        {
                            var responseString = response.Content.ReadAsStringAsync();
                            Console.WriteLine("Success: " + responseString);
                        }
                        else
                        {
                            Console.WriteLine("Error: " + response.StatusCode);
                        }
                    }
                    //clear the collection once we've added all the new jobs
                    NewJobs.Clear();
                }
            }
            catch(Exception ex)
            {
                throw new Exception("Create new jobs error: " + ex.Message);
            }
        }
       
        private void GetJobParameters()
        {
            ProcessJobParameters();
        }

        private void GetJobParameterValues()
        {
            ProcessJobParameterValues();
        }
        
        private void GetScheduledJobs()
        {
            ProcessScheduledJobs();
        }

        private void GetScheduleDays()
        {
            ProcessScheduleDays();
        }

        private void GetScheduleFrequencies()
        {
            ProcessScheduleFrequencies();
        }

        private string GetScheduledJobsData()
        {
            using (HttpClient client = new HttpClient())
            {
                var requestData = new
                {
                    table_name = "schedule_jobs2"
                };
                string jsonRequest = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

                // Make a synchronous HTTP POST request
                HttpResponseMessage response = client.PostAsync("http://localhost:5001/select_table", content).Result;
                response.EnsureSuccessStatusCode();

                string jsonResponse = response.Content.ReadAsStringAsync().Result;
                return jsonResponse;
            }
        }

        private void ProcessScheduledJobs()
        {
            string jsonResponse = GetScheduledJobsData();
            try
            {
                // Parse the response as a JArray since it's a list of dictionaries
                JArray jobsData = JArray.Parse(jsonResponse);

                // Deserialize the list of dictionaries directly into your DataModel
                var jobsList = JsonConvert.DeserializeObject<List<ScheduledJobDataModel>>(jobsData.ToString());

                if (ScheduledJobs == null)
                {
                    ScheduledJobs = new ObservableCollection<ScheduledJobDataModel>();
                }
                else
                {
                    ScheduledJobs.Clear();
                }

                // Add each job to the ObservableCollection
                foreach (var item in jobsList)
                {
                    ScheduledJobs.Add(item);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private string GetScheduleDaysData()
        {
            using (HttpClient client = new HttpClient())
            {
                var requestData = new
                {
                    table_name = "schedule_days"
                };
                string jsonRequest = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

                // Make a synchronous HTTP POST request
                HttpResponseMessage response = client.PostAsync("http://localhost:5001/select_table_all", content).Result;
                response.EnsureSuccessStatusCode();

                string jsonResponse = response.Content.ReadAsStringAsync().Result;
                return jsonResponse;
            }
        }

        private void ProcessScheduleDays()
        {
            string jsonResponse = GetScheduleDaysData();
            try
            {
                // Parse the response as a JArray since it's a list of dictionaries
                JArray daysData = JArray.Parse(jsonResponse);

                // Deserialize the list of dictionaries directly into your DataModel
                var daysList = JsonConvert.DeserializeObject<List<ScheduleDaysDataModel>>(daysData.ToString());

                if (ScheduleDays == null)
                {
                    ScheduleDays = new ObservableCollection<ScheduleDaysDataModel>();
                }
                else
                {
                    ScheduleDays.Clear();
                }

                // Add each job to the ObservableCollection
                foreach (var item in daysList)
                {
                    ScheduleDays.Add(item);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        //schedule frequ
        private string GetScheduleFrequenciesData()
        {
            using (HttpClient client = new HttpClient())
            {
                var requestData = new
                {
                    table_name = "schedule_frequencies"
                };
                string jsonRequest = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

                // Make a synchronous HTTP POST request
                HttpResponseMessage response = client.PostAsync("http://localhost:5001/select_table_all", content).Result;
                response.EnsureSuccessStatusCode();

                string jsonResponse = response.Content.ReadAsStringAsync().Result;
                return jsonResponse;
            }
        }

        private void ProcessScheduleFrequencies()
        {
            string jsonResponse = GetScheduleFrequenciesData();
            try
            {
                // Parse the response as a JArray since it's a list of dictionaries
                JArray freqData = JArray.Parse(jsonResponse);

                // Deserialize the list of dictionaries directly into your DataModel
                var daysList = JsonConvert.DeserializeObject<List<ScheduleFrequenciesDataModel>>(freqData.ToString());

                if (ScheduleFrequencies == null)
                {
                    ScheduleFrequencies = new ObservableCollection<ScheduleFrequenciesDataModel>();
                }
                else
                {
                    ScheduleFrequencies.Clear();
                }

                // Add each job to the ObservableCollection
                foreach (var item in daysList)
                {
                    ScheduleFrequencies.Add(item);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        //get the jobparameters data

        private string GetJobParametersData()
        {
            using (HttpClient client = new HttpClient())
            {
                var requestData = new
                {
                    table_name = "schedule_jobs_parameters"
                };
                string jsonRequest = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

                // Make a synchronous HTTP POST request
                HttpResponseMessage response = client.PostAsync("http://localhost:5001/select_table_all", content).Result;
                response.EnsureSuccessStatusCode();

                string jsonResponse = response.Content.ReadAsStringAsync().Result;
                return jsonResponse;
            }
        }

        private void ProcessJobParameters()
        {
            string jsonResponse = GetJobParametersData();
            try
            {
                // Parse the response as a JArray since it's a list of dictionaries
                JArray paramData = JArray.Parse(jsonResponse);

                // Deserialize the list of dictionaries directly into your DataModel
                allJobParameters = JsonConvert.DeserializeObject<List<JobParametersDataModel>>(paramData.ToString());

                // No need to fill JobParameters here, it will be done when the job is selected
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private string GetJobParameterValuesData()
        {
            using (HttpClient client = new HttpClient())
            {
                var requestData = new
                {
                    table_name = "schedule_jobs_param_values"
                };
                string jsonRequest = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

                // Make a synchronous HTTP POST request
                HttpResponseMessage response = client.PostAsync("http://localhost:5001/select_table_all", content).Result;
                response.EnsureSuccessStatusCode();

                string jsonResponse = response.Content.ReadAsStringAsync().Result;
                return jsonResponse;
            }
        }

        private void ProcessJobParameterValues()
        {
            string jsonResponse = GetJobParameterValuesData();
            try
            {
                // Parse the response as a JArray since it's a list of dictionaries
                JArray paramValueData = JArray.Parse(jsonResponse);

                // Deserialize the list of dictionaries directly into your DataModel
                allParameterValues = JsonConvert.DeserializeObject<List<JobParameterValueDataModel>>(paramValueData.ToString());

                // Check if allParameterValues is populated
                if (allParameterValues == null || !allParameterValues.Any())
                {
                    throw new Exception("No job parameter values were returned from the API.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing job parameter values: {ex.Message}");
            }
        }

        // Fetch parameter values when a parameter is selected
        // Placeholder method to load parameter values for the selected parameter
        private void LoadParameterValuesForSelectedJobParameter(int parameterId)
        {
            var valuesForSelectedParameter = allParameterValues.Where(pv => pv.ParameterId == parameterId).ToList();
            JobParameterValues.CollectionChanged -= JobParameterValues_CollectionChanged;
            foreach (var value in valuesForSelectedParameter)
            {
                value.PropertyChanged += JobParameterValues_PropertyChanged;
                JobParameterValues.Add(value);
            }
            JobParameterValues.CollectionChanged += JobParameterValues_CollectionChanged;
        }


        //exports job parameter values to Excel
        private void ExportJobParameterValuesToExcel(ObservableCollection<JobParameterValueDataModel> jobparametervalues)
        {
            string filepath = Path.Combine(_excelpath, "schedule_jobs_param_values.xlsx");
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            using (ExcelPackage package = new ExcelPackage())
            {
                // Add a new worksheet
                var worksheet = package.Workbook.Worksheets.Add("Schedule Jobs parameter values");

                // Set headers
                worksheet.Cells[1, 1].Value = "param_value_id";
                worksheet.Cells[1, 2].Value = "param_id";
                worksheet.Cells[1, 3].Value = "param_value";
                worksheet.Cells[1, 4].Value = "environment";

                // Add data for each job
                for (int i = 0; i < jobparametervalues.Count; i++)
                {
                    var param = jobparametervalues[i];

                    if (param.ParameterValueId == 0)
                    {
                        param.ParameterValueId = GetNewParameterValueId();
                    }

                    worksheet.Cells[i + 2, 1].Value = param.ParameterValueId;
                    worksheet.Cells[i + 2, 2].Value = param.ParameterId;
                    worksheet.Cells[i + 2, 3].Value = param.ParameterValue;
                    worksheet.Cells[i + 2, 4].Value = param.Environment;
                }

                // Save the Excel file
                FileInfo fi = new FileInfo(filepath);
                package.SaveAs(fi);
            }
        }

        //exports job parameters to excel
        private void ExportJobParametersToExcel(ObservableCollection<JobParametersDataModel> jobparameters)
        { 
            try
            {
                string filepath = Path.Combine(_excelpath, "schedule_jobs_parameters.xlsx");
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                // Create a new Excel package
                using (ExcelPackage package = new ExcelPackage())
                {
                    // Add a new worksheet
                    var worksheet = package.Workbook.Worksheets.Add("Schedule Jobs parameters");

                    // Set headers
                    worksheet.Cells[1, 1].Value = "param_id";
                    worksheet.Cells[1, 2].Value = "job_id";
                    worksheet.Cells[1, 3].Value = "param_name";

                    // Add data for each job
                    for (int i = 0; i < jobparameters.Count; i++)
                    {
                        var param = jobparameters[i];

                        if (param.ParameterId == 0)
                        {
                            param.ParameterId = GetNewParameterId();
                        }

                        worksheet.Cells[i + 2, 1].Value = param.ParameterId;
                        worksheet.Cells[i + 2, 2].Value = param.JobId;
                        worksheet.Cells[i + 2, 3].Value = param.ParameterName;

                    }

                    // Save the Excel file
                    FileInfo fi = new FileInfo(filepath);
                    package.SaveAs(fi);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Export job parameters to Excel error: " + ex.Message);
            }
        }

        private void ExportScheduleJobsToExcel(ObservableCollection<ScheduledJobDataModel> jobs)
        {
            try {
                // Define the path to save the Excel file
               
                string filePath = Path.Combine(_excelpath, "schedule_jobs2.xlsx");
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                // Create a new Excel package
                using (ExcelPackage package = new ExcelPackage())
                {
                    // Add a new worksheet
                    var worksheet = package.Workbook.Worksheets.Add("Schedule Jobs");

                    // Set headers
                    worksheet.Cells[1, 1].Value = "runtime";
                    worksheet.Cells[1, 2].Value = "job_id";
                    worksheet.Cells[1, 3].Value = "job_name";
                    worksheet.Cells[1, 4].Value = "python_module";
                    worksheet.Cells[1, 5].Value = "python_class";
                    worksheet.Cells[1, 6].Value = "execute_method";
                    worksheet.Cells[1, 7].Value = "day_id";
                    worksheet.Cells[1, 8].Value = "freq_id";
                    worksheet.Cells[1, 9].Value = "global_closing_delta_minutes";
                    worksheet.Cells[1, 10].Value = "backfill";

                    // Add data for each job
                    for (int i = 0; i < jobs.Count; i++)
                    {
                        var job = jobs[i];
                        worksheet.Cells[i + 2, 1].Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        
                        if (job.JobId == 0)
                        {
                            job.JobId = GetNewJobId();
                        }
       
                        worksheet.Cells[i + 2, 2].Value = job.JobId;
                        worksheet.Cells[i + 2, 3].Value = job.JobName;
                        worksheet.Cells[i + 2, 4].Value = job.PythonModule;
                        worksheet.Cells[i + 2, 5].Value = job.PythonClass;
                        worksheet.Cells[i + 2, 6].Value = job.ExecuteMethod;
                        worksheet.Cells[i + 2, 7].Value = job.DayId;
                        worksheet.Cells[i + 2, 8].Value = job.FrequencyId;
                        worksheet.Cells[i + 2, 9].Value = job.GlobalClosingDeltaMinutes;
                        worksheet.Cells[i + 2, 10].Value = job.BackFill;

                    }

                    // Save the Excel file
                    FileInfo fi = new FileInfo(filePath);
                    package.SaveAs(fi);
                }
            }
            catch(Exception ex)
            {
                throw new Exception ("Export schedulejobs to Excel error: " + ex.Message);
            }

}

        public int GetNewJobId()
        {
            int maxid = GetMaxJobId();
            switch(maxid)
            {
                case -1:
                    return 1;
                default:
                    return maxid + 1;
            }
        }
        //gets the max jobid from the collection
        public int GetMaxJobId()
        {
            if (ScheduledJobs != null && ScheduledJobs.Any())
            {
                return ScheduledJobs.Max(job => job.JobId);
            }
            else
            {
                // Handle the case where there are no jobs, return -1 or a suitable default value
                return -1;
            }
        }

        public int GetNewParameterId()
        {
            int maxid = GetMaxParameterId();
            switch (maxid)
            {
                case -1:
                    return 1;
                default:
                    return maxid + 1;
            }
        }

        public int GetMaxParameterId()
        {
            if (allJobParameters != null && allJobParameters.Any())
            {
                return allJobParameters.Max(parameter => parameter.ParameterId);
            }
            else
            {
                // Handle the case where there are no jobs, return -1 or a suitable default value
                return -1;
            }
        }

        public int GetNewParameterValueId()
        {
            int maxvalueId = GetMaxParameterValueId();
            switch(maxvalueId)
            {
                case -1:
                    return 1;
                default:
                    return maxvalueId + 1;
            }
        }

        public int GetMaxParameterValueId()
        {
            if(JobParameterValues != null & JobParameterValues.Any())
            {
                return allParameterValues.Max(paramvalues => paramvalues.ParameterValueId);
            }
            else
            {
                return -1;
            }
        }
    }
}

