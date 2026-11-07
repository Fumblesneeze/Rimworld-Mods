# Drug Response (Continued)

## Identity and provenance

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, target1.7 beta.
Origin: replacement, S124. Source `Mlie.DrugResponse`, Workshop
[2196401201](https://steamcommunity.com/sharedfiles/filedetails/?id=2196401201), pelador/Mlie,
installed modVersion1.6.1 declaring support1.1–1.6. Inspected2026-11-06; no1.7 source compatibility
claim. Pinned source `bc24a72ff131946b687521a009c9cee89463b39b`; selected56832byte DLL SHA256
`5577DB01936D5FF976059C63C9BFD1E9BF903C87CDD2DA0EC8948D6969DE10DE` equals the pinned repository DLL.
MIT continuation LICENSE and CC BY-NC-SA4.0 source credits differ; no source code/art/DLL reuse here.
[Research](../refinement/2026-11-06-drug-response-research.md) contains complete source paths,
catalogs, settings, PDF inspection, native extracts and narrow S134 comparison.

## Behavior inventory

All rows below are **drafted; nothing implemented**. References point to the owned
[observable contract](../../../../openspec/changes/specify-megamod-drug-response/specs/megamod-drug-response/spec.md).

| Behavior | Input → result | Default/edge | Evidence / contract |
| --- | --- | --- | --- |
| Configuration | Configure global/save responses → visible effective values | Off/None initially, common inheritance | DRSettings/UI/manual; DR01–02 |
| Catalog | Select pain/condition and drug → available named choices | Purposeful pain effect; researched chooser only; missing choices retained | MSPainDrug/MSDRUtility/XML; DR03 |
| Triggers | Pain band or visible condition appears → applicable response | Exact1/15/40/80% boundaries; two condition slots; immunity/anesthetic guards | MSPainUtility/MSPainlessData; DR04 |
| Permissions | Eligible patient and actual rule → optional response | No animals; care/forced orders preserved; schedule/addiction distinctions | MSDrugUtility/native policies; DR05–06 |
| Actions | Self-use or doctor administration → actual ordinary consumption | One full-dose equivalent; operation/surgeon supplies; prisoners always bills on map; other self-use condition incapacity waits | MSPainResponse/MSAddDrugBill/native; DR07 |
| History | Actual dose → interval, effects and next eligibility | No exposure means no dose; actual S085 partial exposure counts; no first-day delay | Source discrepancy/native ingestion; DR08 |
| Shared orders | S124/S134 requests → one pending drug/chemical dose | Suspended/manual bills block duplicates; native15 limit | Inspected S134 DrugAdministration; DR09 |
| Player control | Cancel/edit/recover → hold, control transfer or cleanup | Preserve unrelated/manual work and all real stock | Proposed correction; DR10 |
| Caravan | Eligible carried supply → real caravan dose | Both slots; night-rest wait; no invented bed/remote stock | MSCaravanUtility/native callers; DR11 |
| Context UI | Health status/filter click → explain/control, no implicit surgery | Existing contextual UI; no new bottom tab | User UI/S137 intent; DR12 |
| Lifecycle/compatibility | Reload/disable/drift → reconcile or explained fallback | No automatic import or duplicate source execution | DR13–14 |

## Interactions and composed result

S032 repairs current native policies without inventing permission. S062 supplies per-condition care
and the operation's surgeon-inventory/medicine rules; its tending-only patient/other-donor extension
does not authorize administration ingredients. S021/S033 provide shared quantity claims/candidates.
S085 owns fully admitted partial consumption and quantity/exposure history; no-exposure failure
and interrupted positive consumption are distinct. S086 governs actual Medical go-juice categories
with each initiating action's permissions. No separate
drug map cache or inventory-capacity exemption. S134 retains urgency-first rescue/tending, actual surgeon capability,
dependency eligibility and quill scheduling in its own future change; its owned medication producer
shares DR09's pending-dose decision. Source S134's `allowedForAddiction` permission is material, not
a speculative conflict. Ordinary scheduled use suppresses S124 supplementation but cannot permanently
block separately permitted S134 assistance to an incapacitated dependent patient.

S007/S062 Health controls remain contextual. S137 condition-to-recipe filtering is separately queued;
clicking infection/bionic/quills neither configures nor orders an S124 dose or surgery. No internal
Harmony patch stack against owned replacements.

## UI audit and relocation

| Source surface | Purpose / classification | Owned destination / deviation |
| --- | --- | --- |
| DrugResponse minimized main tab, order100 | Configuration; not a mechanic-management tab | Medical > Drug responses in common mod settings |
| Yes/no float menus; pain selectors/slider | General and pain configuration | Direct common rows; four visible bands and intervals |
| Two single-condition selectors with Set/Clear | Condition-rule configuration | Searchable saved-rule list; both responses visible together; immediate valid edits |
| Drug image/info | Information | Preserve ordinary drug info access, no copied source art |
| New patient holds/status | Patient gameplay state | Health context; never global/save settings |

All settings use the common path and named scope, including a per-condition Use-global row for each complete two-slot response bundle.
Save controls disabled while inheriting or at main menu; globals available. No inheritance exception.
No new debug or bottom-bar toggle is introduced: this is configuration plus contextual patient care.
Seven-language/small-window/native Health and operations interaction acceptance remains pending.

## Contracts consumed by mods or saves

| Consumer / evidence | Contract | Decision and remaining verification |
| --- | --- | --- |
| Author manual and source12 XML patches | MSPainless.MSPainDrug.ManagesPain XML extension | Inventory retained; inspect actual external consumers before a facade; never unsafe duplicate type or package spoof |
| Source WorldComponent and pawn comp | DRSettings static field save keys, semicolon rules, comma response history | No automatic import; owned typed persistence and independent defaults |
| Source multiplayer adapter | Private UI sync methods, DRSettings fields and watcher Harmony owner | Not preserved as a claimed multiplayer API; a future MP adapter needs separate verification |
| Installed replacement catalog3396308787 | pelador.drugresponse → Mlie.DrugResponse package mapping | Catalog metadata only; megamod retains its own package ID |
| Native operations | Actual Bill_Medical recipe/part and ordinary execution | Preserve existing bills and player authority; no adoption/deletion of unowned entries |
| S134 Dreadarm API/provider | Actual medication eligibility/pending chemical; separate read-only API exists | Narrow overlap inspected; full API consumers and S134 facade decision remain S134 work |

Bounded installed XML/C#/JSON search found no external text caller of the pain marker outside source;
that does not rule out compiled/reflected consumers. No drop-in binary or removal-safe save claim.

## Implementation and coverage

| Scope | Implemented | Intentional deviations / outstanding work |
| --- | --- | --- |
| DR01–14 | Nothing | Entire runtime/TDD/patch targets/native verification pending |
| Source UI | Nothing | No bottom settings tab, hidden Set, second-slot bug or static save leakage |
| Timing/actions | Nothing | Successful/partial-dose intervals, protected orders, prisoner bills, operation-source limits, automatic-origin holds after edits, no extra refill, truthful notifications |
| Optional source catalogs | Nothing |12 patch inventories and illness candidates retained; package/effective Def admission and native provider matrix still required |
| Upstream code/assets/migration/MP | Nothing | Deliberately no copying, automatic migration or unsupported guarantee |

Required RimWorld; Harmony when used; XML Extensions required for actual XML integration. DLC,
drug/illness/UI providers optional. Co-installed original wins over owned S124. Unsupported external
automatic medication overlap disables the owned overlapping scope with warning until cooperation
is proved. No source settings/patch edits. Merge-preserving content checks and minimal semantic
admission are mandatory, not exact-hash or no-foreign-owner tests.

## Verification and acceptance

[Design](../../../../openspec/changes/specify-megamod-drug-response/design.md) names target groups
and direct composition. [Tasks](../../../../openspec/changes/specify-megamod-drug-response/tasks.md)
retain all implementation work unchecked. Document review/strict validation is separate from runtime
acceptance. DR-Q1–7 remain in [progress](../refinement/specification-progress.md), with source analysis
distinguished from final usability/balance/provider questions. No build, test, game launch or normal
save/config mutation has occurred for this documentation slice.

Two independent document reviews identified four source/composition gaps; all were corrected and
re-reviewed with no remaining findings: prisoner condition bills, actual partial exposure, operation
supply scope and cancellation provenance after player edits. Strict validation passed146items with
0failures. This is one capability with14requirements,42scenarios and26unchecked implementation tasks.
Prospective playability review retained ordinary costs/effects and explicit proposed dose assumptions;
finalized balance/native acceptance still requires the reviewed running build.

## Reconciled settings — 2026-11-07

Use global is per condition, covering its two-slot response bundle. A local flu override does not
freeze global wound-infection changes. Explicit empty override disables that condition; absence
inherits, including future global additions. Global deletion preserves local overrides. Scoped
clear-all and dormant overrides are specified in DR01–02. S134 uses actual doctor capability,
with no injury/scar veto. Initial original-provider overlaps are disabled and warned, not cooperative.
