# S079 — Stop, Drop, And Roll! [BAL] research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Reference `balistafreak.StopDropAndRoll`, Workshop2362707956, author balistafreak, coding credit
Taranchuk. Research2026-11-04; no implementation or accepted compatibility.

## Documentation first and reference identity

Read the [Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=2362707956)
before code. It describes stationary self-extinguishing instead of fire panic/water seeking, plus
an independent apparel heat-armor ignition check. Both have colonist/non-colonist/animal controls.
The last described release is the June2025 1.6 recompile; older notes mention a water-seeking fix.
The description excludes hands/feet from sampling, distinguishes ignition prevention from direct
burn damage, and makes no tested behavior-mod or Combat Extended compatibility promise. Comments
are not verification evidence. The scraped page's generic removal/incompatibility banners are not
used to infer the item's actual availability.

Installed root `F:/Steam/steamapps/workshop/content/294100/2362707956/` contains current About metadata
supporting1.2–1.6 and a nested older `StopDropAndRoll/` copy supporting1.2–1.5. These are one package
identity, not two selected features. Analyze root `1.6/Source/StopDropAndRoll` and
`1.6/Assemblies/StopDropAndRoll.dll`; this is reference selection, not proof it runs on1.7.
No README, license, LoadFolders or manifest file was found in the bounded package file inventory.
Do not copy source/assets/assemblies or infer a redistribution license. AssemblyInfo says1.0.0.0;
source project targets net472 and exposes three patch classes, a Mod, settings and startup class.

SHA-256:

| Input | Hash |
| --- | --- |
| Root About/About.xml | `59BB2227910AA0FDE1C55074064C13F432CEE6E996C67BB1E278C88836A874B1` |
| 1.6/Assemblies/StopDropAndRoll.dll | `31BA7B9F6B942C8BD0020A4F5A81BA35E003308915E819C588BE8BED8C57D6F2` |
| HarmonyPatches.cs | `DF47B91E1693B9517FC37FAC0AFB2653308415AF692F936AC3506C57F8455472` |
| StopDropAndRollSettings.cs | `EB7985B22BFEB73EEC4FCCD663C74AC56AF06E0BDAA6EAADB05DA9C7414A10F0` |
| StopDropAndRollMod.cs | `CAA2EE7C02585B88DA2F833A15E1149227ADB0C7D144BE9AC6DAFFC0B56ED3DD` |
| Game1.7.4998rev1299 Assembly-CSharp.dll | `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |

Fully read included patch/settings/Mod source and English keys; compared all three behavior classes
and settings with read-only decompilation. Extracts under
`artifacts/Refinement/megamod-spec-20261104/stop-drop-roll/{source,native}/`. No selected behavior
discrepancy between included1.6 source and DLL was found. Startup uses unguarded PatchAll under the
source package's Harmony ID; this will not be copied.

## Behavior and defaults

Two independent sets of group booleans, plus two numeric settings, are stored globally by exact
field-name Scribe keys. UI has sliders and checkboxes plus one reset-all button and writes settings
on drawing. Numeric input is not separately validated on load. No bottom tab, custom job, Def,
animation, apparel stat, map cache or game/save component is supplied by these files.

| Setting/key | Default | UI range / semantics |
| --- | --- | --- |
| IRS | 4 | 0–10 ignition-resistance strength |
| MHA | 0.25 | 0–2 heat-armor threshold, fractions corresponding to0–200% |
| enableStopDropAndRollOnColonists | true | Disable fire panic for humanlike IsColonist |
| enableStopDropAndRollOnNonColonists | false | Same for humanlike !IsColonist, not just enemies |
| enableStopDropAndRollOnAnimals | false | Same for all RaceProps.Animal, wild or owned |
| enableCustomFireAttachOnColonists | true | Apparel ignition resistance for colonists |
| enableCustomFireAttachOnNonColonists | true | Apparel ignition resistance for other humanlikes |
| enableCustomFireAttachOnAnimals | true | Apparel ignition resistance for animals |

### Stationary self-extinguishing

JumpInWater prefix skips the native job giver when the matching group option is enabled, leaving
the result null. ExtinguishSelf prefix returns native behavior when a matching group is disabled;
otherwise an attached Fire produces the native ExtinguishSelf job immediately. It does not roll a
new probability or provide a separate rolling animation. No custom firefighting duration exists.

The negative-check implementation has a fallthrough: a pawn neither Humanlike nor Animal can still
receive the source's unconditional self-extinguish job. Its water-seeking suppression does not have
the same fallthrough. This is not a documented fourth group and should not be copied implicitly.

### Apparel ignition check

TryAttachFire prefix applies to Pawns except matching disabled groups. As with self-extinguishing,
unclassified pawns fall through. It chooses one nonmissing Outside body part from all body records,
excluding LeftHand/RightHand groups, Hand Def, Feet group **and Legs group**. The description mentions
hands/feet, not legs. Weight is each record's local `coverage`, not native damage's absolute coverage
and damage-specific hit factor. `Feet` uses a throwing Def lookup; empty/zero-weight bodies have no
explicit safe fallback. Source checks happen before the original can reject destroyed/nonflammable
or already-burning targets, so even an otherwise no-op call can consume generation randomness.

It gathers distinct worn apparel whose bodyPartGroups overlap the chosen part's groups, sorts by
last layer drawOrder descending, and gets each item's effective ArmorRating_Heat. Per garment:

`passChance = 1 - IRS * (heatArmor - MHA)`.

Each covering garment gets one independent pass check, outermost first. Any failed check suppresses
the entire native fire-attachment call; every pass or no covering apparel permits native behavior.
No natural pawn heat armor, armor penetration, equipment wear or damage cancellation is added by
this check. The helper returns an unclamped float, but native Rand.Chance returns false at<=0 and
true at>=1. Thus default25% contributes nothing,26% allows96%,49% allows4%,50% or more blocks the
attempt at that covered part. Blocking a sampled part is not whole-pawn immunity.

## Native1.7 contracts

Fully read JobGiver_ExtinguishSelf, JobGiver_JumpInWater, JobDriver_ExtinguishSelf,
ThinkNode_ConditionalBurning and FireUtility. Read Fire.DoComplexCalcs, native pawn classification,
HediffSet.GetRandomNotMissingPart, Rand.Chance and relevant BodyPartRecord/ArmorUtility fields/methods.
Some native files are reused from hunting-companions/unlimited-armor extracts of the same binary.

- BurningResponse's tree condition is an attached Fire. Order is ToolUser water search, then
  self-extinguish, then RunRandom with Deadly danger. Humanlike/Animal trees put Downed and mandatory
  lying-down handling earlier. Patching a job giver does not make an incapacitated pawn able to act.
- Native water giver seeks extinguishing terrain in its random10–16 maximum-distance search.
  Native self-extinguish has a10% activation check per job-giver evaluation.
- Native ExtinguishSelf waits150 ticks, then destroys the targeted fire and increments
  FiresExtinguished; the final toil fails if that fire disappeared. Job Def is not casually
  interruptible. Existing fire damage continues during the wait. This is not instant immunity.
- CanEverAttachFire rejects destroyed/nonflammable/non-pawn/attachment-incapable targets.
  TryAttachFire adds only when no fire exists, creates/attaches/spawns it, stops pawn jobs and
  increments TimesOnFire. Suppressing an admitted new attachment must suppress those consequences
  together without cancelling separate weapon damage or unrelated provider work.
- Fire retains terrain extinguishing, ordinary damage/heat and rain/vacuum behavior. Already standing
  in extinguishing terrain can lose fire through native updates; this mod need not force a water trip
  or preserve a fire that has naturally gone out.
- IsColonist in the inspected beta is player-faction Humanlike, not subhuman, with slaves included
  only when secure. Other humanlikes include visitors/enemies/prisoners and applicable excluded
  colonist cases. Do not relabel the source's non-colonist option as enemy-only.
- Native random damage selection weights coverageAbs*GetHitChanceFactorFor(damage), with fallback
  to coverageAbs, not each child's local fraction. Core hands/fingers and feet/toes are recognized
  by the source's groups; native legs are separately excluded by the source implementation.

## Static balance anchors, not finalized measurements

Current Core Apparel_Various.xml supplies flak vest Heat0.27, recon base0.46 and marine base0.54.
At default formula these imply92%,16% and0% per-layer pass chance at an eligible covered part, before
other effective modifiers. ArmorRatingBase has Normal quality factor1 and native cap2; Heat adds the
Stuff power*armor multiplier part. Duster multiplier0.3 with Cloth0.18/Synthread0.90/Devilstrand3.00
gives illustrative base heat0.054/0.27/0.9 and pass100%/92%/0%, respectively. Finalized effective
values, real coverage, quality and optional modifiers still need a live comparator pass.

S075 UnLimitedArmor modifies direct armor behavior; this feature only prevents new attachment and
must consume its effective heat rating without applying its direct-damage formula a second time.
S004 weather-ready apparel changes what is actually worn, so the check sees that actual clothing.
Stabilize Bleeding follows next in the queue; no invented overlap is assumed.

## External contracts, proposed corrections and remaining work

Public source helpers include Patch_CanEverAttachFire.GetApparelIgnitionChance(float) and
ApparelsOnBodyPart(Pawn,BodyPartRecord), plus public settings and the static Mod.settings field.
Installed text searches found source/build metadata, RimPy identity data and LayoutHarness example
mod lists; no actual external caller of the two helpers was found. This is not a compiled/reflection
consumer proof. Do not spoof the source package or claim drop-in binary compatibility.

The [owned draft](../../../../openspec/changes/specify-megamod-fire-response/) now explicitly proposes
positive group admission, safe missing/custom-body fallback, no chance roll on ineligible/no-op
attachment, finite validated shared settings and ordinary flame-hit weighting with legs restored.
The documented hands/feet/subparts exemption remains. These are intentional deviations, not claimed
source parity or user-confirmed numeric rebalancing. Source defaults4/25%, six booleans and native150
ticks remain. An active source owns both branches; no source settings or saves are imported.

[Coverage](../replacements/stop-drop-roll.md) maps8 requirements/24 scenarios and19 unchecked tasks.
Questions are recorded for later; do not ask the user to explain existing source behavior:

| ID | Remaining work | Boundary |
| --- | --- | --- |
| SR-Q1 | Explicit leg/native-weight proposal, custom/prosthetic anatomy and finalized equipment distribution | Source local weights/leg exclusion are not silently retained or changed; current numbers are static inputs, not live balance evidence |
| SR-Q2 | Exact effective burning tree/job/attachment and lifecycle, provider effects and semantic target admission | No isolated water-suppression patch, broad damage cancellation, unsafe whole-method skip or accepted method-name-only check |
| SR-Q3 | Actual compiled/reflected helper, race/apparel/combat contracts and source coexistence | Text search found no helper consumer; no assembly/package spoof, automatic import or unverified CE claim |
| SR-Q4 | Focused RED/GREEN, inherited settings/languages, reviewed native fire/action/damage/save/provider results | All19 runtime tasks unchecked; no diagnostic-only acceptance |

## Document review and checkpoint

Independent read-only source/native/contract review found **no issues**. The host's semantic balance
pass retains the explicit leg/weight deviation, independent direct damage and finalized comparator
work; static XML/derived probabilities are not live balance evidence. No new numeric rebalancing.

Initial typed validation returned an invocation error. Focused diagnostic validation identified a
requirement opening-line SHALL formatting issue; wording was reordered without changing behavior.
Final typed strict validation passed **101 items, zero failed**, exit0/no stderr. Eight scoped
documents passed links/whitespace checks. Draft: **8 requirements,24 scenarios,19 unchecked tasks**.
Queue: **80 reviewed drafts /136 entries /342 unique question IDs**. Next: S080 Stabilize Bleeding.
Floor Pipette Tool remains queued once as S136.

All live/TDD/package/measurement work is future; no game launch, Workshop write, foreign-source
execution or normal save/config change occurred.
