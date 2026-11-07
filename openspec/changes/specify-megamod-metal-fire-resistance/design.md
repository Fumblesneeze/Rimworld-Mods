## Context

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
S104/P2-34 replaces `Unon.NoBurnMetal`, Workshop1923990111. The selected1.7 source is a dynamic
global Def mutator, despite the older mod's XML-only history. `loadFolders.xml` selects `Common`
and `1.7`, excluding root/1.4/1.5 patches. Full source and selected DLL decompilation were inspected.
[Research](../../../mods/megamod/docs/refinement/2026-11-05-no-burn-metal-research.md) retains exact
identities, source defaults, optional XML observations and native1.7.4998 contracts. No live result
is claimed. Every implementation and admission task remains open.

## Goals / Non-Goals

**Goals:** Preserve material-factor and named-infrastructure behavior, current stats and ordinary
fire consequences; provide common per-save inheritance without leaking through global Defs; retain
other mods' unrelated data; keep evidence-backed defaults, source attribution and safe fallback.

**Non-Goals:** Blanket final-stat zeroing, flame-damage immunity, pawn protection, metal-cost
heuristics, floor edits, raw-resource base changes, new research/costs, source assembly emulation,
automatic settings migration, a private Thing/fire index, or runtime implementation in this slice.

## Decisions

### Preserve the contribution, not an unconditional final result

Native `StatWorker.GetValueUnfinalized` obtains the base, multiplies by Stuff's Flammability factor
where native conditions permit, then applies Stuff offsets, quality/component contributions and
later finalization. The source zeroes only `stuffProps.statFactors` for material targets, or
`statBases` for direct non-Stuff targets. Preserve that semantic boundary. A later positive offset
is deliberately not cancelled. A final-result-zero postfix or late StatPart would broaden behavior
and obscure another provider's contribution. A subtractive StatPart cannot generally reconstruct
the earlier value after arbitrary factors/offsets and is also rejected.

Proposed narrow adapters, subject to NBM-Q1's effective-code proof:

- Replace only the material Flammability-factor lookup in the existing evaluation with an owned
  choice between its current native result and zero. Consume the actual request's Stuff, current
  stat and admitted policy; preserve multiplication conditions, every other stat and subsequent
  offset/factor flow. Do not patch the generic factor-list helper globally.
- For the three direct infrastructure types, use a guarded Flammability base-value seam preserving
  all subsequent modifiers. `StatWorker.GetBaseValueFor(StatRequest)` is an inspected candidate;
  prove actual callers/overrides before choosing the hook. No global final-value override.
- Integrate material/base explanation and prospective/current/MinifiedThing paths using the same
  policy so native reports do not display unchanged inputs while reporting an unexplained zero.
  Native `minifiedThingInherits` already delegates to the inner Thing; avoid applying twice.

Do not mutate or temporarily swap global `ThingDef` stat lists during a query. Per-save effective
choices require dynamic behavior; static XML changes alone cannot meet the inherited-settings
contract. Stable declarative catalogue data belongs in owned Defs/XML. Any external XML patch uses
XML Extensions package and shape guards, additive entries and idempotent unique membership. No
whole list/category replacement is intended. XML Extensions becomes required at first such use.

### Build a small content catalogue from finalized definitions

Reuse the owned finalized-Def catalogue if one exists; otherwise keep this feature's immutable
definition descriptors local until a real second consumer needs them. Do not scan maps. Resolve
each descriptor by actual provider package plus Def identity; display current labels and attribution.
Discover native `Metallic` Stuff membership, plus the36 curated source identifiers retained in
research. Defaults require expected provider/content/shape, not a label or DefName collision.

The source's six `knownMods` entries only create inactive headers. They do not make every material
from those packages default-on. The installed Expanded Materials item3333419387 still uses
`Argon.ExpandedMaterials.Metals`; inspected1.7 metallic resources already have zero factors. More
Useful Materials item3734307960 advertises only1.6; its XML is comparison evidence, not1.7 acceptance.
Alpha Animals, Medieval Overhaul, Cupro's Alloys and Anomaly content were not locally available for
this inspection. Keep their specific mappings gated pending NBM-Q2, without blocking Core.

Use current finalized contribution semantics: absent material factor normally means1; absent
direct base uses the current stat default, inspected0. Omit already-zero or nonpositive contributions
that need no protection; do not show a nonfunctional direct toggle simply because an entry is absent.
Nonfinite/ambiguous duplicate entries or unprovable custom semantics are unavailable, not guessed.
The source's captured original is evidence only, not an owned restoration value. Keep preferences
when owned protection itself produces zero; catalogue visibility must not oscillate with toggles.
Reconcile genuine content/shape changes through the owned admission lifecycle, not each draw.

### Use common settings and one commit per bulk action

Stable setting identity includes provider and material/infrastructure identity. Use the common
descriptor/resolution/persistence/renderer path, ordered under Materials and fire. Each row exposes
global default, Use global, current save, actual availability and the current effective choice.
Keep configuration distinct from admission: disabled/failed adapters must not claim effective
protection. Common context resolution uses globals at the main menu and current save inheritance
inside a game, including world view. No per-building component or save-specific Def snapshot.

For bulk mutation, build an explicit target set of current available descriptors before committing.
The UI names Global defaults or Current save, All available or one provider, and count. Search does
not silently restrict the source's all-target semantics. Save On/Off creates explicit overrides;
save Use global retains dormant overrides. Validate and commit atomically through common settings,
with one policy/cache invalidation, not the source's per-item invalidation loop. Reject save mutation
callbacks when no game exists. UI-only hide-unavailable (defaulttrue) and collapsed groups (default
empty) are common global preferences with the documented navigation-only exception. Search and
scroll remain transient. Searching reveals rows without writing collapsed state.

No original `NBMSettings` file is read or written. The source keys are DefName-only; silently
importing them would introduce provider ambiguity and substitute global values for save inheritance.
No migration facade is justified by the inspected consumer scan. Any future import needs its own
backup/atomicity/failure restoration contract.

### Admit each native path semantically and keep failure groups coherent

Create/index actual patch target specs before implementation. The candidate groups are material
evaluation plus its explanations, direct-infrastructure evaluation plus its explanations, and
common context/cache lifecycle. A failed shared lifecycle prerequisite suspends all dependent
groups; one unresolved provider entry suspends only that entry. Document actual grouping once seams
are proved, without multiplying one generic patch into imaginary per-material patches.

Target predicates cover the exact consumed argument/request, material lookup and multiplication
data flow, relevant subsequent independent contributions, explanation inputs and lifecycle. Check
incoming effective IL for every transpiler invocation, tolerate unrelated edits/other owners, and
return it unchanged on failure. Inspect skip-original/relevant prefix interactions without replaying
foreign transpilers speculatively. Invalidate/revalidate on supported later repatching. Original IL,
hashes, exact versions and owner absence alone do not prove suitability. Hashes identify evidence.

If false/unknown, a validator throws or installation fails, deactivate the affected group and roll
back only newly installed owned patches. Emit one bounded startup warning per skipped enabled patch
with package/patch/target-spec ID, target, reason, unavailable behavior and fallback. Explicitly
disabled paths and absent unselected optional content are not failed patch candidates. Settings
retain choices but report actual unavailability. On later invalidation stop dependent effects and
clear stale outputs before returning to ordinary behavior.

When `Unon.NoBurnMetal` is active, suppress our entire overlap rather than fighting its process-wide
Def mutations. Preserve source behavior and owned dormant preferences. No unpatching foreign owners,
restoring captured Def entries, removing another mod, or reporting that our Off undoes source On.

### Freshness, fire consumers and composition

On a committed effective policy change, save load/switch/teardown, admission change or supported
provider reconciliation, invalidate affected native Flammability caches and shared prospective
stat results before the next read. Inspect actual immutable/stale-cache behavior and any cached
inspection UI under NBM-Q1. Never recursively query the modified statistic to calculate its own
unmodified baseline. A descriptor's material contribution is not a substitute for an instance's
final stat. Bound catalogue work to definition changes and UI work to visible rows.

Ordinary `Fire` already re-reads fuel at its150-tick complex update. `Thing.FlammableNow` and
fire-fuel selection use the resulting stat with native0.01threshold; terrain is separate. Native
`DamageWorker.Apply` keeps a0.05minimum flammability-based multiplier for damage types that use it;
`DamageWorker_Flame` can damage before ignition. No direct damage/fire/pawn hook is needed just to
force immunity. Do not scan/delete fires on a setting change or copy the source into a second job.

S072 structural damage, S079 fire response, S091 corpse burning, S096 item recipe orders and the
S009/S012 material/recipe displays consume current native results and their own rules. Confirm
these intersections without modifying their defaults or patching owned implementations. No new
Ctrl+F/hauling index is justified: these are definition and stat requests, not Thing-list queries.

### External contracts and UI

Source public `NBMTargetsDef` (`defNames`, `knownMods`) and `NBMKnownMod` (`packageId`, `label`)
allow source-target extensions; `NBM.Targets/Sections/IsOn/SetOn`, `NBMTarget` and settings are public.
The inspected source's own XML consumes that class. Bounded installed ASCII XML/C#/DLL searches
found no external caller, only YART evaluation mod-list entries. This does not prove reflection or
future consumers absent. No public facade or package spoof is planned without an actual consumer.
Our owned catalogue can carry curated entries; a future external extension has to be separately
specified and gated instead of loading an unknown assembly.

The source has a mod-settings page and no bottom tab. Retain that placement in the common page,
not its fixed-width row arithmetic. Preserve provider attribution, localized label/identifier search,
counts and source labels while replacing unconditional Fireproof/Yes Burn claims with precise
contribution/effective-state help. Seven baseline languages are required. Any later native icon
selection follows the repository's asset/visual gates; this prose slice selects no raster art.

## Risks / Trade-offs

- **Dynamic contribution hooks are more involved than source global mutation** → Narrow guarded
  seams, public-observable fixtures, native explanation parity and disable coherently on uncertainty.
- **Another source legitimately adds flammability after the selected contribution** → Preserve it
  and explain the resulting stat; do not claim an all-fire immunity contract.
- **Source advertises providers beyond current verified content** → Package/content/shape gates,
  source-revision evidence and explicit optional acceptance work; no universal compatibility claim.
- **Cheap steel walls and basic wiring gain a material advantage** → Preserve costs/HP/research and
  other hazards; measure native stone/steel/wood and hidden/ordinary conduit comparators before acceptance.
- **Unresolved API consumers or old settings** → State unsupported bindings and no silent migration;
  add a facade only after proving an actual consumer contract.

## Migration Plan

Nothing is installed by this change. Implement in focused vertical slices after target specs and
tests. Store owned common settings only; no new Thing class or rewritten existing item is required.
Disabling protection withdraws its calculation contribution and clears stale results; it never
restores global snapshots or repairs HP. Missing providers leave settings dormant. Mid-save addition,
disable/re-enable and removal remain unaccepted until actual native save workflows prove them.

## Open Questions

- **NBM-Q1:** Exact selected1.7 effective factor/base/explanation/cache/lifecycle seams, atomic
  grouping and repeated/repatch admission; native unknown/custom stat-worker boundaries.
- **NBM-Q2:** Missing optional-provider1.7 identities/shapes, finalized combined patches and actual
  API/reflection consumers; reconcile source-declared exceptions without name-only activation.
- **NBM-Q3:** Finalized balance/fire/damage comparisons, source-default choices including bioferrite
  and carbon fiber, seven-language crowded UI and bounded catalogue/cache cost.
- **NBM-Q4:** Focused RED/GREEN, independent runtime review and causal native construction/fire/
  inspection/settings/save/provider workflows; source migration remains deliberately absent.
