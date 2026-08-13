using System;
using System.Collections.Generic;
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
            Assert.That(normalized, Does.Contain("[h1]Optional Mods[/h1]"));
            Assert.That(normalized, Does.Contain("[b]Royalty[/b] - "));
            Assert.That(normalized, Does.Contain("[b]Biotech[/b] - "));
            Assert.That(normalized, Does.Not.Contain("TODO"));
            Assert.That(normalized, Does.Not.Contain("planned"));
            Assert.That(Regex.Matches(normalized, @"\[img\]\{\{image:[a-z0-9-]+\}\}\[/img\]").Count,
                Is.EqualTo(7),
                "The Workshop template must place every reviewed title and feature graphic.");
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

        foreach (var chain in RequiredCompatibilityChains)
        {
            Assert.That(
                normalized,
                Does.Contain("[b]" + chain.Key + "[/b] - " + chain.Value),
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
            Assert.That(disclosureBody, Does.Contain("artwork"));
            Assert.That(disclosureBody, Does.Contain("localization"));
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

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(manifestPath), Is.True, "The mod-owned presentation manifest is missing.");
            Assert.That(File.Exists(templatePath), Is.True, "The reusable feature-card template is missing.");
            Assert.That(File.Exists(fontPath), Is.True, "Workshop rendering must use a repository-local font.");
            Assert.That(File.Exists(licensePath), Is.True, "The local font license is missing.");
        });

        var manifest = new JavaScriptSerializer().Deserialize<PresentationManifest>(File.ReadAllText(manifestPath));
        Assert.That(manifest, Is.Not.Null);
        Assert.That(manifest!.schema, Is.EqualTo("ImmersiveChefs/WorkshopPresentation/v1"));
        Assert.That(manifest.cards, Has.Exactly(7).Items);
        Assert.That(manifest.cards.Select(card => card.token), Is.Unique);

        foreach (var card in manifest.cards)
        {
            var path = Path.Combine(workshopRoot, card.path.Replace('/', Path.DirectorySeparatorChar));
            Assert.Multiple(() =>
            {
                Assert.That(File.Exists(path), Is.True, card.token + " generated image");
                Assert.That(ReadPngDimensions(path), Is.EqualTo((1164, 655)), card.token + " dimensions");
                Assert.That(new FileInfo(path).Length, Is.LessThanOrEqualTo(2 * 1024 * 1024), card.token + " Steam payload");
                Assert.That(card.alt, Is.Not.Null.And.Not.Empty, card.token + " accessibility copy");
            });
        }
    }

    private static void AssertEntryHasDescription(string description, string label)
    {
        Assert.That(
            description,
            Does.Match(@"(?m)^\[\*\]\[b\]" + Regex.Escape(label) + @"\[/b\] - \S.+$"),
            label + " must have a short nonempty behavior description.");
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
        public PresentationCard[] cards { get; set; } = Array.Empty<PresentationCard>();
    }

    private sealed class PresentationCard
    {
        public string token { get; set; } = string.Empty;
        public string path { get; set; } = string.Empty;
        public string alt { get; set; } = string.Empty;
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

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
