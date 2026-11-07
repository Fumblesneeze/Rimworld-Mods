# Prosthetic apparel: sequential refinement and source evidence

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Queue **S025 / P1-19**, origin replacement. Inspected **2026-11-02**, following S024.
[Owning change](../../../../openspec/changes/specify-megamod-prosthetic-apparel/) is specification only.

## Documentation and pinned inputs

Read the [Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=2739055353)
first, then the continued author's [repository](https://github.com/emipa606/ProstheticNoMissingBodyParts).
The page describes an apparel-only remedy, adjustable replacements, a switch from prefix to postfix,
and Android support. Its broad compatibility/safe-removal assertions are source claims, not our proof.
Comments about modular replacements led to inspecting the installed consumer; they are not evidence
that its current version is broken. The source name does not mean missing anatomy is restored.

| Input | Pinned identity |
| --- | --- |
| Source | Prosthetic No Missing Body Parts (Continued), original iamrespawn/RimworldFixes, continued by Mlie; `Mlie.ProstheticNoMissingBodyParts`, Workshop 2739055353 |
| Installed root | `F:/Steam/steamapps/workshop/content/294100/2739055353` |
| Installed About SHA-256 | `7882F4E306BEA742BC9846EC0C06A7EDDB2254D16D797B99BA29EDCD2ACA1B82` |
| Declared support/dependency | 1.1–1.7; Harmony required; load-after Harmony and `syrchalis.prosthetictable` |
| Installed assembly | `1.7/Assemblies/ProstheticNoMissingBodyParts.dll`; file 1.7.0; product `1.0.0+9d76d22cc864d045d4bd1058bbd5f44fd4f37d90` |
| DLL SHA-256 | `ED964FE29D382645BBE6BA32ADBA979721D2C5FB5D146BA1FA57944CC6A6EEFE` |
| Public checkout | `417551f191fa4f2d0576921437ba1c5f8a22b4ae`; shipped 1.7 assembly byte-matches installed DLL |
| Metadata correspondence | About and LICENSE differ only in line endings; Manifest is byte-identical. Matching committed binary is not a reproducible-build claim |
| License | MIT, copyright 2020 RimworldFixes, installed/public LICENSE.md; retain notice if substantial licensed material is ever reused. This feature is independently specified and no code/assets are copied |
| Native inspection | Typed installation inspection: 1.7.4995 rev309; Assembly-CSharp SHA-256 `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21` |

Ignored artifact root: `artifacts/Refinement/megamod-spec-20261102/prosthetic-apparel`. It contains the
checkout, complete installed decompilation, selected native types, consumer decompilation and bounded
search path lists. All five source behavior/settings files and their installed counterparts were
inspected. Runtime source inventory is one Harmony patch, helpers, bootstrap, Mod and ModSettings;
there are no source gameplay Defs/PatchOperations to port. VersionFromManifest supplies UI version
text, not a gameplay requirement for our implementation.

## Actual source behavior

- `ApparelUtility_HasPartsToWear.Postfix(ref bool,Pawn,ThingDef)` preserves true and investigates only
  false. It rebuilds four string HashSets from global settings on every failed query, then scans
  apparel groups and installed hediffs. It does not change health, body trees or apparel coverage.
- Groups `LeftHand`, `RightHand`, `Hands`, `Feet` have explicit routes. Otherwise the source checks
  whether the requested group occurs anywhere under a native Shoulder or Leg in that race's body.
  It **breaks on the first recognized group**, even if a later group has the usable replacement.
  The native method instead accepts **any** group containing a not-missing part. Owned evaluation
  must preserve that union, not require every group or preserve order-dependent failure.
- Left/right routes search the installed replacement's own subtree with `BodyPartUtils.ExistsDeep`;
  they also unnecessarily require `customLabel != null`. Generic hands/feet routes only check the
  replacement's attachment kind and enabled name; the arbitrary-group fallback can therefore use
  a different branch from the one that actually owns that group. Proposed correction uses actual
  anatomical branch/group identity without translated/custom labels or cross-limb substitution.
- Source attachment names: native Shoulder/Hand/Leg/Foot, `LeftMechanical*`, `RightMechanical*`, and
  `ATR_Mechanical*` equivalents. The generic fallback still hardcodes Shoulder/Leg. These are evidence
  of intended Android support, not proof of every installed race/provider. Actual equivalent body
  topology and replacement capability must be admitted; a display name alone is insufficient.
- The source scans **any** matching hediff name, not only actual added parts. Its recipe catalog
  accepts every addsHediff with a matching appliedOnFixedBodyParts, including implants that do not
  replace anatomy. In inspected EPOE Forked XML, MuscleStimulatorLegs is a Hediff_Implant. Treating
  that alone as restoration of a lost foot is an overbroad exception. Proposed owned correction
  requires actual replacement coverage or an explicit equivalent provider contract, regardless of
  an enabled preference; it does not heal a severed part or give an incidental implant missing digits.
- One bootstrap owner `com.prostheticnomissingbodyparts.rimworld.mod` unconditionally PatchAlls.
  Owned semantic admission replaces that path; no source owner/package spoof or pristine-method gate.

## Settings, defaults and UI

- Four global lists: `ArmsWhitelist`, `HandsWhitelist`, `LegsWhitelist`, `FeetWhitelist`, saved as
  `armsWhitelist`, `handsWhitelist`, `legsWhitelist`, `feetWhitelist`. No per-save state/importer.
- Arm defaults: SimpleProstheticArm, BionicArm, AdvancedBionicArm, ArchotechArm. Leg defaults:
  SimpleProstheticLeg, BionicLeg, AdvancedBionicLeg, ArchotechLeg, MuscleStimulatorLegs. Hands empty.
  **Feet erroneously start with the same leg names**. They usually do nothing at actual Foot sites;
  proposed foot defaults are empty, with distinct foot replacements opt-in rather than copied leg IDs.
  Source-derived leg preferences remain recorded, but an ordinary nonreplacement stimulator cannot
  grant unsupported coverage. New/unlisted replacements default off; no efficiency/cost rebalance.
- `init()` runs only when settings first render, scans all RecipeDefs with addsHediff and fixed sites,
  and appends labels for each recipe; duplicate recipes duplicate visible rows for the same decision.
  Hand/foot recipes work; recipes that replace an existing implant without fixed sites are missed.
- Search is case-insensitive substring matching over label, Def name and originating mod name.
  Sections order Arms, Hands, Legs, Feet; scroll list with description tooltips and installed version.
  There is no bottom tab, gameplay gizmo or development cheat workflow in this source.
- Checkboxes edit per-session dictionaries; WriteSettings clears/rebuilds four lists from those maps.
  Saved unknown IDs vanish when their content isn't represented; a write before initialization could
  clear all lists. Missing saved lists become empty. Owned settings must initialize independently of
  rendering, deduplicate, preserve absent choices and use common per-row global/save inheritance.
  An explicit empty selection means no replacement exception, not a reset to defaults.

## Native beta contracts inspected

- Full `RimWorld.ApparelUtility` inspected: HasPartsToWear checks GetNotMissingParts and returns true
  on the first apparel group with an existing member; false only when none exist. CanWearTogether
  independently uses layers and interfering groups. Do not replace this with an all-groups test,
  pair requirement, forced layering or added health parts.
- `RimWorld.Pawn_ApparelTracker.Wear` checks body availability, biocoding, PawnCanWear and layer
  conflicts before adding real apparel. `Notify_LostBodyPart` rechecks every worn item and removes
  those failing availability. The owned query must serve both actual wear and retention, not only
  the float-menu presentation. No synthetic re-equipping or direct inventory creation is selected.
- Additional lifecycle review inspected full `RimWorld.JobDriver_Wear`: it waits through equip time,
  removes conflicting apparel during that wait, detaches an IApparelSource target at completion,
  then calls Wear and records forced status without checking success. Wear itself despawns before
  its body-availability rejection. A stale job that is merely rejected at that last query can lose
  its physical target or create a false forced reference. The owned correction specifies stopping
  before further removal/transfer, retaining the real object, safe recovery for already-transferred
  targets, no new forced entry on rejection and no repeated equip loop.
- `Verse.HediffSet.AddDirect` adds the hediff before PostAdd, updates missing-part state/cache, and
  notifies apparel when a missing-part change occurs. HasDirectlyAddedPartFor uses Hediff_AddedPart;
  GetNotMissingParts enumerates body records whose PartIsMissing is false. Selected methods were
  inspected, not all possible surgery/provider paths. Exact successful surgery, failed surgery,
  modular conversion, removal and save transitions still require native workflow verification.
- Core Humanlike body XML places LeftHand/RightHand groups on finger descendants, not just the hand
  record. Recursive descendants matter; side cannot be inferred from a translated label. Native arms
  and legs have distinct branches. Prosthetic, bionic and archotech recipe research/costs remain native.

## Actual consumers

The bounded identity scan found **37 paths**, including source versions/localization, management
metadata and two executable consumer families. Member-name scan found the same executable families.
This is not proof against dynamically constructed names.

| Consumer | Binding and player-visible consequence | Owned disposition |
| --- | --- | --- |
| Integrated Implants `lts.I`,3223443793 | loadFolders includes its PNMBP DLL only when `Mlie.ProstheticNoMissingBodyParts` is active. Directly references Mod type and patches private init plus four private catalog fields; under EBSG active and MSE2 absent it inserts modular bionic/archotech arm/leg rows default true | Source private patch cannot bind without package spoofing. Register the four proven equivalent replacements through an owned optional adapter and common settings, independent of opening settings; never run a source patch against ourselves |
| Choose Your Outfit `oels.chooseyouroutfit`,3231731155 | Canonical source-package gate; reflects private static mod/instance settings and four public List<string> fields. Constructor snapshots union; GetExistPartsAndButtons accepts missing child/grandchild if the ancestor's hediff is in that union. ListingApparelToShow separately calls native HasPartsToWear | Exact package/reflected fields not emulated. Narrow optional display adapter should use current effective owned coverage for selected-pawn nodes and native actual availability; refresh on pawn/settings/health changes, without globally restoring anatomy |
| [FSF] Vanilla Bionics Expansion,1419675146 | About has loadBefore `com.prostheticnomissingbodyparts.rimworld.mod` (source Harmony/old identity string) | Ordering metadata, not a callable contract. Discover finalized definitions instead of relying on this hint |
| RimPy/community rules, replacements database and LayoutHarness mod lists | Package identity/index/list mentions | No gameplay API consumer found in these hits; no identity spoof |

Integrated adapter DLL SHA-256: `51E997EE9789BCD982B107FA6AD23446DB42573A84B8FDD0D6E4987A95C981C8`.
Choose Your Outfit 1.7 DLL SHA-256:
`5E1AE06A0909C72EEE5978940DE4F5CE6616E7376A933FF5C02D30B3AE5DF17D`.
Inspected installed source and decompiled Integrated adapter agree: four injected IDs are
LTS_ModularBionicArm, LTS_ModularArchotechArm, LTS_ModularBionicLeg, LTS_ModularArchotechLeg.
They come from `1.7/Mods/MedicalSystemExpansion2Absent/Defs/HediffDefs/Hediffs_EBSGModularParts.xml`,
require EBSG, and derive from LTS_ModularBodyPartBase using EBSGFramework.Hediff_ModularAddedPart.
Their conversion recipes replace an existing hediff rather than declare fixed body sites. Preserve
the conversion's actual attached branch; do not infer every arbitrary modular implant provides hands.
MSE2 present is a distinct unproven anatomy route and receives no inferred four-ID exception.
Installed EBSG Framework (`EBSG.Framework`,3112549163) 1.7 Hediff_ModularAddedPart was decompiled and
does derive from Hediff_AddedPart. DLL SHA-256:
`738C3E6D03B88AF21328C0A85CB95305BA0A8C91D04E8E022C988A0FA96FCE31`.
The derivation supports the candidate mapping; it does not prove finalized modular state or surgery.

## Proposed corrections and retained work

All replacements still require ordinary installed anatomy/provider semantics and actual garments.
There is no new apparel, medicine, recipe, research, body functionality, armor multiplier or free
surgery. Native single-member sufficiency is intentional; the feature does not require two limbs
for a garment when the native game accepts one. Optional UI must not imply regenerated digits.
Shared finalized definition facts may be reused; a per-pawn clothing decision is current state,
not another Thing index or a persistent always-true result cache.

| ID | Kind | Retained work without interrupting the specification pass |
| --- | --- | --- |
| PA-Q1 | Proposed behavioral corrections | Any-group/actual-branch coverage, nonreplacement exclusion, empty foot defaults and exact effect timing; verify resulting apparel availability with representative providers |
| PA-Q2 | Native lifecycle / admission | Effective HasPartsToWear semantics and vetoes, actual successful/failed surgery/removal/retention, worn-item handling and game/save transitions |
| PA-Q3 | Provider / consumed interfaces | Finalized Android/alternate-body and modular coverage, exact Choose Your Outfit refresh seam, absent/changed providers and original source co-installation; no blanket drop-in claim |
| PA-Q4 | Implementation / acceptance | Individual targets, common settings/catalog persistence, focused RED/GREEN, seven languages, review and native clothing/operation workflows |

No game launched, no implementation or source-save migration performed, and no provider compatibility
accepted. Source license, static inspection and strict OpenSpec validation cannot substitute for the
reviewed-build native workflow gate.

## Independent specification review

Two read-only reviewers examined native/source behavior and provider/settings contracts. The
contract/provenance pass found no P1/P2 issues. The behavior pass found one P2: queued-action rejection
needed explicit physical target preservation and success-dependent forced status. The spec now
includes ground and apparel-source cases, settings/health changes during work and safe recovery of
an already-transferred item. Design and tasks include the admitted continuation/transfer boundary.
Focused resolution review found no remaining issues. The capability now has 9 requirements and
28 scenarios, with all 24 implementation/acceptance tasks unchecked. No runtime changes, build or
game launch occurred.
