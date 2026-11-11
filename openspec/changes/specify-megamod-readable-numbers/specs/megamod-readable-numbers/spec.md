# Readable numbers

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target **1.7 beta**.
S034 Readable Numbers. Specification only; nothing implemented or verified in game.

## ADDED Requirements

### Requirement: Change presentation without changing quantities
The feature SHALL format ordinary displayed quantities while preserving their sign, integer and fractional digits, leading/trailing zeros, units, currency/percent symbols and surrounding meaning.
It introduces no rounding, added precision, abbreviation, conversion, resource change or altered
sorting/calculation. Integer displays do not gain decimal places. Existing nonnumeric labels such
as Infinite and special numerical representations retain their existing meaning. Formatting is
enabled by default and applies to supported ordinary game and modded UI using shared display paths,
including resource/stack counts, costs, stats, tooltips, quest reward quantities/values and map labels
that represent quantities. It changes neither stored names nor raw data, save quantities, clipboard
data copied from input fields, numeric entry parsing, bill targets or gameplay decisions.

#### Scenario: Display a signed amount
- **WHEN** an ordinary quantity is displayed as `-1234567.00 kg` with default formatting
- **THEN** it appears as `-1,234,567.00 kg` with its two fractional zeros and actual quantity unchanged.

#### Scenario: Read a large bill target
- **WHEN** a finite target is `1234567`
- **THEN** its display is `1,234,567`, while production still uses the same finite target and does not treat it as Infinite.

### Requirement: Group integer digits in the chosen pattern
The player SHALL choose Three digits, Three then two digits, Four digits or None, with Three digits as the built-in default.
Group from the right side of the integer part only. Three then two places the rightmost three
digits together and then groups remaining digits in pairs. None inserts no group separators;
decimal punctuation remains independently configurable. Fractional digits are never grouped.

#### Scenario: Compare grouping patterns
- **WHEN** `123456789.25` is displayed with comma grouping and period decimal punctuation
- **THEN** the four modes show `123,456,789.25`, `12,34,56,789.25`, `1,2345,6789.25`, and `123456789.25`, respectively.

### Requirement: Apply the threshold to each number separately
The Minimum integer digits setting SHALL default to four and accept whole numbers from four through 2,147,483,647.
Count only integer digits, excluding sign, punctuation, units and fractional digits. A quantity
receives grouping only when its own integer part meets that threshold and the chosen group pattern
has a boundary to insert. Another longer number elsewhere in the same label cannot make a shorter
number eligible. Invalid/out-of-range input retains the last valid setting with an explanation.

#### Scenario: Mixed lengths in one label
- **WHEN** the threshold is five and a label contains `1234 / 12345`
- **THEN** it displays `1234 / 12,345` with Three digits mode, rather than grouping both numbers.

#### Scenario: Fractional length is not integer length
- **WHEN** the threshold is five and a quantity is `1234.56789`
- **THEN** the long fractional part does not cause grouping of its four integer digits.

### Requirement: Separate decimal punctuation from grouping eligibility
Apply decimal format to small numbers SHALL default to true.
When enabled, the chosen decimal mark applies to supported decimal quantities even when their
integer part is below the grouping threshold. When disabled, below-threshold quantities retain
their original decimal punctuation. At or above the threshold, decimal punctuation still follows
the selected format even when grouping mode is None. The input's existing fractional precision and
zeros remain intact; an integer remains an integer. This decision is per quantity, not per label.

#### Scenario: Format a small mass
- **WHEN** period-group/comma-decimal formatting is chosen for `9.18 kg`
- **THEN** the small-number option shows `9,18 kg` when on and leaves `9.18 kg` when off.

#### Scenario: Disable grouping but retain decimal choice
- **WHEN** grouping is None and a qualifying value is `12345.50` with comma decimal punctuation
- **THEN** it displays `12345,50` without grouping or a change in precision.

### Requirement: Offer meaningful punctuation choices
The feature SHALL offer comma/period, period/comma, space/period, space/comma, apostrophe/period and apostrophe/comma grouping/decimal pairs, plus Custom.
The built-in choice and dormant custom pair are comma/period. Space grouping uses nonbreaking
spaces so an individual number does not split at group separators. The selected preset and saved
custom pair form one punctuation preference with one inheritance choice; the two marks cannot inherit
from different scopes. Selecting another preset retains the saved custom pair, and returning to
Custom restores it, including after saving/loading. A custom pair requires one
Unicode scalar for each role and distinct characters. A supplementary-plane symbol counts as one
character; a multi-scalar sequence or unpaired surrogate is invalid. A group separator can be a printable punctuation
or symbol character, or a space treated as nonbreaking; a decimal mark must be a visible printable
punctuation or symbol character. Digits, letters, plus/minus signs, markup delimiters `<`, `>` and
`&`, line/control/format characters, combining-only marks, empty input and multiple characters are
rejected with an explanation. The ordinary space is the only invisible character accepted, and only
for grouping; it is rendered as a nonbreaking space.
Edits are provisional until the complete pair is valid; invalid input cannot silently replace it
with another pair or corrupt the effective format. Provide examples of negative, large and small
decimal quantities for the current choices.

#### Scenario: Choose custom punctuation
- **WHEN** the player chooses `#` for grouping and `@` for decimals
- **THEN** a qualifying `1000000.00` displays as `1#000#000@00`, and the settings preview shows the same rule.

#### Scenario: Reject an ambiguous pair
- **WHEN** the player enters identical marks, a digit, a newline or an empty decimal mark
- **THEN** the UI explains the invalid entry and continues using the previously valid pair without changing saved settings.

#### Scenario: Return to a saved custom pair
- **WHEN** a scope has saved Custom `#/@`, switches to comma/period, saves/reloads and then selects Custom again
- **THEN** `#/@` returns unchanged; selecting the preset did not overwrite the stored custom pair.

#### Scenario: Validate complete visible characters
- **WHEN** a custom mark is a single supplementary-plane punctuation or symbol character
- **THEN** it is treated as one mark rather than rejected for occupying two UTF-16 code units.
- **WHEN** a mark instead contains a zero-width format character, a combining-only mark, an unpaired surrogate or multiple Unicode scalars
- **THEN** the entire proposed pair is rejected with an explanation and the previous preference remains effective.

#### Scenario: Inherit a complete punctuation preference
- **WHEN** a save switches Use global on while global punctuation uses Custom `#/@`
- **THEN** both marks and the selected Custom mode follow the global preference together; turning inheritance off restores that save's entire dormant punctuation preference rather than mixing marks from different scopes.

### Requirement: Preserve text that is not a quantity
The feature SHALL leave editable text fields, pawn/item names, technical identifiers, dates/year readouts, save/file names, versions, debug/profiler text and numeric markup attributes unchanged.
This protection includes native date display, history message timestamps, file-list metadata and
version-corner text. Ordinary settings fields retain their original values; the Number formatting
preview deliberately shows formatted examples. A quantity next to a name can still be formatted
when its role is known without changing the name. Generic text formatting handles standalone
decimal quantities; it does not extract digits from alphanumeric identifiers such as `M1911`.
Attached unit/currency/percent text is supported when the display context identifies the quantity
and unit; unsupported ambiguous text is preserved, not guessed. Already formatted foreign text,
dates embedded in unknown syntax, scientific notation, hexadecimal and malformed numeric sequences
are left intact unless an admitted context explicitly identifies their quantity representation.

#### Scenario: Keep a weapon name intact
- **WHEN** a row identifies an item as `M1911` and separately displays a quantity of `12345`
- **THEN** the name stays `M1911` and the quantity can display as `12,345`.

#### Scenario: Edit a target
- **WHEN** the player enters, selects, copies or edits `12345` in a numeric field
- **THEN** the input, caret, selection and copied text remain native; formatting applies only to a separate supported display.

#### Scenario: Read history and file information
- **WHEN** a message timestamp contains year `5500` or a save entry contains a version/file identifier
- **THEN** the date and identifiers remain unchanged while a separate ordinary reward quantity can still be formatted.

### Requirement: Preserve rich text and avoid repeated transformation
Formatting SHALL preserve visible emphasis, colors, links and all markup attributes and SHALL format an identified quantity only once through a display operation.
Numeric portions split by styling, including a bold integer with a smaller fractional part, retain
their styles and behave as one identified quantity. Measurement, drawing and repeated refresh do
not insert duplicate separators or reinterpret a formatted group mark as an original decimal mark.
For unknown already-formatted input, preserve the original text until its representation is known.
If the input cannot be safely processed, including malformed markup or excessive text expansion,
show the original complete label rather than a partial/truncated rewritten label or an exception.

#### Scenario: Display a styled HUD decimal
- **WHEN** an admitted HUD quantity has a bold `1234` and smaller `.50` fractional display
- **THEN** period-group/comma-decimal formatting shows the equivalent `1.234,50` with the original styles and unchanged markup attributes.

#### Scenario: Measure then draw again
- **WHEN** the same quantity passes through measurement, drawing and later refresh
- **THEN** it has the same single correctly formatted representation every time, including formats where a group mark is a period.

### Requirement: Exclude selected labels literally
The player SHALL be able to maintain a text exclusion list, empty by default, whose entries match literal case-sensitive substrings of the original visible label before numeric formatting.
Any matching entry leaves that entire label unchanged. Matching is not a regular expression and
recognizes overlapping occurrences. Reject empty entries and exact duplicates without altering
the list. Preserve meaningful spaces, punctuation, backslashes and vertical bars through save/load.
Provide explicit add/remove controls. An inherited list follows the current global list; a save
override is a complete independent list rather than an implicit union. Exclusions cannot disable
the protected-text rules or cause input fields to be formatted.

#### Scenario: Exclude overlapping text
- **WHEN** `aab` is excluded and the original visible label is `aaab 12345`
- **THEN** the entire label is unchanged because it contains that literal substring.

#### Scenario: Preserve punctuation in an exclusion
- **WHEN** a user saves an exclusion containing a vertical bar and backslash and reloads the game
- **THEN** that exact entry and its matching behavior survive without being split into extra entries.

### Requirement: Keep text measurement and controls consistent
The feature SHALL measure and draw the same effective display text and preserve the surrounding control's action, hit area and native interaction rules.
Quest reward currency/value labels remain visible, map quantity backgrounds fit the displayed
number, and ordinary tooltips/readouts retain their existing layout behavior. Where a supported
fixed-width surface truncates text, retain the native truncation/tooltip or equivalent full-value
access instead of overlapping controls or suppressing the value. A changed format invalidates
affected display widths and cached text together. Formatting one excluded/nested/failed drawing
context cannot leak into unrelated subsequent labels or disable their formatting for the session.

#### Scenario: Display quest rewards
- **WHEN** a quest reward shows a large quantity or currency value
- **THEN** grouping is visible with its original reward/currency meaning and the label's measured space matches the text actually drawn.

#### Scenario: Finish an excluded field
- **WHEN** an excluded date/input/profiler draw completes or fails, followed by an ordinary quantity label
- **THEN** the ordinary label still follows its own formatting rules and the excluded context's state does not leak.

### Requirement: Use common configuration and honest scope
All player formatting preferences SHALL use the common settings system under an ordered Number formatting section: Enable, punctuation/custom pair, grouping, minimum integer digits, small-number decimal option and text exclusions.
Each applicable row provides Global default, Current save and Use global; new/missing save entries
inherit global. A save override does not modify globals or another save. Re-enabling inheritance
retains dormant overrides, and inherited values follow later global edits. At the main menu global
controls remain usable while every save control and write path is disabled with an explanation.
Valid changes take effect by the next normal redraw, including existing labels, with no restart.
Malformed stored preferences use that complete row's built-in value with an explanation in their
existing scope. An explicit save override with a missing value remains an invalid override rather
than silently becoming inheritance; reading it does not rewrite its payload or another scope.
Global Restore defaults restores all listed built-in global values, including the preset/custom
pair, grouping and exclusions, without changing save overrides or their inheritance flags.
The current-save reset is labeled Use global for all: it enables inheritance for every formatting
preference while retaining every dormant save override unchanged. Its effective values are the
current globals, which can differ from built-in defaults; it does not claim to reset those globals.
Leaving/switching saves applies the appropriate effective settings
without stale text from the previous scope. Main-menu display uses global values.

#### Scenario: Override one save
- **WHEN** save A uses comma decimals as an override while global defaults use period decimals
- **THEN** A follows its override, inherited save B follows globals, and switching between them updates already displayed quantities on redraw.

#### Scenario: Restore all defaults
- **WHEN** the player resets the global Number formatting section after choosing Four digits and custom exclusions
- **THEN** Three digits, comma/period, threshold four, small decimals on, Enable on and an empty exclusion list are restored globally, without changing independent save overrides.

#### Scenario: Configure from the main menu
- **WHEN** the section is opened without a loaded game
- **THEN** global preferences and preview work, while current-save inheritance, edits and resets are unavailable in both UI and write behavior.

#### Scenario: Load an explicit override whose stored value is missing
- **WHEN** the global minimum integer digits is nine and a save explicitly overrides it but contains no stored override value
- **THEN** that save uses the built-in threshold four with an invalid-stored-value explanation, retains its override state and leaves the global nine unchanged.

#### Scenario: Reset a mixed current-save configuration
- **WHEN** globals use Three then two grouping and threshold five, while the save overrides grouping to Four digits and inherits threshold with a dormant override of six, and the player chooses Use global for all
- **THEN** both rows inherit Three then two/five; later unchecking their inheritance restores the dormant Four digits/six values without changing globals.

### Requirement: Separate configuration from diagnostics and internal tuning
The feature SHALL add no bottom tab or dedicated gameplay window; its persistent formatting configuration belongs in mod settings.
Any developer-only formatting/cache inspection, diagnostic logging or explicit diagnostic clear
action belongs to the top debug toolbar and is unavailable outside developer mode. Internal
formatting-cache capacity/enable knobs are not copied into ordinary player settings; normal
formatting must remain correct whether a cached result exists or not. Diagnostics default off
and must not emit routine per-label/per-frame log spam. No separate Thing index is introduced for
number presentation, and no diagnostic action changes a gameplay quantity.

#### Scenario: Developer controls are hidden
- **WHEN** developer mode is off and the player opens ordinary settings
- **THEN** formatting preferences remain available without profiler/cache implementation controls or a new bottom tab.

### Requirement: Compose optional displays and fail safely
Supported external displays SHALL retain their own precision, actions and context rules while applying the effective formatting preferences only to admitted quantities.
For external RimHUD, preserve the HUD's choice to show/hide decimals and its existing fractional
digits/styling; disabling owned formatting or excluding the label also prevents decimal-mark rewriting.
For selected owned RimHUD, use the same owned formatter directly. An admitted Toggleable Readouts
integration updates existing readouts after preference changes; an admitted Dawn of a New Day
integration preserves its date text; profiler/debug output remains unformatted. These providers
are optional. Missing/incompatible adapters leave those displays native and disable only that
integration when isolation is safe. If the generic formatting path cannot isolate an unsafe surface,
disable the dependent generic path rather than corrupt that surface, while retaining safe owned
quantity rendering. No blanket compatibility is implied for uninspected custom renderers.

#### Scenario: Disable formatting with a HUD active
- **WHEN** the player turns Enable off with an admitted external or owned HUD displayed
- **THEN** its original integer/decimal punctuation and styling return without a residual decimal-only rewrite.

#### Scenario: Optional provider changes shape
- **WHEN** a provider's required display contract is missing or incompatible
- **THEN** its integration stays inactive with one bounded startup warning identifying the target/reason and native fallback; other safely isolated formatting remains available.

### Requirement: Admit complete formatting paths and avoid duplicate providers
Every enabled native or optional formatting patch SHALL run only when its documented target prerequisites hold, with compatible foreign patches allowed and one bounded startup warning for each skipped enabled patch.
Coupled measurement/drawing/protection changes apply atomically; failure leaves the affected surface's
original text and interaction intact. A patch changing a label argument must verify that the target
still consumes it as required, not merely that a method name exists. Failure cannot remove foreign
patches, damage stored settings or leave only half of a display path active. If original Readable
Numbers is active, warn once and attempt best-effort suppression through the ordinary owned formatter-off
path with the same behavior and timing as manually off. Retain stored preferences and unrelated owned
displays/actions; do not modify the source, impersonate its package or migrate its settings. An
inactive installed source SHALL NOT trigger this policy. No original API or coexistence guarantee is made.

#### Scenario: Both formatters are installed
- **WHEN** startup finds original Readable Numbers active alongside the owned feature
- **THEN** a bounded startup warning explains best-effort ordinary formatter-off handling, without disabling search, stock counts or HUD actions or rewriting stored preferences
- **AND** the warning does not promise source-only presentation, rollback of prior effects or coexistence compatibility.

#### Scenario: A measurement target is no longer safe
- **WHEN** the required paired measurement contract fails admission
- **THEN** the dependent draw transformation also remains inactive, native text remains usable, and the warning names the failed prerequisite and unavailable formatting.
