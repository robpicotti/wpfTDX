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
    public enum LimitType
    {
        Weight,
        StockNotionalPct,
        FuturesNotionalPct,
        Liquidity,
        StockLeverage,
        FuturesLeverage,
        Leverage,
        VarLimitFactor,
        StressLimitFactor,
        Drawdown
    }

    public class Limits
    {
       public SqlConnection gbl_conn { get; set; }
       public FundLimits fundLimits { get; set; }
       public List<string> lstFundLimits = new List<string>();

        public Limits(string fundName, SqlConnection conn)
        {
            this.gbl_conn = conn;
            this.fundLimits = new FundLimits(fundName,this.gbl_conn);
            this.lstFundLimits = GetFundLimits();
        }
        /// <summary>
        /// these are all the limits that we configure on the fund level
        /// </summary>
        /// <returns></returns>
        private List<string> GetFundLimits()
        {
            lstFundLimits.Add("weight_limit");
            lstFundLimits.Add("stk_notional_pct_limit");
            lstFundLimits.Add("fut_notional_pct_limit");
            lstFundLimits.Add("liquidity_limit");
            lstFundLimits.Add("stk_leverage_limit");
            lstFundLimits.Add("fut_leverage_limit");
            lstFundLimits.Add("leverage_limit");
            lstFundLimits.Add("var_limit_factor");
            lstFundLimits.Add("stress_limit_factor");
            lstFundLimits.Add("drawdown_limit");
            return lstFundLimits;
        }

    }

    class WeightLimits
    {

    }
    class NotionalLimits
    {

    }
    public class FundLimits
    {
        private string fundname { get; set; }
        public StockNotionalPctLimit stk_notional_pct_limit { get; set; }
        public FuturesNotionalPctLimit fut_notional_pct_limit { get; set; }
        public StockLeverageLimit stk_leverage_limit { get; set; }
        public FuturesLeverageLimit fut_leverage_limit { get; set; }
        public LeverageLimit leverage_limit { get; set; }
        public VarLimitFactor var_limit_factor { get; set; }
        public StressLimitFactor stress_limit_factor { get; set; }
        public DrawDownLimit drawdown_limit { get; set; }
        public LiquidityLimit liquidity_limit { get; set; }
        public WeightLimit weight_limit { get; set; }
        private Table tblFunds { get; set; }
        private Table tblFundLimits { get; set; }
        private Table tblTargetPositions { get; set; }
        private SqlConnection gbl_conn { get; set; }
        private DataTable dtFundData { get; set; }
        private DataTable dtFundLimits { get; set; }
        private DataTable dtLive { get; set; }
        private db DB = new db();
        public FundLimits(string fundname, SqlConnection conn)
        {
            try
            {
                this.fundname = fundname;
                this.gbl_conn = conn;
                this.stk_notional_pct_limit = new StockNotionalPctLimit();
                this.fut_notional_pct_limit = new FuturesNotionalPctLimit();
                this.stk_leverage_limit = new StockLeverageLimit();
                this.fut_leverage_limit = new FuturesLeverageLimit();
                this.leverage_limit = new LeverageLimit();
                this.var_limit_factor = new VarLimitFactor();
                this.stress_limit_factor = new StressLimitFactor();
                this.drawdown_limit = new DrawDownLimit();
                this.liquidity_limit = new LiquidityLimit();
                this.weight_limit = new WeightLimit();
                //get the default data from the funds table
                tblFunds = new Table("funds", this.gbl_conn, "WHERE fundname='" + this.fundname + "'");
                dtFundData = new DataTable();
                dtFundData = tblFunds.table_data;
                // get the custom data from the funds_limit table
                tblFundLimits = new Table("fund_limits", this.gbl_conn, "WHERE fundname='" + fundname + "'");
                dtFundLimits = tblFundLimits.table_data;
                //get the live limits data from the postions_target table
                tblTargetPositions = new Table("target_positions", this.gbl_conn, "WHERE fundname='" + fundname + "'");
                dtLive = tblTargetPositions.table_data;

                if (dtFundLimits.Rows.Count > 0)
                {
                    bool enabled = dtFundLimits.Rows[0]["enabled"] != null ? Convert.ToBoolean(dtFundLimits.Rows[0]["enabled"]) : false;

                    if (enabled)
                    {
                        string strValue = dtFundLimits.Rows[0]["stk_leverage_limit"].ToString();
                        double parsedOut;
                        if (Double.TryParse(strValue, out parsedOut))
                        {
                            this.stk_leverage_limit.customValue = parsedOut;
                        }
                        strValue = dtFundLimits.Rows[0]["weight_limit"].ToString();
                        if (Double.TryParse(strValue, out parsedOut))
                        {
                            this.weight_limit.customValue = parsedOut;
                        }
                        strValue = dtFundLimits.Rows[0]["stk_notional_pct_limit"].ToString();
                        if (Double.TryParse(strValue, out parsedOut))
                        {
                            this.stk_notional_pct_limit.customValue = parsedOut;
                        }
                        strValue = dtFundLimits.Rows[0]["fut_notional_pct_limit"].ToString();
                        if (Double.TryParse(strValue, out parsedOut))
                        {
                            this.fut_notional_pct_limit.customValue = parsedOut;
                        }
                        strValue = dtFundLimits.Rows[0]["liquidity_limit"].ToString();
                        if (Double.TryParse(strValue, out parsedOut))
                        {
                            this.liquidity_limit.customValue = parsedOut;
                        }
                        strValue = dtFundLimits.Rows[0]["stk_leverage_limit"].ToString();
                        if (Double.TryParse(strValue, out parsedOut))
                        {
                            this.stk_leverage_limit.customValue = parsedOut;
                        }
                        strValue = dtFundLimits.Rows[0]["fut_leverage_limit"].ToString();
                        if (Double.TryParse(strValue, out parsedOut))
                        {
                            this.fut_leverage_limit.customValue = parsedOut;
                        }
                        strValue = dtFundLimits.Rows[0]["leverage_limit"].ToString();
                        if (Double.TryParse(strValue, out parsedOut))
                        {
                            this.leverage_limit.customValue = parsedOut;
                        }
                        strValue = dtFundLimits.Rows[0]["var_limit_factor"].ToString();
                        if (Double.TryParse(strValue, out parsedOut))
                        {
                            this.var_limit_factor.customValue = parsedOut;
                        }
                        strValue = dtFundLimits.Rows[0]["stress_limit_factor"].ToString();
                        if (Double.TryParse(strValue, out parsedOut))
                        {
                            this.stress_limit_factor.customValue = parsedOut;
                        }
                        strValue = dtFundLimits.Rows[0]["drawdown_limit"].ToString();
                        if (Double.TryParse(strValue, out parsedOut))
                        {
                            this.drawdown_limit.customValue = parsedOut;
                        }
                        strValue = dtFundLimits.Rows[0]["drawdown_limit"].ToString();
                        if (Double.TryParse(strValue, out parsedOut))
                        {
                            this.drawdown_limit.customValue = parsedOut;
                        }
                    }
                }
                if (dtFundData.Rows.Count > 0)
                {
                    string stkLeverageLimitDefault = dtFundData.Rows[0]["stk_leverage_limit"].ToString();
                    double parsedOut;
                    if (Double.TryParse(stkLeverageLimitDefault, out parsedOut))
                    {
                        this.stk_leverage_limit.defaultValue = parsedOut;
                    }
                    string futLevLimit = dtFundData.Rows[0]["fut_leverage_limit"].ToString();
                    if (Double.TryParse(futLevLimit, out parsedOut))
                    {
                        this.fut_leverage_limit.defaultValue = parsedOut;
                    }
                    string leverageLimit = dtFundData.Rows[0]["leverage_limit"].ToString();
                    if (Double.TryParse(leverageLimit, out parsedOut))
                    {
                        this.leverage_limit.defaultValue = parsedOut;
                    }
                    string varlimitfactor = dtFundData.Rows[0]["var_limit_factor"].ToString();
                    if (Double.TryParse(varlimitfactor, out parsedOut))
                    {
                        this.var_limit_factor.defaultValue = parsedOut;
                    }
                    string stressLimitFactor = dtFundData.Rows[0]["stress_limit_factor"].ToString();
                    if (Double.TryParse(stressLimitFactor, out parsedOut))
                    {
                        this.stress_limit_factor.defaultValue = parsedOut;
                    }
                    string drawDownLimit = dtFundData.Rows[0]["drawdown_limit"].ToString();
                    if (Double.TryParse(drawDownLimit, out parsedOut))
                    {
                        this.drawdown_limit.defaultValue = parsedOut;
                    }
                    string stkNotionalPctLimit = dtFundData.Rows[0]["stk_notional_pct_limit"].ToString();
                    if (Double.TryParse(stkNotionalPctLimit, out parsedOut))
                    {
                        this.stk_notional_pct_limit.defaultValue = parsedOut;
                    }
                    string futNotionalPctLimit = dtFundData.Rows[0]["fut_notional_pct_limit"].ToString();
                    if (Double.TryParse(futNotionalPctLimit, out parsedOut))
                    {
                        this.fut_notional_pct_limit.defaultValue = parsedOut;
                    }
                    string liquidityLimit = dtFundData.Rows[0]["liquidity_limit"].ToString();
                    if (Double.TryParse(liquidityLimit, out parsedOut))
                    {
                        this.liquidity_limit.defaultValue = parsedOut;
                    }
                    string weight_limit = dtFundData.Rows[0]["weight_limit"].ToString();
                    if (Double.TryParse(weight_limit, out parsedOut))
                    {
                        this.weight_limit.defaultValue = parsedOut;
                    }
                }
            }
            catch(Exception ex)
            {
                throw new Exception("FundLimits exception: " + ex.Message);
            }
        }

        public void UpdateFundLimits(string action)
        {
            string now = DateTime.UtcNow.ToString("dd-MMM-yyyy HH:mm:ss");
            Table tbl = new Table("fund_limits", this.gbl_conn, select_data: false);
            List<string> fund_limits_columns = tbl.get_table_columns();
            string insertSQL = "INSERT fund_limits(";
            for (int i = 0; i < fund_limits_columns.Count; i++)
            {
                insertSQL += fund_limits_columns[i].ToString() + ",";
            }
            insertSQL = insertSQL.Substring(0, insertSQL.Length - 1);
            insertSQL += ") VALUES('" + now + "','" + this.fundname + "',";
            insertSQL += (this.weight_limit.customValue != null ? this.weight_limit.customValue.ToString() : "NULL") + ",";
            insertSQL += (this.stk_notional_pct_limit.customValue != null ? this.stk_notional_pct_limit.customValue.ToString(): "NULL") + ",";
            insertSQL += (this.fut_notional_pct_limit.customValue != null ? this.fut_notional_pct_limit.customValue.ToString(): "NULL" )+ ",";
            insertSQL += (this.liquidity_limit.customValue != null ? this.liquidity_limit.customValue.ToString(): "NULL" ) + ",";
            insertSQL += (this.stk_leverage_limit.customValue != null ? this.stk_leverage_limit.customValue.ToString(): "NULL") + ",";
            insertSQL += (this.fut_leverage_limit.customValue != null ? this.fut_leverage_limit.customValue.ToString(): "NULL" ) + ",";
            insertSQL += (this.leverage_limit.customValue !=null ? this.leverage_limit.customValue.ToString() :"NULL") + ",";
            insertSQL += (this.var_limit_factor.customValue != null ? this.var_limit_factor.customValue.ToString() : "NULL") + ",";
            insertSQL += (this.stress_limit_factor.customValue != null ? this.stress_limit_factor.customValue.ToString() :"NULL") + ",";
            insertSQL += (this.drawdown_limit.customValue != null ? this.drawdown_limit.customValue.ToString(): "NULL") + ",";
            insertSQL += "1,'"; //set enabled to true
            insertSQL += action + "')";
            //insert data to fund_limits table
            DB.execSQL_noresults(insertSQL, this.gbl_conn);
        }

    }



    public  class StockLeverageLimit
    {
        public double? defaultValue { get; set; }
        public double? customValue { get; set; }
        public string action { get; set; }
        public double? liveValue { get; set; }
    }
    public class FuturesLeverageLimit
    {
        public double? defaultValue { get; set; }
        public double? customValue { get; set; }
        public string action { get; set; }
        public double? liveValue { get; set; }
    }
    public class LeverageLimit
    {
        public double? defaultValue { get; set; }
        public double? customValue { get; set; }
        public string action { get; set; }
        public double? liveValue { get; set; }
    }
    public class VarLimitFactor
    {
        public double? defaultValue { get; set; }
        public double? customValue { get; set; }
        public string action { get; set; }
        public double? liveValue { get; set; }
    }
    public class StressLimitFactor
    {
        public double? defaultValue { get; set; }
        public double? customValue { get; set; }
        public string action { get; set; }
        public double? liveValue { get; set; }
    }
    public class DrawDownLimit
    {
        public double? defaultValue { get; set; }
        public double? customValue { get; set; }
        public string action { get; set; }
        public double? liveValue { get; set; }
    }
    public class StockNotionalPctLimit
    {
        public double? defaultValue { get; set; }
        public double? customValue { get; set; }
        public string action { get; set; }
        public double? liveValue { get; set; }
    }
    public class FuturesNotionalPctLimit
    {
        public double? defaultValue { get; set; }
        public double? customValue { get; set; }
        public string action { get; set; }
        public double? liveValue { get; set; }
    }
    public class LiquidityLimit
    {
        public double? defaultValue { get; set; }
        public double? customValue { get; set; }
        public string action { get; set; }
        public double? liveValue { get; set; }
    }
    public class WeightLimit
    {
        public double? defaultValue { get; set; }
        public double? customValue { get; set; }
        public string action { get; set; }
        public double? liveValue { get; set; }
    }
}
