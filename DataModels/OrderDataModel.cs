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
    public class OrderDataModel
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
        private string _brokername;
        [JsonProperty("broker")]
        public string BrokerName
        {
            get => _brokername;
            set
            {
                if(_brokername != value)
                {
                    _brokername = value;
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
                if(_account !=value)
                {
                    _account = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _subaccounts;
        [JsonProperty("subaccounts")]
        public string Subaccounts
        {
            get => _subaccounts;
            set
            {
                if(_subaccounts != value)
                {
                    _subaccounts = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _allocationtype;
        [JsonProperty("allocation_type")]
        public string AllocationType
        {
            get => _allocationtype;
            set
            {
                if(_allocationtype != value)
                {
                    _allocationtype = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _allocationamount;
        [JsonProperty("allocation_amount")]
        public string AllocationAmount
        {
            get => _allocationamount;
            set
            {
                if( _allocationamount != value)
                {
                    _allocationamount = value;
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
        private int _traderid;
        [JsonProperty("trader_id")]
        public int TraderId
        {
            get => _traderid;
            set
            {
                if(_traderid != value)
                {
                    _traderid = value;
                    OnPropertyChanged();
                }
            }
        }
        private int _strategyid;
        [JsonProperty("strategy_id")]
        public int StrategyId
        {
            get => _strategyid;
            set
            {
                if(_strategyid != value)
                {
                    _strategyid = value;
                    OnPropertyChanged();
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
                if(_tickername != value)
                {
                    _tickername = value;

                }
            }
        }
        private double _price;
        [JsonProperty("price")]
        public double Price
        {
            get => _price;
            set
            {
                if(_price != value)
                {
                    _price = value;
                    OnPropertyChanged();
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
                if(_multiplier != value)
                {
                    _multiplier = value;
                    OnPropertyChanged();
                }
            }
        }
        private double _size;
        [JsonProperty("size")]
        public double Size
        {
            get => _size;
            set
            {
                if(_size != value)
                {
                    _size = value;
                    OnPropertyChanged();
                }
            }
        }
        private double _executedsize;
        [JsonProperty("executed_size")]
        public double ExecutedSize
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
        private string _tif;
        [JsonProperty("tif")]
        public string TIF
        {
            get => _tif;
            set
            {
                if(_tif != value )
                {
                    _tif = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _ordertype;
        [JsonProperty("order_type")]
        public string OrderType
        {
            get => _ordertype;
            set
            {
                if(_ordertype != value)
                {
                    _ordertype = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _status;
        [JsonProperty("status")]
        public string Status
        {
            get => _status;
            set
            {
                if(_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }
        private DateTime? _statustime;
        [JsonProperty("status_time")]
        public DateTime? StatusTime
        {
            get => _statustime;
            set
            {
                if(_statustime != value)
                {
                    _statustime = value;
                    OnPropertyChanged();
                }
            }
        }
        private DateTime? _effectivedate;
        [JsonProperty("effective_date")]
        public DateTime? EffectiveDate
        {
            get => _effectivedate;
            set
            {
                if(_effectivedate != value)
                {
                    _effectivedate = value;
                }
            }
        }
        private DateTime? _orderdecisiontime;
        [JsonProperty("order_decision_time")]
        public DateTime? OrderDecisionTime
        {
            get => _orderdecisiontime;
            set
            {
                if(_orderdecisiontime != value)
                {
                    _orderdecisiontime = value;
                    OnPropertyChanged();
                }
            }
        }
        private DateTime? _ordersubmittime;
        [JsonProperty("order_submit_time")]
        public DateTime? OrderSubmitTime
        {
            get => _ordersubmittime;
            set
            {
                if(_ordersubmittime != value)
                {
                    _ordersubmittime = value;
                    OnPropertyChanged();
                }
            }
        }
        private DateTime? _ordercompletiontime;
        [JsonProperty("order_completion_time")]
        public DateTime? OrderCompletionTime
        {
            get => _ordercompletiontime;
            set
            {
                if(_ordercompletiontime != value)
                {
                    _ordercompletiontime = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _newpositionids;
        [JsonProperty("newposition_ids")]
        public string NewpositionIds
        {
            get => _newpositionids;
            set
            {
                if(_newpositionids != value)
                {
                    _newpositionids = value;
                    OnPropertyChanged();
                }
            }
        }
        private int _autoexecute;
        [JsonProperty("autoexecute")]
        public int AutoExecute
        {
            get => _autoexecute;
            set
            {
                if(_autoexecute!=value)
                {
                    _autoexecute = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _tradeallocations;
        [JsonProperty("trade_allocations")]
        public string TradeAllocations
        {
            get => _tradeallocations;
            set
            {
                if(_tradeallocations != value)
                {
                    _tradeallocations = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _txtypedetail;
        [JsonProperty("tx_type_detail")]
        public string TxTypeDetail
        {
            get => _txtypedetail;
            set
            {
                if(_txtypedetail != value)
                {
                    _txtypedetail = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool _canceltdx;
        [JsonProperty("cancel_tdx")]
        public bool CancelTdx
        {
            get => _canceltdx;
            set
            {
                if(_canceltdx != value)
                {
                    _canceltdx = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
