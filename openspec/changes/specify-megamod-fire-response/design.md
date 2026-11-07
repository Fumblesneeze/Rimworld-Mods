## Context

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S079 reference is `balistafreak.StopDropAndRoll`, Workshop2362707956. [Research](../../../mods/megamod/docs/refinement/2026-11-04-stop-drop-roll-research.md)
records documentation-first inspection, root1.6 source/binary hashes, native1.7 evidence and source
deviations. No license permitting code redistribution was found; implement the specified behavior
independently, without source assembly/assets or package/type impersonation.

The source has two behaviors, not one broad fire-immunity feature: deterministic selection of the
native self-extinguish job for enabled groups, and separate apparel checks before new attachment.
Its three unguarded prefixes and per-draw settings writes are not the desired architecture.

## Goals / Non-Goals

**Goals:** reliable in-place action; source-derived independent group options and numeric defaults;
meaningful coverage/layer protection; native damage/incapacity/environment preservation; guarded
coexistence, common settings and explicit save/provider limits.

**Non-goals:** new roll animation or combat job, passive permanent immunity, damage negation, natural
armor becoming apparel, building/fire-spread changes, automatic water prioritization, source save
migration, generic race framework, per-map fire/pawn cache or unverified Combat Extended support.

## Decisions

### 1. Positive scope and native response

Resolve one of colonist humanlike, other humanlike or animal, otherwise no owned contribution.
Use actual native IsColonist semantics, including the inspected secure-slave distinction; faction
membership alone is not equivalent. Do not broaden other humanlikes to enemies only. Keep all
wild/owned animals in their one source-derived group. This fixes source negative-check fallthrough
into unclassified pawns without adding a fourth unrequested option.

Use the normal burning tree and native ExtinguishSelf job; preserve Downed/mandatory-bed priorities,
drafted interruption and its150-tick delay, fire target and record. Enabled response suppresses the
water choice and removes the native10% self-job selection gate as one atomic group. Do not mutate
the whole think tree, globally suppress panic/movement or set a pawn's fire/health state directly.
Admit the actual loaded tree ordering and JobDriver contract, not just method names. A missing
required action leaves both response contributions inactive. No custom duration setting is needed;
150 is the inspected native contract, not a new duplicated tuning constant.

The response does not grant a job to an incapacitated pawn outside native scheduling. If fire is
gone, the native target-failure path must settle safely, without repeated record credit. Save/load
uses the ordinary job/fire schema; no new pawn state or action timer is introduced. A settings
change affects the next choice, not forcibly terminating a valid ongoing native job.

### 2. Fix coverage sampling explicitly

The source uses nonmissing Outside records weighted by local coverage and excludes Legs along
with hand/foot groups. Documentation describes ordinary-hit selection and only hands/feet exemption.
The proposed contract follows that intent: eligible present outer parts, excluding hands/feet and
their descendants, **including legs**, with native flame-hit weights and their native fallback.
Record the balance change; do not claim byte-for-byte source parity. SR-Q1 retains review of this
proposal and custom anatomy coverage before code.

Use loaded native body/part/group semantics, not translated name matching or a hardcoded Human body.
Resolve the exclusion meaning through native part identity/ancestry and documented group tags;
empty/ambiguous/nonfinite/nonpositive total weighting returns native attachment without immunity.
Retain native missing/added-part semantics: verify actual present parts and compatible prosthetic
coverage rather than sampling every BodyDef record blindly. Source's throwing GetNamed("Feet") and
unguarded RandomElementByWeight are not acceptable fallbacks.

For selected part, use the effective apparel coverage contract and distinct actually worn items.
Native ApparelProperties.CoversBodyPart/loaded provider behavior must be inspected and guarded for
the admitted route; source's group overlap is evidence, not sufficient proof of every custom race.
Order by native effective layer ordering where required, preserving one check per physical garment.
Do not inspect an item's listed raw stat alone or cache a pawn's current clothing/health protection.

### 3. Pure probability, narrow attachment decision

Host-safe probability is clamp01(1-strength*(heatArmor-threshold)). At strength4/threshold0.25,
threshold and below pass,0.26 passes0.96,0.49 passes0.04,>=0.50 blocks. Strength0 always passes.
This is an independent ignition check; native damage/armor penetration and S075 direct-armor
calculation are not invoked again. Pre-resolve valid current protection for the attempt before any
roll so invalid data can fall back without having already granted partial immunity.

Each unique covering garment receives one independent roll, outermost inward, stop at first block.
No garment or no usable protection means native behavior, not forced ignition. Check only after
the native eligibility/no-existing-fire conditions for an actual new attachment have been established.
Do not consume random draws or suppress unrelated work for destroyed, nonflammable, already-burning,
unspawned/otherwise invalid native contexts. Native authority still decides its own admissibility.

Prefer a narrowly guarded decision at the proven attachment boundary. A prefix skipping an entire
TryAttachFire is acceptable only if effective semantics prove that all skipped work is exactly the
new fire and its dependent job interruption/record, with no unrelated provider effects. If another
provider adds required effects, integrate through its supported boundary or disable our ignition
branch; do not erase those effects. Passing preserves size, instigator, attachment/spawn, job stop
and record ordering. Blocking prevents that bundle without touching direct attack damage.

Existing Fire updates own environmental extinguishing, heat/spread and ongoing damage. No new fire
scan, water path search or local fire copy is needed. Candidate anatomy/apparel work is pawn-local;
reuse native collections. Any later shared immutable descriptor optimization must state consumers,
freshness and measured need; never add a private Thing index for this small operation.

### 4. Settings and UI

Use eight common definitions in `Combat / Fire response`, ordered Stationary response (colonist,
other humanlike, animals), then Ignition resistance (same groups, strength, threshold). Defaults:
stationary true/false/false; resistance true/true/true; strength4 in0–10; threshold0.25 in0–2.
Display threshold as0–200% to avoid mixing percentages and fractions. Explain independent feature
groups, actual colonist classification, apparel-only coverage and remaining direct damage.

Common load/store/effective-value/row rendering supplies global defaults, inherited current-save
values, dormant overrides, no-save disabled controls and save switching. Reject nonfinite inputs,
recover corrupt loaded numeric values to their own defaults and clamp finite out-of-range values.
Settings apply to the next choice/attempt. Do not change values mid-attempt or write settings while
merely rendering. Resets target one labeled scope and obey the same callback-level disable rules.
No source import, separate settings serializer, bottom tab, debug cheat or per-pawn switch.

Translate every new label, scope/disabled explanation and tooltip into seven baseline languages.
Native job reports remain native. Technical startup IDs/log details stay invariant. When source
authority makes our contribution inactive, explain that state without editing the source's options.

### 5. Target specs and provider authority

Create actual stable target documents from the mod template before any implementation:

| Candidate group | Minimal consumed semantics | Fallback |
| --- | --- | --- |
| SR-RESPONSE | Correct burning tree path, current group/fire, native water choice and self-job decision, safe150-tick target/record lifecycle | Whole owned response pair inactive; loaded response remains |
| SR-IGNITION | Actual new-attachment eligibility, current anatomy/coverage/stat contract and exact dependent fire/job/record effects | Native/provider attachment; no added immunity |
| SR-LIFECYCLE, only if needed | Required save/target cleanup or invalidation callback shape | Do not admit a dependent feature lacking safe lifecycle |

These are semantic groups, not exact-hash gates. Check effective prefixes/transpilers and consumed
data/control flow; allow unrelated edits and compatible owners. Transpilers, if used, validate their
actual incoming stream each invocation and return it unchanged on failure. Do not repeatedly run
foreign transpilers to inspect them. Validator exception/unknown, ambiguous match or install failure
fails only the affected atomic group, rolling back newly installed owned patches only. Later
invalidation stops new owned decisions; existing native jobs/fire remain under native ownership.

Warn once per skipped enabled patch at startup with package/patch/spec ID, target, failed predicate,
unavailable behavior and loaded-provider fallback. Report later invalidation once, not per pawn/tick.
Explicitly disabled groups and absent optional providers are not failed candidates. Body-data fallback
does not fabricate blanket compatibility; retain a bounded diagnostic if needed, never per-attempt spam.

When source package `balistafreak.StopDropAndRoll` is active, leave both owned branches inactive,
regardless of whether its toggles happen to be enabled. Do not inspect private source settings to
fill apparent gaps, unpatch it, spoof its identity or apply a second ignition contest. Optional race,
apparel and combat packages require canonical package/Def/member guards and narrow adapters.
Unknown combat replacement affects only the relevant branch; source comments cannot prove CE support.

No XML/Def mutation is proposed. Harmony becomes a required dependency with eager use; do not bundle
it or old source copies. Gateway is never a runtime dependency. Proven external helper consumers,
if found, need explicit contract decisions: our clamped rule is not the source public helper's raw
unclamped return, and a new type name cannot satisfy an assembly binding.

## Risks / Trade-offs

- **Leg/weight corrections change protection distribution** → explicit proposal, focused statistical
  coverage checks and real equipment comparators; retain source provenance rather than silent parity.
- **A paired response patch fails halfway** → atomic admission; never remove water escape without a
  supported self-extinguish action.
- **Whole-method suppression drops provider work** → effective semantic boundary check, narrow adapter
  or inactive ignition branch; never assume a familiar signature implies safe skip-original.
- **Stopping in ground fire can permit reignition** → preserve native fire sources and continued
  damage; this is not invulnerability or a promise of safe terrain.
- **Custom anatomy/prosthetics provide no valid sample** → native fallback without error or immunity.
- **Global-only source config leaks across saves** → common inherited settings, no source import.

## Migration Plan

Implement one behavior at a time with focused RED/GREEN, preserving the independent ignition/response
branches. Use native job/fire saves and common settings only; existing saves gain missing entries
through inheritance, without bulk pawn edits. Removing/inactivating this contribution restores
loaded native/provider decisions while existing native fire/jobs remain ordinary saved objects.
Any later source import or custom schema migration requires a separate backup/atomicity/restoration
contract; this draft does not authorize it.

## Open Questions

SR-Q1 proposed body sampling and finalized balance; SR-Q2 exact effective burning/attachment/lifecycle
admission and provider effects; SR-Q3 actual external/helper/combat/race contracts; SR-Q4 focused tests,
seven-language inherited settings and reviewed-build native fire/action/damage/save acceptance.
Research these later without asking the user to explain source behavior.
