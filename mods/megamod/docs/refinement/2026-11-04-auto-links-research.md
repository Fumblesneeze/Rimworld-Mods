# S074 — Auto links research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
Read-only inspection **2026-11-04**. Origin replacement. **Specification draft independently reviewed
and strictly validated; no implementation/acceptance.** Owning change:
[description links](../../../../openspec/changes/specify-megamod-auto-links/).

## Documentation first and installed identity

Read the [Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=2059389912),
then the linked [author repository](https://github.com/AUTOMATIC1111/Autolinks), installed README,
license and code. The description lists recipe relations with results, fixed ingredients, research,
workbenches and body parts/groups; buildable relations with research and fixed costs; egg/race
relations; and one-way nonhuman corpse-to-living-race links. Its terminology about optional ingredients
needs the precise source interpretation below. Generic removed/incompatible page boilerplate is not
evidence that the item was removed. User comments requesting1.7 or reporting long-link-list lag are
reports, not observed compatibility/performance results.

Title **Auto links**, author **AUTOMATIC**, package **automatic.autolinks**, Workshop **2059389912**.
Installed About advertises1.1–1.6 only. No LoadFolders.xml and no1.7 directory. Current native
ModContentPack.InitLoadFolders selects the newest available version not newer than the current game,
then Common/root; this makes **1.6/Assemblies/Autolinks.dll** the static1.7 fallback selection.
It does not establish that the source works on1.7. Assembly **1.0.379.18**, file **1.0.379.0018**,
net472. MIT copyright2020 AUTOMATIC1111; retain notice for future reuse. No source copied into product.

Read-only installed root `F:/Steam/steamapps/workshop/content/294100/2059389912`.

| Input | SHA-256 |
| --- | --- |
| About/About.xml | `90CA2B65BE9F2560439E92E8A6064ADC60277BBF701B718905DC7FFAF6439CBA` |
| Source/Autolinks.cs | `9E6DA88B3410556D41F04FAB471E33CD0FBF9D60C99490B899A5BEDD12A4EAB4` |
|1.6/Assemblies/Autolinks.dll | `B8643CAF4CD74119EA3F145A0A5211A9EFE4CD462A7DC8FF2D329F7B2AA5DB13` |

The package ships0Harmony.dll/XML in version directories, and its old project references Harmony,
but the source PatchAll call is commented. Full selected-binary decompilation shows one runtime
class, no Harmony invocation or selected assembly reference, no Mod subclass/settings/UI, no save
state, and no Def/Patch XML. Do not copy or infer a required runtime Harmony dependency from those
bundled unused files. Future owned dependencies follow the chosen implementation, not this artifact.

Full selected class and metadata are read under
`artifacts/Refinement/megamod-spec-20261104/auto-links/upstream/`. Current game hash rechecked:
`FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B`,1.7.4998 rev1299 reference.
Native extracts under sibling `native/` include Def, DefHyperlink, Dialog_InfoCard, StatsReportUtility
and Thing. Read the relevant description/hyperlink/navigation paths, not every unrelated method.

## Exact source relations and omissions

A StaticConstructorOnStartup first processes every ThingDef, then every RecipeDef. AddLink ignores
null endpoints, creates a missing descriptionHyperlinks list, and appends only when no existing entry
has that target Def. AddMulti adds both directions. Existing descriptions and links are retained;
there is no alphabetic sorting, relation heading or filtering by map stock/research completion.
It does not deduplicate pre-existing duplicates or remove stale links. No relation provenance is saved.

| Processed relation | Direction | Exact selected source boundary |
| --- | --- | --- |
| Recipe → product | Both | Every nonnull products entry; no special-products inference |
| Recipe → research | Both | Singular researchPrerequisite plus list researchPrerequisites |
| Recipe → bench | Both | recipeUsers only, excluding entries whose race is nonnull |
| Recipe → ingredient | Both | Each IngredientCount with IsFixedIngredient true, meaning its filter allows exactly one Def |
| Recipe → anatomy | Both | appliedOnFixedBodyParts and appliedOnFixedBodyPartGroups |
| Thing → research | Both | ThingDef.researchPrerequisites |
| Thing → fixed construction cost | Both | costList entries, not costStuffCount or all eligible Stuff |
| Fertilized egg → race | Both | CompProperties_Hatcher.hatcherPawn.race, not PawnKindDef |
| Nonhuman corpse → race | One way | race.corpseDef when race.Humanlike is false; no new human-corpse link |

Important distinctions for the future observable draft:

- “Fixed” is a filter with one allowed item type, not a cost percentage or optional quantity flag.
  A category resolving to one type qualifies; a multi-choice filter does not. The source does not
  intersect that filter with a bill's player choices or infer every compatible ingredient.
- The description says buildables, but iteration is **ThingDef only**. TerrainDef floors are not
  processed by this source. A broader floor contract would be an explicit correction, not parity.
- Bench links omit recipes supplied only by a workbench's own recipes list. Current ThingDef.AllRecipes
  combines that list with reverse recipeUsers references. Decide the owned relationship scope explicitly.
- Source skips pawn race recipe users, so surgery relations do not add every pawn species as a
  workbench. Fixed anatomy references still link both ways.
- No handling of research hidden/discovery state, unfinished work, byproducts, material alternatives,
  actual stock, recipe availability or special runtime-only relationships is added by the source.
- AddLink checks destination Def only, ignores contextual faction distinctions, permits self-links
  and assumes existing list entries are nonnull. ProcessRecipe recipeUsers filtering dereferences
  entries without a null guard. Invalid content can interrupt the static constructor. Corrections
  must preserve unrelated valid relationships and ordinary information rather than aborting all links.
- One startup scan does not refresh after later Def mutation/hot reload. Repeated calls avoid newly
  duplicated destinations but do not give a reversible ownership ledger for the added links.

## Current native information path

Def.descriptionHyperlinks is a public List<DefHyperlink>; DefHyperlink contains a Def plus optional
Faction. XML construction resolves the typed target. Def.ConfigErrors removes entries with null
target Defs and reports repeated destination Defs, but this earlier pass is not a runtime guarantee
for later mod changes or a null link object.

StatsReportUtility.DescriptionEntry(Def) makes the native Description stat entry from that list.
The Thing overload uses Thing.DescriptionFlavor and Thing.DescriptionHyperlinks; the base Thing
property enumerates its Def's links, while overrides can add their own. Preserve those paths and
context rather than overwriting every description or replacing all native links with a private UI.

Dialog_InfoCard.DefsToHyperlinks constructs Hyperlink from the generic Def. **A linked research Def
therefore opens an info card through this generic path**, not automatically the dedicated research-tab
selection constructor. Do not promise starting research, selecting work, placing a bill or manufacturing
anything. ActivateHyperlink preserves native history/back navigation and chooses ordinary default
Stuff for a ThingDef where appropriate. The information card is type-level knowledge, not a claim
that a real item exists or can currently be made.

Native Hyperlink.IsHidden consults HiddenItemsManager for Thing/ThingDef targets; the label becomes
NotYetDiscovered and activation returns without navigation. Preserve this protection and native
icons/tooltips/history; no source-derived bypass or new reveal of hidden content is justified.
Other target kinds may have distinct current visibility semantics that remain to inspect.

Current RecipeDefGenerator already adds product links to generated recipes. Those are part of the
native relationship set and must not be duplicated, overwritten or counted as newly implemented
source behavior. No complete1.7 native coverage audit is claimed yet for every source relation.

## Real consumer and composition evidence

Bounded installed C#/XML/JSON search for Autolinks/automatic.autolinks found only the source, a mod
manager database, layout-harness mod-list fixtures and **Info Card Plus** metadata. No callable
Autolinks API consumer was found; this does not exclude compiled-only or generic Def-link consumers.

Installed **Info Card Plus**, package **kaamalauppias.infocardplus**, Workshop **3776660810**, advertises
1.6 and declares loadBefore automatic.autolinks so generated links survive its setup. Its source
StatsPanel.Detail reads description.GetHyperlinks and draws them via native Widgets.HyperlinkWithIcon,
including when it hides the description text. This is a concrete consumed **native description entry**
contract, not a source package/API call. A replacement that only paints links in vanilla's original
window could lose that consumer. Do not add Info Card Plus to the feature queue merely because this
integration is inspected, and do not infer incompatibility from its UI replacement alone.

Read the selected compiled StatsPanel description-link and DrawHyperlinks paths plus BetterInfoUIMod
constructor under sibling `consumer/`; they confirm the relevant source behavior. Full consumer
behavior is outside this bounded integration check. Its selected DLL hash:
`4CC6B56D51C0CCF064EED415458FB6EBEA8FD944E08660A0D14597C19A80FF19`.
Its constructor applies Harmony patches; exact effective interception and setup timing remain to audit
before a compatibility claim. This package's dependency is not automatically a megamod requirement.

S010 recipe inspection already opens normal recipe/product/ingredient cards and preserves the Add bill
selection when returning. S012 owns the integrated browser, S033 physical Thing search and S038 content
selection. Related knowledge must compose without issuing bills/jobs, restoring removed content or
building a separate map Thing cache. A derived Def/recipe relation dataset has a different lifecycle
from live item locations; reuse common native/owned relation data where actually useful, with no
invented universal indexing framework. Source has no settings, bottom tab, debug control or operational
toggle to relocate. Any new configuration would need its own justified common global/save contract.

## Completed native and consumer follow-up

Read StatDrawEntry.GetHyperlinks and the DescriptionEntry overloads: the entry retains a private
hyperlink enumerable, returned before any stat-worker fallback. The selected design enriches this
ordinary entry while preserving its identity and metadata, rather than changing Def lists or replacing
drawing. The Thing overload preserves instance links; Pawn adds its non-custom, non-baseliner xenotype.
The native description scroll area draws every supplied link through Widgets.HyperlinkWithIcon; no
hard link-count cap was found. That does not establish acceptable large-list rendering performance.

Current ThingDef.AllRecipes unions building-owned recipes with reverse RecipeDef.recipeUsers, though
its cache is not itself a post-refresh guarantee. The draft explicitly includes that union and
TerrainDef buildables. BuildableDef.CostList can select difficulty-specific fixed costs, unlike the
source's raw costList pass. Positive declared outputs/costs, stable appended ordering, isolated bad
endpoints and no new self/duplicate links are proposed corrections. Optional Stuff and special-product
inference remain excluded. Native generic research navigation and item discovery checks are preserved;
no broader spoiler policy is inferred for every kind of Def.

Compiled Info Card Plus DrawStatsWorker reads native cached entries and passes them to its StatsPanel.
Its Setup postfix adjusts forcePause from its setting; it does not clear definition links. The inspected
source tree has no descriptionHyperlinks mutation. Thus the metadata loadBefore comment is not proof
of a clearing step. Its description pane's GetHyperlinks consumption is the concrete integration seam,
including when description text is hidden. Some unrelated decompiled consumer code lacks Unity
references; this bounded relevant-method inspection is not a whole-provider compatibility claim.

The chosen C# presentation derives current links from finalized/generated recipes, current costs and
instance/provider entries. Static XML insertion cannot supply that transient view. No post-load Def
mutation or XML operation is selected. Future Harmony is required for the narrow native entry adapter;
the source's unused bundled library is not the justification. Share finalized recipe/content data with
S010/S012, preserve S038 availability authority, and keep physical map search separate.

Composition clarification: S038 deliberately preserves definitions and existing goods, applying
availability at production/generation boundaries. Excluded-but-present types therefore retain
descriptive information; true missing content loses owned links. Neither information navigation nor
a shared relation dataset can re-offer an excluded recipe or publish pending S038 settings early.

Both entry routes, safe merge and retirement form an atomic integration. Relationship families can
fail independently only without half-published two-way edges. Original Auto links presence disables
the entire owned feature, including corrections, without altering the original's links. Unknown
raw-definition-list consumers are a documented limit, not covered by package spoofing. Exact effective
hooks, later repatch and stale-link retirement still need implementation-time evidence.

## Later questions and implementation evidence

| ID | Unresolved boundary | Current direction / evidence needed |
| --- | --- | --- |
| AL-Q1 | Exact effective entry/link/refresh/action admission and lifecycle | Candidate seams are not approved patches; prove consumed semantics, completed content/cost generations, atomic retirement and no stale owned activation on current beta, including later repatch. |
| AL-Q2 | Proposed coverage and presentation corrections | Retain terrain, building-owned recipes, current positive fixed costs and stable additions; verify complete accessible lists without optional-material/special-output guesses or silent truncation. |
| AL-Q3 | Actual external consumers and current optional provider | Preserve the demonstrated native-entry contract; inspect newly demonstrated raw-list consumers and the selected provider's effective1.7 behavior before expanding compatibility claims. |
| AL-Q4 | Implementation, localization, performance and native proof | Focused RED/GREEN, localized player explanations/invariant technical logs, combined relation/rendering measurements, independent review and real card/history/refresh/provider workflows remain required. |

Proposal, design, observable requirements, tasks and the [replacement report](../replacements/auto-links.md)
are drafted. Review and validation outcomes belong in the progress ledger; every runtime task remains
unchecked. These questions are recorded for later work, not requests for the user to explain the source.

Independent source/native review found no factual issue and confirmed the recorded hashes. Composition
review found an unconditional Add bill return promise: S010/S012 clear invalid choices and reset on
context changes. The corrected contract preserves only still-valid choices, follows their existing
clearing/comparison/reset rules with explanations and adds a return-after-removal scenario. It cannot
restore stale selections merely because an information card was open.
Focused composition re-review found the correction consistent. Strict OpenSpec validation passed
96 items with no failures; all22 runtime tasks remain unchecked. See the progress checkpoint for counts.

No runtime code/Defs/assets, tests, build/install, game launch, source execution, Workshop writes or
normal save/configuration mutation occurred. All evidence is static analysis; the full goal remains active.
