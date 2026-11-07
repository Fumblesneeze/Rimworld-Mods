# Readable Numbers: source research and draft decisions

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). S034, **1.7 beta**.
Inspected **2026-11-03**. [Owned change](../../../../openspec/changes/specify-megamod-readable-numbers/proposal.md).
This is static research/specification, not executed upstream code or native gameplay acceptance.

## Documentation first and exact inputs

Read the [Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=3762441264)
and [public repository/README](https://github.com/7h0m4s/RimworldReadableNumbers) before source.
Public docs describe configurable punctuation/grouping, minimum length, small decimal conversion,
text exclusions and broad shared-UI coverage. The dedicated Steam changelog endpoint failed to load;
the description's embedded changelog and dated author comments were available. They report the
1.7 port and an October26 interface-change fix. These are provenance/compatibility leads, not proof
of current live behavior or universal mod support.

- Installed root: `F:/Steam/steamapps/workshop/content/294100/3762441264`.
  **Readable Numbers**, **7h0m4s.RimworldReadableNumbers**, About author **7h0m4s**; Steam credits
  Fifbef and DubiousPhysics. About declares 1.6/1.7 and required **brrainz.harmony**; DLC entries
  are loadAfter hints, not dependencies. No LoadFolders.xml exists; selected input is its 1.7 DLL.
- Read all **26 C# source files**, project, About, Analyzer.xml and English keys: **30 text inputs**
  match the pinned public checkout after newline/BOM normalization.
- Checkout: `artifacts/Refinement/megamod-spec-20261103/readable-numbers/upstream`, commit
  **3cb3f83b8e4881f5bdbc3b2ac8452e60db7b707f**, 2026-10-27 +10:00, GuiToDevicePoint interface fix.
  Source project: net10.0, UnityStubs/Waylith/native references, Lib.Harmony3.0.0-rc.2.
- Selected DLL decompiled read-only under this artifact root's `compiled`, using ILSpy11.2.
  Assembly/file **1.0.0.0**, informational version
  **1.0.0+6ab7fae73d2fcb6ebbbe9affd309dbf962b36343**. Do not equate that build identifier to the
  later checkout commit; checked compiled core/settings/backend/RimHUD paths agree on the findings
  below, including the added GuiToDevicePoint forwarder.
- Source LICENSE.txt declares **GPL-3.0**. No source code, assemblies or art are copied into the
  owned product. Retain attribution/research provenance; code reuse would need a separate license review.

| Input | SHA-256 |
| --- | --- |
| About/About.xml | `80467F4454E7D0635DD4C2C159C80A75E217BF314927DAFEDC23EBC76C023AEB` |
| 1.7/Assemblies/RimworldReadableNumbers.dll | `DCA05AA6C2B859B94C001C75761309A16380F25B1E35F44F387ECFCB6B93A240` |
| Native UnityStubs.dll, 1.7.4995 rev309 installation | `07B33DC47D1C5136B50793AE11F566261ED844CB4E7D9DBE7A441D0954F5A90C` |
| Native Assembly-CSharp.dll | `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21` |
| Installed RimHUD root Assemblies/RimHUD.dll | `D24BB116949DA4F2EEFCF350CB73E20B014A9DB28D922DDAE256D11D59DB206F` |

## Complete source inventory and effective behavior

Eight core files: RnMod, RnPatcher, RnSetting, RnGuiBackend and Utility/{Patching,Processing,Text,
Validation}; eighteen patch files listed in the surface table below. Source/compiled facts:

- RnMod replaces **GuiBackend.Current** with a forwarding **RnGuiBackend**. MeasureText and DrawText
  format a local text argument; all other backend operations forward. This is a render/measurement
  interception, not a numerical-stat change. RnPatcher uses unconditional Harmony.PatchAll with
  owner **7h0m4s.RimworldReadableNumbers**; neither approach meets our per-target admission rules.
- Processing.ProcessLabel exits for null, length<=2, length>32766, disabled formatting or its global
  suppression flag. It caches original string → changed string or null meaning unchanged. It then
  performs label-level validation, tokenization, per-token validation/formatting and reconstruction.
- Tokenization recognizes char.IsNumber and literal `.` decimal syntax, splitting surrounding
  signs/units/text. Only the literal `<color=` tag receives special protection. Multiple periods
  invalidate a number token. It does not parse a floating value: grouping manipulates characters
  from right to left, retaining original digits and fractional zeros.
- Grouping modes: rightmost groups of3; rightmost3 then2; groups of4; none. Decimal replacement is
  independent in the formatter. There is no rounding, quantity mutation or automatic two-place
  decimal insertion. Source examples showing `.00` are examples, not a precision requirement.
- **Threshold defect, code-derived:** MinimumDigits is checked only in the label prefilter; token
  validation still uses a hardcoded 3-digit boundary. At threshold5, `1234 / 12345` admits the whole
  label and can format both tokens. Enabling small decimal conversion can also admit a label without
  any qualifying long integer. Owned thresholds are per number, based on integer digits only.
- Source checks Unicode IsNumber rather than only decimal digit sequences, and only `.` input
  decimals. Identifiers such as M1911 can be tokenized and changed; generic foreign decimal/group
  representations are not reliably identified. Native name labels are not generally protected.
- **Exclusion defect, code-derived:** literal character comparison is case-sensitive, but its
  hand-written matcher resets without rechecking overlaps. Pattern `aab` can miss `aaab 12345`.
  A match skips the whole raw label. Empty UI entries can be added; save load filters empty entries.
- **Bounds/reentrancy risks, code-derived:** short counters and fixed32767-character token/result
  storage bound input but not worst-case output expansion. Shared static working state and a single
  suppression bool do not preserve nested prior state or exception recovery. These are not live
  reproduced crashes; implementation must test safe boundaries and restore scope reliably.
- Cached outputs are keyed only by original string. ExposeData clears the cache, as do exclusion
  add/remove. Ordinary setting edits/reset do not consistently clear cached results at the edit
  itself. The cache clears on insertion only after Count>capacity, not as an exact bounded LRU.
  Initialisation constructs a dictionary through another dictionary, not a measured optimization claim.

### Source settings and persistence

All source preferences are static values serialized in global ModSettings. There is no game-save
component or per-save inheritance. The owned common-model changes are deliberate.

| Source option | Default / UI validation / save key |
| --- | --- |
| Enable | true; `enable` |
| Debug logging | false; `debugmode` |
| Apply decimal format to all numbers | true; `formatallnumbers` |
| Minimum number length | 4; IntAdjuster minimum4, no explicit smaller UI choice; `minimumdigitsforseparatorformatting` |
| Punctuation | CommaPeriod; six presets + Custom; `numberformat` |
| Custom pair | comma/period; one UTF-16 char UI buffers, empty falls back to defaults; `customseparatorchar`, `customdecimalchar` |
| Grouping | ThreeDigits; alternatives ThreeThenTwoDigits/FourDigits/None; `separatorgrouping` |
| Cache | enabled, capacity25000; numeric UI100..1000000; `cacheenable`, `cachemaxcapacity` |
| Exclusions | empty; strings joined with `|`; `blacklist` |

Space presets use **nonbreaking space U+00A0**. Presets display examples; grouping examples always
use comma/period rather than the current punctuation. Custom input accepts unsafe/ambiguous same
characters and warns rather than validating. ResetToDefault **does not reset SeparatorGrouping**.
Blacklist serializing escapes `|` as `\|`, but deserialization splits on every `|` first, so that
escape is not a round-trip for a literal vertical bar. Duplicate entries are checked by exact string.
Removal uses a checkbox that marks an entry and removes it on a later settings draw. Owned explicit
add/remove, exact storage and full reset are corrections, not claimed source behavior.

### Native and optional surface inventory

| Source patch group | Actual purpose / limitations |
| --- | --- |
| GUI.TextFieldImpl | Prefix true/postfix false suppression, keeping editable fields raw; no exception-safe nesting |
| DateReadout.DateOnGUI; VersionControl.DrawInfoInCorner; DevGUI.Label; Dialog_ModSettings.DoWindowContents | Suppress formatting across those draws |
| Dialog_FileList.DoWindowContents and DrawDateAndVersion | Suppress save/file-list text and metadata; nested bool resets are not restoration of outer state |
| MainTabWindow_History.DoArchivableRow | Replace first Widgets.Label(Rect,string) with a suppressing wrapper, intended timestamp protection; no semantic check that first call is still date |
| MainTabWindow_Quests.DoRewards | Replace single-argument Translate calls with resolve-then-format helper to preserve reward translation/color semantics |
| QuestPartUtility.GetRewardStackElementsForThings, two enumerators | Replace Text.CalcSize with formatting measurement helper for remembered/live item reward labels |
| GenMapUI.DrawThingLabel and DrawText | Format string before measuring/drawing, then suppress downstream formatting until postfix |
| GenMapUI.GetPawnLabel and DrawPawnLabel | GetPawnLabel postfix formats returned name and turns suppression on; DrawPawnLabel postfix resets it. Not a blanket name exclusion; GetPawnLabel also has measurement callers |
| RimHUD.TextExtensions.ToDecimalString | Package-gated postfix replaces **every period** in result with selected decimal mark, independently of Enable/exclusions/small-decimal option |
| ToggleableReadouts.ReadoutCache constructor/Update | Reflection lookup; mutate `valueLabel` to formatted text. Stored display value complicates later reformatting/disable behavior |
| DawnNewDay.DawnComponent.DrawText | Package-gated suppression of date overlay |
| Dubs Performance Analyzer | Enumerated panel/row/graph/search/slider/color-picker methods suppressed; FPS/TPS hook deliberately commented out, so not all analyzer text is excluded |

Exact current native types decompiled under `native`: UnityStubs.Gui.IGuiBackend, UnityEngine.GUI,
Verse.GenMapUI. The interface includes GuiToDevicePoint; its addition broke earlier source builds
before normal patch admission, according to author reports. Native TextFieldImpl uses input/caret/
selection state and the backend, so changing measurement inside editable input is not harmless.
Native GetPawnLabel returns truncated Pawn.LabelShortCap; changing it after truncation can enlarge
the label. Native GenMapUI measures text/background widths, requiring one consistent formatted value.
Remaining effective bodies/foreign interactions are RN-Q1 before implementation, not proven by signatures.

## Provider and consumer audit

- Installed **RimHUD**, Workshop1508850027, **Jaxe.RimHUD**; its compiled ToDecimalString(int,int)
  was read under `rimhud`. It returns a bold integer and, if Theme.ShowDecimals is on, a literal
  period plus a smaller two-digit remainder (>=100 displayed99). Preserve that existing choice/
  precision; do not implement blanket period replacement in tags or a decimal rewrite while disabled.
  S127/S128 are selected future owned features: compose directly, not through our own Harmony adapter.
  Its installed About/LoadFolders support declarations end at 1.6, as recorded in the
  [HUD intake](2026-11-03-rimhud-reference-intake.md). Reading that root DLL is a consumed-contract
  reference, not evidence of supported/running 1.7 integration.
- Installed **Dubs Performance Analyzer**, Workshop2038874626, **Dubwise.DubsPerformanceAnalyzer.steam**.
  This slice reads the source adapter's target catalogue, not every current profiler body. Analyzer.xml
  registers a **ReadableNumbers** profiling group containing Processing/Text/Validation/Patching.
  That is a developer profiler grouping, not a gameplay bottom tab or evidence of external API parity.
- No installed About identity matching **alendio.dawnofanewday** or **owlchemist.toggleablereadouts**
  was found in the bounded case-insensitive metadata audit. Source adapter evidence alone does not
  establish live support. Record absent/present/changed-shape runs before claiming either integration.
- Bounded installed C#/XML/JSON search for RimworldReadableNumbers, DisableReadableNumberFormatting
  and ClearResultCache found this source and its own Analyzer.xml/About, no external source consumer.
  It is not an exhaustive binary/reflection audit. Public static utility methods and the mutable
  suppression flag exist, but do not promise a binary facade without a real caller.

## Owned decisions, deviations and composition

User's confirmed process/UI/common-setting requirements apply. Proposed owned defaults remain the
source's useful presentation defaults: enabled, comma/period, groups3, threshold4, small decimals on,
empty exclusions. Changes are explicit in the spec:

- Per-number threshold, literal overlapping exclusions, exact list persistence, complete reset,
  atomic custom-pair validation and immediate settings/width/cache refresh.
- Protect semantic names/identifiers/input/date/debug contexts; keep broad ordinary numeric UI
  coverage through admitted shared paths. Ambiguous foreign representations stay native. Preserve
  original precision and rich-text attributes, including styled HUD decimals, with no double conversion.
- Common global/save settings for actual preferences, including whole-list exclusion overrides.
  Punctuation mode/custom pair inherit as one value; presets/reload preserve the dormant custom
  pair. Global Restore defaults resets every global value; current-save Use global for all restores
  inheritance while preserving dormant overrides, even when global values differ from built-ins.
  Source cache enable/capacity knobs are deliberately **not copied into ordinary settings**; bounded
  internal caching is a design choice, with developer inspection/logging/clear only in top debug UI.
  No extra bottom tab or bottom-right switch is needed for persistent formatting configuration.
- Use one owned presentation formatter for HUD, search counts, buildable quantities and production
  UI; never copy a cache per source port or patch owned callers. A text-result cache is distinct from
  the shared Thing index: it stores no candidate lists, quantities as authoritative state, or live
  game references. Changing formatting must not alter search criteria, bills, hauling or stock counts.
- Suppress owned overlap when source package is active; do not remove source, spoof ID or migrate
  its settings automatically. Independently admit external integrations and coupled generic protection/
  measure/draw paths. Unsupported protection must not leave a generic renderer corrupting that surface.

## Open research and acceptance

| ID | Topic | Remaining work |
| --- | --- | --- |
| RN-Q1 | Effective native target/admission | Resolve exact generic quantity contexts, measurement/draw/input/name/date protections, semantic replacement positions and safe beta/backend drift isolation before hooks |
| RN-Q2 | Optional contracts and consumed APIs | Current profiler bodies, absent Toggleable/Dawn provider evidence, external binary/reflected consumers, RimHUD structured quantity boundaries; no universal support claim |
| RN-Q3 | Formatting edge/performance evidence | Unicode/custom glyphs, markup/ambiguous representations, idempotence provenance, long expansion, nesting, scope-change invalidation and bounded text-cache cost across shared owned consumers |
| RN-Q4 | Implementation/acceptance | Honest-tier focused TDD, seven languages, common settings save switching, actual quest/map/HUD/input/date workflows, package independence and final reviewed-build native evidence |

Semantic pass: display only; no quantities, resource values, work, research, selection or input rules
change. Costs/search results remain the same values under a different presentation. Format-wide
compatibility and performance remain unverified until measured/native workflows. Document validation
and independent review are recorded in the [progress ledger](specification-progress.md).
