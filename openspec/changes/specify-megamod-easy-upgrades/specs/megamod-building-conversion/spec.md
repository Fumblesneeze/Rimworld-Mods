# Building conversion

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S082 documentation only; all behavior awaits implementation and native verification.

## ADDED Requirements

### Requirement: Explicit conversion choices preserve ordinary eligibility
The player SHALL be able to select Upgrade or Downgrade on a supported, installed player building,
or use an Upgrade order over visible eligible buildings. Repeated cells and repeated orders identify
each building once. The command names the result, additional materials, expected returns and missing
requirements before commitment. Fog, ownership, research, construction skill, allowed material,
terrain, footprint, access, reservations and provider restrictions remain applicable. A forced work
order does not bypass these restrictions. A planned conversion does not instantly change the building.

The initial Core routes are the following; the reverse conversion refunds the same structural
difference, subject to the accounting requirement below. These are source-derived baseline quantities,
not permission to use stale quantities after another mod changes construction costs.

| Upgrade | Initial additional materials | Research needed for result |
| --- | --- | --- |
| Fueled stove → electric stove | 2 components | Electricity |
| Hand tailoring bench → electric tailoring bench | 50 steel, 2 components | Complex clothing and Electricity |
| Fueled smithy → electric smithy | 3 components | Smithing and Electricity |
| Wood-fired generator → chemfuel generator | 1 component | Electricity |
| Door → autodoor | 40 steel, 2 components | Autodoors |
| Stool → dining chair | 20 units of the same construction material at ordinary material volume | Complex furniture |

Both directions require the destination's current normal construction prerequisites. An absent or
unsupported optional building does not create a placeholder. No menu lists an arbitrary building as
convertible solely because it occupies a similar space.

#### Scenario: Researched stove conversion
- **WHEN** the player orders a baseline fueled stove upgraded with Electricity researched
- **THEN** one order previews the electric stove and 2 components, and the old stove remains until physical conversion completes

#### Scenario: Mixed selection and missing prerequisite
- **WHEN** an Upgrade drag includes a previously ordered stove, an enemy stove and an unresearched chair conversion
- **THEN** no duplicate, enemy or unresearched conversion is created and the unavailable choices have an explanation

#### Scenario: Changed construction materials
- **WHEN** another supported mod changes a destination's construction price or accepted material
- **THEN** the preview and charge use the currently valid difference, or the route is unavailable if that difference cannot be established safely

### Requirement: Conversion spends and returns only its stated difference
Conversion SHALL retain the common structural investment, charge positive differences in the
destination's normal material requirements, and return positive differences in the source's
requirements exactly once. Compare both buildings in the retained construction material using
ordinary whole-unit material quantities. Costs that reduce in one ingredient and increase in another
must show both legs; they do not cancel by market value. No full deconstruction salvage is added to
the difference refund. The destination retains the same material when both buildings support it;
an unsupported material rejects the conversion rather than substituting another.

Use the final current price shown before work begins. If that price or route changes before
completion, pause for a refreshed preview and player confirmation without consuming further inputs.
Do not silently create an extra order or silently charge the increase. Only actual reserved and
delivered quantities satisfy the obligation; nearby stacks, including stacks merged with delivered
items, are not wholly consumed. Missing supplies pause work with a reason. Cancellation before
completion leaves the original and recovers surviving delivered inputs; no refund of the still
existing structure occurs. Resource destruction by other causes does not create replacement supplies.

#### Scenario: Stone furniture round trip
- **WHEN** a same-material stool is converted to a chair and back
- **THEN** only the ordinary volume-adjusted material difference is paid and returned once, never wood substituted for stone or a second salvage payment

#### Scenario: Ingredient merges or disappears
- **WHEN** delivered components merge into a larger stack or another action removes part of the delivery
- **THEN** only the actual owed quantity can be consumed, unrelated units survive, and a shortage prevents completion

#### Scenario: Cancel or changed price
- **WHEN** the player cancels an unfinished conversion or its current material price changes
- **THEN** cancellation recovers surviving supplied inputs without replacing the old building, while a changed price pauses for a new preview and confirmation

### Requirement: Physical conversion preserves state and has a bounded paid completion
An assigned eligible constructor SHALL deliver the required resources and perform work at the
building. The baseline work is the source building's ordinary construction work, limited to
**20–3000 work**, advanced at **1.3 times Construction speed**; construction experience accrues at
**0.08 per work tick**, affected by normal learning modifiers. These source-derived values remain
provisional for balance review. Zero or unavailable work speed cannot complete or spin indefinitely.

Keep the old building and enclosure while work is pending. Physical work waits for incompatible
usage to settle safely: do not remove occupied beds, erase unfinished production, fire through a
conversion or accept a second incompatible construction/rework order. Preserve supported bills and
their whole-list membership, counters and policies; storage rules and membership; temperature,
flick/hold-open and supported bed-use/owner choices; style/color and supported provider configuration.
If a destination cannot retain necessary state or owners, refuse before removal. An unavailable
retained recipe is visibly blocked, with linked-production rules governing the group.

Conversion is a modification of the existing investment: preserve its quality and the fraction of
remaining durability, rounding remaining HP down with minimum **1**. It grants neither a quality
roll nor a free repair, and does not restart a quality-construction retry budget. This intentionally
differs from the source's new full-health object and from S028's separately priced reconstruction.
Only one completed destination exists; normal power, fuel and operation restrictions still apply.

#### Scenario: Linked workbench conversion
- **WHEN** a constructor finishes converting a safely idle linked tailoring bench
- **THEN** the new bench keeps its supported bill list, group and accounting, while its actual recipes and power requirements govern new work

#### Scenario: Damaged quality furniture
- **WHEN** a damaged Good stool is converted to a chair
- **THEN** the chair is Good with the same durability fraction rounded down to at least 1 HP, and no free quality roll or new quality retry allowance appears

#### Scenario: Occupancy or unknown state
- **WHEN** conversion would remove an occupied bed or lose unsupported provider state
- **THEN** it waits or refuses with the specific reason before destroying anything, and the old building and occupants remain

### Requirement: Contents and remaining fuel are conserved separately
Conversion SHALL recover physical contents without duplication, lost items or invented destination
capacity. For supported ordinary single-fuel buildings, return only recoverable whole fuel units
from the remaining fuel balance, rounded down after the applicable fuel-unit conversion, once.
Do not choose an arbitrary allowed fuel when its identity is unknown, refund consumed fuel, round a
fraction up into an extra item, or refund again through ordinary removal. The new building receives
no free fuel or battery charge. A multi-fuel, nonstandard fuel or energy provider requires an explicit
supported preservation rule; otherwise the unsafe route is unavailable with an explanation.

#### Scenario: Partly fueled generator
- **WHEN** a supported generator with a known fractional remaining fuel balance completes conversion
- **THEN** its recoverable whole fuel returns once, the fractional remainder is not rounded up, and the destination starts without free fuel or energy

#### Scenario: Contents cannot be recovered
- **WHEN** no safe placement or supported transfer exists for the old building's contents
- **THEN** completion waits without deleting the contents or charging the structural difference twice

### Requirement: Orders survive interruption without acting on another building
The conversion SHALL retain its exact target, paid inputs, earned work and pending completion across
save/load and normal interruption. Cancellation, removal of the order, target destruction or loss of
eligibility stops further work and prevents a late conversion against a different object in the same
cell. It never repeats a completed refund or charge after load. Failure within conversion's own
completion transition leaves either the intact original with recoverable surviving inputs or the
correctly completed destination with its transferred state; never neither or both. Destruction by
combat or another external cause instead cancels the conversion and recovers only surviving delivered
inputs: it does not recreate either building or refund the lost structure. A new order does not
inherit a canceled order's work or refunds.

#### Scenario: Save during work
- **WHEN** the player reloads a partially worked conversion
- **THEN** the same original, delivered quantities and earned progress remain, and completion pays and refunds once

#### Scenario: Cancel then replace the target manually
- **WHEN** the player cancels the conversion and builds another object in that cell
- **THEN** no former worker or restored order converts that new object or grants the former refund

#### Scenario: Combat destroys the original
- **WHEN** combat destroys the original before conversion completes
- **THEN** the conversion stops, only surviving delivered inputs remain recoverable, and it creates neither a replacement building nor a structural refund

### Requirement: Optional routes and changed integrations fail locally and visibly
Optional conversions SHALL appear only when their actual content and necessary state handling are
supported. The source families are VGP loom/oven/preparation/sweets/milling routes, VFE butcher/drug/
smelter/stonecutting routes and Medieval Overhaul bed/sawing/spinning/furnace/research routes. Their
documented directions and research restrictions remain; a one-way source route does not acquire an
invented reverse. They require exact provider verification before any compatibility claim.

Compatible unrelated mod changes must remain supported. When an enabled required integration cannot
safely operate, omit only that integration or its necessary feature group and report a bounded
startup warning identifying the skipped patch, failed condition, missing player behavior and fallback.
Do not remove other mods' changes. Optional provider absence is normal, not a failed enabled patch.
When an original Easy Upgrades provider is active, it owns new overlapping conversion and quality
orders; suppress duplicate owned offers with an explanation and retain recovery of existing owned
work without transferring it into foreign jobs. Source saves/settings are not automatically imported.

#### Scenario: Optional content partially removed
- **WHEN** a provider is active but a required destination, ingredient or state capability is missing
- **THEN** that unsafe conversion is unavailable with a bounded startup explanation while supported Core routes remain usable

#### Scenario: Compatible changes or original provider
- **WHEN** another mod changes an unrelated building behavior, or the original Easy Upgrades provider is activated
- **THEN** compatible owned conversions remain available in the first case, while the second has one owner for new overlapping orders and safe recovery of existing owned work
