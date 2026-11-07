# Trees Don't Just Die

## Identity and provenance

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, **1.7 beta**.
Origin replacement, S077/P2-06. Source **Trees Don't Just Die**, **Azuraal**,
`azuraal.treedontjustdie`, [Workshop2784870434](https://steamcommunity.com/sharedfiles/filedetails/?id=2784870434).
Read-only inspection2026-11-04. Metadata1.3–1.6; no selected binary or version route. Sole behavioral
XML SHA-256 `045C14E1078709CC3D54E3CF4DF02161585430F6E2980EC0F280DD98B16EA128`.

Steam returned a request-rate error; installed author description preceded complete XML analysis.
Only four installed files, no README/license/settings/assemblies; no reuse rights inferred.
[Research](../refinement/2026-11-04-tree-lifespan-research.md) records all inputs, current native
damage/UI/save/spawner paths, XML Extensions limitations and static comparators. No upstream code or
assets copied. [Owned change](../../../../openspec/changes/specify-megamod-tree-longevity/)
specifies future work only.

## Behavior inventory

IDs refer to the [observable contract](../../../../openspec/changes/specify-megamod-tree-longevity/specs/megamod-tree-longevity/spec.md).

| ID | Source/native action → result | Owned coverage/deviation | Status |
| --- | --- | --- | --- |
| TL01 | Four XML operations put0 in direct TreeBase/DeciduousTreeBase children's local lifetime scalar | Keep trees/bonsai/tree-like source plants; extend to compatible nested descendants; exclude unrelated crops/stumps | Drafted |
| TL02 | Native limited-lifespan age term is disabled; other death terms unchanged | Preserve each threat, normal chopping/harvest and special tree mechanics | Drafted |
| TL03 | Scalar changes apply to existing/new objects; native age/save/HP untouched | No healing/resurrection/reset; retained age matters again when finite lifespans return | Drafted |
| TL04 | Native info omits Limited lifespan, duration and instance age rows | Preserve native presentation; no new UI/settings | Drafted |
| TL05 | Longer-lived trees continue occupying cells and native listers | Preserve population/spacing/regrowth rules; no promised density change | Drafted |
| TL06 | Source hides failed XPath matches via Always | Add semantic/content guards, atomic scalar contribution and bounded warning/authority behavior | Correction drafted |

## Interactions and composed result

S006/S076 growing choices, S063 construction clearing and S069 blight keep normal plant work and
designation contracts. This feature supplies no scheduler, automatic order or protection from those
actions. Future growth changes can alter growth duration while this feature still disables only the
finite age limit. Do not stack internal patches or duplicate native/shared plant data.

Explicit correction: recursively cover compatible descendants rather than only immediate parent
names. Their positive age-limit overrides are intentionally superseded, which conflicts with a mod
deliberately giving that same supported tree a finite lifetime. Unrelated custom lifetime/damage
mechanics require separate evidence; no blanket immunity or compatibility claim. Generic IsTree
alone is too broad because native fibercorn/stumps also satisfy it.

When original `azuraal.treedontjustdie` is active, our contribution remains inactive before mutation;
the source's actual coverage applies, without our nested-descendant extension. XML Extensions and its
Harmony dependency are required at implementation. Optional DLC/provider content is absent-safe;
specific integrations use canonical package/Def/shape checks. Preserve unrelated XML siblings/lists
and existing nonpositive lifespans; no whole-list replacement or runtime Def mutation.

## UI audit and settings

| Source surface | Purpose | Owned destination |
| --- | --- | --- |
| Native plant information cards | Show effective plant traits/lifetime | Native information; no separate finite-lifetime override |
| No source commands, tabs, toggles or settings | No additional interaction | No new bottom/debug/settings surface |

No configurable parameter is selected, so no global/save/Use global row or exemption is invented.
Any new player-facing explanation uses all seven baseline languages; technical log-only warnings
remain invariant. No private plant index or recurring scan.

## Contracts and persistence

| Consumer/evidence | Contract | Disposition |
| --- | --- | --- |
| Native plant damage, display and spawning | Limited-lifespan scalar and native age | Preserve all other fields and consumers; actual reviewed-build workflows pending |
| Native saves | Existing age/growth/HP fields | No custom state/import; no age reset; finite lifespan restoration can resume damage |
| RimPy database text hit | Descriptive source package/Workshop record | Not a runtime facade requirement; no source package alias |
| Other executable providers | No actual consumer proved by bounded text scan | Audit demonstrated bindings/overrides; no compiled/reflection or universal drop-in claim |

Source supplies no new Def, API, save/settings schema or assembly. No automatic migration is needed
or promised. Finite-age restoration preserves native state rather than manufacturing safety by reset.

## Implementation and acceptance

**Nothing implemented.** All TL01–06 target documents, validators, XML, focused RED/GREEN, dependency
proof, final native acceptance and performance/balance evidence remain outstanding. Source initial-
parent/local-node misses and silent Always success are deliberately not copied.

Static oak/pine/saguaro/bonsai thresholds are270/180/200/13.5days; these are source calculations,
not accepted finalized Def measurements. The intended gain is standing tree longevity and less
age-driven replacement. Growth, harvest work/yield, research and non-age threats stay unchanged.
Longer-lived occupants can affect native forest turnover without a guaranteed rate or density.

TL-Q1–4 in the [progress ledger](../refinement/specification-progress.md) retain exact XML/admission,
scope/balance, actual provider contracts and implementation/native acceptance. Real time progression,
inspection, chopping, environmental threats and save/load outcomes must be personally observed on
the reviewed build; startup/Def inspection alone cannot accept this feature.
