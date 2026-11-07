# Most Skilled Bill Restriction replacement coverage

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Queue **S019**, intake **P1-14**, origin **replacement**. **Analyzed; specification drafted.
Nothing implemented or accepted in game.**

Source **Most Skilled Bill Restriction**, **MatheusWerneck**, `Matheus.MostSkilledBill`, Workshop
**3757516418**. [Author description](https://steamcommunity.com/sharedfiles/filedetails/?id=3757516418)
and [change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/3757516418) were read
before decompilation. Installed assembly/file **1.0.0.0**, declared **1.5/1.6**, not 1.7. No attributable
public source or redistribution license found in the bounded inspection; no code/assemblies are copied.

[Research and exact inputs](../refinement/2026-11-02-most-skilled-bill-research.md) support the
[proposal/design/tasks](../../../../openspec/changes/specify-megamod-most-skilled-workers/) and
[observable contract](../../../../openspec/changes/specify-megamod-most-skilled-workers/specs/megamod-most-skilled-workers/spec.md).

## Coverage and deliberate changes

| Behavior | Source finding | Proposed replacement / status |
| --- | --- | --- |
| Most skilled assignment | Extra worker menu option clears named/category flags; ordinary choice disables it | **Specified:** contextual mutually exclusive choice, unavailable reason for recipes without a work skill |
| Ranking | Current recipe skill Level / controlled mech fixed level; ties admitted | **Specified:** same metric, no skill, speed or quality bonus |
| Candidate benchmark | Player free colonists/slaves/controlled mechs, any enabled work type sharing the skill | **Proposed refinement:** recipe-capable relevant work and visible range; unrelated craft work cannot set the maximum |
| Skill range | Named-worker early return bypasses it; source Most skilled clears the name, then hides the now-operative range | **Specified correction:** visible/editable, part of benchmark eligibility, no hidden-blocked winner; ordinary named semantics retained |
| Include away / Ignore downed | Both false by default; saved per Bill | **Specified:** same defaults, explicit quality-standard waiting and current scope/reasons |
| Away origin | Humanlike comp, home despawn record and home spawn clear; source controlled-mech test requires spawn | **Specified composition:** shared S017 home facts, explicit owned-mech/container capability proof; no blanket away-mech parity claim |
| Freshness | Maximum cached 30 ticks, expired sweep above 500 keys | **Proposed improvement:** relevant next-evaluation freshness including paused edits, shared data and measured combined cost |
| Existing work | Base worker prefix can affect UFT/output selection before native continuation branch | **Specified:** S014 committed-work/collection distinction; no extra new batch or creator bypass |
| Native copy/save | Three same-name bool keys, Bill.Clone postfix | **Specified owned state:** full snapshot/lifetime/save recovery, no automatic foreign schema import |
| BWM copy integration | Actual DoCopy(Bill_Production) and DoPasteInto(Bill_Production) hooks only; static flags | **Direct composition:** copy-one/all/new/existing and full linked policies with S014/S017; no internal patches or stale clipboard |
| Global UI interception | WindowStack label matching, generic Widgets.Dropdown label prefix, global 0–20 IntRange replacement | **Not reproduced:** actual owner-scoped controls; preserve unrelated Assign policy painting |
| Default-on feature / medical bills / universal compatibility | User comment asks default-on; source exposes no global setting, UI is production dialog, blanket compatibility is an author claim | **Not inferred:** no new default preference, medical feature or verified universal support |

Recipe-specific candidates, visible range, away-mech capability handling, fresh decisions and complete
copy propagation are explicit proposed refinements, not assertions of exact source parity or separately
confirmed user choices. Source facts and remaining proof stay in MS-Q1–4.

## UI, state and dependencies

Use native production bill details and shared bench/list policy controls. Most skilled, Include away,
Ignore if downed and skill range are entity gameplay choices, not mod settings. No new bottom tab,
debug override, small toggle or unused global preference. Both options begin off; ordinary new bills
retain existing worker defaults unless an explicit destination policy applies. Seven-language text,
UI-scale access and actual native interactions remain implementation work.

Required future dependency **Harmony** must be declared, correcting the source About omission despite
its direct reference. BWM and the original mod are not runtime dependencies of the owned composition.
No optional external adapter is promised. DLC workers require their actual available content/capability.
No XML mutation is selected; shared saved home facts replace blanket runtime comp insertion. Any
future XML patch requires XML Extensions, existence/content guards and merge-preserving operations.

## Contracts and limits

Real modifier binding: source reflects public instance **ImprovedWorkbenches.BillCopyPaste.DoCopy
(Bill_Production)** and **DoPasteInto(Bill_Production)**. Those installed methods exist; that does not
prove source Copy all/new-bill/link integration. Own snapshots preserve all three flags directly.
Potential public contracts include BillExtensions, PawnOriginMapExtensions, CompPawnOriginalMap,
bool save keys and originMap_MostSkilledBill. The installed bounded literal scan found no external
direct consumer; dynamic/uninstalled consumers remain possible. No speculative facade, original
package identity spoof or automatic save conversion is promised.

Source active canonically withholds our overlap. Saved owned restrictions remain visible and
suspended rather than silently broadening to Any worker. Semantic per-patch target admission permits
benign foreign edits and emits bounded startup warnings on failure; MS-T1–4 are design candidates,
not implemented validators. Restored support needs explicit Resume. No Gateway/source assemblies ship.

## Remaining verification

All implementation tasks are unchecked. MS-Q1 tracks benchmark/UX refinements; MS-Q2 exact beta
worker/continuation/away capability; MS-Q3 contracts and full plan lifetime; MS-Q4 guards, tests,
languages, performance, independent review and final native acceptance. Document checks are recorded
in the [sequential ledger](../refinement/specification-progress.md).

Final proof requires actual highest/tied worker starts, lower-worker rejection, intentional waiting,
work-assignment and downing/travel changes, ongoing physical work, copies/reload/recovery and ordinary
Assign policy painting on the reviewed build. A computed maximum or clean startup log is insufficient.
The selection rule changes no recipe costs/yields/research, quality roll or skill value; economic
effects come from who works and when production waits, to be checked in real playability workflows.
