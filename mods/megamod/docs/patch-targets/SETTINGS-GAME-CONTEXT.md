# SETTINGS-GAME-CONTEXT: detach settings when the game changes

Owner: `fumblesneeze.megamod`; C02, foundation common-settings behavior, orchestrator bootstrap.
**Implemented, independently reviewed and startup registered with the lifecycle pump.
Full native save/lifecycle acceptance remains pending.**
Validator/owned bridge: [CurrentGameObserverPatch](../../Source/Patching/Harmony/CurrentGameObserverPatch.cs).
Registration: [MegamodMod](../../Source/MegamodMod.cs). Persistence/lifecycle:
[SettingsRuntime](../../Source/Settings/Runtime/SettingsRuntime.cs).
Host fixtures: [CurrentGameObserverTests](../../../../tests/Megamod.Harmony/Patching/CurrentGameObserverTests.cs).
Native scenario: `megamod.settings.native-page-lifecycle` (authored, not run).

Observable contract: [main-menu disabling and save switching](../../../../openspec/changes/add-megamod-foundation/specs/megamod-ui-placement/spec.md),
especially Open settings from the main menu and Switch saves or open settings in world view.
Current source evidence: `artifacts/Implementation/megamod-p0/native-1.7.5000/Verse.Current/`.
Inspected Assembly-CSharp SHA256
`D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254` identifies evidence only.

## Target and minimal predicates

Public static non-generic `void Verse.Current.set_Game(Verse.Game)` plus public static non-generic
`Verse.Game Verse.Current.get_Game()` in Assembly-CSharp. The inspected setter assigns its field;
the owned postfix observes the actual public getter after the native call. It does not alter the
argument, assume that the original body ran, or infer that the requested game became current.

Admission checks the exact consumed callable shapes and the initialized common settings runtime.
No whole-body IL, field layout, exact game hash/version, or foreign-owner absence predicate is
required. A prefix that skips the original and leaves Current.Game unchanged must not detach an
otherwise valid binding. Compatible observers and unrelated body changes are allowed.

A GameComponent supplies new/load finalization but has no exit-to-menu callback. This one observer
provides identity-change invalidation; component finalization still supplies loaded data and never
activates a partially loading game's controls. Map presence does not define save availability.

## Activation and failure

Install through C01 after serialized global-settings initialization and before any game is created.
Only successful admission enables C02's save-binding capability. Failed required admission keeps
save activation unavailable, preserves persisted overrides, and warns once about unavailable save
scope; global-only settings remain independent. Features requiring that capability must not claim
their inherited or overridden behavior is available. Component callbacks cannot bypass admission.

An actual identity change can occur on a loading worker. Publish its immutable identity/generation
and revoke the previous save's mutation token there; do not touch mutable settings storage or invoke
store change callbacks on that worker. The [main-thread lifecycle pump](settings.lifecycle-pump.md),
admitted in the same atomic group, captures
and detaches previous owned state. A later finalized snapshot activates only for the exact current
game/generation after loading completes. Repeated same-identity observations are idempotent.
Disabled/unavailable capability and supported invalidation detach safely. The pump and worker-safe
runtime replace the earlier synchronous adapter under plan revision4; review and native acceptance remain open.

Rollback owns only a newly added postfix. Preserve foreign and preexisting owned patches. Supported
repatches retain the existence/callable predicate; missing effective observation invalidates the save
capability before reuse. Do not repeatedly execute foreign transpilers to inspect them. Direct
foreign writes bypassing the public game lifecycle are not a verified compatibility claim.

Initial synchronization and setter callbacks share the same guarded observation path.
Observer failure disables further owned callbacks and calls the root failure handler once to disable
save scope and report the unavailable behavior. The native setter retains its result. Failure while
reporting is retained in the bridge rather than replacing a game exception or repeatedly retrying.

## Verification state

| Case | Required result | Status |
|---|---|---|
| Normal current-game setter | Observer receives actual getter result; old save detaches before another activation | Host observation passed; native save lifecycle pending |
| Skip-original foreign prefix leaves same actual game | Observer receives unchanged actual game; binding remains | Exact owned Harmony fixture passed; native workflow pending |
| Foreign compatible observer/unrelated body change | Admitted; other owner preserved | Pending Harmony fixture |
| Changed return/generic/parameter shape | Observer unavailable with target-spec warning | Host fixtures passed |
| Repeated admission check before rollback | Original owned receipt retained, foreign owner untouched | Host fixture passed |
| Observer callback fails | Callback inactive, native setter still works, failure reported once | Host fixture passed; native scope disabling pending |
| Initial current-game getter fails | Same bounded unavailable fallback; constructor does not propagate the getter failure | Host fixture passed; review recheck pending |
| Missing target or failed install | Save capability inactive, overrides retained, bounded warning | Shared admission/receipt tests; exact native workflow pending |
| Menu, same-save reload, another save, world view | Correct active controls and values; stale callbacks rejected | Pending native settings/save workflows |
| Later invalidation/revalidation | No stale active save scope, no duplicate patch | Pending lifecycle tests and native evidence |

Nine focused host cases pass with current installed Harmony. Exact source/build and RED/GREEN
evidence: `artifacts/Implementation/megamod-p0/settings-observer-ledger.md`.
Later invalidation/revalidation is still pending integration; no blanket dynamic-repatch compatibility
is claimed. No direct state mutation can replace native save/menu/UI actions in final acceptance.
