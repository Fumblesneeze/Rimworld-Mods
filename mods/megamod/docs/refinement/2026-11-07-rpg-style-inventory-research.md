# S132 — RPG Style Inventory Revamped research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin replacement; inspected2026-11-06–07. [Coverage](../replacements/rpg-style-inventory.md),
[change](../../../../openspec/changes/specify-megamod-gear-presentation/). Specification only.

## Documentation first and provenance

Read the [author's Steam description](https://steamcommunity.com/sharedfiles/filedetails/?id=2478833213)
before compiled inspection. V4.1 credits avilmask's revamp of Sandy's original. It describes XML slots,
empty-space compaction/anchors, fallback items, per-race simplified mode, backgrounds, quality borders,
condition bars and ghost indications for multi-slot apparel. It calls the UI cosmetic and says it
adds no new actions; compiled code nevertheless supplies force/drop menus and substantial optional
CE actions. Those are existing gameplay concepts, but preserving their current rules needs analysis.
RWoM, CE (explicitly limited testing), sidearms and a multiplayer addon are advertised, not verified.

The [linked public repository](https://github.com/catgirlfighter/-1.0-RPG-Style-Inventory-V3.0-)
root and [README](https://github.com/catgirlfighter/-1.0-RPG-Style-Inventory-V3.0-/blob/master/README.md)
were readable. The README only summarizes Gear/taint/forced display. Root lists1.2–1.6, Source,
ModIntegrations, assets and change.log; source subtree/API/direct retrieval failed. No public commit
or full source snapshot was pinned. No license found in the installed inventory or visible root;
this is not a claim of universal absence. Independently implement; no copied assemblies/code/art.

Installed root `F:/Steam/steamapps/workshop/content/294100/2478833213`, package
`Sandy.RPGStyleInventory.avilmask.Revamped`; About credits Sandy, description avilmask, Workshop avil.
No modVersion or LoadFolders found. Metadata advertises1.2–1.6, load-after CE and incompatibility
with original `Sandy.RPGStyleInventory`. Selected latest available1.6 binaries both version1.0.0.0;
this is not proof that the beta selects/loads them. Source bundles0Harmony.dll despite no declared
Harmony dependency. Never copy it. Local languages: English, French, Czech, Russian,
ChineseSimplified, ChineseTraditional. Owned feature still requires all seven repository languages.

| Input, relative to installed root unless absolute | Bytes | SHA256 |
| --- | ---: | --- |
| About/About.xml | 636 | `a7f2df1349d76bfe1a00b69b0c54b9112b58853e06a6dc95943fca93db12b565` |
| 1.6/Assemblies/Sandy_Detailed_RPG_Inventory.dll | 54272 | `cf30287e5a95597866690b27892097e8fd9ba8762a55418e9f4a3080ea96d143` |
| 1.6/Assemblies/CEPatches.dll | 28672 | `7c8a2193a917d8dee033270c50284dce830449ec3f0ed4c6b9715032d62024e5` |
| 1.6/Assemblies/0Harmony.dll, inspection only | 2195456 | `498ede1d20a87afaa6c7b1ed5b3beb505b01dc418b952c1be5c322204404b033` |
| 1.6/Defs/ItemSlotDefs.xml | 15038 | `c6699b5d9a1d13a41270208006a0fd588147fc0d29b4d46d1ed6e494b4706fe8` |
| Native Assembly-CSharp.dll,1.7.5000 rev1275 | 15636480 | `d55aa8e47fa3c5463ca85b0eccd51f195b4bbcd04f56da3af21c2f6688a81254` |
| Native UnityStubs.dll | 1024512 | `ea1dfbaea8d21cf838efe30b1417949843372908d14ba070aa4c192db8da2714` |
| Fortified3498575851/1.7/Assemblies/Fortified.dll | 1391104 | `eb7b2a3af12257c9868bf37b8dccbf92e63cc352e9717f7730ac61ab655723b9` |

Complete main assembly, CE adapter, selected XML and English keys were read; consumer inspection is
bounded to relevant paths. All11 source-input hashes plus4 native/Fortified hashes are retained in
`artifacts/Refinement/megamod-spec-20261106/rpg-style-inventory/`. Native Gear extract is retained
under the adjacent `quality-colors/native/` evidence for the same exact beta assembly.

## Complete behavior inventory

| Source surface | Inspected behavior | Owned disposition |
| --- | --- | --- |
| Sandy_Detailed_RPG_GearTab.FillTab | Humanlike classic slots/portrait/stats, Simple apparel grid or Show list; nonhumanlike list; inventory below | Three layouts in existing Gear; preserve native subject visibility, complete scroll access and safe fallback |
| Primary/secondary/fallback gear | Primary weapon tile, first additional equipment tile, remaining equipment and unmatched apparel in lists | Every physical item once; secondary is presentation, not a new equip slot |
| Multi-slot apparel | First matched location has actual item; later coverage slots get dim ghost icons | Secondary coverage indicators, never duplicate quantity or extra action targets |
| Portrait | South initially, rotation button, hats hidden initially, clothes shown | Inspection only; current view controls cannot alter the pawn or wear policy |
| DrawSlot/DrawSlotIcons | Quality frame, HP background or bar, info tooltip, forced/locked/tainted/brawler warning, optional enchantment | Shared palette plus separate readable condition/status/cargo meanings |
| PopupMenu and DrawThingRow | Info; controllable pawn force/unforce apparel and drop; list supports ingest; lock/lodger rules | Current native/provider actions, confirmations and action-time checks rather than old copied checks |
| DrawStats/DrawStats1 | Mass/capacity, temperature, sharp/blunt/heat summaries; icon layout additionally vacuum if relevant | Same applicable facts in every layout; truthful units and zero/unknown handling |
| Sandy_RPG_Settings / mod settings | Width/height, fit, empty backgrounds, temp line, HP bar, quality frames/colors, race simplified map | Common scoped settings; one shared quality editor; current view switches relocated |
| ItemSlotDef/Slots | XML layout, priority/coverage matching, compaction, fallback | Declarative presentation extension; malformed/unknown layout cannot hide real items |
| RWoMIntegration | Enchantment tooltip/icon and ArmorRating_Alignment summary when package active | Optional isolated adapter with actual content/API admission; no magic mechanics added |
| CEPatches/CEAccess | Weight/bulk, CE armor and menus, held-item/loadout interactions | Direct narrow provider integration; retain native provider tab if essential actions cannot be preserved |
| Dev gear option button | Native PawnGearDevOptions under developer mode | Move to top debug toolbar; no ordinary Gear override button |

### Layout algorithm and catalogue

`ItemSlotDef` has xPos/yPos (default int.MinValue), validationOrder, placeholder, apparelLayers,
bodyPartGroups, anchor and Default, plus mutable runtime listid/hidden. Each layer/group list matches
any listed entry; an empty list is a wildcard. Layer and body-group conditions combine. Coverage
uses `GetInterferingBodyPartGroups(BodyDefOf.Human)`, making nonstandard bodies an explicit gap.
First valid match wins the real tile; later matches/coverage receive ghosts. Source caches all
apparel Def mappings statically, rather than each pawn's actual anatomy and currently worn items.

There are42 layout entries:34 real slots and8 placeholders in a7×6 catalogue. Real groupings cover
shield; torso skin/shell/belt/underwear/middle; head/eyes/teeth/jaw/ears; neck; waist; legs; feet;
hands/right/left/shoulders. Six Default slots are torso skin/shell/middle, head, waist belt and
legs skin. Eight placeholders reserve5 magic cloak/artifact and3 CE webbing/backpack/strapped-head
positions. Full exact coordinate/priority/anchor inventory is retained in local `slot-catalogue.json`.
Source also defines Accessories350, Shield600, Underwear-25, UnderwearTop-10, VFEC_OuterShell500
apparel layers and Ears group195. These definitions do not themselves equip anything.

Slots.MakePreps sorts validation priority, fills coordinates and replaces placeholders with real
entries, hiding only the replaced placeholder. Other coordinate overlaps merely warn. It removes unused rows/columns and moves
anchors toward usable space. Display all includes declared unused slots, not arbitrary undefined
holes. Source dimension allocation accepts raw coordinates; missing/huge values need validation.
Rendering collisions can append the same item to fallback more than once or obscure it; Simple
assumes body-group lists/positive wrap width. Nonhuman list returns before scroll-height update.
Owned layout uses the actual subject, deduplicates real items, bounds malformed layout handling,
always exposes unmatched items and keeps geometry/compaction separate from shared Defs.

XML source replaces only exact `ITab_Pawn_Gear` entries under ThingDefs with thingClass Pawn;
CE similarly replaces `ITab_Inventory`. It does not replace the entire inspectorTabs list. CE and
RWoM slot additions check layer existence, not canonical package identity. Owned declarations require
XML Extensions content checks and external package checks, preserve unrelated entries, and leave
unknown custom tabs alone. No new gameplay apparel layers/body groups merely to make a UI slot fit.

### Item appearance and action correctness

Quality borders use seven source colors matching S131's RPG palette; S131's unified default is
instead **Default**. This is an intentional composition choice, with RPG still selectable in the
shared editor. Condition is separate: source bar red below20%, yellow below50%, green at100%,
otherwise teal(.2,.8,.85). At20% it is yellow; at50% teal. Background mode fills by remaining HP
and switches its tattered appearance at or below50%. Those are display thresholds, not changes to
the game's tattered-apparel thought. Source bar disappears during hover because of an early return;
owned bar remains visible. HP-less/zero-maximum items must not produce invented HP or division errors.

Ghosts are gray/half-opacity and noninteractive. Forced/locked/tainted and brawler-ranged warnings
are inspection facts even for a pawn the player cannot command; source gates many icons on control.
An ordinary item tooltip retains label, details, mass and HP. Restoring GUI state is required.

Source InterfaceDrop uses native RemoveApparel/DropEquipment jobs or direct inventory TryDrop.
Its old cloned row/control logic lacks current CarriedBy restrictions, kind.destroyGearOnDrop and
Biotech bandwidth confirmation; its food eligibility is older than
FoodUtility.WillIngestFromInventoryNow. Native1.7 retains an obsolete private InterfaceIngest in
the base Gear class, so absence in the derived source class alone does not prove a broken binding.
Owned UI consumes current action paths/authority, revalidates exact pawn/item/holder on click, and
does not grant control because an item tile exists. Inspection of corpses/animals/hostiles and
provider pawns must respect current native/provider visibility and permission decisions.

### Stats and shared meaning

Mass is current GearAndInventoryMass and Capacity, not an enlarged private hauling allowance.
Temperature is current comfortable min/max; source shows vacuum only in icon layout. Owned layouts
show the same applicable information and clearly distinguish unavailable from zero.

Native overall armor is coverage-weighted and capped: per part, combine normalized capped natural
and apparel ratings through remaining-penetration factors, then weight coverage. Source tooltips
instead add individual ratings for selected outer body parts; that sum is not a combat probability.
[S075 UA04](../../../../openspec/changes/specify-megamod-uncapped-armor/specs/megamod-uncapped-armor/spec.md)
explicitly preserves the native capped summary while allowing individual ratings above200%.
Owned Gear must label individual contributions and summary distinctly, never uncap the summary
as a cosmetic fix or claim summed layer values guarantee protection.

S021/S126 cargo classification, S062 personal supplies, S123 meals, S124 drug-response contributions,
S127 HUD selection and S130 apparel policy all remain authoritative in their own modules. This
view reads them directly; no second item scanner, target dictionary, medicine job or carrying limit.
Personal-supply controls remain pawn orders and show existing effective targets/contributors.

## Defaults and persistence decisions

| Preference | Source default/evidence | Owned selected default |
| --- | --- | --- |
| Size | width706, height500 | Same preferred UI size, fit within current viewport |
| Fit | Cycles recommended tight/wide widths; rendering can mutate global width | Explicit Tight/Wide fit choices in scoped settings; rendering never writes a preference |
| Layout | Classic; global per-race simplified false; Show list transient | Classic default, per-race default collection; current-play override via bottom-right control |
| Show hats | Transient false; rotation south | Hidden initially; view-only toggle and portrait rotation |
| All declared slots | false | false |
| Empty backgrounds | displayBG=true; static/default backgrounds=true | Both true; dormant subordinate choice retained |
| One-line temperatures | initializerfalse, Scribe missing-key defaulttrue | true, explicit choice using serialized default |
| HP bar | initializerfalse, Scribe missing-key defaulttrue | true, explicit choice using serialized default |
| Quality frame / thick frame | true / false | true / false; S131 shared palette |

Default layout and per-race defaults are presentation preferences (race types, not pawn orders).
Use common global/save rows, retaining dormant overrides. Use global stays operable in a loaded
save while the save-value editor is disabled during inheritance. At the main menu both save-value
and Use global controls/callbacks are disabled. Current layout/hat switches are transient per-game
view state, reset on game change; they do not silently write settings. Default layout changes apply
when no temporary override exists, with a return-to-default choice. Per-race settings default to
the common Classic default unless explicitly selected; they cannot alter anatomy/equip restrictions.
Source's per-Def-name `_simplifiedView` globals and color keys are not automatically imported.

The source picker writes on Close/PostClose, silently ignores parse failure and repeatedly creates
textures; it is not a second owned editor. Reuse S131's transaction, cancel, validation, opacity,
scope checks and shared palette. Missing/invalid owned values get documented built-in fallback.

## Optional providers and actual consumers

| Provider or consumer | Inspected contract | Decision / limit |
| --- | --- | --- |
| CE adapter shipped by RPG | Package gates ceteam.combatextended and sandy.rpgstyleinventory.avilmask.revamped; reflection into RPG stats/menu/rows/FillTab; CEAccess exposes loadouts, weight/bulk, switching, armor and held-state methods | Preserve useful outcomes via isolated provider adapter; no internal patches against owned Gear |
| RWoM | torann.arimworldofmagic; CompEnchantedItem.HasEnchantment, TM_Calc.GetEnchantmentsString, ArmorRating_Alignment; optional TM_Cloak/TM_Artifact slots | Package+content+semantic shape admission; absent provider has no fake stats/slots |
| Fortified Features Framework, AOBA.Framework,3498575851 | RPG CanControlColonist postfix: if false, player IWeaponUsable or draftable cached drone becomes controllable. Parallel postfix targets native ITab_Pawn_Gear.CanControlColonist | Consume admitted current native/provider authority. No need to impersonate RPG class simply to retain this permission; exact beta/provider native workflow still required |
| Smart Medicine Continued, Memegoddess.SmartMedicine,3526251680 | PatchRPG uses short TypeByName Sandy_Detailed_RPG_GearTab then patches DrawThingRow, InterfaceDrop and FillTab, catches/unpatches its group on exception | Legacy reflection binding not promised. Owned S062 controls compose directly into all owned layouts; source co-install remains S062 overlap policy |
| Layout XML extensions | Public ItemSlotDef fields and enum anchors; source Def names | Preserve useful declarative layout semantics in owned extension, not an unverified legacy assembly/type alias. Existing external XML referring to Sandy type needs an explicit port/adapter; no drop-in claim |
| Sidearms / multiplayer addon claims | Advertised by author; no inspected current binding proving support | Do not claim support from the page; RPG-Q3 requires concrete provider/consumer evidence |

CE adapter source replaces stats with weight/bulk, sharp mmRHA, blunt MPa and heat percentage;
it has a HumanBodyCoverage aggregate, optional PartialStat reflection and natural-armor coverage.
Menus include equip/put-away, apparel force, reload from inventory, ingest, drop, drop-and-haul,
forget held items and generate/manage loadout from current inventory/equipment. Item eligibility,
biocoding, violence, manipulation, lodger restrictions and actual provider job paths must survive.
Source put-away uses Primary rather than selected equipment; held tracking can be cleared before
a failed drop; inventory right-click receives the wrong inventory flag; its drop-and-haul only
actually requests hauling on the apparel branch. Do not turn those defects/labels into promises.
Owned menu offers hauling only through an actual supported haul path; otherwise ordinary Drop.
Loadout creation must use a real provider transaction, without half-created/assigned records.

No actual installed CE/RWoM package was found by canonical About metadata search in Workshop/local
Mods at this checkpoint. A file named CombatExtended.dll under3642977893 belongs to **Bill Doors'
Framework -1.6 Temporary**, package3HSTltd.Framework; it is not evidence of installed CE itself.
Another DLL under3066208882/Source is a development reference. Provider APIs used by RPG are
documented from the adapter, not certified against a currently installed CE/RWoM release.

Bounded installed XML scan for RPG class/package identifiers outside the source found no consumer.
ASCII DLL identifier scan found current Fortified and historical VFECore1.0–1.2 assemblies; old
VEF is not a selected beta consumer. This does not prove absence of dynamically generated names,
Unicode strings or remote mods. Smart Medicine is a known consumer from S062 and was read directly.
Fortified native target was additionally confirmed from IL custom-attribute metadata, retained in
`fortified-il/`. No blanket binary compatibility or source-ID spoofing is selected.

## UI audit, fallback and implementation boundaries

Source adds an inspector tab replacement, no new bottom main tab. Owned Gear remains contextual.
Bottom-right compact view control selects Classic/Simple/List and hat visibility while Gear is
applicable; portrait rotation stays a direct inspection action. Settings and race defaults live in
Interface / Gear presentation, palette in Interface / Quality colors. Debug equipment options go
to the top toolbar and disappear outside developer mode. Supply orders/loadouts stay with the pawn.

Original or Revamped RPG active → retain its UI, withhold overlapping owned tab/layout contribution
and warn. Do not silently remove other mods, edit their settings or patch their own package files.
Unknown essential provider action/permission contract → preserve native/provider Gear, not a
partially functional replacement. Optional cosmetic decorations can fail independently. Every hook
needs an indexed target spec, executable effective/incoming semantic check and declared atomic group.
Benign other patches allowed; false/unknown/exception/install failure restores owned group only,
bounded startup warning identifies missing behavior/fallback. XML checks package and actual targets;
merge lists, replace only the expressly intended tab entry and never overwrite the inspector list.

No gameplay stats, research, costs, equipment capacity, body coverage, supply targets or combat
rules are rebalanced here. Native actions remain real jobs/transfers, not instant fabricated results.
All runtime/asset/native evidence remains future work. No art selected, generated or copied here.

## Open research and acceptance gates

- RPG-Q1: source inventory resolved by this research and owned observable draft.
- RPG-Q2: exact1.7 effective tab/action/provider admission, preserving fallback when XML and runtime
  admission occur at different phases; malformed layout, UI scales and fresh-save lifecycle evidence.
- RPG-Q3: CE/RWoM/sidearms/multiplayer and external XML binding evidence; public source/license;
  no silent expansion from advertised support to accepted compatibility.
- RPG-Q4: direct owned cargo/medicine/meal/drug/palette/HUD composition and exact native supply actions;
  preserve S075 summary distinction and S130 policy authority.
- RPG-Q5: seven-language/settings/portrait/layout/action accessibility, performance and reviewed-build
  native acceptance, including real drops/ingestion, locked/bandwidth restrictions and provider pawns.
