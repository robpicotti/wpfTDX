using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using TDX;

namespace wpfTDX
{
    class Order
    {
        public string BrokerName { get; set; }
        public string tad_order_id { get; set; }
        public string ExecutionAccountName { get; set; }
        public string SubaccountName { get; set; }
        public string FundName { get; set; }
        public DataTable dtOrders { get; set; }
        public DataTable dtOpenOrders { get; set; }
        public DateTime StartDate;
        public DateTime EndDate;
        private SqlConnection gbl_conn { get; set; }
        private db DB = new db();

        public Order(string fundName, DateTime startDate,DateTime endDate, SqlConnection conn)
        {
            this.FundName = fundName;
            this.StartDate = startDate;
            this.EndDate = endDate;
            this.gbl_conn = conn;

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type">select from subaccountname, fundname</param>
        public void Get(string type)
        {
            if(type.ToLower()=="fundname" && (this.FundName !=null && this.FundName!=""))
            {
                dtOrders = GetOrdersByFundName(this.FundName);
                dtOpenOrders = GetOpenOrdersByFundName(this.FundName);
            }
        }
        /// <summary>
        /// get all the orders where the subaccounts are in the fund specified
        /// </summary>
        /// <param name="fundname"></param>
        /// <returns></returns>
        private DataTable GetOrdersByFundName(string fundname)
        {
            DataTable dtOut = new DataTable();
            string endDateTime = this.EndDate.ToString("dd-MMM-yyyy HH:mm:ss");
            string startDateTime = this.StartDate.ToString("dd-MMM-yyyy");
            string execSQL = "DECLARE @fundname varchar(100) IF OBJECT_ID('tempdb..#subaccounts') IS NOT NULL DROP TABLE #subaccounts ";
            execSQL += "SET @fundname ='" + fundname + "' ";
            execSQL += "SELECT subaccountname INTO	#subaccounts FROM	subaccounts WHERE fundname  = @fundname ";
            execSQL += "SELECT ord.* FROM orders ord CROSS APPLY string_split(ord.subaccounts,',') s ";
            execSQL += "JOIN #subaccounts sub ON s.value = sub.subaccountname WHERE	ord.status = 'FILLED' ";
            execSQL += " AND ord.order_submit_time BETWEEN '" +startDateTime ;
            execSQL += "' AND '" + endDateTime + "'";
            dtOut = DB.execSQL(execSQL, this.gbl_conn);
            return dtOut;
        }
        /// <summary>
        /// returns all the current open orders for a given fund, by 
        /// </summary>
        /// <param name="fundname"></param>
        /// <returns></returns>
        private DataTable GetOpenOrdersByFundName(string fundname)
        {
            DataTable dtOut = new DataTable();
            string endDateTime = this.EndDate.ToString("dd-MMM-yyyy HH:mm:ss");
            string startDateTime = this.StartDate.ToString("dd-MMM-yyyy");
            string execSQL = "DECLARE @fundname varchar(100)  IF OBJECT_ID('tempdb..#subaccounts') IS NOT NULL ";
            execSQL += "DROP TABLE #subaccounts ";
            execSQL += "IF OBJECT_ID('tempdb..#all') IS NOT NULL ";
            execSQL += "DROP TABLE #all ";
            execSQL += "IF OBJECT_ID('tempdb..#max_orders_key') IS NOT NULL ";
            execSQL += "DROP TABLE #max_orders_key  ";
            execSQL += "SET @fundname ='ENBW-testing'  ";
            execSQL += "SELECT subaccountname INTO #subaccounts FROM subaccounts WHERE fundname  = @fundname ";
            execSQL += "SELECT ord.* into #all FROM orders ord CROSS APPLY string_split(ord.subaccounts,',') s JOIN #subaccounts sub ";
            execSQL += "ON s.value = sub.subaccountname WHERE ord.order_submit_time BETWEEN ";
            execSQL += "'" + startDateTime + "' AND '" + endDateTime + "' ";
            execSQL += "SELECT	tad_order_id,MAX(orders_key) AS max_orders_key ";
            execSQL += "INTO #max_orders_key ";
            execSQL += "FROM #all ";
            execSQL += "GROUP BY tad_order_id ";
            execSQL += "SELECT status,a.* ";
            execSQL += "FROM #all a ";
            execSQL += "INNER JOIN #max_orders_key m ";
            execSQL += "ON m.max_orders_key = a.orders_key ";
            execSQL += "AND m.tad_order_id = a.tad_order_id ";
            execSQL += "WHERE a.status NOT IN ('FILLED','CANCELLED')";
            dtOut = DB.execSQL(execSQL, this.gbl_conn);
            return dtOut;
        }

    }
}
