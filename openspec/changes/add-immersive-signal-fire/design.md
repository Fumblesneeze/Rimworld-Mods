## Context

The old Tribal Signal Fire was a permanently fuelled/glowing 2×2 fire whose job eventually opened the ordinary faction-comms tree. The requested successor is instead a cold signal structure, a world-settlement-filtered contact action, a short group performance, and an uncertain direct request for warriors. RimWorld 1.6 provides the visual vocabulary of its shared ritual/bestowal/childbirth setup and the exact faction, goodwill, cooldown, incident parameters, and worker behavior used by the comms military-aid path.

The owner is Immersive Signal Fire (`fumblesneeze.immersivesignalfire`) at `mods/ImmersiveSignalFire`. Dynamic Effects Forge is required. Show Me Your Tools is optional. No product source or package references the developer gateway.

## Goals / Non-Goals

**Goals:**

- Make settlement proximity, alliance, tech level, selected participants, signal quality, delay, effects, aid, and soot visible and testable through player workflows.
- Keep Core-only game concepts independent of DLC while reusing RimWorld's established begin-lord-job dialog layout and native military-aid mutation.
- Make all participant motion, smoke cadence, delayed responses, and optional blanket state deterministic from saved game ticks except for the explicitly sampled delay.
- Package original, RimWorld-scale raster art without copying the old mod or Workshop assets.

**Non-Goals:**

- Reproduce the comms-console conversation tree, call factions without a nearby settlement, contact industrial factions, or replace caravans/visitors with a custom warrior incident.
- Make the signal structure a heater, room light, cooking station, or ordinary campfire.
- Add a dependency on Ideology, Royalty, Biotech, Simple FX: Smoke, Show Me Your Hands, or the Dev Gateway.
- Promise military aid when RimWorld's native incident worker cannot find a valid arrival result.

## Decisions

### 1. A cold single-use 2×2 structure owns the interaction

`ImmersiveSignalFire.Building_SignalFire` is a 2×2, pass-through, player-buildable structure unlocked by the new Neolithic `ImmersiveSignalFire_SmokeSignals` research project. Its Def has no `CompGlower`, `CompRefuelable`, `CompHeatPusher`, or `CompFireOverlay`; construction therefore creates a visibly cold, neatly arranged wood-only signal stack. The sprite uses a small flat palette of pale green freshly cut wood mixed with brown seasoned wood, no stone ring, restrained shading, and a chunky abstract silhouette suitable for ordinary RimWorld map zoom. A product component owns only active-session state and temporary effects. Once an active signal reaches any terminal result—including interruption—the prepared stack is spent and the building is destroyed after outcome resolution and before final soot placement. Cancellation while pawns are still gathering, before flame or smoke begins, leaves the unused structure in place.

The revised balance target is 60 WoodLog, 400 work, 120 hit points, 500 research points, and a 16-unit ash/soot cleanup. Removing the former 20-block Stuff cost follows the explicit wood-only visual identity and avoids material tint obscuring the pale-green/brown prepared logs. The retained 60-wood price is three Core campfires and 20 wood above the old reusable Tribal Signal Fire: the new object provides no idle cooking, heat, light, fuel, beauty, or meditation utility and is consumed after one attempt, but that attempt can call native military aid before radio technology. Its strongest performance occupies three colonists for 600 ticks and every terminal active attempt consumes the hearth and leaves 16 filth thickness. The native base request of -25 goodwill, including Core's natural-goodwill adjustment, and the 60,000-tick cooldown remain intact, while allied sub-industrial settlement proximity within ten world tiles sharply limits availability. The 2×2 `PassThroughOnly` footprint, three distinct perimeter work cells, constrained-first reachability assignment, construction skill 3, and Tribal/Classic research tags keep the building and synchronized job reachable without making it a free universal contact device.

### 2. One contact catalog drives pawn and no-pawn map menus

The catalog enumerates non-player, non-defeated factions whose `FactionDef.techLevel` is strictly below `Industrial`, whose current player relation is `Ally`, and which owns at least one spawned world `Settlement` whose world-grid approximate tile distance from the current map tile is at most 10. Each faction appears once, ordered by nearest qualifying settlement distance and then faction name.

`Building_SignalFire.GetFloatMenuOptions(Pawn)` supplies the normal selected-pawn route. Core's low-priority `Selector.HandleMapClicks` skips `FloatMenuMakerMap.GetOptions` when no pawn is selected, then consumes the right-button release without opening a menu. A narrow Harmony prefix on the exact public `Selector.SelectorOnGUI` low-priority player-input seam therefore queues the same building action only while the map renderer and player control are active, no input-absorbing window is open, RimWorld's windows and higher-priority handlers have left a right-button release unconsumed, zero pawns are selected, and the clicked cell contains exactly one usable signal fire. A second explicitly owned postfix on `UIRoot_Play.UIRootOnGUI` flushes that one-shot queue after Core's remaining map-input handlers have processed the creating event. The menu therefore opens in the same GUI frame without depending on simulation updates while paused, and no remaining handler can interpret the creating release as a request to close the new window. The no-pawn projection uses Core's native base `FloatMenu`, because `FloatMenuMap` intentionally removes itself when no pawn is selected; the chosen action revalidates the fire and contacts before continuing. The post-window low-priority seam preserves clicks already consumed by dialogs, menus, and other UI, while the delivery seam never interprets input. The action opens a second native `FloatMenu` containing one faction entry per catalog result. An empty catalog opens that submenu with the exact disabled label requested by the user. A no-pawn faction choice opens participant selection with no locked caller; a pawn choice locks that valid actor as caller. The catalog and locked caller are re-evaluated when the faction entry is chosen so stale diplomacy, world, or pawn changes fail closed rather than silently changing routes.

### 3. Reuse the shared ritual/bestowal/childbirth presentation without a DLC dependency

`Dialog_BeginSignalFire` is a Core `Window` that composes the same two-column participant/quality/outcome presentation used by `Dialog_BeginLordJob`, the shared ritual/bestowal/childbirth window. It does not inherit that class because RimWorld 1.6's base constructor unconditionally invokes `ModLister.CheckAnyExpansion("Ritual")` and emits a false expansion error for a player who owns no DLC. The signal dialog displays eligible free, spawned, non-downed adult player colonists on the current map; one caller is required and at most two additional helpers can be selected. When invoked by a pawn, that pawn is the locked caller. When invoked without one, the best eligible pawn by displayed signal score is selected as the initial caller and the player may change it. In both routes, up to two highest-scoring eligible helpers are initially selected so a minimized acceptance run can exercise the real group workflow through the ordinary dialog; the player may freely remove or replace helpers.

The dialog reports a 600-tick expected duration, live Melee and Social factors, the computed percentage, and the four deterministic outcome bands. Accepting it revalidates the structure, faction, settlement, participants, reservations, and reachability before any jobs start.

Composing the shared visual vocabulary instead of constructing a synthetic `Precept_Ritual` or inheriting its DLC-guarded window avoids an Ideology dependency, a false expansion warning, and the pretense that this signal is an ideoligion ritual with thoughts, obligations, spectators, or ritual rewards.

### 4. Signal quality is an equal group average with explicit gap behavior

RimWorld's Social skill is the requested charisma analogue. For every selected participant, `(Melee level + Social level) / 40` produces a 0–1 individual score; signal quality is the arithmetic mean of all selected participant scores and is captured when the dialog is accepted so its preview remains truthful. Incapable or missing skills contribute level zero.

The user-supplied strict thresholds leave 15–30 percent unspecified. This change records the following complete assumption:

- quality greater than 50%: immediate noticed-and-understood success;
- quality greater than 30% and at most 50%: delayed noticed-and-understood success;
- quality at least 15% and at most 30%: noticed but misunderstood, with no aid;
- quality below 15%: not noticed, with no aid.

At most three equally weighted colonists keeps the formula legible in the dialog and makes a helper a tradeoff instead of an automatic bonus.

### 5. A saved coordinator synchronizes real pawn jobs

Accepting the dialog issues ordinary `Job` instances to the chosen pawns. The coordinator owns the shared building session while each job reserves only its distinct interaction cell, allowing all three pawns to gather without competing for one exclusive building reservation. A constrained-first backtracking assignment chooses a complete set when one exists rather than greedily consuming the only cell another pawn can reach. They walk there through normal pathing and wait. When every participant is present, the building component starts the shared 600-tick phase on one game tick. All job drivers face and work at the fire until the same saved end tick. A participant becoming unavailable before synchronization cancels cleanly; interruption after the smoke begins fails the signal, extinguishes it, and performs cleanup rather than manufacturing aid.

The component saves the faction load ID, participant load IDs, captured quality, phase, start/end ticks, and cleanup state. A map component saves delayed calls as faction load ID plus due tick. One random delay is sampled at completion from the inclusive range 600–3,600 game ticks; loading a save never resamples it.

### 6. Dynamic Effects Forge renders flame and two spaced groups of original smoke puffs

The required `blues.forge` assembly is referenced for compilation with `Private=false` and is never copied into the package. Product-owned `Blues.SingleTakeDef` definitions and fleck textures describe the temporary flame and dark smoke. An attached `EffectRunner` is ticked only during the active phase.

The 600-tick performance uses a Morse-like grouped-puff rhythm. The original effect is launched at ticks `0`, `60`, `120`, `360`, `420`, and `480`; its final owned runner ends at tick 501. These starts form two groups of three puffs. Each start raises every blanket for exactly the same 21 ticks as one plume emitter and restores the complete original reviewed smoke configuration: the compact grayscale particle artwork, `0.72` emission rate, scale `0.55~1.15`, opacity endpoints `0.88` and `0.82`, movement speed `0.25~0.75`, and travel range `1.7~2.6`. Nothing about the particle or its per-run emission is redrawn or intensified. The user's spacing sketch is cadence guidance only and is not a replacement particle design. The 39 non-emitting ticks between puffs let earlier copies of that original particle rise above later copies, recovering the familiar layered vertical plume without a continuous emitter; the 219-tick break between groups lets the bottom of the preceding slow plume move visibly clear of the source while blankets remain lowered. The product owns one attached smoke runner at a time and explicitly kills it at the exclusive 21-tick boundary, before Forge's attached-runner duration reset could loop it; terminal cleanup also kills that runner. A load during ticks 1–20 of a puff reconstructs the missing transient runner against the original scheduled start, so it still stops at the same exclusive boundary and remains synchronized with the restored blankets. Blankets lower as soon as that emitter stops, no additional emitter starts before the next scheduled tick, and lowered phases start no smoke. The steady fire remains Forge-driven but uses the product's simple white-alpha flame silhouette; stock Forge grayscale fire flecks are excluded because their dark lobes rendered as a source plume even while blankets were lowered. Already-emitted particles remain physical particles and may continue rising and fading naturally.

### 7. Show Me Your Tools conditionally enables one synchronized blanket visual

The original and maintained fork share canonical package ID `meathax.showmeyourtools`; the adapter detects the package ID case-insensitively and validates the expected `JobEffects.JobToolDef` marker only inside the assemblies owned by that matched `ModContentPack`. A same-named type loaded by another mod cannot enable the adapter. On a supported shape, each active participant draws the product-owned blanket texture between their hands and the signal fire. Height/tilt comes from the coordinator's shared emitter phase, so every blanket rises only for the owned 21-tick plume and lowers when that emitter stops. No optional assembly is referenced, no foreign Def database is mutated, and absence or shape drift leaves the core ritual playable while logging one bounded diagnostic.

The blanket is rendered by the narrow product adapter rather than registering a foreign `JobToolDef`: Show Me Your Tools' ordinary per-pawn animator begins when each job begins, which cannot guarantee a shared phase after different path lengths.

### 8. Successful outcomes call RimWorld's native aid mutation once

Immediate success performs the same native military-aid mutation as `FactionDialogMaker.CallForAid(map, faction)` after completion: a base -25 goodwill request processed by Core's `CalculateAdjustedGoodwillChange`, `lastMilitaryAidRequestTick`, the native requested-aid points range, quick-aid arrival mode, and `IncidentDefOf.RaidFriendly.Worker.TryExecute`. Delayed success queues that same native-equivalent action at the sampled due tick. Owning the narrow projection preserves the comms behavior while retaining the worker's Boolean result, so a failed incident reports that no warriors could answer instead of announcing success. The mod posts its own concise message for delayed, misunderstood, unnoticed, interrupted, or no-longer-valid responses; it does not open `Dialog_Negotiation`.

Immediately before a call, the map component re-resolves the faction and requires it to remain non-defeated and allied and the native 60,000-tick military-aid cooldown to have elapsed. A cooldown rejection makes no goodwill, cooldown, or incident mutation and tells the player to wait. Settlement proximity is not re-required after the smoke was already understood: the responding warband may already be travelling. The comms option's sub-industrial "cannot arrive in time" refusal is intentionally not copied because it would make every faction explicitly allowed by this signal-fire design unable to answer. Native incident failure is reported as no warriors able to answer; the mod does not refund goodwill after the native-equivalent mutation has begun.

### 9. Completion extinguishes effects and creates bounded cleanable soot

Every terminal active outcome stops and kills all Forge runners, clears active jobs/session state, resolves its outcome, destroys the spent signal-fire building, and then places and verifies exactly 16 thickness units of `Filth_Ash` on walkable cells in the occupied rectangle expanded by radius one. This post-despawn ordering prevents RimWorld's own building removal from deleting part of the promised final ash when older soot has made the footprint dense. An externally deconstructed active fire is already despawned and enters the same final soot path without recursive destruction. This is ordinary cleanable filth. Outcome, soot, and destruction are ordered and idempotent so save/load, external destruction, cancellation, or multiple finishing job callbacks cannot duplicate soot, aid, or destruction. A gathering-phase cancellation is pre-use and does not destroy the cold prepared stack.

### 10. Verification uses two small Gateway-owned float-menu additions

World factions and settlements are bounded test preconditions and can be arranged/cleaned inside the isolated E2E fixture; they do not require a public mutation endpoint. The existing exact-window accept step already drives the dialog's native accept path, and initially selected participants let acceptance cover three-pawn work without a product testing hook. Two reusable nested-map-menu gaps do require Gateway ownership:

1. a typed map float-menu open step that runs `FloatMenuMakerMap.GetOptions` for an exact clicked target with zero or more exact pawn actors, opens and captures the resulting real native menu, and fails closed on stale targets or actor identities; and
2. an extension to exact current-float-menu choice that can explicitly adopt the sole native menu opened by either exact-PID physical input or a faithful typed in-process native GUI-event action, then require and transfer the automation lease to exactly one replacement submenu opened by the chosen option.

The semantic zero-actor `GetOptions` projection is useful for deterministic option-set diagnostics but cannot prove RimWorld's outer selection/input gate, because Core skips the low-priority menu call when no pawn is selected. Final acceptance therefore uses the typed in-process no-pawn selector-seam projection to invoke the public post-window `SelectorOnGUI` seam under a target-centered native mouse-up event while RimWorld remains minimized, followed by the explicit sole-menu adoption mode for safe typed traversal. The Gateway first requires the map renderer and player control, rejects any input-absorbing native window, and uses Core's target-point `WindowStack.GetWindowAt(UI.GUIToScreenPoint(mousePosition))` lookup to reject a native window covering the synthetic event without rejecting unrelated map UI. This exercises the product's exact native GUI-event selector hook but is not physical OS input and does not claim a native window processed the event. Selected-pawn acceptance uses the exact native `GetOptions` semantic map-menu projection and is described as such. Neither route restores, focuses, or foregrounds the game window.

These are specified and tasked in `add-rimworld-dev-gateway`. The product remains entirely unaware of Gateway types.

### 11. Original art is reference-measured and accepted in game

The old fire, Core campfire/brazier, and current RimWorld building silhouettes are read-only visual references. At least two original candidates are generated from one written fixed-camera brief for a neat, prepared, single-use signal stack: no stones, more logs than the former design, predominantly light/green fresh wood mixed with fewer brown seasoned pieces, a small controlled palette, broad matte shapes, restrained gradients, little surface detail, no flame, and no smoke. Candidates are normalized to strict 8-bit RGBA, compared on light/dark final-scale contact sheets, and only the chosen building plus optional blanket/smoke textures are packaged. Final acceptance includes close, ordinary, and far in-game zooms, light/dark terrain, active/inactive states, and a blind context-free visual review.

## Risks / Trade-offs

- **[Nearby world settlement APIs or right-click internals change]** → Keep each seam behind one catalog/patch class, test the RimWorld 1.6 shape, and fail closed without contacting an ineligible faction.
- **[A pawn becomes unavailable during coordination]** → Revalidate before work, reserve distinct cells, and terminate the whole shared session idempotently if a selected participant drops out.
- **[Dynamic Effects Forge changes its public runner/Def shape]** → Treat it as a hard dependency, cover the installed real shape in a process-isolated test, and surface a clear startup error rather than silently substituting vanilla fire.
- **[Optional tool integration changes]** → Consume no optional members beyond a validated marker; disable only blanket rendering and emit one diagnostic.
- **[Direct native aid has side effects before incident failure]** → Preserve native semantics and report the failed arrival; do not attempt a partial rollback of goodwill or faction cooldown.
- **[Three pawns crowd a 2×2 structure]** → Precompute distinct standable adjacent interaction cells and reject start when fewer cells are reachable.
- **[Morse cadence must leave readable space without changing the smoke]** → Centralize the six exact starts as two groups of three 21-tick lifts, preserve every reviewed fleck and emission parameter, and leave explicit no-emission intervals between puffs and groups.

## Migration Plan

1. Add the owned OpenSpec contract and focused RED tests.
2. Add the mod project, Defs, localization, cold building behavior, contact catalog, dialog, jobs, outcomes, effects, optional adapter, and assets in vertical green slices.
3. Add the two separately owned Gateway E2E actions with their own contract tests and focused Gateway regression.
4. Build/install through `mod_build`, validate the allowlisted package, run exact optional-mod integration groups, and complete independent code and visual review.
5. On the reviewed build, launch a fresh minimized isolated process and observe no-contact, participant-dialog, active smoke/blankets, immediate warriors, delayed scheduling, failed signal, burnout, and soot through native player workflows and screenshots.

Rollback removes/disables `fumblesneeze.immersivesignalfire`. Existing saves may retain harmless missing-building/filth references under RimWorld's ordinary missing-mod behavior; no foreign save component or Workshop content is changed.

## Open Questions

- Art candidate selection and final draw size remain intentionally open until final-scale contact sheets and in-game zoom captures are reviewed.
- Construction/research/soot values are provisional balance assumptions and may change only with updated spec values and comparator evidence.
