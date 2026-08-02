namespace ImmersiveChefs;

public enum WashProvenance
{
    None = 0,
    Safe = 1,
    WildWater = 2
}

public readonly struct SanitationSnapshot
{
    public SanitationSnapshot(int schemaVersion, bool isDirty, WashProvenance washProvenance)
    {
        SchemaVersion = schemaVersion;
        IsDirty = isDirty;
        WashProvenance = washProvenance;
    }

    public int SchemaVersion { get; }

    public bool IsDirty { get; }

    public WashProvenance WashProvenance { get; }
}

public sealed class SanitationStateModel
{
    public const int CurrentSchemaVersion = 2;

    public bool IsDirty { get; private set; }

    public WashProvenance WashProvenance { get; private set; }

    public void MarkDirty()
    {
        IsDirty = true;
    }

    public void MarkClean(WashProvenance washProvenance = WashProvenance.Safe)
    {
        IsDirty = false;
        WashProvenance = washProvenance;
    }

    public SanitationSnapshot Capture()
    {
        return new SanitationSnapshot(CurrentSchemaVersion, IsDirty, WashProvenance);
    }

    public static SanitationStateModel Restore(SanitationSnapshot snapshot)
    {
        return new SanitationStateModel
        {
            IsDirty = snapshot.IsDirty,
            WashProvenance = snapshot.WashProvenance
        };
    }

    public bool CanStackWith(SanitationStateModel? other)
    {
        return other is not null &&
               IsDirty == other.IsDirty &&
               WashProvenance == other.WashProvenance;
    }
}
