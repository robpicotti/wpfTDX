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
    public class LivePositionsDataModel : INotifyPropertyChanged
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

        private DateTime _runttime;
        [JsonProperty("runtime")]
        public DateTime RunTime
        {
            get => _runttime;
            set
            {
                if (_runttime != value)
                {
                    _runttime = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _subtickername;
        [JsonProperty("sub_tickername")]
        public string SubTickerName
        {
            get => _subtickername;
            set
            {
                if (_subtickername != value)
                {
                    _subtickername = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _brokercodeexec;
        [JsonProperty("broker_code_exec")]
        public string BrokerCodeExec
        {
            get => _brokercodeexec;
            set
            {
                if (_brokercodeexec != value)
                {
                    _brokercodeexec = value;
                    OnPropertyChanged();
                }
            }
        }

        private DateTime _marketpitopen;
        [JsonProperty("market_pitopen")]
        public DateTime MarketpitOpen
        {
            get => _marketpitopen;
            set
            {
                if (_marketpitopen != value)
                {
                    _marketpitopen = value;
                    OnPropertyChanged();
                }
            }
        }

        private double? _lastprice;
        [JsonProperty("lastprice")]
        public double? LastPrice
        {
            get => _lastprice;
            set
            {
                if (_lastprice != value)
                {
                    _lastprice = value;
                    OnPropertyChanged();
                }
            }
        }

        private double? _positionlimit;
        [JsonProperty("position_limit")]
        public double? PositionLimit
        {
            get => _positionlimit;
            set
            {
                if (_positionlimit != value)
                {
                    _positionlimit = value;
                    OnPropertyChanged();
                }
            }
        }

        private double? _positionlive;
        [JsonProperty("position_live")]
        public double? PositionLive
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

        private double? _positiontarget;
        [JsonProperty("position_target")]
        public double? PositionTarget
        {
            get => _positiontarget;
            set
            {
                if (_positiontarget != value)
                {
                    _positiontarget = value;
                    OnPropertyChanged();
                }
            }
        }

        private double? _deployment;
        [JsonProperty("deployment")]
        public double? Deployment
        {
            get => _deployment;
            set
            {
                if (_deployment != value)
                {
                    _deployment = value;
                    OnPropertyChanged();
                }
            }
        }


        private double? _deploymenttad;
        [JsonProperty("deployment_tad")]
        public double? DeploymentTad
        {
            get => _deploymenttad;
            set
            {
                if (_deploymenttad != value)
                {
                    _deploymenttad = value;
                    OnPropertyChanged();
                }
            }
        }



        private double? _scaledpercent;
        [JsonProperty("scaled_percent")]
        public double? ScaledPercent
        {
            get => _scaledpercent;
            set
            {
                if (_scaledpercent != value)
                {
                    _scaledpercent = value;
                    OnPropertyChanged();
                }
            }
        }

        private double? _positiontargetraw;
        [JsonProperty("position_target_raw")]
        public double? PositionTargetRaw
        {
            get => _positiontargetraw;
            set
            {
                if (_positiontargetraw != value)
                {
                    _positiontargetraw = value;
                    OnPropertyChanged();
                }
            }
        }
       

        private bool? _valid;
        [JsonProperty("valid")]
        public bool? Valid
        {
            get => _valid;
            set
            {
                if (_valid != value)
                {
                    _valid = value;
                    OnPropertyChanged();
                }
            }
        }
        }
        }
