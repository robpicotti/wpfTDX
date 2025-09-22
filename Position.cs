using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
namespace TDX
{
    public class Position
    {
        public SqlConnection gbl_conn;
        TDX.db _db = new TDX.db();
        public string subaccount { get; set; }
        public DataTable position { get; set; }
        public DataTable cash_position { get; set; }
        public DateTime position_date { get; set; }

        public Position(string subaccount, SqlConnection conn)
        {
            this.subaccount = subaccount;
            this.position_date = DateTime.Today;
            this.gbl_conn = conn;
        }
        public Position positions()
        {
            DataSet ds = _db.get_positions(this.subaccount, this.position_date.ToString("yyyy-MM-dd"), gbl_conn);
            if (ds.Tables.Count > 0)
            {
                this.position = ds.Tables[0];
                this.cash_position = ds.Tables[1];
            }
            return this;
        }
        public Position positions(DateTime posn_date)
        {
            this.position_date = posn_date.Date;
            DataSet ds = _db.get_positions(this.subaccount, this.position_date.ToString("yyyy-mm-dd"), gbl_conn);
            this.position = ds.Tables[0];
            this.cash_position = ds.Tables[1];
            return this;
        }
        /// <summary>
        /// allows you to scale in and out of positions for a particular tickername/subaccount
        /// </summary>
        public void scale_position(string fundname, string tickername, double scale_stepsize, 
            double scaled_target, string scale_type, double scaled_timestep = 5)
        {
            string now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            DataTable dt = new DataTable();
            dt.Columns.Add("runtime");
            dt.Columns.Add("fundgroupname");
            dt.Columns.Add("fundname");
            //dt.Columns.Add("account");
            //dt.Columns.Add("subaccount");
            dt.Columns.Add("tickername");
            dt.Columns.Add("scaled_stepsize");
            dt.Columns.Add("scaled_percent");
            dt.Columns.Add("scaled_timestep");
            dt.Columns.Add("scaled_target");
            dt.Columns.Add("scaled_type");

            DataRow drow = dt.NewRow();
            drow["runtime"] = now;
            drow["fundgroupname"] = "*";
            drow["fundname"] = fundname;
            //drow["subaccount"] = this.subaccount;
            drow["tickername"] = tickername;
            drow["scaled_stepsize"] = scale_stepsize;
            if (scale_type.ToUpper() != "FILTERED")
            {
                drow["scaled_percent"] = get_scaled_percent(fundname, tickername);
            }
            else
            {
                Table tbl_scaled = new Table("scaled_positions", gbl_conn);
                string _where  = "WHERE fundname='" + fundname + "' AND tickername='" + tickername + "'";
                DataTable dtScaled = tbl_scaled.select_latest(_where).table_data;
                double? _scaledPercent_filtered = dtScaled.Rows.Count > 0 ? dtScaled.Rows[0].Field<double?>("scaled_percent") : 1;
                drow["scaled_percent"] = _scaledPercent_filtered;
            }
                drow["scaled_target"] = scaled_target;
            drow["scaled_timestep"] = scaled_timestep;
            drow["scaled_type"] = scale_type;
            dt.Rows.Add(drow);
            Table tbl = new Table("scaled_positions", gbl_conn);
            tbl.load_dataTable(dt);

        }
        /// <summary>
        /// returns -1 if there is no tickername in target_positions
        /// </summary>
        /// <param name="tickername"></param>
        /// <returns></returns>
        public double? get_scaled_percent(string fundname,string tickername)
        {
            string where = "WHERE tickername='" + tickername +  "' AND fundname='" + fundname + "'";
            Table tbl = new Table("target_positions", gbl_conn, where);

            if (tbl.table_data.Rows.Count == 0) return null;

            var row = tbl.table_data.Rows[0];
            // If the DataTable column is numeric, this maps DBNull -> null automatically:
            return row.Field<double?>("scaled_percent");
        }
        public static double? get_scaled_percent(string fundname,  string tickername, SqlConnection conn)
        {
            string where = "WHERE tickername='" + tickername +  "' AND fundname='" + fundname + "'";
            Table tbl = new Table("target_positions", conn, where);
            if (tbl.table_data.Rows.Count == 0) return null;
            var row = tbl.table_data.Rows[0];
            // If the DataTable column is numeric, this maps DBNull -> null automatically:
            return row.Field<double?>("scaled_percent");
        }
        public  DataTable get_daily_positions(string fundName, string tad_id,string tickername,DateTime startDate, DateTime endDate)
        {
            DataTable dtOut = new DataTable();
            try
            {
                string tsql = "DECLARE @fund VARCHAR(100), @tad_id VARCHAR(80), @tickername VARCHAR(80),  @start DATETIME, @end DATETIME;";
                tsql += " SET @fund ='" + fundName + "'";
                tsql += " SET @tad_id ='" + tad_id + "'";
                tsql += " SET @tickername='" + tickername + "'";
                tsql += " SET @start = '" + startDate.ToString("dd-MMM-yyyy HH:mm:ss") + "'";
                tsql += " SET @end = '" + endDate.ToString("dd-MMM-yyyy HH:mm:ss") + "'";
                tsql += " SELECT @end =  dateadd(s,-1,dateadd(dd,1,@end))";
                tsql += " IF OBJECT_ID('tempdb..#data') IS NOT NULL DROP TABLE #data;";
                tsql += " WITH net_qty (execution_time, net_qty) AS (";
                tsql += " SELECT a.execution_time,SUM(a.qty) AS net_qty FROM( SELECT tad_id,";
                tsql += " tickername,execution_time,CASE action WHEN 'SELL' THEN executed_qty * -1 ELSE executed_qty ";
                tsql += " END AS qty FROM transactions WHERE tx_type = 'trade' AND fund = @fund ";
                tsql += " AND tad_id = @tad_id AND tickername = @tickername) a GROUP BY a.execution_time), ";
                tsql += " cum_qty AS(SELECT execution_time,net_qty AS net_traded,SUM(net_qty) OVER (ORDER BY execution_time) AS net_position";
                tsql += " FROM net_qty) SELECT *,ROW_NUMBER() OVER(PARTITION BY CONVERT(DATE, execution_time) ORDER BY execution_time DESC) AS row_num ";
                tsql += " INTO #data FROM cum_qty WHERE execution_time BETWEEN @start AND @end; ";
                tsql += " SELECT CONVERT(DATE, execution_time) AS execution_date,";
                tsql += " MAX(CASE WHEN row_num = 1 THEN net_position ELSE NULL END) AS latest_net_position ";
                tsql += " FROM #data GROUP BY CONVERT(DATE, execution_time); ";
                
                dtOut = this._db.execSQL(tsql, this.gbl_conn);

            }
            catch (Exception ex)
            {
                throw new Exception("get_daily_position error: " + ex.Message);
            }
            return dtOut;
        }
    }
}
