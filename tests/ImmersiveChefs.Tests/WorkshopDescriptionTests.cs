using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class WorkshopDescriptionTests
{
    private static readonly IReadOnlyDictionary<string, string> RecommendedMods =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Dubs Bad Hygiene"] = "836308268",
            ["Processor Framework"] = "3210544395",
            ["Common Sense"] = "1561769193",
            ["Pick Up And Haul"] = "1279012058",
            ["SBZ Fridge"] = "3486264784"
        };

    private static readonly string[] RequiredMechanics =
    {
        "Stuff-aware kitchenware",
        "Physical meal service",
        "Dishwashing and sanitation",
        "Ingredient preparation",
        "Cooperative cooking",
        "Culinary quality",
        "Meal temperature and reheating",
        "Dining standards",
        "Travel, guests, children and patient feeding",
        "Nutrient paste",
        "Trade and world-generated meals"
    };

    private static readonly string[] RequiredThings =
    {
        "Primitive stone cookware set",
        "Cookware set",
        "Glitterworld cookware set",
        "Plate and adobe plate",
        "Cutlery setting",
        "Chef's knife set",
        "Prepared ingredients"
    };

    private static readonly string[] RequiredBuildings =
    {
        "Dishwasher",
        "Industrial dishwasher",
        "Ingredient prep station",
        "Sauce station",
        "Meat station",
        "Vegetable station",
        "Pastry station",
        "Microwave"
    };

    private static readonly string[] RequiredCompatibilityEntries =
    {
        "Processor Framework",
        "Expanded Materials - Metals",
        "Expanded Materials - Masonry",
        "Ceramics (Continued)",
        "Simply Sublime ABS Polymer",
        "Dubs Bad Hygiene",
        "Gastronomy",
        "Hospitality",
        "Common Sense",
        "Pick Up And Haul",
        "Cook for Yourself",
        "Variety Matters",
        "Vanilla Food Variety Expanded",
        "Dynamic Meal Texture Replacer",
        "Food Texture Variety",
        "Vanilla Expanded Framework",
        "Vanilla Textures Expanded - Variations",
        "Vanilla Nutrient Paste Expanded",
        "Adaptive Meal Bill",
        "Meals on Wheels Continued",
        "Replimat / Replimat Meals",
        "Vanilla Cooking Expanded",
        "Fried Meals",
        "Fast Meals",
        "Prioritize Meals over Preserved Foods",
        "RimCuisine 2",
        "Meal Printer",
        "RimFridge",
        "Adaptive Storage Framework",
        "SBZ Fridge",
        "Overcooked Meals",
        "No Vanilla Meals",
        "Thermodynamics - Hot Meals"
    };

    private static readonly IReadOnlyDictionary<string, string> RequiredCompatibilityChains =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Gastronomy"] = "With Cash Register",
            ["Food Texture Variety"] = "Core loads before main; matching VCE add-ons",
            ["Vanilla Textures Expanded - Variations"] = "With Vanilla Expanded Framework",
            ["Vanilla Nutrient Paste Expanded"] = "With Vanilla Expanded Framework",
            ["Replimat / Replimat Meals"] = "Replimat loads before its Meals add-on",
            ["Vanilla Cooking Expanded"] = "With Vanilla Expanded Framework (and Fishing for Sushi)",
            ["Fried Meals"] = "With Harmony and Vanilla Expanded Framework",
            ["RimCuisine 2"] = "Processor Framework loads before Core, then modules",
            ["SBZ Fridge"] = "Loads after Adaptive Storage Framework"
        };

    [Test]
    public void Description_is_an_illustrated_player_showcase_and_ends_with_the_authors_note_within_Steams_exact_limit()
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Release",
            "workshop",
            "description.template.bbcode");
        Assert.That(File.Exists(path), Is.True, "The authored Workshop description template is missing.");

        var descriptionBytes = File.ReadAllBytes(path);
        var description = new UTF8Encoding(false, true).GetString(descriptionBytes);
        var normalized = description.TrimEnd('\r', '\n');
        var steamLimit = ReadSteamDescriptionLimit();

        Assert.Multiple(() =>
        {
            Assert.That(description.IndexOf('\0'), Is.EqualTo(-1), "Steam descriptions may not contain NUL bytes.");
            Assert.That(descriptionBytes.Length + 1, Is.LessThanOrEqualTo(steamLimit),
                "Steam reserves the terminating null inside k_cchPublishedDocumentDescriptionMax.");
            Assert.That(normalized, Does.Contain("RimWorld 1.6"));
            Assert.That(normalized, Does.Contain("[h1]Required Mods[/h1]"));
            Assert.That(normalized, Does.Contain("Harmony"));
            Assert.That(normalized, Does.Contain("XML Extensions"));
            Assert.That(normalized, Does.Contain("[h1]Recommended Mods[/h1]"));
            Assert.That(normalized, Does.Contain("[h1]Optional Mods[/h1]"));
            Assert.That(normalized, Does.Contain("[b]Royalty[/b] - "));
            Assert.That(normalized, Does.Contain("[b]Biotech[/b] - "));
            Assert.That(normalized, Does.Not.Contain("TODO"));
            Assert.That(normalized, Does.Not.Contain("planned"));
            Assert.That(Regex.Matches(normalized, @"\[img\]\{\{image:[a-z0-9-]+\}\}\[/img\]").Count,
                Is.EqualTo(6),
                "The Workshop template must place every reviewed feature graphic.");
            Assert.That(normalized, Does.Not.Contain("{{image:hero}}"),
                "Steam's primary preview already carries the title art; the description must not repeat it.");
            Assert.That(normalized, Does.Not.Contain("{{image:immersive-chefs}}"),
                "The first additional gallery preview is the title art, but the description must not embed it.");
            Assert.That(normalized, Does.Not.Contain("package-gated"));
            Assert.That(normalized, Does.Not.Contain("absent-safe"));
            Assert.That(normalized, Does.Not.Contain("Exclusively owns"));
            Assert.That(normalized, Does.Not.Contain("adapter"));
            Assert.That(normalized, Does.Not.Contain("implementation seam"));
            Assert.That(normalized, Does.Not.Contain("provenance"));
            Assert.That(
                Regex.Matches(normalized, @"\[(?<tag>/?[A-Za-z0-9]+)(?:=[^\]]+)?\]")
                    .Cast<Match>()
                    .Select(match => match.Groups["tag"].Value.TrimStart('/'))
                    .Where(tag => tag is not "h1" and not "b" and not "list" and not "url" and not "img")
                    .ToArray(),
                Is.Empty,
                "The authored description contains a Steam BBCode tag outside the reviewed allowlist.");
            Assert.That(Regex.Matches(normalized, @"\[(?:h1|b|list|url|img)(?:=[^\]]+)?\]").Count,
                Is.EqualTo(Regex.Matches(normalized, @"\[/(?:h1|b|list|url|img)\]").Count),
                "The authored Steam BBCode has an unbalanced supported tag.");
        });

        foreach (var mechanic in RequiredMechanics)
        {
            AssertEntryHasDescription(normalized, mechanic);
        }

        foreach (var thing in RequiredThings)
        {
            AssertEntryHasDescription(normalized, thing);
        }

        foreach (var building in RequiredBuildings)
        {
            AssertEntryHasDescription(normalized, building);
        }

        foreach (var integration in RequiredCompatibilityEntries)
        {
            AssertEntryHasDescription(normalized, integration);
        }

        var recommendedBody = GetSectionBody(normalized, "Recommended Mods");
        var optionalBody = GetSectionBody(normalized, "Optional Mods");
        Assert.That(
            normalized.IndexOf("[h1]Recommended Mods[/h1]", StringComparison.Ordinal),
            Is.LessThan(normalized.IndexOf("[h1]Optional Mods[/h1]", StringComparison.Ordinal)),
            "Recommended Mods must appear immediately before the broader Optional Mods catalog.");
        foreach (var recommendation in RecommendedMods)
        {
            var linkedLabel = "[b][url=https://steamcommunity.com/sharedfiles/filedetails/?id="
                + recommendation.Value + "]" + recommendation.Key + "[/url][/b]";
            Assert.Multiple(() =>
            {
                Assert.That(recommendedBody, Does.Contain(linkedLabel + " - "),
                    recommendation.Key + " must be linked in Recommended Mods.");
                Assert.That(optionalBody, Does.Not.Match(EntryPattern(recommendation.Key)),
                    recommendation.Key + " must not be duplicated under Optional Mods.");
                Assert.That(Regex.Matches(normalized, EntryPattern(recommendation.Key)).Count, Is.EqualTo(1),
                    recommendation.Key + " must have exactly one list entry in the Workshop description.");
            });
        }
        Assert.That(
            recommendedBody,
            Does.Contain("Adaptive Storage Framework ([url=https://steamcommunity.com/sharedfiles/filedetails/?id=3033901359]Workshop page[/url])"),
            "The SBZ Fridge recommendation must link its required storage framework.");
        Assert.That(
            Regex.Matches(normalized, "meals chill or freeze quickly, thaw slowly, and lose more quality after freezing", RegexOptions.IgnoreCase).Count,
            Is.EqualTo(2),
            "Both supported fridge integrations must explain the fast chilling/freezing, slow thaw, and deeper frozen-quality loss.");
        Assert.That(normalized, Does.Contain("plates stay attached"),
            "The RimFridge entry must explain that attached tableware remains with the stored meal.");

        foreach (var chain in RequiredCompatibilityChains)
        {
            Assert.That(
                normalized,
                Does.Match(@"(?m)^\[\*\]\[b\](?:\[url=[^\]]+\])?" + Regex.Escape(chain.Key)
                    + @"(?:\[/url\])?\[/b\] - .*" + Regex.Escape(chain.Value) + @".*$"),
                chain.Key + " dependency-chain guidance");
        }

        const string disclosureHeading = "[h1]Author's Note[/h1]";
        var disclosureIndex = normalized.LastIndexOf(disclosureHeading, StringComparison.Ordinal);
        var disclosureBody = disclosureIndex < 0
            ? string.Empty
            : normalized.Substring(disclosureIndex + disclosureHeading.Length);
        var disclosureParagraphs = disclosureBody
            .Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(paragraph => paragraph.Trim())
            .Where(paragraph => paragraph.Length > 0)
            .ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(disclosureIndex, Is.GreaterThan(0));
            Assert.That(normalized.IndexOf(disclosureHeading, StringComparison.Ordinal), Is.EqualTo(disclosureIndex),
                "The Author's Note must occur exactly once.");
            Assert.That(disclosureBody, Does.Contain("completely developed by AI"));
            Assert.That(disclosureBody, Does.Contain("professional software developer"));
            Assert.That(disclosureBody, Does.Contain("agentic engineering"));
            Assert.That(disclosureBody, Does.Contain("maintainability"));
            Assert.That(disclosureBody, Does.Contain("testability"));
            Assert.That(disclosureBody, Does.Contain("performance testing"));
            Assert.That(disclosureBody, Does.Contain("open-source").IgnoreCase);
            Assert.That(disclosureBody, Does.Contain("proving ground"));
            Assert.That(disclosureBody, Does.Match("playtest(?:ed|ing)"));
            Assert.That(disclosureBody, Does.Contain("comment"));
            Assert.That(disclosureBody, Does.Not.Contain("Pre-generated AI tools helped"));
            Assert.That(disclosureBody, Does.Contain("does not generate AI content while RimWorld is running"));
            Assert.That(disclosureParagraphs, Has.Exactly(2).Items,
                "The final Author's Note must contain exactly the two reviewed paragraphs and nothing after them.");
            Assert.That(disclosureBody, Does.Not.Contain("[h1]"), "The Author's Note must be the last section.");
            Assert.That(disclosureBody, Does.Not.Match(@"(?m)^\[(?!/?(?:b|url)\b)"),
                "No list or section content may follow the final AI disclosure heading.");
        });
    }

    [Test]
    public void Feature_art_has_versioned_templates_local_font_and_six_reviewable_wide_outputs()
    {
        var root = FindRepositoryRoot();
        var workshopRoot = Path.Combine(root, "mods", "ImmersiveChefs", "Release", "workshop");
        var manifestPath = Path.Combine(workshopRoot, "presentation.json");
        var templatePath = Path.Combine(root, "release", "templates", "workshop", "feature-card.svg");
        var fontPath = Path.Combine(root, "release", "templates", "workshop", "fonts", "Oswald-SemiBold.ttf");
        var licensePath = Path.Combine(root, "release", "templates", "workshop", "fonts", "OFL.txt");
        var illustrationRoot = Path.Combine(workshopRoot, "illustrations");

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(manifestPath), Is.True, "The mod-owned presentation manifest is missing.");
            Assert.That(File.Exists(templatePath), Is.True, "The reusable feature-card template is missing.");
            Assert.That(File.Exists(fontPath), Is.True, "Workshop rendering must use a repository-local font.");
            Assert.That(File.Exists(licensePath), Is.True, "The local font license is missing.");
            Assert.That(
                File.ReadAllText(templatePath),
                Does.Not.Contain("<circle cx=\"1095\"").And.Not.Contain("M1087 75L1093 81L1104 68"),
                "Feature cards must not imply a completion/status state with decorative checkmark badges.");
            Assert.That(File.Exists(Path.Combine(illustrationRoot, "colony-service.png")), Is.True,
                "The colony-life card needs its owned home/hospital/caravan illustration.");
            Assert.That(File.Exists(Path.Combine(illustrationRoot, "compatibility-loop.png")), Is.True,
                "The compatibility card needs its owned food/service/storage illustration.");
        });

        foreach (var illustration in new[] { "colony-service.png", "compatibility-loop.png" })
        {
            using var bitmap = new Bitmap(Path.Combine(illustrationRoot, illustration));
            var transparentPixels = 0L;
            var visiblePixels = 0L;
            for (var y = 0; y < bitmap.Height; y++)
            for (var x = 0; x < bitmap.Width; x++)
            {
                var alpha = bitmap.GetPixel(x, y).A;
                if (alpha == 0) transparentPixels++;
                if (alpha > 0) visiblePixels++;
            }

            Assert.Multiple(() =>
            {
                Assert.That(bitmap.GetPixel(0, 0).A, Is.Zero,
                    illustration + " must not carry an opaque background rectangle.");
                Assert.That(transparentPixels, Is.GreaterThan(bitmap.Width * bitmap.Height / 4L),
                    illustration + " needs a materially transparent background.");
                Assert.That(visiblePixels, Is.GreaterThan(bitmap.Width * bitmap.Height / 20L),
                    illustration + " lost its visible illustrated subjects.");
            });
        }

        var manifest = new JavaScriptSerializer().Deserialize<PresentationManifest>(File.ReadAllText(manifestPath));
        var featureCardTemplate = File.ReadAllText(Path.Combine(root, "release", "templates", "workshop", "feature-card.svg"));
        Assert.That(manifest, Is.Not.Null);
        Assert.That(manifest!.schema, Is.EqualTo("ImmersiveChefs/WorkshopPresentation/v1"));
        Assert.That(featureCardTemplate, Does.Contain("Steam Workshop page background observed 2026-08-14: #1b2838"),
            "The versioned card template must retain the measured Steam matte and observation date.");
        Assert.That(manifest.carouselCards, Is.EqualTo(new[] { "kitchenware", "teamwork", "dishwashing", "meals", "colony", "compatibility" }));
        Assert.That(manifest.cards, Has.Exactly(6).Items);
        Assert.That(manifest.cards.Select(card => card.token), Is.Unique);
        Assert.That(manifest.cards.Select(card => card.token), Does.Not.Contain("hero"),
            "The primary Workshop preview must not be duplicated as an additional preview.");

        var steelKitchenwareArt = manifest.cards
            .SelectMany(card => card.art)
            .Where(art => Regex.IsMatch(
                art.source,
                @"Things/Item/Kitchenware/(?:Cookware/Cookware|Plate/Plate|Cutlery/Cutlery|ChefsKnife/ChefsKnife)(?:_Dirty)?\.png$",
                RegexOptions.CultureInvariant))
            .ToArray();
        Assert.That(steelKitchenwareArt, Is.Not.Empty, "The feature cards lost their Stuff-colored kitchenware art.");
        foreach (var art in steelKitchenwareArt)
        {
            var maskSource = art.source.Substring(0, art.source.Length - ".png".Length) + "_m.png";
            Assert.Multiple(() =>
            {
                Assert.That(art.stuffColor, Is.EqualTo("#696969"),
                    art.source + " must be rendered with Core Steel's exact Stuff color.");
                Assert.That(
                    File.Exists(Path.Combine(
                        root,
                        "mods",
                        "ImmersiveChefs",
                        "Textures",
                        "ImmersiveChefs",
                        maskSource.Replace('/', Path.DirectorySeparatorChar))),
                    Is.True,
                    art.source + " must have a shipped mask before Workshop tinting.");
            });
        }

        foreach (var card in manifest.cards)
        {
            var path = Path.Combine(workshopRoot, card.path.Replace('/', Path.DirectorySeparatorChar));
            Assert.Multiple(() =>
            {
                Assert.That(File.Exists(path), Is.True, card.token + " generated image");
                Assert.That(ReadPngDimensions(path), Is.EqualTo((1164, 655)), card.token + " dimensions");
                Assert.That(new FileInfo(path).Length, Is.LessThanOrEqualTo(2 * 1024 * 1024), card.token + " Steam payload");
                Assert.That(card.alt, Is.Not.Null.And.Not.Empty, card.token + " accessibility copy");
                using var bitmap = new Bitmap(path);
                var steamMatte = Color.FromArgb(255, 27, 40, 56).ToArgb();
                Assert.That(new[]
                    {
                        bitmap.GetPixel(0, 0).ToArgb(),
                        bitmap.GetPixel(bitmap.Width - 1, 0).ToArgb(),
                        bitmap.GetPixel(0, bitmap.Height - 1).ToArgb(),
                        bitmap.GetPixel(bitmap.Width - 1, bitmap.Height - 1).ToArgb()
                    },
                    Is.All.EqualTo(steamMatte),
                    card.token + " corners must be composited to Steam Workshop #1b2838 rather than alpha-matted white.");
                Assert.That(card.lines.All(line => !line.TrimStart().StartsWith("✓", StringComparison.Ordinal) &&
                                                  !line.TrimStart().StartsWith("✔", StringComparison.Ordinal) &&
                                                  !line.TrimStart().StartsWith("☑", StringComparison.Ordinal)),
                    Is.True,
                    card.token + " must not use completion/status marks in its feature copy.");
            });
        }


        var colonyCard = manifest.cards.Single(card => card.token == "colony");
        var compatibilityCard = manifest.cards.Single(card => card.token == "compatibility");
        var mealsCard = manifest.cards.Single(card => card.token == "meals");
        Assert.Multiple(() =>
        {
            Assert.That(colonyCard.art.Select(art => art.source),
                Is.EqualTo(new[] { "workshop:illustrations/colony-service.png" }));
            Assert.That(compatibilityCard.art.Select(art => art.source),
                Is.EqualTo(new[] { "workshop:illustrations/compatibility-loop.png" }));
            Assert.That(mealsCard.art.Select(art => art.source),
                Does.Contain("Things/Building/Appliance/Microwave_north.png"),
                "The Workshop meal card must use the reviewed North-rotation frame whose front faces its south interaction cell.");
            Assert.That(mealsCard.art.Select(art => art.source),
                Does.Not.Contain("Things/Building/Appliance/Microwave_south.png"),
                "The Workshop meal card must not present the South-rotation rear frame as its front.");
        });
    }

    [Test]
    public void Showcase_manifest_defines_exact_natural_workflows_crops_and_carousel_slots()
    {
        var root = FindRepositoryRoot();
        var workshopRoot = Path.Combine(root, "mods", "ImmersiveChefs", "Release", "workshop");
        var path = Path.Combine(workshopRoot, "showcases.json");
        var manifest = new JavaScriptSerializer().Deserialize<ShowcaseManifest>(File.ReadAllText(path));
        Assert.That(manifest, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(manifest!.schema, Is.EqualTo("ImmersiveChefs/WorkshopShowcases/v1"));
            Assert.That(manifest.publicationStatus, Is.EqualTo("human-deferred"));
            Assert.That(manifest.showcases, Has.Exactly(5).Items);
            Assert.That(manifest.showcases.Select(showcase => showcase.id), Is.Unique);
            Assert.That(manifest.showcases.Select(showcase => showcase.carousel.slot).OrderBy(value => value),
                Is.EqualTo(new[] { 5, 6, 7, 8, 9 }));
            Assert.That(manifest.showcases.All(showcase => showcase.formats.Contains("screenshot")), Is.True);
            Assert.That(manifest.showcases.Count(showcase => showcase.formats.Contains("gif")), Is.EqualTo(4));
            Assert.That(manifest.showcases.All(showcase => showcase.crop.width == 1280 && showcase.crop.height == 720), Is.True);
            Assert.That(manifest.captureDefaults.maximumDurationSeconds, Is.EqualTo(5));
            Assert.That(manifest.captureDefaults.maximumGifBytes, Is.EqualTo(1048575));
            Assert.That(manifest.captureDefaults.thingSelection, Is.False);
            Assert.That(
                manifest.showcases.All(showcase => showcase.requiredPackageIds.Take(3).SequenceEqual(new[]
                {
                    "brrainz.harmony",
                    "ludeon.rimworld",
                    "imranfish.xmlextensions"
                })),
                Is.True,
                "Every Immersive Chefs showcase declaration must preserve its hard dependency order.");
        });

        var gastronomy = manifest!.showcases.Single(showcase => showcase.id == "gastronomy-service");
        Assert.Multiple(() =>
        {
            Assert.That(gastronomy.requiredPackageIds, Does.Contain("orion.hospitality"));
            Assert.That(gastronomy.requiredPackageIds, Does.Contain("orion.cashregister"));
            Assert.That(gastronomy.requiredPackageIds, Does.Contain("orion.gastronomy"));
            Assert.That(gastronomy.beats, Is.EqualTo(new[] { "order", "cutlery", "full-kitchen-roster", "meal-delivery" }));
        });
    }

    private static void AssertEntryHasDescription(string description, string label)
    {
        Assert.That(
            description,
            Does.Match(EntryPattern(label) + @" - \S.+$"),
            label + " must have a short nonempty behavior description.");
    }

    private static string EntryPattern(string label)
    {
        return @"(?m)^\[\*\]\[b\](?:\[url=[^\]]+\])?" + Regex.Escape(label)
            + @"(?:\[/url\])?\[/b\]";
    }

    private static string GetSectionBody(string description, string heading)
    {
        var marker = "[h1]" + heading + "[/h1]";
        var start = description.IndexOf(marker, StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), heading + " section is missing.");
        start += marker.Length;
        var end = description.IndexOf("[h1]", start, StringComparison.Ordinal);
        return end < 0 ? description.Substring(start) : description.Substring(start, end - start);
    }

    private static int ReadSteamDescriptionLimit()
    {
        var rimWorldPath = typeof(WorkshopDescriptionTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "RimWorldPath")
            .Value;
        var steamworksPath = Path.Combine(
            rimWorldPath,
            "RimWorldWin64_Data",
            "Managed",
            "com.rlabrecque.steamworks.net.dll");
        var steamworks = Assembly.LoadFrom(steamworksPath);
        var constants = steamworks.GetType("Steamworks.Constants", throwOnError: true)!;
        var field = constants.GetField(
            "k_cchPublishedDocumentDescriptionMax",
            BindingFlags.Public | BindingFlags.Static);
        Assert.That(field, Is.Not.Null, "The installed Steamworks SDK description-limit constant is missing.");
        return (int)field!.GetValue(null)!;
    }

    private static (int Width, int Height) ReadPngDimensions(string path)
    {
        var bytes = File.ReadAllBytes(path);
        Assert.That(bytes.Take(8).ToArray(), Is.EqualTo(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }), path);
        return (
            (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19],
            (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23]);
    }

    private sealed class PresentationManifest
    {
        public string schema { get; set; } = string.Empty;
        public string[] carouselCards { get; set; } = Array.Empty<string>();
        public PresentationCard[] cards { get; set; } = Array.Empty<PresentationCard>();
    }

    private sealed class PresentationCard
    {
        public string token { get; set; } = string.Empty;
        public string path { get; set; } = string.Empty;
        public string alt { get; set; } = string.Empty;
        public string[] lines { get; set; } = Array.Empty<string>();
        public PresentationArt[] art { get; set; } = Array.Empty<PresentationArt>();
    }

    private sealed class PresentationArt
    {
        public string source { get; set; } = string.Empty;
        public string stuffColor { get; set; } = string.Empty;
    }

    private sealed class ShowcaseManifest
    {
        public string schema { get; set; } = string.Empty;
        public string publicationStatus { get; set; } = string.Empty;
        public ShowcaseCaptureDefaults captureDefaults { get; set; } = new();
        public Showcase[] showcases { get; set; } = Array.Empty<Showcase>();
    }

    private sealed class ShowcaseCaptureDefaults
    {
        public int maximumDurationSeconds { get; set; }
        public int maximumGifBytes { get; set; }
        public bool thingSelection { get; set; }
    }

    private sealed class Showcase
    {
        public string id { get; set; } = string.Empty;
        public string[] formats { get; set; } = Array.Empty<string>();
        public string[] requiredPackageIds { get; set; } = Array.Empty<string>();
        public string[] beats { get; set; } = Array.Empty<string>();
        public ShowcaseCrop crop { get; set; } = new();
        public ShowcaseCarousel carousel { get; set; } = new();
    }

    private sealed class ShowcaseCrop
    {
        public int width { get; set; }
        public int height { get; set; }
    }

    private sealed class ShowcaseCarousel
    {
        public int slot { get; set; }
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "RimWorldMods.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
