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
- Status: choice/decision/state and native persistence components have focused implementation
  evidence; root's persistence-only native gate is recorded separately. Revision21 authors the
  unregistered enforcement/editor/runtime packet and seven catalogs, pending independent review,
  geometry composition, owning compilation and native player workflows. Source observations are evidence;
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

`drugTolerance.enabled` is authored on, Drug policies section before generation, through common
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
| DT-02–04, DT-07–08 | Host-safe comparison/codec, native reader, guarded selection/social patches, direct-joy exception proof and registered atomic runtime | Any differs from numeric 100%; missing hediff zero; native rejection remains; no medical/manual/refill hook | Further numeric boundaries, shared chemicals, caravan, refill and future S024 recipient call | Reviewed native scheduled/direct-joy/social zero/Any and positive-trait routes, drinkless social recreation and explicit manual Drink exclusion; no full S138 acceptance |
| DT-06 | Exact native-row weak state, S029 capture/restore and native four-target copy/Scribe support | State follows row identity; deleted rows have no restore archive | Persistence-only root gate does not prove the new editor or routine selection | Root evidence: `artifacts/Implementation/megamod-p0/revision20-native-verification.md` |
| DT-01, DT-05, remaining DT-08 | Reviewed drafts/layout/typed bridge, contextual Window, common setting/provider policy, atomic runtime and seven catalogs | Requested preference remains editable while effective capability is unavailable; medical scope stays separate | Original suppression, save override/switching, remaining route exclusions and seven-language presentation | Reviewed English native Any/zero/100/decimal/invalid input and Apply/Cancel; global off/on preserves the executing dose and restricts the next selection; [design/tasks](../../../../openspec/changes/specify-megamod-drug-tolerance/) retain wider obligations |

Active original `seohyeon.onlyiftolerancebelow`: bounded startup warning plus best-effort suppression
of `drugTolerance.enabled` through the ordinary effective-off path, with the timing/limitations
above. Stored preferences are not rewritten; installed-inactive does nothing; unrelated features
remain. Do not modify/unpatch the original or promise rollback of all effects. No original-provider
compatibility matrix. Core/Harmony required; Prepatcher/source mod are not dependencies; no planned
XML/XML Extensions or product Gateway dependency. Native semantic admission still required.

## Verification and acceptance

The 2026-11-11 component tranche has nonzero focused RED/GREEN in honest Unit and Defs tiers;
the local ledger is `artifacts/Implementation/megamod-p0/s138-state-batch/handoff.md`.
That original tranche installed no native hooks; the later root-owned persistence group and its
scoped acceptance are recorded in the revision20 evidence above. The revision21 enforcement/editor
packet is reviewed and registered, with its manifests and focused receipts in
`artifacts/Implementation/megamod-p0/s138-revision21/`. The English editor workflow passed a reviewed
78-step run after correcting inherited native-method identity. The subsequent scheduled and direct
recreation workflows passed101/96steps; root inspected all19 captures of physical Beer use or
retention, actual drinking and resulting native Health. Any, strict zero, the direct-only positive
DrugDesire exception and explicit manual Drink exclusion are observed scoped results. Exact builds,
actions, process identities, full logs and cleanup are retained in the revision21 ledger and
`artifacts/Implementation/megamod-p0/s138-native-routes/native-verification.md`.
The next reviewed batch at7899799 passed96 social steps and153 settings-transition steps. Root
inspected all26 causal captures: native table/chair social recreation continued without a drink
for both zero-limit pawns, while Any physically drank; native global off/on preserved an executing
dose and restricted the next selection. Exact evidence, including the corrected fixture selection,
is under `s138-native-social-routes/native-verification.md` and
`s138-native-settings-routes/native-verification.md` in the same local evidence root. These are
scoped social and global-setting results; party events and other configuration/lifecycle cases remain.
DT-Q1–4 retain their remaining obligations; these slices do not accept all native routes or S138.
The reviewed positive-boundary A run at805580c passed61steps/all8 root-inspected captures: an
actual native decision rejected seeded20% equality, then native decay enabled an ordinary later
same-Beer ingestion. Exact evidence is `s138-native-boundaries/positive-native-verification.md`
under the same local root. Initial seeded tolerance is not natural-accumulation evidence; Any/100,
shared chemicals and remaining route/lifecycle gates are still open.
Independent review and integrated product checks are retained separately from live evidence. The
orchestrator must personally observe the reviewed build's actual native policy edits, routine
consumption/refusal, patient treats, medical independence, persistence and configuration transitions
in minimized isolated Gateway runs. Include focused warning/effective-off cases, seven languages,
exact build/mod/config identity, causal captures, complete relevant logs and cleanup. No general
compatibility or addiction-safety claim follows from source inspection. The exercised inventory,
routine ingestion and manual routes were reachable without changing cost, cadence or supply rules;
no refill, optimal-schedule or broader pharmacology claim follows. Remaining balance and composed
job-reachability review is pending with the unrun workflows.
