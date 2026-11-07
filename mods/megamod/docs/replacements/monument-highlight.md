# Monument Highlight

## Identity and provenance

- Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
- Origin replacement **S101/P2-31**, Monument Highlight, Espio Idsavant, `espio.monhighlight`,
  [Workshop3018715974](https://steamcommunity.com/sharedfiles/filedetails/?id=3018715974).
- Selected1.6 assembly0.0.0.0; [research](../refinement/2026-11-05-monument-highlight-research.md)
  records hashes, full selected-type inspection, native quest scripts and bounded consumer search.
  No source/atlas reuse license established; no source code/binary/art copied into product output.
- [Owned change](../../../../openspec/changes/specify-megamod-monument-labels/).
  **Analyzed/drafted only; no behavior implemented or native visual compatibility accepted.**

## Behavior inventory

Every row references the [observable spec](../../../../openspec/changes/specify-megamod-monument-labels/specs/megamod-monument-labels/spec.md).
Evidence is public description, selected binary and inspected native code/XML, not a live workflow.

| ID | Input → observable result | Boundaries / values | Spec scenarios | State |
| --- | --- | --- | --- | --- |
| MHL-01 | Place monument → quest name beside it | Actual ongoing known association; copies/multiple tags; no packed/preview labels | Place a quest monument; Replacement marker and reordered tags | Draft |
| MHL-02 | Active objective → Build, Protect or Keep | Correct enabled timer, native deadline; four supported native workflows; due/unknown distinction | Finish construction and begin protection; Retain a basic, decree or heir monument | Draft |
| MHL-02a | Late decree construction → overdue, later expiry or Keep |15day native penalty deadline separate from80day forgetting; no reset Build deadline | A decree is finished late; An overdue decree is forgotten | Draft |
| MHL-03 | Quest/map/marker changes → current local view | Next map update, paused load, historical removal, world/map/fog exclusion | Switch colony and world views; End a quest or destroy a marker | Draft |
| MHL-04 | Read label near monument | North title/south time preference; white50%, uniform.5–1.2; measured fitting and seven languages | Rotated or edge monument; Long localized name; Partial viewport and adjacent monuments | Draft |
| MHL-05 | Toggle view → visibility only | Default on; common inherited setting and bottom-right control; floor labels independent | Toggle while paused; Inheritance and main menu | Draft |
| MHL-06 | Optional/original/changed content → scoped fallback | Royalty/content required for this feature; original single provider; bounded diagnostics | Content is absent; Co-install the original; Native hook changes | Draft |

## Interactions and composed result

Share S093 [owned floor-label](floor-labels.md) font/geometry/resource infrastructure directly without
sharing room/zone semantics, custom label records or display-mode state. Source references to
Falconne routines do not imply an external dependency or whole Labels on Floor replacement here.
Use native marker/quest lists; no separate Thing cache or full-map Thing scan per label. S036
Architect work and native monument construction/quest obligations are unchanged by this overlay.

The native decree Keep delay starts on MonumentCompleted, just like threatened Protect elsewhere;
the composed result uses actual obligation meaning. Replacement markers copy tags but may not be
the same objects referenced by old timers. Both require explicit native contract coverage, not a
generic last-delay guess. Unsupported custom quests have bounded visible fallback, not invented timers.

## UI audit and relocation

| Source surface | Purpose | Owned destination / deviation |
| --- | --- | --- |
| Automatic north/south world-space text | Read-only local map information | Retain monument-adjacent annotations, readable fitting and correct current surface |
| No controls/settings | Source always-on display | Add small bottom-right Monument labels toggle under user's view-control rule |
| No settings menu | Configuration | One shared Display → Map labels → Monuments visibility row with Global default / Current save / Use global |
| No editor/tab/debug control | None | Add none; normal native marker/quest access remains |

Toggle changes only the active save override; global default/other saves remain unchanged. Main-menu
save controls and inherited save editor are disabled, dormant overrides preserved, and paused
changes immediate. No source settings/state migration exists. Seven baseline languages and actual
glyph/readability cases remain required future native evidence.

## Contracts consumed by other mods or saves

| Evidence / contract | Owned decision |
| --- | --- |
| No external source-identity/type/key consumer found in bounded installed string search | No speculative monhighlight type/assembly facade; not a universal consumer-absence claim |
| Native MonumentMarker, quest targets/tags, active timer lifecycle and Royalty scripts | Read/observe admitted semantics; preserve all native state, signal effects and quest UI |
| Source static mesh/name cache and empty map component; no serialized own fields | No import or source save-schema impersonation; rebuild owned derived presentation after load |
| Source bundled Harmony, no selected assembly Harmony usage | Do not bundle it; declare external Harmony only if actual owned patches need it |
| Source fixed font texture | No reuse right established; use independently admitted owned/native resources |

## Implementation and coverage

**Nothing implemented for any MHL row.** Source defaults retained where readable: two annotation
blocks, north/south preference, white50%, scale.5–1.2 and coarse native duration display. Intentional
changes: reliable quest/objective association and active-phase checks, case/Unicode preservation,
less-than-hour/due/unknown cues, finite placement, complete text-bound culling, fog/history handling,
paused current-state refresh, bounded resource ownership and common toggle. No automatic monument
renaming, construction protection or game-rule edits are introduced.

Original active suppresses the owned producer without editing the source. Royalty is optional for
megamod but required for native monument content. No XML edit proposed; later edits require XML
Extensions and guarded merge semantics. Each actual external patch needs minimal executable target
admission, compatible-change tolerance, incoming/effective checks where relevant, owned-only atomic
rollback and one bounded startup warning per skipped enabled patch. Unsupported timing falls back
per obligation; native quest controls remain authoritative.

## Verification and acceptance

The [ledger](../refinement/specification-progress.md) records document review and strict validation.
**MHL-Q1–3** retain exact generated-instance contracts, shared visual resources and performance,
common-setting lifecycle and real native evidence. All implementation tasks are unchecked.

Future acceptance requires a reviewed build in a fresh minimized Gateway-controlled run: actual
quest/marker placement and construction → phase transition/countdown → expiry/removal, with
replacement/rotation, native quest/inspect comparison, paused/world/other-map/load/settings actions,
several zooms, fog/edge/long-language text and separate absent/original/changed-provider cases.
Preserve causal screenshots, exact build/mod list, actions/logs, configuration hashes and cleanup.
Static tests, startup, font manifests and a prepared end-state screenshot are not visual/gameplay proof.
