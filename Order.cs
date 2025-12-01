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
        public DataTable dtOpenOrdersProposed { get; set; }
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
                dtOpenOrdersProposed = dtOpenOrders.Copy();
                // Filter out the "ALL_FUNDS" row
                var filteredRows = dtOpenOrdersProposed.AsEnumerable()
                    .Where(row => row.Field<string>("status") == "PROPOSED");
                if (filteredRows.Count()>0)
                {
                    dtOpenOrdersProposed = filteredRows.CopyToDataTable();
                }
                else
                {
                    dtOpenOrdersProposed = dtOpenOrders.Clone();
                }
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
            execSQL += "JOIN #subaccounts sub ON s.value = sub.subaccountname WHERE	ord.status in ('FILLED','PARTIALLYCANCELLED') ";
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
            string out_columns = "ord.runtime, ord.broker,ord.account,ord.subaccounts,ord.allocation_amount,ord.tad_order_id,ord.order_id,";
            out_columns += "ord.brok_uniq_order_id,ord.action,ord.tad_id,ord.tickername,ord.price,ord.multiplier,ord.size,";
            out_columns += "ord.tif,ord.order_type,ord.status,ord.cancel_tdx,ord.status_time,ord.newposition_ids,ord.bbg_figi,ord.trade_allocations,ord.autoexecute,ord.tx_type_detail,";
            out_columns += "ord.orders_key";
            string execSQL = "DECLARE @fundname varchar(100)  IF OBJECT_ID('tempdb..#subaccounts') IS NOT NULL ";
            execSQL += "DROP TABLE #subaccounts ";
            execSQL += "IF OBJECT_ID('tempdb..#all') IS NOT NULL ";
            execSQL += "DROP TABLE #all ";
            execSQL += "IF OBJECT_ID('tempdb..#max_orders_key') IS NOT NULL ";
            execSQL += "DROP TABLE #max_orders_key  ";
            execSQL += "SET @fundname ='" + fundname + "'";
            execSQL += "SELECT subaccountname INTO #subaccounts FROM subaccounts WHERE fundname  = @fundname ";
            execSQL += "SELECT " + out_columns + " into #all FROM orders ord CROSS APPLY string_split(ord.subaccounts,',') s JOIN #subaccounts sub ";
            execSQL += "ON s.value = sub.subaccountname WHERE ord.order_submit_time BETWEEN ";
            execSQL += "'" + startDateTime + "' AND '" + endDateTime + "' ";
            execSQL += "SELECT	tad_order_id,MAX(orders_key) AS max_orders_key ";
            execSQL += "INTO #max_orders_key ";
            execSQL += "FROM #all ";
            execSQL += "GROUP BY tad_order_id ";
            execSQL += "SELECT a.* ";
            execSQL += "FROM #all a ";
            execSQL += "INNER JOIN #max_orders_key m ";
            execSQL += "ON m.max_orders_key = a.orders_key ";
            execSQL += "AND m.tad_order_id = a.tad_order_id ";
            execSQL += "WHERE a.status NOT IN ('FILLED','CANCELLED','VAPORIZED','PARTIALLYCANCELLED') ";
            execSQL += "ORDER BY a.tad_order_id ";
            dtOut = DB.execSQL(execSQL, this.gbl_conn);
            
            return dtOut;
        }
        
        private DataTable SetApprovedFlag(DataTable dtIn)
        {
            // Clone the structure of the input DataTable
            DataTable dtOutCloned = dtIn.Clone();
            
            // Create a new DataColumn for the "Approved" column
            DataColumn approvedColumn = new DataColumn("Approved", typeof(bool));
            // Add the "Approved" column as the first column
            dtOutCloned.Columns.Add(approvedColumn);
            // Set the display index of the "Approved" column to 0
            approvedColumn.SetOrdinal(0);


            // Copy the data from the input DataTable to the cloned DataTable
            foreach (DataRow row in dtIn.Rows)
            {
           
                DataRow newRow = dtOutCloned.NewRow();
                // Set the value for the "Approved" column based on the "status" field
                string status = row["status"].ToString();
                bool approved = false;
                if(status == "APPROVED") { approved = true; }
                newRow["Approved"] = approved;

                // Copy data from original row to new row (except for the "Approved" column)
                foreach (DataColumn col in dtIn.Columns)
                {
                    if (col.ColumnName != "Approved")
                        newRow[col.ColumnName] = row[col.ColumnName];
                }

                // Add the new row to the cloned DataTable
                dtOutCloned.Rows.Add(newRow);
            }
          
            return dtOutCloned;
        }
        /// <summary>
        /// reset the order status based on the tad_order_id
        /// and the orders_key -> we dont want to set the status for all row
        /// entries for a particular order
        /// initially used to approve "proposed" orders but technically
        /// could be used to set any status
        /// this inserts a new row into orders with the udpated status
        /// </summary>
        /// <param name="tad_order_id"></param>
        /// <param name="status"></param>
        public void UpdateOrderStatus(string tad_order_id, string status, string orders_key)
        {
            Table tbl = new Table("Orders", this.gbl_conn, select_data:false);
            List<string> lstTblColumns = tbl.get_table_columns();
            lstTblColumns.Remove("orders_key");
            string execSQL = "IF OBJECT_ID('tempdb..#update') IS NOT NULL ";
            execSQL += " DROP TABLE #update ";
            execSQL += " SELECT * INTO #update FROM orders WHERE tad_order_id = '" + tad_order_id + "'";
            execSQL += " AND orders_key = " + orders_key;
            execSQL += " UPDATE #update SET runtime = getdate(), status = '" + status + "' ";
            execSQL += " ALTER table #update DROP COLUMN orders_key ";
            execSQL += "INSERT orders(";
            //loop over table columns and create insert statement for new row
            string insert_columns = "";
            for(int i =0; i < lstTblColumns.Count; i++)
            {
                insert_columns += lstTblColumns[i].ToString() + ",";
            }
            insert_columns = insert_columns.Substring(0, insert_columns.Length - 1) ;
            execSQL += insert_columns + ")";
            execSQL += " SELECT TOP 1 " + insert_columns + " FROM #update";
            DB.execSQL_noresults(execSQL, this.gbl_conn);
        }
        /// <summary>
        /// updates the cancel_tdx bit column in Orders table based on a tad_order_id/orders_key combo
        /// </summary>
        /// <param name="tad_order_id"></param>
        /// <param name="status"></param>
        /// <param name="orders_key"></param>
        public void UpdateCancelTDXStatus(string tad_order_id,  bool status, string orders_key)
        {
            Table tbl = new Table("Orders", this.gbl_conn, select_data: false);
            List<string> lstTblColumns = tbl.get_table_columns();
            lstTblColumns.Remove("orders_key");
            string execSQL = "IF OBJECT_ID('tempdb..#update') IS NOT NULL ";
            execSQL += " DROP TABLE #update ";
            execSQL += " SELECT * INTO #update FROM orders WHERE tad_order_id = '" + tad_order_id + "'";
            execSQL += " AND orders_key = " + orders_key;
            execSQL += " UPDATE #update SET runtime = getdate(), cancel_tdx = " + (status? 1:0).ToString() + " ";
            execSQL += " ALTER table #update DROP COLUMN orders_key ";
            execSQL += "INSERT orders(";
            //loop over table columns and create insert statement for new row
            string insert_columns = "";
            for (int i = 0; i < lstTblColumns.Count; i++)
            {
                insert_columns += lstTblColumns[i].ToString() + ",";
            }
            insert_columns = insert_columns.Substring(0, insert_columns.Length - 1);
            execSQL += insert_columns + ")";
            execSQL += " SELECT TOP 1 " + insert_columns + " FROM #update";
            DB.execSQL_noresults(execSQL, this.gbl_conn);
        }
        public static void CancelTDX(List<DataRow> listOfDataRows, SqlConnection conn)
        {
            try
            {
                //Table tblOrder = new Table("orders", conn, select_data: false);
                //List<string> ordersColumns = new List<string>();
                //ordersColumns = tblOrder.get_table_columns();
                //ordersColumns.Remove("orders_key");
                //string InsertSQL = "INSERT orders(";
                ////build up string of insert columns
                //for(int i = 0; i < ordersColumns.Count; i++)
                //{
                //    InsertSQL += ordersColumns[i].ToString() + ",";
                //}
                //InsertSQL = InsertSQL.Substring(0, InsertSQL.Length - 1);
                //InsertSQL += ") VALUES ('";
                //for (int i =0; i < listOfDataRows.Count;i++)
                //{
                //    row
                //}
            }
            catch(Exception ex)
            {
                throw new Exception("CancelTDX error: " + ex.Message);
            }
        }
    }
}
