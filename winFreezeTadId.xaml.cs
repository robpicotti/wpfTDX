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
using System.Data;
using System.Data.SqlClient;
using TDX;
using System.ComponentModel;

namespace wpfTDX
{
    /// <summary>
    /// Interaction logic for winFreezeTadId.xaml
    /// </summary>
    public partial class winFreezeTadId : Window
    {
        SqlConnection gbl_conn;
        DataTable dtTadIdsMaster;
        ucTickerFreezer ucTickerFreezer;
        public winFreezeTadId(SqlConnection conn, ucTickerFreezer ucTick)
        {
            InitializeComponent();
            gbl_conn = conn;
            ucTickerFreezer = ucTick;
            LoadForm();
        }
        /// <summary>
        /// custom window initializer
        /// </summary>
        private void LoadForm()
        {
            try
            {
                PopulateControlData();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "LoadForm", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateControlData()
        {
            dtTadIdsMaster = GetTadIdsMaster();
        }

        private DataTable GetTadIdsMaster()
        {
            TDX.Table tblTim = new TDX.Table("tad_ids_master", gbl_conn).select_latest();
            return tblTim.table_data;
        }

        private void cmdSearch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                if (txtSearchTadId.Text != "")
                {
                    DataView dataView = new DataView(dtTadIdsMaster);
                    dataView.RowFilter = $"tad_id LIKE '{txtSearchTadId.Text}%'";
                    if (dataView.ToTable().Rows.Count < 251)
                    {
                        dgTadIds.ItemsSource = dataView;
                    }
                    else
                    {
                        MessageBox.Show("Please refine your search as there are more than 250 possible tad_ids",
                            "tad_id search", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "tad_id search", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private void dgTadIds_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                DataGrid dataGrid = (DataGrid)sender;

                // Set the context menu's placement
                dataGrid.ContextMenu.PlacementTarget = dataGrid;
                dataGrid.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;

                // Show the context menu
                dataGrid.ContextMenu.IsOpen = true;

                e.Handled = true;
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            var selectedRow = dgTadIds.SelectedItem as DataRowView;
            if(selectedRow != null)
            {
                string tad_id = selectedRow["tad_id"].ToString();
                try
                {
                    FreezeTadId(tad_id);
                    MessageBox.Show(tad_id + " has been frozen", "freeze tad_id",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "freeze tad_id", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    Cursor = Cursors.Arrow;
                    ucTickerFreezer.Refresh();
                }
            }
        }
        private void FreezeTadId(string tad_id)
        {
            TickerFreezer tf = new TickerFreezer(gbl_conn);
            tf.Freeze(
                    runtime: DateTime.UtcNow.ToString("dd-MMM-yyyy HH:mm:ss"),
                    error_code: 2003, error_string: "Manual freeze on tad_id-fund",
                    freeze_expiration: null, _override: 0, override_expiration: null,
                    notes: "", runtime_resolved: null, tad_id: tad_id,
                    fundname: "*");
        }
    }
}
