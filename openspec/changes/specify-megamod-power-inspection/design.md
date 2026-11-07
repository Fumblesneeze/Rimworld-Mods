## Context

Single owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S102/P2-32 analyzes `arl85.MorePowerInfo`, Workshop 2906529763, selected 1.7 DLL 1.3.0.0 and author
revision `7a837a8b869bf72694e6b2e3745795e1df0565e5`. The committed and installed DLL hashes match.
[Research](../../../mods/megamod/docs/refinement/2026-11-05-more-power-info-research.md) retains
source/native identities, complete source behavior, consumer bounds and open questions. This is a
documentation-only draft, not live evidence or an implementation.

## Goals / Non-Goals

**Goals:** truthful network totals and two qualified reserve estimates in ordinary inspection,
native-list reuse, preservation of other text and direct composition with owned power policies.

**Non-goals:** power control, generator/battery rebalancing, future weather/fuel/work prediction,
load-shedding simulation, another network model/cache, importing source identities or implementing
this slice now. Source behavior is reference material, not automatically the desired algorithm.

## Decisions

### 1. Native network snapshot, shared ownership

Resolve the selected CompPower's actual PowerNet and use its powerComps/batteryComps. Sum current
PowerOn EnergyOutputPerTick by sign; do not use Def basePowerConsumption as live draw. Read usable
reserve according to admitted CurrentStoredEnergy semantics. Preserve W versus Wd and the native
WattsToWattDaysPerTick conversion; use wider host-safe numeric intermediates for calculations.

This is a read-only projection from the same owned power service used by S052/S053. Their actual
rate decisions remain authoritative. S053's provisional job-start reservations are not physical
draw and must not enter this current-load readout. No new Thing search, duplicate periodic map
scan, independent provider cache or runtime library mod is warranted. Native lists are already the
network index. Initially compute on demand for the inspected network; if repeated UI calls justify
memoization, share one bounded projection with explicit map/network/state/language invalidation.
Never persist live references, retain removed maps, or access Unity/Verse off-thread.

Freshness is next native inspection refresh after state availability, including paused selection.
A queued topology update has not happened merely because a player clicked; avoid predicting its
result, and discard an old network view as soon as the actual relationship changes. Read-only
observation must not invoke PowerNetTick, AddEnergy, DrawPower, shuffle or any mutation/random path.

### 2. Qualified snapshot calculation with explicit states

Separate numeric facts, supported model, and localized rendering. The spec's examples are normal
steady snapshots, not exact future-outage tests. Let E be usable Wd, P and U current production and
usage W, B=P-U, and L the admitted instantaneous self-discharge W. At a deficit, current depletion
is E/(U-P+L) game days. The generation-off comparison is E/(U+L) days. Multiply by 24 for hours or
60000 for game ticks only at the boundary. Examples: 300/305×24≈23.6066 h and 300/505×24≈14.2574 h.

For a partially charged network with a surplus, determine storage gain from admitted native
charge distribution, actual per-battery capacity/eligibility and efficiency, then subtract L.
Project a pure immutable snapshot through bounded capacity-redistribution steps; never call native
mutators to ask what they would do. Do not assume all input stores at 50%, take an unqualified mean
efficiency, or iterate one game tick at a time across the estimated duration. Source loss is 5 W per
nonempty native battery, capped to its remaining energy in the observation interval. An external
provider requires the actual consumed loss/charging semantics, not its display name.

Native capacity redistribution stops below 0.0005 Wd/tick (about 30 W) after a pass; when a full
battery was removed first, a smaller residual can be discarded before reaching a partial battery.
Preserve this admitted branch in the pure projection rather than crediting an idealized transfer.
This is a native comparator, not a new configurable limit or permission to mutate the simulator.
The current snapshot must establish battery-versus-network tick ordering before interpreting losses.

The result is **at current rates**, not a future integrated depletion simulation: becoming empty,
full, disconnected, disabled, differently loaded or differently supplied changes the snapshot.
Native restart reserves, load shedding, fuel use, sunlight and wind are not predicted. A full usable
reserve with a positive surplus returns **Full — at current output**, not an extrapolation from a
single capacity-clipped charge step. The separate generation-off comparison still has meaning.
This deliberate state replaces source's unconditional “Forever” at sufficient assumed charging.
At full reserve with zero/negative balance the ordinary loss/deficit estimate still applies.

State precedence: disconnected/uninspectable → no owned network contribution; native electricity
disabled/debug unlimited → explicit mode, no normal duration claims; unsupported model → estimate
unavailable; E=0 → no usable reserve; full with surplus → current full state; supported zero/positive
stored-energy trend → no depletion at current rates; negative trend → finite depletion. Evaluate
the storage-only branch separately, retaining it at full reserve. Never label zero reserve as an
outage when current production can power loads. State/ratio decisions precede display rounding.

Keep any numerical tolerance proportional to floating-point error, not source's 1e-5 Wd/tick≈0.6 W
deadband. Genuine small deficits remain deficits. Use bounded period formatting without casting
unbounded values to int; tiny positive periods and very large finite periods receive meaningful
text. NaN, negative reserves or invalid efficiency/capacity are unknown, not clamped into a claim.
Use S034's common number formatting where appropriate, without duplicating its settings.

Native EMP behavior needs particular care: total usable energy excludes stunned batteries but the
discharge giving list does not use the same filter. Keep the truthful native usable total; until
the mixed-state distribution is admitted, show estimate unavailable rather than “fixing” native
power or silently treating all stored charge as usable. Ordinary native-compatible batteries with
different validated Def capacities/efficiencies remain supported; this is not a whitelist of mods.

### 3. Additive contextual inspection

Prefer an existing owned structural inspection contribution; otherwise a narrow postfix of the
admitted native CompPower.CompInspectStringExtra is the likely integration seam. It returns string,
is instance/parameterless and uses the parent's network. Native CompPowerTrader and CompPowerBattery
prepend their own lines before/around the base call; do not replace their entire results. Some
overrides/Off branches do not call base and require investigation before promising coverage.

Append a single clearly labelled owned section; preserving a native summary is preferable to
deleting another contributor's text with English string matching. Existing device faults, fuel,
capacity and efficiency remain. Duplication of an already-native balance/stored number is acceptable
when retaining the original line is the only safe option. Owned S052/S053 lines compose directly,
without Harmony patches against our own implementation. Do not introduce a bottom tab, button,
generic setting or saved field for a read-only contextual section.

Use all seven baseline languages, localized W/Wd and native/common period conventions, clear
current-load scope and concise estimate qualifications. If a tooltip is used, its native hit target
and keyboard/readability path need separate admission; the essential qualification must remain
readable in the section itself. No raster asset is needed.

### 4. Minimal executable target contracts

Before implementation create stable patch-target documents with owning scenarios, validators, tests
and eventual evidence. Do not copy source PatchAll or treat inspected hashes as runtime gates.

| Boundary | Minimum semantic admission |
| --- | --- |
| Contextual inspection hook | Exact callable shape, selected parent/network lifecycle, invocation coverage and string result; additive composition and duplicate prevention even with benign other patches |
| Current totals | Current network lists, PowerOn/sign/units and usable-storage meaning; compatible content values allowed; skip-original/effective changes cannot silently invalidate assumptions |
| Battery estimate | Actual self-discharge, efficiency/acceptance, distribution/disabled semantics and consumed modifiers; resolve known optional providers behind package/shape adapters |
| Optional later invalidation hooks | Exact observed lifecycle and required original/effective behavior; if a transpiler is unavoidable validate its actual incoming stream on every run |

Compatible foreign owners and unrelated IL changes are allowed. Original/effective/incoming checks
must match what a patch consumes, without repeatedly executing foreign transpilers to probe them.
False/unknown predicates, validator exceptions and install errors leave the affected contribution
inactive. Use separate admission groups so forecast failure does not erase trustworthy totals.
Initialization is idempotent, rolls back only newly installed owned patches in its failed group,
and emits one bounded startup warning per skipped enabled patch with package/patch/spec/target,
reason, unavailable behavior and fallback. Later supported repatching invalidates/revalidates the
relevant group; no stale claim survives. Runtime nonfinite model inputs get a bounded explanatory
fallback, not per-frame logs. A display read must not mutate gameplay as a recovery action.

No XML patch is currently required. If declarative compatibility work becomes necessary, use XML
Extensions package/Def/node guards, preserve unrelated fields/list entries, and declare its required
dependency with the first actual use. Optional providers remain absent-safe, narrowly resolved and
outside eager references. Core is required; Harmony is required with the native hook; no DLC is.

### 5. Provider and contract policy

Original `arl85.morepowerinfo` active (case-insensitive canonical comparison) suppresses the owned
overlapping contribution with an explanation. Do not remove its patches, spoof its package, or add
a whole-megamod incompatibility declaration. No external consumer was established by the bounded
installed ordinary-string scan. Source has no settings/own save schema, so no migration or facade
is justified. Unknown custom power models fail only the unsupported readout branch; no universal
power-mod compatibility claim. Sources and previews are not bundled and no reuse grant was found.

## Risks / Trade-offs

- Instantaneous forecasts can change quickly → explicit current-rate scope, event-state handling and
  live updates; do not conflate remaining reserve with exact uptime.
- Full/mixed battery behavior can defeat a simple ratio → explicit full state and bounded admitted
  allocation, with unavailable fallback when required semantics are unknown.
- Additional inspection text competes for space → concise localized lines, seven-language narrow
  UI acceptance and retention of native workflow; never remove unrelated information to fit.
- Game beta/effective patches can change numerical semantics → semantic admission, independent
  totals/forecast groups and exact reviewed-build causal evidence.

## Migration Plan

No gameplay data, settings or upstream saves are migrated. Implement one observable slice at a time
under focused RED/GREEN; preserve native state. Removing the owned feature removes only its readout.
No implementation task is checked until the root review and final native-workflow gates are met.

## Open Questions

**MPI-Q1–3** in the [ledger](../../../mods/megamod/docs/refinement/specification-progress.md) retain
exact effective hooks/EMP semantics, bounded allocation/formatting/shared cost, and reviewed-build
native/language/provider acceptance. Research these without asking the user to explain the source.
