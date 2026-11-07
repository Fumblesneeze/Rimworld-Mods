## Context

Sole owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
S069 replaces Auto-Cut Blight - 1.6 (`Defi.AutoCutBlight`, Workshop 3520167264).
[Research](../../../mods/megamod/docs/refinement/2026-11-04-auto-cut-blight-research.md) records the
complete five-class binary/XML inspection, exact hashes, native 1.7.4998 paths and bounded consumer
search. This is a design draft; compatibility and runtime behavior remain unverified.

## Goals / Non-Goals

**Goals:** react to confirmed new infection, share ordinary cut orders, prioritize blighted cutting
through native work scheduling, preserve cancellation and compose directly with manual order tools.
Use existing native collections and common settings rather than source-specific jobs or caches.

**Non-goals:** alter infection/spread, clear healthy buffers, force every pawn to respond, scan old
outbreaks on enable/load, bypass access/protection, clone source saved types, or claim safe uninstall.

## Decisions

### Native orders and jobs

Add native `CutPlant` only if absent; never remove/re-add it or call the destructive native
`Designator_Plants.DesignateThing` path that clears all designations. Inspect ordinary cut-designator
eligibility and confirmations separately. Automatic eligibility requires a current spawned plant,
revealed cell, player designation authority and no manual protection/confirmation requirement.
Map-wide means no additional home-area/growing-zone filter, not permission to operate hidden content.

An existing `ExtractTree` designation excludes both automatic cut addition and this giver's preferred
cut selection. Check it at infection and work selection, including coexisting CutPlant. Preserve the
extraction and any existing markers; do not upgrade a competing cut over the selected physical-tree
action. Cancelling extraction alone is not a new infection and causes no catch-up. This is direct
composition with S063, not a patch against it or a new guarantee of extraction success. Native
adjacent-blight propagation already excludes plants with any map designation. Foreign/native ordinary
work on independently conflicting orders is not silently rewritten by this feature.

An owned work giver uses the native per-Def designation collection, filters current blight and
constructs the native `CutPlantDesignated` job only after equivalent current native eligibility.
Do not blindly return `WorkGiver_PlantsCut.JobOnThing`: when HarvestPlant and CutPlant coexist it can
choose harvesting. Preserve both orders but deliberately choose cutting in this urgent candidate path.
Retain generic job reachability, allowed-area/access, forbidden, reservation, willingness, required
Manipulation and prevent-cut checks. Do not add a stricter danger policy while claiming native parity;
the inspected cutting scanner permits Deadly danger. Revalidate at native work use time.

Keep the source work-giver comparison values (`priorityInType=1400`, `emergency=true`,
`prioritizeSustains=true`) as the initial native-scheduling design, subject to exact Def guards.
They do not rewrite a pawn's work priorities. In current 1.7, emergency eligibility depends on the
Plant Cutting priority relative to the best enabled ordinary work type, not solely priority 1.
Other work types, think-tree needs/safety branches and interruption remain native.

Native PlantCut/PlantWork own movement, work amount/speed, collection, XP, cleanup and subsequent
queued targets. No custom JobDef/driver/DesignationDef is needed. Native cutting of blighted plants
can add adjacent blighted targets (current native flood-fill bound 100); do not duplicate, suppress
or describe this as owned infection automation. Native queue cleanup still applies.

### Event transition, not a recurring world scan

Capture infection state around the admitted infection path and act only on an actual unblighted →
blighted transition on the same supported spawned plant. A postfix callback, skipped original,
already infected call or failed spawn alone proves nothing. Account for virtual/custom providers in
target scope; do not claim arbitrary direct Blight spawning is covered. Use no per-plant suppression
registry, saved infection history, map scan, added flood-fill or separate Thing index.

No load/enable/reveal catch-up: old blight remains a manual native/S008 operation. Cancellation is
ordinary designation removal; repeated notification cannot recreate it, while an actual later
reinfection or independent native adjacent-cut continuation can. Recovery leaves the explicit order
but removes this giver's urgent eligibility at its next choice. Disable leaves orders and running
native jobs alone and stops owned new designation/urgent selection.

### One policy, contextual operational toggle

Define one common boolean under **Plants / Blight**, built-in true. The bottom-right **Automatic
blight cutting** control changes that current save's override through the same setter/resolver;
it does not maintain another flag. Global default, Current save and Use global apply, including
dormant overrides, first-override seeding, main-menu callback guards and save switching. No active
game means no operational mutation. The control remains meaningful in loaded-game world view and
applies to subsequently active maps in that save. Per-plant orders stay native gameplay state.

Show requested state separately from availability: if only marking or priority is admitted, explain
which part works in the control/settings and retain the requested preference. If neither is admitted,
show unavailable rather than a working on-state. No new work column, bottom tab or debug control.
Use existing native icons if suitable; translate all new labels/tooltips/explanations in seven baseline
languages. No new raster asset is proposed.

### Minimal semantic admission and required content

Before code create stable target documents and executable validators for each actual contribution:

| Group | Required meaning | Failure / fallback |
| --- | --- | --- |
| New-infection observation | Supported infection transition and usable current map/plant state; safe ordinary designation eligibility, extraction precedence and idempotent add | No owned automatic marking; native/S008 manual marking remains |
| Blighted cutting priority | Guarded native PlantCutting work type, CutPlant designation/job, current job eligibility/extraction exclusion and scheduling contract | Ordinary native cutting remains; admitted marking may still place ordinary orders |

Use Harmony only for the event bridge/necessary engine access. Prefer guarded declarative XML for
the owned work-giver contribution; actual XML Extensions operations and its 1.7 route must be
inspected before implementation. First use adds required `imranfish.xmlextensions` metadata/load
order; Harmony is required when eagerly referenced. Preserve all unrelated Def fields/list entries
and avoid duplicate registration. Never runtime-mutate shared Defs to toggle policy.

Each patch target checks its minimal consumed semantic shape, including effective foreign prefixes
that skip execution and relevant transpilers. Do not reject all foreign owners or require exact
hashes/whole IL. Original IL cannot prove effective semantics; every actual transpiler validates its
incoming stream and returns unchanged on failure. Define startup ordering and later-repatch
invalidation/revalidation without repeatedly probing foreign transpilers. False/unknown/exception
or install failure rolls back only newly owned patches in the affected group. Each skipped enabled
patch gets one bounded startup warning with package/patch/spec/target, reason, unavailable behavior
and fallback; explicit user disable is not an admission failure. No per-tick warnings.

### Providers, saved data and direct composition

`Defi.AutoCutBlight` active: original is sole provider, owned overlapping groups inactive, owned
settings dormant and controls explain provider authority. Never unpatch or alter source settings.
Older IDs in replacement catalogs require separate actual-source inspection before support claims.
Do not spoof package, assembly, namespace, source Def names or settings keys.

Smart Farming (`Owlchemist.SmartFarming`) can produce the same native order on CropBlighted. Reuse
an equivalent existing order; allow compatible ordering rather than treating another postfix as
automatic rejection. Our disable does not stop its automation. No adapter should be added merely for
an add-if-missing producer; any actually needed adapter must be package-gated and shape-guarded.

S008 native/manual cutting and cancellation, S006 growing-zone choices and S063 construction
clearing share orders/jobs directly. No patches against owned features. Linked growing settings do
not impose a new zone-only blight scope. Native designation data suffices; no private scan/cache or
speculative shared-index dependency. Measure combined real workloads before claiming faster work.

Source custom designations/jobs/driver classes are real save bindings. No automatic migration is
selected. Before authorizing a migration, specify backup, atomic conversion, restoration and
verified postconditions. Even native jobs can save work-giver references: native job reuse does
not prove safe removal of our feature. Preserve source files and saves during inspection.

## Risks / Trade-offs

- Event-only scope leaves old/previously hidden outbreaks manual → explicit tooltip and native/S008 command.
- A cured plant's explicit order can still cut it → preserve ordinary cancel semantics and explain retained orders.
- Native adjacent continuation can re-mark cancelled blight → state this limit; no new exclusion registry.
- Emergency work can change labor allocation → preserve native priorities, needs and eligibility; observe real jobs.
- Partial admission or another producer can look like an ineffective toggle → display actual available contribution/provider.
- Current source/XML dependency lacks proved 1.7 support → exact installed-content guards and isolated live acceptance remain open.

## Migration Plan

Documentation only now. Implement vertical slices after focused RED, then reviewed minimal code,
package checks and native action-to-result evidence. Rollback removes newly owned patches/contributions
only; it does not erase ordinary player orders or fabricate restored save state. No source-save import,
uninstall conversion, build, dependency edit or game run is delivered by this proposal.

## Open Questions

ACB-Q1–4 in the refinement ledger retain selected corrections/control scope, exact transition/work/XML
admission, provider/save lifecycle and focused tests/localization/performance/native acceptance.
