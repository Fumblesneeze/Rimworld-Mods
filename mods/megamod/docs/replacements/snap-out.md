# Snap Out!

## Identity and provenance

- Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
- Origin replacement **S100/P2-29**, Snap Out!, Weilbyte/Weil, package `weilbyte.snapout`, Workshop
  [1319782555](https://steamcommunity.com/sharedfiles/filedetails/?id=1319782555).
- Selected installed1.6 DLL0.9.1.0; source commit621ba2b953981ddbf1624c055995bb91d44e0fcd.
  [Research](../refinement/2026-11-05-snap-out-research.md) records hashes, actual folder selection,
  MIT provenance, complete selected binary/Defs, native comparison and actual consumers.
- [Owned change](../../../../openspec/changes/specify-megamod-calm-mental-breaks/).
  **Analyzed and drafted only; nothing implemented or runtime-verified.**

## Behavior inventory

All rows link to the [observable contract](../../../../openspec/changes/specify-megamod-calm-mental-breaks/specs/megamod-calm-mental-breaks/spec.md).
Evidence is selected source/binary/XML and native static analysis, not a played workflow.

| ID | Input → observable result | Values / boundaries | Contract scenarios | Status |
| --- | --- | --- | --- | --- |
| SO-01 | Assigned Warden or contextual Calm → approach/conversation | Normal work priorities/capacities; one helper; colonist-only colony supported | Colonist-only colony; Contextual order and competing helpers | Draft |
| SO-02 | Mental-state target → eligibility | Colonist/slave, optional prisoner/visitor; aggression off; care/combat/Android exclusions | Prisoner and visitor permissions; Care and combat exclusions; Android reformatting and solar flare | Draft |
| SO-03 | Complete conversation → calibrated roll | .5h, native negotiation/opinion, score/70 probability, 30 base XP | Neutral relationship; Direction of opinion and capped ability | Draft |
| SO-04 | Aggressive approach → distraction or ordinary retaliation | SocialImpact×.55/.85 gate, ordinary stun rules; no attack invented when blocked | Failed distraction; Zero weight and ordinary stun rules | Draft |
| SO-05 | Failure → shared recipient cooldown and possible retreat | 6h, unrelated interactions excluded;66% safe-room retreat1750–3499ticks | Failure is shared across helpers; Unsafe or disappearing retreat | Draft |
| SO-06 | Success → time alone and complete recovery | Safe bed/local fallback,500ticks; helper+3/.5day/one copy and50–124 extra baseXP | Successful colonist counselling; Recovery cannot travel | Draft |
| SO-07 | Interrupted/loaded episode → safe continuation or cancellation | No stale-state cure, reroll, duplicated reward or deletion of prior catharsis/orders | No collateral catharsis removal; Save or new state during time alone | Draft |
| SO-08 | Configure / debug / read outcome | Common inherited settings; seven languages; session-only top debug | Settings inheritance and timing; Speech and debugging are separate | Draft |
| SO-09 | Co-install/optional want/shape drift → single provider and bounded fallback | Preserved helper memory and existing optional want; no fake package | Original is co-installed; Character Development consumes a success | Draft |

## Interactions and composed result

Owned toddler care and social-fight rules exclude care requests and fights from counselling;
health/capacity changes contribute through actual Negotiation ability/Social impact once. Native
work priorities and access remain authoritative. No owned feature is patched as though external.
One map-native pawn candidate source and shared health/work data suffice; no private pawn/Thing cache.
Current eligibility and relationships are checked at use, not held in a stale pair cache.

VRE Android's actual reflected exclusion contract becomes a package/Def-guarded owned policy;
reformatting and solar flare remain unaffected. Character Development consumes the existing helper
memory through a scoped want-prerequisite adapter, preserving its eligibility/reward. Neither source
is a required content dependency. Source Multiplayer attributes are research evidence only.

## UI audit and relocation

| Source entry point | Purpose | Owned destination / intentional change |
| --- | --- | --- |
| Warden work / native contextual order | Gameplay | Keep native work/order surface; repair colony-only automatic reachability |
| Job progress, speech, messages, memory | Gameplay feedback | Distinguish calming/time alone/final recovery; speech toggle does not hide outcomes |
| Global settings and advanced disclosure | Configuration | Common Social → Counselling; Global default / Current save / Use global, range validation and admitted-attempt snapshots |
| Debug and AlwaysSucceed in settings | Debug/cheat | Top debug surface only, session default off; no ordinary saved override |
| Bottom tabs / bottom-right toggles | None in selected source | Add neither |

Save controls and Use global are disabled at main menu; inheriting controls inactive; dormant
overrides retained. Settings apply to subsequent attempts, presentation immediately; established
cooldowns retain their expiry. Recipient phase/cooldown is entity state. Baseline English, German,
Spanish, French, ChineseSimplified, Russian and Japanese all remain future acceptance requirements.

## Contracts consumed by other mods or saves

| Consumer / evidence | Binding | Owned treatment / verification |
| --- | --- | --- |
| Character Development3770112044 XML/source | MayRequire original package; SnapOut_HelpedSomeoneThought; WQE_WantHelpSnapOut; Social10/reward1000/Neolithic | Preserve thought identity and provider rules with guarded prerequisite adaptation; actual native want completion pending |
| VRE Android2975771801 selected DLL | Original package gate; reflection into private SnapOut.SnapCheck.incompatDef; two state exclusions | Preserve equivalent exclusions directly; no package spoof/private-list facade; exact-provider native cases pending |
| School & Learning3526830120 source only | Reflection SnapOut.SnapDefOf.CalmDownJob; forced student intervention;900tick restore | No installed DLL, so no active consumer claim or facade yet; explicitly unsupported pending compiled-consumer analysis |
| Source saves/settings | Original job classes/Def names, native interaction timestamp, global keys and memory | No silent migration or whole-source drop-in promise; any import needs backed-up atomic conversion and restoration proof |
| Source Multiplayer | Bundled0MultiplayerAPI, registered SyncMethods | Optional compatibility not accepted or inferred for new owned lifecycle/settings |

Bounded installed string search is not an exhaustive all-mod/reflection guarantee. Preserve further
bindings only after finding their actual consumer and observable result.

## Implementation and coverage

**Implemented portion: none for every SO row.** The proposal/spec/design/tasks describe future work.
Intentional deviations: safe typed/faction/care eligibility; real colony-only work scan; episode
revalidation; interaction-result handling; separate cooldown; current random outcomes without same-tick
coupling; cancellable owned distraction preserving independent stuns; narrow allowed counselling
interactions for supported normally blocked breaks; safe recovery without global Wander_Sad edit; intervention-only catharsis suppression;
no clearing unrelated orders; consistent55% distraction default; actual guaranteed debug override.
The exact old phrase set, unreachable CM21 defect, obsolete translation-only options and accidental
hostile/infant targets are not contracts to copy. Source successful-attempt shared-timestamp penalty
is replaced by recovery-phase exclusion, leaving a later unrelated break eligible.

Original active suppresses owned overlap before Def/job registration without modifying the original.
Every actual external Harmony/XML patch needs an executable minimal target contract, effective/incoming
checks as appropriate, compatible-foreign-change tolerance, atomic own-only rollback and bounded
per-patch startup warnings. Optional XML requires XML Extensions and content/package/node guards;
the existing want's prerequisite is intentionally changed, and supported mental-state exception
lists receive only the owned counselling interaction, preserving other fields and entries.
Changed Android exclusion shapes require safe affected-population fallback. Details in design.

## Verification and acceptance

Independent document review and strict schema/link checks are recorded in the
[ledger](../refinement/specification-progress.md). They do not accept runtime behavior. **SO-Q1–4**
retain exact target/state identity, finalized balance/native work, optional integration timing and
language/settings/performance evidence. Every implementation task is unchecked.

Future focused RED/GREEN must use honest host tiers and real isolated processes for native Defs,
patches and providers. On the reviewed build, personally observe normal work selection/contextual
order → conversation → failure/cooldown or time alone/recovery, with aggression, interruption,
blocked destinations, save/load, prior-memory preservation and actual optional want completion.
Retain exact build/mod list, before/action/after views, logs, configuration hashes and cleanup.
No build/install/game launch, Workshop edits, ordinary save changes or publication occurred here.
