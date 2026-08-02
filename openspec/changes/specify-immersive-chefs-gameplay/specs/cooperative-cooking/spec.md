## ADDED Requirements
Owning mod: **Immersive Chefs** — package ID `fumblesneeze.immersivechefs`; repository path `mods/ImmersiveChefs`.

### Requirement: Lead cooks request kitchen assistance before cooking begins
When a pawn accepts a supported meal-cooking job, Immersive Chefs SHALL create an assistance request as soon as the lead cook reserves the bill and begins collecting its first ingredient. The request SHALL remain associated with that specific bill job while the lead hauls later ingredients and moves to the primary cooking building; it MUST NOT wait until the lead reaches the building. With automatic assistance enabled, available cooks SHALL be offered support jobs up to the configured assistant limit.

#### Scenario: Assistants are called during ingredient hauling
- **WHEN** a lead cook reserves a supported meal bill and starts the first ingredient-hauling toil
- **THEN** the bill job exposes an assistance request before the lead arrives at the cooking building
- **THEN** eligible cooks can reserve linked support stations and travel to them while the lead is hauling

#### Scenario: Unsupported food does not call assistants
- **WHEN** a cook starts a recipe excluded by the meal-production classification
- **THEN** Immersive Chefs does not create an assistance request for that job

### Requirement: Assistance uses linked specialty stations
The mod SHALL provide sauce, meat, vegetable, and pastry support stations, all unlocked by `ImmersiveChefs_ProfessionalKitchens`. A station SHALL be eligible only when it is linked through RimWorld's linked-building/facility mechanism to the exact primary cooking building reserved by the lead, is spawned and reachable, is not forbidden or broken, and has any required power or fuel. Each eligible station SHALL accept at most one assistant, and one assistant SHALL claim at most one station and one lead-cook request at a time. For this initial contract, every specialty station type MAY support every plated cooked-meal recipe; future recipe-specific station classifications MUST be additive rather than silently disabling existing modded recipes.

#### Scenario: Support stations await professional-kitchen research
- **WHEN** a colony has not completed `ImmersiveChefs_ProfessionalKitchens`
- **THEN** none of the four specialty support stations is available to construct

#### Scenario: Cook mans a valid linked station
- **WHEN** a sauce station is operational and linked to the stove reserved by a lead cook with an open assistance request
- **THEN** one eligible assistant can reserve that station and begin its support job for that lead

#### Scenario: Adjacent but unlinked station gives no assistance
- **WHEN** a specialty station is physically near a stove but is not linked to that stove by the linked-building mechanic
- **THEN** it is not offered for the stove's assistance request and contributes no speed or quality

#### Scenario: Station cannot serve two lead jobs
- **WHEN** an assistant and support station are already claimed by one active lead-cook request
- **THEN** another lead-cook request cannot claim either of them until the first claim is released

### Requirement: Only simultaneous active work contributes
An assistant MAY wait or perform its manning animation after reaching a claimed support station, but the mod SHALL count assistance only during simulation ticks in which the lead is performing the recipe's actual cooking work and the assistant is simultaneously performing work at the linked station. Hauling, walking, waiting, gathering, suspended bills, pauses, and interruptions by either pawn MUST contribute neither cooking speed nor culinary quality. Contributions SHALL be accumulated per tick so partial assistance is retained accurately.

#### Scenario: Early arrival waits without granting a bonus
- **WHEN** an assistant reaches and mans a linked station while the lead is still hauling ingredients
- **THEN** no cooking-speed progress or assistant-quality contribution is recorded until the lead starts the actual cooking toil

#### Scenario: Simultaneous work accumulates assistance
- **WHEN** the lead is actively cooking and a claimed assistant is actively working at an operational linked station during the same ticks
- **THEN** those ticks add the assistant's skill-scaled speed bonus to bill progress
- **THEN** those ticks are included in the meal's accumulated assistant-quality input

#### Scenario: Lead interruption pauses contribution
- **WHEN** the lead stops the cooking toil to wait, recover from an interruption, or leave the primary building while the assistant remains at the station
- **THEN** the assistant contributes nothing for those non-cooking ticks

### Requirement: Assistant skill and time determine the contribution
For each simultaneous work tick, the mod SHALL normalize the assistant's current Cooking skill as `clamp(skill / 20, 0, 1)`. That assistant's speed bonus SHALL be `0.15 * normalized skill * AssistantEffectScale`; the bonuses of active assistants SHALL be summed and clamped to the configured system maximum of `1.80` (+180%). The `1.80` bound is the natural maximum produced by four skill-20 assistants at the maximum effect scale of `3.0` and MUST hold even if malformed external state exposes more contributors. The final meal's normalized assistant-quality input SHALL be `100 * clamp(AssistantEffectScale * sum(normalized skill * simultaneous ticks) / (4 * lead actual-cooking ticks), 0, 1)`. Ticks before actual cooking and ticks after the recipe completes MUST NOT enter either side of either calculation. A zero-duration or cancelled cook SHALL produce no assistant-quality input.

#### Scenario: Expert assistant grants the full per-assistant speed bonus
- **WHEN** a Cooking-skill-20 assistant works simultaneously with the lead and `AssistantEffectScale` is `1.0`
- **THEN** that assistant adds 15% to cooking speed for those ticks

#### Scenario: Two assistants contribute independently
- **WHEN** skill-20 and skill-10 assistants both work simultaneously with the lead for the same cooking tick at effect scale `1.0`
- **THEN** their speed bonuses for that tick are 15% and 7.5%, respectively, and are summed
- **THEN** their normalized skills are accumulated separately for the assistant-quality input

#### Scenario: Full expert team supplies the maximum quality input
- **WHEN** four skill-20 assistants work during every actual-cooking tick and `AssistantEffectScale` is `1.0`
- **THEN** the meal receives an assistant-quality input of `100`

#### Scenario: Effect scale changes speed as well as quality
- **WHEN** one skill-20 assistant works simultaneously for a tick and `AssistantEffectScale` is `2.0`
- **THEN** that assistant adds 30% cooking speed for that tick
- **THEN** four skill-20 assistants at effect scale `3.0` cannot exceed the +180% system maximum

### Requirement: Assistant selection is safe and non-blocking
An automatically selected assistant MUST have Cooking work enabled, be capable of the required work, and be able to reach and reserve an eligible linked station. Drafted, downed, mentally broken, forbidden, or otherwise unavailable pawns MUST NOT be selected. The scheduler MUST NOT interrupt a player-forced job or a higher-priority emergency job. A shortage of assistants or stations MUST NOT block the lead cook, reserve the bill indefinitely, or penalize the finished meal beyond receiving less or no assistance.

#### Scenario: No available assistant does not block the meal
- **WHEN** a lead starts and completes a supported meal job but no eligible assistant or linked station is available
- **THEN** the lead continues normally and the meal records zero assistant contribution

#### Scenario: Forced job is not preempted
- **WHEN** the only otherwise eligible cook is performing a player-forced job
- **THEN** the assistance scheduler leaves that pawn's job unchanged and the assistance request remains optional

### Requirement: Claims and support jobs follow the lead job lifecycle
The mod SHALL release assistance requests, pawn claims, and station reservations when the lead completes, cancels, fails, suspends, or loses the cooking job, or when the primary building or bill becomes invalid. If an assistant, support station, or route becomes invalid, its contribution SHALL stop immediately and its claim SHALL be released without cancelling the lead. Drafting or assigning urgent work to an assistant MUST be able to preempt the support job safely.

#### Scenario: Lead cancellation releases the team
- **WHEN** the lead's meal job is cancelled after assistants have claimed linked stations
- **THEN** all support jobs for that request end and their pawn and station reservations are released

#### Scenario: One assistant is interrupted
- **WHEN** an assistant is drafted while the lead continues cooking with other assistants
- **THEN** the drafted pawn's contribution stops and that station claim is released
- **THEN** the lead and remaining assistants continue without losing contribution already accumulated

### Requirement: Cooperative-cooking settings are bounded and live
The mod SHALL expose `AutoCallAssistants` (default `On`), `MaximumAssistants` (default `4`, integer range `0`–`4`), and `AssistantEffectScale` (default `1.0`, range `0.0`–`3.0`). `MaximumAssistants` SHALL cap simultaneously claimed assistants even when more linked stations exist. A maximum of zero or disabled automatic calling SHALL prevent new automatic support jobs. Scalar setting changes SHALL affect newly evaluated work ticks and requests without requiring a restart, while preserving contribution already recorded on active jobs.

#### Scenario: Configured maximum limits claims
- **WHEN** six eligible cooks and linked stations exist but `MaximumAssistants` is `2`
- **THEN** no more than two assistants are claimed for the lead job at the same time

#### Scenario: Automatic calling is disabled
- **WHEN** `AutoCallAssistants` is `Off` as a lead starts a supported bill
- **THEN** no automatic support jobs are issued and the lead can complete the meal unassisted

#### Scenario: Effect scale changes during cooking
- **WHEN** `AssistantEffectScale` changes while a lead and assistant are already working
- **THEN** contribution recorded before the change remains unchanged and later simultaneous ticks use the new scale
