# S028 — Replace Stuff research

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Intake **P1-22**, researched **2026-11-02**. Status: source analysis for a documentation-only draft;
no runtime behavior, provider compatibility or performance accepted.

## Evidence and order

Read the [Steam description](https://steamcommunity.com/sharedfiles/filedetails/?id=3526354009),
[changelog](https://steamcommunity.com/sharedfiles/filedetails/changelog/3526354009) and linked
[public repository](https://github.com/MemeGoddess/RimWorld-ReplaceStuff) first. Then inspected the
installed metadata, all 52 C# source files, current version's 28 Def XML files, inherited/root patches,
settings/save paths, installed assembly, selected exact native methods and actual consumer bindings.
The README is only a brief description. Steam establishes the intended conveniences; the detailed
inventory below comes from source/decompilation, not an assumption that the description is exact.

| Evidence | Pinned identity |
| --- | --- |
| Installed source | `F:/Steam/steamapps/workshop/content/294100/3526354009`, package `Memegoddess.ReplaceStuff`, author Uuugggg |
| About SHA-256 | `1BFEFAE3F2A0AB007408AED271490DDC82D682C4442C6BE07DDCBA49E7AD139C` |
| Installed `1.7/Assemblies/Replace_Stuff.dll` SHA-256 | `18DC66FD3721C5BB879304EA919E8BA9B43E0060F0DF404E0FF6A7DF2A1221A3` |
| Installed metadata | Release; assembly/file 1.7.0.0; informational `1.7+11e33b712e62cc1820b5196ee5581db522bc69a3` |
| Public checkout | `11e33b712e62cc1820b5196ee5581db522bc69a3`, 2026-11-02; all 52 installed C# source files match after newline normalization |
| Public committed DLL | SHA-256 `4698AD8ED0463C2E48D8B5CF92119FE61AA483332E8B803C14805210BEA614A2`; Debug; informational `1.7+0ae10c09bc79bf7c4f99328b67f778de0d929d0d` |
| Native game | typed installation inspection: 1.7.4995 rev309, net10.0; `F:/Steam/steamapps/common/RimWorld/Assembly-CSharp.dll` |
| Native assembly SHA-256 | `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21` |

The public and installed binaries **differ**. Both were decompiled to 109 C# files; 58 files differ,
including debug/compiler shape and metadata. Critical replacement-frame and dropdown paths were
checked against the installed decompilation. This is not a reproducible-build claim or a claim that
every differing method was proven equivalent. S013's earlier DLL fingerprint is historical evidence,
not the current snapshot. The MIT-style license names **[2025] [Alex Tearse-Doyle]**; retain notices
for future substantial licensed reuse. No upstream build was executed or assembly copied into a mod.

Local evidence: `artifacts/Refinement/megamod-spec-20261102/replace-stuff/` contains `upstream`,
`installed-decompiled`, `upstream-decompiled`, `game`, `consumers`, `consumer-paths.txt` and
`consumer-utf16-paths.txt`. These are ignored research artifacts. The typed operation registry has
live method decompilation, but no offline equivalent; bounded local ILSpy inspection launched no game.

Native `ModContentPack` confirms later version folders override equal relative paths. Thus 1.7 Defs
and assembly take precedence; unique root/1.5 files still participate. The 1.5 patch adds Autodoor to
Door's related commands. Source metadata requires Harmony, supports 1.1–1.7 and declares incompatibility
with `solaris.furniturebase` and `gloomy.furniture.continued`; these are upstream declarations, not
verified compatibility outcomes for the owned version. November changelog entries address many-frame
and particle costs and non-buildable bridge selection; no performance measurement is claimed here.

## Material replacement: source facts

`DesignatorReplaceStuff`, `GenReplace`, `InterceptBlueprint`, `ReplaceFrame`, `DisableThings`,
`ReserveSharing` and attachment/frame patches establish the following:

- Structure-menu Replace Stuff defaults to wood; material choices come from map resource/Thing data.
  A drag selects eligible player or neutral artificial Stuff buildings/plans, not enemy buildings.
  Neutral targets are claimed. Building the same definition/rotation with different Stuff also
  starts replacement. Terrain/material legality and an existing different-type upgrade constrain it.
- Built buildings remain the same object. Generated replacement frames track `oldThing` and `oldStuff`;
  generated Def names end in `_ReplaceStuff`. Ordinary blueprints/frames are canceled and replanned
  for new material; their construction work does not carry over. Retargeting a material replacement
  retains only completed removal work, refunds delivered inputs and discards assembly work. Selecting
  original material cancels; duplicate identical orders are rejected.
- New input is only the Stuff portion: at least one unit, rounded from building Stuff count divided
  by material volume. Fixed components are neither charged nor returned. Old Stuff returns through
  the native deconstruction calculator. Work is new-material construction work plus old-material
  work capped at **3000**. Source omits native deconstruction's **20** minimum.
- Successful completion changes Stuff, invalidates stats, restores full HP, refreshes appearance and
  rerolls quality using the completing builder. It consumes frame contents and returns old Stuff
  once. Same-object bills, bed ownership and other resident state survive; this does not establish
  that every external provider observes the material change.
- Construction failure after the removal phase retains that phase and applies native frame failure
  to delivered replacement inputs. Failure during removal does not consume them. Native failure
  returns approximately half using its rounding/minimum rules, rather than a guaranteed exact half
  or loss of the old building. Notification threshold is 1400 replacement work.
- Cancel returns actual supplied inputs, not fabricated full costs. If the old object disappears,
  the frame drops inputs and cancels. A drag's source cost tally can count a multi-cell object more
  than once. Its declared cost cache does not insert new values; no optimization claim follows.
- After positive construction work, workbenches stop accepting bills, turrets stop beginning attacks
  and the rest job does not choose a replacing bed. This is not a blanket building shutdown, does
  not prove occupied-bed handling, and does not forbid the wall continuing to seal the room.

Source reserves a replacement frame through its old target across many reservation-manager methods.
It globally suppresses the ordinary deconstruct-for-blueprint work giver and broadly changes frame
blocking. Those scopes are not adopted; owned changes must only affect their admitted orders.
The god-mode branch has no costs/work; it belongs to developer controls, with no normal-play bypass.

## Different-building upgrades and automatic rebuilding

`NewThingFrame`, `DeconstructOriginal`, `DeconstructWork`, `TransferSettings` and the four
`DestroyedRestore` files describe two different mechanisms:

| Workflow | Source result / limitation |
| --- | --- |
| Upgrade | Native matching replacement tags or registered category predicates admit a different building. Full new costs plus old removal work; old building remains until completion, then deconstruct returns replace a full refund/minification. |
| Family rules | Walls/place-over walls/fences/doors; coolers; constructed beds; Fences designation category; tables; exact reflected refrigerator class; plant-selectable buildings; batteries; watermill/wind/geyser families. Native tags add chairs/conduits and other current relationships. |
| Generic upgrade transfer | Worktable bill objects and storage rules; storage copy delayed one tick. Cached-pair existence, rather than its true value, gates generic copying. |
| Family transfer | Cooler temperature; bed prisoner/medical status and owners; reflected fridge desired temperature; selected crop. Batteries have **no charge-copy action**. |
| Footprint handling | First matching old Thing found across the footprint; no explicit multi-target decision. Cached weak references can become stale. Free-build bed targets excluded, but not every zero-work special building. |
| Auto-rebuild start | Only an actual native automatic rebuild blueprint: enabled automatic rebuilding, destruction mode, player/home/research/placement requirements remain native. Deliberate deconstruction is not resurrection. |
| Auto-rebuild memory | Deep-saves old building keyed by cell; exact-type handlers restore worktable bills, heater/cooler temperature, plant choice and storage settings. Bed ownership, fuel, battery charge and arbitrary components are not promised. |
| Auto-rebuild retirement | Cancel/removal purges memory; failed frame replacement retains it. Saving purges cells without a plan. Source cell association risks applying old state to an unrelated later building. |

Source uses a static spawn-context flag to alter deconstruction and a global next-tick action queue
for delayed copies. Reentrancy, exceptions, save timing and another game instance are unproved.
Owned design uses explicit order identity and lossless handoff; it does not preserve these mechanisms.
Native 1.7 Frame already carries storage-group/settings state, consumes actual inputs, creates quality,
scales completed HP by frame HP and copies style/glow choices. An upgrade must compose with those
paths; material replacement's full repair is a distinct source behavior. Native GenLeaving uses the
building's deconstruction fraction (default **0.5**, not an unconditional description-derived 75%).

## Planning through rock, fog and support terrain

- Bottom-right `blueprintOverRock` starts true and is saved with play settings; it also controls
  fogged positive-work building plans. No new bottom tab. Source static toggle/save switching is
  not proof of correct owned save isolation.
- Unsmoothed mineable rock can be planned over. Placement adds mining unless an existing mine or
  suitable smooth-wall designation already supplies the intended outcome. Builders only mine when
  native mining capability/work eligibility allows; normal ore yield applies. Frames/resources can
  exist under rock, but final building cannot remain inside it.
- A compatible designated smoothed wall is completed instead of unnecessarily mining it. Corner
  touch for construction/mining does not grant movement through walls. Mined products are intended
  to stay in the miner's room; source's composed-validator closure can recurse into itself.
- Fog plans remain selectable and visible without showing hidden terrain. Source uses an early
  placement return and rechecks only the first blueprint when a cell is revealed; it calls
  `Find.CurrentMap` from the fog path. Full-footprint, correct-map and multiple-plan checks are
  required corrections. Fog does not authorize construction through a revealed invalid blocker.
- Pawns passing through no longer block completion. Scope must preserve pawn safety, meaningful
  reachability and unrelated construction; no entombing, deleting or teleporting a passer-by.
- Bridge discovery considers removable player-buildable terrain with blueprints and useful support
  affordances; excludes ordinary floors, grow-soil/diggable conversions and Odyssey substructure.
  It does not recursively build arbitrary terrain chains. Existing support plans are reused.
- Source preference follows its terrain list and completed research. It tests only the **first**
  ingredient: first candidate with more than **10 times** that ingredient's one-tile cost wins;
  otherwise last candidate with any of that resource, otherwise first researched candidate even
  without stock. Free-cost bridges win immediately. This is not full-footprint affordability.
- Support plans are created before top building/install plans; support frames must finish first.
  Cost previews add bridge material separately. Canceling/removing support cancels dependent plans
  except selected ones in source, an accidental selection-sensitive exception not adopted.

## Wall coolers and vents; UI and settings

Four Defs: `Cooler_Over`, `Cooler_Over2W`, `Vent_Over`, `Vent_Over2W`; dropdowns
`OverWallCooler` and `OverWallVent`. The wide footprints are **1×2**, not doubled output.
Current XML inherits native cooler/vent construction costs and thermal behavior. Inspected Core
anchors: cooler **90 steel + 3 components / 1600 work / HP100 / skill5 / Air conditioning / 200W**;
vent **30 steel / 400 work / HP100 / Complex furniture / no electricity**. Wide variants keep the
same costs/work. These are source/XML anchors, not measured finalized modded Def values.

Coolers coexist with walls, are not an edifice and do not form an airtight seal themselves. Wide
cooler north/hot port is shifted an extra cell; cold port remains on the opposite exterior side.
Source vents retain native edifice/airtight behavior and use native heat/gas/vacuum rules. Native
`GenTemperature.EqualizeTemperaturesThroughBuilding` walks the occupied footprint, so a wide vent
does not require the cooler's offset patch. Blocked-port placement checks are bypassed for ordinary
coolers/vents too; blockage can still prevent useful operation. No explicit backing-wall placement
requirement was found. The draft retains freestanding placement, with correct sealing/blockage
feedback, rather than inventing an enforced mounting rule from the name.

Source global settings: hide wall devices **false**, hide ordinary coolers/vents **false**, bridge
preference order. Own version uses common global defaults/save overrides, with all save editors and
Use global disabled at the main menu; building temperature/flick state remains entity state. Hide
only affects build offers. Source globally sets all dropdown orders to 20; this unrelated UI rewrite
is not retained. Its current sanity fix only unpacks source wall-device dropdowns when Stuff choices
need it, skipping `cedaro.material.submenu`. Root/1.5/1.7 related-command additions need guarded,
idempotent merging, never wholesale replacement of another mod's lists.

## Compatibility catalog and actual consumers

The [exact XML catalog](2026-11-02-replace-stuff-catalog.md) records **117 lists across 27 files**,
plus the separate device file. It is an evidence inventory, not a blanket compatibility promise.
Native/category families are broader than those lists. `InterchangeableItems` merges replacement
tags and resolves callback names; missing callbacks are silently omitted. Armour Racks, RimBees and
VFE Security use deliberately disabled package IDs. The Security lists lack category/callback and
therefore add no rule. RimBees reuses the ArmourRacks Def name. SoS2's solar generators are tagged
as walls, and its declared vent-state callbacks are absent from this source assembly. Those mistakes
must not silently become owned specifications or claims of preserved external state.

Workshop ASCII source/XML/binary scan found 134 paths; a separate UTF-16 metadata-string scan found
25 DLLs, revealing reflection consumers missed by ASCII. The scans are bounded identity discovery,
not a complete proof of all installed or future consumers. YART harness lists and metadata-only
mentions in Ancient urban ruins/Worldbuilder are not callable contracts. Read-only decompilation:

| Installed consumer / inspected version | Actual binding | Owned disposition |
| --- | --- | --- |
| QualityBuilder `hatti.qualitybuilder` | See S013: source frame/type queries and source detection; Replace Stuff also discovers QB comps/designation by unqualified type names | Direct S013 composition; preserve finite attempt budget and target/worker policy. No internal compatibility patch. |
| NANAME Floors `oels.nanamefloors`, 1.7 | New-source package gate, reflected static `List<TerrainDef> allBridgeTerrains` on `Replace_Stuff.PlaceBridges.BridgelikeTerrain`; `BlendedTerrainUtil` checks base-terrain membership and appends its newly generated blended terrain | Real read/write catalog consumer; own package cannot activate its gate. A verified registration/classification adapter or provider update is needed; merely returning a snapshot is insufficient. No alias claim. |
| Poured Concrete `Aelanna.PouredConcrete`, 1.7 | Gate new source or `DEBUuugggg.ReplaceStuff`; postfix `Replace_Stuff.ReplaceFrame.FinalizeReplace(Thing thing, ThingDef stuff, Pawn worker)` to register/deregister curing | Material-change completion notification and actual curing state need explicit adapter; package gate remains unmet without it. |
| Vanilla Gravship Expanded Ch1 `vanillaexpanded.gravship`, 1.7 | New/old source package gates; reflects `NewThingReplacement.replacements`, nested Replacement and private newCheck/oldCheck predicates; excludes zero-work vacuum checkpoints | Preserve exclusion as direct placement safety; do not impersonate the mutable source registry. |
| VGE Ch2 `vanillaexpanded.gravship2`, 1.7 | New-source package gate; postfix `BridgelikeTerrain.FindBridgeFor`, veto substructure or VGE Designer terrain | Optional guarded support-terrain exclusion; no facade can alone bypass package gate. |
| More grouped buildings `WSP.GroupedBuildings`, installed MaterialSubMenu.dll | Embedded Material Sub-Menu Harmony code uses old **display name**, then suppresses source `DesignatorBuildDropdownStuffFix.SanityCheck` | Do not rewrite unrelated menus or foreign patches. Embedded assembly is not package `cedaro.material.submenu`; source skip gate misses that identity. |
| Basic Dropdowns Replace Stuff Patch `cruesoe.basicdropdownsreplacestuffpatch`, 1.6 | Hard dependencies on source and Basic Dropdowns; shape-checks source SanityCheck then replaces it with only-wall-device unpacking | Source current fix already mirrors this narrow behavior. No owned self-patch or fake dependency satisfaction; add-on is not needed for owned UI. |
| VFE Architect `VanillaExpanded.VFEArchitect`, 1.4 | Old display-name gate; reflects ReplaceFrame.CountStuffHas, TotalStuffNeeded, MaterialsNeeded to substitute billed resource for special Stuff | Historical consumed shape; current source has TotalMaterialCost instead of declared MaterialsNeeded. Do not claim 1.7 support from this 1.4 assembly. |
| Wall Light `Murmur.WallLight`, 1.4 | Compatibility patch orders native spawn/placement postfixes after source Harmony owner | Ordering dependency, not a source API call; current native wall lights are a separate baseline. No owner spoof. |
| Research Reinvented `PeteTimesSix.ResearchReinvented`, v1.6 | Before-source Harmony ordering on Frame.CompleteConstruction for prototype work | Actual ordering consumer, not a Replace Stuff API. Prototype/research costs must survive admitted effective completion flow; no 1.7 claim from v1.6 evidence. |

Source-generated Defs, saved frame class/fields, destroyed-building deep records and settings keys
are additional persistence bindings. Retaining public names does not make old saves load safely.
No automatic importer, safe-removal guarantee, universal callback registry or package identity spoof
is proposed. Original active: withhold overlapping new owned work, retain lossless recovery of owned
orders/devices and log reason. Never unpatch foreign owners or mutate the active mod list.

## Proposed corrections and outstanding questions

These are explicitly drafted defaults, not additional user answers. Existing-mod behavior was
researched instead of asking the user to explain it.

| ID | Remaining choice or evidence | Draft position / boundary |
| --- | --- | --- |
| RS-Q1 | Replacement balance/UX and overlapping footprints | Use native removal minimum20/maximum3000; charge only actual material change, full costs for upgrade; ambiguous multiple old buildings rejected with feedback. Source full material repair/quality reroll retained. |
| RS-Q2 | Bridge selection and device niche | Prefer first legal researched bridge whose **full planned support cost** is available, otherwise first legal researched preference with shortage shown; no 10-tile stock heuristic. Wide devices retain same cost/output. Freestanding placement remains allowed; blocked ports still matter. Validate playability, not new magic constants. |
| RS-Q3 | Native/provider state transitions | Occupied beds, failed swap, partial bills/storage, attachments, gas/vacuum, dynamic costs, minify/gravship, concrete curing and exact provider adapters need implementation-time shape and live evidence. No generic copy of arbitrary state; unresolved unsafe upgrade is unavailable. |
| RS-Q4 | Catalog and legacy support | Validate each active optional provider's exact content/state, disabled historical groups and suspicious SoS2/RimEffect tags before admitting it; unsupported original save import and package-gated bindings are explicit. No blanket source parity. |
| RS-Q5 | Implementation and acceptance | Individual target specs/executable guards, focused RED/GREEN, seven languages, asset workflow, reviewed-build native action/result evidence and shared-data cost review all remain. |

Semantic balance pass: replacement saves fixed components and keeps enclosure, but costs labor/new
Stuff and rerolls quality. Upgrade consumes a complete new building and returns only native old
deconstruction materials, never battery charge/fuel as free new supplies. Research/skill/prototype
rules remain effective. Two-cell devices buy reach through a thicker wall, not doubled output.
Rock/fog/bridge conveniences do not create ore, bridge materials or free construction. Exact final
Def comparisons and all native acceptance remain unchecked implementation work.

## Draft review resolutions

Independent document review found four contract gaps: ordinary native rebuilding must start S013's
fresh finite quality run rather than retaining an unrelated destroyed building's exhaustion;
structural recipe loss blocks new linked work group-wide under S014; an installed item's existing
configuration/HP/quality takes precedence and conflicting groups refuse install-over; and bridge
plans/costs must not disclose fogged terrain. All are made explicit in the draft. S013's material
wording is clarified to allow the single S028 completion result without a duplicate quality roll or
budget reset. Host follow-up also traced NANAME's **mutation** of the bridge list, not just its read.
Focused resolution review and final validation status belong in the progress ledger. No runtime
acceptance follows from resolving a documentation finding.
