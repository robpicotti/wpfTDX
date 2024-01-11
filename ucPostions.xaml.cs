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
    /// Interaction logic for ucPostions.xaml
    /// </summary>
    public partial class ucPostions : UserControl
    {
        public DataTable dtFunds;
        public SqlConnection gbl_conn;
        TDX.db _db = new TDX.db();
        public string DEFAULT_FUND = "";
        public event EventHandler RemoveControlRequested;
        public ucPostions(SqlConnection conn, string default_fund)
        {
            InitializeComponent();
            gbl_conn = conn;
            DEFAULT_FUND = default_fund;
            LoadForm();
            SetControlValues();
        }
        private void LoadForm()
        {
            dtFunds = _db.get_funds(gbl_conn);
            cboFundname.Items.Clear();
            foreach (DataRow row in dtFunds.Rows)
            {
                cboFundname.Items.Add(row["fundname"].ToString());
            }
            cboFundname.SelectedItem = DEFAULT_FUND;
        }
        private void SetControlValues()
        {
            dtPickerPostionFrom.SelectedDate = DateTime.Today;
        }
        private void cmdClose_Click(object sender, RoutedEventArgs e)
        {
            RemoveControlRequested?.Invoke(this, EventArgs.Empty);
        }

        private void cmdRun_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                string fundname = cboFundname.SelectedItem.ToString();
                if (fundname != null || fundname != "")
                {
                    Position posn = new Position(fundname, gbl_conn);
                    posn.position_date = dtPickerPostionFrom.SelectedDate.Value;
                    posn.positions();
                    dgPosition.ItemsSource = posn.position.DefaultView;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Positions error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { Cursor = Cursors.Arrow; }
        }
    }
}
