# Whole-list workbench linking — original feature research and draft decisions

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
S014 / P1-11, **origin: original**. This follows the reviewed QualityBuilder draft. Native research
now supports [the owning draft](../../../../openspec/changes/specify-megamod-linked-workbenches/).
No implementation or compatibility acceptance is claimed.

## Confirmed intent and documentation boundary

The [refinement interview](2026-11-01-handwritten-notes.md) and conversation confirm that linked
workbenches synchronize the **entire bill list**, including additions, removals, ordering and every
bill's configuration. The user explicitly chose “the whole thing,” not individual linked bills
mixed with independent local bills. This is gameplay state attached to workbenches, not an inherited
mod setting, and it belongs in contextual bench/bill controls rather than another bottom tab.

No source mod was named for this original feature, so there is no upstream Workshop description,
source version or license to substitute for the user's intent. The later selected Better Workbench
Management, Everybody Gets One and Most Skilled Bill Restriction remain separate sequential source
deep dives. Their eventual controls must compose with whole-list linking; this document does not
claim to have analyzed them or adopt their per-bill link models.

Read the confirmed intake and existing S009–S012 browser contracts before inspecting native code.
Those contracts preserve one explicit bill addition through the opened bench's native boundary;
S014 must specify its propagation explicitly. S013 requires preserving applicable linked-bill
configuration through supported quality reconstruction. Existing shared-storage behavior is relevant
to count/output references, but storage groups and bill-list groups are different gameplay entities.

## Native inspection identity and method

Read-only local game input **1.7.4995 rev309**, `F:/Steam/steamapps/common/RimWorld/Assembly-CSharp.dll`,
SHA256 **56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21**, previously pinned
for this sequential research. No game was launched and no runtime behavior accepted.

The operation registry has no offline whole-assembly inspection operation; used existing local
ILSpy for bounded types, with output under ignored
`artifacts/Refinement/megamod-spec-20261102/workbench-linking/game`. Inspected available type names
before decompilation. Read `RimWorld.Bill`, `BillStack`, `Bill_Production`,
`Bill_ProductionWithUft`, `Building_WorkTable`, `Verse.RecipeWorkerCounter`; relevant UI/work paths
in `Dialog_BillConfig` and `WorkGiver_DoBill` are partly read and need completion. Reused earlier
read-only `quartermaster/game/RimWorld.ITab_Bills.decompiled.cs` and `RimWorld.BillUtility`.
Subsequently decompiled and read `Verse.AI.JobDriver_DoBill`, `Verse.AI.Toils_Recipe`,
`Verse.UnfinishedThing`, `RimWorld.BillRepeatModeUtility`, `Bill_Autonomous`, `Bill_Mech`,
`Bill_ProductionMech`, `Bill_ResurrectMech` and `Building_WorkTableAutonomous`. Rechecked the operation
registry: still no offline whole-mod/type operation; live Gateway decompilation is not needed here.

## Native list and identity facts

- Each `Building_WorkTable` owns a `BillStack(this)` and deep-saves it. A Bill points back to exactly
  one stack, whose giver determines its map/position context. Assigning the same Bill instance to
  several stacks would overwrite that owner context; whole-list behavior needs distinct native
  execution identities even if its player-facing choices are shared.
- `BillStack.AddBill` sets that owner and appends. `Delete` sets `deleted`, removes the bill and
  notifies the giver. `Clear` only clears the list; it is not a safe substitute for individual delete
  lifecycle. `Reorder` moves an existing bill. Native load removes null/missing-recipe bills and
  rebinds remaining bills to their stack.
- Native UI exposes at most **15 bills** through list Add and clipboard Paste checks. `AddBill`
  itself does not enforce that cap. A shared group must not accidentally multiply logical entries
  or bypass the applicable UI limit merely because propagation uses a lower-level call.
- Native Add/Paste checks recipe availability on the actual bench. Copy/Clone needs a fresh bill
  load identity via `InitializeAfterClone`. Base Clone copies many choices and allocates a new
  ingredient filter, but does **not** copy `graphicIndexOverride`; “use Clone” alone does not prove
  every observable bill choice has transferred. Source class-specific cloning must be inspected.

## Native configuration versus execution state

| Area | Inspected native choices / state | Implication for the eventual contract |
| --- | --- | --- |
| Identity/output | Recipe, precept, style, global-style choice, graphic variant, xenogerm, custom name | Shared choices must preserve identity and legal output choice; special bills require explicit support |
| Authorization | Suspended; pawn restriction; any/slaves/mechs/non-mechs; allowed skill interval | These are bill gameplay choices; keep provider/native eligibility rather than bypassing it |
| Ingredients | ThingFilter including special filters, ingredient-search radius | Same radius means a radius around each actual bench, not one shared search origin |
| Repeat | Repeat mode, remaining repeatCount, targetCount, pauseWhenSatisfied, unpauseWhenYouHave | Define shared counting explicitly; cloning remaining counts per bench could multiply requested production |
| Product counting | Include equipped/tainted, HP and quality ranges, limit to allowed Stuff, includeGroup | Same count scope must not be summed once per linked bench and double-count the same stock |
| Output | Best stockpile / drop on floor / specific storeGroup | Native references may be a storage group, zone or building and can become unavailable |
| Execution | nextTickToSearchForIngredients; active job; unfinished-product/worker binding; current paused latch | Do not confuse these with portable editable configuration or copy a physical item/job to every bench |

`Bill_Production.ShouldDoNow` reads actual product count, updates its paused latch and checks suspended
state. In repeat-count mode, `Notify_IterationCompleted` decrements a positive remaining count and
messages at zero, then notifies the recipe worker. Thus “synchronize configuration” alone does not
answer whether several already-started jobs can overshoot a shared remaining count or when a group's
completion notification occurs. These require explicit observable decisions, not repeated queries
with side effects or an independent hidden quota per bench.

`Bill_ProductionWithUft` holds one bound unfinished Thing; its worker follows that Thing's creator.
It clears an unusable worker binding in some cases and clears the bound Thing after completion.
The work giver tries a bound unfinished Thing first, otherwise a reachable matching recipe/creator/
allowed-ingredient unfinished Thing. Those native objects cannot be cloned just because an ordered
row is shared. Investigate deletion, changing filters, joining/leaving and save/load while work exists.

## Native UI, validation and work facts

- `ITab_Bills` adds a bill of the recipe's actual class. Factory branches include ordinary production,
  unfinished products, mech resurrection/gestation and autonomous forming. A medical bill also exists
  elsewhere. “Every IBillGiver” is not equivalent to the user's ordinary workbench request.
- `Dialog_BillConfig` exposes repeat/target/hysteresis, include filters/scope, storage destination,
  worker restriction and skill range, suspension, ingredient filter/radius and applicable style/variant.
  The source slider represents unlimited radius with **999**, displays 3–100 with the top mapped to
  that sentinel; preserve user meaning rather than making internal sentinel values new tuning options.
- Native output/count menus already include linked storage groups. They collapse grouping categories
  and use `CanPossiblyStore`; linked benches should retain the same selected storage identity.
- `Bill.ValidateSettings` clears a named pawn restriction when dead, not player-faction or kidnapped.
  Production validation removes invalid include/store references and changes missing specific output
  storage to Drop on floor. These are native automatic mutations that a linked group must reconcile
  explicitly; they cannot silently broaden one member while other members keep old restrictions.
- `Building_WorkTable.SpawnSetup` validates bills except during gravship transport. Native bill
  enumeration includes minified Things and caravans, so map transfer/removal cannot be treated as a
  simple UI-only event. Decide supported group/map lifetime before choosing hooks.
- `WorkGiver_DoBill` checks bench usability, reservations, interaction cell, burning and fuel; it can
  return refueling work before a production job. It removes incompletable bills and iterates list
  order, then checks work-giver type, ingredient recheck delay, ShouldDoNow, worker restriction,
  required skills, bound unfinished work, special state and actual ingredient access. Linked lists
  must keep real bench-local execution rather than promising every member can currently work.
- `RecipeWorkerCounter` uses map resource counters/native listers, carried/held/equipped goods and
  optional storage scope according to bill choices. It does not use the number of linked benches.
  Reuse native facts and the common shared-data owner where necessary; do not create a parallel
  full-map per-bench stock cache merely to synchronize lists.

## Additional native lifecycle evidence

- `JobDriver_DoBill` owns its concrete bill reference and physical target/ingredient queue, reserves
  the actual bench, and saves remaining work/timing. It fails when that bill is deleted/dereferenced,
  the station becomes unusable or a building despawns. A continuation compares the actual bill object.
- The first toil binds a resumed unfinished Thing to that concrete bill. `MakeUnfinishedThingIfNeeded`
  moves the actual ingredients into one new unfinished Thing, records creator, bill and remaining
  work, then reserves it. `DoRecipeWork` uses the worker and actual worktable speed, updates that
  Thing's remaining work and fails on suspension. It does not reevaluate every editable filter each
  work tick; shared configuration must not retroactively transform already selected materials.
- Ordinary finalization calculates actual ingredients and style, creates products, consumes ingredients,
  calls `Notify_IterationCompleted`, then stores/drops the real output. A count decrement occurs before
  hauling finishes. Reordering or refreshing settings must not replay this completion callback.
- `UnfinishedThing` reciprocally binds to one `Bill_ProductionWithUft`; deleted/dereferenced bindings
  clear on access/save. Its physical ingredients/work/creator are separately saved. Native cancel
  destroys it and recovers randomly rounded **75%** of ingredient stack quantities; simply deleting
  its bill is not that cancel operation and must not be treated as a free full refund.
- `Bill_Autonomous` has Gathering/Preparing/Forming/Formed state, machine-local forming ticks and
  an active bill on one `Building_WorkTableAutonomous`. `Bill_Mech` additionally binds a mechanitor,
  gestation-cycle progress and bandwidth restrictions. Resurrection uses an actual corpse/mech, not
  a generic interchangeable recipe result. Shared choices must not clone any of these local identities.
- An autonomous workstation holds actual ingredients/products. Its `Notify_BillDeleted` ejects contents
  and clears the matching active bill; `Destroy` resets and ejects contents. Bypassing that notification
  with `BillStack.Clear` can leave the wrong physical state. Ordinary deletion semantics matter.
- Machine `Notify_FormingCompleted` can create a real held product before the later extraction/work
  step calls the usual bill iteration callback. Accounting must distinguish physical production,
  remaining completion work and delivery; destroyed/removed formed machines must not duplicate a
  completed product's finite quota merely because collection was pending. Exact class-specific seams
  remain a documented implementation prerequisite.
- Native Repeat mode UI permits target count only when `RecipeWorkerCounter.CanCountProducts` is true;
  shared grouping does not invent a meaningful aggregate for recipes that cannot be counted.

## Proposed refinement decisions

The next spec treats one group as one ordered logical list, with a common supported recipe catalog
across its same-map production stations. All editable supported choices synchronize; physical work
and special-machine identities stay local. Ordinary and unfinished/autonomous/mech production are
intended scope, with class-specific lifecycle research required rather than silently excluding them.
Medical orders on pawns are not workbench production.

Finite repetition counts represent the remaining group order, including admitted unfinished work.
New starts cannot reserve more iterations than remain. A player reduction below already committed
iterations is rejected with an explanation; the player can finish/cancel that work first. Target-count
mode preserves native counting rules, shares hysteresis and permits ordinary in-flight overshoot;
it does not pretend reserved inputs or unmade products are already stock.

Group creation/join/merge explicitly chooses the authoritative whole list, previews lost destination
configuration and commits all-or-nothing. A destination with attached unfinished or active physical
work cannot have its list overwritten until that work is finished or canceled; an authoritative
source can retain its active jobs. Separation retains all row choices and physical progress, allocating
committed finite iterations to the station that owns the work and giving unstarted repetitions to
one explicitly selected successor, initially the continuing group. This avoids silently duplicating
the remaining quota into every newly independent list. These are proposals, not interview answers.

Independent review found one composed-contract conflict: S005's native missing-storage fallback
versus S014's explicit unresolved choice. Both drafts now scope native fallback to ordinary unlinked
bills; linked rows retain the missing scope and block new starts. S009/S012 also explicitly use the
linked group's common catalog, including ordinary fallback, without removing otherwise-supported
special bills merely because comparison cannot represent them.

Two lifecycle seams received additional observable scenarios while their implementation remains
WB-Q2/Q3: already credited machine output is collectable at zero remaining count after Leave,
Dissolve or last-member conversion; transferring a bench alone does not transfer an unfinished Thing
left behind. With an original group still present that item/commitment stays there. If all members
move without it, their moved plan retains one visibly unavailable commitment until physical recovery,
native cancellation or ordinary irrecoverable loss. No teleportation or double accounting is implied.

## Remaining research and acceptance questions

| Question | Work still required | Boundary until resolved |
| --- | --- | --- |
| WB-Q1 | Exact supported edit/native validation paths, special/provider choices, common catalog and stale UI | No claim that generic Clone or public-field copying covers every observable choice |
| WB-Q2 | Per-family actual admission, unfinished/formed completion credit, initialization, interruption and persistence | No duplicate finite quotas, copied physical progress or stranded result collection; exact semantic seams remain implementation research |
| WB-Q3 | Minification, caravan/gravship/map lifetime, partial/full transfer, surviving work, S013/S028 handoff and later bill modifiers | Same-Thing or explicit replacement identity, no old-cell guessing or presumed provider compatibility |
| WB-Q4 | Native UI/languages, proposed common catalog/allocation/recovery playability, measured multi-member cost | No gameplay, balance or optimization acceptance from static source analysis |

Independent review and document-check outcomes belong in the sequential ledger. All implementation
and native acceptance tasks remain unchecked; no gameplay code or normal save/configuration changed.
