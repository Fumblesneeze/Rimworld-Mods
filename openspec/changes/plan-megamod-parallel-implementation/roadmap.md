# Coordinated implementation roadmap

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, RimWorld **1.7 beta**.
Plan revision **1**, 2026-11-07. Status: **planned; no implementation assignment dispatched**.
The [design](design.md) defines shared-component contracts and delivery protocol; [tasks](tasks.md)
is the governing gate checklist. The [feature ledger](../../../mods/megamod/docs/refinement/specification-progress.md)
remains the source-ID/research record. Individual feature specs and user decisions remain authoritative.

## How to execute and revise this plan

There are **137 feature drafts, 27 primary chunks and nine integration parts**. A chunk is a bounded
assignment spanning related source-derived features, not a single TDD step. At dispatch, split it
into one-behavior slices with exact requirement IDs, file leases and test/scenario IDs in its owning
feature tasks. The foundation's five capabilities are cross-cutting obligations, not extra S IDs.

Parts run in the default order P0 → P1 → P2 → P3 → P4 → P5 → P6 → P7 → P8. This is an integration
order, not a claim that every feature depends on everything earlier. Each part has its own frozen
contract tranche, parallel lanes and orchestrator acceptance gate. Up to three implementers may work
at once; queue extra lanes. Research can proceed earlier, and implementation can move earlier after
an explicit revision establishes satisfied dependencies. Do not accept a part until all chunks in
its recorded scope are ready and its selected combined scenarios pass with personal observation.

Cross-part enrichment does not justify a circular dependency. An earlier part uses the native
baseline and records later composition obligations below. Its initial acceptance covers only that
baseline. The later part tests the newly combined behavior and reopens affected earlier acceptance
when it changes. Do not describe a conditional future integration as already verified.

The orchestrator owns this plan and may change it on evidence without asking permission for routine
implementation choices. Update the revision log, coverage allocation, dependent contracts/tasks,
assignment leases and scenario manifest together. Notify affected implementers and supersede old
assignments. Moving work retains its outstanding tasks and explicit scope. Root safety, user intent,
honest tests and the native acceptance gate remain in force.

## Parts, prerequisite milestones and combined verification

### P0 — Activation, settings and content foundation

**Entry:** recheck the 1.7 build/profile/package gates and the foundation's historical tooling blocker.
Read the current operation registry. Resolve tooling failures under their own owner. Register honest
test environments only when their first real tests exist.

**Parallel lanes:** P0-A admission/diagnostics, P0-B common settings/presentation, P0-C content policy
and S029 drug-policy maintenance. Freeze C01/C02 activation/settings contracts first. B publishes
`settings-core` (early load/resolve, no renderer or active save) before A's `early-hooks`; A's admission
core itself has no settings dependency. Initial XML diagnostics run before conversion. A publishes
`activation` for later registrations; B finishes the scoped renderer independently. C publishes the
stable Def `catalog`, then `drug-maintenance`, then `applied-policy` with atomic drug-row publication
at the specified new-game/load boundary. Only the policy projection is filtered; excluded Defs and
existing Things retain identity. These milestones avoid an A↔B startup cycle and a P0↔P4 drug-policy
dependency. The integration writer owns bootstrap, metadata, project/test registrations and indexes.

**Gate:** native log/settings/content-selection workflows; main-menu disabled save controls, global
inheritance/save switching, content unavailable to later consumers, compatible foreign hooks, missing
target warnings and original-provider suppression at the earliest actual phase. Capture diagnostics
from initial XML conversion; test requested versus applied content choices, save switching, and
atomic drug exclusions/policy repair including a later-policy failure and active original DrugPolicyFix.
XML checks apply
with the first XML consumer; an unused XML path is not claimed verified in advance. Observe the
actual affected feature behavior in addition to startup evidence.

### P1 — One inventory view, linked storage and real physical cargo

**Entry:** P0 activation/settings/content contracts integrated. Freeze C04 query scope, C05 physical
portions/claims and C06 storage result contracts together before feature consumers code against them.

**Milestone DAG:** P1-A publishes `facts`; P1-C publishes storage-independent `portions` using native
Things/reservations. These two can progress in parallel. P1-B's `storage` consumes `facts` and
`portions`. P1-C's `hauling` consumes `storage` plus its own `portions`; P1-A search consumes `facts`.
This is not a whole-chunk B↔C dependency. The portion export must pass focused real-provider checks
before B integrates, and C cannot hand off hauling against a stub storage provider.

**Gate:** find the same physical goods through search, then use a native hauling order to move them
into a mixed shelf/stockpile group. Observe updated search/storage totals, carrying limits and visible
cargo. Exercise split/merge/partial ingestion with conserved taint/nutrition/quantities, cancellation,
threshold/refill behavior, destination restrictions, own-save reload and map teardown. Test a failed
invalidation path's documented fallback. Measure combined query/update/memory cost before optimization
claims. Later meal, ingredient and medical consumers add their own composition proof.

### P2 — Production authority and integrated recipe browsing

**Entry:** P1 facts, portions and storage available. P2-A publishes `bills` (logical identities,
group quantities, native execution boundaries). P2-B then publishes `materials` using bills and stock
facts. P2-C consumes both to build the add-bill UI. Pure selection/UI models can be developed in
parallel against the frozen contracts; final integration waits for the actual exports.

**Gate:** search/filter/sort a native add-bill view, change available/all materials, select a material
alternative, create/execute a bill, and observe linked totals and automatic production behavior. Cover
stock changes between preview and job, worker selection, cancellation, linked bench removal and
bulk-smelting equivalence to piecewise execution. Reuse P1 stock/cargo checks only where this changes
their behavior. No separate Quartermaster inventory or self-patches between bill features.

### P3 — Construction, designations and item processing

**Entry:** P0–P2 accepted default baseline. P3-A publishes construction identity/placement and
replacement ownership before P3-B integrates quality/repair/roof-safe removal. P3-C owns contextual
designation and Architect entry points; it can work independently, consuming A/B result contracts
where actions overlap. P3-D owns item-processing authorization using C05/C07 and shared material counts.
Have one designated writer for any common native-command registration.

**Gate:** player places/rotates/replaces buildings, exercises manual chair rotation within one cell,
uses floor sampling/designations, and observes native work and safe roof handling. Compose quality,
upgrade, access and plant-clearing rules; check the source-derived contracts rather than assuming
the operations commute. Recycle weapons/apparel with HP-scaled returns and destroy eligible items
through melee attacks; retain mass/ingredient accounting and real resulting objects. Compose S008
urgent orders with S021 exact cargo quantities, cancellation, mixed-stack protection and save/load. Native Architect
UI placement and required locale/visual gates remain part of acceptance.

### P4 — Supplies, physiology and consolidated medical behavior

**Entry:** P1 quantities/cargo, P2 bills and P0 settings. Freeze care-priority, supply and operation-effect
contracts before dispatch. P4-A publishes `supply` (food/medicine availability and commitments) before
its opportunistic errands. P4-B publishes `capabilities` from native physiology/needs. P4-C consumes
those exports, publishes `care` (eligibility, urgency, dose commitments) and `effects` (operation matching).
P4-A then finishes `errands` using care priority; P4-D consumes `effects` and the native surgery paths.
The core supply export must not depend on the care scheduler, avoiding A↔C recursion.

**Gate:** ordinary colonists carry and share meals, preserve packing under shortages, and do eligible
opportunistic work without defeating urgent care. Trigger Drug Response and Priority Treatment on
the same patient; observe one permitted treatment, proper doctor capability/medicine use, manual
takeover and cancellation. Click a condition and observe relevant surgery bills, including transitive
limb effects and explicit exception cases with uniform presentation. Test inherited per-condition
settings, own-save reload, physical partial-dose accounting, self-tend/stabilization and specified
emergency surgery. Include compact health and integrated bill UI in the same workflow. Verify S058
structures → terrain → traps ordering before S060 within-category construction ordering, including
native work/forced-order boundaries; medical ownership of S058 does not omit its construction scope.

### P5 — Animals, growing and environmental safety

**Entry:** shared storage/cargo, medical and contextual placement baseline. P5-B publishes `environment`
before P5-A integrates hunter/companion safety; P5-C can develop growing/terrain policies independently.
P5-A owns hunting/slaughter plus the adopted herd-wandering behavior, and coordinates pen/room use
with B. Inspect the repository's pathfinding experiment as a read-only reference; no product dependency.

**Gate:** designate a hunting station and species, observe waiting/range-triggered hunting, bonded
Attack+Haul animal following, downed-prey finishing and capacity-valid corpse delivery. Observe safe
termination in a one-cell pen/room. Compare specified slaughter skill/yield behavior through actual
jobs. Verify toddler temperature avoidance, native growth/bed ownership, linked growing/hydroponics,
blight/plant actions and Grazing Lands composition without duplicated scans or self-patches. Compose
S073 portable-bed custody/rest with S115 ownership across departure, temporary deployment, return and
explicit reassignment; use one release-on-departure setting and no duplicate or stolen bed claims.

### P6 — Colony behavior, combat and information

**Entry:** prior native needs/medical/environment/storage facts where consumed. P6-A owns mood/social
autonomy, P6-B combat/fire/classification, P6-C power/information. They can run in parallel after
shared fact contracts and C10 presentation registrations are assigned. Source-derived mood gating
and mental-break changes are direct policies within A, not patches against one another.

**Gate:** observable thought-gated behavior and scheduling; combat/fire responses under current native
target eligibility; power switching and inspection agreement. Exercise alerts/labels/right-click
indicators from their real controls, with classified entry points. Compose combat injury/fire with
medical/environmental behavior where touched, and verify content filtering/classification do not
silently hide valid actions. Skip unrelated historical scenarios; enumerate the affected set.

### P7 — Arrivals, transport and asynchronous generation

**Entry:** P0–P6 baseline and source/intent publication contract frozen. P7-A publishes `arrival-ready`
after complete legal equipment/consent/policy decisions; P7-B publishes `transport-intent` identity
and cancellation/commit boundaries. P7-C consumes these exports for generated arrivals/maps. A/B
remain independent of C's implementation: native publication is held behind the contract in C's
integration phase, not called recursively by producers. Research generator thread affinity early.

**Gate:** native trader/arrival/hostile-map/caravan workflows with simulation continuing. The notice
appears only after generation is complete and source intent still valid; abandonment, caravan removal
or changed destination discards pending work without publishing rewards/people/items. There is no
one-to-two-second assertion or hard deadline. Observe correct humanlike vacuum gear when Odyssey or
SOS2 content is supported, no free player equipment, trading area behavior and actual bulk/basic
loading with conserved physical cargo. Run separate exact optional-provider combinations; no portal
containers. Installed-only source mods do not suppress features; active originals do.

### P8 — Unified pawn inspection and accumulated integration

**Entry:** all preceding parts accepted for their recorded scope, required UI contracts stable.
P8-A integrates RimHUD, passion display, quality colors and gear presentation using current shared
facts, including P7-B physical transporter/caravan cargo. Its large contextual surface deliberately
comes after production, health, inventory and status
behavior, avoiding multiple incompatible pawn presenters during development.

**Gate:** inspect/select actual pawns, equipment, passions and needs; navigate to relevant controls,
observe current state and localized/quality-aware presentation. Exercise S133/S135 loading and
unloading through S132 gear presentation; visible cargo, accepted quantity and native inventory must
agree through partial unload/cancellation. Run the accumulated interaction set
selected from the dependency ledger, including provider suppression with unrelated consumers of the
same shared service still enabled. Reconcile all 137 coverage rows and unresolved/deferred scenarios.
Perform the final accumulated semantic/playability review. This is development acceptance, not a
release/publication instruction or a substitute for a later deliberate release regression matrix.

## Later composition obligations

Each row becomes explicit scenario IDs in the dispatch manifest. The table is a minimum, not an
excuse to omit interactions discovered during implementation.

| Earlier owner | Later consumer/gate | Additional proof required on the integrated build |
|---|---|---|
| P0-A/B/C | Every subsequent part | Activation/disabled dependencies, relevant settings scope, content availability and actual provider cases touched by that part |
| P1-A/B/C | P2, P3-C/D, P4-A/C, P5-A, P7-B, P8-A | Shared facts and physical conservation through production, processing, supplies/doses, animal hauling, transport and UI; S008/S021 urgent quantities, cancellation, mixed-stack protection and save/load at P3 |
| P2-A/B/C | P3-D, P4-D | Recipe/item orders and condition-filtered medical bills compose with the existing browser without another authority |
| P3-A | P4-C | S060 construction within-category order composes after S058 structures/terrain/traps ordering without bypassing native eligibility |
| P4-A | P5-B | S073/S115 share actual bed-claim transitions, portable rest fallback and one release-on-departure setting; explicit provider lease transfer |
| P7-B | P8-A | S133/S135 physical loading/unloading and S132 cargo presentation agree on quantities and cancellation |
| P3-C | P5-A/C, P7-A | Native designation/area controls for hunting, growing and fixed trading areas retain their contextual rules |
| P4-B/C/D | P5-B, P6-A/B, P7-A, P8-A | Environmental/mental/combat/arrival changes preserve medical eligibility and current inspection results |
| P5-A/B/C | P6-B, P7-A/C | Fire, vacuum arrivals and newly generated maps respect current environment/wildlife/terrain contracts |
| P6-A/C | P7-C, P8-A | Notifications and presentation expose completed valid state, not partially generated or stale data |

## Primary feature coverage

Every S ID appears once below. Each row links the original owning change, whose complete tasks and
observable requirements remain mandatory. Foundation capabilities apply throughout. These are
planned allocations, not implementation status. New sources require new owned specs plus a roadmap
revision; do not tuck them into a chunk without recording them.

### P0-A — Runtime admission and diagnostics

| Feature | Owning change / reference |
|---|---|
| S035 | [Better Log - Fix your errors — `bs.betterlog`](../specify-megamod-debug-log/proposal.md) |
| S094 | [Log After Def Error (Continued) — `Mlie.LogAfterDefError`](../specify-megamod-definition-diagnostics/proposal.md) |

### P0-B — Common settings and presentation

| Feature | Owning change / reference |
|---|---|
| S034 | [Readable Numbers — `7h0m4s.RimworldReadableNumbers`](../specify-megamod-readable-numbers/proposal.md) |
| S047 | [Mod Options Sort — `Superniquito.ModOptionsSort`](../specify-megamod-mod-settings-navigation/proposal.md) |
| S071 | [Better Message Placement (Continued) — `Mlie.BetterMessagePlacement`](../specify-megamod-message-placement/proposal.md) |

### P0-C — Content selection

| Feature | Owning change / reference |
|---|---|
| S020 | [Cherry picker scroll bars — `lucius.cherryscroll`](../specify-megamod-content-filter-selectors/proposal.md) |
| S029 | [Drug Policy Fix (Continued) — `Mlie.DrugPolicyFix`](../specify-megamod-drug-policy-maintenance/proposal.md) |
| S038 | [Cherry Picker — `Owlchemist.CherryPicker`](../specify-megamod-content-selection/proposal.md) |

### P1-A — Shared Thing facts and queries

| Feature | Owning change / reference |
|---|---|
| S033 | [Ctrl F - Continued — `Memegoddess.CtrlF`](../specify-megamod-thing-search/proposal.md) |
| S114 | [Select Perishable — `Danoweb.SelectPerishable`](../specify-megamod-perishable-selection/proposal.md) |
| S121 | [TD Find Lib - Continued — `Memegoddess.TDFindLib`](../specify-megamod-query-library/proposal.md) |

### P1-B — Linked storage and refill

| Feature | Owning change / reference |
|---|---|
| S005 | [**Original:** Linked stockpiles and shelves](../specify-megamod-linked-storage/proposal.md) |
| S049 | [KanbanStockpile Continued — `endag.kanbanstockpile`](../specify-megamod-storage-refill/proposal.md) |
| S050 | [Simple Stockpile Presets (Continued) — `Mlie.SimpleStockpilePresets`](../specify-megamod-storage-presets/proposal.md) |
| S098 | [Storage Stats in Description — `ferny.StorageStatsinDescription`](../specify-megamod-storage-descriptions/proposal.md) |
| S107 | [Quick Stockpile Creation — `longwater.smartstockpilecreation`](../specify-megamod-quick-storage-actions/proposal.md) |

### P1-C — Physical portions, cargo and ingestion

| Feature | Owning change / reference |
|---|---|
| S021 | [Pick Up and Haul - Continued — `Zzyxzz.PickUpAndHaulReworked`; compare Reworked — `Dreadarm.PickUpAndHaul`](../specify-megamod-batch-hauling/proposal.md) |
| S041 | [Food Poisoning Stack Fix (Continued) — `Mlie.FoodPoisoningStackFix`](../specify-megamod-food-taint-accounting/proposal.md) |
| S085 | [FoodTracker — `b0arl0ck.foodtracker`](../specify-megamod-partial-ingestibles/proposal.md) |
| S126 | [dd.Armful Haul — `dd.armfulhaul`, Workshop 3808100512](../specify-megamod-visible-haul-cargo/proposal.md) |

### P2-A — Production authority and linked bills

| Feature | Owning change / reference |
|---|---|
| S014 | [**Original:** Whole-list workbench linking](../specify-megamod-linked-workbenches/proposal.md) |
| S017 | [Better Workbench Management — `falconne.BWM`](../specify-megamod-workbench-management/proposal.md) |
| S018 | [Everybody Gets One - Continued — `Memegoddess.EverybodyGetsOne`](../specify-megamod-adaptive-production/proposal.md) |

### P2-B — Ingredients, workers and recipes

| Feature | Owning change / reference |
|---|---|
| S015 | [Bulk Smelting (Continued) — `zal.bulksmelting`](../specify-megamod-bulk-smelting/proposal.md) |
| S019 | [Most Skilled Bill Restriction — `Matheus.MostSkilledBill`](../specify-megamod-most-skilled-workers/proposal.md) |
| S026 | [Use Best Materials — `kathanon.UseBestMaterials`](../specify-megamod-best-material-selection/proposal.md) |
| S059 | [Best Mix (Continued) — `Mlie.BestMix`](../specify-megamod-ingredient-preferences/proposal.md) |
| S122 | [De-generalize Work — `Alias.DegeneralizeWork`](../specify-megamod-specialized-crafting-speed/proposal.md) |

### P2-C — Integrated production browser

| Feature | Owning change / reference |
|---|---|
| S009 | [Quartermaster — `slim.quartermaster`](../specify-megamod-quartermaster-comparison/proposal.md) |
| S010 | [Nice Bill Tab — `Andromeda.NiceBillTab`](../specify-megamod-recipe-inspection/proposal.md) |
| S011 | [Nice Bill Tab - Expansion — `HICON.NiceBillTabExpansion`](../specify-megamod-recipe-list-controls/proposal.md) |
| S012 | [**Original:** Integrated Add bill browser](../specify-megamod-integrated-add-bill-browser/proposal.md) |

### P3-A — Construction planning and access

| Feature | Owning change / reference |
|---|---|
| S028 | [Replace Stuff - Continued — `Memegoddess.ReplaceStuff`](../specify-megamod-building-replacement/proposal.md) |
| S048 | [Auto-Facing Chairs — `scarf.autofacingchairs`](../specify-megamod-furniture-orientation/proposal.md) |
| S060 | [Smarter Construction — `dhultgren.smarterconstruction`](../specify-megamod-construction-access/proposal.md) |
| S063 | [Cut plants before building (Continued) — `Mlie.CutPlantsBeforeBuilding`](../specify-megamod-construction-plant-clearing/proposal.md) |
| S129 | [Interaction Spot On X — `dragonissa.InteractionSpotOnX`, Workshop 3827781564](../specify-megamod-interaction-spot-placement/proposal.md) |

### P3-B — Quality, repair and safe removal

| Feature | Owning change / reference |
|---|---|
| S013 | [QualityBuilder Unofficial 1.6 — `hatti.qualitybuilder`](../specify-megamod-quality-construction/proposal.md) |
| S054 | [I Cant Fix That — `Mlie.ICantFixThat`](../specify-megamod-repair-knowledge/proposal.md) |
| S061 | [Smarter Deconstruction and Mining (Continued) — `Mlie.SmarterDeconstructionAndMining`](../specify-megamod-roof-safe-removal/proposal.md) |
| S082 | [Easy Upgrades (Continued) — `Mlie.EasyUpgrades`](../specify-megamod-easy-upgrades/proposal.md) |
| S097 | [Mining Priority (Continued) — `Uuugggg.MiningPriority`](../specify-megamod-mining-priorities/proposal.md) |

### P3-C — Designations and Architect controls

| Feature | Owning change / reference |
|---|---|
| S008 | [Allow Tool — `UnlimitedHugs.AllowTool`](../specify-megamod-allow-tool/proposal.md) |
| S016 | [Designations Tooltip — `CheaterEater.DesignationsTooltip`](../specify-megamod-designation-preview/proposal.md) |
| S036 | [Architect Menu Optimizer — `MRK.architectmenuoptimizer`](../specify-megamod-architect-menu/proposal.md) |
| S067 | [Add Deconstruct Button To Floor Category — `geodesicdragon.deconstructfloorcategory`](../specify-megamod-floor-order-shortcuts/proposal.md) |
| S136 | [Floor Pipette Tool — `ferny.FloorPipetteTool`, Workshop3706877822](../specify-megamod-floor-sampling/proposal.md) |

### P3-D — Item processing and material counts

| Feature | Owning change / reference |
|---|---|
| S022 | [**Original:** Weapons/apparel recycling](../specify-megamod-equipment-recycling/proposal.md) |
| S023 | [**Original:** On-site melee destruction of items with HP](../specify-megamod-melee-item-destruction/proposal.md) |
| S032 | [Show Buildable Material Count — `BP.ShowBuildableMaterialCount`](../specify-megamod-buildable-material-counts/proposal.md) |
| S096 | [Micro Designations — `avilmask.MicroDesignations`](../specify-megamod-item-recipe-orders/proposal.md) |

### P4-A — Practical supplies and opportunistic work

| Feature | Owning change / reference |
|---|---|
| S024 | [Common Sense — `avilmask.CommonSense`](../specify-megamod-common-sense/proposal.md) |
| S055 | [Meals On Wheels - Continued — `Memegoddess.MealsOnWheels`](../specify-megamod-shared-carried-food/proposal.md) |
| S057 | [Prioritize Meals over Preserved Foods — `seekiworksmod.no10`](../specify-megamod-preserved-food-preference/proposal.md) |
| S073 | [Use Bedrolls - Continued — `Memegoddess.UseBedrolls`](../specify-megamod-portable-beds/proposal.md) |
| S111 | [Room Food — `TheLordDrake.RoomFood`](../specify-megamod-room-food/proposal.md) |
| S123 | [**Original:** Routine meal carrying for colonists, excluding animals](../specify-megamod-routine-meal-carrying/proposal.md) |
| S125 | [While You're Up — current `patch1.7.whileyoureup`, Workshop 3832180338; original `CodeOptimist.JobsOfOpportunity`, 2034960453](../specify-megamod-opportunistic-hauling/proposal.md) |

### P4-B — Needs, physiology and apparel rules

| Feature | Owning change / reference |
|---|---|
| S003 | [Improved Need Indicator — `AmCh.ImprovedNeedIndicator`](../specify-megamod-need-forecasts/proposal.md) |
| S025 | [Prosthetic No Missing Body Parts (Continued) — `Mlie.ProstheticNoMissingBodyParts`](../specify-megamod-prosthetic-apparel/proposal.md) |
| S045 | [Consciously Malnourished — `ProfileName.MalCon`](../specify-megamod-malnutrition-consciousness/proposal.md) |
| S046 | [Consciousness Patch — `Vex.Consciousness`](../specify-megamod-physiological-consciousness/proposal.md) |
| S099 | [Spines Matter More Now — `Jdalt.SMMN`](../specify-megamod-spine-manipulation/proposal.md) |
| S103 | [Need Bar Overflow — `AmCh.NeedBarOverflow`](../specify-megamod-need-overflow/proposal.md) |
| S106 | [Quality Based Durability — `gabrieel1482.QualityBasedDurability`](../specify-megamod-quality-apparel-durability/proposal.md) |

### P4-C — Medical authority and automatic care

| Feature | Owning change / reference |
|---|---|
| S042 | [Auto Self-Tend — `funstab.AutoSelfTend`](../specify-megamod-auto-self-tend/proposal.md) |
| S058 | [SmarterDoctors (Continued) — `zal.smarterdoctors`](../specify-megamod-smarter-work-priorities/proposal.md) |
| S062 | [Smart Medicine - Continued — `Memegoddess.SmartMedicine`](../specify-megamod-smart-medicine/proposal.md) |
| S080 | [Stabilize Bleeding — `Defi.StabilizeBleeding`](../specify-megamod-stabilize-bleeding/proposal.md) |
| S086 | [Go-juice is medical — `orioM.Gojuicemedical`](../specify-megamod-medical-go-juice/proposal.md) |
| S124 | [Drug Response (Continued) — `Mlie.DrugResponse`, Workshop 2196401201](../specify-megamod-drug-response/proposal.md) |
| S134 | [Priority Treatment — `Dreadarm.PriorityTreatment`, Workshop3813351382](../specify-megamod-priority-treatment/proposal.md) |

### P4-D — Health inspection and explicit surgery

| Feature | Owning change / reference |
|---|---|
| S007 | [Compact Hediffs — `PeteTimesSix.CompactHediffs`](../specify-megamod-compact-health-display/proposal.md) |
| S056 | [Cut It Off — `LegendaryMinuteman.CutItOff`](../specify-megamod-emergency-self-amputation/proposal.md) |
| S089 | [Healer Mech Serum Choice — `Syrus.HMSChoice`](../specify-megamod-healer-serum-choice/proposal.md) |
| S137 | [Original: click a health condition to filter medical bill recipes](../specify-megamod-condition-medical-bills/proposal.md) |

### P5-A — Hunting, companions and slaughter

| Feature | Owning change / reference |
|---|---|
| S064 | [**Original:** Waiting/hunting spot and seat](../specify-megamod-hunting-stations/proposal.md) |
| S065 | [**Original:** Bonded Attack + Haul companions](../specify-megamod-hunting-companions/proposal.md) |
| S066 | [**Original:** Directional hunter/pen/room animal wandering](../specify-megamod-directional-animal-wandering/proposal.md) |
| S117 | [Skilled Slaughtering — `MrHydralisk.SkilledSlaughtering`](../specify-megamod-skilled-slaughtering/proposal.md) |
| S118 | [**Original:** Cooking-dependent slaughter-job yield](../specify-megamod-slaughter-yield/proposal.md) |

### P5-B — Environmental safety, children and beds

| Feature | Owning change / reference |
|---|---|
| S030 | [Brrr and Phew (Continued) — `Mlie.BrrandPhew`](../specify-megamod-environmental-self-preservation/proposal.md) |
| S031 | [Toddlers — `cyanobot.toddlers`](../specify-megamod-toddler-development/proposal.md) |
| S044 | [Auto Name Babies — `cruesoe.autonamebabies`](../specify-megamod-auto-name-babies/proposal.md) |
| S088 | [Growth Speed Makes Sense Now — `Jdalt.GSMSN`](../specify-megamod-child-growth-learning/proposal.md) |
| S115 | [Sensible Bed Ownership — `sensiblebedownership.1trickPwnyta`](../specify-megamod-bed-ownership/proposal.md) |

### P5-C — Plants, growing and terrain

| Feature | Owning change / reference |
|---|---|
| S006 | [**Original:** Linked growing zones](../specify-megamod-linked-growing-zones/proposal.md) |
| S043 | [[Kit] Graze up — `kittahkhan.grazeup`](../specify-megamod-grazing/proposal.md) |
| S069 | [Auto-Cut Blight - 1.6 — `Defi.AutoCutBlight`](../specify-megamod-auto-cut-blight/proposal.md) |
| S076 | [Linkable Hydroponics — `Yakobo.LinkableHydroponics`](../specify-megamod-linkable-hydroponics/proposal.md) |
| S077 | [Trees Don't Just Die — `azuraal.treedontjustdie`](../specify-megamod-tree-longevity/proposal.md) |
| S087 | [Grazing Lands — `avilmask.GrazingLands`](../specify-megamod-grazing-lands/proposal.md) |
| S104 | [No Burn Metal — `Unon.NoBurnMetal`](../specify-megamod-metal-fire-resistance/proposal.md) |
| S105 | [Permeable Terrain — `Owlchemist.PermeableTerrain`](../specify-megamod-terrain-filth-lifetimes/proposal.md) |
| S112 | [Scattered Stones 1.7 — `Owlchemist.ScatteredStones`](../specify-megamod-scattered-stones/proposal.md) |

### P6-A — Mood, social behavior and autonomy

| Feature | Owning change / reference |
|---|---|
| S001 | [CM Color Coded Mood Bar [1.1+] — `CrashM.ColorCodedMoodBar.11`](../specify-megamod-color-coded-mood-bar/proposal.md) |
| S002 | [Chill the Fork Out — `KT.ChillOut`](../specify-megamod-directed-recreation/proposal.md) |
| S037 | [[DN] Blunt Social Fights — `dani.BluntSocialFighting`](../specify-megamod-blunt-social-fights/proposal.md) |
| S051 | [Auto-Cast Specialist Commands — `Linnun.AutoCastSpecialistCommands`](../specify-megamod-specialist-autocast/proposal.md) |
| S068 | [All Memories Fade — `Mlie.AllMemoriesFade`](../specify-megamod-memory-fading/proposal.md) |
| S070 | [Automatic Night Owl — `ben.automaticnightowl`](../specify-megamod-nocturnal-schedules/proposal.md) |
| S092 | [Keep Converting — `Linnun.KeepConverting`](../specify-megamod-repeat-conversion/proposal.md) |
| S100 | [Snap Out! — `weilbyte.snapout`](../specify-megamod-calm-mental-breaks/proposal.md) |
| S119 | [Dubs Break Mod — `Dubwise.DubsBreakMod`](../specify-megamod-thought-gated-breaks/proposal.md) |
| S120 | [Research Whatever — `avilmask.ResearchWhatever`](../specify-megamod-automatic-research/proposal.md) |

### P6-B — Combat, fire and classification

| Feature | Owning change / reference |
|---|---|
| S072 | [Walls are solid — `Victor.WallsAreSolid`](../specify-megamod-structural-damage/proposal.md) |
| S075 | [UnLimitedArmor — `Paragon.Hanul.UnLimitedArmor`](../specify-megamod-uncapped-armor/proposal.md) |
| S079 | [Stop, Drop, And Roll! [BAL] — `balistafreak.StopDropAndRoll`](../specify-megamod-fire-response/proposal.md) |
| S081 | [Spray Transfer — `Spray.Transfer`](../specify-megamod-burst-retargeting/proposal.md) |
| S090 | [Is This A Weapon? — `S4.IsThisAWeapon`](../specify-megamod-weapon-classification/proposal.md) |
| S091 | [Keep Burning, Corpse! — `tixiv.KeepBurningCorpse`](../specify-megamod-corpse-fire-continuity/proposal.md) |

### P6-C — Power and world information

| Feature | Owning change / reference |
|---|---|
| S052 | [LightsOut — `juanlopez2008.LightsOut`](../specify-megamod-lights-out/proposal.md) |
| S053 | [Turn It On and Off - RePowered — `Mlie.TurnOnOffRePowered`](../specify-megamod-repowered/proposal.md) |
| S074 | [Auto links — `automatic.autolinks`](../specify-megamod-auto-links/proposal.md) |
| S084 | [Extra Alerts — `zylle.ExtraAlerts`](../specify-megamod-extra-alerts/proposal.md) |
| S093 | [Labels on Floor (continued) — `falconne.LabelsOnFloor`](../specify-megamod-floor-labels/proposal.md) |
| S101 | [Monument Highlight — `espio.monhighlight`](../specify-megamod-monument-labels/proposal.md) |
| S102 | [More power info — `arl85.MorePowerInfo`](../specify-megamod-power-inspection/proposal.md) |
| S110 | [Right-Click Gizmo Indicator — `CheaterEater.RightClickGizmoIndicator`](../specify-megamod-right-click-indicators/proposal.md) |
| S113 | [Scenario Searchbars — `nimrag.scenariosearchbar`](../specify-megamod-scenario-search/proposal.md) |

### P7-A — Arrivals, trading areas and vacuum protection

| Feature | Owning change / reference |
|---|---|
| S004 | [Dress for the Weather (Continued) — `Mlie.DressForTheWeather`](../specify-megamod-weather-ready-arrivals/proposal.md) |
| S027 | [Just Put It Over There — `Mlie.JustPutItOverThere`](../specify-megamod-visitor-deliveries/proposal.md) |
| S039 | [[PL] Trading Spot — `tradingpost.programmerlily.com`](../specify-megamod-trader-gathering/proposal.md) |
| S040 | [**Original:** Dedicated trading area](../specify-megamod-trading-area/proposal.md) |
| S083 | [Enemies Use Apparel Too - EUAT — `Argotis.EnemiesUseApparelToo`](../specify-megamod-enemy-apparel/proposal.md) |
| S108 | [RescueeJoinsPlus — `Ogliss.RescueeJoinsPlus`](../specify-megamod-rescuee-join-consent/proposal.md) |
| S109 | [WanderJoinsPlus — `Ogliss.WanderJoinsPlus`](../specify-megamod-wanderer-offer-details/proposal.md) |
| S130 | [Dynamic Spacefarer Policy — `asunib.DynamicSpacefarerPolicy`, Workshop 3829638797](../specify-megamod-spacefarer-policy/proposal.md) |

### P7-B — Caravans and transporter loading

| Feature | Owning change / reference |
|---|---|
| S095 | [Max Buy, Max Sell — `Leoltron.MaxBuyMaxSell`](../specify-megamod-trade-quantity-shortcuts/proposal.md) |
| S116 | [Show gender/age in caravan form dialog — `arl85.ShowGenderAgeCaravanFormDialog`](../specify-megamod-caravan-pawn-details/proposal.md) |
| S133 | [Bulk Load For Transporters — `Ilarion.BulkLoadForTransporters`, Workshop 3559292908](../specify-megamod-bulk-transporter-loading/proposal.md) |
| S135 | [Basic Caravan Loading — `LM420.BCL`, Workshop3370434966](../specify-megamod-basic-caravan-loading/proposal.md) |

### P7-C — Background generation

| Feature | Owning change / reference |
|---|---|
| S078 | [**Original:** Spread generation work across ticks](../specify-megamod-generation-smoothing/proposal.md) |

### P8-A — Unified pawn inspection

| Feature | Owning change / reference |
|---|---|
| S127 | [RimHUD — `Jaxe.RimHUD`, Workshop 1508850027](../specify-megamod-pawn-hud/proposal.md) |
| S128 | [RimHUD Passion Icons — `DanZinagri.RimHUDPassionIcons`, Workshop 3829500081](../specify-megamod-passion-display/proposal.md) |
| S131 | [Quality Colors (Continued) — `DawnsGlow.qualcolor`, Workshop 3513846773](../specify-megamod-quality-colors/proposal.md) |
| S132 | [RPG Style Inventory Revamped — `Sandy.RPGStyleInventory.avilmask.Revamped`, Workshop 2478833213](../specify-megamod-gear-presentation/proposal.md) |

## Plan revisions and execution records

| Revision | Evidence / decision | Allocation and dependency changes | Evidence/assignment impact |
|---|---|---|---|
| 0 — 2026-11-07 | User requested shared architecture, parallel chunks, orchestrator acceptance and agile plan authority | Initial 137-feature allocation; 27 chunks; P0–P8 | No workers dispatched; no runtime implementation or acceptance claimed |
| 1 — 2026-11-07 | Independent architecture/coverage review against S094, S038/S029 and existing composition specs | Phase early loader hooks before conversion; separate Def catalog from applied policy; move S029 from P4-C to P0-C; add urgent-haul, construction-order, bed-claim and transport-display gates | Tasks/design/allocation updated before dispatch; no handoff or runtime evidence to invalidate; reviewers recheck affected text |

For later revisions append: reason/source evidence; affected S IDs and old/new chunks/parts;
contract/spec/design revisions; superseded assignments and acknowledged recipients; invalidated
handoffs/scenarios/build evidence; remaining blockers and tasks. Do not overwrite history.

At dispatch record the assignment/contract manifest path under ignored `artifacts/`, lease holder,
export readiness and scenario IDs. At a gate record the reviewed candidate identity and evidence path,
accepted scope, failures/deferred cases and cleanup. `tasks.md` holds the governing gates; feature
tasks and replacement records retain the detailed completion evidence. A roadmap row alone is not
proof that a component or feature exists.
