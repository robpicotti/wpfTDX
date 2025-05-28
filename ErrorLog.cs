using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;

namespace TDX
{
    class ErrorLog
    {
        public SqlConnection gbl_conn;
        public DataTable dtErrorLog { get; set; }
        public DateTime max_runtime { get; set; }
        public double minutes_delta { get; set; }
        public HeartBeats heartbeat { get; set; }
        /// <summary>
        /// gets the latest heartbeat based on the application(created_by) flag
        /// </summary>
        /// <param name="created_by"></param>
        /// <returns></returns>
        public ErrorLog get_heartbeat(string created_by,SqlConnection conn)
        {
            string where_clause = "WHERE created_by='" + created_by + "' AND heartbeat =1";
            List<string> exclude_cols = null;
            string pks = "created_by";
            Table tbl = new Table("error_log", conn, where_clause,exclude_cols,pks); //,pks="created_by");
            this.dtErrorLog = tbl.table_data;
            if (this.dtErrorLog.Rows.Count > 0)
            {
                DataRow row = this.dtErrorLog.Rows[this.dtErrorLog.Rows.Count - 1];
                this.max_runtime = DateTime.Parse(row["runtime"].ToString());

            }
            else
            {
                this.max_runtime = DateTime.Parse("1900-01-01");
            }
            DateTime now = DateTime.Now.ToUniversalTime();
            TimeSpan span = now - this.max_runtime;
            this.minutes_delta = span.TotalMinutes;
            return this;
        }


    }

    class HeartBeats
    {


        public SqlConnection gbl_conn { get; set; }
        public List<ProcessBeat> ListHeartBeats {get;set;}
        public DataTable dtBeat;
        public string HostEnvironment { get; set; }

        public HeartBeats(SqlConnection sql_conn,string hostenvironment)
        {
            gbl_conn = sql_conn;
            this.HostEnvironment = hostenvironment;
            ListHeartBeats = new List<ProcessBeat>();
            dtBeat = GetMonitoredHeartBeats(gbl_conn);
            Refresh();
        }
        public void Refresh()
        {
            for (int i = 0; i < dtBeat.Rows.Count; i++)
            {
                ErrorLog elog = new ErrorLog();
                ProcessBeat newHeartBeat = new ProcessBeat();
                newHeartBeat.process_name = dtBeat.Rows[i]["process_name"].ToString();
                newHeartBeat.age_limit_minutes = int.Parse(dtBeat.Rows[i]["age_limit_minutes"].ToString());
                newHeartBeat.minutes_delta = elog.get_heartbeat(newHeartBeat.process_name, gbl_conn).minutes_delta;
                ListHeartBeats.Add(newHeartBeat);
            }
        }
        private DataTable GetMonitoredHeartBeats(SqlConnection conn)
        {
            gbl_conn = conn;
            DataTable dtHB = new DataTable();
            Table tbl = new Table("heartbeats", conn, "WHERE monitored =1 and env='" + this.HostEnvironment + "'", null, "runtime,process_name");
            dtHB = tbl.table_data;
            return dtHB;
        }
    }
    class ProcessBeat
    {
        /// <summary>
        /// the name of the process whose heartbeat you are monitoring
        /// </summary>
        public string process_name { get; set; }
        /// <summary>
        /// the amount of time allowed between heartbeats
        /// </summary>
        public int age_limit_minutes { get; set; }

        /// <summary>
        /// the actual time between heartbeats measured
        /// </summary>
        public double minutes_delta { get; set; }
        public ProcessBeat()
        {

        }
    }
}
