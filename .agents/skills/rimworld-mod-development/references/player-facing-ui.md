# Player-facing RimWorld UI and behavior

## Make alerts actionable

An alert should identify an unexpected condition the player can fix now. Before adding one, require:

- a real owned building/bill/job or policy that currently needs the missing resource;
- an eligible non-drafted pawn or other actor for whom the condition matters;
- no ordinary fallback already resolving the problem;
- sufficient importance that persistent screen attention helps rather than annoys.

Examples: do not alert for drafted pawns during every battle; do not alert about missing kitchenware
when only hand-eaten food/campfire recipes exist; do not alert merely because all existing ware is
dirty when chefs/cleaners can wash it. Alert for zero required kitchenware only while an owned active
recipe actually requires it.

E2E alert tests start from a clean slate. Clear live messages, visible/delayed letters, alert readout,
and test windows between cases, then drive the condition through the player-facing configuration.

## Do not reveal hidden simulation state

Inspect panes and labels must show what a player could reasonably know. Keep latent food-poisoning,
contamination, hidden provenance, and sanitation-risk rolls internal unless the game exposes their
effects. Toxic buildup becomes visible through the pawn's normal hediff/health UI at vanilla
thresholds; do not label an item “poisoned,” “not poisoned,” or expose a hidden wash-source marker.

Suppress hidden inspect output structurally at its owning comp/section. Never filter a few English
literal strings: that leaks in other languages and when wording changes. Restore temporary structural
suppression in a finalizer so exceptions cannot leave global inspection state changed.

When the game reports a causal notification the player is entitled to see, correlate the exact pawn,
Thing, and newly created message and report the strongest actual contributing cause. Do not invent
`unknown` when the product tracks a dominant cause, and do not expose predictive risk before the
event occurs.

## Offer sensible native actions

- Prefer clean/default behavior automatically.
- If a recoverable resource exists, let the actor resolve it (for example wash dirty equipment before
  cooking) rather than wait forever for a separate workgiver.
- Put risky overrides in an explicit native right-click option with honest player-facing wording;
  keep hidden probability internals out of the label unless the design explicitly exposes them.
- Use the native float-menu/gizmo path and exact eligibility/rejection behavior. A direct job start is
  not proof that the player can issue the order.

## Reuse the neighboring native control

Before adding a button or modal editor for a value, inspect the controls beside its intended row.
When the same kind of value already uses a native slider, call that control with its native
geometry and interaction conventions. Do not add a separate editor unless a specified behavior
needs it. Inspect normalization timing: some native slider rounding runs during every draw, so
apply input rounding only after actual interaction when saved precision must survive painting.
Inspect the control's native event gate too: on Windows a slider can claim MouseDown but update
only on MouseDrag. A consumed press is not an adjustment. Native verification must send the real
held movement and also check that a press without movement preserves retained precision.

After capturing a native tooltip, move the scoped pointer to an unobscured inert window margin
through ordinary input and observe the tooltip window disappearing before the next gesture.
Native hit-testing can reject a later slider drag while that tooltip covers its path. Do not remove
the tooltip directly or weaken the input obscuration guard; budget the native transition in the fixture.

## Keep terminology and localization coherent

Choose one concept name and use it across Def labels, recipes, categories, jobs, alerts, settings,
inspect text, docs, and translations. Do not expose inheritance/template labels such as `root` in
recipe ingredient text. In this repository, prefer “cutlery” over “silverware”; distinguish a
specific cookware set from the broader kitchenware category.

All product UI text uses translated DefInjected or Keyed strings. Validate every required locale and
render representative narrow UI surfaces; structural correctness in English is not localization.

Search adapters must include the actual displayed fallback title and current group/author labels,
not only raw source metadata. Keep that presentation context explicit so a page-name result cannot
accidentally match unrelated option text, and retain opaque identity separately. Exercise missing
metadata and translated fallbacks through the combined query adapter, including label ranking.

## Check the coordinate space and recovery owner

Native window sizing can consume physical `Screen.width` while positioning and input use logical
`UI.screenWidth`/`UI.screenHeight` after scale/DPI. Sufficient local column width does not prove that
the window or controls fit the viewport. Inspect the actual native producer, test prospective fit
before first paint when behavior depends on editor availability, and recheck current open-window
bounds. Keep scroll-content coordinates distinct from absolute window/viewport coordinates.

Keep transient viewport availability separate from persistent row/entity allocation failures.
Otherwise painting policy A while offscreen can latch its valid rows as failed and block recovery
after switching to policy B. Test recovery with both policies still current, and separately retain
real row failures until that row's allocation is reassessed. For native verification, inspect the
installed backend behind offered resolution/scale controls, including windowed-mode filtering;
the native menu's wrapper alone does not prove alternate choices exist. Use actual offered controls
and their confirmation/restore path. If a window size must be disposable setup, label it as setup
and accept only the subsequent native opening/scale workflow; do not claim native resizing. A scale
rejected by the engine does not exercise a narrow layout. Capture no-manager availability before
opening the window so its actual-window guard cannot mask a broken prospective check.

Display setters can request a resize before native Screen dimensions publish on a later frame.
Use bounded asynchronous cleanup observation through the test context, after closing exact owned
confirmation windows. Keep independent object/control cleanup separate so a failed display restore
does not skip it. A same-call dimension assertion is not proof of eventual restoration.
