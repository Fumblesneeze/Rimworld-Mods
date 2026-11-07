# S137 — Condition-driven medical bill filtering research

Sole owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
Origin **original**. The [2026-11-05 intake](2026-11-05-condition-medical-bill-filter-intake.md) is the
user's provenance. This is source research and proposed design, not implemented or live-verified behavior.

## Public documentation first and scope

[Dubs Mint Menus](https://steamcommunity.com/sharedfiles/filedetails/?id=1446523594), by Dubwise,
advertises searchable list-based menus including medical bills. Its other menus are outside this
user-selected UI reference. [Compact Hediffs](https://steamcommunity.com/sharedfiles/filedetails/?id=2031734067),
by PeteTimesSix, documents compact health presentation and folded augmentation names. S007 already
owns that replacement; S137 adds the contextual recipe interaction. Public descriptions do not
establish a universal condition-to-operation mapping or promise the proposed ancestor availability.

Read the public pages before focused installed decompilation; public source repository metadata was
also inspected. Do not infer compatibility from Steam's generic incompatible/removed page boilerplate.

## Exact read-only inputs

Evidence root: `artifacts/Refinement/megamod-spec-20261107/condition-medical-bills/`.
`input-manifest.json` records file sizes and SHA256 values. Installed content was not changed.

| Input | Identity |
| --- | --- |
| Mint | `Dubwise.DubsMintMenus`, Workshop1446523594, About version1.3.1704, declares1.0–1.7; selected `1.7/Assemblies/DubsMintMenus.dll`,214016bytes, SHA256 `155b7762c1fb9ff927d7c5d9c561d97baf0950b34e91e0e55e8ec9180707ae2f` |
| Compact | `PeteTimesSix.CompactHediffs`, Workshop2031734067, installedv1.6 DLL51200bytes, SHA256 `e223a52401b18c06be9d5f7b7b5bd6bdc1c9d3d75957dd30301a5a7f44418196`; About declares only through1.6 |
| Game |1.7.5000 rev1275; Assembly-CSharp15636480bytes, SHA256 `d55aa8e47fa3c5463ca85b0eccd51f195b4bbcd04f56da3af21c2f6688a81254` |
| XML | Core BodyParts_General and Recipes_Surgery_Misc; Odyssey RemovePorcupineQuill recipe, conditional recipe membership on human/animal races |

Mint public [repository](https://github.com/Dubwise56/Dubs-Mint-Menus) head inspected at
`d5395f530a9303927737f1c3ad151782a9b1a6b1` (2026-10-28); GitHub license API404/no reported license.
That does not prove a license grant or binary/source identity. Installed binary is the behavioral
reference. Compact public commit/license and source-versus-DLL correspondence remain documented in
[S007 coverage](../replacements/compact-hediffs.md): MIT, copyright2020 PeteTimesSix, commit
`0971051c045803ee53f1e754a89d9565b7320ce6`. Independent code planned; no downloaded code, assemblies
or art copied into product output. Hashes establish research identity, never runtime admission.

## Inspected surface and findings

### Mint medical pane

Full selected DLL decompiled; **focused medical surface inspected**, not every unrelated Mint feature.
`Patch_HealthCardUtility.cs` was read in full, including `BillEntry`, candidate generation, label/search,
rendering, action dispatch and the separate legacy helper. `HarmonyPatches.TabSizeAdjuster` and
constructor patch ownership were inspected. Its health postfix runs only on the Operations subtab;
native health drawing still displays the condition listing beside the summary. `Visible` starts true,
the sidebar can be toggled, and search matches label or provider name without case sensitivity.

Candidate generation follows native AvailableNow, worker report, missing-ingredient hiding,
body-part enumeration and AvailableOnNow checks. `GenerateSurgeryOption` supplies the real action;
the active `DrawSurgeryOption` invokes it. A different `GenerateListing` helper directly constructs
bills, but it is not the inspected postfix's active path. Do not confuse the two or copy its weaker
ordering path. The source uses static patient/list state and refreshes after500 draw passes; S137
instead needs immediate patient/availability revalidation, including paused changes.

Mint About omits a dependency list although Workshop lists Harmony and source eagerly uses Harmony.
Our actual patches must declare their dependency. Source static fields/methods are visible in the
decompilation, but no consumer requiring their identities is established by this focused audit;
do not promise a Mint facade or full replacement. HBF-Q3 retains actual consumer/provider inspection.

### Compact row identity and native actions

Reinspected S007's pinned `CustomHealthCardUtility.DrawHediffRow`, `GetReplacingPart` and the native
1.7 `CanEntryBeClicked`, `EntryClicked`, `GetCombatLogInfo`. Compact groups several conditions and
can fold an added-part condition into its body-part label. Its ordinary clickable region delegates
to native combat-log navigation. Our selection must receive exact displayed members, including the
folded replacement, rather than infer from text or select every condition on that part. S007's
unimplemented draft is amended to reserve primary click for S137 when active and offer a separate
labelled event action. When filtering is unavailable its original log click remains.

### Native candidate and action authority

Read `HealthCardUtility.DrawPawnHealthCard`, `DrawHealthSummary`, `DrawMedOperationsTab`,
`GenerateSurgeryOption`, `CreateSurgeryBill` and log navigation; recipe worker/Def fields, available
reports and selected surgery workers; BodyPartRecord ancestry; health restoration and missing-part
creation. Native bodies/recipes are inspection inputs, not a finalized active-mod database.

The list uses the actual bill giver's recipes, current recipe availability and worker permission.
Rejected reports with a reason can remain as disabled options. Missing tech-hediff or drug
ingredients hide recipes; other missing ingredients either hide or disable according to recipe
rules. Part-targeted recipes enumerate actual body records and check current availability. Native
actions retain royal implant/worker confirmations, xenogerm selection and medical bill creation.
Warnings about doctor skill, beds, medicine restriction and faction harm are not interchangeable
with permission to browse/order; some occur after an ordinary bill is added. Filtering must not
silently add a new doctor/bed prohibition or bypass confirmation.

### General effects and limits

There is no universal effect-description method on `RecipeWorker`: arbitrary overrides can do
anything, and metadata fields alone do not prove their implementation. The initial general rules
use inspected families and exact current patient/option identity:

- `Recipe_RemoveHediff`: per-part mode removes the **first** visible matching condition on that part;
  whole-body mode removes **all** visible matching conditions. Odyssey quill removal is the latter:
  work600, no ingredients, no anesthesia, no surgery outcome failure effect. These are source facts,
  not new balance changes. Optional Odyssey/content presence is required for that example only.
- `Recipe_RemoveImplant`: candidates identify parts, but successful code selects the first matching
  condition by Def **without filtering part**. A naïve exact-part map would misidentify duplicate
  implants. Match the actual affected instance or withhold certainty; this feature does not silently
  repair that native worker. Bionic limb removal uses the part-removal path instead.
- `Recipe_ChangeImplantLevel`: first operable matching level condition on the target part, with a
  nonzero configured offset. Both increase and decrease affect a condition; neither is automatically
  labelled a cure.
- `Recipe_RemoveBodyPart`: surgical damage99999/penetration999 creates a missing part through health
  handling. `Hediff_MissingPart.PostAdd` calls `RestorePart(Part,this,false)` and creates missing
  descendants. `RestorePartRecursiveInt` clears conditions except `keepOnBodyPartRestoration` and
  the explicit exception. Thus match actual cleared descendants, not every condition indiscriminately.
- Natural/artificial installation workers call `RestorePartAndSpawnAllPreviousParts`; the same
  restoration exceptions matter. An arbitrary implant worker or addsHediff field is not evidence
  that every injury on that part is removed.
- `Recipe_AdministerIngestible` calls the ingredient's real ingestion path. No generic offset-hediff
  type was found under the two probed names; empty failed extraction files are not evidence. Further
  concrete ingestion effects require their own inspected descriptors/corrections. No trial ingestion.

Known families must be checked against effective overrides/patches. Heuristics are deliberately
limited to metadata directly naming the selected condition with compatible target scope, and are
visibly uncertain. Explicit researched mappings/exclusions handle effects that do not fit these
families. The proposal does not claim universal treatment discovery.

## Important discrepancy: the foot-infection example

The inspected `Recipe_RemoveBodyPart.GetPartsToApplyOn` includes direct added parts, clean droppable
parts, forced removable parts, or non-core suggestible parts with a **directly attached** visible
bad non-injury/permanent injury. Core Leg has no forced-removal or dropped-natural-part field.
Consequently an otherwise healthy leg is not enumerated solely because its foot has an infection.
Transitive matching alone does not satisfy the user's example.

**Proposed, not user-confirmed implementation choice:** extend that one suggestible-part branch
to qualifying descendant conditions for present outside non-core ancestor limbs, retaining all
other eligibility and provider vetoes. The choice remains available with the filter cleared and
clearly names the entire body part lost. Existing surgery costs, products, risk, anesthesia and
thought/faction effects remain native. HBF-Q2 records a decision before implementation; no recipe
or health data has been changed. If this extension is rejected, revise the example/contract rather
than quietly describing a filter-only implementation as complete.

## UI, shared data and composition

Proposed interaction details are explicit draft choices, not retroactive user answers: primary click
selects, same-row/clear removes that selection, grouped selection snapshots current members, search
intersects results, known results precede Possible matches, and stale selections clear visibly.
Selection/search reset on patient/view/game change and are not settings. Existing queues stay intact.
No new main tab or bottom-right global filter toggle is needed for this contextual action.

S007 row/tooltip/layout, S062 care controls, S089 serum choice, S124 drug response and S134 treatment
order reconciliation compose by direct owned calls. Reuse one medical catalog and current patient
projection, with static content descriptors only when useful. Native/shared supply data replaces
duplicate map scans; never persist live patient references or Mint's global 500-frame cache.
No automatic request, cancellation hold or policy change is created by selecting a condition.

## Review and remaining work

Draft [change](../../../../openspec/changes/specify-megamod-condition-medical-bills/) and
[coverage](../replacements/condition-medical-bills.md) distinguish observable rules from implementation.
HBF-Q1 research is resolved for this draft; HBF-Q2–5 retain the ancestor choice, actual effective
contracts, localized layout and all implementation/acceptance work. Independent review and strict
validation are recorded in the sequential ledger and ignored evidence, never as gameplay acceptance.
No product build, install, config change or game launch occurred for this research.
