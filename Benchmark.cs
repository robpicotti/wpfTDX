using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;


namespace TDX
{
    class Benchmark
    {
        private TDX.db _db = new TDX.db();
        public Benchmark(string benchmarkname, SqlConnection conn)
        {
            this.conn = conn;
            this.table_name = "benchmarks";
            Table tbl = new Table(this.table_name, this.conn);
            this.benchmark_columns = tbl.get_table_columns();
            this.benchmarkname = benchmarkname;
        }

        public static List<string> get(SqlConnection conn)
        {
            List<string> out_list = new List<string>();
            Table tbl = new Table("benchmarks", conn).select_latest();
            out_list = tbl.table_data.AsEnumerable().Select(x => x["benchmarkname"].ToString()).ToList();

            return out_list;
        }
        /// <summary>
        /// adds a row to the benchmarks table. assumes all necessary attributes have been set
        /// </summary>
        public void Add()
        {
            DataTable dtBench = new DataTable();

            string sql_insert = "INSERT INTO " + this.table_name;
            string sql_columns = "(";
            foreach (string colname in this.benchmark_columns)
            {
                if(colname !="sort_key")
                {
                    sql_columns += sql_columns + ",";
                }
            }
            sql_columns = sql_columns.Substring(0, sql_columns.Length - 1) + ")";

            string quotes = "'";
            string sql_values = quotes +  DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss") + quotes + "," + quotes + this.benchmarkname + quotes ;
            sql_values += "," + quotes + this.longname + quotes + "," + quotes + this.sub_portfolios + quotes + "," + quotes + this.strategyname + quotes + ",";

            //row["strategyname_base"] = this.strategyname_base;
            //row["var_target"] = this.var_target;
            //row["var_scale"] = this.var_scale;
            //row["portfolio_wt"] = this.portfolio_wt;
            //row["wt_scale"] = this.wt_scale;
            //row["level"] = this.level;
            //row["allocation_level"] = this.allocation_level;
            //row["allocation_wt"] = this.allocation_wt;
            //row["notional"] = this.notional;
            //row["min_lots"] = this.min_lots;
            //row["parent_portfolio"] = this.parent_portfolio;
            //row["keep_updated"] = this.keep_updated;

        }
        public SqlConnection conn
        {
            get; set;
        }
        public List<string> benchmark_columns
        {
            get;set;
        }
        public string table_name { get; set; }
        public string benchmarkname { get; set; }
        public string longname { get; set; }
        public string sub_portfolios { get; set; }
        public string strategyname { get; set; }
        public string strategyname_base { get; set; }
        public float var_target { get; set; }
        public int var_scale { get; set; }
        public string portfolio_wt { get; set; }
        public int wt_scale { get; set; }
        public string level { get; set; }
        public string allocation_level { get; set; }
        public float allocation_wt { get; set; }
        public int notional { get; set; }
        public int min_lots { get; set; }
        public string parent_portfolio { get; set; }
        public int keep_updated { get; set; }
        public int calc_attribution { get; set; }
        public int custom { get; set; }
        public int bangbang { get; set; }
        public int daily_only { get; set; }
        public int hedge { get; set; }

        public List<string> hedges { get; set; }
        public float mgmt_fee { get; set; }
        public float incentive_fee { get; set; }
        public string fee_type { get; set; }

    }
}
