# Trading Spot research — 2026-11-03

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
S039/P1-31. Complete installed-source analysis and selected native contracts, not runtime acceptance.
S040 separately owns the user's dedicated fixed trading area and its contextual controls.

## Documentation, identity and evidence

Read the [Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=3507108484),
the linked [maintainer repository README](https://github.com/sam2332/rimworld-tradingspot), installed
README/About, two source files, one ThingDef and English keyed text before inspecting compiled/native
details. Workshop broadly describes where traders arrive; the actual code changes an existing
party's gathering destination after spawning. The source does not choose the map-entry cell.
README describes optional visitors, one marker per map and configurable work. Its broad safety and
compatibility claims are documentation, not verified facts. Workshop changelog retrieval failed;
user-reported missing traders/frozen pawns are unverified reports, not reproduced bugs here.

| Evidence | Identity |
| --- | --- |
| Installed root | `F:/Steam/steamapps/workshop/content/294100/3507108484` |
| About | [PL] Trading Spot, Programmer-Lily, `tradingpost.programmerlily.com`, modVersion **1.2.0**; supports 1.0/1.1/1.2/1.3/1.5/1.6, not 1.7 |
| Assembly | Root `Assemblies/TradingSpot.dll`, 8704 bytes, assembly version **0.0.0.0** |
| Assembly SHA-256 | `122599599E606E2D0CB9B11F040A53B2BC0839334DF545081C2A6A2EAA761778` |
| Source/TradingSpot.cs SHA-256 | `3D83F2643269B3C0933EEF38465D74340842814022D76BBAED359744A36C1138` |
| Source/Settings.cs SHA-256 | `34FF31D57473F517D6422FA1977B7768E62410443595D505DE695DC6DB5D587C` |
| ThingDef XML SHA-256 | `31E20777990C4398F2FBA8C348EC1BD0968BB023008F1ABFECB00CDE6F61D9E8` |
| About SHA-256 | `29702A9682C0A8C9F0B717C6D9D2D1604E8C90E80A18A0FEBC906DB0ABBCF4B6` |
| License | No specific license established from installed tree or inspected public README, which only says to check the repository; credits Kiame Vivacity and translators. No permission to copy source/assets is inferred |
| Native reference | Installed Assembly-CSharp, **1.7.4995 rev309**, SHA-256 `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21` |

Read-only decompilation uses ILSpy **11.2.0.10050**, retained under
`artifacts/Refinement/megamod-spec-20261103/trading-spot/compiled` and `native`.
All **six compiled runtime types** were read: TradingSpot, WorldComp, Settings, SettingsController,
Main and DefOf. They agree with the two installed source files on the behavior below. No declared
Harmony patch class or method exists, despite startup calling PatchAll. No LoadFolders file or
version-specific runtime assembly was found. The extra `Source/obj/Release/TradingSpot.dll` is build
output, not another selected game runtime. The project's net472/build script paths are historical
build metadata; none was executed. No mod/game process was launched or installed content modified.

## Complete installed behavior

### Physical marker

The root ThingDef `TradingSpot` inherits BuildingBase, class `TradingSpot.TradingSpot`. It uses the
vanilla PartySpot graphic, FloorEmplacement layer, standable placement, Normal ticker, Misc category
and Misc12 hotkey. It allows zone overlap, does not clear the area, is inert/non-edifice, has no
hit points, flammability 0 and beauty 0. It disallows placement over impassable plants and uses
SupportPlantsOnly; no own research prerequisite or cost list is declared. WorkToBuild is 10 in XML.
No resources are left when killed or refunded when deconstructed. Size 1×1 is stated in README;
the source XML does not explicitly override size. No new bottom tab or bespoke marker gizmos exist;
native building controls apply.

The **constructor**, before a spawned-map context is established, reads `Current.Game.CurrentMap`.
It finds the first player building whose defName is TradingSpot, destroys it (Vanish in supplied
source), displays a negative message and stops. It does not check `Current.Game` for null, use the
new object's eventual Map, inspect pending blueprints or enforce a transactional replacement.
Thus the intended one-per-map rule has current-view/cross-map/deserialization risks; exact outcomes
require a live reproduction and are not claimed here. Do not copy this constructor side effect.

### Party redirection

Every Tick increments `count`; when `count % 60 == 1` it resets count to 0 and processes every map
Lord. Starting from 0, this condition runs **every tick**, not every 60 ticks. Reflection is repeated
per eligible party: obtain private instance `chillSpot` on the runtime LordJob type and set it to
the marker's Position without a field/null/semantic guard.

Eligibility is `RimWorld.LordJob_TradeWithColony` or CheckVisitor. CheckVisitor accepts native
`RimWorld.LordJob_VisitColony` only when the visitor option is on and the cached Hospitality flag
is false. It does not identify arbitrary trade inventory, world caravans, orbital trade or every
foreign visitor system. There is no explicit friendly/phase/reachability/safety/animal/group-size
test in the source loop.

For a current `LordToil_Travel`, change destination and UpdateAllDuties only when FlagLoc differs.
For a current `LordToil_DefendPoint` (including derived caravan defend toils), change the defend
point and update duties similarly. Other current toils receive no duty update, though chillSpot is
still overwritten. No explicit job interrupt is issued by this code; duty changes may affect future
job selection. It does not reset the Lord graph, generate a trader, alter stock or change prices.
It does not constrain individual wandering to an area; ordinary point radii/roles remain.

The source neither restores the previous destination on removal/option-off nor retains a baseline
for that restoration. A changed field can be serialized by native LordJob persistence after the
marker is gone. Stopping redirection therefore does not guarantee a return to the old gathering spot.

### Settings and lifecycle

| Setting | Source default/storage | Effect |
| --- | --- | --- |
| VisitorsGoToTradeSpot | true; global key `TradingSpot.VisitorsGotoTradeSpot` (different capitalization/spelling from UI key) | Read dynamically for ordinary visitors during each loop; trade caravans always included |
| RequiresWorkToPlace | true; global key `TradingSpot.RequiresWorkToPlace` | Mutates shared WorkToBuild to 10 when on, 0 when off |

SettingsController displays two checkboxes in a fixed 300×100 listing; SettingsCategory returns
literal `TradingSpot`. ApplyWorkSetting runs after WriteSettings, via startup ExecuteWhenFinished
and WorldComp.FinalizeInit. It changes an existing WorkToBuild stat entry only, without adding a
missing one, and can overwrite another provider's XML work-value choice. No per-save settings,
Use global control or setting-specific migration exists. WorldComp adds no serialized custom state.
Marker position survives as native building data; source count and Hospitality flag are not saved.

Main creates Harmony owner `com.tradingspot.rimworld.mod`, calls PatchAll and schedules work setting.
The inspected six-type assembly contains **zero actual Harmony patches**. Required source Harmony
metadata does not imply that its current gameplay depends on an installed patch.

English has four keyed labels/messages. Ten language directories include source translations;
German contains both ThingDef and obsolete ThingDefs_Buildings injection folders for the same
label/description. This confirms the file-layout concern, not a reproduced loader error. The owned
seven-language catalog must be authored and verified independently.

## Native 1.7 contracts inspected

Nine native types were decompiled and read: LordJob_TradeWithColony, LordJob_VisitColony,
LordToil_Travel, LordToil_DefendPoint, LordToil_DefendTraderCaravan, LordJob_TravelAndExit,
TraderCaravanUtility, IncidentWorker_TraderCaravanArrival and IncidentWorker_VisitorGroup.

- Trader arrival spawns pawns and stock through ordinary incident work, then selects a colony-near
  gathering cell reachable by all party members and creates the trading Lord. Source redirection
  happens later; changing the destination must not redo creation or stock generation.
- Native LordToil_Travel uses TravelOrLeave duties; its default arrival check is a 10-cell radius
  plus destination reachability, checked every 205 game ticks. These are native reference values,
  not proposed area membership or update guarantees.
- LordToil_DefendPoint defaults defend radius to 28 and applies a pawn-kind-specific radius when
  supplied; wanderRadius is separate. A point destination does not imply confinement to one cell.
- **LordToil_DefendTraderCaravan inherits LordToil_DefendPoint**. The main trader/guards defend;
  carriers Follow the trader within 5 and chattel Escort within 5, walking. It does not allow long
  needs and uses wake threshold 0.5. The source's DefendPoint cast includes this subtype.
- Native trading stages include incoming travel, harmed-party defense, ordinary waiting and several
  exits. Routine waiting lasts a native random 27000–45000 ticks before gift/check and departure;
  dismissal, temperature, weather, game conditions, losses and hostility have distinct transitions.
  Those numbers/transitions must be preserved, not re-randomized by destination edits.
- Native ordinary visitors attach a travel subgraph, DefendPoint, wounded-guest rescue and a separate
  **travel-and-exit subgraph**. Routine duration is the supplied duration or native 8000–22000 ticks.
  Gifts, danger, hostility and rescue have their own transitions. Source matching every Travel toil
  can redirect the departure travel phase; source changing every DefendPoint can also redirect
  defense during harm. These are code-derived risks, not a live defect reproduction.
- Both native LordJobs serialize their `chillSpot`; changing only the field is insufficient to
  update a currently instantiated toil, hence source updating current duties separately. Conversely,
  reconstructing the graph to update its point can reset timers/state and must not be used casually.
- VisitorGroup may convert an ordinary visitor to a small trader after Lord creation; eligibility
  follows the party's actual visit type, not whether one pawn has items to trade.

No claim that the installed source binary is a working 1.7 port follows from these shapes.

## Hospitality and external contracts

The source caches a Hospitality flag by **case-insensitive display name exactly `hospitality`** in
the building constructor. The installed continuation's display name is `Hospitality (Continued)`,
canonical package **`Orion.Hospitality`**, Workshop **3509486825**, About 1.1.8. The name test does
not recognize that title. Its inspected `Hospitality.LordJob_VisitColony` derives directly from
LordJob, not the native RimWorld visitor class, so native-class matching alone still does not prove
redirection of Hospitality's guests. Do not turn the name mismatch into an invented compatibility bug.

Reused S027's read-only Hospitality class evidence after verifying current 1.7 DLL SHA-256
`6C169FED1229D1EE18B478AABFC7CBF23F92DC401A641A33C938E8055B345D56`.
No guest-control adapter is adopted. Native visitors and provider-owned guests must be distinguished
by actual party ownership; package-ID detection and shape admission replace name guesses.

Bounded Workshop/local-Mod text scan found current-source files, Use This Instead replacement
metadata and XML integrations targeting **older packages**:

| Consumer | Actual inspected binding | Implication |
| --- | --- | --- |
| Better Architect Menu, `ferny.BetterArchitect`, Workshop 3563882422 | LoadFolders gates Trading Spot folder on `grizzlemethis.tradingspot.rw`; XML replaces TradingSpot designation category with Ferny_ColonyLocations and WorkToBuild with 0 | Real legacy Def consumer, not proof it activates for the selected PL package; source ApplyWorkSetting could overwrite a work patch if actually co-active |
| Drawn Spots, `overpl.DrawnSpots`, Workshop 2917791361 | 1.7 XML gates texture replacement on `Grizzlemethis.TradingSpot.RW`; separate Trading Control branches, and conditional Barely Better Spots random-graphic changes | Legacy marker appearance contracts; not applicable to an area with no physical TradingSpot ThingDef |
| Use This Instead, Workshop 3396308787 | Redirect metadata to current `tradingpost.programmerlily.com` | Catalogue identity, no callable behavior contract |

An ignore-disabled raw DLL scan for `TradingSpot` found only the selected mod's runtime and obj DLLs;
an earlier scan respecting ignores found nothing and is not used as proof of absent consumers.
Literal scans cannot exclude dynamically constructed/reflected or uninstalled bindings. No consumed
runtime method API was established. Do not create a TradingSpot DLL/class/Def facade to satisfy
unproven users or to preserve marker-only texture/category patches after the user replaced the marker.

## Proposed owned behavior and S040 boundary

The user's dedicated fixed area replaces the physical marker. S039 adopts trader gathering and the
optional ordinary-visitor preference; S040 will specify drawing/editing cells and contextual gizmos,
not allowed-area assignment. No instant spawning, teleporting, stock creation or price change.

Proposed corrections: admit only ordinary incoming/waiting states; preserve emergency/departure
and existing party roles; use reachable/safe candidates; reselect or yield to native gathering when
the destination is unavailable; restore ordinary routing when visitors opt out or the area disappears.
Preserve visit timers, gifts, trader identity, cargo and pending transactions. Map identity comes
from the actual party/map, never CurrentMap. Simultaneous parties keep separate lifecycle state.

S027 couriers may travel outside the gathering area for their finite delivery, then return only
while their party is still visiting; urgent/routine departure follows S027's existing contract.
The area is not a permission boundary and grants no access to doors, stock or other providers' areas.
Idle destination selection inside the painted footprint belongs to S040; pathfinding between valid
destinations may cross outside it, and emergencies/needs/deliveries are not imprisonment.

The ordinary-visitor preference retains default **on**, through common global/save/Use global
controls. Changes apply to eligible parties at their next normal routing update without interrupting
urgent jobs. No construction-work preference survives because drawing an area does not build the
omitted marker. The exact update/invalidation strategy belongs in design; no repeated per-tick
reflection/whole-party scan is accepted as an optimization.

Original PL provider active: owned overlapping redirection is withheld with a visible explanation
and startup warning. Do not delete old markers, unpatch the original, migrate its global settings
or pretend its marker-only consumers bind to the new area. Other destination providers require a
proven cooperative adapter before shared ownership is claimed.

## Open questions and verification boundary

| ID | Remaining research / decision |
| --- | --- |
| TS-Q1 | Exact effective 1.7 state/arrival/wait/exit seams and native fallback restoration, including subclass/foreign transitions; prove semantic admission before any patch or reflective adapter |
| TS-Q2 | S040 area editing, candidate selection and constrained ordinary idle destinations, including blocked/empty/disconnected/crowded areas and real party accessibility |
| TS-Q3 | Optional provider ownership, exact Hospitality/native-visitor distinction, other trading controllers and actual future API consumers; no name-based or universal compatibility claim |
| TS-Q4 | Source license/provenance if reuse is ever proposed, focused load/save/source-present/native delivery workflows and measured update costs; implementation and native acceptance remain open |

Semantic balance pass: ordinary routes, exposure to hazards and travel distance can change, but no
new goods, wealth, prices, trade chance, movement speed, carrying allowance or visit duration is
introduced. Keep visitor choice enabled by source default; preserve emergency escape and party
roles. Unsafe/unreachable areas must yield rather than trap traders for indefinite protection or
repeat their gifts. This is a documentation pass, not measured gameplay acceptance.
