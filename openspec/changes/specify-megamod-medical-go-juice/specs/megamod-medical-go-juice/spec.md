# Medical go-juice classification

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S086/P2-15 replacement of Go-juice is medical. Observable specification only.

## ADDED Requirements

### Requirement: The existing go-juice item is a medical drug
Go-juice SHALL be classified as Medical instead of Hard for normal category-dependent behavior.
This applies to the existing item wherever it occurs, for all pawns and factions; it is not limited
to Hussars, nobles, dependency carriers or one map. Existing stacks, drug-policy entries and supply
orders retain their identities. Other drugs, including psychite variants, are unchanged by this
feature. It adds no substitute item and does not make go-juice a general tending medicine.

#### Scenario: Existing stock and another drug
- **WHEN** a save with go-juice, yayo and existing drug policies is opened with supported classification active
- **THEN** go-juice is treated as Medical, yayo retains its applicable classification, and the existing stock and policy choices remain intact.

#### Scenario: A non-Hussar takes go-juice
- **WHEN** an otherwise permitted non-Hussar consumes go-juice
- **THEN** it has the same Medical classification as a Hussar's dose; no special ancestry or title check is required.

### Requirement: Apply the actual category-dependent consequences
Go-juice consumption SHALL cease to count as recreational-drug or hard-drug use where the ordinary
rule distinguishes those categories from Medical. Consequently, medical-only ideology does not
produce the recreational-drug-use penalty for that consumption. Generic drug-use consequences and
unrelated existing memories remain applicable. Permission depends on the initiating action:
Medical go-juice can be carried or scheduled under the ordinary drug policy without the nonmedical
drug willingness check, even when direct inventory use would be refused. Direct inventory use keeps
its ordinary permission check, including active dependency exceptions. This feature adds no new
universal veto or universal permission and does not erase old penalties.

#### Scenario: Medical-only ideology
- **WHEN** a pawn with a medical-only ideology completes a permitted go-juice dose
- **THEN** that consumption does not create a recreational-drug or hard-drug use penalty merely for taking go-juice.

#### Scenario: A consequence applies to all drugs
- **WHEN** a permitted route completes go-juice consumption for a pawn whose ideology penalizes all drug use
- **THEN** the applicable generic drug-use consequence still occurs; previous memories are not retroactively deleted.

#### Scenario: Policy-authorized scheduled use differs from manual willingness
- **WHEN** a nondependent pawn's policy authorizes scheduled go-juice, its ordinary timing/mood/joy/access conditions are met, but direct inventory use would fail the pawn's general drug willingness check
- **THEN** Medical classification permits the ordinary scheduled-use route without adding that manual-use veto; any generic ingestion consequences still apply.

#### Scenario: Policy carrying does not authorize manual injection
- **WHEN** a nondependent pawn has a positive carry-policy target, scheduled use is off and ordinary carrying conditions are met, but direct drug use is disallowed
- **THEN** Medical go-juice can be stocked under that policy without automatically taking it or making a manual inventory injection permissible.

#### Scenario: Manual inventory permission and dependency exception
- **WHEN** a nondependent pawn fails the ordinary manual drug-use permission check, or a pawn has an active matching dependency exception and otherwise usable inventory go-juice
- **THEN** the first pawn cannot inject through the manual inventory control; the second retains the native dependency allowance rather than being blocked by a newly invented universal prohibition.

### Requirement: Classification does not remove chemical risks or supply costs
Go-juice SHALL retain its applicable high, rest/psyfocus effects, joy amount, dependency satisfaction,
addiction/overdose behavior, dose size, use duration, combat-enhancing role, recipes, research, price,
mass and stack limits. Medical classification is not a promise of safety or healing. The feature
does not supply doses or add an independent administration order. Scheduled, dependency, medical-response and
player-ordered use still follow the rules of their initiating action. Partial consumption, when
supported by S085, uses its complete admitted dose contract without a second set of effects here.

#### Scenario: Dependency and chemical effects
- **WHEN** a dependent pawn consumes an available permitted dose
- **THEN** ordinary dependency satisfaction and chemical effects occur once; the classification change does not cancel applicable overdose/addiction risks or supply the dose for free.

#### Scenario: A policy does not authorize scheduled use
- **WHEN** no applicable action authorizes taking go-juice
- **THEN** changing the category does not itself create a scheduled use, emergency response or treatment order.

### Requirement: Keep ordinary inventory use and accurate behavior claims
Permitted go-juice use SHALL remain accessible through the supported native inventory and drafted
pawn controls, including the native menu when more than one drug is available. Existing action-specific
eligibility remains authoritative. No separate tab, debug action or settings page is added for
ordinary injection. The feature does not impose a universal binge or recreation ban: the existing
selection rules remain authoritative, and an existing applicable any-category chemical binge can
still select go-juice. A stronger no-binge policy is unresolved GJM-Q1, not delivered by this category
change. No tooltip or documentation may claim go-juice is harmless or categorically binge-proof.

#### Scenario: Drafted pawn uses its inventory dose
- **WHEN** a single selected, player-controlled drafted pawn carries go-juice and is permitted to take it
- **THEN** the native drug command or its multi-drug menu offers the dose and ordering it uses the actual item with ordinary effects.

#### Scenario: Existing any-category binge
- **WHEN** an existing chemical binge's ordinary selection rules admit the pawn's available go-juice
- **THEN** this category-only feature adds no special veto and does not advertise guaranteed exclusion from that binge.

### Requirement: Preserve unrelated content and fail locally
Only the supported go-juice category SHALL change. Unrelated provider additions and changes to its
effects, recipe, graphics, categories or other content remain intact. An already-Medical item is an
idempotent success. Missing, ambiguous or incompatible target content leaves this change unapplied
and produces one bounded startup warning explaining the lost classification and fallback; compatible
unrelated edits do not reject it. With the original provider selected, it owns overlap without a
second owned mutation. No source configuration/import, new per-save setting or gameplay entity state
is introduced. Existing native text and any added explanation must be usable in all seven baseline
languages: English, German, Spanish, French, Simplified Chinese, Russian and Japanese.

#### Scenario: Other content additions and repeated loading
- **WHEN** another provider adds go-juice effects or recipe entries, or the category is already Medical
- **THEN** supported classification preserves those additions and does not duplicate content or apply a second chemical effect.

#### Scenario: Missing or changed target
- **WHEN** the go-juice target is absent, duplicated or no longer has a supported category-bearing drug shape
- **THEN** no substitute or partial edit is made, a bounded startup warning explains the unavailable classification, and unrelated features remain usable.
