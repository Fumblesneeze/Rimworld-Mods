using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;
using RimWorldDevGateway.EndToEndTesting;
using RimWorldDevGateway.PerformanceTesting;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class PerformanceTestingContractTests
{
    [Test]
    public void Describe_preserves_the_complete_reproducible_benchmark_contract()
    {
        var descriptor = PerformanceTestContract.Describe(typeof(ValidProductBenchmark));

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.Id, Is.EqualTo("immersive-chefs.base-colony"));
            Assert.That(descriptor.StagingOwnerPackageId, Is.EqualTo("fumblesneeze.immersivechefs"));
            Assert.That(descriptor.MeasuredSubjectPackageId, Is.EqualTo("fumblesneeze.immersivechefs"));
            Assert.That(descriptor.ActivePackageIds, Is.EqualTo(new[]
            {
                "brrainz.harmony",
                "ludeon.rimworld",
                PerformanceTestContract.CircinusPackageId,
                "fumblesneeze.immersivechefs"
            }));
            Assert.That(descriptor.LaunchedPackageIds.Last(), Is.EqualTo(EndToEndTestContract.GatewayPackageId));
            Assert.That(descriptor.WorkloadVersion, Is.EqualTo("immersive-chefs-colony/v1"));
            Assert.That(descriptor.ComparisonId, Is.EqualTo("immersive-chefs.base-colony"));
            Assert.That(descriptor.WarmUpTicks, Is.EqualTo(2_500));
            Assert.That(descriptor.SampleTicks, Is.EqualTo(12_000));
            Assert.That(descriptor.GameSpeed, Is.EqualTo(PerformanceGameSpeed.Superfast));
            Assert.That(descriptor.Repetitions, Is.EqualTo(3));
            Assert.That(descriptor.EvidenceLens, Is.EqualTo(PerformanceEvidenceLens.ProductInstrumented));
            Assert.That(descriptor.ProductAbsentControlId, Is.Null);
            Assert.That(descriptor.MethodSelectors.Select(selector => selector.Value), Is.EqualTo(new[]
            {
                "fumblesneeze.immersivechefs",
                "ImmersiveChefs.WorkGiver_DoDishes::JobOnThing"
            }));
            Assert.That(descriptor.ThroughputCheckpoints.Select(checkpoint => checkpoint.Id), Is.EqualTo(new[]
            {
                "dishwasher-cycles",
                "meals-cooked"
            }));
        });
    }

    [Test]
    public void Dpa_diagnostic_clone_replaces_only_the_profiler_and_one_exact_outer_selector()
    {
        var canonical = PerformanceTestContract.Describe(typeof(ValidProductBenchmark));
        var diagnostic = PerformanceTestContract.ForDpaDiagnostic(
            canonical,
            "Verse.Map::MapPreTick()");

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Profiler, Is.EqualTo(PerformanceProfilerKind.DpaDiagnostic));
            Assert.That(diagnostic.ActivePackageIds, Is.EqualTo(new[]
            {
                "brrainz.harmony", "ludeon.rimworld", PerformanceTestContract.DpaPackageId,
                "fumblesneeze.immersivechefs"
            }));
            Assert.That(diagnostic.ActivePackageIds, Does.Not.Contain(PerformanceTestContract.CircinusPackageId));
            Assert.That(diagnostic.MethodSelectors, Has.Count.EqualTo(1));
            Assert.That(diagnostic.MethodSelectors[0].Kind, Is.EqualTo(PerformanceMethodSelectorKind.Method));
            Assert.That(diagnostic.MethodSelectors[0].Value, Is.EqualTo("Verse.Map::MapPreTick()"));
            Assert.That(diagnostic.Repetitions, Is.EqualTo(1));
            Assert.That(diagnostic.WorkloadVersion, Is.EqualTo(canonical.WorkloadVersion));
            Assert.That(diagnostic.ThroughputCheckpoints, Is.SameAs(canonical.ThroughputCheckpoints));
        });
    }

    [Test]
    public void Grouping_is_ordinal_deterministic_and_never_inherits_downloaded_mods()
    {
        var groups = PerformanceTestContract.DescribeAndGroup(new[]
        {
            typeof(ValidOptionalBenchmark),
            typeof(ValidOptionalArmedDisabledBenchmark),
            typeof(ValidOptionalDisarmedBenchmark),
            typeof(ValidProductBenchmark),
            typeof(ValidArmedDisabledBenchmark),
            typeof(ValidSecondBaseBenchmark),
            typeof(ValidNeutralProductPresentBenchmark),
            typeof(ValidNeutralArmedDisabledBenchmark),
            typeof(ValidNeutralDisarmedBenchmark),
            typeof(ValidProductAbsentControl)
        });

        Assert.Multiple(() =>
        {
            Assert.That(groups, Has.Count.EqualTo(3));
            var baseGroup = groups.Single(group => group.Benchmarks.Any(test =>
                test.Id == "immersive-chefs.base-colony"));
            Assert.That(baseGroup.Benchmarks.Select(test => test.Id), Is.EqualTo(new[]
            {
                "gateway.immersive-chefs-neutral-present",
                "gateway.immersive-chefs-neutral-present-armed-disabled",
                "gateway.immersive-chefs-neutral-present-disarmed",
                "immersive-chefs.base-colony",
                "immersive-chefs.base-colony-armed-disabled",
                "immersive-chefs.base-colony-disarmed"
            }));
            Assert.That(baseGroup.ActivePackageIds, Does.Not.Contain("downloaded.but.inactive"));
            Assert.That(groups.Single(group => group.Benchmarks.Any(test =>
                test.Id == "immersive-chefs.dubs-colony")).Benchmarks.Select(test => test.Id), Is.EqualTo(new[]
            {
                "immersive-chefs.dubs-colony",
                "immersive-chefs.dubs-colony-armed-disabled",
                "immersive-chefs.dubs-colony-disarmed"
            }));
            Assert.That(groups.Select(group => group.GroupId), Is.Ordered);
        });
    }

    [Test]
    public void Product_absent_control_is_owned_elsewhere_and_keeps_the_subject_absent()
    {
        var descriptor = PerformanceTestContract.Describe(typeof(ValidProductAbsentControl));

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.StagingOwnerPackageId, Is.EqualTo(EndToEndTestContract.GatewayPackageId));
            Assert.That(descriptor.MeasuredSubjectPackageId, Is.EqualTo("fumblesneeze.immersivechefs"));
            Assert.That(descriptor.ActivePackageIds, Does.Not.Contain("fumblesneeze.immersivechefs"));
            Assert.That(descriptor.EvidenceLens, Is.EqualTo(PerformanceEvidenceLens.ProductAbsentControl));
        });
    }

    [Test]
    public void Implicit_gateway_can_be_the_measured_subject_without_listing_it_twice()
    {
        var descriptor = PerformanceTestContract.Describe(typeof(ValidGatewayBenchmark));

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.StagingOwnerPackageId, Is.EqualTo(EndToEndTestContract.GatewayPackageId));
            Assert.That(descriptor.MeasuredSubjectPackageId, Is.EqualTo(EndToEndTestContract.GatewayPackageId));
            Assert.That(descriptor.ActivePackageIds, Does.Not.Contain(EndToEndTestContract.GatewayPackageId));
            Assert.That(descriptor.LaunchedPackageIds.Last(), Is.EqualTo(EndToEndTestContract.GatewayPackageId));
        });
    }

    [Test]
    public void Grouping_rejects_missing_or_semantically_different_product_absent_controls()
    {
        var missing = Assert.Throws<PerformanceContractException>(() =>
            PerformanceTestContract.DescribeAndGroup(new[]
            {
                typeof(ValidProductBenchmark),
                typeof(ValidArmedDisabledBenchmark),
                typeof(ValidSecondBaseBenchmark),
                typeof(ValidNeutralProductPresentBenchmark),
                typeof(ValidNeutralArmedDisabledBenchmark),
                typeof(ValidNeutralDisarmedBenchmark)
            }));
        var drift = Assert.Throws<PerformanceContractException>(() =>
            PerformanceTestContract.DescribeAndGroup(new[]
            {
                typeof(ValidProductBenchmark),
                typeof(ValidArmedDisabledBenchmark),
                typeof(ValidSecondBaseBenchmark),
                typeof(ValidNeutralProductPresentBenchmark),
                typeof(ValidNeutralArmedDisabledBenchmark),
                typeof(ValidNeutralDisarmedBenchmark),
                typeof(DriftedProductAbsentControl)
            }));

        Assert.Multiple(() =>
        {
            Assert.That(missing!.Message, Does.Contain("gateway.immersive-chefs-neutral-control").And.Contain("missing"));
            Assert.That(drift!.Message, Does.Contain("throughput").Or.Contain("workload"));
        });
    }

    [Test]
    public void Grouping_accepts_an_ordinary_instrumented_run_but_requires_complete_optional_calibration()
    {
        Assert.That(() => PerformanceTestContract.DescribeAndGroup(new[]
        {
            typeof(ValidOptionalBenchmark)
        }), Throws.Nothing);

        var partial = Assert.Throws<PerformanceContractException>(() =>
            PerformanceTestContract.DescribeAndGroup(new[]
            {
                typeof(ValidOptionalBenchmark),
                typeof(ValidOptionalDisarmedBenchmark)
            }));
        var drift = Assert.Throws<PerformanceContractException>(() =>
            PerformanceTestContract.DescribeAndGroup(new[]
            {
                typeof(ValidOptionalBenchmark),
                typeof(DriftedOptionalArmedDisabledBenchmark),
                typeof(ValidOptionalDisarmedBenchmark)
            }));

        Assert.Multiple(() =>
        {
            Assert.That(partial!.Message, Does.Contain("ArmedDisabledWrapper"));
            Assert.That(drift!.Message, Does.Contain("incompatible").And.Contain("lens"));
        });
    }

    [TestCase(typeof(MissingAttributeBenchmark), "attribute")]
    [TestCase(typeof(AbstractBenchmark), "concrete")]
    [TestCase(typeof(AttributedNonBenchmark), "IRimWorldPerformanceTest")]
    [TestCase(typeof(EmptyOwnerBenchmark), "owner")]
    [TestCase(typeof(MissingCircinusBenchmark), "astryl.circinus")]
    [TestCase(typeof(DpaInCanonicalBenchmark), "DubsPerformanceAnalyzer")]
    [TestCase(typeof(InvalidPackageOrderBenchmark), "brrainz.harmony")]
    [TestCase(typeof(DuplicatePackageBenchmark), "duplicate")]
    [TestCase(typeof(MissingSubjectBenchmark), "measured subject")]
    [TestCase(typeof(InvalidTimingBenchmark), "ticks")]
    [TestCase(typeof(InvalidRepetitionBenchmark), "repetitions")]
    [TestCase(typeof(InvalidEvidenceLensBenchmark), "evidence lens")]
    [TestCase(typeof(DuplicateSelectorBenchmark), "duplicate method selector")]
    [TestCase(typeof(InvalidSelectorBenchmark), "method selector")]
    [TestCase(typeof(DuplicateCheckpointBenchmark), "duplicate throughput checkpoint")]
    [TestCase(typeof(InvalidCheckpointBenchmark), "throughput checkpoint")]
    [TestCase(typeof(MissingThroughputCounterBenchmark), "IPerformanceThroughputCounter")]
    [TestCase(typeof(InvalidAbsentControlBenchmark), "absent")]
    [TestCase(typeof(MissingHarmonyBenchmark), "brrainz.harmony")]
    [TestCase(typeof(CircinusAfterProductBenchmark), "astryl.circinus")]
    [TestCase(typeof(InvalidPackageTokenBenchmark), "package IDs")]
    public void Describe_rejects_invalid_or_ambiguous_declarations(Type type, string expected)
    {
        var error = Assert.Throws<PerformanceContractException>(() => PerformanceTestContract.Describe(type));

        Assert.That(error!.Message, Does.Contain(expected).IgnoreCase);
    }

    [Test]
    public void Published_package_identity_and_cardinality_boundaries_are_exact()
    {
        var exactPackages = CanonicalPackages(PerformanceTestContract.MaximumActivePackages);
        var overPackages = CanonicalPackages(PerformanceTestContract.MaximumActivePackages + 1);
        var exactPackageLength = "p" + new string('a', PerformanceTestContract.MaximumPackageIdCharacters - 1);
        var overPackageLength = "p" + new string('a', PerformanceTestContract.MaximumPackageIdCharacters);

        Assert.Multiple(() =>
        {
            Assert.That(() => PerformanceTestContract.Describe(
                BuildDynamicBenchmark("packages-exact", "dynamic.packages-exact", exactPackages)), Throws.Nothing);
            Assert.That(() => PerformanceTestContract.Describe(
                BuildDynamicBenchmark("packages-over", "dynamic.packages-over", overPackages)),
                Throws.TypeOf<PerformanceContractException>().With.Message.Contains("64"));
            Assert.That(() => PerformanceTestContract.Describe(
                BuildDynamicBenchmark(
                    "package-length-exact",
                    "dynamic.package-length-exact",
                    CanonicalPackages(5, exactPackageLength))), Throws.Nothing);
            Assert.That(() => PerformanceTestContract.Describe(
                BuildDynamicBenchmark(
                    "package-length-over",
                    "dynamic.package-length-over",
                    CanonicalPackages(5, overPackageLength))),
                Throws.TypeOf<PerformanceContractException>().With.Message.Contains("128"));
        });
    }

    [Test]
    public void Published_identity_selector_and_checkpoint_boundaries_are_exact()
    {
        var exactId = "i" + new string('a', PerformanceTestContract.MaximumIdentityCharacters - 1);
        var overId = "i" + new string('a', PerformanceTestContract.MaximumIdentityCharacters);
        var exactSelector = "S" + new string('x', PerformanceTestContract.MaximumSelectorCharacters - 1);
        var overSelector = "S" + new string('x', PerformanceTestContract.MaximumSelectorCharacters);
        var exactCheckpoint = "c" + new string('x', PerformanceTestContract.MaximumIdentityCharacters - 1);
        var overCheckpoint = "c" + new string('x', PerformanceTestContract.MaximumIdentityCharacters);

        Assert.Multiple(() =>
        {
            Assert.That(() => PerformanceTestContract.Describe(
                BuildDynamicBenchmark("identity-exact", exactId, CanonicalPackages(4))), Throws.Nothing);
            Assert.That(() => PerformanceTestContract.Describe(
                BuildDynamicBenchmark("identity-over", overId, CanonicalPackages(4))),
                Throws.TypeOf<PerformanceContractException>().With.Message.Contains("256"));
            Assert.That(() => PerformanceTestContract.Describe(
                BuildDynamicBenchmark("selector-count-exact", "dynamic.selector-count-exact", CanonicalPackages(4),
                    selectorCount: PerformanceTestContract.MaximumMethodSelectors)), Throws.Nothing);
            Assert.That(() => PerformanceTestContract.Describe(
                BuildDynamicBenchmark("selector-count-over", "dynamic.selector-count-over", CanonicalPackages(4),
                    selectorCount: PerformanceTestContract.MaximumMethodSelectors + 1)),
                Throws.TypeOf<PerformanceContractException>().With.Message.Contains("256"));
            Assert.That(() => PerformanceTestContract.Describe(
                BuildDynamicBenchmark("selector-length-exact", "dynamic.selector-length-exact", CanonicalPackages(4),
                    selectorValue: exactSelector)), Throws.Nothing);
            Assert.That(() => PerformanceTestContract.Describe(
                BuildDynamicBenchmark("selector-length-over", "dynamic.selector-length-over", CanonicalPackages(4),
                    selectorValue: overSelector)),
                Throws.TypeOf<PerformanceContractException>().With.Message.Contains("1024"));
            Assert.That(() => PerformanceTestContract.Describe(
                BuildDynamicBenchmark("checkpoint-count-exact", "dynamic.checkpoint-count-exact", CanonicalPackages(4),
                    checkpointCount: PerformanceTestContract.MaximumThroughputCheckpoints)), Throws.Nothing);
            Assert.That(() => PerformanceTestContract.Describe(
                BuildDynamicBenchmark("checkpoint-count-over", "dynamic.checkpoint-count-over", CanonicalPackages(4),
                    checkpointCount: PerformanceTestContract.MaximumThroughputCheckpoints + 1)),
                Throws.TypeOf<PerformanceContractException>().With.Message.Contains("64"));
            Assert.That(() => PerformanceTestContract.Describe(
                BuildDynamicBenchmark("checkpoint-length-exact", "dynamic.checkpoint-length-exact", CanonicalPackages(4),
                    checkpointId: exactCheckpoint)), Throws.Nothing);
            Assert.That(() => PerformanceTestContract.Describe(
                BuildDynamicBenchmark("checkpoint-length-over", "dynamic.checkpoint-length-over", CanonicalPackages(4),
                    checkpointId: overCheckpoint)),
                Throws.TypeOf<PerformanceContractException>().With.Message.Contains("256"));
        });
    }

    [Test]
    public void Published_benchmark_discovery_count_stops_before_semantic_grouping()
    {
        var exact = Enumerable.Repeat(typeof(ValidProductBenchmark), PerformanceTestContract.MaximumBenchmarks);
        var over = Enumerable.Repeat(typeof(ValidProductBenchmark), PerformanceTestContract.MaximumBenchmarks + 1);

        var exactError = Assert.Throws<PerformanceContractException>(() =>
            PerformanceTestContract.DescribeAndGroup(exact));
        var overError = Assert.Throws<PerformanceContractException>(() =>
            PerformanceTestContract.DescribeAndGroup(over));

        Assert.Multiple(() =>
        {
            Assert.That(exactError!.Message, Does.Contain("Duplicate").And.Not.Contain("ceiling"));
            Assert.That(overError!.Message, Does.Contain("1024").And.Contain("ceiling"));
        });
    }

    [RimWorldPerformanceTest(
        "immersive-chefs.base-colony",
        "fumblesneeze.immersivechefs",
        "fumblesneeze.immersivechefs",
        "brrainz.harmony",
        "ludeon.rimworld",
        PerformanceTestContract.CircinusPackageId,
        "fumblesneeze.immersivechefs",
        WorkloadVersion = "immersive-chefs-colony/v1",
        WarmUpTicks = 2_500,
        SampleTicks = 12_000,
        GameSpeed = PerformanceGameSpeed.Superfast,
        Repetitions = 3,
        EvidenceLens = PerformanceEvidenceLens.ProductInstrumented,
        ComparisonId = "immersive-chefs.base-colony",
        ProductAbsentControlId = null)]
    [PerformanceMethodSelector(PerformanceMethodSelectorKind.HarmonyOwner, "fumblesneeze.immersivechefs", "product-harmony")]
    [PerformanceMethodSelector(PerformanceMethodSelectorKind.Method, "ImmersiveChefs.WorkGiver_DoDishes::JobOnThing", "dish-search")]
    [PerformanceThroughputCheckpoint("dishwasher-cycles", 2)]
    [PerformanceThroughputCheckpoint("meals-cooked", 12)]
    public sealed class ValidProductBenchmark : NoOpBenchmark
    {
    }

    [RimWorldPerformanceTest(
        "immersive-chefs.base-colony-armed-disabled",
        "fumblesneeze.immersivechefs",
        "fumblesneeze.immersivechefs",
        "brrainz.harmony",
        "ludeon.rimworld",
        PerformanceTestContract.CircinusPackageId,
        "fumblesneeze.immersivechefs",
        WorkloadVersion = "immersive-chefs-colony/v1",
        ComparisonId = "immersive-chefs.base-colony",
        WarmUpTicks = 2_500,
        SampleTicks = 12_000,
        GameSpeed = PerformanceGameSpeed.Superfast,
        Repetitions = 3,
        EvidenceLens = PerformanceEvidenceLens.ArmedDisabledWrapper,
        ProductAbsentControlId = null)]
    [PerformanceMethodSelector(PerformanceMethodSelectorKind.HarmonyOwner, "fumblesneeze.immersivechefs", "product-harmony")]
    [PerformanceMethodSelector(PerformanceMethodSelectorKind.Method, "ImmersiveChefs.WorkGiver_DoDishes::JobOnThing", "dish-search")]
    [PerformanceThroughputCheckpoint("dishwasher-cycles", 2)]
    [PerformanceThroughputCheckpoint("meals-cooked", 12)]
    public sealed class ValidArmedDisabledBenchmark : NoOpBenchmark
    {
    }

    [RimWorldPerformanceTest(
        "immersive-chefs.base-colony-disarmed",
        "fumblesneeze.immersivechefs",
        "fumblesneeze.immersivechefs",
        "brrainz.harmony",
        "ludeon.rimworld",
        PerformanceTestContract.CircinusPackageId,
        "fumblesneeze.immersivechefs",
        WorkloadVersion = "immersive-chefs-colony/v1",
        ComparisonId = "immersive-chefs.base-colony",
        WarmUpTicks = 2500,
        SampleTicks = 12000,
        Repetitions = 3,
        EvidenceLens = PerformanceEvidenceLens.FullyDisarmed,
        ProductAbsentControlId = null)]
    [PerformanceMethodSelector(PerformanceMethodSelectorKind.HarmonyOwner, "fumblesneeze.immersivechefs", "product-harmony")]
    [PerformanceMethodSelector(PerformanceMethodSelectorKind.Method, "ImmersiveChefs.WorkGiver_DoDishes::JobOnThing", "dish-search")]
    [PerformanceThroughputCheckpoint("dishwasher-cycles", 2)]
    [PerformanceThroughputCheckpoint("meals-cooked", 12)]
    public sealed class ValidSecondBaseBenchmark : NoOpBenchmark
    {
    }

    [RimWorldPerformanceTest(
        "gateway.immersive-chefs-neutral-present",
        EndToEndTestContract.GatewayPackageId,
        "fumblesneeze.immersivechefs",
        "brrainz.harmony",
        "ludeon.rimworld",
        PerformanceTestContract.CircinusPackageId,
        "fumblesneeze.immersivechefs",
        WorkloadVersion = "immersive-chefs-neutral/v1",
        ComparisonId = "gateway.immersive-chefs-neutral-present",
        WarmUpTicks = 2500,
        SampleTicks = 12000,
        Repetitions = 3,
        EvidenceLens = PerformanceEvidenceLens.ProductInstrumented,
        ProductAbsentControlId = "gateway.immersive-chefs-neutral-control")]
    [PerformanceThroughputCheckpoint("neutral-jobs", 12)]
    public sealed class ValidNeutralProductPresentBenchmark : NoOpBenchmark { }

    [RimWorldPerformanceTest(
        "gateway.immersive-chefs-neutral-present-armed-disabled",
        EndToEndTestContract.GatewayPackageId,
        "fumblesneeze.immersivechefs",
        "brrainz.harmony",
        "ludeon.rimworld",
        PerformanceTestContract.CircinusPackageId,
        "fumblesneeze.immersivechefs",
        WorkloadVersion = "immersive-chefs-neutral/v1",
        ComparisonId = "gateway.immersive-chefs-neutral-present",
        WarmUpTicks = 2500,
        SampleTicks = 12000,
        Repetitions = 3,
        EvidenceLens = PerformanceEvidenceLens.ArmedDisabledWrapper,
        ProductAbsentControlId = "gateway.immersive-chefs-neutral-control")]
    [PerformanceThroughputCheckpoint("neutral-jobs", 12)]
    public sealed class ValidNeutralArmedDisabledBenchmark : NoOpBenchmark { }

    [RimWorldPerformanceTest(
        "gateway.immersive-chefs-neutral-present-disarmed",
        EndToEndTestContract.GatewayPackageId,
        "fumblesneeze.immersivechefs",
        "brrainz.harmony",
        "ludeon.rimworld",
        PerformanceTestContract.CircinusPackageId,
        "fumblesneeze.immersivechefs",
        WorkloadVersion = "immersive-chefs-neutral/v1",
        ComparisonId = "gateway.immersive-chefs-neutral-present",
        WarmUpTicks = 2500,
        SampleTicks = 12000,
        Repetitions = 3,
        EvidenceLens = PerformanceEvidenceLens.FullyDisarmed,
        ProductAbsentControlId = "gateway.immersive-chefs-neutral-control")]
    [PerformanceThroughputCheckpoint("neutral-jobs", 12)]
    public sealed class ValidNeutralDisarmedBenchmark : NoOpBenchmark { }

    [RimWorldPerformanceTest(
        "immersive-chefs.dubs-colony",
        "fumblesneeze.immersivechefs",
        "fumblesneeze.immersivechefs",
        "brrainz.harmony",
        "ludeon.rimworld",
        PerformanceTestContract.CircinusPackageId,
        "dubwise.dubsbadhygiene",
        "fumblesneeze.immersivechefs",
        WorkloadVersion = "immersive-chefs-colony/v1-dubs",
        ComparisonId = "immersive-chefs.dubs-colony",
        WarmUpTicks = 2500,
        SampleTicks = 12000)]
    public sealed class ValidOptionalBenchmark : NoOpBenchmark
    {
    }

    [RimWorldPerformanceTest(
        "immersive-chefs.dubs-colony-armed-disabled",
        "fumblesneeze.immersivechefs",
        "fumblesneeze.immersivechefs",
        "brrainz.harmony",
        "ludeon.rimworld",
        PerformanceTestContract.CircinusPackageId,
        "dubwise.dubsbadhygiene",
        "fumblesneeze.immersivechefs",
        WorkloadVersion = "immersive-chefs-colony/v1-dubs",
        ComparisonId = "immersive-chefs.dubs-colony",
        WarmUpTicks = 2500,
        SampleTicks = 12000,
        EvidenceLens = PerformanceEvidenceLens.ArmedDisabledWrapper)]
    public sealed class ValidOptionalArmedDisabledBenchmark : NoOpBenchmark
    {
    }

    [RimWorldPerformanceTest(
        "immersive-chefs.dubs-colony-disarmed",
        "fumblesneeze.immersivechefs",
        "fumblesneeze.immersivechefs",
        "brrainz.harmony",
        "ludeon.rimworld",
        PerformanceTestContract.CircinusPackageId,
        "dubwise.dubsbadhygiene",
        "fumblesneeze.immersivechefs",
        WorkloadVersion = "immersive-chefs-colony/v1-dubs",
        ComparisonId = "immersive-chefs.dubs-colony",
        WarmUpTicks = 2500,
        SampleTicks = 12000,
        EvidenceLens = PerformanceEvidenceLens.FullyDisarmed)]
    public sealed class ValidOptionalDisarmedBenchmark : NoOpBenchmark
    {
    }

    [RimWorldPerformanceTest(
        "immersive-chefs.dubs-colony-armed-disabled-drift",
        "fumblesneeze.immersivechefs",
        "fumblesneeze.immersivechefs",
        "brrainz.harmony",
        "ludeon.rimworld",
        PerformanceTestContract.CircinusPackageId,
        "dubwise.dubsbadhygiene",
        "fumblesneeze.immersivechefs",
        WorkloadVersion = "immersive-chefs-colony/v1-dubs",
        ComparisonId = "immersive-chefs.dubs-colony",
        WarmUpTicks = 2500,
        SampleTicks = 12001,
        EvidenceLens = PerformanceEvidenceLens.ArmedDisabledWrapper)]
    public sealed class DriftedOptionalArmedDisabledBenchmark : NoOpBenchmark
    {
    }

    [RimWorldPerformanceTest(
        "gateway.immersive-chefs-neutral-control",
        EndToEndTestContract.GatewayPackageId,
        "fumblesneeze.immersivechefs",
        "brrainz.harmony",
        "ludeon.rimworld",
        PerformanceTestContract.CircinusPackageId,
        WorkloadVersion = "immersive-chefs-neutral/v1",
        WarmUpTicks = 2500,
        SampleTicks = 12000,
        EvidenceLens = PerformanceEvidenceLens.ProductAbsentControl,
        Repetitions = 3)]
    [PerformanceThroughputCheckpoint("neutral-jobs", 12)]
    public sealed class ValidProductAbsentControl : NoOpBenchmark
    {
    }

    [RimWorldPerformanceTest(
        "gateway.immersive-chefs-neutral-control",
        EndToEndTestContract.GatewayPackageId,
        "fumblesneeze.immersivechefs",
        "brrainz.harmony",
        "ludeon.rimworld",
        PerformanceTestContract.CircinusPackageId,
        WorkloadVersion = "drifted/v2",
        WarmUpTicks = 2500,
        SampleTicks = 12000,
        EvidenceLens = PerformanceEvidenceLens.ProductAbsentControl,
        Repetitions = 3)]
    [PerformanceThroughputCheckpoint("neutral-jobs", 1)]
    public sealed class DriftedProductAbsentControl : NoOpBenchmark
    {
    }

    [RimWorldPerformanceTest(
        "gateway.valid",
        EndToEndTestContract.GatewayPackageId,
        EndToEndTestContract.GatewayPackageId,
        "brrainz.harmony",
        "ludeon.rimworld",
        PerformanceTestContract.CircinusPackageId)]
    public sealed class ValidGatewayBenchmark : NoOpBenchmark { }

    public sealed class MissingAttributeBenchmark : NoOpBenchmark { }

    [RimWorldPerformanceTest("invalid.abstract", "owner", "owner", "ludeon.rimworld", PerformanceTestContract.CircinusPackageId)]
    public abstract class AbstractBenchmark : NoOpBenchmark { }

    [RimWorldPerformanceTest("invalid.interface", "owner", "owner", "ludeon.rimworld", PerformanceTestContract.CircinusPackageId, "owner")]
    public sealed class AttributedNonBenchmark { }

    [RimWorldPerformanceTest("invalid.owner", " ", "subject", "ludeon.rimworld", PerformanceTestContract.CircinusPackageId, "subject")]
    public sealed class EmptyOwnerBenchmark : NoOpBenchmark { }

    [RimWorldPerformanceTest("invalid.circinus", "owner", "owner", "brrainz.harmony", "ludeon.rimworld", "owner")]
    public sealed class MissingCircinusBenchmark : NoOpBenchmark { }

    [RimWorldPerformanceTest("invalid.dpa", "owner", "owner", "brrainz.harmony", "ludeon.rimworld", PerformanceTestContract.CircinusPackageId, PerformanceTestContract.DpaPackageId, "owner")]
    public sealed class DpaInCanonicalBenchmark : NoOpBenchmark { }

    [RimWorldPerformanceTest("invalid.order", "owner", "owner", "ludeon.rimworld", "brrainz.harmony", PerformanceTestContract.CircinusPackageId, "owner")]
    public sealed class InvalidPackageOrderBenchmark : NoOpBenchmark { }

    [RimWorldPerformanceTest("invalid.duplicate", "owner", "owner", "brrainz.harmony", "ludeon.rimworld", PerformanceTestContract.CircinusPackageId, "owner", "OWNER")]
    public sealed class DuplicatePackageBenchmark : NoOpBenchmark { }

    [RimWorldPerformanceTest("invalid.subject", "owner", "subject", "brrainz.harmony", "ludeon.rimworld", PerformanceTestContract.CircinusPackageId, "owner")]
    public sealed class MissingSubjectBenchmark : NoOpBenchmark { }

    [RimWorldPerformanceTest("invalid.timing", "owner", "owner", "brrainz.harmony", "ludeon.rimworld", PerformanceTestContract.CircinusPackageId, "owner", WarmUpTicks = -1, SampleTicks = 0)]
    public sealed class InvalidTimingBenchmark : NoOpBenchmark { }

    [RimWorldPerformanceTest("invalid.repetitions", "owner", "owner", "brrainz.harmony", "ludeon.rimworld", PerformanceTestContract.CircinusPackageId, "owner", Repetitions = 0)]
    public sealed class InvalidRepetitionBenchmark : NoOpBenchmark { }

    [RimWorldPerformanceTest("invalid.lens", "owner", "owner", "brrainz.harmony", "ludeon.rimworld", PerformanceTestContract.CircinusPackageId, "owner", EvidenceLens = (PerformanceEvidenceLens)99)]
    public sealed class InvalidEvidenceLensBenchmark : NoOpBenchmark { }

    [RimWorldPerformanceTest("invalid.selectors", "owner", "owner", "brrainz.harmony", "ludeon.rimworld", PerformanceTestContract.CircinusPackageId, "owner")]
    [PerformanceMethodSelector(PerformanceMethodSelectorKind.Method, "A.B::C", "one")]
    [PerformanceMethodSelector(PerformanceMethodSelectorKind.Method, "A.B::C", "two")]
    public sealed class DuplicateSelectorBenchmark : NoOpBenchmark { }

    [RimWorldPerformanceTest("invalid.selector", "owner", "owner", "brrainz.harmony", "ludeon.rimworld", PerformanceTestContract.CircinusPackageId, "owner")]
    [PerformanceMethodSelector((PerformanceMethodSelectorKind)99, " ", " ")]
    public sealed class InvalidSelectorBenchmark : NoOpBenchmark { }

    [RimWorldPerformanceTest("invalid.checkpoints", "owner", "owner", "brrainz.harmony", "ludeon.rimworld", PerformanceTestContract.CircinusPackageId, "owner")]
    [PerformanceThroughputCheckpoint("meals", 1)]
    [PerformanceThroughputCheckpoint("MEALS", 2)]
    public sealed class DuplicateCheckpointBenchmark : NoOpBenchmark { }

    [RimWorldPerformanceTest("invalid.checkpoint", "owner", "owner", "brrainz.harmony", "ludeon.rimworld", PerformanceTestContract.CircinusPackageId, "owner")]
    [PerformanceThroughputCheckpoint(" ", 0)]
    public sealed class InvalidCheckpointBenchmark : NoOpBenchmark { }

    [RimWorldPerformanceTest("invalid.missing-throughput-counter", "owner", "owner", "brrainz.harmony", "ludeon.rimworld", PerformanceTestContract.CircinusPackageId, "owner")]
    [PerformanceThroughputCheckpoint("native-work", 1)]
    public sealed class MissingThroughputCounterBenchmark : IRimWorldPerformanceTest
    {
        public void Arrange(IEndToEndContext context) { }
        public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
        {
            yield break;
        }
    }

    [RimWorldPerformanceTest("invalid.absent", "owner", "owner", "brrainz.harmony", "ludeon.rimworld", PerformanceTestContract.CircinusPackageId, "owner", EvidenceLens = PerformanceEvidenceLens.ProductAbsentControl)]
    public sealed class InvalidAbsentControlBenchmark : NoOpBenchmark { }

    [RimWorldPerformanceTest("invalid.harmony-missing", "owner", "owner", "ludeon.rimworld", PerformanceTestContract.CircinusPackageId, "owner")]
    public sealed class MissingHarmonyBenchmark : NoOpBenchmark { }

    [RimWorldPerformanceTest("invalid.circinus-order", "owner", "owner", "brrainz.harmony", "ludeon.rimworld", "owner", PerformanceTestContract.CircinusPackageId)]
    public sealed class CircinusAfterProductBenchmark : NoOpBenchmark { }

    [RimWorldPerformanceTest("invalid.package-token", "owner", "owner", "brrainz.harmony", "ludeon.rimworld", PerformanceTestContract.CircinusPackageId, "bad\npackage", "owner")]
    public sealed class InvalidPackageTokenBenchmark : NoOpBenchmark { }

    public abstract class NoOpBenchmark : IRimWorldPerformanceTest, IPerformanceThroughputCounter
    {
        public void Arrange(IEndToEndContext context) { }

        public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
        {
            yield break;
        }

        public long Read(string id) => 1;
    }

    public class DynamicNoOpBenchmark : NoOpBenchmark { }

    private static string[] CanonicalPackages(int count, string? lastOptional = null)
    {
        if (count < 4)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        var packages = new List<string>
        {
            "brrainz.harmony",
            "ludeon.rimworld",
            PerformanceTestContract.CircinusPackageId
        };
        var optionalCount = count - 4;
        for (var index = 0; index < optionalCount; index++)
        {
            packages.Add(lastOptional is not null && index == optionalCount - 1
                ? lastOptional
                : "optional.p" + index);
        }

        packages.Add("owner");

        return packages.ToArray();
    }

    private static Type BuildDynamicBenchmark(
        string suffix,
        string id,
        string[] packages,
        int selectorCount = 0,
        string? selectorValue = null,
        int checkpointCount = 0,
        string? checkpointId = null)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("PerformanceContract.Dynamic." + suffix + "." + Guid.NewGuid().ToString("N")),
            AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("main");
        var type = module.DefineType(
            "Dynamic." + suffix.Replace('-', '_'),
            TypeAttributes.Public | TypeAttributes.Class,
            typeof(DynamicNoOpBenchmark));
        type.DefineDefaultConstructor(MethodAttributes.Public);

        var benchmarkConstructor = typeof(RimWorldPerformanceTestAttribute).GetConstructor(new[]
        {
            typeof(string),
            typeof(string),
            typeof(string),
            typeof(string[])
        })!;
        type.SetCustomAttribute(new CustomAttributeBuilder(
            benchmarkConstructor,
            new object[] { id, "owner", "owner", packages }));

        var selectorConstructor = typeof(PerformanceMethodSelectorAttribute).GetConstructor(new[]
        {
            typeof(PerformanceMethodSelectorKind),
            typeof(string),
            typeof(string)
        })!;
        var selectors = selectorValue is null ? selectorCount : 1;
        for (var index = 0; index < selectors; index++)
        {
            type.SetCustomAttribute(new CustomAttributeBuilder(
                selectorConstructor,
                new object[]
                {
                    PerformanceMethodSelectorKind.Method,
                    selectorValue ?? "Dynamic.Type::Method" + index,
                    "dynamic"
                }));
        }

        var checkpointConstructor = typeof(PerformanceThroughputCheckpointAttribute).GetConstructor(new[]
        {
            typeof(string),
            typeof(long)
        })!;
        var checkpoints = checkpointId is null ? checkpointCount : 1;
        for (var index = 0; index < checkpoints; index++)
        {
            type.SetCustomAttribute(new CustomAttributeBuilder(
                checkpointConstructor,
                new object[] { checkpointId ?? "checkpoint-" + index, 1L }));
        }

        return type.CreateType()!;
    }
}
