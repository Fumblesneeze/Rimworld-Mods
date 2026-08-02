namespace ImmersiveChefs;

public readonly struct SanitationSnapshot
{
    public SanitationSnapshot(int schemaVersion, bool isDirty)
    {
        SchemaVersion = schemaVersion;
        IsDirty = isDirty;
    }

    public int SchemaVersion { get; }

    public bool IsDirty { get; }
}

public sealed class SanitationStateModel
{
    public const int CurrentSchemaVersion = 1;

    public bool IsDirty { get; private set; }

    public void MarkDirty()
    {
        IsDirty = true;
    }

    public void MarkClean()
    {
        IsDirty = false;
    }

    public SanitationSnapshot Capture()
    {
        return new SanitationSnapshot(CurrentSchemaVersion, IsDirty);
    }

    public static SanitationStateModel Restore(SanitationSnapshot snapshot)
    {
        return new SanitationStateModel { IsDirty = snapshot.IsDirty };
    }

    public bool CanStackWith(SanitationStateModel? other)
    {
        return other is not null && IsDirty == other.IsDirty;
    }
}
