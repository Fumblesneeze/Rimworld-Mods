# Drug policy generation — C1 core

Owner: `fumblesneeze.megamod`, S139. The owning
[change](../../../../../openspec/changes/specify-megamod-drug-policy-generation/design.md)
defines the complete feature. The C1 calculation has an independent no-finding review. Detached
draft editing is implemented with an independent no-finding review; native projection, settings/UI and publication
are not implemented or accepted.

`Core/DrugPolicySuggestions.cs` consumes immutable values and opaque canonical identity handles.
It reuses S029's `DrugCatalogEntry`/`DrugPolicyChoices` and S138's immutable tolerance choice. It does
not inspect a Def database, install patches, schedule jobs, modify policies or save data. Unit's
existing `Source/**/Core/**/*.cs` link includes it; no project/source-link change is needed.

`Create` produces one row per distinct current catalog identity in the supplied native order. It
computes the reviewed C1 binary32 model, withholds unsupported shapes and unrepresentable intervals,
and connects exposure through actual chemical **or** tolerance reference identity. Unsupported rows
can connect a channel, but a channel needs multiple otherwise eligible candidates before automatic
schedules are withheld. Display names/equality overrides do not establish identity.

The future native fact reader must supply `OrdinaryWholeDose` only after admitting the complete
quantity, effect, tolerance and deterministic-decay contract in the design/research. Other enum
values preserve a concrete unavailable reason. Passing ordinary numeric fields is not admission.
The scalar fields retain raw values so malformed/nonfinite input cannot be normalized into a
permissive proposal. Body-size division follows the admitted giver flag and supplied actual adult
baseline; no gain is clamped to a threshold or maximum to manufacture eligibility.

Every set carries its baseline and the `NativeBatchingMayLeaveTolerance` disclosure. C1 is a
cadence-based suggestion, not a bound on actual native clearance. Runtime UI must translate and
display the full population/exposure/batching assumptions before Create; no live reader or UI is
present in this tranche to establish that behavior.

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
Host success does not accept finalized pharmacology or any player workflow. Root owns review,
canonical build/install, native scenarios and commits.
