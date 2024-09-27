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
    public class SubaccountDataModel :INotifyPropertyChanged
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
                if (_subaccountname != value)
                {
                    _subaccountname = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _subaccounttype;
        [JsonProperty("subaccount_type")]
        public string SubaccountType
        {
            get => _subaccounttype;
            set
            {
                if (_subaccounttype != value)
                {
                    _subaccounttype = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _brokercode;
        [JsonProperty("broker_code")]
        public string BrokerCode
        {
            get => _brokercode;
            set
            {
                if (_brokercode != value)
                {
                    _brokercode = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _brokercode_exec;
        [JsonProperty("broker_code_exec")]
        public string BrokerCodeExec
        {
            get => _brokercode_exec;
            set
            {
                if (_brokercode_exec != value)
                {
                    _brokercode_exec = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
