# Readable-number implementation boundary

Owner: `fumblesneeze.megamod`, S034. This is an incomplete host-tested milestone, with no runtime
formatting hook or native acceptance yet. The full owning OpenSpec remains authoritative.

`QuantityFormatter.Format(RawDecimalQuantity, NumberFormatOptions)` currently formats one
caller-identified raw decimal token. The caller supplies its unchanged prefix and suffix, including
units/currency. The token must have an optional leading ASCII sign, integer ASCII digits, and an
optional period followed by fractional digits. Unsupported token shapes return the complete original
label. No numeric parsing, rounding or source-data mutation occurs.

The formatter groups integer digits from the right using the four specified patterns, applies the
minimum integer-digit threshold separately to each supplied quantity, and changes decimal punctuation
according to the small-number option. Space group marks become U+00A0. The immutable input retains
the original representation for another render; output strings must not be reintroduced as raw
tokens. In particular, `1.234` must not be guessed to mean either grouped thousands or a decimal:
the caller must establish raw representation provenance first.

The existing settings registration includes Enable, Grouping, Minimum integer digits and the
small-number decimal option. The new format
options are a pure value input, not another persisted settings store. A validated composite
`ReadableNumbersSettings.Punctuation` definition now exists: preset plus dormant custom pair,
GlobalAndSave/NextDisplay, one Unicode scalar per mark and strict typed JSON fields. Its 29 host
cases cover atomic rejection, malformed persisted data and whole-preference inheritance/persistence.
`NumberPresentation.Format(quantity, store, globalDefault)` now resolves the selected scope's Enable,
punctuation, grouping, threshold and small-number decimal choice through that common store. All seven presets preserve dormant custom marks;
supplementary marks remain complete strings. Disabling restores the complete original quantity,
including decimal punctuation. Scope changes are read on each call, with no presentation cache.
The native composite row uses this same projection for negative, large and small saved-choice previews.
Draft marks stay provisional until the complete pair is valid and Apply is clicked. Preset changes
preserve the stored custom pair, and common inheritance/activation resets clear the provisional editor.
Inherited/main-menu preset controls and unavailable Apply controls draw a fixed native button
background with the native inactive tint and a static label. They do not call ButtonText or register
its hover/click sound path. The preset's click-help tooltip is shown only when that control is enabled.
Temporary GUI color, text anchor and wrapping are restored in finally; the common callback guards
remain authoritative. This source-reviewed behavior still needs native disabled-state observation.

The slice remains opt-in for root integration after review: register
`ReadableNumbersSettings.Punctuation` in the store before runtime initialization, and call
`ReadableNumbersSettingsUi.RegisterPunctuation(renderer)` after ordinary feature row registration.
The existing Register methods deliberately do not enable this row yet. The common renderer owns
global/save callbacks, disabled save controls and stale binding rejection. No additional persistence
authority or source-mod UI is introduced. All seven feature catalogs contain the same 30 keys.
Braille pattern blank and modifier plus/minus signs are explicitly rejected in both mark roles;
Unicode symbol classification alone does not establish visibility or acceptable meaning. Actual
glyph availability and visibility in the game's selected font still require native verification.
`numbers.grouping` is a typed NumberGrouping preference, default ThreeDigits, order20. Its exact-name
codec accepts only one of ThreeDigits/ThreeThenTwoDigits/FourDigits/None; invalid edits reject and
malformed stored fields use the row's built-in value without rewriting the payload. `numbers.smallDecimals`
is Boolean true, order40. Both are GlobalAndSave/NextDisplay, registered through the existing feature
Register methods. The grouping editor cycles the four valid choices; the small-number row uses the
common Boolean editor. Each has a 28-unit editor and 160-unit complete common row. Punctuation's
228-unit editor/360-unit row and explicit RegisterPunctuation API are unchanged.

NumberSettingsButton is the feature-local extraction of the existing inactive button drawing path;
grouping and punctuation share it, with explicit callback eligibility checks. No new renderer API is
required. Native scenarios and pending input/render observations are in [NativeScenarios.md](NativeScenarios.md).
Exclusions, reset actions and their UI remain pending.
Until validated preference integration exists, this formatter is not a public arbitrary-user-input
configuration boundary. It currently expects a valid punctuation pair and threshold from its caller.

Remaining formatting work includes exclusion application, styled quantities, generic label
identification and protected contexts, repeated measure/draw provenance, excessive-expansion fallback,
native/optional semantic admission, invalidation and complete native workflows. This narrow token API
does not reduce the broad supported shared-display obligation to a handful of owned screens. Any
additional accepted source representations require focused tests and inspected context contracts.

Read-only source research used the retained upstream README and Utility/Processing.cs and Text.cs.
The source's global mutable buffers/cache/token state and label-level prefilter were inspected as
behavioral evidence only; none was copied. The owned implementation follows the specified per-number
eligibility and immutable raw-input boundary. Exact source and test hashes are retained in the local
P0-B ledger under `artifacts/Megamod/P0-B/tdd-ledger.md`.
