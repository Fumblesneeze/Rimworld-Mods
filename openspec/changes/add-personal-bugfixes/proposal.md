## Why

Personal saves need narrowly targeted repairs for third-party bugs without retaining obsolete patches after upstream updates. Exploration Mode's short world-feature discovery list currently aborts baby placement into a crib.

## What Changes

- Add Personal Bugfixes (`fumblesneeze.personalbugfixes`) at `mods/PersonalBugfixes`, for local use only. This mod is the sole owner of this change.
- Give each fix a relevant IL pattern check, a disposable behavioral probe, post-patch verification, an independent Harmony owner, startup diagnostics and an author-facing Markdown report.
- Deliver the first Exploration Mode list-bound repair and host tests now; defer isolated gameplay acceptance until the user stops playing.

## Capabilities

### New Capabilities

- `personal-bugfixes`: Defensive opt-in-by-evidence repairs, first Exploration Mode fix, reports and verification.

### Modified Capabilities

None.

## Impact

New mod, ordinary and Harmony test suites, test-runner registration and solution entries. Harmony is required. Exploration Mode (`thelastbulletbender.rwexploration`) is optional; its own Vanilla Expanded Framework dependency belongs to that external mod. Biotech and Toddlers are reproduction inputs for the reported childcare workflow, not requirements of the discovery-list repair. No Gateway dependency, XML Extensions, art, new Defs or distribution metadata is required.
