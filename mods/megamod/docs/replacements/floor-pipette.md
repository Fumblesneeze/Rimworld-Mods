# Floor Pipette Tool

## Identity and provenance

Owner Make Rimworld Great Again (`fumblesneeze.megamod`, `mods/megamod`),1.7 beta. Replacement S136.
`ferny.FloorPipetteTool`, [Workshop3706877822](https://steamcommunity.com/sharedfiles/filedetails/?id=3706877822),
assembly1.0.0.0 SHA256 `e00a7eb6f2f29bb79390927a6841e76bd4755cd67f0bbc9047b3a1b37cacd237`.
Complete supplied/binary source, public commit and focused native1.7 inspected2026-11-07.
[Research](../refinement/2026-11-07-floor-pipette-research.md) pins hashes,1.6-folder fallback, asset
pack, native rules and limitations. Credits ferny, Taranchuk(C#), Oskar Potocki(art); no reuse license
found. Independent implementation/art, no copied source binary/texture/translation.

## Behavior inventory

All rows refer to the [owned observable spec](../../../../openspec/changes/specify-megamod-floor-sampling/specs/megamod-floor-sampling/spec.md).

| ID | Action and visible result | Boundaries | Status |
| --- | --- | --- | --- |
| FPT01 | Pick floor, hover and click → exact construction tool | Sampling gesture creates no blueprint/removal/paint; separate construction input | Specified, unimplemented |
| FPT02 | Resolve current exposed Floors terrain | Native current research/rules, S038 exclusions, variants, no paint/buried-layer copying | Specified, unimplemented |
| FPT03 | Invalid sample explains why | No fog information; current map/cell check; no silent unrelated tool | Specified, unimplemented |
| FPT04 | Existing Architect action with one-shot reveal | S036 paging, native color picker, no Cancel shortcut collision or new setting/tab | Specified, unimplemented |
| FPT05 | Guarded entry and fallback | Preserve siblings, source suppresses owned overlap, bounded startup warning | Specified, unimplemented |

## Interactions and UI audit

Source and owned action live in Architect/Floors; it is a contextual command, not a bottom main
tab, settings row, bottom-right switch or cheat. Selection remains transient UI state. Direct S036
resolution/reveal, S038 eligibility and S067 removal separation; no self-patches or extra item cache.
Source's C/Cancel binding deliberately omitted, fog hover corrected, click admission unified and
one-shot reveal added. Native colored-floor sampling and paint/removal tools remain intact. New UI
supports seven languages; independent owned icon must pass the repository asset/native visual gate.

## External contracts and persistence

Public source Designator type and English keys are observed; no actual installed consumer found by
bounded text scan. No proven API facade required, no package spoof or source settings import.
No source saved state. Native tool/blueprint state stays native; no persistent sampled cell or type.
Source co-installation withholds owned sampler without touching original controls/patches.

## Implementation and coverage

**Nothing implemented.** [Proposal/design/tasks](../../../../openspec/changes/specify-megamod-floor-sampling/)
cover the complete source sampling behavior plus documented corrections. Differently categorized
terrain, hidden layers, paint cloning and planned floors are deliberately omitted. Original icon,
empty PatchAll and unguarded XML are not copied. Core/XML Extensions required for guarded additive
entry; no Harmony requirement solely for this feature, no other repository mod or Gateway dependency.
Optional floor definitions use native current availability; changed providers are not bypassed.
Target documents/executable checks and live evidence remain future tasks.

## Verification and acceptance

FPT-Q1 resolved; FPT-Q2–4 tracked in [progress](../refinement/specification-progress.md).
Focused tests, package checks and independent review precede final fresh minimized Gateway native
sample/placement/build/cancel workflows. Personally inspect causal screenshots, current rules/fog,
paused context, variants/paging/provider cases and seven-language/art legibility. Preserve exact
build/mod/config hashes and cleanup. No research/cost/time discount accepted; source/static evidence
and startup logs cannot establish runtime compatibility or gameplay acceptance.
