## Why

Caravan preparation and carrier unloading should be assignable as Basic work without raising
ordinary hauling priority. S135 independently specifies the two assignments made by Basic Caravan Loading.

Sole owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
Documentation for later implementation only.

## What Changes

- Put ordinary caravan-loading assistance and carrier unloading under existing Basic work.
- Preserve player priorities, current jobs, native cargo/eligibility and internal work ordering.
- Compose eligible carrier unloading directly with S133 batching and shared S021/S126 cargo rules.
- Guard both assignments as one compatible change, accept equivalent assignments and preserve
  unrelated work definitions. Do not invent a new work category, menu or settings panel.
- Keep pod/shuttle loading, ordinary hauling and portal behavior outside this reassignment.

## Capabilities

### New Capabilities

- `megamod-basic-caravan-loading`: two Basic-work assignments, ordinary work controls and guarded composition.

### Modified Capabilities

None. Existing owned hauling capabilities retain their physical-cargo contracts.

## Impact

Future implementation requires Core and XML Extensions (`imranfish.xmlextensions`, Workshop2574315206)
for guarded scalar XML edits. This feature adds no Harmony requirement of its own, no dependency on
Basic Caravan Loading, another repository mod or Dev Gateway. DLC/modded transports remain outside
its two native assignments. Source LM420.BCL, Workshop3370434966, is a flat XML-only mod; complete
installed behavior and focused native1.7 work paths inspected. No license file or public source
license was found; independently author the two behavioral changes and copy no assets.
