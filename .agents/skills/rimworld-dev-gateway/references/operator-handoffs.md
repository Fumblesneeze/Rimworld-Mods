# Governed live-game operator handoffs

Delegate repetitive live interaction without delegating acceptance. The governor defines distinct observable
outcomes, assigns non-overlapping work inside one persistent colony, reviews friction, and personally inspects the
final live frames. Operators drive the game and report evidence; their reports are never final acceptance by
themselves.

## Shared-colony ownership and parallelism

- Put parallel operators in the same persistent RimWorld process, map, colony, and native save unless the user
  explicitly asks for separate processes. Do not clone or restart the game merely to make coordination easier.
- Give every operator a different objective, spatial work area, and exact owned Thing/Pawn/job set. Duplicate
  attempts at the same task are not parallel progress.
- Reads may run concurrently. Mutations may run concurrently only when their target sets and native side effects do
  not overlap. Do not let two operators alter the same Thing, pawn, bill, zone, blueprint, reservation, or job.
- Camera, selection, open windows, game speed, pause state, keyboard focus, mouse input, and placement/designator
  state are process-global shared resources. Hold them only for a short control block, restore or hand them off, and
  never assume they remained unchanged while another operator was active.
- Use named native save checkpoints for the shared colony. Only the governor coordinates save/reload/restart because
  those operations replace or suspend the state underneath every operator.
- If operators repeatedly collide, record the exact shared resource and add Gateway coordination or a typed semantic
  control. Do not use process isolation as the default workaround.
- Keep the operator on typed Gateway reads/actions, native gizmos/designators/context menus, camera, and exact-PID
  local input. Raw C# may diagnose a genuinely missing seam; it must not become the scene editor or manufacture the
  claimed gameplay outcome.

## Governor task brief

Provide all of the following:

```text
Shared process: <PID, start UTC, run ID, manifest path, package order>
Task lane: <distinct objective, map area, owned Things/Pawns/jobs>
Current checkpoint: <save name and observed state>
Observable objective: <player action -> visible result>
Allowed setup: <bounded preconditions the operator may arrange>
Forbidden shortcuts: <direct mutations/outcome construction/restart>
Required evidence: <before/action/after views, inspect pane, log or state support>
Stop conditions: <missing control, stale process, unexpected state, error window>
```

Keep objectives narrow. A useful operator slice ends at one stable checkpoint or one clearly described missing
capability, not at “finish the whole showcase.”

## Operator loop

1. Validate exact PID/start/run and read game/UI state before every control block.
2. Revalidate the assigned target set and confirm no other operator currently owns it.
3. Inspect the live map through Thing queries and screenshots; do not reconstruct state from old coordinates.
4. Perform the native player command or faithful control path. Let power, plumbing, jobs, reservations, rooms,
   temperature, and pathing settle for ordinary ticks before judging the result.
5. Reapply and read back camera state immediately before each retained capture. Clear selection unless the requested
   evidence needs an inspect pane.
6. Release or restore process-global camera, selection, window, speed, and input state after the control block.
7. Ask the governor to create a named native checkpoint after a stable shared-colony improvement. Never save/reload
   unilaterally while another operator is active.
8. Stop on an absent control instead of substituting raw mutation. Describe the exact recurring operation.

## Handoff report

```text
Process identity: <PID/start/run/packages>
Task lane and exact targets: <area, Things, Pawns, jobs>
Checkpoint produced: <save/path/time>
Native actions performed: <ordered list>
Visible observations: <what changed in each retained frame>
Evidence paths: <before/action/after screenshots>
Restored state: <pause/speed/camera/selection/windows>
Friction: <operation, attempts/time, why existing controls were insufficient>
Gateway proposal: <only when recurring; request, response, cancellation, ownership, stale-handle rules>
Remaining work: <one concrete next objective>
```

## Turn friction into tooling

The governor classifies each friction report:

- **Existing semantic control:** improve the task brief or this skill; do not add an endpoint.
- **One-off native UI:** use exact-PID input with current bounds and preserve focus/input cleanup.
- **Recurring structured operation:** propose a typed endpoint or versioned automation, then implement it through the
  Gateway OpenSpec/TDD/live-smoke workflow before relying on it.

Good recurring endpoint candidates include bounded work-priority/schedule edits, exact contextual float-menu order
selection, furniture uninstall/reinstall/rotation, governor-owned native save/checkpoint creation, stable settlement
waits, and task/object leases for coordinating multiple agents in one live colony. A coordination lease must be scoped
to exact map/Thing/Pawn/task targets or a named process-global resource; it must not serialize unrelated work merely
because it occurs in the same process.
Specify exact targets, observable results, cancellation/deadline behavior, process/map ownership, stale handles,
partial-failure rules, and restoration. Keep these authoring controls separate from product-behavior evidence: an
endpoint that edits a schedule or places furniture proves only the control, while the subsequent ordinary job proves
the mod behavior.
