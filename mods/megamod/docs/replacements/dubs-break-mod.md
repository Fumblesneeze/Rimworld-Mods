# Dubs Break Mod — replacement coverage

## Identity and status

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin replacement **S119/P2-48**, Dubs Break Mod by Dubwise, `Dubwise.DubsBreakMod`,
[Workshop1722398508](https://steamcommunity.com/sharedfiles/filedetails/?id=1722398508).
Selected1.7 DLL1.0.9781.38431 matches pinned public repository binary; metadata version1.0.2152.
[Research](../refinement/2026-11-06-dubs-break-mod-research.md) records exact identities, complete
binary inventory, public/native evidence, source discrepancies and consumer searches.

**Specified only; nothing implemented or runtime-verified.**
[Owned change](../../../../openspec/changes/specify-megamod-thought-gated-breaks/).

## Behavior inventory

| ID | Observable contract | Retained behavior / intentional correction |
| --- | --- | --- |
| DB01 | Current thought-group severity ceiling |−10/−15/−20 inclusive defaults; native stacking/modifiers, not unrelated sums or base values |
| DB02 | Lower of mood demand and thought ceiling | No forced opportunity; native eligibility/timing/cooldown preserved even when severity is reduced |
| DB03 | Native candidates/fallback within ceiling | Weights/anomaly/lower tiers retained; ideological shortcut excludes higher/unset/unknown severity and cannot hide ordinary fallback after exclusion |
| DB04 | All eligible pawns by default; optional colonists only | Native current classification, safe missing mood; direct/non-mood/active episodes remain unchanged |
| DB05 | Four common inheritable settings | Independent−100..0 integer limits and off-default scope; consistent defaults instead of missing-key0; next-evaluation timing |
| DB06 | Current shared thought/lifecycle facts | S068 fading once, no history/cache/catch-up; native save/timers and S100 recovery |
| DB07 | Settings explanations and unchanged mood UI | Seven languages, grouped/hierarchy/scope/timing explanation; native warnings/final straw, no new tab |
| DB08 | One semantically admitted provider | Compatible modifications allowed; source-selected overlap off; bounded warnings, owned rollback/revalidation and no partial-evaluation replay |

The [observable contract](../../../../openspec/changes/specify-megamod-thought-gated-breaks/specs/megamod-thought-gated-breaks/spec.md)
is separate from implementation choices in design. Full runtime target documents/tests remain open.

## Interfaces and co-installation

| Surface | Treatment |
| --- | --- |
| Source package/Harmony owner/assembly | Do not impersonate them; select source when active and disable only owned overlap |
| Public Settings/Prefix types and global keys | Inventoried, no actual consumer found in bounded searches; no facade or automatic import promised |
| Thought grouping/modified effects | Use current native/provider results, including memory fading, without copying a pawn cache |
| Candidate workers/weights/ideological and anomalous paths | Preserve admitted native/optional contracts within scope and ceiling; unproved replacements need guarded adapters |
| Direct mental states and existing episodes | Remain with their actual initiator/recovery system, not globally intercepted |
| Saved pawns/timers | Native state retained; only common settings added; no claimed source-save/removal migration |

Harmony is required with actual C# patch use; no bundled assembly. No XML, hard source/DLC, other
repository product or Gateway dependency. No source code/license/art copied. Public distribution
contains no license grant; independent behavioral implementation is intended.

## Composition, UI and omissions

S024 affects actual mood/needs upstream; S068 supplies current faded thought effects; S100 counsels
existing episodes. S117/S118 affect a slaughterer's later jobs, not the break trigger. These compose
directly without patches against owned features. Native thought data supplies current facts; no
new shared Thing-index consumer or per-port pawn cache is necessary.

Source has only a global settings page; owned controls use common Global default/Current save/Use
global rows and all seven languages. Pawn thoughts are not settings. Native warning colors and
final-straw messages stay native, and are not promises that their named thought alone permits the
same severity. No bottom tab, small toggle or cheat action is added.

Intentional differences: consistent missing defaults, inherited save controls, localization,
safe no-mood/admission handling and ideological severity enforcement. Source ResetSettings is empty
and unused optionsWindow/LastVersion are not adopted as gameplay. Original selected as provider does
not receive or claim these owned corrections. No existing episode is cured when limits change.

| Scope | Implemented | Remaining |
| --- | --- | --- |
| DB01–08 behavior and controls | Nothing | All code, common settings, target docs, localization, TDD and native proof |
| Candidate/provider compatibility | Nothing | Effective branch checks, real optional fixtures, source-present and later revalidation |
| Balance and performance | Nothing | Native thought anchors inspected; actual combined mood/fading/recreation timing and cost unverified |
| Migration | Nothing | No source key conversion or blanket safe-removal claim |

## Verification

[Ledger](../refinement/specification-progress.md) tracks DB-Q1–4 and document review/validation.
Final proof needs native gameplay that produces actual thoughts and mood opportunities, followed by
observed allowed/downgraded/absent ordinary episodes. Include native restrictions/timing, grouped
effects/fading, populations, save/transfer, direct non-mood starts, ideological fallback and anomaly
choices in exact provider-present runs. Review code first, then personally inspect causal views from
the reviewed minimized isolated build and retain actions/screenshots/build/mod order/config hashes/
cleanup. Startup logs, raw state assignment and host calculations cannot accept the gameplay.
