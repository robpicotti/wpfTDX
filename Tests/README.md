# System Trades review

Build the Debug solution with Visual Studio or MSBuild. The real application opens
**System Trades** from MainWindow, after **Slippage** in alphabetical order. It
uses the existing TradingApiBaseUrl and TradingApiKey settings. A deployed TAPI
and the registered System Trades tables are required for live data; follow the
Python repository's `trading/docs/SYSTEM_TRADES.md`. Do not enable capture solely
to demonstrate the client.

## Offline checks

From the wpfTDX repository, using the existing Python installation:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tests\check_system_trades.ps1 -BuildDirectory bin\Debug
```

The runner compiles against the built client and starts a synthetic API on an
automatically allocated loopback port. It uses no live API configuration,
databases, Excel or trading services. It stops the fixture process on exit and
reports the local diagnostics directory. Checks cover versioned reviews,
predefined/custom rejection, clearing, validation, pagination, UTC dates,
concurrent edits, uncertain save outcomes and failed reconciliation.

## Demonstration for Bill

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tests\check_system_trades.ps1 -BuildDirectory bin\Debug -Preview
```

This opens the actual System Trades window against synthetic local data. The
title identifies the demo. It does not start MainWindow or read production API
credentials. Close the demo window to stop its local API. Restarting resets the
synthetic decisions.

1. Inspect the trade grid and selected-row qualification evidence.
2. Double-click a trade row (or click **Review selected trade...**). Check the reviewer name and select
   **Accepted**, then **Save review**. The row leaves the default Unreviewed list.
3. Set Review to **All**, then wait for the next automatic refresh to see the saved decision.
4. Open the review again. Select **Rejected**, choose a configured reason and
   save. Repeat with **Other (custom reason)** and enter your own text.
5. Choose **Unreviewed (clear decision)** and save to clear the review. Cancel
   closes an unsaved draft without changing the record.

The reviewer name defaults to the Windows username and can be edited. It is an
operator label in the audit record, not individual authentication. An accepted
review never places an order or changes deployment.

If another reviewer saves first, the dialog reloads their decision and asks you
to inspect it before another explicit save. If a save response fails, the dialog
reloads current state rather than automatically resubmitting. Saving remains
blocked while that reload fails; use **Reload saved review** to recover.

The synthetic demo exercises normal saves. The automated checks simulate
conflicts and failed responses. Live SQL concurrency, RTU capture and production
deployment still require separate validation.

The grid refreshes automatically every 15 seconds. Refresh pauses while a review
dialog is opening or open, then resumes after it closes. Review and lifecycle dropdowns apply immediately. Press Enter in a Fund or
Ticker field to apply those text filters immediately. Double-clicking headers or blank grid
space does not open a review.


## Fund Risk checks

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tests\check_fund_risk.ps1 -BuildDirectory bin\Debug
```

This compiles with the Visual Studio 2022 Roslyn compiler and exercises the actual
Fund Risk window against synthetic in-memory observations. It covers net MTM P&L,
missing-day gaps, signed currency VaR envelopes, date-selected ticker detail,
numeric formatting, category labels and empty/intraday rendering. It does not
contact an API or database. Synthetic chart PNGs are written to a fresh temp folder
(or `-ArtifactDirectory PATH`) for visual review. TAPI must be restarted for live
daily MTM P&L; Python checks are `python -m unittest
trading.test_scripts.webservice.test_fund_risk` from the scripts directory.

Fund Risk loading checks use a gated HTTP handler to verify that risk appears
before slow MTM responses, latest-day P&L renders progressively, filters remain
enabled, missing endpoints stop after one request with a restart instruction,
and cancelled/closed loads cannot replace newer results. No live API is used.


System Trades checks also cover accepted-trade System/Discretionary exit choices,
required reason/price/UTC validation, impossible dates, reload and round-trip,
keeping model exit fields unchanged, and clearing discretionary fields with System.
The backend suite separately checks full audit snapshots, concurrent versions,
audit-write rollback and RTU capture independence. For live upgrades, run the scoped
schema-sync command in trading/docs/SYSTEM_TRADES.md before restarting TAPI.

System Trades gated-API checks verify busy state throughout list/options reads,
review-opening lookups, saves and reloads, plus a queued filter change while
an earlier read is pending. UTC input normalizes all seconds to 00.
