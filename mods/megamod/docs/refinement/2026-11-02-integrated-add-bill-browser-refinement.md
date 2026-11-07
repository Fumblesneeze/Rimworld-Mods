# Integrated Add bill browser — original feature refinement

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
S012 / P1-09. **Origin: original.** Source-mod identity/version/license is not applicable to this
composition; the user's interview is its provenance. The resulting
[change](../../../../openspec/changes/specify-megamod-integrated-add-bill-browser/) and
[report](../replacements/integrated-add-bill-browser.md) specify proposed behavior only.
No implementation or live acceptance.

## Confirmed intentions

The [interview](2026-11-01-handwritten-notes.md) requires a rich view inside the bench's **Add bill**
workflow, not a separate menu or bottom tab. It has search, top product-category pills derived from
the bench's recipes, ingredient-plus-one-colonist/all-skills highlighting, and a relevant product-stat
sort dropdown with common properties first. Compare at **Normal quality**, without a quality selector.
**Use all materials** sits beside that dropdown and starts unchecked; checked includes unavailable
materials. The selected material must carry into the added bill. An explicit alternate-material
action is required; right-click was tentative, not a fixed exclusive gesture.

Nice Bill Tab/Expansion are UI references, not blanket selection of every upstream feature. Their
sequential research is complete enough for separate drafts before this composition:

- S009 [Quartermaster](2026-11-02-quartermaster-research.md): comparison, finite ingredient allocation,
  same-colonist skills, exact recipe/style/material bills and native fallback.
- S010 [Nice Bill Tab](2026-11-02-nice-bill-tab-research.md): read-only details, two-choice comparison,
  information cards/navigation and explicit omissions of queue/subtask/heuristic extras.
- S011 [Expansion](2026-11-02-nice-bill-tab-expansion-research.md): hiding/recovery, row Add, shortage
  cues, scopes/default changes and responsive access. Independent review corrected preservation
  of a sort after hiding its last relevant recipe; S012 must retain that correction.

No fourth copy of their material, stock, skill or bill rules is needed. Final original behavior must
call those owned policies directly, never patch one owned feature with another.

## Native inputs inspected for composition

The exact game identity remains **1.7.4995 rev309**, Assembly-CSharp SHA256
**56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21**. Read-only S009 evidence is
`artifacts/Refinement/megamod-spec-20261102/quartermaster/game/RimWorld.ITab_Bills.decompiled.cs`.
The ordinary path filters AvailableNow and AvailableOnNow for this bench, includes ideology-style
choices, makes native bills through MakeNewBill(precept), preserves missing-skill/mechanitor messages,
adds the bill, and emits concept/tutorial notifications. Menu options carry recipe display priority.
Do not silently replace this with all known product Defs or a single generic Bill_Production.

Read local Core XML, not finalized loaded Defs:

- `Data/Core/Defs/ThingCategoryDefs/ThingCategories.xml`: real hierarchy includes Foods / FoodMeals,
  Weapons / WeaponsMelee / WeaponsRanged, Apparel / ApparelArmor, Medicine under Manufactured and
  many mod-extensible branches. Several different categories have the label **headgear**. Therefore
  categories need identity/ancestry, and the UI must distinguish equal leaf labels. Root is not a
  useful player filter. A recipe can have several outputs/categories; first-product-only grouping
  from source UIs is not the user's required rule.
- `Data/Core/Defs/Stats/Stats_Apparel.xml`: sharp/blunt/heat armor and cold/heat insulation have
  distinct native meanings. Material multipliers are implementation fields, not substitute output
  attributes. These are local input definitions, not a measured modded final value.
- `Stats_Weapons_Ranged.xml`: close/short/medium/long accuracy and ranged cooldown are explicit item
  facts; accuracy descriptions distinguish weapon accuracy from actual shot hit chance. Hidden
  damage/penetration/range multipliers are not themselves final damage/penetration/range results.
- `Stats_Weapons_Melee.xml`: native melee DPS is expressly conditional on all attacks hitting and
  uses a specialized worker. It is not evidence for an arbitrary actual combat-result prediction.
- `Stats_Basics_General.xml` / `Stats_Basics_Special.xml`: distinct HP, mass, market value, nutrition,
  medical potency and medical quality limit. Their applicability and safe contexts remain under
  QM-Q1/NB-Q1; no worker is assumed safe merely because a StatDef exists.

Primary source documentation was already read before the three source deep dives. This original
composition does not need an invented Workshop provider. No settings/save migration or third-party
reuse permission is implied by these native/reference observations.

## Explicit proposed decisions in the original draft

1. All case-insensitive space-separated search terms match recipe/style/all-output labels across
   those fields. No initial ingredient/internal-ID search. Category ancestry and multiple-selection
   OR intersect with text/visibility; identical leaf labels are disambiguated. Other recipes retains
   unknown/uncategorized output. The catalog derives from addable recipes before text/visibility;
   a genuinely removed category clears with explanation instead of becoming an invisible filter.
2. Native Recipe order initially; filtered relevant stats only. The priority list explicitly names
   armor/insulation, weapon accuracy/cooldown, nutrition/medicine, HP/mass/value; further supported
   properties follow alphabetically. “Common” is a design choice, not player-usage telemetry.
3. No-stat Any allowed material keeps native filters and makes no material-dependent promise.
   Explicit preview and alternate Add are distinct labeled actions. An implicit inspected list choice
   follows direct stat/Use all materials changes, updating main Add too; explicit previews and pinned
   comparisons stay fixed. Use list choice resets a single-preview override, while background
   refresh still requires review before material substitution. Row click/double-click inspect;
   Compare pins a reference and subsequent selections choose/replace its partner. A removed reference
   ends comparison and keeps only a valid partner. Main Add, row Add and alternate Add each use one
   explicit variant and one native action. Right-click opens the same list as a visible alternate action.
4. Browser enablement defaults on with common inheritance; comparison enablement controls ranking,
   optimization and two-choice comparison without removing ordinary inspection/planning. No whole
   queue replacement. Fresh context resets browsing state; card/material-popup return preserves it.

These are observable proposed choices, not new user answers. The related requirements that S009–S011
left to S012 are now drafted; exact game/provider/visual implementation work remains open:

- **AB-Q1:** native entry/order/special and provider paths and minimal semantic target admission.
- **AB-Q2:** responsive layout, focus/navigation, Unicode and category-graph edge cases.
- **AB-Q3:** supported stat expansion and safe native/provider evaluation; no guessed aggregate or
  combat score, and no assumption that a known StatDef can always be evaluated safely.
- **AB-Q4:** shared-consumer costs/freshness, seven languages and reviewed-build native acceptance.

Document review/validation is tracked in the [ledger](specification-progress.md). No interview or
runtime implementation is needed to continue sequential specification; all future tasks stay unchecked.
