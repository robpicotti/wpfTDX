using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
namespace TDX
{
    class TickerFreezer
    {
        private Table tbl { get; set; }
        public SqlConnection gbl_conn;
        TDX.db _db = new TDX.db();
        public TickerFreezer(SqlConnection conn)
        {
            gbl_conn = conn;
            this.tbl = new Table("ticker_freezer", gbl_conn);
        }
        /// <summary>
        /// adds an entry to ticker_freezer table to freeze ticker. NO slack messages
        /// </summary>
        /// <param name="runtime"></param>
        /// <param name="error_code"></param>
        /// <param name="error_string"></param>
        /// <param name="freeze_expiration"></param>
        /// <param name="_override"></param>
        /// <param name="override_expiration"></param>
        /// <param name="notes"></param>
        /// <param name="runtime_resolved"></param>
        /// <param name="ems"></param>
        /// <param name="broker_code_exec"></param>
        /// <param name="fundname"></param>
        /// <param name="subaccountname"></param>
        /// <param name="execaccountname"></param>
        /// <param name="tad_id"></param>
        /// <param name="tickername"></param>
        /// <param name="benchmarkname"></param>
        public void Freeze(string runtime, int error_code, string error_string, DateTime? freeze_expiration,
            int _override,DateTime? override_expiration,string notes,DateTime? runtime_resolved,
            string ems="*",string broker_code_exec= "*",
            string fundname="*",string subaccountname="*",string execaccountname="*",string tad_id="*",string tickername="*",
            string benchmarkname = "*"
            )
        {
            int resolved = 0;
            string expiration = "";
            string override_expire = "";
            string rt_resolved = "";
            string override_freeze = "";
            if(freeze_expiration.HasValue)
            {
                expiration = "'" + freeze_expiration.Value.ToString("dd-MMM-yyyy HH:mm:ss") + "'" ;
            }
            else
            {
                expiration = "NULL";
            }
            if(override_expiration.HasValue)
            {
                override_expire = "'" + override_expiration.Value.ToString("dd-MMM-yyyy HH:mm:ss") + "'";
            }
            else
            {
                override_expire = "NULL";
            }
            if(runtime_resolved.HasValue)
            {
                rt_resolved = "'" + runtime_resolved.Value.ToString("dd-MMM-yyyy HH:mm:ss") + "'";
            }
            else
            {
                rt_resolved = "NULL";
            }
            string tsql = "INSERT ticker_freezer VALUES('" + runtime + "','";
            tsql +=   ems + "','" + broker_code_exec + "','" + fundname + "','";
            tsql += subaccountname + "','" + execaccountname + "','" + tad_id + "','";
            tsql += tickername + "'," + error_code.ToString() + ",'" + error_string + "',";
            tsql += resolved + "," + expiration + "," + _override.ToString() + "," + override_expire;
            tsql += ",'" + benchmarkname + "','" + notes + "'," + rt_resolved + ")";
            _db.execSQL(tsql, gbl_conn);
        }
      
        public string Thaw(string runtime, string ems, string broker_code_exec, string fundname, string subaccountname,string execaccountname, string tad_id, 
            string tickername,string error_code, string benchmarkname,string notes,SqlConnection gbl_conn)
        {
            return _db.thaw_tad_id(runtime, ems, broker_code_exec, fundname, subaccountname,execaccountname, tad_id,tickername, error_code,benchmarkname,notes, gbl_conn);
        }
        
        public void Override_freezer(string runtime, string ems, string brokercode_exec, string fundname, string execaccountname, string subaccountname, string tad_id, string error_code,
            bool add_override,string expiration,SqlConnection gbl_conn)
        {
            string SQL = generate_override_freezer_sql(runtime, ems, brokercode_exec, fundname, execaccountname,subaccountname, tad_id, error_code,add_override,expiration);
            _db.execSQL(SQL, gbl_conn); ;
        }
        
        private string generate_override_freezer_sql(string runtime, string ems, string brokercode_exec, string fundname, string execaccountname,string subaccountname,
            string tad_id, string error_code,bool add_override,string expiration)
        {
            string _override = "1";
            if (!add_override) { _override = "0"; } //remove the override
            string t_sql = "UPDATE " + tbl.table_name ;

            t_sql += " SET override = " + _override;
            if (expiration == "After the close")
            {
                string override_expiration = DateTime.Now.Date.AddDays(1).ToString("yyyy-MM-dd HH:mm:ss");
                t_sql += ",  override_expiration='" + override_expiration + "'";
            }
            t_sql += " WHERE runtime='" + runtime + "' AND broker_code_exec ='" + brokercode_exec + "' AND fundname='" + fundname + "' AND subaccountname='" + subaccountname + "'";
            t_sql += " AND execaccountname = '" + execaccountname + "'";
            t_sql += " AND tad_id='" + tad_id + "' AND error_code=" + error_code;


            return t_sql;
        }
        
        private DataTable SearchTadId(string tad_id_wildcard)
        {
            DataTable dtOut = new DataTable();
            DataTable dtTadIdsMaster;
            Table tblTadIdsMaster = new Table("tad_ids_master", gbl_conn);
            dtTadIdsMaster = tblTadIdsMaster.table_data.Copy();
            var query = from row in dtTadIdsMaster.AsEnumerable()
                        where row.Field<string>("tad_id").Contains(tad_id_wildcard)
                        select row;
            dtOut = dtTadIdsMaster.Clone();
            foreach (var resultRow in query)
            {
                dtOut.ImportRow(resultRow);
            }
            return dtOut; 
        }
    }
}
