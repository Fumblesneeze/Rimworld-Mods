# S067 — floor/order shortcuts research

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
Inspected **2026-11-04**, read-only. Origin: replacement with one proposed keyboard correction.
[Change](../../../../openspec/changes/specify-megamod-floor-order-shortcuts/proposal.md).
This is source research and refinement, not implemented or live-verified behavior.

## Documentation and identity

**Add Deconstruct Button To Floor Category**, GeodesicDragon,
`geodesicdragon.deconstructfloorcategory`, [Workshop 2905837332](https://steamcommunity.com/sharedfiles/filedetails/?id=2905837332).
Installed at `F:/Steam/steamapps/workshop/content/294100/2905837332`, modVersion **1.1.4**;
About advertises **1.1–1.7**. Author-provided About description and changelog were read before XML.
The 2026-10-23 changelog records 1.7 support; version 1.1.2, 2025-02-08, added Remove Floor to Orders.

Public Steam URL, English-language URL and search were attempted first but did not return the page;
a direct public HTTP attempt also failed. **No claim that the Steam page was read.** Installed
author documentation supplies the available primary description. About links an author Discord,
not a source repository. No license file was found; no upstream code, binary or art is copied.

Complete installed inventory: About/About.xml, About/Changelog.txt, About/PublishedFileId.txt,
About/Preview.png, Patches/AddDeconstructGizmoToFloorCategory.xml and
Patches/AddRemoveFloorButtonToOrdersCategory.xml. There is no DLL, C#, LoadFolders, settings or
translation directory. Preview was not inspected, selected or reused.

| Input | SHA-256 |
| --- | --- |
| About.xml | `EE4E58BA5AF9C5A3967D217374E0CBC32726BBD29E9B42412B9A0FCFB13A8794` |
| Changelog.txt | `570D0213FC9E4878A27513B398841DBC1C4756D3AB1C3FD355691D817BB5E99F` |
| AddDeconstructGizmoToFloorCategory.xml | `8480A07C6B99359CA55B772667842955563136912253374058F4688C8F0DE145` |
| AddRemoveFloorButtonToOrdersCategory.xml | `523E52D9492B2B9B4BDF6615DA7EDC44CB6F1CFDD07F43A0AB823FFEBDA13EC5` |

## Complete source behavior

Each XML contains one unguarded PatchOperationAdd. The first appends `Designator_Deconstruct` to
the Floors DesignationCategoryDef's specialDesignatorClasses. The second appends
`Designator_RemoveFloor` to the same list in Orders. Neither relocates an original command,
checks for existing membership, changes work or creates a separate menu. No hidden C# feature exists.

The replacement selects both additions, using independent presence/shape guards and merge-preserving
membership. A valid missing list can be created with its entry in one mutation; malformed or
ambiguous content is skipped with an explanation. No replacement of the complete command list.
Same native command already present means no new duplicate; similar labels are not identity proof.

## Current native evidence

Installed game **1.7.4998 rev1299**, Assembly-CSharp.dll SHA-256
`FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B`.
Core `Data/Core/Defs/Misc/Designations/DesignationCategories.xml` SHA-256
`DBC93C9069AF619EDBB53C72D1F73B523BA72083F6A886106601E7883E6A17C3`.
Orders already has Deconstruct and PlantsHarvestWood. Floors already has RemoveFloor and the
separate RemoveFoundation. Source changes append the complementary commands, not fixed screen slots.

Offline ILSpy output is local under
`artifacts/Refinement/megamod-spec-20261104/floor-order-shortcuts/native/`. Fresh types inspected:
Designator_Deconstruct, Designator_RemoveFloor, WorkGiver_ConstructRemoveFloor, JobDriver_RemoveFloor,
DesignationCategoryDef, and targeted Command/GizmoGridDrawer rendering. The current Orders command
constructors were additionally inspected for bindings, not claimed as a full behavior audit.
No game/Workshop assembly was loaded or executed for this research.

| Native tool/path | Observable boundary retained |
| --- | --- |
| Deconstruct | Buildings eligible for the player; repeated deconstruct/uninstall orders rejected; top eligible Thing selection; normal fog/ownership limits |
| Deconstruct exceptions | Native god mode, frames and zero-work cases can remove directly; occupied holding-platform warning remains; no new instant-removal exception |
| Remove Floor | In-bounds revealed removable top floor; rejects repeat orders, full impassable blockers and loss of required building support |
| Remove Floor work | Ordinary cell designation, native removal work and cancellation; native god mode removes directly; no new labor/yield/refund tuning |
| Terrain layering | Removal uses the native temporary/top-layer path and cleans filth; underlying foundation is considered for support; Remove Foundation is a different command |
| Category resolving | Creates native command instances from class entries; no duplicate suppression; preserves game-rule/research availability checks |

Inspected JobDriver_RemoveFloor has base work 200 with ConstructionSpeed and clearSnow enabled.
These are native comparators, **not new fixed megamod balance constants**. Reusing the native action
allows current game/compatible-provider costs and refunds to remain authoritative.

### Keyboard evidence and proposed correction

RemoveFloor and PlantsHarvestWood both use `KeyBindingDefOf.Misc1` (Core default **B**).
Deconstruct has its own native binding (default **X**). Command rendering consults
`GizmoGridDrawer.drawnHotKeys`; the first displayed command for a key draws/handles it and consumes
the event. Consequently this is a **display-order-dependent allocation**, not evidence of two
commands executing together. No live keypress experiment has occurred.

Proposed correction: Orders' additional Remove Floor is mouse-only; its Floors counterpart keeps
the native binding. Other Orders commands and global remappings remain untouched. S036 must carry
this contextual policy through search/pagination/order changes, including when optimization is off.
This adds a small runtime presentation concern to an otherwise XML-only source. Keep the exact
native command class and action; do not introduce a custom deconstruction/removal implementation.
If the contextual correction cannot be admitted, retain native command/key allocation, warn that
the correction is unavailable, and do not claim stable Orders shortcut allocation. The XML
contributions and this correction are separately admitted groups, not an unimplementable promise
that XML rolls back when a later presentation hook fails. FOS-Q1 retains the proposed policy.

## Consumers, coexistence and related features

Installed XML/CS/JSON package-ID search found the source About plus RimPy Mod Manager Database
(`1847679158/db/db.json`): its `database.2905837332.packageId` is a catalog entry. No runtime
consumer was found by that scoped scan; this is not an all-binary consumer audit. Source has no
assembly/type API, own saved keys or settings import to preserve. Native command identity remains.
Do not spoof the old package ID to satisfy a catalog entry.

Select the active original package as the placement provider when co-installed: skip our two
duplicate additions, let its native entries supply the shortcuts, and independently admit the
contextual key correction against actual supported native Orders instances. This avoids assuming
the original's unconditional appends run before our duplicate check. Source-present provider
selection is not a failed patch warning or a claim that foreign patches are incompatible. An
older/changed source missing one command is not silently repaired or claimed to provide full parity.
Other mods' already-present exact native entries need no additional entry; late duplicate injection
remains external behavior, not authority to delete arbitrary foreign list members.

S036 Architect presentation consumes the same native entries/current availability. S008 order
tools and S061 roof-safe removal compose through their own ordinary policies; no internal Harmony
stack. S136 Floor Pipette remains separately queued: sampling a floor must not issue a removal order.
No shared Thing-index consumer or private scan/cache is needed for these menu additions.

Both controls belong in existing Architect categories. No bottom tab, settings page, bottom-right
toggle or new debug control. Native god-mode consequences remain native. Source has no configurable
policy/save state; no inheritance exception is invented. Native labels/icons are reused; new
player-facing explanations need all seven baseline languages. No new artwork is selected here.

## XML Extensions and remaining work

Official [Conditional](https://github.com/15adhami/XmlExtensions/wiki/XmlExtensions.Conditional),
[Log](https://github.com/15adhami/XmlExtensions/wiki/XmlExtensions.Log) and
[FindMod](https://github.com/15adhami/XmlExtensions/wiki/XmlExtensions.FindMod) references were consulted.
Complete selected installed type decompilation confirms node-existence branching, warning logging
and package-ID matching. XML evidence is under the sibling `xml-extensions/` artifact folder.
Inspected `2574315206/1.6/Assemblies/XmlExtensions.dll` SHA-256
`1E64E318C905B94DE3EF05186D82E2067933238453D818366EAC73D688464B35`.
About advertises 1.2–1.6 and LoadFolders has no explicit 1.7 entry. **This is not evidence of beta
compatibility.** XML Extensions becomes required at the first actual XML use; exact 1.7 dependency
loading/admission remains implementation preflight. Harmony becomes required on eager use.

| Question | Remaining work / proposed direction |
| --- | --- |
| FOS-Q1 | Review proposed Orders mouse-only correction; retain native Floors/remapped bindings and no extra settings. No user reconfirmation required to continue drafting. |
| FOS-Q2 | Prove exact 1.7 XML Extensions route, effective category/type/binding target shapes, source/provider selection and repeated-resolution/repatch lifecycle before implementing patches. |
| FOS-Q3 | Focused RED/GREEN, package/language checks, independent implementation review and real Architect click/key/drag/cancel/pawn-work evidence, including S036/S061 and original-present runs. |

Semantic balance pass: these are navigation changes, not new building/terrain eligibility, free work,
resource yield, research unlocks or roof immunity. Ordinary removal must still be performed through
the existing job path; native instant exceptions are stated rather than hidden. The proposed key
deviation affects access only. No performance benefit or runtime compatibility is accepted.

## Review checkpoint

The draft contains **5 requirements, 16 observable scenarios and 18 unchecked tasks**. A fresh
independent source/native/composition review found no actionable P1/P2 issues and confirmed recorded
source/game/XML Extensions hashes. The review covered both upstream additions, merge/admission,
original-provider policy, contextual key fallback and S036/S061/S008 composition. S136 remains queued.

Strict typed OpenSpec validation passed **89 items, zero failures**, exit code 0. The CLI separately
reported a PostHog telemetry transport failure; schema validation succeeded. All **8 scoped Markdown
documents** passed local-link and whitespace checks. Queue: **68 reviewed drafts / 136 entries /
290 unique question IDs**. FOS-Q1–3 remain open; no runtime compatibility or acceptance is claimed.

No runtime tasks are checked and no code/Defs/assets, build/install, tests, game launch, source
execution, Workshop edit or normal save/configuration mutation occurred. **Next: S068 All Memories Fade.**
