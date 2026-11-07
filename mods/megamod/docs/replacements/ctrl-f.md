# Ctrl F - Continued

## Identity and provenance

- Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; origin: replacement.
- S033, **Ctrl F - Continued**, **Memegoddess.CtrlF**, Workshop **3529520961**, About author Uuugggg,
  continuation by Meme Goddess. Selected 1.7 DLL; About supports 1.4–1.7. Inspected 2026-11-03.
- [Research](../refinement/2026-11-03-ctrl-f-research.md) pins the source commit and installed hashes,
  all eight C# files/selected XML, compiled wrapper, consumed current library and exact native selector.
  MIT source license; no copied assemblies/code/art. Static research is not live beta verification.
- [Owned change](../../../../openspec/changes/specify-megamod-thing-search/proposal.md): **drafted,
  nothing implemented or accepted**. TD Find Lib's full catalogue/library remains separately S121.

## Behavior inventory

| ID | Action → result | Defaults / important boundaries | Evidence / contract | Status |
| --- | --- | --- | --- | --- |
| CF-01 | New/resume/find-like-selection → focused query | Ctrl+F / Ctrl+Shift+F / Ctrl+Alt+F, exact type/material alternatives, safe Plan/no-selection fallback | Wrapper and native Selector; [workspace](../../../../openspec/changes/specify-megamod-thing-search/specs/megamod-search-workspace/spec.md) | Drafted |
| CF-02 | Edit criteria/listing/scope → matching known things | Literal name, type/range/material/zone, All/Any/threshold groups, exact map identity | Consumed compiled library; [search](../../../../openspec/changes/specify-megamod-thing-search/specs/megamod-thing-search/spec.md) | Basic subset plus separately reviewed [S121 catalog](td-find-lib.md); implementation pending |
| CF-03 | Inspect/select/navigate results → ordinary native actions | Shift, double-click, drag, current-map bulk selection, holder/off-map labels, native cap | ThingListDrawer/native Selector; workspace | Drafted |
| CF-04 | Accept search → locate first current match | Correct advertised intent missing from compiled wrapper; stale-target checks | Public/source discrepancy; workspace | Drafted correction |
| CF-05 | Refresh/detach/close → current controlled snapshot | Manual even paused, continuous off initially / 60 ticks, separation off initially, no orphan work | Wrapper/library refresh paths; workspace | Drafted |
| CF-06 | Change save/visibility preference → isolated UI state | Temporary workspace, common true visibility default, native global keybind exception | Source settings and owned lifecycle decisions; workspace | Drafted |
| CF-07 | Transfer supported query → independent complete copy | Locked incoming view, exact binding, reject whole unsupported import/export | SearchTransfer and S018 participant; workspace | Boundary drafted; S121/adapter work pending |
| CF-08 | Compare search/stock/hauling → same facts, different rules | One shared candidate owner, ordinary knownness, actual stored membership and pawn eligibility | User instruction + S032/S021/S018; search/design | Drafted; measurements/native proof pending |

## Interactions and composed result

One owned query path supplies search and relevant production predicates. One map source supplies
candidate facts to Ctrl+F, S032 material counts and S021/S125 hauling. Counts can differ for legitimate
reasons: found does not mean stored, accessible, reservable or haulable. S018 bill-to-search transfers
copy the query, without changing the bill from the search editor. No internal Harmony layering.
P1-25's arrow requests shared index/cache, not a click-to-search command from material counts.

S121 owns remaining predicate catalogue, library storage/grouping/defaults/debug facilities and codecs.
Do not mark these delivered or entirely specified from the smaller S033 wrapper analysis. External
providers are optional; the owned core needs neither TD Find Lib nor its TDS Bug Fixes dependency.

## UI audit and relocation

| Upstream entry | Purpose | Owned destination / intentional difference |
| --- | --- | --- |
| Minimized MainButtonDef TD_Ctrl-F | Toggle search information view | Small bottom-right control; no ordinary bottom tab |
| Keyboard component | New/resume/selection search | Retained shortcuts, documented native keybinding scope |
| Query editor / paired result popup | Criteria and current-play inspection/selection | Workspace with explicit lock, maps, counts and separate result option |
| ShowButton global setting | Entry visibility preference | Common Search section: Global default / Current save / Use global; default true |
| Library/receive/export buttons | Reuse queries, independent copies | Common library boundary; unsupported mappings reject whole transfer |
| Library god-mode listings / hidden data | Debug knowledge | Separate future S121 top-debug entry; ordinary queries unaffected by god mode |

Source Ctrl-selection documentation differs from its actual Shift code; owned UI uses Shift. Enter
restoration, human-readable sort, object/unit totals, holder/map labels, lock covering replacement,
plain text clipboard behavior and current snapshot/action checks are deliberate corrections.
No all-source-library UI or universal custom provider support is claimed.

## Consumed contracts and persistence

| Contract | Finding and intended treatment | Verification still required |
| --- | --- | --- |
| TD_Find_Lib.ISearchReceiver / QuerySearch / CloneArgs / SearchTransfer | Actual transfer binding, source tag Ctrl-F; narrow optional supported mapping, no duplicate registration | Exact library/codecs and real round trips under CF-Q3/S121 |
| Everybody Gets One editor receiver TD.EGO | Concrete separately inspected participant; direct owned composition with S018 | Native bill-map transfer and independent-copy actions |
| Ctrl_F classes / TD_Ctrl-F / OpenCtrlF | No direct external caller found in bounded installed text audit | Binary/reflected callers not exhaustively checked; no drop-in claim |
| TD.ShowSearchButton | Upstream global bool true | New common owned setting, no automatic source-settings migration |
| Source static window / nonpermanent refresher | No workspace save contract or game-reset found | Owned temporary game-session lifecycle, no live references persisted |
| Library Scribe XML / global saved queries | Upstream library responsibility | S121 full codec/storage contract; no foreign migration claim |

Co-installation: current Memegoddess.CtrlF or original Uuugggg.CtrlF suppresses only the overlapping
owned workspace with a bounded warning. Keep independent owned library/stock/hauling/production
usable. Do not remove originals or spoof IDs. Missing/incompatible optional adapters disable only
their exchange actions. Native hooks need documented semantic admission; any future XML changes
need guarded merging and the appropriate required dependency.

## Implementation and coverage

All CF-01–08 implementations, target documents, tests, languages, measurements and native evidence
are **pending**. S033 has basic query/workspace contracts; advanced predicates, library CRUD,
debug inspection and complete-transfer boundaries now have a separate reviewed [S121 draft](td-find-lib.md).
Actual codecs/provider admission and runtime proof remain pending. Source bugs are not compatibility commitments. Automatic foreign
save/settings migration and unconditional binary/package impersonation are **not proposed**.

## Verification and acceptance

This is a document-only slice. [Progress](../refinement/specification-progress.md) records independent
review and strict OpenSpec validation; CF-Q1–4 retain native targets, shared facts/performance,
S121/contracts and implementation acceptance. Future acceptance needs actual keyboard/mouse actions
and observed selection/camera/result changes, paused and multi-map/holder cases, settings/save
isolation and exact provider/source runs on the reviewed build. No game was launched for the draft.
Semantic playability assessment: no resource, work, capacity or research changes; ordinary knowledge
and native action rules stay intact. Performance and compatibility remain unclaimed until measured.
