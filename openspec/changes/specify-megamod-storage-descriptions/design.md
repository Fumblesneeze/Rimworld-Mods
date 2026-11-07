## Context

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**, S098/P2-27.
[Research](../../../mods/megamod/docs/refinement/2026-11-05-storage-descriptions-research.md) pins the
installed source/binary and native 1.7.4998 input. Source advertises 1.6/1.7 but uses its 1.6 folder
through native version fallback. Its startup constructor rewrites every ThingDef description once.
It appends rectangle-based capacity and coarse root-category names, and truncates text at a literal
old heading before even checking storage eligibility. This design replaces that mechanism with
current contextual presentation. Nothing is implemented or live-accepted.

## Goals / Non-Goals

**Goals:** accurate physical units and eligibility scope, native description placement, preservation
of unrelated text, current provider/instance facts, common settings and independent admission.

**Non-goals:** new storage capacity, a warehouse inventory dashboard, free-slot/stock counters,
group-total descriptions, automatic filter changes, physical or refill rebalance, a new tab,
source package impersonation, whole-provider ports or an unbounded description-processing framework.

## Decisions

### Decorate presentation rather than rewriting shared definitions

Use one owned storage-summary function receiving the actual description context and facts. S036's
owned Architect presentation calls it directly. Native candidate boundaries are
Designator_Build.get_Desc and StatsReportUtility.DescriptionEntry(Def/Thing), with the actual
construction definition/stuff or instance carried through. Native Thing.DescriptionFlavor returns
def.description; ThingDef.DescriptionDetailed has a separate cache. Info cards use DescriptionEntry,
not that detailed getter. Avoid decorating every getter and then appending again at the card.
No patches against our own UI; any external native hooks must be admitted and idempotent.

Do not assign generated text back to Def.description. This avoids contaminating consumers, save
contexts, other languages and later comparisons with our own previous output. Preserve returned
foreign text and hyperlinks; source block normalization applies only to the current rendered text.
Use the existing description layout and scrolling; the Architect surface can expose a clearly
labeled continuation to the existing info card when necessary. Implementation must measure actual
native layout rather than assuming a longer string automatically fits.

The native stats report caches entries. A context/revision change must refresh the owned description
entry before its next display without resetting another provider's unrelated entries, selection or
scroll state. Exact card update seam and cache lifecycle remain SSD-Q1, with paused-change evidence.
Eager startup-wide mutation and broad clearing of foreign caches are rejected alternatives.

### Admit real storage models and preserve capacity units

Use a small native model plus narrow optional adapters, not IStoreSettingsParent alone. Native
Building_Storage uses occupied cells as slot cells; ordinary uniform capacity uses those cell counts
and the actual physical cell limit. Native Building_Bookcase is a distinct ThingOwner<Book> model:
MaximumBooks = maxItemsInCell × size.Area, with actual book acceptance. It is included deliberately,
even though it is not a Building_Storage subtype. A bed comp, turret ammunition filter or machinery
ingredient selection does not establish a storage building model.

For a type preview, derive admitted finalized definition/provider facts without instantiating a
building, invoking PostMake or manufacturing a quality. For an instance, read its actual physical
capacity and relevant quality/configuration. If quality/context is not determined, show a qualified
base/conditional result or unavailable value. Unsupported models must not silently use rectangular
area × maxItemsInCell. Use checked/wide sums and reject malformed/overflowing facts rather than
displaying negative or wrapped totals. A supported zero capacity differs from missing information.

Adaptive Storage Framework is the concrete optional variable-capacity case. Inspected source and
compiled ThingClass expose TotalSlots, per-cell capacity and quality-dependent initialization;
CurrentSlotLimit is a separate player limit. Its TotalSlots sum cannot always be recovered from the
one-cell maximum. Resolve `adaptive.storage.framework` and exact callable semantics in a narrow
adapter; no eager assembly reference when absent. Definition-level preview needs its own supported
projection, not an uninitialized instance. Provider global settings that actually alter physical
capacity and instance quality belong in revision keys. CurrentSlotLimit and owned S049 refill limits
must not be presented as physical maximum capacity.

### Summarize physical acceptance rather than user policy

Native Building_Storage.GetParentStoreSettings uses fixedStorageSettings or
StorageSettings.EverStorableFixedSettings; GetStoreSettings separately resolves a linked group's
editable filter. The source incorrectly falls back to defaultStorageSettings, which is only initial
policy. Derive the physical supported set from the admitted storage model and finalized restrictions.
Do not infer universal acceptance from null/default settings. Bookcase type checks and external
storage rules remain part of the model.

ThingFilter.Allows(ThingDef) checks only definition membership. Allows(Thing) additionally handles
minified contents, hit points, quality, anomaly book risk and special filters. A type summary cannot
promise every concrete item passes these checks. Resolve a bounded, deterministic category/type
projection over actual storable definitions: compress a category only when the supported set fully
covers its relevant descendants; otherwise descend or mark it partial. Deduplicate definitions in
multiple categories, retain uncategorized supported types and distinguish empty from unknown.
Use localized names and communicate additional item-specific restrictions. No fake Thing instances
or side-effectful acceptance calls just to build text. Exact provider constraints remain SSD-Q2.

Share finalized definition/filter projections with S050 presets and the common storage model when
useful. No inventory scan is needed to report maximum capacity. If an adapter genuinely needs map
facts, reuse the S021/S033 map-owned data and native listers. Do not add a private Thing cache or
copy S049's demand/eligibility decisions. Linked groups change editable policy, not this member's
physical capacity. Direct common number formatting from S034 preserves units and rich-text markup.

### Replace a known block without destroying the surrounding description

The source searches two literal yellow English headings and keeps only the preceding substring.
That destroys every later paragraph, applies to nonstorage definitions and misses translations or
different colors. Instead, recognize only provider/content-scoped storage blocks with a provable
grammar and end boundary. The inspected Reel's Expanded Storage form has a heading and named total,
per-tile and allowed-items lines; keep later unrelated text. Gate `reel.expanded.storage`, the actual
target content and supported formatting. Translation-specific forms need equally bounded evidence.
Do not infer ownership merely from matching the words Storage stats or a color tag.

Normalize only that rendered block, then add one owned section. If complete boundaries or overlapping
provider ownership are unknown, preserve the original text and decline the competing contribution
with one bounded reason. This is presentation transformation, not an XML or runtime Def mutation.
If implementation later requires declarative Def changes, add the XML Extensions dependency and
explicit package/content/node/merge target contract instead of mutating the Def in startup code.

### Settings, lifecycle and coexistence

One common setting: Storage descriptions → Heading color, RGB string default `#E5A84C`. Source field
and Scribe fallback are orange, but its Reset button chooses yellow `#E5E54C`; the owned reset uses
the actual built-in orange consistently. Validate a leading # plus six hexadecimal digits and
normalize uppercase. Keep an invalid edit buffer separate from the effective value so markup cannot
escape into generated text. Common loading handles malformed persisted values with a valid fallback
and explanation; do not import the source's global `titleColor` key automatically. Use normal common
dynamic inheritance, dormant overrides, disabled main-menu save callbacks and seven locales.

Keep a small presentation/definition projection only when measured useful. Key by definition/model,
instance context where needed, finalized capacity/filter revisions, language, common color and number
format revision. Recheck cheaply at display; events can invalidate shared derived facts. No map or
Thing references in persisted settings, cross-save reuse or off-thread Verse/Unity access. Clear
ephemeral instance facts on teardown. No periodic stale TTL is a substitute for current information.

Canonical original `ferny.storagestatsindescription` suppresses owned overlap with a bounded warning;
do not undo its static initialization, edit its settings or unpatch foreign owners. No external
consumer of the inspected public API was found in the bounded scan, so no facade is selected.
Relevant Stats In Description (`mlie.relevantstatsindescription`) independently computes storage
space and can replace the Architect description or use an extra tooltip. Admit a coordinated
presentation only for a known active route/settings contract; otherwise defer owned construction
summary on that competing surface. Preserve its other stats and independently valid info-card
integration. This is a demonstrated overlapping provider, not an inferred conflict with all mods.

### Candidate target contracts and failure isolation

These are design candidates, not completed patch documents. Every actual hook must receive its
stable target document, executable validator and tests before implementation.

| Candidate | Minimal consumed behavior | Failure boundary |
| --- | --- | --- |
| SSD-T1 construction description | Correct definition/stuff context, returned description, active provider presentation and single owned contribution | Preserve original construction text; other surfaces can remain |
| SSD-T2 info-card description | Def versus instance identity, description entry and hyperlinks, effective cached-entry refresh | Preserve native/provider card; no repeated or cross-instance entry |
| SSD-T3 model/invalidation adapter | Actual physical capacity, fixed acceptance and revision/lifecycle semantics, narrow canonical provider shape | Unknown facts marked unavailable or affected summary inactive; never guessed capacity |
| SSD-T4 recognized legacy-block normalization | Active provider/content, bounded supported block grammar and complete boundaries | Preserve all incoming text and withhold competing section |

Validate minimal original and effective semantics, including skip-original prefixes or replacement
results where they matter. Mere method existence can suffice only for independent observation, not
for assumed argument usage or retained provider data flow. Accept benign unrelated changes. Any
transpiler validates its actual incoming stream on every run and returns it unchanged on mismatch;
do not replay foreign transpilers speculatively. Admit required groups atomically; false/unknown,
exceptions or installation failure roll back only newly added owned patches. Later repatching must
invalidate/revalidate affected contributions. Log one bounded startup warning per skipped enabled
patch with package/patch/spec ID, target, failed predicate, unavailable behavior and fallback.
No exact-hash/version-only guards or blanket prohibition of other patches. Runtime unknowns use the
same bounded fallback without per-frame log spam. Dependencies: Core and Harmony when hooks exist;
optional providers remain absent-safe and no Gateway/source binaries enter product packages.

## Risks / Trade-offs

- Broad storage interfaces include consumption settings → admit native storage models and provider
  semantics, not a name/interface heuristic alone.
- A short category name can imply more than it proves → compress only complete sets and disclose
  partial/special restrictions; test large localized catalogs and uncategorized content.
- Effective provider configuration can differ from raw XML → separate type/instance facts and
  required finalized/native verification, with explicit unknown values.
- Old text may have no reliable end delimiter → preserve it and decline that normalization; never
  truncate the tail for the sake of a tidy display.
- Fresh descriptions can be expensive if rebuilt for every draw → share bounded definition facts
  and measure update/display costs; no map-wide scan or unmeasured optimization claim.

## Migration Plan

No world/entity save schema or source-settings migration is added. Common settings hold only the
owned color configuration. Removing/disabling this contribution leaves storage, jobs and saved
filters unchanged; ephemeral text disappears on the next native display. No shared Def restoration
is needed because generated text was never written there. A future importer needs a separate
backup/atomicity/recovery contract.

## Open Questions

- **SSD-Q1:** exact effective construction/card seams, cache refresh and safe recognized-block
  boundaries across supported languages and provider renderers.
- **SSD-Q2:** finalized native/ASF physical-capacity and acceptance projection, quality/type previews,
  current provider configuration and demonstrated optional-description coexistence.
- **SSD-Q3:** source reuse license/actual external consumers, common setting persistence and
  localization/formatting layout without duplicate shared caches.
- **SSD-Q4:** honest-tier TDD, measured display cost, final semantic comparators and reviewed-build
  native construction/card/configuration/provider workflows. All runtime work remains unchecked.
