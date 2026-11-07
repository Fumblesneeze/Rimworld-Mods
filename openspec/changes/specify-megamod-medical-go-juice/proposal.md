## Why

Go-juice can sustain a chemically dependent pawn, yet its hard-drug classification produces
recreational/hard-drug consequences. S086 gives the existing item medical classification without
turning it into harmless medicine or changing its chemical effects.

## Owner

Exactly one owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`).
Target **RimWorld 1.7 beta**, replacement **S086/P2-15**, Go-juice is medical. Specification only.

## What Changes

- Classify the existing go-juice item as Medical for ordinary category-dependent policies,
  history/thoughts and displays, for every pawn rather than only Hussars.
- Retain its existing effects, dependency chemistry, addiction/overdose risks, production, value,
  physical identity and ordinary action-specific permission rules.
- Use guarded idempotent XML for the one category field, preserving all unrelated content.
- Keep the current beta's native inventory injection workflow; the author's older Common Sense
  workaround does not justify a duplicate patch on this inspected version.
- Record that the author's blanket no-binge claim is not established by this XML on the inspected
  beta. A stronger explicit binge ban is an open extension, not silently added behavior.

## Capabilities

### New Capabilities

- `megamod-medical-go-juice`: medical classification, native consequences and action availability,
  unchanged chemical/economic behavior, narrow content scope and compatibility fallback.

### Modified Capabilities

None. Common Sense, drug policy/response, partial ingestion and treatment features consume the
resulting category directly while retaining their own permission, dose and priority contracts.

## Impact

Future XML-only category patch. Core and XML Extensions (`imranfish.xmlextensions`, Workshop2574315206)
are required; XML Extensions also requires external Harmony. Declare the actual dependency graph
with implementation. No custom Harmony patch, code framework, new setting, tab or asset is justified
for this slice. Original provider `oriom.gojuicemedical` and DLC are optional. Ideology/Biotech are
needed only for their particular thought/dependency workflows, not the category change.

No gameplay implementation, source-copying, package spoof, migration, product Gateway content or
other repository-mod runtime dependency. [Research](../../../mods/megamod/docs/refinement/2026-11-05-go-juice-medical-research.md)
records source XML, native consequences and the unresolved binge claim.
