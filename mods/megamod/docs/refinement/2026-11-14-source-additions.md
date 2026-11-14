# Source additions and integration decisions — 2026-11-14

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
The user added the sources below while implementation was paused. This records intake and one
Needle-inspired specification; it does not resume implementation or accept gameplay.

## Identity and duplicate audit

The queue now contains **152 unique entries: 140 specification drafts and 12 new intake entries**.
S053 **Turn It On and Off - RePowered** and S059 **Best Mix (Continued)** were already selected and
specified; retain those IDs without duplicates. DragSelect, Clothing Sorter and Relevant Stats had
no source queue entries. Earlier Relevant Stats references were integration research, not selection
of its replacement. The twelve intake rows below require full sequential research and a separately
owned OpenSpec change before implementation. S141 has its selected-scope draft now.

Public author documentation and installed About metadata were checked on 2026-11-14. Descriptions
below are intake summaries, not proven runtime compatibility. Installed metadata comes from
`F:/Steam/steamapps/workshop/content/294100/<Workshop ID>/About/About.xml`; local hashes and bounded
research remain in `artifacts/Refinement/2026-11-14-intake/source-intake.md`.

| ID | Source / canonical package | Primary allocation | Intake purpose / composition |
| --- | --- | --- | --- |
| S140 | [Linkable Descriptions](https://steamcommunity.com/sharedfiles/filedetails/?id=3837322529), `sk.linkabledescriptions` | P6-C | Facility build-button descriptions; distinct from S074 info-card links |
| S141 | [Needle](https://steamcommunity.com/sharedfiles/filedetails/?id=3834934844), `ZIPender.Needle` | P0-B | [Owned settings search draft](../../../../openspec/changes/specify-megamod-settings-search/proposal.md); compose with S047/common renderer |
| S142 | [Xenotype Descriptions](https://steamcommunity.com/sharedfiles/filedetails/?id=3832924427), `lakinmanheim.xenotypedescriptions` | P8-A | Editable custom xenotype/xenogerm text and saved templates/tooltips; Biotech-dependent behavior |
| S143 | [Insects Aren't Raiders](https://steamcommunity.com/sharedfiles/filedetails/?id=3838615303), `tk421storm.InsectsArentRaiders` | P6-B | Threat-aware hive/swarm aggression, defense and return-home behavior; research native/DLC/provider boundaries |
| S144 | [Gear Tab - Compare & Swap](https://steamcommunity.com/sharedfiles/filedetails/?id=3835986381), `DOORAJP.GearPage` | P8-A | Colony equipment comparison and real swap/equip orders through **Assign**; genuine Simple Sidearms support |
| S145 | [Meal Ingredient Filter](https://steamcommunity.com/sharedfiles/filedetails/?id=3837849562), `NachoToast.MealIngredientFilter` | P2-A | Count existing meals toward bill targets according to their ingredients; compose S014/S017/S018/S059 |
| S146 | [Which Mod's Scenario Is This?](https://steamcommunity.com/sharedfiles/filedetails/?id=3836189392), `szkkka.whichmodsscenarioisthis` | P6-C | Scenario-provider labels; compose S113 search on the existing scenario-selection screen |
| S147 | [Uniform Growing Zone Tool](https://steamcommunity.com/sharedfiles/filedetails/?id=1898969926), `asmallrabbit.UniformGrowZone` | P5-C | Fertility-matched growing-zone painting; compose S006 linking, S016 counts and S150 shapes |
| S148 | [DragSelect](https://steamcommunity.com/sharedfiles/filedetails/?id=2599942235), `telardo.DragSelect` | P0-B | Drag across list controls in trade/loading/Work/Schedule; compose S095/S133/S135, not map rectangle selection |
| S149 | [Resource Dictionary](https://steamcommunity.com/sharedfiles/filedetails/?id=2817607528), `Scorpio.ResourceDictionary` | P0-C | Primary-resource substitution/reference redirection in the **same content settings area as Cherry Picker** |
| S150 | [Designator Shapes](https://steamcommunity.com/sharedfiles/filedetails/?id=1235181370), `Merthsoft.DesignatorShapes` | P3-C | Shape-driven construction/mining/zoning, fills and source undo/redo; research 1.7 native overlap; compose S008/S016/S036/S147 |
| S151 | [Clothing Sorter](https://steamcommunity.com/sharedfiles/filedetails/?id=2275655161), `Mlie.ClothingSorter` | P8-A | Apparel categories by layer/technology/source/tags and subcategories; compose clothing filters and gear views |
| S152 | [Relevant Stats In Description](https://steamcommunity.com/sharedfiles/filedetails/?id=2692669482), `Mlie.RelevantStatsInDescription` | P6-C | Relevant building stats in Architect descriptions; merge overlapping owned S098 storage and S052/S053 power contributions |

## Fixed user decisions and research boundaries

### S144 — Assign entry and Simple Sidearms

The equipment comparison/swap workflow must be accessible through the existing **Assign tab**, with
no standalone Gear bottom tab. The source's colony-wide workflow is distinct from S132's selected
pawn Gear presentation. The broader Assign redesign is deliberately waiting for the user's later
requirements; neither its layout nor a redesign of all existing Assign functions is decided here.

Support [Simple sidearms](https://steamcommunity.com/sharedfiles/filedetails/?id=927155256), canonical
`PeteTimesSix.SimpleSidearms`, as a genuine optional integration. Research main/sidearm selection,
remembered equipment, actual physical transfers and canceled/failed orders. The feature must remain
usable without that provider and preserve provider rules when present; a label or source-package
facade is not support. Installed metadata currently lists versions through 1.6, so actual 1.7
adapter admission and native absent/present workflows remain required. The Gear source advertises
sidearm support with a drop-first limitation for direct sidearm transfers; later specification must
explicitly choose and document the owned workflow rather than claim universal exchange capability.

### S149 — Content settings, distinct resource semantics

Place resource equivalence/substitution alongside S038 Cherry Picker content selection, reusing
S020 selectors and common scope/status presentation. Resource Dictionary redirects references to
a chosen primary resource; Cherry Picker excludes content. These remain distinct explicit actions
and must not silently remove physical stock or reinterpret a substitution as deletion. Source
restart requirements, existing items, quests/traders, recipe/blueprint references, unavailable
primary choices and S038 exclusions need one coherent later contract. Installed source metadata
lists versions through 1.6; its broad compatibility claim is not owned 1.7 acceptance.

### S053 / S052 — RePowered and LightsOut already compose

Both are already specified: [RePowered](../replacements/repowered.md) and
[LightsOut](../replacements/lights-out.md). LightsOut supplies occupancy/sleep lighting and actual
device-use state. RePowered supplies configured idle/active costs and its work-start power budget
for admitted equipment. Overlapping equipment has **one calculation**, not stacked reductions or
multipliers. LightsOut governs other admitted devices and is the ordinary fallback when budgets
are disabled; failure of a required combined capability follows the existing documented native
fallback. No new duplicate power controller is added by this request.

The distinction is supported by the [RePowered author's description](https://github.com/emipa606/TurnOnOffRePowered)
and [LightsOut description](https://steamcommunity.com/sharedfiles/filedetails/?id=2584269293).
Their existing balance/admission/native verification obligations remain open. S059
[Best Mix](../replacements/best-mix.md) also retains its existing separate ingredient-preference scope.

### Remaining source-specific cautions for refinement

- Linkable Descriptions describes facility build controls; Xenotype Descriptions edits text. Neither
  is interchangeable with a generic extra-stats feature.
- Meal Ingredient Filter is a **bill stock counter**; source ignores meals without ingredient data
  and excludes conflicting recipe counters. Preserve that distinction from cooking ingredient choice
  and from S024's independently specified meal stacking/provenance behavior.
- Insects source leaves Odyssey lairs unchanged and advertises VFE Insectoids 2 integration; inspect
  exact provider behavior before claiming support. A source claim is not our compatibility evidence.
- Uniform Growing source reports inaccurate affected-cell counts; investigate and compose the owned
  S016 preview instead of inheriting a known discrepancy. Shape admission and fertility filtering
  must agree on the cells ultimately designated.
- DragSelect preserves each native control's meaning; later composition must resolve drag gestures
  against quantity shortcuts, Work priorities and Schedule paste without double actions.
- Clothing Sorter source CE options do not reopen initial CE deferral. Relevant Stats becomes an
  owned source now; shared stat contributions should compose directly, not through an imitation of
  the original's reflected power API or duplicate storage/power lines.

## Follow-through and status

Revision 23 updates the queue, replacement index, primary chunk allocations, affected handoff gates
and final 152-entry reconciliation. No new part/chunk is required: 27 chunks across nine parts stay.
The twelve unspecific sources are assigned research obligations, not runnable implementation leases.
Each needs its own source analysis, proposal/design/observable specs/tasks and coverage report before
code. Preserve every existing feature and accepted user decision. Needle's selected owned-search
draft and documentation checks do not resume the paused S024/runtime work or close a gameplay gate.

Documentation verification: independent review found only a stale total/pause statement in the mod
README; that entry point is reconciled. Queue, primary allocation and active handoff lists each
contain the same 152 unique IDs. All twelve relative links in the seven new Markdown files resolve.
Strict typed OpenSpec validation passed **169 items, zero failures**. Evidence is retained under
`artifacts/Refinement/2026-11-14-intake/`; no runtime tests, install or game launch were needed for
this documentation-only change. Existing paused source edits remain a separate uncommitted batch.
