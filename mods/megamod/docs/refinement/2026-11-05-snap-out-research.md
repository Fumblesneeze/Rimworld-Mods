# S100 — Snap Out! research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
S100/P2-29, replacement. Inspected 2026-11-05. Read-only documentation/source/binary/native analysis;
**no gameplay test, runtime compatibility claim or implementation**.

## Public documentation first

[Author Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=1319782555) identifies
Snap Out!, by Weil/Weilbyte, advertised through 1.6. Its text changelog reports removal of the Harmony
dependency, multiplayer support and historical formula/settings changes. The main description is
an image at https://i.imgur.com/TUwekIZ.png; the web reader could not fetch that image, so its text
was not invented. The [author repository](https://github.com/Weilbyte/SnapOut) README describes
calming colonists/prisoners out of mental breaks, linking the same image. Public comments reporting
recovery failures or toddler interactions are reports, not reproduced defects. Code below supplies
the detailed contract; community comments are not specifications.

## Identity, selection and evidence

| Item | Inspected identity |
| --- | --- |
| Installed source | `F:/Steam/steamapps/workshop/content/294100/1319782555`, package `weilbyte.snapout`, author Weilbyte |
| About SHA256 | `9F2EF22C0351760923F074F0F29C24D5A80C072E3D6AFCA4C92C4689402A6467` |
| Selected binary | `1.6/Assemblies/SnapOut.dll`, assembly/file 0.9.1.0 / 0.9.1 |
| Binary SHA256 | `0F69C0081171F1A38E9C6A2F3D8D95342EB8C9EE06DAB883A8574C9254A106BC` |
| XML patch SHA256 | `1.6/Patches/CalmDownPatches.xml`: `C75694DED1B298E16E40C37F9CB7CAC17B3C83F165973C83109F1CD9E37FB5A6` |
| Public source | [commit 621ba2b953981ddbf1624c055995bb91d44e0fcd](https://github.com/Weilbyte/SnapOut/tree/621ba2b953981ddbf1624c055995bb91d44e0fcd), 2025-07-12, merge of 0.9.1 |
| Native comparator | RimWorld 1.7.4998 rev1299; Assembly-CSharp SHA256 `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |
| Local ignored evidence | `artifacts/Refinement/megamod-spec-20261105/snap-out/{upstream,binary,native}` |

About advertises 1.0–1.6. **About/LoadFolders.xml is misplaced**: inspected ModMetaData.Init reads
LoadFolders.xml at the package root. There is no root file or 1.7 directory; native folder fallback
selects 1.6 plus root. Root languages therefore remain available. No 1.7 support is inferred from
that selection. Selected assemblies are SnapOut and its 0MultiplayerAPI; no Harmony dependency in
the selected binary. Older version directories' bundled Harmony files are not selected evidence.

Public source was cloned into ignored artifacts, never built/executed. Selected DLL was decompiled
read-only with local ILSpy. All eleven generated SnapOut classes and the selected Def/XML/English
keys were inspected; binary behavior is authoritative over any unverified source/build equivalence.
Public repository LICENSE is MIT, copyright 2018 DustyTB; assembly attribution is Weilbyte 2025.
Preserve the MIT notice if source portions are later reused; no source code, binaries or artwork
are copied into product output by this draft.

## Source behavior and native comparison

| Source location | Observed behavior / important boundary |
| --- | --- |
| WorkGiver_CalmPawnDown | Warden subclass; ShouldSkip is a conjunction of base skip, non-player faction and non-humanlike helper. JobOnThing casts to Pawn, checks humanlike mental state, compatibility, target availability, helper capacities, recipient cooldown and reservation. No explicit colonist-only target filter or hostile-faction rejection. |
| WorkGiver XML | Warden work, priority 18, Manipulation/Talking requirements. Code additionally requires helper Talking/Hearing/Moving. Native 1.7 inherited PotentialWorkThingsGlobal lists only colony slaves/prisoners: automatic colonist/visitor scanning is not proven by source intention. |
| SnapCheck | Target must have a faction and be awake. Prisoners and non-hostile non-player factions have separate options; hostile uncaptured factions fall through. Aggro option required for aggressive states. Substring exclusion list: RunWild, PanicFlee, HuntingTrip, SocialFighting, Fighter, Giggling, Crying. |
| JobDriver_CalmDown | Reserve recipient; approach, wait for interaction availability, find interaction position. Aggressive target first compares seeded uniform 0–0.85 against SocialImpact × StunWeight; failure attacks helper/sets cooldown, success StunFor(CalmDuration). Then wait with progress for CalmDuration. |
| Completed attempt | Interrupt target job, call TryInteractWith but ignore its bool; compare seeded uniform 0–0.7 against formula. Failure messages; aggressive target tries attack; non-aggressive target may be sent to safety. Success gives helper memory and Social bonus, starts forced recovery job. Last-assigned-interaction time is written, including successful completion. |
| SnapUtils.DoFormula | Score = BaseValue + 0.25 × min(opinion,0) + min(NegotiationAbility ×100,cap) × NegMult/100 + opinion × OpnMult/100. Returns score/100; actual probability is clamp(score/70). Recipient opinion of helper, not reverse. |
| SnapUtils.AttemptSendSafety | Only with owned room; integer random 0–99 inclusive and <=65 gives 66%. Forced jogging job to random room cell, then wait seeded integer 1750–3499. No reliable safe/reachable destination admission. Does not end the mental state. |
| JobDriver_SnappingOut | Owned bed destination; prisoner first takes tiny local wander, then still goes to bed. No bed: nearby wander. Wait500 then force ActuallyRecoverJob. Source's exact wander radius parameters .3/2 are not new owned gameplay requirements. |
| JobDriver_ActuallyRecover | Wait1 then unconditionally pawn.MentalState.RecoverFromState; optional RemoveMemoriesOfDef(Catharsis); clear all queued jobs and end current. Missing/new state and interruption need owned correction. |
| Aggressive success | Recover original then TryStartMentalState(Wander_Sad), optionally deleting all existing catharsis; recovery job later recovers the replacement. Non-aggressive success leaves original state until delayed recovery. |
| InteractionDef | SnapOut_CalmDownInteraction, Social base XP30, speech symbol/log entries. Native TryInteractWith awards XP only if interaction succeeds; source still rolls and rewards if it fails. |
| ThoughtDef | SnapOut_HelpedSomeoneThought: +3 mood, duration0.5days, stackLimit1, stackedEffectMultiplier.4, nullified by Psychopath. Extra success XP integer50–124, subject to native Learn. |
| Patch XML | Unconditional replacement of Wander_Sad.colonistsOnly with false, globally expanding eligibility to support source recovery. Owned recovery will not change that unrelated native Def. |
| Messages | MessagesEnabled gates colored floating phrases only; status messages remain unconditional. Twenty-one English phrase keys exist, but integer random1–20 means CM21 is never selected. No tab/gizmo surface in source. |
| Multiplayer | Static constructor MP.enabled then MP.RegisterAll; several SyncMethod attributes. Not evidence that new owned lifecycle/settings/randomness are synchronized. |

Native inspection: WorkGiver_Warden, Pawn_InteractionsTracker, SocialInteractionUtility,
Toils_Interpersonal, Pawn_MindState, MentalState, MentalStateHandler, ModMetaData and ModContentPack.
Pawn_MindState.lastAssignedInteractTime is saved and shared with ordinary assigned interactions,
default -99999. Social interaction checks awake/burning/blocked/provider conditions and valid
positions. MentalState.RecoverFromState clears current state, conditionally grants moodRecoveryThought
when causedByMood, notifies mental-break/fit generators and traits, clears aggressive enemy targeting,
stops jobs where specified and invokes PostEnd. A direct clear is not equivalent to that lifecycle.

Independent review additionally checked native PawnUtility.IsInteractionBlocked and Core mental-state
XML: Berserk/FireStartingSpree permit only Insult and Slaughterer permits none. Merely honoring the
source-ignored interaction result would therefore make these interventions impossible. Owned design
adds a guarded counselling-only allowed interaction to supported state lists, retaining unrelated
restrictions and entries. That also makes the documented 30-base-XP interaction real in cases where
the source's rejected interaction did not award it. Owned distraction now explicitly ends with its
attempt and preserves independent stuns, including cancellation and load; the source's full-duration
StunFor has no corresponding job-end cleanup in its inspected driver.

Core Warden XML priorities include execution110, release100, take-to-bed90, feeding80/70 and chat60;
source priority18 does not automatically preempt them. Core NegotiationAbility base1 uses Social
factor .4 + .075/level and Talking/Hearing factors; read actual stat instead of inventing a skill
formula. Core Catharsis is +40 mood for3days, stackLimit5. Recovery's large mood benefit makes
duplicate or collateral deletion particularly material.

## Configuration and persistence

| Source field / key | New/reset default; load difference | UI range / owned treatment |
| --- | --- | --- |
| MessagesEnabled | true | Common speech option; notifications retained |
| AggroCalmEnabled | false | Common aggression permission |
| NonFaction / TraderCalm | true / true | Prisoners / non-hostile visitors; source trader label is narrower than its predicate |
| DisableCath | false | Common option, suppress this intervention only |
| BaseValue | -25 | Saved, no source UI; fixed documented rule, no invented control |
| NegMult / OpnMult | 50 / 30 | 0–100 each |
| NegotiationCap | 165 | 40–190 percentage points |
| StunWeight | .55 initializer/reset, **.45 missing-key load** | 0–1; owned consistently .55 |
| CalmDuration / SOCalmDuration | 1250 | 500–15000 ticks; common UI0.2–6hours |
| Cooldown / CoolDown | 15000 | 2500–60000 ticks; common UI1–24hours |
| AdvancedMenu | false | Source saves disclosure bool; owned presentation only |
| Debug / AlwaysSucceed | false / false | Source ordinary advanced settings; owned top debug session-only |

Source advanced reset resets numeric exposed settings, Debug and cooldown, but not AlwaysSucceed
or general booleans. AlwaysSucceed sets only the final draw to0; it neither passes negative scores
nor bypasses the aggression gate. Obsolete translation keys mention opinion-only and launch counter,
but selected code has no such active features. Source global keys, saved JobDef/type references,
shared native interaction timestamp and memories are not automatically migrated. No source-owned
map cache exists. Owned cooldown/phase/reward state belongs to participating pawns, not settings.

## Demonstrated external consumer

Bounded read-only installed `.dll`, `.cs`, `.xml` ordinary-string search for source package/name/
job/type tokens, broadened to case-insensitive `snapout`, found Character Development, VRE Android,
School & Learning source and unrelated UI harness mod-list entries. This is not
proof against arbitrary reflection, UTF-16 encoding or uninstalled consumers.

Character Development (`ferny.characterdevelopment`, Workshop3770112044), About1.6/1.7, selected1.6
content with no root load-folder override/1.7 directory, contains
`1.6/Defs/WantDefs/Wants.xml`, SHA256
`AB9E5F9E939FD7B9A270DCF20C06B7905DB506DF3012C864C845D7AC63D47E9B`.
WQE_WantHelpSnapOut has MayRequire=weilbyte.snapout; worker WantWorker_Thought;
completedByThought=SnapOut_HelpedSomeoneThought; reward1000, required Social10, minimumTechLevelNeolithic.
Inspected source WantWorkers.cs checks HasThought; MemoryThoughtHandler_TryGainMemory_Patch completes
matching active wants via the provider's own CompleteWant. This is source/XML evidence, not native
acceptance or a binary-equivalence claim for that provider.

The provider postfix observes the attempted newThought, not a proven successful insertion. Native
TryGainMemory can reject a thought (for example a nullifying trait) before storing it. Acceptance
must inspect actual memory and want state, including blocked/nullified memory, rather than infer
receipt from calling the method. Preserve the provider's own reward rules; do not add a second reward.

Owned contract preserves the thought identity and needs a guarded narrow prerequisite adapter;
name preservation alone leaves the want filtered out. Raw-XML timing and post-admission availability
are SO-Q3. No invented SnapOut.dll facade or package spoof. Installed Toddlers2903359152 defines
RemoveClothes outside source substring exclusions; owned S031 care excludes toddler counselling.
The Workshop comment's specific Uppies state was not established in this installed XML.

VRE Android (`vanillaracesexpanded.android`,2975771801) has an explicit root loadFolders.xml whose
1.7 stanza selects1.6. Selected `VREAndroids.dll` SHA256
`72A5952EAA0CD0C3FD56845F1E1DAF55BC547DFEF3BE5AB86CE45D6E268A7BBA` was decompiled into the local
`androids` evidence directory. ModCompatibility checks the original package then reflects
SnapOut.SnapCheck.incompatDef and adds VREA_Reformatting and VREA_SolarFlared. Preserve those
exclusions through an owned optional adapter, not package spoofing or a copied private list.

School & Learning (`tehdav.schoolandlearning`,3526830120), About1.1.4.6/1.5–1.6, root LoadFolders
selects root+1.6 on beta. **No DLL is present anywhere in this inspected package.** Its source
Utilities/TeacherReactionManager.InterveneExtreme reflects SnapOut.SnapDefOf.CalmDownJob, submits
a forced job targeting the student, then schedules teacher restoration after900ticks; missing
reflection falls back to move/wait and600tick restoration. This source-only lead is not an active
runtime contract. No facade is promised until the compiled consumer and restore behavior are pinned.

## Intentional owned corrections and balance pass

The spec makes colony-only automatic selection work, guards helper/target type and faction, avoids
infant/toddler care cancellation, uses episode identity, honors interaction failure, separates the
recipient cooldown, and never removes prior catharsis or unrelated queued orders. Safe bounded
recovery uses an owned phase instead of globally changing sad wander. Successful recovery excludes
another attempt while active; unlike source's shared timestamp, a later unrelated break does not
inherit a successful-attempt penalty. Ordinary settings inherit globally/per-save; debug does not.

Static comparators, not finalized/game-tested balance:

| Inputs at source defaults | Derived consequence |
| --- | --- |
| Negotiation100%, opinion0 | score25, effective35.714% success |
| Negotiation165%, opinion0 | score57.5, effective82.143% success |
| Negotiation165%, opinion+100 | score87.5, capped100% success |
| Negotiation100%, opinion-100 | score-30, zero success |
| SocialImpact100%, aggression enabled | distraction64.706%; neutral Negotiation100% full success about23.109% across both gates |
| Normal success | 0.5h conversation + travel +0.2h alone; helper+3 for12h and30+50–124 baseSocialXP; native catharsis only if warranted |
| Failed attempt | 6h recipient lockout, no success memory; optional66% retreat lasting0.7 to just under1.4h |

No new research, item, cost, medical treatment, recruitment or combat damage rule. Actual finalized
stats, work reachability, aggressive retaliation and recovery outcomes remain future exact-mod-list
native verification. Source defaults are preserved except documented corrections; no optimization
or multiplayer claim is accepted from static inspection.

## Open questions and status

- **SO-Q1:** exact admitted 1.7 work/context/state lifecycle, serialized episode identity and recovery hooks.
- **SO-Q2:** finalized comparators and causal native balance/workflow checks.
- **SO-Q3:** actual optional prerequisite timing/availability, wider proven consumers and explicit Multiplayer scope.
- **SO-Q4:** common settings, debug/language lifecycle, measured costs and final reviewed-build evidence.

[Change](../../../../openspec/changes/specify-megamod-calm-mental-breaks/),
[coverage](../replacements/snap-out.md), [ledger](specification-progress.md).
Independent review and strict validation are recorded in the ledger after completion. All runtime tasks open.
