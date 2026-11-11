# Initial legacy XML diagnostics

Owner: `fumblesneeze.megamod`, S094. **Authored, not compiled or run in this tranche.** Root owns
compilation, metadata planning, review integration and the separate native processes. This case does
not accept the feature or replace the pending default Copy workflow.

## Exact cases and startup selection

Use the existing typed `e2e_run_start` operation with project
`tests/Megamod.EndToEndTests/Megamod.EndToEndTests.csproj` and one exact test ID per run:

- `megamod.diagnostics.definition-initial-default`: affected shared-workflow baseline, native default
  loading and no legacy flag.
- `megamod.diagnostics.definition-initial-legacy`: new declaration with
  `LegacyXmlDeserializer = true`, so the existing runner starts a separate process with exactly one
  native `-legacy-xml-deserializer` argument before initial XML conversion.

Both use Harmony, Core, megamod and `fumblesneeze.megamoddiagnosticsfixture`, in that order, followed
by the runner-appended Gateway. The default group ID retains that pipe-delimited package sequence;
the legacy group adds `::legacy-xml`. Each run uses fresh isolated savedata, English, audio disabled
and default settings (definition context enabled, warnings unmuted). No prelaunch settings file is
required. Existing scenario bounds remain 2,400 frames/ticks and 120 seconds; the orchestrator can use
the existing 420-second process timeout for startup plus scenario/cleanup.

No project item or fixture change is needed. The existing project-owned manifest supplies exactly
`About/About.xml` and `Defs/DefinitionDiagnostics.xml`; the late-options fixture is not selected.
Its exact one missing-type error expectation remains unchanged. The test bundle now contains a
legacy declaration and therefore emits v2 metadata; it requires the reviewed native-mode tooling
from `select-e2e-native-xml-loader`, including selected-group mode preservation.

## One native workflow

Two thin public declarations own separate instances of the same internal scenario. Before any
Arrange mutation, it compares the native command-line predicate with its immutable expected mode.
The first checkpoint records both modes and actual startup diagnostics. The Gateway independently
checks the declaration against observed mode before constructing a selected scenario.

The workflow retains the original native actions: open or retain the ordinary debug log, capture its
current handle/geometry, use Trace small and bounded measured track clicks, click the actual First,
Second and MissingType rows, verify their selected references, use Trace big and capture complete
details. It never calls a loader, emits a diagnostic, clears history/once keys or assigns an asserted
selection/scroll result. Cleanup-only restoration stays separate.

Both loaders are expected to produce two distinct `Parsed 37.5 as int.` Warning records and the
actual missing-type ErrorOnce from the unchanged XML fixture. Require repeat count one, original
text/severity, exactly one source context and unchanged available trace for each retained record.
Installed native source shows the same missing-type message and legacy permissive integer parsing;
that is an execution expectation, not a native pass. Available stack traces may differ between
loaders and are compared with each record's own initial snapshot. Unexpected diagnostics or changed
counts fail; they do not justify changing the fixture or weakening expected-error accounting.

## Copy and cleanup remain mandatory

The native Copy-to-clipboard button copies all retained records. The existing reviewed helper must
first observe a stable empty/plain-Unicode-only clipboard, within its 262,144-character limit, with
successful bounded native reads. It rechecks the snapshot immediately before the native click.
Image, file, rich-text, unknown formats, instability, read failure or an already-equal result stop the
scenario before Copy. Prior private content stays only in memory; it is never checkpointed.

After actual Copy, require exact owned output containing all three full diagnostics and available
traces. Restore prior text/empty state only while the current supported value is the owned output,
and verify restoration privately before releasing ownership. Preserve later external edits. Failed
restoration remains pending for deferred cleanup and cannot be reported complete.

There is no skip-Copy mode and no clipboard clearing to manufacture a supported precondition.
Stopping at the preflight can retain useful earlier loader/selection evidence, but does not pass the
case or satisfy Copy. Successful legacy Copy would not close the separate fresh default obligation.
The helper cannot reserve the clipboard across asynchronous input; avoid concurrent clipboard use.

## Remaining review and native gate

Root first compiles the exact E2E project and dry-runs both exact IDs. Compare identical ordered
packages and fixture file identities, their distinct selected modes, and one legacy flag versus
none. No ordinary Unit/Harmony/Defs process should simulate loaded XML or call this scenario's Arrange.
Independent review must verify the shared-body refactor, unchanged native actions/clipboard guard,
declarations and mode check before native execution.

On the reviewed build, root runs the two separate minimized processes and personally inspects the
causal native screenshots/details and Copy result. Retain requested/observed mode, exact build and
process identities, selected XML files, full flushed Player.log, one expected error, unchanged normal
configuration hashes and exact process/stage/fixture cleanup. A mode flag, host pass, dry-run or
startup log alone is supporting evidence.

This initial-only case leaves legacy hot reload, generated/inherited/patched/nested/exception and
concurrent native paths, original/external providers, locale rendering and broader S035 controls open.
