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
    public class TadPositionsDataModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        private string _tickername;
        [JsonProperty("tickername")]
        public string Tickername
        {
            get => _tickername;
            set
            {
                if(value != _tickername)
                {
                    _tickername = value;
                    OnPropertyChanged(nameof(Tickername));
                }
            }
        }
        private string _subtickername;
        [JsonProperty("sub_tickername")]
        public string SubTickername
        {
            get => _subtickername;
            set
            {
                if(value != _subtickername)
                {
                    _subtickername = value;
                    OnPropertyChanged(nameof(SubTickername));
                }
            }
        }
        private string _currentcontract;
        [JsonProperty("current_contract")]
        public string CurrentContract
        {
            get => _currentcontract;
            set
            {
                if(value != _currentcontract)
                {
                    _currentcontract = value;
                    OnPropertyChanged(nameof(CurrentContract));
                }
            }
        }
        private string _tadid;
        [JsonProperty("tad_id")]
        public string TadId
        {
            get => _tadid;
            set
            {
                if(value != _tadid)
                {
                    _tadid = value;
                    OnPropertyChanged(nameof(TadId));
                }
            }
        }

        private string _bbgsymbol;
        [JsonProperty("bbg_symbol")]
        public string BbgSymbol
        {
            get => _bbgsymbol;
            set
            {
                if(value != _bbgsymbol)
                {
                    _bbgsymbol = value;
                    OnPropertyChanged(nameof(BbgSymbol));
                }
            }
        }

        private string _rolldate;
        [JsonProperty("roll_date")]
        public string Rolldate
        {
            get => _rolldate;
            set
            {
                if(value != _rolldate)
                {
                    _rolldate = value;
                    OnPropertyChanged(nameof(Rolldate));
                }
            }
        }

        //view Position stuff
        private float _view_position_base_D1;
        [JsonProperty("view_position_base_D1")]
        public float ViewPositionBaseD1
        {
            get => _view_position_base_D1;
            set
            {
                if(value != _view_position_base_D1)
                {
                    _view_position_base_D1 = value;
                    OnPropertyChanged(nameof(ViewPositionBaseD1));
                }
            }
        }
        private float _view_position_base_y1;
        [JsonProperty("view_position_base_y1")]
        public float ViewPositionBaseY1
        {
            get => _view_position_base_y1;
            set
            {
                if(value != _view_position_base_y1)
                {
                    _view_position_base_y1 = value;
                    OnPropertyChanged(nameof(ViewPositionBaseY1));
                }
            }
        }
        private float _view_position_base_h1;
        [JsonProperty("view_position_base_h1")]
        public float ViewPositionBaseH1
            {
            get => _view_position_base_h1;
            set
            {
                if(value != _view_position_base_h1)
                {
                    _view_position_base_h1 = value;
                    OnPropertyChanged(nameof(ViewPositionBaseH1));
                }
            }
        }
        private float _view_position_y1;
        [JsonProperty("view_position_y1")]
        public float ViewPositionY1
        {
            get => _view_position_y1;
            set
            {
                if(value != _view_position_y1)
                {
                    _view_position_y1 = value;
                    OnPropertyChanged(nameof(ViewPositionY1));
                }
            }
        }
        private float _view_position_y2;
        [JsonProperty("view_position_y2")]
        public float ViewPositionY2
        {
            get => _view_position_y2;
            set
            {
                if(value != _view_position_y2)
                {
                    _view_position_y2 = value;
                    OnPropertyChanged(nameof(ViewPositionY2));
                }
            }
        }
        private float _view_position_y3;
        [JsonProperty("view_position_y3")]
        public float ViewPositionY3
        {
            get => _view_position_y3;
            set
            {
                if(value != _view_position_y3)
                {
                    _view_position_y3 = value;
                    OnPropertyChanged(nameof(ViewPositionY3));
                }
            }
        }
        private float _view_position_h2;
        [JsonProperty("view_position_h2")]
        public float ViewPositionH2
        {
            get => _view_position_h2;
            set
            {
                if(value != _view_position_h2)
                {
                    _view_position_h2 = value;
                    OnPropertyChanged(nameof(ViewPositionH2));
                }
            }
        }
        private float _view_position_h3;
        [JsonProperty("view_position_h3")]
        public float ViewPositionH3
        {
            get => _view_position_h3;
            set
            {
                if(value != _view_position_h3)
                {
                    _view_position_h3 = value;
                    OnPropertyChanged(nameof(ViewPositionH3));
                }
            }
        }
        private float _view_position_h4;
        [JsonProperty("view_position_h4")]
        public float ViewPositionH4
            {
            get => _view_position_h4;
            set
            {
                if(value != _view_position_h4)
                {
                    _view_position_h4 = value;
                    OnPropertyChanged(nameof(ViewPositionH4));
                }
            }
        }
        private float _view_position_h5;
        [JsonProperty("view_position_h5")]
        public float ViewPositionH5
            {
            get => _view_position_h5;
            set
            {
                if(value != _view_position_h5)
                {
                    _view_position_h5 = value;
                    OnPropertyChanged(nameof(ViewPositionH5));
                }
            }
        }
        private float _view_position_h6;
        [JsonProperty("view_position_h6")]
        public float ViewPositionH6
        {   get => _view_position_h6;
            set
            {
                if(value != _view_position_h6)
                {
                    _view_position_h6 = value;
                    OnPropertyChanged(nameof(ViewPositionH6));
                }
            }
        }
        private float _view_position_h12;
        [JsonProperty("view_position_h12")]
        public float ViewPositionH12
        {
            get => _view_position_h12;
            set
            {
                if(value != _view_position_h12)
                {
                    _view_position_h12 = value;
                    OnPropertyChanged(nameof(ViewPositionH12));
                }
            }
        }
        private float _view_position_h16;
        [JsonProperty("view_position_h16")]
        public float ViewPositionH16
            {
            get => _view_position_h16;
            set
            {
                if(value != _view_position_h16)
                {
                    _view_position_h16 = value;
                    OnPropertyChanged(nameof(ViewPositionH16));
                }
            }
        }
        private float _view_position_D1;
        [JsonProperty("view_position_D1")]
        public float ViewPositionD1
        {
            get => _view_position_D1;
            set
            {
                if(value != _view_position_D1)
                {
                    _view_position_D1 = value;
                    OnPropertyChanged(nameof(ViewPositionD1));
                }
            }
        }
        private float _view_position_h36;
        [JsonProperty("view_position_h36")]
        public float ViewPositionH36
        {
            get => _view_position_h36;
            set
            {
                if(value != _view_position_h36)
                {
                    _view_position_h36 = value;
                    OnPropertyChanged(nameof(ViewPositionH36));
                }
            }
        }
        private float _view_position_D2;
        [JsonProperty("view_position_D2")]
        public float ViewPositionD2
        {
            get => _view_position_D2;
            set
            {
                if(value != _view_position_D2)
                {
                    _view_position_D2 = value;
                    OnPropertyChanged(nameof(ViewPositionD2));
                }
            }
        }
        private float _view_position_D3;
        [JsonProperty("view_position_D3")]
        public float ViewPositionD3
        {
            get => _view_position_D3;
            set
            {
                if(value != _view_position_D3)
                {
                    _view_position_D3 = value;
                    OnPropertyChanged(nameof(ViewPositionD3));
                }
            }
        }
        private float _view_position_D4;
        [JsonProperty("view_position_D4")]
        public float ViewPositionD4
        {
            get => _view_position_D4;
            set
            {
                if(value != _view_position_D4)
                {
                    _view_position_D4 = value;
                    OnPropertyChanged(nameof(ViewPositionD4));
                }
            }
        }
        private float _view_position_W1;
        [JsonProperty("view_position_W1")]
        public float ViewPositionW1
        {
            get => _view_position_W1;
            set
            {
                if(value != _view_position_W1)
                {
                    _view_position_W1 = value;
                    OnPropertyChanged(nameof(ViewPositionW1));
                }
            }
        }
        private float _view_position_D8;
        [JsonProperty("view_position_D8")]
        public float ViewPositionD8
        {
            get => _view_position_D8;
            set
            {
                if(value != _view_position_D8)
                {
                    _view_position_D8 = value;
                    OnPropertyChanged(nameof(ViewPositionD8));
                }
            }
        }
        private float _view_position_W2;
        [JsonProperty("view_position_W2")]
        public float ViewPositionW2
        {
            get => _view_position_W2;
            set
            {
                if(value != _view_position_W2)
                {
                    _view_position_W2 = value;
                    OnPropertyChanged(nameof(ViewPositionW2));
                }
            }
        }
        private float _view_deployment;
        [JsonProperty("view_deployment")]
        public float ViewDeployment
        {
            get => _view_deployment;
            set
            {
                if(value != _view_deployment)
                {
                    _view_deployment = value;
                    OnPropertyChanged(nameof(ViewDeployment));
                }
            }
        }
        private float _num_view_trades;
        [JsonProperty("num_view_trades")]
        public float NumViewTrades
        {
            get => _num_view_trades;
            set
            {
                if(value != _num_view_trades)
                {
                    _num_view_trades = value;
                    OnPropertyChanged(nameof(NumViewTrades));
                }
            }
        }
        private float _num_views;        
        [JsonProperty("num_views")]
        public float NumViews
        {
            get => _num_views;
            set
            {
                if(value != _num_views)
                {
                    _num_views = value;
                    OnPropertyChanged(nameof(NumViews));
                }
            }
        }
        //position stuff
        private float? _position_base_y1;
        [JsonProperty("position_base_y1")]
        public float? PositionBaseY1
        {
            get => _position_base_y1;
            set
            {
                if(value != _position_base_y1)
                {
                    _position_base_y1 = value;
                    OnPropertyChanged(nameof(PositionBaseY1));
                }
            }
        }
        private float? _position_base_h1;
        [JsonProperty("position_base_h1")]
        public float? PositionBaseH1
        {
            get => _position_base_h1;
            set
            {
                if(value != _position_base_h1)
                {
                    _position_base_h1 = value;
                    OnPropertyChanged(nameof(PositionBaseH1));
                }
            }
        }
        private float? _position_base_D1;
        [JsonProperty("position_base_D1")]
        public float? PositionBaseD1
        {
            get => _position_base_D1;
            set
            {
                if(value != _position_base_D1)
                {
                    _position_base_D1 = value;
                    OnPropertyChanged(nameof(PositionBaseD1));
                }
            }
        }
        private float? _position_y1;
        [JsonProperty("position_y1")]
        public float? PositionY1
        {
            get => _position_y1;
            set
            {
                if(value != _position_y1)
                {
                    _position_y1 = value;
                    OnPropertyChanged(nameof(PositionY1));
                }
            }
        }
        private float? _position_y2;
        [JsonProperty("position_y2")]
        public float? PositionY2
        {
            get => _position_y2;
            set
            {
                if(value != _position_y2)
                {
                    _position_y2 = value;
                    OnPropertyChanged(nameof(PositionY2));
                }
            }
        }
        private float? _position_y3;
        [JsonProperty("position_y3")]
        public float? PositionY3
        {
            get => _position_y3;
            set
            {
                if(value != _position_y3)
                {
                    _position_y3 = value;
                    OnPropertyChanged(nameof(PositionY3));
                }
            }
        }
        private float? _position_h2;
        [JsonProperty("position_h2")]
        public float? PositionH2
        {
            get => _position_h2;
            set
            {
                if(value != _position_h2)
                {
                    _position_h2 = value;
                    OnPropertyChanged(nameof(PositionH2));
                }
            }
        }
        private float? _position_h3;
        [JsonProperty("position_h3")]
        public float? PositionH3
        {
            get => _position_h3;
            set
            {
                if(value != _position_h3)
                {
                    _position_h3 = value;
                    OnPropertyChanged(nameof(PositionH3));
                }
            }
        }
        private float? _position_h4;
        [JsonProperty("position_h4")]
        public float? PositionH4
        {
            get => _position_h4;
            set
            {
                if(value != _position_h4)
                {
                    _position_h4 = value;
                    OnPropertyChanged(nameof(PositionH4));
                }
            }
        }
        private float? _position_h5;
        [JsonProperty("position_h5")]
        public float? PositionH5
        {
            get => _position_h5;
            set
            {
                if(value != _position_h5)
                {
                    _position_h5 = value;
                    OnPropertyChanged(nameof(PositionH5));
                }
            }
        }
        private float? _position_h6;
        [JsonProperty("position_h6")]
        public float? PositionH6
        {
            get => _position_h6;
            set
            {
                if(value != _position_h6)
                {
                    _position_h6 = value;
                    OnPropertyChanged(nameof(PositionH6));
                }
            }
        }
        private float? _position_h12;
        [JsonProperty("position_h12")]
        public float? PositionH12
        {
            get => _position_h12;
            set
            {
                if(value != _position_h12)
                {
                    _position_h12 = value;
                    OnPropertyChanged(nameof(PositionH12));
                }
            }
        }
        private float? _position_h16;
        [JsonProperty("position_h16")]
        public float? PositionH16
        {
            get => _position_h16;
            set
            {
                if(value != _position_h16)
                {
                    _position_h16 = value;
                    OnPropertyChanged(nameof(PositionH16));
                }
            }
        }
        private float? _position_D1;
        [JsonProperty("position_D1")]
        public float? PositionD1
        {
            get => _position_D1;
            set
            {
                if(value != _position_D1)
                {
                    _position_D1 = value;
                    OnPropertyChanged(nameof(PositionD1));
                }
            }
        }
        private float? _position_h36;    
        [JsonProperty("position_h36")]
        public float? PositionH36
        {
            get => _position_h36;
            set
            {
                if(value != _position_h36)
                {
                    _position_h36 = value;
                    OnPropertyChanged(nameof(PositionH36));
                }
            }
        }
        private float? _position_D2;
        [JsonProperty("position_D2")]
        public float? PositionD2
        {
            get => _position_D2;
            set
            {
                if(value != _position_D2)
                {
                    _position_D2 = value;
                    OnPropertyChanged(nameof(PositionD2));
                }
            }
        }
        private float? _position_D3;
        [JsonProperty("position_D3")]
        public float? PositionD3
        {
            get => _position_D3;
            set
            {
                if(value != _position_D3)
                {
                    _position_D3 = value;
                    OnPropertyChanged(nameof(PositionD3));
                }
            }
        }
        private float? _position_D4;
        [JsonProperty("position_D4")]
        public float? PositionD4
        {
            get => _position_D4;
            set
            {
                if(value != _position_D4)
                {
                    _position_D4 = value;
                    OnPropertyChanged(nameof(PositionD4));
                }
            }
        }
        private float? _position_W1;
        [JsonProperty("position_W1")]
        public float? PositionW1
        {
            get => _position_W1;
            set
            {
                if(value != _position_W1)
                {
                    _position_W1 = value;
                    OnPropertyChanged(nameof(PositionW1));
                }
            }
        }
        private float? _position_D8; 
        [JsonProperty("position_D8")]
        public float? PositionD8
        {
            get => _position_D8;
            set
            {
                if(value != _position_D8)
                {
                    _position_D8 = value;
                    OnPropertyChanged(nameof(PositionD8));
                }
            }
        }
        private float? _position_W2;
        [JsonProperty("position_W2")]
        public float? PositionW2
        {
            get => _position_W2;
            set
            {
                if(value != _position_W2)
                {
                    _position_W2 = value;
                    OnPropertyChanged(nameof(PositionW2));
                }
            }
        }
        private float? _position_deployment;
        [JsonProperty("position_deployment")]
        public float? PositionDeployment
            {
            get => _position_deployment;
            set
            {
                if(value != _position_deployment)
                {
                    _position_deployment = value;
                    OnPropertyChanged(nameof(PositionDeployment));
                }
            }
        }
        private float? _position_num_trades;
        [JsonProperty("num_trades")]
        public float? PositionNumTrades
        {
            get => _position_num_trades;
            set
            {
                if(value != _position_num_trades)
                {
                    _position_num_trades = value;
                    OnPropertyChanged(nameof(PositionNumTrades));
                }
            }
        }
        private float? _position_num_intervals;
        [JsonProperty("num_posintervals")]
        public float? PositionNumIntervals
        {
            get => _position_num_intervals;
            set
            {
                if(value != _position_num_intervals)
                {
                    _position_num_intervals = value;
                    OnPropertyChanged(nameof(PositionNumIntervals));
                }
            }
        }


        //filter position stuff
        private float _filter_position_base_y1;
        [JsonProperty("filter_position_base_y1")]
        public float FilterPositionBaseY1
            {
            get => _filter_position_base_y1;
            set
            {
                if(value != _filter_position_base_y1)
                {
                    _filter_position_base_y1 = value;
                    OnPropertyChanged(nameof(FilterPositionBaseY1));
                }
            }
        }
        private float _filter_position_base_h1;
        [JsonProperty("filter_position_base_h1")]
        public float FilterPositionBaseH1
        {
            get => _filter_position_base_h1;
            set
            {
                if(value != _filter_position_base_h1)
                {
                    _filter_position_base_h1 = value;
                    OnPropertyChanged(nameof(FilterPositionBaseH1));
                }
            }
        }
        private float _filter_position_base_D1;
        [JsonProperty("filter_position_base_D1")]
        public float FilterPositionBaseD1
        {
            get => _filter_position_base_D1;
            set
            {
                if(value != _filter_position_base_D1)
                {
                    _filter_position_base_D1 = value;
                    OnPropertyChanged(nameof(FilterPositionBaseD1));
                }
            }
        }
        private float _filter_position_y1;
        [JsonProperty("filter_position_y1")]
        public float FilterPositionY1
        {
            get => _filter_position_y1;
            set
            {
                if(value != _filter_position_y1)
                {
                    _filter_position_y1 = value;
                    OnPropertyChanged(nameof(FilterPositionY1));
                }
            }
        }
        private float _filter_position_y2;
        [JsonProperty("filter_position_y2")]
        public float FilterPositionY2
        {
            get => _filter_position_y2;
            set
            {
                if(value != _filter_position_y2)
                {
                    _filter_position_y2 = value;
                    OnPropertyChanged(nameof(FilterPositionY2));
                }
            }
        }
        private float _filter_position_y3;
        [JsonProperty("filter_position_y3")]
        public float FilterPositionY3
        {             get => _filter_position_y3;
            set
            {
                if(value != _filter_position_y3)
                {
                    _filter_position_y3 = value;
                    OnPropertyChanged(nameof(FilterPositionY3));
                }
            }
        }
        private float _filter_position_h2;
        [JsonProperty("filter_position_h2")]
        public float FilterPositionH2
        {
            get => _filter_position_h2;
            set
            {
                if(value != _filter_position_h2)
                {
                    _filter_position_h2 = value;
                    OnPropertyChanged(nameof(FilterPositionH2));
                }
            }
        }
        private float _filter_position_h3;
        [JsonProperty("filter_position_h3")]
        public float FilterPositionH3
            {
            get => _filter_position_h3;
            set
            {
                if(value != _filter_position_h3)
                {
                    _filter_position_h3 = value;
                    OnPropertyChanged(nameof(FilterPositionH3));
                }
            }
        }
        private float _filter_position_h4;
        [JsonProperty("filter_position_h4")]
        public float FilterPositionH4
        {
            get => _filter_position_h4;
            set
            {
                if(value != _filter_position_h4)
                {
                    _filter_position_h4 = value;
                    OnPropertyChanged(nameof(FilterPositionH4));
                }
            }
        }
        private float _filter_position_h5;
        [JsonProperty("filter_position_h5")]
        public float FilterPositionH5
        {
            get => _filter_position_h5;
            set
            {
                if(value != _filter_position_h5)
                {
                    _filter_position_h5 = value;
                    OnPropertyChanged(nameof(FilterPositionH5));
                }
            }
        }
        private float _filter_position_h6;
        [JsonProperty("filter_position_h6")]
        public float FilterPositionH6
        {
            get => _filter_position_h6;
            set
            {
                if(value != _filter_position_h6)
                {
                    _filter_position_h6 = value;
                    OnPropertyChanged(nameof(FilterPositionH6));
                }
            }
        }
        private float _filter_position_h12;
        [JsonProperty("filter_position_h12")]
        public float FilterPositionH12
        {
            get => _filter_position_h12;
            set
            {
                if(value != _filter_position_h12)
                {
                    _filter_position_h12 = value;
                    OnPropertyChanged(nameof(FilterPositionH12));
                }
            }
        }
        private float _filter_position_h16;
        [JsonProperty("filter_position_h16")]
        public float FilterPositionH16
        {
            get => _filter_position_h16;
            set
            {
                if(value != _filter_position_h16)
                {
                    _filter_position_h16 = value;
                    OnPropertyChanged(nameof(FilterPositionH16));
                }
            }
        }
        private float _filter_position_D1;
        [JsonProperty("filter_position_D1")]
        public float FilterPositionD1
        {
            get => _filter_position_D1;
            set
            {
                if(value != _filter_position_D1)
                {
                    _filter_position_D1 = value;
                    OnPropertyChanged(nameof(FilterPositionD1));
                }
            }
        }
        private float _filter_position_h36;
        [JsonProperty("filter_position_h36")]
        public float FilterPositionH36
            {
            get => _filter_position_h36;
            set
            {
                if(value != _filter_position_h36)
                {
                    _filter_position_h36 = value;
                    OnPropertyChanged(nameof(FilterPositionH36));
                }
            }
        }
        private float _filter_position_D2;
        [JsonProperty("filter_position_D2")]
        public float FilterPositionD2
            {
            get => _filter_position_D2;
            set
            {
                if(value != _filter_position_D2)
                {
                    _filter_position_D2 = value;
                    OnPropertyChanged(nameof(FilterPositionD2));
                }
            }
        }
        private float _filter_position_D3;
        [JsonProperty("filter_position_D3")]
        public float FilterPositionD3
        {
            get => _filter_position_D3;
            set
            {
                if(value != _filter_position_D3)
                {
                    _filter_position_D3 = value;
                    OnPropertyChanged(nameof(FilterPositionD3));
                }
            }
        }
        private float _filter_position_D4;
        [JsonProperty("filter_position_D4")]
        public float FilterPositionD4
        {
            get => _filter_position_D4;
            set
            {
                if(value != _filter_position_D4)
                {
                    _filter_position_D4 = value;
                    OnPropertyChanged(nameof(FilterPositionD4));
                }
            }
        }
        private float _filter_position_W1;
        [JsonProperty("filter_position_W1")]
        public float FilterPositionW1
        {             get => _filter_position_W1;
            set
            {
                if(value != _filter_position_W1)
                {
                    _filter_position_W1 = value;
                    OnPropertyChanged(nameof(FilterPositionW1));
                }
            }
        }
        private float _filter_position_D8;
        [JsonProperty("filter_position_D8")]
        public float FilterPositionD8
            {
            get => _filter_position_D8;
            set
            {
                if(value != _filter_position_D8)
                {
                    _filter_position_D8 = value;
                    OnPropertyChanged(nameof(FilterPositionD8));
                }
            }
        }
        private float _filter_position_W2;
        [JsonProperty("filter_position_W2")]
        public float FilterPositionW2
        {
            get => _filter_position_W2;
            set
            {
                if(value != _filter_position_W2)
                {
                    _filter_position_W2 = value;
                    OnPropertyChanged(nameof(FilterPositionW2));
                }
            }
        }
        private float _filtered_deployment; 
        [JsonProperty("filtered_deployment")]
        public float FilteredDeployment
        {
            get => _filtered_deployment;
            set
            {
                if(value != _filtered_deployment)
                {
                    _filtered_deployment = value;
                    OnPropertyChanged(nameof(FilteredDeployment));
                }
            }
        }
        private float _num_filtered_trades;
        [JsonProperty("num_filtered_trades")]
        public float NumFilteredTrades
        {
            get => _num_filtered_trades;
            set
            {
                if(value != _num_filtered_trades)
                {
                    _num_filtered_trades = value;
                    OnPropertyChanged(nameof(NumFilteredTrades));
                }
            }
        }
        private float _num_filtintervals;
        [JsonProperty("num_filtintervals")]
        public float NumFiltIntervals
        {
            get => _num_filtintervals;
            set
            {
                if(value != _num_filtintervals)
                {
                    _num_filtintervals = value;
                    OnPropertyChanged(nameof(NumFiltIntervals));
                }
            }
        }

    }
}
