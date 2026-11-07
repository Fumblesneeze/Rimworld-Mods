## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Specification-only S019 replacement of **Matheus.MostSkilledBill**, Workshop **3757516418**.
[Research](../../../mods/megamod/docs/refinement/2026-11-02-most-skilled-bill-research.md) records all
22 installed classes, metadata/hash, source UI/defaults/cache and exact BWM methods. Installed DLL
1.0.0.0 supports 1.5/1.6, not declared 1.7; SHA-256
0C6BA86C54A0931DA489C1F7EC37879B15D259AAC261DF661F5A136A9DDC1258. No source license/repository was
found in the bounded inspection; independently implement behavior, do not copy or ship its binary.

Native Bill.PawnAllowedToStartAnew immediately returns pawnRestriction == p for a named assignment;
that branch bypasses its later range and mechanitor checks. Other recipe/work-giver requirements
remain separate. With no named restriction, the method checks worker categories, allowedSkillRange
and mechanitor eligibility. Source Most skilled clears named/category fields but hides the unchanged range.
Its benchmark is any enabled work type sharing recipe.workSkill, not actual recipe eligibility.
Source global Widgets.Dropdown/IntRange patches are deliberately not our integration architecture.

## Goals / Non-Goals

**Goals:** explicit highest-skill restriction, current benchmark/explanation, exact source defaults,
recipe-capable candidates, away/downed semantics and direct S014/S017 composition with safe recovery.

**Non-goals:** job priority reassignment, a guarantee of output quality, changing skills/recipes,
automatic default-on setting, medical-bill extension without its own specification, source API/save
impersonation, unproven compatibility with every work-management provider, runtime implementation now.

## Decisions

### Complete worker choice, visible range

Represent Most skilled in the existing owned worker-policy value, with includeAway/ignoreDowned
flags and allowed range. Both flags default false. Most skilled and native named/category policies
are mutually exclusive, but the range remains active and visible. This avoids source hidden-range
deadlock without silently discarding a deliberate range. S017 bench policy and S014 logical-row
transactions copy/propagate the whole value. Each recipe computes its own benchmark; a shared list
does not pick one universal person or skill. Snapshot copy/paste is through the existing owned path.
For ordinary named assignments, retain native range applicability: the range is stored for subsequent
non-named choices but is not a new named-worker gate. S019 introduces no stricter named-worker rule;
any such future change would need an explicit proposed refinement and separate observable scenarios.

Source BWM compatibility hooks public instance BillCopyPaste.DoCopy(Bill_Production) and
DoPasteInto(Bill_Production); Copy all, paste-new and linked propagation are not proven by those
hooks. Compose directly, with no patches against our own mod. Do not retain static independent
clipboard booleans. Group merges/paste use the S017 destination-policy preview/atomicity rules.

### Benchmark capability versus immediate availability

Use source current skill Level or supported mechFixedSkillLevel, retaining ties. Determine candidate
work/recipe capability from actual native work-giver/recipe rules, range and required skills. Do not
call the fully patched worker predicate recursively to compute its own maximum. Separate reusable
native capability evaluation from the owned rank restriction and the actual worker's start check.
For linked rows, a candidate capable of the recipe at a member can set the shared benchmark; actual
execution still checks that specific bench. Unknown provider-specific capability yields unsupported
or unavailable evaluation, not guessed true. Prove vanilla/selected DLC paths before optional adapters.

This intentionally refines source's any-relevant-work-type maximum: a high tailoring-only pawn must
not block a recipe they cannot perform. Preserve quality-standard waiting for busy/asleep/drafted/
unreachable experts, rather than estimating the soonest free worker. IgnoreDowned controls that
specific exception. A tested manually forced pawn still passes native gates and must meet/exceed the
maximum; work-priority-disabled manual orders are not blocked merely by absence from the automatic
benchmark. An empty valid candidate set imposes no extra minimum, whereas failed evaluation blocks.
Use explicit result state and explanation, not zero as both empty and failed.

Native WorkGiver checks PawnAllowedToStartAnew before bound UFT and formed-output selection. Integrate
with S014's commitment/continuation boundary so raising a benchmark does not strand the previous
creator's work or collection. Do not simply inject a global false prefix and call it new-work-only.
Ordinary continuation, creator identity, reservation, physical inputs and machine state remain native.

### Shared facts and away association

Reuse native listers and the common map-owned candidate facts, plus S017's saved home-association
service. Share facts, not S018 query membership or S017 stock preferences. These have different
eligibility contracts. No unconditional ThingDef comp injection: own saved associations suffice.
The source humanlike-only comp and spawned-controlled-mech test do not establish away-mech support;
the draft intentionally requires an explicit known ownership/work-capability path for such mechs.
Treat unproved off-map capability as unavailable, not an unrestricted skill-zero fallback.

Cache only a concrete derived benchmark when justified, keyed by recipe/capability, map/group scope,
range, flags and candidate data revisions. Invalidate work assignment/skill/disability, pawn status,
downing/recovery, faction/ownership, spawn/despawn/container, home and map changes, and policy edits.
Do not preserve source's 30-tick stale window or 500-key sweep as a player contract. Paused edits must
be current before UI/start evaluation. Clear per-game results on teardown and rebuild after load;
keep Verse access on its owning thread. Benchmark combined consumers/update/memory before claiming
optimization. Recheck native per-worker access/reservation at use time, never cache job permission.

### Scoped UI and admission candidates

Render through the actual BillConfig/bill-owner worker surface. No translated AnyWorker-label search
through unrelated FloatMenus, global closed generic Widgets.Dropdown patch or global 0–20 IntRange
interception. Source UI crash reports are unverified, but these broad seams are unnecessary for our
owned contextual controls. Preserve unrelated Assign painting and open-dialog owner identity.

| Candidate | Required consumed semantics | Failure boundary |
| --- | --- | --- |
| MS-T1 worker selection/UI | Actual bill owner and worker-choice mutation; range/options and unrelated buttons preserved; no shared generic-widget interception | Withhold owned selection and dependent new starts |
| MS-T2 eligibility/continuation | Native worker rejection remains authoritative; correct recipe skill/capability and physical new-start versus UFT/output paths; all consumed family overrides | Suspend affected saved choice; preserve independently safe continuation |
| MS-T3 persistence/copy/group | Correct logical owner, save/clone/edit/delete/transfer lifetime, complete flags/range, no physical work copy | Disable unsafe transfer/start while retaining recoverable data |
| MS-T4 candidate/home freshness | Required notification existence/timing and exact identity; state-mutating interception proves consumed fields/flow | Unavailable benchmark unless a proven fresh fallback exists |

These are design candidates, not completed target specs. Before each patch, create its stable target
document and executable semantic validator linked to focused tests. Observer callbacks can require
only callable existence/shape; argument-changing prefixes need the consumed use/control flow.
Account for skip-original prefixes and effective transpiler inputs, allow benign foreign changes,
validate incoming streams on every transpiler run and return unchanged on failure. Do not require a
pristine method, hash or absent foreign owner, or rerun foreign transpilers as a speculative probe.
Document startup and later repatch revalidation. Required dependencies activate atomically; roll back
only newly added owned patches and warn once per skipped enabled patch with IDs, target, reason,
unavailable behavior and fallback. Never unpatch another mod's owner.

Detect original Matheus.MostSkilledBill canonically and withhold overlapping owned behavior. Saved
owned restrictions must be recovered/suspended before any unrestricted job starts. If that protection
cannot be established, reject activation explicitly rather than discard policy or run Any worker.
Restored support requires Resume. Required Harmony metadata must be correct despite the source's
missing About declaration. No XML mutation is chosen; future XML changes require XML Extensions,
Def/node/package/content checks and merge-preserving individual target specs. No source/Gateway DLLs.

## Risks / Trade-offs

- Recipe-specific capability is stricter than source's broad skill benchmark → record proposed
  semantics, test different work types sharing a skill and unsupported provider capability.
- Busy or remote experts can intentionally stop work → explain current maximum/winners/scope,
  retain explicit away/downed choices, never silently prioritize throughput.
- Away-mech and unavailable-container support exceeds what source code proves → exact native proof
  remains required, with unavailable state if capability/ownership cannot be safely resolved.
- Worker-policy changes can interact with existing UFT jobs → use S014 commitment rules and observe
  real resume/collection on the final reviewed build.
- No concrete installed binary consumer was found → do not build a speculative compatibility facade;
  retain potential public extension/save keys and any future demonstrated consumer separately.

## Migration Plan

No runtime migration occurs here. Later add owned persistence and explicit admission-safe recovery,
then enable validated UI/decision slices. Do not import source mostSkilledOnly/includeAway/ignoreDowned
or originMap_MostSkilledBill keys implicitly. A future foreign migration needs its own backup,
atomic transformation/restore and verified postconditions. Test with isolated disposable saves only.

## Open Questions

MS-Q1 holds proposed recipe-capability/range/unavailability choices for later UX review; MS-Q2 exact
beta family/away-mech/container proof; MS-Q3 verified source contracts and owned composition; MS-Q4
validators, focused RED/GREEN, seven languages, independent implementation review and native acceptance.
No question interrupts sequential specification. This document does not close those tasks.

Semantic pass: comparison changes worker selection, not skill values, material cost, yield, research,
quality probability or job priority. Test ordinary Any worker, named worker and Most skilled on the
same recipe, including intentional waiting and ties. Final acceptance needs actual worker starts,
rejected manual orders, continuation, travel/downing changes and unaffected Assign UI, not diagnostics.
