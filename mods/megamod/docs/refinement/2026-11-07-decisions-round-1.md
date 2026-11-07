# Refinement decisions — round one

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), RimWorld **1.7 beta**.
Recorded **2026-11-07** from the user's numbered answers. These are accepted product decisions,
not implementation or gameplay acceptance. Earlier draft defaults remain historical proposals
where they disagree. The affected drafts have now been reconciled; runtime work remains pending.

## Accepted answers

| Question | Decision | Intention and bounds |
| --- | --- | --- |
| 1 | Initially require a new colony. If an original mod remains loaded, log a startup warning and leave our conflicting replacement features inactive. | Preserve the original provider and unrelated megamod features. Installed-but-inactive mods do not count. Normal save/load for colonies begun with megamod remains required. No initial import of original-mod or pre-megamod colony state. |
| 2 | Initial support excludes Multiplayer and Combat Extended. | Existing upstream integration research is reference material, not an initial support commitment. |
| 3 | Initially cap paid quality reworking at Masterwork. | The previous Legendary ceiling is superseded; ordinary creation or acquisition of Legendary items is unaffected. |
| 4 | When choosing a new medical patient, imminent danger to life takes precedence over a blanket rescue-before-tending rule. | Compose one urgency policy across rescue and treatment with S058/S062/S124/S134. Preserve protected care already underway; exact comparable urgency semantics still require specification and native research. |
| 5 | Injured doctors may operate when their actual capabilities and ordinary medical rules permit it. | Remove the blanket injury/scar veto and its associated automatic-operation setting. Keep ordinary medical risks and capability restrictions. |
| 6 | Emergency self-amputation may be offered when nobody else can actually provide the required medical help. | Mere physical existence of another colonist is insufficient. Actual medical eligibility and access matter; source behavior that lets an unqualified, unavailable or unreachable pawn block the emergency option is superseded. |
| 7 | About 60% additional slaughter yield at mastered skills is reasonable. | Retain the proposed normalized curve's approximately160.54% at all relevant skills20 and100% at10. This is deliberate slaughter recovery, not a global increase to animal base yields or a new butchering bonus. The magnitude is accepted; runtime balance verification remains open. |
| 8 | Bulk smelting improves efficiency only; output amounts must equal doing the same inputs piecewise. | Source60 steel per three ordinary slag chunks is superseded by45 for the inspected15-per-chunk baseline. Follow the actual corresponding ordinary outputs if another admitted source changes them. Do not silently introduce mixed-kind batching or an additional output bonus. |
| 9 | Use the proposed conservative recycling default:25% basic construction materials, complex ingredients/components excluded. | Record this as the accepted initial policy. Do not interpret it as approval of more generous recovery, new repair behavior or salvage from invented ingredients. The initial complex-recovery alternative/control is deferred. |
| 10 | Survival equipment may displace otherwise protected armor. Any humanlike pawn spawning on a vacuum map should have suitable vacuum gear, including Odyssey/SOS2 environments. | Compose arrival protection with S130's vacuum-policy work. Vacuum protection is not restricted to Spacer factions, adult pawns or a raider toggle. Preserve already adequate protection. Actual provider support and complete environmental protection must be researched; no SOS2 compatibility is accepted by this decision. |

Question10 settles who needs protection. [Round two, answer11](2026-11-07-decisions-round-2.md)
settles equipment supply: **no free equipment for the player**; existing player-owned pawns must bring real gear.
Incompatible anatomy or absent suitable gear still needs an explicit failure policy; no outfit may
be labelled safe without actually providing protection.

## Completed draft reconciliation

The foundation, affected feature proposals/specs/designs/tasks, coverage and current refinement
statuses now incorporate decisions1–19. [Reconciliation audit](2026-11-07-decision-reconciliation-audit.md)
records the affected contracts and verification. Historical source-inspection reports retain what
upstream actually does; their older proposed policies are superseded by these decisions.

| Affected change | Required amendment | Status |
| --- | --- | --- |
| `add-megamod-foundation` | New-colony initial scope, Multiplayer/CE deferral, original-provider suppression and warning. | Specified; runtime mechanism and verification pending. |
| All feature drafts | Reconcile co-installation alternatives and any active Multiplayer/CE requirements with the foundation. Preserve supported own-save reload and unrelated optional integrations. | Reconciled in the active drafts; implementation and live verification pending. |
| `specify-megamod-easy-upgrades` | Masterwork ceiling in the quality-reworking capability, associated controls, reachable outcome tables, proposal/design/tasks and coverage. | Reconciled in the active drafts; implementation and live verification pending. |
| `specify-megamod-priority-treatment`, `specify-megamod-smarter-work-priorities` | Shared urgency across rescue/tending; actual doctor capability; remove the blanket injury setting and reconcile dependent medical order rules. | Reconciled in the active drafts; implementation and live verification pending. |
| `specify-megamod-emergency-self-amputation` | Available medical help replaces a purely physical census; update examples and commit-time rechecks. | Reconciled in the active drafts; implementation and live verification pending. |
| `specify-megamod-slaughter-yield` | Mark the approximately60% top-end bonus accepted, retaining the neutral level10 point and unchanged base animal values. | Reconciled in the active drafts; implementation and live verification pending. |
| `specify-megamod-bulk-smelting` | Match current piecewise products and quantities; update all60-steel source-parity claims, examples and accounting tests. Keep source facts separate. | Reconciled in the active drafts; implementation and live verification pending. |
| `specify-megamod-equipment-recycling` | Mark25% basic-only default accepted; reconcile scope of the optional complex-ingredient control without silently expanding initial policy. | Reconciled in the active drafts; implementation and live verification pending. |
| `specify-megamod-weather-ready-arrivals`, `specify-megamod-spacefarer-policy` | Survival-before-armor, all-humanlike vacuum scope and shared environmental eligibility; retain policy choices independently of outfit supply; research SOS2 and resolve existing-pawn supply. | Reconciled in the active drafts; implementation and live verification pending. |

No code, tests, deployed package, source-mod edits or live compatibility results are delivered by
this refinement record. All gameplay acceptance tasks remain open.

## Historical second-batch questions — now answered

Questions11–18 were answered in the [round-two decision record](2026-11-07-decisions-round-2.md).
The table below preserves the questions and original recommendations for traceability, **not the
selected outcomes**. In particular, the user rejected the simulation hold, harsh unmedicated odds,
scarcity cutoff, empty hunting filter and visible Possible matches distinction.
This batch excludes reconfirmation of source defaults such as RePowered's active rates and Easy
Upgrades' short work caps. Those remain source-analysis and playability-review work unless a
demonstrated issue requires a new product choice.

| Question | Choice to resolve | Recommendation for discussion | Existing draft context |
| --- | --- | --- | --- |
| 11 | Does an existing colonist entering vacuum receive free protective gear, or need to bring real equipment? | Require actual gear for existing pawns; automatic outfit generation covers newly generated arrivals. Resolve unsafe/forced transfers explicitly. | Round-one10; S004/S130. |
| 12 | During lengthy raid/trader preparation, hold simulation time while the UI remains responsive, or let the colony run and accept an arrival delay? | Hold simulation time so preparation does not grant extra game time before arrival. | S078/GS-Q1 currently leaves pause/delay/overload policy open; maps already use ordinary paused loading. |
| 13 | If a save changes one Drug Response condition rule, should other conditions keep inheriting their global rules, or should the save own the entire rule collection? | Per-condition Use global, so one local change does not freeze unrelated global rules. | S124/DR01–02 currently proposes inheritance for the whole collection; this is our UI/settings design, not a request to explain upstream behavior. Per-condition insertion/deletion and fallback need specification if selected. |
| 14 | Should recycling a nearly destroyed item recover as much as an intact one, or scale recovery with remaining HP? | Scale the accepted25% base return by remaining condition. | S022/ER-Q1 currently does not scale construction return by condition. Fixed explicit salvage needs separate reconciliation. |
| 15 | Retain the proposed harsh unmedicated self-amputation odds, or make it more forgiving? | Keep it a last resort, with exact odds shown before confirmation. | S056/EA-Q3:13% at Medical0,19.5% at10,26% at20; this0.2 unmedicated factor is our draft choice. |
| 16 | During food scarcity, continue personal reserve collection or suspend optional packing to keep communal meals available? | Suspend optional packing below one day of colony food; ordinary eating/sharing continues. | S123/MC-Q1: routine colonist carrying confirmed, scarcity cutoff not yet selected; proposed one-day threshold requires precise later definition. |
| 17 | Does a newly placed hunting station authorize no species, or automatically authorize a preset such as non-predators? | Start empty and require an explicit species selection. | S064/HS-Q1 currently proposes an empty filter. Existing station choices remain entity state. |
| 18 | Show uncertain medical-operation matches in a separate section immediately, or hide them until Show possible matches is enabled? | Show a clearly labelled separate Possible matches section. | S137 currently proposes separate possible matches based on meaningful effect metadata; no cure claim or permission bypass. |
