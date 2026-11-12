## Context

Sole owner: Make Rimworld Great Again (`fumblesneeze.megamod`, `mods/megamod`), target 1.7 beta.
S139 is **idea-only**, not a port of the installed Safe Drug Policy Generator. The
[research](../../../mods/megamod/docs/refinement/2026-11-08-safe-drug-policy-generator-research.md)
pins author documentation, source/DLL/native hashes and static findings. User-reported historical
fragility is not attributed to an unproven upstream revision. The
[2026-11-11 native research](../../../mods/megamod/docs/refinement/2026-11-11-drug-policy-generation-calculation-research.md)
resolves DG-Q1–3 into the bounded C1 calculation below. Independent review, finalized runtime
fact/admission checks, DG-Q4 creation and DG-Q5 UI work remain prerequisites. No runtime behavior
is accepted from these documents.

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

Calculation C1 chooses **modeled tolerance clearance for one ordinary whole dose** as a conservative
risk-reduction objective. It does not guarantee actual clearance or optimize the most frequent use
below an addiction threshold. Native camera-driven batching can skip decay events, so the preview
must disclose that actual tolerance can remain when a suggested schedule permits another dose.
The research records exact 1.7.5005 native methods, source XML hashes, four Core comparisons and
excluded cases. It is an independently derived model, not an adoption of `GetSafeDoseInterval`.
Its positive initial candidates are native-shaped social/joy drugs with a finite positive addiction
threshold, a single deterministic positive tolerance gain, deterministic negative decay and zero
overdose fields. Require normalized gain strictly below the threshold, zero hediff minimum and
known quantity/effect semantics. Unknown outcomes or effective method changes remain unavailable.

Use actual finalized facts, never the comparator names as an allowlist. The initial population is
healthy ordinary adult humans with BodySize at least the displayed finalized Human-adult baseline
(currently 1), neutral drug metabolism, no initial tolerance/addiction/dependency, no additional
exposure and unsuspended normal ticking. Smaller bodies, relevant amplifying/unknown gene or hediff
effects, unresolved S085 partial ingestion and dependency/treatment scheduling are outside C1.
Weaker gene factors do not shorten suggestions. Disclose these assumptions; the policy does not
inspect/enforce them for every assignee or future state. S138 remains an independent ordinary choice.

Compute binary32 decay events from normalized gain `g`: `q = float(s * 0.0033333334f)` for admitted
negative daily rate `s`; repeat `x = max(0, float(x + q))` until zero. Nonfinite/non-decreasing steps
are unsupported. For the first zero event `k`, use `requiredTicks = 200*k + 415` and integer
`days = 1 + ceil(requiredTicks / 60000)`. The modeled 200-tick cadence, a 14-tick dispatch allowance,
two at-most-200-tick binary32 calendar-boundary errors and one ordering tick derive this budget.
The 14-tick allowance is not a universal native bound: negative hash phases and camera-driven
batch changes can skip whole events. C1 is a disclosed scheduling model, not a clearance monitor.
The extra calendar day is necessary because the native policy compares day indices, not
elapsed 24-hour periods. The research explains the nonnegative signed-int clock domain and rounding
bound; clock rewrites/overflow or custom cadence are not supported. No guessed severity epsilon.

The native editor offers a maximum of 25 days. This is a representability boundary, not a safety
cap: a longer required interval receives no automatic schedule and is never shortened to 25.
The recurrence may stop after 7,197 events, the largest count that can fit that editor under C1.
Do not add a delay preference. Source-derived expected comparisons are Beer 3 days, psychite tea
4, smokeleaf 4 and ambrosia 3; runtime facts must be finalized and admitted before those results
can be offered. The model remains useful without Beer and recalculates from changed facts.

No autonomous recreation is established in C1: native joy ignores schedule and a helper result of
zero does not prove harmlessness (medical penoxycyline and psilocap are counterexamples). Manual
recreation and schedule edits remain available, visibly identified as player choices.

Connect rows when **either** their actual chemical identity or actual tolerance-Hediff identity is
shared, including transitive connections. More than one otherwise eligible candidate in a connected
channel withholds automatic schedules for that channel; never choose by name/order. Unsupported
off rows such as Flake/Yayo do not alone suppress a tea candidate, but their manual/medical exposure
is outside its stated assumption. Any manual recreation/additional schedule on that channel marks
the combination uncalculated; do not prevent ordinary creation or imply C1 still covers it.

After independent research review, a pure suggestion function consumes immutable catalog/established drug facts
and preference values, returning row proposals plus supported/unsupported reasons. No Verse objects
or loaded Def database in its Unit tier. Unknown data produces no autonomous routine-use suggestion;
do not silently omit its visible row, borrow another drug's identity or choose a permissive fallback.
Player edits in the preview remain explicit ordinary policy decisions, never labeled computed-safe.
Any automatic result must identify its assumptions and avoid implying it holds for every pawn.

### 2. Consume the current S029 catalog and native defaults directly

The first native projection consumes an **already captured** S029 catalog and explicit identities
whose partial-dose handling remains unresolved. It returns detached scalar facts in that order,
without a DefDatabase scan, new catalog/lifecycle registry, native-effect invocation or permissive
name lookup. A positive definition-shape result is not a current effective-method admission receipt:
task 1.6 and the owning C01 target still gate every player-facing use. Changed inputs require a new
projection; the creation controller must validate all consumed shape/fact inputs, not only C1's
numeric output. Constructed Defs tests cannot establish finalized runtime data or active patches.

The initial inspected shape is exact native ThingWithComps with one CompProperties_Drug/CompDrug,
known native Forbiddable/Rottable auxiliary comps, explicit quantity 1/1 and exact chance-1
GiveHediff outcomes. Rottable can cause food poisoning: fresh doses are a disclosed model assumption,
not a rule enforced on each assignee/item. Unknown classes/comps or unresolved quantities stay off.
The unique chemical tolerance giver requires positive explicit gain and the checked private
divideByBodySize field; no initialSeverity fallback or guessed body flag. Tolerance uses exact
HediffWithComps, zero minimum, finite positive maximum, one exact deterministic negative
SeverityPerDay comp (range 0/0, reversal 0, minAge 0, mechanitor factor 1), and unit stage decay
factors. Its optional exact DrugEffectFactor must use the same chemical. Neutral gene/metabolism
and no-prior-effect assumptions remain unchanged; the reader does not inspect or enforce pawn state.
Initial/lethal severity metadata must be finite; a nonnegative lethal threshold, chemical-need
attachment, ability attachment or remove-with-tags lifecycle effect is outside the plain decay shape.
Nonempty onlyLifeThreateningTo is also unsupported for tolerance and side highs: the native
whole-body GiveHediff path supplies no Part, while PostMake dereferences Part.def for this metadata.

Secondary native RandomDrugEffect givers are supported only by their inspected early-return bound:
their first curve X (or minSeverity without a curve) must be at least minToleranceToAddict. C1
subsequently requires normalized gain strictly below that threshold; native secondary effects
return for severity less than or equal to their own threshold. Curves must be nonempty with finite,
strictly increasing nonnegative X and positive finite nonincreasing Y. This proves inactivity only
inside the modeled single-dose range, not after actual residual accumulation from native batching.
Unknown/reachable givers and malformed/nonmonotone curves remain unavailable.

Side outcomes initially admit exact native High/Alcohol GiveHediff and Rest OffsetNeed shapes.
Distinct high identities require a positive finite explicit dose without body division, same/null
chemical modifier, monotone stage minima beginning at zero and deterministic negative decay. Active
secondary/mental-state givers or unproved effecter branches remain unavailable. Alcohol's native
hangover branch requires stage index at least 3; support requires its modeled peak to stay below
that index. An effecter's configured stage interval must be unreachable throughout the modeled
zero-to-peak range. Each side high must clear within 297 binary32 decay events: `200*297+415=59815`
fits the earliest modeled one-day elapsed gap represented by C1's minimum two-calendar-day result.
This prevents **modeled** side-high accumulation without changing C1's interval calculation; it
does not guarantee actual high clearance. Slower/unknown side effects stay unavailable. Rest offsets
require exact Need_Rest, finite offset, chance 1, perIngested false and same/null modifier chemical;
the inspected perIngested branch multiplies by remaining stack count. Impairment and other native
risks are not certified harmless. All four Core comparison obligations remain subject to these
actual finalized shape checks, with no drug-name allowlist.

Use C03's finalized available Item/IsDrug identities, applicable flags and publication generation.
Physical stock, research completion and current map are not catalog admission criteria. Share this
catalog; do not create a new per-source scan/cache or invent drug availability from names. Respect
the existing unsupported native component boundary instead of synthesizing drug comps.

Start from S029's normalized native defaults: addiction = IsAddictiveDrug, joy/scheduled false,
frequency 1, mood/joy 1, inventory 0. S138 state starts at Any. Source raw `DrugPolicyEntry` defaults
addiction false, and its explicitly generated rows copy that false; this accidental source outcome
is intentionally omitted. Suggestions and preview edits apply explicit fields over the full base,
with visible medical permission. Missing suggestions never mean missing policy entries.

Initial reusable preferences are enabled=true, initial mood=1, initial joy=1, initial
tolerance=Any. Mood/joy 1 are native/source-unrestricted values; Any follows proposed S138 semantics,
not the source minimum-tolerance multiplier. These defaults initialize a draft and are not safety
calibration. C1 has no additional-days preference or source multiplier; the admitted native editor
bound must never shorten a required interval. Every rebuild starts from fresh immutable baselines,
not differences from prior entries.
Reset/return-to-1/Any writes the complete intended values rather than conditionally leaving old data.

The initial detached draft implementation adopts those initial mood/joy/tolerance defaults.
It projects every row, including unsupported off-schedule rows, from fresh C1/native defaults before
applying preferences. Edits replace complete immutable choices by canonical identity; resets accept
entirely new catalog/fact/preference inputs. This layer rejects unresolved tolerance encodings,
inapplicable permission flags, nonfinite/out-of-range percentages and negative inventory targets.
The native creation boundary must additionally validate the current admitted frequency range,
per-drug `PawnUtility.GetMaxAllowedToPickUp` limit, policy name and all currentness receipts. Those
native constraints are not inferred from a fixed inventory cap or a successful pure projection.

Transaction validation uses the native rename character set and length1–27 (UTF-16), without silent
trimming or duplicate-label rejection. Spaces remain valid native characters. It preserves finite
reviewed frequencies in0.1–25 exactly; only the preview's native slider rounds an edit to whole days
or a reciprocal integer. Per-row inventory uses the actual null-map native pickup helper, including
zero without an ordered-take group. Current effective helper/catalog authority remains part of the
runtime admission gate. It rejects incomplete/reference-mismatched edits and unresolved S138 values;
unavailable S138 accepts only a refreshed all-Any preview, never silent commit-time coercion.

Constructed-Defs failure checks may wrap the existing IDrugPolicyStorage contract through an internal
preparation-only seam; public construction always uses sealed NativeDrugPolicyStorage. Final append
and verification use direct captured storage, with no injected callback between them. A feature-local
publication receipt separates Append from VerifyOrRollback so tests can exercise an owned completed
append and independent conflict through the same production operations; public Confirm calls those
operations synchronously back-to-back. This is not a new registry, runtime fault option, or proof of
effective native admission. Disposal clears native references/delegates and invalidates busy work.

### 3. A draft is transient gameplay editing, preferences are common configuration

Add Generate in `Dialog_ManageDrugPolicies` through a narrow admitted contextual extension. Preview
lists native policy fields with reasons, limitations, editable name, Cancel, Reset draft and Create
new policy. It must work without a selected map in a loaded game. No new bottom tab or mod-settings
Create button. Use S029 identity-safe display sorting without changing native execution order.
Measure the layout in all seven languages; determine fit from the actual native window, not copied
source sizes. Normal ordinary policy controls remain available if generation is unavailable.

Common C02 preferences in Drug policies → Generation use stable IDs `drug-generation.enabled`,
`drug-generation.initialMood`, `drug-generation.initialJoy`, `drug-generation.initialTolerance`.
Use one shared load/resolve/store/renderer,
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
DrugPolicyDef or source-style shared template would not meet the contract. The exact 1.7.5005
[DG-Q4 research](../../../mods/megamod/docs/refinement/2026-11-11-drug-policy-generation-publication-research.md)
establishes the following implementation contract, pending independent review and executable proof.
It consumes S029's existing row preparation/storage rather than adding another policy row store.

1. On the game thread, capture the exact current Game/database, actual policy-list identity and
   ordered prior object/ID/label/source/row snapshots, plus catalog/fact/preference/admission receipts.
   The existing default is the first list element. Require a nonempty database: adding its first
   policy would implicitly choose a default, contrary to this action's contract. Do not call
   `DefaultDrugPolicy()` or pawn `CurrentPolicy` getters for snapshots; both can mutate state.
2. Allocate a positive ID as checked `max(0, current IDs) + 1` in widened arithmetic. Refuse exhaustion
   instead of wrapping or reusing a prior ID. An edited name is not identity or duplicate detection.
   Prepare `new DrugPolicy()` with its admitted parameterless base constructor, then set ID, final
   label and `sourceDef = null`. The parameterless path does not scan Defs or publish the object.
   Do not call `MakeNewDrugPolicy`, `NewDrugPolicyFromDef` or the initializing two-argument constructor.
3. Use `DrugPolicyReconciler.Plan(null, currentCatalog, admittedNativeOrdering)` to establish complete
   native row order/defaults. Overlay the final validated draft choices by canonical drug identity.
   Require every planned `RowIdentity` to be null; a prepared policy must never borrow an existing
   mutable entry. `NativeDrugPolicyStorage.Apply` then creates fresh native entries, writes all
   choices including S138 side state, and attaches the complete entry list to the unpublished policy.
   S029's reconciliation batch is not an arbitrary editor or a database creation API.
4. Verify the complete unpublished object and all captured current-state receipts/snapshots. Admit
   the consumed constructor/getter/field semantics through C01: no prepared-object escape,
   publication or unrollbackable callback is permitted. Compatible unrelated foreign work remains
   allowed. Use a feature-local reentrancy guard and the existing S029 content-boundary receipt;
   no async yield, external callback or new generic transaction registry belongs in this path.
5. After the final current-state check, append the complete object once to the captured actual
   native `List<DrugPolicy>`. The no-callback section covers validation, append and postcheck. Verify
   the original prefix/order and fields, exactly one owned tail object and all prepared choices
   before producing success. The existing default remains at index zero; no assignment/history
   method is called. Select the result only after this verified completion receipt.
6. Before append, failure discards only detached state. After append, rollback may remove only the
   exact owned tail when the list identity and prior prefix still match; verify the original
   snapshot. Never call `TryDelete`, which inspects/changes pawn policy references. Unexpected
   independent/foreign mutation invalidates the draft and admission; do not overwrite it or report
   successful restoration. This is a bounded admitted-path guarantee, not arbitrary foreign-side-
   effect rollback. No autosave or file rewrite occurs.

The [DG-POLICY-CREATE target contract](../../../mods/megamod/docs/patch-targets/DG-POLICY-CREATE.md)
records consumed native semantics and the pending verification matrix. Its static proof permits
the focused preparation/commit implementation; it does not accept that implementation or a live
workflow. Indexed target admission must exist before runtime wiring.

The current staged adapter has65 constructed-Defs checks for detached preparation, exact complete
edit membership, flags, native bounds/name, S138 availability, fresh rows and intended-choice
verification. It also covers positive ID allocation/exhaustion, stale/reentrant refusal, one append,
same-operation idempotence, preparation failures, owned-tail rollback/conflict refusal and disposal.
Its supplied currentness predicate and ordering delegate remain explicit caller assumptions until
the existing S029/C01 receipts are integrated. A feature-local constructor/getter guard now has
scoped Harmony checks, but no production caller or actual native receipt is present; host checks
cannot authorize Create or establish real controller lifetime.

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

The generation-local constructor/getter milestone uses `DrugPolicyCreationAdmission` with explicit
`Revalidate` and inspection-only `Available`. Owned final observation transpilers retain the actual
incoming policy/base/entry/getter and relevant callback bodies. Each body is tied to its published
generation and all consumed publication identities are rechecked before a positive decision.
Callback identity/signature alone cannot authorize it. New callbacks require explicit acquisition;
failed revalidation cleans its new observers, and full rollback attempts all owned members.
The bounded straight-line grammar proves fresh-receiver non-escape and actual-list return while
allowing scalar initialization, local aliases and scalar diagnostic observers. Unknown calls,
branches, native-reference effects, replacement/skip callbacks, by-ref parameters and implicit
type initializers are unsupported. Limits are 256 instructions, 32 observed methods and eight
dependency levels. The unpatched inert System.Object root is checked without a global observer.
This implements a positive narrow host admission path; actual native acquisition, finalization and
controller wiring remain unverified, and no native task is closed by its host cases.

The shared C01 addition for creation admission is an immutable inner-callback metadata projection
from an already captured HarmonyPublication snapshot. It retains the registered MethodInfo, role,
outer-binding/factory flags, match presence and exact selected operation indexes. The existing
Effect selector remains the sole interpretation of target kinds and positive/negative positions;
FailureFor and FailureForConversion retain their existing semantics. The projection invokes no
foreign attribute, factory, callback or transpiler and exposes no mutable provider patch/selector
array. Snapshot Version/Identity still govern currentness; this metadata alone proves no callback
body safe and enables no native action. The creation guard must prove its consumed no-escape and
actual-list semantics, admitting irrelevant/compatible effects and refusing unproved effects.

The first F1 native-facts milestone is a separate contribution-flow guard and admission, documented
in `DG-NATIVE-FACTS`. It binds `CompPostTickInterval(ref float,int)`, the inherited pawn getter,
`Gen.IsHashIntervalTick(Thing,int,int)` and the virtual `SeverityChangePerDay()` declaration.
The actual incoming body must pass the same pawn,200 and incoming delta to the interval check;
false skips the contribution, true performs one rate call, Single-local rate rounding, binary32
multiply by0.0033333334f, Single-local product rounding, then the original-ref read, addition and
write in that order. Reads cannot move across the virtual call because its body may change an
aliased accumulator. All four native callable bindings are nongeneric.
Harmless aliases/nops and body-proved scalar observers can qualify. This small conditional grammar
remains separate from constructor proof and refuses unknown arithmetic, branches, calls and ref
effects. Owned observers and C01 snapshots cover the root plus relevant callback bodies, including
repatch currentness, explicit acquisition, failed-acquisition rollback and obsolete-observer pruning.
The helper remains unregistered. Its positive receipt says nothing about pawn/hash/rate bodies,
rate initialization, other accumulators, outer comp dispatch or severity clamping. All remaining
F0–F5 dependencies and finalized/native workflows are required before any suggestion is exposed.

The next reviewed boundary extracts only the contribution observer's finite root/callback lifecycle
into feature-local `DrugGenerationObservedBodies`, keeping the public contribution API and all
opcode proofs separate. Its one-to32 declared roots are canonical actual method declarations;
duplicate inherited lookup aliases cannot acquire competing routes. Multiple roots reuse one
callback observation, and candidate rollback after a later root fails preserves another bundle's
owned observer. Constructor admission and shared C01 remain unchanged. Eventual runtime union-bundle
binding belongs to the orchestrator; overlapping independent claims are refused.

`DrugGenerationPawnBodyGuard` proves the actual inherited getter returns the same comp's current
parent.pawn field with no native-reference write/escape or substituted result, while allowing
harmless reference locals. This is a positive field-projection subclaim and remains unregistered.
It does not prove native instance initialization, hash semantics or future field stability.
The next seven-role `DrugGenerationSeverityInitializationGuard` is also unregistered and reuses
the same finite observation bundle unchanged. It proves the actual zero-input Rand Range/Chance
early returns, same-address FloatRange getter, properties calculator, comp properties projection,
empty base PostAdd hook and concrete initialization write. The calculator reads the rate before
the range call and stores the sum into Single before the zero-chance branch; the hook forwards
the original receiver/input to its base once and stores one calculated result into that comp.
Nops and proved scalar observers are compatible. Address writes/escape, substituted properties
or destination, changed stores/order, skip/duplicate calls and relevant callback repatching refuse
the current claim. All selected inner callbacks still receive the existing conservative proof,
including selections in a specialized unreachable tail; no new reachability API is introduced.

This is a conditional initialization producer, not a current-input or perpetual-field invariant.
F0/F2 must separately bind the actual nonnull comp/properties pair and finite negative rate with
zero endpoints/reversal; either signed zero is allowed. The real types, especially Rand, must
already have naturally completed initialization. Host NoInlining fixtures do not prove native
Mono call-edge/repatch behavior, actual construction/assignment/PostAdd dispatch, save/load or
later field stability. The concrete rate and hash/calendar/batching/outer accumulation/clamp
obligations remain open. Root integrates the native union of roots without overlapping observers;
none of these partial receipts exposes generation or closes task1.6.

Generation preference controls use the common composite-settings renderer. A small transient
percentage draft reuses S138's localized percentage parser and exact unchanged-value preservation;
it owns no persisted settings or policy rows. The mood/recreation controls accept numeric values,
and the tolerance control additionally exposes Any. Apply uses the renderer's current scope commit;
invalid input or rejected binding retains text. Stored-value, numeric-culture and save-activation
changes reset the transient editor. Global/save lifecycle enforcement remains in C02, while the
generator controller must still invalidate an open preview after an effective preference change.

Active canonical `seohyeon.bestdrugpolicy` produces one bounded startup overlap warning and
best-effort ordinary effective-off for `drug-generation.enabled`: no new previews/commit,
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

- DG-Q1–3: C1 is specified by the 2026-11-11 native/source research and this design/spec. Its
  limited supported shapes, arithmetic and population/channel assumptions await independent review.
  Task 1.6 still requires actual finalized facts and effective native semantics before exposing
  suggestions; no loaded-Def or gameplay acceptance follows from source inspection. New content
  shapes, partial ingestion, population variants and any positive recreation model need their own
  evidence/scenarios, not a guessed permissive fallback. Implement the established C1 slice once
  reviewed rather than substituting an empty-policy shell or indefinitely reopening the same research.
- DG-Q4: The inspected parameterless-object/S029 fresh-row/single-list-append contract above resolves
  the native design choice. Independent review and focused executable preparation/rollback,
  S138 participation, stale/reentrant refusal and confirmation-idempotence proof remain required
  before exposing Create. Native workflow acceptance remains separate.
- DG-Q5: Confirm proposed reusable preferences and preview layout as owned decisions, including
  unsupported rows, unavailable S138, manual edits and seven-language fit. Do not invent a delay
  range or source tolerance multiplier merely to complete a settings page.
