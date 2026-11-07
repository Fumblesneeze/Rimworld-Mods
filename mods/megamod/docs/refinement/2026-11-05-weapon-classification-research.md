# S090 — Is This A Weapon? research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: replacement. Inspected2026-11-05, public documentation first, then complete source/runtime
decompilation and targeted native/consumer inspection. No source execution or native acceptance.

## Identity and evidence

| Evidence | Inspected value |
| --- | --- |
| Original | SP4RTAN, Is This A Weapon?, `S4.IsThisAWeapon` / canonical `s4.isthisaweapon`, Workshop3566108804 |
| Installed | `F:/Steam/steamapps/workshop/content/294100/3566108804`; About1.6/1.7, Harmony required/load-after; only1.6/Assemblies/S4.IsThisAWeapon.dll supplied, no LoadFolders |
| DLL | Assembly/file1.0.0.0; SHA256 `D4EA225A4A6E61AF5223ECA89BA503BE5FFC8F898B4839382DA25874A5CC052A` |
| About | SHA256 `1D284EB600874505415897F8F2DFED8383C6FA36F4E05D9A2A133C485DEE69E8` |
| Public source | [ThisIsSP4RTAN/IsThisAWeapon](https://github.com/ThisIsSP4RTAN/IsThisAWeapon), commit `e7ea9aa58b2e29259450f9eebbf00be55eb9c31e`; committed DLL equals installed hash |
| Project/license | net472, Harmony2.4.1 reference; repository LICENSE.txt carries GNU GPL version3. Independent behavior specification, no source/assembly distribution in product |
| Native | 1.7.4998 rev1299; Assembly-CSharp.dll SHA256 `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |
| Local evidence | `artifacts/Refinement/megamod-spec-20261105/is-this-a-weapon/{upstream,binary,native,sidearms}` |

The [Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=3566108804)
describes a per-type weapon toggle and mentions tools, equipment type, an improvised-melee tag and
equippable components. It claims Simple Sidearms compatibility and credits Not a Weapon as inspiration.
The page's discussion index contains an Exosuit Framework incompatibility topic, but no inspected
report establishes a reproduced cause. The user's handwritten P2-19 explicitly requests Mod Config
placement. Both web and metadata are provenance, not proof of1.7 compatibility.

## Complete selected source/binary behavior

All three C# source files and the selected binary's scanner, settings/UI, model, exclusions and six
patch classes were read. No XML gameplay patches, Defs, new content or separate save component.

| Component | Inspected behavior |
| --- | --- |
| ManualIgnores | Case-insensitive12-name set: Gun_Needle, Gun_NeedleLauncher, Gun_ChargeBlasterHeavy, Gun_HellsphereCannon, Gun_InfernoCannon, Gun_MiniShotgun, Gun_MiniFlameblaster, Gun_Spiner, Gun_Scattergun, Gun_Slugthrower, Gun_ThumpCannon, Gun_ToxicNeedle |
| Settings | Public toggles Dictionary<string,bool>, saved under `toggles`, recreates missing dictionary; global only |
| WeaponScan | Static startup owner `s4.IsThisAWeapon`, unconditional PatchAll then once-only scan/seed; Item category, attempted abstract/parentName reflection, name/manual exclusions, required nonempty tools; seeds every candidate true and sorts label ignoring case |
| Inclusion explanations | Tools mandatory. EquipmentType!=None plus verbs, exact ImprovisedMelee tag and assignable CompEquippable add reasons only; they do not independently qualify a candidate |
| Exclusions | Identifier substring turret/artillery/weapon_grenade ignoring case; attempts same checks up a reflected parentName chain capped32. Missing reflection fields just disable those branches; XML template ancestry is not established by this code |
| Main UI | Search trimmed case-insensitive substring on defName/label; ranged hidden by default; changed-only false; columns identifier/label+icon, reasons, Consider weapon; scroll/wrapped row heights |
| Ranged filter | Reads unmodified Verbs.Ranged, with IsMeleeAttack fallback on exception; does not use patched IsRangedWeapon |
| Bulk/reset | Enable/Disable all currently filtered candidates; Reset all candidates true regardless of filter; settings written every DoSettingsWindowContents call; no explicit save scope |
| IsWeapon postfix | Replaces result with configured bool, including true; does not recheck candidate membership |
| IsMeleeWeapon / IsRangedWeapon postfixes | Force false only for disabled stored keys; suppress exceptions silently |
| EquipmentUtility patches | Discover CanEquip overloads by signature pattern; on native success, disabled key forces false; out-reason overload sets reason null; existing failures stay unchanged |
| Equip provider prefix | Disabled key returns null/skips normal FloatMenuOptionProvider_Equip.GetSingleOptionFor, removing even a Cannot equip row |

The source's actual hard tools gate differs from its public description. It can include apparel
with tools and force IsWeapon=true despite native apparel exclusion; toggling it false can deny the
shared CanEquip path used by apparel. These are static deductions, not an accepted exosuit diagnosis.
Its one-time scan stores translated reason strings, settings are written during rendering, and saved
keys can still affect types no longer in the catalogue. No native lister refresh or final queued-job
recheck exists in the inspected feature. Public classes/fields include WeaponScan.Candidates,
ResetToDefaults, Candidate and Settings/toggles; a bounded installed text scan found no executable
source-API consumer (only the mod's About identity).

## Native1.7 contracts and discovered gaps

Retained `Verse.ThingDef`, `RimWorld.EquipmentUtility`, `FloatMenuOptionProvider_Equip`,
`Verse.AI.JobDriver_Equip`, `Verse.ThingListGroupHelper` and `Verse.ListerThings` decompilation:

- IsWeapon means Item with verbs or tools, excluding apparel. IsRangedWeapon requires IsWeapon and
  any non-melee verb; IsMeleeWeapon requires IsWeapon and not ranged. IsWeaponUsingProjectiles also
  reads IsWeapon. For eligible non-apparel tools candidates, native baseline is already true.
- EquipmentUtility.CanEquip checks biocoding, bonds, ideology roles and apparel developmental stage.
  The native Equip menu separately checks equippable component, violence/shooting, reachability,
  manipulation, fire and quest constraints. Positive classification does not create equipment ability.
- JobDriver_Equip's final normal or outfit-stand toil removes/splits/despawns the new item and calls
  MakeRoomFor/AddEquipment without repeating CanEquip. A setting changed after queuing therefore
  needs a new pre-transfer check; a postfix after mutation is too late to preserve prior equipment.
- ThingListGroupHelper's Weapon membership reads IsWeapon. ListerThings caches membership on Add;
  Remove tests current membership before removing. Flipping classification alone can retain stale
  entries after removal or omit newly enabled existing items. This is a concrete native consistency
  obligation, not a speculative performance improvement. Current-generation refresh and failure
  restoration must be part of admission before promising immediate settings.

## Simple Sidearms is a real native-contract consumer

Installed `petetimessix.simplesidearms`, Workshop927155256, About1.0–1.6; LoadFolders through1.6.
Selected research input `v1.6/Assemblies/SimpleSidearms.dll` SHA256
`703FDA907021F9D76CF6DF6941E73B519A7D0A28B022D67ACAFAED44A40A1F40`.
This is a native-contract inspection input, not a verified1.7 load selection or replacement source.

- FloatMenuOptionProvider_EquipSidearm uses native ranged classification and CanEquip, plus its own
  carrying/role/tool rules. Source disabled settings reach this pickup path indirectly.
- Extensions.GetCarriedWeapons filters inventory by IsRangedWeapon/IsMeleeWeapon, but its existing
  primary/offhand handling differs. Gizmos, memory choices and weapon selection consume those getters.
- WeaponAssingment.equipSpecificWeapon swaps inventory/primary/offhand directly. It uses its own
  canUseSidearmInstance (biocoding/role), not native CanEquip, and AllowBlockedWeaponUse can bypass
  that check. Some offhand mutations precede that check. Source menu suppression alone does not
  prove direct-switch exclusion. A supported owned adapter must veto before any equipment mutation,
  including direct/offhand callers, preserving null/unarmed operations and existing memories.
- Pawn_InventoryTracker_FirstUnloadableThing returns early when the proposed unload item's IsWeapon
  is false and uses IsWeapon again when protecting remembered items. Disabling a type can therefore
  remove protection from ordinary unloading; direct-switch checks alone cannot prove retained
  inventory. The owned admitted adapter must preserve that existing retention policy while keeping
  explicit drop/forget possible and new equip forbidden. This consumer was added during review.

No source-specific API facade is justified by these consumers. Preserve native contracts and add
package/shape-guarded adapters only where actual transfer semantics demand them. Other equipment
providers or source forks remain unverified; exact current Sidearms/beta behavior is WCL-Q3.

## Owned decisions and intentional deviations

- Preserve nonempty melee-tool gate and explicit source identifier exclusions. Do not infer unknown
  parent-family exclusions. Add apparel exclusion and validate stored entries against current
  admitted catalogue/provider identity before applying them.
- True permits compatible native classification; false vetoes. This removes source positive forcing
  of incoming false and prevents an independent provider's rejection being silently overruled.
- Add the required common globals/save overrides, dormant inheritance and main-menu guards. Place
  everything in Equipment/Weapon classification settings; type-wide policy is not entity state.
- Clarify bulk current-save actions as explicit inheritance overrides, snapshot filtered identities,
  and label full-scope resets. Scope-relative changed filter replaces the source's global-only one.
- Publish stored settings, effective policy and current native/shared membership coherently; retain
  previous stored values/inheritance and effective state on a failed edit, display the restored
  values with an error and never replay rejected changes on reload. Review corrected this outcome
  from design-only wording into an observable specification scenario. No fake respawn, lost items,
  per-frame scans or independent inventory cache.
- Recheck queued/native/provider equip before physical mutation, preserve already-held items and
  normal non-combat use. This is prospective classification, not a command to disarm everyone or
  cancel existing attacks. No item stats/categories, market/trade generation definitions are rewritten.
- Seven-language plain reasons instead of XML/member text; rendering is read-only. Search/filter
  state stays transient. Source co-install suppresses overlap; no automatic source settings import.

## Static playability/balance pass

| Core comparator | Inspected provenance | Contract consequence |
| --- | --- | --- |
| Wood | Items_Resource_Stuff.xml, ResourceVerbBase, resource/fuel/building use; market1.2 | Disable weapon status without losing construction/fuel/material uses or changing item value |
| Beer | Alcohol_Beer.xml, Primary/CompEquippable, bottle/neck tools power9 and cooldown2 | Consumable can also be an improvised weapon; no ingestion or attack-stat rewrite |
| Knife | MeleeNeolithic.xml, BaseMeleeWeapon_Sharp_Quality | Deliberately disabling a normal melee weapon is reversible; do not silently keep a minimum weapon set |
| Autopistol | RangedIndustrial.xml, BaseHumanMakeableGun | Test finalized inherited melee strikes and ranged filter; no invented shooting capability or damage bonus |

These are static XML/code anchors; inherited/patched content and exact effects need finalized Defs
and player actions. No numerical balance is changed. Disable-all can intentionally exclude every
editable weapon, so scope/count and reversible resets matter. Existing equipment is not stripped,
native/provider restrictions remain, and optional direct switches require genuine pre-mutation proof.

## Questions and remaining work

| ID | Remaining obligation |
| --- | --- |
| WCL-Q1 | Finalized candidate/apparel/exclusion content, identity ambiguity and complete common row/filter/bulk/reset semantics. |
| WCL-Q2 | Effective getters/actions and native aggregate consumers, coherent map/region refresh, queued/outfit-stand transfer, failure restoration and save-load ordering. |
| WCL-Q3 | Exact current Sidearms binary/load selection, direct/offhand callers and memories/options; unsupported providers explicitly bounded. |
| WCL-Q4 | Focused honest TDD, reviewed package, seven-language/settings lifecycle, real equip/search/movement/save/provider acceptance. |
| WCL-Q5 | Finalized resource/consumable/weapon comparators, preserved normal uses/restrictions and no item loss or forced disarm. |

[Proposal](../../../../openspec/changes/specify-megamod-weapon-classification/proposal.md),
[design](../../../../openspec/changes/specify-megamod-weapon-classification/design.md),
[spec](../../../../openspec/changes/specify-megamod-weapon-classification/specs/megamod-weapon-classification/spec.md),
[tasks](../../../../openspec/changes/specify-megamod-weapon-classification/tasks.md),
[coverage](../replacements/weapon-classification.md). All implementation/acceptance tasks open;
independent review and strict validation are recorded separately in the progress ledger.
