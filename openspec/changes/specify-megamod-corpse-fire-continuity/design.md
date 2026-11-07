## Context

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S091/P2-20. [Research](../../../mods/megamod/docs/refinement/2026-11-05-corpse-fire-continuity-research.md)
pins the source's single class, selected DLL, native death/holder/fire chain and bounded consumers.
This is documentation only.

## Goals / Non-Goals

**Goals:** restore the intended one-time native fire handoff after corpse placement, preserve actual
flame strength/instigator and normal cell/fire rules, isolate each death and admit effective semantics.

**Non-goals:** permanent corpse fire, making corpses attachable, overriding water/rain/foam behavior,
guaranteed corpse disposal, changing Anomaly resurrection, death effects, corpse placement, damage
rates or normal holder extinguishing. No new setting, UI framework, scan/cache or persistent state.

## Decisions

### Repair the consumed data at its actual loss boundary

In inspected 1.7.4998, Pawn.Kill eventually calls MakeCorpse, places it Direct or Near, then queries the
original pawn's Fire attachment and calls FireUtility.TryStartFireIn(corpse.Position, corpse.Map,
fire.CurrentSize(), fire.instigator). MakeCorpse sets Corpse.InnerPawn, whose innerContainer.TryAdd
invokes ThingOwner.NotifyAdded. ShouldAutoExtinguishInnerThings returns true for every non-Map
holder, so putting the pawn into its corpse destroys that attachment before the later query.
This concrete chain, not a generic guess about DeSpawn, explains the late lookup.

Capture the remaining ordinary fire's scalar size and instigator immediately before the admitted
corpse-holder transition removes it, then feed only the matching successful on-map placement's
normal fire attempt. Prefer a local payload in the narrowly transformed death body. Do not globally
change ThingOwner auto-extinguishing or AttachmentUtility.GetAttachment, resurrect a destroyed Fire,
or inject a second generic Pawn.Kill postfix which fires without knowing the placement branch.

Capturing at the actual loss boundary preserves legitimate prior extinguishing or changing flames
from other death effects; source captures at Kill entry and shares a static Fire reference. Bind the
payload to this pawn, this invocation and this placement branch. A repeated same-pawn callback or
already-consumed payload cannot add another fire. If a contextual helper is unavoidable, use scoped
invocation identity and guaranteed cleanup on success/skip/exception, never one process-global slot.
Do not retry a partially executed death or roll back foreign death effects to obtain a flame.

### Preserve the native attempt and subsequent lifecycle

Current Fire.CurrentSize returns fireSize. FireUtility.TryStartFireIn tests the cell's ordinary
chance/eligibility value for >0; it does not itself roll an extra probability. It spawns a new native
Fire with supplied size/instigator. Any Fire already in the cell makes the eligibility value 0. Thus
do not add an extra random roll, grow an existing fire or assume that only static fires block it.

Normal eligibility includes flammable terrain/items, impassable-edifice interior and fire-blocking
filth. A nonflammable floor alone does not reject a corpse as fuel. Extinguishing terrain, rain,
vacuum, available fuel, damage targets and later spread remain Fire's native runtime behavior; some
conditions can extinguish a newly started fire rather than block initial placement. Do not simplify
this into a blanket promise that fire is never created on water or always consumes the corpse.

Only the successful spawned corpse placement branch participates. Native enclosed-holder, caravan,
world-pawn/no-corpse and failed-placement branches do not execute this handoff. Preserve that scope;
do not retain delayed fire promises for a corpse spawned later. Capture must be finite positive size;
invalid values decline this handoff with a bounded feature diagnostic, not a default/max-size flame.
Preserve the original instigator, including null; the fatal DamageInfo's instigator is a different
concept. Avoid retaining live references after the invocation; native Fire owns its saved instigator.

The fire is static cell state, not a corpse attachment. Its ordinary SpawnSetup/timers, exposed
fireSize/ticksSinceSpawn/instigator, growth, damage, heat, suppression and disposal stay native.
Moving or resurrecting the corpse does not drag this fire or explicitly attach it to the revived
pawn. Normal nearby fire can subsequently ignite a pawn through ordinary eligibility.

### Semantic admission and single-provider policy

Before implementing any hook, create stable target documents and executable predicates for the
actual capture/use group. Prove the same pawn's remaining Fire is lost through corpse containment,
the success-only placement branch supplies the actual corpse cell/map, and the size/instigator
reach the intended normal TryStartFireIn call exactly once. Match typed operands/control/data flow
and required cardinality, not every attachment call, fixed local numbers, exact hashes or whole IL.
Preserve labels/exception blocks when replacing instructions.

Validate effective semantics and meaningful skip-original/transpiler interactions. The source
replaces every GetAttachment call and only warns after emitting modified instructions when its
count is not 1; do not copy this partial-failure policy. Validate the actual incoming stream on every
transpiler invocation before yielding changes, returning it unchanged on mismatch. Never replay
foreign transpilers as speculative probes. Accept benign edits and compatible fire/death providers;
recognize a game/provider which already preserves the needed data and do not add another handoff.

Install idempotently as an atomic owned group; validator exception, ambiguous shape or installation
failure withholds the group and rolls back only newly added owned hooks. Revalidate supported later
repatches. Each skipped enabled patch logs package/patch/spec ID, target, failed predicate, lost owned
behavior and fallback once at startup. Already-fixed behavior/source overlap is explained as such,
not falsely reported as unavailable normal fire. No per-pawn ticking warning or unpatching others.
Active canonical `tixiv.keepburningcorpse` suppresses owned overlap whenever the original provider is loaded.

No actual source-API consumer was found in bounded installed `.cs/.xml/.json/.csproj` searches;
public fire_tmp/MyGetAttachment/patch methods do not justify a compatibility façade. Keep native
death/corpse/Fire contracts and identities. Optional external C# adapters, if later required, must be
canonical-package/Def-resolved, absent-safe and shape-guarded. Do not guess a fire overhaul contract.

### Composition, dependencies and configuration

S079 owned fire response can extinguish a live pawn before death, or prevent a new attached fire.
S091 uses only the actual remaining fire; it does not repeat apparel ignition resistance on the
corpse's static-fire handoff or cancel a successful extinguishing. Any future combined policy is a
direct owned call; no self-Harmony layering. External death/pre-death changes must be admitted by
their actual consumed semantics. No race/faction filter is added beyond native fire/corpse eligibility.

Core and external Harmony required on implementation. No DLC required; Anomaly supports additional
ordinary corpse/resurrection scenarios when present, not a dependency or separate custom mechanic.
No XML/Defs currently changed. Any later XML use must add XML Extensions as required with guarded
content/package/node checks and merge-preserving mutation. No Gateway/foreign DLL/private Harmony
shipment or dependency on another repository-owned runtime mod.

Source has no settings, UI or save schema. This unconditional continuity repair needs no numerical
setting or operational toggle; global/save inheritance is inapplicable. No player-facing entry point
is introduced. Common support/failure explanations must use all seven baseline languages where
displayed. Per-death payload is transient, not serialized or added to a shared Thing index; native
fire/corpse objects remain the data owners. No repeated map scanning or performance claim.

## Risks / Trade-offs

- Death invokes complex/nested provider side effects -> local invocation payload at the actual loss
  point; test distinct victims/instigators, stale/skip/exception paths and preserve foreign effects.
- Future game version already repairs the branch -> detect effective preserved data and stand down;
  evidence version/hash identifies research only, never the compatibility predicate.
- Cell fire differs from a permanently burning corpse -> explicitly retain native refusal, placement,
  suppression/movement and resurrection behavior. Increased corpse fire exposure is intended; no
  guaranteed burn-to-destruction or anti-resurrection rule is claimed.

## Migration Plan

No health/corpse/fire conversion, source-settings import or pending-fire persistence. Existing fire
saves remain native. Enabling affects future admitted deaths only, without reigniting old corpses.
No mutation of the user's normal configuration or saves during implementation verification.

## Open Questions

KBC-Q1–3 retain exact effective capture/use/exception contracts, provider/S079/native-fix composition,
and reviewed-build native actions/localization/balance acceptance. Static inspection is not proof
that the future transpiler is correct or that a real death visibly preserves fire.
