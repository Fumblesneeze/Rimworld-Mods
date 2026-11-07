## Why

Rescuing someone should not silently add them to the colony. S108 replaces RescueeJoinsPlus with
an informed Accept/Reject decision at its two rescue-related joining points, preserving the rescue
and each route's ordinary consequences while making the joining result truthful.

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
Origin replacement **S108/P2-38**, RescueeJoinsPlus / Rescuee Joiner Plus (`Ogliss.RescueeJoinsPlus`,
Workshop2177170403). This change specifies later implementation; no gameplay is delivered.

## What Changes

- Ask before a willing recovering rescuee joins, and before the separate rescue-site promise-to-join
  outcome. Preserve native eligibility and willingness rather than creating more recruitment chances.
- Show the person's identity, age, background role, relationships, work restrictions, traits and
  passions, with access to their ordinary information card before deciding.
- Accept exactly once through the appropriate joining route; reject without a false joined letter,
  a hidden recruitment reward or an added refusal penalty. Continue ordinary guest/rescue behavior.
- Handle carried pawns, simultaneous requests, stale choices, reload and failed integrations safely.
  Correct source wording and lifecycle defects without copying its branch-specific assumptions.
- Keep the decision in a contextual paused dialog. No settings or bottom tab are needed; each
  decision is pawn-specific gameplay state. Compose future S109 presentation directly where useful.

## Capabilities

### New Capabilities

- `megamod-rescuee-join-consent`: Informed rescuee joining decisions, route-specific outcomes,
  request lifecycle and compatibility fallback.

### Modified Capabilities

None. S058 rescue eligibility, S031 child care and the separately queued S109 wanderer events retain
their own contracts; this change does not grant ordinary guest, ritual or wanderer consent coverage.

## Impact

Core and Harmony are required when this integration is implemented. No XML patch is planned and
this feature adds no XML Extensions dependency. Humanoid Alien Races and other pawn providers stay
optional; native pawn information is sufficient for the base feature. No dependency metadata changes
in this documentation slice. Active original provider suppresses owned overlap with diagnostics.

The installed1.6 binary and older public1.4 source were inspected separately against native1.7.
No source1.7 support, assembly/package alias, save migration or universal compatibility is claimed.
Credit Ogliss and implement independently: no license was found in the inspected repository/package,
and no upstream code, text, art or assembly is selected for copying.
