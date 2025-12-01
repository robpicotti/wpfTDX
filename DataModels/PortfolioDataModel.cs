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
    public class PortfolioDataModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        private DateTime _runtime;
        [JsonProperty("runtime")]
        public DateTime RunTime
        {
            get => _runtime;
            set
            {
                if (_runtime != value)
                {
                    _runtime = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _benchmarkname;
        [JsonProperty("benchmarkname")]
        public string BenchmarkName
        {
            get => _benchmarkname;
            set
            {
                if (_benchmarkname != value)
                {
                    _benchmarkname = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool _custom;
        [JsonProperty("custom")]
        public bool Custom
        {
            get => _custom;
            set
            {
                if (_custom != value)
                {
                    _custom = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
