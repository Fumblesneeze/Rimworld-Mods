# S070 — Automatic Night Owl research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
Read-only inspection **2026-11-04**. Origin: replacement. **Observable draft independently reviewed
and validated. No implementation or gameplay acceptance.**
[Change](../../../../openspec/changes/specify-megamod-nocturnal-schedules/proposal.md).

## Primary documentation and exact source

[Author Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=2056012179)
was read before source/decompilation. It describes schedules for starting/recruited/Prepare Carefully
pawns and optional Ideology darkness preference and Biotech sunlight-disliking genes. The inspected
page lists a 1.6 update; installed About and LoadFolders explicitly include 1.7 and route it to root.
This disagreement is recorded, not treated as a verified compatibility claim. Generic Steam
removed/incompatible boilerplate is not evidence of actual removal or game compatibility.

Installed **Automatic Night Owl**, author **Ben**, package **ben.automaticnightowl**, Workshop
**2056012179**, root `F:/Steam/steamapps/workshop/content/294100/2056012179`.
Selected `Assemblies/AutomaticNightOwl.dll`, assembly/file **1.0.0.0**. Its recruitment attribute
references a 1.6 Assembly-CSharp build. Required Harmony in metadata. Source_1.2/1.3/1.4 trees exist,
including old build/dependency files, but are not presumed authoritative for the selected binary.
No author repository/license link found in inspected metadata/docs; vendor dependency licenses do
not grant rights to this mod. No code/art/binary copied into product or source assembly executed.

| Input | SHA-256 |
| --- | --- |
| About/About.xml | `50196B0B27CDF84C51FC64DC00D8DCEE8F10CA20CDF0EF75EAA9261601E070C3` |
| LoadFolders.xml | `44315D1131E2260F406FC85CC365979F1C35C9AEC3119B5C96A54D8FB4A9B3FB` |
| Root selected DLL | `89B442F2B6B7451916AF8AE495CD37AA3F6C57A3D2FC125F53763F925DD984F3` |
| Native 1.7.4998 rev1299 Assembly-CSharp.dll | `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |

Offline ILSpy artifacts: `artifacts/Refinement/megamod-spec-20261104/automatic-night-owl/`, with
complete selected binary under upstream, dependency-resolved main class under resolved, raw main
class IL under il, native extracts under native and consumer-paths.txt. No game launched.

## Complete selected-binary behavior inventory

All five top-level classes were read. Nested patches and unused Def holders are in AutomaticNightOwl.

| Surface | Actual source behavior |
| --- | --- |
| Initializer | Startup PatchAll under AutomaticNightOwl_Ben, no semantic guards |
| Thing.SpawnSetup postfix | Any spawned player-faction humanlike pawn is considered, including load/respawn; does not inspect respawningAfterLoad or existing timetable contents |
| RecruitAttempt postfix | Successful player-faction humanlike candidate after DoRecruit; attribute selects the six-parameter overload |
| EnslaveAttempt.Interacted postfix | If recipient is non-null and GuestStatus is Slave, consider it; no explicit player-faction/humanlike gate in this patch |
| Night Owl qualification | NightOwl trait, timetable exists, pawn not already handled |
| Ideology qualification | DLC active, AddOn_Ideology, non-null ideology, native IdeoPrefersDarkness, timetable, not handled |
| Biotech qualification | DLC active, AddOn_Biotech, genes.EnjoysSunlight false, timetable, not handled |
| Schedule mutation | Replace all 24 hours: Sleep for hours 11 through 18 inclusive (11:00–19:00), Anything otherwise |
| Handled tracking | Add pawn to static WorldComp.PawnsWithNightOwl only after replacement; no record for unqualified pawns |
| Persistence | WorldComponent serializes pawn references under pawnsWithNightOwl; constructor/load clears global static set, post-load reconstructs non-null references |
| Mod/settings | Global window with two checkboxes; no base-trait switch, time editor, operational toggle, bottom tab or per-pawn auto control |

The recruitment attribute was not decoded by ILSpy's C# output even with native references. Raw IL
contains DoRecruit and six parameters: Pawn, Pawn, string, string, bool, bool, with the two string
arguments marked out. Current native 1.7 has that exact overload and a shorter wrapper. This is
static target identification, not proof that the old binary patches the current runtime.

All qualifying reasons use the same schedule and share handled tracking, so matching multiple reasons
does not apply multiple templates. Handled pawns' later manual edits survive subsequent source calls
while tracking remains. Source does not continuously respond to trait, gene or ideology changes;
an unhandled pawn can qualify at a later spawn/recruit/enslave callback. There is no new event hook
for conversion or gene addition. Existing nondefault schedules are overwritten on first qualifying
callback. The broad spawn callback can affect existing pawns after installing into an old save.

The source's save-add/remove assurance is a documentation claim, not accepted evidence. Removing
its component does not inherently undo a native timetable already written. Losing/recreating its
handled list can make reapplication possible. Source PostLoadInit iterates the exposed list without
a null guard; exact missing/legacy component behavior still needs native Scribe analysis.

## Settings-default discrepancy and UI audit

AddOn_Ideology and AddOn_Biotech have no field initializers (false in a new object), but ExposeData
uses true as both missing-key defaults. Inspected 1.7 LoadedModManager.ReadModSettings returns new T()
when no file exists without loading keys, so fresh configuration takes false, while missing keys in
a loaded object take true. This is a static derivation, not a live UI observation. Do not describe
the source as having one reliable true default.

Only English localization is supplied. Labels call the Biotech option Dirtmoles / Vampires, but actual
selection is active sunlight-disliking genes rather than xenotype identity. Unused Tunneler/Dirtmole
Def holders do not determine eligibility. Optional controls belong in common mod settings; the native
Schedule grid remains pawn gameplay state, not a per-pawn inherited setting. Seven languages and
clear active-DLC/availability explanations will be required. No source bottom tab to relocate.

## Native 1.7 evidence inspected so far

- Pawn_TimetableTracker starts with Sleep at 22–05 inclusive and Anything at 06–21. CurrentAssignment
  uses the pawn's local hour and returns Anything when not a colonist or when a colony prisoner.
  SetAssignment changes one cell; times is natively serialized. Schedule entries are permissions,
  not a guarantee of sleep, rest gain, protection from sunlight or absence of emergency work.
- Core NightOwl trait description gives awake-at-night benefit at 23–06 and daytime penalty at
  11–18. Fresh native thought-worker inspection resolves those as integer hours 23–05 and 11–17
  respectively, each requiring Awake. Source's eight-hour Sleep block 11–18 includes one extra hour
  beyond the current daytime-penalty interval; it does not reduce sleep need or change those thoughts.
- IdeoPrefersDarkness returns true for any current precept whose Def prefersDarkness. Do not special-case
  a displayed meme name or infer a bug from Workshop reports about external Light memes without inspecting them.
- EnjoysSunlight returns false if a current active gene has dislikesSunlight; overridden/inactive genes
  alone do not qualify. No hard-coded Dirtmole/Sanguophage list is necessary.
- Current DoRecruit delegates to RecruitUtility.Recruit before reports/letters. EnslaveAttempt only
  calls TryEnslavePrisoner after its normal will/interaction gates; unsuccessful will reduction is
  not enslavement. Actual successful player control, starting setup and timetable assignment ordering
  still need deeper inspection before choosing owned lifecycle targets.

## Consumers and composition

A completed bounded installed XML/CS/JSON identifier search found only source files/metadata, RimPy
database/rules and LayoutHarness sample mod lists. No actual runtime consumer found in that search;
it is not an all-binary/reflection audit. Source settings class/static helpers and WorldComp saved
identity are not reasons to spoof a package or invent a facade. Source-specific saved handled state
and settings import need an explicit decision before any migration promise.

S001 mood-bar schedule outline and S002 directed recreation read timetable intent/effective behavior;
ordinary Schedule editing remains authoritative. S031 toddler development requires lifecycle review
for when a pawn has a usable controllable timetable. Starting setup and custom schedule providers,
including Prepare Carefully, must preserve proven external contracts and avoid double initialization.
No private pawn/map cache is indicated: handled initialization state is gameplay lifecycle data, not
the shared Thing search/haul index. No performance claim is accepted.

## Further native lifecycle and choice evidence

Fresh excerpts of GameInitData, ScenPart_PlayerPawnsArriveMethod, Pawn, PawnComponentsUtility,
RecruitUtility, GenGuest, Pawn_GuestTracker and native timetable row/copy-paste workers were inspected.
GameInitData removes unselected candidates from the starting set, retaining them as world pawns
with KeepForever, and gives selected player pawns dynamic components.
The arrival part handles standing/drop-pod/gravship placement; native schedules are not rewritten by
that part. Pawn.SpawnSetup calls the base Thing method before AddComponentsForSpawn, making a source-
style base observer an unnecessarily early place to rely on ready pawn state. Exact owned hook choice
remains implementation research, not a claim all native paths are admitted.

RecruitUtility clears guest state and changes faction before recruitment notification. Pawn.SetFaction
adds/removes dynamic components after the new faction is assigned. Player humanlikes receive a timetable
if missing; no age filter in that component creation. Successful TryEnslavePrisoner changes guest status,
which can set player faction and dynamic components; failed attempts remain separate. Colony slaves'
normal ability to follow schedules also depends on native secure/control state. Do not force behavior
merely because a schedule exists.

Native Schedule filters out Baby developmental stage; S031 supplies meaningful child rows/care as its
own feature. Timetable row drawing excludes subhumans. Native edits call SetAssignment; native PasteTo
writes all 24 existing list entries directly, so watching SetAssignment alone misses pasted intent.
Viewing/copying is not assignment. Known manual edits that return to baseline need retained intent,
not a one-time final-value comparison; old baseline-looking history cannot be reconstructed.

Installed About-name search did not locate Prepare Carefully itself; the only text match was another
mod's description. No provider binary was inspected and no provider-specific compatibility is claimed.
The source's generic player-humanlike spawn callback explains its advertised broad initial support
without proving all current setup-provider orderings.

## Selected draft decisions and intentional deviations

- Retain source any-match Night Owl / enabled actual darkness-precept / active sunlight-disliking-gene
  qualification and one complete 24-hour native timetable. Consistent optional defaults true/true are
  selected explicitly, correcting fresh-source false versus missing-key true. No xenotype-name whitelist.
- Default Sleep 11:00–19:00, Anything otherwise; expose start 0–23 and duration 1–24 whole hours as one
  common scoped window (built-in 11/8), wrapping midnight. Invalid values retain the prior valid window.
  This follows the user's preference for settings over hard-coded tuning, not a newly optimized schedule.
- Preserve a non-baseline initial timetable and known native/manual/provider choices even when equal to
  baseline. Once assigned or preserved, retain that decision through load/travel/rejoin/settings changes.
  This intentionally corrects source first-application blanket overwrite. Historical baseline-equal
  manual or unannounced provider intent cannot be inferred and must be explained honestly.
- Keep event-driven initial consideration: no immediate reschedule on trait/gene/ideology/settings
  changes. Undecided, unedited pawns can qualify at a later supported arrival/control event. First
  insertion into an old save can initialize its loaded qualifying baseline pawns, preserving custom
  ones; no all-world off-map catch-up scan. Delay load work until owned history/settings/references restore.
- Restrict to living player humanlikes with supported schedules, excluding prisoners/subhumans and
  foreign/animal/mech scope. Confirm successful control changes rather than an attempted interaction.
  Preserve ordinary native child/slave needs, care and control semantics without a forced-sleep promise.
- Use game-owned decision history, not a static cross-world set. Capture original timetable/decision,
  commit a complete validated day and verify postconditions; restore both on failure. Corrupt known
  history is not a fresh old save. No automatic source-settings/class/history migration selected.
- Common Pawns / Initial schedules rows, dormant overrides/main-menu/save-switch rules and seven
  languages; existing Schedule for pawn state. No new tab, runtime toggle, debug control or base-trait
  switch. S001/S002/S031 compose through native timetable data, not internal patches.
- Original source active: original alone owns automation; owned preferences/tracking/writes dormant.
  Explain source authority and differences rather than promise our preservation under its writes.
  Minimal semantic patch admission and independent optional-criterion fallback remain mandatory.

## Open questions and semantic pass

| ID | Remaining work / draft direction |
| --- | --- |
| ANO-Q1 | Proposed custom-schedule preservation, historical baseline limit, consistent optional defaults and configurable initial window; record as explicit corrections |
| ANO-Q2 | Exact 1.7 completed lifecycle/load/control and native edit/paste/history targets, effective foreign changes, atomic write and later-repatch admission |
| ANO-Q3 | Original/setup/custom provider authority, missing/corrupt history, source migration and remove/re-add boundaries; no unsupported identity facade |
| ANO-Q4 | Focused RED/GREEN, seven languages/common settings, combined costs and observed native start/recruit/enslave/Schedule/load/travel/child-care acceptance |

Semantic pass: eight hours of sleep permission move from the normal night to daytime, not a change in
sleep requirement or mood effects. Anything does not compel night work. Gene-specific rest, emergencies,
medical needs and child care remain independent. Custom windows can reduce useful work hours and are
not automatically optimal. Preserve meaningful custom schedules and explain the unavoidable old-intent
limit. Core behavior needs Harmony when implemented; DLC optional, no selected XML contribution.

Draft contains **7 requirements, 23 scenarios and 23 unchecked tasks**. Independent source/native
review found no P1/P2 issues; its minor starting-candidate wording correction is applied. Independent
composition review found no issues. Typed strict validation passed **92 items, zero failures**, exit 0
and no stderr. All **8 scoped Markdown documents** passed local-link and whitespace checks. Queue
question IDs remain unique. No runtime code/Defs/assets, tests, build/install, game launch, upstream
execution, Workshop edits or normal save/configuration mutation.
