using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace wpfTDX
{
    /// <summary>One explicit review. Conflicts and uncertain saves require reinspection.</summary>
    public sealed class SystemTradeReviewViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly SystemTradesService _service;
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private readonly string _customCode;
        private SystemTradeDataModel _current;
        private SystemTradeReviewChoice _decision;
        private SystemTradeRejectionReason _reason;
        private string _reviewer, _customReason = "", _status = "Choose a review decision and save when ready.";
        private string _exitDecision = "System", _exitReason = "", _exitPrice = "", _exitUtc = "";
        private DateTime? _exitDate;
        private string _exitHour = "00", _exitMinute = "00";
        private bool _busy, _saveBlocked, _disposed;

        public SystemTradeReviewViewModel(SystemTradesService service, SystemTradeDataModel current,
            SystemTradesOptions options, string reviewer)
        {
            _service = service;
            _customCode = options.CustomRejectionCode;
            Decisions = options.Decisions.Select(d => new SystemTradeReviewChoice { Code = d }).ToList();
            Reasons = options.RejectionReasons.Where(r => r.Code != _customCode).ToList();
            Reasons.Add(new SystemTradeRejectionReason { Code = _customCode, Label = "Other (custom reason)" });
            _reviewer = reviewer;
            SetCurrent(current);
        }

        public List<SystemTradeReviewChoice> Decisions { get; }
        public List<SystemTradeRejectionReason> Reasons { get; }
        public SystemTradeDataModel Current => _current;
        public SystemTradeDataModel LatestRow { get; private set; }
        public bool RowMissing { get; private set; }
        public bool Saved { get; private set; }
        public SystemTradeReviewChoice Decision { get => _decision; set { _decision = value; Changed(); Changed(nameof(IsRejected)); Changed(nameof(IsCustom)); Changed(nameof(IsAccepted)); Changed(nameof(IsDiscretionary)); Changed(nameof(ExitValidationMessage)); Changed(nameof(CanSave)); } }
        public SystemTradeRejectionReason Reason { get => _reason; set { _reason = value; Changed(); Changed(nameof(IsCustom)); Changed(nameof(CanSave)); } }
        public string Reviewer { get => _reviewer; set { _reviewer = value; Changed(); Changed(nameof(CanSave)); } }
        public string CustomReason { get => _customReason; set { _customReason = value; Changed(); Changed(nameof(CanSave)); } }
        public List<string> ExitDecisions { get; } = new List<string> { "System", "Discretionary" };
        public bool IsAccepted => Decision?.Code == "accepted";
        public bool IsDiscretionary => IsAccepted && ExitDecision == "Discretionary";
        public string ExitDecision { get => _exitDecision; set { _exitDecision = value; ExitChanged(); Changed(nameof(IsDiscretionary)); } }
        public string ExitReason { get => _exitReason; set { _exitReason = value; ExitChanged(); } }
        public string ExitPrice { get => _exitPrice; set { _exitPrice = value; ExitChanged(); } }
        public List<string> ExitHours { get; } = Enumerable.Range(0, 24).Select(n => n.ToString("00", CultureInfo.InvariantCulture)).ToList();
        public List<string> ExitMinutes { get; } = Enumerable.Range(0, 60).Select(n => n.ToString("00", CultureInfo.InvariantCulture)).ToList();
        public DateTime? ExitDate { get => _exitDate; set { _exitDate = value?.Date; UpdateExitUtc(); Changed(); } }
        public string ExitHour { get => _exitHour; set { _exitHour = value; UpdateExitUtc(); Changed(); } }
        public string ExitMinute { get => _exitMinute; set { _exitMinute = value; UpdateExitUtc(); Changed(); } }
        public string ExitUtc
        {
            get => _exitUtc;
            set
            {
                _exitUtc = value;
                bool parsed = ParseExitUtc(out var time);
                _exitDate = parsed ? (DateTime?)time.UtcDateTime.Date : null;
                _exitHour = parsed ? time.ToString("HH", CultureInfo.InvariantCulture) : "00";
                _exitMinute = parsed ? time.ToString("mm", CultureInfo.InvariantCulture) : "00";
                if (parsed) _exitUtc = time.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + ":00";
                Changed(nameof(ExitDate)); Changed(nameof(ExitHour)); Changed(nameof(ExitMinute));
                ExitChanged();
            }
        }
        private void UpdateExitUtc()
        {
            _exitUtc = ExitDate.HasValue ? ExitDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) +
                " " + ExitHour + ":" + ExitMinute + ":00" : "";
            ExitChanged(nameof(ExitUtc));
        }
        public void UseExitTimeNow() => ExitUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + ":00";
        private void ExitChanged([CallerMemberName] string name = null)
        { Changed(name); Changed(nameof(CanSave)); Changed(nameof(ExitValidationMessage)); }
        private bool ParseExitPrice(out double price) => double.TryParse(ExitPrice, NumberStyles.Float,
            CultureInfo.InvariantCulture, out price) && !double.IsNaN(price) && !double.IsInfinity(price);
        private bool ParseExitUtc(out DateTimeOffset time) => DateTimeOffset.TryParseExact(ExitUtc,
            new[] { "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm" }, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out time);
        public string ExitValidationMessage
        {
            get
            {
                if (!IsAccepted) return "";
                if (ExitDecision != "System" && ExitDecision != "Discretionary") return "Choose System or Discretionary.";
                if (!IsDiscretionary) return "";
                if (string.IsNullOrWhiteSpace(ExitReason) || ExitReason.Length > 1000) return "Enter an exit reason (up to 1,000 characters).";
                if (!ParseExitPrice(out var price)) return "Enter a numeric exit price, using a decimal point.";
                if (!ParseExitUtc(out var time)) return "Choose the exit date and time in UTC.";
                if ((Current.EntryTime.HasValue && time < Current.EntryTime.Value) || time > DateTimeOffset.UtcNow)
                    return "Exit UTC must be on/after entry and not in the future.";
                return "";
            }
        }
        public bool IsRejected => Decision?.Code == "rejected";
        public bool IsCustom => IsRejected && Reason?.Code == _customCode;
        public bool IsBusy { get => _busy; private set { _busy = value; Changed(); Changed(nameof(CanEdit)); Changed(nameof(CanSave)); } }
        public bool CanEdit => !IsBusy;
        public bool SaveBlocked { get => _saveBlocked; private set { _saveBlocked = value; Changed(); Changed(nameof(CanSave)); } }
        public bool CanSave => !IsBusy && !SaveBlocked && Decision != null && string.IsNullOrEmpty(ExitValidationMessage) &&
            !string.IsNullOrWhiteSpace(Reviewer) && Reviewer.Trim().Length <= 100 &&
            (!IsRejected || (Reason != null && (!IsCustom ||
                (!string.IsNullOrWhiteSpace(CustomReason) && CustomReason.Trim().Length <= 1000))));
        public string Status { get => _status; private set { _status = value; Changed(); } }

        private void SetCurrent(SystemTradeDataModel row)
        {
            _current = row;
            LatestRow = row;
            Decision = Decisions.FirstOrDefault(d => d.Code == row.Decision);
            Reason = Reasons.FirstOrDefault(r => r.Code == row.RejectionCode);
            CustomReason = row.RejectionCode == _customCode ? row.RejectionReason ?? "" : "";
            ExitDecision = row.ExitDecision == "discretionary" ? "Discretionary" : "System";
            ExitReason = row.DiscretionaryExitReason ?? "";
            ExitPrice = row.DiscretionaryExitPrice?.ToString("G17", CultureInfo.InvariantCulture) ?? "";
            ExitUtc = row.DiscretionaryExitUtc?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "";
            Changed(nameof(Current));
        }

        public async Task<bool> SaveAsync()
        {
            if (!CanSave || _disposed) return false;
            IsBusy = true;
            Status = "Saving review...";
            try
            {
                ParseExitPrice(out var price);
                ParseExitUtc(out var time);
                var row = await _service.SaveReviewAsync(Current, Decision.Code, Reason?.Code,
                    IsCustom ? CustomReason : null, Reviewer, _lifetime.Token,
                    IsAccepted ? ExitDecision.ToLowerInvariant() : null, ExitReason,
                    IsDiscretionary ? (double?)price : null, IsDiscretionary ? (DateTimeOffset?)time : null);
                if (_disposed) return false;
                SetCurrent(row);
                Saved = true;
                Status = "Review saved.";
                return true;
            }
            catch (SystemTradesApiException ex) when (ex.StatusCode == 409)
            {
                await ReconcileAsync("Another reviewer changed this trade. The latest saved review is shown. Inspect it before saving again.");
            }
            catch (SystemTradesApiException ex) when (ex.StatusCode == 400 || ex.StatusCode == 401 || ex.StatusCode == 403)
            {
                if (!_disposed) Status = "Review was not saved: " + ex.Message;
            }
            catch (Exception) when (!_disposed)
            {
                await ReconcileAsync("The save outcome was uncertain. The latest saved review is shown. Check whether your decision was recorded before saving again.");
            }
            finally { if (!_disposed) IsBusy = false; }
            return false;
        }

        public async Task ReloadAsync()
        {
            if (IsBusy || _disposed) return;
            IsBusy = true;
            try { await ReconcileAsync("Latest saved review loaded. Inspect it before saving."); }
            finally { if (!_disposed) IsBusy = false; }
        }

        private async Task ReconcileAsync(string message)
        {
            if (_disposed) return;
            SaveBlocked = true;
            Status = "Reloading the saved review before another save...";
            try
            {
                var row = await _service.FindAsync(Current, _lifetime.Token);
                if (_disposed) return;
                if (row == null)
                {
                    RowMissing = true;
                    Status = "This trade is no longer available. Close this dialog and refresh the trade list.";
                    return;
                }
                SetCurrent(row);
                RowMissing = false;
                SaveBlocked = false;
                Status = message;
            }
            catch (Exception ex)
            {
                if (!_disposed) Status = "Could not reload the saved review: " + ex.Message +
                    " Saving is blocked. Use Reload saved review before trying again.";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void Dispose()
        {
            _disposed = true;
            _lifetime.Cancel();
            _lifetime.Dispose();
        }
    }
}
