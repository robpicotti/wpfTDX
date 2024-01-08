using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;

namespace TDX
{
    class Broker
    {
        public int broker_id { get; set; }
        public string brokername { get; set; }
        public bool autolocate { get; set; }
        public string broker_code { get; set; }
        public string emsname { get; set; }
        public string topaccountname { get; set; }
        public string broker_type { get; set; }
        public List<string> subaccounts_list { get; set; }
        public TDX.db _db = new TDX.db();

        public Broker(string broker_code, SqlConnection conn)
        {
            if (broker_code!="")
            {
                DataTable dt = new DataTable();
                string sql_text = "SELECT * FROM broker WHERE broker_code='" + broker_code + "'";
                dt = _db.execSQL(sql_text, conn);
                if(dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    this.broker_id = int.Parse(row["broker_id"].ToString());
                    this.brokername = row["broker"].ToString();
                    this.autolocate = Convert.ToBoolean(row["autolocate"]);
                    this.broker_code = broker_code;
                    this.emsname = row["emsname"].ToString();
                    this.topaccountname = row["topaccountname"].ToString();
                    this.broker_type = row["broker_type"].ToString();
                }
            }

        }
        public Broker(int broker_id, SqlConnection conn)
        {
                DataTable dt = new DataTable();
                string sql_text = "SELECT * FROM broker WHERE broker_id=" + broker_id.ToString() + "";
                dt = _db.execSQL(sql_text, conn);
                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    this.broker_id = broker_id;
                    this.brokername = row["broker"].ToString();
                    this.autolocate = Convert.ToBoolean(row["autolocate"]);
                    this.broker_code = row["broker_code"].ToString();
                    this.emsname = row["emsname"].ToString();
                    this.topaccountname = row["topaccountname"].ToString();
                    this.broker_type = row["broker_type"].ToString();
                }

        }

    }
}
