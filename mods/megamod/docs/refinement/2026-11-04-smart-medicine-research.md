# S062 — Smart Medicine - Continued research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Inspected **2026-11-04**, sequentially after S061. This is read-only source/native analysis, not
reproduced gameplay. [Proposal](../../../../openspec/changes/specify-megamod-smart-medicine/proposal.md),
[design](../../../../openspec/changes/specify-megamod-smart-medicine/design.md) and the three capability
specs separate intentions, mechanisms and selected observable behavior. Nothing implemented.

## Documentation first and provenance

Read the [continued Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=3526251680),
[recent change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/3526251680),
[original page](https://steamcommunity.com/sharedfiles/filedetails/?id=1309994319) and
[repository](https://github.com/MemeGoddess/RimWorld-SmartMedicine) before local analysis.
The pages advertise inventory medicine, personal stocks, injury-specific care, field tending and
surgery preferences. The continued page's warning that injury choices are not saved is stale: the
current component serializes them. The installed About additionally advertises choosing cheaper
medicine when doctor/bed effectiveness is sufficient; the selected code does not implement that
general optimization. Minimal non-urgent potency selection is a different feature.

Recent change notes describe 1.7 support, temporary-spot interruption cleanup, field-care fixes,
continued tending of drafted patients and emergency care. [PR 22](https://github.com/MemeGoddess/RimWorld-SmartMedicine/pull/22)
was inspected as provenance for the emergency work; its existence does not prove runtime behavior.
Public performance, compatibility and safe-removal claims remain unverified. Generic Steam banners
are not compatibility evidence. Public text is summarized here; source establishes the details below.

| Input | Exact inspected identity |
| --- | --- |
| Current | `Memegoddess.SmartMedicine`, Workshop `3526251680`; maintainer Meme Goddess, About author Uuugggg |
| Original | `Uuugggg.SmartMedicine`, Workshop `1309994319`; installed replacement metadata corroborates ID |
| Installed root | `F:/Steam/steamapps/workshop/content/294100/3526251680` |
| Public revision | `0410071ef1ddc23bea7115da9787b7e817fe18b0`; all 38 installed Source C# files match public after CR normalization |
| Selected DLL | `1.7/Assemblies/SmartMedicine.dll`, assembly/file 1.7.0.0, informational `1.7+0410071ef1ddc23bea7115da9787b7e817fe18b0` |
| DLL SHA256 | `3E28327108A663D60788B0A185D4DD17DADBB150280FBFE96281922EFEDD0315` |
| About SHA256 | `26499A76BC1666DDE46A2D910D85A46E301F400B8FEF5100A999DC083E856BDE` |
| Public bundled1.7 DLL | SHA256 `CD6823F981A1712D1E73FE9BF30F0439D4998C42CF57A144C78EC9D1E690F032`; differs from installed DLL, so source agreement is not binary equivalence |
| Native | 1.7.4998 rev1299, `F:/Steam/steamapps/common/RimWorld/Assembly-CSharp.dll`, SHA256 `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |
| License | MIT, copyright 2025 Alex Tearse-Doyle; retain applicable notices for any future reuse. No source or assets copied into the product |

All 38 runtime source files (4,964 lines), selected gameplay XML, LoadFolders, metadata and English
labels were read. Full selected DLL decompiled; `FindBestMedicine`, `FieldTendingUtility`,
`GetMedicineCountToFullyHeal_Patch`, `TendPatient_Notify_Starting_Patch`, `DefaultCareFix` and assembly metadata were
read to corroborate central paths. This is not a claim that every reconstructed class was reread.
Source tests were located, not run or accepted as evidence. Public project targets net10.0/1.7;
no source build or game launch. Local ignored artifacts:
`artifacts/Refinement/megamod-spec-20261104/smart-medicine/{upstream,compiled,native,consumer-mod-medicine-patch}`
and `consumer-hits.txt`.

LoadFolders lists root and 1.1 through 1.7. Native ModContentPack reverses the selected list for
precedence; assembly preserve-order enumeration removes earlier duplicate relative paths. The 1.7
SmartMedicine.dll wins; same-path 1.7 CE XML and 1.5 patch XML override earlier copies. No duplicate
old DLL loading is inferred from the folder list alone.

## Recovered behavior

### Care and condition UI

`SetMedForHediff`, `PriorityCareSettingsComp`, `GetPawnMedicalCareCategory`, `IgnoreHediffsForRest`
and `DefaultCareFix` supply the condition policy. Right-click native Health rows groups conditions
by UIGroupKey plus body part, gated by TendableNow ignoring the timer. Choices are ordinary five
care categories, Default care and Patient Bed Rest. Care/rest icons are added to rows. Group setting
affects all members, but **Default care removes only the primary member**. External list menu API
also overwrites its optional primary argument with the first member. Per-condition care and ignored
rest sets are saved by reference; removed conditions clean both. The obsolete PriorityCareComp Get
forwards to the new dictionary. These are per-condition gameplay choices, not global settings.

NoCare suppresses TendableNow except ignoreTimer calls. PriorityHediff replaces native TendPriority
with the care difference times 5; it does not add to native priority despite the source expression.
Medicine-count filtering groups by highest applicable care and optionally excludes non-urgent
injuries. It temporarily **writes pawn.playerSettings.medCare** upward during a count query and
stores the previous value. Cleanup restores it, potentially overwriting a later player edit.
Original values are saved, expiry times are not; expired entries are removed without restoring care.
The static minimal-medicine flag also couples separate queries. These are static risks, not live
bug reproductions. Owned behavior must avoid saved-policy mutation and shared transient decisions.

An injury is urgent in this code if bleeding or it has an infection component; every non-injury is
urgent. Public permanent-effects wording is broader than that predicate. Untendable conditions stay
untendable. Rest suppression removes selected tended-healing injuries/nonimmune conditions from
non-urgent rest decisions, not genuine downing, pending treatment, surgery or labor. A global phase
flag tries to preserve health-tick healing behavior; other immunity-query users can still be affected.
Owned suppression will be scoped to rest choice, not disease progression or medical eligibility.

### Medicine choice and physical collection

MedicineGrabbing replaces WorkGiver_Tend.JobOnThing and HealthAIUtility.FindBestMedicine. It considers
ground medicine and one preferred medicine stack per allowed player-faction spawned inventory,
including the doctor's carry item. Doctor/patient/other-free-colonist/animal toggles interact: turning
animals off actually removes all non-colonists, and non-player patients' own supplies are never in
the initial donor list. Inventory items lack the ground path's individual forbidden/reservation checks.
Mechanical-pawn extension reflection switches ground candidates to repair-tagged haulables, while
inventory selection still requires IsMedicine; a missing repair extension type is not safely handled.

Normal ranking is highest potency then travel cost, inventory winning a full tie. Minimal non-urgent
mode instead favors lowest potency. Equal ground supplies can replace the selected inventory item
within a configured detour. Source mixes path costs with Manhattan distances for that comparison;
other-donor range uses Manhattan detour consistently. Additional equal-rated stacks within 8 Manhattan
cells of the primary are collected up to the full-treatment count. Equal rating is not physical
stack compatibility; replacement of the primary can leave it duplicated in the secondary list.
Total requested count can exceed actual selected stock, and reserved portions are not subtracted.

The deadline filter applies only to ground medicine: reject total travel above estimated remaining
life, or above half that life when it is under 5,000 ticks. Donor fetches miss that safety filter.
Path failure falls back to Manhattan distance. Candidate evaluation does not include tending time.
The owned design uses explicit cell detour preferences and separate actual journey estimates,
filters every source consistently and never treats a failed route as a quick reachable route.

Notify_Starting drops donor inventory near its owner, clears the item's forbidden flag and reserves
it. `DropIt` returns null for an already-spawned item; the caller assigns that null to primary target B.
Both source and selected compiled class confirm this risk. Additional split-drop results are not
written back to queued references. Generated pickup patches use compiler-local assumptions.
Owned collection must preserve permission, exact split identities, reservations and actual capacity.
Source release debug methods are empty/conditional; do not confuse them with active per-tick log spam.

### Surgery

InventorySurgery holds the current Bill in static state, adds the doctor's inventory to candidates,
rechecks care and bill filters, sorts potency then distance, and drops held ingredients before the
native collection path. Other donors are not included for surgery. Minimal non-urgent mode reverses
potency for the anesthesia-only recipe; No medicine mode does not eliminate operation ingredients.
Static bill scope is not exception/nesting safe. Source drop helper is injected into all bill collection.

SurgeryUnlimited saves individual pawn checkboxes and a save-level default; new/reset medical-care
pawns copy that default. It raises operation medicine eligibility to Best, not quantity or free
success. Native Health overview and Assign column expose individual choices; an extra checkbox in
Medical Defaults edits the save default. That duplicated configuration moves to common settings;
individual checkbox choices remain on the pawn, initialized once without reapplying on every reset.

### Field care

FieldTendingActive is limited to living free colonists; a provided doctor must reserve the patient.
Modes: emergency on, no-bed off, always off. Always wins; no-bed/always UI clears the other boolean.
Emergency requires a bed, a doctor and a downed patient. It estimates doctor-to-patient travel plus
**doctor-to-bed** travel at a fixed 60% carrying speed, plus two game hours. This is not the actual
patient-to-bed journey. A missing path returns zero in that helper. No-bed emergency is accidentally
excluded unless the separate no-bed mode is enabled.

GoodLayingStatus can extend treatment to lying/downed or idle drafted patients, excluding full-body
busy/staggered patients. After each tend, a player-controlled downed patient's posture is reevaluated;
the job ends when field eligibility no longer holds, leaving ordinary rescue possible. There is no
explicit guaranteed rescue job. Remaining medicine may be returned to inventory.

PatientGoToBed creates/reuses a temporary medical sleeping spot when normal bed search fails and
field mode allows it. Placement tests against wiping content; failure can still yield a LayDown job
with a null bed in source. Spot has a Cancel action, removes itself on rest-job cleanup, or after
more than 2,500 idle ticks checked at rare ticks. Occupied/reserved spots refresh the last-use clock,
which is saved. It has no construction cost/HP, Comfort 0.4 and surgery factor 0.7: these match native
SleepingSpotBase, not a newly invented comfortable hospital bed. Native defaults supply other stats.
Owned marker will retain ordinary ground-rest consequences, cannot erase content or become a durable
hospital facility, and will clean only itself without canceling unrelated patient work.

FeedPatientUtility's InBed check is extended to field eligibility. Native **standing-pawn rejection
still runs first**, so the patch does not by itself feed standing healthy colonists. Food need,
medical-rest, faction, prisoner and slaughter restrictions also remain. Restrict owned extension to
an actual field-rest patient rather than merely an enabled mode.

Death prediction starts with native blood loss, then tendable timed-death conditions and lethal
positive severity growth. Values below 625 ticks return early, possibly before a smaller estimate
later in the list. No guarantee of survival follows from these approximate predictions. Owned rule
takes the minimum known estimate, excludes unknowns from invented certainty and uses current paths.

### Personal stocks

StockUpUtility stores Pawn→ThingDef→count with save references; desired-count cache can stay stale
for 625 ticks. Counts include matching minified contents but the return selector compares outer Def.
Copy stores a live source pawn, so later paste reads later edits rather than copy-time values.
Clear can cancel the global clipboard. Pawn destruction/save cleanup removes dead references.

Gear rows show desired count, +/- controls, Paused/Encumbered and red missing-type rows. Missing means
zero, not partial shortage. The dialog lists medicines, drugs, hemogen and bedrolls if Use Bedrolls is
active. Stock ANYTHING lists EverHaulable types and enables search; it is developer-gated unless a
global option exposes it. This still requests real physical items, so it is ordinary gameplay in our
UI, not a cheat. Dialog counts clamp 0–9999 but row +1 lacks that upper clamp. Manual Gear drop removes
the entire source target for that type. Drug ingest buttons reuse native actions. RPG and CE Gear
views receive ad hoc button patches; the selected RPG Revamped contract needs its S132 inspection.

Stocking runs at Humanlike_PostDuty with insertion priority100. If Doctor work is active it defers
for reachable reservable tendable patients and pending operations, but the source does not establish
all actual job eligibility. It skips UnloadEverything. Pickups honor mass, stock need and ordinary
ground access. Nutrient paste is **dispensed during job selection**, can create several meals before
a job starts, and bypasses scarcity if any dispenser can dispense. Owned query paths must not produce
items; ordinary dispensing must be real work, with final meal policy composed in S123.

Scarcity default is 150% of all spawned free-colonists' requested quantities: counted supply includes
resourceCounter ground stock, minified contents and their inventories. It is a colony-total threshold,
not 50% that must remain on shelves after every pickup. Multiplier0 disables it; insufficient supply
pauses replenishment, optionally returns existing supply (off). Over-target supply returns anyway.
StockDown transfers physical inventory to storage. Cleanup can return all carried medicine without a
fresh mass check. Drug retention protects whole configured types, including target0, rather than
only desired quantities. Caravan-unload patches prepend desired counts to native retention. Low
Medicine alert adds free-colonist inventory medicine, regardless of actual medical access.

## Native 1.7 comparison and direct composition

Read current WorkGiver_Tend/HealthAIUtility (S058 artifacts) and newly reconstructed Medicine,
TendUtility, JobDriver_TendPatient, FeedPatientUtility, JobGiver_PatientGoToBed, Pawn_InventoryTracker,
Pawn_InventoryStockTracker, InventoryStockEntry and JobGiver_TakeForInventoryStock.

- Native FindBestMedicine already checks doctor inventory and, as a fallback, colony animals.
  TendPatient has target-C holder transfer, real collection and subsequent medicine search. Extend
  those paths rather than assume 1.6-style mandatory dropping.
- Native treatment consumes one medicine per treatment; injury batches allow total severity20,
  some conditions tend all of their type together. Without medicine ordinary injuries are separate.
  Potency, quality cap, doctor skill, bed offset and self-tend factor0.7 remain native outcomes.
  Our care partition must govern both counting and actual treatment, not just a display query.
- Native 1.7 already has saved per-pawn inventory-stock groups; Core supplies Medicine with three
  standard medicines, industrial default. Stock jobs and caravan retention consume these choices.
  Independent Smart Medicine targets must not compete with them. Preserve native choices and use
  a single counted target per item with visible contributing orders; never add duplicate targets.
- Native inventory retention also includes drug policies and food. Its first matching count is not
  a sound general merger for duplicate policy entries. S021/S055 quantity ownership must combine
  protected requirements once, without turning transient medical/bill cargo into permanent supply.
- S007 owns compact display; S062 directly supplies care actions/icons. S024 cleanup yields to
  continuing treatment. S042 governs self-tend permission, not new medical ability. S058 ranks
  eligible patients, not condition batches or medicine. No invented priority conflict or own-mod patch.
- S080 stabilization, S123 routine meals, S124 Drug Response, S132 RPG inventory and S134 Priority
  Treatment are later analyses. Reserve their composition boundaries; do not infer drug orders,
  interrupt rules or re-prioritize patients from their names.

## Options and UI audit

| Source option | Source built-in default | Selected treatment |
| --- | --- | --- |
| Doctor/patient/other-colonist/animal inventory | all true | Preserve independent roles; don't let animal switch remove humans |
| Equal ground preferred / detour | true /6, UI0–99 | Preserve cell-detour intent with consistent units |
| Other donor detour / ignore limit | 12, UI0–99 /false | Preserve; urgency/permission still apply when unlimited |
| Minimal / no non-urgent medicine | false /false | One mutually exclusive mode: ordinary, lowest available, none |
| Stocking / threshold / return under scarcity | true /1.5, UI0–5 /false | Shared quantity/capacity, no phantom paste, immediate edits |
| Emergency / no-bed / always field | true /false /false | Independent emergency, ordinary field mode off/no-bed/always |
| Default unlimited surgery | false | Common default initializes new individual choice once |
| Stock anything without dev | false | Extended catalogue preference; ordinary real pickup, not debug |
| NiceHealthTab label cache | true | Implementation optimization only; omit copied switch/cache, preserve current-language text |

Health condition menu/icons, Health surgery checkbox, Assign/optional Medical column, Gear targets
and temporary-spot Cancel are contextual gameplay controls. No source bottom main tab exists.
Source global settings plus its separate saved medical default become common ordered Medical care,
Field care and Personal supplies sections with global/save inheritance. No extra operational toggle
is added merely to duplicate a configuration checkbox. No settings exception is needed.

## External consumers and optional contracts

Bounded ASCII/UTF16 installed source/assembly search retained in consumer-hits. Metadata-only matches
do not prove a runtime consumer, and absence from this scan is not proof of no consumers.

| Consumer/provider | Inspected binding and consequence | Decision |
| --- | --- | --- |
| Compact Hediffs `PeteTimesSix.CompactHediffs`,2031734067 | Compiled v1.6 adapter package-gates `Memegoddess.SmartMedicine`; calls Get→Dictionary<Hediff,MedicalCareCategory>, GetCare(Pawn), CreateCareMenuOptionsWithList(List<Hediff>,Hediff optional), GetElementsByList(List<Hediff>)→List<Action<Rect>> | Direct S007 composition; an external-view adapter needs its own package/shape checks. A name facade cannot satisfy the package gate; no spoof/drop-in claim |
| Mod Medicine Patch1.6 `koth.modmedicinepatch`,3550702277 | Source and selected compiled binding demand assembly SmartMedicine, Mod.settings, legacy PriorityCareComp.Get and old LabelButton(Rect,string,Hediff). It replaces the source owner's TendPriority prefix and exposes extra medicine categories | Current source lacks old LabelButton; already partial/stale binding. Do not export a fake assembly. Unsupported extended-care provider leaves affected owned care decision inactive with warning until an explicit adapter is verified |
| Pick Up And Haul - Reworked,3813653998 | Compiled ProviderDiscovery specifically requires **Dreadarm.SmartMedicine** and same-named assembly/API V1 | Different provider, not a consumer of this Continued mod. Preserve direct owned retention; do not manufacture this identity |
| Pharmacist | Source reflects Pharmacist.PharmacistUtility.TendAdvice(Pawn) without package check | Optional dynamic default advice, explicit per-condition choices win; exact active package/shape still required before support |
| Nice Health Tab `andromeda.nicehealthtab` | Seven source adapters inject disease/injury menus/icons and translation cache into DollDrawer; static current-Hediff context, brittle generated methods/locals | No installed matching package found. Planned guarded contextual adapter, no copied cache or compatibility claim |
| Combat Extended `CETeam.CombatExtended` | Source reflects CE_Utility.CanBeStabilized, Stabilize, JobDef Stabilize and concept; XML adds Doctor emergency WorkGiver priority115 even when CE absent, DefOf references use MayRequire | No installed matching package found. Optional group needs real CE job/supply validation before implementation; absent means no CE work/Defs/actions |
| Use Bedrolls `Memegoddess.UseBedrolls`,3534169351 | Source tests package only to list Bedroll for stocking | Item must also exist and be portable; later S073 owns use, no invented sleeping bonus |
| RPG Revamped `Sandy.RPGStyleInventory.avilmask.Revamped`,2478833213 | Source targets old Sandy_Detailed_RPG_GearTab methods DrawThingRow/InterfaceDrop/FillTab | Planned S132 direct UI composition; selected provider signatures not yet certified |
| Androids / DynamicPawnTable / old While You're Up | Source reflects special repair extensions, patches optional Medical column by XML shape, and rewrites an unspawned Thing target to PositionHeld for WhileYoureUp.Utils.MyDistance | Provider identities/semantics not proved. No speculative facade; shared owned holder/route rules replace internal workaround |

CE source emergency candidate: downed, eligible care at least herbal, stabilizable condition, death
under2h or earlier than bed travel, reserve/reach and ordinary faction/mental-state restrictions.
It looks for inventory-only medicine, but that query includes other donors despite documentation
suggesting medicine already carried. Job created with one medicine. Owned adapter must prove the
actual CE consumption/holder path; cannot pretend a donor inventory is the doctor's inventory.

## XML, persistence and deliberate changes

Selected XML adds stocking jobs/think tree, a temporary bed and surgery column; vanilla patches set
TendPatient nonsuspendable when missing and append Assign/optional Medical columns. These are not
target-spec guarded and optional external content is not consistently package-gated. Future owned
declarations use XML Extensions guards, merge once, preserve all unrelated columns/think nodes and
avoid globally changing job suspension simply to repair source bookkeeping.

No upstream save/settings import selected. Source keys include condition-reference dictionaries,
ignored conditions, OriginalCare, per-pawn stock dictionaries, surgery pawn set/default and temporary
spot last-used ticks. Old stock job class aliases are source-save contracts, not portable owned state.
Do not promise safe source removal with outstanding jobs/temporary beds. A later import requires an
explicit backed-up atomic migration contract. Owned state saves exact entities/targets and restores
without performing treatment, producing supplies or rewriting general care on load.

Intentional proposed corrections include group-wide reset, explicit mixed-state UI, urgent-condition
precedence within a patient, one scoped treatment permission, equal-unit range comparisons, all-source
emergency timing, real holder transfers, copy-time snapshots, native target consolidation, ordinary
physical paste dispensing, current alerts, bounded temporary-rest recovery and no independent caches.
These are proposed choices, not user-confirmed numerical tuning or claims upstream bugs were reproduced.

## Semantic balance and document review

Static Core comparators were read from `Defs/ThingDefs_Items/Items_Resource_Manufactured.xml`,
including MedicineBase. These are declared inputs, not finalized mod-list values or live evidence.

| Medicine | Potency / tend-quality cap | Mass / market value | Availability / cost boundary retained |
| --- | --- | --- | --- |
| Herbal | 0.60 /0.70 | 0.35 /10 | Ordinary healroot supply and spoilage |
| Industrial | 1.00 /1.00 | 0.50 /18 | MedicineProduction, DrugLab, Crafting4/Intellectual4; recipe1herbal+1neutroamine+3cloth,700work |
| Glitterworld | 1.60 /1.30 | 0.50 /50 | Ordinary advanced trade/reward supply; no manufacturing recipe added |

MedicineBase stack limit25 remains unchanged. The selected feature neither increases yields nor
discounts those ingredients/work, and actual native treatment still consumes one unit per valid
medicated treatment. Different allowed batches can cost more total units than mixed native care;
Lowest available can also reduce real tending quality. Surgery preserves the ordinary effect of
the medicine consumed, not equal success regardless of potency. Shared inventory capacity accounts
for the real mass of all held supplies, and no field marker creates hospital-bed quality/research
benefits. Emergency timing and scarcity can change access/travel and deserve live playability review;
the 150% gate's paste behavior is deliberately conservative pending S123. No finalized Def export,
benchmark or actual treatment outcome was substituted by these static comparisons.

Independent source/medical review found three P2 contract gaps, all resolved and rechecked: economy
permissions now constrain actual mixed urgent/minor batches, emergency timing includes the complete
multi-stop pickup/transfer/handling/treatment itinerary, and surgery wording preserves medicine's
native success effect. Independent composition review found no issues. Host inspection also
corrected the old WhileYou'reUp helper description and recorded the differing public bundled DLL.
Typed strict validation passed84items,0failed after correcting two parser line-placement errors.
All runtime tasks remain unchecked; review/validation do not prove compatibility or gameplay.

## Open questions for later

| ID | Kind | Remaining work / proposed default |
| --- | --- | --- |
| SM-Q1 | Balance and UX | Review urgent-before-care-tier correction, 2h emergency buffer, 6/12-cell detours, 8-cell collection, 150% scarcity and ordinary field scope; specs state provisional defaults |
| SM-Q2 | Runtime contracts | Derive actual semantic targets for final 1.7/native-stock/condition batches, generated actions, holder transfer, clean field transitions and effective foreign patches |
| SM-Q3 | External contracts | Inspect exact optional Pharmacist/CE/NiceHealth/extended-medicine/special-patient providers when available; certify explicit adapters or retain documented fallback, never identity spoofing |
| SM-Q4 | Composition and acceptance | Complete S080/S123/S124/S132/S134 intersections; prove medical, stock, cargo, paste and save-switch workflows, all seven languages and shared-index cost in reviewed isolated runs |

No question interrupts the sequence. Strict validation and independent document review are recorded
in the progress ledger when complete; implementation, tests and final native acceptance stay open.
