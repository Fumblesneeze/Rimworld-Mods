# I Cant Fix That

## Identity and provenance

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target1.7 beta.
Origin **replacement**, S054/P1-47. Source Mlie, `Mlie.ICantFixThat`,
[Workshop2780971535](https://steamcommunity.com/sharedfiles/filedetails/?id=2780971535), inspected2026-11-03.
Version1.7.0; selected1.7 DLL SHA256
`79A30002226DF721B48D5D874BDD416609CB7A27DB0AE861E0E1F818278C32EB`, byte-identical to pinned checkout
[7761227b5ab66950a724307daad0a2219b00e2ff](https://github.com/emipa606/ICantFixThat/tree/7761227b5ab66950a724307daad0a2219b00e2ff).
MIT copyright2020 Mlie; retain notices for any future reuse. No upstream assembly, code or art shipped.
[Research](../refinement/2026-11-03-i-cant-fix-that-research.md) records all29 current source files,
selected compiled comparisons, all11 adapter identities/hashes, native traces and bounded consumers.

Status: analyzed and **observable draft independently reviewed and strictly validated**. Nothing implemented or accepted.
[Proposal](../../../../openspec/changes/specify-megamod-repair-knowledge/proposal.md),
[design](../../../../openspec/changes/specify-megamod-repair-knowledge/design.md),
[spec](../../../../openspec/changes/specify-megamod-repair-knowledge/specs/megamod-repair-knowledge/spec.md),
[unchecked tasks](../../../../openspec/changes/specify-megamod-repair-knowledge/tasks.md).

## Behavior inventory

Every row is researched/specified, not runtime verified.

| ID | Action → intended observable result | Defaults / edges | Owning requirement |
| --- | --- | --- | --- |
| RK01 | Seek repair → colony knowledge limits otherwise valid work | Existing worker/repair rules preserved; colony actors and admitted player automation | RK01 in linked spec |
| RK02 | Learn research → any genuine manufacturing route unlocks repair knowledge | AND within route, OR between recipes/benches, multi-output; no fabricated no-recipe gate | RK02 |
| RK03 | Repair building → direct research/minimum technology/difficulty checked | Advanced colony retains primitive furniture repair | RK03 |
| RK04 | Configure ceiling → predictable current policy | Off, allowance1, integer0..7, common global/save model | RK04 |
| RK05 | Inspect unavailable action → actual refusal explained | Preserve earlier native reason; continue other candidates; no bottom tab | RK05 |
| RK06 | Start/continue/complete repair → current eligibility without losses | No extra costs/results on refusal, prior legitimate progress remains, safe recovery | RK06 |
| RK07 | Use admitted maintenance/mending/donor/automation → same target rule | Materials remain ordinary ingredients; costs/range/power/rates retained | RK07 |
| RK08 | Enable native God mode → bypass added policy only | Existing top debug control, native/provider refusal retained | RK08 |
| RK09 | Missing/changed/source-coinstalled provider → coherent truthful fallback | Missing optional quiet, incompatible path native, source controls overlap | RK09 |

## Interactions and composed result

Owned repairs use one direct policy; no patches against our own implementation. Recycling,
destruction, production and medical work retain separate contracts. Manufacturing research uses
finalized recipe metadata shared with relevant consumers; it does not create a separate hauling/
search Thing cache or confuse ingredients/workbench presence with learned knowledge.

Intentional corrections: alternative rather than last recipe, valid bench fallback, multi-output
manufacture, consistent defaults, primitive furniture repair after tech advancement, target-only
ingredient gating, actor scope and complete work lifecycle. Source-advertised compatibility is not
automatically adopted. Exact VFEAncients1.5 and Glittertech1.5–1.6 references were inspected; beta
admission remains unverified. Other nine adapter families need actual current-provider research.

## UI audit and relocation

| Source entry point | Purpose | Megamod destination / deviation |
| --- | --- | --- |
| Mod settings checkbox, allowance slider and current/max tech | Repair policy configuration | Work → Repair knowledge common global/save rows; consistent Off/1 and protected main menu |
| WorkGiver JobFailReason and drafted repair option | Explain refusal | Native contextual work/command surfaces, actual missing condition and preserved earlier refusal |
| Native God mode bypass | Unrestricted development override | Existing top developer control; no ordinary cheat button |
| Bottom bar / per-entity controls | None found in all29 files | None added; no inherited pawn/building settings |

Use global retains dormant overrides, missing save entries inherit current globals, overrides do not
edit other saves, and both save editor and Use global callbacks are inactive at the main menu. Change
timing is next relevant work decision, including ongoing checks. No scope exception is needed.

## Contracts consumed by other mods or saves

| Consumer / evidence | Binding | Decision / required proof |
| --- | --- | --- |
| Source's eleven own adapters | Public Main.CanRepair overloads, actor predicate and own assembly; MendAndRecycle recipe-name list | Direct owned policy and narrow provider adapters; do not recreate an internal patch stack |
| Bounded5452-DLL/text search | All51 DLL matches are own source versions; RimPy catalog is metadata only | No demonstrated external executable consumer; no speculative API facade or whole-source drop-in claim |
| Source global settings | AlsoBlockTechlevel and AlsoBlockTechlevelLevel; fresh false/1 versus missing-key true/5 | Own common configuration, no automatic migration or foreign-file mutation |
| Provider repair jobs / ingredient result handling | Real costs, donors, target identity, work and persistence | Admit full safe lifecycle; unknown/changed branch stays native |

No source-owned item/save type was found. Absence in the bounded installed scan is not a universal
absence claim. Future actual consumers need explicit preserved/adapted contracts; a later migration
requires backup, atomicity, failure restoration and verified postconditions. No package spoofing.

## Implementation and coverage

**Nothing implemented.** All RK01–09 behavior, semantic target documents, adapters, translations,
honest-tier RED/GREEN and native acceptance remain pending. Source's original package suppresses the
overlapping owned restriction with a warning. Each changed repair path keeps its complete native/
provider behavior; no half-installed ingredient restriction. Other owners are never unpatched.

Core and Harmony are required when runtime patches are implemented. All repair providers and their
content/DLC are optional. No XML change is currently planned; first real use requires XML Extensions,
content/package/node guards and merge-preserving operations. No upstream or Gateway assembly,
repository-mod dependency or foreign-data mutation is introduced.

## Verification and acceptance

Independent review corrected one research-only native warmup description; focused re-review found
no issues. Typed strict validation passed76items, zero failed; eight scoped documents passed local
link/whitespace checks. The draft contains9requirements,46scenarios and26unchecked tasks; see the
[ledger](../refinement/specification-progress.md).
All implementation tasks are unchecked. No game/build/install was run for this documentation slice.
RK-Q1–4 retain effective native/provider admission, finalized progression, UI and implementation.
Final acceptance must personally observe actual repair, missing/completed research, breakdown
component use and cancellation, mending/donor recovery, automation, native refusals, God mode and
settings/save lifecycle on the reviewed build in fresh isolated minimized runs. Source/decompilation,
logs, installed patches and synthetic state checks are supporting evidence only.
