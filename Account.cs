using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;

namespace TDX
{
    
    class Account
    {
        public string account_name { get; set; }
        public List<Subaccount> subaccounts_list { get; set; }
        public string account_groupname { get; set; }
        public float notional { get; set; }
        public float var_target { get; set; }
        public float allocation_target { get; set; }
        public float allocation_limit { get; set; }
        public string benchmark_name { get; set; }
        public string rebalance { get; set; }
        public int min_lots { get; set; }
        public float weight_limit { get; set; }

        public  TDX.db _db = new TDX.db();

        public SqlConnection gbl_conn;

        public Account(string accountname,SqlConnection conn)
        {
            gbl_conn = conn;
            account_name = accountname;

            this.subaccounts_list = Subaccount.GetSubaccounts(account_name,gbl_conn);
        }
        public Account()
        {
        
        }
        public static DataTable get_accounts(SqlConnection conn)
        {
            TDX.db _db = new TDX.db();
            DataTable dtout = _db.get_funds(conn);
            return dtout;
        }

    }
    class Subaccount
    {
        public string subaccount_name { get; set; }
        public string subaccount_type { get; set; }
        public string account_name { get; set; }
        public string broker_code { get; set; }
        public string broker_code_exec { get; set; }

        TDX.db _db = new TDX.db();
        SqlConnection gbl_conn;
        public Subaccount(string account_name)
        {
            this.account_name = account_name;
        }
        /// <summary>
        /// still calling it account class but its actually Fund
        /// </summary>
        /// <param name="fund"></param>
        public Subaccount(Account fund,string subaccountname,SqlConnection conn)
        {
            string sql_text = "SELECT * FROM subaccounts WHERE subaccountname='" + subaccountname + "' AND fundname='" + fund.account_name + "'";
            DataTable dt = _db.execSQL(sql_text, conn);
            if(dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                this.subaccount_name = row["subaccountname"].ToString();
                this.subaccount_type = row["subaccount_type"].ToString();
                this.broker_code = row["broker_code"].ToString();
                this.broker_code_exec = row["broker_code_exec"].ToString();
            }
        }

        /// <summary>
        /// returns a list of subaccount objects based on accountname
        /// </summary>
        /// <param name="account_name"></param>
        /// <returns></returns>
        public static List<Subaccount> GetSubaccounts(string account_name,SqlConnection conn)
        {
            List<Subaccount> out_list = new List<Subaccount>();
            TDX.db _db = new TDX.db();
            DataTable dtsubaccounts = _db.get_subaccounts(account_name, conn);
            foreach(DataRow row in dtsubaccounts.Rows)
            {
                Subaccount sub = new Subaccount(account_name);
                sub.subaccount_name = row["subaccountname"].ToString();
                sub.subaccount_type = row["subaccount_type"].ToString();
                sub.account_name = row["fundname"].ToString();
                sub.broker_code = row["broker_code"].ToString();
                sub.broker_code_exec = row["broker_code_exec"].ToString();
                out_list.Add(sub);
            }
            return out_list;
        }
        public static DataTable getSubaccountsAll(SqlConnection conn)
        {
            DataTable dt = new DataTable();
            TDX.db _db = new TDX.db();
            string sql_text = "SELECT * FROM subaccounts ORDER BY subaccountname";
            dt = _db.execSQL(sql_text,conn);
            return dt;
        }
    }


}
