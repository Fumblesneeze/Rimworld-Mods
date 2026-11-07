## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Original S023/P1-17. The user expressly rejected a DPS work-timer substitute: use repeated actual
melee attacks on HP-using items at their location. [Research](../../../mods/megamod/docs/refinement/2026-11-02-melee-item-destruction-research.md)
pins source/native inputs and distinguishes the reference mod from this requirement. No runtime
implementation or acceptance is delivered by this design.

## Goals / Non-Goals

**Goals:** actual native attacks, exact target authorization, ordinary effects, safe cancellation,
bounded failed attempts and direct owned processing composition.

**Non-goals:** HP-less deletion, installed-building/pawn combat, recycling refunds, weapon automation,
copied source thoughts/records, instantaneous debug disposal, source speed/multiple-worker preferences,
general combat AI replacement or foreign save migration.

## Decisions

### Item eligibility and exact order ownership

Use ThingCategory.Item, useHitPoints, destroyable and current physical identity/holder checks, not
MaxHitPoints alone or a list of named resources. Reject installed buildings/plants/pawns; a qualifying
MinifiedThing is a loose item wrapper, a proposed MD-Q1 interpretation with normal inner consequences.
Do not unwrap it into authority to attack a future building. Preserve native restrictions and actual
foreign damage semantics; a relic is not auto-exempt merely because native smelting rejects it.

Reuse the S022 owned processing-choice and S021 quantity/holder primitives directly. One owned
destruction record describes marked quantities, suspension/retry state and the actual current Thing;
never create a second per-mod map index. S096 actions replace the same choice through owned calls.
Preserve unrelated designations. Use native work reservations for exactly one disposer and prevent
concurrent processing; no claim of general immunity from external damage or ordinary released effects.

Track processing choice, active/suspended state and ineffective-attempt progress per authorized
portion, not one merged stack-wide flag. Merging, splitting, ordinary restart and reload cannot
activate another portion or erase its suspension. If native stack merging combines currently
attack-authorized units with unmarked, suspended or differently ordered units, isolate only the
currently attack-authorized portion through
admitted native split/placement at the same cell before attacking. Preserve HP, quality, Stuff and
provider state; do not reconstruct a healed survivor after damaging the entire stack. Placement must
not immediately merge the parts again. If that behavior cannot be guaranteed for the current stack,
defer with a status reason. Keep the operation bounded and recover the original physical quantities
on a failed split/placement. This may require a narrow owned-processing merge guard; it must not
globally rewrite stacking rules. Recheck before each actual attack release, not only job creation.

Known consumed/removed portions retire their corresponding authorization. When an ordinary native
removal supplies only a quantity from mixed marked/unmarked material, debit destructive authorization
first, capped at zero; do not clamp old authorization to the remaining physical count and silently
promote survivors. Where multiple processing choices/suspension states prevent reliable allocation,
suspend affected surviving orders for explicit quantity resolution. S021 cargo-versus-kept attribution
does not establish this processing attribution. Share this rule directly with S022 and future S096.

Ordinary hauling moves authorization with the held portion but pauses attacks; equip/wear/install/keep
cancels it. Map departure suspends and releases local claims; explicit Resume is required on return.
No Def-name substitution after loss or save load. Native HP is progress; there is no copied work bar
or separate synthetic HP. Source coexistence/admission failure must suspend before any saved job's
attack callback resumes.

### Own the job boundary, reuse native melee

Define owned Job/WorkGiver/Designation Defs. Use Hauling, after ordinary native hauling at equal
priority, with Manipulation plus real melee/violence capability. Source priorityInType=50 is evidence,
not a guarantee of actual order in the target beta; inspect finalized native order before selecting
the implementation value. Respect player priorities and current pawn permissions, including any
supported mech ability; do not grant animals a work type merely because they can bite.

Do not reuse JobDriver_AttackMelee wholesale. Its finish action can auto-retarget nearby identical
Things, and path failure can redirect toward a building. An owned job uses native adjacent movement
and melee selection/cast/cooldown seams under a current authorized item guard, with safe ordinary
work traversal instead of unconditional combat Danger.Deadly pursuit. A blocking door is not a target.
No benchmark-derived DPS timer, direct HitPoints write, damage calculator, synthetic DamageDef or
manual final Kill/Destroy shortcut belongs in this job.

Prefer the native Pawn_MeleeVerbs/usable melee Verb path, retaining normal selection including body,
equipment and supported terrain/hediff attacks. Do not call a generic ranged-or-melee chooser and
assume the returned verb is melee. Pin actual CanHit/IsUsable/FullBodyBusy/current-job semantics.
TryMeleeAttack returns true without propagating TryStartCastOn's result, so it cannot by itself count
attacks or prove progress. Identify actual cast release/completion and resulting damage through the
smallest existing callbacks or admitted observer. No fake surprise-attack flag, free extra XP or
cancel/reset of native cooldown to accelerate disposal.

Cancellation/suspension stops new casts and pending unreleased warm-up; already released effects
cannot be rolled back. A custom delayed melee provider needs explicit proven lifecycle boundaries;
if it cannot obey target/cancel scope, leave that route unsupported rather than assuming an immediate
vanilla strike. Keep callbacks scoped to the current owned job and current authorization; stale
callbacks cannot apply a new hit after another processing choice replaces the order.

### Real consequences and bounded failed attempts

Let actual native damage reach TakeDamage, relevant DamageWorker and Kill/Destroy(KillFinalize),
including pre/post handlers, real instigator, stack consequences, quest/record/filth/ignition effects.
Do not manually trigger CompDissolution after lethal damage: its inspected pre-damage path already
does the relevant work. Preserve ordinary corpse/container effects and warn contextually when known;
do not strip contents, manufacture refunds or promise clean disposal. No source custom memories are
granted at toil initialization. Ordinary immobile targets do not grant native Melee XP; add none.

Use a named default policy **10 completed ineffective attack cycles**. Track the lowest observed HP
and remaining authorized quantity during this attempt; reset the counter only on a lower HP or fewer
units. Count completed actual attempts, not ticks, a bool from TryMeleeAttack or a queued animation.
Natural warm-up/cooldown remains ordinary waiting. If a legitimate non-busy cast cannot start, end
with a reason immediately. Suspend a stalled worker/target attempt so the next work scan cannot
repeat it unchanged. Explicit Retry or a relevant worker/equipment/target capability change permits
a fresh bounded attempt; worker identity alone is not a capability change. Retain the counter and
low-water HP through interruptions, unchanged restart, split/merge and reload, including nine failed
cycles before an interruption. Persist that state with the authorized portion, not just the running
job. Reload must not clear suspension or replay effects.
Exact asynchronous provider progress and cancellation remain MD-Q2; do not hide uncertainty behind
an infinite timeout or force damage through an invulnerability rule.

### UI, dependencies and patches

Add the Orders class once through XML Extensions existence/shape and duplicate-membership guards;
declare required XML Extensions and Harmony in the implementation metadata/release slice. Keep
owned Defs additive and unrelated Orders entries intact. External XML must also use canonical
packageId=true plus real content/node guards. Do not copy source global work ordering or runtime
GUI-per-frame registration. Contextual UI is an admitted lifecycle registration, not a map scan.

No new preference rows are selected: per-item pending/cancel/resume/retry state is gameplay state,
and the bounded safety counter is a named policy constant. Future configurable policy needs an
observable spec and common global/save inheritance first. No source instant-mode setting relocated
into normal settings; the feature always performs real attacks. Translate Orders, status, known
effects, HP/quantity, cancellation and disabled reasons in all seven baseline languages. Artwork is
not selected here; any later icon follows the asset skill.

| Candidate boundary | Minimal consumed semantics | Failure isolation |
| --- | --- | --- |
| MD-T1 Orders/contextual registration | Existing Orders node, additive class membership, exact current item/command context | Missing entry reports once; no duplicate UI or broad list replacement |
| MD-T2 actual melee start/release/completion, if patched | Correct pawn/current job, authorized target, usable melee/cooldown and cancellable pending action; actual completion rather than attempted cast | Suspend affected attacks; no timer/delete fallback |
| MD-T3 quantity/holder/merge lifecycle | Exact successful split/merge/placement/held/equip/install/map transition, preserved HP/metadata and no unmarked promotion | No attack without safe attribution and isolation |
| MD-T4 saved job suspension/processing ownership | Load/resume occurs after admission and one processing choice, before any attack effect | Preserve HP/order with visible suspension |
| MD-T5 optional external order/attack/item adapter | Canonical package plus actual member/Def shapes, effect and cancellation semantics | Only affected route inactive; unknown state is not permission |

These are candidates, not completed target specs. Prefer direct owned jobs/public APIs; never patch
our own S022/S096 policies. Each selected actual Harmony/XML patch needs an individual stable target
document and executable startup check. Existence-only suffices only for a genuine observer. Consumed
argument/control-flow semantics require exact minimal snippets, cardinality and data flow in effective
code, accounting for skip-original prefixes and incoming transpiler shape on every run. Tolerate
unrelated edits and compatible owners, not whole-method hashes or pristine code. Return incoming IL
unchanged on failure; never speculate by executing foreign transpilers repeatedly. Later repatching
must revalidate or suspend safely. Failed atomic-group installation removes only newly owned patches.
Warn once per skipped enabled patch with package/patch/spec/target, reason, unavailable behavior and
fallback. Attack authorization/lifecycle checks are prerequisites; a partial UI is not safe activation.

Original Garwel.DestroyItem active disables overlapping owned new/resumed attacks and suspends saved
orders; removing it requires explicit Resume after safe activation. Do not alias its package/Def/save
identities. Mlie.RecycleThis bench disposal is not the same mechanism: no blanket disabling solely
for that presence, but mutually conflicting external orders on the same target require a narrowly
verified ownership adapter or visible blocked state. VEF's similarly named workgiver is unrelated
by inspected binding; do not disable based on that class-name substring. No external mod is removed,
and no product references the Gateway or another repository product.

## Risks / Trade-offs

- Native damage may spill effects onto surroundings → preserve real consequences and communicate
  known hazards; intentional target restriction is not an immunity bubble.
- Shared stack HP can erase unmarked units → same-cell separation or no attack, with exact quantity
  and native metadata retention. Unsupported custom stacks remain visibly blocked.
- Combat helpers have extra pursuit/retarget behavior → own the job and test those negative cases
  through real player inputs, rather than trust an attack job label.
- Timers/booleans can misreport progress → observe actual completed attacks and HP/quantity, retain
  cooldown and bounded suspension. Do not conflate a successful route call with visible damage.

## Migration Plan

No runtime migration now. Future owned saved orders load inactive until admission and identity checks
finish. Preserve HP and explicit suspension, clear stale map/worker claims and never replay previous
hits. Foreign jobs/settings are not imported. Any future source migration or safe removal process
needs its own backup, atomic conversion/restoration and verified postconditions.

## Open Questions

S096 now supplies [item-recipe processing](../specify-megamod-item-recipe-orders/design.md), including
native bench destruction/burning where applicable. Those recipes remain distinct from this actual
melee action while sharing the existing exclusive processing choice and exact authorized portions.

MD-Q1 keeps proposed classification/worker/stack/safety choices; MD-Q2 native cast/cancel/completion;
MD-Q3 exact quantity and external processing/effect contracts; MD-Q4 tests/UI/admission/native proof.
These do not interrupt drafting. Real swings, current HP and normal destruction effects must be
personally observed on the reviewed build before gameplay acceptance.
