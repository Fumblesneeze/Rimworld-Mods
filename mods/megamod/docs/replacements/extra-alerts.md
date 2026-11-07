# Extra Alerts

## Identity and provenance

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
Replacement **S084/P2-13**, `zylle.ExtraAlerts`, Zylle/Zylleon,
[Workshop1926923167](https://steamcommunity.com/sharedfiles/filedetails/?id=1926923167).
Installed1.7 net10.0 DLL SHA256
`BEE27DC4B9AB1DAFA91AF894CBC12D4869FFB51A04269224ED70498865D03030` matches the committed DLL in
[source1937247](https://github.com/Zylleon/ExtraAlerts/tree/1937247e488842cb39f7b1ac8666fc55ab3a7560).
No affirmative reuse license found in the bounded scan; no source code, binary or art copied into
product. [Research](../refinement/2026-11-04-extra-alerts-research.md) records public-first evidence,
full selected-binary behavior, native checks, deviations and gaps. No beta runtime acceptance.

[Proposal](../../../../openspec/changes/specify-megamod-extra-alerts/proposal.md),
[design](../../../../openspec/changes/specify-megamod-extra-alerts/design.md),
[ordinary alerts](../../../../openspec/changes/specify-megamod-extra-alerts/specs/megamod-colony-alerts/spec.md),
[hidden diagnostic](../../../../openspec/changes/specify-megamod-extra-alerts/specs/megamod-hidden-enemy-diagnostic/spec.md),
[unchecked tasks](../../../../openspec/changes/specify-megamod-extra-alerts/tasks.md).

## Behavior inventory

All rows **specified only; nothing implemented or accepted**.

| Scope | Observable proposed result | Boundary |
| --- | --- | --- |
| Ordinary display | Hover for current explanation; click cycles valid subjects across locations | Native suppression/priority, consistent counts/names, no hidden disclosure or automatic orders |
| Combat/downed | Visible enemies and distinct downed entity/enemy/nonhostile/unaffiliated people | Actual hostility, no category duplicates, care resolved physically rather than by any queued job |
| Environment | Blight remains until removed; owned rain-vulnerable exposure warns before rain | Designation does not cure; native exposure cell, no foreign/weatherproof building false positive |
| Mood | Six actual negative-thought reports | Bonding, tainted/human-leather apparel, lovers/shared beds, ascetic room; nullification/no-mood safety |
| Health | Colony-animal minor-or-worse temperature illness; Odyssey colonist quills | Native minor baseline20%; quills include travel, no automatic care |
| Equipment | Eligible unarmed free colonists | Juveniles optional; primary melee weapon suffices; no hunter/ranged requirement |
| Trade | Actual available visitor or orbital trade opportunity | Real trade ship, supported same-map communicator, honest temporary outage and native player contact |
| Hidden information | Explicit read-only developer diagnostic | Top toolbar, transient, no ordinary alert/count/message or hidden-target selection |

## Deviations and composition

Source has20 concrete alerts and21 serialized booleans. Owned ordinary catalog is19, with hidden
information relocated to developer controls under the user's hierarchy. Fix unused entity toggle,
grey-but-editable child dependency, stale explanations, duplicate downed categories, ready-unrelated-job
suppression, ascetic nullification/no-mood handling and native electrical lister ownership ambiguity.
Mood alerts require actual negative effects. Orbital presence distinguishes trade ships from other
passing vessels and admits supported terminal variants instead of exact CommsConsole naming; it adds
map/terminal navigation and honest outage information. These are intentional draft corrections.

Native wild-animal hypothermia debug reporting remains distinct from owned colony-animal alerts.
S069 blight cutting, S039/S040 trader gathering, S027 delivery and selected medicine/bonded hunting
features retain their action ownership; alerts observe final state directly. No self-patch stack or
private Thing index. No action is considered completed merely because it was ordered.

## UI and settings audit

No source bottom tab. Ordinary reports remain native readout/hover/click controls. Historical activation
messages keep only known information/locations and cannot follow a newly hidden pawn. Actual configuration
uses Alerts / Urgent, Mood, Animals and Other in common mod settings:19 independent On defaults plus
Include child combatants Off. Each has Global default, Current save and Use global with dynamic
inheritance/dormant overrides, disabled inherited editor, protected main-menu/reset callbacks and
next-refresh timing. Entity downed is independently configurable; unavailable DLC keeps dormant values.

Hidden diagnostic is explicitly requested from the top debug toolbar with developer mode on. No
persistent setting, no automatic opening, no ordinary information leak; mode-off/save switch clears
the view and callbacks. Existing pawn/bed/animal/trade/world actions remain contextual native controls.
Seven-language authoring/rendering remains due; the source's six language folders are not baseline
coverage or proof of rendered correctness.

## Contracts, omissions and remaining implementation

Bounded installed text/binary searches found no incoming runtime consumer. RimPy package records
including a translation-package entry do not consume source helpers. Public types/DefOf/settings
keys are inventoried, not replicated blindly. Original package owns overlap if active; no foreign
settings mutation, package spoof, source-save/config import or universal communicator support.

Unused translation-only night-owl/tameness/handler/rest entries and1.0-only paused caravan are not
implemented by the selected1.7 binary and are omitted here. Any separately selected feature keeps
its own specification. Anomaly containment/content and actual third-party terminal contracts remain
research gaps, not acceptance claims.

**Implemented: nothing.** Native alert registration needs no source Harmony/XML patch. Declare any
future actual dependency truthfully; every actual patch must have executable semantic admission,
local failure/warning and owned-only rollback/later revalidation. XAL-Q1–5 retain provider meaning,
lifecycle/performance, trade adapters, UI/playability and complete implementation/native evidence.
