# S028 — source replacement XML catalog

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`).
Evidence companion to [research](2026-11-02-replace-stuff-research.md), inspected 2026-11-02.
This is the exact **source inventory**, not an executable patch, final owned grouping or compatibility claim.
All 117 source lists are included; items with an additional requirement show it in brackets.
Empty categories with callbacks can still form a source predicate group. Empty categories without
callbacks do nothing. Package IDs containing `disabled` are upstream-disabled groups, not active support.
Owned XML must separately check provider, Def and node shapes, add without duplicate membership,
and preserve unrelated entries. SoS2 Wall tagging of solar/vent entries and missing callbacks,
RimEffect's bedside Bed grouping and disabled groups require explicit admission review.

## AdaptivePrimitiveStorage.xml

Source gate: `adaptive.primitivestorage`. Source Def: `ReplaceStuffModCompat_AdaptivePrimitiveStorage`.

| Category | Exact item Defs | Callback names |
| --- | --- | --- |
| ASF_LogPile | `ASF_SmallLogPile`, `ASF_LargeLogPile` | None |
| ASF_Cellar | `ASF_StoragePit`, `ASF_CellarWood`, `ASF_CellarStone` | None |
| ASF_ClayPot | `ASF_UncoveredClayPot`, `ASF_CoveredClayPot` | None |
| ASF_WovenBasket | `ASF_WovenBasket`, `ASF_WovenBasketDouble`, `ASF_WovenBasketTriple` | None |

## ArmourRacks.xml

Source gate: `khamenman.armorracks.disabledForNewVersion`. Source Def: `ReplaceStuffModCompat_ArmourRacks`.

| Category | Exact item Defs | Callback names |
| --- | --- | --- |
| None | `ArmorRacks_ArmorRack`, `ArmorRacks_MechanizedArmorRack`, `ArmorRacks_MendingArmorRack` | `Replace_Stuff.Comps.TransferArmourRack` |

## Biotech.xml

Source gate: `Ludeon.RimWorld.Biotech`. Source Def: `ReplaceStuffModCompat_Biotech`.

| Category | Exact item Defs | Callback names |
| --- | --- | --- |
| Biotech_Deathrest_Accessories | `PsychofluidPump`, `Hemopump`, `HemogenAmplifier`, `DeathrestAccelerator`, `GlucosoidPump` | None |
| Biotech_Mech_Gestators | `MechGestator`, `LargeMechGestator` | None |
| Biotech_Mech_Rechargers | `BasicRecharger`, `StandardRecharger` | None |
| Biotech_Mech_SubcoreScanner | `SubcoreSoftscanner`, `SubcoreRipscanner` | None |
| Biotech_Mech_Antennas | `BurnoutMechlinkBooster`, `MechbandDish` | None |
| Biotech_Mech_BandNodes | `BandNode` | None |
| Biotech_Mech_Boosters | `MechBooster` | None |

## ColonyManager.xml

Source gate: `ilyvion.colonymanagerredux`. Source Def: `ReplaceStuffModCompat_ColonyManager`.

| Category | Exact item Defs | Callback names |
| --- | --- | --- |
| ColonyManagers | `CM_BasicManagerStation`, `CM_ManagerStation`, `CM_AIManager` | None |

## CombatExtended.xml

Source gate: `ceteam.combatextended`. Source Def: `ReplaceStuffModCompat_CombatExtended`.

| Category | Exact item Defs | Callback names |
| --- | --- | --- |
| AmmoBench | `AmmoBench` | None |
| AmmoBench | `AmmoElectricBench` | None |

## DeepStorage.xml

Source gate: `lwm.deepstorage`. Source Def: `ReplaceStuffModCompat_DeepStorage`.

| Category | Exact item Defs | Callback names |
| --- | --- | --- |
| Shelves | `LWM_BigShelf`, `LWM_VeryBigShelf` | None |
| LWM_Pallet | `LWM_Pallet`, `LWM_Pallet_Covered` | None |
| Shelves_Weapons | `LWM_WeaponsCabinet`, `LWM_WeaponsLocker` | None |

## DryadsGauranlenReworked.xml

Source gate: `redmattis.dryads`. Source Def: `ReplaceStuffModCompat_DryadsGauranlenRework`.

| Category | Exact item Defs | Callback names |
| --- | --- | --- |
| DryadReworked_Shrines | `Dryad_GauranlenShrine_Large`, `Dryad_GauranlenShrine_Small`, `Dryad_VikingShrine` | None |

## DubsBadHygiene.xml

Source gate: `dubwise.dubsbadhygiene`. Source Def: `ReplaceStuffModCompat_BadHiegene`.

| Category | Exact item Defs | Callback names |
| --- | --- | --- |
| Basins | `BasinStuff`, `Fountain` | None |
| Toilets | `PitLatrine`, `ToiletStuff`, `ToiletAdvStuff` | None |
| Showers | `WashBucket`, `BathtubStuff`, `ShowerSimple`, `ShowerStuff`, `ShowerAdvStuff` | None |
| WaterTowers | `WaterButt`, `WaterTowerS`, `WaterTowerL` | None |
| SewageTanks | `SewageSepticTank`, `SewageTreatment` | None |

## DubsBadHygieneLite.xml

Source gate: `dubwise.dubsbadhygiene.lite`. Source Def: `ReplaceStuffModCompat_BadHiegene`.

| Category | Exact item Defs | Callback names |
| --- | --- | --- |
| Toilets | `PitLatrine`, `ToiletStuff`, `ToiletAdvStuff` | None |
| Showers | `WashBucket`, `BathtubStuff`, `ShowerSimple`, `ShowerStuff`, `ShowerAdvStuff` | None |

## FortificationIndustrialCitadel.xml

Source gate: `aoba.fortress.industrial.citadel`. Source Def: `ReplaceStuffCompat_FI_Citadel_Walls`.

| Category | Exact item Defs | Callback names |
| --- | --- | --- |
| Wall | `FT_Stony_Palisade`, `FT_Stony_Palisade_Corner`, `FT_Stony_Palisade_Embrasures`, `FT_Stony_Palisade_CornerEmbrasures`, `FTC_CitadelWall`, `FTC_CitadelWall_Embrasure` | None |

## HigherPower.xml

Source gate: `leion247612.higherhpower`. Source Def: `ReplaceStuffModCompat_HigherPower`.

| Category | Exact item Defs | Callback names |
| --- | --- | --- |
| PoweredGenerators | `ChemfuelPoweredGenerator_II` | None |
| SolarGenerators | `SolarGenerator_II` | None |

## Ideology.xml

Source gate: `Ludeon.RimWorld.Ideology`. Source Def: `ReplaceStuffModCompat_Ideology`.

| Category | Exact item Defs | Callback names |
| --- | --- | --- |
| Lights | `Darktorch`, `DarktorchFungus` | None |

## Jewelry.xml

Source gate: `kikohi.jewelry`. Source Def: `ReplaceStuffModCompat_Jewelry`.

| Category | Exact item Defs | Callback names |
| --- | --- | --- |
| ArtTables | `LaserTableSculpting` | None |

## Odyssey.xml

Source gate: `Ludeon.RimWorld.Odyssey`. Source Def: `ReplaceStuffModCompat_Odyssey`.

| Category | Exact item Defs | Callback names |
| --- | --- | --- |
| Wall | `SmoothedSolidIce`, `SmoothedVacstone` | None |

## RimBees.xml

Source gate: `sarg.rimbees.disabledForGenetics`. Source Def: `ReplaceStuffModCompat_ArmourRacks`.

| Category | Exact item Defs | Callback names |
| --- | --- | --- |
| None | `RB_Beehouse`, `RB_ClimatizedBeehouse`, `RB_AdvancedBeehouse`, `RB_AdvancedClimatizedBeehouse`, `RB_SpacerBeehouse` | `Replace_Stuff.Comps.TransferBees` |

## RimEffect.xml

Source gate: `rimeffect.core`. Source Def: `ReplaceStuffModCompat_RimEffect`.

| Category | Exact item Defs | Callback names |
| --- | --- | --- |
| Dressers | `RE_PrefabDresser` | None |
| Lights | `RE_PrefabStandingLamp` | None |
| EndTables | `RE_PrefabEndTable`, `RE_PrefabEndTableWithLamp` | None |
| Wardrobes | `RE_PrefabWardrobe` | None |
| Columns | `RE_PrefabColumn` | None |
| PoweredGenerators | `RE_PrefabGenerator` | None |
| TVs | `RE_PrefabTelevision` | None |
| SolarGenerators | `PrefabSolarCollectors` | None |
| Bed | `RE_PrefabBedsideTable`, `RE_PrefabBedsideTableLamp` | None |

## Royalty.xml

Source gate: `Ludeon.RimWorld.Royalty`. Source Def: `ReplaceStuffModCompat_Royalty`.

| Category | Exact item Defs | Callback names |
| --- | --- | --- |
| Lights | `Brazier` | None |
| Lights | `DarklightBrazier` | None |

## SoS2.xml

Source gate: `kentington.saveourship2`. Source Def: `ReplaceStuffModCompat_SaveOurShip2`.

| Category | Exact item Defs | Callback names |
| --- | --- | --- |
| Wall | `HullFoamWall`, `Ship_Beam_Unpowered`, `Ship_Beam`, `Ship_BeamArchotech_Unpowered`, `Ship_BeamArchotech`, `ShipAirlock`, `ShipAirlockArchotech`, `Ship_Beam_Wrecked`, `Ship_Beam_Wrecked_Fake`, `Ship_Corner_OneOne`, `Ship_Corner_OneOneFlip`, `Ship_Corner_OneTwo`, `Ship_Corner_OneTwoFlip`, `Ship_Corner_OneThree`, `Ship_Corner_OneThreeFlip` | None |
| Wall | `ShipInside_SolarGenerator`, `ShipInside_SolarGeneratorArchotech` | None |
| ShipReactor | `Ship_Reactor`, `ArchotechAntimatterReactor` | None |
| ShipHeatsink | `ShipHeatsink`, `ShipHeatManifold`, `ShipHeatsinkAntiEntropy` | None |
| Holodeck | `Holodeck_Basic`, `Holodeck_Standard`, `Holodeck_AI` | None |
| ShipShield | `ShipCombatShieldGenerator`, `ArchotechShieldGenerator` | None |
| Wall | `ShipInside_PassiveVent`, `ShipInside_PassiveVentArchotech` | `Replace_Stuff.Comps.ReplaceTemperatureControl`, `Replace_Stuff.Comps.ShipVentHeatWithPower` |

## UtilityColumns.xml

Source gate: `nephlite.orbitaltradecolumn`. Source Def: `ReplaceStuffModCompat_UtilityColumns`.

| Category | Exact item Defs | Callback names |
| --- | --- | --- |
| Lights | `LightColumnMod`, `DarkColumnMod` | None |
| Sunlamps | `SunColumnMod` | None |
| Columns | `LightColumnMod`, `DarkColumnMod`, `DetColumnMod`, `FlameColumnMod`, `GraveColumnMod`, `SunColumnMod`, `IceColumnMod` | None |

## Vanilla.xml

Source gate: `Core / none`. Source Def: `ReplaceStuffModCompat_Vanilla`.

| Category | Exact item Defs | Callback names |
| --- | --- | --- |
| Wall | `SmoothedMarble`, `SmoothedGranite`, `SmoothedSandstone`, `SmoothedLimestone`, `SmoothedSlate` | None |
| Dressers | `Dresser` | None |
| EndTables | `EndTable` | None |
| Lights | `TorchLamp`, `StandingLamp`, `FloodLight` | None |
| WallLights | `TorchWallLamp`, `WallLamp` | None |
| ResearchBenches | `SimpleResearchBench`, `HiTechResearchBench` | None |
| Sunlamps | `SunLamp` | None |
| Columns | `Column` | None |
| Wall | `Column` | None |
| PoweredGenerators | `WoodFiredGenerator`, `ChemfuelPoweredGenerator` | None |
| TVs | `TubeTelevision`, `FlatscreenTelevision`, `MegascreenTelevision` | None |
| SolarGenerators | `SolarGenerator` | None |
| PowerConduits | `PowerConduit`, `WaterproofConduit` | None |
| ArtTables | `TableSculpting` | None |
| Smelters | `ElectricSmelter` | None |
| Smithys | `FueledSmithy`, `ElectricSmithy` | None |
| Stonecutters | `TableStonecutter` | None |
| Butchers | `TableButcher` | None |
| DrugLab | `DrugLab` | None |
| MachiningTables | `TableMachining` | None |
| Stoves | `FueledStove`, `ElectricStove` | None |
| TailoringBenches | `HandTailoringBench`, `ElectricTailoringBench` | None |
| Fabricators | `FabricationBench` | None |
| Campfire | `Campfire` | None |
| Crematoriums | `ElectricCrematorium` | None |
| Refineries | `BiofuelRefinery` | None |
| Shelves | `Shelf` | None |

## VanillaExpandedBooks.xml

Source gate: `vanillaexpanded.vbookse`. Source Def: `ReplaceStuffModCompat_VanillaExpandedBooks`.

| Category | Exact item Defs | Callback names |
| --- | --- | --- |
| VBE_WritersTables | `VBE_WritersTable`, `VBE_TypewritersTable` | None |

## VanillaExpandedHelixienGas.xml

Source gate: `vanillaexpanded.helixiengas`. Source Def: `ReplaceStuffModCompat_VanillaExpandedHelixien`.

| Category | Exact item Defs | Callback names |
| --- | --- | --- |
| Lights | `VHGE_GasLamp`, `VHGE_GasFloodlight` | None |
| Sunlamps | `VHGE_GasSunLamp` | None |
| PoweredGenerators | `VHGE_HelixienGenerator`, `VHGE_IndustrialHelixienGenerator` [VanillaExpanded.VFEPower] | None |
| Smelters | `VHGE_GasPoweredSmelter` | None |
| Refineries | `VHGE_GasPoweredRefinery` | None |
| Crematoriums | `VHGE_GasPoweredCrematorium` | None |
| Smithys | `VHGE_GasPoweredSmithy` | None |
| Stoves | `VHGE_GasPoweredStove` | None |

## VanillaExpandedPower.xml

Source gate: `vanillaexpanded.vfepower`. Source Def: `ReplaceStuffModCompat_VanillaExpandedPower`.

| Category | Exact item Defs | Callback names |
| --- | --- | --- |
| PoweredGenerators | `VFE_IndustrialWoodFiredGenerator`, `VFE_IndustrialChemfuelPoweredGenerator` | None |
| SolarGenerators | `VFE_AdvancedSolarGenerator` | None |

## VanillaExpandedSecurity.xml

Source gate: `vanillaexpanded.vfesecurity.disabledForRemake`. Source Def: `ReplaceStuffModCompat_VanillaExpandedSecurity`.

| Category | Exact item Defs | Callback names |
| --- | --- | --- |
| None | `Sandbags`, `VFES_BarbedWire`, `Barricade`, `VFES_DeployableBarrier` | None |
| None | `Turret_MiniTurret`, `VFES_Turret_FloorTurret`, `VFES_Turret_MilitaryTurret`, `VFES_Turret_Searchlight`, `VFES_Turret_Flame`, `VFES_Turret_SentryGun`, `VFES_Turret_ChargeTurret` | None |
| None | `TrapSpike`, `VFES_BearTrap` | None |
| None | `VFES_Turret_Ballista`, `Turret_Autocannon`, `Turret_Sniper`, `VFES_Turret_AutocannonDouble`, `VFES_Turret_ChargeRailgunTurret`, `VFES_Turret_EMPTurret` | None |
| None | `VFES_ShieldGenerator_Small`, `VFES_ShieldGenerator_Large` | None |

## VanillaExpandedSpacer.xml

Source gate: `vanillaexpanded.vfespacer`. Source Def: `ReplaceStuffModCompat_VanillaExpandedSpacer`.

| Category | Exact item Defs | Callback names |
| --- | --- | --- |
| Dressers | `Table_IlluminatedDresser` | None |
| EndTables | `Table_IlluminatedEndTable` | None |
| Lights | `Spacer_OutdoorLamp` | None |
| Chair | `VFES_ModernCouch` | None |
| TVs | `UltrascreenTV` | None |

## VanillaFurnitureExpanded.xml

Source gate: `vanillaexpanded.vfecore`. Source Def: `ReplaceStuffModCompat_VanillaExpandedFurniture`.

| Category | Exact item Defs | Callback names |
| --- | --- | --- |
| Dressers | `Table_RoyalDresser` | None |
| EndTables | `Table_RoyalEndTable` | None |
| Wardrobes | `Table_Wardrobe` | None |
| Lights | `Light_Streetlamp`, `VFE_RoyalLamp` | None |
| Campfire | `Stone_Campfire` | None |
| Radio | `Radio_Industrial`, `Radio_Spacer` | None |

## VanillaFurnitureProduction.xml

Source gate: `vanillaexpanded.vfeproduction`. Source Def: `ReplaceStuffModCompat_VanillaFurnitureProduction`.

| Category | Exact item Defs | Callback names |
| --- | --- | --- |
| Smelters | `VFE_FueledSmelter` | None |
| Stonecutters | `VFE_TableStonecutterElectric` | None |
| Butchers | `VFE_TableButcherElectric` | None |
| DrugLab | `VFE_TableDrugLabElectric` | None |
| Smithys | `VFE_TableSmithyLarge` | None |
| MachiningTables | `VFE_TableMachiningLarge` | None |
| Stoves | `VFE_TableStoveLarge` | None |
| TailoringBenches | `VFE_TableTailorLarge` | None |
| Fabricators | `VFE_ComponentFabricationBench` | None |

The separate `OverWallCoolerVent.xml` defines the four wall devices and two dropdown groups;
its behavior/cost inventory is in research. Root and unique versioned patches add related build
commands and the Replace Stuff designator. None of these source files have been copied into the
product, and no new compatibility is accepted by this catalog.
