## Context

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target1.7 beta.
S113 is documentation only. Read [research](../../../mods/megamod/docs/refinement/2026-11-05-scenario-searchbars-research.md)
for the complete installed1.6 binary inventory, original/current Workshop evidence and native
1.7.5000rev1275 comparison. This is independent implementation planning, not a source-code port.

The source has ten Harmony patches and two helper/startup classes. Its candidate-method postfixes
also affect Randomize and random-pet generation; its Dictionary keys use scenario parts whose hash
codes change with their edited values. Its regex constructor is outside the fallback try block.
These are static code findings, not reproduced runtime failures. Preserve the convenience, not
these incidental behaviors. Source identity hashes establish evidence only, never admission.

## Goals / Non-Goals

**Goals:** local choice-list filtering, retained native callbacks and eligibility, explicit safe
regex, stable transient identity, useful fallback and compatibility with supported native extensions.

**Non-goals:** a replacement scenario editor, new scenario content or balancing, universal foreign
editor support, search in every selector, migrated queries, a global mod setting or map cache.

## Decisions

### Filter at the editor's choice-list boundary

Use current effective native candidate generation, labels and callbacks. Filter only the menu being
built for the specific part/selector. Do not globally postfix PossibleThingDefs, PossibleAnimals or
NonRedundantResearchProjects as the source does: these are also used for non-UI randomization and
generation. Prefer a narrow menu-boundary adapter to replacing complete DoEditInterface methods.
Any scoped capture must unwind in guaranteed cleanup, handle nesting/reentrancy, and never leak an
active filter after an exception. Avoid a generic global FloatMenu filter.

Preserve actual FloatMenuOption identity/behavior, including provider extras and disabled explanations
when present. Recheck the current part and relevant eligibility at selection without reconstructing
or arbitrarily replaying foreign actions. Keep native choice ordering and duplicate labels. Preserve
Random pet as an explicit unfiltered sentinel; no made-up animal Def represents it.

Native ThingCount changes thingDef, default stuff and quality together. Native stat/incident UIs
contain existing number/timing controls; those must survive. Do not copy the source's full stat
prefix or incident prefix that draws its own button and then returns true into the original draw.
Search display labels, including LabelForFullStatListCap for stats, not only Def.label.

### One small editor-local search state

State is owned by the active ScenarioUI/editor session, keyed by reference identity of the actual
ScenPart plus selector identity if necessary. Native ScenPart subclasses override GetHashCode using
mutable content, so ordinary value hashing is unsuitable. The state includes text, explicit regex
mode and current validation result. Remove entries for removed parts; clear on scenario replacement,
editor exit and start-game handoff. Do not serialize it, alter scenario hashing or touch ModSettings.

The native main-menu scenario editor is an appropriate contextual surface. It is neither live-game
cheat UI nor mod configuration. No Use-global exception implementation is needed because there is
no setting. Native scenario choices retain their own persistence.

### Safe search without another data subsystem

Reuse an owned text-search utility directly where its contract matches. Literal matching defaults
to case-insensitive substring on current display labels. Regex is explicit and case-insensitive;
validate construction and bound total evaluation as well as individual matching. Catch syntax and
timeout failures before they escape draw/input. Do not silently fall back to literal semantics.
SCS-Q2 resolves concrete measured bounds, localization/culture/IME behavior and runtime support;
record user-visible limits in the spec before implementation.

Cache at most the current query's compiled matcher and a short-lived display snapshot if measured
necessary. Invalidate on query/mode/language/current-selector/provider-candidate changes. No scans of
map Things, duplicate search index, persisted Def references or asynchronous Verse/UI access. Search
must not call randomization or provider action delegates to discover labels/candidates.

### Semantic admission and atomic integration groups

Create indexed per-patch target documents before implementation. The following are investigation
targets, not declarations that name/signature proves compatibility:

| Candidate target | Minimal semantics consumed | Safe failure |
| --- | --- | --- |
| ScenarioUI.DrawScenarioEditInterface and Listing_ScenEdit layout | One current scenario/part identity, native row allocation, active editor lifetime and retained ordinary controls | No owned search on unsafe surface |
| ThingCount.DoEditInterface item menu | Eligible item sequence feeds its displayed menu; callbacks retain native thing/stuff/quality updates; stuff/quality menus distinguishable | Ordinary item selection |
| StartingAnimal.DoEditInterface | Current non-tamer-filtered editing choices, Random pet sentinel, original callback; no changes to RandomPets/PlayerStartingThings | Ordinary animal selection |
| StartingResearch.DoEditInterface | Nonredundant current candidates feed only this menu and original project assignment | Ordinary research selection |
| StatFactor.DoEditInterface | Eligibility checks, full labels, original selection and unchanged factor control | Ordinary stat selection |
| IncidentBase.DoIncidentEditInterface with base/CreateIncident layout | Exactly one button/menu path and assignment; existing timing controls untouched | Ordinary incident selection |
| Editor/scenario lifecycle and stale callbacks | Actual session/part survival and scenario replacement boundaries, without scenario-data mutation | Drop transient state and leave ordinary editor |

Pair search presentation and filtering atomically for each selector family. Do not show a field that
does nothing. Shared lifecycle safety is a prerequisite; its failure disables dependent owned search.
Validate effective incoming data flow, compatible prefixes/transpilers and skip-original behavior;
not exact assemblies, offsets or absence of foreign patches. Every transpiler validates each actual
incoming stream and returns it unchanged on failure. Do not execute foreign transpilers as probes.
Later incompatible repatches invalidate the affected group. Idempotent registration and install
failure cleanup remove only newly added owned patches. Warnings follow the foundation per-patch
format, including the missing player behavior and fallback. Explicitly disabled features are not
failed candidates.

### External contracts and source overlap

Active `nimrag.scenariosearchbar` suppresses owned overlap before registration; never unpatch it.
The source's public Tools methods have no actual external runtime consumer in the bounded installed
search. Do not publish a speculative facade or spoof package/assembly identity. Its only retained
query data is a static dictionary, with no Scribe/settings schema. Native scenario serialization
remains unchanged, so no query migration is selected. A future proven consumer requires its own
contract and tests. Inherited native UI can be supported; overriding candidate or draw behavior
requires actual inspection, not the source page's broad compatibility slogan.

Harmony is required with implementation use; no source DLL is shipped. This is UI code, not a
declarative Def change; no XML operation is selected. XML Extensions becomes required if a later
owned slice actually uses it. No hard DLC or other product-mod dependency; Gateway stays dev-only.

## Risks / Trade-offs

- Explicit regex changes the source's default → retain the capability with a visible mode control,
  document the safer literal default and test punctuation/mode switching.
- Beta and optional UI rewrites alter menus → semantic family admission, native fallback and exact
  provider-present acceptance, without promising support for every custom scenario editor.
- Mutable part hashes or retained global dictionaries lose/leak state → session/reference identity,
  scenario replacement cleanup and tests for real value edits/reordering/removal.
- Whole-method replacement loses future native controls → narrow menu/presentation composition,
  preserve callbacks and personally inspect native workflows on the reviewed build.
- Large lists or expressions stall UI → measure combined matcher/menu costs and enforce bounded
  evaluation; no unsupported performance claim from static inspection.

## Migration Plan

No native scenario/save conversion or source settings migration. Enable only admitted owned editor
enhancements; removal restores native editing and discards transient state. Existing chosen scenario
values remain native. Do not alter active mod lists automatically.

## Open Questions

- **SCS-Q1:** implementation-revision menu/layout/lifecycle semantics, family grouping, effective
  foreign additions/overrides, stale callback checks and later-repatch handling.
- **SCS-Q2:** measured query/list/regex budgets, shared utility fit, language/culture/IME behavior
  and clear layout at supported scales; add any selected visible bounds to the spec first.
- **SCS-Q3:** focused RED/GREEN, actual-provider cases, package checks, seven languages and final
  reviewed-build native editor/save/start-game evidence. Static inspection is not1.7 compatibility.
