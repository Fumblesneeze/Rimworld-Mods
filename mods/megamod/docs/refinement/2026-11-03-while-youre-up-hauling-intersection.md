# While You're Up 1.7: batch-hauling intersection

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Research date **2026-11-03**. This is S021 PH-Q5's bounded S125 interface comparison, **not** the
complete S125 specification or a native compatibility test. S125 remains separately queued.

The [subsequent composition record](2026-11-03-hauling-composition-decisions.md) applies this evidence
to S021. Independent read-only source review found no issues and confirmed all three hashes below;
full S125 behavior/helper/UI/consumer research remains open.

## Documentation and exact inputs

Documentation first: the [modern Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3832180338),
installed About/English keyed descriptions, the
[original Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=2034960453), and
[original public repository](https://github.com/CodeOptimist/rimworld-while-youre-up).
The web reader could not open the modern page; a direct public HTTP read returned its description.
It advertises along-route hauling, moving work supplies closer, storage-type filters, diagnostic
paths and optional PUAH unloading improvements. The original describes avoiding general opportunities
while bleeding or forming caravans and grouping unloads by storage. These claims required code checks.

Current installed source: `F:/Steam/steamapps/workshop/content/294100/3832180338`, title
**While You're Up/顺路搬运**, author **xiaopu**, package **patch1.7.whileyoureup**, no modVersion in
About, advertised 1.6/1.7, Harmony required. Declared incompatible with hoodie/kevlou ports and
`CodeOptimist.JobsOfOpportunity`. Original reference: Workshop 2034960453, package
`CodeOptimist.JobsOfOpportunity`, installed highest assembly 1.5. Upstream and port credit AGPL-3.0;
independent behavior implementation only, no copied source, assembly or art.

| Input | SHA256 |
| --- | --- |
| Modern About/About.xml | `D0B24A68FD7D3CCE2391C6629C88C87F8DE48481B9D2AC6FB5D7B76017854F54` |
| Modern 1.7/Assemblies/WhileYoureUp.dll | `867B0C299677621C03888204FDD1045DBF64EBA257FBBBAA38F42C4647F2E1F2` |
| Original 1.5/Assemblies/WhileYoureUp.dll | `2252ED2E24B5A7C9D5A43247278A76A01E37933E0C021B109A602CBCE86133E2` |

The modern hashes agree with the earlier intake. Installed assembly metadata is **1.7.0.0**,
informational **1.7.0**. No modern source files are included in its Workshop inventory. Read-only
decompilation is retained at `artifacts/Refinement/megamod-spec-20261103/while-youre-up/installed-project`.
All non-settings-window sections of its main `WhileYoureUp/Mod.cs` were read: detour state,
PUAH discovery/hooks, opportunity/supply selection, storage routing/cache, settings persistence,
initialization and capacity hook. `JobsOfOpportunity/ModCompat.cs` and assembly metadata were read.
The settings-window/filter implementation and bundled CodeOptimist helper classes are **not yet
fully audited**; complete defaults/filter UI, helper failure semantics, inbound consumers and native
targets belong to S125's remaining full deep dive. No upstream code was executed or built.

## Which hauling fork the port can discover

`havePuah` requires all four `PickUpAndHaul.*` types below plus selected methods/field, resolved by
type/member names without an active-package gate or full signature/semantic validation:

| Required binding | Consumer use |
| --- | --- |
| CompHauledToInventory | Generic native GetComp; later private `takenToInventory` field and `GetHashSet()` access |
| WorkGiver_HaulToInventory | HasJobOnThing, JobOnThing, TryFindBestBetterStoreCellFor, AllocateThingAtStoreTarget **or** AllocateThingAtCell; static skipCells |
| JobDriver_HaulToInventory | Identify source collection reports |
| JobDriver_UnloadYourHauledInventory | FirstUnloadableThing replacement, MakeNewToils finish callback and report identification |
| Native Def names | `HaulToInventory` WorkGiver and `UnloadYourHauledInventory` job lookup |

The first four type names and checked methods/field exist in the pinned **Continued** source and
its inspected installed bodies. AllocateThingAtCell is the selected fallback; private
FirstUnloadableThing still takes Pawn plus HashSet<Thing>, and the Harmony prefix only consumes
Pawn. Continued retains the source Def identities. Thus this name-discovery gate can recognize
Continued; it is **not proof** that every patch or resulting job is safe on the current beta.

**Reworked** uses `Dreadarm.PickUpAndHaul.*` and its own Defs/quantity API; its complete prior audit
and decompiled text contain no matching legacy type family. **Armful** likewise exposes none of
these types. With either alone, this PUAH bridge's discovery predicate is false; the port's native
opportunity/supply patches remain a separate path. Its docs' statement that PUAH has no 1.7 release
is stale relative to the installed Continued reference. Do not confuse that with a proven failure
to recognize Continued, or assume Reworked's public V1 API is consumed here.

Owned S125 must call S021 directly. No fake upstream assembly/type/Def identities or self-patching
stack is needed. External While You're Up is an actual broad/private consumer, not a compatible
public facade contract merely because the required names can be supplied.

## Actual PUAH changes and differences from our contract

The bridge adds a per-pawn detour record plus static planning caches/call-stack markers. Detour
types distinguish ordinary PUAH, on-the-way opportunities and supplies-before-carry work.

- JobOnThing records the seed and destination. HasJobOnThing/JobOnThing/allocation prefix/postfix
  pairs push/pop a shared marker stack; finalizer cleanup is not present in these pairs.
  Exceptions can leave context that needs explicit owned cleanup rather than a shared global flag.
- The source replaces PUAH storage lookup with native StoreUtility and intercepts that lookup
  during marked collection or the source unload job. It tracks destinations by Thing in short-lived
  caches and by **ThingDef** in the detour. Candidate cells are still checked against filters,
  priority, native acceptance and skip cells. A per-Def remembered cell is not exact allocation
  for different material/quality/stack limits and must not replace our quantity budget.
- `FirstUnloadableThing` is replaced with nearest remembered destination selection, then groups
  items by that actual SlotGroup and orders within it by category index/defName. Missing destinations
  fall back to the source item order. This motivates delivering a finite group before walking away;
  it is not proof of one literal drop operation or a globally optimal route.
- The replacement removes a stale tracked reference, then searches inventory for the **first
  same-Def item** and returns its whole count. This can substitute personal or different-quality
  goods. Continued's own newer implementation removed that fallback, but this prefix bypasses
  it when active. Our exact quantities/identity protection must survive composition.
- Opportunity batching totals a proposed collection path, first unload leg, successive nearest
  unload legs and the return to the original target using straight-line distances. Extra pickups
  that exceed total/new-leg ratios are rejected. This is whole-trip accounting, not permission to
  apply independent allowances to every extra item. The S125 full spec must own route thresholds.
- When the original store fills during an opportunity, an alternate in the same SlotGroup is
  accepted; a different group is tested against remaining-route ratios. Default debug-tweak
  `dumpIfStoreFilledAndAltsInopportune=true` can reject the alternative and lead into PUAH's drop
  path. Our retained-cargo/failed-drop protection still applies; rejection is not successful delivery.
- Before-carry extras must use the selected SlotGroup. At equal priority, or while bleeding,
  construction/bill relevance further restricts extras. The code checks a current HaulToContainer
  frame's remaining Def need or a current DoBill queue's Defs; it is not a general proof of exact
  material/quantity relevance for every job state. S014/S026 choices remain authoritative.
- Equal-priority support temporarily makes a source storage's Priority getter report one level
  less and appends its items into returned haulables, then restores state in a postfix. Direct
  owned planning should express that permitted move without mutating global storage meaning,
  foreign preferences or the authoritative shared candidate collection.

Detours are weakly keyed by pawn and not serialized. Finish/queue clearing deactivates them;
destroy removes them. PUAH opportunity finish retains a frame-count stamp and suppresses new
opportunities for up to **five rendered frames**, not five simulation ticks. AlreadyHauling also
checks same-frame loop state, active detour and source private cargo references. It cannot discover
our future owned quantity ledger without an explicit integration. The source's protection is
therefore not automatically inherited by an independently implemented batch system.

## Other observed intersections, not complete S125 scope

General opportunities run in the native opportunistic hook, skip four caravan-preparation JobDefs
and bleeding above **0.001**, and select a current native target. Bill supply detours are attempted
before that general bleeding check. Supply detours require the resource not already be in pawn
inventory, room for two units by native mass and a storage cell strictly closer to the work target
in straight-line distance. They can fall back to a normal native HaulToCell job when PUAH is absent
or returns no job. This fallback still needs owned cargo/capacity coordination when composing S125.

Settings persistence gives source defaults: enabled and PUAH-plus on; path mode Default; start
range 30, start/original ratio 0.5, store/job range 50, store/original ratio 0.6, total trip ratio
1.7, new-leg ratio 1.0, both region budgets 25; stockpiles and automatic building filters on;
supplies, bills and equal-priority relocation on. These are comparison inputs, not already selected
S125 values. Default mode stops the entire opportunity search after its candidate fails final path
cost checks; Pathfinding mode continues to other candidates. Soft search ranges expand by two.
The complete filter/default catalog, helper failure behavior and path-state semantics remain open.

The port adds an optional **mass-capacity multiplier**, default off, stored factor 1, UI text
advertising 1–10. Its transpiler replaces the first 35f constant in MassUtility.Capacity; the helper
returns 35 × clamped factor while that option is enabled. The helper does **not** consult the main
Enabled toggle. This is an additional global capacity modification, not a new safe batch allowance.
It is not adopted by our no-inflation hauling contract; externally supplied actual capacity changes
still require validation as ordinary runtime inputs. Literal-first replacement is not semantic
proof of the correct calculation.

Common Sense integration reflects `CommonSense.CommonSense`, Settings and `hauling_over_bills`.
Opening one source settings window can turn off the other's global bill-hauling option and write
Common Sense settings. S024 already rejects writing foreign preferences and requires one detour
provider. Owned S125/S024 must share the opportunity and parent-job lifetime directly, with no
duplicate or recursive storage trips. Source debug routes belong in our developer controls;
actual configuration uses common global/save settings, with no new bottom gameplay tab.

Startup is unguarded PatchAll under old owner `CodeOptimist.WhileYoureUp`. The new construction
transpiler has local-load and injection-point checks, but its local finder can fall back to the
first local without proving type; one late failure returns a stream after earlier insertion.
The source's warning text is not proof of no partial patch. Full helper/incoming-IL analysis and
native beta verification remain future S125 work. Every owned actual hook needs minimal semantic
target predicates, dependent rollback and unchanged incoming code on failure.

## Consequences for S021 reconciliation

1. Keep Continued as primary behavior reference, Reworked's exact-quantity/recovery ideas and
   Armful's selected visible cargo. Choose one owned inventory/quantity model with native transfers;
   do not compose upstream patch sets or grant separate source allowances.
2. Select grouped nearest-destination unloading as the S125-derived shared S021 delivery behavior;
   retain per-item eligibility, physical success, partial capacity and exact personal protection.
3. Keep finite collection before delivery. A route supplied by S125 further constrains the same
   batch and may not silently become unrestricted ordinary collection. Preserve the parent job,
   prevent recursive opportunities and coordinate S024's whole-stack bill detour.
4. Retain exact cargo after failure, drafting and save/load. No stale same-Def substitution,
   source-private static state, rendered-frame retry rule, per-Def destination authority or
   preference-dependent disappearance of recovery.
5. Treat external source While You're Up's private interface as unsupported pending a verified
   bounded adapter. Never expose the old HashSet facade over mixed personal/cargo quantities.
   A loaded owned batch must not receive an uncoordinated native extra hand allowance through an
   external opportunity. If that boundary cannot be protected, withhold affected new batching
   and preserve independently safe cargo recovery with an explanation.

**WY-Q1 remains open:** complete modern filter/settings/helper/source analysis, original provenance
comparison, actual inbound consumers, full owned S125 proposal/design/scenarios/tasks and native
admission/acceptance. This intersection resolves which fork is name-discoverable and what must be
composed safely; it does not certify the port or complete its replacement.
