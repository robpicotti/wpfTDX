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
    public class TargetPositionsDataModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

        private string _fundName;
        [JsonProperty("fund_name")]
        public string FundName
        {
            get { return _fundName; }
            set
            {
                if (_fundName != value)
                {
                    _fundName = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _tadId;
        [JsonProperty("tad_id")]
        public string TadId
        {
            get { return _tadId; }
            set
            {
                if (_tadId != value)
                {
                    _tadId = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _tickerName;
        [JsonProperty("tickername")]
        public string TickerName
        {
            get { return _tickerName; }
            set
            {
                if (_tickerName != value)
                {
                    _tickerName = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _subaccountName;
        [JsonProperty("subaccountname")]
        public string SubaccountName
        {
            get { return _subaccountName; }
            set
            {
                if (_subaccountName != value)
                {
                    _subaccountName = value;
                    OnPropertyChanged();
                }
            }
        }

        private double? _lastPrice;
        [JsonProperty("last_price")]
        public double? LastPrice
        {
            get { return _lastPrice; }
            set
            {
                if (_lastPrice != value)
                {
                    _lastPrice = value;
                    OnPropertyChanged();
                }
            }
        }

        private double? _positionLimit;
        [JsonProperty("position_limit")]
        public double? PositionLimit
        {
            get { return _positionLimit; }
            set
            {
                if (_positionLimit != value)
                {
                    _positionLimit = value;
                    OnPropertyChanged();
                }
            }
        }

        private double? _positionTarget;
        [JsonProperty("position_target")]
        public double? PositionTarget
        {
            get { return _positionTarget; }
            set
            {
                if (_positionTarget != value)
                {
                    _positionTarget = value;
                    OnPropertyChanged();
                }
            }
        }

        private double? _deployment;
        [JsonProperty("deployment")]
        public double? Deployment
        {
            get { return _deployment; }
            set
            {
                if (_deployment != value)
                {
                    _deployment = value;
                    OnPropertyChanged();
                }
            }
        }

        private double? _deploymentTad;
        [JsonProperty("deployment_tad")]
        public double? DeploymentTad
        {
            get { return _deploymentTad; }
            set
            {
                if (_deploymentTad != value)
                {
                    _deploymentTad = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _newpositionId;
        [JsonProperty("newposition_id")]
        public string NewpositionId
        {
            get { return _newpositionId; }
            set
            {
                if (_newpositionId != value)
                {
                    _newpositionId = value;
                    OnPropertyChanged();
                }
            }
        }

        private double? _rollPercent;
        [JsonProperty("roll_percent")]
        public double? RollPercent
        {
            get { return _rollPercent; }
            set
            {
                if (_rollPercent != value)
                {
                    _rollPercent = value;
                    OnPropertyChanged();
                }
            }

        }

        private double? _scaledTarget;
        [JsonProperty("scaled_target")]
        public double? ScaledTarget
        {
            get { return _scaledTarget; }
            set
            {
                if (_scaledTarget != value)
                {
                    _scaledTarget = value;
                    OnPropertyChanged();
                }
            }
        }

        private double? _scaledPercent;
        [JsonProperty("scaled_percent")]
        public double? ScaledPercent
        {
            get { return _scaledPercent; }
            set
            {
                if (_scaledPercent != value)
                {
                    _scaledPercent = value;
                    OnPropertyChanged();
                }
            }
        }

        private double? _varDaily;
        [JsonProperty("var_daily")]
        public double? VarDaily
        {
            get { return _varDaily; }
            set
            {
                if (_varDaily != value)
                {
                    _varDaily = value;
                    OnPropertyChanged();
                }
            }
        }

        private double? _ptvalConstant;
        [JsonProperty("ptval_constant")]
        public double? PtvalConstant
        {
            get { return _ptvalConstant; }
            set
            {
                if (_ptvalConstant != value)
                {
                    _ptvalConstant = value;
                    OnPropertyChanged();
                }
            }
        }

        private double? _spotRate;
        [JsonProperty("spot_rate")]
        public double? SpotRate
        {
            get { return _spotRate; }
            set
            {
                if (_spotRate != value)
                {
                    _spotRate = value;
                    OnPropertyChanged();
                }
            }
        }

        private double? _var;
        [JsonProperty("var")]
        public double? Var
        {
            get { return _var; }
            set
            {
                if (_var != value)
                {
                    _var = value;
                    OnPropertyChanged();
                }
            }
        }

        private double? _scale;
        [JsonProperty("scale")]
        public double? Scale
        {
            get { return _scale; }
            set
            {
                if (_scale != value)
                {
                    _scale = value;
                    OnPropertyChanged();
                }
            }
        }

        private double? _weight;
        [JsonProperty("weight")]
        public double? Weight
        {
            get { return _weight; }
            set
            {
                if (_weight != value)
                {
                    _weight = value;
                    OnPropertyChanged();
                }
            }
        }

        private double? _capital;
        [JsonProperty("capital")]
        public double? Capital
        {
            get { return _capital; }
            set
            {
                if (_capital != value)
                {
                    _capital = value;
                    OnPropertyChanged();
                }
            }
        }

        private double? _varTarget;
        [JsonProperty("var_target")]
        public double? VarTarget
        {
            get { return _varTarget; }
            set
            {
                if (_varTarget != value)
                {
                    _varTarget = value;
                    OnPropertyChanged();
                }
            }
        }

        private double? _varAllocated;
        [JsonProperty("var_allocated")]
        public double? VarAllocated
        {
            get { return _varAllocated; }
            set
            {
                if (_varAllocated != value)
                {
                    _varAllocated = value;
                    OnPropertyChanged();
                }
            }
        }

        private double? _subTickerWt;
        [JsonProperty("sub_ticker_wt")]
        public double? SubTickerWt
        {
            get { return _subTickerWt; }
            set
            {
                if (_subTickerWt != value)
                {
                    _subTickerWt = value;
                    OnPropertyChanged();
                }
            }
        }

        private double? _varLimit;
        [JsonProperty("var_limit")]
        public double? VarLimit
        {
            get { return _varLimit; }
            set
            {
                if (_varLimit != value)
                {
                    _varLimit = value;
                    OnPropertyChanged();
                }
            }
        }

        private double? _positionLimitRaw;
        [JsonProperty("position_limit_raw")]
        public double? PositionLimitRaw
        {
            get { return _positionLimitRaw; }
            set
            {
                if (_positionLimitRaw != value)
                {
                    _positionLimitRaw = value;
                    OnPropertyChanged();
                }
            }
        }

        private double? _positionTargetRaw;        
        [JsonProperty("position_target_raw")]
        public double? PositionTargetRaw
        {
            get { return _positionTargetRaw; }
            set
            {
                if (_positionTargetRaw != value)
                {
                    _positionTargetRaw = value;
                    OnPropertyChanged();
                }
            }
        }

    }


}
