# Common Sense: sequential refinement and source evidence

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Queue **S024 / P1-18**, origin replacement. Inspected **2026-11-02**, following S023.
[Owning change](../../../../openspec/changes/specify-megamod-common-sense/) is specification only.

## Inputs and scope

Read [Workshop documentation](https://steamcommunity.com/sharedfiles/filedetails/?id=1561769193)
first, then the author's [repository/README](https://github.com/catgirlfighter/RimWorld_CommonSense).
The description covers cleaning, needs, food, inventory and wandering, but the implementation and
XML add details such as freezer-room labels, item categories and unconditional food-packing changes.
Comment reports are leads, not proof. No gameplay or full 1.7 compatibility is inferred from them.

| Input | Pinned identity |
| --- | --- |
| Source | Common Sense, avilmask, `avilmask.CommonSense`, Workshop 1561769193 |
| Installed root | `F:/Steam/steamapps/workshop/content/294100/1561769193` |
| About SHA-256 | `7D88D50091E0EBE694EB1C2118CF9B7496942352B1A11D8856B8A080F1D3220C` |
| Declared support / dependency | 1.0–1.6, Harmony required; no installed 1.7 folder |
| Inspected assembly | `1.6/Assemblies/CommonSense.dll`, file/product 1.0.9771.40664 |
| DLL SHA-256 | `2BA4EC439242F3AE190DCE5226A59BCAFDC48C72CE822C11399B021CFB990BF5` |
| Public checkout | `aed142c6121e104b78afe3b973e1677b1ff601e4`; shipped 1.6 DLL byte-matches installed DLL |
| XML correspondence | About plus nine 1.6 Def/Patch XML files: 10/10 byte matches |
| License evidence | No license grant found in checked-out README/license-file/source search; assembly copyright 2018. Independent implementation; do not infer permission to copy source/artwork from public availability |
| Native comparison | Typed inspection: 1.7.4995 rev309; Assembly-CSharp SHA-256 `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21` |

Ignored artifact root: `artifacts/Refinement/megamod-spec-20261102/common-sense`. It retains the
checkout, installed decompilation, selected native classes and consumer inspections. Source in
`Source/CommonSense16/CommonSense` was read across all behavior files, settings and XML; installed
decompilation establishes shipped settings/type inventory. A matching committed DLL does not prove
the source was reproducibly built. The legacy SocialRelax driver remains compiled and XML-named;
it is not a reason to import old saved source jobs.

## Cleaning and bill-work behavior

- Opportunistic StartJob handling is restricted to player-controlled colonists and an empty queue.
  Ordinary cleaning skips forced/queued jobs, downed/forbidden targets and a worker currently needing
  tending. Eligible building/recreation jobs and explicit CleanOnOpportunity extensions can trigger
  cleaning. Prisoner recruit/convert/enslave and ceremony spectators opt in; construction frame jobs
  opt out. Source DoBill takes a separate hauling path, then its replacement driver cleans after
  ingredient delivery. Our urgent-care and explicit-order safeguards apply consistently to all paths.
- Cleaning requires tool-user intelligence, player faction, initialized active Cleaning work and no
  disabled work, mental state or fire. Filth goes through native CleanFilth.HasJobOnThing plus allowed
  area checks. Small rooms use contained/adjacent room or doorway filth; rooms over **160 cells** or
  huge use the first **200 radial cells**. Defaults: **5** before work, **5** before bill/eating,
  **0 = unlimited** after tending. Source large-room selection can exceed the limit by adding several
  filth objects in its final cell; our total budget is exact. One selection pass, no endless refill.
- Cleaning is physical work: source in-job implementation scales by CleaningSpeed and terrain
  CleaningTimeFactor, thins filth, plays effects and increments MessesCleaned. It is not instant
  removal. Route ordering is nearest-next straight-line, not optimal routing. Pre-job detour rejection
  compares three path costs, skipping when worker→building exceeds **500** and direct ingredient
  travel is less than **70%** of travel via the building; hidden approximate mode uses **10 cells**.
  Our proposed rule uses supported actual path cost and does not expose an approximation switch.
- Successful food delivery can enqueue cleaning. After successful tending, another patient in the
  same room or predicted to bleed out within **6 hours** takes precedence. Source calls DetermineNextJob
  during that check; our design must not consume or duplicate scheduler decisions to preview them.
- Per-building DoCleanComp.Active defaults true, persisted as `active`; source `clean_gizmo=false`
  hides the control **and ignores its false value**. Proposed correction: hide controls without
  undoing saved opt-outs. Entity choices are contextual gameplay state, never global/save settings.
- Whole-stack-before-bill hauling tries HaulToInventory if defined, else HaulGeneral; ingredient
  must stack and be outside the bench room (or bench is outdoors/huge). It chooses a storage job
  whose destination is closer to the pawn than the bench, using squared straight-line distance.
  Proposed correction: actual current usable path and no recursive bill delay; shared S021 capacity.
- The replacement bill driver gathers multiple ingredient types in inventory, pauses at capacity,
  and carries/deposits actual quantities before normal production. It handles unfinished items and
  autonomous bill-giver containers. Source transfer delay is max(**30 ticks**, count × base mass /10).
  It reorders placed ingredients to preserve the dominant material. Our contract keeps real material,
  full costs, bill ownership and native production rather than copying the whole driver or its fields.
- Source ingredient sort first retains greater MedicalPotency, then perishable before nonperishable,
  then remaining rot-start time in **2500-tick hour buckets**; surgery recipes with addsHediff skip it.
  It expands candidates through native storage groups after a perishable candidate is found, excluding
  medicine additions and retaining a base validator plus reachability. Our mixed shelf/stockpile groups
  use S005 and current bill radius/access; no new map-wide inventory cache.
- Meal optimality adds **(6 − 2d)²** for remaining rot-start time d strictly between 0 and 3 days;
  bonuses at 1/2 days are 16/4. This is a preference, not permission to eat forbidden food or a guarantee
  that the earliest meal wins over every other score. It is remaining rot progress, not a forecast
  of time to spoil at the current refrigerator temperature.

## Needs, food and presentation

- Outdoors preference activates below **40% Outdoors**, tries eligible non-boring recreation types
  and accepts a psychologically outdoor target; fallback is native recreation. It may miss an outdoor
  copy when the selected recreation provider returns an indoor copy. Proposed owned selection should
  inspect supported actual eligible locations without granting unsafe access or looping forever.
- Potential-mood drug guard rejects scheduled use when either current mood or instantaneous/potential
  mood is at least the policy threshold. All other native scheduled-use rules still run. It does not
  set a new drug policy, satisfy addiction for free or authorize joy use independently.
- Patient visits can become FeedPatient for allowed enjoyable food/drugs. Source searches inventory
  then shuffled allowed drug types, weights chemical opportunity at **4**, applies boredom weighting
  max(0.001, (1−tolerance)^5), and falls back to visiting. It mixes visitor CanBeGivenTo with patient
  selection and uses deadly reach. Proposed corrections validate patient policy/ingestion and visitor
  actual access at use time, preserve urgent medical work and no forced medicines as treats.
- Joy snacks cap the selected job count to native WillIngestStackCountOf. Inventory-use override
  allows edible permitted nutrition or any CanTakeDrug item that is ingestible now; the proposed
  contract preserves each initiating purpose's policy instead of globally bypassing drug restrictions.
- Recreation hysteresis uses a saved CommonSenseJoyToppedOff bool, set after a job above **95%**,
  reset at a job start below **80%**. It replaces native 0.95 thresholds with 0.95/0.8 and sleep priority
  2 with 4 while not topped off. Proposed explicit state transitions at reached 95% / below 80% avoid
  missing state changes between jobs; forced/direct recreation remains separate. Native beta priority
  inspected: first 5000 ticks and lord prohibitions block recreation; Anything below 35% priority 6,
  Recreation below 95% priority 7, Sleep below 95% priority 2. Do not broadly rewrite those other guards.
  Review additionally checked JoyPriority.cs and installed decompilation: with default
  optimal_patching=false, the installed patch still substitutes 0.8 when fun_police=false. Our off
  state restores native thresholds and provider hygiene selection; this is a deliberate correction,
  not source off-state parity.
- Social relaxation estimates drink joy after tolerance, stops social gain near **99% minus expected
  drink gain**, adjusts for at least max(base ingestion time, **240 ticks**) worth of social gain, then
  physically ingests once. Gatherings retain their own ending rule. Source no longer replaces the
  SocialRelax Def class; legacy SocialRelaxCommonSense remains for older state. Our proposed simpler
  explicit duration/ending rule preserves native gain, consumption and interruption, not tick-copy code.
- Optional meditation economy stops nonscheduled meditation when joy is absent, at least **98%** or
  boring, and psyfocus is not needed or is **100%**. A Meditation timetable assignment is exempt.
- Optional sleeping mood movement uses **1% of the normal per-hour rise/fall per ordinary need update**,
  moving up below 50% or down above 50% toward potential mood, only on-map, frozen need, nonsuspended and
  wake-capable. Source does not clamp crossing 50%; proposed correction clamps at the nearer of 50%
  and potential mood, restricts this to actual sleep, never unconsciousness/cryptosleep.
- Meal stacking compares flags for humanlike ingredients and negative special ingredient thoughts.
  Odd-is-normal ignores the latter flag, not humanlike meat. Positive ingredient effects are not a
  separate flag despite an old comment. This adds a restriction to native stacking, not permission.
- Optional generated meal provenance chooses a producing recipe then neutral eligible ingredients
  per requirement; excludes humanlike meat, negative ingredient thoughts, nested ingredient comps
  and merge-compatibility-tag ingredients. Source hooks every MakeThing, then clears synthetic data
  on real crafting/paste/split paths, with a hidden dummy paste recipe. Proposed correction limits
  assignment to supported generated-without-provenance meals, preserves actual recipe/dispenser data,
  does not invent player-visible crafting recipes, reroll saves/splits or overwrite provider history.
- Recipe summaries list categories/material kinds/allowed items and item info includes category paths.
  Source mutates cached customSummary and can become stale; owned browser/info are read-only and use
  actual allowed intersection. No new bottom tab. S009–S012 supply the existing bill browser surface.
- Both FoodOptimality.xml and PackMeal.xml independently replace the same Humanlike packing threshold
  **0.6→0.3**, unconditionally. Proposed one guarded rule, not two edits or an unrelated toggle.
  Beta JobGiver_PackFood still checks existing inventory nutrition (>0.4 stops), colony supply
  (1.5 per spawned colonist), enough accessible chosen food to reach 0.8, native preference/diet and
  capacity. S123 will own custom routine-colonist scope; S024 records this source-derived trigger.

## Inventory, wandering and external integrations

- Manual unload marks inventory, worn apparel or equipped gear through the Gear tab; quest-lodger
  and locked apparel guards exist in UI. It waits 10 ticks, finds storage, walks there and unequips
  using apparel equip delay or **30 ticks** for weapons. Source fallback without storage only tries
  the inventory container even when the item is worn/equipped. Proposed correction performs native
  unequip and safe local physical drop, or visibly waits, retaining restrictions at use time.
- CompUnloadChecker persists ShouldUnload/WasInInventory for held items, dynamically installs itself
  on objects, resets flags on drop, and intercepts ordinary unloading. The source marks last inventory
  element after TryAdd, which is not proof of correct merged identity. Our S021 quantity/lifecycle
  policy avoids stack-wide promotion, cancelled mark resurrection and equipment pickup/drop loops.
- Return-to-inventory replaces cleanup's carried-item drop if WasInInventory; source transfer has no
  explicit capacity fallback and does not consistently return the transferred object. Owned recovery
  uses actual resulting quantities, combined capacity and safe overflow, preserves player drop/unload.
- Optional polite wandering excludes hospital/prison/freezer/containment destinations and other
  bedrooms for nonhostiles, but source tests **current-room** ownership rather than destination.
  Proposed correction uses destination ownership, allows ordinary required travel through rooms and
  never changes jobs into permission to enter forbidden areas.
- CooledStoreroom score is native storeroom score plus qualifying inward cooler count, only when base
  score is nonzero. Qualifying cooler target is below 0°C, cold side passable and in that room; source
  does not require actual power/current temperature. Retain intended freezer classification and visible
  label by capability; don't equate every cold room with food storage or overwrite unrelated room roles.
- Safe wander is optional/off: player faction, home map, no duty; mood pawns avoid unenjoyable weather,
  flesh pawns avoid fallout, and susceptible pawns avoid vacuum. Source compares fractional resistance
  stats with **100f**. Inspected native ToxicResistance/ToxicEnvironmentResistance/VacuumResistance
  XML all cap at **1**, so retain actual immunity instead of this literal threshold. Roof alone is
  not pressure safety. Source searches 100 regions, uses wander radius 7 and a 400–800 movement-cost
  budget plus200 exposed-cost cap for already sheltered wandering. Proposed bounded same-map shelter
  selection uses actual safe destination/path; no unsafe fallback excursion when no shelter exists.
- Snow/sand clearing skips whenever SnowRate, RainRate or SandRate is nonzero; retain designation,
  retry after weather, not deleting snow/sand for free.
- Dubs hygiene adapters replace toilet/water/wash priorities under fun_police: critical below 30%,
  Recreation/Sleep maintenance below 80% bladder/thirst or 55% hygiene; priorities 9.6/6.6/3.6 for
  toilet/water and 9.25/6.25/3.25 for wash. No need or externally fed pawn returns 0. Proposed behavior
  retains provider eligibility/fixture costs and urgent need ordering, not whole-provider replacement.
  Nonurgent maintenance ranks below eligible incomplete recreation (7 during Recreation, 4 during
  awake Sleep with the source policy), whereas urgent needs rank above it. The owned specification
  now explicitly distinguishes unfinished recreation, topped-off recreation and urgent hygiene.
- Source RPG inventory/Awesome Inventory adapters inject manual unload UI; Simple Sidearms removes
  dropped sidearm memory through GetMemoryCompForPawn(pawn,false)/InformOfDroppedSidearm(thing,true).
  Adapters are name/reflection based, not sufficient package/signature admission for megamod.

## Actual incoming consumers and limitations

Canonical literal scan:8 paths; broader CommonSense DLL/C#/XML scan:61 paths, including source
historical folders. This is bounded evidence, not proof that no dynamic consumers exist.

| Consumer | Inspected binding and resulting obligation |
| --- | --- |
| While You're Up, `CodeOptimist.JobsOfOpportunity`,2034960453, installed 1.5 | Reflects CommonSense.CommonSense and Settings.hauling_over_bills; opening settings can mutate either source's global option and WriteSettings. No writable-global facade compatible with our per-save model is promised; coordinate overlap without editing foreign preferences |
| Ushankas Glittertech Expansion, `Ushanka.GlittertechExpansion`,3522676478 | Assembly-qualified CommonSense.CommonSense plus Settings.adv_haul_all_ings only to display a fabricator incompatibility warning. Absence of that warning with megamod is not proof of safe custom ingredient handling |
| As above, So below II, `astryl.AsAboveSoBelow2`,3776015553,1.7 | Reflects private static OpportunisticTasks.Hauling_Opportunity(Job,Pawn):Job and transpiles two squared-distance calls. This exact internal patch cannot transparently bind our new implementation; actual route provider needs a narrow owned adapter or no cross-level detour |
| SimpleBabyCarry, `b4ttl3m3ds.simplebabycarry`,3692756679 | HarmonyBefore(net.avilmask.rimworld.mod.CommonSense) on StartJob before replacing carried-baby jobs. Preserve resulting baby/job safety via explicit admitted ordering, not source owner spoof |
| Cash Register (Continued), `Orion.CashRegister`,3509487668,1.7 | CashRegister_EmptyRegister sets allowOpportunisticPrefix=false with CommonSense-error comment. Respect the opt-out; no consumed CommonSense type |
| Pick Up And Haul,1279012058 | Source comment documents Common Sense direct workgiver calls; S021 supplies direct owned opportunity selection, not a dependency on its HaulToInventory Def |
| Repository ImmersiveChefs | Local adapter requires assembly CommonSense, Settings.adv_cleaning_ingest and Utility.IncapableOfCleaning(Pawn):bool, gated by source package. No binary emulation or runtime repository-mod dependency; any future consumer update has its own owner |

No full assembly/Def/job/save/settings facade is selected. Preserve player outcomes via narrow
supported adapters where feasible; unsupported bindings are explicit. Original Common Sense active
withholds owned overlap. While You're Up active overlapping bill detours require a supported
selection rule or our detour stays off; no foreign settings edits. Unsupported autonomous/custom
bill paths use their native workflow, retaining owned physical cargo safely. Research of provider
methods is not a runtime compatibility claim.

## Defaults, corrections and open work

Source booleans are global. Enabled defaults: separate meals, outdoors, cleaning before work/after
tending/between bill stages, full-path checks, whole-stack detours, potential-mood drug check,
batch ingredients, extended recipe summary, spoil preference (ingredients/meals), manual unload,
return to inventory, snack count, recreation hysteresis, skip precipitation clearing, patient treats,
social/meditation economy, inventory drugs, capacity respect. Disabled: odd-is-normal, generated
provenance, eating cleanup, sleeping mood, safe/polite wandering, cleaning gizmo, optimal patching.
Missing polite_wander key inherits safe_wander in source; proposed stable independent default is off.
Source optimal_patching omits disabled patches until restart; omit this preference in favor of
mandatory per-patch admission and common setting application. Capacity bypass is not offered.

| ID | Kind | Retained work, without interrupting specification |
| --- | --- | --- |
| CS-Q1 | Proposed corrections / balance | Exact cleaning budgets, protected queued work, actual immunity/bedroom ownership, bounded detours, social minimum and sleep clamp; final labor/food/recreation comparison on native beta |
| CS-Q2 | Native lifecycle / provider safety | Job continuation, committed recipe/ingestion effects, physical inventory recovery, transient cleaning targets, real scheduler and native/source save transitions |
| CS-Q3 | Consumer / direct composition | Exact provider packages/members, writable-global/internal-transpiler limits, S021 capacity, S029 drug policy, S030 toddler safety, S058/S062 care and S123 meal carrying |
| CS-Q4 | Implementation / acceptance | Individual target specs, merge-preserving XML, focused TDD, seven languages/common settings, shared-data measurement, independent review and native workflows |

No game launched. Documentation alone does not accept job wiring, optional providers, meal balance,
save compatibility or optimization. Every implemented branch needs the root reviewed-build native gate.

## Specification review resolutions

Three independent read-only reviewers examined source coverage, physical lifecycle and configuration/
consumer contracts. Six P2 findings were accepted: exclude bills from general pre-work cleanup;
specify exact manual-unload quantities and partial cancellation without cancelling S021 cargo;
retain S014's started-iteration choices across collection/cleanup/reload; preserve dormant owned
state through saves even when execution is not admitted; specify hygiene/recreation ordering; and
record the source's disabled-recreation threshold side effect as an intentional correction.
Additional host clarification makes sleep movement directional toward the midpoint and distinguishes
escape through existing exposure from an optional unsafe trip out of shelter. These remain proposed
observable behavior, not claims of an implemented fix or native acceptance.
Focused resolution reviews found no remaining issues; one overbroad sleep-direction qualifier
introduced during clarification was removed. Final strict typed OpenSpec validation passed all
45 items; local links/whitespace passed across twelve affected documentation files. The five
capabilities contain 34 requirements and 80 scenarios; all 38 implementation/acceptance tasks remain
unchecked. No runtime or optional-provider compatibility was accepted.
