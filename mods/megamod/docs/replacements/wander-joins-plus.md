# Wander Joiner Plus

## Identity and provenance

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin **replacement**, queue **S109**. Source **Ogliss.WanderJoinsPlus**, installed title
WanderJoinsPlus; [Workshop Wander Joiner Plus](https://steamcommunity.com/sharedfiles/filedetails/?id=2177016016)
by Ogliss, item2177016016. Inspected 2026-11-05.

Selected 1.6 DLL version1.0.0.0, 32,768 bytes, SHA-256
`C6E9EC5DCFD4A92151794EDCC80B9CF483AE8895ED4CB2B1A8131B5DF8FBEA7A`;
[public source](https://github.com/Ogliss/AskJoinWanderer/tree/804127b9dafe142090a183d595396ca0cc6f716b)
is older1.4, not this binary. No source license found; independent implementation only, no assembly
or asset redistribution. [Research](../refinement/2026-11-05-wander-joins-plus-research.md) records
all five current classes, six historical files, XML/localization, native13 types, optional VEE4
types, exact hashes, attribution and consumer limitations. No live behavior has been verified.

## Behavior inventory

All rows are **specified, not implemented** in the
[owned change](../../../../openspec/changes/specify-megamod-wanderer-offer-details/).

| ID | Player action → result | Defaults / edge cases | Evidence and observable contract |
| --- | --- | --- | --- |
| WJP-01 | Open a wanderer offer → added work restrictions, traits, passions and information action | Same pawn, original title/context; explicit None/unavailable | Current formatter/subclass; [information](../../../../openspec/changes/specify-megamod-wanderer-offer-details/specs/megamod-wanderer-join-offers/spec.md#requirement-show-ordinary-visible-pawn-information) |
| WJP-02 | Open More information → native card, then return to same offer | No membership/spawn/hidden-state side effect | Current subclass; [contextual UI](../../../../openspec/changes/specify-megamod-wanderer-offer-details/specs/megamod-wanderer-join-offers/spec.md#requirement-contextual-controls-and-localized-presentation) |
| WJP-03 | Accept/reject → original event's outcome | Walk-in arrives on acceptance; native refusal/charity preserved | Native quest/choice parts; [decisions](../../../../openspec/changes/specify-megamod-wanderer-offer-details/specs/megamod-wanderer-join-offers/spec.md#requirement-native-decisions-keep-their-distinct-consequences) |
| WJP-04 | Inspect abasia offer → illness/care/navigation remain | Royalty optional; pods can arrive before consent; no cure/automatic rescue | Royalty Defs and native root; [event identity](../../../../openspec/changes/specify-megamod-wanderer-offer-details/specs/megamod-wanderer-join-offers/spec.md#requirement-enrich-identified-offers-without-changing-their-event) |
| WJP-05 | Postpone/reopen/reload → same remaining time | Native60000-tick day; preserve actual provider deadline; no forced untimed rescuee dialog | Native LetterWithTimeout; [timing](../../../../openspec/changes/specify-megamod-wanderer-offer-details/specs/megamod-wanderer-join-offers/spec.md#requirement-preserve-the-original-deadline-and-postponement) |
| WJP-06 | Inspect admitted VEE offer → ordinary information, hidden behavior retained | Effective generator dispatch unresolved; no secret repair or universal support | Advertised intent, selected1.7 provider; [optional scope](../../../../openspec/changes/specify-megamod-wanderer-offer-details/specs/megamod-wanderer-join-offers/spec.md#requirement-optional-content-retains-its-own-authority) |
| WJP-07 | Revisit/switch/load → exact identity or unavailable details | No archived decisions, guessed pawn or automatic source-save migration | Source Scribe/type; [save boundary](../../../../openspec/changes/specify-megamod-wanderer-offer-details/specs/megamod-wanderer-join-offers/spec.md#requirement-save-boundaries-and-missing-information-remain-honest) |
| WJP-08 | Unsupported target/provider → original offer, bounded startup warning | Compatible foreign patches allowed; no partial rewrite or duplicate controls | Owned correction; [compatibility](../../../../openspec/changes/specify-megamod-wanderer-offer-details/specs/megamod-wanderer-join-offers/spec.md#requirement-failed-compatibility-preserves-the-existing-offer) |

## Interactions and composed result

S108 RescueeJoinsPlus can share ordinary information formatting through direct owned calls, but
its separate consent request does not replace existing S109 timing or denial/charity effects.
Native events own generation, arrival and membership. VEE's concealed future behavior remains
provider-owned, not a self-patch or copied event. Current VEE generation dispatch remains unproven;
an unsupported adapter leaves that provider route alone. To avoid revealing a concealed event by
missing controls, unavailable active VEE support also withdraws added presentation uniformly from
comparable Core walk-in offers, including pending/reloaded offers and later invalidation. Distinct
abasia enhancement remains independent. No duplicate pawn indexes or Thing caches.

## UI audit and relocation

| Source entry point | Purpose | Owned destination / correction |
| --- | --- | --- |
| Native joiner letter and injected stats | Read offer before deciding | Same contextual letter, preserve event text and deadline; no bottom tab |
| More info | Inspect actual offered pawn | Native information card; explicit action after Accept/Reject in standard layout |
| Archived letter | Read past event | No revived decisions; current source adds no card to its single Close option |
| Historical paused choice dialog | Consent for older direct incidents | Not adopted as a second/current beta prompt |
| Startup DevMode log / dormant helper | Diagnostics/dead source path | No product UI feature or English about suffix |

No source settings or bottom controls. Pawn/event choices are contextual state; no Use global row
or main-menu editing. Seven baseline languages, complete long-content access and ordinary hidden
information policy are required. No runtime artwork is selected.

## Contracts consumed by other mods or saves

| Consumer / evidence | Contract | Decision |
| --- | --- | --- |
| Saved letters, including archives | WandererJoinPlus.ChoiceLetter_AcceptJoinerPlus; joiner plus inherited native saved fields | Unsupported legacy migration; do not claim source can safely be removed or that resolving pending letters is sufficient |
| Native quest and optional VEE | Original letter/pawn/quest, decision signals, time, map, visibility and provider state | Preserve through narrow admitted enrichment; no global letter-class replacement |
| Bounded installed-consumer scan | Only source files matched package/type strings; About loadBefore Vanya.Tools.IncidentPersonStat | No proven external facade; scan limitations retained; no identity spoofing |

The source's historical KeepForever retention, direct worker replacement and hardcoded mime
creation are not adopted. Current source HAR flag is unused in the summary call; optional race
information follows ordinary visible native/provider surfaces, without a hard HAR dependency.

## Implementation and coverage

**Implemented: nothing.** All WJP-01–08 await target documents, tests, code, review and native proof.
Intentional corrections: preserve illness/context/targets/provider fields; preserve actual deadline;
safe empty/missing information; semantic insertion and patch guards; no broad worker replacement.
Original co-installation: Ogliss.WanderJoinsPlus owns overlap, owned enhancement inactive with warning.
No broad VEE, Alpha Animals, TI-Joins+, arbitrary alien-pawn or source-save compatibility claim.

Required game1.7 beta; Harmony required when implemented. Royalty/Ideology/Biotech/VEE/HAR optional
according to actual content. VEE retains its own framework requirements. No XML edit is chosen;
first actual XML Extensions use requires its hard dependency and package/Def/node guards. Never
bundle Harmony, upstream content or the Gateway. Source global letterClass replacement is omitted.

## Verification and acceptance

This is a documentation draft. Review/strict-validation status and WJP-Q1–4 are retained in the
[ledger](../refinement/specification-progress.md). Static provenance and semantic comparator review
do not prove native compatibility. Future acceptance must exercise real offer → information →
Accept/Reject/postpone/expiry → observed native outcomes, save/lifecycle cases, Core/Royalty/admitted
VEE and incompatible/original-provider cases, all languages and exact-process causal screenshots.
Retain build identity, normal-config hashes and cleanup. Every implementation task remains unchecked.
