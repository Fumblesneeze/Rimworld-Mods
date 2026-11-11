Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
S094/P2-23, Log After Def Error (Continued). Observable contract; initial implementation is
implemented-unverified and native acceptance remains pending.
Warning-and-error coverage and one-occurrence enrichment are deliberate corrections to the inspected
continuation, whose extra informational message currently covers warnings only.

## ADDED Requirements

### Requirement: Definition conversion diagnostics include identifiable source context
The feature SHALL add a clearly separated definition-context section to each accepted warning or
error reported while an admitted definition conversion is in progress. It identifies the definition
name and declared kind when available, source mod display name and package identity when available,
and the XML file being converted. The original diagnostic text, severity and available stack trace
remain intact and inspectable. Context is included in retained full details, copied retained output
and ordinary log-file output; a shortened list preview does not truncate that evidence.

The section retains the searchable marker `[Def Error]` for this source-derived feature, but a
warning remains a warning. Files under a known mod root are shown relative to that root; other known
paths are explicitly identified as external paths, not falsely made relative. Unknown fields are
marked unknown, rather than filled from another definition or an inferred mod name.
Relative names without a known absolute path and invalid paths are explicitly unresolved; the
process working directory is not invented as their origin. Contextual metadata escapes controls
and Unicode line separators so it cannot manufacture another field; original diagnostic text is
preserved verbatim.

#### Scenario: Locate a permissive integer warning
- **WHEN** conversion of a named definition reports that a fractional value was parsed as an integer
- **THEN** its warning retains that original text and gains the current definition/mod/file context
- **AND** selecting and copying it exposes the complete context without creating a second informational occurrence

#### Scenario: Conversion reports an error
- **WHEN** a definition conversion reports an error such as an invalid declared type or a caught parsing exception
- **THEN** the error retains its original severity, text and available trace alongside current source context
- **AND** this feature does not turn the failed conversion into a successful one

#### Scenario: Inspect diagnostics from native legacy loading
- **WHEN** a fresh process uses the game's native legacy XML loader and actual definition inputs produce warnings and a missing-type error
- **THEN** selecting those retained records exposes their complete current definition, source mod and file context with their original text, severity and available trace
- **AND** native copied retained output and ordinary file output preserve that same context without a second informational occurrence
- **AND** separate definitions with the same original warning remain distinguishable

#### Scenario: Source fields are missing
- **WHEN** a generated or otherwise unattributed definition reports a conversion diagnostic
- **THEN** known current definition fields remain useful and missing source fields are explicitly unknown
- **AND** no unrelated mod or previous file is substituted

#### Scenario: Source path cannot establish containment
- **WHEN** the known source path is outside the mod root, relative without an absolute origin, or invalid
- **THEN** context identifies it as external or unresolved as appropriate, without inventing a mod-relative path
- **AND** the original diagnostic and other known fields remain intact

### Requirement: Context identifies the current conversion rather than assigning blame
The feature SHALL describe the definition being converted and its known asset origin, not claim
that the named source supplied the erroneous value or is the mod responsible for the fault.
Inherited values and external XML patches can contribute to a definition; this feature does not
invent a patch history, line number or causal author. It observes conversion without modifying
values, definitions, inheritance, patch order, native fallback results or exception handling.

Ordinary messages, diagnostics outside definition conversion, XML syntax/file-read errors before
conversion, later cross-reference/configuration validation, assembly loading and gameplay errors
remain ordinary diagnostics without guessed definition context. A successful conversion adds no
diagnostic by itself. No historical log rescan or automatic repair/upload is performed.

#### Scenario: Another mod patched the definition
- **WHEN** a warning occurs while converting a definition whose values were inherited or patched
- **THEN** context names the currently converted definition and known asset origin without labelling that source as the culprit
- **AND** loaded values, patch results and native error/fallback behavior are unchanged

#### Scenario: Errors outside the conversion scope
- **WHEN** XML cannot be read before conversion, a later cross-reference fails, or gameplay reports an unrelated error
- **THEN** this feature supplies no stale or guessed definition context
- **AND** the original diagnostics still follow the ordinary logger

#### Scenario: Successful loading
- **WHEN** definitions load without warnings or errors
- **THEN** this feature emits no per-definition success messages or additional diagnostic stream

### Requirement: Nested and interrupted conversions keep truthful context
The feature SHALL use the conversion actually in progress when a diagnostic is reported, even
when conversions nest, run concurrently or fail. A nested conversion's diagnostic uses its own
definition and known source; after it finishes, an outer conversion resumes its own context.
An unknown source for a genuinely different nested definition stays unknown rather than borrowing
the outer file. Multiple entry points forwarding the same conversion do not add repeated sections.

A diagnostic reported on one loading path cannot acquire the definition from another concurrent
path. Deferred display retains the context from the time of reporting. When a conversion exits,
including by an exception, its context no longer applies to later unrelated messages. Native
definition reloads establish fresh contexts and never reuse a previous game's or earlier load's
last definition as a default.

#### Scenario: Nested conversion with missing source
- **WHEN** an outer definition A calls conversion of B without known source metadata, B reports a warning,
  then A reports a warning after B returns
- **THEN** B's warning names B with unknown source, and A's warning names A with A's known source
- **AND** neither warning gains multiple owned context sections merely from forwarding

#### Scenario: Concurrent reporting and deferred inspection
- **WHEN** separate loads of A and B report the same warning text and the player inspects them after loading finishes
- **THEN** each retained occurrence identifies its own definition and file
- **AND** their distinct context is not lost by combining them into one undifferentiated repeated row

#### Scenario: Exception followed by another diagnostic
- **WHEN** a conversion exits exceptionally and an unrelated warning is reported afterward
- **THEN** any accepted diagnostic reported during conversion retains that conversion's context
- **AND** the later warning has no leftover context and the exception retains its native handling

### Requirement: Context is part of one ordinary diagnostic occurrence
The feature SHALL compose directly with the owned debug-log behavior so original diagnostic and
context are accepted, muted, stopped, filtered, selected and copied together as one ordinary
occurrence. Warning context cannot bypass Mute warnings by appearing as an unmuted Message.
Context adds no second capture-limit charge, announcement, sound, auto-open or pause action.
Native once-only identities and their suppression remain intact; enabling this feature does not
replay already suppressed occurrences. Same-text diagnostics from different definition contexts
remain distinguishable; identical context can use native repeated-row behavior.

Ordinary capture limits and temporary native log suppression apply normally. Reaching an owned
capture limit still uses S035's separate limit notice; the definition context is not that notice.
Required startup compatibility warnings retain their protected path and are not mislabeled as
definition diagnostics. Failure while obtaining or presenting context cannot recursively generate
a stream or hide the original diagnostic: keep the ordinary diagnostic, omit unavailable context
and provide at most one bounded feature-failure explanation for that failure episode.

#### Scenario: Muted warning and hidden warning
- **WHEN** Warning capture is muted, or instead Warning display alone is hidden
- **THEN** the muted warning produces neither ordinary captured text nor context, while the hidden accepted warning retains both for later inspection and Copy all

#### Scenario: The last permitted occurrence
- **WHEN** one capture slot remains and a definition warning is accepted
- **THEN** the warning and context consume that one slot together
- **AND** context does not consume the slot before the warning or produce another announcement; the ordinary limit notice follows S035

#### Scenario: Native once-only suppression
- **WHEN** the same native once-only diagnostic identity is attempted again after its first accepted occurrence
- **THEN** this feature emits no separate context for the suppressed attempt and does not reset the once-only identity

#### Scenario: Context handling fails
- **WHEN** source-context extraction or formatting fails while reporting an otherwise accepted error
- **THEN** the original error and its ordinary effects still reach the admitted native reporting path
- **AND** context failure produces no recursion, duplicate original occurrence or repeated failure spam

### Requirement: Configuration has an explicit process-wide scope
The feature SHALL offer Definition diagnostic context, enabled by default, in the common mod
settings under diagnostics. It uses the common loading, storage, validation, reset and UI path but
is global-only: definition loading begins before a save exists, and diagnostics from that process
are shared across saves. The row explains this exception and has no active per-save override or
Use global control. The global choice is editable from the main menu and a loaded game; loading a
different save never changes it. A valid edit affects subsequent eligible occurrences, not already
captured records, and does not reset the logger's capture interval or native once-only identities.

No bottom tab, bottom-right control, pause-menu button or separate log window is added. Inspection
uses the ordinary log and S035's top developer entry points; owned manual diagnostics controls stay
hidden with developer mode off, while native automatic startup reporting and file evidence remain
available. Owned setting labels, scope explanations and unavailability text support the seven
baseline languages; original messages, definition identifiers and paths are preserved.

#### Scenario: Configure before loading a colony
- **WHEN** the player changes the global option at the main menu and later loads or switches saves
- **THEN** the same process-wide choice controls subsequent eligible diagnostics without a save-specific value taking over
- **AND** existing evidence and capture counts remain unchanged by that option edit

#### Scenario: Disable and re-enable
- **WHEN** the player disables the option, triggers a later admitted definition reload, then re-enables it for another reload
- **THEN** only subsequent eligible diagnostics while enabled receive context, with no retroactive annotation or replay
- **AND** an initially disabled feature receives its first guarded admission opportunity before the later reload, while previously rejected paths remain inactive with their explanation

#### Scenario: Native reload preserves repeated occurrences and once-only identity
- **WHEN** two fractional-integer definition warnings and one missing-type ErrorOnce were captured during initial loading, followed by a disabled and a re-enabled native Hot reload Defs command
- **THEN** the two disabled warning occurrences retain only their original text, the two re-enabled occurrences identify their current definitions, and the initial records remain unchanged
- **AND** occurrence accounting includes native repeat counts when identical adjacent plain warnings combine; the original missing-type error remains one occurrence and no once-key reset or log replay is required

#### Scenario: Ordinary Options diagnostic after reload
- **WHEN** the player opens ordinary Options after the re-enabled reload and its native category validation reports an unrelated error
- **THEN** that error has its ordinary severity and text without the previous conversion's context
- **AND** selecting it in the native developer log exposes that ordinary diagnostic; no direct test logging supplies the expected result

#### Scenario: Developer mode is off
- **WHEN** a conversion error occurs while developer mode is off
- **THEN** accepted diagnostic context still reaches ordinary file/retained evidence and native automatic error reporting
- **AND** this feature does not add a hidden-mode manual control or gameplay notification

### Requirement: Incompatible integrations fall back to ordinary diagnostics
The feature SHALL activate only the definition-loading and reporting paths whose documented
minimal semantic conditions hold at startup, allowing benign edits and compatible other patches.
Unknown, changed or failed integration conditions leave the affected path or declared group inactive,
preserve ordinary loading/logging and the stored setting, and produce one bounded startup warning
per skipped enabled patch identifying the target condition, unavailable context and fallback.
Installation failure removes only newly installed owned work; later supported changes are rechecked.

Owned admission warnings normalize control characters to spaces and bound each diagnostic field to
256 characters, with a visible ellipsis on truncation. This bound applies only to owned admission
metadata/reasons; it never truncates the original warning/error text or captured definition context.
When installation and cleanup both fail, the bounded reason preserves the cleanup-failure count and
separately bounded installation/first-cleanup summaries, rather than hiding cleanup behind a long
installation error. Failed cleanup is not reported as successful removal; any remaining owned
callback stays inactive until its complete required group is admitted.
Failure of the protected primary warning output tries one independent fallback without recursively
logging the delivery failure. If neither destination can receive the warning, retain the first 64
bounded delivery-failure records and the total undelivered count for inspection; unrelated feature
admission continues. This small startup failure inventory is not a second ordinary log history.

With original Log After Def Error or its continuation active, S094 SHALL emit a bounded startup
warning and attempt best-effort disablement through its ordinary feature-off path, with manual-off
behavior and timing. Stored preferences and independent owned debug-log behavior remain intact under
their own settings/admission. Inactive installed originals SHALL NOT trigger this rule. No original
setting, assembly or package identity is imported or impersonated, and no original API or coexistence
guarantee is made. Absence of S035's
optional presentation enhancements does not by itself disable source context; the admitted native
diagnostic path remains sufficient. Unknown third-party capture paths are not advertised as covered.

#### Scenario: One definition loader changes
- **WHEN** the selected native loader no longer supplies a trustworthy current definition or the reporting path no longer permits safe context association
- **THEN** that path reports ordinary diagnostics without owned context and gives its bounded startup explanation
- **AND** independently admitted log capture and inspection remain usable without altering foreign patches or parsed content

#### Scenario: The shared warning route remains useful after error-context rejection
- **WHEN** warning reporting is admitted but the required error-context route fails admission
- **THEN** no owned definition context is published for either severity
- **AND** independently requested and admitted warning muting remains usable without removing the shared warning route

#### Scenario: An original provider is active
- **WHEN** original Log After Def Error or its continuation is active alongside megamod
- **THEN** one bounded startup warning identifies best-effort S094 handling through the ordinary feature-off path with the same behavior and timing as manually off
- **AND** its saved option and independently admitted log inspection/capture remain intact, without modifying the original or promising complete suppression/coexistence safety

#### Scenario: A long installation error accompanies failed cleanup
- **WHEN** an enabled patch group fails installation with a long error and one or more owned cleanup actions fail
- **THEN** its bounded startup explanation retains the cleanup-failure count and an installation/cleanup summary
- **AND** the group remains inactive without claiming that all installed work was physically removed

#### Scenario: Published inner effects change native reporting
- **WHEN** Harmony rebuilds a reporting target with an inner effect that changes required native suppression, message flow or an owned reporting helper
- **THEN** owned reporting remains inactive unless the actual published generation preserves its required semantics; ordinary foreign-modified logging remains available
- **AND** removing the incompatible effect permits a newly validated published generation, without reusing an earlier publication
- **AND** unrelated effects and compatible observations are assessed by their consumed effects rather than rejected by owner identity

#### Scenario: A reporting rebuild has not published
- **WHEN** refresh is requested during composition or after a failed rebuild
- **THEN** old metadata cannot admit the attempted generation, and an older executing body cannot borrow a newer generation's permission
- **AND** output callbacks do not inspect Harmony metadata while holding native logging locks

#### Scenario: A read-only scalar observation coexists with reporting
- **WHEN** a void inner postfix reads the native suppression result by value without a mutable or outer binding
- **THEN** native suppression and owned context remain available without executing the observer during admission

#### Scenario: Published metadata cannot be inspected safely
- **WHEN** the reporting metadata format is unsupported or its exact published entry exceeds the owned 4 MiB inspection bound
- **THEN** owned reporting remains inactive with a specific bounded explanation and ordinary native diagnostics remain available
- **AND** admission and refresh do not construct foreign attributes or invoke callbacks/factories merely to inspect metadata

### Requirement: Conversion failures cannot strand diagnostic context
The mod SHALL add definition context only when it can restore the previous context after conversion.
If another modification prevents reliable restoration, affected diagnostics SHALL remain ordinary
native diagnostics. A later compatible conversion configuration MAY restore context for future records.

#### Scenario: Cleanup is skipped or fails before restoration
- **WHEN** another modification can skip or interrupt the cleanup needed for a definition context
- **THEN** the mod does not acquire that context and later diagnostics do not inherit it
- **AND** the native conversion result or foreign exception is preserved

#### Scenario: Compatible observation and an already active conversion
- **WHEN** compatible observation leaves context acquisition and restoration intact
- **THEN** definition context remains available
- **AND** a conversion that already acquired context restores its caller even if future context becomes unavailable during that conversion

### Requirement: Nested loading cannot borrow another definition's identity
Definition context SHALL require trusted entry coverage for both native loading paths as one S094
capability. Missing initial coverage SHALL leave ordinary diagnostics available on both paths;
independent warning-capture settings SHALL continue to operate.

#### Scenario: A nested conversion loses trusted coverage
- **WHEN** either loading path enters nested work whose context or restoration cannot be trusted
- **THEN** its records never inherit the parent's definition identity
- **AND** the remaining parent work conservatively uses ordinary diagnostics until its existing scope ends
- **AND** that scope still restores its caller and preserves native exceptions

#### Scenario: Foreign callbacks precede a native entry guard
- **WHEN** callback ordering cannot establish trusted coverage before nested conversion work
- **THEN** owned definition context remains unavailable rather than assigning a parent's identity to the child

#### Scenario: Coverage changes during an active conversion
- **WHEN** an observed change replaces either loading path's trusted coverage while another conversion is active
- **THEN** subsequent records from that active conversion omit its context, including records from a newly earlier callback
- **AND** a newly admitted child can use its own context without reviving the older parent's identity

#### Scenario: An older conversion body rejects entry
- **WHEN** retained older conversion work cannot acquire trusted context under the current coverage
- **THEN** its diagnostics and the remaining parent work have no borrowed parent identity
- **AND** the parent's existing cleanup still restores its caller

Later external removal of the observation callbacks is not fully observable. In particular, removing
the body observer while also placing reporting ahead of the surviving entry guard leaves an interval
that this integration cannot cover; complete removal of the observers is likewise unsupported.
These limits do not relax initial coverage checks or claim compatibility with arbitrary removal.
