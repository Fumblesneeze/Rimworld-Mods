## Context

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin **original**, S118. The user requests Cooking-dependent slaughter effectiveness and output,
neutral Cooking10, unchanged base yields and a slaughter-job-only origin. Earlier doubled-base/
skill16 suggestions are superseded. [Refinement](../../../mods/megamod/docs/refinement/2026-11-06-slaughter-yield-refinement.md)
separates that intent from proposed product/scope/formula defaults and static native evidence.

S117 already owns coordinated slaughter timing and a fresh handling snapshot. Native slaughter
creates a corpse, not meat. Later `GenRecipe.MakeRecipeProducts` sends recipe efficiency through
`Corpse.ButcherProducts` to `Pawn.ButcherProducts`; meat/leather each use their current native stat
times efficiency and `RoundRandom`. Generic extra products and discrete body-part yields are other
branches. `Toils_Recipe` materializes products, consumes ingredients, notifies the bill, then stores
products. Native butchery Cooking efficiency is `0.75 + 0.025*level` before capacity/other modifiers.
Use current native1.7.5000rev1275 evidence, not assumptions about older code.

## Goals / Non-Goals

**Goals:** one direct Cooking/timing composition; a real slaughter-earned corpse result; bulk
meat/leather adjustment before native rounding; bounded common tuning; stable earned state; safe
generic native/modded-animal support and explicit unsupported-provider fallback.

**Non-Goals:** global Def-yield increases, death-time resource spawning, bonus hunting/mental kills,
extra organs/horns, bypassed carcass/recipe restrictions, whole-recipe efficiency replacement,
invented legacy history, performance/compatibility acceptance, user interviews or implementation.

## Decisions

### One policy and one handling snapshot

Use pure functions for Cooking contribution and recovery, driven by the six common definitions in
the spec. Cooking defaults borrow the native butchery efficiency anchor:75/100/125% at0/10/20.
Normalize S117's Animals/Melee skill factors against the configurable neutral level. Half-strength
yield influence is a proposed moderation because native butcher efficiency also remains: default
all20 slaughter plus an ordinary healthy Cooking20 butcher is about2.00676 times the corresponding
native quantity before final rounding, while all10 is neutral. The user accepted the approximately60% mastered-slaughter bonus; no base stat doubles.
Keep the selected curve and distinguish that accepted magnitude from the still-provisional50/200%
tuning bounds and unverified combined slaughter/butchery balance.

Only S118's skill curve clamps inputs to0–20. S117's source-derived timing still uses ordinary
native effective factors, including admitted modifiers; this addition's Cooking curve stays bounded.
Do not derive recovery from the complete `SlaughteringSpeed` value: work-speed drugs, Manipulation,
Sight and a creature's size would then become unintended extra yield multipliers. Ordinary native
butchering can still depend on the butcher's capacities. No skills means neutral Cooking/recovery;
missing required definitions fail admission, not fabricated level-zero records.

Compose a settings-aware Cooking contribution into the owned S117 stat calculation before final
minimum processing, directly in the owned definition/worker. Do not Harmony-patch our own helper,
append Cooking twice, or edit the source-selected foreign definition. Reuse S117's entry-time job
snapshot and save association for pending recovery; it holds captured Cooking factor, recovery
factor, skill/tuning provenance and attempt identity. No second global job dictionary. Mid-attempt
settings/skill changes affect future attempts only. If S118 invalidates, future attempts use neutral
Cooking; S117 retains already-captured timing if its own contract remains valid, while an unfinished
uncommitted S118 recovery becomes ineligible.

### Commit an earned result to the exact corpse

Qualifying origin is ordinary designated/auto-selected animal Slaughter, outside a mental state.
Use native intent and current actor/victim/job identity, not an injury or generic execution-method
call. Optional job providers need a narrow proof of equivalent deliberate intent. Preserve native
faction/violence/ideology/reservation gates; no extra player-faction shortcut or newly allowed job.

The owned S117 completion seam calls this policy directly around the preserved native final cut.
Require the intended live victim before that cut and its actual matching newly produced corpse
afterwards. Record once only when the cut caused death; another cause, prior death, failure before
the cut, no corpse or ambiguous outcome grants no record. Never retry a cut or undo death because
recording failed. Preserve all native records, history, blood, sound and mental-state consequences.
Runtime failure leaves an ordinary corpse with a bounded diagnostic, not a manufactured reward.

Attach the durable record to the **Corpse instance**, not the living/inner Pawn, animal Def, map or
slaughterer. Store finite captured factor, schema version and minimal historical skill/tuning/name
explanation. Avoid a strong reference that keeps a departed worker alive just to show its name.
Read missing data as neutral; never infer a factor from ExecutionCut or recalculate old preparation.

A small weak corpse-associated extension record serialized through admitted `Corpse.ExposeData`
is a candidate. This is entity state, not an inventory cache. Native corpse definitions are implied
at startup; do not add runtime Def mutations merely to attach a component when a narrow serialized
extension suffices. Evaluate the concrete persistence seam before code, keep Scribe keys owned,
and clear stale associations on teardown. A component alternative needs actual generated-Def and
save contracts plus its own guards, not a global race rewrite or speculative entity framework.

Native resurrection detaches the inner pawn and destroys its corpse, so corpse ownership naturally
prevents carrying the entitlement into another death. Container/map changes preserve the same
record. Consumption/destruction retires it; no copied/stacked-corpse support is assumed without an
adapter proving per-body identity. Validate stored factors against their supported schema/absolute
allowed range, **not the current user's tuning bounds**: reducing today's max cannot rewrite a
previously earned result. Keep valid unsupported records dormant while their corpse exists.

### Transform the two native bulk quantities without replacing production

Prefer a minimal guarded transformation of the actual meat and leather float amounts in
`Pawn.ButcherProducts` immediately before their existing `GenMath.RoundRandom` calls. Match the
native stat/product/control-flow relationships separately, not just two multiplication opcodes or
literal indices. Require the record's corpse to own this exact dead pawn. No global MeatAmount/
LeatherAmount patch: their base values and unrelated consumers, including ingestion, stay native.

Do not change the method's whole efficiency argument: it also feeds base generic butcher products,
which would multiply unrelated items. Do not multiply already-rounded stacks or append bonus
products afterwards. Preserve native iteration, ordinary modifiers, material/product identities,
RNG call count/order, body-part removal, ingredient consumption and product postprocessing. Factor1
must not consume extra randomness or alter normal output. Invalid quantity arithmetic fails to
the unchanged native value with a bounded diagnostic, never overflow/negative owned stacks.

The record is read-only during quantity calculation; do not consume it from a getter, product
enumeration or preview. Ordinary actual corpse consumption ends its availability. Use the native
completion once, no separate bonus transaction/retry or parallel ingredient ledger. Test real
one-corpse completion, admitted mixed-input batches, interrupted work and failure boundaries. This
does not claim to repair a foreign processor that independently duplicates native products; such
unknown contracts must not be advertised as supported or given an unguarded adapter.

For owned previews, use pure amount queries and the same scalar policy. Never enumerate
ButcherProducts to discover outputs: it creates Things and can remove trophy body parts. S010's
input-dependent preview contract stays in force; no fixed-count promise without known corpse and
butcher facts. Native target-count bills count actual meat resources, not an estimated corpse bonus.
S014/S017/S021/S033/S049 receive normal physical production/Thing events, never a speculative count
or a second feature-owned Thing/corpse index. Existing native/shared data is enough for real consumers.

### Common settings and contextual UI

Register six stable definitions in the common loader/store/resolver/renderer, ordered as in SY07.
Ranges ensure positive Cooking factors, valid interpolation intervals and bounds containing1.
Each row uses Global default, Current save and Use global; no scope exemptions. Invalid interactive
edits retain the previous valid value. Missing save entries inherit; malformed persisted values
follow the common validated fallback with bounded diagnostics. Snapshot one coherent effective
configuration per new handling attempt. World-view settings bind to the active save; main-menu
callbacks cannot mutate any save. Checking inheritance retains dormant overrides.

Use S117 native speed explanation plus a read-only recovery entry in `Pawn.SpecialDisplayStats`.
Use an admitted corpse inspect/stat append for captured factor and scope; retain incoming native/
foreign text. Clearly distinguish predicted current proficiency, earned recovery and final output
after carcass/butcher modifiers. Do not edit the native base stat to make UI match a bonus estimate.
No new bottom tab, cheat action or operational toggle. All seven languages include scope labels,
factor descriptions, numeric validation and unsupported/dormant explanations.

### Coherent admission and dependency policy

Create actual stable-ID target documents before patches exist: native final-cut provenance seam,
corpse serialization/lifecycle, both bulk amount branches, and contextual presentation. S118
requires admitted owned S117, valid common tuning and usable required native content. Original
Skilled Slaughtering selected by S117 means no owned Cooking contribution or new S118 record;
do not quietly patch that original as a second implementation.

Cooking/new-record/apply support must be coherent: failed required production/persistence admission
disables S118 while S117 can still provide its base timing. Keep safe record reading/preservation
where independently admitted so valid existing records can stay dormant. If even their schema/
serialization is unknown, do not interpret or silently rewrite it as a new earned value; record
the migration limitation and use native output. No mid-death rollback or compensation promises.

Validators check the minimal effective relationships actually used. A display-only append may need
only a callable target/result shape; transforming quantities or wrapping death requires the actual
relevant flow and operands. Every transpiler checks its incoming stream each time and returns it
unchanged on failure, including unexpected duplicate owned transforms. Do not execute foreign
transpilers as probes or require exact hashes/versions/no other owners. Permit compatible changes;
unknown semantic replacements/skip-original behavior disable the affected group.

False/unknown predicates, validator exceptions or installation failure roll back only newly added
owned patches and leave native/provider behavior. Emit one bounded startup warning per skipped
enabled patch with package/patch/spec/target, failed condition, unavailable behavior and fallback.
Idempotent startup and supported later revalidation must not repeat kills or product generation.
Optional providers are package/Def/type-shape resolved. No upstream source/assembly facade is needed
for this original feature. Harmony and S117's XML Extensions use remain declared dependencies.
Amend the owned guarded definition directly for Cooking; no destructive list replacement, self-patch
or hard runtime dependency on another repository product/Gateway.

## Risks / Trade-offs

- Two skilled production stages compound → half-strength proposed recovery influence, explicit
  combined comparator table and tunable bounds; final playability remains unverified.
- Low proficiency deliberately loses yield → explain factor before work and retain ordinary
  butcher effects; SY-Q1 holds the proposed balance/product/scope choices for later review.
- Raw native base information differs from final output → show separate earned factor and honest
  input-dependent estimates; do not relabel a guess as a fixed recipe count.
- A corpse persists beyond worker/settings lifetime → serialize captured factor/provenance on that
  corpse; never recompute from a live worker or current cap.
- Native iterators can have side effects → pure preview calculation, no speculative enumeration,
  repeated transpiler probing or product replay.
- Nonstandard processors/corpse stacks may bypass the proven path → bounded adapter/fallback,
  measured actual consumer cases and no blanket compatibility claim.

## Migration Plan

No retroactive import or old-corpse inference. Missing records use neutral native quantities. Add
owned versioned state only when implementation begins. Preserve supported records on normal load/
transfer, and document any later schema change with explicit migration, backup, atomicity, failure
restoration and verified postconditions before conversion. No normal save/config writes here.
Removing the product or consuming a corpse while support is inactive does not promise later recovery
of an unrecorded/lost bonus. No save-removal claim is accepted without a specified tested workflow.

## Open Questions

SY-Q1–5 in the [ledger](../../../mods/megamod/docs/refinement/specification-progress.md) retain
proposed tuning/scope review, effective/native persistence and optional pipeline admission,
finalized balance/shared cost, and actual implementation/localization/reviewed native acceptance.
Record these for later; do not interrupt this specification pass with questions.
