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
using System.Windows.Shapes;
using System.Data.SqlClient;
using System.Data;

namespace wpfTDX
{
    /// <summary>
    /// Interaction logic for winPortfolioWeights.xaml
    /// </summary>
    public partial class winPortfolioWeights : Window
    {
        PortfolioWeightsViewModel viewmodel { get; set; }
        public winPortfolioWeights(SqlConnection conn)
        {
            InitializeComponent();
            viewmodel = new PortfolioWeightsViewModel();
            DataContext = viewmodel;
            Loaded += async (_, __) => await viewmodel.GetPortfolioData();
        }


    }
}
