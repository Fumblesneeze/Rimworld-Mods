## Why

Immersive Chefs will need fast tests with different global RimWorld conditions: ordinary domain tests must remain unpatched, focused Harmony tests need explicit patches, and Def-dependent tests need known database contents. The current Zlepper test project references RimWorld assemblies but does not state or prove which game/mod state it initializes.

## What Changes

- Characterize what `Zlepper.RimWorld.ModSdk.Testing/0.0.9` does and does not initialize.
- Keep the ordinary Immersive Chefs test process free of loaded mods, Harmony, and Def data.
- Add process-isolated Harmony and lightweight Def-database test suites with scoped setup and cleanup.
- Teach the guarded test wrapper to run the three Immersive Chefs environments together or individually.
- Document the boundary between synthetic host tests and a genuinely loaded RimWorld/mod/Def environment.
- Add a product-owned, separately staged in-game integration-test assembly that is selected only when Immersive Chefs is in the complete active-mod set.
- Prove an Immersive Chefs XML `PatchOperation` against RimWorld's finalized live Def database with Harmony, Core, XML Extensions, Immersive Chefs, and the Dev Gateway all active.

## Capabilities

### Modified Capabilities

- `developer-verification`: adds explicit host-test environment isolation, scoped Harmony patching, test-only Def loading, and guidance for optional-mod tests.

## Impact

- Adds a separately staged test project below `tests/`; its DLL and shared test contract never ship in the ordinary product package.
- Adds one harmless product-owned `DefModExtension` and unconditional XML `PatchOperation` probe to Immersive Chefs so the real loader can be verified without any Gateway dependency or reference.
- Updates the repository test runner and developer documentation.
- Reads the installed Harmony assembly from the configured Workshop content folder for the isolated patch suite.
- The fast host suites do not launch RimWorld, instantiate external `Mod` classes, invoke RimWorld's XML/Def loader, apply external XML patches, or claim that a referenced assembly is a loaded mod.
- The separate startup-gated integration tier does launch an isolated RimWorld process and keeps its test DLL outside the ordinary Immersive Chefs package.

## Affected Mods

- **Immersive Chefs** (`fumblesneeze.immersivechefs`) at `mods/ImmersiveChefs`: developer-verification infrastructure only.
