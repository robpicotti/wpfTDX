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
    public class TadPositionsDataModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        private string _tickername;
        [JsonProperty("tickername")]
        public string Tickername
        {
            get => _tickername;
            set
            {
                if(value != _tickername)
                {
                    _tickername = value;
                    OnPropertyChanged(nameof(Tickername));
                }
            }
        }
        private string _subtickername;
        [JsonProperty("sub_tickername")]
        public string SubTickername
        {
            get => _subtickername;
            set
            {
                if(value != _subtickername)
                {
                    _subtickername = value;
                    OnPropertyChanged(nameof(SubTickername));
                }
            }
        }
        private string _currentcontract;
        [JsonProperty("current_contract")]
        public string CurrentContract
        {
            get => _currentcontract;
            set
            {
                if(value != _currentcontract)
                {
                    _currentcontract = value;
                    OnPropertyChanged(nameof(CurrentContract));
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
                if(value != _tadid)
                {
                    _tadid = value;
                    OnPropertyChanged(nameof(TadId));
                }
            }
        }

        private string _bbgsymbol;
        [JsonProperty("bbg_symbol")]
        public string BbgSymbol
        {
            get => _bbgsymbol;
            set
            {
                if(value != _bbgsymbol)
                {
                    _bbgsymbol = value;
                    OnPropertyChanged(nameof(BbgSymbol));
                }
            }
        }

        private string _rolldate;
        [JsonProperty("roll_date")]
        public string Rolldate
        {
            get => _rolldate;
            set
            {
                if(value != _rolldate)
                {
                    _rolldate = value;
                    OnPropertyChanged(nameof(Rolldate));
                }
            }
        }
        private float _view_position_base_D1;
        [JsonProperty("view_position_base_D1")]
        public float ViewPositionBaseD1
        {
            get => _view_position_base_D1;
            set
            {
                if(value != _view_position_base_D1)
                {
                    _view_position_base_D1 = value;
                    OnPropertyChanged(nameof(ViewPositionBaseD1));
                }
            }
        }

    }
}
