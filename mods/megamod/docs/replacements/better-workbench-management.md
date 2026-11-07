# Better Workbench Management replacement coverage

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Queue **S017**, intake **P1-14**, origin **replacement**. **Analyzed; specification drafted.
Nothing implemented or accepted in game.**

Source: **Better Workbench Management**, **Falconne**, `falconne.BWM`, Workshop **935982361**.
[Author description](https://steamcommunity.com/sharedfiles/filedetails/?id=935982361),
[change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/935982361),
[public source](https://github.com/Falconne/ImprovedWorkbenches/).
Installed latest version **1.6.1.3** supports through **1.6**, not declared 1.7. Installed decompilation
is authoritative where its informational commit differs from the available public checkout. GPL-3.0
source provenance is recorded; no assemblies, source code or art are copied into the product by this work.

[Research, exact hashes and consumer inspection](../refinement/2026-11-02-better-workbench-management-research.md)
support [proposal/design/tasks](../../../../openspec/changes/specify-megamod-workbench-management/) and
three observable contracts:
[bill management](../../../../openspec/changes/specify-megamod-workbench-management/specs/megamod-workbench-bill-management/spec.md),
[worker policy](../../../../openspec/changes/specify-megamod-workbench-management/specs/megamod-workbench-worker-policy/spec.md),
[stock counting](../../../../openspec/changes/specify-megamod-workbench-management/specs/megamod-production-stock-counting/spec.md).

## Behavior coverage and deliberate deviations

| Behavior | Source / native finding | Proposed replacement and status |
| --- | --- | --- |
| Copy one/all | Live source references; empty copy leaves old clipboard | **Specified:** copy-time snapshot, exact ordered rows, no stale success or physical work; deliberate deviation |
| Paste new bills | Source makes new recipe bills and mirrors fields; capacity checked per row | **Specified:** complete preflight/preview and atomic append; preserve applicable name/style/choices; fifteen baseline rows |
| Paste into existing bill | Conditional field copy and fixed allowed-Def equivalence | **Specified:** explicit transferred/retained/overridden summary, semantic filter equivalence, preserved destination recipe/work and active-count safeguards |
| Paste link / break one bill link | Source individual bill sets mirrored while drawing/hovering | **Replaced by S014:** user-confirmed whole-list groups and shared production accounting; no individual internal link graph |
| Suspension mirror option / restricted overview mode change | Source can leave suspension local and blocks overview repeat changes on links | **Omitted:** whole-list edits synchronize fully; ordinary overview edits use shared invariants |
| Drag order / copy-paste controls | Source broad image-button suppression and selection-based layout | **Specified:** scoped row hit regions and one deliberate action, native fallback and preserved unrelated buttons |
| Delivery shortcut / default | Toggle floor vs best, default-floor off | **Specified:** visible specific destination replacement; explicit copied choices override new-bill default |
| Resume-level display / navigation | Adds threshold text and adjacent details; navigation uses current selection | **Specified:** readable current/target/resume and actual-owner navigation with no wrapping |
| Renaming | Native 1.7 already provides it; source keeps legacy label override | **Native retained:** no second rename field/UI or unpromised legacy-name import |
| Restrict workbench | Enabled flag; capture/propagate on dialog close using selected bench | **Specified:** explicit owner-correct atomic policy, existing/new bills, group-wide composition, preserve assignments on disable |
| Additional products / CountAway | Single ordinary output target bills only, bill-specific saved fields | **Specified:** union of qualifying product units, explicit away policy, contextual choice persistence |
| Loose resources / inventories / nonhumans | Source conditional additions can overlap native counts and differ across filter paths | **Specified corrections:** unique physical units, consistent resource freshness/loose policy, exact scope/carrier rules |
| Nonhuman direct carrying | Native 1.7 already includes player-faction direct carriers | **Native retained:** no legacy FreeColonists rewrite; nonhuman preference governs only added inventories/equipment |
| Home association | Pawn comp records home despawn, clears home spawn | **Specified:** same intended origin behavior with explicit unavailable-container/transfer/death boundaries; narrow owned saved association, no unconditional comp injection |
| Installed count / older extra filter/numeric controls | Orphan language keys do not establish current features | **Not inferred:** native supported installed-product counting retained, no invented separate controls |
| Source settings and link import | Different global/WorldComponent/type keys and migration logic | **Not promised:** no automatic import, package spoof or foreign save rewrite |

The user's confirmed whole-list and infrastructure rules are distinct from proposed corrections.
Snapshot semantics, atomic preview/paste, uniform units/freshness/scope and explicit recovery are
documented refinements, not claims of exact source parity or user-approved implementation details.

## UI and configuration

Use existing Bills list/details, the integrated Add bill browser and appropriate inspect tabs.
Bill filters, Count when away, worker assignment and group policy are **entity gameplay state**.
No new bottom tab, debug-bar feature or global operational button is introduced.

| Common preference | Built-in | Applies |
| --- | --- | --- |
| Auto-open bills and storage tabs | On | Next single supported selection; preserve an already compatible tab |
| Drag to reorder | On | Next list interaction; native controls when off |
| New bills drop on floor | Off | Newly created ordinary production bills, not older bills or explicit pasted delivery |
| Count loose resources | On | Next count evaluation; uniform resource scope rule |
| Count inventories | Off | Local map-scope inventory addition |
| Count nonhuman inventory and equipment | On | Enabled extra local inventory/equipped sets; native direct carry stays intact |

All use common definitions/loading/resolution/storage/rendering with Global default / Current save /
Use global, inherited inactive editors, dormant overrides, main-menu save-control/callback guards
and save isolation. No inheritance exception or source-settings import. Seven-language text is future
implementation work. Restrict workbench begins disabled; extra types empty and Count when away off.

## External contracts and interoperability

| Consumer/provider | Observed contract | Preservation / limit |
| --- | --- | --- |
| No Max Bills: Redux, justharry.nomaxbillsredux, Workshop 3526216885 | Reflects ImprovedWorkbenches.Main.GetMaxBills and patches 125; exposes NoMaxBills marker type | Real binding recorded. Do not fabricate that class/sentinel. Future canonical adapter must share one limit across all owned/native paths; no beta support claim. Existing longer lists must not be truncated. |
| Everybody Gets One older bridge | Direct assembly binding to ExtendedBillDataStorage.MirrorBills; copies person-counter state | [S018 draft](everybody-gets-one.md) composes complete query snapshots with S014/S017 directly; no own-mod patch stack or source identity spoof. |
| Everybody Gets One installed 1.7 | Targets the existing public static BillConfig_DoWindowContents_Patch.DrawFilters(Dialog_BillConfig, Rect); extends target-mode UI to person-count modes, excluding surplus. Conditional old copy-bridge folder absent from 1.7 inventory. | S018 explicitly extends ordinary product controls and selects Include equipped carriers through its query, retaining other S017 paths and dedup. Actual beta interoperability and separate MirrorBills copy support are not proven by the source binding. |
| Most Skilled Bill Restriction | Reflects public instance BillCopyPaste.DoCopy(Bill_Production) and DoPasteInto(Bill_Production), storing three static flags | [S019 draft](most-skilled-bill-restriction.md) composes complete worker choices with all snapshot/list/policy routes directly; source hooks do not prove Copy all/new-bill/link propagation. |
| Outfitter / Project RimFactory / Colony Groups | BWM patches external listing, probes special bill givers or copies a bill-to-group dictionary | Optional source integrations, not required native dependencies. Canonical IDs/shapes/current workflows pending BM-Q3; no support inferred from type names. |
| Deep Storage | Source skips all storage-building auto-open by display-name presence | Do not copy blanket exclusion; preserve unaffected native shelves, isolate any proven provider adapter. |
| Source saves/settings | ExtendedBillData and linkedBillsSets, legacy ID migration, worktableRestrictionDictonary, OriginMap and global preferences | Owned equivalent choices persist; binary/schema migration is deliberately unpromised pending separate explicit work. |
| Manager rules and saved mod lists | Catalog/order references only | No callable facade or package spoof needed. |

The bounded installed literal scan cannot prove there are no dynamic/uninstalled consumers. Preserve
actual safe semantics when feasible and document unsupported bindings rather than equating a familiar
feature name with drop-in API compatibility. Required future runtime dependency is **Harmony**.
No XML operation is selected; if one becomes necessary, required XML Extensions and semantic
merge-preserving guards must be added before implementation. The source-active overlap is withheld
with a bounded startup warning; our saved policies remain explicit and safely recoverable.

## Implementation and verification

**Nothing is implemented.** All capability scenarios require future focused tests and actual native
workflows. Design BM-T1–9 identifies patch dependency groups, not completed executable validators.
Candidate/accepted target shapes must permit benign foreign edits; exact hashes are evidence identity,
never blanket admission rules. Unknown saved counts cannot become trustworthy zero, and lost worker
references cannot become Any worker. Restored support requires explicit Resume of affected work.

BM-Q1 retains clone/UX/active-work details; BM-Q2 holder/away/shared-data proof and measurements;
BM-Q3 exact provider contracts and S018 composition; BM-Q4 guards, tests, languages, review and native
acceptance. Document validation/review is tracked in the
[sequential ledger](../refinement/specification-progress.md). No game is launched for these prose edits.

Future acceptance must connect actual UI actions, pawn production/hauling/consumption and caravan/
container transitions to observed counts, restrictions and results. Test reviewed-build native and
exact provider-present workflows, main-menu/save switching, malformed targets and rollback; retain
causal screenshots/build/mod-order/config hashes/cleanup evidence. Logs and prepared end states alone
are insufficient. Counting/worker changes can alter production availability but never grant resources,
skills, research or free work; source cost/yield/quality probabilities remain unchanged.
