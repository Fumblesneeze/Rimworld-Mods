using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class ReleaseCandidateTests
{
    [Test]
    public void OwnerScan_RequiresCompleteExactTitleAbsence()
    {
        const string html = """
            <div class="workshopBrowsePagingInfo">Showing 1-1 of 1 entries</div>
            <a data-publishedfileid="3782589902"><div class="workshopItemTitle ellipsis">Immersive Chefs</div></a>
            """;

        var scan = SteamWorkshopOwnerScanner.ParsePage(html);
        Assert.That(scan.Total, Is.EqualTo(1));
        Assert.That(scan.Items.Single().PublishedFileId, Is.EqualTo("3782589902"));
        Assert.That(scan.Items.Single().Title, Is.EqualTo("Immersive Chefs"));
        Assert.That(scan.Items.Count(item => item.Title == "Hospitality + Ideology Patch"), Is.Zero);
    }

    [Test]
    public void Candidate_StagesOnlyDeclaredFilesAndDigestChangesWithContent()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"candidate-{Guid.NewGuid():N}");
        var source = Path.Combine(root, "source");
        var first = Path.Combine(root, "first");
        var second = Path.Combine(root, "second");
        Directory.CreateDirectory(Path.Combine(source, "About"));
        Directory.CreateDirectory(Path.Combine(source, "1.6", "Assemblies"));
        File.WriteAllText(Path.Combine(source, "About", "About.xml"), "about");
        File.WriteAllText(Path.Combine(source, "1.6", "Assemblies", "Product.dll"), "one");
        File.WriteAllText(Path.Combine(source, "1.6", "Assemblies", "Product.pdb"), "forbidden");
        try
        {
            var a = ReleaseCandidateBuilder.Stage(
                source,
                first,
                ["About/About.xml", "1.6/Assemblies/Product.dll"]);
            File.WriteAllText(Path.Combine(source, "1.6", "Assemblies", "Product.dll"), "two");
            var b = ReleaseCandidateBuilder.Stage(
                source,
                second,
                ["About/About.xml", "1.6/Assemblies/Product.dll"]);

            Assert.That(a.Files.Select(file => file.Path), Is.EqualTo(new[] { "1.6/Assemblies/Product.dll", "About/About.xml" }));
            Assert.That(File.Exists(Path.Combine(first, "1.6", "Assemblies", "Product.pdb")), Is.False);
            Assert.That(a.ContentDigest, Is.Not.EqualTo(b.ContentDigest));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Candidate_RejectsDirectoryReparsePointsInsteadOfFollowingThem()
    {
        Assert.That(
            Assert.Throws<InvalidOperationException>(() => ReleaseCandidateBuilder.RejectReparseAttributes(
                FileAttributes.Directory | FileAttributes.ReparsePoint, "linked/"))!.Message,
            Does.Contain("reparse point"));
        Assert.DoesNotThrow(() => ReleaseCandidateBuilder.RejectReparseAttributes(
            FileAttributes.Directory, "ordinary/"));
    }

    [Test]
    public void Candidate_InspectsEveryIntermediateIncludePathBeforeFollowingIt()
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.WorkDirectory, "source"));
        var candidate = Path.Combine(root, "linked", "nested", "Product.dll");

        Assert.That(
            ReleaseCandidateBuilder.PathsToInspect(root, candidate),
            Is.EqualTo(new[]
            {
                root,
                Path.Combine(root, "linked"),
                Path.Combine(root, "linked", "nested"),
                candidate
            }));
    }

    [Test]
    public void ProductBoundary_TrustsOnlyExactPlatformAssemblyNames()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ReleasePackageValidator.IsPlatformReference("System.Core"), Is.True);
            Assert.That(ReleasePackageValidator.IsPlatformReference("System.Drawing"), Is.True);
            Assert.That(ReleasePackageValidator.IsPlatformReference("System.Net.Http"), Is.True);
            Assert.That(ReleasePackageValidator.IsPlatformReference("UnityEngine.CoreModule"), Is.True);
            Assert.That(ReleasePackageValidator.IsPlatformReference("UnityEngine.UI"), Is.True);
            Assert.That(ReleasePackageValidator.IsPlatformReference("UnityEngine.UIModule"), Is.True);
            Assert.That(ReleasePackageValidator.IsPlatformReference("System.Impostor"), Is.False);
            Assert.That(ReleasePackageValidator.IsPlatformReference("UnityEngine.Impostor"), Is.False);
        });
    }

    [Test]
    public void RemoteBaseline_BindsPreviewBytesButNotEphemeralCdnUrl()
    {
        WorkshopRemoteBaseline Create(string url, string previewHash) => WorkshopRemoteBaseline.Create(
            "1234567890", "Example", new string('A', 64), 12, ["Mod"], "metadata", "Public",
            url, previewHash, "76561198077136238", 294100, 1234, 5678, ["2009463077"], [], []);

        var first = Create("https://cdn.example/preview?token=one", new string('B', 64));
        var refreshedUrl = Create("https://cdn.example/preview?token=two", new string('B', 64));
        var changedBytes = Create("https://cdn.example/preview?token=two", new string('C', 64));

        Assert.That(refreshedUrl.StateDigest, Is.EqualTo(first.StateDigest));
        Assert.That(changedBytes.StateDigest, Is.Not.EqualTo(first.StateDigest));
    }

    [Test]
    public void RemoteBaseline_BindsAndDiffsSteamApplicationDependenciesSeparately()
    {
        var root = TestRepository.FindRoot();
        var profile = ReleaseProfileCatalog.Discover(root)
            .Single(item => item.PackageId == "fumblesneeze.guestbedgizmo");
        var baseline = WorkshopRemoteBaseline.Create(
            profile.PublishedFileId!, profile.Title, new string('A', 64), 12, ["Mod", "1.6"],
            "metadata", "Private", "https://cdn.example/preview", new string('B', 64),
            profile.SteamUserId, profile.SteamAppId, 1234, 5678, profile.RequiredWorkshopItems, [], []);
        var candidate = new ReleaseCandidateStage("package", new string('C', 64), []);

        var appDependencies = typeof(WorkshopRemoteBaseline).GetProperty("AppDependencies");
        var diff = WorkshopRemoteBaseline.DescribeDiff(baseline, profile, candidate);

        Assert.That(appDependencies, Is.Not.Null);
        Assert.That(diff, Has.One.StartsWith("APP DEPENDENCIES").And.Contains("1392840"));
    }

    [Test]
    public void ExistingItemIdentity_AllowsAnAuthenticatedTitleChange()
    {
        var root = TestRepository.FindRoot();
        var profile = ReleaseProfileCatalog.Discover(root)
            .Single(item => item.PackageId == "fumblesneeze.guestbedgizmo");
        var baseline = WorkshopRemoteBaseline.Create(
            profile.PublishedFileId!, "Prior title", new string('A', 64), 12, ["Mod", "1.6"],
            "metadata", "Private", "https://cdn.example/preview", new string('B', 64),
            profile.SteamUserId, profile.SteamAppId, 1234, 5678, profile.RequiredWorkshopItems, [], []);

        Assert.DoesNotThrow(() => baseline.AssertItemIdentity(profile));
        Assert.That(WorkshopRemoteBaseline.DescribeDiff(
                baseline, profile, new ReleaseCandidateStage("package", new string('C', 64), [])),
            Has.One.EqualTo($"TITLE [Prior title] -> [{profile.Title}]"));
    }

    [Test]
    public void RelationshipReconciliation_ProducesSeparateExactWorkshopAndApplicationOperations()
    {
        var reconciler = typeof(ReleasePreparer).Assembly.GetType(
            "RimWorldModding.Mcp.WorkshopRelationshipReconciler", throwOnError: false);
        var plan = reconciler?.GetMethod("Plan", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        Assert.That(reconciler, Is.Not.Null);
        Assert.That(plan, Is.Not.Null);
        Assert.That(
            (string[])plan!.Invoke(null, new object[] { "dependency", new ulong[] { 2 }, new ulong[] { 1 } })!,
            Is.EqualTo(new[] { "dependency-remove:1", "dependency-add:2" }));
        Assert.That(
            (string[])plan.Invoke(null, new object[] { "app-dependency", new ulong[] { 1392840 }, Array.Empty<ulong>() })!,
            Is.EqualTo(new[] { "app-dependency-add:1392840" }));
    }

    [Test]
    public void ProductBoundary_RejectsBundledAssembliesAndAboutIdentityDrift()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"product-boundary-{Guid.NewGuid():N}");
        var package = Path.Combine(root, "package");
        Directory.CreateDirectory(Path.Combine(package, "About"));
        Directory.CreateDirectory(Path.Combine(package, "1.6", "Assemblies"));
        var project = Path.Combine(root, "Example.csproj");
        File.WriteAllText(project, "<Project><PropertyGroup><AssemblyName>RimWorldModding.Mcp</AssemblyName><IncludeHarmony>false</IncludeHarmony></PropertyGroup></Project>");
        File.WriteAllText(Path.Combine(package, "About", "About.xml"), """
            <ModMetaData>
              <name>Example</name><author>Fumblesneeze</author><packageId>example.mod</packageId>
              <supportedVersions><li>1.6</li></supportedVersions>
              <modDependencies></modDependencies><loadAfter></loadAfter>
            </ModMetaData>
            """);
        File.Copy(typeof(ReleaseCandidateBuilder).Assembly.Location,
            Path.Combine(package, "1.6", "Assemblies", "RimWorldModding.Mcp.dll"));
        var description = Path.Combine(root, "description.txt");
        var preview = Path.Combine(root, "preview.png");
        File.WriteAllText(description, "description");
        File.WriteAllText(preview, "preview");
        var profile = new ReleaseProfile(
            "RimWorldModRelease/v1", Path.Combine(root, "release.json"), project,
            "example.mod", "Example", "Fumblesneeze", "Product", "1.6",
            "1.6.4871 rev590", "1.6.4871 rev591", "23969874", new string('A', 64),
            294100, "76561198077136238", null, true, "Private", ["Mod", "1.6"], [], [], CustomAssemblyReferences(),
            "mod_build", "presentation_render", package, ["About/", "1.6/"], description,
            preview, null, "Initial release.");
        try
        {
            var valid = ReleaseCandidateBuilder.Inspect(package, profile.PackageInclude);
            Assert.DoesNotThrow(() => ReleasePackageValidator.Validate(profile, valid));

            var undeclared = profile with { HardRuntimeAssemblyReferences = [] };
            Assert.That(
                Assert.Throws<InvalidOperationException>(() => ReleasePackageValidator.Validate(undeclared, valid))!.Message,
                Does.Contain("undeclared runtime AssemblyRef"));

            File.Copy(typeof(ReleaseCandidateBuilder).Assembly.Location,
                Path.Combine(package, "1.6", "Assemblies", "0Harmony.dll"));
            var bundled = ReleaseCandidateBuilder.Inspect(package, profile.PackageInclude);
            Assert.That(
                Assert.Throws<InvalidOperationException>(() => ReleasePackageValidator.Validate(profile, bundled))!.Message,
                Does.Contain("exactly its one declared product assembly"));
            File.Delete(Path.Combine(package, "1.6", "Assemblies", "0Harmony.dll"));

            var about = Path.Combine(package, "About", "About.xml");
            File.WriteAllText(about, File.ReadAllText(about).Replace("example.mod", "wrong.mod"));
            var drifted = ReleaseCandidateBuilder.Inspect(package, profile.PackageInclude);
            Assert.That(
                Assert.Throws<InvalidOperationException>(() => ReleasePackageValidator.Validate(profile, drifted))!.Message,
                Does.Contain("identity"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void ProductBoundary_AllowsSteamRequirementForOptionalLoadAfterMod()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"optional-steam-dependency-{Guid.NewGuid():N}");
        var package = Path.Combine(root, "package");
        Directory.CreateDirectory(Path.Combine(package, "About"));
        Directory.CreateDirectory(Path.Combine(package, "1.6", "Assemblies"));
        var project = Path.Combine(root, "Example.csproj");
        File.WriteAllText(project, """
            <Project>
              <PropertyGroup><AssemblyName>RimWorldModding.Mcp</AssemblyName><IncludeHarmony>false</IncludeHarmony></PropertyGroup>
              <ItemGroup><RimWorldLoadAfter Include="Orion.Hospitality" /></ItemGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(package, "About", "About.xml"), """
            <ModMetaData>
              <name>Example</name><author>Fumblesneeze</author><packageId>example.mod</packageId>
              <supportedVersions><li>1.6</li></supportedVersions>
              <modDependencies></modDependencies>
              <loadAfter><li>Orion.Hospitality</li></loadAfter>
            </ModMetaData>
            """);
        File.Copy(typeof(ReleaseCandidateBuilder).Assembly.Location,
            Path.Combine(package, "1.6", "Assemblies", "RimWorldModding.Mcp.dll"));
        var description = Path.Combine(root, "description.txt");
        var preview = Path.Combine(root, "preview.png");
        File.WriteAllText(description, "description");
        File.WriteAllText(preview, "preview");
        var profile = new ReleaseProfile(
            "RimWorldModRelease/v1", Path.Combine(root, "release.json"), project,
            "example.mod", "Example", "Fumblesneeze", "Product", "1.6",
            "1.6.4871 rev590", "1.6.4871 rev591", "23969874", new string('A', 64),
            294100, "76561198077136238", null, true, "Private", ["Mod", "1.6"], ["3509486825"], [], CustomAssemblyReferences(),
            "mod_build", "presentation_render", package, ["About/", "1.6/"], description,
            preview, null, "Initial release.");
        try
        {
            var candidate = ReleaseCandidateBuilder.Inspect(package, profile.PackageInclude);
            Assert.DoesNotThrow(() => ReleasePackageValidator.Validate(profile, candidate));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string[] CustomAssemblyReferences() => typeof(ReleaseCandidateBuilder).Assembly
        .GetReferencedAssemblies()
        .Select(reference => reference.Name!)
        .Where(reference => !ReleasePackageValidator.IsPlatformReference(reference))
        .ToArray();
}
