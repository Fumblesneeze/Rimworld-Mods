## Context

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
S080/P2-09 replaces `Defi.StabilizeBleeding`, Workshop3521178066. Read documentation first, then the
entire small installed assembly, JobDef and keys. Current native evidence:1.7.4998rev1299, hash and
read-only extract paths in [research](../../../mods/megamod/docs/refinement/2026-11-04-stabilize-bleeding-research.md).
This is a design for later implementation; no source code or assembly is copied or executed.

## Goals / Non-Goals

**Goals:** the advertised bleeding-only action, correct current patient/condition selection,
ordinary unmedicated consequences, direct owned medical composition, safe job lifecycle.

**Non-goals:** automatic triage, ordinary all-condition Tend, a second temporary-stabilization
system, free healing, extra settings, source-save import, a generic medical framework or a map cache.

## Decisions

### Use the menu extension and an owned medical job

The source uses `FloatMenuOptionProvider`, not Harmony, plus `StabilizeHere` JobDef. Prefer this
public extension mechanism and a prefixed owned JobDef with a medical feature-local driver.
Current `FloatMenuMakerMap.Init` discovers concrete provider subclasses and constructs each through
Activator; no registration Harmony patch is evidenced. Prove effective startup timing, provider
whitelists and future shape admission before implementation. Gate availability and job creation on admission status.
Use one eligibility policy in presentation/start/commit; an enabled menu is not proof a later
reservation, manipulation or patient permission still holds. Preserve native ordered/queued-job
path and rejection reports. No UI diagnostic panel or configuration switch is needed.

Source details worth retaining: drafted/undrafted single-selection, ordinary Medical>0 or permitted
mutant, wider actual patient eligibility than the short description, native proximity/reservation,
self permission, no medicine targets/fetch. Intentional corrections: require manipulation, reject
aggressive patients consistently (source menu allows Scaria but driver rejects aggression), treat
medical ability independently of dumb labor, and apply current care/self permission at commit.
Base provider excludes mechanoids and fogged/despawned targets. Missing-part bleeding must qualify.

### Filter the real treatment rather than only testing whether bleeding exists

Source driver calls `Toils_Tend.FinalizeTend(Patient)` unchanged. Current native finalization awards
XP then calls `TendUtility.DoTend`, which chooses every `TendableNow` candidate using ordinary
priority. A life-threatening non-bleeding condition can outrank a bleeding injury. Native
`GetOptimalHediffsToTendWithSingleTreatment` accepts a caller-provided ordered list, but `DoTend`
does not pass one. The source's two `HasBleedingWounds` predicates are not a treatment filter;
menu requires `Hediff_Injury`, while the driver also accepts other `Bleeding` hediffs.

Implement one direct owned medical selection policy: finite positive current bleeding contribution,
current tendability, current resolved care!=NoCare, valid actor/patient and supported effect contract.
Sort by bleed contribution descending, native TendPriority, severity, then stable hediff identity.
Reuse S062 care resolution directly. For this order, individual NoCare wins even though the source
does not enforce it; no lasting general-policy mutation or temporary global replacement list.
Preserve legitimate no-medicine same-def `tendAllAtOnce` only inside the filtered set. Normally this
is one wound per600/speed ticks; no medicine means no20-severity medicated injury batching.

Select a narrow way to apply the filtered set while retaining native quality, notifications,
relationships/possible animal bonding/recruitment, guest counters, records, self-tend notice,
Ideology role effects and quest signal. Reimplementing just `Hediff.Tended` omits real effects;
calling unrestricted `DoTend` violates the selected set. Resolve this seam before coding (SB-Q2).
A supported external treatment adapter is package/shape-gated; no compile-time foreign references.
Do not invoke an unsafe provider then retry native after it may already have mutated health.

Each successful commit owns exactly one batch and completion credit. Revalidate after the timed
work, before XP/effects, and avoid invoking any finalization for an empty set. Interrupted partial
work earns no completion credit. Track the condition identities already attempted without useful
change during this order; a persistent tendable/bleeding mod condition must not cause infinite XP
or retries. Clear only in a genuinely new valid order; explain no useful remaining treatment.

### Keep physical work and temporary waits honest

Use native stats, timing/rounding and health behavior rather than freeze a1.6 copied toil sequence.
600/speed and0.3/0.7/0.7 are inspected baseline comparators, not extra owned multipliers/settings.
Bad non-finite/non-positive work speed fails the affected action; no division-based instant tending.
Source computes path mode in `Notify_Starting` and never scribes it; current native TendPatient
scribes `pathEndMode` and `usesMedicine`, while base JobDriver scribes current toil/remainingticks.
Owned serialization/reconstruction must preserve correct bed/self/touch positioning and remaining
work. Use supported reconstruction for current native lifecycle, test load during each stage.

Source WaitWith calls `PawnUtility.ForceWait` and cleanup cancels any current Wait/Wait_MaintainPosture,
not just its own. Retain an ownership link for only the temporary wait introduced by this order;
never cancel a replacement player's wait. An occupied bed remains a bed, no spot is spawned.
Re-evaluate immediate reach and target life/faction/mental state; clean owned reservation/wait on
completion, cancellation, death/despawn/map transfer and after loading an invalid job.

### Compose direct medical behavior, not internal patches

S042 supplies self permission; S007 compact presentation reads actual outcomes; S058 automatic
ranking yields to an explicit valid order; S062 provides current per-condition care and shared
medical result policy. No medicine-selection/index query is needed for S080. S024 cleaning, S021
opportunistic hauling and stock refills cannot prefix this emergency order. There is no map-wide
scan/cache: inspect the selected patient's actual health only at relevant action boundaries.

S062 FC06 explicitly defers CE stabilization outside initial support. Ordinary owned medical actions
share reservation/ownership and update subsequent automatic choices from actual results. No
same-patient simultaneous treatment, synthetic medicine dose or source-like cross-patches.
S124 Drug Response and S134 Priority Treatment need their own later sequential source analysis;
do not invent drug-precedence policy now. Their implementation must reference this explicit action.

### Admission and co-installation

`Defi.StabilizeBleeding` active => owned overlap off, even if the provider's UI happens to be absent;
do not fill perceived gaps or introspect foreign configuration. Source exposes assembly `Stabilize`,
namespace `Stabilize`, public provider/Base/DefOf and unprefixed JobDef `StabilizeHere`. The separate
original Stabilize mod also uses these names; bounded text matches are not incoming consumers.
No fake assembly/package/Def alias without proven consumer requirements. Existing foreign jobs need
the foreign provider; complete/cancel them before removal, with no promised automatic conversion.

Native public extensions still require startup shape/semantic preflight for consumed behavior.
Candidate groups, **not yet executable target specs**:

| Candidate | Required minimal semantics | Failure boundary |
| --- | --- | --- |
| SB-MENU | Discovered provider receives actual selected doctor/clicked pawn; native queued order creation and current reservations | No owned contextual action |
| SB-TREAT | Exact selected condition set can be treated with native no-medicine work/quality/effects once; unrelated candidates not substituted | Entire owned action inactive, ordinary tending unchanged |
| SB-WAIT | Positioning, wait ownership, interruption and save reconstruction support exact lifecycle | No partial job offered |

Prefer no Harmony where the public path suffices. If a bridge is required, create a stable per-patch
document and executable check: full callable shape, necessary data/control-flow, actual effective
chain and skip-original behavior, supported later repatch revalidation. A transpiler validates its
incoming stream every run and returns it unchanged on failure. Accept unrelated changes and
compatible owners; no exact hashes/full method text or no-other-owner test. False/unknown/throwing
checks or installation failure deactivate the atomic dependent action; rollback only newly owned
patches and emit one bounded invariant startup warning per skipped enabled patch. No PatchAll.
Public-extension failure gets a bounded feature diagnostic even if there is no actual patch.

Add a static owned JobDef declaratively. No XML patch is presently justified. If later composition
requires one, use XML Extensions package/content/node guards, idempotent merging and atomic failure;
declare its hard dependency then. Harmony is required when actually used. No DLC dependency for
base action; optional mutant/Ideology semantics remain absent-safe; CE remains deferred. No Gateway product coupling.

## Risks / Trade-offs

- No medicine removes retrieval delay but retains slow work and limited quality; patients can die
  during care. No invented speed boost or guarantee. Baseline, self-care and medicated ordinary Tend
  are the three comparison cases for final native balance, not three claimed live measurements.
- The skill1 floor and broader patient scope come from source; consistent care restriction,
  missing-part inclusion and no-aggression exception are proposed corrections (SB-Q1).
- Bleeding-only order deliberately ignores disease urgency for this explicit action; automatic
  patient triage remains separate. A condition's later ordinary treatment stays available.
- Foreign medical patches can replace batching/effects; current original IL alone is insufficient
  to prove a future bridge. Exact supported combinations require live workflows.
- Partial treatment failure cannot be undone by pretending it never occurred; record actual effects,
  stop subsequent duplication, and require atomic admission before offering the action.

## Migration Plan

No source settings or jobs imported, no original identifiers spoofed. Owned job schema versioning,
valid save resume and invalid-job cleanup need tests before first implementation. Installation
uses canonical typed packaging; this document neither deploys nor accepts anything.

## Open Questions

- **SB-Q1:** Final policy/balance review of retained Medical1 floor, broader patient scope,
  NoCare precedence, consistent aggression rejection and missing-part/modded bleeding inclusion.
  Defaults/corrections are explicit draft decisions, not questions asking the user to reverse engineer.
- **SB-Q2:** Prove current discovery, selected-set finalization, effective external interactions,
  wait ownership, nonprogress definition and save reconstruction; write actual target specs if patched.
- **SB-Q3:** Complete incoming compiled/reflected consumer and external medical-provider analysis
  where support is claimed, including the distinct older Stabilize identity and later S124/S134.
- **SB-Q4:** Focused honest RED/GREEN, package/seven-language UI, reviewed-build native action/result
  and playability evidence. All implementation/acceptance tasks remain unchecked.
