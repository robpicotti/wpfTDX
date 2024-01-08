using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;

namespace TDX
{
    class NAV
    {
        public SqlConnection gbl_conn;
        TDX.db _db = new TDX.db();
        public string subaccount { get; set; }
        public double equities_liquidiation { get; set; }
        public double futures_liquidation { get; set; }
        public double cash { get; set; }
        public double cash_legs { get; set; }
        public double deposits { get; set; }
        public double payments { get; set; }
        public double comm_fee_adjustments { get; set; }
        public double commission { get; set; }
        public double net_liquidation_value { get; set; }
        public double withdrawals { get; set; }
        public double receivables { get; set; }
        public DataTable all_data { get; set; }
        private DataTable get_nav(string subaccount,string where = "")
        {
            DataTable dt = new DataTable();
            Table tbl = new Table("nav", gbl_conn).select_latest(where);
            dt = tbl.table_data;
            return dt;
        }
        public NAV (string subaccount,SqlConnection conn,string where="")
        {
            this.gbl_conn = conn;
            DataTable dt = get_nav(subaccount,where);
            this.all_data = dt;
            double equity_liquid;
            double futures_liquid;
            double csh;
            double csh_leg;
            double depo;
            double paym;
            double comm_fee_adj;
            double comm;
            //double NLV;
            double withdrawals;
            double receivables;
            

            if(dt.Rows.Count>0)
            {
                Double.TryParse(dt.Rows[0]["equities_liquidation"].ToString(), out equity_liquid);
                Double.TryParse(dt.Rows[0]["futures_liquidation"].ToString(), out futures_liquid);
                Double.TryParse(dt.Rows[0]["cash"].ToString(), out csh);
                Double.TryParse(dt.Rows[0]["cash_legs"].ToString(), out csh_leg);
                Double.TryParse(dt.Rows[0]["deposits"].ToString(), out depo);
                Double.TryParse(dt.Rows[0]["payments"].ToString(), out paym);
                Double.TryParse(dt.Rows[0]["comm_fee_adj"].ToString(), out comm_fee_adj);
                Double.TryParse(dt.Rows[0]["commission"].ToString(), out comm);
                //Double.TryParse(dt.Rows[0]["transaction_fees"].ToString(), out comm);
                //Double.TryParse(dt.Rows[0]["net_liquidation_value"].ToString(), out NLV);
                Double.TryParse(dt.Rows[0]["withdrawals"].ToString(), out withdrawals);
                Double.TryParse(dt.Rows[0]["receivables"].ToString(), out receivables);
                this.equities_liquidiation = Math.Round( equity_liquid,2);
                this.futures_liquidation = Math.Round(futures_liquid, 2);
                this.cash = Math.Round(csh, 2);
                this.cash_legs = Math.Round(csh_leg, 2);
                this.deposits = Math.Round(depo, 2);
                this.payments = Math.Round(paym, 2);
                this.comm_fee_adjustments = Math.Round(comm_fee_adj, 2);
                this.commission = Math.Round(comm, 2);
                //this.net_liquidation_value = Math.Round(NLV, 2);
                this.withdrawals = Math.Round(withdrawals, 2);
                this.receivables = Math.Round(receivables, 2);
            }

        }
        /// <summary>
        /// updates the NAV table with a deposit from a specific date
        /// </summary>
        /// <param name="account"></param>
        /// <param name="subaccount"></param>
        /// <param name="from_date"></param>
        /// <param name="amount"></param>
        public static void add_deposit(string subaccount,string from_date,double amount,SqlConnection conn)
        {
            string where = "WHERE navDate >='" + from_date + "' AND subaccountname='" + subaccount + "'";
            Table tabl = new Table("nav", conn).select_latest(where);
            string now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            foreach (DataRow row in tabl.table_data.Rows)
            {
                object depositValue = row["deposits"];
                double deposit = (depositValue != DBNull.Value) ? Convert.ToDouble(depositValue) : 0.0;
                row["runtime"] = now;
                row["deposits"] = deposit + amount;
                row["net_liquidation_value"] = Convert.ToDouble(row["net_liquidation_value"]) + amount;
            }
            tabl.load_dataTable(tabl.table_data);
        }
        public static void add_withdrawal(string subaccount,string from_date,double amount,SqlConnection conn)
        {
            string where = "WHERE navDate >='" + from_date + "' AND subaccountname='" + subaccount + "'";
            Table tabl = new Table("nav", conn).select_latest(where);
            string now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            foreach (DataRow row in tabl.table_data.Rows)
            {
                object withdrawalValue = row["withdrawals"];
                double withdrawals = (withdrawalValue != DBNull.Value) ? Convert.ToDouble(withdrawalValue) : 0.0;
                row["runtime"] = now;
                row["withdrawals"] = withdrawals - amount;
                row["net_liquidation_value"] = Convert.ToDouble(row["net_liquidation_value"]) - amount;
            }
            tabl.load_dataTable(tabl.table_data);
        }
        public static void add_receivable(string subaccount,string from_date,double amount,SqlConnection conn)
        {
            string where = "WHERE navDate >='" + from_date + "' AND subaccountname='" + subaccount + "'";
            Table tabl = new Table("nav", conn).select_latest(where);
            string now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            foreach (DataRow row in tabl.table_data.Rows)
            {
                object receivablesValue = row["receivables"];
                double receivables = (receivablesValue != DBNull.Value) ? Convert.ToDouble(receivablesValue) : 0.0;
                row["receivables"] = receivables + amount;
                row["runtime"] = now;
                row["receivables"] = receivables + amount;
                row["net_liquidation_value"] = Convert.ToDouble(row["net_liquidation_value"]) + amount;
            }
            tabl.load_dataTable(tabl.table_data);
        }
        /// <summary>
        /// updates NAV table with a payment from a specific date
        /// </summary>
        /// <param name="subaccount"></param>
        /// <param name="from_date"></param>
        /// <param name="amount"></param>
        /// <param name="conn"></param>
        public static void add_payment(string subaccount, string from_date, double amount, SqlConnection conn)
        {
            string where = "WHERE navDate >='" + from_date + "' AND subaccountname='" + subaccount + "'";
            Table tabl = new Table("nav", conn).select_latest(where);
            string now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            foreach (DataRow row in tabl.table_data.Rows)
            {
                object paymentsValue = row["payments"];
                double payments = (paymentsValue != DBNull.Value) ? Convert.ToDouble(paymentsValue) : 0.0;
                row["runtime"] = now;
                row["payments"] = payments - amount;
                row["net_liquidation_value"] = Convert.ToDouble(row["net_liquidation_value"]) - amount;
            }
            tabl.load_dataTable(tabl.table_data);
        }
        /// <summary>
        /// updates NAV table with a comm fee adjustment from a specific date
        /// </summary>
        /// <param name="subaccount"></param>
        /// <param name="from_date"></param>
        /// <param name="amount"></param>
        /// <param name="conn"></param>
        public static void add_comm_fee_adj(string subaccount,string from_date,double amount,SqlConnection conn)
        {
            TDX.db _db = new TDX.db();
            _db.nav_add_comm_fee_adj(subaccount, from_date, amount, conn);
        }
        /// <summary>
        /// updates NAV table with an expense/s from a specific date
        /// </summary>
        /// <param name="subaccount"></param>
        /// <param name="from_date"></param>
        /// <param name="amount"></param>
        /// <param name="conn"></param>
        public static void  add_expenses(string subaccount,string from_date,double amount,SqlConnection conn)
        {
            TDX.db _db = new TDX.db();
            _db.nav_add_expense(subaccount, from_date, amount, conn);
        }
    }
}
