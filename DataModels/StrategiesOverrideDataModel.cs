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
    public class StrategiesOverrideDataModel : INotifyPropertyChanged
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
        private string _tickername;
        [JsonProperty("tickername")]
        public string TickerName
        {
            get => _tickername;
            set
            {
                if (_tickername != value)
                {
                    _tickername = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _env;
        [JsonProperty("env")]
        public string Env
        {
            get => _env;
            set
            {
                if (_env != value)
                {
                    _env = value;
                    OnPropertyChanged();
                }
            }
        }
        private int _sortkey;
        [JsonProperty("sort_key")]
        public int SortKey
        {
            get => _sortkey;
            set
            {
                if (_sortkey != value)
                {
                    _sortkey = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool? _enable;
        [JsonProperty("enable")]
        public bool? Enable
        {
            get => _enable;
            set
            {
                if(value != _enable)
                {
                    _enable = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _strategyname;
        [JsonProperty("strategyname")]
        public string StrategyName
        {
            get => _strategyname;
            set
            {
                if(_strategyname != value)
                {
                    _strategyname = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _strategynamebase;
        [JsonProperty("strategyname_base")]
        public string StrategyNameBase
        {
            get => _strategynamebase;
            set
            {
                if (value != _strategynamebase)
                {
                    _strategynamebase = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _strategynamebasedaily;
        [JsonProperty("strategyname_base_daily")]
        public string StrategyNameBaseDaily
        {
            get => _strategynamebasedaily;
            set
            {
                if (value != _strategynamebasedaily)
                {
                    _strategynamebasedaily = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool? _watchlist;
        [JsonProperty("watchlist")]
        public bool? Watchlist
        {
            get => _watchlist;
            set
            {
                if(_watchlist != value)
                {
                    _watchlist = value; 
                    OnPropertyChanged();
                }
            }
        }
        private bool? _keepupdated;
        [JsonProperty("keep_updated")]
        public bool? KeepUpdated
        {
            get => _keepupdated;
            set
            {
                if (_keepupdated != value)
                {
                    _keepupdated = value;   
                    OnPropertyChanged();
                }
            }
        }
        private bool? _calctrades;
        [JsonProperty("calc_trades")]
        public bool? CalcTrades
        {
            get => _calctrades;
            set
            {
                if(_calctrades != value)
                {
                    _calctrades = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool? _takeposition;
        [JsonProperty("take_position")]
        public bool? TakePosition
        {
            get => _takeposition;
            set
            {
                if( _takeposition != value)
                {
                    _takeposition = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool? _continuousupdate;
        [JsonProperty("continuous_update")]
        public bool? ContinuousUpdate
        {
            get => _continuousupdate;
            set
            {
                if (_continuousupdate != value)
                {
                    _continuousupdate = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _minupdatefreq;
        [JsonProperty("min_update_freq")]
        public string MinUpdateFreq
        {
            get => _minupdatefreq;
            set
            {
                if( _minupdatefreq != value)
                {
                    _minupdatefreq = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _minchartfreq;
        [JsonProperty("min_chart_freq")]
        public string MinChartFreq
        {
            get => _minchartfreq;
            set
            {
                if( _minchartfreq != value)
                {
                    _minchartfreq = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _minposfreq;
        [JsonProperty("min_pos_freq")]
        public string MinPosFreq
        {
            get => _minposfreq;
            set
            {
                if(_minposfreq != value)
                {
                    _minposfreq = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
