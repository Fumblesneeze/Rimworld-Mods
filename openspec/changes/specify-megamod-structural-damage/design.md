## Context

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
S072 source is `Victor.WallsAreSolid`, Workshop2896548513. Read the
[research](../../../mods/megamod/docs/refinement/2026-11-04-walls-are-solid-research.md) for exact
selected files, hashes, primary sources, default disagreement and static evidence limits.
No runtime implementation or target-admission document exists yet.

The source edits DamageDefs at startup, adds reciprocal target multipliers to exempt buildings,
converts selected head capacities, and changes two exit-job arguments. Literal replication would
lose independent save settings, risk division by zero and inherited-list damage, and accidentally
change a dedicated breaching attack. Its raw XML selectors are not a reliable finalized category.

## Goals / Non-Goals

**Goals:** observable structural effectiveness, explicit default scope, preserved modifiers and native
attack mechanics, native breaching alternatives, optional source coverage and coherent failure.

**Non-goals:** new physical content, weapon rebalancing against pawns, guaranteed escape, global AI/path
rewrites, improved raid intelligence, generic combat-overhaul support, source settings import, borrowed
source package/assembly identity or copying its code. No source license was identified in this pass.

## Decisions

### 1. Compose damage once rather than undo it with reciprocals

Use one owned pure decision with victim classification, supported damage family and current effective
settings. Return a factor, including neutral1 and zero; no division by a configured value. The runtime
integration belongs at the native **building** damage-factor stage of `DamageWorker.Apply`, before
final hit-point rounding. Preserve earlier `Thing.TakeDamage` per-target multipliers and rounding,
native general/passability factors, flammability, shambler and Biotech door factors. Do not instead
modify the initial `DamageInfo.Amount`: that changes ordering and rounding relative to other effects.

`DamageWorker_AddInjury.Apply` delegates nonpawns to that base path; pawn injury remains separate.
Custom workers can bypass it or apply damage recursively. An adapter must prove which final building
application it governs and avoid duplicate scaling. Never classify by a damage name substring or
armor category alone: native Poke has blunt armor/general factors but source sharp-family intent.

The ordinary family matrix and crushing bonus are **additional** to existing independent factors. Native Bomb passable2 /
impassable4 explains normalized0.4 /0.25 from installed source0.8 /1; native nonexplosive roots are1.
Common0..3 ranges preserve the source's corresponding possible Bomb0..6 /0..12 range on those roots.
This intentionally differs where another mod supplies explicit factors which source SafeAdd retained
or Bomb replacement overwrote. Do not advertise exact numerical parity across arbitrary mod lists.

Four paired settings and two booleans use the foundation's common definition/load/store/resolve/UI
path; no separate XML Extensions settings menu. Resolve at the damage event from the active game,
never rewrite shared Defs when a save opens. No-game contexts use global defaults. Apply pair validation
atomically. Stable IDs/schema are implementation work; no automatic legacy-key migration is selected.

### 2. Semantic target and family classification

Classify HP buildings by effective `isWall`, door identity and supported mineable-barrier semantics;
passability comes from the Def, not current door openness. Default exemption means every other building
skips ordinary family scaling. This is a deliberate correction from source claimability/draw-style/
direct-parent selectors. BuildingNatural/RockBase ancestry identifies the optional immunity group,
including smoothed/collapsed variants whose `isNaturalRock` is false. Do not include loose Item chunks.
Changed/unprovable optional classifications remain outside this contribution with a target diagnostic.

Family records use inspected roots/identities and admitted inherited behavior. Explicitly exclude
Demolish and Thump **before** ordinary Blunt/Bomb ancestry: Demolish inherits BluntBase but defines a
dedicated general10/impassable0.75 policy. Source's added Blunt passability can leak into its passable
value. Recognized head strikes also precede ordinary-family matching. Unsupported custom damage remains neutral.
The immunity option covers exact native RangedStab, Bullet, Arrow and ArrowHighVelocity effects;
do not silently extend it to optional lasers or every descendant carrying independent behavior.

No map/world scan or private Thing cache is needed. Reuse resolved Def relations and the current victim.
If immutable classification is precomputed, tie it to the finalized Def/load generation, not map save
state. Do not add a reusable index framework; there is no shared Thing-dataset consumer here.

### 3. Recognize the native head attack without replacing its damage identity

Retain the original native Blunt DamageDef, tool capacity and maneuver. Current `DamageInfo.Tool`
identifies the actual selected tool; `RimWorld.Verb_MeleeAttackDamage.DamageInfosToApply` stamps its
primary damage with SetTool(tool), whereas its separate extra-damage records do not carry that stamp.
Admit this causal delivery before relying on it. Match the finalized supported BaseWeapon descendant,
its actual selected `head` tool with Blunt capacity and primary native Blunt damage. Verify tool
membership against the actual weapon; do not infer the strike from an equipped mace alone. A handle,
another capacity or unproven custom delivery does not qualify. Additional damage keeps its own policy.

At the already admitted building factor stage, select fixed passable2/impassable1.5 instead of the
ordinary blunt pair for that primary head strike. This preserves native target multipliers keyed to
Blunt, worker/armor effects, attack weights and ordinary labels without a new damage identity, extra
capacity or weapon conversion. Effective inherited tools can be read from finalized Defs; no mutation
is needed to preserve or discover them. Absent/unsafe head recognition leaves the native attack usable
under the ordinary blunt policy if that independent contribution is admitted.

Rejected source-like new DamageDef: identical worker/armor is insufficient. `Thing.TakeDamage` compares
damage identities exactly, so a new Def bypasses native `damageMultipliers` keyed to Blunt. Review found
installed `FTC_BasePermanentStructure` with Blunt0.25 (Workshop2887532326). A new alias plus damage
rewrite adapter would add risk where native Tool context already supplies the needed distinction.
Source BluntCrushing/SmashCrushing aliases and the cosmetic smash(crushing) label are not retained;
no actual consumer requires them in the inspected set. Exact custom consumer claims still need proof.

### 4. Keep fixed mining content declarative and merge-preserving

The fixed Mining0.5 contribution also belongs in guarded declarative XML for constructed nonmineable
walls. Preserve existing/inherited damage multipliers and their order; add one owned half-damage
contribution after existing applicable contributions, with provenance/idempotence so it cannot be
applied again to both a parent and descendant. Do not reset a container to `Inherit=False`, replace
foreign Mining entries, or apply to Mineable targets whose final native hit bypasses ordinary damage.
Establish effective inheritance and contribution order in the actual XML phase before implementation;
if that cannot be guaranteed, fail this group closed instead of silently moving fixed Def edits to C#.

Inspected XML Extensions operations include `HybridPatch`, `PatchOperationSafeAdd`, `ForEachDescendant`
and `PatchOperationMath`. Safe/hybrid behavior is not automatically inheritance-safe or transactional;
math multiplication alone is not idempotent. Compose presence/shape/idempotence guards and owned-only
transaction handling using the current registry, selecting actual supported operations at implementation.
For external content use `XmlExtensions.FindMod` with `packageId=true` plus exact content/shape checks.
The first XML use requires XML Extensions metadata and load order in that implementation slice.

### 5. Narrow native exit contribution

Current target `Verse.AI.JobGiver_ExitMap.TryGiveJob` passes the selected blocker to
`RimWorld.DigUtility.PassBlockerJob` with canMineMineables and canMineNonMineables true. Change only
those arguments for that already selected dig-enabled exit decision. This is not a global patch of
DigUtility or mining jobs. Inspect argument data flow/control flow and unique relevant call, not the
source's unguarded first-call/two-previous-instructions assumption.

Keep DigUtility's ranged building-destroyer branch for nonmineables. Its UseVerbOnThing job retains
native failure/interruption; the helper does not itself guarantee nearby waiting on ranged failure.
Melee/mining helpers return WaitNearJob specifically when CanReserve fails. Preserve that melee
fallback and portal/flying/normal route selection. No retry scheduler, map scan,
new damage job, reachable-exit promise or forced weapon selection. Actual AI progress under reduced/zero
damage must be observed; native inability to break a blocker is not solved by adding a loop.

### 6. Optional identities and direct owned composition

Source adapter inventory and full Def lists live in research. Primary identity evidence exists for
`sarg.alphaanimals`, `VanillaExpanded.VGeneticsE`, `lts.I`, `jecrell.jecstools`,
`OskarPotocki.VFE.Vikings`, `sarg.magicalmenagerie`. Oddities Core/Weapons package identities remain
research tasks; third-party logs are insufficient. Inspect actual selected provider workers before
claiming support. Package presence alone does not prove damage calls pass the admitted stage.
An absent provider is not a failed enabled patch. Present requested support whose prerequisite is
unknown is skipped with a bounded warning and native/provider fallback.

S037 blunt-social-fights uses actual armor category/attack eligibility: the original Blunt identity
remains without changing the social selection policy or adding equipment to native social choices.
S023 item-destruction is item-only and never gains
structural factors. S061 roof-safe-removal still controls safe designation/job selection; ordinary
Mineable final removal remains native. Use direct owned policy calls, not patches of megamod features.
The exit substitution intentionally changes a Mine job into combat: S061 excludes ordinary attacks.
Even with S061 Other workers enabled, an escaping foreign pawn's new attack does not prepare/cancel
for roof safety and can destroy required support with native collapse consequences. Do not quietly
route combat through roof preparation or imply the former mining safeguard still protects that action.
S097 mining priority is not specified yet; record shared path context without inventing a conflict.

Installed CAI (`Krkr.rule56`) metadata names the source package as incompatible; source warns about
CAI/CE and declares `Aelanna.FistNerf` incompatible. These are provenance, not a whole-megamod blacklist
or verified incompatibility with this design. Before claiming cooperation inspect actual AI valuation,
damage workers and foreign patch semantics. Preserve independent compatible modifiers; disable only
the contribution whose prerequisites cannot be established. Never unpatch another owner.

### 7. Target documents and admission are required before code

Create stable target IDs/index entries from the mod template for every actual patch, not one generic
"method exists" entry. Planned groups below are design candidates, not implemented validators.

| Contribution | Minimal consumed semantics | Failure result |
| --- | --- | --- |
| Ordinary factors and optional immunity | Exact building-victim damage computation, family/victim inputs, ordering before final rounding and HP mutation; relevant skip-original/reentrant/foreign worker behavior | Leave native/provider damage; preserve settings and show unavailable state |
| Crushing recognition | Actual primary DamageInfo.Tool delivery/membership, supported finalized head/capacity, native Blunt identity and secondary-effect distinction, plus admitted building computation | Native attack remains; ordinary blunt policy only if independently admitted |
| Constructed-wall Mining contribution | Effective wall/nonmineable classification, Mining entry/list inheritance/order, exactly-once owned addition | Retain original multipliers and ordinary mining |
| Exit attack substitution | Existing dig-enabled blocker branch and the two mining arguments to the relevant PassBlockerJob call | Native exit/mining decision |
| Each optional adapter | Canonical active package, exact expected Def/worker/family path and effect boundary | Provider behavior unchanged for that adapter |

Validate minimal semantic prerequisites, including effective foreign transformations and relevant
prefix skipping; unrelated instructions or other Harmony owners are allowed. Every transpiler checks
the actual incoming stream on every invocation, including later repatching, returning it unchanged on
failure. Do not repeatedly execute foreign transpilers as probes. Record later-repatch revalidation
and owned-only withdrawal for dependent groups. Validator exceptions/install failure require atomic
rollback of that newly owned group, never foreign unpatching. XML failure requires equivalent no-partial
results; a stock sequential PatchOperation list is not a rollback guarantee.

Bounded startup warning per skipped enabled patch includes package/patch/spec ID, target, failed
predicate, lost behavior and fallback. Explicitly disabled or absent optional candidates are not
failures. Original `Victor.WallsAreSolid` authority suppresses **all** S072 runtime/XML contributions,
including crushing/mining and exit behavior; preserve saved preferences and display why
they are inactive. This requires choosing authority before any owned XML mutation, not just before
Harmony registration. No source settings import or promise that source authority obeys owned corrections.

## Risks / Trade-offs

- Relative factors preserve other mods but differ from source overwrite/compensation behavior → show
  the normalization and semantic scope in settings help/replacement coverage; verify exact load lists.
- Zero factors and slower structural destruction can prevent AI escape → no invented progress promise;
  observe melee, ranged breaching, contention and interruption, retaining native alternatives.
- Exit combat can collapse roofs which covered exit mining would have prepared or refused → explicitly
  specify this interaction and observe it; other covered removal work keeps its existing safeguard.
- Semantic scope covers smoothed rock and excludes furniture by default → explicit observable examples;
  measure constructed walls, doors, natural/smoothed rock, turret and loose chunk separately.
- Custom attack delivery may not retain native primary Tool context → omit that head enhancement when
  unproven; preserve native Blunt identity, exact target resistance and secondary effects. No universal
  custom-worker or source-Def alias support claim.
- Static XML grouping/order can fail after foreign changes → prove finalized inheritance/idempotence
  and transactional failure in an isolated game; no inferred acceptance from valid XML.
- Settings can differ per save → resolve on use; no mutated global DamageDef fields or retained previous
  game's overrides. Measure hot-path allocations/cost; no performance claim without measurements.

## Migration Plan

This slice writes documents only. Later use the existing foundation package; add required dependencies
with the first actual use. Do not copy upstream binaries, change installed Workshop content or migrate
source configuration. No new crushing Def or physical inventory object is introduced. Removal restores
ordinary behavior on a fresh startup; existing HP loss is not undone. Save-scoped settings retain unknown
keys according to the common policy, without injecting source state. Any future automatic migration
requires a separately specified backup, atomic conversion, failure restoration and verified result.

## Open Questions

WAS-Q1–5 in the refinement ledger retain exact effective admission/XML inheritance, proposed scope and
balance, optional providers/consumers, AI/composition and implementation/native acceptance. None require
interrupting this specification pass or authorize checking runtime tasks.
