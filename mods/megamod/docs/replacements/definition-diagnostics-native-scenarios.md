# S094 native scenario authoring notes

Owner `fumblesneeze.megamod`, P0-A. **Default-loader selection and toggle/reload tracers have run;
complete S094/P0 acceptance remains open.** The current reviewed reload passed163 steps with
root-inspected causal captures. Its affected initial-load rerun reached Copy but safely refused the
existing clipboard format, so current-build Copy evidence remains incomplete. Exact identities,
actions, observations and cleanup are retained at
`artifacts/Implementation/megamod-p0/native-reload-reviewed.md`.
These cases refine the existing [S094 acceptance tasks](../../../../openspec/changes/specify-megamod-definition-diagnostics/tasks.md)
for the orchestrator's integrated P0 gate. Native debug-action/log/window controls are now available;
legacy/provider/adverse variants below still require authoring and isolated runs. Every result must
use the reviewed product build and its exact isolated process. The
[initial-xml-preflight](../../../../tests/Megamod.EndToEndTests/Diagnostics/InitialDefinitionDiagnosticsEvidence.cs)
collects supporting actual-loader records only; it cannot accept the behavior by itself.

## Fixture with an actual native conversion warning

Current 1.7.5000 `RimWorld.StatCategoryDef` is a lightweight native Def with integer `displayOrder`
and boolean `displayAllByDefault`; it adds no Thing, recipe or gameplay job. A development-only fixture
package can supply two named unused categories with the same fractional displayOrder value:

```xml
<Defs>
  <StatCategoryDef>
    <defName>MegamodDiagnosticsFixtureFirst</defName>
    <label>first diagnostic fixture</label>
    <displayOrder>37.5</displayOrder>
  </StatCategoryDef>
  <StatCategoryDef>
    <defName>MegamodDiagnosticsFixtureSecond</defName>
    <label>second diagnostic fixture</label>
    <displayOrder>37.5</displayOrder>
  </StatCategoryDef>
</Defs>
```

This disposable fixture is development-only, not product Def content. Its permissive-int warning
has been observed in the real default loader; do not synthesize Log.Warning calls as its result.
Keep fixture source/package hash with the run. Both diagnostics must retain
distinct definition contexts even if the original warning text matches. The legacy loader is a
separate isolated run using its supported command-line switch, not a host invocation of the parser.

An additional XML node declaring a deliberately nonexistent Def type supplies the native
invalid-type ErrorOnce path and expected null/failure outcome. Keep its identity unique to this
fixture and explicitly classify that expected startup error. A later repeated native reload must
not replay an already suppressed native once-only identity. Do not reset usedKeys through setup.

## Candidate case IDs and action sequence

| Proposed ID | Real player action and observed result | Extra evidence |
| --- | --- | --- |
| `megamod.diagnostics.definition-initial-default` | Start the exact fixture mod list; open native top debug log, select each actual converted warning/error, Copy all | Before-selection and selected-detail screenshots; copied retained text; original process output; complete context and unchanged severity |
| `megamod.diagnostics.definition-initial-legacy` | Same workflow in the isolated legacy-loader mode | Exact launcher arguments and same fixture hashes; ordinary parse/failure outcomes |
| `megamod.diagnostics.definition-toggle-reload-default` | In common mod settings disable context, invoke the native Hot reload Defs action, inspect resulting warnings; enable and repeat | Setting screenshots, action handles, three warning contexts/counts, no retroactive editing |
| `megamod.diagnostics.definition-repeat-identity` | Select/copy the two same-text warnings produced by the actual fixture load | Distinct source definitions in retained details; native repeated-row policy for identical context |
| `megamod.diagnostics.definition-provider` | With one exact active original provider, inspect native log after fixture conversion and the bounded startup explanation | Owned context absent, original provider untouched, independent S035 behavior available only when admitted |

The exact native reload action is `Verse.DebugActionsMisc.HotReloadDefs`, named **Hot reload Defs**
in category **General**, with `AllowedGameStates.Playing` in the inspected binary and discovered
path **Actions / Hot reload Defs** in the recorded run. Discover and require
one exact enabled debug-action handle; invoke through the typed Gateway native action. Do not call
PlayDataLoader.HotReloadDefs directly as a substitute for the player's debug-menu command. Wait for
the queued native long event to finish and for player control before inspecting records.

Existing Gateway log reads are supporting data. Opening/selecting/filtering/copying the native log
and changing the actual common settings require faithful native actions; creating a log window or
reading its private selected record directly does not prove those entry points. If the minimized
typed surface lacks the needed native controls, extend the Gateway-owned contract and operation
before the scenario. Do not silently switch to desktop input or report API success as observation.

## Remaining composition and stress cases

The reporting adapter now has61 focused scoped-host reporting/publication/slot tests passing after
independent review fixes, including `NativeLogPatchTests` and `NativeLogPublishedEffectsTests`.
These include bounded concurrent logging/patching, startup/repatch suppression-bypass rejection,
inner-effect admission and a benign rebuild remaining inactive until explicit refresh outside
logging locks. They are not native scenario proof. For each actual loader run, retain the report route's Candidate ID,
Available/Failure state and exact patch owners as supporting evidence before the native log action.
Then inspect the actual warning and ErrorOnce records, select/copy them, and correlate their full
original/context text with the flushed isolated log. The Error path must leave native secondary
logging-failure behavior untouched. An instrumented fixture may supply incompatible/after-ordered
transforms in its own isolated variant; confirm ordinary native output and bounded rejection, and
never use an artificial host emitter as the native conversion outcome. The default source/context
and bootstrap path is now composed. Converter inner-effect admission and the additional variants
remain separate unfinished work.

Preserve explicit cases for warning mute versus display hiding, final custom slot, Error auto-open/
pause, LogLock, protected startup/control notices, developer mode off, main-menu global scope and
save switching. They depend on S035/C02 admission and cannot be marked done by S094 alone. Source
unknown/generated, inherited/patched, nested, exceptional and concurrent conversion fixtures remain
to be authored without turning direct state mutation into acceptance. Pure scoped tests support,
but do not replace, the actual engine workflow.

Normal configuration hashes, ordered package list, exact game/product/fixture identities, action
handles/request IDs, before/action/after screenshots, copied evidence, flushed isolated output and
cleanup are mandatory. The current 1.7 launcher captures stdout as its isolated Player.log with
`-NoFileLog`; retain that owned process output rather than accessing the user's normal profile log.
Restore settings/control/camera/selection state and request graceful exact-process shutdown.
