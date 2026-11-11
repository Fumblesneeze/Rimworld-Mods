## Context

Sole owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S029 has host-tested policy and native data adapters; native lifecycle/UI integration remains incomplete.
[Research](../../../mods/megamod/docs/refinement/2026-11-02-drug-policy-fix-research.md)
pins the source, installed assembly, native beta and narrow Cherry Picker evidence. The source's
late repair duplicates native backfill, misses some stale content and can fail before reaching its
LoadedGame hook. Its underlying-list alphabetization also changes native job selection.

## Goals / Non-Goals

**Goals:** current native policy rows; preserve player choices and identity; safe unavailable-drug
preset handling; alphabetical navigation without changed job priority; exact new-row defaults;
direct owned content/scheduling composition; minimal semantic admission and bounded diagnostics.

**Non-goals:** new drug mechanics, new policy UI, prescribing drug schedules, physical-stock filtering,
health/withdrawal repair, general corrupted-save recovery, source API/package emulation, multiplayer
acceptance, historical deleted-row restoration or a duplicate map index.

## Decisions

### 2026-11-11 ordinary startup master

`drug-policy.enabled` defaults true and uses the common global-only Restart definition. This narrow
scope exception follows the inspected preset factory timing documented below: constructors may run
before any current-save authority. Root captures the effective Boolean once after global settings
load and returns before owned preset registration when false. UI edits remain pending until restart;
worker factories never consult the mutable settings store. Future maintenance and presentation
registrations must consume this same startup master, without a second provider-specific gate.

The exact active original mapping returns an immutable master-off reason plus one bounded startup
warning. Root combines the reason with other features before common store construction. Stored
global preferences, native policy data and the shared Def lifecycle guard are retained. This adds
no original-provider compatibility claim and does not close remaining native maintenance tasks.

### 1. Complete native maintenance at the relevant lifecycle boundary

Keep native DrugPolicy/DrugPolicyEntry objects and their ordinary Scribe representation. Prefer
narrow corrections around native initialization/post-load and preset-copy behavior over replacing
the database or installing source PatchAll. Reinspect effective 1.7 code before implementation.
Native PostLoadInit already drops nulls/backfills before GameComponent.LoadedGame; a late observer
cannot alone prevent InitializeIfNeeded's stale-component access or NewDrugPolicyFromDef's throwing
lookup. The admitted maintenance group must encompass the early repair/preset prerequisites it claims.

Reconcile only after the relevant catalog is finalized; constructor/preset creation can precede an
active Current.Game. Use the actual policy/database passed by the native path, never a global game
assumption to repair a different or partially constructed game. Do not hold a pending stale result
across catalog changes. Owned S038 supplies a completed-content notification directly; external
providers need exact lifecycle evidence before admission. If filtering runs too late, a LoadedGame
postfix is not a substitute for the specified early preset protection.

Catalog eligibility is active registered ThingDef identity, Item category and IsDrug, plus the
supported engine shape required by native policy operations. Current physical stock, research,
reservation or map reachability is irrelevant. Nonnull removed references and Cherry Picker's
no-longer-drug objects are not catalog members. Unresolved malformed drug components remain an
admission gap, not permission to synthesize a missing comp or silently swallow all initialization
errors. Define the precise unsupported-provider scope in individual target docs before coding.

Prepare a policy result from a snapshot before mutation: retain the first entry per eligible drug
in its existing effective order, remove invalid/duplicate identities, add genuinely missing entries,
and normalize only inapplicable addiction/joy flags. Where native initialization/post-load orders
the complete list, preserve that ordering operation. For an admitted content addition outside that
path, apply the same effective native whole-list ordering before the repaired policy is used;
merely appending new rows is not the final result. Choose duplicate winners before ordering.
Retain valid entry objects so actual external readers and UI edits keep their identity. Snapshot
their values as well as list membership: restoring a list alone cannot undo an in-place flag edit.
Commit once, restore on failure, and verify the expected catalog/row identity before publishing.
Avoid recursing through a getter that itself initializes or re-enters reconciliation.

Preserve native underlying order established by its normal initialization. At cleanup-only boundaries
where native initialization does not otherwise reorder, retain surviving relative order. Additions
use the full native listOrder ordering, including its admitted tie behavior, never localized display order. This
does not promise reconstruction of an unknown pre-source order already saved by another mod.
Native load initialization remains the baseline where it sorts. Do not sort the policy database:
policies[0] is the default and native load identity includes policy label and ID.

### 2. Preserve actual 1.7 defaults and fresh-preset semantics

New rows in existing policies and fresh blank policies use addiction = IsAddictiveDrug, joy=false,
scheduled=false, daysFrequency=1, both thresholds=1, takeToInventory=0. This is native initialization
followed by the source's applicable-value normalization. Record named constants for these defaults;
they belong to policy data and do not justify a new settings surface. Never infer newness from
values matching the defaults; a player can intentionally choose those exact values.

Do not revive source label comparisons for SocialDrugs/Unrestricted/OneDrinkPerDay. On an ordinary
1.7 load native backfill already prevented that missing-row branch from running. Choosing stable
sourceDef-based pleasure inheritance instead was considered, but would still introduce automatic
permissions absent from the inspected actual load path. Keep that possible preference change in
DP-Q1 rather than treating it as source parity.

For new **preset** policies, preserve actual DrugPolicyDef.allowPleasureDrugs and valid explicit
entry.CopyFrom results, then normalize only inapplicable flags. The target is the preset loop's
drug-index lookup/copy relationship: filter only unavailable source rows before the lookup and keep
the native remainder. Do not catch ArgumentException around the whole factory or replace global
DrugPolicyDef.entries. Valid explicit overrides must execute in original order; this draft does
not invent semantics for malformed external presets. Never reapply a preset when merely loading,
renaming, copying or repairing an edited policy.

### 3. Sort presentation by identity, not gameplay data

Native Dialog_ManageDrugPolicies draws SelectedPolicy entries in list order; the scheduled and
inventory job givers consume the same order. Build a stable display projection of eligible entry
references keyed by drug identity and current translated label. A culture-appropriate label compare
with ordinal defName tie-break is a candidate; confirm label fallback and duplicate-label usability
in native UI. Do not swap entriesInt temporarily around drawing: callbacks/reentrancy can expose
the presentation order to native consumers and foreign readers.

Patch only the row-enumeration boundary if supported, preserving native controls, selected policy,
tooltips, numeric editing/focus and scroll behavior. A row index is not identity. On a catalog,
policy or language change, rebuild the projection at a safe UI boundary; preserve pending input
for the same drug or cancel it if that row disappeared. Do not commit to a drug that moved into
the old position. Native copy/default/delete/assignment controls remain unchanged. Cache a bounded
display projection only if useful; no map Things or off-thread Verse access.

Display can fail independently to native ordering. Maintenance must not depend on presentation.
When exact original `Mlie.DrugPolicyFix` is active, log a bounded startup warning and route the
owned feature through the ordinary per-feature settings disable path. This best-effort action uses
the same effective off behavior as a user disabling it; it is not a second compatibility mechanism
or a promise about the original's sorting/repair. Detect canonical active package identity, not
installation on disk. Never unpatch the original or modify its settings.

### 4. Direct composition and absence-safe external integration

S038 will finalize content and invoke reconciliation, not patch an owned Cherry Picker replica.
Cherry Picker's inspected drug-category clearing remains source-behavior research. Current native
catalog facts still exclude no-longer-drug definitions, but no original Cherry Picker API, private
reprocess list or dedicated coexistence adapter is implemented or verified for replacement parity.
If a future declarative fix requires XML, specify it separately with XML Extensions guards,
additive preservation and dependency metadata; this draft requires no XML patch.

S024's potential-mood gate and native Pawn_DrugPolicyTracker remain authoritative for permission
and timing. Reconciliation must not clear valid drugTakeRecords or force a job. S021/S024 use the
same normal carried-supply targets; no extra inventory, hauling permission or capacity is granted.
The catalog is game-content data, not Ctrl+F/hauling stock data. Reuse finalized catalog facts and
native listers, invalidate on admitted content/language changes, and discard per-game projections
on teardown. No per-tick scan or speculative shared cache framework.

Native policy types, fields and lifecycle remain the preserved game contract. Original-mod
API/binary/save compatibility checks, consumer-parity scans and DrugPolicyFix/DrugPolicySort/
NonPublicFields facades are outside scope, including future facade work inferred from a consumer.

### 5. Semantic target families and failure boundaries

The following are candidate families, **not completed patch target documents or executable checks**.
Before any runtime patch, split into stable-ID docs from the mod template and link owning scenarios,
actual target, consumed predicates, effective interactions, validator, tests and live evidence.

| Family | Minimum consumed semantics and failure scope |
| --- | --- |
| Policy initialization/post-load | Actual instance row storage, native add-if-missing behavior, current catalog and execution before unsafe lookup/sort; if changing an input/candidate stream, prove its use in that specific loop rather than signature alone; fail the maintenance group |
| Preset construction | Each explicit row's drug is used in the target policy lookup then CopyFrom; uniquely scoped skip only for unavailable content, preserve valid copies and return/lifecycle; fail the maintenance group if absent/ambiguous |
| Creation/copy completion | Successful returned/copied policy identity and completed row state; observer existence/shape only where original body need not execute, otherwise include skip-original/effective-result semantics; no callback on an unrelated global game |
| Native dialog row enumeration | Effective selected-policy entry access, per-row controls and identity/focus correspondence without mutating gameplay list; unsupported shape disables presentation only |
| Optional provider finalization | Canonical package plus actual content finalization/result timing; an existing callback alone cannot prove final content; reject unsupported adapter, withhold claims for its affected catalog |

Inspect relevant effective prefix/transpiler interactions, allow unrelated edits/foreign owners,
and revalidate supported later repatching. Each transpiler validates its actual incoming stream on
every run and returns it unchanged on failure. Never run foreign transpilers repeatedly as probes.
No hash/version pin, pristine-body check or no-other-patches rule constitutes admission.
False/unknown predicate, validator exception or install failure rolls back only newly installed
owned patches in the declared atomic group. One bounded startup warning per skipped enabled patch
states package/patch/spec ID, target, predicate, unavailable behavior and fallback. An explicitly
disabled feature is not a failed candidate. The original-loaded startup warning is separate from
semantic admission failure and invokes only the ordinary settings disable path, best effort.

Repair warnings at runtime are bounded per failed policy/catalog event, not per tick/draw. Successful
change summaries count actual policies and each row action across all policies; no-op normal loads
stay quiet. Duplicate resolution is reported because it discards conflicting choices. No automatic
save write, global catch-and-continue or claim that a rejected early repair guarantees load success.

### 6. Verification and balance boundaries

Pure deterministic catalog identities/default decisions/duplicate resolution and counts fit Unit.
Lightweight constructed Def property fixtures fit Defs with an initially empty scoped database.
Owned patch-shape fixtures belong to Harmony with guaranteed owner cleanup; real native loaded
Defs, PatchOperations, initialization order and provider callbacks require an isolated game.
Do not manufacture a loaded provider with LoadedModManager mutation or claim synthetic assertions
prove a native policy workflow.

On the reviewed runtime revision, use real policy management, rename/copy/assign/default/save/load
and pawn drug-taking/refill paths. Prepare distinct native/alphabetical priorities, enable ordinary
schedules through the UI, then observe the actual first drug/quantity, dose timing and restrictions.
Include an admitted in-session content addition with an earlier native drug priority; configure
both drugs through native controls and compare selection before versus after save/reload with the
same eligibility/history. Use saved fixtures with removed drugs and a fresh preset containing a removed reference to prove
early recovery; a clean startup log alone is insufficient. Opening/editing the native window and
using the resulting policy connects the load repair to player behavior. Verify owned content-change
composition and any genuine admitted content integration in its required environment. Verify the
ordinary feature-off path and bounded original-loaded warning without adding an original-provider
compatibility run or inferring 1.7 support from metadata.

Semantic balance comparators are the exact native new-row baseline, Core SocialDrugs/NoDrugs/
Unrestricted/OneDrinkPerDay and the source's actual 1.7 load path. Defaults match the latter load path;
fresh explicit presets retain their final choices; no costs/yields/research or health are rebalanced.
Alphabetical display avoids language-dependent drug priority. Finalized provider definitions and
actual addiction/joy/scheduled/refill outcomes still require final live balance/playability review.

## Risks / Trade-offs

### P0-C implementation tranche — 2026-11-07

The first pure policy slice lives in `Source/Features/DrugPolicyMaintenance/Core` and is
source-linked into `Megamod.Unit` without Verse, Harmony, or a populated Def database.
`DrugPolicyReconciler.Plan` consumes detached choices plus opaque reference-identity handles for
the actual drug and row objects. An equal name/key is not proof that an old drug reference is the
currently registered Def. Planning does not change the supplied rows; the engine adapter will
apply a complete plan while preserving retained native row objects. Native whole-list ordering
will be supplied by the engine adapter, including native tie behavior, instead of invented here.
This tranche is neither a runtime adapter nor accepted gameplay. Existing task boxes stay open.

Read-only decompilation of the installed 1.7.5000 rev1275 assembly on 2026-11-07
(SHA-256 `D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254`)
confirms the earlier `DrugPolicy.InitializeIfNeeded` and `ExposeData` shapes. Additional lifecycle
inspection found that `Game` constructs its drug database in a field initializer before
`FillComponents`; `SavedGameLoaderNow.LoadGameFromSaveFileNow` constructs two Game instances.
`Game.ExposeSmallComponents` loads the drug database before the component collection, and
`Game.LoadGame` finishes small components before loading world/maps and calling
`Scribe.loader.FinalizeLoading`. Therefore early preset recovery must use finalized content
identity without assuming `Current.Game` is the policy's owner or using a previous save's applied
policy. Save overrides can be available after the small-component LoadingVars pass, before
post-load reconciliation. Exact admission and native action evidence remain required before
choosing or accepting a publication hook. These hashes identify research, not runtime admission.

The next host tranche introduces one explicit engine-storage boundary, `IDrugPolicyStorage`,
with capture, apply and restore operations. `DrugPolicyStorageSnapshot` detaches row membership
and immutable choice values, keeping opaque original list/entry handles only for identity and
restoration. Batch orchestration prepares every plan before mutation, marks the current target
attempted before applying it, and restores that target plus earlier targets in reverse on failure.
Rollback failure is a distinct unsafe failure, never a successful inactive result. The C03 caller
prebuilds its immutable candidate and publishes only its own reference after a successful batch;
there is no arbitrary publication callback or second reconciliation implementation. Revalidate
the exact catalog and game/save generation before mutation and before publication. All mutable
native state belongs to the game thread; detached settings resolution follows C02's ownership.

Native storage admission must establish private instance `DrugPolicy.entriesInt` as
`List<DrugPolicyEntry>` and the exact writable public choice field shapes on `DrugPolicyEntry`.
Capture via the field, not a getter that initializes or assumes non-null storage. Retained entries
keep their object identity and numeric edit buffer; normalizing permissions must not call preset
`CopyFrom` or edit a pawn's dose history. A null stored list with an empty valid catalog still needs
an actual empty list after successful native initialization even when the pure plan's change
counts are zero. Restore the original nullable list identity and original choice values on failure.
These adapter obligations are not established merely by the host storage double.

Current read-only `Verse.GenCollection.SortBy<T, TSortBy>` uses `List<T>.Sort` with the selected
value's `CompareTo`. Its tie ordering is not promised stable. The native adapter must invoke the
native whole-list ordering over the prepared sequence without adding a label, key, or stable-sort
tie-break. Current `DrugPolicyEntry` and `GenCollection` decompilations are retained with the other
1.7.5000 input evidence. Exact admitted fields and native workflows remain unimplemented here.

### S029 native storage tranche — 2026-11-08

`NativeDrugPolicyStorage` now supplies the real `DrugPolicy` storage boundary to the reviewed pure
batch. It reads `entriesInt` directly, detaches drug handles and all seven choice values, and retains
the original nullable list and entry handles for rollback. Apply builds replacement membership in
the already planned order, reuses retained entries, creates only missing entries, and writes the
planned values without touching the numeric edit buffer. Restore reinstates original membership,
entry values and the exact original list reference, including null storage. It performs no sorting,
catalog discovery, policy discovery, provider detection, logging, settings access or publication.
The eventual admitted caller must supply the finalized eligible catalog and exactly one native
ordering operation when additions require it; it must not sort again after this adapter writes.

Six focused `Megamod.Defs` cases pass against constructed native objects and an initially empty,
exclusively scoped ThingDef database. Four successive behavior changes have actual RED/GREEN
evidence; null-storage and multi-policy failure regressions then passed without further production
changes. These fixtures supply explicit catalog facts; they do not prove XML-loaded eligibility,
provider finalization or a lifecycle hook. The standalone adapter has no registration or native
callback. Field-shape admission, ordinary feature-off wiring, early init/preset/load ordering,
content-policy publication and the native editor/use workflows remain open. See
[the storage verification record](../../../mods/megamod/Source/Features/DrugPolicyMaintenance/NativeStorage-Verification.md)
for exact tests, artifacts, and the next native scenario dependency. No gameplay task is closed.

### S029 native catalog tranche — 2026-11-08

`NativeDrugCatalog.Capture(Func<ThingDef, bool> isAvailable)` now captures the current registered
ThingDef references in native database enumeration order. The availability delegate is mandatory
and belongs to the caller's finalized content decision; this adapter has no settings, provider,
save-generation, content-reload or cache authority. The caller must invoke it at a supported native
boundary with stable finalized availability, then use the existing catalog/save-generation checks
before applying any prepared policies. No installed-mod lookup or map/stock/research check occurs.

Native inspection confirms `IsDrug` requires ingestible data with a non-None drug category,
`IsPleasureDrug` requires a drug with joy greater than zero, and `IsAddictiveDrug` reads the first
native drug comp's addictiveness greater than zero. Capture requires Item category and IsDrug before
consulting availability. Explicitly unavailable candidates are skipped before comp/addiction/joy
access. An available candidate with null comps or no `CompProperties_Drug` rejects the complete
capture with its ThingDef name in the diagnostic. It must not be quietly omitted and then treated
as removed by reconciliation. This is the concrete unsupported-content condition in the owning
spec: no owned policy write, no complete-catalog result, and no claim of repairing the native or
provider loader. Bounded reporting and disabling the affected maintenance attempt belong to the
future admitted caller; the adapter does not log independently.

Accepted candidates yield read-only entries with detached addictive/pleasure values and the actual
native Def references. No name-based replacement or ordering occurs. Native initialization sorts
by `GetCompProperties<CompProperties_Drug>().listOrder`; ordering remains the existing caller-owned
operation and must execute only where the owning reconciliation contract requires it. The adapter
does not invent finite/range restrictions or tie-breaks that native float comparison does not need.
Eight focused real-Defs cases now pass; storage remains at its independently reviewed hash. This
tranche does not admit the early native hooks or resolve the missing S038 save/reload boundary.

### Next bounded native intervention: preset explicit-entry guard (implementation in progress)

[MM.DRUG.PresetEntries](../../../mods/megamod/docs/patch-targets/MM.DRUG.PresetEntries.md) specifies
the first proposed caller. It inserts a conditional skip immediately before the existing
`newPolicy[sourceEntry.drug].CopyFrom(sourceEntry)` operation in
`DrugPolicyDatabase.NewDrugPolicyFromDef`. The native creation, label/sourceDef assignment, broad
pleasure loop, valid lookups/copies, loop advancement and return are retained. It does not invoke
DrugPolicyBatch, sort, normalize completed presets, or rewrite native preset definitions. The
reviewed batch/storage are deliberately not called just to create an orchestration path. This is
the baseline removed/no-longer-drug-entry milestone, not completion of the preset requirement.
The owned Harmony guard is now frozen for review recheck with 36 focused passing cases,
including actual RED/GREEN for missing-entry recovery, unavailable-baseline reporting, benign
aliases/nops, relevant inner decision/count effects, escaped receiver identity, bypassed creation
and a repatch during capture. The review P2 correction additionally rejects branch/switch entry
strictly after the matched receiver load through the creation store, including intervening nops:
the earlier store-dominance check admitted an alternate policy already on the stack. Entry at the
receiver or preceding nops stays compatible because it executes the intact creation chain. Actual
foreign-transpiled factories reproduced both false admission and the first correction's excessive
receiver-entry rejection before their respective fixes; the affected fixture now passes 36/36.
The target document retains raw RED/GREEN artifacts and exact source hashes for recheck.
It preserves native broad pleasure permission and all seven explicit choices; compatible outer
result transformations remain foreign behavior. This is host evidence only. Central publication,
native wiring, provider suppression and player acceptance remain open. No batch/init repair or
gameplay task is completed by this guard result.

At the native body's entry, capture one immutable finalized-baseline reference into a local. At
the existing source-drug load, test that already-evaluated identity against that baseline before
the lookup. On an unavailable drug, discard only the lookup receiver/drug stack values and branch
to this loop's existing increment. On an available drug, retain the original lookup and subsequent
CopyFrom with the original source. No extra source-list reads, default-value copying, replacement
policy, thread-static scope or cleanup resource is needed. Actual-body capture naturally observes
earlier effective argument changes and executes no owned code when a foreign prefix skips the
body. External postfix transformations do not by themselves invalidate this local guard: preserving
identity means our contribution leaves the native result flow intact, not policing every external
transformation. Admission protects the consumed source/drug/lookup/copy and branch data flow.

The native baseline is a main-thread capture of `NativeDrugCatalog.Capture(_ => true)` at an
established finalized-Defs boundary, not a global-requested-settings policy. It is published as
immutable reference identities with a caller-owned current-generation validity receipt. Worker
factories consume that publication and their actual method arguments synchronously; they do not
scan DefDatabase, read SettingsStore, query active mods, run metadata inspection or queue the guard
for later execution. The existing common active-package snapshot supplies original-provider
suppression. A missing/malformed baseline yields no owned omission and a bounded unavailable reason;
an available malformed drug must never be silently reclassified as removed.

**Initial native publication phase identified; integration remains unimplemented.** The source
proof below establishes an earlier existing callback than root's current idle incident setup. An
untouched, dedicated `[StaticConstructorOnStartup]` trigger queues the publisher through
`LongEventHandler.ExecuteWhenFinished` during the post-Defs `CallAll` drain. Its callback executes
on the main thread after the synchronous startup constructors return and before the next queued
native Game-creation worker. The trigger must have no earlier caller; keep root instance state and
the publisher on a separate ordinary type so referencing them cannot run this one-shot constructor.
Do not require `!LongEventHandler.AnyEventNowOrWaiting` here: Quicktest or save loading can already
be queued. Require the actual main thread, loaded native Defs, successful complete catalog capture
and admitted invalidation/patch routes before publishing an available baseline.

Root will own one instance of the existing
`ContentSelection/Harmony/NativeContentInvalidationPatch.cs`, admit it at this earlier boundary and
inject that same instance into the incident runtime and the drug-baseline publisher. Today the
incident runtime constructs it internally and root admits it at idle; those are central integration
changes, not a reason to create another same-owner prefix pair or a shared lifecycle framework.
The existing one-way atomic latch is sufficient to revoke this conservative initial-generation
baseline on HotReloadDefs/ClearAllPlayData entry. The baseline contains one immutable identity map;
the body reads its published reference and the same latch, never a worker catalog scan or a second
generation authority. Install/admit the shared invalidation route before enabling the preset guard.
Root also owns its separate safe metadata-reader migration/review; the older public-reader path is
not certified merely by reusing the instance. The preset guard introduces no duplicate invalidator.
Its failure withholds the drug guard without suppressing an unrelated feature merely because the
original Drug Policy Fix is active. Root's canonical active-package snapshot supplies detection for
the bounded warning and ordinary per-feature settings disable request, best effort; shared services
needed by unrelated features remain independent of that effective off state.

Expiry before any omission can leave the native call untouched. Expiry after earlier omissions
cannot be called a complete repair merely by switching remaining iterations to native behavior.
The inspected native paths below exclude Def mutation concurrent with a supported synchronous
factory; arbitrary external/reentrant mutation is outside that proof. No factory transaction,
rollback scheme or reload-success authority is introduced. A static constructor runs only once:
after native HotReload/Clear, the latch stays invalid and this baseline stays unavailable until a
separately implemented successful-generation reconciliation. This is an explicit incomplete
milestone, not a restart-only substitute for the remaining S038 reload/recovery requirement.

Proposed feature-local contracts are an immutable `PresetEntriesRead` containing baseline membership
and its existing-owner validity receipt, an actual-body `Capture()` that returns a read or no guard,
and `ShouldCopy(read, drugIdentity)` that preserves the native path unless a current admitted read
proves the identity unavailable. The native adapter captures classification once per finalized Def
generation; the body captures its published reference once per call. A narrow Harmony bridge can
consume opaque identity handles and these delegates in its host fixture, while runtime wiring uses
real ThingDefs. Exact types/registration are not implemented or leased here. Startup/repatch checks
reuse AdmissionCoordinator, OwnedHarmonyPatch and supported HarmonyPublication inspection; no new
generic patch framework or wider outer-postfix metadata export is required by this guard.

#### Native phase proof and its limits — 2026-11-08

The evidence is the installed 1.7.5000 rev1275 Assembly-CSharp SHA256
`D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254`. The following retained
decompilations were rechecked; source hashes identify research, not runtime admission predicates.

| Retained source | Relevant path | SHA256 |
| --- | --- | --- |
| [Verse.Root](../../../artifacts/Megamod/P0-B/native-1.7.5000/Verse.Root/Verse.Root.decompiled.cs) | Start/CheckGlobalInit queue early Core CallAll, then async LoadAllPlayData and interface initialization; Update drives LongEventsUpdate; OnGUI waits for the event | `7039CB7E159B4D9A2F2668413CA6894BFDFD37075439EC460F1CD1E446E1ABB8` |
| [Verse.PlayDataLoader](../../../artifacts/Megamod/P0-C/native-1.7.5000/Verse.PlayDataLoader/Verse.PlayDataLoader.decompiled.cs) | DoPlayLoad finalizes references/implied Defs/static data/short hashes before queued late CallAll (355–394); Loaded is set before the worker exits; HotReload mutation is a synchronous queued event (411–632) | `7382D74979D32963D9E1269CB852C8B506CF3EC828FBFA02F7FB5DC6F3442A51` |
| [Verse.LongEventHandler](../../../artifacts/Megamod/P0-B/native-1.7.5000/Verse.LongEventHandler/Verse.LongEventHandler.decompiled.cs) | ExecuteWhenFinished (298–305); worker exit then deferred drain (364–405); dynamically extending callback loop (469–502) | `4A42915023CC79AADC12B5EEB24D528C3412010BABC75EE6CC9F5FE9D7E75DCE` |
| [Verse.Root_Play](../../../artifacts/Megamod/P0-B/native-1.7.5000/Verse.Root_Play/Verse.Root_Play.decompiled.cs) | base.Start first; queued autostart/load/GenerateMap workers afterward (23–48); Quicktest creates Game at 89 | `AE763C2DA80FCA62B51D92D63F2818648B45CB86ADCCC4AF83A5FB9D6D607D81` |
| [Verse.GameDataSaveLoader](../../../artifacts/Megamod/P0-B/native-1.7.5000/Verse.GameDataSaveLoader/Verse.GameDataSaveLoader.decompiled.cs) | LoadGame queues PreLoadAct; that worker creates Game (153–160) | `77C7FBFF97EBB99128431E6BB920A605105B6C646AB2B8F7FC3663063A0FEA65` |
| [Verse.SavedGameLoaderNow](../../../artifacts/Megamod/P0-C/native-1.7.5000/Verse.SavedGameLoaderNow/Verse.SavedGameLoaderNow.decompiled.cs) | Later load worker creates Games (13, 26), before saved component data exists | `7A92E83EB90B57FDA5C0AFDE7EAE39EB2C3502A0014CDAEC3B440731C6FB39B6` |

Additional same-assembly read-only inspection used ILSpy `-t` for `Verse.GenTypes`,
`Verse.LoadedModManager`, `Verse.StaticConstructorOnStartupUtility`, `Verse.Root_Entry`,
`RimWorld.Page_SelectScenario`, `Verse.LanguageDatabase` and `Verse.DebugActionsMisc`; these
particular outputs were inspected on stdout rather than retained as separate files. GenTypes uses
Assembly-CSharp plus running mod assemblies; InitializeMods clears its type cache, and the later
load step loads mod assemblies/constructs Mods before Defs. Thus our unreferenced trigger is absent
from Root's initial Core-only CallAll and appears in the post-Defs pass. CallAll uses
RuntimeHelpers.RunClassConstructor and catches each constructor exception; constructor enumeration
order is not a promised provider ordering.

During the async Def loader, the current long event does not wait for a standard window display,
so its ExecuteWhenFinished registrations do not execute immediately on that worker. After worker
exit, Root's main-thread LongEventsUpdate drains them before clearing the current event/dequeuing
the next one. During the late CallAll callback, the drain's `executingToExecuteWhenFinished` flag
prevents nested immediate execution. Its live-count loop subsequently consumes the publisher
appended by our trigger. This proves this specific call site; ExecuteWhenFinished is not a general
main-thread dispatcher. Callback exceptions are caught separately and there is no aggregate success
result. Publish only after our complete capture/admission succeeds; do not use absence of all log
errors as a global gate.

Root_Play queues Quicktest/autostart/load after base.Start's Def-loading work. Root_Entry autostart
also queues its load after base.Start. Ordinary scenario creation reaches new Game from native UI
after the startup event drain. Game's drug database field initializer creates starting presets
before Game components and Current.Game assignment; the earlier callback covers these inspected
native first-generation paths without needing a save binding.

HotReloadDefs reads its initial body-part index snapshot, then queues the actual Def mutation as
a synchronous long event. LongEventsUpdate cannot dequeue it while an async Quicktest/load worker
is alive; normal debug UI is also withheld during that worker. A main-thread native factory and
synchronous reload cannot execute concurrently. Native language selection queues ClearAllPlayData
and LoadAllPlayData together in an async event; initial-load recovery clears on the loader worker
before factories. ClearAllPlayData itself is public and not internally queued, so this native-path
proof does not cover arbitrary external concurrent or reentrant calls.

Unsupported boundaries remain explicit: a third-party constructor/deferred callback can create a
policy before our publication, and another provider can enqueue a later relevant Def mutation.
Waiting until all synchronous startup constructors return is not proof that every provider's
arbitrary deferred work finished. Such factories retain native/provider behavior while the guard
is unavailable; optional filtering needs exact provider finalization evidence. A prematurely
referenced trigger, modified queue/completion semantics, wrong-thread callback or incomplete
catalog also withholds the route. No speculative queue-tail loop, foreign callback execution or
blanket foreign-owner ban is proposed.

#### Initial publication integration milestone

Root now owns the untouched InitialDefinitionsStartup type and its native post-CallAll callback.
MegamodMod admits one shared NativeContentInvalidationPatch independently of original-provider
selection, binds the exact native preset factory members, and publishes a complete immutable
NativeDrugPresetBaseline before enabling the preset guard. IncidentContentRuntime receives that
same guard; it no longer creates or admits a separate lifecycle owner. Worker callbacks read only
published membership and atomic receipts. Existing incident settings still initialize at their
later idle boundary; this does not lend them authority over the earlier preset baseline.

The earlier exact active Mlie.DrugPolicyFix suppression passed a focused Unit RED/GREEN; this is
historical evidence, not acceptance of the revised ordinary settings disable path. The thin baseline
adapter passed three Defs checks and independent review. Root's integration review corrected two
diagnostics: retain the malformed content reason/target, and report a failed recompilation once per
attempted generation through the existing protected sink. The reviewed build compiles without
warnings. At that milestone, initial native timing, policy controls and reload workflows remained
unrun; no task containing native acceptance is closed. Evidence is retained under
artifacts/Implementation/megamod-p0/initial-drug-publication/ and artifacts/Megamod/P0-C/native-preset-baseline/.

#### First implementation slice and ownership split

The next feature slice is only the preset-entry guard consuming an injected immutable baseline.
Proposed owned files are `Source/Features/DrugPolicyMaintenance/Core/PresetEntriesRead.cs`,
`Source/Features/DrugPolicyMaintenance/Harmony/DrugPresetEntriesPatch.cs` and matching
`tests/Megamod.Harmony/Features/DrugPolicyMaintenance/DrugPresetEntriesPatchTests.cs`. Add a small native
baseline adapter under the existing Runtime folder only when root's concrete publication caller
needs it; reuse NativeDrugCatalog rather than create a second classifier. Root owns source links,
central registration, the untouched startup trigger/publisher, shared-latch ownership/injection,
canonical active IDs and protected warning sink. Existing storage, batch and native ordering are
unchanged by this slice.

The bridge receives the target/member bindings plus an injected capture function returning
`PresetEntriesRead` or no guard. The read owns detached reference-identity membership only and is
bound to the same caller-owned invalidation receipt; it invents no availability lifecycle. Root
publishes one complete native baseline atomically at the proven callback, and capture refuses it
after shared invalidation or unavailable patch admission. The actual body captures once; its
per-entry decision receives that local and the already-evaluated native drug identity. Final exact
constructor signatures can follow the first fixture without broadening this responsibility.

The implemented constructor now receives exact factory/create/list/drug/lookup/copy bindings,
`Func<PresetEntriesRead?>` and the protected warning action. Candidate admission installs only the
owned transpiler; `Enable` requires a published accepted stream. `NeedsPublicationRefresh` and
explicit `RefreshPublication` separate main-thread metadata admission from worker factory use.
Capture checks the same body generation again after the injected reader returns. Null capture
bypasses the decision callback entirely so a denied inner Boolean writer cannot force omissions.
Current shape proof admits direct/immutable aliased arguments and labeled nops, proves creation
dominates the loop, rejects branch/switch entry strictly after the receiver load through its
creation store (including intervening nops), and rejects address escape of the policy local.
Entry at the receiver or preceding nops is eligible because it still executes the complete chain.
The sequence-entry check proves the store cannot receive an alternate incoming stack value;
store dominance alone was insufficient. Unsupported exception-handler
entry before the loop is rejected rather than guessed. ExpectedVersion failures preserve the
supplied stream, including during removal of oversized foreign metadata.

Inner rewriting of Capture's returned read or arguments is explicitly unsupported. Admission
rejects it without executing the callback, but a foreign-forged nonnull local cannot be claimed
to retain native fallback during that unsupported composition. The fixture checks rejection and
recovery after removal, not a fabricated safe runtime outcome. No broader invocation framework
or arbitrary foreign-patch removal was added; this limit must remain visible during review and
integration. The tested native fallback applies to ordinary absent/expired/throwing capture and
denied decision effects while capture itself remains intact.

First genuine RED: invoke a native-shaped factory with an unavailable entry followed by two valid
explicit entries under the real owned Harmony transpiler and an injected complete baseline. The
unmodified factory fails its unavailable lookup; the intended GREEN skips just that lookup/copy,
executes both following explicit copies in order, and returns the originally created policy with
its native label/source identity. This is an owned-patch fixture, not fake loaded Defs or acceptance
of startup ordering. Next individual behaviors cover absent/invalid baseline native fallback,
benign prefix/postfix changes and changed consumed data flow. Real native startup/Quicktest/load
timing and policy UI/save workflows remain root's isolated acceptance work. The first RED/GREEN
above establishes only the first scoped fixture; task closure and native results remain unclaimed.

C03 remains a separate dependency for actual save exclusions: constructor presets can run before
Current.Game and before saved overrides are read. Do not feed previous Applied, pending global
requests or the incident-only policy into this guard. Future candidate save decisions must compose
inside the existing ContentApplication publication boundary, not through a repair called before it.
The first guard neither admits that family nor applies the pending choices early.

Native initialization and null-storage repair remain separate. InitializeIfNeeded always executes
its terminal native SortBy after backfill, whereas the current pure planner calls its ordering
delegate only when it adds a row. A future terminal-sort intervention therefore needs an explicit
native-boundary ordering contract before reuse; prefix repair plus native sort is not acceptable.
ExposeData's PostLoadInit null-list exit also needs its own admitted route to initialization. None
of these changes is authorized or implied by the preset target document.

- Earlier-than-LoadedGame failures → include early preset/init semantics in one admitted maintenance
  group; leave recovery unclaimed when unavailable, without swallowing unrelated failures.
- New content can change drug permissions/properties → preserve player choices, normalize only
  inapplicable flags, distinguish new rows from default-valued existing rows and report actual edits.
- Display sorting conflicts with foreign row controls → preserve identity and effective native
  callbacks or disable presentation; never reorder gameplay storage to simplify drawing.
- Duplicate rows contain conflicting choices → first effective row wins, bounded diagnostic; no
  permissive merge, hidden history or promise of full malformed-provider recovery.
- Source license statements differ → independent implementation and attribution; no upstream reuse
  without a separate check. Source claims of safe removal/multiplayer are not our acceptance evidence.

## Migration Plan

There is no source-specific settings import or external file migration. Source behavior already
uses ordinary native policy data. Keep those policy identities and valid values; apply owned
maintenance in memory and persist only through normal saving. Retain pre-repair list/value snapshots
for rollback of an owned failed commit. The previous on-disk save is unchanged until the player
saves; after a saved removal, re-enabled drugs get new defaults rather than inaccessible old choices.
Do not claim that removing this feature or arbitrary content repairs all dependent saves. Any future
automatic import/rewrite must separately specify backup, atomicity, restoration and postconditions.

## Open Questions

DP-Q1–4 in the [refinement report](../../../mods/megamod/docs/refinement/2026-11-02-drug-policy-fix-research.md)
retain optional preference review, exact beta/effective target proof, finalized provider timing and
runtime verification. They do not require asking the user to explain existing source behavior.
