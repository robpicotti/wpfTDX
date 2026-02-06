using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace wpfTDX
{
    public partial class ucLimits : UserControl
    {
        public event EventHandler RemoveControlRequested;

        public SqlConnection gbl_conn;
        private DataTable dtFunds;

        private Limits limits;                 // UI-thread-owned "current" limits (used for editing)
        private TickerLimits tickerLimits;     // UI-thread-owned "current" tickerLimits (used for editing)

        private readonly string ALL_FUNDS = "<* ALL FUNDS *>";
        private readonly TDX.db _db = new TDX.db();

        public List<string> ListOfFundNames = new List<string>();

        private bool blnValueChanged = false;
        private bool blnTickerLimitChanged = false;

        private bool _isLoadingLimits;
        private CancellationTokenSource _loadCts;

        // ------------------- DTOs -------------------
        private sealed class LimitsLoadResult
        {
            public DataTable FundLimitsTable { get; set; }
            public DataTable Notional { get; set; }
            public DataTable Liquidity { get; set; }
            public DataTable Weights { get; set; }
            public Limits CurrentLimitsForSelectedFund { get; set; }          // optional: to keep editing instance
            public TickerLimits CurrentTickerLimitsForSelectedFund { get; set; } // optional: to keep editing instance
            public List<string> Errors { get; set; }

            public LimitsLoadResult()
            {
                Errors = new List<string>();
            }
        }

        // ------------------- ctor -------------------
        public ucLimits(SqlConnection conn)
        {
            InitializeComponent();
            this.gbl_conn = conn;
            LoadForm();
        }

        private void LoadForm()
        {
            dtFunds = _db.get_funds(gbl_conn);

            // Filter out the "ALL_FUNDS" row (if it exists already)
            var filteredRows = dtFunds.AsEnumerable()
                .Where(row => row.Field<string>("fundname") != ALL_FUNDS);

            this.ListOfFundNames = filteredRows.Select(row => row.Field<string>("fundname")).ToList();

            // add "all funds" row
            DataRow newrow = dtFunds.NewRow();
            newrow["fundname"] = ALL_FUNDS;
            dtFunds.Rows.Add(newrow);

            // order the datatable
            var orderedRows = dtFunds.AsEnumerable()
                .OrderBy(row => row.Field<string>("fundname"));

            dtFunds = orderedRows.CopyToDataTable();

            cboFundName.Items.Clear();
            foreach (DataRow row in dtFunds.Rows)
            {
                cboFundName.Items.Add(Convert.ToString(row["fundname"]));
            }
        }

        // ------------------- Busy UI -------------------
        private void SetBusy(bool isBusy)
        {
            // these elements exist in your XAML (pbBusy, txtBusy)
            pbBusy.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
            txtBusy.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;

            cboFundName.IsEnabled = !isBusy;

            Cursor = isBusy ? Cursors.Wait : Cursors.Arrow;
        }

        // ------------------- SelectionChanged (async + safe) -------------------
        private async void cboFundName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingLimits) return;
            if (cboFundName.SelectedValue == null) return;

            string selectedFund = cboFundName.SelectedValue.ToString();

            // cancel any previous load
            if (_loadCts != null)
            {
                try { _loadCts.Cancel(); } catch { }
                _loadCts.Dispose();
            }
            _loadCts = new CancellationTokenSource();
            CancellationToken token = _loadCts.Token;

            _isLoadingLimits = true;
            SetBusy(true);

            try
            {
                LimitsLoadResult result = await Task.Run(() => BuildAllTables(selectedFund, token), token);

                if (token.IsCancellationRequested) return;

                // UI thread bind
                dgFundLimits.ItemsSource = result.FundLimitsTable != null ? result.FundLimitsTable.DefaultView : null;
                dgTickerLimits.ItemsSource = result.Notional != null ? result.Notional.DefaultView : null;
                dgLiquidityLimits.ItemsSource = result.Liquidity != null ? result.Liquidity.DefaultView : null;
                dgWeightLimits.ItemsSource = result.Weights != null ? result.Weights.DefaultView : null;

                // Store "current" instances for editing (ONLY for single fund mode)
                // If ALL_FUNDS selected, editing across many funds is ambiguous; keep null.
                this.limits = result.CurrentLimitsForSelectedFund;
                this.tickerLimits = result.CurrentTickerLimitsForSelectedFund;

                if (result.Errors != null && result.Errors.Count > 0)
                {
                    MessageBox.Show(string.Join("\r\n\r\n", result.Errors),
                        "Limits load warnings",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (OperationCanceledException)
            {
                // ignore: user changed fund again quickly
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Refresh limits", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetBusy(false);
                _isLoadingLimits = false;
            }
        }

        // ------------------- Background builder: fund + ticker tables -------------------
        private LimitsLoadResult BuildAllTables(string selectedFund, CancellationToken token)
        {
            LimitsLoadResult result = new LimitsLoadResult();

            // build local funds list ONLY (do not mutate this.FundsList)
            List<string> fundsToQuery;
            if (selectedFund != ALL_FUNDS)
                fundsToQuery = new List<string> { selectedFund };
            else
                fundsToQuery = new List<string>(this.ListOfFundNames);

            // Fund limits table
            result.FundLimitsTable = BuildFundLimitsTable(fundsToQuery, result.Errors, token);

            // Ticker limits tables
            TickerLimitsTables tables = BuildTickerLimitsTables(fundsToQuery, result.Errors, token);
            result.Notional = tables.Notional;
            result.Liquidity = tables.Liquidity;
            result.Weights = tables.Weights;

            // Only keep "current instances" when a single fund is selected
            if (selectedFund != ALL_FUNDS)
            {
                // Build a "fresh" instance for edit actions on UI thread later
                // (These constructors may hit DB; that's ok since we're already in background here.)
                try
                {
                    Limits l = new Limits(selectedFund, this.gbl_conn);
                    result.CurrentLimitsForSelectedFund = l;

                    try
                    {
                        TickerLimits tl = new TickerLimits(selectedFund, l.fundLimits, this.gbl_conn);
                        result.CurrentTickerLimitsForSelectedFund = tl;
                    }
                    catch { /* keep null */ }
                }
                catch
                {
                    // ignore - editing will still work via Save SQL logic if you rely on inserts
                }
            }

            return result;
        }

        private DataTable BuildFundLimitsTable(List<string> fundsList, List<string> errors, CancellationToken token)
        {
            blnValueChanged = false;

            DataTable dtAll = new DataTable();
            dtAll.Columns.Add("fundname");
            dtAll.Columns.Add("metric");
            dtAll.Columns.Add("default");
            dtAll.Columns.Add("custom", typeof(double));
            dtAll.Columns.Add("action");
            dtAll.Columns.Add("live");
            dtAll.Columns.Add("Flagged", typeof(bool));
            dtAll.Columns.Add("Color");

            for (int x = 0; x < fundsList.Count; x++)
            {
                token.ThrowIfCancellationRequested();

                string _fundName = fundsList[x];
                Limits localLimits = null;

                try
                {
                    localLimits = new Limits(_fundName, this.gbl_conn);
                }
                catch (Exception ex)
                {
                    errors.Add("Error setting up fund limits for fund: " + _fundName + "\r\n" + ex.Message);
                    continue;
                }

                for (int i = 0; i < localLimits.lstFundLimits.Count; i++)
                {
                    token.ThrowIfCancellationRequested();

                    DataRow row = dtAll.NewRow();
                    string limit = localLimits.lstFundLimits[i];

                    row["metric"] = limit;
                    row["fundname"] = _fundName;

                    switch (limit)
                    {
                        case "weight_limit":
                            row["default"] = localLimits.fundLimits.weight_limit.defaultValue;
                            row["custom"] = localLimits.fundLimits.weight_limit.customValue != null ? (object)localLimits.fundLimits.weight_limit.customValue : DBNull.Value;
                            row["action"] = localLimits.fundLimits.weight_limit.action;
                            break;

                        case "stk_notional_pct_limit":
                            row["default"] = localLimits.fundLimits.stk_notional_pct_limit.defaultValue;
                            row["custom"] = localLimits.fundLimits.stk_notional_pct_limit.customValue != null ? (object)localLimits.fundLimits.stk_notional_pct_limit.customValue : DBNull.Value;
                            row["action"] = localLimits.fundLimits.stk_notional_pct_limit.action;
                            break;

                        case "fut_notional_pct_limit":
                            row["default"] = localLimits.fundLimits.fut_notional_pct_limit.defaultValue;
                            row["custom"] = localLimits.fundLimits.fut_notional_pct_limit.customValue != null ? (object)localLimits.fundLimits.fut_notional_pct_limit.customValue : DBNull.Value;
                            row["action"] = localLimits.fundLimits.fut_notional_pct_limit.action;
                            break;

                        case "liquidity_limit":
                            row["default"] = localLimits.fundLimits.liquidity_limit.defaultValue;
                            row["custom"] = localLimits.fundLimits.liquidity_limit.customValue != null ? (object)localLimits.fundLimits.liquidity_limit.customValue : DBNull.Value;
                            row["action"] = localLimits.fundLimits.liquidity_limit.action;
                            break;

                        case "stk_leverage_limit":
                            row["default"] = localLimits.fundLimits.stk_leverage_limit.defaultValue;
                            row["custom"] = localLimits.fundLimits.stk_leverage_limit.customValue != null ? (object)localLimits.fundLimits.stk_leverage_limit.customValue : DBNull.Value;
                            row["action"] = localLimits.fundLimits.stk_leverage_limit.action;
                            row["live"] = localLimits.fundLimits.stk_leverage_limit.liveValue;
                            break;

                        case "fut_leverage_limit":
                            row["default"] = localLimits.fundLimits.fut_leverage_limit.defaultValue;
                            row["custom"] = localLimits.fundLimits.fut_leverage_limit.customValue != null ? (object)localLimits.fundLimits.fut_leverage_limit.customValue : DBNull.Value;
                            row["action"] = localLimits.fundLimits.fut_leverage_limit.action;
                            row["live"] = localLimits.fundLimits.fut_leverage_limit.liveValue;
                            break;

                        case "leverage_limit":
                            row["default"] = localLimits.fundLimits.leverage_limit.defaultValue;
                            row["custom"] = localLimits.fundLimits.leverage_limit.customValue != null ? (object)localLimits.fundLimits.leverage_limit.customValue : DBNull.Value;
                            row["action"] = localLimits.fundLimits.leverage_limit.action;
                            row["live"] = localLimits.fundLimits.leverage_limit.liveValue;
                            break;

                        case "var_limit_factor":
                            row["default"] = localLimits.fundLimits.var_limit_factor.defaultValue;
                            row["custom"] = localLimits.fundLimits.var_limit_factor.customValue != null ? (object)localLimits.fundLimits.var_limit_factor.customValue : DBNull.Value;
                            row["action"] = localLimits.fundLimits.var_limit_factor.action;
                            row["live"] = localLimits.fundLimits.var_limit_factor.liveValue;
                            break;

                        case "stress_limit_factor":
                            row["default"] = localLimits.fundLimits.stress_limit_factor.defaultValue;
                            row["custom"] = localLimits.fundLimits.stress_limit_factor.customValue != null ? (object)localLimits.fundLimits.stress_limit_factor.customValue : DBNull.Value;
                            row["action"] = localLimits.fundLimits.stress_limit_factor.action;
                            row["live"] = localLimits.fundLimits.stress_limit_factor.liveValue;
                            break;

                        case "drawdown_limit":
                            row["default"] = localLimits.fundLimits.drawdown_limit.defaultValue;
                            row["custom"] = localLimits.fundLimits.drawdown_limit.customValue != null ? (object)localLimits.fundLimits.drawdown_limit.customValue : DBNull.Value;
                            row["action"] = localLimits.fundLimits.drawdown_limit.action;
                            row["live"] = localLimits.fundLimits.drawdown_limit.liveValue;
                            break;
                    }

                    double? live = TryGetDouble(row["live"]);
                    double? custom = TryGetDouble(row["custom"]);
                    double? def = TryGetDouble(row["default"]);

                    bool flagged = TickerLimits.GetFlaggedStatus(live, custom, def);
                    row["Flagged"] = flagged;
                    row["Color"] = SetColor(flagged, null, custom);

                    dtAll.Rows.Add(row);
                }
            }

            if (dtAll.Rows.Count > 0)
            {
                dtAll = dtAll.AsEnumerable()
                    .OrderByDescending(r => r.Field<bool>("Flagged"))
                    .ThenBy(r => r.Field<string>("fundname"))
                    .ThenBy(r => r.Field<string>("metric"))
                    .CopyToDataTable();
            }

            dtAll.AcceptChanges();
            return dtAll;
        }

        private sealed class TickerLimitsTables
        {
            public DataTable Notional { get; set; }
            public DataTable Liquidity { get; set; }
            public DataTable Weights { get; set; }
        }

        private TickerLimitsTables BuildTickerLimitsTables(List<string> fundsList, List<string> errors, CancellationToken token)
        {
            blnTickerLimitChanged = false;

            DataTable dtNotionalAll = CreateTickerTableSchema();
            DataTable dtLiquidityAll = CreateTickerTableSchema();
            DataTable dtWeightsAll = CreateTickerTableSchema();

            for (int i = 0; i < fundsList.Count; i++)
            {
                token.ThrowIfCancellationRequested();

                string fund = fundsList[i];

                Limits localLimits;
                try
                {
                    localLimits = new Limits(fund, this.gbl_conn);
                }
                catch (Exception ex)
                {
                    errors.Add("Error setting up fund limits for fund: " + fund + "\r\n" + ex.Message);
                    continue;
                }

                TickerLimits localTickerLimits;
                try
                {
                    localTickerLimits = new TickerLimits(fund, localLimits.fundLimits, this.gbl_conn);
                }
                catch (Exception ex)
                {
                    errors.Add("Error setting up ticker limits for fund: " + fund + "\r\n" + ex.Message);
                    continue;
                }

                try
                {
                    dtNotionalAll.Merge(PopulateNotionalTickerLimits(localTickerLimits));
                }
                catch (Exception ex)
                {
                    errors.Add("Error populating notional ticker limits for fund: " + fund + "\r\n" + ex.Message);
                }

                try
                {
                    dtLiquidityAll.Merge(PopulateTickerLiquidityLimits(localTickerLimits));
                }
                catch (Exception ex)
                {
                    errors.Add("Error populating liquidity ticker limits for fund: " + fund + "\r\n" + ex.Message);
                }

                try
                {
                    dtWeightsAll.Merge(PopulateTickerWeightLimits(localTickerLimits));
                }
                catch (Exception ex)
                {
                    errors.Add("Error populating weight ticker limits for fund: " + fund + "\r\n" + ex.Message);
                }
            }

            // Sort
            dtNotionalAll = SortTickerTable(dtNotionalAll);
            dtLiquidityAll = SortTickerTable(dtLiquidityAll);
            dtWeightsAll = SortTickerTable(dtWeightsAll);

            dtNotionalAll.AcceptChanges();
            dtLiquidityAll.AcceptChanges();
            dtWeightsAll.AcceptChanges();

            return new TickerLimitsTables
            {
                Notional = dtNotionalAll,
                Liquidity = dtLiquidityAll,
                Weights = dtWeightsAll
            };
        }

        private static DataTable CreateTickerTableSchema()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("fundname");
            dt.Columns.Add("benchmarkname");
            dt.Columns.Add("tickername");
            dt.Columns.Add("Default");
            dt.Columns.Add("Custom", typeof(double));
            dt.Columns.Add("Live");
            dt.Columns.Add("Action");
            dt.Columns.Add("Frozen");
            dt.Columns.Add("Flagged", typeof(bool));
            dt.Columns.Add("Color");
            return dt;
        }

        private static DataTable SortTickerTable(DataTable dt)
        {
            if (dt == null) return dt;
            if (dt.Rows.Count == 0) return dt;

            return dt.AsEnumerable()
                .OrderByDescending(r => r.Field<bool>("Flagged"))
                .ThenByDescending(r => r.IsNull("Custom") ? double.MinValue : r.Field<double>("Custom"))
                .ThenBy(r => r.Field<string>("fundname"))
                .ThenBy(r => r.Field<string>("tickername"))
                .CopyToDataTable();
        }

        // ------------------- Populate helpers (NO this.tickerLimits usage!) -------------------
        private DataTable PopulateTickerWeightLimits(TickerLimits tl)
        {
            DataTable dtWeight = CreateTickerTableSchema();

            foreach (var kvp in tl.dictOfTickers)
            {
                string tickername = kvp.Key.ToString();
                string instrument = kvp.Value.ToString();

                DataRow row = dtWeight.NewRow();

                TickerWeightLimits one = tl.TickerWeightLimitsList.FirstOrDefault(tnl => tnl.tickerName == tickername);

                row["fundname"] = tl.fundName;
                row["benchmarkname"] = tl.benchmarkName;
                row["tickername"] = tickername;

                bool? frozen = null;

                if (one != null && (one.customLimit != null || one.liveValue != null || one.valid != null))
                {
                    row["Custom"] = one.customLimit != null ? (object)one.customLimit : DBNull.Value;
                    row["Live"] = one.liveValue != null ? (object)one.liveValue : DBNull.Value;
                    row["Action"] = one.action;

                    frozen = one.valid == true ? (bool?)false : (one.valid == false ? (bool?)true : null);
                    row["Frozen"] = frozen;
                }
                else
                {
                    row["Custom"] = DBNull.Value;
                    row["Live"] = DBNull.Value;
                    row["Action"] = DBNull.Value;
                    row["Frozen"] = DBNull.Value;
                }

                // NOTE: your original code used liquidity_limit.defaultValue here (maybe intentional, but odd).
                // Keeping your existing behaviour:
                row["Default"] = tl.fundLimit.liquidity_limit.defaultValue;

                bool flagged = TickerLimits.GetFlaggedStatus(one != null ? one.liveValue : null,
                                                            one != null ? one.customLimit : null,
                                                            tl.fundLimit.liquidity_limit.defaultValue);
                row["Flagged"] = flagged;

                double? custom = TryGetDouble(row["Custom"]);
                row["Color"] = SetColor(flagged, frozen, custom);

                dtWeight.Rows.Add(row);
            }

            return dtWeight;
        }

        private DataTable PopulateTickerLiquidityLimits(TickerLimits tl)
        {
            DataTable dtLiquidity = CreateTickerTableSchema();

            foreach (var kvp in tl.dictOfTickers)
            {
                string tickername = kvp.Key.ToString();
                string instrument = kvp.Value.ToString();

                DataRow row = dtLiquidity.NewRow();

                TickerLiquidityLimits one = tl.TickerLiquidityLimitsList.FirstOrDefault(tnl => tnl.tickerName == tickername);

                row["fundname"] = tl.fundName;
                row["benchmarkname"] = tl.benchmarkName;
                row["tickername"] = tickername;

                bool? frozen = null;

                if (one != null && (one.customLimit != null || one.liveValue != null || one.valid != null))
                {
                    row["Custom"] = one.customLimit != null ? (object)one.customLimit : DBNull.Value;
                    row["Live"] = one.liveValue != null ? (object)one.liveValue : DBNull.Value;
                    row["Action"] = one.action;

                    frozen = one.valid == true ? (bool?)false : (one.valid == false ? (bool?)true : null);
                    row["Frozen"] = frozen;
                }
                else
                {
                    row["Custom"] = DBNull.Value;
                    row["Live"] = DBNull.Value;
                    row["Action"] = DBNull.Value;
                    row["Frozen"] = DBNull.Value;
                }

                row["Default"] = tl.fundLimit.liquidity_limit.defaultValue;

                bool flagged = TickerLimits.GetFlaggedStatus(one != null ? one.liveValue : null,
                                                            one != null ? one.customLimit : null,
                                                            tl.fundLimit.liquidity_limit.defaultValue);
                row["Flagged"] = flagged;

                double? custom = TryGetDouble(row["Custom"]);
                row["Color"] = SetColor(flagged, frozen, custom);

                dtLiquidity.Rows.Add(row);
            }

            return dtLiquidity;
        }

        private DataTable PopulateNotionalTickerLimits(TickerLimits tl)
        {
            DataTable dtNotional = CreateTickerTableSchema();

            foreach (var kvp in tl.dictOfTickers)
            {
                string tickername = kvp.Key.ToString();
                string instrument = kvp.Value.ToString();

                DataRow row = dtNotional.NewRow();

                TickerNotionalLimits one = tl.TickerNotionalLimitsList.FirstOrDefault(tnl => tnl.tickerName == tickername);

                row["fundname"] = tl.fundName;
                row["benchmarkname"] = tl.benchmarkName;
                row["tickername"] = tickername;

                bool? frozen = null;

                if (one != null && (one.customLimit != null || one.liveValue != null || one.valid != null))
                {
                    row["Custom"] = one.customLimit != null ? (object)one.customLimit : DBNull.Value;
                    row["Live"] = one.liveValue != null ? (object)one.liveValue : DBNull.Value;
                    row["Action"] = one.action;

                    frozen = one.valid == true ? (bool?)false : (one.valid == false ? (bool?)true : null);
                    row["Frozen"] = frozen;
                }
                else
                {
                    row["Custom"] = DBNull.Value;
                    row["Live"] = DBNull.Value;
                    row["Action"] = DBNull.Value;
                    row["Frozen"] = DBNull.Value;
                }

                double? defaultValue = null;
                if (instrument == "equity" || instrument == "etf")
                    defaultValue = tl.fundLimit.stk_notional_pct_limit.defaultValue;
                else if (instrument == "future" || instrument == "spread" || instrument == "swap")
                    defaultValue = tl.fundLimit.fut_notional_pct_limit.defaultValue;

                row["Default"] = defaultValue.HasValue ? (object)defaultValue.Value : DBNull.Value;

                bool flagged = TickerLimits.GetFlaggedStatus(one != null ? one.liveValue : null,
                                                            one != null ? one.customLimit : null,
                                                            defaultValue);
                row["Flagged"] = flagged;

                double? custom = TryGetDouble(row["Custom"]);
                row["Color"] = SetColor(flagged, frozen, custom);

                dtNotional.Rows.Add(row);
            }

            return dtNotional;
        }

        // ------------------- Helpers -------------------
        private static double? TryGetDouble(object v)
        {
            if (v == null || v == DBNull.Value) return null;
            double d;
            if (double.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture),
                                NumberStyles.Any, CultureInfo.InvariantCulture, out d))
                return d;
            return null;
        }

        private string SetColor(bool flagged, bool? frozen, double? customValue)
        {
            string strOut = "";
            if (flagged)
            {
                if (frozen == null) strOut = "R";
                else if (frozen == true) strOut = "R";
                else if (frozen == false) strOut = "Y";
            }

            if (strOut == "" && customValue != null)
                strOut = "G";

            return strOut;
        }

        // ------------------- Close -------------------
        private void cmdClose_Click(object sender, RoutedEventArgs e)
        {
            RemoveControlRequested?.Invoke(this, EventArgs.Empty);
        }

        // ------------------- Existing Save logic (minimal changes) -------------------
        // IMPORTANT: RefreshTickerLimits() and RefreshFundLimits() were UI-thread + slow.
        // We now refresh via cbo selection async loader. After inserts, just trigger a reload.

        private void TriggerReloadCurrentSelection()
        {
            // re-run the selection load safely (UI thread)
            if (cboFundName.SelectedValue != null)
            {
                // Force "SelectionChanged" logic without changing selection:
                cboFundName_SelectionChanged(cboFundName, null);
            }
        }

        private void UpdateFundLimits()
        {
            try
            {
                if (this.limits != null && this.limits.fundLimits != null)
                {
                    this.limits.fundLimits.UpdateFundLimits(this.limits.fundLimits.fundname);
                }
                TriggerReloadCurrentSelection();
                MessageBox.Show("fund_limits updated", "fund limits", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "update fund limits", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ---------- utilities used by your RowSave_Click ----------
        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null)
            {
                DependencyObject parent = VisualTreeHelper.GetParent(child);
                if (parent is T)
                    return (T)parent;
                child = parent;
            }
            return null;
        }

        private static bool ColumnChanged(DataRow row, string col)
        {
            if (!row.HasVersion(DataRowVersion.Original)) return true;

            object cur = row[col];
            object orig = row[col, DataRowVersion.Original];

            if (cur == DBNull.Value) cur = null;
            if (orig == DBNull.Value) orig = null;
            return !Equals(cur, orig);
        }

        private static double? ToNullableDouble(object v)
        {
            if (v == null || v == DBNull.Value) return null;
            double d;
            if (double.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture),
                                NumberStyles.Any, CultureInfo.InvariantCulture, out d))
                return d;
            return null;
        }

        private void RowSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;

                var btn = (Button)sender;
                var grid = FindParent<DataGrid>(btn);
                if (grid != null)
                {
                    grid.CommitEdit(DataGridEditingUnit.Cell, true);
                    grid.CommitEdit(DataGridEditingUnit.Row, true);
                }

                var drv = btn.DataContext as DataRowView;
                if (drv == null) return;
                var row = drv.Row;

                var tag = (btn.Tag as string) ?? "";

                if (string.Equals(tag, "Fund", StringComparison.OrdinalIgnoreCase))
                {
                    SaveFundLimitsRow(row);
                }
                else if (string.Equals(tag, "Notional", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(tag, "Liquidity", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(tag, "Weight", StringComparison.OrdinalIgnoreCase))
                {
                    SaveTickerLimitRow(tag, row);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Save", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Cursor = Cursors.Arrow;
                // your old code called UpdateFundLimits() always; keep behaviour for fund rows only:
            }
        }

        private void SaveFundLimitsRow(DataRow row)
        {
            bool customChanged = ColumnChanged(row, "custom");
            bool actionChanged = ColumnChanged(row, "action");
            if (!customChanged && !actionChanged) return;

            string fund = Convert.ToString(row["fundname"]);
            string metric = Convert.ToString(row["metric"]);
            string action = Convert.ToString(row["action"]) ?? "Default";
            double? custom = ToNullableDouble(row["custom"]);

            if (string.Equals(action, "Default", StringComparison.OrdinalIgnoreCase))
                custom = null;
            else if (!custom.HasValue && !string.Equals(action, "Default", StringComparison.OrdinalIgnoreCase))
                return;

            // Make sure limits exists even if ALL_FUNDS was selected
            this.limits = new Limits(fund, this.gbl_conn);
            this.limits.fundLimits = new FundLimits(fund, this.gbl_conn);

            ActionType at = this.limits.fundLimits.GetActionType(action);

            switch (metric)
            {
                case "weight_limit":
                    this.limits.fundLimits.weight_limit.customValue = custom;
                    this.limits.fundLimits.weight_limit.action = at;
                    this.limits.fundLimits.weight_limit.updated = true;
                    break;

                case "stk_notional_pct_limit":
                    this.limits.fundLimits.stk_notional_pct_limit.customValue = custom;
                    this.limits.fundLimits.stk_notional_pct_limit.action = at;
                    this.limits.fundLimits.stk_notional_pct_limit.updated = true;
                    break;

                case "fut_notional_pct_limit":
                    this.limits.fundLimits.fut_notional_pct_limit.customValue = custom;
                    this.limits.fundLimits.fut_notional_pct_limit.action = at;
                    this.limits.fundLimits.fut_notional_pct_limit.updated = true;
                    break;

                case "liquidity_limit":
                    this.limits.fundLimits.liquidity_limit.customValue = custom;
                    this.limits.fundLimits.liquidity_limit.action = at;
                    this.limits.fundLimits.liquidity_limit.updated = true;
                    break;

                case "stk_leverage_limit":
                    this.limits.fundLimits.stk_leverage_limit.customValue = custom;
                    this.limits.fundLimits.stk_leverage_limit.action = at;
                    this.limits.fundLimits.stk_leverage_limit.updated = true;
                    break;

                case "fut_leverage_limit":
                    this.limits.fundLimits.fut_leverage_limit.customValue = custom;
                    this.limits.fundLimits.fut_leverage_limit.action = at;
                    this.limits.fundLimits.fut_leverage_limit.updated = true;
                    break;

                case "leverage_limit":
                    this.limits.fundLimits.leverage_limit.customValue = custom;
                    this.limits.fundLimits.leverage_limit.action = at;
                    this.limits.fundLimits.leverage_limit.updated = true;
                    break;

                case "var_limit_factor":
                    this.limits.fundLimits.var_limit_factor.customValue = custom;
                    this.limits.fundLimits.var_limit_factor.action = at;
                    this.limits.fundLimits.var_limit_factor.updated = true;
                    break;

                case "stress_limit_factor":
                    this.limits.fundLimits.stress_limit_factor.customValue = custom;
                    this.limits.fundLimits.stress_limit_factor.action = at;
                    this.limits.fundLimits.stress_limit_factor.updated = true;
                    break;

                case "drawdown_limit":
                    this.limits.fundLimits.drawdown_limit.customValue = custom;
                    this.limits.fundLimits.drawdown_limit.action = at;
                    this.limits.fundLimits.drawdown_limit.updated = true;
                    break;

                default:
                    MessageBox.Show("Unknown metric: " + metric, "Save", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
            }

            // write + reload
            UpdateFundLimits();
        }

        private void SaveTickerLimitRow(string limitType, DataRow row)
        {
            bool customChanged = ColumnChanged(row, "Custom");
            bool actionChanged = ColumnChanged(row, "Action");
            if (!customChanged && !actionChanged) return;

            string fund = Convert.ToString(row["fundname"]);
            string ticker = Convert.ToString(row["tickername"]);
            string action = Convert.ToString(row["Action"]) ?? "Default";
            double? custom = ToNullableDouble(row["Custom"]);

            if (string.Equals(action, "Default", StringComparison.OrdinalIgnoreCase))
                custom = null;
            else if (!custom.HasValue && !string.Equals(action, "Default", StringComparison.OrdinalIgnoreCase))
                return;

            InsertTickerLimitRow(fund, limitType, ticker, custom, action);

            // reload async
            TriggerReloadCurrentSelection();
        }

        private void InsertTickerLimitRow(string fundname, string limitType, string tickername, double? custom, string action)
        {
            string table;
            if (string.Equals(limitType, "Notional", StringComparison.OrdinalIgnoreCase))
                table = "notional_pct_limits";
            else if (string.Equals(limitType, "Liquidity", StringComparison.OrdinalIgnoreCase))
                table = "liquidity_limits";
            else if (string.Equals(limitType, "Weight", StringComparison.OrdinalIgnoreCase))
                table = "weight_limits";
            else
                throw new InvalidOperationException("Unknown limit type: " + limitType);

            string runtime = DateTime.UtcNow.ToString("dd-MMM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            string customSql = custom.HasValue
                ? custom.Value.ToString(CultureInfo.InvariantCulture)
                : "NULL";

            string sql = "INSERT " + table + " VALUES('" + runtime + "','" + fundname + "','" + tickername + "'," + customSql + ",'" + action + "')";
            this._db.execSQL_noresults(sql, this.gbl_conn);
        }

        // ------------------- Legacy handlers you had (left empty) -------------------
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void ComboBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e) { }
        private void ComboBox_SelectionChanged_2(object sender, SelectionChangedEventArgs e) { }
        private void ComboBox_SelectionChanged_3(object sender, SelectionChangedEventArgs e) { }
        private void ComboBox_SelectionChanged_4(object sender, SelectionChangedEventArgs e) { }

        private void btnUpdateTickerLimits_Click(object sender, RoutedEventArgs e) { }

        // If you still have these CellEditEnding handlers wired, you can keep them,
        // but they call RefreshTickerLimits() (UI + slow). Better: remove their Refresh calls
        // and rely on RowSave_Click + TriggerReloadCurrentSelection().
        // Keeping your existing ones is fine as long as they don't call RefreshTickerLimits().
        private void dgTickerLimits_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e) { }
        private void dgLiquidityLimits_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e) { }
        private void dgWeightLimits_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e) { }
        private void dgFundLimits_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e) { }
    }

    public class ActionItemsConverter : MarkupExtension, IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var staticItems = new string[] { "Scale", "Hard", "Default" };
            var dynamicItems = values[0] as IEnumerable<string>;

            var combinedItems = new List<string>(staticItems);
            if (dynamicItems != null)
                combinedItems.AddRange(dynamicItems);

            return combinedItems;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
