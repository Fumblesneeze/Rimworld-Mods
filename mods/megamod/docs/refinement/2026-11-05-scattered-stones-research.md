# S112 — Scattered Stones research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, **1.7 beta**.
Origin **replacement**, inspected **2026-11-05**. Documentation/source research only; no runtime or
asset acceptance. [Change](../../../../openspec/changes/specify-megamod-scattered-stones/) and
[coverage](../replacements/scattered-stones.md).

## Public documentation and exact identity

Read [current Workshop3528503211](https://steamcommunity.com/sharedfiles/filedetails/?id=3528503211),
its [change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/3528503211), the
linked [original2598436855](https://steamcommunity.com/sharedfiles/filedetails/?id=2598436855) and
[1.5 continuation3278176206](https://steamcommunity.com/sharedfiles/filedetails/?id=3278176206)
before interpreting implementation. The original web-tool request was rate-limited; direct public
HTML retrieval succeeded and supplied the author repository link. All three HTML pages are retained.

The current maintainer describes updated1.6/1.7 builds, varied chunk appearance and cliff/chunk debris.
Older public documentation describes configurable scattering, persistent debris while rock remains,
texture/category compatibility and negligible runtime cost. Those are intent/claims, not measured
performance or verified compatibility. Current comments report storage appearance, flooding and
settings-window issues; no live reproduction or attributed cause is inferred from them.

- Installed title **Scattered Stones1.7**, package **Owlchemist.ScatteredStones**, Workshop
  **3528503211**. Workshop maintainer **kkirspel**; About author still **Owlchemist**. Current page
  credits original Owlchemist and Hank Bot's1.5 continuation; that older page currently displays
  creator **Satpyn**. Do not collapse display names into a new package identity.
- Installed About supports1.3–1.7, requires Harmony. Root shared XML/textures plus versioned DLLs;
  no LoadFolders, installed source or license file. Select **1.7/Assemblies/ScatteredStones.dll**.
- Selected DLL **17,920 bytes**, SHA-256
  `1637A7128B820F247B42A6A0BF4A80263B799F0093861333C43D4F71A4C9ED3F`;
  assembly **1.5.1.0**, file **1.5.1**, informational **1.4**, **Debug**, MVID
  **71bb8368-96f9-4067-ab3e-2ac755c6a711**. Old assembly labels do not negate the selected1.7 folder,
  and the folder label is not runtime acceptance.
- Original [Owlchemist/scattered-stones](https://github.com/Owlchemist/scattered-stones), master
  commit **ff68f1ed5827642713c0d200fbaa53d6d5e0c29b**, **2022-12-03T22:06:21Z**. Eight public runtime
  source files read against the complete selected binary. This is historical source, not a claim
  that it reproduces the continued1.7 binary. The continuation pages link no modern source repository.
- Fifteen public text files are retained with hashes/Git blobs. The root Def, patch and English
  keyed XML are byte-identical between that original commit and the installed continuation.
  Original project targets net48; selected decompilation is not a rebuilt public project.
- Original public **MIT LICENSE**, literal copyright **2020 jptrrs**. Installed continuation includes
  no license. Preserve the actual upstream notice for any permitted reuse; do not invent a different
  copyright or assume the license proves all later additions/art rights. Independently implement;
  no upstream assembly, raster pixels or icon is copied/selected for the product.

Ignored evidence: `artifacts/Refinement/megamod-spec-20261105/scattered-stones/`. Includes three
Workshop HTML pages, public commit/tree/text manifest, XML comparison, complete selected binary
decompilation/metadata, ten inspected source/native input identities, seven native type extracts,
and bounded installed consumer search. No target assembly executed or installed content mutated.

## Complete selected behavior inventory

| Source / selected class | Actual inspected behavior | Owned treatment |
| --- | --- | --- |
| Mod_ScatteredStones | Global settings, PatchAll using player-facing package ID, queued Setup; six settings; WriteSettings calls UpdateModifiers(true) | Guarded startup and common settings; no standalone port framework |
| ModSettings_ScatteredStones | Static minScaleModifier/maxScaleModifier/offsetModifier1; allowOnWater/minedFilth true; neverDespawn false; same-named global Scribe keys | Common inherited settings with truthful labels; no source config import |
| RandomDraw | Def extension with min/max1, offset0 and derived modified fields | Owned guarded declaration; no per-save mutation of shared Def defaults |
| Graphic_RandomSpread | Graphic_Random subclass; per-Thing-ID float[4] session cache; every sample seeded from Position.GetHashCode | Stable identity with independent channels; no stale lifetime dictionary |
| Graphic_RandomSpread storage/render | Building_Storage forces both cached offsets to0 permanently; scale/rotation remain; current MatAt/atlas/vertex color and shadow; extraRotation ignored | Storage layout owns all transforms; ground samples restored, actual native/provider draw context preserved |
| MapComponent_ScatteredStones | Saved applied bool; FinalizeInit first pass over map.listerThings.AllThings with captured count; directly places colored debris under candidate chunks/cliffs | One-time idempotent owned seeding with safe snapshot, one patch/cell and failure ownership |
| Initial chunk rule | StoneChunks category, not IsInAnyStorage; water flag checked only here | Preserve unstored scope; apply water/placement rules consistently to all future deposits |
| Initial cliff rule | Natural rock excluding resource rock; unfogged; ValidateCell; not map edge | Actual exposed cardinal cleaning access, no count-of-blocking-objects proxy |
| ScatteredStonesUtility.Setup | Populates static ushort Def-index sets from direct StoneChunks category and natural/non-resource buildings; does not clear sets | Current finalized category/capability catalogue, rebuild on lifecycle changes |
| UpdateModifiers | min = base*minModifier; max = Clamp(base*maxModifier,min,10); offset = base*modifier; clears cached graphics on settings save | Coherent bounds/current save resolution; refresh actual visible maps |
| ValidateCell | Collects owned filth; any adjacent water forces rejection; counts every full-fill or non-Standable neighboring Thing; >3 rejects and optionally DeSpawns local debris | Geometric access by cell, preserve unrelated filth; water setting checks actual deposit cell |
| Rocks.DrawColor / MatchColor | Saved color; source DrawColor when given; otherwise same-cell chunk or granite fallback | Stable saved tint, genuine deposit updates only; no drawing mutation or missing-map access |
| Rocks.Destroy | If neverDespawn and age>=lifetime, any same-cell chunk/cliff causes ThickenFilth and returns instead of Destroy | Protect only actual natural expiry; never cancel cleaning/terrain/construction removal |
| Patch_Destroy | Prefix Mineable.DestroyMined; thickens each owned local filth before mining succeeds; possible null Map list dereference | Refresh age only on verified successful mining, retain stored lifetime |
| Patch_TrySpawnYield | Postfix exact Map,bool,Pawn overload; when minedFilth true scans old mineable cell for any stone chunk, directly creates debris tinted by mineable | Follow actual successful yielded destinations, not preexisting chunks; water/storage checks and dedup |
| Patch_SpawningWipes | For old Owl_Filth_Rocks and new qualifying chunk/cliff, returns false; other result unchanged | Narrow owned-underlay coexistence with semantic guards |
| Patch_SpawnSetup | Non-Standable Building after map day0; checks cardinal neighbors that are not Standable, calls ValidateCell(autoClean=true) | Completed construction/local geometry; safe load/bootstrap distinction |
| ResourceBank / nested DefOf classes | Owl_Filth_Rocks, ChunkGranite, StoneChunks | Actual content guards, stable owned identities; no hardcoded granite dependency for every supported stone |
| Generated Resources and access-check attribute | Resource-manager boilerplate and publicized-reference support | No gameplay or integration contract adopted |

The eight original source files cover twelve top-level runtime classes plus the two nested DefOf
holders. Generated resource/access-support types are separately accounted for. Original public
UpdateModifiers includes `Find.CurrentMap?.mapDrawer.WholeMapChanged(MapMeshFlag.Things)`; the
selected1.7 binary has **no corresponding redraw call**. The explicit TrySpawnYield overload is
also a continued-build change from the old name-only target. Do not claim full binary equivalence.

## Data, settings and actual visual scope

Source `Owl_Filth_Rocks` derives from BaseFilth and names `ScatteredStones.Rocks`, Graphic_Random,
texPath Things/Filth/OwlRocks, drawSize4×4 and color(105,95,97). Its three raster variants and About
preview are inventoried, not selected as owned art or visually accepted. Source patches immediate
ChunkRockBase children only, removes graphicClass, adds Graphic_RandomSpread, then adds RandomDraw
with minSize0.82, maxSize1.28, offsetRange0.27. Other declared graphic fields remain in that source XML,
but another mod's graphicClass is overwritten without checking its semantics.

| Parameter | Source value / behavior | Draft choice |
| --- | --- | --- |
| Minimum / maximum size modifiers |1 each; sliders0.5–2; labels rounded2 decimals |100%,50–200 in1% steps; upper effective size at least minimum |
| Offset modifier |1; slider0–3; same seed on both axes makes diagonal-only displacement |100%,0–300 in1% steps; independently sampled axes |
| Rotation | Integer seeded0–360 inclusive; no exposed rotation setting | Full-turn stable variation, not an invented rotation slider |
| Water / Dirty mining | true / true | Keep defaults, enforce future-deposit rule consistently |
| Never despawn | false; tooltip promises no disappearance except cleaning | Relabel Keep debris while covered by rock; off default, natural-only protection |
| Base lifetime |60–90days | Preserve, with S105 Other lifetime branch once |
| Thickness / work | Max1 /240 | One owned patch/cell,240 base work; ordinary speed/terrain factors |
| Beauty / outdoor beauty / cleanliness |-10 /0 /-2 | Preserve actual gameplay consequences |
| Tracking / rain | canFilthAttach false /rainWashes false | Preserve; legitimate other terrain/removal paths remain independent |

There is no new bottom tab, button, debug control or gameplay window. The source mod-settings page
contains six configuration rows; owned choices use Environment and cleaning / Scattered stones,
with appearance/debris grouping and all common global/save rules. Source English keys exist for rows
and help; category title is hardcoded. All seven owned baseline languages remain required.

Concrete inferred defects/limitations, not reproduced live bug reports:

- Identical seeded calls correlate both offsets; a shared per-Thing cache preserves first-position
  samples until reload, while storage writes0 into them. Source does not clean this cache on map teardown.
- Settings clear the selected1.7 cache but lack the old explicit mesh refresh; immediate visual
  application is not established by WriteSettings alone.
- Seeding/deposit direct placement can create multiple same-cell filth objects despite maxThickness1.
  Max thickness caps each object's layer count, not the number of objects.
- Yield scan can add debris for an old chunk after failed yield or miss a real chunk placed nearby.
  Water/storage rules are not applied to this path. Map/list null/mutation safety is not guaranteed.
- A broad Destroy override conflates natural expiration with cleaning/other removals; tooltip and
  old public description do not match the false default or the actual same-cell support condition.
- Category eligibility for debris is not enough to prove graphic compatibility of all added chunks.
  Raw parent XPath also misses deeper/custom declaration paths and overwrites foreign renderers.

## Native1.7 semantics and composition

Game **1.7.5000 rev1275**; Assembly-CSharp.dll **15,636,480 bytes**, SHA-256
`D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254`;
Version.txt18bytes, SHA-256 `BAD5BED449D17B2AF39925A3A25007164B2B1F89B29A4E24AFA3B505EDCC535A`.
Seven native types were read: Filth, Mineable, JobDriver_CleanFilth, SteadyEnvironmentEffects,
GenSpawn, TerrainGrid and FilthMaker. This is static target research, not effective-patch admission.

- `Filth.SpawnSetup` samples lifetime only for fresh non-load/non-gravship initialization. It saves
  growTick, thickness, disappearAfterTicks, drawing instances and source labels. `ThickenFilth`
  resets age and raises thickness only below the configured max; `ThinFilth` decrements and calls
  Destroy when zero. Source Destroy can therefore turn a completed clean back into thickness1.
- `SteadyEnvironmentEffects.DoCellSteadyEffects` performs possible rain thinning, then age removal
  when duration!=0, age>duration and not already destroyed. This is a candidate narrow renewal
  boundary; no general Destroy veto is needed. Zero lifetime has native permanent meaning.
- `JobDriver_CleanFilth` uses native home-area/target/reservation and Touch travel; per tick, work
  depends on CleaningSpeed and terrain CleaningTimeFactor. It calls ThinFilth after the work threshold
  and proceeds when destroyed. 240 is work, not an unconditional fixed four-second job.
- `Mineable.DestroyMined` captures Map, destroys the mineable, then calls the current three-argument
  TrySpawnYield. KillFinalize destruction also calls it. The obsolete four-argument overload delegates.
  Actual yield checks chance, creates the yielded Thing and uses Near placement with a callback.
- `GenSpawn.SpawningWipes(BuildableDef,BuildableDef,bool ignoreDestroyable=false)` preserves new
  filth but normally wipes old filth under non-Standable new entities. A future exception must apply
  only to admitted rock over owned debris, not arbitrary foreign dirt or all buildings.
- `TerrainGrid` destroys filth when the new terrain rejects it through `FilthMaker.TerrainAcceptsFilth`.
  `FilthMaker` can merge an existing same-type patch and performs terrain/placement checks. Water
  setting is permission for new deposits, not a promise against native flood/terrain removal.

Direct composition: **S105 Permeable Terrain** owns lifetime scaling; scattered sound is not the
fluid sound, so Other filth lifespan applies once. Renewal/mining thickening retains the resulting
duration. **S024 Common Sense** and ordinary cleaning may target this real filth without a second
cleaning implementation. **S008/S021/S033/S060/S061/S082/S097** designation/hauling/search/construction/
removal/upgrade/mining owners retain their native physical actions; no duplicate quantity/index or self-patch.
Future rendering/data reuse must preserve actual storage providers, not assume a raw grid position
represents their item layout.

## Actual external and saved contracts

Bounded installed `.cs/.xml/.json/DLL` byte-text search found only source files and three metadata
files: Workshop database, replacement catalogue and community load rules. No installed code/Def
consumer of RandomDraw/Graphic_RandomSpread was found beyond the source, with the usual reflection,
encoding and noninstalled-consumer limitations. The database lists translation package IDs, not
proof those translations are active or need runtime aliases.

The community rule says `zhrocks11.stackablechunks` should load before `owlchemist.scatteredstones`
because the opposite order loses stacking behavior. Treat it as evidence to inspect renderer/data
composition, not verified current1.7 support. The replacement catalogue maps original2598436855 →
continued3278176206 → current3528503211, all with Owlchemist.ScatteredStones. No package spoof or
generic facade is selected. Source active means owned overlap inactive with a bounded warning.

Saved source data is a concrete contract: Owl_Filth_Rocks, ScatteredStones.Rocks/color, map component
ScatteredStones.MapComponent_ScatteredStones/applied and native Filth fields. Its global settings
also retain the six source keys. A new owned Def/class does not automatically read them. **SS-Q3**
retains practical alias/migration investigation, collision handling and a separate transactional
spec before any conversion is offered. Do not silently equate add/remove marketing with lossless
replacement, or claim the full megamod can be removed from arbitrary saves safely.

## Semantic playability and acceptance state

Static XML comparator inputs, not finalized loaded-Def measurements:

| Comparator | Cleaning work / lifetime | Relevant room / physical properties |
| --- | --- | --- |
| Core dirt |35 /45–50days | Base beauty-15, outdoor-1, cleanliness-5; rain-washed, trackable |
| Core rock rubble |35 /45–50days | Base beauty-15, outdoor-4, cleanliness-5 |
| Source rocky debris |240 /60–90days | Beauty-10, outdoor0, cleanliness-2; no tracking/rain thinning; max thickness1 |
| Core stone chunk | Not cleanable filth | Separate beauty-8, outdoor0, cleanliness-2; HP300, pathCost42, stackLimit1 before optional changes |

One debris patch costs about6.86 times the base work of one dirt/rubble layer. Its room contribution
is additional to the chunk's; this is a real maintenance trade-off. Keep source-derived values as
explicit draft choices and measure finalized native work/room outcomes before accepting balance.
No output/resource/research/economy change is selected; graphics do not reduce hauling mass/capacity.

| ID | Scope | Remaining work |
| --- | --- | --- |
| SS-Q1 | Effective targets | Beta XML/render/yield/expiry semantics, guards, atomic groups and later invalidation |
| SS-Q2 | Visual/providers/balance | Measured source/Core brief, owned art, storage/stack/texture contracts, costs and finalized cleaning/room outcomes |
| SS-Q3 | Legacy saved contracts | Practical alias/migration options and explicitly transactional supported conversion if selected |
| SS-Q4 | Verification | Focused TDD, common settings/languages, causal reviewed-build native evidence and independent context-free visual review |

Independent behavior/composition and provenance reviews found no issues. Strict typed validation
passed 134 items with zero failures. The validator required SHALL on SS04's first wrapped line;
the opening was rephrased without changing behavior and independently rechecked. The ledger records
the final checkpoint.
All implementation/asset/live tasks remain unchecked. No runtime code/tests/build/install/game launch,
normal save/config mutation, source-content edit, image selection, commit or publication in this draft.
