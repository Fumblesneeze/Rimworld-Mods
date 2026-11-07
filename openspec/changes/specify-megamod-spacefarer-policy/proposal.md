## Why

The Spacefarer apparel policy should recognize added vacuum-resistant gear while retaining the
player's exclusions. S130 adopts Dynamic Spacefarer Policy's additive, once-per-type behavior and
corrects its dependence on a translated policy name and incomplete history after content removal.

Sole owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. **Specified for later implementation only**; nothing accepted in game.

## What Changes

- Add eligible apparel types to the fresh native Spacefarer policy, using the source's positive
  declared vacuum-resistance criterion and preserving all other filter restrictions and allowances.
- Remember which qualifying types have already been considered for the actual policy, so later
  manual exclusions survive reload, rename, language change and temporary content removal.
- Respect native linked item toggles: existing exclusions and preview declines take precedence;
  hold and explain conflicting additions while permitting independent nonconflicting candidates.
- Bind the freshly generated policy by its identity, not its label. For an existing save without
  reliable ownership history, let the player choose the target in the native apparel-policy editor
  and review initial additions. Do not silently guess which same-name policy or old exclusion to change.
- Keep later newly qualifying apparel additions automatic for the tracked policy. Deleting or
  explicitly releasing that policy stops tracking; a duplicate/reused name does not inherit it.
- Preserve native pawn outfit assignment, actual vacuum protection and wearing restrictions.
  Policy choices are gameplay state, not global/save mod configuration, and add no bottom tab.
- Document intentional deviations: no global mutation of apparel default tags, no source save-key
  impersonation, no automatic migration of foreign history, and original-provider co-installation
  withholds the owned overlap with warning.

## Capabilities

### New Capabilities

- `megamod-spacefarer-policy`: additive vacuum-gear policy maintenance, reliable policy/history
  identity, safe existing-save adoption and preserved native equipment behavior.

### Modified Capabilities

None. Owned weather-ready arrivals and HUD remain separate contracts consumed directly as needed.

## Impact

- Documentation, research, coverage and refinement tracking; no game or source-mod files changed.
- Odyssey is **required for this feature**, optional for the megamod as a whole. Without its content,
  the feature is inactive and other megamod functionality remains available. Source mod and added
  apparel providers are optional; source metadata needs Odyssey but no Harmony. Our future native
  lifecycle/UI hooks require Harmony with semantic target admission. No XML Def changes selected.
- No runtime Def-tag mutation is copied; native policy data is changed through ordinary filter
  operations. No XML Extensions dependency is introduced without an actual XML operation.
- No public source/license was located in the author link or bounded searches. Independently
  engineer observed behavior; no upstream DLL/assets/source redistribution or binary parity claim.
- Focused tests, guarded hooks, seven-language contextual UI, review, package checks and final
  minimized native policy/save/wearing workflows remain unchecked implementation work.


Initial support is for colonies started with megamod. Existing-policy enrollment is contextual policy management within those colonies, not an original-mod save import. S004 consumes shared apparel facts for its separately owned all-humanlike vacuum-arrival behavior.
