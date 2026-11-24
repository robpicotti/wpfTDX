using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Windows;
using Newtonsoft.Json.Linq;
using wpfTDX;

internal static class SystemTradesClientChecks
{
    private static string _url;
    private static int _checks;
    private static void Check(bool pass, string message)
    {
        if (!pass) throw new Exception(message);
        _checks++;
    }
    private static SystemTradesService Service(string mode)
    { return new SystemTradesService(_url + mode + "/", "synthetic-test-key"); }
    private static int Writes(string mode)
    {
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("X-Api-Key", "synthetic-test-key");
            var result = client.PostAsync(_url + mode + "/stats", new StringContent("{}")).GetAwaiter().GetResult();
            return (int)JObject.Parse(result.Content.ReadAsStringAsync().GetAwaiter().GetResult())["writes"];
        }
    }
    private static void Release(string mode)
    {
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("X-Api-Key", "synthetic-test-key");
            client.PostAsync(_url + mode + "/release", new StringContent("{}"))
                .GetAwaiter().GetResult().EnsureSuccessStatusCode();
        }
    }
    private static SystemTradeReviewViewModel Review(SystemTradesService service)
    {
        var options = service.OptionsAsync(CancellationToken.None).GetAwaiter().GetResult();
        var row = service.LoadAsync("TenDSelect", "ES1", "All", "All", CancellationToken.None).GetAwaiter().GetResult().Single();
        return new SystemTradeReviewViewModel(service, row, options, "Test reviewer");
    }
    private static void Decision(SystemTradeReviewViewModel vm, string code)
    { vm.Decision = vm.Decisions.Single(d => d.Code == code); }
    private static bool Save(SystemTradeReviewViewModel vm) { return vm.SaveAsync().GetAwaiter().GetResult(); }

    [STAThread]
    public static int Main(string[] args)
    {
        _url = args[0].TrimEnd('/') + "/";
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-GB");
        try
        {
            if (args.Length > 1 && args[1] == "--preview")
            {
                var app = new Application();
                var vm = new SystemTradesViewModel(Service("demo"));
                app.Run(new winSystemTrades(vm) { Title = "System Trades - synthetic demo (local only)" });
                return 0;
            }
            using (var service = Service("pagination"))
            {
                var rows = service.LoadAsync("TenDSelect", "", "Unreviewed", "All", CancellationToken.None).GetAwaiter().GetResult();
                Check(rows.Count == 501, "Pagination must deduplicate IDs across page boundaries");
                Check(rows[0].EntryUtc == new DateTime(2026, 11, 13, 14, 5, 0), "UTC dates must parse under en-GB");
                Check(rows[0].ExitUtc == null && rows[0].ReviewLabel == "Unreviewed", "Null lifecycle and review");
                Check(rows[0].QualificationText.Contains("1 = 1"), "Recorded admission evidence");
            }
            using (var service = Service("workflow"))
            using (var vm = Review(service))
            {
                vm.Reviewer = " ";
                Check(!vm.CanSave, "Blank reviewer must block save");
                vm.Reviewer = "Test reviewer";
                Decision(vm, "rejected");
                Check(!vm.CanSave, "Rejection requires a reason");
                vm.Reason = vm.Reasons.Single(r => r.Code == "custom");
                Check(!vm.CanSave, "Custom rejection requires text");
                vm.CustomReason = new string('x', 1001);
                Check(!vm.CanSave, "Custom reason length must be bounded");
                vm.CustomReason = "Custom test reason";
                Check(Save(vm) && vm.Current.DecisionVersion == 1, "Custom rejection must save once");
                Check(vm.Current.RejectionReason == "Custom test reason", "Server's returned row is authoritative");
                vm.Reason = vm.Reasons.Single(r => r.Code == "weekly");
                Check(Save(vm) && vm.Current.RejectionReason == "against weekly trend", "Predefined reason uses server label");
                Decision(vm, "accepted");
                Check(Save(vm) && vm.Current.RejectionCode == null && vm.Current.RejectionReason == null, "Accept clears rejection fields");
                Decision(vm, null);
                Check(Save(vm) && vm.Current.Decision == null && vm.Current.DecisionVersion == 4, "Clear sends actual null");
            }
            using (var service = Service("exit-log"))
            using (var vm = Review(service))
            {
                Decision(vm, "accepted");
                Check(Save(vm) && vm.Current.ExitDecision == "system", "Accepted trades default to system exit");
                vm.ExitDecision = "Discretionary";
                Check(!vm.CanSave && vm.IsDiscretionary, "Discretionary exit requires complete fields");
                vm.ExitReason = "Take profit ahead of the model";
                vm.ExitPrice = "NaN"; vm.ExitUtc = "2026-11-13 15:30:00";
                Check(!vm.CanSave, "Discretionary price must be finite");
                vm.ExitPrice = "125.75"; vm.ExitUtc = "2026-11-12 15:30:00";
                Check(!vm.CanSave, "Discretionary exit cannot precede entry");
                vm.ExitUtc = "2099-11-13 15:30:00";
                Check(!vm.CanSave, "Discretionary exit cannot be in the future");
                vm.ExitDate = null;
                Check(!vm.CanSave, "Date picker requires a date");
                vm.UseExitTimeNow();
                Check(vm.ExitDate == DateTime.UtcNow.Date && vm.CanSave, "Use now fills a valid UTC date and time");
                vm.ExitDate = new DateTime(2026, 11, 13); vm.ExitHour = "15"; vm.ExitMinute = "30";
                Check(vm.ExitUtc == "2026-11-13 15:30:00", "Calendar/time selections compose UTC without local conversion");
                vm.ExitUtc = "2026-11-13 15:30:37";
                Check(vm.ExitUtc == "2026-11-13 15:30:00", "Seconds are always normalized to 00");
                Check(vm.CanSave && Save(vm), "Complete discretionary exit saves");
                Check(vm.Current.DiscretionaryExitPrice == 125.75 &&
                    vm.Current.DiscretionaryExitUtc == new DateTime(2026, 11, 13, 15, 30, 0), "Discretionary exit round-trips in UTC");
                Check(vm.Current.ExitPrice == null && vm.Current.ExitTime == null, "Discretionary save does not invent a model exit");
                vm.ReloadAsync().GetAwaiter().GetResult();
                Check(vm.ExitDecision == "Discretionary" && vm.ExitReason == "Take profit ahead of the model" && vm.ExitUtc.EndsWith(":00"), "Reload restores saved exit decision and reason");
                vm.ExitDecision = "System";
                Check(Save(vm) && vm.Current.DiscretionaryExitPrice == null && vm.Current.ExitDecision == "system", "System choice clears discretionary details");
            }
            using (var service = Service("conflict"))
            using (var vm = Review(service))
            {
                Decision(vm, "accepted");
                Check(!Save(vm) && Writes("conflict") == 1, "409 must never auto-resubmit");
                Check(vm.Current.DecisionBy == "Another reviewer" && vm.Current.DecisionVersion == 1, "409 must reload latest saved review");
                Check(vm.Decision.Code == "rejected" && !vm.SaveBlocked, "Conflict draft resets to server state for inspection");
                Decision(vm, "accepted");
                Check(Save(vm) && vm.Current.DecisionVersion == 2, "Explicit second save uses reloaded version");
            }
            using (var service = Service("uncertain"))
            using (var vm = Review(service))
            {
                Decision(vm, "accepted");
                Check(!Save(vm) && Writes("uncertain") == 1, "Uncertain save must not auto-resubmit");
                Check(vm.Current.Decision == "accepted" && vm.Current.DecisionVersion == 1, "Reconcile a committed save after failed response");
                Check(vm.Status.Contains("uncertain"), "User must see uncertain-save explanation");
            }
            using (var service = Service("blocked"))
            using (var vm = Review(service))
            {
                Decision(vm, "accepted");
                Check(!Save(vm) && vm.SaveBlocked && !vm.CanSave, "Failed reconciliation must block saving");
                Check(!Save(vm) && Writes("blocked") == 1, "Blocked retries must not write");
                vm.ReloadAsync().GetAwaiter().GetResult();
                Check(!vm.SaveBlocked && vm.Current.DecisionVersion == 1, "Explicit reload restores verified state");
            }
            using (var service = Service("invalid"))
            using (var vm = Review(service))
            {
                Decision(vm, "accepted");
                Check(!Save(vm) && vm.Decision.Code == "accepted" && vm.Status.Contains("synthetic validation error"), "400 preserves draft and reports validation");
            }
            using (var service = Service("missing"))
            using (var vm = Review(service))
            {
                Decision(vm, "accepted");
                Check(!Save(vm) && vm.RowMissing && !vm.CanSave, "Missing trade must not remain editable");
            }
            using (var vm = new SystemTradesViewModel(Service("grid")))
            {
                vm.RefreshAsync().GetAwaiter().GetResult();
                using (var review = vm.BeginReviewAsync().GetAwaiter().GetResult())
                {
                    review.Reviewer = "Test reviewer";
                    Decision(review, "accepted");
                    Check(Save(review), "Grid review save");
                    vm.ApplyReviewResult(review);
                    Check(vm.Rows.Count == 0 && !vm.HasSelection && vm.IsEmpty, "Accepted row leaves applied unreviewed filter");
                }
            }
            using (var vm = new SystemTradesViewModel(Service("refresh")))
            {
                vm.RefreshAsync().GetAwaiter().GetResult();
                var selected = vm.SelectedTrade;
                Check(selected != null && vm.Rows.Count == 1, "Initial refresh loads trade");
                vm.RefreshAsync().GetAwaiter().GetResult();
                Check(vm.Rows.Count == 1 && vm.SelectedTrade == selected, "Failed background refresh preserves rows and selection");
                Check(vm.Status.Contains("Previous results are retained"), "Failed refresh clearly identifies retained results");
                vm.RefreshAsync().GetAwaiter().GetResult();
                Check(vm.Rows.Count == 1 && vm.SelectedTrade.SystemTradeId == selected.SystemTradeId,
                    "Next refresh recovers and preserves selected trade identity");
            }
            using (var vm = new SystemTradesViewModel(Service("filter-progress")))
            {
                var pending = vm.RefreshAsync();
                Check(vm.IsLoading && !vm.CanReview, "List/capture lookups expose progress until the API completes");
                vm.ReviewFilter = "Accepted";
                vm.ApplyFiltersAsync().GetAwaiter().GetResult();
                Check(vm.IsLoading && vm.Status.Contains("Applying selected filters"), "Filter changes show progress while an earlier read is pending");
                Release("filter-progress");
                pending.GetAwaiter().GetResult();
                Check(!vm.IsLoading && vm.Rows.Count == 0, "Queued Accepted filter completes immediately after the pending request");
            }
            using (var vm = new SystemTradesViewModel(Service("review-open-progress")))
            {
                vm.RefreshAsync().GetAwaiter().GetResult();
                var pending = vm.BeginReviewAsync();
                Check(vm.IsLoading && vm.Status.Contains("Loading trade details"), "Opening a review exposes progress through options and trade lookup");
                Release("review-open-progress");
                using (var review = pending.GetAwaiter().GetResult())
                    Check(review != null && !vm.IsLoading, "Review opening clears progress after the lookup");
            }
            using (var service = Service("save-progress"))
            using (var vm = Review(service))
            {
                Decision(vm, "accepted");
                var pending = vm.SaveAsync();
                Check(vm.IsBusy && !vm.CanEdit, "Saving exposes modal progress and prevents concurrent edits");
                Release("save-progress");
                Check(pending.GetAwaiter().GetResult() && !vm.IsBusy, "Save completion clears progress");
            }
            using (var service = Service("reload-progress"))
            using (var vm = Review(service))
            {
                var pending = vm.ReloadAsync();
                Check(vm.IsBusy && !vm.CanEdit, "Reloading exposes modal progress");
                Release("reload-progress");
                pending.GetAwaiter().GetResult();
                Check(!vm.IsBusy && vm.CanEdit, "Reload completion clears progress");
            }
            Console.WriteLine("PASS: " + _checks + " offline client checks (synthetic loopback API).");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
