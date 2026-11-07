# S105 — Permeable Terrain research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
Origin replacement **S105/P2-35**. Inspected 2026-11-05. The handwritten “optimize?” is a research
question, not an established performance fault. [Coverage](../replacements/permeable-terrain.md),
[proposal/design/tasks](../../../../openspec/changes/specify-megamod-terrain-filth-lifetimes/) and
[observable contract](../../../../openspec/changes/specify-megamod-terrain-filth-lifetimes/specs/megamod-terrain-filth-lifetimes/spec.md)
separate evidence from intended behavior and technical implementation. Nothing is implemented.

## Provenance and inspection order

- Read [Continued Workshop3261312042](https://steamcommunity.com/sharedfiles/filedetails/?id=3261312042)
  first. FerrisCG maintains the continuation; About still names Owlchemist/Permeable Terrain and
  package **`Owlchemist.PermeableTerrain`**. The page explicitly shares that package with
  [original2629981384](https://steamcommunity.com/sharedfiles/filedetails/?id=2629981384).
- Public description promises shorter liquid-filth lifetimes on permeable terrain, creation-time
  rather than continuous work, a second slider for other filth, and no retroactive adjustment of
  existing stains. It documents two extension classes and historical ReGrowth/FSF interactions.
  Its “no performance impact” statement is a source claim, not measured evidence. Comments report
  duplicate oil-smear lifetime XML; inspect the code rather than treating comments as proof.
- Installed root `F:/Steam/steamapps/workshop/content/294100/3261312042`. About supports1.3–1.6,
  requires Harmony and loads after `ReGrowth.BOTR.Core`. There is no loadFolders.xml or1.7 folder.
  Inspected native `ModContentPack.InitLoadFolders` selects highest available version<=current,
  then root: **1.6 + root** on1.7. This selection is not proof the old binary behaves correctly there.
- Selected `1.6/Assemblies/PermeableTerrain.dll`: AssemblyVersion **1.2.0.0**, FileVersion attribute
  **1.2**, InformationalVersion **1.6**, Debug configuration, MVID
  **`87d0ff23-695e-47e1-948e-137148330e54`**, SHA256
  **`BEDAB729C35A0DAEB133A388B28685E6AB169A7DEF1077CD4AA5A2F340F219F0`**.
  The selected Assemblies folder also ships0Harmony.dll; never copy either assembly into the product.
- [Original author source](https://github.com/Owlchemist/permeable-terrain), revision
  **`491a9de848f453fedc39ebb02e8bfe58ba9f3812`**, predates the continuation. Read README/LICENSE,
  all three C# files, project, both patches and English keys; decompile the selected continuation
  DLL read-only. Core logic matches; continuation UI adds named tooltip arguments and the project
  changes references/output/version for1.6. Do not present the original revision as the continuation's
  exact build revision.
- Original public LICENSE is **MIT**, with copyright **2020 jptrrs**. Retain that actual license
  attribution and Owlchemist/FerrisCG provenance. The installed continuation has no separate LICENSE
  in the inspected file inventory. No license conclusion about unrelated bundled binaries is needed:
  independently implement behavior and copy no source, binary or raster into the product.
- Native **1.7.4998 rev1299**, Assembly-CSharp SHA256
  **`FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B`**. Read relevant native
  `Filth`, `FilthMaker`, `FilthProperties`, `SteadyEnvironmentEffects`, `TerrainGrid`, `LiquidFuel`,
  `ModContentPack` and `PatchOperationFindMod` decompilation and Core terrain/filth XML.

Ignored evidence: `artifacts/Refinement/megamod-spec-20261105/permeable-terrain/` contains public
checkout, complete selected DLL decompilation, native/provider method extracts,25-input source
manifest and6-input native manifest. Hashes bind evidence; runtime compatibility needs semantic
predicates. No provider code was run or Workshop file edited.

## Source behavior inventory

`Mod_PermeableTerrain` immediately installs a Harmony PatchAll under its package ID and loads
global settings. Its only gameplay patch is a `Filth.SpawnSetup` postfix:

1. Return if `respawningAfterLoad`.
2. Read the already assigned `disappearAfterTicks` and `Map?.terrainGrid.TerrainAt(Position)`.
3. If the filth has `Liquidy` and the terrain has `Permeable`, write integer(duration*terrain.value*
   modifier); otherwise integer(duration*otherModifier).
4. Optionally log the before/after duration in integer days. No Tick method, timer manager or map
   index exists. It makes a terrain lookup and extension lookups at creation; the cost is not literally
   only a multiplication, but no measured performance defect is established.

The source settings are static globals stored by `ModSettings_PermeableTerrain`:

| Field/key | Default | UI bounds | Actual effect |
| --- | --- | --- | --- |
| `modifier` |1 |0.25–3 | Multiplies terrain-specific lifetime fraction for liquid+permeable only |
| `otherModifier` |1 |0.01–3 | All other combinations, including liquid on a sealed floor |
| `logging` |false | Developer-mode checkbox | Transient, not scribed; logs every non-load spawn without a bound |

No master toggle, reset, preset, save component or migration exists. Settings labels mention
percentages but append a rounded raw multiplier (e.g.1), which is ambiguous beside100%. Runtime
uses the globals directly; malformed persisted floats are not validated by the postfix.

Source UI is only a mod-settings page with two sliders and a developer-only logging checkbox.
No bottom tab, bottom-right button or entity gizmo exists. Developer mode at construction queues a
one-time report listing labelled terrain definitions with a Permeable extension. English has four
keyed strings; debug text and settings title are hardcoded. Our common UI replaces this with seven
languages, explicit percentages, global/save/Use global, future-only help and top-toolbar diagnostics.

## XML classification and compatibility defects

`patch.owlchemist.permeableterrain.xml`:

- Adds `PermeableTerrain.Permeable(value=0.5)` to Ice, then value0.1 to any TerrainDef containing a
  `takeFootprints` node or named StrawMatting/PackedDirt. Ice itself has takeFootprints=True in Core1.7,
  so two same-type extensions can be appended. `GetModExtension` consumes one; owned precedence
  must explicitly select ice0.5 once instead of depending on order/duplicates.
- The XPath tests node presence rather than boolean true. A false value still matches; inheritance
  and another provider's explicit classification can also interact with raw XML. Owned rules use
  finalized supported traits and unique metadata, preserving unrelated extensions.
- For filth whose raw label equals `cum`, `mixture` or `water`, it appends the fluid cleaning sound
  without checking for an existing sound. This is language/name-dependent classification and can
  create duplicate nodes or change audio for unrelated content.
- Adds `PermeableTerrain.Liquidy` to filth using `Interact_CleanFilth_Fluid`. Membership matters;
  `Liquidy.value` exists but is unused. The category includes machine bits as well as liquids.

Bounded installed exact-label searches found the ReGrowth water definitions as the relevant filth
matches. Other `water` results were items/buildings/precepts/terrain-affordance definitions, outside
the source's filth predicate. No installed `cum`/`mixture` filth match was established. Do not carry
the unowned label guesses into guarded product XML; additional real cases remain PFT-Q2.

`patch.regrowth.botr.core.xml` uses vanilla display-name FindMod for **ReGrowth: Core**, removes
BaseFilth's inherited duration, adds45–50days to rock/building rubble, and adds35–40days to Ideology
oil smear. Building rubble is repeated in the selector, leaving slag omitted. Operations add without
checking an existing child; the oil-smear duplicate report is consistent with the inspected XML.
This historical compensation is deliberately not reproduced. Current providers' aging/rain
choices stay authoritative; our narrow mapping only classifies genuine supported puddles.

## Native lifecycle facts and numerical boundaries

`Filth.SpawnSetup` assigns growTick and a sampled lifetime from `disappearsInDays *60000` only if
neither loading nor `BeingTransportedOnGravship`. The source's postfix ignores the transport guard,
so it can repeatedly shrink an existing duration on transport. Our hook must prove an actual fresh
assignment, including skip-original/overrides; flags alone are insufficient.

`Filth.ExposeData` saves growTick, thickness and disappearAfterTicks. `ThickenFilth` resets growTick
and increments thickness under native limits but does not resample the lifetime. `FilthMaker` merges
new filth into an existing same-type stain when possible and only otherwise creates/spawns a new
object. Source metadata and ordinary spread/placement remain native.

`SteadyEnvironmentEffects` cycles through ceil(map area*0.0006) cells per simulated tick. For
eligible cells it first permits rain thinning when unroofed/rain-washable, then destroys a stain
when duration!=0 and age since thickening>duration. Thus0 means **no age expiration**, not immediate
cleanup. Positive products truncated to0 can accidentally become permanent in the source. Owned
results preserve ordinary0, otherwise floor to whole ticks with minimum1; invalid/unrepresentable
arithmetic leaves the ordinary duration unchanged. Do not promise disappearance at an exact tick:
the native environment check and other removal mechanisms remain authoritative.

`TerrainGrid.TerrainAt` uses temporary surface, then exposed foundation where appropriate, then
top terrain (with its native fallback). This is not a buried-soil lookup. `FilthMaker` separately
uses foundation/terrain acceptance, roof/room and vacuum conditions. Our feature modifies no
placement probability or acceptance mask, and never creates a rejected deposit.

Core `LiquidFuel` inherits Filth but has independent drying after its1500tick threshold and a
refill/thickening path. Shortening its ordinary filth age limit must not suppress or reinterpret
that faster mechanism. Roofs/weather do not affect permeability selection, but normal rain cleanup
and other provider adjustments remain active after initial scaling.

Static balance comparators; finalized/native acceptance still required:

| Type / surface | Ordinary native input | Owned default age limit if positive | Other properties retained |
| --- | --- | --- | --- |
| Blood/vomit on soil or sand |35–40days |3.5–4days | Cleaning work70, ordinary rain/beauty/cleanliness |
| Blood/vomit on ice |35–40days |17.5–20days | Ice fraction0.5 despite footprints |
| Blood/vomit on sealed floor |35–40days |35–40days | Other multiplier1; normal cleaning |
| Firefoam on permeable ground |5–10days |0.5–1day | Native fire prevention while present |
| Water puddle on permeable ground |0.2–0.4days |0.02–0.04days | Ordinary acceptance and other removal |
| Dirt or animal filth |45–50days |45–50days | Not automatically liquid; Other multiplier1 |
| Machine bits on permeable ground |45–50days, fluid cleaning sound |4.5–5days | Retains source's broad classification |
| Blast mark | No natural interval; default0 | No age expiration | Cleaning and other removal remain possible |

Permeation25–300% makes ordinary0.1 surfaces use2.5–30% of base and ice use12.5–150%; Other1–300%
can make unclassified filth very short-lived or longer-lived. These are source-derived bounds, not
new balance tuning. Faster firefoam disappearance and easier soil/straw cleanliness need actual
playability review; no new resources, research, jobs, yields or economy loops are introduced.

## Optional inputs and consumer contracts

| Provider | Inspected evidence | Composition and limits |
| --- | --- | --- |
| ReGrowth2, `ReGrowth.BOTR.Core`,2260097569 | About5.3-rev3 supports1.7; loadFolders selects1.6; Core-Filth.xml adds BaseFilth14–15days/rain, removes selected rubble intervals, preserves oil interval while changing rain | Preserve current finalized durations/rain; omit legacy base removal/range restoration |
| ReGrowth puddles | `RG_FilthWater`, `RG_FilthWaterSpatter`, class`ReGrowthCore.PuddleFilth`, ordinary0.04166–1day, no explicit fluid sound | Explicit package/content/shape-gated liquid mapping replaces source label/sound mutation |
| FSF Filth Vanishes With Rain And Time,1508341791 | `FrozenSnowFox.FilthVanishesWithRainAndTime`, selected1.7 XML replaces lower-bound>=15 ranges with14–15days and modifies rain with exclusions | Apply one multiplier to current baseline; published250% advice is not an automatic setting |
| Dubs Bad Hygiene,836308268 | About3.2.3260, selected1.7 `FilthUrine`, `FilthFaeces`, `RawSewage`:10–15days, no fluid sound/extension in inspected data | Default Other branch; no invented liquid compatibility from names/comments |

ReGrowthCore.dll: AssemblyVersion0.0.0.0, MVID`7dee9688-ae56-49f3-9181-3973f44b3163`, SHA256
`C6D4478364A87B98E7B93351FE3A2BF7F42F1325D6193EB28F62EA582010EC92`.
Read `PuddleFilth.TickLong`: rain>0.1 adds2000ticks to its current duration. Owned initial scaling
must preserve that later extension. Native FindMod checks display names, so the old ReGrowth: Core
condition does not establish support for the installed ReGrowth2 title. Package-ID/shape gating and
focused live runs are still required. The distinct FSF Tweaks variant is not covered by inspecting
the standalone filth mod.

The public extension contract is `PermeableTerrain.Permeable : DefModExtension` with float`value`,
and `Liquidy : DefModExtension` with an ignored float`value`. Documented external XML uses
`MayRequire="owlchemist.permeableterrain"`; a same-named class alone cannot satisfy that package
condition. Public settings and Mod classes also exist, but bounded installed XML/C#/DLL scans and
local Mods searches established no external runtime consumer. This does not prove all reflective/
UTF16/future consumers absent. No speculative facade, package spoof or config import is planned.
Own explicit declarations need a documented contract when implemented; actual external consumers
must be rechecked before claiming drop-in compatibility.

Original and continuation share one package, so suppress the full owned overlap if either is active.
Do not unpatch either or rewrite stored durations. Optional XML uses XML Extensions package/content/
node guards and unique additive metadata; no whole-list/category replacement. Generic valid
content traits remain usable without enumerating every provider package.

## Intentional differences and remaining work

| Difference | Reason |
| --- | --- |
| Actual fresh-assignment hook, including gravship guard | Avoid repeated scaling of native saved/transported duration |
| Positive minimum1tick and finite arithmetic | Prevent tiny positive lifetimes becoming permanent or overflowing |
| Finalized true footprints and explicit ice precedence | Avoid presence-only/duplicate-extension classification |
| Current provider baselines and targeted puddle mapping | Preserve ReGrowth/FSF choices; remove legacy destructive compensation and guessed-label sound changes |
| Common global/save settings and explicit percentages | User contract; future-only effects and invalid-entry fallback are visible |
| Developer-toolbar report and transient trace-next20 | Move debugging out of gameplay settings; replace automatic list/unbounded per-spawn logging |
| No private filth cache or new tick loop | Source already acts at creation; measure before claiming improvement |

| Question | Type | Remaining work |
| --- | --- | --- |
| PFT-Q1 | Runtime admission | Exact effective assignment/transport/consumer paths, provider subclasses, XML completion and coherent failure groups |
| PFT-Q2 | Optional contracts | Proven external consumers, further label-only cases, FSF Tweaks and finalized provider combinations |
| PFT-Q3 | Performance/balance/UI | Measure whether optimization is warranted; actual terrain/filth/firefoam comparisons and seven-language layout |
| PFT-Q4 | Acceptance | Focused nonzero TDD, reviewed implementation and causal native creation/aging/cleaning/rain/thickening/save/transport/provider evidence |

No implementation or live acceptance is claimed. Native evidence must come from the reviewed build,
real player creation/cleaning/transport actions, personally inspected action/result views and the
root isolated-run identity/configuration/cleanup requirements. Writing a timer or constructing an
already-expired stain is supporting setup/diagnostics, never the acceptance proof.
