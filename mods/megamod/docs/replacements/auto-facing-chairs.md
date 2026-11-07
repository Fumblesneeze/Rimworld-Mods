# Auto-Facing Chairs replacement

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: replacement, **S048 / P1-42**, with user-required same-cell manual-rotation control.
Status: **source/native analysis and observable draft independently reviewed and strictly validated**.
No implementation, reproduced source bug or native/provider compatibility acceptance.

## Provenance and contract

**Auto-Facing Chairs**, scarf, `scarf.autofacingchairs`,
[Workshop3783758781](https://steamcommunity.com/sharedfiles/filedetails/?id=3783758781), About1.1.1,
advertises1.6, requires Harmony and FSharp.Core. Published
[source](https://github.com/scarf005/rimworld-AutoFacingChairs) carries AGPL-3.0. Installed informational
revision identifies the preceding1.1.0 commit while inspected facility behavior matches current1.1.1;
[research](../refinement/2026-11-03-auto-facing-chairs-research.md) pins hashes, exact discrepancy,
complete source/decompilation, native1.7 seams and bounded consumers. Do not equate version metadata
with exact behavior or beta compatibility. No source code, runtime assembly or art is copied.

[Proposal](../../../../openspec/changes/specify-megamod-furniture-orientation/proposal.md),
[design](../../../../openspec/changes/specify-megamod-furniture-orientation/design.md),
[observable contract](../../../../openspec/changes/specify-megamod-furniture-orientation/specs/megamod-furniture-orientation/spec.md)
and [tasks](../../../../openspec/changes/specify-megamod-furniture-orientation/tasks.md) own the work.

## Behavior coverage and deviations

| Behavior | Selected result | Implemented / missing / deviation |
| --- | --- | --- |
| AF-01 placement scope | Rotatable seats/facilities, built/planned/frame targets | Nothing; extend source build-only hooks to actual native install/reinstall |
| AF-02 seat suggestions | Actual chair-use marker including reverse/multiple positions, then eating-surface side | Nothing; complete current geometry coverage, not arbitrary target-name lists |
| AF-03 facility suggestions | Placing facility toward accepted target, native geometry and source same/opposite preference | Nothing; preserve removal of reverse bed rotation, no guaranteed facility bonus |
| AF-04 conflicting targets | Intersect all relations, leave rotation on conflict, keep allowed current value, cardinal tie order | Nothing; no nearest-target override or duplicated multicell influence |
| AF-05 manual control | One suggestion per cell/session; manual input wins until a new cell/session | Nothing; explicit user requirement, already partly present in source rather than a proven source bug |
| AF-06 Chairs only | False default, common global/save controls, next-cell timing | Nothing; replaces source global-only configuration |
| AF-07 native result | Shown facing survives valid native order and completed build/install | Nothing; ordinary cost/work/eligibility/material and saved orientations remain native |
| AF-08 compatibility | Semantic admission, original suppression, intact native fallback and bounded warnings | Nothing; no source PatchAll, silent missing field or blanket provider claim |

The source has one settings checkbox and native placement behavior; no bottom tab/debug command/
operational toggle. Keep Chairs only in structured mod settings with global/save inheritance,
dormant overrides and disabled main-menu save controls. Use native rotation keys/buttons and
Architect/Install/Reinstall for gameplay. Cursor state is transient; placed orientation is native
entity state and is not a per-save settings row.

## Composition and external contracts

S036 Architect browsing preserves the selected command and rotation; browsing must not reset the
same-cell session. S028 replacement construction retains its own native validity/material policies.
S129 Interaction Spot On X remains queued: its eventual actual-position contract must compose through
one directly owned resolver, not a Harmony patch against our orientation code. No speculative provider
API or guarantee for arbitrary moved interaction cells is claimed before that analysis.

Native build/install/reinstall preview, order and real construction/logistics remain the authoritative
workflow. Moving an existing item keeps its identity, material, quality, condition and style; this
feature provides no free item or construction. Facility-facing is geometric assistance, while native
LOS/medical/activity/link limits still decide the actual benefit.

Bounded installed DLL scans found only the source assembly for its namespace/package/Harmony owner;
no outside executable consumer was found. No package spoof, source API facade, automatic settings
import or saved-object migration. Existing furniture/blueprint rotations are not swept on load.
Any later source settings import needs explicit backup, atomicity, rollback and verified postconditions.

Core and Harmony required when implemented; DLC/furniture packs optional. Independent owning C# does
not require source FSharp.Core, XML tags or XML Extensions. Original `scarf.autofacingchairs` suppresses
the coherent owned feature, including install/reinstall assistance, with bounded startup diagnostics.
Foreign behavior remains in charge and is not guaranteed repaired. Compatible native-style furniture
and unrelated patches are allowed; every actual owned hook still needs its semantic target spec.
No source/private framework/Gateway DLL or repository product dependency.

## Verification and remaining work

AF-Q1–4 retain exact input/preview/order admission, complete geometry/query boundaries, selected-source
and settings/lifecycle composition, localization, focused tests, independent code review and native
acceptance. All implementation tasks are unchecked. The install extension and manual-input ordering
need their own causal proof; source inspection or a final ghost screenshot does not establish them.

Final minimized isolated runs must show real hover/rotate/place/completed construction and native
install/reinstall, same-cell motion, conflict, settings/save switch and exact provider/fallback cases.
Inspect actual facility outcomes separately from suggested facing. Retain personally inspected
before/action/after views, exact reviewed package, ordered mods, supporting logs, normal-config
protection and cleanup. No balance/compatibility/performance acceptance from this documentation.
