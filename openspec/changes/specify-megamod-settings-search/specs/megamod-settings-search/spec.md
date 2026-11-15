# Cross-mod settings search

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), 1.7 beta.
S141, Needle-inspired universal option search. Query and ordinary-draw recorder implemented;
recorder component verified, full discovery/search/reveal acceptance open.
The 2026-11-14 all-mod clarification supersedes the earlier owned-only draft.

## ADDED Requirements

### Requirement: SS-01 Every mod's settings pages are eligible
The mod SHALL search individual options across all available active-mod settings pages through generic discovery without a package allowlist, per-mod adapter or author-registration prerequisite.
This includes megamod and independently identifiable provider-generated pages. Searchable context
includes readable labels, help/tooltips, observed displayed values, tabs, page/mod names, authors
and package IDs, plus owned feature/category names. Eligibility does not mean unreadable controls
were successfully captured; SS-08 governs gaps. Do not hide disabled/default-valued options.

#### Scenario: Find an option in an unfamiliar mod
- **WHEN** a newly installed active mod exposes a settings page through the normal page collection
  and has no megamod-specific integration
- **THEN** generic discovery attempts its options and finds a readable option by help text even when
  neither its label nor mod title contains the query; a failed capture is reported.

#### Scenario: Options across tabs and duplicate pages
- **WHEN** matching options occur on different tabs or separate provider pages with the same title
- **THEN** each retains its actual owner/page/tab identity and remains independently selectable.

#### Scenario: Find an owned option outside the current section
- **WHEN** the query matches an owned setting's feature/category or help outside the viewport
- **THEN** its real control is discoverable, including disabled rows with their unavailability reason.

### Requirement: SS-02 Query behavior is predictable across result kinds
Search SHALL support case-insensitive readable Unicode substrings, whitespace-separated AND terms, quoted phrases, exclusions and mod/author/id/tab field filters with deterministic ranking and bounded typo fallback.
Match terms across the combined entry context; fields restrict terms to their named metadata.
Rich-text markup is not content. Unknown prefixes remain literal; an unclosed quote consumes the
remaining phrase. Exclusion-only queries filter all entries. Positive unqualified unquoted words
without an exact match permit at most one edit at 5–8 characters or two at 9+; shorter words,
phrases, exclusions and field terms stay literal. Insert/delete/substitute/transpose are edits.
Exact results rank before typo results and labels before help/value-only matches, with stable
page/path/occurrence ties. Empty input restores the page list, not every captured option.
Canonical-equivalent Unicode and normalized whitespace SHALL match; syntactic rich-text tags are
not searchable, while literal comparisons such as `value < 5` remain readable text. Typo fallback
eligibility is determined against the complete current supplied search snapshot before field and
exclusion filtering; an exact occurrence anywhere keeps that word literal.

#### Scenario: Combine owner and option terms
- **WHEN** the player enters `mod:"Example Mod" "work speed" -animals`
- **THEN** matching options belong to that mod/page name, contain the phrase in their combined
  context, and exclude entries containing animals; no setting is changed.

#### Scenario: Unicode, typo and empty results
- **WHEN** the player enters an exact Chinese, Japanese or Cyrillic substring, a qualifying
  misspelled word, an unmatched query and then clears it
- **THEN** readable text matches, bounded typo fallback follows exact results, no-match status
  distinguishes discovery gaps, and clearing restores ordinary page browsing.

### Requirement: SS-03 Results reveal original controls from the native chooser
Selecting an option result SHALL open its actual settings page, navigate its observed tab path, resolve its current control, scroll it into view and highlight it for three real elapsed seconds without editing it.
The entry point is native Options → Mod options, sharing S047's query field when both are enabled.
Page matches and option matches coexist; a page-title mismatch must not hide an option hit.
S141 query syntax governs both result kinds when enabled, while page cards use page metadata only.
S047's grouping/identity rules remain; its disabled sorting does not disable option search.
No new gameplay tab or replacement foreign settings editor is introduced.

#### Scenario: Reveal and edit an offscreen foreign option
- **WHEN** the player searches, chooses a result on another tab, then changes the revealed native
  control and closes/reopens its page
- **THEN** selection alone leaves the value unchanged, the current control is visibly highlighted,
  and the player's subsequent edit persists through the original owner's normal path.

#### Scenario: Target changed or is ambiguous
- **WHEN** the option disappears, changes layout or cannot be uniquely resolved before selection
- **THEN** the correct page opens with an explanation and no guessed click or unrelated edit;
  ordinary navigation remains usable and user navigation cancels pending reveal.

### Requirement: SS-04 Results distinguish current owned values from captured foreign values
Search SHALL preserve value provenance and the original settings owner's persistence, without copying foreign options into megamod's global/save store.
Owned results use current binding, requested/effective values, Global default, Current save/Use
global and unavailable reasons. Main-menu results never invent save values or permit save edits.
Foreign values are marked last observed until a new draw refreshes them; missing values are unknown.

#### Scenario: Value changes after capture
- **WHEN** a foreign option changes after indexing and its page has not rendered again
- **THEN** the result does not call the captured value current, and opening/refreshing the page
  updates the observation without rewriting the option.

#### Scenario: Owned inherited or effective-off option
- **WHEN** the player opens an owned inherited setting or a feature made effectively unavailable
- **THEN** search preserves the stored request, actual binding and common edit guards, including at
  the main menu and after switching saves.

### Requirement: SS-05 Indexing respects lifecycle and bounded work
Discovery SHALL run in bounded sessions at valid GUI phases, refresh affected observations on lifecycle changes, expose progress/cancellation and avoid recurring scans during ordinary gameplay.
Initial capture occurs at startup when enabled or on first enable/open if needed. Querying uses
captured text, not repeated draws per keystroke. Real page renders and explicit Refresh update
observations. Query/highlight are view-local; the index is process-local and never saved to disk.
Language, page-collection and main-menu/save/world changes invalidate affected text, values and
targets before reuse. Conditional rows are discoverable when rendered; do not edit prerequisites.

#### Scenario: Refresh after language or save transition
- **WHEN** the player changes language or moves between main menu and different saves/worlds
- **THEN** stale text, values and reveal targets are invalidated and capture status remains honest.

#### Scenario: Cancel a large capture
- **WHEN** the player cancels discovery or a page reaches its documented work budget
- **THEN** no further callbacks are scheduled for that session, retained results stay usable with
  partial status, and Refresh can retry; no background gameplay scan is started.

### Requirement: SS-06 Generic discovery observes controls without issuing edits
The reader SHALL use neutral bounded page drawing and observed navigation callbacks for discovery, prohibit setting-edit/reset/apply/write actions, and restore capture-owned GUI, event, tab, scroll and temporary-window state in guaranteed cleanup.
Use shape-guarded generic GUI capture rather than a list of supported packages. Do not reflect over
arbitrary foreign setting values. Owned registration metadata is the direct fast path.
Third-party callbacks may have effects the reader cannot undo; known mutation or failed restoration
is a failed page, not successful read-only coverage. Cancellation/budgets apply between callbacks;
do not claim forced preemption or transactional rollback of arbitrary callback internals.

#### Scenario: Traverse tabs without changing settings
- **WHEN** generic capture reads an actual tabbed settings page with sliders, checkboxes and help
- **THEN** its options become searchable, the original tab/scroll is restored, and native before/
  after observations demonstrate unchanged settings and no reset/apply/editor action.

#### Scenario: Callback fails or violates the reader boundary
- **WHEN** a page throws, visibly mutates settings during a draw or fails state restoration
- **THEN** that page is reported failed, further automated calls to it stop for this session, and
  unrelated results plus normal settings access remain available with bounded diagnostics.

#### Scenario: Observe an ordinary native page without adding callbacks
- **WHEN** the generic recorder surrounds one ordinary settings-page draw
- **THEN** it forwards that callback once, publishes bounded immutable partial observations only
  after completion, preserves original editing/persistence, and neither invokes tooltip getters
  nor treats the observed draw as proof of complete page coverage. A failed callback discards its
  observations and preserves the original exception; cleanup preserves a distinct replacement backend.

#### Scenario: A page catches a refused native action
- **WHEN** a read callback attempts to add/remove a window, queue an immediate window, save mod
  settings, write the clipboard or change native IME composition, and catches the refusal
- **THEN** the native action is stopped before its effects and the read remains failed; later
  ordinary player input and native settings operations outside the read remain available.

#### Scenario: Native action guard ordering becomes unavailable
- **WHEN** a required native action guard is missing, has an unreadable callable/order, or loses
  its first-prefix position before a read begins
- **THEN** no automatic read starts, other owners' patches remain intact, and normal page access
  remains available with an explicit discovery gap.

#### Scenario: Native controls normalize values during an idle draw
- **WHEN** a hidden native draw would truncate or normalize text, initialize numeric buffers,
  normalize a listing's minimum integer value, round or clamp sliders/ranges, or clamp scroll
  positions without a player edit
- **THEN** the original outward values and buffer identities are retained, including nulls and
  exact float values; native slider drag sounds and hidden tooltip registration are suppressed.
  Literal help may be recorded, but search never evaluates a lazy help getter. Subsequent ordinary
  native typing and slider input still edit the original visible controls.

#### Scenario: Control composition throws or loses preservation admission
- **WHEN** a controlled native call throws, or another patch prevents the required entry/exit
  ordering or shares its preservation state slot
- **THEN** acquired original values are restored even on exception, nesting cleanup allows later
  independent calls, and unavailable preservation prevents automatic read entry. Other owners'
  registrations remain intact; no full transaction over arbitrary foreign effects is claimed.

#### Scenario: Discovery waits for native input and resumes while focus remains
- **WHEN** the player types, drags, holds or repeats a key/button, moves or scrolls the pointer,
  or has an active IME composition during a possible discovery frame
- **THEN** automatic reads wait until that input is idle, without resetting, consuming or pumping
  it. A focused but idle editor remains eligible, and ordinary typing/dragging still changes its
  native controls. Missing or unverified native input state prevents automatic reads while
  ordinary settings access remains available.

#### Scenario: Reused native listings retain their label scroll state
- **WHEN** hidden drawing submits long labels through a reused native listing and completes or
  throws during its Label/End callbacks
- **THEN** the original label-scroll collection identities, contents and nullness survive the
  read, the visible label keeps its prior scroll position, and subsequent native scrolling still
  works. Unavailable native field shapes or exceeded snapshot budgets prevent the affected read;
  they do not exclude the mod from search or prevent ordinary settings access.

#### Scenario: Read native rows below the current viewport
- **WHEN** an admitted neutral read draws a bounded native listing with an offscreen option
- **THEN** the option's label and literal help can be observed without moving the original viewport
  or changing its value, and subsequent ordinary native scrolling and editing still work. During
  that read the bounds getter does not invoke its scroll callback or populate/change its cache.

#### Scenario: Getter callbacks can hide a read failure
- **WHEN** a getter finalizer can suppress a callback exception before the read's final observer
- **THEN** read admission is refused; an attempted scoped getter call fails before native getter
  effects and leaves the read failed even if its caller catches the exception. Compatible void
  observers and ordinary getter execution outside reads remain available.

#### Scenario: Standard listing culling or a numeric method family changes
- **WHEN** a hidden read uses the native unbounded-listing behavior, or a patch changes any
  published generic numeric listing variant after its discovery contract was captured
- **THEN** only a proved null-to-draw relationship may be used for offscreen rows. A changed or
  unproved relevant composition invalidates that capture, including newly published sibling
  variants; a later refresh cannot revive an old result. Unrelated method changes do not revoke
  the family proof, and ordinary page access remains available without a numeric-type whitelist.

### Requirement: SS-07 Common activation preserves independent browsing
Settings search SHALL default on through one common GlobalAndSave Boolean in Interface/navigation, applying at NextDisplay with normal inheritance, dormant overrides and main-menu guards.
Effective off cancels further discovery and hides option search without changing foreign settings,
S047 grouping or ordinary page access. Later enable schedules missing discovery. Active original
`ZIPender.Needle` receives the standard bounded warning and best-effort ordinary effective-off
handling, retaining stored preferences; an inactive installed copy does not trigger this path.

#### Scenario: Disable and re-enable search
- **WHEN** the player disables search and later enables it
- **THEN** ordinary browsing remains available, values remain unchanged and missing discovery
  resumes only when enabled, with its progress/coverage visible.

#### Scenario: Original Needle is active
- **WHEN** startup detects active Needle
- **THEN** our option search follows its ordinary effective-off path with a bounded explanation,
  preserving stored preferences and independent common settings access.

### Requirement: SS-08 Coverage gaps are visible rather than silently narrowing scope
Search SHALL report per-page pending, partial, failed or unverified discovery and accessible reasons, preserve page opening, and never describe page-name-only fallback as completed individual-option coverage.
Show aggregate discovery status alongside results, including zero-result searches. Unsupported
custom rendering, inaccessible provider pages and budget exhaustion remain outstanding gaps.
Every mod stays eligible; test fixtures or optional provider adapters are not a product whitelist.
An exception-free draw alone does not establish complete control coverage.

#### Scenario: One page cannot be read
- **WHEN** one custom page is unreadable while other mods' options were captured
- **THEN** those results remain usable, the unreadable page is listed with its reason and normal
  access route, and no-match text explains the incomplete coverage instead of claiming absence.

#### Scenario: Generic capture is unavailable
- **WHEN** the engine GUI contract no longer satisfies the reader's shape guards
- **THEN** search reports the cross-mod discovery gap, preserves manual page access and any valid
  owned metadata results, and does not present this degraded state as the all-mod feature completed.
