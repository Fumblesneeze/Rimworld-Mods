# [DN] Blunt Social Fights research — S037

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, target **1.7 beta**. Inspected
2026-11-03 read-only. This document records evidence; no behavior is implemented or accepted in game.

## Documentation first and pinned identity

The [Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=2886174990)
describes preventing sharp attacks during social fights, with a Bloodlust exception. It identifies
the work as a commission for Raccoononi and links the author's repository. The visible tags and
installed About advertise **1.3–1.6**, not 1.7. Both attempts to read the changelog endpoint failed;
no uninspected changelog claims are used. User comments are not compatibility proof.

The [author repository](https://github.com/Danimineiro/Blunt-Social-Fighting) has no README or prose
behavior guide beyond metadata. Pinned commit **`e8e2291881d336d35673f603fb1acc885a193638`**, dated
2025-07-19, is the 1.6 update merge. Read its sole runtime source file,
[`Source/BluntSocialFightingPatcher.cs`](https://github.com/Danimineiro/Blunt-Social-Fighting/blob/e8e2291881d336d35673f603fb1acc885a193638/Source/BluntSocialFightingPatcher.cs),
project, About and license. The 1.6 DLL in that checkout is byte-identical to the installed one.

- Title **[DN] Blunt Social Fights**, author **Danimineiro**, package **`dani.BluntSocialFighting`**,
  Workshop **2886174990**.
- Installed root `F:/Steam/steamapps/workshop/content/294100/2886174990`; latest supplied binary
  `1.6/Assemblies/Blunt Social Fighting.dll`, assembly/file/product version **0.0.0.0**. No supplied
  1.7 folder or LoadFolders.xml. This is the latest reference, not a verified beta-selected binary.
- Required Harmony; loadAfter Core, Royalty, Ideology, Biotech and Harmony. DLC load hints do not
  establish DLC dependencies; the code uses Core trait/state/damage identities without a DLC gate.
- Installed About says mental break, which is broader than the actual code and Workshop description.
  The code tests **only exact SocialFighting state**, not Berserk or all mental states.
- MIT license, copyright **2024 Dani**, present locally and publicly. Any copied/substantial derived
  source would require retaining its notice; this work independently specifies behavior and copies
  no implementation or assets into the product. No upstream DLL is a release input.

| Input | SHA-256 |
| --- | --- |
| Installed About/About.xml | `08A10C1C6B76641B4A7D6B510683F161453CAD044E0850CCC4E053EE755EB9BF` |
| Installed LICENSE | `8EA18A05E952C0F8A360909E9AC10923D19787639B84BA1F1244D8035481F09E` |
| Installed/pinned 1.6 DLL | `7D2CD74E3D64015E6541F625824FB603D2FEF0927CFF78E5095ACA7ED53C2D04` |
| Pinned sole runtime C# source | `AF0C808387B9230D90914386BDCAF88C9419917AC3C3AA9017127FD034D6DEB9` |
| Native Assembly-CSharp.dll, 1.7.4995 rev309 | `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21` |

Evidence is under `artifacts/Refinement/megamod-spec-20261103/blunt-social-fights/`: `upstream`
is the pinned public checkout; `compiled` is read-only installed decompilation with ILSpy
11.2.0.10050; `native` contains eleven inspected game types. These hashes identify evidence,
not runtime admission rules. No source assembly or game process was executed for this audit.

## Complete source inventory and truth table

There is **one runtime class, one patch and no settings or game-state serialization**. The source
project targets net481 with reference package 1.6.4503-beta and Harmony 2.3.6. Its static startup
constructor installs an unguarded postfix on `Verse.Verb.Available` using the source package as
Harmony owner, then logs a colored success message. The owned process must replace that startup
pattern with semantic admission and accurate failure diagnostics.

`BluntSocialFightingPatcher.IsAvailablePatch(Verb, ref bool)` gets CasterPawn. If there is no pawn,
or the pawn is outside exact SocialFighting, or native HasTrait(Bloodlust) returns true, it leaves
the result alone. Otherwise it ANDs the existing result with the primary damage's armor category
being different from Sharp. The debug conditional source logs classifications; that diagnostic
block is absent from the inspected installed body.

| Context | Source result |
| --- | --- |
| Result already false | Remains false; never enables an attack |
| Non-pawn caster | Unchanged |
| Any pawn outside exact SocialFighting | Unchanged, including other mental states |
| Social-fighting attacker with active Bloodlust | Unchanged; not guaranteed to select or land a sharp attack |
| Social-fighting attacker without active Bloodlust, primary category Sharp | False |
| Same attacker, primary category not Sharp | Unchanged, including Heat or other categories |
| Same attacker, valid lookup returns no primary DamageDef or no armor category | Unchanged; null is not classified as Sharp |
| Pawn without a story/trait record | No Bloodlust exemption; the state/damage rule still applies |

No faction/colonist, opponent-trait, weapon-name or race whitelist exists. The exemption is checked
for the **attacker**, separately for each fighter. Native 1.7 TraitSet.HasTrait excludes suppressed
traits, so a suppressed Bloodlust trait does not grant the exception. A hostile/non-colonist pawn
that actually enters the applicable native state receives the same rule; this does not newly grant
any pawn access to that state.

The source filters primary damage classification. It does not rewrite damage, blunt a sharp attack,
heal existing wounds, remove bleed, clamp injuries, prohibit every non-blunt category or guarantee
survival. Tool extra damage, gene/hediff effects, environmental harm and other independent damage
are not globally suppressed. That boundary matters: a Blunt-category attack may still create a
cut-like injury or carry secondary effects. No stronger nonlethal-combat claim is supported.

## Native 1.7 path inspected

| Type / relevant members | Evidence and implication |
| --- | --- |
| Verse.Verb: Available, IsStillUsableBy, TryCastNextBurstShot | Available checks fuel, apparel-owner permission and equipment role restrictions. IsStillUsableBy calls it before ownership/damage-factor checks; casting checks it again before TryCastShot. AND-only filtering preserves existing denial. Virtual overrides/changed effective call chains still require admission. |
| Verse.VerbProperties | Identifies melee/projectile classes and primary meleeDamageDef; classification must follow actual attack data, not a translated label or weapon name. |
| Verse.VerbUtility.GetDamageDef | Uses projectile DamageDef for projectile verbs, otherwise primary meleeDamageDef. It does not enumerate extra melee damages. |
| RimWorld.SocialInteractionUtility.HasAnyVerbForSocialFight / TryGetRandomVerbForSocialFight | Uses pawn.verbTracker.AllVerbs, melee predicate and IsStillUsableBy. Weighted choice uses adjusted melee damage multiplied by tool chanceFactor or one. Removing sharp candidates retains ordinary relative weights among survivors. This is not the full equipped-weapon pool used by ordinary combat. |
| RimWorld.JobGiver_SocialFighting | Requires usable participant/target context, uses that selection, returns no job if none qualifies; otherwise SocialFight job with one melee attack and selected verb. No invented fallback punch. |
| RimWorld.Pawn_MeleeVerbs | Explicit social verb is checked for usability before casting. General melee selection includes other sources and uses a different selection model; do not replace it globally with the social pool. |
| RimWorld.Verb_MeleeAttack | Native hit/dodge, XP, sounds, battle feedback and threat handling remain. It inherits base Available in the inspected body. |
| RimWorld.Verb_MeleeAttackDamage | Primary damage plus optional tool/surprise extras flow into TakeDamage. Native random damage/armor/injury logic remains; primary category filtering is not a global damage interceptor. |
| Verse.AI.MentalState_SocialFighting | Tracks partner; ends on partner despawn/death/downing or no longer reciprocating, otherwise uses native base recovery. PostEnd stops jobs/threat and applies native outcome thoughts/notification; serializes partner. |
| RimWorld.Pawn_InteractionsTracker | Native initiation, participant checks, notification and tale remain. HasAnyVerb checks before SocialFighting is entered do not preemptively remove sharp attacks, so a sharp-only pawn can still enter and later have no permitted attack. |
| RimWorld.TraitSet.HasTrait | Matches trait identity only when not suppressed. No string/label matching. |

Read relevant Core XML: SocialFighting in `MentalStates_Special.xml`, Bloodlust in
`Traits_Singular.xml`, and Cut/Stab/Bite/Blunt/Crush in `Damages_MeleeWeapon.xml`. The inspected
native state has minimum recovery ticks 420 and recovery MTB 0.02 days; Bloodlust's fight-chance
factor is four. These are **native reference values to preserve**, not new owned balance constants.
Their final loaded values and other mods' changes are not established by reading raw XML.

In the no-permitted-attack case, retain ordinary no-job/recovery behavior rather than granting a
new attack, forcing an immediate recovery or spinning through repeated random picks. Filtering
does not itself cancel the social state or guarantee a particular fight duration. State/trait
changes are evaluated when attacks are considered and before execution; no new persistent cache
is needed. Native save state supplies current state/trait/partner after load.

## UI, contracts and integrations

The source contains no Mod subclass/settings UI, gizmo, bottom tab, overlay, Def, XML patch or
custom saved data. Its only added presentation is startup logging. The owned feature adds no new
player control or configurable parameter; native combat logs, Health and mental-state display show
consequences. No common-setting inheritance exception is needed because no setting is introduced.

A bounded installed C#/XML/JSON search for package/type identifiers found the source About and
RimPy Mod Manager Database (`1847679158/db/db.json`). The latter is a Workshop/package catalogue
entry with stale 1.3/1.4 metadata, not a demonstrated callable API consumer. No external invocation
of public IsAvailablePatch was found; binary/reflection consumers are not excluded by this scan.
The source exposes no discovered saved type or Def that requires migration. Do not impersonate
its assembly/package or claim all third-party bindings are satisfied.

Other combat/attack providers may override Available, skip the native social selection or add
secondary damage. Those are specific integration boundaries, not evidence that every such mod
conflicts. Resolve canonical active packages and their actually consumed shapes before claims.
With original `dani.BluntSocialFighting` active, suppress the owned duplicate rule with a startup
warning rather than unpatching or modifying the original. A downloaded inactive copy has no effect.

Selected owned social/medical/trait features compose directly. Nothing here changes S008 finishing,
custom item melee destruction, toddler social eligibility, treatment or non-social fighting rules.
No shared Thing list is needed for a decision from one attacker and one attack; adding such a cache
would contradict the consolidation goal rather than satisfy it.

## Proposed contract and remaining evidence

The [specification](../../../../openspec/changes/specify-megamod-blunt-social-fights/specs/megamod-blunt-social-fights/spec.md)
keeps source intent and narrow semantics. The [design](../../../../openspec/changes/specify-megamod-blunt-social-fights/design.md)
defines minimum guarded integration and verification; no target is yet admitted.

- **SF-Q1:** effective native selection/use/admission and override/repatch handling, including empty
  eligible pools, null classifications and active/suppressed trait transitions.
- **SF-Q2:** actual optional combat/race/gene consumers and callable contracts; exact original
  provider activation and separately verified supported-provider paths.
- **SF-Q3:** focused tests, reviewed build, package isolation and native causal fight/attack/Health/
  recovery/save-load acceptance. Verify surviving blunt lethality and secondary-damage boundaries.

Specification refinement does not prove beta compatibility. No live game, build, installation,
normal configuration/save edits or Workshop modifications occurred.
