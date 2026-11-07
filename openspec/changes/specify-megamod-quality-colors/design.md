## Context

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, target **1.7 beta**.
S131 is a documentation-only replacement draft. [Research](../../../mods/megamod/docs/refinement/2026-11-06-quality-colors-research.md)
pins the complete installed DLL, native beta contracts and two actual consumers. The author page
was read first; public source could not be pinned because retrieval failed. No runtime acceptance.

## Goals / Non-Goals

**Goals:** preserve useful source color coverage and six palettes, share owned palette/settings,
keep status and actions authoritative, correct formatting/localization/invalidation defects, guard
every runtime integration and expose honest omissions.

**Non-goals:** change item quality/statistics, invent a second inventory UI here, import foreign
settings automatically, copy provider caches/GUI implementation, claim universal compatibility,
repair every rich-text user globally or add speculative facades for all public source helpers.

## Decisions

### Semantic presentation rather than text guessing

Query current native `QualityUtility.TryGetQuality` semantics, including MinifiedThing.InnerThing.
No map scan or static Thing→quality cache: source's negative whole-Def cache and stale positive
cache are unnecessary for this cheap authoritative read. Owned consumers call the palette resolver
directly. If shared item facts already exist for a concrete consumer, use them only with freshness
proof and recheck current presentation values. No independent Ctrl+F/haul index is created here.

Keep localized label components/spans and their quality identity together at the relevant display
boundary. Do not search for lowercase English enum names, use substring→color dictionaries, change
unrelated repeated words or rewrite every global label just to color one surface. Preserve provider
status/annotation spans and native plain text for comparison, sorting and search. Generic quality
label decoration must be admitted with every downstream shortening path it affects; otherwise use
narrow display hooks and document any unavailable surface. Unknown/ambiguous provider formatting
leaves its result unchanged rather than guessing which text to replace.

Use one owned span-aware shortening facility at admitted display boundaries. Preserve occurrence
identity, width/font/language, valid tags, neutral full tooltip and outer draw state. Source globally
patches both GenText.Truncate overloads; that broad stripping/rebuilding strategy is rejected.
The selected beta string overload truncates raw characters, and TaggedString is not sufficient
proof of safe colored truncation either. No nested prefix/postfix shared mutable state dictionary.

Source sets GUI.color in MainTabWindow_Inspect.GetLabel's postfix without lexical restoration.
Own the actual title draw scope and restore GUI/text state in guaranteed cleanup. The source's
same-quality multi-selection branch is unreachable in this beta because SingleSelectedThing is
null for multiple objects; preserve single-item scope instead of promising inferred multi-coloring.

### Common settings and palette transactions

Stable Interface / Quality colors section: one seven-entry RGBA palette collection setting and one
Full inspect title boolean. Collection-level inheritance prevents mixed global/save preset state;
inside each scoped editor seven clearly labeled controls remain individually editable. Built-in
presets are immutable data copied into a working palette. Preserve exact source normalized preset
values; channel UI conversion must not silently rewrite untouched float values on every redraw.
Eight-bit editing rounds once on commit; accept dot or active-locale decimal separator unambiguously.
Reject invalid/nonfinite/out-of-range values and ambiguous separators; expose validation rather than
source's silent minus removal or stale input mismatch. All representations use one color model.

Fix wheel/slider/preview agreement: source wheel uses HSV at value1, interactions interpret HSL,
gradient has a saturation-zero special range, and slider/selector clamp lightness to50. Owned
controls use a coherent HSL model with full0–100 lightness and consistent RGBA on every admitted
surface, unlike RGB-only source markup. Neutral labels/tooltips remain readable at zero opacity.

Use the foundation resolver/load/store/UI path. Save edits need game identity and captured setting
scope/version checks at commit. Retain previous complete value, commit/validate all seven entries
atomically, restore/verify on failure. Disable Use global only without a loaded save; inheritance
disables the save-value editor, not the checkbox that leaves inheritance. Enforce editor and
main-menu restrictions in callbacks too; global editing remains independent.
Common file persistence remains responsible for durable atomic write/backup; never write provider
files. Loading malformed entries repairs only those colors and logs a bounded diagnostic. No
provider `colors`/`fullLabel` key impersonation or silent old-save import. Seven languages.

### Small, bounded presentation caching only if measured

Prefer inexpensive live quality lookup plus palette lookup. Source preallocates large caches,
retains Things statically and evicts only every100 simulation ticks; do not copy it. A demonstrated
render-formatting cache must include content spans, palette revision, language, font/width and
relevant context, bound insertions even while paused, and clear on game teardown. Never mutate a
cached color-map value during rendering. Avoid clearing foreign/native global dictionaries just
to make our palette refresh; own presentation invalidation and supported narrow cache integration.
Benchmark before claiming a speed improvement. Source CSV/cache helper API has no discovered UI
caller in the selected assembly and is not a selected user feature.

### Actual external contracts and deliberate limits

| Consumer | Inspected contract | Decision |
| --- | --- | --- |
| Dynamic Trade Interface, zeracronius.dynamictradeinterface,3020706506 | Package check DawnsGlow.qualcolor; reverse-patches QualityColors.QualityColorsMod:AddColors into GetColor(Transferable,Rect,ref Color); ColumnCaption/ColumnQuality cache values on PostOpen | Selected source1.7 has global QualityColorsMod and no AddColors, so binding already drifts. Do not spoof package or activate consumer's reverse patch. Native display support does not automatically cover custom columns. A future narrow consumer bridge needs its own observable delta and live evidence |
| Nice Inventory Tab, Andromeda.NiceInventoryTab,3609897594 | Finds exact package dawnsglow.qualcolor and assembly QualityColors; manual Match button reads global QualityColorsMod.Settings, QualityColors.ColorSettings.Colors as Dictionary<QualityCategory,Color>, calls Settings.SetQualityColor for each entry | Existing button copies values to consumer settings, not continuous synchronization. Megamod does not satisfy package/assembly discovery. No fake loaded pack or global mutable facade; a future explicit scoped export/adapter requires preview, atomicity and persistence contract |
| S132 owned inventory | Separate source UI with its own palette and picker, not yet deeply analyzed at this checkpoint | Quality palette resolves directly from S131, no patches to owned UI; retain distinct condition/status settings in S132 |

These concrete gaps are recorded, not silently called drop-in compatibility. A broad facade would
not repair source-package discovery and would create an unnecessary mutable second settings model.
QC-Q3 retains consumer bridge work; this draft deliberately supplies no new external bridge.
Original provider active: withhold owned overlap and any not-yet-selected owned-to-provider palette
bridge, retain foreign behavior/settings. Other unaffected megamod features remain usable.

### Target specifications and atomic groups

Before implementing any hook, create its stable indexed target document, executable validator,
tests and fallback. Candidate boundaries below are design leads, not assertions that hooks are installed:

| Candidate boundary | Minimum required semantics | Failure group |
| --- | --- | --- |
| QualityUtility.GetLabel/GetLabelShort or narrower display callers | Quality argument and corresponding returned localized text; preserve non-display consumers and downstream formatting | Affected label surface plus required shortening support |
| GenLabel.LabelExtras / item-info and mouseover callers | Item quality/HP/taint assembly, correct operand origin and only intended annotation spans | Item annotations; no removal of unrelated provider extras |
| CompQuality.CompInspectStringExtra / apparel inspect | Returned quality description and taint preservation; no duplicate base execution | Inspection description only |
| TransferableUIUtility.DrawTransferableInfo | labelColor parameter reaches the intended name draw with neutral/status decision intact; valid Thing/non-Thing/empty branches and tooltip scope | Transfer display plus its tooltip/shortening support |
| ITab_Pawn_Gear.DrawThingRow | Actual item argument, label composition with forced/locked flags, intended label/truncation call and unchanged action flow | Gear label; never broad match every Widgets.Label |
| Inspect title draw | Correct selected item identity, exactly one selected quality-bearing item, title draw and restorable state | Optional full-title surface |
| Settings/language/save display lifecycle | Exact observed callback identity and relevant state availability; original execution only if required | Affected invalidation integration |

Check semantic data flow/cardinality in effective code and each actual incoming transpiler stream,
not hashes, exact text/local numbers or absence of foreign owners. Skip-original/order/later
repatch must preserve required behavior or invalidate the affected group. Do not execute foreign
transpilers speculatively. Every failed enabled patch logs bounded patch/spec/target/reason/behavior/
fallback at startup. Installer catches predicate and installation errors, rolls back only newly
added owned patches in an inseparable group and never unpatches other owners. Optional Full title
off is not a failed candidate. Retain common settings even if a surface cannot be colored, with
truthful availability information. No unguarded PatchAll or broad global text repair.

## Risks / Trade-offs

- Shared generic labels reach unanticipated callers → admit complete rendering paths, preserve
  plain text semantics and use narrower hooks when required; no universal compatibility claim.
- Palette alpha can hide text → consistent opacity plus neutral labels/tooltips and readable status;
  no hidden alpha clamp or claim that the Colorblind preset suits every player.
- Third-party consumer discovery cannot be reproduced honestly → document unsupported bindings,
  retain underlying displays and specify future adapters separately when selected.
- Six presets and seven categories versus large custom layouts → all controls scroll/fit and show current
  scope; seven languages and UI scales need native observation, not only conversion unit tests.
- Beta/foreign methods can change after startup → semantic admission/revalidation with independent
  groups; exact package hashes identify evidence, never runtime eligibility.

## Migration Plan

No save-game item data changes. First use starts with Default and Full title off; missing save values
inherit global. No automatic source settings migration. Disable/remove only owned rendering hooks
to restore underlying presentation. Normal item data, foreign settings and provider assemblies
remain untouched. No installation, migration or rollback was performed during specification.

## Open Questions

QC-Q1 inventory resolved; QC-Q2 exact beta display targets/formatting and native evidence,
QC-Q3 consumer bridges/source retrieval/license, QC-Q4 shared S132 palette integration and
QC-Q5 language/opacity/UI/performance evidence remain in the refinement ledger. These are future
research/acceptance gates, not questions asking the user to explain source behavior.
