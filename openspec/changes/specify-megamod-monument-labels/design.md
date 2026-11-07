## Context

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), S101/P2-31, 1.7 beta.
[Research](../../../mods/megamod/docs/refinement/2026-11-05-monument-highlight-research.md) pins the
selected 0.0.0.0 DLL, all ten generated type files, native 1.7.4998 quest/marker code and Royalty
scripts. Source reads all maps every2,000ticks, parses the first marker tag's first digits as quest
ID, chooses last delays by signal substrings and renders two font-atlas meshes. No code implemented.

## Goals / Non-Goals

**Goals:** correct associations and active deadlines, readable bounded map annotations, current
lifecycle, shared owned rendering and a common inherited view toggle with no gameplay mutation.

**Non-goals:** changing quest generation/rewards/failure, new markers, a quest manager, copying the
source atlas or static mesh cache, an independent Thing index, arbitrary custom quest inference,
per-monument editing, a new bottom tab or full external Labels on Floor support.

## Decisions

### Resolve actual monument obligations, not arbitrary signal substrings

Use the native current map's marker lister, active quest lists, marker identity and exact quest
relationships. Native MonumentMarker.GetInspectString uses QuestLookTargets and
QuestUtility.AppendInspectStringsFromQuestParts; the latter already filters ongoing quests and
enabled activable parts. Reuse those semantics and target-bearing parts where adequate. Never parse
localized display text to discover a phase.

Those helpers alone do not solve every source case: the native BuildMonumentWorker construction
timeout lacks inspectStringTargets, and replacement markers preserve tags while some parts retain
the old object reference. Bind supported native quest roots and their actual signal graph to all
exact marker tags, corroborated by real quest/target relationships. A copied marker's exact tag
links preserve objective identity; do not extract arbitrary digits or accept a substring anywhere.
Inspect current generated instances before finalizing the adapter. No write to questTags or quests.

For Build, admit the actual enabled construction timeout with the supported root's completion-disable
relationship. For the decree, DecreeCompleted alone is insufficient: DecreeSetup adds an80-day
delay ending the quest as forgotten, while Decree_BuildMonument adds a15-day delay whose completion
enables the DecreeUnmet situational-thought part. Both disable on DecreeCompleted. Distinguish their
actual output graph/effect and typed part linkage, not creation order, duration literals or the
shared disable signal. Once the15-day part completes, keep the known construction role overdue while
the decree is active and incomplete; use the active DecreeUnmet chain/other admitted native state,
not EnableTick=-1 arithmetic on a completed delay. The80-day timer is only the separately labelled
forgotten countdown, not a fresh Build deadline. Late construction disables both and the unmet
thought, then activates Keep. Native quest failure at forgetting removes the display.
For Protect/Keep, use the known
root/objective semantics plus the enabled target-associated retention delay. Source signal naming
is not sufficient: Decree_BuildMonument Keep explicitly enables on MonumentCompleted just like
BuildMonument_TimeProtect Protect. Basic and heir Keep use inherited nested sequence signals.
Track the typed/semantic obligation role separately from the display translation.

Allow multiple verified obligations/quests, deduplicate by actual objective identity and order
stably by quest ID and native part identity. If association is known but deadline classification
is ambiguous, show name plus unavailable cue. Ongoing confirmed untimed obligations get the
explicit untimed description; absence of a recognized delay alone does not establish that fact.
Do not expose hidden/unaccepted/historical quests; source's all-quests scan has no such filter.

### Read the active deadline, never drive quest transitions

QuestPart_Delay.TicksLeft returns0 unless State is Enabled; source EnableTick returns-1 when not
enabled yet source arithmetic still uses it. The owned projection must check active state before
reading a deadline. Keep native remaining-time semantics, including waitUntilPlayerHasHomeMap and
provider subclasses; if a subclass's semantics cannot be admitted, use fallback. Clamp presentation
at due, not quest data. Do not trigger DelayFinished/Complete or force a monument's complete flag.

Native tick interval recognizes monument completion every177ticks. Our label follows that recognized
state, not a separate scan that changes completion earlier. Observe phase/quest/marker changes and
coalesce invalidation before the next map render. Read a small active deadline projection for text
refresh at the next displayed unit boundary; do not rescan every quest per marker or every frame.
Use native period formatting with allowSeconds=false, shortForm=false, canUseDecimals=false,
allowYears=false, plus localized less-than-hour/due/overdue/forgotten/unavailable wrappers. No negative interval.

At first render after load/map switch, rebuild or rebind the current map projection even if paused.
Entity changes while paused and view toggles cannot depend on the next2,000-tick source poll. Remove
labels on marker/quest teardown, and release all game/map references at lifecycle end. Derived
associations/text/geometry are not saved; only the common view preference needs persistence.

### Share owned label resources with S093 while keeping semantics separate

Use the map presentation/resource ownership already planned for S093; add a monument label producer
and a fixed appearance profile (white/.5alpha/.5–1.2uniformscale), not a second font/mesh/cache system.
This is shared code within megamod, not a runtime dependency on another repository mod. S093's room/
zone visibility, customizable records, overlap modes and settings do not govern these annotations.
Share font fallback, measured text bounds, resource disposal and viewport/fog infrastructure directly.

Read current native sketch.OccupiedRect.MovedBy(marker.Position), whose sketch is rotated by
SpawnSetup on placement; do not rotate it twice. Use the inclusive rect width rather than source
maxX-minX. Fit actual text metrics and height, with preferred north/south offsets; adapt locally at
edges, wrap/ellipsize within bounded readable placement and omit an impossible block. Viewport tests
use the final footprint rather than source startAt. Avoid overlapping monument blocks with stable
priority/placement order; do not change S093 records to make room. Native marker/quest UI supplies
full text when abbreviation or omission is necessary.

Keep case and Unicode text elements; source ToUpper plus ASCII atlas turns unsupported characters
into blanks. Use licensed/native/shared owned resources without copying MonHighlight/Consolas.png.
When implementation creates/selects raster resources, the asset skill's live zoom/material/context-free
visual review gate applies. No raster is created, selected or accepted here. Never treat text as
commands or trusted markup. Seven baseline languages include the controls and fallback messages.

Cache keys include actual game/map/marker/quest/objective identities, text/language/font generation,
footprint/rotation and effective visibility. Keep only current demand plus bounded reusable render
resources, destroy owned meshes on eviction/replacement, and preserve shared MaterialPool ownership.
No static all-games cache keyed by every historical countdown string. No background Verse access.
The feature requires only native marker/quest lists, not the hauling/Ctrl+F Thing index; if a future
shared fact is needed, consume its owning service rather than introducing another scan/cache.

### Common view preference and compatible failure handling

One setting display.mapLabels.monuments.enabled defaults true. The small bottom-right button
writes the current save override through common persistence/rendering; Use global restores live
inheritance and preserves the dormant override. Menu save controls are disabled. Visibility changes
are immediate, independent of game ticks. No hotkey is assigned in this slice to avoid an invented
binding collision. No font/scale settings or entity customizations are introduced merely because
S093 has them. Content absence is a normal unavailable feature, not a startup failure.

Royalty package ludeon.rimworld.royalty and actual native marker/quest content are checked together:
MonumentMarker.SpawnSetup itself rejects absent Royalty. No hard dependency for the entire megamod.
Original espio.monhighlight presence suppresses only this owned producer with a bounded explanation.
Source contains an unused bundled Harmony assembly but the selected monhighlight assembly does not
reference/use Harmony. Prefer ordinary owned component hooks and native readers; only introduce
Harmony for a demonstrated missing lifecycle/UI observation boundary.

Every actual patch needs a stable target document and executable minimal semantic admission for the
behavior it consumes, considering original/effective code, skip-original prefixes and later repatching.
Transpilers, if any, validate actual incoming instructions every run and return them unchanged on
failure. Allow benign foreign changes, never require exact hashes or no other owners. False/unknown
checks or install errors leave the affected owned group inactive; roll back only newly added owned
hooks and emit one bounded startup warning per skipped enabled patch with spec/target/reason/display/
fallback. Malformed one-marker content is isolated with bounded diagnostics. No mutation of external
providers to make the display work. No declarative patch is currently needed; future XML changes
require XML Extensions package/content/shape checks and merge-preserving edits.

## Risks / Trade-offs

- Native timer graph differs across roots/copies → inspect actual generated quests and exact signals;
  prefer unavailable over a wrong countdown, without reducing support for the four specified roots.
- Event hooks can miss paused or copied-object changes → map-render revalidation and explicit first
  render rebuilding, bounded supporting reads and identity-aware invalidation.
- Text cannot always fit → preserve readable size, wrap/ellipsis/omit with normal full quest access;
  use actual in-game zoom/fog/edge cases before visual acceptance.
- Source rights are unspecified → independent implementation and licensed owned resources; no atlas reuse.

## Migration Plan

No source settings or primary label data are saved by the inspected source, so no data import is
proposed. New owned view preference follows the common schema. Do not spoof public source classes,
package identity or migrate historical static cache state. Disabling/removing the owned display
leaves all native quests and markers intact; no ordinary save is touched during this draft.

## Open Questions

MHL-Q1: exact generated native root/objective/tag/copy associations and semantic admission, including
multiple simultaneous obligations and changed custom timing classes.
MHL-Q2: owned shared renderer/resource metrics, seven-language/fog/edge/zoom/collision workflows and
performance measurements; no source atlas reuse or speculative external font API.
MHL-Q3: focused RED/GREEN, common view-setting lifecycle, provider absence/coexistence and reviewed
native construction → phase/countdown → expiry/removal evidence with unchanged quest outcomes.
