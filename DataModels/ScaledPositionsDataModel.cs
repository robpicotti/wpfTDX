using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace wpfTDX
{
    public  class ScaledPositionsDataModel: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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
        private string _fundgroupname;
        [JsonProperty("fundgroupname")]
        public string FundGroupName
        {
            get { return _fundgroupname; }
            set
            {
                if (_fundgroupname != value)
                {
                    _fundgroupname = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _fundname;
        [JsonProperty("fundname")]
        public string FundName
        {
            get { return _fundname; }
            set
            {
                if (_fundname != value)
                {
                    _fundname = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _tickername;
        [JsonProperty("tickername")]
        public string TickerName
        {
            get { return _tickername; }
            set
            {
                if (_tickername != value)
                {
                    _tickername = value;
                    OnPropertyChanged();
                }
            }
        }
        private double? _scaledStepSize;
        [JsonProperty("scaled_stepsize")]
        public double? ScaledStepSize
        {
            get { return _scaledStepSize; }
            set
            {
                if (_scaledStepSize != value)
                {
                    _scaledStepSize = value;
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
        private double? _scaledTimeStep;
        [JsonProperty("scaled_timestep")]
        public double? ScaledTimeStep
        {
            get { return _scaledTimeStep; }
            set
            {
                if (_scaledTimeStep != value)
                {
                    _scaledTimeStep = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _scaledType;
        [JsonProperty("scaled_type")]
        public string ScaleType
        {
            get { return _scaledType; }
            set
            {
                if (_scaledType != value)
                {
                    _scaledType = value;
                    OnPropertyChanged();
                }
            }
        }

    }
}
