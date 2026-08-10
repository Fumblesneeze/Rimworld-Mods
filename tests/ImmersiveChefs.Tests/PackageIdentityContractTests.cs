using System.IO;
using System.Xml.Linq;
using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class PackageIdentityContractTests
{
    private const string ExpectedAuthor = "Fumblesneeze";
    private const string ExpectedPackageId = "fumblesneeze.immersivechefs";

    [Test]
    public void ProjectRuntimeAndAboutManifestUseStablePackageIdentity()
    {
        var root = FindRepositoryRoot();
        var project = XDocument.Load(Path.Combine(root, "mods", "ImmersiveChefs", "ImmersiveChefs.csproj"));
        var about = XDocument.Load(Path.Combine(root, "mods", "ImmersiveChefs", "About", "About.xml"));

        Assert.Multiple(() =>
        {
            Assert.That(
                project.Descendants("RimWorldPackageId").Single().Value,
                Is.EqualTo(ExpectedPackageId));
            Assert.That(project.Descendants("Authors").Single().Value, Is.EqualTo(ExpectedAuthor));
            Assert.That(ImmersiveChefsMod.PackageId, Is.EqualTo(ExpectedPackageId));
            Assert.That(
                about.Root?.Element("packageId")?.Value,
                Is.EqualTo(ExpectedPackageId));
            Assert.That(about.Root?.Element("author")?.Value, Is.EqualTo(ExpectedAuthor));
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
