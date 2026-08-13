# wpfTDX — Claude Context

C# WPF (.NET Framework, classic `.csproj`) desktop client for the TAD trading system.
Talks to the Python Flask API (`trading/apis/tapi.py`) — **all** SQL access goes through
that webservice; do not add direct ADO.NET/SqlClient for new features, add a route to
`tapi.py` and call it.

Build: `MSBuild.exe wpfTDX.sln /t:Build /p:Configuration=Debug`. If the build fails only
with `MSB3027/MSB3021` "file locked by … wpfTDX", that's the app still running holding
`bin\Debug\wpfTDX.exe` — the code compiled fine; close the running instance and rebuild.

## RTU Status screen (`winTickerStatusMonitor`)

Live per-ticker `run_ticker_update` progress, opened from MainWindow's **"RTU Status"**. Polls
`POST /ticker_process_status` (10s auto-refresh) — no direct SQL. `TickerProcessStatusModel` maps the
route's per-ticker object (Tickername is the map key, assigned on build). The DataGrid is coloured by
`Status` (queued/running/done/failed/timeout) and frozen on the ticker column; a **client-side status
filter** (`Show:` dropdown) narrows the grid without re-fetching, while the summary keeps the full
counts. `stale (min)` = `UtcNow − last_processed` (last successful `tad_positions` runtime).
Backed by the Python `ticker_process_status` feature — see the trading repo's CLAUDE.md
"RTU per-ticker status monitor".

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

### Market-announcement highlight (BUILT 2026-07-08)

Tickers with a market-moving announcement still to come **this week** get an amber left **stripe**
on the ticker cell + a row **tooltip** naming the event(s)/time. Backed by the trading repo's
announcements feed (`Calendar.Events`); design note in trading `CLAUDE.md` → "Market announcements feed".

- **Data**: `/filtered_intervals` returns an extra `event_affected` section
  `{ ticker: [ {title, event_time_utc, importance, categories} ] }`. `PopulateEventAffectedFromJson`
  parses it into `_eventTooltipByTicker`; `ApplyEventAffected` (called at the end of `RebuildMerged`)
  stamps `MergedTickerRow.IsEventAffected` + `EventTooltip`.
- **View**: `winFilterIntervals.xaml` — ticker column `CellStyle` `DataTrigger` on `IsEventAffected`
  (amber `#FFB300` left border, kept distinct from the scale/deployment colouring); row `ToolTip`
  bound to `EventTooltip`.
- **Toggle**: an **Events: High/Medium/Low** ComboBox on the toolbar (default High) → 
  `EventImportanceCombo_SelectionChanged` → `FilterIntervalsViewModel.RefreshEventAffectedAsync()`,
  which POSTs the on-screen tickers + importance to the lightweight **`/event_affected`** endpoint and
  re-stamps rows (no full reload). Note: Crude Oil Inventories is *Medium* — set the toggle to
  Medium/Low to see it stripe.

### Interval columns + toggling

Each interval column (`t1`, `h2`, `D1`, …) is read-only; you **toggle it by double-clicking**
the cell (cycles null → True → False). The flag means "filtered out": flag **on** =
excluded from the filtered model, flag **off** = included.

A cell is **grey and non-clickable when its `Position…` value is null** — i.e. the ticker
has no position for that interval, so filtering it is moot. This is intentional and applies
to every interval column.

The double-click maps a column → `MergedTickerRow` property via `TryMapToggleTarget` (a switch on
the **real Header code**, not the displayed label). **Every interval code must have a case there**
or that column silently won't toggle. Gotcha: the duration relabel shows `y4…y72` as `h2…h36`, so a
missing `y*` case looks like "the h columns don't toggle" (the 2026-06 bug — `v4/v6/v8/y4/y6/y8/y12/y24/y32/y72`
had been added as columns but not to `TryMapToggleTarget`). Note `TryMap` (a second, similar switch)
is dead code — the live path is `TryMapToggleTarget`.

### Base interval columns (`b_t1`, `b_v1`, `b_n1`, `b_y1`, `b_h1`, `b_d1`, `b_w1`)

**Independent columns, one per `filter_intervals` base flag** — same set as the
data model / `FilterIntervalsUpsertRow`. No grouping: each is just its own flag bound to
its own `Base*` / `PositionBase*`, behaving exactly like every other interval column. The
**active** bases are `b_t1, b_v1, b_y1, b_d1, b_w1` (`b_n1`/`b_h1` are retired/collapsed —
see the 2026-06 change note below).

| Column | Field | Position |
|---|---|---|
| `b_t1` | `BaseT1` | `PositionBaseT1` |
| `b_v1` | `BaseV1` | `PositionBaseV1` |
| `b_n1` | `BaseN1` | `PositionBaseN1` |
| `b_y1` | `BaseY1` | `PositionBaseY1` |
| `b_h1` | `BaseH1` | `PositionBaseH1` |
| `b_d1` | `BaseD1` | `PositionBaseD1` |
| `b_w1` | `BaseW1` | `PositionBaseW1` |

`base_W1` (weekly) was added 2026-07 — it's a *derived* base in the model (`base_W1 ← base_D1`
at the W1 view, `Interval.parameters["BASE_DERIVED"]`) and slots as the **5th active base
after `b_d1`**, so it's the last base column and now carries the **base│intraday group
separator** (dark right border moved off `b_d1` onto `b_w1`). It's wired everywhere the other
bases are (flag/Position/Original/Brush/HasChanged on `MergedTickerRow`, `FilterIntervalsDataModel`,
`TadPositionsDataModel`, `FilterIntervalsUpsertRow`, both merge loops, parse, counts
[`CountBaseGroup`/`CountCurrentFiltered`/`CountBaselineFiltered`/`CountFiOnly*`], `RecalcNewTrades`,
`ToUpsertRow` [static + `PickForSave`], `DesiredColumnOrder`, base group band, `TryMapToggleTarget`,
XAML) — so it participates in the trade/deployment calcs exactly like `b_d1`.

The Python side decides per-ticker which base belongs to each group (e.g. the hourly base
is `base_y1` **or** `base_h1` depending on `trading_hours`), so for any given ticker only
the relevant base columns will carry a position and light up — the rest stay grey. No
app-side grouping/guessing.

> **Gotcha:** the daily/weekly base position columns are `position_base_D1` / `position_base_W1`
> (capital D/W, like the regular `position_D1`/`position_W1`), not `position_base_d1`/`_w1`.
> Reading the wrong casing makes `b_d1`/`b_w1` grey for every ticker.

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
| `new_n` | `RescaledIntervals` | interval count, live | `RecalcRescaledIntervals`: **rescale=true** → `n − filtered-out` (clamp **0..n**, all-filtered row shows 0). **rescale=false** → per-group full count: `baseTotal (iff ≥1 base active) + nonBaseTotal (iff ≥1 non-base active)`, where a group's total = its position-interval count and "active" = `flag==false` (via `CountBaseGroup`/`CountNonBaseGroup`/`Tally`). i.e. no rescale keeps each group's whole interval count as long as that group has any unfiltered interval, else 0 |
| `n_dep` | `NewDeployment` | deployment, live | `RecalcNewDeployment`: `new_t ÷ max(new_n, 1)`, long/short clamped — divisor clamped to ≥1 so `new_n = 0` doesn't divide by zero (`new_n` itself is still displayed as 0) |
| `s_dep` | `ScaledDeployment` | scaled deployment, live | `ReCalcScaledDeployment`: `n_dep × scale factor` (`new_f` else `scale_f` else 1). **Hidden on screen** (`Visibility="Collapsed"`, out of `DesiredColumnOrder`) — VM/calc kept, so re-showing = remove the collapse + re-add to the order array |

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
`DesiredColumnOrder` array — Bill's layout: `ticker, category, fundgrp, fund, strategy, strategy_b,
m__int, p__int, c__upd, manual, rescale…filt_all, trd…n_dep, scale_f, new_f, pos_lim,
s_pos_lim, pos_tgt,` then **all intervals at the far right** (`b_t1…b_w1`, then `t1…W2`). `s_dep` is
`Visibility="Collapsed"` (hidden on screen, ViewModel kept) and dropped from this array. To change the
on-screen order, edit `DesiredColumnOrder` (not the XAML block order). If you add a column,
add its header to that array or it'll fall to the end.

**`ticker` is frozen (Excel-style).** The DataGrid sets `FrozenColumnCount="1"`, so the first
display column stays pinned when scrolling horizontally. `ticker` leads `DesiredColumnOrder`
(so `ApplyColumnOrder` gives it `DisplayIndex=0`), which is what makes it the frozen one. If you
reorder, keep whatever should stay pinned first in the array, or the wrong column will freeze.

**`category` column + row sort.** `category` is ticker metadata from the `tickers` table.
`get_filtered_intervals` does **not** merge `tickers` (deliberately — avoids an extra per-load
query); instead the WPF reuses the already-loaded `TickerUniverse` (from `LoadTickerUniverseAsync`,
which added a `category` field to `TickerRow`). `RebuildMerged` builds a `tickername→category`
map from it and sets `MergedTickerRow.Category`, then **sorts the grid by `fundgroup → fundname →
category → tickername`** (category groups within each fund/fundgroup, before ticker). The
`category` column is read-only display.
If a filter ticker isn't in the universe (e.g. `valid_tickername=0`) its category is blank.

The interval columns' underlying order (in the XAML Columns collection and within
`DesiredColumnOrder`) is **chronological (duration)**, e.g. `… v8, y2, y3, y4, y6, y8, y12,
y24, y32, D1, y72, D2, D3, D4, W1, D8, W2` — `y72` (36h) between `D1` (24h) and `D2` (48h),
`W1` (1wk) before `D8` (8d). Intentional; don't "tidy" it into per-letter family blocks.

#### 2026-06 interval-set change (Bill)
The `filter_intervals` table schema changed: the **`n` and `h` interval families were retired**
and replaced with **extended `v` (`v4 v6 v8`) and `y` (`y4 y6 y8 y12 y24 y32 y72`) families**;
bases dropped from 6 to **4** (`base_t1 base_v1 base_y1 base_D1` — no `base_n1`/`base_h1`).
The live set is whatever `Interval.filter(groupname="trading")` emits server-side
(`FILTER_INTV_EXPECTED_COLS` in [tapi.py](trading/apis/tapi.py)).
- **New columns are fully wired** (flag/Position/Original/Brush/HasChanged on `MergedTickerRow`,
  `FilterIntervalsDataModel`, `TadPositionsDataModel`, `FilterIntervalsUpsertRow`, both merge
  loops, parse, counts, deployment, `ToUpsertRow`, `IntervalOrder`, `DesiredColumnOrder`, XAML).
- **Retired columns (`n2 n3 n4 h2 h3 h4 h6 h12 h16 h36 b_n1 b_h1`) are only hidden, not deleted:**
  their XAML columns carry `Visibility="Collapsed"` and their full ViewModel/model wiring is kept,
  so reverting = remove the `Visibility="Collapsed"` and restore them to `IntervalOrder`/
  `DesiredColumnOrder`. They're absent from `IntervalOrder`, so `UpdateIntervalColumnVisibility`
  (which `continue`s on non-interval-order headers) won't re-show them.
- `GetMinVisibleInterval`'s all-default fallback is now `"v6"` (was `"n3"`, which no longer exists).

Three **collapsible group bands** (Excel-style outline brackets) sit in a row above the grid,
generated in code by `BuildColumnGroups()` ([winFilterIntervals.xaml.cs](winFilterIntervals.xaml.cs)):
- **`base`** → the `b_*` columns (`b_t1, b_v1, b_y1, b_d1, b_w1`).
- **`intraday`** → the contiguous `t1 → y12` block (all sub-daily trading intervals:
  `t1 t2 t3 t4 t5 t8 v2 v3 v4 v6 v8 y2 y3 y4 y6 y8 y12`).
- **`daily/weekly`** → the contiguous `y24 → W2` block (`y24, y32, D1, y72, D2, D3, D4, W1,
  D8, W2`) — starts at `y24`; `y72` (36h) sits between D1 and D2, `W1` before `D8`.

Each group's `Headers` array MUST be in **display (chronological) order** — the bracket spans the
first-visible to last-visible entry, and the collapsed `+` marker is placed at `Last`'s boundary.
Groups must be contiguous in `DesiredColumnOrder` (base, then intraday, then daily/weekly).

**`scale_f` highlight.** The `scale_f` cell shows a very light orange background
(`ScaleFactorBrush` → `BgLightOrange` `#FFE8CC`) when a scale factor is present and ≠ 1 (the
ticker is actually being scaled); blank otherwise. The `NewScaleFactorHasChanged` edit trigger
(Yellow + black border) overrides it while `new_f` is being edited. Distinct from the manual
column (which uses a border highlight, not a fill).

**Stat-triplet colouring.** live (`trd/n/dep`) and filtered (`filt_t/filt_n/f_dep`) cells get a
**diverging background** — dark red (−100%) → white (0) → dark green (+100%), intensity by
magnitude — driven by that triplet's deployment (`PositionDeployment` / `FilteredDeployment`); all
three cells of a triplet share the one value, so the whole triplet reads as one block.
`DivergeColor`/`DeploymentDivergeBrush` compute it; `DeploymentForeground` flips text to white on
dark backgrounds (luminance < 140). Proposed (`new_t/new_n/n_dep`) all bind one
**`ProposedChangeBrush`**: light green if the proposed deployment is more bullish than the saved
`FilteredDeployment`, light red if more bearish, transparent if unchanged. (Previously each
proposed cell used a different brush comparing a different metric — trades vs intervals vs
deployment — so they disagreed; unified to the deployment delta.) `NewDeployment`'s setter
notifies `ProposedChangeBrush`; the live/filtered brushes read load-time values and need no notify.

**Non-collapsible label bands (`live` / `filtered` / `proposed`).** The three stat triplets are
grouped by a labelled band (same bracket style, **no toggle**) via `AddLabelBand(...)` in
`BuildColumnGroups`: `live` over `trd/n/dep`, `filtered` over `filt_t/filt_n/f_dep`, `proposed`
over `new_t/new_n/n_dep`. A label band is just a `ColumnGroup` with `LabelOnly=true` / always
`Expanded` (positioned by `PositionExpandedBand`, never collapses). The columns keep their
**distinct Header codes** (so `DesiredColumnOrder`, tooltips, bindings are unchanged), but the
filtered/proposed six are **relabelled display-only** to `trd`/`n`/`dep` via a `ContentTemplate`
`<TextBlock Text="…"/>` on each column's `HeaderStyle`. So all three triplets read `trd/n/dep`
and the band says which model. Don't collapse these to shared Header values (would duplicate keys).

**Interval column headers are relabelled by duration for display only.** `IntervalHeaderLabelConverter`
(applied in the `DataGrid.ColumnHeaderStyle` `ContentTemplate`) maps the interval *code* shown in
the header to a duration label: ≤ 90 min → the minute count (`t1→1 … v8→40, y2→60, y3→90`), the
hour range → `hN` (`y4→h2, y6→h3, y8→h4, y12→h6, y24→h12, y32→h16, y72→h36`); `D*`/`W*`, the base
`b_*` columns, and every non-interval header pass through unchanged. This is **display only** — each
column's real `Header` stays the interval code, so `ApplyColumnOrder`, the collapser groups,
`IntervalOrder` and column-visibility all still key off the code (`byText` in `PositionGroupBands`
reads `Column.Header`, not the rendered text). Don't rename the actual `Header` values.

**Interval cell text is inverted for display only.** All interval/base columns bind their text
through `FlagTextOrBlankConverter` (`TrueText=""`, `FalseText="ON"` in the XAML resource): an
**active** interval (`flag == false`, i.e. not filtered out) shows **`ON`**, a **filtered-out**
one (`flag == true`) shows **blank**, and a cell with no position stays blank. This is display
only — the columns are read-only and saving uses the real bool values via `ToUpsertRow`, so the
stored `true`/`false` semantics are unchanged. (Don't "fix" the converter to show True/False.)

**Group separators (dark vertical gridlines).** Boundaries are marked with a 2px `#FF5A6B8C`
single-side cell border on always-visible anchor columns (the DataGrid's own gridlines fill the
other edges): **right** on `b_w1` (base│intraday — `b_w1` is the last base after `base_W1` was
added; the separator moved off `b_d1`), **left** on `y24` (intraday│daily), **left** on
`b_t1` (s_pos_lim│base), **right** on `filt_all` (filt_all│live-stats — a right border on the
left column reads bolder/cleaner here than a left border on `trd`, whose deployment-gradient
background competes), **left** on `c__upd` (p_int│c_upd). Anchor on the column whose displayed edge is the boundary (DisplayIndex order, not
XAML order — e.g. `c__upd` sits right of `p__int` on screen though declared earlier). Anchored on `b_w1`/`y24` because
bases and `y24` aren't hidden by the min-`p_int` pass, so the line is always drawn even when the
adjacent intraday columns are hidden/collapsed. Single-side thickness relies on the DataGrid's
own gridlines (`GridLinesVisibility` defaults to `All`) for the other edges. The per-cell
`…HasChanged` edit highlight (black, 3px) transiently overrides it while a cell is edited.

How it works:
- No placeholder columns — collapsing sets the member columns to `Visibility.Collapsed`
  (zero width, grid closes up); the collapsed band becomes a small floating **`+`** marker
  at the group boundary (in the `GroupHeaderCanvas` outline row), packed so adjacent markers
  don't overlap. Expanded shows a bracket + `−` chip + label.
- Bands are **measured against live column headers** and repositioned on scroll / layout, so
  `EnableColumnVirtualization="False"` is required (headers must always be realisable).
- A bracket spans the **first-visible to last-visible** column of its group, so it appears
  whenever *any* member column is on screen (the min-`p_int` pass may hide part of a group).
- **Marker/bracket placement works in `DisplayIndex` (on-screen) order, NOT `Columns`-collection
  order.** `ApplyColumnOrder()` reorders via `DisplayIndex`, so the XAML collection order ≠ visual
  order (stat columns are declared *after* the intervals but shown to their left). A collapsed
  group's `+` is placed at the nearest **visible** neighbour found by `DisplayIndex`
  (`PositionCollapsedBand` scans for smallest `DisplayIndex > lastDi` on the right, else largest
  `DisplayIndex < firstDi` on the left). Do **not** revert this to `IndexOfColumn`/collection-order
  walking — that put the daily `+` marker to the *left* of the intraday group.
- Band `Border.Background` is `null` (not `Transparent`) so empty band area isn't
  hit-testable and doesn't swallow clicks meant for an overlapping `+`; collapsed markers
  also get a higher `Panel.ZIndex`.
- Expanding re-applies `UpdateIntervalColumnVisibility()` (min-`p_int` baseline); that method
  also calls `ReapplyCollapsedGroups()` so a reload doesn't re-show a user-collapsed group.

**Performance — band repositioning is debounced.** `PositionGroupBands()` does a visual-tree
walk, and `FilterGrid.LayoutUpdated` fires on nearly every layout pass, so it must **not** be
called directly from `LayoutUpdated`/`ScrollChanged`. Those handlers call
`QueuePositionGroupBands()`, which coalesces a burst into a single pass per render frame
(`DispatcherPriority.Render` + a `_bandsUpdateQueued` guard). Calling `PositionGroupBands()`
directly on every layout tick made load/reload visibly slow (it ran hundreds–thousands of times
as rows streamed in). Relatedly, `RebuildMerged()` refills `MergedRows` via
`RangeObservableCollection.ReplaceAll(...)` — one `Reset` notification, not N `Add`s — so the
grid regenerates/lays out once instead of per row. Keep both patterns if you touch this path.

**Right-click "Turn intervals off" (per selected row).** The row context menu has a *Turn
intervals off* submenu — *Base intervals* / *Non-base intervals* / *All intervals* — wired to
`MergedTickerRow.TurnOffBaseIntervals()` / `TurnOffNonBaseIntervals()` / `TurnOffAllIntervals()`.
"Off" = filtered = flag `true` (so any `ON` cells go blank). Only the visible interval set is
set; each flag setter recalcs, and `ReCalcScaledDeployment()` runs once at the end so `s_dep`
updates. It's an edit like any other (highlights, persists on save via `ToUpsertRow`). Cells are
read-only, so this menu is the bulk-filter path.

**Add/Delete ticker + `Refresh()`.** Delete is a *soft* delete (`row.IsDeleted = true`, hidden by
`RowFilter`) followed by `MergedRowsView.Refresh()`. `DeleteTickerMenuItem_Click` must first end
any open grid edit (`FilterGrid.CommitEdit(Cell)` then `CommitEdit(Row)`, cancel if commit is
rejected) — otherwise deleting a freshly-added, still-unsaved row (which is mid-`EditItem`
transaction) makes `Refresh()` throw *"not allowed during an AddNew or EditItem transaction."*
Any code that calls `MergedRowsView.Refresh()` while a row could be in edit needs the same guard.

### Strategy columns (`strategy`, `strategy_b`) — editable dropdowns

Two editable ComboBox columns backed by `strategies_override`:
- **`strategy`** ↔ `StrategyName` ↔ `strategies_override.strategyname`.
- **`strategy_b`** ↔ `StrategyNameBase` ↔ `strategies_override.strategyname_base` (the base strategy).

Each has its own `Original…`/`…HasChanged` tracking, khaki "changed" highlight, and is wired
through `AttachStrategyOverride` (baseline), `ToStrategyOverrideInsertModel` (save payload —
sends `this.StrategyName` / `this.StrategyNameBase`), the clone/snapshot/preserve paths, and
the save triggers (`affectedStrategyTickers`, the override-payload `.Where`, `HasStrategyEdit`).
Server-side the upsert maps both into `strategies_override` ([tapi.py](trading/apis/tapi.py) `upsert_filter_intervals` job).

**Changing a strategy offers to turn its side's intervals off.** `FilterGrid_BeginningEdit`
captures the pre-edit value; `FilterGrid_CellEditEnding` compares on commit and, if the value
genuinely changed, prompts (deferred via `Dispatcher.BeginInvoke` so the edit fully commits
first) — Yes calls `TurnOffNonBaseIntervals()` for `strategy` or `TurnOffBaseIntervals()` for
`strategy_b`. Using the edit lifecycle (not ComboBox `SelectionChanged`) avoids false fires on
load/scroll/virtualization, since the ComboBox lives in a `CellEditingTemplate`.

The `strategy` / `strategy_b` **column widths are auto-sized** to the widest name in their
**dropdown list** (`StrategyNames` / `StrategyNamesBase`, plus the bold header) — so whatever the
user picks fits without clipping. (Sizing to only the currently-shown row values looks tighter but
clips as soon as a longer name is selected.) `SizeStrategyColumns()` (end of `LoadAllAsync`, after
the names load) measures with `FormattedText` and sets each column's `Width` (+12px padding). The
editing ComboBoxes use `MinWidth="0"` so they fill the computed column rather than forcing 140.

Dropdown lists come from **one endpoint**, `/get_strategynames`, which takes an optional JSON
body — `{"interval_type": "<t>"}` restricts to that type; `{"exclude_interval_type": "<t>"}` drops
that type (`where` is equality-only, so exclusion is post-filtered in the handler):
- `LoadStrategyNamesAsync` posts `{"exclude_interval_type":"base"}` → all manual strategies
  **except** base ones (`strategy` must not offer base strategies — those belong to `strategy_b`).
- `LoadStrategyNamesBaseAsync` posts `{"interval_type":"base"}` → `where {manual:1, interval_type:"base"}`.
`"NONE"` and `"default"` are always prepended, so the dropdown is never empty.

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

## Scaled Positions screen (`winScaledPositions`)

Views `scaled_positions` (`/get_scaled_positions`, all scale types) with fund-group/fund/ticker
text filters, plus **manual scaling entry**:
- **"Show manual scaled types only"** checkbox → `ScaledPositionsViewModel.ManualOnly` (filters to
  `scaled_type=="manual"`; **off by default**). The filter also always **hides trivial manual rows**
  (both `scaled_percent` and `scaled_target` == 1).
- **All scaling actions live on the right-click menu** (there is no toolbar "Add new" button):
  - **"Edit scale…"** edits the right-clicked row and **preserves its `scale_type`** (manual,
    filtered, out, in, …). Enabled whenever a row is selected (`Grid_ContextMenuOpening` →
    `miScaleIn.IsEnabled`). Passes the row's `scaled_type` + `scaled_timestep` through to `winScale`.
  - **"Add new manual scale…"** opens the shared `winAddTicker` picker (ticker+fundgroup+fund) to
    start a brand-new scale. **Adding is manual-only** (always `scale_type="manual"`,
    `scaled_percent=1.0`).
  Both open the shared **`winScale`** popup via `OpenScale(...)` and reload on success.
- **Editing routes through the API for ANY type** (`winScale.UseApiInsert = true`), because this
  screen has no `SqlConnection`. `winScale.Button_Click` inserts via `ScaleService` when
  `UseApiInsert` **or** `scale_type=="manual"`; the row carries the actual `scale_type` and the
  preserved `scaled_timestep` (`winScale.ExistingTimeStep`, default 5). The server `Scale.insert`
  honours the per-row `scaled_type`. `winScale.validate_scaling` caps the target at `ScaleLimits.Max`
  for **scale-in types** (`manual`/`filtered`/`in`) and at `1.0` for scale-out types; `LoadForm` shows
  the current `scaled_percent` for any type being edited. The effect-aware confirm (`ScaleResolver`,
  see below) is passed the `scale_type` so its wording matches ("This *filtered* scale …").
- **`winScale` manual mode** inserts via **`ScaleService`** → `/insert_scaled_positions` (server-side
  `Scale` class: validation, stepping, overlap checks) — **not** the direct-SQL `Position.scale_position`
  used by the filtered/positions paths. So the scaled-positions screen needs no `SqlConnection`
  (`winScale` is constructed with `conn: null` for manual). This is the preferred path for new scaling.
- **Effect-aware confirm (manual only) — `ScaleResolver`.** Scales **never compound**: for a given
  fund exactly **one** `scaled_positions` row applies per ticker, chosen by specificity then target.
  So a new manual scale may be superseded by (or supersede) an existing scale (e.g. a filtered one).
  Before inserting, `winScale` calls `ScaleResolver.Resolve(...)` with the current rows (passed in via
  `winScale.ExistingScales`, set by `OpenManualScale` from `ScaledPositionsViewModel.Rows`) and shows a
  confirm stating the **concrete outcome**: either "WILL take effect / take precedence over the existing
  *type* scale (target n%)", or "will be stored but WILL NOT take effect: the existing *type* scale …
  takes precedence" (dialog defaults to **No** + warning icon in the no-effect case). `ScaleResolver`
  **mirrors the server precedence** in `hoover/scale.py` (`specificity_sort_cols` / `_wildcard_rank`):
  rank `= 4·(ticker≠'*') + 2·(fundname≠'*') + 1·(fundgroupname≠'*')`, higher wins; **tie → lower
  `scaled_target`** (most conservative). It excludes the exact same `(group,fund,ticker)` key (a re-scale
  just replaces it via `select_latest`, not a rival) and trivial 1/1 rows. Keep it in sync if the
  server precedence changes.
- Scaling roles across screens: **positions** = scale out, **filter-intervals** = scale in (filtered
  tickers), **scaled-positions** = scale in (`manual`).
- **Scale-factor cap = server `Scale.max_scale`, fetched (not hardcoded).** The endpoint
  `/get_scale_limits` returns `Scale.min_scale`/`Scale.max_scale`; the client caches it in the static
  `ScaleLimits` (`EnsureLoadedAsync`, fallback 0..2). Both scaling screens validate against it, so
  **changing `Scale.max_scale` server-side needs zero client edits** (just a service restart):
  - scaled-positions `winScale` manual: `validate_scaling` caps a manual target at `ScaleLimits.Max`
    (non-manual stays ≤ 1); the API is the backstop.
  - filter-intervals `new_f`: `RangeValidationRule UseScaleMax="True"` reads `ScaleLimits.Max` (was a
    hardcoded `Max="3"`, which mismatched the server's 2.0 and only failed at save). Its
    `NumericRangeBehavior` now only blocks negatives live; the rule enforces the upper bound on commit.
  Load it wherever scaling can happen (`LoadAllAsync`; `OpenManualScale`).
- **`fundname` vs `scaled_fundname`:** `fundname` is a PK column and may be a wildcard `*` (one scaling
  row for all funds in a group; used by `Scale.load`'s `fundname IN ('*', fund)` matching).
  `scaled_fundname` was a **write-only provenance** column (the concrete fund behind a wildcard) that
  **nothing ever read**; it has been **removed from `dbmodels.Scaled_positions`**. The API/`Scale`
  insert path does not write it (so manual inserts show it blank); only the legacy direct-SQL
  `Position.scale_position` still references it. Treat it as deprecated — don't rely on or re-add it.

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
