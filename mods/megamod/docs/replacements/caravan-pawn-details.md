# Show gender/age in caravan form dialog

## Identity and provenance

- Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target1.7 beta.
- Origin **replacement**, S116/P2-45; arl85, package `arl85.ShowGenderAgeCaravanFormDialog`,
  [Workshop2914157264](https://steamcommunity.com/sharedfiles/filedetails/?id=2914157264).
- Selected1.7 DLL1.2.0.0,6144bytes,SHA256
  `22B807500B30AB631150734289A0D267D8E0EEB65037355A65D9EA14B3974135`.
  [Research](../refinement/2026-11-06-caravan-pawn-details-research.md) pins public commit,
  native build, complete patch inventory, TinyTweaks and consumer checks.
- No standalone license found; independent implementation planned, no code/DLL/art copy.

## Behavior inventory

All rows are **specified, not implemented or verified** in the
[owned contract](../../../../openspec/changes/specify-megamod-caravan-pawn-details/specs/megamod-caravan-pawn-details/spec.md).

| ID | Observable behavior | Source / owned decision |
| --- | --- | --- |
| CPD01 | One-way pawn name plus localized gender/completed biological years; species for individually named nonhumans | Source getter patch; form/reform/split/transporter scope confirmed natively; items/corpses/two-way trade untouched |
| CPD02 | Uniform group age or completed-age range; all-available-member explanation; unchanged selection | Intentional correction to source first-pawn-only summary; missing/incompatible data preserves ordinary label |
| CPD03 | Native search and Name order consume displayed text; ordinary controls remain | Source effects through native consumers, no new numeric-age sorter |
| CPD04 | Existing row/tooltip, current facts on native refresh/reopen, seven languages, transient description | Preserved native context with explicit layout/lifecycle guarantee |
| CPD05 | One known detail provider; compatible name changes retained | Original/TinyTweaks overlap suppresses ours, unknown overlap fails closed; foreign settings/patches untouched |
| CPD06 | Minimal guarded targets, bounded warning/fallback and safe revalidation | Replaces source unguarded PatchAll; all implementation/acceptance pending |

## Interactions and UI audit

S133 Bulk Load For Transporters and S135 Basic Caravan Loading must use the same actual row
description without independent indexing or self-patches. This feature changes no loading/hauling
mechanics. Native grouping, transfer counts, pawn eligibility, capacities and actual selected
members remain authoritative. A partial count is not a request for a particular age within a row.

Source has no tab, window, gizmo, settings or debug action: it changes existing labels. Keep that
native placement. No new operational toggle or Use-global setting is introduced; pawn facts and
transfer selections are entity state. Long text remains available on hover without covering native
info or count controls. No separate per-feature pawn cache or map scan.

## Contracts and compatibility

| Surface | Evidence / decision |
| --- | --- |
| Source package/assembly/static Harmony field/nested patch class | No callable consumer found in bounded scan; no facade, package spoof or binary compatibility claim |
| RimPy package catalog entry | Discovery metadata only; not an API consumer |
| Native one-way label, tooltip, quick search and Name comparer | Preserve input names and native query/order/control behavior; separately guard any required presentation hook |
| Original provider | Suppress owned overlap; source group-age limitation remains when source is selected |
| TinyTweaks | Public inspected bool/default true; optional, not installed/accepted; on selects provider, known off permits ours, unknown disables ours |
| Existing save/settings | No source/owned helper state; no migration/import necessary or promised |

Compatible other patches alone do not reject admission. Unknown required target semantics or
installation failures disable only the affected owned group with bounded patch-specific warnings.
No removal of foreign patches or correction of duplicate behavior already produced by two foreign
providers. Harmony required with actual use; no hard DLC, source, other product or Gateway dependency.
No XML mutation/XML Extensions operation or source asset selected.

## Implementation, deviations and omissions

| Scope | Implemented here | Deviation / remaining work |
| --- | --- | --- |
| Complete source display behavior | Nothing | Owned grouped ranges, integer completed-age boundary, safe missing-data fallback and provider admission are deliberate corrections |
| Sorting/pregnancy | Nothing | Source has no new numeric sorter or pregnancy display; comment requests are not adopted |
| Runtime and compatibility | Nothing | All target docs, code, focused TDD, seven-language UI, measurements, packages and causal native acceptance remain open |

## Verification

[Ledger](../refinement/specification-progress.md) tracks CPD-Q1–3 and document review/validation.
Future acceptance uses native form/reform/split/load lists, real search/sort/hover/info/quantity
actions and transfers in a fresh minimized isolated reviewed-build game. Include uniform/mixed
ages, partial counts, narrow/scaled translations, source/provider-present, setting transitions,
fallback, native refresh/reopen and save switching. Personally inspect causal views and retain
build/mod order, actions, screenshots, normal configuration hashes and cleanup. A getter assertion,
clean startup or static screenshot does not accept this UI or transfer behavior.
