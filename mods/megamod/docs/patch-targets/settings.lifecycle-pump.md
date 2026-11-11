# SETTINGS-LIFECYCLE-PUMP: main-thread settings handoff

Owner: `fumblesneeze.megamod`, foundation common settings. This patch and SETTINGS-GAME-CONTEXT form
one atomic required capability; root owns registration. Native acceptance remains pending.

Target: public static non-generic `void Verse.LongEventHandler.LongEventsUpdate(out bool sceneChanged)`.
Inspected 1.7.5000 Assembly-CSharp SHA-256
`D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254` identifies evidence, not admission.
Root.Update calls this before ordinary UI updates. The postfix observes no arguments, preserves the
out value and calls SettingsRuntime.ProcessPendingLifecycle. Validate declared public/static exact
bool-byref parameter, void return, non-generic closed shape and out-only metadata; retain the acquired
OwnedHarmonyPatch receipt across revalidation. No XML or public component callback supplies this
main-thread transition before ordinary UI, and no owned method is patched.

Implementation: `Source/Settings/Harmony/SettingsLifecyclePumpPatch.cs`; tests:
`tests/Megamod.Harmony/Settings/SettingsLifecyclePumpTests.cs`. Other owners and unrelated IL changes
are permitted. A foreign skip-original prefix does not establish completion: runtime independently
checks actual main thread, queued events, game identity/generation and Playing state. No hash/body
fingerprint or absence-of-owners guard is used. Later patches remain compatible only while those
callable and actual-state conditions hold; readiness is checked on every invocation.

Inspected LoadGame/Root_Play queue initialization asynchronously; component FinalizeInit precedes
the rest of Game.FinalizeInit. Worker callbacks therefore publish immutable mailbox requests and
atomically revoke old tokens only. The main-thread pump captures/detaches old state and activates
only a finalized current candidate after all long events finish. UI entries reconcile too. Revoked
tokens reject edits immediately and effective reads use globals. Same-owner observation is a no-op.
Native owner reconciliation compares the immutable mailbox reference captured before the getter,
under the publication lock, before applying the returned owner. A racing publication causes that
read to be discarded, preserving the newer owner and finalized snapshot for the next pump.
The main-thread SettingsLifecycleEntry prevents Changed subscribers reading CurrentBinding from
recursively activating. The candidate stays unpublished during Changed; the outer entry publishes
only after current-owner and receipt checks. Revoked bindings are never returned by CurrentBinding.
ExecuteWhenFinished is not a dispatcher: its plain action list may execute immediately on the caller.
Failed loading queues an Entry transition clearing Current.Game; queued events withhold activation.

LoadingVars publishes the immutable detached component snapshot before world/maps initialize for C03.
Main-thread Changed publishes edited save snapshots. ExposeData(Saving) serializes that reference
without Store access, regardless of caller thread. Native SaveGame synchronously invokes SafeSaver/
Scribe on its caller; that alone is not a main-thread guarantee. Snapshot availability is distinct
from UI-binding readiness. Worker decoding requires separately safe codecs/validation.

Enable only after both group members are admitted. Missing shape/install/callback failure disables
save scope while preserving globals and persisted overrides. All observer/pump/UI getter failures
share runtime ReportLifecycleFailure: revoke/disable before one protected diagnostic notification.
Unavailable UI reads never retry the failed getter. Root supplies the bounded sink at Initialize;
warnings name the patch/capability, reason, unavailable save controls and global fallback. Rollback
removes only owned receipts, never foreign patches. Diagnostic failure cannot break fallback.

Six real Harmony cases cover interception after enable, unchanged out value, ref/return/generic
drift, bounded callback failure, foreign skip-original and owned rollback after revalidation. Pure
tests cover worker revocation, stale readiness, detached decoding, failed getter containment,
publication during the native owner read and real Store.Changed binding-read reentrancy. The latter
uses the same pure entry guard as runtime; native adapter wiring remains source-reviewed evidence.
They do not execute native Scribe/GameComponent/loaded games. Native acceptance still needs actual
new-game/save/load/menu actions, visible disabled/bound settings, preserved values, exact-process
captures, supporting thread evidence and cleanup on the reviewed build.
