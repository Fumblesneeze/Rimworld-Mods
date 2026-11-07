## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S020 replaces **Cherry picker scroll bars**, `lucius.cherryscroll`, Workshop **3781285156**.
[Research](../../../mods/megamod/docs/refinement/2026-11-02-cherry-picker-scrollbars-research.md)
records public documentation, all six installed classes, exact hashes and the two consumed Cherry
Picker callbacks. The add-on declares 1.7; the installed base provider declares only through 1.6.
This mismatch is not evidence of a working or broken 1.7 runtime.

The add-on replaces exactly two FloatMenu constructors, by positional order, inside the external
CherryPicker.Mod_CherryPicker.DoSettingsWindowContents. It passes through original option objects.
The first builds content-type filters; the second builds source filters. Original source selection
and icon lookup both use display names, so equal names are not unambiguous. That source behavior
must not be mistaken for an exact-identity contract simply because the callback object is retained.

## Goals / Non-Goals

**Goals:** searchable, scrollable, responsive selectors; correct identity and cancellation;
contextual composition with S038; no new setting or unnecessary patch; documented source differences.

**Non-goals:** implement Cherry Picker's removal machinery early; redesign its entire catalogue;
replace every game dropdown; add a bottom tab, persistent window preference or external adapter;
claim source API/save compatibility; implement or accept runtime behavior in this documentation slice.

## Decisions

### Owned host and explicit selection identity

S038 will provide the content page and authoritative catalogue. Call a small selector directly from
its type/source buttons, passing stable choice identity, complete display text, current availability,
reason and source icon where relevant. Do not patch our own method or require CherryPickerScrollBars
classes. Keep this code with the content-configuration UI, not a generic menu framework.

Preserve the host's option ordering, with All first. Inspected source type choices are label-sorted
over represented eligible Def types; source choices are label-sorted active content packs, plus All.
The source menu priority sort is stable for those equal-priority options. S038 owns exact catalogue
coverage; this change must not narrow options based on another current page filter. The type choice
changes filteredType only; the source choice changes the source filter only; both combine with the
page text query. There is no content-enable action in either selector callback.

Use actual source identity for filtering, label and icon, with canonical package identity displayed
to distinguish equal names. Do not silently merge equal display names as the source does. If a
malformed duplicate package set cannot be identified safely, represent that ambiguity as unavailable
instead of guessing. This contract constrains S038's future catalogue and filtering implementation;
it is not a general source save/API migration promise. Revalidate against the host catalogue revision
before applying an option; invalidation preserves previous selection, reports why and allows reopen.

### Small shared selector view, transient navigation

Both selectors use the same local renderer/filter rule. Search the complete displayed label,
including identity distinctions, by whitespace-separated AND substrings using ordinal case-insensitive
comparison. No regex, stemming, fuzzy ranking, hidden description search or package-ID search unless
that ID is actually displayed. Cache the filtered list for the current immutable options snapshot
and search text only; rebuild when either changes. Preserve stable option order and count each option,
not the number of matching content entries. All is an ordinary searchable option in that count.

Retain the source's row-major responsive layout, vertical scrolling and fresh opening state. Source
measurements are 340 by 560 initial size, 280 minimum column width, 6 column gap, 16 scrollbar allowance,
32 row height and 24 icon size. These are initial layout references, not fixed player-facing balance
values. Clamp the actual window and minimum usable content area to supported resolutions/UI scales;
full-label hover text and controls must remain accessible. Resize recomputes rows and clamps the scroll
offset. No persistent width, position, query or scroll setting is introduced.

Use host/localized labels and baseline seven-language strings for titles, counts, empty results,
identity explanations and disabled/failure messages. Reuse actual source metadata icons or a neutral
presentation; no bundled Workshop texture or new art is selected by this specification. Do not key
icons by translated names. Scope and restore GUI state so fonts/colors do not leak to the parent.

Source only culls drawing for off-screen rows while still iterating all options. Improve draw traversal
if measured costs warrant it; do not claim O(visible) or a performance gain merely from filtering cache.
This is a small settings catalogue, not map Thing data. Reuse S038's catalogue and the concrete shared
renderer, not the Ctrl+F/hauling map index or a second global registry. Release window references on
close; no off-thread Verse access or saved live object references.

### Selection, disabled state and close ownership

Select one exact available choice through the host's owned command once, then close. Do not duplicate
the command in both a row handler and a window close callback. Search/resize/hover must have no callback
effects. Outside click consumes input while dismissing; the parent checkbox must never receive it.
Use the native window lifecycle for Escape/close, validated on the exact beta. A disabled or stale
choice keeps the previous filters and stays uncommitted with a reason.

The source calls FloatMenuOption.Chosen(false, null), then closes unconditionally. Native 1.7 Chosen
checks Disabled (action == null), so disabled options do not execute their action; the source still
draws them as ordinary rows and closes. Its custom renderer also bypasses FloatMenuOption.DoGUI's
tooltips and other extra rendering. We preserve needed selector behavior, not a claim to render every
possible FloatMenuOption extension. No actual external consumer of the add-on's public menu classes
was found in the bounded installed literal scan.

### Settings scope, coexistence and compatibility admission

The content page belongs in mod settings. Selector navigation is transient UI state, so there is no
Use global row for search/scroll. Actual content enable/disable configuration, any reasoned scope
exception and save-specific main-menu restrictions belong to S038. Do not invent those rules here.
Browsing cannot activate content or modify a save override; changing the page's view filters remains
per-page behavior, with its wider lifetime owned by S038.

The source add-on only targets CherryPicker.Mod_CherryPicker; its presence does not by itself
overlap our direct renderer. Do not suppress ours solely because lucius.cherryscroll is active, or
patch/unpatch its provider. S038 must establish one underlying content-config implementation and
its coexistence policy before our page activates. Do not impersonate Owlchemist.CherryPicker to make
the old add-on attach. No external adapter or binary facade is currently selected. Source S038's
full public contracts/consumers remain a separate required analysis.

**No Harmony/XML patch is selected for this owned UI slice.** Direct calls need no target-spec
document falsely describing an external patch. S038 owns any necessary native entry seams. If later
research shows a patch is needed here, add an observable scenario and a stable target document with
executable admission before code. For an external selector adapter, minimal predicates would need
to prove the correct catalogue and selection callback feeding each specific window construction;
exactly two arbitrary FloatMenu constructors does not prove that semantics. Unrelated third menus
must be allowed where the required two flows remain identifiable. Never use whole-method hashes or
absence of other patch owners as admission criteria.

Any selected future patch must handle effective code/skip-original prefixes, validate actual incoming
transpiler streams on every run, return unchanged on failure and document later repatch invalidation.
Do not rerun foreign transpilers speculatively. Missing/unknown predicates, exceptions or installation
failure roll back only newly installed owned patches in the declared group and warn once per skipped
enabled patch with package/patch/spec/target/reason/unavailable behavior/fallback. This slice never
changes content configuration on failure. Required dependencies must match actual use; XML Extensions
is required on its first operation, with package/Def/node existence checks and merge preservation.

## Risks / Trade-offs

- Parent feature S038 is not specified yet → record this as a composition dependency, not a working
  content manager or a reason to research a second full mod out of sequence.
- Display-name identity correction changes base filtering as well as icons → preserve it as an explicit
  proposed observable rule and carry it into S038, without claiming literal upstream parity.
- Additional columns can move a row under the pointer → resolve selection from the current layout and
  exact identity; test after resize/search/scroll and at UI scales.
- Source supports 1.7 while its installed base metadata stops at 1.6 → inspect and test exact beta
  integration later; do not infer compatibility from a clean assembly decompilation.
- Disabled state/hover/cancellation differ from the source → keep proposed corrections explicit and
  verify actual native UI behavior, including click-through prevention.

## Migration Plan

No runtime migration or content edits occur. Later implement focused selector rules and UI against
S038's owned catalogue when available; enable the host entry only when its own admission/coexistence
contract permits it. Removing this selector enhancement cannot change enabled content or saved
configuration. No source settings import, assembly copying or save conversion is promised.

## Open Questions

SC-Q1 retains the proposed identity/disabled/full-label refinements for later UX review; SC-Q2 the
exact S038 catalogue, scope, ownership and public contracts; SC-Q3 beta window behavior and any
necessary target admission; SC-Q4 seven-language/layout/performance tests and final native acceptance.
Questions stay recorded, not asked during the sequential deep dive.

Semantic pass: these controls change which entries the player sees, not availability, research,
costs, yield, job eligibility or save settings. Accidental enable/disable is the material playability
risk. Final acceptance must select real filters and observe the page, cancel over underlying controls,
and verify unchanged content configuration on the reviewed build; logs alone cannot prove this.
