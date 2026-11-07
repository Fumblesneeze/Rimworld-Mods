# Bulk Smelting (Continued): sequential research

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`).
Queue **S015**, photo **P1-12**, origin **replacement**. Inspected **2026-11-02**.
Documentation and read-only XML/native-code inspection below are research, not live acceptance.

## Documentation first

- [Maintainer's Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=2889803802)
  describes a three-slag batch and approximately ten percent less work. Zaljerem continues Neceros's
  original [Workshop item](https://steamcommunity.com/sharedfiles/filedetails/?id=2060240608).
- [Maintainer's change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/2889803802)
  record the 1.4/1.5/1.6 updates and addition of mechanoid slag in April 2024. The installed latest
  version is 1.0.6. Public and installed icon-update dates differ; this does not establish a recipe change.
- Public metadata lists 1.4–1.6. A generic Workshop incompatibility/removal banner is not evidence of
  a tested 1.7 failure. This analysis does not claim that the source runs in the beta.

## Installed provenance and complete content inventory

Read-only root: `F:/Steam/steamapps/workshop/content/294100/2889803802`.
About identifies **Bulk Smelting (Continued)**, author **Zaljerem**, package **zal.bulksmelting**,
version **1.0.6**; Manifest says **1.0.6.0**. Supported versions and LoadFolders entries are
1.4/1.5/1.6, with root and corresponding version folders. No 1.7 entry or dependency is declared.

The complete inventory is About metadata, license, changelog, publication ID and preview/icon;
LoadFolders; and one `Defs/Recipes_BulkSlag.xml` for each declared version. There is **no assembly,
C# source, PatchOperation, settings implementation, extra UI or language directory**. XML is the
complete feature source. No decompilation of an upstream DLL is needed.

| Input | SHA-256 |
| --- | --- |
| About/About.xml | `C4935F5E541C407B20760AD041FEE2759C3A3CA512125E29EE57E5363F7F453C` |
| LoadFolders.xml | `56C87AB164BFCDAE9CBB8EAA523D6B94D8C075005D2CB201B622A6AD7F8D0B07` |
| 1.6/Defs/Recipes_BulkSlag.xml; identical 1.5 file | `4BC41DB3B5F07B750E32E6687214CD3200AB8342FFF83E56D309051947850CE6` |
| 1.4/Defs/Recipes_BulkSlag.xml | `81C8392F865FD6FD67DAFABD3A0B7D14A6EC8A06C834E2E6B8297B29559C72BE` |

These are the current inspected inputs; the historical identity-audit About hash is not silently
rewritten. `About/LICENSE` is MIT, copyright 2020 Billy Barnes. Retain required notices for any
substantial reuse; this specification neither copies source assets nor grants a broader reuse right.

## Exact recipe and code-derived behavior

`ExtractMetalFromSlagBulk` is an independent RecipeDef, displayed as smelting slag x3. It supplies
ElectricSmelter through `recipeUsers`, consumes one ingredient requirement of count **3**, restricts
both its ingredient and fixed filter to **ChunkSlagSteel / ChunkMechanoidSlag**, produces **60 Steel**,
and has **1100 work**, **SmeltingSpeed**, **Smelt** working effect and **Recipe_Smelt** sound.
The 1.4 variant permits only steel slag. It does not replace the vanilla recipe or a station's list.

No work skill, skill minimum, extra research, custom worker, quality rule, special output, separate
ingredient-value getter, display priority or mixing flag is supplied. Native defaults therefore
matter. The inspected 1.7 `RecipeDef.allowMixingIngredients` defaults false; `WorkGiver_DoBill`'s
non-mixing selection aggregates by ThingDef within one ingredient requirement. The ordinary volume
getter counts a non-Stuff slag item as one. Consequently the source recipe requires **three chunks
of one kind**, not two steel chunks plus one mechanoid chunk. This is code-derived, unplayed evidence.
Native bills, ingredient restrictions, work eligibility, physical hauling, repeat/target counts,
save persistence and product delivery are inherited; there is no separate bulk-smelting lifecycle.

## Installed 1.7 comparison and balance consequences

Typed installation inspection: **1.7.4995 rev309**, `F:/Steam/steamapps/common/RimWorld`.
`Assembly-CSharp.dll` SHA-256:
`56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21`.
Core `Data/Core/Defs/RecipeDefs/Recipes_Production.xml` SHA-256:
`FC7697575FB2F716CB5BE6045B02D6471AEEEA9E768D08607099D54E3E114829`.

| Recipe / normalized comparison | Ingredients | Steel | Work | Consequence |
| --- | --- | --- | --- | --- |
| Core ExtractMetalFromSlag | 1 steel or mechanoid slag | 15 | 400 | Ordinary single-chunk baseline |
| Three Core executions | 3 slag chunks | 45 | 1200 | Normalized same input quantity |
| Installed bulk recipe | 3 same-kind slag chunks | 60 | 1100 | 33⅓% more steel and 8⅓% less work than three singles |
| Core SmeltWeapon / SmeltApparel | One eligible item | Item-dependent materials | 1600 | Nearby station workflows, not equivalent slag-yield comparators |

The source's approximately ten-percent description is not its entire balance contract. At equal
work speed, the work-only throughput ratio is about 9.09% faster per three chunks; actual elapsed
time also depends on hauling and interruptions. Do not call the recipe merely a UI convenience.
Core comments explicitly make single-slag output a fixed product to support target-count bills.

Core ElectricSmelter retains Electricity research, construction 4, 170 steel plus two industrial
components, 3500 construction work and 700 power. Its existing smelt/destroy recipes remain present.
`Buildings_Production.xml` SHA-256:
`3221C30ADC223498636AC7B6CA86EF9C50FCE681B138BE8F0919C6E652045A31`.
Core ChunkSlagSteel is a haulable, stack-limit-one, mass-eight non-Stuff item; ChunkMechanoidSlag
inherits it in Core. No extra DLC gate appears in the inspected definition. Finalized inheritance,
other mods' edits and real reachability have not been verified in a running game.

Native read-only excerpts are under ignored
`artifacts/Refinement/megamod-spec-20261102/bulk-smelting/game` (RimWorld.IngredientValueGetter_Volume),
`quartermaster/game` (Verse.RecipeDef) and `workbench-linking/game` (IngredientCount, WorkGiver_DoBill,
bill execution and counts). The failed initial Verse namespace lookup was corrected to RimWorld;
it is not source evidence. These artifacts are not packaged dependencies.

## Consumer and UI audit

Bounded searches of installed Workshop/local mod XML, C#, JSON and text for
`ExtractMetalFromSlagBulk` / `zal.bulksmelting` found source files and two metadata catalogs:
Workshop **1847679158/db/db.json** identifies the item/package, and
**3396308787/replacements.json** maps original **neceros.bulksmelting** to the continued package.
Neither inspected match consumes the recipe behavior. A binary literal scan of installed DLLs
returned no match. This does not prove the absence of dynamically constructed or uninstalled consumers.

The stable recipe Def name can be referenced by saved ordinary bills and external XML, even though
no actual external behavior consumer was found in this scan. Package checks for either original
identity cannot be preserved by pretending megamod is that package. Source coexistence and saved
bill continuity must have explicit boundaries, not an unqualified drop-in claim.

Source UI is only an ordinary Add bill entry plus native bill details. No bottom tab, global toggle,
settings window or cheat is added. Bill-specific material/repeat/output choices remain gameplay
state. The integrated browser and whole-list workbench linking should treat this as one ordinary
recipe: one batch execution counts once, while stock targets count the resulting steel items.

## XML Extensions admission research

The author's primary references for
[Conditional](https://github.com/15adhami/XmlExtensions/wiki/XmlExtensions.Conditional),
[FindMod](https://github.com/15adhami/XmlExtensions/wiki/XmlExtensions.FindMod) and
[Log](https://github.com/15adhami/XmlExtensions/wiki/XmlExtensions.Log) were read, then compared with
the installed 1.6 assembly. Its SHA-256 is
`1E64E318C905B94DE3EF05186D82E2067933238453D818366EAC73D688464B35` under Workshop 2574315206.
Read-only decompilation is retained in `bulk-smelting/xml-extensions` beside the native artifacts.

`Conditional` checks a selected XML node and selects `caseTrue` / `caseFalse` operation containers.
`FindMod` supports canonical IDs with `packageId=true`, compares case-insensitively and defaults to
OR over supplied packages. `Log` accepts `warning` text and sends it to the ordinary warning log.
These provide concrete candidates for prerequisite/collision guards, original-provider selection
and bounded startup diagnostics. No invented operation name or raw XML dump is needed.

This does **not** prove atomic rollback, repeated-load deduplication, post-patch validation or 1.7
compatibility. The installed framework declares only through 1.6. Use a fully guarded single recipe
insertion, not multiple mutations falsely treated as a transaction; prove the framework and final
Def behavior before accepting the port. Exact target documentation remains implementation work.

## Open work and choices

- **BS-Q1 — proposed balance:** retain the source's 3 chunks / 60 steel / 1100 work for fidelity, with
  the explicit yield benefit above. A yield-neutral 45-steel alternative is a later choice, not an
  already approved change. Measure finalized Defs and native production before acceptance.
- **BS-Q2 — proposed mixing:** retain homogeneous three-chunk batches. Mixed steel/mechanoid batches
  would be an intentional convenience change; record it if selected instead of silently inferring it.
- **BS-Q3 — implementation research:** the candidate XML Extensions operations are now pinned by
  docs and installed code. Still prove 1.7 behavior, per-patch target guards, warning deduplication,
  atomic insertion and collision handling at the real load boundary.
- **BS-Q4 — verification:** establish native bill save continuity, source-present overlap handling,
  missing/changed Def behavior and composed browser/linked-bill production on the reviewed build.

Questions are retained for later. No question interrupts the sequential source queue.
