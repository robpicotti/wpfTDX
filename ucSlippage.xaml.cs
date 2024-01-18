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
using System.Data;
using System.Data.SqlClient;
using TDX;

namespace wpfTDX
{
    /// <summary>
    /// Interaction logic for ucSlippage.xaml
    /// </summary>
    public partial class ucSlippage : UserControl
    {
        SqlConnection gbl_conn;
        List<string> lstNumericColumns = new List<string>();
        public event EventHandler RemoveControlRequested;
        DataTable dtFunds;
        db _db = new db();
        public ucSlippage(SqlConnection conn)
        {
            InitializeComponent();
            gbl_conn = conn;
            LoadForm();
        }
        /// <summary>
        /// custom initializer for window
        /// </summary>
        private void LoadForm()
        {
            //dgSlippage.AutoGeneratingColumn += datagrid_AutoGeneratingColumn;
            PopulateControls();
        }
        /// <summary>
        /// fill the controls with the necessary data
        /// </summary>
        private void PopulateControls()
        {
            dtFunds = _db.get_funds(gbl_conn);
            cboFundname.Items.Clear();
            foreach (DataRow row in dtFunds.Rows)
            {
                cboFundname.Items.Add(row["fundname"].ToString());
            }
            cboFundname.SelectedIndex = 0;
            //set dt pickers
            dtFrom.SelectedDate = DateTime.Today;
            dtTo.SelectedDate = DateTime.Today;
        }

        //private void datagrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        //{
        //    if (lstNumericColumns.Contains(e.PropertyName))
        //    {
        //        var textColumn = e.Column as DataGridTextColumn;
        //        if (textColumn != null)
        //        {
        //            // Set the StringFormat to "N" for thousand separator
        //            textColumn.Binding = new Binding(e.PropertyName) { StringFormat = "N" };
        //        }
        //    }
        //}

        private void cmdRun_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                string calStart = dtFrom.SelectedDate.Value.ToString("yyyy-MM-dd HH:mm:ss");
                string calEnd = dtTo.SelectedDate.Value
                    .AddHours(23)
                    .AddMinutes(59).AddSeconds(59).ToString("yyyy-MM-dd HH:mm:ss");
                string fund_name = cboFundname.Text;
                Slippage slip = GetSlippage(fund_name, calStart, calEnd);
                if(slip.dsSlippage.Tables.Count >0)
                {
                    dgSlippage.ItemsSource = slip.dsSlippage.Tables[0].DefaultView;
                    DataTable dtSumm = slip.dtSlippageAnalysis;
                    if (dtSumm != null)
                    {
                        dgSummary.ItemsSource = dtSumm.DefaultView;
                    }
         
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Slippage", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }

        }

        private Slippage GetSlippage(string fundname,string date_from,string date_to)
        {
            Slippage slip = new Slippage(date_from, date_to, fundname, gbl_conn);
            return slip;
        }

        /// <summary>
        /// close the window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void cmdClose_Click_1(object sender, RoutedEventArgs e)
        {
            RemoveControlRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
