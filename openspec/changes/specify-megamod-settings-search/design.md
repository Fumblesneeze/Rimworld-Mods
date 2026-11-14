## Context

Sole owner: Make Rimworld Great Again (`fumblesneeze.megamod`, `mods/megamod`), 1.7 beta.
On 2026-11-14 the user explicitly selected **all mods' individual options**, not just megamod or
a supported subset. This supersedes this change's original owned-only draft and S047's exclusion of
foreign control search. See [research](../../../mods/megamod/docs/refinement/2026-11-14-needle-research.md).

## Goals / Non-Goals

**Goals:** find options across all available active-mod settings pages with no package allowlist or
per-mod opt-in; reveal their real page/tab/control; preserve ordinary editing; make discovery gaps
visible. Keep the common settings scope/inheritance rules for owned controls.

**Non-goals:** replace foreign settings stores/editors, change settings during discovery, import
Needle preferences, reproduce its binary APIs, or provide a general Def browser. Universal mod
eligibility is required; invisible conditional branches and unreadable custom rendering cannot be
invented as captured options. Those limits require truthful status and refresh, not a curated scope.

## Decisions

1. **One chooser, two result kinds.** Extend native Options → Mod options with individual-option
   results alongside page matches. S047 owns page identity, author grouping and normal opening;
   S141 owns option discovery/query/reveal. Reuse one query field when both are enabled. With S141
   disabled, S047 retains its existing substring behavior; with S141 enabled, its query syntax
   governs both result kinds, while page cards match only their own metadata. Option hits remain
   visible even when no page title matches. Turning S047 sorting off must not disable S141.
   No second settings framework, gameplay tab or separate foreign editor.
2. **Generic reader, metadata fast path.** Enumerate the complete actual settings-page collection
   and retain independent page instances, including provider-generated pages sharing a title or
   package ID. Every page is eligible without a mod-specific adapter or author registration.
   Observe standard/native GUI text, tooltips, value displays, control geometry, scroll contexts and
   observed tab navigation, as Needle does. Traverse navigational tabs in a bounded session; do not
   click arbitrary buttons or change settings to expose conditional branches. Optional provider
   adapters may improve access to their actual page collection; they cannot be the only way a
   particular package qualifies. Missing provider access is an explicit coverage gap.
   For owned rows use existing registration IDs, labels/help, feature/category and reveal handles;
   composite/dynamic rows expose meaningful subentries through that same registration.
3. **Capture at a valid GUI boundary.** Inspect exact 1.7 contracts before choosing hooks. Draw
   callbacks and observed tab callbacks are permitted for discovery, with neutral input, scoped
   recording and reentrancy guards. Never dispatch editor/reset/apply/write actions. Restore
   capture-owned GUI/event state, selected tab path, scroll and temporary windows in guaranteed
   cleanup. Needle restores GUI state but cannot transactionally undo arbitrary callback effects;
   ours must not promise that either. Detectable mutation or restoration failure aborts that page,
   reports failure and retains ordinary access; native acceptance must demonstrate unchanged
   settings for the exercised readers. Do not swallow an unsafe failure as successful indexing.
4. **Bounded, observable discovery.** Perform initial capture at startup's valid GUI phase when
   enabled, or on first enable/open if not yet read. Schedule bounded work between callbacks with
   progress and cancellation. Pin draw/tab/depth/time budgets from measured layouts before code;
   exhausting one produces partial status, never silent complete coverage. An in-flight third-party
   callback cannot be forcibly preempted on the Unity thread; report measured overruns and stop
   further calls. No indexing loop during ordinary gameplay or capture on every query keystroke.
   Observe normal page draws to refresh changed rows; explicit Refresh retries incomplete pages.
5. **Source-inspired query.** Readable localized text, case-insensitive Unicode substrings,
   normalized whitespace, AND terms, quoted phrases, exclusions (`-term`) and
   `mod:`/`author:`/`id:`/`tab:` filters. Fields restrict the associated term; `mod:` covers
   mod and settings-page names, `id:` the package ID, and other fields their named metadata.
   Unknown field prefixes remain literal text. An unclosed quote consumes the remaining phrase.
   Exclusion-only queries filter all entries. Empty input restores ordinary page browsing without
   dumping all option rows. Label matches rank before help/value-only matches; exact matches precede
   typo fallback. Use deterministic page/path/occurrence ties, not copied source numeric weights.
   Default typo fallback applies only to positive unqualified unquoted words with no exact match,
   length 5–8 at most one edit, 9+ at most two; insertion, deletion, substitution and transposition
   count as edits. Phrases, exclusions and fields remain literal. No `def:` classifier is selected.
6. **Identity and reveal use fresh observations.** Keep package identity plus actual page identity,
   tab path and control occurrence/owned setting ID; labels alone never select a duplicate. Result
   activation opens the real page, follows only observed navigation and resolves the target in the
   current layout before scrolling/highlighting for three real elapsed seconds. No setting edit is
   performed by selection. Missing or ambiguous targets show a reason and open the correct page
   without guessing or clicking a nearby control. User navigation cancels pending reveal.
7. **Values retain their provenance.** Owned results read common requested/effective values and
   current Global/Current save/Use global binding. Foreign values are last-observed display text,
   identified as such until refreshed by a real draw; never deserialize/reflection-read arbitrary
   private settings or label stale values current. Unknown values stay unknown. Opening/editing/
   closing uses the original owner's normal persistence; our scope controls apply only to our own
   options. Do not mirror foreign settings into megamod storage.
8. **Lifecycle and activation.** Query/results/highlight are view-local. Captured index data is
   process-local; language, page collection and main-menu/save/world binding changes invalidate
   affected observations and reveal targets before reuse. Conditional controls refresh when their
   owner renders them; discovery never flips a prerequisite setting. The default-on
   `settingsSearch.enabled` common GlobalAndSave Boolean belongs in Interface/navigation and uses
   NextDisplay. Effective off cancels further capture and restores normal browsing. Later enable
   schedules missing discovery. Active `ZIPender.Needle` takes the standard bounded warning and
   best-effort ordinary effective-off path, preserving stored preferences; no coexistence claim.

## Risks / Trade-offs

Generic GUI observation is the selected approach because foreign mods need no metadata contract.
Custom rendering, inaccessible tabs, conditional rows and nonreturning callbacks remain real limits.
Show per-page pending/partial/failed status and counts; a zero-result query with gaps must not imply
that no mod has that option. Absence of an exception is not proof that every custom control was read.
Unknown rendering should remain unverified. Shape guards apply to generic engine/provider contracts,
not package eligibility lists. Preserve ordinary page access even if the search hooks are unavailable.

The index contains displayed text only and is not serialized to saves or exported to logs. Keep
diagnostics bounded and avoid dumping arbitrary user-entered option values. No installed source
assembly/code/assets may be copied. Measure discovery/query/reveal cost on representative real pages
before performance claims. Host tests own detached matching/identity policy; real draw callbacks and
provider editing require fresh isolated native workflows.

## Migration Plan

The first implementation tranche is detached query behavior only, without runtime registration.
It reuses S047's immutable page descriptor and retains opaque page/path/occurrence identity; labels
and coordinates are never identity. Unicode text uses canonical composition, invariant case matching
and collapsed whitespace; syntactic rich-text tags are removed while literal comparisons remain.
Typo eligibility is determined per positive plain word against the whole supplied snapshot before
field/exclusion filtering: an exact occurrence anywhere suppresses that word's typo fallback.
This preserves the inspected source behavior without its numeric scoring algorithm. Match ranking
uses exact before typo, label matches before other context, then ordinal package/page/path/occurrence
ties. Page records search page metadata only. No callbacks, settings access or persistent index are
part of this tranche; snapshot lifecycle remains the future native adapter's obligation.

Later implementation adds one common enabled
preference and transient discovery state. Missing preference means inherited/default-on; disabling
does not alter foreign settings or require importing Needle data.

## Open Questions

Implementation must pin native/provider page enumeration, GUI/tab/scroll capture and reveal
semantics, budgets, custom-control failure detection and representative real mod fixtures. Those are
implementation research tasks; they cannot be resolved by restricting product scope to those fixtures.
Plan revision28 dispatches the detached query tranche and read-only native-contract research.
