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
    public class PortfolioWeightsDataModel : INotifyPropertyChanged
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
            set { if (_runtime != value) { _runtime = value; OnPropertyChanged(); } }
        }

        private string _portfolioname;
        [JsonProperty("portfolioname")]
        public string PortfolioName
        {
            get => _portfolioname;
            set { if (_portfolioname != value) { _portfolioname = value; OnPropertyChanged(); } }
        }

        private string _tickername;
        [JsonProperty("tickername")]
        public string TickerName
        {
            get => _tickername;
            set { if (_tickername != value) { _tickername = value; OnPropertyChanged(); } }
        }

        private double? _weight;
        [JsonProperty("weight")]
        public double? Weight
        {
            get => _weight;
            set { if (_weight != value) { _weight = value; OnPropertyChanged(); } }
        }

        private bool _isOriginal;
        public bool IsOriginal
        {
            get => _isOriginal;
            set { if (_isOriginal != value) { _isOriginal = value; OnPropertyChanged(); } }
        }

        private bool _isNew;
        public bool IsNew
        {
            get => _isNew;
            set { if (_isNew != value) { _isNew = value; OnPropertyChanged(); } }
        }

        private bool _isEdited;
        public bool IsEdited
        {
            get => _isEdited;
            set { if (_isEdited != value) { _isEdited = value; OnPropertyChanged(); } }
        }

        private bool _isMarkedForRemoval;
        public bool IsMarkedForRemoval
        {
            get => _isMarkedForRemoval;
            set { if (_isMarkedForRemoval != value) { _isMarkedForRemoval = value; OnPropertyChanged(); } }
        }
    }
}
