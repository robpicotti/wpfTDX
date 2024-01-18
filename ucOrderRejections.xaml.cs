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
        public event EventHandler RemoveControlRequested;
        public ucOrderRejections(SqlConnection conn)
        {
            InitializeComponent();
            DataContext = this;
            gbl_conn = conn;
            LoadForm();

        }
        private void LoadForm()
        {
            dtFrom.SelectedDate = DateTime.Today;
            dtTo.SelectedDate = DateTime.Today;
            GetOrdeRejections();
        }
        /// <summary>
        /// retrieve all order rejections with selected date range
        /// </summary>
        private void GetOrdeRejections()
        {
            try
            {
                if (calEnd == null)
                {
                    calEnd = calStart.AddDays(1).AddMilliseconds(-1);
                }
                if (calStart != null && calEnd != null)
                {
                    DataSet ds = _db.get_order_rejections(calStart.ToString("yyyy-MM-dd HH:mm:ss"), calEnd.ToString("yyyy-MM-dd HH:mm:ss"), gbl_conn);
                    //List<DataRow> rows = dt.Tables[0].AsEnumerable().ToList();
                    //dgOrderRejections.ItemsSource = rows;
                    if (ds.Tables.Count > 0)
                    {
                        DataTable dt = ds.Tables[0];
                        dgOrderRejections.ItemsSource = dt.DefaultView;
                        dgOrderRejections.Items.Refresh();
                    }
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

        private void cmdRun_Click(object sender, RoutedEventArgs e)
        {
            if (dtFrom.SelectedDate != null)
            { 
                calStart = dtFrom.SelectedDate.Value;
            }
            if (dtTo.SelectedDate != null)
            {
                calEnd = dtTo.SelectedDate.Value;
            }
            GetOrdeRejections();
        }

        private void cmdClose_Click(object sender, RoutedEventArgs e)
        {
            RemoveControlRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
