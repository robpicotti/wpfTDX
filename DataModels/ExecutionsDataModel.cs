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
    public class ExecutionsDataModel: INotifyPropertyChanged
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
        private string _account;
        [JsonProperty("account")]
        public string Account
        {
            get => _account;
            set
            {
                if (_account != value)
                {
                    _account = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _subaccount;
        [JsonProperty("subaccount")]
        public string Subaccount
        {
            get => _subaccount;
            set
            {
                if (_subaccount != value)
                {
                    _subaccount = value;
                    OnPropertyChanged();
                }
            }
        }
        private int _brokerid;
        [JsonProperty("broker_id")]
        public int BrokerId
        {
            get => _brokerid;
            set
            {
                if (_brokerid != value)
                {
                    _brokerid = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _executionid;
        [JsonProperty("execution_id")]
        public string ExecutionId
        {
            get => _executionid;
            set
            {
                if (_executionid != value)
                {
                    _executionid = value;
                    OnPropertyChanged();
                }
            }
        }
        private DateTime _executiontime;
        [JsonProperty("execution_time")]
        public DateTime ExecutionTime
        {
            get => _executiontime;
            set
            {
                if (_executiontime != value)
                {
                    _executiontime = value;
                    OnPropertyChanged();
                }
            }
        }
        private DateTime _effectivedate;
        [JsonProperty("effective_date")]
        public DateTime EffectiveDate
        {
            get => _effectivedate;
            set
            {
                if (_effectivedate != value)
                {
                    _effectivedate = value;
                    OnPropertyChanged();
                }
            }
        }
        private DateTime _settlementdate;
        [JsonProperty("settlement_date")]
        public DateTime SettlementDate
        {
            get => _settlementdate;
            set
            {
                if (_settlementdate != value)
                {
                    _settlementdate = value;
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
        private string _tadid;
        [JsonProperty("tad_id")]
        public string TadId
        {
            get => _tadid;
            set
            {
                if (_tadid != value)
                {
                    _tadid = value;
                    OnPropertyChanged();
                }
            }
        }
        private double? _multiplier;
        [JsonProperty("multiplier")]
        public double? Multiplier
        {
            get => _multiplier;
            set
            {
                if(_multiplier != value)
                {
                    _multiplier = value;
                }
            }
        }
        private string _action;
        [JsonProperty("action")]
        public string Action
        {
            get => _action;
            set
            {
                if(_action != value)
                {
                    _action = value;
                    OnPropertyChanged();
                }
            }
        }
        private double? _executedsize;
        [JsonProperty("executed_size")]
        public double? ExecutedSize
        {
            get => _executedsize;
            set
            {
                if(_executedsize != value)
                {
                    _executedsize = value;
                    OnPropertyChanged();
                }
            }
        }
        private double? _executedprice;
        [JsonProperty("executed_price")]
        public double? ExecutedPrice
        {
            get => _executedprice;
            set
            {
                if(_executedprice != value)
                {
                    _executedprice = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _brokuniqorderid;
        [JsonProperty("brok_uniq_order_id")]
        public string BrokUniqOrderId
        {
            get => _brokuniqorderid;
            set
            {
                if(_brokuniqorderid != value)
                {
                    _brokuniqorderid = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _orderid;
        [JsonProperty("order_id")]
        public string OrderId
        {
            get => _orderid;
            set
            {
                if(_orderid != value)
                {
                    _orderid = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _tadorderid;
        [JsonProperty("tad_order_id")]
        public string TadOrderId
        {
            get => _tadorderid;
            set
            {
                if(_tadorderid != value)
                {
                    _tadorderid = value;
                    OnPropertyChanged();
                }
            }
        }
        private double? _cumulativeqty;
        [JsonProperty("cumulative_qty")]
        public double? CumulativeQty
        {
            get => _cumulativeqty;
            set
            {
                if(_cumulativeqty != value)
                {
                    _cumulativeqty = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _exchange;
        [JsonProperty("exchange")]
        public string Exchange
        {
            get => _exchange;
            set
            {
                if(_exchange != value)
                {
                    _exchange = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _bbgfigi;
        [JsonProperty("bbg_figi")]
        public string BBGFigi
        {
            get => _bbgfigi;
            set
            {
                if(_bbgfigi != value)
                {
                    _bbgfigi = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _bbgisin;
        [JsonProperty("bbg_isin")]
        public string BBGIsin
        {
            get => _bbgisin;
            set
            {
                if(_bbgisin != value)
                {
                    _bbgisin = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _parentorderid;
        [JsonProperty("parent_order_id")]
        public string ParentOrderId
        {
            get => _parentorderid;
            set
            {
                if(_parentorderid != value)
                {
                    _parentorderid = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
