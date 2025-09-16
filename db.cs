using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;
using System.Data.SqlClient;
//using IronPython;
//using Microsoft.Scripting;
//using IronPython.Hosting;
//using IronPython.Runtime;
//using Microsoft.Scripting.Hosting;



using System.Text.RegularExpressions;

namespace TDX
{
    class db
    {
        /// <summary>
        /// checks if a provided sqlconnection instance is a valid connection path
        /// </summary>
        /// <param name="conn"></param>
        /// <returns> bool</returns>
        public static bool IsValidConnection(SqlConnection conn)
        {
            try
            {
                conn.Open();
                return true;
            }
            catch(SqlException ex)
            {
                return false;

            }
            finally
            {
                conn.Close();
            }

        }
        public DataTable getSprocresults(string sprocname, SqlConnection conn)
        {
            DataTable dt = new DataTable();
            // Ensure any existing SqlDataReader is closed before executing a new query
            if (conn.State == ConnectionState.Open)
            {
                conn.Close();
            }
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            using (var cmd = new SqlCommand(sprocname, conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 0;
                using (var da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }
            if (conn != null) { conn.Close(); }

            return dt;
        }
        /// <summary>
        /// execute a parameterized sproc , returns datatable
        /// </summary>
        /// <param name="sprocname"></param>
        /// <param name="arrParams"></param>
        /// <param name="conn"></param>
        /// <returns></returns>
        public DataTable getSprocresults(string sprocname, string[,] arrParams, SqlConnection conn)
        {
            DataTable dt = new DataTable();
            int rows = arrParams.GetUpperBound(0);
            // Ensure any existing SqlDataReader is closed before executing a new query
            if (conn.State == ConnectionState.Open)
            {
                conn.Close();
            }
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            using (var cmd = new SqlCommand(sprocname, conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 0;
                for (int i = 0; i <= rows; i++)
                {
                    string par = arrParams[i, 0].ToString();
                    string val = arrParams[i, 1].ToString();
                    cmd.Parameters.AddWithValue(par, val);
                }
                using (var da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }
            if (conn != null) { conn.Close(); }

            return dt;
        }
        public DataTable get_table(string tablename,SqlConnection conn)
        {
            DataTable dt = execSQL("SELECT * FROM " + tablename,conn);
            return dt;
        }
        public DataTable get_table(string tablename,string where, SqlConnection conn)
        {
            string sql_text = "SELECT * FROM " + tablename + " WHERE " + where;
            DataTable dt = execSQL(sql_text, conn);
            return dt;
        }

        public DataSet getSprocResults(string sprocname, Dictionary<string, string> dictParams, SqlConnection conn)
        {
            DataSet dsReturn = new DataSet();
            SqlDataAdapter da = new SqlDataAdapter();

            try
            {
                // Ensure any existing SqlDataReader is closed before executing a new query
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }
                using (var cmd = new SqlCommand(sprocname, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 0;

                    foreach (var item in dictParams)
                    {
                        string par = item.Key;
                        string val = item.Value;
                        cmd.Parameters.AddWithValue(par, val);
                    }

                    using (da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dsReturn);
                    }
                }

                // Optionally, you can reorder columns for each DataTable in the DataSet
                //foreach (DataTable dataTable in dsReturn.Tables)
                //{
                //    ReorderDataTableColumns(dataTable, sprocname, conn);
                //}

                return dsReturn;
            }
            catch (Exception ex)
            {
                string s = ex.Message;
                return dsReturn;
            }
            finally
            {
                da.Dispose();
            }
        }

        private void ReorderDataTableColumns(DataTable dataTable, string sprocname, SqlConnection conn)
        {
            // Ensure any existing SqlDataReader is closed before executing a new query
            if (conn.State == ConnectionState.Open)
            {
                conn.Close();
            }
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            using (var cmd = new SqlCommand(sprocname, conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.KeyInfo))
                {
                    DataTable schemaTable = reader.GetSchemaTable();

                    // Create a dictionary to store the index of each column in the result set
                    Dictionary<string, int> columnIndexMap = new Dictionary<string, int>();

                    for (int i = 0; i < schemaTable.Rows.Count; i++)
                    {
                        string columnName = schemaTable.Rows[i]["ColumnName"].ToString();
                        columnIndexMap.Add(columnName, i);
                    }

                    // Reorder columns in the existing DataTable
                    DataColumn[] orderedColumns = dataTable.Columns.Cast<DataColumn>()
                        .OrderBy(c => columnIndexMap[c.ColumnName])
                        .ToArray();

                    dataTable.Columns.Clear();
                    dataTable.Columns.AddRange(orderedColumns);
                }
            }
        }


        public void execSQL_noresults(string sql,SqlConnection conn)
        {
            if(conn.State == ConnectionState.Open)
            {
                conn.Close();
            }
            if(conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            //conn.Open();
            using(var cmd  = new SqlCommand(sql,conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.ExecuteNonQuery();
            }
        }
        public DataTable execSQL(string sql, SqlConnection conn)
        {
            DataTable dt = new DataTable();
            if ((conn != null) )
            {
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    // Ensure any existing SqlDataReader is closed before executing a new query
                    if (conn.State == ConnectionState.Open)
                    {
                        conn.Close();
                    }
                    if (conn.State != ConnectionState.Open)
                    {
                        conn.Open();
                    }

                    //// Open the connection explicitly before executing the query
                    //conn.Open();

                    using (var da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
                //if (conn != null) { conn.Close(); }
            }
            return dt;
        }
        /// <summary>
        /// get a dataset with a non-parameterized sproc
        /// </summary>
        /// <param name="sprocname"></param>
        /// <param name="conn"></param>
        /// <returns></returns>
        public DataSet getSprocResults(string sprocname, SqlConnection conn)
        {
            DataSet dsReturn = new DataSet();
            SqlDataAdapter da = new SqlDataAdapter();
            try
            {
                using (var cmd = new SqlCommand(sprocname, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 0;
                    // Ensure any existing SqlDataReader is closed before executing a new query
                    if (conn.State == ConnectionState.Open)
                    {
                        conn.Close();
                    }
                    if (conn.State != ConnectionState.Open)
                    {
                        conn.Open();
                    }
                    using (da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dsReturn);
                    }
                }
                return dsReturn;
            }
            catch (Exception ex)
            {
                string s = ex.Message;
                return dsReturn;
            }
            finally
            {
                da.Dispose();
                dsReturn = null;
            }

        }
        public DataTable get_funds(SqlConnection conn)
        {
           
            string sql_text = "SELECT DISTINCT fundname from funds WHERE  closed = 0 ";
            sql_text += " ORDER BY fundname ";
            DataTable dt = execSQL(sql_text, conn);

            return dt;
        }
        public DataTable get_accounts(SqlConnection conn)
        {
            //string sql_text = "SELECT sa.subaccountname FROM subaccounts sa INNER JOIN accounts a ON a.accountname = sa.accountname ";
            //sql_text += "WHERE a.account_groupname NOT IN ('benchmarks', 'advisory', 'market_neutral') ORDER BY sa.subaccountname";
            string sql_text = "SELECT accountname from accounts WHERE account_groupname NOT in ('benchmarks', 'advisory', 'market_neutral') order by accountname ";
            DataTable dt = execSQL(sql_text, conn);
            return dt;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="from_date">date from</param>
        /// <param name="date"> date to</param>
        /// <param name="subaccount"></param>
        /// <param name="conn"></param>
        /// <returns></returns>
        public DataSet get_slippage(string from_date,string date,string subaccount,SqlConnection conn)
        {
            Dictionary<string, string> arrParams = new Dictionary<string, string>();
            arrParams.Add("@day", date);
            arrParams.Add("@from_day", from_date);
            arrParams.Add("@fundname", subaccount);
            DataSet dt = getSprocResults("get_slippage",arrParams,conn);
            return dt;
        }
        public DataTable get_subaccounts(string accountname,SqlConnection conn)
        {
            string sql_text = "SELECT * from subaccounts WHERE fundname='" + accountname + "' ORDER BY subaccountname";
            DataTable dt = execSQL(sql_text,conn);
            return dt;
        }
        public DataTable get_fund_executions(string fundname,string start_date,string end_date,SqlConnection conn)
        {
            DataTable dt = new DataTable();

            //string sql_text = "with max_runtime as(select execution_id, max(runtime) as runtime from executions WHERE account ='" + fundname + "' group by execution_id), ";
            //sql_text += "broker_id as (select b.broker_id from broker b inner join subaccounts a on a.brokername  = b.broker inner join accounts acc on acc.accountname = a.accountname where acc.accountname ='" + accountname + "' ) ";
            //sql_text += "select distinct e.* from	executions e inner join broker_id b on	e.broker_id  =  b.broker_id inner join max_runtime mr on	mr.execution_id =  e.execution_id ";
            //sql_text += "and		mr.runtime  = e.runtime where	e.account  ='" + accountname + "' and execution_time between '" + start_date + "' AND '" + end_date + "'";
            //sql_text += " ORDER by e.execution_time DESC";
            string sql_text = "IF OBJECT_ID(N'tempdb..#topaccount') IS NOT NULL BEGIN DROP TABLE #topaccount END ";
            sql_text += " select distinct CASE topaccountname WHEN 'None' then s.subaccountname ELSE topaccountname END as topaccountname ";
            sql_text += "INTO #topaccount FROM broker b INNER JOIN subaccounts s ON s.broker_code_exec = b.broker_code WHERE broker_type = 'execution'";
            sql_text += "AND s.fundname='" + fundname + "'";
            sql_text += ";with max_runtime as(select execution_id, max(runtime) as runtime from executions WHERE account in(select topaccountname from #topaccount) group by execution_id),";
            sql_text += " broker_id as (select b.broker_id from broker b inner join subaccounts a on a.broker_code_exec = b.broker_code ";
            sql_text += " inner join funds f on f.fundname = a.fundname where a.fundname = '" + fundname + "')";
            sql_text += "select distinct e.* from executions e inner join broker_id b on e.broker_id  =  b.broker_id ";
            sql_text += " inner join max_runtime mr on mr.execution_id = e.execution_id and mr.runtime = e.runtime ";
            sql_text += " where e.account  in(select topaccountname from #topaccount) and execution_time between '" + start_date + "' AND '" + end_date  + "'" ;
            sql_text += " ORDER by e.execution_time DESC";

            dt = execSQL(sql_text, conn);
            return dt;
        }
        public DataTable get_account_transactions(string fundname, string start_date, string end_date, SqlConnection conn)
        {
            DataTable dt = new DataTable();

            string sql_text = "with max_runtime as(select transaction_id, max(runtime) as runtime from transactions WHERE fund ='" + fundname + "' and tx_type = 'trade'  group by transaction_id), ";
            sql_text += "broker_id as (select b.broker_id from broker b inner join subaccounts s on s.broker_code  = b.broker_code  ) ";
            sql_text += "select DISTINCT e.* from	transactions e inner join broker_id b on	e.broker_id  =  b.broker_id inner join max_runtime mr on	mr.transaction_id =  e.transaction_id ";
            sql_text += "and mr.runtime  = e.runtime where	e.fund  ='" + fundname + "' and execution_time between '" + start_date + "' AND '" + end_date + "'";
            sql_text += " ORDER by e.execution_time";
            dt = execSQL(sql_text, conn);
            return dt;
        }

        public DataTable ticker_freezer(string subaccount,SqlConnection conn)
        {
            DataTable dt = new DataTable();
            string sql_text = "SELECT * FROM ticker_freezer WHERE resolved = 0 and error_code not in (125)";
            dt = execSQL(sql_text, conn);
            return dt;
        }
        public DataSet get_equities_pnl(string accountname,string posn_date,string posn_date_t1,SqlConnection conn)
        {
            Dictionary<string, string> arrParams = new Dictionary<string, string>();
            arrParams.Add("@account", accountname);
            arrParams.Add("@posn_date", posn_date);
            arrParams.Add("@posn_date_1", posn_date_t1);
            DataSet ds = getSprocResults("get_equity_pnl",arrParams,conn);
            return ds;
        }
        public DataSet get_futures_pnl(string accountname,string posn_date,string posn_date_t1,SqlConnection conn)
        {
            Dictionary<string, string> arrParams = new Dictionary<string, string>();
            arrParams.Add("@accountname", accountname);
            arrParams.Add("@mtm_date", posn_date);
            arrParams.Add("@mtm_date_1", posn_date_t1);
            DataSet ds = getSprocResults("get_futures_pnl", arrParams, conn);
            return ds;
        }
        public DataSet get_positions(string accountname,string posn_date,SqlConnection conn)
        {
            Dictionary<string, string> arrParams = new Dictionary<string, string>();
            arrParams.Add("@account", accountname);
            arrParams.Add("@posn_date", posn_date);
            DataSet ds = getSprocResults("get_positions", arrParams, conn);
            return ds;
        }
        public DataSet get_order_rejections(string from_date,string to_date,SqlConnection conn)
        {
            DataSet ds_out =new DataSet();
            Dictionary<string, string> arrParams = new Dictionary<string, string>();
            arrParams.Add("@from", from_date);
            arrParams.Add("@to", to_date);
            ds_out = getSprocResults("get_order_rejections", arrParams, conn);
            return ds_out;
        }
        public DataSet get_cash_dividends_paid(string subaccount,string from_date,string to_date,SqlConnection conn)
        {
            Dictionary<string, string> arrParams = new Dictionary<string, string>();
            arrParams.Add("@subaccounts", subaccount);
            arrParams.Add("@from_date", from_date);
            arrParams.Add("@to_date", to_date);
            DataSet ds = getSprocResults("get_cashdividend_paid", arrParams, conn);
            return ds;
        }
        public DataSet get_cash_dividends_paid(string subaccount, SqlConnection conn)
        {
            Dictionary<string, string> arrParams = new Dictionary<string, string>();
            arrParams.Add("@subaccount", subaccount);
            DataSet ds = getSprocResults("get_cashdividends_paid", arrParams, conn);
            return ds;
        }
        public DataTable get_txtype_details(string action, SqlConnection conn)
        {
            string where = "";
            if (action == "BUY")
            {
                where = " AND detail in('rebate','stockloan','interest','deposit')";
            }
            else if (action =="SELL")
            {
                where = " AND detail not in ('rebate','stockloan','interest','deposit')";
            }
            string sql_text = "SELECT detail FROM tx_type_detail WHERE tx_type = 'cash_depo_pay'" + where + " ORDER BY detail";
            DataTable dt = execSQL(sql_text, conn);
            return dt;
        }
        public DataTable get_cash_currencies(SqlConnection conn)
        {
            string sql_text = "select	tickername from tickers where instrument = 'cash' and category = 'fx' and description like '%cash' order by tickername";
            DataTable dt = execSQL(sql_text, conn);
            return dt;
        }
        public DataTable get_tad_ids_master(SqlConnection conn)
        {
            string sql_text = @"with tad_ids as (
                select max(runtime) as runtime,tad_id,tickername from tad_ids_master group by tad_id, tickername
	            )
            select tim.*
            from tad_ids_master tim
            inner
            join
              tad_ids ti
            on ti.runtime = tim.runtime
            and     ti.tad_id = tim.tad_id
            and ti.tickername = tim.tickername";
            DataTable dt = execSQL(sql_text, conn);
            return dt;
        }
        public DataTable get_broker_by_account(string subaccount,SqlConnection conn)
        {
            DataTable dt = new DataTable();
            string sql_text = "select DISTINCT brokername from accounts where subaccount='" + subaccount + "'";
            dt = execSQL(sql_text,conn);
            return dt;
        }
        public string transaction_id(string tad_id,string broker,string subaccount)
        {
            string tx_id = "";
            Guid guid = Guid.NewGuid();
            Random rand = new Random();
            int index = rand.Next(1, 10000);
            tx_id = tad_id + "-" + broker + "-" + subaccount + "-" + DateTime.Now.ToString("yyyyMMddhhmmss") + guid.ToString() + index.ToString();
            return tx_id;
        }
        public int get_broker_id(string brokername,SqlConnection conn)
        {
            int broker_id = -1;
            string sql_text = "SELECT broker_id from dbo.broker WHERE broker ='" + brokername + "'";
            DataTable dt = execSQL(sql_text,conn);
            if(dt.Rows.Count>0)
            {
               string broker_id_string = dt.Rows[0]["broker_id"].ToString();
                broker_id = int.Parse(broker_id_string);
            }
            return broker_id;
        }
        public void insert_closeout_position(string account,string subaccount,string tad_id,SqlConnection conn)
        {
            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string sql_text = "INSERT dbo.closeout_positions VALUES(";
            sql_text += "'" + now + "','" + account + "','" + subaccount + "','" + tad_id + "',1)";
            execSQL_noresults(sql_text, conn);
        }
       
        public string get_master_account(string subaccount,SqlConnection conn)
        {
            string master_account = "";
            string sql_text = "SELECT account FROM accounts WHERE account='" + subaccount + "'";
            DataTable dt = execSQL(sql_text, conn);
            if(dt.Rows.Count > 0)
            {
                master_account = dt.Rows[0]["account"].ToString();
            }
            return master_account;
        }

        //public bool add_portfolio(DataTable portfoliodata,string portfolioname ,SqlConnection conn)
        //{
        //    bool ret = false;
            
        //    if (!portfoliodata.Columns.Contains("portfolioname"))
        //    {
        //        portfoliodata.Columns.Add("portfolioname");
        //    }

        //    if (portfoliodata.Rows.Count > 0)
        //    {
        //        for (int i = 0; i < portfoliodata.Rows.Count; i++)
        //        {
        //            DataRow row = portfoliodata.Rows[i];
        //            row["portfolioname"] = portfolioname;
        //        }
        //    }

        //    try
        //    {
        //        using (conn)
        //        {
        //            conn.Open();
        //            using (SqlBulkCopy bulkcopy = new SqlBulkCopy(conn))
        //            {
        //                foreach (DataColumn column in portfoliodata.Columns)
        //                {
        //                    //if(column.ColumnName in )
        //                    bulkcopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        //                }
        //                bulkcopy.DestinationTableName = "portfolio_weights";
        //                bulkcopy.WriteToServer(portfoliodata);
        //            }
        //            ret = true;
        //        }
        //        return ret;
        //    }
        //    catch(Exception ex)
        //    {
        //        string msg = ex.Message;
        //        return ret;
        //    }
        //}

        public DataTable get_portfolionames(SqlConnection conn)
        {
            DataTable dt = new DataTable();
            string sql_text = ";with portf as (select distinct portfolioname,max(runtime) as runtime from portfolio_weights group by portfolioname) ";
            sql_text += "select	distinct p.portfolioname from portfolio_weights p inner join portf po on po.portfolioname = p.portfolioname and p.runtime = po.runtime order by p.portfolioname";
            dt = execSQL(sql_text, conn);
            return dt;
        }
        public DataTable get_portfoliodetails(string portfolioname,SqlConnection conn)
        {
            DataTable dt = new DataTable();
            string sql_text = ";with portf as (select distinct portfolioname,max(runtime) as runtime from portfolio_weights where portfolioname ='" + portfolioname +"' group by portfolioname) ";
            sql_text += "select p.*,mp.max_pos from portfolio_weights p inner join portf po on po.portfolioname = p.portfolioname and p.runtime = po.runtime ";
            sql_text += " FULL OUTER JOIN max_position mp on mp.portfolioname = p.portfolioname and mp.tickername = p.tickername ";
            sql_text += "WHERE p.portfolioname is NOT NULL order by p.portfolioname,p.tickername";
            dt = execSQL(sql_text, conn);
            return dt;
        }
        public string thaw_tad_id(string runtime,string ems,string broker_code_exec,string fundname,string subaccountname,
            string execaccountname,string tad_id,string tickername,string error_code,string benchmarkname,string notes,SqlConnection conn)
        {
            //bool isNumber = int.TryParse(broker_id);
            string sql_text = "UPDATE ticker_freezer SET resolved=1,notes='" + notes + "', runtime_resolved='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'";
            sql_text += " WHERE runtime ='" + runtime + "' AND emsname ='" + ems + "' AND broker_code_exec='" + broker_code_exec + "' AND fundname='";
            sql_text += fundname + "' and subaccountname='" + subaccountname + "' and tad_id = '" + tad_id + "'" + " AND tickername='" + tickername + "' AND error_code=" + error_code;
            sql_text += " AND benchmarkname='" + benchmarkname + "'" ;
            execSQL(sql_text,conn);
            return sql_text;
        }
        /// <summary>
        /// creates the SQL to update the NAV table to add a deposit
        /// </summary>
        /// <param name="account"></param>
        /// <param name="subaccount"></param>
        /// <param name="from_date"></param>
        /// <param name="amount"></param>
        /// <param name="conn"></param>
        public void nav_add_depo(string subaccount,string from_date,double amount,SqlConnection conn)
        {
            string sql = "UPDATE dbo.nav SET deposits = deposits +" + amount.ToString() + ", net_liquidation_value= net_liquidation_value + " + amount.ToString();
            sql += " WHERE subaccountname='" + subaccount + "' AND navDate >='" + from_date + "'";
            execSQL(sql, conn);
        }
        /// <summary>
        /// creates the SQL to update the NAV table for a payment
        /// </summary>
        /// <param name="subaccount"></param>
        /// <param name="from_date"></param>
        /// <param name="amount"></param>
        /// <param name="conn"></param>
        public void nav_add_payment(string subaccount,string from_date,double amount,SqlConnection conn)
        {
            string sql = "UPDATE dbo.nav SET payments = payments +" + amount.ToString() + ", net_liquidation_value= net_liquidation_value - " + amount.ToString();
            sql += " WHERE subaccountname='" + subaccount + "' AND navDate >='" + from_date + "'";
            execSQL(sql, conn);
        }
        /// <summary>
        /// creates the SQL to update the NAV table for a comm_fee adjustment
        /// </summary>
        /// <param name="subaccount"></param>
        /// <param name="from_date"></param>
        /// <param name="amount"></param>
        /// <param name="conn"></param>
        public void nav_add_comm_fee_adj(string subaccount,string from_date,double amount,SqlConnection conn)
        {
            string sql = "UPDATE dbo.nav SET comm_fee_adj = comm_fee_adj + " + amount.ToString() + ", net_liquidation_value= net_liquidation_value + " + amount.ToString();
            sql += " WHERE subaccountname='" + subaccount + "' AND navDate >='" + from_date + "'";
            execSQL(sql, conn);
        }
        /// <summary>
        /// creates the SQL to update the NAV table for an expense/s
        /// </summary>
        /// <param name="subaccount"></param>
        /// <param name="from_date"></param>
        /// <param name="amount"></param>
        /// <param name="conn"></param>
        public void nav_add_expense(string subaccount, string from_date, double amount, SqlConnection conn)
        {
            string sql = "UPDATE dbo.nav SET expenses = expenses + " + amount.ToString() + ", net_liquidation_value= net_liquidation_value - " + amount.ToString();
            sql += " WHERE subaccountname='" + subaccount + "' AND navDate >='" + from_date + "'";
            execSQL(sql, conn);
        }
    }
}
