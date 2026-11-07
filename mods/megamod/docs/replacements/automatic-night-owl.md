# Automatic Night Owl — replacement coverage

## Identity and provenance

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
Origin replacement, S070/P1-64. Source **Automatic Night Owl**, author Ben,
`ben.automaticnightowl`, [Workshop 2056012179](https://steamcommunity.com/sharedfiles/filedetails/?id=2056012179),
selected root assembly/file **1.0.0.0**, inspected **2026-11-04**. Installed metadata routes 1.7 to
root; inspected Workshop page describes 1.6. Neither proves compatibility. No mod license/repository
link found in inspected metadata/docs; vendor dependency licenses do not cover source-mod reuse.
No code, binary or art copied into product. Exact hashes, complete binary/native evidence and
source/default discrepancies are in [research](../refinement/2026-11-04-automatic-night-owl-research.md).

Status **observable draft independently reviewed and validated. Nothing implemented.**
[Proposal](../../../../openspec/changes/specify-megamod-nocturnal-schedules/proposal.md),
[design](../../../../openspec/changes/specify-megamod-nocturnal-schedules/design.md),
[spec](../../../../openspec/changes/specify-megamod-nocturnal-schedules/specs/megamod-nocturnal-schedules/spec.md),
[tasks](../../../../openspec/changes/specify-megamod-nocturnal-schedules/tasks.md).

## Behavior inventory and coverage

All rows are specified, **none implemented or verified**. Requirement names correspond to the linked
spec. Exact target docs, focused tests, build and native evidence remain open.

| ID / requirement | Input → observable result | Selected difference / limits |
| --- | --- | --- |
| ANO-01 Qualifying player pawns receive one initial nocturnal schedule | Starting/arrival or successful player recruit/enslave event → one any-match decision | Night Owl or actual enabled dark-precept/active sunlight-disliking gene; correct player/prisoner scope and ready timetable |
| ANO-02 The template is a complete native schedule with explicit time bounds | Undecided baseline candidate → Sleep 11:00–19:00, Anything otherwise | Configurable whole start 0–23 and duration 1–24; normal sleep/needs/emergency behavior remains |
| ANO-03 Customized schedules and later edits remain authoritative | Custom prior timetable or observed edit/paste → preserved choice | Intentional correction to source first-application blanket replacement; historical baseline-looking edits cannot be inferred |
| ANO-04 Load and changing eligibility do not cause repeated scheduling | Reload/respawn/rejoin or new preference → retained prior decision | Delay load consideration until history restored; only undecided unedited candidates can qualify at a later supported event |
| ANO-05 Automatic writes are atomic and failures preserve the schedule | Failed initialization/history → original schedule retained, explained fallback | Capture and restore schedule/decision together; no per-tick retry or silent corruption-as-new treatment |
| ANO-06 Configuration uses the shared settings model and native gameplay surfaces | Common optional criteria/window edits → future decisions | Consistent true/true defaults, atomic scoped window, seven languages, no per-pawn inheritance or new tab |
| ANO-07 Compatibility admits supported behavior and identifies the active provider | Admission/provider selection → supported automation or ordinary Schedule fallback | Mandatory preservation group, independent optional criteria; original sole-provider authority is explicit |

## Interactions and composed result

S001 mood bar reads actual stored schedule intent. S002 temporary directed recreation does not
rewrite it or trigger initialization. S031 child Schedule/care uses the same timetable and retains
medical/rest/safety rules. No forced sleep, sunlight immunity, changed gene/trait/mood effect, new
work priorities or internal patch stack. No private map/pawn scan/cache or shared Thing index:
per-pawn decision history is lifecycle state. Combined costs remain unmeasured.

Source responds to spawn/recruit/enslave, not directly to conversion or gene changes. Keep that
event-driven distinction. Previously unqualified, unedited baseline pawns can qualify at a later
supported event. Once assigned or preserved, they remain decided through reload/travel/rejoin and
settings changes. In an existing save, uncustomized loaded candidates can receive the template;
off-map candidates wait for a supported event. No blanket retrospective rescheduling command.

## UI audit and common settings

| Source surface | Purpose | Owned destination |
| --- | --- | --- |
| Global ideology/gene checkboxes | Additional qualification configuration | Pawns / Initial schedules common rows; actual behavior labels instead of only Dirtmoles / Vampires |
| Native Schedule assignments | Pawn gameplay state | Existing native Schedule, including meaningful S031 child rows |
| No source time editor/tab/toggle | Initial time configuration | One common scoped Initial sleep window row; no new gameplay tab or bottom-right switch |

Defaults: optional ideology **on**, optional genes **on**, start **11:00**, duration **8h**. The two
source fields are false in a fresh object but missing saved keys load true; consistent on is an
explicit correction, not an assertion of source parity. All common inheritance/dormant override,
main-menu disabled save callbacks and save-switch rules apply. Start/duration inherit together.
Unavailable DLC/content is explained and dormant values retained. Valid edits affect future undecided
candidates, not existing assignments. No new raster art; seven-language text and native UI verification
are required. No new base-trait off switch is proposed.

## External contracts, save bindings and providers

| Evidence / binding | Treatment | Unverified boundary |
| --- | --- | --- |
| Source WorldComp/pawnsWithNightOwl pawn-reference history and native times | Own game-scoped decisions, preserve native schedule serialization; no source identity impersonation | Migration/remove-readd and malformed/legacy history need actual native verification |
| Public settings class/static fields and patch/helper classes | No facade selected without real consumer | Bounded installed XML/CS/JSON search found source, RimPy and LayoutHarness samples only; not an all-binary audit |
| Source Prepare Carefully description | Treat supported custom initial timetable as authoritative | Exact installed provider not found; no arbitrary Prepare Carefully compatibility claim/adapter |
| Original ben.automaticnightowl active | Original sole provider; owned writes/tracking dormant, preferences retained | UI states source overwrite/default behavior differs; owned preservation is not promised under source authority |
| Compatible custom schedule providers | Preserve nondefault/native observed choices; optional narrow adapter only for proven consumed contract | Unannounced direct baseline-equal foreign writes or pretracking manual history cannot be inferred |

Existing native assignments are not erased when owned admission stops. Whole-source settings import,
saved-class conversion and safe uninstall are not promised; any future migration requires durable
backup, atomicity, restoration and verified postconditions. Runtime write rollback captures the full
original schedule/decision locally and restores both on failure.

Harmony becomes required when implementing lifecycle/choice observers. Ideology/Biotech optional;
Core qualification stands alone. No XML contribution selected and no new XML Extensions dependency.
No source binary, other repository mod or Gateway dependency. Every actual patch requires its
minimal documented semantic predicate, effective foreign-patch/later-repatch handling, owned-only
rollback and bounded startup warning. Failed required preservation disables automatic writes;
optional eligibility can decline separately. Compatible foreign patches remain admissible.

## Verification and remaining work

ANO-Q1–4 retain custom-preservation/default/window choices, precise native lifecycle/choice/history
admission, provider/save boundaries and focused tests/localization/performance/native acceptance.
Required workflows use actual startup/arrival/recruit/enslave actions and native Schedule paint/paste,
save/reload/travel/settings, then personally inspected assignments and ordinary pawn behavior.
Assigned test data, direct mutation and clean startup alone cannot accept gameplay.

Semantic pass: the default moves an eight-hour sleep permission window to daytime, avoiding the
ordinary Night Owl daytime-awake penalty opportunity without promising sleep, mood immunity or
optimal work allocation. Anything remains permission, not forced night work. Other sleep/medical/
child care mechanisms retain control. Custom/template settings can reduce work time; user edits and
prior schedules remain authoritative within the documented historical-baseline limit.

Independent source/native and composition reviews found no P1/P2 issues. Minor research wording
about retained unselected starting pawns is corrected. The draft contains **7 requirements, 23
scenarios and 23 unchecked tasks**. Typed strict validation passed **92 items, zero failures**, exit 0
and no stderr; **8 scoped Markdown documents** passed local-link/whitespace checks. No runtime code,
tests, build/install, game launch, source execution, Workshop or normal save/configuration changes.
