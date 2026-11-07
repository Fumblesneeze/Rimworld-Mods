# S069 — Auto-Cut Blight research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
Read-only inspection **2026-11-04**. Origin: replacement with explicit corrections/proposed control.
[Change](../../../../openspec/changes/specify-megamod-auto-cut-blight/proposal.md). No implementation.

## Documentation, source and identity

[Selected Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3520167264), installed
About and LoadFolders were read before decompilation. Installed title **Auto-Cut Blight - 1.6**,
author metadata **Defi** (Workshop display name Captain Shitface), package **Defi.AutoCutBlight**.
Location `F:/Steam/steamapps/workshop/content/294100/3520167264`. It advertises 1.0–1.6, not 1.7;
LoadFolders' newest explicit route is Common + 1.6. No source C#/repository URL, license file or
version manifest found. Selected assembly/file version **1.1.1.0**. No code/art/binary copied into product.

The linked [1.5 fork](https://steamcommunity.com/sharedfiles/filedetails/?id=3225340303) and
[original Razor 2.3 description](https://steamcommunity.com/sharedfiles/filedetails/?id=1885186534)
were also read. Original documentation identifies new-blight-triggered marking and a separate
emergency plant-cutting job, and says preexisting blight still needs manual designation. Its priority-1
explanation is historical; current native 1.7 scheduling below is more precise. Comments about whole-map
scope, uninstall problems or forced firefighting behavior are reports, not reproduced acceptance evidence.

| Input | SHA-256 |
| --- | --- |
| About/About.xml | `0DC74FABB0292047D30EF1D60590087A29D81B1B1D9DE5A70FEB24C107376632` |
| LoadFolders.xml | `90F1E7AC270C34932B2EA2F5BD099CAFC5A332A622520235151866A689C86D6C` |
| Common/Defs/CutBlightedPlants.xml | `C76CE4D7D764E30F84BDDC88EFC7F1A14623207B8684F01116617CEA5E8EC620` |
| 1.6/Assemblies/AutoCutBlight.dll | `1FC82306D68A42FD359A46B4534D798A1184C67C9EF7F3D6A1C46B576866B554` |
| 1.7.4998 rev1299 Assembly-CSharp.dll | `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |

Typed installation inspection confirms the native build. Offline ILSpy output is local under
`artifacts/Refinement/megamod-spec-20261104/auto-cut-blight/{upstream,native}/`. All five source
classes and the complete behavioral XML were read. The root legacy Assemblies folder also contains
0Harmony.dll; do not bundle or treat it as our dependency. No source assembly was executed.

## Complete source behavior

| Surface | Inspected behavior |
| --- | --- |
| Startup | PatchAll under razor2_3.AnotherRimworldMod.AutoCutBlight; copy native cutting job/report/work strings after Def lookup |
| Plant_CropBlighted.Postfix | Remove any existing native CutPlant designation, then add custom CutBlight; no current-state, duplicate, fog, zone, map-null or cut-eligibility check |
| WorkGiver_BlightCut | Scan all designations for CutBlight; skip if none spawned; accept plant category, not forbidden/burning, reservable, with custom designation |
| JobDriver_BlightCut_Designated | Native PlantCut driver with custom RequiredDesignation and Plants XP forced to zero |
| CutBlight | Custom Thing designation, native cut icon |
| BlightsCut | PlantCutting work giver, emergency=true, priorityInType=1400, prioritizeSustains=true, Manipulation required |
| BlightCutDesignated | Custom driver/report, allowOpportunisticPrefix=true |

There are no source settings, toggle, per-zone control, bottom tab, catch-up scan or custom map cache.
Work giver does not recheck current blight and bypasses newer native willingness/prevent-cut checks.
The inherited job still performs real native movement/work. Source save bindings include CutBlight,
BlightCutDesignated and the AnotherRimworldMod driver, not merely cosmetic markers. Source stripping
an existing cut designation is unnecessary when our own features can share one native order.

## Current native 1.7 evidence

Targeted Plant infection/gizmo/collection/yield sections, complete small WorkGiver_PlantsCut,
JobDriver_PlantCut, Designator_Plants/PlantsCut, BlightUtility and selected Blight, PlantWork,
IncidentWorker_CropBlight, JobGiver_Work and Pawn_WorkSettings paths were inspected. Core work-giver
and Humanlike think-tree XML supply scheduling context. This is static evidence, not live acceptance.

- CropBlighted creates a Blight only if currently unblighted. Repeated calls can return without a
  new infection, so a postfix firing is not itself evidence of a transition. Incidents and ordinary
  spread reach this native infection path; arbitrary third-party direct spawns need separate admission.
- The native Cut all blight gizmo places ordinary cut orders on existing blighted plants lacking
  a map designation. S008 adds its own explicit revealed/eligible manual scope; preserve that contract.
- Cutting a blighted plant can natively flood-fill nearby blighted plants, add ordinary CutPlant
  orders and queue targets (inspected bound 100). That is existing player-job behavior, not this
  feature's infection trigger or a new healthy-plant buffer. Do not suppress or duplicate it.
- Ordinary cutting uses native movement/reservation, PlantWorkSpeed and growth, harvestWork,
  actual plant collection/destruction and existing queued-target cleanup. Blighted CanYieldNow is
  false; the native cut driver normally gives zero Plants XP for a blighted crop. Special provider
  byproducts remain their own contracts; no added yield/XP/speed bonus is selected.
- Native cutting checks willingness and CompPlantPreventCutting; generic job selection handles
  access, forbidden state and reachability. Source's abbreviated JobOnThing does not reproduce all
  those current conditions. The native cutting path can permit Deadly path danger; do not invent
  a claim that this policy always avoids every danger or overrides an allowed area.
- Native order placement can remove all prior designations; automatic composition must not call
  that destructive path blindly. Preserve unrelated orders; an explicit cut action by the player
  still follows its existing replacement/confirmation rules.

### What emergency actually means

Pawn_WorkSettings finds the best enabled numeric priority among work types with a non-emergency
giver. An emergency giver enters the emergency list if its work-type priority is at least that
strong; otherwise it can be considered in normal work. For example Plant Cutting 2 can qualify if
the best ordinary work is 2; it does not when another ordinary work type is 1. Priority 1 is therefore
not the only case. Work-type ordering remains native. Humanlike emergency work follows allowed-area
and safe-temperature handling in the inspected branch. This does not prove immediate job interruption
or unlimited needs suppression; no new forced all-pawn dispatch is selected.

## Selected behavior and intentional differences

Use ordinary CutPlant designation and native cut job, with an owned work giver preferring currently
blighted, already cut-designated targets within Plant Cutting. Keep the source's priority 1400 as
an implementation comparator for the relative priority, emergency classification and sustained-work
preference, not a new global work-type priority. Manual/S008/native marks participate too, an explicit
composition improvement over the source's separate marker. Avoid a second designation/job system.

Only a confirmed new infection while enabled creates an automatic order. Require spawned/current
map, revealed cell and supported ordinary player cut eligibility; protected plants requiring
confirmation stay manual. Scope is map-wide within ordinary player designation authority, including
outside growing zones/hydroponics; no home-area or linked-zone filter is invented. Preserve existing
equivalent and unrelated orders, with priority choosing cutting even if a harvest marker coexists.
No healthy buffer, forced un-forbidding or forbidden/zone/plant-protection bypass.

Composition review identified an extraction conflict: preserving an ExtractTree marker while adding
and prioritizing CutPlant can still destroy the tree, defeating the existing S063 physical extraction
choice. Selected correction: existing extraction excludes owned automatic cut addition and preferred
cut selection, including when both markers already coexist. Retain normal extraction eligibility;
do not claim it always succeeds. Cancelling extraction alone is not a new infection/catch-up trigger.
Native adjacent-blight continuation already excludes any map-designated plant. Independently conflicting
foreign/native ordinary orders remain their own behavior, not a hidden rewrite by S069.

Preserve source event scope: no load/enable/reveal scan of old infections. Use the existing native/S008
manual command for an old outbreak. Cancel removes an ordinary order; repeated notification of the
same infection or loading cannot recreate it. A later actual reinfection may create a new order.
Native explicit/manual cutting and its existing adjacent-blight continuation remain available.
This avoids a persistent per-plant suppression registry merely to support a new catch-up feature.

Cut orders remain explicit orders until cancellation/removal: curing a plant does not silently delete
its order; it simply stops qualifying for the blight-specific priority at the next work choice.
Already running native work follows native lifecycle. Turning the feature off similarly leaves orders
and current jobs intact, stops new owned designation/priority, and leaves ordinary plant cutting usable.

Proposed control: **Automatic blight cutting**, initially on, one small bottom-right gameplay toggle
and common **Plants / Blight** default/override row. Its current-save click writes the same override,
not an independent state. Common inheritance, dormant overrides, main-menu disabled save callbacks,
save switching and seven languages apply. Per-plant orders remain gameplay state, not mod settings.
No new work column, bottom tab or debug button. Native icons reuse needs no new raster art.

## Consumers, optional providers and lifecycle

Scoped installed XML/CS/JSON search found source identifiers in its own XML/About, RimPy catalog
and Use This Instead replacement mappings, not a runtime consumer. The catalog identifies older
packages `razor2.3.AnotherRimworldMod.AutoCutBlight` and `tr.AutoCutBlight`; these mappings are provenance,
not proof of old binaries' compatibility. No package/type/Def impersonation or automatic source-save
migration. Existing legacy saved jobs/designations require an explicit migration decision; no claim
that removing the original mid-job is safe. Even native jobs can retain work-giver references, so
our own eventual removal/save-load behavior still needs real verification.

The broader CutBlight search found Smart Farming's same-event native CutPlant addition. Inspected
`3220129183/Source/Patch_GrowZone.cs` and Mod_SmartFarming.cs show an optional autoCutBlighted
setting and an add-if-missing postfix, package Owlchemist.SmartFarming. This is an actual overlapping
producer, not a consumer of the source API or a newly selected mod. Compatible equivalent orders are
reused; do not turn another mod's setting off. Our toggle cannot promise to stop another producer.
Original Defi source active: select it as sole blight provider, leave owned overlapping behavior
inactive and show the unavailable owned control with dormant settings. Do not remove its patches.

S008 manual/cancel commands and S006 linked growing choices remain native entity actions. S063
construction plant clearing and other producers share native orders/jobs rather than patches to our
feature. Use the native per-designation collection for candidate lookup; no repeated all-designation
scan, map/zone flood fill or private plant cache. The shared Thing index is unnecessary here.
No performance improvement is accepted without combined cost measurements.

XML Extensions is planned for required-content guards around the owned work-provider contribution;
first use requires dependency metadata. Its inspected installed route still lacks explicit 1.7
support; actual dependency acceptance remains open. Harmony required for the event bridge, never
bundled. No upstream assemblies, DLC or Gateway dependency.

## Open questions and semantic pass

| ID | Remaining work / draft direction |
| --- | --- |
| ACB-Q1 | Proposed shared toggle, ordinary-order reuse/manual priority, map-wide revealed scope and source-like no-catch-up lifecycle; retain manual cancellation and existing native propagation |
| ACB-Q2 | Exact infection-transition/virtual-provider semantics, cut eligibility, guarded work contribution, scheduling and 1.7 XML Extensions route; per-patch targets before code |
| ACB-Q3 | Source legacy save migration decision, original/Smart Farming coexistence, work-giver save references, UI/settings and effective later-repatch lifecycle |
| ACB-Q4 | Focused RED/GREEN, localization, combined costs, independent implementation review and real infection→order→pawn-cut/cancel/reload acceptance |

Semantic pass: automatic marking changes reaction time and labor scheduling, not infection chances,
spread, plant cost, cut speed, harvest yield or healthy-crop destruction. Normal safety/permission
and disabled-work rules remain; emergency work is not immunity to needs or a guarantee of instant
response. Revealed/protected eligibility is a selected correction. Native additional blight cutting
and retained explicit orders remain visible limits to cancellation/disable expectations.
Extraction precedence preserves the chosen physical-tree action but can delay blight removal when
extraction cannot currently be performed; the player can explicitly change the order.

## Review checkpoint

Draft prepared: **6 requirements, 23 scenarios and 22 unchecked tasks**. Independent source/native
review found no P1/P2 issues. Composition review's P2 extraction-precedence gap is corrected with
an explicit infection-after-extraction scenario and linked S063 composition/verification obligations.
Focused composition re-review found no remaining P1/P2. Typed strict validation passed **91 items,
zero failures**, exit 0 with no stderr. All **12 scoped Markdown documents** passed local-link and
whitespace checks. The question prefix is ACB to avoid the existing S012 Add bill browser's AB IDs.
No runtime code/Defs/assets,
tests, build/install, game launch, upstream execution, Workshop edits or normal save/configuration
mutation occurred.
