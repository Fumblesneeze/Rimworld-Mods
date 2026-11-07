# S055 — Meals On Wheels - Continued research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin replacement P1-48. Inspected2026-11-03. Source analysis and observable draft complete;
independent review/strict checks passed on2026-11-04. No gameplay implementation or acceptance.

## Documentation and identities

Read [Continued Workshop3538082807](https://steamcommunity.com/sharedfiles/filedetails/?id=3538082807),
[original1323773476](https://steamcommunity.com/sharedfiles/filedetails/?id=1323773476) and
[official repository/README](https://github.com/MemeGoddess/RimWorld-MealsOnWheels) first. Intent is
fallback meals from other pawns when ordinary sources fail; the description acknowledges native
animal food access since1.3. Advertised save safety and Smarter Food Selection compatibility are
claims, not our acceptance. Comments mention PUAH job loops and a maintainer note about debug builds;
these are research leads, not reproduced bugs or proof about the user's newer hauling references.

Installed `F:/Steam/steamapps/workshop/content/294100/3538082807`, package
`Memegoddess.MealsOnWheels`, author metadata Uuugggg, maintained by Meme Goddess. Supports1.1–1.7;
LoadFolders selects root+1.7, requires Harmony. Assembly/file1.0.0.0. MIT copyright2025 Alex Tearse-Doyle
in LICENSE.txt (assembly copyright2018); retain notice for any later reuse. No code/art/binary shipped.

| Input | SHA256 / revision |
| --- | --- |
| Installed About.xml | `11E7D8E21FA77E723625879468447ECA3527D2613E825DF2F4C44C72F37F5039` |
| Installed1.7 Meals_On_Wheels.dll | `44BE131F7D879C1BA1B4574575D5EFFE334E4D46D333137C78EC821ADF335BD9` |
| Clean official source revision | `6c39427568029f62562c495d8e65a1a92e0f3f99` |
| Repository1.7 DLL, different DEBUG build | `E592D04C3761FA84B6563375448B574C4C69166DC7D2AB47EBB90933E462F691` |
| Native Assembly-CSharp,1.7.4998rev1299 | `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |
| Installed ImmersiveChefs1.6 consumer DLL | `0937C7721341DDA4B925A75B3107A0FD1AC0ECF404F9FF48E1CB408BB240E147` |

Artifacts retained under ignored `artifacts/Refinement/megamod-spec-20261103/meals-on-wheels/`:
`source`, `compiled`, `source-compiled`, `native`, `consumer-immersivechefs`. All five included
source C# files and the project were read fully: FoodGrabbing, Mod, Settings, Log, AssemblyInfo.
Installed and official source trees match under git's text comparison. All selected installed
decompiled classes were read; repository DLL comparison exposes DEBUG calls and global Harmony.DEBUG
that are absent from installed1.7 behavior. Both have the same food-selection logic. Project net10.0,
Krafs1.7 prerelease floating reference, Harmony2.*, no upstream build run. Root source README is brief.

The game installation advanced from the older1.7.4995 reference during this work. Current native
files below were re-decompiled against1.7.4998/hash above; FoodUtility, JobDriver_Ingest and Toils_Haul
text comparison against retained older files showed no changes. Older S054 evidence remains identified
as its own inspected build; neither source equality nor a version label proves effective runtime support.

## Exact source behavior

One Priority.Low(200) postfix on FoodUtility.TryFindBestFoodSourceFor. It preserves native true and
returns unless eater.IsFreeColonist, both inventory flags and getter tool use/manipulation pass.
It uses eater.Map's spawned player-faction pawns without a null/same-map guard, excluding getter,
empty inventories, holder currently JobDriver_FeedBaby, forbidden holder position, inability to reach
OnCell at Danger.Some and CanReserve(holder) unless ignoreReservations is true.

First pass asks native BestFoodInInventory(holder,eater,MealAwful), which defaults to no drugs;
only that returned item is tested for fresh rot stage and strictly<30000ticks to rot. Second pass
asks for DesperateOnly..MealLavish with drugs permitted for non-teetotalers. Each pass returns the
first qualifying holder in pawn-list order; it does not rank all food/holders by distance. Native
BestFoodInInventory itself returns the first qualifying inventory item, not the best by score.
The postfix returns the physical held Thing and its final ingestible Def and sets success.

It does not receive/honor several original request flags: desperation, allowCorpse/allowForbidden/
allowSociallyImproper/allowVenerated, minimum preference or requested-count calculation. Its native
inventory helper supplies some policy/title/ingredient checks, but these omissions are not blanket
permission. No direct meal transfer or job is added for pawn results: native callers must handle it.

After both pawn passes fail, OdysseyActive enables player-faction PassengerShuttle enumeration.
It reads transporter.innerContainer (nullable but Count used unguarded), finds the first nutrition-
giving/IngestibleNow/WillEat item in preference range, excluding drugs for teetotalers and testing
stack nutrition>=0. It calls TryDrop(food,Near,1,out_) immediately during the search, ignores success
and returned object, then returns the original food reference as success. No shuttle access/launch/
forbidden/reservation check. Native split/drop evidence below demonstrates why returned identity can
be wrong; no player failure was reproduced.

Settings is dead sample scaffolding: Mod's GetSettings, UI override and SettingsCategory are commented
out. Settings.setting has implicitfalse versus Scribe fallbacktrue, but no live settings path exists.
Do not invent an actual toggle/default from it. One unused English key TD.MealsOnWheels. No bottom
tab, entity Def, custom job or source save data. DebugLog calls are conditional; installed constructor
does not enable Harmony.DEBUG, whereas repository DLL does. No per-search/global debug copying.

## Native food and transfer contracts

Read complete current JobGiver_GetFood, JobDriver_TakeFromOtherInventory, Pawn_FoodRestrictionTracker;
read relevant full FoodUtility methods, JobDriver_Ingest and TakeFromOtherInventory toil. Read
ThingOwner.TryDrop count overloads and targeted shuttle types/methods below.

- TryFindBestFoodSourceFor has17 parameters including all food permission flags. It already searches
  own inventory, map food, nearest reachable colony-animal inventory and lower-preference fallback.
  Successful source choice is preserved. Native pack-animal search itself disallows nutritious drugs.
- BestFoodInInventory checks nutrition-giving/IngestibleNow, WillEat(eater,holder), preference bounds,
  drugs and minimum stack nutrition, then returns the first match. WillEat checks actual ingredients,
  respected policy, baby eligibility, race suitability, veneration and title exceptions. The food
  tracker can disregard policy in mental states; owned behavior must retain current respected policy,
  not impose a separate immutable blanket diet.
- JobGiver_GetFood requests inventory+animal access, checks an eating spot, computes nutrition and
  WillIngestStackCountOf. Another Pawn_InventoryTracker produces TakeFromOtherInventory(Afood,Bholder)
  with that count. Otherwise it produces Ingest. Failed search can fall through to permitted Odyssey
  fishing. Our fallback must not erase ordinary no-food behavior for a candidate that cannot be used.
- TakeFromOtherInventory has no actual pre-toil reservation. It moves to holderTouch, fails if food
  changes holder or holder is forbidden, then transfers into eater inventory. Transfer checks current
  containment and accepted result; it does not itself ingest or queue eating. JobDef allows opportunistic
  prefixes. Final acceptance must prove subsequent ingestion and no hauling/refill loop.
- JobDriver_Ingest distinguishes own inventory versus ground, takes real food into hands, finds a
  table/spot, chews and finalizes actual ingestion. Current-use protection must survive that path.
- ThingOwner.TryDrop count can split a new object; on failed drop it merges the split back. Whole-stack
  success removes original from holder; output can be the actual placement/merge result. Ignoring the
  boolean and out result cannot establish a usable food source.

## Passenger-shuttle content and lifecycle

Read Odyssey Buildings_Misc.xml PassengerShuttle and Ship_PassengerShuttle definitions. It is a3×5
Building_PassengerShuttle with interaction offset(2,0,0), CompTransporter/CompLaunchable/CompShuttle,
ITab_ContentsTransporter and playerShuttle=true. Read Building_PassengerShuttle fully; CompTransporter
loading flags/cancel/cleanup/removed notifications, CompLaunchable launch transaction, CompShuttle
spawn/player/loading/auto-load/tick paths, TransportShip Waiting/LeavingSoonAutomatically and targeted
contents-tab drop methods. This is targeted reading, not every member of the large transporter classes.

Contents UI uses the rotated interaction offset for drops and notifies removal (mass cache). Generic
contents drop uses SplitOff+GenDrop, so reuse the behavior boundary with checked failure/result rather
than copying unchecked UI code. LoadingInProgressOrReadyToLaunch is groupID>=0 or caravan state;
completed loading still protects cargo. CancelLoad/CleanUpLoadingVars removes load ownership, drops
all contents and clears loading lists: not a meal-retrieval shortcut. Waiting includes null curJob;
LeavingSoonAutomatically inspects current/queued FlyAway. Spawned/waiting alone is insufficient.
Launch moves contents into an ActiveTransporter and changes actual holder, recording launch tick.

The draft explicitly restricts shuttle borrowing to reachable idle/uncommitted cargo, excludes all
contents under an active/ready loading plan and never cancels it. Effective access/recovery and a
naturally reachable idle-cargo fixture remain required before admission/acceptance. The attempted
lookup CompLaunchable_Shuttle found no such type; the actual Def uses generic CompLaunchable.

## Consumers and direct composition

Bounded successful installed DLL scans (ASCII names and UTF16LE reflection strings) found seven own
version DLLs and **one external consumer**, installed ImmersiveChefs1.6. Read its local source
FoodSearchCompatibilityAdapters MealsOnWheels classes, integration catalog/bootstrap and selected
compiled adapter. It gates canonical Memegoddess.MealsOnWheels, checks Meals_On_Wheels assembly,
FoodGrabbing.Postfix eight-parameter static shape, actual source Harmony owner and low priority on
the17-parameter food target. It validates/logs coexistence and leaves source selection authoritative;
no other Enabled reader was found in the current source scan. A facade alone cannot satisfy the
package/owner contract. Do not spoof or add a repository-mod dependency; any consumer change needs
its own owner. Ordinary independent coexistence and beta support remain unverified.

Successful XML/CS/JSON name searches additionally found RimPy's old package catalog and Use This
Instead's original→Continued replacement mapping, not executable API consumers. Historical original
identity `Uuugggg.MealsOnWheels` is supported by that mapping; no original DLL was inspected. The
public original/Continued pages point at each other. No generic third-party compatibility claim.

S021 cargo/kept attribution and capacity, S024 food preference/interruption, S041 tainted portions
and S033 shared holder facts compose directly. General keep protects unloading, not all ordinary
food use. Explicit provider claims and active feeding still protect quantities. S123 routine meal
carrying and S133/S135 transport loading must use these same actual portions; no patch stack against
our own code. S057 preserved-food preference remains next relevant food-source comparison.

## Selected deviations and remaining work

The [draft](../../../../openspec/changes/specify-megamod-shared-carried-food/) selects read-only discovery,
normal self-feeding scope, complete candidate inspection, near-spoilage class plus score/distance
ranking, actual current food permissions, positive nutrition, capacity-limited real transfer,
current-use protection and checked shuttle retrieval. It does not claim those corrections are source
parity. No food quality/nutrition/spoilage/work-rate/content change is introduced. Constant12hours is
source-derived and documented, not an inert setting. Actual pawn food policy and keep choices remain
gameplay state. Existing source packages suppress the overlapping owned group with a warning.

MW-Q1 effective lifecycle/claims; MW-Q2 admitted shuttle/provider/consumer boundaries; MW-Q3 finalized
ranking/selected-source composition and shared-data measurement; MW-Q4 implementation/language/review/
live acceptance. No game, build/install, user configuration or Workshop content changed. All runtime
tasks remain unchecked; source and static risks are not accepted gameplay evidence.
