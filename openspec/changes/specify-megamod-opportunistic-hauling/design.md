# Useful hauling detours — design

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, target1.7 beta.
S125; design only. [Observable contract](specs/megamod-opportunistic-hauling/spec.md).

## Context

Read the [source audit](../../../mods/megamod/docs/refinement/2026-11-06-while-youre-up-research.md)
and the earlier narrow S021 intersection. Installed modern1.7 DLL is the behavior reference;
original public source is pinned separately. Decompilation proves neither effective patched IL
nor native acceptance. S021 already owns exact cargo, capacity, finite pickup radius, grouped
delivery and recovery. S024 already owns ingredient collection and a pre-bill storage detour.

## Goals / Non-Goals

Goals: useful general and supply detours with a finite parent lifetime, deliberate source-default
choices, no duplicate bill producer, current native permissions, shared capacity/data and truthful
fallback. Non-goals: capacity inflation; wholesale source port; a fake legacy PUAH API; migration;
new work abilities; shortest-path guarantees; new bottom tabs; transporter/caravan loading parity.

## Decisions

### One opportunity decision attached to the pending work

Use one narrow owned request carrying pawn, map, parent-attempt identity, purpose, selected resource
portions, destination permissions, initial route/budget and consumed-detour state. S024 bill
preparation calls that policy directly; it cannot schedule a second job around it. Do not patch
owned methods to reproduce interactions between formerly separate mods.

Preserve native prefix eligibility before selecting general work: player humanlike, valid target,
distance>=3, ordinary hauling capability/age, no player-forced job, no required finalizer. Inspect
`CanPawnTakeOpportunisticJob` and job queues in the implementation slice; all explicit/queued orders,
emergencies and actual loading/caravan jobs must remain protected. Apply the bleeding guard to
both purposes, unlike source's bill-before-bleed ordering and unguarded construction branch.
Persist only the minimal attempt/cargo correlation needed for reload; no serialized paths or live
Thing references. Attempt identity must survive native job recreation without treating a finished
bill's later iteration as the old attempt. An accepted detour consumes the allowance even after
interruption. An unchanged negative search is memoized for the attempt and invalidated only by a
meaningful relevant change. This replaces the source's frame-based same-pawn marker/five-frame delay.

### Candidate facts shared; decisions remain current and local

Use the S033/S021 shared map-owned fact index, native listers as inputs/fallback, and exact quantity
ownership from the common cargo authority. Key by physical item/portion and holder, not ThingDef.
Route/reservation/eligibility checks are per pawn at use time. No static global planning call stack,
separate source-port item cache, or background Verse/Unity access. Source candidate order is native
lister order; owned ties must be stable. A finite snapshot and finite preference expansion prevent
zero-limit ×2 loops. Zero hard distances are exact restrictions; zero soft preferences skip to the
remaining finite candidates instead of multiplying forever. Bound per-attempt work and benchmark
combined Ctrl+F/hauling updates before claiming an optimization.

### Routing and explicit equal-priority permission

General source distances are 30/.5 and50/.6, whole-trip1.7 and added-legs1.0. The source modes are
Vanilla/Default/Pathfinding; present Nearby/Balanced/Thorough with honest search trade-offs.
Nearby retains its two25-region budgets as advanced **nearby-area search budgets**; design uses
actual native region traversal, not a cell-radius approximation. Bound editable counts1–1000.
Balanced/Thorough use the source soft preferences, but expansion cannot be unbounded. Both actual
path cost and geometric screening must pass; missing paths must be distinguished from valid
zero-cost same-cell legs. Zero original-route cost has no budget. Do not reproduce `TotalCost==0`
as a universal failure check or compare unrelated map coordinates.

Compute the route S021 will actually deliver: nearest currently valid physical destination,
complete its valid portions, then next. The source budget simulation starts with its first planned
destination while unloading may choose the nearest; ours evaluates the actual chosen sequence.
For a complete plan, total includes pickup legs, last pickup→first delivery, further deliveries,
return. Added legs omit only last pickup→first delivery. For supply work the baseline is
pawn→selected resource→work. Retain spent cost when replanning; do not mint a fresh budget after
each pickup or failed cell. Source same-SlotGroup replacement bypasses its budget; ours does not.

Prefer highest-priority destinations that satisfy permission, space and route bounds, then near
the item/work midpoint with stable ties. S049 automatic refill and incoming commitment quantities
remain authoritative. Equal-priority is an explicit authorization attached only to this supply
request; never alter `StorageSettings.Priority` or append already-stored goods to native global
haulables. Same physical store is excluded; linked groups are settings relationships, not location.
Equal-priority extra resources must pass the actual pending recipe/construction choices. Do not
infer eligibility solely from a Def appearing in a recipe or a current job of a coincidental type.

### Consolidate S024 supply storage geometry deliberately

The combined bill preference is one setting. S125 replaces S024's source-specific room gate and
worker-to-store comparison with actual resource-to-work improvement, surplus, whole-route bounds
and one attempt. A source Common Sense-only detour is not left behind as a second fallback producer.
This is an intentional observable revision to the active S024 draft. Keep its ordinary ingredient
collection, perishable/material selection, native queues and S014 committed iterations untouched.
Supply detours must carry a useful surplus beyond the immediate selected portion, with real mass,
volume and space. Failure falls back to ordinary ingredient work; it does not change consumption.

### Storage-type preferences use the common settings machinery

Represent each destination collection by stable package/Def identity, automatic/manual state and
dormant explicit selections. No copied ThingFilter reverse-patch framework, source-only synthetic
Def database or Scribe global-mode swapping. Lists classify storage buildings, not stored item
types. Native filters still decide contents. First manual edit seeds current automatic choices;
later switches restore the dormant list. New manual entries start off; missing entries stay saved
but unavailable. The whole collection has one global/save inheritance selector, not hidden
per-item cross-scope merges. Reset only the chosen scope; it cannot clear another save/global list.

Source general defaults allow all Building_Storage except Deep Storage; supply defaults hardcode
24 package families. Owned automatic lists use admitted physical-storage semantics: ordinary Core
storage, supported optional handling/capacity, with slow optional destinations excluded for general
opportunities by default. Supply lists can include admitted slow stores. This deliberately avoids
display-name grouping as identity and stale package allowlists as proof. Manual opt-in cannot
authorize unknown handling. Adapter support needs its own measured native workflow, not an enabled
checkbox. Non-slot containers are not silently claimed supported because native1.7 can use some;
keep their ordinary route unless a narrow adapter preserves this contract.

### UI and lifecycle

Configuration uses the common renderer/persistence path and seven baseline languages. Defaults
and limits are specified, including source0–999 distance and0–1000% ratio entry ranges; source
unbounded integer entry receives a proposed1–1000 range. No capacity setting. The initial enable
**default** is a documented global-only scope exception through the common renderer: it seeds
current-play state for a new game or an existing save with no saved value. A per-save seed would
have no useful effect after initialization; the persisted bottom-right toggle owns that choice.
Subsequent initial-default changes do not overwrite it. All remaining configuration uses global/save
controls, with save callbacks disabled at main menu and dormant overrides.
Expand/collapse/search are window state; visualization is developer session state. Diagnostic
colors retain source meaning with textual legend and rejection reasons. No persistent syncing into
native debug preferences from a regular settings dialog, and no source support/Discord warning.

S021 owns retained cargo and600-tick unchanged-failure retries. Disable/repatch/map removal closes
new collection but never erases physical portions. Delivery or safe recovery is a separate admitted
path. Deleted parent, new forced work, hauling quota changes or a draft transition cannot recreate
old work. Save/load clears derived indexes/routes, restores minimal identities and revalidates.

### External contracts and semantic admission

Do not expose `WhileYoureUp.Mod`, `JobsOfOpportunity.ModCompat.Settings`, legacy `Utils.MyDistance`,
`CodeOptimist.*` helper clones, source config keys or Harmony owner as claimed binary compatibility.
Concrete consumers are retained in research. Smart Medicine's historical Utils hook is not a
consumer of this port's actual Mod class; Vehicle Map Framework's inspected adapter gates older
packages and changes map selection, not proof of safe multi-map routing. A later facade needs an
actual supported consumer and absent-safe package/member checks. Keep own package identity.

Before any patch, create stable target documents and executable checks for these prospective groups:

| Group | Minimal semantic contract to prove | Failure / alternative |
| --- | --- | --- |
| General admission | Native job eligibility/finalizer/forced/target checks precede owned replacement; actual parent continuation remains reachable | No owned general detour; native scheduling |
| Construction supply | Actual chosen resource, current need and pending constructible are available after valid selection; forced/cleanup paths retained; returned detour doesn't corrupt ordinary job construction | Ordinary construction supplies |
| Bill composition | Actual selected ingredients/counts/committed iteration and ordinary scheduling permissions; no duplicate S024 producer | Ordinary bill ingredient collection |
| Cargo handoff | S021 actual accepted quantities, route plan, capacity/commitments and recovery are available together | Safe single-load only if complete contract can be retained |
| Lifecycle/reports | Correct parent/cargo identity on finish/cancel/load/clear; report hooks require only used callable shape if pure observers | Disable affected tracking-dependent collection; recover cargo |
| UI/debug | Current save/main-menu context and actual debug-mode path; no mutating callback while inactive | Explain unavailable control; no hidden mutation |
| Optional storage/route | Canonical active package, individual content and current capacity/handling/map/route semantics | Withhold only affected candidates/routes |

Use effective code and incoming transpiler streams; every transpiler returns the original incoming
stream unchanged on failure, every dependent group installs atomically, rollback touches only newly
owned patches. Revalidate admitted later repatches without speculatively executing foreign
transpilers. False/unknown/exception/install failure produces one bounded startup warning per
skipped enabled patch with package/patch/spec/target/reason/behavior/fallback. No exact whole-method
hash or blanket foreign-owner exclusion. Original providers take their overlapping scope; detect
the selected and original/older known IDs case-insensitively, with shape inspection for actual
interaction. External Common Sense bill detours win until supported cooperation; never edit its
`hauling_over_bills`. Optional PUAH private bindings are not reproduced over exact mixed quantities.

Runtime job decisions need C#/Harmony; XML cannot express them. Harmony is required when introduced.
No XML changes currently planned; any future declarative integration uses XML Extensions guards,
additive merge and required metadata in that slice. No gateway dependency enters the product.

## Risks / Trade-offs

- Restricting all bleeding supply detours and requiring useful surplus can select fewer trips than
  source → intentional care/work protection; measure real production before tuning.
- Thorough route searches can be costly → finite sets, shared facts, scoped invalidation and
  combined-consumer benchmark, no published performance guarantee.
-1.7 beta job/control flow and external containers can drift → semantic admission and focused
  provider-present native workflows; static hashes identify evidence only.
- Attempt identity through native job recreation is delicate → host model plus actual bill,
  construction, interruption/reload and forced-order workflows; no five-rendered-frame workaround.
- Source-style geographic defaults do not measure net time saved → preserve required handling,
  compare native/S021/source behavior and retain WY-Q4 playability review before acceptance.

## Migration Plan

No automatic source config/save migration. Add common owned defaults only when implemented. Removing
or disabling new collection must retain a supported recovery path for existing owned cargo. Any
later importer/removal guarantee needs a separate backup/atomicity/restore/postcondition contract.

## Open Questions

WY-Q1–5 in the sequential ledger retain beta integration, actual optional adapters, finite-search
performance, proposed travel/surplus balance and complete native/UI acceptance. No question is
permission to skip a stated safeguard; implementation must resolve affected admission before use.
