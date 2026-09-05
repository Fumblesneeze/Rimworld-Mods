using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace PersonalBugfixes.Exploration;

// All optional types are resolved at the package boundary; no external assembly reference.
public sealed class ExplorationTarget
{
    public MethodInfo Update { get; }
    public FieldInfo Learned { get; }
    public Type Component { get; }
    public Type Feature { get; }
    public Type World { get; }
    public MethodInfo WorldGetter { get; }
    public MethodInfo StateGetter { get; }
    public FieldInfo WorldFeatures { get; }
    public FieldInfo Features { get; }
    public FieldInfo Renderer { get; }
    public FieldInfo UpdateType { get; }
    public FieldInfo RevealAll { get; }
    public FieldInfo Layer { get; }
    public MethodInfo Tiles { get; }

    public ExplorationTarget(Type visibility, Type component, Type world, Type feature, Type find, Type current)
    {
        Component = component; Feature = feature; World = world;
        Update = visibility.GetMethod("UpdateGraphics", BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
            null, Type.EmptyTypes, null) ?? throw new InvalidOperationException("Missing static UpdateGraphics().");
        if (Update.ReturnType != typeof(void) || Update.IsGenericMethod) throw new InvalidOperationException("UpdateGraphics signature changed.");
        Learned = RequiredField(component, "learnedFeatures", false);
        if (Learned.FieldType != typeof(List<bool>)) throw new InvalidOperationException("learnedFeatures is not List<bool>.");
        WorldGetter = RequiredGetter(find, "World", true, world);
        StateGetter = RequiredGetter(current, "ProgramState", true, null);
        WorldFeatures = RequiredField(world, "features", false);
        Features = RequiredField(WorldFeatures.FieldType, "features", false);
        if (Features.FieldType != typeof(List<>).MakeGenericType(feature)) throw new InvalidOperationException("World feature list changed.");
        Renderer = RequiredField(world, "renderer", false);
        UpdateType = RequiredField(visibility, "_updateType", true);
        if (!UpdateType.FieldType.IsEnum || Enum.GetUnderlyingType(UpdateType.FieldType) != typeof(int) ||
            !Enum.IsDefined(UpdateType.FieldType, "Full") || Convert.ToInt32(Enum.Parse(UpdateType.FieldType, "Full")) != 2 ||
            !Enum.IsDefined(UpdateType.FieldType, "Planet") || Convert.ToInt32(Enum.Parse(UpdateType.FieldType, "Planet")) != 3)
            throw new InvalidOperationException("Full/Planet update discriminators changed.");
        RevealAll = RequiredField(visibility, "RevealAll", true);
        Layer = RequiredField(visibility, "VisibilityLayer", true);
        if (RevealAll.FieldType != typeof(bool) || Layer.FieldType.IsValueType) throw new InvalidOperationException("Visibility flags changed.");
        Tiles = RequiredGetter(feature, "Tiles", false, typeof(IEnumerable<int>));
    }

    private static FieldInfo RequiredField(Type type, string name, bool isStatic)
    {
        var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | (isStatic ? BindingFlags.Static : BindingFlags.Instance));
        if (field == null || field.IsStatic != isStatic) throw new InvalidOperationException($"Missing {type.FullName}.{name} field.");
        return field;
    }

    private static MethodInfo RequiredGetter(Type type, string name, bool isStatic, Type? result)
    {
        var method = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | (isStatic ? BindingFlags.Static : BindingFlags.Instance))?.GetGetMethod(true);
        if (method == null || method.IsStatic != isStatic || method.IsGenericMethod || method.GetParameters().Length != 0 ||
            (result != null && method.ReturnType != result)) throw new InvalidOperationException($"Unexpected {type.FullName}.{name} getter.");
        return method;
    }

    public ProbeFixture CreateFixture()
    {
        var world = FormatterServices.GetUninitializedObject(World);
        var component = FormatterServices.GetUninitializedObject(Component);
        var features = FormatterServices.GetUninitializedObject(WorldFeatures.FieldType);
        var list = (IList)Activator.CreateInstance(Features.FieldType)!;
        for (int i = 0; i < 3; i++) list.Add(FormatterServices.GetUninitializedObject(Feature));
        Features.SetValue(features, list);
        WorldFeatures.SetValue(world, features);
        var learned = new List<bool> { true, false };
        Learned.SetValue(component, learned);
        return new ProbeFixture(world, component, learned);
    }
}

public sealed class ProbeFixture
{
    public ProbeFixture(object world, object component, List<bool> learned)
    { World = world; Component = component; Learned = learned; }
    public object World { get; }
    public object Component { get; }
    public List<bool> Learned { get; }
}
