using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class LocalizationReleaseGateTests
{
    private static readonly string[] RequiredLanguages =
    {
        "English",
        "German",
        "Spanish",
        "French",
        "ChineseSimplified",
        "Russian"
    };

    private static readonly HashSet<string> TranslatableDefFields = new(StringComparer.Ordinal)
    {
        "label",
        "description",
        "jobString",
        "reportString",
        "verb",
        "gerund"
    };

    private static readonly string[] InvariantProperNameKeys =
    {
        "ImmersiveChefs_Integration_ProcessorFramework",
        "ImmersiveChefs_Integration_ExpandedMaterials",
        "ImmersiveChefs_Integration_CeramicsContinued",
        "ImmersiveChefs_Integration_AbsPolymer",
        "ImmersiveChefs_Integration_DubsBadHygiene",
        "ImmersiveChefs_Integration_Gastronomy",
        "ImmersiveChefs_Integration_CommonSense",
        "ImmersiveChefs_Integration_Hospitality",
        "ImmersiveChefs_Integration_VarietyMatters",
        "ImmersiveChefs_Integration_VanillaFoodVarietyExpanded",
        "ImmersiveChefs_Integration_VanillaExpandedFramework",
        "ImmersiveChefs_Integration_VanillaNutrientPasteExpanded",
        "ImmersiveChefs_Integration_AdaptiveMealBill",
        "ImmersiveChefs_Integration_OvercookedMeals",
        "ImmersiveChefs_Integration_MealsOnWheels",
        "ImmersiveChefs_Integration_PrioritizeMeals",
        "ImmersiveChefs_Integration_Replimat",
        "ImmersiveChefs_Integration_MealPrinter",
        "ImmersiveChefs_Integration_FoodTextureVariety",
        "ImmersiveChefs_Integration_PickUpAndHaul",
        "ImmersiveChefs_Integration_CookForYourself",
        "ImmersiveChefs_IngredientRequirement"
    };

    private static readonly IReadOnlyDictionary<string, string> RuntimeLocalizedConditionalDefFields =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["RecipeDef:ImmersiveChefs_MakeAdobePlates.label"] = "ImmersiveChefs_Recipe_MakeAdobePlates_Label",
            ["RecipeDef:ImmersiveChefs_MakeAdobePlates.description"] = "ImmersiveChefs_Recipe_MakeAdobePlates_Description",
            ["RecipeDef:ImmersiveChefs_MakeAdobePlates.jobString"] = "ImmersiveChefs_Recipe_MakeAdobePlates_JobString"
        };

    [Test]
    public void Every_mod_is_classified_and_only_distributable_products_require_catalogs()
    {
        var root = FindRepositoryRoot();
        var classifications = Directory.EnumerateDirectories(Path.Combine(root, "mods"))
            .Select(path => new
            {
                Name = Path.GetFileName(path),
                Path = path,
                Project = XDocument.Load(Directory.EnumerateFiles(path, "*.csproj").Single())
            })
            .ToDictionary(
                item => item.Name,
                item => item.Project.Descendants("RimWorldDistributionKind").SingleOrDefault()?.Value,
                StringComparer.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(classifications["ImmersiveChefs"], Is.EqualTo("Product"));
            Assert.That(classifications["RimWorldDevGateway"], Is.EqualTo("DevelopmentOnly"));
            Assert.That(
                classifications.Values,
                Has.All.Matches<string?>(value => value is "Product" or "DevelopmentOnly"),
                "Every repo-owned RimWorld mod must explicitly declare whether it is distributable.");
        });

        foreach (var product in classifications.Where(item => item.Value == "Product"))
        {
            foreach (var language in RequiredLanguages)
            {
                Assert.That(
                    Directory.Exists(Path.Combine(root, "mods", product.Key, "Languages", language)),
                    Is.True,
                    $"{product.Key} is missing the required {language} catalog.");
            }
        }
    }

    [Test]
    public void Required_keyed_catalogs_match_English_with_placeholder_and_tag_parity()
    {
        foreach (var product in DiscoverProductMods())
        {
            var canonical = LoadCatalog(Path.Combine(product.Root, "Languages", "English", "Keyed"));
            Assert.That(canonical, Is.Not.Empty,
                $"{product.Name}/English keyed text is the canonical runtime catalog.");

            foreach (var language in RequiredLanguages.Skip(1))
            {
                var translated = LoadCatalog(Path.Combine(product.Root, "Languages", language, "Keyed"));
                AssertCatalogMatches(canonical, translated, language, product.Name + "/Keyed");
            }
        }
    }

    [Test]
    public void Only_explicitly_reviewed_proper_names_and_technical_tokens_remain_identical_to_English()
    {
        var languageSpecific = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["German"] = new[]
            {
                "ImmersiveChefs_SettingsCategory",
                "ImmersiveChefs_Enum_OptionalIntegrationMode_Auto",
                "ImmersiveChefs_CulinaryBand_Normal",
                "ImmersiveChefs_ThermalBand_Warm"
            },
            ["Spanish"] = new[]
            {
                "ImmersiveChefs_SettingsCategory",
                "ImmersiveChefs_Enum_OptionalIntegrationMode_Auto",
                "ImmersiveChefs_CulinaryBand_Normal"
            },
            ["French"] = new[]
            {
                "ImmersiveChefs_SettingsCategory",
                "ImmersiveChefs_Enum_WareRequirementMode_Strict",
                "ImmersiveChefs_Enum_OptionalIntegrationMode_Auto"
            },
            ["ChineseSimplified"] = Array.Empty<string>(),
            ["Russian"] = Array.Empty<string>()
        };

        foreach (var product in DiscoverProductMods())
        {
            var canonical = LoadCatalog(Path.Combine(product.Root, "Languages", "English", "Keyed"));
            foreach (var language in RequiredLanguages.Skip(1))
            {
                var translated = LoadCatalog(Path.Combine(product.Root, "Languages", language, "Keyed"));
                var identical = translated
                    .Where(entry => entry.Value.Text == canonical[entry.Key].Text)
                    .Select(entry => entry.Key);
                var approved = product.Name == "ImmersiveChefs"
                    ? InvariantProperNameKeys.Concat(languageSpecific[language])
                    : Enumerable.Empty<string>();
                Assert.That(identical, Is.EquivalentTo(approved),
                    $"{product.Name}/{language} contains an unreviewed English fallback or a stale equality approval.");
            }
        }
    }

    [Test]
    public void English_keyed_catalog_matches_the_complete_runtime_key_inventory()
    {
        var modRoot = Path.Combine(FindRepositoryRoot(), "mods", "ImmersiveChefs");
        var canonical = LoadCatalog(Path.Combine(modRoot, "Languages", "English", "Keyed"));
        var sourceKeys = new HashSet<string>(StringComparer.Ordinal);
        var directKeyPattern = new Regex(
            "\\\"(?<key>ImmersiveChefs_[A-Za-z0-9_]+)\\\"\\s*\\.Translate",
            RegexOptions.CultureInvariant);
        var indirectKeyPattern = new Regex(
            "\\\"(?<key>ImmersiveChefs_(?:PoisonCause|Product|Stat|Cleanliness)_[A-Za-z0-9_]+)\\\"",
            RegexOptions.CultureInvariant);
        foreach (var file in Directory.EnumerateFiles(Path.Combine(modRoot, "Source"), "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            foreach (Match match in directKeyPattern.Matches(text).Cast<Match>()
                         .Concat(indirectKeyPattern.Matches(text).Cast<Match>()))
            {
                sourceKeys.Add(match.Groups["key"].Value);
            }
        }

        foreach (var value in Enum.GetNames(typeof(WareRequirementMode)))
        {
            sourceKeys.Add("ImmersiveChefs_Enum_WareRequirementMode_" + value);
        }
        foreach (var value in Enum.GetNames(typeof(DirtyWareFallback)))
        {
            sourceKeys.Add("ImmersiveChefs_Enum_DirtyWareFallback_" + value);
        }
        foreach (var value in Enum.GetNames(typeof(OptionalIntegrationMode)))
        {
            sourceKeys.Add("ImmersiveChefs_Enum_OptionalIntegrationMode_" + value);
        }
        foreach (var value in new[] { "Awful", "Poor", "Normal", "Good", "Excellent", "Masterwork", "Legendary" })
        {
            sourceKeys.Add("ImmersiveChefs_CulinaryBand_" + value);
        }
        foreach (var value in Enum.GetNames(typeof(ThermalBand)))
        {
            sourceKeys.Add("ImmersiveChefs_ThermalBand_" + value);
        }

        Assert.That(canonical.Keys, Is.EquivalentTo(sourceKeys),
            "The English runtime catalog and every stable/dynamic product key must stay exact.");
    }

    [Test]
    public void Release_package_contains_the_exact_reviewed_language_files()
    {
        var root = FindRepositoryRoot();
        foreach (var product in DiscoverProductMods())
        {
            var sourceRoot = Path.Combine(product.Root, "Languages");
            var packageRoot = Path.Combine(
                root,
                "artifacts",
                "Mods",
                product.PackageId,
                "1.6",
                "Languages");
            var sourceFiles = Directory.EnumerateFiles(sourceRoot, "*.xml", SearchOption.AllDirectories)
                .ToDictionary(path => RelativeTo(sourceRoot, path), File.ReadAllBytes, StringComparer.OrdinalIgnoreCase);
            var packageFiles = Directory.EnumerateFiles(packageRoot, "*.xml", SearchOption.AllDirectories)
                .ToDictionary(path => RelativeTo(packageRoot, path), File.ReadAllBytes, StringComparer.OrdinalIgnoreCase);

            Assert.That(packageFiles.Keys, Is.EquivalentTo(sourceFiles.Keys), product.Name);
            Assert.Multiple(() =>
            {
                foreach (var relativePath in sourceFiles.Keys)
                {
                    Assert.That(packageFiles[relativePath], Is.EqualTo(sourceFiles[relativePath]),
                        product.Name + "/" + relativePath);
                }
            });
        }
    }

    [Test]
    public void Required_DefInjected_catalogs_cover_every_owned_translatable_Def_field()
    {
        foreach (var product in DiscoverProductMods())
        {
            var expected = InventoryOwnedDefFields(product.Root);
            RemoveRuntimeLocalizedConditionalDefFields(expected);
            Assert.That(expected.Values.Sum(keys => keys.Count), Is.GreaterThan(0), product.Name);

            foreach (var language in RequiredLanguages.Skip(1))
            {
                var languageRoot = Path.Combine(product.Root, "Languages", language, "DefInjected");
                var actualTypes = Directory.EnumerateFiles(languageRoot, "*.xml", SearchOption.AllDirectories)
                    .Select(path => RelativeTo(languageRoot, path).Split(Path.DirectorySeparatorChar)[0])
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
                Assert.That(actualTypes, Is.EquivalentTo(expected.Keys),
                    $"{product.Name}/{language} contains a missing or stale DefInjected type tree.");

                foreach (var defType in expected.Keys.OrderBy(value => value, StringComparer.Ordinal))
                {
                    var actual = LoadCatalog(Path.Combine(languageRoot, defType));
                    Assert.That(
                        actual.Keys,
                        Is.EquivalentTo(expected[defType].Keys),
                        $"{product.Name}/{language}/{defType} must translate the exact owned Def-field inventory.");
                    AssertCatalogMatches(expected[defType], actual, language, product.Name + "/" + defType);
                    Assert.That(
                        actual.Where(entry => entry.Value.Text == expected[defType][entry.Key].Text)
                            .Select(entry => entry.Key),
                        Is.Empty,
                        $"{product.Name}/{language}/{defType} contains an unapproved English-identical Def value.");
                }
            }
        }
    }

    [Test]
    public void Conditional_Defs_use_keyed_runtime_localization_without_absent_DefInjected_errors()
    {
        foreach (var product in DiscoverProductMods())
        {
            var source = string.Join(
                "\n",
                Directory.EnumerateFiles(Path.Combine(product.Root, "Source"), "*.cs", SearchOption.AllDirectories)
                    .Select(File.ReadAllText));
            foreach (var language in RequiredLanguages)
            {
                var keyed = LoadCatalog(Path.Combine(product.Root, "Languages", language, "Keyed"));
                foreach (var localizationKey in RuntimeLocalizedConditionalDefFields.Values)
                {
                    Assert.That(keyed.ContainsKey(localizationKey), Is.True,
                        $"{product.Name}/{language} lacks runtime localization {localizationKey}.");
                }
            }

            foreach (var localizationKey in RuntimeLocalizedConditionalDefFields.Values)
            {
                Assert.That(source, Does.Contain($"\"{localizationKey}\".Translate()"),
                    $"{product.Name} must apply {localizationKey} only after the conditional Def exists.");
            }
        }
    }

    [Test]
    public void Conditional_recipe_label_translation_invalidates_RimWorlds_cached_LabelCap()
    {
        var path = Path.Combine(
            FindRepositoryRoot(),
            "mods",
            "ImmersiveChefs",
            "Source",
            "Integrations",
            "OptionalMaterialAdapter.cs");
        var source = File.ReadAllText(path);
        var labelAssignment = source.IndexOf(
            "adobeRecipe.label = \"ImmersiveChefs_Recipe_MakeAdobePlates_Label\".Translate();",
            StringComparison.Ordinal);
        var cacheReset = source.IndexOf(
            "CachedLabelCapField.SetValue(adobeRecipe, default(TaggedString));",
            StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("typeof(Def).GetField(\"cachedLabelCap\", BindingFlags.Instance | BindingFlags.NonPublic)"),
                "The exact RimWorld 1.6 label cache must be resolved explicitly.");
            Assert.That(labelAssignment, Is.GreaterThanOrEqualTo(0));
            Assert.That(cacheReset, Is.GreaterThan(labelAssignment),
                "Changing RecipeDef.label must invalidate the already-computed LabelCap value.");
        });
    }

    [Test]
    public void Guarded_player_UI_call_sites_do_not_receive_raw_literals()
    {
        var guardedPatterns = new[]
        {
            @"listing\.(?:Label|CheckboxLabeled)\(\s*\$?""(?<literal>[^""]+)",
            @"listing\.SliderLabeled\(\s*\$?""(?<literal>[^""]+)",
            @"new\s+FloatMenuOption\(\s*\$?""(?<literal>[^""]+)",
            @"Messages\.Message\(\s*\$?""(?<literal>[^""]+)",
            @"default(?:Label|Desc)\s*=\s*\$?""(?<literal>[^""]+)",
            @"JobFailReason\.Is\(\s*\$?""(?<literal>[^""]+)",
            @"SetField\([^\r\n]*""label""\s*,\s*\$?""(?<literal>[^""]+)"
        };

        foreach (var product in DiscoverProductMods())
        {
            var sourceRoot = Path.Combine(product.Root, "Source");
            var source = string.Join(
                "\n",
                Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
                    .Select(File.ReadAllText));
            var canonical = LoadCatalog(Path.Combine(product.Root, "Languages", "English", "Keyed"));
            Assert.Multiple(() =>
            {
                foreach (var pattern in guardedPatterns)
                {
                    foreach (Match match in Regex.Matches(source, pattern, RegexOptions.CultureInvariant))
                    {
                        var literal = match.Groups["literal"].Value;
                        Assert.That(canonical.ContainsKey(literal), Is.True,
                            $"{product.Name} guarded player-UI API receives raw or unknown text '{literal}'.");
                    }
                }
            });
        }
    }

    private static Dictionary<string, Dictionary<string, CatalogEntry>> InventoryOwnedDefFields(string modRoot)
    {
        var result = new Dictionary<string, Dictionary<string, CatalogEntry>>(StringComparer.Ordinal);
        var files = Directory.EnumerateFiles(Path.Combine(modRoot, "Defs"), "*.xml", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(modRoot, "Patches"), "*.xml", SearchOption.AllDirectories));
        foreach (var file in files)
        {
            var document = XDocument.Load(file, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            var definitions = document.Descendants()
                .Where(element =>
                    element.Name.LocalName.EndsWith("Def", StringComparison.Ordinal) &&
                    element.Elements().Any(child => child.Name.LocalName == "defName"));
            foreach (var definition in definitions)
            {
                var defName = definition.Elements().Single(child => child.Name.LocalName == "defName").Value.Trim();
                if (!result.TryGetValue(definition.Name.LocalName, out var fields))
                {
                    fields = new Dictionary<string, CatalogEntry>(StringComparer.Ordinal);
                    result.Add(definition.Name.LocalName, fields);
                }

                InventoryDefFields(definition, defName, string.Empty, fields);
            }
        }

        return result;
    }

    private static void RemoveRuntimeLocalizedConditionalDefFields(
        IDictionary<string, Dictionary<string, CatalogEntry>> inventory)
    {
        foreach (var entry in RuntimeLocalizedConditionalDefFields)
        {
            var separator = entry.Key.IndexOf(':');
            var defType = entry.Key.Substring(0, separator);
            var defField = entry.Key.Substring(separator + 1);
            Assert.That(inventory.TryGetValue(defType, out var fields) && fields.Remove(defField), Is.True,
                $"Runtime-localized conditional Def mapping is stale: {entry.Key}.");
        }
    }

    private static void InventoryDefFields(
        XElement parent,
        string defName,
        string prefix,
        IDictionary<string, CatalogEntry> fields)
    {
        var listIndex = 0;
        foreach (var child in parent.Elements())
        {
            if (child.Name.LocalName == "defName")
            {
                continue;
            }

            var segment = child.Name.LocalName == "li"
                ? (listIndex++).ToString(System.Globalization.CultureInfo.InvariantCulture)
                : child.Name.LocalName;
            var path = string.IsNullOrEmpty(prefix) ? segment : prefix + "." + segment;
            if (TranslatableDefFields.Contains(child.Name.LocalName))
            {
                var key = defName + "." + path;
                Assert.That(fields.ContainsKey(key), Is.False, $"Duplicate Def field {key}.");
                fields.Add(key, CatalogEntry.From(child));
            }
            else
            {
                InventoryDefFields(child, defName, path, fields);
            }
        }
    }

    private static Dictionary<string, CatalogEntry> LoadCatalog(string directory)
    {
        var result = new Dictionary<string, CatalogEntry>(StringComparer.Ordinal);
        if (!Directory.Exists(directory))
        {
            return result;
        }

        foreach (var file in Directory.EnumerateFiles(directory, "*.xml", SearchOption.AllDirectories))
        {
            var document = XDocument.Load(file, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            Assert.That(document.Root?.Name.LocalName, Is.EqualTo("LanguageData"), file);
            foreach (var entry in document.Root!.Elements())
            {
                Assert.That(result.ContainsKey(entry.Name.LocalName), Is.False,
                    $"Duplicate localization key {entry.Name.LocalName} in {directory}.");
                result.Add(entry.Name.LocalName, CatalogEntry.From(entry));
            }
        }

        return result;
    }

    private static void AssertCatalogMatches(
        IReadOnlyDictionary<string, CatalogEntry> canonical,
        IReadOnlyDictionary<string, CatalogEntry> translated,
        string language,
        string catalog)
    {
        Assert.That(translated.Keys, Is.EquivalentTo(canonical.Keys), $"{language}/{catalog} key drift.");
        Assert.Multiple(() =>
        {
            foreach (var key in canonical.Keys.OrderBy(value => value, StringComparer.Ordinal))
            {
                Assert.That(translated[key].Text, Is.Not.Empty, $"{language}/{catalog}/{key} is empty.");
                Assert.That(translated[key].Text, Does.Not.Contain("TODO"), $"{language}/{catalog}/{key}");
                Assert.That(translated[key].Text, Does.Not.Contain("MISSING"), $"{language}/{catalog}/{key}");
                Assert.That(translated[key].Placeholders, Is.EqualTo(canonical[key].Placeholders),
                    $"{language}/{catalog}/{key} placeholder drift.");
                Assert.That(translated[key].Tags, Is.EqualTo(canonical[key].Tags),
                    $"{language}/{catalog}/{key} rich-text tag drift.");
            }
        });
    }

    private sealed class CatalogEntry
    {
        private static readonly Regex PlaceholderPattern = new(@"\{[^{}]+\}", RegexOptions.CultureInvariant);

        private CatalogEntry(string text, string[] placeholders, string[] tags)
        {
            Text = text;
            Placeholders = placeholders;
            Tags = tags;
        }

        internal string Text { get; }
        internal string[] Placeholders { get; }
        internal string[] Tags { get; }

        internal static CatalogEntry From(XElement element)
        {
            return new CatalogEntry(
                element.Value.Trim(),
                PlaceholderPattern.Matches(element.Value).Cast<Match>().Select(match => match.Value).OrderBy(value => value).ToArray(),
                element.Descendants().Select(child => child.Name.LocalName).OrderBy(value => value).ToArray());
        }
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

    private static IReadOnlyList<ProductMod> DiscoverProductMods()
    {
        return Directory.EnumerateDirectories(Path.Combine(FindRepositoryRoot(), "mods"))
            .Select(path =>
            {
                var project = XDocument.Load(Directory.EnumerateFiles(path, "*.csproj").Single());
                return new
                {
                    Name = Path.GetFileName(path),
                    Root = path,
                    Kind = project.Descendants("RimWorldDistributionKind").SingleOrDefault()?.Value,
                    PackageId = project.Descendants("RimWorldPackageId").SingleOrDefault()?.Value
                };
            })
            .Where(item => item.Kind == "Product")
            .Select(item => new ProductMod(
                item.Name,
                item.Root,
                string.IsNullOrWhiteSpace(item.PackageId)
                    ? throw new AssertionException($"Product {item.Name} has no RimWorldPackageId.")
                    : item.PackageId!))
            .OrderBy(item => item.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private static string RelativeTo(string root, string path)
    {
        return path.Substring(root.TrimEnd(Path.DirectorySeparatorChar).Length + 1)
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
    }

    private sealed class ProductMod
    {
        internal ProductMod(string name, string root, string packageId)
        {
            Name = name;
            Root = root;
            PackageId = packageId;
        }

        internal string Name { get; }
        internal string Root { get; }
        internal string PackageId { get; }
    }
}
