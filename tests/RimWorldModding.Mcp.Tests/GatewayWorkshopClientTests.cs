using NUnit.Framework;
using System.Text.Json;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class GatewayWorkshopClientTests
{
    [TestCase("{\"programState\":\"Entry\",\"map\":null,\"longEventActive\":false}", false)]
    [TestCase("{\"programState\":\"Playing\",\"map\":null,\"longEventActive\":false}", false)]
    [TestCase("{\"programState\":\"Entry\",\"map\":{\"Handle\":\"map-0\"},\"longEventActive\":false}", false)]
    [TestCase("{\"programState\":\"Playing\",\"map\":{\"Handle\":\"map-0\"}}", false)]
    [TestCase("{\"programState\":\"Playing\",\"map\":{\"Handle\":\"map-0\"},\"longEventActive\":true}", false)]
    [TestCase("{\"programState\":\"Playing\",\"map\":{\"Handle\":\"map-0\"},\"longEventActive\":false}", true)]
    public void PlayableStatus_RequiresPlayingStateCurrentMapAndNoLongEvent(string json, bool expected)
    {
        using var document = JsonDocument.Parse(json);

        Assert.That(GatewayWorkshopClient.IsPlayableStatus(document.RootElement), Is.EqualTo(expected));
    }

    [Test]
    public void PlayableMapPoll_BoundsEveryCallAndDelayByTheRemainingDeadline()
    {
        var now = DateTimeOffset.Parse("2026-08-24T00:00:00Z");
        var callBudgets = new List<TimeSpan>();
        var delayBudgets = new List<TimeSpan>();

        Assert.That(async () => await GatewayWorkshopClient.PollUntilPlayableMapAsync(
                now.AddMilliseconds(750),
                (budget, _) =>
                {
                    callBudgets.Add(budget);
                    now = now.AddMilliseconds(300);
                    return Task.FromResult(ParseOuterStatus(
                        "{\"programState\":\"Playing\",\"map\":null,\"longEventActive\":false}"));
                },
                (budget, _) =>
                {
                    delayBudgets.Add(budget);
                    now = now.Add(budget);
                    return Task.CompletedTask;
                },
                () => now,
                CancellationToken.None),
            Throws.InstanceOf<TimeoutException>());
        Assert.That(callBudgets, Is.EqualTo(new[] { TimeSpan.FromMilliseconds(750) }));
        Assert.That(delayBudgets, Is.EqualTo(new[] { TimeSpan.FromMilliseconds(450) }));
    }

    [Test]
    public void PlayableMapPoll_PropagatesCancellationIntoAnInFlightStatusCall()
    {
        using var cancelled = new CancellationTokenSource();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var poll = GatewayWorkshopClient.PollUntilPlayableMapAsync(
            DateTimeOffset.UtcNow.AddMinutes(1),
            async (_, token) =>
            {
                started.SetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return default;
            },
            Task.Delay,
            () => DateTimeOffset.UtcNow,
            cancelled.Token);

        Assert.That(async () => await started.Task.WaitAsync(TimeSpan.FromSeconds(1)), Throws.Nothing);
        cancelled.Cancel();
        Assert.That(async () => await poll, Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public void DeadlineBoundOperation_TimesOutCompanionAcquisitionAndPreservesCallerCancellation()
    {
        Assert.That(async () => await GatewayWorkshopClient.RunWithinDeadlineAsync(
                TimeSpan.FromMilliseconds(100),
                async token =>
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, token);
                    return 1;
                },
                CancellationToken.None),
            Throws.InstanceOf<TimeoutException>());

        using var cancelled = new CancellationTokenSource();
        var operation = GatewayWorkshopClient.RunWithinDeadlineAsync(
            TimeSpan.FromMinutes(1),
            async token =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return 1;
            },
            cancelled.Token);
        cancelled.Cancel();

        Assert.That(async () => await operation, Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public void CompanionBuildPlan_IsRepositoryLocalAndProducesTheExactRequiredOutputs()
    {
        var root = TestRepository.FindRoot();

        var plan = GatewayWorkshopClient.CreateCompanionBuildPlan(root);

        Assert.That(plan.ProjectPath, Is.EqualTo(Path.Combine(
            root, "tools", "RimWorldDevGateway.Client", "RimWorldDevGateway.Client.csproj")));
        Assert.That(plan.OutputDirectory, Does.StartWith(Path.Combine(
            root, "artifacts", "HostTools", "RimWorldModdingMcp", "GatewayCompanion")));
        Assert.That(plan.InputFingerprint, Does.Match("^[A-F0-9]{64}$"));
        Assert.That(plan.ClientPath, Is.EqualTo(Path.Combine(plan.OutputDirectory, "RimWorldDevGateway.Client.exe")));
        Assert.That(plan.ContractPath, Is.EqualTo(Path.Combine(plan.OutputDirectory, "RimWorldDevGateway.Contracts.dll")));
        var staging = Path.Combine(plan.CacheRoot, "test-staging");
        Assert.That(GatewayWorkshopClient.CreateCompanionBuildArguments(plan, staging), Is.EqualTo(new[]
        {
            "build", plan.ProjectPath, "--configuration", "Release", "--framework", "net480",
            "--output", staging, "--nologo", "--verbosity", "minimal"
        }));
    }

    [Test]
    public void CompanionBuildPlan_GlobalJsonChangeChangesInputFingerprint()
    {
        var root = CreateMinimalRepository();
        try
        {
            var before = GatewayWorkshopClient.CreateCompanionBuildPlan(root);
            File.WriteAllText(Path.Combine(root, "global.json"),
                "{ \"sdk\": { \"version\": \"8.0.401\", \"rollForward\": \"latestMajor\" } }");

            var after = GatewayWorkshopClient.CreateCompanionBuildPlan(root);

            Assert.That(after.InputFingerprint, Is.Not.EqualTo(before.InputFingerprint));
            Assert.That(after.OutputDirectory, Is.Not.EqualTo(before.OutputDirectory));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task CompanionBuild_MissingStaleCancelledAndConcurrentCallsRemainRetryableAndExact()
    {
        var root = CreateMinimalRepository();
        try
        {
            var first = GatewayWorkshopClient.EnsureCompanionBuiltAsync(root, CancellationToken.None);
            var second = GatewayWorkshopClient.EnsureCompanionBuiltAsync(root, CancellationToken.None);
            var plans = await Task.WhenAll(first, second);

            Assert.That(plans[0].OutputDirectory, Is.EqualTo(plans[1].OutputDirectory));
            Assert.That(GatewayWorkshopClient.IsPublishedCompanionValid(plans[0]), Is.True);

            await File.AppendAllTextAsync(plans[0].ClientPath, "stale");
            Assert.That(GatewayWorkshopClient.IsPublishedCompanionValid(plans[0]), Is.False);
            var repaired = await Task.WhenAll(
                GatewayWorkshopClient.EnsureCompanionBuiltAsync(root, CancellationToken.None),
                GatewayWorkshopClient.EnsureCompanionBuiltAsync(root, CancellationToken.None));
            Assert.That(repaired.All(GatewayWorkshopClient.IsPublishedCompanionValid), Is.True);

            await File.AppendAllTextAsync(plans[0].ClientPath, "stale-before-cancellation");
            var projectDirectory = Path.GetDirectoryName(plans[0].ProjectPath)!;
            File.WriteAllText(Path.Combine(projectDirectory, "delay.flag"), "delay");
            using var cancelled = new CancellationTokenSource();
            var cancelledBuild = GatewayWorkshopClient.EnsureCompanionBuiltAsync(root, cancelled.Token);
            await WaitForStagingAsync(plans[0].CacheRoot);
            cancelled.Cancel();
            Assert.That(async () => await cancelledBuild, Throws.InstanceOf<OperationCanceledException>());
            Assert.That(Directory.EnumerateDirectories(plans[0].CacheRoot, "*.staging-*"), Is.Empty);
            File.Delete(Path.Combine(projectDirectory, "delay.flag"));

            File.WriteAllText(Path.Combine(projectDirectory, "fail.flag"), "fail");
            Assert.That(async () => await GatewayWorkshopClient.EnsureCompanionBuiltAsync(root, CancellationToken.None),
                Throws.InstanceOf<InvalidOperationException>().With.Message.Contains("induced build failure"));
            Assert.That(Directory.EnumerateDirectories(plans[0].CacheRoot, "*.staging-*"), Is.Empty);
            File.Delete(Path.Combine(projectDirectory, "fail.flag"));

            var recovered = await GatewayWorkshopClient.EnsureCompanionBuiltAsync(root, CancellationToken.None);
            Assert.That(GatewayWorkshopClient.IsPublishedCompanionValid(recovered), Is.True);
            Assert.That(Directory.EnumerateDirectories(recovered.CacheRoot, "*.staging-*"), Is.Empty);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateMinimalRepository()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "gateway-companion-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "mods"));
        Directory.CreateDirectory(Path.Combine(root, "openspec"));
        Directory.CreateDirectory(Path.Combine(root, "tools", "RimWorldDevGateway.Client"));
        Directory.CreateDirectory(Path.Combine(root, "shared", "RimWorldDevGateway.Contracts"));
        File.WriteAllText(Path.Combine(root, "AGENTS.md"), "fixture");
        File.WriteAllText(Path.Combine(root, "Directory.Build.props"), "<Project />");
        File.WriteAllText(Path.Combine(root, "Directory.Build.targets"),
            "<Project><Target Name=\"FixtureControl\" BeforeTargets=\"CoreCompile\">" +
            "<Error Condition=\"Exists('fail.flag')\" Text=\"induced build failure\" />" +
            "<Exec Condition=\"Exists('delay.flag')\" Command=\"powershell -NoProfile -NonInteractive -Command &quot;Start-Sleep -Seconds 30&quot;\" />" +
            "</Target></Project>");
        File.WriteAllText(Path.Combine(root, "global.json"),
            "{ \"sdk\": { \"version\": \"8.0.401\", \"rollForward\": \"latestFeature\" } }");
        File.WriteAllText(Path.Combine(root, "shared", "RimWorldDevGateway.Contracts", "RimWorldDevGateway.Contracts.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net480</TargetFramework><AssemblyName>RimWorldDevGateway.Contracts</AssemblyName></PropertyGroup></Project>");
        File.WriteAllText(Path.Combine(root, "shared", "RimWorldDevGateway.Contracts", "Contract.cs"),
            "namespace RimWorldDevGateway.Contracts { public static class Contract { public const string Value = \"ok\"; } }");
        File.WriteAllText(Path.Combine(root, "tools", "RimWorldDevGateway.Client", "RimWorldDevGateway.Client.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net480</TargetFramework><AssemblyName>RimWorldDevGateway.Client</AssemblyName></PropertyGroup><ItemGroup><ProjectReference Include=\"..\\..\\shared\\RimWorldDevGateway.Contracts\\RimWorldDevGateway.Contracts.csproj\" /></ItemGroup></Project>");
        File.WriteAllText(Path.Combine(root, "tools", "RimWorldDevGateway.Client", "Program.cs"),
            "using RimWorldDevGateway.Contracts; class Program { static void Main() { System.Console.Write(Contract.Value); } }");
        return root;
    }

    private static async Task WaitForStagingAsync(string cacheRoot)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (Directory.Exists(cacheRoot) && Directory.EnumerateDirectories(cacheRoot, "*.staging-*").Any()) return;
            await Task.Delay(25);
        }
        throw new TimeoutException("The cancellable companion build never created its isolated staging directory.");
    }

    private static JsonElement ParseOuterStatus(string statusJson)
    {
        using var document = JsonDocument.Parse("{\"ok\":true,\"result\":" + statusJson + "}");
        return document.RootElement.Clone();
    }
}
