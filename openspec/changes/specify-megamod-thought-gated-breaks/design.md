## Context

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
S119 replaces Dubs Break Mod, `Dubwise.DubsBreakMod`, Workshop1722398508. Public documentation
was inspected before the selected1.7 DLL. The public repository contains binaries, metadata and a
README, no source project or license grant; its pinned1.7 binary matches the installed input.
[Research](../../../mods/megamod/docs/refinement/2026-11-06-dubs-break-mod-research.md) records identities
and the complete four-type implementation. No source code or bundled Harmony is to be shipped.

The source prefixes `MentalBreaker.CurrentPossibleMoodBreaks`, obtains current distinct mood
groups, computes their `MoodOffsetOfGroup`, chooses a maximum supported intensity and calls native
`GetBreaksForIntensity` with the lower of that ceiling and `CurrentDesiredMoodBreakIntensity`.
It does not alter the native opportunity test, direct mental-state starts or recovery. Four global
settings have initializers−10/−15/−20/false; missing serialized threshold keys instead use0.

Current native1.7.5000rev1275 has an important exception: `GetBreaksForIntensity` first yields
eligible `pawn.Ideo.cachedPossibleMentalBreaks` without checking requested intensity, including
None. Only its ordinary branch walks the requested intensity downward and handles anomalous
fallback. Merely reproducing the source prefix would not enforce its general severity promise.

## Goals / Non-Goals

**Goals:** source-derived grouped-thought ceiling, source scope/default controls, preserved native
timing and weighted selection, coherent ideological ceiling correction, current owned fading,
common settings/localization and narrow guarded integration.

**Non-Goals:** modifying moods/thought definitions, changing break probabilities or recovery,
suppressing every mental state, redesigning native warnings, new content/tab, guessed optional
compatibility, source migration, copied assemblies/code, implementation or game acceptance now.

## Decisions

### Use native current groups and one pure severity policy

Use the current `ThoughtHandler.GetDistinctMoodThoughtGroups` and `MoodOffsetOfGroup` relationship
under a minimal admitted adapter. It incorporates active memory/situational groups, native stacking,
nullification and current overridden `MoodOffset` results. Do not use `Thought.cachedMoodOffsetOfGroup`
as an authoritative persistent cache or reconstruct native memory math from base Def values.
Opinion-only thoughts do not become mood contributors. Compare actual float values without UI
rounding. An empty collection supports None; a zero configured threshold still needs an eligible
group whose current effect is nonpositive.

The host-safe calculation accepts finite group effects and three validated integer thresholds.
For each group, test Extreme, then Major, then Minor; choose the highest supported tier across
groups. Equivalent pure reduction is fine. Bound final selection by native requested severity.
The independently editable source limits need not be monotone: meeting Extreme authorizes all
lesser tiers even if their own thresholds are stricter. Preserve this observable contract and explain
it, rather than silently sorting/clamping controls or changing old expectations. No synthetic
positive/infinite values on missing data. A failed provider evaluation has bounded fallback, not
partial decisions based on whichever groups happened to finish.

Use a per-evaluation scratch collection with guaranteed cleanup and reentrancy-safe ownership.
No map-wide polling, stored per-pawn worst-thought cache or Thing index. Native current data is the
shared source; S068 memory fading contributes through its normal owned effect calculation exactly
once, S024 changes ordinary mood/needs upstream, and S100 acts only after an episode exists.

Alternative: sum all negatives or take the raw worst individual memory. Rejected because the source
uses groups and their actual modified contribution. Alternative: read displayed rounded values.
Rejected because visible formatting must not change threshold eligibility.

### Limit the selection seam without rewriting mental-state scheduling

Keep native `TestMoodMentalBreak`, `CanHaveMentalBreak`, timing counters, cooldown and downstream
`TryDoMentalBreak`/`TryStartMentalState` untouched. The owned behavior belongs only to the ordinary
`CurrentPossibleMoodBreaks` path. Direct `TryGetRandomMentalBreak`, explicit break starts, incident/
trait/gene/ability/debug paths remain outside scope, even when another caller supplies a mood flag.
Do not globally replace `CurrentDesiredMoodBreakIntensity`: native nonplayer eligibility also uses
it, so changing that property would silently change which pawns may break.

At that selection seam, obtain desired intensity and one current thought ceiling; use their lower
value. None means an empty candidate set, including the ideological shortcut. Preserve one native
anomaly choice for the evaluation and native weighted selection/final-straw handling. At current
native timings, a pawn requesting Extreme but capped to Minor still uses the Extreme opportunity
rate; do not reset counters or reschedule with Minor's rate. Native risk alerts/portrait thresholds
remain based on mood; explain this in settings rather than patching unrelated warning code.

Prefer the smallest semantic insertion that substitutes the capped intensity at the native
candidate call and scopes the ideological-branch admission to this evaluation. Preserve native
ordinary candidate iteration and fallback, rather than cloning all present/future worker rules.
The concrete iterator/call shape must be finalized before implementation. A local wrapper may own
one lazy enumeration and a narrowly scoped context; it must dispose/clear context on success,
exception and early enumeration exit, never leak policy to direct helper calls or another pawn.
Repeated enumeration is a fresh evaluation, not a cached random candidate set. Never consume an
iterator speculatively to decide whether to run it again.

The same-build `GenCollection.TryRandomElementByWeight(IEnumerable<T>)` consumer makes one pass
using reservoir selection, but does not dispose its enumerator or catch `CommonalityFor`/weight
exceptions. An iterator `finally` alone is therefore insufficient: scope contextual admission to
each `MoveNext` (restore before yielding), or use an admitted outer selection cleanup boundary.
Cover a weight exception while enumeration is suspended and confirm the next direct/other-pawn
helper call is unaffected. Keep ordinary selection mechanics; do not convert to a persistent list
or add another weighted-selection pass just to manage context.

### Apply the ceiling to the ideological shortcut too

During the owned selection only, accept a native ideological candidate if ordinary `BreakCanOccur`
succeeds and its declared severity is recognized Minor/Major/Extreme and at most the effective
ceiling. Unset/unknown severity cannot retain preference. Set the native
"ideological candidates available" result only from those retained candidates. If none remains,
allow the normal ordinary branch rather than returning an empty result merely because a blocked
ideological candidate existed. Keep eligible ideological preference and weights. Do not filter
every call to the general native helper or mutate an ideology's cached definitions.

This is a deliberate correction to source behavior. No installed static Core/DLC XML use of
`PreceptComp_MentalBreak` was found in the bounded inspection, but the executable native branch
exists and dynamic/modded precepts can populate it. Do not invent a currently affected Core precept
or call this a reproduced live bug. Final optional-provider evidence remains DB-Q2.

Ordinary candidates retain the native same-tier eligibility, anomaly preference/non-anomaly fallback,
and lower-tier search if that pool is empty. Do not treat zero selection weights as permission to
invent a new fallback: ordinary weighted selection can fail without starting a state. No second
roll of `ChooseAnomalyBreak` per tier. Config-invalid/unknown severity providers need a narrow
absent-safe adapter or unavailable group, not a blanket skip of unrelated mods.

Alternative: filter only the final source result. Rejected: the ideological early return may
already have hidden ordinary fallback candidates. Alternative: suppress every ideology break or
mutate its cached set. Rejected: unnecessarily changes ordinary ideology behavior and other callers.

### Preserve native scope and current entity facts

Default ColonistsOnly=false. True follows `Pawn.IsColonist`, not a home-map/faction-only test or
`IsFreeNonSlaveColonist`. Inspected current classification requires player faction, humanlike,
not subhuman, and secure status when enslaved; it does not exclude a player colonist just because
HostFaction/location changed. Reevaluate at selection. Missing usable mood/handler data bypasses
the owned thought calculation; it does not add a tracker or relax native break eligibility.

No feature-owned per-pawn persisted state is needed. Native thoughts, episode/timing state and
common save settings survive ordinary load/transfer; don't replay offline checks, reset cooldown,
clear active episodes or retain worker/Pawn references across teardown. Death simply ends use of
that native pawn; counselling/mental-state recovery remains with S100/native systems.

### Four common settings and honest UI

Register ordered Minor/Major/Extreme integer rows−100..0, defaults−10/−15/−20, then Colonists only
off. Common validated globals/save/Use-global resolution is authoritative; do not copy the source's
initializer-versus-Scribe0 mismatch or its no-op ResetSettings method. Invalid interactive edits
leave the prior value; corrupt saved values follow common fallback with bounded diagnostics.
Read a coherent effective configuration once per evaluation. A settings edit affects the next
evaluation, never directly starts/ends a state or resets native timing.

Use common main-menu disabled save controls/callbacks, active-save world view, dormant overrides,
new-save inheritance from current globals and save switching. No exception from the shared model.
Source `LastVersion` and unused `optionsWindow` are not gameplay contracts to reproduce. No legacy
settings import is authorized by merely finding those keys.

All seven languages cover group meaning, negative limits, equality, unusual ordering, native
colonist scope, timing/warning distinctions and source-selected unavailability. No new bottom tab,
operational toggle, debug action or per-pawn setting. Preserve final-straw choice: native code
selects from individual negative thoughts, not necessarily the qualifying grouped ceiling.

### Minimal target admission and provider selection

Before implementation, document stable-ID target contracts for the ordinary selection seam,
thought APIs, exact intensity enum/content, native ordinary fallback and ideological shortcut,
plus any chosen wrapper/context cleanup. Required semantics form one coherent feature group; an
unproved ideological branch cannot be silently claimed to obey the ceiling. Check actual effective
flow and incoming transpiler streams, not just method names, exact binary hashes or no other owners.
Compatible additions/edits must pass. Each transpiler validates its incoming stream on every run,
returns it unchanged on failure and never executes foreign transpilers as probes.

False/unknown checks, validator exceptions or partial install leave the owned group inactive; roll
back only newly installed owned patches. Emit one bounded startup warning per skipped enabled patch
with package/patch/spec/target, failed predicate, unavailable behavior and fallback. Idempotence and
supported later invalidation/revalidation cannot double gate, consume extra RNG or replay starts.
Current-thought failure before candidate side effects can return native evaluation once. If candidate
evaluation already consumed randomness or invoked potentially stateful foreign checks, abort that
opportunity with bounded diagnostics, disable the affected unsafe path, and use fallback on later
opportunities; do not rerun/compensate an ambiguous selection.

Detect `dubwise.dubsbreakmod` case-insensitively by actual package identity; when active, select the
original and disable this owned overlap with unavailable controls and a bounded provider notice.
Do not unpatch the source, spoof its assembly/package or promise the owned fixes on source-selected
games. Bounded text/binary searches found only source versions, RimPy catalog/load-after Common
Sense metadata and layout test modlists, not a proven external callable consumer. No facade for
the source's public Settings/Prefix types is justified by those mentions. Revisit actual consumers
if later found; generic compatible thoughts/worker extensions remain supported inputs.

Required Harmony is declared when used, not copied from source. No XML is required for this runtime
selection policy; future declarative changes require XML Extensions content/shape guards and its
dependency. No source/DLC, other repository mod or Dev Gateway dependency. No product files change
in this documentation pass.

## Risks / Trade-offs

- Thought grouping can combine repeated small events → specify actual groups and current modifiers,
  not the misleading promise that every qualifying cause is one unstacked memory.
- Reduced severity keeps severe-mood opportunity rates → explicit spec/settings explanation and
  combined playability checks with fading/recreation; do not claim unchanged difficulty.
- A native shortcut ignores intensity → scoped semantic correction with preserved fallback and
  concrete optional-provider validation; no new global candidate cache or mutated ideology state.
- Native warning/final-straw text differs from the ceiling → retain native facts and explain scope;
  no claim the final-straw message is the unique gating cause.
- Modded lazy candidate evaluation can have effects → single enumeration/choice, bounded cleanup,
  no speculative probes/retry after an ambiguous partial evaluation.

## Migration Plan

No source settings/save conversion. The source stores global configuration and patches a selection
getter; this feature adds only common configuration. Preserve native existing mental states and
timers across ordinary load/removal, but do not claim safe removal/migration until its workflow is
specified and tested. Any future conversion requires backup, atomic application, restoration on
failure and verified postconditions. No normal user save/config write occurs here.

## Open Questions

DB-Q1–4 in the [ledger](../../../mods/megamod/docs/refinement/specification-progress.md) retain
effective iterator/shortcut admission, concrete optional/provider and boundary consumers, finalized
combined playability/UI/performance, and implementation/reviewed native acceptance. Record gaps for
later instead of asking the user to explain existing source behavior.
