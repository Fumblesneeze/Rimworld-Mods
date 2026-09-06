using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using GuestBedGizmo.Compatibility.Hospitality;
using Mono.Cecil;
using NUnit.Framework;

namespace GuestBedGizmo.Harmony.Tests;

[TestFixture]
public sealed class HospitalityAssemblyShapeTests
{
    [Test]
    public void CompatibleHospitalityRebuildDoesNotNeedAnAllowlistedBinaryIdentity()
    {
        using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(Metadata("HospitalityAssemblyPath"));
        assembly.MainModule.Mvid = Guid.NewGuid();
        assembly.Name.Version = new Version(9, 8, 7, 6);
        using var rebuilt = new MemoryStream();
        assembly.Write(rebuilt);
        Assembly loadedAssembly = Assembly.Load(rebuilt.ToArray());

        bool resolved = HospitalityRuntimeAdapter.TryResolve(
            new[] { loadedAssembly },
            out HospitalityRuntimeAdapter? adapter,
            out string? failure);

        Assert.Multiple(() =>
        {
            Assert.That(resolved, Is.True, failure);
            Assert.That(adapter, Is.Not.Null);
        });
    }

    [Test]
    public void InspectedHospitality16ShapeMatchesTheGuardedRuntimeContract()
    {
        string path = Metadata("HospitalityAssemblyPath");
        using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(path);

        TypeDefinition guestBed = RequiredType(assembly, HospitalityRuntimeContract.GuestBedTypeName);
        MethodDefinition swap = guestBed.Methods.Single(method =>
            method.Name == HospitalityRuntimeContract.SwapMethodName);
        TypeDefinition patch = RequiredType(assembly, HospitalityRuntimeContract.LegacyPatchTypeName);
        MethodDefinition postfix = patch.Methods.Single(method => method.Name == "Postfix");
        TypeDefinition action = RequiredType(assembly, HospitalityRuntimeContract.LegacyActionTypeName);
        MethodDefinition callback = action.Methods.Single(method =>
            method.Name == HospitalityRuntimeContract.LegacyActionMethodName);
        Assembly loadedAssembly = Assembly.LoadFile(path);
        bool resolved = HospitalityRuntimeAdapter.TryResolve(
            new[] { loadedAssembly },
            out HospitalityRuntimeAdapter? adapter,
            out string? failure);

        Assert.Multiple(() =>
        {
            Assert.That(assembly.Name.Name, Is.EqualTo(HospitalityRuntimeContract.AssemblySimpleName));
            Assert.That(guestBed.IsPublic, Is.True);
            Assert.That(guestBed.BaseType.FullName, Is.EqualTo("RimWorld.Building_Bed"));
            Assert.That(swap.IsPublic && swap.IsStatic, Is.True);
            Assert.That(swap.ReturnType.FullName, Is.EqualTo("System.Void"));
            Assert.That(swap.Parameters.Select(parameter => parameter.ParameterType.FullName),
                Is.EqualTo(new[] { "RimWorld.Building_Bed" }));
            Assert.That(postfix.IsPublic && postfix.IsStatic, Is.True);
            Assert.That(postfix.Parameters.Select(parameter => parameter.ParameterType.FullName),
                Is.EqualTo(new[]
                {
                    "RimWorld.Building_Bed",
                    "System.Collections.Generic.IEnumerable`1<Verse.Gizmo>&",
                }));
            Assert.That(callback.IsStatic, Is.False);
            Assert.That(callback.Parameters, Is.Empty);
            Assert.That(callback.ReturnType.FullName, Is.EqualTo("System.Void"));
            Assert.That(action.Fields.Single(field => field.Name == "__instance").FieldType.FullName,
                Is.EqualTo("RimWorld.Building_Bed"));
            Assert.That(resolved, Is.True, failure);
            Assert.That(adapter, Is.Not.Null);
            Assert.That(adapter!.GuestBedType.FullName, Is.EqualTo(HospitalityRuntimeContract.GuestBedTypeName));
            Assert.That(HospitalityRuntimeContract.GuestLabelKey, Is.EqualTo("CommandBedSetAsGuestLabel"));
            Assert.That(HospitalityRuntimeContract.GuestDescriptionKey, Is.EqualTo("CommandBedSetAsGuestDesc"));
            Assert.That(HospitalityRuntimeContract.GuestIconPath, Is.EqualTo("UI/Commands/AsGuest"));
        });
        TestContext.WriteLine($"Inspected Hospitality: MVID={assembly.MainModule.Mvid}; SHA256={Sha256(path)}");
    }

    [TestCase(HospitalityRuntimeContract.GuestBedTypeName, HospitalityRuntimeContract.SwapMethodName)]
    [TestCase(HospitalityRuntimeContract.LegacyPatchTypeName, "Postfix")]
    [TestCase(HospitalityRuntimeContract.LegacyActionTypeName, HospitalityRuntimeContract.LegacyActionMethodName)]
    public void MissingConsumedHospitalityInterfaceLeavesTheAdapterUnavailable(string typeName, string methodName)
    {
        using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(Metadata("HospitalityAssemblyPath"));
        assembly.MainModule.Mvid = Guid.NewGuid();
        TypeDefinition type = RequiredType(assembly, typeName);
        type.Methods.Single(method => method.Name == methodName).Name = "UpstreamReplacement_" + methodName;
        using var changed = new MemoryStream();
        assembly.Write(changed);

        bool resolved = HospitalityRuntimeAdapter.TryResolve(
            new[] { Assembly.Load(changed.ToArray()) },
            out HospitalityRuntimeAdapter? adapter,
            out string? failure);

        Assert.Multiple(() =>
        {
            Assert.That(resolved, Is.False);
            Assert.That(adapter, Is.Null);
            Assert.That(failure, Does.Contain(methodName));
        });
    }

    [Test]
    public void VanillaDeferredOwnerAssignmentCommitShapeMatchesThePatchSeam()
    {
        using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(Metadata("AssemblyCSharpPath"));
        TypeDefinition bed = RequiredType(assembly, "RimWorld.Building_Bed");
        TypeDefinition closure = bed.NestedTypes.Single(type =>
            type.Name == "<>c__DisplayClass55_0");
        MethodDefinition commit = closure.Methods.Single(method =>
            method.Name == "<SetBedOwnerTypeByInterface>b__0");

        Assert.Multiple(() =>
        {
            Assert.That(commit.IsStatic, Is.False);
            Assert.That(commit.Parameters, Is.Empty);
            Assert.That(commit.ReturnType.FullName, Is.EqualTo("System.Void"));
            Assert.That(closure.Fields.Single(field => field.Name == "bedsToAffect").FieldType.FullName,
                Is.EqualTo("System.Collections.Generic.List`1<RimWorld.Building_Bed>"));
            Assert.That(closure.Fields.Single(field => field.Name == "ownerType").FieldType.FullName,
                Is.EqualTo("RimWorld.BedOwnerType"));
        });
    }

    private static TypeDefinition RequiredType(AssemblyDefinition assembly, string runtimeName)
    {
        TypeDefinition? type = assembly.MainModule.GetType(runtimeName.Replace('+', '/'));
        Assert.That(type, Is.Not.Null, $"Missing {runtimeName}");
        return type!;
    }

    private static string Sha256(string path)
    {
        using SHA256 sha = SHA256.Create();
        using FileStream stream = File.OpenRead(path);
        return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
    }

    private static string Metadata(string key) =>
        typeof(HospitalityAssemblyShapeTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == key)
            .Value;
}
