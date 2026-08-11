using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using RimWorldDevGateway.Performance;

namespace RimWorldDevGateway.CircinusShape.Tests;

[TestFixture]
[NonParallelizable]
public sealed class CircinusInstalledShapeTests
{
    [Test]
    public void Installed_workshop_assembly_matches_the_pinned_identity_and_exact_runtime_shape()
    {
        var path = typeof(CircinusInstalledShapeTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "CircinusAssemblyPath")
            .Value;
        var assembly = Assembly.LoadFrom(path);

        Assert.That(CircinusRuntimeAdapter.TryBind(
            packageActive: true,
            new[] { assembly },
            out var binding,
            out var reason), Is.True, reason);

        Assert.Multiple(() =>
        {
            Assert.That(binding!.Identity.AssemblyIdentity,
                Is.EqualTo("Circinus, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"));
            Assert.That(binding.Identity.ModuleVersionId,
                Is.EqualTo(Guid.Parse("397fdc63-4b94-4548-8990-c319d9c32484")));
            Assert.That(binding.Identity.Length, Is.EqualTo(381952));
            Assert.That(binding.Identity.Sha256,
                Is.EqualTo("C597B6FC56E77E817E38AE63826F71FD7AC8830CFDE1AD32C59C15FACCDBAFA1"));
            Assert.That(binding.SchemaMajor, Is.EqualTo(1));
            Assert.That(binding.SchemaMinor, Is.EqualTo(15));
            Assert.That(Sha256(path), Is.EqualTo(binding.Identity.Sha256));
        });
    }

    [Test]
    public void Installed_Harmony_assembly_matches_the_exact_runtime_patch_discovery_shape()
    {
        var path = typeof(CircinusInstalledShapeTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "HarmonyAssemblyPath")
            .Value;
        var assembly = Assembly.LoadFrom(path);

        Assert.That(ReflectionPerformanceHarmonyCatalog.TryBind(
            new[] { assembly },
            out var catalog,
            out var reason), Is.True, reason);
        Assert.That(catalog, Is.Not.Null);
    }

    private static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        using var algorithm = SHA256.Create();
        return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty);
    }
}
