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
    public class MtmDataModel: INotifyPropertyChanged
    {
        private string _fundname;
        private DateTime _date_t;
        private DateTime _date_tminus1;
        private string _subaccountname;
        private string _base_currency;
        private string _exchange_currency;
        private string _tad_id;
        private string _tickername;
        private string _instrument;
        private double? _executed_quantity;
        private double? _closing_price_t;
        private double? _closing_price_tminus1;
        private double? _multiplier;
        private double? _cash_t;
        private double? _cash_tminus1;
        private double? _mtm_t;
        private double? _mtm_tminus1;
        private double? _adjusted_mtm_tminus1;
        private double? _adjusted_cash_tminus1;
        private double? _pnl_basecurrency;
        private double? _commission_basecurrency;
        private DateTime _execution_time;
        private string _spot_currency;
        private double? _fx_spot_rate;
        private double? _transaction_price;
        private double? _totalbuyandhold;
        private double? _totalnewdeals;

        public event PropertyChangedEventHandler PropertyChanged;
        [JsonProperty("fundname")]
        public string FundName
        {
            get => _fundname;
            set
            {
                if(_fundname != value)
                {
                    _fundname = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("date_t")]
        public DateTime Date_t
        {
            get => _date_t;
            set
            {
                if(_date_t !=value)
                {
                    _date_t = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("date_tminus1")]
        public DateTime Date_tminus1
        {
            get => _date_tminus1;
            set
            {
                if(_date_tminus1 != value)
                {
                    _date_tminus1 = value;
                    OnPropertyChanged();
                }
            }
        }

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

        [JsonProperty("exch_currency")]
        public string ExchangeCurrency
        {
            get => _exchange_currency;
            set
            {
                if(_exchange_currency != value)
                {
                    _exchange_currency = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("tad_id")]
        public string TadId
        {
            get => _tad_id;
            set
            {
                if(_tad_id != value)
                {
                    if(_tad_id != value)
                    {
                        _tad_id = value;
                        OnPropertyChanged();
                    }
                }
            }
        }

        [JsonProperty("tickername")]
        public string Tickername
        {
            get => _tickername;
            set
            {
                if(_tickername != value)
                {
                    _tickername = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("instrument")]
        public string Instrument
        {
            get => _instrument;
            set
            {
                if(_instrument != value)
                {
                    _instrument = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("executed_qty")]
        public double? ExecutedQuantity
        {
            get => _executed_quantity;
            set
            {
                if(_executed_quantity != value)
                {
                    _executed_quantity = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("closing_price_t")]
        public double? ClosingPriceT
        {
            get => _closing_price_t;
            set
            {
                if(_closing_price_t !=value)
                {
                    _closing_price_t = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("closing_price_tminus1")]
        public double? ClosingPriceTMinus1
        {
            get => _closing_price_tminus1;
            set
            {
                if (_closing_price_tminus1 != value)
                {
                    _closing_price_tminus1 = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("multiplier")]
        public double? Multiplier
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

        [JsonProperty("cash_t")]
        public double? CashT
        {
            get => _cash_t;
            set
            {
                if(_cash_t != value)
                {
                    _cash_t = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("cash_tminus1")]
        public double? CashTMinus1
        {
            get => _cash_tminus1;
            set
            {
                if(_cash_tminus1 != value)
                {
                    _cash_tminus1 = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("mtm_t")]
        public double? MtmT
        {
            get => _mtm_t;
            set
            {
                if(_mtm_t != value)
                {
                    _mtm_t = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("mtm_tminus1")]
        public double? MtmTMinus1
        {
            get => _mtm_tminus1;
            set
            {
                if(_mtm_tminus1 !=value)
                {
                    _mtm_tminus1 = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("adjusted_mtm_tminus1")]
        public double? AdjustedMtmTMinus1
        {
            get => _adjusted_mtm_tminus1;
            set
            {
                if(_adjusted_mtm_tminus1 != value)
                {
                    _adjusted_mtm_tminus1 = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("adjusted_cash_tminus1")]
        public double? AdjustedCashTMinus1
        {
            get => _adjusted_cash_tminus1;
            set
            {
                if(_adjusted_cash_tminus1 != value)
                {
                    _adjusted_cash_tminus1 = value;
                    OnPropertyChanged();
                }
            }
        }
        
        [JsonProperty("execution_time")]
        public DateTime ExecutionTime
        {
            get => _execution_time;
            set
            {
                if (_execution_time != value)
                {
                    _execution_time = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("pnl_basecurrency")]
        public double? PnlBaseCurrency
        {
            get => _pnl_basecurrency;
            set
            {
                if(_pnl_basecurrency != value)
                {
                    _pnl_basecurrency = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("commission_basecurrency")]
        public double? CommissionBaseCurrency
        {
            get => _commission_basecurrency;
            set
            {
                if(_commission_basecurrency != value)
                {
                    _commission_basecurrency = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("spot_curr")]
        public string SpotCurrency
        {
            get => _spot_currency;
            set
            {
                if (_spot_currency != value)
                {
                    _spot_currency = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("spot_rate")]
        public double? FXSpotRate
        {
            get => _fx_spot_rate;
            set
            {
                if(_fx_spot_rate != value)
                {
                    _fx_spot_rate = value;
                    OnPropertyChanged();
                }
            }
        }


        [JsonProperty("tx_price")]
        public double? TransactionPrice
        {
            get => _transaction_price;
            set
            {
                if (_transaction_price != value)
                {
                    _transaction_price = value;
                    OnPropertyChanged();
                }
            }
        }



        public string BaseCurrency
        {
            get => _base_currency;
            set
            {
                if(_base_currency != value)
                {
                    _base_currency = value;
                    OnPropertyChanged();
                }
            }
        }

        public void OnPropertyChanged([CallerMemberName] string name=null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
