# Pick Up and Haul - Continued and alternative references

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Queue **S021**, intake **P1-16**, origin **replacement**. **Current-reference composition drafted.
Nothing implemented or accepted in game.**

Current primary: **Pick Up and Haul - Continued**, `Zzyxzz.PickUpAndHaulReworked`, Workshop
**3829549663**, [public source](https://github.com/Zzyxz/RimWorld-PickUpAndHaulContinued).
Compare **Pick Up And Haul - Reworked**, `Dreadarm.PickUpAndHaul`, **3813653998**, and
**dd.Armful Haul**, `dd.armfulhaul`, **3808100512**. The latter's **visible hauled stacks on the
pawn are required** under S126, regardless of the selected hauling logic. S125 now owns selected
While You're Up behavior, using the modern `patch1.7.whileyoureup` reference plus its original.
See [exact identities, user direction and research limits](../refinement/2026-11-02-hauling-reference-update.md).

The [Continued source audit](../refinement/2026-11-03-pick-up-and-haul-continued-research.md) covers
all 17 C# files and selected installed 1.7 bodies. It finds separate personal/cargo merging,
stale-reference removal without same-type substitution and real CE weight/bulk integration.
Its default 130% capacity multiplier and ordinary hand-load remainder path remain deliberate
deviations for our single allowance; recovery after a preference is disabled and failed-drop
tracking also need owned corrections.
The [Reworked audit](../refinement/2026-11-03-pick-up-and-haul-reworked-research.md) records its own
quantity ledger, physical recovery, native retention, merging/loading, API and narrower worker scope.
It still adds hand capacity to full inventory and differs in search/settings; its advertised manual
pickup needs a beta-path check.
The [Armful audit](../refinement/2026-11-03-armful-haul-research.md) covers all 17 source files and
selected installed bodies: physical hand stacks, per-stack limits, native job wrappers, demand
coordination, recovery boundaries and visible cargo. Its [selected visual subset](armful-haul.md)
has an owned S126 draft. The [modern While You're Up intersection](../refinement/2026-11-03-while-youre-up-hauling-intersection.md)
records its legacy bindings and actual routing/recovery changes; its complete S125 analysis is still queued.

## Selected current composition

The [decision record](../refinement/2026-11-03-hauling-composition-decisions.md) selects one owned
inventory-based quantity model with native physical transfers. Continued supplies worker/search
defaults, Reworked informs exact tracking and bounded recovery, While You're Up informs grouped
delivery, and S126 supplies Armful-inspired visible cargo. Every selected behavior is specified;
no upstream implementation or patch stack is imported.

| Selection | Observable draft / deliberate difference |
| --- | --- |
| One allowance | Count personal supplies, cargo and hands together; reject each source's extra hand or multiplied allowance; PH-Q1 retains combined-volume playability research |
| Protected quantities | Respect kept/loadout/medicine/sidearm, cart/holder and reservation authority; no same-type recovery guess, including While You're Up's replacement unload path |
| Grouped delivery | Visit a physical destination's valid portions together, choose nearest valid reachable cell by straight-line distance, recheck every placement; linked shelves remain separate physical destinations |
| Recovery | Preserve failed physical cargo and urgency; unchanged automatic failures wait 600 simulation ticks, saved across reload, with changed-condition/manual retry exceptions |
| Drafting | Retain cargo by default; optional safe cargo-only draft drop initially off, deliberately differing from Continued; no drop when merely enabling the setting for an already drafted pawn |
| Common preferences | Corpses off, capable animals/mechs on, 20% free-space threshold and draft-drop off; all five use global defaults/save overrides |
| Opportunities | One directly composed S024/S125 detour retaining its parent purpose, quantity/destination/travel bounds and production choices; full S125 filters/thresholds remain separate |
| Explicit limits | No blanket adoption of Reworked merging/loading/repair tools, Armful construction/prison/floor wrappers or While You're Up's capacity multiplier; S133/S135 own loading work |

### Historical comparison retained for provenance

The following historical table and source facts describe the **earlier Mehni analysis** and retained
proposed contracts. They do not describe every newer implementation; the current selection above
and linked audits supersede the legacy-only basis for PH-Q5.
Keep the user's capacity/shared-data requirements and visible-cargo selection. No source superiority,
binary compatibility or whole-mod adoption is inferred from a newer title/version.

Historical source **Pick Up And Haul**, Mehni, `Mehni.PickUpAndHaul`, Workshop **1279012058**. Documentation:
[Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=1279012058),
[official source](https://github.com/Mehni/PickUpAndHaul),
[releases](https://github.com/Mehni/PickUpAndHaul/releases).
The installed latest assembly targets 1.6; no 1.7 compatibility is verified. Source is MIT-licensed;
no downloaded assembly is copied into the product. Source/binary identity, native comparisons and
actual consumers are recorded in [research](../refinement/2026-11-02-pick-up-and-haul-research.md).

[Proposal/design/tasks](../../../../openspec/changes/specify-megamod-batch-hauling/) own
[batch hauling](../../../../openspec/changes/specify-megamod-batch-hauling/specs/megamod-batch-hauling/spec.md)
and [cargo/personal-supply care](../../../../openspec/changes/specify-megamod-batch-hauling/specs/megamod-haul-cargo-care/spec.md).

## Historical source coverage and retained changes

| Behavior | Source evidence | Proposed replacement / status |
| --- | --- | --- |
| Multi-item collection | Native haulables, inventory queue, better storage, dynamic nearby search | **Specified:** physical mixed-item trip, same 12-cell/half-distance search rule, finite plan then unload |
| Normal inventory capacity | Native mass clamp at pickup; gear-only starting threshold | **Specified correction:** one combined mass plus inventory/hand volume budget, including personal supplies; actual quantities checked at pickup |
| Extra hand load | Native remainder haul can follow inventory pickup | **User-directed correction:** no second allowance; unload cargo before native single-load fallback |
| Planning arithmetic | Whole-stack mass estimate and excess-count final assignment | **Specified correction:** only positively allocated quantities, rechecked for actual space and provider limits |
| Corpse/animal/mech options | Corpses off; animals/mechs on, actual inventory/capability still required | **Specified:** same defaults, no new training/pack ability, ordinary fallback preserved |
| Start threshold | UI 20%, actual gear-only ratio excludes equality; 100% differs from tooltip | **Proposed correction:** actual occupied mass, inclusive boundary, positive capacity; common global/save model |
| Urgent hauling | Real Allow Tool work-giver adapter; urgent extras filtered; external cleanup can remove designation before delivery | **Specified:** direct S008 composition; optional original delegate plus owned-cargo urgency bridge, explicit cancellation and saved quantity lifetime |
| Storage | Slot cells/containers, interface capacity, queued reservations | **Specified:** real quantity/slot commitments, mixed shelves, current acceptance; S005/S049 direct policy composition |
| Cargo tracking | HashSet references; same-Def fallback after merge; unchecked transfer results | **Proposed correction:** exact successful quantities, partial transfer conservation, explicit personal/cargo portions |
| Unloading | Current best storage, 3-tick handling, 20-tick name-based special case, nearby drop on failure | **Specified:** native physical placement, 3-tick baseline, real provider handling, safe failed-drop state; no name-based delay |
| Interruptions/idle | Queued recovery hooks, including enumerable construction rather than proven delivery | **Specified:** native priority/interruptions, one recovery request, unloading before another owned load |
| Personal pickup | Broad native take permission override; no separate own keep schema | **Proposed scoped behavior:** capacity-limited Take and keep, explicit quantity release/drop, protected personal supplies |
| Gear display | Tracked item tint through first-white-color replacement | **Specified correction:** readable cargo/kept quantities and reason/action in existing Gear UI; no generic color interception |
| Save and map lifecycle | Reference collection and driver drop count | **Specified owned state:** conservation, stale-route invalidation, caravan/death/control-loss recovery; no automatic foreign schema import |
| Shared index | Repeated native-list copies/sorts and global temporary exclusions | **User-directed:** one shared Ctrl+F/count/hauling dataset; local route state and fresh action checks; optimization unmeasured |

Combined mass/volume, exact threshold semantics, finite trips, personal keep/release and mixed-stack
attribution are explicit proposals refining the user's intent. They are not falsely presented as
confirmed source parity. Ordinary heavy-item native hauling remains accessible after owned cargo is
unloaded; PH-Q1 records this capacity interpretation for review.

## UI, settings and dependencies

Actual preferences live in the ordered Hauling section of mod settings with common Global default,
Current save and Use global controls. New saves inherit; dormant overrides are retained; all save
mutations are disabled at the main menu. Corpse/race changes affect the next pickup, starting-space
threshold changes the next batch; draft-drop changes the next transition into drafting. Existing
cargo can still unload. Search radius/ratio, handling pause and retry delay are named constants,
not additional preference rows.

Take/keep/release/unload belong to contextual item/pawn Gear controls as entity state. No bottom tab,
debug override, bottom-right toggle or extra work category. Seven-language UI and actual workflows
remain unimplemented. Future S123 meal carrying must share protection and capacity accounting.

Required future dependency **Harmony**. No required original source, IHoldMultipleThings binary,
other repository product or Gateway. Owned Job/WorkGiver Defs are additive; saved owned state avoids
blanket pawn-comp XML injection. Any later XML Extensions operation makes that dependency required
and needs package/Def/node guards with merge preservation. Every actual patch requires a documented
semantic target and executable startup admission, tolerant of benign unrelated changes.

## Consumed contracts and compatibility boundaries

Actual consumers include Allow Tool's type-resolved work-giver factory; Keyz' package-gated binary
bridge; Kanban/Stack gap capacity hooks; While You're Up private fields and drivers; VMF map-sensitive
planning; SimpleBabyCarry's private unload toil; Shared Storage Access's generated pickup/reservation
hooks; and Immersive Chefs' original assembly/package-gated cargo adapter. The research table records
their actual signatures/surfaces. A literal scan is not proof against unknown dynamic consumers.

S125's selected opportunistic-hauling behavior composes directly with S021, not through patches
against our batch jobs. The modern port still consumes the legacy private family supplied by
Continued, whereas Reworked/Armful do not supply it. Its bounded intersection is audited; complete
S125 source/helper/UI and inbound-consumer analysis remains queued. External hooks must preserve
actual owned quantities/capacity/recovery; unknown protection withholds affected new batching.
The owned source set composes directly. Preserve feasible external public behavior through canonical,
shape-guarded adapters, beginning with the real Allow Tool delegate. A full source namespace/private
facade, original Def/save identity and package impersonation are not promised. Keyz 1.7 specifically
does not load its older PUAH bridge. Broader private/binary bindings remain unsupported until separately
resolved. Repository products remain independent; no runtime dependency on Immersive Chefs is added.

The external Allow Tool adapter also needs independent urgency tracking for our collected quantities:
its periodic cleanup and pre-placement removal cannot prove delivery. Held Gear cancellation and
existing loose-item cancellation must remain effective through drops and reloads. Delegate and
lifetime admission succeed together; otherwise new external urgent batching stays unavailable while
supported ordinary external hauling remains. This deliberately does not change unrelated source jobs.
For mixed loose stacks, the marker denotes only the recorded urgent quantity. Both owned batch and
supported single-load urgent pickup must respect that quantity; otherwise the mixed stack's urgent
work is deferred visibly. Cancelling part of held cargo therefore cannot make it urgent again when
the portions are dropped or merged. Exact fallback and transfer guards remain implementation work.

Historical Mehni CE helpers were stubs. Continued implements reflective weight/bulk/loadout handling,
with permissive fallback that we do not adopt. Actual provider-present behavior was not verified.
Our optional custom capacity/storage support
must prove real limits before batching; unknown means unavailable, never unlimited. Cross-map VMF
hauling and complete legacy-provider behavior are not initial supported claims.

Original PUAH, Continued, Reworked or Armful active means overlapping owned collection is withheld with bounded reason;
existing owned cargo stays recoverable. No automatic uninstall or foreign-save import. If safe
protection/recovery cannot be established, activation fails explicitly instead of losing cargo or
adding another load. Compatibility failures preserve supported ordinary hauling where safe.

## Remaining verification

PH-Q1–4 retain capacity interpretation, exact beta lifecycle, external contracts and provider/UI/
performance/native acceptance. PH-Q5's reference reconciliation is reviewed and validated; only its
five research tasks are checked. All 26 implementation/acceptance tasks remain unchecked. Required proof includes actual
mixed pickups and storage delivery, partial capacities, protected supplies, urgent lifetime, two-hauler
contention, interruptions/retry, settings scopes and supported provider runs on the reviewed build.
No log, patch list or direct state assignment proves those workflows. Batching changes travel effort,
not item costs/yields/research, and cannot grant hauling ability or an oversized load allowance.
