# S026 — Use Best Materials research

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Inspected **2026-11-02**, sequentially after S025. Origin: replacement, photo **P1-20**.
This is refinement evidence, not an implementation or live verification report.

## Documentation and exact inputs

The first Steam request returned 429; the author's installed About description and public repository
were read before implementation inspection. A later retry of the
[Steam description](https://steamcommunity.com/sharedfiles/filedetails/?id=2901561962) succeeded:
per-bill automatic best-material choice, multiple comparison choices, material tooltips and ingredient
display sorting. The author explicitly warns that Performance Fish's WorkGiver_DoBillOptimization
category must be disabled for the source mod. This is an upstream limitation, not proof that every
Performance Fish configuration conflicts with our future implementation.

[Recent change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/2901561962)
record 1.6 support in July 2025, malformed-Def/butchery fixes, translation support, Math! repairs,
fixed/non-fixed ingredient handling and weighted display sorting. The
[older notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/2901561962?p=2) record
bill-copy/BWM integration and the original UI additions. These are documentation claims, not live
acceptance of the currently installed binary.

| Input | Inspected identity |
| --- | --- |
| Source | Use Best Materials, Kathanon, `kathanon.UseBestMaterials`, Workshop `2901561962` |
| Installed root | `F:/Steam/steamapps/workshop/content/294100/2901561962` |
| Installed About | SHA-256 `730FC0C945B09A872C0811E05B33A9FF98C3A9EB4C8BCF14A646D7FE0982CE31` |
| Installed inspected DLL | `1.6/Assemblies/UseBestMaterials.dll`; file/product `1.0.0.0`; SHA-256 `384B7C94E1B0B890BF52C63CE343AFF5979860D5E46EA083500396C217B1626A` |
| Declared support/dependency | 1.4, 1.5, 1.6; Harmony required and load-after; no 1.7 payload |
| Public checkout | [kathanon/RimWorld-UseBestMaterials](https://github.com/kathanon/RimWorld-UseBestMaterials), commit `6e6e4d1e5f3d0ff92a03c414c9774e2823150d24`, 2022-12-28 |
| License | BSD-3-Clause, copyright 2022 kathanon; retain applicable notice, conditions and disclaimer for future licensed reuse; no endorsement claim |
| Native comparison | Typed installation inspection: `1.7.4995 rev309`, root `F:/Steam/steamapps/common/RimWorld`, net10.0 |
| Native Assembly-CSharp | SHA-256 `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21` |

Public README is only a title. Public source is older than the installed build: it has direct
Dialog_BillConfig hooks, hard-coded English and no comparer cache; the installed binary uses Window
context hooks, keyed labels, a product comparer cache, MyDefOf and robustness changes. Do not treat
the public checkout as a reproducible source for the installed DLL. Installed decompilation is the
authority for the behavior inventory below. No downloaded assembly/source/art is shipped or modified.

Local ignored evidence: `artifacts/Refinement/megamod-spec-20261102/use-best-materials/` contains
`upstream`, `installed-decompiled` and `consumer-paths.txt`. Earlier same-beta selected native
decompilations are in `workbench-linking/game/RimWorld.WorkGiver_DoBill.decompiled.cs` and
`quartermaster/game/Verse.GenRecipe.decompiled.cs` beneath the same dated artifact root. Their
original input identity is recorded in those feature reports; the current native hash agrees.

## Complete installed behavior inventory

Inspected all 17 feature-class files, plus assembly metadata, project inventory and English strings.
There are no content Defs, XML PatchOperations, settings class, bottom tab or gameplay gizmo.

| Code surface | Static evidence and resulting behavior |
| --- | --- |
| Main, Patches_Bill | Startup PatchAll then dialog setup. Prefix sorts `TryFindBestIngredientsInSet_NoMixHelper` inputs for an active bill and sets `alreadySorted`; this is the non-mixing path only. No whole-map best-material search is added |
| State | ConditionalWeakTable keyed by Bill; `active` starts false. Validity requires a material-made ProducedThingDef with relevant stats. Separate crafting and display comparers. Bill ExposeData saves active and crafting comparer only; Clone copies state |
| StuffComparers | Relevant properties from allowed Stuff factors/offsets and applicable product stats; melee DPS/penetration added. Installed cache returns the same mutable crafting and display comparers for every bill producing a given ThingDef |
| StuffComparer | Single criterion menu; melee starts DPS and offers Attack strength; apparel starts Armor and offers Insulation; other properties alphabetically. No menu constructs an ordered multi-criterion chain |
| CompareSingle | Abstract product stat with candidate Stuff; highest first except lower Flammability and WorkToMake. Non-Stuff candidates score zero. No pawn quality forecast |
| CompareWeighted | Armor = 2×sharp + blunt + 0.2×heat; insulation = cold + heat; attack = DPS + 4×average armor penetration. Raw native units, no normalization. Copy duplicates lists; Matches checks stat membership/count, not weights |
| CompareOrdered | Serialized lexicographic comparator, copied recursively; no installed creation UI found. Empty/missing parts are not robustly recovered. It is not evidence of a player-facing multi-rule editor |
| StuffComparerForThing | Breaks equal scores by squared horizontal distance from the supplied bill root. Copy accepts supported comparer identities; origin is mutable. No promised stable final tie |
| StuffComparerBasic | Display sort starts Default; sorts a copy within each ingredient category. Tooltip appends relevant material bases, factors and offsets; merges same displayed labels. This does not change allowed ingredients |
| Patches_Bill UI | During recognized bill Window rendering, captures bill via private field; shrinks filter and repositions the next two generic ButtonText calls. Appends descriptions and substitutes category child-list ordering only in captured context. Prefix/postfix cleanup has no finalizer |
| BetterWorkbenchManagement + mirror patch | Active original package `falconne.bwm`; resolves `ImprovedWorkbenches.ExtendedBillDataStorage.MirrorBills`; postfix copies source state to destination |
| Compat_Math | Active `crunchyduck.math`; resolves either `CrunchyDuck.Math.Dialog_MathBillConfig` or `.Dialogs.Dialog_MathBillConfig`; installed Window context includes that exact type |
| Strings, MyDefOf, Textures, ICompareFor | Keyed labels; late melee-penetration Def binding; empty texture utility; public compare/copy/label/tooltip interface. Weighted tooltip still contains an English literal |

The static cache plus mutable comparator assignment implies cross-bill preference leakage: choosing
a criterion on one bill can change another bill for the same product, including across save loads in
one process. `active` is separately stored, so this is not a shared activation flag. This is a
code-derived risk, not a personally observed game reproduction. The draft explicitly corrects it.
CopyFrom also leaves the destination unchanged if the source has no State, and can copy `active`
without a compatible criterion; these outcomes must not become our clipboard semantics.

## Native 1.7 selection boundaries

`TryFindBestIngredientsHelper` searches progressively through reachable regions, includes eligible
haul-source contents and special preloaded/autonomous inputs, and stops once a sufficient set is
found. Radius, forbidden state, reservations and recipe eligibility constrain discovery. Consequently
the upstream sort means best among materials discovered at that selection attempt, not globally best
on the map. The draft retains this scope with the already specified S024 linked-storage addition:
with perishable preference enabled, discovery of an eligible perishable candidate adds eligible
contents throughout its actual linked group before selecting, retaining radius/access even when
the first stock was sufficient. Wider unrelated/map-wide search is UB-Q1, not an accidental promise.

The non-mixing helper skips native distance sorting when `alreadySorted`, builds counts by definition,
tests required quantities and ingredient filters (fixed ingredients have their ordinary exception to
the bill filter), then selects physical stacks. Mixing has a separate value-per-unit/distance sort.
Native medicine selection has its own potency/order rules. Source activation is not proof that any
of those other routes is supported. Multi-product/dominant-material ambiguity must be detected.

`GenRecipe.MakeRecipeProducts` uses the actual dominant ingredient for Stuff-made outputs and color,
preserves recipe efficiency and delegates actual quality generation to the crafter. Route optimization
must not change that dominant material. Our Normal comparison is an informational reference, never
a command to create Normal items. Existing S009–S012 already specify that reference and explicit
material locking; S014/S024 protect started iteration choices through pickup, cleanup and reload.

## Contracts and actual consumer search

Read-only literal search, all installed Workshop `.cs`, `.xml`, `.dll` for `UseBestMaterials` /
`kathanon.UseBestMaterials`: **five files**, all this source's About, English strings and 1.4/1.5/1.6
DLLs. Same search in local game Mods returned no matches. This bounds the claim: no independent
consumer of source types was found by this scan, not proof of universal absence. No original facade
or package/assembly spoof is justified. Public source exposes `State.For(Bill,bool)`, State copying,
comparers and `ICompareFor`, but merely public members are not established consumption contracts.

The BWM and Math! bindings are **outbound source adapters**, not discovered consumers of this mod.
BWM is installed (Workshop 935982361, `falconne.BWM`); its inspected current MirrorBills takes
`Bill_Production sourceBill, Bill_Production destinationBill, bool preserveTargetProduct,
bool initialCopy = true`. Its native-copy/mirror lifecycle has already been researched in S017.
No Math! or Performance Fish provider About identity was found in the installed Workshop/local Mods
scan; unrelated descriptions mentioning Fish were not treated as installed providers. Their current
optional runtime shapes remain UB-Q3. Do not claim tested compatibility or copy the source's blanket
instruction to disable a whole optimization category into our runtime admission policy.

Source save schema: under each bill, `kathanon.UseBestMaterials`, `active`, deep `compare` with
source-qualified comparator type. Single: `stat`, `label`; weighted: `stats`, `weights`, `label`;
ordered: deep `parts`. Display sort is not serialized. Cached comparers can be overwritten as bills
load. Original labels are persisted display text, not safe stable identities. No source-file save
import, exact source-type deserialization facade or removal safety is claimed. A future explicit
importer needs backup, atomicity, restoration and validated postconditions before use.

## Proposed composed result and deliberate deviations

- Keep per-bill activation **off** initially and independent criterion choice; display sort defaults
  to ordinary order. Both are entity/view state, not global/save mod settings. Retain the three named
  source presets and their exact documented weights, with clear directions and readable explanation.
- A supported bill produces one unambiguous Stuff-dependent output kind through non-mixing selection.
  Output quantity may exceed one. Other recipes remain ordinary and explain unavailable automation;
  no fake active control for butchering, mixed input or ambiguous multiple products.
- Locks/permissions and started-iteration choices win. S024's enabled bounded linked-storage discovery
  participates before choosing, even if native early stopping would have accepted the first stock.
  For a fresh output-material choice, active
  bill criterion precedes S024 spoilage; equally preferred eligible materials/stacks use S024 spoilage,
  then proximity. Other ingredient slots keep their ordinary/S024 selection. No internal competing
  Harmony sorts. S059 receives this boundary when researched, not an assumed conflict now.
- Keep browser sorting and bill automation distinct. Existing Add's selected material is still a
  lock; toggling automatic preference does not broaden that filter. Named weighted bill choices do
  not introduce aggregate combat scores into the browser's expressly single-stat menu.
- Own choices survive load, absent content and compatibility suppression. Invalid saved active
  criterion pauses new owned automatic selections with a visible reason until repaired/disabled;
  never silently switch to another stat. Completed physical items are not rewritten. A valid recipe
  without the feature can still use the ordinary path with its ordinary filters.
- Keep list sorting local to the open view, stable within category, reset on reopening. Preserve
  description text; distinguish same-name stats by meaning/provider and do not leak UI context.
- Snapshot/clone/mirror must copy both active and dormant criterion independently, and clear stale
  destination activation when source is off or has no preference. Whole-list linking intentionally
  shares the logical bill; identical unlinked bills remain separate. Reject consequential unsupported
  paste atomically under S017 instead of losing a choice.

## Open questions retained for later

| ID | Topic | Current disposition |
| --- | --- | --- |
| UB-Q1 | Selection scope and source defaults | Retain discovered-search scope, off-by-default activation, preset weights and source single-stat directions; a deliberate map-wide optimum, custom weights or multi-criterion UI would need new observable decisions |
| UB-Q2 | Exact beta data flow and interrupted selection | Prove non-mixing count/dominant-material semantics, provider haul sources, multiple ingredients and S014 snapshot recovery; detect ambiguous output ownership rather than guessing |
| UB-Q3 | External adapters and migration | BWM outbound binding documented; current Math!/Fish absent. Future exact-provider admission and any explicit source-save importer need separate proof; no blanket drop-in claim |
| UB-Q4 | Implementation and acceptance | Per-target documents, common data reuse, focused RED/GREEN, seven languages, independent review and native action/result workflows remain open |

No user question is pending. Uncertainty is recorded here without stopping sequential specification.

## Document review and checkpoint

Two independent read-only reviews inspected source/contracts and observable composition. The
source/contracts pass found no issues. The behavior pass found one P2 ambiguity between native
early stopping and S024's promised linked-storage expansion. The spec now explicitly includes the
enabled bounded expansion before selection, even if the first stock suffices, with a combined
same-linked-group scenario. Design, tasks and coverage were reconciled; focused resolution review
found no remaining P1/P2 issue. No runtime verification was substituted for this document review.

Typed strict `openspec_validate`: **47 passed, 0 failed**. New capability: **8 requirements / 28
scenarios**; **24 implementation tasks unchecked**. Local links/whitespace in ten scoped Markdown
files: **0 issues**. Progress queue: **123 rows, 26 drafted**. No C#/XML implementation, tests, build,
deployment, save/config edit or game launch occurred. Source/behavior/balance assumptions remain
explicit; finalized native/provider results and performance are future acceptance evidence.
