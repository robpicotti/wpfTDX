using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.SqlClient;
namespace wpfTDX
{
    /// <summary>
    /// Interaction logic for ucDeposits.xaml
    /// </summary>
    
    public partial class ucDeposits : UserControl
    {
        public event EventHandler RemoveControlRequested;
        public ucDeposits(SqlConnection conn)
        {
            InitializeComponent();
        }

        private void cmdClose_Click(object sender, RoutedEventArgs e)
        {
            RemoveControlRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
