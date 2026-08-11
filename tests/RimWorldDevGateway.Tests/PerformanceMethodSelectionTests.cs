using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using RimWorldDevGateway.Performance;
using RimWorldDevGateway.PerformanceTesting;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class PerformanceMethodSelectionTests
{
    private static readonly Assembly FixtureAssembly = typeof(PerformanceMethodSelectionTests).Assembly;

    [Test]
    public void Harmony_owner_selects_runtime_prefix_postfix_and_finalizer_but_retains_transpiler_as_unsupported()
    {
        var target = Method(typeof(PatchFixtures), nameof(PatchFixtures.Target));
        var prefix = Method(typeof(PatchFixtures), nameof(PatchFixtures.Prefix));
        var postfix = Method(typeof(PatchFixtures), nameof(PatchFixtures.Postfix));
        var finalizer = Method(typeof(PatchFixtures), nameof(PatchFixtures.Finalizer));
        var transpiler = Method(typeof(PatchFixtures), nameof(PatchFixtures.Transpiler));
        var catalog = new FakeHarmonyCatalog(new[]
        {
            new PerformanceHarmonyPatch("product.owner", PerformanceHarmonyPatchKind.Prefix, target, prefix),
            new PerformanceHarmonyPatch("product.owner", PerformanceHarmonyPatchKind.Postfix, target, postfix),
            new PerformanceHarmonyPatch("product.owner", PerformanceHarmonyPatchKind.Finalizer, target, finalizer),
            new PerformanceHarmonyPatch("product.owner", PerformanceHarmonyPatchKind.Transpiler, target, transpiler),
            new PerformanceHarmonyPatch("other.owner", PerformanceHarmonyPatchKind.Prefix, target, transpiler)
        });

        var result = Resolve(
            catalog,
            new PerformanceSelectionRequest(
                PerformanceMethodSelectorKind.HarmonyOwner,
                "product.owner",
                "harmony"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Methods.Select(item => item.Method),
                Is.EqualTo(new[] { finalizer, postfix, prefix }.OrderBy(PerformanceMethodIdentity.Of)));
            Assert.That(result.Unsupported, Has.Count.EqualTo(1));
            Assert.That(result.Unsupported[0].Method, Is.SameAs(transpiler));
            Assert.That(result.Unsupported[0].PatchedTargets, Is.EqualTo(new[] { target }));
            Assert.That(result.Unsupported[0].Reason, Does.Contain("transpiler").And.Contain("runtime"));
        });
    }

    [Test]
    public void Tick_override_and_explicit_method_selectors_deduplicate_one_exact_method_and_retain_reasons()
    {
        var tick = Method(typeof(TickDerived), nameof(TickDerived.Tick));
        var result = Resolve(
            new FakeHarmonyCatalog(Array.Empty<PerformanceHarmonyPatch>()),
            new PerformanceSelectionRequest(
                PerformanceMethodSelectorKind.TickOverrides,
                FixtureAssembly.GetName().Name!,
                "ticks"),
            new PerformanceSelectionRequest(
                PerformanceMethodSelectorKind.Method,
                typeof(TickDerived).FullName + "::Tick",
                "explicit"));

        var selected = result.Methods.Single(item => item.Method == tick);
        var fixtureOverrides = result.Methods
            .Where(item => item.Method.DeclaringType == typeof(TickDerived))
            .Select(item => item.Method.Name)
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(result.Methods.Count(item => item.Method == tick), Is.EqualTo(1));
            Assert.That(selected.Categories, Is.EqualTo(new[] { "explicit", "ticks" }));
            Assert.That(selected.SelectionReasons.Count(item => item.Contains("explicit method")), Is.EqualTo(1));
            Assert.That(selected.SelectionReasons.Count(item => item.Contains("tick override")), Is.EqualTo(1));
            Assert.That(result.Methods.Any(item => item.Method.Name == nameof(TickDerived.Helper)), Is.False);
            Assert.That(fixtureOverrides, Is.EqualTo(new[]
            {
                "CompTick",
                "GameComponentTick",
                "MapComponentTick",
                "Tick",
                "TickLong",
                "TickRare",
                "WorldComponentTick"
            }));
        });
    }

    [Test]
    public void Type_selector_resolves_supported_declared_methods_and_Circinus_target_remains_separate()
    {
        var result = Resolve(
            new FakeHarmonyCatalog(Array.Empty<PerformanceHarmonyPatch>()),
            new PerformanceSelectionRequest(
                PerformanceMethodSelectorKind.Type,
                typeof(ExplicitTypeFixture).FullName!,
                "type"),
            new PerformanceSelectionRequest(
                PerformanceMethodSelectorKind.CircinusTarget,
                "system.tick",
                "system"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Methods.Select(item => item.Method.Name),
                Is.EqualTo(new[] { nameof(ExplicitTypeFixture.First), nameof(ExplicitTypeFixture.Second) }));
            Assert.That(result.CircinusTargets, Is.EqualTo(new[] { "system.tick" }));
            Assert.That(result.Methods, Has.All.Property(nameof(PerformanceResolvedMethod.AssemblyName))
                .EqualTo(FixtureAssembly.GetName().Name));
            Assert.That(result.Methods, Has.All.Property(nameof(PerformanceResolvedMethod.ModuleVersionId))
                .EqualTo(FixtureAssembly.ManifestModule.ModuleVersionId));
        });
    }

    [TestCase("missing", "does not resolve")]
    [TestCase("overloaded", "ambiguous")]
    [TestCase("generic", "generic")]
    [TestCase("compiler-generated", "compiler-generated")]
    public void Stale_ambiguous_generic_or_compiler_generated_explicit_method_fails_closed(
        string failure,
        string expectedReason)
    {
        var value = failure switch
        {
            "missing" => "Missing.Type::Missing",
            "overloaded" => typeof(UnsupportedFixtures).FullName + "::Overloaded",
            "generic" => typeof(UnsupportedFixtures).FullName + "::Generic",
            "compiler-generated" => CompilerGeneratedSelector(),
            _ => throw new AssertionException("unknown fixture")
        };

        var exception = Assert.Throws<PerformanceMethodSelectionException>(() => Resolve(
            new FakeHarmonyCatalog(Array.Empty<PerformanceHarmonyPatch>()),
            new PerformanceSelectionRequest(PerformanceMethodSelectorKind.Method, value, "explicit")));

        Assert.That(exception!.Message, Does.Contain(expectedReason).IgnoreCase);
    }

    [Test]
    public void Selector_count_uses_the_shared_exact_host_bound_before_resolution()
    {
        var exact = Enumerable.Range(0, PerformanceTestContract.MaximumMethodSelectors)
            .Select(index => new PerformanceSelectionRequest(
                PerformanceMethodSelectorKind.CircinusTarget,
                "target." + index,
                "system"))
            .ToArray();
        Assert.That(Resolve(new FakeHarmonyCatalog(Array.Empty<PerformanceHarmonyPatch>()), exact)
            .CircinusTargets, Has.Count.EqualTo(PerformanceTestContract.MaximumMethodSelectors));

        var over = exact.Append(new PerformanceSelectionRequest(
            PerformanceMethodSelectorKind.CircinusTarget,
            "target.over",
            "system"));
        var exception = Assert.Throws<PerformanceMethodSelectionException>(() => Resolve(
            new FakeHarmonyCatalog(Array.Empty<PerformanceHarmonyPatch>()),
            over.ToArray()));
        Assert.That(exception!.Message, Does.Contain(PerformanceTestContract.MaximumMethodSelectors.ToString()));
    }

    [Test]
    public void Harmony_global_registry_discovery_has_an_exact_published_provider_ceiling()
    {
        var exact = ReflectionPerformanceHarmonyCatalog.MaterializeBounded(
            Enumerable.Range(0, ReflectionPerformanceHarmonyCatalog.MaximumPatchedTargets),
            ReflectionPerformanceHarmonyCatalog.MaximumPatchedTargets,
            "patched targets");
        Assert.That(exact, Has.Count.EqualTo(ReflectionPerformanceHarmonyCatalog.MaximumPatchedTargets));

        var exception = Assert.Throws<PerformanceMethodSelectionException>(() =>
            ReflectionPerformanceHarmonyCatalog.MaterializeBounded(
                Enumerable.Range(0, ReflectionPerformanceHarmonyCatalog.MaximumPatchedTargets + 1),
                ReflectionPerformanceHarmonyCatalog.MaximumPatchedTargets,
                "patched targets"));
        Assert.That(exception!.Message,
            Does.Contain(ReflectionPerformanceHarmonyCatalog.MaximumPatchedTargets.ToString()));

        Assert.That(ReflectionPerformanceHarmonyCatalog.IncrementBounded(
                ReflectionPerformanceHarmonyCatalog.MaximumPatchAttachments - 1,
                ReflectionPerformanceHarmonyCatalog.MaximumPatchAttachments,
                "patch attachments"),
            Is.EqualTo(ReflectionPerformanceHarmonyCatalog.MaximumPatchAttachments));
        var attachmentException = Assert.Throws<PerformanceMethodSelectionException>(() =>
            ReflectionPerformanceHarmonyCatalog.IncrementBounded(
                ReflectionPerformanceHarmonyCatalog.MaximumPatchAttachments,
                ReflectionPerformanceHarmonyCatalog.MaximumPatchAttachments,
                "patch attachments"));
        Assert.That(attachmentException!.Message,
            Does.Contain(ReflectionPerformanceHarmonyCatalog.MaximumPatchAttachments.ToString()));
    }

    [Test]
    public void A_transpiler_selected_through_another_route_never_enters_the_hand_armed_set()
    {
        var target = Method(typeof(PatchFixtures), nameof(PatchFixtures.Target));
        var transpiler = Method(typeof(PatchFixtures), nameof(PatchFixtures.Transpiler));
        var catalog = new FakeHarmonyCatalog(new[]
        {
            new PerformanceHarmonyPatch(
                "product.owner",
                PerformanceHarmonyPatchKind.Transpiler,
                target,
                transpiler)
        });

        var exception = Assert.Throws<PerformanceMethodSelectionException>(() => Resolve(
            catalog,
            new PerformanceSelectionRequest(
                PerformanceMethodSelectorKind.HarmonyOwner,
                "product.owner",
                "harmony"),
            new PerformanceSelectionRequest(
                PerformanceMethodSelectorKind.Method,
                typeof(PatchFixtures).FullName + "::" + nameof(PatchFixtures.Transpiler),
                "explicit")));

        Assert.That(exception!.Message,
            Does.Contain("transpiler").And.Contain("hand-armed").IgnoreCase);
    }

    private static PerformanceMethodSelection Resolve(
        IPerformanceHarmonyCatalog catalog,
        params PerformanceSelectionRequest[] requests) =>
        PerformanceMethodSelectorResolver.Resolve(
            new[] { FixtureAssembly },
            catalog,
            requests);

    private static MethodInfo Method(Type type, string name) =>
        type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                             BindingFlags.Instance | BindingFlags.DeclaredOnly)!;

    private static string CompilerGeneratedSelector()
    {
        var method = Method(typeof(UnsupportedFixtures), nameof(UnsupportedFixtures.Generated));
        if (method.GetCustomAttribute<CompilerGeneratedAttribute>() is null)
            throw new AssertionException("fixture method lost CompilerGeneratedAttribute");
        return typeof(UnsupportedFixtures).FullName + "::" + method.Name;
    }

    private sealed class FakeHarmonyCatalog : IPerformanceHarmonyCatalog
    {
        private readonly IReadOnlyList<PerformanceHarmonyPatch> patches;

        public FakeHarmonyCatalog(IReadOnlyList<PerformanceHarmonyPatch> patches) => this.patches = patches;

        public IReadOnlyList<PerformanceHarmonyPatch> ResolveOwner(string exactOwnerId) =>
            patches.Where(item => string.Equals(item.OwnerId, exactOwnerId, StringComparison.Ordinal)).ToArray();
    }

    private class TickBase
    {
        public virtual void Tick() { }
        public virtual void TickRare() { }
        public virtual void TickLong() { }
        public virtual void CompTick() { }
        public virtual void MapComponentTick() { }
        public virtual void WorldComponentTick() { }
        public virtual void GameComponentTick() { }
    }

    private sealed class TickDerived : TickBase
    {
        public override void Tick() { }
        public override void TickRare() { }
        public override void TickLong() { }
        public override void CompTick() { }
        public override void MapComponentTick() { }
        public override void WorldComponentTick() { }
        public override void GameComponentTick() { }
        public void Helper() { }
    }

    private sealed class ExplicitTypeFixture
    {
        public void First() { }
        internal static void Second() { }
        public void Generic<T>() { }
    }

    private static class PatchFixtures
    {
        public static void Target() { }
        public static void Prefix() { }
        public static void Postfix() { }
        public static void Finalizer() { }
        public static void Transpiler() { }
    }

    private sealed class UnsupportedFixtures
    {
        public void Overloaded() { }
        public void Overloaded(int value) { _ = value; }
        public void Generic<T>() { }

        [CompilerGenerated]
        public void Generated() { }
    }
}
