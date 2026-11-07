# Toddlers — provider integration and external consumers

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`), S031, target **1.7 beta**.
Partial specification research. Exact original inputs are pinned in the [research index](2026-11-03-toddlers-research.md).
No upstream code was executed or modified; no compatibility or migration is accepted by this file.

## Source and installed-input coverage

Read all **12 `Source/Toddlers/Compat/DBH` files**, its hygiene XML patch, the HAR bridge, all
**seven files compiled by `Source/ToddlersHARCompat/ToddlersHARCompat.csproj`**, the project file
and `1.6/HAR/Defs/HAR_DefLists.xml`. The 13 other files in the main project's `Compat/HAR` folder
are excluded `None` items; the active 1.6 adapter is the separate project. Do not port the old
graphics/life-stage implementation merely because those larger files remain in the repository.

Current installed About inputs were read on 2026-11-03; advertised support is not verified support:

| Workshop ID | Name / package | About versions | About SHA256 |
| --- | --- | --- | --- |
| 836308268 | Dubs Bad Hygiene / `Dubwise.DubsBadHygiene` | 1.0–1.7 | `4CAB64A2425847EA4AC9D2C8092EE2141C4F1BCB04DC03AFCF26569930187125` |
| 2992224079 | Hospital / `Adamas.Hospital` | 1.4–1.7 | `E709156A7654022A5EEC42426F73080FFAADF85AFEB59C27ED88E4039F787EE9` |
| 2725863762 | Celsius / `Garwel.Celsius` | 1.3–1.7 | `060F7E86355A014B7D6CFC3D7AD59868A44273F9FD32F837FE1BD55BBB6D76F3` |
| 3497264525 | [1.6] Realistic Human Sounds (Continued) / `Mlie.RealisticHumanSounds` | 1.6 | `379EE09D495297CF2EE000DB01080C3267F90A64B146A6AEF6BB5E2076A5FA88` |
| 3634874325 | Progression: Attire / `ferny.progressionattire` | 1.6, 1.7 | `D75C74EC71D1863AC54A36E1661076C1F832B601E49EBD9A1783C77EA42E93B7` |

The known HAR Workshop directory **839005762** is absent. This is not an exhaustive assertion
that no other local HAR copy exists. No provider assembly/native method has been accepted for
beta. Original startup detects DBH/HAR/Hospital/Celsius and other providers by exact display name;
our adapters require canonical package identity, exact available content and semantic admission.

## Dubs Bad Hygiene: observable source behavior

The source also explicitly recognizes **Dubs Bad Hygiene Lite**, package
`Dubwise.DubsBadHygiene.Lite`, in `Toddlers_Init`, `DBHDefOf` and the hygiene XML gate. It is a
separate provider variant to research, not proof that the full provider's APIs/content apply.
This pass pinned installed full DBH only; Lite's exact input, needs, fixtures and water accounting
remain pending under TD-Q9. Intended care outcomes apply only to the variant's admitted capabilities.

The manual describes caregiver washing for infants/toddlers, bath play, no independent toddler
washing and no infant/toddler bladder system. The source uses a mixture of runtime type reflection,
conditional Defs, XML additions and Harmony suppression of existing DBH commands/work.

`Patch_DBH` resolves required types, fields and methods for need selection, hygiene, water sources,
bottles, baths, buckets, contamination and blockage. Its reference initialization returns false
on missing members and skips its explicit need-selection patch. Other PatchAll classes and XML
are gated only by detected DBH presence, so this does **not** establish atomic shutdown of the
whole feature when one member is missing.

For a live toddler, the need-selection postfix returns false for DBH thirst and returns the
`babyBladder` flag for bladder; that flag defaults false and its checker never enables it. For
Hygiene it stores the provider's result in a **single static `babyHygiene` flag**. This last-pawn
result is not a safe per-race/per-pawn capability model. The code does not independently establish
the manual's infant need behavior; inspect the provider's native eligibility too.

When that flag is true, the Baby-stage hygiene stat part multiplies hygiene decay by **0.8** in a
bed other than SleepingSpot, DoubleSleepingSpot or BabySleepingSpot. Outside that reduction, it
adds **0.2** during one of the seven recognized solo-play jobs on terrain that generates filth or
a cell containing any filth. The potential **0.4 bladder multiplier** is inactive while bladder
support remains disabled. The XML adds Baby to HygieneLevel thought eligibility and appends the
stat part. These are source multipliers/additions, not measured final hygiene loss rates.

Washing normally requires a suckling-capable baby belonging to or hosted by the player faction,
hygiene **≤0.3**, a separate reserving caregiver, and no busy-baby condition. The urgent giver
searches the caregiver's **Urgent** autofeed relationships and has priority **9.3**. A separate
Childcare work giver has priority-in-type **80**, requires Manipulation, and is not directly
orderable. Its own `HasJobOnThing` does not filter autofeed Never/Childcare; do not claim all routes
honor the same priority relationship without reconciling that difference.

Source job selection tries a bath for a toddler not needing medical rest if Play is below **0.7**
or on a **20% random roll**. It prefers provider-usable exact bath/bucket types, rejects a fixture
forbidden to the baby, and scores distance from the baby by Manhattan cells with **+30** for a
bucket. A derived fixture type is not accepted by those exact-type comparisons. Without a bath,
it first chooses a named water bottle in caregiver inventory, then asks the provider for a clean
water source up to **9999** distance. Reachability/ownership still needs the provider contract.
The work giver calls this randomized selection during both checking and job creation, so a
candidate check need not describe the eventual chosen job.

The explicit Wash context option appears only with detected/flagged hygiene, a Baby-stage target
with the need at or below 0.3, and a non-toddler, non-mech selected pawn. Caring incapacity gets
disabled feedback. The provider declares no manipulation requirement, although the issued job
fails without Manipulation. A valid action clears the baby's forbidden flag, attempts reservation
without checking its return, and orders the job. Missing water can simply make the option vanish.
Owned UI needs truthful disabled reasons and must not silently rewrite unrelated permissions.

`JobDriver_WashBaby` reserves baby and water, takes inventory water or uses provider bottle-filling
toils, approaches and starts an **800-tick** wash with progress/effects. The baby receives a
**5000-tick** waiting job; hygiene gains **0.002 per tick**, capped at 1, updating last-gain time.
Reaching full hygiene ends early. Finish handling clears contamination, stops a matching wait
job, and calls `SplitOff(1)` on WaterExt water without retaining or destroying the returned piece.
That is **not proven consumption**. The source also runs finish handling on interruption; water
accounting, partial completion, contamination and pair ownership need an explicit outcome contract.

Bath play carries the toddler to the fixture, drops it at a rotation-dependent bath cell, starts
a **10000-tick** wait and adds Washing. The adult finds a standing cell, fills a real bath through
provider calls for up to **1000 ticks**, then plays/washes for up to **1000 ticks** or until both
Play and hygiene exceed **0.99**. The source calls the bath `IsFull` property even when constructing
the alternative bucket fill branch; compatible property ownership cannot be assumed.

During bath callbacks:

- Hygiene rises **0.001 per callback**; Play rises **delta × 0.0002 × 1.25 × room factor**.
- Loneliness cure is called without forwarding delta; the adult gains **0.000144 × 1.25** Social
  joy per callback. These mixed time bases need simulation-time-invariant owned rates.
- The source attempts a baby-play interaction at global tick multiples of **1250** and removes
  SoakingWet memories on bath start. The native thought/interaction outcomes remain unmeasured.
- Bath finish clears Washing/carried filth/the wait. A real bath clears its occupant, resets then
  rechecks contamination, checks blockage and pulls the plug; a bucket loses all remaining uses.

The adult then picks up and tries to drop the child at the standing cell; a safety finalizer is
scheduled once the fixture was reached. Several drops ignore success; `reachedBath` is not
explicitly saved. The Washing condition is added during placement, but its cleanup belongs to the
later bath-time activity, so failure during filling needs recovery coverage. The whole job's finish action awards PlayedWithMe even
if the activity never reached bath play and clears playerForced. These are source hazards, not
desired rewards or cleanup semantics. Safe transfer, interrupted-resource accounting and pair/
fixture release must compose with the owned care behavior and S030 temperature safety.

The source hides toddler Wash/stockpile-water fixture options by translated label substring,
suppresses the existing wash-patient option for babies, and unconditionally returns false from
DBH's washChild work check when detected. Replacement should suppress duplicate paths only when
the owned caregiver feature has been admitted and can meet the need. A provider lookup failure
must not leave a child with a need and no available caregiver route.

## Humanoid Alien Races: current adapter

The main bridge invokes the separate adapter reflectively and turns HARLoaded off if initialization
throws. The adapter examines all alien race Defs, reports additions/skips, and records race-specific
ages/gait. Its initialization clears report lists but **not the race-info dictionary**; a repeated
initialization can fail on duplicate entries. Per-race catches log errors without rolling back
already-mutated life stages/graphics.

The source recognizes an existing exact HumanlikeToddler Def first, deriving its neighboring baby/
child stages and using life expectancy when no following stage exists. Otherwise it looks for an
immobile baby stage followed by a mobile child stage using Def identity, developmental stage and
name hints. It skips a baby-to-child span shorter than **2 years**. The insertion formula is
**baby minimum age + max(1, round(child minimum age / 3))**. Despite its comment, it uses absolute
child age, not the span: a baby stage beginning at 20 with child at 24 would propose toddler at 28.
The owned lifecycle must keep ordered ages and derive progression from the actual race interval.

A standard HumanlikeBaby reuses the standard toddler Def. A custom baby stage is field-copied,
given a race-specific name, toddler worker/think trees, `alwaysDowned=false` and MoveSpeed factor
**0.4**, then registered. Field copying does not establish independent nested lists; modifications
of shared stat-factor objects can affect the source baby stage. Missing fields, already-custom
toddler stages and incomplete existing stage sequences also need explicit admission/fallback.

The adapter duplicates baby-conditioned graphics for toddler use across body, masks, head, skeleton,
skull, stump, swaddle and body addons, retaining original paths and non-age conditions. The copies
share some nested data. Its dictionary allows only one new graphic per parent, traversal stores a
single mutable parent rather than a restored parent stack, and an already-existing toddler does
not initialize the new toddler-age condition in the insertion branch. Duplicate siblings, root
conditions, reused/cyclic graphics, existing toddler conditions and failure rollback are therefore
unproven. Preserve provider appearance; do not apply partial global mutation and call it support.

Gait chooses whitelist first, blacklist second, then exactly **two legs and at least two arms**,
recognized through canonical parts or English name/label fragments. The whole body definition is
examined, not just currently intact limbs. XML forces Moonjelly to humanlike gait, excludes TTancon
from that gait, and blacklists Mincho from automatic toddler-stage generation. The stage blacklist
has no whitelist override. Each optional entry must be package-and-Def guarded and deduplicated.
Non-humanlike gait does not itself mean no toddler manipulation/needs; the learning helper determines
which walking condition to add. No unsupported race should be silently forced into a human body.

## Concrete installed consumer: Progression: Attire

A bounded installed `*.cs`/`*.xml` text search for `cyanobot.toddlers`, `HumanlikeToddler`,
`Toddlers.ToddlerUtility`, `ToddlerPlayDef` and `Toddlers.DefListDef` found the original mod,
Progression: Attire and LayoutHarness evaluation mod lists. This is **not a complete assembly/
reflection or arbitrary-string consumer audit**. The evaluation lists merely name the package;
no runtime contract is inferred from them.

Attire is a real inbound package/XML contract. Its About load-after entry and `LoadFolders.xml`
activate `Mods/Toddlers` only for **cyanobot.toddlers**, in the file's sole **v1.6** section, even
though About advertises 1.7. The selected 2299-byte patch:

- Renames `ApparelBaby` and replaces `BabyApparelMakeableBase` categories with LemonSkin_KidApparel.
- Adds replacing child-headgear categories for `Apparel_BabyTuque` and `Apparel_BabyShadecone`.
- Replaces `Apparel_BabyTribal` research with VFET_Tribalwear if that VFE Tribals research exists.
  The experimental shadecone research/workbench operations are commented out.

Exact installed paths are under `F:/Steam/steamapps/workshop/content/294100/3634874325`:

| Input | SHA256 |
| --- | --- |
| LoadFolders.xml | `0FA759E239D4BD167E33DE8819494D02CB2674A622022D438A0A845CDFCA43EB` |
| Mods/Toddlers/Patches/LemonSkin_Toddlers.xml | `08D35FC2542474F477ED1653D8EDA281D63A589A6B1250EB8C097EC60EEFBE7E` |

Keeping original garment Def names alone cannot activate that package-gated folder after removing
Toddlers. Preserve a deliberate equivalent through a narrow adapter or document unsupported
integration, without impersonating the source package. Our category additions should preserve
unrelated membership unless the spec intentionally selects an exclusive taxonomy. Research
consolidation must preserve actual provider progression, not erase it through global templates.
This inspection does not establish that Attire's folder loads or works on the current beta.

## Other providers and remaining contract work

Hospital's source hook applies its reflected bed-finder postfix to Toddlers' cloned finder;
Altered Carbon lookup uses `AC_Utils.IsEmptySleeve` and `AC_SleeveCasket`. Exact provider semantics,
signatures and caller-side restrictions still need read-only inspection before claiming support.
The source's Realistic Human Sounds name gate does not exactly match the installed `[1.6]`-prefixed
display name; verify actual native name matching and selected Defs rather than assume activation.
Some startup flags may be legacy-only; a detected package is not a behavior contract.

**TD-Q9** records atomic DBH admission, per-pawn needs, real water consumption, bath interruption
and recovery, HAR monotonic ages/appearance/idempotence, provider identities and the concrete Attire
consumer. **TD-Q3** retains broader inbound binary/reflection and native-beta checks. Every adopted
behavior needs an observable scenario; adapters and implementation choices belong in design.
No optional dependency becomes an unconditional requirement for unrelated megamod features.
