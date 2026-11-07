# Auto-Cast Specialist Commands

## Identity and provenance

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, target1.7 beta.
Origin replacement **S051/P1-45**. Source Linnun.AutoCastSpecialistCommands, authorLinnun,
[Workshop3459647882](https://steamcommunity.com/sharedfiles/filedetails/?id=3459647882).
Selected1.7 net10.0 Release assembly1.0.0.0, SHA256
`519ABC6D74438EE1DBACF038632F97AD4CF0E59BE1762BA9B4EF2C560B84ACA2`.
All twelve included source files and selected compiled behavior were inspected2026-11-03.
No source license/repository revision established; independent implementation only.
[Research](../refinement/2026-11-03-specialist-autocast-research.md) separates actual source facts,
provider gaps and proposed changes. [Owning change](../../../../openspec/changes/specify-megamod-specialist-autocast/proposal.md).

## Behavior inventory

Every row is **drafted, unimplemented, unverified**. [Observable contract](../../../../openspec/changes/specify-megamod-specialist-autocast/specs/megamod-specialist-autocast/spec.md).

| Contract | Input → result | Scope/default/edge cases |
| --- | --- | --- |
| AC01 | Begin matching work → consider an already possessed command | Four native, three optional; six defaultsOn, MajordomoOff; no new roles or construction/idle automation |
| AC02 | Reach first actual work → one current eligibility check per command/job | After travel/preparation; no cooldown polling; loops/reload do not add opportunities |
| AC03 | Allowed attempt → ordinary cast/effect/cooldown and continued work | Preserve costs/recipients/durations/manual use; interruptions win; sequential commands recheck |
| AC04 | Bottom-right live choice → shared current-game behavior | On/Off/Use configured value; explicit scope, no ability right-click takeover or bottom tab |
| AC05 | Edit configured defaults → common inherited settings | Global/save distinction, dormant overrides, main-menu protection; live choices remain explicit |
| AC06 | Save/load/switch → preserved choices and no duplicate casts | Native cooldown/effect ownership; unavailable providers dormant; no upstream import |
| AC07–08 | Content/semantic admission → coherent automation or safe fallback | Optional all-six RR group; source co-install suppression; bounded startup warnings, no foreign unpatch |

## Interactions and composed result

Native work and ability behavior remain authoritative. Owned recipe/bill/worker features
S010/S014/S018/S019 cooperate directly without changing chosen recipes/ingredients or duplicating
attempts. Later S092 Keep Converting must share input ownership without an assumed conflict.
No Thing scan/cache is required; do not add an isolated map index for this port.

Ideology is required for the feature, optional for the whole product. Cooking/Void, Anomaly,
VME/Royalty and RR are optional. Source lookup hints for Cooking/Void are not verified canonical
provider IDs yet; installed RR advertises through1.6. Their actual content/beta admission remains
open, not covered by a blanket compatibility claim. With original Linnun.AutoCastSpecialistCommands
active, this entire owned feature is suppressed and its choices stay dormant.

## UI audit and relocation

| Source surface | Purpose | Owned destination / deviation |
| --- | --- | --- |
| Mod settings with seven global booleans | Configuration | Work → Specialist commands; common Global default/Use global/Current save rows; all save writes disabled at main menu |
| Ability right-click changing the same global bool | Shared operational switch, despite pawn-local appearance | One bottom-right live selector; current-game choices do not silently change global configuration |
| Auto-cast overlay on Command_Ability | Current state display | Optional noninteractive native indicator with scope explanation; no copied texture/VEF dependency |
| No source bottom tab | None | None introduced |

All seven languages, compact state display, native right/left-click coexistence, world view and
save switching need actual UI proof. There is no pawn-specific configuration exception: source
switches were never pawn-local. Live operational values are saved gameplay state separate from
the common configured defaults, with an explicit return-to-configured action.

## Contracts consumed by other mods or saves

No external consumer was found in a bounded namespace/owner DLL scan; only three source versions
matched. Public helper/settings/type identities do not alone justify a facade. No package spoof,
assembly copy or automatic upstream settings import is promised. Existing native abilities,
effects and cooldowns retain their native persistence; owned live choices and handled-job markers
need new save lifecycle tests. Adding/removing the feature is not accepted save compatibility yet.

## Implementation and coverage

Nothing implemented. Deliberate departures include safe actual-work timing, current readiness,
player-controlled scope, no repeated attempt in a job, waited native casting, uniform specific-command-first
order (upstream fallback insertion reverses this on research/study), independent
Majordomo study, coherent provider admission and UI/default scope separation. The four native and
three optional commands are specified; this does not replace optional provider roles/content.
Construction, deep drilling, unrelated commands, periodic/idle recasts and source migration are
outside this draft. Provider costs and requirements are preserved, not rebalanced.

## Verification and remaining work

AC-Q1–4 retain effective hooks, missing provider evidence, UI/lifecycle and honest-tier/native
acceptance. Independent review corrected source-order attribution and spec/design separation;
focused re-review found no issues. Strict validation passed73 items, zero failed. All implementation tasks
remain unchecked. Static source analysis is not proof of cast completion, in-game work continuity,
runtime compatibility or successful UI integration.
