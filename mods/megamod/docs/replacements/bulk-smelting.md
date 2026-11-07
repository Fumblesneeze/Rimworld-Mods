# Bulk Smelting (Continued)

## Identity and provenance

- Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
- Origin: **replacement**, S015 / P1-12. Source **zal.bulksmelting**, Workshop **2889803802**,
  title **Bulk Smelting (Continued)**, author **Zaljerem**, continuing **Neceros**.
- Inspected 2026-11-02: [Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=2889803802)
  and [change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/2889803802) first,
  then the complete installed XML inventory and native 1.7 behavior for gaps.
- Installed About **1.0.6**, Manifest **1.0.6.0**, supports **1.4–1.6**. Read-only root
  `F:/Steam/steamapps/workshop/content/294100/2889803802`; latest `1.6/Defs/Recipes_BulkSlag.xml`
  SHA-256 **4BC41DB3B5F07B750E32E6687214CD3200AB8342FFF83E56D309051947850CE6**.
  No source assembly, custom UI, settings or save implementation exists in the inspected inventory.
- [Research journal](../refinement/2026-11-02-bulk-smelting-research.md) retains remaining hashes,
  native paths, source-derived facts versus proposed choices, consumer scans and BS-Q1–4.
- Installed license: **MIT**, copyright 2020 Billy Barnes. Preserve notices for substantial reuse;
  no source code, assembly or raster asset has been copied into the product by this document pass.

## Behavior inventory

All entries are **specified, not implemented or verified** in
[the owning change](../../../../openspec/changes/specify-megamod-bulk-smelting/).
Requirement names below refer to its
[observable capability](../../../../openspec/changes/specify-megamod-bulk-smelting/specs/megamod-bulk-slag-smelting/spec.md).

| ID | Action → observable result | Defaults / edge cases | Evidence / owning requirement |
| --- | --- | --- | --- |
| BS-01 | Open electric-smelter Add bill → one additional three-chunk recipe | Existing singles and other recipes retained; no extra research/station | Source recipeUsers + native catalog; Additional contextual smelting recipe |
| BS-02 | Work permitted batch → three same-kind chunks consumed | Steel or mechanoid slag; two-plus-one is insufficient; ordinary bill restrictions | XML filters + native non-mixing/value rules; Three permitted chunks of one kind |
| BS-03 | Complete ordinary smelting → piecewise-equivalent products (45 steel baseline) | 1100 base work at SmeltingSpeed; ordinary station/work/delivery; no bonus quality or XP | Source recipe fields; Explicit paid work and output |
| BS-04 | Configure repetitions/target/suspension → ordinary bill behavior | One batch is one iteration; target counts steel, permits full-batch overshoot; linked quota shared | Native bills + S005/S014; Ordinary bill counting and control |
| BS-05 | Other content changes → unrelated entries remain | No recipe-list/category replacement; derive admitted single-operation outputs or withhold the bulk addition | Additive source shape and user merge policy; Preserve existing content |
| BS-06 | Save/reload supported ordinary bill → same recipe and choices | Stable Def identity; no custom migration or normal-save rewrite | Native Bill persistence; Saved ordinary bills retain their identity |
| BS-07 | Startup lacks required target → skip addition and warn | Whole recipe group, no dangling ingredient/reference or partial insertion | User target-spec rule + inspected XML Extensions ops; Guarded support and bounded startup diagnostics |
| BS-08 | Original active or same identity present → retain external provider | No duplicate or overwritten recipe; inactive disk copies do not gate; no package spoofing | Identity/consumer audit; One provider and honest external compatibility |

The source yields60 steel /1100 work versus native singles45 /1200. The user selected **efficiency
only**: our baseline is45 /1100, with output tracking admitted current single-operation products.
Homogeneous batches remain; mixed batches are not introduced. Actual work and playability still
require verification. Source research retains its original60-steel finding.

## Interactions and composed result

- **S009–S012 recipe browser:** the same recipe catalog and fact/availability policy expose this
  entry. Availability must respect one three-chunk, non-mixing requirement; combined mixed supply
  cannot be presented as sufficient. No new bottom tab, material scanner or private cache.
- **S005 linked storage / S014 linked workbenches:** count physical Steel once in the selected scope;
  share finite iterations across benches. An output stack counts as one finite repetition.
- **Later S017–S019 / S059:** selected bill modifiers and Best Mix receive their own research and
  composed requirements. This report does not infer their defaults or invent a conflict. Adopted
  behavior integrates directly rather than patching our own recipe implementation.
- **External balance mods:** preserve their single-recipe changes and derive equivalent bulk outputs
  through the admitted contract; unknown or unrepresentable outcomes withhold our recipe.

## UI audit and settings boundary

| Source surface | Purpose | Megamod destination / impact |
| --- | --- | --- |
| Electric-smelter Add bill entry | Ordinary production choice | Existing/contextual integrated Add bill view; no new tab |
| Native bulk bill controls | Ingredients, worker, quantity, suspension, delivery | Entity gameplay state in native/composed bill configuration |
| Recipe description and job label | Explain the batch and current work | Translated in all seven baseline languages |
| Settings, debug commands or view toggles | None in source | None introduced by this feature |

Fixed recipe data is not a user setting. No inheritance exception or alternate settings loader is
introduced. A later actual balance setting must use the common global/save path and specify when it
affects existing work. Source batch numbers are explicit in the spec and declarative named fields.

## Contracts consumed by other mods or saves

| Consumer / evidence | Binding | Preservation and boundary | Future proof |
| --- | --- | --- | --- |
| Native saved ordinary bills | Recipe name `ExtractMetalFromSlagBulk`, ordinary Bill_Production choices/counts | Keep name and native schema when admitted; no claim for missing content or foreign custom records | Own-colony reload and native production |
| Possible XML recipe consumers | Stable recipe name and ordinary filters/products/users | Keep source identity/shape where practical; none found in bounded installed behavior scan | Exact external consumer workflow if discovered |
| Installed metadata catalogs 1847679158/db/db.json and 3396308787/replacements.json | Package/Workshop identity and predecessor mapping | Documentation metadata only; do not impersonate `zal.bulksmelting` or `neceros.bulksmelting` | Confirm source detection depends on active package, not installed directory |
| Active original or foreign same-name recipe | Provider ownership of content | Withhold ours; never overwrite, duplicate or unpatch provider | Focused present/collision startup and native recipe inspection |

The installed DLL literal scan found no binding; absence in that bounded scan is not universal
compatibility proof. No facade is justified. Initial support starts a new colony; ordinary megamod
bill reload is required, while original-provider save import is deferred.

## Implementation and coverage

**Nothing is implemented.** BS-01–08 lack runtime recipe/guard code, executable target validators,
focused RED/GREEN and reviewed-build native acceptance. All implementation tasks remain unchecked.

Intentional improvements over the source are prerequisite/collision guarding, merge-preserving
idempotent addition, bounded startup diagnostics and seven-language presentation. Homogeneous
batches and1100 work remain, with user-selected piecewise-equivalent output. Extra slag kinds, mixed
batches, universal batching, source settings/custom-record migration and automatic
mod-list changes are not implemented or silently promised.

Required future dependency: **XML Extensions**, with its ordinary external Harmony dependency graph.
The source is not required; active original providers disable our overlap. Use inspected
`XmlExtensions.FindMod` with `packageId=true`, content/node checks through `XmlExtensions.Conditional`
and bounded `XmlExtensions.Log` warnings, subject to exact beta proof. Add one complete recipe;
do not replace the station's recipes or any existing filter/category list. Missing one required
slag kind rejects this entire addition. The exact target document and finalized-load safeguards
remain BS-Q3; raw name existence alone is not claimed to prove all native semantics.

## Verification and acceptance

Document validation/review is recorded in the [sequential ledger](../refinement/specification-progress.md).
No game was launched for this prose work. Final implementation acceptance requires actual native
recipe selection, worker hauling/smelting, physical input/output, finite and target counts, ordinary
restrictions, own-colony save/reload, plus focused provider/collision cases.
Logs and finalized Def reads support those workflows but do not replace them.

Retain exact reviewed build, ordered mod list, action/result screenshots, normal-configuration hashes
and cleanup. Perform the final balance pass on real work and finalized resources; source XML ratios
alone do not establish live playability. BS-Q1–4 remain in refinement until resolved with evidence.
