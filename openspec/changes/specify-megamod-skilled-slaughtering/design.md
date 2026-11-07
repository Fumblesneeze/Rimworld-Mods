## Context

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
S117 references `MrHydralisk.SkilledSlaughtering`, Workshop2873284405. The installed manifest says
1.3.0.0; latest payload is1.6 DLL1.0.0.0. Native load-folder fallback selects root plus1.6 on the
inspected1.7.5000rev1275 game. This is source selection, not a runtime compatibility claim.
[Research](../../../mods/megamod/docs/refinement/2026-11-06-skilled-slaughtering-research.md)
records complete four-type decompilation, XML, native semantics, comparator inputs and consumers.

The source postfix materializes `JobDriver_Slaughter.MakeNewToils()`, finds the first debug-name
`WaitWith` with duration180 and changes only `defaultDuration`. Native `WaitWith` captures its
original duration for `ForceWait`, so the animal still waits180 ticks. Source can also produce zero
duration at exceptional speed, throw when its assumed toil is missing, retain pawn references
across games and reuse outdated speed for60 ticks. It has no own settings/save record.

## Goals / Non-Goals

**Goals:** retain the source-derived duration/stat model, correct coordinated handling and cleanup,
honor native selection/death semantics, preserve a useful public stat identity and allow compatible
providers without unguarded rewriting or duplicated caches.

**Non-Goals:** Cooking/yield details (S118), global meat/leather changes, hunting/finishing speed,
milking/shearing, execution of prisoners/entities, new job eligibility, automatic designations,
XP generation, source assembly/package spoofing, performance or beta acceptance without evidence.

## Decisions

### Preserve declarative stat semantics and safe identity

Retain `SlaughteringSpeed` as the public StatDef name where the owned definition is admitted;
ordinary StatModifier/Def-based contributions can then resolve it without a fake source package.
Define the source-derived base/factors declaratively: base1, minimum0.1, PercentZero, PawnWork,
WorkSpeedGlobal; Animals `0.64 + 0.09*level`; Melee `0.95 + 0.0125*level`; Manipulation weight1;
Sight weight0.5 with capacity cap1.2. Normal native stat modifiers, skill-less factor1 and finalized
stat processing remain authoritative. Native helpers already implement this; do not duplicate the
full stat system in a parallel calculator. A deterministic test model can verify the owned duration
rule independently of global Def state, but real finalized stats require an isolated game.

Use XML Extensions for guarded additive definition creation: verify required native stat/skill/
capacity/category content and absence of a conflicting definition. Detect original provider with
`XmlExtensions.FindMod`, `packageId=true`; source active means no duplicate owned Def and no owned
timing. Never overwrite an existing third-party stat or replace any unrelated factors/list. An
unexpected same-name definition requires an actually inspected/admitted provider contract; name
alone is insufficient. Unknown shape disables affected owned behavior with diagnostics. Required
dependency `imranfish.xmlextensions` accompanies first actual use; Harmony likewise required with
actual patch use. No such runtime metadata/code is changed in this draft.

Keep contextual speed information in the native stats page. An owned stat worker can inherit native
calculation/explanation and hide its owned entry when no owned timing is admitted; do not make an
unused percentage look effective. Do not hide/edit a provider's own stat entry. Seven languages
cover owned name/description/explanations. There is no new global/save setting in S117.
[S118](../specify-megamod-slaughter-yield/design.md) owns six proposed Cooking/recovery controls.
Its Cooking multiplier enters this owned calculation before the final minimum, once; source-derived
Animals/Melee/work/capacity factors remain intact. Both features share this handling-start snapshot
and saved attempt association rather than separate job ledgers.

Alternative: rename every source stat. Rejected here because this isolated generic identity is
practical to preserve with guarded creation. Alternative: require/spoof the original package or
public patch class. Rejected: no actual callable consumer was found, and preserving a Def does not
establish binary compatibility. Metadata suggestions from four other mods are not API calls.

### Replace only the native slaughter handling construction

Prefer a narrowly admitted substitution of the native animal-slaughter handling stage, keeping
the original approach, failures and final execution action. Resolve the iterator through its owning
method, not a compiler-generated class suffix. Identify the unique victim wait between approach
and execution from its effective semantic context, not debug names/list position/literal180 alone.
Other added toils and compatible outcome patches must remain intact. Do not enumerate a foreign
iterator repeatedly or replace the entire native job to avoid understanding it.

The replacement stage takes one fresh effective speed/body-size snapshot when handling starts,
then computes the source two-step truncation and size factor from the observable spec, bounded to
at least one tick. Changing skills/health during this stage affects the next attempt, not its
remaining work. If the required inputs are invalid/unrepresentable, use the ordinary native stage
without a fabricated zero/negative/overflow duration; retain a bounded reason. Do not bypass native
incapacity/eligibility gates just because the stat minimum is positive.

Create one finite coordinated native wait for that exact victim, using the same duration as the
handler. Respect native `ForceWait` posture/downed/deactivated choices and ordinary interruptions.
Avoid the source's captured180 mismatch. Current `JobDriver` sets remaining duration before its
init action, so any entry-time calculation must also establish the actual remaining work and
progress denominator; changing only the future default does not implement the requirement.
The final cut, corpse, record and mental-state notification remain the original action, once only.

Alternative: mutate the returned toil's duration. Rejected: captured wait is unchanged. Alternative:
globally increase animal waits. Rejected: affects unrelated interactions and can leave animals
held by abandoned jobs. Alternative: extend the source cache or patch it. Rejected: independent
owned behavior and direct S118 composition are required.

### Exact wait ownership, lifecycle and saved work

Track only the active worker/job/victim association, original total and remaining handling work,
and the exact temporary wait created for that attempt. Use native job serialization for remaining
ticks; add the smallest owned job-associated saved record needed for total/ownership consistency.
Do not introduce a map-wide pawn index or a global long-lived Pawn-key dictionary. State is job
state, not a setting. Remove entries on completion/cancellation/despawn/teardown and rebuild only
validated live associations after load.

Release only the wait created for that attempt if it is still the animal's current matching job;
never cancel a later animal job, restore a stale interrupted job, or override aggression/fire/damage
reactions. A successful handoff from wait to the original cut must not schedule a wandering step
between them. Cancellation must not advance into the cut or repeat records/death. A restarted
attempt calculates fresh work without carrying old partial credit to a new worker/animal.

On save/load, preserve remaining work and the captured total; no full-duration reset, free finish
or repeated side effects. Unknown/missing associations fail safely: release only proved owned
waits, abandon the affected owned attempt without executing its cut, then allow an ordinary fresh
job. This is not automatic source-job migration. Older/native in-progress jobs without owned
records retain native behavior unless a separately validated transition can reconstruct it safely.
Disabling the owned group after admission invalidation cancels owned unfinished work and releases
only its matching hold; subsequent jobs use native/provider behavior.

### Scope and direct composition

Native WorkGiver retains animal/faction/designation/automatic-list, aggression, reservation,
Violent-work and ideology gates, with Manipulation required by its WorkGiverDef. The slaughterer
mental-state JobGiver uses the same Slaughter job with ignoreDesignations; retain its timing scope
without granting it new targets. Entity slaughter uses ExecuteEntity and is separate. Do not apply
this policy to every `ExecutionUtility` call, hunting, Allow Tool finishing or companion attacks.

Keep the native injury/difficulty/body/part/yield rules untouched. S118 specifies a deliberate-cut
corpse record and later meat/leather recovery, independently of native mental-state timing scope.
The preserved native final cut calls the owned recording policy directly; no generic execution
wound establishes eligibility. S118's corpse record and quantity adjustment do not alter base yields.
Current source creates no extra Animals/Melee/Cooking XP. S064/S065/S066 hunting/wandering systems
must respect the temporary native job; no own wandering patch is required to pin an animal.

### Admission and failure groups

Before coding, create stable-ID target documents for XML definition admission, handling-stage
substitution and any necessary persistence/cleanup hooks. Keep timer/wait/cleanup/restore changes
atomic: no half-lengthened interaction. Check required native fields and actual effective wait/
duration/failure/execution semantics, and separately admit original-provider selection. A signature
alone cannot establish a duration-changing patch. Any transpiler validates its actual incoming
stream on every invocation and returns that same stream unchanged on failure. Do not execute
foreign transpilers speculatively. Compatible prefixes/transpilers/toils are allowed when required
semantics survive; unknown skip-original/replacement semantics fail the affected owned group closed.

False/unknown predicates, checker exceptions and installation failure leave affected behavior off;
rollback only newly installed owned patches. Preserve native/provider actions and all unrelated
Defs. One bounded startup warning per skipped enabled patch identifies package/patch/spec, target,
failed condition, unavailable behavior and fallback. Startup is idempotent; later supported
invalidations/revalidation do not duplicate stages or remove foreign patches. Hashes/version and
absence of other owners are evidence/policy inputs, not semantic compatibility predicates.

## Risks / Trade-offs

- Coordinating slow waits makes slaughter more reliable, including during a slaughterer episode →
  intentional source correction, but ordinary emergency interruptions and target rules stay intact.
- Large bodies/low speed can take a long time → preserve source curve, real progress and cancellation;
  measure finalized comparators, no arbitrary shortening or timeout introduced.
- Shared Def identity may collide → guarded addition, original-provider suppression and no foreign
  replacement; exact optional consumers still need real inspection.
- Entry-time and load-time work can disagree → persist narrow job facts, test remaining/total work
  and exact wait ownership, and fail safely on unprovable associations.
- Source cache avoids a few stat calls but holds stale references → compute once per attempt using
  native facilities; measure actual cost before any shared caching, no speculative framework.
- No public source/license found → behavior reimplementation only, no copied binaries/code/art.

## Migration Plan

No automatic source settings/save import. Source itself has no custom save fields; it still changes
in-flight native job duration, so do not infer safe mid-job switching from the author's broad claim.
Validate native/source/owned transition cases separately. If a future explicit save conversion is
needed, specify backup, atomicity, restoration and verified postconditions before enabling it.
All normal save/configuration writes, runtime builds/installations and game launches are deferred.

## Open Questions

SSK-Q1–4 in the [ledger](../../../mods/megamod/docs/refinement/specification-progress.md) retain
effective stage/serialization/Def admission, optional consumer/provider binding, finalized timings
and UI measurements, and focused TDD/reviewed native acceptance. S118 owns Cooking/yield questions.
