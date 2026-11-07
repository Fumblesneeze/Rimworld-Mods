## Why

Drug policies must stay usable when the installed drug catalog changes, without losing the player's
choices. Drug Policy Fix overlaps RimWorld 1.7's built-in repair but still identifies useful cleanup
and alphabetical navigation; the user's additional request explicitly includes removing missing drugs.

## What Changes

- Specify one current row per usable drug, removal of missing/disabled/non-drug entries, safe empty
  policies and repair before unavailable preset entries can prevent policy creation.
- Retain existing policy choices, assignments, default policy and dose history. Newly added rows in
  existing policies use the inspected 1.7 defaults; creating a fresh preset retains that preset's
  valid choices. Policy names and translations never grant drug-taking permission.
- Alphabetize the native policy window while preserving native drug-taking/refill priority. This
  intentionally corrects the source's underlying-list sort, which also changes job selection.
- Compose content filtering with S038 and scheduled-use restrictions with S024 directly. Distinguish
  unavailable content from zero colony stock; define persistence and bounded repair diagnostics.
- Record provenance, omissions, source differences and open questions. **Documentation only**;
  no gameplay implementation or compatibility acceptance is delivered.

## Capabilities

### New Capabilities

- `megamod-drug-policy-maintenance`: current and usable drug-policy rows, preserved choices,
  explicit new-row defaults, alphabetical navigation, safe preset creation and content-change recovery.

### Modified Capabilities

None. S024's potential-mood condition remains an additional restriction on otherwise permitted
scheduled use. S038's complete content-filtering behavior will be specified in its own source turn.

## Impact

Sole owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Source **S029 / P1-23**, Drug Policy Fix (Continued), `Mlie.DrugPolicyFix`, Workshop **2195390245**.
See [research](../../../mods/megamod/docs/refinement/2026-11-02-drug-policy-fix-research.md) and
[replacement coverage](../../../mods/megamod/docs/replacements/drug-policy-fix.md).

Future engine boundaries are native policy initialization/loading/copying and policy-window rows.
Harmony becomes **required** in the implementation slice if its types are eagerly referenced.
Original Drug Policy Fix and external Cherry Picker (`Owlchemist.CherryPicker`) are **optional**;
owned maintenance works without either. Original co-installation withholds the owned overlap.
No XML, XML Extensions use, metadata or runtime dependency change is introduced by this draft.
No source-package identity, public API facade, multiplayer compatibility or universal save-repair
claim is made. Product packages remain independent of other repository mods and the Dev Gateway.
