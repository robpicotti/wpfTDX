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
    public class ScheduleFrequenciesDataModel
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private int _frequencyid;
        private string _frequencyname;
        private float _hourvalue;

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

        [JsonProperty("freq_name")]
        public string FrequencyName
        {
            get => _frequencyname;
            set
            {
                if (_frequencyname != value)
                {
                    _frequencyname = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("hour_value")]
        public float HourValue
        {
            get => _hourvalue;
            set
            {
                if (_hourvalue != value)
                {
                    _hourvalue = value;
                    OnPropertyChanged();
                }
            }
        }


    }
}
