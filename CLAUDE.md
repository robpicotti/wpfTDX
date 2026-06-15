# wpfTDX — Claude Context

C# WPF (.NET Framework, classic `.csproj`) desktop client for the TAD trading system.
Talks to the Python Flask API (`trading/apis/tapi.py`) — **all** SQL access goes through
that webservice; do not add direct ADO.NET/SqlClient for new features, add a route to
`tapi.py` and call it.

Build: `MSBuild.exe wpfTDX.sln /t:Build /p:Configuration=Debug`. If the build fails only
with `MSB3027/MSB3021` "file locked by … wpfTDX", that's the app still running holding
`bin\Debug\wpfTDX.exe` — the code compiled fine; close the running instance and rebuild.

## Filter Intervals screen ("Adjust Intervals")

Lets you customise, per ticker/fund, which intervals are filtered and how positions scale,
then save back to the server. Three files:

- `winFilterIntervals.xaml` — the DataGrid and all columns.
- `winFilterIntervals.xaml.cs` — code-behind; load orchestration, save, and the
  double-click toggle handler (`DataGrid_PreviewMouseDoubleClick` → `TryMapToggleTarget`).
- `ViewModels/FilterIntervalsViewModel.cs` — `MergedTickerRow` (per-row state, all the
  calculations) and the VM (load/merge/save).

Data is loaded from the `/filtered_intervals` endpoint, which returns both the saved
filter flags (`filter_intervals` table) and the latest `tad_positions` snapshot.

### Interval columns + toggling

Each interval column (`t1`, `h2`, `D1`, …) is read-only; you **toggle it by double-clicking**
the cell (cycles null → True → False). The flag means "filtered out": flag **on** =
excluded from the filtered model, flag **off** = included.

A cell is **grey and non-clickable when its `Position…` value is null** — i.e. the ticker
has no position for that interval, so filtering it is moot. This is intentional and applies
to every interval column.

### Base interval columns (`b_t1`, `b_v1`, `b_n1`, `b_y1`, `b_h1`, `b_d1`)

**Six independent columns, one per `filter_intervals` base flag** — same set as the
data model / `FilterIntervalsUpsertRow`. No grouping: each is just its own flag bound to
its own `Base*` / `PositionBase*`, behaving exactly like every other interval column.

| Column | Field | Position |
|---|---|---|
| `b_t1` | `BaseT1` | `PositionBaseT1` |
| `b_v1` | `BaseV1` | `PositionBaseV1` |
| `b_n1` | `BaseN1` | `PositionBaseN1` |
| `b_y1` | `BaseY1` | `PositionBaseY1` |
| `b_h1` | `BaseH1` | `PositionBaseH1` |
| `b_d1` | `BaseD1` | `PositionBaseD1` |

The Python side decides per-ticker which base belongs to each group (e.g. the hourly base
is `base_y1` **or** `base_h1` depending on `trading_hours`), so for any given ticker only
the relevant base columns will carry a position and light up — the rest stay grey. No
app-side grouping/guessing.

> **Gotcha:** the daily base position column is `position_base_D1` (capital D, like the
> regular `position_D1`), not `position_base_d1`. Reading the wrong casing makes `b_d1`
> grey for every ticker.

#### When a base column is toggleable

Like all interval columns, a `b_*` cell is grey + non-clickable when its `PositionBase*`
is null. `position_base_*` is only written to `tad_positions` when that base interval is
**currently holding an open, position-eligible trade** ([tickerposition.py:741-784](../../tenoris/python_dev/scripts/trading/hoover/tickerposition.py#L741-L784)).
For that to ever happen the chain in [ticker.py:2569-2578](../../tenoris/python_dev/scripts/trading/hoover/ticker.py#L2569-L2578) must hold:

1. `tickers.take_position = True` (ticker takes positions at all),
2. the ticker has a base strategy (`strategyname_base` ≠ NONE),
3. that base strategy's row in the `strategies` table has **`take_position = True`**,
4. the base interval is a primetime base for the ticker's `trading_hours`.

Then the value is `±1` only while the base strategy is actually long/short there; flat → null → grey.

### Stats columns (everything after `W2`)

These are position/deployment stats arranged as **three trios** plus a scale multiplier.
Each trio is (net trades, # intervals, deployment). The first two trios are **read-only
server baselines** loaded from `tad_positions`; the third is **recomputed live** as you
toggle (and feeds the colours). Server-side calcs live in
`trading/hoover/tickerposition.py`; live calcs in `MergedTickerRow`.

| Column | Binding | Meaning | Calculation |
|---|---|---|---|
| `trd` | `NumTrades` | net trades, **unfiltered** | longs − shorts over `position_intervals` (`num_trades`) |
| `n` | `NumPositionIntervals` | interval count, unfiltered | side cap `max(len(long/short_pos_intervals),1)` (`num_posintervals`) |
| `dep` | `PositionDeployment` | deployment, unfiltered | `trd ÷ n` (`deployment`) |
| `filt_t` | `NumFilteredTrades` | net trades, **filtered** | longs − shorts over filtered set, buy/sell-only applied (`num_filtered_trades`) |
| `filt_n` | `NumFiltIntervals` | interval count, filtered | filtered-side cap (`num_filtintervals`) |
| `f_dep` | `FilteredDeployment` | deployment, filtered | `filt_t ÷ filt_n` — **sizes real orders for customised funds** (`filtered_deployment`) |
| `new_t` | `NewTrades` | net trades, **live what-if** | `RecalcNewTrades`: Σ positions of un-filtered intervals (buy/sell-only); 0 if `filt_all` on |
| `new_n` | `RescaledIntervals` | interval count, live | `RecalcRescaledIntervals`: `n − filtered-out` (clamp 1..n) if `rescale`, else `n` |
| `n_dep` | `NewDeployment` | deployment, live | `RecalcNewDeployment`: `new_t ÷ new_n`, long/short clamped |
| `s_dep` | `ScaledDeployment` | scaled deployment, live | `ReCalcScaledDeployment`: `n_dep × scale factor` (`new_f` else `scale_f` else 1) |

Key relationships:
- `dep = trd ÷ n`, `f_dep = filt_t ÷ filt_n`, `n_dep = new_t ÷ new_n`.
- The **live trio is the what-if of the filtered trio**: `new_t`/`new_n`/`n_dep` are what
  `filt_t`/`filt_n`/`f_dep` *become after you save*. With no edits they coincide.
- `trd`/`n`/`dep` are the full-model reference; **toggles never change them** (only a
  server reprocess after save updates them, then a reload).
- **`rescale` flag** controls `new_n`: on → filtering an interval out shrinks the
  denominator (deployment redistributes onto survivors); off → denominator stays at `n`.
- Live-column **colours** compare to the saved baseline: `new_t`↔`filt_t`,
  `new_n`↔`filt_n`, `n_dep`↔`f_dep` (pale green = higher, misty rose = lower).

Every column after `W2` has a **header tooltip** documenting the above — hover the column
title. (The `b_*` base columns do not have tooltips.)

### Column order & collapsible group headers

**On-screen column order is set in code, not by the XAML order.** `ApplyColumnOrder()`
(called from `FilterGrid_Loaded`) assigns each column's `DisplayIndex` from the
`DesiredColumnOrder` array — Bill's layout: `ticker, fundgrp, fund, strategy, strategy_b,
m__int, p__int, c__upd, manual, rescale…filt_all, trd…n_dep, scale_f, new_f, s_dep, pos_lim,
pos_tgt, s_pos_lim,` then **all intervals at the far right** (`b_t1…b_d1`, then `t1…W2`). To change the
on-screen order, edit `DesiredColumnOrder` (not the XAML block order). If you add a column,
add its header to that array or it'll fall to the end.

The interval columns' underlying order (in the XAML Columns collection and within
`DesiredColumnOrder`) is **chronological (duration)**, e.g. `… h16, D1, h36, D2, D3, D4, W1,
D8, W2 …` — `h36` (36h) between `D1` (24h) and `D2` (48h), `W1` (1wk) before `D8` (8d).
Intentional; don't "tidy" it into per-letter family blocks.

Two **collapsible group bands** (Excel-style outline brackets) sit in a row above the grid,
generated in code by `BuildColumnGroups()` ([winFilterIntervals.xaml.cs](winFilterIntervals.xaml.cs)):
- **`base`** → the `b_*` columns.
- **`daily/weekly`** → the contiguous chronological `D1 → W2` block (`D1, h36, D2, D3, D4,
  W1, D8, W2`).

Shorter families (`t/v/n/y/h`) intentionally have **no** collapser. How it works:
- No placeholder columns — collapsing sets the member columns to `Visibility.Collapsed`
  (zero width, grid closes up); the collapsed band becomes a small floating **`+`** marker
  at the group boundary (in the `GroupHeaderCanvas` outline row), packed so adjacent markers
  don't overlap. Expanded shows a bracket + `−` chip + label.
- Bands are **measured against live column headers** and repositioned on scroll / layout, so
  `EnableColumnVirtualization="False"` is required (headers must always be realisable).
- A bracket spans the **first-visible to last-visible** column of its group, so it appears
  whenever *any* member column is on screen (the min-`p_int` pass may hide part of a group).
- Band `Border.Background` is `null` (not `Transparent`) so empty band area isn't
  hit-testable and doesn't swallow clicks meant for an overlapping `+`; collapsed markers
  also get a higher `Panel.ZIndex`.
- Expanding re-applies `UpdateIntervalColumnVisibility()` (min-`p_int` baseline); that method
  also calls `ReapplyCollapsedGroups()` so a reload doesn't re-show a user-collapsed group.

### Strategy columns (`strategy`, `strategy_b`) — editable dropdowns

Two editable ComboBox columns backed by `strategies_override`:
- **`strategy`** ↔ `StrategyName` ↔ `strategies_override.strategyname`.
- **`strategy_b`** ↔ `StrategyNameBase` ↔ `strategies_override.strategyname_base` (the base strategy).

Each has its own `Original…`/`…HasChanged` tracking, khaki "changed" highlight, and is wired
through `AttachStrategyOverride` (baseline), `ToStrategyOverrideInsertModel` (save payload —
sends `this.StrategyName` / `this.StrategyNameBase`), the clone/snapshot/preserve paths, and
the save triggers (`affectedStrategyTickers`, the override-payload `.Where`, `HasStrategyEdit`).
Server-side the upsert maps both into `strategies_override` ([tapi.py](trading/apis/tapi.py) `upsert_filter_intervals` job).

Dropdown lists come from **one endpoint**, `/get_strategynames`, which takes an optional JSON
body `{"interval_type": "<type>"}`:
- `LoadStrategyNamesAsync` posts `{}` → all manual strategies (`where {manual:1}`).
- `LoadStrategyNamesBaseAsync` posts `{"interval_type":"base"}` → `where {manual:1, interval_type:"base"}`.
`"default"` is always prepended, so the dropdown is never empty.

### Position columns (`pos_lim`, `pos_tgt`, `s_pos_lim`) — read-only

`get_filtered_intervals` merges `target_positions` (latest, `sub_tickername=="NONE"`, deduped to
one row per `(tickername, fundname)`) onto the filter rows **on `(tickername, fundname)`** and
returns a `positionlimits` section keyed by ticker (`position_limit`, `position_target`,
`scaled_position_limit`). The WPF parses it into `TadPositionsDataModel` and surfaces read-only
`PositionLimit` / `PositionTarget` / `ScaledPositionLimit` on the row → grid columns
`pos_lim` / `pos_tgt` / `s_pos_lim` (N0 — rounded to whole numbers, right-aligned). `pos_tgt`
sits immediately after `pos_lim`. A wildcard (`*`) `fundname` in `filter_intervals` won't match a
real fund → cells are blank (intended).

### Scaled-positions join (`scale_f` / `new_f`) — wildcard-aware

`get_filtered_intervals` returns a `scaledpositions` section: every `scaled_positions` row with
`scaled_type='filtered'` (and not a trivial 1.0/1.0) whose `tickername` is in the requested set,
passed through verbatim — including rows whose `fundname` and/or `fundgroupname` are `'*'`
(blanket rules). It does **not** pre-resolve wildcards.

The match to a concrete filter row happens in `RebuildMerged()`
([FilterIntervalsViewModel.cs](ViewModels/FilterIntervalsViewModel.cs)) via the local
`FindScaled(ticker, fundgroup, fundname)`. It is **wildcard-aware**: a scaled row matches when,
for each of ticker/fundgroup/fundname, the scaled value equals the row's value **or is `'*'`**
(mirrors `Scale.load()`'s `fund IN ('*', fund)` semantics). When several rows match it picks the
**most specific** (exact fund > exact group > exact ticker, scored 4/2/1). The matched
`scaled_target` → `ScaleFactor` (`scale_f`), `scaled_percent` → `ScaledPercent` (`new_f`).
Applied at both join sites (TAD-positions rows and FI-only rows).

⚠️ Don't revert this to an exact composite-key dictionary lookup (`ticker|group|fund`): that was
the old bug — a `fundname='*'` scaling silently failed to appear on a fund-specific filter row.

## Ticker Freezer screen (`ucTickerFreezer`)

Loaded into MainWindow on connect (`EnsureTickerFreezerLoaded`). Has a manual **Refresh**
button (bound to `RefreshCommand` → `viewmodel.Refresh()` → `ProcessTickerFreezer`, async
HTTP to `/get_unresolved_tickerfreezer`).

**Auto-refresh:** a dedicated `DispatcherTimer` in `ucTickerFreezer.xaml.cs` calls the same
`viewmodel.Refresh()` on an interval. Interval = `TickerFreezerRefreshSeconds` app setting
(default 60s; 0 disables). Started after the initial load, stopped on `Unloaded`. It's
`async`/non-blocking with a re-entrancy guard, and swallows transient errors (the manual
button still surfaces them). Note: the refresh mutates a bound `ObservableCollection`, so
the bind-update must run on the UI thread — only the HTTP wait is off-thread; do **not**
wrap it in `Task.Run` without `BindingOperations.EnableCollectionSynchronization`.

There is **no shared app timer**: the process monitor boxes (`ucProcess`) each create their
own `DispatcherTimer` at 5-min intervals (`ucProcess.xaml.cs`) with a *synchronous* refresh,
which is why the freezer needed its own (better, async) timer rather than hooking an
existing one.

The far-right header label is **"Refresh time:"** and shows `LastRunTime` (stamped on every
refresh, manual or auto).

### Chart win/loss labels (related, on the Python side)

The plotly chart JSON (`trading/hoover/plotdata.py::extract_chart_data`, mirrored in
`trading/test_scripts/test_plotly_chart.py`) labels trade-exit markers WINNING/LOSING by
pairing each exit with its **true entry**, tracked over full history (not the previous
visible marker). This also drives the arrow colour and the dashed connector.
