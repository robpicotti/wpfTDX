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
    public enum ActionType
    {
        Default,
        Scale,
        Hard
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
                ProcessCustomFundLimits(dtFundLimits);
                
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
        public ActionType GetActionType(string action)
        {
            switch( action)
            {
                case "Hard":
                    return ActionType.Hard;
                case "Scale":
                    return ActionType.Scale;
                default:
                    return ActionType.Default;
            }
        }
        /// <summary>
        /// sets all the sub-classes updated status to false. 
        /// this should be set after an update is submitted
        /// </summary>
        public void ResetUpdated()
        {
            this.stk_leverage_limit.updated = false;
            this.fut_leverage_limit.updated = false;
            this.leverage_limit.updated = false;
            this.var_limit_factor.updated = false;
            this.stress_limit_factor.updated = false;
            this.drawdown_limit.updated = false;
            this.stk_notional_pct_limit.updated = false;
            this.fut_notional_pct_limit.updated = false;
            this.liquidity_limit.updated = false;
            this.weight_limit.updated = false;
        }
        /// <summary>
        /// processes datatable from database of fundlimits
        /// </summary>
        /// <returns></returns>
        public void ProcessCustomFundLimits(DataTable dtIn)
        {
            if(dtIn.Rows.Count>0)
            {
                try
                {
                    for(int i=0;i < dtIn.Rows.Count;i++)
                    {
                        string metric = dtIn.Rows[i]["metric"].ToString();
                        string metricValue = dtIn.Rows[i]["value"].ToString();
                        string action = dtIn.Rows[i]["action"].ToString();
                        //bool enabled = bool.Parse(dtIn.Rows[i]["enabled"].ToString());
                        double parsedOut;
                        switch (metric)
                        {
                            case "stk_leverage_limit":
                                if (Double.TryParse(metricValue, out parsedOut))
                                {
                                    this.stk_leverage_limit.customValue = parsedOut;
                                    this.stk_leverage_limit.action = GetActionType(action);
                                }
                                break;
                            case "weight_limit":
                                if(Double.TryParse(metricValue,out parsedOut))
                                {
                                    this.weight_limit.customValue = parsedOut;
                                    this.weight_limit.action = GetActionType(action);
                                }
                                break;
                            case "stk_notional_pct_limit":
                                if(Double.TryParse(metricValue, out parsedOut))
                                {
                                    this.stk_notional_pct_limit.customValue = parsedOut;
                                    this.stk_notional_pct_limit.action = GetActionType(action);
                                }
                                break;
                            case "fut_notional_pct_limit":
                                if(Double.TryParse(metricValue, out parsedOut))
                                {
                                    this.fut_notional_pct_limit.customValue = parsedOut;
                                    this.fut_notional_pct_limit.action = GetActionType(action);
                                }
                                break;
                            case "liquidity_limit":
                                if(Double.TryParse(metricValue,out parsedOut))
                                {
                                    this.liquidity_limit.customValue = parsedOut;
                                    this.liquidity_limit.action = GetActionType(action);
                                }
                                break;
                            case "fut_leverage_limit":
                                if(Double.TryParse(metricValue,out parsedOut))
                                {
                                    this.fut_leverage_limit.customValue = parsedOut;
                                    this.fut_leverage_limit.action = GetActionType(action);
                                }
                                break;
                            case "leverage_limit":
                                if(Double.TryParse(metricValue, out parsedOut))
                                {
                                    this.leverage_limit.customValue = parsedOut;
                                    this.leverage_limit.action = GetActionType(action);
                                }
                                break;
                            case "var_limit_factor":
                                if (Double.TryParse(metricValue, out parsedOut))
                                {
                                    this.var_limit_factor.customValue = parsedOut;
                                    this.var_limit_factor.action = GetActionType(action);
                                }
                                break;
                            case "stress_limit_factor":
                                if (Double.TryParse(metricValue, out parsedOut))
                                {
                                    this.stress_limit_factor.customValue = parsedOut;
                                    this.stress_limit_factor.action = GetActionType(action);
                                }
                                break;
                            case "drawdown_limit":
                                if (Double.TryParse(metricValue, out parsedOut))
                                {
                                    this.drawdown_limit.customValue = parsedOut;
                                    this.drawdown_limit.action = GetActionType(action);
                                }
                                break;
                        }
                    }
                }
                catch(Exception ex)
                {
                    throw new Exception("ProcessFundLimitsError: " + ex.Message);
                }
            }
        }
        public void UpdateFundLimits()
        {
            string execSQL = "";
            if(this.stk_leverage_limit.updated)
            {
                execSQL = InsertFundLimitsSQLStatement("stk_leverage_limit",
                    this.stk_leverage_limit.customValue,
                    this.stk_leverage_limit.action);
                DB.execSQL_noresults(execSQL, this.gbl_conn);
            }
            if (this.weight_limit.updated)
            {
                execSQL = InsertFundLimitsSQLStatement("weight_limit",
                    this.weight_limit.customValue,
                    this.weight_limit.action);
                DB.execSQL_noresults(execSQL, this.gbl_conn);
            }
            if (this.stk_notional_pct_limit.updated)
            {
                execSQL = InsertFundLimitsSQLStatement("stk_notional_pct_limit",
                    this.stk_notional_pct_limit.customValue,
                    this.stk_notional_pct_limit.action);
                DB.execSQL_noresults(execSQL, this.gbl_conn);
            }
            if (this.fut_notional_pct_limit.updated)
            {
                execSQL = InsertFundLimitsSQLStatement("fut_notional_pct_limit",
                    this.fut_notional_pct_limit.customValue,
                    this.fut_notional_pct_limit.action);
                DB.execSQL_noresults(execSQL, this.gbl_conn);
            }
            if (this.liquidity_limit.updated)
            {
                execSQL = InsertFundLimitsSQLStatement("liquidity_limit",
                    this.liquidity_limit.customValue,
                    this.liquidity_limit.action);
                DB.execSQL_noresults(execSQL, this.gbl_conn);
            }
            if (this.fut_leverage_limit.updated)
            {
                execSQL = InsertFundLimitsSQLStatement("fut_leverage_limit",
                    this.fut_leverage_limit.customValue,
                    this.fut_leverage_limit.action);
                DB.execSQL_noresults(execSQL, this.gbl_conn);
            }
            if (this.leverage_limit.updated)
            {
                execSQL = InsertFundLimitsSQLStatement("leverage_limit",
                    this.leverage_limit.customValue,
                    this.leverage_limit.action);
                DB.execSQL_noresults(execSQL, this.gbl_conn);
            }
            if (this.var_limit_factor.updated)
            {
                execSQL = InsertFundLimitsSQLStatement("var_limit_factor",
                    this.var_limit_factor.customValue,
                    this.var_limit_factor.action);
                DB.execSQL_noresults(execSQL, this.gbl_conn);
            }
            if (this.stress_limit_factor.updated)
            {
                execSQL = InsertFundLimitsSQLStatement("stress_limit_factor",
                    this.stress_limit_factor.customValue,
                    this.stress_limit_factor.action);
                DB.execSQL_noresults(execSQL, this.gbl_conn);
            }
            if (this.drawdown_limit.updated)
            {
                execSQL = InsertFundLimitsSQLStatement("drawdown_limit",
                    this.drawdown_limit.customValue,
                    this.drawdown_limit.action);
                DB.execSQL_noresults(execSQL, this.gbl_conn);
            }

            this.ResetUpdated();
        }

        private string InsertFundLimitsSQLStatement(string metric,  double? customValue, ActionType action )
        {
            string now = DateTime.UtcNow.ToString("dd-MMM-yyyy HH:mm:ss");
            Table tbl = new Table("fund_limits", this.gbl_conn, select_data: false);
            List<string> fund_limits_columns = tbl.get_table_columns();
            string insertSQL = "INSERT fund_limits(";
            for (int i = 0; i < fund_limits_columns.Count; i++)
            {
                insertSQL += fund_limits_columns[i].ToString() + ",";
            }
            insertSQL = insertSQL.Substring(0, insertSQL.Length - 1) + ")";
            string customValueString = customValue?.ToString();
            string actionString = (customValueString is null) ? "NULL" : "'" + action.ToString() + "'";

            string valuesSQL = "VALUES('" + now + "','" + this.fundname + "',"
                              + "'" + metric + "'," + (customValueString ?? "NULL") + "," + actionString + ")";

            return insertSQL + valuesSQL;
        }
    }

    public  class StockLeverageLimit
    {
        public double? defaultValue { get; set; }
        public double? customValue { get; set; }
        public ActionType action { get; set; }
        public double? liveValue { get; set; }
        public bool updated { get; set; }
    }
    public class FuturesLeverageLimit
    {
        public double? defaultValue { get; set; }
        public double? customValue { get; set; }
        public ActionType action { get; set; }
        public double? liveValue { get; set; }
        public bool updated { get; set; }
    }
    public class LeverageLimit
    {
        public double? defaultValue { get; set; }
        public double? customValue { get; set; }
        public ActionType action { get; set; }
        public double? liveValue { get; set; }
        public bool updated { get; set; }
    }
    public class VarLimitFactor
    {
        public double? defaultValue { get; set; }
        public double? customValue { get; set; }
        public ActionType action { get; set; }
        public double? liveValue { get; set; }
        public bool updated { get; set; }
    }
    public class StressLimitFactor
    {
        public double? defaultValue { get; set; }
        public double? customValue { get; set; }
        public ActionType action { get; set; }
        public double? liveValue { get; set; }
        public bool updated { get; set; }
    }
    public class DrawDownLimit
    {
        public double? defaultValue { get; set; }
        public double? customValue { get; set; }
        public ActionType action { get; set; }
        public double? liveValue { get; set; }
        public bool updated { get; set; }
    }
    public class StockNotionalPctLimit
    {
        public double? defaultValue { get; set; }
        public double? customValue { get; set; }
        public ActionType action { get; set; }
        public double? liveValue { get; set; }
        public bool updated { get; set; }
    }
    public class FuturesNotionalPctLimit
    {
        public double? defaultValue { get; set; }
        public double? customValue { get; set; }
        public ActionType action { get; set; }
        public double? liveValue { get; set; }
        public bool updated { get; set; }
    }
    public class LiquidityLimit
    {
        public double? defaultValue { get; set; }
        public double? customValue { get; set; }
        public ActionType action { get; set; }
        public double? liveValue { get; set; }
        public bool updated { get; set; }
    }
    public class WeightLimit
    {
        public double? defaultValue { get; set; }
        public double? customValue { get; set; }
        public ActionType action { get; set; }
        public double? liveValue { get; set; }
        public bool updated { get; set; }
    }
}
