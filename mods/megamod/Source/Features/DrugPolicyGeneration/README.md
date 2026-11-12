# Drug policy generation — C1 core

Owner: `fumblesneeze.megamod`, S139. The owning
[change](../../../../../openspec/changes/specify-megamod-drug-policy-generation/design.md)
defines the complete feature. The C1 calculation has an independent no-finding review. Detached
draft editing and native definition projection are implemented with resolved independent reviews.
Native creation, preference controls and partial effective-method guards are staged and unregistered.
Finalized native binding, complete fact admission and the live generator workflow remain open;
the feature is not accepted.

`Core/DrugPolicySuggestions.cs` consumes immutable values and opaque canonical identity handles.
It reuses S029's `DrugCatalogEntry`/`DrugPolicyChoices` and S138's immutable tolerance choice. It does
not inspect a Def database, install patches, schedule jobs, modify policies or save data. Unit's
existing `Source/**/Core/**/*.cs` link includes it; no project/source-link change is needed.

`Create` produces one row per distinct current catalog identity in the supplied native order. It
computes the reviewed C1 binary32 model, withholds unsupported shapes and unrepresentable intervals,
and connects exposure through actual chemical **or** tolerance reference identity. Unsupported rows
can connect a channel, but a channel needs multiple otherwise eligible candidates before automatic
schedules are withheld. Display names/equality overrides do not establish identity.

`Runtime/NativeDrugGenerationFacts.Capture` consumes the already captured S029 catalog and explicit
unresolved partial-dose identities. It does not rescan DefDatabase, invoke ingestion/random effects
or create a lifecycle registry. It preserves catalog identity/order, rejects duplicate/non-native
handles and projects raw scalar values into immutable facts. `OrdinaryWholeDose` denotes only the
inspected definition shape in this adapter. Runtime callers still need the current effective native
method/finalized-input gate in task 1.6; this adapter is deliberately not registered or exposed.
Other enum values preserve a concrete unavailable reason. Passing ordinary numeric fields is not admission.
The scalar fields retain raw values so malformed/nonfinite input cannot be normalized into a
permissive proposal. Body-size division follows the admitted giver flag and supplied actual adult
baseline; no gain is clamped to a threshold or maximum to manufacture eligibility.

Every set carries its baseline and the `NativeBatchingMayLeaveTolerance` disclosure. C1 is a
cadence-based suggestion, not a bound on actual native clearance. Runtime UI must translate and
display the full population/exposure/batching assumptions before Create; no live reader or UI is
present to establish that behavior.

The reader admits the exact bounded shapes in the owning design, including inactive native secondary
givers and Alcohol branches only inside the modeled single-dose range. It never claims those
branches stay inactive after actual residual accumulation. Side highs must clear within the derived
297-event model budget. Fresh-dose/no-prior-effect assumptions and remaining impairment/food-poison
risks still need visible UI disclosure. All four source-comparator profiles are covered by constructed
Defs tests; those fixtures are not loaded Core definitions or evidence of finalized mod-list behavior.
The native contract and remaining runtime gate are in `docs/patch-targets/DG-NATIVE-FACTS.md`.

`Runtime/DrugGenerationFactInputs` captures the declared consumed field/reference shape from the
same shared catalog, explicit partial-dose membership, freshly supplied Human-adult inputs and
effect-factor curve. It compares identities and exact float bits, permanently stales a failed
receipt and refuses a complete read beyond4096 entries per collection or65536 in total. It neither
discovers nor certifies the native origin of supplied handles. Four RED/GREEN batches and60 focused
Defs cases passed independent review; the local ledger is
`artifacts/Implementation/megamod-p0/s139-native-facts/fact-inputs/implementation.md`.

The F1 guards reuse one finite `DrugGenerationObservedBodies` bundle. Contribution and pawn
projection are followed by the seven-method zero-range initialization proof: actual helper branches,
same-properties calculator flow, explicit Single store and same-comp destination. The final scoped
Harmony checkpoint passes62 cases:43 initialization plus19 contribution/pawn regressions, with the
reviewed five call-replacement cases included. See the local `initialization-checkpoint.md` beside
the F0 directory. These conditional host proofs do not establish native type initialization,
dispatch/inlining, remaining rate/hash/calendar/clamp behavior or complete F0–F5 admission.

`ReviewEdits` is a detached provenance projection, not creation validation. It retains the player's
choices, identifies departures from the proposal, and marks every affected exposure-channel row
when recreation or multiple schedules make exposure uncalculated. It rejects unknown/duplicate
row handles. Reapplying original choices restores computed provenance without mutating an earlier
view. The future draft/commit layer must independently validate name/ranges/applicable flags,
S138 availability, current game/catalog/fact/preference generations and confirmation identity.

`Core/DrugPolicyDraft.cs` now provides immutable whole-row projection, editing, renaming and full
reset from fresh suggestions/preferences. Unsupported rows remain visible. Missing tolerance
support creates only Any values and rejects numeric edits, preserving stored preferences. Complete
edits recompute provenance across affected channels. Basic detached validation rejects unresolved
tolerance choices, unknown row identities and invalid percentages/permissions. This is not a
currentness or creation receipt: native name/frequency/inventory limits, lifecycle disposal and
current facts still belong to the upcoming native controller. Reset confirmation belongs to its UI.

Focused Unit checks cover positive output, float residue, dose/body/decay variation, the native
editing boundary, unsupported values and shapes, identity/channel composition, and edit provenance.
The local RED/GREEN ledger is `artifacts/Implementation/megamod-p0/revision20-drug-generation.md`.
The native projection's seven initial RED/GREEN slices and84 focused Defs checks are retained in
`artifacts/Implementation/megamod-p0/s139-native-facts/implementation.md`; its explicit source links
were added by root. Defs fixtures register all required definitions in initially empty owned databases.
Host success does not accept finalized pharmacology or any player workflow. Root owns review,
canonical build/install, native scenarios and commits.
Independent review added a two-case RED/GREEN correction for body-part-only PostMake metadata;
four affected positive comparator checks still pass. All86 distinct cases and the resolved review
are recorded in the same ledger. No full fixture suite was replayed for that correction.

`Core/DrugGenerationSettings.cs` declares the enabled, initial mood/joy and initial tolerance
preferences through the common store. Capture returns immutable effective preferences for a new
draft. Validation rejects invalid edits without changing prior values; invariant persistence keeps
Any distinct from numeric 100% and retains malformed stored payloads for repair. The two validation
messages exist in all seven baseline languages. These definitions are not registered in bootstrap
or rendered yet; preview staleness, native UI and provider effective-off remain separate work.
The focused preference ledger is `artifacts/Implementation/megamod-p0/revision20-generation-preferences.md`.

`Runtime/NativeDrugPolicyCreation.cs` prepares fresh native rows through S029 storage and overlays
the complete supplied choices, including S138. One transient confirmation appends its prepared
object once, rejects changed captured state or reentry, and preserves the prior list prefix. It
validates complete edits and native bounds, checks the written fresh ordered payload against every
reviewed choice, refuses S138-off numeric edits, and releases transient references on disposal.
Constructed-Defs checks cover preparation failures, IDs, stale state and the production receipt's
owned-tail rollback/conflict refusal. The supplied currentness predicate/native ordering remain
caller assumptions until integrated with actual receipts. Effective constructor/getter admission,
controller lifecycle integration and native player proof remain open; the adapter has no production
caller. The internal failure seam is a Defs proof, not native admission. See
`docs/patch-targets/DG-POLICY-CREATE.md` and the local
ledger `artifacts/Implementation/megamod-p0/s139-native-creation/implementation.md` before continuing.
