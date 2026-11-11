# UI placement and settings scope

These rules override source-mod placement. Audit all upstream bottom tabs and dynamically injected
controls before adopting functionality. The [normative spec](../../../openspec/changes/add-megamod-foundation/specs/megamod-ui-placement/spec.md)
contains the acceptance scenarios. No UI is implemented in the scaffold.

| What the function does | Destination | Example / boundary |
| --- | --- | --- |
| Debugs the game or performs an unrestricted cheat/override | Top debug toolbar, visible in developer mode | Character Editor-style changes outside sensible mechanics, including cosmetic overrides. |
| Governs configuration, whether global or save-specific | Mod settings menu | Parameters/defaults and save configuration; clear feature order and scope sections. |
| Only enables/disables a current-play view or behavior | Small bottom-right control | A view toggle or automatic rebuilding; state is visible without opening a window. |
| Opens a window to control an ordinary game mechanic | Eligible for the bottom tab bar | A genuine gameplay management workflow, following normal game rules. |

Classify by purpose and split mixed-purpose screens. A settings page or cheat does not become a
gameplay window because an upstream mod gave it a MainButtonDef. A simple toggle does not become a
gameplay tab because it opens a window containing one checkbox. Debug overrides take the debug
route even when they are toggles. Operational toggles can persist state; their defaults and tuning
remain configuration in mod settings. Use native contextual commands for actions outside these
categories rather than adding unjustified tabs.

Choices specific to a pawn, area/zone, bill, building, or another gameplay entity are gameplay state,
not settings. They belong in native contextual UI or the mechanic's management window, even though
they are saved. Do not apply configuration inheritance to those choices. Unrestricted entity
overrides still belong in debug controls.

Actual mod settings use one common loading, storage, effective-resolution, and rendering path.
Organize them in stable feature/category order with clear boundaries. By default each row contains:

| Control | Loaded game | Main menu |
| --- | --- | --- |
| **Use global** checkbox | Editable; checked for new/missing save entries | Disabled; no current-save target |
| **Current save** control | Enabled only when Use global is unchecked | Always disabled |
| **Global default** control | Editable; clearly distinguished from the save value | Editable |

While inheriting, display the effective global value clearly in the disabled per-save control.
Global edits affect inheriting saves when resolved, while existing overrides remain unchanged.
Specify when the feature applies an effective-value change. Store globals in global mod configuration
and each save's inheritance flag/override with that save. Preserve dormant overrides when Use global
is checked; unchecking restores one or initializes the first override from the current global value.
Do not serialize inherited values as stale save-local copies or silently copy save values into globals.

Definitions provide stable IDs, types/defaults/validation, labels, category/order and any scope
exception. If global inheritance is impossible or nonsensical, document the reason, actual scope,
persistence and UI behavior in the feature spec/definition. Exempt settings still use the common
path with honest unavailable-control labels. Do not exempt a setting merely to avoid implementing it.

At the main menu, global settings stay usable. All current-save controls, resets/imports, and their
write paths are disabled with a localized explanation. There is no implicit target save and no
editable last-loaded save. In a loaded game, bind to that exact save, including in world view with
no selected map; discard stale references on exit or save switch.

Record upstream placement, chosen destination, rationale and intentional navigation deviations in
each replacement report. Do not preserve original-mod APIs or recreate a tab for source-compatibility
purposes; that work is outside scope. Verify native entry-point/action/result,
developer mode on/off, main-menu disabling, inheritance/override resolution, persistence, documented
exceptions, scope isolation and switching between saves. Review all
required locales on actual UI when the first settings/control slice is implemented.
