using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace wpfTDX
{
    public class ScheduledJobDataModel
    {
        private int _jobid;
        private string _jobname;
        private string _pythonmodule;
        private string _executemethod;
        private int _dayid;
        private int _frequencyid;
        private int _globalclosingdeltaminutes;
        private bool _backfill;

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        [JsonProperty("job_id")]
        public int JobId
        {
            get => _jobid;
            set
            {
                if (_jobid != value)
                {
                    _jobid = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("job_name")]
        public string JobName
        {
            get => _jobname;
            set
            {
                if (_jobname != value)
                {
                    _jobname = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("python_module")]
        public string PythonModule
        {
            get => _pythonmodule;
            set
            {
                if (_pythonmodule != value)
                {
                    _pythonmodule = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("execute_method")]
        public string ExecuteMethod
        {
            get => _executemethod;
            set
            {
                if (_executemethod != value)
                {
                    _executemethod = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("day_id")]
        public int DayId
        {
            get => _dayid;
            set
            {
                if (_dayid != value)
                {
                    _dayid = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("freq_id")]
        public int FrequencyId
        {
            get => _frequencyid;
            set
            {
                if (_frequencyid != value)
                {
                    _frequencyid = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("global_closing_delta_minutes")]
        public int GlobalClosingDeltaMinutes
        {
            get => _globalclosingdeltaminutes;
            set
            {
                if (_globalclosingdeltaminutes != value)
                {
                    _globalclosingdeltaminutes = value;
                    OnPropertyChanged();
                }
            }
        }

    }
}
