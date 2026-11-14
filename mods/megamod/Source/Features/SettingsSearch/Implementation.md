# S141 detached query checkpoint

Owner `fumblesneeze.megamod`. This tranche implements supplied-text matching only. Nothing is
registered with the native chooser; it neither discovers controls nor exposes a search UI.
Native capture, metadata projection, reveal, coverage status, lifecycle, preferences/catalogs and
player-workflow acceptance remain open. No per-mod eligibility list exists.

`SettingsSearch` copies immutable `SettingsSearchEntry` descriptors and pre-normalizes their text.
Each retains S047's actual opaque `SettingsPage` descriptor, path identity and occurrence; native
code must derive those from actual page/tab/control identity, never display labels or coordinates.
`Find` returns original descriptors and match-kind flags. It never calls an owner, changes a value,
logs arbitrary text or persists anything. Recreate/discard snapshots when native lifecycle demands;
this class is not a runtime cache or currentness receipt.

Queries support AND words, quoted/unclosed phrases, exclusions and mod/author/id/tab filters.
Unrecognized prefixes stay literal. Text normalization removes syntactic rich tags, treats line
break tags as whitespace, collapses whitespace and uses Form C/invariant case. Original displayed
strings remain untouched. Page entries search their page metadata only; options additionally use
label/help/display/tab/feature/category. A page-title miss cannot hide an option hit. Empty input
returns no option hits so the future chooser can restore ordinary page browsing.

Typos apply only to eligible positive plain words with no exact occurrence anywhere in the supplied
snapshot before filters. Five-to-eight Unicode scalar values allow one edit; nine or more allow
two. Insertion/deletion/substitution and adjacent transposition are supported. A depth-two search
uses bounded branches rather than a quadratic distance matrix. Literal fields/exclusions/phrases
never expand. Results sort exact before typo, label before other context, then ordinal package,
page, path and numeric occurrence; equal complete identities retain input order.

Read-only inspiration: Needle1.7 DLL SHA256
7FBB6CE4E88EFB63A407E4277F96959761737D51BF3F94DAFB15BC95443D60F4,
`Needle.Search.Query` and `SearchEngine` in the retained November14 decompilation. Owned parser,
matching, normalization and bounded edit implementation are independent; no source code, weights,
private character-fold table, binary or assets were copied. The scoped native research packet is
`artifacts/Implementation/megamod-p0/settings-search/native-research/`.

Focused Unit tests exercise only detached data; no Verse/Harmony/loaded mod database is present.
Five initial RED/GREEN cycles, a generated two-edit counterexample/fix and seven review regressions are retained in
`artifacts/Implementation/megamod-p0/settings-search/query/`. The current affected search/chooser
batch passes61/61. Review regressions cover literal sharp-S casing, supplementary-letter typo words
and S047's empty-title fallback ranking. Independent re-review found no remaining issues; the
canonical build/package and strict OpenSpec checks passed. Native acceptance is absent and every
S141 gameplay gate remains open.
