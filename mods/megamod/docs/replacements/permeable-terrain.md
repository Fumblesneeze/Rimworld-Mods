# Permeable Terrain / Continued

## Identity and provenance

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
Origin replacement **S105/P2-35**. Source package **`Owlchemist.PermeableTerrain`**;
[installed continuation3261312042](https://steamcommunity.com/sharedfiles/filedetails/?id=3261312042)
by FerrisCG retains original Owlchemist metadata/package. Inspected2026-11-05. Native1.7 fallback
selects1.6+root, despite About support ending at1.6. Selected DLL AssemblyVersion1.2.0.0,
FileVersion attribute1.2, InformationalVersion1.6, MVID`87d0ff23-695e-47e1-948e-137148330e54`, SHA256
`BEDAB729C35A0DAEB133A388B28685E6AB169A7DEF1077CD4AA5A2F340F219F0`.

[Original source](https://github.com/Owlchemist/permeable-terrain) revision
`491a9de848f453fedc39ebb02e8bfe58ba9f3812` has MIT LICENSE copyright2020jptrrs; it is not the exact
continuation build revision. Independently implement and preserve attribution; copy no source,
binary, bundled Harmony or art. [Research](../refinement/2026-11-05-permeable-terrain-research.md)
retains exact evidence, source defects, provider boundaries and the open optimization question.

[Proposal/design/tasks](../../../../openspec/changes/specify-megamod-terrain-filth-lifetimes/) and
[observable specification](../../../../openspec/changes/specify-megamod-terrain-filth-lifetimes/specs/megamod-terrain-filth-lifetimes/spec.md)
own all behavior below. **Nothing is implemented or natively accepted.**

## Behavior inventory and coverage

| ID | Player action → observable result | Defaults and edges | Remaining work |
| --- | --- | --- | --- |
| PT-01 | Generate liquid filth on permeable ground → shorter natural age limit | Soil/sand/footprint surfaces, straw, packed dirt10%; ice50%; one selected fraction | Fresh-assignment and current-surface integration |
| PT-02 | Generate supported modded filth/terrain → actual traits or explicit mapping apply | Inherited fluid family, broad source classification, no label guessing; invalid mapping leaves ordinary duration | Finalized classification and gated XML |
| PT-03 | Change multipliers → future assigned lifetimes use exactly one branch | Permeation25–300%, Other1–300%, both default100%; zero means no age expiration | Common rules, safe arithmetic and boundary tests |
| PT-04 | Reload, transport, thicken or replace floor → stored duration retained unless truly reinitialized | No retroactive rescale; ordinary thickening restarts age; independent cleanup still works | Native lifecycle and Odyssey workflow |
| PT-05 | Configure a colony → common global/save choices | Use global default, dormant overrides, main-menu disabled save callbacks, future-only help | Common settings/persistence/UI |
| PT-06 | Use ReGrowth or FSF → current provider baseline/rain preserved | Explicit ReGrowth puddle classification; Dubs unclassified filth uses Other; no automatic250% preset | Focused provider matrices and public consumer evidence |
| PT-07 | Request developer report/trace → bounded diagnostic evidence | Top toolbar only, trace next20, transient/off by default; seven languages | Native entry points and readable fractional durations |
| PT-08 | Co-install original or encounter incompatible shape → no duplicate/unsafe scaling | Shared original/continued package owns overlap; coherent per-patch admission/fallback | Target specs, tests and reviewed native acceptance |

Every row is specified but unimplemented. Research and the observable spec link the evidence and
scenarios; implementation, target documents, tests and native evidence remain open.

## UI audit and settings

| Source entry point | Purpose | Owned destination / intentional change |
| --- | --- | --- |
| Two mod-settings sliders | Gameplay configuration | Common Environment and cleaning section, percentages and global/save/Use global |
| Developer checkbox in mod settings | Debug trace | Top developer toolbar, transient next20 cap rather than unbounded logging |
| Automatic startup report in developer mode | Debug terrain classification | Explicit top-toolbar report; no automatic full list |

No bottom tab exists in the source; add no bottom tab, entity gizmo or bottom-right switch. Per-stain
age/duration remains gameplay state, not an inherited setting. Main-menu save controls and callbacks
are inactive. Seven baseline languages, narrow layouts and diagnostic visibility need native proof.

## Contracts consumed by other mods or saves

| Evidence | Contract | Decision / gap |
| --- | --- | --- |
| Public modder documentation and own XML | `PermeableTerrain.Permeable.value`, `Liquidy` membership; legacy MayRequire package gate | Own equivalent declarations need their own contract; no proven foreign caller, facade or package spoof |
| Source global ModSettings | `modifier`, `otherModifier`; static transient `logging` | Common owned settings; no silent import or per-save reinterpretation of source config |
| Native saved filth | growTick, thickness, disappearAfterTicks | Preserve native lifetime/age; no custom Thing conversion or restoration of unknown original samples |
| ReGrowth puddle subclass | Native Filth creation plus later rain extension | Keep provider behavior; explicit package/content/shape mapping, no runtime assembly dependency |
| Bounded installed/local consumer search | Only source declarations found | No complete reflection/UTF16/future-consumer absence claim; PFT-Q2 remains open |

## Composition, deviations and omissions

Owned vomit, cleaning, construction and fire features retain their ordinary actions and share the
native filth lifecycle. No self-patches, private Thing index or extra decay loop is introduced.
“Optimize?” remains a measurement task: the source already does its work on creation, and no
performance claim is accepted without a representative comparison.

Deliberate corrections: guard the actual assignment including gravship transfer, retain positive
duration with a minimum1tick, reject unsafe arithmetic, use true finalized footprint traits and one
ice rule, preserve current provider base/rain rules, and classify supported ReGrowth puddles without
changing their cleaning sound. Replace guessed English labels, duplicate data, legacy base-lifetime
removal/restoration and unbounded debug logging. Existing zero-expiration stains stay unchanged.

Original and continuation share a package; either owns the full overlap when active. Optional
providers are absent-safe and shape-gated. FSF standalone evidence does not establish FSF Tweaks
coverage. Dubs urine/faeces/sewage get no invented liquid classification. Legacy package-gated XML
does not become active through an unsupported identity facade. No automatic source settings import,
retroactive cleanup, new resources/research, global Def mutation or general rain-rule replacement.

Harmony and XML Extensions are required when their planned native hooks/guarded XML ship. The source
assemblies, bundled Harmony and developer Gateway never become product dependencies or payloads.

## Verification and acceptance

Both independent document reviews found no issues, including verification of all25 source and6
native manifest hashes. Typed strict OpenSpec validation passes127 items with zero failures;
scoped link/format checks pass. Static research does not accept actual timing, optional compatibility,
transport, cleaning, UI or balance. **PFT-Q1–4** retain the implementation and native gates. All tasks
remain unchecked until focused RED/GREEN, reviewed runtime changes and root native action/result,
build identity, configuration and cleanup evidence exist.
