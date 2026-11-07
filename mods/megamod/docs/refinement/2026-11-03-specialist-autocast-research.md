# S051 — Auto-Cast Specialist Commands research

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
Origin replacement, **P1-45**. Inspected2026-11-03. Selected source analysis and observable draft
complete; independently reviewed and strictly validated. Review corrected upstream fallback cast
ordering and moved technical mechanisms out of the observable spec; focused re-review found no
issues. Strict validation passed73 items, zero failed. Optional-provider gaps are explicit below.
Nothing implemented, built, installed or run in game.

## Documentation and identity

Read the author's [Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=3459647882)
before source analysis: work-triggered specialist buffs, seven supported commands, shared switches
in settings/right-click gizmos, and optional VEF icon reuse. Author comments explain the default-Off
Majordomo choice because of its drawback. Historical right-click bug reports and hotfix claims
are research prompts, not proof of a present installed bug or of a fix.

| Field | Inspected identity |
| --- | --- |
| Title / author | Auto-Cast Specialist Commands / Linnun |
| Package / Workshop | Linnun.AutoCastSpecialistCommands /3459647882 |
| Installed root | F:/Steam/steamapps/workshop/content/294100/3459647882 |
| Loading | LoadFolders.xml selects root plus1.7; other trees1.5/1.6 not selected |
| Selected assembly | 1.7/Assemblies/AutoCastSpecialistCommands.dll; net10.0, Release, assembly/file1.0.0.0, informational1.0.0 |
| Included source | Twelve C# files under1.7/Source, including Compatibility/ResearchReinvented.cs; all read and checked against selected decompilation |
| Hard dependencies upstream | Harmony and Ideology |
| Load hints | Core/all current DLC, Harmony, RR, SierraOnline.AdvCannibal, SierraOnline.VoidSpecialist, VanillaExpanded.VMemesE |
| License / revision | No license grant or canonical repository revision located in installed inventory or inspected public page; included source is analysis evidence, not a reuse license |

SHA256 identities, for evidence only, never runtime compatibility predicates:

| Artifact | SHA256 |
| --- | --- |
| Selected AutoCast DLL | 519ABC6D74438EE1DBACF038632F97AD4CF0E59BE1762BA9B4EF2C560B84ACA2 |
| About.xml | 45A9EB33E9D96779A4FB91E037FC317F31B8246EF71B83A87BFF8833464A5593 |
| Native Assembly-CSharp,1.7.4995 rev309 | 56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21 |
| RR v1.6/Assemblies/ResearchReinvented.dll | A3292C14B384D244F13AB96CB4899F48A54AA92794F9E1DB68FE2E50EC692749 |
| VME1.6/Assemblies/VanillaMemesExpanded.dll, identity only | BC1E30BD3ACB24B2BC962A0D9049E70369EE5956E46C01C4497AC03BF5B4EC65 |

Ignored inspection outputs: `artifacts/Refinement/megamod-spec-20261103/auto-cast-specialist-commands/`
contains `compiled`, `native`, `research-reinvented`. No upstream assembly, source or art is placed
in the product. Assets.zip inventory contains a baked fallback texture and manifests, not more
source. The source project references Harmony, Assembly-CSharp, UnityStubs and Waylith and was not
built. No downloaded code or mod initialization was executed.

## Full selected-source inventory

Command order in this table describes injector-call order, not guaranteed cast execution order.
The index0 fallback reverses two successive injections; see the native timing analysis below.

| Included file / compiled counterpart | Observed behavior and contract |
| --- | --- |
| AutoCastDefs | Four static named native AbilityDefs; optional properties resolve SO_ButcherCommand, SO_VoidCommand, VME_WorkCommand by name only |
| AutoCastSpecialistCommands | Mod constructor loads one static settings object, unguarded PatchAll with owner net.linnun.autocastspecialistcommands, then optional RR patching; hard-coded English settings rows |
| AutoCastSpecialistCommandsSettings | Seven global bools and same-named Scribe keys; all true except enableAutoCastMajordomoWorkCommand=false; no save-specific or pawn state |
| AutoCastSpecialistCommandsUtils | Prefer direct pawn ability, else actual role ability with requiredMemes check; create Instant toil that rechecks CanCast then TryStartCastOn(pawn); no waited completion or release-build result handling |
| AutoCastToilInjector | Early settings/ability/CanCast gate, materialize existing toil enumerable, insert before first Never toil else at index0; no semantic work match, current-policy recheck or per-job persistence |
| Patch_JobDriver_DoBill_MakeNewToils | Crafting→Production; Cooking→Prepare Feast; every non-null recipe→Majordomo, including other skills |
| Patch_JobDriver_Mine_MakeNewToils | Mining then Majordomo |
| Patch_JobDriver_PlantWork_MakeNewToils | Only PlantSeed, Harvest, HarvestDesignated, CutPlant, CutPlantDesignated; Farming then Majordomo |
| Patch_JobDriver_Research_MakeNewToils | Research then Majordomo |
| Patch_JobDriver_StudyInteract | Type-existence Prepare; whole postfix returns if SO_VoidCommand absent, consequently also suppresses Majordomo study; otherwise Void then Majordomo |
| Compatibility/ResearchReinvented | Active package check, then six reflective postfix applications in one try/catch; failure sets success=false/logs but does not remove earlier partial patches |
| Patch_Pawn_GetGizmos, including nested ProcessInput class | Actual method attributes patch Command.GizmoOnGUIInt and Command_Ability.ProcessInput, despite outer GetGizmos attribute/name. Hovered right mouse-down flips the command-type global bool, plays click and returns Clear; Event.Use is commented out. Input prefix suppresses supported command right-click. Overlay only for exact Command_Ability type,12/24px based on compact display, tries UI/CheckAuto then UI/CheckAutoFallback |

No MainButtonDef/bottom tab, custom Defs/XML patch, repeat scheduler, own Thing cache, dedicated
save schema, per-pawn switches or configuration migration found. Right-click has no explicit
WriteSettings call; do not promise immediate durable global persistence from that path. Overlay
texture presence does not prove VEF package identity and will not be copied as an integration.
No source faction/player-control guard exists. Included source and decompiled selected behavior
agree; that does not prove current Harmony attribute resolution or actual native runtime wiring.

## Native timing, effects and balance evidence

Inspected beta Ability.CanCast, QueueCastingJob/GetJob, Verb.TryStartCastOn, Verb_CastAbility,
JobDriver_Research, DoBill, Mine, PlantWork and StudyInteract work paths. Native research travels
then uses a4000-tick Delay work toil; source first-Never fallback therefore inserts before travel.
Study uses delayed interactions and travel; source does not narrow to only captured pawns.
The index0 fallback also reverses two successive command injections. On native research the
result is Majordomo → Research → travel; on study it is Majordomo → Void → travel when both qualify.
Mining and plant work have a Never work toil; plant work loops queued targets. DoBill collects ingredients,
goes to the bench, makes unfinished work and invokes recipe work. A generic completion-mode search
does not establish which operation uses the buff or when a failed preparation should spend it.

Native CanCast includes comps, charges/cooldown and environmental checks, but is not the complete
manual eligibility path. QueueCastingJob also checks CanQueueCast/CanApplyOn, and manual disabled
checks cover further constraints. TryStartCastOn creates a warmup stance; true means started,
not a completed effect. Activation handles costs/cooldowns normally. These observations motivate
AC02–03's explicit work-start and normal-casting semantics, not a finalized hook implementation.

Native Ideology XML RoleAuraBuffBase supplies0.5s nominal warmup,1000 ability-duration units and9.9
radius; MultiRole has180000 cooldown ticks. The four command hediffs grant nearby colonist buffs:
ResearchSpeed+0.3; PlantWorkSpeed+0.4; Production ConstructionSpeed+0.4 and GeneralLaborSpeed+0.2;
MiningSpeed+0.4. These are raw-source comparators, not finalized loaded values or new megamod
balance constants. The owned spec preserves whatever the admitted native/provider abilities do,
including durations, eligibility and costs, without copying/rebalancing Defs.

## Optional providers and remaining evidence limits

- [Cooking Specialist](https://steamcommunity.com/sharedfiles/filedetails/?id=3753995127) documents
  a cooking role and nearby Prepare Feast buffs. [Void Specialist](https://steamcommunity.com/sharedfiles/filedetails/?id=3751240132)
  documents an Ideology/Anomaly role and nearby entity-study buffs. Neither canonical package was
  found by the installed About scan; source load hints are not independent identity verification.
  Actual provider Def/ability restrictions and beta compatibility remain AC-Q2. No inferred license
  or whole-provider replacement is claimed.
- [Vanilla Ideology Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2636329500),
  installed2636329500, canonical VanillaExpanded.VMemesE: v1.7 LoadFolders selects1.6 and a
  Royalty-gated subtree. Read Majordomo ability/hediff XML. VME_WorkCommand requires VME_Royal,
  belongs to VME_Majordomo with60000 cooldown ticks, nominal0.5s warmup,1000 duration/9.9 radius;
  buff XML has WorkSpeedGlobal×1.5. Author descriptions say recreation is depleted; the exact
  provider code implementing that cost was not traced, so no magnitude/implementation is asserted.
  Preserve all actual provider side effects; the command remains default-Off. This is consumed
  content research, not a full VME assembly review or native compatibility acceptance.
- [Research Reinvented](https://steamcommunity.com/sharedfiles/filedetails/?id=2868392160),
  installed2868392160, PeteTimesSix.ResearchReinvented, advertises through1.6. Read the six
  decompiled MakeNewToils paths named by the source adapter: bench item analysis, in-place analysis,
  terrain analysis, prisoner interrogation, comms learning, bench research. They preserve distinct
  travel/hauling, opportunity/project, power/forbidden and prison/comms checks. Some repeat delayed
  steps; some use Never. The1.6 binary is not proof of beta compatibility. Remaining exact binding,
  native behavior and full provider-specific side effects stay AC-Q2; no full RR port is proposed.

## Contracts, deliberate changes and deferred work

Bounded binary scan over installed Workshop and local Mods DLLs for AutoCastSpecialistCommands or
its Harmony owner matched only the source's1.5/1.6/1.7 assemblies. No actual external consumer was
identified; this is not proof that none exist. Public source helpers/settings are documented, but
no compatibility facade or spoofed package ID is justified. Native ability/role state persists
through native mechanisms; no upstream mod-specific save importer is proposed.

Owned deviations: actual work-start opportunity with current readiness, once-per-job saved markers,
native cast completion and interruption handling, uniform specific-command-before-Majordomo order,
player-controlled humanlike scope, independent
Majordomo study, complete optional-group admission, common inherited defaults and a distinct
bottom-right live selector. No right-click global mutation, broad name-only integration or imported
texture. Ordinary native/input behavior remains the fallback. Source co-install suppresses the
whole owned feature. S010/S014/S018/S019 and later S092 compose directly where paths meet.

| Question | Remaining work, not a request to explain the source |
| --- | --- |
| AC-Q1 | Effective beta work/cast/persistence seams, semantic guards and interruption/reload/loop behavior |
| AC-Q2 | Actual Cooking/Void identities/content, RR beta availability and full consumed provider cost/restriction paths |
| AC-Q3 | Compact seven-language live selector versus configured defaults, map/world/save lifecycle and command composition |
| AC-Q4 | Honest TDD, package/review/performance and final native job→cast→effect→continued-work evidence |

Semantic review: automation reduces micromanagement; it does not grant skills/roles, bypass work
eligibility, change ability balance or maintain permanent buffs. It is deliberately not periodic;
long jobs can outlast a buff. Native restrictions and provider costs still require exact live proof.
