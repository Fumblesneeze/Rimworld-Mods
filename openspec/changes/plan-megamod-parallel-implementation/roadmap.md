# Coordinated implementation roadmap

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, RimWorld **1.7 beta**.
Plan revision **27**, 2026-11-14. Status: **implementation active; scheduled-mood batch accepted; focused English batches; aggregate acceptance incomplete**.
The [design](design.md) defines shared-component contracts and delivery protocol; [tasks](tasks.md)
is the governing gate checklist. The [feature ledger](../../../mods/megamod/docs/refinement/specification-progress.md)
remains the source-ID/research record. Individual feature specs and user decisions remain authoritative.

## How to execute and revise this plan

There are **152 feature entries: 140 drafts and 12 intake entries, 27 primary chunks
and nine integration parts**. New intake needs a per-source owned specification before code. A chunk is a bounded
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
target warnings and ordinary feature-off behavior with a bounded original-loaded startup warning. Capture diagnostics
from initial XML conversion; test requested versus applied content choices, save switching, and
atomic drug exclusions/policy repair including a later-policy failure and a disabled maintenance feature.
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
containers. Installed-only source mods do not trigger the warning/disable path; known active originals
use best-effort ordinary feature-off handling, with no coexistence acceptance matrix.

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
selected from the dependency ledger, including a disabled feature with unrelated consumers of the
same shared service still enabled. Reconcile all 152 coverage rows and unresolved/deferred scenarios.
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
| S141 | [Needle-inspired all-mod option search — `ZIPender.Needle`](../specify-megamod-settings-search/proposal.md) |
| S148 | [DragSelect — `telardo.DragSelect`; intake, per-source spec pending](../../../mods/megamod/docs/refinement/2026-11-14-source-additions.md) |

### P0-C — Content selection

| Feature | Owning change / reference |
|---|---|
| S020 | [Cherry picker scroll bars — `lucius.cherryscroll`](../specify-megamod-content-filter-selectors/proposal.md) |
| S029 | [Drug Policy Fix (Continued) — `Mlie.DrugPolicyFix`](../specify-megamod-drug-policy-maintenance/proposal.md) |
| S038 | [Cherry Picker — `Owlchemist.CherryPicker`](../specify-megamod-content-selection/proposal.md) |
| S138 | [Only If 'Tolerance' Below in Drug Policy — `seohyeon.onlyIfToleranceBelow`](../specify-megamod-drug-tolerance/proposal.md) |
| S139 | [Safe Drug Policy Generator — `seohyeon.bestdrugpolicy`, idea-only reference](../specify-megamod-drug-policy-generation/proposal.md) |
| S149 | [Resource Dictionary — `Scorpio.ResourceDictionary`; intake, shared content settings, spec pending](../../../mods/megamod/docs/refinement/2026-11-14-source-additions.md) |

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
| S145 | [Meal Ingredient Filter — `NachoToast.MealIngredientFilter`; intake, bill-count spec pending](../../../mods/megamod/docs/refinement/2026-11-14-source-additions.md) |

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
| S150 | [Designator Shapes — `Merthsoft.DesignatorShapes`; intake, per-source spec pending](../../../mods/megamod/docs/refinement/2026-11-14-source-additions.md) |

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
| S147 | [Uniform Growing Zone Tool — `asmallrabbit.UniformGrowZone`; intake, per-source spec pending](../../../mods/megamod/docs/refinement/2026-11-14-source-additions.md) |

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
| S143 | [Insects Aren't Raiders — `tk421storm.InsectsArentRaiders`; intake, per-source spec pending](../../../mods/megamod/docs/refinement/2026-11-14-source-additions.md) |

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
| S140 | [Linkable Descriptions — `sk.linkabledescriptions`; intake, per-source spec pending](../../../mods/megamod/docs/refinement/2026-11-14-source-additions.md) |
| S146 | [Which Mod's Scenario Is This? — `szkkka.whichmodsscenarioisthis`; intake, per-source spec pending](../../../mods/megamod/docs/refinement/2026-11-14-source-additions.md) |
| S152 | [Relevant Stats In Description — `Mlie.RelevantStatsInDescription`; intake, shared descriptions spec pending](../../../mods/megamod/docs/refinement/2026-11-14-source-additions.md) |

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
| S142 | [Xenotype Descriptions — `lakinmanheim.xenotypedescriptions`; intake, per-source spec pending](../../../mods/megamod/docs/refinement/2026-11-14-source-additions.md) |
| S144 | [Gear Tab - Compare & Swap — `DOORAJP.GearPage`; intake, Assign entry and Simple Sidearms required](../../../mods/megamod/docs/refinement/2026-11-14-source-additions.md) |
| S151 | [Clothing Sorter — `Mlie.ClothingSorter`; intake, per-source spec pending](../../../mods/megamod/docs/refinement/2026-11-14-source-additions.md) |

## Plan revisions and execution records

### Revision 27 — 2026-11-14 scheduled-mood batch acceptance

S024-M1–M5 and direct S138 scheduled composition have completed focused RED/GREEN, independent
runtime/fixture review, package checks and four personally observed English workflows. Runtime
checkpoint43f0b71 and reviewed fixture corrections through91cb62c retain679steps/58 inspected
captures across map, caravan, layout/recovery and settings reload. The exact per-case identities,
failed-attempt dispositions, native outcomes, classified complete logs and graceful cleanup are
in `artifacts/Implementation/megamod-p0/s024-resume/native-verification.md`.

The map fixture authors' leases are closed. Independent review fixed sole-selection, caravan split
accounting and pooled-job provenance issues before final affected reruns. These are fixture fixes,
not reasons to replay unrelated completed scenarios. The scheduled producer and shared hook are
accepted only for this scope; patient treats, other S024 portions, complete S138/S139, all152 entries
and every aggregate part remain allocated. Revision26's English-only implementation policy and
final language task12.5 remain in force. No remaining source obligation is removed.

### Revision 26 — 2026-11-14 English-only implementation verification

The user retains all translations and explicitly moves full language verification to the very end
for the whole megamod suite. During implementation, run focused native batches only in English;
keep static catalog checks and the final consolidated language obligation. Earlier per-feature
language run allocations are superseded as scheduling instructions, not deleted from history.
The already-started French settings run may finish owned cleanup; no additional locale launches
or screenshot reviews are allocated now. S024 continues with its map job-provenance fixture fix
and affected English A/C cases. No product scope, package boundary or gameplay acceptance is waived.

### Revision 25 — 2026-11-14 resume the scheduled-mood batch

The user reactivated implementation after the intake/search scope commits. Resume S024-M1–M5
and direct S138 composition from the existing reviewed packet; keep the all-mod search requirement
and all 152 entries, 27 chunks and nine parts. The old paused author leases are superseded by the
bounded recovery-fix and native-review assignments in p0-dispatch. No new feature is substituted
for the unfinished batch, and no historical native evidence accepts its changed shared hook.

The repeated-skip regression was reproduced and corrected with 37 focused Harmony checks green;
independent review is clear. Native review found a fixture-only caravan stock error: the discarded
partial-ingestion split belongs to cleanup/provenance, not remaining stock. Fix that before the
integrated build and exact native batch. Root owns installation, observed acceptance and logical
commits. All native and aggregate feature gates remain open until their exact evidence exists.

### Revision 24 — 2026-11-14 all-mod option search clarification

The user explicitly requires searching all mods' individual settings, not just our options or a
supported subset. S141 therefore adopts generic Needle-style page/tab discovery with no package
whitelist or per-mod opt-in, original-control reveal and visible incomplete/failed coverage. This
supersedes revision 23's owned-only draft and S047's exclusion of foreign controls; S047 retains
page grouping/identity and normal owner editing. Owned metadata remains a fast path within the
universal search. The owning S141/S047 drafts, queue and coverage references are reconciled.

Counts stay 152 entries (140 drafts and twelve intake sources), 27 chunks and nine parts. P0-B
retains S141 with the expanded contract; no new lease, code, build, game run or acceptance is resumed.

### Revision 23 — 2026-11-14 intake while implementation is paused

Add S140–S152 once, retaining S053 RePowered and S059 Best Mix as existing entries. S141 now
specifies Needle-inspired owned settings search; the other twelve are intake, with full sequential
source research and separate owned OpenSpec contracts required before any implementation lease.
See the [decision and identity record](../../../mods/megamod/docs/refinement/2026-11-14-source-additions.md). The current total is 140 drafts
plus 12 intake entries, with the same 27 chunks/nine parts. Historical dated counts below stay unchanged.

Gear comparison/swapping enters through Assign with genuine optional Simple Sidearms integration;
the broader Assign redesign waits for user requirements. Resource Dictionary shares the content
settings surface with S038 while keeping substitution distinct from removal. S152 later consumes
owned S052/S053/S098 stat contributions directly. Native 1.7 designation overlap, shape/fertility
composition, bill counting and scenario provenance are explicit refinement obligations.

Affected handoff gates include these new IDs and require their missing specifications. Later
composition adds S148 trade/loading gestures to P7, S150/S147 accepted-cell previews to P5, S149
resource mappings to content/recipe consumers, and S151 clothing categories to later gear/filter
views. Their actual contracts must be reconciled before either consumer is claimed integrated.
No part, runtime task, current S024 batch, code lease or game launch is resumed by this revision.


| Revision | Evidence / decision | Allocation and dependency changes | Evidence/assignment impact |
|---|---|---|---|
| 0 — 2026-11-07 | User requested shared architecture, parallel chunks, orchestrator acceptance and agile plan authority | Initial 137-feature allocation; 27 chunks; P0–P8 | No workers dispatched; no runtime implementation or acceptance claimed |
| 1 — 2026-11-07 | Independent architecture/coverage review against S094, S038/S029 and existing composition specs | Phase early loader hooks before conversion; separate Def catalog from applied policy; move S029 from P4-C to P0-C; add urgent-haul, construction-order, bed-claim and transport-display gates | Tasks/design/allocation updated before dispatch; no handoff or runtime evidence to invalidate; reviewers recheck affected text |
| 2 — 2026-11-07 | Current 1.7.5000 native source inspection and first host implementation expose worker-thread logging and partial-install failure boundaries | Same 137-feature allocation. C02 publishes immutable diagnostic options to workers; C01 establishes cleanup before installation and preserves preexisting owned patches. Serialize shared Unit output leases. Refresh exact beta tooling inventory under its separate owner. See [P0 dispatch](p0-dispatch.md). | A/B/C notified; core milestones reviewed separately before adapter work. No accepted runtime evidence exists to invalidate. Native settings/drug/log widget actions still need a Gateway capability assessment; scenarios and final P0 gate remain open. |
| 3 — 2026-11-07 | Executable settings page/save scenario exposed missing minimized widget and menu controls; native source audit proved Update-time input loses edges and UI labels are not instance identities | Same feature/chunk allocation. Add separately owned Gateway minimized-window-input prerequisite before complete P0 control verification; add honest Megamod.Harmony environment for first actual patch receipt tests. Root retains integration and final acceptance. | No accepted gameplay evidence to invalidate. Host component reviews remain scoped; authored page/save scenario does not cover editing. All missing control/menu cases remain open. Current dispatch manifest records owned paths, API boundaries and source findings. |

| 4 — 2026-11-07 | Independent review and installed 1.7 loading code show Current.Game assignment and GameComponent finalization on worker threads; ExecuteWhenFinished can execute on its caller and is not a universal dispatcher | Keep all 137 obligations and P0 lanes. C02 receives immutable lifecycle requests and revokes stale tokens on workers; an admitted main-thread long-event pump owns store detachment/activation. Publish immutable globals/save snapshots for worker readers. C03 gets an early load receipt independent of later settings-UI activation. | Supersedes the synchronous settings observer/finalization adapter; B owns lifecycle pump and snapshot API, root owns atomic bootstrap, C owns early applied-policy integration. A/B/C notified. Earlier pure-core tests remain scoped; runtime adapter review must repeat. No accepted live evidence exists to invalidate. Save/load/menu and failed-load scenarios remain open. |

Revision 5 — 2026-11-07: initial finalized-Defs quiescence and successful hot reload are distinct.
Inspected 1.7 HotReloadDefs keeps Loaded true and catches body/deferred-action errors. P0-C/S038
therefore adds explicit reload/clear invalidation first, retaining immutable applied stable-key
choices and requested choices; successful same-instance catalog/UI reconciliation requires its own
proven generation receipt. Do not reconstruct the runtime on idle transitions or apply pending
save/load-only choices. C acknowledged this bounded assignment; root retains bootstrap and shared
contract coordination. All 137 features and their chunks/parts stay allocated as before. Reviewed
host components remain scoped, no accepted native evidence is invalidated, and complete P0
acceptance/recovery remains open. The separately owned Gateway input and failed-run evidence
corrections are prerequisites for reliable native verification, not accepted product behavior.

Revision 6 — 2026-11-07: the reviewed Gateway checkbox, native console and post-layout replacement
workflows have passed. The actual S038 native selector test now reaches the ordinary settings page
and fails because central content composition is absent. Root therefore composes the already
reviewed common settings, protected warning sink and first incident catalog/UI/guarded runtime as
one diagnostic tracer before adding early XML diagnostics. XML fixture runner integration proceeds
under its separate tooling owner. S071's reviewed registration observer/atomic batch is handed off
separately from its required native frame/window identity boundary; its UI remains unregistered.
These narrower implementation tranches preserve all P0-A/B/C obligations and the final integrated
part gate. Only tooling has native acceptance; the product failure is RED evidence, not acceptance.
Root owns central registrations, and affected implementers have been notified of the leases.

Revision 7 — 2026-11-07: the composed S038 page now visibly renders native incident choices. Its
selector diagnostic exposed a Gateway pointer-lifetime defect: a separate native probe proved that
the real button opens a FloatMenu, then immediate pointer restoration closes it before follow-up
input. The separately owned Gateway change now adds explicit workflow pointer ownership, preserving
standalone clicks and requiring exact restoration evidence after failure, cancellation or expiry.
Root owns shared scenario steps, original-deadline propagation, composition and final runs; the
input implementer owns provider/automation guards and the native popup probe. The S038 scenario
uses that same scope rather than altering product menus. Independent provider review identified
pre-press ownership and reentrant-restoration gaps; they must be fixed/reviewed before native GREEN.
XML fixture integration also remains gated on independent recovery-review fixes before diagnostic
loader runs. S071 advances through a separately reviewed queued-registration timing boundary.
All137 features, P0 chunks and final part gates remain unchanged. Prior Gateway native evidence
supports its previously tested build only; the new provider requires fresh native evidence. The
composed page observation does not accept selector editing, S038 or P0. No accepted product
evidence is invalidated because none exists yet.

Revision 8 — 2026-11-07: reviewed Gateway scoped-pointer native evidence and XML-runner recovery
reviews permit the next diagnostic tracer. The S038 selector/global-checkbox workflow now passes:
root observed both native menus and the requested/pending/restored incident choice. Full S038/P0
acceptance stays open, including save/reload and actual incident behavior; a partial final native
shutdown warning also prevents calling the run log clean. P0-A now authors actual XML-loader log
selection/copy scenarios while root composes the existing S035 warning-capture and S094 context
owners before XML loading. P0-B separately implements an active-pass early-consumption veto; it
does not replace the outstanding caller-order boundary, and placement stays unregistered. Feature
allocations and part gates are unchanged. Later central composition invalidates affected runtime
acceptance until fresh reviewed evidence; earlier captures remain scoped historical observations.

Revision 9 — 2026-11-07: the actual XML-loader tracer reproduced two native fractional-integer
warnings and the deliberate missing-type error, then failed because startup context composition
was absent. Root added that composition after RED; independent review found no issue. The same
run exposed a separate verification-tooling prerequisite: the final log scanner rejects the
intentional fixture error even when its diagnostic is the expected result. The selected-fixture
tooling owner therefore adds exact-message/count expected-error accounting tied to the validated
fixture plan, while preserving failure for missing, additional or unexpected errors. This is no
general log ignore and does not remove the required error case. P0-A's native log scenario uses
observed native row/scrollbar clicks and Copy, not state assignment. Source/output leases remain
separate and canonical builds/runs wait for the tooling handoff. All feature allocations, normal
cleanup invariants and native part gates remain unchanged; no whole feature is accepted.

Revision 10 — 2026-11-07: the reviewed fixture accounting now passes its first actual loader run,
but S094 still lacks context. The retained startup fallback identifies a false ambiguity in native
Error overload discovery: an unrelated Exception overload is counted before its message shape is
checked. P0-A owns the narrow semantic-selection regression/fix and independent recheck; another
native run waits for that reviewed handoff. The authored native log/Copy scenario and clipboard
restoration helper have completed independent review, but have not run. P0-B's outer event boundary
is host-tested/reviewed; it advances to native binding/composition while a separate read-only lane
resolves current colonist-bar preparation and the common editor. Their feature-local bounds receipt
is prepared once per message collection and only revalidated before commit. Automatic clearance is
still required, not replaced by a manual-only milestone. Root retains central/project integration;
Harmony output leases serialize the two implementers. All137 allocations and part gates remain;
the loader failure accepts no feature, and later fixes require fresh native evidence.

Revision 11 — 2026-11-07: the Error-overload correction and native clipboard workflow passed
independent review. The initial default-loader/native selection/Copy case now passes all 66 steps,
with all 12 screenshots personally inspected. A real intermediate stdout-sharing failure was fixed
and independently reviewed with open-writer/tail regressions; the fresh complete runner passes,
including exact expected-error accounting, fixture cleanup and unchanged normal configuration.
This proves the initial diagnostic tracer on its recorded build, not S035/S094 or the P0 gate.
P0-A next prepares native settings-toggle/hot-reload coverage; a narrow separately owned Gateway
debug-action E2E projection is its prerequisite. P0-B's message composition and pure bar-preparation
components have completed independent review but remain unregistered. B is split into a reviewed
Entries-shape milestone followed by execution/current-layout authority, and a common paired-offset
editor. Automatic clearance, preview and S001 composition remain required. Root owns central links,
registrations and all builds/runs; delegated Gateway composition has an explicit narrow lease.
Affected implementers were notified. All 137 features, 27 chunks and P0–P8 gates remain allocated;
changed runtime compositions require new affected evidence, and no whole feature is accepted.

Revision 12 — 2026-11-08: exact installed Harmony 3 inspection found inner rewriting after ordinary
transpilers, followed by optional persistence rewriting. Current last-transpiler checks cannot by
themselves certify these later effects. Add a C01 shared admission milestone for published inner
effects on each consumer's required operations, including inserted observers. Recompilation already
revokes existing generations; renewed publication must also validate the new effective effects.
Do not run foreign patches speculatively or reject a target merely because another owner/inner patch
exists. Relevant unknown effects need specific fallback diagnostics. Persistence rewriting remains
an explicit separate unknown until inspected. Initial consumers are S035/S094 logging/conversion
and S071 drawing/registration/bar reads; outer lifecycle/refusal observers get their own effect
assessment. These guards' prior reviews remain evidence for their inspected ordinary-patch scope,
not proof of inner-patch compatibility. No complete feature has been accepted.

The S071 design review also removes an unnecessary planned proof of the private layout algorithm.
The observable requirement is clearance of the bar actually rendered in the current GUI pass.
Replace private-calculator production and unconditional out-field-address milestones with coherent
prepared geometry, its effective renderer consumption, visibility, and stability until consumption.
Retained geometry can be compatible when the renderer uses it; numerical change is not freshness.
Keep a particular call/address check only if a demonstrated ambiguity still needs it. Existing
execution receipts remain partial and unregistered; reassess their necessity before composing them.
Do not delete or weaken first resize/UI-scale/map/group display scenarios, S001 integration, labels,
weapons, frames, preview or native interaction evidence. This supersedes the proposed production
plan in the local S071 handoff, not the observable feature contract.

Root owns shared contract/registration coordination and native runs; narrow implementer leases are
updated in P0 dispatch. The reviewed S094 toggle/reload case and corrected owner-bound fixture
were used in two native runs; the first timed out after the command, and the diagnostic rerun passed
163 steps but left critical blank captures. Root recorded that visual gap and assigned bounded native
readback proof to Gateway. The initial selection/Copy case also needs an affected helper rerun. Typed
debug-action projection passed review after argument/UTF-8 corrections. Later legacy-loader coverage
has a read-only explicit test-metadata plan, not an implemented launcher variant. All137 features,
27 chunks and P0–P8 gates remain allocated. Affected implementers are notified; changing relevant
runtime guards will require fresh reviewed-build evidence for the affected scenarios.

Revision12 implementation follow-up: the first C01 NativeLog reporting slice now has51 focused
Harmony tests passing and is frozen for independent review. Its inspected synchronous provider scope
does not include converter Enter/Exit or generated persistence. S071's pure collection/later-commit
split has24 affected Unit tests passing and is independently reviewing; the bridge remains
unregistered/nonmoving until actual prepared-bar and fallback barriers own its pending lifetime.
The old six-argument completion fixture caller was adapted under an explicit narrow lease after a
shared-build preflight failure; that zero-test build is not behavioral RED. No part/feature closes.

Revision12 reviewed follow-up: C01 NativeLog fixes are61/61 focused Harmony GREEN and independently
rechecked CLEAR. S071's pure timing split is24/24 Unit plus16/16 simulated-barrier Harmony GREEN,
reviewed CLEAR. Root committed the isolated Gateway screenshot fix at6c2324c, with33/33 focused
checks and independent review; unavailable-frame native recovery remains open. On the reviewed
integrated build, the product reload now passes163 steps with inspected off/on controls, plain and
contextual warning details, and a later unrelated Options error without stale context. Readback
receipts and cleanup are retained in artifacts/Implementation/megamod-p0/native-reload-reviewed.md.
The initial helper rerun reached native Copy and safely refused its unsupported clipboard snapshot
precondition; no clipboard contents were discarded and Copy remains open. Root recorded the user's
incremental checkpoint permission at9893d90. No whole feature or part is accepted.

P0-B now owns pending bridge lifetime and one C01-admitted prepared-barrier handoff, still
unregistered, with a real failing intervening-layout/first-consumer Harmony test. P0-A plans only
the minimal safe observer snapshot exports needed by S071; runtime changes await a coordinated
lease. Complete geometry, S001, fallback and older body/observer admission migrations remain due.
This is a narrowed implementation dispatch within revision12, not a scope reduction.

Revision12 prepared-handoff follow-up: S071's first actual prepared-barrier and pending bridge
lifetime slice is41/41 focused Harmony GREEN and independently rechecked CLEAR. Review exposed
an early bar consumption before message collection; the corrected active-envelope veto prevents
later movement and permits the next ordinary invocation. Admission is reused only for an unchanged
compiled generation; recomposition still revokes it and busy new generations retry. A real inherited
getter fixture also removed an unnecessary declaring-type restriction. Native geometry, fallback,
render-consumption stability, S001 and registration remain due; no S071 acceptance follows.

P0-C's native incident save-override scenario is authored, independently reviewed and dry-run
compiled. It retains actual refusal before reload, native settings/save/load actions and physical
chunk arrival afterward, with current owned-policy checks that exclude an accidental vanilla
fallback pass. The first isolated run passed38 steps; root personally inspected all8 captures,
including both refusals and the physical chunk after reload. Normal configuration was unchanged
and cleanup passed. This is component evidence, not P0 acceptance.
P0-A receives a narrow immutable Prefixes/Finalizers
snapshot export lease after that run's build/deployment freeze; S071 consumer migration follows
separately. The C01 provider reader, NativeLog and converter remain outside this export slice.

The narrow Gateway expected-refusal tooling is now checkpointed at3a4830d after its20 focused host
checks, independent closure audit and the actual C11 refusal/success workflow. Only that tooling
task closes. A's immutable outer-role export passed68 focused checks; independent review required
method-attribute constructor coverage, then rechecked the strengthened7-case fixture CLEAR with
unchanged runtime. B now migrates only MessageGuiEventPatch and its fixture first. Scope/consumption
readers and draw/registration body admission follow as separate slices. C12 native Def-reload
completion research found no adequate built-in success aggregate; its minimal phase receipt plan
must still be paired with catalog/UI reconciliation before the existing unavailability latch clears.

Event migration then passed34 affected checks and independent review. The revision12 obsolete
Entries/Execution source experiments and their two dedicated fixtures were removed after a
no-caller audit; current prepared/event/bridge requirements remain. Independent retirement review
was CLEAR, retained Prepared checks passed10/10, and the product build had no warnings/errors.
A next owns cleanup-sensitive converter publication/admission; B's remaining outer readers wait
for that shared-helper freeze. No gameplay obligation or full feature is removed by this cleanup.

Revision12 concurrent follow-up: the converter reached120 focused passing checks, then independent
review found concurrent admission loss, metadata-refusal repatch failure and nested false-provenance
risks. A owns their focused correction; the affected S094 native rerun waits. B's disjoint S047 pure
settings chooser reached16 passing checks and awaits independent review before native integration.
C proceeds with S029's first actual native policy storage adapter, supported by a root-owned
`Megamod.Defs` environment for scoped constructed definitions. This avoids putting native data in
Unit or Harmony. No S029 hooks are enabled yet; early preset/load recovery and the native Assign
entry workflow remain dependencies. These are narrower P0 assignments, with unchanged feature
allocation, acceptance gates and later-composition obligations.

Revision13 — truthful nested diagnostic coverage. Independent review reproduced a rejected nested
conversion borrowing its parent's definition context. Parent-generation checks alone cannot cover
the other loader or a child whose owned body wrapper was removed. P0-A now owns one atomic S094
family with both body adapters and native entry guards DDI-GUARD-NEW/DDI-GUARD-LEGACY, backed by one
C01 fixed-pair metadata inspection interval. Guards observe external game targets, never owned
helpers. When nested coverage is untrusted, hide that frame's context without losing its original
cleanup token; ordinary diagnostics and independent S035 warning capture remain available. Full
removal of all observers is explicitly outside observable coverage. The first candidate has144
focused Harmony checks and5 scope checks and is under two independent reviews; it is not accepted.
All affected older S094 native evidence must be rerun after reviewed fixes and the final build.

During that review, B migrates only the next S071 Scope reader against the frozen single-target
snapshot API; the new pair API does not change it. B owns Harmony test output while reviewers are
read-only. Any shared-helper correction requires revalidating affected consumers. S047 pure chooser
projection is independently reviewed at18/18, with current native/provider entry research retained;
native chooser code is still pending. C's S029 storage6/6 and catalog8/8 are independently reviewed,
with a separate honest Def host tier; native availability/lifecycle and entry workflows remain due.
No feature allocation or part gate changes. Root owns central integration, builds and native runs.
Standalone debug-action CLI/MCP support is checkpointed at1832bf4 after51 working-tree and45 isolated
source-export checks; this does not accept its separate native workflow or any product feature.

Revision13 review closure: the first family candidate exposed three P1 gaps (stale-body entry,
pre-guard reporting and guard-only removal). Focused corrections now pass58 affected Harmony and7
scope Unit checks; both independent rechecks are CLEAR. Removal detection is limited to fresh
inspection, so unchanged top-level cached entries may defer it. The documented removal-plus-earlier-
reporting interval remains unsupported. S071 Scope migration and a separately reproduced cleanup
failure are independently CLEAR at60 affected checks; completion is attempted even when metadata
inspection throws after acquisition. Consumption migration remains next. Root has built and locally
installed the reviewed product without warnings; a fresh default-loader settings/reload tracer is
next, with initial Copy still subject to its unchanged clipboard preservation precondition. C prepares
only the next preset-entry target/design while this binary is frozen. No feature or part is accepted.

Revision14 — close the outer-reader migration and resolve concrete native prerequisites. The
reviewed converter family passed the163-step default-loader reload tracer with root's direct image
inspection and complete cleanup. Initial Copy is still subject to its clipboard-preservation
precondition; legacy loading needs the native process flag before initial loading. P0-A therefore
prepares the separately owned EndToEndHost change select-e2e-native-xml-loader, with fixed declared
mode, separate groups and runtime mode rejection before arrangement. No arbitrary process arguments
or direct loader calls substitute for native loading. Product runtime changes are not part of that
tooling lease. Root retains launch/build/install and verification ownership.

P0-B's Event/Scope/Consumption outer readers are now independently reviewed. Consumption's2RED→2GREEN
and56 affected checks close that scoped host migration only; next is the concrete native renderer
integration plan, without restoring retired private-layout experiments. P0-C's preset-entry guard
now has a draft target and bounded observable contract; its next dependency is actual finalized
main-thread baseline publication before worker factories, not another generic reconciliation helper.
Only source/lifecycle research is authorized until that timing is resolved. All137 feature drafts,
27chunks, part assignments and acceptance obligations remain; no whole feature or P0 gate closes.

Revision15 — integrate the reviewed P0 prerequisites in small native slices. The S029 preset guard
has36 focused passing checks and independent CLEAR review; root now owns initial native baseline
publication at the already inspected post-CallAll callback, with one Def-lifecycle instance shared
by incident and drug consumers. A narrowly leased adapter reuses NativeDrugCatalog and is tested in
the existing Defs environment; no second content coordinator or worker-side scan is introduced.
The original provider still suppresses only overlapping owned behavior. Early publication supersedes
idle-only initialization for this prerequisite; successful reload recovery remains separate.

S071's Draw, queued-registration and Entry WindowStack fallback are reviewed host components.
The updated local skill exposed the same metadata-exception issue in the prepared-bar observer;
that exact regression is assigned before further geometry integration. Root retains central settings,
registration and native acceptance. Automatic bar clearance is still required and unimplemented.

The separately owned XML-loader tooling has correct default/legacy dry-runs with identical fixtures
and no game mutation. Checkpoint isolation exposed a host test coupled to an untracked product
scenario project; its narrow fixture correction is assigned without changing loader behavior.
The current clipboard cannot be safely preserved by the native Copy scenario, so no Copy acceptance
or new game run is claimed. These source changes invalidate affected earlier runtime evidence;
all feature/part gates and later composition obligations remain open.

For later revisions append: reason/source evidence; affected S IDs and old/new chunks/parts;
contract/spec/design revisions; superseded assignments and acknowledged recipients; invalidated
handoffs/scenarios/build evidence; remaining blockers and tasks. Do not overwrite history.

At dispatch record the assignment/contract manifest path under ignored `artifacts/`, lease holder,
export readiness and scenario IDs. At a gate record the reviewed candidate identity and evidence path,
accepted scope, failures/deferred cases and cleanup. `tasks.md` holds the governing gates; feature
tasks and replacement records retain the detailed completion evidence. A roadmap row alone is not
proof that a component or feature exists.

Revision15 observed milestone: the initial preset/Assign tracer now has reviewed native evidence
(43 steps, ten root-inspected captures and complete cleanup). S029 task 2.5.3 closes only that scope;
original providers, full reconciliation, save/load and aggregate P0 remain open. The next scenario
uses the existing save/load command to check edited preset retention and loading constructors.
This is a narrow continuation of P0-C, with no new shared subsystem or changed feature intent.

Revision16 — user additions and compatibility scope correction,2026-11-08. Add S138 Only If
'Tolerance' Below in Drug Policy and S139 Safe Drug Policy Generator to P0-C. Research sequentially,
documentation first, and specify each separately. S139 supplies the idea only: independently design
safe generation around S029's current catalog, preserved edits/identities and failure isolation.
Retain later direct composition with S024 scheduled restrictions and S124/S134 dosing; no second
policy store, internal patch stack or new shared cache is justified by these additions.

The user removes original-mod drop-in/API compatibility investigation, facades and acceptance runs.
Keep canonical active-ID detection only to log a startup warning and apply the normal effective
feature-off settings path on a best-effort basis. This does not certify coexistence, preserve the
original's APIs or permanently rewrite user choices. Game-target predicates, honest tests, genuine
external integrations and own-save continuity remain required. Active feature artifacts and coverage
are being reconciled; historical source findings are retained as evidence, not acceptance demands.

The reviewed unchanged-catalog S029 persistence continuation passed with the initial tracer in
group `20261108T104425075Z-09a7bfcbed474ee385e2073e8ef3f831` (98 steps/20 root-inspected images).
Only tasks2.5.3/2.5.4 close; content repair and wider P0 stay open. See
`artifacts/Implementation/megamod-p0/drug-native-preflight/persistence-verification.md` for exact
identities, warning classification, log-tail caveat and cleanup. Later runtime edits require fresh
affected evidence. S071's next geometry participant must extend its one existing prepared transpiler;
the proposed disjoint second-transpiler lease was withdrawn before edits. Root retains that expansion,
central wiring and native acceptance; current agents reconcile docs and research the new additions.

Revision16 drafting closure: S138/S139 now have separate researched proposal/design/observable-spec/
task sets and replacement reports. This brings the draft queue to139; calculations, route/state
contracts and atomic creation listed in their tasks still block affected implementation. No runtime
source was changed. The original-mod policy is checkpointed separately at310e094 after independent
review and162-item strict validation of that exact five-path export. The new drafts and broader
document reconciliation have separate review/validation records; that commit does not contain them.

Revision17 — implement the clarified ordinary feature-off path, and resume the corrected S071
participant slice. C02 first receives an immutable startup override for explicitly identified Boolean
feature switches, separate from requested preferences and persistence. S035/S094 immutable option
sources consume that same effective resolution. Root owns provider-to-setting mapping, bounded
startup warnings, removal of their old provider gates and preference UI composition. Content and drug
features need their own specified ordinary master-off path; disabling an exclusion by forcing its
`allowed` value false would have the opposite effect and is not authorized. No original API or
coexistence matrix is restored. Foundation task3.7 and the P0 gates remain open.

The S071 implementer may extend the existing prepared transpiler and its fixture, adding one
non-owning entry-consumption participant. The participant shares the actual incoming stream,
generation and C01 publication snapshot while keeping geometry authority separate from timing
handoff. The first scope is host integration and its real inner-rewrite fallback; runtime geometry,
invocation lifetime, full renderer coverage and central registration remain with root. This replaces
the withdrawn second-transpiler assignment without removing any visible-layout requirement.

Exact leases and RED/GREEN handoffs are recorded in
`artifacts/Implementation/megamod-p0/revision17-dispatch.md`. Unit and Harmony output leases are
separate; root retains builds/install/native runs. Changed diagnostic composition will require fresh
affected native evidence after review. Prior S029 evidence retains its narrow scope until an affected
runtime path changes; neither lane closes a feature from host tests alone.

Revision18 — user requests continued implementation in batches, grouped testing/fixing and logical
commits. Resume the existing P0 working tree rather than rebuilding its completed prerequisites.
P0-C owns ordinary content and drug-policy master switches, their application timing and preserved
preferences; root composes their original-provider mapping through C02. P0-B owns the next S071
prepared-hook control-flow/geometry tranche and assesses usable manual-offset registration. Existing
single publication ownership is retained. The Gateway prerequisite tree receives a separate current
review and tooling checkpoint. Root owns central registrations, projects, build/install, independent
integration review and native execution after the selected batch is ready. Group affected checks
and fixes at batch boundaries; no full repository regression is implied. All 139 feature obligations
and unfinished part gates remain. Changed runtime paths invalidate affected prior native evidence.
Concrete current leases and scope are recorded in p0-dispatch.md; revision17 assignments are closed.

Revision18 clarification: the user confirmed retaining individual incident exclusions in a dedicated
Content selection browser. The incident-only tracer must no longer populate the general settings
list. Reuse the common scoped editors inside that browser; preserve all existing choices. The current
installed beta is 1.7.5005 rev1082 (Steam build 26571351), so development package pins are refreshed
from the actual installation and all affected native evidence must be regenerated on that build.

Revision19 — continue the user's batch implementation and logical checkpoints from bfe5e0e.
The previous content/master-switch and manual message runtime batch is committed; its native
workflows remain due. Root reviews and verifies the inherited Gateway mouse-projection correction
as a separately owned prerequisite. P0-B adds the required inert settings preview and explicit
automatic-clearance status using the existing common settings/placement calculation. P0-C advances
S138's lossless choice/route rules and exact native policy-row state through S029's existing
capture/apply/rollback transaction. Native tolerance hooks, UI and save serialization remain
explicit subsequent work, with no enabled enforcement claim. Existing automatic bar clearance,
all 139 feature obligations and P0–P8 gates remain allocated. Revision18 leases are superseded by
the revision19 section in p0-dispatch.md. Root retains central wiring, independent review, builds,
installation, native evidence and commits; affected prior runtime evidence must be regenerated.

Revision19 checkpoint: the two implementer leases have returned. The reviewed Gateway fix and S138
state tranche are committed separately. Root's integrated S071 preview/startup correction has
bounded English native evidence, including actual Reset/enable/disable actions, and the affected
S029 native save/reload regression passes. A failed preview screenshot attempt is retained alongside
the passing unchanged-build retry. The local revision19 ledger records build-specific evidence and
the bounded semantic pass. No automatic geometry, tolerance enforcement or whole-part acceptance
is inferred; all remaining obligations and the preceding unrun content cases stay allocated.

Revision20 — the continuing goal starts from clean d1607aa after the reviewed revision19 checkpoints.
Advance S071 from manual placement to the concrete current native bar-geometry adapter under its
existing single prepared owner. Extend S138 from detached state into native copy/save persistence
and its contextual controls, retaining the atomic enforcement/editor requirement. Resolve native
eligibility timing from the owned contract and inspected game paths before decision hooks; do not
invent a shared-driver ingestion veto. A partial storage handoff cannot expose a falsely enforced
limit or satisfy the full feature. Later S001/S024 compositions remain allocated. Workers own
disjoint feature paths and coordinate host output leases; root owns shared registration/projects,
integrated review/build/package, native scenarios and logical commits. Group affected regression
and fixes at the batch boundary. All139 feature obligations, prior unrun native cases and full
P0–P8 acceptance remain active; no scope is removed to fit a turn.

The third revision20 slot resolves S139 DG-Q1–3 calculation/native-content research in parallel,
with only its owned contract/research documents leased. This prepares the required generator
implementation instead of bypassing its numeric prerequisites. DG-Q4 atomic publication and all
runtime/native gates remain; no algorithm or universal-safety claim follows from research alone.

Revision21 — continue the reviewed revision20 checkpoints with two disjoint implementation batches.
S138 owns its feature implementation, matching focused host tests, seven `DrugTolerance.xml`
catalogs and owned specs/target documents: finish editor-call machine-arithmetic and reachable-use
admission, then contextual Apply/Cancel controls and the atomic scheduled/direct-joy/social/editor
runtime. Existing persistence and fault fixtures stay root-owned. S139 owns its feature/tests/specs:
finish the staged native transaction's complete validation, intended-choice verification, disposal,
ID/failure/rollback cases, then constructor/getter admission. Its narrow shared C01 metadata-only
projection needs an explicit shared-source milestone and independent review before consumption;
existing publication/failure semantics remain unchanged. Root retains central registration,
project links, canonical builds/packages, native scenarios, integration and commits.

Source/test work can resume after the current native run has built, deployed and launched its
reviewed immutable package. Workers cannot build/install the product or mutate that package; their
source-linked host outputs stay separate. S138 initially leases Harmony/Unit outputs; S139 starts
with Defs transaction cases and coordinates later Harmony/C01 work before compiling. This shortens
the source freeze without changing the identity or acceptance of the already running process.
The observed native late-load cleanup limitation is recorded under S138, with exact fixture
accounting and required visible rejection; it does not remove native acceptance or promise recovery.
All139 feature obligations, P0–P8 gates and later compositions remain allocated and unaccepted
until their own evidence exists.

Revision21 allocation update — the reviewed shared inner-callback projection is committed before
its S139 consumer. S138 runtime/UI and native geometry admission now use disjoint implementer
leases: runtime owns current policy/row identity and contextual Apply/Cancel; geometry owns the
effective upstream window/sidebar/header/row producers and their currentness. Both remain one
atomic feature at integration. Inspection found that captured-window native clicks are available,
but typed text still uses desktop input. The S139 implementer temporarily owns the narrowly scoped
Gateway native focused-text prerequisite so numeric editor and generator entry can be verified in
minimized runs. It must use the actual native input path with an owned contract, focused tests and
independent review; direct draft setters cannot substitute. Constructor/getter admission and the
remaining generator runtime/UI resume after that prerequisite. Root retains central composition,
native scenario execution and final visual observation. This supersedes the immediate S139 guard
sequence only; no feature, acceptance case or part obligation is removed.

Revision21 follow-through — after the C01 projection and generator preference UI checkpoints were
reviewed and committed, a separate S139 implementer takes the constructor/getter admission lease
while the Gateway author resolves the reviewed native-text prerequisite. This uses the newly free
slot without overlapping source ownership or delaying the input fixes. The new lane owns only
generation Harmony adapters, matching focused tests and its contract sections; root retains all
central links, runtime integration, product builds and native evidence. Coordinate the Harmony
output lease with the Gateway's concrete callback-state regression. Existing transaction and
preference implementations remain intact. S138's reviewed runtime/geometry await native execution;
all unrun feature, composition and part obligations remain open.

Revision21 native-route allocation — the S139 constructor/getter checkpoint6a71db4 is independently
reviewed and compiled but remains unregistered. While the Gateway author fixes retained cleanup
diagnostics, a separate S138 lane researches actual scheduled/direct-recreation pawn workflows.
Its initial lease is local research/scenario planning only; root releases new fixture source after
the editor bundle is frozen for its run. This avoids changing a compiling E2E project while root
owns native text and editor verification. The lane cannot launch games, install packages or accept
its own scenarios. Social/patient/refill, settings, language and all other pending feature cases
remain required; this allocation selects the next executable routes without removing those cases.

Revision21 next batch — the focused-text prerequisite, English tolerance editor and selected
scheduled/direct-recreation routes now have reviewed native observations. Their exact builds and
limits remain in the local ledgers; no full S138/P0 gate is closed. S139 resumes with the first
bounded effective-decay contribution proof from its six-stage native-facts plan. A separate S138
lane prepares social-drink/drinkless scenarios under ignored artifacts. Root owns compiled fixture
integration, build freezes, native execution, observation and logical commits. S139's catalog
authority, remaining decay/dose/profile/calendar proofs and final UI/publication remain allocated;
incident-only content receipts cannot substitute for drug-catalog authority.

Revision21 continuation after3666ca0 — the initial shared catalog, pawn projection and scoped
positive-tolerance boundary are checkpointed. Root batches numeric100/Any and Tea-to-Yayo native
verification while two disjoint S139 lanes prepare seven-method initialization proof and consumed
definition-input currentness. The latter starts as an artifact-only draft lease; its author no
longer serves as that implementation's independent reviewer. Exact scopes and source-freeze
release conditions are in `p0-dispatch.md`. The shared initial catalog is not an applied-save
exclusion receipt, and detached input snapshots do not prove native Human/static-curve binding.
Root retains integration and final observation. This sequencing preserves all139 features and
every remaining F0–F5, native composition and P0–P8 acceptance obligation.

Revision21 continuation after526ed1e — the reviewed consumed-input snapshot and deterministic
initialization checkpoints are committed with focused host evidence and remain unregistered.
Root takes the D/E save-context native batch. The next disjoint S139 lease narrows rate work to
age, Core expansion flag and ordered rate consumption; stage membership awaits exact closed
generic/interface/BCL dispatch and selected-runtime inlining evidence. The installed1.7 game
ships Waylith CLR; installed-file inspection does not establish a live process's call edges.
`p0-dispatch.md` records exact paths and deployment-before-source-release ownership. Native
Human/static-curve binding, applied-save exclusions, full F0–F5 admission, all139 features and
the P0–P8 gates remain required. No prior native evidence is reused to accept a changed runtime.

Revision21 after51c839a — the corrected save-context fixture models the native initialization
tick and observes fresh loaded objects without lifecycle-pumping getters. D's native dormant
override/inheritance workflow is accepted for its exact one-save scope; E runs on the same frozen
deployment. Two disjoint compiled lanes now advance the age/flag/rate body proof and full-grown
Human input selection/origin reader. The latter reuses the existing F0 DTO through a narrow
host-safe extraction, with root retaining the Defs project link. Explicit file/output leases are
in `p0-dispatch.md`. Broader native stages/BodySize, curve initialization, current applied exclusions,
F0–F5 composition, all139 features and every P0–P8 acceptance gate remain open as allocated.

Revision21 aftera82beb2 — D/E's one-save workflows are recorded atf0d57f1, and the reviewed Human
origin/rate checkpoints are committed with a green integrated canonical package. The next native
slice exercises main-menu disabled save controls and actual A→B→A/B identity changes. Native Entry
menu input is the concrete missing prerequisite; a bounded Gateway lane implements it while a
separate artifact-only lane prepares the full S138 scenario. S139 continues stage-membership
research with closed generic/interface dispatch and actual runtime proof kept distinct. Tooling
checkpoint3697b57 provides exact same-group test batches without selecting unrelated scenarios.
The dispatch document records edit/output leases. This changes sequencing only; no feature, native
obligation, original user decision or P0–P8 gate is removed or accepted from these host checkpoints.

Revision21 stage correction — an owned host canary proved closed generic code-sharing interference:
repatching one closure changed its sibling while that sibling's old publication remained current.
The planned stage observer expansion is superseded by a positive severity leaf and fail-before-
acquisition protection of the existing finite helper. Actual stage/List/helper semantics remain
allocated to passive publication/body inspection or separately proved runtime isolation; no BCL
patch or permanently unavailable generator substitutes for that obligation. The parallel F0
refactor reads physical curve storage directly, preserving the current snapshot contract and all
remaining native initialization/evaluation premises. This evidence-driven correction preserves the
four positive C1 profiles, all139 features and the complete native acceptance plan.

Revision21 user correction2026-11-12 — S138 uses the same native horizontal slider as adjacent
mood/recreation controls. The modal button/dialog design is superseded. Root removes its draft
session and migrates affected native scenarios as one batch; no new numeric persistence format is
introduced. Whole-percent slider stops preserve a distinct Any endpoint, while painting retains
exact existing fractions and malformed encodings. Earlier modal/native-layout evidence is historical;
the reviewed slider/editor/layout batch requires fresh native observation. All remaining features,
S138/S139 compositions and P0–P8 obligations remain allocated.

Revision 22 — the native slider correction has scoped reviewed acceptance, followed by caravan
tolerance and all seven owned slider-language catalogs. Exact-build records are in S138 tasks;
no complete feature or part is accepted. Move only S024's scheduled potential-mood condition and
its common setting forward from P4-A to compose with S138 at the existing scheduled boundary.
This slice needs C01/C02, native policy identity and the implemented S138 consumer, not S021 cargo,
patient treats or other unimplemented P4 producers. Those obligations remain in their original parts.

The reviewed source packet is `artifacts/Implementation/megamod-p0/s024-scheduled-mood-plan/`.
The original plan is superseded for three specific gaps: existing save fixtures must follow the real
shared patch receipt, independent tolerance needs a fresh history-empty native-permitted control,
and raw read exceptions must cross the real production conversion boundary. Root owns contracts,
central registration, the additive settings measurement, package/compile coordination and final
native observation. The feature lane owns only the exact packet paths. No C01/C02/Gateway rewrite,
second threshold control, medical producer or generic restriction registry is allocated.
Changed runtime invalidates affected older S138 evidence; only focused composed workflows are run
after integrated review. All 139 features, S139's unresolved publication work and P0–P8 gates remain.

Revision 22 scheduling refinement — after the first 30 focused Unit checks passed, the runtime
author released the four still-absent PracticalNeeds E2E files to a separate scenario author.
The exact disjoint lease is recorded in p0-dispatch. Runtime and native workflow authoring proceed
together; builds remain serialized, defects return as a batch, and an independent reviewer replaces
the previous plan reviewer for code that reviewer now authors. No behavior or acceptance scope changed.
