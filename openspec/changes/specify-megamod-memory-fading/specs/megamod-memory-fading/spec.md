# Timed memory fading

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
S068; observable draft only. Coverage corrections and defaults are proposed in MF-Q1.

## ADDED Requirements

### Requirement: MF01 Select supported memories by their underlying duration
The feature SHALL apply its policy to supported nonpermanent timed memories whose positive underlying duration lies within the configured inclusive range, initially 0 days through Unlimited.
Underlying means the current ordinary duration before this feature's multiplier, or an authoritative
individual lifetime when one exists. Selection does not change merely because the duration multiplier
changes. Short memories, including exactly 0.1 days, are included by default. Ongoing situational,
permanent and nonpositive-lifetime effects retain their current behavior; excluded memories retain
their ordinary strength, duration and any existing native fading.

#### Scenario: Inclusive bounds remain stable under scaling
- **WHEN** the range is 1–10 days, ordinary memories have underlying durations of 1, 10 and 11 days,
  and the duration setting changes to 200%
- **THEN** the 1- and 10-day memories remain included and the 11-day memory remains excluded
- **AND** revisiting settings or reevaluating memories does not make them enter/leave the range solely
  because the included lifetimes became 2 and 20 days.

#### Scenario: Short, permanent and ongoing effects are distinguished
- **WHEN** a pawn has a supported 0.1-day memory, a permanent effect and an ongoing situational thought
  under default settings
- **THEN** the short timed memory is included
- **AND** this feature does not impose timed fading or expiry on the permanent/situational effects.

#### Scenario: Exclusion preserves existing behavior
- **WHEN** a memory is outside the selected range or has no supported positive lifetime
- **THEN** it retains ordinary provider behavior, including any native fade already present
- **AND** it is not forcibly made permanent, newly faded or treated as a new memory.

### Requirement: MF02 Fade good and bad mood effects exactly once
Included timed memory mood contributions SHALL decline linearly from their current unfaded contribution to zero across their effective lifetime, without a second fade, reversed sign or non-finite result.
At half the lifetime, an otherwise unchanged contribution has half its starting magnitude; at the
end it contributes zero. Both positive and negative effects follow this rule. Native nullification
still gives zero. Age, stacking, grouping, renewal and event acquisition retain their ordinary rules.

#### Scenario: Positive and negative effects fade symmetrically
- **WHEN** unchanged included memories begin with +10 and −20 mood and reach half their respective lifetimes
- **THEN** their individual contributions are +5 and −10 before unrelated group/display processing
- **AND** neither retains full strength until an abrupt expiry.

#### Scenario: Existing native fade and nullification
- **WHEN** an included memory already fades natively or is nullified for its pawn
- **THEN** the ordinary fading memory has one linear fade, not a squared fade
- **AND** the nullified memory contributes zero regardless of its configured strength.

#### Scenario: Renewal and boundary evaluation
- **WHEN** native repeat-event rules renew an included memory, or it reaches/passes its effective end
- **THEN** renewal uses the normal reset age and existing stack/group rules
- **AND** at/after the end its contribution is zero until native removal, never an opposite-sign effect.

### Requirement: MF03 Tune base mood strength without multiplying unrelated effects
Base mood strength SHALL default to 100%, accept finite values from 0% through 200%, and scale included ordinary base memory mood once while preserving sign and independent effects.
The control does not multiply social opinion values, separately added mood adjustments or a captured
counselling award. Native pawn-specific factors still apply. A legitimate base mood of −1 is a real
effect, not an absent entry. Zero base strength need not erase independent adjustments or opinions.

#### Scenario: Scale a small real negative memory
- **WHEN** an included ordinary memory with base mood −1 is evaluated at 200% strength
- **THEN** its unfaded base contribution becomes −2 before ordinary pawn-specific factors
- **AND** it is not silently omitted because of its value.

#### Scenario: Separate base mood from opinions and added adjustments
- **WHEN** strength is set to 0% for a memory with an ordinary base mood, an independent added mood
  adjustment and a social opinion effect
- **THEN** the ordinary base mood contributes zero
- **AND** this strength control does not erase the independent adjustment or multiply the opinion.

### Requirement: MF04 Tune ordinary lifetime with native expiry and explicit overrides
Ordinary memory duration SHALL default to 100%, accept finite values from 0% through 200%, and scale included ordinary lifetimes once while preserving authoritative individual lifetimes and elapsed age.
For a social memory this also changes its ordinary opinion lifetime; its opinion strength and shape
remain native. Individual lifetime overrides are not multiplied again. Zero effective lifetime gives
zero contribution and expiry at the next ordinary memory maintenance step. Invalid or unrepresentable
calculations must not wrap into negative durations, produce non-finite values or break unrelated memories.

#### Scenario: Lengthen an existing ordinary memory without renewing it
- **WHEN** a 10-day ordinary memory is 5 days old and its included duration changes from 100% to 200%
- **THEN** its lifetime becomes 20 days with the same 5-day age
- **AND** its unchanged unfaded mood contribution is now multiplied by 0.75, rather than being renewed.

#### Scenario: Authoritative individual lifetime is respected
- **WHEN** a supported memory has an explicit 5-day lifetime and ordinary duration is set to 200%
- **THEN** it is selected and faded using those 5 days, not an invented 10-day lifetime
- **AND** the UI explains that ordinary duration scaling does not change such captured lifetimes.

#### Scenario: Zero duration and once-only expiry
- **WHEN** an included ordinary memory's duration becomes zero
- **THEN** it contributes zero immediately on ordinary evaluation and expires through the next native maintenance
- **AND** ordinary successor consequences run only through their normal once-only removal path, without
  invalid arithmetic or resurrection if the duration setting is later increased.

#### Scenario: Social duration changes without opinion-strength scaling
- **WHEN** an included ordinary social memory's duration changes while its base opinion remains the same
- **THEN** its native opinion timing uses the changed lifetime
- **AND** the mood-strength percentage does not become an opinion-strength percentage.

### Requirement: MF05 Supported counselling fades its captured amount once
Supported timed counselling SHALL fade its captured positive mood amount over its own captured lifetime when that lifetime is included, without applying strength or duration multipliers a second time.
Counselling retains native success/failure, eligibility, captured amount and captured remaining
lifetime. It does not continuously recalculate its award after the original negative memory is
renewed/removed or later settings change. Inclusion is based on counselling's own lifetime. The
ordinary boost given when no negative memory qualifies, and failed-counsel memories, follow ordinary
timed-memory rules.

#### Scenario: Captured compensation fades with an unchanged included penalty
- **WHEN** successful counselling captures a currently −10 included penalty with 5 days remaining
  and the resulting 5-day counselling memory is included
- **THEN** its starting bonus is +10, even if configured strength/duration already affected the penalty
- **AND** after 2.5 days under unchanged conditions its bonus is +5; neither its amount nor its
  remaining lifetime receives another multiplier.

#### Scenario: Existing counselling and later source changes
- **WHEN** an existing saved counselling memory is loaded or its original negative memory is later renewed/removed
- **THEN** its stored amount, elapsed age and captured lifetime remain authoritative
- **AND** inclusion/fading uses those values without granting a new counselling award or extending its life.

#### Scenario: Separate inclusion and optional availability
- **WHEN** counselling's captured lifetime is excluded, or the supported counselling integration is unavailable
- **THEN** it retains native/provider behavior; an unavailable enabled integration is explained
- **AND** the feature does not promise exact compensation when the two memories follow different policies.

#### Scenario: Fading can move a penalty below the native counselling threshold
- **WHEN** the current native qualifying threshold is −10 and the pawn's strongest negative memory
  now contributes −9 after fading
- **THEN** successful counselling uses its ordinary fallback boost rather than capturing that penalty
- **AND** this feature does not bypass the current qualifying threshold merely because the memory
  was once stronger.

### Requirement: MF06 Use shared global and save-specific settings
The three configuration rows SHALL use the common Global default, Current save and Use global controls in Mood / Memories, ordered base mood strength, ordinary duration and included baseline-duration range.
New or missing save values inherit. Percentages are finite 0–200%; the range has a nonnegative finite
minimum and an ordered finite maximum or explicit Unlimited. Invalid edits retain the last valid
state. The range commits as one value; a valid range matching nothing is allowed. There is no new
feature toggle, bottom tab or per-pawn settings page.

#### Scenario: Inheritance and dormant overrides
- **WHEN** a save inherits and a global value changes, or the player toggles Use global after making an override
- **THEN** inherited saves follow the current global value while other saves' overrides stay unchanged
- **AND** checking Use global retains the dormant override; unchecking restores it or seeds the first
  override from the current effective global value, with the per-save editor disabled while inheriting.

#### Scenario: Main-menu and reset scope
- **WHEN** settings are opened without a loaded game or the player uses a scoped reset
- **THEN** all current-save and Use global controls/callbacks are inactive at the main menu
- **AND** resets clearly affect only their chosen global/save scope and never another save's values.

#### Scenario: Existing memories follow committed policy without replay
- **WHEN** valid settings are committed, a save is reloaded or a different save is opened
- **THEN** existing and new supported memories use that game's effective policy on the next ordinary evaluation
- **AND** ages are not rewound, removed memories are not recreated and no other save's policy leaks in.

#### Scenario: Invalid inputs and an empty selection
- **WHEN** a player enters negative/unordered bounds, non-finite values, or a valid range with no matches
- **THEN** invalid edits leave the prior valid setting unchanged
- **AND** the valid empty range is retained and clearly reports that no ordinary memory types match.

### Requirement: MF07 Present current effects and honest previews
Settings, supported numerical event messages and ordinary mood displays SHALL explain the active policy using current supported content and distinguish baseline mood, effective duration, social opinion and individually timed effects.
Preview counts refer to memory types, not existing pawn instances. Use current translated names and
values rather than fixed historical examples; identify unsupported/dynamically timed entries without
inventing a fixed lifetime. Missing content or stages must not break other rows. Current pawn mood
remains the actual game-calculated value, subject to its ordinary update cadence. New text supports
all seven baseline languages; no new asset or menu is required.

#### Scenario: Current-content examples and type counts
- **WHEN** settings are opened with changed/removed memory definitions or only opinion-based effects
- **THEN** examples and counts reflect the current supported content and selected range
- **AND** an opinion value is not advertised as a mood value; dynamically timed counselling is described
  separately rather than assigned a fabricated fixed duration.

#### Scenario: Real mood and readable localized controls
- **WHEN** an included memory ages and the player inspects its ordinary Needs/Social display or the shared mood UI
- **THEN** displayed contributions and actual mood/opinion follow the same applicable calculation
- **AND** all supported languages retain readable scope labels, percentages, duration units and explanations.

#### Scenario: Counselling message describes the actual fallback award
- **WHEN** supported successful counselling gives an ordinary fallback boost with base mood +8 at
  200% strength and no other mood modifiers
- **THEN** its initial awarded contribution and success message both report +16
- **AND** the message does not report the unchanged +8 baseline as the granted amount; if the
  required presentation cannot be supported and acquisition is allowed, the affected owned
  transformation declines with the explained native fallback rather than advertising a mismatched award.

#### Scenario: Acquisition exclusion does not hide retained affected memories
- **WHEN** an applied content-selection policy prevents new memories of a type but a pawn still has
  an existing memory of that type
- **THEN** the retained memory continues to follow the current fading policy and ordinary expiry
- **AND** the preview retains the type with its acquisition restriction clearly distinguished from
  fading support; its inclusion in fading-type counts is not removed merely because new acquisition is blocked.

#### Scenario: Pending acquisition choices remain pending
- **WHEN** a content-selection exclusion is requested during play but is pending until reload
- **THEN** current acquisition and previews still describe the applied policy, with the pending request identified
- **AND** fading-setting edits do not apply or cancel that separate pending content choice.

#### Scenario: Counselling award is refused by applied content selection
- **WHEN** an applied exclusion prevents the captured counselling memory or ordinary fallback boost
  from being granted during a supported native counselling action
- **THEN** no excluded memory or substitute bonus is created and the result reports that no such bonus was granted
- **AND** it does not announce successful mood compensation solely because the ability succeeded or
  attempted to add a memory; existing memories retain their current effects and age.

#### Scenario: Result reporting cannot support an applied counselling exclusion
- **WHEN** a counselling award would be blocked by an applied exclusion but its result cannot be
  reported correctly through a supported action path
- **THEN** the affected counselling action is unavailable with an explanation before it consumes
  the ability or grants an effect
- **AND** if safe action refusal also cannot be supported, the coupled exclusion is reported unavailable
  rather than applied; native reporting is never claimed as an exclusion-safe fallback by itself.

### Requirement: MF08 Admit compatible behavior and preserve native ownership
Every enabled runtime patch SHALL check its documented target conditions before application, tolerate compatible foreign changes, and leave its affected contribution inactive with a bounded explanatory startup warning if those conditions fail.
The ordinary fading/strength/duration policy acts as a coherent group; an unsupported core target
leaves ordinary native/provider behavior intact. Optional counselling can fail independently with
its explicit native fallback. Unknown custom memory behavior is not guessed from a name. Warnings
identify the patch, failed condition, unavailable behavior and fallback without per-query spam.

#### Scenario: Coherent fallback and independent supported content
- **WHEN** a required ordinary-policy target fails or a particular custom memory/counselling path is unsupported
- **THEN** the affected policy/adapter leaves its native/provider behavior intact and explains the limitation
- **AND** unrelated supported memory content and other features continue; compatible patches alone do not cause rejection.

#### Scenario: Original source is the selected provider
- **WHEN** original All Memories Fade is active
- **THEN** it supplies this feature alone and megamod's overlapping transformations do not run
- **AND** owned controls explain their unavailable state and retain dormant values without editing the
  original's global settings, spoofing its identity or applying its multipliers twice.

#### Scenario: Native events, persistence and removal remain intact
- **WHEN** ordinary events add/renew/replace memories, a save reloads, or owned memory fading is removed
- **THEN** current native memory identity, stacking, nullification and pending expiry consequences remain authoritative
- **AND** no separate memory store restores already expired effects or changes other features' event rules.
