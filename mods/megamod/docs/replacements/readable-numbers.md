# Readable Numbers

## Identity and provenance

- Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; origin: replacement.
- S034 **Readable Numbers**, **7h0m4s.RimworldReadableNumbers**, Workshop **3762441264**; About
  author 7h0m4s, Steam Fifbef/DubiousPhysics. Selected 1.7 DLL, advertised 1.6/1.7, inspected 2026-11-03.
- [Research](../refinement/2026-11-03-readable-numbers-research.md) pins all 26 C# files, 30 matching
  source/metadata inputs, selected compiled paths, native UI interface and consumed RimHUD method.
  GPL-3.0 source; no source code/DLL/art copying. No live beta acceptance is implied by decompilation.
- [Owned change](../../../../openspec/changes/specify-megamod-readable-numbers/proposal.md) specifies
  future behavior only. **Nothing implemented or accepted.**

## Behavior inventory

| ID | Action → visible result | Defaults / edge boundaries | Evidence and contract | Status |
| --- | --- | --- | --- | --- |
| RN-01 | Read ordinary quantities → grouped digits | Enabled, groups3, comma/period; preserve precision/sign/units/value | Processing/Text/Validation; [capability](../../../../openspec/changes/specify-megamod-readable-numbers/specs/megamod-readable-numbers/spec.md) | Drafted |
| RN-02 | Choose grouping/punctuation → selected representation | Four grouping modes, six presets/custom, nonbreaking space, atomic valid custom pair | RnSetting + owned validation correction; capability | Drafted |
| RN-03 | Set threshold/small decimals → each number follows its own rule | Threshold4, small decimals on; no whole-label eligibility leak | Source/compiled threshold discrepancy; capability | Drafted correction |
| RN-04 | Add/remove exclusions → entire matching label remains raw | Literal case-sensitive, overlap correct, empty/duplicate rejected, exact persisted entries | Validation/setting persistence; capability | Drafted correction |
| RN-05 | Edit inputs/read names/dates/debug text → original meaning retained | Contextual exclusions, no name/identifier digit extraction or input/caret changes | Native patches/backend and intended broader protection; capability | Drafted |
| RN-06 | Measure/draw styled quantities → consistent usable UI | Quest/map/HUD, markup attributes preserved, no double conversion, safe fallback | Backend/reward/map hooks and current RimHUD; capability | Drafted |
| RN-07 | Change preferences/saves/reset → correct current display | Common inheritance, next redraw, full default reset, main-menu restrictions | Global-only source differs; capability | Drafted |
| RN-08 | Use optional displays/co-installed original → isolated coherent behavior | Optional contracts, owned direct calls, source-overlap suppression, bounded warnings | Source adapters/current boundary reads; capability/design | Drafted; provider/native proof pending |

## Interactions and composed result

Use one owned formatter for quantity presentation in future RimHUD, search, material counts and
bill displays. Formatting cannot change query definitions, counts, materials, capacity, jobs or
targets. Do not patch owned UI or create independent per-source formatting caches. A bounded text
cache stores presentation and is separate from the shared map Thing index.

S127 RimHUD's styled decimals are a real consumed source interaction; integrate directly in owned
code. External RimHUD, Toggleable Readouts, Dawn of a New Day and Dubs Performance Analyzer remain
optional adapters, not hard dependencies or accepted universal compatibility.

## UI audit and relocation

| Source control/surface | Owned destination and intentional differences |
| --- | --- |
| Global ModSettings formatting controls | Common Number formatting section with global/default/save rows, previews and exact scoped reset |
| Blacklist text/add/checkbox-removal | Explicit exclusions add/remove UI; literal matching and whole-list save overrides |
| Debug logging checkbox | Top debug toolbar diagnostics, default off; no ordinary diagnostic setting |
| Cache enable/capacity controls | Deliberately omitted from player configuration; bounded internal design with developer diagnostics only |
| Analyzer.xml profiling group | Developer integration only, no new gameplay bottom tab |
| Game text/backend rendering | Existing contextual/native displays, no new bottom tab, gameplay window or bottom-right toggle |

Owned corrections: per-number threshold; valid distinct custom marks; complete grouping reset;
correct exclusion overlap/storage; immediate text/width refresh; names/input/date/debug protection;
structured rich-text handling and disabled-feature consistency for HUD decimal punctuation. These
deviations can differ from source screenshots or permissive invalid custom inputs and are explicit.
Punctuation mode/custom pair inherit together and retain custom values across presets/reload.
Global reset restores built-ins; current-save Use global for all restores inheritance and retains
dormant overrides, without rewriting globals or promising built-in effective values.

## Consumed contracts and persistence

| Source contract | Treatment / outstanding proof |
| --- | --- |
| Public Processing/Text/Validation/Patching utility types and suppression flag | No direct external source caller found in bounded text audit; binary/reflected consumers still RN-Q2, no facade or assembly-identity claim |
| IGuiBackend forwarding | Native beta shape is volatile; guard isolated integration, avoid type-load failure before admission |
| RimHUD.Extensions.TextExtensions.ToDecimalString(int,int) | Actual current structured integer/remainder output inspected; preserve source precision/theme choice, apply owned preferences coherently |
| Toggleable Readouts valueLabel; Dawn DrawText; Analyzer panel catalogue | Source adapter contracts identified; actual current provider paths/absent-present cases remain RN-Q2 |
| Global source settings keys and pipe-delimited blacklist | New common owned settings; no automatic source migration, literal list entries round-trip in owned storage |

Harmony is required for planned native hooks. No DLC/source mod dependency or current XML patch.
Any future XML Extensions use adds its required dependency and guarded merging. Source coexistence
suppresses the overlapping owned formatter with a warning, preserving other owned actions. Optional
failure disables only the adapter when safe; unsafe generic interception must be disabled as a group.

## Implementation and coverage

All RN-01–08 code, target guards, tests, translation/live UI review, performance and native evidence
remain **pending**. Ordinary cache-capacity/enable controls and package/API impersonation are
**deliberately not adopted**. Automatic source settings/save migration is **not proposed**.
The source's broad text coverage is a future owned implementation requirement for supported ordinary
quantities, not a claim that every custom renderer/provider syntax already works.

## Verification and acceptance

[Progress](../refinement/specification-progress.md) records document validation/review; RN-Q1–4
retain native target, optional consumer, representation/performance and implementation evidence.
This draft needs no game launch and closes no runtime task. Final acceptance requires actual UI
actions and personally inspected text/layout/input results on the reviewed build, including disabled,
source/provider presence, settings/save switching, seven-language and scale cases. Semantic pass:
display punctuation only; underlying quantities, bills, names and input semantics stay unchanged.
