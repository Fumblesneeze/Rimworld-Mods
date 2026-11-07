# Cut plants before building (Continued)

## Identity and provenance

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin replacement **S063/P1-57**, `Mlie.CutPlantsBeforeBuilding`,
[Workshop3286376165](https://steamcommunity.com/sharedfiles/filedetails/?id=3286376165), continuing
tammybee / 民火's `tammybee.cutplantsbeforebuilding`,
[Workshop1539025677](https://steamcommunity.com/sharedfiles/filedetails/?id=1539025677).
[Public source](https://github.com/emipa606/CutPlantsBeforeBuilding), inspected2026-11-04.
Selected installed1.7.0 DLL SHA256
`6BFF6A8477A27FF12FDE99596A25164866519C28898D7B99449B766091B64568`;
public revision `cbf33efcc1842f869adefc09d178cf8c723b46f2` bundles the matching DLL.
License names CC BY-NC-SA4.0 and MIT, copyright2017TammyBee. No code, assembly or artwork copied.

Status: complete selected source/native/consumer research; **specification independently reviewed and strictly validated**.
**Nothing implemented or verified in game.**
[Research](../refinement/2026-11-04-cut-plants-before-building-research.md),
[proposal](../../../../openspec/changes/specify-megamod-construction-plant-clearing/proposal.md),
[design](../../../../openspec/changes/specify-megamod-construction-plant-clearing/design.md),
[observable spec](../../../../openspec/changes/specify-megamod-construction-plant-clearing/specs/megamod-construction-plant-clearing/spec.md),
[unchecked tasks](../../../../openspec/changes/specify-megamod-construction-plant-clearing/tasks.md).

## Behavior inventory and coverage

Every row is **specified, not implemented**. Requirement IDs refer to the owned observable contract.

| ID | Action and selected result | Boundary |
| --- | --- | --- |
| PC01 | Accept construction/install work → mark actual plant obstructions | Actual map/full rotation; no rejected placement, sampling or instant-completion side effects |
| PC02 | Add manual/automatic Build roof cells → mark roof obstructions | New accepted unroofed cells only; no stale room cells, No roof or Remove roof confusion |
| PC03 | Select clearing action → extract, currently available removing harvest or cut | Optional extraction starts off; immature/perennial cutting, real labor/yield/physical tree; no duplicate harvest |
| PC04 | Existing orders or canceled construction → preserve explicit choices | No blanket designation deletion; independent plant orders remain until ordinary completion/manual cancellation |
| PC05 | Encounter protected special tree → skip advance ordering | All automatic actions excluded; bounded notice defaults on; no universal tree immunity |
| PC06 | Incidental construction/roof clearing → apply worker restriction | Plant Cutting assignment default required; independent of advance switch; ordinary Growing unaffected |
| PC07 | Corner control or shortcut → saved operational state | Per-game state, default on, Backslash rebindable, map-view-only |
| PC08 | Configure preferences → common scoped settings | Three inherited global/save preferences; explicitly global-only initialization and native keybinding |
| PC09 | Compose owned/provider work → once-only behavior or explained fallback | Preserved physical costs/permissions and no fake provider identity |

## Interactions and intentional differences

The source marks the first plant with at least200 work in a cell, not necessarily a true obstruction.
It can run after rejected/sampling callbacks and use stale automatic-roof cells; the selected contract
requires actual accepted work. It also wipes all plant designations and can harvest a perennial that
remains in the way. We preserve explicit/unrelated orders and choose a single ordinary clearing action.
Roof incidental cutting respects an existing extraction order. No plant stats or work/yield multipliers
change; products and minified trees remain physical and use ordinary hauling limits.

Source OnlyCutters reaches global harvest/sowing eligibility. The selected rule is restricted to
incidental construction/install/roof clearing, still independent of the advance-order switch and
without an implicit forced-order bypass. Missing work settings preserve ordinary eligibility.
The special-tree exclusion applies to auto-designation, not general immunity from ordinary cutting.
Generated plant orders remain free-standing after parent cancellation, as in the source; no silent
cleanup, enable-time rescan or permanent parent-planning service is introduced.

S006 growing permissions and S008 explicit orders retain their scope. S013 quality cycles and S028
replacement use one owned policy without extra retries or lost resources/contents. S058 work priority,
S060 access and S061 roof safety retain independent checks. S136 Floor Pipette sampling cannot create
plant orders. S028 fog planning reveals no hidden plants; its successful full-footprint reveal
revalidation triggers the deferred first clearing decision under current rules. Shared data stays
native-first, with any genuinely shared derived facts in the common
map index rather than one new cache per source.

## UI audit and settings

| Source surface | Purpose | Selected destination/change |
| --- | --- | --- |
| Bottom-right icon | Operational on/off | Retained location; per-game persistence and explicit remaining-order help |
| Backslash key, including hidden world-view handling | Same operational toggle | Rebindable native global key; active only in usable colony-map view and outside text input |
| Four global checkboxes | Configuration | Construction / Plant clearing common settings; positive Show skipped notices label |
| Per-tree rejection message | Explain excluded trees | One bounded contextual notice per placement action/drag |

Source adds no bottom main tab; none is selected. Built-in defaults: start enabled=true,
prefer extraction=false, require Plant Cutting for incidental clearing=true, skipped-tree notice=true.
The latter three use Global default/Current save/Use global with disabled save controls at the main
menu and dormant overrides. Start enabled is global-only because each initialized game already has
its saved corner-control choice; changing this initializer never resets that choice. Existing orders
are not rewritten by settings. Seven baseline languages remain implementation work.

## External contracts, conflicts and omissions

Installed Vehicle Map Framework `OELS.VehicleMapFramework`, Workshop3426502333, has a real1.6
incoming binding: it checks the continued package and prefixes `CutPlantsBeforeBuilding.Util.DesignatePlants`
to redirect the map to the focused vehicle level. Its installed metadata supports1.5/1.6 only.
We do not spoof the source package or promise that internal method binding; a supported vehicle adapter
requires separate research/admission and native acceptance. Correct map ownership is specified;
unresolved external map contexts must never silently affect the parent map.

The bounded5374-DLL literal scan found that consumer and source versions; it is not proof that every
possible dynamic binding is absent. Source `DefaultMode/OnlyCutters/DigUp/NoInfo` settings, static
operational state, `TMB_` key Def, source icon, broad willingness hook and unguarded PatchAll are not
imported. Both original package IDs trigger owned-overlap disablement with an explanation, leaving
foreign patches/orders alone. No migration, safe mid-save removal, multiplayer, all-map-provider or
whole-mod drop-in compatibility claim is made.

## Remaining implementation and verification

PC-Q1–4 in the [progress ledger](../refinement/specification-progress.md) retain policy review,
effective target guards, optional providers and native acceptance. Per-patch target documents,
focused honest RED/GREEN, implementation review, package checks, measured combined cost and personally
observed build/install/roof/harvest/extract/order/settings workflows remain unchecked. The research
comparators are static Core XML, not finalized active-mod Defs or live gameplay measurements.

The initial reviewed draft had **9 requirements,37 scenarios and28 unchecked tasks**. Independent source review's
current-harvestability finding was corrected and rechecked. Fog planning preserves S028's existing
reveal/revalidation contract; independent composition review found no issues. Typed strict validation
passed **85 items,0failed**, and all eight scoped Markdown documents passed local-link/whitespace
checks. These documentation checks do not establish runtime compatibility or gameplay acceptance.

S069 adds an explicit extraction→infection composition scenario: automatic blight cutting neither
adds a competing cut nor prioritizes a coexisting cut while extraction is ordered. Normal extraction
eligibility remains; cancelling it alone causes no blight catch-up. Current draft: **9 requirements,
38 scenarios and28 unchecked tasks**. This directly preserves the chosen physical-tree action;
runtime verification remains open in both owners' feature tasks under the same megamod owner.
