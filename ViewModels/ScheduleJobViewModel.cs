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

namespace wpfTDX
{
    public class ScheduleJobViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

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



        private ObservableCollection<JobParametersDataModel> _jobparameters;

        public ObservableCollection<JobParametersDataModel> JobParameters
        {
            get => _jobparameters;
            set
            {
                if (_jobparameters != value)
                {
                    _jobparameters = value;
                    OnPropertyChanged(nameof(JobParameters));
                }
            }
        }


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

                    // Load job parameters based on the selected job's JobId
                    if (_selectedJob != null)
                    {
                        LoadJobParametersForSelectedJob(_selectedJob.JobId);
                    }
                }
            }
        }

        private JobParametersDataModel _selectedjobparameter;

        public JobParametersDataModel SelectedJobParameter
        {
            get => _selectedjobparameter;
            set
            {
                if(_selectedjobparameter !=value)
                {
                    _selectedjobparameter = value;
                    OnPropertyChanged(nameof(SelectedJobParameter));
                }
            }
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
            GetScheduledJobs();
            GetScheduleDays();
            GetScheduleFrequencies();
            GetJobParameters();
            GetJobParameterValues();
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
                var paramsList = JsonConvert.DeserializeObject<List<JobParametersDataModel>>(paramData.ToString());

                if (JobParameters == null)
                {
                    JobParameters = new ObservableCollection<JobParametersDataModel>();
                }
                else
                {
                    JobParameters.Clear();
                }

                // Add each job to the ObservableCollection
                foreach (var item in paramsList)
                {
                    JobParameters.Add(item);
                }
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
                JArray paramDataValue = JArray.Parse(jsonResponse);

                // Deserialize the list of dictionaries directly into your DataModel
                var paramValuesList = JsonConvert.DeserializeObject<List<JobParameterValueDataModel>>(paramDataValue.ToString());

                if (JobParameterValues == null)
                {
                    JobParameterValues = new ObservableCollection<JobParameterValueDataModel>();
                }
                else
                {
                    JobParameterValues.Clear();
                }

                // Add each job to the ObservableCollection
                foreach (var item in paramValuesList)
                {
                    JobParameterValues.Add(item);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private void LoadJobParametersForSelectedJob(int jobId)
        {
            // Assuming JobParameters is already loaded with all parameters
            if (JobParameters != null)
            {
                var filteredParameters = JobParameters.Where(p => p.JobId == jobId).ToList();

                // Clear and update JobParameters collection
                JobParameters.Clear();
                foreach (var param in filteredParameters)
                {
                    JobParameters.Add(param);
                }
            }
        }


    }
}
