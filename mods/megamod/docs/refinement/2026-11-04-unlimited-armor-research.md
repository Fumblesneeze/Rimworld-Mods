# S075 — UnLimitedArmor research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
Read-only inspection **2026-11-04**. Origin replacement. Specification draft independently reviewed
and strictly validated; no implementation or native acceptance.
[Owning change](../../../../openspec/changes/specify-megamod-uncapped-armor/).
S136 Floor Pipette remains separately queued.

## Documentation first and identity

Read the [author's Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=1695493009)
before the complete installed XML. The author describes removing the apparel armor-rating ceiling,
acknowledges the Gear overall summary still being capped, explicitly excludes Combat Extended and
offers only tentative Yayo compatibility. Mid-save addition/removal is an author claim, not our
acceptance evidence. Generic removed/incompatible page boilerplate is not a verified removal notice.

Installed title **UnLimitedArmor**, author **Paragon**, package **Paragon.Hanul.UnLimitedArmor**,
Workshop **1695493009**. About supports1.0–1.6; no1.7 declaration, LoadFolders.xml or version directory.
Only four files: About.xml, PublishedFileId.txt, Preview.png and Patches/Core/Stat_Apparel.xml. No C#,
assembly, settings, saved types, dependencies or custom UI. Preview not selected or reused. No license
file or public source repository was supplied by these inspected inputs; do not infer reuse permission.
The XML is the complete behavior source and remains read-only.

Installed root `F:/Steam/steamapps/workshop/content/294100/1695493009`.

| Input | SHA-256 |
| --- | --- |
| About/About.xml | `586923E86F65A14750338722551E60B6A214E5E9EAEDA6FC682F0806CFA1E926` |
| Patches/Core/Stat_Apparel.xml | `0BA6C0023830BCB8EF6D645A0BEB00EB18E2EBC0E0E77EB8DCC9E91BD806CFA6` |

## Complete selected source behavior

One PatchOperationRemove targets `*/StatDef[@Name="ArmorRatingBase"]/maxValue`, with success Always.
It removes the **shared abstract stat parent's upper bound**, not an apparel item field or damage
algorithm. There is no predicate proving unique compatible content, no source-provided failure warning,
no active-package incompatibility gate and no changes to quality/material factors, lower bounds,
damage, armor penetration, costs, research, items or overall-Gear drawing.

Effects flow through XML inheritance. Child stats with an explicit separate upper bound retain it;
unrelated stats and separate armor systems are not intentionally uncapped. Additional compatible
stats inheriting this parent can also inherit its changed bound. Removing the field is idempotent as
a desired result, but Always masks the difference between already absent and wrong/missing targets.
The owned version needs to distinguish those cases. This scalar ceiling removal is intentionally
destructive; no additive list merge can remove a bound. All unrelated fields/list entries must survive.

The source description's apparel wording is narrower than its actual effect: shared rating stats also
serve a pawn's natural armor. No faction, colonist, species or equipped-only filter exists. A future
spec must cover both, without promising immortality or uncapping every other combat value.

## Current native chain

Current installed game1.7.4998 rev1299, Assembly-CSharp.dll SHA-256 rechecked:
`FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B`.
Native extracts are in `artifacts/Refinement/megamod-spec-20261104/unlimited-armor/native/`.
Read the complete ArmorUtility, relevant StatDef/StatWorker finalization and ITab_Pawn_Gear armor paths;
unrelated methods are not claimed audited.

`Data/Core/Defs/Stats/Stats_Apparel.xml` still defines abstract ArmorRatingBase with default0, min0,
hideAtValue0, **max2**, percent formatting and quality factors. Sharp, Blunt and Heat inherit it.
Their Stuff parts remain independent factors. Native StatDef default maxValue is **9999999**, not
literal infinity. Removing the explicit ceiling restores that generic default; it does not add a
new feature-specific upper bound. Do not promise support for infinite, NaN or overflowing data.

StatWorker.FinalizeValue applies stat parts, optional curves/factors, scenario factors and rounding,
then clamps to the stat's bounds when applyPostProcess is true. The selected patch affects that final
ceiling; it does not multiply base armor or change quality/material calculation. A normal individual
item information card using finalized values can therefore show over200% after admission.

ArmorUtility.GetPostArmorDamage evaluates worn pieces covering the hit part, outside inward, followed
by pawn natural armor. Both request the relevant finalized stat. ApplyArmor subtracts armor penetration
from rating, with a zero floor, and compares a random value against half/full residual rating. A
deflection sets damage to zero; the middle band halves damage and converts Sharp to Blunt. Armor wear
occurs before that roll: quarter of incoming damage, randomly rounded, when an armor item participates.
The source changes none of these steps. The declared MaxArmorRating=2 constant does not clamp incoming
rating in this inspected method. Armor-bypassing damage and uncovered body parts are not made safe.

ITab_Pawn_Gear.TryDrawOverallArmor is a separate summary: clamp each rating/2 to0..1, combine covered
layers multiplicatively per body part, weight by coverage, double and clamp the result to0..2. It is
not the sum of armor ratings and does not subtract a particular incoming attack's penetration. Thus
a200% overall summary and an individual216% rating are not contradictory combat values. Removing
the summary clamps would invent a different metric and could make the probability-style product
nonsensical. Preserve it unless a later UI specification deliberately replaces it with a defined
alternative. S132 inventory UI must distinguish its summary from individual ratings.

## Static balance comparators

These are source/XML calculations, not finalized loaded-Def measurements or live balance acceptance.
Core `ThingDefs_Misc/Apparel_Various.xml`, Royalty's file of the same name and the shared quality part
give the following no-other-modifier examples. Legendary factor is1.8; Normal is1.0.

| Item | Normal Sharp | Legendary calculation | Existing ceiling / proposed inherited-bound removal |
| --- | --- | --- | --- |
| Flak vest |100% |180% |180% /180% |
| Marine armor |106% |190.8% |190.8% /190.8% |
| Cataphract armor, Royalty |120% |216% |200% /216% |

No acquisition, crafting skill/work, material quantity, research, movement penalty, layer coverage or
HP is changed. The balance effect is extra protection against penetration where a finalized rating
would otherwise exceed200%; it benefits hostile pawns and natural armor as well as colonists.
For example, a216% layer facing30 percentage points of penetration has186% residual instead of170%:
under the unchanged native roll that means93% versus85% deflection, with the remaining middle band
reducing damage. This is a formula illustration, not an observed combat distribution. Multiple layers,
converted damage, wear and actual hit coverage still matter. No new tuning setting is selected yet.

## External consumers and provider limits

Bounded installed C#/XML/JSON search for UnLimitedArmor/package identity found only its own metadata
and a mod-manager database; no callable source API or saved binding exists in the four-file source.
This does not prove the absence of generic stat consumers or compiled package checks. The intended
shared contract is the existing stat identities and normal GetStatValue result, without package spoofing.
S009/S010/S012 compare Normal item stats; they must consume current ordinary values without a private
200% clamp or treating armor rating as guaranteed colony protection. S131 colors and S132 inventory
remain separate queued UI work; no new source bottom tab, settings or runtime toggle exists to relocate.

Combat Extended is explicitly unsupported by the source. The current
[CE primary metadata](https://raw.githubusercontent.com/CombatExtended-Continued/CombatExtended/Development/About/About.xml)
confirms **CETeam.CombatExtended**. No installed exact canonical CE package was found in the bounded
About search. Selecting CE must suppress this owned inherited-bound change before mutation; do not
reject the entire megamod or claim a CE combat adapter. Its different armor system remains its own.

Installed Yayo's Combat3 Continued is **Mlie.YayosCombat3**, Workshop2854006492. Its README was read
before inspecting the selected1.6 binary's relevant types. Hash
`8612503FFB539ECB8679B04C93764E6E415F45E1BA5D3CC2943E69D467369997`.
LoadFolders/About advertise through1.6, with no explicit1.7 route; no beta compatibility inferred.
Relevant decompilation with game references is under sibling `consumer/` (read-only).

Its DamageWorker_AddInjury prefix selects the custom path only when advArmor is active; otherwise it
allows native behavior. Custom ArmorUtility still gets ordinary rating stats for apparel and pawn,
but uses technology, penetration, durability/health, configured effectiveness and different random
bands. It does not impose a simple incoming-rating2 cap there. This establishes a real generic stat
consumer, not a proof that Yayo's whole prefix is1.7-compatible. Retain provider calculations, do not
apply vanilla combat probabilities to it, and do not patch Yayo to mimic the source XML change.
The source's “maybe” is not an accepted compatibility claim. Provider/effective-shape verification remains.

## Guarded XML route and remaining work

Read official [Conditional](https://github.com/15adhami/XmlExtensions/wiki/XmlExtensions.Conditional),
[FindMod](https://github.com/15adhami/XmlExtensions/wiki/XmlExtensions.FindMod) and
[Log](https://github.com/15adhami/XmlExtensions/wiki/XmlExtensions.Log) documentation, then their complete
selected compiled type extracts retained for S067. Conditional tests node existence; XPath predicates
can express shape/cardinality. FindMod with packageId=true compares active package IDs; Log warning
emits a warning. Do not infer transactionality or semantic C# inspection from these helpers.

Inspected XML Extensions1.6 DLL hash rechecked:
`1E64E318C905B94DE3EF05186D82E2067933238453D818366EAC73D688464B35`.
Metadata/LoadFolders support1.2–1.6 only. Exact1.7 dependency loading remains implementation preflight.
XML Extensions becomes required with first owned operation use, together with its Harmony dependency;
do not copy either assembly. No owned Harmony patch or post-load Def mutation is justified merely to
remove this declarative scalar. Source-active authority must also be decided before XML mutation.

The draft selects inherited-bound removal only, preserving explicit child limits, native summary and
ordinary combat/stat consumers. It distinguishes already-uncapped compatible content from a wrong or
malformed target, retains original-provider authority and CE suppression, and adds no setting or new
combat formula. Proposal/design/spec/tasks and the [replacement report](../replacements/unlimited-armor.md)
are written. Independent source/native and design/composition reviews found no material issues;
strict validation passed97 items with zero failures. The draft has6 requirements/14 scenarios and
all18 runtime tasks remain unchecked. Technical log-only diagnostics stay invariant; any introduced
player-facing explanation uses all seven baseline languages, without inventing a new warning UI.

| ID | Later question / evidence | Current direction |
| --- | --- | --- |
| UA-Q1 | Exact XML phase, inheritance, beta dependency and failure semantics | Unique compatible target, preserved child limits, valid absent-bound state, source/CE selection before mutation, one scalar edit and owned-only rollback; no silent Always success. |
| UA-Q2 | Finalized scope and balance | Preserve natural/enemy parity, normal calculations and costs; verify high-quality and added-content ratings, penetration/coverage/wear and current flak/marine/cataphract comparators in game. |
| UA-Q3 | Provider and information consumers | Current Yayo advanced-armor paths, CE absence/fallback and S132 summary meaning; no universal compatibility, native-probability claim for providers or source-package spoof. |
| UA-Q4 | Implementation and native acceptance | Focused RED/GREEN, localized player explanations/invariant technical logs, independent review and actual equip/inspect/attack/save/provider workflows with retained causal evidence. |

No product code/Defs/assets, test, build/install, game launch, source execution, Workshop edit or normal
save/configuration mutation occurred. These findings do not accept gameplay or1.7 compatibility.
