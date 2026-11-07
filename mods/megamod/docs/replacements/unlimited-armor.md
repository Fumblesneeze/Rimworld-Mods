# UnLimitedArmor

## Identity and provenance

- Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
- Origin replacement, S075/P2-04. **Paragon.Hanul.UnLimitedArmor**, Workshop1695493009, Paragon.
  Installed metadata advertises1.0–1.6. No selected DLL: the complete source is one root XML patch.
- [Author description](https://steamcommunity.com/sharedfiles/filedetails/?id=1695493009) read before
  XML/native code. Hashes, exact inputs and source-versus-current-engine distinctions are in
  [research](../refinement/2026-11-04-unlimited-armor-research.md).
- No license file/source-code reuse permission established; no upstream files/artwork copied.
  **Specification draft only. Nothing implemented or accepted in game.**

## Behavior inventory

| ID | Observable behavior | Scope / limitation | Evidence / status |
| --- | --- | --- | --- |
| UA01 | Retain armor ratings above the inherited ceiling | Sharp/Blunt/Heat; ordinary factors and independent child limits survive | Complete XML/native finalization; draft |
| UA02 | Same rating policy for apparel and natural armor | All factions/species using that contract; no new equipment or costs | Native stat consumers; source effect broader than apparel wording; draft |
| UA03 | Higher rating participates in ordinary armor calculation | Penetration, coverage, wear/layers and provider formula remain | Complete native ArmorUtility, bounded Yayo consumer; draft |
| UA04 | Individual rating differs from Gear overall summary | No invented sum, probability or quality selector | Native stat/overall drawing and existing browser contracts; draft |
| UA05–06 | Guarded declarative admission and stateless lifecycle | Original authority, CE suppression, no silent wrong-target success or source migration | Proposed safety correction; draft |

All rows map to [observable requirements](../../../../openspec/changes/specify-megamod-uncapped-armor/specs/megamod-uncapped-armor/spec.md).
[Proposal/design/tasks](../../../../openspec/changes/specify-megamod-uncapped-armor/) distinguish intent,
implementation and unchecked future verification.

## Interactions and composed result

Use ordinary finalized stat identities for S009/S010/S012 comparisons, S074 information navigation and
future S132 inventory. Normal comparison quality remains Normal; no private armor calculation, map
cache, aggregate-cap rewrite or internal patch stack. Equipment/quality/coverage and active combat
provider rules remain their own systems. S038 excluded content is not restored or made craftable.

## UI audit and relocation

| Source surface | Purpose | Owned placement |
| --- | --- | --- |
| Individual native stat information | Inspect actual armor rating | Existing contextual information, including above200% values |
| Native Gear overall summary | Coverage-weighted summary, capped separately | Preserve meaning/calculation; future inventory composition must distinguish it from individual stats |
| No source settings/tab/debug/toggle | None | No new window, operational control or global-only settings exception |

The source owns no settings or entity orders to migrate. Future configuration would need the common
global/save/Use global path and explicit application timing. Current selected behavior is static
fresh-load content. Technical log-only diagnostics remain invariant. Any introduced player-facing
explanation requires all seven languages; ordinary labels use native text. No new UI is needed merely
to host a warning.

## Contracts consumed by other mods or saves

| Consumer / evidence | Contract | Decision / limit |
| --- | --- | --- |
| Native stat/armor/info consumers | Existing Sharp/Blunt/Heat identities and finalized GetStatValue values | Preserve identity/calculation, remove inherited ceiling only |
| Yayo's Combat3 Continued, Mlie.YayosCombat3 | Selected advanced-armor branch reads ordinary apparel/natural stats and applies its own formula | Real generic consumer; no API facade/internal patch, current1.7 support remains unverified |
| Combat Extended, CETeam.CombatExtended | Source explicitly declares unsupported separate armor model | Suppress owned contribution before mutation; do not disable all megamod or alter CE |
| Source identity in mod-manager data | Package metadata | Provenance only, no demonstrated callable contract or saved type |

No source assembly/API or settings/save schema exists. Bounded text search cannot prove absence of
all compiled identity checks. Do not spoof the package or claim universal drop-in compatibility.

## Implementation and coverage

Nothing implemented. Preserve the actual source's shared-stat scope and separate child caps; correct
its silent Always-success behavior with minimal guards and diagnostics. Scalar ceiling removal is
intended; replacing whole stats/lists or changing all maxima is not. Generic native numeric limits
remain, so “uncapped” does not mean arbitrary precision or literal infinity.

Required future dependency XML Extensions, with Harmony as its dependency; no owned runtime Harmony
patch selected. Original/content/providers/DLC remain optional. Original active means original alone
owns this feature. CE active means this contribution stays inactive; no automatic mod-list change.
Unknown/malformed required content leaves ordinary behavior intact. Exact target/order/beta proof is
pending. No base stat, recipe, research, price, skill, cost, HP or progression compensation is added.

## Verification and acceptance

Review/strict-validation outcomes belong in the [progress ledger](../refinement/specification-progress.md).
All implementation tasks remain unchecked. Required future evidence includes finalized inheritance,
individual versus summary UI, actual equip/attack/armor-wear workflows, natural and hostile armor,
source/provider/child-override cases, save switching and fresh-load removal. Formula calculations and
static comparators do not replace native attacks or prove balanced gameplay.
