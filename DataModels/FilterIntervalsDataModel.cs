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
    public class FilterIntervalsDataModel :INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        private DateTime _runtime;
        [JsonProperty("runtime")]
        public DateTime Runtime
        {
            get { return _runtime; }
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
            get { return _tickername; }
            set
            {
                if (_tickername != value)
                {
                    _tickername = value;
                    OnPropertyChanged(nameof(TickerName));
                }
            }
        }
        private string _fundgroup;
        [JsonProperty("fundgroups")]
        public string FundGroup
        {
            get { return _fundgroup; }
            set
            {
                if (_fundgroup != value)
                {
                    _fundgroup = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool? _rescale;
        [JsonProperty("rescale")]
        public bool? Rescale
        {
            get { return _rescale; }
            set
            {
                if (_rescale != value)
                {
                    _rescale = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool? _long_only;
        [JsonProperty("long_only")]
        public bool? LongOnly
        {
            get { return _long_only; }
            set
            {
                if (_long_only != value)
                {
                    _long_only = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool? _short_only;
        [JsonProperty("short_only")]
        public bool? ShortOnly
        {
            get { return _short_only; }
            set
            {
                if (_short_only != value)
                {
                    _short_only = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool? _buy_only;
        [JsonProperty("buy_only")]
        public bool? BuyOnly
        {
            get { return _buy_only; }
            set
            {
                if (_buy_only != value)
                {
                    _buy_only = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool? _sell_only;
        [JsonProperty("sell_only")]
        public bool? SellOnly
        {
            get { return _sell_only; }
            set
            {
                if (_sell_only != value)
                {
                    _sell_only = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool? _all_intervals;
        [JsonProperty("all_intervals")]
        public bool? AllIntervals
        {
            get { return _all_intervals; }
            set
            {
                if (_all_intervals != value)
                {
                    _all_intervals = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool? _base_y1;
        [JsonProperty("base_y1")]
        public bool? BaseY1
        {
            get { return _base_y1; }
            set
            {
                if (_base_y1 != value)
                {
                    _base_y1 = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool? _base_h1;
        [JsonProperty("base_h1")]
        public bool? BaseH1
        {
            get { return _base_h1; }
            set
            {
                if (_base_h1 != value)
                {
                    _base_h1 = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool? _base_D1;
        [JsonProperty("base_D1")]
        public bool? BaseD1
        {
            get { return _base_D1; }
            set
            {
                if (_base_D1 != value)
                {
                    _base_D1 = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool? _y1;
        [JsonProperty("y1")]
        public bool? Y1
        {
            get { return _y1; }
            set
            {
                if (_y1 != value)
                {
                    _y1 = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool? _y2;
        [JsonProperty("y2")]
        public bool? Y2
        {
            get { return _y2; }
            set
            {
                if (_y2 != value)
                {
                    _y2 = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool? _y3;
        [JsonProperty("y3")]
        public bool? Y3
        {
            get { return _y3; }
            set
            {
                if (_y3 != value)
                {
                    _y3 = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool? _h1;
        [JsonProperty("h1")]
        public bool? H1
        {
            get { return _h1; }
            set
            {
                if (_h1 != value)
                {
                    _h1 = value;
                    OnPropertyChanged(nameof(H1));
                }
            }
        }


        private bool? _h2;
        [JsonProperty("h2")]
        public bool? H2
        {
            get { return _h2; }
            set
            {
                if (_h2 != value)
                {
                    _h2 = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool? _h3;
        [JsonProperty("h3")]
        public bool? H3
        {
            get { return _h3; }
            set
            {
                if (_h3 != value)
                {
                    _h3 = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool? _h4;
        [JsonProperty("h4")]
        public bool? H4
        {
            get { return _h4; }
            set
            {
                if (_h4 != value)
                {
                    _h4 = value;
                    OnPropertyChanged();
                }
            }
        }
        //private bool? _h5;
        //[JsonProperty("h5")]
        //public bool? H5
        //{
        //    get { return _h5; }
        //    set
        //    {
        //        if (_h5 != value)
        //        {
        //            _h5 = value;
        //            OnPropertyChanged(nameof(H5));
        //        }
        //    }
        //}

        private bool? _h6;
        [JsonProperty("h6")]
        public bool? H6
        {
            get { return _h6; }
            set
            {
                if (_h6 != value)
                {
                    _h6 = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool? _h12;
        [JsonProperty("h12")]
        public bool? H12
        {
            get { return _h12; }
            set
            {
                if (_h12 != value)
                {
                    _h12 = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool? _h16;
        [JsonProperty("h16")]
        public bool? H16
        {
            get { return _h16; }
            set
            {
                if (_h16 != value)
                {
                    _h16 = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool? _D1;
        [JsonProperty("D1")]
        public bool? D1
        {
            get { return _D1; }
            set
            {
                if (_D1 != value)
                {
                    _D1 = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool? _h36;
        [JsonProperty("h36")]
        public bool? H36
        {
            get { return _h36; }
            set
            {
                if (_h36 != value)
                {
                    _h36 = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool? _D2;
        [JsonProperty("D2")]
        public bool? D2
        {
            get { return _D2; }
            set
            {
                if (_D2 != value)
                {
                    _D2 = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool? _D3;
        [JsonProperty("D3")]
        public bool? D3
        {
            get { return _D3; }
            set
            {
                if (_D3 != value)
                {
                    _D3 = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool? _D4;
        [JsonProperty("D4")]
        public bool? D4
        {
            get { return _D4; }
            set
            {
                if (_D4 != value)
                {
                    _D4 = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool? _W1;
        [JsonProperty("W1")]
        public bool? W1
        {
            get { return _W1; }
            set
            {
                if (_W1 != value)
                {
                    _W1 = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool? _D8;
        [JsonProperty("D8")]
        public bool? D8
        {
            get { return _D8; }
            set
            {
                if (_D8 != value)
                {
                    _D8 = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool? _W2;
        [JsonProperty("W2")]
        public bool? W2
        {
            get { return _W2; }
            set
            {
                if (_W2 != value)
                {
                    _W2 = value;
                    OnPropertyChanged();
                }
            }
        }

    }
}
