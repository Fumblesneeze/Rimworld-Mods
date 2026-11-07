# Hauling reference reconciliation and selected behavior

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Decision date **2026-11-03**. S021 PH-Q5 reconciliation, following separate documentation/source
audits. Specification only: no source build, product implementation or native acceptance.

## Evidence and scope

| Reference | Evidence read | Role |
| --- | --- | --- |
| Pick Up and Haul - Continued, 3829549663, `Zzyxzz.PickUpAndHaulReworked` | [17 source files and selected installed 1.7 bodies](2026-11-03-pick-up-and-haul-continued-research.md) | Primary S021 behavior reference |
| Pick Up And Haul - Reworked, 3813653998, `Dreadarm.PickUpAndHaul` | [Guide/API and all 44 decompiled behavior/API/support files](2026-11-03-pick-up-and-haul-reworked-research.md) | Alternative quantity/recovery/loading design |
| dd.Armful Haul, 3808100512, `dd.armfulhaul` | [All 17 source files and selected installed bodies](2026-11-03-armful-haul-research.md) | Alternative native job/hand-stack design; selected visible cargo S126 |
| While You're Up 1.7, 3832180338, `patch1.7.whileyoureup` | [Current private PUAH bindings, relevant routing/recovery/state and native intersection](2026-11-03-while-youre-up-hauling-intersection.md) | Narrow S125 composition prerequisite; full feature deep dive remains queued |
| Mehni PUAH, 1279012058 | [Earlier exact source/native/consumer audit](2026-11-02-pick-up-and-haul-research.md) | Historical provenance, not evidence that a newer fork retains a defect |

Each audit pins its exact files/hashes or revision. A modern name, public performance claim, a
matching member or static inspection does not establish runtime compatibility. The bounded While
You're Up intersection does not complete S125, and the transport observations do not complete
S133 Bulk Load For Transporters or S135 Basic Caravan Loading.

## Chosen composition

Use **one owned inventory-based batch and quantity model**, with native physical movement,
pickup and placement. Preserve Continued's broader eligible worker set and ordinary nearby-trip
shape; use exact quantity accounting informed by Reworked; deliver by grouped storage visits
informed by While You're Up; present actual moving cargo through S126's Armful-inspired view.
Do not choose an entire fork or layer their patches. Armful's useful visuals do not require its
expanded hand container or separate per-stack allowance.

This is a deliberate selection of behavior and boundaries, not copied implementation. It keeps
personal/urgent quantities, native transfer success and one shared dataset authoritative across
ordinary, urgent, opportunistic and later loading consumers. Required external contracts receive
narrow verified adapters where practical; broad private compatibility is not fabricated.
Owned S008 and the supported external urgent adapter share exact urgent quantities and held/loose
cancellation, including ordinary fallback pickup. A merged stack's visible mark cannot make its
ordinary or cancelled units urgent. The S008 capability is aligned explicitly during reconciliation.

| Concern | Selected behavior | Difference / reason |
| --- | --- | --- |
| Workers | Player humanlikes and natively capable animals/mechs with usable inventories/capacity; no new training or work column | Retains Continued's breadth; does not adopt Reworked's ordinary humanlike-only scope or Armful multipliers |
| Hard limits | One actual mass plus proposed combined carrying-volume allowance, gear/personal inventory/hands counted correctly | Reject Continued's 1.3 factor/separate remainder, Reworked's extra hand allowance and Armful's full limit per stack; PH-Q1 retains native measurement/playability work |
| Start threshold | 20% free actual combined mass, inclusive; 0 still requires room, 100 means empty burden | Same user-facing primary default, corrected gear-only/equality behavior |
| Search | Finite plan; radius max(12 cells, half seed-to-destination distance), nearest next eligible pickup, stable ties | Retains Continued's ordinary search shape; rejects its repeated 12-cell re-collection and Reworked's different radius/beyond-area defaults |
| Destination space | Exact current quantities/slots and incoming commitments; linked settings do not duplicate physical room | Native rechecks and S049 limits supersede source whole-cell/per-Def shortcuts |
| Ownership | Actual accepted cargo/kept/urgent portions; preserve material/quality/condition and holder through split/merge | Reworked-inspired precision; no stale-reference same-Def substitution, including the one reintroduced by modern While You're Up |
| Protected goods | Current cart/holder/reservation/loadout/medicine/sidearm authority respected; unknown provider ownership fails only its affected candidates/routes | Continued's cart/CE boundaries inform intent; no permissive missing-comp or type-name-only authorization |
| Delivery order | Group by physical shelf/stockpile/container; nearest reachable valid delivery cell by straight-line distance; finish valid group portions before next destination | While You're Up-derived, with exact per-item capacity and continued access checks; no shortest-route or single-drop promise |
| Handling time | 3 simulation ticks per unloaded portion plus actual provider handling | Retains primary ordinary delay; drops name-based 20-tick exception and invents no speed bonus |
| Failed recovery | Retain physical cargo; one pending recovery; automatic retries wait 600 simulation ticks for an unchanged problem; relevant change or explicit safe retry can act earlier | Reworked-derived interval, generalized consistently; no source failed-drop tracking erasure, immediate looping or frame-rate dependence |
| Drafting | Stop pickup and retain cargo by default; **Drop hauled cargo when drafted** optional, initially off | Intentional deviation from Continued's on default preserves existing proposed safe retention, while keeping an explicit safe-drop choice; never discard kept goods or lose failed drops |
| Preferences | Corpse batching off; capable animals/mechs on; 20% threshold; draft-drop off | Five common global/save preferences, effect timing and inheritance specified; no capacity inflation option |
| Manual keep | Contextual actual-fit Take and keep; quantity release/drop/retry | Retains useful manual availability without global unlimited permission or beta home-menu assumptions |
| Opportunity composition | Owner supplies item/destination/whole-trip bounds and parent job; S021 adds capacity/conservation constraints | S125/S024 do not schedule duplicate detours or recursively postpone work; route rejection does not become unrestricted batching |
| Visibility | S126 actual held-stack pictures and contextual individual details | Required regardless of storage model; no new bottom tab |
| Shared data | One map-owned item-fact view used by Ctrl+F and hauling; pawn eligibility rechecked | No source-private copied caches or static call-stack authority; performance remains unmeasured |
| Co-installation | Active competing PUAH/Continued/Reworked/Armful collection withholds owned overlap; recover existing owned cargo safely | Specific competing implementations, not blanket rejection of foreign patches; no automatic uninstall |
| External opportunity hooks | Admit only if actual owned cargo, capacity and recovery boundaries can be preserved | No legacy HashSet facade over mixed quantities; if protection cannot be established, affected new batching stays unavailable with warning |

All newly chosen timing/default values appear in the observable spec. The 600-tick retry delay is
a named behavioral constant, not an added player setting; expose a setting later only through an
explicit contract change. The draft-drop option uses the same common settings row and affects a
future transition into drafting, not a pawn already drafted. Its safe drop preserves outstanding
urgent quantities, and a failed drop retains cargo. No automatic bulk dropping while an emergency
is being handled is inferred from this preference.

## Deliberate omissions and separate owners

- Do not import any source DLL, namespace/assembly impersonation, job/save schema or settings file.
  Continued preserves some old names; Reworked intentionally changes them; neither implies our
  binary compatibility. Existing consumer tables remain provenance, with concrete gaps retained.
- Reworked's ground-stack merging, unrestricted area scanning, native-retention implementation,
  transporter/caravan helpers and orphan-cargo relocation tool are references, not blanket adopted
  S021 features. Exact cargo protection is selected; unrestricted repair actions, if ever selected,
  belong in developer controls. No new repair cheat is added now.
- Armful's construction/bill native toil wrapping, hand-stack multipliers, floor/port adapters and
  prison-food commands are not silently adopted. Its source audit records them for later explicit
  decisions. Existing S024 supply collection and later S125/S133 loading contracts determine those
  workflows; selecting visible cargo alone does not change construction or prison authority.
- While You're Up's optional 1–10 mass multiplier is not adopted. Its private destination cache,
  per-Def authority, temporary storage-priority changes and automatic Common Sense setting edits
  are replaced by direct owned policy/quantity calls where their behavior is selected. Full route
  thresholds, filters, source UI and all nonintersection behavior remain S125 work.
  S125 must explicitly specify any equal-priority supply-relocation exception and reconcile it
  with S021's ordinary better-storage rule before that behavior can be implemented; this comparison
  does not silently authorize equal-priority moves in ordinary batches.
- S133 and S135 must specify actual loading requests, cancellation, capacity and work-category
  behavior in their own changes. S021 supplies shared quantities/capacity/recovery; it does not
  grant complete loading parity from a narrow comparison. S122 restores crafting-speed stats and
  does not change Work-tab categories (corrected after its 2026-11-06 source inspection).
- Exact Combat Extended, custom storage, cart, sidearm/medicine and urgency adapter implementations
  remain PH-Q2–4. The specs define required safe observable outcomes; no source-specific adapter is
  called verified or silently assumed to handle unsupported providers.

## Research completion versus implementation

This decision record and revised S021 proposal/design/spec/tasks resolve the newer-reference
selection for the **specification draft** after independent document review and strict
validation. PH-Q1's explicit volume interpretation and PH-Q2–4's runtime/consumer/acceptance work
remain; they are not replaced by source hashes. S125's full analysis is still queued as WY-Q1.
Only research/reconciliation tasks can close from these documents. All coding, guard, test,
package and reviewed-build native acceptance tasks remain open.

Semantic check: every delivered item must physically arrive; batching reduces repeated travel
without manufacturing resources, inflating capacity or granting work permissions. Pending bill,
emergency, urgent-order, native ownership and storage restrictions remain authoritative. Source
performance claims and static safety checks do not replace observed jobs or measured data cost.

Independent source review found no issues and confirmed the three While You're Up input hashes.
Independent behavior review identified an ambiguity between S021's external-adapter quantity rules
and S008's owned loose-stack wording. Both observable capabilities now explicitly preserve exact
urgent/cancelled portions and quantity-limited batch/ordinary pickup; the focused resolution review
found no remaining issues. Typed `openspec_validate` passed **52 items, 0 failed**, and focused
strict S021/S008 validation passed. Local links and whitespace passed for the 16 reviewed documents.
S021 now has **2 capabilities, 16 requirements and 69 scenarios**. Its five research/reconciliation
tasks are complete; **26 implementation/acceptance tasks remain unchecked**. No runtime files,
package, build, install, game launch or normal user configuration were changed by this reconciliation.
