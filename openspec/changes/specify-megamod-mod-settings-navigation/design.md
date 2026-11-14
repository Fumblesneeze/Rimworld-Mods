## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S047 Mod Options Sort modifies the outer native Options → Mods chooser. Complete selected1.7 code,
native chooser and the narrow older HugsLib reference are recorded in
[research](../../../mods/megamod/docs/refinement/2026-11-03-mod-options-sort-research.md).
A bounded host-only chooser projection is implemented; native/provider integration and acceptance
remain pending. Its contract and limits are recorded in
[the implementation note](../../../mods/megamod/Source/Features/ModSettingsNavigation/Implementation.md).

## Goals / Non-Goals

**Goals:** preserve every actual settings entry, group/search useful identity information, open the
correct owner page, retain browsing state and keep optional-provider failures local and diagnosable.

**Non-goals:** foreign settings migration/default introspection, mod activation/load ordering, new
bottom tabs, a second owned settings framework, persisted browser preferences, or creating one source
settings entry per integrated megamod feature. Advertised-only yellow/blue highlights remain a gap.

## Decisions

### Project existing settings entries without taking ownership of their values

Start from the native/provider-completed entry collection, retaining exact Mod or provider-pack
identity and opening callback. Do not rediscover all installed mods or instantiate settings objects.
One package may have multiple settings pages; identical titles are not duplicates. Store a per-dialog
projection of category, content name, raw author, normalized group, icon availability and owner/open
route. Preserve input ordinal as a final stable tie-break after displayed title/content name/package
identity; provider-pack identity disambiguates where available. Never serialize live object identity.

Sort group labels/title strings using the active language's normal text comparison; handle missing
metadata, empty author and safe title fallback explicitly. Don't derive ownership from a translated
title. For a null/empty settings title, use the first nonempty content name, package identity, then
the localized untitled-page label; retain the original metadata and opening identity unchanged.
This fallback order makes the previously required safe fallback concrete. Retain source grouping
conventions, in precedence order after recognized HugsLib proxies:
content name containing both Vanilla and Expanded → Vanilla Expanded; author containing sarg →
Sarg Bjornson; else author containing helixien → Helixien; otherwise complete author metadata.
These are display aliases, not modifications of actual authorship or package identity. Keep raw
metadata available for full identity inspection and search. No custom package taxonomy is introduced.

HugsLib-generated settings entries get the provider group when positively recognized by the adapter,
even when they have non-null Content. A content-less unrecognized entry is Unknown author, not guessed
HugsLib. Group sizes are counted over all available settings entries before filtering. With singleton
grouping on, any one-entry normalized group, including a special group, moves under Various Authors.
Various Authors sorts last; other groups alphabetically. Name mode orders by title, omitting group
headings and showing author attribution on each card. Author mode shows attribution for Various
Authors cards, with full raw attribution still discoverable for every row.

The synthetic singleton bucket has its own identity, separate from its translated heading. A real
multi-page author whose name equals that heading remains an independent, normally sorted author
group, even though two headings display the same text. Only singleton-bucket rows need per-row
attribution in author mode. Displayed-group search can match both groups; name-mode author search
still matches the real author's metadata without matching unrelated singletons. A renderer must
preserve the separate projected groups rather than deduplicate their headings.

### Search and layout remain chooser operations

Match a case-insensitive substring against displayed settings title, mod name, raw metadata authors,
normalized author and displayed group. Empty query includes all; clearing restores the same original
group sizes/order. Keep every independent match, including same-titled packs. Do not filter out
mods because their settings happen to be default.

The 2026-11-14 S141 all-mod decision supersedes this draft's former foreign-control search exclusion.
When S141 is enabled, reuse one query field: S141's syntax governs page matches on their own metadata
and separate option results from generic discovery across all mods, without a package whitelist.
A nonmatching page title cannot hide a matching option. Keep actual page identities, complete-list
grouping and owner edit/close persistence. With S141 off, retain the page-only substring behavior
above; with S047 sorting off, S141's native chooser search still works.

Displayed group means the current view: Various Authors participates only when author mode and
singleton grouping actually place that page there. Name mode has no displayed group, so that label
alone cannot match; raw and normalized authors remain searchable in either mode. A literal match in
another searched field still counts. This intentionally differs from the selected source's hidden
group search in name mode and follows MO-04's displayed-group wording. Switching modes does not
discard the caller's remembered singleton choice or recalculate group sizes from filtered results.

Retain native two-column cards where width permits and readable reflow at constrained scale. Group
headings start a row, appear only for matching groups and cannot leave the first result inaccessible.
Use native available icons without copying source art, neutral/missing-icon handling, full-title and
author hover details, and stable identity detail when title/author cannot distinguish entries. Restore
GUI state in guaranteed cleanup, including when a provider callback fails. Own labels and explanations
need all seven supported languages; third-party labels remain the provider's responsibility.

Avoid per-frame metadata/SettingsCategory/provider-reflection queries. Rebuild the chooser projection
on dialog open and genuine provider-list/language changes, and only refilter/resort for browsing
changes. Preserve current query and a sensible scroll position through refresh; clamp empty/shortened
results. The native list and provider lifecycle determine contents. One dialog-local view cache is
not another Thing index and must not survive save/discovery changes as stale static Mod references.

### Source browsing state stays transient

The two sort choices are UI navigation state, not durable game configuration: author mode and group
single-entry authors both default on at process start and retain their current choice across chooser
openings until restart. Query resets on a fresh opening, as native. Name mode hides/disables the
singleton control without forgetting its choice. Sorting acts immediately and never writes globals,
save overrides or third-party settings. No new persistent preference/Use global exception is added.
If persistence is later desired, it requires a common-settings definition and explicit scope contract.

Controls belong beside the settings list they navigate. They are not colony-view toggles and do not
belong on the bottom-right gameplay toolbar. The single megamod settings entry opens the existing
planned structured common renderer; its actual configuration rows retain global/save inheritance,
dormant overrides and main-menu protections. This outer chooser neither flattens those sections nor
projects former source mods as separate fake settings pages.

### Native and optional-provider opening paths

Native rows open the original Mod's native Dialog_ModSettings, preserving its drawing and native
WriteSettings-on-close behavior. Do not write while filtering, sorting, inspecting metadata or merely
drawing a chooser. A provider row delegates to that provider's established factory and pack, with no
foreign-value reflection or replacement renderer. The inspected HugsLib1.6 reference uses
HugsLib.Settings.OptionsDialogExtensions.GetModSettingsWindow(Mod) and SettingsProxyMod.SettingsPack;
these internal types are reference evidence, not an accepted beta interface.

Resolve optional HugsLib by canonical active package ID and its actual assembly/type/member shapes,
visibility, result Window and proxy identity. Inspect forks before adding explicit adapters. Never use
an unqualified TypeByName alone or route every native row through an unvalidated optional factory.
No hard HugsLib assembly reference. Confirm injection ordering/refresh and existing provider path.
If optional admission fails, leave native rows usable and retain provider-owned access. If an affected
row is shown but cannot be safely opened, show a localized unavailable explanation and the existing
provider entry point where known. Do not substitute native Dialog_ModSettings for a proxy: its base
Mod drawing is not the HugsLib pack renderer. If preservation cannot be proven, suppress the owning
chooser takeover for that combination rather than hide settings or promise an invented fallback.

Each callback failure is isolated and logged once per entry/failure identity in the dialog session;
restore GUI/window state and keep unrelated entries usable. Do not swallow a provider failure and
claim a setting was saved. Provider's own successful editing/close behavior remains authoritative.
Our integration adds no bulk edits, source imports, reset/delete/export or config-file rewrite.

### Minimal semantic targets and atomic chooser admission

Before code, publish stable target documents for each actual native/provider patch with linked
requirements, guards, tests and future evidence. Proposed seams are native PostOpen entry discovery
and DoModOptions search/card/open behavior; inspect alternatives and choose the smallest complete
integration rather than blindly copying the source's whole-method suppressing prefix.

A PostOpen observer needs its callable/existence contract and the completed collection it consumes;
ordering must account for provider injection. Replacing a native menu body needs the exact consumed
field shapes, native list/search/open/write meaning, other relevant prefixes, effective provider
augmentation and supported fallback. Whole-method hashes, exact versions and absence of other owners
are not semantic prerequisites. Retain compatible injected entries and foreign behavior. One coherent
owned chooser group prevents sorting from taking over while opening is unavailable.

If a transpiler is chosen, validate the actual incoming stream on every invocation, including operand/
data flow/uniqueness, preserving labels and exception regions; failure returns it unchanged. Never
re-execute foreign transpilers as a probe. False/unknown guards, exceptions or install failures leave
the group unavailable and roll back only newly added owned patches. Define later repatch revalidation.
Log one bounded startup warning per skipped enabled patch containing package/patch/spec ID, target,
failed predicate, lost navigation behavior and native/provider fallback. Runtime per-entry failures
are separately bounded; no frame-by-frame log storm.

Active original `Superniquito.ModOptionsSort` triggers a bounded startup warning and best-effort
suppression through the ordinary outer-chooser feature-off path, with manual-off behavior and timing.
Keep stored preferences and the original untouched, and retain the independently registered megamod
settings page. Installed-but-inactive sources do not trigger this rule. No source-menu usability,
coexistence safety or repair of the source's observed broken HugsLib factory is promised. Original
API/consumer/facade work and coexistence test matrices are excluded. Other settings mods
are not conflicts by mere presence. HugsLib adapter failure affects only its admitted boundary unless
native/provider access cannot otherwise be preserved, in which case disable the chooser group.

### Dependencies and external contracts

Core and Harmony required when implemented; HugsLib/forks optional, no DLC or XML operation. Declare
only justified ordering for exact admitted integration, not source's blanket before-Core hint.
No bundled source/framework/Gateway DLL or runtime repository-mod dependency. No original API facade
or package identity spoof. Preserve deliberately used native/provider
entry identity and owner behavior. Source license is unknown; independent implementation only.

The selected1.7 assembly contains no yellow/blue default/strange-implementation highlighting. Do not
construct arbitrary foreign settings just to guess defaults, or label inaccessible values as default.
MO-Q3 records the advertised-only gap. Any future truthful default/override indicators belong in a
separately specified common-settings or explicit provider contract; no hidden replacement claim.

## Risks / Trade-offs

- Late injection or same-title entries → native/provider-completed list and exact identities, stable
  ties and bounded refresh; no package/title deduplication.
- Group aliases hide real authors → retain source visual conventions but search/show raw attribution.
- Provider lacks metadata/icon or throws → local neutral fallback, GUI cleanup and isolated diagnostics;
  do not crash the entire chooser or make a fake working settings page.
- Source HugsLib assumptions differ from modern proxies → explicit optional adapter and exact beta
  proof; metadata presence is not provider identity.
- Session state is not persisted → matches source behavior; any later persistence needs an explicit
  common-settings contract instead of silently creating a global-only preference.

## Migration Plan

No settings files, save data or provider objects are migrated. Restart resets browsing choices to
source defaults. Original-provider handling uses the ordinary feature-off path on a best-effort basis.
New navigation
must preserve native/provider edit/close persistence but must never infer permission to reset or
rewrite arbitrary config files. Disable/remove the navigation feature without deleting third-party
settings; gameplay settings remain with their original owners. The current pure projection is
unregistered; no native chooser takeover is delivered yet.

## Open Questions

MO-Q1–4 retain native/injection admission, actual beta HugsLib/forks, the explicitly unimplemented
advertised highlights, languages/scales/performance and real native workflow acceptance. These are
source/proof obligations, not questions asking the user to explain Mod Options Sort.
