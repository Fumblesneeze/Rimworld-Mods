# Handwritten feature intake — 2026-11-01

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`). Target: **RimWorld 1.7 beta**.
Stage: preserved **refinement / transcription** evidence; sequential feature specification started
on 2026-11-02. Follow [specification progress](specification-progress.md) for the current queue,
source reports, owning changes and deferred questions. No runtime feature is implemented yet.

The [full installed-source audit](2026-11-01-source-audit.md) rechecks every entry, identifies
normalizations and custom notes, and corrects the initial missed matches. Use it alongside this
transcript before asking further feature questions.

**Interview correction, 2026-11-02:** the user explicitly asked not to explain existing mod
behavior. Investigate upstream functionality, controls and defaults directly before asking any
question. Do not turn source analysis into a feature-by-feature confirmation interview. Reserve
questions for unresolved custom changes or genuine ambiguities that research cannot settle.

This records the two supplied photographs and matches their names against locally installed mod
metadata. It is not `spec.md`, a replacement/parity claim, a compatibility claim, or an approved
implementation plan. A name identifies an investigation candidate; its behavior still needs to be
reverse-engineered under [AGENTS.md](../../AGENTS.md). Handwritten imperatives below are source
material, not instructions to execute now. Later OpenSpec changes are tracked separately from this intake.

## Reading guide and sources

- **R** means the handwriting is confidently read, possibly with an installed title resolving its
  spelling or abbreviation. Red strike-throughs in the annotated copies mean **transcribed**, not
  rejected, implemented, or deselected. Question marks in confidently read text are retained.
- **U** means some words remain uncertain; bracketed readings are guesses. Those uncertain portions
  are left without new red marks. The original black strike-through is the author's, not ours.
- A readable title does not settle intent, precedence, scope, or whether a source mod is wanted in
  full. Those questions are separate from transcription confidence.
- Package IDs below come from installed `About.xml` files. The [source index](installed-sources.json)
  retains exact titles, package IDs, Workshop IDs, local paths, supported-version declarations and
  metadata hashes. It proves presence in the inspected installation, not activation or 1.7 support.
- The unedited originals are authoritative: [page 1](images/page-1-original.png),
  [page 2](images/page-2-original.png). Annotated review copies:
  [page 1](images/page-1-read.png), [page 2](images/page-2-read.png).
  Image-editing prompts and limitations are recorded in [annotation-method.md](annotation-method.md).
- Local PaddleOCR supplied an initial pass; every entry below was visually checked and obvious OCR
  substitutions corrected. Sideways text is included. Raw OCR/inventory evidence remains under
  `artifacts/Refinement/megamod-notes-20261101/`, outside the product package.
- Printed stationery branding and clipped fragments of the neighboring sheet are not feature notes.
  The handwritten `1`/`2` and enclosing lines near page 1's bottom are preserved in the original;
  their significance as grouping or priority is not assumed.

## Page 1 — upper, left and middle notes

| ID | Read | Transcription / normalized reading | Installed candidate or refinement note |
|---|---|---|---|
| P1-01 | R | CM Color Coded Mood Bar | `CrashM.ColorCodedMoodBar.11` |
| P1-02 | R | Chill the fuck out | Installed title **Chill the Fork Out**, `KT.ChillOut`; preserve the wording difference. |
| P1-03 | R | Improved Need Indicator | `AmCh.ImprovedNeedIndicator` |
| P1-04 | U | Already crossed-out line: `[... shelves? → ... storage piles?]` | Exact text remains illegible. User confirmed it is already implemented in the base game and must be excluded; reconstructing its name is unnecessary for this intake. |
| P1-05 | R | Dress for the Weather | `Mlie.DressForTheWeather`; repeated at P1-39. |
| P1-06 | R → clarified | Link Stockpile Zones / Growing Zones | Stockpiles should use vanilla shelf-linking mechanics, including mixed shelf/stockpile groups. Linked growing zones synchronize the selected crop and the sowing/cutting controls. These directions are sufficient for refinement; detailed specification and vanilla inspection are deferred. |
| P1-07 | R | Compact Hediffs | `PeteTimesSix.CompactHediffs` |
| P1-08 | R | Allow Tool | `UnlimitedHugs.AllowTool` |
| P1-09 | R → clarified | Something like Quartermaster but directly in Bills menu and showing what can and cannot be made | Replace the Add bill dropdown with a richer view inspired by Nice Bill Tab and its Expansion: search, recipe-output category pills, craftability highlighting and output-stat sorting. Categories derive from this workbench's recipes; sort choices derive from the filtered recipes, with commonly used properties first. No separate menu or bottom tab. See interview details below. |
| P1-10 | R | Quality Builder | Installed **QualityBuilder Unofficial 1.6**, `hatti.qualitybuilder`; installed title does not establish 1.7 suitability. |
| P1-11 | R → clarified | Link Workbenches | Synchronize the entire bill list across linked workbenches: additions, removals, order and every bill's configuration. The user chose whole-list linking, not individually linked bills with other bills remaining local. No distinct source provider named. |
| P1-12 | R | Bulk smelting | `zal.bulksmelting` |
| P1-13 | R | Designations Tooltip | `CheaterEater.DesignationsTooltip`; repeated at P1-36. |
| P1-14 | R | Better Workbench Management / Everyone gets one / Most Skilled Bill Restriction | `falconne.BWM`; installed **Everybody Gets One - Continued**, `Memegoddess.EverybodyGetsOne`; `Matheus.MostSkilledBill`. “Most” continues down the right-hand column as “Skilled / Bill / Restriction”; this is one title, not a fourth fragment. |
| P1-15 | R | Cherry picker scrollbars | Installed **Cherry picker scroll bars**, `lucius.cherryscroll`; compose with Cherry Picker, P1-30. |
| P1-16 | R → partly clarified; reference updated | PUAH + optimized | Original photo match: **Pick Up And Haul**, `Mehni.PickUpAndHaul`. User later selected **Pick Up and Haul - Continued**, `Zzyxzz.PickUpAndHaulReworked`, as primary reference and **Reworked**, `Dreadarm.PickUpAndHaul`, as an alternative. Our hauling must respect pawn carrying capacity and shared-data requirements. Original capacity suspicion remains unverified; no performance target was supplied. [Reference update](2026-11-02-hauling-reference-update.md). |
| P1-17 | R → partly clarified | A recycle and destroy mod that works | **Recycling is limited to weapons and apparel. Destruction applies only to items with HP**, performed where the item lies through repeated actual pawn melee attacks until destroyed. Buildings, plants and living pawns are outside this item order. Micro Designations (P2-25) supplies the other item-specific options. Recycle This and Destroy Item remain research references, not blanket feature-parity selections; detailed recycling refunds and native attack lifecycle remain for specification. |
| P1-18 | R | Common Sense | `avilmask.CommonSense` |
| P1-19 | R | Prosthetic no missing Bodypart | **Prosthetic No Missing Body Parts (Continued)**, `Mlie.ProstheticNoMissingBodyParts` |
| P1-20 | R | Best Materials | **Use Best Materials**, `kathanon.UseBestMaterials`; keep distinct from Best Mix, P1-53. |
| P1-21 | R | Just Put it over there | **Just Put It Over There**, `Mlie.JustPutItOverThere`, is installed. The first lookup missed it. Its description concerns caravans placing purchased goods, and guests placing gifts, in reachable storage; it is not Just Ignore Me Passing. |
| P1-22 | R | Replace Stuff | `Memegoddess.ReplaceStuff` |
| P1-23 | R | Drug Policy Fix → also remove not found drugs | `Mlie.DrugPolicyFix`; removing missing-drug entries is an explicit extension, with persistence and missing-definition semantics unresolved. |
| P1-24 | R → clarified | Brrr and Phew + Toddlers fix | `Mlie.BrrandPhew` plus the separate `cyanobot.toddlers`. On 2026-11-02 the user confirmed toddler idle wandering should proactively avoid dangerous temperatures, and mobile toddlers should seek safety if their surroundings become unsafe. Reported wandering/rescue symptom and suspected source interaction are not independently verified. Specify the combined behavior directly. |
| P1-25 | R → clarified | Show buildable Material Count → Ctrl+F | `BP.ShowBuildableMaterialCount`; `Memegoddess.CtrlF`. The arrow means material counts must use the same index/cache as Ctrl+F. It does not request a click-to-search action. |
| P1-26 | R | Readable Numbers | `7h0m4s.RimworldReadableNumbers` |
| P1-27 | R | Better Log | **Better Log - Fix your errors**, `bs.betterlog` |
| P1-28 | R | Architect Menu Optimizer? | `MRK.architectmenuoptimizer`; question mark retained. |
| P1-29 | R | Blunt Social Fights | **[DN] Blunt Social Fights**, `dani.BluntSocialFighting` |
| P1-30 | R | Cherry Picker | `Owlchemist.CherryPicker`; separate from scrollbar amendment P1-15. |
| P1-31 | U → clarified | Trading Spot `(or area?)` | **[PL] Trading Spot**, `tradingpost.programmerlily.com`, is the reference. User wants a dedicated, fixed trading area with its own gizmos, drawn through area-designation tools, not a pawn allowed area. It gives more control over where traders go than a spot they hover around. Original image annotations retain the initial reading state. |
| P1-32 | R | Food Poisoning Stack Fix | `Mlie.FoodPoisoningStackFix` |
| P1-33 | R | Auto Self Tend | `funstab.AutoSelfTend` |
| P1-34 | R | Graze Up | **[Kit] Graze up**, `kittahkhan.grazeup` |
| P1-35 | R | Auto Name Babies | `cruesoe.autonamebabies` |
| P1-36 | R | Designations Tooltips | Repeat of P1-13, not an additional implementation. |
| P1-37 | R | Consciously Malnourished | Sideways: `ProfileName.MalCon`. |
| P1-38 | R | Consciousness Patch | Sideways, next to P1-37: `Vex.Consciousness`. |

## Page 1 — right column and bottom additions

| ID | Read | Transcription / normalized reading | Installed candidate or refinement note |
|---|---|---|---|
| P1-39 | R | Dress for the Weather? | Top margin; repeat of P1-05 with a question mark. |
| P1-40 | R | Mod Options Sort? | `Superniquito.ModOptionsSort`; also P2-30 with search explicitly mentioned. The installed source already describes author-aware mod-list search. |
| P1-41 | R → corrected | Grazing Area | User confirmed this is the wrong name: **Grazing Lands** at P2-16 is intended. Grazing Area is not a selected source. |
| P1-42 | R → clarified | Auto-Facing Chairs + manual fix | `scarf.autofacingchairs`. During placement, manual rotation overrides automatic facing for the current hovered cell. Mouse movement within that cell must retain the manual orientation; moving to another cell releases the override and permits automatic facing again. This says **Facing**, not trading. |
| P1-43 | R → partly clarified | Kanban Stockpile-ish | **KanbanStockpile Continued**, `endag.kanbanstockpile`. Group refill totals remain per item type across linked storages, with start-below X/refill-to Y applying only to automatic refilling. Separately, one standard slider applies to all stacks in a storage, initially **infinite/undefined**, limiting how far a hauler refills each stack. It never overrides an item's actual stack maximum or creates per-item slider values. Research remaining source behavior directly. |
| P1-44 | R → partly clarified | Simple Stockpile Presets + fixes! + UI change? | `Mlie.SimpleStockpilePresets`. Independent specs/tests/implementation address correctness. Prefer a compact selector instead of many preset gizmos: one dropdown gizmo or integration into storage configuration; placement remains undecided. Intended scope includes other storage types. Hide presets with no items the target storage can accept, such as meat for woody-only storage. |
| P1-45 | R | Autocast Specialist Commands | **Auto-Cast Specialist Commands**, `Linnun.AutoCastSpecialistCommands` |
| P1-46 | R | Lights Out / Turn it on and off | **LightsOut**, `juanlopez2008.LightsOut`; **Turn It On and Off - RePowered**, `Mlie.TurnOnOffRePowered`. Slash does not settle combination versus alternatives. |
| P1-47 | R | I can't fix that | **I Cant Fix That**, `Mlie.ICantFixThat` |
| P1-48 | R | Meals on Wheels | `Memegoddess.MealsOnWheels` |
| P1-49 | R | Ctrl+F | `Memegoddess.CtrlF`; arrow from P1-25 and repeated in P2-51. |
| P1-50 | R | Cut it off | `LegendaryMinuteman.CutItOff` |
| P1-51 | R | Meals over Preserved Foods | **Prioritize Meals over Preserved Foods**, `seekiworksmod.no10` |
| P1-52 | R | Smarter Doctors | **SmarterDoctors (Continued)**, `zal.smarterdoctors` |
| P1-53 | R | (That other “Random Materials” Mod) → Best Mix | `Mlie.BestMix`; arrow identifies Best Mix, without establishing its exact relationship to Use Best Materials. |
| P1-54 | R | Smarter Construction | `dhultgren.smarterconstruction` |
| P1-55 | R | Smarter Deconstruction and Mining | `Mlie.SmarterDeconstructionAndMining`; “Mining” continues beneath the first line. |
| P1-56 | R | Smart Medicine | `Memegoddess.SmartMedicine`; separate entry interleaved with P1-55's wrapped text. |
| P1-57 | R | Cut plants before building | `Mlie.CutPlantsBeforeBuilding` |
| P1-58 | R → partly clarified | Hunting rework? Hunting Spot / Hochsitz | Custom feature. Wait at the spot/seat for an animal within an initial 40-cell detection radius, with no line-of-sight check, then leave to hunt. Storage-style animal filter with predator/non-predator and small-game/big-game groupings, listing species that can appear on this map even when absent. **Hochsitz** = raised hunting seat/stand. |
| P1-59 | R → partly clarified | hingehen und Ausschau halten, dann erst schießen | Original: **go there and keep lookout, then shoot**. Interview clarification: wait at the spot/seat, detect an animal by range alone (initially about 40 cells), then leave to hunt it. No line-of-sight requirement; detailed targeting/return rules remain open. |
| P1-60 | R → custom direction confirmed | Hunting dogs? | User rejected Hunt for Me and requested a custom feature: all of a hunter's bonded animals with existing **Attack + Haul** training may accompany them, using the herd-wandering update from Pathfinding Lab. When shot prey falls unconscious or dies, one companion claims it, rushes to it, kills it if necessary, then hauls the corpse if a valid destination exists and it can carry it. Separate interview addition extends the directional wandering to penned/room-confined animals. |
| P1-61 | R | Add Deconstruct to Floors Category | **Add Deconstruct Button To Floor Category**, `geodesicdragon.deconstructfloorcategory` |
| P1-62 | R | All Memories Fade | `Mlie.AllMemoriesFade` |
| P1-63 | R | Auto-Cut Blight | **Auto-Cut Blight - 1.6**, `Defi.AutoCutBlight` |
| P1-64 | R | Automatic Night Owl | Bottom-right, partly cramped against the fold; installed title `ben.automaticnightowl` resolves the abbreviated/edge-obscured ending. |
| P1-65 | R | Better Message Placement | `Mlie.BetterMessagePlacement` |

## Page 2 — upper and left notes

| ID | Read | Transcription / normalized reading | Installed candidate or refinement note |
|---|---|---|---|
| P2-01 | R | Walls are solid | `Victor.WallsAreSolid` |
| P2-02 | R | Use Bedrolls | `Memegoddess.UseBedrolls` |
| P2-03 | R | Auto Links | `automatic.autolinks` |
| P2-04 | R | Unlimited Armour | Installed **UnLimitedArmor**, `Paragon.Hanul.UnLimitedArmor` |
| P2-05 | R | Linkable Hydroponics? | `Yakobo.LinkableHydroponics`; question mark retained. |
| P2-06 | R | Trees don't just die | `azuraal.treedontjustdie` |
| P2-07 | R → partly clarified | Don't generate everything in one Tick (Traders, Raids, Maps/Encounter etc.) | Distribute preparation across ticks. User confirmed raids and trader groups should appear together once the whole group is ready, rather than exposing partly generated groups to gameplay. Map/encounter generation details, scheduling, determinism and latency limits remain for specification. No threading architecture is selected. |
| P2-08 | R | Stop, Drop, and Roll | **Stop, Drop, And Roll! [BAL]**, `balistafreak.StopDropAndRoll`; multiple installed About files for this identity are retained in the source index. |
| P2-09 | R | Stabilize Bleeding | `Defi.StabilizeBleeding`; do not substitute the separately installed **Stabilize**. |
| P2-10 | R | Spray Transfer | `Spray.Transfer` |
| P2-11 | R | Easy Upgrades | `Mlie.EasyUpgrades` |
| P2-12 | R | Enemies use Apparel Too | **Enemies Use Apparel Too - EUAT**, `Argotis.EnemiesUseApparelToo` |
| P2-13 | R | Extra Alerts | `zylle.ExtraAlerts` |
| P2-14 | R | Food tracker | **FoodTracker**, `b0arl0ck.foodtracker` |
| P2-15 | R | Go-Juice is Medical | `orioM.Gojuicemedical` |
| P2-16 | R → confirmed | Grazing Lands (Not Area) | `avilmask.GrazingLands`; user confirmed this is the correct source, replacing the mistaken Grazing Area entry. |
| P2-17 | R | Growth Speed makes sense now | `Jdalt.GSMSN` |
| P2-18 | R | Healer Mech Serum Choice | `Syrus.HMSChoice` |
| P2-19 | R | Is this a weapon? (→ Mod Config) | `S4.IsThisAWeapon`; mod-configuration placement is explicit. Any entity-specific action still follows the existing distinction between gameplay state and settings. |
| P2-20 | R | Keep Burning, Corpse | `tixiv.KeepBurningCorpse` |
| P2-21 | R | Keep Converting | `Linnun.KeepConverting`; this says **Converting**, not constructing. |
| P2-22 | R | Labels on Floor | `falconne.LabelsOnFloor` |
| P2-23 | R | Log after Def Error | `Mlie.LogAfterDefError` |
| P2-24 | R | Max Buy, Max Sell | `Leoltron.MaxBuyMaxSell` |
| P2-25 | R | Micro Designations | `avilmask.MicroDesignations` |
| P2-26 | R | Mining Priority | `Uuugggg.MiningPriority` |

## Page 2 — right column and sideways additions

| ID | Read | Transcription / normalized reading | Installed candidate or refinement note |
|---|---|---|---|
| P2-27 | R | Storage stats in Description | `ferny.StorageStatsinDescription` |
| P2-28 | R | Spines Matter More Now | `Jdalt.SMMN`; installed title resolves the cramped handwriting. |
| P2-29 | R | Snap Out | **Snap Out!**, `weilbyte.snapout`; not “Swap Out.” |
| P2-30 | R → clarified | Mod Options Sort + Search | `Superniquito.ModOptionsSort`; repeats P1-40 and explicitly mentions search. Source description includes author-aware mod-list search. Separately, user confirmed megamod's own settings search matches **setting names, descriptions, feature names and category names**. |
| P2-31 | R | Monument Highlight | `espio.monhighlight` |
| P2-32 | R | More Power Info | `arl85.MorePowerInfo`; **Power**, not pawn. |
| P2-33 | R | Need Bar Overflow | `AmCh.NeedBarOverflow` |
| P2-34 | R | No burn Metal | `Unon.NoBurnMetal` |
| P2-35 | R | Permeable Terrain (optimize?) | `Owlchemist.PermeableTerrain`; optimization is a question, not an established problem or solution. |
| P2-36 | R | Quality base durability | Installed **Quality Based Durability**, `gabrieel1482.QualityBasedDurability` |
| P2-37 | R | Quick Stockpile Generation | Installed **Quick Stockpile Creation**, `longwater.smartstockpilecreation`; wording difference retained. |
| P2-38 | R | Rescuee Joins Plus / Wanderer Joins Plus | Both are installed: **RescueeJoinsPlus**, `Ogliss.RescueeJoinsPlus`, and **WanderJoinsPlus**, `Ogliss.WanderJoinsPlus`. The initial lookup missed the second because its title uses “Wander,” not “Wanderer.” |
| P2-39 | R | Right-Click Gizmo Indicator | `CheaterEater.RightClickGizmoIndicator` |
| P2-40 | R | Room Food | `TheLordDrake.RoomFood` |
| P2-41 | R | Scattered Stones | Installed **Scattered Stones 1.7**, `Owlchemist.ScatteredStones` |
| P2-42 | R | Scenario searchbars | `nimrag.scenariosearchbar` |
| P2-43 | R | Select perishable | `Danoweb.SelectPerishable` |
| P2-44 | R | Sensible bed ownership | `sensiblebedownership.1trickPwnyta` |
| P2-45 | R | Show gender/age in caravan form dialog | Exact installed title, `arl85.ShowGenderAgeCaravanFormDialog`. The original classification as custom was incorrect. |
| P2-46 | R → clarified | Skilled slaughtering (+ cooking) | `MrHydralisk.SkilledSlaughtering`; user confirmed Cooking should contribute to slaughtering effectiveness and skill should affect yield, not only duration. Combination with existing skill factors remains open. |
| P2-47 | U → clarified | `(+ amount[?])` | User confirmed skill-dependent output quantity. Latest direction: preserve base values and apply the skill-dependent yield change through the slaughter job only. Cooking 10 is the proposed neutral point; lower skill penalizes effectiveness and higher skill improves it. Earlier doubled-base/skill-16 suggestions are superseded. |
| P2-48 | R | Dubs Break mod | Sideways between columns: `Dubwise.DubsBreakMod`. |
| P2-49 | R | Toddlers | Sideways beside Keep Burning/Converting: `cyanobot.toddlers`; also implicated in P1-24. |
| P2-50 | R | Research Whatever | Sideways near bottom: `avilmask.ResearchWhatever`. |
| P2-51 | R | TD Find Lib + Ctrl+F | Sideways near bottom: `Memegoddess.TDFindLib` plus `Memegoddess.CtrlF`. |
| P2-52 | R | De-generalize work | Sideways near bottom: `Alias.DegeneralizeWork`. |

## Later selected sources

- **2026-11-02 — Drug Response (Continued):** explicitly added by the user after the Drug Policy
  Fix draft; this is not a photographed entry. Installed identity is `Mlie.DrugResponse`, Workshop
  **2196401201**, tracked as **S124** in the [specification queue](specification-progress.md).
  The user requires a **UI redesign**, describing the existing UI as unacceptable. Research its
  behavior and existing workflows during its own sequential deep dive; do not copy its layout or
  navigation. Apply the established UI placement rules and distinguish entity/policy choices from
  genuine mod settings. Exact replacement layout and interactions remain refinement work, with
  questions recorded for later rather than interrupting the research.
  Later Priority Treatment selection (S134) adds joint compatibility/consolidation research under
  DR-Q2/PT-Q1 without cancelling this UI redesign requirement.

- **2026-11-02 — While You're Up and hauling references:** the user selected While You're Up,
  now **S125**, and supplied its newer 1.7 port (`patch1.7.whileyoureup`, Workshop **3832180338**)
  for reference. Original `CodeOptimist.JobsOfOpportunity` (**2034960453**) remains comparison evidence.
  S021 now uses **Pick Up and Haul - Continued** (**3829549663**) with **Pick Up And Haul - Reworked**
  (**3813653998**) as an alternative research implementation. These variants do not create duplicate
  feature entries or determine the final implementation by name/age. See the
  [identity and research update](2026-11-02-hauling-reference-update.md); prior S021 research is
  explicitly legacy evidence requiring reconciliation, while confirmed user constraints remain.

- **2026-11-03 — dd.Armful Haul:** added as **S126**, `dd.armfulhaul`, Workshop **3808100512**.
  Compare it with both hauling continuations and prefer the better-supported, simpler behavior
  and implementation after analysis. **Visible hauled stacks on the pawn are required at minimum**,
  even if another source supplies the underlying hauling decisions. The author's Workshop page
  states incompatibility with Pick Up And Haul; exact causes and fork coverage remain unverified.
  This is comparison input plus a selected visual subset, not blanket adoption of all Armful
  features or multiple independent capacity allowances. See the [reference update](2026-11-02-hauling-reference-update.md).

- **2026-11-03 — RimHUD and RimHUD Passion Icons:** selected as **S127** and **S128**.
  Installed identities are `Jaxe.RimHUD` / **1508850027** and
  `DanZinagri.RimHUDPassionIcons` / **3829500081**. Each gets its own megamod-owned change during
  the sequential deep dive; the resulting HUD composes both behaviors directly, with no patches
  against our own implementation. [Identity and initial documentation](2026-11-03-rimhud-reference-intake.md).
  These are later additions, not new readings of either photograph.

- **2026-11-03 — Interaction Spot On X:** explicitly selected through
  [Workshop 3827781564](https://steamcommunity.com/sharedfiles/filedetails/?id=3827781564), tracked
  as **S129**. Installed identity: `dragonissa.InteractionSpotOnX`, author Dragonissa.
  Initial author metadata describes interaction spots over chunks/trees; complete behavior,
  options and compatibility remain to be researched in its own megamod-owned change.
  See the [identity audit supplement](2026-11-01-source-audit.md#later-additions-outside-the-photograph-audit).
  This addition is outside the photographed list.

- **2026-11-03 — Dynamic Spacefarer Policy:** added as **S130**, installed identity
  `asunib.DynamicSpacefarerPolicy`, author asunib, Workshop **3829638797**. The author describes
  automatic vacuum-resistant apparel inclusion in the Spacefarer policy, modded gear and existing
  saves, while preserving manual exclusions. Full behavior and Odyssey/content admission remain
  queued for its own megamod-owned change. See the
  [identity audit supplement](2026-11-01-source-audit.md#later-additions-outside-the-photograph-audit).
  This is a later addition, not a photographed entry.

- **2026-11-03 — Quality Colors (Continued) and RPG Style Inventory Revamped:** selected as
  **S131/S132**, respectively `DawnsGlow.qualcolor` / **3513846773** and
  `Sandy.RPGStyleInventory.avilmask.Revamped` / **2478833213**. Each gets its own megamod-owned
  change after source research. Record quality-color customization and Gear presentation as
  initial documented behavior, with full scope, settings and external contracts still queued;
  compose owned HUD/cargo UI directly. See the
  [identity audit supplement](2026-11-01-source-audit.md#later-additions-outside-the-photograph-audit).
  These are later additions, not photographed entries.

  **2026-11-06 S131 research follow-up:** [Quality Colors research](2026-11-06-quality-colors-research.md)
  and its owned draft now cover six palettes, native display coverage, scoped settings and actual
  consumer discovery limitations. S132 remains the next separate source analysis; no implementation
  or source-package impersonation follows from choosing an RPG-named color preset.

  **2026-11-07 S132 research follow-up:** [RPG Inventory research](2026-11-07-rpg-style-inventory-research.md)
  and its owned draft now cover three Gear layouts, complete item access, current native actions,
  shared quality/cargo/supplies and honest optional-provider fallback. Legacy discovery/import and
  current CE/RWoM proof remain explicit limits. No runtime implementation or compatibility accepted.

- **2026-11-03 — Bulk Load For Transporters:** selected as **S133**, installed identity
  `Ilarion.BulkLoadForTransporters`, author Ilarion, Workshop **3559292908**. Its metadata describes
  bulk transport loading and pack-animal unloading using Pick Up And Haul. Research its behavior
  and fork/provider contracts in its own megamod-owned change; compose directly with S021's
  hauling, capacity and shared data rather than retaining patches against our implementation.
  See the [identity audit supplement](2026-11-01-source-audit.md#later-additions-outside-the-photograph-audit).
  This is a later addition, not a photographed entry.

  **2026-11-07 research and scope:** [complete source/native research](2026-11-07-bulk-load-transporters-research.md)
  and [owned draft](../../../../openspec/changes/specify-megamod-bulk-transporter-loading/) cover pod/shuttle
  batching and carrier unloading with exact quantities and the shared real carrying allowance.
  **Portal loading explicitly excluded by the user**, including bulk portal jobs, controls and
  automatic cargo redirection. Vehicles remain deferred pending actual adapter evidence. No runtime
  implementation or compatibility acceptance; BLT-Q2–Q5 retain remaining evidence gates.

- **2026-11-03 — Priority Treatment:** selected as **S134**, using exact installed title
  `Dreadarm.PriorityTreatment`, author Dreadarm, Workshop **3813351382**. The user expects possible
  compatibility changes/consolidation with **Drug Response (S124)**. Analyze the actual interaction
  before claiming a conflict; specify shared treatment/drug behavior and UI coherently, with direct
  owned composition and separate source changes. **DR-Q2/PT-Q1** retain this research and the existing
  Drug Response UI-redesign requirement. See the
  [identity audit supplement](2026-11-01-source-audit.md#later-additions-outside-the-photograph-audit).
  This is a later addition, not a photographed entry.

- **2026-11-03 — Basic Caravan Loading:** selected as **S135**, installed identity `LM420.BCL`,
  author LM, Workshop **3370434966**. The complete installed XML assigns caravan gathering and
  carrier unloading to Basic work; actual priority/eligibility and integration with the selected
  work-category and hauling/loading features remain **BCL-Q1** research. Its own megamod-owned
  specification and guarded XML implementation are still pending. See the
  [identity audit supplement](2026-11-01-source-audit.md#later-additions-outside-the-photograph-audit).
  This is a later addition, not a photographed entry.

- **2026-11-03 — Floor Pipette Tool:** selected as **S136**, installed identity
  `ferny.FloorPipetteTool`, author ferny, Workshop **3706877822**. The Workshop describes sampling
  an existing floor through an Architect tool to select more of that flooring. Full source,
  terrain eligibility and input/UI behavior remain **FPT-Q1** research for its own megamod-owned
  change. See the [identity audit supplement](2026-11-01-source-audit.md#later-additions-outside-the-photograph-audit).
  This is a later addition, not a photographed entry.

## Confirmed during the refinement interview

These user clarifications supersede provisional readings, without promoting them into OpenSpec:

- **Product purpose:** a baseline mega-mod expressing the user's preferred version of how the game
  behaves. It is not intended as a content mod; the focus is changes to base-game behavior.
- **Grazing:** Grazing Lands is the intended mod. Grazing Area was a naming mistake, not a request
  to combine both. Graze Up remains a separate candidate whose interaction still needs analysis.
- **Cancelled line:** excluded because its functionality is already implemented in the base game.
  This is the user's scope decision; no independent claim about its unidentified implementation is made.
- **Trading:** use Trading Spot as a reference for a spiritual successor with a designated trading
  area, drawn through area tools, instead of a single spot around which traders gather. The purpose
  is better player control of where traders go. The user clarified **a dedicated, fixed trading area
  with its own gizmos, not a pawn allowed area**. Do not turn this into a generic allowed-area selector
  or require assigning visiting caravans to ordinary allowed areas. "Fixed" identifies this dedicated
  area; it does not establish an immutable footprint, preset size or fixed map coordinates. Exact
  gizmo actions, movement, fallback and lifecycle rules remain for specification.
- **Hunting spot/seat:** hunters wait at the hunting spot or seat until animals enter a detection
  radius, then leave the spot to hunt. The user's earlier "sight range" means **range alone**:
  **no line-of-sight check**, including for animals behind walls. Use **about 40 cells** as the
  initial, arbitrarily chosen tuning value; this is not a measured balance result. Record the
  eventual value in the feature spec and follow the existing numeric-parameter policy.
  Give the spot/seat a storage-style interface for enabling/disabling animals to hunt, with
  **predator/non-predator** and **small game/big game** groupings. **Map-specific** means listing
  species that **can appear on that map**, including species not currently present. Do not derive
  the list solely from animals currently spawned. Determine the eligible species from the relevant
  game/mod rules during specification. The filter belongs to the hunting gameplay interface, not
  mod settings. No small-game/big-game classification cutoff has been chosen.
  Detailed target selection and return behavior remain open.
- **Hunting companion (P1-60):** the user rejected Hunt for Me and explicitly chose a custom
  feature. Existing **Attack + Haul training is sufficient**; no separate Hunting training is
  required. If a hunter has a bonded animal with that training, it accompanies the hunter during
  the hunting job, hanging around them using the herd-wandering update from **Pathfinding Lab**.
  The hunter is the companion's reference for staying nearby. When the hunter
  shoots prey and the prey falls unconscious or dies, the companion rushes to the downed prey,
  kills it if necessary, and then hauls the corpse if a valid hauling destination exists and the
  animal can carry it. **All qualifying bonded animals may accompany the hunter**, with **only
  one companion claiming each downed prey** to avoid duplicate finishing/retrieval work. The
  user accepted this recommendation. Exact claimant selection and reassignment after interruption
  remain for specification. The destination/capacity condition governs hauling; finishing downed prey
  is not stated to depend on haulability. No dog-only species restriction, movement-speed bonus,
  or companion wandering radius has been selected. Detailed injury attribution,
  recovery/interruption and return behavior belong in later refinement
  or specification. The experiment's directional-walk source was inspected read-only; this is not
  implementation or acceptance in megamod.
- **Directional wandering extension (interview addition, not another photographed entry):** adopt
  the herd-wandering update from Pathfinding Lab and also apply its local directional walking to
  animals in a pen or room. For these confined animals, use much shorter walks, provisionally
  **2–3 adjacent cell steps per idle walk**, with the exact tuning still provisional. This interprets
  "cells to move" as walk length in the source algorithm, not a radius or speed increase. Confined
  animals do **not** select another herd animal as a center: they roam locally within their pen or
  room, starting from their own position and respecting the enclosure and legal movement. Wild
  herds retain their herd reference; hunting companions remain near their hunter.
  Preserve the source's generally persistent direction, obstacle turns and bounded local work.
  A shorter legal walk or no move must be valid outcomes: never retry until a requested minimum
  number of steps succeeds. When no move is possible, wait with normal idle cadence; no endless
  search, recursive retry, immediate repeated job churn, or global path search merely to wander.
  Later scenarios/tests must cover a single-cell room, tiny enclosures with fewer legal steps than
  requested, fully blocked neighbors, narrow passages, and a route blocked after selection. A
  trapped animal must remain responsive to normal jobs. This is idle movement; feeding, fleeing,
  finishing/retrieving prey and other purposeful jobs must keep their own routing and precedence.
  The confined-animal step count does not silently set the companion's follow radius. Exact
  enclosure recognition, boundary changes, companion regrouping and tuning belong in specification.
  This selects the wandering behavior, not the entire experimental pathfinder or a runtime
  dependency on Pathfinding Lab. Reuse owned policies within repository boundaries; all eventual
  megamod patches still require their own target specs and checks.
- **Stockpile linking:** extend vanilla's shelf-linking mechanics to stockpile zones and allow
  shelves and stockpiles to join the same linked group. The user initially favored synchronized
  priority, then explicitly chose vanilla shelf mechanics as the reference for the shared behavior.
  This is clear enough for refinement. Inspect exact vanilla semantics during later specification
  work; do not prolong this interview with implementation-level checks.
- **Growing-zone linking:** synchronize the selected crop and the controls for allowing sowing
  and cutting across linked growing zones. The user explicitly confirmed that those controls
  should synchronize too, rather than remain independent per zone.
- **Kanban-style refill scope:** evaluate refill thresholds against the total quantity of each
  **individual item type** across **all linked storages**. The user chose individual types only;
  do not provide category-wide targets that combine, for example, all meal types into one pool.
  The whole linked storage group is the aggregation scope,
  including the mixed shelf/stockpile groups already requested. Separate per-stack or per-member
  thresholds are not the requested model. The user approved two quantities per item and linked
  group: **start refilling below X** and **refill up to Y**. Once triggered, refilling continues
  toward Y even after the total rises back above X, allowing batches instead of constant top-ups.
  **Y applies only to automatic refilling**, not as a general storage cap. Other deliveries,
  including finished products from crafting bills, may take the group's total above Y.
  No numeric defaults for X/Y were chosen. These are controls for the storage group, hence gameplay state.
  **The stack slider is one standard control for all stacks in that storage**, clarified on
  2026-11-02. Its initial value is **infinite/undefined**, imposing no additional hauling limit.
  There are no per-item slider values or a separate feature enable/disable toggle. A finite value
  limits **how far a hauler continues refilling a stack**, not the actual maximum size of that
  item's stack. The actual applicable stack maximum always wins: a full stack remains full even
  if the slider is higher or unlimited. Different items can therefore stop at different native
  maxima under the same slider value. For finite values the hauling ceiling is the smaller of
  the configured value and that stack's applicable maximum; with undefined/infinite, ordinary
  capacity rules apply. The slider does not resize, split or remove existing stacks, nor make a
  general hard cap on non-hauling deliveries. Existing stacks at/above the slider receive no
  further refill from hauling. This is separate from the per-item group X/Y thresholds above.
  The assistant's proposed per-item defaults and framing as a hard stack-size cap were incorrect
  and are superseded. Five meals was only an example. Analyze remaining source controls directly
  and document actual custom deviations during specification instead of asking the user to explain them.
- **Workbench linking:** synchronize the entire bill list across linked workbenches, including
  adding/removing bills, their order and every bill's configuration. The user explicitly chose
  "the whole thing" over linking individual bills while retaining independent local bills.
- **Add bill view (Quartermaster-inspired):** the desired workflow belongs directly in **Add bill**,
  replacing its simple dropdown with a richer view. There must be no separate menu or bottom-bar
  tab for this feature. The user named Nice Bill Tab and its "extension" as UI references; the
  installed matches are **Nice Bill Tab** (`Andromeda.NiceBillTab`) and **Nice Bill Tab - Expansion**
  (`HICON.NiceBillTabExpansion`). These were introduced during the interview, not transcribed from
  the photographs, and do not establish a request for every upstream feature.
  The view should provide a search bar and filter pills at the top, such as weapons or meals,
  derived from the categories of products made by this workbench's recipes. Craftability highlighting
  requires **both enough ingredients and at least one colonist who meets the recipe's skill
  requirements**, as confirmed by the user. Provide a dropdown to sort by resulting product stats, for example
  "best armor blunt". Put commonly used properties first in that dropdown, and offer only
  properties relevant to the recipes remaining after filtering. Compare quality-dependent product
  stats at **Normal quality**, with **no quality selector**, as confirmed by the user. This is the
  comparison basis. Detailed ingredient-availability checks and the ordering of common properties remain for
  refinement/specification. This answer defines the requested comparison workflow; it does not
  establish a separate automatic combat-role loadout recommender.
- **Add bill material choice:** put a **Use all materials** checkbox beside the stat-sort dropdown,
  **unchecked by default**. Unchecked compares each recipe using the best compatible material the
  colony currently has enough of. Checked considers all compatible materials, including unavailable
  ones, and uses the best for the selected stat. The user confirmed these modes and explicitly
  reversed the initially suggested checkbox polarity. Whichever mode is used, automatically select
  the corresponding material for the recipe when adding its bill. Offer a way to add the bill with
  a different material; right-click is the user's tentative interaction choice.
- **Auto-Facing Chairs manual override:** while hovering over a placement cell, the player can
  rotate the chair manually. Retain that orientation for as long as the cursor stays in that cell;
  small mouse movements within the cell must not trigger an automatic rotation back. Moving to
  another cell ends the override and allows automatic facing again. This is a placement interaction,
  not a request for a persistent orientation lock on already-built furniture.
- **Slaughtering and Cooking:** Cooking contributes to the existing slaughtering effectiveness;
  skill affects output yield as well as time. The latest scope is **slaughter job only**, preserving
  the game's base yield values and allowing higher skill to produce more than that base. It is not
  a global increase for every carcass or hunting outcome. **Cooking 10 → 100%/neutral contribution**
  is the proposed baseline: lower Cooking penalizes slaughtering effectiveness and higher Cooking
  improves it. This supersedes the earlier doubled-base and full-recovery-at-16 suggestions.
  The exact skill-combination formula, affected products, curve/cap, and connection between the
  slaughter action and eventual corpse processing still need refinement; no storage or spawn
  implementation has been selected.
- **Settings search (P2-30; clarified 2026-11-02):** megamod's own settings search must match
  **setting names, setting descriptions, feature names and category names** so the user can find
  a control without knowing its section. This is separate from the source Mod Options Sort's
  documented author-aware search of the game's mod-options list. The answer does not select
  arbitrary searching inside other mods' custom settings controls. Search remains part of the
  common settings UI and preserves its global/current-save separation and disabled controls.
  Detailed matching, result presentation and navigation belong in specification.
  **Superseded in scope on 2026-11-14:** the user now explicitly requires individual-option
  search across all mods, without a supported-mod whitelist, like Needle. The earlier owned-only
  boundary above is historical; [S141's current contract](../../../../openspec/changes/specify-megamod-settings-search/proposal.md)
  extends the same native chooser while retaining common scope rules for our own settings.
- **Numeric parameters:** chosen numeric values must be documented in the eventual spec and at
  least named constants in code. User-adjustable settings are preferred where appropriate, using
  the existing common settings model; not every implementation detail must become a setting.
- **Allow Tool + Pick Up And Haul:** both remain selected candidates. The user sees no conflict
  requiring a decision now. Do not invent a blanket dominance rule or ask speculative conflict
  questions. Investigate concrete interactions during analysis; resolve a demonstrated conflict
  when one is found, with the relevant observable behavior specified before implementation.
- **Hauling carrying capacity:** our Pick Up And Haul-inspired behavior must respect the pawn's
  carrying capacity. Items held for hauling must count toward the applicable limit; a separate
  hauling inventory must not provide an oversized allowance that bypasses it. The user suspects
  the original mod's separate inventory ignores capacity; this is an unverified concern, not a
  demonstrated upstream defect. Inspect the target game's capacity rules and source behavior
  during specification to define the exact calculation and checks before implementation.
- **Buildable material counts and Ctrl+F:** the arrow in P1-25 explicitly means reuse the **same
  index/cache** for buildable material counts and Ctrl+F-style search. It does not request a click
  action or navigation between the interfaces. This identifies another consumer of the existing
  shared-data policy alongside search and hauling; do not introduce a separate cache per feature.
- **Meal carrying (additional interview preference):** routine meal carrying should apply to all
  colonists, excluding animals. Quantity, eligibility exceptions and detailed behavior remain open.
  This answer does not establish a source-mod identity called “Carry Food.”
- **Interview scope:** the user rejected speculative conflict groupings and an overly broad
  question about the desired dining outcome. Ask specific questions grounded in the actual notes
  and inspected behavior; do not infer a conflict merely because multiple mods touch related systems.
- **Source bugs and independent implementation:** we will specify the desired features and implement
  them ourselves, so upstream bugs do not require a separate user-supplied diagnosis or a ported
  bugfix layer. Correct behavior belongs in our observable scenarios, tests and verification.
  For Simple Stockpile Presets, the user began mentioning stockpile placement failures but did not
  finish the reproduction details; no defect or cause was verified. Do not block refinement on
  completing that report. Ask about intended behavior or UI changes when those remain unclear.
- **Storage presets:** the exact UI is undecided. The user tentatively prefers avoiding many
  individual preset gizmos; candidates are a single gizmo opening a dropdown or a selector integrated
  into the storage configuration interface. The intended scope includes other things that store
  items, not just stockpile zones. Filter choices by what the target storage is capable of accepting:
  a woody-only store must not offer a meat preset when it has no selectable meats. This concerns
  storage capability, not merely the player's current item selections. Do not silently choose a
  final UI placement during refinement; applicable shared UI rules still govern the later design.

- **Generation smoothing (P2-07):** the user confirmed preparation spread across ticks while
  raids and trader groups appear together only once the whole group is ready. Do not expose a
  partially generated raid to combat or make a partial trader group available to interact with.
  This is an observable arrival requirement, not a decision about threading, pre-generation,
  exact tick budgets, or how internal objects are staged. Preserve the complete-group arrival while
  avoiding a single large generation stall; no measurable performance claim is made yet. Exact
  latency limits, native incident lifecycle, cancellation/failure, save/load and determinism belong
  in specification. Maps/encounters remain in the original request; their loading/presentation
  behavior has not been separately clarified.
- **Toddler temperature safety (P1-24; clarified 2026-11-02):** the user confirmed the intended
  fix is that **mobile toddlers autonomously seek safe temperatures**, extending Brrr and Phew's
  temperature-avoidance behavior to the life stage supplied by the separate Toddlers mod. The
  user's reported symptom is that toddlers wander into bad-temperature areas and repeatedly need
  rescue, suggesting the source behaviors do not work together as expected. This is a reported
  interaction, not an independently reproduced compatibility defect or identified code cause.
  Specify and implement the combined behavior directly in megamod. A mobile toddler capable of
  reaching safety should take that action, rather than relying on repeated adult rescues. This
  does not eliminate ordinary rescue for toddlers that cannot get to safety themselves. The user
  additionally confirmed **idle wandering proactively avoids dangerously hot/cold areas**, and
  mobile toddlers **seek safety if their surroundings become unsafe**. Both prevention and
  response are required. Exact thresholds, destination/path
  eligibility, mobility stages and job precedence belong in specification.
- **Recycling scope (P1-17):** weapons and apparel are sufficient; do not expand recycling to
  other crafted items merely because their ingredients could be recovered. This answer concerned
  recycling, and does not choose a source mod. Destruction eligibility is separately defined below. Refund rules
  still need specification.
- **Destruction workflow (P1-17):** the user chose a pawn destroying an item **where it lies**.
  Eligibility is **only items that use HP**. "Everything with HP" was immediately narrowed by
  the user to items: do not include buildings, plants, living pawns or other non-item targets.
  Items without HP are ineligible; do not introduce instant disposal for them.
  The pawn repeatedly performs **actual melee attacks** against the item until it is destroyed.
  Use the normal attack, damage and destruction behavior, including applicable item destruction
  effects. Do not replace attacks with a work timer, direct HP subtraction based on melee DPS,
  or a direct delete when a progress bar completes. Detailed native eligibility, attack selection,
  stack damage, interruptions, reservations and job termination belong in specification.
  This order does not require hauling the item to a burner or workbench. Other item-specific
  processing options are covered by the already selected **Micro Designations (P2-25)**. Compose
  these behaviors directly in megamod; do not retain a patch between our own feature implementations.
  This is a gameplay job, not an instant debug-delete command. On-site disposal does not by itself
  confirm every Destroy Item feature or setting.

Correction to the interview framing: no actual Allow Tool / Pick Up And Haul conflict or odd-priority
behavior has been demonstrated. Installed descriptions identify Allow Tool's urgent-haul tools and
Pick Up And Haul's inventory-based collection of multiple items. These roles can be complementary;
this observation is not a completed compatibility audit.

Further interview correction: the assistant introduced “Carry Food” and “Just Ignore Me Passing”
as if they were in the handwritten notes; neither appears in the verified transcript. They are not
automatically added to the selected replacement list. At the user's request, **Just Ignore Me
Passing** was looked up: the [author's description](https://github.com/pardeike/JustIgnoreMePassing)
says it keeps construction from being interrupted when a colonist or animal passes through the
work location. The earlier description about social chats, hauling or dining was incorrect.
This lookup does not establish that its behavior is needed on the target beta.

Provenance of that error: “Just Ignore Me Passing” first appeared in the assistant's spoken
grouping of supposed dining mods, after the Allow Tool / Pick Up And Haul discussion. It was
not a transcription of any photographed entry, not an installed-source match, and not selected
by the user. The actual note **Just Put It Over There** remained intact in the written transcript.
There is no evidence establishing that one was misread into the other; attributing a specific
internal cause would be another guess. The three newly recovered source matches in the full
audit were already present in the original inventory, so their omissions were lookup errors,
not changes to the user's installation.

Read-only source check: the installed Skilled Slaughtering description and `Defs/StatDef/StatDefs.xml`
define a slaughtering-speed stat using Animals and Melee, with work-speed and capacity factors.
They do not establish the requested Cooking/yield extension. This is source evidence, not live
verification or acceptance of the installed mod on the target beta.

Read-only source check for P1-43: the installed KanbanStockpile Continued description identifies
per-stack refill thresholds, a limit on the number of similar stacks, and a maximum stack size.
The user chose item totals across all linked storages for the adaptation's refill thresholds.
The user clarified the standard slider as a storage-wide hauling limit for all stacks, initially
infinite/undefined, always subordinate to each item's actual stack maximum. It is not a per-item
or hard stack-size override. The source's other controls and exact implementation still require
read-only analysis; the user should not be asked to supply their behavior or defaults.

Read-only source check for P1-17: the installed
[Recycle This (Continued) description](<F:/Steam/steamapps/workshop/content/294100/3253550009/About/About.xml>)
describes item designations for weapons/apparel, with pawns taking them to appropriate workbenches
for recycling or destruction instead of the player configuring bills. It describes vanilla-based
refunds and work speed, plus a choice to exclude components/complex items from refunds. The installed
[Destroy Item description](<F:/Steam/steamapps/workshop/content/294100/2423311270/About/About.xml>)
only says colonists destroy designated items. These are research inputs, not selected providers,
or verified behavior on 1.7. The user subsequently chose weapons/apparel as our recycling scope.
Further inspection of Destroy Item's
[README](<F:/Steam/steamapps/workshop/content/294100/2423311270/README.md>) and
[job source](<F:/Steam/steamapps/workshop/content/294100/2423311270/Source/JobDriver_DestroyItem.cs>)
shows on-site destruction: a pawn approaches the designated item and works on it there, without
hauling it to a workbench. Its documented scope includes resources and corpses as well as equipment.
Its inspected job source calculates HP loss from melee DPS and labor speed, rather than issuing
actual melee attacks. The user's chosen behavior deliberately differs: only HP-using items and
real repeated melee attacks, with no instant deletion for items that lack HP.
This differs from Recycle This's described workbench disposal. The user selected on-site
destruction, with Micro Designations supplying the other options. Micro Designations' installed
[description](<F:/Steam/steamapps/workshop/content/294100/1877972690/About/About.xml>) confirms that
it provides designations for actions otherwise available through bills and recipes. Detailed
supported operations and their contracts still need source analysis during specification.

P1-60 provenance: the assistant initially assumed this was custom without confirmation. The user
asked about the mod's behavior; no exact installed title/package match for "Hunting Dogs" was found,
so Hunt for Me (`aRandomKiwi.HuntForMe`, Workshop `1593245720`) was investigated as a candidate.
After hearing its documented behavior, the user rejected it and explicitly requested the custom
hunting companion described above. The source index retains Hunt for Me as excluded, not selected;
no identity confirmation is outstanding and none of its other features are implied.

Read-only source check for the wandering interview addition: see
[Pathfinding Lab's wildlife adapter](../../../PathfindingLab/Source/WildlifeWander.cs),
[directional walker](../../../PathfindingLab/Source/Core/DirectionalWalk.cs),
[existing host tests](../../../../tests/PathfindingLab.Unit/DirectionalWalkTests.cs), and
[owning experiment contract](../../../../openspec/changes/add-pathfinding-lab/specs/pathfinding-lab-routing/spec.md).
The inspected experiment restricts this shortcut to ordinary factionless ground-animal wandering,
with 3–10 steps, persistent heading, at most eight direction attempts per step, and no repeated
cell within a walk. It excludes pen providers. A trapped walk returns only its starting cell;
the runtime preserves native waiting. Its existing trapped-walk test checks eight rejected
directions; that test was read, not executed as megamod evidence. The current core rejects fewer
than three requested steps and the adapter discards paths with fewer than four cells (including
the start), so both constraints need deliberate adaptation for the proposed 2–3-step confined
walks and valid shorter results. The reference implementation is not already suitable unchanged.
This source inspection does not establish live correctness, performance, or compatibility of the
new behavior. No runtime change or gameplay acceptance accompanies these refinement notes.

## Remaining refinement and source-analysis work

The intended meanings of both active uncertain annotations have been clarified. The cancelled
line's exact text is still unreadable but excluded. Original marked images show the initial reading
pass; the interview clarifications above supersede its uncertainties.

Research the items below before asking anything further. Existing source behavior and defaults
are analysis work for the agent; ask only about unresolved custom intent or a demonstrated ambiguity:

1. How should Graze Up combine with Grazing Lands? Investigate actual interaction before asking
   the user to decide any unresolved behavior.
2. Toddler temperature safety is clarified: idle wandering proactively avoids dangerous temperatures,
   and mobile toddlers seek safety if their surroundings become unsafe. Recycling is limited to weapons/apparel;
   destruction uses actual melee attacks against items with HP. Further source analysis and
   specification must cover these selected behaviors. Upstream bug diagnosis is not required
   from the user; independent implementation must meet its own correctness scenarios. Storage
   presets now have a direction: compact selection across storage types, excluding inapplicable
   presets. Final placement is deliberately undecided between a dropdown gizmo and the storage UI.
   Auto-Facing Chairs is clarified: manual rotation persists until the hovered placement cell changes.
3. Analyze the named source pairs (LightsOut / Turn It On and Off and Rescuee / Wanderer Joins Plus)
   before proposing a choice. A slash is not evidence of a conflict or mutually exclusive behavior.
   The custom hunting spot/seat direction is already recorded.
4. Source identities for Just Put It Over There, WanderJoinsPlus, and the caravan gender/age
   display are now resolved by the full installed-source audit; no identity question is needed.
5. Linking direction is clarified for later specification: stockpiles use vanilla shelf mechanics,
   including mixed groups; growing zones synchronize the selected crop and sowing/cutting controls;
   workbenches synchronize the entire bill list, its order and every bill's configuration.
6. For the slaughter-job yield change, define affected products, the connection to eventual corpse
   processing, skill combination, yield curve and cap. Preserve base values and use the proposed
   neutral Cooking level 10 as the refinement starting point; measure/playtest before acceptance.
7. Hunting now starts with waiting at a spot/seat until an animal enters the detection radius
   (initially about 40 cells, no line-of-sight requirement), then leaving
   to hunt it. The spot/seat gets a storage-style animal filter with predator/non-predator and
   small-game/big-game groupings, listing species that can appear on the map even when absent.
   Refine target selection, return behavior and failure paths before turning this into gameplay
   scenarios. P1-60 is now an explicitly custom companion: bonded trained animal, herd-style wandering
   around its hunter, then finishing/retrieving downed prey. Existing Attack + Haul training qualifies.
   All qualifying bonded animals may accompany the hunter; one companion claims each downed prey.
   Use Pathfinding Lab's directional wandering as the reference, and extend idle wandering to pen/room
   animals with provisional 2–3-step walks, no herd-animal center and bounded no-move behavior.
   Exact lifecycle and failure cases remain for specification. Hunt for Me is excluded.
8. The trading area is a dedicated fixed area with its own gizmos, not a pawn allowed area. During
   specification, distinguish the caravan's gathering destination from restrictions on
   individual movement; define participants, arrival/departure, danger, unavailable areas and edits
   while a caravan is already visiting. These are unresolved design questions, not selected defaults.
9. For the richer Add bill view, the highlight requires enough ingredients and a colonist meeting
   the recipe's skill requirements. Detailed availability checks and common-stat ordering remain
   for specification. Compare at Normal quality with no quality selector. Placement, search, category pills,
   highlighting, and relevant/common-stat sort choices are clarified. Use all materials is unchecked
   by default: compare the best material
   currently available in sufficient quantity; check it to include unavailable compatible materials.
   Carry the compared material into the new bill by default,
   with an alternative-material action tentatively accessed through right-click. Nice Bill Tab and
   its Expansion are interview-added UI references.
10. For Kanban-style storage, refill calculations use item totals across the entire linked group.
    Individual item types only, with no category-wide targets; separate start-below and refill-up-to
    quantities are confirmed. Y stops automatic refilling only; other deliveries may exceed it.
    One standard slider for all stacks in a storage defaults to infinite/undefined and limits only
    hauling, always respecting each item's actual stack maximum. It is not per-item configuration
    or a hard stack-size override. X/Y defaults and remaining source controls require analysis/specification.

## Relationships to investigate before specification

These are investigation groupings, not new feature commitments or asserted dependencies.

- **Production and materials:** Better Workbench Management, Everybody Gets One, Most Skilled Bill
  Restriction, Quartermaster-inspired bill UI, linked workbenches, Bulk Smelting, Use Best Materials
  and Best Mix. Nice Bill Tab and its Expansion are additional interview references for the Add bill
  view. Determine composed behavior and external consumers before designing shared controls.
- **Storage, search and hauling:** linked zones, Kanban-style stockpiles, presets, quick stockpile
  creation, buildable material counts, Ctrl+F/TD Find Lib, Pick Up And Haul, Common Sense and
  Just Put It Over There. Apply the existing shared-data policy; do not allocate one cache per port.
  The user specifically confirmed that buildable material counts must use the same index/cache
  as Ctrl+F; the handwritten arrow represents shared data, not a new UI navigation action.
- **Source amendments:** Cherry Picker + scrollbars and Brrr and Phew + Toddlers fix must become
  directly composed behavior where selected, not patches against our own code. The installed catalog
  may contain other related patches, but mere presence does not add them to the requested list.
- **Item processing:** custom on-site destruction and weapons/apparel recycling must compose with
  Micro Designations' other item-specific processing options. The user explicitly linked these
  features; avoid duplicate controls/jobs and specify the selected actions directly.
- **Medical and capacity interactions:** prosthetic missing-parts behavior, Drug Policy Fix,
  Smart Medicine, SmarterDoctors, self-tending, Go-juice, serum choice, consciousness changes,
  Spines Matter More Now and stabilization need overlap/default/balance investigation.
- **Construction and plants:** QualityBuilder, Replace Stuff, construction/deconstruction/mining
  changes, cut-before-build and Auto-Cut Blight may share native job paths or conflict in ordering.
- **Diagnostics and configuration:** audit actual UI functions of Better Log, Log After Def Error,
  and Mod Options Sort/search. Existing UI rules supersede original tab placement. Do not infer an
  actual bottom tab from a mod's title.

For every selected source, the subsequent analysis must record its actual UI entry points and apply
[UIPlacement.md](../UIPlacement.md). A pawn, bill, zone or building choice remains gameplay state,
not an inherited setting. Genuine configuration uses the common global-default/per-save model.
At that later stage, write observable scenarios, source contracts, coverage/deviations and patch
target specs before tests and implementation. Nothing in this transcript checks off those gates.
