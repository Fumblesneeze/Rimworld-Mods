# S047 chooser projection milestone

Owner: `fumblesneeze.megamod`. Scope: pure MO-01–04 projection only; no native integration or live
acceptance. All feature tasks, native/provider opening, persistent-owner behavior, controls, seven
catalogs, compatibility admission and native scenarios remain pending.

`Core/SettingsChooser.cs` accepts immutable `SettingsPage` metadata, translated
`SettingsChooserLabels`, and the active language's `CultureInfo`. Construction copies the supplied
page collection. `PageIdentity` and `OpenToken` are opaque dialog-local adapter tokens: each
independently openable page must receive its own identity, including pages sharing title/package.
The model never derives either token from metadata, resolves native objects or executes a callback.
The future adapter owns the token-to-open-route table and positive `IsHugsLibGenerated` proof.

`Project(query = null, byAuthor = true, groupSingles = true)` returns read-only groups and rows.
Name mode returns one heading-less group when matches exist; empty results return no groups.
`DisplayGroup` is null in name mode. Each row preserves its descriptor, safe title, normalized
author, attribution visibility and original ordinal. Author mode counts the complete copied list
before filtering. Strings sort with the supplied culture; package/provider identity ties are ordinal,
then original input order. Search and source alias detection use ordinal case-insensitive substrings.
Exact normalized labels define author groups; raw metadata remains untouched.

The caller retains session-only sort/group choices. Projection has no settings authority or session
global and does not forget a choice while name mode hides its control. Rebuild the descriptor snapshot
on admitted native/provider-list or language changes, not on every rendered frame. Only browsing
changes require another projection. No provider metadata/default lookup belongs in this model.

Safe title order for null/empty title is content name, package identity, then translated untitled
label. Missing/empty author is the translated unknown label. Whitespace and foreign labels are not
rewritten. Various Authors is searchable only when it is the current displayed group; name-mode raw
and normalized author searches still work. This is the intentional source difference in MO-04.

Read-only provenance: selected S047 assembly SHA256
`35B7D4E31304F1D13FABFE938541308CF3E38D892807334577AB32339008CEAD`, retained decompilation
under `artifacts/Refinement/megamod-spec-20261103/mod-options-sort/compiled/ModOptionsSort/`.
Reviewed `ModOptionsSort.cs`, `Dialog_Options_PostOpen_Patch.cs`, and the chooser sorting/search/opening
portion of `Dialog_Options_DoModOptions_Patch.cs`; code is independently implemented, not copied.

`tests/Megamod.Unit/Features/ModSettingsNavigation/SettingsChooserTests.cs` verifies supplied metadata
only, with no Verse, active mod database, provider callbacks or Harmony. Native proof still needs
Options → Mods search/sort/group actions and opening each distinct same-title page, including supported
provider pages, followed by ordinary editing/closing. No host assertion proves that integration.
