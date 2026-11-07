# Dynamic Spacefarer Policy

## Identity and provenance

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
Origin replacement **S130**, source **asunib**, `asunib.DynamicSpacefarerPolicy`,
[Workshop3829638797](https://steamcommunity.com/sharedfiles/filedetails/?id=3829638797).
Selected1.7 assembly0.0.0.0 SHA256 `47600f0044e67e3a40f46d3e0c7e5ef34dede774792c2eb90db4b27097954b28`.
No public source/license located in author/local/bounded source searches; no redistribution selected.
[Research](../refinement/2026-11-06-dynamic-spacefarer-policy-research.md) distinguishes author claims,
complete compiled inspection and proposed corrections. **No behavior implemented or accepted.**

## Behavior inventory

| ID | Input → observable result | Criterion/default/deviation | Status |
| --- | --- | --- | --- |
| DSP-01 | Loaded positive-resistance apparel → candidate | Any finite positive own or worn declaration; no non-apparel/net-protection assumption | Specified only |
| DSP-02 | Fresh native Spacefarer policy → additive eligible allowances | Preserve utility, taint/quality/condition and other native filters; no pawn/default reassignment | Specified only |
| DSP-03 | New content after enrollment → one-time addition | Previously considered manual exclusions survive reload and content removal/re-add | Specified only |
| DSP-04 | Rename/language/reorder/copy/delete/release → follow real policy identity | One active target; no name/ID reuse transfer; history retained for surviving released policy | Specified only |
| DSP-05 | Existing-save enrollment → preview/decline/confirm/cancel | Explicit initial review replaces unsafe title guessing and unknown exclusion overwrite; atomic verified recovery | Specified only |
| DSP-06 | Use ordinary policy editor/assignment → native clothing behavior | Entity gameplay state, no new settings/tab or automatic dressing | Specified only |
| DSP-07 | Missing DLC, bad targets or original co-installation → safe inactivity | Expected no-DLC absence; bounded warning for enabled failed integration/owned overlap | Specified only |
| DSP-08 | Native linked item operation would reverse another choice → held and explained before commit | Existing exclusions/declines and ineligible types take precedence; independent additions still proceed | Specified only |

[Observable contract](../../../../openspec/changes/specify-megamod-spacefarer-policy/specs/megamod-spacefarer-policy/spec.md)
and [proposal/design/tasks](../../../../openspec/changes/specify-megamod-spacefarer-policy/) retain
future implementation, target checks and native acceptance work.

## Interactions and composed result

Native policy/filter data remain authoritative. S004 weather-ready arrivals chooses/generated
vacuum clothing under its own protection/faction rules; this policy inclusion does not claim full
protection or grant gear. S127 HUD and S132 future Gear UI use the same policy and ordinary editor.
No internal patch stack, additional pawn assignment rule, per-map cache or forced whitelist.

Odyssey is required for this feature only. Without it, other megamod features continue. Original
Dynamic Spacefarer Policy is optional; when active it remains authoritative and owned overlapping
maintenance is withheld/explained with warning. No foreign Def/tag/settings edits or unpatching.

## UI audit and relocation

| Source surface | Actual purpose | Owned result |
| --- | --- | --- |
| No source configuration/tab; native policy filter edits | Gameplay policy choices | Existing apparel-policy editor remains authoritative |
| Silent translated-name selection on old saves | Automatic one-time addition | Explicit contextual target/enrollment preview when reliable history is absent |
| Native manual exclusions | Per-policy allowed apparel | Persist through consideration history, no global/save configuration model |

Tracked-policy selection/release and item choices are entity state, not settings. No extra bottom
tab, bottom-right switch or debug override. Mutations require a live game and valid selected policy.
All owned text uses seven languages. New games automatically track the native-created policy;
subsequent newly qualifying types are automatic after enrollment. Existing saves remain untouched
until their one-time explicit enrollment. No source settings/history import is selected.

## Contracts consumed by other mods or saves

| Source/native surface | Preservation decision | Remaining evidence |
| --- | --- | --- |
| Source Spacefarer default tag mutation | Deliberately not copied; native target policy directly seeded, unrelated tags untouched | Any concrete external tag consumer needs its own evidence |
| Source public DynamicSpacefarerTags.Tag/Defs | No demonstrated external consumer; no speculative facade | Bounded search is not universal absence |
| dynamicSpacefarerAdded HashSet save key | No impersonation/import; owned per-policy history retains missing identities | Separate migration contract if import later selected |
| Native ApparelPolicy.filter / assignments | Additive ordinary filter operations, unchanged assignments and special/range filters | Native editor, save and wearing workflow |
| Mutable native id/label/load ID | Owned persistent identity for enrolled object; no copied/reused-ID tracking | Serialization/load/copy/delete proof on exact beta build |

## Coverage, deviations and acceptance

All eight groups remain unimplemented. Intentional differences: finite/invalid-data handling;
stable target and missing-content history; old-save preview rather than silent ambiguous mutation;
no global runtime tag changes; no automatic source-history migration; explicit handling of native
linked-item conflicts before committing. History prevents unsolicited
re-add of excluded content but does not restore native allowed-item state lost on provider removal.

All future hooks need executable minimal semantic targets, effective/incoming code guards,
compatible-owner tolerance, owned-only rollback and bounded warnings. No XML operations selected.
Focused TDD and independent runtime review precede final fresh minimized native policy/save/
assignment/clothing workflows. Separate exact optional-provider runs, build/config hashes,
causal screenshots and cleanup remain necessary. No stat, research, cost or full-protection
claim follows from this documentation or the author's Workshop description.


Initial scope is colonies started with megamod; existing-policy enrollment is contextual management within those colonies, not source-save import. S004 separately owns all-humanlike vacuum-arrival protection, including supported SOS2, with shared facts but no forced policy edits or free player equipment.
