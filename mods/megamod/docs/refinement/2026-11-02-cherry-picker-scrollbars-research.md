# Cherry picker scroll bars: sequential source research

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Queue **S020**, intake **P1-15**, origin **replacement**. Researched **2026-11-02**, after S019.
[Owning draft](../../../../openspec/changes/specify-megamod-content-filter-selectors/) and
[coverage](../replacements/cherry-picker-scrollbars.md). **Nothing implemented or accepted in game.**

## Documentation first

The [Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=3781285156)
presents searchable scrolling selectors for long Cherry Picker lists. The
[change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/3781285156) record the
August 2026 addition of expandable multi-column windows and source icons. They do not specify
matching rules, callbacks or persistence; those gaps were filled from the installed assembly.
Generic Steam removed/incompatible banners are not runtime findings.

A bounded public search for the add-on's namespace/package and source found no attributable public
repository. No source/license file is installed. Do not infer a redistribution license; implement
observable behavior independently and do not copy the binary. The required
[Cherry Picker Workshop item](https://steamcommunity.com/workshop/filedetails/?id=3521312241) is a
1.6 port published by ajperson1927, with Owlchemist in installed author metadata. Its linked
[original source repository](https://github.com/Owlchemist/cherry-picker) is MIT-licensed, but the
inspected public tree is older and does not establish equality with this installed fork. Only the
consumed selector catalogue/callbacks were inspected here; the full provider remains S038.

## Exact read-only inputs

| Input | Identity |
| --- | --- |
| Add-on root | `F:/Steam/steamapps/workshop/content/294100/3781285156` |
| About | Cherry picker scroll bars; Lucius; `lucius.cherryscroll`; modVersion 1.0.0; support 1.6 / 1.7 |
| About SHA-256 | `97844FC4FD9CAB540C1D036BFD417C1423DD91EF7F31B360081B3DF6FBD0396E` |
| Selected assembly | `1.7/Assemblies/CherryPickerScrollBars.dll`; assembly/file 1.0.0.0 |
| Assembly SHA-256 | `FBEEC63D734B58AFF256048E89C8BAEB8A52FCF3D125BE9CA6BD9418627EC842` |
| Informational version | `1.0.0+92b0bba2068cf4306115fe41a4d0718d6d7e718b` |
| Dependency root | `F:/Steam/steamapps/workshop/content/294100/3521312241` |
| Dependency metadata | Cherry Picker; Owlchemist; `Owlchemist.CherryPicker`; declares 1.3 through 1.6, not 1.7 |
| Dependency assembly | `1.6/Assemblies/CherryPicker.dll`; assembly 1.0.62.0; file 1.0.62; informational `1.6+7e77b7fdba2daa472a5b7c2b35ac17c149dffa99` |
| Dependency DLL SHA-256 | `4F4AB849B5A3F5BF9FDC497607DE05B03035ED920E2C955FE6660C89BDAF73E1` |
| Native comparison | RimWorld 1.7.4995 rev309, `F:/Steam/steamapps/common/RimWorld/Assembly-CSharp.dll` |
| Native SHA-256 | `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21` |

Artifact root: `artifacts/Refinement/megamod-spec-20261102/cherry-picker-scrollbars`.
The add-on inventory contains About/PublishedFileId, LoadFolders, 1.6/1.7 assemblies and Common
English keyed text. No Defs, XML patches, settings class, textures or source. LoadFolders selects
Common plus the matching version folder. Four English keys cover the two titles, shown/total count
and no matches. Required/load-after metadata names Harmony and Owlchemist.CherryPicker. Direct
references include Harmony and game/Unity assemblies, not a CherryPicker DLL reference.

All six add-on behavioral classes and AssemblyInfo were read under `installed-decompiled`.
The dependency was decompiled to `dependency-cherrypicker16`; relevant Mod_CherryPicker and DrawUtility
paths below were read, not the full content-removal implementation. Native FloatMenuOption was read
under `game`. These are inspection artifacts, not product sources or acceptance evidence. No game ran.

## Complete add-on behavior and consumed callbacks

### Startup and attachment

CherryPickerScrollBarsMod is a Verse.Mod with public constant PackageId **lucius.cherryscroll**.
Its constructor calls Harmony.PatchAll using that owner. CherryPickerSettingsPatch dynamically
resolves **CherryPicker.Mod_CherryPicker.DoSettingsWindowContents(Rect)**. Missing type/method logs
an error. The transpiler resolves FloatMenu(List<FloatMenuOption>) and the two replacement constructors,
then finds all exact newobj calls to that FloatMenu constructor. It requires **exactly two**, replacing
the first with SearchableDefTypeMenu and the second with SearchableSourceModMenu. Missing constructors
or wrong count log errors and return unchanged instructions.

This is a real guard, but not a semantic proof: another third menu rejects unrelated valid changes,
while two reordered or repurposed menus could pass. It does not prove the originating catalogue and
callback of each construction. Our selected integration is direct owned calls, so no such patch or
runtime dependency on the source is needed. Any future external adapter needs a separate target spec.

### The two base selectors

The inspected dependency's settings renderer has precisely the two consumed constructions, in the
expected order, inside separate button handlers with try/catch. Type options come from
**DrawUtility.MenuOfDefs()**; source options from **MenuOfPacks()**. Nonempty lists open a FloatMenu.
The page's third control is a separate QuickSearchWidget that updates its content-text search; the
page content list already has its own scrolling. The add-on changes neither that text search nor
the main content list scrollbar, content checkboxes or removal logic.

MenuOfDefs caches choices from CherryPickerUtility.allDefs. Distinct types are keyed by type name,
or name plus namespace for non-Verse/RimWorld types present in DefUtility.typeCache. Labels show
qualified names for those external types. Options are label-sorted and **AllDefTypes** is inserted
first. ApplyCategoryFilter sets only filteredType, with null for All. WriteSettings clears this
cached type menu and the base search-string cache; the add-on has no extra persistence hook.

MenuOfPacks caches a choice for each active RunningModsListForReading pack, sorts by label and inserts
**AllPacks** first. Each callback captures **item.Name**, then ApplyModFilter stores that string in
filteredMod; All stores empty. DrawList compares the filter with **Def.modContentPack?.nameInt**.
Thus two packs with identical names are merged by the actual source filter, not merely visually
ambiguous. DrawList combines source equality, exact type and the separate content-text search.
Choosing either selector does not clear the other filters and does not toggle content.

The source includes active packs even when they contribute no currently visible entries. Neither
selector's choices are narrowed to the current intersection. This slice preserves access to the
host catalogue; S038 must independently specify the entire eligible content/type catalogue.

### Search, layout and invocation

SearchableCherryPickerMenu snapshots non-null options and stably sorts by descending Priority then
descending orderInPriority. The inspected base supplies equal ordinary priorities, so the base's
All-first and label order survive. It opens at **340 x 560**, with close X, outside dismissal,
background-input absorption, dragging and resizing. There is no persisted size/search/scroll.

Each opening starts with empty search and zero scroll. Text edits reset scroll. Filtering trims the
query, splits space/tab/newline/carriage-return words and requires every word to be a substring of
the full option.Label, **OrdinalIgnoreCase**. Blank matches all. No fuzzy/regex/description/hidden-ID
matching or phrase-order requirement. Matching retains order; a cached list is reused while the
trimmed query stays the same. Shown/total counts include all non-null options, including All. Empty
matches show a separate localized label.

ResponsiveOptionGrid uses **280** minimum column width, **6** column gap, **16** scrollbar allowance
and **32** row height. Columns increase with available width; placement is row-major, left-to-right
then down. View height covers all rows and at least the viewport. Scroll X is forced to zero and Y
clamped to the current content. Each frame still iterates all options, drawing only rows intersecting
the visible vertical range. That is draw culling, not proof of O(visible) processing or optimization.

Rows have alternating backgrounds and hover highlighting. Labels are single-line truncated without
full-text tooltip. Every clicked row calls **option.Chosen(false, null)**, then closes unconditionally.
Native 1.7 Chosen checks Disabled (action == null), invokes a valid action once, or plays rejection
sound for disabled options. The custom row does not render the native option's disabled style,
tooltip or extra controls, and it still closes after rejection. Do not claim it executes a disabled
action. The inspected base creates ordinary enabled filter options; extended/failed choices need
our explicit handling. Escape and background-input behavior need exact beta native acceptance;
the proposed Escape/cancellation contract is not evidence from a running source instance.

### Source icons

SearchableDefTypeMenu only selects its title. SearchableSourceModMenu reserves icon space on every
row and constructs a dictionary from active pack **Name** to Icon, using the **first** equal name.
It uses ModMetaData.Icon or the native OptionsMod texture fallback. Options then look up their exact
label in that name dictionary. Equal-name packs can display the first pack's icon. All or unmatched
labels have a blank reserved area; invalid textures are not drawn. No download or asset persistence.

Our proposed correction carries one exact identity through label, filter and icon, visibly
distinguishing equal names. Missing icons remain neutral and never change eligibility. This changes
the composed base filter too; S038 must adopt it. It is not described as a source feature.

## Contract and interaction audit

| Surface | Evidence and decision |
| --- | --- |
| Required external target | Public Mod_CherryPicker.DoSettingsWindowContents(Rect), reflected by the add-on; consumed two option-list constructors are proven in the installed 1.6 base |
| Add-on public classes | CherryPickerScrollBarsMod; abstract SearchableCherryPickerMenu; sealed SearchableDefTypeMenu and SearchableSourceModMenu with List<FloatMenuOption> constructors; no required external consumer found |
| Add-on internal classes | CherryPickerSettingsPatch and ResponsiveOptionGrid; do not preserve their patch/grid implementation as public API |
| Literal consumer scan | Package/namespace/menu-type patterns across installed Workshop plus local Mods; DLL matches only its own 1.6/1.7 assemblies, text only own About and keyed language file; results retained in consumer-dll-paths.txt / consumer-text-paths.txt |
| Limits of scan | Bounded literal scan cannot exclude dynamically constructed names, uninstalled consumers or the wider base Cherry Picker API ecosystem |
| Source-specific dependency | Add-on declares 1.7 while latest installed base declares through 1.6; no source-present 1.7 behavior is verified |
| Owned composition | One direct selector view for both entry points in S038's owned page; no self-patch, duplicate content tab or blanket FloatMenu replacement |
| Source coexistence | Add-on presence alone does not disable ours; its external target is separate. S038 must decide base-provider coexistence before activating any owned content configuration |
| Persistence | No add-on settings/save schema to migrate. Source base settings and APIs remain S038, not covered by this absence |
| XML/assets | No source Def/XML change or bundled artwork. No XML Extensions dependency for this UI slice; future XML operations require declared dependency/guards |

No source package impersonation, automatic import, speculative facade or universal compatibility
claim follows from this audit. The user's Cherry Picker drop-in intent remains an explicit S038
contract-analysis task; S020 does not waive or satisfy that wider requirement.

## Proposed refinements and recorded questions

The draft retains the source's two selectors, search rule/count/order, responsive scrolling, icon
purpose and fresh opening state. Proposed corrections are exact source identity with visible
disambiguation, full-label hover, clear unavailable rows, cancellation that cannot click through,
stale-catalogue rejection and supported-screen clamping. They are documented design proposals,
not separately confirmed user preferences or reproduced source-runtime defects.

| ID | Kind | Remaining work without interrupting specification |
| --- | --- | --- |
| SC-Q1 | Proposed UX refinements | Review exact-source disambiguation and its displayed identifier, full-label hover and unavailable-choice presentation; retain observable safety semantics unless explicitly changed |
| SC-Q2 | Parent contract research | S038 owns full eligible catalogue, content settings scope/application, original-provider coexistence, actual public consumers and any migration; carry over exact identity instead of name equality |
| SC-Q3 | Runtime integration research | Prove exact 1.7 window close/resize/input ownership and host lifetime; only introduce a patch with its own semantic target/admission if direct composition cannot suffice |
| SC-Q4 | Implementation acceptance | Focused RED/GREEN, seven languages, real UI-scale access, measured catalogue costs, independent implementation review and root native player-workflow evidence |

No independent global/save preference is introduced; these are temporary navigation controls. Actual
content settings belong in mod settings and follow common inheritance or a documented S038 exception.
There is no additional bottom tab or gameplay toggle. Browsing changes no research, costs, yields,
jobs or enabled content; preserving that separation is the semantic/playability gate. No runtime,
performance or compatibility acceptance is inferred from document validation.
