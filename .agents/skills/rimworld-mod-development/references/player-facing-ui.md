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

## Keep terminology and localization coherent

Choose one concept name and use it across Def labels, recipes, categories, jobs, alerts, settings,
inspect text, docs, and translations. Do not expose inheritance/template labels such as `root` in
recipe ingredient text. In this repository, prefer “cutlery” over “silverware”; distinguish a
specific cookware set from the broader kitchenware category.

All product UI text uses translated DefInjected or Keyed strings. Validate every required locale and
render representative narrow UI surfaces; structural correctness in English is not localization.
