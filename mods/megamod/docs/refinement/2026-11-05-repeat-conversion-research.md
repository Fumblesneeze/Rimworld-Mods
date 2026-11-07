# S092 — Keep Converting research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: replacement, P2-21. Inspected 2026-11-05. Public documentation first, then complete shipped
1.7 source/DLL and targeted native read-only decompilation. No runtime acceptance.

## Identity and evidence

| Input | Inspected identity |
| --- | --- |
| Original | Linnun, Keep Converting, `Linnun.KeepConverting` / canonical `linnun.keepconverting`, Workshop 3461478214 |
| Root / selection | `F:/Steam/steamapps/workshop/content/294100/3461478214`; LoadFolders v1.7 selects `/` plus `1.7` |
| Metadata | About supports 1.5/1.6/1.7; Harmony and Ideology required; Core, DLC and Harmony load-after entries |
| Selected assembly | `1.7/Assemblies/KeepConverting.dll`, assembly/file 1.0.0.0, informational 1.0.0, Release; shipped project targets net10.0 |
| DLL SHA256 | `714E1E59986FCC0D5B90AC8BEDF29077143885C82BEC7BACF5AF0602EB3D5E35` |
| About SHA256 | `9ADCDF9FCF21424B15F94F3FE03790645D5E90843552F38BB4AF47B0103033CE` |
| Shipped source | Seven C# files and one csproj in `1.7/Source`; no public source URL supplied in metadata/page |
| Source-set SHA256 | `2EDF5A45E5E569B8D4D1C2C21A0F39C1EA59CCB4B63D83AF94B78E191096439C`; sorted relative path + TAB + uppercase file SHA256 + LF, UTF-8, over eight source/project files |
| Native game | 1.7.4998 rev1299; root Assembly-CSharp SHA256 `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |
| Local ignored evidence | `artifacts/Refinement/megamod-spec-20261105/keep-converting/{source,binary,native}` and `source-sha256.json` |

No LICENSE/COPYING file was found in the installed filename scan. Assets.zip contains two baked
textures and bake metadata, not an additional source/license tree. No affirmative reuse grant is
assumed. Independently implement behavior; do not copy the assembly, source or raster assets into
the product. The bundled project and foreign code were read/decompiled, never built or executed.
Identity hashes describe evidence, not compatibility predicates.

## Public intent

The [Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=3461478214)
describes repeatedly using a Moral Guide's Convert ability on an explicit target until conversion
or invalidation, with configurable checks and distance. Its FAQ gives a 2,500-tick check interval.
The author's October 27 update says a guide's mental break no longer clears the assignment and that
the change applies to 1.7. This is ability automation, not the prisoner's warden conversion setting.
The FAQ's removal/performance claims are not adopted as verified compatibility or benchmark evidence.

## Complete source / binary inventory

All seven shipped C# files agree with the selected Release assembly on the relevant behavior.

| File / path | Actual source behavior |
| --- | --- |
| KeepConverting | Loads global ModSettings; renders interval and distance sliders plus three booleans in its own mod settings section |
| KeepConvertingSettings | Static global fields and Scribe keys; defaults/ranges below, no loaded-value validation |
| KeepConvertingUtils | Resolves Convert then VME_LeaderConvert; checks direct ability tracker first, then role AbilitiesFor with required memes checked only in the role branch |
| CompProperties_ConvertAuto | Attaches CompConvertAuto |
| CompConvertAuto | Public convertTarget and convertIdeo; saves target reference under `convertTarget`; reconstructs convertIdeo from current pawn ideology after load and applies target cleanup |
| GameComponent_ConvertWatcher | Serialized `trackedGuides` list, public static tickNow, periodic or next-tick checks; queues ordinary conversion jobs and clears selected invalid states |
| HarmonyPatches / Pawn.GetGizmos | Appends assign/cancel commands when a player-controlled pawn has the chosen ability and its gizmos; assigns target/ideology and registers the guide |
| HarmonyPatches / Command.GizmoOnGUIInt | Adds an active marker by comparing translated description text; uses `UI/CheckAuto` if supplied, otherwise the bundled fallback |
| Patches/Comp.xml | Unguarded additive Comp injection into `Human/comps`; command action dynamically adds the Comp if absent on another eligible pawn |
| Languages / assets | English and German strings; command icon and active-marker fallback, plus packaged bake files; no bottom tab |

Harmony owner is `net.linnun.keepconverting`, installed by unconditional PatchAll. No other patches
or gameplay Defs were found. Assigning uses copied canTarget flags and CompAbilityEffect_Convert.Valid,
but not the complete original TargetingParameters contract. Cancel only clears convertTarget and
plays the low tick sound. It does not withdraw already queued jobs.

Watcher decisions run globally when TicksGame modulo ticksBetweenChecks is zero or static tickNow
is set. RegisterPawn avoids a duplicate list entry. Guide mental state skips the whole iteration.
Guide destruction, despawn, loss of player control, death, null ideology or changed recorded ideology
clear the target and remove its registration. CanUseConvertNow then rejects drafted/downed/crawling/
mental/non-mobile/LayDown guides, invalid targets, sleeping targets when disallowed, different maps,
unspawned/invalid-position targets, the ritual preference, excess straight-line distance, target cell
outside the effective allowed area and InteractionCell reachability with Danger.None.

It selects the ability/convert effect, checks CanCast and Valid, then re-resolves and checks CanCast /
CanApplyOn before GetJob + TryTakeOrderedJob(requestQueueing:true). It does not call CanQueueCast.
After this submission attempt it calls ShouldRemoveAutoConvertTarget: non-humanlike, self, dead,
cryptosleep, hostile or same-ideology targets clear the target field. Null/destroyed input returns
false. No target map-departure cleanup is present, despite public invalidation wording. The helper's
comment also contradicts its actual boolean meaning; callers treat true as remove.

### Source defaults and persistence

| Setting / saved key | Static initial value | Missing loaded-key default | UI bounds |
| --- | --- | --- | --- |
| ticksBetweenChecks | 2500 | 2500 | Integer slider 1–60000 |
| limitDistanceToTriggerAutoConvert | true | true | Boolean |
| maxDistanceToTriggerAutoConvert | 1000 | 50 | Integer slider 1–1000 |
| allowWakingUpTarget | true | true | Boolean |
| skipWhenConversionRitualOffPenalty | false | false | Boolean |

Native LoadedModManager.ReadModSettings returns new T when no file exists, establishing **1000**
as the fresh-install distance. A loaded file missing that key gets 50. The owned default is a
consistent 1000, with this difference documented rather than pretending the source has one value.
The source distance slider stays editable even when limiting is off; owned UI disables that control
while preserving its value. All source settings are global; owned settings add common inheritance.

Comp save skips writing a null target, then post-load assigns current pawn.Ideo to convertIdeo.
The watcher saves guide references separately and removes guides with no target during FinalizeInit.
Assignments cancelled during runtime remain registered until a later load unless another cleanup
branch removes them. There is no source migration API or ownership marker on its queued jobs.

## Native and optional content contracts

Retained native types: Ability, CompAbilityEffect_Convert, AbilityUtility, TargetingParameters,
Precept_Role, Precept_Ritual, Pawn_JobTracker, relevant cast job drivers/verb, Mod and LoadedModManager.
Review also prompted fresh decompilation of CompAbilityEffect_MustBeCapableOf from the same pinned
native assembly, rather than relying on an older unpinned full-game dump.
Pawn.IsColonistPlayerControlled was inspected in the earlier retained native Pawn dump: it is false
during a mental state, so enduring ownership must be distinguished from temporary control loss.

- Convert validates humanlike, non-baby, no target mental state, different ideology and ability to
  be awake. That consciousness test is distinct from whether a pawn is currently asleep.
- Full target parameters contain additional restrictions and prisoner/slave exceptions beyond the
  copied category flags. Preserve the effective native predicate without editing its shared object.
- Ability.CanQueueCast examines current and queued jobs for the same verb/group. CanCast alone
  does not prevent accumulating casts behind another job. Native ordered queueing appends for busy
  pawns; the idle/immediate branch clears the queued list, requiring explicit manual-order care.
- Ability.GizmoDisabled additionally checks component refusals, lord.AbilityAllowed, deathrest and
  applicable caster capability restrictions. CompAbilityEffect_MustBeCapableOf checks disabled
  Social work only through GizmoDisabled; both inspected Convert variants include this component.
  CanCast/CanQueueCast/CanApplyOn alone cannot prove native command availability. Owned cast admission
  and pre-effect validation preserve these refusals without rejecting their own active queue entry.
- Convert's actual job is CastAbilityOnThingMelee. The melee driver travels to touch range and
  checks CanApplyOn during approach; final ability execution provides effects and group cooldown.
  Owned assignment validity and cancellations still require their own admitted effect boundary.
- Native conversion uses current speaker/target factors, attempts ideology conversion, produces
  normal success/failure messages, thoughts and play-log entries. No owned certainty formula is needed.
- Native RepeatPenaltyActive is true only for a qualifying anytime ritual with a recorded completion
  within its 1,200,000-tick repeat window. It does not mean a ritual currently has all prerequisites.

[Vanilla Ideology Expanded - Memes and Structures](https://steamcommunity.com/sharedfiles/filedetails/?id=2636329500)
is the optional Exalted Priesthood provider, canonical `vanillaexpanded.vmemese`, Workshop 2636329500.
Current v1.7 LoadFolders selects the 1.6 tree. Its own metadata requires Ideology, Harmony and Vanilla
Expanded Framework (`oskarpotocki.vanillafactionsexpanded.core`). Read About/LoadFolders,
Abilities_ExaltedPriesthood.xml, LeaderRolePatch.xml and Precepts_Ritual_LeaderConversion.xml.
LeaderRolePatch adds VME_LeaderConvert; that ability requires VME_ExaltedPriesthood and uses native
CompAbilityEffect_Convert. Its group is VME_ExaltedLeader; the corresponding ritual is
VME_LeaderConversion, with explicit source role/ability/group links.

Consumed XML SHA256: ability file
`295178621D7F3E9A2726CAAF6C3ED58D582F2EB0A1E5881AF30E65381ABD71E9`;
leader ritual file `A48C332B87265BCB8A38601AEA34DC33250CBE7DFE3E10E637C91BCC97D10706`.
These are source XML, not finalized loaded Def evidence or a full provider-code/1.7 compatibility audit.
The owned narrow adapter checks actual package/Def/role/meme/effect/job/group/ritual shapes; no broader
VME feature replacement, framework reuse or source texture dependency is inferred.

## Source defects and owned deviations

The following are static deductions, not live reproductions:

1. Repeated checks can queue multiple casts while busy because CanQueueCast is omitted. Owned jobs
   use current/queued group checks and one automatic job per guide, including after load.
2. Missing ability makes CanUseConvertNow false before the inside-branch ability-loss cleanup can
   run. Owned terminal checks precede temporary readiness and clear lost roles/abilities reliably.
3. Null/destroyed targets and map departure are not cleaned consistently; registrations can outlive
   their target. Owned state has one authoritative collection and terminal-first cleanup.
4. Checking guide mental state before terminal conditions can hide unrelated terminal invalidity.
   Owned temporary retention does not mask death, departure or loss of ownership.
5. convertIdeo is not saved; loading can reinterpret an assignment for a changed ideology. Owned
   state persists the original intent and cancels rather than retargeting it.
6. Clearing a target does not cancel already queued jobs; native approach validation alone does not
   know the owned assignment was cancelled. Owned cancellation affects only its proven unfinished jobs.
7. Target-category copying omits meaningful flags and queueing precedes terminal-target cleanup.
   Owned target validation preserves native restrictions and runs before submission/effect.
8. Source ritual preference always looks for Core Conversion even when using the leader ability.
   Owned preference selects the corresponding supported role/group ritual and says repeat penalty,
   not total readiness. It never automates a ceremony.
9. Global fields, translated-text command identification, broad UI draw patch and Human-only Comp
   injection are replaced by common settings, owned contextual rendering and per-game assignments.
10. Source readiness bypasses native command-disabled checks, including Social incapability and
    lord/activity restrictions. Owned casts preserve these at admission and relevant pre-effect
    boundaries while still allowing assignments during cooldown.

Departure and cryptosleep cancel; drafted/downed/sleep/mental/cooldown/range/path waits retain.
The explicit distinction is part of the observable contract, not an implementation guess. Manual
orders and native costs remain; assignments are pawn gameplay state, not settings. S051 specialist
autocast composes through ordinary cast ownership and direct owned code, with no assumed conflict.

## External consumers, coexistence and migration

Bounded installed text search (`.cs/.xml/.json/.csproj/.md`) for package/owner/Comp/watcher found only
the source. An ASCII DLL-symbol scan across Workshop and local Mods for KeepConverting,
CompConvertAuto and GameComponent_ConvertWatcher matched only its 1.5/1.6/1.7 assemblies. No actual
external API consumer was established; this is not proof none exist. Public members alone do not
justify a facade or spoofing another package/assembly identity.

Original co-installation suppresses the owned assignment UI and scheduler absent verified
cooperation. No silent source assignment/settings import or guaranteed error-free removal is claimed.
A future importer requires backup, atomicity, restoration and postconditions under its own contract.
Core/Harmony required on use; Ideology feature-required but whole-product optional; VME/framework
optional. No source/Gateway/private Harmony shipment or repository runtime-mod dependency.

## Static balance and playability pass

Normalize these comparisons to one completed ability cast, not one scheduler check:

| Comparator | Source XML/native anchor | Owned result |
| --- | --- | --- |
| Manual Core Convert | Moralist group 180000 ticks; 3-second warmup; convertPowerFactor 2 with current native speaker/target factors | Identical effect, reaction and group cost; automation only chooses when to issue the existing action |
| Repeated Core Convert | Same ability; source checks every 2500 ticks | Check interval never resets cooldown; one pending cast prevents queue multiplication |
| VME leader Convert | VME_ExaltedLeader group 600000 ticks; 3-second warmup; convertPowerFactor 2 | Preserve provider ability/group, not Core's shorter cooldown |
| Conversion ritual | Native repeat penalty 1200000 ticks when applicable; separate role/group/venue requirements | Optional pause on the corresponding penalty state; no ceremony automation or false readiness promise |

The fresh 1000-cell trigger default can cover ordinary maps; it is intentionally preserved and
configurable, not described as a small radius. Walking still requires a safe reachable target in
the allowed area. Invalid source intervals could divide by zero; owned bounds prevent that without
changing cast balance. Finalized Defs, optional provider side effects, job reachability, reactions,
certainty recovery between casts and live queue/cancellation behavior remain KCV-Q5. Static source
values cannot accept native behavior or performance claims.

## Open obligations

| ID | Required follow-up |
| --- | --- |
| KCV-Q1 | Exact effective submit/start/effect/cancel/save seams, owned job identity, manual-order preservation and minimal executable patch groups. |
| KCV-Q2 | Full target/control/role/meme semantics and exact optional leader ability, group, ritual and provider compatibility. |
| KCV-Q3 | Common inherited settings, consistent defaults/bounds, readable time/range controls, compact seven-language commands and current waiting reasons. |
| KCV-Q4 | Original-provider suppression, proven consumer bindings if found, and direct S051 job/cast coordination without duplicate schedulers. |
| KCV-Q5 | Focused TDD, reviewed package, finalized balance and real assignment/approach/cast/cancel/mental/departure/save/settings/provider workflows. |

[Proposal](../../../../openspec/changes/specify-megamod-repeat-conversion/proposal.md),
[design](../../../../openspec/changes/specify-megamod-repeat-conversion/design.md),
[spec](../../../../openspec/changes/specify-megamod-repeat-conversion/specs/megamod-repeat-conversion/spec.md),
[tasks](../../../../openspec/changes/specify-megamod-repeat-conversion/tasks.md),
[coverage](../replacements/repeat-conversion.md). Review/validation status lives in the progress ledger.
