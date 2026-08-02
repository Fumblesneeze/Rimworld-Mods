## ADDED Requirements

**Mod scope:** Immersive Chefs (`fumblesneeze.immersivechefs`) at `mods/ImmersiveChefs`.

### Requirement: Framework-backed external tests
The repository SHALL provide an NUnit test project based on the validated RimWorld testing framework and SHALL run it in a test-runner process without starting `RimWorldWin64.exe`.

#### Scenario: Run fast tests
- **WHEN** a contributor invokes the documented test command while RimWorld is not running
- **THEN** the test runner executes public Immersive Chefs behavior against the configured RimWorld assemblies and no RimWorld game process is created

### Requirement: Vertical TDD baseline
The optional integration catalog SHALL be developed as observable vertical slices, with one failing public-behavior test followed by the minimum implementation and a green test before the next behavior is added.

#### Scenario: Audit baseline development
- **WHEN** a contributor reviews the baseline task record and tests
- **THEN** the record distinguishes RED, GREEN, and refactor steps and the tests assert public catalog results rather than private implementation calls

### Requirement: Dynamic game-version assertion
The isolated verification scripts SHALL derive the expected RimWorld version from the configured installation's `Version.txt` rather than pinning a transient build number in source.

#### Scenario: RimWorld receives a point update
- **WHEN** `Version.txt` changes while the major supported version remains 1.6
- **THEN** each applicable smoke command validates and records the new installed value without a source edit

### Requirement: Isolated minimal mod list
In-game verification SHALL use a dedicated `-savedatafolder` containing a minimal `ModsConfig.xml` with only Harmony, RimWorld Core, and Immersive Chefs active; it SHALL NOT modify the user's normal RimWorld configuration or saves.

#### Scenario: Prepare an in-game smoke run
- **WHEN** the verification script starts RimWorld
- **THEN** it writes isolated test configuration beneath ignored build artifacts and launches the game with active packages `brrainz.harmony`, `ludeon.rimworld`, and `fumblesneeze.immersivechefs` in that order

#### Scenario: Verification fails partway through
- **WHEN** launch, UI automation, or log validation fails
- **THEN** the user's normal `ModsConfig.xml` and save-data folder remain byte-for-byte untouched and the script reports the isolated artifact paths for diagnosis

### Requirement: Desktop and log smoke evidence
The in-game smoke check SHALL connect FlaUI to the launched RimWorld PID, verify its visible native main-window handle/title, capture a FlaUI screenshot, and confirm the Immersive Chefs startup marker in the isolated Player log. Empty FlaUI element/window trees SHALL be accepted because RimWorld renders its menu on a Unity canvas.

#### Scenario: Baseline loads successfully
- **WHEN** the minimal-mod-list game reaches the main menu
- **THEN** FlaUI reports a connection to the exact launched PID, the process exposes a titled native window, a screenshot is saved, the log contains the startup marker, and no Immersive Chefs load error is present

### Requirement: Owned-process cleanup
Verification automation SHALL close only the RimWorld process it launched, disconnect FlaUI, and stop the FlaUI service it started.

#### Scenario: Smoke verification completes
- **WHEN** evidence collection succeeds or fails
- **THEN** the recorded RimWorld process is no longer running and the FlaUI client and service are disconnected without affecting unrelated processes

### Requirement: Reproducible verification entry points
The repository SHALL expose documented commands for restore, build, test, deploy, isolated in-game launch, evidence inspection, and strict OpenSpec validation.

#### Scenario: New contributor verifies the baseline
- **WHEN** a contributor follows the repository instructions on a machine with RimWorld 1.6 and Harmony
- **THEN** each verification tier can be run independently and produces a clear pass or actionable failure

### Requirement: Repo-local RimWorld development skill
After the workflow is proven, the repository SHALL contain a validated `.codex/skills/rimworld-mod-development` skill that routes future work through OpenSpec and TDD and documents the verified C# build, Harmony patching, compatibility, Def/XML, non-destructive patch XML, testing, and FlaUI smoke procedures.

#### Scenario: Future Codex task changes Immersive Chefs
- **WHEN** a task involves RimWorld C# code, Harmony, Defs, XML patches, optional-mod compatibility, tests, or game verification in this repository
- **THEN** the skill supplies concise workflow instructions and points to focused reference files and tested repository scripts

#### Scenario: Validate the local skill package
- **WHEN** the skill-creator validation command runs against `.codex/skills/rimworld-mod-development`
- **THEN** its name, trigger description, frontmatter, agent metadata, and referenced resources pass validation
