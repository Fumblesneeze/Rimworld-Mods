# S086 — Go-juice is medical research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
2026-11-05; specification only, no product edits/build/game launch.
[Change](../../../../openspec/changes/specify-megamod-medical-go-juice/),
[coverage](../replacements/go-juice-medical.md), [ledger](specification-progress.md).

## Public-first identity

[Workshop2971408015](https://steamcommunity.com/sharedfiles/filedetails/?id=2971408015) describes
medical classification to avoid recreational-drug penalties for imperial Hussars, claims binge
exclusion, and recommends Common Sense for inventory use. An author reply confirms the inventory
workaround; a comment is the reported older symptom, not proof of current1.7 behavior. The displayed
author is PYAS, installed author orioM. The page and About list1.4–1.6, not1.7. Retrieved page warnings
about compatibility/removal are not a technical runtime diagnosis.

| Input | Identity |
| --- | --- |
| Entry | S086/P2-15, Go-juice is medical |
| Canonical package | `oriom.gojuicemedical` (About spells `orioM.Gojuicemedical`) |
| Installed root | `F:/Steam/steamapps/workshop/content/294100/2971408015` |
| About SHA256 | `E82E6D5A09226D1A497948A65D53E6F94BAA5169C04B2C0505CD0B27382DF95C` |
| Patch SHA256 | `B462CE6CE3AD966B1B76FF317D67B768C06B1C1F4F9569BFF09F6D2FE510B634` |
| Native input | 1.7.4998 rev1299, root Assembly-CSharp.dll |
| Native SHA256 | `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |

Installed distribution has About.xml, PublishedFileId, preview and one XML patch, no DLL/source
project/settings/language/save code. The patch **is the complete source**; no decompilation of the
mod or GitHub hunt is required to fill implementation gaps. No affirmative reuse license was found
in this bounded distribution/page inspection. No source/art is copied to product.

## Complete XML behavior

`Patches/Gojuicemedical.xml` contains one PatchOperationSequence with one PatchOperationReplace.
It targets `/Defs/ThingDef[defName="GoJuice"]/ingestible/drugCategory` and replaces that scalar with
Medical. There is no pawn/Hussar/title/faction condition, no chemistry change and no separate binge
filter or inventory hook. Missing or changed target has no explicit admission/fallback warning.

Owned change keeps that category intent but adds XML Extensions target checks, idempotence and
diagnostics. It deliberately replaces one scalar; preserving Hard and Medical together is not a
meaningful merge. Unrelated lists/nodes/effects must remain. Original package owns overlap when
present; no package spoof or duplicate implementation. Exact current value Hard is not an admission
requirement: already Medical or compatible recognized prior category changes need sensible handling.

## Native consumers and limits

Read-only current native types retained under
`artifacts/Refinement/megamod-spec-20261105/go-juice-medical/native/`: ThingDef, Pawn_InventoryTracker,
Pawn_DrugPolicyTracker, JoyGiver_Ingest, JoyGiver_SocialRelax, MentalState_BingingDrug,
MentalStateWorker_BingingDrug, AddictionUtility, PawnUtility, ThingListGroupHelper, JobGiver_BingeDrug,
DrugAIUtility, DrugCategoryExtension/enum, ChemicalDef and selected DrugStatsUtility. S085's retained
FoodUtility and CompDrug provide same-hash inventory/effect paths. These are static findings, not
executed gameplay acceptance.

- `IsDrug` still true; `IsNonMedicalDrug` false; `IsPleasureDrug` depends on positive joy, not category.
  Source does not set joy0 or remove the existing chemical comp/effects.
- CompDrug keeps ordinary addiction/overdose/chemical effects and combat-drug history/timestamp.
  It records generic IngestedDrug but omits Hard/Recreational conditional history and the recreational
  timestamp for Medical. Ideology MedicalOnly listens to IngestedRecreationalDrug, explaining the
  author intent. The AllDrugs branch still has generic events. No previous memories are removed.
- PawnUtility.CanTakeDrug handles active dependency exceptions, teetotaler permission and ideology;
  Medical skips category-specific checks, not every generic rule. Drug-policy scheduled/inventory
  routes call CanTakeDrug only for nonmedical drugs. Medical therefore can be scheduled/carried
  by policy when direct inventory use would be refused. Current same-hash JobGiver_TakeDrugsForDrugPolicy
  and JobGiver_MoveDrugsToInventory were additionally decompiled/read and do not add that veto.
  A universal 'all permissions unchanged', 'all-drugs prohibition always blocks use' or 'Medical
  permits everyone' statement would be incorrect; retain the actual initiating path and generic
  consequences after consumption. Independent review prompted explicit route-specific scenarios.
- Current FoodUtility.WillIngestFromInventoryNow accepts CanTakeDrug directly with IngestibleNow.
  Pawn_InventoryTracker.GetGizmos uses this for a single selected player-controlled drafted pawn;
  one usable drug is a command, multiple drugs form a menu. Thus the author-recommended older
  Common Sense inventory prefix is not necessary merely for medical go-juice in the inspected beta.
  Existing owned S022 purpose-specific checks still compose directly.
- Binge claim is **not a guaranteed consequence**. ChemicalDef.canBinge defaultstrue and GoJuice's
  ChemicalDef does not override it. CanBingeOnNow accepts Any; addiction-driven and fallback chemical
  selection can choose Any. Drug group includes every IsDrug. Actual JobGiver_BingeDrug uses
  DrugCategory.IncludedIn, implemented as enum lhs<=rhs, with Medical below Social/Hard/Any. An
  already applicable binge can therefore still target it subject to ordinary access/overdose checks.
  The draft does not add an undocumented stronger veto. GJM-Q1 records whether one is wanted later.
- Positive joy and ordinary joy selection are likewise not removed just by renaming the category.
  No claim of universal recreation exclusion is made.

Inspected Core XML: GoJuice retains80ticks, joy0.4, high severity0.5, rest0.4, psyfocus0.15,
addictiveness0.026, existing addiction offset0.20, need relief0.9, overdose offset0.18–0.35 and
large-overdose chance0.005. Its recipe remains2neutroamine+1yayo,600work, GoJuiceProduction/DrugLab,
mass0.1 and marketvalue53. Penoxycyline is a Medical comparator (18value,0.005mass,600work), while
yayo is a Hard comparator (21value,0.05mass,350work). These are source anchors, not finalized active
values or a new tuning table. Category changes access/history semantics without repricing the drug,
granting free doses, removing risk or making it general wound medicine.

## XML Extensions and actual contracts

Inspected installed-reference decompilation retained with S067 at
`artifacts/Refinement/megamod-spec-20261104/floor-order-shortcuts/xml-extensions/`: Conditional tests
SelectSingleNode(xpath), FindMod supports case-insensitive package-ID matching/true-false branches,
and Log supports a warning field. Verify exact selected1.7 loading before use; operation names are
not invented. The owned patch needs XML Extensions plus its external Harmony dependency. No custom
Harmony patch or imperative Def mutation is justified for a scalar XML change.

All predicates must precede mutation; use explicit unique-target/shape checks, not mere package
presence or an exact whole-Def fingerprint. Preserve provider-added comp/recipe/effect/category lists.
Finalized/load-order drift diagnostics are GJM-Q2, not proved by prepatch XML inspection. No extra
runtime status framework is introduced solely to mirror this one operation.

Bounded installed text search for the source package found its About and RimPy1847679158/db/db.json;
the latter is package metadata, not an executable consumer. There is no mod-defined API/Def/save key
to impersonate. Native and other mods consume the preserved GoJuice identity and final category.
This search is not a universal reflected/compiled-consumer audit.

## Composition, UI and open work

No source bottom tab or settings. Native inventory/gizmos remain contextual ordinary game mechanics;
Medical classification is fixed selected behavior, not an unused global/save configuration row.
No data cache or saved entity component is needed. Shared policy/inventory/treatment features consume
final classification directly, without owned-to-owned patches or another repository-mod dependency.
S085 unresolved dose/risk handling remains gated; Medical does not bypass it. Seven baseline languages
apply to any new explanation and real native UI verification; preview art is outside this change.

| ID | Open boundary |
| --- | --- |
| GJM-Q1 | Stronger universal go-juice binge exclusion is not implemented by this XML on the inspected beta; current draft retains category-only behavior, stronger policy needs its own observable intent |
| GJM-Q2 | Exact1.7 XML Extensions/dependency route, target semantic guards, original/competing providers and finalized startup diagnostics |
| GJM-Q3 | Reviewed native ingestion/ideology/dependency/inventory/partial-dose composition, baseline languages and unchanged chemical/economic balance evidence |

No runtime task is complete. Independent review and focused re-review are complete with no remaining
issues after the permission-route correction. Typed strict validation passes108 items with zero
failures. The draft contains5 requirements,13 scenarios and12 unchecked tasks; these checks do not
accept gameplay. Next entry is S087 Grazing Lands, still queued for its own deep dive;
earlier grazing work records its future composition but does not replace that analysis.
