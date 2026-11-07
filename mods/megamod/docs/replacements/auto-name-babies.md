# Auto Name Babies

## Identity and provenance

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: replacement, **S044 / P1-35**, Auto Name Babies by Cruesoe, `cruesoe.autonamebabies`, Workshop
**3782676886**. Inspected 2026-11-03; About version 261016, selected 1.7 assembly 0.0.0.0, SHA-256
`27EC375F26BA8E315F8AED6556DF81FE21E184C494EADD905071659216F63CFA`.
Included MIT license, copyright 2026 Cruesoe; no upstream code, DLL or art reused.

[Research](../refinement/2026-11-03-auto-name-babies-research.md) records public-document access limits,
complete installed behavior, native 1.7 contracts, exact source discrepancies and external-consumer
limits. [Proposal](../../../../openspec/changes/specify-megamod-auto-name-babies/proposal.md),
[design](../../../../openspec/changes/specify-megamod-auto-name-babies/design.md),
[observable spec](../../../../openspec/changes/specify-megamod-auto-name-babies/specs/megamod-auto-name-babies/spec.md)
and [tasks](../../../../openspec/changes/specify-megamod-auto-name-babies/tasks.md) own the draft.
**Nothing implemented or accepted in game.**

## Behavior inventory

| ID | Native input/action → result | Selected scope / correction | Status |
| --- | --- | --- | --- |
| AN-01 | Player baby born → generated full name | Native generator and nickname, player ownership, established parent/xenotype inputs, one assignment | Drafted |
| AN-02 | Surname policy → parent/generated surname | Father/Mother order, default native Random parent weighting, prepared surname preservation, optional Ideology | Drafted |
| AN-03 | Birth notice opened → information with Jump/Close | No required naming prompt after success; retain outcome, foreign actions and archive identity | Drafted |
| AN-04 | Stillbirth or no-letter birth → named pending baby | Preserve death/outcome and intentional notification suppression | Drafted |
| AN-05 | Legitimate adoption → name only if pending | Preserve genuine existing name and native letter; no new adoption permission | Drafted |
| AN-06 | Save opens → confirmed pending names caught up | Maps/holders/world/caravan/deceased scope, no repeated scan or legacy guessing | Drafted |
| AN-07 | Player accepts name → retained name | Intentional Baby protected; native editing window unchanged; safe failure restoration | Drafted |
| AN-08 | Policy edit → future names follow effective scope | Common global/save row, optional DLC fallback, no retroactive rename | Drafted |

All rows derive from the complete research inventory and link to their corresponding requirements
in the observable spec above. They are specified behavior, not delivered code or compatibility proof.

## Interactions and composed result

S031 Toddlers retains its development/care ownership. Its toddler stages still use the native Baby
developmental category, so confirmed pending toddler names remain eligible for catch-up/adoption;
chosen names and toddler development remain unchanged. Supported alien stages use their actual
stage/naming rules. No patch to our own stage handling is needed. Other selected name/HUD/notification
features must call owned behavior directly when their contracts are specified.
No demonstrated external consumer was found for source helper/types in bounded installed text/DLL
searches, so there is no speculative compatibility facade or package spoof.

Native full-name generation, parent relationships, ordinary names/deadlines and letter/history
remain the external contracts. Random new births preserve native parent-entry weights; fallback
without a prepared surname uses distinct surname choices as in the source. Ideology uses actual
native marriage-role/willingness behavior, not the source tooltip's oversimplification. Existing
stochastic surnames are not rerolled on load. No changes to parental ideology/marriage or gameplay stats.

## UI audit and relocation

| Source surface | Purpose | Megamod destination / deliberate change |
| --- | --- | --- |
| Standalone global radio group | Surname configuration | Common Colonists / Family and names global/Use global/current-save row |
| Birth/adoption letter edits | Announce completed name without forced prompt | Same native notification/history; touch only confirmed successful owned naming |
| Native pawn renaming | Individual name choice | Native contextual controls, unchanged remaining deadline; never a mod setting |
| No bottom tab/toggle/debug entry | None | None introduced |

Default Random; no inheritance exception. Main-menu save controls/callbacks are disabled and dormant
overrides preserved. Absent Ideology retains stored Ideology but explains effective Random; absent
Biotech makes this feature unavailable. Settings affect future naming only. All seven languages
must have accurate policy/help/notice text. The Mother fallback help is corrected to actual source
order; usually-name help describes native rules. Automatic names do not add a permanent rename cheat.

## Contracts consumed by other mods or saves

| Contract and evidence | Decision / boundary | Required verification |
| --- | --- | --- |
| Native Pawn.Name, parents and deadline | Preserve names and relationships; no deadline renewal or -1 write just to suppress notices | Birth, voluntary rename, deliberate Baby, expiry, reload |
| Native baby letter, target and archive | Preserve outcome/unrelated additions and identity; success-only prompt changes | Read/Jump/Close/archive, dead/missing target, provider-present workflows |
| Source public helpers/patches/enum and assembly identity | No outside executable consumer found; no binary facade/drop-in claim | Inspect concrete consumers if discovered |
| Source global `surnameMode` | No silent import; common owned policy instead | Global/save scope, main menu, optional DLC fallback |
| Owned pending/success history | Minimal saved pawn gameplay state; ambiguous/malformed history preserves current name | Save switching, containment/death, language change and conservative recovery |

No file migration is included. Runtime catch-up captures prior event state, commits name/notification
together, verifies them and restores only owned changes on failure. Source removal/safe mid-save
provider swapping is unproved; future migration needs backup, atomicity, restoration and postconditions.

## Implementation and coverage

| Scope | Implemented | Deviations / possible conflict | Missing or omitted |
| --- | --- | --- | --- |
| AN-01–08 | Nothing | Pending provenance, usable surname fallback and safe name/letter transaction correct source risks | All implementation/tests/native acceptance |
| Source placeholder heuristic | Nothing | Do not rename arbitrary literal Baby or miss owned pending state after language change | Deliberately omit guessing ambiguous legacy names |
| Source global surname prefix | Nothing | Restrict to the actual player birth | No foreign-birth policy change |
| Already named adoption letter edits | Nothing | Preserve native behavior without falsely claiming owned auto-naming | Deliberately omit blanket non-placeholder suppression |
| Compatibility/performance | Nothing | Source suppresses owned overlap; no second pawn/Thing index | Exact provider seams, lifecycle costs and real runs |

Core/Harmony required for implemented hooks; Biotech required for feature activation, optional for
the product. Ideology and original source optional. Active `cruesoe.autonamebabies` disables the owned
overlap with bounded diagnostics, leaving foreign patches intact. Other naming/letter providers
need narrow semantic admission, not an absence-of-other-patches rule. No XML currently; later XML
requires XML Extensions content/shape/package guards and merge-preserving operations. No upstream,
private Harmony or Gateway DLLs packaged. Each hook requires its target document and startup check.

## Verification and acceptance

AN-Q1–4 retain exact effective target/transaction/provenance, generator/Ideology/provider, common
settings/lifecycle and native acceptance work. Final reviewed-build evidence must show actual births,
adoptions, notifications, manual names and load/settings actions in fresh minimized isolated games,
with exact optional-mod runs and personally inspected causal screenshots. Math/state tests and logs
alone cannot establish successful native names or preserved notifications. No build/install/game
launch is required to review this prose draft; none establishes gameplay acceptance here.
