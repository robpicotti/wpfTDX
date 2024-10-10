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

        public ScheduleJobViewModel()
        {
            GetScheduledJobs();
            GetScheduleDays();
            GetScheduleFrequencies();
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
                HttpResponseMessage response = client.PostAsync("http://localhost:5001/select_table", content).Result;
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
                HttpResponseMessage response = client.PostAsync("http://localhost:5001/select_table", content).Result;
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

    }
}
