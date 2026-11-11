## Why

Doctors should respond to urgent care without abandoning deliberate orders or duplicating doses.
S134 independently replaces Priority Treatment and composes dependency assistance directly with
S124 Drug Response and the existing medical-care contracts.

Sole owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. Specification for later implementation only.

## What Changes

- Let ready, highest-priority doctors interrupt ordinary work for rescue and eligible tending,
  respecting orders, ongoing care, sleep/meal policy and their own immediate needs.
- Choose new patients by imminent danger across rescue/tending while protecting care already underway.
- Use actual doctor capability and ordinary medical risks; omit the source blanket injury/scar veto.
- Queue ordinary dependency-drug operations for incapacitated patients using shared S124
  pending-dose, actual-consumption, policy, cancellation and save rules.
- Queue the available native porcupine-quill operation, with a persistent player stop/resume choice.
- Keep configuration in inherited settings, automation switches bottom right and patient decisions
  in Health. No new main bottom tab and no automatic surgery from a condition click.
- Preserve treatment costs, medicine rules and ordinary work. Document corrections and omitted source
  defects; omit original API/binary/save compatibility checks and facades rather than impersonating
  an upstream package.
- Warn at startup when original Priority Treatment is active and request ordinary per-feature
  settings disablement of the owned overlap, best effort, without a cooperative compatibility guarantee.

## Capabilities

### New Capabilities

- `megamod-priority-treatment`: emergency readiness, care scheduling, actual-capability operation eligibility,
  coordinated dependency assistance and quill-operation automation.

### Modified Capabilities

None. Consume shared orders and existing owned medical policies directly.

## Impact

Documentation only. Future runtime requires Core, Harmony and XML Extensions
(`imranfish.xmlextensions`, Workshop2574315206) for guarded additive native scheduling integration.
Biotech dependency and Odyssey quill content are optional and content/shape gated. No Priority
Treatment, Drug Response, other repository mod or Dev Gateway product dependency.

Installed reference: Dreadarm.PriorityTreatment, Workshop3813351382, version0.1.22, 1.7 assembly.
Public guide/API, complete compiled behavior and focused native1.7 contracts were inspected.
Its all-rights-reserved license requires independent implementation; no copied source, binary or art.
Seven-language UI, executable target admission, TDD, review and live acceptance remain future work.
