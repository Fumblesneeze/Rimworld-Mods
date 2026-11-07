## Context

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S090/P2-19. [Research](../../../mods/megamod/docs/refinement/2026-11-05-weapon-classification-research.md)
records installed/source identity, native1.7 getters/equip/lister behavior and selected1.6 Simple
Sidearms binary. Documentation only; no current beta compatibility claim from About versions.

## Goals / Non-Goals

**Goals:** coherent type-level classification and prospective equip exclusion, searchable common
global/save settings, no item loss, current native/shared lists, honest provider coverage.

**Non-goals:** make arbitrary things equippable, per-pawn restrictions, disarm existing equipment,
rewrite apparel/attack/category definitions, replace Simple Sidearms, import its preferences, or
maintain a second Thing index. No new numerical combat balance or optional UI framework.

## Decisions

### Separate source facts from intended corrections

Source has one global defName->bool dictionary, startup Candidates scan, six patch classes and a
settings window. Contrary to the public OR-like description, tools.Count>0 is the hard candidate gate;
equipmentType+verbs, ImprovisedMelee and assignable CompEquippable only add displayed reasons.
Retain that gate and explicit name/manual exclusions. Parent-name/abstract reflection in the source
is not proof that XML templates exist as live Defs; inspect actual finalized metadata before relying
on it. Do not invent a wider inherited-family exclusion from unavailable XML ancestry.

The owned catalogue adds IsApparel exclusion: current native IsWeapon deliberately excludes apparel,
whereas source's default true can promote apparel with tools and its false value blocks the shared
EquipmentUtility.CanEquip used by clothing. Preserve wearable status, including modded armor.
This is an intentional safety correction, not verified reproduction of the Workshop exosuit report.

Candidate default true permits native classification. False vetoes it. For true return the compatible
incoming result instead of forcing every incoming false to true. All ordinary non-apparel Item
candidates with tools are already native weapons; no positive promotion is needed. This differs from
source's unconditional true overwrite and preserves independent provider policy. IsMeleeWeapon and
IsRangedWeapon depend on IsWeapon in current Core; patch only the proven seams required for coherent
effective behavior, adding separate guards where a supported foreign implementation bypasses it.
Do not duplicate postfixes solely because the source has them.

### Common configuration, stable catalogue and explicit batch semantics

Register per-type boolean definitions under Equipment/Weapon classification through the common
load/store/resolve/render path. Key by defining package identity plus exact Def identity, not label;
verify any ambiguous provider attribution before applying stored values. Built-in true, save inherits
by default. Show Use global, Current save and Global default on each row; use current effective save
context, no static copy of the last save or source file. Checking inheritance retains dormant data;
unchecking restores it or seeds from current global. Validate callbacks as well as disabled UI.

Use a scope selector for bulk actions and Show only changed. Global baseline=true; save baseline is
current global, so an explicit override equal to global is not changed for this filter. Bulk sets use
an immutable snapshot of matching identities at click time. Current-save bulk override is explicitly
labeled to uncheck Use global; it is an inheritance-changing action, not an enabled hidden editor.
Full global reset affects all loaded candidate globals; full save reset sets Use global for all loaded
candidates but preserves dormant overrides. Missing/unloaded entries are retained without binding
them by similar label to another provider. No automatic import of source `toggles` storage.

Search/filter/scroll are transient presentation state, not gameplay settings. Source filters raw
defName/label, defaults ranged=false/changed=false, and sorts labels ignoring case. Use translated
labels, identifiers for disambiguation/search and provider names; render reasons in player terms
instead of raw XML/member names. Determine ranged capabilities from unmodified attack definitions,
never the policy-patched IsRangedWeapon, or disabling a gun would change its own filter membership.
Restore GUI state, wrap long text and keep all controls accessible in seven languages. No new tab,
gizmo, cheat action or operational toggle. Type-wide rule is mod configuration, not entity state.

### Publish a classification change with its native/shared data refresh

ThingListGroupHelper.Includes(Weapon) reads def.IsWeapon. ListerThings.Add records group membership,
while Remove checks current classification before removing from that group. Merely flipping the
getter can leave old weapon entries behind or omit newly enabled existing things. Admission must
include coherent map/region weapon membership and native query/version notifications, not only
getter patch existence. Inspect all relevant native aggregate consumers before implementation.

Apply setting batches at a safe game-thread boundary, reconcile affected membership from existing
native listers/Def groups, and publish one new effective policy generation with the corresponding
membership. Do not despawn/respawn things, issue fake gameplay callbacks, clear unrelated lists or
scan every map each UI frame. A failed transition restores the complete prior stored values and
inheritance flags as well as effective policy/list state; show those prior values and the failure,
and never serialize rejected edits. Stage settings persistence with the transition rather than
write the new dictionary before refresh. This includes individual, bulk, global, inheritance and
reset operations. Use one shared
derived-fact invalidation for Ctrl+F/hauling/equipment consumers, including held/source things, with
native action-time rechecks. No separate per-source inventory cache.

Save selection must precede construction of its native map indexes; global changes, save override,
load/switch, provider changes and teardown must never expose a prior game's resolved policy. Native
generation/filter consumers may cache Def classifications too: WCL-Q2 must trace and either refresh
supported state or withhold the dependent behavior. Do not promise all mods' private caches update.

### Guard the action before moving equipment

Native FloatMenuOptionProvider_Equip.GetSingleOptionFor checks CompEquippable, violence/shooting,
reach, manipulation, burning, quest/bond and CanEquip before queuing an Equip job. Disabled candidates
hide this Equip option. CanEquip denial adapters are scoped to admitted candidates and preserve an
existing false/reason; a new denial gets a translated classification reason rather than source null.
True never grants manipulation, a CompEquippable, a weapon verb, violent work or a removed permission.

Native JobDriver_Equip does not re-run CanEquip at final transfer. Its normal and outfit-stand paths
split/despawn/remove an item before MakeRoomFor/AddEquipment. An owned late policy recheck must occur
before all those mutations, not merely postfix AddEquipment after the previous weapon has dropped.
Hold no stale allowed bit from menu time; no forced removal of already-equipped items. This policy
is prospective, not an in-progress attack canceller or direct generation/raid disarm operation.

For exact supported `petetimessix.simplesidearms`, menu pickup uses EquipmentUtility.CanEquip, but
WeaponAssingment.equipSpecificWeapon and its offhand path perform their own inventory/weapon swaps.
Its canUseSidearmInstance checks biocoding and roles, not the native CanEquip function, and the
AllowBlockedWeaponUse option can bypass that call. Guard disabled types before offhand/primary
mutations regardless of that option; leave null/unarmed switches and unrelated valid behavior alone.
Inspect all direct callers and rollback/reservation semantics before declaring this adapter supported.
Its retained memories remain intact; do not rewrite its tool preferences, erase remembered weapons,
or delete/drop inventory on classification changes. Exact1.7 provider resolution remains WCL-Q3.

Also preserve its existing remembered-item unload protection: the inspected
Pawn_InventoryTracker_FirstUnloadableThing postfix exits when the result's IsWeapon becomes false,
and its scan also checks IsWeapon. A direct-switch guard alone does not preserve carried inventory.
The admitted adapter must distinguish prospective weapon use from this established retention
policy, honoring existing memories and explicit forget/drop without making the item equippable again.
Do not create a second inventory cache to achieve this. Exact native/provider unload tracing remains
required before claiming retained-item behavior.

### Semantic admission and coexistence

Create stable patch-target documents and executable predicates for actual getters, native menu,
CanEquip result path, pre-transfer action and data refresh, plus each optional provider seam. A name
or overload pattern alone does not prove argument meaning or transfer ordering. Validate effective
classification dependencies, pawn/item/reason/result shape, pre-mutation ordering and lister group
semantics. Do not gate on exact assembly hash/version or absence of Harmony owners. Compatible
foreign changes and irrelevant additions remain admitted. Any actual transpiler validates its
incoming stream every invocation and returns it unchanged on mismatch; no speculative replay of
foreign transpilers. Account for skip-original prefixes and supported later repatching.

Core classification, equip and refresh form an atomic admitted group; optional Sidearms enforcement
is separate and reports its exact unsupported paths if absent/changed. Package ID and shape checks
are mandatory for adapters. Install idempotently, rollback only newly added owned hooks on failure,
and log one bounded startup warning per skipped enabled patch with package/patch/spec/target,
reason, lost behavior and fallback. Stored settings survive but display inactive support honestly.
Active `s4.isthisaweapon` suppresses owned overlap; no package spoof, unpatching others or importing
source public static fields. Bounded installed text search found no source-API consumer; native
classification/CanEquip are the actual shared contracts found in Simple Sidearms.

Harmony becomes required on first use, Core required, DLC/provider mods optional. No XML currently
needed and no runtime Def field rewrite; future declarative change must use XML Extensions guards,
package/content/cardinality checks and merge-preserving operations with required dependency. No
foreign/private Harmony/Gateway assemblies or other repository-owned runtime mod dependency.

## Risks / Trade-offs

- Dynamic classification affects cached native lists -> gate runtime enablement on coherent refresh
  and removal; WCL-Q2 requires real move/despawn/re-enable workflows, not only getter tests.
- Public compatibility says less than every transfer path -> inspect/directly guard optional swaps
  before mutation, and show unsupported coverage until exact provider acceptance exists.
- Source default can turn apparel into weapons -> explicit apparel exclusion; verify ordinary
  wearing with weapon-like apparel and retain provider restrictions when enabled.
- Disable all can intentionally remove all selectable weapons -> clear affected counts/scope and
  reversible reset, no silent minimum-weapon exception or enemy disarm.

## Migration Plan

No original global-settings import or save/weapon-memory conversion. New rows default on and new
saves inherit current globals. Retain unknown provider entries dormant. Current native item and
equipment identities are unchanged. Resolve own settings before save indexes build, clear only
owned transient data on teardown, and never edit a normal user save during verification.

## Open Questions

WCL-Q1–5 cover finalized candidate/exclusion and configuration behavior, native aggregate/late-action
admission, exact optional/provider consumers, implementation/localization/native acceptance, and
balance/playability with ordinary consumable/resource/weapon comparators. No user interview needed.
