# Just Put It Over There

## Identity and provenance

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: replacement, **S027 / P1-21**. Source Mlie, idea credit ʟᴇɢɪᴏɴ,
`Mlie.JustPutItOverThere`, Workshop **2856471776**. Inspected **2026-11-02**.
[Research](../refinement/2026-11-02-visitor-deliveries-research.md) pins documentation-first evidence,
all three source/installed classes, native boundaries, actual provider calls and search limits.

Installed 1.7.0 DLL SHA-256 `F0526527A6EC6F4A7F8B287C84397936C22C5CC9EA45A0308BEDC8F6A7B27032`
matches the committed DLL at `c38c20aedf8c8b87b8996fbf21226088a6a954aa`. About differs only by
newline representation; installed/public MIT license matches (copyright 2020 Mlie). Retain notices
for future substantial licensed reuse. No source assembly, code or artwork is copied into a package,
and a matching binary is not a reproducible build or gameplay acceptance claim.

[Steam description](https://steamcommunity.com/sharedfiles/filedetails/?id=2856471776) and
[source](https://github.com/emipa606/JustPutItOverThere) establish purchase/gift storage courtesy,
ground fallback and Hospitality scope. Changelog/documented exclusions are distinguished from
incomplete source-path checks and proposed corrections in research.

## Behavior inventory

All rows: **analyzed; specified with retained gaps; not implemented**. Owning
[capability](../../../../openspec/changes/specify-megamod-visitor-deliveries/specs/megamod-visitor-deliveries/spec.md).

| ID | Player input/event → observable result | Boundaries/defaults |
| --- | --- | --- |
| VD-B1 | Complete on-map trade → eligible visitor can carry received goods, including sale-proceeds silver, to storage | Native payment/ownership and actual handover; outgoing sold goods, pawn/orbital/world trade untouched |
| VD-B2 | Receive ordinary visitor or Hospitality gift → bounded courtesy delivery | Gift chance/value/messages stay native/provider-owned; routine departure differs from emergency exit |
| VD-B3 | Goods originate on pack animal or trader → choose eligible other courier | Non-animal/free/manipulating same-party giver; main trader excluded on every path; try next reachable candidate |
| VD-B4 | More goods or insufficient capacity → partial load and ground remainder | One native carrying load per available courier/event; no general visitor inventory batching, old cargo or indefinite queue |
| VD-B5 | Choose storage → actual better-priority eligible destination | Stockpile/shelf/supported container, member filters and shared real-space commitments; X/Y refill target exempt, per-stack hauling ceiling retained |
| VD-B6 | Receipt merges/splits or another actor moves it → exact remaining received units | Anonymous removal retires receipts first, oldest event first; no old-stock entitlement, repeated trade effect or synthetic replacement |
| VD-B7 | Routine farewell or urgent departure → finite courtesy then exit, or immediate fallback | No visit restart, dismissal override, animal escort into storage or indefinite departure hold |
| VD-B8 | Interrupt/save/load during delivery → actual cargo recoverable | Local remainder drop, personal inventory preserved, native death/drop, unavailable placement retained without retry loop |
| VD-B9 | Original/changed provider active → one admitted owner and native fallback | New owned overlap withheld; old owned obligations safely retired; per-path startup warnings |

## Interactions and UI placement

| Source surface | Purpose | Owned destination / difference |
| --- | --- | --- |
| Native trade confirmation | Exchange goods, then courier delivery | Same native trade UI and visible pawn workflow, including sell-only silver receipts; payment and pawn transactions unchanged |
| Native/provider gift event and letter | Give goods, then courier delivery | Same gift decision/letter; exact receipt and routine farewell lifecycle are explicit |
| Native hauling job/selection view | Show courier carrying actual load | Ordinary contextual job observation and any needed recoverable-failure explanation |
| Dev-mode log messages | Source diagnostics | Bounded owned startup/recovery diagnostics; no new debug command or bottom tab |

There is no source mod-settings window, user toggle, custom tab or gizmo. No new one is proposed.
Receipt/courier obligations are gameplay state, not global/save settings. Any later genuine
configuration must follow common inheritance; no exception is invented now. Seven-language text
and native workflow rendering remain implementation tasks.

S005 linked storage retains per-member physical rules. S021 shares current destination/capacity
facts and reservations but does not gain nonplayer batch workers. S024 cannot redirect the courier
into personal inventory, housekeeping or unrelated detours. S049's confirmed X/Y-versus-stack-ceiling
distinction is recorded without preempting that queued source analysis. S039's future trading area
controls gathering, not hidden access permission. All selected behavior composes directly in owned
code rather than keeping source-on-source patches.

## Consumed contracts and limits

| Evidence / binding | Preserve / adapt / unsupported |
| --- | --- |
| Source-name scan: source files plus four YART LayoutHarness mod-list entries | No actual external source API consumer established; package-list references do not justify a facade |
| Public GetHaulJob/LogMessage | No demonstrated consumer; no speculative source assembly/type identity emulation |
| Hospitality Continued `Orion.Hospitality`, installed 1.7 Leave/Cleanup → native GiveRandomGift/GiveGift | Shared native call is real; source has no explicit adapter. Guard any needed guest lifecycle/permission integration and preserve provider gift rules |
| Native storage/HaulToCell/HaulToContainer and gift/trade calls | Preserve actual physical semantics, add bounded receipt ownership and safe lifecycle; signatures alone cannot prove successful delivery |
| Source native jobs/lord extraForbiddenThings | Exact-receipt courtesy permission preserves general anti-reacquisition and player/provider restrictions. No own source save schema; do not infer legacy ownership from arbitrary haul jobs |

Optional native-boundary caller search includes unrelated providers and copied/reference game DLLs;
it is not a compatibility claim or selected-mod list. No automatic original-save import or safe-removal
guarantee. A future importer needs backup, atomicity, restoration and verified postconditions.

## Implementation, deviations and remaining work

**Implemented: nothing.** [Proposal](../../../../openspec/changes/specify-megamod-visitor-deliveries/proposal.md),
[design](../../../../openspec/changes/specify-megamod-visitor-deliveries/design.md),
[tasks](../../../../openspec/changes/specify-megamod-visitor-deliveries/tasks.md) and VD-Q1–4 record the work.

Corrections: consistent main-trader/departure/availability checks, fallback to another reachable
courier, bounded assignments without queue destruction, actual placed receipt quantities, no
unrelated duplicate pickup, receipt-first mixed-stack removal, narrow receipt handling permission,
exact interrupted cargo and save recovery. Incoming sale-proceeds silver retains source delivery
eligibility. Ordinary farewell gifts
remain possible because both native visitors and Hospitality give gifts during departure. New
trade deliveries while already leaving and urgent/dismissal cases remain excluded. A finite
single-load service can leave excess on the ground; automatic multi-trip service is not added.

Original active: suppress owned overlap, preserve/recover old obligations, do not change original
settings or unpatch it. Other providers are judged by required semantics, not merely presence.
Harmony will be required when referenced; Hospitality is optional. No XML/Def mutation or XML
Extensions dependency in this proposal. Candidate VD-T1–6 families are design only; individual
executable target checks, tests and live evidence do not exist yet. Native unsuccessful placement
before a delivery receipt remains outside the feature's guarantee; no claim to fix all trade loss.

## Verification and acceptance

Document validation/review status is recorded in [progress](../refinement/specification-progress.md).
Every implementation task remains unchecked. Final native trade/gift receipt → visitor pickup →
storage/drop observations must use the reviewed build and exact source/provider combinations,
including interrupted/merged/partial/save cases. Static semantic review preserves payment, gifts,
research and actual space/carrying limits; it adds finite visitor labor, not free items or universal
hauling. No runtime, persistence, provider compatibility or optimization is accepted by this draft.
