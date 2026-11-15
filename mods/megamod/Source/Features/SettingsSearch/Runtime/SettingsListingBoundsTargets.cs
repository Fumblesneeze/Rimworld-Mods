using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using LudeonTK;
using Megamod.Features.SettingsSearch.Harmony;
using Megamod.Patching;
using UnityEngine;
using Verse;
using Draw = Megamod.Features.SettingsSearch.Harmony.SettingsListingBoundsSemantics.Draw;

namespace Megamod.Features.SettingsSearch.Runtime;

// Native declaration/original-body catalog only. Registration, effective composition and
// publication currentness are separate admission obligations owned by the neutral reader.
public static class SettingsListingBoundsTargets
{
    public sealed record Consumer(MethodInfo Method, SettingsListingBoundsSemantics Checker);

    public sealed class Catalog
    {
        public MethodInfo Getter { get; }
        public IReadOnlyList<Consumer> Consumers { get; }
        internal Catalog(MethodInfo getter, Consumer[] consumers)
        {
            Getter = getter;
            Consumers = Array.AsReadOnly((Consumer[])consumers.Clone());
        }
    }

    public static bool TryBind(out Catalog? catalog, out string? failure)
    {
        catalog = null;
        failure = null;
        try
        {
            Type listing = typeof(Listing_Standard), rectangle = typeof(Rect), widgets = typeof(Widgets);
            Type text = typeof(string), single = typeof(float), boolean = typeof(bool), texture = typeof(Texture2D);
            Type booleanRef = boolean.MakeByRefType(), textRef = text.MakeByRefType(), integerRef = typeof(int).MakeByRefType();
            var getter = Exact(listing, "get_BoundingRectCached", false, typeof(Rect?));
            var hasValue = Exact(typeof(Rect?), "get_HasValue", false, boolean);
            var value = Exact(typeof(Rect?), "get_Value", false, rectangle);
            var overlaps = Exact(rectangle, "Overlaps", false, boolean, rectangle);
            var accessors = new[] { "x", "xMin", "xMax", "y", "width", "height" }
                .SelectMany(name => new[] { Exact(rectangle, "get_" + name, false, single),
                    Exact(rectangle, "set_" + name, false, typeof(void), single) }).ToArray();
            var constructor = rectangle.GetConstructor(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                null, new[] { single, single, single, single }, null);
            Require(constructor != null && !constructor.IsStatic && constructor.DeclaringType == rectangle,
                "The native Rect four-Single constructor is unavailable.");

            var label = Exact(widgets, "Label", true, typeof(void), rectangle, text);
            var labelScrollable = Exact(widgets, "LabelScrollable", true, typeof(void), rectangle, text,
                typeof(Vector2).MakeByRefType(), boolean, boolean, boolean);
            var radio = Exact(widgets, "RadioButtonLabeled", true, boolean, rectangle, text, boolean, boolean);
            var checkbox = Exact(widgets, "CheckboxLabeled", true, typeof(void), rectangle, text, booleanRef,
                boolean, texture, texture, boolean, boolean);
            var selectable = Exact(widgets, "CheckboxLabeledSelectable", true, boolean, rectangle, text,
                booleanRef, booleanRef, texture, single);
            var button = Exact(widgets, "ButtonText", true, boolean, rectangle, text, boolean, boolean, boolean, typeof(TextAnchor?));
            var image = Exact(widgets, "ButtonImage", true, boolean, rectangle, texture, boolean, text);
            var intRange = Exact(widgets, "IntRange", true, typeof(void), rectangle, typeof(int), typeof(IntRange).MakeByRefType(),
                typeof(int), typeof(int), text, typeof(int));
            var intEntry = Exact(widgets, "IntEntry", true, typeof(void), rectangle, integerRef, textRef, typeof(int));

            var leftHalf = new SettingsListingBoundsSemantics.RectangleProjection(
                Exact(typeof(GenUI), "LeftHalf", true, rectangle, rectangle));
            var rightHalf = new SettingsListingBoundsSemantics.RectangleProjection(
                Exact(typeof(GenUI), "RightHalf", true, rectangle, rectangle));
            var leftPart = new SettingsListingBoundsSemantics.RectangleProjection(
                Exact(typeof(GenUI), "LeftPart", true, rectangle, rectangle, single), new(2));
            var rightPart = new SettingsListingBoundsSemantics.RectangleProjection(
                Exact(typeof(GenUI), "RightPart", true, rectangle, rectangle, single), new(2, true),
                new(Exact(rectangle, "set_height", false, typeof(void), single), 30f));

            var consumers = new List<Consumer>();
            void Add(MethodInfo method, params Draw[] draws) => consumers.Add(new(method,
                new(new(getter, hasValue, value, overlaps, draws, accessors, new[] { constructor! }))));
            MethodInfo Row(string name, Type result, params Type[] arguments) => Exact(listing, name, false, result, arguments);

            Add(Row("Label", rectangle, text, single, typeof(TipSignal?)), new Draw(labelScrollable), new Draw(label));
            Add(Row("LabelDouble", typeof(void), text, text, text), new Draw(label, Projection: leftHalf),
                new Draw(label, Sequence: 1, Projection: rightHalf));
            Add(Row("RadioButton", boolean, text, boolean, single, single, text, typeof(float?), boolean), new Draw(radio));
            Add(Row("CheckboxLabeled", typeof(void), text, booleanRef, single), new Draw(checkbox));
            Add(Row("CheckboxLabeled", typeof(void), text, booleanRef, text, single, single), new Draw(checkbox));
            Add(Row("CheckboxLabeledSelectable", boolean, text, booleanRef, booleanRef), new Draw(selectable));
            Add(Row("ButtonText", boolean, text, text, single), new Draw(button));
            Add(Row("ButtonTextLabeledPct", boolean, text, text, single, typeof(TextAnchor), text, text, texture),
                new Draw(label, Projection: leftPart), new Draw(button, Sequence: 1, Projection: rightPart));
            Add(Row("ButtonImage", boolean, texture, single, single), new Draw(image));
            Add(Numeric(listing, false), new Draw(Numeric(widgets, false), ConsumerGenericArguments: new[] { 0 }));
            Add(Numeric(listing, true), new Draw(Numeric(widgets, true), ConsumerGenericArguments: new[] { 0 }));
            Add(Row("IntRange", rectangle, typeof(IntRange).MakeByRefType(), typeof(int), typeof(int)), new Draw(intRange));
            Add(Row("IntEntry", typeof(void), integerRef, textRef, typeof(int), typeof(int)), new Draw(intEntry));
            Add(Row("LabelCheckboxDebug", typeof(void), text, booleanRef, boolean), new Draw(checkbox));
            Add(Row("ButtonDebug", boolean, text, boolean), new Draw(button));
            Add(Row("ButtonDebugPinnable", typeof(DebugActionButtonResult), text, boolean, boolean), new Draw(button));
            Add(Row("CheckboxPinnable", typeof(DebugActionButtonResult), text, booleanRef, boolean, boolean), new Draw(checkbox));

            Require(consumers.Count == 17 && consumers.Select(consumer => consumer.Method).Distinct().Count() == 17,
                "The native listing catalog is incomplete or ambiguous.");
            VerifyGetterUses(listing, getter, consumers.Select(consumer => consumer.Method).ToHashSet());
            foreach (var consumer in consumers)
            {
                Require(HarmonyOriginalBody.TryRead(consumer.Method, out var body, out var reason),
                    "Original native listing body unavailable for " + Name(consumer.Method) + ": " + reason + ".");
                var proof = consumer.Checker.Describe(consumer.Method, body!.CopyInstructions());
                Require(proof.Failure == null && proof.Required != null && proof.Required.Count != 0,
                    "Original native listing relationship refused for " + Name(consumer.Method) + ": " + proof.Failure);
            }
            catalog = new(getter, consumers.ToArray());
            return true;
        }
        catch (BindingFailure error) { failure = error.Message; return false; }
        catch (Exception) { failure = "Native listing catalog metadata inspection failed."; return false; }
    }

    private static MethodInfo Exact(Type owner, string name, bool isStatic, Type result, params Type[] arguments)
    {
        var method = owner.GetMethod(name, BindingFlags.Public | BindingFlags.DeclaredOnly |
            (isStatic ? BindingFlags.Static : BindingFlags.Instance), null, arguments, null);
        Require(method != null && method.DeclaringType == owner && method.IsPublic && method.IsStatic == isStatic &&
            !method.IsVirtual && !method.IsGenericMethod && !method.ContainsGenericParameters && method.ReturnType == result &&
            method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(arguments),
            "The exact native callable is unavailable: " + owner.FullName + "." + name + ".");
        return method!;
    }

    private static MethodInfo Numeric(Type owner, bool labeled)
    {
        string name = labeled ? "TextFieldNumericLabeled" : "TextFieldNumeric";
        bool widget = owner == typeof(Widgets);
        var matches = owner.GetMethods(BindingFlags.Public | BindingFlags.DeclaredOnly |
            (widget ? BindingFlags.Static : BindingFlags.Instance)).Where(method =>
        {
            if (method.Name != name || method.DeclaringType != owner || method.IsStatic != widget || method.IsVirtual ||
                !method.IsGenericMethodDefinition || method.ReturnType != typeof(void) || method.GetGenericArguments().Length != 1) return false;
            Type parameter = method.GetGenericArguments()[0];
            if ((parameter.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask) !=
                (GenericParameterAttributes.NotNullableValueTypeConstraint | GenericParameterAttributes.DefaultConstructorConstraint) ||
                !parameter.GetGenericParameterConstraints().SequenceEqual(new[] { typeof(ValueType) })) return false;
            var arguments = new List<Type>();
            if (widget) arguments.Add(typeof(Rect));
            if (labeled) arguments.Add(typeof(string));
            arguments.AddRange(new[] { parameter.MakeByRefType(), typeof(string).MakeByRefType(), typeof(float), typeof(float) });
            return method.GetParameters().Select(value => value.ParameterType).SequenceEqual(arguments);
        }).ToArray();
        Require(matches.Length == 1, "The exact native generic callable is unavailable: " + owner.FullName + "." + name + ".");
        return matches[0];
    }

    private static void VerifyGetterUses(Type listing, MethodInfo getter, ISet<MethodInfo> expected)
    {
        const BindingFlags members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        var declarations = listing.GetMethods(members).Cast<MethodBase>().Concat(listing.GetConstructors(members))
            .Concat(listing.TypeInitializer is ConstructorInfo initializer ? new MethodBase[] { initializer } : Array.Empty<MethodBase>())
            .Distinct().ToArray();
        Require(declarations.Length <= 256, "Native listing declaration inventory exceeds its bound.");
        var found = new HashSet<MethodInfo>();
        int total = 0;
        foreach (var declaration in declarations)
        {
            // Only reference inventory here: unrelated EH is allowed. Consumed originals are
            // subsequently read with HarmonyOriginalBody's stricter semantic-body policy.
            byte[]? code = declaration.GetMethodBody()?.GetILAsByteArray();
            Require(code != null && code.Length != 0 && code.Length <= 65536 && (total += code.Length) <= 1048576,
                "Native listing reference inventory is unavailable or exceeds its bound: " + Name(declaration) + ".");
            Require(HarmonyPublishedBody.TryDecode(code!, out var operations, out _) && operations!.Count <= 4096,
                "Native listing reference inventory is malformed or exceeds its bound: " + Name(declaration) + ".");
            Type[]? methodArguments = declaration is MethodInfo method && method.IsGenericMethod ? method.GetGenericArguments() : null;
            foreach (var operation in operations!)
            {
                MethodBase? referenced;
                try
                {
                    if (operation.Code.OperandType == OperandType.InlineMethod)
                    {
                        referenced = declaration.Module.ResolveMethod((int)operation.Operand!, null, methodArguments);
                        Require(referenced != null, "A native listing method operand is unresolved: " + Name(declaration) + ".");
                    }
                    else if (operation.Code.OperandType == OperandType.InlineTok)
                        referenced = declaration.Module.ResolveMember((int)operation.Operand!, null, methodArguments) as MethodBase;
                    else continue;
                }
                catch (BindingFailure) { throw; }
                catch (Exception) { throw new BindingFailure("A native listing member operand is unreadable: " + Name(declaration) + "."); }
                if (!Equals(referenced, getter)) continue;
                Require((operation.Code == OpCodes.Call || operation.Code == OpCodes.Callvirt) && declaration is MethodInfo caller && expected.Contains(caller),
                    "An unbound native listing getter use was found: " + Name(declaration) + ".");
                found.Add((MethodInfo)declaration);
            }
        }
        Require(found.SetEquals(expected), "The native listing direct getter consumer inventory changed.");
    }

    private static string Name(MethodBase method)
    {
        string name = method.ToString() ?? method.Name;
        return name.Length <= 240 ? name : name.Substring(0, 240);
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new BindingFailure(message);
    }
    private sealed class BindingFailure : Exception
    {
        internal BindingFailure(string message) : base(message) { }
    }
}
