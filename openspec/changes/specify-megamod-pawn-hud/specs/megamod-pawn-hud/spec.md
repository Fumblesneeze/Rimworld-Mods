# Selected-pawn HUD

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`).
Target: **RimWorld 1.7 beta**. S127 observable draft; implementation and acceptance remain pending.

## ADDED Requirements

### Requirement: Show a summary of the single selected pawn

The HUD SHALL show the single selected pawn in map view, initially integrated into the inspect
pane. A separately positioned floating presentation SHALL be available. Switching selection SHALL
replace the subject before any interaction; selecting several objects, a non-pawn, nothing, or
entering the full world view SHALL return to the applicable native interface without stale pawn
controls. Specialized pawn-like objects such as supported vehicles SHALL retain their native
inspection until a specific HUD integration is admitted. The HUD SHALL NOT reveal information
the ordinary inspection of that subject is not permitted to disclose.

The default integrated layout SHALL group identity and policy shortcuts above condition/needs and
skills/training, with current work and equipment below. The floating layout SHALL present the same
groups vertically with a name heading. Missing or inapplicable information SHALL collapse without
invented zero values. The four configurable display groups SHALL distinguish player-managed
humanlikes, player-managed creatures, other humanlikes and other creatures; player-managed includes
subjects hosted by the player, but display membership SHALL NOT grant control permissions.

#### Scenario: Switch subjects while paused
- **WHEN** the player selects a colonist, an animal and then a different colonist while paused
- **THEN** each selection shows that subject's applicable information immediately, with no previous
  pawn's values or active controls carried into the new selection.

#### Scenario: Unsupported subject or full world view
- **WHEN** selection changes to several pawns, a building, an unsupported vehicle, nothing, or full world view
- **THEN** the owned pawn summary disappears and the applicable native inspection remains usable.

### Requirement: Preserve native inspection and screen access

Both presentations SHALL preserve accessible native inspect tabs, select-next-in-cell, information
cards and admitted third-party inspect buttons. Resizing SHALL keep the tabs, their contents,
tooltips and contextual controls within the usable screen at the current UI scale. Excess content
SHALL remain reachable through scrolling or overflow access rather than silently disappearing.
Unrelated objects' inspect panes SHALL retain their native dimensions and behavior.

In floating mode, modifying the underlying inspect pane SHALL be optional; when enabled, its unused
summary area SHALL offer a scrollable pawn activity/combat log, enabled by default, showing up to
the latest **300** native entries. Selecting another pawn or changing map SHALL reset its scroll
and contents. Floating-HUD letter avoidance SHALL preserve every letter and its native actions;
only letters that would overlap SHALL be moved or bundled. With no overlap or no visible HUD,
ordinary letters SHALL remain visible. Tutorial guidance SHALL remain accessible.

#### Scenario: Open a native tab from the resized pane
- **WHEN** the player opens Health, Gear or Genes from a pawn's resized pane
- **THEN** the native tab and its controls remain usable and correctly positioned, without losing
  admitted custom inspect buttons or changing the size of an unrelated building's pane.

#### Scenario: Floating window and letters
- **WHEN** the player selects a pawn with floating mode enabled and receives enough letters to overlap it
- **THEN** every letter remains reachable and actionable, including any bundled overflow;
  deselecting the pawn restores ordinary letter placement without losing letters.

#### Scenario: Resolution change
- **WHEN** screen resolution or UI scale changes so the configured HUD no longer fits
- **THEN** its controls and full contents remain reachable within the usable screen, without
  silently overwriting the player's saved preferred dimensions or hiding tutorial guidance.

### Requirement: Show identity and detailed background

The HUD SHALL display name, gender when applicable, race/xenotype or known transformed kind,
biological age in years with days since the birthday, role/title, relation and relevant faction.
Birthday indication SHALL replace the day remainder on the birthday. It SHALL distinguish player,
ally, neutral, hostile, wild, prisoner and slave relationships using configurable colors plus text.
Animal descriptions SHALL distinguish applicable pet, predator, pack, herd, farm, insect and other
creature roles using actual known characteristics, without altering those characteristics.

Identity tooltips SHALL expose available title, faction, ideology, backstories, traits, work
incapabilities and psychic/meditation/ideological statistics. Animal tooltips SHALL instead expose
trainability, training decay/tameness persistence, diet, petness and master. Missing DLC, backstory,
traits or other data SHALL simply omit those details. Identity shortcuts SHALL open the subject's
available Bio or Social tab, and SHALL NOT enable a tab unavailable through normal inspection.

#### Scenario: Humanlike and animal background
- **WHEN** the player hovers a colonist's identity and then a tame animal's identity
- **THEN** each tooltip presents its applicable background; the animal shows training/diet/master
  information rather than invented human backstories, and clicking opens an available native tab.

### Requirement: Summarize health without concealing urgent conditions

The HUD SHALL show current overall health percentage, a summary warning and a tooltip listing
visible conditions with their body part or whole-body scope in native health-display order.
Hidden conditions SHALL remain hidden. A condition SHALL be described as beneficial, permanent or
immune, tended/addictive, or concerning as appropriate; color SHALL supplement readable text.

The summary SHALL use this ordered source-derived policy:

1. Dead subject: dead.
2. Untended bleeding with the game's predicted blood-loss death time below **one day**: bleeding
   warning with that time.
3. Conditions currently requiring tending: name the single condition or show their count;
   use critical severity if any is life-threatening, otherwise warning severity.
4. Life-threatening conditions: identify a single condition or their count as critical.
5. Other active harmful sickness/lethal-progression conditions, excluding permanent, missing-part,
   fully immune and beneficial conditions: name the strongest pain/severity concern and summarize
   additional conditions.
6. Downed subject: incapacitated.
7. Otherwise: health below **30%** critical, **30% to below 60%** poor, above **90%** with no visible
   harmful condition good, and stable for the remaining cases.

These are presentation thresholds, not changes to damage, tending, immunity or survival. The tooltip
SHALL retain all visible conditions even when one summary takes precedence. Clicking the summary or
health bar SHALL open/toggle the normal Health tab. S137's subsequent condition selection/filter
belongs inside that tab; a HUD click SHALL NOT order surgery or select an arbitrary condition.

#### Scenario: Untended infection and an urgent bleed
- **WHEN** the selected pawn has a visible untended infection and a bleed predicted to kill within a day
- **THEN** the summary prioritizes the bleeding time, the tooltip still includes both conditions,
  and clicking opens Health without ordering or performing treatment.

#### Scenario: Health boundaries and hidden illness
- **WHEN** health is exactly 30%, 60% or 90% and no higher-priority visible condition applies
- **THEN** the respective summary is poor, stable or stable; a hidden illness neither appears nor
  changes a warning in a way that discloses it before native inspection would.

### Requirement: Summarize mental state and actual needs

The HUD SHALL show available mood, food, rest, recreation, suppression, mech energy and configured
additional needs as labeled bars with actual percentages. Inapplicable needs SHALL be absent.
Mood SHALL mark the pawn's current native break thresholds. Percentage text SHALL remain truthful
above 100% where the owned need-overflow feature allows it, with the bar itself remaining bounded;
the HUD SHALL NOT change need values, break chances or rates.

Mental summary precedence SHALL be stun/trance, stagger, current mental state, imminent extreme,
major or minor break, inspiration with remaining time, then mood above **90%** excellent, above
**65%** good, otherwise neutral. Aggressive/extreme mental states and imminent extreme breaks SHALL
be critical warnings; other breaks/stagger SHALL be warnings. Actual native rules determine whether
a break is imminent. Mood tooltips SHALL list native thought groups, multiplicities and their
effective total mood contribution, plus inspiration; they SHALL NOT assume stacked thoughts add
linearly when the game scales their effect. Thought magnitudes at or beyond **-10/+10** SHALL use
critical/excellent presentation, lesser negative/positive values warning/good, zero neutral.

Need tooltips SHALL include applicable diet/eating/bed hunger rates, energy fall per day, rest rate,
beauty/comfort/outdoors, or suppression fall/terror. Clicking a need SHALL open its available native
Needs tab, with suppression opening Slave. Tooltips SHALL omit unsupported statistics.

#### Scenario: Imminent break with an inspiration
- **WHEN** an inspired pawn also meets the game's imminent major-break condition
- **THEN** the summary shows the break warning, while the tooltip retains the inspiration and
  correctly aggregated thoughts; no mood or break behavior changes.

#### Scenario: Energy and overflow
- **WHEN** a normal-energy mech or a pawn with an above-100% supported need is selected
- **THEN** the display shows the real values; the mech is not described as needing transport to a
  charger merely because it has an energy need, and the overflow value is not silently capped to 100%.

### Requirement: Show skills and their useful consequences

The HUD SHALL offer the twelve ordinary skills and discovered additional skills. Each SHALL show
level, disabled state, passion and current progress to the next level; default progress presentation
is the integer level followed by a smaller two-digit percentage remainder, rounded and bounded to
**00–99**, and explained as progress rather than a fractional skill statistic. The player SHALL be
able to hide that remainder. Disabled skills SHALL show a dash. Emphasis precedence SHALL be
disabled, daily learning saturation, active-job skill, then passion. The S128 passion display SHALL
compose directly when specified/implemented; unknown passion kinds SHALL remain bounded and readable.

Skill tooltips SHALL show the native skill explanation and applicable general work/labor speed,
plus the following consequences without changing them:

| Skill | Additional applicable statistics |
| --- | --- |
| Shooting / Melee | Shooting accuracy, aiming delay / melee damage rate, hit and dodge chance |
| Construction | Success, construction speed, repair success, smoothing speed |
| Mining | Yield, speed, deep drilling speed |
| Cooking | Cooking speed, poisoning chance, flesh butchery yield/speed, drug cooking speed |
| Plants | Work speed, harvest yield, pruning speed |
| Animals | Gathering speed/yield, tame/train chance, hunting stealth |
| Crafting | Smelting speed, mechanoid butchery speed/yield |
| Medicine | Operation speed, surgery success, tending speed/quality |
| Social | Negotiation, trade improvement, beauty, social impact, conversion/suppression power |
| Intellectual | Research and hacking speed |
| Artistic / other skills | Native skill explanation and applicable general work statistics |

Missing, disabled or DLC-inapplicable statistics SHALL be omitted. Opening a skill SHALL lead to
the subject's available Bio tab. Number punctuation and styled quantities SHALL use the owned
readable-number preferences coherently, without rewriting markup or changing numeric meaning.

#### Scenario: Active and saturated skill
- **WHEN** the selected pawn's active skill reaches its daily learning saturation
- **THEN** the HUD shows saturation emphasis, retains level/progress/passion information and opens
  the native skill context without awarding experience or changing work priorities.

### Requirement: Show animal training and current pawn activity

Applicable animal training SHALL show wanted/disabled state, learned emphasis and completed versus
required training steps, with a tooltip explaining training and decay. The widget catalog SHALL
include ordinary tameness, obedience, release, rescue and hauling plus discovered available
training such as attack-target, comfort, digging, foraging, roars/trumpeting and egg/sludge spewing.
Training/master clicks SHALL open available Training/Social tabs rather than issuing training orders.

The HUD SHALL show colony prisoners' resistance or unrecruitable reason, and will when applicable;
prisoner information SHALL open Prisoner. It SHALL show actual current job/group activity or attack
target, first queued job plus the count remaining, primary equipment/restraints and current carried
item. Activity SHALL open Work only where available; equipment/cargo SHALL open Gear. Supplemental
component/quest information SHALL remain available, with special emerging/shutdown descriptions
only while those conditions are actually true. Traits, selected pawn statistics and records SHALL
be available as additional widgets; time records SHALL use durations. Hovering current activity
SHALL show its applicable work type and relevant skills. Hovering the standalone Traits widget
SHALL show each listed trait's complete native description for that pawn, without revealing hidden traits.

Owned batch cargo SHALL be identified through the existing hauling/cargo presentation, with each
physical stack counted once and distinguished from personal inventory. The HUD SHALL NOT add a
second cargo store or change carrying limits. All summaries SHALL reflect the same pawn state as
the native and other owned views.

#### Scenario: Training and queue changes
- **WHEN** a selected animal completes a training step or a selected pawn finishes its current job
- **THEN** the next refresh shows the new training/queue state, without a duplicate order or a stale
  master, job, carried stack or prisoner value from an earlier selection.

#### Scenario: Batch cargo inspection
- **WHEN** the player selects a pawn carrying several owned hauling stacks and opens its cargo details
- **THEN** the HUD and Gear/cargo presentation agree on the real stacks and counts, distinct from
  personal items, without doubling the native hand stack or granting extra carrying capacity.

#### Scenario: Activity and trait explanations
- **WHEN** the player hovers a working pawn's activity and then its standalone Traits widget
- **THEN** activity explains the applicable work type and relevant skills, and Traits exposes each
  listed trait's full native description; absent work-type data is omitted without inventing a role.

### Requirement: Offer contextual policy shortcuts without broadening authority

For pawns with the corresponding native permission, the HUD SHALL offer food policy, apparel
policy, current-hour schedule and allowed-area selectors with their current values and native
management entry points. Food policy includes controllable hosted pawns and supported animals only
when their actual food policy is configurable. Area hover SHALL highlight the current applicable
area; choices SHALL include unrestricted, currently assignable areas and area management. Schedule
selection SHALL change only the hour identified when the menu was opened, visibly labeled there.

Medical care, hostility response, eligible self-tend, rename, xenotype, faction and ideology
controls SHALL preserve their native eligibility, actions and explanations. Self-tend SHALL retain
native doctor incapability restrictions and the warning when doctor work is disabled; its tooltip
SHALL reflect the game's actual efficiency rather than a hardcoded obsolete multiplier.

Menus SHALL stay bound to the pawn, map, policy and hour they identify. If selection, control rights,
map or the chosen object changes before activation, the action SHALL revalidate and either affect
that still-valid intended subject or refuse with an explanation; it SHALL never silently target the
new selection. These choices are pawn/gameplay state, not inherited HUD settings.

#### Scenario: Change food policy from the HUD
- **WHEN** the player selects a controllable pawn and chooses a currently valid food policy
- **THEN** that pawn's native policy changes once and both HUD and native assignment view agree;
  unrelated pawns and global HUD preferences remain unchanged.

#### Scenario: Menu survives a selection change
- **WHEN** the player opens pawn A's schedule menu, then selects B or the hour advances before choosing
- **THEN** the action never edits B or an unlabeled hour; it applies to the still-valid stated pawn
  and hour or refuses if that target is no longer valid.

#### Scenario: Native permission blocks a shortcut
- **WHEN** a pawn cannot perform doctor work or cannot have an allowed area assigned
- **THEN** the HUD does not provide a functioning shortcut that bypasses that restriction, and an
  already-open stale action is rejected at activation.

### Requirement: Keep display refresh and view control separate from gameplay

Ordinary visible values SHALL refresh at the configured real-time interval, initially **500 ms**,
including while paused. Selection, accepted local HUD actions and applicable configuration changes
SHALL refresh before the next interaction. Hidden HUDs SHALL not keep presenting stale tooltips or
active hit targets. Map/save changes SHALL discard the previous subject and displayed context.

A small bottom-right control SHALL show/hide the owned HUD, with visible state, initially on for a
save without a stored choice and retained with that save. It SHALL be unavailable in full world view
and SHALL NOT alter pawn policies or run jobs. Configuration SHALL remain accessible from mod settings
while the HUD is hidden. Any gear/shortcut navigation SHALL lead to that same settings section,
not a separate configuration system. Timing diagnostics SHALL exist only in the top debug toolbar,
default off and session-only; disabling developer mode SHALL remove them.

#### Scenario: Hide and restore
- **WHEN** the player hides the HUD, saves, loads a different save and later reloads the first
- **THEN** each save retains its own view choice, native inspection is available while hidden, and
  settings remain accessible without selecting a pawn.

### Requirement: Isolate unsupported integrations and failures

The HUD SHALL operate without optional DLC or providers. Admitted additional needs/widgets and
native inspect extensions SHALL appear only when their content and callable behavior are supported.
An unavailable widget SHALL preserve its saved layout position as an identifiable inactive entry;
one faulty provider SHALL not erase the layout or disable unrelated working information.

When original RimHUD is active, its HUD SHALL be the sole renderer of this overlapping feature:
the owned HUD and its overlapping hooks SHALL remain inactive with one bounded startup warning.
Other compatible patches alone SHALL NOT be grounds for rejection. An enabled patch or atomic
group whose documented semantic prerequisite fails SHALL be withheld with a startup warning naming
the failed target, missing behavior and fallback. Unrelated features and foreign patches SHALL be
left intact. Core display failure SHALL restore usable native inspection and offer diagnostics
without resetting settings; provider/action failure SHALL remain localized and non-repeating.

The owned HUD SHALL NOT claim to satisfy upstream package dependencies, private patch targets or
binary bindings merely because it offers similar information. Unsupported bindings SHALL be
documented. Multiplayer support and synchronized gameplay shortcuts are deferred outside initial
support; upstream adapter research SHALL NOT be treated as an admitted owned multiplayer session.

#### Scenario: Malformed custom widget
- **WHEN** one provider throws while preparing, drawing, hovering or clicking its widget
- **THEN** that widget becomes inactive with bounded diagnostics, the remainder of the HUD and
  native tabs stay usable, and saved layouts/preferences are not erased.

#### Scenario: Co-installed original or changed game target
- **WHEN** original RimHUD is active or a required game hook no longer meets its target contract
- **THEN** the overlapping owned behavior is not installed, startup identifies what was skipped and
  why, and the original/native inspection remains available without unpatching another owner.

### Requirement: Provide readable localized information

All owned labels, help, disabled reasons, settings and diagnostics SHALL support the seven baseline
languages. Long names, translated labels, color choices and UI scaling SHALL not make information
or actions inaccessible; truncated values SHALL expose their complete text. Warning meaning SHALL
not depend on color alone. Provider-authored labels SHALL retain their provider's localization,
with a stable identifier fallback where no usable label exists.

#### Scenario: Long translated labels
- **WHEN** the player uses a baseline language with long skill, policy or condition labels
- **THEN** values, complete-text access and action targets remain readable and correctly associated,
  including warnings when colors are customized.
