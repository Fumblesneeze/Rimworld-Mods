## Why

A player looking at a health condition should be able to find operations that affect it without
searching the entire surgery list. This includes removing a containing limb, removing a bionic
replacement and removing porcupine quills, with useful operation descriptions and one consistent list of relevant choices.

Sole owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin **original**, queue **S137**. Specification for later implementation only.

## What Changes

- Select a visible condition in the Health tab to filter an adjacent searchable medical recipe
  list. Preserve queued bills, individual condition identity, native confirmations and medical work.
- Match actual operation effects and exact body parts, including descendants; present general and heuristic matches alike
  and support explicit, researched corrections for unusual content.
- Keep a clear return to all operations, current patient state, grouped/folded health rows and a
  separate labelled combat-log action. Compose S007 directly, without patches against owned code.
- **Proposed availability extension, pending HBF-Q2:** a removable ancestor limb may be offered
  because a descendant has a condition that qualifies for amputation. Native 1.7 otherwise requires
  the condition on the amputated part itself. This narrow extension makes the user's foot-infection
  example work; all other operation restrictions, costs and consequences remain in force.
- Reuse the owned medical catalog and current-patient view. No automatic treatment, condition
  deletion, new main tab, configuration system or separate medical cache.

## Capabilities

### New Capabilities

- `megamod-condition-medical-bills`: contextual medical recipe discovery, exact effect matching,
  uniform matching presentation, ancestor eligibility and safe native ordering.

### Modified Capabilities

None in the archived capability set. The **unimplemented S007 change draft** is amended alongside
this change: its row click retains combat-log navigation when S137 is unavailable, and otherwise
uses a separate Combat log control while the primary click selects this filter.

## Impact

Documentation only. Later native integration requires Core and Harmony when its types/patches are
used. Odyssey is optional: quill recipes appear only with actual supplying content. Compact Hediffs
and Dubs Mint Menus are read-only references, not runtime dependencies. The latter's health recipe
pane is the selected reference; its other menus are not selected by this request. No other
repository mod or Dev Gateway product dependency. XML Extensions becomes required only if a later
slice actually uses its operations; no external XML mutation is currently proposed.

S007 owns compact display; S062 owns condition-specific care; S089 owns healer-serum selection;
S124/S134 own automatic medical orders and dose reconciliation. This feature discovers choices
and submits explicit native player orders to those established ownership rules. No package/type
spoofing, source settings import or general drop-in compatibility claim.
