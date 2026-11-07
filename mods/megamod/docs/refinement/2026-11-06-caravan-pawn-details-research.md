# S116 — Show gender/age in caravan form dialog

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin **replacement**, P2-45. Documentation-first research on **2026-11-06**; static inspection
only. The [proposal](../../../../openspec/changes/specify-megamod-caravan-pawn-details/proposal.md),
[design](../../../../openspec/changes/specify-megamod-caravan-pawn-details/design.md) and
[observable contract](../../../../openspec/changes/specify-megamod-caravan-pawn-details/specs/megamod-caravan-pawn-details/spec.md)
separate intent, implementation analysis and proposed behavior. No game was launched.

## Identity and inspected material

| Input | Exact identity / outcome |
| --- | --- |
| Installed source | `F:/Steam/steamapps/workshop/content/294100/2914157264`; title Show gender/age in caravan form dialog; author arl85; package `arl85.ShowGenderAgeCaravanFormDialog` |
| Metadata | Advertises1.4–1.7; Harmony required/loadAfter; no modVersion; PublishedFileId2914157264 |
| Selected assembly | `1.7/Assemblies/ShowGenderAndAgeInCaravanFormDialog.dll`,6144bytes, assembly/file1.2.0.0 |
| SHA256 / MVID | `22B807500B30AB631150734289A0D267D8E0EEB65037355A65D9EA14B3974135`; `36230c9b-a5eb-4b19-9549-eb0992f12a34` |
| Public source | [Correct repository](https://github.com/angelolocritani/Rimworld-ShowGenderAndAgeInCaravanFormDialog), pinned commit `1d29652f57dc4a42b227c504dced5e8ccbbff499`,2026-10-25T16:40:42Z |
| Installed inventory | Seven files: About.xml, PublishedFileId, Preview.png, four version DLLs; no installed source, Defs, patches, language files, settings or license |
| Public verification | Seven retained text files match Git blobs; all four installed DLLs match public Git blobs. Installed1.6/1.7 DLLs are byte-identical. About normalized text matches (line-ending difference); PublishedFileId matches |
| Native game | 1.7.5000rev1275; root `Assembly-CSharp.dll`,15,636,480bytes,SHA256 `D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254` |
| Native Version.txt |18bytes,SHA256 `BAD5BED449D17B2AF39925A3A25007164B2B1F89B29A4E24AFA3B505EDCC535A` |

The About URL omits **And** from the repository name and returns404. The correct repository is
linked from Steam; do not infer source absence from that stale metadata link. Public project targets
.NET4.7.2, references Krafs.Rimworld.Ref1.6.4488-beta/Harmony2.3.6 and has a Release output under1.6.
Metadata advertising1.7 and identical binaries do not establish effective beta compatibility.

No explicit standalone license was found in installed or public inventory; GitHub's license field
is null. Plan independent behavior implementation, no code/DLL/art copy. Preview was inventoried,
not selected or evaluated as product art.

## Public documentation before decompilation

[Steam](https://steamcommunity.com/sharedfiles/filedetails/?id=2914157264) and the pinned README
promise gender/age in caravan formation like trading and say the feature is included in
XNDTinyTweaks since1.4.2. Steam was posted2023-01-07 and updated2026-10-25. The author explicitly
places additional sorting outside scope and suggests a separate sorting mod. Pregnancy requests
in comments do not establish implemented behavior.

[Changelog](https://steamcommunity.com/sharedfiles/filedetails/changelog/2914157264) was retained
with direct read-only fetching after the web reader could not open it. Five entries include the
2026-10-25 automated update,2025-06-11 and2024-03-15 entries both saying update for1.5, then two
2023-01-07 entries. Retain the stated text rather than interpreting the inconsistent date/title as
a new behavior. Workshop HTML88708bytes SHA256
`D722263F912833DDCBD18E52DE83AA8E6547459A22BA9E0A28F3A5C9268FF1DC`;
changelog HTML51563bytes SHA256
`FC9268A6945A0EABFB16AA73B77AD02B6595A8F137E6E05B8D14B370649508E6`.

## Complete selected implementation inventory

The small selected DLL has one outer startup class and one nested patch class. Public source and
read-only decompilation agree on the behavior below; no additional gameplay subsystem exists.

| Source surface | Actual behavior / boundary |
| --- | --- |
| `ShowGenderAgeCaravanFormDialogMod` | Static startup creates Harmony owner `arl85.ShowGenderAndAgeInCaravanFormDialog` (different from package ID) and unguarded PatchAll; public static `harmonyInstance` |
| Nested `TransferableOneWay_Label` | One postfix on `TransferableOneWay.Label` getter; retains incoming result and inspects `AnyThing` |
| Named nonhuman | If Name exists, is not Numerical and RaceProps is not Humanlike, appends comma and `pawn.def.label`; applies to qualifying nonhuman mechanical pawns too, not just animals |
| All pawn rows | Appends parentheses containing native `GetGenderLabel()` and `Mathf.FloorToInt(AgeBiologicalYearsFloat)`; no faction, child, animal or caravan-window gate |
| Non-pawn | No addition; a corpse is not a Pawn |
| Grouped row | Only first representative is inspected; other members' ages are not summarized |
| State / UI / assets | No settings, save keys, extra menu, tab, gizmo, query/sort control, map scan, cache, Def/XML mutation or runtime artwork |

Null AnyThing makes the postfix do nothing, but the native getter already dereferences AnyThing:
the source does **not** repair an empty row's native getter failure. Missing race/age data are not
guarded. GetGenderLabel uses native localized human/animal male/female and gender-none labels;
invalid gender values are not a new supported category. Unguarded append duplicates another
provider's equivalent append. These are static findings, not observed live failures.

## Exact native contracts and observable consequences

Sixteen successful type extracts are retained. Three guessed wrong names produced empty files and
are marked unsuccessful in the manifest; they are not evidence. Successful relevant paths:

| Native surface | Finding |
| --- | --- |
| `TransferableOneWay`, `Transferable` | AnyThing is first list member or null; Label is AnyThing.LabelNoCount; LabelCap capitalizes it. Selection/count serialization is separate |
| `TransferableOneWayWidget` | Native cache filters `quickSearch.Matches(tr.Label)` and sorts by selected comparers; default Category/MarketValue. Query/sort changes refresh the cache |
| `TransferableComparer_Name` | Compares Label text; appended gender/age participate lexically, not as a numeric age sorter |
| `TransferableUIUtility` | Draws icon/info card/LabelCap and quantity-related layout; hover text begins LabelCap and retains native description/contents; empty row is skipped by drawing |
| `Dialog_FormCaravan`, `Planet.Dialog_SplitCaravan`, `Planet.CaravanUIUtility` | Form/reform/split use shared one-way pawn/items/supplies widgets |
| `Dialog_LoadTransporters` | Builds one-way transfer widget sections directly; source display also reaches this UI |
| `Tradeable_Pawn` | Native two-way trade already has the same named-nonhuman/gender/floored-biological-age pattern; source does not patch it |
| `GenderUtility`, `Pawn_AgeTracker` | Localized ordinary gender; completed biological years available directly. Float years can round near an integer boundary; chronological age is separate |
| `TransferableUtility` | Grouping excludes humanlikes, poor health, individually named pawns, overseers/bonds/visible pregnancy under the inspected rules; compatible pawns share kind/Def/gender/life stage with age difference at most1year and relevant health agreement |

Thus two grouped animals can be2.9 and3.1 biological years but source shows only the first's
completed age. Proposed correction: common age or minimum–maximum **completed** ages, preserving
native grouping and selection. This is an owned decision, not source parity or user-confirmed new
sorting. It describes all available row members; partial selection does not identify exact members.
Current missing/incompatible data preserves the ordinary/provider label. Existing native refresh
and reopen boundaries govern when cached search/order is rebuilt; no unmeasured birthday polling.

## TinyTweaks and consumers

[XNDTinyTweaks public repository](https://github.com/emipa606/XNDTinyTweaks), pinned commit
`24401ebf3185b52336a46142727ff20cce005782`,2025-08-18T19:00:54Z, is an overlap reference, not an
additional wholesale selected mod. Package `Mlie.XNDTinyTweaks`, Workshop2270049772, mod1.6.2,
advertised1.1–1.6; author XeoNovaDan/maintainer Mlie. Five retained public files (About, license,
patch, mod class, settings) match Git blobs. MIT Copyright2020Mlie applies to that repository; it
does not license the standalone source.

Its postfix uses the same one-way label behavior gated by static bool
`TinyTweaksSettings.ShowGenderAgeCaravanFormDialog`, default true, serialized under that name and
exposed in QoL settings. Harmony owner `XeoNovaDan.TinyTweaks`. About explicitly lists the
standalone package incompatible. No actual installed TinyTweaks was found: the installed
VarietyMattersRedux reference is only a loadAfter entry, not the provider itself.

Owned selection: source active → ours off; admitted TinyTweaks option on → ours off; admitted
option off → ours eligible; unknown overlap → ours off with bounded diagnostic. Recheck supported
setting changes without touching foreign settings. This avoids adding our duplicate; it does not
repair two foreign providers duplicating each other or inherit their feature's group correction.

Bounded installed text scan (C#/XML/JSON/Markdown) found source metadata and RimPy Mod Manager
Database (`rupal.rimpymodmanagerdatabase`,Workshop1847679158) catalog entry in `db/db.json`.
That entry records the source package, not a callable API. Bounded assembly-name byte search found
only the four source version DLLs. No external binding was proved; dynamic or uninstalled consumers
remain possible. Do not spoof package/assembly/Harmony/type/static-field identity without evidence.

## Owned composition, UI and semantics

The [coverage report](../replacements/caravan-pawn-details.md) maps all source behavior to the
observable contract. S133/S135 loading work will share actual native row presentation; no patches
against our own code or second index. Current labels stay in native context, so no bottom tab,
debug control, toggle, settings or Use-global row is warranted. Group membership is gameplay state,
not configuration. Seven-language owned explanations and native localized fields are required.

No pawn eligibility, group/count allocation, hauling capacity, work, recipe, acquisition, economy,
research or travel consequence is changed. No balance constants are invented. No new DLC/runtime
provider required; Harmony required when actually used. No XML/Defs selected; XML Extensions rules
apply if that changes. Semantic review of this draft can inspect these boundaries statically;
real transfer outcomes still require future reviewed-build native acceptance.

## Evidence and remaining work

Ignored evidence: `artifacts/Refinement/megamod-spec-20261106/caravan-pawn-details/` contains input
and inventory manifests, assembly identity, retained public commit/tree/text/HTML, read-only
decompilation, native extracts and bounded consumer scans. Source assemblies were not loaded or
executed. No Workshop file, normal game config/save or product runtime file was edited.

CPD-Q1–3 in the [ledger](specification-progress.md) retain effective target/provider checks,
layout/group-cost/native-refresh measurements and implementation/acceptance. Review and strict
validation status is recorded there; static research is not native compatibility proof.
