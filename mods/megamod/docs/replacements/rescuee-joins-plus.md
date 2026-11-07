# RescueeJoinsPlus replacement coverage

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin replacement **S108/P2-38**. Ogliss, `Ogliss.RescueeJoinsPlus`,
[Rescuee Joiner Plus](https://steamcommunity.com/sharedfiles/filedetails/?id=2177170403).
Selected installed1.6 DLL1.0.0.0 and older public1.4 source inspected separately.
**Nothing implemented or accepted in game.**

[Research and exact identities](../refinement/2026-11-05-rescuee-joins-plus-research.md),
[proposal](../../../../openspec/changes/specify-megamod-rescuee-join-consent/proposal.md),
[design](../../../../openspec/changes/specify-megamod-rescuee-join-consent/design.md),
[observable spec](../../../../openspec/changes/specify-megamod-rescuee-join-consent/specs/megamod-rescuee-join-consent/spec.md),
[unchecked tasks](../../../../openspec/changes/specify-megamod-rescuee-join-consent/tasks.md).

## Behavior inventory

| ID | Input/action/result | Selected boundaries/deviations | Evidence/status |
| --- | --- | --- | --- |
| RJP01 | Willing guest recovers → consent before joining | Native eligibility/chance; carried context; no god-mode forced willingness | Full guest patch and native route; specified |
| RJP02 | Offer help/Free prisoner → rescue-promise consent | Real native job/path and promise rules; defer joining-only tail | Full promise patch/native caller; specified |
| RJP03 | Inspect request → ordinary biography/relations/restrictions/traits/passions/info card | Correct empty/missing data, visibility and race text; no HAR requirement | Full formatter and native dialog; specified |
| RJP04 | Accept → one route-correct joining outcome | Correct letter roles, real membership and no early rewards | Source/native faction/recruitment trace; specified |
| RJP05 | Reject → remain unjoined under ordinary rescue/site rules | Truthful message, consumed chance, no new penalty or teleport | Source gaps plus explicit correction; specified |
| RJP06 | Concurrent requests/save/context change → isolated pending or stale outcome | First valid route retained; partial effects before/after membership resolve incomplete without replay; durable identity and no cross-save state | Owned robustness requirement; source has no request persistence |
| RJP07 | Compatible/incompatible provider → admitted route or explained fallback | Original owns overlap; semantic guards, bounded per-patch warnings | Foundation composition; unimplemented |

## Interactions and composed result

S058 rescue ordering and native eligibility remain authoritative; S031 child care is unaffected.
S109 WanderJoinsPlus will have its own event contract; share useful presentation directly without
self-patches. Ordinary prisoner recruitment, ritual joiners and unrelated visitors are outside this
scope. Preserve independent physical rescue facts and real quest rules, while delaying recruitment
effects until accepted. Do not describe rejection as universally quest-penalty-free.

## UI audit and relocation

| Source entry | Purpose | Owned placement/change |
| --- | --- | --- |
| Forced-pause dialog, More info, Accept, Reject | Decide whether this rescuee joins | Contextual gameplay dialog; native one-second delay, clear outcome, ordinary info card |
| Summary headings and race branch | Inform decision | Current visible native information, safe missing data, all seven baseline languages |
| Positive letter / neutral rejection message | Report outcome | Correct title/body and timing, accurate target and refusal feedback on both routes |
| Developer startup patch count | Diagnostics | Foundation startup diagnostics; no gameplay-facing technical details |
| Mod settings / bottom tab / view toggle | None | None introduced; request is pawn-specific gameplay state |

No configuration setting needs global/save inheritance here. Pending/resolved decisions persist with
their game where required; window state and callbacks do not. No new custom asset is selected.

## Contracts consumed by other mods or saves

| Binding | Evidence and policy |
| --- | --- |
| Public RescueeJoinPlus helper/patch classes and HAR boolean | Bounded installed scan found no external consumer; no speculative facade/package spoof |
| Source native faction/guest/pawn data | Keep ordinary native persistence; no retroactive reconsideration or automatic removal of existing colonists |
| Source custom settings/save schema | None found; no implicit migration |
| Optional HAR or quest/race content | Package/shape-admitted adapter only for a concrete need; base summary uses native pawn data |
| New owned request schema | Must validate saved identity, stale choices and completed outcomes; no old source dialog restoration claim |

Public source at Ogliss/AskJoinRescuee is older than installed1.6; no license found in inspected material.
Credit Ogliss and independently implement behavior. Copy no source code, text, art or bundled Harmony.

## Implementation and coverage

**All behaviors remain unimplemented.** Preserve two consent routes and summary/info-card access.
Intentionally correct carried-map assumptions, incomplete refusal, wrong letter roles, premature
quest-success text, empty passions, hardcoded pronouns and unsafe duplicate/lifecycle handling.
Omit the source's god-mode forced willingness. No general joining framework, new recruitment chance,
ritual event interception or ordinary prisoner-recruitment rewrite is selected.

Core/Harmony required at implementation; no XML change/new XML Extensions dependency. Original active
provider suppresses owned overlap with per-patch diagnostics. Compatible unrelated patches remain
allowed. Failure leaves the affected native route in force, which can still auto-join new events;
do not claim protection when disabled. Captured owned requests never silently auto-accept on failure.

## Verification and acceptance

Source/native analysis and the three recruitment-route comparisons establish the draft's provenance,
not runtime compatibility or acceptance. Independent behavior findings were resolved and focused
re-review found no further issues; provenance review and its normalization clarification are recorded
in the sequential ledger. Strict validation passes130 items, zero failed.
RJP-Q1–4 retain effective continuation, lifecycle, provider and native acceptance work. Future proof
requires real rescue/tending or Offer help/Free prisoner → request/info card → Accept/Reject → observed
membership/letter/guest/quest outcomes on the reviewed build. Include carried recovery, simultaneous
requests, stale/save cases, blocked departures, all languages and exact provider combinations.
Retain causal screenshots, package identity, config hashes and cleanup; startup/tests alone do not accept it.
