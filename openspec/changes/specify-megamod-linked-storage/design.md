## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), 1.7 beta.
Origin: original. [Research and coverage](../../../mods/megamod/docs/replacements/linked-storage.md)
records the exact native assembly and methods inspected. These are design candidates, not implemented
adapters or compatibility claims.

## Goals / Non-Goals

**Goals:** extend native shelf groups to stockpiles; preserve selection order, complete policies,
unlink copies, same-map lifecycle and native consumers; make physical eligibility member-specific.

**Non-goals:** a separate grouping system per replacement, automatic migration of third-party groups,
unrestricted support for every container, new storage capacity, or implementing Kanban/presets here.

## Decisions

1. **Use native semantics, with one shared authority.** Inspected `Building_Storage` implements
   `IStorageGroupMember`; `Zone_Stockpile` does not. Native `StorageGroup` owns a `StorageSettings`,
   member collection and map. Prefer extending this path through a narrow zone adapter over
   maintaining a parallel cache of copied settings. Whether a native interface proxy can satisfy
   selection, Scribe and hauling consumers without brittle type replacement requires a prototype.
   Do not assume adding Harmony patches can make an existing type implement an interface.
2. **Preserve full policy, not just allowed Defs.** `StorageSettings.CopyFrom` copies priority and
   `ThingFilter.CopyAllowancesFrom`; the latter includes quality, hit points, mental-break chance,
   special filters and other restrictions. Centralize owned group edits and notify the native
   hauling/destination systems. Pasting into an effective group policy must use that same path.
3. **Mixed physical constraints need a deliberate extension.** Native `StorageGroup.ParentStoreSettings`
   reads the first member's fixed settings. Stockpiles allow more than shelves: Core shelf XML
   excludes chunks, plants, buildings, wastepacks when present, and large corpses. A first-member
   fixed filter cannot represent the mixed group correctly. Present the union of intrinsic choices
   but enforce each destination member's fixed filter in actual admission. This is a justified
   extension of native homogeneous groups, not a way to expand shelf capacity or overwrite Defs.
4. **Membership operations preserve native ordering.** `StorageGroupUtility` chooses the first
   eligible selected member, reuses its group or initializes from its policy, and moves only selected
   members. Its unlink path explicitly copies back only to `Building_Storage`; the zone adapter must
   supply equivalent behavior. `Verse.StorageGroupManager.Notify_MemberRemoved` dissolves groups at
   one member. Guard reentrant removal so a final survivor retains its policy exactly once.
5. **Share membership with later consumers.** Native `StorageGroup.HeldThings`, cells and haul sources
   already expose member unions. Inspect and reuse native listers before adding any derived index;
   later Ctrl+F/hauling use the one map-owned shared index. Kanban totals and preset application must
   use this same group identity and policy, not patch owned methods or create another link graph.
   Preserve the native removal notification, but delegate whole-list workbench bill recovery to
   S014's owner: it retains unavailable count/output choices and blocks new starts until explicitly
   resolved. Ordinary unlinked bills retain native fallback. Do not let native validation silently
   clear a linked row's scope before that owner records the unavailable choice.
6. **Entity state uses native surfaces.** Group names, membership and filters are save-owned gameplay
   state. They are not global/per-save mod configuration. Existing storage inspect UI, selection and
   gizmos are the surfaces; no bottom-tab entry is justified.

### Target-spec work before implementation

Document each actual interception separately. Likely seams include zone effective settings, native
selection/link UI, member-specific destination admission, save/load and group removal. Each target
requires minimal structural/data-flow predicates for its consumed semantics, actual incoming IL
validation for transpilers, atomic installation/rollback and one bounded startup warning per skipped
enabled patch. An unrelated Harmony owner or assembly hash change is not failure by itself. Hashes
below identify research only. XML-only additions, if any, use XML Extensions presence/shape guards
and merge-preserving edits; no fixed-filter list replacement is planned.

## Risks / Trade-offs

- **Mixed-group UI and admission disagree** → test chunk/large-corpse ground-only acceptance through
  real hauling, in both first-selection orders, including clipboard and presets later.
- **Reference lifetime changes** → exercise deletion, unlink, construction/minification and map
  transfer. `BillUtility.Notify_ISlotGroupRemoved` validates global bills rather than directly
  deleting their choices; inspect exact `Bill_Production` validation before implementing.
- **Third-party storage classes have extra rules** → ordinary Core shelves and zones are baseline;
  only separately audited canonical-package/shape adapters extend support. No speculative facade.
- **Removal from an existing save loses links** → do not promise safe uninstall until persistence
  representation and recovery are proven. Existing native policies must not be destructively migrated.

## Migration Plan

Initial use leaves existing independent storage unchanged until the player links it. No import of
third-party settings or mutation of normal saves is authorized by this specification work. Any later
migration must specify backup, atomic update, restoration on failure and verified postconditions.

## Open Questions

- **LS-Q1 (research):** exact blueprint/frame/minified-shelf and gravship transfer lifetime, renamed
  singleton group behavior, and native bill destination/count recovery when a group dissolves.
- **LS-Q2 (research):** inspect installed consumers of `IStorageGroupMember`, `StorageGroup`, raw zone
  settings and storage tags; identify supported provider adapters and concrete external contracts.
- **LS-Q3 (design research):** persistence representation and graceful disabled-feature/uninstall
  behavior for saves with mixed groups; no migration/removal guarantee yet.
