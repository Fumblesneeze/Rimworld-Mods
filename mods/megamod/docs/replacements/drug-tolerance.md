# Only If 'Tolerance' Below in Drug Policy — S138

## Identity and provenance

- Owner: Make Rimworld Great Again (`fumblesneeze.megamod`, `mods/megamod`), target 1.7 beta.
- Origin: replacement; independent behavior implementation, with explicit proposed Any/zero fixes.
- Source: SeoHyeon, [Workshop 3088210509](https://steamcommunity.com/sharedfiles/filedetails/?id=3088210509),
  package `seohyeon.onlyiftolerancebelow` (About uses mixed case).
- Inspected 2026-11-08: installed `F:/Steam/steamapps/workshop/content/294100/3088210509`, selected
  `1.7/Assemblies/DrugPolicyEnhanced.dll`, SHA256
  `6C48ACE04DECFEDD65549489046CA5E23B7B40B2DD033331C9BB6E68AA7F6F29`.
- MIT source, copyright 2023 SeoHyeon; no source/binary/art/translation copied. Public author links,
  complete source hashes and native seams are in the [research](../refinement/2026-11-08-drug-tolerance-research.md).
- Status: choice/decision and policy-state components implemented-unverified; native hooks/UI and
  player workflows remain unfinished. Source observations are evidence;
  Any/zero semantics, distinct numeric 100%, invalid-state behavior and feature configuration are
  proposed owned decisions. No source API, saved-state import or original-mod compatibility work.

## Behavior inventory

All scenarios belong to the [tolerance capability](../../../../openspec/changes/specify-megamod-drug-tolerance/specs/megamod-drug-tolerance/spec.md).

| Behavior ID | Native input/action → observable result | Options/defaults and edge cases | Evidence | Owning scenario | Status |
| --- | --- | --- | --- | --- | --- |
| DT-01 | Edit a policy row → choose Any or numeric tolerance | New row Any; numeric 100% distinct; inactive choices retained | Research UI/source; proposed correction | Open a new applicable row; Turn off routine uses | Drafted |
| DT-02 | Pawn considers routine use → strict current-tolerance limit | Missing hediff zero; equality rejects; zero blocks; Any unrestricted by this feature | Research predicates; proposed Any/zero correction | Boundary and absent tolerance; Any differs from numeric 100 percent | Drafted |
| DT-03 | Scheduled use with S024 → all conditions hold | No new interval, amount or history change | Research native seam and S024 | Mood or interval still rejects; Tolerance has fallen | Drafted |
| DT-04 | Recreation/treat → recipient rule and native joy exceptions | No visitor-policy bypass; native joy exceptions do not extend to patient feeding | Research and direct owned composition | Visitor could take a drug that the patient cannot; Patient trait does not grant a new feeding exception; Joy exception does not become scheduled permission | Drafted |
| DT-05 | Medical/manual/refill → existing route authority | No universal tolerance veto, dose grant or extra claim | Source scope and S124/S134 | Needed dependency assistance; Manual order or carried supply | Drafted |
| DT-06 | Copy, repair, save/load → retain actual choices | S029 valid row identity; new/returned rows Any | Source copy/save seams; proposed owned schema | Copy and edit independently; Content repair and return after saved removal | Drafted |
| DT-07 | Load malformed choice → visible repair needed | Withhold threshold-governed routine use; absent field Any | Proposed conservative behavior, not source parity | Invalid saved number; Older owned save lacks the new field | Drafted |
| DT-08 | Disable/unavailable/original active → honest ordinary fallback | Proposed enabled by default; retain stored values; bounded best-effort warning | Common settings and latest user scope | Disable and re-enable in a loaded save; Required behavior is unavailable; Original is active | Drafted |

## Interactions and composed result

S029 owns current row/catalog identity, surviving choices, copy and atomic maintenance. S138 adds
one owned choice to those boundaries; maintenance never regenerates it. S024 scheduled potential
mood and tolerance are conjunctive, while patient treats retain recipient joy permission and add
the numeric veto without new self-recreation trait/mental-state exceptions. S124/S134 retain their medical eligibility, dose history and shared pending-chemical
authority. S139 can later propose values through this owned state, not an upstream adapter.
Source postfixes disappear into directly composed owned decisions; no self-patches or generic
all-ingestion veto. Detailed composed scenarios are in the capability linked above.

## UI audit and relocation

| Source entry point / implementation | Purpose | Class | Megamod destination and rationale | Intentional change |
| --- | --- | --- | --- | --- |
| Injected native drug-policy column | Choose tolerance preference | Gameplay policy | Existing native row | Distinct Any/numeric 100%; no copied fixed-width reduction |
| Source copy/save hooks | Retain row choice | Gameplay state | Native policy lifecycle through S029 | Owned schema and explicit invalid-state presentation |
| No source bottom tab or settings window | No navigation to preserve | Not applicable | No new tab | Common settings hosts only the owned feature enable switch |

`drugTolerance.enabled` is proposed on, Drug policies section before generation, through common
Global default/Use global/Current save controls. Inherited saves resolve the current global value;
dormant overrides remain, and all save controls/callbacks are disabled at main menu. Effect timing:
next eligibility/editor refresh; no cancellation or reversal of an already executing dose. Exact
save switching/world view and seven-language disabled/control layout require native verification.
Individual row choices are save-local gameplay state and never use configuration inheritance.

## Own persistence and external integrations actually used

| Owned state or integration | Required contract | Behavior/errors/persistence | Limits | Verification |
| --- | --- | --- | --- | --- |
| Policy row tolerance choice | Own version-1 Any/numeric/invalid codec and S029 row/rollback boundary; native copy/Scribe admission remains DT-Q1 | Component copy and rollback retain immutable choices and raw unknown data; absent Any | No original field import; no hidden deleted-row archive; no native save claim yet | Focused Unit/Defs evidence; native copy, repair and save/reload still pending |
| Common enable preference | C02 global/save/inheritance and game lifecycle | Preserve dormant overrides and row choices while off | No previous-save callback; next decision only | Main menu and save switching |
| Native scheduled/joy routes | Current Core semantics and required Harmony | Narrow successful native choices; fail feature unavailable on unknown semantics | No medical/manual/refill expansion | Actual blocked/allowed native use |

No genuine external adapter is currently required. Optional drug content uses current native
capabilities; unsupported pharmacology is not guessed. Original APIs, consumers, identities, save
imports and coexistence/drop-in tests are excluded. A later owned-schema migration requires a
separate backup/staging/atomicity/restoration/postcondition contract before implementation.

## Implementation and coverage

| Behavior | Implemented portion | Deviation / conflict | Missing / omitted | Target/tests/live evidence |
| --- | --- | --- | --- | --- |
| DT-02–04, DT-07–08 | Host-safe strict comparison, route-specific exception projection, explicit unknown-reader/Needs-review outcomes and lossless choice codec | Any differs from numeric 100%; missing hediff zero; native rejection remains; no medical/manual/refill route exists in this projection | Native reader, route proof, effective-body admission, controls and all player workflows | 56 affected Unit checks; no live acceptance |
| DT-06 | Exact native-row weak state; S029 snapshots, first-duplicate retention, normalization, new-row Any and rollback; independent component copy | State follows surviving row identity rather than policy/drug name; deleted rows have no restore archive | Admitted native CopyFrom/ExposeData hooks and actual save/load/copy acceptance | 11 affected Defs checks; no live acceptance |
| DT-01, DT-05, remaining DT-08 | None | Medical scope stays separate | Native editor, common setting, bounded original warning, route/medical workflows and seven-language presentation | [Design/tasks](../../../../openspec/changes/specify-megamod-drug-tolerance/); target docs and native tests future |

Active original `seohyeon.onlyiftolerancebelow`: bounded startup warning plus best-effort suppression
of `drugTolerance.enabled` through the ordinary effective-off path, with the timing/limitations
above. Stored preferences are not rewritten; installed-inactive does nothing; unrelated features
remain. Do not modify/unpatch the original or promise rollback of all effects. No original-provider
compatibility matrix. Core/Harmony required; Prepatcher/source mod are not dependencies; no planned
XML/XML Extensions or product Gateway dependency. Native semantic admission still required.

## Verification and acceptance

The 2026-11-11 component tranche has nonzero focused RED/GREEN in honest Unit and Defs tiers;
the local ledger is `artifacts/Implementation/megamod-p0/s138-state-batch/handoff.md`.
It does not install any tolerance decision, editor, copy or serialization patch. DT-Q1–4 retain
their unfinished native integration obligations, with the concrete state contract now pinned.
Independent review and integrated product checks remain separate evidence. The
orchestrator must later personally observe the reviewed build's actual native policy edits, routine
consumption/refusal, patient treats, medical independence, persistence and configuration transitions
in minimized isolated Gateway runs. Include focused warning/effective-off cases, seven languages,
exact build/mod/config identity, causal captures, complete relevant logs and cleanup. No general
compatibility or addiction-safety claim follows from source inspection. Balance/job-reachability
review and coverage updates remain pending.
