# While You're Up — useful hauling detours

## Identity and provenance

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`,1.7 beta.
Origin: replacement S125. Selected [While You're Up/顺路搬运](https://steamcommunity.com/sharedfiles/filedetails/?id=3832180338),
xiaopu, `patch1.7.whileyoureup`, installed1.7DLL112128bytes SHA256
`867B0C299677621C03888204FDD1045DBF64EBA257FBBBAA38F42C4647F2E1F2`.
Original [CodeOptimist source](https://github.com/CodeOptimist/rimworld-while-youre-up) and
[Workshop2034960453](https://steamcommunity.com/sharedfiles/filedetails/?id=2034960453) retain credit;
AGPL3.0-or-later NOTICE. No upstream code/art/DLL reuse. Inspected2026-11-06.
[Research](../refinement/2026-11-06-while-youre-up-research.md) records exact hashes, defaults,
filter package list, helpers, native paths and actual consumers.

## Behavior inventory and coverage

All rows are **specified for later implementation; nothing implemented or verified**.
[Owned change](../../../../openspec/changes/specify-megamod-opportunistic-hauling/),
[observable contract](../../../../openspec/changes/specify-megamod-opportunistic-hauling/specs/megamod-opportunistic-hauling/spec.md).

| Behavior | Input → result | Defaults / intentional difference |
| --- | --- | --- |
| General opportunity | Start eligible ordinary work → useful physical storage haul then parent | Humanlike native permissions; bleeding/forced/queued/loading/emergency protection |
| Search | Nearby/Balanced/Thorough → finite selected candidate or normal work | Balanced;30/.5 and50/.6 preferences; no zero-limit expansion loop or optimality guarantee |
| Trip limits | Plan all pickups/deliveries/return → accept only within one budget |170% total/100% added legs; actual grouped route, spent-budget replan |
| Construction supply | Selected resource and useful surplus → closer storage then work | On; actual route benefit and proposed whole-supply budget |
| Bill supply | Selected bill ingredient → same useful detour decision | On; one shared S024 preference/attempt, original material/radius/iteration retained |
| Equal priority | Work-bound supplies → different same-priority store | On; actual eligible supplies only, no global priority mutation; ordinary hauling still better storage |
| Destination filters | Separate stockpile/building choices → allowed supported destinations | Stockpiles/automatic on; common whole-collection scope, manual search/dormant choices |
| Batch composition | Owned opportunity → S021 pickup/capacity/grouped delivery | No source-private cargo store; no extra capacity; S126 actual visible cargo remains |
| Recovery | Lost destination/interruption/load → stop new cargo, retain/recover exact portions | S021 safe recovery/600-tick retry, stable parent lifetime; no per-Def substitution |
| Reports and controls | Inspect/toggle → understandable purpose and current-play state | Bottom-right operational toggle; settings in Hauling > Opportunities; no bottom tab |
| Diagnostics | Developer route view → color plus textual reasons | Top debug controls only, session state/off initially |

## Interactions and contracts

S021 is the quantity/capacity/storage commitment/delivery/recovery authority. S033 supplies shared
map facts; each pawn still passes current native eligibility. S049 refill and physical stack limits
apply to every automatic destination. S005 linked shelves/stockpiles share settings but no physical
space/travel. S014 committed bill material/radius/counts survive the detour. S024 and S125 call one
owned decision, with no internal Harmony stack. S133/S135 loading remains separately owned and
does not inherit opportunistic preemption. No source speed or compatibility guarantee is adopted.

| Concrete consumer | Inspected contract | Decision / gap |
| --- | --- | --- |
| Legacy PUAH / Continued | Reflected Comp/HashSet, workgiver methods, drivers, skipCells and named Defs | Preserve useful behavior directly; no false whole-stack facade over exact quantities |
| Reworked / Armful | Different implementation identities | Reference behavior only; selected port cannot infer its old bridge from these names |
| Common Sense | hauling_over_bills setting, source changes it while configuring | Owned single preference/decision; never edit foreign settings; original external overlap takes precedence |
| Smart Medicine Continued | WhileYoureUp.Utils.MyDistance historical hook | Target absent from selected port; no active modern binding claimed |
| Vehicle Map Framework | Older package checks plus midpoint-store method map adjustment | Exact multi-map adapter remains unverified; no package spoofing or coordinate-only shortcut |
| Source settings alias | JobsOfOpportunity.ModCompat.Settings and global save/filter keys | No automatic import or save-removal guarantee |

Active original opportunity providers suppress owned overlapping scheduling. Unsupported external
batch integration suppresses affected inventory routes, with safe single-load fallback only where
all rules still hold. Existing owned cargo remains recoverable. Compatible unrelated patches do
not disqualify the feature. Per-patch semantic target docs/executable startup checks/atomic groups
and warnings are required before implementation, not fulfilled by this source inventory.

## UI audit and omissions

Source has internal mod-settings pages, not a gameplay bottom tab. Retain useful configuration in
common global/save rows, make automatic building choices visibly read-only, fix functional list
search and preserve missing selections. Current-play enable belongs bottom right; diagnostic routes
belong top debug controls. Actual pawn/attempt/cargo choices are state, never global configuration.
The initial enable default is the documented global-only exception, seeding only games without an
operational value; an existing saved toggle is never overwritten by a later default change.

Deliberately omit port capacity multiplier1–10, foreign setting writes, source error-log suppression,
source support-channel notices, unguarded patch registration, old config auto-import, generic
reverse-patch filter clone and binary/package impersonation. Replace source five-rendered-frame
anti-loop logic and unbounded soft expansion with finite attempt semantics. Proposed supply surplus,
route budgets, all-purpose bleeding exclusion and automatic supported-storage policy are explicit
deviations. No complete external container/floor/vehicle/MP support is claimed.

Required game; Harmony when patches are implemented. Optional providers remain optional. No XML
mutation is planned; any actual XmlExtensions use requires its dependency and content/shape guards
in that slice. All runtime tasks, reviewed-build native acceptance, performance measurement and
seven-language UI verification remain open under WY-Q1–5.
