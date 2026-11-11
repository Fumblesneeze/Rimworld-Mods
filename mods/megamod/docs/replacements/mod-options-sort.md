# Mod Options Sort replacement

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: replacement, **S047 / P1-40, P2-30**. Status: **bounded pure chooser projection; review pending**.
No native/provider integration or compatibility acceptance.

## Source and intent

**Mod Options Sort**, Superniquito / superniqui10, `Superniquito.ModOptionsSort`,
[Workshop2910865748](https://steamcommunity.com/sharedfiles/filedetails/?id=2910865748).
Selected1.7 assembly1.0.0.0, Release .NET10, informational revision
`1bab3ab2d11d9bb719348a586fa88340e30d3c00`. Metadata supports1.4–1.7 and requires Harmony.
[Research](../refinement/2026-11-03-mod-options-sort-research.md) records complete three-class
decompilation, source/native/provider behavior, hashes, missing advertised highlights and limitations.
No reuse license or public source was located; independently implement, copy no source code/DLL/art.

Finding a settings page by author should be straightforward without changing who owns the page.
[Proposal](../../../../openspec/changes/specify-megamod-mod-settings-navigation/proposal.md),
[design](../../../../openspec/changes/specify-megamod-mod-settings-navigation/design.md),
[observable contract](../../../../openspec/changes/specify-megamod-mod-settings-navigation/specs/megamod-mod-settings-navigation/spec.md)
and [tasks](../../../../openspec/changes/specify-megamod-mod-settings-navigation/tasks.md) own the work.

## Coverage and differences

| Source behavior | Selected contract | Implemented / deviation / missing |
| --- | --- | --- |
| Existing settings list | MO-01 preserves exact independent page identities | Pure projection retains each supplied page and opaque opening token; actual collection/opening pending |
| Author/name sorting and singles | MO-02 retains source defaults, groups, complete-list counts and Various Authors last | Pure sorting/grouping and stable ties implemented; native controls/session state pending |
| Special group aliases | MO-03 retains VE/Sarg/Helixien conventions and source singleton rule | Pure aliases, retained raw attribution and unknown fallback; positive HugsLib identity must come from a future admitted adapter |
| Search by title/name/author/group | MO-04 preserves substring search and broadens it to original authors hidden by aliases | Pure current-view search implemented; hidden Various Authors does not match in name mode unless another field matches |
| Cards and browse controls | MO-05 retains session-only sort/group choices and fresh-query reset | Nothing; seven-language/readable layout and robust icons instead of hardcoded English/unsafe dereference |
| Native/provider pages | MO-06 preserves each actual owner and normal edit/close persistence | Nothing; admitted optional HugsLib route instead of failed-reflection click crashes |
| Compatibility | MO-07 preserves compatible entries and ordinary fallback | Nothing; semantic atomic admission, owned-only rollback and bounded error isolation |
| Yellow non-default / blue unusual implementation colors | Advertised in installed About, absent from complete selected1.7 code and current public summary | **Not specified as implemented functionality**; MO-Q3 retains explicit gap, no unsafe foreign-default probing or replacement claim |

No configuration rows, own settings page, save component or gameplay toolbar are supplied by this
source. Sort/group choices are transient navigation state and reset at restart; search resets on a
fresh chooser opening. This adds no persisted global-only preference and therefore no inheritance
exception. If persistence is later desired it needs a common-settings scope contract.

The [host projection note](../../Source/Features/ModSettingsNavigation/Implementation.md) records the
bounded API. The source searched a calculated group even without a displayed heading; the selected
contract deliberately searches only the current displayed group. Raw and normalized authors remain
searchable in either mode, and full-list group membership survives filtering and mode changes.

Keep controls with the existing Options → Mods list. One megamod entry leads to its common structured
settings, whose configuration still uses normal global/save inheritance and disabled main-menu save
controls. Do not recreate every legacy source page as a fake outer entry or add a bottom gameplay tab.

## Contracts and coexistence

Native/provider entries keep identity, settings labels, renderer and normal close/write lifecycle.
Provider-generated pages may share a package and must remain separate. The inspected HugsLib1.6
proxy/factory is only reference evidence: actual1.7 selection, forks and shapes need MO-Q2 proof.
Never replace a provider's unavailable page with a blank native one or route ordinary pages through
a broken optional method. Keep original provider access, or disable the owned chooser for a
combination that cannot preserve it. Browsing never resets/imports/rewrites foreign settings.

Bounded DLL scans found no outside executable consumer of source types/owner. No source package spoof,
facade or save/settings migration. Core and Harmony required when implemented; HugsLib/forks optional,
no DLC/XML Extensions needed. Do not copy source load-before-Core hints without actual necessity.
Original `Superniquito.ModOptionsSort` suppresses owned chooser overlap while the megamod's own
structured settings page remains registered. Access is retained through otherwise-working source
navigation; a pre-existing broken source/HugsLib click handler remains an unsupported combination,
not something suppression repairs. No bundled source/private Harmony/Gateway assembly.

## Verification and gaps

MO-Q1–4 retain effective native/injection contracts, beta HugsLib/forks and existing menu fallback,
advertised-only highlights, UI/languages, bounded cost and reviewed-build evidence. All implementation
tasks remain unchecked. Sorting should not change gameplay balance or foreign values; preserving
actual settings access is a playability requirement, not proven by a startup log.

Final native evidence must use Options → Mods, real searches/sort switches and opening/editing/closing
actual native/provider pages, with duplicate entries, main menu versus save, provider-present/absent,
source suppression, languages/scales and failure cases. Retain personally inspected causal views,
reviewed package identity, normal-settings protection and cleanup. No UI/game acceptance from this draft.
