# S089 — Healer Mech Serum Choice research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Inspected2026-11-05. Origin: replacement. Public description first, then all selected runtime source,
read-only binary/native decompilation and bounded consumer searches. No runtime acceptance.

## Identity and reproducible evidence

| Evidence | Inspected identity |
| --- | --- |
| Source | Syrus, Healer Mech Serum Choice, Workshop2714095848, About `Syrus.HMSChoice`, canonical `syrus.hmschoice` |
| Installed root | `F:/Steam/steamapps/workshop/content/294100/2714095848` |
| Selection | About1.3–1.6; selected1.6/Assemblies/HMSChoice.dll, assembly/file1.6.0.0; Harmony required/load-after; no1.7 declaration |
| DLL SHA256 | `71EC536114541FCF55CC746726AD6C0B5DE72B49D953C067FE0E0B9B2111BC18` |
| About SHA256 | `4189F33496658A7D3F6D399964CB7F4D3F5B7A707332AEC5D726510DA2C2DA5A` |
| 1.6/Patches/Patches.xml SHA256 | `E3D6FCBBE27C8BF53986C8A82D585DD0AD6380B9346DAC80B21788E5A0937A57` |
| Public source | [ChrisF-127/HealerMechSerumChoice](https://github.com/ChrisF-127/HealerMechSerumChoice), commit `d58bff499ef6f02bcc7addec0356855218e9fe8e`; committed1.6 DLL matches selected hash |
| Native game | 1.7.4998 rev1299, root Assembly-CSharp.dll SHA256 `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |
| Local ignored evidence | `artifacts/Refinement/megamod-spec-20261105/healer-serum/{upstream,binary,native,mfi,multiplayer}` |

Project targets net48/C#7.3, references1.6.4518 and Harmony2.3.6, and publicizes game members. Its
personal deployment post-build is not executed. No LICENSE/COPYING file was found in the bounded
source filename scan; no reuse permission is assumed. Independently specify behavior, preserve
attribution and ship neither foreign code nor assemblies. Hashes identify evidence, not admission rules.

## Public intent and full source inventory

The [Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=2714095848)
describes choosing the condition after normal item use and spending the item only after confirmation.
It includes animal administration, Archotech Medicine and Healing Potion compatibility, and excludes
Pawnmorpher's mutagenic serum. The author's discussion distinguishes requested earlier selection and
Anomaly ability support from delivered behavior. The1.6 update removed older patches; older branches
are not the selected runtime contract.

| Complete selected component | Source/binary behavior |
| --- | --- |
| CompUsable_FixHealthConditionChoice | Rechecks CanBeUsedBy(pawn,true), opens chooser, returns immediately; callback enumerates CompUseEffects by descending OrderPriority, assigns selected Hediff on its choice effect, calls every effect inside one outer try/catch. Null choice does nothing. Does not call base UsedBy/useMessage. |
| CompUseEffect_FixHealthConditionChoice | Public mutable HediffToHeal; null is no-op. Missing part calls private native Cure(part,pawn) through publicized access; other conditions call Cure(Hediff). Sends ordinary positive notification. Inherits generic CompUseEffect, not native worst-condition effect. |
| Dialog_HediffSelection | Visible && isBad && everCurableByItem && !FullyImmune; missing parts additionally reject added replacements on part/ancestors and missing parent. Snapshot sorted by health-card part priority. Part/whole-body, condition, native severity labels/colors; no selected row initially; Confirm only after selection, Cancel callback null. 500x500 scrolling modal, forcePause, multiple same-type windows allowed. No settings or serialized pending state. |
| HarmonyPatches | Owner syrus.hmschoice; reflects MoreFactionInteraction.More_Flavour.MysticalShaman.Notify_CaravanArrived. Replaces interval beginning at MechSerumHealer string through method-name DoEffect with a fixed-local helper call. Helper automatically fixes worst condition. No package guard, semantic/cardinality proof or transactional rollback. |
| Selected XML | Adds usable compClass and replaces the worst-healing compClass for MechSerumHealer; Def-only conditionals repeat for ArchotechishMedicine/HealingPotion. No current animal recipe patch. Add can duplicate existing compClass; whole component lists are not replaced. |
| Localization | Five English keys for title/no eligible condition/part/condition/severity plus native Confirm/Cancel/WholeBody. No mod settings, bottom tab, toggle, asset or own cache. |

Source shortcomings are inspection-based risks, not reproduced live bugs: raw captured item/Hediff
references survive native job completion; no confirmation-time eligibility/custody check, no duplicate
completion guard, mutable effect selection remains set, optional items use serum-specific title,
source outer catch can skip later effects, and the shaman patch assumes compiler locals and an
unbounded interval. Preserve intended player behavior, not those unsafe mechanics.

## Inspected native1.7 paths

- `JobDriver_UseItem` checks manipulation, ordinary use eligibility, item reservation/access, then
  waits the configured duration and invokes CompUsable.UsedBy. `CompUsable` normally rechecks with
  `forced:false, ignoreReserveAndReachable:true`, applies descending effects with per-effect catches,
  and sends configured useMessage. Source does not preserve this final-check shape.
- `Recipe_AdministerUsableItem` checks the ingredient's usable on the **patient**, ignoring final
  reach/reservation checks, then uses it. AvailableOnNow uses TryGetWorstHealthCondition after base
  eligibility. ConsumeIngredient is empty. The item DestroySelf effect splits/destroys one unit.
- Core `Items_Exotic.xml` explicitly defines AdministerMechSerumHealer; Animal base and Human race
  XML both list it. RecipeDefGenerator generates ingestible drug recipes, not this usable-item one.
- Source chooser eligibility and native worst-condition selection are not equivalent. Native ranking
  includes life-threatening/bleeding/brain/missing-part/immunizable/addiction/permanent/injury paths;
  the owned item command and medical bill need the chooser's actual eligible set without dropping
  ordinary actor/patient restrictions.
- `HealthUtility.Cure(Hediff)` removes the target, then all same-Def conditions for
  cureAllAtOnceIfCuredByItem. Private Cure(part,pawn) restores the part. Pawn_HealthTracker.RestorePart
  recursively restores descendants, removes their conditions except keepOnBodyPartRestoration, and
  dirties/rechecks health state. One chosen operation need not remove only one displayed condition.

These are static code/XML observations. Doctor ingredient cleanup, stack transfers while the modal
is open, bill completion and saving during a pending choice still need native causal verification.

## Real consumers and bounded compatibility evidence

Installed text searches across `.cs/.xml/.json/.csproj` found source/self references and mod-manager
or translation metadata, but no established installed Archotech, medieval, MFI or Pawnmorpher provider.
Do not convert an About description mention or the same Def name into an installed-provider claim.

Public consumer research adds two concrete contracts:

1. [More Faction Interaction](https://github.com/emipa606/MoreFactionInteraction), read-only clone
   commit `dd6b085b4f1a6b91500ce742ea88d9a38859c728`, About `Mlie.MoreFactionInteraction`,1.0–1.6.
   `Source/MoreFactionInteraction/More Flavour/MysticalShaman.cs` chooses the sickest caravan member,
   constructs a temporary MechSerumHealer, looks up CompUseEffect_FixWorstHealthCondition, and calls
   DoEffect directly. It then shows its event text and removes the world object. This explains the
   source compensating patch. Preserve native component assignability/direct automatic behavior
   rather than blindly copy that transpiler; exact installed-binary support remains unverified.
2. [Multiplayer Compatibility](https://github.com/rwmt/Multiplayer-Compatibility/blob/master/Source/Mods/HealerMechSerumChoice.cs),
   commit `1a050f05b8b3292b8b1f8b0662c6bc58d98112f9`: MpCompatFor(Syrus.HMSChoice), reflection of
   HMSChoice.Dialog_HediffSelection, SelectedHediff field, Close(Hediff), DoWindowContents; sync worker
   resolves the first matching window. This is an actual source-API consumer, not proof the adapter
   works for our package or multiple pending dialogs. No automatic facade/package impersonation.

Source-described optional items are candidates, not admitted adapters. Public lookup of Medieval
Quest Rewards (Continued), Workshop3288448627, returned unavailable Workshop content. Archotech
fork search returned different identities across continuations; none establishes the selected
item's current behavior. HMSC-Q3 retains exact primary source/About/effect inspection before any
activation. Absence/unknown fork must remain safe. Unnatural Healing and mutagenic/resurrector serum
are outside scope; do not generalize from shared healing vocabulary.

## Proposed owned corrections and composition

- Keep after-preparation selection, but own dose custody, current eligibility, one completion and
  cleanup. Pre-treatment cancellation/rejection loses nothing. If an unexpected callback fails after
  side effects, report known dose outcome and do not auto-refund/retry; no arbitrary health rollback.
- Preserve native automatic component calls via assignable effect and a separate selected operation.
  Do not retain public mutable selection as ambient state. Native subtype feasibility remains a gate.
- Explain actual recipient/item and native group/restoration effects; seven-language contextual UI,
  no new tabs/settings. Pending patient choice is gameplay state. No global/save setting warranted.
- Replace only admitted scalar XML fields with XML Extensions content/shape/package checks;
  preserve unrelated comps and ordering, reject unknown effect semantics, no partial conversion.
- Original HMSChoice suppresses owned overlap; multiplayer choice disabled pending its own contract.
  Compose medical policies directly. No autonomous rare-treatment permission from Drug Response,
  Priority Treatment or Smart Medicine; no FoodTracker partial ingestion of this usable dose.
- Use live health/holder facts and existing shared candidate data. No source-specific persistent
  list, map scan or per-tick cache. No migration of earned health or source settings.

## Static balance/playability pass

| Comparator | Inspected Core XML anchor | Consequence for this draft |
| --- | --- | --- |
| Healer mech serum | Market1300, stack10, useDuration600; medical workAmount600, one ingredient; sound and DestroySelf | Keep one physical rare item and configured preparation; change target authority only |
| Resurrector mech serum | Market1700, stack10, separate corpse-targeting action, useDuration0 | Not a selected treatment item; no new resurrection behavior |
| Glitterworld medicine | Market50, potency1.60, quality maximum1.30 in Items_Resource_Manufactured.xml | Ordinary medicine is not substituted for a serum or granted its cure power |

Values are source XML anchors, not finalized gameplay values or new hardcoded mod constants. No
crafting/trader/research rebalance is proposed. Optional healing providers are not installed-proven
comparators; finalize them only when an adapter is admitted. Player authority over scarce treatment
increases precision without adding doses, group healing beyond the native cure, or free repeated
work. Final progression, available bills, physician reachability and exact dose accounting remain
HMSC-Q5 native acceptance work.

## Open work and status

| ID | Outstanding obligation |
| --- | --- |
| HMSC-Q1 | Exact custody/bill/save cleanup and fault outcomes; confirm cure-consequence wording and proposed one-pending-per-patient UX in native use. |
| HMSC-Q2 | Executable effective XML/method/component contracts, idempotence, group rollback, benign/changed providers, native subtype lookup and once-only effects. |
| HMSC-Q3 | Exact optional fork identity/binary/effects; MFI direct-call proof; multiplayer remains off until owned synchronization is specified and accepted. |
| HMSC-Q4 | Focused TDD, seven-language UI, reviewed package and personally observed native direct/doctor/animal/cancel/stale/save/provider workflows. |
| HMSC-Q5 | Finalized scarce-item comparators, unchanged acquisition/cost/work/power, reachable medical jobs and no duplication/free-treatment loop. |

[Proposal](../../../../openspec/changes/specify-megamod-healer-serum-choice/proposal.md),
[design](../../../../openspec/changes/specify-megamod-healer-serum-choice/design.md),
[observable spec](../../../../openspec/changes/specify-megamod-healer-serum-choice/specs/megamod-targeted-healing-items/spec.md),
[tasks](../../../../openspec/changes/specify-megamod-healer-serum-choice/tasks.md),
[coverage](../replacements/healer-serum-choice.md). Nothing implemented; independent review and
strict-validation checkpoint is recorded in the progress ledger, not inferred from artifact creation.
