## Context

Sole owner: Make Rimworld Great Again (`fumblesneeze.megamod`, `mods/megamod`), target 1.7 beta.
S138 is an independent behavior replacement under implementation. The pinned
[research](../../../mods/megamod/docs/refinement/2026-11-08-drug-tolerance-research.md) records the
installed source/DLL and native 1.7.5000 identities. It establishes source predicates, not live
acceptance. Source UI conflates 1 with Any, yet its predicate still rejects severity at least 1;
missing hediffs also bypass numeric zero. Both discrepancies are deliberately corrected here.

## Goals / Non-Goals

Goals: understandable native policy controls, explicit strict comparison, identity-preserving
owned state, direct composition with S029/S024/S124/S134, bounded truthful failure and ordinary
settings-off behavior. Preserve source provenance without inheriting its implementation shortcuts.

Non-goals: universal addiction safety, predicted-dose pharmacology, new drugs, new dose scheduling,
manual/medical/refill enforcement, source API or assembly identity, importing source settings/saves,
original-mod compatibility matrices, CE/multiplayer or migration of pre-megamod colonies.

## Decisions

### 1. Distinct domain choice and one owned policy state

Represent the choice as Any, numeric fraction in [0,1], or a persisted invalid-state record requiring
review. Any is a distinct variant, not numeric 1. Compare unrounded current severity with the numeric
value; no current hediff means zero. Do not convert missing capability/giver into zero: it means the
drug cannot expose this control. An admitted current giver with no pawn hediff is the zero case.
Numeric 1 and Any must round-trip independently. Invalid encoding/out-of-range/nonfinite values
remain a visible conservative state, rather than clamping or silently granting use. Missing field
alone defaults to Any. This is a proposed semantic correction, not upstream compatibility.

Attach owned serialized state to native policy/entry identity through the narrow S029 storage
boundary; exact implementation must be decided with that owner before edits. The source weak table
is evidence that row replacement loses data, not a mandated implementation. Include choices in
S029 copy, prepare/commit/rollback, duplicate-retention and publication boundaries. Keep surviving
entries and saved policy IDs/assignments, default ordering and native dose history intact. New rows
alone get Any. A valid numeric zero is not an uninitialized default. Removal follows S029's saved
deletion semantics; no tombstone archive or source-save migration is introduced.

Retain state when a surviving drug temporarily loses an applicable tolerance capability, but show
the control as unavailable and do not fabricate enforcement; if the row is actually removed and
saved, its state is removed with it. Validate against the published current catalog, not physical
stock or defName-only cached eligibility. The minimal persistence codec, schema version and lifecycle
identity must be pinned before the storage slice, including handling a future unknown version without
overwriting its raw data. This research decision does not approve a migration implementation.

The follow-up native research confirms that policy copy creates new entry objects and save/load
deep-serializes entries. Current S029 storage snapshots retained row identity and seven native
choices; it has no tolerance codec or side-state rollback contract. Retained identity alone cannot
cover copy, load-created rows or partial failure. DT-Q1 remains blocking until one owned extension
covers those cases; new-row defaults are S029's native initialization followed by normalization.

### 2. Native route restriction, not a general ingestion veto

The inspected map and caravan scheduled paths reach
`Pawn_DrugPolicyTracker.ShouldTryToTakeScheduledNow(ThingDef)`, which requires
`AllowedToTakeScheduledNow(ThingDef)`. The latter is a candidate shared eligibility boundary with
native `DrugStatsUtility.GetToleranceGiver`. Native false remains false. Scheduled eligibility
combines native result, S024's potential-mood predicate and the owned tolerance predicate, while
the later native timing/overdose decisions remain authoritative.

The S024 scheduled-mood batch moves this one native hook into a fixed shared owner. S138 retains a
read-only dependency on its current structural receipt inside the atomic group; S138 rollback never
removes a hook still needed by mood checking. Its full editor/geometry/persistence/joy/social
availability continues to gate tolerance. Either consumer's setting or local read failure removes
only its own extra restriction; a shared boundary failure revokes both scheduled additions and
S138's atomic capability. No new universal ingestion check, rule registry or stored state is added.

`JoyGiver_TakeDrug` inherits `JoyGiver_Ingest.CanIngestForJoy(Pawn,Thing)`. On this direct
self-recreation route, the native positive DrugDesire/mental-state policy exception skips only the
additional tolerance veto; earlier ordinary eligibility remains required. Native
`JoyGiver_SocialRelax` instead selects drinks through its separate
`TryFindIngestibleToNurse(IntVec3,Pawn,out Thing)` path. That path requires joy permission and a
nurseable drug without those exceptions. Apply the tolerance choice to social drinks without
importing the direct joy-route bypass; rejecting the drink retains an otherwise eligible social
job without a drink. A base joy hook alone does not cover this route. Unknown/changed native
exception semantics require admission failure, not a guessed trait-name rule.

S024 patient treats retain their existing recipient joy-policy permission and call the shared
numeric comparison at selection and the appropriate native action boundary, without importing
self-recreation exceptions into feeding. Native `JobDriver_FoodFeedPatient` passes its Deliveree
to chewing/final ingestion; the visitor/toil actor is not the recipient. The driver also serves
ordinary feeding, so its type alone cannot prove an S024 recreational purpose. Keep exception
policy at its proven route, outside the shared threshold comparison; inventory sourcing never
changes purpose. Do not Harmony-patch our own S024 implementation.

Native standalone `AllowedToTakeToInventory` excludes scheduled rows. A scheduled ingest job can
instead carry `takeExtraIngestibles` through `DrugAIUtility` and `JobDriver_Ingest`. Withholding
that dose also withholds its attached pickup; preserve independent native refill without promising
or inventing a substitute refill job. This does not alter inventory targets or add a direct refill
veto. The exact inspected routes and identities are retained in the refinement research addendum.

Medical/manual/addiction-relief/refill paths do not call a blanket tolerance veto. S124 and S134
retain their current eligibility and shared pending-chemical authority; S138 neither acquires a
medical claim nor observes consumption twice. Actual chemical/hediff facts come from the current
pawn at the eligibility boundary. Do not persist pawn/hediff references, cache per-pawn permission
or create a private drug inventory index. The inspected native jobs do not repeat scheduled/joy
policy at final ingestion, and manual, addiction/dependency and routine routes share downstream
helpers. DT-Q2 is resolved at the native selection boundary: policy, tolerance and enable changes
affect the next routine selection. An already selected order retains native execution/interruption
and save/resume behavior, without a second final-ingestion veto or cancellation. Do not infer
purpose from a shared driver or invent persisted job-purpose tags. The future S024 treat selector
must pass the actual patient to the owned rule; its present absence does not block native routes.

### 3. One admitted enforcement-and-editor group

C# is required for current pawn state and native UI; static XML cannot express it. No new XML or
XML Extensions dependency is planned. Harmony is required. Create indexed target documents before
patch implementation. C01 admission must prove only consumed semantics: relevant native result and
route, effective surviving control/decision, correct policy identity and native exception scope.
Account for skip-original and effective transformed bodies when required; do not use full hashes,
exact versions, exact whole-body matching or absence of other owners as admission conditions.

Install the enforcement/editor group atomically so a configurable limit cannot appear enforced
while its decision hook is absent. Include serialization/copy support needed to retain already
owned state even when enforcement is effectively off; this support must not mutate choices or grant
permission. If only presentation is unavailable, do not silently leave hidden new restrictions active.
Supported repatches revalidate; incoming transpilers return unchanged on failed predicates. Roll
back only newly installed owned patches and retain one useful reason per failed enabled target.
When required semantics are unknown, the feature is unavailable and native behavior remains; this
is different from a valid enforcement path encountering a malformed saved choice, which blocks
only its threshold-governed routine decision. UI must distinguish both cases.

### 4. Contextual policy UI and common feature configuration

Policy values live beside the corresponding native drug row, with distinct Any/numeric selection,
visible percentage, scope tooltip and unavailable/Needs review states. Measure a workable layout
with S029's identity-safe alphabetical presentation and all seven languages; do not copy fixed 80%
width reductions. Preserve focus during list refresh and native copy/rename/default/assignment.
No new bottom tab, source mod-settings page or debug action is introduced for ordinary editing.

Proposed common setting `drugTolerance.enabled` has built-in true, category Drug policies, ordered
before generation. Global/save/Use-global behavior follows C02, with no save callback at main menu,
current global inheritance, dormant overrides and exact game lifecycle. Policy-entry choices are
not settings. Effective disablement takes effect at the next eligibility/editor refresh; it does
not cancel an in-flight native job, reverse consumed effects or erase row data. Re-enable uses the
retained choices. Do not retain a previous game, policy, dialog or context reference after disposal.

Canonical original package `seohyeon.onlyiftolerancebelow` active at startup produces the bounded
warning and a best-effort effective-off reason on this ordinary path. Preserve stored preferences;
inactive installation does nothing; later startup without the original resolves preferences
normally. Never disable/unpatch the original or promise complete coexistence safety. Focused
warning/effective-off verification suffices; no original API/drop-in test matrix is in scope.

### Revision21 native composition assumptions

The2026-11-12 user correction supersedes the contextual dialog: use the same Widgets.HorizontalSlider
as the adjacent native mood/recreation controls. Reuse its native rectangle offset/contract, middle
alignment, rail, handle and current-value label; do not author another control. Position0..101 maps
explicit interaction to whole numeric percentages0..100 and the final Any stop. Rounding happens
only after interaction, so merely drawing a retained12.5% or other exact value never changes it.
This one-percent edit granularity is an implementation assumption; the stored codec and comparison
precision remain unchanged. Needs review retains its encoding until a deliberate slider selection.
No extra modal, Apply or Cancel is involved. Before committing, revalidate the exact current Game,
open manager/selected database policy, unique current drug row, original immutable choice and current
effective availability. There is no cross-frame edit authority. Native font measurement still uses
the current locale; ordinary enable preferences remain editable when support is unavailable.
Earlier dialog-native evidence remains historical and cannot accept this changed control.

The direct-joy adapter proves the consumed positive DrugDesire/mental-state suffix in each incoming
generation before projecting the exception. The initial native resolver supports an inherited
CanIngestForJoy identity; a loaded direct-drug provider override is unavailable until delegation
can be proved. This is a bounded unsupported executable shape, not a mod identity blacklist.
Unique current native rows are resolved by reference; no duplicate-drug or defName fallback exists.
A mapped but invalid current tolerance read grants no new veto and emits a localized message and
diagnostic once per current Game. Weak notice tracking retains no Game or pawn permission. Missing
drug capability instead appears as a disabled Unavailable row, retaining its stored choice.

Runtime registration and integration with the separately owned geometry service are implemented.
Independent review and scoped native editor, route and persistence evidence are recorded with
their exact tested builds in tasks; the full integrated acceptance remains pending. The final
activation marker and all
geometry/allocation/editor/direct-joy/social candidates and shared scheduled dependency belong to one C01 group; the
existing persistence support remains independently active. These scoped results do not accept
the whole S138 feature.

The reviewed correction tracks layout failures by exact policy and row identities within the
open manager. Successful assessment of another row, switching selection or successful header
layout cannot clear that row's failure. Current database/row reconciliation may discard removed
or replaced allocations; closing the manager discards its state. Initial window admission is
exact-native-type only: an open subclass could override a virtual producer without touching the
admitted native body. Supporting a subclass later requires proving those consumed overrides.
The existing lifecycle pump observes open policy managers and gives a localized explanation for
off or unavailable controls, at most once per reason per window. It can explain initially-off
state without constructing patches. No policy manager means no contextual notice, and ordinary
off is not diagnosed as a failed enabled patch.

### Focused caravan consumer assumptions

The paired Core-only caravan scenario prepares two separate stationary single-adult caravans, each
with its own policy and two-Beer stack. Disposable setup removes traits/health confounders and starts
food/rest/mood/joy full with no prior drug history. Beer scheduling is on, joy/addiction permissions
off, daysFrequency1, mood/joy thresholds1 and stock target2; all other drug rows are disabled. The
native policy slider changes A from Any to0; B retains Any. No needs, history, policy or consumed
effects are assigned after native time starts. A600..2400-tick observation window remains within
one game day; actual hunger, another ingestion route or changed current identities fails the case.
The native caravan path calls IngestDrug/Thing.Ingested directly, with no map job or refill claim.
Passive seven-method provenance ties actual caravan ticks to scheduled decisions, native Allowed
results, ingestion and physical splitting, while native Items/Health captures remain the visible proof.
The existing typed World lease, native generation wait, exact caravan/Health controls and pointer
cleanup are reused. The100 counted steps reserve60 actions/captures,13 screenshots and495s waits:
1785s bounded work plus90s reserve (including30s cleanup), scenario2100s/launcher2400s. This is an
authored consumer allowance, not a gameplay duration or accepted native result.

### Baseline-language slider observation

The existing seven-language obligation uses the native row header, Any, Needs review, numeric and
retained inactive labels, together with RuleHelp/InactiveHelp, at the supported native viewport.
Verification selects an inert header or presses the unchanged native slider through scoped native
input, retains its pointer and allows actual tooltip dwell; no separate hover route is required.
Native dragging edits the value and ordinary joy/scheduled toggles demonstrate retention/reactivation.
Every selected locale needs personally inspected text/glyph/clipping evidence. The removed dialog's
numeric-text parsing is not a claim of this slider workflow.

## Risks / Trade-offs

- Strict zero can surprise users familiar with source behavior → distinguish Any, explain zero and
  show both boundary scenarios in native acceptance; no implicit upgrade of zero to Any.
- Malformed choices can prevent routine use → visible per-row repair and bounded diagnostic; native
  joy exceptions and separate medical/manual routes retain their stated scope. This conservative
  choice is explicit in the accepted implementation contract.
- Current tolerance does not prove post-dose safety → describe a preference/restriction only; no
  addiction/overdose guarantee, invisible body-size correction or invented dose prediction.
- S029/C01 lifecycle milestones are incomplete → storage/admission integration is blocked on their
  actual reviewed contracts, not another settings store or metadata reader.
- Native joy subclasses or patient feeding can bypass an assumed hook → research exact supported
  paths before the affected slice; preserve unavailable behavior if it cannot be proved.

## Migration Plan

The owned field uses missing = Any and retains megamod saves and copied policies through the
implemented persistence support. Do not import the original's field. No automated old-schema
migration is authorized. If a later migration is needed, specify backup, staged conversion,
atomic commit, restoration and verified postconditions before implementing it. Effective-off is a
reversible gameplay switch, not save conversion. The task list records scoped evidence and remaining
implementation/native obligations; dated tranche sections below retain their historical status.

## Open Questions and implementation blockers

### 2026-11-11 state tranche

The first implementation tranche pins an immutable, host-safe choice and one raw string codec.
The owned field is `megamodTolerance`; absent (`null`) means Any. Schema 1 encodes `1:any` or
`1:limit:<invariant round-trip Single fraction>`, including distinct numeric 0 and 1. Unknown schema,
unknown kind, empty/malformed text and nonfinite/out-of-range fractions remain Needs review and
retain their exact original text until an explicit valid edit. Known input also retains its encoding
so ordinary capture/save does not normalize or widen a value. This introduces no migration.

The concrete S029 extension is an immutable optional choice on `DrugPolicyRow`, captured from the
exact `DrugPolicyEntry` by `NativeDrugToleranceState`. A weak table associates state with native
entries, never policy names, drug names or save-global keys. S029's existing plan carries a surviving
row's snapshot through normalization and duplicate selection; new rows use Any. Apply/Restore write
that snapshot with the native values, covering later-policy failure without another transaction.
Removed rows have no archive; a subsequently created entry starts at Any. The weak association does
not retain disposed games or deleted rows. Copy uses the source's immutable choice and replaces only
the destination association, so later edits are independent.

Current 1.7.5005 `DrugPolicyEntry.CopyFrom(DrugPolicyEntry)` and `ExposeData()` remain the native
copy/save boundaries. This tranche supplies explicit state/encoding entry points for those future
adapters; it does **not** register them or claim native copy/save persistence. Their C01 target
documents, admitted hooks and native workflow are still required before exposing the editor.

The routine decision projection accepts only scheduled, direct recreation, social drink and
patient-treat routes. It preserves native rejection and treats the direct-recreation exception only
on that exact route. Disabled state adds no veto. A successfully read absent hediff is zero; invalid,
nonfinite or negative reader data is unavailable and keeps native permission with an explicit
unavailable result for the later adapter to report. A valid reader and malformed choice produces
Needs review and blocks. Any does not impose a threshold, even above 100%. No cached permission,
final-ingestion hook or medical/manual/refill interception is introduced.

- DT-Q1: The state tranche pins S029 capture/apply/restore and exact raw codec preservation.
  The native support tranche uses the four-member `DT-POLICY-PERSISTENCE` group below; real copy,
  native save/load and failure acceptance remain required before claiming persistence verified.
- DT-Q2: Resolved 2026-11-11 with the orchestrator: current route eligibility is the selection
  boundary, with no new in-flight cancellation/final veto. Native effective-body admission remains
  required for the forthcoming enforcement group. S024 owns a future direct recipient-aware call.
- DT-Q3: Verify the intended layout and failure presentation on the native target in all languages;
  no claim about readability follows from source inspection.
- DT-Q4: Resolved 2026-11-11: retain explicit Any versus numeric zero/100%, missing hediff zero,
  default-on, invalid saved choice Needs review, and unknown reader unavailable/native permission.
  The adapter/editor still owes visible feedback and bounded diagnostic acceptance.

### 2026-11-11 native persistence support tranche

`DrugTolerancePersistencePatch` admits `DrugPolicyEntry.CopyFrom`, `DrugPolicyEntry.ExposeData`,
`ScribeSaver.FinalizeSaving` and `ScribeLoader.FinalizeLoading` as one C01 Early group, before
policy factories and saves use it. Copy captures the final outer-prefix source's immutable choice
and exact receiver; the first outer finalizer commits only a successfully executed native call.
Scribe exports the raw encoding and imports only LoadingVars. This support runs independently of
ordinary effective enablement. Immutable choices plus weak entry identity also work on native
loader callers; they do not touch Unity/Def services or retain disposed games.

Inspected 1.7.5005 `Scribe_Deep.Look` catches ordinary per-row ExposeData errors and continues.
An entry exception alone therefore cannot guarantee persistence. A sticky failure latch aborts
the enclosing native save/load finalization and subsequent attempts until restart. `SafeSaver`
only replaces an existing save after its temporary save completes; an aborted finalization leaves
that old file intact. Preserve native exceptions, never manufacture OutOfMemoryException to
escape Scribe. Admit only finalization compositions whose owned guard runs first and whose foreign
finalizers cannot change exceptions. A supported later repatch revokes generation receipts; normal
callbacks reuse admitted immutable receipts rather than rebuilding metadata on every row.

Startup rejection and arbitrary complete observer removal are explicitly unavailable, not a
preservation promise. Later unsupported exception-suppressing foreign code may defeat an abort;
the contract does not claim protection against that composition. No editor/enforcement may become
available without persistence support. The native Duplicate/save/reload scenario proves component
wiring with hidden-field assertions and visible copied native row edits; full tolerance editing,
enforcement and native failure observation remain separate obligations.

The actual 1.7.5005 late-load fault exposed a native recovery limitation: `Game.LoadGame` invokes
`Scribe.loader.FinalizeLoading` before map finalization initializes `MapDrawer` sections. After
the owned guard aborts that boundary, the native load-error handler schedules its map-error dialog
and attempts main-menu recovery; `MapDrawer.Dispose` then dereferences the uninitialized sections.
The resulting partial game remains `MapInitializing` and non-playable. The failure contract requires
rejection, preserved source and visible restart guidance, not automatic main-menu recovery. The
native scenario must acknowledge the actual map-error dialog before observing the owned notice;
it binds the additional exact handler error to the observed disposal stack and injected fault.
No product patch repairs or resumes that partial game, and no unrelated error is exempted.

### Smallest next enforcement/editor batch

Use one owned reader for the drug's resolved `CompProperties_Drug.chemical` and the pawn's matching
tolerance hediff; absent recognized hediff is zero, unknown mapping/data is unavailable. Admit the
scheduled policy tracker boundary shared by map and caravan selection, direct `JoyGiver_Ingest`
candidate evaluation with its proven native exception scope, and the drink candidate inside
`JoyGiver_SocialRelax.TryFindIngestibleToNurse` without vetoing social activity itself. All three
routes and the contextual row editor must install atomically, composed directly with the owned
rule and the common enable setting. Direct joy subclasses and stock/refill bypasses require the
recorded route proof, not a shared JobDriver patch. S024 is absent and becomes a future direct
consumer at its actual recipient-selection boundary; it does not require a pretend treat adapter
or block the otherwise complete base feature. Native map/caravan/joy/social scenarios and all
seven language layouts are still necessary to accept this next batch.

The reader tranche uses native `DrugStatsUtility.GetToleranceGiver` for applicability, so a chemical
field alone does not invent a tolerance effect. It reads the first matching current hediff, matching
native `AddictionUtility.FindToleranceHediff`, without summing duplicates or caching pawn permission.
A missing health list or encountered malformed null entry is unavailable rather than known absent;
the host reader tests use only explicitly registered constructed Defs and native Hediff objects.

The prospective editor gate must prove the native initial size, window margins, policy sidebar,
scrollbar allowance and consumed calculator allocation before enabling retained restrictions.
Measure the current translations and viewport on the main thread before routine selection; first
paint is not an activation event. Reserve at least120 GUI units or the wider measured owned text,
retain at least600 native units, and admit calculator widths only within600..1300. These bounds
still require the real seven-language and narrow-window gate. Integer width deductions27/108 or
combined135 and one native Single multiplication are supported; arbitrary ideal affine algebra
is not a floating-point proof. Actual header and row inputs are rechecked before changing a Rect.
An unsupported actual allocation invalidates the entire atomic group for subsequent decisions,
preserves native controls and stored choices, and displays the reason. Recovery requires a fresh
supported allocation/locale/viewport assessment; an already-open manager with an actual-input
failure additionally needs successful validation of its current allocation. A timer alone cannot
clear that failure. Selected native jobs retain the already specified in-flight behavior.

### Revision21 upstream width proof

Eight passive required-helper observers cover native InitialSize, Margin, initial window storage,
inner contraction, manager right-area production, concrete config forwarding, header/row production
and scroll-view adjustment. Final effective bodies must deliver the consumed widths; a constant
inventory alone is insufficient. Each observer has a current immutable publication receipt and
joins the same atomic editor/enforcement group. Missing observations, mutable outer bindings and
skip/write effects on protected operations revoke availability without invoking native GUI code.
Benign observation and unrelated incoming drawing remain composable. Construction is metadata-only;
current physical Screen.width is queried only after the runtime's main-thread/live-game gate.

The prospective native width is min(Screen.width-50,1300)-36-210. Header production retains this
width and its separately guarded consumer subtracts16; conservative row input additionally removes
22 for section margins and the largest native scrollbar deduction. The logical viewport must fit
the admitted prospective window width and720-unit height before first paint. Each open exact native
manager's actual windowRect must also fit wholly within UI.screenWidth/UI.screenHeight, both before
routine restrictions and before reserving a header/row column. This corrects the initial bridge's
local-width-only implementation: physical1280x1024 at accepted1.25 scale can yield a1230-wide window
inside a1024-wide logical viewport. No native size or position is rewritten. Native row scrolling
remains valid; virtual row coordinates are not mistaken for absolute screen bounds. A fresh valid
viewport/window assessment restores this prerequisite, while existing per-row allocation failures
still require their own successful assessment. Rect value primitives and the inspected
GenUI.AtZero/ContractedBy/Rounded arithmetic remain the existing native-contract boundary; the new
proof does not claim recursive engine certification. It supports the exact native manager and
requires separate admission for virtual geometry overrides. The exact predicates and source identity
are in DT-POLICY-EDITOR; focused host evidence does not establish native readability or acceptance.

The inspected1.7 windowed backend exposes only its current resolution in the native menu. The
combined verification scenario therefore explicitly arranges1024x768 and1280x1024 windowed
preconditions, then uses native manager opening and actual1.25/1 scale selections to prove unavailable
controls and recovery. Native resolution selection is not claimed. Restore the original1600x900
fixture size and observe eventual display restoration under the runner's bounded cleanup protocol.
