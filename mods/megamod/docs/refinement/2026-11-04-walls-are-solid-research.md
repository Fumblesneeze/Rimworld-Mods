# S072 — Walls are solid research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
Read-only inspection **2026-11-04**. Origin replacement. Owning
[draft](../../../../openspec/changes/specify-megamod-structural-damage/) now exists; independent review
and validation are tracked below. **No implementation or runtime acceptance.**

## Documentation first and identity

[Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=2896548513) and the
[author repository/README](https://github.com/victorguima/WallsAreSolid) were read before code.
Their stated intent is stronger structures against ordinary attacks, with breaching equipment still
effective; hammer-like weapon heads gain a separate crushing attack. Documentation describes
configurable damage factors, optional exceptions for non-wall structures and an escape-path change
from mining to attacking. It acknowledges overlapping combat/AI mods and recommends the exception
option. This is author intent, not an independently verified compatibility or balance result.

Installed title **Walls are solid**, package **Victor.WallsAreSolid**, Workshop **2896548513**,
authors **Victor, wilki24**. Root `F:/Steam/steamapps/workshop/content/294100/2896548513`.
About advertises **1.4/1.5/1.6**, requires Harmony and XML Extensions, and declares
`aelanna.fistnerf` incompatible. It does not advertise 1.7. Source metadata is not a whole-megamod
incompatibility decision. The public repository root contains 1.5/1.6/About/Assemblies/README;
its folder links failed through the browser tool. No license was identified in the installed tree
or inspected repository listing. Independent behavior analysis only; no upstream product copying.

No LoadFolders.xml, Common or 1.7 folder is installed. Native 1.7 ModContentPack.InitLoadFolders
selects the highest applicable version directory when an exact directory is absent, then root.
With this tree that yields **1.6 plus root**. This is static load-selection evidence, not a successful
1.7 load claim. The reference is therefore all eight 1.6 XML files and root
`Assemblies/WallsAreSolid.dll`, assembly/file **1.0.0.0**, informational **1.0.0**. The complete small
binary was decompiled read-only: two behavior classes, generated Settings/Resources, assembly metadata
and access-check attribute. No installed source C# was present. All eight selected XML files were read.

| Input | SHA-256 |
| --- | --- |
| About/About.xml | `45A7B20FC7CDEBB3B2B44781721BD365FF5CA649E526C790D96CD8E46E34D23D` |
| Assemblies/WallsAreSolid.dll | `812C92D59531D86C6D553A8EBEB98AC8EA1F48A811C78530C5A734D7DB8AA426` |
| 1.6/Defs/DamageDefs.xml | `D2660A1A6F9B2CC7C948505190BC4856B770F47B0B0714230A1B7B74531927DD` |
| 1.6/Defs/ManeuverDefs.xml | `6BC9E8E35CF4317BED490C0D92117FC851861C938CDF0ED2C0A523BEFEA89C41` |
| 1.6/Defs/ToolCapacityDefs.xml | `AFCEB306B1ABDA6C625E2194B8D38683FFB1F8ABF1BEDB9FD4F1047D15E0D7E0` |
| 1.6/Defs/WallsAreSolid_Settings.xml | `2852B619CBA9CDBD0018067A0E3B4291DAAD24B584A5187C9E6F71FFCD5264E1` |
| 1.6/Patches/BaseWeapons_Patch.xml | `9352098DE0043E7B7C69EB4D7E48DFD98C737592D360436BF760894EA91BF3B2` |
| 1.6/Patches/Buildings_Patch.xml | `3C85A170C6060417B2E27058E959BF8C31B1EE23385DD4E12DB367E0BE4823C3` |
| 1.6/Patches/Damages_Patch.xml | `10FC2D2B2BC301A4B4FC778B7E91ADB6968EAF54289656294F4533617A0C9F3A` |
| 1.6/Patches/DamagesModded_Patch.xml | `2112D808115AE91F11E63A6F5B1F7161B01FBC2B4620CEE06EDE6F586B4714A1` |

Local artifacts: `artifacts/Refinement/megamod-spec-20261104/walls-are-solid/{upstream,native}/`.
Native reference is 1.7.4998 rev1299, Assembly-CSharp SHA-256
`FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` from this research session.

## Selected XML and settings findings

The source settings are XML Extensions load-time values with restart-required help, not per-save
live settings. Eight sliders have two decimal places; the exception checkbox defaults true and
natural-stone ranged immunity false. No bottom tab, gameplay control or top debug feature is present.

| Family | Passable factor / range | Impassable factor / range | Selected source targets |
| --- | --- | --- | --- |
| Sharp melee | 0.4 / 0..3 | 0.1 / 0..3 | CutBase, Cut, Poke, Stab and named Scratch/Bite selectors |
| Blunt | 0.5 / 0..3 | 0.3 / 0..3 | Blunt and Crush |
| Ranged sharp | 0.2 / 0..3 | 0.2 / 0..3 | RangedStab and named Bullet/Arrow bases |
| Explosion | **0.8 / 0..6** | **1 / 0..12** | Named Bomb base |
| New crushing | 2, no setting | 1.5, no setting | BluntCrushing derived from BluntBase |

**Documentation discrepancy:** the public description gives Bomb passable1/impassable0.8; installed
settings and both UseSettings default arrays give **passable0.8/impassable1**. Do not choose one
silently. The source uses SafeAdd for ordinary families and SafeAddOrReplace for Bomb; exact XML
Extensions safe-operation effects are resolved below; exact finalized active-list values still require
an isolated game. None of this static inspection is loaded compatibility evidence.

BaseWeapons_Patch iterates concrete descendants of BaseWeapon and replaces only a tool-capacity
entry Blunt when that tool's label is exactly `head`, with BluntCrushing. Other capacities are left
in the XML. The new capacity label is smash (crushing); SmashCrushing uses Verb_MeleeAttackDamage
with the new DamageDef and normal smash combat-log rule references. Native BluntBase supplies general
building factor1.5 and the normal blunt worker/armor behavior; no source Def alias has been selected.

Buildings_Patch does not simply exempt a definitive list of turrets/hives/chunks. It uses raw XML
selectors for claimability, parent names, threat markers, passability and absence of the Walls draw
style. Its passable branch includes explicit Standable/PassThroughOnly, turret accuracy and direct
MechBuildingBase children. Actual finalized descendants, doors and overlaps must be enumerated.
It writes per-building reciprocal factors (1/configured or Bomb 2/configured and 4/configured),
with `damageMultipliers Inherit="False"` and hybrid keyed AddOrReplace operations. Risks to resolve:
zero is allowed by every slider despite division; inherited/foreign multipliers may be suppressed or
replaced; unlisted damage descendants may not receive matching compensation. These are static risks,
not observed loaded failures or an acceptable owned merge policy.

Natural-stone immunity adds zero multipliers for RangedStab, Arrow, ArrowHighVelocity and Bullet under
RockBase/damageMultipliers. A separate always-on patch writes Mining0.5 to explicit impassable
Building nodes with statBases, also setting Inherit=False. Source comments/documentation are not proof
of which current natural/built/smoothed walls match. Exact mining scope and native damage must be traced.

DamagesModded_Patch has eight active **display-name** gates: Alpha Animals, Vanilla Genetics Expanded,
Oddities Core, Oddities Weapons, Integrated Implants, JecsTools, VFE Vikings and Alpha Mythology.
Several projectile-like custom names are placed in the sharp-melee family; JecsTools Laser uses fixed
0.2 impassable/0.4 passable and Vikings writes only the impassable ranged value. A commented Ancient
Amorphous block is inactive. Resolve actual package IDs/Defs before selecting adapters; names alone
do not establish behavior. Buildings_Patch emits display-name-gated warnings for CE and an old CAI
title; these are upstream reports, not inspected current integration contracts.

## Binary and native exit behavior

HarmonyPatches starts unguarded PatchAll owner `com.wilki24.rimworld.mod.wallsaresolid`, catching/logging
exceptions. PatchTryGiveJob targets JobGiver_ExitMap.TryGiveJob and replaces the two immediately
preceding constant-true instructions at the first DigUtility.PassBlockerJob call with false, then
logs success. It assumes stack/argument order and does not establish unique semantic cardinality,
effective foreign behavior or safe i−1/i−2 bounds. No source settings toggle controls this patch.

Current native type is **Verse.AI.JobGiver_ExitMap**. It chooses a dig-enabled exit path based on
force/duty/threat/reachability conditions, finds the first blocking building, then calls
PassBlockerJob with both mining arguments true. Native **RimWorld.DigUtility** interprets them as
canMineMineables and canMineNonMineables. False chooses melee-or-wait for mineable blockers;
nonmineables can still use a building-destroyer ranged verb before the second flag is considered.
Reservation failure yields the existing nearby wait/move behavior. Source therefore does **not**
mean every pawn always melees every obstacle, nor does it disable all mining or rewrite pathfinding.
Portal/flying exits, canBashDoors and ordinary exit selection remain separate native paths.

Native Thing.TakeDamage applies every matching per-ThingDef damage multiplier, rounding after each,
before PreApplyDamage and the DamageWorker. DamageWorker.Apply's Building branch then multiplies
the general building factor and the appropriate passability factor; flammability, shambler bonus and
Biotech melee-door factor can also apply. Final damage uses native rounding and remaining HP.
DamageWorker_AddInjury.Apply was read: nonpawns delegate to base.Apply, while pawns take ApplyToPawn.
DamageWorker_Blunt inherits it and uses its inherited blunt effect parameters.
No simple documentation arithmetic establishes final damage for every weapon/material/active mod.

## Completed XML Extensions and current Core trace

Read the selected XML Extensions1.6 binary's HybridPatch, PatchOperationSafeAdd, PatchOperationSafe,
PatchOperationExtended, CreateVariable, Helpers division, ForEachDescendant and PatchOperationMath.
Artifacts are in the sibling `xml-extensions/` directory. No source operation was executed.

- SafeAdd retains an existing direct leaf; it does not resolve inheritance before deciding presence.
- Hybrid Safe on an existing container recurses without updating its attributes. A newly created
  container receives `Inherit=False`; an existing container does **not** necessarily acquire it.
  Matching damage entries still have their multipliers replaced. Earlier risk wording is therefore
  conditional, not a claim that every existing list is reset.
- Helpers divides floating values directly, without a zero guard. The source's allowed zero can yield
  a nonfinite reciprocal string. Loaded parsing/damage failure was not executed or observed.
- ForEachDescendant walks raw named-parent ancestry and filters exact `Abstract="True"` for its concrete
  option. It does not flatten inherited tools. PatchOperationExtended is not delayed by default and
  the selected source requests no special delayed inheritance phase.
- PatchOperationMath can multiply selected leaf values; it does not supply idempotence or group rollback.
  No guessed API is accepted as an implementation design. Finalized inheritance and atomic XML admission
  remain required before the guarded owned contributions are implemented.

Current Core paths inspected include DamageDefs/Damages_MeleeWeapon.xml, Damages_RangedWeapon.xml,
Damages_Misc.xml; ThingDefs_Buildings/Buildings_Structure.xml and Buildings_Natural.xml;
ThingDefs_Misc/Various_Stone.xml and Weapons/{MeleeMedieval,Breach,Weapons_Breach}.xml.

Wall explicitly sets categoryBuilding, Impassable, isWall, HP300 and Stuff5, with Bomb2 and Thump2
target multipliers. DoorBase has Building_Door and HP160 with no explicit passability override. RockBase
inherits categoryBuilding, is Mineable/Impassable/natural rock and has Thump4. Sandstone has HP400;
smoothed sandstone and collapsed rock retain Mineable/RockBase ancestry but set isNaturalRockfalse.
ChunkBase is **Item**, not a building, despite PassThroughOnly. Source raw predicates do not reliably
express the user-facing building categories and can inspect nonbuildings or miss inherited fields.
The fixed mining XPath matches explicit Wall fields while RockBase inherits category elsewhere.

BluntBase's concrete Blunt has general building factor1.5. Poke inherits it but source classifies Poke
as sharp melee. **Demolish also inherits BluntBase**, overriding general factor10 and impassable0.75,
but not passable: source-added Blunt passable0.5 can leak into the breach axe despite the documentation's
breaching promise. Thump is independently15 impassable/7.5 passable; its projectile base is9.
Bomb baseline is4 impassable/2 passable. BombSuper inherits Bomb.

Native JobDriver_Mine.DoDamage chooses80 for isNaturalRock and40 otherwise. When a Mineable's remaining
HP is no more than that amount it calls Notify_TookMiningDamage and DestroyMined directly; that last
hit is not an ordinary TakeDamage invocation. The owned fixed wall-only rule excludes these Mineables
and does not attempt to alter their yield or final removal. Native reset timing uses MiningSpeed and
retains its nonplayer minimum. Nothing here authorizes a new player mining order against a built wall.

### Static comparator matrix

Amounts below assume the named native factors and no other material/quality/attacker effects. They are
pre-final-rounding arithmetic, **not finalized loaded measurements or observed hit results**. Source
exception selectors can alter a particular source result; the owned column uses its explicit scope.

| Attack / target | Native amount | Proposed owned default | Reason / provenance |
| --- | --- | --- | --- |
| Ordinary Blunt10 / wall | 10×1.5 =15 | 4.5 | Ordinary solid0.3 |
| Ordinary Blunt10 / door | 15 | 7.5 | Ordinary passable0.5 |
| Bomb50 / wall | 50×2×4 =400 | 100 | Installed source impassable1 normalized against native4 |
| Bomb50 / door | 50×2 =100 | 40 | Installed source passable0.8 normalized against native2 |
| Bomb50 / natural rock | 50×4 =200 | 50 | Owned rock barrier scope; source selector compensation can differ |
| Thump9 / wall, rock, door | 270 /540 /67.5 | Same | Dedicated breaching excluded from ordinary families |
| Demolish head7.5 / wall, door | 56.25 /75 | Same | Prevent the source's inherited passable-factor leak |
| Mace head15.7 / wall, door | 23.55 /23.55 | 35.325 /47.1 | Fixed crushing1.5 /2; handle Poke remains separate |
| Mining hit / built wall | 40 | 20 | Half-damage contribution; no new job |
| Mining hit / natural, smoothed rock | 80 /40 | Same | Native Mineable last-hit path and yields retained |

Mace provenance also includes Smithing, Crafting3, Stuff50 Metallic/Woody, Work6000, head cooldown2
and handlePoke9. These are comparator inputs, not new balance edits. Wall/door/rock durability,
availability and material costs remain unchanged. Strengthening structural resistance makes ordinary
escape/raid attacks slower while keeping specialized breach equipment useful; zero can make a barrier
unbreakable for that attack. No claim that the result is balanced or that an AI always escapes.

## Optional content identities and exact source coverage

| Source gate | Primary package identity / evidence | Source damage identities / coverage |
| --- | --- | --- |
| Alpha Animals | `sarg.alphaanimals`; [author About](https://raw.githubusercontent.com/juanosarg/AlphaAnimals/master/About/About.xml) | AA_ToxicSting, AA_ToxicBite, AA_ToxicBolt, AA_Electric, AA_VeryToxicSting, AA_FrostClaws; sharp pair |
| Vanilla Genetics Expanded | `VanillaExpanded.VGeneticsE`; installed2801160906 About | GR_GoForTheHead, GR_GoForTheEyes, GR_RabidSlash, GR_DoubleSlash, GR_Acid, GR_BloodFrenzy, GR_VeryToxicBite, GR_LightToxicBite, GR_HairballProjectileDamage, GR_GreatHairballProjectileDamage, GR_ToxicGas, GR_PlagueBite; sharp pair |
| Oddities Core | Workshop2723455136; canonical ID not primary-verified | OCBulletBase; ranged pair |
| Oddities Weapons | Canonical ID remains unresolved | OW_StabDevil; sharp pair |
| Integrated Implants | `lts.I`; installed3223443793 About | HemoBite, StabToxic; sharp pair |
| JecsTools | `jecrell.jecstools`; [author About](https://raw.githubusercontent.com/jecrell/JecsTools/master/About/About.xml) | Laser; fixed passable0.4/impassable0.2 |
| VFE Vikings | `OskarPotocki.VFE.Vikings`; [author About](https://raw.githubusercontent.com/AndroidQuazar/Vanilla-Factions-Expanded-Vikings/master/About/About.xml) | VFEV_Cut, VFEV_CryptoDamageBullet, VFEV_CryptoDamageBolt; impassable ranged only |
| Alpha Mythology | `sarg.magicalmenagerie`; [author About](https://raw.githubusercontent.com/juanosarg/AlphaMythology/master/About/About.xml) | MM_ToxicBite, MM_VeryToxicBite, MM_ToxicSting, MM_Grapple, MM_BleedingWound, MM_UncontrollableBleeding, MM_PetrifyingGaze, MM_ExtraDamageToPiratesHorn; sharp pair |

Remote Abouts verify identity, not selected 1.7 worker compatibility. Third-party logs mentioning an
Oddities ID are not authoritative. Installed Genetics custom damage XML includes custom GeneticRim
workers as well as AddInjury and inherited Bite; exact worker chains need admission research. Installed
Implants HemoBite inherits Bite with a Biotech gate/modExtension, while StabToxic uses DamageWorker_Stab.
Secondary effects must be retained; matching a name or parent does not prove full adapter semantics.
All eight remain intended coverage, with unverified adapters explicitly unimplemented.

Bounded installed XML/C#/JSON searches for source package, BluntCrushing, SmashCrushing and Harmony
owner found only source files and installed CAI metadata. This is not an exhaustive binary-consumer
audit. CAI Workshop3673768803 package `Krkr.rule56` explicitly names `Victor.WallsAreSolid` incompatible.
Fists Aren't Made of Steel installed2845980214 identifies `Aelanna.FistNerf`; behavior was not researched
for a support claim. No genuine callable/Def consumer establishes a required facade yet. Preserve
working contracts when found; do not claim the new damage identity satisfies every exact Blunt check.

## Proposed decisions and deferred questions

The spec chooses installed-source explosion defaults, normalized relative to native damage, and semantic
wall/door/mineable scope. It replaces reciprocal exceptions with skipping the owned factor and keeps
zero valid. Fixed mining remains guarded XML; crushing and save-specific ordinary/immunity decisions
use the common damage policy. No field/list reset or unproven absolute factor parity is intended.
S037 social fights, S023 item destruction and S061 safe removal retain their direct owned rules.

Independent source/native review found two concrete P2 corrections. `Thing.TakeDamage` checks exact
damage identities; installed FTC_BasePermanentStructure (`2887532326/1.6/Defs/Things/FTC_PermanentStructure.xml`,
line61) has a Blunt0.25 multiplier. A new crushing DamageDef would bypass it. Additional read-only native
trace confirms `DamageInfo.Tool` and `RimWorld.Verb_MeleeAttackDamage.DamageInfosToApply` primary
SetTool delivery (lines51–58); secondary extras lack that stamp. The revised design retains Blunt and
all native tools/capacities, recognizes the actual supported primary head and applies its factor at the
common building stage. No new crushing Def, weapon conversion or cosmetic source attack label remains.
This is simpler and preserves existing exact-Blunt target resistance; new observable coverage tests it.

The second finding distinguished DigUtility's ranged UseVerbOnThing return from CanReserve-based
WaitNearJob in melee/mining helpers. The spec now promises nearby wait/move only for failed melee
reservation; ranged failure/interruption remains native, with no invented general fallback. These are
static design corrections, not successful runtime verification.

Independent composition review found two further P2 corrections. The social-fight test now verifies
that equipping a mace does not add equipment attacks to the native pawn verb pool; normal combat is
the actual workflow for proving unchanged pawn damage from a head strike. The exit substitution now
explicitly documents the S061 boundary: combat is outside roof-safe mining, so even Other workers
coverage does not prepare roof before the new attack destroys required support. Native collapse can
follow; other covered mining/removal is unchanged. Combined observable scenarios and acceptance tasks
retain both consequences. The reviewer also confirmed the revised native-Blunt resistance approach.

| ID | Retained issue | Next evidence / decision |
| --- | --- | --- |
| WAS-Q1 | Exact 1.7 target admission and XML inheritance | Effective damage/exit semantics, actual XML guard/transaction phase, retained inherited ordering and exactly-once contributions before code |
| WAS-Q2 | Proposed scope and balance | Review normalized explosion defaults, structural-only exemptions, fixed crushing/mining and immunity coverage; measure finalized wall/door/rock/nonwall cases and playtest real attacks |
| WAS-Q3 | Optional and external contracts | Resolve both Oddities identities; inspect all selected provider workers and real Blunt/source-Def consumers, including CAI/CE/Fists behavior before any support claim |
| WAS-Q4 | AI and owned composition | Observe eligible escape melee/ranged/wait/interruption and zero-damage limits; preserve S037/S023/S061 and inspect S097 only when its own turn arrives |
| WAS-Q5 | Implementation and native acceptance | Focused RED/GREEN, shared settings/seven languages, hot-path measurements, reviewed-build attacks/mining/settings/provider workflows with personally observed results |

Independent review and strict validation are recorded in the sequential ledger. All gameplay tasks
remain unchecked; target documents and executable admission are implementation obligations.

No runtime code/Defs/assets, tests, build/install, game launch, source execution, Workshop edits or
normal save/configuration mutation occurred. No compatibility, damage balance or performance claim.
