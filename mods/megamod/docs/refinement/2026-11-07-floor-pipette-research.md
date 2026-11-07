# S136 Floor Pipette Tool research — 2026-11-07

Owner Make Rimworld Great Again (`fumblesneeze.megamod`, `mods/megamod`),1.7 beta.
Source/static native evidence; no runtime acceptance or universal compatibility claim.

## Public-first provenance and package identity

[Workshop3706877822](https://steamcommunity.com/sharedfiles/filedetails/?id=3706877822) describes an
Architect action that samples existing floors to select their construction tool. Credits: ferny,
Taranchuk for C#, Oskar Potocki for art. Current author note calls v2 the1.7/asset-pack update.
Comments describing failures are reports, not reproduced defects or permission to copy code/art.

Installed `F:/Steam/steamapps/workshop/content/294100/3706877822`, package
`ferny.FloorPipetteTool`, About supports1.6/1.7, requires Harmony and load-after Core/DLC/Harmony.
No modVersion; assembly1.0.0.0. Supplied source and all four decompiled files (including generated
metadata/access attribute) read. Public [repository commit](https://github.com/fernyrepos/Floor-Pipette-Tool/commit/49a9976438f22f3c99815b1dc6b24a2fde3fe0c7)
`49a9976438f22f3c99815b1dc6b24a2fde3fe0c7`, message “meta”, matches installed git HEAD; GitHub API
confirmed it. No license in installed tracked source or GitHub license endpoint (404). No reuse
permission inferred; independent implementation/art, no copied assemblies or textures.

| Input | Bytes | SHA256 |
| --- | ---: | --- |
| About/About.xml | 985 | 7b468218f7e199f781e74f7cf6f668698286f5118aaae0feb64751926a94a8bc |
| About/PublishedFileId.txt | 10 | 80bba6626315905476ed2186ff5e03ae6a66f667cdc1892e0e984cd1ba84c302 |
| 1.6/Patches/Patch.xml | 274 | bc37167f78cd1e336a6b522ba9927dddfa37c07846fe99b18c6327ce075b494c |
| 1.6/Assemblies/FloorPipetteTool.dll | 7680 | e00a7eb6f2f29bb79390927a6841e76bd4755cd67f0bbc9047b3a1b37cacd237 |
| 1.6/Source/Designator_FloorEyedropper.cs | 2088 | 2d3f1facfe643ad062980c7fd312a1c9499d59eb49a2a625bca812caeeb728b8 |
| 1.6/Source/FloorPipetteToolMod.cs | 265 | 661d88612431a2ba1bddd558ac85623081b5ae3b9a2a3797ae6a05acd66ee61e |
| Languages/English/Keyed/Keys.xml | 325 | c8d3c00f93b238b9f6cbd26a838f041acb67165c43cde567bb7dbc403ed3ee6a |
| Assets.zip | 6051 | af27388bc070c4aeaefcf069c3d85a01af7cebb5ad6a16d1590b9da3a49c9dbe |

There is no1.7 folder or LoadFolders.xml. Inspected native ModContentPack.InitLoadFolders falls back
to highest available version<=current, then Common/root, so this layout selects1.6 plus root for
1.7. Assets.zip contains only the22020-byte DDS icon, bake.manifest and textures.info, not alternate
code. IL confirms calls to FindAllowedDesignator(BuildableDef,bool) with true; optional-argument
decompilation displaying one argument does not prove a missing binary method. No source changes
were made, and static loading analysis is not a successful live1.7 run.

## Complete behavior and native inspection

- Constructor sets four English-keyed labels/hover texts, UI/FloorPipetteGizmo icon, mouse icon and
  **Designator_Cancel hotkey**. Native key defaults C and Floors already includes Cancel, so this
  introduces a binding collision. No source settings, MainButtonDef, saved state or custom API.
- CanDesignateCell rejects out-of-bounds/fog, missing terrain or category!=Floors, then requires
  BuildCopyCommandUtility.FindAllowedDesignator. No natural-ground/general terrain copying.
- DesignateSingleCell reads terrain and resolves designator again, plays success sound and Selects.
  It does not repeat bounds/fog/category admission or place a blueprint. Native input usually calls
  CanDesignateCell first; direct/stale invocation and unified hover feedback still need correction.
- DrawMouseAttachments reads in-bounds terrain/category and shows its name **without checking fog**
  or build availability. This is a source information leak inferred from code, not live reproduction.
- SelectedUpdate renders mouseover bracket; RenderHighlight delegates native selectable-thing
  highlight. No instant floor placement/removal path exists.
- Source XML blindly appends FloorPipetteTool.Designator_FloorEyedropper to Floors/specialDesignatorClasses.
  Mod constructor calls Harmony.PatchAll but there are no patch classes in complete source/binary.

Native Assembly-CSharp1.7.5000rev1275,15636480 bytes, SHA256
`d55aa8e47fa3c5463ca85b0eccd51f195b4bbcd04f56da3af21c2f6688a81254` inspected. Full BuildCopyCommandUtility
and DesignatorManager, focused Designator_Build visibility/input/construction/color-eyedropper,
ModContentPack.InitLoadFolders, native Floors category and key Defs read.
Ignored evidence: `artifacts/Refinement/megamod-spec-20261107/floor-pipette/` manifest, compiled, IL,
native extracts. Evidence hashes are not executable compatibility predicates.

Native resolver searches nested dropdowns, Current.Game.Rules.DesignatorAllowed and exact PlacingDef,
caches the result (including null), and rechecks Visible on cache hits. It does not re-run all
category/rule membership on a hit. Visible considers research, tech, difficulty, optional progression,
place workers, buildings and discovery; god mode has its ordinary bypass. Use current S036/S038
evaluation rather than treating cached presence as permanent permission. S038 requested edits remain
pending until its save/reload boundary; only its applied exclusions/category policy governs sampling.
Immediate paused invalidation covers genuinely effective changes, not pending content edits.

Native DesignatorManager keeps a local original designator during mouse-down, calls admission,
DesignateSingleCell and original Finalize, then consumes input. Drag/release paths depend on current
draw style, so the owned contract must prove the gesture cannot fall through into placement.
Native actual placement separately calls GenConstruct and creates a blueprint or native god/zero-work
instant result. The sampler never calls that action. Native colored-floor eyedropper selects a
color within a dropdown with includeEyeDropperTool; it is preserved, not replaced by this tool.

## Selected differences, UI and ownership

Keep Floors-only current exposed terrain and ordinary availability; don't extend to buried layers,
planned construction or differently categorized terrain. Pick exact variants, not source paint.
Correct fog disclosure, recheck at click and provide useful blocked reasons. No default shortcut:
preserve native C/Cancel and S067 removal controls. Add one-shot active command/dropdown reveal for
S036 pagination; no repeated snap-back or replayed construction input. Share its current resolver,
not another floor/Thing index. S038 exclusions remain authoritative without rewriting existing ground.

Architect/Floors is the contextual action surface. No new bottom tab, view/behavior toggle, settings
row or debug control. Sampling/map/page state is transient UI state, not per-save configuration.
Seven-language UI and owned icon require future native/independent asset acceptance.

XML Extensions required for checked additive entry: unique category/node, existing siblings retained,
no duplicate membership, no whole-list replacement. Guard actual native selection semantics and
fallback to remaining Architect if unsupported. Source active withholds owned sampler with warning;
do not remove source commands or impersonate package/type. No Harmony needed merely for source's
empty PatchAll. Any later necessary patch requires its own executable semantic target spec.

Bounded installed XML/C#/JSON/Markdown scans for package/type/key names found only the source's own
files. No actual external consumer identified; public type and translation keys exist but no broad
facade is selected. Native persistence stays native; no migration or saved sampled-floor state.
No source texture copied and no raster asset selected/created during this documentation work.

## Remaining work and balance

FPT-Q1 inventory resolved. FPT-Q2 deliberate scope/shortcut/reveal UX; FPT-Q3 current effective
selection/fog/input/provider behavior; FPT-Q4 implementation/localization/art/native acceptance.
Compare native manual Architect selection and colored-floor picker with source and owned sampler:
same ordinary research/material/time/placement costs, fewer discovery steps, no gameplay discount.
Runtime proof must include real sample→separate blueprint→pawn-built floor, no-order gestures,
fog/research/exclusions, paused changes, map switching, dropdown/paging and source-present fallback.
