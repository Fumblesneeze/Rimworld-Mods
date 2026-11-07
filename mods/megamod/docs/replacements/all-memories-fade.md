# All Memories Fade — replacement coverage

## Identity and provenance

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
Origin: replacement, S068/P1-62. Source: Mlie's **All Memories Fade**, `Mlie.AllMemoriesFade`,
[Workshop 2800155563](https://steamcommunity.com/sharedfiles/filedetails/?id=2800155563),
[author repository](https://github.com/emipa606/AllMemoriesFade), installed **1.7.0**.
Inspected 2026-11-04: primary documentation, all three source-binary classes and selected native
1.7.4998 thought, social and counselling paths. MIT license; no code/binary/art copied into product.
Exact hashes, source update timing and evidence limitations are in
[research](../refinement/2026-11-04-all-memories-fade-research.md).

Status: **observable draft independently reviewed and validated. Nothing implemented.**
[Proposal](../../../../openspec/changes/specify-megamod-memory-fading/proposal.md),
[design](../../../../openspec/changes/specify-megamod-memory-fading/design.md),
[spec](../../../../openspec/changes/specify-megamod-memory-fading/specs/megamod-memory-fading/spec.md),
[unchecked tasks](../../../../openspec/changes/specify-megamod-memory-fading/tasks.md).

## Behavior inventory and implementation coverage

Every row is specified, not implemented; MF IDs refer to the linked observable requirements.

| ID | Native input → observable result | Default / correction / edge boundary |
| --- | --- | --- |
| MF01 | Select duration range → stable included memories | Inclusive 0 days–Unlimited; underlying length before our multiplier; includes short memories; permanent/situational effects unchanged |
| MF02 | Memory ages/renews → one proportional mood fade | Both signs, nullification, no squared fade/sign reversal/non-finite value; native events/groups remain |
| MF03 | Set base strength → scale base mood only | 100%, range 0–200%; −1 is real; opinion/independent offset/captured award not multiplied |
| MF04 | Set duration → change ordinary life and native expiry | 100%, range 0–200%; preserve age/overrides; social timing changes too; zero expires normally |
| MF05 | Successful counselling → captured positive amount fades | Own included captured lifetime, no second strength/duration multiplier; no later live linkage to original negative |
| MF06 | Edit scoped controls → common global/save policy | Dormant overrides, main-menu callback guards, atomic range, immediate ordinary reevaluation, no revival |
| MF07 | Open settings/Needs/Social or receive a numerical event message → current honest values | Actual examples/type counts and awarded counselling bonus, seven languages; dynamic lifetimes and opinion distinguishable |
| MF08 | Startup/provider/load transitions → one admitted policy | Coherent ordinary group, optional counselling fallback, original sole provider when active |

All implementation, tests and native acceptance remain missing. Source is not yet replaced.

## Selected corrections and composition

Source collects only definitions longer than 0.1 days, selects by already-mutated durations, uses −1
as a null-stage sentinel, and does not update fade flags on normal settings writes. Those behaviors
are deliberately corrected. Duration zero receives explicit finite expiry semantics. Base numerical
examples are replaced with current values; source's −80 example refers to an inspected opinion-only
definition and cannot be promised as a mood effect.

Native counselling captures current magnitude and remaining lifetime but bypasses ordinary fade.
Its selected support is an explicit extension; the public report of endless counselling is not
reproduced or claimed proven. Fading its captured bonus does not grant another multiplier or create
a new target-memory relationship. Different inclusion policies or later renewal/removal need not
remain exactly compensating. Explicit individual lifetimes stay authoritative for other supported
memories too. Native social-opinion strength/curve stays, while ordinary duration affects its clock.
Counselling preserves its current qualifying threshold (inspected default −10); a −9 penalty takes
the fallback boost. That boost's message must agree with its actual scaled award, with an admitted
action-local presentation path or coherent native fallback for the affected content. This closes
the P2 gap found by independent native review of the initial contextual design.

S001/S003 ordinary displays consume current actual mood; S037 and other thought-producing features
retain their event rules. S038 applied exclusions block new acquisition but retain existing memories:
keep those types visible/countable with a separate acquisition status, and keep their current fading.
Pending content choices remain pending until S038 application; missing definitions are a different case.
Refused counselling awards must not be announced as granted. Unsupported reporting refuses the affected
action before cost/cooldown, or leaves the coupled exclusion unavailable if safe refusal cannot be
admitted. Preserve requested choices and explain the actual fallback. S124 Drug Response is queued:
this feature does not independently change withdrawal, drugs or persistent conditions. Direct owned
composition replaces internal patches. Native thought data/common settings suffice; no map index,
private pawn scan, extra tick loop or performance claim.

## UI audit, settings and persistence

| Source surface | Purpose | Megamod destination / change |
| --- | --- | --- |
| Mod settings multipliers/range/reset | Configuration | Mood / Memories common rows: Global default, Current save, Use global |
| Fixed numerical examples/count/version footer | Explanation | Actual current labeled types/values and supported coverage; no separate source-version UI |
| Native Needs and Social displays | Pawn gameplay state | Retain native surfaces and current actual effects; no per-pawn mod-settings rows |

No source/new bottom tab or extra toggle/debug control. Defaults **100% strength, 100% ordinary
duration, inclusive 0 days–Unlimited**, percentages **0–200%**. Bounds are one atomic range value.
All common inheritance/dormant-override/main-menu restrictions apply, including resets/callbacks.
Seven-language text required. Accepted edits affect existing memories without age rewind; native
expiry can remove shortened memories and later settings do not recreate them. Save switching selects
the correct policy without mutating globally shared definitions. No custom memory save schema/import.

## Contracts, providers and omissions

Source exposes public static update/query helpers and MaxDuration, but scoped installed XML/CS/JSON
search found no runtime consumer: only RimPy identity data and YART sample mod lists. This is not an
all-binary consumer audit. No source package/assembly identity or settings keys are impersonated.
Preserve native memory class/identity/serialization and acquisition/expiry ownership.

Original active: it supplies this feature alone; owned overlapping transformations stay inactive,
controls explain that original global settings govern behavior, and owned values remain dormant.
No source-settings mutation or automatic import. Other compatible foreign changes are allowed by
effective semantic admission. Unsupported custom paths are explicitly left to their provider;
arbitrary third-party override support is not claimed. Each patch needs a minimal documented target
spec, owned-only rollback and bounded startup warning. Ordinary calculations are one coherent group;
optional counselling can decline separately without disabling ordinary supported memories.

Harmony becomes required on implementation. No XML patch selected, so no new XML Extensions use;
no source binaries, other repository product or Gateway dependency. Ideology is optional for its
content-specific counselling path. No historical expired-memory recovery, permanent-effect fading,
opinion-strength scaling, new drug behavior or broad promise of greater emotional stability.

## Verification and remaining work

MF-Q1–4 retain proposed corrections, exact effective target/lifecycle proof, custom/provider coverage,
settings/performance and native acceptance. Required player workflows obtain memories through actual
events/ingestion, watch native aging and Needs/Social values, edit real settings, renew/expire effects
and perform the native counselling action. Raw assigned memories/ages are setup or diagnostics only.

Semantic pass: default linear fading reduces the cumulative magnitude of formerly constant positive
and negative effects; duration changes can affect social opinions too. No unqualified easier-game or
performance claim. Zero base strength differs from zero total mood; zero duration has real expiry
consequences. All implementation/native evidence remains open; no runtime code, build/install, tests,
game launch or normal save/configuration changes occurred.

Independent native review's P2 numerical-message gap and composition review's two P2 retained-memory/
refused-award gaps are resolved; focused composition re-review found no remaining P1/P2 issues.
The draft contains **8 requirements, 30 scenarios and 25 unchecked tasks**. Strict typed OpenSpec
validation passed **90 items, zero failures**, with a separate telemetry transport error. All
**12 scoped Markdown documents** passed local-link/whitespace checks. None of these checks accepts
gameplay or closes a runtime task.
