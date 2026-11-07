## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S046 replaces Consciousness Patch. [Research](../../../mods/megamod/docs/refinement/2026-11-03-physiological-consciousness-research.md)
pins complete matching published source/installed 1.6 binary and exact native 1.7 IL. No runtime work
or beta compatibility acceptance accompanies this draft.

## Goals / Non-Goals

**Goals:** retain physiological contributions and source tuning ranges, implement the intended input
ceiling, keep native health consequences, compose directly with S045 and apply settings coherently.

**Non-goals:** added organs/research, guaranteed survival, a final-consciousness cap, a second capacity
calculation/cache, source identity emulation, per-pawn settings or rewriting the complete native worker.

## Decisions

### Change the three contributions once, preserving surrounding native calculation

The inspected worker computes brain efficiency minus native pain loss, then three successive Lerps.
For each input X and weight w, preserve the equivalent factor `1 + w * (X - 1)`; with a limit use
`min(X,K)`. Order remains BloodPumping, Breathing, BloodFiltration. Defaults are .2/.2/.1, limit off,
stored limit 3.0. Weight controls accept .1–1.0; the common input limit accepts 1.0–10.0. Labels show
20/20/10%, and 300% with allowed 100–1000%. Higher weights also increase impairment below 100%.

Retain the native brain/pain computation, CalculateCapacityAndRecord impactors, applicability and
outer PawnCapacityUtility ordering: offsets, factors and lowest maximum only for a positive worker
result, followed by minimum and rounding regardless. Positive offsets cannot rescue a nonpositive
worker result through the skipped modifier branch. Do not
recompute the entire worker in a postfix or duplicate impactor collection. Ordinary native input
capacities may themselves have limits; this feature removes only the worker's three input ceilings.

One narrow transpiler is the intended integration. Candidate emission can replace each native input
limit constant with a runtime policy value (unbounded when off), retaining its Min operation, and
replace its weight with the runtime policy value. Preserve all labels and exception boundaries.
Choose the exact emission only after the target checks prove stack/control-flow correctness. Do not
copy the source's fixed-index matcher or its defective cap-on replacement of Min by a constant.

One committed immutable policy must serve the whole calculation. No yields or policy writes inside
the worker; confirm Unity-thread/call semantics before relying on one shared snapshot. Values must
be finite and within configured domains. Invalid persisted/input settings retain the last valid
policy, or built-in defaults when none exists, with an explanatory common-settings diagnostic.
Supported provider inputs and intermediate results must be finite and representable. CO-Q3 must
establish safe numerical handling before admitting extraordinary provider domains; no arbitrary new
gameplay cap or promise to repair a provider's NaN/overflow is inferred from this draft.

### Compose health behavior directly

S045's [malnutrition ceilings](../specify-megamod-malnutrition-consciousness/proposal.md) remain in
the ordinary final-capacity maximum phase. The worker benefit is calculated once, then the effective
condition maximum applies. With all inputs at 200%, a 100% brain/pain baseline becomes 158.4% before
outer effects; S045 trivial malnutrition still limits final consciousness to 95%. Other stronger
condition maxima remain effective. No Harmony patch against our own feature is involved.

Preserve native injury, pain, consciousness/wakefulness/downing/death and healing logic. Changing a
capacity may legitimately change those outcomes; do not force awake, clear conditions, resurrect,
restore health on disable or promise consequences can be undone by changing the setting back.
All pawn kinds using the supported worker are eligible, not only player colonists. No added organs,
item stats, research access or material costs; real enhancements retain their own acquisition costs.

### Five settings through the common path, with coherent application timing

Use the shared definition/load/resolve/store/render path for the limit switch, limit value and three
weights. All five inherit global by default, support dormant per-save overrides, and expose labeled
Use global / Current save / Global default controls. Disable the save editor while inherited and
both save controls at the main menu, including mutation callbacks. No implicit previous-save target.
The input-limit editor may stay usable while the limit is off, labeled as dormant until enabled.

Validate and store a completed edit, then stage one resolved policy. An active save continues using
its previous applied policy until the next unpaused game update; show pending status while paused.
Apply the new policy and invalidate affected native capacity/dependent-health state at a safe point
before ordinary work advances. The same applied policy must underlie native Health details and actual
health/work decisions. A menu edit must not unexpectedly advance simulation. Loading a save resolves
its own policy before its first ordinary update; never carry the previous save's applied snapshot.
Global edits affect an active inherited row but not its overridden counterpart.

CO-Q2 retains the exact lifecycle seam, dependent capacity dirtying and native health reevaluation,
including living map/world/container pawns and dormant/dead state. Use native enumeration once per
committed policy or relevant load, not every frame/tick. Do not directly manufacture awake/downed/dead
results; let the ordinary health path evaluate the changed capacities. If a coherent application cannot
be guaranteed, keep the old applied policy and report that the new choice is pending/unavailable;
do not expose partially refreshed gameplay as the selected policy.

Persist settings on committed changes, not drawing. Supply seven-language labels, explanations of
relative weights and input versus final limits, and pending/disabled diagnostics. No new bottom tab,
debug control or bottom-right toggle: these are configuration values in the structured mod menu.
Existing Health/Compact Hediffs/RimHUD surfaces read actual native capacities; no parallel HUD value.

### Semantic target and one atomic patch group

Before implementation publish a stable-ID target document linked from the patch-target index,
requirements, validator, tests and eventual live evidence. Require the callable worker/impactor
contract and three unique semantic chains: the named capacity Def, CalculateCapacityAndRecord,
input upper bound, multiplication with current consciousness and the intended interpolation weight.
Check operands/data flow/cardinality, not exact offsets, locals, whole-method hashes or absence of
other Harmony owners. Brain/pain and later native outer maximum semantics must remain supported.
Compatible surrounding edits and foreign contributions are allowed and preserved.

Check actual incoming instructions every time the transpiler executes; build candidate edits only
after all three chains pass and validate emission before returning it. On ambiguity/unknown shape,
return the incoming stream unchanged. Treat the three contributions as one atomic feature group.
Check effective ordering and skip-original prefixes; never repeatedly run foreign transpilers as a
probe. Specify later repatch invalidation/revalidation. On installation failure remove only newly
added owned hooks, never a foreign owner or historical unrelated owned patch.

Active original `Vex.Consciousness` suppresses the owned overlap; its Harmony owner is
`com.consciousness.patch`, not an identity to borrow. Other organ/health packages are not conflicts by
presence. Optional adapters require package-ID and actual contract resolution. False/unknown guard,
validator exception or install failure leaves this group unavailable and native/provider behavior in
charge. Log one bounded startup warning per skipped enabled patch: package/patch/spec ID, target,
failed predicate, unavailable behavior and fallback; no per-pawn/tick spam. Common UI states when
stored choices cannot be applied. Disabled features are not failures.

### Dependency, save and performance boundaries

Core and Harmony required when the hook is implemented; declare the dependency then. No DLC or
organ pack is required. No XML is needed for S046; S045 independently owns XML Extensions usage.
No upstream/private Harmony/Gateway assembly, source settings type, new pawn state or package spoof.
Native capacity caching remains authoritative; do not add a pawn/Thing index or scan the map during
capacity queries. No on-disk source settings import or saved-health conversion. No executable outside
consumer was found in bounded scans; do not create speculative public facades.

## Risks / Trade-offs

- Uncapped enhanced inputs multiply together → preserve documented defaults/ranges, compare real
  supported organs and ordinary impairment before acceptance; no economy-neutral claim.
- A weight edit can affect survival → explicit next-update timing, coherent native refresh and
  ordinary health consequences; no silent healing or forced outcome.
- Source cap-on transformation is malformed → implement its documented intent and prove valid
  incoming/emitted semantics rather than preserving the defect.
- Cached capacities can disagree with new policy → one staged/applied policy and native dependent
  invalidation; exact lifecycle seam remains implementation research.
- Native/API/provider changes → narrow fail-closed admission, retained foreign behavior and bounded
  warnings instead of whole-method fingerprinting or unpatching others.

## Migration Plan

No automatic source configuration import or file rewrite. Existing native health and capacity state
remains native; own settings use the common global/save schema. Missing entries inherit global.
Save switching must restore the correct policy and dirty affected native values before ordinary work.
Changing/removing providers may change health outcomes and is not claimed reversible or universally
safe mid-save. Any later source import requires explicit backup, atomic publication, rollback and
verified postconditions. This proposal adds documentation only.

## Open Questions

CO-Q1–4 in the refinement ledger retain effective admission, coherent native refresh, finite provider
domains/finalized balance and implementation/live acceptance. Observable source behavior and selected
defaults are resolved; these are future technical proof obligations, not requests for explanations
of the existing mod. S045 ordering is resolved at contract level, pending combined runtime proof.
