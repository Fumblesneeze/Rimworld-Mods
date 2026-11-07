# S091 — Keep Burning, Corpse! research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: replacement. Inspected 2026-11-05. Public docs first, full source/binary and targeted native
read-only decompilation afterward. No game launch or implementation acceptance.

## Identity and provenance

| Input | Inspected identity |
| --- | --- |
| Original | Tixiv, Keep Burning, Corpse!, `tixiv.KeepBurningCorpse` / canonical `tixiv.keepburningcorpse`, Workshop 3717835691 |
| Installed root | `F:/Steam/steamapps/workshop/content/294100/3717835691`; About 1.6, Harmony required, load after Core/Harmony |
| Selected DLL | `1.6/Assemblies/KeepBurningCorpse.dll`, assembly/file 1.0.0.0, informational 1.0.0, Debug configuration |
| DLL SHA256 | `46C742437983EAD45BB2C28520CC6565E448E23654CC58FB7DD5C6162FD79588` |
| About SHA256 | `7CFB38A7D88BACF5794A44912B8766BB2BE8CFE0C264B44CD02F995C7E66B8DE` |
| Public source | [tixiv/KeepBurningCorpse](https://github.com/tixiv/KeepBurningCorpse), commit `a5e6ca5bfc5508755438abb92c84ade64a7b9120` |
| Build reference | net48, Krafs.Rimworld.Ref 1.6.4759-beta, Harmony 2.4.2; post-build copy script read but not executed |
| Native | 1.7.4998 rev1299, root Assembly-CSharp.dll SHA256 `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |
| Local ignored evidence | `artifacts/Refinement/megamod-spec-20261105/keep-burning-corpse/{upstream,binary,native}` |

No LICENSE/COPYING file found in the bounded repository filename scan. No affirmative reuse grant
is assumed. Independently specify behavior and retain attribution; do not copy/ship the source or
DLL. Version/hash pins identify inspected evidence, not runtime compatibility predicates.

## Public behavior and complete source inventory

The [Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=3717835691) and
author README describe retaining fire after a burning pawn dies. The author attributes the loss to
a late check after fire removal and mentions shamblers/Death Pall as motivation. This does not
promise a new permanent fire or independent anti-resurrection system.

The source and selected binary agree on one Mod class and these members:

| Member | Actual behavior |
| --- | --- |
| Constructor | Owner `tixiv.KeepBurningCorpse`; explicitly patches Pawn.Kill with prefix/transpiler/postfix, then PatchAll; no additional attributed patches present |
| Public static fire_tmp | One process-global Fire reference |
| Pawn_Kill_Prefix | Captures the pawn's ordinary Fire attachment at Kill entry |
| MyGetAttachment | For ThingDefOf.Fire, returns and clears fire_tmp; for other Defs, delegates to ordinary attachment lookup |
| Pawn_Kill_Transpiler | Replaces every call to AttachmentUtility.GetAttachment with MyGetAttachment; counts matches and warns if count 0 or not 1, but does not revert emitted changes |
| Pawn_Kill_Postfix | Clears fire_tmp on normal completion; no finalizer/scoped invocation state |

No source settings, gameplay Defs/XML, UI/tab, new assets, saved keys or cache. The only changed
behavior is the existing death-fire lookup. Replacing instructions constructs fresh instructions
without an explicit transfer of labels/exception blocks. Static-state and broad-match risks are
inspection deductions: nested deaths can overwrite/clear another invocation's Fire, an early
capture can ignore legitimate later extinguishing, and a mismatch can still partially transform
the method. They are not reproduced live failures in this documentation turn.

## Exact current native chain

Retained decompilation includes Pawn, Corpse, Thing/ThingOwner/ThingOwnerUtility, CompAttachBase,
PawnComponentsUtility, FireUtility and Fire. The relevant sequence is:

1. Pawn.Kill runs ordinary death effects/notifications and despawn logic, then creates a corpse where
   the native death outcome permits one. Its on-map path calls MakeCorpse and attempts Direct then
   Near placement. Container/caravan/world/no-corpse/failure paths are distinct.
2. MakeCorpse assigns Corpse.InnerPawn. That setter calls innerContainer.TryAdd(pawn), which invokes
   ThingOwner.NotifyAdded. ShouldAutoExtinguishInnerThings is true for a non-Map holder; NotifyAdded
   destroys an attached Fire. This is the actual loss boundary. The inspected DeSpawn and component
   removal code do not establish an earlier mandatory attachment deletion.
3. After successful on-map corpse placement, Kill queries the pawn's Fire attachment, then would
   call TryStartFireIn at the corpse's actual Position/Map using CurrentSize and fire.instigator.
   The earlier holder transition explains why the lookup is ordinarily too late.
4. Fire.CurrentSize returns fireSize. TryStartFireIn requires a positive ordinary cell eligibility
   value, creates a normal static Fire and passes size/instigator. It does not roll another chance.
   Any Fire already at the cell causes refusal; no doubling or transfer into an existing flame.
5. Cell eligibility considers flammable terrain/items, blocking filth and impassable-edifice
   interior. Fire's later calculations handle extinguishing terrain, rain, vacuum and available
   fuel. A nonflammable floor alone does not prove the corpse cannot sustain fire, and a fire that
   appears can subsequently go out under normal environmental rules.
6. Fire owns normal size/timer/instigator persistence, heat/spread/damage and extinguishing.
   CanEverAttachFire is pawn-only; a corpse is not given an attached fire by this native route.
   Moving the corpse does not move the cell fire. Resurrection retains ordinary interaction with it.

This is strong static explanation of the intended repair, not evidence that the future owned patch
will work or that the source DLL is compatible with the inspected beta.

## Owned specification decisions

- Retain one normal fire attempt only for the successful on-map corpse placement and reuse its
  actual location, prior flame strength and original fire instigator. Do not substitute the fatal
  damage source, add damage, copy a fixed size or guarantee complete corpse disposal.
- Capture immediately before actual containment removes the fire; preserve legitimate earlier
  extinguishing. Use invocation-local scalar payload, no shared Fire slot or resurrected object.
  Nested/repeated/skipped/failed paths cannot leave stale state for another death.
- Reject non-finite/non-positive input size with a bounded diagnostic; no fabricated replacement.
  Native existing-fire refusal, no/failed/held/off-map corpse paths and later fire lifecycle remain.
- Require minimal executable capture/use targets and actual incoming-stream validation; tolerate
  benign edits/other owners, preserve instruction control metadata and return unchanged on failure.
  Detect already-fixed behavior and stand down. Original source presence suppresses owned overlap.
- S079 can stop/prevent a living pawn's fire. This feature does not revive it or run apparel ignition
  resistance again for a corpse cell. Compose owned behavior directly; no self-patch stack.
- No feature setting or UI: a continuity correction has no new numeric parameter. Status strings
  use common localized presentation. No save import, permanent fire state or map-wide scanner.

## Contracts, dependencies and bounded consumer search

Installed `.cs/.xml/.json/.csproj` search for the package, helper and temporary field found only the
original About metadata. No actual executable source-API consumer is established by that bounded
search; public methods/field are not a reason to impersonate the original namespace/package.

Native Pawn.Kill/corpse placement, holder extinguishing and Fire/instigator handling are the consumed
contracts. External death/fire providers need exact effective shape validation. Core and Harmony
required when implemented; no DLC required. Anomaly optional for additional shambler/resurrection
cases, not a runtime adapter inferred from the mod description. No XML currently used; any later
declarative operation requires XML Extensions dependency/guards/merge policy. No foreign/private
Harmony/Gateway shipment or another repository-owned runtime mod dependency.

## Static balance and playability pass

| Comparator | Native anchor | Owned result |
| --- | --- | --- |
| Living pawn's attached fire | CurrentSize/instigator; normal damage and extinguishing | Preserve remaining size/origin, no additional ignition resistance or direct damage |
| Newly placed corpse's static cell fire | Native death branch and TryStartFireIn | Restore existing intended attempt once; corpse flammability/environment govern subsequent burning |
| Existing ordinary fire at that cell | ChanceToStartFireIn rejects any existing Fire | Leave existing size/origin alone; no second flame or free intensity increase |

No work, cost, spawn-rate, damage or growth constant is introduced. Continuing fire increases corpse
fire exposure relative to the broken path, and can affect nearby fuel or corpse survival. Those are
normal fire consequences, not a promised anti-shambler mechanic. Finalized corpse/terrain/provider
values and native suppression/resurrection playability remain live acceptance work. Exact deaths
must follow real player combat/fire actions; spawning a burning corpse is not causal evidence.

## Open obligations

| ID | Required follow-up |
| --- | --- |
| KBC-Q1 | Exact current effective loss/capture/use branch, instruction control metadata, minimal semantic predicate and per-invocation/exception lifecycle. |
| KBC-Q2 | Original/compatible/changed/already-fixed provider policy; direct S079 composition, distinct instigators and optional Anomaly cases without duplicate handoff. |
| KBC-Q3 | Focused TDD, reviewed package/localization, finalized fire/corpse balance and personally observed native burning-death/extinguish/placement/held/save/provider workflows. |

[Proposal](../../../../openspec/changes/specify-megamod-corpse-fire-continuity/proposal.md),
[design](../../../../openspec/changes/specify-megamod-corpse-fire-continuity/design.md),
[spec](../../../../openspec/changes/specify-megamod-corpse-fire-continuity/specs/megamod-corpse-fire-continuity/spec.md),
[tasks](../../../../openspec/changes/specify-megamod-corpse-fire-continuity/tasks.md),
[coverage](../replacements/corpse-fire-continuity.md). Document review/validation status is recorded
in the progress ledger; all runtime tasks remain open.
