## Context

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
S082 replaces `Mlie.EasyUpgrades`, Workshop2904429048, manifest1.6.3. The installed 1.6 binary
matches the binary committed at upstream `b2170be74708814fa4520e0dfe83ce0005939b0a`; this is
artifact identity, not evidence it runs on beta or that a fresh source build is reproducible.
[Research](../../../mods/megamod/docs/refinement/2026-11-04-easy-upgrades-research.md) records
the public-first evidence, exact XML routes, source/compiled paths, native checks and consumers.
No runtime implementation or acceptance is part of this change.

## Goals / Non-Goals

**Goals:** two explicit mechanics: differential-cost building conversion and one paid quality
attempt. Preserve actual ownership/configuration, exact resource accounting, ordinary work
eligibility, current policies and safe recovery. Compose with existing owned features directly.

**Non-goals:** universal upgrades between arbitrary buildings; import of original jobs/settings;
quality grinding until a target; an unrestricted quality editor; generic engine-wide transaction
framework; a separate item index; broad claims for every source-advertised provider.

## Decisions

### 1. Keep conversion distinct from S028 reconstruction

S028 builds a different object at full new cost with ordinary old salvage and new construction
quality; its material replacement pays material/removal work and produces one builder quality roll.
This feature's explicit conversion retains common structure, pays/refunds differential quantities,
preserves quality and damage fraction, and adds no quality roll. Labels/previews must make the
difference visible. Do not silently redirect S028's build-over path to a cheaper conversion.

Use a finite, declarative route catalog with source/destination/direction and provider ownership.
The six Core pairs are in the observable spec. Resolve construction costs with retained Stuff and
native cost/volume rules after Def finalization; aggregate duplicate ingredient Defs before deriving
positive differences. Don't preserve WoodLog as an ambiguous sentinel in the owned representation.
Research gates include destination prerequisites, not just the source XML's occasional single
Electricity gate. Inspected XML baseline comparisons support the six pairs; finalized optional
costs/footprints/state are still verification work. Research catalogs 17 optional source routes;
each requires its own real provider evidence and target admission before activation.

Store exact target identity, chosen destination, material, price/policy revision, actual delivered
quantities, earned work and completion state with the owned order. The implementation must prove a
bounded native-safe commit/recovery path before removing the old object. Do not represent resource
ownership merely as references to nearby merged stacks. The source's `resourcesPlaced` plus whole
Thing destruction is insufficient. At completion, current eligibility, state transfer and inputs
must all be valid; a stale quote pauses rather than silently charging. Cancellation is observed
during work and immediately before commitment. Save/load cannot repeat an outcome/refund.
External destruction of the target cancels conversion and releases only surviving delivered inputs;
the intact-original-or-destination guarantee applies to our own commit failures, not enemy damage.
Never recreate or structurally refund an externally destroyed target.

Reuse S028's supported state-transfer decisions directly, including S005 storage and S014 whole-list
workbench membership/counters and the later production policies. Keep actual physical item/UFT
ownership; no recipe-list reassignment that disconnects an active bill. Unsupported external state
blocks the route. Preserve style and art metadata where supported. S013 target/spent quality budget
survive while the target remains unmet and do not automatically trigger another cycle from a
conversion or rework event. A rework reaching that target closes the run and clears its count through
S013's ordinary achieved-target rule; later deterioration shows unmet quality but requires an explicit
contextual request for a new construction run. S054 repair knowledge gates durability-repairing
rework. These are owned calls, never self-patches.

### 2. Fuel is a separate conserved balance

The source emits `Ceil(Fuel)` of the first allowed fuel before `Destroy(WillReplace)`. Current
native `CompRefuelable.PostDestroy` can also return `Floor(Fuel)` when one fuel is allowed,
initialFuelPercent is zero and mode is not Vanish. Native `Refuel(float)` multiplies item units by
`FuelMultiplierCurrentDifficulty`; raw Fuel is not universally an item count. Thus copying the
source replacement path can double-return fuel and mishandle fractional/multiplier/provider cases.
This is a code-derived risk, not a live reproduction.

The admitted transition must choose exactly one return owner. Preserve no free destination fuel or
energy. Single identified ordinary fuel uses remaining balance divided by its actual unit multiplier,
rounded down. Native hooks/provider transitions must not issue another return or overwrite preserved
state. Multi-fuel identity, unusual fuel pools, difficulty changes affecting historical conversion,
and provider energy require an explicit adapter contract; unknown cases stay unavailable rather than
choosing the first filter item. Do not suppress native destruction globally. Account for physical
placement failure before committing destruction; no unplaced refund is considered delivered.

### 3. One quality job with honest role and recipe eligibility

Source uses Construction for buildings, Tailoring for apparel, Crafting for weapons and Art for
sculpture in WorkGiverDefs, but the apparel/art factories additionally require Crafting assignment.
Remove that mismatch. Work-capable mechs use their supported skill without fabricating pawn skills
or bypassing work restrictions. A candidate recipe/station must actually support the item, current
research/skill and provider restrictions. Inspect final recipe routes and current operational checks;
do not equate `recipeMaker.recipeUsers` or `AllRecipes` membership with an operable route.
Current `WorkGiver_DoBill` checks usable/fueled bill giver, reservation/interaction access, worker
permission and recipe skill; it may schedule refueling. Our rework is not an ordinary production
iteration, so reuse the relevant eligibility policies without creating a fake bill or product count.

Use the real item (inner Thing for minified sculpture) and a separate material delivery obligation.
Safe native uninstall precedes art transport; preserve exact inner identity and cancellation recovery.
Don't copy the source's `TryMakeMinified` plus RemoveAllDesignations shortcut. A paid rework is a
single player intent, independent of S013's finite reconstruction run. Completion clears its own
designation only and preserves unrelated orders/metadata. Unsupported custom stackable quality
items need per-item splitting/accounting proof before admission.

Source-derived costs, work, chances, HP and XP are in observable specs. Clamp skill/probabilities to
valid ranges, preserve sequential-success-first probability mass, calculate the actual residual
deterioration chance for display, and snapshot/confirm any policy changes during committed work.
Quality outcome and payment commit once. Messaging and graphics invalidation happen after that
outcome is durable and cannot trigger a second roll. Name formatting must not slice at a literal
parenthesis; style/art metadata must not be regenerated merely by calling source `SetQuality` logic.
Minimum-1 deterioration HP, current-max clamp, ordinary research/skill/power gates and exact payment
are intentional corrections. Full repair on success and 10% on neutral remain explicit benefits.

### 4. Native data and common UI/settings

Keep designation-driven candidate work and native map listers. Material candidates can consume the
same map-owned facts used by S021 hauling and Ctrl+F, but pawn reachability/reservations/forbidden
state remain current action checks. The existing recipe metadata/comparison service can supply
recipe/station mappings; don't scan every ThingDef in each gizmo render or introduce a private cache.
If a concrete shared index is extended, document fields, invalidation, lifecycle and native fallback
with the owning feature before coding. No speculative service is needed for six Core pairs.

UI: contextual gizmos plus Architect Upgrade/Increase-quality area tools, native job/refusal/inspect
feedback, structured Work settings. No new bottom tab, standalone toggle or source settings page.
Entity orders are not inherited settings. Owned maximum quality (initial ceiling Masterwork), five success bases, four
non-Awful failure bases and five material factors become common typed definitions; all defaults/ranges/timing
and dormant overrides are in the quality spec. No speculative feature toggle is added. Any actual
debug override belongs in existing top developer controls. Seven-language text and keyboard access
must be reviewed; do not blindly reuse the source's ItemForbid hotkey conflict.

### 5. Declare every real integration and validate semantics

Prefer owned JobDefs/WorkGiverDefs/designations and guarded declarative comp/designator additions.
The inspected source runtime has no Harmony patches. Do not invent a generic interception layer
just to emulate it. XML Extensions is required with the first XML extension use; package metadata
and load order change in that implementation slice. Additive operations preserve comps/designators,
create a missing container only where appropriate, avoid duplicates and never append a second
`comps` node as the source Medieval Overhaul patches can do. Optional XML uses
`XmlExtensions.FindMod` with `packageId=true` plus actual source/destination/ingredient/research/node
shape checks. Inspect exact supported operations before writing XML.

Before implementation create/index stable target-spec documents for every actual XML/Harmony
patch, linked to observable scenarios, executable validators, tests and later live evidence.
Candidate atomic groups are each directed conversion's declarative registration, quality eligibility
registration, Architect tool registration, and only proven native transition bridges. Validate the
required minimal content/call/data-flow semantics, allowing benign edits and compatible owners;
do not use a whole-method hash, exact version or unpatched-method requirement. Effective code and
skip-original prefixes matter when relying on native execution. A transpiler validates its incoming
stream on every invocation and returns it unchanged on failure; do not speculatively execute foreign
transpilers. Supported later repatching must revalidate/invalidate the group. False/unknown/exception/
install failure leaves the group inactive and rolls back newly installed owned patches only.

Emit one bounded startup warning per skipped enabled patch naming package/patch/spec/target, failed
predicate, unavailable behavior and fallback. Optional absent providers are not failed candidates.
Original `Mlie.EasyUpgrades` and historical `toywalrus.easyupgrades` own overlap when active: disable
new overlapping owned offers, retain safe completion or cancellation recovery for existing orders,
never remove source patches or import foreign jobs. Compatible foreign mods are not blanket-disabled.

### 6. Concrete external contract and migration boundary

Colony Manager Redux (`ilyvion.colonymanagerredux`, Workshop3310027356) loads
`Integrations/EasyUpgrades/Common` only under `IfModActive="Mlie.EasyUpgrades"`, including its 1.7
branch. Its XML consumes `EasyUpgrades.CompProperties_Upgradable`, CompUpgrade/CompDowngrade,
linkedThing/keyedTooltipString/researchPrerequisite and resource lists; WoodLog deliberately denotes
the existing desk's Stuff. Copying the namespace will not satisfy the package-gated folder.

Prefer a narrow optional owned route adapter for the actual manager-station pair, preserving its
observable route and manager state once provider inspection proves that transfer. Do not spoof
another package, expose every source class, or claim blanket binary/save compatibility. This adapter
remains EU-Q3 research, not implemented support. Source settings keys and job/comp types are evidence,
not automatic migration promises. A future migration requires a separate backed-up, atomic,
failure-restorable contract and verified postconditions. Disabling an owned feature preserves recovery;
removing its assembly mid-job is not claimed safe.

## Risks / Trade-offs

- Source caps make chair conversion use 450 work versus 8000 for a new chair, and quality work at
  most 200. Keep explicit provisional baselines; compare native manufacture, S028 rebuild and this
  action in actual finalized game conditions before acceptance (EU-Q1).
- Largest-fixed-ingredient and bulk-count quality pricing can be unintuitive or cheap for expensive
  objects. Record rather than hide that source behavior; no claim of balanced resale; the user excludes a Legendary reworking path.
- Lost/duplicated physical inputs or fuel are more serious than an unavailable conversion. Bounded
  precommit validation and actual recovery take precedence over broad advertised compatibility.
- Cross-feature budget, recipe or provider state can be lost by reconstruction. Direct state ownership
  and exact target identity are required; no generic reflection copying or same-cell fallback.
- Current beta lifecycle differs from source1.6; native RemoveBuilding uses interval ticks and 1.7
  speed multiplier. This draft deliberately retains the source's explicit 1.3 work multiplier rather
  than silently inheriting a new superclass rate. No source binary was loaded to test it.

## Migration Plan

Implement one observable vertical slice at a time after research gaps are closed. Introduce common
settings and guarded registrations only with their first behavior. Test recovery and source-present
suppression before enabling new offers; source saves are unsupported unless a later migration is
specified. Release/package dependencies follow actual use. No install, publication or save editing
is authorized by completion of these documents.

## Open Questions

- **EU-Q1:** measure source economic and labor baselines under the selected Masterwork ceiling, full/partial repair,
  bulk scaling and largest-fixed-ingredient pricing against three current comparators.
- **EU-Q2:** prove exact beta commit, fuel/native destruction, art identity/graphics, work interruption,
  authoritative item delivery and save restoration paths; identify only the necessary guarded bridges.
- **EU-Q3:** inspect each optional provider and the manager-station consumer state contract; decide
  supported adapters/facades and any explicit migration without package spoofing.
- **EU-Q4:** verify combined S013/S028/S054, linked production, hauling and current external quality
  behavior; preserve budgets and identity without hidden repeat or double product/payment.
- **EU-Q5:** validate contextual previews, common settings/localization and reviewed-build native
  workflows. All implementation, RED/GREEN and live acceptance tasks remain open.
