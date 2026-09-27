## Context

Owner: repository tooling RimWorld Dev Gateway (`mods/RimWorldDevGateway`). Core Selector consumes
UI.MousePositionOnUI, which reads Unity Input rather than Event.current. Synthetic GUI events alone
therefore cannot place its pointer. Direct Select(Thing) would bypass the behavior under test.

## Goals / Non-Goals

Goals: finite fractional map x/z, native left-click selection/cycling, minimized operation, typed
semantic API and E2E step with identical implementation. Non-goals: drag selection, modifiers,
double-clicks, right-click orders or simulated desktop input.

## Decisions

- Scope a Harmony override of UI.MousePositionOnUI and Selector.ShiftIsHeld to a synchronous native
  SelectorOnGUI mouse-down/up pair. Override only pointer input and neutralize desktop Shift.
  Do not override target queries, eligibility, candidate ordering or selected objects.
- Validate settled player-controlled map, finite in-bounds visible coordinates, no obstructing windows,
  active designator/targeter/debug tool or drag before dispatch. Fail closed on unsupported seams.
- Use a unique patch owner; remove only those exact prefixes in finally, including partial installation.
  Restore Event.current and drag state. Preserve native resulting selection and inspect tab; expose
  before/after selected IDs. Failures retain their actual result; do not fabricate rollback of native effects.
- Register `map.pointer.select` with required numeric x/z on existing authenticated semantic API and
  repository gateway_mutation operation. Add a typed E2E step calling the same leaf.

## Risks / Trade-offs

Short-lived detours cost more than direct selection but clicks are bounded control operations, never
tick work. Shape guards plus focused host tests and a real native-selection run gate acceptance.
Host tests cannot prove Unity pointer projection; exact-run screenshots and native selection do.
