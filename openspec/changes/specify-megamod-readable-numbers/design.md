## Context

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**, S034.
Specification only. [Research](../../../mods/megamod/docs/refinement/2026-11-03-readable-numbers-research.md)
pins all 26 source files, metadata, selected DLL, current UI interface and consumed RimHUD behavior.
Source intercepts backend text measurement/drawing, adds contextual patches and caches text. That
breadth must be specified and guarded; blindly replacing the backend and calling PatchAll is unsuitable.

## Goals / Non-Goals

**Goals:** readable quantities across native/owned and admitted modded UI, correct presentation/
measurement, protected inputs/names/date/debug data, one common configuration/formatter and explicit
fallbacks. Preserve source grouping/punctuation functionality with documented correctness fixes.

**Non-goals:** runtime implementation in this change, altered numeric values or precision, copied
GPL source/DLL/art, universal renderer compatibility, automatic source migration, external package
impersonation or ordinary cache tuning controls. This feature owns no Thing candidate index.

## Decisions

### Separate quantity meaning from final text

One owned formatter consumes an identified quantity representation plus effective preferences and
returns presentation, preserving digit/sign/precision/unit information. Owned consumers call it
directly, including future RimHUD S127 and relevant search/material/bill displays. Do not format
raw saved names, internal IDs or numeric values used for computation. Generic display integration
can handle unambiguous standalone quantities; native/context adapters establish quantity-versus-name,
unit and already-formatted provenance where raw strings alone cannot. Do not create an allowlist of
only a few easy screens and claim the broad shared-display requirement complete.

Interpret the raw representation once. A formatted `1.000` may mean one thousand, while native raw
`1.000` may mean one with three decimals; raw text alone cannot decide. Preserve provenance through
measure/draw and adapters, and leave unknown foreign representations untouched. Repeated formatting
must not guess an inverse numeric parse. Rich-text handling treats known visible quantity segments
as one value while preserving tag/attribute boundaries; `<size=...>` is not display quantity data.
Do not solve every arbitrary provider syntax with speculative parsing; support only specified forms.

Use plain character manipulation for accepted decimal sequences so no float parse/rounding loses
digits or precision. Define number-level eligibility before grouping. Source global-label prefilter
is only an optimization and cannot determine another token's threshold. Exclusions use correct
literal ordinal substring semantics over original visible text, not the source overlap matcher.
Settings validate full punctuation pairs and exact exclusion lists before committing. These are
owned corrections; do not silently inherit the source's permissive custom character failures.

Implementation clarification, 2026-11-07: the observable "one character" punctuation rule is
interpreted as one Unicode scalar, not one UTF-16 code unit. This inferred assumption is now explicit
in the capability: supplementary punctuation/symbols are eligible, while multi-scalar sequences,
unpaired surrogates, control/format and combining-only marks are not. Ordinary space is the only
accepted invisible group mark and becomes nonbreaking. The complete preset plus dormant custom pair
remains one validated setting; a change to a dormant pair is still validated even when a preset is active.

### Admit native display paths as coherent groups

Prefer appropriate native/owned quantity surfaces and narrowly guarded shared text hooks over an
eager replacement class implementing a volatile backend interface. The reported 1.7 IGuiBackend
addition can cause type loading to fail before a startup validator runs. Any version-specific
adapter must stay isolated until shape admission; never let an unsupported backend prevent the
rest of the product from loading. If a backend wrapper is ultimately necessary, document its actual
minimal forwarding contract and construction/teardown safety before adopting it.

Measurement, drawing, contextual exclusions, truncation and input/caret behavior are coupled where
they share a surface. Validate those prerequisites together, install atomically and roll back only
new owned hooks on failure. All hooks need stable-ID target documents/executable checks. A string
argument prefix needs proof that the target uses that argument for the relevant measure/draw, not
merely the method's presence. History timestamp protection needs the actual date-label context,
not the first arbitrary Widgets.Label call. Quest translation/measurement replacements need exact
operation semantics/cardinality, including changed incoming transpiler streams and later repatching.
Tolerate unrelated code and compatible foreign patches; no hash/version-only or owner-absence guards.

Use scoped/reentrant context restoration in guaranteed cleanup instead of a shared true/false flag
that nested callers overwrite. Local failure cannot leak disabled formatting into later UI.
Formatting failure returns the original full label. Required protection failure leaves the dependent
generic path inactive; a native fallback remains usable. Optional provider failure isolates its
adapter when possible; otherwise suppress the unsafe generic interception while safe direct owned
quantity formatting continues. Emit bounded per-skipped-enabled-patch warnings with spec/target/
predicate/behavior/fallback. Do not silently fall back to a partially protected global renderer.

### Common configuration and text cache ownership

Use the foundation common definitions/load/store/resolve/UI mechanism. Section order: Enable,
punctuation/custom pair and previews, grouping, threshold, small-decimal option, exclusions.
Punctuation/custom pair is a validated composite value to avoid half-valid inherited combinations;
the stored dormant custom pair survives preset changes. Each setting and the exclusion list has
the specified global/save inheritance; saving a list override replaces rather than merges globals.
Global Restore defaults resets all global values, including grouping omitted by the source reset,
without touching save overrides/flags. Current-save Use global for all enables inheritance on every
row and preserves dormant overrides; its effective values follow current globals, not necessarily
built-ins. Both actions invalidate presentation appropriately. No main-menu mutation can address a previous save.

A single bounded text-result cache can serve consumers if measurements justify it. It is not the
shared Thing index and cannot store authoritative stock/cargo facts or Verse references. Key by
original representation, semantic/protection context, effective format/exclusion revision and
language/representation metadata as needed; never original string alone across settings/scopes.
Invalidate text and measured-width caches together on changes, load/save switch and language changes.
Bound both entries and text bytes; a tentative 25,000-entry ceiling is a source-derived design input,
not a proven optimum or gameplay setting. Avoid fixed short-sized buffers whose transformed output
can overflow. Processing must remain correct without any cached answer.

Source cache enable/capacity knobs are omitted from ordinary settings. Developer diagnostics may
inspect/clear or trace formatting through top debug controls, default off and without routine log
spam. No new bottom tab, bottom-right gameplay toggle or ordinary diagnostics settings are needed.

### Optional provider and selected-source composition

- **RimHUD** Jaxe.RimHUD: consumed ToDecimalString(int,int) separates styled integer/remainder.
  Preserve its Theme.ShowDecimals decision and remainder semantics. Formatting must respect Enable,
  small-decimal rules and exclusions; blanket Replace('.') is incorrect. Owned S127 uses direct calls.
- **Toggleable Readouts** owlchemist.toggleablereadouts: source changes cached valueLabel at ctor/
  Update. Before adopting any adapter, preserve raw representation so preference changes/disable
  can refresh existing rows correctly; never repeatedly transform an already-mutated cache.
- **Dawn of a New Day** alendio.dawnofanewday: preserve date overlay through actual current draw
  contract. Provider is not currently installed; source lookup is not acceptance.
- **Dubs Performance Analyzer** Dubwise.DubsPerformanceAnalyzer.steam: diagnostic text/profiler
  integration is optional. Inspect actual current methods/overloads before supported coverage.
  Source Analyzer.xml is its own profiling registration, not a required owned external type facade.

All deliberately used adapters require canonical package detection plus consumed type/member/semantics
checks and absent-safe loading. Original Readable Numbers API/consumer audits, public utility/flag
facades and drop-in/coexistence work are excluded. Do not spoof its assembly or package.
An active source triggers a bounded startup warning and best-effort suppression through the ordinary
owned formatter-off path, with the same behavior and timing as manually off. Keep stored preferences,
unrelated owned features and the original untouched. Installed-but-inactive sources do not trigger
this rule. No guarantee that only the source formats text or that all earlier effects are undone follows.

## Risks / Trade-offs

- **Ambiguous text damages meaning** → preserve unknown representations, expose exclusions and
  inspect semantic contexts; no digits extracted from identifiers to maximize a superficial count.
- **Broader strings no longer fit** → one measurement/display result, native truncation/full details,
  focused quest/map/HUD verification across language and scale.
- **Beta backend shape changes** → guard isolated adapters before loading volatile shapes, atomic
  coherent groups, native fallback and retained warnings.
- **Shared caches retain wrong scope or grow** → revision/context keys, entry/byte bounds, cleanup
  and total draw/update/memory measurements, not a cache-hit-only benchmark.

## Migration Plan

No source-settings/save migration or deployment in this change. Owned missing entries inherit global
defaults; owned override configuration is intentionally save-associated unlike the source. No game
quantities or names are rewritten. Any future migration needs explicit backup/atomicity/restoration/
postcondition contracts before action. Disabling formatting preserves preferences and raw game state.

## Verification plan and open questions

RN-Q1–4 retain actual semantic targets, deliberately used optional display integrations, edge/performance evidence
and implementation acceptance. Pure formatting, threshold, exclusions, invalid config, context restoration
and scope resolution fit ordinary Unit tests; explicitly owned hook cases belong in Harmony with cleanup.
Loaded UI/translation/provider/render behavior belongs in fresh isolated native runs. Tests must demonstrate
multiple quantities in one label, same-string/different-context ambiguity, repeated draw, markup/HUD
fractions, overlapping/escaped exclusions, long expansion, valid resets and game switches.
After independent runtime review, native actions must open quest rewards/stats/map labels/settings,
edit/copy real fields, inspect dates/history/save lists and admitted provider displays. Personally
inspect before/action/after screenshots, including seven languages/scales and disabled/used-integration
cases. Original-loaded checks cover only warning/ordinary-off behavior and retained preferences, not
coexistence support. Record actual text/layout and unchanged underlying values. No live acceptance from this prose.
