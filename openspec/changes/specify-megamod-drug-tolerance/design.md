## Context

Sole owner: Make Rimworld Great Again (`fumblesneeze.megamod`, `mods/megamod`), target 1.7 beta.
S138 is a proposed independent behavior replacement. The pinned
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
helpers. DT-Q2 remains unresolved for whether/when a final recheck is needed, threshold changes
during an executing routine job, and the exact recipient/purpose proof across pickup and save/resume.
Resolve that contract before the affected hook; do not infer purpose from a shared driver or add an
unscoped `Thing.Ingested` veto. Feature-off still does not cancel an already executing native job.

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

## Risks / Trade-offs

- Strict zero can surprise users familiar with source behavior → distinguish Any, explain zero and
  show both boundary scenarios in native acceptance; no implicit upgrade of zero to Any.
- Malformed choices can prevent routine use → visible per-row repair and bounded diagnostic; native
  joy exceptions and separate medical/manual routes retain their stated scope. This conservative
  choice is proposed explicitly and must be reviewed before implementation.
- Current tolerance does not prove post-dose safety → describe a preference/restriction only; no
  addiction/overdose guarantee, invisible body-size correction or invented dose prediction.
- S029/C01 lifecycle milestones are incomplete → storage/admission integration is blocked on their
  actual reviewed contracts, not a parallel private state store or another metadata reader.
- Native joy subclasses or patient feeding can bypass an assumed hook → research exact supported
  paths before the affected slice; preserve unavailable behavior if it cannot be proved.

## Migration Plan

This change only drafts future behavior. Introduce the owned field with missing = Any; retain/load
megamod saves and copied policies. Do not import the original's field. No automated old-schema
migration is authorized. If a later migration is needed, specify backup, staged conversion,
atomic commit, restoration and verified postconditions before implementing it. Effective-off is a
reversible gameplay switch, not save conversion. All runtime tasks remain unchecked.

## Open Questions and implementation blockers

- DT-Q1: Pin the exact S029 extension point and owned codec/invalid-record preservation before
  persistence work; prove copy and rollback cover side data and saved duplicate handling.
- DT-Q2: Native route facts now distinguish direct joy, social drinks, scheduled use and separate
  medical/manual/refill paths. Final recheck timing, changes during executing routine jobs, exact
  recipient/purpose across save/resume, the future S024 treat boundary and effective-body admission
  remain unresolved before enforcement; no shared-driver inference or unscoped `Thing.Ingested` hook.
- DT-Q3: Verify the intended layout and failure presentation on the native target in all languages;
  no claim about readability follows from source inspection.
- DT-Q4: Review the proposed Any/zero/default-on and invalid-state choices as explicit behavioral
  decisions. Unknown tolerance-reader results must not be silently substituted with zero; define
  a visible unavailable result and bounded diagnostic in the adapter tests.
