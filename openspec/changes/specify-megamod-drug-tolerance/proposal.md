## Why

Drug policies can limit how often a pawn takes each drug but cannot express a shared-chemical
tolerance limit. S138 adds that player choice while keeping medical assistance and native permission
separate from routine recreation and scheduled use.

Sole owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target: **RimWorld 1.7 beta**. Choice/decision rules, S029 state transactions and the guarded native
runtime are implemented. Scoped persistence, English editor and scheduled/direct-recreation routes
have reviewed live evidence; the full feature remains incomplete. See tasks for exact accepted scope.

The focused caravan consumer passed on reviewed4d4d57e: separate numeric-zero and Any caravans
exercise real world ticks, native Items and detailed Health, preserving the ordinary daily interval
and physical stock. Root inspected all13 captures; full feature acceptance remains open.

## What Changes

- Add an optional tolerance threshold to applicable drug-policy rows in the native policy window.
- Default new rows to Any, meaning no additional restriction. Treat absent tolerance as zero and
  numeric thresholds as strictly below; numeric zero therefore blocks threshold-governed routine use.
- Keep ordinary scheduled and recreation conditions, including native exceptions specific to joy.
  Compose directly with S024 and preserve the separate S124/S134 medical routes.
- Preserve valid player choices through S029 maintenance, copying and our own save/load. Expose
  malformed saved choices and conservatively withhold threshold-governed routine use until repaired.
- Keep policy choices as save-local gameplay state and the feature switch in common configuration.
- Keep the bounded original-loaded warning and best-effort ordinary settings-off behavior, without
  original-mod compatibility, API preservation or state imports.

## Capabilities

### New Capabilities

- `megamod-drug-tolerance`: visible policy tolerance choices, routine-use restrictions, persistence,
  direct owned composition, configuration and unavailable-state behavior.

### Modified Capabilities

None. This new capability consumes S029/S024/S124/S134 contracts without editing their changes.

## Impact

Code belongs to the megamod policy feature, its shared configuration and native decision/UI
adapters. Core is required; Harmony (`brrainz.harmony`) is required for planned native hooks and is
already a product dependency. No XML Extensions operation is planned. DLC drug content is optional
and capability-resolved; the source mod and Prepatcher are not runtime dependencies. No dependency
on another repository mod or the Dev Gateway is introduced.

Research: `mods/megamod/docs/refinement/2026-11-08-drug-tolerance-research.md`.
Source: SeoHyeon's Only If 'Tolerance' Below in Drug Policy, Workshop 3088210509,
`seohyeon.onlyiftolerancebelow`; independent implementation only. Any/zero behavior and conservative
malformed-data handling are explicit proposed corrections, not claims about source behavior or
blanket runtime acceptance. Focused TDD, review and scoped native observations are retained; remaining
route/composition, seven-language UI and playability gates are still required.
