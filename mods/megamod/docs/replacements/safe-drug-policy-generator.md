# Safe Drug Policy Generator — S139

## Identity and provenance

- Owner: Make Rimworld Great Again (`fumblesneeze.megamod`, `mods/megamod`), target 1.7 beta.
- Origin: replacement **idea-only**. Keep explicit policy suggestions; do not port the algorithm.
- Source: SeoHyeon, [Workshop 2582640021](https://steamcommunity.com/sharedfiles/filedetails/?id=2582640021),
  `seohyeon.bestdrugpolicy`.
- Inspected 2026-11-08: `F:/Steam/steamapps/workshop/content/294100/2582640021`, selected
  `1.7/Assemblies/BestDrugPolicyGen.dll`, SHA256
  `E66B669C0644A75E72443849939389707AD734300C81265B839A557E11B22889`.
- Supplied MIT source, copyright 2025 SeoHyeon; no source/binary/art/translation copied. Exact
  source/native hashes, public links, UI and limitations are in the
  [research](../refinement/2026-11-08-safe-drug-policy-generator-research.md).
- Status: drafted workflow/invariants; calculation and creation-contract research block affected
  implementation. No build, tests or native acceptance. User-reported historical fragility is not
  a proven defect in this revision. Static observed reset/Beer/default-permission issues inform
  independent design, not original compatibility or bug-parity work.

## Behavior inventory

Scenarios belong to the [generation capability](../../../../openspec/changes/specify-megamod-drug-policy-generation/specs/megamod-drug-policy-generation/spec.md).

| Behavior ID | Native input/action → observable result | Options/defaults and edge cases | Evidence | Owning scenario | Status |
| --- | --- | --- | --- | --- | --- |
| DG-01 | Generate → preview → Cancel/Create | Explicit native window action; no startup generation or mod-settings Create | Source idea; proposed owned flow | Inspect and cancel; No available drugs | Drafted |
| DG-02 | Current drug → explained suggestion or unknown | No autonomous routine suggestion for unsupported behavior; no safety guarantee | Research helper limits; DG-Q1–3 pending | Unsupported drug behavior; Shared chemical or different pawn needs | Research-blocked |
| DG-03 | Inspect row → explicit medical permission | S029 normalized native defaults, S138 Any; never source accidental false | Source/native entry difference | Addictive drug without a suggestion; Suggested schedule retains separate medical choice | Drafted |
| DG-04 | Edit/reset draft → complete final values | Return to 100%/Any truly resets; discard edits only after reset confirmation | Source reset finding; proposed correction | Lower a limit and restore its unrestricted value; Reset an edited draft | Drafted |
| DG-05 | Change inputs → stale preview refuses Create | Same save/current inputs; S138 off/unavailable forces refreshed/created choices to Any, retains stored preference and never activates a hidden limit later | S029 lifecycle; proposed contract | Drug content changes while preview is open; Save changes or tolerance support disappears; Numeric preference while tolerance support is off | Drafted |
| DG-06 | Confirm → one new complete policy or no change | Current native identity; no partial append/duplicates/false success | Native factory inspection; DG-Q4 pending | Successful creation and repeated confirmation; Creation fails after work has begun | Contract-blocked |
| DG-07 | Edit/assign/save/load → ordinary policy persists | No automatic assignment/default/history; later rows native defaults/Any | S029/native persistence | Save and reload an edited generated policy; Add content after creation | Drafted |
| DG-08 | Configure generation defaults → future previews only | Common inheritance; initial mood/joy100%, toleranceAny; per-row values not settings | Source 100% defaults; proposed owned Any/scope | Global preference changes for inherited and overridden saves; Preview edit is not a preference edit | Drafted |
| DG-09 | Composed use → existing route decides | S024/S138 routine restriction; S124/S134 actual medical authority | Existing owned contracts | Generated interval is due but another rule rejects; Medical assistance remains independently eligible | Drafted |
| DG-10 | Disable/unavailable/original active → honest no-new-generation | Keep prior policies/preferences; bounded best effort only | Latest user scope/common settings | Main menu and disabled feature; Original generator is active; Creation support is unavailable | Drafted |

## Interactions and composed result

S029 supplies current admitted catalog/native defaults, publication and identity/choice preservation.
Generation creates a deliberate new policy; maintenance never reapplies suggestions. S138 supplies
one owned tolerance choice and predicate, including Any/zero and route-specific exceptions. S024
potential mood, patient recipient and actual inventory purpose remain additional conditions.
S124/S134 retain medical reasons, actual consumption and pending chemical coordination. Visible
addiction permission defaults deliberately follow S029 instead of raw source-entry false.
All composition uses direct owned calls/data; no source API adapter or patches against our own code.

## UI audit and relocation

| Source entry point / implementation | Purpose | Class | Megamod destination and rationale | Intentional change |
| --- | --- | --- | --- | --- |
| Startup implied policy Def | Automatically supply preset | Automatic gameplay mutation | Omitted | No policy created on startup/load |
| Mod settings Add to current save | Create policy | Gameplay action | Native policy window Generate/preview/Create | Player sees exact choices before creation |
| Mod settings Apply/global sliders | Tune shared source template | Mixed configuration and gameplay draft | Actual reusable defaults in common settings; draft values in preview | No shared mutable global template or retroactive policy editing |
| Source no dedicated bottom tab | No new navigation required | Not applicable | Existing policy management | No new bottom tab |

Proposed common Generation preferences: enabled on, initial mood/recreation limits100%, initial
toleranceAny. These are draft defaults, not a safe-dose formula. Use common Global default/Use
global/Current save controls; new saves inherit current globals, dormant overrides remain, main-menu
save callbacks are disabled. Preferences affect new/refreshed previews and invalidate an open draft;
they never rewrite a created policy. No additional-delay range or source tolerance multiplier is
adopted before research. Draft edits and created policy rows are gameplay state without inheritance.
Verify exact loaded save, world view, save switching, seven-language reasons and disabled controls.

## Own persistence and external integrations actually used

| Owned state or integration | Required contract | Behavior/errors/persistence | Limits | Verification |
| --- | --- | --- | --- | --- |
| Common generation preferences | C02 global/save/inheritance definitions | Current global resolution and retained overrides; no old-save callbacks | Future preview only; no per-row inheritance | Configuration/main-menu/save-switch scenarios |
| Transient preview and one confirmation | Current game/catalog/facts/calculation/preferences and S138 availability | Stale refusal; complete reset; one publication; dispose with owner | No saved live references or global mutable template | Stale and duplicate-confirmation scenarios |
| Created native policy plus owned S138 choice | S029 identity/order/atomic creation and persistence; DG-Q4 unresolved | Add complete independent object; preserve previous state; rollback owned failure | No source Def/schema import, no autosave | Creation/failure/copy/edit/save/reload scenarios |
| Native routine/medical use | Existing S024/S124/S134/S138 contracts | Ordinary route permission and actual consumption/history | Generator is not a scheduler or medical request | Composed native workflows |

No genuine external adapter is currently required. Actual supported native drug behavior must be
researched before automatic suggestions; unknown behavior stays visibly unsupported. Do not audit
original consumers, preserve source identities, import old settings or build coexistence tests.
Any future owned-schema migration requires its own backup, atomicity, restoration and verified
postconditions before implementation.

## Implementation and coverage

| Behavior | Implemented portion | Deviation / possible conflict | Missing / omitted | Target/tests/live evidence |
| --- | --- | --- | --- | --- |
| DG-01–10 | None | Explicit preview/new-policy flow, corrected resets, applicable medical defaults, no Beer or global template | DG-Q1–5 and all runtime work pending; source algorithm/API and universal safety omitted | [Design/tasks](../../../../openspec/changes/specify-megamod-drug-policy-generation/); target docs/tests future |

Canonical original `seohyeon.bestdrugpolicy` active: bounded warning plus best-effort ordinary
effective-off of `drugPolicyGeneration.enabled`; prevent new owned preview/commit and discard
transient draft, retain prior policies/preferences. This is the same timing as manual off, not
rollback of source startup effects or complete coexistence safety. Installed-inactive does nothing;
unrelated features remain. Never change/unpatch the original; no original-provider compatibility
matrix. Core/Harmony required; original generator/tolerance mod and Prepatcher not dependencies.
No XML/XML Extensions or product Gateway dependency is planned. Native semantic admission remains.

## Verification and acceptance

Artifact completion is not apply readiness: tasks1.1–1.3 block autonomous calculations and task1.4
blocks Create. The exact formulas/limits/population assumptions must enter the observable spec
before their implementation. A blank-policy UI shell does not satisfy generation. Later focused
nonzero RED/GREEN uses honest tiers, independent review and scoped package checks. The orchestrator
must personally observe reviewed-build native preview/cancel/reset/create/edit/assignment/save/load,
failure/stale behavior, visible medical choices and any claimed actual composed drug use through
minimized isolated Gateway workflows. Retain exact identity, actions, screenshots, complete relevant
logs and cleanup; finish balance/job-reachability review against calibrated native comparators.
Keep unknown pharmacology and untested compatibility explicitly unclaimed.
