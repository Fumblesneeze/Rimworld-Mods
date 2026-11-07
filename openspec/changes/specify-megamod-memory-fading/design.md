## Context

Sole owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, **1.7 beta**.
S068 source 1.7.0 mutates shared ThoughtDefs after startup and settings edits. Complete installed
binary and selected 1.7.4998 native evidence are in
[research](../../../mods/megamod/docs/refinement/2026-11-04-all-memories-fade-research.md).
This is an unimplemented design; exact executable patch targets remain MF-Q2.

## Goals / Non-Goals

**Goals:** one fade for supported timed memories; stable unmodified-duration selection; source
strength/duration controls with isolated save settings; finite expiry behavior; explicit counselling,
social and override semantics; native presentation and actual mood consequences.

**Non-Goals:** rewriting memory acquisition/stacking, global Def restoration, changing withdrawal or
ongoing situational effects, opinion-strength scaling, a separate thought store/ticker, source facade
or settings import, and arbitrary custom-memory parity without an admitted adapter. No new bottom
tab, raster art, map index or implementation in this change.

## Decisions

### 1. Evaluate in the owning game's context; do not mutate shared definitions

Source dictionaries capture values once and restore them globally. That cannot represent inherited
per-save settings and can overwrite later provider edits. Use current game-owned settings plus
current admitted native/provider inputs. Do not temporarily mutate a shared ThoughtDef during a
query, even with restoration; nested evaluations and save switching make that unsafe.

The proposed ordinary calculation receives current underlying base mood and baseline lifetime,
instance age/power/offset, nullification, permanence and authoritative override status. Inclusion
uses positive baseline duration and the inclusive configured range, before our duration scaling.
For definition-timed memories, scale duration once. Explicit instance durations are authoritative
and not multiplied again; use that explicit duration for eligibility and fading. Permanent,
nonpositive-baseline and unsupported contexts keep their provider behavior.

Base mood scaling occurs before native stat/worker and instance factors. Independent instance mood
offsets remain unscaled. Impose one clamped remaining-life factor on the resulting included mood
contribution, replacing an already-native fade rather than multiplying a second one. Preserve
nullification. An included effective duration of zero gives a zero contribution, never division by
zero; native memory maintenance performs actual discard/successor processing. Native renewal,
stacking and memory identity remain. Do not restart elapsed age for a setting edit or reload.

Ordinary social memories use the same duration policy but their own native opinion interpolation;
no opinion multiplier or replacement curve. Counselling is the separate case below. Avoid basing
class support solely on IsMemory: inspect the actual effective mood/duration/discard paths.

### 2. Minimal runtime bridges with explicit semantic admission

Shared configuration is dynamic and per-save, so declarative XML is not the right implementation
for these runtime calculations. Harmony is a design-time fallback where no supported public
extension seam exists; required on eager use. No XML patch or XML Extensions dependency now.

| Proposed target family | Required semantic proof / boundary |
| --- | --- |
| `MM.Memory.Policy` | Native base mood use, instance factors/offset, existing fade branch, effective duration/override and zero-discard lifecycle; ordinary policy is coherent as one group |
| `MM.Memory.Counselling` | Optional actual Counselled class/Def, captured amount/lifetime and fallback boost award/message path; one captured override path, no double multiplication or mismatched granted amount |
| `MM.Memory.Presentation` | Current settings/catalog previews, native memory displays and supported numerical event messages; no fake mood calculation or raw-baseline message presented as actual awarded mood |

These are **families, not completed per-patch target specs**. Before code, document every actual
hook and exact target condition, affected behavior/fallback and revalidation boundary. A getter
postfix that changes a duration must prove what its result represents, explicit override handling
and actual downstream use. A base-mood hook must prove that changing its value preserves current
stat/nullification/additive semantics. Observation-only hooks may need only a callable shape;
behavior-changing hooks require the relevant effective data/control flow.

Potential seams are Thought.BaseMoodOffset, Thought_Memory.DurationTicks/MoodOffset and the native
expiry path. Do not blindly patch the shared ThoughtDef duration property, which has no game/pawn
context. Do not replace an entire native mood method merely to reproduce its body. If a narrow
transpiler is genuinely necessary to bridge the fade branch, validate actual incoming instructions
every run, accept unrelated instructions/foreign patches, and return them unchanged on failure.
Never run foreign transpilers speculatively. Native/provider calculations must not be recursively
re-entered to obtain an imagined unmodified result; pass already resolved inputs through one policy.

If a core ordinary-policy prerequisite fails, leave owned ordinary fading/strength/duration inactive
as a coherent group; native/provider behavior remains. Roll back only newly owned patches in that
group and leave independent features available. Counselling depends on the ordinary policy and is
separately content/shape-admitted; if unavailable, native counselling remains and its limitation is
explained. No forced input/age mutation merely to make a failed route appear functional.

Checks use minimal effective semantics, not exact hashes/versions or an unpatched-method rule.
Warn once at startup per skipped enabled patch with package/patch/spec, target, failed predicate,
unavailable behavior and fallback. Exceptions and unknown shapes fail closed. Avoid logs per mood
query, pawn or tick. Supported later repatching/provider changes invalidate and revalidate relevant
adapters without undoing other owners. Catalogue unsafe content once with bounded reasons; a bad
stage/subclass is not permission to disable unrelated supported memories or crash startup.

### 3. Counselling uses its captured result once

Current native success captures the negative memory's current mood magnitude and remaining
duration. Thought_Counselled overrides MoodOffset directly and has a zero-duration Def, so the source
does not fade it. Admit that exact optional path and use its captured amount as the full initial
contribution, fading over its actual override lifetime when that lifetime is included. No strength
or duration multiplier is applied again: the target's current amount/remaining time can already
contain them. No custom class replacement, target-memory link, second save store or duplicated
ability effect is needed. Existing saved counselling also has amount, age and lifetime available.

This extends source coverage rather than preserving its fixed positive compensation. It does not
prove or reproduce an endless-duration report. If membership/settings change or the original
negative memory is renewed/removed, preserve native snapshot semantics; the captured award is not
recomputed. A counselling lifetime outside the configured range retains native behavior even when
the original negative is included. Preview/explanation must not promise universal exact neutrality.
Normal successful fallback boost and failed counselling are ordinary native timed memories.
The optional bridge needs canonical Ideology activation plus actual content and method-shape checks.

The actual eligibility test selects current mood at or below CompProperties_AbilityCounsel's
minMoodOffset (inspected default −10). A −9 penalty is still negative but takes the fallback boost;
preserve current configured eligibility, not a new any-negative rule. The fallback success message
currently formats MOODBONUS directly from its stage's raw baseMoodEffect. Unlike the source's global
mutation, contextual scaling leaves that field unchanged. The native action's numerical message
must therefore use the admitted actual awarded contribution, not that raw baseline. A +8 base at
200% strength is +16 initially without other modifiers. Guard the exact award-to-message path and
its current arguments, not all translated messages globally; no shared Def mutation or fabricated
second award. If that bridge cannot be admitted, decline the owned transformation for this affected
boost content coherently and explain the native fallback, while other ordinary memories remain
supported. Preview eligibility must reflect that narrower unavailable content. This presentation
dependency belongs in the optional counselling group and its tests.

S038 can refuse the addition itself. Native TryGainMemory returns void, so the attempted input is
not proof of an award. Compose with the applied content policy and actual native acquisition outcome
for both captured Counselled and fallback Counselled_MoodBoost. Preserve the refusal and report no
granted bonus; never synthesize compensation, remove the original penalty or infer success from the
ability's successful random roll. Existing instances still use their current policy. Do not use
pending S038 requests as if they were applied acquisition rules.

Falling back to native success text alone is unsafe when the applied policy denies the award. If
truthful outcome reporting cannot be admitted, decline the affected counselling action through an
admitted native eligibility/activation boundary before cost, cooldown or effects, with an explanation.
If that boundary also cannot be admitted, the coupled S038 exclusion group must remain unavailable
at policy application; retain the requested choice and explain that it is not applied. Admission
must establish one coherent route before play, with later supported invalidation handled likewise.
This is direct owned composition with S038's unavailable-group contract, not a self-patch or permission
to silently ignore an applied exclusion. Other content families and ordinary memories remain usable.

### 4. Common settings, timing and previews

Use common definition/load/store/effective-value/rendering infrastructure in **Mood / Memories**:

| Setting | Built-in default | Validation / scope |
| --- | --- | --- |
| Base mood strength | 100% | Finite 0–200%; applies to ordinary base mood, not opinion or captured counselling |
| Ordinary memory duration | 100% | Finite 0–200%; explicit instance lifetimes remain authoritative |
| Included baseline durations | 0 days through Unlimited | Nonnegative finite minimum; finite maximum >= minimum or explicit unlimited; inclusive |

Use a single range value so bounds commit atomically. New saves inherit. Global changes affect
inheriting saves; dormant overrides survive checking Use global and are restored when unchecked,
or seeded from current global on the first override. At the main menu all save editors, inheritance,
reset/import and mutation callbacks are disabled. Reset is explicitly scoped, using the common
definition defaults; no implicit reset of another column/save. While the original provider is
selected, owned policy controls are read-only and values remain dormant.

Accepted edits take effect on the next ordinary memory/mood evaluation, including existing memories,
without rewinding age. Keep displayed preview/current mood semantics aligned with native update
cadence rather than claiming immediate mood-bar animation. Shortening can expire an existing memory
at native maintenance; later extension cannot resurrect removed objects or replay their successors.
No global mutation means changing saves naturally selects their own policy. No custom aging clock.

Preview uses the current supported thought catalog, native translated labels and actual relevant
base values. Count types, not existing memory instances; separately explain dynamically timed effects.
Display ordinary/social/explicit-lifetime distinctions, baseline versus effective duration and
inclusion. Native finite precision/clock limits must not wrap values into negative/instant durations;
decline unsupported overflow per content with a bounded explanation. Do not shrink a user's range
when their duration percentage changes or bind Unlimited to a scanned longest current duration.
No-result and missing/null-stage rows are safe; unsupported rows are explained and not counted as
fully supported. No fabricated hardcoded historical examples or private Def names as primary labels.

No new feature toggle: the source always fades included memories, and its range is the exclusion
control. These are configuration rows; pending memories are pawn gameplay state displayed natively,
not per-pawn settings. No new bottom/debug tab or bottom-right control. Seven languages required.

### 5. Data, contracts and direct composition

Use native memory collections and common settings/catalog data. Any derived catalog snapshot is
keyed by current content and policy generation, rebuilt on the relevant change, and never confused
with runtime pawn eligibility. No map-wide pawn scans, per-port Thought lists, live references in
global settings or extra ticking subsystem. Do not involve the shared Thing index just because other
features use it. Measure cost with current mood/needs displays before claiming an optimization.

Source public static update/query API has no runtime consumer found in the scoped installed XML/CS/
JSON scan. Found package mentions are RimPy catalog data and YART sample mod lists. No facade,
source assembly/package spoofing or settings import is justified. Preserve native memory classes,
identity, save fields, relations and expiry behavior instead.

When `mlie.allmemoriesfade` is active, select the original as sole provider for this feature; owned
calculation patches are inactive and controls explain that the original uses global settings.
Do not overwrite its mutated Defs, run both multipliers or silently change its settings. This is
intentional provider selection, not incompatibility inferred from any foreign patch owner. Other
providers are evaluated by effective semantics; custom paths require narrow admitted adapters.

S001/S003 and ordinary Needs UI read current native mood. S037 and all other memory-producing
features retain their own events, not internal patches. Distinguish physically absent definitions
from S038 acquisition restrictions: S038 keeps existing memories, so their types remain visible in
the fading catalog/count with a separate new-acquisition status and continue under the current policy.
Use S038's applied policy; show pending requests separately until its save/reload application boundary.
No content restriction becomes a fading-eligibility filter. Actually missing definitions cannot
provide previews, but exclusion alone is not evidence of absence.
S124 Drug Response remains queued, and no drug/withdrawal mechanics are imported from a loose
documentation example. No product runtime dependency on another repository mod or the Gateway.

## Risks / Trade-offs

- Default fading changes both pleasant and painful memories → preserve 100% strength/lifetime,
  document changed total impact and verify real mood; no universal stability claim.
- Social lifetime tuning affects relationships → explain this separately from unchanged opinion
  strength/curve and exercise native Social displays as well as Needs.
- Special classes bypass native seams → narrow adapters and explicit unsupported fallback; no
  broad compatibility claim from a Def name or source package alone.
- Settings can shorten live memories → visible semantics, no age rewind or revival; native once-only
  expiry/nextThought paths must be observed on the reviewed build.
- Counselling amount may already include modifiers → captured-value exception prevents doubling;
  no new promise of live coupling to the original negative memory.

## Migration Plan

Keep native memory serialization; effective policy comes from the current save, with no source key
import. Existing saved memories retain age/overrides and are evaluated on load under current policy.
Removing owned behavior restores native calculations without deleting ordinary memories or rewriting
user saves/mod lists. Already expired memories remain gone; there is no historical restoration.
Provider-selected dormant settings persist but are not applied. Any later necessary persistence
change needs its own explicit migration/rollback contract before implementation.

## Open Questions

MF-Q1–4 cover proposed coverage corrections, exact effective target groups, custom/provider scope
and remaining settings/lifecycle/performance/native evidence. Record discoveries there; do not ask
the user to explain source behavior. Every implementation and acceptance task remains unchecked.
