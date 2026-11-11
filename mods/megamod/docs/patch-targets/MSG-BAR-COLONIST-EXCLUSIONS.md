# MSG-BAR-COLONIST-EXCLUSIONS: absent native selection and mood outlines

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, S071 message placement.
This is a required-operation participant in the existing passive `DrawColonist` geometry observer.
It installs no patch and does not draw, measure or suppress native effects.

The owning contracts are the S071 visible-bar clearance and contribution-specific compatibility
scenarios, plus `MSG-BAR-LAYOUT-PREP.md`. The current bounded runtime declines a selected pawn or
its selected corpse, world view, and **any** positive current `EdgeExpansion`. Positive-outline
geometry remains a later obligation. This conservative interim boundary is independent of mood
visibility/alpha; it avoids claiming that a visible mood edge was measured.

Target: `RimWorld.ColonistBarColonistDrawer.DrawColonist(Rect,Pawn,Map,bool,bool):void`, public
instance, non-generic. Research is the exact1.7.5005 rev1082 source/IL under
`artifacts/Megamod/P0-B/automatic-1.7.5005/{source-1,il-drawer}`. No hash/version is an admission
predicate. Implementation: `MessageBarColonistExclusionGuard`; honest owned-patch fixture:
`MessageBarColonistExclusionGuardTests` in `Megamod.Harmony`.

The participant must prove these effective incoming relationships:

1. The current pawn argument produces the actual mood-threshold local. That same unchanged local
   produces the actual edge-expansion local. Strictly positive expansion controls the unique
   `ExpandedBy`/`DrawAtlas` path, using the supplied rectangle, expansion and native mood atlas.
   No alternate branch may bypass either producer/store or the positive-expansion gate.
2. Selection uses the current pawn's `Dead` value to choose that pawn or its `Corpse` for
   `Find.Selector.SelectedObjects.Contains`. False selection bypasses native selection drawing;
   `!WorldSelected` is also required. The drawer and pawn arguments remain the actual supplied ones.
3. The caravan-selection call is independently unreachable through the false world-view gate.
   Both selection consumers use the native rectangle derived from the supplied portrait by
   `ContractedBy(-2*ColonistBar.Scale)`. Relevant local/argument changes and address escapes decline.

Required operations join the existing owner's immutable incoming generation and C01 publication
receipt. Relevant inner rewriting fails this contribution; ordinary unrelated work/NOPs remain
eligible. Inspection never executes foreign transpilers. Native exceptions and rendering remain
unchanged on refusal, and admitted manual message placement remains independently available.
The guard does not inventory arbitrary external drawing helpers; root owns the finite known
outward-consumer boundary across the integrated native guards.

Focused test-first host evidence is retained in
`artifacts/Megamod/P0-B/messages-adornments/exclusions-handoff-20261111.md`:15/15 checks pass,
including actual inner rewriting, selection bypass, native pawn/corpse/world/mood paths,
producer-store branches, local/reference-argument mutation, world/mood gate changes and benign NOPs.
Independent review and native acceptance are pending. The passive
fixture executes actual supported/rejected host methods and retains native effects; tests cannot
substitute for the reviewed exact-build native message/settings workflow. Root must personally
inspect the before/action/after view and retain build/mod list, logs, configuration hashes and
cleanup. No S071 acceptance task is closed by this participant.
