using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class ImmersiveChefsRequiredDependencyContractTests
{
    private const string OwnerPackageId = "fumblesneeze.immersivechefs";

    [Test]
    public void Maintained_exact_game_groups_load_xml_extensions_after_core()
    {
        var root = FindRepositoryRoot();
        var failures = new List<string>();
        var requiredOrder = new Regex(
            "(?:EndToEndTestContract\\.CorePackageId|\"ludeon\\.rimworld\")\\s*,\\s*\"imranfish\\.xmlextensions\"",
            RegexOptions.CultureInvariant);
        var attribute = new Regex(
            @"\[RimWorldEndToEndTest\((?<arguments>.*?)\)\]",
            RegexOptions.CultureInvariant | RegexOptions.Singleline);

        var e2eSources = Directory
            .EnumerateFiles(Path.Combine(root, "tests", "ImmersiveChefs.EndToEndTests"), "*.cs")
            .Append(Path.Combine(
                root,
                "tests",
                "RimWorldDevGateway.ReleaseSmoke.EndToEndTests",
                "SubscribedImmersiveChefsSmokeTest.cs"));
        foreach (var sourcePath in e2eSources)
        {
            foreach (Match match in attribute.Matches(File.ReadAllText(sourcePath)))
            {
                var arguments = match.Groups["arguments"].Value;
                if (arguments.Contains($"\"{OwnerPackageId}\"") && !requiredOrder.IsMatch(arguments))
                {
                    failures.Add(RelativeTo(root, sourcePath));
                }
            }
        }

        foreach (var manifestPath in Directory.EnumerateFiles(
                     Path.Combine(root, "tests"),
                     "*.integrationtests.json",
                     SearchOption.AllDirectories)
                 .Where(path => !path.Split(Path.DirectorySeparatorChar)
                     .Any(segment => segment == "bin" || segment == "obj")))
        {
            var manifest = File.ReadAllText(manifestPath);
            if (manifest.Contains($"\"ownerPackageId\": \"{OwnerPackageId}\"") &&
                !requiredOrder.IsMatch(manifest))
            {
                failures.Add(RelativeTo(root, manifestPath));
            }
        }

        Assert.That(
            failures.Distinct().OrderBy(path => path),
            Is.Empty,
            "Every exact Immersive Chefs game group must load XML Extensions immediately after Core.");
    }

    private static string RelativeTo(string root, string path) =>
        path.Substring(root.TrimEnd(Path.DirectorySeparatorChar).Length)
            .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RimWorldMods.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
