# Drug Policy Fix (Continued)

## Identity and provenance

- Sole owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
- Origin: **replacement**, with the user's explicit missing-drug cleanup refinement, **S029 / P1-23**.
- Source: `Mlie.DrugPolicyFix`, [Workshop 2195390245](https://steamcommunity.com/sharedfiles/filedetails/?id=2195390245),
  pelador original / Mlie continuation; [public source](https://github.com/emipa606/DrugPolicyFix).
- Inspected 2026-11-02: version 1.7.0, installed DLL SHA256
  `BE53BE819B2489D7A230AC8F1C6B6C6336D7A9BFDC010BEE9D0FEC8A6F043D20`, matching committed public DLL;
  checkout `ed3666385d5e1ffe970f68fff240c72f3163544d`. Exact native 1.7.4995 rev309 inputs, complete
  source paths, narrow Cherry Picker evidence and license discrepancy are in the
  [research report](../refinement/2026-11-02-drug-policy-fix-research.md).
- Source/repository MIT notice and Workshop CC BY-NC-SA notice are recorded separately. Independent
  implementation; no upstream code, binary, asset or translation is copied. Credits include
  sable-starflower (Cherry Picker improvement) and Dimos (German).

## Behavior inventory

All listed native behaviors remain **specified, not integrated or accepted**. A first pure
reconciliation slice is implemented-unverified under `Source/Features/DrugPolicyMaintenance/Core`:
it plans current-identity cleanup, first-duplicate retention, inapplicable-flag normalization,
native new-row defaults, and counts, with an injected native ordering operation for additions.
Nine focused reconciliation tests pass, plus eight atomic-write boundary cases for all-policy
preparation, partial-write restoration and stale-generation refusal. A standalone native storage
adapter now has six real-Defs host cases for entry identity/choices, normalization, missing rows,
native order, nullable storage and partial-write restoration. The separate catalog adapter has eight
real-Defs host cases for native classification, current identities, availability and incomplete-content
refusal. Both adapters are independently reviewed. Native hook admission, lifecycle integration and
applied content publication remain unfinished; these adapters install no hook or gameplay behavior. Local RED/GREEN
records are in `artifacts/Megamod/P0-C/tdd-ledger.md`. Source inspection is not native acceptance.
Scenarios belong to the
[drug-policy capability](../../../../openspec/changes/specify-megamod-drug-policy-maintenance/specs/megamod-drug-policy-maintenance/spec.md).

| ID | Input/action → proposed observable result | Defaults, edge cases and source difference | Scenario |
| --- | --- | --- | --- |
| DP-01 | Load after drug content changes → one current row per policy | Include added content, remove absent/disabled/non-drug/null rows; physical stock irrelevant; native 1.7 already handles part of this | Add content to an existing save; Remove or disable content; No colony stock; Empty policy data or no drugs |
| DP-02 | Repair edited/duplicate data → retain effective choices | Preserve identity/default/assignments; first duplicate wins; clear only inapplicable addiction/joy flags | Edited policy remains edited; Drug properties change; Conflicting duplicates |
| DP-03 | Add a new row → native 1.7 defaults | Addiction if addictive; pleasure/schedule off; 1 day, 100%/100%, inventory 0; no translated-name grants | New drug in a restrictive or permissive named policy; Rename or language change; Return after saved removal |
| DP-04 | Create preset containing unavailable content → usable remaining preset | Valid native explicit choices remain; guard before missing lookup; do not reapply over existing edits | Fresh ordinary presets; Preset refers to removed content; Customized or external valid preset |
| DP-05 | Open/edit policy → alphabetical rows for the correct drug | Display-only sorting avoids source's scheduled/refill priority change; additions enter full native priority; stable equal labels and focus | Find and edit a drug; Two eligible scheduled drugs; Two carried-supply deficits; Newly enabled drug has earlier native priority; Display changes during editing |
| DP-06 | Use native policy controls → unchanged copy/assignment/default/delete meaning | Save-local gameplay state, no added settings/tab | Copy and assign a policy; Default and delete controls; Main menu and save switching |
| DP-07 | Content and scheduled-use features meet → one composed result | S038 final choices; optional supported external filtering; S024 additional restriction | Owned content filtering; Optional external filtering; Additional scheduled-use restriction |
| DP-08 | Repeat/save/fail repair → consistent state and accurate diagnostics | No duplicate mutation/no-op chatter, no autosave; rollback owned partial changes; saved removal returns with defaults | Repeated successful reconciliation; Save and reopen repaired policies; Abandon an unsaved repair; Repair failure |
| DP-09 | Startup compatibility check → admitted behavior or bounded warning | Compatible foreign edits allowed; original active suppresses owned overlap; presentation can fail independently | Compatible foreign changes; Unsupported maintenance or display; Original provider active |

## Interactions and composed result

RimWorld 1.7 already backfills on load before the original component runs. Keep that native behavior
and fill actual gaps instead of copying redundant source loops. Source alphabetical list mutation
also controls which scheduled/refill drug is tried first; owned presentation deliberately separates
these effects. Source's legacy label-based permission branch is normally bypassed on a 1.7 load and
is not revived. Fresh native preset choices remain distinct from defaults for later-added rows.

S038 Cherry Picker will supply finalized content decisions directly; no self-patches. Current narrow
external inspection shows disabled drugs can retain a ThingDef with category None, so a nonnull
reference is insufficient. Its installed 1.6 provider is not accepted for beta. S024 Common Sense's
potential-mood condition remains conjunctive with native scheduled-use eligibility. S021/S024 supply
behavior retains native row targets/capacity. No per-map stock cache or Ctrl+F scan determines content
availability. Actual combined provider/order behavior remains to be verified.

## UI audit and relocation

| Source surface | Purpose | Class | Owned destination / difference |
| --- | --- | --- | --- |
| Native drug policy window rows | Edit ordinary drug use and carried supply | Gameplay policy | Same native window, alphabetical identity-safe display; no new bottom tab |
| Native pawn assignment / policy controls | Choose, copy, rename, default and delete policies | Entity gameplay state | Preserve native controls and in-use deletion restriction |
| Source log messages | Report repair/sort | Diagnostics | Accurate bounded change summaries and admission warnings; omit ordinary no-op/sort chatter |
| Source mod settings or toggle | None found | Not applicable | None introduced |

Policies and assignments are gameplay data, not settings. No global/default override model is
applied to individual drug policies. Native default policy is save-local; it is not a global mod
preference. Any later real configuration would use common settings, with save editing disabled at
the main menu. Existing native labels remain; new text requires the seven baseline languages.

## Contracts consumed by other mods or saves

| Consumer / evidence | Contract and result | Disposition / verification |
| --- | --- | --- |
| Native policy dialog, pawn tracker and job givers | Entry identities, permissions, scheduling thresholds/frequency, inventory target and underlying iteration order | Preserve native data and priority; verify actual editor→pawn use/refill workflows |
| Native save and pawn assignment | Policy ID/name/load identity, source preset reference, entries, default and assigned pawn references | Retain ordinary format, valid values and dose history; no external migration; verify save/load/switch |
| Inspected Cherry Picker removal branch | Clears drug category/removes template references while old content can remain | Consume finalized active-content facts, optional adapter only when timing is proven; native present/absent workflow |
| Installed ASCII/UTF-16 scans | 5,485 DLLs; source versions and YART inventory references, no established callable incoming source API | No package/assembly/type/owner emulation; reassess only for a proven consumer |

Public source utilities alone do not establish consumed contracts. The original's native-data use
does not prove universal safe removal or multiplayer compatibility. No source-specific settings are
imported. Ordinary successful repairs live in memory until the player saves; failed owned commits
restore prior row membership and values. Saved deletions carry no hidden restoration store.

## Implementation and coverage

| Behavior | Implemented here | Deviation / possible conflict | Missing / omitted | Targets and evidence |
| --- | --- | --- | --- | --- |
| DP-01–04, DP-08 | Pure reconciliation, atomic-write boundary and standalone native storage/catalog adapters, implemented-unverified; no native hook integration | Early cleanup, duplicate/null/stale recovery and native 1.7 defaults replace source's late/name-based gaps | Finalized lifecycle authority, field/target admission, early preset/load hooks and publication remain open; general corrupted-save repair, old deleted-choice recovery and physical item/health edits omitted | [Storage/catalog tests and native scenario dependency](../../Source/Features/DrugPolicyMaintenance/NativeStorage-Verification.md); target guards and native evidence pending |
| DP-05–06 | Nothing | Alphabetical presentation preserves job order; foreign dialog mods need shape admission | Source alphabetical job priority omitted | Identity/focus and two-drug native workflows pending |
| DP-07, DP-09 | Nothing | Direct owned composition and source-overlap suppression; no source facade or private Cherry Picker coupling | Unproven external beta/multiplayer support deferred | Exact providers, effective semantic guards and startup diagnostics pending |

Original `Mlie.DrugPolicyFix` active: withhold owned overlapping groups and explain; never unpatch
foreign code. Core maintenance does not require Cherry Picker. Harmony is required when the future
implementation eagerly uses it. No XML changes or XML Extensions dependency are proposed now; any
future declarative operation must obey guarded additive policy. No runtime metadata changes here.

## Verification and acceptance

[Proposal/design/tasks](../../../../openspec/changes/specify-megamod-drug-policy-maintenance/) define
the implementation sequence and final acceptance. No gameplay task is checked. Strict validation
and independent document review are recorded in the
[progress ledger](../refinement/specification-progress.md), separately from runtime acceptance.
DP-Q1–4 preserve remaining decisions/research. Final proof requires honest focused RED/GREEN, reviewed
build/package, exact admitted provider lists and actual native policy/edit/save/assignment plus
pawn scheduled-use/refill behavior. Inspect resulting drug choice, timing and quantities personally;
logs/Def assertions alone are not acceptance. Finish balance/playability review on finalized
native/preset/provider comparators, retain evidence and restore isolated run state.
