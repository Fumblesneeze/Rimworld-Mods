# Power network inspection

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin replacement **S102 More power info**. Observable contract for future implementation only.

## ADDED Requirements

### Requirement: MPI01 — Inspect the actual connected network

The feature SHALL show current production, current consumption and their signed balance in watts,
plus usable stored energy in watt-days, for the selected object's connected network. Consumption
means equipment currently drawing power, not every installed device's potential demand. It MUST
retain device-specific, disconnected and unrelated inspection information and SHALL remain within
the ordinary contextual inspection surface.

#### Scenario: Select equipment on a running circuit
- **WHEN** the player selects a connected device on a circuit producing 200 W and currently consuming 500 W
- **THEN** its network information shows production 200 W, consumption 500 W and balance −300 W, alongside its own device information and the circuit's usable reserve

#### Scenario: Different circuits and powered-off loads
- **WHEN** another circuit exists and a device on the selected circuit is switched off or shut down for lack of power
- **THEN** other circuits are excluded, and the stopped device's potential demand is not presented as current consumption; the readout identifies its current-load scope

#### Scenario: Preserve existing inspection information
- **WHEN** a battery, generator or supported externally supplied device already has capacity, efficiency, fuel, fault or other inspection information
- **THEN** those details and unrelated added text remain visible, with one owned network contribution and no whole-pane replacement

#### Scenario: Disconnected or uninspectable selection
- **WHEN** the selected object has no connected circuit, is packed or removed, or no longer exposes an ordinary power inspection surface
- **THEN** no previous circuit's totals remain; native disconnected or unavailable inspection behavior is preserved

### Requirement: MPI02 — Qualify current-rate reserve estimates

The feature SHALL present the current-rate estimate as remaining stored-energy depletion at the
observed generation, consumption and admitted battery behavior. It MUST account for applicable
self-discharge and actual charging efficiency, and MUST identify that weather, fuel exhaustion,
new work, future switches and native load shedding can change the result. It SHALL NOT present a
predicted reserve duration as a guarantee that every device stays powered for that duration.

#### Scenario: Reserve is draining under a steady deficit
- **WHEN** the circuit has 300 Wd usable reserve, production 200 W, current consumption 500 W and one ordinary battery losing 5 W
- **THEN** its current-rate estimate is approximately 23.61 game hours, before display rounding, and identifies that estimate as dependent on unchanged rates

#### Scenario: Small surplus does not cover battery losses
- **WHEN** a partially charged ordinary battery receives a circuit surplus of 6 W, stores half that input and loses 5 W
- **THEN** the current-rate estimate reflects the remaining 2 W loss, rather than calling the reserve non-depleting merely because production exceeds consumption

#### Scenario: Actual efficiency and non-depleting reserve
- **WHEN** an admitted partially charged battery stores all of a 6 W surplus and loses 5 W under otherwise unchanged conditions
- **THEN** the readout reports no depletion at current rates, rather than applying a universal 50% efficiency or promising everlasting power

#### Scenario: Full reserve rejects momentary charging
- **WHEN** all usable batteries are full and generation exceeds current consumption
- **THEN** the current estimate reports Full — at current output instead of extrapolating one moment of rejected charging into an exhaustion deadline; the separate storage-only estimate remains available when supported

#### Scenario: No usable reserve with adequate generation
- **WHEN** usable stored energy is zero but current generation covers consumption
- **THEN** the display reports no usable reserve while retaining the positive or balanced production information; it does not claim that the circuit is currently unpowered

#### Scenario: Capacity limits prevent a small transfer
- **WHEN** full and partially filled batteries share a circuit whose current charging rules discard a small surplus before it reaches the partial batteries
- **THEN** the estimate does not credit that discarded input as stored energy or claim non-depletion because of an idealized transfer; normal charging behavior is unchanged

#### Scenario: Heterogeneous or unsupported battery behavior
- **WHEN** battery capacities, efficiencies or losses differ, or a provider's storage behavior cannot be established
- **THEN** admitted differences are reflected; an unsupported estimate is visibly unavailable with a concise reason, while independently trustworthy totals remain, and no numeric duration or infinity is invented

### Requirement: MPI03 — Distinguish the generation-off comparison

The feature SHALL separately show how long the currently usable reserve would last if generation
were removed while the same current loads and admitted battery losses continued. This comparison
MUST remain hypothetical: reading it does not turn off generators, activate stopped equipment or
change battery charge. Both estimates SHALL handle empty reserves, zero losses and long durations
without negative, invalid or wrapped values.

#### Scenario: Compare reserve-only operation
- **WHEN** the circuit has 300 Wd usable reserve, 500 W current consumption and one ordinary battery losing 5 W
- **THEN** the storage-only estimate is approximately 14.26 game hours before display rounding, regardless of the currently operating generator's contribution

#### Scenario: An unused battery still loses energy
- **WHEN** a charged ordinary battery supplies no active consumers
- **THEN** its storage-only estimate still includes its 5 W self-discharge; 600 Wd corresponds to approximately 120 game days, rather than infinite storage

#### Scenario: No reserve or no admitted loss
- **WHEN** there is no usable reserve, or a positive reserve has no consumption and no self-discharge under an admitted provider
- **THEN** the first case shows no usable reserve and the second shows no depletion at current rates, without dividing by zero or implying those different states are equivalent

#### Scenario: Small changes and large periods
- **WHEN** a genuine small deficit exists or a finite estimate exceeds the normal numeric display range
- **THEN** rounding does not reclassify depletion as non-depletion; the display uses meaningful small-value or bounded-duration wording and never a negative period, invalid number or misleading zero

### Requirement: MPI04 — Reflect current state without altering it

The information SHALL refresh on the next inspection update after the underlying state changes,
including paused selection and completed connection changes. It SHALL use the selected circuit's
current state after load, map changes, reconnection and building removal. Electrical disabling and
unlimited-power debug mode MUST be distinguishable from ordinary operation. Merely inspecting
power SHALL NOT alter simulation state or consume randomness.

#### Scenario: Switch equipment or begin work
- **WHEN** native switching or an owned Lights Out/RePowered work transition changes actual consumption
- **THEN** the next refreshed readout reflects that resulting consumption once, without applying another discount, reserving forecast power or changing the job's eligibility

#### Scenario: Reconnect or change maps while paused
- **WHEN** the player changes selection, completes a reconnection, switches maps or loads another save while paused
- **THEN** the next inspection shows the correct current circuit once its native state is available, without retaining old map, network or save information

#### Scenario: Electrical outage or EMP restrictions
- **WHEN** electricity is disabled or an EMP makes battery energy unavailable to the native reserve calculation
- **THEN** the readout identifies the disabling state or reduced usable reserve; it does not advertise healthy unlimited operation, and an estimate with unsupported mixed-battery effects is unavailable

#### Scenario: Unlimited-power debugging
- **WHEN** the native unlimited-power debug option is active
- **THEN** the inspection explicitly identifies that mode and withholds ordinary reserve-duration claims until it is disabled; the readout adds no cheat control of its own

#### Scenario: Observe repeatedly
- **WHEN** the player repeatedly opens or refreshes power inspection, including during charging or discharge
- **THEN** only the display changes; energy, switching, topology, fuel, jobs and random outcomes follow their ordinary simulation paths, with no repeated diagnostic spam

### Requirement: MPI05 — Preserve supported composition and isolate failures

Compatible native and external changes SHALL be allowed when the required behavior remains true.
The feature SHALL suppress its overlapping readout when original More power info is active, and
SHALL NOT impersonate that package, mutate it or claim unverified consumers. Failure of an enabled
integration SHALL leave native operation and unrelated information usable, with a bounded startup
warning identifying the unavailable contribution and fallback.

#### Scenario: Original provider is active
- **WHEN** original More power info is active at startup
- **THEN** the owned overlapping readout remains inactive with a startup explanation, and the original provider is neither removed nor changed

#### Scenario: Compatible foreign contributions
- **WHEN** another mod supplies native-compatible equipment or changes unrelated inspection behavior
- **THEN** that origin or modification alone does not suppress correct owned totals and estimates, and the other contribution remains intact

#### Scenario: Forecast unavailable but totals reliable
- **WHEN** an enabled battery-model integration lacks its required behavior while current production and usable reserve can still be established
- **THEN** only the estimate is unavailable, with its reason and bounded startup diagnostic; trustworthy totals and normal electrical simulation remain usable

#### Scenario: Inspection contribution cannot be applied safely
- **WHEN** the required inspection behavior is missing, ambiguous or fails during installation
- **THEN** the affected owned contribution stays inactive, startup logs identify each skipped enabled patch and its fallback, and no partial owned intervention or removal of another mod's changes remains

### Requirement: MPI06 — Keep contextual presentation readable

The feature SHALL use clear localized labels for production, current consumption, balance, usable
reserve, current-rate estimate and storage-only estimate. It SHALL support all seven baseline
languages and ordinary inspection navigation. It SHALL add no persistent entity state, settings
row, bottom tab or view toggle solely to display this contextual information.

#### Scenario: Localized narrow inspection pane
- **WHEN** the player inspects a supported circuit in English, German, Spanish, French, Simplified Chinese, Russian or Japanese
- **THEN** labels, qualifiers, units and unavailable reasons remain readable without missing keys, broken glyphs or meaning-destroying clipping, while existing selection and gizmos still work

#### Scenario: Selection is not configuration
- **WHEN** the player changes the selected device or closes the pane
- **THEN** the readout follows that contextual selection without saving an override, changing a global default or opening a separate management window
