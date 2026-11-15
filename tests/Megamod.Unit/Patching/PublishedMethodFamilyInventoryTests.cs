using System.Reflection;
using System.Reflection.Emit;
using System.Globalization;
using System.Runtime.CompilerServices;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Unit.Patching;

[TestFixture]
public sealed class PublishedMethodFamilyInventoryTests
{
    private static readonly MethodInfo Definition = typeof(PublishedMethodFamilyInventoryTests).GetMethod(nameof(Native))!;
    private static readonly MethodInfo Integer = Definition.MakeGenericMethod(typeof(int));
    private static readonly MethodInfo Other = typeof(PublishedMethodFamilyInventoryTests).GetMethod(nameof(Unrelated))!;

    [Test]
    public void DetachedInventoryRetainsOnlyTheSelectedFamilyAndItsPublicationIdentities()
    {
        // Detached input data only: these bytes and keys are never inserted into Harmony state.
        byte[] firstPayload = { 1 }, unrelatedPayload = { 2 };
        var input = new Dictionary<MethodBase, byte[]> { [Integer] = firstPayload, [Other] = unrelatedPayload };
        Assert.That(PublishedMethodFamilyInventory.TryCreate(Definition, input, out var first), Is.True);
        Assert.That(first!.Members, Is.EquivalentTo(new[] { Integer }));

        input[Other] = new byte[] { 3 };
        Assert.That(PublishedMethodFamilyInventory.TryCreate(Definition, input, out var second), Is.True);
        Assert.That(first.SamePublications(second!), Is.True, "Unrelated publication identity is not part of this family.");

        input[Integer] = new byte[] { 1 };
        Assert.That(PublishedMethodFamilyInventory.TryCreate(Definition, input, out var third), Is.True);
        Assert.That(first.SamePublications(third!), Is.False, "Equal bytes from a new publication must invalidate identity.");
        Assert.That(first.Members, Is.EquivalentTo(new[] { Integer }), "Projection must be detached from input membership changes.");
        input.Remove(Integer);
        Assert.That(PublishedMethodFamilyInventory.TryCreate(Definition, input, out var removed), Is.True);
        Assert.That(first.SamePublications(removed!), Is.False);
        Assert.That(first.Members, Is.EquivalentTo(new[] { Integer }));
    }

    [Test]
    public void GlobalEntryBoundCountsUnrelatedKeysWithoutCreatingAnyDetour()
    {
        var entries = new List<KeyValuePair<MethodBase, byte[]>>(65537) { new(Integer, new byte[] { 1 }) };
        byte[] token = { 2 };
        for (int index = 1; index < 65536; index++)
            entries.Add(new(new DynamicMethod("unrelated", typeof(void), Type.EmptyTypes), token));
        Assert.That(PublishedMethodFamilyInventory.TryCreate(Definition, entries, out var exact), Is.True);
        Assert.That(exact!.Members, Is.EquivalentTo(new[] { Integer }));
        entries.Add(new(new DynamicMethod("one-more", typeof(void), Type.EmptyTypes), token));
        Assert.That(PublishedMethodFamilyInventory.TryCreate(Definition, entries, out var over), Is.False);
        Assert.That(over, Is.Null);
    }

    [Test]
    public void FamilyMemberBoundCountsDistinctUnfamiliarStructClosures()
    {
        var entries = new Dictionary<MethodBase, byte[]>();
        for (int index = 0; index < 256; index++) entries.Add(Definition.MakeGenericMethod(StructType(index)), new byte[] { 1 });
        Assert.That(PublishedMethodFamilyInventory.TryCreate(Definition, entries, out var exact), Is.True);
        Assert.That(exact!.Members, Has.Count.EqualTo(256));
        entries.Add(Definition.MakeGenericMethod(StructType(256)), new byte[] { 1 });
        Assert.That(PublishedMethodFamilyInventory.TryCreate(Definition, entries, out var over), Is.False);
        Assert.That(over, Is.Null);
    }

    [TestCase(-1, false)]
    [TestCase(0, false)]
    [TestCase(4194304, true)]
    [TestCase(4194305, false)]
    public void ConsumedPayloadShapeIsBoundedWithoutInterpretingItsBytes(int length, bool expected)
    {
        var entries = new Dictionary<MethodBase, byte[]> { [Integer] = length < 0 ? null! : new byte[length] };
        Assert.That(PublishedMethodFamilyInventory.TryCreate(Definition, entries, out var result), Is.EqualTo(expected));
        Assert.That(result != null, Is.EqualTo(expected));
    }

    [Test]
    public void OpenDefinitionAndInheritedReflectionAliasRemainRealFamilyKeys()
    {
        var definition = typeof(Base).GetMethod(nameof(Base.Inherited))!;
        var inherited = typeof(Derived).GetMethod(nameof(Base.Inherited))!.MakeGenericMethod(typeof(decimal));
        Assert.That(inherited.ReflectedType, Is.EqualTo(typeof(Derived)));
        var entries = new Dictionary<MethodBase, byte[]> { [definition] = new byte[] { 1 }, [inherited] = new byte[] { 2 } };
        Assert.That(PublishedMethodFamilyInventory.TryCreate(definition, entries, out var inventory), Is.True);
        Assert.That(inventory!.Members, Is.EquivalentTo(new[] { definition, inherited }));
    }

    [Test]
    public void OrdinaryConstructorAndDynamicKeysAreProvablyUnrelated()
    {
        var entries = new Dictionary<MethodBase, byte[]>
        {
            [typeof(object).GetConstructor(Type.EmptyTypes)!] = Array.Empty<byte>(),
            [new DynamicMethod("not-generic", typeof(void), Type.EmptyTypes)] = Array.Empty<byte>(),
            [Other] = Array.Empty<byte>(),
            [Integer] = new byte[] { 1 }
        };
        Assert.That(PublishedMethodFamilyInventory.TryCreate(Definition, entries, out var inventory), Is.True);
        Assert.That(inventory!.Members, Is.EquivalentTo(new[] { Integer }));
    }

    [Test]
    public void OpaqueMethodWrapperRefusesWithoutCallingItsMetadata()
    {
        var opaque = new OpaqueMethod();
        var entries = new Dictionary<MethodBase, byte[]> { [opaque] = new byte[] { 1 } };
        OpaqueMethod.Reads = 0;
        Assert.That(PublishedMethodFamilyInventory.TryCreate(Definition, entries, out _), Is.False);
        Assert.That(PublishedMethodFamilyInventory.TryCreate(opaque, new Dictionary<MethodBase, byte[]>(), out _), Is.False);
        Assert.That(OpaqueMethod.Reads, Is.Zero);
    }

    [Test]
    public void DifferentDefinitionsCannotShareAnEmptyFamilyReceipt()
    {
        var empty = new Dictionary<MethodBase, byte[]>();
        Assert.That(PublishedMethodFamilyInventory.TryCreate(Definition, empty, out var first), Is.True);
        Assert.That(PublishedMethodFamilyInventory.TryCreate(typeof(Base).GetMethod(nameof(Base.Inherited))!, empty, out var second), Is.True);
        Assert.That(first!.SamePublications(second!), Is.False);
        Assert.That(PublishedMethodFamilyInventory.TryCreate(Integer, empty, out _), Is.False);
        Assert.That(PublishedMethodFamilyInventory.TryCreate(Other, empty, out _), Is.False);
        Assert.That(PublishedMethodFamilyInventory.TryCreate(typeof(GenericContainer<int>).GetMethod(nameof(GenericContainer<int>.Native))!, empty, out _), Is.False);
    }

    [Test]
    public void DuplicateConsumedKeyCannotMasqueradeAsTwoMembers()
    {
        var entries = new[] { new KeyValuePair<MethodBase, byte[]>(Integer, new byte[] { 1 }), new(Integer, new byte[] { 2 }) };
        Assert.That(PublishedMethodFamilyInventory.TryCreate(Definition, entries, out _), Is.False);
    }

    private static Type StructType(int index)
    {
        var values = Enumerable.Range(0, 7).Select(bit => ((index >> bit) & 1) == 0 ? typeof(byte) : typeof(short)).ToList();
        values.Add(typeof(ValueTuple<>).MakeGenericType((index >> 7) switch { 0 => typeof(byte), 1 => typeof(short), _ => typeof(int) }));
        return typeof(ValueTuple<,,,,,,,>).MakeGenericType(values.ToArray());
    }

    public class Base { public int Inherited<T>(int value) where T : struct => value; }
    public sealed class Derived : Base { }
    public sealed class GenericContainer<T> { public int Native<U>(int value) where U : struct => value; }

    private sealed class OpaqueMethod : MethodInfo
    {
        internal static int Reads;
        private static T Read<T>() { Reads++; throw new InvalidOperationException("Opaque metadata must not execute."); }
        public override string Name => Read<string>();
        public override Type? DeclaringType => Read<Type>();
        public override Type? ReflectedType => Read<Type>();
        public override Module Module => Read<Module>();
        public override int MetadataToken => Read<int>();
        public override MethodAttributes Attributes => Read<MethodAttributes>();
        public override RuntimeMethodHandle MethodHandle => Read<RuntimeMethodHandle>();
        public override Type ReturnType => Read<Type>();
        public override ICustomAttributeProvider ReturnTypeCustomAttributes => Read<ICustomAttributeProvider>();
        public override MethodInfo GetBaseDefinition() => Read<MethodInfo>();
        public override MethodImplAttributes GetMethodImplementationFlags() => Read<MethodImplAttributes>();
        public override ParameterInfo[] GetParameters() => Read<ParameterInfo[]>();
        public override object[] GetCustomAttributes(bool inherit) => Read<object[]>();
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => Read<object[]>();
        public override bool IsDefined(Type attributeType, bool inherit) => Read<bool>();
        public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture) => Read<object>();
        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
        public override bool Equals(object? obj) => ReferenceEquals(this, obj);
    }

    public static int Native<T>(int value) where T : struct => value;
    public static int Unrelated(int value) => value;
}
