# Scenario Searchbars

## Identity and provenance

- Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target1.7 beta.
- Origin **replacement**, S113/P2-42. Source `nimrag.scenariosearchbar`, current Workshop
  [3579415129](https://steamcommunity.com/sharedfiles/filedetails/?id=3579415129), earlier
  [3052467818](https://steamcommunity.com/sharedfiles/filedetails/?id=3052467818). Public creator
  Robfal2; installed author Nimrag. Advertised versions1.4–1.6; no inspected1.7 payload.
- Selected1.6 ScenarioSearchbar.dll11264bytes, assembly0.0.0.0,
  SHA256 `0FE9241F3FBF924A9774E53C0276B6742D7BF8C0D22171D6B6497E335906A0AE`.
- [Research](../refinement/2026-11-05-scenario-searchbars-research.md) records public-first evidence,
  all12 source classes/10 patches, native1.7 contracts, metadata, consumer search and limitations.
  No source repository/license located; independent implementation only, no code/DLL/asset copying.

## Behavior inventory

All rows are **specified, not implemented or verified** in
[the owned contract](../../../../openspec/changes/specify-megamod-scenario-search/specs/megamod-scenario-search/spec.md).

| ID | Player action → result | Defaults / edge cases | Evidence / requirement |
| --- | --- | --- | --- |
| SCS01 | Search supported primary scenario selectors in their existing editor | Item, animal, research, stat, incident and scheduled incident; secondary controls retained | Complete patch inventory/native UIs; SCS01 |
| SCS02 | Type a name fragment → ordinary displayed matches | Case-insensitive literal default; native order, duplicate identities and disabled states/reasons; Random pet separate | Tools.Filter plus native candidate methods; SCS02 |
| SCS03 | Enable expression mode → expression matches or recoverable error | Initially off; no silent fallback or indefinite freeze | Original docs/Filter; intentional correction, SCS03 |
| SCS04 | Select a result → original choice action once | Material/quality defaults and other current values preserved; stale choice rejected | Native menu callbacks; SCS04 |
| SCS05 | Edit/reorder parts → independent stable query | Clear on removed part/session/scenario replacement; not saved or dirty | Source dictionary/current mutable hashes and editor lifecycle; SCS05 |
| SCS06 | Start/randomize/save scenario → native outcomes | Search cannot influence random pets, candidates, timing, RNG or scenario data | Native non-UI callers; SCS06 |
| SCS07 | Provider/shape changes → admitted search or ordinary editing | Bounded warnings, source overlap suppression, seven languages | Foundation plus inspected targets; SCS07 |

## Interactions and composed result

Existing owned content selection still determines which definitions exist. This feature filters
only currently eligible selector results; it neither restores removed definitions nor duplicates
that policy. Reuse suitable owned text matching directly, not patches against owned implementations.
No map Thing index is relevant to pre-game Def-choice menus. Arbitrary custom scenario editors and
overridden native candidate methods need actual adapter inspection; no blanket support claim.

## UI audit and relocation

| Source surface | Purpose / class | Owned destination and change |
| --- | --- | --- |
| Per-part search fields in scenario editor | Contextual scenario editing | Same editor, space allocated around ordinary controls, translated labels, optional explicit regex |
| No source bottom tab, debug button or settings window | None | None added |

Search text/mode is transient context, not settings or saved scenario data. No Use-global row or
main-menu save override is created. The scenario editor remains usable from the main menu. Choosing
scenario content retains native permissions and serialization; this feature adds no live-game cheat.

## Contracts consumed by other mods or saves

| Consumer/evidence | Binding | Decision / verification |
| --- | --- | --- |
| Replacement catalogue metadata | Same package ID across old/current Workshop IDs | Source co-install suppression, not a runtime facade |
| No external runtime consumer in bounded search | Public ScenarioSearchbar.Tools methods and assembly identity | No speculative shim or package spoof; revisit only for proven caller |
| Native scenarios | Native item/stuff/quality/count, animal, project, stat/factor and incident fields | Preserve original callbacks and native files; search never serialized; verify actual save/load/start workflow |
| Source dictionary only | In-memory ScenPart→query | No persisted query schema or migration; owned lifecycle deliberately bounded |

## Implementation and coverage

| Scope | Implemented | Deviation / conflict | Missing / omitted |
| --- | --- | --- | --- |
| SCS01–07 | Nothing | Literal default and explicit regex; display-name matching; safe expression errors; stable session state; editor-only filtering | All implementation, target specs, tests and native acceptance |
| Other scenario selectors | Nothing | Does not promise all custom parts or editors | Add-part, materials, qualities and unrelated part families deliberately outside this source's scope |
| Original provider active | Nothing | Owned overlap disabled with warning; provider untouched | Exact source-present run |

Harmony required with actual use; no hard DLC/provider/other product-mod/Gateway dependency. No XML
mutation selected. Later XML Extensions use requires its dependency and content/shape guards in the
same slice. No upstream DLL shipped. Native candidate eligibility and selection behavior remain the
balance boundary; no new costs, yields or scenario execution rules.

## Verification and acceptance

Documentation review/validation is tracked in the [ledger](../refinement/specification-progress.md).
SCS-Q1–3 retain effective target, UI/regex measurement and implementation evidence work. Future
acceptance requires actual search→native selection→save/load/start-game observations on the reviewed
build in a fresh minimized isolated game, source/optional-present cases, all seven languages, config
hashes and cleanup. Unit results, decompilation and startup logs cannot accept native behavior.
