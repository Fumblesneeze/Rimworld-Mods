# Most Skilled Bill Restriction: sequential source research

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Queue **S019**, intake **P1-14**, origin **replacement**. Started **2026-11-02** after S018's independent
resolution review. [OpenSpec draft](../../../../openspec/changes/specify-megamod-most-skilled-workers/)
and [coverage report](../replacements/most-skilled-bill-restriction.md) now exist. No implementation or live acceptance.

## Documentation first and provenance

Read the [Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=3757516418)
and [eight change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/3757516418).
The description presents automatic highest-skill restrictions, Include away and Ignore downed.
It advertises broad compatibility and performance; these are author claims, not verified results.
July 2026 notes discuss UI compatibility. Comments report policy drag-paint crashes and an author
reply describes removing ref arguments, but the exact installed DLL still has such a dropdown prefix.
This discrepancy calls for scoped native UI design, not a claimed reproduced crash. Generic Steam
removed/incompatible page headers are not treated as installed-runtime findings.

The bounded public-source search found mirrors and user logs, not an attributable source repository;
the installed package has no source/license file. Do not infer redistribution permission. All behavior
below comes from read-only decompilation and native comparison, not copied implementation.

- Installed root `F:/Steam/steamapps/workshop/content/294100/3757516418`.
- About name **Most Skilled Bill Restriction**, author **MatheusWerneck**, package
  **Matheus.MostSkilledBill**, support **1.5 / 1.6**, no declared 1.7 or About version.
- About SHA-256 **4F3A5E908CE6BC31A1E2DF3DE95F0A06B4AE6E71BD32615269515A2A369CBB0B**.
- Root `Assemblies/MostSkilledBill.dll`, assembly/file **1.0.0.0**, SHA-256
  **0C6BA86C54A0931DA489C1F7EC37879B15D259AAC261DF661F5A136A9DDC1258**.
- Workshop requires Harmony; installed About omits the dependency despite a direct 0Harmony reference.
  Our required runtime dependency must be declared correctly when implementation begins.
- Inventory: DLL, About/preview/PublishedFileId and 26 language directories. No Defs, XML patches,
  LoadFolders, settings class or separate bottom tab. English has four labels/reasons only.
- All 22 decompiled behavioral classes plus AssemblyInfo were read under
  `artifacts/Refinement/megamod-spec-20261102/most-skilled-bill/installed-decompiled`.
  Startup PatchAll owner **YourName.MostSkilledBill**; separate BWM owner
  **Matheus.MostSkilledBill.Compatibility**. Neither is an appropriate owned admission mechanism.

## Existing worker decision

BillExtensions stores three booleans in a ConditionalWeakTable keyed by Bill: mostSkilledOnly,
includeAway, ignoreDowned, **all false** initially. Bill.ExposeData postfix saves same-name keys,
loads them during LoadingVars; Bill.Clone postfix copies all three. No global settings are exposed.

PawnAllowedToStartAnew prefix only acts when enabled, recipe.workSkill exists and bill.Map exists.
It obtains a maximum level and rejects tested workers below it with the localized reason; at/above
the maximum returns to native eligibility. It never directly grants eligibility. SkillRecord.Level
is used, or mechFixedSkillLevel for a controlled colony mech without skills. Ties are allowed. No
quality-stat, inspiration, passion or XP-fraction ranking occurs. Empty benchmark starts at zero.

Local candidates are SpawnedPawnsInFaction(player). Candidates with ordinary skills must be free
colonists or Ideology slaves, with the chosen skill not TotallyDisabled; controlled colony mechs can
use their fixed level. Ignore downed excludes downed candidates only when enabled. A candidate needs
any enabled WorkTypeDef whose relevantSkills contains that skill, or the source's mech fallback when
that work-settings branch is unavailable. The source does **not** check the exact recipe work type,
bill allowed range, recipe-specific capability, reachability, reservation, current job or sleep when
forming its maximum. A tailor can therefore set a Crafting benchmark for another craft work type;
a hidden restrictive range can exclude the benchmark winner later through native checks.

Include away instead enumerates PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction,
then requires current Map == bill.Map or GetOriginMap() == bill.Map, plus the same eligibility tests.
Only humanlike race Defs receive CompPawnOriginalMap through an unconditional runtime comp injector.
Home despawn records the origin; spawn at any player home clears it. Save key is
**originMap_MostSkilledBill**, only while player faction. This does not cover ordinary mech origins.
On native 1.7 IsColonyMechPlayerControlled additionally requires Spawned and Overseen, so the source
predicate cannot be assumed to include caravan mechs. No off-map ownership fallback is justified.

The source caches maxima for **30 ticks**, keyed by map unique ID, skill, includeAway and ignoreDowned.
It sweeps expired keys only above **500 entries**. A GameComponent constructor clears maxima on load;
the separate SkillDef-to-relevant-WorkTypes cache is static. No event invalidation or paused-state
refresh is evident. These are implementation facts, not required player-visible delays or proven
performance. Shared map facts/away associations and fresh decisions should replace per-source caches.

## UI, copying and concrete contracts

WindowStack.Add globally inspects FloatMenus while Dialog_BillConfig exists. It finds translated
AnyWorker by label, inserts Most skilled at index one unless already present, and reflects the bill.
Choosing it enables the flag and clears pawnRestriction, slavesOnly, mechsOnly and nonMechsOnly.
Postfixes on SetPawnRestriction/SetAnyPawnRestriction/SetAnySlaveRestriction/SetAnyMechRestriction/
SetAnyNonMechRestriction disable Most skilled but leave Include away / Ignore downed dormant.

The installed dropdown label patch selects generic Widgets.Dropdown by name/10-parameter count,
closes it over Bill_Production/Pawn and changes ref buttonLabel. Its global IntRange prefix intercepts
any 0–20 range while that dialog is open, paints over the region and substitutes the two checkboxes.
It suppresses rendering but **does not clear allowedSkillRange**. Use the actual bill-specific UI
boundary in our design; unrelated Assign-policy dropdowns and other ranges must remain untouched.
The source can offer Most skilled even for a recipe with no workSkill, where it has no effect.

CompatibilityPatches probes **ImprovedWorkbenches.BillCopyPaste** by type name, without package gate,
then patches public instance **DoCopy(Bill_Production)** and **DoPasteInto(Bill_Production)**.
Three static clipboard booleans snapshot the last non-null single-bill copy and are applied on paste.
No cleanup is attached to BWM Clear, save switch or the worktable overload. Inspected installed BWM
has both methods; its Copy all overload independently fills a list, and Paste new worktable bills
uses BillUtility.MakeNewBill + MirrorBills rather than those single-bill methods. Thus this bridge
does not prove complete list/new-bill/link propagation. Our S014/S017 composition must carry all
three choices directly with the actual snapshot/logical row and destination worker policy.

Public BillExtensions, PawnOriginMapExtensions and CompPawnOriginalMap plus saved keys are potential
contracts. Bounded installed literal scans of source/XML/metadata and DLLs found only the selected
mod's own About/language/DLL hits, retained in `consumer-text-paths.txt` and `consumer-dll-paths.txt`.
No concrete external direct consumer was found; dynamic/uninstalled consumers remain possible.
No broad binary facade, source package spoof or automatic foreign-save conversion is warranted.

## Native 1.7 comparison

Assembly-CSharp **1.7.4995 rev309**, SHA-256
**56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21**. Reuse S014 Bill/WorkGiver/dialog
and S018 Pawn decompilations. Additional PawnsFinder/Pawn_WorkSettings reads are retained in
`most-skilled-bill/game`. A broad first text excerpt truncated; the exact consumed property bodies
were reread with anchored names, so no missing output is counted as evidence.

- Bill.PawnAllowedToStartAnew first returns pawnRestriction == p immediately for a named assignment,
  bypassing its later range/mechanitor checks. With no named restriction, category choices, range and
  mechanitor-only checks apply. Separate recipe/work-giver requirements still govern actual work.
  Selecting Most skilled clears name/category, making the retained hidden range operative. Independent
  review corrected the initial overly broad native-baseline wording; S019 adds no stricter named gate.
- WorkGiver_DoBill checks the bill worker restriction before selecting bound UFT/formed output and
  checks recipe skill requirements separately. A naive new-only restriction can block continuation;
  S014/S017 physical-commitment rules need explicit integration.
- WorkIsActive is GetPriority > 0; it is not a promise that the pawn can reach this recipe/bench.
- The consumed PawnsFinder uses AllMaps (mapPawns.AllPawns, not only spawned) plus caravans/travelling
  transporters, then player faction. Its sibling NoCryptosleep property is distinct and unused here.
  This source path therefore needs careful contained-pawn/origin interpretation, not merely "caravans".
- IsColonyMechPlayerControlled requires spawned colony mech, an overseer subject, and Overseen state.
  Default benchmark is materially different from S018's population query; do not share that decision.

## Proposed refinements and open questions for later

Propose a contextual Most skilled choice, both options initially off, visible retained skill range,
and benchmark workers capable of the recipe's relevant work rather than any work sharing a skill.
Keep busy/asleep/unreachable masters in a quality benchmark, with Ignore downed and Include away as
explicit exceptions/scope controls; do not silently optimize for throughput. Share S017 origin facts
and map candidates without inheriting its stock settings or S018 matching query. All source-modifier
relationships compose directly. No implementation or compatibility acceptance is implied.

- **MS-Q1:** pin recipe-specific benchmark capability, temporary unavailability/contained workers,
  tied maxima and visible range interaction as observable proposed rules; preserve source facts above.
- **MS-Q2:** exact beta workgiver/family/native continuation and controlled-mech/off-map capability
  adapters need proof before implementation; no skill or quality bonus is introduced.
- **MS-Q3:** retain consumed BWM signatures and source save/API gaps; define full owned snapshot,
  shared bench policy, group and lifecycle behavior with no stale clipboard or own-mod patches.
- **MS-Q4:** executable semantic guards, focused tests, seven languages, shared-data measurements,
  independent review and actual native worker/production/away/Assign-UI workflows remain future work.

Questions stay recorded here rather than interrupting the sequential deep dive.
