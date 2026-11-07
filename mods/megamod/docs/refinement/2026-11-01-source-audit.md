# Full handwritten-list source audit — 2026-11-01

Owner: Make Rimworld Great Again (`fumblesneeze.megamod`). This is a refinement audit, not an
OpenSpec, implementation, compatibility decision, or claim of feature parity.

The two **original**, unedited photographs were re-read against all 117 entries in the
[transcript](2026-11-01-handwritten-notes.md). A fresh recursive scan of the local game Mods folder
and RimWorld Workshop directory found **1,517 About.xml files**, all parseable. One is a translated
About file without a package ID; it was not treated as another mod. Nested/version-specific metadata
files and repeated package IDs are retained as evidence, not counted as separate source identities.

Installed means files are present in those directories. It does not mean active, compatible with
the target beta, or selected for implementation. Every candidate below has an exact metadata path
and hash in [the source index](installed-sources.json). The [machine-readable reconciliation](source-audit.json)
records the per-entry mapping and classifications.

Interview rule clarified on 2026-11-02: investigate existing source behavior/defaults directly.
Do not ask the user to explain or reconfirm them. Ask only about unresolved custom intent or
genuine ambiguities that remain after research. Outstanding source work below is not a user questionnaire.

## Later additions outside the photograph audit

On **2026-11-02**, the user added **Drug Response (Continued)**, requiring its UI to be redesigned.
Read-only installed metadata confirms `Mlie.DrugResponse`, Workshop **2196401201**:
[About.xml](<F:/Steam/steamapps/workshop/content/294100/2196401201/About/About.xml>), SHA256
`32A4A040E3A9D68B08C84BA834C81FAA993F0B360BDC122606F9F8B44BC63CBD`.
Its metadata points to [the source repository](https://github.com/emipa606/DrugResponse).
This is an identity check only; behavior/source/UI analysis and beta compatibility remain unverified.
It is tracked as **S124** in the [specification queue](specification-progress.md), separately from
the original 117 photographed entries and their historical machine-readable reconciliation.
The user's later **Priority Treatment (S134)** addition explicitly requests analysis of compatibility
changes/consolidation with this feature; see **DR-Q2/PT-Q1** and the intake below. Its mandatory UI
redesign still applies. Rechecking Drug Response's About on 2026-11-03 found the same hash.

The later hauling update is recorded in [its own identity supplement](2026-11-02-hauling-reference-update.md):
S021 now selects Continued **3829549663** and compares Reworked **3813653998**; S125 adds While
You're Up with the **3832180338** 1.7 port and **2034960453** original as references. Exact package
IDs, installed About/binary hashes and evidence limits are in that supplement. Historical photo
matches and the original machine-readable inventory remain unchanged; they do not claim the newer
references were inspected in the original photo audit.
On 2026-11-03 **dd.Armful Haul**, `dd.armfulhaul`, Workshop **3808100512**, was also added as
comparison input and required visible-cargo subset **S126**; its exact hashes and evidence limits
are in the same supplement. The author's PUAH incompatibility statement is not a reproduced defect.

On 2026-11-03 **RimHUD** (`Jaxe.RimHUD`, **1508850027**) and **RimHUD Passion Icons**
(`DanZinagri.RimHUDPassionIcons`, **3829500081**) were selected as **S127/S128**. Their
[identity supplement](2026-11-03-rimhud-reference-intake.md) records About/binary hashes, initial
documentation, version-declaration limits and pending direct-composition research. They are outside
the historical photo audit; neither source has a completed behavior specification yet.

On 2026-11-03 the user selected **Interaction Spot On X** from
[Workshop 3827781564](https://steamcommunity.com/sharedfiles/filedetails/?id=3827781564), now **S129**.
Installed About and PublishedFileId confirm **Dragonissa**, package **`dragonissa.InteractionSpotOnX`**,
advertised game versions **1.6/1.7**, and a required Harmony dependency. About has no modVersion.
Its description concerns allowing building interaction spots over chunks and trees; precise
obstacle scope, options/defaults, placement restrictions and actual pawn-use effects remain **ISX-Q1**
research. This is a separately selected source with its own future megamod-owned change.

- Metadata: `F:/Steam/steamapps/workshop/content/294100/3827781564/About/About.xml`, SHA256
  `45B83B16C7DB505B186FC7CFF3AA1EBAA8D640162BC15E9EF7190BC3EFE35DA2`.
- Assembly: `F:/Steam/steamapps/workshop/content/294100/3827781564/Assemblies/InteractionSpotOnX.dll`,
  SHA256 `8848B5ADD25C04A34E2D1337FD5DA0755E6A82D4945DB179BA557249D62FE144`.
- The Workshop page could not be retrieved through the web tool in this intake. The initial
  description above comes from installed author metadata; third-party search summaries were not
  adopted as behavioral evidence. The bounded local inventory found no C# source, README, license
  file or LoadFolders.xml. Source discovery/read-only decompilation, licensing and complete behavior
  analysis remain queued; no runtime or compatibility claim follows from the version list.

S129 follow-up on2026-11-06: [complete research](2026-11-06-interaction-spot-on-x-research.md) and
[owned change](../../../../openspec/changes/specify-megamod-interaction-spot-placement/) supersede
that initial research status. Author page, all public/compiled code and native1.7 predicates were
inspected. Chunks default on and trees off; selected behavior is only a placement exception with
ordinary clearing/use rules. Bounds, ignored-object and unrelated-rejection preservation correct
the source's broad acceptance override. ISX-Q2–Q4 retain implementation and native proof.

On 2026-11-03 the user selected **Dynamic Spacefarer Policy**, now **S130**. Installed About and
PublishedFileId confirm **asunib**, package **`asunib.DynamicSpacefarerPolicy`**,
[Workshop 3829638797](https://steamcommunity.com/sharedfiles/filedetails/?id=3829638797), advertised
game versions **1.6/1.7**, and a required **Odyssey** dependency (`Ludeon.RimWorld.Odyssey`). About
has no modVersion. Its author description says vacuum-resistant apparel, including modded gear,
is included automatically in the Spacefarer policy, existing saves are supported and manual
exclusions persist. These are author claims pending implementation inspection, not tested results.

- Metadata: `F:/Steam/steamapps/workshop/content/294100/3829638797/About/About.xml`, SHA256
  `877451BC51B3D6E872539060E95D2D035354665A099A4E63EE7C82344E0F68A5`.
- Selected beta reference: `F:/Steam/steamapps/workshop/content/294100/3829638797/1.7/Assemblies/DynamicSpacefarerPolicy.dll`,
  SHA256 `47600F0044E67E3A40F46D3E0C7E5EF34DEDE774792C2EB90DB4B27097954B28`.
- Workshop retrieval failed through the web tool; initial behavior comes from installed author
  metadata. The bounded inventory found separate 1.6/1.7 assemblies, with no C# source, README,
  license file or LoadFolders.xml. Source discovery/read-only decompilation and licensing remain
  research work. No source behavior, beta compatibility or complete replacement is accepted here.

**DSP-Q1** tracks precise apparel eligibility, update timing, policy identity, existing-save behavior
and preservation of manual exclusions. Inspect content availability and absent-Odyssey behavior
before choosing dependency/admission policy; the source's DLC requirement is not automatically a
hard requirement for the whole megamod. Apparel-policy choices stay contextual gameplay state under
the existing UI rules. This separately selected source gets its own future megamod-owned change.

S130 follow-up on2026-11-06: [complete research](2026-11-06-dynamic-spacefarer-policy-research.md)
and [owned change](../../../../openspec/changes/specify-megamod-spacefarer-policy/) supersede that
initial research status. The author page was retrieved, selected DLL and native policy types read,
and the installed lowercase `loadFolders.xml` found (correcting the initial inventory omission).
Source inclusion is once per qualifying Def and matches the first translated policy name. The
draft preserves manual choices with stable identity/history and explicit initial enrollment for
old saves without trustworthy history; it does not copy global tag mutations or import source keys.
DSP-Q2–Q5 retain exact lifecycle/provider/migration and native acceptance work.

On 2026-11-03 the user selected **Quality Colors (Continued)** as **S131** and **RPG Style Inventory
Revamped** as **S132**. Each receives its own megamod-owned change during the sequential deep dive.
Installed About and PublishedFileId files establish these exact editions:

| Queue | Installed title / author | Package ID | Workshop | Version/dependency evidence |
| --- | --- | --- | --- | --- |
| S131 | Quality Colors (Continued) / DawnsGlow | `DawnsGlow.qualcolor` | [3513846773](https://steamcommunity.com/sharedfiles/filedetails/?id=3513846773) | No modVersion; advertises 1.2–1.7; requires Harmony |
| S132 | RPG Style Inventory Revamped / About credits Sandy; description credits avilmask, Workshop author avil | `Sandy.RPGStyleInventory.avilmask.Revamped` | [2478833213](https://steamcommunity.com/sharedfiles/filedetails/?id=2478833213) | No modVersion; advertises 1.2–1.6; load-after CE, incompatible with original `Sandy.RPGStyleInventory` |

Evidence paths below are relative to `F:/Steam/steamapps/workshop/content/294100/`:

| Evidence file | SHA256 |
| --- | --- |
| `3513846773/About/About.xml` | `BBCAF5DB4CDF5633D7E200E1B8319A5EDE483AC7CA708F8BD5B556B57BB140E4` |
| `3513846773/1.7/Assemblies/QualityColors.dll` | `3D3003B6FA4D6E31B4DAC1F728F530C6CD67A23159378E22DC28139AECEDD1DE` |
| `2478833213/About/About.xml` | `A7F2DF1349D76BFE1A00B69B0C54B9112B58853E06A6DC95943FCA93DB12B565` |
| `2478833213/1.6/Assemblies/Sandy_Detailed_RPG_Inventory.dll` | `CF30287E5A95597866690B27892097E8FD9BA8762A55418E9F4A3080EA96D143` |
| `2478833213/1.6/Assemblies/CEPatches.dll` | `7C8A2193A917D8DEE033270C50284DCE830449EC3F0ED4C6B9715032D62024E5` |

Quality Colors' installed author description specifies customizable item colors based on quality
in the trade window. Its broad compatibility assertion remains unverified. Workshop retrieval
returned 429; no additional display surfaces or defaults were inferred. **QC-Q1** retains complete
surface/mapping/settings/persistence and contract analysis, using our common settings/UI rules.

**2026-11-06 S131 follow-up:** that intake limitation is superseded by
[complete selected1.7 research](2026-11-06-quality-colors-research.md). Author Steam content was
read successfully before full installed decompilation; public repository source retrieval failed.
Six presets, expanded native surfaces, picker/persistence/cache behavior, nine native types and
two real external consumers are now recorded. The [owned draft](../../../../openspec/changes/specify-megamod-quality-colors/)
uses common palette inheritance, current localized formatting and transparent compatibility limits.
DTI/NIT package-bound bridges are explicitly not supplied; no drop-in or runtime acceptance claim.
QC-Q1 is resolved for inventory; QC-Q2–Q5 retain implementation, composition and evidence gates.

RPG Inventory's Workshop description was read. It describes a cosmetic Gear-tab presentation with
Def-configured slots, compact empty-space handling, a fallback item list, race-specific simplified
layouts, configurable colors and condition bars, and multi-slot indication. It does not claim to
add inventory actions. It credits a reused color selector from original Quality Colors, which is
a shared-UI research lead, not proof of dependence on the selected continuation. The page links
[public source](https://github.com/catgirlfighter/-1.0-RPG-Style-Inventory-V3.0-); that repository has
not been audited in this intake. The author says it works on 1.7 despite the version list; this is
an author claim, not local acceptance. Its CE compatibility statement explicitly notes limited
testing. Other advertised integrations remain claims pending exact contract inspection.

**RPG-Q1** tracks slots/layouts, native item actions, external Def/provider contracts, persistence
and exact beta behavior. Compose presentation directly with owned Gear cargo controls (S021) and
HUD work (S127/S128); no patches against owned UI. Actual configuration uses the common settings
model unless a documented exception applies. No new bottom tab is implied by a Gear-tab redesign.
The bounded installed inventory found no source, README, license file or LoadFolders for either
mod; RPG's 1.6 folder includes a bundled Harmony assembly, which must not be copied into our package.
No assets were selected, assemblies executed or gameplay compatibility accepted by this intake.

**2026-11-07 S132 follow-up:** [complete research](2026-11-07-rpg-style-inventory-research.md)
and [owned draft](../../../../openspec/changes/specify-megamod-gear-presentation/) supersede the
initial inventory status. Author docs preceded complete installed1.6 main/CE assembly and XML
inspection, current beta Gear and actual Fortified/Smart Medicine consumer paths. The draft keeps
three layouts and current native actions, uses the shared palette/cargo/supply state directly,
and records legacy XML/reflection and optional-provider gaps. CE/RWoM canonical packages were not
found locally; a CombatExtended DLL inside Bill Doors' framework is not provider evidence.
RPG-Q1 inventory resolved; RPG-Q2–Q5 retain admission/composition/provider/native proof.

**2026-11-07 S133 follow-up:** [complete research](2026-11-07-bulk-load-transporters-research.md)
now covers Workshop/public license, all73 installed code files, source defaults/contracts and native
1.7 request/transfer/unloading paths. [Owned draft](../../../../openspec/changes/specify-megamod-bulk-transporter-loading/)
covers pod/shuttle batching and carrier unloading through shared hauling. **The user explicitly
excluded portal loading on2026-11-07.** No portal adapter, bulk job/menu/redirect or patch is selected.
Vehicle support is deferred: no working adapter established by the inspected source. BLT-Q1 is
resolved; BLT-Q2–Q5 retain runtime admission, external contracts, composition and native proof.
The earlier intake below records what was known when selected; it is not the current research state.

On 2026-11-03 the user selected **Bulk Load For Transporters**, now **S133**. Installed About and
PublishedFileId confirm **Ilarion**, package **`Ilarion.BulkLoadForTransporters`**,
[Workshop 3559292908](https://steamcommunity.com/sharedfiles/filedetails/?id=3559292908), advertised
game versions **1.6/1.7**, and no modVersion. Metadata requires Harmony and original
`Mehni.PickUpAndHaul`; its description also claims support for compatible forks. Additional load-after
entries name `teemo.pickupandhaulforked`, `memegoddess.pickupandhaul`,
`mehni.pickupandhaul.unoffical` and `SmashPhil.VehicleFramework`. These hints do not prove working
integration with the newer Continued/Reworked references or any vehicle provider.

- Metadata: `F:/Steam/steamapps/workshop/content/294100/3559292908/About/About.xml`, SHA256
  `F3E6B8470B643C44CEBC4551D496AAA06496BCCD8BDB046309EB4E8525706E83`.
- Assembly: `F:/Steam/steamapps/workshop/content/294100/3559292908/Assemblies/BulkLoadForTransporters.dll`,
  SHA256 `AA46929C3CFE4E27528F6FBF877F160B3E18783F1ED1349E7A4F408BFB4A19E1`.
- The Workshop response was a regional-unavailability error; the initial description comes from
  installed author metadata. The bounded inventory found no C# source, README, license file or
  LoadFolders.xml. Source discovery/read-only decompilation and licensing remain queued.

The author describes multi-colonist bulk loading for transport pods, shuttles, map portals and
vehicles, plus bulk unloading of pack animals. Claimed speed/labor improvements are not measured
results. **BLT-Q1** tracks exact supported workflows, requested quantities, capacity and concurrent
reservations, interruptions/cancellation, unloading destinations, persistence and external bindings.
Compare loading already supplied by the S021 reference forks before defining the combined result.
S133 gets its own megamod-owned change and uses direct owned hauling calls, shared data and the same
aggregate carrying limits. Do not retain patches against our own S021 implementation or add an
upstream PUAH runtime dependency. Actual optional content must be resolved and guarded. No full
source analysis, beta compatibility or gameplay acceptance is claimed by this intake.

**2026-11-07 S134 follow-up:** [complete0.1.22 research](2026-11-07-priority-treatment-research.md)
and [coverage](../replacements/priority-treatment.md) now supersede the intake-only status below.
Public guide/API and all16 compiled files inspected; rescue/tend/operation/dose/quill contract
independently reviewed and strictly validated. Runtime remains unimplemented; older0.1.20 hashes
below remain historical intake evidence.

On 2026-11-03 the user selected **Priority Treatment** and anticipated compatibility changes or
consolidation with **Drug Response (S124)**. The installed exact-title match is **Dreadarm**, package
**`Dreadarm.PriorityTreatment`**, [Workshop 3813351382](https://steamcommunity.com/sharedfiles/filedetails/?id=3813351382),
version **0.1.20**, advertised **1.6/1.7**, requiring Harmony. Track it as **S134**. A second installed
item, **Priority Treatment Ressurected**, `tk421storm.PriorityTreatmentRessurected`, **3009738919**,
is a distinct edition; its metadata was read for disambiguation, not added as another selected source.

- Metadata: `F:/Steam/steamapps/workshop/content/294100/3813351382/About/About.xml`, SHA256
  `0D0DEFF42CA54185C5D7A711B376A0615695C6E1F6C176D47CF49AEC652A0F44`.
- Beta assembly: `F:/Steam/steamapps/workshop/content/294100/3813351382/1.7/Assemblies/Dreadarm.PriorityTreatment.dll`,
  SHA256 `D569DE77E6D771B41CFF523C6F247F6183F31C5A30887BACF703DFCAC76837A5`.
- About, PublishedFileId, LoadFolders and LICENSE were read. Version 1.7 selects `Common` plus
  `1.7`. The license states all rights reserved; independent behavior specification/reimplementation
  remains the plan, with no code, documentation, assets or binaries copied into the product.
- The Workshop response was regional unavailability, and the linked
  [author guide/API](https://rmapi.dreadarm.net/mods/priority-treatment/) was inaccessible through
  the web tool. Initial behavior comes from installed author metadata. No C# source or README was
  found for the selected Dreadarm item in the bounded inventory; full analysis remains queued.

The author describes priority-1 doctors interrupting automatic work/recreation/self-tending for
rescue and treatment, rescue before bedside/self treatment, optional sleep/disease/meal rules,
and restrictions on automatic surgery by injured or scarred doctors. It also describes automatic
drug operations for unconscious or deathresting bed patients with unmet addiction/genetic
dependency, gated by matching reachable drugs and addiction permission when a policy exists;
its stated no-policy behavior also needs explicit review. Odyssey quill-removal operations are
another advertised feature. These are author-described behavior, not accepted source facts or
selected defaults, and exact triggers/thresholds/operation lifetime remain **PT-Q1** research.

Drug Response's installed description also discusses automatic drug-taking tasks and surgery
bills for conditions. This establishes related workflows to inspect, not a demonstrated conflict.
**DR-Q2/PT-Q1** jointly track overlapping pending orders, repeat-dose prevention, policy permissions,
patient conditions, drug availability, doctor priorities, player cancellation/overrides and save
recovery. Record the chosen combined observable result after inspecting both sources. Each source
still gets its own megamod-owned change; overlapping behavior uses direct composition rather than
internal patches or competing schedulers. Consolidate controls with the required Drug Response UI
redesign, distinguishing entity treatment policies from global/save settings. No coexistence defect,
third-party compatibility or beta acceptance is asserted by this intake.

**2026-11-07 S135 follow-up:** [research](2026-11-07-basic-caravan-loading-research.md)
and [coverage](../replacements/basic-caravan-loading.md) now provide the complete XML/native analysis
and reviewed/validated draft. Runtime remains unimplemented; intake below is historical.

On 2026-11-03 the user selected **Basic Caravan Loading**, now **S135**. Installed About and
PublishedFileId confirm **LM**, package **`LM420.BCL`**,
[Workshop 3370434966](https://steamcommunity.com/sharedfiles/filedetails/?id=3370434966), advertised
game versions **1.0–1.7**. About declares no modVersion or dependencies. Its description says
caravan loading moves from hauling into Basic work for higher priority; actual priority still
needs native/category and player-configuration analysis before becoming an observable contract.

- Metadata: `F:/Steam/steamapps/workshop/content/294100/3370434966/About/About.xml`, SHA256
  `0DE108E9807083E43EA5026083CCB078F1F70A9E332198E21287D18667FEFEB4`.
- Complete installed patch read: `F:/Steam/steamapps/workshop/content/294100/3370434966/Patches/WorkGiversPatch.xml`,
  SHA256 `2EE506CD1D979DA1A637F7F8A7C8E6EFF7F08E7EBFB235555EC3CB88991C45E4`.
- The installed inventory contains About metadata, PublishedFileId, a preview and that XML patch;
  no assemblies, C# source, README, license file or LoadFolders were found. No preview was selected
  for reuse. Public Workshop retrieval returned regional unavailability; the author description
  above comes from installed metadata, followed by read-only XML inspection.

The XML contains two unconditional `PatchOperationReplace` operations: it assigns `BasicWorker`
to the `workType` of `HelpGatheringItemsForCaravan` and `UnloadCarriers`. Thus the inspected source
also affects carrier unloading, not just the loading named in the title. No runtime behavior,
effective final Def values or beta compatibility has been verified by reading those operations.

**BCL-Q1** retains the observable specification and exact native worker/category investigation.
Compare the chosen result with **S021 batch hauling** and **S133 Bulk
Load For Transporters**, including eligible workers, player priorities and carrier-unloading
handoff. Do not infer a conflict from shared scope or change unrelated Hauling work. Each selected
source retains its own megamod-owned change; owned composition is direct. Any owned XML mutation
must follow the existing XML Extensions Def/node guards, target specs and startup-warning rules.
Document deliberate replacement of the scalar work category; unrelated fields/list entries remain
preserved. The upstream unconditional replacements do not waive those requirements. Full behavior
specification, licensing review and native acceptance remain pending.

On 2026-11-03 the user selected **Floor Pipette Tool**, now **S136**. Installed About and
PublishedFileId confirm **ferny**, package **`ferny.FloorPipetteTool`**,
[Workshop 3706877822](https://steamcommunity.com/sharedfiles/filedetails/?id=3706877822), advertised
**1.6/1.7**, and required Harmony. About has no modVersion and an empty description; behavior
below comes from the successfully read Workshop description, not inferred metadata.

- Metadata: `F:/Steam/steamapps/workshop/content/294100/3706877822/About/About.xml`, SHA256
  `7B468218F7E199F781E74F7CF6F668698286F5118AAAE0FEB64751926A94A8BC`.
- Installed assembly found: `F:/Steam/steamapps/workshop/content/294100/3706877822/1.6/Assemblies/FloorPipetteTool.dll`,
  SHA256 `E00A7EB6F2F29BB79390927A6841E76BD4755CD67F0BBC9047B3A1B37CACD237`.
- Included source was located under `1.6/Source/` (`Designator_FloorEyedropper.cs` and
  `FloorPipetteToolMod.cs`), but has not been analyzed in this intake. The bounded inventory found
  no README, license file, LoadFolders or separate 1.7 assembly. Advertised beta support does not
  establish the effective load route, source/binary correspondence or live compatibility.

The author describes an Architect tool that samples an existing floor to identify it and select
building more of the same flooring. Workshop credits Taranchuk for C# and Oskar Potocki for art.
No artwork was selected or copied. Exact eligible terrains, modded/natural floors, unavailable
build options, interaction/cancellation and any restrictions remain **FPT-Q1** research; the brief
description does not establish that every terrain can be copied or unlock unavailable construction.
The source gets its own megamod-owned change. Check composition with selected Architect features
(including S036 and S067), retain native construction rules and contextual UI, and follow existing
semantic target checks and asset/license requirements during later implementation. No new bottom
tab, completed source analysis or gameplay acceptance is implied by this intake.

## Corrections found

| Note | Correct installed identity | Error in the first pass |
|---|---|---|
| Just Put It Over There | **Just Put It Over There** — `Mlie.JustPutItOverThere` | Incorrectly left unmatched. Its description concerns placing purchased goods and visitor gifts in storage. |
| Wanderer Joins Plus | **WanderJoinsPlus** — `Ogliss.WanderJoinsPlus` | Missed the title's “Wander” spelling. It is a separate installed mod from RescueeJoinsPlus. |
| Show gender/age in caravan form dialog | **Show gender/age in caravan form dialog** — `arl85.ShowGenderAgeCaravanFormDialog` | Incorrectly called custom despite an exact installed-title match. |

All three were already in the first captured inventory. These were incomplete lookup errors, not
new installations or unreadable handwriting. Their metadata has now been opened directly.

**Just Ignore Me Passing and Carry Food were introduced by the assistant during the voice interview.**
Neither is in the photographed list, and neither had an installed-title match in this scan. They
are not replacements for any transcript entry and are not added to the selected sources. The
written entry Just Put It Over There was never overwritten. There is no evidence identifying a
particular misreading that produced Just Ignore Me Passing; it was an unsupported conversational
addition. The subsequent routine-meal-carrying preference is recorded separately as an interview
answer, not proof that a named Carry Food mod was requested.

## What remains uncertain or lacks a named provider

Seven entries have no selected named provider. For **Hunting dogs?**, the assistant's original
custom classification was an assumption; the user initially asked for the mod's behavior. The
installed candidate **Hunt for Me** was then investigated and explicitly rejected by the user,
who chose a custom hunting companion instead. There is no outstanding identity question for this
entry. No provider is silently selected for any of these sketches.

| Note | Current interpretation | What still needs clarification |
|---|---|---|
| Link Stockpile Zones / Growing Zones | A linking feature request | Direction clarified: vanilla shelf-linking mechanics for stockpiles, including mixed shelf/stockpile groups; linked growing zones synchronize crop selection and sowing/cutting controls. Detailed specification and vanilla checks are deferred. |
| Link Workbenches | A linking feature request | Direction clarified: synchronize the entire bill list, including additions, removals, order and every bill's configuration. Detailed specification is deferred. |
| A recycle and destroy mod that works | Recycling limited to weapons/apparel; item destruction by melee | Only items with HP; pawn repeatedly makes actual melee attacks where the item lies until destroyed. Excludes non-item targets and items without HP. Micro Designations supplies other processing options. Recycling refunds and native attack lifecycle remain for specification; research references do not imply full source parity. |
| Hunting rework? Hunting Spot / Hochsitz | Custom hunting feature with a spot/seat and animal filter | Wait for animals within an initial 40-cell radius, with no line-of-sight check, then leave to hunt. Storage-style filter grouped by predator/non-predator and small game/big game; list species that can appear on the map, including currently absent species. Detailed targeting/return rules remain open. |
| Go there and keep lookout, then shoot | A continuation of the hunting idea | Workflow clarified as wait, spot, then leave to hunt; detailed selection/return rules remain open. |
| Hunting dogs? | Custom hunting companion; Hunt for Me rejected | All qualifying bonded animals with existing **Attack + Haul** training may accompany their hunter using Pathfinding Lab's herd-wandering update. One companion claims each downed prey, finishes it if necessary, and hauls it if destination and carrying capacity permit. No separate Hunting training. |
| Don't generate everything in one tick | Preparation distributed across ticks; complete groups appear together | User confirmed raids/trader groups appear only once the whole group is ready; no partial-group gameplay. Maps/encounters, exact latency, scheduling and lifecycle remain for specification. |

The quantity annotation under Skilled Slaughtering is a modifier, not another unnamed mod. Its
meaning was clarified by the user: skill-dependent yield through the slaughter job, preserving
base values. The cancelled line is excluded; its exact unreadable text need not be reconstructed.
Grazing Area is installed but excluded by the user's correction to **Grazing Lands**.

## Shorthand and layout readings worth keeping visible

These mappings are supported by the photograph and installed metadata. They are explicitly shown
because the note and full title differ; no silent substitution is needed.

| Handwritten/normalized note | Installed title | Basis / limitation |
|---|---|---|
| Chill the fuck out | Chill the Fork Out | Wording differs; installed title is the close match. |
| Everyone gets one | Everybody Gets One - Continued | “Everyone” versus “Everybody.” |
| Best Materials | Use Best Materials | Shortened name; remains separate from Best Mix. |
| Quick Stockpile Generation | Quick Stockpile Creation | “Generation” versus “Creation.” |
| Quality Builder | QualityBuilder Unofficial 1.6 | Spacing and edition suffix differ. |
| Quality base durability | Quality Based Durability | Slight grammatical shortening. |
| Prosthetic no missing Bodypart | Prosthetic No Missing Body Parts (Continued) | Singular/shortened wording. |
| Meals over Preserved Foods | Prioritize Meals over Preserved Foods | Omitted leading word. |
| Add Deconstruct to Floors Category | Add Deconstruct Button To Floor Category | Shortened wording. |
| Wanderer Joins Plus | WanderJoinsPlus | Different word ending and spacing. |
| PUAH | Pick Up And Haul | Acronym; “optimized” is an amendment, not a separate selected package. |
| Automatic Night … | Automatic Night Owl | Fold/edge obscures the ending; metadata supports the completed title. |
| Kanban Stockpile-ish | KanbanStockpile Continued | “-ish” describes the requested adaptation, not the package name. |
| That other “Random Materials” mod → Best Mix | Best Mix (Continued) | The arrow supplies Best Mix; “Random Materials” is not silently added as another title. |
| Most / Skilled / Bill / Restriction | Most Skilled Bill Restriction | Wrapped across the right column from the workbench-management line. |
| Smarter Deconstruction and Mining; Smart Medicine | Two separate installed mods | Interleaved wrapping was checked in the original photograph. |
| Brrr and Phew + Toddlers fix | Brrr and Phew (Continued), plus Toddlers | Clarified 2026-11-02: mobile toddlers should seek safe temperatures themselves. Repeated wandering into bad temperatures and needing rescue is a user-reported symptom; exact source interaction is unverified. |
| More Power Info; Snap Out; Keep Converting | More power info; Snap Out!; Keep Converting | Rechecked against the image; not “Pawn,” “Swap,” or “Constructing.” |

Other differences in the full table are capitalization, punctuation, joined words, author tags or
Continued/version suffixes. None is treated as a verified compatibility contract. In particular,
source identity confidence does not establish that an entire mod is desired unchanged.

## Amendment decisions and remaining source-analysis work

These concern explicit notes. Research source behavior first; do not invent conflicts between
otherwise complementary mods or ask the user to supply existing functionality:

- Quartermaster-inspired Add bill view: placement is clarified, with search, recipe-output category
  pills, craftability highlighting and output-stat sorting. Categories derive from the workbench's
  recipes; sort options derive from the filtered recipes, with common properties first. The
  **Use all materials** checkbox beside the sort dropdown is unchecked by default: compare the best
  compatible material currently available in sufficient quantity; checked includes unavailable
  materials. Carry the compared material into the added bill; right-click is the proposed
  alternative-material action.
  Compare quality-dependent stats at **Normal quality**, with no quality selector. Craftability
  highlighting requires **both enough ingredients and at least one colonist who meets the recipe's
  skill requirements**. Detailed availability checks remain for specification. No separate menu/bottom tab.
- Auto-Facing Chairs: clarified. Manual rotation during placement persists while the cursor stays
  within the same cell; moving to another cell releases the override and permits automatic facing.
- Simple Stockpile Presets: compact selection across storage types, hiding presets with no
  acceptable items (for example, meat in woody-only storage). A single dropdown gizmo versus
  integration into storage configuration remains deliberately undecided. Correctness belongs in
  our independent specs/tests/implementation, without requiring an upstream bug audit from the user.
- Brrr and Phew with Toddlers: toddler idle wandering must proactively avoid dangerous temperatures,
  and mobile toddlers must seek safety if their surroundings become unsafe. Both are user-confirmed.
  The reported source wandering/rescue problem has not been independently reproduced.
- Drug Policy Fix: inspect existing handling of removed/missing drug definitions and policy entries;
  clarify only a custom difference that source analysis cannot resolve.
- Show Buildable Material Count → Ctrl+F: clarified as use of the same index/cache for material
  counts and search. The arrow does not request a click-to-search action.
- Kanban-style stockpiles: refill thresholds use totals per individual item type across all linked
  storages, with no category-wide targets. Two quantities per type/group: start refilling below X, then continue
  up to Y. Y stops automatic refilling only; other deliveries, including crafting output, may exceed
  it. A separate standard slider applies to all stacks in a storage, initially **infinite/undefined**.
  It limits further hauling into each stack, always respecting that item's actual stack maximum;
  it neither supplies per-item values nor overrides physical stack sizes. Existing stacks are not
  resized. X/Y defaults and remaining source controls require analysis/specification, not explanations
  from the user.
- Pick Up And Haul: our hauling must respect pawn carrying capacity, including items held in a
  separate hauling inventory. The suspected upstream capacity bypass is unverified; inspect source
  and target-game capacity rules during specification. Optimization goals for this and Permeable
  Terrain still need refinement.
- Mod Options Sort + Search: the installed description already documents author-aware search of
  the mod-options list. The annotation does not by itself establish an additional source amendment.
  Separately, user confirmed megamod's own search must match setting names and descriptions,
  feature names and category names. Detailed matching/results remain for specification.
- Trading area: dedicated fixed area with its own gizmos, not a pawn allowed area or generic
  allowed-area assignment workflow. Exact footprint editing, movement and lifecycle remain for specification.
- Skilled Slaughtering: user clarifications are recorded in the transcript; remaining detailed
  rules are not silently settled by the source match.

No dominance question is needed for Allow Tool versus Pick Up And Haul without evidence of an actual
conflict. No dining grouping involving Just Ignore Me Passing is supported by the notes.

## References added during the interview

The user named these as UI references while clarifying P1-09. They are installed, with metadata
paths and hashes in the source index under `interview-ui-reference`. They are not additional
handwritten entries or blanket feature-parity commitments; the original-photo counts remain unchanged.

| User wording | Installed identity | Role |
|---|---|---|
| Nice Bill Tab | **Nice Bill Tab** — `Andromeda.NiceBillTab` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3520130671/About/About.xml>)) | Reference for a richer Add bill view. |
| Nice Bill Tab extension | **Nice Bill Tab - Expansion** — `HICON.NiceBillTabExpansion` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3721023311/About/About.xml>)) | Same UI reference; installed metadata declares its dependency on Nice Bill Tab. |

Neither metadata nor this matching step proves behavior or compatibility on the target beta.

### Repository reference added during the interview

The user explicitly selected the **herd-wandering update from Pathfinding Lab**
(`fumblesneeze.pathfindinglab`) as the wandering reference and asked to extend it to pen/room
animals. This is an interview addition, not a new photographed row or selected Workshop identity.
The owned reference files inspected on 2026-11-01 are
[WildlifeWander.cs](../../../PathfindingLab/Source/WildlifeWander.cs) and
[DirectionalWalk.cs](../../../PathfindingLab/Source/Core/DirectionalWalk.cs), with
[source tests](../../../../tests/PathfindingLab.Unit/DirectionalWalkTests.cs) and the
[experiment's specification](../../../../openspec/changes/add-pathfinding-lab/specs/pathfinding-lab-routing/spec.md).

The source implements bounded directional 3–10-step idle walks and currently excludes pen
providers. Requested adaptation: pen/room animals use provisional 2–3-step local walks without
another herd animal as their center, remaining within their enclosure. A shorter walk or no
movement is allowed; a single-cell room must produce normal waiting, never an endless search or
rapid job retry loop. The existing minimum-step/core and minimum-path/adapter constraints both
need adaptation. Hunting companions use the hunter as their nearby reference and require only
the existing Attack + Haul training. Detailed refinement and safety cases are in the transcript.
This selects only the wandering behavior; it adds no runtime dependency on the experiment and
does not accept its broader pathfinder or claim this extension has been implemented/tested.

Inspected source fingerprints (SHA-256; working files may change independently):

- `WildlifeWander.cs`: `110215E90289EAC9F800EC9C6698767C08D10F2D0E7636FEA3A2D3B1310A4B2F`
- `DirectionalWalk.cs`: `E74F0F5DA100EB71DE6AB833C4FA766C82E63400E70AF2D0333DEAD20D713FE5`

## Complete reconciliation

The following table covers every photographed entry exactly once. “Mod reference” includes direct
names, clear abbreviations, shorthand matches, duplicates and sources explicitly used as inspiration.
“Feature sketch” has no selected named provider. “Modifier” belongs to an adjacent identified mod.
The source index distinguishes selected candidates, excluded providers, research-only suggestions,
later interview UI references and rejected identity candidates.

<!-- GENERATED_RECONCILIATION -->

**Coverage:** 117 entries = 107 mod-reference rows, 7 feature sketches, 1 modifier and 2 excluded entries. Source mappings retain 108 distinct installed candidate package IDs. P1-60 is a custom feature; its rejected Hunt for Me candidate is excluded from the selected-candidate count.

| Entry | Photograph reading | Classification | Installed identity / result |
|---|---|---|---|
| P1-01 | CM Color Coded Mood Bar | Mod reference | **CM Color Coded Mood Bar [1.1+]** — `CrashM.ColorCodedMoodBar.11` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2006605356/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P1-02 | Chill the fuck out | Mod reference | **Chill the Fork Out** — `KT.ChillOut` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3523235444/About/About.xml>))<br>Profanity in note; Fork in installed title. |
| P1-03 | Improved Need Indicator | Mod reference | **Improved Need Indicator** — `AmCh.ImprovedNeedIndicator` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3211635205/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P1-04 | Already crossed-out line: `[... shelves? → ... storage piles?]` | Excluded | Cancelled; user says already covered by base game. Exact text remains unreadable. |
| P1-05 | Dress for the Weather | Mod reference | **Dress for the Weather (Continued)** — `Mlie.DressForTheWeather` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3607724833/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P1-06 | Link Stockpile Zones / Growing Zones | Feature sketch | Vanilla shelf-linking semantics confirmed for stockpiles, including mixed shelf/stockpile groups. Linked growing zones synchronize crop selection and sowing/cutting controls. Detailed specification and vanilla checks are deferred. |
| P1-07 | Compact Hediffs | Mod reference | **Compact Hediffs** — `PeteTimesSix.CompactHediffs` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2031734067/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P1-08 | Allow Tool | Mod reference | **Allow Tool** — `UnlimitedHugs.AllowTool` ([metadata](<F:/Steam/steamapps/workshop/content/294100/761421485/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P1-09 | Something like Quartermaster but directly in Bills menu and showing what can and cannot be made | Mod reference | **Quartermaster** — `slim.quartermaster` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3737474260/About/About.xml>))<br>Richer Add bill view: search, category pills based on this workbench's recipe outputs, craftability highlighting requiring enough ingredients and a colonist meeting recipe skill requirements, and output-stat sorting with common properties first and choices relevant to filtered recipes. Compare at Normal quality with no quality selector. Use all materials checkbox beside the sort dropdown: unchecked by default uses the best compatible material available in sufficient quantity; checked includes unavailable materials. Carry the compared material into the added bill, with right-click proposed for a different material. No separate menu/bottom tab. Nice Bill Tab and its Expansion were added as UI references during the interview. |
| P1-10 | Quality Builder | Mod reference | **QualityBuilder Unofficial 1.6** — `hatti.qualitybuilder` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3512466087/About/About.xml>))<br>Unofficial 1.6 is the installed edition title; no target-version acceptance implied. |
| P1-11 | Link Workbenches | Feature sketch | No distinct provider named. Synchronize the entire bill list across linked workbenches: additions, removals, order and every bill's configuration; not individual bill links with other bills remaining local. |
| P1-12 | Bulk smelting | Mod reference | **Bulk Smelting (Continued)** — `zal.bulksmelting` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2889803802/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P1-13 | Designations Tooltip | Mod reference | **Designations Tooltip** — `CheaterEater.DesignationsTooltip` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3743959668/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P1-14 | Better Workbench Management / Everyone gets one / Most Skilled Bill Restriction | Mod reference | **Better Workbench Management** — `falconne.BWM` ([metadata](<F:/Steam/steamapps/workshop/content/294100/935982361/About/About.xml>))<br>**Everybody Gets One - Continued** — `Memegoddess.EverybodyGetsOne` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3530806680/About/About.xml>))<br>**Most Skilled Bill Restriction** — `Matheus.MostSkilledBill` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3757516418/About/About.xml>))<br>Everyone maps to Everybody. Most Skilled Bill Restriction wraps into the right column. |
| P1-15 | Cherry picker scrollbars | Mod reference | **Cherry picker scroll bars** — `lucius.cherryscroll` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3781285156/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P1-16 | PUAH + optimized | Mod reference | **Pick Up And Haul** — `Mehni.PickUpAndHaul` ([metadata](<F:/Steam/steamapps/workshop/content/294100/1279012058/About/About.xml>))<br>Optimization requested. Our hauling must respect pawn carrying capacity, including items held in a separate hauling inventory; no capacity bypass. The user's suspicion about upstream capacity behavior is unverified. |
| P1-17 | A recycle and destroy mod that works | Feature sketch | **Weapons and apparel only for recycling. Destruction only for items with HP**, through repeated actual pawn melee attacks where the item lies until destroyed. No buildings, plants, living pawns or instant deletion of HP-less items. Micro Designations (P2-25) supplies other processing options. Recycle This and Destroy Item remain research references, not blanket feature-parity selections. |
| P1-18 | Common Sense | Mod reference | **Common Sense** — `avilmask.CommonSense` ([metadata](<F:/Steam/steamapps/workshop/content/294100/1561769193/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P1-19 | Prosthetic no missing Bodypart | Mod reference | **Prosthetic No Missing Body Parts (Continued)** — `Mlie.ProstheticNoMissingBodyParts` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2739055353/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P1-20 | Best Materials | Mod reference | **Use Best Materials** — `kathanon.UseBestMaterials` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2901561962/About/About.xml>))<br>Shortened title. Distinct from Best Mix. |
| P1-21 | Just Put it over there | Mod reference | **Just Put It Over There** — `Mlie.JustPutItOverThere` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2856471776/About/About.xml>))<br>Recovered missed match. Not Just Ignore Me Passing. |
| P1-22 | Replace Stuff | Mod reference | **Replace Stuff - Continued** — `Memegoddess.ReplaceStuff` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3526354009/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P1-23 | Drug Policy Fix → also remove not found drugs | Mod reference | **Drug Policy Fix (Continued)** — `Mlie.DrugPolicyFix` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2195390245/About/About.xml>))<br>Source identified; removal of missing-drug entries is an additional requested behavior. |
| P1-24 | Brrr and Phew + Toddlers fix | Mod reference | **Brrr and Phew (Continued)** — `Mlie.BrrandPhew` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2195938471/About/About.xml>))<br>**Toddlers** — `cyanobot.toddlers` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2903359152/About/About.xml>))<br>User confirmed mobile toddlers should seek safe temperatures themselves; repeated wandering into bad temperatures and needing rescue is their reported symptom. Combine the behaviors directly; no verified source defect or code cause claimed. |
| P1-25 | Show buildable Material Count → Ctrl+F | Mod reference | **Show Buildable Material Count** — `BP.ShowBuildableMaterialCount` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3245101540/About/About.xml>))<br>**Ctrl F - Continued** — `Memegoddess.CtrlF` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3529520961/About/About.xml>))<br>The arrow means material counts must use the same index/cache as Ctrl+F. It does not request a click-to-search action. |
| P1-26 | Readable Numbers | Mod reference | **Readable Numbers** — `7h0m4s.RimworldReadableNumbers` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3762441264/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P1-27 | Better Log | Mod reference | **Better Log - Fix your errors** — `bs.betterlog` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2772559481/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P1-28 | Architect Menu Optimizer? | Mod reference | **Architect Menu Optimizer** — `MRK.architectmenuoptimizer` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3697920753/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P1-29 | Blunt Social Fights | Mod reference | **[DN] Blunt Social Fights** — `dani.BluntSocialFighting` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2886174990/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P1-30 | Cherry Picker | Mod reference | **Cherry Picker** — `Owlchemist.CherryPicker` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3521312241/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P1-31 | Trading Spot `(or area?)` | Mod reference | **[PL] Trading Spot** — `tradingpost.programmerlily.com` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3507108484/About/About.xml>))<br>User confirmed a successor using a dedicated fixed trading area with its own gizmos, not a pawn allowed area. |
| P1-32 | Food Poisoning Stack Fix | Mod reference | **Food Poisoning Stack Fix (Continued)** — `Mlie.FoodPoisoningStackFix` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2843483188/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P1-33 | Auto Self Tend | Mod reference | **Auto Self-Tend** — `funstab.AutoSelfTend` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3300580124/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P1-34 | Graze Up | Mod reference | **[Kit] Graze up** — `kittahkhan.grazeup` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2302739121/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P1-35 | Auto Name Babies | Mod reference | **Auto Name Babies** — `cruesoe.autonamebabies` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3782676886/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P1-36 | Designations Tooltips | Mod reference | **Designations Tooltip** — `CheaterEater.DesignationsTooltip` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3743959668/About/About.xml>))<br>Duplicate of P1-13. |
| P1-37 | Consciously Malnourished | Mod reference | **Consciously Malnourished** — `ProfileName.MalCon` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2935404464/About/About.xml>))<br>Sideways text checked against original. |
| P1-38 | Consciousness Patch | Mod reference | **Consciousness Patch** — `Vex.Consciousness` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2913127696/About/About.xml>))<br>Sideways text checked against original. |
| P1-39 | Dress for the Weather? | Mod reference | **Dress for the Weather (Continued)** — `Mlie.DressForTheWeather` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3607724833/About/About.xml>))<br>Duplicate of P1-05, with question mark. |
| P1-40 | Mod Options Sort? | Mod reference | **Mod Options Sort** — `Superniquito.ModOptionsSort` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2910865748/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P1-41 | Grazing Area | Excluded | **Grazing Area** — `gguake.grazingarea` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3769685249/About/About.xml>))<br>Installed, but user corrected intended source to Grazing Lands (P2-16). |
| P1-42 | Auto-Facing Chairs + manual fix | Mod reference | **Auto-Facing Chairs** — `scarf.autofacingchairs` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3783758781/About/About.xml>))<br>Manual rotation during placement persists within the current hovered cell, including small mouse movements. Moving to another cell releases the override and permits automatic facing again. |
| P1-43 | Kanban Stockpile-ish | Mod reference | **KanbanStockpile Continued** — `endag.kanbanstockpile` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3767756434/About/About.xml>))<br>Per-item group totals govern automatic refill below X/up to Y across linked storages; other deliveries may exceed Y. Separately, one standard slider for all stacks in a storage defaults to infinite/undefined and limits only further hauling into each stack. Actual item stack maxima always apply; no per-item slider values or hard stack-size override. Research remaining source behavior directly. |
| P1-44 | Simple Stockpile Presets + fixes! + UI change? | Mod reference | **Simple Stockpile Presets (Continued)** — `Mlie.SimpleStockpilePresets` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2189006791/About/About.xml>))<br>Compact selector preferred over many preset gizmos; dropdown gizmo versus storage-configuration integration remains undecided. Extend to other storage types; hide presets with no items the storage can accept (meat in woody-only storage). Independent specs/tests/implementation address correctness; no upstream bug diagnosis required from the user. |
| P1-45 | Autocast Specialist Commands | Mod reference | **Auto-Cast Specialist Commands** — `Linnun.AutoCastSpecialistCommands` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3459647882/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P1-46 | Lights Out / Turn it on and off | Mod reference | **LightsOut** — `juanlopez2008.LightsOut` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2584269293/About/About.xml>))<br>**Turn It On and Off - RePowered** — `Mlie.TurnOnOffRePowered` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2228219259/About/About.xml>))<br>Both sources matched; no conflict or precedence is inferred. |
| P1-47 | I can't fix that | Mod reference | **I Cant Fix That** — `Mlie.ICantFixThat` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2780971535/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P1-48 | Meals on Wheels | Mod reference | **Meals On Wheels - Continued** — `Memegoddess.MealsOnWheels` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3538082807/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P1-49 | Ctrl+F | Mod reference | **Ctrl F - Continued** — `Memegoddess.CtrlF` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3529520961/About/About.xml>))<br>Ctrl+F already appears in P1-25; also P2-51. |
| P1-50 | Cut it off | Mod reference | **Cut It Off** — `LegendaryMinuteman.CutItOff` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3628405994/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P1-51 | Meals over Preserved Foods | Mod reference | **Prioritize Meals over Preserved Foods** — `seekiworksmod.no10` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2899127245/About/About.xml>))<br>Shortened title omits Prioritize. |
| P1-52 | Smarter Doctors | Mod reference | **SmarterDoctors (Continued)** — `zal.smarterdoctors` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3744377058/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P1-53 | (That other “Random Materials” Mod) → Best Mix | Mod reference | **Best Mix (Continued)** — `Mlie.BestMix` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2195986094/About/About.xml>))<br>Arrow names Best Mix. Random Materials is descriptive, not another selected identity. |
| P1-54 | Smarter Construction | Mod reference | **Smarter Construction** — `dhultgren.smarterconstruction` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2202185773/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P1-55 | Smarter Deconstruction and Mining | Mod reference | **Smarter Deconstruction and Mining (Continued)** — `Mlie.SmarterDeconstructionAndMining` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3261302741/About/About.xml>))<br>Wrapped Mining belongs to this title. |
| P1-56 | Smart Medicine | Mod reference | **Smart Medicine - Continued** — `Memegoddess.SmartMedicine` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3526251680/About/About.xml>))<br>Smart Medicine is a separate interleaved entry. |
| P1-57 | Cut plants before building | Mod reference | **Cut plants before building (Continued)** — `Mlie.CutPlantsBeforeBuilding` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3286376165/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P1-58 | Hunting rework? Hunting Spot / Hochsitz | Feature sketch | Wait at the spot/seat for an animal within an initial 40-cell detection radius, with no line-of-sight check, then leave to hunt. Storage-style animal filter grouped by predator/non-predator and small game/big game; list species that can appear on this map even when absent. Radius is an initial tuning choice. No named provider. |
| P1-59 | hingehen und Ausschau halten, dann erst schießen | Feature sketch | Continuation of P1-58. Wait at the spot/seat, detect an animal by range alone (initially about 40 cells), then leave to hunt. No line-of-sight requirement; detailed targeting/return rules remain open. |
| P1-60 | Hunting dogs? | Feature sketch | User explicitly chose custom behavior and rejected **Hunt for Me** (`aRandomKiwi.HuntForMe`). All qualifying bonded animals with **Attack + Haul** training may accompany their hunter using Pathfinding Lab's herd-wandering update; one companion claims each shot prey when unconscious/dead, finishes it if needed, then hauls if destination and carrying capacity permit. See the separate interview addition for pen/room wandering. |
| P1-61 | Add Deconstruct to Floors Category | Mod reference | **Add Deconstruct Button To Floor Category** — `geodesicdragon.deconstructfloorcategory` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2905837332/About/About.xml>))<br>Shortened title omits Button. |
| P1-62 | All Memories Fade | Mod reference | **All Memories Fade** — `Mlie.AllMemoriesFade` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2800155563/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P1-63 | Auto-Cut Blight | Mod reference | **Auto-Cut Blight - 1.6** — `Defi.AutoCutBlight` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3520167264/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P1-64 | Automatic Night Owl | Mod reference | **Automatic Night Owl** — `ben.automaticnightowl` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2056012179/About/About.xml>))<br>Installed title supports edge-obscured ending; exact hidden letters are not claimed. |
| P1-65 | Better Message Placement | Mod reference | **Better Message Placement (Continued)** — `Mlie.BetterMessagePlacement` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2259516339/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P2-01 | Walls are solid | Mod reference | **Walls are solid** — `Victor.WallsAreSolid` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2896548513/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P2-02 | Use Bedrolls | Mod reference | **Use Bedrolls - Continued** — `Memegoddess.UseBedrolls` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3534169351/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P2-03 | Auto Links | Mod reference | **Auto links** — `automatic.autolinks` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2059389912/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P2-04 | Unlimited Armour | Mod reference | **UnLimitedArmor** — `Paragon.Hanul.UnLimitedArmor` ([metadata](<F:/Steam/steamapps/workshop/content/294100/1695493009/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P2-05 | Linkable Hydroponics? | Mod reference | **Linkable Hydroponics** — `Yakobo.LinkableHydroponics` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3535816519/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P2-06 | Trees don't just die | Mod reference | **Trees Don't Just Die** — `azuraal.treedontjustdie` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2784870434/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P2-07 | Don't generate everything in one Tick (Traders, Raids, Maps/Encounter etc.) | Feature sketch | Distribute preparation across ticks; user confirmed raids and trader groups appear together only when the whole group is ready. Maps/encounters and exact scheduling, determinism and latency remain for specification. No source mod or threading architecture selected. |
| P2-08 | Stop, Drop, and Roll | Mod reference | **Stop, Drop, And Roll! [BAL]** — `balistafreak.StopDropAndRoll` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2362707956/StopDropAndRoll/About/About.xml>))<br>Two installed metadata files share this package ID; count one identity. |
| P2-09 | Stabilize Bleeding | Mod reference | **Stabilize Bleeding** — `Defi.StabilizeBleeding` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3521178066/About/About.xml>))<br>Exact Stabilize Bleeding match; do not substitute the separate Stabilize mod. |
| P2-10 | Spray Transfer | Mod reference | **Spray Transfer** — `Spray.Transfer` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3490194365/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P2-11 | Easy Upgrades | Mod reference | **Easy Upgrades (Continued)** — `Mlie.EasyUpgrades` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2904429048/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P2-12 | Enemies use Apparel Too | Mod reference | **Enemies Use Apparel Too - EUAT** — `Argotis.EnemiesUseApparelToo` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3746069759/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P2-13 | Extra Alerts | Mod reference | **Extra Alerts** — `zylle.ExtraAlerts` ([metadata](<F:/Steam/steamapps/workshop/content/294100/1926923167/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P2-14 | Food tracker | Mod reference | **FoodTracker** — `b0arl0ck.foodtracker` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3795037221/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P2-15 | Go-Juice is Medical | Mod reference | **Go-juice is medical** — `orioM.Gojuicemedical` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2971408015/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P2-16 | Grazing Lands (Not Area) | Mod reference | **Grazing Lands** — `avilmask.GrazingLands` ([metadata](<F:/Steam/steamapps/workshop/content/294100/1770268130/About/About.xml>))<br>User confirmed Grazing Lands and rejected Grazing Area. |
| P2-17 | Growth Speed makes sense now | Mod reference | **Growth Speed Makes Sense Now** — `Jdalt.GSMSN` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2969141187/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P2-18 | Healer Mech Serum Choice | Mod reference | **Healer Mech Serum Choice** — `Syrus.HMSChoice` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2714095848/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P2-19 | Is this a weapon? (→ Mod Config) | Mod reference | **Is This A Weapon?** — `S4.IsThisAWeapon` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3566108804/About/About.xml>))<br>Source identified; mod-configuration placement is explicitly noted. |
| P2-20 | Keep Burning, Corpse | Mod reference | **Keep Burning, Corpse!** — `tixiv.KeepBurningCorpse` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3717835691/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P2-21 | Keep Converting | Mod reference | **Keep Converting** — `Linnun.KeepConverting` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3461478214/About/About.xml>))<br>Converting, not constructing. |
| P2-22 | Labels on Floor | Mod reference | **Labels on Floor (continued)** — `falconne.LabelsOnFloor` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3546003256/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P2-23 | Log after Def Error | Mod reference | **Log After Def Error (Continued)** — `Mlie.LogAfterDefError` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3550944748/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P2-24 | Max Buy, Max Sell | Mod reference | **Max Buy, Max Sell** — `Leoltron.MaxBuyMaxSell` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2889812094/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P2-25 | Micro Designations | Mod reference | **Micro Designations** — `avilmask.MicroDesignations` ([metadata](<F:/Steam/steamapps/workshop/content/294100/1877972690/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P2-26 | Mining Priority | Mod reference | **Mining Priority (Continued)** — `Uuugggg.MiningPriority` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3610712721/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P2-27 | Storage stats in Description | Mod reference | **Storage Stats in Description** — `ferny.StorageStatsinDescription` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3740185419/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P2-28 | Spines Matter More Now | Mod reference | **Spines Matter More Now** — `Jdalt.SMMN` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2971629404/About/About.xml>))<br>Installed title corroborates the cramped Spines wording. |
| P2-29 | Snap Out | Mod reference | **Snap Out!** — `weilbyte.snapout` ([metadata](<F:/Steam/steamapps/workshop/content/294100/1319782555/About/About.xml>))<br>Snap Out, not Swap Out. |
| P2-30 | Mod Options Sort + Search | Mod reference | **Mod Options Sort** — `Superniquito.ModOptionsSort` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2910865748/About/About.xml>))<br>Same source as P1-40; author-aware mod-list search is already documented upstream. User separately confirmed megamod settings search across setting names/descriptions and feature/category names. |
| P2-31 | Monument Highlight | Mod reference | **Monument Highlight** — `espio.monhighlight` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3018715974/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P2-32 | More Power Info | Mod reference | **More power info** — `arl85.MorePowerInfo` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2906529763/About/About.xml>))<br>Power, not pawn. |
| P2-33 | Need Bar Overflow | Mod reference | **Need Bar Overflow** — `AmCh.NeedBarOverflow` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2566316158/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P2-34 | No burn Metal | Mod reference | **No Burn Metal** — `Unon.NoBurnMetal` ([metadata](<F:/Steam/steamapps/workshop/content/294100/1923990111/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P2-35 | Permeable Terrain (optimize?) | Mod reference | **Permeable Terrain** — `Owlchemist.PermeableTerrain` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3261312042/About/About.xml>))<br>Optimization question retained; performance defect not established. |
| P2-36 | Quality base durability | Mod reference | **Quality Based Durability** — `gabrieel1482.QualityBasedDurability` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3798015624/About/About.xml>))<br>Base maps to Based in installed title. |
| P2-37 | Quick Stockpile Generation | Mod reference | **Quick Stockpile Creation** — `longwater.smartstockpilecreation` ([metadata](<F:/Steam/steamapps/workshop/content/294100/1742151109/About/About.xml>))<br>Generation maps to Creation in installed title. |
| P2-38 | Rescuee Joins Plus / Wanderer Joins Plus | Mod reference | **RescueeJoinsPlus** — `Ogliss.RescueeJoinsPlus` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2177170403/About/About.xml>))<br>**WanderJoinsPlus** — `Ogliss.WanderJoinsPlus` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2177016016/About/About.xml>))<br>Recovered WanderJoinsPlus; two distinct sources, no automatic merger. |
| P2-39 | Right-Click Gizmo Indicator | Mod reference | **Right-Click Gizmo Indicator** — `CheaterEater.RightClickGizmoIndicator` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3750529401/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P2-40 | Room Food | Mod reference | **Room Food** — `TheLordDrake.RoomFood` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3530729319/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P2-41 | Scattered Stones | Mod reference | **Scattered Stones 1.7** — `Owlchemist.ScatteredStones` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3528503211/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P2-42 | Scenario searchbars | Mod reference | **Scenario Searchbars** — `nimrag.scenariosearchbar` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3579415129/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P2-43 | Select perishable | Mod reference | **Select Perishable** — `Danoweb.SelectPerishable` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3667956712/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P2-44 | Sensible bed ownership | Mod reference | **Sensible Bed Ownership** — `sensiblebedownership.1trickPwnyta` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3328702448/About/About.xml>))<br>Installed title matches the note, allowing case, punctuation, joined words and edition/author suffixes. |
| P2-45 | Show gender/age in caravan form dialog | Mod reference | **Show gender/age in caravan form dialog** — `arl85.ShowGenderAgeCaravanFormDialog` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2914157264/About/About.xml>))<br>Exact installed-title match; previous custom classification corrected. |
| P2-46 | Skilled slaughtering (+ cooking) | Mod reference | **Skilled Slaughtering** — `MrHydralisk.SkilledSlaughtering` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2873284405/About/About.xml>))<br>Source identified; Cooking and yield changes clarified by user, formula still open. |
| P2-47 | `(+ amount[?])` | Modifier | Clarified quantity modifier of P2-46, not a separate mod. |
| P2-48 | Dubs Break mod | Mod reference | **Dubs Break Mod** — `Dubwise.DubsBreakMod` ([metadata](<F:/Steam/steamapps/workshop/content/294100/1722398508/About/About.xml>))<br>Sideways text: Dubs Break Mod. |
| P2-49 | Toddlers | Mod reference | **Toddlers** — `cyanobot.toddlers` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2903359152/About/About.xml>))<br>Same Toddlers source referenced in P1-24. |
| P2-50 | Research Whatever | Mod reference | **Research Whatever** — `avilmask.ResearchWhatever` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2552092060/About/About.xml>))<br>Sideways title checked against original. |
| P2-51 | TD Find Lib + Ctrl+F | Mod reference | **TD Find Lib - Continued** — `Memegoddess.TDFindLib` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3529443295/About/About.xml>))<br>**Ctrl F - Continued** — `Memegoddess.CtrlF` ([metadata](<F:/Steam/steamapps/workshop/content/294100/3529520961/About/About.xml>))<br>Both sources matched; Ctrl+F repeats earlier entries. |
| P2-52 | De-generalize work | Mod reference | **De-generalize Work** — `Alias.DegeneralizeWork` ([metadata](<F:/Steam/steamapps/workshop/content/294100/2011655761/About/About.xml>))<br>Sideways title checked against original. |


**2026-11-07 S136 follow-up:** [complete Floor Pipette Tool research](2026-11-07-floor-pipette-research.md)
and [coverage](../replacements/floor-pipette.md) supersede intake-only status. Supplied source, compiled
IL, public commit and native1.7 selection/load fallback inspected; reviewed/validated observable draft,
no runtime acceptance. Fog, applied-content policy and sampling/placement separation are explicit.

**2026-11-07 S137 follow-up:** [condition-medical-bills research](2026-11-07-condition-medical-bills-research.md)
and [coverage](../replacements/condition-medical-bills.md) now distinguish the original feature from
its Mint/Compact UI references. Focused native effects and exact targets are drafted; the healthy
leg/infected foot availability discrepancy is recorded with a proposed narrow extension in HBF-Q2.
No whole Mint replacement, automatic treatment, source facade or runtime compatibility is claimed.
