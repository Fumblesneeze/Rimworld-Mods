# Decision reconciliation — 2026-11-07

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), RimWorld **1.7 beta**.

Answers1–19 from [round one](2026-11-07-decisions-round-1.md) and
[round two](2026-11-07-decisions-round-2.md) have been applied to the active OpenSpec drafts,
supporting designs/tasks, replacement coverage and current refinement statuses. This completes
document reconciliation, not implementation or gameplay acceptance. The earlier source reports and
dated checkpoints remain historical evidence; their superseded proposals are not current policy.

## Decision-to-contract coverage

| Answers | Reconciled observable behavior | Owning changes |
| --- | --- | --- |
| 1–2 | New colonies, ordinary own-save reload, single-player without CE; loaded originals suppress only conflicting owned features with startup warnings. No initial cooperative-original exception or original-save import. | Foundation and affected provider contracts throughout the active drafts; CE/MP implementation tasks deferred, source research retained |
| 3 | Paid quality reworking ends at Masterwork; ordinary Legendary production/acquisition unaffected. Reachable settings, outcome tables and planned test coverage agree. | [Easy upgrades](../../../../openspec/changes/specify-megamod-easy-upgrades/) |
| 4–5 | Imminent danger first when choosing a new patient across rescue/tending; preserve current care. Actual surgeon capability replaces the blanket injury/scar veto and setting. | [Work priorities](../../../../openspec/changes/specify-megamod-smarter-work-priorities/), [priority treatment](../../../../openspec/changes/specify-megamod-priority-treatment/) and composed medical contracts |
| 6,15 | Actually available medical help replaces the physical census; more forgiving self-amputation with visible chance, real cost, commit-time checks and common tuning. | [Emergency self-amputation](../../../../openspec/changes/specify-megamod-emergency-self-amputation/) |
| 7 | Approximately60% mastered-skill slaughter bonus accepted, neutral level10 retained, animal base yields unchanged. | [Slaughter yield](../../../../openspec/changes/specify-megamod-slaughter-yield/) |
| 8 | Bulk output equals corresponding piecewise output;45 steel for three baseline chunks,1100 work. No mixed-kind expansion or output bonus. | [Bulk smelting](../../../../openspec/changes/specify-megamod-bulk-smelting/) |
| 9,14 |25% basic construction recovery, complex ingredients excluded. Construction plus fixed salvage scales by current HP fraction before one probabilistic rounding. | [Equipment recycling](../../../../openspec/changes/specify-megamod-equipment-recycling/) |
| 10–11 | Survival gear may displace armor; all humanlike vacuum arrivals included, including reused NPCs. Existing player-owned pawns use real gear. Shared policy facts do not imply full protection or authorize policy rewrites. | [Weather-ready arrivals](../../../../openspec/changes/specify-megamod-weather-ready-arrivals/), [Spacefarer policy](../../../../openspec/changes/specify-megamod-spacefarer-policy/) |
| 12,19 | Active-game generation runs in the background while simulation continues. Raid/ambush/hostile-entry notices wait for complete valid generation. Lost sources/entry intent cancel temporary work without destroying borrowed pawns/property. The rough1–2 real seconds is never a deadline, timeout, assertion or acceptance threshold. | [Generation smoothing](../../../../openspec/changes/specify-megamod-generation-smoothing/) |
| 13 | Use global per condition's complete two-slot bundle; explicit empty override, global additions/deletions, dormant data and scoped clear/reset behavior. | [Drug Response](../../../../openspec/changes/specify-megamod-drug-response/) |
| 16 | Meal packing continues during shortages. Remove the existing communal nutrition guard as well as rejecting a new days-of-food cutoff; actual supplies/claims/capacity/care still apply. | [Routine meals](../../../../openspec/changes/specify-megamod-routine-meal-carrying/) and [Common Sense practical supplies](../../../../openspec/changes/specify-megamod-common-sense/specs/megamod-practical-supplies/spec.md) |
| 17 | New stations select currently map-possible species with exactly0% retaliation chance; unknown or rounded-down positive values excluded. Later player choices persist. | [Hunting stations](../../../../openspec/changes/specify-megamod-hunting-stations/) |
| 18 | One list/style/order for general, heuristic and researched medical-operation matches; no confidence UI. Provenance stays internal; native permission and ordinary operation descriptions remain. | [Condition medical bills](../../../../openspec/changes/specify-megamod-condition-medical-bills/) |

## Explicit calibration and remaining implementation research

The emergency unmedicated curve is an **agent-selected draft**,50/70/90% at Medical0/10/20,
not percentages supplied by the user or accepted in play. Medicine takes the greater of the
source medicine chance and the unmedicated chance plus a positive potency benefit, capped100%.
The two skill endpoints inherit as one curve so a global edit cannot invert a saved curve.
The separate benefit defaults10 percentage points per potency, with a positive valid range.

Urgent medical classification and tie rules are explicit in S058/S134; current native eligibility,
death prediction and job wiring still require implementation research. Hunting retaliation data,
complete Odyssey/SOS2 protection, provider availability and safe generation continuations likewise
remain unverified. A source/API question is not another unanswered product choice from these batches.

The semantic balance pass checked the specified arithmetic and consequences: bulk45/1100 versus
single-operation45/1200; normalized slaughter100%/160.54%; damaged recycling including fixed salvage;
medicine never worse than no medicine; no shortage-created food; no free gear for existing player
pawns; no event weakening, early notice or phantom successful cancellation. This is document-level
review. Finalized Def measurements and native player workflows are still required before acceptance.

## Verification and independent review

Three independent read-only reviewers covered medical/settings behavior, gameplay/balance/generation,
and initial support/provider/vacuum scope. Findings were corrected: atomic curve inheritance and
positive medicine benefit, stale tuning/confidence/medical-ranking text, hunting default wording,
remaining original-provider cooperation exceptions, deferred CE tasks and selected survival precedence.
All three reviewers rechecked their corrections and found no remaining substantive issues. Minor
audit/grammar wording from the recheck was corrected. Strict typed OpenSpec validation passed
**158 items, zero failures**. All **128 changed documents** passed local-link and trailing-whitespace
checks. No gameplay tasks were newly checked; no unit, integration or in-game tests were run for
this prose-only reconciliation.

Before/after snapshots and the scoped changed-file manifest are retained locally under ignored
`artifacts/Refinement/megamod-spec-20261107/decision-reconciliation/`. They distinguish this prose-only
reconciliation from unrelated concurrent work in the repository. No runtime task was checked, no
game was launched, and no product code, package, installed source mod or normal save was changed.
