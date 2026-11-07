# S117 — Skilled Slaughtering

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin **replacement**, P2-46. Documentation-first static research on **2026-11-06**. No runtime
implementation, source execution, game launch or compatibility acceptance. The user's Cooking/
yield extension is the separate **S118** change; its request is not behavior of this source.

## Identity, documentation and selection

| Input | Exact evidence |
| --- | --- |
| Source | Skilled Slaughtering; MrHydralisk; `MrHydralisk.SkilledSlaughtering`; [Workshop2873284405](https://steamcommunity.com/sharedfiles/filedetails/?id=2873284405) |
| Installed root | `F:/Steam/steamapps/workshop/content/294100/2873284405` |
| Versions | About advertises1.3–1.6; Manifest1.3.0.0; selected1.6 DLL assembly/file1.0.0.0; no advertised1.7 support |
| Selected DLL | `1.6/Assemblies/Skilled Slaughtering.dll`,6656bytes; SHA256 `C51BA483A3970248643A80766172AF91803206D7E9373F6DFF830CA0B3857294` |
| PE identity | MVID `e35733e3-85a6-447b-ae49-6014598cac62`; references mscorlib,Assembly-CSharp,0Harmony,System.Core; read-only PEReader, no assembly loading |
| Full installed inventory |10files: four1.3–1.6 DLLs, About/Manifest/PublishedFileId/Preview, loadFolders.xml and one StatDef XML; no source, license, language files or other content |
| Definition | `Defs/StatDef/StatDefs.xml`,1081bytes; SHA256 `CF729887B47A717A431D8BE92A3B4EEBB6589FEF816DD0C744250EA1F8FBF150` |
| Native |1.7.5000rev1275; root Assembly-CSharp.dll15,636,480bytes SHA256 `D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254`; Version.txt18bytes SHA256 `BAD5BED449D17B2AF39925A3A25007164B2B1F89B29A4E24AFA3B505EDCC535A` |

Read Steam before XML/decompilation. The author describes worker/animal-dependent duration and
acknowledges animals can resume moving during slow handling. A later author comment attributes this
to the handler timer outlasting the animal wait. Compatibility and existing-save safety are author
claims, not accepted beta evidence. Comment questions about milk/wool do not make those features.

[Changelog](https://steamcommunity.com/sharedfiles/filedetails/changelog/2873284405) has five entries:
2025-07-17 update1.6,2024-05-07 update1.5,2022-10-29 compatibility1.4,2022-10-10 manifest change,
2022-10-09 initial upload. Direct read-only fetch retained it after the web reader failed. Steam
HTML97398bytes SHA256 `08D5ED8BA710FE11C8EFB28C898DFD8502468281C88D1D95D1A8F23FD615E29A`;
changelog51447bytes SHA256 `840AE875BA82FA16BA6FE2DF71A7BEAC152F51CBF058A0918CA845219487BBF5`.

No public source/license was located by exact-name searches, GitHub repository search or the
[author's public repository inventory](https://github.com/MrHydralisk?tab=repositories) (21 entries
at inspection). This bounded search does not prove none exists. Installed payload has no license;
the assembly copyright statement is not permission to copy. Plan independent implementation of
behavior; do not copy source DLL/art. Preview was only inventoried, not selected as product art.

`loadFolders.xml` declares root+1.6 for1.6, with no1.7/default entry. Inspected
`ModContentPack.InitLoadFolders` selects the greatest declared version not above the current game,
so root+1.6 is the selected1.7 inspection input. It does not load all four binaries on1.7. The
unusual1.4 entry names root+1.3+1.4, outside this target selection. File hashes are evidence only.

## Complete selected code and XML inventory

| Type/surface | Source-derived behavior |
| --- | --- |
| `HarmonyPatches` | Static startup manually installs one unguarded MakeNewToils postfix; owner `rimworld.mrhydralisk.skilledslaughtering.main`; public postfix method |
| `JobDriverSlaughter_MakeNewToils_Postfix` | Materializes returned toils; computes integer180/speed, then integer size factor times that duration; first debug-name containing WaitWith with defaultDuration180 is mutated; no null/ambiguity checks |
| `SlaughteringUtility` | Internal class, public static speed helper; null pawn returns1; otherwise GetStatValue(SlaughteringSpeed), with static Dictionary keyed by Pawn |
| `CachedSlaughteringSpeed` | Internal struct holding validUntilTick/value; expires only when current tick exceeds the stored expiry (calculation tick+60); no source cleanup/invalidation/save hook |
| `StatDefOfLocal` | Public DefOf field `SlaughteringSpeed`; no other exposed gameplay API proved |

Four source-level types in total (the postfix is a method, not an additional class). No transpiler,
settings/UI class, extra job/work type, save serialization, Def patch, yield modification, XP grant,
map scan or source asset use. Metadata suggests Skilled Refueling and CleaningSpeed as promotions;
these are not runtime requirements. Harmony is required by source; Core/Harmony loadAfter metadata.

The single StatDef adds PawnWork/PercentZero **SlaughteringSpeed**, base1, min0.1. Factors:

| Contribution | Source value / native meaning |
| --- | --- |
| Work | `WorkSpeedGlobal`, using its finalized native modifiers |
| Animals | `0.64 + 0.09 × effective level` |
| Melee | `0.95 + 0.0125 × effective level` |
| Manipulation | Capacity factor with weight1; ordinary capacity value, native maximum default9999 |
| Sight | Capacity capped1.2, weight0.5; multiplicative contribution `0.5 + 0.5 × min(Sight,1.2)` |
| Missing skill tracker | Native StatWorker uses noSkillFactor default1, not level0 for each skill |
| Other modifiers/floor | Native normal offsets/factors, postprocessing/scenario and min/max still apply; minimum10% does not override work incapacity |

Native `SkillNeed_BaseBonus`, `PawnCapacityFactor` and `StatWorker` confirm multiplication and
weighted capacity behavior. Do not mistake weight0.5 for halving the whole stat. Source duration
is two positive-number truncations: first180/speed, then `((1+bodySize)/2) × firstResult`.
Source has no minimum-one safeguard; speed above180 can make the first truncation zero. Target
null/type assumptions and missing toil can throw rather than gracefully disable the patch.

## Native workflow and exact mismatch

Seventeen successful native type extracts retained; one wrong initial capacity-utility namespace
produced an empty file marked unsuccessful. Relevant evidence:

| Native surface | Evidence / consequence |
| --- | --- |
| WorkGiver_Slaughter + WorkGivers.xml | Explicit/auto-slaughter candidates; animal/same-faction/ShouldBeSlaughtered/aggression/reservation/violence/ideology gates; Handling work, required Manipulation |
| JobDriver_Slaughter | Reserve victim; fail aggression or lost slaughter status unless ignoreDesignations; GotoThing Touch; WaitWith180/progress; DoExecutionByCut, AnimalsSlaughtered record, mental-state notification |
| Toils_General.WaitWith | Captures ticks in initAction for PawnUtility.ForceWait; separately sets defaultDuration; failure on missing/despawned/untouchable victim |
| PawnUtility.ForceWait | Starts a finite native wait preserving appropriate posture/downed/deactivated/sleep choices and queued resume; expiry is the captured ticks |
| JobDriver | Sets remaining ticks from defaultDuration before init; saves current index/remaining work/start tick; rebuilds toils on load; cleanup invokes registered actions |
| JobDriver_Wait | Wait ends through job expiry/native conditions; ordinary combat/fire behavior is not a permanent immobilization primitive |
| JobGiver_SlaughterRandomAnimal | Mental-state path uses the same Slaughter job with ignoreDesignations=true; source duration therefore also affects it |
| JobGiver_SlaughterEntity | Uses ExecuteEntity, not this Slaughter driver; remains out of scope |
| ExecutionUtility | Native cut/damage/blood/death/sound and applicable ideology event; not a skill/yield implementation |
| StatPart_NotCarefullySlaughtered + native yield XML | Nonpermanent non-ExecutionCut injuries trigger the native0.66 meat/leather factor; other body/parts/difficulty/malnutrition rules remain separate |

The mismatch is direct: source changes defaultDuration only; the closure still forces the animal
to wait180. A long handler can lose contact when the animal resumes walking, while a very short
attempt leaves the original longer wait until death/cancellation. Source leaves that defect and
the global cached Pawn references intact. The cache can survive game changes with stale references
and an expiry tick from a different game; no source purge is present. These are static findings,
not reproduced game failures.

Proposed owned corrections: compute one fresh attempt snapshot at handling entry, coordinate both
participants for its finite duration, release only that attempt's still-current wait on abandonment,
preserve emergencies and actual native completion, and retain total/remaining work on reload.
Use at least one tick for valid exceptional speed; invalid/unrepresentable data falls back to native
handling. No global speed dictionary, partial timer mutation or string-only toil search is retained.

## Static timing comparators and semantic boundaries

Current Core XML supplies adult base sizes Rat0.2, Cow2.4, Thrumbo4. These are **static comparators**;
actual body size includes life stage/other effects. With work speed/manipulation/sight100%, no
other modifiers and neutral future Cooking contribution, source-derived arithmetic is:

| Animals and Melee | Speed | Rat0.2 ticks | Cow2.4 ticks | Thrumbo4 ticks |
| --- | --- | --- | --- | --- |
|0/0|60.8%|177|503|740|
|10/10|165.55%|64|183|270|
|20/20|292.8%|36|103|152|

At minimum10% and body size4, the source model reaches4500 ticks (75 normal-speed seconds), far
above the animal's source180-tick hold. No new upper shortening cap is proposed: retain meaningful
slow labor, progress and cancellation while fixing hold ownership. Live finalized comparisons and
effective beta frame/tick behavior remain future tasks; no performance or balance acceptance claim.

This change does not alter resources, research, native selection, kill damage, basic meat/leather
values, corpse processing or add XP. Reliable coordinated holding also affects native slaughterer
episodes; that source-scope consequence is explicit, not hidden as a harmless-only player action.
S008 finishing/hunting and S065 companion kills are distinct. The user rejected the earlier global
doubling/skill16 suggestion: S118 must keep base yields and define a deliberate slaughter-origin
Cooking/yield policy, not patch every cut/animal death or change this source's provenance.

## Public contracts, consumers and UI

Bounded installed C#/XML/JSON/Markdown search found only source files, RimPy's package catalog
(`rupal.rimpymodmanagerdatabase`,Workshop1847679158), and four Manifest suggestions:

| Referrer | Package / Workshop | Nature |
| --- | --- | --- |
| Skilled Refueling | `MrHydralisk.SkilledRefueling`,2873628478 | Suggestion |
| Vanilla Outposts Expanded: Additional Outposts | `MrHydralisk.VOEAdditionalOutposts`,2873841790 | Suggestion |
| Vanilla Outposts Expanded: Prisoner Patch | `MrHydralisk.VOEPrisonerPatch`,3002936071 | Suggestion |
| Vanilla Outposts Expanded: Delivery Logistics | `MrHydralisk.VOEDeliveryLogistics`,3006726393 | Suggestion |

Bounded DLL ASCII name search found only the four source version binaries. No callable consumer or
external SlaughteringSpeed modifier was proved; dynamic/uninstalled consumers remain possible.
Preserve the useful Def name where safely admitted, but do not equate it with a source package,
assembly, public Harmony method or DefOf-class facade. Original provider active selects its timing
and Def; ours stays off and does not repair its wait behavior. Unknown same-name content requires
an inspected contract, never destructive replacement.

Source adds no bottom tab/menu/gizmo/configuration. The stat belongs in native pawn information;
duration uses ordinary progress. No new Use-global row is justified by this source. Actual future
Cooking configuration belongs in shared settings under S118. All seven languages are required for
owned stat/explanatory text; source has English XML only. New runtime dependencies with planned
implementation: Harmony plus XML Extensions for guarded additive Def creation; no DLC, source mod,
other repository product or Gateway dependency.

## Evidence and remaining work

[Proposal](../../../../openspec/changes/specify-megamod-skilled-slaughtering/proposal.md),
[design](../../../../openspec/changes/specify-megamod-skilled-slaughtering/design.md),
[observable spec](../../../../openspec/changes/specify-megamod-skilled-slaughtering/specs/megamod-skilled-slaughtering/spec.md)
and [coverage](../replacements/skilled-slaughtering.md) contain the proposed replacement.
SSK-Q1–4 in the [ledger](specification-progress.md) retain admission/lifecycle, actual providers,
finalized timing/UI/shared cost and implementation/native acceptance. No user interview required.

Ignored `artifacts/Refinement/megamod-spec-20261106/skilled-slaughtering/` retains10-file inventory,
28 input identities, public HTML/API responses, read-only PE identity/decompilation, native manifest,
consumer scans and static duration comparators. Review/validation evidence is added there after
completion. No source assembly was executed, Workshop content changed or product art selected.
