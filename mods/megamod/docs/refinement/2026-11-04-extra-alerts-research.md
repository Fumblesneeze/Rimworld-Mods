# S084 — Extra Alerts research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Research started2026-11-04, continued2026-11-05. Specification only; no game launch, source execution,
runtime implementation or acceptance. [Change](../../../../openspec/changes/specify-megamod-extra-alerts/),
[coverage](../replacements/extra-alerts.md), [ledger](specification-progress.md).

## Public documentation first

[Workshop1926923167](https://steamcommunity.com/sharedfiles/filedetails/?id=1926923167) lists urgent,
mood, animal and miscellaneous notices, including hidden enemies, unarmed colonists, traders and
Odyssey quills; paused caravans are explicitly1.0-only. An author reply describes hidden-enemy
presence rather than guaranteed location. A user reports a compact communications console missing
orbital alerts: a research lead, not established provider compatibility.

Then inspected [public repository](https://github.com/Zylleon/ExtraAlerts) and installed About text.
The latter has an older, shorter alert list. GitHub's cached page omitted1.7 in its listing, but the
actual clone includes current1.7 source/binary. Language keys also outlive removed features; neither
a README/listing nor a translated label establishes a working alert. No README/license file or
affirmative reuse grant found in the bounded clone/distribution scan. Independent implementation;
do not copy source code, DLLs or art.

## Identity and retained inputs

| Input | Value |
| --- | --- |
| Selected entry | S084/P2-13, Extra Alerts |
| Author/package | Zylle / Zylleon; `zylle.ExtraAlerts` |
| Installed root | `F:/Steam/steamapps/workshop/content/294100/1926923167` |
| Declared versions | 1.0–1.7 |
| Selected DLL | `1.7/Assemblies/net10.0/Z_MoreAlerts.dll` |
| DLL SHA256 | `BEE27DC4B9AB1DAFA91AF894CBC12D4869FFB51A04269224ED70498865D03030` |
| About.xml SHA256 | `2FDEC6B9A28EA441E0AC47664D50631A644AC054D3B021CB911B46F07333DFEC` |
| Public revision | `1937247e488842cb39f7b1ac8666fc55ab3a7560`; committed1.7 DLL matches installed |
| Assembly metadata | Decompiled AssemblyVersion0.0.0.0; project net10.0 and GenerateAssemblyInfo=false despite version properties1.0.0.0 |
| Native input | 1.7.4998 rev1299, root Assembly-CSharp.dll; SHA256 `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |

Ignored evidence `artifacts/Refinement/megamod-spec-20261104/extra-alerts/`: `upstream`, full selected
`binary` decompilation and selected `native` types. No source build was run. Installed root has the
singular filename `LoadFolder.xml`, with1.7 branch and separate root Languages; current native
ModContentPack also has automatic version/Common/root fallback. Actual loading/translation resolution
still requires a future native run; source metadata is not acceptance.

## Complete selected-binary inventory

Read all24 classes in `binary/Z_MoreAlerts` (20 concrete alerts, one abstract styling helper, settings,
Mod entry and DefOf), plus AssemblyInfo and selected public source/csproj. Source uses native Alert
subclasses, no Harmony or PatchOperations. This table records code-derived behavior, not a live run.

| Class / group | Actual source condition / effect |
| --- | --- |
| EnemiesOnMap | AllMaps_Spawned, HostileTo player, not downed, not Fogged/IsHiddenFromPlayer; lists/counts names |
| HiddenEnemiesOnMap | Same but hidden; both cb_enemies and cb_hiddenEnemies required; supplies actual pawn culprits as well as names/counts |
| EnemyNeedsRescue | Spawned humanlikes hostile to player, common NeedsRescue |
| AllyNeedsRescue | Spawned humanlikes with nonplayer nonnull faction; not prisoner, AllyOrNeutralTo player, common helper. Includes neutral factions despite label |
| NeutralNeedsRescue | Spawned factionless humanlikes, common helper; no explicit hostility exclusion |
| EntitiesDowned | Spawned RaceProps.IsAnomalyEntity, any faction, common helper; reads **cb_enemyRescue**, not serialized cb_entityDowned |
| Common NeedsRescue | Downed, not in any bed, not Pawn_CarryTracker, and no queued job able to begin now. Does not prove a queued job is care |
| Blight | All maps' HarvestablePlant listers, Plant.Blight nonnull; designated plants remain |
| Bonded/Tainted/HumanLeather/Lovers/SharedBed | Alert_Thought over respective NotBondedAnimalMaster/DeadMansApparel/HumanLeatherApparelSad/WantToSleepWithSpouseOrLover/SharedBed thoughts |
| AsceticBedroomQuality | Free colonists across maps/caravans/travelling transporters, no cryptosleep; GetAllMoodThoughts, Ascetic stage>=5, no mood-null/nullification guard or explicit dedupe |
| AnimalHeatstroke/Hypothermia | Spawned animals with player faction, matching hediff CurStageIndex>=2; no caravans |
| UnroofedElectrical | All maps' allBuildingsColonistElecFire candidates, not roofed at Position; no explicit ownership or weather/power filter |
| UnarmedCombatant | AllMaps_FreeColonistsSpawned, no equipment.Primary, violent work enabled, not downed, manipulation capable; optionally removes Juvenile stages |
| TraderOnMap | Spawned humanlikes with trader tracker whose CanTradeNow is true |
| TraderOrbital | Any passingShips.Count>0 and any colonist building with exact defName CommsConsole; boolean report without navigation targets |
| PorcupineQuills | Odyssey gate, alive/spawned free colonists across maps/caravans/travelling transporters, not suspended, PorcupineQuill hediff present; cached list used by explanation |

Enemies/hidden/blight inherit native critical red pulse. Four downed reports inherit source
SemiCritical, which changes background to grey but retains critical priority/activation messaging.
Other alerts are medium (default enum value or explicit ascetic setting). Ordinary native behavior
still controls suppression, click navigation and critical message lifecycle.

### Settings and language audit

Twenty-one static booleans are serialized globally. All field defaults are true except child inclusion;
hidden-enemy Scribe missing-key default is **false**, conflicting with its field initialization.
Other Scribe defaults match. `cb_entityDowned` is serialized but never used in reporting and has no UI
row. Source child/hidden controls are greyed when their parent is off, but not structurally disabled.
Settings are one scroll view grouped Urgent/Mood/Animals/Other/Odyssey. No bottom tab.

Actual ordinary catalog is19 alerts plus the hidden-enemy feature; child inclusion is an option.
Unused strings include NightOwl, AnimalGoingWild, HandlerNeeded, PausedCaravan and ColonistNeedsRest.
No current classes implement those keys. Do not add them based on translation inventory.
Six source languages: English/French/Japanese/Russian/ChineseSimplified/PortugueseBrazilian. German
and Spanish baseline coverage is absent; placeholder usage and rendered completeness were not verified.

## Current native evidence

Inspected `Alert`, `Alert_Critical`, `AlertPriority`, `AlertReport`, `Alert_Thought`, `AlertsReadout`,
`Alert_HypothermicAnimals`, `Building_CommsConsole`, `Pawn_TraderTracker`, `ListerBuildings` and
`ModContentPack`. These were decompiled from the exact native assembly identified above.

- AlertsReadout reflects leaf Alert types; no Harmony registration is required. After initial600
  ticks/tutorial grace, ordinary checks are spread over24 update frames. Hover recalculates; click
  gets current culprits and cycles forward/back via native camera jump/select. Report collections
  are references, not immutable snapshots; independent cached explanations can go stale.
- Alert_Thought uses current free colonists across maps/caravans/travelling transporters, no suspended
  pawns, skips absent mood and ThoughtNullified, adds each pawn once. Its label appends xN when plural.
  Custom ascetic code omits these safeguards. Native thought handling, not surface relationships or
  apparel alone, is the actual source of the mood conditions.
- Native critical code emits MessageCriticalAlert when becoming active, not every continuous frame.
  SemiCritical inherits it; grey is not a lower urgency tier.
  This creates a separate Message with captured LookTargets; later message/archive clicks do not
  refresh the alert report. Owned historical messages must retain known locations only, never track
  a later-hidden pawn. Exact message/archive lifecycle is still implementation research and acceptance.
- ListerBuildings.Add adds shortCircuitInRain buildings to the electrical-fire set **outside** its
  player-faction branch. Its name does not establish ownership. Owned reporting adds explicit player
  ownership and keeps the supported actual exposure-cell rule.
- Native trader eligibility includes willingness, casual interaction, nondowned/nonprisoner,
  nonplayer/nonhostile and eligible goods/favor currency. It is not negotiator-specific reachability.
- Native comms checks electricity/power; float menu also checks path/talking/contact targets. Ship
  announcement explicitly selects TradeShip, unlike source orbital report's any-passing-ship test.
  Current native-derived communicator behavior provides an inspection anchor for variants, not a
  blanket guarantee for every modded console.
- Native Alert_HypothermicAnimals reports factionless **wild** animals, only in developer mode; it
  does not duplicate ordinary colony-animal illness reporting.

Selected Core XML comparators: Heatstroke and Hypothermia both have hidden/initial stages, then minor
at0.20, serious0.35, extreme0.62. Ascetic stages5+ represent impressive-bedroom unhappiness at−5 mood;
earlier stages are positive/neutral. These are current unfinalized source facts, not finalized
provider values. Anomaly data is absent locally; entity/containment acceptance needs real present
content. Odyssey types/data are available for read-only reference; no quill surgery was executed.

## Intentional owned changes and composition

The observable contract moves hidden information to an explicit developer-only top-toolbar read-only
diagnostic, closed initially and cleared on mode/save changes. It has no ordinary alert/setting,
automatic message or hidden-target navigation. This follows the user's UI hierarchy; it is not
claimed source parity. All ordinary reports filter hidden subjects before any counts/text/messages.

Fix independent entity preference; mutually exclusive downed categories and accurate neutral-visitor
label; suppress only actual bed/carry/containment/resolution rather than any ready queued job. Native
player rescue remains separate. Mood reports require an actual negative, non-nullified thought;
ascetics gain no-mood safety/dedupe. Explicit ownership prevents foreign electrical buildings from
leaking into colony reports. Orbital reporting requires a real trade ship and supported communicator,
retains presence during power outage with honest status and adds map/terminal navigation.

Common settings supply19 independent ordinary On defaults plus child Off. No hidden-enemy default
ambiguity remains because debug viewing is transient, not a migrated setting. No automatic source
config import. Source package present owns overlap; owned rows/debug view suppress duplicates without
mutating its preferences. This cannot guarantee what another mod chooses to display.

Direct composition: S069 actual blight removal; S039/S040 trading gathering and S027 deliveries;
S062 medical work and the other selected treatment policies; bonded hunting-companion behavior.
Alerts observe their native results and never self-patch or assume ordering a job completed it.
Use native candidate listers and the common map-owned data only for proven shared derived facts.
No source-specific persistent cache, per-pawn cached permission or speculative generic scheduler.

Bounded installed XML/C#/JSON search found only source About/deps and RimPy database package records
(including `zylle.extraalerts.zh`). ASCII DLL search found only source-version DLLs. No actual incoming
runtime consumer was demonstrated. Public namespaces, NeedsRescue/IsHidden helpers, DefOf and typoed
settings key cb_porqupineQuills are inventory, not a promised facade, package spoof or save contract.
ASCII scans do not exhaust UTF-16 package/reflection strings in .NET user-string metadata; broader
metadata/#US inspection is required before any concrete compatibility/facade claim.

## Balance, safety and open work

Information changes playability without changing resource prices: early awareness affects rescue,
crop loss, combat and trade. Preserve known-information boundaries, precise independent toggles and
normal alert pacing. No illness threshold/mood effect or combat/medical outcome is changed. Source
minor20% illness and impressive−5 thought anchors are provisional until finalized-provider verification.
Alert fatigue, duplicate native/provider reports and actual rendering/polling cost need native review;
no performance or balance measurement is claimed by public “lightweight” comments.

| Question | Remaining work |
| --- | --- |
| XAL-Q1 | Finalized thought/illness/visibility/roof-exposure semantics, absent-safe Anomaly containment and exact present-provider contracts |
| XAL-Q2 | Native multi-map/travel/carry/bed/save lifecycle, report consistency, activation pacing and measured shared enumeration/update/memory cost |
| XAL-Q3 | Actual compact/custom communicator identity and normal contact path, true trade ships and same-map outages; no guessed provider support |
| XAL-Q4 | Alert fatigue, proposed corrective scope, debug placement and narrow seven-language native UI; no imported source settings without contract |
| XAL-Q5 | Focused honest RED/GREEN, any necessary patch admission/rollback/repatch tests, independent implementation review and final native action/result evidence |

## Independent review and validation

`review_extra_alert_conditions` and `review_extra_alert_ui` found no actionable issues in their
scoped independent passes. The UI review's message/archive lifecycle edge was made explicit in the
spec and acceptance tasks, retaining historical known locations without tracking new hidden movement;
focused re-review found no issues. Its bounded ASCII/#US consumer-search limitation is retained above.

Typed strict validation passes106 items, zero failed. Nine scoped Markdown files pass local links/
whitespace. The draft has11 requirements,34 scenarios and22 unchecked tasks. All runtime tasks remain
unchecked. Documentation/schema validity cannot accept gameplay.
