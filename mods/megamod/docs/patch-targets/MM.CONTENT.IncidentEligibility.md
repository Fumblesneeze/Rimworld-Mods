# MM.CONTENT.IncidentEligibility: exclude incident candidates

Owner `fumblesneeze.megamod`; S038 content availability, “Select an event with fewer eligible options.”
Candidate composed and independently reviewed with focused host evidence;
native incident eligibility acceptance remains pending.

## Native target and purpose

Assembly-CSharp `RimWorld.IncidentWorker`, public instance nongeneric
`bool CanFireNow(RimWorld.IncidentParms parms)`. The postfix only vetoes true results for the
current worker's excluded `def`; it must preserve native or foreign false results. Native public
instance field `IncidentDef def` supplies the exact identity. No Def, incident weights, or ongoing
event state is mutated. Runtime per-save policy cannot be expressed through static XML.

Inspected 1.7.5000 `StorytellerComp.UsableIncidentsInCategory` consumes this result; native
`TrySelectRandomIncident` preserves its ordinary weight calculation and returns false for no result.
This is source evidence only. It does not establish every foreign incident generator uses this path.

## Admission and ordering obligations

- Resolve the exact callable/field shapes after definitions finalize and require the complete atomic
  incident group, including MM.CONTENT.IncidentExecute. Publish no incident exclusions beforehand.
- Consume only the immutable applied snapshot. No settings reads from the patched selection path.
- Veto after earlier result writers. Later unrelated void observers remain compatible. A later
  result writer whose preservation of false cannot be established makes the group unavailable.
- Reject unproven non-void pass-through postfixes regardless of metadata order: the inspected
  Harmony provider emits its void postfix phase before its pass-through phase, so a pass-through
  method sorted earlier can still replace false after this owned void veto. Read-only void result
  observers remain compatible both before and after the veto.
- Inspect current Harmony metadata and sorted patch methods without executing foreign transpilers
  as a probe. Revalidate metadata before application and on supported explicit repatch notification;
  invalidate incident availability before any further native action if that contract no longer holds.
- Original Cherry Picker active disables the owned group without editing/unpatching the provider.
  Enabled skipped patches each receive the common bounded startup warning and retain requests.
- Use the common AdmissionCoordinator and exact OwnedHarmonyPatch receipts. Group failure disables
  the runtime gate even if physical cleanup throws; it must not claim safe removal after such failure.

No whole-method hash or “no other owners” requirement is proposed. An effective-stream guard is
needed only for consumed body semantics; this veto must also work when an earlier prefix skips the
original and supplies a result. Repatch support beyond the explicit invalidation path is unverified.

## Evidence still required

Scoped Harmony tests must prove native/foreign false preservation, skip-original result handling,
compatible late observers, incompatible later writers, partial group rollback and invalidation.
`IncidentPatchOrderTests` and `IncidentPatchBridgeTests` now cover these host contracts using ordinary
classes, not game Def fixtures. Native weighted
selection, an empty admitted candidate set, save reload/pending state and original-provider cases
remain unrun. The direct IncidentActionStep scenarios exercise the final action boundary separately;
they do not replace weighted storyteller selection acceptance.
