# S121 — Bundled sample-query analysis

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, target **1.7 beta**.
Source facts from the inspected `About/DefaultSearches.xml` and compiled predicate/load behavior;
[provenance](2026-11-06-td-find-lib-research.md), [catalog](2026-11-06-td-find-lib-catalogue.md).
These are **research findings**, not selected owned defaults. The names are upstream example titles.
There are five groups, 40 searches. A group called Good Alerts does not itself register alerts.
No example issues a job, order, bill or medical treatment.

## Reading the source examples

Omitted enum fields load as their declared serialization default, not necessarily the fresh editor
choice. In particular, an omitted faction selector loads relation kind zero, hostile, whereas a
new faction predicate starts with the Player extra option. Inventory predicates missing their
direction load “held by”; fresh editor predicates start with “holds.” Missing relation submatch mode
loads Has and therefore ignores nested relation children. Numeric slider endpoints use the loaded
game's absolute bounds, so a historical literal maximum is not necessarily still “unbounded.”

AllMaps below means all loaded maps, with the ordinary source knowledge limitations. CurMap is
the default when the XML omits mapType. ChosenMaps with an empty list is unresolved stored scope;
source use-clones substitute the current map, whereas owned S033 requires deliberate binding.
Enumerable wrapper/inner behavior is described in the catalog. This table records source intent
and mechanics without claiming that stale Def/designator names resolve successfully on current 1.7.

## General samples (six)

| Sample | Actual source expression | Scope / issue |
| --- | --- | --- |
| People without Pants | Exact Human pawn without recursively held Apparel_Pants | Everyone, AllMaps. Includes carried pants as possession; does not simply mean bare legs or support every humanlike race |
| Skilled People | At least three of Shooting, Melee, Construction, Cooking, Crafting, Intellectual at level 8–20 with any passion | Everyone, AllMaps; unavailable/totally disabled skills fail |
| Single People | Humanlike pawn with no visible spouse of either sex, fiancé of either sex, or lover of any gender | Everyone, AllMaps. Redundant nested relation blocks are ignored because mode is Has; not a general compatibility/marriage prediction |
| Hunter lacks ranged weapon | Hunting work enabled and no recursively held item in WeaponsRanged | Everyone, AllMaps. Not restricted to primary equipped weapon, usable ammunition or an actual hunting job |
| Enemies In Home Area | Hostile faction pawn in home area | Everyone, AllMaps; source omitted faction selector resolves hostile |
| Enemy Gear | Item held by a hostile-faction holder | Inventory, CurMap; includes inventory/gear contents, not only worn equipment |

## Suggested alert queries (15)

| Sample | Actual source expression | Scope / issue |
| --- | --- | --- |
| Door Blocked Open | Door momentarily blocked open AND item/Other candidate on its cell | Buildings, AllMaps; Nearby radius omitted = 0; not every reason a door is open |
| %n Hungry People | Food need 0–0.103195697 | Everyone, AllMaps; no human-only condition despite title; %n is a name token for consuming displays, not a library job |
| Forbidden Animal Corpse | CorpsesAnimal category AND forbidden | Items, AllMaps |
| Damaged | Player faction AND HP/health fraction 0–0.995464921 | Everyone plus Buildings, AllMaps; below-100% threshold is a literal slider artifact |
| %n Deteriorating Items | Currently deteriorating AND allowed | Items, AllMaps; does not check whether a hauler can save them |
| %n Wild Plants | Harvestable, harvest output not WoodLog, outside any growing zone | Plants, AllMaps; “wild” is a zone test, not sowing history |
| %n Unmined Ores | Resource mineable, not MineableSteel, no selected Mine designation | Natural, AllMaps; excludes steel intentionally; source stores bare Mine as designation reference although current designator resolution uses type names, requiring a conversion audit |
| %n Tame Targets | Animal/nonplayer, wildness at most 50%, AND pack animal OR egg/milk/wool producer in specified count ranges | Everyone, AllMaps; annual limits 60 eggs, 840 milk, 600 wool are historical values, not universal maxima; no actual tame-order eligibility |
| Enemy Weapon Danger | Weapon-category item held by a hostile holder, market value 300–9,999,999 | Inventory, AllMaps; value is not combat threat or equipment use |
| Good Loot | Item held by a hostile holder, AND Excellent–Legendary OR market value 500–9,999,999 | Inventory, AllMaps; can include unequipped possessions |
| Enemies to Capture | Recruitable hostile pawn doing Wait_Downed, with passionate Shooting/Melee at least 10 AND at least one passionate Construction/Mining/Cooking/Plants/Animals/Crafting at least 7 | Everyone, AllMaps; named job rather than all downed causes; not a capture order or doctor assessment |
| Enemies to Harvest | Hostile pawn doing Wait_Downed AND health category Implant (Archotech) OR Implant (Bionic) | Everyone, AllMaps; English category bindings; source category path lacks visible-hediff restriction |
| Prisoner Noninteraction | Prisoner with MaintainOnly interaction | Everyone, AllMaps; source's literal mode name must be resolved |
| Fixable Mood Debuff | Any selected ClothedNudist, Jealous, Greedy stage 0–4, Ascetic 0–6, ProsthophobeUnhappy 0–5, NotBondedAnimalMaster 0–1 thought, OR NightOwl whose schedule differs from Sleep at any hour 11–17 | Everyone, AllMaps; fixed thought-stage ranges are not a general negative-mood classifier or guarantee that every case is currently fixable |
| Blighted plants | Blighted plant | Plants, CurMap; no cutting action |

## Everybody Gets One examples (five)

All five use Everyone with empty ChosenMaps. They are prospective bill population filters, not a
change to S018's bill limits or S033's binding policy.

| Sample | Actual source expression |
| --- | --- |
| Melee warriors | Colonist, not colony slave, not quest lodger, with Melee level at least 6 ignoring passion OR any Melee passion at any level |
| Ranged warriors | Same population restrictions with Shooting level/passion |
| Mechanitors | Same population restrictions with MechlinkImplant health condition |
| Slaves | Colony slave |
| Kids | Child developmental stage AND player faction |

## Expansion examples (eight)

| Sample | Actual source expression | Scope / issue |
| --- | --- | --- |
| Permit action available | At least one unspent permit point OR any permit not on cooldown | Everyone, AllMaps; no target/use eligibility test |
| Royal uncomfortable | Any title AND not comfortably warm | Everyone, AllMaps; the negated comfortable category includes both cool/cold and warm/hot |
| Ideological problem | Mining specialist AND Mining work not enabled | Everyone, AllMaps; not every ideology issue |
| Mechlink available | Dead pawn AND MechlinkImplant condition | Selectable, AllMaps; wrapper permits corpse result; no surgery or extraction ordered |
| Mechanitor needs mechs | At least one available bandwidth | Selectable, AllMaps; does not examine desired mech composition |
| Wasted Genes | Active strong/remarkable Animals aptitude gene AND Handling work not enabled | Everyone, AllMaps; not every aptitude/work mismatch |
| Guarenlen low | Current dryads at least 4 AND strength ≤0.769230783, OR at least 3 AND ≤0.519230783, OR at least 2 AND ≤0.269230813 | Plants, CurMap; source upper dryad bound 4 is unbounded because it equals slider max; thresholds are literal sample values |
| To Convert | Player faction AND ideology differs from player's primary | Everyone, CurMap (omitted mapType loads as Current map); does not order conversion or assess role/social consequences |

## Custom inspection examples (six)

These use unrestricted member chains in the source and belong in an owned **debug** surface unless
an individual ordinary feature is separately specified using admitted, known-only observations.
An example member chain is not a compatibility guarantee for CE, HAR or a future native build.

| Sample | Actual source expression | Scope / issue |
| --- | --- | --- |
| Low Power/Fuel | Building has battery stored-energy fraction 0–0.2 **AND** refuelable target-fuel fraction 0–0.2 | Buildings, CurMap; title suggests alternatives but source All requires both components |
| Idle Generators | Building has power-plant component and power output 0–1 | Buildings, CurMap; no fuel/power toggle action |
| Is cooking | Pawn current job's bill recipe uses Cooking work skill | Everyone, CurMap; does not match every cooking-related job without that bill chain |
| Ideology can reform | Pawn ideology development reports reform available | Everyone, CurMap; no reform performed |
| CE: Loadout has weapon | Pawn's CE loadout contains a slot whose Thing type is a weapon | Everyone, CurMap; depends on actual GetLoadout extension, slot and Def shapes; not existing possession |
| HAR: Non-human | Pawn type definition is an AlienRace ThingDef subtype and is not exact Human Def | Everyone, CurMap; depends on HAR class identity, not every modded humanoid |

## Owned draft decisions

The separate [library](../../../../openspec/changes/specify-megamod-query-library/specs/megamod-query-library/spec.md)
and [debug](../../../../openspec/changes/specify-megamod-query-library/specs/megamod-debug-queries/spec.md)
drafts specify34 ordinary and6 debug templates, explicit Add examples rather than initial automatic
population, clearer names, stable/disclosed bindings and complete numeric choices. Correcting an
example is a proposed observable change, not an upstream fact. Independent sample review corrected
To Convert to Current map; that correction is applied above.
Imported old queries must either convert without semantic loss or remain explicitly unsupported;
silently rounding arbitrary imported thresholds or dropping unresolved conditions is not acceptable.
