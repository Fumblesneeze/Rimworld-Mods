# Toddlers — apparel, independence and mood behavior

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`); **S031**, target **1.7 beta**.
Partial research, not a full feature draft or implementation. The [research index](2026-11-03-toddlers-research.md)
pins the original manual, source revision and installed package. Paths here are relative to that
checkout; findings concern its selected 1.6 branch unless stated otherwise.

## Exact coverage

Read all **16 C# files outside `Apparel/1.5`** and all **7 MentalStates C# files** under
`Source/Toddlers/`. `Apparel/Harmony/TestPatch.cs` is explicitly a project `None` item, not compiled.
The three legacy apparel files were checked for their enclosing `RW_1_5` guards, not audited as
active 1.6 behavior. The former forbidden-state and two Lord-trigger patches in MentalStates
are commented out; the current combined transpiler supersedes them.

Read complete `1.6/Defs/{Apparel,ThingCategories,MentalStates,Jobs_Adult}.xml` and
`1.6/Patches/{MentalFits,Expectations,Inspirations}.xml`; revisited the manipulation stages in
Hediffs and the life-stage values. No native 1.7 apparel job, finalized XML inheritance, asset,
external-provider or full published-binary acceptance has been performed.

## Functional clothing and availability

The manual names four garments for infants and toddlers. These are functional temperature
protection, not merely a decorative pack to omit from the selected replacement. Source XML puts
them under the new **baby clothes** child category of Apparel, with Baby developmental eligibility.
The common base requires Complex Clothing; baby tribalwear explicitly clears that research and
uses CraftingSpot. Other recipe users depend on native inheritance and must be resolved exactly
before the owned crafting contract is finalized.

| Garment | Stuff cost | Work | HP | Mass | Equip delay | Armor multiplier | Cold / heat insulation multipliers |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Baby onesie | 20 | 2000 | 50 | 0.18 | 1.5 | 0.2 | 0.6 / 0.6 |
| Baby tuque | 10 | 1200 | 50 | 0.03 | 0.8 | 0.2 | 0.5 / 0 |
| Baby shadecone | 10 | 1800 | 50 | 0.07 | 0.8 | 0.2 | 0.1 / 0.5 |
| Baby tribalwear | 15 | 1400 | 60 | 0.5 | 1.5 | 0.2 | 0.5 / 0.5 |

These are **declared XML values**, not measured final stats or approved owned balance. Material,
quality, inherited fields and native processing remain relevant. The base lists Fabric and
Leathery, while tuque adds a Fabric entry without explicitly clearing the inherited list; do not
promise fabric-only finalized eligibility without checking inheritance. The onesie covers torso,
neck, shoulders, arms, legs and feet; tribalwear covers torso/legs. Hats cover upper head, declare
Overhead and do not count as clothing for nudity. Final layer inheritance also needs validation.
Ground item draw size is 0.5; graphics and worn paths reference corresponding child/native apparel.

`ApparelSettings.InitializeApparelLists` scans **all providers' Baby-only apparel**, not just its
four garments, and finds recipes whose produced Thing matches. It remembers original tradeability,
classifies Neolithic versus all other tech levels, and uses the onesie/tribalwear's recipe users
and research as the two templates. Any Child-wearable apparel and the WearableByBaby whitelist
are additional groups. The selected mode then changes global Defs and recipes:

| Source mode | Source action |
| --- | --- |
| NoBabyApparel | Hide Baby-only garments and remove the added special/child Baby eligibility |
| NoTribal | Hide the originally Neolithic Baby-only garments; other Baby-only garments use the onesie template; enable special whitelist for Baby |
| AllTribal | Make all Baby-only garments Neolithic with the tribalwear research/workbench template; enable special whitelist for Baby |
| BabyTribalwear, default | Keep original Neolithic group on tribal template and every other Baby-only garment on medieval/onesie template; enable special whitelist for Baby |
| AnyChildApparel | Hide Baby-only garments, enable Child garments and special whitelist for Baby, and attempt missing body-graphic generation |

Here “hide” means set tradeability to zero, clear Baby eligibility, clear research and recipe users;
it does **not destroy existing items or remove Defs**. This does not by itself prove exclusion from
every generation or item-menu path; the manual itself reports babies still generated in disabled
babywear. Re-enabling restores saved tradeability but applies shared research/tech/workbench
templates, not every provider's original data. Child/special items have Baby bits cleared when
not selected, even if some other provider originally supplied that eligibility.

The special whitelist includes gas mask, shield belt, psychic foil helmet, cloth mask and conditional
Odyssey children's vacsuit/helmet. The settings method changes their Baby eligibility, not their
tech or research. They still require exact content and native capability checks. Source `ResolveReferences`
calls and reflected recipe/research cache clearing are not our merge-preserving integration model.
A saved global preference does not justify rewriting unrelated third-party lists or leaking one
save's availability policy into another. Record explicit ownership, merge and effect timing.

AnyChildApparel's missing-graphic routine searches the originating package's filesystem for Child
textures, copies renamed Baby files into **Toddlers' package directory**, then reloads its texture
holder. Existing base/north Baby texture checks can skip copying; asset-bundle handling is not
established. The manual describes this as a local cache, not bundled permission. Do not execute or
reproduce this package-writing design, and do not ship copied provider art. Future runtime graphic
reuse or independently authored assets require appropriate provenance and the repository visual gate.

The old `Toddlers_Settings.tribalBabyClothes` boolean has no caller in the inspected active source;
the apparent old UI use is in `Scrapyard.cs`, explicitly excluded from compilation. The similarly
named `ApparelSettings.tribalBabyClothes` is a different, active list. Do not expose a duplicate
setting based on the stale field.

## Dressing and undressing workflows

The manual's clothing-policy choices belong to the pawn's native Assign/Gear surfaces, not the
shared mod-settings inheritance UI. Current source supports these paths:

- A manipulating caregiver can be directed through native interactions to dress a baby. In 1.6,
  the automatic dresser issues native **ForceTargetWear**; the custom DressBaby/UndressBaby
  implementations and XML definitions are legacy/commented. Exact native menu, pair locking,
  duration, forced-apparel status and completion require target-version inspection.
- `JobGiver_OptimizeBabyApparel` requires active Childcare/Caring, player-faction adult and baby,
  the baby's matching **Childcare** autofeed mode, and an elapsed native apparel-check cooldown.
  It skips a baby forming a caravan. It chooses the first eligible baby, not a proven globally
  best or nearest dress candidate, and does not itself call the busy-baby helper.
- It first creates a Strip job when worn apparel violates the baby's policy and is neither locked
  nor protected from automatic dropping. It passes that garment as target B and asks to haul
  dropped apparel. **Do not assume this native Strip job removes only that garment** until its
  exact body is inspected. Protection of other forced/locked clothes and urgent eating is open.
- Otherwise it scores stored, unforbidden, unburning apparel through native scoring. Gain must
  be at least **0.05**; biocoding, body-part compatibility, Baby eligibility and adult reservation/
  reachability to both garment and baby are checked. No candidate sets the native next-check
  time. Declared 6000/9000 interval constants are not the setter's implementation; do not infer
  actual timing from unused constants. The local `neededWarmth` calculation also does not prove
  every claimed weather preference in the delegated scorer.
- Confident manipulation enables self-dressing. Younger toddlers' Wear menu options retain
  native absent/disabled results and otherwise gain an inability explanation. Their self-apparel
  optimization uses the native job, with `haulDroppedApparel=false`: old clothing stays where
  dropped rather than becoming a toddler hauling task.
- Colony stripping is additionally allowed for a toddler with something to strip. When a live,
  awake, non-downed toddler outside a crib reaches a native Strip wait toil, it receives
  BeDressed; finishing that toil ends this waiting job if still active. The waiting driver checks
  that the adult's target is this pawn, but dereferences its current job without a null guard.
  Exact pair cancellation/reload behavior remains to specify and verify.

The gear-tab visibility fallback is for the exact HumanlikeBaby life stage and delegates existing
inventory/apparel/equipment visibility checks. It is not proof of equivalent behavior for every
alien infant. The generated-hat validator rejects wrong-age hats after native validation, but
locates a compiler-generated method by name fragment without semantic admission. Both require
narrow target checks rather than unconditional copied hooks.

Swaddling returns false for toddlers and for Baby-stage pawns that are not psychologically nude.
An infant wearing only a hat may still count as nude; the broader manual phrase “wearing other
clothing” must not be read as every apparel item disabling the wrap. Preserve actual wearable
layers/protection, body types and ordinary player-visible rendering in the future contract.

## Clothes-removal episodes and mood

Learning manipulation supplies a RemoveClothes mental-state giver at clumsy stage with mean time
between events **6 days**, and at confident stage **20 days**. These are random-rate inputs, not
guaranteed schedules. Completed learning removes the learning condition, so it no longer supplies
this giver. The state requires Manipulation, at least one worn garment, and cannot run in a caravan.

On a state tick, no worn clothes or medical-rest need ends it. With no current worn target there
is a **50%** chance to end; otherwise it randomly chooses worn apparel. Before continuing it
compares current cell temperature to the pawn's comfort bounds expanded by **10 Celsius**, adjusted
for the chosen item's cold/heat insulation. It cancels if the estimated post-removal condition is
unsafe. It does not forecast later weather or prove correct composition with every provider's
temperature effects. The chosen target is not explicitly serialized in this state.

The removal job waits **EquipDelay × 60 ticks**, shows a progress bar, and drops that item at the
pawn's held position if still worn. It records its duration, checks drop success and fails cleanly
if the item is no longer worn. It does not explicitly filter forced or locked apparel when choosing
the target. Define deliberate episode semantics and recheck safety at the actual removal boundary;
do not let this source path bypass the composed S030 safety policy.

The toddler life stage disables ordinary random mental breaks. Imminent/approaching-break warnings
are suppressed for **any pawn** whose mental breaker cannot do random breaks. XML removes Baby
from ordinary expectations, while BabyNoExpectations supplies the configurable **+20** default.
The new go-frenzy minimum-age field is **1**; native inspiration eligibility remains authoritative.

The source replaces Crying/Giggling mood-to-mean-days curves, affecting the targeted native fits:

| Mood fraction | Crying mean days | Giggling mean days |
| --- | --- | --- |
| 0 | 0.001 | 8 |
| 0.25 | 0.02 | 4 |
| 0.3 | 0.1 | No separate point |
| 0.5 | 1 | 1 |
| 0.75 | 4 | 0.1 |
| 1 | 8 | 0.05 |

Native interpolation and fit eligibility still apply; these values alone do not specify actual
crying duration or guaranteed events. Whole-curve replacement is a deliberate behavior decision
to justify and guard, unlike adding an item to an unrelated list.

The current mental-state transpiler treats **all Baby-stage mental states** as absent in selected
caravan/departure warnings, area forbiddance, Lord triggers, gatherings, ritual-role checks and
shuttle-health eligibility. It is broader than the manual's crying/giggling explanation; it does
not establish that every other eligibility condition is bypassed. A separate player-control
postfix returns true for a live spawned colonist toddler whose HostFaction is null **or who is a slave**,
without specifically checking which mental state caused the original false result.

Draft retention is narrower: Crying, Giggling and RemoveClothes on a Baby-stage pawn. Its transpiler
rewrites flow incrementally and logs failures without returning the original stream; prefix/
postfix also remember and restore drafting. This is a target-admission hazard, not an architecture
to copy. Ordinary play, caravan eligibility and player control must share explicitly chosen state
semantics, especially for foreign mental states, rather than hide every Baby state indiscriminately.

## Remaining specification work

- **TD-Q7 apparel/mood contracts:** inspect exact native dressing/stripping/scoring, finalized
  material/layer/research inheritance, automatic-care urgency and forced/locked protection. Decide
  intended settings modes and provider-preserving eligibility, safe saved policy transitions,
  swaddling, clothes-removal behavior and mental-state scope. Distinguish source facts from owned
  corrections, and give all adopted values observable scenarios and future balance validation.
- **TD-Q2/3/4:** compose S030 temperature safety, common save/global settings, shared candidate
  indexing, provider-native visuals and existing-save lifecycle. Clothing policy is entity state;
  no independent settings screen or bottom tab is justified by the source.
- **TD-Q1/5:** travel, carrying, kidnapping, combat, refugee arrivals, animation, DBH/HAR and
  remaining misc/XML/consumer work are still pending. No full S031 draft or gameplay acceptance.
