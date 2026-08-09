using System.IO;
using System.Xml.Linq;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class PackageIdentityContractTests
{
    private const string ExpectedPackageId = "fumblesneeze.rimworlddevgateway";

    [Test]
    public void ProjectRuntimeAboutManifestAndSharedContractUseStablePackageIdentity()
    {
        var root = FindRepositoryRoot();
        var project = XDocument.Load(Path.Combine(root, "mods", "RimWorldDevGateway", "RimWorldDevGateway.csproj"));
        var about = XDocument.Load(Path.Combine(root, "mods", "RimWorldDevGateway", "About", "About.xml"));

        Assert.Multiple(() =>
        {
            Assert.That(
                project.Descendants("RimWorldPackageId").Single().Value,
                Is.EqualTo(ExpectedPackageId));
            Assert.That(RimWorldDevGatewayMod.PackageId, Is.EqualTo(ExpectedPackageId));
            Assert.That(
                about.Root?.Element("packageId")?.Value,
                Is.EqualTo(ExpectedPackageId));
            Assert.That(
                EndToEndTesting.EndToEndTestContract.GatewayPackageId,
                Is.EqualTo(ExpectedPackageId));
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
