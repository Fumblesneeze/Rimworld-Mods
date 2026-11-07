# Show Buildable Material Count — source research

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; S032, target **1.7 beta**.
Inspected **2026-11-03**. This is static research, not implemented behavior or native acceptance.
P1-25's arrow to Ctrl+F was explicitly clarified by the user: share its index/cache, not a
click-to-search action. That requirement also composes with hauling's shared data.

## Documentation first and exact inputs

The [Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=3245101540)
describes on-hand counts in buildable popup menus. The
[author's change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/3245101540)
say the 2026-10-21 update added 1.7 metadata without changing the mod, the 2025-07-04 update
recompiled for 1.6, and earlier updates addressed small-volume resources, multiple floor choices
using one resource, Fertile Fields, dropdowns and material-free buildables. Those are historical
author statements, not verified current compatibility. Comments mentioning Floating Menu or
Digital Storage are investigation leads, not reproduced defects or selected new requirements.
The public page also contains generic incompatibility/removal boilerplate; it is not evidence
against the inspected installed package. No public source repository was located by the bounded
title/assembly search, and the installed About URL is empty.

Installed input: `F:/Steam/steamapps/workshop/content/294100/3245101540`.
Name **Show Buildable Material Count**; package **BP.ShowBuildableMaterialCount**; About author
**BustedPretex**, Workshop display **BustedPretext**. About advertises **1.5/1.6/1.7**, requires and
loads after Harmony. Files consist of About metadata/preview and one assembly under each version;
no source, license, Defs, settings, languages or LoadFolders file were present in this inventory.

| Input | SHA256 |
| --- | --- |
| About/About.xml | `31E21C0CE99937786A9BA4D52EA05097512824E7DF9EBCA4CFC2BF248B3B38DE` |
| 1.7/Assemblies/ShowBuildableMaterialCount.dll | `880720D2BBFE3CBD968A1AA02617D839D8CE6CB438882429020D08E000B8460A` |
| 1.6/Assemblies/ShowBuildableMaterialCount.dll | Same bytes as 1.7 |
| Current native Assembly-CSharp.dll | `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21` |

ILSpy **11.2.0.10050** decompiled the selected installed 1.7 DLL read-only to ignored
`artifacts/Refinement/megamod-spec-20261103/buildable-material-count/decompiled`.
All **eight behavioral/support type files plus AssemblyInfo** were read completely: Buildables,
Floors, Common, Patches, ShowBuildableMaterialCountMod, BuildDict, buildElement and Utils.
Assembly/file version is 1.0.0.0; metadata targets .NET Framework 4.7.2 and references Assembly-CSharp,
Harmony and System.Core. No source project was compiled or code executed. Initial decompilation
has missing-reference type comments; native definitions and explicit casts were separately checked.
No license grant is assumed; independently implement behavior without source DLL/code/art copying.

The typed installation inspector reports **1.7.4995 rev309** at
`F:/Steam/steamapps/common/RimWorld`, target framework net10.0. Exact native read-only type outputs
under this evidence directory's `native/` cover ResourceCounter, CostListCalculator and
Designator_Dropdown in full; relevant ProcessInput/cost/visibility paths in Designator_Build,
BuildableDef and GenStuff; selected FloatMenu/FloatMenuOption/WindowStack construction, label and
Add paths. This is original assembly evidence, not effective foreign-patched code or a live run.

## Source menu behavior

`ShowBuildableMaterialCountMod` is a startup static class holding process-global buildable/floor
flags, last buildable and an unused buildList. It calls `Patches.DoPatches`, which finds three
methods by name and stops if one is missing. It applies:

1. `Designator_Build.ProcessInput` prefix and postfix, setting/clearing buildable context when
   reflected `entDef` is a Stuff-made ThingDef.
2. `Designator_Dropdown.SetupFloatMenu` postfix, recording terrain/material/count/cost rows.
3. `WindowStack.Add` **prefix**, despite its handler being named WindowStackAdd_postfix. It takes
   `ref FloatMenu window`, while native Add takes Window. That narrower parameter is an admission
   risk to inspect in actual Harmony binding, not a demonstrated crash in this static pass.

The prefix annotates whichever FloatMenu arrives while a global flag is set, then clears both
flags. No per-menu ownership, exception-safe scope, idempotent label marker or semantic target
guard is supplied. Name-existence messages are Log.Message, not the required structured skip
warnings. Patch installation has no explicit transactional rollback.

### Stuff-made buildings

`Buildables.doBuildable` reflects FloatMenu.options and each option's shownItem. For every option
it loops through currentMap.resourceCounter.AllCountedAmounts keys. A key must be Stuff, able to
make the selected buildable, and have any map lister Thing or god mode enabled; stringified Def
identity must match shownItem. The label is prefixed **stored count / CostStuffCount**, with the
denominator multiplied by ten for smallVolume Stuff. It does not include other fixed ingredients
or an extra fixed cost in the same material. Exceptions are silently swallowed; failure to read
an option's shownItem breaks the entire option loop. No shortage color, filtering or disabling
is added; native actions and existing text are retained.

### Terrain dropdowns

`Floors.desdropdown_SetupFloatMenu_postfix` reflects Elements and GetDesignatorCost, clears a
global dictionary and examines every element, not just visible ones. Only Designator_Place with
TerrainDef is accepted. It finds a resource-counter key that is Stuff and string-matches the
native selected cost material. It records that material's stored count but assigns the cost from
**PlacingDef.CostList.First().count**. Those can refer to different ingredients.

The dictionary key combines buildable and material names. However, `doFloors` matches each
actual menu option **only by shownItem material**, prefixes the first matching entry's saved
count/cost, then breaks. Two floors with the same material and different costs can therefore be
annotated with the first floor's cost despite the historical fix claim. Rows with placed-icon
presentation need not have shownItem at all. Null/non-place elements and terrain whose cost material
cannot be matched make setup return before setting floorsClicked, so later good choices can lose
annotations. By contrast, a place designator with null PlacingDef, a nonterrain placing Def or null
native cost is already skipped with continue. Old floorsClicked is not reset on entry to the failed
return path. There is no grid-menu
hook, even though current native dropdowns can use grids.

Counts are snapshots taken at menu construction. There is no stock-change invalidation or open-
menu refresh; BuildDict is only a transient menu cache, not a shared map Thing index. BuildDict and
its public fields/methods are not a documented extension API. Utils.PrintAllMembersWithValues is a
reflection logger with no callers in these files and no player/debug menu. No settings or saved
source state were found. Merely reading this assembly does not prove safe source removal in all
external environments.

## Exact native facts informing the owned contract

### What the count actually measures

Native ResourceCounter updates every **204 game ticks**, and some CheckUpdateResource calls can
refresh it sooner. It resets the dictionary from CountAsResource Defs, then enumerates each storage
SlotGroup's HeldThings, unwrapping minified items and summing stack counts when ShouldCount passes.
ShouldCount rejects nonfresh items and fogged held positions. No forbidden, reservation, pawn-path
or faction test appears here. Thus this is **counted stored stock**, not every material on the map
and not a guarantee a particular builder can use it. Loose material outside storage, ordinary pawn
inventory/carry and material already inside construction are not enumerated by this loop merely
because they exist. Supported storage-provider semantics still need exact adapter evidence.

GetCount returns zero for Uncounted priority and logs/adds a missing counted key otherwise. Our
display must not call that to fabricate zero for an unsupported resource. A broader Ctrl+F candidate
set must supply a storage/count-policy view for this feature; sharing data does not equate filters.
Actual slot membership, fog, freshness and stack changes must be current for the owned view.

### Costs and row identity

Native BuildableDef.CostList and CostStuffCount can select a difficulty-specific alternative.
CostListAdjusted converts Stuff cost by **round(CostStuffCount / VolumePerUnit)** with minimum one
when Stuff is selected, and merges fixed costs in that same material. Fixed CostList quantities are
already item counts; multiplying all of them again for smallVolume would be wrong. The native cost
cache keys buildable/Stuff and resets on difficulty identity changes; other dynamic provider changes
still need their own contract.

Native building ProcessInput makes material options from counted resource keys, sorted by Stuff
commonality then market value; it requires CanMake and a spawned map Thing unless god mode. It
keeps selection callbacks, tutor tags and close handling. The owned annotation must not change
these choices, research/visibility, ordinary blueprint permission, reservations or costs.

Native dropdown SetupFloatMenu includes only visible elements. Its cost icon chooses the item
with greatest **BaseMarketValue × count**, not necessarily CostList.First. Other rows use the
placed object's texture or plain label. SetupGridMenu provides equivalent grid selection and
sometimes an eyedropper entry with no construction cost. Exact designator/buildable identity must
travel with each row; neither displayed label nor icon is an adequate key. Label's native setter
updates size calculations, but long/localized annotations and grid tooltip behavior remain live UI
acceptance work. WindowStack.Add runs PreOpen/insert/PostOpen after its input is supplied.

## Consumers, composition and deliberate corrections

A bounded installed `.cs`/`.xml`/`.json` search for the package, namespace prefix and Harmony owner
found only the source About and **Use This Instead** (`Mlie.UseThisInstead`, Workshop 3396308787)
replacement metadata. Its JSON suggests this source for old StuffCount (`ruyan.stuffcount`); it
does not consume the public dictionary or patch methods. No actual runtime API consumer was found
in that text scope; this is not an exhaustive binary/reflected consumer audit or drop-in guarantee.

Owned choices for the draft:

- Keep contextual stored/required numbers for material choices and terrain dropdowns, with no new
  tab, settings window or click-to-Ctrl+F action. Also support native grid choices through their
  tooltips and resolvable nonterrain construction dropdown rows; these are explicit UI extensions.
- Bind each annotation to the actual row and selected material, use adjusted costs including the
  same-material fixed component, and retain all additional ingredients in explanatory detail.
  Handle an unsupported/zero-cost/utility row locally without suppressing later valid rows.
- Preserve counted-stock scope and ordinary menu/actions. Zero stock is not a ban on planning;
  missing/unsupported information is not zero, and sufficient stored stock is not path/permission
  or reservation assurance. Do not add requested-by-comment hiding/color features by inference.
- Use one shared map-owned Thing source/index with S033 Ctrl+F and hauling, then a distinct current
  storage-count view. Do not copy the global flags/dictionary or create a per-feature full scan.
  Recompute after relevant changes before the next displayed query; never silently reuse another
  map's data or subtract planned orders from raw stored totals.
- Preserve upstream labels/actions and harmless third-party changes. If the original provider is
  active, leave owned annotations inactive with a warning instead of double-prefixing labels.
  Unknown custom menu/storage providers remain research/admission work, not blanket support.

## Open work retained for later

- **BC-Q1:** exact supported native row/grid association, other menu-provider contracts and effect of
  foreign patches; target specs must prove semantic row/cost binding, not only method existence.
- **BC-Q2:** shared storage-view freshness and membership, fog/freshness/minified/provider paths,
  current-map lifecycle and combined Ctrl+F/hauling maintenance cost; no performance claim yet.
- **BC-Q3:** compatibility consumers/co-installation, optional virtual storage and unusual costs;
  no count or ABI claim for unsupported provider content.
- **BC-Q4:** focused tests, seven-language layout, correct menu actions, actual hauling/construction
  evidence, build/package checks and final native acceptance remain wholly unimplemented.

## Draft and verification checkpoint

[Owned change](../../../../openspec/changes/specify-megamod-buildable-material-counts/proposal.md)
and [coverage](../replacements/show-buildable-material-count.md) contain one capability with
**9 requirements / 16 scenarios**, plus **19 unchecked implementation/acceptance tasks**.
Independent source review checked all nine files, native paths, hashes/version and the bounded
consumer search. Its one correction distinguishes floor-setup aborts from existing continue
branches; a focused follow-up confirmed resolution. Independent contract review found no substantive
issues. Strict typed OpenSpec validation passed **54 items, 0 failed**. Eight scoped Markdown files
passed local links/whitespace checks; the queue has **136 unique entries, 33 drafts and 148 unique
question IDs**. No gameplay, source-removal, provider compatibility or optimization is accepted.
