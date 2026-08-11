using System.Collections;
using System.Reflection;

namespace RimWorldDevGateway.Performance;

internal sealed class ReflectionPerformanceHarmonyCatalog : IPerformanceHarmonyCatalog
{
    // Harmony's global patch registry belongs to the loaded game, not this repository. This high
    // host-safety ceiling prevents a corrupt provider from enumerating forever; the much smaller
    // Circinus 1000-method/3000-patch output ceilings are enforced after owner selection.
    public const int MaximumPatchedTargets = 65_536;
    public const int MaximumPatchAttachments = 65_536;

    private readonly MethodInfo getAllPatchedMethods;
    private readonly MethodInfo getPatchInfo;
    private readonly IReadOnlyDictionary<PerformanceHarmonyPatchKind, FieldInfo> patchCollections;
    private readonly FieldInfo patchOwner;
    private readonly PropertyInfo patchMethod;

    private ReflectionPerformanceHarmonyCatalog(
        MethodInfo getAllPatchedMethods,
        MethodInfo getPatchInfo,
        IReadOnlyDictionary<PerformanceHarmonyPatchKind, FieldInfo> patchCollections,
        FieldInfo patchOwner,
        PropertyInfo patchMethod)
    {
        this.getAllPatchedMethods = getAllPatchedMethods;
        this.getPatchInfo = getPatchInfo;
        this.patchCollections = patchCollections;
        this.patchOwner = patchOwner;
        this.patchMethod = patchMethod;
    }

    public static bool TryBind(
        IEnumerable<Assembly> loadedAssemblies,
        out ReflectionPerformanceHarmonyCatalog? catalog,
        out string reason)
    {
        catalog = null;
        if (loadedAssemblies is null)
        {
            reason = "The loaded-assembly catalog is unavailable.";
            return false;
        }

        try
        {
            var matches = loadedAssemblies.Where(assembly => assembly is not null && string.Equals(
                assembly.GetName().Name,
                "0Harmony",
                StringComparison.Ordinal)).ToArray();
            if (matches.Length != 1)
            {
                reason = matches.Length == 0
                    ? "Harmony assembly '0Harmony' is not loaded."
                    : $"Expected one loaded Harmony assembly, observed {matches.Length}.";
                return false;
            }

            var assembly = matches[0];
            var harmony = ExactType(assembly, "HarmonyLib.Harmony");
            var patches = ExactType(assembly, "HarmonyLib.Patches");
            var patch = ExactType(assembly, "HarmonyLib.Patch");
            var getAll = ExactMethod(
                harmony,
                "GetAllPatchedMethods",
                isStatic: true,
                typeof(IEnumerable<MethodBase>));
            var getInfo = ExactMethod(
                harmony,
                "GetPatchInfo",
                isStatic: true,
                patches,
                typeof(MethodBase));
            var owner = ExactField(patch, "owner", isStatic: false, typeof(string));
            var method = ExactProperty(patch, "PatchMethod", isStatic: false, typeof(MethodInfo));
            var collections = new Dictionary<PerformanceHarmonyPatchKind, FieldInfo>
            {
                [PerformanceHarmonyPatchKind.Prefix] = ExactPatchCollection(patches, patch, "Prefixes"),
                [PerformanceHarmonyPatchKind.Postfix] = ExactPatchCollection(patches, patch, "Postfixes"),
                [PerformanceHarmonyPatchKind.Finalizer] = ExactPatchCollection(patches, patch, "Finalizers"),
                [PerformanceHarmonyPatchKind.Transpiler] = ExactPatchCollection(patches, patch, "Transpilers")
            };

            catalog = new ReflectionPerformanceHarmonyCatalog(getAll, getInfo, collections, owner, method);
            reason = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            reason = "Unsupported Harmony patch-discovery shape: " +
                     CircinusRuntimeAdapter.Describe(exception);
            return false;
        }
    }

    public IReadOnlyList<PerformanceHarmonyPatch> ResolveOwner(string exactOwnerId)
    {
        if (string.IsNullOrWhiteSpace(exactOwnerId))
            throw new PerformanceMethodSelectionException("Harmony owner is empty.");

        try
        {
            var targets = getAllPatchedMethods.Invoke(null, Array.Empty<object>()) as IEnumerable ??
                          throw new InvalidOperationException("Harmony GetAllPatchedMethods returned no sequence.");
            var resolved = new List<PerformanceHarmonyPatch>();
            var patchAttachmentCount = 0;
            foreach (var item in MaterializeBounded(
                         targets.Cast<object?>(),
                         MaximumPatchedTargets,
                         "patched targets"))
            {
                if (item is not MethodBase target)
                    throw new InvalidOperationException("Harmony exposed a non-method patched target.");
                var info = getPatchInfo.Invoke(null, new object[] { target });
                if (info is null) continue;
                foreach (var collection in patchCollections)
                {
                    var entries = collection.Value.GetValue(info) as IEnumerable ??
                                  throw new InvalidOperationException(
                                      $"Harmony {collection.Value.Name} returned no sequence.");
                    foreach (var entry in entries)
                    {
                        patchAttachmentCount = IncrementBounded(
                            patchAttachmentCount,
                            MaximumPatchAttachments,
                            "patch attachments");
                        if (entry is null) throw new InvalidOperationException("Harmony exposed a null patch entry.");
                        var owner = patchOwner.GetValue(entry) as string;
                        if (!string.Equals(owner, exactOwnerId, StringComparison.Ordinal)) continue;
                        var method = patchMethod.GetValue(entry) as MethodInfo ??
                                     throw new InvalidOperationException("Harmony patch has no exact PatchMethod.");
                        resolved.Add(new PerformanceHarmonyPatch(owner!, collection.Key, target, method));
                    }
                }
            }

            return resolved.OrderBy(item => PerformanceMethodIdentity.Of(item.PatchMethod), StringComparer.Ordinal)
                .ThenBy(item => PerformanceMethodIdentity.Of(item.PatchedTarget), StringComparer.Ordinal)
                .ThenBy(item => item.Kind)
                .ToArray();
        }
        catch (PerformanceMethodSelectionException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new PerformanceMethodSelectionException(
                "Could not discover Harmony owner '" + exactOwnerId + "': " +
                CircinusRuntimeAdapter.Describe(exception));
        }
    }

    internal static IReadOnlyList<T> MaterializeBounded<T>(
        IEnumerable<T> values,
        int maximum,
        string label)
    {
        var result = new List<T>(Math.Min(maximum, 1024));
        foreach (var value in values)
        {
            if (result.Count >= maximum)
                throw new PerformanceMethodSelectionException(
                    $"Harmony exposed more than {maximum} {label}.");
            result.Add(value);
        }
        return result;
    }

    internal static int IncrementBounded(int current, int maximum, string label)
    {
        if (current >= maximum)
            throw new PerformanceMethodSelectionException(
                $"Harmony exposed more than {maximum} {label}.");
        return current + 1;
    }

    private static Type ExactType(Assembly assembly, string fullName) =>
        assembly.GetType(fullName, false, false) ??
        throw new MissingMemberException(fullName);

    private static MethodInfo ExactMethod(
        Type declaringType,
        string name,
        bool isStatic,
        Type returnType,
        params Type[] parameterTypes)
    {
        var method = declaringType.GetMethod(
            name,
            BindingFlags.Public | (isStatic ? BindingFlags.Static : BindingFlags.Instance),
            binder: null,
            types: parameterTypes,
            modifiers: null);
        if (method is null || method.IsStatic != isStatic || method.ReturnType != returnType ||
            method.DeclaringType != declaringType)
            throw new MissingMemberException(declaringType.FullName, name);
        return method;
    }

    private static FieldInfo ExactField(Type type, string name, bool isStatic, Type fieldType)
    {
        var field = type.GetField(
            name,
            BindingFlags.Public | (isStatic ? BindingFlags.Static : BindingFlags.Instance));
        if (field is null || field.IsStatic != isStatic || field.FieldType != fieldType ||
            field.DeclaringType != type)
            throw new MissingMemberException(type.FullName, name);
        return field;
    }

    private static PropertyInfo ExactProperty(Type type, string name, bool isStatic, Type propertyType)
    {
        var property = type.GetProperty(
            name,
            BindingFlags.Public | (isStatic ? BindingFlags.Static : BindingFlags.Instance));
        var getter = property?.GetGetMethod(nonPublic: false);
        if (property is null || getter is null || getter.IsStatic != isStatic ||
            property.PropertyType != propertyType || property.DeclaringType != type)
            throw new MissingMemberException(type.FullName, name);
        return property;
    }

    private static FieldInfo ExactPatchCollection(Type patches, Type patch, string name)
    {
        var expected = typeof(System.Collections.ObjectModel.ReadOnlyCollection<>).MakeGenericType(patch);
        return ExactField(patches, name, isStatic: false, expected);
    }
}
