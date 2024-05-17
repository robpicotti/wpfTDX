using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using TDX;

namespace wpfTDX
{
    public class Fund
    {
        public string fundName { get; set; }
        public string fundGroupName { get; set; }
        public string owner { get; set; }
        public double? notional { get; set; }
        public double? var_target { get; set; }
        public double? allocation_target { get; set; }
        public double? allocation_limit { get; set; }
        public string benchmarkName { get; set; }
        public string rebalance { get; set; }
        public bool hedge { get; set; }
        public int? min_lots { get; set; }
        public string base_currency { get; set; }
        public bool new_fund { get; set; }
        public double? mgmt_fee { get; set; }
        public double? incentive_fee { get; set; }
        public string fee_type { get; set; }
        public string account_type { get; set; }
        public bool plot_fund { get; set; }
        public bool send_orders { get; set; }
        public bool send_email { get; set; }
        public string send_endpoint { get; set; }
        public bool send_aws { get; set; }
        public bool send_levels { get; set; }
        public string email_recipients { get; set; }
        public bool closed { get; set; }
        public Table tblFund { get; set; }
        public DataTable dtFund { get; set; }
        private SqlConnection gbl_conn { get; set; }
        private db DB = new db();
        public Fund(string fundName, SqlConnection conn)
        {
            this.fundName = fundName;
            this.gbl_conn = conn;
            tblFund = new Table("funds", this.gbl_conn, "WHERE fundname='" + this.fundName + "'");
            dtFund = tblFund.table_data;
            PopulateFundAttributes();
        }
        
        private void PopulateFundAttributes()
        {
            try
            {
                for(int i = 0; i < dtFund.Rows.Count;i++)
                {
                    this.account_type = dtFund.Rows[i]["account_type"].ToString();
                    string alloc_limit = dtFund.Rows[i]["allocation_limit"].ToString();
                    double parsedOut;
                    bool blnParsedOut;
                    int iParsedOut;
                    if(Double.TryParse(alloc_limit, out parsedOut))
                    {
                        this.allocation_limit = parsedOut;
                    }
                    if (Double.TryParse(dtFund.Rows[i]["allocation_target"].ToString(),out parsedOut))
                    {
                        this.allocation_target = parsedOut;
                    }
                    this.base_currency = dtFund.Rows[i]["base_currency"].ToString();
                    this.benchmarkName = dtFund.Rows[i]["benchmarkname"].ToString();

                    if(bool.TryParse(dtFund.Rows[i]["closed"].ToString(), out blnParsedOut))
                    {
                        this.closed = blnParsedOut;
                    }
                    this.email_recipients = dtFund.Rows[i]["email_recipients"].ToString();
                    this.fee_type = dtFund.Rows[i]["fee_type"].ToString();
                    this.fundGroupName = dtFund.Rows[i]["fundgroupname"].ToString();
                    if(bool.TryParse(dtFund.Rows[i]["hedge"].ToString(),out blnParsedOut))
                    {
                        this.hedge = blnParsedOut;
                    }
                    if(Double.TryParse(dtFund.Rows[i]["incentive_fee"].ToString(),out parsedOut))
                    {
                        this.incentive_fee = parsedOut;
                    }
                    if(Double.TryParse(dtFund.Rows[i]["mgmt_fee"].ToString(),out parsedOut))
                    {
                        this.mgmt_fee = parsedOut;
                    }
                    if(int.TryParse(dtFund.Rows[i]["min_lots"].ToString(),out iParsedOut))
                    {
                        this.min_lots = iParsedOut;
                    }
                    if (bool.TryParse(dtFund.Rows[i]["new_fund"].ToString(), out blnParsedOut))
                    {
                        this.new_fund = blnParsedOut;
                    }
                    if (Double.TryParse(dtFund.Rows[i]["notional"].ToString(), out parsedOut))
                    {
                        this.notional = parsedOut;
                    }
                    this.owner = dtFund.Rows[i]["owner"].ToString();
                    if (bool.TryParse(dtFund.Rows[i]["plot_fund"].ToString(), out blnParsedOut))
                    {
                        this.plot_fund = blnParsedOut;
                    }
                    this.rebalance = dtFund.Rows[i]["rebalance"].ToString();
                    if (bool.TryParse(dtFund.Rows[i]["send_aws"].ToString(), out blnParsedOut))
                    {
                        this.send_aws = blnParsedOut;
                    }
                    if (bool.TryParse(dtFund.Rows[i]["send_email"].ToString(), out blnParsedOut))
                    {
                        this.send_email = blnParsedOut;
                    }
                    this.send_endpoint = dtFund.Rows[i]["send_endpoint"].ToString();
                    if (bool.TryParse(dtFund.Rows[i]["send_levels"].ToString(), out blnParsedOut))
                    {
                        this.send_levels = blnParsedOut; 
                    }
                    if (bool.TryParse(dtFund.Rows[i]["send_orders"].ToString(), out blnParsedOut))
                    {
                        this.send_orders = blnParsedOut;
                    }
                }
            }
            catch(Exception ex)
            {
                throw new Exception("PopulateFundAttributes error: " + ex.Message);
            }
        }
    }
}
