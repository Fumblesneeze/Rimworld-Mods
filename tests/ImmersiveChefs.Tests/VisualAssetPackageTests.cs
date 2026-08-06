using System.Drawing;
using System.IO;
using System.Xml.Linq;
using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class VisualAssetPackageTests
{
    private const string CookwareTexturePath =
        "ImmersiveChefs/Things/Item/Kitchenware/Cookware/Cookware";
    private const string PlateTexturePath =
        "ImmersiveChefs/Things/Item/Kitchenware/Plate/Plate";
    private const string CutleryTexturePath =
        "ImmersiveChefs/Things/Item/Kitchenware/Cutlery/Cutlery";
    private const string ChefsKnifeTexturePath =
        "ImmersiveChefs/Things/Item/Kitchenware/ChefsKnife/ChefsKnife";
    private const string GlitterCookwareTexturePath =
        "ImmersiveChefs/Things/Item/Kitchenware/GlitterCookware/GlitterCookware";
    private const string PreparedFoodTexturePath =
        "ImmersiveChefs/Things/Item/Food/PreparedIngredients/PreparedIngredients";
    private const string DishwasherTexturePath =
        "ImmersiveChefs/Things/Building/Dishwasher/Dishwasher";
    private const string IndustrialDishwasherTexturePath =
        "ImmersiveChefs/Things/Building/Dishwasher/IndustrialDishwasher";
    private const string PrepStationTexturePath =
        "ImmersiveChefs/Things/Building/KitchenStation/PrepStation";
    private const string SauceStationTexturePath =
        "ImmersiveChefs/Things/Building/KitchenStation/SauceStation";
    private const string MeatStationTexturePath =
        "ImmersiveChefs/Things/Building/KitchenStation/MeatStation";
    private const string VegetableStationTexturePath =
        "ImmersiveChefs/Things/Building/KitchenStation/VegetableStation";
    private const string PastryStationTexturePath =
        "ImmersiveChefs/Things/Building/KitchenStation/PastryStation";
    private const string MicrowaveTexturePath =
        "ImmersiveChefs/Things/Building/Appliance/Microwave";

    [Test]
    public void Portable_texture_variation_families_are_complete_masked_and_chroma_free()
    {
        var root = FindRepositoryRoot();
        var pairs = new[]
        {
            (CookwareTexturePath + "_Dirty", true),
            (CookwareTexturePath + "_Stone", true),
            (CookwareTexturePath + "_StoneDirty", true),
            (PlateTexturePath + "_Dirty", false),
            (PlateTexturePath + "_Wood", false),
            (PlateTexturePath + "_WoodDirty", false),
            (PlateTexturePath + "_Stone", false),
            (PlateTexturePath + "_StoneDirty", false),
            (CutleryTexturePath + "_Dirty", false),
            (CutleryTexturePath + "_Wood", false),
            (CutleryTexturePath + "_WoodDirty", false)
        };

        foreach (var (texturePath, requireFixedBlackRegion) in pairs)
        {
            var diffusePath = TextureFile(root, texturePath + ".png");
            var maskPath = TextureFile(root, texturePath + "_m.png");
            var packagedDiffuse = PackagedTextureFile(root, texturePath + ".png");
            var packagedMask = PackagedTextureFile(root, texturePath + "_m.png");

            Assert.Multiple(() =>
            {
                Assert.That(File.Exists(diffusePath), Is.True, texturePath + " diffuse");
                Assert.That(File.Exists(maskPath), Is.True, texturePath + " mask");
                Assert.That(File.Exists(packagedDiffuse), Is.True, texturePath + " packaged diffuse");
                Assert.That(File.Exists(packagedMask), Is.True, texturePath + " packaged mask");
            });
            AssertPackagedTextureMatchesSource(diffusePath, packagedDiffuse);
            AssertPackagedTextureMatchesSource(maskPath, packagedMask);
            AssertTransparentMatchingPair(diffusePath, maskPath, requireFixedBlackRegion);
            AssertNoVividGreenChroma(diffusePath);
        }

        AssertSpritesDiffer(
            TextureFile(root, PlateTexturePath + ".png"),
            TextureFile(root, PlateTexturePath + "_Wood.png"));
        AssertSpritesDiffer(
            TextureFile(root, PlateTexturePath + ".png"),
            TextureFile(root, PlateTexturePath + "_Stone.png"));
        AssertSpritesDiffer(
            TextureFile(root, CookwareTexturePath + ".png"),
            TextureFile(root, CookwareTexturePath + "_Stone.png"));
        AssertSpritesDiffer(
            TextureFile(root, CutleryTexturePath + ".png"),
            TextureFile(root, CutleryTexturePath + "_Wood.png"));
        foreach (var (clean, dirty) in new[]
                 {
                     (CookwareTexturePath, CookwareTexturePath + "_Dirty"),
                     (CookwareTexturePath + "_Stone", CookwareTexturePath + "_StoneDirty"),
                     (PlateTexturePath, PlateTexturePath + "_Dirty"),
                     (PlateTexturePath + "_Wood", PlateTexturePath + "_WoodDirty"),
                     (PlateTexturePath + "_Stone", PlateTexturePath + "_StoneDirty"),
                     (CutleryTexturePath, CutleryTexturePath + "_Dirty"),
                     (CutleryTexturePath + "_Wood", CutleryTexturePath + "_WoodDirty")
                 })
        {
            AssertSpritesDiffer(
                TextureFile(root, clean + ".png"),
                TextureFile(root, dirty + ".png"));
        }
    }

    [Test]
    public void Ordinary_cookware_uses_owned_stuffable_art_with_a_matching_mask()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "Kitchenware.xml"));
        var def = document.Root!.Elements("ThingDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_Cookware");
        var graphicData = def.Element("graphicData")!;
        var diffusePath = TextureFile(root, CookwareTexturePath + ".png");
        var maskPath = TextureFile(root, CookwareTexturePath + "_m.png");

        Assert.Multiple(() =>
        {
            Assert.That((string?)graphicData.Element("texPath"), Is.EqualTo(CookwareTexturePath));
            Assert.That((string?)graphicData.Element("graphicClass"), Is.EqualTo("Graphic_Single"));
            Assert.That((string?)graphicData.Element("shaderType"), Is.EqualTo("CutoutComplex"));
            Assert.That(File.Exists(diffusePath), Is.True, "The selected cookware source sprite must exist.");
            Assert.That(File.Exists(maskPath), Is.True, "Stuffable cookware needs a source RimWorld mask.");
            Assert.That(File.Exists(PackagedTextureFile(root, CookwareTexturePath + ".png")), Is.True);
            Assert.That(File.Exists(PackagedTextureFile(root, CookwareTexturePath + "_m.png")), Is.True);
        });

        AssertTransparentMatchingPair(diffusePath, maskPath, requireFixedBlackRegion: true);
    }

    [Test]
    public void Plate_family_uses_owned_single_sprite_art_with_a_stuff_mask()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "Kitchenware.xml"));
        var defs = document.Root!.Elements("ThingDef")
            .Where(element => element.Element("defName") is not null)
            .ToDictionary(element => (string)element.Element("defName")!);
        var plateGraphic = defs["ImmersiveChefs_Plate"].Element("graphicData")!;
        var adobeGraphic = defs["ImmersiveChefs_AdobePlate"].Element("graphicData")!;
        var diffusePath = TextureFile(root, PlateTexturePath + ".png");
        var maskPath = TextureFile(root, PlateTexturePath + "_m.png");

        Assert.Multiple(() =>
        {
            Assert.That((string?)plateGraphic.Element("texPath"), Is.EqualTo(PlateTexturePath));
            Assert.That((string?)plateGraphic.Element("graphicClass"), Is.EqualTo("Graphic_Single"));
            Assert.That((string?)plateGraphic.Element("shaderType"), Is.EqualTo("CutoutComplex"));
            Assert.That((string?)adobeGraphic.Element("texPath"), Is.EqualTo(PlateTexturePath));
            Assert.That((string?)adobeGraphic.Element("graphicClass"), Is.EqualTo("Graphic_Single"));
            Assert.That(File.Exists(diffusePath), Is.True);
            Assert.That(File.Exists(maskPath), Is.True);
            Assert.That(File.Exists(PackagedTextureFile(root, PlateTexturePath + ".png")), Is.True);
            Assert.That(File.Exists(PackagedTextureFile(root, PlateTexturePath + "_m.png")), Is.True);
        });

        AssertTransparentMatchingPair(diffusePath, maskPath, requireFixedBlackRegion: false);
    }

    [Test]
    public void Cutlery_setting_uses_owned_single_sprite_art_with_a_stuff_mask()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "Kitchenware.xml"));
        var def = document.Root!.Elements("ThingDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_Cutlery");
        var graphicData = def.Element("graphicData")!;
        var diffusePath = TextureFile(root, CutleryTexturePath + ".png");
        var maskPath = TextureFile(root, CutleryTexturePath + "_m.png");

        Assert.Multiple(() =>
        {
            Assert.That((string?)graphicData.Element("texPath"), Is.EqualTo(CutleryTexturePath));
            Assert.That((string?)graphicData.Element("graphicClass"), Is.EqualTo("Graphic_Single"));
            Assert.That((string?)graphicData.Element("shaderType"), Is.EqualTo("CutoutComplex"));
            Assert.That(File.Exists(diffusePath), Is.True);
            Assert.That(File.Exists(maskPath), Is.True);
            Assert.That(File.Exists(PackagedTextureFile(root, CutleryTexturePath + ".png")), Is.True);
            Assert.That(File.Exists(PackagedTextureFile(root, CutleryTexturePath + "_m.png")), Is.True);
        });

        AssertTransparentMatchingPair(diffusePath, maskPath, requireFixedBlackRegion: false);
    }

    [Test]
    public void Chefs_knife_set_uses_owned_single_sprite_art_with_a_stuff_mask()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "Kitchenware.xml"));
        var def = document.Root!.Elements("ThingDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_ChefsKnife");
        var graphicData = def.Element("graphicData")!;
        var diffusePath = TextureFile(root, ChefsKnifeTexturePath + ".png");
        var maskPath = TextureFile(root, ChefsKnifeTexturePath + "_m.png");

        Assert.Multiple(() =>
        {
            Assert.That((string?)graphicData.Element("texPath"), Is.EqualTo(ChefsKnifeTexturePath));
            Assert.That((string?)graphicData.Element("graphicClass"), Is.EqualTo("Graphic_Single"));
            Assert.That((string?)graphicData.Element("shaderType"), Is.EqualTo("CutoutComplex"));
            Assert.That(File.Exists(diffusePath), Is.True);
            Assert.That(File.Exists(maskPath), Is.True);
            Assert.That(File.Exists(PackagedTextureFile(root, ChefsKnifeTexturePath + ".png")), Is.True);
            Assert.That(File.Exists(PackagedTextureFile(root, ChefsKnifeTexturePath + "_m.png")), Is.True);
        });

        AssertTransparentMatchingPair(diffusePath, maskPath, requireFixedBlackRegion: true);
    }

    [Test]
    public void Glitterworld_cookware_uses_owned_fixed_color_single_sprite_art()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "Kitchenware.xml"));
        var def = document.Root!.Elements("ThingDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_GlitterworldCookware");
        var graphicData = def.Element("graphicData")!;
        var diffusePath = TextureFile(root, GlitterCookwareTexturePath + ".png");

        Assert.Multiple(() =>
        {
            Assert.That((string?)graphicData.Element("texPath"), Is.EqualTo(GlitterCookwareTexturePath));
            Assert.That((string?)graphicData.Element("graphicClass"), Is.EqualTo("Graphic_Single"));
            Assert.That((string?)graphicData.Element("shaderType"), Is.EqualTo("Cutout"));
            Assert.That(graphicData.Element("color"), Is.Null, "Fixed glitter art must retain its authored color.");
            Assert.That(File.Exists(diffusePath), Is.True);
            Assert.That(File.Exists(PackagedTextureFile(root, GlitterCookwareTexturePath + ".png")), Is.True);
        });

        AssertTransparentSprite(diffusePath);
    }

    [Test]
    public void Prepared_ingredients_use_owned_fixed_color_single_sprite_art()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "PreparedFoodAndStation.xml"));
        var def = document.Root!.Elements("ThingDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_PreparedFood");
        var graphicData = def.Element("graphicData")!;
        var diffusePath = TextureFile(root, PreparedFoodTexturePath + ".png");

        Assert.Multiple(() =>
        {
            Assert.That((string?)graphicData.Element("texPath"), Is.EqualTo(PreparedFoodTexturePath));
            Assert.That((string?)graphicData.Element("graphicClass"), Is.EqualTo("Graphic_Single"));
            Assert.That((string?)graphicData.Element("shaderType"), Is.EqualTo("Cutout"));
            Assert.That(File.Exists(diffusePath), Is.True);
            Assert.That(File.Exists(PackagedTextureFile(root, PreparedFoodTexturePath + ".png")), Is.True);
        });

        AssertTransparentSprite(diffusePath);
    }

    [Test]
    public void Domestic_dishwasher_uses_owned_rotatable_single_sprite_art()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "KitchenBuildings.xml"));
        var def = document.Root!.Elements("ThingDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_Dishwasher");
        var graphicData = def.Element("graphicData")!;
        var diffusePath = TextureFile(root, DishwasherTexturePath + ".png");

        Assert.Multiple(() =>
        {
            Assert.That((string?)graphicData.Element("texPath"), Is.EqualTo(DishwasherTexturePath));
            Assert.That((string?)graphicData.Element("graphicClass"), Is.EqualTo("Graphic_Single"));
            Assert.That((string?)graphicData.Element("shaderType"), Is.EqualTo("Cutout"));
            Assert.That((string?)graphicData.Element("drawSize"), Is.EqualTo("(2,1)"));
            Assert.That(File.Exists(diffusePath), Is.True);
            Assert.That(File.Exists(PackagedTextureFile(root, DishwasherTexturePath + ".png")), Is.True);
        });

        AssertTransparentSprite(diffusePath);
    }

    [Test]
    public void Industrial_dishwasher_uses_owned_rotatable_single_sprite_art()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "KitchenBuildings.xml"));
        var def = document.Root!.Elements("ThingDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_IndustrialDishwasher");
        var graphicData = def.Element("graphicData")!;
        var diffusePath = TextureFile(root, IndustrialDishwasherTexturePath + ".png");

        Assert.Multiple(() =>
        {
            Assert.That((string?)graphicData.Element("texPath"), Is.EqualTo(IndustrialDishwasherTexturePath));
            Assert.That((string?)graphicData.Element("graphicClass"), Is.EqualTo("Graphic_Single"));
            Assert.That((string?)graphicData.Element("shaderType"), Is.EqualTo("Cutout"));
            Assert.That((string?)graphicData.Element("drawSize"), Is.EqualTo("(3,1)"));
            Assert.That(File.Exists(diffusePath), Is.True);
            Assert.That(File.Exists(PackagedTextureFile(root, IndustrialDishwasherTexturePath + ".png")), Is.True);
        });

        AssertTransparentSprite(diffusePath);
    }

    [Test]
    public void Ingredient_prep_station_uses_owned_rotatable_single_sprite_art()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "PreparedFoodAndStation.xml"));
        var def = document.Root!.Elements("ThingDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_PrepStation");
        var graphicData = def.Element("graphicData")!;
        var diffusePath = TextureFile(root, PrepStationTexturePath + ".png");

        Assert.Multiple(() =>
        {
            Assert.That((string?)graphicData.Element("texPath"), Is.EqualTo(PrepStationTexturePath));
            Assert.That((string?)graphicData.Element("graphicClass"), Is.EqualTo("Graphic_Single"));
            Assert.That((string?)graphicData.Element("shaderType"), Is.EqualTo("Cutout"));
            Assert.That((string?)graphicData.Element("drawSize"), Is.EqualTo("(3.5,1.5)"));
            Assert.That(File.Exists(diffusePath), Is.True);
            Assert.That(File.Exists(PackagedTextureFile(root, PrepStationTexturePath + ".png")), Is.True);
        });

        AssertTransparentSprite(diffusePath);
        if (File.Exists(diffusePath))
        {
            using var bitmap = new Bitmap(diffusePath);
            Assert.That(
                bitmap.Width * 15,
                Is.EqualTo(bitmap.Height * 35),
                "The texture canvas must match the 3.5:1.5 draw mesh without Unity stretching it.");
        }
    }

    [Test]
    public void Sauce_station_uses_owned_rotatable_single_sprite_art()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "AssistantStations.xml"));
        var def = document.Root!.Elements("ThingDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_SauceStation");
        var graphicData = def.Element("graphicData")!;
        var diffusePath = TextureFile(root, SauceStationTexturePath + ".png");

        Assert.Multiple(() =>
        {
            Assert.That((string?)graphicData.Element("texPath"), Is.EqualTo(SauceStationTexturePath));
            Assert.That((string?)graphicData.Element("graphicClass"), Is.EqualTo("Graphic_Single"));
            Assert.That((string?)graphicData.Element("shaderType"), Is.EqualTo("Cutout"));
            Assert.That((string?)graphicData.Element("drawSize"), Is.EqualTo("(2,1)"));
            Assert.That(File.Exists(diffusePath), Is.True);
            Assert.That(File.Exists(PackagedTextureFile(root, SauceStationTexturePath + ".png")), Is.True);
        });

        AssertTransparentSprite(diffusePath);
        AssertCanvasAspect(diffusePath, widthUnits: 2, heightUnits: 1);
        AssertNoBrightBlueEmission(diffusePath);
    }

    [Test]
    public void Meat_station_uses_owned_rotatable_single_sprite_art()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "AssistantStations.xml"));
        var def = document.Root!.Elements("ThingDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_MeatStation");
        var graphicData = def.Element("graphicData")!;
        var diffusePath = TextureFile(root, MeatStationTexturePath + ".png");

        Assert.Multiple(() =>
        {
            Assert.That((string?)graphicData.Element("texPath"), Is.EqualTo(MeatStationTexturePath));
            Assert.That((string?)graphicData.Element("graphicClass"), Is.EqualTo("Graphic_Single"));
            Assert.That((string?)graphicData.Element("shaderType"), Is.EqualTo("Cutout"));
            Assert.That((string?)graphicData.Element("drawSize"), Is.EqualTo("(2,1)"));
            Assert.That(File.Exists(diffusePath), Is.True);
            Assert.That(File.Exists(PackagedTextureFile(root, MeatStationTexturePath + ".png")), Is.True);
        });

        AssertTransparentSprite(diffusePath);
        AssertCanvasAspect(diffusePath, widthUnits: 2, heightUnits: 1);
        AssertNoBrightBlueEmission(diffusePath);
    }

    [Test]
    public void Vegetable_station_uses_owned_rotatable_single_sprite_art()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "AssistantStations.xml"));
        var def = document.Root!.Elements("ThingDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_VegetableStation");
        var graphicData = def.Element("graphicData")!;
        var diffusePath = TextureFile(root, VegetableStationTexturePath + ".png");
        var packagedPath = PackagedTextureFile(root, VegetableStationTexturePath + ".png");

        Assert.Multiple(() =>
        {
            Assert.That((string?)graphicData.Element("texPath"), Is.EqualTo(VegetableStationTexturePath));
            Assert.That((string?)graphicData.Element("graphicClass"), Is.EqualTo("Graphic_Single"));
            Assert.That((string?)graphicData.Element("shaderType"), Is.EqualTo("Cutout"));
            Assert.That((string?)graphicData.Element("drawSize"), Is.EqualTo("(2,1)"));
            Assert.That(File.Exists(diffusePath), Is.True);
            Assert.That(File.Exists(packagedPath), Is.True);
        });

        AssertPackagedTextureMatchesSource(diffusePath, packagedPath);
        AssertTransparentSprite(diffusePath);
        AssertCanvasAspect(diffusePath, widthUnits: 2, heightUnits: 1);
        AssertNoVividGreenChroma(diffusePath);
        AssertNoBrightBlueEmission(diffusePath);
    }

    [Test]
    public void Pastry_station_uses_owned_rotatable_single_sprite_art()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "AssistantStations.xml"));
        var def = document.Root!.Elements("ThingDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_PastryStation");
        var graphicData = def.Element("graphicData")!;
        var diffusePath = TextureFile(root, PastryStationTexturePath + ".png");
        var packagedPath = PackagedTextureFile(root, PastryStationTexturePath + ".png");

        Assert.Multiple(() =>
        {
            Assert.That((string?)graphicData.Element("texPath"), Is.EqualTo(PastryStationTexturePath));
            Assert.That((string?)graphicData.Element("graphicClass"), Is.EqualTo("Graphic_Single"));
            Assert.That((string?)graphicData.Element("shaderType"), Is.EqualTo("Cutout"));
            Assert.That((string?)graphicData.Element("drawSize"), Is.EqualTo("(2,1)"));
            Assert.That(File.Exists(diffusePath), Is.True);
            Assert.That(File.Exists(packagedPath), Is.True);
        });

        AssertPackagedTextureMatchesSource(diffusePath, packagedPath);
        AssertTransparentSprite(diffusePath);
        AssertCanvasAspect(diffusePath, widthUnits: 2, heightUnits: 1);
        AssertNoVividGreenChroma(diffusePath);
        AssertNoBrightBlueEmission(diffusePath);
    }

    [Test]
    public void Fallback_microwave_uses_compact_owned_countertop_art()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Patches",
            "Compatibility",
            "ThermodynamicsHotMeals.xml"));
        var def = document.Descendants("ThingDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_Microwave");
        var graphicData = def.Element("graphicData")!;
        var diffusePath = TextureFile(root, MicrowaveTexturePath + ".png");
        var packagedPath = PackagedTextureFile(root, MicrowaveTexturePath + ".png");
        var obsoleteSourcePath = TextureFile(root, "Things/Building/Microwave/Microwave.png");
        var obsoletePackagedPath = PackagedTextureFile(root, "Things/Building/Microwave/Microwave.png");

        Assert.Multiple(() =>
        {
            Assert.That((string?)graphicData.Element("texPath"), Is.EqualTo(MicrowaveTexturePath));
            Assert.That((string?)graphicData.Element("graphicClass"), Is.EqualTo("Graphic_Single"));
            Assert.That((string?)graphicData.Element("shaderType"), Is.EqualTo("Cutout"));
            Assert.That((string?)graphicData.Element("drawSize"), Is.EqualTo("(0.82,0.82)"));
            Assert.That(File.Exists(diffusePath), Is.True);
            Assert.That(File.Exists(packagedPath), Is.True);
            Assert.That(File.Exists(obsoleteSourcePath), Is.False);
            Assert.That(
                File.Exists(obsoletePackagedPath),
                Is.False,
                "Rejected microwave art must not survive incremental package staging.");
        });

        AssertPackagedTextureMatchesSource(diffusePath, packagedPath);
        AssertTransparentSprite(diffusePath);
        AssertCanvasAspect(diffusePath, widthUnits: 1, heightUnits: 1);
        AssertVisibleBounds(
            diffusePath,
            minimumWidth: 280,
            maximumWidth: 430,
            minimumHeight: 180,
            maximumHeight: 340);
        AssertNoVividGreenChroma(diffusePath);
        AssertNoBrightBlueEmission(diffusePath);
    }

    private static string TextureFile(string root, string texturePath)
    {
        return Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Textures",
            texturePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string PackagedTextureFile(string root, string texturePath)
    {
        return Path.Combine(
            root,
            "artifacts",
            "Mods",
            "fumblesneeze.immersivechefs",
            "1.6",
            "Textures",
            texturePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static void AssertTransparentMatchingPair(
        string diffusePath,
        string maskPath,
        bool requireFixedBlackRegion)
    {
        if (!File.Exists(diffusePath) || !File.Exists(maskPath))
        {
            return;
        }

        using var diffuse = new Bitmap(diffusePath);
        using var mask = new Bitmap(maskPath);
        var visibleDiffusePixels = 0;
        var redStuffPixels = 0;
        var fixedBlackPixels = 0;
        var alphaMismatches = 0;
        for (var y = 0; y < diffuse.Height; y++)
        {
            for (var x = 0; x < diffuse.Width; x++)
            {
                var diffusePixel = diffuse.GetPixel(x, y);
                var maskPixel = mask.GetPixel(x, y);
                if (diffusePixel.A > 0)
                {
                    visibleDiffusePixels++;
                }

                if (maskPixel.A > 0 && maskPixel.R >= 240 && maskPixel.G <= 15 && maskPixel.B <= 15)
                {
                    redStuffPixels++;
                }

                if (maskPixel.A > 0 && maskPixel.R <= 15 && maskPixel.G <= 15 && maskPixel.B <= 15)
                {
                    fixedBlackPixels++;
                }

                if (diffusePixel.A != maskPixel.A)
                {
                    alphaMismatches++;
                }
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(diffuse.Size, Is.EqualTo(mask.Size));
            Assert.That(diffuse.Width, Is.GreaterThanOrEqualTo(128));
            Assert.That(diffuse.Height, Is.GreaterThanOrEqualTo(128));
            Assert.That(diffuse.GetPixel(0, 0).A, Is.EqualTo(0));
            Assert.That(diffuse.GetPixel(diffuse.Width - 1, 0).A, Is.EqualTo(0));
            Assert.That(diffuse.GetPixel(0, diffuse.Height - 1).A, Is.EqualTo(0));
            Assert.That(diffuse.GetPixel(diffuse.Width - 1, diffuse.Height - 1).A, Is.EqualTo(0));
            Assert.That(mask.GetPixel(0, 0).A, Is.EqualTo(0));
            Assert.That(visibleDiffusePixels, Is.GreaterThan(100));
            Assert.That(redStuffPixels, Is.GreaterThan(100), "The mask needs a visible red Stuff region.");
            Assert.That(alphaMismatches, Is.Zero, "Diffuse and mask silhouettes must match exactly.");
            if (requireFixedBlackRegion)
            {
                Assert.That(
                    fixedBlackPixels,
                    Is.GreaterThan(100),
                    "Fixed handles need a visible black mask region.");
            }
        });
    }

    private static void AssertTransparentSprite(string diffusePath)
    {
        if (!File.Exists(diffusePath))
        {
            return;
        }

        using var diffuse = new Bitmap(diffusePath);
        var visiblePixels = 0;
        for (var y = 0; y < diffuse.Height; y++)
        {
            for (var x = 0; x < diffuse.Width; x++)
            {
                if (diffuse.GetPixel(x, y).A > 0)
                {
                    visiblePixels++;
                }
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(diffuse.Width, Is.GreaterThanOrEqualTo(128));
            Assert.That(diffuse.Height, Is.GreaterThanOrEqualTo(128));
            Assert.That(diffuse.GetPixel(0, 0).A, Is.EqualTo(0));
            Assert.That(diffuse.GetPixel(diffuse.Width - 1, 0).A, Is.EqualTo(0));
            Assert.That(diffuse.GetPixel(0, diffuse.Height - 1).A, Is.EqualTo(0));
            Assert.That(diffuse.GetPixel(diffuse.Width - 1, diffuse.Height - 1).A, Is.EqualTo(0));
            Assert.That(visiblePixels, Is.GreaterThan(100));
        });
    }

    private static void AssertCanvasAspect(string diffusePath, int widthUnits, int heightUnits)
    {
        if (!File.Exists(diffusePath))
        {
            return;
        }

        using var bitmap = new Bitmap(diffusePath);
        Assert.That(
            bitmap.Width * heightUnits,
            Is.EqualTo(bitmap.Height * widthUnits),
            "The texture canvas must match its draw mesh without Unity stretching it.");
    }

    private static void AssertNoBrightBlueEmission(string diffusePath)
    {
        if (!File.Exists(diffusePath))
        {
            return;
        }

        using var bitmap = new Bitmap(diffusePath);
        var brightBluePixels = 0;
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.A > 0 &&
                    pixel.B >= 100 &&
                    pixel.B > pixel.R * 1.25 &&
                    pixel.B > pixel.G * 1.05)
                {
                    brightBluePixels++;
                }
            }
        }

        Assert.That(
            brightBluePixels,
            Is.Zero,
            "Unconditional base art must not look powered while the building is off or broken.");
    }

    private static void AssertNoVividGreenChroma(string diffusePath)
    {
        if (!File.Exists(diffusePath))
        {
            return;
        }

        using var bitmap = new Bitmap(diffusePath);
        var vividGreenPixels = 0;
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.A > 0 &&
                    pixel.G >= 128 &&
                    pixel.G > pixel.R * 1.4 &&
                    pixel.G > pixel.B * 1.4)
                {
                    vividGreenPixels++;
                }
            }
        }

        Assert.That(
            vividGreenPixels,
            Is.Zero,
            "The selected sprite must not retain vivid green chroma-key pixels.");
    }

    private static void AssertPackagedTextureMatchesSource(string sourcePath, string packagedPath)
    {
        if (!File.Exists(sourcePath) || !File.Exists(packagedPath))
        {
            return;
        }

        Assert.That(
            File.ReadAllBytes(packagedPath),
            Is.EqualTo(File.ReadAllBytes(sourcePath)),
            "The staged texture must be the exact reviewed source asset.");
    }

    private static void AssertSpritesDiffer(string leftPath, string rightPath)
    {
        if (!File.Exists(leftPath) || !File.Exists(rightPath))
        {
            return;
        }

        Assert.That(
            File.ReadAllBytes(rightPath),
            Is.Not.EqualTo(File.ReadAllBytes(leftPath)),
            "A selected variation must not duplicate its base sprite byte-for-byte.");
    }

    private static void AssertVisibleBounds(
        string diffusePath,
        int minimumWidth,
        int maximumWidth,
        int minimumHeight,
        int maximumHeight)
    {
        if (!File.Exists(diffusePath))
        {
            return;
        }

        using var bitmap = new Bitmap(diffusePath);
        var minimumX = bitmap.Width;
        var minimumY = bitmap.Height;
        var maximumX = -1;
        var maximumY = -1;
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).A == 0)
                {
                    continue;
                }

                minimumX = Math.Min(minimumX, x);
                minimumY = Math.Min(minimumY, y);
                maximumX = Math.Max(maximumX, x);
                maximumY = Math.Max(maximumY, y);
            }
        }

        var visibleWidth = maximumX >= minimumX ? maximumX - minimumX + 1 : 0;
        var visibleHeight = maximumY >= minimumY ? maximumY - minimumY + 1 : 0;
        Assert.Multiple(() =>
        {
            Assert.That(visibleWidth, Is.InRange(minimumWidth, maximumWidth));
            Assert.That(visibleHeight, Is.InRange(minimumHeight, maximumHeight));
        });
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ImmersiveChefs.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Immersive Chefs repository root.");
    }
}
