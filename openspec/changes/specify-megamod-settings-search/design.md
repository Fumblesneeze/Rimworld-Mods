## Context

Sole owner: Make Rimworld Great Again (`fumblesneeze.megamod`, `mods/megamod`), 1.7 beta.
S141 uses Needle as an implementation reference for the owned-settings search requested on
2026-11-02 and revisited on 2026-11-14. S047 already specifies searching other mods' page titles,
authors and groups; it deliberately does not search opaque foreign controls. The current common
renderer registers Boolean, integer, composite, dynamic and presentation-only rows, but has no
settings search. See [research](../../../mods/megamod/docs/refinement/2026-11-14-needle-research.md).

## Goals / Non-Goals

**Goals:** find an owned setting by its visible name, help, feature or category; reveal its real
control; preserve current scope, inheritance, permissions and common persistence; keep browsing
usable in all seven baseline languages and supported layouts.

**Non-goals:** individual-option search inside arbitrary other mods, Needle binary/API/save parity,
indexing every Def, synthetic tab clicks during discovery, or an additional settings system. These
are deliberate scope choices, not claims that Needle lacks those capabilities. The current scope
follows the earlier user decision; a later request for foreign options needs an explicit extension.

## Decisions

1. **Index owned metadata directly.** Retain searchable label/help keys or bounded current-text
   readers, stable setting ID, feature/category identity and a renderer reveal handle alongside the
   existing row registration. Extend that registration rather than add a second feature registry.
   Dynamic and composite rows declare their meaningful searchable subentries explicitly. A purely
   decorative section is not a pretend setting. No settings value is inferred from text or widgets.
   Alternative: Needle's hidden `DoSettingsWindowContents` draws and tab callback traversal. Its
   implementation restores GUI/tab state, but arbitrary callbacks can have other effects; megamod
   already owns the metadata and does not need that discovery mechanism.
2. **Small deterministic query.** Use localized plain text, normalized whitespace and case-insensitive
   substring matching, AND across terms, with quoted phrases. Search name, description, feature and
   category together. Name matches precede help/path-only matches; remaining ties use the existing
   category/row order and stable ID. Source-specific numeric scores are not copied. Initial scope
   omits fuzzy matching, negative terms and `mod:/author:/id:/tab:/def:` operators; these are not
   necessary to fulfill the owned-settings request and will not appear as misleading controls.
3. **Result navigation reuses the renderer.** The search field and results live within the existing
   single megamod settings page. A result carries stable identity, not cached screen coordinates.
   Selecting it reveals/expands its containing section, scrolls the current layout and highlights
   its row for three real elapsed seconds after reveal. It never invokes an editor/reset callback.
   User scrolling or another selection replaces the navigation intent; no repeated focus stealing.
4. **Current values stay current.** Results project the common store and current binding when drawn.
   Show Global default, Current save/Use global and effective-off or unavailable status accurately;
   no serialized duplicate settings or persistent value cache. Search does not index arbitrary value
   serialization. A read failure is an unknown value, not false/zero. Main-menu save values are absent.
5. **Lifecycle and bounded work.** Query/results/highlight are view-local and discarded on close.
   Rebuild text on language/registration changes; re-resolve value/status on store or binding changes.
   No per-tick scan during normal gameplay, reflection across loaded mods or forced UI replay.
   Any retained text index is process-local and bounded by registered owned metadata. A missing
   dynamic target yields a reason and safe section navigation instead of a stale click. Search
   failure leaves ordinary scrolling and the existing settings controls available.
6. **Ordinary activation.** A default-on `settingsSearch.enabled` Boolean uses common GlobalAndSave
   inheritance, Interface/navigation, NextDisplay. Search query/navigation are explicitly transient
   browsing state, not another inherited setting. Active `ZIPender.Needle` triggers the standard
   bounded original warning and best-effort ordinary search-off handling without rewriting stored
   choices. S047 and common editing remain independent. No source coexistence claim follows.

## Risks / Trade-offs

- Conditional rows can disappear between result and activation → resolve current identity/visibility
  before reveal; show the owning feature's reason, never enable it implicitly.
- Duplicate labels and composite editors → show feature/category path and distinct registered
  identities; navigate to the actual owning control without guessed label-based selection.
- Long translated labels and rich text → match readable text; preserve Unicode and native layout,
  full help and scrolling, including non-Latin queries. Do not use Needle's narrow character table
  as a universal language contract.
- Source metadata is not a public license grant → independent implementation; no source code or DLL
  is copied. Inspected hashes identify evidence only, never runtime admission.
- Larger registry costs → measure actual query/reveal responsiveness before performance claims;
  keep detached matcher tests host-safe and native UI tests in fresh isolated processes.

## Migration Plan

This documentation adds no runtime or saved data. Later implementation adds metadata to the same
registration path and one ordinary enabled preference. Missing preference uses global/default-on.
Disabling search restores manual browsing without changing other settings or importing Needle data.

## Open Questions

Implementation must pin the existing native text-entry and renderer reveal boundaries, including
dynamic composite row identity and supported UI scales. Those are implementation research tasks,
not permission to crawl third-party pages. The wider implementation goal remains paused.
