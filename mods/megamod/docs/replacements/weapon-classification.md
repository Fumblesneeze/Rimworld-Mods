# Is This A Weapon?

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: replacement. S090/P2-19. **Specified draft; nothing implemented or live-verified.**

## Identity and provenance

SP4RTAN, `s4.isthisaweapon`, [Workshop3566108804](https://steamcommunity.com/sharedfiles/filedetails/?id=3566108804).
About1.6/1.7, selected1.6 DLL1.0.0.0. Full source/binary, GPL3 provenance, hashes/revision and targeted
native/Sidearms evidence: [research](../refinement/2026-11-05-weapon-classification-research.md).
[Change](../../../../openspec/changes/specify-megamod-weapon-classification/proposal.md),
[design](../../../../openspec/changes/specify-megamod-weapon-classification/design.md),
[spec](../../../../openspec/changes/specify-megamod-weapon-classification/specs/megamod-weapon-classification/spec.md),
[tasks](../../../../openspec/changes/specify-megamod-weapon-classification/tasks.md).

## Behavior inventory

| ID | Action -> result | Defaults / edge cases / evidence | Owning requirement / state |
| --- | --- | --- | --- |
| WCL-01 | Open catalogue -> listed melee-tool item types with clear identities/reasons | Source hard tools gate; explicit name/manual exclusions; apparel excluded as correction | Offer a bounded and understandable item catalogue / specified |
| WCL-02 | Edit type setting -> current global or save policy | Built-in on; common Use global, dormant override, main-menu guards; no per-pawn state | Configure global defaults and save overrides in mod settings / specified |
| WCL-03 | Search/filter/bulk/reset -> explicit bounded settings changes | Ranged/changed initially off; filter snapshot; full resets distinctly labeled | Search and bulk changes have explicit scope / specified |
| WCL-04 | Disable type -> no supported weapon classification/new equip | Existing physical items retained; true restores native restrictions; queued action rechecked | Exclusion governs new weapon use without removing items / specified |
| WCL-05 | Reclassify existing map items -> current search/list/action behavior | Native lister Add/Remove evidence demands coherent refresh; one shared derived dataset | Exclusion governs new weapon use without removing items / specified |
| WCL-06 | Admitted Sidearms pickup/switch -> same prospective exclusion | Actual native getter/CanEquip consumers; direct swaps need a narrow pre-mutation adapter | Integrate supported equipment providers directly / specified boundary |
| WCL-07 | Unsupported target/original provider -> bounded honest fallback | Preserve stored intent and foreign owners; no partial Core group | Admit coherent policy and preserve configuration intent / specified |

## UI audit and configuration

Source already uses mod settings, matching the user's Mod Config note. Owned destination:
**Equipment → Weapon classification**. No bottom tab, bottom-right button, entity gizmo or debug tool.
Rows show name/icon/source/reasons and shared Global default / Current save / Use global controls.
Save edits disabled while inherited; all save operations disabled at main menu. Per-type policy is
configuration; search/filter/scroll are transient UI state. Bulk/reset explicitly identify scope and
affected rows. New strings in seven languages; source raw XML/member reasons are not copied into UI.

## Interactions and consumed contracts

| Consumer | Evidence / contract | Disposition |
| --- | --- | --- |
| Native equip/UI and listers | IsWeapon/IsMeleeWeapon/IsRangedWeapon, CanEquip; cached Weapon group; final transfer bypasses menu eligibility | Preserve coherent classification, existing denial reasons and current membership; recheck before transfer |
| Simple Sidearms | Selected1.6 menu calls CanEquip; inventory/gizmo/memory and unload protection use classification; direct swaps use own checks | Optional exact adapter before direct/offhand mutation and preservation of existing unload protection; no blanket1.7 support claim or preference/memory rewrite |
| Owned Ctrl+F/hauling/equipment policies | Shared current Thing facts and native action eligibility | One common classification/invalidation, direct composition; no self-patch stack or duplicated scan |
| Public source Candidate/WeaponScan/Settings | Bounded installed text search found no executable caller | No fabricated source facade or package spoof |

Source global `toggles` is not imported automatically. Own missing-provider entries remain dormant
under stable identity. Existing items/equipment and source memories are not migrated or deleted.

## Implementation, deviations and omissions

Nothing implemented. Intentional corrections: protect apparel; true passes through ordinary
classification rather than forcing false providers true; use common save inheritance; scope bulk
actions/resets; restore stored/effective state after a failed edit with visible feedback; revalidate
queued/direct equip before mutation; preserve remembered-item unload protection; refresh native/shared membership;
no per-render settings writes or private inventory cache. Source positive-forcing or bare defName
API consumers are not claimed compatible. Optional provider direct paths remain off until admitted.

Preserve source's actual melee-tools gate rather than silently expanding its marketing description.
No new equippable capability, source-source patch stack, combat values, forced disarm, item deletion,
XML/category rewrite or broad promise about arbitrary external caches. Required Core/Harmony on
implementation; no DLC required, providers optional; XML Extensions required if later used. No
foreign DLL/Gateway shipment or dependency on another repository mod.

## Verification and acceptance

See [ledger](../refinement/specification-progress.md) for document validation/review. WCL-Q1–5 keep
finalized catalogue, effective admission, native lister/action lifecycle, exact Sidearms support,
localization and reviewed-build causal acceptance open. Native settings -> menu/search -> equip
and physical item movement must be observed, including global/save/main-menu, already-held items,
queued/outfit-stand jobs, apparel, absent/changed providers and direct sidearm swaps. Static code or
getter tests do not prove gameplay, item retention or cache correctness. Balance pass retains
ordinary material/consumable uses and restrictions without changing stats or automatically disarming.
