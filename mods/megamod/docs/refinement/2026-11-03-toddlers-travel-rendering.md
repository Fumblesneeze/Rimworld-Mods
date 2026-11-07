# Toddlers — travel, threats, refugee arrivals and presentation

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`), S031, target **1.7 beta**.
Source research only. The [research index](2026-11-03-toddlers-research.md) pins the manual,
installed input and public revision. Paths here are relative to that checkout.

## Coverage

Fully read current source in `Source/Toddlers/`: four Caravan files outside `1.5`; the three
compiled non-legacy Carrying files; all three Kidnap and two Combat files; the four compiled
RefugeePod files; four MiscHarmony files outside `1.5`; all ten Animation files; and root
`Toddlers_DebugTools`, `DefListDef`, `Toddlers_DefOf`, `LogUtil` and `TestPatch`. Read the main
assembly version declarations. Inspected project includes and enclosing version guards:

- Three Caravan, three Carrying and two MiscHarmony legacy files are `RW_1_5`-only, not current
  behavior audited here. Their full old bodies are unnecessary to describe the selected 1.6 source.
- `Carrying/FloatMenuOptionProvider_CarryToddler.cs`, `Carrying/JobDriver_CarryToddler.cs` and
  `RefugeePod/WanderDestDebug.cs` are explicitly excluded project items. Do not infer custom carry
  behavior or debug hooks from their filenames. Root `TestPatch` contains only commented code.

Read complete `1.6/Defs/{Duties,Animations,Backstories}.xml`,
`1.6/Patches/{QuestScripts,GeneSeed40k,zz_RealisticHumanSounds}.xml` and all four English Keyed
files. Adult-job and life-stage XML were read earlier and cross-checked as needed. These are
static inputs; exact native 1.7 workflows, external targets and live visuals remain unverified.

## Caravan and carrying behavior

The manual says toddlers travel as passengers, may reach a gathering spot themselves if mobile,
and remain carryable even when awake. Current source supplies these changes:

| Source path | What it changes | What it does not prove |
| --- | --- | --- |
| `Caravan/LordJob_FormAndSendCaravan_Notify_PawnAdded_Patch` | Appends a toddler to the forming caravan's downed-pawn list | No deduplication or matching removal is added here; repeated notifications and a toddler already counted as downed need reconciliation |
| `Caravan/TreatToddlerAsDowned_Patch` | Substitutes Downed-or-Toddler checks in four selected native gathering/travel/late-join methods | It does not actually down the pawn or waive every native carrier, route or capacity rule |
| `Caravan/WantsToBeCarried_Patch` | A toddler always wants to be carried by the caravan carry tracker | Availability of a suitable carrier and actual movement speed remain native outcomes to verify |
| `Caravan/ToddlersCannotHelp_Patch` | Returns no gathering-items, gathering-downed-pawns or roping job for a toddler | Unrelated foreign work givers are not shown to obey this automatically |
| `Carrying/FloatMenuOptions_AlwaysCarryToddler_Patch` | Relaxes Downed checks for carry/exit/shuttle menus and selected carry, portal, kidnap and biosculpter job methods | Name-based reflection over MakeNewToils methods is not proof that every relevant generated helper was patched or every route works |
| `Carrying/JobDriver_Carried_Patch` | Sets the toddler's carried posture to the normal lying posture on entering the carry toil | It does not alone establish the final carried drawing offset for every renderer/race |
| `Carrying/Pawn_CarryTracker_GetGizmos_Patch` | Adds Drop carried pawn for an undrafted player-controlled carrier holding **any pawn** | It is broader than toddlers; the action tries a nearby drop and ignores the success result |

The already-audited mental-state hooks prevent Baby-stage mental states from blocking selected
caravan/shuttle paths. That broad source rule remains to narrow or deliberately adopt; ordinary
crying/giggling should not by itself strand a passenger. Native age/capacity/assignment checks,
actual carrying and safe destinations still matter. No shared item-hauling batch or extra cargo
allowance is implied by carrying a child. Compose with S133/S135 loading and S030 safety through
owned calls, retaining exact passenger identity and one reservation owner.

`MiscHarmony/FloatMenuOptions_ToddlerCannot_Patch.cs` suppresses the selected toddler's native
arrest/capture/rescue, carrying-other-pawns, dressing/stripping others, extinguishing another pawn,
childcare, corpse handling, entity transfer, caravan/pack-animal loading and reload menu providers.
This is an explicit provider list, not a proof that every possible player command is disabled.
Learning-dependent self-feeding and self-dressing are separate allowed mechanics. An optional
draft setting therefore grants positioning/control, not violence, childcare or ordinary work.

## Combat and kidnapping

Toddlers are incapable of work/violence through the earlier life-stage/work policies. The combat
group adds no-job results for native fight-enemy, approach-hostile, sapper, ambush and man-turret
givers. `Pawn.ThreatDisabled` additionally returns true for toddlers. This is threat selection,
**not damage immunity**: the manual expressly retains battlefield hazards and predator hunting.
Do not promise that every foreign targeting system automatically consumes the native threat flag.

Kidnap selection is a wholesale replacement of the native victim finder. It accepts a humanlike
player-faction pawn hostile to the kidnapper when downed **or a toddler**, reservable and not in
the caller's disallowed list. The kidnapper needs Manipulation and a path to the map edge at
Danger.Some; victim search uses the caller's maximum distance, no passing closed doors and
Danger.Some. Thus awake toddlers become eligible victims, not guaranteed captures through walls.

When a native kidnapping job targets a toddler, the postfix constructs KidnapToddler with victim,
exit target and count one. Other original job fields are not copied. Its driver requires the target
still be a toddler, then delegates taking/exiting the map. A non-hostile takee uses the Rescue
report text. Exact ongoing reservation, changed age, danger and disappearance behavior belongs in
native verification; preserve the intended vulnerability without broad uncontrolled replacement
of unrelated victim rules or silent loss of another provider's job intent.

## Refugee crash, loitering and adoption

QuestScripts XML changes the existing `RefugeePodCrash_Baby` root class. The custom root checks
the storyteller's ChildrenAllowed flag, generates a Baby-stage space refugee, initializes spawn
components early for mood, grants the traumatic-crash memory, and makes chronological age equal
biological age. The memory XML is **-20 mood for 6 days, stack limit 1**. Baby-stage generation
can produce an infant or toddler; exact age distribution and remaining quest prerequisites depend
on native generation. The source request does not force an extra warm clothing layer.

There is a **50% chance to try adding a dead parent**, conditional on an absent/untracked parent
slot. The source chooses a corresponding mother/father gender, generates a dead same-faction
space refugee with the baby's xenotype, establishes a parent relation and drops the corpse in the
same pod. It does not assert that every crash includes a parent. The pod does not join the child
to the player or make it a prisoner automatically. Cleanup behavior and already-adopted survivors
must be checked against the inherited quest lifecycle before promising safe cleanup.

The spawned subject signal starts a dedicated loiter Lord rather than an immediate map exit.
Its initial anchor is a random wander destination within **12 cells**, permitting Danger.Deadly
in that search; the duty then wanders within **3 cells** of the anchor. It allows long needs and
offers food only if the child can self-feed, plus rest. The solo-play node is commented out.
This does not prove complete play/safety behavior through other native subtrees. The user-confirmed
S030 temperature rule must apply to these arrivals too; do not inherit an unsafe loiter exemption.

On the source's **512-tick** polling schedule, adoption into the player faction removes/destroys
the Lord. A living abandoned pawn that has advanced beyond Baby changes kind to WildMan, becomes
factionless, stops jobs and leaves that Lord. The anchor and child reference are saved. Empty-group
destruction lacks an immediate return before subsequent child access; null/missing children,
reload, removal, death and adoption races need safe lifecycle behavior, not a copied cleanup loop.

The manual promises an Adopt command for an abandoned non-colonist toddler without adults of its
faction. These current source groups do not define a new adoption command; Baby-stage native
adoption is the likely inherited path, **not yet pinned native evidence**. Inspect that path before
specifying exact eligibility, relation effects or the command's final placement. Adoption is pawn
gameplay state and belongs on its contextual controls, not mod settings or a new bottom tab.

## Rendering and animation

The ten Animation files use native animation/render-tree facilities and five AnimationDefs.
These numeric source choices describe appearance, not approval to copy raster assets:

| Animation | Source appearance and activation |
| --- | --- |
| Crawl | Moving spawned pawn: body **±40°** east/west, head counter-rotation **∓20°**; south-facing map rendering is converted to north with flipped head and a 180° correction |
| Wobble | Moving spawned pawn: root swings with the walking-learning progress; cached magnitude falls from **10° to 0°**, period from **60 to 30 ticks**, using a shaped waveform through `(0.4, 0.7)` |
| Bugwatch | Root leans **±40°** east/west only for the matching job and named play toil |
| Lay at angle in crib | Matching job/toil reads the job driver's chosen angle; earlier crib research records duration and angle-change timing |
| Wiggle in crib | Matching named toil swings the root **±15°** on a **120-tick** triangular cycle; in 1.6 the worker itself does not also check the job Def |

Animation XML disables play while downed, uses root/head parts as appropriate and a root pivot
`(0.5, 0.3)` for wobble/wiggle. The base worker otherwise returns neutral scale/offset. The
locomotion setter avoids unspawned/dead/downed pawns and does not overwrite the three named play/
crib animations; learning removal clears only crawl/wobble if currently selected. These checks
are not a general animation-ownership system: other provider animations, interrupted jobs, missing
renderer/path state, portraits, save/load and race-specific body geometry remain to verify.

Only the south-facing crawl adjustment explicitly excludes portraits here. Do not infer that
every animation is portrait-safe solely from that patch. The retained Settings XML contains a
Custom renderer label/tooltip, but the inspected current settings class exposes no corresponding
live switch. A translation key is not evidence of a functioning disable-animation option.

No raster art was selected, created, changed or accepted in this pass. Future visuals still need
the repository's reference, in-game zoom/material and independent-review gates. Existing native
animations and graphic paths may inform behavior without redistributing upstream art.

## Other player-facing details

- Toddlers return false from the native IsIdle getter; Baby-stage pawns are removed from the
  Need meditation spot alert's targets. Neither change creates an ability to work or meditate.
- Backstory generation selects the Toddler category; XML supplies a simple childhood backstory.
  Preserve aging/replacement behavior rather than leaving it permanently after childhood.
- Two developer actions use the existing debug surface: spawn a Baby-stage pawn and reset
  toddler learning to age. Both require Biotech and a map. These are debug overrides; our UI rule
  keeps them behind the top debug controls, with no ordinary gameplay tab.
- English keys cover transition/first-steps letters, inability explanations, safe-place feedback,
  Play/loneliness and settings. They contain stale/unused labels and typos. In particular the
  medical-move message says “medical bed” even though the search can choose a non-medical bed.
  Owned feedback should describe the actual selected action; all seven repository languages
  remain future implementation tasks.
- GeneSeed XML inserts a toddler stage starting at biological age 1 after HumanlikeBaby on `AstarteAdaptusCore`,
  gated by display name. Realistic Human Sounds XML adds four sound references to Human's
  toddler stage, also by display name. Package/Def/shape checks and merge-preserving additions
  must replace those broad assumptions; no supported-provider claim follows from these patches.

## Remaining questions

**TD-Q8** tracks exact native adoption/carrying/departure, list deduplication, changed passenger
eligibility, kidnapping job intent, refugee cleanup/age-up, animation ownership and source-visible
versus actual provider behavior. The owned spec should retain the selected functional scope and
S030 safety, while documenting corrections. No new user interview is needed to inspect these
existing behaviors. This research does not finish their implementation or certify native 1.7 support.
