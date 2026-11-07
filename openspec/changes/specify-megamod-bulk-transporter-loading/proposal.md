## Why

Loading a pod or shuttle should use a pawn's real carrying allowance efficiently without
losing selected cargo, claiming delivery early or competing with the pawn's personal supplies.
S133 brings Bulk Load For Transporters into the same hauling system as S021 and visible cargo S126.

Sole owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. **Specification for later implementation only**.

## What Changes

- Collect several eligible cargo portions per trip for the selected native transport request;
  coordinate workers while preserving exact quantities, item distinctions and destination capacity.
- Offer contextual single-batch and until-complete orders. Preserve ordinary work permissions,
  native boarding, quest requirements, travel and launch controls.
- Unload eligible carrier cargo in batches while preserving protected supplies and actual capacity.
- Account for cargo still being carried, cancellation, partial delivery, failed placement,
  interruption and save/load through the shared owned cargo and recovery rules.
- Keep physical cargo visible through S126 and readable through S132; share the same item index as
  ordinary hauling and search. No additional hauling inventory or capacity bypass.
- Use ordered inherited configuration and existing contextual controls; current automation switches
  belong at the bottom right, diagnostics in the top debug toolbar. Add no bottom main tab.
- Document provider boundaries, source defects and intentional omissions. Vehicle Framework support
  remains deferred until an actual adapter contract is inspected; advertised support is not proof.
- Exclude portal loading at the user's explicit request on 2026-11-07. The inspected portal path
  remains provenance only; no portal adapter, bulk job, control or patch belongs to this feature.

## Capabilities

### New Capabilities

- `megamod-bulk-transporter-loading`: exact multi-item transport loading, coordinated delivery,
  carrier unloading, contextual controls and safe recovery.

### Modified Capabilities

None. Consume existing owned hauling, recovery, visible cargo, supply and settings contracts directly.

## Impact

- Documentation only: proposal, observable scenarios, design, research, coverage and unchecked tasks.
- Core and Harmony required with runtime implementation. DLC-specific transport content and
  third-party transports are optional and content/shape gated. No upstream Pick Up And Haul or Bulk
  Load For Transporters dependency. No dependency on another repository-owned mod or the Dev Gateway.
- Selected design uses native contextual entry points and owned JobDefs; no foreign Def mutation is
  selected. Any later XML patch requires XML Extensions (`imranfish.xmlextensions`, Workshop2574315206),
  metadata changes and guarded merge-preserving operations in that same slice.
- Installed `Ilarion.BulkLoadForTransporters`, Workshop3559292908, flat assembly1.0.0.0, advertises
  1.6/1.7. Author documentation and complete installed behavior inspected; public MIT license credits
  Ilarion2025. Independently engineer the result; no source assembly or asset is copied.
- Seven-language UI, executable semantic patch admission, focused TDD, independent runtime review,
  measured performance and causal native player-workflow acceptance remain future work.
