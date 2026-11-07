# Grazing Lands

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S087/P2-16; `avilmask.grazinglands`, avilmask / Workshop avil,
[Workshop1770268130](https://steamcommunity.com/sharedfiles/filedetails/?id=1770268130).
Selected installed1.6 DLL and matching upstream source are pinned in
[research](../refinement/2026-11-05-grazing-lands-research.md).

[Proposal](../../../../openspec/changes/specify-megamod-grazing-lands/proposal.md),
[observable spec](../../../../openspec/changes/specify-megamod-grazing-lands/specs/megamod-grazing-plant-damage/spec.md),
[design](../../../../openspec/changes/specify-megamod-grazing-lands/design.md),
[tasks](../../../../openspec/changes/specify-megamod-grazing-lands/tasks.md).

**Implemented: nothing.** The draft combines this source directly with [Graze up](graze-up.md).

| Source behavior | Owned draft and deviations | Missing |
| --- | --- | --- |
| Retained grass / hunger-sized bites | S043 growth consumption, exact hunger cap and no growth restoration from an invalid bite | Runtime code, target admission, tests and native acceptance |
| Edible-yield nutrition / crop damage | Yield-aware growth plants; cultivated food crops consume current health, scaled by growth; source integer health discontinuity and food ceiling corrected | Finalized balance and damage/provider proof |
| Grass depletion chance | One exact5% default roll at actual depletion, no bonus or repeated idle rolls | Native outcomes/probability validation |
| Three global sliders | One shared nutrition scalar plus pasture loss/crop cost, common global/save/Use global scope, honest fresh defaults and seven languages | UI/persistence/native scope verification |
| Native plant persistence | Keep native growth, health and age; no additional food pool or copied source save state | Exact round trips and supported provider/save cases |

Proposed combined defaults differ from source-only Graze up and Grazing Lands: edible-yield
nutrition combines with the regrow bonus and200% consumption cost; crop nutrition also scales by
current growth/health. Positive crop damage rounds upward to whole health, so tiny bites can waste
more crop. These choices and ordinary grazing-then-harvest consequences remain GL-Q1 balance work.
Source rounding overfeed, below-floor growth restoration,5/99 probability, fresh0%/100% defaults and
unconditional patch installation are not adopted. No new content, bottom tab, per-source cache or
internal patches against our own grazing implementation.

Core/Harmony required when implemented. Original source mods/DLC optional; an original overlapping
provider suppresses the owned grazing group whenever the original is loaded. Minimal
effective target contracts, atomic own-only rollback and bounded warnings apply to every actual
patch. XML is not proposed; any later XML requires XML Extensions at first use with guarded merges.

No source/API/package impersonation or settings import. Installed text search found identity metadata,
not a proven executable public-API consumer; compiled-only consumers remain a research boundary.
Mid-save source removal/swapping, all modded plants and exact pen estimates are unverified. GL-Q2–4
retain semantic guard/provider, lifecycle/data, implementation/localization and native workflow work.
All runtime tasks remain unchecked; no foreign assemblies, assets or Gateway dependency are shipped.
