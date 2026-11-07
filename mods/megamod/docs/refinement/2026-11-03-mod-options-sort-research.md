# S047 — Mod Options Sort research

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Inspected **2026-11-03**, origin replacement, handwritten **P1-40 / P2-30**. Documentation and complete
selected-source analysis for specification only; no runtime/UI compatibility acceptance.

## Documentation first and exact installed input

The author's [Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=2910865748)
describes author grouping, a miscellaneous group for single-author entries, special Vanilla Expanded/
HugsLib grouping, author search, switchable sorting and a HugsLib search repair. Installed About also
advertises yellow non-default and blue unusual-implementation highlights. Those highlights are
absent from the complete inspected 1.7 code. Current public description does not repeat them.
Comments report an October1.7 author-null error and an already addressed packaging upload problem;
neither is an observed failure on this machine. Generic page banners are not compatibility evidence.

Installed root `F:/Steam/steamapps/workshop/content/294100/2910865748`: About XML, PublishedFileId,
preview/icon and separate1.4/1.5/1.7 DLLs. No LoadFolders, source, license, Defs, patches or languages.
Package **`Superniquito.ModOptionsSort`**, author **Superniquito** (Workshop superniqui10), supports
1.4–1.7. Harmony required, source loadAfter Harmony and loadBefore Core/Faster Game Loading/HugsLib.
Those hints do not establish a mandatory before-Core requirement for our replacement.

The explicit **1.7/Assemblies/ModOptionsSort.dll** is the selected reference: assembly/file1.0.0.0,
Release, .NET10, informational **`1.0.0+1bab3ab2d11d9bb719348a586fa88340e30d3c00`**. Complete three
classes decompiled read-only with ILSpy11.2.0.10050 under
`artifacts/Refinement/megamod-spec-20261103/mod-options-sort/compiled`. No public source/license was
located through installed metadata, Workshop links or bounded exact-title/package searches. Do not
infer a reuse license; independently implement behavior and do not copy source code, assemblies or art.

| Input | SHA-256 |
| --- | --- |
| Selected1.7 DLL | `35B7D4E31304F1D13FABFE938541308CF3E38D892807334577AB32339008CEAD` |
| About.xml | `EFF68E1EB70D38EA40653945074AC4E24E0FFD221F8AE2DA5008B2447A23F241` |
| Native1.7.4995 rev309 assembly | `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21` |
| Inspected HugsLib v1.6 DLL | `8BAD9C2EB1CB7D2735AEF9DDFB209F422C257DA14A4740A0F99F9EF7363386D7` |

## Complete source behavior

One Mod constructor calls unguarded PatchAll under **`Superniquito.DoModOptions`**, then initializes
optional HugsLib reflection. Two patches:

1. Dialog_Options.PostOpen postfix, priority200, sorts its completed cachedModsWithSettings list.
2. Dialog_Options.DoModOptions prefix, priority800, wholly replaces the native mod-list drawing,
   search and click behavior and returns false. It uses private list/search/truncation fields.

No settings object, ExposeData, file persistence, save component, gameplay job, Def, runtime map
index, own settings page or new bottom tab. `byAuthor=true` and `groupSingles=true` are static process
state; buttons change them immediately for later openings in the same process. Restart resets both.
Search belongs to the native dialog; PostOpen resets its quick-search widget. Source passes modFilter
by value and uses it locally; it does not reliably update the backing native field.

### Grouping, sorting and display

SortModList clears two static dictionaries, materializes the current settings-entry list, counts
entries per normalized author/group key and sorts the list. It counts **settings entries**, not unique
packages or matching search results. Author keys ordinarily use the whole AuthorsString, not each
author separately. Source normalization precedence:

1. No Content → `Config menu created with HugsLib`.
2. Content name contains both `vanilla` and `expanded`, case-insensitively → `Vanilla Expanded`.
3. Author string contains `sarg` → `Sarg Bjornson`.
4. Else author contains `helixien` → `Helixien`.
5. Otherwise retain metadata AuthorsString.

With groupSingles on, every normalized key with exactly one entry becomes `Various Authors`, even a
special key. Various Authors sorts last. Other groups sort by current string comparison; entries
within a group sort by SettingsCategory. Name mode sorts by SettingsCategory without group headers.
There is no stable tie-break for duplicate display names. Search does not recompute group sizes.

Two controls switch author/name sorting and, in author mode, single-entry grouping. Results are
two-column native-like cards, group heading when group changes, new row per group, existing mod icon,
truncated settings category and full-category hover text. Name mode and Various Authors rows show
an additional `by ...` author line. Other author-group rows omit that line. Headings, buttons and
`by` are hardcoded English; no localized keys. No collapse control, favorites, user-authored grouping,
mod enable/disable/reordering, search inside individual controls or export/reset function.

### Search and opening

Case-insensitive substring search accepts SettingsCategory, Content.Name if present, the displayed
group key and its ungrouped normalized author key. Empty search includes all entries. The raw metadata
author lost by VE/Sarg/Helixien normalization is not additionally searched. Searching a singleton's
ungrouped author still finds it under Various Authors; searching Various Authors finds that group.
Source filtering occurs before rendering and does not alter underlying settings values.

Without HugsLib, a click opens native Dialog_ModSettings for that exact Mod. With active package
`unlimitedhugs.hugslib`, all clicks call reflected `OptionsDialogExtensions.GetModSettingsWindow`.
Reflection failure is logged but hugsActive stays true, so later clicks can call a null method. The
source claims that failure is harmless; the code does not supply a proven safe click fallback.
Missing Content is handled by some author/search branches but dereferenced for icons; malformed
metadata/author values and callback exceptions can escape before or during rendering. Render catch
logs then rethrows; static counts can be stale for late injected entries. These are static risks,
not reproduced1.7 exceptions. No non-default comparison, constructors for default settings,
reflection into foreign values or yellow/blue coloring exists in the selected three classes.

## Exact native and optional-provider contracts

Native1.7 Dialog_Options.PostOpen resets search and builds SettingsCategory-nonempty ModHandles,
sorted by category. DoModOptions draws native cards, searches category/content name and opens
Dialog_ModSettings. The latter calls that Mod's DoSettingsWindowContents and WriteSettings on close.
Preserve this owner/lifecycle rather than clone foreign controls into our common settings schema.
Native dialogs already live under Options → Mods; no added gameplay tab or top-debug entry is needed.
Native evidence is under the research artifact's `native` directory.

Installed HugsLib Workshop818773962, `UnlimitedHugs.HugsLib`, About12.0.0, advertises through1.6.
Its LoadFolders has no1.7 mapping. The inspected **v1.6** DLL is a narrow API reference, not a claim
that this exact folder is selected or supported in beta. Complete OptionsDialogExtensions and
SettingsProxyMod were read:

- InjectHugsLibModEntries appends proxy entries for visible settings packs, including the library's
  own pack and standalone packs; initializes content from the owning mod or HugsLib fallback.
- **Modern proxies normally have non-null Content.** Source's null-Content HugsLib classifier therefore
  does not reliably identify them; provider-resolved identity is required for correct special grouping.
- GetModSettingsWindow(Mod) distinguishes a SettingsProxyMod and returns the HugsLib pack dialog;
  ordinary Mods receive native RimWorld.Dialog_ModSettings.
- Proxy SettingsCategory is the pack entry label. Multiple independent packs may share content/package;
  don't merge by package or category. Native dialog on a proxy is not an equivalent renderer.

Retain optional, package-ID/shape-guarded provider navigation. The feature must work without HugsLib.
Resolve forks only through explicit inspected adapters; don't locate an arbitrary same-named class in
another assembly. Do not globally route all native rows through a broken optional method. If a provider
cannot be safely represented/opened, retain its established menu path and disable only the owned
integration or affected row with explanation. No undocumented fallback that loses its settings.

Bounded installed DLL scan for ModOptionsSort, FixedAuthorsString and Superniquito.DoModOptions found
only the source's three versioned DLLs. No outside executable consumer or save contract was found.
No package spoof, exported facade or source-file migration is justified. Metadata references are not
proof of executable API consumption. Existing native/provider mod objects stay the opening targets.

## Proposed behavior and intentional corrections

Retain author/name sorting, singleton grouping, documented special headings, search and native owner
opening. Keep the two sort controls **transient browsing state**, as upstream: same-process retention,
reset on game restart, no save/global configuration row. Search resets on opening the dialog. This is
not a persisted-settings exemption; no policy is stored. Controls stay with the settings list they
organize, not on the colony's bottom-right bar. Future persisted preferences require a separate scope
contract through the common settings path.

Keep source VE and Sarg/Helixien naming normalization as visible grouping conventions, not assertions
about exclusive authorship. Also preserve raw authors for search and full identity inspection. Resolve
HugsLib-generated rows by actual provider identity, not null Content. Missing metadata uses an unknown
author fallback and never assumes HugsLib. Preserve independent duplicate-title/settings-pack entries,
stable tie ordering, recoverable missing icons and seven-language labels. Keep per-entry errors bounded
and unrelated menu entries usable. No source highlights are claimed from stale About text; their absent
implementation is explicitly a gap, with any future truthful non-default status needing its own scope.

Megamod remains one settings entry leading to its structured common settings pages; do not recreate
one legacy source-mod settings entry per integrated feature. Individual owned configuration rows retain
global/save semantics there. This feature organizes the outer native/provider chooser only.

| ID | Remaining work |
| --- | --- |
| MO-Q1 | Effective native chooser/late provider injection and minimal per-patch target documents; independent entries, native click/write lifecycle and safe source-provider suppression |
| MO-Q2 | Exact beta HugsLib/forks selection and supported proxy/opening contract; absent/changed provider fallback and original menu accessibility |
| MO-Q3 | Advertised-only yellow/blue highlights have no selected1.7 implementation; any requested future status indicators need a truthful default/unknown contract, not unsafe foreign-settings introspection |
| MO-Q4 | Seven-language UI/scales, per-dialog list freshness/cost, focused tests/review/package and actual native Options/search/open/edit/close workflows |

Co-installation suppression preserves otherwise-working source navigation and our page registration.
It cannot repair a pre-existing broken original/HugsLib click handler; such a combination remains
unsupported and diagnostics must not promise usable access merely because the owned chooser is off.

No code, provider DLL or normal configuration was executed/edited. This analysis does not accept
sorting/search, HugsLib compatibility, source error recovery or beta loadability as working gameplay.
