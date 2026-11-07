# Toddlers — feeding, play, cribs and caregiver safety

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`); queue **S031**, target **1.7 beta**.
This is source research, not a completed specification or native acceptance result. Exact package,
manual, revision, hashes and license boundary are in the [research index](2026-11-03-toddlers-research.md).
All source paths below are relative to that pinned checkout. Unless qualified, findings describe
the 1.6 source branch. Do not infer compatibility with beta or unchanged external mods.

## Coverage and evidence boundaries

Fully read all **10 Feeding, 35 Play, 11 Crib and 6 Safety C# files** under `Source/Toddlers/`,
including the Harmony subdirectories. The main project includes these paths. Presence in the
project is not proof that commented or version-excluded code executes: notably
`Play/Harmony/JobDriver_BabyPlay_MakeNewToils_Patch.cs` is entirely commented out.

Also read complete `1.6/Defs/{ThinkTrees,ToddlerPlay,BabyPlay,Jobs_Play,Jobs_Baby,DefLists,
WorkGivers,Filth,Thoughts}.xml` and `1.6/Patches/{ThinkTrees,Jobs}.xml`; learning/loneliness Hediff
XML was already read in the initial pass. Job references to apparel and hygiene are inventoried
here; their complete behavior still requires the corresponding source groups.

Existing `artifacts/decompiled/RimWorld-1.6-current` files were consulted narrowly for
`ToilFailConditions.FailOnChildLearningConditions`, `FoodUtility.MoodFromIngesting` and its callers.
That older decompilation's generating assembly was **not re-established in this pass**. It is a
research lead only, not pinned beta evidence or proof of the source package's native environment.
No source project was built, no game was launched, and no package or user configuration was changed.

## Feeding and food mess

The manual promises learning-dependent self-feeding, slower messy eating, adult cleanup and no
food-related mood effects. The ten Feeding files establish the following narrower facts:

| Input or condition | Source result and remaining limit |
| --- | --- |
| Toddler food search | `BestFoodSource_Patch` sets the minimum food preferability to DesperateOnly, permitting baby food to be considered; it does not itself waive every diet, access or reservation condition |
| Player orders eating before the learned ability | The ingest-menu postfix keeps native absent/disabled options and otherwise disables the toddler's option with an inability reason |
| Clumsy self-feeder | `CarryIngestibleToChewSpot_Patch` replaces the move action with a nearby reservable standing eating spot at Danger.None; later stages retain the ordinary route |
| Toddler chewing | `JobDriver_Ingest_Patch` multiplies the native chew-duration multiplier by **10**; final duration still depends on native inputs |
| Urgent eating | Food is below the pawn's urgently-hungry threshold and its job is Ingest, or TakeFromOtherInventory targeting something it will eat. The helper itself does not establish toddler identity |
| Native suckling decision is already false | Remains false |
| Toddler is eating urgently | `WantsSuckle_Patch` suppresses the otherwise-positive suckling decision |
| Feed-capable-toddlers option is off | A spawned toddler that can feed itself and gets a successful native best-food search is not selected for suckling; unsuccessful search retains native eligibility |
| Mood-related helper | `MoodFromIngesting_Patch` returns zero for every pawn in developmental stage Baby, not just toddlers; this helper alone does not establish removal of actual food memories |

The inspected older native decompilation uses `MoodFromIngesting` for warning/menu paths and
separately calls `ThoughtsFromIngesting` when food is consumed. Thus the manual's blanket mood
claim is not proven by this patch. Inspect exact native thought eligibility and ingestion on the
target version before specifying either immunity or food-policy precedence. Do not silently
turn a helper return value into a claim about every resulting memory.

`FeedingUtility.TryMakeMess` computes rate **0.06 × activity factor × baby's FilthRate /
max(feeder's Manipulation, 0.1)** and tests a random chance of **rate × 0.005** per invocation.
Self-chewing supplies activity factor **2**; bottle feeding supplies **1**. This is a per-callback
source formula, not a measured per-meal probability or proof of interval-invariant behavior.

On a successful random roll, the enabled feeding-mess option permits food filth at the feeder's
position. If the feeder is another pawn and is capable of Cleaning, it reuses the first queued
Clean job or adds one at the front, then adds that filth target. This schedules cleanup; it does
not guarantee successful cleaning or justify interrupting emergencies. The XML food mess is
rain-washable, can attach, disappears after **35–40 days**, and costs **5** work per thickness
reduction. These are added-Def values, not the full inherited filth contract.

DBH hygiene loss is inside the random-roll branch but **outside the feeding-mess toggle and
successful-filth condition**. It subtracts **rate × 0.2** from the baby's hygiene when baby hygiene
is enabled; a separate adult feeder with hygiene has a 50% chance of the same loss. Values clamp
at zero. Turning off floor mess therefore does not necessarily turn off hygiene loss. The full
DBH feature, public settings and provider lifecycle remain unread; retain this as a composition
question rather than inventing an option or promising compatibility.

## Play, attention and loneliness

`Play/ToddlerPlayUtility.cs`, `Hediff_ToddlerLonely.cs`, the givers/drivers and Play Harmony files
separate solo entertainment from adult attention:

- Loneliness is a hidden condition, shown in the Play tooltip. XML starts and clamps its minimum
  at **0.001**, maximum **1**, with severe stage at **0.7**. The matching severe thought gives
  **-8 mood**. `GetLoneliness` lazily adds the condition to a live toddler if missing; even reading
  the patched tooltip can therefore initialize state. It is removed outside the toddler stage.
- The 1.6 condition checks a **200-tick hash interval**, then adds
  **delta × 0.0008 × (1 − 0.6 × fraction-through-toddlerhood) × configured loneliness rate**.
  The older per-tick branch omits delta. The rate decreases with age. Multiplying the current
  batch size only on selected hash intervals creates an interval-size sensitivity to investigate;
  no particular actual engine batch size or live growth rate is claimed here.
- Solo play adds **0.00012 per tick × room play factor**, while current Play is at or below
  **1 − loneliness**. It can overshoot that cap by a gain step; reaching the cap alone does not
  end the activity. Activities normally last **2000 ticks**, ending early at Play **0.99**.
- Adult play reduces existing toddler loneliness by **0.0006 per tick**. It does not add the
  missing condition itself. The completion postfix keeps full-Play interaction running while
  loneliness exceeds **0.01**, but permits completion if the baby has fallen asleep.
- The ordinary solo giver does not issue jobs at Play **0.95** or higher, orders activity Defs
  randomly and takes the first valid non-null job. The `selectionWeight` field is unused there.
  Priority is **6** below `0.7 × maximum solo Play`, otherwise **2**. The basic activity check
  additionally requires Play below **0.9**, a non-downed toddler and no imminent basic need;
  the crib variant allows downed toddlers.
- Crib solo play tries television before skydreaming, requires the pawn awake, and has priority
  **8** below `0.8 × maximum solo Play`, otherwise **2**. Other solo activities are not crib options.
- Patched Play-is-low is true at Play **≤0.4**, loneliness **≥0.8**, or loneliness **≥0.4** with
  Play **≤0.8**. Non-toddlers normally obtain zero from the loneliness helper. The Play interval
  replacement subtracts native base decay multiplied by the infant/toddler setting unless frozen;
  defaults are **5 / 3**. It bypasses the rest of that original method.

The sad-baby giver seeks the first reservable/reachable, unforbidden, unsuspended suckling-capable
faction baby that is spawned or carried by this adult. It requires an active Childcare work type
and Caring capability, **Play <0.3 or loneliness >0.5**, and **mood <0.4 or the Crying mental state**.
It tries play Defs in random order, but returns immediately from the first acceptable giver even
if that giver returns no job. Its advertised crying-related priority is faulty: `Select(predicate)
.Count()` counts all thoughts, so any thought yields priority **8.5**, otherwise **7.3**. This is a
source defect, not a desired rule that every thoughtful caregiver outranks other needs.

Adult play forces a paired BePlayedWith job so the child stays put. Static play adds a final
adjacency move/check. Crib play is for medical rest; static/toy play rejects a baby that needs
medical rest and is downed or already in bed, while walking play rejects medical rest in bed.
Crib selection uses the current bed or an acceptable nearby bed within **15.9 cells**. The intended
interaction is adjacent play, but the driver computes a spot beside the child before pickup and
tucking, ignores search failure, and later goes to that precomputed spot. It can therefore target
the child's old position instead of the bed. Recompute and validate actual interaction adjacency
in the owned behavior; the source does not prove successful bedside play after moving a child.
The interaction helper is called every **600 ticks** during this job;
actual native social outcome remains to inspect. Sleeping, interruption and finishing use native
job/bed helpers, not instant satisfaction.

Two implementation hazards need explicit owned behavior: BePlayedWith checks the adult's driver
class, not that its current target is still this child; and the source clears `playerForced` when
ending any BabyPlay job to suppress subsequent safety messages. Pair ownership, interruption and
notifications should be specified directly, without accidentally keeping a child waiting for a
different target or rewriting the player's unrelated command intent.

## Seven independent activities

Each row is grounded in its `Play/ToddlerPlayGiver_*` and `JobDriver_Toddler*` files. Native helpers
still need exact beta inspection, especially interruption, reachability and reservation behavior.

| Activity | Source selection and observable activity | Discrepancy or boundary |
| --- | --- | --- |
| Bugwatching | Safe-temperature, allowed, reservable outdoor cell; stand leaning east/west, custom animation, 2000 ticks | Manual permits indoors, but finder and running job both require psychologically outdoors |
| Firegazing | Whitelisted fire within **<15.9 cells**, fueled and switched on when those comps exist; reserve reachable adjacent spot and face fire | Running driver has no explicit fuel/switch recheck. Selection uses non-short-circuit `\|` around null/invalid-target checks; exact native failure behavior is unproven |
| Floor drawing | Allowed, standable, safe-temperature Home cells, no door, reservable drawing surface; face the adjacent drawing cell and create native floor-drawing filth | Normal CanDo requires **self-dressing/confident**; TryGiveJob only requires **self-feeding/clumsy**, matching the manual. Default 2000-tick job draws on tick zero, with another drawing only at 2500 ticks |
| Toybox | Reachable, reservable, unforbidden, unburning native ToyBox within **15.9 cells**; stand facing it, five maintained toy motes around the pawn | Native toybox identity, not arbitrary similarly named buildings; no extra bottom tab |
| Baby decoration | Equivalent proximity/access checks for native BabyDecoration; horse variant uses its cell, others Touch; toddler faces/stands with a visual offset | Variant derives from Thing ID modulo five. Room fast path mistakenly enumerates ToyBox, but global BabyDecoration fallback remains. This is a search flaw, not missing decoration support |
| Skydreaming | Normally requires enjoyable outdoor conditions and an allowed outdoor, unroofed, safe-temperature location; lies face up. Crib variant uses current bed/cell when unroofed | Running job ends if roofed; crib selection does not explicitly repeat the outside-enjoyment/temperature checks |
| Television | Uses native WatchTelevision activity's building list and reservation participant limit; finds a view position/chair, or watches from current bed/crib if visible | Nearby-room search has no explicit 15.9-cell limit; global fallback does. Giver accepts a television with no power comp, but driver dereferences one unconditionally; powered viewing ends when power goes off |

Most solo drivers use `FailOnChildLearningConditions`; television's replacement toils do not add
that explicit helper. In the older, unpinned native decompilation the helper checks juvenile stage
and imminent needs, **not temperature, fire or changing allowed areas**. Do not assume the source
has ongoing environmental safety merely because it invokes a helper with a broad name. S030 must
compose into selection, ongoing activity and recovery across all seven activities.

Firegazing XML includes Campfire, conditional Royalty/Ideology braziers, and conditional
Undergrounders/Vanilla Expanded entries. `Stone_FungusDarklightCampfire` occurs twice. Future
optional-content patches need package-and-Def guards and deduplicated membership. Television
Def/participant caches have no explicit invalidation in this source; native finalized Def access
and the megamod shared-data policy take precedence over copying another cache.

## Crib independence and assistance

`Crib/CribUtility.cs` recognizes a crib through a bed's label/Def name containing Crib/crib or
Cradle/cradle, and requires the pawn not moving. That language-sensitive heuristic is not an
adequate universal content contract. The original manual explicitly documents it; replacing it
with verified capabilities/adapters is an intentional compatibility decision to record.

Crawlers cannot leave independently. Medical rest blocks independent exit as well. In the 1.6
exit giver, a self-feeder below the race's want-eat threshold gets priority **9**; after that an
asleep pawn gets zero, Play below **0.7** gets **5**, otherwise **2**. It scans eight adjacent cells
in random order at Danger.None and keeps the last reachable/reservable candidate. No cell means
no exit job. The driver normally wiggles for **2000 ticks** before moving out, skipping that delay
when urgently hungry or Play below **0.3**. Its own toils do not recheck all mobility/medical/crib
conditions after selection.

Crawling toddlers seeking a different bed are diverted to ground sleep. The patch calls a private
native ground-spot finder but does not inspect its success result. The let-out context command is
for a live crawler in a crib and a manipulating non-toddler caregiver; it picks up the child and
places it nearby. Its adjacent-spot search and carry-drop return values are unchecked. Future
behavior needs a valid reserved destination and recoverable failure, particularly in a one-cell
room, blocked exit, changed reservation or changed temperature.

Idle crib jobs rest, wiggle or lie at a random angle for **2000 ticks**; the latter changes angle
after more than **600 ticks**. Exact `Nej_TribalCrib` excludes the angled variant. Idle selection
can return the existing non-LayDown job. Animation angle/tick bookkeeping is not explicitly saved
in that driver; animation classes and persistence remain to inspect. No new crib construction Def
has been established by these jobs.

## Caregiver safety, bedtime and destinations

`Safety/BabyMoveUtility.cs` (862 lines) and all five companion files were read in full. The source
replaces native unsafe-baby selection and safe-place search, suppresses the native context provider,
and replaces both forced/unforced BringBabyToSafety JobDef drivers through XML. Those broad
replacements are evidence of upstream architecture, not our chosen integration strategy.

Automatic selection follows the caregiver's exact autofeed priority, excludes Never and busy
babies, and considers free spawned humanlike faction babies. Busy includes the already-recorded
drafting/Lord/urgent-eating/current-care-target checks. Bed preference is true for a Baby-stage pawn
that is downed, asleep with floor-sleep care enabled, held with rest below **0.3**, or scheduled to
Sleep with bedtime care enabled. Ordinary awake unscheduled toddlers need not be put back in bed.

Destination logic first checks native suckling/haul eligibility and a cloned bed finder. An
undowned baby's forbidden bed is rejected; downed babies retain the source's bed-area exception.
The caregiver must reach/reserve a different bed. The main priority branches are:

1. Medical rest in a safe-temperature suitable bed, retaining that bed if already there.
2. For an unsafe medical-rest bed, retain the current bed until temperature injury is **Serious**;
   for a new bed, permit it until injury is **Minor**. If that medical bed is rejected, try a safer
   ordinary bed before a floor location.
3. Outside medical rest, do not select an unsafe-temperature bed merely for sleep. Prefer a safe
   bed when bedtime/floor-sleep/downed/held-tired rules call for one.
4. Otherwise check current allowed area before temperature; if both are fine, leave the pawn
   where it is, or seek a nearby place to put down a currently carried baby.
5. Search a comfortable-temperature region, then a safe-temperature region, using the caregiver's
   traversal and both pawns' area checks. These searches do not demonstrate exhaustive inspection
   of every suitable cell on the map.
6. If carrying and no preferred result exists, try a nearby spot ignoring forbidden areas, then
   return the caregiver's own cell even when the preceding spot validator found no valid result.

The allowed-area-only fallback has its actual cell-search call commented out; it tests an old
invalid target instead. The source comment claiming a temperature-independent allowed destination
is therefore not proof it finds one. The secondary-bed finder can return null immediately for an
unsafe owned bed without searching other ordinary beds. Its deathrest/owned-bed early returns
also do not establish every promise in the method comment. The main cloned finder prefers an
existing medical/owned bed before later searches and reflects private native bed-ranking lists.
These facts prevent claiming that it always chooses the safest available bed.

Within a region, the spot finder initially rejects nonstandable/blocked cells, danger worse than
Some, fire, traps and forbidden cells. Its second attempt drops standability/blocker checks;
an optional third attempt can drop peril checks, although normal calls here do not request it.
Guest caregivers cannot choose a different room. The helper does not prove a cell remains safe
after selection. No automatic unsafe-area return or unsafe-drop fallback should defeat S030's
confirmed temperature rule in the owned composition.

The safety job reserves the baby, determines a reason, optionally announces it, approaches,
rechecks the reason, picks up, recomputes the destination, reserves/moves and uses the native tuck
helper. It stops for lost manipulation, downing, forbidden/destroyed baby or inappropriate drafting.
It checks changing physical interaction and target validity, but does not itself repeat temperature
selection during every travel step. After pickup, a forming-caravan gathering spot can replace the
primary destination solely because it is nearer, without a new explicit safety comparison here.
Future composition with caravan loading must retain safety rather than blindly prefer proximity.

The context menu offers Put somewhere safe, disabled No path feedback when unreachable, and a
separate Put in crib when useful. An unsafe-temperature crib is labeled but the source still permits
that explicit command. These are ordinary contextual mechanics, not settings or new bottom tabs;
the eventual spec must distinguish a deliberate warned order from automatic safety behavior.

Move-message modes filter medical/temperature danger and unknown reasons. Even All suppresses
the ordinary Held reason; player-forced jobs do not use automatic move announcements, and can
report already-at-best-location. Feedback must describe the selected action without claiming that
the move has already succeeded. The manual also promises a disabled explanatory safety option
when no action is possible, but this provider can emit nothing for a reachable baby already in
bed when safe-place search returns invalid: it exits before its disabled fallback, and the native
provider has been suppressed. Retain an explicit unavailable-action explanation in the future
menu contract rather than silently reproducing that missing option.

Cloned native logic and optional Altered Carbon/Hospital hooks
require the separate provider/target audit; they are not proof of drop-in compatibility.

## Decisions still to resolve during specification

Research questions remain recorded rather than sent to the user:

- **TD-Q1 catalog:** apparel, mental states, travel/carrying/combat/refugees, rendering, remaining
  XML/keyed text and DBH/HAR/provider consumers are still pending. This checkpoint does not
  complete Toddlers or authorize source removal.
- **TD-Q2 safety composition:** unify solo activity, crib exit, urgent eating and caregiver bed/
  destination selection with S030. Define safe fallback, changed conditions, medical priorities,
  warned explicit orders, unavailable-menu feedback and caravan gathering; reject loops, stale
  bedside interaction positions and unchecked destructive transfers.
- **TD-Q6 feeding/play discrepancies:** settle actual food memories/policy; floor-mess versus
  hygiene toggle scope; simulation-time loneliness/play/mess rates; independent-Play cap and
  need interruption; sad-baby crying priority; indoor bugwatching and drawing-learning threshold.
  Decide and label corrections in proposal/spec rather than inherit accidental source behavior.
- **TD-Q3/4 provider/lifecycle:** verify native beta helper semantics, target failure behavior,
  third-party crib/TV/fire eligibility, saved paired jobs and learned state. Keep settings in the
  shared inheritance model and entity policies in native contextual controls.
- **TD-Q5 acceptance:** none of the source observations proves runtime behavior, safety, optional
  provider compatibility or performance. Retain focused and final native action/result verification
  tasks when the owned feature change is drafted.
