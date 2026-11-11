# Content settings UI integration (native acceptance pending)

Owner fumblesneeze.megamod, S020/S038. The first native page uses the ordinary common settings
menu and only the currently admitted incident family. Full catalogue/family/preset UI obligations
remain open. No new preference stores search, scroll, selected source/type or window size.

`ContentChoiceRow` binds exactly one existing ContentSettings Boolean definition to immutable display
metadata and the native identity handle. `ReadState` reads common requested settings and separately
projects the actual applied policy passed by the runtime. Its fields are RequestedAllowed,
AppliedAllowed (nullable), Status, UnavailableReasonKey and ValueErrorKey. Never supply a requested
snapshot as the applied argument. Unsupported/ambiguous/provider/patch unavailability is supplied
as a feature reason; malformed stored data is a distinct repairable validation error.

`ContentChoiceBrowser.Select(query, typeName, sourcePackageId)` reads the reconciled common store and
returns rows in requested-excluded-first, readable-name/source/identity order. Query is a literal
ordinal-case-insensitive substring of one field, unlike the S020 selector's all-word search. Type
and source identities are exact; equal source display names do not merge. Main menu must first use
the common lifecycle reconciliation so browser ordering follows global values. Browsing itself
never owns lifecycle mutation or persistence. The row's no-save status explicitly reads globals.

## Shared integration

- Dynamic Boolean registration takes the real definition, already-localized label/help/status
  delegates, category key, visibility predicate, unavailable-reason key predicate, and optional
  display-rank delegate. Root/B retain the common save/global/inheritance controls.
- Every input and actual commit rechecks feature eligibility; save commits then recheck the captured
  current binding. A stale callback must not edit another save or an unavailable feature.
- A nonpersistent section has stable ID/category/order, localized category key, owned height,
  native draw callback and reset. It hosts search/type/source controls without a dummy setting.
- Renderer snapshots visibility/rank once per draw, keeps category/section order coherent and resets
  transient editor state on the existing activation boundary. No feature comparison mutates data.

The reviewed shared signatures are AddDynamicBoolean(definition, label, categoryKey, help, status,
visible, unavailableReasonKey, displayOrder) and AddSection(id, categoryId, order, categoryKey,
height, draw, reset). `IncidentContentSettingsUi.Register(runtime, settings, renderer, catalog)`
requires the exact finalized catalog supplied to the incident runtime. It maps actual IncidentDef
labels/source names and stable canonical identities to the existing definitions. It does not
register duplicate settings or edit either lifecycle. Root owns the registration call.

`ContentBrowseSession` caches immutable visible ranks until a common-store revision or transient
filter change. Its type/source menus use the full row set, so one filter cannot trap another.
Native FloatMenu entries display full type/package identities; stale native identities cannot
select a replacement. The common renderer rechecks feature eligibility at each setting commit.
Original Cherry Picker, missing identities, ambiguity and unadmitted integration disable edits
while retained choices remain in the common store. A null applied policy is shown as unavailable,
never synthesized from the requested values. Supported globals remain editable in the main menu.
Loaded-game presence comes from native Playing state plus Current.Game, independently of the common
save binding. If that binding/lifecycle is unavailable in a loaded game, the row reports application
unavailable; it must not claim that no save is active. Global controls remain available for repair.

The later C09 safety milestone requires the runtime's DefinitionLifecycle group alongside incident
hooks. Native HotReloadDefs/ClearAllPlayData entry disables incident rows with an explicit localized
Def-lifecycle reason, preserving stored requests. This one-way guard does not implement catalog/UI
reconciliation or recovery and does not apply pending edits. See DefReload-Verification.md.

`IncidentContentRuntime.CurrentAppliedPolicy` exposes the existing immutable policy only under the
same availability gates as PolicyAvailable. Its required fourth constructor argument is the shared
protected warning Action<string>; root supplies that sink so diagnostics do not pass through mutable
Log.Warning when ordinary warnings are muted. Source readiness is not live acceptance.

All seven baseline ContentSelection.xml files contain the page/status strings. Existing translated
native Def labels and source mod names remain the metadata authority. Translation layout still needs
native review, particularly long source labels and narrow viewports.

## Authored native scenario

The selector workflow's 2026-11-11 budget is 450 seconds wall time, MaxFrames = int.MaxValue and
the existing independent 1800-game-tick bound. Seven screenshots × 30 seconds = 210 seconds;
four native window captures, five clicks, two pointer-scope actions and two console actions ×
15 seconds = 195 seconds. The remaining 45 seconds cover synchronous settings-open/menu/window
cancellation, assertions and iteration. The harness supplies separate 30-second cleanup; the
launcher must also include startup/host allowance. Each owned window and speed restoration is
attempted independently during cleanup. These bounds do not establish gameplay acceptance.

`megamod.content.native-selector-global-choice` opens the real mod settings dialog, captures its
native handle/bounds, clicks the type and source FloatMenus and cancels each, then clicks the first
row's real global checkbox off and back on. Read-only snapshots correlate those actions with only
the requested global edit; screenshots show the pending/current status separately. No setting is
written by setup or assertions. The scenario requires a fresh disposable default profile, inherited
incident choices, a loaded admitted policy, an untouched first opening (scroll/filter defaults),
and content as the first registered category. Its geometry is derived from native Window margin 18,
Dialog_ModSettings content offset 40, common category height 32, section height 168 including gap,
and shared editor offset 54. Clicks convert GUI bounds to rendered pixels using captured UI scale.
Changing that layout requires reviewing the scenario coordinates. The orchestrator must personally
inspect the screenshots and reject unavailable or incorrect pending labels; JSON assertions alone
do not prove presentation. Successful execution restores the effective global value through the
native checkbox; a failed run's settings stay isolated to its disposable profile.

The earlier allowed/refused incident scenarios remain separate gameplay checks. This new scenario
does not prove search typing, committing a filter option, save overrides, main-menu editing, reload
application, unsupported/provider guards or seven-language layout. Minimized typed text entry is
still needed for faithful automated search input. No native scenario has been executed by this chunk.

## Remaining native work

Root must integrate/build/review and exercise the incident page. Absent stored entries remain dormant
and need the future complete catalog view; no physical Thing is removed. Build the two local
searchable selector windows with source identity/icons, all-word search/counts, responsive columns,
scroll clamping, one committed selection and cancellation without click-through. Use the inspected
native Window/WindowStack lifecycle; no Harmony patch of owned code is selected.

Read-only 1.7.5000 Window/WindowStack source is retained in
artifacts/Implementation/gateway-native-window-input/native. Native Window has resizeable,
closeOnClickedOutside, absorbInputAroundWindow, doCloseX and closeOnCancel controls; observations of
their source are not live input evidence. Verify exact outside-click consumption, Escape, resize
and fresh-opening behavior through authored minimized native workflows. Main-menu/global edits,
save override/inheritance, pending reload, unsupported guards and the real incident action must be
observed on the reviewed integrated build. No native UI or language acceptance is claimed yet.
