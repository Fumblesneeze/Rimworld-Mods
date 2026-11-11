using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;
using UnityEngine;

namespace RimWorldDevGateway.Waylith.Tests;

[TestFixture]
public sealed class NativeMouseProjectionTests
{
    private static readonly MethodInfo MousePosition = typeof(Input).GetProperty(nameof(Input.mousePosition))!.GetMethod!;
    private static readonly MethodInfo Height = typeof(Host).GetProperty(nameof(Host.DisplayHeight))!.GetMethod!;
    private static readonly FieldInfo X = typeof(Vector3).GetField(nameof(Vector3.x))!;
    private static readonly FieldInfo Y = typeof(Vector3).GetField(nameof(Vector3.y))!;
    private static readonly ConstructorInfo Point = typeof(Vector2).GetConstructor(new[] { typeof(float), typeof(float) })!;

    [TestCase(0)]
    [TestCase(1)]
    public void InspectedNativeMappingsAdmitTheirOwnInverse(int offset)
    {
        var result = GatewayNativeWindowInputContract.MouseOriginOffset(Body(offset), MousePosition, Height, X, Y, Point);
        Assert.That(result, Is.EqualTo((double)offset));
    }

    [TestCase("different-offset")]
    [TestCase("wrong-axis")]
    [TestCase("wrong-local")]
    [TestCase("branch")]
    public void ChangedProjectionCannotBeGuessed(string change)
    {
        var body = Body(1);
        if (change == "different-offset") body[6] = Op(OpCodes.Ldc_R4, 2f);
        if (change == "wrong-axis") body[9] = Op(OpCodes.Ldfld, X);
        if (change == "wrong-local") body[8] = Op(OpCodes.Ldloc, (9, typeof(Vector3), false));
        if (change == "branch") body.Insert(0, new GatewayNativeInstructionShape(OpCodes.Br.Value, null,
            new[] { 3 }, Array.Empty<(int, Type?)>()));
        Assert.That(GatewayNativeWindowInputContract.MouseOriginOffset(body, MousePosition, Height, X, Y, Point), Is.Null);
    }

    private static List<GatewayNativeInstructionShape> Body(int offset)
    {
        object local = (3, typeof(Vector3), false);
        var body = new List<GatewayNativeInstructionShape>
        { Op(OpCodes.Call, MousePosition), Op(OpCodes.Stloc, local), Op(OpCodes.Ldloc, local),
            Op(OpCodes.Ldfld, X), Op(OpCodes.Ldarg, 0), Op(OpCodes.Callvirt, Height) };
        if (offset != 0) { body.Add(Op(OpCodes.Ldc_R4, (float)offset)); body.Add(Op(OpCodes.Sub)); }
        body.AddRange(new[] { Op(OpCodes.Ldloc, local), Op(OpCodes.Ldfld, Y), Op(OpCodes.Sub), Op(OpCodes.Newobj, Point), Op(OpCodes.Ret) });
        return body;
    }

    private static GatewayNativeInstructionShape Op(OpCode code, object? operand = null) =>
        new(code.Value, operand, Array.Empty<int>(), Array.Empty<(int, Type?)>());
    private sealed class Host { public float DisplayHeight => 900; }
}
