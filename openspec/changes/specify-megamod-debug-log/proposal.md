## Why

Troubleshooting a large mod list needs readable diagnostics without losing the evidence needed to
explain failures. Better Log supplies useful controls, but its extra navigation and older logging
assumptions need to be reconciled with the native 1.7 log and megamod's debug-only UI rules.

## Owner

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target: **RimWorld 1.7 beta**. S035, Better Log - Fix your errors (`bs.betterlog`, Workshop
`2772559481`), is a read-only behavior reference, not a co-owner or dependency.

## What Changes

- Specify one native debug-log workflow: top debug toolbar access, severity filters, selectable
  full details, adjustable trace pane, clear and copy. Reuse behavior 1.7 already supplies.
- Specify configurable severity colors, display-only removal of embedded colors, optional
  diagnostic announcements, explicit message suppression, and clearly distinguished logging limits.
- Keep process-wide capture controls global; use the common global/save settings model for display
  preferences. Explain which controls discard future evidence and which only change its presentation.
- Specify optional suppression of future Harmony diagnostic-file writes without deleting existing
  files, and optional readable untranslated fallback text in developer mode.
- **BREAKING relative to source navigation:** omit the bottom log button and added pause/main-menu
  buttons. Diagnostic access and announcements belong at the top developer toolbar.
- Record source defects, intentional default changes, optional-provider boundaries and startup
  admission requirements; never suppress megamod's required startup compatibility warnings.

This is **specification only**. No runtime features, hooks, tests, acceptance evidence or source-mod
replacement are delivered by this change. See the [research](../../../mods/megamod/docs/refinement/2026-11-03-better-log-research.md)
and [coverage report](../../../mods/megamod/docs/replacements/better-log.md).

## Capabilities

### New Capabilities

- `megamod-debug-log`: native diagnostic inspection, presentation, capture controls, announcements,
  developer text options, configuration scopes and graceful compatibility fallback.

### Modified Capabilities

None. S094 Log After Def Error has a [separate owning draft](../specify-megamod-definition-diagnostics/proposal.md);
its definition context composes with this ordinary diagnostic channel. This change does not own its loader behavior.

## Impact

- Future owned diagnostics UI/settings and narrow native logging adapters, independent of the
  shared Thing index and gameplay jobs. No game economy, research or content changes.
- Future Harmony integration requires `brrainz.harmony`. Better Log, Prepatcher, RocketMan,
  Performance Fish and HugsLib are not runtime dependencies; present-provider behavior needs its
  own evidence. No XML Extensions operation is currently proposed; if one becomes necessary it
  requires XML Extensions and guarded content checks in that slice.
- No foreign DLL, artwork, source settings, package identity or automatic configuration migration.
  No dependency on other repository mods or the developer Gateway.
