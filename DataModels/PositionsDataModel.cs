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
    class PositionsDataModel: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private string _fundname;
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
        private List<string> _subaccountnames;
        public List<string> SubaccountNames
        {
            get => _subaccountnames;
            set
            {
                if(_subaccountnames != value)
                {
                    _subaccountnames = value;
                    OnPropertyChanged();
                }
            }
        }

    }
}
