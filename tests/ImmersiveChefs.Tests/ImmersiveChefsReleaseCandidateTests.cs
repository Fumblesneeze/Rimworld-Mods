using System.Collections;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class ImmersiveChefsReleaseCandidateTests
{
    [Test]
    public void Release_descriptor_and_stager_define_the_published_public_RimWorld_1_6_release()
    {
        var root = FindRepositoryRoot();
        var manifestPath = Path.Combine(root, "mods", "ImmersiveChefs", "Release", "release.json");
        var scriptPath = Path.Combine(root, "scripts", "Build-ImmersiveChefsRelease.ps1");
        var publishScriptPath = Path.Combine(root, "scripts", "Invoke-ImmersiveChefsWorkshopRelease.ps1");
        var publisherFixturePath = Path.Combine(root, "scripts", "Fixtures", "GatewaySteamWorkshopPublisher.cs");
        var subscribedSmokePath = Path.Combine(root, "scripts", "Invoke-ImmersiveChefsSubscribedSmoke.ps1");

        Assert.That(File.Exists(manifestPath), Is.True, "The release descriptor is missing.");
        Assert.That(File.Exists(scriptPath), Is.True, "The release stager is missing.");
        Assert.That(File.Exists(publishScriptPath), Is.True, "The reviewed Workshop publisher is missing.");
        Assert.That(File.Exists(publisherFixturePath), Is.True, "The in-game Steamworks publisher is missing.");
        Assert.That(File.Exists(subscribedSmokePath), Is.True, "The subscribed-copy smoke is missing.");

        var release = (IDictionary<string, object>)new JavaScriptSerializer()
            .DeserializeObject(File.ReadAllText(manifestPath));
        Assert.Multiple(() =>
        {
            Assert.That(release["packageId"], Is.EqualTo("fumblesneeze.immersivechefs"));
            Assert.That(release["title"], Is.EqualTo("Immersive Chefs"));
            Assert.That(release["author"], Is.EqualTo("Fumblesneeze"));
            Assert.That(release["rimWorldVersion"], Is.EqualTo("1.6"));
            Assert.That(release["rimWorldBuild"], Is.EqualTo("1.6.4871 rev590"));
            Assert.That(release["rimWorldRuntimeBuild"], Is.EqualTo("1.6.4871 rev591"));
            Assert.That(release["steamBuildId"], Is.EqualTo("23969874"));
            Assert.That(release["managedAssemblySha256"], Is.EqualTo("5CF1B5BE399D5B1C9C56CA72C9D35B4ECF307FEACF5859D04AC5A1AA5926356A"));
            Assert.That(release["steamUserId"], Is.EqualTo("76561198077136238"));
            Assert.That(release["visibility"], Is.EqualTo("Public"));
            Assert.That(release["allowFirstPublication"], Is.False);
            Assert.That(release["publishedFileId"], Is.EqualTo("3782589902"));
            Assert.That(release["previousChangeNote"], Is.EqualTo("Dirty kitchenware is now much easier to spot. Dubs Bad Hygiene sinks, prep stations, and dishwashing now use real water; meals spawned without plates no longer interrupt eating or patient feeding; and Ceramics (Continued) porcelain can be crafted into plates."));
            Assert.That(
                release["changeNote"],
                Is.EqualTo("Dishwashers can now accept new loads while already washing, and Common Sense cooks and diners prefer available dishwashers. Meal reheating works reliably from inventory and at stoves, campfires, heaters, and the fallback microwave; microwave artwork and facing are corrected. Several cooking, feeding, and ware-placement bugs were fixed, and Immersive Chefs' added food-poisoning chances were halved."),
                "The pending Workshop update must carry its exact reviewed player-facing change note.");
            Assert.That(
                ((IEnumerable)release["requiredWorkshopItems"]).Cast<object>().Select(value => value.ToString()),
                Is.EqualTo(new[] { "2009463077", "2574315206" }));
        });

        var script = File.ReadAllText(scriptPath);
        Assert.Multiple(() =>
        {
            Assert.That(script, Does.Contain("git status --porcelain"));
            Assert.That(script, Does.Contain("ImmersiveChefs.pdb"));
            Assert.That(script, Does.Contain("package-files.json"));
            Assert.That(script, Does.Contain("publication-plan.json"));
            Assert.That(script, Does.Contain("UseArtifactsOutput=true"));
            Assert.That(script, Does.Contain("managedAssemblySha256"));
            Assert.That(script, Does.Contain("description.bbcode"));
            Assert.That(script, Does.Contain("preview-main.png"));
        });

        var publisher = File.ReadAllText(publishScriptPath);
        Assert.Multiple(() =>
        {
            Assert.That(publisher, Does.Contain("publish immersive chefs to steam workshop"));
            Assert.That(publisher, Does.Contain("-InteractiveCompletionFile"));
            Assert.That(publisher, Does.Contain("GatewaySteamWorkshopPublisher.cs"));
            Assert.That(publisher, Does.Contain("sourceRevision"));
            Assert.That(publisher, Does.Contain("descriptionSha256"));
            Assert.That(publisher, Does.Contain("RemoteOwnerSteamId"));
            Assert.That(publisher, Does.Contain("reconcileOnly"));
            Assert.That(publisher, Does.Contain("publication-state.txt"));
            Assert.That(publisher, Does.Contain("Invoke-ImmersiveChefsSubscribedSmoke.ps1"));
            Assert.That(publisher, Does.Contain("publication-receipt.json"));
        });

        var publisherFixture = File.ReadAllText(publisherFixturePath);
        Assert.Multiple(() =>
        {
            Assert.That(publisherFixture, Does.Contain("SteamUGC.CreateItem"));
            Assert.That(publisherFixture, Does.Contain("SteamUGC.SubmitItemUpdate"));
            Assert.That(publisherFixture, Does.Contain("SteamUGC.AddDependency"));
            Assert.That(publisherFixture, Does.Contain("SteamUGC.RemoveDependency"));
            Assert.That(publisherFixture, Does.Contain("SteamUGC.SubscribeItem"));
            Assert.That(publisherFixture, Does.Contain("SteamUGC.GetQueryUGCResult"));
            Assert.That(publisherFixture, Does.Contain("RequireExistingPreflight"));
            Assert.That(publisherFixture, Does.Contain("submit-admitted"));
            Assert.That(publisherFixture, Does.Contain("DataContractJsonSerializer"));
        });

        var subscribedSmoke = File.ReadAllText(subscribedSmokePath);
        Assert.Multiple(() =>
        {
            Assert.That(subscribedSmoke, Does.Contain("release.immersive-chefs-subscribed-native-cooking-dining"));
            Assert.That(subscribedSmoke, Does.Contain("RimWorldDevGateway.ReleaseSmoke.EndToEndTests.csproj"));
            Assert.That(subscribedSmoke, Does.Contain("native Prioritize and Consume float-menu callbacks"));
            Assert.That(subscribedSmoke, Does.Contain("Move-Item -LiteralPath $backupProduct -Destination $localProduct"));
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

        throw new DirectoryNotFoundException("Could not find the repository root.");
    }
}
