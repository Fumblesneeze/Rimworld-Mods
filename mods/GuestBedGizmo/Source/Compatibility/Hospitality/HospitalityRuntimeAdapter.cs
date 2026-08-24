using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace GuestBedGizmo.Compatibility.Hospitality;

internal static class HospitalityRuntimeContract
{
    internal static readonly Guid SupportedModuleVersionId =
        new Guid("2960a88a-6247-4553-ae68-04319c05246e");
    internal const string AssemblySimpleName = "Hospitality";
    internal const string HarmonyOwner = "Orion.Hospitality";
    internal const string GuestBedTypeName = "Hospitality.Building_GuestBed";
    internal const string SwapMethodName = "Swap";
    internal const string LegacyPatchTypeName = "Hospitality.Patches.Building_Bed_Patch+GetGizmos";
    internal const string LegacyActionTypeName =
        "Hospitality.Patches.Building_Bed_Patch+GetGizmos+<>c__DisplayClass1_0";
    internal const string LegacyActionMethodName = "<Process>b__0";
    internal const string GuestLabelKey = "CommandBedSetAsGuestLabel";
    internal const string GuestDescriptionKey = "CommandBedSetAsGuestDesc";
    internal const string GuestIconPath = "UI/Commands/AsGuest";
}

internal sealed class HospitalityRuntimeAdapter
{
    private readonly MethodInfo swapMethod;
    private readonly Type legacyActionType;

    private HospitalityRuntimeAdapter(Type guestBedType, MethodInfo swapMethod, Type legacyActionType)
    {
        GuestBedType = guestBedType;
        this.swapMethod = swapMethod;
        this.legacyActionType = legacyActionType;
    }

    internal Type GuestBedType { get; }

    internal static bool TryResolve(
        IEnumerable<Assembly> assemblies,
        out HospitalityRuntimeAdapter? adapter,
        out string? failure)
    {
        adapter = null;
        failure = null;

        Assembly[] matches = assemblies
            .Where(assembly => string.Equals(
                assembly.GetName().Name,
                HospitalityRuntimeContract.AssemblySimpleName,
                StringComparison.Ordinal))
            .ToArray();
        if (matches.Length != 1)
        {
            failure = $"expected one loaded {HospitalityRuntimeContract.AssemblySimpleName} assembly, found {matches.Length}";
            return false;
        }

        Assembly assembly = matches[0];
        if (assembly.ManifestModule.ModuleVersionId != HospitalityRuntimeContract.SupportedModuleVersionId)
        {
            failure = $"unsupported Hospitality module identity {assembly.ManifestModule.ModuleVersionId}";
            return false;
        }

        Type? guestBedType = assembly.GetType(HospitalityRuntimeContract.GuestBedTypeName, false);
        if (guestBedType == null || !guestBedType.IsPublic ||
            !typeof(Building_Bed).IsAssignableFrom(guestBedType))
        {
            failure = $"missing public {HospitalityRuntimeContract.GuestBedTypeName} : Building_Bed";
            return false;
        }

        MethodInfo? swap = guestBedType.GetMethod(
            HospitalityRuntimeContract.SwapMethodName,
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(Building_Bed) },
            null);
        if (swap == null || swap.ReturnType != typeof(void) || swap.IsGenericMethod)
        {
            failure = $"missing public static void {HospitalityRuntimeContract.GuestBedTypeName}.{HospitalityRuntimeContract.SwapMethodName}(Building_Bed)";
            return false;
        }

        Type? legacyPatchType = assembly.GetType(HospitalityRuntimeContract.LegacyPatchTypeName, false);
        MethodInfo? postfix = legacyPatchType?.GetMethod(
            "Postfix",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[]
            {
                typeof(Building_Bed),
                typeof(IEnumerable<Gizmo>).MakeByRefType(),
            },
            null);
        if (postfix == null || postfix.ReturnType != typeof(void) || postfix.IsGenericMethod)
        {
            failure = $"missing exact {HospitalityRuntimeContract.LegacyPatchTypeName}.Postfix shape";
            return false;
        }

        Type? legacyActionType = assembly.GetType(HospitalityRuntimeContract.LegacyActionTypeName, false);
        MethodInfo? legacyAction = legacyActionType?.GetMethod(
            HospitalityRuntimeContract.LegacyActionMethodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            Type.EmptyTypes,
            null);
        FieldInfo? instanceField = legacyActionType?.GetField(
            "__instance",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (legacyActionType == null || legacyAction == null || legacyAction.IsStatic ||
            legacyAction.ReturnType != typeof(void) || instanceField == null ||
            instanceField.FieldType != typeof(Building_Bed))
        {
            failure = $"missing exact {HospitalityRuntimeContract.LegacyActionTypeName}.{HospitalityRuntimeContract.LegacyActionMethodName} action shape";
            return false;
        }

        adapter = new HospitalityRuntimeAdapter(guestBedType, swap, legacyActionType);
        return true;
    }

    internal bool TryValidateUiAssets(out string? failure)
    {
        failure = null;
        try
        {
            if (!Translator.CanTranslate(HospitalityRuntimeContract.GuestLabelKey) ||
                !Translator.CanTranslate(HospitalityRuntimeContract.GuestDescriptionKey))
            {
                failure = "Hospitality guest-bed translation keys are unavailable";
                return false;
            }

            if (ContentFinder<Texture2D>.Get(HospitalityRuntimeContract.GuestIconPath, false) == null)
            {
                failure = $"Hospitality texture '{HospitalityRuntimeContract.GuestIconPath}' is unavailable";
                return false;
            }
        }
        catch (Exception)
        {
            failure = "Hospitality guest-bed UI assets are not available at this load phase";
            return false;
        }

        return true;
    }

    internal bool IsGuestBed(Building_Bed bed) => bed != null && GuestBedType.IsInstanceOfType(bed);

    internal bool CanSwapTo(Building_Bed bed, bool expectedGuestState, out string? failure)
    {
        failure = null;
        if (bed == null || !bed.Spawned || bed.Map == null)
        {
            failure = "the source bed is not spawned on a map";
            return false;
        }

        if (IsGuestBed(bed) == expectedGuestState)
        {
            return true;
        }

        string[] vanillaNameParts = bed.def.defName.Split(
            new[] { "Guest" },
            StringSplitOptions.RemoveEmptyEntries);
        if (!expectedGuestState && vanillaNameParts.Length == 0)
        {
            failure = $"the guest bed Def '{bed.def.defName}' has no corresponding vanilla name";
            return false;
        }

        string replacementDefName = expectedGuestState
            ? bed.def.defName + "Guest"
            : vanillaNameParts[0];
        ThingDef? replacement = DefDatabase<ThingDef>.GetNamedSilentFail(replacementDefName);
        if (replacement?.thingClass == null ||
            !typeof(Building_Bed).IsAssignableFrom(replacement.thingClass) ||
            (expectedGuestState && !GuestBedType.IsAssignableFrom(replacement.thingClass)) ||
            (!expectedGuestState && GuestBedType.IsAssignableFrom(replacement.thingClass)))
        {
            failure = $"the exact replacement bed Def '{replacementDefName}' is unavailable";
            return false;
        }

        return true;
    }

    internal bool IsLegacyGuestToggle(Gizmo gizmo)
    {
        if (!(gizmo is Command_Toggle toggle) || toggle.toggleAction == null)
        {
            return false;
        }

        MethodInfo method = toggle.toggleAction.Method;
        return method.DeclaringType == legacyActionType &&
               string.Equals(
                   method.Name,
                   HospitalityRuntimeContract.LegacyActionMethodName,
                   StringComparison.Ordinal);
    }

    internal void Swap(Building_Bed bed) => swapMethod.Invoke(null, new object[] { bed });
}
