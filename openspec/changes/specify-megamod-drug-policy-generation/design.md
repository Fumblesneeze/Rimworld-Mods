## Context

Sole owner: Make Rimworld Great Again (`fumblesneeze.megamod`, `mods/megamod`), target 1.7 beta.
S139 is **idea-only**, not a port of the installed Safe Drug Policy Generator. The
[research](../../../mods/megamod/docs/refinement/2026-11-08-safe-drug-policy-generator-research.md)
pins author documentation, source/DLL/native hashes and static findings. User-reported historical
fragility is not attributed to an unproven upstream revision. This draft specifies the workflow
and invariants; **calculation work is blocked on DG-Q1–3 and task group 1**. Completed OpenSpec
artifacts do not mean the algorithm or feature is ready to apply.

## Goals / Non-Goals

Goals: explicit preview and one new policy, visible assumptions and medical choices, current content,
deterministic reset, no partial publication, owned persistence and direct S029/S024/S124/S134/S138
composition. Retain the useful suggestion concept without copying source defects or safety branding.

Non-goals: source algorithm/API/type/package parity, original-mod coexistence or state import,
automatic startup policy generation, global DrugPolicyDef mutation, replacing edited policies,
automatic assignment/default changes, dose scheduling/history changes, guarantees against addiction
or overdose, new medical rules, CE/multiplayer or source-save migration.

## Decisions

### 1. Separate suggestion research from the creation workflow

The source reads `DrugStatsUtility.GetSafeDoseInterval` at default body factor 1, classifies with a
tolerance ratio, and adjusts mutable template rows using Beer minus 1. Native negative sentinels,
clamping and combined-chemical exposure make that insufficient as an owned safety calculation.
Do not copy/translate the source algorithm or put its numbers behind a new label. Native helpers
are inspected inputs, not a certified dosing model.

Before any autonomous schedule/recreation rule is implemented, DG-Q1 must define admitted native
pharmacology shapes and DG-Q2 must establish the calculation, rounding, limits, units and meaningful
comparators. DG-Q3 must resolve combined chemical exposure and supported pawn assumptions (including
body size, relevant genes and dose quantity/S085). Add every player-significant value and assumption
to the observable spec, with evidence and balance reasoning. Until then this change is not
numerically implementation-ready; a blank-policy shell does not fulfill the generation feature.

After those decisions, a pure suggestion function consumes immutable catalog/established drug facts
and preference values, returning row proposals plus supported/unsupported reasons. No Verse objects
or loaded Def database in its Unit tier. Unknown data produces no autonomous routine-use suggestion;
do not silently omit its visible row, borrow another drug's identity or choose a permissive fallback.
Player edits in the preview remain explicit ordinary policy decisions, never labeled computed-safe.
Any automatic result must identify its assumptions and avoid implying it holds for every pawn.

### 2. Consume the current S029 catalog and native defaults directly

Use C03's finalized available Item/IsDrug identities, applicable flags and publication generation.
Physical stock, research completion and current map are not catalog admission criteria. Share this
catalog; do not create a new per-source scan/cache or invent drug availability from names. Respect
the existing unsupported native component boundary instead of synthesizing drug comps.

Start from S029's normalized native defaults: addiction = IsAddictiveDrug, joy/scheduled false,
frequency 1, mood/joy 1, inventory 0. S138 state starts at Any. Source raw `DrugPolicyEntry` defaults
addiction false, and its explicitly generated rows copy that false; this accidental source outcome
is intentionally omitted. Suggestions and preview edits apply explicit fields over the full base,
with visible medical permission. Missing suggestions never mean missing policy entries.

Current proposed reusable preferences are enabled=true, initial mood=1, initial joy=1, initial
tolerance=Any. Mood/joy 1 are native/source-unrestricted values; Any follows proposed S138 semantics,
not the source minimum-tolerance multiplier. These defaults initialize a draft and are not safety
calibration. No additional-days preference, numeric delay cap or source multiplier is adopted before
DG-Q2. Every rebuild starts from fresh immutable baselines, not differences from prior entries.
Reset/return-to-1/Any writes the complete intended values rather than conditionally leaving old data.

### 3. A draft is transient gameplay editing, preferences are common configuration

Add Generate in `Dialog_ManageDrugPolicies` through a narrow admitted contextual extension. Preview
lists native policy fields with reasons, limitations, editable name, Cancel, Reset draft and Create
new policy. It must work without a selected map in a loaded game. No new bottom tab or mod-settings
Create button. Use S029 identity-safe display sorting without changing native execution order.
Measure the layout in all seven languages; determine fit from the actual native window, not copied
source sizes. Normal ordinary policy controls remain available if generation is unavailable.

Common C02 preferences in Drug policies → Generation are `drugPolicyGeneration.enabled`,
`initialMoodLimit`, `initialJoyLimit`, `initialTolerance`. Use one shared load/resolve/store/renderer,
current-global inheritance, dormant overrides and main-menu mutation guards. Overrides are saved
with the current game. Effects apply to the next preview/reset; a relevant preference revision
invalidates an open preview rather than silently changing reviewed choices. A draft edit does not
change those preferences. A created row is entity gameplay state and has no Use-global inheritance.

Draft identity includes exact current game generation, catalog generation and relevant fact digest,
calculation version, effective preference revision and needed feature availability (including S138
for numeric tolerance). Validate the actual consumed facts again at commit, not just a static list
count. Save switch, main-menu exit, window disposal and feature-off cancel/clear the transient draft.
No retained Context, Game, policy/window closure or current-selection reference may outlive its
owner. Map switching alone does not change a save-owned policy. If S138 is off/unavailable, show that
its extra restriction cannot be provided, use Any in refreshed drafts, and do not claim a numeric
value is enforced. Re-enable can offer a newly prepared preview; existing policies remain untouched.

### 4. Prepare completely, then publish one native policy

The inspected native `NewDrugPolicyFromDef` first appends a new policy, then copies rows through a
throwing drug indexer. It is not an atomic factory. A catch around that whole call, a global temporary
DrugPolicyDef or source-style shared template would not meet the contract. Extend the owned S029
prepare/commit boundary for this concrete creation consumer, by agreement with its owner.

DG-Q4 must establish an admitted native-object preparation and database publication path: prepare
all row values and S138 side state while unpublished, allocate a current noncolliding native policy
identity, validate every current drug/choice, and publish only the complete object. Snapshot prior
database membership/order/default and every owned field touched before mutation. Keep existing
policies/entries and pawn assignments unchanged. Same-thread validation and publication must not
allow a stale game/catalog or an observer to consume a half-configured row. If a native path can
invoke unrollbackable external callbacks before complete publication, it cannot be admitted as an
atomic path; find a narrower native boundary or leave generation unavailable. Do not claim to undo
arbitrary foreign side effects.

Verify the full result before reporting success/selecting it: exactly one new identity, final
displayed choices, proper current catalog/native order, retained prior defaults/assignments/history.
On failure remove only the unpublished/newly appended object owned by this action, restore any
owned touched state and verify the original snapshot. Do not restore over independent player or
foreign changes; the transaction must detect/reject reentrancy before allowing unsafe mutation.
Selection changes only on successful completion. No autosave or file rewrite; normal player saves
persist the result. The future design must prove these conditions before implementing Create.

A window-local one-shot confirmation identity prevents the same action/re-entrant click from
committing twice. Subsequent deliberate fresh workflows can create independent policies. Do not
deduplicate by translated policy name or turn repeated Generate into replacement. Dispose receipts
with the draft; a generic persistent transaction framework is unnecessary.

### 5. Existing runtime rules remain authoritative

S139 only produces native row choices plus owned S138 choice data. S024 potential mood, patient
treat recipient and inventory-purpose checks execute through their existing composed decisions.
S138 has the only tolerance predicate, preserving its Any/zero and native joy exceptions; no
generator-specific patch against S138. S124/S134 remain the sole owners of their medical reasons,
claims, blockers and actual dose records. S139 never queues jobs, claims Things, changes withdrawal,
records scheduling as ingestion or resets a pawn tracker. Explicit medical permission is visible
because S134 consumes it, not because generation is an automatic medical rule.

After creation there is no regeneration marker that triggers later rewriting. Native copy/load and
S029 maintenance preserve edited choices. Later new rows use S029 defaults/Any, not earlier
generation preferences or a hidden persisted template. SourceDef/global Def registration is not
needed merely to keep provenance; research identity belongs in documentation, not runtime spoofing.

### 6. Admission and overlap use existing shared paths

Dynamic preview and current-state creation require C#; no product XML mutation is planned. Required
Harmony supports the native UI boundary; no XML Extensions/Prepatcher/source dependency. Pin target
specs before hooks, covering only consumed window/action/transaction semantics. C01 handles effective
body changes, skip-original, compatible patches, supported repatching and bounded failure. No
whole-method or version/hash gate. Withhold Generate if complete creation cannot be supported while
preserving ordinary native controls. Do not inspect external consumers or construct source facades.

Active canonical `seohyeon.bestdrugpolicy` produces one bounded startup overlap warning and
best-effort ordinary effective-off for `drugPolicyGeneration.enabled`: no new previews/commit,
cancel transient preview, keep all prior policies and stored preferences. No rollback of source
startup Defs, original policies or earlier effects is promised. Inactive installation does nothing;
later startup without the original resolves retained preferences normally. Never change/unpatch
the original. Only focused warning/effective-off cases are required, not a coexistence matrix.

## Risks / Trade-offs

- A helpful suggestion can be mistaken for a guarantee → precise reasons, visible assumptions,
  unsupported rows off and a blocking calibration gate; avoid an absolute Safe policy label.
- Native creation may expose partial state → DG-Q4 must establish a real atomic publication path,
  including S138 side data, or the action remains unavailable. Do not mask failure with success text.
- Global preferences could overwrite policy edits → preferences initialize new previews only;
  existing and copied policies are gameplay state, and maintenance never regenerates them.
- Unknown custom ingestion behavior → no arbitrary-provider safety claim or name-based fallback;
  only capability-admitted, researched suggestions. Genuine future integrations need their own
  package/Def-resolved absent-safe contracts.
- Source reset/Beer/clamp defects are easy to reproduce accidentally → complete baselines and
  boundary tests, not source-derived deltas or universal caps.

## Migration Plan

Documentation only. Initial support is new megamod colonies and their later saves. Missing common
preference values inherit defaults; existing native policies are unchanged. New created policies
use ordinary native persistence and the owned S138 codec. No original templates or saved settings
are imported. A later owned-schema migration needs backup, staged atomic conversion, restoration
and verified postconditions before implementation. Feature off preserves created policies.

## Open Questions and implementation blockers

- DG-Q1: Classify actual finalized pharmacology inputs and native helper limitations, including
  unsupported/no-safe interval, nonfinite values and custom outcomes. Specify admitted versus unknown
  rows before any autonomous suggestion code.
- DG-Q2: Derive and validate each schedule/recreation suggestion, units, rounding, limits and optional
  added delay against exact native behaviors and measured comparators. Put the resulting numeric
  assumptions in the observable spec before implementation; no Beer or source-label shortcuts.
- DG-Q3: Establish the policy's population/quantity and shared-chemical assumptions, relevant genes,
  body size and S085 effects. Determine what can be claimed honestly by a policy shared among pawns;
  unresolved cases receive no autonomous suggestion. No universal safety calibration is presumed.
- DG-Q4: Agree and prove atomic native policy publication/rollback, S138 side-state participation,
  stale/reentrant refusal and confirmation idempotence with S029/C01 before the Create slice.
- DG-Q5: Confirm proposed reusable preferences and preview layout as owned decisions, including
  unsupported rows, unavailable S138, manual edits and seven-language fit. Do not invent a delay
  range or source tolerance multiplier merely to complete a settings page.
