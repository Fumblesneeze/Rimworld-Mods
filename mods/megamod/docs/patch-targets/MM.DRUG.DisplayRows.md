# MM.DRUG.DisplayRows: alphabetical drug-policy navigation

## Ownership and traceability

- Owner: `fumblesneeze.megamod` (`mods/megamod`); S029, P0-C.
- Contract: [Alphabetical navigation does not change drug priority](../../../../openspec/changes/specify-megamod-drug-policy-maintenance/specs/megamod-drug-policy-maintenance/spec.md), especially Find and edit a drug, Two eligible scheduled drugs, Two carried-supply deficits, and Display changes during editing.
- Source evidence: [S029 research](../refinement/2026-11-02-drug-policy-fix-research.md).
- Kind: narrowly scoped transpiler at the native row read; no owned-method patch.
- **State: target design only, not registered or admitted.** Implementation, executable validator,
  Harmony fixtures, and native scenario registration remain pending. This document cannot enable a patch.
- Read-only current evidence: `artifacts/Megamod/P0-C/native-1.7.5000/RimWorld.Dialog_ManageDrugPolicies/`;
  assembly SHA-256 `D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254`.
  The hash identifies inspected input and is not an admission predicate.

The native window has no public row-enumeration override. XML cannot change its runtime row
projection. The adapter must leave `DrugPolicy.entriesInt`, ordinary policy controls, and
`DrugPolicyEntry` instances intact; sorting the stored list or swapping it temporarily is forbidden.

## Target and minimal predicates

Target is private, non-static, non-generic
`void RimWorld.Dialog_ManageDrugPolicies.DoPolicyConfigArea(UnityEngine.Rect)` in Assembly-CSharp.
It consumes the private non-static `void DoEntryRow(Rect, DrugPolicyEntry, int)` and native
`DrugPolicy.get_Item(int): DrugPolicyEntry` / `get_Count(): int` callable shapes.
Admission occurs after game assemblies are available and before this window can draw.

The executable validator must establish all of these in the **incoming transpiler stream**:

1. One supported row loop uses a selected `DrugPolicy` instance, its count, and one integer row index.
   That same index controls the row's vertical placement and the corresponding `DoEntryRow` call.
2. The loop reads that same policy via `get_Item(int)` and passes the resulting entry to
   `DoEntryRow(Rect, DrugPolicyEntry, int)`. The row getter is not an unrelated policy read or a
   lookup used by permission/job logic. Exactly one consumed relationship must be identified.
3. The replacement read preserves the stack shape and supplies an existing entry from the exact
   selected policy. It cannot change the policy count, row values, or row membership. The projected
   sequence must have the same reference-identity membership and count as the current native list.
4. The draw adapter can identify policy/catalog/language changes before committing GUI edits.
   Reusing a screen index is not sufficient; pending edit state stays with its entry or is cancelled
   before a changed projection is used. If the native focus path cannot meet this, presentation is
   unavailable rather than claiming identity-safe editing from a sorted list alone.

The transformer replaces only the proven row getter with the owned display read. Preserve labels,
exception blocks, unrelated instructions, row geometry, native row controls, and entry buffers.
Validate the whole edit before producing a changed stream; absent or ambiguous relationships return
the original stream unchanged. Local numbers, offsets, unrelated extra calls, and foreign owners
alone do not cause rejection.

## Composition, ordering, and invalidation

This patch sees the actual incoming stream every time Harmony builds the replacement. An earlier
transpiler that changes unrelated drawing is compatible when the consumed relationship remains.
A prefix that skips the entire native renderer also skips this projection; it cannot be advertised
as owned alphabetical presentation on that draw. A foreign renderer that reads rows differently
needs its own proven adapter, not a broad compatibility claim.

The C01 admission owner must coordinate supported later repatches. Every invocation revalidates
the incoming stream; failure invalidates the presentation capability and preserves native drawing.
Do not execute foreign transpilers as speculative probes. Runtime projection reuse is scoped to
the exact policy, current entry identities/order, catalog revision, and displayed language. Inactive
or stale projection falls back to native order and cannot change gameplay priority.

Native drug-taking/refill code keeps reading the stored list. Row projection cannot sort policies,
change which policy is default, reset a drug-taking record, or apply a preset. Maintenance remains
independent of whether presentation can be admitted. No XML behavior is involved.

## Admission, failure, and diagnostics

Register once through C01. Active canonical `Mlie.DrugPolicyFix` suppresses this owned overlap; mere
installation does not. Preserve its patches and settings. Suppress only this presentation group
when the target or focus contract is unknown; S029 maintenance can remain available.

Discovery/validator/install exceptions, missing targets, or ambiguous relationships leave this
patch inactive. Roll back only newly installed owned patches in this presentation group. A changed
runtime row set falls back without indexing a stale projection. Never repeatedly log per draw.

Startup warning shape:
`[fumblesneeze.megamod] Patch MM.DRUG.DisplayRows not applied; target=Dialog_ManageDrugPolicies.DoPolicyConfigArea(Rect); targetSpec=MM.DRUG.DisplayRows; predicate=row-getter-to-draw; reason=unsupported relationship; unavailable=alphabetical drug-policy navigation; fallback=native row order`.

## Verification matrix and final observation

| Case | Expected result | Honest tier | State |
| --- | --- | --- | --- |
| One proven row read/draw relationship | One replacement; reference membership preserved | Harmony structural fixture | Pending |
| Unrelated instructions, changed local indices, compatible foreign transpiler | Admitted; foreign owner retained | Harmony fixture with guaranteed owner cleanup | Pending |
| Different policy/index, missing draw, two ambiguous loops | Incoming stream unchanged; bounded warning | Harmony fixture | Pending |
| Repeated registration, validator/install failure, supported repatch | No duplicate hook or partial group | C01 Unit/Harmony | Pending |
| Equal labels, translated labels, native-first differs from alphabet-first | Correct native row edited; native job priority unchanged | Unit projection then isolated native workflow | Pending |
| Policy/language/catalog change during numeric input | Same entry retains input or removed input is cancelled | Isolated native workflow | Pending |
| Original provider active | Owned presentation absent; foreign behavior untouched | Isolated exact-provider workflow | Pending |
| XML list merging | Not applicable: no XML mutation | — | Not applicable |

Planned native scenario `megamod.drug-policy-display-priority`: arrange two ordinary accessible
drugs whose inspected native priority differs from displayed alphabetical order, with one eligible
pawn and no dose history confound. Open the native Assign drug-policy control and its management
window, configure both rows through real widgets, and capture the visible alphabetical list before
closing it. Observe the pawn's first native scheduled-use/refill job and physical chosen drug.
Capture before/action/after frames and compare with a run/configuration without opening the window.
Changing rows directly in setup cannot serve as evidence that the displayed controls edit correctly.
Repeat after ordinary save/load with equivalent eligibility and dose history. Setup, exact control
driver, runnable test registration, and final execution are still pending; this prose is not an
executable scenario or acceptance evidence.
