# S138 — Only If 'Tolerance' Below in Drug Policy

Research date: 2026-11-08. Reserved source ID S138. Prospective owner is Make Rimworld Great Again,
`fumblesneeze.megamod`, targeting the inspected native 1.7 beta. This is behavior research, not an
implemented feature or compatibility/acceptance claim. Public documentation was read first, followed
by the installed About, supplied source and stdout-only decompilation of the selected 1.7 DLL and
native seams. No Workshop input was edited; no implementation was copied, built or launched.

Latest user scope explicitly excludes original-mod drop-in and API compatibility. No source API,
package/type facade, source-settings/save migration or external-consumer parity is proposed. The
existing original-loaded warning remains; suppression is best-effort through ordinary feature
settings-off behavior, not full compatibility testing or a guarantee of overlap prevention.
S139 generator research follows this source sequentially and remains idea-only.

## Public documentation observed

The [Steam description](https://steamcommunity.com/sharedfiles/filedetails/?id=3088210509) identifies
SeoHyeon's mod and a drug-policy tolerance slider, with restrictions for addictive drugs that have
tolerance effects. It describes shared chemical tolerance and recommends the separately researched
generator. Current description and the author's October comment distinguish Harmony for 1.7 from
Prepatcher for 1.6 and earlier. Its broad save/mod compatibility and addiction-safety statements are
author claims, not established acceptance here. Steam's generic removal/incompatibility banners
are not treated as evidence about the installed binary.

The [author repository](https://github.com/csh1668/OnlyIfToleranceBelow) links the same Workshop item
and identifies an MIT license. Its README contains no detailed behavior specification. Public
[change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/3088210509) describe
historical tolerance-giver filtering and optional-mod support. The changelog crawl lagged the current
description, so it does not establish the latest binary revision. Public comments about older Yayo
layout failures are reports, not a reproduced failure of this installed 1.7 build.

## Exact installed identity and inspection inputs

- Title: **Only If 'Tolerance' Below in Drug Policy**; author: SeoHyeon.
- Workshop ID: **3088210509**; declared package ID: `seohyeon.onlyIfToleranceBelow`
  (canonical comparison: `seohyeon.onlyiftolerancebelow`).
- Installed root: `F:/Steam/steamapps/workshop/content/294100/3088210509`.
- Selected binary: `1.7/Assemblies/DrugPolicyEnhanced.dll`, 15872 bytes. Supplied AssemblyInfo declares
  assembly/file version 1.0.0.0; this is not a reliable release-date label.
- Supplied source: `Source/DrugPolicyEnhanced/`. Project selects net10.0 with `v15;RW17`; inspected
  binary decompilation confirms the 1.7 weak-table storage, control and eligibility paths described below.
- About supports 1.4–1.7; required package for 1.7 is `brrainz.harmony` (Workshop 2009463077).
  Earlier versions instead declare `zetrith.prepatcher` (2934420800). DLC entries are load-after hints,
  not hard DLC dependencies. No mod-settings class or separate settings window was found.
- Local LICENSE is MIT, copyright 2023 SeoHyeon. Research records behavior only.

All relative paths in this table are beneath the installed root. SHA256 identifies inspected inputs;
it is evidence, not a proposed whole-binary admission rule.

| Input | SHA256 |
| --- | --- |
| `About/About.xml` | `DFCDC796DECF267A03F670FB2D763B4FE6D917BB7200034CAE1648782D232515` |
| `1.7/Assemblies/DrugPolicyEnhanced.dll` | `6C48ACE04DECFEDD65549489046CA5E23B7B40B2DD033331C9BB6E68AA7F6F29` |
| `Source/DrugPolicyEnhanced/Patches.cs` | `06D4425E7FDDD4941D620FB1CBDE162266E530BFE38B208620E8E8EC9CA9A1DB` |
| `Source/DrugPolicyEnhanced/DrugPolicyEnhanced.csproj` | `52092C48AC1F274EB30E7A4A90164C84EF117ABA2240598D6F51319CAB5D340B` |
| `Source/DrugPolicyEnhanced/ExposedPrepatcherFields.cs` | `6427A53EF4D1F548557FEA710E7946E19A0AFB82FFCCC5E452C5563B672745A9` |
| `Source/DrugPolicyEnhanced/Patches_SaveStorageSettings.cs` | `654D3F9647B25A059EECD80E9074CBF55580FCD988346D8ACB65852B10DC356D` |
| `Source/DrugPolicyEnhanced/Patch_YayoCombat_CalculateColumnsWidths.cs` | `5785A60394F8A8BB427CD9C0A3F3CA0E8C97D3438CF753499EB3C9F20F2E244C` |

Native input: `F:/Steam/steamapps/common/RimWorld/Assembly-CSharp.dll`, 1.7.5000 rev1275,
SHA256 `D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254`.
The existing `artifacts/Tools/ilspycmd/ilspycmd.cmd` was used read-only with stdout, without writing
decompiled files. `operation_list` was inspected; its Gateway decompiler requires a leased live
session and retains artifacts, so it was not used for this no-launch, two-document-only task.

## Source- and binary-observed behavior

The ordinary Assign → Manage drug policies surface gains a column labeled **Only if tolerance below**.
There is one slider value per actual DrugPolicyEntry, range 0–1. Values below 1 display a percentage;
1 displays the native no-requirement label, described as Any. Default and missing-save-field value
are 1. A row exposes the slider only when its drug is addictive, has a recognized tolerance giver,
and recreation or scheduled use is enabled. An addiction-only row has no slider. Existing frequency,
mood and joy columns are each narrowed to 80% of their previous width to make room. These are
statically observed UI rules; layout readability, translations and rendering have not been run.

After loading, the source builds a defName eligibility dictionary for drug Defs with Addictive and
a nonnull native tolerance-giver hediff. The inspected native `DrugStatsUtility.GetToleranceGiver`
finds an ingestion outcome matching the chemical's tolerance hediff. Different drugs sharing that
hediff therefore read the same current severity, while their thresholds remain separate row values.

| Route | Observed effect |
| --- | --- |
| `Pawn_DrugPolicyTracker.AllowedToTakeScheduledNow(ThingDef)` | Postfix preserves a native false. For an eligible drug, a present tolerance hediff whose severity is greater than or equal to the row threshold changes true to false. |
| `JoyGiver_Ingest.CanIngestForJoy(Pawn,Thing)` | Same extra veto after native permission. Positive DrugDesire trait degrees and mental states bypass this extra joy veto. This mirrors the native joy-policy exception, not the scheduled path. |
| Addiction relief, manual/forced ingestion, medical administration | No corresponding enforcement patch was found in this source. These routes must not be described as covered. |
| Inventory refill | No new refill rule is installed; having a tolerance threshold does not itself prevent carrying a drug. |

Native scheduled admission already checks scheduled permission, ingestibility/drug classification,
drug willingness, mood/joy thresholds and per-drug dose history. The later
`ShouldTryToTakeScheduledNow` also performs overdose/timing deferral. Native joy admission checks
ingestibility, joy, eating permission, reservation/forbidden/social/political/vacuum constraints and
its joy-policy exception. The tolerance postfix only narrows successful decisions; it does not
create a dose, ignore those prerequisites or update dose history.

The 1.7 implementation attaches values to entry objects through ConditionalWeakTable storage.
`DrugPolicyEntry.CopyFrom` copies the value; `ExposeData` scribes `onlyIfToleranceBelow` with default
1. There is no separate global preset setting. Existing entry identity therefore matters: replacing
an entry without a deliberate transfer loses attached state. Native save/load constructs new objects
and uses serialization to restore values. Supplied source includes optional exporter/layout hooks
and a small externally callable value surface; their preservation and external callers are excluded
by the latest user scope and do not constrain the owned design.

## Concrete edge cases and limits

These are static observations or explicit implications of the inspected predicates, not live bugs:

- Equality rejects a present hediff: tolerance exactly at the threshold is not below it.
- Missing tolerance hediff permits the native result, even with a zero threshold; zero is therefore
  not a universal no-use switch in this source.
- The predicate has no special bypass for the Any value 1. A present severity of 1 or greater is
  rejected even though the UI describes no requirement. Specify an explicit owned sentinel rule
  rather than accidentally inheriting that discrepancy.
- UI bounds do not validate arbitrary persisted/reflected floats. Nonfinite/out-of-range input needs
  an owned serialization decision; no live malformed-save reproduction was performed.
- Eligibility is populated at startup, with no observed rebuild/clear hook for later content changes.
  S029/S038 catalog publication cannot reuse that lifetime assumption. A changed tolerance giver
  can generate repeated source warnings on attempted use; new warnings must remain bounded.
- Positive DrugDesire/mental-state exceptions are specific to joy in this source. Generalizing them
  to scheduled use, patient treats or medical assistance would be a new policy decision.
- A current-tolerance comparison does not predict the next dose's severity increase, pawn body size,
  genes, dose quantity, overdose, withdrawal or other drug effects. It is not proof against addiction.

## Proposed owned composition, not adopted specification

| Existing owner | Concrete overlap and proposed boundary |
| --- | --- |
| S029 drug-policy maintenance | Preserve valid row identity and explicit tolerance choices through cleanup, copy, save/load and display sorting; new rows alone receive the owned default. Keep the existing preset unavailable-entry skip. Do not reapply thresholds during ordinary repair or reorder native execution priority. Extend existing choice snapshot/rollback semantics deliberately if owned state is stored outside native fields. |
| S024 Common Sense | Scheduled eligibility combines native conditions, S024 potential mood and S138 tolerance through one owned decision. Neither extra restriction grants permission. Patient recreation delivery must check the actual recipient's applicable recreation/tolerance rule so it cannot bypass the new restriction; define exceptions explicitly. Inventory support retains the originating route and quantity. |
| S124 Drug Response | Condition/pain responses and scheduled suppression are distinct purposes. A recreational/scheduled tolerance preference must not silently become a universal medical veto or an automatic-treatment authorization. Shared actual-consumption history and pending-dose authority remain S124-owned. |
| S134 Priority Treatment | Dependency/addiction assistance uses its own policy permission, native medical eligibility and shared S124 chemical claim. Do not block required assistance merely by interpreting a routine tolerance threshold as an all-routes ban. Any new medical interaction needs a separate explicit rule and real dose evidence. |

Keep the control on the existing policy row, with one owned state/serialization and UI decision.
Do not copy upstream width patches, Prepatcher fields or compatibility APIs. Keep the original-loaded
warning and best-effort suppression through ordinary feature settings-off behavior; this does not
promise overlap prevention, interchangeability or a full compatibility test.

## Unresolved research and later acceptance work

- Resolve owned Any/zero semantics and malformed saved values before an observable spec; the source
  behavior above is not automatically the desired behavior.
- Establish the exact final native call path for each intended route, including S024 patient treats,
  and state which routes remain out of scope. Verify same-chemical drugs and inherited/overridden
  native joy implementations without claiming arbitrary-provider compatibility.
- Define storage/copy/rename/delete/preset/backfill/content-change behavior alongside S029; preserve
  valid existing choices, saved policy references and dose history. No source-save import is implied.
- Measure the UI with seven languages, narrow widths and current owned alphabetical presentation.
  Define neutral failure when an enabled control or its relevant native decision is unavailable.
- Later focused tests should cover boundary comparisons, native denial, missing hediff/giver,
  exceptions, copied/loaded entries and catalog refresh. Reviewed native player workflows must prove
  actual allowed/blocked scheduled/recreation consumption and observed tolerance, not only slider
  rendering or synthetic health state. This research launches nothing and accepts none of these.

Related contracts inspected: `specify-megamod-drug-policy-maintenance/design.md`,
`specify-megamod-common-sense/specs/megamod-practical-needs/spec.md`,
`specify-megamod-drug-response/design.md`, and `specify-megamod-priority-treatment/design.md`
under repository-root `openspec/changes/`. S138/S139 are only reserved here; no ledger, plan or
OpenSpec change was edited by this research.

## Follow-up: exact 1.7 routes and owned-state seams

Later on 2026-11-08, bounded read-only inspection resolved the native route facts needed by the
now-existing S138 draft. This addendum supplements the earlier source research; it does not claim
implementation or close runtime tasks. The exact ignored evidence is
`artifacts/Implementation/megamod-p0/drug-tolerance-route-research/handoff.md`; its original reviewed
SHA256 is `9DCECEB2E64036A1FCD48360661927CE7B01600F5B1F5D5CA16F6FDA5C599BA0`.
A later wording correction identifies S029 defaults accurately and retains that fingerprint in
the handoff's addendum. The inspected native assembly remained 1.7.5000 rev1275 at
`D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254`.

Typed `operation_list` still exposed only a live-session decompiler. Root authorized the existing
offline `artifacts/Tools/ilspycmd/ilspycmd.cmd -t <type> <assembly>` fallback; stdout was inspected
without executing game code, reconstructing foreign patches or retaining copied implementation.
ILSpy 11.2.0.10050 executable SHA256:
`A3596974949CAC05CAB79FE42AD4FC7A2FE1B40A996E7656295A922F1440B3F4`.
The handoff lists exact commands/types and retained policy/UI source fingerprints. Core
`Data/Core/Defs/Joy/JoyGivers.xml` SHA256 is
`E35331B3CD709DE271871BDF75400C7BB45AD7181DCAD30BA8B9EFC0DCA1439F`;
`Data/Core/Defs/ThinkTreeDefs/SubTrees_Misc.xml` is
`C287C0C2A97F151D86516CF058E2EB54B4D87F1BCCF50BFA46FAF8B26EBA3071`.

Observed native and current owned-code facts:

- Map and caravan scheduled use both reach `ShouldTryToTakeScheduledNow` and then
  `AllowedToTakeScheduledNow`. Later native timing/overdose decisions remain separate.
- `JoyGiver_TakeDrug` inherits `JoyGiver_Ingest.CanIngestForJoy`; its positive-DrugDesire/mental-state
  policy exception does not remove earlier ordinary eligibility checks. `JoyGiver_SocialRelax`
  selects nurseable drinks separately through `TryFindIngestibleToNurse`, requiring `allowedForJoy`
  without that bypass. A base joy hook alone misses social drinking. No drink still permits the
  otherwise eligible native social job.
- Manual orders, addiction/dependency relief and routine use share downstream ingestion helpers;
  medical/caravan use can also reach `Thing.Ingested` directly. A shared helper or driver name does
  not establish routine purpose. Native feeding passes the actual Deliveree to ingestion, not the
  visitor/toil actor; S024's owned treat producer and recheck integration remain future work.
- Native standalone refill excludes scheduled rows. A scheduled ingest job can collect extra stock
  through `takeExtraIngestibles`, so blocking that dose also loses its attached pickup opportunity.
  Preserving independent refill does not imply a new substitute refill job.
- Native policy copy creates new entries, while save/load deep-serializes them. Current S029 storage
  retains surviving row identity and snapshots seven native choices, with no tolerance codec or
  side-state rollback extension. Its new-row defaults reflect native initialization followed by
  S029 normalization, not unqualified native defaults. Current storage SHA256:
  `DCC407D9A21BA2E9A136C87F82D41B24348CC4FC57C4A000CE75E3DE569A293B`.

The owned draft now makes social drinking obey the tolerance choice without direct-recreation
exceptions, preserves eligible drinkless social activity, and distinguishes independent refill
from pickup attached to a withheld scheduled dose. These are explicit owned clarifications based
on the observed routes, not claims that the original mod already enforces all of them.

DT-Q2 remains open for final recheck timing, threshold changes during an executing routine job,
purpose/recipient proof across pickup and save/resume, the future S024 treat boundary and effective
native admission. No blanket ingestion veto or new cancellation policy is inferred. DT-Q1 remains
open for the owned codec, invalid-record preservation, copy and S029 prepare/commit/rollback
extension. Any/zero/default-on/malformed-choice review and seven-language native UI acceptance also
remain open. No original API/consumer compatibility work, tests, builds or game launches were added.
