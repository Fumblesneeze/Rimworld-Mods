## Why

Useful colony problems and opportunities should remain visible until addressed, without repeatedly
opening individual pawns, buildings or maps. S084 reconstructs Extra Alerts' actual alert catalog
while keeping hidden-information diagnostics out of ordinary play.

## Owner

Exactly one owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`).
Target **RimWorld 1.7 beta**; replacement **S084/P2-13**. Specification only.

## What Changes

- Specify the conditions and navigation for visible enemies, downed people/entities, blight, six
  mood problems, animal temperature illnesses, unarmed colonists, exposed electrical buildings,
  available traders and Odyssey porcupine quills.
- Keep alerts informational: no automatic orders, captures, treatment, equipment changes or trades.
  Resolve them from current observable conditions across the appropriate maps/caravans.
- Correct independent entity-alert configuration, missing/stale/null data, misleading orbital-trader
  detection and duplicate or hidden culprits. Preserve normal native alert interaction.
- Move hidden-enemy information into developer-only top debug controls under the user's UI rules.
- Use common global/save settings for ordinary alert preferences and native/shared facts, with no
  source-specific settings store, duplicate Thing index or new bottom tab.

## Capabilities

### New Capabilities

- `megamod-colony-alerts`: actual ordinary alert conditions, scope, navigation, feedback, inherited
  preferences, source ownership and isolated unsupported-content handling.
- `megamod-hidden-enemy-diagnostic`: explicit developer-only hidden-enemy information and its
  transient visibility/lifecycle boundary.

### Modified Capabilities

None. Existing blight cutting, medical workflows, trader gathering and other owned actions keep
their contracts; these alerts observe the resulting state and compose directly.

## Impact

Future native alert subclasses, contextual explanations, developer control and common settings.
Core is required. Anomaly and Odyssey are optional, needed only for their entity/quill content;
provider integrations are optional and separately admitted. Source uses no Harmony or PatchOperations,
so neither Harmony nor XML Extensions is introduced just to mirror it. If implementation proves a
real patch necessary, add its target contract and actual required dependency; XML Extensions is
required with any first `XmlExtensions.*` use. No source code/binary/art, package spoof, other
repository-mod runtime dependency or Gateway content is shipped.

No gameplay implementation or acceptance. [Research](../../../mods/megamod/docs/refinement/2026-11-04-extra-alerts-research.md)
records installed/public identity, current native contracts, intentional changes and remaining work.
