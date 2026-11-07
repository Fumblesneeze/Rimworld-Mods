## Why

Colonists can miss self-treatment because the player forgot their Health-tab permission. Auto
Self-Tend supplies a useful skill-based default while allowing individual choices; the megamod
needs that behavior specified independently, with accurate exceptions and shared settings.

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
Origin: replacement, **S042 / P1-33**, Auto Self-Tend (`funstab.AutoSelfTend`, Workshop 3300580124).
This change delivers **specification only**. No gameplay is implemented or accepted.

## What Changes

- Automatically set eligible colonists' ordinary self-tend permission on first assessment, a
  Medical-level threshold crossing, or an effective policy change. Preserve manual choices between
  these events, including across saves and map/caravan transfers.
- Retain source defaults: minimum Medical level **6**, configurable **0–20**, and automatically
  turn permission off below the threshold **enabled**. Preserve the enable-only alternative.
- Put policy in shared global/per-save mod settings; keep individual permissions in native Health.
- Respect native ability to enable self-tending, retain ordinary medical jobs, care restrictions,
  costs and outcomes, and avoid the source's inaccessible baby/incapable-pawn checkbox writes.
- Specify population, timing, persistence, provider coexistence, and first-use limitations. Avoid
  automatic upstream-settings/save migration or claims of source-package/API substitution.

## Capabilities

### New Capabilities

- `megamod-auto-self-tend`: Skill-based self-tend defaults, manual choices, shared policy and safe
  native medical integration.

### Modified Capabilities

None. Future Drug Response and Priority Treatment compose with this permission policy directly;
their treatment/drug behavior is not specified by this change.

## Impact

Affected: megamod refinement/replacement documentation and future medical policy, common settings
and game-scoped assessment state. Core is required; no DLC or original provider is required.
The original provider is optional but suppresses overlapping owned automation when active. No
Harmony or XML Extensions dependency is needed for the proposed ordinary GameComponent approach;
if implementation requires patches, their target specs and actual dependencies must be added first.
The Dev Gateway remains developer-only, never a product dependency.

[Research](../../../mods/megamod/docs/refinement/2026-11-03-auto-self-tend-research.md) and
[coverage](../../../mods/megamod/docs/replacements/auto-self-tend.md) retain provenance and deviations.
