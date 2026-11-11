# Development-only definition diagnostics fixture

Owner: `fumblesneeze.megamod`, S094. This XML-only disposable fixture is never a product
dependency or release artifact. The orchestrator must stage it as a separate explicitly selected
package through the repository-owned runner; central fixture staging is not registered here.
Do not copy it into a user's active configuration or product `Defs`.

Exact ordered packages: `brrainz.harmony`, `ludeon.rimworld`, `fumblesneeze.megamod`,
`fumblesneeze.megamoddiagnosticsfixture`, then the development Gateway. Use a fresh isolated process,
retaining fixture file hashes and reviewed product identity. The two fractional displayOrder values
are intended to exercise the actual permissive-int native warning. The missing XML type exercises
the native ErrorOnce path. Their actual emitted records still require native verification.

`megamod.diagnostics.initial-xml-preflight` only collects supporting records from this real load.
Its assertions and screenshot do not constitute player-workflow acceptance. The pending native
workflow opens the existing log UI, selects each resulting record, copies its complete text,
captures the selected details, then invokes `Verse.DebugActionsMisc.HotReloadDefs` through the
native `General / Hot reload Defs` debug action. Repeat inspection after the reload completes.
Do not call the loader directly or synthesize Log.Warning/Error messages to satisfy this workflow.

The E2E contract currently lacks minimized native debug-action and log-selection/copy steps.
Root is adding these surfaces. Default and `legacy-xml-deserializer` modes need separate exact runs;
settings-off/on reload and original-provider coexistence remain separate pending scenarios.
