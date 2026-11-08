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
- Status: drafted specification, unimplemented and unverified. Source observations are evidence;
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
| Policy row tolerance choice | Own Any/numeric/invalid codec; S029 row/copy/rollback boundary, unresolved DT-Q1 | Valid copy/load retained; absent Any; malformed visibly withheld pending explicit repair | No original field import; no hidden deleted-row archive | Copy, repair and save/reload scenarios |
| Common enable preference | C02 global/save/inheritance and game lifecycle | Preserve dormant overrides and row choices while off | No previous-save callback; next decision only | Main menu and save switching |
| Native scheduled/joy routes | Current Core semantics and required Harmony | Narrow successful native choices; fail feature unavailable on unknown semantics | No medical/manual/refill expansion | Actual blocked/allowed native use |

No genuine external adapter is currently required. Optional drug content uses current native
capabilities; unsupported pharmacology is not guessed. Original APIs, consumers, identities, save
imports and coexistence/drop-in tests are excluded. A later owned-schema migration requires a
separate backup/staging/atomicity/restoration/postcondition contract before implementation.

## Implementation and coverage

| Behavior | Implemented portion | Deviation / conflict | Missing / omitted | Target/tests/live evidence |
| --- | --- | --- | --- | --- |
| DT-01–08 | None | Explicit Any/zero and malformed-state corrections; medical scope stays separate | All runtime/UI/storage work pending | [Design/tasks](../../../../openspec/changes/specify-megamod-drug-tolerance/); target docs and tests future |

Active original `seohyeon.onlyiftolerancebelow`: bounded startup warning plus best-effort suppression
of `drugTolerance.enabled` through the ordinary effective-off path, with the timing/limitations
above. Stored preferences are not rewritten; installed-inactive does nothing; unrelated features
remain. Do not modify/unpatch the original or promise rollback of all effects. No original-provider
compatibility matrix. Core/Harmony required; Prepatcher/source mod are not dependencies; no planned
XML/XML Extensions or product Gateway dependency. Native semantic admission still required.

## Verification and acceptance

No runtime tests, build or game run belong to this draft. DT-Q1–4 block their affected implementation
slices. Focused RED/GREEN must use honest Unit/Harmony/Defs tiers and independent review. The
orchestrator must later personally observe the reviewed build's actual native policy edits, routine
consumption/refusal, patient treats, medical independence, persistence and configuration transitions
in minimized isolated Gateway runs. Include focused warning/effective-off cases, seven languages,
exact build/mod/config identity, causal captures, complete relevant logs and cleanup. No general
compatibility or addiction-safety claim follows from source inspection. Balance/job-reachability
review and coverage updates remain pending.
