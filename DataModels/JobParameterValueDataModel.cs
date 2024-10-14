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
    public class JobParameterValueDataModel
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private int _parametervalueid;
        private int _parameterid;
        private string _parametervalue;
        private string _environment;

        [JsonProperty("param_value_id")]
        public int ParameterValueId
        {
            get => _parametervalueid;
            set
            {
                if (_parametervalueid != value)
                {
                    _parametervalueid = value;
                    OnPropertyChanged();
                }
            }
        }

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

        [JsonProperty("param_value")]
        public string ParameterValue
        {
            get => _parametervalue;
            set
            {
                if (_parametervalue != value)
                {
                    _parametervalue = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("environment")]
        public string Environment
        {
            get => _environment;
            set
            {
                if (_environment != value)
                {
                    _environment = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
