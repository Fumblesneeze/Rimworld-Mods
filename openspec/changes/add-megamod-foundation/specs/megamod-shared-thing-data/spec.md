## Owner and delivery

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). These requirements
govern future shared data; no cache, query feature, or hauling behavior is delivered by the scaffold.

## ADDED Requirements

### Requirement: One owner for each genuinely shared dataset
Features SHALL first reuse suitable native indexes. When additional materialization is needed,
search, hauling, and other consumers of the same Thing dataset SHALL share one map-scoped owner and
invalidation lifecycle rather than independently scanning/caching identical data. A derived index
SHALL document why its query semantics need distinct data and which shared source it uses.

#### Scenario: Search and hauling request the same candidate data
- **WHEN** both consumers query an unchanged map generation
- **THEN** they use the same maintained candidate source without independent full-map materialization
- **AND** each applies its own documented filters and native eligibility rules

### Requirement: Freshness and invalidation are part of the feature contract
Each dataset SHALL specify its fields, freshness boundary, updates, and safe native fallback or
unavailable state. Invalidate relevant spawn/despawn/destruction, movement/holder/map transfer,
stack changes, and indexed metadata changes. Maintain Verse/Unity state on the game thread; never
expose unvalidated live objects to background work. Rebuild an invalid generation once for all
consumers or maintain it incrementally. Invalidation patches require their own target specs.

#### Scenario: A cached Thing moves or is destroyed
- **WHEN** a relevant Thing changes after a consumer's snapshot
- **THEN** the next query meeting the specified freshness boundary reflects the change, or uses its documented fallback
- **AND** neither consumer acts on stale ownership/location/destruction state

#### Scenario: Freshness cannot be established
- **WHEN** a required invalidation hook is rejected by its target spec
- **THEN** consumers use the specified correct native query or disable the dependent behavior with a bounded warning
- **AND** a stale shared cache is not silently treated as authoritative

### Requirement: Sharing candidates does not share pawn eligibility
Cached lists SHALL NOT replace action-time checks for reservation, reachability, forbidden/access
state, pawn capability, holder ownership, or external provider eligibility. Native action/job paths
remain authoritative; exact per-feature policies SHALL be specified before implementation.

#### Scenario: Another pawn reserves a candidate
- **WHEN** a hauling candidate becomes reserved after enumeration and before the native action commits
- **THEN** the acting pawn honors the current native reservation result rather than the cached candidate's earlier eligibility

### Requirement: Transient data respects game and map lifecycle
Shared datasets SHALL not persist live Thing references in saves or across games/maps. They SHALL
clear on owner teardown, rebuild from current state after load, and keep maps isolated.

#### Scenario: Save reload and map removal
- **WHEN** a game is reloaded or a map is removed and another map is queried
- **THEN** no stale Thing references from the old lifecycle are returned and the surviving/new map has its own valid data

### Requirement: Optimization preserves behavior and has measured evidence
Before accepting shared data as an optimization, the owning slice SHALL compare native workflow
results and measure total query/update work, allocations, and memory with multiple consumers and
realistic churn. Record budgets, workloads, and any intentional behavioral changes in the spec.

#### Scenario: An index reduces searches but increases maintenance
- **WHEN** combined consumer benchmarks and player workflows are reviewed
- **THEN** the report includes maintenance/memory costs as well as query speed and does not claim a net win without supporting measurements
