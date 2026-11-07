# Room Food

## Identity and provenance

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, target **1.7 beta**.
Origin replacement **S111/P2-40**. Source **Room Food [Updated for1.6]**, **TheLordDrake.RoomFood**,
[Workshop3530729319](https://steamcommunity.com/sharedfiles/filedetails/?id=3530729319), original by
Uuugggg. Inspected2026-11-05. Public author commit `feb77b45d98dc578462ba9fded8a157db41a5fd8`.
Selected Room_Food.dll is 9,216 bytes, version 1.0.0.0; SHA-256
`91175F0AA1A7BF3EE612FDB7004546EAFB45B27F1BBBE37A196ADDB45E58EBEB`.

[Research and complete inventory](../refinement/2026-11-05-room-food-research.md) retain public docs,
full source/binary reading, native1.7.5000 contracts, exact identities and bounded consumer results.
No license file found in inspected installed/public trees; independently implement the behavior,
retain attribution, and do not copy source code, binary or art. Source1.6 support is not beta acceptance.

## Behavior inventory

All entries below are **specified drafts**, with **nothing implemented** or accepted in game.
[Observable capability](../../../../openspec/changes/specify-megamod-room-food/specs/megamod-room-food/spec.md).

| Behavior | Native input → intended observable result | Defaults / edge cases | Contract / evidence |
| --- | --- | --- | --- |
| Scope | Ordinary colonist food request → room preference; feeding another humanlike → recipient room | Free-colonist getter; no new job eligibility, explicit-item substitution, animal/caravan/refill behavior | RF01; FoodFinder/native caller inspection |
| Dining | Normal map search → eligible food in nearest qualifying room | Whole enclosed room, safe permitted table, skip unusable rooms, fallback unchanged | RF02; source intent plus documented corrections |
| Care | Patient/warden job → eligible food in recipient's room | No table/hospital-role requirement; eater policy and getter access retained | RF03; both native care callers |
| Permissions | Preferred source → ordinary access and consumption rules | Candidate/radius/path, reservations, forbidden, actual dispenser stock/power/refill/serving side | RF04; source/native comparison |
| Composition | Food comparison → room priority with one set of within-room scores | S024/S057 once; native inventory/harvest; S055 only after failure; physical food unchanged | RF05; linked owned contracts |
| Distance | Edit common Food settings → next self-feeding table search |100 default,1–200 inclusive; one-time extension to already selected farther map food; recipient branch unaffected | RF06; source settings plus shared scope |
| Current state | Change room/food, switch save → current selection and ordinary recovery | No persisted winner/new job, no query-time mutation or tight retries | RF07 |
| Compatibility | Unavailable prerequisite/source overlap → ordinary fallback with bounded warning | Compatible other patches allowed; optional absence quiet; no identity spoof | RF08 |

## Interactions and composed result

Directly compose [Common Sense](common-sense.md), [preserved-food preference](preserved-food-preference.md)
and [shared carried food](meals-on-wheels.md). Room priority precedes food scoring across rooms;
the existing contributions rank food inside the chosen room. Thus eligible preserved food beside a
table can beat a fresh meal outside, while personal inventory can still win the native outer choice.
Borrowing never displaces successful ordinary selection. Source hooks disappear into direct owned
selection, with no self-patching, private RoomFood cache or new physical inventory/capacity owner.

RimFridge is a suggestion, not a dependency. The author's untested paste-network reply is not a
compatibility claim. Native exposed food and dispensers retain their normal workflow; arbitrary
foreign containers, remote delivery and other providers require concrete research/admission.

## UI audit and relocation

| Source entry point | Purpose | Owned destination / impact |
| --- | --- | --- |
| Room Food settings, hardcoded-English distance slider | Global configuration | Common Food settings with Global default, Current save and Use global; seven languages |
| Automatic food selection | Ordinary mechanic | Existing eating/care jobs, no separate tab/window/gizmo/toggle |
| Icon / preview | Branding | No selected/copied art; not necessary for this behavior |

New/missing save entries inherit the current global setting. The save editor is inactive while
inheriting; all save controls/callbacks are inactive at the main menu. Dormant overrides survive
Use global. Valid edits apply next selection; invalid persisted values recover to100 with a bounded
diagnostic. No source config import or inheritance exception. The distance help explains expansion.

## Contracts consumed by mods or saves

| Inspected consumer / surface | Actual binding | Owned policy |
| --- | --- | --- |
| Installed Workshop database / replacement catalogue / four layout test mod lists | Source package IDs and replacement mapping | Metadata only; no source ID spoof or claimed runtime facade compatibility |
| Public FoodFinder.FindRoomFood, Mod.settings, Settings.maxDistanceToTable | Exposed source members; no actual runtime caller found by bounded installed search | No speculative assembly/type facade; inspect concrete consumers if discovered |
| Source global Scribe key maxDistanceToTable | Source configuration only | No automatic import, writes or claimed migration; use own common setting |
| Original/continued/dev provider active | Duplicate food-choice patch ownership | Disable owned overlap with warning, preserve other provider's code/settings |

No source runtime saved component/job was found. Owned addition saves common settings only and
does not create special food, room assignments or custom jobs. Removal leaves native food/jobs
usable. Any future import requires a separately specified transactional migration.

## Implementation and coverage

| Portion | Implemented | Intentional deviation / possible conflict | Missing or omitted |
| --- | --- | --- | --- |
| Room priority | Nothing | Whole native room/enclosure replaces one district and >60-region cutoff; skip unusable nearer rooms | Runtime and measured costs pending |
| Food safety | Nothing | Preserve caller domain/path/radius; supported dispenser serving-side location | Effective beta hooks/adapters and native acceptance pending |
| Scope and scoring | Nothing | Explicit ordinary-only context and direct owned composition; native inventory preserved | Combined source/provider workflows pending |
| Settings | Nothing | Shared global/save inheritance and localized help replace source-global English slider | UI/runtime acceptance pending |
| Source infrastructure | Nothing | No PatchAll, global debug switch, package spoof, unsolicited import or copied art | Deliberately omitted |

Required Harmony with actual implementation; no bundled assembly or hard source/DLC/provider/Gateway
dependency. No XML selected; later XML use must have its own target/content guards and merge policy.
Design contains candidate semantic target obligations, not implemented validators.

## Verification and acceptance

Specification-only draft. Independent review and strict validation outcome are tracked in the
[ledger](../refinement/specification-progress.md). **RF-Q1–3** retain implementation research,
composition measurements and native verification. All implementation tasks remain unchecked.
Future acceptance requires actual colonist eating and caregiver feeding with personally inspected
causal results on the reviewed build, changed source/room/access cases, settings/languages and exact
admitted provider lists. Static method inspection does not prove a job, beta compatibility or speedup.
