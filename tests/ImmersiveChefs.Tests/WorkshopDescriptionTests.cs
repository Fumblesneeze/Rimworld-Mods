using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
    public void Description_covers_the_release_and_ends_with_an_AI_disclosure_within_Steams_exact_limit()
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Release",
            "workshop",
            "description.bbcode");
        Assert.That(File.Exists(path), Is.True, "The authored Workshop description is missing.");

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
            Assert.That(
                Regex.Matches(normalized, @"\[(?<tag>/?[A-Za-z0-9]+)(?:=[^\]]+)?\]")
                    .Cast<Match>()
                    .Select(match => match.Groups["tag"].Value.TrimStart('/'))
                    .Where(tag => tag is not "h1" and not "b" and not "list" and not "url")
                    .ToArray(),
                Is.Empty,
                "The authored description contains a Steam BBCode tag outside the reviewed allowlist.");
            Assert.That(Regex.Matches(normalized, @"\[(?:h1|b|list|url)(?:=[^\]]+)?\]").Count,
                Is.EqualTo(Regex.Matches(normalized, @"\[/(?:h1|b|list|url)\]").Count),
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

        const string disclosureHeading = "[h1]AI disclosure[/h1]";
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
                "The AI disclosure must occur exactly once.");
            Assert.That(disclosureBody, Does.Contain("artwork"));
            Assert.That(disclosureBody, Does.Contain("localization"));
            Assert.That(disclosureBody, Does.Contain("pre-generated").IgnoreCase);
            Assert.That(disclosureBody, Does.Contain("does not generate AI content while RimWorld is running"));
            Assert.That(disclosureParagraphs, Has.Exactly(1).Items,
                "The final AI disclosure must contain exactly one authored paragraph and nothing after it.");
            Assert.That(disclosureBody, Does.Not.Contain("[h1]"), "The AI disclosure must be the last section.");
            Assert.That(disclosureBody, Does.Not.Match(@"(?m)^\[(?!/?(?:b|url)\b)"),
                "No list or section content may follow the final AI disclosure heading.");
        });
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
