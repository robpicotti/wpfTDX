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

> **Planned (not started):** a market-announcements feed will flag tickers affected by upcoming
> economic events (NFP, FOMC, …) and **highlight their rows here**. Design note lives in the
> trading repo `CLAUDE.md` → "Planned project — Market announcements feed". The WPF side is part 3
> (a per-row `IsEventAffected`-style flag + row highlight, kept distinct from the deployment colouring).

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
`DesiredColumnOrder` array — Bill's layout: `category, ticker, fundgrp, fund, strategy, strategy_b,
m__int, p__int, c__upd, manual, rescale…filt_all, trd…n_dep, scale_f, new_f, pos_lim,
s_pos_lim, pos_tgt,` then **all intervals at the far right** (`b_t1…b_d1`, then `t1…W2`). `s_dep` is
`Visibility="Collapsed"` (hidden on screen, ViewModel kept) and dropped from this array. To change the
on-screen order, edit `DesiredColumnOrder` (not the XAML block order). If you add a column,
add its header to that array or it'll fall to the end.

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
- **`base`** → the `b_*` columns (`b_t1, b_v1, b_y1, b_d1`).
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
other edges): **right** on `b_d1` (base│intraday), **left** on `y24` (intraday│daily), **left** on
`b_t1` (s_pos_lim│base), **right** on `filt_all` (filt_all│live-stats — a right border on the
left column reads bolder/cleaner here than a left border on `trd`, whose deployment-gradient
background competes), **left** on `c__upd` (p_int│c_upd). Anchor on the column whose displayed edge is the boundary (DisplayIndex order, not
XAML order — e.g. `c__upd` sits right of `p__int` on screen though declared earlier). Anchored on `b_d1`/`y24` because
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
