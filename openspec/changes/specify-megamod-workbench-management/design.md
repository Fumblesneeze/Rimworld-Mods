## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S017 replaces BWM's adopted observable behavior after documentation-first source/native inspection.
This is a design for later implementation, not runtime evidence. Exact inputs, hashes, source/DLL
revision mismatch, completed reads and actual consumers are in the
[research journal](../../../mods/megamod/docs/refinement/2026-11-02-better-workbench-management-research.md).

BWM 1.6.1.3 combines global preferences, a WorldComponent, per-bill serialized additions, reference
clipboard, individual reference link sets, broad UI patches and native-count postfix additions.
Native 1.7 already supplies naming and player-faction direct-carry counting. S014 supplies one owned
whole-list plan with physical work remaining local. Recreating BWM's classes/patch graph would duplicate
state and conflict with that contract. S018's person-count modes remain its separate sequential analysis.

## Goals / Non-Goals

**Goals:** contextual convenience, explicit atomic copy/paste, native worker eligibility, coherent
unique-unit counting, observable recovery, common settings and concrete external contract accounting.
Compose S005 storage, S009–S012 browser and S014 workbench plans without internal compatibility patches.

**Non-goals:** source package impersonation, blanket public-type facades, automatic source-save import,
unlimited bills by default, arbitrary third-party counter emulation, new content, altered recipe
economy, globally intercepting every image button, or a private scanning/cache subsystem per source.

## Decisions

### Keep one production plan and a small owned extension

Native unlinked production bills keep native fields and identity. Owned additional-product/away state
attaches through an explicit save extension; worker policy belongs to the bench or shared S014 plan.
S014 owns group/row identities, transactions, quota and physical commitment tracking. S017 commands
modify that plan directly. S018 and later selected modifiers contribute their state to the same
copy/apply boundary after their own specification. Do not patch our own MirrorBills equivalent.

Snapshot DTOs contain editable choices plus stable Def/reference identifiers, never cloned active
jobs, unfinished things, machine contents or provider runtime objects. Resolve and validate the entire
operation before mutation; retain a rollback snapshot of the destination plan and verify postconditions.
Publish one logical list change only after all projections are valid. A copy of finite remainder is
an explicitly additional order, while link/separation use S014 conservation rules. Paste-choices uses
S014 active-work restrictions and rejects unsafe transitions. Copy-time snapshot is a documented
deviation from source live references; it avoids later unrelated edits altering the paste silently.

Maintain an explicit supported field inventory for each admitted bill family: native name/style/
graphic variant, ingredient filter including special restrictions, radius, repeat/count/pause,
worker/skill, output/count scope, HP/quality/taint/allowed Stuff and owned additions. Cross-recipe
transfer uses semantic compatibility and previews retained/overridden fields, not reflection over
every writable member. If any needed provider field cannot be represented safely, disable that
operation for that bill instead of losing it. Preserve snapshot choices when destination references
need resolution. Current baseline capacity is 15; one admitted capacity policy must govern browser,
native add, list paste, linking and editing before an optional limit extension is called supported.

### Apply worker policy at owned mutations, not window selection

Use bill.billStack.billGiver or the owned logical row's owner. Enabling is an explicit atomic edit,
not a deferred Window.PreClose side effect. On accepted worker edit or add/paste, apply policy through
native restriction setters, retaining the skill range and ordinary job eligibility. Changing UI
selection is irrelevant. Group policy joins/leaves with S014; source policy wins only through its
existing reviewed merge preview. A missing pawn never calls SetAnyPawnRestriction automatically.
Destruction cleanup must distinguish actual loss from temporary minification/reinstallation.

Alternative rejected: copied per-bench restrictions layered over per-row synchronization. That creates
two authorities and violates the user's whole-list requirement. Unsupported autonomous/mech/custom
families need a validated interpretation or disable the policy operation for the whole affected plan;
the policy cannot invent a human worker requirement for a machine.

### Count a policy-consistent union instead of adding opaque totals

For admitted ordinary single-output counters, construct the selected product Def set once, unioning
primary with extra selections. Enumerate qualifying physical candidates under the capability's scope
table, deduplicate stable physical identity/location and sum actual represented units. Do not add a
second inventory pass to a native integer whose overlap is unknown. Native CountValidThing is useful
for HP/quality/taint/Stuff/fog semantics, but inspect its current shape and add the specified consistent
resource freshness/scope rules. Native List-based counting uses one per Thing in this beta; our unit
contract intentionally replaces that discrepancy. Minified outer/inner multiplicities must be proven
from actual supported representations, not multiplied twice through nested ownership paths.

| Selection | Base physical candidates | Explicit additions |
| --- | --- | --- |
| Map scope | Native eligible spawned/installed products, stockpiles/shelves and admitted haul sources; fresh resource loose stock only when enabled | Native player-faction direct carry; enabled local inventory/equipped sets; associated away stock |
| Selected storage/group | Its actual HeldThings across valid members, deduplicated | Include equipped's local inventory/equipment/apparel sets; associated away stock |
| Local inventory option | Free colonists, plus colony animals/mechs/player-controlled subhumans if nonhuman option is on | Map scope only; never extra direct carry already included |
| Include equipped | Free-colonist inventories, equipment and apparel, plus enabled nonhuman equivalents | Either base scope, matching native selected-scope inclusion; local direct carry stays governed by base scope |
| Count when away | Living player-pawn inventory/carry with recorded home, including temporarily unavailable holders | Either base scope; equipment/apparel only if Include equipped; independent of local inventory/nonhuman preferences |

Uniform loose-resource exclusion when disabled and freshness across all locations are deliberate
refinements of inconsistent source/native branches. No extra Count installed option is inferred from
an orphan translation key. Unsupported special/custom recipe counters remain native and cannot
receive our extras until their own contract exists. Later EGO modes require direct, explicit
composition of target semantics and eligible controls, not probing our UI for an old method name.

### Share candidates and track freshness, not stale eligibility

Use native map listers, storage ownership and pawn inventories first. If derived indexes are needed,
extend the single map-owned Thing data used by browser/search/hauling, with keys for Def, physical ID,
holder/location, stack multiplicity and relevant metadata. Store only owned home associations by
pawn/map identity in save state. A narrow shared game-level association view can cover caravan/
transporter/off-map holders; it must not duplicate a second full map inventory cache.

Invalidate at spawn/despawn, transfer/holder change, stack split/merge/count, destruction/consumption,
minification, HP/quality/taint/rot/Stuff, fog, faction/death, home arrival/departure, storage membership,
filter and settings changes. Some events matter while paused. Resolve pending updates before the
next display/ShouldDoNow decision, clear caches on teardown/save switch and rebuild on load. Never
persist live candidate references or access Verse/Unity state off-thread. Fallback to bounded fresh
native enumeration when valid; if neither path can guarantee the contract, show unavailable and stop
affected new work. Benchmark combined counting, browser, later search/hauling, update cost and memory
before claiming optimization. Pawn-specific job eligibility remains evaluated through native paths.

### Own away association without broad Def injection

The source unconditionally adds CompPawnOriginalMap to BasePawn. Prefer a narrow owned WorldComponent
association because only this count feature needs the saved relation, and the data is not behavior
every pawn must gain. Inspect exact pawn/holder transitions, including cryptosleep and transporters,
before selecting hooks. Capture source home before despawn, clear on available spawn at any player
home, preserve non-home visits, filter dead/nonplayer pawns, retain unresolved map identities for
diagnosis without assigning them to another home. A stale origin must not overlap current local stock.

No XML mutation is currently proposed. If actual implementation needs a Def comp, first update this
design/dependencies and use required XML Extensions to verify BasePawn/comps, add only the missing
owned comp and retain every foreign comp. Optional content needs packageId=true FindMod and Def
checks. Never replace a comps/filter/category list merely to insert one entry.

### Use native contextual surfaces and common settings

Extend the existing Bills list/details and Storage tab selection behavior, using the integrated browser
for Add. Reserve proper space/height for contextual controls and localize tooltips, scope and failures.
Retain native rename and reorder fallback. Drag hit regions cannot include action buttons; GUI/group/
font/color state restores on every path. Navigation uses actual bill ownership and stable logical IDs.
No bottom tab, debug control or global operational toggle is needed for these contextual actions.

Six preferences use the shared definition/resolve/load/save/render path: auto-open true, drag true,
default-floor false, loose resources true, inventories false, nonhuman inventories/equipment true.
Group Production with distinct convenience/counting subsections; stable IDs, order, built-ins,
effective timing and scope must be registered centrally. Main-menu callbacks enforce disabled save
controls; inherited rows retain dormant overrides. Source mirrorSuspendedStatus is omitted because
complete shared synchronization is mandatory. There is no global-only exception and no source settings import.

### Target admission is per minimal semantic dependency group

Each eventual patch needs a registered target spec, executable validator, positive/negative fixtures,
ownership, failure warning and dependent-operation gate. The following are candidate seams from
inspected 1.7, **not permission to PatchAll or claim completed validators**. Final exact signatures and
instruction/dataflow slices belong beside implementation and must be added before any patch applies.

| Group / candidate target | Minimal facts that must hold | Failure scope |
| --- | --- | --- |
| BM-T1 list/details controls: BillStack.DoListing, ITab_Bills.FillTab/TabUpdate, Dialog_BillConfig.DoWindowContents | Actual owning BillStack/Bill accessible; native row/control lifecycle and event consumption; if replacing clipboard/add controls, exact clone/add/limit and click branch semantics | Disable only unsafe clipboard/navigation/list extension, preserve unaffected controls; never double-execute native paste |
| BM-T2 overview presentation: Bill.DoInterface, Bill_Production.DoConfigInterface/RepeatInfoText | Row/action hit regions and reorder boundary; relevant target-count branch returns rendered current/target string; changed parameters demonstrably feed intended layout | Independent display/drag/delivery fallback; no global Widgets.ButtonImage suppression |
| BM-T3 selection: Selector.Select and available inspect-tab methods | Selection completes before query, single-selection/count and tab ownership contracts exist; additive after-hook may need only method/result/state shape | Auto-open off, ordinary selection intact |
| BM-T4 creation/edit/policy: BillUtility.MakeNewBill, BillStack.AddBill, supported setters/owned commands | New production bill versus clone/ongoing work; concrete owner known when policy applies; restrictions still consumed by native work eligibility; coherent rollback path | Disable affected mutation/policy; saved affected policy pauses before unrestricted work can start |
| BM-T5 count/decision: RecipeWorkerCounter.CanCountProducts/CountProducts/CountValidThing, Bill_Production.ShouldDoNow and display | Ordinary one-product contract; count result feeds target/pause decision; identify original + foreign consumer paths so no duplicate work/count; custom override classification | Disable extension on unsupported family; saved extended target unavailable/paused, ordinary native counter untouched |
| BM-T6 home/holder lifecycle: Pawn spawn/despawn, actual holder transitions and player-home lifecycle | Previous map available at capture, target home known at clear, life/faction/container transitions tracked before next query; selected off-map enumerator includes required holders | Away counting disabled coherently, dependent saved targets suspended; never retain a plausible stale association |
| BM-T7 save/clone/delete/destroy/reload | Native bill/member identity, loading phases, clone initialization strips physical commitments, minify differs from destruction; load recovery precedes job execution | Preserve saved state with explicit recovery; reject unsafe active operations or loading before unsupported behavior can run |
| BM-T8 shared-data invalidation | Each consumed event's parameter has the specified identity/count/location meaning and precedes dependent use; native enumeration fallback remains valid | Fresh native path or unavailable; never silently use stale index |
| BM-T9 optional providers/limits | Canonical active package plus exact member/Def/dataflow/ownership; same effective capacity across all mutation surfaces; explicit ordering when foreign edits consume same path | Only provider adapter withheld, one warning, retain native support |

Existence alone is sufficient only for genuinely additive notifications that rely on no internal
meaning beyond their documented result/state. A prefix changing inputs or replacing count/control
flow must validate its specific consumed snippet/relationship. Permit harmless patches and unrelated
IL edits. Do not require zero other owners, full-body equality or an exact version/hash. Revalidate
transpiler matches at application; no partial rewrite. Atomic groups install all required seams or none.
Warnings name feature/target, failed expectation and fallback, without recurring per-frame noise.

Source `falconne.BWM` active: withhold overlapping S017 support with explicit ownership explanation.
Detect saved owned state before any jobs can reinterpret it; a robust safety gate must preserve/pause
affected rows even if a deeper feature hook fails. If that safety gate cannot be established, reject
loading the affected plan explicitly rather than treating startup logging as protection. Restored
support requires consistency checks and explicit Resume, with no quota or physical state reset.

### Preserve actual external semantics where feasible

No Max Bills Redux genuinely reflects Main.GetMaxBills and patches its 125 sentinel. A fake type with
that literal would institutionalize an obsolete implementation contract. Its missing-target branch
is safe; instead research an explicit canonical-package capacity adapter when beta support is feasible.
Until admitted, owned mutations remain at fifteen and the unavailable extension is explained. Never
truncate existing longer lists; allow existing-row editing/production, with further adds blocked until
a coherent capacity policy exists. Update S014/browser limits together if supported.

EGO's older MirrorBills assembly binding is a real source-era contract. Its installed 1.7 main
assembly targets the existing public static BillConfig_DoWindowContents_Patch.DrawFilters
(Dialog_BillConfig, Rect), broadening target-mode UI to TD_PersonCount and TD_XPerPerson but excluding
TD_WithSurplusIng. These concrete contracts do not justify broad source facades. S018 must compose
both the person-counter copy behavior and applicable count controls directly in our plan. The
separate old copy-bridge folder is absent from the installed 1.7 inventory; neither that fact nor
the valid UI target proves actual beta execution. Optional external
EGO/source-save interoperability requires its own validated contract, not inferred success. Outfitter,
PRF, Colony Groups and Deep Storage must get canonical IDs and exact supported shapes before adapters
are enabled; source display-name/type-only probes are insufficient. Record unsupported signatures and
save identities in replacement docs so the drop-in boundary is reviewable.

## Risks / Trade-offs

- Snapshot/atomic paste and normalized counting differ from source edge behavior → explicit proposal,
  transfer previews, scoped scenarios, recorded BM-Q1–2; no silent claim of exact parity.
- Counting more goods can suppress production, especially away stock → explicit controls and scope
  help; real physical units and no free resources; measure actual job pause/resume.
- Broader contextual policy can make a recipe impossible → keep ordinary eligibility, explain scope,
  retain missing worker and require deliberate recovery instead of choosing a substitute.
- Native beta and foreign counter changes → semantic admission, coherent unavailable state, exact
  reviewed-build verification; no native-regression claim based solely on decompiled code.
- Shared data can get stale or expensive → concrete invalidation/fallback contracts and combined
  measurements, not copied source loops/static lists or a speculative general framework.
- Source code GPL-3.0 provenance → independent behavior specification; assess licensing before any
  actual source/art reuse, never copy Workshop assemblies into the product package.

## Migration Plan

Documentation only now. Implement vertical slices with focused RED/GREEN and retained evidence.
Own new keys/identities, default absent bill additions to none/false and absent policy to disabled.
Keep source saves/configs read-only; an import requires a separate explicit contract and disposable
fixtures. Product package carries neither original DLLs nor Gateway. Verify metadata and references.
Rollback keeps owned data recoverable and suspends policies that cannot be honored; never silently
discard restrictions or count choices. No normal-save modification is part of research or tests.

## Open Questions

BM-Q1: exact clone/active-work field handling and proposed UX. BM-Q2: holder/freshness/unique units and
combined performance. BM-Q3: canonical providers, capacity adapter and S018 direct composition.
BM-Q4: executable targets, language/layout work, focused tests, code review and native acceptance.
These are tracked in refinement; no user interview is required to proceed with the next source.
