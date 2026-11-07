# Spray Transfer

## Identity and provenance

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin replacement **S081/P2-10**; package`Spray.Transfer`, authorX (WorkshopdisplayYatoro煜),
[Workshop3490194365](https://steamcommunity.com/sharedfiles/filedetails/?id=3490194365&l=latam).
Inspected2026-11-04, assembly/file1.0.0.0 SHA256
`62F93E75C04A9EFC861029087A017E27BD79AC9960FB893EB764E4BC48066DFA`.
Full two-class binary, metadata and current1.7 shooting/turret/search paths read. No distributed
source/license in bounded inventory; no reuse grant inferred or assembly/code copied.
See [research](../refinement/2026-11-04-spray-transfer-research.md) for source/native facts and hashes.

[Proposal](../../../../openspec/changes/specify-megamod-burst-retargeting/proposal.md),
[design](../../../../openspec/changes/specify-megamod-burst-retargeting/design.md),
[spec](../../../../openspec/changes/specify-megamod-burst-retargeting/specs/megamod-burst-retargeting/spec.md),
[unchecked tasks](../../../../openspec/changes/specify-megamod-burst-retargeting/tasks.md).

## Behavior and coverage

All rows **specified only; nothing implemented or accepted**.

| ID | Observable action/result | Boundary |
| --- | --- | --- |
| SPT01 | Old automatic pawn target goes down → remaining burst can change target | Current native-valid threat, no new shot/warmup/movement; no candidate leaves ordinary behavior |
| SPT02 | Fire control/explicit order → transfer respects authority | FireAtWill, HoldFire, targeted pawn/turret, hunting/finishing, faction-relative permissions |
| SPT03 | Candidate selection → actual firing weapon must be able to shoot | Range/LOS/fog/roof/smoke/manning/specialfilters; no alternate-weapon reach |
| SPT04 | Transfer → normal shot effects/counts/costs and coherent feedback | No in-flight redirect, free/refunded/replayed shot, save-state duplication or extra completion |
| SPT05 | Existing contextual controls → no extra UI/state owner | No modsettings/tab/corner toggle/cache; direct ownedcombat/hunting composition |
| SPT06 | Source/changed path → isolated provider/native fallback | Executable target specs and bounded warning; compatible modifications allowed |

## Deviations and possible conflicts

Source prepends an unguarded targetsetter to broadly discovered burst methods. The draft selects
true multi-shot ranged attacks and explicitly protects turret forcedtargets/HoldFire as well as
pawn target-bound work. Sourceonly writesVerbtarget, omits turret-specific filters and consults the
searcher's effective attack, which may differ from the actual firing attack. Owned behavior corrects
those mismatches while retaining no-extra-warmup transfer and native shot accounting.

Native hostile overhead targeting also has a separate random colony-building bombardment branch.
Transfer retains threat/auto-targetable eligibility even on that path; ordinary bombardment outside
transfer remains unchanged. A native target-search result alone does not prove transfer eligibility.

No arbitrary angle/delay or nearest-target algorithm is added. No target at all leaves native firing
unchanged; this is not a guarantee of zero overkill. No melee/single-shot/ability redirection,
blanket custom-weapon compatibility, new settings or source save migration is selected. These scope
decisions and balance advantages remain explicit SPT-Q1–4 work before implementation/acceptance.

Directcomposition with S008/S064/S065 preserves hunting/finishing prey and companion sequencing.
Owned S072/S075/S079 damage/armor/fire behavior still processes each actual shot once. No internal
Harmony stack. Native attackTargetsCache supplies candidates; no competing Thing index is needed.

## UI audit and state

Source adds no UI, Def, setting or serializedstate. It reads existing pawnFireAtWill; owned behavior
also respects native turretHoldFire and explicittarget controls. These are entity gameplay state,
not global/save settings. No new bottom tab/debug/corner control. Native visibleaim and actual
target feedback must agree; any added explanatorytext is translated in all seven languages.
Technical IDs and log-only diagnostics stay invariant.

## Contracts and dependencies

Public `SprayTransferPatch.SprayShift(Verb, ref LocalTargetInfo)` and transpiler can be reflected;
bounded text/ASCIIbinary search established no incoming consumer. Do not equate that with proof
none exist. No fake source assembly/package/namespace or unsafe public targetsetter facade.
Native ongoingburst save behavior stays native; no automatic migration or normal-save edits.

Sourceactive => owned overlap off with bounded diagnostic. Otherspecialproviders require exact
optional package/shape contracts; CE/MVCF/RunAndGun/Milira claims are not established from comments.
Core and Harmony required for implementation; no DLC/XMLExtensions requirement unless laterXML
uses it. SourceAbout onlyloadAfterHarmony is not sufficient metadata to copy. No bundledsource,
HarmonyDLL, otherrepositorymod or Gateway dependency/reference/loadhint.

Every actualpatch needs its own semantic targetdocument/check, compatible-effective-code handling,
laterrepatch behavior, atomicowned-only rollback and bounded startup warning. No whole-bodyhash or
no-other-patch gate, speculative probing of foreigntranspilers or broadPatchAll.

## Verification

SPT-Q1–4 retain policy/balance, effective shot/selection/lifecycle, consumers/providers and native
acceptance. [Progress ledger](../refinement/specification-progress.md) records review/validation.
All runtime tasks unchecked. Final proof requires native firing/control actions and personally
observed real projectiles/targeteffects/costs/savecontinuation on the reviewed build, with exact
providers and cleanup evidence. A loadedpatch or direct targetmutation is not proof.
