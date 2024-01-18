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
using System.Data;
using TDX;
namespace wpfTDX
{
    /// <summary>
    /// Interaction logic for ucOrderRejections.xaml
    /// </summary>
    public partial class ucOrderRejections : UserControl
    {
        SqlConnection gbl_conn;
        db _db = new db();
        DateTime calEnd;
        DateTime calStart;
        public ucOrderRejections(SqlConnection conn)
        {
            InitializeComponent();
            gbl_conn = conn;

        }
        private void LoadForm()
        {
            dtFrom.SelectedDate = DateTime.Today;
            dtTo.SelectedDate = DateTime.Today;
            get_order_rejections();
        }
        private void get_order_rejections()
        {
            try
            {
                if (calEnd == null)
                {
                    calEnd = calStart.AddDays(1).AddMilliseconds(-1);
                }
                if (calStart != null && calEnd != null)
                {
                    DataSet dt = _db.get_order_rejections(calStart.ToString("yyyy-MM-dd HH:mm:ss"), calEnd.ToString("yyyy-MM-dd HH:mm:ss"), gbl_conn);
                    dgOrderRejections.ItemsSource = dt.Tables[0]; ;

                }
                else
                {
                    MessageBox.Show("Please ensure account and dates are selected");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
