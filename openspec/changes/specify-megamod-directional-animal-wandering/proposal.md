## Why

Animals should idle in short, generally forward walks that turn around obstacles instead of repeatedly
selecting distant destinations. Extend the selected Pathfinding Lab behavior to hunting companions
and confined animals, allowing short or stationary outcomes in cramped pens and rooms.

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
**Origin: original**, S066/P1-60 interview addition. **Specification only; no gameplay delivered.**
[User refinement](../../../mods/megamod/docs/refinement/2026-11-01-handwritten-notes.md) selects the
directional behavior, hunter reference, short confined walks and bounded no-move handling.
[Research](../../../mods/megamod/docs/refinement/2026-11-04-directional-wandering-research.md)
separates those decisions from native/reference observations and proposed DW-Q1–5 policies.

## What Changes

- Preserve a generally persistent direction, adjacent movement and obstacle turns for ordinary
  wildlife, with the native herd reference where appropriate.
- Supply S065's idle companion movement around its hunter; keep purposeful regrouping and
  finishing/retrieval under their existing authority and normal routing.
- Let animals in a recognized enclosed pen or room idle locally from their own position, with
  proposed 2–3 steps rather than the reference's 3–10, and no herd-mate center.
- Accept one-step, shorter and stationary results. Never chase a requested minimum, spin through
  repeated failed jobs, or escalate a trapped idle animal into a distant destination search.
- Recheck physical access, enclosure changes, current permissions, hazards and interruptions;
  preserve real movement timing, normal needs, shelter-seeking, feeding, fleeing and player orders.
- Provide common inherited tuning and a small bottom-right operational toggle, replacing the
  reference's unrelated whole-pathfinder control surface. Use shared/native spatial facts.

Exact confinement classification, context precedence, door policy, tuning limits and lifecycle
choices are explicit proposals, not additional interview confirmations. No entire experimental
pathfinder, oversized-map feature, population change or claimed speedup is selected.

## Capabilities

### New Capabilities

- `megamod-directional-animal-wandering`: local idle walks, herd/hunter/enclosure boundaries,
  bounded blockage handling, physical movement, priority, lifecycle and contextual configuration.

### Modified Capabilities

None in the archived baseline. S065 supplies companion authority and the shared follow-distance
setting; S024/S030 retain their separately specified shelter/recovery decisions.

## Impact

Future movement planning, narrow native job/follower adapters, shared spatial context and UI text
belong to megamod. Reference source/tests remain read-only. No Pathfinding Lab assembly, runtime
dependency, source package impersonation, settings migration or old performance acceptance is used.
Other locomotion/map providers require optional, package/shape-admitted adapters; none is required
for ordinary supported ground animals. Harmony becomes required on eager use; XML Extensions
becomes required on the first actual XML patch. No Gateway or DLC dependency.

Per-patch target documents, focused TDD, independent implementation review, measured combined cost,
final balance and personally observed native acceptance remain unchecked future work.
