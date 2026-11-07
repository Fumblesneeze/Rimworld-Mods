## Context

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; S032, **1.7 beta**.
The user explicitly requires the same index/cache as Ctrl+F, also shared by hauling. The arrow in
the note is not a search button request. This change is specification only.

[Research](../../../mods/megamod/docs/refinement/2026-11-03-buildable-material-count-research.md)
pins Workshop documentation, the byte-identical installed 1.6/1.7 assembly, all eight source type
decompilations plus AssemblyInfo, and relevant exact native 1.7.4995 original methods. No source
license/public repository was established. Do not copy implementation or ship upstream binaries.

## Goals / Non-Goals

**Goals:** exact-row cost display; honest stored-stock scope; current per-map values; shared data;
unchanged native selection; absence-safe provider boundaries and clear unsupported information.
**Non-goals:** changing construction price/availability, subtracting proposed blueprints, promising
path feasibility, new bottom tabs/settings, click-to-search, new global caches, broad provider/API
parity, source migration, or an optimization claim without combined-consumer measurements.

## Decisions

### 1. Associate the choice while the menu is created

Keep a narrow immutable choice association: map/lifecycle, actual designator/buildable, selected
Stuff where relevant, original presentation and original action. Attach it to the actual menu row
or grid option while those facts are available. Never recover identity by translated label,
ToString, list position after sorting/filtering, or the displayed icon alone. Two floors with the
same cost icon remain two different choices. Preserve native action/delegate, visibility, ordering,
tutor tags, close handler and extra UI supplied by a compatible provider.

Prefer the narrow native menu construction seam. The source's global buildableClicked/floorsClicked,
last buildable and WindowStack.Add interception can leak context into unrelated windows or fail
on nested/early-exit paths. Do not reproduce that architecture or its ref-FloatMenu-for-Window
parameter assumption. If a limited scoped interception proves necessary, bind it to the exact
creation call/menu and restore context in guaranteed cleanup; a process-global boolean is insufficient.

The source only hooks float menus and terrain dropdowns. This draft explicitly includes native
grid tooltips and resolvable nonterrain construction dropdown rows. Do not replace a provider's
layout merely to fit the source presentation. Utility/eyedropper rows have no construction association.

### 2. Reuse actual cost semantics and retain the full bill of materials

Use native CostListAdjusted for the actual BuildableDef/Stuff after resolving supported provider
cost semantics. It handles difficulty variants, rounded Stuff volume conversion and merging an
additional fixed ingredient in the same material. Do not multiply all fixed ingredients by ten
or equate CostList.First with the native cost icon's most valuable ingredient. Unknown dynamic
provider costs require adapter evidence, not a falsely precise numerator/denominator.

Primary display is the selected Stuff for a Stuff-choice row. For other construction rows use the
ordinary cost-icon material where known; otherwise choose greatest BaseMarketValue×adjusted count,
with original cost order as the deterministic tie break. Aggregate repeated cost entries for the
same material before presenting the list, using wide checked arithmetic and the game's units.
Keep every additional ingredient in the tooltip and explicitly distinguish one-material abundance
from full affordability. Zero-cost rows keep their normal label; unsupported costs do not become 0.

The owned spec's example quantities are acceptance fixtures, not balance changes. No additional
shortage color, sorting/hiding mode or settings are inferred from Workshop comments. Counts use
native/localized numeric formatting and the later Readable Numbers composition when supported,
without losing the exact quantities in details. Bind one annotation to original text; redraw must
not prefix an already modified label. Validate size/tooltip updates through native UI.

### 3. Share candidate ownership; define a stored-resource view

Native ResourceCounter already supplies stored-resource semantics, but its ordinary 204-tick
refresh and menu-construction snapshot are insufficient for the drafted freshness boundary. Its
membership is SlotGroup.HeldThings, GetInnerIfMinified, CountAsResource and nonfresh/fog rejection;
it does not test forbidden/reservation/path or subtract future demand. Preserve that stock meaning
while supplying current quantities through the common map-owned source.

| Dataset concern | Decision |
| --- | --- |
| Owner / consumers | One map/lifecycle owner shared with S033 Ctrl+F, S021/S125 hauling and other actual consumers; native listers first |
| Facts | Thing identity, Def, physical stack count, current map/holder/storage membership, fog/freshness and relevant countability; cost metadata is a separate native Def calculation |
| Derived view | Stored-resource totals keyed by material identity; it filters shared candidates and does not clone the whole candidate dataset |
| Updates | Relevant spawn/despawn/destroy, stack split/merge/count, pickup/drop/move, storage-zone or holder membership, fog/freshness, provider transfer and map teardown/load |
| Freshness | Reconcile completed changes before the next displayed query/redraw; use one consistent generation for a menu update |
| Thread / persistence | Game-thread native access; clear on map/game teardown; reconstruct after load; no live refs or transient totals in saves |
| Fallback | Shared service can perform a current native-policy count from native storage data once for the invalid generation; no stale ResourceCounter snapshot is accepted merely because it exists |
| Failure | If current native/provider counting cannot be established, show unavailable for that supported row and retain ordinary construction actions |
| Measurement | Combined search/hauling/menu queries, stock churn and open-menu refresh; include update time, allocations and memory, not just scan reduction |

This feature must not introduce its own per-map full scan/cache in parallel with Ctrl+F. Coordinate
fallback/rebuilds in the common owner; repeated identical queries in one generation reuse that
result. Shared candidate state is not shared eligibility: loose/carry items can remain search/
hauling candidates while absent from stored totals. Deduplicate physical identities when native
and admitted provider views overlap, and never count both a minified wrapper and its inner Thing.
Uncounted or virtual-only resources need a defined view; avoid GetCount's logging/missing-key side
effects or an invented zero. Shared-index acceptance remains governed by the foundation contract.

### 4. Every hook has a semantic target and fallback

Before adding each Harmony patch, write the executable target-spec document, tests and scenario
mapping. Prospective target families, not implemented patches:

| Family | Minimum semantics to establish | Failure boundary |
| --- | --- | --- |
| BC-T1 building material choices | Actual selected buildable and local Stuff lead to the same row's action; compatible alterations preserve that association | Leave this menu's owned annotations absent; original selection intact |
| BC-T2 dropdown/grid choices | Actual visible designator creates the associated row/tooltip and native action, independent of icon/label ordering | Isolate affected menu integration; do not suppress unrelated valid rows |
| BC-T3 count lifecycle | Relevant state mutation has completed before the query generation is invalidated/refreshed; no stale holder/map identities | Current shared native fallback or explicit unavailable count |
| BC-T4 provider cost/storage/menu adapter | Canonical package, actual content/member shape and coherent row/cost/count semantics | Disable only that adapter with bounded startup diagnostic |

An observer postfix may need existence/callable shape if that truly suffices. Any altered input or
transpiler must prove the relevant operation/control flow, including effective foreign transforms;
incoming streams are checked each time and returned unchanged on failure. No exact full method,
assembly hash or absence-of-other-owner gate. Idempotent installation, bounded one-warning-per-
skipped-enabled-patch and rollback of only owned changes are mandatory. Declare atomic dependent
groups; never leave labels claiming unsupported data after a partial failure. Original co-installation
is an explicit feature policy, not a blanket rule against other Harmony users.

### 5. UI, providers and persistence boundaries

This is contextual information with no separate setting or persistent operational toggle. It does
not justify a new bottom tab or debug control. If later a configurable display preference is
requested, use the common global/save model; do not create an independent ModSettings implementation.
Localize new scope/ingredient/unavailable explanations in all seven baseline languages. Preserve
the original menu text and any already-disabled reasons; newly added explanations must coexist.

Optional menu/storage providers are resolved by package/content and narrow shape checks. Standard
modded Stuff/terrain/buildables sharing admitted native semantics work without a name allowlist.
Digital/virtual storage and nonnative menus remain BC-Q1/3 until their contracts are inspected.
Do not imply support from Workshop comments or require those mods for ordinary construction.
The text consumer audit found only Use This Instead's replacement recommendation, not a runtime API
binding. No facade for BuildDict/buildElement/patch methods or package identity spoof is justified.

Transient association/count state dies with its menu/map/game. No source settings or serialized
gameplay state was found in this provider, but no universal source-removal claim follows. The
owned feature does not migrate or mutate normal saves. Co-installed source keeps owned annotations
inactive with a warning; never edit the user's mod list or unpatch the other owner.

## Risks / Trade-offs

- **Stored is mistaken for available to a builder** → explicit stock scope/details, unchanged
  native eligibility and real hauling/construction acceptance; don't invent an affordability lock.
- **Providers alter effective cost/membership** → admit specific semantics or show unavailable,
  never a misleading zero; retain exact provider present/absent/changed workflows.
- **Live redraw creates excess work or layout churn** → one shared generation, cheap unchanged
  queries, stable annotation ownership and measured mixed-consumer updates/UI layout.
- **Source's advertised floor fixes are incomplete** → test duplicate-material/different-cost rows,
  dominant-not-first ingredient, grids, mixed utilities and same-material fixed+Stuff costs explicitly.

## Migration Plan

Documentation only. Future delivery uses focused behavior slices and normal reviewed packages.
No configuration/save conversion or Workshop edits. All source inputs remain read-only. If a later
integration introduces persisted user-state changes, it needs a separate backup/atomicity/restoration
contract before implementation. No game launch or performance/compatibility acceptance in this draft.

## Open Questions

BC-Q1 retains exact effective row/grid/menu targets and later custom menu providers. BC-Q2 retains
native/provider storage membership, freshness events and combined-index measurements. BC-Q3 retains
actual binary consumers, virtual storage and unusual dynamic costs. BC-Q4 retains executable
admission, honest focused tests, native menu/hauling/construction action-to-result evidence, language
layout and release independence. None requires the user to reverse engineer an existing mod.
