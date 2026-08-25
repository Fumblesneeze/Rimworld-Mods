using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class AdaptiveStorageCompatibilityTests
{
    [Test]
    public void Product_keeps_sbz_fridge_compatibility_passive_and_absent_safe()
    {
        var forbiddenAssemblyPrefixes = new[]
        {
            "AdaptiveStorage",
            "FixesForSBZFridge"
        };
        var references = typeof(ImmersiveChefsMod).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();
        Assert.That(
            references.Where(reference => forbiddenAssemblyPrefixes.Any(prefix =>
                reference.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))),
            Is.Empty,
            "Immersive Chefs must consume the ordinary Thing.AmbientTemperature contract without a hard Adaptive Storage/[sbz] assembly reference.");

        var root = FindRepositoryRoot();
        var productRoot = Path.Combine(root, "mods", "ImmersiveChefs");
        var forbiddenCouplingTokens = new[]
        {
            "adaptive.storage.framework",
            "sbz.NeatStorageFridge",
            "AdaptiveStorage.",
            "AdaptiveStorageFramework",
            "FixesForSBZFridge",
            "sbz_Fridge"
        };
        var productXml = Directory.GetFiles(productRoot, "*.xml", SearchOption.AllDirectories)
            .Select(path => new { Path = path, Text = File.ReadAllText(path) })
            .ToArray();
        var productSource = Directory.GetFiles(
                Path.Combine(productRoot, "Source"),
                "*.cs",
                SearchOption.AllDirectories)
            .Select(path => new { Path = path, Text = File.ReadAllText(path) })
            .ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(
                productXml.Where(file => forbiddenCouplingTokens.Any(token =>
                    file.Text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0))
                    .Select(file => file.Path),
                Is.Empty,
                "Passive compatibility must not add any shipped XML, About/load-order, Def, type, or PatchOperation coupling to Adaptive Storage/[sbz].");
            Assert.That(
                productSource.Where(file => forbiddenCouplingTokens.Any(token =>
                    file.Text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0))
                    .Select(file => file.Path),
                Is.Empty,
                "Passive compatibility must not add a reflective package/type adapter that becomes absent or incomplete-chain unsafe.");
        });
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

        throw new DirectoryNotFoundException("Could not locate the Immersive Chefs repository root.");
    }
}
