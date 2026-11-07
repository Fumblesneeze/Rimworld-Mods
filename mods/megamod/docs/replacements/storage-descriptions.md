# Storage Stats in Description — replacement coverage

## Identity and provenance

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin replacement **S098/P2-27**. Source **Storage Stats in Description**, ferny (published C# credit
Taranchuk), `ferny.StorageStatsinDescription` / canonical `ferny.storagestatsindescription`,
[Workshop 3740185419](https://steamcommunity.com/sharedfiles/filedetails/?id=3740185419).
About advertises 1.6/1.7; native fallback selects the installed 1.6 assembly/file 1.0.0.0.
[Research](../refinement/2026-11-05-storage-descriptions-research.md) pins hashes, complete source/
compiled behavior, native 1.7.4998 and bounded provider/consumer reads. No reuse license found.
**Nothing implemented or accepted; no binary/package drop-in compatibility claim.**

## Behavior inventory

Every row is **specified, not implemented**. Evidence is in the research above; the
[observable contract](../../../../openspec/changes/specify-megamod-storage-descriptions/specs/megamod-storage-descriptions/spec.md)
defines the actual proposed results.

| ID | Input → observable result | Preserved or deliberately changed | Owning requirement |
| --- | --- | --- | --- |
| SSD-01 | Read construction/card description → one contextual storage summary | Existing surfaces; actual storage models and native bookcases, exclude consumption-only settings; per-building scope | Explain storage on existing description surfaces |
| SSD-02 | Compare capacity → actual maximum in stacks/books and meaningful per-cell values | Correct source Stack size wording and rectangle assumptions; distinguish variable capacity, policy and unknown values | Distinguish capacity from item quantity and refill policy |
| SSD-03 | Read supported items → truthful physical acceptance summary | Correct empty/all/partial-category errors; separate initial/current policy and special restrictions | Summarize supported item types truthfully |
| SSD-04 | Read old-stat description → known block replaced without tail loss | Preserve later prose, nonstorage descriptions and foreign information; ambiguous normalization withheld | Replace only recognized obsolete storage information |
| SSD-05 | Change context/configuration/language → current readable summary | Replace startup snapshot with scoped current presentation; no private inventory cache | Keep storage descriptions current and readable |
| SSD-06 | Configure color → valid scoped heading color | Common inheritance, orange #E5A84C default and reset, input validation, immediate redraw and seven languages | Configure the heading color through common settings |
| SSD-07 | Co-install or change providers → admitted presentation or clear fallback | Original overlap suppressed, actual competing surfaces coordinated/deferred, unrelated behavior preserved | Preserve other providers and ordinary storage behavior |

## Interactions and composed result

- S005 owns linked storage; this summary remains about one building, while S049's refill quantities
  retain group totals. Refill limits do not rewrite physical capacity here.
- S050 presets and admitted storage adapters share physical acceptance facts, without turning a
  player's current selection into a building restriction or silently changing filters.
- S036's owned Architect UI calls the summary directly; S034 formats quantities consistently.
  No patches against owned features or independent source-port formatting/index caches.
- S021/S033 map facts are reused only if a real provider needs them; maximum capacity and type
  acceptance normally use definition/model facts, not a scan of stored Things.
- Optional ASF/Reel's storage needs its actual model. Relevant Stats has a demonstrated competing
  construction contribution. Soft Warm Beds is a verified classification counterexample, not a
  general storage provider. These narrow reads do not add whole optional mods to the replacement list.

## UI audit and configuration

| Source entry | Purpose | Owned destination / change |
| --- | --- | --- |
| Generated definition description | Passive planning/inspection information | Existing construction and information-card descriptions, context-aware and readable |
| Global titleColor text field | Presentation setting | Common Storage descriptions → Heading color, validated Global default / Current save / Use global |
| Reset button choosing yellow | Reset presentation | Reset editable scope to the actual orange built-in default #E5A84C |

No bottom tab, bottom-right toggle, debug action or storage gameplay window is added. New/missing
save values inherit dynamically, dormant overrides survive and main-menu save callbacks are
inactive. Building filters/refill/linkage remain entity state. English, German, Spanish, French,
ChineseSimplified, Russian and Japanese are required; source ships English only.

## External contracts and persistence

| Surface | Evidence and treatment |
| --- | --- |
| Startup.ProcessDescription(ThingDef,string), public settings and Mod/static field | Bounded installed scan found no external consumer; no speculative facade or assembly alias |
| Source global titleColor key | No automatic import; use common owned configuration and valid effective color |
| Global mutation of Def.description | Deliberately omitted; render scoped text without contaminating other consumers or changing source definitions |
| Source package identity | Original active → suppress owned overlap with explanation; never modify provider or pretend to be its package |
| Reel's known legacy block | Narrow package/content/grammar admission; replace only proved block boundaries, preserve all unrelated text |
| Relevant Stats construction replacement/tooltip | Coordinate admitted active presentation or defer owned competing surface, preserving other stats |
| ASF instance/quality capacity and physical filters | Optional narrow model with current facts; unknown values never become raw rectangle guesses |

No world/entity save migration or generated-description save state is added. No source code/art
reuse license is presumed. Core and Harmony are required when native hooks are implemented;
optional providers remain absent-safe. Source metadata/Workshop dependency discrepancies are in
research. No XML patch is proposed; any later use requires XML Extensions and its guarded merge
contract. No source/Gateway assemblies ship in the product.

## Implementation and verification

[Proposal, design and tasks](../../../../openspec/changes/specify-megamod-storage-descriptions/) retain
all runtime work. Every SSD row still lacks executable target specs, code, focused RED/GREEN,
reviewed-build native evidence and measured cost. Omitted source behavior includes tail truncation,
empty-as-Everything, indiscriminate interface classification, misleading units, raw provider capacity,
unvalidated color markup, inconsistent reset and stale global generated text.

SSD-Q1–4 retain exact integration/block grammar, finalized provider projection, provenance/UI
lifecycle and acceptance. Final proof needs native Architect/card actions and actual storage/config
workflows, not a startup marker or generated string assertion. Verify common settings, distinct
instances/maps/saves, seven-language layouts, preserved description content and actual optional
provider groups. Retain causal views, build/mod identities, normal-config hashes and cleanup.
Semantic review must confirm unchanged physical capacity, contents, costs, research, work, filters
and hauling; source XML comparators do not substitute for finalized/native evidence.
