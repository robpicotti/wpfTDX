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
using TDX;
using System.Data;
using System.Data.SqlClient;

namespace wpfTDX
{
    /// <summary>
    /// Interaction logic for ucCorpActions.xaml
    /// </summary>
    
    public partial class ucCorpActions : UserControl
    {
        SqlConnection gbl_conn;
        public event EventHandler RemoveControlRequested;
        TDX.db _db = new TDX.db();
        private DataTable dtActionType;
        private DataTable dtLoaded;
        private DataTable dtSubaccounts;
        private DataTable dtTransitions;
        private DataTable dtUpcomingEvents;
        private List<string> distinctActionTypes;
        public ucCorpActions(SqlConnection conn)
        {
            InitializeComponent();
            gbl_conn = conn;
            LoadForm();
        }
        /// <summary>
        /// custom form initiliazer
        /// </summary>
        private void LoadForm()
        {
            try
            {
                GetControlsData();
                PopulateControls();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Loading Form",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void GetControlsData()
        {
            //subaccounts
            dtSubaccounts = Subaccount.getSubaccountsAll(gbl_conn);
            //get loaded corp actions
            GetLoadedCorporateActions();
            //transitions
            GetTransitions();
            //upcoming events
            GetUpcomingEvents();
        }
        /// <summary>
        /// populate all the controls with data if needed
        /// </summary>
        private void PopulateControls()
        {
            //load the subaccounts
            foreach (DataRow row in dtSubaccounts.Rows)
            {
                cboSubaccount.Items.Add(row["subaccountname"].ToString());
            }
            //loaded corp actions
            if (dtLoaded != null)
            {
                dgLoaded.ItemsSource = dtLoaded.DefaultView;
            }

            //transitions
            if (dtTransitions != null)
            {
                dgTransitions.ItemsSource = dtTransitions.DefaultView;
            }
            //upcoming events
            if (dtUpcomingEvents != null)
            {
                dgEvents.ItemsSource = dtUpcomingEvents.DefaultView;
            }
        }
        /// <summary>
        /// get all the loaded corporate actions
        /// </summary>
        private void GetLoadedCorporateActions()
        {
            string sql_text = "SELECT * FROM corporate_actions_load";
            dtLoaded = _db.execSQL(sql_text, gbl_conn);
        }

        //get the distinct action types
        private void GetActionTypes()
        {
            // Extract distinct action_type values using LINQ
             distinctActionTypes = dtLoaded.AsEnumerable()
                .Select(row => row.Field<string>("action_type"))
                .Distinct()
                .OrderBy(actionType => actionType)
                .ToList();
        }

        /// <summary>
        /// close the screen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdClose_Click_1(object sender, RoutedEventArgs e)
        {
            RemoveControlRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// gets the datatable with cash dividends paid to the specified subaccount
        /// </summary>
        /// <param name="subaccountname"></param>
        /// <returns></returns>
        private DataTable GetCashDividendsPaid(string subaccountname)
        {
            DataTable dtCDP = new DataTable();
            DataSet ds = _db.get_cash_dividends_paid(subaccountname, gbl_conn);
            if(ds.Tables.Count >0)
            {
                dtCDP = ds.Tables[0];
            }
            return dtCDP;
        }
        /// <summary>
        /// gets all the transitions
        /// </summary>
        private void GetTransitions()
        {
            string sql_text = "with max_runtime as (select max(runtime) as runtime,transition_type,ca_id from corporate_action_transitions where status <> -1 group by transition_type, ca_id )";
            sql_text += "select	a.* from	corporate_action_transitions a inner join max_runtime m on	m.runtime =  a.runtime and m.ca_id	=	a.ca_id order by a. ca_id";
            dtTransitions = _db.execSQL(sql_text, gbl_conn);
        }
        /// <summary>
        /// get upcoming corp actions events
        /// </summary>
        private void GetUpcomingEvents()
        {
            string sql_text = "with max_runtime as (select	max(runtime) as runtime,transition_type,ca_id from corporate_action_transitions where status <> -1 and payment_date >= convert(date,getdate()) group by transition_type, ca_id ) ";
            sql_text += "select	a.* from	corporate_action_transitions a inner join max_runtime m on	m.runtime =  a.runtime and m.ca_id	=	a.ca_id order by a. payment_date";
            dtUpcomingEvents = _db.execSQL(sql_text, gbl_conn);
        }
        /// <summary>
        /// get the cash dividends that were paid
        /// </summary>
        /// <param name="subaccount"></param>
        /// <returns></returns>
        private DataTable CashDividendsPaid(string subaccount)
        {
            DataTable dtCDP = new DataTable();
            DataSet ds = _db.get_cash_dividends_paid(subaccount, gbl_conn);
            if(ds.Tables.Count >0)
            {
                dtCDP = ds.Tables[0];
            }
            return dtCDP;
        }

        private void cboSubaccount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string subaccount = cboSubaccount.SelectedValue.ToString();
            DataTable dtCD = GetCashDividendsPaid(subaccount);
            if(dtCD != null)
            {
                dgCDivPaid.ItemsSource = dtCD.DefaultView;
            }
        }
    }
}
