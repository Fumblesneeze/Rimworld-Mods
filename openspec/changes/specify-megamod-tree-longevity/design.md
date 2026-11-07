## Context

Sole owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
S077 references Azuraal's `azuraal.treedontjustdie`, Workshop2784870434. Full source is four operations
in one XML file, supporting1.3–1.6 by metadata. [Research](../../../mods/megamod/docs/refinement/2026-11-04-tree-lifespan-research.md)
records exact hashes, current native consumers, static comparators and limitations. No runtime work.

The source sets lifespanDaysPerGrowDays to0 on local plant records directly inheriting TreeBase or
DeciduousTreeBase. Current native LimitedLifespan is multiplier>0; age damage consults it separately
from other threats. Scalar XML is sufficient. A prefix returning false for Plant.Dying or suppressing
all rotting damage would violate the requested scope.

## Goals / Non-Goals

**Goals:** no age damage for the supported native tree family, including bonsai and compatible
nested descendants; accurate native information, unchanged other threats and guarded XML behavior.

**Non-goals:** immortality, healing/resurrection, immortal crops/stumps, extra wood, changed growth/
research, forest-spawner replacement, arbitrary external lifecycle overrides, per-save Def mutation,
new settings, UI or source package impersonation.

## Decisions

### One declarative scalar, with explicit tree-family coverage

Apply lifespanDaysPerGrowDays0 to admitted concrete TreeBase descendants, including descendants
of DeciduousTreeBase and intermediate custom bases, using guarded XML Extensions operations.
Preserve each plant's container, attributes, comps and sibling lists; add the missing scalar or
replace that scalar alone. Already-nonpositive valid lifespans need no edit. Do not reset growDays or
change a whole list/plant record. Prefer concrete contributions rather than a broad TreeBase edit
that would leak into descendants whose actual plant identity has been changed by another provider.

Selection requires valid native plant/lifetime semantics, effective tree identity and not dead/stump
content; a familiar ancestor alone cannot prove these. Conversely, unfamiliar concrete names are not
a reason to reject compatible descendants. `PlantProperties.IsTree` alone is too broad: fibercorn
and native stumps also have harvestTag Wood. Native TreeBase ancestry plus final compatible shape
captures this source family without making all woody-yield crops immortal. Independent foreign
families require separately justified adapters, not name guessing or a universal support claim.

The source's immediate-parent XPath and requirement for an existing local plant node are not retained.
Correct recursive coverage is a deliberate proposal. An accepted native-compatible descendant's
positive local override is intentionally replaced; that finite-lifetime mod balance choice conflicts
with this behavior and must be documented. Unknown/custom lifetime semantics are not assumed safe.

### Inspect XML inheritance before selecting operations

Inspected XmlExtensions.ForEachDescendant recursively traverses Name/ParentName and can select
concrete nodes. ConditionalInherited and FindNodeInherited are ancestor searches, not complete
native inheritance resolution: they cannot by themselves establish effective child overrides,
Inherit=False or IsNull behavior. ForEachDescendant has no cycle detection. Before applying, validate
root/parent identity and cardinality, finite ancestry, effective plant/class/category/IsTree/isStump
semantics, scalar shape and any inherited override/removal relevant to the target. Reject ambiguity
without recursion or partial writes. Determine the exact native-aware staging/admission route in TL-Q1.

Use XML Extensions content checks and HybridPatch Safe container traversal with a narrow scalar
AddOrReplace only where proven appropriate; an existing compatible scalar is not an error. An absent
local plant container can be added without discarding inherited siblings, but only after proving its
inheritance semantics. Partition contributions so malformed optional content does not disable ordinary
Core trees. Each contribution stages and validates its own result before committing, restores only its
owned XML changes on failure and cannot leave duplicate scalars/partly changed containers. The selected
helpers do not supply this transaction guarantee automatically. Never accept success Always as evidence.

Optional specific content checks use XmlExtensions.FindMod with packageId=true and consumed Def/type
shape after the canonical provider is present. Generic compatible descendants are not a name whitelist.
Original `azuraal.treedontjustdie` authority is selected before any owned mutation; leave its operations
and results alone. No additional nested-descendant guarantee is made while that source owns behavior.
XML Extensions and its Harmony dependency are required in implementation metadata/load order. Current
framework1.6 files still need exact1.7 loading proof. Do not move static Def mutation to C# to evade it.

### Target spec, startup diagnostics and finalized content

Before code, write the actual stable target-spec document(s) and map every contribution to the
observable contract and executable predicate. Required semantics: target is supported living tree
content; one meaningful lifetime scalar/inheritance path; disabling that limit removes age damage
and corresponding finite-lifetime information while preserving other death terms. Check the relevant
current game consumers, effective provider changes and XML ordering, not hashes, complete bodies or
absence of other patches. A changed positive numeric lifetime is compatible with the intended0 result.

Missing required content, ambiguous shape, validator exceptions or apply failure leave the affected
owned contribution inactive. Warn once per skipped enabled patch with package/patch/spec/target,
failed predicate, unavailable behavior and fallback. Optional absent content and already-unlimited
valid targets are not failures. Preserve all foreign changes on rollback. Validate final loaded
results before claiming coverage; later external content mutation cannot silently resurrect a stale
success claim. No owned Harmony patch is selected. If one later becomes necessary, specify effective
and actual incoming semantics, atomic admission/owned-only rollback and supported repatch invalidation
before adding it; never execute foreign transpilers as speculative probes.

### Native persistence, presentation and population

Do not store a new age or reset existing native ageInt/growthInt/HP. Current TickLong still increments
age; native removal of finite lifespans stops only its age term. Restoring finite lifespans on a later
run can damage already-old living trees immediately under ordinary rules. No migration, grace period,
healing or resurrection. Existing native saves need no converter. New wild plants with unlimited
lifespan no longer receive native randomized finite age; their normal growth initialization remains.

Native PlantProperties/Plant display automatically omits limited-lifespan/duration/age rows. Use that
presentation; do not add another info patch, tab, debug control, setting or source-specific UI. If new
player-facing explanations are necessary, translate all seven languages; technical logs stay invariant.
There is no actual configurable value, so common global/save settings need no invented row or exception.

Native occupancy, density and species-weight rules continue using native listers. Longer survival
can change actual forest population/standing timber and reduce bonsai replacement, but no fixed
population or rate change is promised. Add no cache, scheduled scan or spawner hook. S006/S076 crop
choices, S063 construction clearing, S069 blight and future growth features compose through normal
plant/work rules; no internal patches or bypass of their explicit protection/designation rules.

## Risks / Trade-offs

- **Finite-lifetime content balance changes** → explicit recursive scope and no automatic special
  lifecycle override; compare loaded trees, crops and stumps in exact optional-content runs.
- **Wrong inherited classification** → validate effective overrides/removals; raw ancestor predicates
  alone are insufficient. Fail only the affected contribution before mutation.
- **Old saved trees when the feature is removed** → retained age means ordinary age damage resumes;
  document this consequence, do not silently reset the save.
- **Long-lived forests affect population/resources** → native occupancy/species weighting retained;
  measure biome turnover before claiming a balance or performance result.

## Migration Plan

No custom save schema, imported source settings or rewritten normal saves. Native objects retain
their state. Original provider active means no owned XML contribution. Removing the owned behavior
returns to whatever lifetime rules the next ordered mod set supplies, using retained native ages.

## Open Questions

- **TL-Q1 (XML/admission):** exact1.7 XML Extensions route, native-aware inheritance selection,
  per-contribution atomic staging, later XML/provider order and finalized target evidence.
- **TL-Q2 (scope/balance):** actual loaded descendants/special trees, crop/stump exclusions, forest
  turnover and static oak/pine/saguaro/bonsai anchors; recursive coverage is proposed, not live proven.
- **TL-Q3 (external contracts):** actual lifetime/damage overrides and information consumers;
  no compiled/reflection audit or generic independent-family adapter is claimed.
- **TL-Q4 (implementation/acceptance):** honest focused RED/GREEN, package/dependency/localization
  checks, independent review and real native wait/inspect/chop/threat/save/provider evidence.
