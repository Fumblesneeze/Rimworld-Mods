# Drug Policy Fix — sequential deep dive

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Research date **2026-11-02**. Queue **S029**, photo **P1-23**, including the user's handwritten
addition **“also remove not found drugs”**. Findings below are read-only source evidence, not live
verification. [Owning change](../../../../openspec/changes/specify-megamod-drug-policy-maintenance/).

## Public documentation first

The [Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=2195390245) advertises
automatic additions/removals in existing policies and alphabetical drug names. Mlie continues
pelador's mod and credits sable-starflower for improved Cherry Picker support. It requires Harmony.
Its add/remove and multiplayer claims are author statements, not accepted compatibility evidence.
The [public repository](https://github.com/emipa606/DrugPolicyFix) repeats that intent. The Workshop
changelog endpoint was inaccessible through the web tool on two attempts; the pinned Git history
and shipped assembly provide the inspected release identity instead.

Comments mention native overlap since 1.4 and a missing-drug exception during preset creation.
These are leads only. The inspected 1.7 code independently establishes both the built-in backfill
and the unchecked preset lookup; no reported failure was reproduced in a running game here.
Generic Steam page removed/incompatible boilerplate is not used as compatibility evidence.

## Exact inputs and provenance

| Input | Inspected identity |
| --- | --- |
| Installed source | `F:/Steam/steamapps/workshop/content/294100/2195390245`; `Mlie.DrugPolicyFix`; modVersion 1.7.0; supports 1.1–1.7 |
| Installed About.xml SHA256 | `340D9C61E6D1CA243F05B056D575FDB6FA01FC174A59B7ECC34B7B345647F01C` |
| Installed `1.7/Assemblies/DrugPolicyFix.dll` SHA256 | `BE53BE819B2489D7A230AC8F1C6B6C6336D7A9BFDC010BEE9D0FEC8A6F043D20` |
| Assembly identity | Assembly/file 1.7.0.0 / 1.7.0; Release; informational `1.0.0+44491ef0e9bcfdaa655a021f1b84dba35a923a98` |
| Public checkout | `ed3666385d5e1ffe970f68fff240c72f3163544d`, 2026-10-23, “Mod updated for 1.7 and passed autotests”; its committed 1.7 DLL matches installed bytes |
| Native game | `F:/Steam/steamapps/common/RimWorld`, 1.7.4995 rev309, net10.0 |
| Native Assembly-CSharp.dll SHA256 | `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21` |
| Optional Cherry Picker inspected narrowly | Workshop 3521312241, `Owlchemist.CherryPicker`, 1.6/Assemblies/CherryPicker.dll; SHA256 `4F4AB849B5A3F5BF9FDC497607DE05B03035ED920E2C955FE6660C89BDAF73E1`; not a 1.7 admission claim |

Local evidence is under `artifacts/Refinement/megamod-spec-20261102/drug-policy-fix/`: `upstream/`,
`installed-decompiled/`, `game/` and `cherry-picker/`. All seven source classes and corresponding
installed decompilation were read. The installed package has no behavior Def/Patch XML or settings
UI; it contains versioned assemblies and English/German/French/ChineseSimplified text. The source
project was inspected, **not built**; its pre-build task would terminate RimWorld processes.

Attribution: pelador (original), Mlie (continuation), sable-starflower (Cherry Picker improvement),
Dimos (German). The installed/repository LICENSE is MIT, copyright 2020 Mlie; assembly metadata also
mentions MIT/pelador. The Workshop description retains CC BY-NC-SA 4.0. Record this discrepancy
without resolving provenance by assumption. Independently implement the observable contract; copy
no upstream code, assembly, artwork or translation. Any later reuse needs its own license check.

## Complete source behavior

Paths below are relative to `upstream/Source/DrugPolicyFix/`.

| Path / entry | Actual behavior and limits |
| --- | --- |
| `HarmonyPatching.cs` | Static startup `PatchAll`, owner `com.Pelador.Rimworld.DrugPolicyFix`; no semantic guard |
| `DrugPolicyDatabase_MakeNewDrugPolicy.cs` | Only Harmony patch: priority-0 postfix sorts the returned policy; runs before a preset's name/source and explicit entries are applied |
| `DrugPolicyFixComponent.cs` | `LoadedGame` calls correction then sorting once per component instance; the guard is not saved; no `StartedNewGame` correction hook |
| `DrugPolicyFix.cs:CorrectPolicies` | Collect all distinct ThingDefs with ingestible/non-None drug category; remove policy entries that fail the same predicate; add missing identities; log changes |
| `DrugPolicyFix.cs:addNewDrugToPolicy` | New entries start without pleasure/schedule permissions; set addiction permission for addictive drugs; derive pleasure permission from translated **policy label** for three named defaults |
| `DrugPolicyUtility.cs` | Checks addictiveness and Alcohol/Smokeleaf chemical identity; Smokeleaf lookup assumes that chemical exists |
| `DrugPolicySort.cs:SortPolicy/SortPolicies` | Drop null drug rows when a nonempty valid result remains, clear addiction permission for nonaddictive drugs and pleasure permission for nonpleasure drugs, replace underlying entry list with label order |
| `NonPublicFields.cs` | Public static readonly field-ref into native private `DrugPolicy.entriesInt` |

The source's candidate test lacks native `category == Item`. Correction dereferences `entry.drug`
before checking whether the entry itself is null. Neither correction nor sorting proves that a
nonnull drug is still registered in the active database. `AddDistinct` compares entry objects, not
drug identity, so separately allocated duplicate rows can survive. Sorting returns without writing
an empty result when all entries are invalid. Removal counts overwrite the total for each policy;
sort logs count processed nonempty policies rather than actual reorderings. These are code-derived
defects/limits, not reproduced runtime bugs.

Source defaults **if its missing-row branch actually runs**:

| Policy label equals current translation | Pleasure permission for new row |
| --- | --- |
| SocialDrugs | Social-category drug, subsequently cleared if not a pleasure drug |
| Unrestricted | Any pleasure drug |
| OneDrinkPerDay | Pleasure drug using Alcohol or Smokeleaf chemical |
| Anything else | Off |

Every branch enables addiction permission only if addictive, leaves scheduled use off, inventory
quantity zero, frequency 1 day and both thresholds 100%. It does **not** schedule a new alcohol drug
or copy the fresh Beer preset's inventory quantity. Renaming a policy or changing language changes
this label test. Sorting later cannot repair an already inappropriate pleasure grant.

## Native 1.7 overlap and ordering

Read complete native `DrugPolicy`, `DrugPolicyDatabase`, `DrugPolicyEntry`, `DrugPolicyDef`, `Policy`,
`Dialog_ManageDrugPolicies`, `Pawn_DrugPolicyTracker`, `JobGiver_TakeDrugsForDrugPolicy` and
`JobGiver_MoveDrugsToInventory`; read `ThingDef` drug properties and Core's `DrugPolicyDefs.xml`.

- `DrugPolicy.InitializeIfNeeded` already adds all Item/IsDrug definitions missing from the policy,
  with addiction permission true and other entry fields at ordinary defaults. It sorts by the drug
  component's `listOrder`, assuming that component exists.
- `ExposeData` PostLoadInit removes null entries/null drug references and calls that initializer
  **before** the source's LoadedGame repair. Consequently normal 1.7 loads already have the new
  rows: the old source's label-derived pleasure rules do not normally run. Native repair does not
  remove nonnull disabled/non-drug/stale-definition entries and does not initialize a null whole list
  in that PostLoadInit branch.
- `NewDrugPolicyFromDef` creates the base policy, assigns the preset name/source, enables pleasure
  if `allowPleasureDrugs`, then copies each explicit preset row via a throwing drug lookup. A stale
  or disabled explicit drug can therefore fail before a late LoadedGame callback repairs anything.
  Source MakeNewDrugPolicy sorting runs before these copies, so its normalization can be overwritten.
- Core SocialDrugs explicitly grants Beer/Smokeleaf pleasure; Unrestricted enables pleasure broadly;
  NoDrugs has no explicit rows. OneDrinkPerDay explicitly schedules Beer every day, pleasure on,
  inventory 1; Smokeleaf only gains pleasure. Explicit rows have addiction false unless specified;
  other initialized rows start true. The “No drugs” label therefore does not mean native addiction
  permission is off. Do not silently reinterpret it.
- Native `Policy` stores ID/name; its unique load key includes both. Database default is the first
  policy; deletion checks assigned pawns. Keep policy objects, names, IDs, ordering/default and pawn
  references. `CopyFrom` copies source and every entry's drug/permissions/frequency/thresholds/count.
- Both scheduled-use and inventory-refill job givers iterate entries and choose the first eligible
  available drug. Source alphabetical **storage order** therefore changes actual work priority, not
  merely the display. Native dialog uses that same list order to draw rows.
- Tracker rules retain frequency/taken-history, mood/recreation conditions, overdose safeguards,
  ideology and pawn capability. Policy maintenance must not reset those records or bypass them.
  S024's potential-mood restriction remains an additional condition, not a new permission grant.

Owned choice: preserve the inspected 1.7 missing-row defaults, not resurrect the dormant legacy
label heuristic. Fresh preset construction still applies its actual valid entries/pleasure rule.
Display alphabetical rows by identity without reordering the underlying gameplay list. These two
intentional deviations are specified explicitly and retained for later preference review in DP-Q1.
Document review identified an addition-order ambiguity, now corrected: admitted in-session additions
receive the complete native priority ordering before use, just as native initialization would order
them. Appending alone would make selection change after reload. Cleanup-only boundaries retain
surviving relative order when native initialization does not otherwise reorder the list.

## Content removal and provider boundaries

“Missing” is interpreted as absent or disabled **content**, not out-of-stock, forbidden, distant,
unresearched or temporarily unreachable physical items. The photo wording does not explicitly
define that distinction; this is the proposed context-grounded choice, not a later user answer.

The inspected CherryPickerUtility removal branch clears a drug's category to None, sets its tech
level to Archotech and removes matching rows from DrugPolicyDef templates. The ThingDef can remain
registered to preserve existing content. This explains the need to remove **nonnull non-drug**
saved rows. CompProperties_Drug is on its preserved component list. This is a narrow integration
inspection, not S038's full deep dive and not proof that the 1.6 provider runs safely on beta.
Use finalized active-content facts; don't bind this feature to those private implementation details.
Owned S038 will call the shared reconciliation boundary directly after its content decision is final.

Absent references, removed active identities, non-drugs and duplicate rows receive one deterministic
repair. For duplicate drug rows retain the first previously effective row; never OR permissions or
sum inventory targets. Remove empty/invalid rows without deleting the policy itself. A removed row
saved away has no hidden restoration history: reintroduced content gets new-row defaults. Source
save claims do not establish arbitrary mod-removal safety or repair of physical items/health state.

## Contracts, settings and UI audit

ASCII searches across installed Workshop/local Mods C#/XML/DLL inputs for source package/type/owner
found source files and five YART LayoutHarness mod-list references, not a callable external consumer.
A separate UTF-16 assembly-string scan covered **5,485 DLLs** and matched only the seven source
version DLLs. These are bounded evidence, not proof that no consumer exists anywhere. Public
CorrectPolicies, SortPolicy/SortPolicies, helper methods and the field-ref alone do not justify a
compatibility facade. Preserve native policy behavior; do not spoof `Mlie.DrugPolicyFix`, its assembly,
Harmony owner or class identities. YART recorded lists are inventory, not an API contract.

There is no source bottom tab, toggle or settings window. It affects native drug-policy management
and adds log messages. Keep native entry points (including assignment and policy management),
copy/rename/default/delete controls and empty-state presentation. Policies and per-pawn assignments
are gameplay state, not mod configuration: **no Use global row for a drug policy**. Native default
policy is save-local gameplay state, distinct from global mod defaults. No new settings are proposed.
Any future actual configuration must use the foundation's common inheritance/rendering path.

No new map cache is needed: this feature consumes current definition identities, not physical stock.
Use the content catalog generation/lifecycle shared with owned filtering; never add a per-frame
Thing scan or duplicate Ctrl+F/hauling index. No performance improvement is claimed without measurement.

## Proposed corrections, boundaries and remaining questions

| ID | Question / work retained for later | Current draft decision |
| --- | --- | --- |
| DP-Q1 | Preference review of native new-row defaults and separating presentation from job priority | Existing policies gain addiction-if-addictive only, no automatic pleasure/schedule/inventory; fresh presets retain native valid choices; names/language never grant permission; alphabetical UI does not reorder jobs |
| DP-Q2 | Exact beta lifecycle, early preset filtering, malformed drug-component shapes and effective patch interactions | Guard each consumed semantic boundary; repair before the known lookup failure; no broad exception swallowing or claim to recover every malformed mod/save |
| DP-Q3 | Finalized S038/optional content-provider ordering, later catalog changes, actual consumers and save transitions | Direct owned composition, identity-safe reconciliation, no physical-stock predicate, original-provider suppression and no source API/package emulation |
| DP-Q4 | Runtime implementation, honest RED/GREEN tiers, localization, native action-to-result and balance proof | All tasks remain unchecked; source inspection is not 1.7 compatibility, multiplayer, performance or live acceptance |

Explicit omissions: legacy translated-name heuristic, alphabetical **job** priority, source per-load
no-op/sort chatter and inaccurate counters, compatibility aliases, automatic historical setting
recovery, physical-drug destruction, health/addiction treatment, general corrupted-save repair and
unverified multiplayer support. No import or external save-file rewrite is proposed; ordinary native
policy data persists only through the player's normal save operation.
