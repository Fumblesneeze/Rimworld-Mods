# Best Mix (Continued)

## Identity and provenance

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin replacement S059/P1-53, `Mlie.BestMix`, original pelador, maintained by Mlie,
[Workshop 2195986094](https://steamcommunity.com/sharedfiles/filedetails/?id=2195986094).
Inspected 2026-11-04; About/file 1.6.2, assembly 1.0.0.0, no advertised 1.7 support. Selected DLL SHA256
`1AD3F21A85C3D6C07ED490D21224FF0A28991396719F01FC7C97E781492F70C9` matches the bundled public DLL at
commit `05fe8ab8ed3ae184bf7d3569a46e18af60586da3`. MIT License.md and inherited CC BY-NC-SA description
both recorded; no source/code/art/assembly reuse selected.

[Research](../refinement/2026-11-04-best-mix-research.md) inventories all 14 behavior classes,28 source
modes, settings, inactive helpers, native 1.7 contracts and the real reflected UI consumer.
Status: analyzed, observable draft independently reviewed and strictly validated. Nothing implemented.
[Proposal](../../../../openspec/changes/specify-megamod-ingredient-preferences/proposal.md),
[design](../../../../openspec/changes/specify-megamod-ingredient-preferences/design.md),
[spec](../../../../openspec/changes/specify-megamod-ingredient-preferences/specs/megamod-ingredient-preferences/spec.md),
[unchecked tasks](../../../../openspec/changes/specify-megamod-ingredient-preferences/tasks.md).

## Behavior inventory

All rows are specified, not implemented or verified.

| ID | Intended behavior | Boundary |
| --- | --- | --- |
| MX01 | Bench default and per-bill inheritance/override | Entity UI/state, initially Default/inherit, no new bottom tab or surgery changes |
| MX02 | All meaningful source criteria plus true Default | Actual ingredient properties, visible direction/units/applicability; optional stats gated |
| MX03 | Current condition and current-temperature expiry | Finite expiry before no finite prediction; no permission to consume unsafe food |
| MX04 | Stock type totals | Spawned same-map units, configured radius/storage, distinct from pickup permission |
| MX05 | Preferred actual ingredients | Mixing values/rounding, sufficient no-mix types, no duplicate unit use or output changes |
| MX06 | Direct composition | Locks, S026 output criterion, explicit S059, S024 remaining ties, native ties |
| MX07 | Stable random stack order | Attempt scope, no UI reroll, committed portions persist |
| MX08 | Independent copies and shared linked default | Full choice transfer, explicit destination inheritance, unchanged started work |
| MX09 | Five common configuration rows | Global/save inheritance, main-menu protection, no dead source controls |
| MX10 |125 production bills | All supported mutation paths, default-on extension, lower limits preserve existing rows |
| MX11 | Proven external button; multiplayer deferred | Narrow callable facade for single-player, no initial synchronized-session claim |
| MX12 | Unavailable choices and source coexistence | Retain intent, hold affected new work, safe explicit recovery and bounded warnings |

## Deviations and direct composition

Default preserves the remaining ordinary/owned order; explicit Nearest remains. Source DIS/off can
still force distance over native mixing's value ordering. Owned Beauty uses current ingredient beauty
once, correcting the source's additional Stuff factor and inconsistent debug offset. HP-inapplicable
items remain neutral instead of damaged; unknown optional properties do not silently score zero.
Current-temperature Expiry stays distinct from Common Sense's intrinsic rot-progress hours.

S026 output-material choice precedes S059 ingredient choice; S024 breaks remaining ties and retains
its bounded linked-storage candidate addition. Secondary slots skip output comparison. Native early
success still limits search; no full-radius/map optimum. Quantity/rounding consequences remain real.
S014/S017 carry full inherited/dormant choices and one linked default; S021/S024 keep real capacity,
committed portions and output material across route changes. No internal patch stack or new cache.

Stock retains the source's spawned-map scope and does not claim to count every colony possession.
Container-only and pawn-carried items are excluded from this statistic even if native ingredient
search can use supported haul-source contents. Whole-map counting is not whole-map pickup permission.
The 125-bill limit is made coherent across production-list mutation paths; medical lists remain unchanged.

## UI settings saves and external contracts

Contextual bench/bill selectors and integrated bill-row action; no source bottom tab exists. Five
configuration rows use the common global/save path. Hidden legacy radius/debug/placement controls
and dormant helpers are omitted; any future diagnostic follows the top-debug-toolbar rule. New art
must pass the asset workflow; source icons/bundles are not reused by this document.

Nice Bill Tab Expansion actually resolves `BestMix.BMBillUtility.GetBillBmTex(Thing,Bill)` and
`SetBillBmVal(Thing,Bill)` by type name, without package/assembly checks. A narrow facade is selected
for this pair and the tooltip key, with source-assembly delegation when original owns behavior.
No full CompBestMix/settings/string-record facade or source-package impersonation. Bounded installed
search found no other executable consumer, not proof of universal absence.

Optional Soft Warm Beds softness needs package plus actual stat semantics. CE electric protection
is deferred. Source Multiplayer setters are synced but its patched random helper is unused by actual
Shuffle. A future separately scoped Multiplayer adapter would need synchronized actual decisions and
host-effective settings; no such session is admitted or implemented in initial support.
No source helper DLL, safe-uninstall guarantee or automatic foreign-save/settings migration.

## Coverage and verification

**Nothing implemented.** The remaining work includes per-patch target documents, current native/
provider contracts, honest RED/GREEN, languages/assets, package review and personally observed bill,
crafting, ingredient/cargo, settings/copy/capacity and optional-provider workflows on a reviewed build.
MX-Q1–4 in the [ledger](../refinement/specification-progress.md) retain selected UX/meaning, shared
stock scope, external/multiplayer compatibility and finalized balance/acceptance. No game or build
is claimed by this documentation draft.


Initial support is single-player without Combat Extended. Source CE/MP adapter details are retained research only; their adapters, controls and acceptance are deferred to a future scoped change.
