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
Both owned groups are suppressed when exact original `Mlie.DrugPolicyFix` is active, because the
source mutates the same rows/order. Do not unpatch its owner or claim native-priority correction
while it still alphabetizes the stored list. Original detection is package-based, case-normalized
through the repository policy, not assembly-name guessing.

### 4. Direct composition and absence-safe external integration

S038 will finalize content and invoke reconciliation, not patch an owned Cherry Picker replica.
External `Owlchemist.CherryPicker` is optional and currently only inspected as a 1.6 input. Its
existing drug-category clearing is sufficient as a fact once finalized, but is not proof that its
initialization timing supports beta. Do not bind to private reprocess lists or copy its mutation.
If a future declarative fix requires XML, specify it separately with XML Extensions guards,
additive preservation and dependency metadata; this draft requires no XML patch.

S024's potential-mood gate and native Pawn_DrugPolicyTracker remain authoritative for permission
and timing. Reconciliation must not clear valid drugTakeRecords or force a job. S021/S024 use the
same normal carried-supply targets; no extra inventory, hauling permission or capacity is granted.
The catalog is game-content data, not Ctrl+F/hauling stock data. Reuse finalized catalog facts and
native listers, invalidate on admitted content/language changes, and discard per-game projections
on teardown. No per-tick scan or speculative shared cache framework.

Bounded installed consumer scans found no callable incoming source binding. Do not create public
DrugPolicyFix/DrugPolicySort/NonPublicFields facades solely because they exist upstream. Native
policy types, fields and lifecycle are the actual preserved contract. A later proven consumer needs
its own narrow adapter/evidence and cannot be satisfied by spoofing the source package.

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
disabled feature is not a failed candidate. Record original-provider suppression distinctly.

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
using the resulting policy connects the load repair to player behavior. Verify admitted filtering
present/absent and original-provider suppression separately; do not infer 1.7 support from metadata.

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
callback. Field-shape admission, original-provider suppression, early init/preset/load ordering,
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
