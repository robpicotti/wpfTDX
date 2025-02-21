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
    public class PositionsDataModel: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private string _fundname;
        [JsonProperty("fundname")]
        public string FundName
        {
            get => _fundname;
            set
            {
                if (_fundname != value)
                {
                    _fundname = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _subaccountname;
        [JsonProperty("subaccountname")]
        public string SubaccountName
        {
            get => _subaccountname;
            set
            {
                if(_subaccountname != value)
                {
                    _subaccountname = value;
                    OnPropertyChanged();
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
                if(_tadid != value)
                {
                    _tadid = value;
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
        private double _positionlive;
        [JsonProperty("position_live")]
        public double PositionLive
        {
            get => _positionlive;
            set
            {
                if (_positionlive != value)
                {
                    _positionlive = value;
                    OnPropertyChanged();
                }
            }
        }
        private double _positionlivetm1;
        [JsonProperty("position_live_tm1")]
        public double PositionLiveTm1
        {
            get => _positionlivetm1;
            set
            {
                if (_positionlivetm1 != value)
                {
                    _positionlivetm1 = value;
                    OnPropertyChanged();
                }
            }
        }
        private double? _postiontarget;
        [JsonProperty("position_target")]
        public double? PositionTarget
        {
            get => _postiontarget;
            set
            {
                if (_postiontarget != value)
                {
                    _postiontarget = value;
                    OnPropertyChanged();
                }
            }
        }
        private double? _postiontargetraw;
        [JsonProperty("position_target_raw")]
        public double? PositionTargetRaw
        {
            get => _postiontargetraw;
            set
            {
                if (_postiontargetraw != value)
                {
                    _postiontargetraw = value;
                    OnPropertyChanged();
                }
            }
        }
        private double _numorders;
        [JsonProperty("num_orders")]
        public double NumOrders
        {
            get => _numorders;
            set
            {
                if (_numorders != value)
                {
                    _numorders = value;
                    OnPropertyChanged();
                }
            }
        }
        private double _pricelive;
        [JsonProperty("price_live")]
        public double PriceLive
        {
            get => _pricelive;
            set
            {
                if (_pricelive != value)
                {
                    _pricelive = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _exchangecurrency;
        [JsonProperty("exch_currency")]
        public string ExchangeCurrency
        {
            get => _exchangecurrency;
            set
            {
                if(_exchangecurrency!=value)
                {
                    _exchangecurrency = value;
                    OnPropertyChanged(nameof(ExchangeCurrency));
                }
            }
        }
        private DateTime _bbglasttradedate;
        [JsonProperty("bbg_lasttrade_date")]
        public DateTime BbgLastTradeDate
        {
            get => _bbglasttradedate;
            set
            {
                if(_bbglasttradedate != value)
                {
                    _bbglasttradedate = value;
                    OnPropertyChanged(nameof(BbgLastTradeDate));
                }
            }
        }
        private double _multiplier;
        [JsonProperty("multiplier")]
        public double Multiplier
        {
            get => _multiplier;
            set
            {
                if(_multiplier!=value)
                {
                    _multiplier = value;
                    OnPropertyChanged(nameof(Multiplier));
                }
            }
        }
    }
}
