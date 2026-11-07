## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
Origin is replacement of `zal.bulksmelting`, Workshop `2889803802`. This is design for later work,
not an implemented feature. Read the [proposal](proposal.md),
[observable contract](specs/megamod-bulk-slag-smelting/spec.md) and
[research/provenance](../../../mods/megamod/docs/refinement/2026-11-02-bulk-smelting-research.md).

Installed source 1.0.6 consists solely of an additive RecipeDef in the 1.4–1.6 content folders.
The latest recipe uses `ExtractMetalFromSlagBulk`, three same-kind slag chunks, `Steel=60`, work
1100, `SmeltingSpeed`, `Smelt` and `Recipe_Smelt`, exposed through `recipeUsers=ElectricSmelter`.
Native defaults supply its non-mixing, ordinary worker/count/save behavior. There is no source
assembly, custom record or settings framework to reproduce. The source and XML Extensions both
declare versions only through 1.6; an installed DLL is not beta acceptance evidence.

## Goals / Non-Goals

**Goals:** faithful observable batching, explicit balance, additive guarded XML, native bill identity,
normal player workflows and a documented co-installation boundary.

**Non-goals:** new smelters, research, resources, stone-to-steel conversion, universal bulk recipes,
source-package spoofing, custom work drivers, per-source caches or configurable yield UI in this slice.
No runtime implementation, metadata change or live launch is part of this documentation change.

## Decisions

### 1. Use a fully guarded declarative addition

Keep the recipe in XML and introduce it through one guarded insertion into `/Defs`, with its full
ingredient/fixed filters, product and recipeUsers present together. This avoids splitting one logical
recipe across a partially successful sequence. Preserve the native recipe list; `recipeUsers` is the
ordinary additive route inspected in native recipe discovery. An unguarded Def file would violate
the user's missing-content contract; a runtime Def mutation or new Harmony recipe driver is unnecessary.

The author's docs and installed DLL confirm `XmlExtensions.Conditional` (`xpath`, `caseTrue`,
`caseFalse`), `XmlExtensions.FindMod` (`packageId=true`) and `XmlExtensions.Log` (`warning`). Use the
actual operation shapes, not guessed names. XML Extensions (`imranfish.xmlextensions`, Workshop
2574315206) becomes a required dependency with load order in the implementing slice. Its external
Harmony dependency must resolve normally. Do not bundle either framework. Verify exact beta support
before shipping; no fallback to an unguarded implementation is authorized by a missing framework.

`Conditional` uses SelectSingleNode, so an XPath must select a node with explicit prerequisite/count
predicates, rather than return a boolean scalar or accept an ambiguous duplicate. Construct the entire
candidate before insertion; document and test exception/failure behavior at that boundary. Multiple
operations in a case container are not a proven transaction. Ordinary load-time warnings use the
Log operation without dumping XML. Idempotence and one-warning-per-startup behavior must be proven;
if the standard pipeline repeats, use the common admission diagnostic facility instead of log spam.

### 2. Minimal semantic target, not exact source identity

Before implementation create and index the stable target document, proposed ID
`megamod.bulk-smelting.recipe`, linking tests and the owning scenario. Its predicates must cover:

- one Defs root and an absent `ExtractMetalFromSlagBulk` recipe identity before insertion;
- no active `zal.bulksmelting` or predecessor `neceros.bulksmelting` selected as provider;
- unique required ThingDefs `ElectricSmelter`, `Steel`, `ChunkSlagSteel`, `ChunkMechanoidSlag`, and
  required stat/effect/sound definitions, with usable ordinary station/product/ingredient shapes;
- native interpretation of these inputs as three physical non-Stuff chunks, one fixed steel output
  and ordinary non-mixing bill work/counting, including inherited shapes actually consumed;
- legal recipeUsers/filter/product references and one owned insertion, with the original document
  unchanged on a rejected predicate or failed insertion.

The exact inheritance/shape checks and final-load ordering remain BS-Q3, not a claim that a simple
name-existence XPath proves job semantics. Distinguish raw XML admission from finalized-Def preflight.
Use supported inherited checks when necessary; do not reject valid inheritance simply because a
field is absent from a child node. Guard only behavior needed by this addition. Other recipe entries,
labels and sort order are compatible. Single-output edits are compatible when the bulk outcome can
be derived faithfully; ordinary work edits do not silently rescale the selected bulk work. Do not compare whole files,
hashes, exact versions or require an unpatched game. Artifact hashes identify research only.

Both slag kinds are required for this one recipe group. Missing one skips the addition, rather than
silently changing the published recipe or leaving a dangling reference. A later steel-only fallback
would need its own observable contract. Optional external content is not added in this slice; any
future extension needs package and Def checks and an additive merge, never a broad category replacement.

Later foreign edits can alter our recipe. Do not repair them by continually reapplying our defaults.
Document the supported load boundary and detect unsafe finalized shapes before claiming acceptance;
do not call initial raw-XML admission proof against arbitrary later C# mutation. Any additional
runtime guard required for safe operation must receive its own target spec and admission tests.

### 3. Efficiency-only homogeneous batches

Keep count3, work1100 and non-mixing explicit. Derive the product bundle from three corresponding
ordinary single operations at the supported finalized load boundary; baseline is45 steel. Guard the
consumed ordinary recipe identities, products, quantities and any result modifiers. Both permitted
kinds must have outcomes representable by this one recipe. Do not assume a matching name establishes
output parity or retain60 from the source. When representation or current semantics are unknown,
withhold the owned recipe and warn. Never overwrite the ordinary recipe or repair foreign content.

Prefer guarded declarative calculation/copying through inspected XML Extensions operations; prove
load ordering and finalized outcome parity under BS-Q3. No speculative runtime Def mutation is
selected. If required arithmetic/ordering cannot be established declaratively, that path remains
unsupported until an owned design with executable admission proves the same observable contract.
Keep the explicit1100 work baseline; retain same-kind batches and actual non-Stuff chunk quantities.

### 4. Compose through the ordinary recipe and bill boundaries

The existing Add bill browser displays this recipe through the same catalog, search, categories,
facts and availability projection as other recipes. Eligibility must represent three same-kind chunks;
adding all allowed-kind counts would falsely mark a two-plus-one supply ready. Any shared availability
correction belongs to the owning browser/comparison policy and serves all recipes, not a bulk-specific cache.
Recheck actual ingredient eligibility through the native job path at use time.

One batch completion is one finite iteration. Target counts use Steel stock, including the chosen
linked-storage scope under S005. Whole-list linking S014 shares finite quota and target hysteresis;
this recipe introduces no special per-bench counter. Stock-target overshoot remains ordinary paid
production. Future S017–S019 bill modifiers and S059 Best Mix must compose through their own explicit
contracts; do not retain a source hook stack against our recipe or silently switch mixing modes.

### 5. Keep identity and overlap boundaries narrow

Preserve `ExtractMetalFromSlagBulk` for ordinary serialized recipe references and possible XML
consumers. The bounded installed scan found only metadata catalogs, not behavioral consumers or DLL
bindings. Do not create an assembly facade or spoof either upstream package for an unproven consumer.
Use a canonical-ID provider check even if the active source has an unknown content-folder fallback;
this prevents claiming to replace it while both are enabled. Withhold ours and warn, even if the
source itself fails to provide a usable recipe. Inactive disk copies do not count as active providers.

An unknown existing recipe with the same name is a collision, not permission to mutate or adopt it.
Skip ours, preserve foreign content, and name the collision. Supported ordering must detect current
Def-file collisions; later patch-generated collisions remain a final-load verification case under BS-Q3.
A source-provider diagnostic is one automatic overlap decision; the same feature must not also emit
a second collision warning for that provider. Do not disable/remove other mods or edit the user's list.

### 6. Native persistence, localized contextual UI and honest testing

Use ordinary Bill_Production persistence for colonies begun with megamod. Preserve our bill identity,
choices and counts through ordinary reload. Original-mod save import is outside initial support;
retaining a stable recipe name does not claim that migration has been verified.

Translate label, description and job text in all seven baseline languages. Do not add source previews
or icons. Workbench controls are entity gameplay state, so no mod settings page or new tab is needed.

Host tests may cover pure predicates and package/schema policy without pretending to load Core or
XML Extensions. Real PatchOperations, inheritance, references, provider loading and bill execution
require fresh isolated RimWorld processes. Final reviewed-build acceptance must perform Add bill,
worker collection/work, physical consumption/output, counting and save/reload through native workflows;
finalized-Def reads and logs are supporting evidence only.

## Risks / Trade-offs

- **Output drift** → Derive and guard current piecewise outcomes; reject unsupported shapes rather
  than adding the source yield bonus. Measure actual work and physical outputs before acceptance.
- **Homogeneous batches can wait with three mixed chunks nearby** → Accurate availability display,
  native restriction and BS-Q2 rather than an accidental mixing change.
- **Unproven 1.7 XML Extensions/load semantics** → Required dependency and focused runtime/target
  proof before implementation acceptance; do not infer support from the 1.6 DLL.
- **Stable Def name collides with a source/provider** → Active-provider and existing-identity gates;
  no overwrite, duplicate recipe or broad compatibility claim.
- **Recipe removed after a save used it** → Explicit continuity boundary, retained original save and
  ordinary provider restoration; no unsupported automated migration.

## Migration Plan

No deployment or save migration now. Later add required metadata and guarded XML in the same tested
slice, build the exact allowlisted package, and verify absent/present/collision shapes with isolated
disposable configurations. Initial acceptance uses colonies created with megamod; source-save import is deferred. Rollback
restores the prior package/provider and an untouched save; no migration engine is justified here.

## Open Questions

- **BS-Q1:** yield-neutral output selected; verify current ordinary outcomes and actual efficiency.
- **BS-Q2:** same-kind source batches versus a deliberate mixed-kind convenience; source behavior is proposed.
- **BS-Q3:** beta framework, inherited/finalized shapes, load ordering, idempotent atomic addition and diagnostics.
- **BS-Q4:** native production, saved-bill continuity, overlap/collision and browser/linked-plan acceptance.

Research and decisions remain tracked in the refinement ledger; none require pausing this document pass.
