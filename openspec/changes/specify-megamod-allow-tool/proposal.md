## Why

Allow Tool reduces repetitive selection and designation work and adds urgent hauling, finishing and
drafted hunting. Replacing it requires explicit contracts for these distinct behaviors, including
its less obvious context actions, saved state and consumers, rather than a blanket parity claim.

## What Changes

- Specify area allow/forbid, similar selection, mass and contextual orders, mature harvesting,
  protected trees, connected ore mining and configurable strip-mine plans.
- Specify urgent hauling, blocked-storage feedback, work priorities, finish-off jobs and drafted
  party hunting, including their eligibility, timings, interruptions and persistence.
- Keep native contextual actions and move configuration into the common global/save settings UI.
  Keep pawn hunting membership and the geometry of a mining plan as gameplay state. Put skill/fog
  overrides only in the developer toolbar.
- Correct premature urgent-order removal, invalid-map access, unrelated designation/gizmo loss,
  saved-priority resets and ambiguous cancellation labels. Preserve native 1.7 actions and warnings.
- Compose owned Pick Up And Haul behavior directly, using real carrying capacity and shared Thing
  data; identify concrete external Def/name/package bindings without claiming unverified drop-in
  compatibility. Original-provider co-installation selects the original and disables this family.

This change **specifies later work only**. No gameplay, patches, settings, assets or migration are
implemented. Existing upstream saves are not automatically migrated.

## Capabilities

### New Capabilities

- `megamod-order-tools`: Selection, allow/forbid, harvesting, contextual orders and their configuration.
- `megamod-urgent-hauling`: Urgent item orders, priority controls, delivery lifecycle and storage alerts.
- `megamod-finishing-and-party-hunting`: Downed-target finishing and drafted hunting coordination.
- `megamod-mining-plans`: Connected ore designation and adjustable strip-mine plans.

### Modified Capabilities

None.

## Impact

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Origin: Allow Tool, `UnlimitedHugs.AllowTool`, Workshop `761421485`, S008 / P1-08.
Core is required. Harmony is required when the proposed hooks are implemented. The proposed guarded
Orders XML integration uses XML Extensions (`imranfish.xmlextensions`, Workshop `2574315206`), which
must become required in that implementation slice. This prose change alters no dependencies.
Original Allow Tool and HugsLib are not required. Pick Up And Haul, Some Things Float, TD Enhancement
Pack, Lifters Haul Urgently and source repair patches are inspected external consumers, not new
mandatory dependencies or additions to the requested replacement queue. DLC-specific content is
absent-safe. No source or game assembly is redistributed.
