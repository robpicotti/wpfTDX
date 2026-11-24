using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace wpfTDX
{
    public sealed class SystemTradesViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly SystemTradesService _service;
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private bool _disposed;
        private bool _isLoading, _refreshPending;
        private string _status = "Ready to load recorded model trades.";
        private string _captureStatus = "Capture status unknown";
        private IReadOnlyList<SystemTradeDataModel> _rows = new List<SystemTradeDataModel>();
        private SystemTradeDataModel _selectedTrade;
        private string _appliedReviewFilter = "Unreviewed", _appliedStatusFilter = "All";

        public SystemTradesViewModel() : this(new SystemTradesService()) { }
        public SystemTradesViewModel(SystemTradesService service) { _service = service; }

        public string FundFilter { get; set; } = "TenDSelect";
        public string TickerFilter { get; set; } = "";
        public string ReviewFilter { get; set; } = "Unreviewed";
        public string TradeStatusFilter { get; set; } = "All";
        public string[] ReviewFilters { get; } = { "Unreviewed", "All", "Accepted", "Rejected" };
        public string[] TradeStatusFilters { get; } = { "All", "Open", "Closed" };
        public IReadOnlyList<SystemTradeDataModel> Rows { get => _rows; private set { _rows = value; Changed(); Changed(nameof(Summary)); Changed(nameof(IsEmpty)); } }
        public SystemTradeDataModel SelectedTrade { get => _selectedTrade; set { _selectedTrade = value; Changed(); Changed(nameof(HasSelection)); Changed(nameof(CanReview)); } }
        public bool HasSelection => SelectedTrade != null;
        public bool CanReview => HasSelection && !IsLoading;
        public bool IsEmpty => !IsLoading && Rows.Count == 0;
        public bool IsLoading { get => _isLoading; private set { _isLoading = value; Changed(); Changed(nameof(CanRefresh)); Changed(nameof(CanReview)); Changed(nameof(IsEmpty)); } }
        public bool CanRefresh => !IsLoading;
        public string Status { get => _status; private set { _status = value; Changed(); } }
        public string CaptureStatus { get => _captureStatus; private set { _captureStatus = value; Changed(); } }
        public string Summary => string.Format("{0:N0} trades  |  {1:N0} open  |  {2:N0} unreviewed",
            Rows.Count, Rows.Count(r => r.TradeStatus == "open"), Rows.Count(r => r.Decision == null));

        public async Task<SystemTradeReviewViewModel> BeginReviewAsync()
        {
            if (!CanReview || _disposed) return null;
            var selected = SelectedTrade;
            IsLoading = true;
            Status = "Loading trade details and review options...";
            try
            {
                var options = await _service.OptionsAsync(_lifetime.Token);
                if (_disposed) return null;
                var current = await _service.FindAsync(selected, _lifetime.Token);
                if (_disposed) return null;
                if (current == null)
                {
                    Status = "This trade is no longer available. Refresh the trade list.";
                    return null;
                }
                Status = "Review details loaded.";
                return new SystemTradeReviewViewModel(_service, current, options, Environment.UserName);
            }
            catch (Exception ex)
            {
                if (!_disposed) Status = "Unable to open review: " + ex.Message;
                return null;
            }
            finally { await FinishLoadingAsync(); }
        }

        public void ApplyReviewResult(SystemTradeReviewViewModel review)
        {
            if (_disposed) return;
            var updated = review.LatestRow;
            if (updated == null) return;
            var rows = Rows.ToList();
            var index = rows.FindIndex(r => r.SystemTradeId == updated.SystemTradeId);
            if (index < 0) return;
            var matches = !review.RowMissing && (_appliedReviewFilter == "All" ||
                (_appliedReviewFilter == "Unreviewed" ? updated.Decision == null :
                 string.Equals(_appliedReviewFilter, updated.Decision, StringComparison.OrdinalIgnoreCase))) &&
                (_appliedStatusFilter == "All" || string.Equals(_appliedStatusFilter, updated.TradeStatus, StringComparison.OrdinalIgnoreCase));
            if (matches) rows[index] = updated;
            else rows.RemoveAt(index);
            Rows = rows;
            SelectedTrade = matches ? updated : rows.ElementAtOrDefault(index) ?? rows.LastOrDefault();
            if (review.Saved) Status = "Review saved. " + (matches ? "The recorded row is updated." : "The trade no longer matches the selected filters.");
            else if (review.SaveBlocked) Status = "Review outcome needs verification. Refresh the trade list before editing again.";
        }

        public async Task ApplyFiltersAsync()
        {
            if (_disposed) return;
            if (IsLoading)
            {
                _refreshPending = true;
                Status = "Applying selected filters after the current request...";
                return;
            }
            await RefreshAsync();
        }

        private async Task FinishLoadingAsync()
        {
            if (_disposed) return;
            IsLoading = false;
            if (_refreshPending)
            {
                _refreshPending = false;
                await RefreshAsync();
            }
        }

        public async Task RefreshAsync()
        {
            if (IsLoading || _disposed) return;
            IsLoading = true;
            var selectedId = SelectedTrade?.SystemTradeId;
            var fund = FundFilter;
            var ticker = TickerFilter;
            var review = ReviewFilter;
            var status = TradeStatusFilter;
            Status = "Loading recorded model trades...";
            CaptureStatus = "Checking capture status...";
            try
            {
                bool enabled = await _service.CaptureEnabledAsync(_lifetime.Token);
                if (_disposed) return;
                CaptureStatus = enabled ? "Capture enabled" : "Capture disabled - lifecycle updates are paused";
                var rows = await _service.LoadAsync(fund, ticker, review,
                    status, _lifetime.Token);
                if (_disposed || _refreshPending) return;
                Rows = rows;
                _appliedReviewFilter = review;
                _appliedStatusFilter = status;
                // A user can change selection while a background request is in flight.
                var currentId = SelectedTrade?.SystemTradeId ?? selectedId;
                SelectedTrade = rows.FirstOrDefault(r => r.SystemTradeId == currentId) ?? rows.FirstOrDefault();
                Status = (rows.Count == 0 ? "No trades match these filters. " : "Loaded recorded trades. ") +
                    "Refreshed " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC.";
            }
            catch (OperationCanceledException)
            {
                if (!_disposed) Status = "Loading timed out. Automatic refresh will retry; previous results are retained.";
            }
            catch (Exception ex)
            {
                if (!_disposed)
                {
                    Status = "Unable to load system trades: " + ex.Message + " Previous results are retained; automatic refresh will retry.";
                    if (CaptureStatus == "Checking capture status...") CaptureStatus = "Capture status unavailable";
                }
            }
            finally { await FinishLoadingAsync(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _lifetime.Cancel();
            _service.Dispose();
            // Pending requests can still inspect the token until their cancellation completes.
        }
    }
}
