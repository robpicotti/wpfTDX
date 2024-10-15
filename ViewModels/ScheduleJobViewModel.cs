
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
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;


namespace wpfTDX
{
    public class ScheduleJobViewModel : INotifyPropertyChanged
    {
        public ICommand SaveCommand { get; private set; }

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
                            JobParameters.Add(param);
                        }
                    }
                }
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
            ScheduledJobs = new ObservableCollection<ScheduledJobDataModel>();
            ScheduleDays = new ObservableCollection<ScheduleDaysDataModel>();
            ScheduleFrequencies = new ObservableCollection<ScheduleFrequenciesDataModel>();
            JobParameterValues = new ObservableCollection<JobParameterValueDataModel>(); // Initialize here

            GetScheduledJobs();
            GetScheduleDays();
            GetScheduleFrequencies();
            GetJobParameters();
            GetJobParameterValues();
        }
        private void SaveAllJobs()
        {
            // Logic to save all jobs to Excel
            ExportToExcel(ScheduledJobs);
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

            foreach (var value in valuesForSelectedParameter)
            {
                JobParameterValues.Add(value);
            }
        }

        private void ExportToExcel(ObservableCollection<ScheduledJobDataModel> jobs)
        {
            // Define the path to save the Excel file
            string path = @"c:\TDX\";
            string filePath = Path.Combine(path, "schedule_jobs2.xlsx");
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
    }
}

