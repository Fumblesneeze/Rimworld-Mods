# Brrr and Phew (Continued)

## Identity and provenance

- Sole owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
- Origin: **replacement plus original toddler-temperature amendment**, **S030 / P1-24**.
- Source: `Mlie.BrrandPhew`, [Workshop 2195938471](https://steamcommunity.com/sharedfiles/filedetails/?id=2195938471),
  pelador original / Mlie continuation, [public source](https://github.com/emipa606/BrrandPhew).
- Inspected 2026-11-02: manifest **1.6.8**, installed/public DLL SHA256
  `858A6CFBFE48F28BD17E6D098DB5EA9AEAC59A18479EE21D2FB5654FB5C84584`, public revision
  `94016d8990e2f0193ffda18b34492b9952d0f88c`. Advertised through 1.6, not accepted on beta.
- [Research](../refinement/2026-11-02-brrr-and-phew-research.md) records all 35 source classes/six
  Def files, native 1.7.4995 rev309 comparison, narrow toddler/temperature-provider inspection,
  hashes, source-derived defects and BP-Q1–4. MIT repository versus CC BY-NC-SA prose discrepancy
  is retained; independent implementation, no copied binaries/code/assets/translations.

## Behavior inventory

All rows are **specified, not implemented**. The
[owning change](../../../../openspec/changes/specify-megamod-environmental-self-preservation/)
contains proposal, design, three capability specs and unchecked implementation/acceptance tasks.

| ID | Native input/action → proposed result | Defaults, edge cases and source difference | Capability / scenarios |
| --- | --- | --- | --- |
| BP-01 | Eligible pawn reaches injury threshold → recovery activity | Cold/heat 20%, toxic/blood/breathing 10%; human/animal and native priority gates; followers excluded | Environmental recovery: Early recovery; Each enabled injury |
| BP-02 | Refuge search → usable suitable bed or floor | Actual cell/health exposure, native ownership/reservations, outside-area default on; no unchecked random cell or roof-only vacuum proof | Environmental recovery: A refuge is physically usable |
| BP-03 | Heat injury and safe local water → real cooling swim | Correct Odyssey capability, native physical cooling, source 29-cell preference; dry fallback | Environmental recovery: Heat response can use real safe swimming |
| BP-04 | Rest or surroundings change → normal recovery/needs exit or replan | Cause-specific completion, actual smoke identity, 250-tick safety check, 500-tick unchanged failure retry; no teleport/new healing rate | Environmental recovery: Recovery follows actual causes; Ordinary food and recreation |
| BP-05 | Low outdoors need → safe short walk/skygaze | 20% start, 75% goal, half ordinary recreation, 6,000/4,000 ticks; full recreation does not end it; feasible-choice correction and retained outside-area exemption | Outdoor breaks: all three requirements |
| BP-06 | Mobile toddler idles or gets cold/hot → prevent exposure or escape | Before injury; actual protection/local heat/mobility, no magical doors; single-cell fallback, safe-arrival handoff and no unsafe automatic area-return after escape/rescue | Toddler temperature safety: all four requirements |
| BP-07 | Configure/reload → correct global/save effective choices | Persist blood values, common Use global rows, scope-specific numeric reset and absent-content dormancy | Environmental recovery: Common settings |
| BP-08 | Startup or optional environment changes → admitted behavior or explicit fallback | Minimal target guards, original-overlap suppression, no source identity spoof or unverified local-temperature fallback | Environmental recovery: Provider support; Toddler safety composition |

## Interactions and composed result

S031 owns full Toddlers behavior; this change owns its confirmed thermal amendment. The source's
ordinary human insertion misses the separate toddler tree; that evidence does not independently
reproduce the user's reported bug. Owned idle/escape/caregiver decisions compose directly, never
through patches against our own features. S002 directed recreation keeps its current player-directed
job; S024 needs rules share facts without creating another activity or duplicate joy gain.

S065 hunting companions can temporarily suspend their active following for an otherwise eligible
recovery/need action. The shared decision prevents native following from immediately reclaiming the
animal while preserving its saved master/follow choice. Other actively following animals retain
the ordinary exclusion; S030's existing injury thresholds and recovery rules remain unchanged.

Native safe-temperature behavior already exists but normally waits for a serious injury; toddler
prevention/early escape is deliberately earlier. Ordinary source hunger/recreation interruptions
remain explicit; neither source nor replacement guarantees survival when care or shelter is absent.
No new shelter, protective clothing, health immunity, medicine, tech unlock or healing rate is added.

Current Vanilla Temperature Expanded has an actual incoming Brrr integration. Its title-gated
patches cannot be activated honestly by a facade without impersonation. An optional narrow adapter
must reproduce the effective-temperature outcome, including local effects once, not emulate its
source patch stack. Unknown active semantics withhold dependent safety behavior. More Events/CE
exposure support still needs provider-specific research; bare condition names are insufficient.

## UI audit and relocation

| Source surface | Purpose | Class | Owned destination / difference |
| --- | --- | --- | --- |
| Two-column ModSettings switches and sliders | Configure automatic responses, thresholds and needs behavior | Genuine mod settings | Ordered Pawn behavior → Environment sections, common global/save controls and unavailable-content explanations |
| Numeric reset | Restore numeric defaults | Settings action | Explicit editable scope, numbers only; no hidden other-save mutation |
| Pawn job report | Explain current recovery or outdoor activity | Native contextual information | Same inspect/report surface, correct cold/heat/breathing reason and contextual outdoor text |
| Bottom tab / gizmo / bottom-right switch | None found | Not applicable | None introduced |

Settings use current global inheritance and dormant save overrides, disable save editing at the
main menu and disable override editors while Use global is checked. Persist every preference,
including blood-rain fields omitted by the source. Jobs and pawn life-stage/mobility are gameplay
state, not settings. Seven-language labels/tooltips/reports require native layout verification.

## Contracts consumed by other mods or saves

| Consumer / evidence | Contract | Disposition and required proof |
| --- | --- | --- |
| Vanilla Temperature Expanded current ProxyHeat.dll | Exact source title, Brrr job-giver namespace/methods, GenNewRRJob and Phew; temporary pawn context and local heating redirects | No identity spoof or source facade; use admitted effective-temperature adapter and real heating/recovery/wandering workflows |
| Source native jobs/Defs and saved execution | Brrr-prefixed recovery/outdoor JobDefs and source driver types | No source-job migration/removal guarantee; owned jobs revalidate health/target/settings on load |
| Source static settings and Scribe keys | Global switches/thresholds; blood pair omitted | No automatic import; owned common settings with full persistence |
| Native care, bed, needs and movement | Ordinary ownership, reservation, health, threshold, route and control semantics | Retain and verify actual physical actions rather than copied utility bodies |
| Optional toddler provider | Specific life-stage, crawler/door/crib and caregiver control | Narrow package/shape admission; full provider replacement is S031 |
| Installed consumer scan | ASCII and UTF-16 scan of 5,485 DLLs found VTE plus source versions | Bounded installed evidence, not proof that no other consumers exist |

No blanket drop-in, multiplayer, uninstall or source-save compatibility claim. The source's MP
self-patching/random-state wrapper is not retained. A later importer needs its own backup, atomicity,
restoration and verified postconditions; this draft performs no migration.

## Implementation and coverage

| Behavior | Implemented here | Deviation / possible conflict | Missing / deliberately omitted | Targets / evidence |
| --- | --- | --- | --- | --- |
| BP-01–04 | Nothing | Independent hazards, exact final-cell suitability, cause-specific rest, correct swimming and bounded replanning | Invalid targets, ignored reservations, wrong smoke name, all-condition waiting, downed teleport and fake swim omitted | Candidate target families in design; actual target docs/guards, RED/GREEN and native proof pending |
| BP-05 | Nothing | Feasible safe activity choice and reached-goal guard; keeps source duration/weight/area policy | Unchecked dangerous outings and requiring both activity routes omitted | Real walk/skygaze/needs/control acceptance pending |
| BP-06 | Nothing | Required proactive temperature safety and direct toddler integration | Full growth/learning/feeding/caregiver feature set deferred to S031 | Mobility, closed door, crib, single-cell and adult rescue workflows pending |
| BP-07–08 | Nothing | Common settings, persistent blood values, semantic admission and direct optional adapter | Source package/title/API emulation and MP certification omitted | Scoped settings, provider absence/drift/co-installation and logs pending |

Original Brrr active withholds owned recovery/outdoor overlap; toddler safety requires separate
nonduplicating admission. Optional providers/DLC are not core dependencies. Harmony and XML
Extensions become required if used in implementation. XML target edits must be guarded and
additive, retaining foreign nodes rather than replacing whole trees. No package metadata changes
here, no Workshop assembly reuse, and no runtime dependency on repository products or the Gateway.
Native map/region/bed data supplies candidates; no separate Ctrl+F/hauling index is introduced.

## Verification and acceptance

Strict schema validation and independent document review are tracked in the
[progress ledger](../refinement/specification-progress.md). BP-Q1–4 retain unknown provider/native
details and later playability/acceptance. No runtime task is checked. Final acceptance requires
focused nonzero RED/GREEN, reviewed build/package and real native recovery/swim/outdoor/toddler/
caregiver/settings workflows in exact isolated provider lists, with personally inspected causal
views and normal-config/cleanup evidence. Final balance review must assess shelter availability,
needs interruptions, threshold/area choices and response timing on the actual build.


Initial support is single-player without Combat Extended. Source CE/MP adapter details are retained research only; their adapters, controls and acceptance are deferred to a future scoped change.
