# Cherry picker scroll bars replacement coverage

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Queue **S020**, intake **P1-15**, origin **replacement**. **Analyzed; specification drafted.
Nothing implemented or accepted in game.**

Source **Cherry picker scroll bars**, **Lucius**, `lucius.cherryscroll`, Workshop **3781285156**.
[Description](https://steamcommunity.com/sharedfiles/filedetails/?id=3781285156) and
[change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/3781285156) were read
before the installed 1.7 assembly. Version 1.0.0 / assembly 1.0.0.0; declares 1.6 and 1.7. No public
add-on source/license found in the bounded inspection. Its required installed Cherry Picker base
declares only through 1.6; source-present 1.7 compatibility is not verified.

[Research and exact provenance](../refinement/2026-11-02-cherry-picker-scrollbars-research.md) support
the [proposal/design/tasks](../../../../openspec/changes/specify-megamod-content-filter-selectors/)
and [observable contract](../../../../openspec/changes/specify-megamod-content-filter-selectors/specs/megamod-content-filter-selectors/spec.md).

## Coverage and deliberate differences

| Behavior | Source finding | Proposed replacement / status |
| --- | --- | --- |
| Two filter selectors | Replaces type and source FloatMenus in Cherry Picker settings | **Specified:** direct owned content-page selectors, composed with queued S038 |
| Search | Full option label, case-insensitive AND substrings, whitespace terms, stable order | **Specified:** same rule; visible identity distinctions are part of the displayed/searchable label |
| Counts and no matches | Matching/all option counts, explicit empty message | **Specified:** includes All choice; counts choices, not visible content entries |
| Scroll/columns | Resizable row-major grid, vertical scroll, fresh query/scroll on open | **Specified:** all choices reachable at supported UI scales; no persisted window preference |
| Source icons | Display-name lookup, first same-name pack wins, generic icon fallback | **Proposed correction:** exact source identity, neutral missing icons, distinct equal-name rows |
| Filter action | Base compares source by display name; equal names merge | **Proposed correction:** exact source selection; S038 must carry this into the composed filter |
| Type/source/text composition | Each callback changes only its own filter | **Specified:** preserve other filters and allow valid empty intersections |
| Row labels and unavailable actions | Truncated labels; no full-label tooltip; disabled Chosen action rejected but window closes | **Proposed correction:** full-label hover, visible reason and no successful-looking close on rejected choice |
| Close/background input | Close X/outside dismissal and input absorption requested in source | **Specified:** native cancellation, Escape and no click-through content mutation; beta verification pending |
| Attachment | Dynamic external target and exactly-two positional constructor guard, unguarded PatchAll startup | **Not reproduced:** direct composition; any future patch needs its own minimal semantic admission |
| Core Cherry Picker removal/settings/API | Not supplied by this add-on | **Pending S038:** not claimed implemented, specified or contract-compatible here |

## UI, state and dependencies

Use the content section of the mod-settings page. No new bottom tab, bottom-right toggle, debug
override or preference. Search/scroll/size are transient UI state, not global/save settings. Browsing
must not change enabled content or save overrides. S038 owns actual content choices and their scope,
timing and any justified exception to the common settings model. Seven-language native UI verification
remains future work.

The source requires Harmony and Owlchemist.CherryPicker because it patches that provider. The owned
selector implementation itself needs neither external runtime dependency nor any selected patch;
S038 is an owned feature composition dependency. No XML mutation is selected. Future XML Extensions
use requires its declared runtime dependency and guarded merge-preserving operations. No source or
Gateway assemblies are shipped.

## Contracts, conflicts and omissions

The actual consumed external contract is Mod_CherryPicker.DoSettingsWindowContents(Rect), with
MenuOfDefs/MenuOfPacks option callbacks and the two FloatMenu constructions. Source public menu
constructors take List<FloatMenuOption>. The bounded installed literal scan found no external add-on
consumer. This does not establish absence of dynamic consumers or cover Cherry Picker's broader APIs.
No package impersonation, speculative facade, automatic foreign settings migration or global dropdown
replacement is promised.

If the original add-on is active, it remains attached to its original provider; its presence alone
does not suppress our direct controls. S038 must settle coexistence of the underlying content feature
before activating it. These selectors cannot reactivate a withheld content feature. Unsupported
catalogue/choice state preserves selection and explains the failure; future patch failures need
bounded startup warnings and leave content configuration intact.

All implementation tasks are unchecked. SC-Q1–4 retain UX refinements, S038 contracts, beta integration
and final acceptance. Actual proof requires native settings navigation, selection, resize/scroll,
equal-name source results and cancellation over underlying content controls on the reviewed build.
A clean log or a displayed popup alone is insufficient. This slice changes no progression, costs,
yield, research or enabled content merely by browsing.
