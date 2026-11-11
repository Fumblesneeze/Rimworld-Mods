# MM.CONTENT.IncidentExecute: refuse new excluded incidents

Owner `fumblesneeze.megamod`; S038 event exclusion and preserved existing state. Implemented
candidate, centrally composed and independently reviewed. Native execution acceptance remains pending.

## Native target and minimal contract

Assembly-CSharp `RimWorld.IncidentWorker`, public instance nongeneric
`bool TryExecute(RimWorld.IncidentParms parms)`, with public instance `IncidentDef def`.
The prefix reads the exact current worker Def and refuses an applied exclusion by setting
false and skipping the original, with a localized player-visible reason. Allowed calls proceed
unchanged. The native body calls `TryExecuteWorker`, records incident fired state, and may update
codex discovery. Refusal must precede all those native side effects. Existing incident outcomes,
quests, pawns and physical Things remain untouched. Static XML cannot represent this per-save gate.

An owned late prefix consumes earlier prefix state. A subsequently ordered prefix that can replace
the worker identity requires a proven safe contract or makes this group unavailable. Unrelated
observer prefixes are not grounds for rejection. `IncidentPatchOrder` checks actual sorted metadata;
priority alone is insufficient because before/after constraints can override it. Foreign generators
that directly perform effects outside the native body are outside this ordinary-path coverage and
must not be silently presented as supported.

## Atomic admission and failure

Pair with MM.CONTENT.IncidentEligibility, MM.CONTENT.IncidentExecuteResult and the two Generation
observers through the common AdmissionCoordinator and exact OwnedHarmonyPatch receipts. Publish
the incident policy only once all five hooks and current metadata
predicates are admitted. Both callbacks must obey the inactive gate after failed install/rollback.
Actual native callable/field semantics and their identity association must be guarded; signatures
are not blanket proof of third-party behavior. Use current metadata ordering, never speculative
foreign transpiler execution. Revalidate before policy application and on explicit owner refresh;
compilation observers revoke availability during normal rebuilds retaining those observers.
Foreign removal of an owned observer is outside that rebuild receipt; explicit refresh detects its
absence. Original Cherry Picker suppresses this owned family only.

Skipped enabled-patch warning must name this ID/target/spec, failed predicate, unavailable incident
exclusion and native fallback. Requested choices survive. Do not log one warning per incident tick.
Failed native drug repair remains a separate publication dependency when drug choices are included;
its distinct restoration failure must not be described as safe inactivity.

## Executable scenario registrations

- `megamod.content.allowed-incident-catalog` in `tests/Megamod.EndToEndTests/ContentSelection/` invokes
  the typed native incident command, waits for an actual ship chunk to land, frames/captures it and
  verifies original Def identity. It is a baseline, not exclusion proof.
- `megamod.content.excluded-incident-action` requires a disposable run whose loaded requested choice
  already excludes ShipChunkDrop, proves a valid native placement cell exists, invokes that same
  native command with explicit expected refusal, captures the result and checks no new incoming/
  landed chunk or fired-history change. It never directly writes product settings or effects.
- UI editing → pending → save/load → applied → reallow and ordinary weighted selection remain
  additional required scenarios, alongside original-active/installed-only controls.

Both registrations are authored but unbuilt/unrun by this implementer. Root owns exact reviewed
build, ordered mods, config hashes, process identity, live screenshot inspection and cleanup proof.
