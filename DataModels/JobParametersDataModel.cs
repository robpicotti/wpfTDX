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
    public class JobParametersDataModel
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private int _parameterid;
        private int _jobid;
        private string _parametername;

        [JsonProperty("param_id")]
        public int ParameterId
        {
            get => _parameterid;
            set
            {
                if (_parameterid != value)
                {
                    _parameterid = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("param_name")]
        public string ParameterName
        {
            get => _parametername;
            set
            {
                if (_parametername != value)
                {
                    _parametername = value;
                    OnPropertyChanged();
                }
            }
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
    }
}
