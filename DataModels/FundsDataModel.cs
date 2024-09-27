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
    public class FundsDataModel
    {
        private string _fundname;
        private string _fundgroupname;
        private string _fundgroupenv;
        private double _notional;
        private string _benchmkarkname;
        private string _basecurrency;
        private double _mgmtfee;
        private double _incentivefee;
        private string _feetype;
        private string _accountype;
        private bool _closed;

        public event PropertyChangedEventHandler PropertyChanged;

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

        [JsonProperty("fundgroupname")]
        public string FundGroupName
        {
            get => _fundgroupname;
            set
            {
                if (_fundgroupname != value)
                {
                    _fundgroupname = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("fundgroup_env")]
        public string FundGroupEnv
        {
            get => _fundgroupenv;
            set
            {
                if (_fundgroupenv != value)
                {
                    _fundgroupenv = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("notional")]
        public double Notional
        {
            get => _notional;
            set
            {
                if (_notional != value)
                {
                    _notional = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("benchmarkname")]
        public string BenchmarkName
        {
            get => _benchmkarkname;
            set
            {
                if (_benchmkarkname != value)
                {
                    _benchmkarkname = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("base_currency")]
        public string BaseCurrency
        {
            get => _basecurrency;
            set
            {
                if (_basecurrency != value)
                {
                    _basecurrency = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("mgmt_fee")]
        public double MgmtFee
        {
            get => _mgmtfee;
            set
            {
                if (_mgmtfee != value)
                {
                    _mgmtfee = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("incentive_fee")]
        public double IncentiveFee
        {
            get => _incentivefee;
            set
            {
                if (_incentivefee != value)
                {
                    _incentivefee = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("fee_type")]
        public string FeeType
        {
            get => _feetype;
            set
            {
                if (_feetype != value)
                {
                    _feetype = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("account_type")]
        public string AccountType
        {
            get => _accountype;
            set
            {
                if (_accountype != value)
                {
                    _accountype = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("closed")]
        public bool Closed
        {
            get => _closed;
            set
            {
                if (_closed != value)
                {
                    _closed = value;
                    OnPropertyChanged();
                }
            }
        }

        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }
}
