# S083 — Enemies Use Apparel Too research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
Read-only source research, 2026-11-04. No source assembly execution, gameplay implementation or live
acceptance. [Change](../../../../openspec/changes/specify-megamod-enemy-apparel/),
[coverage](../replacements/enemy-apparel.md), [progress/questions](specification-progress.md).

## Public-first evidence and identity

Selected P2-12/S083: **Enemies Use Apparel Too - EUAT**, author **Argotis**, package
`Argotis.EnemiesUseApparelToo`, [Workshop3746069759](https://steamcommunity.com/sharedfiles/filedetails/?id=3746069759).
Direct Steam returned HTTP429; the English variant was also inaccessible. Read the installed
About description (author's Workshop text) and accessible [public repository](https://github.com/Argotis-dev/Enemies-Use-Apparel-Too-EUAT)
before source analysis. README advertises six equipment types and future settings; the installed
description/source additionally have lances, deadlife, settings and optional Ideology. README is
older than those actual contents; its “no new items” wording omits hidden implant Things/recipes.

Installed input `F:/Steam/steamapps/workshop/content/294100/3746069759`, support **1.6 only**.
Harmony required; load-after Harmony/Royalty/Odyssey/Anomaly. LoadFolders contains a v1.6 branch with
root/1.6 and DLC-gated Royalty, Ideology, Anomaly, Odyssey folders; no asserted beta load path.

| Artifact | Identity |
| --- | --- |
| Public checkout | `19c1401d7533307639388100040a230340954045` |
| Installed/committed DLL SHA256 | `B417D07FC9269023684F925EFAD844DC6735F3B84F7330A03946155DD6B2816B` |
| About.xml SHA256 | `6DC53A54134EDBCC64E986F588F2FB2973B5AF621A2BCCE9322BC2E605753902` |
| Assembly metadata | 0.1.0.0, Debug; informational `0.1.0.0+3a4f93715fc5318a84102a33a564d9abc67a76e8` |
| Current native assembly | 1.7.4998 rev1299; SHA256 `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |

The matching committed binary does not prove a fresh build from HEAD is identical: its informational
commit differs. No license file or affirmative reuse notice was found in the bounded installed/public
scan. Retain author attribution; independent implementation does not copy source code/binaries/art.

Local ignored evidence root `artifacts/Refinement/megamod-spec-20261104/enemy-apparel/`: `upstream`
read-only clone, `binary` ILSpy output, `native` selected native decompilation. Native input is
`F:/Steam/steamapps/common/RimWorld/Assembly-CSharp.dll`, not the old Managed path. Wrong-namespace
decompilation attempts were corrected; failures are not inspected-method evidence.

## Source and compiled behavior inventory

All 19 source C# files and active XML families were read; installed DLL selected paths were
decompiled to corroborate startup/prefix/settings/charge behavior. These are code-derived results,
not observations of source behavior in a running game.

| Source path/type | Active behavior and boundary |
| --- | --- |
| `EUAT_Logic`, startup and `PatchGetAbilityJob` | Conditional unguarded PatchAll; prefix returns first Available violent reachable apparel verb as one-use UseVerbOnThing, expiry2000. No full lance ValidateTarget or explicit actor gate inside prefix |
| `Settings` and `EUAT_PatchIfHarmonyEnabled` | Global UseHarmonyPatch/KeepModAbilities initially true; Ideology field true but missing-key Scribe default false; restart UI. Wrapper condition `!UseHarmonyPatch || KeepModAbilities` means both false still enable wrappers; jump/deadlife additions outside that condition |
| `CompAbility_ChargesApparel` | Finds exact ability object among worn apparel; CanCast requires real item charges; PostApplied calls UsedOnce. Gizmo enumeration writes ability max/remaining charges from item, making UI part of synchronization |
| `Verb_CastAbilityJumpApparel` | Caches first item range, ability activation then JumpUtility with null reloadable comp; charge helper owns payment. Target checks include CanHit, JumpValid and queued job allowance |
| `Utility`, `JobGiver_AIAbilityJump` | First CastJump ability; exact injury/threat/search/landing branches below; sets enemyTarget before destination validation; stops movement when issuing job |
| Jump ThinkTree | Humanlike_PostMentalState priority0, inverted Colonist condition, supported jump apparel check. Broader than hostile-only and not an explicit all-player-pawns exclusion |
| `JobGiver_FleePotentialExplosionJump` | Nearby exploder prototype, logging and retreat logic; tree has no insertion tag and insertion patch is commented out. Not active shipped explosion escape |
| `JobGiver_AIDeploySpawnable` | Turret-position prototype; no XML consumer. Unused melee/jump conditional helpers likewise do not establish active behavior |
| `CompAbilityEffect_MarkDeadlifeGasAtDest` | At cast application, marks intended-destination corpses and estimated gas-affected pawns for caster faction; amount21 cells×255. Projectile handles real release later |
| `HediffComp_RemovedOnDowned` | Sets guest.Recruitable=false on grant; removes owned hediff on downing/death, does not restore recruitability or explicitly remove already cast effects |

### Equipment/XML values and generation

| Family | Source action / generation additions |
| --- | --- |
| Core psychic lances | Wrappers range41.9, warmup2.2; shock125 duration; insanity666, boss restriction; psychic/brain/goodwill effects. No new pool membership |
| Royalty grenadier | Frag12.9/1.5, but wrapper omits native forcedMissRadius1.9; tags SpacerMilitary and orbital armor variant |
| Royalty phoenix | Fire grenade14.9/1.5, miss1.9; SpacerMilitary/cataphract variant; Empire cataphract torso alternate chance0.5 |
| Royalty locust/jump pack | Jump source range/.5 warmup; locust SpacerMilitary/locust/orbital tags, champion torso alternate0.5; jump pack BeltDefensePop and missing-only generateAllowChance0.15 |
| Odyssey hunter pack | Projectile20.9/1; BeltDefensePop and missing-only0.15; with Royalty, traders-guild elite orbital alternatives0.5 and slasher locust0.1 |
| Anomaly turret/shard lances | Turret22.9/1, BeltDefensePop/missing-only0.15; shard lances16.9/2.4, copied shock125/insanity666, brain/goodwill/boss rules |
| Anomaly deadlife | Unconditional ability addition to source `Apparel_DeadlifePack`; projectile23.9/1.5, miss0.1, 21-cell marking quantity; AbilityDuration20 not a demonstrated marking lifetime. No new pool membership |

Some copied lance descriptions refer to the wrong action. Source wrappers can diverge from actual
item behavior (frag accuracy, status duration and custom provider restrictions). Owned use preserves
the current supported item's behavior, except the explicitly specified ranged deadlife extension.

Source equipment generation appends specific torso requirements/tags. Native `ApparelRequirementTagsMatch`
tests alternate chance per candidate, so the advertised0.5 is not a final50% raid share. Required
apparel selection uses body/layer/Stuff/pair restrictions, subtracts price, but does not simply reject
every candidate exceeding remaining budget. Existing explicit item allowance remains authoritative;
ordinary seeded allow chance and ignoreApparelAllowChance still matter. Owned merges preserve these
semantics and required provider gear without duplicate torso requirements. Native specific choices
run before `apparelRequired`, skipping overlapping ordinary pieces: source alternatives intentionally
replace cataphract torso armor, champion plate armor and guild elite marine armor. Owned scope now
explicitly permits those substitutions only, preserving native fallback and unrelated/provider locks.
Current Odyssey `PawnKinds_Salvagers.xml` makes `Salvager_Elite` and `Salvager_Scrapper` descendants of
the targeted guild elite/slasher bases; both are included rather than excluded by guild-only labels.

### Tactical jumps

Health is summed injury severity divided by lethal damage threshold, not remaining HP. Healthy
below0.5 primary melee seeks a nearby landing; unhealthy0.5 with at least2 close threats within2
cells retreats; otherwise ranged firing positions prefer cover for range>5. Last fallback is
injury≥0.25 with `isThreatend(pawn, 30f, 0)`, but source only enters it when shooting search itself fails, not
when its chosen landing fails jump validation. Approach/shoot displacement≥8, retreat can be shorter.
The helper's third argument becomes native maxCount; count starts at0 and returns count>=maxCount.
Thus this source fallback requires **zero nearby threats**, despite its intended30-cell test.
Search range is8+jump range, plus66% ranged weapon range clamped2–20. Source target search requires
visible, spawned, non-downed threats. Some null/range/landing branches are insufficiently guarded.

Owned explicit corrections: no player-controlled automation, current equipment range, ordinary
target/casting-position validation, valid useful landing before movement replacement, bounded
nonprogress, retreat after an invalid shooting landing **only with at least one threat within30**,
and no extra explosion AI. Requiring an actual nearby threat is an owned correction, not source parity.

### Optional Ideology

Conditional XML makes native BerserkTrance and CombatCommand offensive/AI-usable. Tribal_Berserker
gets a custom implant tag and techHediffsChance0.35, with budget100–200 only when missing. Three
hidden implant Things, brain install recipes (Plasteel1), grant hediffs and abilities exist for
BerserkTrance, CombatCommand and MarksmanCommand. No active commander/marksman pawn-generation
consumer was found. Native role command possession is distinct from adding an ability to a boss.

Source special trance cooldown1800 ticks, warmup.5, effect126 seconds. Commands use radius9.9 and
1000-second aura; Marksman inherits RoleAuraBuffBase. Downing/death removes grant but does not restore
recruitability. That exclusion is source-described intent, not automatically treated as a bug.

Owned deviation: independent35% tribal talent chance preserving unrelated implant pools/budgets;
no new physical implant/surgery content. This is not asserted source final implant probability.
No new commander/marksman recipients are invented. Optional talents default Off to resolve the
field/Scribe discrepancy; actual possessed supported abilities retain native effects and cooldowns.

## Current native checks and semantic limits

Inspected `RimWorld.JobGiver_AIFightEnemy`, `RimWorld.Verb_Jump`, `RimWorld.JumpUtility`,
`Verse.AI.Verb_CastTargetEffectLances`, `RimWorld.PawnApparelGenerator`, `RimWorld.PawnUtility` and selected Core/Royalty/
Odyssey/Ideology XML. Native ability selector is now **static**; the source requests `__instance`.
Its caller excludes player colonists/subhumans from this AI use and validates enemy/control state;
selection checks walkable/unreserved casting positions and AICastableAbilities. A source prefix
cannot be accepted merely because the method name exists.

Lance ValidateTarget checks boss, psychic sensitivity and every target-effect comp before base
validation. Native jump checks current origin/range/LOS and inbounds/unfogged/walkable/closed-door
landing; native charge is spent before flyer creation. No automatic refund on that failure is
claimed. Native Verb_Jump also caches range: owned current-range behavior requires its own supported
invalidation/admission, not blind delegation to a stale cache.

**No Data/Anomaly exists in the inspected game.** Assembly types and references elsewhere do not
prove current Anomaly pack/projectile/gas/Def behavior. That content is pending EUAT-Q3; absence
does not establish a rename or justify inventing a substitute. Source supports1.6 only. No installed
source execution or 1.7 compatibility claim follows from these read-only checks.

## UI, persistence, consumers and composition

Source has a global restart-required settings window, native/converted gizmos and combat effects;
no bottom tab. Hardcoded English labels/refusals and empty English LanguageData are not seven-language
support. Owned settings use Combat / Non-player equipment, global/save/Use global, dormant overrides
and main-menu callback guards; actual worn equipment/charges/grants remain pawn/item state.

Bounded installed XML/C#/JSON searches for package/namespace/EUAT identifiers and binary ASCII scans
found only this source, no demonstrated incoming consumer. Public helpers, charge comps, AbilityDefs,
ThinkTreeDefs, settings keys and source XML-extension claims are potential contracts, not a reason
for blanket facades or package spoofing. No source jobs/abilities/settings are imported. Original
EUAT owns new overlap if active; already committed owned state must resolve safely.

Direct owned composition: S004 final hazard-ready loadouts, S051 player specialist-work autonomy,
S078 once-only prepared generation, S081 ordinary burst retargeting and S082 current quality/range.
Native apparel/target listers suffice; no private search/hauling cache. Any actual shared data addition
must use the common owner and current pawn-specific action checks.

## Semantic balance and remaining questions

Three inspected native comparators: grenadier2 charges,25 steel/use,12.9 range/1.5 warmup/miss1.9;
phoenix1 charge,40 chemfuel/use,14.9/1.5/miss1.9; jump/locust5 charges,20 chemfuel/use,60 reloadticks,
23.9 jump range/.5 warmup. Hunter is a single-use pack,20.9/1, with native recipe60 steel/40 chemfuel/
2 components. These are selected unfinalized XML facts, not proof of current finalized provider stats
or balanced raid outcomes. Native Ideology aura1000 seconds=24 game hours; trance126≈3 game hours.

Automatic use raises damage utilization and reduces recoverable charges; expanded armor can raise
loot value. The specs keep current native costs/accuracy and ordinary budget semantics, not invented
compensating damage/raid-point modifiers. Measure raid/player-loot effects, jump effectiveness and
optional recruitment/talent consequences before acceptance. Source heuristics and35% grant are explicit
provisional policy; no simulated or live balance result was produced.

| Question | Remaining work, not a request to explain source behavior |
| --- | --- |
| EUAT-Q1 | Measure NPC scope/tactical thresholds, equipment utilization, loot and raid balance with finalized native/provider comparators |
| EUAT-Q2 | Prove beta action eligibility, exact charge commitment, current jump range, save/interrupt/projectile/flight lifecycles and executable target admission |
| EUAT-Q3 | Inspect genuine Anomaly content plus each DLC/provider generation route and additive restrictions; no blanket beta support |
| EUAT-Q4 | Decide any future commander/marksman recipients/chances separately; validate tribal grant balance and persistent recruitment exclusion |
| EUAT-Q5 | Follow any actual incoming consumers and source/owned save needs before adding a facade or migration; no package spoofing |
| EUAT-Q6 | Implement common settings/seven languages, focused honest-tier RED/GREEN, independent implementation review and native player-workflow acceptance |

## Independent review and validation

`review_enemy_apparel_use` found the zero-threat fallback provenance error; exact current native
PawnUtility corroborated it. Owned one-threat behavior and separate zero/one-threat scenarios now
distinguish source behavior from correction. `review_enemy_apparel_generation` found the native
required-torso substitution contradiction and omitted inherited Salvager scope. Both are now explicit
in spec/design/coverage/tasks. Focused re-reviews report no remaining findings in those scopes.

Typed strict validation passes105 items, zero failed; ten scoped Markdown files have valid local
links and no trailing whitespace. The draft contains12 requirements,32 scenarios and24 unchecked
tasks. All runtime tasks stay unchecked. Document review/schema validation cannot accept behavior.
