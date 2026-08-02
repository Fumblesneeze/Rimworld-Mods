namespace ImmersiveChefs;

public static class SanitationContamination
{
    public static ContaminationSources ForCookware(bool isDirty, WashProvenance provenance) =>
        For(
            isDirty,
            provenance,
            ContaminationSources.DirtyCookware,
            ContaminationSources.WildWaterCookware);

    public static ContaminationSources ForPlate(bool isDirty, WashProvenance provenance) =>
        For(
            isDirty,
            provenance,
            ContaminationSources.DirtyPlate,
            ContaminationSources.WildWaterPlate);

    public static ContaminationSources ForSilverware(bool isDirty, WashProvenance provenance) =>
        For(
            isDirty,
            provenance,
            ContaminationSources.DirtySilverware,
            ContaminationSources.WildWaterSilverware);

    private static ContaminationSources For(
        bool isDirty,
        WashProvenance provenance,
        ContaminationSources dirty,
        ContaminationSources wildWater)
    {
        var result = isDirty ? dirty : ContaminationSources.None;
        if (provenance == WashProvenance.WildWater)
        {
            result |= wildWater;
        }

        return result;
    }
}
