# Original: whole-list workbench linking

## Identity and provenance

- Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
- Origin: **original**, S014 / P1-11. No upstream source package, Workshop ID, source license or
  third-party link schema is adopted. The user confirmed the **entire bill list**, including add,
  remove, order and every bill's configuration, in the [refinement interview](../refinement/2026-11-01-handwritten-notes.md).
- [Native research](../refinement/2026-11-02-workbench-linking-research.md) records the evidence and
  proposed refinements. Inspected 2026-11-02, user documentation first, then local read-only native
  1.7.4995 rev309 types. No source-mod documentation was invented for this original feature.
- Input `F:/Steam/steamapps/common/RimWorld/Assembly-CSharp.dll`, SHA256
  **56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21**. Offline decompilation under
  ignored `artifacts/Refinement/megamod-spec-20261102/workbench-linking/game`; no game launched.
- Native evidence: Bill/BillStack, production and unfinished/autonomous/mech families, job/recipe
  toils, UnfinishedThing, worktable lifecycle, bill UI, worker and product counters. This is structural
  evidence for proposed behavior, not proof of live compatibility or permission to bundle game code.

## Behavior inventory

All rows are **specified, not implemented or verified** in
[the owning change](../../../../openspec/changes/specify-megamod-linked-workbenches/).
The [capability](../../../../openspec/changes/specify-megamod-linked-workbenches/specs/megamod-linked-workbench-bills/spec.md)
contains observable requirements; the design contains technical research and candidate seams.

| ID | Player action → intended result | Defaults / edge cases | Evidence and owning requirement |
| --- | --- | --- | --- |
| WB-01 | Link compatible same-map stations → one named whole list | New benches unlinked; no member-only rows; different kinds allowed when every chosen bill is supported | Confirmed whole-list intent; A workbench group |
| WB-02 | Select source and confirm join/merge preview → destination whole list replaced | No concatenation or recipe-name merge; entire destination group; canceled/failed operation unchanged | Proposed membership refinement; Linking uses contextual controls / Membership replacement |
| WB-03 | Edit/add/delete/reorder a row → same choice/list at every member | Duplicate recipes retain distinct identities; 15 logical rows initially; one Add creates one logical row | Native Bill/Stack/UI; Every logical bill edit |
| WB-04 | Browse Add bill on a member → common group catalog with existing material/inspection behavior | Hide is only browsing; structural support loss retains unavailable row; power/fuel lack stays local | S009–S012 + native availability; Recipe browsing |
| WB-05 | Several workers produce Do X times → X iterations total | Shared finite remainder includes committed work; batch output counted per iteration; no quota taken by queries | Native completion and physical job ownership; Finite repetitions / Changing repeat choices |
| WB-06 | Target production reaches thresholds → common count and pause/resume | Count native map/storage scope once; equipped/quality/HP/material filters retained; ordinary in-flight overshoot | Native WorkerCounter/production state; Target-count mode |
| WB-07 | Continue unfinished or special production → real local state survives | No copied materials, creator, mechanitor, subject, forming time or free quality | Native UFT/autonomous/mech/job types; Shared choices do not copy physical work |
| WB-08 | Edit/delete while working → intended new-work choice or native cleanup | Reorder/rename does not restart; committed materials survive filter edits; deletion is not automatic item cancellation/refund | Native toils, Delete notification and UFT cancel; Editing and deleting |
| WB-09 | Leave/dissolve → independent complete lists with explicit finite allocation | Commitments follow physical work; unstarted quota defaults to continuing group or selected dissolve bench | Proposed non-duplicating separation; Leaving and dissolving |
| WB-10 | Move/rebuild/remove/reload → meaningful membership and recovery | Same-Thing identity; exact reconstruction handoff; map transfer suspended; no fresh-cell inheritance | Native map/member/reference lifetimes + S013; Movement / Missing references / Save |

## Interactions and composed result

- **S009–S012 Add bill browser:** common group catalog, one explicit logical addition, one chosen
  material copied consistently. Contextual details still use the opened bench for physical facts.
  Recipe hiding does not delete or suspend a shared production row.
- **S005 linked storage:** count/output scopes retain the actual selected storage group. Do not
  sum the same stock once per workbench or create a second storage aggregation/cache here.
  If that storage group disappears, S014 retains the unresolved choice and blocks new starts;
  S005's native invalid-destination fallback is scoped to ordinary unlinked bills.
- **S013 quality construction and later S028 Replace Stuff:** preserve membership through an exact
  supported replacement handoff. Sharing a cell is insufficient evidence that a new object is the
  former member. Physical work must follow the ordinary replacement lifecycle, not be cloned.
- **Later S017–S019 bill modifiers:** BWM, Everybody Gets One and Most Skilled Bill Restriction
  still get their own sequential analysis/change. Their final adopted choices belong to the shared
  row directly; no internal Harmony stack. This report does not claim their behavior or a conflict.

## UI audit and relocation

| Entry point / intent | Classification | Destination and navigation contract |
| --- | --- | --- |
| Create/Join/Merge/Leave/Dissolve/Rename/Select members | Workbench gameplay state | Contextual selected-bench/Bills controls with group name/member scope visible |
| Existing bill row/detail choices | Bill gameplay state | Existing native or composed bill view, clearly marked as applying to the group |
| Whole-list overwrite and finite allocation preview | Consequential gameplay action | In-context preview before commit; cancel preserves prior state; no general mod-settings page |
| Current work, quota, unavailable reference/support | Gameplay status and recovery | Linked row/group status and contextual explicit Resume or replacement choice |
| Bottom tab / global toggle / actual configuration | None required for this feature | No added bottom tab and no inherited setting for a particular group |

New benches are unlinked. Common catalog, shared finite quota, idle-destination overwrite guard and
explicit split allocation are **proposed refinements**, not additional interview confirmations.
No standalone mod setting is introduced, so the common settings path has no new rows or inheritance
exceptions here. Any future actual preference must use that path and its main-menu safeguards.

## Contracts consumed by other mods or saves

There is no pre-existing megamod linking API/save schema or original-provider identity to preserve.
Native consumers already depend on real Bills and physical work; keep those contracts explicit:

| Consumer / native evidence | Contract | Planned preservation / remaining verification |
| --- | --- | --- |
| BillStack and native UI | Each Bill owns one actual stack/giver; Delete notification differs from Clear; row identity separate from recipe | Stable native per-member Bills mapped to logical rows; no shared billStack pointer or periodic recloning. WB-Q1 |
| JobDriver_DoBill and Toils_Recipe | Concrete bill/bench, reservations, physical ingredients, completion notification and output hauling | Group quota coordinates real admission/completion; queries do not claim work and collection does not double count. WB-Q2 |
| UnfinishedThing / Bill_ProductionWithUft | One reciprocal bound bill/physical item, actual creator/materials/work; native cancel has its own recovery | Preserve exact objects; deletion does not manufacture a refund; group transitions reconcile once. WB-Q2/Q3 |
| Autonomous/mech worktables and bills | Local active process, contents, subject/mechanitor/bandwidth/timing; native delete/eject lifecycle | Preserve local state and support remaining collection at zero new quota. These families remain intended scope. WB-Q2 |
| Bill reference validation / save state | Native can clear invalid pawn/storage and deep-load member Bills | Owned recovery retains unavailable choices and reconciles before work; no silent scope broadening or independent quota copies. WB-Q1/Q3 |
| Future selected or external bill modifiers | Their concrete fields/callbacks not yet analyzed in this sequential turn | Direct composition for owned choices, narrow proven external adapters later; no speculative facade or package impersonation |

No import from BWM or another provider's links is promised. A future migration needs backup, atomic
conversion, failure restoration and verified postconditions. Removing the product from a linked save
is not declared safe without an explicit dissolve/export recovery workflow.

## Implementation and coverage

**Nothing is implemented.** Every WB row lacks runtime code, semantic target documents/validators,
focused RED/GREEN and final native acceptance. All implementation tasks remain unchecked.

Intentional departures from naive native copy/paste include a whole-list group instead of independent
copies; finite quota shared across members and explicitly allocated on separation; one hysteresis
state; common recipe catalog; retained invalid choices instead of silent Any worker/Drop on floor;
and startup suspension when required support is unavailable. Independent physical jobs and native
production economics remain intact. Exact special/provider paths still need admission/acceptance.

There is no original provider to disable by name. External linking/bill adapters need their actual
contract before a coexistence policy is claimed; unsupported consequential writes cannot silently
produce divergent lists. Guarded native/provider support must allow benign foreign changes. Failed
group support suspends affected saved groups before replicated native copies can multiply orders.
Optional absence must not disable unrelated native groups. No dependency is added by these documents;
eager Harmony or XML Extensions use must add the corresponding required metadata at implementation.

Whole-list destination replacement is explicitly intended and previewed. Static Def additions, if
needed later, must use appropriate XML Extensions guards and preserve unrelated entries. Shared
stock facts use native listers/counters or the common map-owned data, not independent member caches.

## Verification and acceptance

**WB-Q1–4** remain in the [progress ledger](../refinement/specification-progress.md). Source research
does not accept gameplay. Independent document review and strict schema/link checks are separate
from later implementation tests and native player evidence.
Review found one storage-fallback precedence conflict, now resolved in both owning drafts. Additional
focused scenarios preserve credited-result collection after separation and define single ownership
when a bench moves without its unfinished item. Browser drafts now explicitly respect the common
catalog, including their ordinary fallback. Exact runtime seams remain WB-Q1–3.

Future acceptance covers actual create/join/merge/edit/add/produce/separate actions; ordinary,
unfinished, autonomous and mech work; finite/target counts and simultaneous jobs; native materials,
work and output; failed/canceled operations; stale UI; movement/destruction/reconstruction; invalid
references; save switching and admission failure/recovery. Use focused honest tests, exact package
checks, independent code review and a fresh minimized isolated Gateway-controlled game on the
reviewed build. Personally inspect causal before/action/after evidence and retain exact build/mod
list, logs, configuration hashes and cleanup. Seven-language UI and measured shared-data/membership
cost remain required; no optimization claim follows from a proposed architecture.

Semantic intent: recipe costs/yields, research, work, experience, quality and machine time remain
native. Grouped finite orders avoid accidental multiplication; target mode still allows normal
in-flight overshoot. Splitting allocates uncompleted finite work explicitly rather than silently
duplicating it. Real playability and balancing acceptance remain open.
