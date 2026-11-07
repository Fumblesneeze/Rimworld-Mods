# S130 — Dynamic Spacefarer Policy research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin replacement. Offline/source analysis on2026-11-06; [coverage](../replacements/dynamic-spacefarer-policy.md)
and [change](../../../../openspec/changes/specify-megamod-spacefarer-policy/) specify future work only.

## Author documentation and selected identity

Read the author's [Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=3829638797)
before decompilation. Web opener failed; direct retrieval succeeded, retained87,011-character HTML.
The description says vacuum-resistant gear, including modded gear, joins the Spacefarer policy;
existing saves work and manual exclusions persist. No source link appears on that author page or
installed metadata. Bounded exact-title/package/source searches found no public repository. Mirror
summaries were not used as technical evidence. No source/license file found locally; no universal
claim that none exists. Independently engineer; no upstream code/assets/DLL redistribution.

Installed root `F:/Steam/steamapps/workshop/content/294100/3829638797`, author **asunib**, package
**asunib.DynamicSpacefarerPolicy**, supported1.6/1.7, Odyssey required, no declared Harmony dependency
or modVersion. `loadFolders.xml` selects root+1.7. Complete selected DLL decompiled read-only;
assembly version0.0.0.0, only two behavior types. No patch, settings window or custom assets in code.

| Input | Bytes | SHA256 |
| --- | ---: | --- |
| About/About.xml | 689 | `877451bc51b3d6e872539060e95d2d035354665a099a4e63ee7c82344e0f68a5` |
| loadFolders.xml | 170 | `1b1b5599aad26471655f13e5b2c019905ef1780798fb8e4f488720b54d7d6423` |
| 1.7/Assemblies/DynamicSpacefarerPolicy.dll | 5632 | `47600f0044e67e3a40f46d3e0c7e5ef34dede774792c2eb90db4b27097954b28` |
| Selected game Assembly-CSharp.dll | 15636480 | `d55aa8e47fa3c5463ca85b0eccd51f195b4bbcd04f56da3af21c2f6688a81254` |

## Complete inspected source inventory

| Type / member | Actual behavior | Selected result / deviation |
| --- | --- | --- |
| `DynamicSpacefarerTags` static constructor | Odyssey and VacuumResistance stat guard; scans all ThingDefs once, requires apparel and positive resistance; appends missing Spacefarer default tag | Same type-level candidate meaning; directly update selected policy, no global runtime Def mutation |
| `HasVacuumResistance` / `Positive` | Any value>0 in equippedStatOffsets OR statBases for that StatDef; no sum, pawn protection, material/quality or item-state calculation | Preserve OR/positive criterion, reject nonfinite/malformed data explicitly |
| Public `Tag` / `Defs` | Constant Spacefarer and static List of qualifying ThingDefs | Internal feature data; no external consumer demonstrated; no speculative facade |
| `GameComponent_DynamicSpacefarerPolicy.ExposeData` | HashSet&lt;ThingDef&gt; added saved as dynamicSpacefarerAdded; creates missing set and removes null in PostLoadInit | Owned per-policy stable identity/history; retain inert missing-content identities |
| `FinalizeInit` target selection | If outfit DB and candidates exist, first policy whose label equals current OutfitSpacefarer translation | Fresh native origin capture; explicit adoption when existing identity is unknown |
| `FinalizeInit` update | For each candidate, only added.Add success calls filter.SetAllow(true) | No repeated direct addition for an already recorded candidate; linked-child side effects can still reverse an exclusion as described below |

The source is not an ongoing enforcement filter. It does not repeat the direct SetAllow call for
a candidate already in its saved set, even if the player subsequently excludes it. However, adding
a new parent can indirectly re-enable a recorded child through native linked-item propagation,
as described below. First installation into an old save has an empty set, so it **can override
pre-existing exclusions** for eligible types. The author's persistence promise is therefore limited
by both initial unknown intent and linked-child side effects. No type is removed when eligibility
later changes; no runtime polling.

**Control-flow risks, not live-reproduced bug reports:** renamed or differently translated policies
are not found; duplicate names select the first; global per-game history is not tied to the chosen
policy, so a newly created same-name policy can miss old additions; removed Def references are
pruned, allowing a returning excluded type to be re-added; null stat entries/nonfinite values are
not defended. Absence of a matching policy silently does nothing. No executable patch guard exists
because this source uses lifecycle callbacks instead of Harmony, but shape/lifecycle admission
still matters for our future native hooks and any provider integration.

## Native1.7 evidence

Fresh `OutfitDatabase`, `ApparelPolicy`, `Policy` and `Dialog_ManageApparelPolicies` were extracted
from the hashed game, version1.7.5000 rev1275. Native starting outfits are constructed in the DB
constructor. With Odyssey, GenerateStartingOutfits creates a Spacefarer policy, clears it, disallows
tainted apparel and allows tagged Spacefarer **or ApparelUtility** definitions. Other default
policies precede it, with optional Ideology changing the list; hardcoded index/ID is unsafe.

MakeNewOutfit uses current maximum id+1, allowing reuse after deleting the highest ID. Policy
serializes id/label, and GetUniqueLoadID combines LoadKey, label and id. There is no saved native
Spacefarer-origin marker. ApparelPolicy.CopyFrom copies filter allowances, not a distinct origin.
TryDelete rejects policies in use, clears remaining references and removes the policy. The native
management dialog supplies creation/default/deletion/list and normal ThingFilter UI. These facts
support native contextual enrollment and require preserving normal deletion/assignment rules.

Independent contract review also identified `ThingFilter.SetAllow(ThingDef,true)` recursively
allowing linked `virtualDefs`; a fresh fifth native type extraction confirms it. An apparently
independent parent addition can therefore reverse a considered or explicitly declined child choice.
Selected correction: compute/display the affected set, give existing exclusions/declines and
ineligible unrelated types precedence, hold conflicting roots before confirmation and apply only
the nonconflicting batch atomically. Successfully added eligible linked children enter history;
held roots do not unless explicitly declined. Reconsider held roots only on load/explicit review,
not per repaint. This is a documented strengthening of source behavior, not a global setter change.

Bounded native Odyssey XML inspection found apparel with directly declared positive vacuum
contributions in Apparel_Headgear.xml and Apparel_Various.xml; other occurrences include genes,
prosthetics and races, which are **not apparel candidates**. No balance/stat values are changed.
Static declarations do not prove finalized optional-provider values or actual total pawn protection.
S004 weather-ready arrivals remains the separately specified outfit-generation/protection authority.

## Selected composition, policy identity and adoption

Fresh games track the actual native-created policy before player edits. Owned history follows its
identity across label/language/list changes, not the translated title; copied policies do not inherit
tracking. There is one active target per game. Release retains a surviving policy's history; legal
deletion ends the binding. Preserve absent content identities so removal/re-add cannot reset a
manual exclusion. Native allowance restoration for removed *allowed* content is not promised.

For an old save lacking trustworthy owned identity/history, arbitrary automatic selection cannot
both identify the intended policy and preserve unknown prior exclusions. **Selected deviation:**
an explicit action in the native apparel-policy editor previews new eligible disallowed types,
initially selected; the player can decline entries, confirm or cancel. Confirmation adds only
selected entries, records all considered types and changes the active target atomically. Retain
backup snapshots, revalidate target/content, verify postconditions and restore any failure. Subsequent
new types on load are automatic; initial enrollment does not become a recurring approval prompt.

These are policy-specific gameplay choices, not mod settings or a global/save exception. No new
bottom tab/debug control. Owned labels and cautions use seven languages. S127 opens the same native
policy editor; S132 future gear display reads actual native apparel/filter data. No self-patching,
Thing-cache duplication, automatic equipping, crafting, spawning or pawn assignment. Utility and
all special/quality/condition filters stay native. No XML changes or hidden Def-tag rewriting.

Source history import is **not selected**. Its single global Def set cannot establish target
identity; copying the key is not migration proof. A future import needs a separate reviewed exact
save/input/target/rollback contract. Current old saves can explicitly enroll using their current
native filter, without altering original data files or pretending the old component was loaded.

## External contracts and original provider

A bounded installed DLL string search found only source1.6/1.7 assemblies referencing its namespace.
Source search for defaultOutfitTags found VEE's older ApparelPod reading Soldier/Worker tags and
costume/vanity code initializing empty tags; none consumed the newly added Spacefarer tag. These
bounded observations do not prove no private binary/tag consumer exists. The concrete consumer of
the tag demonstrated here is native starting-policy generation, covered by direct owned seeding.
No blanket source-type/tag/binary/drop-in promise or package impersonation.

Original `asunib.DynamicSpacefarerPolicy` co-installation retains its own behavior; owned overlap is
withheld with warning and inactive-control explanation, without trying to undo its static tag
mutation. Odyssey is required only for this module, optional for the product as a whole. Missing
DLC is ordinary inactivity; unexpected enabled target failure needs bounded diagnostic/fallback.
Corrupt identity leaves policies unchanged and offers re-enrollment; no guessed recovery mutation.

## Remaining evidence and questions

- **DSP-Q1 resolved:** complete source/native inventory and selected source-derived default behavior.
- **DSP-Q2:** explicit old-save enrollment deviation retained for later product review; any future
  source-history import requires a distinct backed-up, verified migration contract.
- **DSP-Q3:** effective beta fresh/load/serialization/UI/delete hooks, policy marker uniqueness,
  copied/id-reused objects and supported repatch/failed-group handling.
- **DSP-Q4:** actual finalized native/added apparel, conditional resistance limits, missing/returning
  providers and malformed data; no universal vacuum-protection claim.
- **DSP-Q5:** native policy edits, enrollment rollback, rename/language/save switching, actual normal
  clothing jobs, no-DLC/original-provider runs and final reviewed-build acceptance.

Ignored evidence `artifacts/Refinement/megamod-spec-20261106/dynamic-spacefarer-policy/` retains
author HTML, selected/native input hashes, complete compiled/native extracts, checks and review.
No code executed from the inspected assemblies. No runtime edits, gameplay tests, game launch,
source-mod writes or acceptance. Final semantic pass preserves stat/progression/cost/gear authority;
policy inclusion alone is explicitly not equivalent to a safe vacuum outfit.
