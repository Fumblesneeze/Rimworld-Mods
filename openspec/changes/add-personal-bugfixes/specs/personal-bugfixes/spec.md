## ADDED Requirements

**Owning mod:** Personal Bugfixes (`fumblesneeze.personalbugfixes`) at `mods/PersonalBugfixes`.

### Requirement: Personal repair lifecycle
The mod SHALL require Harmony, remain absent-safe for optional target mods, carry no Workshop publication identity, and evaluate each registered fix independently after startup initialization. Every fix SHALL have its own Harmony owner and Markdown report. An activation SHALL require both a relevant local IL shape match and an assertion that the expected bug occurs in disposable inputs. Full-method IL, assembly version or MVID equality SHALL NOT be compatibility gates. All state mutations of a probe SHALL be confined to its fixture.

#### Scenario: Target absent
- **WHEN** an optional target package is absent
- **THEN** startup logs an informational absent decision and installs no patch or optional assembly dependency

#### Scenario: Structure changed
- **WHEN** required member signatures or relevant IL operations differ, are ambiguous, or cannot be safely probed
- **THEN** startup logs a warning explaining the unsupported shape and leaves the target unpatched

#### Scenario: Upstream repaired
- **WHEN** the shape remains supported but the disposable assertion passes without this fix
- **THEN** startup logs that the fix is not required and installs no patch

#### Scenario: Bug reproduced and repaired
- **WHEN** the supported disposable probe reproduces the expected failure
- **THEN** the mod installs only that fix, verifies the actual installed IL and repeats the behavioral assertion successfully before logging applied

#### Scenario: Failed activation
- **WHEN** patching, installed-IL verification or the post-patch behavioral assertion fails
- **THEN** the mod removes only its own patch and logs a warning with the fix ID, stage and cause while continuing to evaluate other fixes

#### Scenario: Probe cannot establish behavior
- **WHEN** the probe encounters another exception, unsupported external call, budget exhaustion or pre-existing foreign target patch
- **THEN** the decision is an actionable warning and no fix is installed

### Requirement: Exploration discovery-list repair
For optional Exploration Mode `thelastbulletbender.rwexploration`, the fix SHALL target the supported learnedFeatures indexed read in VisibilityManager.UpdateGraphics. Missing positions SHALL be appended as false; all existing flags SHALL preserve order and value. The original loop, visibility threshold, set_Item, rendering dirty calls and native pawn/job paths SHALL remain active. The fix SHALL add no research, items, recipes, cost/reward, eligibility bypass or whole-world reveal. Biotech and Toddlers SHALL NOT become dependencies of the repair mod.

#### Scenario: New feature exceeds discovery list
- **WHEN** the native world refresh reads an index beyond a non-null discovery list
- **THEN** missing flags are initialized to false and the refresh proceeds without the reported bounds exception

#### Scenario: Existing discoveries
- **WHEN** the list already contains known and unknown features
- **THEN** their flags are unchanged by capacity repair and subsequent normal exploration logic retains authority to discover features

#### Scenario: Ordinary crib transfer
- **WHEN** a carer chooses the native baby safety command with a short discovery list
- **THEN** the pawn completes carrying and tucking the baby into its crib without the Exploration exception, and the child is visibly resting there

### Requirement: Evidence and author reports
Each fix SHALL include a Markdown document describing symptoms, package constellation, required state, reproduction, offending code, proposed code, detection limits and verification status. Assertions, source snippets and synthetic fixtures SHALL be distinguished from observed live behavior. Diagnostics SHALL identify the fix, target, stage, decision and relevant reason at startup without per-tick spam.

#### Scenario: Sharing an upstream report
- **WHEN** an author reads the first fix's report
- **THEN** they can reproduce the short-list condition and inspect inline offending and proposed code without receiving a private save, credentials or unrelated mod data

#### Scenario: User is still playing
- **WHEN** host work is complete but the user has not stopped playing
- **THEN** no game is launched or stopped, live acceptance tasks remain unchecked, and verification waits
