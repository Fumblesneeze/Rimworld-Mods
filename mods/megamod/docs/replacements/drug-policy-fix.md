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

DP-04's initial preset/native-controls and unchanged-catalog save/reload workflows have scoped
native verification; content repair and the other listed behaviors remain unfinished. A first pure
reconciliation slice is implemented-unverified under `Source/Features/DrugPolicyMaintenance/Core`:
it plans current-identity cleanup, first-duplicate retention, inapplicable-flag normalization,
native new-row defaults, and counts, with an injected native ordering operation for additions.
Nine focused reconciliation tests pass, plus eight atomic-write boundary cases for all-policy
preparation, partial-write restoration and stale-generation refusal. A standalone native storage
adapter now has six real-Defs host cases for entry identity/choices, normalization, missing rows,
native order, nullable storage and partial-write restoration. The separate catalog adapter has eight
real-Defs host cases for native classification, current identities, availability and incomplete-content
refusal. Both adapters are independently reviewed. General reconciliation hook admission, lifecycle integration and
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
| DP-09 | Startup target check → admitted behavior or bounded warning | Compatible foreign edits allowed; active original triggers warning and best-effort ordinary settings-off behavior with preferences retained; presentation can fail independently | Compatible foreign changes; Unsupported maintenance or display; Original provider active |

## Interactions and composed result

RimWorld 1.7 already backfills on load before the original component runs. Keep that native behavior
and fill actual gaps instead of copying redundant source loops. Source alphabetical list mutation
also controls which scheduled/refill drug is tried first; owned presentation deliberately separates
these effects. Source's legacy label-based permission branch is normally bypassed on a 1.7 load and
is not revived. Fresh native preset choices remain distinct from defaults for later-added rows.

Owned S038 content selection will supply finalized content decisions directly; no self-patches.
Historical Cherry Picker inspection shows disabled drugs can retain a ThingDef with category None,
so a nonnull reference is insufficient for the current native catalog. That research does not require
an original Cherry Picker API adapter or coexistence run; genuine external content integration, if
actually used, still needs its own admission. S024 Common Sense's
potential-mood condition remains conjunctive with native scheduled-use eligibility. S021/S024 supply
behavior retains native row targets/capacity. No per-map stock cache or Ctrl+F scan determines content
availability. Actual owned composition/order and any genuine used integration remain to be verified.

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

## Native contracts, own persistence and content assumptions

| Consumer / evidence | Contract and result | Disposition / verification |
| --- | --- | --- |
| Native policy dialog, pawn tracker and job givers | Entry identities, permissions, scheduling thresholds/frequency, inventory target and underlying iteration order | Preserve native data and priority; verify actual editor→pawn use/refill workflows |
| Native save and pawn assignment | Policy ID/name/load identity, source preset reference, entries, default and assigned pawn references | Unchanged-catalog preset save/reload retains native edits and assignment; content repair, dose history and save switching remain unverified; no external migration |
| Inspected Cherry Picker removal branch | Historical evidence that removed/non-drug content can retain an object reference | Validate actual native catalog eligibility and owned S038 finalization; no original Cherry Picker API/coexistence obligation |
| Installed ASCII/UTF-16 scans | Historical scan of 5,485 DLLs; source versions and YART inventory references, no established callable incoming source API | Research record only; no further original API/consumer audit, facade, package/assembly/type/owner emulation or drop-in task |

Original API/drop-in compatibility and facade work are outside scope. The source's native-data use
does not prove universal safe removal or multiplayer compatibility. No source-specific settings are
imported. Native gameplay contracts, our own save continuity and genuine external integrations actually
used remain required. Ordinary successful repairs live in memory until the player saves; failed owned commits
restore prior row membership and values. Saved deletions carry no hidden restoration store.

## Implementation and coverage

| Behavior | Implemented here | Deviation / possible conflict | Missing / omitted | Targets and evidence |
| --- | --- | --- | --- | --- |
| DP-01–03, DP-08 | Pure reconciliation, atomic-write boundary and standalone native storage/catalog adapters, implemented-unverified; no reconciliation hook integration | Early cleanup, duplicate/null/stale recovery and native 1.7 defaults replace source's late/name-based gaps | Finalized applied-content authority, reconciliation targets and load hooks remain open; general corrupted-save repair, old deleted-choice recovery and physical item/health edits omitted | [Storage/catalog tests and native scenario dependency](../../Source/Features/DrugPolicyMaintenance/NativeStorage-Verification.md); reconciliation guards and native evidence pending |
| DP-04 initial preset recovery and unchanged-catalog persistence | Guarded initial native preset construction, native edits/assignment, ordinary save/reload, retained loaded choices/assignment and native restoration verified at this narrow scope | Ignore only unavailable entries before lookup/copy; retain native valid choices and underlying order without reapplying the preset over saved edits | Content-change reconciliation, post-preset normalization, dose history and save switching remain open; revised original-loaded warning/ordinary-settings-off handling is unverified, not a coexistence gate | [Preset target](../patch-targets/MM.DRUG.PresetEntries.md); 36 focused host checks reviewed; S029 tasks 2.5.3–2.5.4, reviewed 98-step group with 20 captures personally inspected by root |
| DP-05–06 | Nothing | Alphabetical presentation preserves job order; foreign dialog mods need shape admission | Source alphabetical job priority omitted | Identity/focus and two-drug native workflows pending |
| DP-07, DP-09 | Full composition and revised warning/ordinary-settings-off handling unfinished | Direct owned composition and best-effort feature off; original API/facade/coexistence work excluded | Unproven genuine external integration and multiplayer support deferred | Actual catalog/content timing, effective semantic guards and startup diagnostics pending |

Active original `Mlie.DrugPolicyFix` triggers a bounded startup warning and best-effort use of the
ordinary S029 per-feature settings disable path, with the same effective off behavior and timing as
manually off. Keep stored global/save preferences, the original and unrelated features intact;
installed-but-inactive sources do not trigger it. No original-provider compatibility run, API/facade
work or complete coexistence/suppression guarantee is required. Earlier exact-package suppression
and its Unit policy evidence are historical, not acceptance of the revised ordinary settings path.
Core maintenance does not require Cherry Picker. Harmony is now a required runtime dependency.
This preset slice adds no product XML mutation or XML Extensions dependency; its fixture-only preset
is staged solely for verification and never included in the product. Any future declarative operation
must obey guarded additive policy.

## Verification and acceptance

Shared initial catalog checkpointc091d3c was independently reviewed with2 focused Unit and12 Defs
checks. On805580c's reviewed build, initial preset controls passed43steps and ordinary startup-off
controls passed26steps. Root inspected all14 causal captures: valid preset choices, checkbox edit/
restoration and assignment, then ordinary native editing with preset filtering off while supporting
checks retained the same complete initial catalog. Full logs and cleanup passed; exact evidence is
`artifacts/Implementation/megamod-p0/s139-native-facts/catalog-native-verification.md`. This accepts
the changed initial publication path and scoped ordinary startup-off controls, not applied drug
exclusions, generator authority, original-provider suppression or full maintenance.

[Proposal/design/tasks](../../../../openspec/changes/specify-megamod-drug-policy-maintenance/) define
the implementation sequence and final acceptance. Only the initial preset native-controls task 2.5.3
and unchanged-catalog preset persistence task 2.5.4 are checked; the complete replacement remains
unfinished. Reviewed group `20261108T104425075Z-09a7bfcbed474ee385e2073e8ef3f831` passed both
scenarios and cleanups; root inspected all 20 captures. Exact deployed hashes, unchanged normal
configuration, unique-save removal and graceful exit were independently audited. The retained log
has no ERROR/exception and 25 classified warnings; its final shutdown stack ends midline, so a
complete final log tail is not claimed. Local evidence:
`artifacts/Implementation/megamod-p0/drug-native-preflight/persistence-verification.md`. Strict validation
and independent document review are recorded in the
[progress ledger](../refinement/specification-progress.md), separately from runtime acceptance.
DP-Q1–4 preserve remaining decisions/research. Final proof requires honest focused RED/GREEN, reviewed
build/package, exact supported content/integration lists and actual native policy/edit/save/assignment plus
pawn scheduled-use/refill behavior. Inspect resulting drug choice, timing and quantities personally;
logs/Def assertions alone are not acceptance. Finish balance/playability review on finalized
native/preset/content comparators, retain evidence and restore isolated run state. Original-provider
coexistence is not an acceptance matrix; bounded warning/ordinary-settings-off behavior remains an
open scoped obligation.
