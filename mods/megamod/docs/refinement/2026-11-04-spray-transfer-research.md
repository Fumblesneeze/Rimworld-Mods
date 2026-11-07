# S081 — Spray Transfer research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
Origin replacement, P2-10/S081. Inspected2026-11-04. Documentation before full small binary/native
inspection; no foreign code execution, build/install/game launch or Workshop/normal-config mutation.

## Documentation and identity

Primary [Workshop3490194365](https://steamcommunity.com/sharedfiles/filedetails/?id=3490194365)
initially returned a regional Community Hub error. Search found an accessible public
[localized Steam page](https://steamcommunity.com/sharedfiles/filedetails/?id=3490194365&l=latam)
with the original English description: pawns/turrets can change targets during bursts when the old
target goes down/dies; turning Fire at will off disables transfer. Public page lists Harmony as
required, published30May2025, updated29January (the page omits year; indexed metadata says2026),
author displayYatoro煜. Comments about explicit downed targets, RunAndGun, CE and Milira are research
leads, not reproduced defects or accepted compatibility claims. Scraped generic removed/incompatible
banners do not independently establish availability. Third-party index hits were not used as technical proof.

Installed `F:/Steam/steamapps/workshop/content/294100/3490194365/`: one DLL, AboutXML/publishedID/preview.
No source, license, settings, Defs, patches, language files, README or manifest in that inventory.
TitleSprayTransfer, package`Spray.Transfer`, authorX. Advertises1.5,1.6,1.7,1.8,1.9,2.0; these expansive
labels are not compatibility evidence. About has only HarmonyloadAfter, no hard dependency. DLL
actually referencesHarmony; owned implementation must declare it required. No DLC reference found.

| Input | SHA256 |
| --- | --- |
| `Assemblies/SprayTransfer.dll` | `62F93E75C04A9EFC861029087A017E27BD79AC9960FB893EB764E4BC48066DFA` |
| `About/About.xml` | `F71B02D1C941DCF47F8914868D32702895430944D867CFDA5442F904207D8750` |
| Current `F:/Steam/steamapps/common/RimWorld/Assembly-CSharp.dll`,1.7.4998rev1299 | `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |

Assembly/file1.0.0.0, net472, assemblySprayTransfer, title/productSprayShift, copyright2025 with no
named reuse grant. All behavior classes read from ignored ILSpy output
`artifacts/Refinement/megamod-spec-20261104/spray-transfer/source/`. No foreign assemblies copied
into product or run. Decompiler unresolvedHarmony annotation comments do not establish execution.

## Complete source behavior

Two public static classes:

1. `HarmonyPatcher` startup creates Harmonyowner`SprayTransfer`. Enumerates non-abstract Verb
   subclasses, resolves `TryCastNextBurstShot`, appends baseVerb method, filters non-null/declared
   members, and patches each with its transpiler. No exact resolved-method deduplication, semantic
   guard, failure isolation or optional-provider selector is implemented in this code.
2. `SprayTransferPatch.SprayShift(Verb, ref LocalTargetInfo)` returns unless target.Pawn exists
   and healthState differs fromMobile. Pawn caster requires non-null drafter, FireAtWilltrue and
   CurJob.targetA different from oldpawn; CurJob itself is dereferenced unchecked. Non-pawn caster
   skips these checks. If caster implementsIAttackTargetSearcher, call native
   BestShootTargetFromCurrentPosition with NeedLOSToAll|NeedThreat|NeedAutoTargetable. Non-null
   returnedThing replaces the referenced target; no result leaves it unchanged.
3. Public `Transpiler(IEnumerable<CodeInstruction>)` copies incoming instructions and inserts
   `ldarg0;ldarg0;ldflda Verb.currentTarget;call SprayShift` at index0. No predicate/callable/body
   validation, attack-type restriction or atomic fallback. No other feature methods/options exist.

Consequences: broad Verb-family integration, no explicit multi-shot/ranged check, no extra warmup,
no transfer for targetcells/buildings/mobilepawns, no pawn transfer without drafter/FireAtWill,
preservation of any matching jobTargetA (not only playerforced), and no equivalent turret forced/
hold-fire check. Pawn target preference is native searcher'sCurrentEffectiveVerb, which can differ
from the Verb being patched. Do not infer a nearest-target policy or friendlies from the mod name.

## Current native contracts

Fresh extracts in `artifacts/Refinement/megamod-spec-20261104/spray-transfer/native/`:
`Verse.Verb`, `Verse.AI.AttackTargetFinder`, `RimWorld.Building_TurretGun`.

| Boundary | Inspected facts and consequence |
| --- | --- |
| WarmupComplete/VerbTick | Set full burst/state then immediate firstshot; later scheduled shot when interval expires; despawn/stun resets; target can go down during warmup |
| TryCastNextBurstShot | SnapshotcurrentTarget beforeAvailable/TryCastShot; success flash/sound/pawnnotifications/fuel and remainingcount--; failure zeroscount; nextinterval+stance or finalidle/cooldown/callback/perburstfuel |
| Target feedback | Snapshot feeds engaged/attacked/mental/health notifications; later stance usescurrentTarget. A late-only field write cannot prove coherent shot/effects |
| Native save | Verb storescurrentTarget,burstShotsLeft and nextshotinterval/state through native lifecycle; no source custom save/state |
| BestShootTargetFromCurrentPosition | Uses searcher.CurrentEffectiveVerb, min/max bound, thenBestAttackTarget; actual patchedVerb is not an argument |
| BestAttackTarget | Same native attackTargetsCache, hostility, minrange/actualminrange, lordtargetvalidation, threat/autotargetability, optionalgas/roof/burning filters, fog and weapon constraints; ranged branch validatesCanShoot and uses weighted score selection, with separate fallback |
| Weighted selection | Not simply nearest or deterministic highestscore. Actual returned candidate still needs real shooting validation under the selected supported path |
| Turret TryStartShootSomething | Rejects holdfire, unavailable/roofedoverhead attack; respectsforcedTarget; otherwiseTryFindNewTarget; nativewarmup and laterburst |
| Turret TryFindNewTarget | Uses manningpawn where applicable; directfire addsgas/LOS, incendiarynonburning, mortarNoThickRoof; passesIsValidTarget, unlike sourcegenericsearch |
| Hostile overhead exception | Before the filtered path, a50% branch directly chooses a random in-range colonist building when the attack flies overhead and the searcher faction is hostile to the player; it does not apply the later threat/auto-targetability/IsValidTarget predicates |
| Turret IsValidTarget | Playerprisoner exclusions, overheadthickroof, automaticmachineallegiance and mannedplayeranimal rules |
| Turret controls | Holdfire toggle resetsforcedTarget; forcedTarget,currentTargetInt and AttackVerb.currentTarget are distinct; source onlywritesVerb field |

These are static paths on the pinned1.7 binary, not finalized weapon/provider or live shooting proof.
The source's generic search omits meaningful turret context. A straightforward source-like port can
also use the wrong attack's range when a caster owns multiple Verbs. Individual final projectile and
special weapon implementations still require inspection before their support is claimed.

## Selected behavior and explicit differences

- True multi-shot ranged burst transfer, including firstscheduledshot if target falls duringwarmup;
  exclude melee/singleshot/unrelatedabilities instead of applying blindly across allVerbfamilies.
- Preserve source pawn FireAtWill/controller and targetbound-job intent; correct missingjob/null
  handling and extend explicit-target/hold-fire protection to turrets. Hunting and finishing keep
  their prey, including owned S008/S064/S065 composition through direct calls.
- Use actual firing weapon, original shooter authority and native turret/weapon selection policy.
  Do not impose directfireLOS on supportedoverheadfire or omit smoke/roof/specialtarget restrictions.
  Native preferences apply within the permitted transfer set. Its hostile-overhead bombardment
  branch cannot bypass transfer's threat/auto-targetable and actual-weapon predicates; ordinary
  bombardment outside this feature stays native. Calling TryFindNewTarget alone is insufficient.
- Preserve native burst count/spacing/costs/accuracy/cooldown and in-flightprojectiles. Synchronize
  actual aim/feedback as required; no forcedtarget/jobtarget rewrite or duplicate owned effect.
- No replacementavailable => leave native behavior, including possible continuedfire on oldtarget.
  Turning transferpermission off does not invent instant cancellation of the existing burst.
- No new option, tab, cache or content. Existing entitycontrols are appropriate; no common-settings
  exception or new bottom-right toggle. Logs technicalinvariant; added playertext uses sevenlanguages.

## External contracts and compatibility

Bounded installed `.cs/.xml/.md` search forSprayTransfer/Spray.Transfer/SprayShift returned only
sourceAbout; completed ASCII `.dll` symbol search returned only its own DLL. No actual incoming
consumer established; reflection/computedstrings and absentmods not ruled out. The public helper
and transpiler are possiblebindings, not a reason to spoof package/assembly or preserve an unsafe
unrestricted targetsetter. No source serialization or settings import identified.

An installedAbout text search forMilira/WingsOfDemocracy found a fertility mod's description mention,
not a demonstrated provider. Do not treat it as an integration identity. CE/MVCF/RunAndGun/vehicles
need their own exact package/type/semantic evidence before compatibility claims. Sourceactive =>
ownedoverlapoff with boundedstartup explanation; never unpatch foreign owners or infer conflict merely
from anotherpatch. Every actualpatch needs minimal effective/incoming semantics and laterrepatch
invalidation, atomicdependent rollback and warning. No XMLpatch is currently needed; do not create
a speculative target registry or new shared cache for this source.

## Deferred work and verification

SPT-Q1 retains explicitpolicy/balance (multishot scope, controls, noextrawarmup). SPT-Q2 retains
effective1.7 shot/actualVerb/turretfeedback/save/validator proof. SPT-Q3 covers real consumers and
selectedspecialproviders. SPT-Q4 covers focusedtests, package/UI/localization, reviewednative workflows
and playability. No gameplay accepted. Compare ordinaryautomaticburst, ownedtransfer and forcedtarget
with same weapon and targets; static nominal damage does not measure the utilization advantage.

[Change](../../../../openspec/changes/specify-megamod-burst-retargeting/) and
[coverage](../replacements/spray-transfer.md) retain observable scope and all unchecked work.
