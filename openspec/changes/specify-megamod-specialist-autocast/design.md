## Context

Single owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target1.7 beta.
S051 replaces Linnun.AutoCastSpecialistCommands, Workshop3459647882. Documentation only.
The [research record](../../../mods/megamod/docs/refinement/2026-11-03-specialist-autocast-research.md)
pins installed code, native evidence, optional-provider boundaries and intentional deviations.

The installed net10.0 Release DLL and all twelve included C# files agree on seven global switches,
five native job patches, six optional Research Reinvented patches and command-input decoration.
No dedicated map index, game component, custom job/Def or pawn-local switch exists upstream.

## Goals / Non-Goals

**Goals:** preserve documented work coverage and default choices while making timing, eligibility,
save scope and patch admission explicit; preserve actual ability behavior and ordinary work.

**Non-goals:** new roles/abilities, permanent auras, construction/deep-drill/idle triggers, cooldown
rebalance, per-pawn configuration, all-purpose ability automation, upstream settings import,
provider content implementation or compatibility claims based solely on metadata.

## Decisions

### 1. One small command policy with explicit triggers

Resolve a command through its admitted AbilityDef and the pawn's actual ability/Ideology role.
Source GetIdeoAbility prefers pawn.abilities then Precept_Role.AbilitiesFor, with requiredMemes only
checked on the latter path. Owned activation preserves native/provider eligibility on either
path and never synthesizes an ability. Re-resolve at use; do not retain an ability captured while
building a job's toils across role changes. Limit automation to player-controlled humanlike workers;
this is an explicit source narrowing (source has no faction/player-control check).

| Def | Trigger identity to investigate/admit |
| --- | --- |
| FarmingCommand | JobDriver_PlantWork: PlantSeed, Harvest, HarvestDesignated, CutPlant, CutPlantDesignated |
| MiningCommand | JobDriver_Mine |
| ProductionCommand | Verse.AI.JobDriver_DoBill with recipe.workSkill == Crafting |
| ResearchCommand | JobDriver_Research and admitted RR entry points |
| SO_ButcherCommand | Cooking-skill DoBill; provider ID in source hints is SierraOnline.AdvCannibal, not yet independently verified |
| SO_VoidCommand | JobDriver_StudyInteract; source hints SierraOnline.VoidSpecialist, not yet independently verified |
| VME_WorkCommand | Any valid DoBill recipe plus the other listed work drivers; study independent of SO_VoidCommand |

Optional package/Def resolution is mandatory. Cooking/Void provider packages were not found in the
installed About inventory; their author pages establish content intent, not consumed type/Def
shape or verified canonical IDs. Retain them as designed coverage with admission unfinished.
VanillaExpanded.VMemesE's inspected v1.7 LoadFolders selects1.6 and its Royalty subfolder; the command
requires VME_Royal and belongs to VME_Majordomo. Do not make Royalty or VEF a global megamod dependency.

RR canonical package is PeteTimesSix.ResearchReinvented. Source targets namespace
PeteTimesSix.ResearchReinvented.Rimworld.JobDrivers and MakeNewToils on JobDriver_Analyse,
JobDriver_AnalyseInPlace, JobDriver_AnalyseTerrain, JobDriver_InterrogatePrisoner,
JobDriver_LearnRemotely, JobDriver_ResearchRR. Installed provider advertises through1.6, not1.7;
six decompiled work-entry bodies provide intent evidence only. Exact beta bindings remain AC-Q2.
Preserve provider prerequisites and state, including project/opportunity availability, analysis
hauling, interrogation permissions, comms power/cooldown and native reservations.

### 2. Admit semantic work entry, not a generic toil completion mode

The [foundation patch contract](../add-megamod-foundation/specs/megamod-patch-target-specs/spec.md)
and mod AGENTS.md govern implementation. Each skipped enabled patch requires one bounded startup
warning naming package/patch/spec ID, target, failed predicate, unavailable behavior and fallback.
False/unknown predicates, validator exceptions and installation failures disable the declared
coherent group and remove only newly applied owned patches; never alter foreign patch ownership.

Source Inject materializes the returned enumerable and inserts before its first Never toil, else
index0. It also checks readiness too early during construction. Beta research uses Delay and
therefore falls back to before travel; study does too. Bill preparation and plant target loops
make a generic first-Never search insufficient as a compatibility contract. Do not copy that
heuristic, materialize foreign sequences speculatively, or infer semantic validity from the method
name, assembly hash, absence of foreign owners or an arbitrary toil index.

Inspect each effective work path and select the narrowest before-work seam after preparation.
Specify the actual role of predecessor/goto/jump paths, original-skip prefixes and foreign
transpilers. Each future patch needs its target document, admission validator, owned group and
failure scenario. A transpiler checks its actual incoming stream every invocation and returns it
unchanged on failure; later supported repatches invalidate/revalidate. Never execute foreign
transpilers as a speculative compatibility probe. Exact hook choices are implementation work.

### 3. Run native casting with bounded per-job ownership

Source creates an Instant toil whose init calls ability.verb.TryStartCastOn(pawn) and ignores the
result after optionally logging in Debug. Native CanCast is not the whole manual eligibility
contract; CanQueueCast/CanApplyOn and command disabled checks cover additional conditions. Native
TryStartCastOn sets Stance_Warmup; ability activation subsequently handles effect/cost/cooldown.
Use the supported native path while retaining the current work identity and its failure/cleanup
semantics. Do not call Activate, add hediffs or reset cooldown directly to achieve the result.
Do not assume starting a verb means the cast completed. Await normal completion/interruption before
the next candidate; a refused cast must not leave the worker waiting indefinitely.

Track handled work-start opportunities by stable job identity plus command key, including skipped
and failed attempts. Persist the minimum marker needed to survive reconstruction; do not persist
live pawn/ability/toil references or create a reusable scheduling framework. A plant/research loop
is not a new job. Work already underway at first installation has no historical opportunity to
replay. Load after an already-handled opportunity must not repeat it. A native cast in progress
uses native persistence/interruption behavior, never a second owned replay. Disabling automation
prevents new attempts without stripping existing effects. New player orders win over resumption.

Specific command precedes Work Command uniformly, an intentional departure from source execution
order. Source calls Inject for the specific command first, but two index0 insertions reverse that
order when there is no Never toil: native research/study run Majordomo before the specific command.
With a Never work toil the source preserves injection order. Recheck each command sequentially.
Jobs are not cancelled/restarted merely to induce another attempt.

### 4. Separate configured defaults from current-game operation

Source right-click modifies one static ModSettings boolean for every pawn/save; no immediate
WriteSettings call is present. This is not pawn-local gameplay state. Owned configured defaults use
the foundation common mechanism and seven stable command keys, built-in values AC01. No custom
loading/storage/settings renderer per port. Global changes affect inheriting save values; explicit
live choices take precedence. Keep dormant save configuration overrides when Use global is checked.

One bottom-right selector contains available command entries with On / Off / Use configured value.
This compact live control is a proposed navigation design, not an extra bottom tab. A save-owned
optional operational value per command (absent means follow configuration) permits live choices
without mutating global defaults or the configured save override. It is operational state rather
than a second configuration system; retain it across reloads, clear live references on save exit,
and show explicit-vs-following state. Common settings stay in Work → Specialist commands, with
unavailable provider rows explained and dormant values preserved. No per-pawn controls introduced.

Remove source Command.GizmoOnGUIInt/ProcessInput interception. A noninteractive indicator is optional
presentation within this contract; it cannot consume mouse events or become a dependency on VEF's
UI/CheckAuto texture. All seven locales and actual compact/world-view layouts need native review.
If new raster art is chosen later, the mandatory asset workflow applies; no art is selected here.

### 5. Isolate ownership and avoid new scans

Ideology gates this whole capability; no absent-DLC failure or mandatory DLC for unrelated megamod
features. Core work hooks have independently declared coherent groups; each optional ability
adapter and RR's all-six work hooks have coherent rollback. Package presence alone is insufficient.
No XML mutations are needed. If future declarative edits become necessary, specify XML Extensions
admission/merge behavior and dependency in that slice first.

With Linnun.AutoCastSpecialistCommands active, suppress this whole owned feature and its controls,
retain dormant state and explain why. Never unpatch net.linnun.autocastspecialistcommands or claim
the source's behavior is repaired. A bounded installed DLL scan found only the source's three
versioned assemblies using its namespace/owner strings; no consumer contract is proven. No facade,
package spoof or source settings migration is justified by that scan.

Directly compose with S010 recipe inspection, S014 linked workbenches, S018 adaptive production,
S019 skilled-worker restrictions, and future S092 Keep Converting input ownership only
where they actually share entry points. Do not add source-style patches to owned features or
invent precedence conflicts. Native pawn abilities/job state suffice; no Thing scan or Ctrl+F cache
consumer is needed. Measure job-start/GUI overhead before making optimization claims.

## Risks / Trade-offs

- Cast at actual work start changes source timing; it avoids spending a command before unreachable
  work and sees readiness changes during travel. AC02 makes this observable departure explicit.
- One attempt per job leaves long jobs without recasting after cooldown; deliberately preserve
  work-triggered scope rather than silently designing periodic automation.
- Multiple commands can compete for native cooldowns or warmup; sequential rechecks and native
  eligibility preserve costs and prevent accidental double activation.
- Source Majordomo study was gated on SO_VoidCommand existence; owned independence removes that
  accidental provider coupling, with explicit optional Anomaly admission.
- UI has two clearly labeled roles: configured defaults and live choices. AC-Q3 retains layout
  review, but no global settings change is hidden behind a pawn right-click.
- Provider content/version gaps remain real: no installed Cooking/Void metadata, and RR lacks an
  advertised beta build. Do not enable unknown adapters to satisfy a parity label.

## Migration Plan

Documentation now. Implement vertical slices only after target/fixture design: default resolution,
one native work/cast path, persistence, then additional command/provider groups and live controls.
Use host Unit for pure policy, owned Harmony tests for patch shapes, honest Def fixtures only where
appropriate, and isolated game runs for loaded content and native job/ability wiring. Keep every
runtime task open until reviewed-build player actions and personally inspected causal evidence exist.
No source-config/save import or destructive migration is proposed. Removing the feature removes
automation, not native abilities/effects/cooldowns or ordinary work; normal backups and separate
save-compatibility proof are required before claiming removal compatibility.

## Open Questions

- **AC-Q1:** Effective beta work-start, native cast, interruption and persisted opportunity seams;
  target contracts and failure isolation, including new/reloaded/looping jobs.
- **AC-Q2:** Verify actual Cooking/Void package/Def content, RR beta behavior and optional command
  restrictions/costs; provider-present acceptance before support claims.
- **AC-Q3:** Compact live selector/default distinction, seven locales, save/world-view lifecycle and
  direct composition with owned work/command input.
- **AC-Q4:** Honest TDD, package/review, measured overhead and native action→cast→effect→work evidence.
