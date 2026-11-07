# S129 — Interaction Spot On X research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin replacement. Documentation/offline analysis on2026-11-06; no game, source-mod changes or
runtime acceptance. [Coverage](../replacements/interaction-spot-on-x.md) and
[change](../../../../openspec/changes/specify-megamod-interaction-spot-placement/) retain the selected contract.

## Documentation and exact identity

The user's exact [Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3827781564)
was read before code. The web opener failed, but direct author-page retrieval succeeded and its
87,372-character HTML was retained. The author describes planning buildings without first moving
chunks at their interaction spots; trees are additionally allowed if enabled in settings. No claim
of automatic clearing or changed pawn use appears in that description.

The page links [Dragonissa/Interaction-Spot-On-X](https://github.com/Dragonissa/Interaction-Spot-On-X),
pinned [commit73f1f6ccc1d83a6f3951c5944033347b8cb75ad9](https://github.com/Dragonissa/Interaction-Spot-On-X/tree/73f1f6ccc1d83a6f3951c5944033347b8cb75ad9).
Read all three implementation files. The16-entry tree has no README/license; API license unset.
Its historical project/assembly names include InteractionSpotOnChunk; installed identity is
InteractionSpotOnX. No source/assets/DLL are copied into the product; independent engineering only.

Installed root `F:/Steam/steamapps/workshop/content/294100/3827781564`: author **Dragonissa**, package
**dragonissa.InteractionSpotOnX**, support1.6/1.7, required Harmony. No LoadFolders, modVersion,
installed C# source, README or license found in bounded inventory. Root assembly is selected.

| Input | Bytes | SHA256 |
| --- | ---: | --- |
| About/About.xml | 802 | `45b83b16c7db505b186fc7cff3aa1ebaa8d640162bc15e9ef7190bc3efe35da2` |
| Assemblies/InteractionSpotOnX.dll | 7680 | `8848b5add25c04a34e2d1337fd5da0755e6a82d4945db179ba557249d62fe144` |
| Selected game Assembly-CSharp.dll | 15636480 | `d55aa8e47fa3c5463ca85b0eccd51f195b4bbcd04f56da3af21c2f6688a81254` |

Installed assembly1.0.0.0, informational `1.0.0+1de8b576ad31690718c1dbaa348a95f400d45c58`, Debug
configuration. It was fully decompiled read-only. Its public/compiled settings, UI and postfix
behavior agree on the inspected paths; public commit is not asserted to be the binary's revision.

## Complete source behavior

| File / member | Inspected behavior |
| --- | --- |
| `ISOXModSettings` | `placeOnChunk=true`, `placeOnTree=false`, `debugMode=false`; same global Scribe keys/defaults |
| `InteractionSpotOnX` constructor | Reads settings, Harmony owner dragonissa.InteractionSpotOnX, unconditional PatchAll |
| `DoSettingsWindowContents` | English chunks/tree checkboxes, gap, debug checkbox; settings title Interaction Spot On X |
| `Patch_InteractionCellStandable.Postfix` | Returns if native already accepted or both options off; scans all interaction cells and every Thing, ignoring selected chunks/trees, retains other nonstandable/identical/planned-build blockers; otherwise overwrites report with acceptance |
| `IsChunk` | Native ThingRequestGroup.Chunk.Includes on actual Thing Def |
| `IsTree` | Plant category and plant properties IsTree |
| Debug branch | Logs last qualifying blocker label immediately before acceptance; no null guard or preview throttling |

No other patch, XML change, Def, job, hauling/cutting order, movement rule, bottom tab, gizmo or
save-game component exists in this source. The feature changes the planning predicate; it does not
itself remove obstacles or make a completed building usable. That is a source fact, not evidence
that every allowed building can or cannot be worked while a chunk/tree remains.

## Native1.7 contract and source risks

Freshly extracted `RimWorld.GenConstruct`, `Verse.ThingUtility`, `Verse.ThingListGroupHelper` and
`Verse.ThingRequestGroupUtility` from the hashed game. `InteractionCellStandable` has an additional
optional `thingToIgnore` argument that the source postfix does not consume. It:

1. Gets every required rotated interaction cell, without inventing a fallback cell.
2. Rejects out-of-bounds positions before reading the Thing grid.
3. Skips the explicitly ignored Thing.
4. Rejects non-Standable or identical actual Defs, then non-Standable or identical planned entities.

`CanPlaceBlueprintAt_NewTemp` invokes this before other interaction/terrain/footprint/worker rules.
The source patch affects this single subcheck, not all of those later checks. Additional modded
restrictions inside/replacing that subcheck can nevertheless be overwritten by its broad postfix.
`ThingUtility` honors multiple offsets first, otherwise a declared single interaction offset, and
returns shared scratch storage; callers must not retain it over nested callbacks. Chunk membership
in current `ThingListGroupHelper.Includes` is direct Chunks or StoneChunks membership, not arbitrary
recursive category/name matching. Core XML confirms stone chunks and steel slag in that family;
plant shape determines trees. These static facts still need finalized-content proof for providers.

**Inspected risks, not live-reproduced reports:**

- Out-of-bounds native rejection is followed by an unguarded Thing-grid lookup in the source.
- The postfix does not preserve `thingToIgnore` when it rescans, potentially retaining a rejection
  from an object native callers intentionally exclude.
- A foreign rejection with no remaining checked blockers can become acceptance, even without a
  qualifying chunk/tree; the source does not establish the original cause.
- Debug logging dereferences the last blocker even if none was found and can repeat on previews.
- Blanket PatchAll has no executable semantic admission or atomic failure policy.

Selected corrections retain all bounds/ignored-object/equality/planned and provider restrictions,
relax only the actual qualifying obstruction, keep current rotation/final check authoritative and
make diagnostics temporary and bounded. A narrow branch change is a design candidate; no valid
runtime target or supported foreign combination is claimed merely from this analysis.

## UI, composition, contracts and persistence

Source has only global configuration. Owned chunks=true/trees=false preferences move into ordered
Construction / Interaction positions common global/save/Use global rows. No bottom tab/toggle.
Debug moves to the top developer toolbar, temporary/off and cleared on exiting developer mode or
session. New owned text requires seven languages. Source English strings/settings file are not
migrated automatically; there is no selected import or new entity save data.

S048 orientation supplies the final per-cell rotation; S036 Architect selects the designator.
S028 replacement/planning owns old-object exclusion, fog/rock planning and reveal revalidation.
S060 construction access and native jobs remain authoritative. S063 advance plant clearing remains
limited to its existing actual-work footprint; this exception never silently adds interaction-cell
tree orders. No self-patching, global obstacle mutation or duplicate map cache. Local native Thing
lists suffice; no need to scan or duplicate S033's shared index for a handful of positions.

A bounded binary search for InteractionSpotOnX/InteractionSpotOnChunk across installed Workshop
DLLs found only the source DLL. Public settings/type/Harmony identity are observed surfaces but
no external consumer was demonstrated. Do not infer universal absence or create a speculative facade.
Co-installed original retains authority: withhold owned overlap and warn, explain inactive owned
preferences, never unpatch/remove the original. Other providers require exact package/shape evidence.

## Open work and evidence

- **ISX-Q1 resolved:** complete source/native inventory, defaults and selected deviations.
- **ISX-Q2:** exact effective beta placement hooks, all native install/reinstall callers, compatible
  and incompatible foreign branches, skip-original and later repatch admission.
- **ISX-Q3:** representative modded chunk/tree and multi-position fixtures; direct S028/S036/S048/
  S060/S063 behavior without changed clearing or planning authority.
- **ISX-Q4:** native preview/confirm/clear/construction/use, global/save/main-menu settings and bounded
  debug acceptance on the reviewed future build; no check is closed by this source read.

Ignored local evidence `artifacts/Refinement/megamod-spec-20261106/interaction-spot-on-x/` retains
author HTML, pinned text/manifests, compiled/native extractions, exact hashes, review and validation.
No image assets were chosen or inspected, no game/config/Workshop mutation performed. Final balance
review for this draft confirms unchanged resources/work/research and no new clearing authority;
actual job reachability and compatibility remain future native evidence.
