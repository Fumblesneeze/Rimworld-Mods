# S027 — Just Put It Over There research

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Inspected **2026-11-02**, following completed S026. Origin: replacement, **P1-21**. Refinement
evidence only: source/code deductions are not observed live gameplay.

## Documentation first and pinned inputs

[Steam description](https://steamcommunity.com/sharedfiles/filedetails/?id=2856471776), installed
README/About and [public repository](https://github.com/emipa606/JustPutItOverThere) describe
caravan purchases and guest gifts being taken to reachable storage, with ground fallback and no
trade delivery while the caravan is leaving. Hospitality gifts are included. The
[changelog](https://steamcommunity.com/sharedfiles/filedetails/changelog/2856471776) records custom
haul logic to avoid Pick Up And Haul overrides, fewer-carrier handling, main-trader exclusion to keep
animals out of the base, pawn-trade fixes and storage-building fixes. The fetched web changelog
lagged the installed 1.7.0 entry; installed Changelog and pinned commit establish that current build.
The author's autotest/safe-add/safe-remove claims are provenance, not our acceptance evidence.

| Input | Identity |
| --- | --- |
| Source | Just Put It Over There, Mlie; idea credit ʟᴇɢɪᴏɴ; package `Mlie.JustPutItOverThere`; Workshop `2856471776` |
| Installed root | `F:/Steam/steamapps/workshop/content/294100/2856471776` |
| About SHA-256 | `DC4A28FF0404F18AA4C406693734C252997664E7992C868F9F76813ABE59412E` |
| Installed DLL | `1.7/Assemblies/JustPutItOverThere.dll`; SHA-256 `F0526527A6EC6F4A7F8B287C84397936C22C5CC9EA45A0308BEDC8F6A7B27032` |
| Assembly versions | Assembly `1.7.0.0`, file `1.7.0`, informational `1.0.0+f2474818ef02d3c4e99093c0954f3846a9497d77` |
| Public checkout | Commit `c38c20aedf8c8b87b8996fbf21226088a6a954aa`, 2026-10-21; committed 1.7 DLL matches installed hash |
| Support/dependency | Source declares 1.3–1.7; Harmony required/load-after |
| License | MIT, LICENSE.md copyright 2020 Mlie; installed assembly copyright 2026 Mlie. Installed/public license hashes match; About differs only by normalized newline representation |
| Native input | Typed inspection `1.7.4995 rev309`, `F:/Steam/steamapps/common/RimWorld`, net10.0; Assembly-CSharp SHA-256 `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21` |
| Optional provider | Hospitality (Continued), Zaljerem, Workshop `3509486825`, package `Orion.Hospitality`, About version `1.1.8`; original author Orion |
| Hospitality 1.7 DLL | SHA-256 `6C169FED1229D1EE18B478AABFC7CBF23F92DC401A641A33C938E8055B345D56` |

Ignored local evidence: `artifacts/Refinement/megamod-spec-20261102/just-put-it-over-there/` contains
upstream checkout, installed decompilation, selected native and Hospitality types, source-identity
and native-boundary search inventories. Read all three source classes and matching installed classes,
assembly metadata and project. The public project references 1.7.4984-unstable and has a build/clean
task killing RimWorld processes: it was **not executed**. Source is read-only research, not our build
input. The upstream AI contribution policy concerns submissions to that project; no upstream PR or
submission is made here. Applicable MIT notices accompany any later substantial licensed reuse.

## Complete source inventory

No Defs/Patches, custom JobDefs, settings class, translations, bottom tab or gizmo. Two Harmony
patches plus one utility/static initializer implement all source behavior.

| Surface | Installed behavior |
| --- | --- |
| JustPutItOverThere static constructor | PatchAll under source package-like owner; no per-target admission |
| GetHaulJob(Pawn,Thing) | Reads current storage priority, calls TryFindBestBetterStorageFor using player-owned destinations; slot groups get HaulToCellStorageJob with fitInStoreCell=false; interactable ThingOwner storage gets HaulToContainerJob. No destination returns null; unknown kind logs an error |
| LogMessage | Development-mode messages only; not a player configuration/control |
| GiveSoldThingToPlayer prefix | Pawn purchases and non-Pawn_InventoryTracker holders go through original. Otherwise selects original inventory owner, or another caravan member if owner is animal; splits exact count, calls PreTraded, places near origin and records extraForbiddenThings, then tries haul and suppresses original |
| Alternate carrier selection | Only animal-origin branch checks leaving via a type-name substring. Searches same lord, non-animal, manipulation-capable, current job name not containing haul, not Trader role, closest to original carrier. Chooses only first, without trying another if later reachability fails |
| Direct non-animal inventory owner | Bypasses alternate-carrier leaving/main-trader/manipulation/busy filters. This contradicts the broad wording of documented departure/main-trader intent |
| Trade ordered job | Reach/reserve check, utility job, TryTakeOrderedJob with UnspecifiedLordDuty; no explicit queueing request and no check of acceptance result |
| GiveGift postfix | After native GiveGift, loops gifts. Chooses closest non-animal manipulating possibleGiver whose current job name lacks haul. Returns from whole loop when no candidate; skips that gift if chosen pawn cannot reach or no storage. Calls TryTakeOrderedJob with requestQueueing=true |

Source risks inferred from code: null lord/current job references, unknown receipt after placement
merges/splits, ignored placement success, job replacement/queue accumulation, picking one unreachable
courier without trying the next, and no explicit post-assignment ownership or save recovery. These
are not claimed reproduced bugs. Source custom job construction avoids one generic haul entry point
but does not prove immunity from all other patches on job drivers/toils.

## Native 1.7 observations from read-only code

- `Pawn_TraderTracker.GiveSoldThingToPlayer` handles pawn purchases separately (PreTraded, lord loss,
  sold-prisoner bookkeeping). Item path splits count, applies trade consequences, places near source
  and marks extraForbiddenThings after successful placement. Failed placement logs and destroys its
  remainder. Our enhancement starts from actual successful physical handover; it does not replay
  trade side effects or promise to repair native handover failure outside that boundary.
- `TradeDeal.UpdateCurrencyCount` balances the currency transfer, and `Tradeable.ResolveTrade` sends
  incoming silver through GiveSoldThingToPlayer too. A sell-only/net-sale trade therefore has an
  incoming item receipt eligible under the source prefix. Delivery covers these proceeds as well as
  merchandise purchases; only outgoing sold goods are excluded.
- Current-beta `ForbidUtility.IsForbidden(Thing,Pawn)` returns true for the party's extraForbiddenThings.
  `JobDriver_HaulToCell` records forbiddenInitially and omits its source forbidden failure for an
  initially forbidden item; `JobDriver_HaulToContainer` checks destination forbiddance. The source
  directly orders these jobs. Owned automatic permission must distinguish the exact receipt from
  general reacquisition: permit only its assigned delivery portion without removing the shared
  protection or overriding explicit player, route, reservation, ritual or provider restrictions.
- `VisitorGiftForPlayerUtility.GiveGift` chooses giver, places each supplied gift, destroys failed
  placements and sends the ordinary successful gift letter. Generation/chance/goodwill and letter
  semantics are separate from delivery. The input list can contain consumed/merged references;
  do not infer actual spawned receipt just from method return or original list entries.
- `TransitionAction_GiveGift` calls GiveGift then clears its list. Native `LordJob_VisitColony`
  invokes gift actions during routine timed departure. A blanket check for any departure would
  remove this feature's normal gift use case.
- `StoreUtility` prefers strictly better enabled accepting player storage; equal-priority slot versus
  container favors the slot path. Slot selection has native priority/distance sampling, not a global
  shortest-path guarantee. Actual cell/container permissions, reservations, fire/construction blocks,
  capacity and reachable path constrain it. Linked group policy never creates shared physical space.
- `HaulAIUtility.HaulToCellStorageJob` calculates capacity over slot/group cells and enables
  opportunistic duplicates; it does not limit count to the transferred receipt. Container job count
  is min(source stack, receiver acceptance). `Toils_Haul.StartCarryThing` caps the actual pickup by
  carry space, job count and stack count. Owned delivery must bound receipt quantity and disable
  unrelated opportunistic pickup even when using native single-load movement.
- `Pawn_JobTracker.TryTakeOrderedJob` marks playerForced and can clear queues/interrupt current jobs;
  queue request changes behavior but is not protection for urgent duties. Automatic visitor courtesy
  must not be implemented as an unrestricted player-forced assignment.

Selected native types are retained under `game`; Toils_Haul was inspected from the same-beta S021
artifact (`pick-up-and-haul/game/Verse.AI.Toils_Haul.decompiled.cs`). No game process was launched.

## Hospitality and other bindings

Installed `Hospitality.LordToil_VisitPoint.Cleanup` calls Leave. Leave calls OnLeaveTriggered,
updates guest state and invokes native GiveRandomGift when its own score/chance/options allow it.
That reaches native GiveGift. Its routine departure transition goes to travel/exit; threat/temperature/
hostility transitions differ. The source contains **no explicit Hospitality package/type adapter**:
it benefits from this shared native call. Our delivery must coordinate that routine departure while
preserving urgent exit and Hospitality's gift generation, scores, notifications and visitor policies.
Any required extra lifecycle adapter remains package/shape guarded; it does not justify copying
Hospitality code or adding a hard dependency.

Literal `.cs/.xml/.dll` scan for source name/package across installed Workshop found **12 files**:
eight source-owned About/Manifest/ModSync/version DLLs, and four YART (`seohyeon.yart`, Workshop
3745434121) LayoutHarness sample mod-list XMLs. Those four are package listings, not runtime consumers.
Same local game Mods scan returned no matches. No consumed source API was established; public
GetHaulJob and LogMessage alone do not justify a facade or package spoof.

A separate scan of native GiveGift/GiveSoldThingToPlayer references finds other callers/patches,
including Hospitality and unrelated trade providers, plus copied/reference game DLLs. That inventory
is not an active-mod list or a claim of support for every hit. Inspect exact effective semantics when
admitting a boundary. Orbital ships, world-map trade and mod-specific sale implementations that do
not use the inspected on-map receipt route are outside this feature's promised delivery scope.

Source has no own saved type/key schema: it schedules ordinary jobs and uses native lord state.
Our pending receipt/quantity ownership and recovery require durable owned state while active; that
is a deliberate correction, not an asserted legacy migration. No source-save import or safe-removal
guarantee is inherited. Future import needs its own backup/atomicity/restoration/postcondition contract.

## Proposed corrections and direct composition

1. Apply eligibility consistently: nonplayer, non-animal, capable/free courier in the corresponding
   visiting party or actual gift-giver set; never main trader, purchased pawn or unrelated colonist.
   Try eligible couriers by proximity with current reach/storage checks. Preserve safety, sleep,
   rescue, patient care, combat and existing delivery/haul commitments. No global job-name heuristics.
2. For all incoming trade receipts, including sell-only silver, a trade party already leaving gets
   ordinary ground handover. For actual routine
   departure gifts, allow a bounded courtesy before exit if the provider's lifecycle supports it.
   Dangerous/emergency departure always wins. This distinction preserves the advertised guest feature.
3. Proposed finite effort: each available courier receives at most one ordinary carrying load per
   handover event. No added multi-trip work queue, unrelated pickups or repeated retry after failure;
   excess/unassigned items remain ground-accessible to the colony. Preserve current native carrying
   limits and existing carried/personal items; no inventory batching/extra capacity for visitors.
4. Schedule only actual successfully received item quantities. Match physical placement merges/splits,
   don't double count stacks, and preserve native payment/gift effects. Do not shift old colony stock
   just because it merged with a receipt. Anonymous mixed-stack removal retires receipt units first,
   oldest event first, preserving old stock; attributed transfers affect their named portion. Include
   incoming silver from net sales and give only the assigned courier/portion the narrow handling
   permission above. No teleport, duplicated items, repeated PreTraded or gift letter.
5. Delivery storage obeys priority, actual member/provider filters, reservation and space. S005 shared
   settings do not multiply capacity. Coordinate incoming reservations with S021 shared facts; that
   does not admit visitors into general PUAH work. S024 must not collect receipt cargo for personal use,
   pack it into inventory, clean/detour instead, or delay departure. Owned composition uses direct calls.
6. S049 per-item X/Y thresholds govern automatic refill, not explicit trade/gift receipts; these
   deliveries can exceed Y. Its storage-wide per-stack hauling ceiling still applies to courier
   hauling and cannot exceed the actual item stack maximum. This follows the interview distinction;
   S049 remains separately queued for source analysis, no new slider/settings implementation here.
7. S039 trading-area successors constrain gathering; delivery remains a temporary storage trip,
   followed by ordinary return/departure. No trading area becomes a hidden allowed-area permission
   override. Exact future area behavior is owned by S039 when its turn arrives.

No source settings UI exists. Keep automatic courtesy behavior and native views without a new tab,
gizmo, toggle or settings menu. Receipt ownership is gameplay state, not global/save configuration.
Any later actual configuration must use the common inheritance system and be separately specified.

## Open questions retained for later

| ID | Topic | Current disposition |
| --- | --- | --- |
| VD-Q1 | Finite effort and corrective scope | One load per eligible courier/event, no main trader or queue override, receipt-first/oldest-event-first removal, scoped handling permission and routine-gift departure exception are explicit proposed corrections; no automatic extra-trip service |
| VD-Q2 | Native receipt/recovery boundaries | Prove placement merge/split counts, native notification/order timing, group departure and recovery through reload/death/despawn; ordinary failed handover is outside delivery start |
| VD-Q3 | Providers and composition | Exact Hospitality departure/carry policies, current storage provider shapes, S049 hauling-versus-refill limits and S039 gathering behavior need later reviewed-build proof; no blanket provider compatibility |
| VD-Q4 | Implementation and acceptance | Per-patch target docs/guards, receipt persistence, focused RED/GREEN, seven-language diagnostics, independent review and real trade/gift-to-storage workflows remain open |

No question is being asked now. All implementation tasks remain unchecked.

## Draft review and validation checkpoint

Two independent read-only reviews covered player behavior/composition and source/contracts. Four
findings were resolved: anonymous mixed-stack removal now retires receipts first, oldest event first;
the courier's pickup permission is limited to its receipt and preserves other restrictions; incoming
silver from sales is explicitly covered; and the already-departing trade exclusion also covers that
silver. Focused follow-up reviews found no remaining issues. Additional current-beta ForbidUtility,
HaulToCell, HaulToContainer and Tradeable decompilations are retained with the native evidence.

Typed `openspec_validate`: **48 passed, 0 failed**. The final capability has **8 requirements / 29
scenarios**, all **23 tasks unchecked**. A validator formatting error after the departure clarification
was corrected by keeping its SHALL on the requirement's first line; the meaning did not change.
The eight-file local-link/whitespace check found **0 issues**, with no duplicate refinement question
IDs. Queue: **123 rows, 27 drafted**. No runtime code, package change or game launch occurred.

Semantic review: prices, currency amounts, gift generation, research and actual carrying/storage
capacity stay unchanged. Courtesy reallocates finite hauling labor to available visitors; it cannot
create resources, expand access to old stock or delay emergency work indefinitely. Incoming sale
proceeds are ordinary receipts, not extra income. S049's automatic-refill target differs from its
per-stack hauling ceiling. Final loaded-game route, provider, recovery and shared-data cost proof
remain open, with **S028 Replace Stuff - Continued** next in the sequential queue.
