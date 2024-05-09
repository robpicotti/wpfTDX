using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;


namespace TDX
{
    class Slippage
    {
        /// <summary>
        /// global sql server connection passed in
        /// </summary>
        public SqlConnection gbl_conn;
        /// <summary>
        /// database class
        /// </summary>
        TDX.db _db = new TDX.db();
        /// <summary>
        /// subaccount for the slippage
        /// </summary>
        public string subaccount { get; set; }
        
        /// <summary>
        /// raw slippage data for subaccount and date
        /// </summary>
        public DataSet dsSlippage { get; set; }
        /// <summary>
        /// start date range for the slippage data
        /// </summary>
        public DateTime fromDate { get; set; }
        /// <summary>
        /// end date range for the slippage data
        /// </summary>
        public DateTime toDate { get; set; }
        /// <summary>
        /// average slippage (absolute)
        /// </summary>
        public double avg_slippage { get; set; }
        /// <summary>
        /// average slippage (divided by price)percent for subaccount and date range
        /// </summary>
        public double avg_slippage_price_percent { get; set; }
        /// <summary>
        /// max slippage percent for subaccount and date range
        /// </summary>
        public double max_slippage_percent { get; set; }
        /// <summary>
        /// min slippage percent for subaccount and date range
        /// </summary>
        public double min_slippage_percent { get; set; }
        /// <summary>
        /// total pnl derived from the slippage information. based off of the signal price vs the execution price
        /// </summary>
        public double pnl_slippage { get; set; }
        /// <summary>
        /// the profit or loss from the slippage derived from a BUY action
        /// </summary>
        public double sumOfBuysSlippagePnl { get; set; }
        /// <summary>
        /// the profit or loss from the slippage derived from a SELL or SELLSHORT action
        /// </summary>
        public double sumOfSellsSlippagePnl { get; set; }
        /// <summary>
        /// datatable containing all the slippage analysis metrics
        /// </summary>
        public DataTable dtSlippageAnalysis { get; set; }
        /// <summary>
        /// slippage pnl based on the first 5 minutes of trading
        /// </summary>
        public double pnl_slippage_open { get; set; }
        /// <summary>
        /// slippage pnl based on the last 15 minutes of trading
        /// </summary>
        public double pnl_slippage_close { get; set; }
        /// <summary>
        /// the slippage pnl in the first hour of trading
        /// </summary>
        public double pnl_slippage_9 { get; set; }
        /// <summary>
        /// the slippage pnl in the last hour of trading
        /// </summary>
        public double pnl_slippage_15 { get; set; }

        /// <summary>
        /// slippage pnl in the morning
        /// </summary>
        public double pnl_slippage_morning { get; set; }
        /// <summary>
        /// slippage pnl in the afternoon
        /// </summary>
        public double pnl_slippage_afternoon { get; set; }
        /// <summary>
        /// list of hours that define "morning trading hours"
        /// </summary>
        public List<int> morning_hours { get; set; }
        /// <summary>
        /// list of hours that define "afternoon trading hours"
        /// </summary>
        public List<int> afternoon_hours { get; set; }

        /// <summary>
        /// set up slippage object
        /// </summary>
        /// <param name="posn_date"></param>
        /// <param name="subaccount"></param>
        public Slippage(string from_date,string posn_date,string accountname,SqlConnection conn)
        {
            gbl_conn = conn;
            get_slippage(from_date,posn_date,accountname);
            
        }
        /// <summary>
        /// get the slippage report
        /// </summary>
        /// <param name="from_date">from date range</param>
        /// <param name="to_date"> to date range</param>
        /// <param name="subaccount"></param>
        public void get_slippage(string from_date,string to_date,string accountname)
        {
            dsSlippage = new DataSet();
            dsSlippage = _db.get_slippage(from_date, to_date, accountname, gbl_conn);
            slippage_analysis(dsSlippage);
        }
        /// <summary>
        /// makes a list of the hours for specified window. the morning and afternoon include the first and last hours respectively
        /// </summary>
        /// <param name="window"></param>
        /// <returns></returns>
        private List<int> make_hours(string window)
        {
            List<int> hours = new List<int>();
            switch(window)
            {
                case "first":
                    hours = new List<int>() { 9 };
                    break;
                case "last":
                    hours = new List<int>() { 15 };
                    break;
                case "morning":
                    hours = new List<int>() { 9,10,11,12 };
                    break;
                case "afternoon":
                    hours = new List<int>() { 13, 14, 15 };
                    break;
            }
            return hours;
        }
        private void slippage_analysis(DataSet ds)
        {
            if (ds.Tables.Count > 0)
            {
                if (ds.Tables[0].Rows.Count > 0)
                {
                   
                    //util.dataTableToCsv(ds.Tables[0], @"c:\TDX\App\Log\avg_calc.csv"); //for debug
                    //avg_slippage = (double)ds.Tables[0].Compute("AVG(slippage)", "") ;
                    avg_slippage_price_percent = (double)ds.Tables[0].Compute("AVG(slippage_price)", "")*100;
                    max_slippage_percent = (double)ds.Tables[0].Compute("MAX(slippage_price)", "") * 100;
                    min_slippage_percent = (double)ds.Tables[0].Compute("MIN(slippage_price)", "") * 100;
                    pnl_slippage = (double)ds.Tables[0].Compute("SUM(slippage_pnl)", "");
                    var sumOfBuysSlipPnl = ds.Tables[0].AsEnumerable().Where(x => x.Field<string>("action") == "BUY")
                        .Sum(x => x.Field<double>("slippage_pnl"))
                        .ToString();
                    double dblBuyPnl;
                    var sumOfSellsSlipPnl = ds.Tables[0].AsEnumerable().Where(x => x.Field<string>("action") == "SELL" || x.Field<string>("action").ToUpper() == "SELLSHORT")
                        .Sum(x => x.Field<double>("slippage_pnl"))
                        .ToString();
                    double dblSellPnl;
                    ds.Tables[0].Columns.Add("execution_time_EST");
                    ds.Tables[0].Columns.Add("time");
                    ds.Tables[0].Columns.Add("HH");
                    TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                    foreach (DataRow src in ds.Tables[0].Rows)
                    {
                        DateTime utcDateTime = (DateTime)src["execution_time"];
                        DateTime estDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, easternZone);
                        src["execution_time_EST"] = estDateTime;
                        src["time"] = estDateTime.TimeOfDay;
                        src["HH"] = Convert.ToInt32(estDateTime.Hour);
                        src["slippage_pnl"] = (Convert.ToDouble(src["slippage_pnl"]));
                    }
                    //first 5 minutes of trading (open)
                    DataRow[] filteredRows = ds.Tables[0].Select("time >= #09:30# AND time < #09:36#");
                    if (filteredRows.Length > 0)
                    {
                        pnl_slippage_open = Convert.ToDouble(filteredRows.CopyToDataTable().Compute("SUM(slippage_pnl)", ""));
                    }
                    //last 15 minutes of trading (close)
                    filteredRows = ds.Tables[0].Select("time >= #15:45# AND time < #16:00#");
                    if (filteredRows.Length > 0)
                    {
                        pnl_slippage_close = Convert.ToDouble(filteredRows.CopyToDataTable().Compute("SUM(slippage_pnl)", ""));
                    }

                    Double.TryParse(sumOfBuysSlipPnl, out dblBuyPnl);
                    Double.TryParse(sumOfSellsSlipPnl, out dblSellPnl);
                    sumOfBuysSlippagePnl = dblBuyPnl;
                    sumOfSellsSlippagePnl = dblSellPnl;
                    dtSlippageAnalysis = new DataTable();
                    dtSlippageAnalysis.Clear();
                    dtSlippageAnalysis.Columns.Add("AverageSlippagePricePercent");
                    dtSlippageAnalysis.Columns.Add("Max Slippage %");
                    dtSlippageAnalysis.Columns.Add("Min Slippage %");
                    dtSlippageAnalysis.Columns.Add("Slippage PNL");
                    dtSlippageAnalysis.Columns.Add("Slippage PNL Open");
                    dtSlippageAnalysis.Columns.Add("Slippage PNL Close");
                    dtSlippageAnalysis.Columns.Add("Slippage PNL BUYS");
                    dtSlippageAnalysis.Columns.Add("Slippage PNL SELLS");

                    DataRow drow = dtSlippageAnalysis.NewRow();
                    drow["AverageSlippagePricePercent"] = Math.Round(avg_slippage_price_percent, 3);
                    drow["Max Slippage %"] = Math.Round(max_slippage_percent, 3);
                    drow["Min Slippage %"] = Math.Round(min_slippage_percent, 3);
                    drow["Slippage PNL"] = Math.Round(pnl_slippage, 0);
                    drow["Slippage PNL BUYS"] = Math.Round(dblBuyPnl);
                    drow["Slippage PNL SELLS"] = Math.Round(dblSellPnl);
                    drow["Slippage PNL Open"] = Math.Round(pnl_slippage_open);
                    drow["Slippage PNL Close"] = Math.Round(pnl_slippage_15);
                    dtSlippageAnalysis.Rows.Add(drow);
                }
            }
        }
    }
}
