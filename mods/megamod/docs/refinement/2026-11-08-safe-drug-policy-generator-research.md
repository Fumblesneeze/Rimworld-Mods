# S139 — Safe Drug Policy Generator

Research date: 2026-11-08. Reserved source ID S139. Prospective owner is Make Rimworld Great Again,
`fumblesneeze.megamod`, targeting the inspected native 1.7 beta. This source is **idea-only**: the
user likes the concept and reports historical fragility, but that report does not establish a defect
in any particular upstream revision. This document distinguishes inspected behavior, static risks
and possible owned design. It neither adopts the source algorithm nor accepts gameplay behavior.

Research followed S138 sequentially: public author documentation first, then installed About,
supplied source and stdout-only decompilation of the selected DLL/native methods. Workshop inputs
were read-only; no implementation was copied, built, tested or launched. Only the two research
documents were authorized outputs. Latest user scope excludes original-mod drop-in and API
compatibility completely. No source API, package/type facade, source-settings/save migration or
external-consumer parity is proposed. The original-loaded warning remains; suppression is
best-effort through ordinary feature settings-off behavior, not a compatibility guarantee or test.

## Public documentation observed

The [Steam description](https://steamcommunity.com/sharedfiles/filedetails/?id=2582640021) identifies
SeoHyeon's generator, advertises addiction-safe policy generation, and instructs existing-save users
to add the policy through mod settings. It recommends Better Pawn Control and Vanilla Brewing
Expanded, and strongly recommends the tolerance-slider mod because independent drug schedules
cannot adequately handle variety. The author's August 2025 response acknowledges that limitation.
These recommendations are not declared hard dependencies or proof of broad mod compatibility.

Steam currently tags 1.4–1.7. A comment reports the policy not appearing on 1.7; it is unverified here.
Generic removal/incompatibility banners are not binary evidence. The public changelog could not be
retrieved by the web tool. No confirmed author repository was found through the examined author
links/search, and installed About has an empty URL. The supplied local source therefore anchors the
implementation observations below. Neither comments nor the user's historical experience are used
to attribute a reproduced bug to this build.

## Exact installed identity and inputs

- Title: **Safe Drug Policy Generator**; author: SeoHyeon.
- Workshop ID: **2582640021**; package ID: `seohyeon.bestdrugpolicy`.
- Installed root: `F:/Steam/steamapps/workshop/content/294100/2582640021`.
- Selected binary: `1.7/Assemblies/BestDrugPolicyGen.dll`, 10752 bytes. Supplied AssemblyInfo declares
  assembly/file version 1.0.0.0; this is not a release-date identifier.
- Supplied source: `Source/BestDrugPolicyGen/`; project targets net10.0 with `RW17` and references
  Assembly-CSharp and Unity stubs. Inspected source and DLL agree on the generator, settings and
  startup behavior. Decompiler missing-reference annotations are not game errors.
- About supports 1.4–1.7 and declares no `modDependencies`. Core/Royalty/Ideology load-after entries
  are not DLC requirements. There are no Harmony patches in the inspected current source.
- Supplied LICENSE is MIT, copyright 2025 SeoHyeon. Research records behavior, not implementation.

All relative input paths below are beneath that installed root. SHA256 pins inspected evidence;
it is not a proposed admission guard or a requirement to retain source API shapes.

| Input | SHA256 |
| --- | --- |
| `About/About.xml` | `633C9682496969861247918E3C8A8C199800FA9012F73049F85012E60E852EDF` |
| `1.7/Assemblies/BestDrugPolicyGen.dll` | `E66B669C0644A75E72443849939389707AD734300C81265B839A557E11B22889` |
| `Source/BestDrugPolicyGen/Mod.cs` | `C03ADE4225BEC0CF3DC3C6ED40EDCCE814C5024143C205E21A53BA43E8DB734E` |
| `Source/BestDrugPolicyGen/Settings.cs` | `0348255D15D8145F990883F841C4F8F086C3F9C1B99A0E95D93EEA9B395EC850` |
| `Source/BestDrugPolicyGen/SafeDrugPolicyGenerator.cs` | `27BE01424B7C8CB8BB72BF062D69DD95F1220333513294D153391C1B889BF4DC` |
| `Source/BestDrugPolicyGen/StaticConstructor.cs` | `144DB3E7C24DBAFE655D3409DD5EE2F67532D8A6BA42CADA16C9A53E881CE70A` |
| `Source/BestDrugPolicyGen/BestDrugPolicyGen.csproj` | `DE241EA38BB1D7E8BF90DA662D9BFB5C5DE4D61D088E9833BF30723E064D721D` |
| `Languages/English/Keyed/Mod.xml` | `139369BA438006342E4C5A42D94B5231D6BB66C123948F07F4B64F0030209A0A` |
| `LICENSE` | `8347B09636E5A72B72205F6AA75F2D8ABC6306D92F0BE82CB4FB0F8CE66C7C28` |

Native input: `F:/Steam/steamapps/common/RimWorld/Assembly-CSharp.dll`, 1.7.5000 rev1275,
SHA256 `D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254`.
Selected types were inspected using the existing `artifacts/Tools/ilspycmd/ilspycmd.cmd`, stdout
only. As recorded in S138, the discovered typed Gateway decompiler requires a live lease and writes
artifacts, so it does not fit this no-launch, two-document-only research scope.

## Source- and binary-observed controls and lifecycle

The mod-settings category contains these controls. They affect the generated template, not the
values in every existing policy. Native `DrugPolicyEntry.CopyFrom` copies its field values, so
policies already created from the template retain their copies when the template changes.

| Control | Initial value | Observed behavior |
| --- | --- | --- |
| Additional frequency | 0 days | Slider range 0–25; the prior value is cast to integer for the next slider draw. Used as an offset in template updates. |
| Overall joy threshold | 100% | Slider 0–100%; the prior value is rounded to percentage precision for the next draw. |
| Overall mood threshold | 100% | Same range/rounding as joy. |
| Overall tolerance multiplier | 1 | Displayed only when the source detects an active tolerance mod. Range 0–1.1; above 1 displays Any. Numeric values multiply each drug's minimum tolerance for addiction. This describes source behavior, not an owned external adapter requirement. |
| Apply | Button | Updates the shared generated template and posts a positive message. It does not assign policies to pawns. |
| Add safe drug policy to current save | Button, only with `Current.Game` | Updates the template, calls native `NewDrugPolicyFromDef` when the database exists, then posts a positive message. Repeated presses create distinct policies; no replacement/de-duplication is implemented. |

Three ordinary settings are saved in `ModSettings`; the tolerance multiplier is saved only when
the source's tolerance-mod flag is active. The installed English template label is **Safe from
addiction**. Its `DrugPolicyDef.defName` is `BestDrugPolicy`.

At `[StaticConstructorOnStartup]`, source initializes one shared template from all current Item
ThingDefs with `IsDrug`, applies settings, then calls native `DefGenerator.AddImpliedDef`. Native
`DrugPolicyDatabase` construction enumerates DrugPolicyDefs to make starting policies. Existing
save policies are deep-scribed; source has no load hook that inserts the new policy into a restored
list. The explicit settings button is the documented existing-save path. Nothing here installs a
new ingestion scheduler, forces consumption, fills inventory or changes existing pawn assignments.

## Observed generation decisions and native seams

The source asks native `DrugStatsUtility.GetSafeDoseInterval(drug)` with its default body-size
factor 1. It also divides `minToleranceToAddict` by the tolerance giver's severity when a giver
exists, otherwise uses zero. The three source branches are:

| Native interval / ratio | Explicit template row |
| --- | --- |
| Interval exactly zero | Recreation enabled; scheduled use remains false. |
| Nonzero interval and ratio below 1 | No explicit template row. |
| Nonzero interval and ratio not below 1 | Scheduled use enabled; frequency is the ceiling of the interval. Recreation remains false. |

Raw native `new DrugPolicyEntry` defaults addiction permission to **false**, frequency to 1,
mood/joy thresholds to 1 and carried quantity to zero. The generator only sets the fields above,
so its explicit rows retain addiction permission false. In contrast, native `DrugPolicy`
initialization adds every eligible drug with addiction permission true; native
`NewDrugPolicyFromDef` then copies each explicit template row over the corresponding row. Thus
omission from the template is **not a ban or removal**: omitted drugs remain native rows with
their initial addiction permission. S029 subsequently normalizes this permission away for drugs
that are not addictive, while retaining an explicit false on a surviving addictive row.

Native `DrugPolicyDef.allowPleasureDrugs` defaults false. Native template creation creates/appends
the policy before copying explicit entries and uses a throwing drug-identity indexer. A stale or
unavailable template entry therefore matters to S029's existing skip-before-lookup protection;
the generator cannot treat the entire native call as an atomic validate-then-commit operation.

Native `GetSafeDoseInterval` returns zero for absent/non-addictive drug comps, and -1 for certain
unsupported/no-safe-interval cases, including addictiveness at least 1 or a missing tolerance giver.
Otherwise it combines overdose offset and tolerance gain/decay and divides by body-size factor.
Its own readout is contextual data, not a proof that all pawns, doses, chemicals and routes are safe.
The generator does not inspect a particular pawn, genes, current tolerance, actual dose quantity,
other drugs already used or pending medical administration.

## Concrete static findings and limits

These consequences follow from the inspected source and matching DLL. They have not been
reproduced through a live player workflow and are not attributed to the user's historical report.

- **Reset to 100% is incomplete.** Template mood/joy fields are written only when the setting is
  below 1. Lowering a setting, applying, then returning it to 1 and applying leaves the old lower
  template value. Optional tolerance Any similarly skips assignment rather than restoring a
  no-requirement value. Existing policies already copied from the template are unaffected by Apply.
- **Frequency depends on a named Beer baseline.** The update infers the old added-days value from
  the Beer row's frequency minus 1, then adjusts every scheduled row and clamps to 1–25. Without
  an explicit Beer row, no scheduled frequency update occurs. A changed Beer base invalidates the
  inferred delta. A safe interval over 25 would be shortened by the clamp when Beer is present.
  These are concrete input-shape implications, not evidence that Core Beer currently hits them.
- **Negative/nonfinite inputs are not independently rejected.** A nonzero interval enters the
  ratio branch even when it is the native -1 sentinel. For example, a drug with addictiveness at
  least 1 and a tolerance ratio at least 1 can reach the scheduled branch. Division by zero and
  nonfinite values have no explicit source validation. No malformed Def fixture was executed.
- **A shared template is mutated in place.** Updates are not regenerated from immutable baselines,
  do not refresh the catalog, and have no snapshot/rollback. A later exception can leave earlier
  row changes applied. The Add button's positive message is unconditional after its null-conditional
  database call. These facts motivate honest commit/result handling, not blanket exception swallowing.
- **Medical permission differs from generation eligibility.** Explicit scheduled/recreation rows
  clear the native addiction permission through their false default, while omitted rows retain it.
  S134 relies on this permission for assistance. Copying the source algorithm would therefore make
  an unrelated implicit medical decision.
- **The safety name is broader than the evidence.** Per-drug frequency does not account for cumulative
  use of several drugs sharing a chemical. Neither frequency nor a pre-dose tolerance check proves
  immunity to addiction, overdose, body-size/age differences, genes, existing dependency or other
  adverse effects. No exact policy formula has been adopted for the owned feature.

## Proposed owned direction, not adopted specification

Keep the useful idea: an explicit player action can draft a comprehensible drug policy from the
current admitted catalog, with reasons for each suggested setting. A preview followed by creation
of a new native policy is a candidate workflow. Do not automatically overwrite existing player
choices, assign pawns, change the default, regenerate on ordinary load/repair, or claim universal
safety. Decisions about default presets, duplicate-create behavior and whether a preview is needed
remain open. Use common megamod configuration/UI conventions rather than preserving source UI/API.

| Existing owner | Concrete composition requirement for a future proposal |
| --- | --- |
| S029 maintenance | Consume its finalized available-drug catalog and native policy identity rules. Separate intentional creation from maintenance: repair must never reapply generated choices. Compute from stable inputs rather than Beer or mutated rows, preserve existing objects/assignments, and define failure before/after native append. Respect unavailable-preset skipping and applicable-flag normalization. |
| S024 Common Sense | A suggested schedule grants no bypass of native eligibility or the additional potential-mood condition. Patient treats and carried drugs retain their originating authorization. Preview/explanation should state why a scheduled drug may remain unused even after its interval. |
| S124 Drug Response | The generator only proposes policy data; it must not become a second dose scheduler, record consumption at assignment time, or reset history. Actual exposure, dose equivalents and the shared pending-chemical authority remain with their existing owners. Ordinary policy schedules do not authorize medical response. |
| S134 Priority Treatment | Choose addiction/medical permission explicitly and show it, rather than inheriting a raw entry default. Preserve the separate dependency-assistance route, native medical eligibility and player/foreign-order blockers. Generated schedule/tolerance preferences must not silently veto or duplicate that route. |
| S138 tolerance preference | Use one owned policy-entry value and explicit Any/zero semantics, copied/saved alongside native policy data. Current tolerance may be one explained restriction; it is not proof of post-dose safety. No original-mod API bridge is required or proposed. |

For unsupported pharmacology, a future design should expose uncertainty and avoid inventing a
schedule. Any interval cap, rounding rule, additional delay, chemical grouping or pawn-specific
adjustment needs an observable contract and balance justification. Do not accept a source-derived
number simply because its label says safe. Keep the original-loaded warning and best-effort
suppression through ordinary feature settings-off behavior. This promises neither overlap
prevention nor compatibility and does not authorize facade or source-contract work.

## Unresolved research and later acceptance work

- Define the owned promise precisely: reduce routine addiction risk, offer editable suggestions,
  or another bounded outcome. Avoid an absolute safety label unless its exact assumptions are visible.
- Decide the creation entry point, explicit medical permission, repeat-create semantics, per-save
  versus global suggestion defaults, and whether already-edited policies are ever eligible targets.
- Establish how current available content, shared chemicals, absent tolerance/decay, nonfinite or
  sentinel values, custom ingestion outcomes and pawn-specific factors affect suggestion eligibility.
  No arbitrary-mod pharmacology compatibility is established by this research.
- Resolve S138 Any/zero and saved-choice behavior before using it in generated rows. Generation and
  later S029 maintenance must not infer whether a choice was intentional merely from default values.
- Later focused tests should cover settings reversal, absent/modified Beer, intervals above any UI
  limit, unsupported values, partial failure, copied policy independence, medical permission and
  catalog changes. These are candidate tests, not executed evidence.
- Reviewed native workflows must prove creating, inspecting, editing, assigning and saving/reloading
  the policy. Any claim about consumption, dosing, refill, reduced addiction or medical composition
  additionally needs its actual native action-to-result workflow. No such claim is accepted here.

Related contracts inspected under repository-root `openspec/changes/`:
`specify-megamod-drug-policy-maintenance/design.md`,
`specify-megamod-common-sense/specs/megamod-practical-needs/spec.md`,
`specify-megamod-drug-response/design.md`, and `specify-megamod-priority-treatment/design.md`.
S138 research is `mods/megamod/docs/refinement/2026-11-08-drug-tolerance-research.md`.
No ledger, source, plan or OpenSpec change was edited by this research.
