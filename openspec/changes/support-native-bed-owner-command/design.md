## Owner
Repository tooling: RimWorld Dev Gateway.

## Design
The exact native Command_SetBedOwnerType class inherits Command rather than Command_Action. Its inspected 1.6 and 1.7 ProcessInput methods ignore the event argument, call the base activation sound and open the normal three-choice FloatMenu. Invoke that method with no fabricated Event.current, then use the existing exact newly opened menu lease and CurrentFloatMenuActionStep. Classify only the exact native type, leaving arbitrary custom ProcessInput overrides unsupported. Existing registry disabled/stale/selection checks still apply.

## Evidence and tests
Native RED: GuestBed's hospitality-absent test in grouped run 20261116T180441137Z-4feb65e58e294a8ba7a273aa7148c06f sees the original command with no supported interaction. The product remains inactive as expected. Final GREEN requires the reviewed Gateway on both major/minor targets: select bed, open native three-choice menu, choose prisoners, inspect the changed bed, and clean up the isolated process. Host construction of this texture-initializing command cannot substitute for a real game.
