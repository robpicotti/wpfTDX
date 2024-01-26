using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
namespace TDX
{
    class Position
    {
        public SqlConnection gbl_conn;
        TDX.db _db = new TDX.db();
        public string subaccount { get; set; }
        public DataTable position { get; set; }
        public DataTable cash_position { get; set; }
        public DateTime position_date { get; set; }
        
        public Position (string subaccount,SqlConnection conn)
        {
            this.subaccount = subaccount;
            this.position_date = DateTime.Today;
            this.gbl_conn = conn;
        }
        public Position positions()
        {
            DataSet ds = _db.get_positions(this.subaccount, this.position_date.ToString("yyyy-MM-dd"),gbl_conn);
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
        public void scale_position(string _account,string tickername,double scale_stepsize,double scaled_target)
        {
            string now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            string account = _account;
            DataTable dt = new DataTable();
            dt.Columns.Add("runtime");
            dt.Columns.Add("account");
            dt.Columns.Add("subaccount");
            dt.Columns.Add("tickername");
            dt.Columns.Add("scale_stepsize");
            dt.Columns.Add("scaled_percent");
            dt.Columns.Add("scaled_target");

            DataRow drow = dt.NewRow();
            drow["runtime"] = now;
            drow["account"] = account;
            drow["subaccount"] = this.subaccount;
            drow["tickername"] = tickername;
            drow["scale_stepsize"] = scale_stepsize;
            drow["scaled_percent"] = get_scaled_percent(tickername);
            drow["scaled_target"] = scaled_target;
            dt.Rows.Add(drow);
            Table tbl = new Table("scaled_positions",gbl_conn);
            tbl.load_dataTable(dt);

        }
        /// <summary>
        /// returns -1 if there is no tickername in merge_positions
        /// </summary>
        /// <param name="tickername"></param>
        /// <returns></returns>
        public double get_scaled_percent(string tickername)
        {
            double dbl_out = -1;
            string _where_clause = "WHERE tickername='" + tickername + "' AND subaccountname='" + this.subaccount + "'";
            Table tbl = new Table("merge_positions", gbl_conn, _where_clause);

            if(tbl.table_data.Rows.Count>0)
            {
                Double.TryParse(tbl.table_data.Rows[0]["scaled_percent"].ToString(),out dbl_out);
            }
            return dbl_out;
        }

    }
}
