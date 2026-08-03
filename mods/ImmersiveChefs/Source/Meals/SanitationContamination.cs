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

    public static ContaminationSources ForCutlery(bool isDirty, WashProvenance provenance) =>
        For(
            isDirty,
            provenance,
            ContaminationSources.DirtyCutlery,
            ContaminationSources.WildWaterCutlery);

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
