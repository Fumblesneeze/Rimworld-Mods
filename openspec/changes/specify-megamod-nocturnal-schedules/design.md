## Context

Sole owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
S070 replaces Automatic Night Owl (`ben.automaticnightowl`, Workshop 2056012179). Complete selected
binary and bounded native/consumer evidence are in
[research](../../../mods/megamod/docs/refinement/2026-11-04-automatic-night-owl-research.md).
No runtime implementation or compatibility acceptance is delivered by this design.

## Goals / Non-Goals

**Goals:** one-time useful nocturnal schedules, precise qualification and lifecycle, preservation of
player/provider schedules, consistent common settings and shared native timetable state.

**Non-goals:** continuously manage sleeping, change genes/traits/mood/rest, force jobs, add pawn
settings or a new Schedule tab, dynamically reschedule after conversion/gene edits, or import source saves.

## Decisions

### Native timetable, one initial decision

Use the existing 24-entry Pawn_TimetableTracker. Default proposed template is Sleep at 11–18 inclusive
and Anything elsewhere, matching source; settings represent start hour 0–23 and duration 1–24 whole
hours, default 11/8. Wrapping midnight uses the same cyclic local-hour interpretation as the native
schedule. Store start/duration as one common composite value to avoid mixed inherited half-templates.
No changing TimeAssignmentDefs, CurrentAssignment, native needs or thought calculations.

Eligibility: living player-faction humanlike with supported timetable, not a colony prisoner or
subhuman. Player-owned slaves qualify after confirmed enslavement; their temporary native inability
to act does not become a force-work bypass. Children with supported timetable state qualify too;
native/S031 care and Schedule visibility retain their authority. Animals, mechs, enemy/visitor-only
pawns and unselected starting candidates do not receive this feature's schedules.

NightOwl trait is the Core criterion. Optional Ideology uses actual current prefersDarkness precepts;
optional Biotech uses active dislikesSunlight genes through the admitted native contract. Multiple
matches produce one decision, not stacked or differently ordered templates. Null optional state is
nonqualification, not a reason to crash or skip an independently qualifying Core trait.

### Preserve a customized timetable before and after first application

Inspect the complete existing schedule before mutation. The currently inspected native initial
baseline is Sleep at 22–05 and Anything at 06–21. A non-baseline schedule is preserved and recorded
as decided rather than rewritten. A previously observed native manual/provider assignment also
preserves the pawn, even if its final values equal that baseline. Historical edits made before our
tracking existed cannot be reconstructed from identical values; document this limit explicitly.

Track successful ordinary SetAssignment calls and native timetable PasteTo as explicit choices;
copying/viewing alone is not a choice. The owned initialization writes its prepared complete native
list through a narrow owned path without interpreting itself as a manual edit. Never instrument
every getter or infer a pawn edit from another feature's temporary CurrentAssignment override.
The native paste route writes list elements directly, so SetAssignment alone is insufficient.
Any actually needed foreign editor adapter is separately package/shape-gated; arbitrary direct
foreign writes equal to baseline without a consumed intent contract are not claimed distinguishable.

Once applied or preserved, do not overwrite that pawn on load, respawn, travel, re-recruitment,
subsequent settings changes or eligibility loss/regain. An unqualified unedited pawn remains eligible
for its first decision at a later supported lifecycle event; it is not rescanned merely because a
trait, gene, ideology or setting changed. This preserves source event scope while correcting first-
application destruction of custom schedules. No per-pawn enable/reset control is proposed; ordinary
Schedule editing supplies deliberate changes after the initial decision.

### Observe completed lifecycle with loaded state available

Source patches Thing.SpawnSetup, six-argument DoRecruit and EnslaveAttempt.Interacted. Native
Pawn.SpawnSetup calls base before AddComponentsForSpawn, so observing the base callback is too early
to rely on all pawn state. Prefer a supported completed pawn/control transition, followed by one
owned consideration routine. Starting GameInitData.PrepForMapGen supplies selected player components;
standing, pods and Odyssey gravship eventually spawn those pawns. RecruitUtility.Recruit and
Pawn.SetFaction finalize player components; guest slavery changes also route faction and guest state.
Do not label failed recruitment/will reduction or a prefix callback as successful control transfer.

Choose exact minimal hook sites after target validation; no need to patch every overlapping path.
Cover starting/new player pawn spawn and successful recruit/enslave events including unspawned pawns
with a ready timetable. Coalesce nested notifications into a single final-state consideration. On
load, delay consideration until game identity, owned history, pawn references, settings and timetables
are fully restored. A bounded set of pending spawn/control notifications suffices, not a repeating
world-pawn scan. First insertion into an old save can consider its loaded player pawns: preserve
non-baseline/known custom schedules, initialize only otherwise eligible baseline schedules. Existing
off-map pawns need a later supported event; no new all-world catch-up pass is selected.

### Owned history and atomic mutation

Use game-owned persisted pawn decision state, not the source static cross-world HashSet. Retain it
through travel, imprisonment, faction loss/rejoin and ordinary save/load; prune only discarded/dead
references when safe for supported resurrection behavior. Prefer retaining a small durable record
over incorrectly treating a resurrected pawn as new. Missing state in a genuinely pre-feature save
is distinct from malformed known owned history: corrupt/unresolvable history disables affected
automatic decisions with a bounded load diagnostic, preserving native schedules.

Prepare and validate all 24 entries before committing. Capture original timetable and decision state
as the in-memory rollback snapshot. Commit the whole list and decision together on the game thread;
verify all assignments and history postconditions. On failure restore the original schedule/state,
report once and do not mark success or retry every tick. No file-level save rewrite/import occurs.
Actual migration, if later selected, needs separate durable backup, atomicity, restoration and tested
postconditions. A valid native schedule persists without source class identities, but removing our
history and then reinstalling can lose intent; do not promise safe remove/re-add or migration.

### Configuration and UI

Common **Pawns / Initial schedules** rows: Include darkness-preferring ideologies=true, Include
sunlight-disliking genes=true, Initial sleep window=11:00 for 8h. Source fresh-object false versus
loaded missing-key true is explicitly corrected to consistent true. Optional unavailable DLC rows
are explained, non-operative and retain values; absence does not break Core qualification. All
available settings use Global default / Current save / Use global, dormant overrides, first override
seeding, main-menu callback guards and save switching. Start/duration appear as one scoped row.
Accepted changes affect future undecided candidates, not already assigned/preserved schedules.

Settings explain one-time scope, custom timetable preservation and the historical baseline limit.
Pawn state remains in native Schedule, including S031's meaningful child rows. No extra bottom tab,
toolbar toggle, debug action, schedule editor or raster art. Seven-language text required. No base
NightOwl switch is introduced; original's Core criterion is always considered while admitted.

### Semantic admission and provider authority

Before implementation document stable target IDs for actual patches and executable minimal checks:

| Group | Meaning to establish | Failure |
| --- | --- | --- |
| Core lifecycle/history/choice preservation | Completed supported spawn/control transition, ready native timetable, restored owned history, native edit/paste intent and atomic native assignment | No owned automatic schedule writes; ordinary Schedule remains available |
| Ideology criterion | Correct active DLC/content and actual precept darkness preference semantics | Criterion unavailable; Core and admitted Biotech still function |
| Biotech criterion | Correct active DLC/content and active sunlight-disliking gene semantics | Criterion unavailable; Core and admitted Ideology still function |

Choice preservation is mandatory for the mutation group, not an optional enhancement to skip when
its observer fails. Effective foreign patches, skip-original prefixes, relevant transpilers and later
repatch lifecycle must be accounted for. No owner blacklist, exact assembly/version/hash or whole-IL
matching as sole admission. Hashes identify evidence only. A postfix that consumes only a confirmed
final state needs its minimal callable/observable contract; required original writes need stronger
semantic evidence. Any transpiler validates incoming instructions and returns them unchanged on
failure. Do not speculatively re-run foreign transpilers.

False/unknown predicates, validator exceptions or install failures leave the affected group inactive,
rolling back newly owned patches only. One bounded startup warning per skipped enabled patch names
package/patch/spec/target, reason, unavailable behavior and native fallback. Explicit disabled optional
criteria and absent DLC are ordinary inactive conditions with UI explanation, not repeated warnings.
Later revocation prevents new automatic writes and preserves existing native timetables/history.

Original ben.automaticnightowl active: it alone supplies overlapping automation; owned decision/edit
tracking and writes are dormant, preferences retained, UI explains source authority and that source
has different custom-schedule/default behavior. Do not promise our preservation corrections while
source owns writes. When it is subsequently absent, consider native saved schedules under our rules
without importing its pawn-reference list or pretending its past choices are known. No package/type
spoofing, source settings edits or foreign unpatch. The bounded source-consumer search found no actual
runtime binding requiring a facade. Prepare Carefully's source-advertised support remains unverified;
its installed provider was not found, so do not invent a new adapter or compatibility claim.

### Direct owned composition and costs

S001 outlines the resulting stored timetable; S002 can temporarily change job handling without
rewriting it or creating a new initial decision; S031 child care uses the same state and keeps normal
rest/medical/safety behavior. No owned module patches another. No private pawn/map scan, extra tick
loop or shared Thing-index dependency; this is per-pawn lifecycle history, not cached world content.
Benchmark combined notification, save/load and edit costs before claiming any optimization.

## Risks / Trade-offs

- Correcting first-application overwrite means some existing pawns are deliberately left custom → explain and retain native editing.
- Baseline-looking old manual intent is unknowable → state the exact boundary, do not claim perfect historical recovery.
- Source defaults differ by settings lifecycle → choose and test explicit true/true defaults, with correction documented.
- Multiple startup/control callbacks or early-load state can overwrite choices → coalesce and require restored history before any write.
- A configurable daytime sleep period can reduce work opportunities or be suboptimal for another gene → no promise of optimal scheduling or forced sleep.
- Original/foreign schedulers may own writes → truthful provider authority and guarded adapters; no broad compatibility claim.

## Migration Plan

Documentation only. Implement vertical RED/GREEN slices, native schedule/history mutation, common
configuration and admission, then independent review and fresh native player workflows on the reviewed
build. No source import, ordinary-save rewrite, dependency edit, build or game run now. Rollback preserves
existing schedules and user edits; disabling a patch does not restore a previously overwritten timetable.

## Open Questions

ANO-Q1–4 retain preservation/default/template choices, exact beta lifecycle/history/admission targets,
source/provider/migration scope and focused tests/localization/cost/native acceptance. No open question
licenses continuous scheduling or rewriting customized pawns.
