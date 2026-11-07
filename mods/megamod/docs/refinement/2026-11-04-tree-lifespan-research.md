# S077 — Trees Don't Just Die research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
Read-only inspection **2026-11-04**; no implementation or live acceptance.

## Documentation and identity

The [Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=2784870434) returned a
too-many-requests page. Read the installed author's About description before XML: remove limited
lifespan from trees and bonsai. A bounded web search found mirrors and a different project's wishlist;
neither is used as technical authority or proof of exact source behavior.

**Trees Don't Just Die**, author **Azuraal**, package **azuraal.treedontjustdie**, Workshop2784870434.
Installed directory `F:/Steam/steamapps/workshop/content/294100/2784870434` contains About.xml,
PublishedFileId.txt, preview.png and Patches/LifespanPatch.xml only. No DLL, source project, README,
license, translations, settings or version-folder routing. Metadata supports1.3–1.6 and declares no
dependency; this does not establish1.7 support. No reuse rights inferred and no source/assets copied.

| Input | SHA-256 |
| --- | --- |
| About/About.xml | `131F9F2E7BAAD8BFC8643E389E5D23BF8EC1B3C954041F816FC87E5466D4063F` |
| Patches/LifespanPatch.xml | `045C14E1078709CC3D54E3CF4DF02161585430F6E2980EC0F280DD98B16EA128` |
| Current native Assembly-CSharp.dll,1.7.4998rev1299 | `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |

## Complete source behavior

Read all four operations in the sole XML patch. Two Replace operations set existing local
plant/lifespanDaysPerGrowDays to0 on direct children of TreeBase and DeciduousTreeBase. Two Add
operations insert0 into existing plant nodes of direct children lacking that local scalar. Every
operation says success Always. There are no content/package/shape checks beyond these XPath matches,
no diagnostic for missed targets and no transaction guarantee. Lists and other plant fields are not
explicitly replaced. A second application replaces existing0 rather than adding another local scalar.

This is ancestry-based, not a PlantProperties.IsTree query. Deep custom descendants with their own
finite value can be missed; a direct child with no local plant node also receives no added value.
DeciduousTreeBase itself has no local plant node in current Core. It is not enough to claim every
tree-like modded plant is covered. Corrected recursive coverage is an explicit proposed extension.

## Native consumers and observable consequences

Read current PlantProperties, Plant.CurrentDyingDamagePerTick, TickLong, ExposeData and display
paths, reusing exact-runtime extracts under `megamod-spec-20261104/cut-plants-before-building/native/`
and `auto-cut-blight/native/`. New WildPlantSpawner extract is under sibling `tree-lifespan/native/`.

- PlantProperties default lifespan multiplier8; LimitedLifespan iff multiplier>0. LifespanDays is
  growDays×multiplier and LifespanTicks casts days×60000. Core TreeBase sets multiplier9.
- Plant.CurrentDyingDamagePerTick includes age damage0.005 when LimitedLifespan and age strictly
  exceeds LifespanTicks. It takes the maximum with separate darkness, inappropriate light, vacuum,
  pollution and terrain damage. Removing this one term does not cancel another cause's damage.
- TickLong continues incrementing age by2000, growing normally and applying ceil(damage×2000).
  Therefore the source removes only age-driven damage, without healing existing HP or resurrecting
  dead plants. Other leafless/damage/fire/chop paths are unchanged. Save age uses native key `age`.
- Native info cards omit Limited lifespan, lifespan duration and the instance age row when
  LimitedLifespan is false. This is existing native presentation, not a new display patch.
- IsTree means harvestTag Wood OR forceIsTree. That includes fibercorn and stumps as well as real
  tree-family plants, so replacing every IsTree plant's lifespan would broaden source intent.
- WildPlantSpawner.SpawnPlant randomizes initial age only for limited-lifespan plants; growth still
  receives its normal random initialization. CheckSpawnWildPlantAt retains occupancy/cover/edifice,
  fertility/snow/sand and saturation checks; PlantChoiceWeight uses existing lister counts and local
  proportions. Longer-lived trees can affect native occupancy/composition, but no fixed population
  increase or spawn-rate reduction is guaranteed. No new density control or spawning hook is selected.

## Inspected content and static balance anchors

Read Core Plants_Bases, relevant temperate/arid/special/farm entries and Ideology bonsai/fibercorn/
Gauranlen entries, with Biotech/Odyssey special-tree lifespan fields. Source XML quantities below
are not finalized loaded-Def measurements or observed plant lifetimes:

| Content | Native growth days × lifespan multiplier | Age threshold | Scope |
| --- | --- | --- | --- |
| Oak, DeciduousTreeBase | 30×9 | 270days | Covered tree |
| Pine, TreeBase | 20×9 | 180days | Covered tree |
| Saguaro cactus, TreeBase | 5×40 | 200days | Covered tree-like plant, explicit override |
| Ideology bonsai, TreeBase | 1.5×9 | 13.5days | Covered only when that optional content exists |
| Timbershroom, TreeBase | 20×9 | 180days | Covered; light sensitivity remains |
| Rice, PlantBase | 3×8 default | 24days | Not covered |
| Fibercorn, PlantBaseNonEdible | 6×8 default | 48days | Not covered despite Wood harvest tag |

Gauranlen and inspected Biotech/Odyssey special trees already set multiplier0; preserve their
special mechanics. Core stumps are DeadPlant, isStump true, lifespan0 and canDeteriorate with
DeteriorationRate4; do not turn this into a general decay-prevention feature. Keep native harvest
yields, work, costs, growth/research availability and threats; longer standing timber/less bonsai
replacement is the intended balance change. Exact forest-turnover/biome impact remains TL-Q2.

## XML Extensions capabilities and limits

Primary [ForEachDescendant](https://github.com/15adhami/XmlExtensions/wiki/XmlExtensions.ForEachDescendant),
[ConditionalInherited](https://github.com/15adhami/XmlExtensions/wiki/XmlExtensions.ConditionalInherited)
and [FindNodeInherited](https://github.com/15adhami/XmlExtensions/wiki/XmlExtensions.FindNodeInherited)
documentation was consulted. Read selected1.6 assembly types read-only: ForEachDescendant/HybridPatch
extracts under sibling `walls-are-solid/xml-extensions/`, new ConditionalInherited/FindNodeInherited
extracts under `tree-lifespan/xml-extensions/`. Current framework metadata/wiki advertise through1.6;
beta loading remains unverified. XML Extensions and its Harmony dependency are required if used.

ForEachDescendant recursively visits ParentName/Name descendants, optionally concrete only; it does
not include its root, detect cycles or resolve finalized inheritance. ConditionalInherited checks for
any matching node up the ancestry, so a child false/non-Wood override can still match an ancestor's
true/Wood value. FindNodeInherited returns the first matching path; neither is a full native merger
for Inherit=False/IsNull semantics. HybridPatch supports Safe recursive container traversal and
AddOrReplace for one scalar, preserving sibling lists. None of these alone proves atomic rollback.
Use guarded XML scalar contributions with verified current inheritance and content selection; do not
blindly equate an ancestor match with final eligibility or rewrite whole plant records from C#.

## Contracts and proposed disposition

Bounded installed C#/XML/JSON package-ID search found source metadata and RimPy Mod Manager Database
only. The latter contains a descriptive Workshop/package record and empty dependencies, not a
runtime consumer. There is no source callable API, custom Def, save key or settings schema. No
compiled/reflection compatibility claim; no identity spoof or save conversion is proposed.

Keep native trees/tree-like woody family including optional bonsai, recursively cover compatible
descendants, exclude stumps and unrelated crops such as fibercorn. Preserve other death causes,
native age/save and information semantics. Original provider active: leave owned contribution off
before mutation and explain authority. Already-unlimited valid targets are satisfied, not errors;
missing/ambiguous/malformed required content fails the affected contribution with bounded startup
warnings and no partial plant record. Other compatible changes are allowed. No settings, bottom tab,
private cache or Harmony death override. Exact XML target documents/admission still precede code.

TL-Q1–4 track XML/inheritance/admission, finalized scope/balance, actual external/provider
contracts and implementation/native acceptance.

## Reviewed documentation checkpoint

Independent source/design review found no issues in upstream XML facts, native age/damage/UI/save/
spawning behavior, explicit recursive extension, exclusions, provider authority and guarded merge
requirements. Exact1.7 XML Extensions loading, effective inheritance and atomic staging remain TL-Q1;
the review did not accept those runtime claims.

Draft: **6 requirements, 15 scenarios, 16 unchecked implementation/acceptance tasks**. Typed strict
OpenSpec validation passed **99 items, zero failures**, exit0/no stderr. Eight scoped Markdown
documents passed local-link and whitespace checks. No runtime code/Defs/assets, tests, build/install,
game launch, source execution, Workshop edit or normal save/configuration mutation occurred.
