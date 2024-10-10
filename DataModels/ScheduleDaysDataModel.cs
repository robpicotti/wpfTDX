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
    public class ScheduleDaysDataModel
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        private int _dayid;
        private string _dayname;

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

        [JsonProperty("day_name")]
        public string DayName
        {
            get => _dayname;
            set
            {
                if (_dayname != value)
                {
                    _dayname = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
