# [PL] Trading Spot

## Identity and provenance

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
Origin: **replacement**, S039/P1-31. Source: [PL] Trading Spot, Programmer-Lily,
`tradingpost.programmerlily.com`, Workshop **3507108484**, installed About version **1.2.0**,
assembly **0.0.0.0**, supports through 1.6. DLL SHA-256
`122599599E606E2D0CB9B11F040A53B2BC0839334DF545081C2A6A2EAA761778`.

[Research](../refinement/2026-11-03-trading-spot-research.md) records documentation first, both
supplied source files, all six compiled runtime types, one Def, settings/localization, nine native
types and bounded consumer checks. Installed source and compiled behavior agree. No specific reuse
license was established; no source code, DLL or artwork is copied. This is static research, not a
verified 1.7 port or a reproduction of Workshop comments.

Owning [proposal](../../../../openspec/changes/specify-megamod-trader-gathering/proposal.md),
[design](../../../../openspec/changes/specify-megamod-trader-gathering/design.md),
[observable spec](../../../../openspec/changes/specify-megamod-trader-gathering/specs/megamod-trader-gathering/spec.md)
and [tasks](../../../../openspec/changes/specify-megamod-trader-gathering/tasks.md).
**Drafted only; nothing implemented or accepted.** The user's original **S040 dedicated trading
area** remains separately tracked and is required to complete the composed feature.

## Behavior inventory

| ID | Player action/input → result | Scope and deviations | Contract |
| --- | --- | --- | --- |
| TS-01 | Trade party arrives → physically gathers at usable chosen destination | Entry, stock, trader and roles preserved; area replaces marker | Eligible caravan gathering |
| TS-02 | Ordinary visitors arrive → optionally gather there too | Default on; small trader follows visitor classification; provider guests distinct | Eligibility and common setting |
| TS-03 | Area changes during visit → existing party updates ordinary gathering | Same identity/timers/cargo; no repeated job churn | Changed destinations |
| TS-04 | Area blocked/removed → another usable area destination or native fallback | No false permissions, wall loops, teleporting or retained deleted point | Fallback |
| TS-05 | Danger/rescue/dismissal/exit → ordinary response takes priority | Do not capture departing visitor Travel or defensive DefendPoint phases | Lifecycle precedence |
| TS-06 | Trade creates S027 courier → delivery remains finite and returns/departs normally | No animal following to storage, extra trips, access or capacity | Delivery composition |
| TS-07 | Save/switch maps → party/map state remains independent | Native progress preserved; no CurrentMap constructor side effects | Maps and saves |
| TS-08 | Original or unsupported provider controls party → owned route visibly inactive | Original untouched; native/foreign ownership retained | Compatibility |
| TS-09 | Source marker placement/work preference | **Not adopted:** user explicitly replaced one-cell marker with area | S040 original feature owns new controls |

All adopted rows are specification drafts. Complete source internals, defaults and risks remain in
research; they are not silently described as fixed gameplay.

## Interactions and composed result

S039 supplies source-derived gathering behavior; S040 supplies the user's map-local footprint and
gizmos. Compose both directly, without patching an owned marker. The area chooses ordinary idle
destinations, not every step/follower position; it is not a pawn allowed area or permission override.
S027 courtesy delivery remains temporary work with its own cancellation/departure rules. Native
trade identity, inventory, duration, emergencies, carrier following and gifts stay authoritative.

Original PL active: withhold overlapping owned redirection, preserve both configurations and the
owned area for later use, show the inactive reason and issue bounded startup warnings. Hospitality's
own guests retain provider control; native ordinary parties are distinguished by actual ownership,
not display-name matching or whether one member can trade. No universal controller compatibility.

## UI audit and relocation

| Source surface | Purpose | Owned destination / change |
| --- | --- | --- |
| Architect → Misc → TradingSpot building, native building controls | Designate gathering point | Replaced by S040 dedicated area designation/contextual gizmos; no marker construction or old-marker deletion |
| Visitors go to Trading Spot checkbox | Actual mod preference | Mod settings → Trading → Include ordinary visitors, default on, shared global/save/Use global controls |
| Requires Work To Place checkbox | Marker construction preference, 10 or 0 work | Omitted with marker; not a global setting for area footprint |
| No source bottom tab | None | None added; area entity actions remain contextual |

Map cells are gameplay state, not settings. The one adopted preference uses global/save inheritance
and main-menu callback disabling; no exception. Its next ordinary gathering-decision timing is
explicit. Required seven-language UI, disabled controls and save-switch behavior remain acceptance work.

## Consumed contracts and persistence

| Consumer/evidence | Binding | Preserve / unsupported |
| --- | --- | --- |
| Native source saves | TradingSpot building class/Def and global source keys | No automatic import, source-object deletion or binary/Def identity facade; old-source saved-object conversion is outside this change |
| Better Architect Menu XML | Legacy `grizzlemethis.tradingspot.rw` folder; TradingSpot category/work fields | Real legacy marker-only consumer; not applicable to an area and not proven active for PL |
| Drawn Spots XML | Legacy package-gated TradingSpot graphics, separate Trading Control branches | No marker texture contract to preserve in area representation |
| Source public types/methods | No external callable binding found by bounded text and ignore-disabled DLL scans | Do not invent an API facade; dynamic/uninstalled consumers remain unproven |
| Hospitality (Continued) | `Orion.Hospitality`, distinct guest LordJob | Respect guest ownership; no claim to redirect its room/guest-area system |

No source config or normal save is written. A later opt-in converter needs its own backup, atomicity,
failure restoration and verified postconditions. Source hashes identify inspection inputs, not runtime
compatibility. Per-patch minimal semantic guards, effective-code handling and bounded warnings remain
mandatory if later implementation needs Harmony; guarded reflective adapters also fail safely.

## Implementation and coverage

| Scope | Implemented | Deliberate changes / conflict boundaries | Missing |
| --- | --- | --- | --- |
| TS-01–08 | Nothing | Phase-aware gathering, safe release/fallback, common per-save preference, real provider identity | All runtime code/tests/admission/native evidence |
| Update strategy | Nothing | Source resets its counter and runs each tick; repeated reflection is not adopted | Concrete measured invalidation/decision integration |
| Marker and work | Nothing | Omitted under explicit area requirement; legacy marker consumers do not bind | S040 full area contract and implementation |
| Dependencies | No metadata changed | Original/DLC/Hospitality optional; Harmony required if eagerly used; no current XML patch | Exact package and provider acceptance |

No shared Thing cache is introduced for a native Lord list. Area/party routing state has map/game
lifetime and is composed directly with existing features. Any later XML integration requires XML
Extensions guards and merge preservation rather than destructive unrelated Def edits.

## Verification and acceptance

TS-Q1–4 retain exact native phase/field/selection proof, S040 geometry and idle movement, provider
ownership, possible reuse licensing, measured update cost and final gameplay evidence. Nothing is
accepted by startup logs or destination field assertions. Later verification must personally observe
native area editing, actual incident-created parties walking/gathering/trading, changed/blocked
areas, deliveries and exits, save/load, map isolation and exact provider cases on the reviewed build.

Semantic review preserves stock, prices, goods, visit timing and emergency exit while acknowledging
that chosen routes change travel distance and exposure. No resource bonus or artificial visitor
imprisonment is intended. Pure documentation checks do not require a game launch.
