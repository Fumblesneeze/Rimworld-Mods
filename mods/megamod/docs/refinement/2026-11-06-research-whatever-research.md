# S120 — Research Whatever research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin replacement **S120/P2-50**. Documentation-only deep dive on2026-11-06; no runtime code,
tests, build, installation, launch, Workshop changes, normal save/config writes or acceptance.
[Change](../../../../openspec/changes/specify-megamod-automatic-research/) ·
[Coverage](../replacements/research-whatever.md) · [Ledger](specification-progress.md).

## Public documentation first and exact inputs

[Steam Workshop2552092060](https://steamcommunity.com/sharedfiles/filedetails/?id=2552092060)
describes automatic inexpensive project choice when research is unselected, with bench control,
completion notification choices and Anomaly study support. Its
[changelog](https://steamcommunity.com/sharedfiles/filedetails/changelog/2552092060) identifies
1.2.5's captured-entity study-resume correction. Earlier comments describing the hidden disable bug
are not evidence that the current version still contains that same bug.

Then inspected the installed full selected DLL, metadata, XML and English catalog, and pinned
[public repository](https://github.com/catgirlfighter/RimWorld_ResearchWhatever/tree/7a6cb2da7873e214194408b6ec32e80024fa5652).
The repository's `main` commit **7a6cb2da7873e214194408b6ec32e80024fa5652**,2026-10-16,
is labeled1.2.5. All17 retained source/metadata files match their tree Git blobs. The selected
installed binary's Git blob **a69932fb0b6025ef8ce7efcc420fedea27163aca** matches that tree's1.6 DLL.
Source and decompilation agree on the behavior inventory below. An initial `/commits/master`
request returned422; the retained successful `main` response is the provenance source.

| Input | Identity |
| --- | --- |
| Name / author / package | Research Whatever; avilmask (Steam avil); `avilmask.ResearchWhatever` |
| Installed root | `F:/Steam/steamapps/workshop/content/294100/2552092060` |
| Declared versions |1.3–1.6; no1.7 claim |
| Selected branch | Root plus1.6 via native closest-version fallback; conditional1.6/Mods/vanillaexpanded.gravship if that provider is active |
| DLL | `1.6/Assemblies/ResearchWhatever.dll`,18,432bytes, assembly1.0.0.0 |
| DLL SHA256 | `30C8E1CDE0EFA62F265D06FDEA441664A0DBED91609FD87D3E8786ABBDC7B8DA` |
| Source provenance | `Source/ResearchWhatever16/`, plus About/loadFolders/change.log; version is changelog1.2.5, not assembly version |
| Source dependencies/order | Required Harmony; after`vanillaexpanded.gravship`, before`PeteTimesSix.ResearchReinvented` |
| Native game |1.7.5000rev1275; root`Assembly-CSharp.dll`,15,636,480bytes |
| Native SHA256 | `D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254` |

The source repository exposes code but no inspected LICENSE/COPYING grant; repository API license
is null. Record attribution and independently implement behavior. Do not copy source code, images,
PDB/DLL, bundled dependencies or public identity into the product.

Evidence is ignored/local under `artifacts/Refinement/megamod-spec-20261106/research-whatever/`:
19 exact input identities,6 public response records (one unsuccessful),17 pinned upstream files,
complete selected decompilation,10 successful native type extracts and one empty wrong-namespace
attempt excluded from evidence, plus2 Research Reinvented type extracts. Failed Anomaly data lookup
is also explicit: this installation's Data folders contain Core/Royalty/Ideology/Biotech/Odyssey,
not Anomaly. Anomaly code exists in the game assembly; loaded DLC Defs and workflows remain unverified.

## Complete source behavior inventory

| Surface | Source behavior |
| --- | --- |
| Mod startup | Unconditional Harmony`PatchAll`, owner`net.avilmask.rimworld.mod.ResearchWhatever` |
| Static-constructor hook | BetterLoading`StageRunStaticCctors.PreCallAll` if found, otherwise native`CallAll`; adds a comp to every research-bench class/subclass with a non-null comp list; no deduplication |
| Bench comp | `active=true`, effective only for player faction; saves`active`defaulttrue; selected bench toggle uses native quest icon and TogglePower hotkey |
| Bench right-click | Four completion modes in a floating menu; preference is game-wide despite contextual entry point |
| Game component | `NotifyMode` saves enum Default0/Letter1/Notice2/Mute3; fresh/missing default0 |
| Ordinary scanner | Prefix forces`ShouldSkip=false`; request getter always returns ResearchBench group, replacing originals |
| Ordinary choice | In`HasJobOnThing` before native eligibility, if no normal project and bench comp active, choose available positive-baseCost research, set global project, play research start sound/tutorial event and quiet pawn/project/bench notice |
| No ordinary candidates | Disable that bench and send neutral no-research notice |
| Ordinary policy | Exclude`ignore`, put`lowPriority`behind others with an additive100000000 score penalty |
| Anomaly choice | Positive knowledge only; respect selected primary/immediate overflow project; otherwise primary available category then immediate overflow; start sound/tutorial/quiet notice |
| Anomaly exhaustion | Require local finite study complete or non-completable, study unlocks complete/absent, and unnatural-corpse destruction-by-research ready; pause ordinary object or set captured target MaintainOnly |
| Completion prefix | Only caller-requested dialogs; Default native, other modes suppress argument then emit positive letter/quiet notice/nothing before actual completion |
| Optional XML | Low-priority2 repeatable projects; ignore11 gravtech projects in provider-conditional folder |
| Localization | One English catalog,9 keys; no bottom tab, settings page, custom research tree or owned Thing index |

There are six actual Harmony targets: startup, ShouldSkip, PotentialWorkThingRequest getter,
HasJobOnThing, StudyAnomaly and FinishProject. No transpiler or separate research job is supplied.
The public extension name is **`ResearchWhatever.ResearchWhateverExtansion`**, deliberately retaining
the source spelling in this inventory. Its public bool fields are`ignore`and`lowPriority`.

### Eligibility and choice details

Ordinary source filtering requires`baseCost>0`, `CanStartNow`, difficulty`AllowedBy(hideWhen)`,
and either no required bench type or exact bench type plus its custom active-facility test. Thus
facilities are skipped when requiredResearchBuilding is null. Source does not call actual
`CanBeResearchedAt` before selecting, nor check pawn access/reservation/ideology or power there.
Native eligibility runs afterwards and can refuse the job while leaving the chosen global project.

Sorting uses apparent cost minus apparent progress. But random tie selection then admits every
candidate with the first candidate's **total apparent cost**, not its remaining cost/priority.
Two1000-cost projects with900/0 progress can both win. Low-priority candidates with the same total
cost also enter that tie. A100000000 penalty is not a strict ordering for arbitrary large costs.
These are concrete code-derived paths, not claimed observed gameplay failures.

Anomaly filtering uses`CanStartNow`and category only; it omits difficulty`hideWhen`and both extension
flags. Its sort/tie mismatch is the same. It checks only the immediate overflow category for its
selection; native knowledge routing itself has separate overflow behavior. Source notifies using
studier.Name and target or immediate Thing holder without the native method's null-target guard.

### Exhaustion and the current1.2.5 correction

Source does not pause merely because no global project is available. It also checks
`CompStudyUnlocks.Completed`, finite`CompStudiable.Completed`and unnatural corpse
`Tracker.CanDestroyViaResearch`. Non-completable study counts as locally complete for this decision.
Current source avoids setting`studyEnabled=false`when`CompHoldingPlatformTarget`exists and instead
sets its Study mode to MaintainOnly. This addresses the already-reported captured-entity hidden
switch problem; do not present that fix as newly discovered missing behavior.

For ordinary subjects the source writes`studyEnabled=false`without proving an exposed toggle.
Native`CompProperties_Studiable.showToggleGizmo`defaultsfalse; native prisoner and containment paths
have separate restrictions. Unknown subjects therefore need a real reversible-control check, not
an assumption that every comp is user-resumable. Extra provider research-dependent outcomes are not
covered by the source's three hardcoded checks. Source can emit an exhaustion message even without
a real state transition. Native `StudyAnomaly` still executes after the prefix, so positive knowledge,
subject notification and signal are not intentionally suppressed by current source.

### Completion modes and persistence

Default retains the requested native dialog. Letter supplies positive ResearchFinished plus the
project description; Notice uses SilentInput; Mute suppresses only that requested presentation.
The setting affects manual/automatic normal and Anomaly completions. It does not suppress independent
`doCompletionLetter` discovery letters, automatic-start notices or exhaustion notices. Source sends
replacement success messages before the method completes; owned presentation must observe success.

Source state is per-bench`active`and per-game`NotifyMode`, not a global default. No migration utility
or source settings import exists. Owned common settings and contextual state require separate
serialization; no silent import, package spoof or blanket safe-removal claim is justified.

## Native1.7 contracts and actual optional inputs

Native`WorkGiver_Researcher`skips with no ordinary project and only requests benches with one.
`HasJobOnThing`requires actual`CanBeResearchedAt`, reservations for bench/interaction cell and the
Researching history event. The scanner/driver add native access and job lifetime constraints.
`JobDriver_Research`actually reserves, walks to the bench, performs native research, Intellectual
learning and chair comfort, with current-project/bench failure checks. Owned prospective eligibility
must not duplicate side effects or publish a temporary global project just to reuse these checks.

Native`ResearchProjectDef.CanStartNow`combines unfinished state, prerequisites/hidden prerequisites,
techprints, needed bench availability, mechanitor/analysis/inspection and hidden-state restrictions.
`CanBeResearchedAt`checks exact required building, power and active facilities independently.
Apparent cost/progress include the colony's tech-level factor. `SetCurrentProject`can set both
normal and category slots for a hybrid project; ordinary and category selections must not silently
overwrite one another. `FinishProject`does more than show a dialog: prerequisites/progress, unlocks,
tales, optional power recalculation, discovery letters, signals and clearing current selection.
Native `ConfigErrors` rejects simultaneous positive baseCost and knowledgeCost. Occupied-slot
protection is necessary but does not prove mixed progress/completion support; malformed hybrids are
unsupported and actual provider-specific mechanisms remain ARW-Q2.

`StudyManager`already owns map→studiable HashSets. Native`CompStudiable.Study`awards local progress
before Anomaly knowledge and then performs further completion/learning effects. Native study
notifications drive local unlock journals. Reusing native facts avoids duplicate caches and double
progress; current-action consequences must survive a decision to pause subsequent work.

| Installed/reference provider | Evidence and limit |
| --- | --- |
| Vanilla Gravship Expanded Chapter1,Workshop3609835606,`vanillaexpanded.gravship` | Inspected1.7 research XML: gravtech extension/progression and altered base costs, including BasicGravtech50; source's11 exclusions are purposeful ordinary-bench policy, not research deletion |
| Research Reinvented,Workshop2868392160,`PeteTimesSix.ResearchReinvented` | DLL355840bytes,SHA256`A3292C14B384D244F13AB96CB4899F48A54AA92794F9E1DB68FE2E50EC692749`; actual vanillaShouldSkip prefix forcestrue; separate ResearcherRR uses opportunity selection/restrictions |
| Researchable Stat Upgrades,`firefoxpdm.ResearchableStatUpgrades` | Source XML targets StackingRepeatable/ProductivityRepeatable; exact package tag search found no installed About match, so provider-present content/runtime is unverified |
| BetterLoading | Source's name-based alternate startup hook is inventoried, not adopted; guarded declarative comp addition removes that startup dependency |

Source's repeatable XML tests only whether modExtensions exists then adds an extension, with
successAlways; no package check or deduplication. Gravship's conditional folder gates package but
the XML assumes all11 targets. Owned XML must preflight package/content/shape, preserve existing
lists and avoid partial policy activation. This does not authorize copying the external Gravship
mod's own prerequisite removals or cost replacements.

Installed XML/C#/JSON/Markdown name searches found source files and RimPy catalogs/rules, plus a
Research Reinvented Russian translation file named ResearchWhatever. That file labels its own
VisualScience settings preset; it is **not** a consumer of this source extension. Binary text search
found ResearchWhatever identifiers only in its four versioned DLLs. These bounded searches establish
no actual third-party compiled/reflected extension consumer; they do not prove none exists elsewhere.
Research Reinvented is instead a demonstrated alternate work path needing explicit admission.

## Proposed owned contract and deliberate differences

| Area | Decision |
| --- | --- |
| Selection timing | Prospective facts only during probing; commit for admitted native work with current reservations/access/selection; no unusable-bench side effect |
| Choice order | Exclusion then strict priority class then least remaining apparent work; random only true ties; no100000000 constant |
| Facilities | Check actual bench/facilities even with no required bench type; preserve all native progression/work restrictions |
| Anomaly policy | Consistent visibility/exclusion/priority rules, occupied-slot safety and bounded category inspection; a proposed extension beyond source ordinary-only policy |
| Exhaustion | Retain explicit per-bench off/re-enable; pause study only with complete known local outcomes and a working resume control; notice only real transition |
| Notification timing | Replace actual successful dialog presentation, not a prefix success prediction; preserve independent letters and all completion effects |
| Preferences | Common inherited completion mode, contextual bench/subject choices, seven languages; no hidden right-click settings or new tab |
| Optional policies | XML Extensions package/content/shape checks, merge/idempotency and coherent fallback; required runtime metadata with actual use |
| Co-installation | Original active selects original; alternate worker cannot be bypassed; no source identity facade without a proven consumer |
| Data/lifecycle | Native research/study/listers, transient decision facts; shared owned message/link composition; no new persistent index or replay |

## Static comparators and semantic pass

These are source XML/code anchors, **not finalized loaded-game measurements**:

| Comparator | Current static value / constraint | Implication |
| --- | --- | --- |
| Core Stonecutting |300 base work, Medieval | Cheap native available project; tech penalties still apply |
| Core Microelectronics |3000 base work, Industrial, Electricity prerequisite | Automatic selection must respect progression, not just cheapest number |
| Core Fabrication |4000 base work, Industrial, MultiAnalyzer prerequisite, HiTechResearchBench and active MultiAnalyzer; independent discovery letter | Facility and independent-letter acceptance case |
| Installed VGE BasicGravtech | Its provider XML replaces baseCost with50 and changes progression | A cheap special project is not automatically ordinary bench research |
| Selection counterexample |1000−900=100 versus300−0=300; equal total1000/1000 does not imply equal remaining work | Correct source tie leak without altering project costs |

Automation changes the order in which unattended research advances; it does not grant progress,
new knowledge, speed, techprints, XP or research unlocks. Low-priority repeatables remain last,
gravtech stays with its provider and player choice wins. Study exhaustion cannot discard local
story benefits, captured-entity maintenance or working resume controls. Common notification mode
does not remove Fabrication's independent guidance. Finalized values, optional-provider availability,
Anomaly outcomes and combined work-search cost need implementation-time native measurement.

## Open work and verification

| ID | Remaining work |
| --- | --- |
| ARW-Q1 | Effective target/commit/presentation boundaries, prospective evaluation without side effects, coherent groups and later revalidation |
| ARW-Q2 | Optional policy content/consumers, Research Reinvented adapter, category/hybrid contracts, Anomaly subject controls/outcomes and any separately justified migration |
| ARW-Q3 | Finalized progression/cost/facility and Anomaly measurements, common/contextual UI in seven languages, multi-map selection and combined performance |
| ARW-Q4 | Focused TDD, reviewed package and causal native bench/study/resume/completion/save/provider workflows |

Spec/schema review does not prove gameplay. All implementation tasks remain open. Final acceptance
must show real researchers starting work, actual completed research with the selected presentation,
real positive study and correct pause/resume, and player settings/entity actions on the reviewed
minimized isolated build. Inspect causal views personally and retain actions, ordered mods/build,
screenshots/logs, normal-config hashes and cleanup. Direct project/progress assignments are setup
or diagnostics only, never proof of automatic selection, knowledge delivery or completion.
