using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RimWorldDevGateway.Performance;

namespace RimWorldDevGateway.CircinusShape.Tests;

[TestFixture]
[NonParallelizable]
public sealed class DpaInstalledShapeTests
{
    [Test]
    public void Installed_workshop_DPA_matches_the_pinned_identity_and_exact_diagnostic_shape()
    {
        var path = typeof(DpaInstalledShapeTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "DpaAssemblyPath")
            .Value;
        var assembly = Assembly.LoadFrom(path);
        var harmonyPath = typeof(DpaInstalledShapeTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "HarmonyAssemblyPath")
            .Value;
        var harmony = Assembly.LoadFrom(harmonyPath);

        var loaded = AppDomain.CurrentDomain.GetAssemblies()
            .Where(candidate =>
                !string.Equals(candidate.GetName().Name, "PerformanceAnalyzer", StringComparison.Ordinal) &&
                !string.Equals(candidate.GetName().Name, "0Harmony", StringComparison.Ordinal))
            .Concat(new[] { assembly, harmony })
            .ToArray();
        Assert.That(DpaRuntimeAdapter.TryBind(true, loaded, out var binding, out var reason),
            Is.True, reason);
        Assert.Multiple(() =>
        {
            Assert.That(binding!.Identity.Identity,
                Is.EqualTo("PerformanceAnalyzer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"));
            Assert.That(binding.Identity.ModuleVersionId,
                Is.EqualTo(Guid.Parse("894501e4-b113-48af-9928-8da64293a38c")));
            Assert.That(binding.Identity.Length, Is.EqualTo(238080));
            Assert.That(binding.Identity.Sha256,
                Is.EqualTo("A1758774137F5EFF19F6B98D8D29F46AE5F8247C3EADBF9F611D851568D471A8"));
            Assert.That(binding.Identity.ProductVersion,
                Is.EqualTo("1.0.0+b6d6595b7457219357c25e8cef1082652454b7f7"));
            Assert.That(File.Exists(path), Is.True);
        });
    }
}
