# S073 — Use Bedrolls - Continued research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
Read-only inspection **2026-11-04**. Origin replacement. **Specification draft independently reviewed
and strictly validated; no implementation or acceptance.** Owning change:
[portable beds](../../../../openspec/changes/specify-megamod-portable-beds/).

## Documentation first and selected identity

The [Workshop description](https://steamcommunity.com/workshop/filedetails/?id=3534169351) was read
before behavior source. The sharedfiles URL initially returned a regional error; the ordinary workshop
URL worked. Generic removal/incompatibility boilerplate is not evidence of the item's current status.
The description covers inventory/ground/shared bed deployment, packing after sleep, remembered home
ownership, optional use far from assigned beds, tailoring production, sheltered placement, caravan
redistribution and contextual cancellation/pickup controls. Beds selected for a forming caravan are
excluded from deployment. Author comments discuss an already-packed-bed recovery fix and limitations
on extending automation to visitors; those reports are not independently observed behavior.

The linked [maintainer repository](https://github.com/MemeGoddess/RimWorld-UseBedrolls) identifies the
fork of alextd/RimWorld-UseBedrolls and MIT license. Original Workshop1353625821 lookup returned429.
Installed About credits **Uuugggg**, current Workshop maintainer **Meme Goddess**. Package
**Memegoddess.UseBedrolls**, Workshop **3534169351**, advertises **1.1 through1.7**, requires Harmony.
Installed LICENSE.txt is the MIT text, copyright2025 Alex Tearse-Doyle. No source/code/assets are copied
into the product; any later licensed reuse must retain its notice. Read-only installed root:
`F:/Steam/steamapps/workshop/content/294100/3534169351`.

LoadFolders v1.7 lists root and1.1–1.7 in order. Current native ModContentPack file selection and
ModAssemblyHandler use later-folder precedence for the same relative filename. DirectXmlLoader likewise
deduplicates relative XML paths. Selected binary is **1.7/Assemblies/UseBedrolls.dll**, assembly/file
**1.0.0.0**, not all seven binaries. The identical-path root crafting patch is superseded by
**1.1/Patches/UseBedrollPatches.xml**; both inspected copies contain the same three operations. Root
PickUpBedrollThinkTree.xml and Defs/UseBedrollDefs.xml remain selected; the commented tree in Defs is
inactive. English1.4 overrides the same relative root English file; inspected contents agree.

| Input | SHA-256 |
| --- | --- |
| About/About.xml | `8EC3347234CAAED1ADAADA4C9ADDC8F679EF4493D1F62FB0B26445ADB105C8F7` |
| LoadFolders.xml | `D361804D8FA733E0FB9D90C25F7503F622EB51D9C77BE13A03C14B583B44772B` |
| 1.7/Assemblies/UseBedrolls.dll | `B7540DD6EDE000A3CF3D78AD272DD7ABCD96E2D56B4BD8466A48368182BCAFDA` |
| Defs/UseBedrollDefs.xml | `97F90765F23D4FEFF388D5F828B468C42D384AD2DB68DB441257E38CA76725E7` |
| 1.1/Patches/UseBedrollPatches.xml | `57E9008EB31E308347213C3F8122C6E0AD89DE69977AA8F625ADC2BE2281C7F6` |
| Patches/PickUpBedrollThinkTree.xml | `72D37A0488085C327048E42F1876977A7F40FD76600DA6F23FD06F6AE6B70772` |

Read all installed root runtime C# files, project metadata, assembly attributes, selected XML and both
English layers. Decompiled the whole selected binary to
`artifacts/Refinement/megamod-spec-20261104/use-bedrolls/upstream/`; directly read its Mod, assembly
metadata, InterceptRest, JobGiver_TakeBedBack and JobDriver_TakeBedroll in the initial pass. The subsequent
pass read all remaining compiled behavior classes, including caravan sharing, memory, placement,
gizmos, message hooks, ownership cleanup and serialization. Source uses conditional DEBUG
logging; the inspected1.7 constructor has no Harmony.DEBUG assignment and selected paths omit debug calls.

Current native Assembly-CSharp hash was rechecked:
`FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` (1.7.4998 rev1299 reference).
Prepared native excerpts under sibling `native/` include JobGiver_GetRest, Pawn_Ownership, Building_Bed,
RestUtility, CaravanEnterMapUtility, JobDriver_Uninstall, JobDriver_RemoveBuilding and DirectXmlLoader.
The initial pass read GetRest and loader behavior. The completed native pass below distinguishes the
additional inspected semantics from the initial questions retained in this chronological inventory.
No mod was loaded or executed.

## Source behavior inventory

### Rest interception and search

InterceptRest postfix requires a nonnull native rest job and IsColonistPlayerControlled. Default
alsoColoniesfalse suppresses it on IsPlayerHome maps. If the original target is **any** Building_Bed,
it retains that job unless distanceCheck is enabled and straight-line distance is at least distance
(default100, slider0..300). This checks the selected bed, not necessarily OwnedBed as the label suggests.

Candidate order: first usable own inventory entry, nearest usable ground minified thing by Manhattan
distance, then another same-faction spawned pawn's inventory. Usable means inner Building_Bed,
not bed_defaultMedical, CanUseBedEver for sleeper, not selected by a forming-caravan transferable and
no existing install blueprint. It does not comprehensively recheck current medical/owner/access state.
Ground candidates require not forbidden and CanReserveAndReach with Danger.None. Shared candidates use
native reachability and qualify with more than one compatible bed, or a single bed if animal, has an
OwnedBed, or is the sleeper's love partner. Bed counts count objects, not sleeping slots. Selected
shared stock is **dropped during job search**, before placement succeeds; transfer success/state is
not a transaction. Do not copy that speculative side effect into an owned selector.

Placement centers on the selected bed's held position, tries South/West only, rejects mismatched prison
room, failed blueprint placement, any edifice, zones anywhere in footprint and nonpawn construction
blockers. It prefers psychologically indoors candidates within4 then12 cells, then any qualifying
candidate within4 then12, with FindPathNow cost below500 from the bed location. The test does not
explicitly verify path Found or revalidate heat/vacuum/other hazards beyond those native helpers.
Room null/edge behavior and safe final footprint need tracing. A successful query immediately places
an install blueprint and substitutes PlaceBedroll; this is another mutation before job commitment.

### Deployment, collection and recovery

PlaceBedroll is casually interruptible, nonsuspendable. Its driver reserves only a spawned item in a
later toil; TryMakePreToilReservations returns true. It cleans its remaining blueprint on finish,
collects one ground item if needed, moves inventory to carrier, carries to the install container and
uses native MakeSolidThingFromBlueprintIfNecessary. It claims the resulting bed (sets Slave owner type
for a colony slave), records pawn→bed in map placedBeds, then starts native LayDown. Installation work,
permission and native materialization/ownership consequences still need exact tracing.

The recovery giver is inserted before Humanlike's ConditionalNPCCanSelfTendNow, not the commented
PostDuty insertion. Priority is8 minus native rest priority unless ShouldKeepPlacedBed. The latter
uses source thresholds Anything/Joy below0.3, Meditate below0.16, Workfalse, Sleeptrue, other allowRest
below0.3; it requires no temporary sleep blocker. Blockers include canSleepTick, disturbance, starving
when age disallows sleep, sleeping-blocking hediff and a current noninterruptible-for-sleep job.
It returns TakeBedroll for player-controlled colonists with a recorded bed they can reserve, with
ignoreForbiddentrue. Sleep need and actual continuation/loop behavior need native composition analysis.

TakeBedroll subclasses JobDriver_Uninstall, adds an uninstall designation if needed and uses removal
work (ConstructionSpeed×1.7×delta), native effects and progress. It checks forbidden/explosive states.
At finish it **removes placedBeds first**, finds an existing wrapper in own inventory or map by exact
inner bed identity, else uninstalls the spawned bed, then tries adding/transferring to inventory without
checking success or capacity. It may immediately reclaim a remembered home bed without the return-home
validity/aggressive-setting checks. A failed transfer/recovery must not discard owned tracking in the
replacement. Source placement/recovery are not evidence of balanced capacity, safe roof removal or
absence of repeat install/uninstall cycles.

PlacedBedsMapComponent persists reference dictionary key `placedBeds`, removes null entries and tracks
one bed per pawn; reverse owner lookup scans it. RemoveAllOwners postfix removes the first matching
temporary entry if the bed is spawned. Double-bed sharing/multiple claimants, other-uninstaller recovery,
forbid/ownership changes, destruction, cancellation, load and partial failure need explicit owned rules.

### Caravan redistribution and home-bed memory

CaravanEnterMap prefix chooses the first free colonist as a **single compatibility exemplar**, then
distributes one bed to free colonists without one. Donors have multiple compatible beds or are animals
with any; sort uses ascending bed count. It transfers directly between containers, does not inspect
recipient-specific size/compatibility or capacity, and does not check success. This describes map-entry
inventory redistribution, not a new world-travel sleeping engine or extra sleeping slots.

HomeBedComp persists `homeBeds` pawn→bed and rebuilds `bedOwners` bed→singlepawn. Set does not clear old
reverse entries; a double bed can have multiple remembered pawns but only one reverse owner. Bed
DeSpawn removes remembered state. ExitMap prefix acts on IsColonist (not the same condition as deployment),
remembers a bed on IsPlayerHome and optionally unclaims any owned bed (unassignOnExittrue). SpawnSetup
postfix ignores load-spawns; on any player-home arrival it attempts same-map, nonprisoner reclaim if
aggressive or IsValidBedFor, then clears remembered state even on another home map or failed reclaim.
The aggressive branch may bypass constraints its tooltip says remain; native Claim/medical validation
must be read before precise claims. Prior player edits and multiple home maps are not fully represented.

### Controls, settings and message handling

No source bottom tab or debug surface. Standard settings contain:

| Setting / persisted key | Default | Source control |
| --- | --- | --- |
| reclaimAggresively | false | Reclaim despite someone else's claim |
| unassignOnExit | true | Release bed when leaving map |
| distanceCheck | false | Allow inventory bed when selected bed too far |
| distance | 100 | Slider0..300, visible only while distanceCheck |
| alsoColonies | false | Enable placement on home maps |

These are global ModSettings; owned configuration must use common global/save inheritance. Bed-specific
temporary/home reservation and cancellation are entity state, not settings. BedOwnerGizmo displays a
pawn and opens Cancel. On a home bed this removes remembered state and calls UnclaimBed on the traveler,
potentially releasing a different currently owned bed; on a deployed temporary bed it only removes
pickup tracking. A drafted pawn with a tracked bed gets Pick back up bed, forcibly starting TakeBedroll.
Permissions, current occupancy, capacity and safe removal need rechecks through a native command.

Two source transpilers suppress unassignment messages: DeSpawn wraps ExistingBlueprintFor results with
the temporary-owner condition, and RemoveAllOwners wraps every matching Messages.Message call, hiding
them whenever the bed's map parent faction is not player. This broad suppression is not scoped only to
owned intentional pickup. All owned patches need minimal semantic/effective-stream checks and atomic
cleanup/fallback; never reproduce unguarded PatchAll or mute unrelated bed-loss warnings.
PawnBlockConstruction globally returns false for pawn blockers against **all** beds/install-bed
blueprints, not just this feature's temporary placement. Scope that decision explicitly after native
construction analysis; broad behavior must not slip in as an undocumented convenience.

## Declarative production and current comparator discrepancy

Source XML adds recipeMaker for Bedroll/BedrollDouble with Crafting, GeneralLaborSpeed, Tailor effect,
Recipe_Tailor sound, UnfinishedApparel and both electric/hand tailoring benches. It adds WorkToMake
**350/600**, comments claiming equality with WorkToBuild. Current Core Buildings_Furniture.xml gives:

| Native product | Stuff / slots footprint | Mass | WorkToBuild | Other current base stats |
| --- | --- | --- | --- | --- |
| Bedroll | Fabric/Leathery40;1×2 | 2.5 | **600** | HP75, rest0.95, comfort0.68, immunity1.05, healing4 |
| Double bedroll | Fabric/Leathery85;2×2 | 4.5 | **1100** | rest0.95, comfort0.68, immunity1.05, healing4 |

Do not silently call source350/600 equivalent to current construction. Exact implied recipe output,
minification, stuff/quality/work/skill/yield, inherited stats, third comparator and vanilla tailoring
availability remain to inspect. Preserve unrelated recipe users/fields, use XML Extensions content
guards and idempotent merges; no blind recipeMaker/list replacement. The production addition and
behaviorally significant work choice need explicit observable coverage.

## Known consumed contract and composition to trace

Bounded installed C#/XML/JSON search finds Smart Medicine StockUpGUI testing only package
`Memegoddess.UseBedrolls` to show native Bedroll, plus a replacement-catalog JSON. No callable consumer
was established by this search; compiled/reflected consumers still need inspection. S062 already
specifies available supported portable bedrolls in personal supplies without requiring a spoofed source
ID. Compose directly with that policy, its protected personal stock and field-rest behavior.

S021 batch hauling/capacity and shared index, S060 construction access, S061 roof-safe uninstalling,
S070 native timetables, later S115 Sensible Bed Ownership, and S135 Basic Caravan Loading need precise
overlap checks. Do not invent conflicts or deepen another queued source prematurely; record boundaries
for its turn. Source's global blocker override must not bypass S060/S061. Reuse native listers/shared
derived item data; do not create a bedroll-specific global scan/cache framework.

## Completed native and selected-binary pass

Read all selected compiled behavior classes and the native ownership, rest utility, caravan entry,
uninstall/remove-building, recipe generation/product creation, blueprint install/base, construct toils,
recipe work, ThingOwner transfer/capacity and MassUtility paths. Further extracts are beside the
earlier native artifacts; extraction alone is not claimed as coverage of unrelated methods.

- **Rest permissions:** GetRest can deliberately return ground rest because a lord forbids beds, the
  pawn is wild/roped or its mental state disallows them. A nonnull job is not bed authority. Native
  timetable priorities are Anything/Joy below0.3, Meditate below0.16, Work0, Sleep8. CanFallAsleep also
  considers starvation/age, disturbance400ticks, rest threshold, blocking hediff and interruption.
  Owned behavior consumes those decisions instead of maintaining a second threshold scheduler.
- **Bed validity:** RestUtility covers size, humanlike status, mech exclusion, special deathrest,
  ideology, owner type, free/shared slots, medical use, faction, forbidden, reach/reservations,
  burning and harmful vacuum. CanUseBedEver alone misses most current conditions. Owned placement
  must explicitly validate the complete current sleep context and footprint.
- **Claim side effects:** Pawn_Ownership.ClaimBedIfNonMedical returns false for already-owned/medical,
  otherwise unclaims the current bed and evicts the last owner if full. Ideology violation logs do
  not make it an eligibility gate. Load reconciles native ownership after references are restored.
  Therefore validate before any claim and preserve multiple remembered owners. Source aggressive
  reclaim bypasses far more than occupancy; owned option relaxes that contention only.
- **DeSpawn/messages:** Bed de-spawn can reset owner type/medical state and remove owners; intentional
  install-blueprint transitions receive special native handling. The source map-faction message
  filter suppresses unrelated losses. Owned suppression is limited to its deliberate transition.
- **Installation:** Blueprint_Install unwraps the existing inner Thing, transfers wrapper designations
  and destroys the emptied wrapper. Blueprint.TryReplaceWithSolidThing checks blocking Things but
  then calls WipeExistingThings and removes occupied plan cells. Source delivery calls this immediately;
  no150-work construction delay is performed despite the blueprint's inspection WorkTotal. Owned
  policy retains physical delivery/unfolding and rejects footprints that would erase unrelated
  content/plans before destructive unwrap. It does not borrow install-over to clear camp space.
- **Uninstall:** native RemoveBuilding persists its own private workLeft/totalNeededWork, checks
  CannotTouch, notifies faction, removes designations and invokes ordinary uninstall completion,
  including ThingsUninstalled. The source subclass declares different private fields, does not
  serialize those used fields and omits several native completion actions in its copied loop.
  Ordinary default uninstall work is200; progress is ConstructionSpeed×1.7 per tick. Preserve native
  completion/progress rather than copying this loop. Source ignoreForbidden request does not imply
  permission to bypass later forbidden failures.
- **Capacity:** ThingOwner.GetCountCanAccept checks stack capacity, not carried mass. Container
  transfer splits and restores on failed insertion where possible, but callers must inspect success.
  Source recovery can detach an item and ignore failed TryAdd, then forget tracking first. Shared
  capacity must include S021/S126 hands/cargo as well as inventory and protected personal supplies.
- **Caravan entry:** native entry can move shuttle cargo, drop all inventory or mark individual
  unloading after spawning pawns. A source prefix can transfer a bed only for that continuation to
  drop it. Preserve the selected mode, prove final holders and use per-recipient compatibility.
- **Recovery/rest:** source ShouldKeepPlacedBed uses a momentary sleep blocker to make pickup eligible;
  the specified correction retains a coherent rest episode through short disturbance. One transport
  custodian and multiple actual sleepers prevent duplicate double-bed recovery or ejecting a partner.

## Production comparison and proposed decision

Current XML ancestry is BedWithQualityBase→BedBase→FurnitureBase→BuildingBase. Double-bedroll HP has
no explicit override in that chain and uses MaxHitPoints defaultBaseValue100 before material/quality
and loaded patches. This is a static base trace, not measured finalized gameplay.

| Product / acquisition | Output | Base materials | Mass | Work | Base HP / rest / comfort | Availability |
| --- | --- | --- | --- | --- | --- | --- |
| Core single bedroll build | One native bed |40 Fabric/Leathery |2.5 |600 Construction |75 /0.95 /0.68 | Native early furniture |
| Core double bedroll build | One native double bed |85 Fabric/Leathery |4.5 |1100 Construction |100 default /0.95 /0.68 | Native early furniture |
| Core ordinary bed build | One native bed |45 Metallic/Woody/Stony |30 |800 Construction |140 /1 /0.75 | Complex Furniture |
| Core double bed build | One native double bed |85 Metallic/Woody/Stony |50 |1500 Construction |200 /1 /0.75 | Complex Furniture |
| Source tailoring | One minified native single/double |40/85 same categories |2.5/4.5 |350/600 GeneralLaborSpeed; Crafting quality | Native product unchanged | Hand/electric tailoring benches |
| Proposed owned tailoring | Same native output |Same40/85 |Same |**600/1100** base work; Crafting quality | Native product unchanged | Same benches, no added skill/research gate |

RecipeDefGenerator makes Make_<product>, consumes costStuffCount through the product's eligible
categories plus any ordinary extra cost list, copies recipeMaker fields and recipeUsers. GenRecipe
creates the native product, assigns dominant material/style and workSkill-derived quality, then
minifies when appropriate. RecipeDefGenerator copies recipeMaker.workAmount, whose default is-1;
RecipeDef.WorkAmountForStuff then resolves the product's WorkToMake with its actual Stuff. It does
not freeze that stat into recipe work at generation. Preserve this material-aware fallback. Generated
Stuff recipes use IngredientValueGetter_Volume, so40/85 are base material units; physical counts depend
on VolumePerUnit and applicable bill rules. Do not equate Crafting speed/quality with Construction skill. Existing
native recipes/costs/stats stay unchanged. The proposed labor increase is about71%/83% over source,
explicitly based on current build anchors. Finalized market values, cheapest/common/expensive material
economics, loaded optional recipes and native crafting time still require implementation acceptance.

## Consumed contracts and composed result

The completed bounded installed C#/XML/JSON search returned35 files. Outside this source they are
Smart Medicine's StockUpGUI, the replacement catalog and a mod-manager database. The database records
the original package and a translation package; the catalog maps Uuugggg.UseBedrolls and
TheLordDrake.UseBedrolls to Memegoddess.UseBedrolls. These are identities, not callable consumers.
Smart Medicine tests only the Continued package to expose native Bedroll. No callable/reflected
consumer is established by that text search; compiled-only consumers remain an explicit limit.
No identity facade is warranted. S062 directly exposes the owned supported bed catalogue.

Direct composition selected:

- S021/S126 share exact physical mass/identity and commitments; no second inventory allowance.
- S062 retains its target and real carried count. A temporarily deployed personal bed covers one
  shortfall as a distinct committed object, not free colony stock; it cannot be restocked or borrowed
  again while serving that episode. Donation/automatic deployment is not the manual Drop command.
- S062 medical/field-rest instructions win over optional ordinary rest substitution.
- S060 CT08 excludes installing already-built minified furniture, including this deployment. Do not
  promise its frame enclosure protection here. Source's global pawn-blocker exemption is not adopted.
- S061 applies before a roof-supporting bed is uninstalled. The existing explicit mountain-risk
  exception for a successful forced order remains, with removable-roof preparation and other rules.
- S070 consumes current native timetable decisions. No new rest-priority framework.
- S115 Sensible Bed Ownership and S135 Basic Caravan Loading remain queued; their eventual policies
  must preserve explicit home intent and shared loading commitments. No conflict is asserted yet.

Independent native/source review corrected the material-sensitive WorkToMake fallback and distinguished
base ingredient volume from physical counts. Cached IngredientCount.CountRequiredOfFor and
RimWorld.IngredientValueGetter_Volume confirm division by material VolumePerUnit. The added scenario
covers differing material volume/work. Independent composition review identified S060's installation
exclusion, missing ordinary portable Retry, and the required slave-bed setup: native Bed.DeSpawn
resets owner type to Colonist, RestUtility requires slave status to match, and selected source
ClaimTheGoddamnBed sets Slave before claim. The draft now preserves the S060 exclusion, separates
permission-preserving Retry from forced pickup and explicitly permits only the validated unassigned
bed conversion, retaining shared eligibility and later player edits.
Focused composition re-review confirmed all three findings resolved and also confirmed the corrected
material-work/count contract. No remaining material contradiction was found. Strict validation passed
95 items, zero failures, exit0/no stderr. The draft contains12 requirements,38 scenarios and29 unchecked
implementation/acceptance tasks; documentation checks do not accept gameplay.

## Open questions retained for later

**2026-11-06 composition amendment:** [S115 research](2026-11-06-sensible-bed-ownership-research.md)
now resolves the previously deferred ownership interaction. The source-derived release default true
described above is historical source/initial-draft evidence; the composed owned default is **false**.
Retain ordinary home claims. Optional release creates separate pending intent per map/allowance for
beds left behind, with existing guarded reclaim. Same-allowance temporary rest uses custody without
a second ordinary claim; a manual choice on another map does not cancel remote home memory.
Eligible portable deployment precedes the S115 seat fallback. The owning draft, design, tasks and
coverage are amended together; none of these composed behaviors has been implemented or run.
Independent S115 composition review additionally corrected the released-memory scenario
preconditions and defined occupied-slot reclaim by displayed owner order, not supposed assignment
chronology. The distinguishing scenario, design and task now agree; focused re-review passed.

| ID | Remaining work / decision | Current draft boundary |
| --- | --- | --- |
| PBR-Q1 | Exact effective job/rest/install/uninstall/ownership seams, later repatch and XML phase/atomicity | Candidate contracts in design are not approved target docs; benign changes allowed, failed dependency disables affected work |
| PBR-Q2 | Rest episode, multi-owner manual/automatic claim provenance, optional bed/level providers, S115 composed native proof and later S135 loading | S115 retain/release/per-allowance/same-allowance temporary-rest policy now drafted; native proof still required |
| PBR-Q3 |600/1100 tailoring proposal and material economy | Explicit correction to current construction anchors; no accepted balance without finalized values and real bills |
| PBR-Q4 | Compiled consumers, source-save migration and old-provider binaries | Text search limits reported; no package spoof, automatic import, blanket drop-in or safe-removal claim |
| PBR-Q5 | Implementation, seven languages, common settings, tests, independent reviewed build and native workflows | Every implementation/acceptance task remains unchecked |

No runtime code/Defs/assets, tests, build/install, game launch, upstream execution, Workshop edits or
normal save/configuration mutation occurred. All evidence above is static source/native inspection.
