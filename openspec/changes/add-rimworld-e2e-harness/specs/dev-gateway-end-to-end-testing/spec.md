## ADDED Requirements

**Owning mod:** RimWorld Dev Gateway (`fumblesneeze.rimworlddevgateway`) at `mods/RimWorldDevGateway`.

### Requirement: E2E tests are separate attributed runtime fixtures
The repository SHALL provide a separately packaged `RimWorldDevGateway.EndToEndTesting` contract. Every E2E test SHALL be a concrete attributed test type with a globally stable test ID, one owning package ID, and the complete ordered active package sequence required by that test, excluding only the Gateway package which the host appends last. A sequence without Harmony SHALL start with Core. When `brrainz.harmony` is present, it SHALL be the first package and `ludeon.rimworld` SHALL be second, matching Harmony's installed `loadBefore` contract. A non-Gateway owner MUST appear in that declared sequence. A Gateway-owned infrastructure test MUST declare `fumblesneeze.rimworlddevgateway` as owner, MUST rely on the one implicit final Gateway package, and MUST NOT list Gateway in the sequence. E2E projects and assemblies MUST remain outside ordinary NUnit/VSTest registration, product `Assemblies` directories, and release packages.

#### Scenario: A product declares an E2E workflow
- **WHEN** a test type implements the E2E contract and declares Harmony, Core, its owning product, and an optional integration in exact order
- **THEN** host discovery reports that stable test under exactly that non-Gateway mod group
- **THEN** the product's ordinary release artifact contains neither the E2E assembly nor a Gateway reference

#### Scenario: A test declaration is ambiguous
- **WHEN** a test ID is duplicated, a non-Gateway owner is absent from its package sequence, an owner is neither present nor the exact implicit Gateway owner, Gateway is listed explicitly, or its package sequence is empty, duplicated, missing, or unresolvable
- **THEN** discovery fails before launching RimWorld with an actionable validation error

#### Scenario: Harmony is placed after Core
- **WHEN** a test declares `brrainz.harmony` anywhere other than first or declares Core before Harmony
- **THEN** discovery rejects the declaration before build, staging, or launch

#### Scenario: Gateway owns an infrastructure self-test
- **WHEN** a Gateway-owned test declares Core as its exact non-Gateway sequence and `fumblesneeze.rimworlddevgateway` as owner
- **THEN** discovery accepts the owner through the implicit final Gateway entry and the launched active list is exactly Core followed by Gateway

### Requirement: The host launches one isolated process per exact mod group
The host runner SHALL discover all selected E2E tests before launch, group them by ordinal package sequence, and start exactly one fresh isolated quickstart RimWorld process for each distinct group. The active list in that process SHALL equal the declared sequence followed by `fumblesneeze.rimworlddevgateway`; no downloaded-but-undeclared package may participate. Groups and tests SHALL execute in deterministic order and a process SHALL never be reused for another group.

The runner SHALL accept one or more exact stable test-ID filters independently from its exact mod-group filter. It SHALL validate every requested ID against the complete discovery plan before mutation, launch only groups containing a selected test, and pass each group's exact selected IDs through a restart-bound Gateway startup selection. Runtime discovery SHALL still validate the complete staged bundle and active package order, but SHALL admit and execute only those exact IDs. An unknown, duplicated, invalid, mismatched, unadmitted, or additionally executed ID SHALL fail closed. A run without test-ID filters retains the complete-group behavior for deliberate regression and release verification.

#### Scenario: One current scenario is selected from a mature group
- **WHEN** the operator supplies the stable ID of one E2E test whose exact mod group contains several already-accepted tests
- **THEN** the host launches that group once and the Gateway admits, executes, reports, and cleans exactly the selected test
- **THEN** no other test in that group is arranged or executed

#### Scenario: A requested test ID is not admitted
- **WHEN** a selected stable ID is unknown at host planning or absent after runtime bundle and active-mod validation
- **THEN** the run fails before test arrangement and never substitutes a same-group or similarly named test

#### Scenario: Three tests share two mod combinations
- **WHEN** two tests declare the same Harmony/Core/product sequence and a third also declares one optional mod
- **THEN** the runner starts two processes
- **THEN** the first matching process runs the two same-group tests sequentially and the second runs the optional-mod test

### Requirement: A selected E2E run can start in one exact RimWorld language
The host runner SHALL accept one optional canonical RimWorld language-folder name, validate it as a bounded leaf token, pass it to the isolated launcher, and write it only to that process's disposable `Prefs.xml` as `langFolderName`. Dry-run, aggregate, and child smoke evidence SHALL record the requested language. Omitting the option SHALL retain the isolated launcher's English default. The runner SHALL bind a selected localization test to the exact requested active language so an unavailable locale cannot silently fall back to English. The runner SHALL never edit or derive the language from the user's normal preferences, and language selection SHALL NOT restore, focus, resize, or maximize the game window.

When an exact requested non-English language is not installed in Core, the development-only Gateway package MAY provide metadata-only `LanguageInfo.xml` for that canonical folder. The isolated launcher MAY publish that exact metadata into Core's language-discovery directory only as a hash-bound lease after Gateway deployment and before process launch. It SHALL acquire one destination-derived cross-process ownership primitive with a bounded wait, re-plan provider presence while holding it, retain it through owned-process exit and exact cleanup, and release it afterward. It SHALL preserve an already installed provider, record source/target hashes and ownership, remove only its exact created file after the owned process is confirmed stopped, remove the directory only when it created it and it is empty, and fail the run if safe cleanup cannot be verified. The provider SHALL contain no product, Gateway, or Core translated strings and SHALL NOT be shipped by Immersive Chefs.

#### Scenario: One localization test is repeated across languages
- **WHEN** an operator invokes the same exact stable E2E test separately with `English`, `German`, `Spanish`, `French`, `ChineseSimplified`, and `Russian`
- **THEN** every invocation starts one fresh minimized process whose disposable preferences request exactly that language
- **THEN** each result records the requested language and the user's normal preferences hash remains unchanged

#### Scenario: A language token is unsafe
- **WHEN** the supplied value is empty, contains a path separator, traversal, whitespace normalization, or exceeds the documented bound
- **THEN** the runner exits as invalid usage before deployment, staging, process launch, or normal-configuration mutation

#### Scenario: A requested locale is absent from Core
- **WHEN** an exact supported product locale is requested but Core has no matching language metadata
- **THEN** the launcher leases the Gateway's metadata-only provider into the exact Core language folder, records its hashes, and starts the minimized isolated process in that locale
- **THEN** after the exact owned process exits, the launcher removes only that hash-identical leased file and any empty directory it created and records verified cleanup

#### Scenario: Core already provides the requested locale
- **WHEN** the exact language metadata already exists in Core
- **THEN** the launcher does not overwrite, copy, claim, or later remove it

#### Scenario: Locale-provider ownership changes after planning
- **WHEN** an absent provider appears or an installed provider disappears between planning and publication
- **THEN** the launcher fails closed without borrowing, replacing, or recreating that path

#### Scenario: Locale-provider publication fails after file creation
- **WHEN** exclusive copy, hashing, equality validation, or evidence-record creation fails after the launcher creates its target file
- **THEN** the same publication scope removes its exact partial file and any empty directory it created before returning the failure

#### Scenario: Runtime order differs from discovery
- **WHEN** the actual loaded package set is missing, adds, or reorders any declared package
- **THEN** the Gateway loads no mismatched E2E test and the group fails before test arrangement

#### Scenario: A group has several additional mods and a long descriptive identity
- **WHEN** the host launches the exact group through a child PowerShell process
- **THEN** it passes the ordered additional package IDs through one UTF-8 file rather than repeating an array parameter on the command line
- **THEN** the runtime save-data branch uses a short group-independent name while the descriptive report retains the complete group identity
- **THEN** XML-invalid terminal control characters are replaced in JUnit projection without rewriting the retained stderr artifact

### Requirement: E2E loading is explicit and dynamic
The Gateway SHALL discover, validate, byte-load, and execute marker-owned E2E bundles below active mods only when the exact E2E startup flag is present. RimWorld SHALL never build test source. Bundle publication and cleanup SHALL be atomic and limited to exact owned staging directories, and every loaded assembly SHALL match the host-retained identity, hash, owner, and metadata-derived test list. The host SHALL serialize the complete backup, commit, rollback, and cleanup transaction for one canonical destination across processes. It SHALL retry only short-lived filesystem move failures within a bounded window, preserve the previous owned stage through a failed replacement, and report rollback as incomplete rather than silently succeeding when restoration is obstructed.

#### Scenario: Gateway starts normally
- **WHEN** the Gateway is loaded without the E2E startup flag
- **THEN** it neither enumerates E2E manifests nor loads an E2E assembly nor clears the playable map

#### Scenario: A staged bundle drifts after discovery
- **WHEN** a staged assembly's bytes, identity, attribute list, or owner differ from the host descriptor
- **THEN** the runtime rejects that bundle without executing any of its tests

#### Scenario: Concurrent or transiently locked publication
- **WHEN** two host processes target the same canonical owner/version stage or a commit, backup, or rollback move encounters a short-lived file lock
- **THEN** only one complete transaction owns that destination at a time, the blocked move retries within its fixed bound, and cleanup for an older lease cannot remove the newer publication

### Requirement: E2E tests execute as bounded multi-frame workflows
The E2E contract SHALL separate main-thread fixture arrangement from an iterator of typed `act`, `wait`, and `observe` steps. Synchronous arrangement and iterator-side fixture maintenance SHALL use bounded candidate sets and fixed-cost setup operations; a map-wide sort/search, long-running generation loop, blocking delegate, or other setup that can cross Windows' hung-window interval SHALL instead be bounded or split across frames before it is admitted as an E2E fixture. The Gateway SHALL NOT expose a generic synchronous callback step as an action. The Gateway SHALL wait without capturing or clearing an isolation baseline until the quickstart game reports native player control, SHALL bound that wait by the test's declared deadline, SHALL advance the iterator without blocking frame rendering, SHALL execute Unity/Verse access only on the main thread, and SHALL enforce test-declared frame, game-tick, and wall-clock deadlines plus a host watchdog. Supported action steps SHALL include native gizmos selected from exact Thing owners and/or exact architect category Def names, exact float-menu orders, typed native settlement-trade, trade-dialog, registered exact-window confirmation, exact selected-pawn inspect tabs, exact-Thing info cards, native in-play Escape-menu open/close, incident, and save/reload operations, pause/speed, selection, camera, reversible native screenshot-mode and shadow-rendering controls, and process-scoped input; supported observation steps SHALL include predicate assertions, full or object-bounded screenshots, and named checkpoints. The shared read-only gizmo catalog SHALL preserve the selected descriptor's observed toggle state and exact hotkey Def name when available, while retaining the original constructor contract for already-built E2E bundles.

A typed Escape-menu action SHALL require a player-controlled playable game and accept only open or close intent. Open SHALL invoke RimWorld's exact `MainTabsRoot.SetCurrentTab(MainButtonDefOf.Menu, playSound: false)` path and verify that exact tab became current. Close SHALL require that exact menu tab to be current, invoke `MainTabsRoot.EscapeCurrentTab(playSound: false)`, and verify it is no longer current. The action SHALL fail closed on unavailable play UI or mismatched close state and SHALL NOT synthesize input, focus, restore, resize, move, or maximize RimWorld.

#### Scenario: A minimized Gateway workflow inspects its Escape-menu indicator
- **WHEN** a Gateway-owned E2E test captures ordinary map play, opens the exact native in-play Escape menu through the typed action, captures it, then closes it through the typed action and captures ordinary play again
- **THEN** the native menu visibly opens and closes while the isolated process remains minimized and uses no desktop input
- **THEN** the Gateway warning and test-status overlay are visible only in the middle Escape-menu frame

#### Scenario: UI-free evidence is captured without desktop input
- **WHEN** a test enables the typed screenshot-mode action before a rendered evidence capture
- **THEN** the Gateway changes only RimWorld's native screenshot-mode state on the Unity thread, normal interface chrome is filtered from the captured frame, no process input or desktop focus is used, and guaranteed cleanup restores the prior state even if a later step fails

A typed shadow-rendering action MAY prepare exact-camera supporting raster measurements by changing only RimWorld's `DebugViewSettings.drawShadows` state through its native map-mesh invalidation callback on the Unity thread. The step SHALL be recorded as `act`, SHALL require a playable current map, SHALL NOT use process input or desktop focus, and SHALL be restored in guaranteed test cleanup. A shadow-free measurement is supporting segmentation evidence only; public visual acceptance SHALL still inspect a later capture with ordinary shadows restored.

#### Scenario: A visual test separates structure from native sun shadows
- **WHEN** a test disables shadow rendering through the typed action, captures same-camera structural reference stages, restores shadows through the typed action, and captures the final public candidate
- **THEN** both toggles are retained as explicit actions, RimWorld's native shadow map meshes are invalidated without foreground input, cleanup restores the original setting, and only the final shadowed capture is eligible as publication art

A typed pawn inspect-tab action SHALL accept one exact selected pawn runtime ID and one allowlisted semantic tab identity (`Gear`, `Needs`, or `Health`). It SHALL require that pawn to be the current sole selection, open RimWorld's exact current `InspectPaneUtility` tab type, and verify the resulting open tab. A typed Thing info-card action SHALL accept one exact live Thing runtime ID plus open/close intent; opening SHALL resolve that exact Thing across spawned Things, pawn inventories, and the one-hop direct contents of spawned holders, invoke the native `Dialog_InfoCard(Thing)` path, and require exactly one card bound to the same live Thing, while closing SHALL remove only that exact card through the native window lifecycle. Exact-Thing resolution SHALL aggregate reference-distinct matches across all supported scopes, SHALL NOT recursively invoke untrusted `GetChildHolders`, and SHALL scan no more unique candidates or holders than the Gateway's existing maximum native interaction-target budget. Exhaustion SHALL fail with stable `info_card_resolution_limit`; an exception while enumerating any supported scope SHALL fail with stable `info_card_resolution_incomplete` instead of silently accepting a partial search. A typed inspect-pane close action SHALL require one exact sole-selected Thing and the exact full runtime type name of the currently open inspect tab, invoke RimWorld's native `CloseOpenTab` plus the Inspect main-button toggle when its main pane remains active, and verify that neither the subtab nor its force-pausing main pane remains open. A typed window-cancel action SHALL require exactly one currently open window with the requested full runtime type, invoke its native `OnCancelKeyPressed` lifecycle, and verify that exact window is gone; zero or multiple matches SHALL fail without mutating any window. When Unity's current event is not already an Escape `KeyDown`, the adapter SHALL provide an isolated synthetic Escape `KeyDown` for that native lifecycle and SHALL restore the original event in guaranteed cleanup so `Event.Use()` neither warns on repaint/layout nor consumes unrelated input. Missing, stale, ambiguous, unsupported, or mismatched selection/window state SHALL fail closed. These actions SHALL run on the Unity main thread and SHALL NOT call process input, restore, focus, resize, move, or maximize the RimWorld window.

A typed mod-settings action SHALL accept one exact active package ID, resolve exactly one live `Verse.Mod` whose content package ID matches it ordinal-ignore-case, open RimWorld's native `Dialog_ModSettings(Mod)` window, and verify that exactly one open settings dialog remains bound to the same `Mod` instance. Missing or duplicate package matches, constructor/field shape drift, missing player control, or an already-open mismatched or duplicate settings dialog SHALL fail closed. The action SHALL NOT select a mod by translated name, invoke the settings renderer directly, or use process input, focus, restore, resize, move, or maximize.

A typed supporting hit-point fixture action MAY prepare damage-rendering catalogs without misclassifying direct state mutation as an observation or native player action. It SHALL accept one to 64 unique exact current-map Thing runtime IDs, each with a finite remaining-hit-point ratio strictly between zero and one; SHALL resolve every target exactly once and verify that every target uses hit points before mutating any target; and SHALL fail closed on a missing, ambiguous, duplicate, non-damageable, or out-of-range target. Its retained outcome SHALL label the action as direct supporting setup. This action is fixture evidence only and SHALL NOT satisfy native combat, damage, destruction, or player-workflow acceptance.

#### Scenario: A visual catalog prepares bounded damage grades honestly
- **WHEN** an E2E workflow applies declared damage ratios to exact native-built walls for a rendering comparison
- **THEN** the result records an `act` step explicitly labeled as direct supporting setup rather than an `observe` step
- **THEN** separate native player-workflow evidence remains required for actual combat damage behavior

#### Scenario: A minimized dining suite inspects native pawn and Thing UI
- **WHEN** an E2E workflow selects one pawn, opens its Gear or Health tab, opens the exact returned plate's info card, or closes an exact optional-mod inspect tab and captures the visible UI while the isolated process is minimized
- **THEN** the native inspect pane and exact info card visibly render the requested state
- **THEN** the Gateway uses no screen coordinate, keyboard event, process activation, restore, resize, or maximize operation

#### Scenario: A minimized localization suite opens the product settings page
- **WHEN** an E2E workflow requests the exact active product package and captures the resulting native settings dialog
- **THEN** the dialog is bound to that exact loaded `Mod` instance and visibly renders its translated settings labels
- **THEN** the Gateway uses no translated mod name, screen coordinate, keyboard event, process activation, restore, resize, or maximize operation

A typed gizmo `Place` or `Drag` step MAY declare one exact cardinal rotation. The shared contract SHALL permit that option only for a cell-shaped placement or line-shaped drag step, and the runtime SHALL project it to the Gateway's native place-designator interaction rather than mutate a resulting Thing. Other gizmo interaction kinds and rectangle drags SHALL reject the option during contract validation or native input validation.

A cell-shaped `Place` or line-shaped `Drag` overload MAY independently declare one exact Stuff Def name, with or without a rotation. The runtime SHALL project that material choice into the same native `Designator_Build` interaction before preflight and SHALL NOT mutate the designated or completed Thing afterward.

#### Scenario: An E2E test performs a rotated native placement
- **WHEN** a test selects one exact Architect place designator and applies it to one cell with `East`
- **THEN** the runtime passes the cardinal choice into the native placement interaction before preflight and designation
- **THEN** later waits, queries, and screenshots can observe the east-facing placed building

#### Scenario: An E2E test preserves directional line semantics
- **WHEN** a test selects one exact line-capable Architect designator and applies an ordered line with one cardinal rotation
- **THEN** the runtime passes both line endpoints and the cardinal choice into the native place-designator interaction before preflight and designation

A typed Architect-category action SHALL accept one exact loaded `DesignationCategoryDef` and an
open/close intent. Opening SHALL use RimWorld's native Architect main-button activation path, select
the exact category through the current Architect tab's own cached category object and inspected
category-click method, and require the requested tab/category to be active afterward. Closing SHALL
use the native current-tab escape path and require Architect to be closed afterward. Missing player
control, missing or duplicate category tabs, method-shape drift, or a rejected tab transition SHALL
fail closed. The action SHALL NOT restore, focus, resize, maximize, or use process input. A later
screenshot and read-only architect designator query SHALL prove player-visible menu readability and
the exact buildable identities.

#### Scenario: A minimized visual catalog opens the real Production menu
- **WHEN** a focused product E2E test opens the exact Production Architect category through the typed action
- **THEN** the current native build menu is visibly open while the isolated RimWorld process remains minimized
- **THEN** the exact category designator catalog contains the product buildings and a retained screenshot proves their labels/icons are readable
- **THEN** the typed close action leaves no Architect tab open without foreground input

A typed trade-dialog adjustment SHALL require exactly one open native `Dialog_Trade`, resolve exactly one current `Tradeable` containing the supplied exact Thing runtime ID, validate the requested nonzero bounded delta against that tradeable's native minimum and maximum, invoke its native nonpublic count setter, and invoke the current dialog's native count-changed refresh. Typed acceptance SHALL re-resolve exactly one current dialog and invoke the exact native Accept callback generated by that RimWorld build. Neither action SHALL restore, focus, resize, or otherwise mutate the native window; neither SHALL set private count fields directly, call `TradeDeal.ResolveTrade` itself, close the dialog independently, or silently fall back to pixel input. Missing, ambiguous, stale, out-of-range, or version-drifted members SHALL fail closed. A later wait/assertion and screenshot SHALL prove the native dialog and trade deal produced the expected observable result.

A typed optional-mod dialog confirmation SHALL expose only the expected exact open-window type through the host-safe test contract. The Gateway SHALL retain the private allowlisted adapter shape, including exact assembly identity, confirmation callback, and argument fields; the test bundle SHALL NOT supply callback or field names and an unregistered window SHALL have no reflective fallback. The adapter SHALL require exactly one matching open window, a void-returning callback, and assignable exact argument values, SHALL close the dialog before invoking the callback when that is the inspected native button order, and SHALL fail closed on any identity, cardinality, or shape drift. It SHALL neither restore, focus, resize, nor foreground RimWorld. A later wait/assertion and screenshot SHALL prove the ordinary dialog action produced the claimed player-visible result.

The shared host-safe contract SHALL expose a read-only native float-menu catalog service through the E2E context. A query SHALL return each current actor/target option's visible label, disabled state, and callback-sensitive stable identity without exposing or retaining its RimWorld callback. Tests SHALL invoke the chosen result only through the typed float-menu action step, and the Gateway SHALL re-query and require exactly one enabled stable-identity match before running the native callback.

A typed current-float-menu action SHALL accept one exact visible option label after a preceding native gizmo action has opened exactly one real `FloatMenu`. The Gateway SHALL retain the exact newly opened window as a single-use automation lease, re-read that same open window's current options, and require exactly one enabled ordinal label match. It SHALL then mirror `FloatMenuOption.DoGUI`'s native success order: when `tutorTag` is non-null, require `TutorSystem.AllowAction`; invoke `FloatMenuOption.Chosen` with the leased window's actual `givesColonistOrders`; notify the accepted tutorial event; and only then remove that same source menu through `WindowStack.TryRemove`. Tutorial rejection SHALL invoke neither the option callback, notification, nor close. A missing lease, replacement, duplicate, disabled, stale, rejected, or multiply open menu SHALL fail closed. The action SHALL NOT reproduce a product callback, synthesize an end state, or use process input, focus, restore, resize, move, or maximize.

When an exact native semantic gizmo callback opens a new `FloatMenu` while RimWorld is minimized, the Gateway SHALL disable mouse-distance vanishing only on that one newly opened menu and publish that exact instance as the pending automation lease. Zero or multiple newly opened menus SHALL publish no lease. A later current-menu action SHALL accept only the leased instance while it remains the sole open `FloatMenu`, consume the lease only after the native success lifecycle closes it, and clear any remaining lease during test isolation cleanup or before another semantic gizmo action. Releasing an open lease for success, failure, replacement, or cleanup SHALL restore that exact menu's prior mouse-distance behavior. Every pre-existing player window SHALL remain unchanged, and the product's native callback SHALL still create the menu.

#### Scenario: A native action gizmo opens a choice menu
- **WHEN** an E2E workflow invokes an exact enabled `Command_Action`, visibly captures the resulting four-option `FloatMenu`, and selects one exact enabled label through the typed current-menu action
- **THEN** the exact option's native callback runs once, the same real menu closes through its ordinary lifecycle, and the minimized process uses no desktop input
- **THEN** only the newly opened menu ignores off-window mouse distance while awaiting observation; unrelated existing windows retain their original behavior
- **THEN** a replaced menu, tutorial-rejected option, or stale lease fails without invoking a different callback

A typed native incident action SHALL require a playable current map and one exact loaded `IncidentDef` name. An optional faction load ID SHALL resolve to exactly one currently loaded faction without imposing positivity or player-faction assumptions. The Gateway SHALL construct RimWorld's current native storyteller parameters, mark the incident forced, apply the exact faction when supplied, and invoke the real `IncidentWorker.TryExecute`. A missing map, Def, or faction, a false native result, or an exception SHALL fail closed with bounded token-safe diagnostics. The action SHALL NOT expose an arbitrary callback, queue a guessed storyteller event, use translated labels, or substitute desktop input.

A typed native save-and-reload action SHALL accept only a canonical Windows leaf save name bounded to preserve room beneath the 255-code-unit component limit for RimWorld's extension and recovery suffixes. It SHALL reject whitespace normalization, trailing dot/space aliases, DOS device basenames, path separators, invalid filename characters, and an existing isolated save. From a player-controlled playable game with saving enabled, the Gateway SHALL call RimWorld's native save operation once, require the exact resulting save to exist and be nonempty, and then call RimWorld's native load operation once. The action SHALL complete only after `Current.Game` is a different instance, player control and a current map return, and no long event is active or queued. Native exceptions SHALL fail closed without tokens or arbitrary exception text. The attributed test SHALL register deferred cleanup for the exact isolated save, reacquire objects by stable game identity after replacement, and retain before/after screenshots or checkpoints; frame and wall-clock limits remain authoritative when loading resets game ticks.

#### Scenario: An E2E workflow crosses the native persistence boundary
- **WHEN** a focused product test observes a visible object, saves and reloads through the typed action, reacquires it by stable identity, and observes it again
- **THEN** the Gateway performs one native save and load without overwriting existing data and resumes the iterator only in the settled replacement game

#### Scenario: A requested save name aliases or already exists
- **WHEN** the requested leaf is noncanonical, reserved by Windows, over the documented component bound, or already exists in the isolated save folder
- **THEN** the action fails before loading and does not overwrite or reinterpret the target

#### Scenario: A pawn must finish a real job
- **WHEN** arrangement creates a drafted pawn and fixtures, an action step invokes the native Undraft command, and a wait step watches the ordinary job outcome
- **THEN** rendered frames and game ticks continue while the pawn's normal scheduler and job driver run
- **THEN** the test resumes at its observable assertions only after the predicate succeeds or its declared deadline fails

#### Scenario: Fixture arrangement searches for clear map cells
- **WHEN** a test needs a small fixed number of separated fixture locations on the disposable map
- **THEN** arrangement scans a bounded deterministic candidate set, returns control before the native window is treated as hung, and leaves multi-frame waiting to typed steps

#### Scenario: Quickstart has a map before it grants player control
- **WHEN** an E2E bundle is admitted while the generated map exists but `Game.PlayerHasControl` is still false
- **THEN** the runner leaves the map untouched and retries readiness on later frames without recording an isolation failure
- **THEN** readiness that remains false beyond the test deadline fails as `player_control_not_ready`, taints that process, and runs no unowned cleanup against an uncaptured baseline

#### Scenario: A fixture needs a just-in-time deterministic precondition
- **WHEN** iterator-side fixed-cost setup adjusts bounded test-only state after a persisted wait boundary and before native completion
- **THEN** no synthetic action is recorded and acceptance still requires a separate native player action plus visible observation
- **THEN** arbitrary synchronous callback steps remain unavailable because their wall-clock deadline cannot preempt a blocked Unity main thread

#### Scenario: Test code attempts no player action
- **WHEN** a fixture directly constructs the claimed end state but records no native action between its before and after observations
- **THEN** the result cannot satisfy the player-workflow acceptance classification even if its state assertion passes

#### Scenario: A test discovers a native ingestion option
- **WHEN** a dynamically loaded product test queries the current pawn and meal through the shared float-menu catalog
- **THEN** it can select one unambiguous enabled visible option and submit the returned stable identity without referencing Gateway implementation assemblies
- **THEN** a stale, disabled, missing, or ambiguous option fails closed without assigning a synthetic ingestion job

#### Scenario: A minimized orbital trade dialog is operated
- **WHEN** a test adjusts the exact trader-owned meal by one within its native transfer range and invokes the native Accept callback while the isolated RimWorld process remains minimized
- **THEN** the native trade deal records and executes that exact purchase while the Gateway neither restores nor foregrounds the window

#### Scenario: A minimized optional-mod confirmation dialog is operated
- **WHEN** a test opens Replimat's registered survival-batch dialog through its native terminal gizmo and confirms its default one-meal value
- **THEN** the exact native callback creates one packaged survival meal while the Gateway exposes no generic callback surface and neither restores nor foregrounds the process

#### Scenario: A caravan opens settlement trade without desktop input
- **WHEN** a test identifies one player caravan and one visited settlement by their exact live world-object IDs
- **THEN** the Gateway requires both exact objects at the same tile, obtains RimWorld's native `CaravanVisitUtility.TradeCommand`, requires its enabled action shape, and invokes that command to open exactly one settlement `Dialog_Trade`
- **THEN** missing, ambiguous, wrong-type, wrong-tile, nonvisited same-tile target, disabled, or changed command shapes fail closed without using translated labels, pixel input, focus, restore, or maximize operations
- **THEN** a negative E2E step MAY require one exact expected failure code, passes only when that code is observed, and retains both the expected and observed codes without converting any other failure or successful open into a passing result

#### Scenario: A native trader caravan incident exercises a compatibility boundary

- **WHEN** a test supplies exact `TraderCaravanArrival` and the exact load ID of one eligible non-player faction
- **THEN** the Gateway invokes RimWorld's native incident worker once with forced current-map parameters and that exact faction
- **THEN** the test waits for ordinary caravan entry and observes the resulting pawns and arrival letter, while missing, ambiguous, rejected, or version-drifted state fails closed without a generic callback or synthetic caravan

### Requirement: Arranged fixtures preserve native object invariants
Product-owned E2E fixtures SHALL keep engine-managed objects valid for every native subsystem that may revisit them later in the same process. In particular, generated humanlike Pawns SHALL retain a valid `Verse.NameTriple` with a stable requested short label; fixtures MUST NOT replace that name with the animal-style `Verse.NameSingle`. This invariant applies even when a Pawn is subsequently destroyed or removed from the disposable map, because vanilla world-pawn and relationship generation may retain or revisit it before process shutdown.

#### Scenario: Sequential tests generate humanlike Pawns
- **WHEN** an earlier test generates, labels, spawns, and cleans up one or more humanlike Pawns and a later test asks vanilla `PawnGenerator` for another colonist
- **THEN** every earlier humanlike fixture still has a valid native triple name and the later generation completes without a relation/name exception
- **THEN** animal fixtures MAY continue using `NameSingle`

### Requirement: The map is empty and verified between tests
After native player control becomes available, before the first test and in guaranteed cleanup after every test, the runner SHALL pause the game and remove every roof cell, including constructed roofs and overhead mountain, before it destroys any map content. It SHALL then remove all destroyable spawned map Things and Pawns, jobs, zones, designations, selections, active interactions, test-opened windows, and registered scenario-owned world objects. After removal has finished, the runner SHALL clear every current live message, visible or delayed letter, and active alert-readout entry, and SHALL verify those notification surfaces are empty before arranging the next test. Clearing SHALL use RimWorld's public removal path where one exists; any required private delayed-letter or alert collection shape SHALL stay exact and Gateway-owned and SHALL fail closed on version drift. Newly arranged scenario conditions MAY generate fresh alerts normally. Permanent non-destroyable map features such as steam geysers are part of the map environment, MUST NOT be destroyed, and MUST be excluded from the disposable-state emptiness check. The runner SHALL restore developer/god mode, speed, camera, and pressed input to the process baseline and verify both that no roof remains and that the disposable map and notification slate are empty before arranging the next test. Tests SHALL be able to register additional cleanup actions for mod-specific global state.

#### Scenario: A generated map contains unsupported natural and constructed roofs
- **WHEN** the runner prepares or cleans a test on a map containing constructed roofs or overhead mountain
- **THEN** every roof is removed before any wall, support, pawn, item, or other map Thing is destroyed
- **THEN** no roof-collapse damage, alert, or leaked death contaminates the next test

#### Scenario: Initial removal or a completed test leaves player notifications
- **WHEN** map removal produces messages, visible or delayed letters, or active alert-readout entries before the first test or between sequential tests
- **THEN** notification cleanup runs after the map-content phases and clears every current entry
- **THEN** the empty-baseline verification fails closed unless all three notification surfaces are empty
- **THEN** a later alert-sensitive scenario starts from a clean slate and can observe only alerts generated by its own arranged conditions

#### Scenario: A test fails after spawning fixtures
- **WHEN** an assertion throws while pawns, buildings, items, filth, zones, or jobs remain
- **THEN** cleanup still removes them and verifies the empty baseline before the next test starts

#### Scenario: Reset cannot prove isolation
- **WHEN** cleanup throws or any tracked/spawned fixture, interaction, or pressed input remains
- **THEN** the current test records an infrastructure failure, the process is tainted, and later tests in that group are skipped rather than run against contaminated state

#### Scenario: A quicktest map contains a permanent feature
- **WHEN** the generated map contains a non-destroyable steam geyser or equivalent permanent map Thing
- **THEN** reset preserves that feature while still removing and verifying the absence of every destroyable disposable fixture

### Requirement: Failures are isolated and diagnostically complete
An assertion, test exception, unsupported native action, stale handle, or ordinary test timeout SHALL fail only the current test when map reset remains trustworthy. Each failure SHALL retain the current step, bounded causal exception, live job/target and selection checkpoint when available, final screenshot, log cursor page, and cleanup result. Before any failure snapshot becomes persistable, the runtime SHALL redact the exact live session credential from every failure field and cap the failure message to the same 8 KiB UTF-8 diagnostic-message budget used by Gateway log entries; identity and stack fields SHALL remain separately bounded. A test failure MUST NOT be converted into a process success merely because later cleanup passed.

#### Scenario: One test fails and cleanup succeeds
- **WHEN** the first test in a group fails an observable assertion and the reset returns to a verified empty map
- **THEN** the second test still runs
- **THEN** the aggregate contains one failed and one independently evaluated result

#### Scenario: RimWorld exits during a test
- **WHEN** the exact process exits before terminal persistence
- **THEN** the host synthesizes explicit aborted results for the active and unrun tests and retains the flushed process log and cleanup status

#### Scenario: A mod returns hostile native rejection text
- **WHEN** a native player action returns an oversized rejection reason containing the live bearer credential
- **THEN** the persisted failure retains a useful deterministic prefix, replaces the credential, fits the 8 KiB UTF-8 message budget, and never invokes arbitrary exception formatting

### Requirement: Results are durable and automation-friendly
The runner SHALL emit one aggregate JSON result, one JUnit XML result, and table or JSON console output. Results SHALL identify the game version, exact PID/start identity, complete ordered mods, product/Gateway/E2E assembly identities and hashes, test and step timings, game ticks, assertions, screenshots, logs, cleanup, and before/after normal configuration hashes. Exit code `0` SHALL mean every selected test and cleanup passed, `1` SHALL mean a test/runtime/infrastructure failure, and `2` SHALL mean invalid usage or discovery/selection failure.

#### Scenario: A CI run succeeds
- **WHEN** every selected group and test passes and every process, credential, stage, input, and configuration cleanup succeeds
- **THEN** the command exits `0` and its JSON/JUnit artifacts contain the same deterministic terminal results

#### Scenario: No test matches a filter
- **WHEN** a caller supplies a test or group filter that selects zero tests
- **THEN** the command exits `2`, launches no process, and reports the unmatched filter

### Requirement: Existing gameplay scenarios migrate without weakening evidence
Manual Gateway scenarios SHALL be migrated into product-owned E2E tests by player-visible behavior. A descriptor MAY remain for interactive exploration, but duplicate automated orchestration SHALL be deleted after its E2E replacement proves the same or stronger native action and observable result on the reviewed build. The migration inventory SHALL track every existing scenario as pending, converted, interactive-only, or retired.

#### Scenario: Adverse meal outcome becomes the tracer test
- **WHEN** the first Immersive Chefs E2E test arranges an awful frozen meal with dirty cookware, plate, and cutlery and then invokes native undrafting and ordinary game time
- **THEN** the real ingestion job consumes the meal, returns the ware dirty, applies the visible awful/frozen/dirty dining thoughts, and produces food poisoning under a deterministic configured risk
- **THEN** the result retains before/action/after screenshots and the next test begins from an empty map

### Requirement: The repository workflow treats E2E as a distinct acceptance tier
`AGENTS.md`, the repo-local RimWorld development skill, README, and testing documentation SHALL explain when to use host tests, startup integration tests, E2E tests, interactive acceptance, and performance tests. A gameplay change with a repeatable multi-frame workflow SHALL add or update an E2E test, while the acting agent SHALL still personally inspect the reviewed build's native action and observable screenshots before accepting new behavior.

#### Scenario: A future agent changes a pawn job patch
- **WHEN** the change can be exercised through a bounded playable-map workflow
- **THEN** the workflow requires focused host coverage, a matching E2E regression, reviewed-build E2E execution, and personal inspection of retained behavior evidence

#### Scenario: RimWorld or a supported third-party mod updates
- **WHEN** a game or mod update may have changed internal methods, Defs, jobs, UI actions, or Harmony targets without an obvious load error
- **THEN** the documented maintenance workflow runs the relevant exact E2E groups in quick succession and uses their aggregate plus personally inspected behavior evidence to locate regressions
