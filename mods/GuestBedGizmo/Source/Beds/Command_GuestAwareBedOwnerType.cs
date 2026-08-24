using System;
using System.Collections.Generic;
using GuestBedGizmo.Compatibility.Hospitality;
using RimWorld;
using UnityEngine;
using Verse;

namespace GuestBedGizmo.Beds;

internal sealed class Command_GuestAwareBedOwnerType : Command_Action
{
    private const int ConversionErrorLogKey = 1748243117;

    private readonly Building_Bed bed;
    private readonly HospitalityRuntimeAdapter adapter;

    internal Command_GuestAwareBedOwnerType(
        Building_Bed bed,
        HospitalityRuntimeAdapter adapter)
    {
        this.bed = bed;
        this.adapter = adapter;
        action = OpenMenu;

        var vanilla = new Command_SetBedOwnerType(bed);
        defaultLabel = vanilla.defaultLabel;
        defaultDesc = vanilla.defaultDesc;
        icon = vanilla.icon;

        if (adapter.IsGuestBed(bed))
        {
            defaultLabel = HospitalityRuntimeContract.GuestLabelKey.Translate();
            defaultDesc = HospitalityRuntimeContract.GuestDescriptionKey.Translate();
            icon = ContentFinder<Texture2D>.Get(HospitalityRuntimeContract.GuestIconPath);
        }
    }

    private void OpenMenu()
    {
        var options = new List<FloatMenuOption>(BedOwnerChoiceCatalog.All.Count);
        foreach (BedOwnerChoice choice in BedOwnerChoiceCatalog.All)
        {
            BedOwnerChoice captured = choice;
            Texture2D optionIcon = ContentFinder<Texture2D>.Get(captured.IconPath);
            options.Add(new FloatMenuOption(
                captured.LabelKey.Translate(),
                () => Apply(captured.Kind),
                optionIcon,
                Color.white));
        }

        Find.WindowStack.Add(new FloatMenu(options));
    }

    private void Apply(BedOwnerChoiceKind choice)
    {
        try
        {
            GuestBedSelectionConverter.Apply(bed, adapter, choice);
        }
        catch (Exception exception)
        {
            Log.ErrorOnce(
                $"[Guest Bed Gizmo] Bed owner selection failed safely: {exception.GetBaseException().Message}",
                ConversionErrorLogKey);
        }
    }
}
