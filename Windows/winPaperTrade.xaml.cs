using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace wpfTDX
{
    public partial class winPaperTrade : Window
    {
        private readonly PositionsDataModel _position;
        private readonly string _fundname;
        private double _tadPosition;
        private double _lastPrice;

        /// <summary>
        /// Paper trade dialog — reconciles TAD position with actual IBK position
        /// by inserting a synthetic execution row.
        /// </summary>
        public winPaperTrade(PositionsDataModel position, string fundname)
        {
            InitializeComponent();
            _position = position;
            _fundname = fundname;
            _tadPosition = position.PositionLive;
            _lastPrice = position.PriceLive;

            // Populate read-only fields
            txtTicker.Text = position.TickerName;
            txtTadId.Text = position.TadId;
            txtSubaccount.Text = position.SubaccountName;
            txtTadPosition.Text = _tadPosition.ToString("F0");
            txtPrice.Text = _lastPrice.ToString("F4");
        }

        private void txtIbkPosition_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (double.TryParse(txtIbkPosition.Text, out double ibkPos))
            {
                double offset = ibkPos - _tadPosition;
                txtOffset.Text = offset.ToString("F0");
                txtAction.Text = offset > 0 ? "BUY" : offset < 0 ? "SELL" : "NO TRADE";
                btnSubmit.IsEnabled = Math.Abs(offset) > 0.001;
                btnPreview.IsEnabled = btnSubmit.IsEnabled;

                // Colour feedback
                txtOffset.Foreground = offset > 0
                    ? System.Windows.Media.Brushes.DarkGreen
                    : offset < 0
                        ? System.Windows.Media.Brushes.DarkRed
                        : System.Windows.Media.Brushes.Black;
                txtAction.Foreground = txtOffset.Foreground;
            }
            else
            {
                txtOffset.Text = "";
                txtAction.Text = "";
                btnSubmit.IsEnabled = false;
                btnPreview.IsEnabled = false;
            }
        }

        private async void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(txtIbkPosition.Text, out double ibkPos)) return;

            double offset = ibkPos - _tadPosition;
            string action = offset > 0 ? "BUY" : "SELL";
            double executedSize = Math.Abs(offset);
            string now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string execId = "PAPER_preview";

            // Get tad_order_id from API
            string tadOrderId = "N/A";
            try
            {
                tadOrderId = await GetTadOrderIdAsync(_position.TadId, _position.SubaccountName, executedSize.ToString("F0"));
            }
            catch { }

            var preview =
                $"runtime: {now}\n" +
                $"account: {_position.SubaccountName}\n" +
                $"subaccount: {_position.SubaccountName}\n" +
                $"broker_id: 16\n" +
                $"execution_id: {execId}\n" +
                $"execution_time: {now}\n" +
                $"effective_date: {today}\n" +
                $"settlement_date: {today}\n" +
                $"tickername: {_position.TickerName}\n" +
                $"tad_id: {_position.TadId}\n" +
                $"multiplier: {(double.IsNaN(_position.Multiplier) || _position.Multiplier == 0 ? 1.0 : _position.Multiplier)}\n" +
                $"action: {action}\n" +
                $"executed_size: {executedSize:F0}\n" +
                $"executed_price: {_lastPrice:F4}\n" +
                $"cumulative_qty: {executedSize:F0}\n" +
                $"brok_uniq_order_id: {execId}\n" +
                $"order_id: NULL\n" +
                $"tad_order_id: {tadOrderId}\n" +
                $"exch_currency: {_position.ExchangeCurrency}\n" +
                $"commissions_fees: 0\n" +
                $"roll_date: {_position.RollDate?.ToString("yyyy-MM-dd") ?? "NULL"}\n" +
                $"bbg_lasttrade_date: {_position.BbgLastTradeDate?.ToString("yyyy-MM-dd") ?? "NULL"}\n" +
                $"instrument: {_position.Instrument}\n" +
                $"report_category: {_position.ReportCategory}\n" +
                $"contract_increment: {_position.ContractIncrement}\n" +
                $"category: {_position.Category}\n" +
                $"bbg_symbol_exp: {_position.BbgSymbolExp}\n" +
                $"solicited_trade: False\n" +
                $"is_paper: True";

            MessageBox.Show(this, preview, "Preview — Execution Row", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(txtIbkPosition.Text, out double ibkPos))
            {
                MessageBox.Show(this, "Please enter a valid IBK position.", "Invalid input",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            double offset = ibkPos - _tadPosition;
            if (Math.Abs(offset) < 0.001)
            {
                MessageBox.Show(this, "Positions are already aligned — no trade needed.", "No offset",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string action = offset > 0 ? "BUY" : "SELL";
            double executedSize = Math.Abs(offset);

            var confirmMsg = $"This will insert a PAPER {action} of {executedSize:F0} {_position.TickerName} " +
                             $"at {_lastPrice:F4} into the executions table.\n\n" +
                             $"TAD position: {_tadPosition:F0} → IBK position: {ibkPos:F0}\n\n" +
                             $"This is flagged as is_paper=True.\n\nProceed?";

            if (MessageBox.Show(this, confirmMsg, "Confirm Paper Trade",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                btnSubmit.IsEnabled = false;
                btnSubmit.Content = "Submitting...";

                string now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
                string execId = "PAPER_" + Guid.NewGuid().ToString("N").Substring(0, 16);

                // Get tad_order_id from API
                string tadOrderId = await GetTadOrderIdAsync(
                    _position.TadId, _position.SubaccountName, executedSize.ToString("F0"));

                var executionRow = new
                {
                    runtime = now,
                    account = _position.SubaccountName,
                    subaccount = _position.SubaccountName,
                    broker_id = 16, // TAD_EMS execution broker
                    execution_id = execId,
                    execution_time = now,
                    effective_date = today,
                    settlement_date = today,
                    tickername = _position.TickerName,
                    tad_id = _position.TadId,
                    multiplier = double.IsNaN(_position.Multiplier) || _position.Multiplier == 0
                        ? 1.0 : _position.Multiplier,
                    action = action,
                    executed_size = executedSize,
                    executed_price = _lastPrice,
                    brok_uniq_order_id = execId,
                    order_id = (string)null,
                    tad_order_id = tadOrderId,
                    cumulative_qty = executedSize,
                    exchange = (string)null,
                    bbg_figi = (string)null,
                    bbg_isin = (string)null,
                    parent_order_id = (string)null,
                    exch_currency = _position.ExchangeCurrency,
                    commissions_fees = 0.0,
                    roll_date = _position.RollDate?.ToString("yyyy-MM-dd"),
                    bbg_lasttrade_date = _position.BbgLastTradeDate?.ToString("yyyy-MM-dd"),
                    instrument = _position.Instrument,
                    report_category = _position.ReportCategory,
                    contract_increment = (int)_position.ContractIncrement,
                    category = _position.Category,
                    bbg_symbol_exp = _position.BbgSymbolExp,
                    solicited_trade = false,
                    is_paper = true,
                };

                var ws = new WebServiceData();
                var payload = new
                {
                    table_name = "executions",
                    data = new[] { executionRow }
                };

                string body = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Include,
                    FloatFormatHandling = FloatFormatHandling.DefaultValue, // NaN → 0.0
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc
                });

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-API-Key", ws.apiKey);
                    using (var content = new StringContent(body, Encoding.UTF8, "application/json"))
                    using (var resp = await client.PostAsync($"{ws.baseUrl}/insert_table", content))
                    {
                        resp.EnsureSuccessStatusCode();
                    }
                }

                MessageBox.Show(this,
                    $"Paper {action} of {executedSize:F0} {_position.TickerName} inserted successfully.\n\n" +
                    $"Execution ID: {execId}\n" +
                    $"RTL will process this into a transaction on next run.",
                    "Paper Trade Submitted", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to insert paper trade:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                btnSubmit.IsEnabled = true;
                btnSubmit.Content = "Submit Paper Trade";
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private async Task<string> GetTadOrderIdAsync(string tadId, string subaccount, string allocationAmount)
        {
            var ws = new WebServiceData();
            var request = new { tad_id = tadId, subaccount, allocation_amount = allocationAmount };
            string body = JsonConvert.SerializeObject(request);

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", ws.apiKey);
                using (var content = new StringContent(body, Encoding.UTF8, "application/json"))
                using (var resp = await client.PostAsync($"{ws.baseUrl}/create_tad_order_id", content))
                {
                    resp.EnsureSuccessStatusCode();
                    string json = await resp.Content.ReadAsStringAsync();
                    var result = Newtonsoft.Json.Linq.JObject.Parse(json);
                    return (string)result["tad_order_id"];
                }
            }
        }
    }
}
