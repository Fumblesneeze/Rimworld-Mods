## Context

Single owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target1.7 beta.
S053 analyzes `Mlie.TurnOnOffRePowered`, Workshop2228219259, source
`88447b6019dcbb10f0b8719ac91deed942e35eb8` and the installed1.6 reference DLL. Its hash, source and
compiled coverage, native1.7 samples, real consumers and provider limitations are in the
[research](../../../mods/megamod/docs/refinement/2026-11-03-repowered-research.md).
This is a documentation-only draft. No upstream build or gameplay execution is evidence here.

## Goals / Non-Goals

**Goals:** one composed activity/rate decision, explicit configurable source budgets, correct
network admission, useful current information and bounded optional adapters. Correct source defects
without copying its global scan caches, inspection-text heuristics or self-patch structure.

**Non-goals:** a new power network, generator balancing, blanket coverage of unknown automation,
importing source settings, package impersonation, runtime implementation or beta compatibility claims.

## Decisions

### 1. Compose S052 and S053 directly

One map-owned activity policy supplies both features. Native listers are the first candidate source;
share derived membership only when necessary, following the foundation's lifecycle/invalidation
contract. Do not copy TurnItOnUtility's map-global sets or independent 2000-tick scans. Building
activity, demand and eligibility are related but distinct: lack of PowerOn must not erase a pending
legitimate demand and lock a standby device out of ever starting.

On overlapping work/TV/research/drill devices, S052 determines actual use and S053 selects the rate.
S053 adds scanners, doors, turrets, basins and linked facilities. S052 remains authoritative for
lamps, charging, ritual and other equipment outside S053's admitted classification. Settings label
the fallback LightsOut percentages as applying to equipment outside enabled RePowered budgets.
Disabling S053 makes shared equipment use S052 and exclusive equipment native; no extra job gate
remains. Failed combined device admission instead suppresses both sources' activity/rate/forecast
behavior for that device, retaining native operation and truthful unavailable inspection. This
failure mask takes precedence over the intentional-disable fallback and cannot silently enable
LightsOut's discount after a RePowered rate conflict. This preserves both sources' configurable behavior without a second multiplier or public
profile framework. No Harmony patches target our own implementation.

Use actual work callbacks/current jobs for ordinary work; reservations alone are not work. Explicit
external reservation declarations are a separate admitted activity kind. Schedule plus work or
reservation means schedule AND the union of work/reservation; schedule alone means allowed time.
No activity declaration means unsupported unless the target already has a proved native activity
adapter. The source's dead reservation/schedule sets are not a desired contract.

### 2. Rates are magnitudes with declared scope and precedence

Normalize owned calculations to nonnegative W; convert the native negative-consumption convention
at the admitted boundary. Order: explicit effective type pair, uniquely admitted external pair,
enabled vanilla preset, generic category policy. Ambiguous external pairs fail that device closed
unless an explicit player pair resolves only the rate conflict. Duplicate identical definitions
can deduplicate; conflicting definitions must not silently depend on iteration order.

Defaults and four preset pairs are normative in RP04. Generic active rates use an unmodified current
native baseline, never last frame's already-adjusted PowerOutput. Fixed W choices are explicitly
absolute. The spec deliberately preserves research scaling for generic rates while fixed choices
state their override effect. Optional provider costs for speed/range/extra services must be retained
as independent obligations; a provider adapter must establish separable baseline, controllable
portion, independent costs and lifecycle before applying a rate. If it cannot, leave that machine
native. Avoid overwriting a dynamic producer with a periodically reassigned fixed CompPowerTrader.

Native zero, flick, EMP, breakdown and disconnected state remain native. The shared0.01 W electrical
standby floor applies to all configured values below0.01 W, including tiny positive ones, and is
capped to a smaller positive native baseline; it never creates consumption from native zero or
turns a consumer into generation. Valid active rates are positive; no source <=0 multiplier clamp
to0.001 or accidental negative production. Float/overflow bounds are validation concerns, not an
arbitrary new balance ceiling. Effective-type rows store Automatic or one validated idle/active
pair through common scoped settings. Preserve absent-type entries and dormant overrides.

### 3. Work-start forecast uses incremental aggregate demand

Native WorkGiver_DoBill calls UsableForBillsAfterFueling during availability queries, before job
commitment. That postfix can supply a pure eligibility prediction and reason; it must not reserve
power just because the UI or a candidate scan queried it. Recheck and provisionally account for
new demand at the actual admitted job-start boundary, releasing it if start fails, cancels or loses
the target. Charge a linked facility once across simultaneous users. Do not persist provisional
admissions; reconstruct current native work after load. In-progress jobs bypass this added gate.

Partition pending and current demand by actual PowerNet, not by facility-link ownership. For each
network, let G be current net power W, D its incremental device/facility demand including already
admitted-but-not-yet-reflected starts, and h the configured horizon converted to simulation ticks.
That network's projected balance is G-D. If negative, its usable native stored energy must be at least
`(D-G) × h × CompPower.WattsToWattDaysPerTick`. Use native units; a RimWorld watt-day is not a
3600-second real-world watt-hour. Do not subtract current idle draw twice or promise reserves already
consumed by other loads. The bench network and networks of natively mandatory facilities must pass.
An unavailable optional facility does not create a new job prerequisite; an optional facility on a
different network does not veto this bench's start on forecast alone, and native power determines
its actual benefit. Its prospective load is still recorded on its own network for other starts.
Optional facilities sharing the bench circuit do affect that circuit's forecast, because their
draw competes with the bench directly. Revalidate membership at work-phase commitment; topology
changes discard/recompute pending allocations. Candidate queries and travel do not hold a durable
power reservation. Re-evaluate on real native retry, retain source's optional gate and500-tick
default horizon, correct its full-active-draw and incomplete-deficit formula.

Do not add forecast rejection to research, turrets or unrelated equipment merely because it consumes
power: source gate is electrical bill worktables. Never turn a native false result true. No actual
energy is withdrawn by the forecast. Native network switching remains the authority for availability.

### 4. Minimal semantic target specs and coherent failures

Follow foundation/mod AGENTS.md; create each stable target document before code. Required predicates
must cover effective semantics, skip-original prefixes and actual incoming transpiler IL. No hash,
whole-method equality or foreign-owner absence gate. Installation is guarded/idempotent with
owned-only rollback and one startup warning per skipped enabled patch. Compatible unrelated edits
are allowed; supported later repatching revalidates.

| Boundary | Evidence required before admission |
| --- | --- |
| WorkTable.UsedThisTick, research/drill/scanner and WatchTickAction | Actual use, successful native start/end, already-at-target recovery, final-user removal; observing a callback is insufficient if it can be skipped without recovery |
| Native power demand | Sign/units, original baseline, native zero/availability, research and independent provider demand; single application after valid native calculation |
| WorkTable.UsableForBillsAfterFueling and job start | Preserve native false; pure candidate query versus real start, incremental forecast, failed/cancelled-start cleanup |
| Building_Door | Both opening and closing progress, hold/blocked/access behavior; source Open check misses closing, so a name/signature alone is insufficient |
| Turret | Valid current target and target lifecycle, idle readiness and manual/native off states |
| Basins and facilities | Current plants, native valid links and occupied beds, link changes and facility power availability; no activation cycle through CanBeActive |
| Explicit reservation/schedule definitions | Finalized target ownership and permitted declaration, valid reservation lifecycle or CompSchedule semantics, combined gate |
| Optional providers | Canonical package, actual type/Def/member shape, dynamic costs and replacements; absence is not failed optional activation |
| Inspection/description consumer | Additive text, exact current rate/phase semantics, cache refresh; no broad regex replacement of native localized paragraphs |

Activity, rate, forecast and display for a device form a coherent admitted group. Skipping its
calculation cannot leave its extra work blocker behind. XML adaptation uses XML Extensions
FindMod(packageId=true), Def/content/node guards and idempotent additive merge; never replace
unrelated comps/extensions/lists. Update required dependency metadata with the first actual use.

### 5. Optional provider and consumed-contract boundaries

- Relevant Stats In Description `Mlie.RelevantStatsInDescription`, Workshop2692669482: selected1.6
  DLL reflects `TurnOnOffRePowered.TurnItOnUtility:PowerLevels` only after its original-package
  check, converts signed Vector2 to positive min/max, and caches text. It ignores custom overrides.
  An admitted consumer-specific adapter must use the composed effective rates and invalidate its
  cached descriptions. A facade alone cannot satisfy its package check; no spoofing. Current1.6
  reference is not a1.7 compatibility claim. S052 owns the same narrow adapter, not a duplicate.
- RePower Patch Pack `Mlie.RePower.PatchPack`, Workshop1836577627, pinned
  `a07a2ff346b476cbbab80791ec300dbfb659d17b`: metadata hard-requires Mlie.TurnOnOffRePowered.
  It creates `RePower.RePowerDef` XML entries without per-content gates; source loader ignores
  missing targetDef. An unchanged pack is not drop-in supported by megamod. Do not publish a duplicate
  type alias merely to hide errors while leaving the dependency unmet. A future explicitly admitted
  producer adapter must solve metadata/loading/class translation without changing downloaded files,
  retain signed values and guarded per-target behavior, or document the unsupported binding.
- EPOE Forked `vat.epoeforked`: historical1.0–1.4 RePower PatchOperations inspect/replace/remove
  names but use old display-name FindMod gates. This is historical evidence, not a current beta
  consumer. Do not attach a facade for dead source paths.
- VVE `OskarPotocki.VanillaVehiclesExpanded`, Workshop3014906877: installed1.6 GarageAutoDoor
  inherits `GarageDoor.OpenProgress()` (public float method), opening/closing timestamps and
  replacement between closed/opened Defs. Boundary must survive replacement and preserve power/
  obstruction controls. No1.7 LoadFolders entry was found; reference only.
- Project RimFactory Revived `spdskatr.projectrimfactory`, Workshop2033979700: pinned source
  `ede0db655d1f97120a6b42da265716919537ccf2` proves real CurrentBillReport/Active work and
  CompPowerWorkSetting speed/range/additional costs. Avoid RePowered's localized GetInspectString
  punctuation heuristic and its broad ProjectRimFactory exclusion contradiction. Supported1.7
  runtime and exact standalone machine/workbench automation paths remain RP-Q2 work.
- Doors Expanded: source recognizes `DoorsExpanded.Building_DoorRemote`. Official dev source clone
  commit was obtained, but source checkout timed out and was cancelled. Its current package/member
  contract is not admitted. Do not promote the source's reflection guesses to compatibility.
- Self-lit hydroponics: verify `Aidan.SelfLitHydroponics` content and continuous light demand. The
  source skips its plant evaluator without necessarily removing its power tracking; owned behavior
  leaves the entire unsupported combined device native rather than permanent idle.

External `juanlopez2008.LightsOut`, `Mlie.TurnOnOffRePowered`, `EbonJaeger.RePowerReborn` and
`ryder.turnitonoff` suppress the combined owned power group. The last two identities are source
metadata evidence, to revalidate against actual active packages before claiming tested co-install.
Do not remove their patches. Public source mutable sets, initialization methods and settings keys
have no demonstrated additional current consumer in the bounded installed scan; do not recreate
their entire API speculatively. No exact drop-in claim is made.

### 6. UI persistence and data lifecycle

Use common settings load/store/resolve/render. A device-type choice is configuration, unlike a
single door's hold-open or lamp Keep On state. Per-row Automatic versus explicit pair nests within
scope inheritance; reset automatic does not mean Use global and labels must distinguish them.
Rows identify supported/unavailable content, rate source and absolute versus baseline-derived values.
Search cannot filter the persisted dictionary. Main-menu save mutations are disabled in callbacks
as well as drawing. No source global settings singleton, HugsLib dependency or destructive importer.

Read all maps independent of CurrentMap. Invalidate spawn/despawn, transfer, ownership, plant and
link changes, job/target/schedule transitions and effective settings. Reconstruct after load without
waiting for a missed callback; release derived references on teardown. Use shared native/derived
data and benchmark combined update cost before making performance claims. Native power remains the
fallback if a provider fails, never a stale last-owned output or unbounded per-tick error.

## Risks / Trade-offs

- **Large source budget changes** → RP04 exposes source defaults and absolute presets. Core XML
  comparators are recorded in research, but finalized Def and native job reachability/balance review
  remain mandatory before implementation acceptance; no1.7 balance claim from source values.
- **Power readiness and simultaneous starts** → pure query plus real-start recheck, incremental
  shared-facility demand, explicit failure cleanup and native workflows for low/zero standby.
- **Dynamic optional machines** → admit only a semantically separable rate contract; otherwise leave
  native with a specific explanation. No fixed-output fight with provider code.
- **Cross-source settings confusion** → one visible rate source per device and explicit S052
  fallback labels; no hidden compounded percentages.

## Migration Plan

No runtime/data migration occurs. Future owned defaults initialize only missing owned entries;
upstream/HugsLib files remain untouched. Removal/disable restores ordinary rates or the S052
fallback without changing other providers, native flick state or stored choices. Any future import
requires a separately specified backup, atomic commit, failure restoration and verified postconditions.

## Open Questions

- **RP-Q1:** effective beta power/activity/job-start targets and low-power readiness/forecast proof.
- **RP-Q2:** actual1.7 optional providers, supported definition producers, dynamic costs and RSID
  binding; absent/unproved adapters remain inactive rather than inferred compatible.
- **RP-Q3:** review the chosen source-derived budget defaults and absolute-preset trade-off against
  finalized content and player workflows; S052/S053 draft composition is now specified.
- **RP-Q4:** implementation, honest RED/GREEN, seven languages, independent review and final native
  multi-map/settings/work/power/provider acceptance. None completed by this documentation change.
