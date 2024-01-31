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
            string endDateTime = this.EndDate.AddHours(23).AddMinutes(59).AddSeconds(59).ToString("dd-MMM-yyyy hh:mm:ss");
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
    }
}
