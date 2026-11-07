# S123 — Routine colonist meal carrying research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target1.7 beta.
Origin **original**. The user's confirmed requirement is routine meal carrying for all colonists,
excluding animals. Quantity and eligibility details were not confirmed. This is not a source mod
called Carry Food; that invented interview name remains excluded.

Status on2026-11-06: **native/composition research and independently reviewed, strictly validated draft**.
[Owned change](../../../../openspec/changes/specify-megamod-routine-meal-carrying/) and
[coverage](../replacements/routine-meal-carrying.md). No implementation or native acceptance is claimed.
MC-Q1–5 retain proposed choices and remaining implementation evidence.

## Existing owned contracts to preserve

- S024 Common Sense, `megamod-practical-supplies`, already specifies the native packing opportunity
  above30% Food rather than60%, retaining ordinary inventory/reserve eligibility and shared
  carrying capacity. It explicitly reserves S123 for complete colonist-carrying composition.
- S021 owns physical kept/cargo quantities and combined carrying allowance. A personal reserve
  must not be counted twice or become a second unlimited inventory.
- S055 Meals On Wheels supplies fallback food sharing after ordinary selection fails. General
  keep protects unloading, not food sharing; current-use portions and active transport claims
  stay protected. S123 must not reclaim a borrowed meal awaiting ingestion or create a steal/refill loop.
- S057 preserved-food preference, S024 food scoring and S111 Room Food contribute to ordinary
  food selection in their specified scopes. Do not copy their patches or give personal packing
  unrestricted donor/shuttle access.
- S041 preserves ingredients, condition and tainted portions through split/merge/transfer.
  S062 Smart Medicine and S073 bedrolls consume the same capacity; explicit pawn inventory choices
  remain contextual gameplay state, not global/save settings.

## Native source findings

Read-only offline ILSpy of current native Assembly-CSharp.dll, RimWorld1.7.5000rev1275, SHA256
`D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254`.
Local extracts: `artifacts/Refinement/megamod-spec-20261106/routine-meal-carrying/native/`.
The user requirement is the primary source for this original feature; there is no upstream mod
documentation to reverse engineer. Related selected-mod documents were already researched in their
own sequential changes; their owned contracts above are the composition inputs.

### Native already has an automatic packing opportunity

Core `ThinkTreeDefs/Humanlike.xml` puts JobGiver_PackFood in the colonist branch after emergency
work, starving-food handling, inventory-stock collection and unloading. It is gated by Food above
0.6. ThinkNode_ConditionalColonist delegates to Pawn.IsColonist: player-faction humanlike,
non-subhuman, with slaves included only when secure. Earlier tree branches handle downed, burning,
mental/danger, bed and forced-duty states. The owned draft adds explicit safe self-feeding/capability
checks and excludes dependent patients; it does not copy S021 batch-hauler membership wholesale.

`JobGiver_PackFood`:

- Requires inventory and rejects carried packable nutrition above0.4.
- Requires map TotalHumanEdibleNutrition at least1.5 × ColonistsSpawnedCount; rejects already
  over-encumbered pawns. This is a communal supply check, not reserved ownership of individual food.
- Searches reachable non-plant food within20 cells, using food optimality at a supplied distance0
  after eligibility checks. It requires a haulable nutrition-giving ingestible, at least awful-meal
  preference, WillEat permission, mass room for at least one unit, non-forbidden/reservable/socially
  proper food and existing nutrition+candidate stack nutrition at least0.8.
- Rejects a candidate when any returned ingestion thought's stage0 base mood is negative. That is
  the exact source test, not yet a promise about the pawn's effective final mood or title permissions:
  its IsGoodPackableFoodFor calls WillEat with careIfNotAcceptableForTitle=false.
- Requests floor((Food.MaxLevel−existing nutrition)/candidate nutrition), capped to source count,
  then at least1. The shown calculation does not cap the entire requested count to remaining mass;
  the completed transfer inspection below establishes the missing whole-request native clamp.
- Counts all qualifying inventory stacks when measuring existing nutrition; it does not distinguish
  another delivery's cargo from the pawn's personal meal reserve.

`Verse.AI.JobDriver_TakeInventory` reserves the requested count, approaches the actual stack,
checks despawn/forbidden changes, observes any job pickup delay and uses Toils_Haul.TakeToInventory.
Its encumbrance failure checks whether at least one unit fits when job.checkEncumbrance is enabled;
the completed transfer inspection below clarifies that flag. It does not generate food.

### Native retention needs composition with explicit inventory quantities

`Pawn_InventoryTracker.FirstUnloadableThing` combines drug-policy and native inventory-stock keep
entries, then retains eligible non-drug colonist food relative to Food.MaxLevel. It selects whole
units to unload until remaining packable nutrition is no more than that maximum. Already assigned
stock and automatic food can therefore interact in this calculation; use S021's actual quantity
authority rather than implementing independent retention rules for each port.

`JobGiver_DropUnusedInventory` has separate raw-food aging and drug-policy dropping behavior; it
is not the reserve-meal selector. Do not indiscriminately disable inventory cleanup.

Native Need_Food.MaxLevel is not universally1 nutrition: during play it reads MaxNutrition; outside
play it derives from body size and life-stage food factor. Investigate small bodies, unusually large
meal portions and changed nutrition providers before choosing a reserve target. The mismatch
between minimum pickup1 and an unload ceiling below one meal's nutrition is a possible loop to
test, not an observed game failure.

## Completed transfer and composition inspection

Fresh offline extracts of Verse.Pawn, Need_Food, FoodUtility, Toils_Haul and Job accompany the
giver/driver/inventory extracts. Native Job.checkEncumbrance defaults/resets to false; PackFood
sets the count but not that flag. Toils_Haul.TakeToInventory clamps to remaining mass only when
the flag is true. The default pack path therefore does not prove a full-count mass limit from its
initial one-unit fit check. Its shown TryAdd of SplitOff also ignores the acceptance result. These
are code-derived risks, not observations of lost food or overloading in a running game. The owned
contract requires checked actual acceptance and S021 combined mass+volume limits regardless.

WillEat applies suitable food and respected policy/ingredient restrictions, but PackFood explicitly
passes careIfNotAcceptableForTitle=false. Owned optional reserves use non-desperate title permission.
In the inspected native NutritionForEater, prepared meals use the Thing nutrition unchanged;
Biotech RawNutritionFactor applies only to raw human food, which reserve packing excludes. The
owned draft still consumes the actual eater-aware nutrition result so an admitted provider can
change prepared-food yield; that scenario is conditional provider behavior, not a claim that
vanilla genes change prepared-meal nutrition. Native packing's
stage0 thought rejection also cannot establish the effect actually applicable to this pawn.

The shared-carried-food contract already permits another eligible colonist to borrow kept food
for real hunger and protects current-use portions. Native inventory stock may keep additional
explicit items; that is entity state, not another automatic reserve quota. S111 explicitly excludes
routine inventory refilling, so dining-room preference is not expanded here. Each existing owned
food feature supplies its already-specified contribution once, not another independent food finder.

## Proposed policy and intentional differences

These are proposed refinements of the short user requirement, not claimed interview decisions:

| Concern | Chosen draft behavior | Basis / difference |
| --- | --- | --- |
| Reserve size | Default one current full Food bar; common global/save target0–3 by0.25 | Native targets Food.MaxLevel; upper bound/setting are proposed bounded tuning |
| Refill gate | At or below40% of target and below target; current Food>30% | Scales native0.4 inventory threshold; retains selected S02430% opportunity |
| Whole-unit pickup | Floor remaining target divided by actual nutrition, limited by real quantity/capacity; one oversized unit only if empty | Native floor/min1 basis with explicit empty-only exception and corrected retention |
| Partial supply | Useful smaller portions allowed without native0.8 candidate minimum | Avoids rejecting a safe partial reserve; no new food generated |
| Communal stock/range | Native1.5 nutrition per spawned colonist entry check and20-cell nearby limit | Recheck before taking; not a guaranteed post-pickup floor or diet-specific allocation |
| Food permissions | Prepared, fresh, positive actual nutrition; real policy/title/ingredient/thought conditions; no drugs/raw substitutes | Explicit optional meal scope and corrected title/stage assumptions |
| Retention | Same shared protected quantities as pickup, including one oversize meal | Prevents contradictory pickup/unload plans; explicit keep remains authoritative |
| Manual removal | Suspend replacement while pending; successful deliberate removal pauses until explicit Resume | Proposed visible entity control to prevent immediate pickup reversal; auto eating/sharing does not pause |
| Shared use | Reserved meals remain usable under normal eating/S055 sharing | Keep is not a private food lock; donor refills only from communal map food |

S024's active practical-supplies requirement is updated in place to name this refinement of native
inventory thresholds/retention explicitly, while retaining its Food trigger and communal guard.
No internal patches or second packing loop are selected. No settings scope exception is needed:
target is common configuration, while pause, explicit stock, diet and quantities belong to entities.

## Quantity and balance cross-check

The closest comparators are native PackFood (adult appetite1, usual meal0.9 → one meal), native
unload ceiling (one unit can exceed a small body's Food.MaxLevel), and S021/S055 carrying/use of
actual limited portions. The proposed default reproduces the ordinary one-meal case while making
retention consistent for small bodies and obeying combined capacity. Examples in MC02 use actual
nutrition units, not percentages of current hunger: target0.35 with meal0.9 permits one unit;
target1 with two fitting0.2 portions permits0.4 reserve; reserve0.3 plus only0.9 meals permits no
oversize top-up. A partial pickup above40% can remain below target until food is used. This is
intentional opportunity-based behavior, not a promise to fill every target immediately.

Meals remain physical existing stock. Their nutrition, poison history, spoilage and mood are not
improved by packing. Higher configured targets can tie up communal food and carrying capacity;
the upper range, communal entry threshold and work/care interruption must be measured in native
playability review. No runtime balance or performance improvement is claimed from static analysis.

## Open questions and implementation evidence

| ID | Remaining issue | Handling |
| --- | --- | --- |
| MC-Q1 | Reserve target/range, eligible colonist edge cases and rounding playability | Preserve confirmed all-colonist intent; proposed defaults are explicit in MC01–02/MC10, native examples support them but live balance remains pending |
| MC-Q2 | Exact effective opportunity, transfer and retention target semantics | Per-patch target documents, actual incoming/effective code, atomic admission and native pickup/cleanup proof |
| MC-Q3 | Shared provenance, current-use/stock claims and pause/restore lifecycle | One S021 authority and S024/S055 composition; verify split/merge, explicit versus automatic removal, changed permissions and no loops |
| MC-Q4 | Optional inventory/food providers and S132 presentation | Inspect actual package/content/member contracts and one packer authority; no blanket compatibility claim |
| MC-Q5 | Final native and semantic balance acceptance | Focused RED/GREEN, reviewed-build real actions, displayed/physical counts, settings/save switching, actual eating and cleanup |

No runtime code/test/build/install, game launch, user configuration/save mutation, Workshop edits
or source-mod replacement claim occurred. Document review and strict validation are recorded in
the progress ledger; all implementation tasks remain unchecked.
