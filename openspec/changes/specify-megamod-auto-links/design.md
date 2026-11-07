## Context

Sole owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
This is a specification-only design. See [proposal](proposal.md),
[observable contract](specs/megamod-description-links/spec.md) and
[research](../../../mods/megamod/docs/refinement/2026-11-04-auto-links-research.md).
Source modifies Def.descriptionHyperlinks once during static startup. Current native descriptions
and the inspected Info Card Plus consumer both expose links through StatDrawEntry.GetHyperlinks.

## Goals / Non-Goals

Goals: all selected source relation families, explicit terrain/recipe-user/current-cost corrections,
native information navigation, additive preservation and a reusable current recipe/content relation
dataset. Keep unknown content isolated and source/provider boundaries explicit.

Non-goals: change recipes/costs/research, enumerate optional Stuff, infer special products, reveal
hidden objects, issue jobs/bills, replace the information UI, spoof source identity, add per-save
settings or build a second map Thing index. Raw source list mutation is not a proven external API.

## Decisions

### Derive information after content resolution without rewriting definitions

Build a small immutable adjacency dataset from currently finalized Def references. Relation families
are recipe outputs/research/fixed ingredients/anatomy, non-pawn recipe users, object/terrain fixed
costs/research, hatching and one-way nonhuman corpse references. Use actual registered identities,
not labels or nearby content guesses. Discard malformed/null/removed endpoints and nonpositive
costs/outputs for owned additions. Commit each valid two-way edge together. Source iteration order
is not an external contract; keep existing entry order, append localized-label-sorted owned additions
with type/defName tie breakers. No new relation UI headings or translation burden beyond native text
is necessary. Any new player-facing explanation uses all seven baseline languages; technical
log-only diagnostics and identifiers remain invariant under the repository localization rules.

Recipe users are the union of RecipeDef.recipeUsers and ThingDef.recipes, excluding pawn races.
Current ThingDef.AllRecipes demonstrates that native union, but its cache must not be mistaken for
post-refresh truth. Share the finalized recipe/user table already needed by S010/S012, not a second
source-specific scan. Fixed ingredient means IngredientCount.filter.AllowedDefCount==1 in the recipe
default; a bill restriction never changes the graph. Preserve all declared recipe research fields,
products and fixed anatomy. SpecialProducts is intentionally not an inferred-output engine.

Enumerate both ThingDef and TerrainDef for inherited BuildableDef research and current CostList;
the latter can vary with costListForDifficulty. Source used only raw ThingDef.costList. Category-based
costStuffCount remains absent from automatic links. Hatcher endpoints use hatcherPawn.race, not the
PawnKindDef or an individual pawn. Corpse links remain corpse→nonhuman race only. Existing native
links such as generated recipe products are authoritative and do not become duplicated owned data.

**XML-first decision:** this change does not create/modify declarative Defs. Its output is transient
information derived from finalized/generated recipes, current difficulty costs, surviving content
selection and instance/provider description entries. Load-time XML insertion cannot supply that
view's current rules or preserve instance-only hyperlinks. Do not reproduce the source's permanent
post-load mutation of Def.descriptionHyperlinks. Use C# for this computed presentation and a narrow
Harmony native boundary. No XML Extensions operations or new XML dependency are selected; adding
declarative content later would require its own guarded XML decision, not silent runtime mutation.

### Enrich the native description entry once

Preferred narrow seams: postfix the exact StatsReportUtility.DescriptionEntry(Def) and
DescriptionEntry(Thing) overloads. Preserve the returned StatDrawEntry identity, text, category,
priority, optional request, overridesHideStats and all unrelated metadata. A validated adapter can
wrap only its private hyperlinks enumerable with an additive owned sequence; GetHyperlinks consumes
that field first on the inspected native build. No full entry reconstruction, translated-label
matching, global static entry registry or replacement DrawStatsWorker is needed.

Use the actual receiver Def or Thing.def only for additional generic relationships, while preserving
the original enumerable (including Thing/Pawn overrides). Do not replace Thing.DescriptionHyperlinks
with Def.descriptionHyperlinks: current Pawn adds its non-custom xenotype. Existing contextual
Hyperlink fields can include Thing, Stuff, faction/title, quest, ideology, world object and selection
context; preserve the whole native value. A generic added Def link is not equivalent to every
same-label or same-Def contextual link. Deduplicate only equivalent destinations and never delete
the original collection or its duplicates. The wrapper must be idempotent for the same entry and
must not accumulate nested wrappers on repeated callbacks.

The native GetHyperlinks contract must be verified, not assumed merely because a private field exists.
If a foreign prefix returns a compatible description entry, original execution need not be required;
validate the result and consumed semantics. If its result no longer carries an ordinary description
contract, preserve it and withhold the affected augmentation. Do not execute foreign iterators just
to probe effects, swallow foreign exceptions, or treat an unreadable link stream as an empty one.
Unknown context keeps the original information. Owned additions carry a dataset generation so a
stale entry can stop yielding them after invalidation without altering foreign links.

Native drawing uses the description's scroll area, native labels/icons and Widgets.HyperlinkWithIcon.
Keep native navigation, hidden-item check and history. Generic Def research links remain generic
info cards: do not substitute the ResearchProjectDef-specialized constructor that selects the
research tab. Native default Stuff behavior remains a type-level view. Preserve S010/S012's return
context only while it remains valid. S010/S012 retain authority over changed availability/filtering,
comparison invalidation and bench/map/save resets; following or closing information cards cannot
restore a cleared choice or silently choose another. No independent browser-selection mutation or
physical item search occurs as a side effect.

### Dataset lifecycle and bounded cost

Own this derived dataset alongside shared recipe/content knowledge, not map-instance caches. Key
adjacency by actual finalized content identity/generation plus applicable cost-policy epoch. Construct
edges once per relevant generation, not on each repaint. Reuse forward/reverse results across native
cards and browser consumers; no duplicated all-recipe reverse scans. Localized sorting depends on
language/label generation; current labels remain native rather than saved in a user's game.

Invalidate after supported finalized Def rebuild/hot reload, recipe-user/ingredient/product/research
changes, content-selection application and cost/difficulty changes. Save/game switching invalidates
context-dependent costs; language changes invalidate label order. Rebuild atomically before publishing
the new generation, while unaffected ordinary info remains. Membership and generation are rechecked
at use so a stale owned destination cannot be activated after content removal. Reopening/refreshing
the card must replace stale owned view data, not keep a working link merely because its Def object
still exists in memory. Precise native callback admission and refresh/action mechanics remain AL-Q1.

S038 preserves finalized Def identity and existing goods; its applied availability policy is not
Def removal. Retain descriptive relationships for still-registered excluded content, without making
its recipes available or altering policy. Actual removed/unregistered content is different and loses
owned endpoints. Pending S038 settings do not prematurely change the applied gameplay policy; its
normal load-boundary publication remains authoritative. Shared data must retain these distinctions.

Do not periodically scan map Things or serialize Def references/link lists into saves. All Verse
access stays on the game thread. Retire old graph references on content/game teardown. Measure total
edge count, build cost, duplicate suppression, memory and repeated large-card drawing with the
combined consumers. Native drawing can remain costly for high fan-out; there is no arbitrary truncation
or accepted performance guarantee. Optimize only after measuring the real rendered workload.

### Concrete consumer and compatibility admission

Info Card Plus (`kaamalauppias.infocardplus`, Workshop3776660810) is an optional native-contract
consumer. Its DrawStatsWorker prefix reads native cachedDrawEntries, passes them to StatsPanel.Draw
and skips only native drawing. Its description pane calls GetHyperlinks even when the description
body is hidden. Its Setup postfix changes forcePause according to its setting, not link definitions.
Thus augmenting native description entries can serve both layouts without patching the provider.
The inspected source tree has no descriptionHyperlinks mutation despite its loadBefore comment;
the comment alone is not evidence that its setup clears links. Compiled relevant seams were checked.
Validate effective optional behavior on the installed selected build before claiming support.

Candidate target groups below require stable-ID target documents and executable predicates before
implementation. They are design seams, not approved patches or evidence of runtime compatibility.

| Candidate integration | Minimal target semantics / failure |
| --- | --- |
| DescriptionEntry(Def)/(Thing) | Exact argument/return shape and supported description result; preserve existing text/context, append to the actual consumed links. A compatible foreign prefix/postfix is allowed. |
| StatDrawEntry link adapter | Field type plus effective GetHyperlinks use/provenance; required data-flow can no longer be bypassed by an unsupported replacement. Do not accept field existence alone. |
| Content and cost refresh | Callback observes completed content/cost change, not an incomplete loading phase; matching generation reaches view refresh and prevents stale owned navigation. Missing required refresh disables affected dynamic family. |
| Native hidden/action path | Existing Widgets/Hyperlink activation still enforces hidden-target behavior and native history; no alternate unguarded click path. |
| Info Card Plus | Active canonical package and effective native entry/GetHyperlinks contract; absent is dormant, incompatible loses only that supported enhancement with bounded explanation. |

One atomic description integration requires both Def and Thing routes plus safe merge/retirement
support. Individual relationship families may fail independently only when no half-published edge
remains. Validate semantic consumed snippets and actual effective prefixes/transpilers where needed,
not hashes, full-method text or absence of other owners. Any actual transpiler validates its incoming
stream on every run and returns it unchanged on failure; no speculative foreign-transpiler execution.
Support later repatch invalidation/revalidation, remove only newly installed owned patches on failed
atomic installation, and retire owned transient additions without removing native/provider data.
Log one bounded startup warning per skipped enabled patch naming package/patch/spec/target, reason,
unavailable behavior and fallback. Aggregate malformed endpoint diagnostics by relation family, not
by frame or every repeated card opening. Explicit source-authority disablement is not a failed patch.

When automatic.autolinks is active, the entire owned feature, including terrain/user-list corrections,
stays off. It remains the sole provider, without undoing its links or imposing a package alias.
Raw Def.descriptionHyperlinks mutation is intentionally not preserved as an API for unknown readers;
the demonstrated description-entry consumer remains supported. A concrete new consumer must be
researched before broadening the adapter or changing this documented compatibility boundary.

## Risks / Trade-offs

- Late/foreign information replacement can bypass normal entries → validate actual consumer semantics,
  preserve its original view and report unsupported integration rather than adding a competing UI.
- Source raw-list readers would not see owned dynamic links → documented interface deviation; no
  such callable dependency was established by the bounded source search. Investigate real reports.
- Large reverse-link lists can be expensive even with a shared graph → native workflow measurements;
  keep every valid link accessible and avoid a hidden cap or speculative general framework.
- Runtime content refresh can leave old windows alive → generation-aware additions and activation
  revalidation, tested with content removed while an old card is open. No stale target resurrection.
- Terrain and current-cost extensions go beyond the binary → explicit observable requirements,
  no new recipe availability or balance effects. Optional material expansion remains omitted.

## Migration Plan

Documentation only now. Future feature has no saved orders/settings to migrate; existing native
history remains the game's responsibility. Missing optional content contributes no relation, and
source provider presence suppresses new owned contributions. Disabling/removing owned runtime hooks
must retire their transient data while preserving normal cards. No raw-list/save importer is selected.

## Open Questions

AL-Q1–4 in refinement retain exact effective adapter/refresh/action admission, proposed relationship
coverage and presentation, actual optional/raw-list consumers, and implementation/performance/native
verification. Record new findings there instead of asking the user to explain source behavior.
