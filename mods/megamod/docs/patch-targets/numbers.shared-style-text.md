# numbers.shared-style-text: paired native quantity text

Owner: `fumblesneeze.megamod`, S034. Candidate research document only: no patch, executable validator,
registration or native acceptance exists yet. The paired-path/protected-context requirements in
`openspec/changes/specify-megamod-readable-numbers/specs/megamod-readable-numbers/spec.md` govern this work.
The formatter remains safe to call directly only with known raw quantity provenance.

## Inspected target and reason

Current 1.7.5000 `UnityStubs.dll` SHA-256 is
`EA1DFBAEA8D21CF838EFE30B1417949843372908D14BA070AA4C192DB8DA2714`; this identifies research, not an
admission predicate. Read-only decompilations are retained in
`artifacts/Megamod/P0-B/native-1.7.5000/`. Native `Verse.Text.CalcSize/CalcHeight` passes the text through
`GUIContent` into GUIStyle measurement. `Widgets.Label(Rect,string)` calls `GUI.Label` and GUIStyle
drawing. XML cannot change these runtime text arguments. No owned UI method would be patched.

Candidate atomic group has two eventual argument-prefix targets in `UnityEngine.GUIStyle`:

- `private instance UnityEngine.Vector2 MeasureText(string text, float wrapWidth)`.
- `private instance void DrawTextIn(UnityStubs.Gui.IGuiBackend be, UnityEngine.Rect content,
  UnityEngine.Rect position, string text, UnityEngine.Color color, UnityEngine.TextAnchor anchor)`.

The measurement method forwards `text` to the backend's MeasureText call when a backend exists,
otherwise to `FallbackMeasure(text,wrapWidth)`. Drawing forwards `text` to `be.DrawText` when clipping
allows rendering, retaining rectangle/font/anchor/wrap/color/clip arguments. No backend wrapper or
implementation of the volatile IGuiBackend interface is proposed. Native TextField drawing uses
backend calls directly with its native caret/selection state; that does not establish complete input
protection because nested style measurement and future changed callers still need analysis.

## Required executable predicates before registration

1. Resolve both exact non-generic callable shapes and the consumed backend signatures without an
   eagerly constructed replacement backend. Pair failure disables both transformations.
2. Verify text argument flow into the relevant backend/fallback operands, allowing unrelated IL,
   offsets, local numbers and compatible foreign Harmony owners. For measurement, both backend and
   fallback must consume the same raw text argument. For drawing, preserve the native clipping guard
   and all other backend operands. Names alone are insufficient.
3. Establish original/effective-body semantics and foreign prefix/transpiler interactions through
   the admitted C01 path. A skip-original path does not prove native consumption. Do not run arbitrary
   foreign transpilers speculatively. Later effective repatching must revalidate or safely disable
   the dependent group; the exact executable strategy remains pending.
4. Admit all dependent context protections before generic interception: inputs/settings except the
   deliberate preview, names/identifiers, native dates/history/file/version/profiler surfaces and
   optional providers whose unsafe display cannot otherwise be isolated. Nested contexts restore in
   guaranteed cleanup, including exceptions. This list is a requirement inventory, not yet a proven
   complete caller catalogue.
5. Retain raw representation/protection context across measure/draw/refresh. Unknown already-formatted
   strings remain native. Rich-text visible spans and attributes must be handled together; formatting
   output is not reparsed as raw input. One effective preference revision governs both dimensions
   and drawing, including upstream cached widths and truncation.

Outstanding: exact provenance propagation, protected caller targets, native cache invalidation,
GUIStyle truncation and rich-text handling, effective-body validator/owned Harmony fixtures. These
must be resolved before enabling this candidate. Safe direct owned quantity calls remain available
when the generic group is unavailable; this is not a reduction of the full shared-display obligation.

2026-11-07 follow-up source inspection for the punctuation preview reconfirmed both argument paths.
GUIStyle.GetNumCharactersThatFitWithinWidth repeatedly measures UTF-16 substrings, while its fallback
measurement also walks UTF-16 code units. A generic hook cannot assume those fragments preserve a
supplementary mark or complete quantity. This is an additional reason to keep the shared-style group
unregistered until provenance/truncation semantics are demonstrated. The owned preview passes a
complete already-formatted string to native Widgets.Label and does not install any GUIStyle patch.

## Failure and native observation

Only an enabled candidate whose complete predicates pass can register. Missing/ambiguous semantics,
exceptions, installation failure or required-protection failure leave native text intact, remove only
newly installed owned members of the atomic group, and emit a bounded startup warning naming the
patch ID, failed predicate, unavailable generic formatting and native fallback. An active original
`7h0m4s.rimworldreadablenumbers` suppresses owned overlapping formatting without changing the provider.

Required future tests cover supported/benign/unsafe operand flow, paired rollback/repeated admission,
compatible foreign patches, skip-original, nested failed scopes, repatching and unchanged malformed
labels in the honest Unit/Harmony tiers. Native acceptance must open actual stats, quest rewards and
map quantity labels, change formatting through settings and observe matching text/space; then edit,
select/copy numeric input and inspect dates/history/file metadata. Before/action/after captures must
show preserved quantities, precision, markup and native actions. No current host pass proves this.
