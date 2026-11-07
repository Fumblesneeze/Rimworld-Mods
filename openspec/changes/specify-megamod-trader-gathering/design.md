## Context

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
S039 replaces the source-derived gathering behavior; S040 owns the dedicated area. Read the
[proposal](proposal.md), [spec](specs/megamod-trader-gathering/spec.md), and
[research](../../../mods/megamod/docs/refinement/2026-11-03-trading-spot-research.md).
Source has six runtime types, one marker Def, two global booleans and **no actual Harmony patches**.
It repeatedly mutates LordJob.chillSpot and current travel/defend duties from Building.Tick.

This design does not authorize implementation before S040's area contract is available. The two
changes share one mod owner and compose directly; they are not runtime dependencies on separate mods.

## Goals / Non-Goals

**Goals:** redirect real incoming/waiting parties, preserve visit identity/state, support a current
usable area and common visitor preference, respect other job/provider ownership and safe fallback.

**Non-Goals:** source-marker recreation, teleporting arrivals, price/stock/trader generation changes,
altered visit timers, an allowed-area assignment system, guest-system takeover, source binary/API
impersonation or automatic migration. No shared Thing cache is needed to enumerate native Lords.

## Decisions

### Separate area representation from party policy

S040 provides the map-local footprint, stable identity/revision, availability status and candidate
selection under its explicit geometry/idle-target rules. S039 consumes that directly. Do not invent
a public provider interface merely to connect two owned components; one cohesive map-owned module
can hold the area and party routing records. No per-marker scan, global CurrentMap or static
cross-save destination. Ordinary lords come from the native map lord manager.

The source constructor's replacement/destruction behavior and WorkToBuild mutation disappear with
the physical marker. No TradingSpot ThingDef alias or stand-in building is needed for old texture
patches. New declarative controls/Defs in S040 must follow its own XML/package rules.

### Model incoming, waiting and relinquished control explicitly

Inspect the actual native graph/lifecycle before choosing a seam. TradeWithColony and VisitColony
contain both normal gathering and emergency/exit phases. LordToil_DefendTraderCaravan derives from
DefendPoint and must retain its role-specific UpdateAllDuties behavior. Visitor exit contains Travel,
so testing only `curToil is LordToil_Travel` is insufficient. Likewise a DefendPoint can be active
during defense; changing its focus can pull guards away from protecting the trader.

Use admitted party/stage identity, not loose field names or the existence of a trade inventory.
Capture the native incoming destination before the first owned redirect. Track current owned target,
area revision and whether the party is still in an eligible ordinary stage. Restore a still-usable
native destination or request ordinary selection when releasing control; do not freeze a deleted
area's location in chillSpot. If native selection cannot provide a destination, yield to its actual
failure/exit behavior. Do not repeatedly restore a stale baseline over a provider's newer decision.

Set current/native future gathering state coherently without reconstructing the Lord graph, rerolling
timers, clearing jobs or re-spawning members. A transactional owned update prepares all required
destination/duty changes, then commits at the ordinary decision boundary; on failure restore only
owned changes while the phase still permits it. Never roll a party back from a newly entered exit
or emergency stage. Revalidate phase, map, target and permissions immediately before committing.

Preference/area edits become pending routing changes for an existing party. Legitimate current
needs, trade and S027 delivery ownership are respected. Do not implement automatic redirection
as player-forced jobs or indiscriminate EndAllJobs. Unchanged target/duty decisions are no-ops.

### Native roles and permission checks remain authoritative

Choose a candidate that satisfies the party's actual movement/access needs, including relevant
carriers and escorted members. S040's detailed algorithm must not check only the trader and strand
an animal behind an inaccessible route. Preserve native ability to open doors where genuinely
granted by the Lord; do not invent additional access and do not revoke existing native permissions.
The area selects ordinary destinations, not every route cell or follower location.

Ordinary emergency reactions remain native, including temperature, anomalous weather, game
conditions, hostility, losses, dismissal and rescue. Existing native duration/arrival-radius/escort
values in research are reference facts, not values to duplicate in a new engine. No new visit-time
slider, artificial grace period, reset threshold or resource bonus is introduced.

### Common settings and persistence

Register Include ordinary visitors, default true, in the common Trading settings section with
global/default and save override controls. Save inheritance and dormant values follow the foundation;
main-menu rendering and mutation callbacks both reject save edits. Only this preference is adopted.
The source's marker-construction option is deliberately omitted, not migrated into area state.

Area cells are saved under S040. Native Lords retain their own lifecycle, serialized chillSpot and
party data. If restoring original gathering requires an owned baseline across save/load, store the
minimal map/party identity and original destination, not live references or a rebuilt graph. Resolve
against the loaded native party; missing/destroyed identities cannot affect another party. Rebuild
transient target caches and clear them on teardown. No save-file rewrite or foreign configuration import.

### Efficient updates have concrete consumers

Prefer direct area/preference/lifecycle notifications and native decision hooks over the source's
every-tick reflection loop. If a bounded fallback poll is needed for unsupported notification gaps,
name its constant and measured cost in the implementation design before adding it; it must not
alter observable visit timing or become a full-map Thing scan. Do not invent a poll interval as a
promised speedup without evidence. Cache only admitted callable metadata and per-party routing state,
with explicit provider/map/game lifetimes. All Verse/Unity access stays on the game thread.

S027 delivery state is consulted directly: it protects the finite courier job, never assigns the
trader/animals, and determines routine/urgent departure behavior. Gathering does not create or extend
a receipt, increase capacity, append extra trips or suspend native departure indefinitely. No self-patch.

### Optional providers and actual consumers

Detect `Orion.Hospitality` by canonical package ID, not the source's display-name equality test.
The inspected continuation has a distinct LordJob type derived directly from LordJob; its party
ownership is separate from native RimWorld.VisitColony. Leave those guests unchanged; this is not
an integration with its room/area/scoring controls. Retain ordinary native trade-party behavior even
when the provider exists. A later cooperative route needs exact package/member/Def contracts.

With `tradingpost.programmerlily.com` active, withhold owned overlapping redirection, preserve area
data and show its inactive status. Do not remove markers or unpatch foreign owners. If another
controller claims the same party, select only an admitted strategy or leave that overlap inactive;
its presence is not a universal conflict claim about every party or mod.

Actual legacy consumers found: Better Architect's `grizzlemethis.tradingspot.rw`-gated folder changes
TradingSpot category/work; Drawn Spots uses that older package for graphics, with separate Trading
Control branches. Their XML cannot bind meaningfully to an area without a marker. Document this
intentional break rather than spoofing a package/Def. Source public classes/settings keys are recorded
but no consumed callable API was found in the bounded scan; no speculative facade is proposed.

### Semantic target documents and admission

Before any actual patch, create its stable-ID target document and executable startup validator,
linked to the owning scenario, implementation, tests and eventual native evidence. This table is
candidate design work, not a substitute for per-patch documents:

| Boundary | Consumed semantics / failure scope |
| --- | --- |
| Incoming destination | Actual party/map identity, ordinary entry versus gathering destination, preserved stock/trader creation and native fallback; any modified argument must demonstrably flow into gathering state, not spawn location |
| Waiting/reassignment | Current ordinary stage and role-aware duty update; other stages using the same base class must be excluded; guard consumed private fields and the behavior they influence |
| Exit/emergency | Correct terminal/emergency transition recognition, including nested visitor travel-and-exit; do not rely on the mere presence of Travel or DefendPoint types |
| Area/preference refresh | Committed owned revision and actual map/party, stale/cancelled change rejection; direct owned calls require no self-patch |
| Native fallback and load | Baseline/selection validity, serialized phase/progress preserved, no replayed arrivals or gifts; restoration cannot supersede foreign changes |
| Optional controller | Canonical package plus exact type/member/Def and party ownership; absence safe, unsupported overlap isolated |

Prefer native/public extension points and narrow adapters, then Harmony only where necessary.
Static XML cannot express runtime Lord phase, current reachability or courier ownership; C# is
justified for these decisions. A pure observer may need only existence/callable shape, but a changed
destination needs its actual argument/field data flow and execution conditions. Inspect original and
effective code, skip-original prefixes and relevant transpilers. Compatible foreign owners and
unrelated body edits are permitted; exact assembly hashes/whole-body equality are not admission.

Every transpiler must validate its actual incoming instructions every run and return the same stream
unchanged on failed semantics. Do not repeatedly execute foreign transpilers as probes. Handle later
repatch invalidation/revalidation. Admission failure/exception or installation failure leaves the
declared atomic routing group inactive and rolls back only newly installed owned hooks. One bounded
startup warning per skipped enabled patch names package/patch/spec ID, target, failed condition,
unavailable behavior and fallback. No per-tick warnings. Original-provider suppression is explicit;
an absent optional provider is not a failure. Direct reflective adapters need equivalent shape and
failure isolation even if no Harmony patch is chosen.

No XML patch is currently needed. Any later external XML adaptation must use XML Extensions
package-ID/content/shape guards, preserve unrelated entries and declare its dependency in the same
runtime slice. Do not copy the source's unconditional shared-Def stat mutation.

## Risks / Trade-offs

- **Source type checks include departure/defense states** → explicit phase admission and native
  transition scenarios; no unconditional recurring point write.
- **Area unavailable for one party** → per-party candidate validity/fallback, never teleportation,
  fabricated access or repeated unreachable jobs. S040 owns geometry and candidate selection.
- **Removed area leaves mutated native destination** → capture/restore only owned preference state,
  save minimal baseline if needed, and respect newer native/provider choices.
- **Reduced polling misses changes** → use real invalidation/decision boundaries and measure both
  reaction and cost before any optimization claim; source's reset bug is not copied.
- **Provider class or engine graph changes** → fail the affected integration closed, preserve native
  routing, and record unsupported ownership instead of unsafe reflection.

## Migration Plan

No automatic import of source keys or conversion/deletion of its building. Its marker type, Def name
and source settings remain unsupported saved-source contracts in this area-based successor. Players
can draw the owned area through S040; a future opt-in converter requires a separate preview, backup,
atomicity, failure restoration and verified postconditions contract. Ordinary owned saves preserve
only their own preference/area/routing state. Feature absence must not modify another mod's state.

## Verification and Open Questions

TS-Q1–4 cover exact effective state/lifecycle seams, S040 geometry, providers/consumers, source reuse
rights, native acceptance and measured update costs. Pure party-policy decisions fit Unit; owned
patch shapes fit Harmony with owner cleanup; lightweight Def fixtures fit Defs; actual Lords, active
providers and loaded-world behavior require fresh isolated RimWorld processes.

After focused RED/GREEN and scoped implementation review, personally inspect the reviewed-build
native workflow: draw/select area through S040, let an actual incident create a caravan, observe
physical arrival/waiting, trade, edit/remove/block the area, observe delivery and departure, save/load
and repeat a focused second-map/provider case. Directly editing chillSpot is not player acceptance.
Keep exact mod/build identity, causal actions/screenshots, supporting logs and normal-config/PID/
cleanup evidence. Seven-language UI and native graphical review apply to S040's new controls.
No test, startup marker or patch ledger substitutes for seeing the party behave correctly.
