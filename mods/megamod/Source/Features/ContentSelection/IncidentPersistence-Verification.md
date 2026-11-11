# Native incident save-override persistence — observed component workflow

Owner: fumblesneeze.megamod, S038 with the current shared settings/S020 incident page.
This bounded scenario advances the existing content-selection requirements for global/save choices
and application at a new/load boundary. It changes no product code and closes no feature task.
Full S020 windows, S029 native reconciliation, the remaining content families, early load boundaries,
save switching and successful Def-reload recovery remain open.

Root ran the reviewed scenario on 2026-11-08:38/38 steps passed and all8 causal captures were
personally inspected, including actual refusals before reload and a physical ship chunk afterward.
Normal configuration/preferences were unchanged and cleanup passed. Build/action/log evidence is
`artifacts/Implementation/megamod-p0/native-incident-save-override-reviewed.md`. This records the
tested component workflow before the full P0 integration gate; relevant later edits require fresh
affected evidence. The procedure below remains the reproducible scenario contract.

## Exact scenario and isolated setup

Test ID: `megamod.content.native-incident-save-override` in
`tests/Megamod.EndToEndTests/ContentSelection/NativeIncidentPersistenceTests.cs`.
The exact non-Gateway package order is Harmony, Core, megamod; the typed runner appends Gateway.
Run this test alone in a fresh minimized process, without earlier settings browsing or Def reload.
The tracked source fixture is
`tests/Megamod.EndToEndTests/ContentSelection/PrelaunchExcludedShipChunk/Mod_fumblesneeze.megamod_MegamodMod.xml`.
Its SHA256 is `BB07B7C7D44DED87659A38CC7557A5D48032F477989277C15B4B979F64918E41`.
Before the typed dry run, root stages an exact copy under an existing ignored artifacts directory,
for example `artifacts/Megamod/P0-C/native-incident-save-override/prelaunch/`. Compare the source
and staged SHA256 with that expected hash and retain source path, staged path, both hashes and
the tested source revision in the run's evidence manifest (outside the prelaunch directory).
Pass that staged directory as `prelaunchConfigDirectory` for both dry run and live run.
Passing the tracked test-source directory is invalid: the operation requires an existing path
beneath repository artifacts. Keep staging separate from normal Config and verify provenance
again if the fixture changes. No staging or live run has been performed by this authoring chunk.
The runner copies the staged settings file to the disposable profile; never copy it to normal Config.
It excludes only ShipChunkDrop globally, with no save override. Its dictionary shape/class/file name
match the native serialized settings retained in root's isolated runs. The stable key is the existing
ContentKey length-prefixed source/type/Def identity; it is not a new settings authority or XML Def patch.

This prelaunch value is a precondition only. No scenario call writes a global/save preference,
manufactures a chunk, invokes the incident worker directly or changes the applied policy.
The preflight verifies the actual loaded values, an admitted common save lifecycle, an available
owned incident policy with an explicit excluded ShipChunkDrop decision, absent dormant incident
overrides and a native valid placement cell. The one excluded row sorts first independent
of translated labels. This avoids requiring the still-missing minimized native text/scroll capability.

## Native actions and observations

1. Pause and capture the map. Request ShipChunkDrop through typed IncidentAction with expected
   rejection. Capture its visible rejection and unchanged map; corroborate no incoming/landed chunk
   and unchanged fired-history state. A placement failure cannot substitute for the exclusion.
2. Open the ordinary mod settings dialog and capture the first row showing ShipChunkDrop inherited
   and excluded. Click its real Use global checkbox, observe the current-save editor enabled with
   the seeded false value, and capture that view.
3. Click its real current-save checkbox to allow the incident. Read-only snapshots must show exactly
   that save override changed and every global value retained. The row now sorts among allowed rows;
   this scenario does not claim its pending label remains visible after reordering. Close settings.
4. Request the same native incident again. Its visible refusal and lack of physical/history effects
   demonstrate that the edit is still pending, independently of the requested-value assertion.
5. Save and reload through native SaveLoad into a unique disposable save. Await a different game and
   fresh settings binding. Corroborate retained save identity, changed activation generation, the true
   override and untouched globals/unrelated save entries. Also require available owned incident
   policy, a fresh policy reference and native lifecycle receipt/generation for the actual new Game,
   and an explicit applied allowed decision for the same ShipChunkDrop key and Def object.
   Re-resolve the map by its saved unique ID; retain Thing IDs across load rather than dereferencing
   old map/Thing objects.
6. Capture the reloaded map, request ShipChunkDrop normally, and record the exact new incoming
   objects and their contained physical chunks while paused. Let ordinary ticks land an owned chunk,
   pause, frame that real object and capture it. This is the applied-policy outcome; inspecting a
   published policy or a successful JSON response cannot replace seeing the landed physical chunk.
7. Open/capture/close the real developer console. Root inspects the complete flushed log as well.

The scenario reads the existing root-owned runtime through one test-only reflection read of
`MegamodMod.incidents`, then uses its public `PolicyAvailable` and `CurrentAppliedPolicy` APIs.
The latter already validates the private publication's exact lifecycle receipt and game owner.
The scenario correlates that guarded result with the public current `LifecycleReceipt`, including
its owner, saved identity and fresh generation, and verifies receipt/policy stability around the read.
It retains the original policy/receipt for comparison and rechecks the available allowed policy
after observing the chunk. No raw private publication is assumed to be a current policy; the
runtime is never reconstructed or pumped by the test. This rejects a false pass where a saved
request persists but unavailable owned policy silently permits vanilla execution. These reads are
supporting diagnostics; the native refusal/physical landing and personal capture review remain
required. No extra product API or unrelated native scenario is introduced.

Checkbox geometry reuses the already authored native selector workflow: Window margin 18,
Dialog_ModSettings content offset 40, category height 32, navigation section including gap 168,
inheritance offset 32 and save editor offset 54. Coordinates convert captured GUI bounds by UI scale.
Use the captured native window and pointer-scope handles; no OS desktop input or clipboard changes.

## Cleanup and evidence

The Gateway owns pointer-scope failure cleanup; the scenario ends it explicitly on the normal path.
Independent deferred actions restore camera/speed, close only owned windows and delete only the
unique save path that did not exist at arrangement. Native incident results are captured while
paused before ordinary ticking resumes; cleanup destroys only those exact incoming/payload objects.
Unexpected successful refusal attempts also have their paused results captured during cleanup.
Settings changes exist only in the disposable game/profile and are not restored with direct setters.
The runner owns the profile/process, normal-config hashes, credentials and final exact-PID cleanup.

Root owns compilation, dry run, isolated native execution and independent review scheduling.
No artificial host RED is warranted for scenario-only authoring. A pre-reload chunk or a refused
post-reload request is a genuine native failure to retain and reproduce before a product fix.
A passing baseline warrants no product edit. Acceptance requires root's personal inspection of the
before/refusal/editor/reloaded-map/landed-chunk/console captures on the reviewed build, correlated
with native action receipts, serialized choice diagnostics, build identity and cleanup evidence.

## Why successful Def reload is a later milestone

The retained `artifacts/Megamod/P0-C/next-def-generation-research.md` identifies multiple native
queued/copy/finishing callbacks whose errors can be caught independently. Idle or the outer
HotReloadDefs return is not a success receipt. C09 remains a one-way unavailability guard, and the
current catalog/registered rows are fixed. Recovery still needs correlated successful completion,
the original applied policy plus its original save receipt, and same-instance catalog/UI
reconciliation without applying pending choices or duplicating settings. New-key semantics remain
unapproved. Root's successful S094 reload diagnostic proves its own logging workflow, not C03 recovery.
The research's old note about missing typed debug-action projection is superseded by that tooling;
the lifecycle/reconciliation gaps are not. This scenario deliberately uses ordinary save/load only.
