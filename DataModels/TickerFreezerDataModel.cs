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
    public class TickerFreezerDataModel
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        private DateTime _runtime;
        private string _emsname;
        private string _broker_code_exec;
        private string _fundname;
        private string _subaccountname;
        private string _execaccountname;
        private string _tadid;
        private string _tickername;
        private int _errorcode;
        private string _errorstring;
        private int _resolved;
        private DateTime _freezeexpiration;
        private int _override;
        private DateTime _overrideexpiration;
        private string _benchmarkname;
        private string _notes;
        private DateTime _runtimeresolved;
        private bool _thaw = false;

        [JsonProperty("runtime")]
        public DateTime Runtime
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

        [JsonProperty("emsname")]
        public string Emsname
        {
            get => _emsname;
            set
            {
                if (_emsname != value)
                {
                    _emsname = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("broker_code_exec")]
        public string BrokerCodeExec
        {
            get => _broker_code_exec;
            set
            {
                if (_broker_code_exec != value)
                {
                    _broker_code_exec = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("fundname")]
        public string Fundname
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

        [JsonProperty("subaccountname")]
        public string Subaccountname
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

        [JsonProperty("execaccountname")]
        public string Execaccountname
        {
            get => _execaccountname;
            set
            {
                if (_execaccountname != value)
                {
                    _execaccountname = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("tad_id")]
        public string Tadid
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

        [JsonProperty("tickername")]
        public string Tickername
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

        [JsonProperty("error_code")]
        public int Errorcode
        {
            get => _errorcode;
            set
            {
                if (_errorcode != value)
                {
                    _errorcode = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("error_string")]
        public string Errorstring
        {
            get => _errorstring;
            set
            {
                if (_errorstring != value)
                {
                    _errorstring = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("resolved")]
        public int Resolved
        {
            get => _resolved;
            set
            {
                if (_resolved != value)
                {
                    _resolved = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("freeze_expiration")]
        public DateTime FreezeExpiration
        {
            get => _freezeexpiration;
            set
            {
                if (_freezeexpiration != value)
                {
                    _freezeexpiration = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("override")]
        public int Override
        {
            get => _override;
            set
            {
                if (_override != value)
                {
                    _override = value;
                    OnPropertyChanged();
                }
            }
        }
        [JsonProperty("override_expiration")]
        public DateTime OverrideExpiration
        {
            get => _overrideexpiration;
            set
            {
                if (_overrideexpiration != value)
                {
                    _overrideexpiration = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("benchmarkname")]
        public string Benchmarkname
        {
            get => _benchmarkname;
            set
            {
                if (_benchmarkname != value)
                {
                    _benchmarkname = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("notes")]
        public string Notes
        {
            get => _notes;
            set
            {
                if (_notes != value)
                {
                    _notes = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("runtime_solved")]
        public DateTime RuntimeResolved
        {
            get => _runtimeresolved;
            set
            {
                if (_runtimeresolved != value)
                {
                    _runtimeresolved = value;
                    OnPropertyChanged();
                }
            }
        }
    
        public bool Thaw
        {
            get => _thaw;
            set
            {
                if(_thaw != value)
                {
                    _thaw = value;
                    OnPropertyChanged();
                }
            }
        }
    
    }
}
