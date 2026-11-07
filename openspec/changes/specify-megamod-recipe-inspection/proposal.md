## Why

The player needs to see what a recipe produces and requires before adding its bill. Nice Bill Tab
provides useful inspection and comparison ideas for the requested richer Add bill view, but its queue
replacement, implicit bill edits and approximate statistics are not the intended contract.

## What Changes

- Specify a contextual recipe detail view with output quantities, material/style identity,
  ingredient alternatives, work and required skills, plus access to ordinary information cards.
- Let the player compare two recipe/material choices without changing existing bills or making
  production orders merely by inspecting a recipe.
- Use the material-aware comparison contract already drafted from Quartermaster: Normal quality,
  honest units and unknown values, one-execution supply estimates and one colonist meeting all skills.
- Keep recipe details usable for multi-output and special recipes whose results cannot be ranked.
- Retain useful ingredient inspection and explicit map navigation; omit automatic ingredient bills,
  queue editing, arbitrary combat estimates and upstream settings/save imports from this slice.
- Record actual consumers and deliberate incompatibilities. Nice Bill Tab's Expansion will receive
  its own change, then the original Add bill browser will compose these capabilities directly.

This change **specifies later work only**. No UI, patch, adapter or gameplay behavior is delivered.
The two-choice comparison and ingredient navigation are proposed source-derived details of the
confirmed UI-reference scope; unresolved final layout decisions remain in the refinement ledger.

## Capabilities

### New Capabilities

- `megamod-recipe-inspection`: Inspect recipe outputs, requirements and comparable product details
  in Add bill, with read-only browsing and explicit navigation.

### Modified Capabilities

None. This slice consumes the proposed `megamod-material-aware-recipe-comparison` contract without
changing its material selection, availability, ranking or bill-creation rules.

## Impact

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Source: **Nice Bill Tab** (`Andromeda.NiceBillTab`, Workshop `3520130671`), S010 / interview:P1-09.
See the [source report](../../../mods/megamod/docs/replacements/nice-bill-tab.md) and
[research journal](../../../mods/megamod/docs/refinement/2026-11-02-nice-bill-tab-research.md).

Future implementation affects the workbench's Add bill workflow and shared recipe/stat/map data.
It must preserve native queue controls and optional providers' actions. No bottom tab, research,
production cost, quality roll, worker eligibility or resource balance change is intended.
Nice Bill Tab, its Expansion and Quartermaster are reference sources, **not required dependencies**.
CE, VEF, Research Reinvented, BWM and identified external consumers are optional integration cases,
not compatibility promises. No external runtime dependency is introduced by these documents.
Harmony is required if used by the implementation; XML Extensions is required with any
`XmlExtensions.*` operation. No upstream assembly or Dev Gateway dependency belongs in the product.
