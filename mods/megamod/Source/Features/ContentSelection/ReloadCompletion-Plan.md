# C12: proposed native Def-reload completion receipt

Owner: **fumblesneeze.megamod**, C03/S038. Recorded 2026-11-08 after read-only inspection.
This document proposes the next bounded milestone; it does not authorize runtime implementation,
add hooks, change settings semantics, or establish native acceptance. C09's existing invalidation
latch remains in force. No code, test, build, staging or game operation belongs to this document edit.

The [earlier retained research](../../../../../artifacts/Megamod/P0-C/next-def-generation-research.md)
identified candidate native phases. This plan narrows what their completion could establish and
separates that evidence from subsequent incident-catalog validity and UI reconciliation. It is not
a general lifecycle transaction framework or a proof that every native loader algorithm succeeded.

## Existing native signals and their limits

Inspected game: RimWorld **1.7.5000 rev1275**. The installed `Assembly-CSharp.dll` was rehashed during
this research and matches the retained decompilation identity below. Source locations refer to those
exact retained files; compiler-generated names and IL offsets identify evidence, not version/hash
requirements for runtime admission.

| Existing native surface | Observed fact | Consequence for C03 |
| --- | --- | --- |
| `PlayDataLoader.Loaded`, C#20–22; `HotReloadDefs`, C#411–632 | `Loaded` is not reset by hot reload. The public reload returns after queueing a synchronous Action. It has no returned success/result object. | Neither the public method's return nor `Loaded == true` proves completion. |
| `LongEventHandler.QueueLongEvent(Action, ...)`, C#140 | An optional `Action callback` exists. This HotReloadDefs call supplies no callback and no exception handler. | There is no existing reload-specific completion receipt to consume. |
| `UpdateCurrentSynchronousEvent`, C#407–445 | The event body runs, deferred actions are drained, then the optional callback runs. Escaping body exceptions are caught and the event is cleared. | Becoming idle does not prove the body succeeded. Adding a callback alone would still be insufficient. |
| `ExecuteToExecuteWhenFinished`, C#469–502 | Each deferred Action's exception is caught, logged and discarded; later actions continue. No result/error aggregate is retained or returned. | A successful finisher or queue callback can follow a failed Def-copy batch. |
| `LoadedModManager.LoadAllActiveMods`, C#29 onward | Returns void; public state exposes loaded mod/Def collections rather than a reload outcome. | No inspected public aggregate supplies the needed result. |
| `LoadModContent`, C#189–233; `LoadModXML`, C#258–276 | Per-pack exceptions are caught and loading continues. | A normal outer return is not proof that all provider content loaded; an unrelated caught error is also not automatically an incident-family failure. |
| Hot-reload finisher, C#554–630 | Rebinds comps/body parts, notifies Things, rebuilds map drawing and closes windows. Its ThingComp initialization catch at C#593–596 logs and continues. | Actual finisher completion means that callback completed, not that every engine operation was error-free. |

No adequate existing reload result or error aggregate was found in these owning paths. This does
not justify inventing a universal error-log gate. Expected optional-content failures and unrelated
diagnostics must not be equated with a globally failed Def generation. C03 must establish the
validity needed by the admitted incident family, and leave other families' requirements to their
own implementation slices.

## Minimal proposed milestone: completion evidence, not recovery

Observe only the concrete native reload work and its directly bound callbacks:

1. Identify the queued Action bound by `HotReloadDefs` and correlate one attempt with that delegate's
   actual closure. The inspected binding is `<>c__DisplayClass6_0.<HotReloadDefs>b__3` (outer IL0077).
2. Establish actual entry and normal completion of that queued body. It calls
   `LoadAllActiveMods(true)`, flushes deferred copies at `ForceExecuteToExecuteWhenFinished`, completes
   its resolution/reset work and registers the finisher. An outer postfix or a normal return from a
   delegate wrapper cannot substitute for proof that the original body executed.
3. Observe `ParseAndProcessXML(..., hotReload: true)` completing its actual body and its copy-batch
   registration disposition. C#448–451 is a legitimate zero-copy return. Otherwise C#452 onward
   registers the batch containing `Parallel.ForEach(toHotReloadCopy, ...)`. The inspected batch is
   `<>c__DisplayClass19_0.<ParseAndProcessXML>b__0`; prior IL inspection located its binding at
   IL02ee and `ExecuteWhenFinished` at IL02f9. That IL was inspected to stdout, not retained as a
   separate artifact; the corresponding C# is retained. Absence of a callback alone cannot prove
   the legitimate zero-copy path.
4. Record registration before dispatch and completion/failure of that batch. `ExecuteWhenFinished`
   can dispatch synchronously. An escaping batch exception, including a parallel-copy failure,
   prevents this attempt from becoming a completion candidate even if the event driver catches it
   and continues. That is a conservative limit on unknown partial copy work, not a claim that all
   unrelated game content is broken. Do not instrument every individual copy worker.
5. Establish actual completion of the finisher bound by the same reload closure. The inspected
   binding is `<>c__DisplayClass6_0.<HotReloadDefs>b__4`, queued at body IL0216/IL0229. Its entry,
   completion and association must be proved rather than inferred from `Log.ResetMessageCount`.

The result is evidence that these required phases completed for one attempt. It is only a candidate
for later reconciliation. Main-thread idle is an additional safe observation time, not the success
signal. A newer HotReloadDefs attempt, ClearAllPlayData, game/save receipt change or incompatible
patch recompilation must prevent an old completion from being consumed for a new boundary.

Resolve callback targets from the actual delegate bindings and validate the relevant incoming
instruction streams, callable shapes, receiver association and bypass paths. Preserve native
results, exceptions and scheduling. Unknown binding/skip/exception behavior leaves completion
unavailable. Do not execute foreign patch factories or transpilers as an inspection probe, ban all
foreign owners, hook every queued action, or use every logged error as an invalidation signal.

The final interception form remains an **unresolved design choice**: local registration wrapping
and actual-body observation are candidates, not admitted implementations. The existing converter
demonstrates body-owned try/finally and supported EH/return checks, but its reporting-specific
callbacks are not a reusable proof of reload success. C01's pending inner-effect/helper admission
work must be accounted for before claiming that receipt entry/exit effects cannot be skipped or
altered. No shared framework or shared-helper edits are authorized by this plan.

## First focused executable tests

Use `Megamod.Harmony` with ordinary host types and Actions, individually owned patches and guaranteed
cleanup. Do not load fake active mods, populate a representative Def database or claim engine
acceptance. Acquire the output lease before running either case and retain nonzero RED/GREEN evidence.

1. **First negative case:** a native-shaped deferred driver catches the copy batch's original
   exception, then runs the finishing Action normally. The owned observation must record the copy
   failure, preserve the native exception/continuation behavior and expose no completion candidate.
   An idle queue or successful finisher cannot turn this case into success.
2. **Next positive case:** the same concrete binding shape performs a clean copy. No completion is
   available after enqueue, body return alone, or before the registered finisher completes. Once
   the actual required bodies finish, exactly that attempt becomes a completion candidate. This
   case prevents an always-unavailable implementation from satisfying the tranche.

Subsequent vertical cases cover a legitimate zero-copy return, skipped originals, exception
suppression, changed bindings, synchronous deferred dispatch, delayed completion from a superseded
attempt and changed game/save identity. The exact case order follows the first implementation's
evidence; these are not authorization for a horizontal test/framework scaffold. C09 stays latched
even in the positive test until separately implemented reconciliation can safely consume the receipt.

## Required dependencies before incident recovery

- **Preserve the original publication pair.** `IncidentPolicyLifecycle` already privately owns
  `Publication(Receipt, Policy)`. Retain/expose that same immutable pair at successful publication.
  `IncidentContentRuntime.RetainedAppliedPolicy` currently retains only the policy; attaching a later
  current receipt to it could associate old decisions with a new failed load. This needs a separately
  coordinated API change, not another settings or application authority. Worker observations must
  not read mutable Store or recapture pending requested values.
- **Validate the actual incident catalog and owned hooks.** Completion of native phases is not a
  substitute for unique stable identities, source provenance, current native identity mapping and
  available incident admission. Decide the precise required postconditions before allowing recovery.
  If the available evidence cannot distinguish a relevant failed load from legitimate missing
  content, keep that recovery unavailable; do not treat an empty or partial snapshot as success.
  Do not require proof of unrelated provider/private algorithms to admit a valid incident family.
- **Reconcile the existing catalog/UI instance.** Current rows capture labels/identity once and
  the common renderer has registration, not same-ID replacement/removal. A second Register call
  would duplicate settings/rows. Root must coordinate a narrow reconciliation API or equivalent
  stable view before coherent publication; no runtime reconstruction or duplicate registration.
- **Keep pending choices pending.** Reconciliation must use the original applied key decisions for
  the same original save/load receipt. Nothing in this plan changes the treatment of new keys.
  Their behavior remains an unresolved design decision; the earlier default-allow suggestion is
  not approved and must not be silently encoded.
- **Keep distinct boundaries distinct.** ClearAllPlayData/full mod-list changes must not reuse the
  old canonical active-package snapshot as a recovery authority. Early-load families, S029 native
  policy reconciliation and the other requested content families remain separate open obligations.

Proposed first implementation lease, subject to root approval: a new feature-local
`Harmony/NativeDefReloadCompletionPatch.cs`, its matching
`tests/Megamod.Harmony/ContentSelection/NativeDefReloadCompletionPatchTests.cs`, a narrow update to
`Harmony/NativeContentInvalidationPatch.cs` for attempt supersession coordination if required, and
the owning target/design documents. Root retains central registration, projects, metadata indexes,
builds and native runs. The publication-pair and catalog/UI changes need their own agreed scope.

## Evidence identities and existing-helper baseline

Artifacts below are local retained research, not package content or live acceptance for C12.
SHA256 values pin what was read; they must not become runtime admission predicates.

| Evidence | SHA256 |
| --- | --- |
| Inspected `F:/Steam/steamapps/common/RimWorld/Assembly-CSharp.dll` | `D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254` |
| [Earlier C03 research](../../../../../artifacts/Megamod/P0-C/next-def-generation-research.md) | `A939E83FE683480178F93184540D398D8B05BF50C3EDCBC338BE3389E2E27732` |
| [PlayDataLoader C#](../../../../../artifacts/Megamod/P0-C/native-1.7.5000/Verse.PlayDataLoader/Verse.PlayDataLoader.decompiled.cs) | `7382D74979D32963D9E1269CB852C8B506CF3EC828FBFA02F7FB5DC6F3442A51` |
| [PlayDataLoader IL](../../../../../artifacts/Megamod/P0-C/native-1.7.5000/Verse.PlayDataLoader/Verse.PlayDataLoader.il) | `03C55ED7C24B254D8EA198A313071D3AFAF2C6735CBD388A5326812767EE188B` |
| [LoadedModManager C#](../../../../../artifacts/Implementation/megamod-p0-a/native-1.7.5000/Verse.LoadedModManager.decompiled.cs) | `83EC2F9E45DA0744CFAF3E5EB48692019C920A7C67E7D20E6B8AB9F987EF7338` |
| [LongEventHandler C#](../../../../../artifacts/Megamod/P0-B/native-1.7.5000/Verse.LongEventHandler/Verse.LongEventHandler.decompiled.cs) | `4A42915023CC79AADC12B5EEB24D528C3412010BABC75EE6CC9F5FE9D7E75DCE` |
| [Current C09 latch](Harmony/NativeContentInvalidationPatch.cs) | `BA27603E01CE3148800824B692F5AA95EA5BCBF9D6A8D8B3F9480193C32C62C1` |
| [Existing policy/receipt authority](Core/IncidentPolicyLifecycle.cs) | `F4CE44A6AE5A12AE9D380E7D28EA41987EE3DD2E837014588FB129DA3BF5EF04` |
| [Existing incident runtime](Runtime/IncidentContentRuntime.cs) | `BB734CBA19F35DBFBFA8EF39EF0D09F5F97DA413EF6453A3DD2DE14DB863196E` |
| [Converter body/EH pattern](../../Patching/Harmony/NativeDefinitionConversionPatch.cs) | `EA2F862C2AF5DD4F7D3AE885102C7E9C2F5EAC93E6A6D5FD5F1C9BE7E9027174` |
| [Owned Harmony acquisition/rollback helper](../../Patching/Harmony/OwnedHarmonyPatch.cs) | `D5E96BB82F4891F0A9023234F371CE3FC36CD76883043E215D7FCE6E6E51C2E0` |

Root reports that the separate C11 native save-override workflow passed 38/38 steps and was
personally reviewed in [its isolated run](../../../../../artifacts/EndToEndRuns/Grouped/20261108T011851719Z-4e8b8d65ad5840f190cedda2f2c628de),
product SHA256 `470FEE1A62C2D58026E0BFF02C015CF89D15DF03372D17DE96D10688B91A5984`.
That proves the reviewed ordinary save/load workflow, not Def-reload recovery. Likewise S094's
native reload/log-context workflow does not accept C03 recovery. The earlier research's missing
typed debug-action projection note is superseded by current tooling; its publication/catalog
dependencies remain open. No C12 gameplay or implementation task is closed by this document.
