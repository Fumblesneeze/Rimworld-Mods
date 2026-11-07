## Context

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, **1.7 beta**.
S112 replaces Scattered Stones; no runtime implementation or asset is delivered by this change.
[Research](../../../mods/megamod/docs/refinement/2026-11-05-scattered-stones-research.md) records
the complete installed1.7 binary/XML, older public source, native1.7.5000 paths and actual consumers.

The source combines a custom chunk graphic, new colored Filth subclass, one-time map seeding, four
Harmony patches and six global settings. The default debris affects room stats and cleaning despite
the Workshop's graphical framing. Source1.7 lacks the older public source's mesh-refresh call after
settings changes. Existing graphic ownership and source save types must be treated as real contracts.

## Goals / Non-Goals

**Goals:** stable varied ground chunks; native storage presentation; one cleanable patch per cell;
accurate deposits/lifetimes; common settings; guarded XML/runtime integration; direct S105 composition.

**Non-Goals:** altered chunk gameplay or yields, periodic landscape redecoration, decorative trails
from hauling, guaranteed compatibility with every renderer, copied artwork/binaries, speculative
source API facades or an unverified legacy-save migration/uninstall-safety claim.

## Decisions

### 1. Guarded declarative graphics, dynamic behavior only where needed

Chunk graphic declarations and owned debris properties belong in XML, not runtime Def rewrites.
Use XML Extensions to establish required native content/nodes and compatible graphic shapes before
changing them. Add **imranfish.xmlextensions** as a required dependency with first usage, plus Harmony
when its types are used. No bundled dependencies and no product Gateway reference/load hint.

Inspect finalized inheritance when deciding coverage. Source XPath selects only immediate
`ParentName=ChunkRockBase` children and removes/readds graphicClass; its separate runtime category
set is broader, so advertised category-wide support does not prove graphical coverage. Prefer
guarded native-base/admitted-child transformations that preserve child overrides, texPath, shader,
color, draw size, shadow, other extensions and category membership. Replacing the specific admitted
graphicClass is intentional; replacing the whole graphicData, category or modExtensions list is not.
Add owned extension entries once. Do not use Success=Always to hide failed changes.

Static eligibility includes finalized StoneChunks membership (including inherited category membership)
and compatible ordinary graphics; do not hardcode the five Core stone names. Natural cliff support
requires isNaturalRock and not isResourceRock. Steel slag, ore and blocks retain their own behavior.
Foreign declared renderer or storage ownership needs an actual adapter; otherwise preserve it and
report the skipped enhancement. Named optional XML content requires `XmlExtensions.FindMod` with
packageId=true plus required Def/node shape. A package ID alone is insufficient.

Owned debris has its own stable Def/class identity and source-derived properties from SS05. Keep
filth placement semantics explicit and do not bypass `FilthMaker` checks merely because the source
uses direct `GenPlace`. A narrow admitted rock-underlay placement may be necessary; it must preserve
terrain/water support, bounds, one-patch policy and ordinary unrelated object replacement.

### 2. Deterministic scatter from identity, not a static lifetime dictionary

Source graphic caches four floats per Thing ID on the Graphic object, seeds every draw parameter
from the same position hash, and writes zero offsets into the cache in Building_Storage. Both offset
axes consequently match; moving through storage destroys the cached ground offset, while reloading
can reseed moved chunks. The dictionary is not explicitly cleared on map/game teardown.

Use stable Thing identity plus distinct local channel salts for scale, rotation and the two offsets.
Use pure deterministic local math, not advancing the simulation RNG. Reuse normalized samples as
settings change; do not resample from current cell or mutate shared graphic defaults per save.
No persistent per-Thing scatter state/cache is necessary for these cheap calculations. Item identity
changes due to native splitting/merging can create a new visual identity without altering quantities.

Base owned ranges: scale0.82–1.28, rotation full turn, offset each axis ±0.27 cells. Three common
percentage settings scale the corresponding bounds; upper size is max(effective minimum,effective
maximum). Keep native texture/tint/atlas/shadow semantics and actual extra rotation where required by
the admitted draw route. Leave unsupported renderers alone rather than replacing every Graphic_Random
subclass. Ground offsets do not change Thing position, path cost, selection or actual job targets.

Storage building/provider item layout takes precedence over all ground transforms, an intentional
correction from source's offset-only suppression. Ground stockpiles retain scatter. Held/inventory
paths remain native. Returning to ground recomputes the same underlying samples, without sticky
centering. On appearance change, dirty relevant rendered sections on all loaded maps in the active
game once; avoid a per-frame whole-map refresh. Main-menu global changes touch no implicit game.

### 3. Deposit events, one-time initialization and bounded local maintenance

Use current native listers and grid data. For first admitted map initialization, snapshot eligible
source identities before creating anything so collection mutation cannot change iteration. Seed
unstored chunks and exposed non-resource rock cliffs once; cliffs must be unfogged, off the map edge
and touchable from at least one in-bounds cardinal standable approach. Unlike source ValidateCell,
count accessible cells rather than blocking Things and do not reject an entire cliff merely because
one neighbor is water. Creation on the actual water cell respects the common water choice and
ordinary terrain support. Initial chunks in any native/admitted storage are excluded.

Use one map-owned initialization marker. Reentry/load cannot reseed; cleaned cells are not rebuilt by
rendering or ordinary hauling. No new per-cliff history system for repeatedly decorating fresh faces
is selected. On initialization failure, roll back only patches newly created by that attempt, do not
mark success, and retain unrelated/preexisting objects. Existing owned patches are not thickened just
because initialization saw them. Exact resume/failure behavior must be tested before implementation.

Dirty mining must follow an actual newly created stone yield and its successful destination(s).
The source postfix scans the old mineable cell for any chunks: this can miss a Near placement or
mistake preexisting chunks for yield. Native1.7 TrySpawnYield(Map,bool,Pawn) creates an item and uses
GenPlace.TryPlaceThing(...Near,callback); an obsolete four-argument wrapper delegates to it. Design
the smallest semantic hook exposing the created Thing and actual placement receipt, without a
global before/after map scan or duplicated yield execution. An actual non-mining destruction yield
through the same route also participates; an ore-only or failed yield does not. Respect yielded
forbidden state/count and storage/water rules. One event/patch, no stale global current-mineable field.

Refresh age of existing owned debris at a successfully mined cell, independently of Dirty mining.
Capture only required pre-action identity/map data and verify real completion; source's unconditional
DestroyMined prefix is not a sufficient success contract. Preserve lifetime and maxThickness1.
Deposit color comes from the actual source; a genuine later deposit may update it, drawing never does.
Choose a stable source identity for initial same-cell ties, not enumeration/RNG order.

After completed relevant construction, examine only the affected footprint/neighborhood. Remove an
owned patch fully buried beneath an obstacle without cardinal cleaning access; do not use a pawn's
allowed area/reservation or whole-map reachability as permission to erase dirt. No unrelated filth
cleanup and no attempted-cleaning retry loop. Snapshot any collection before removals. Native data
or the existing shared map index is sufficient; no ScatteredStones-specific Thing scan/cache service.

### 4. Natural expiry protection must not intercept all destruction

Use ordinary Filth persistence and cleaning. Base lifetime60–90days is sampled once by native
creation, maxThickness1, cleaningWork240, Beauty-10/BeautyOutdoors0/Cleanliness-2, rainWashes=false,
canFilthAttach=false. A minimal owned class can hold tint; do not copy the source's general Destroy
veto. Native ThinFilth decrements thickness then calls Destroy at zero. The source age test can
re-thicken at that point, cancelling completed cleaning, or veto unrelated removal when old enough.

Admit an expiry-specific decision in the actual native age-removal path. In inspected1.7,
SteadyEnvironmentEffects.DoCellSteadyEffects checks duration !=0, age>duration and !Destroyed after
possible rain thinning. If the owned patch is still valid, persistence is enabled and a qualifying
rock occupies the same cell, restart age without resampling/multiplying lifetime. Every other removal
path remains ordinary, including construction, flood/terrain rejection and explicit cleanup.
If the expiry integration cannot be admitted, retain ordinary aging with a bounded warning.

Compose directly with S105. The scattered cleaning sound is non-fluid, so Other filth lifespan
applies once at the actual native fresh lifetime assignment. Do not patch our own Filth subclass to
repeat that multiplier. Load and gravship transport keep stored duration; thickening and persistence
renewal reset age only. No separate debris tick scanner. Stored zero/native permanent or changed
provider lifetimes need the same S105 numeric truthfulness; do not invent a new sampled duration.

### 5. Shared settings and saved-data boundaries

Six descriptors under Environment and cleaning / Scattered stones: minimum and maximum size100%
(50–200), offset100%(0–300), water true, covered persistence false, dirty mining true. Percentage
editors use1% steps; source exposes continuous floats with two-decimal labels. This is an explicit
readability choice, not a claim about source storage. Common load/store/resolve/render handles global,
current-save, Use global, dormant overrides, invalid-default recovery and main-menu callback guards.
No scope exemption or separate settings implementation. Appearance values refresh visible maps;
water/mining affect future deposits; persistence affects later age-removal decisions. None reseeds.

Own saved state: map initialization marker, debris's native age/lifetime/thickness and owned tint.
Do not save live Def indices, caches, transform samples or shared graphic instances. Rebuild static
classification on Def/admission generation changes. Clear any transient rendering work on game/map
teardown; pending refreshes must not affect another save. All seven baseline languages are required.

Proven legacy source contracts include Owl_Filth_Rocks, ScatteredStones.Rocks/color, and
ScatteredStones.MapComponent_ScatteredStones/applied plus native Filth fields. Removing the source
does not automatically bind those names to our new types. Preserve these as a concrete migration
research obligation SS-Q3, not an invented runtime consumer or a silently accepted data loss. Before
any legacy migration is offered, specify an explicit transactional workflow, exact supported schema,
backup, duplicate-provider handling, atomicity/restoration, preserved native ages/tint and verified
postconditions. No such conversion or source-settings import is implemented/claimed in this draft.

Original-provider co-installation is detected by shared package ID Owlchemist.ScatteredStones:
disable owned graphic changes/new deposits/enhanced persistence, preserve existing owned debris's
ordinary cleanup, and do not mutate source objects/settings. No package/assembly impersonation.
Actual installed consumer search found only metadata, translation catalogue entries and a community
Stackable Chunks ordering rule. That rule describes an interaction, not a demonstrated current
assembly consumer or verified adapter. Custom/stack-count graphics remain explicit compatibility work.

### 6. Per-target semantic admission and isolation

Before implementing any patch, create its stable-ID document from the
[target template](../../../mods/megamod/docs/patch-targets/TEMPLATE.md), linked to SS01–SS08,
executable predicates, honest tests and later native/visual evidence. Candidate obligations:

| Boundary | Required semantics | Fallback |
| --- | --- | --- |
| XML chunk graphic / extension | Required content and compatible inherited graphics; source absent; unique owned entry; unrelated graphic fields/categories retained | Prior renderer, bounded warning per skipped enabled patch |
| Render/mesh refresh | Actual ground versus storage/held route, texture/tint/atlas, transform/selection behavior and current save scope | Native/provider presentation |
| Map first initialization | Current valid map/lister/fog/storage/terrain contracts; idempotent marker and safe partial-failure ownership | No unsafe new debris |
| Actual yielded placement / successful mining | Exact created stone and successful native placement receipt; original execution and completion; no duplicate wrapper interception | Ordinary yield, skip owned deposit/refresh |
| Rock-over-debris coexistence | GenSpawn.SpawningWipes result for owned filth and qualifying rock only, including ignoreDestroyable meaning | Disable dependent debris group if it cannot coexist safely |
| Local construction cleanup | Completed relevant spawn, actual occupied/adjacent cells and geometric access; exclude respawn/load/bootstrap ambiguity | No unsafe additions requiring unsupported cleanup |
| Natural expiry | Positive age-limit removal of owned living filth, after any prior removal; no interception of cleaning/terrain destruction | Ordinary aging; existing debris stays cleanable |
| S105 assignment | Existing admitted one-time fresh lifetime rule | Preserve its own failure policy, no second local hook |

No exact-version/hash gate or blanket rejection of other Harmony owners. Check original versus
effective code, skip-original/result-changing patches, actual argument use and required data-flow
cardinality. Each transpiler validates its actual incoming stream on every run and returns it
unchanged on failure; never execute foreign transpilers speculatively. Document later-repatch
invalidation/revalidation and stop only affected owned work when prerequisites change.

One guarded idempotent startup. Declare atomic groups before code: scattering, safe base debris
creation/maintenance, and optional natural-expiry protection can fail separately only if the
remaining behavior is safe and truthful. False/unknown checks, validator exceptions or installation
failures leave the affected patch/group inactive; roll back only newly installed owned members.
Log one bounded startup warning per skipped enabled patch with package/patch/spec ID, target,
reason, unavailable behavior and fallback. Optional absence is quiet. Repeated jobs/frames do not spam.

### 7. Visual and playability verification are separate from code correctness

Apply the asset skill before any graphic declaration/art selection. Measure Core stone chunk,
native rock rubble and source debris as structural/conceptual references; source's drawSize4 and
three OwlRocks variants are evidence, not an approved owned canvas or copied asset choice. Retain
source pixels only as ignored inspection evidence. Define a measured owned brief before generation:
alpha bounds, pixels/world cell, low ground projection, tint response, outlines, overlap/altitude,
selection readability and useful close/ordinary/far zoom. Do not infer acceptable sizing from XML alone.

Final native workflows must actually mine, haul into/out of storage, clean and construct. Personally
inspect matching screenshots on the reviewed build with representative granite/marble/other tints,
texture replacements, fog, materials, multiple zooms and supported storage views. Apply damage-state
checks where the actual graphic can show damage. A context-free independent visual reviewer receives
only live captures, not this diagnosis or expected identity. No artwork is accepted by this document.

The semantic comparator pass retains source cleaning240 versus Core dirt/rubble35, and source
beauty/cleanliness alongside the chunk's separate native contribution. That is substantial extra
cleaning effort, not cost-free decoration. Source-derived balance is provisional pending measured
finalized Defs/native outcomes. No new resources, recipes, research, yields or profitable loops.

## Risks / Trade-offs

- Debris adds work and room penalties → state them explicitly and measure cleaning/travel outcomes;
  do not silently convert it into pure decoration or claim negligible performance from marketing.
- Strict one-patch and true-yield rules differ from source's broad scans → retain source provenance
  and test same-cell stacks, nearby placement, failed yields and real destruction independently.
- Rendering adapters can break specialized storage/stack graphics → preserve their owner and admit
  narrow actual contracts, with custom surfaces and legacy saves tracked rather than overclaimed.
- New saved debris can outlive enabled generation → keep native cleaning/aging available; distinguish
  disabling an enhancement from removing the whole package or migrating unknown source content.

## Migration Plan

This change is docs only. First future implementation adds required dependency metadata and guarded
owned content. Existing native maps receive the specified one-time admitted seeding, without changing
chunks or foreign debris. Source settings/saves are not edited by this draft. Legacy source conversion
needs SS-Q3's separate transaction contract before being offered; whole-mod removal is not claimed safe.

## Open Questions

- **SS-Q1:** Current effective beta render/yield/expiry/XML contracts and precise atomic groups,
  including original-versus-effective patches and later invalidation.
- **SS-Q2:** Measured reference/owned-art brief, storage/stack/texture provider contracts, combined
  rendering/deposit cost and finalized cleaning/room balance.
- **SS-Q3:** Proven source Def/type/map-state bindings, practical migration/alias options without
  co-installed collisions, and a separately specified transactional migration if selected.
- **SS-Q4:** Focused TDD, seven-language settings, native action/result evidence and independent
  context-free visual acceptance on the reviewed build. No interview is needed now.
