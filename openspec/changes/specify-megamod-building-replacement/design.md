## Context

Sole owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S028 replaces independently specified Replace Stuff behavior. This document is implementation design,
not a claim that target validation, code, tests or live verification exist. Read the
[source/native/consumer research](../../../mods/megamod/docs/refinement/2026-11-02-replace-stuff-research.md)
and [117-list catalog](../../../mods/megamod/docs/refinement/2026-11-02-replace-stuff-catalog.md).
The installed 1.7 DLL differs from the public committed DLL; provenance is pinned in research.

## Goals / Non-Goals

**Goals:** keep physical construction and native economy; distinguish material change, upgrade,
installation and automatic rebuild; transfer explicit entity state without erasing unsupported
choices; compose owned features directly; preserve consumed external contracts where practical;
use minimal effective-behavior admission and absent-safe, merge-preserving XML.

**Non-goals:** implementation in this change; wholesale source port; a second construction engine;
general arbitrary component serialization; original package impersonation; speculative universal
callback registry; automatic legacy-save migration; reproducing disabled historical compatibility
groups; optimizing all construction or adopting other queued source mods early.

## Decisions

### 1. Model each physical order explicitly

Use a small owned order record associated with the native blueprint/frame and exact old Thing.
Material replacement, different-type upgrade, installation and automatic rebuild have different
input/state rules; do not infer them from a shared cell or a global boolean. Record stable order,
map/old/new identities, requested definition/material/rotation, phase/work, actual supplied contents,
required handoff state and completion/recovery status. Save stable identities and supported value
state through native serialization, not closures or an entire destroyed Thing graph.

Material replacement retains the original Thing and changes Stuff only after validated completion.
Upgrade completes the normal new Thing and transfers explicit compatible state. Installation uses
the actual existing minified object and its native work/input path, retaining its actual HP/quality
and existing choices. Conflicting old-target/incoming configuration or group membership refuses
installation before removal; no default-value heuristic, concatenation or silent detachment resolves
it. Use ordinary explicit configuration/linking workflows first. Ordinary automatic rebuilding
attaches a restoration record only to the exact native destruction-created order. Source alternatives
rejected: first-overlapping-Thing association, per-cell deep old-building cache, one static spawn
flag, weak-reference caches without teardown, and unsaved next-tick state transfer.

Keep one order per old target. Multi-cell drags deduplicate Thing identity. Larger new footprints
can contain one eligible old building plus legitimately coexisting objects; a second replaceable
old building makes the order ambiguous and unavailable. Revalidate old identity, complete footprint,
provider restrictions, occupants and actual inputs immediately before work/commit. Same-cell later
construction cannot inherit a canceled order. Cross-map transfer/minify/gravship must explicitly
retain or cancel/recover an order; until that transition is implemented, disallow the unsafe operation
with an explanation rather than carrying stale map references.

### 2. Make completion and refunds one recoverable operation

Retain native jobs, reservations, material hauling, research/skill checks and ordinary construction
failure semantics. Own only the replacement-specific input/work calculation and handoff. Native
1.7 Frame completion consumes inputs before spawn, so a wrapper that merely runs a postfix is not
sufficient to promise atomic recovery. Preflight the supported old/new state and placement, snapshot
only the necessary reversible state, and choose a guarded completion boundary before irreversible
input destruction. Stage exact returns so a retry/load cannot issue them twice. If native/provider
callbacks cannot support the declared recovery, withhold that upgrade path before accepting orders.

Durable phases distinguish uncommitted work, committed completion, pending physical output placement
and retired order. A completed replacement awaiting output placement must not reroll quality or
reapply returns. Actual input/output containers remain authoritative. Failure rollback preserves
old building and supplied contents; successful completion preserves new building and remaining
physical returns. Never reconstruct spent resources from a nominal cost list. Source god-mode
behavior is a separately admitted native developer path, not a regular settings bypass.

Use actual old-instance deconstruction work clamped by native 20/3000 rules; target-material
construction work and Stuff volume determine replacement work/input. This corrects source's missing
minimum and static abstract-stat assumptions. Fixed ingredient costs stay untouched on material
change. Upgrade gets the full native cost and normal frame-health/quality rules. Native deconstruction
fraction/failure rounding is authoritative, not the old Steam percentage or a hard-coded half return.
Cost-changing providers require effective-cost validation and matching preview/consumption/returns.

### 3. Explicit state handoffs compose the owned features

| Workflow | Transfer boundary |
| --- | --- |
| Same-object material change | Keep current configuration/contents, update material-dependent stat/appearance facts; notify admitted providers such as curing through a narrow adapter |
| Worktable upgrade/rebuild | S014 whole list and S017/S018/S019/S026 bill state, order/counters/worker/material rules; preserve committed physical production and inapplicable entries according to their contracts |
| Storage upgrade/rebuild | S005 explicit membership handoff plus native filters/priority and later S049 entity limits; new physical capacity/provider acceptance remains authoritative |
| Quality construction | S013 existing budget survives material change/upgrade/retry/reload; ordinary native rebuilding after unrelated destruction starts a fresh finite run with inherited enablement/target |
| Bed upgrade | Medical/prisoner mode and every eligible owner; preflight slot/ownership compatibility, wait for occupants to leave normally; otherwise reject before removal |
| Cooler/heater/refrigerator | Corresponding supported temperature choice; refrigerator requires exact package/type/field adapter, not any global type with that name |
| Growing building | Crop identity remains as a choice; if unavailable under new restrictions, expose inactive choice without sowing invalid crop |
| Physical contents | Keep actual items if accepted; otherwise normal safe physical relocation before old removal; no synthetic storage capacity or reconstructed destroyed stock |
| Arbitrary provider state | No generic copying; exact provider contract or decline unsafe upgrade |

An active bill settles or suspends through owned production semantics before construction; do not
erase committed material collection or credit progress merely because a workbench frame appeared.
New bill work is withheld once construction begins. Structural recipe/provider support lost after
an upgrade blocks new work on that row throughout the group under S014; distinguish that from
temporary construction downtime and already committed work. Material completion makes exactly one
quality result, with no extra S013 roll or budget reset; subsequent quality retries remain ordinary
paid cycles. S013's draft wording is clarified to make that composition explicit.
Native turret attack/bed rest limits are narrow;
do not globally mark every building unusable. Native 1.7 storage-group transfer must be reconciled
once with S005; copying after group membership can otherwise overwrite the shared group. Use the
explicit order handoff already required by S005/S014, never location matching.

S021/S024/S027 keep their distinct hauling/job intent, use actual destination availability, and share
facts/reservations. An unavailable replacing workbench is not a housekeeping target that restarts
bill work. Later S060/S061 construction sources must compose with these explicit rules when their
own research turn arrives; no speculative source patch is retained against owned code.

### 4. Keep planning native and bounded

Rock planning adds normal designations and respects mining capability. Recheck smoothing versus
mining using actual intended finished geometry. Native listers provide local obstacles; avoid a new
whole-map cache. Fog admission can defer only checks dependent on hidden state. Reveal revalidation
uses the actual map and every relevant plan's occupied rectangle; do not use `Find.CurrentMap` or
select only the first blueprint. Show plans above fog without revealing terrain/hidden wall links.
Fog precedence also covers support discovery: never expose hidden terrain through a bridge plan,
rejection or cost difference. Unknown support is displayed as unknown; mixed-footprint orders wait
for relevant reveal before new support/physical construction and the full cost decision. On reveal,
validate and commit the known prerequisite layout or recover/cancel the invalid order.

Corner-touch patches must preserve native reachability and reservations. A composed drop validator
captures the previous validator, not itself; bounded legal placement remains pending if no safe cell
exists. Ignore a temporary pawn obstruction only when completion remains physically safe. Do not
copy global deconstruct-workgiver suppression, arbitrary frame nonblocking or attachment-on-frame
permission. Atomic ownership groups couple placement relaxation with the prerequisite/job checks
that make it safe.

Bridge discovery starts from a finalized-Def catalog, not a per-feature map Thing cache. Supported
providers can generate terrain later: NANAME reads membership and appends blended terrain when its
base is bridgelike. An admitted adapter must register that concrete derived relationship and
invalidate affected support classification/preferences on the game thread, without pretending that
source list mutation automatically updates every support-selection index. Recheck actual support
and buildability; generated identity alone does not make a legal automatic bridge. Require a valid
buildable/blueprint/removable terrain and actual support transition; exclude ordinary floor/soil
conversion/substructure and guarded VGE Designer terrain. Do not recurse through terrain chains.
Group needed cells by their actual support need and candidate set. Process groups in stable footprint
order, consider candidate support quantity and **all** costs, subtract the proposed building's inputs
and existing/new plan commitments, and select first fully affordable preference or first legal
preference with shortages. Available stock is a planning estimate; normal hauling rechecks usable
items. Never pre-reserve an entire map quantity as if delivered. Preview and plan use the same
snapshot, then revalidate before placement; retry/change is visible if state invalidates that plan.

Track dependencies between exact support/top orders, reuse existing plans and roll back only plans
created by a failed order. Selection state is irrelevant to support cancellation. Native failure
that leaves a support retry retains dependencies. Remove top dependencies without automatically
demolishing finished or independently useful support. These finite relationships do not require a
general graph framework beyond concrete footprint dependencies.

### 5. Declare content in XML and keep catalog uncertainty explicit

Use XML Extensions package/Def/node checks before declarative additions, including replacement tags,
related commands, build tools and device inheritance. The first such runtime slice declares
**required** `imranfish.xmlextensions` alongside required Harmony when its types are used. Inspect
the installed operations before choosing exact operations; do not invent operation names.
`XmlExtensions.FindMod` with `packageId=true` gates external content, and shape/content guards still
apply inside that gate. Merge memberships without duplicates; preserve existing tags, related
commands, categories and fuel/filter entries. No whole-list replacement is intended here.

The 117-list catalog supplies exact evidence, not blanket activation. Implement ordinary examined
families first; each optional group requires meaningful new/old pairing and explicit state needs.
Keep upstream-disabled ArmourRacks/RimBees/Security groups inactive pending separate research.
Do not silently adopt SoS2 solar-as-wall, missing vent callbacks or questionable cross-purpose
RimEffect bedside grouping. Absent optional providers require no warning for an unselected adapter;
an enabled expected integration with missing required content does require a bounded skipped-patch
warning. Missing one optional group must not disable unrelated Core replacement.

Wall devices need owned stable unique Def identities so co-installation can retain existing owned
objects without colliding with source Defs. Source Def names are recorded as actual external bindings,
not promised aliases. Compare normal cooler, normal vent and wide wall-device counterparts under
finalized Defs before acceptance. Inherit effective native costs/output/research with narrow explicit
geometry/airtight differences. The native vent's occupied-rectangle exchange is different from the
wide cooler's offset; validate heat/gas/vacuum separately. No backing-wall requirement is invented.
Obstructed-port placement relaxation cannot bypass runtime thermal checks. Raster work is deferred
to the mandatory asset skill and native visual gate; research did not select upstream art for reuse.

### 6. Shared facts and settings have one owner

Use native local grids/listers first. Shared material availability, exact carried/incoming quantities
and map Thing facts belong to the foundation's common facts subsystem used by S021 and future Ctrl+F.
Invalidate on relevant spawn/despawn/transfer/stack/material change, release on map/game teardown,
rebuild after load, and recheck pawn-specific permission/reachability/reservation at action time.
Do not port source Thing-ID/static caches or separate resource scans into this feature. Only the
finalized Def support catalog and concrete order relationships need new feature-local derived data.
Measure combined consumers/update/memory costs before calling any of this faster.

Actual settings use common global/default/save definitions and rendering. Stable settings cover
hide normal devices=false, hide wall devices=false, rock/fog new-save default=true, ordered bridge
preferences. Unknown bridge preference identities remain dormant; effective order ignores them,
preserves returning identities and appends new candidates in stable Def-name order. All save editors,
Use global, resets/import callbacks are disabled without a game; global editing remains available.
Rock/fog operational state is save gameplay state, copied only on first initialization from the
effective default. Building temperatures, bills, ownership, orders and links are entity state.
Keep native/contextual actions, bottom-right ordinary toggle and top-debug cheats. No new bottom tab,
all-dropdown reorder or foreign unpatching. All added text needs the seven baseline languages.

### 7. Preserve demonstrated boundaries without claiming package substitution

Research records exact consumers. Package-gated NANAME, Poured Concrete and VGE consumers do not
automatically activate against `fumblesneeze.megamod`; an identical type name is insufficient.
Prefer narrow supported adapters at real data/notification boundaries; only add a legacy facade if
a demonstrated consumer can actually bind safely without package impersonation. NANAME needs the
bridge catalog and registration of generated blended terrain; Poured Concrete needs completed
material-change curing registration; VGE needs
zero-work checkpoint and Designer/substructure exclusions. Inspect exact consumer call sites,
signature/lifetime/order and final native behavior before marking each supported.

Historical v1.4 Architect/Wall Light and v1.6 Research Reinvented evidence is not 1.7 compatibility.
Source `MaterialsNeeded` mismatch is recorded rather than emulated speculatively. Source mutable
predicate registry, callback names and deep save graph are not a general owned API commitment.
Basic Dropdowns' corrective behavior becomes correct owned menu composition; it is not another
internal patch against the owned replacement. Do not invent the source's Harmony owner for ordering.

With an original provider active, suppress owned overlapping new orders/offers. Preserve uniquely
named owned save content and recovery handlers, exclude owned pending jobs from duplicate source
processing where safely supported, and admit cancellation/completion recovery independently. If
effective recovery cannot coexist, retain the save state dormant with an actionable diagnostic;
never delete it, auto-switch providers, mutate normal config or unpatch a foreign owner.

## Candidate patch targets and runtime admission

These are **research families, not completed per-patch target specs**. Before implementation split
every actual Harmony/XML intervention into stable RS-T child IDs, individual documents and executable
validators linked to a concrete scenario. Declare atomic activation groups and recovery prerequisites.

| Family | Candidate consumed boundary | Minimum semantic predicate / fallback |
| --- | --- | --- |
| RS-T1 Material order | Designator placement, same-definition interception, frame work/cost/completion/failure | Exact requested target/material reaches one supported physical order; consumed inputs/refund/quality path isolated to owned replacement. No owned order if group unsafe. |
| RS-T2 Upgrade/install | GenConstruct placement/blocking, Frame completion/GenSpawn destruction, install path | Exact old/new identities, deconstruction versus full-refund semantics and committed actual installed item are demonstrable; no ambiguous wipe. Decline affected upgrades. |
| RS-T3 Usage/state | Bill usability, turret initiation, rest/occupancy, explicit owned handoffs | Preserve existing false/ineligible results; state remains usable/recoverable around work. Occupant/unfinished-work safety cannot depend on method existence alone. |
| RS-T4 Auto-rebuild | ThingUtility.CheckAutoRebuildOnDestroyed, blueprint/frame success/cancel/failure, serialization | Observe returned actual order after native eligibility; keep exact restoration identity through retry and successful spawn. Withhold restoration instead of guessing from cell. |
| RS-T5 Rock/fog/corners | GenConstruct placement/blockers, mining/smoothing, reveal/select/ghost, placement validator | Only declared blocker/hidden-state checks relaxed; normal research/capability and full-footprint recheck remain effective. Turn off affected planning path if incomplete. |
| RS-T6 Bridges | Terrain eligibility, blueprint/install placement, costs, support cancellation | Actual usable support/catalog, all ingredients, prerequisite order and dependency recovery; no ignored blocker without support. Return native placement on unsafe admission. |
| RS-T7 Device behavior | Cooler/vent place workers, wide cooler ghost/tick direction, native vent exchange | Correct port offset flows to actual heat exchange; footprint and sealing retained; native blocked operation still effective. Disable affected new device offers if unsafe. |
| RS-T8 Declarative XML | Native base device inheritance, Structure tools, related commands/replacement tags, optional catalog | Provider/Def/node shapes plus additive unique membership; XML Extensions guards and no partial active group. Preserve unrelated content on failure. |
| RS-T9 External adapters | Concrete curing, NANAME bridge facts, VGE restrictions, optional fridge/cost/state providers | Canonical package and exact consumed member/Def/lifetime semantics; local absent-safe adapter, failure closes only unsupported integration. |
| RS-T10 Shared facts | Material/stat changes, spawn/despawn/stack/transfer and teardown | Notification actually corresponds to successful state change, including skipped-original flow; retain correct native lookup fallback when freshness cannot be established. |

Method existence/callable shape suffices only for an observational callback whose occurrence is the
entire prerequisite. Argument changes, suppression, spawn-mode changes and direction offsets need
structural IL/data-flow checks of required uses/cardinality, not names alone. Validate the effective
incoming instruction stream on every transpilation, returning it unchanged on failure. Account for
skip-original prefixes and later repatching; do not execute foreign transpilers as speculative probes.
No exact assembly hashes, pristine-body tests or blanket foreign-owner veto. False/unknown predicates,
validator exceptions and install failures roll back only newly installed owned group patches and emit
one bounded startup warning per skipped enabled patch with ID, target, failed predicate, lost behavior
and fallback. Existing recovery must remain representable when scheduling/admission is disabled.

## Risks / Trade-offs

- [Same-object material mutation bypasses external spawn notifications] → Exact completed-change
  adapters and physical curing/stat acceptance; no universal provider-state guarantee.
- [Native completion destroys inputs before spawn] → Guarded transaction boundary and explicit
  recovery; tests cannot substitute for real failure/cancel/save workflow proof.
- [Stateful optional buildings or footprint changes lose contents/owners] → Preflight, explicit
  supported transfer or refusal, never generic source callback success by absence of an exception.
- [Bridge choice changes player cost unexpectedly] → Shared preview/plan decision, whole cost and
  visible shortage; choice recorded as RS-Q2 instead of claimed source parity.
- [Broader construction fixes affect unrelated jobs] → Order-scoped checks and native unaffected
  controls in acceptance, especially deconstruction, attachments, occupied beds and prototype work.
- [Beta/native/DLC geometry shifts] → Per-semantic admission plus exact isolated native observation;
  version provenance is evidence rather than a runtime allowlist.

## Migration Plan

No implementation/deployment or migration now. Introduce owned order/settings identities with the
first behavior; do not deserialize source frame/deep-building state under guessed type aliases.
Any future original-save importer needs separate observable backup, atomicity, failure restoration
and postcondition requirements. Co-installation must retain old owned orders/devices without new
duplicates. Removing the owning mod while its content remains is not claimed safe. Rollback keeps
recoverable state and actual containers; it does not delete player data to match an older package.

## Open Questions

RS-Q1–5 in research and the progress ledger cover provisional corrections, bridge/device balance,
native/provider handoffs, optional catalog/legacy support and concrete implementation/acceptance.
Keep these recorded; do not ask the user to explain known source behavior. The draft is a behavioral
baseline, not a waiver of those required implementation investigations.
