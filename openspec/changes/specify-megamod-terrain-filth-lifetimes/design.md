## Context

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
S105/P2-35 replaces `Owlchemist.PermeableTerrain`. Installed Workshop3261312042 is publicly titled
Permeable Terrain Continued by FerrisCG; About still says Permeable Terrain/Owlchemist and supports
through1.6. Native1.7 folder fallback selects1.6 plus root. The exact DLL, bundled source and original
public revision were inspected separately; this is not a1.7 compatibility claim.
[Research](../../../mods/megamod/docs/refinement/2026-11-05-permeable-terrain-research.md) records
identities, numerical rules, XML defects, optional providers and native lifecycle. No implementation.

## Goals / Non-Goals

**Goals:** Preserve creation-time terrain-sensitive aging and the separate other-filth multiplier;
retain current provider and native rules; use common save inheritance; avoid repeated scaling and
numeric permanent-stain defects; bound diagnostics and measure actual cost.

**Non-Goals:** Per-tick absorption simulation, retroactive map cleanup, rain/cleaning/filth amount
changes, provider-wide lifespan rewrites, package spoofing, copying assemblies, a private filth
index, a stand-alone settings implementation, or an unsupported optimization claim.

## Decisions

### Adjust the actual lifetime assignment rather than every spawn

Native1.7 `Filth.SpawnSetup(Map,bool)` initializes `growTick` and `disappearAfterTicks` only when
`!respawningAfterLoad && !BeingTransportedOnGravship`. The source postfix tests only the former,
so a gravship transfer can multiply an already adjusted duration again. The exact assigned field is
a duration, not an absolute deadline; `TicksSinceThickened` is compared against it later.

Prefer a guarded helper at the fresh assignment: consume the native sampled duration, current filth,
actual map/cell and effective settings once. A narrowly admitted incoming-IL transformation can
adjust the assigned value without a global field rewrite or per-object tracking dictionary. A
postfix is acceptable only if its executable target spec proves the original assignment actually
occurred and its preconditions still hold, including skip-original/override interactions. Do not
infer freshness from value differences, zero age or a `respawningAfterLoad` flag alone. Preserve
native RNG consumption; never draw another lifetime or reevaluate the whole method speculatively.

`FilthMaker` thickens an existing same-type stain rather than spawning another. `ThickenFilth`
resets `growTick` but preserves the assigned duration. `ExposeData` persists both and thickness.
`SteadyEnvironmentEffects.DoCellSteadyEffects` uses strict age>duration, zero means no natural
expiration, and rain can thin first. Keep all these paths native. No extra Tick/cleanup loop,
retroactive rescaling, custom filth class or migration registry is needed.

### Resolve current surface and definition classification once per assignment

Use the actual `TerrainGrid.TerrainAt(position)` contract, not a cached cell or buried terrain.
In inspected1.7 it resolves temporary terrain first, then exposed foundation when there is no
under-terrain, otherwise top terrain with a native fallback. `FilthMaker.CanMakeFilth` has separate
placement/foundation rules; do not bypass or replace them. A changed floor affects future lifetime
assignments only.

Static classification and explicit compatibility declarations belong in owned Def/XML data with
XML Extensions existence/shape guards. Use unique additive extensions, preserve other extensions
and properties, and avoid adding duplicate Ice entries. Resolve finalized inherited values rather
than matching raw element presence: source XPath `[takeFootprints]` also matches False and may miss
custom inherited semantics. Generic liquid classification follows the exact native fluid cleaning
sound `Interact_CleanFilth_Fluid`, not guessed English labels. Its broad family includes machine
bits; retain that source behavior explicitly instead of silently narrowing by intuition.

Precedence: one supported explicit surface rule, then Ice0.5, then enabled footprints/StrawMatting/
PackedDirt0.1, otherwise no permeability. Explicit liquid membership or finalized fluid-sound
membership qualifies filth. Identical redundant declarations collapse; contradictory equal-priority
declarations, missing required shape and nonpositive/nonfinite explicit fractions mark the affected
rule unavailable. Do not turn that failure into a false non-liquid classification and apply Other.
Generic capability-based classification includes ordinary mod content without a hardcoded provider
allowlist; specifically named exceptions require the actual package and content identity.

Use the existing owned finalized-Def catalogue where suitable, or small local read-only descriptors
until another real consumer needs sharing. Rebuild on definition/admission generation changes.
One map surface lookup and bounded definition/settings resolution at assignment suffice. No pawn,
Thing, filth or per-cell index is warranted; do not introduce a cache merely because the intake asks
whether optimization is possible.

### Preserve zero and perform arithmetic safely

For an admitted newly assigned positive duration D, liquid+permeable uses D*p*modifier; other
valid combinations use D*otherModifier. A zero ordinary duration remains zero. Use finite checked
arithmetic before conversion, truncating positive products toward zero and then clamping to at
least1tick. If the input/result is invalid or cannot fit the native positive duration representation,
leave the ordinary value unchanged and issue one bounded diagnostic for that failing rule. Never
cast overflow/NaN into a negative duration or round a positive duration into zero's permanent meaning.

This is a declared correction to source casts. Preserve source UI bounds: modifier0.25–3 and
otherModifier0.01–3, defaults1. Common validation rejects interactive invalid edits; invalid stored
entries use their built-in1 default without discarding other entries. Fractions and source-default
ranges are in the observable spec, including the surprising longer-than-normal ice case at300%.

### Keep common settings separate from per-stain state and diagnostics

Two common descriptors under Environment and cleaning supply global/save/Use global resolution,
loading, persistence and rendering. Native per-stain duration/age is gameplay state, not a setting.
Settings commits affect subsequent assignments only. Resolve the current save at assignment, not
from the active camera map or a cached previous save; globals apply when no game exists. Main-menu
save callbacks reject mutation. Retain dormant overrides and explain future-only application.

The source's globals `modifier`/`otherModifier` and its config file are not imported. A deliberate
future import would need its own provider/version mapping, backup, atomic commit, failure restoration
and native postconditions. Existing adjusted durations survive save/reload and source removal through
the native field, without an attempted reconstruction of their unknown original samples.

Replace the source settings-page debug checkbox and automatic startup terrain listing with the
owned top debug-toolbar report and trace-next20 action. This is transient diagnostic state, cleared
on developer-mode exit/teardown and never a save setting. Record fractional days or ticks, since
the source integer day division hides short durations. Respect the existing bounded logging facility;
no per-spawn allocations/log formatting when tracing is off. Seven baseline languages are required.

### Compose optional content without the legacy destructive patch

Use `XmlExtensions.FindMod` with `packageId=true` for actual external mappings. XML Extensions
becomes required at the first operation; Harmony is required for the native lifetime hook. Never
bundle the source's included0Harmony.dll. All external adapters are absent-safe and shape-guarded.

- **ReGrowth**, `ReGrowth.BOTR.Core`, inspected5.3-rev3/Workshop2260097569: current title ReGrowth2
  differs from the source's display-name test ReGrowth: Core. Selected1.7 load folders use1.6.
  Its BaseFilth14–15day/rain changes and explicit per-filth intervals stay authoritative. Do not
  remove its inherited interval or re-add historic45–50/35–40 ranges. The source's old patch can
  duplicate oil-smear data and repeats building-rubble in its selector instead of covering slag.
  These are omissions/deviations from legacy compensation, not claims of equivalent XML.
- Explicitly classify `RG_FilthWater` and `RG_FilthWaterSpatter` as liquid when their actual package,
  Filth-derived type and finite ordinary duration contract are supported; preserve their cleaning
  sound. Source finds them by English label and appends a sound node; owned classification does not.
  `ReGrowthCore.PuddleFilth.TickLong` can add2000ticks when rain>0.1. Do not scale those later
  extensions or replace the method. Exact selected1.7 execution remains an acceptance gate.
- **FSF**, `FrozenSnowFox.FilthVanishesWithRainAndTime`, Workshop1508341791: inspected1.7 XML
  changes long intervals to14–15days and rain flags with exclusions. Accept the finalized baseline
  and multiply once. Its published250% compensation suggestion is a possible player choice within
  existing bounds, not an automatic preset. The separate FSF Tweaks variant needs its own identity/
  enabled-setting inspection if claimed later; no blanket FSF compatibility is inferred.
- **Dubs Bad Hygiene**, `Dubwise.DubsBadHygiene`, Workshop836308268: inspected1.7 `FilthUrine`,
  `FilthFaeces`, `RawSewage` have10–15days but no fluid cleaning sound or Permeable Terrain extension
  in inspected XML. They use Other unless another admitted declaration classifies them. No extra
  semantic urine mapping is silently added on the strength of a Steam comment.

Core `LiquidFuel` has an independent1500tick drying path. Base `Filth` classification does not
override that path. Preserve it, and ordinary firefoam/fuel effects while the stain exists. Owned
vomit, cleaning/construction and fire features compose through native creation/removal and direct
policy calls if needed, never self-patches or a second aging job.

### Public contracts and provider authority

The source documents public `PermeableTerrain.Permeable : DefModExtension` with float `value`, and
`Liquidy : DefModExtension` whose float `value` is actually ignored: membership alone matters.
Its examples use `MayRequire="owlchemist.permeableterrain"`. The bounded installed XML/C#/DLL and
local Mods searches established only the source's own declarations, no actual foreign consumer.
This does not prove reflection/UTF16 or future consumers absent. Owning equivalent metadata does
not satisfy legacy MayRequire without the original package; never spoof it. No speculative class/
assembly facade is planned. Record any proven new consumer and migration need before implementation.

Original and continuation share `Owlchemist.PermeableTerrain`, so one package gate suppresses our
entire adjustment group and dependent owned classification patches. Keep choices dormant and explain
source authority. Do not undo foreign XML, remove foreign patches or rescale stored durations.

### Target specs and failure boundaries

Before code, index target specs for every actual native hook and classification/compatibility XML
operation. Prove the native assignment's duration units, RNG result, map/surface context, load and
transport bypasses, and subsequent ordinary age consumer. A method name/signature alone cannot
admit changing this value. Check incoming effective IL on every transpiler run, tolerate unrelated
edits and compatible owners, account for skip-original/relevant prefixes, and return an unchanged
stream on failure. Do not execute foreign transpilers as probes or use hashes as admission predicates.

Prove static classification completion before applying a branch. Global native-path failure disables
all owned lifetime adjustment; a provider/definition mapping failure disables only affected cases
when they can be identified safely. A partial XML batch must not leave behavior active under an
incomplete classification. Unknown predicate, validator exception or install failure rolls back
only newly added owned hooks in the group. Revalidate supported later patch changes, stop new
adjustments on loss and retain already stored native durations. Emit one startup warning per skipped
enabled patch with package/patch/spec ID, target, reason, unavailable behavior and fallback; disabled
paths and absent unselected content are not failed candidates.

## Risks / Trade-offs

- **Once-only source behavior conflicts with new transport lifecycle** → Adjust actual fresh
  assignments, prove the transport bypass, and test repeated load/transfer without a private timer.
- **Source calls a broad sound-based family liquid** → Explicitly retain machine bits and other
  ordinary members; allow declared content mappings without hidden name-based expansion.
- **Straw/soil cleanliness and firefoam duration affect play** → Preserve source fractions, costs,
  cleaning and hazard rules; compare native and provider values before balance acceptance.
- **Current ReGrowth differs from historical compensation** → Preserve current baseline/rain,
  narrowly classify puddles and document changed outcomes rather than copying destructive patches.
- **Optimization may add more cost/state than it removes** → Measure assignment rate, classification,
  allocations and combined consumers against native and source baselines before claiming improvement.

## Migration Plan

This slice installs nothing. Later implementation uses common settings and native saved duration
fields only. Existing stains remain as saved; future assignments use current rules. On disable,
original-provider suppression or admission loss, future assignments remain ordinary and already
stored values are retained. No migration import or retroactive rescale is claimed.

## Open Questions

- **PFT-Q1:** Exact effective1.7 assignment/inheritance/transport/consumer contracts, repeated hook
  admission and provider subclass behavior; fail-closed grouping and optional XML execution shape.
- **PFT-Q2:** Actual foreign public-extension/reflection consumers, further optional label-based
  cases, FSF Tweaks identity/settings and finalized ReGrowth/FSF compositions.
- **PFT-Q3:** Measured spawn cost versus native/source, shared descriptor lifecycle and whether any
  optimization is worthwhile; native soil/ice/floor/firefoam/provider balance and seven-language UI.
- **PFT-Q4:** Focused RED/GREEN, reviewed build and causal native creation/aging/cleaning/rain/
  thickening/save/gravship/provider workflows; no gameplay acceptance from stat or timer mutation.
