# De-generalize Work

## Identity and provenance

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target1.7 beta.
Origin replacement **S122/P2-52**. Source `Alias.DegeneralizeWork`, author Alias,
[Workshop2011655761](https://steamcommunity.com/sharedfiles/filedetails/?id=2011655761), modVersion2.0.1.
Pinned source9ab0e3024fa547e85ab3be728a12243f0e855d74 and matching1.7 assemblies were inspected read-only.
GPL-2.0 provenance; no upstream code or assembly copied into product. Exact hashes, paths, primary
documentation and code-derived evidence are in [research](../refinement/2026-11-06-de-generalize-work-research.md).

Owning [proposal/design/tasks](../../../../openspec/changes/specify-megamod-specialized-crafting-speed/),
[crafting behavior](../../../../openspec/changes/specify-megamod-specialized-crafting-speed/specs/megamod-specialized-crafting-speed/spec.md)
and [integration behavior](../../../../openspec/changes/specify-megamod-specialized-crafting-speed/specs/megamod-crafting-speed-integration/spec.md).
Status: **independently reviewed and strictly validated specification draft**, not implemented or
accepted in game.

## Behavior inventory

| ID | Player action → result | Default, edge and evidence | Owning contract | Status |
| --- | --- | --- | --- | --- |
| DG-01 | Inspect/work with a pawn → distinct smithing/tailoring/sculpting speeds | Base100%, floor10%, general labor once; Common/Defs and native StatWorker | Crafting: separate speeds | Specified draft |
| DG-02 | Run an eligible recipe → corresponding speed advances actual work | Source activity mapping, corrected inheritance; native Toils_Recipe | Crafting: recipe activity/inheritance | Specified draft |
| DG-03 | Cook/smelt/run an unrelated recipe → its established speed remains | Native specialized speeds preserved; S013 uses SmeltingSpeed | Crafting: existing specialized work | Specified draft |
| DG-04 | Use VSE expertise → distinct effects compose | Conservative built-in; four VSE children, FSF bionics if admitted | Integration: conservative/bionics | Specified draft |
| DG-05 | Select Consolidated and restart → single art/tailoring contribution domain | Source BestGuess intent, preserved aliases instead of removed stats; unknown shapes reject whole group | Integration: consolidation/bindings | Specified draft; DGW-Q2 implementation proof pending |
| DG-06 | Change compatibility setting → pending restart choice | Common global-only exception, active/pending display, no Heavy hand | Integration: shared restart settings | Specified draft |
| DG-07 | Use direct stat consumers → bonus/display stays meaningful | No package spoof; package-gated Additional Traits remains a documented gap | Integration: existing modifiers | Specified draft; adapter gaps open |
| DG-08 | Start with changed targets/original → coherent provider or warning | Semantic checks, merge-preserving XML, atomic rollback, no duplicate stat provider | Integration: ownership/admission | Specified draft |

## Interactions and composed result

VSE and FSF are optional providers retained in their own right, not ports being silently folded into
this change. Their expertise levels and multipliers remain theirs. Conservative preserves the
child-stat graph; Consolidated merges known art/tailoring contributions into one domain, deliberately
changing combined arithmetic. Examples and unsupported cases are specified explicitly.

S013 Bulk Smelting stays native-smelting based. S009–S019 bill/recipe/worker features and S121 queries
read the same effective stats and recipes through direct owned composition. No internal patches
against another megamod feature, repeated Thing scans or new stat cache. S135 Basic Caravan Loading
owns its work-assignment choices; this source **does not** split Work-tab categories.

## UI audit and settings

| Source surface | Purpose | Owned destination |
| --- | --- | --- |
| Native pawn stat information | Observe specialized speed and explanation | Same pawn/stat context; localized speed-versus-quality descriptions |
| Mod settings compatibility list | Choose startup-wide provider transformation | Shared Production / Specialized crafting section, Conservative/Consolidated, active/pending and restart indication |
| Source primer mentions Heavy hand | Describes commented-out behavior | Deliberately omitted as a nonfunctional option |
| Bottom tabs/debug/toggles | None introduced by source | None introduced by this replacement |

Compatibility mode is global-only because all saves in the process share patched definitions. Use
common load/store/validation/rendering; disabled Current save and Use global explain that exception
at main menu and in play. No pawn-specific policy or save migration is added. All seven languages
must be maintained when implementation ships.

## Contracts consumed by others

| Consumer | Binding | Preserve / adapt / limit | Required evidence |
| --- | --- | --- | --- |
| Direct equipment/traits | SmithingSpeed, TailoringSpeed, SculptingSpeed | Preserve admitted Def identities and modifier meaning | Exact consumer native equipment/stat/crafting workflow |
| VSE3400246558 / FSF1419675146 | Named specialized stats, recipe references, expertise/health modifiers | Narrow package/content-shape adapters; retain VSE aliases in consolidated mode | Both numeric composition modes plus displayed/applied equivalence |
| Additional Traits1621571024 | Alias package MayRequire, conditional folder; exact trait names in research | **Unsupported package-gated parity** pending separate narrow proof; direct names do not activate hidden content | Actual enabled/disabled provider branches on1.7, no trait resurrection |
| Primitive Tools3751288694 | Alternative definitions and fallback recipe patches | Reuse only proven compatible complete behavior; do not overwrite/delete or import its unrelated content | Duplicate/partial-collision, ordering and native job checks |
| VSE Rexamined3235834179 | Additional expertise art/tailoring offsets | Recognized mappings once; installed metadata only through1.6 | Actual1.7 active-provider analysis before support claim |
| Numbers1414302321 | Smithing-speed generated-column reference | Preserve available observations; no full Numbers replacement | Native column/stat readout workflow |
| AliasDynamicPatch public framework | Source patch-operation types and global dictionary | No external literal consumer demonstrated; no whole-framework facade or automatic migration | Revisit only for an actual consumer |

No package/assembly impersonation or full drop-in claim. Original co-installation selects the
original for the overlapping feature. Advanced FSF's adapter code is known but its current installed
content is not verified. Older declared versions and literal references are not acceptance evidence.

## Implementation and coverage

**Nothing implemented.** No patches, runtime target documents, tests or live acceptance exist for
this feature yet. All implementation tasks remain unchecked.

Intentional deviations: effective inheritance-aware mapping, exact-factor preservation, transactional
group failure, retained consolidated stat identities, honest active/pending settings and no dead
Heavy hand option. No whole-list replacement is intended. Native work, product quality and time-based
training rules remain; production speed still changes elapsed work and therefore possible XP per item.

Required later: XML Extensions metadata and guarded operations; all actual target documents;
proof of consolidated alias behavior; provider-gated trait decisions; localization; reviewed code
and exact native workflows. The source's global settings are not automatically migrated. Unsupported
external bindings are explicit rather than resolved by copying source assemblies.

## Verification and acceptance

Read-only documentation/source/decompile and inert XPath observations support the draft only.
Two independent document reviews found no issues; strict validation passed144 items with0 failures,
recorded in the
[progress ledger](../refinement/specification-progress.md). DGW-Q1–5 retain remaining research and
implementation evidence. Later use honest Unit/Defs environments, exact active-provider processes,
focused RED/GREEN, independent code review, native bill/stat/settings actions and personally viewed
results. Final balance review must use finalized native/VSE comparisons, not only source numbers.
