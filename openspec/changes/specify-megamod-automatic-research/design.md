## Context

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
Replacement S120/P2-50. [Research](../../../mods/megamod/docs/refinement/2026-11-06-research-whatever-research.md)
records installed1.6 Research Whatever1.2.5, matching public binary/source, current game1.7.5000rev1275
and actual Research Reinvented/Gravship inputs. This is a documentation design, not admitted targets.
The local installation lacks Anomaly's data directory; code inspection is not DLC-present acceptance.

## Goals / Non-Goals

**Goals:** useful research while selection is empty, player/provider selection precedence, correct
remaining-work ordering, native progress and study consequences, reversible exhaustion, common
notification preferences, and bounded compatibility failures.

**Non-goals:** new research content/tree/queue, faster research, bypassed prerequisites, automatic
completion, replacing optional providers' research systems, source save conversion, copied upstream
code/binaries, blanket 1.7 or Anomaly compatibility, implementation or native acceptance in this draft.

## Decisions

### Native data, transient decisions and late selection

Reuse `ResearchManager`, current `ResearchProjectDef` facts, native bench listers and
`StudyManager`'s map-owned studiable cache. No per-port research/bench/study registry. The source
materializes and sorts all projects during `HasJobOnThing`, then sets a global project before native
eligibility. Replace this with a bounded prospective evaluation plus a revalidated commit in the
actual research-job path. Do not temporarily publish a global project merely to ask native predicates.

Exact seams remain ARW-Q1: relax only the empty-project restriction in the native scanner; use a
scoped prospective project for the specific eligibility evaluation, retaining effective native
bench/pawn/provider checks, and commit only for an admitted job with successful reservations and
current access. Current `JobDriver_Research` reserves the bench and interaction cell, walks to it,
then begins a research toil with project/bench failure conditions. Prefer the last safe boundary
before that work begins; recheck selection, toggle, ownership, facilities, access and eligibility
there. A changed occupied selection wins; re-evaluate whether that job can use it or fail normally.
No field/global project mutation for candidate probing; no permanent reference to a speculative job.

Only native checks that actually belong to a committed job may perform history notifications or
other side effects. Avoid duplicating them during candidate scans. Selection and its notice happen
once on the game thread. Tie randomness belongs to the final choice, not every scanner query.
Failed reservation, lost power, ownership change or an intervening player choice must not leak a
prospective choice or switch off the bench. Clear transient context on every success/failure/exception,
job cancellation and teardown; do not save it. Exhaustion may switch the bench off only after a
usable-bench/pawn evaluation distinguishes no eligible project from temporary inability to work.

Compare priority as a separate ordinal, then current finite nonnegative remaining apparent work.
Use the native colony tech factor and progress without rounding before comparison. Reject malformed
estimates; do not turn NaN or infinity into a preferred project. Random ties include only the same
priority and remaining amount. This replaces the source's100000000 additive penalty and total-cost
tie filter. A bounded single pass can collect minima/ties; benchmark before adding any cache.

### Declarative bench state and optional policies

Prefer guarded XML Extensions additions of one owned comp to actual research-bench Defs/subclasses,
after resolving inherited class/content shapes; preserve all existing comps. Do not repeat the
source's global static-constructor postfix that mutates every matching Def at runtime or hooks
BetterLoading by name. Dynamic external bench creation needs a specific adapter, not an unconditional
fallback mutation. Use the current extension operation capabilities, package and content checks,
duplicate prevention and coherent preflight before mutation. Exact operations must be inspected
before implementation; no guessed operation API is accepted by this document.

Bench enabled state defaults true, is effective only for player faction and persists with the comp.
It is contextual gameplay state, not an inherited setting. Missing owned keys use the documented
default. Save/load, minification/reinstallation, transfer and ownership changes must preserve identity
without forcing another bench on or off. Exhaustion is explicit off, not a hidden timer that silently
re-enables the bench when research unlocks. Do not copy source `active` keys into unrelated comp scopes.

An owned research-policy extension carries exclusion/low-priority facts. Source public type
`ResearchWhatever.ResearchWhateverExtansion` (spelling intentional) has bool `ignore` and
`lowPriority`. Bounded text/binary searches found no external binding consumer; source's own XML is
the known user. No source-named facade or assembly/package spoof is justified yet. Translate the
known optional policies to owned data; record original type bindings as not preserved until a real
consumer is inspected. Exclusion wins over low priority; duplicate/conflicting policy data must not
be resolved by arbitrary first-entry order. A later proven consumer gets a narrow adapter and its
own admission tests, not a generic compatibility framework.

Use `XmlExtensions.FindMod` with `packageId=true` plus Def/node/type checks for:

| Optional provider | Source policy to retain |
| --- | --- |
| `firefoxpdm.ResearchableStatUpgrades` | Low priority for `StackingRepeatable`, `ProductivityRepeatable` |
| `vanillaexpanded.gravship` | Exclude `BasicGravtech`, `StandardGravtech`, `AdvancedGravtech`, `OrbitalTech`, `VGE_GravshipWeaponry`, `VGE_CompactWorkspaces`, `VGE_OxygenNetwork`, `VGE_AstrofuelRefining`, `VGE_GravshipPower`, `VGE_HeatDissipation`, `VGE_GravshipLiving` |

Each optional group is absent-safe, merge-preserving and idempotent. Preserve provider extensions,
facility lists, costs and prerequisites. Validate the complete expected policy group before adding
it. Missing expected content or an unproved gravtech shape must not fall through into selecting
special research as ordinary work: suspend ordinary automatic selection for the affected provider
combination if its safe eligible domain cannot be established. Manual/provider research stays native.
Source's lack of a package gate for repeatable research and destructive operations in the external
Gravship provider are not copied into this feature. XML Extensions becomes a required runtime
dependency with the first owned XML operation; Harmony is required with actual C# patch use.

### Anomaly selection and local exhaustion are different decisions

Inspect `StudyManager.StudyAnomaly` before its `ApplyKnowledge` for positive knowledge only.
Source respects a selected primary or immediate overflow project, then considers primary category
candidates and only then immediate overflow candidates. Retain that order; do not recursively start
a succession of projects in one action or assign excess knowledge ourselves. Native `ApplyKnowledge`
retains its own routing. Owned examination must detect unsupported/cyclic/null relationships and
remain bounded; it must not amplify a malformed provider chain.

Use native availability plus the same owned selection policy/remaining-work ordering; applying the
policy and difficulty visibility consistently to Anomaly is a **proposed correction** beyond the
source, which only checks `CanStartNow` and category there. No costs, XP, cooldowns or research-speed
changes. Native `SetCurrentProject` can affect both normal and knowledge slots for hybrid projects;
inspect every affected slot before committing. Do not displace a manual normal project via a
knowledge candidate. Unknown extra slots make that candidate ineligible for automatic selection.
Native `ResearchProjectDef.ConfigErrors` also rejects simultaneous positive `baseCost` and
`knowledgeCost`; protecting occupied slots alone does not establish valid hybrid progress/completion.
Treat malformed hybrids as unsupported. Any valid provider-specific mixed mechanism needs its
actual cost/progress/completion contract admitted under ARW-Q2 before automatic selection.

If no eligible destination exists, local exhaustion needs `CompStudiable` finite completion,
`CompStudyUnlocks.Completed`, and `UnnaturalCorpseTracker.CanDestroyViaResearch` where applicable.
The source's1.2.5 change already avoids setting `studyEnabled=false` on holding-platform subjects.
Preserve that distinction: Study→MaintainOnly uses the containment control, while ordinary objects
use `SetStudyEnabled(false)` only when their actual contextual Study toggle is available and can
resume. `CompProperties_Studiable.showToggleGizmo` defaults false; a comp alone does not prove UI.
Prisoner-specific/other provider controls need their real path admitted; leave unknown subjects
unchanged rather than create an invisible second blocking flag.

Current `CompStudiable.Study` applies local study before `StudyAnomaly` and has further post-study
consequences. The prefix must not suppress or replay either call. Decide transition using current
facts, perform safe stopping at an admitted boundary, and preserve any discoveries made by this
same action. Unknown extra research-dependent outcomes prevent auto-stop until an adapter exists.
Send the exhaustion notice only for a real on→off/Study→MaintainOnly transition. A valid native
held target is preferred for messages; null/off-map/no-name targets cannot break study.
Do not auto-resume on new project availability; the player's contextual choice is authoritative.

### One completion setting, no changes to completion effects

Common definition `research.completionPresentation`: enum NativeDialog/Letter/QuietMessage/Silent,
built-in NativeDialog, global default plus save override, next-completion timing. Shared loading,
validation, storage and row rendering follow the foundation; invalid/missing enum values resolve to
NativeDialog. No parallel settings serializer. Disable main-menu save callbacks as well as controls.

The source changes `doCompletionDialog` and emits replacements in a prefix before completion.
Instead intercept only the admitted native dialog-presentation boundary after successful completion
is established. Do not replay `FinishProject`, recursively finish prerequisites ourselves, or use
a prefix success notice. If argument suppression is used, prove its actual flow into that exact
dialog branch and pair it with a verified successful-completion observation; an occupied/throwing/
skipped-original foreign path must not produce a false success or lost native fallback.
Keep `doCompletionLetter` story/discovery behavior, unlock notifications, progress clamping, tales,
power recalculation and completion signals intact. Ordinary and Anomaly callers share the setting;
caller-suppressed dialog requests remain suppressed. No suppression of automatic-start/exhaustion
notices. Capture one effective mode per completion and send no duplicate replacement.

Keep bench toggles contextual; remove the source's right-click configuration menu in favor of the
common ordered Research settings section. No new bottom tab, cheat action or bottom-right global
toggle. Use all seven baseline languages. Reuse native icons under their existing ownership; no new
raster art is selected by this draft. S071 message placement and S074 description links compose
through the owned common presentation, without patching another owned feature.

### Co-installation and target admission

`avilmask.ResearchWhatever` active selects the original and disables owned bench, study and
completion overlap, including owned optional policy additions. Do not disable/unpatch the source
or advertise that it receives owned fixes. Preserve saved owned entity/settings data inertly.
No drop-in original assembly, package, Harmony owner, saved GameComponent or extension binding claim.

Actual `PeteTimesSix.ResearchReinvented`1.6 evidence shows its prefix forces vanilla
`WorkGiver_Researcher.ShouldSkip=true` and its separate `WorkGiver_ResearcherRR` selects research
opportunities. Its source metadata load order is not proof that owned bench hooks work. Do not
force that vanilla work-giver back on. A narrow adapter must inspect the actual job/opportunity
contract before bench autonomy can be supported; otherwise disable that owned group with a useful
warning. This does not inherently disable independent Anomaly/presentation groups if they admit.
Project/queue providers keep current selection; unknown external queues are not cleared or rewritten.

Provisional target families (stable IDs to become individual target documents before code):

| Family | Minimal semantic evidence | Failure scope / fallback |
| --- | --- | --- |
| `ARW-BENCH-DEFS` | Final research-bench class, compatible comp list, no duplicate owned state; XML merge and package selection | Bench automation group off; native manual research |
| `ARW-BENCH-WORK` | Empty-project scanner gates, actual eligibility reads and side-effect boundaries, native job reservation/start, current-project use; effective provider restrictions preserved | Scanner/eligibility/commit coherent group off, no speculative selection |
| `ARW-STUDY-SELECT` | Positive knowledge/category flow before exactly one native award; occupied-slot behavior and native route retained | Auto-selection off, native study/knowledge unchanged |
| `ARW-STUDY-PAUSE` | Correct local outcomes and target-specific reversible control; current action consequences preserved | Auto-pause off for unsupported subject/group, study remains native |
| `ARW-COMPLETION` | Actual request→dialog consumption and successful completion presentation boundary, independent letters untouched | Native completion presentation |
| `ARW-OPTIONAL-POLICY-*` | Canonical provider, complete intended Def set, relevant research mechanism, merge/duplicate checks | Provider adapter off; suspend unsafe ordinary auto-selection domain as above |

Minimal semantic checks, not exact hashes, whole IL or no-other-patches requirements. Hashes identify
evidence only. Check effective code, skip-original prefixes and transpiler input semantics; each
transpiler revalidates its actual incoming stream and returns it unchanged on failure. Never execute
foreign transpilers speculatively. False/unknown predicates, validator exceptions or installation
errors disable the affected coherent group; roll back only newly added owned patches. Supported
later repatch triggers invalidation/revalidation. Preserve completed/in-flight native work; never
retry a partially applied knowledge award, project selection or completion for fallback.
One bounded startup warning per skipped enabled patch names package/patch/spec/target, reason,
lost behavior and fallback. An absent optional provider or explicitly disabled feature is not an
enabled failed target. No per-pawn/per-tick retry-log spam.

## Risks / Trade-offs

- **Selection before actual work** → reserve/revalidate/commit late, no global temporary project;
  native job cancellation still remains possible after a legitimate start.
- **Automation changes progression order** → cheapest remaining work is retained source intent,
  optional special/repeatable policies preserve their roles; no added speed or completion reward.
- **Exhaustion requires manual re-enable** → explicit notice/toggle; avoids polling and preserves the
  source's deliberate off state. Reconsider only through a separately specified UX change.
- **Anomaly content missing locally** → code-backed draft only; DLC-present data/UI and special subject
  acceptance remain mandatory. Unknown outcome/control adapters fail auto-pause closed.
- **Alternate worker systems** → Research Reinvented is a demonstrated different path, not an assumed
  conflict between unrelated mods; do not claim support without an admitted adapter and native proof.
- **Source extension contract** → known own-XML semantics are translated, unproved third-party type
  bindings are explicitly unsupported; record new consumers before promising a facade.

## Migration Plan

None in this phase. New owned saves use explicit defaults and common settings. Preserve native
projects/progress and owned state through disabling/re-enabling; do not import original `active`,
`NotifyMode` or GameComponent data or claim safe source removal. Any requested migration needs its
own backup, atomic conversion, failure restoration and verified postconditions before implementation.

## Open Questions

- **ARW-Q1:** exact effective scanner/job/knowledge/presentation seams, target documents, coherent
  admission, side-effect-safe prospective evaluation and later revalidation.
- **ARW-Q2:** complete optional provider groups, Research Reinvented adapter, external policy/queue
  consumers, Anomaly category/subject controls and any justified future migration.
- **ARW-Q3:** finalized research priorities/costs/facilities, local Anomaly benefits, seven-language
  contextual/settings UX and combined selection cost; measure in the exact supported environment.
- **ARW-Q4:** focused TDD, reviewed implementation/package and causal native bench/study/completion/
  save/provider workflows. All remain open; no implementation acceptance is implied by validation.
