# S113 — Scenario Searchbars research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **RimWorld1.7
beta**. Origin **replacement**. Read-only documentation/binary/native inspection on2026-11-05.
[Change](../../../../openspec/changes/specify-megamod-scenario-search/) ·
[coverage](../replacements/scenario-searchbars.md) · [ledger](specification-progress.md).

## Public documentation first

The [current Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3579415129)
is titled Scenario Searchbars[1.6], created by Robfal2, posted2025-10-03. It describes search for
large scenario-part choice lists and claims extensions of native parts also benefit. This is intent,
not proof every override or a1.7 editor works. Installed metadata credits Nimrag and advertises1.4,
1.5 and1.6. Do not collapse display author and assembly metadata or infer1.7 support from a directory
fallback. The [current changelog](https://steamcommunity.com/sharedfiles/filedetails/changelog/3579415129)
does not supply additional behavior detail in the inspected response.

The [earlier Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3052467818)
directs users to the current release. Its description records expression support, stat-multiplier
search and later scheduled-incident search. No public repository/source or license link was found
on either description, in the installed payload or in the bounded source search. The complete
selected binary therefore fills the gaps. This is not a claim no public source exists anywhere.
No code/art reuse permission is inferred; implement the observable contract independently.

## Exact inputs and inventory

Installed root: `F:/Steam/steamapps/workshop/content/294100/3579415129`.

| Input | Inspected identity |
| --- | --- |
| Package | `nimrag.scenariosearchbar`; Harmony dependency; no LoadFolders.xml |
| Selected binary | `1.6/Assemblies/ScenarioSearchbar.dll`,11264bytes, assembly0.0.0.0 |
| Binary SHA256 | `0FE9241F3FBF924A9774E53C0276B6742D7BF8C0D22171D6B6497E335906A0AE` |
| Binary MVID | `516fb426-2eb5-4a4d-91c7-199eb8cf7a92` |
| Current native |1.7.5000rev1275; Assembly-CSharp.dll15636480bytes |
| Native SHA256 | `D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254` |
| Version.txt SHA256 | `BAD5BED449D17B2AF39925A3A25007164B2B1F89B29A4E24AFA3B505EDCC535A` |

The installed payload contains About.xml, PublishedFileId.txt, preview.png and three versioned
DLLs1.4/1.5/1.6. No Defs, patches, translations, settings resources, source or license file. Only
the selected1.6 binary is the behavioral source; older DLLs are not claimed equivalent. The preview
was inventoried, not selected/copied. Offline decompilation did not execute or load provider code.

Ignored evidence: `artifacts/Refinement/megamod-spec-20261105/scenario-searchbars/` includes two
retained Workshop pages, five hashed installed/native inputs, PE metadata, full binary decompilation,
ten native extracts, bounded consumer hits and later document/review/validation evidence.

## Complete selected binary behavior

Twelve top-level runtime classes: startup `ScenarioSearchbar`, public `Tools`, and ten patches.
No settings class, Scribe hook, custom Def, bottom tab, debug menu or world/map component exists.

| Class / target | Observed behavior |
| --- | --- |
| ScenarioSearchbar startup | Static startup creates Harmony owner `nimrag.scenariosearchbar`, unguarded PatchAll and patched-count log |
| Tools.SearchString(ScenPart) | Creates/returns an entry in a static Dictionary<ScenPart,string>; default empty; no cleanup or persistence |
| Tools.DrawSearchbar(Listing_ScenEdit,ScenPart,int=0) | Uses current listing position, native row height and hardcoded English Search label; places a half-width input using an offset rather than allocating a new row |
| Tools.Filter<T>(IEnumerable<T>,ScenPart), T:Def | Materializes input; nonempty query builds IgnoreCase Regex and matches Def.label; attempted OrdinalIgnoreCase literal fallback catches evaluation, not constructor failure |
| ThingCount.DoEditInterface postfix | Draws search with default0 offset |
| ThingCount.PossibleThingDefs postfix | Replaces every result with Tools.Filter, without distinguishing UI calls |
| StartingAnimal.DoEditInterface postfix | Draws search with35 offset |
| StartingAnimal.PossibleAnimals postfix | Filters every result, without distinguishing editor, randomization or generation |
| StartingResearch.DoEditInterface postfix | Draws search with35 offset |
| StartingResearch.NonRedundantResearchProjects postfix | Filters every result, including callers outside editing |
| StatFactor.DoEditInterface prefix | Reimplements full native-style selector and multiplier UI, filters all stats then checks !forInformationOnly/CanShowWithLoadedMods; displays full stat label; suppresses original |
| IncidentBase.DoEditInterface postfix | Draws search with35 offset |
| CreateIncident.DoEditInterface postfix | Resolves internal type by name and draws search with35 offset |
| IncidentBase.DoIncidentEditInterface prefix | Draws a button and filtered incident menu with reflected incident assignment, then returns true into original method |

The table separates three Tools methods but the class count is still12; patches count10. Source
stat multiplier editing retains its native percent control bounds0..100 in the widget's factor
units. This source observation does not introduce a new owned balance range.

Static issues/limitations, **not live reproductions**:

- `new Regex` occurs before try. Invalid syntax can escape; no explicit match timeout exists.
  A valid pattern uses regex by default. The fallback is not a reliable literal-search mode.
- Def.label differs from the displayed full stat label in some cases. Search is unlocalized in its
  own label even though Def labels can be translated.
- Dictionary keys use mutable ScenPart.GetHashCode implementations. Editing item/stuff/count/quality,
  animal/count, research, stat/factor or incident can change lookup hashes. No reference comparer or
  removal/editor cleanup exists. State can become inaccessible or retained beyond its useful lifetime.
- Candidate postfixes are not UI-scoped. Current native Randomize and RandomPets use those methods,
  so a query can constrain non-UI candidate sets if it remains retrievable. Mutable-hash behavior
  means the exact symptom is conditional; do not state all starts are always restricted.
- Incident prefix returns true after its own button/menu path. Original drawing/input still runs;
  actual duplicate-menu behavior depends on event handling. It is a composition risk, not an observed
  assertion that every click opens two menus.
- Subclass overrides can bypass patched base candidate/draw methods. The page's broad extension
  statement is not universal compatibility. Material/quality/Add-part/trait/other families are not
  covered by these ten patches.

## Current native comparison

Ten complete extracts: ScenPart_ThingCount, ScenPart_StartingAnimal, ScenPart_StartingResearch,
ScenPart_StatFactor, ScenPart_IncidentBase, ScenPart_CreateIncident, Page_ScenarioEditor,
Listing_ScenEdit, ScenarioUI and FloatMenuUtility. Relevant native paths:

| Family | Editor behavior | Non-editor callers / preserved contract |
| --- | --- | --- |
| ThingCount | Item candidates sorted by label; choosing item resets stuff to default and quality to null; separate allowed-stuff/quality controls; count at least1 | Randomize uses PossibleThingDefs; saved thingDef/stuff/count/quality and mutable content hash |
| StartingAnimal | Random pet sentinel plus PossibleAnimals(false), quantity at least1 | Randomize uses false; RandomPets uses handling eligibility then fallback, petness and ideology weighting; PlayerStartingThings generates animals; selected animal/count/bond chance saved |
| StartingResearch | NonRedundantResearchProjects excludes projects represented by starting faction research tags | Same candidate method used by Randomize; project saved and finished on PostGameStart |
| StatFactor | Excludes forInformationOnly or unavailable stat; full list label, original factor editor | Randomize uses scenarioRandomizable; selected stat/factor saved, native factor result unchanged |
| IncidentBase | All current IncidentDefs through FloatMenuUtility, assignment to incident | RandomizableIncidents separate; saved incident; coexist/merge behavior unchanged |
| CreateIncident | Incident selector plus minDays/maxDays and repeat; no base DoEditInterface call | Random interval scheduling and execution after game start; source search must not alter timing |

Native ThingCount eligibility is `(Item && scatterableOnMapGen && !destroyOnDrop) ||
(Building && Minifiable)`, unless a legitimate override/provider supplies another candidate set.
Do not replace that with every ThingDef. Native animal editing deliberately passes false for
checkForTamer; generation has separate rules. These choices are not new megamod balance changes.

ScenarioUI constructs a Listing_ScenEdit and calls each current part's DoEditInterface in a scroll
view. Listing_ScenEdit allocates native part rectangles, remove/reorder controls and labels. The
source's offset overlay does not increase the allocated row height. Owned search must allocate
space and preserve these controls, not copy fragile coordinates.

Page_ScenarioEditor can load another scenario into the same page, randomize/create another,
toggle editing, save and begin configuration. ScenarioEdited compares the native scenario hash;
queries must not enter that hash or imply a save prompt. Search state belongs to the session/part,
not to global configuration or scenario serialization.

## Selected owned contract and deviations

| Decision | Reason / observable contract |
| --- | --- |
| Literal default, explicit regex mode | Ordinary punctuation is usable; regex remains available with visible mode and recoverable syntax/time-budget feedback |
| Current displayed labels | Search finds what the player can actually see, including full stat names and translations |
| Editor-only menu filtering | Search cannot constrain randomization, random pets or game-start effects |
| Session/reference identity | Query remains stable after real field edits/reorder; no retained removed parts or query persistence |
| Native candidate/actions preserved | No hidden eligibility broadening, duplicate menu, source full-method overwrite or changed scenario values; matching already-disabled provider rows keep their reasons and cannot be enabled by search |
| Clear no-match/error states | No automatic first-result selection; Random pet remains a separate native action |
| Per-family semantic admission | Unsupported foreign/editor shapes retain native usability; compatible additions remain allowed |

SCS01–07 specify these outcomes. Query/mode is contextual editing state, not a setting, so common
Use-global rows do not apply. No persistent feature preference or unrequested search scope is added.
Seven languages, text composition, scale and long-label acceptance remain future work.

## External contracts and bounded consumers

Public source API: ScenarioSearchbar.Tools.SearchString, DrawSearchbar and Filter<T>, in assembly
ScenarioSearchbar0.0.0.0. No direct external caller was found in installed `.cs/.xml/.json/DLL`
byte-text search for package/assembly/type tokens. Hits: provider metadata/DLLs plus the replacement
catalogue `3396308787/replacements.json`, which maps3052467818→3579415129 with the same package ID.
That is metadata, not a runtime consumer. Reflection, alternative encodings and noninstalled code
remain limitations of this negative search. No facade, package spoof or complete API drop-in claim.

No source query/settings save schema exists. Native scenario keys are unchanged; source-package
removal requires no query conversion. The owned implementation adds no scenario dependency by
searching. This is not a general safe-removal claim for every megamod feature. Active source package
means owned overlap inactive with a bounded startup warning; no automatic unsubscription/removal.

## Playability and acceptance state

The closest comparators are native ThingCount selection/default material/quality, starting-animal
editing versus random-pet generation, research nonredundancy and stat/incident controls. The draft
preserves their content/eligibility/timing and adds no research unlock, resource, quantity, job or
economy change. No balancing numbers need invention. Query lifetime and expression evaluation
must not influence gameplay RNG or scenario hashes. Static analysis cannot accept those claims.

| ID | Scope | Remaining work |
| --- | --- | --- |
| SCS-Q1 | Effective integrations | Implementation-revision menu/layout/lifecycle semantics, safe family groups, foreign overrides, stale callbacks and later repatches |
| SCS-Q2 | Search/UI limits | Measured expression/list bounds, shared utility, culture/localization/IME and readable layouts; specify visible limits before code |
| SCS-Q3 | Verification | Focused TDD, source/optional cases, package/seven-language checks and reviewed-build native save/start-game outcomes |

Independent provenance review found no issues. Behavior review identified an ambiguous distinction
between omitted candidates and displayed disabled rows; SCS02 and its scenarios now preserve matching
disabled rows/reasons without exposing omitted content. Focused re-review closed that finding on
2026-11-06. Strict typed validation passed135 items, zero failed; the ledger records final checks.
All runtime tasks remain unchecked. No product code/tests/build/install/game launch, normal
save/config write, Workshop mutation, art selection, commit or publication is part of this research.
