# S048 — Auto-Facing Chairs research

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Inspected **2026-11-03**, origin replacement, **P1-42**. Complete source/native analysis supports the
observable draft. Independent review and strict validation passed; no implementation or native acceptance.

The user explicitly requires manual rotation to survive all mouse movement **within the same cell**,
with automatic facing allowed again on entering the next cell. This is already captured in the
[identity/refinement audit](2026-11-01-source-audit.md). It is a required behavior, not an optional
heuristic or permission to override every mouse movement.

## Documentation and provenance

Read the author's [Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3783758781)
first, then its [linked repository](https://github.com/scarf005/rimworld-AutoFacingChairs). The public
README describes seat-facing, interaction markers/eating surfaces, built/planned/frame targets,
cell-entry snapping and preserving rotation on conflicts. It also mentions an XML patch that is
absent from both installed files and the inspected repository tree; that claim is stale.
The Workshop's Combat Extended compatibility statement is not accepted as tested evidence.

Installed `F:/Steam/steamapps/workshop/content/294100/3783758781` contains About, DLL/PDB, preview,
PublishedFileId and English/Korean settings keys. **Auto-Facing Chairs**, scarf,
**`scarf.autofacingchairs`**, About **1.1.1**, supports **1.6** only. Required source dependencies:
Harmony and **`latta.fsharp.core`**. No installed source, XML patches, Defs or explicit1.7 folder.
Root DLL assembly/file1.0.0.0, informational
**`1.0.0+8bc770c5ae13e3adc141d0e13539b562e757ce7a`**. Beta loadability is not established.

Read-only reference clone under `artifacts/Refinement/megamod-spec-20261103/auto-facing-chairs/source`
is HEAD **`f7e8a542f367be5aa21575bf7c6bea10d282623f`**; fetched the DLL's informational commit too.
The repository has **AGPL-3.0** license text. Complete executable source is one F# file,
`Source/AutoFacingChairs/AutoFacingChairs.fs`; it and the project/README were read completely.
ILSpy11.2.0.10050 decompiled installed DLL under sibling `compiled`; functional types were read.
No code, FSharp.Core assembly, art or language files were copied into the product and no upstream
build/install/enable script was executed.

| Input | SHA-256 |
| --- | --- |
| Installed DLL | `1E07815DD74B84C79D8613D7FFF9D379A9B40FAFB02621241418A3A15FA816FA` |
| Installed About.xml | `335EE41CC96DEB23F3A956D3215AB401F434AAC2C8C0D52BA74BEFDD6E8D5E16` |
| HEAD AutoFacingChairs.fs | `01D58C2CD2E8EE75C0CC6990400C89399F899247EEE9082CEF69DFECC6FFF56C` |
| Native1.7.4995 rev309 assembly | `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21` |

**Revision discrepancy:** the DLL informational commit is the parent1.1.0 revision, but its inspected
facility relation/radius code agrees with HEAD1.1.1's removal of reverse facility-to-affected-target
rotation. HEAD commit message says it stops beds auto-rotating. Do not treat informational revision
as proof of exact source equality or accidentally reintroduce the removed reverse behavior. Installed
DLL behavior is authoritative for replacement analysis; explicit source differences are evidence.

## Observed source behavior

- Global `chairsOnly` boolean defaults **false**, persisted under that key through ModSettings.
  Settings load after the long event and appear as one localized checkbox. No per-save inheritance,
  enable toggle, bottom tab, debug button, custom keybinding or saved placement state.
- Static bootstrap unguarded `PatchAll`, Harmony owner **`scarf.chairsnap`**. Two hooks on
  Designator_Place: Selected postfix resets state; SelectedUpdate prefix attempts orientation.
  **Both act only on Designator_Build.** The words “when installing” do not establish support for
  the separate native Designator_Install path.
- Candidate placing Def must be rotatable and have building properties. If chairsOnly is enabled,
  it must also be sittable. Default false allows recognized facilities as well as seats; it does not
  arbitrarily rotate every building merely because it is rotatable.
- Per-build-designator weak state remembers current Map reference and mouse cell. Selection resets
  it. No map, out-of-bounds cell or hovering Architect info panel returns without rotation. A valid
  already-remembered map/cell returns early; new valid cell is remembered **before** finding a result.
  Thus own repeated updates do not overwrite Q/E/manual rotation in the same cell, even if furniture
  changes there. Moving to another valid cell reevaluates; returning later also reevaluates. No
  explicit manual-input listener exists. Returning from the excluded panel to the same remembered
  cell does not by itself re-run snapping. These are static observations, not live acceptance.
- Reads/writes native private `placingRot` by reflection. Missing field silently leaves behavior
  inactive; method and effective data flow are not semantically validated and no startup warning
  proves skipped-feature visibility.

### Seat-facing constraints

For each candidate target, source resolves ThingDef through `entityDefToBuild` if applicable, letting
ordinary blueprints and frames describe their intended building. For a sittable placing Def:

1. A target with a sittable `interactionCellIcon` supplies a facing at its actual interaction cell.
   Nonempty multipleInteractionCellOffsets take precedence over hasInteractionCell/single offset.
   Target's rotation is used unless interactionCellIconReverse, which uses its opposite.
2. If no interaction-cell result for that target, an eating surface supplies facing when the chair
   anchor cell is cardinally adjacent to its rotated occupied rectangle. West side→East, east→West,
   south→North, north→South. Corners/interior/nonadjacent cells yield no table-facing constraint.

Source lazily builds sparse candidate offsets once from all seat-icon interaction offsets in four
rotations plus the four cardinal neighbors. It uses current native ThingGrid cells and a per-search
seen set to avoid applying the same multicell object twice. No independent map-wide Thing index.
Def-derived cache freshness after provider changes and larger-seat/large-target geometry still need
contract checks; do not blindly inherit incidental search-radius omissions.

### Facility-facing constraints

Only the **placing object is a facility** and the target is affected by that facility. The target's
linkableFacilities must include the placing Def. Source tests four rotations with native
CompAffectedByFacilities.CanPotentiallyLinkTo_Static's Def/position/rotation overload. If multiple
rotations work and the target is rotatable, it narrows to the target's same rotation when allowed,
otherwise its opposite when allowed, otherwise retains the whole allowed set.

Targets with no permitted rotation are ignored as a relation. Nonempty relation masks intersect
across all encountered targets and seat constraints. If there is no relation or intersection becomes
empty, current rotation is unchanged. Otherwise keep current rotation if allowed, else choose first
allowed North/East/South/West. Source does not select a nearest target or maximize number of links.

Facility search uses a clipped square around the placement cell: base2 for adjacency/bed-head
constraints; for linear-facing relations max(8,ceil(maxDistance)+2); otherwise max(2,ceil(maxDistance)+2).
Add the placing object's largest size dimension. It scans ThingGrid cells with the same seen set.
These are source search heuristics, not selected gameplay range settings; large target footprints,
unusual provider distances and bounded costs require analysis before drafting a supported contract.

## Native1.7 observations and scope gaps

Native types were decompiled to sibling `native`: Designator_Place, Designator_Build,
Designator_Install and CompAffectedByFacilities. Inspected relevant source paths:

- Designator_Place.Selected resets placingRot to the Def's default. SelectedUpdate uses it for ghost,
  interaction cells and placement checks; it also has a native wall-attachment rotation path.
- Native keyboard, middle-click and visible rotation controls update placingRot. Designator_Build
  creates the real build blueprint using that rotation; source orientation is only a placement aid.
- **Designator_Install derives from Designator_Place independently of Designator_Build**. Installed
  source hooks exclude native install/reinstall. The owned draft must explicitly choose and document
  supported build/install/reinstall workflows rather than repeat the description's broad wording.
- The native Def-only facility overload checks adjacency, bed-head/facing, distance or linear
  containment geometry. It is **not** full actual-link validation: other overloads/lifecycle add LOS,
  medical-bed restrictions, facility usability, simultaneous-link limits and selection. Do not promise
  a working bonus solely because this geometric orientation suggestion succeeds.
- Source facility relation is one-way in inspected DLL/HEAD: placing a bed/other affected object near
  an existing facility is not a reason to rotate it. Preserve this deliberate1.1.1 restriction unless
  a separately specified change says otherwise.

Ordinary legality, cost, work, research, materials, construction and actual facility links remain
native. No free placement or automatic rotation of already placed objects is supplied. Same-cell
protection from **this feature** does not prove no native/other provider path later changes rotation;
the owned target/admission and observable user requirement must account for that distinction.

## Completed checks and selected draft

Bounded installed DLL scan for AutoFacingChairs, scarf.chairsnap and scarf.autofacingchairs found
only the original DLL. No outside executable consumer was found; no facade/package spoof or source
settings import is justified. Compiled startup initialization was also read: native Rot4 order is
North/East/South/West and the actual bootstrap remains unguarded PatchAll. No hidden XML patch path.

Native Designator_Install was read completely. It preserves source item selection, delegates validity
to native CanPlaceBlueprintAt, writes placingRot into native Install/Reinstall blueprints, and deselects
after designation. Its ProcessInput also retains native blueprint-cancellation and Anomaly warnings.
The owned extension must preserve those behaviors instead of treating relocation as new construction.
Core BenchBase supplies a DiningChair marker; supported scanner definitions demonstrate reversed
chair markers. Source metadata and native contracts, not English object names, explain the relation.

The [owned draft](../../../../openspec/changes/specify-megamod-furniture-orientation/proposal.md)
selects new-build **and actual install/reinstall** assistance, explicitly extending inspected source
coverage. It retains one-way facilities, source relation precedence/conflict/ties, native benefits
and same-cell behavior, with explicit manual-input priority at cell entry. Source incidental search
heuristics are not gameplay range caps. Chairs only=false moves to common global/save policy, applied
at the next cell/session; cursor rotation remains transient entity interaction.

Independent C# needs no source FSharp.Core or XML-tagging dependency. Native/shared data supplies
current geometry; no duplicated per-port index. Actual patches need semantic target checks, effective
input/preview/commit ordering, owned-only rollback and startup diagnostics. Direct owned composition
with S036/S028 and future S129 replaces any internal patch stack. No claim that a whole provider is
compatible merely because its declared versions or descriptions look suitable.

| ID | Remaining work |
| --- | --- |
| AF-Q1 | Exact native manual-input/preview/validity/build/install/reinstall semantics, per-context admission, stable target docs, foreign ordering and bounded failure behavior |
| AF-Q2 | Complete multicell/long-offset/facility geometry, native/shared query coverage/freshness and measured bounded cost; distinguish geometry from real links |
| AF-Q3 | Common Chairs only policy, new-session and item/map lifetime, S036/S028 and future S129 direct composition, compatible actual provider cases |
| AF-Q4 | Honest-tier TDD, seven-language UI, independent implementation review/package and personally observed native placement/construction/install workflows |

No gameplay implementation or acceptance. Sources and Workshop files remain unchanged; all future
runtime tasks are open and no ghost-only screenshot can close the actual placement workflow.
