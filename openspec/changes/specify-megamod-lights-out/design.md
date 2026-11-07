## Context

Single owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target1.7 beta.
S052 replaces LightsOut, `juanlopez2008.LightsOut`, Workshop2584269293. Documentation only.
[Research](../../../mods/megamod/docs/refinement/2026-11-03-lights-out-research.md) pins the selected
net10 DLL, source commit, all17 compatibility families, native samples and two actual consumers.
It distinguishes compiled behavior, source explanations, unverified concerns and intended changes.

## Goals / Non-Goals

**Goals:** occupancy-driven lamps, reliable activity-based standby, source-derived defaults, normal
work availability, common settings and narrow optional integration. Preserve useful observable
contracts while replacing upstream self-patches and stale global state with direct owned policies.

**Non-goals:** content/resource networks, granting electricity, new flick jobs, all-device automation,
changing work speed/yields/research, old settings import, unproven whole-mod compatibility, or
claiming every modded glower/worktable can be inferred safely from a name.

## Decisions

### 1. Classify concrete capabilities and choose one activity rule

Native Core lamps with the ordinary CompGlower/power contract and admitted modded equivalents use
room occupancy. Wall adapters identify the illuminated/facing room; helper glowers are not separate
lamps. Preserve source exclusions for sun/growing lights, heat/climate/scheduled/landing-beacon
devices, power plants and continuous equipment. Type/Def names locate candidates; they do not prove
ordinary-light semantics. Unknown multifaceted devices stay native until a specific adapter exists.

Attended bill/research/drill activity, TV viewers, rechargeable state and each autonomous device
have explicit policies. Use current native activity, including demand necessary to start work;
merely waiting for a worker must not create a power-off/no-job deadlock. A consumer stays active
while any valid user/process needs it. Use native idle/active baselines, including research upgrades,
instead of rewriting raw basePowerConsumption or persistent PowerOn/flick state.

Native beta already handles phase-based idlePowerDraw in gestators, vats and gene assemblers, and
CompAutoPowered handles loudspeakers. Extend that behavior only where needed for the selected
standby policy. Never wrap it in competing active/idle state machines. Gene facilities count all
current dependent work; tuning nodes retain bandwidth service; selected/entering occupants retain
readiness without pretending an empty, abandoned device is processing. Runtime admission must
prove cancel/load/failure paths as well as successful start/finish callbacks.

Source standby is global; the source active multiplier applies mainly to recognized tables and
rechargeables, omitting some registered consumers. The owned contract deliberately normalizes the
active multiplier across the listed work/charging/TV/ritual equipment, with lights retaining their
native active draw. Default100% is unchanged. This removes an opaque implementation-dependent
exception; LO07 records the non-default behavior change. Continuous/excluded devices are unaffected.

### 2. Minimal map state, current native facts and bounded work

Use Room.ContainedAndAdjacentThings and native map/building/pawn listers before introducing any
derived Thing index. No independent world scan/cache per upstream subsystem. If other owned
features need the same derived candidates, add them to the common map-owned index with explicit
keys/consumers/invalidation; do not make the search/hauling index store pawn eligibility or room
decisions. Ordinary resource reads must not scan every pawn/building.

Keep only per-building effective activity, Keep On, bounded pending departure and required
user/process ownership. A new activation, Keep On change, room replacement, despawn or transfer
invalidates old deadlines. Use cancellation/generation identity rather than the source's FIFO
queue whose order changes with delay settings. Coalesce room work and recheck native occupancy at
expiry. Source checks every30 ticks; retain at most0.5 simulation seconds of deadline scheduling
latency, with immediate wake/entry/work activation. No repeated100-attempt snapshot loop; defer
an unstable room and leave native behavior until a stable snapshot can be read.

Discard transient references on map/game teardown. Rebuild from current native state after load,
including active autonomous jobs, TV viewers, charging, sleepers and moved ships/gravships. Save
remaining departure time so reload does not prematurely turn lights off or restart the entire
delay. Persist building Keep On and current-game live selector separately from configuration.

### 3. One resource calculation with native restrictions intact

Treat consumption as a nonnegative magnitude in the owned policy and translate signs/units only
at admitted native/provider boundaries. Preserve EMP, breakdown, disconnected/flicked-off state,
generation, native research factors and provider restrictions. Never multiply the same demand
twice. Native zero demand stays zero; the0.01 W floor applies only to an otherwise positive,
available electrical consumer that this feature puts into standby. Source explicitly documents
this floor as protection against incorrect power availability and repeated bench pathing.

For devices admitted to enabled S053 equipment budgets, use S052 actual-use activity and S053
rates once, as specified in [RP01–04](../specify-megamod-repowered/specs/megamod-repowered/spec.md).
S052 rates remain the fallback for shared equipment when budgets are disabled and apply normally
to equipment outside S053's scope. Label their settings accordingly; charging and ritual equipment
do not silently acquire the generic S053 work multiplier. One rate resolver, no internal patches.
Failure of the combined device group overrides that normal fallback and keeps the device native,
with both owned rate policies and the added forecast inactive; inspection states unavailable.

Lights automatically suppressed by occupancy draw0.01 W; supported idle equipment in S052's rate scope draws
max(0.01 W, native same-phase demand × standby percentage). Active supported equipment uses native
same-phase demand × active percentage; active lights keep native demand. Idle50% of a100 W native
baseline is50 W; active150% is150 W. These are policy examples, not new building Def values.
Cap the standby floor to ordinary positive demand if that is smaller than0.01 W. Native/provider
idle baselines remain distinct from active demand. Inspect text adds status and
truthful consumption without replacing unrelated inspection lines.

Gas is a separate resource contract. The source applies negative electrical MinDraw to
CompGasTrader.GasConsumption, but inspected current VFEPower lacks that legacy target. Do not infer
the correct sign, unit, nonzero floor or a successor package. LO-Q2 must resolve an actual provider;
until then the gas adapter is inactive and its buildings retain native consumption and glow.

### 4. Patch target groups and effective semantics

Follow the [foundation contract](../add-megamod-foundation/specs/megamod-patch-target-specs/spec.md)
and mod AGENTS.md. Create stable per-patch target documents before implementation, with original
versus effective assumptions, incoming-stream checks for any transpiler and owned-only rollback.
No PatchAll, whole-method/hash equality gate or foreign-owner prohibition. Pure observation hooks
need only the callable/lifecycle prerequisites actually consumed; changing power/glow results
requires evidence of their sign, availability and calculation semantics.

| Candidate boundary | Required semantic evidence / coherent group |
| --- | --- |
| Pawn room movement and LayDown sleep | Old/new room and current sleep transitions; retain native actions, no swallowed callback; occupancy branch must include spawn/rebuild/removal recovery |
| CompGlower.ShouldBeLitNow / UpdateLit | Owned suppression preserves false from native/providers and refreshes current map without altering color/radius; one coherent light/resource group |
| CompPowerTrader.PowerOutput / native device phase demand | Effective demand sign, upgrades, EMP/zero behavior and single owned scaling; skip affected device group if its activity cannot be reconstructed |
| Bills, research, drilling, TV | Native user/work start/end, already-at-target jobs, cancellation and last-user ownership; integrate directly with S010/S014/S018/S019/S051 work flow |
| Rechargeable / Biotech device policies | Current charge/process/selected occupant/shared facility demand and all stop/load/transfer paths, not just a matching method name |
| Ritual speakers | Actual native ritual demand and availability; never mutate activity from outcome Count or prediction |
| Optional glower, wall/ceiling, temperature, gas, ship movement | Package owner, actual Def/component shape and provider semantics; coupled resource/glow/heat/location behavior admitted together |
| Inspect text and description consumer | Additive display, correct same-phase units and configuration cache refresh; no upstream patch invocation |
| XML component attachment | Native/external package/content/node existence, idempotent additive comp merge and preserved other entries |

Every skipped enabled patch logs one bounded startup warning with package/patch/spec ID, target,
failed predicate, unavailable behavior and native fallback. Optional absent packages create no
failed candidate. Present-but-changed content disables only its coherent branch. Validator/install
exceptions remove only newly installed owned patches in that group. Supported later repatching
must revalidate; incompatible effective code cannot leave a half-active resource controller.

### 5. Optional content and external consumers

The research table is the full source-family inventory. Verified canonical IDs include
VanillaExpanded.VFEA, Murmur.WallLight, VanillaExpanded.VFEPower, Aidan.SelfLitHydroponics,
Erin.JapaneseFurniture, Dubwise.DubsBadHygiene, Rikiki.MiningCo.MiningHelmet,
kentington.saveourship2, VanillaExpanded.Temperature and Owlchemist.CeilingUtilities.
Canonical identities/content for Androids, Questionable Ethics, Extinguish Refuelables, legacy
Colony Manager, Reinforced Mechanoid2 and More Furniture require remaining verification. Do not
substitute installed Colony Manager Redux or a same-ID ceiling fork without content admission.
Ideology/Biotech are optional native DLC branches, not global dependencies.

Declarative Keep On component attachment uses XML Extensions and additive duplicate checks,
including external packageId=true/content guards. Avoid adding a component to every unrelated
building merely because the source patches abstract BuildingBase. No list/category replacement.
The first actual XML Extensions operation requires the dependency and load order in metadata.

Relevant Stats In Description actually detects the old package and reflects an upstream postfix
and rate property. A type facade alone would not satisfy its package gate. Prefer an absent-safe
adapter to its narrow getMinMaxPower/display path using an owned pure consumption projection and
cache invalidation after configuration/research changes. Never modify its files or spoof the old
package. Installed1.6 consumer behavior is evidence of the contract, not beta admission. If no
adapter is admitted, keep its original behavior, expose truthful owned inspect values and document
the unavailable variable-range integration. S098 is a different mod, not this consumer.

WOWGAG's Performance Patch probes GetTypesToPatch by name and quietly skips absent type. Do not
create a redundant upstream startup scan/facade just to let it optimize that scan. Its unrelated
patches remain untouched. No blanket compatibility claim follows from this narrow observation.

When original LightsOut, external RePowered, RePowerReborn or original Turn It On and Off is active,
suppress the combined owned lighting/equipment power group with a startup explanation. Never
unpatch the provider. Internally S052 and S053 have the same megamod owner and one direct policy;
the external co-install boundary does not omit either source's requested owned behavior.

### 6. Common configuration and contextual state

Six settings in ordered Lighting and standby sections: automatic lighting defaultOn, keep lights
on while sleepingOff, count animalsOff, departure delay1.5 seconds, standby draw0%, active work
draw100%. All use global default / Use global / Current save with dormant overrides and main-menu
callback protection. Inputs reject nonfinite/overflowing values and retain the prior valid value;
the displayed supported range must match storage and scheduling limits. Percent standby0–100,
active integer>=100; active limits cannot overflow a supported demand calculation.

One compact bottom-right operational selector offers On/Off/Use configured value for automatic
lighting, current-game scope across loaded maps, with Use configured value for new/missing state.
Disabling it restores native lights without
disabling standby equipment. Per-light Keep On is saved entity state, with no global inheritance;
ordinary ownership/selection rules apply. Debug diagnostics are bounded and developer-only at the
top, not a log-spam setting. Maintain seven baseline languages and native lamp color/flick commands.

## Risks / Trade-offs

- Zero-cost standby can change power economics → preserve source0% default and floor, expose rates;
  final semantic/native balance pass checks representative lamp, bench and autonomous processor.
- Phase-aware/native auto-power creates circular readiness risks → prove native work assignment,
  power loss/restoration and completed-cycle behavior, not just values from a getter.
- Room and building transfers can retain stale state → current map identity and cancellable work;
  native movement plus optional ship workflows require separate causal acceptance.
- Legacy providers differ from installed successors → record unavailable branches, no guessed sign,
  package or type fallback. This draft is not a claim that all17 families run on1.7.

## Migration Plan

No automatic import of source globals, HugsLib XML, keepItOn state or upstream component identities.
New owned entries use configured defaults and Keep On=false; do not rewrite an upstream save on
startup. Any later opt-in importer requires its own backup, atomicity, restoration and postcondition
contract. Disable/uninstall behavior restores ordinary power/glow without rewriting native job,
recipe, occupant or resource data. No code or XML is shipped by this documentation change.

## Open Questions

- **LO-Q1:** Exact effective beta target groups, native phase/readiness and floor behavior, including
  start/cancel/load, shared processors, already-at-target workers and powerless workbench loops.
- **LO-Q2:** Canonical missing providers and current content; gas sign/unit/floor; ceiling forks,
  ship movement, foreign glowers/heat; RSID beta/cache contract and WOWGAG coexistence verification.
- **LO-Q3:** S053 research and direct draft composition now exist; verify their effective rate/
  activity boundary and audit future selected work, temperature and display changes against it.
- **LO-Q4:** Native UI/settings/lifecycle, multiple maps, removal, seven languages and final causal
  acceptance on the reviewed build. Measure bounded work/memory before performance claims.
