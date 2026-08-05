using Verse;

namespace ImmersiveChefs;

public sealed class CompProperties_CulinaryState : CompProperties
{
    public CompProperties_CulinaryState()
    {
        compClass = typeof(CompCulinaryState);
    }
}

public sealed class CompCulinaryState : ThingComp
{
    private List<CulinaryServingData> servings = new();

    public IReadOnlyList<CulinaryServingRecord> Servings => servings.Select(item => item.ToRecord()).ToList().AsReadOnly();

    public void AddServing(CulinaryServingRecord record)
    {
        servings.Add(CulinaryServingData.From(record));
    }

    public void ReplaceServings(IEnumerable<CulinaryServingRecord> records)
    {
        servings = records.Select(CulinaryServingData.From).ToList();
    }

    public CulinaryServingRecord? ConsumeOne()
    {
        EnsureServingCount();
        if (servings.Count == 0)
        {
            return null;
        }

        var index = servings.Count - 1;
        AdvanceServing(index);
        var serving = servings[index].ToRecord();
        servings.RemoveAt(index);
        return serving;
    }

    public CulinaryServingRecord? PeekCurrentServing()
    {
        EnsureServingCount();
        if (servings.Count == 0)
        {
            return null;
        }

        var index = servings.Count - 1;
        AdvanceServing(index);
        return servings[index].ToRecord();
    }

    internal CulinaryServingRecord? PeekCurrentServingWithoutThermalUpdate()
    {
        EnsureServingCount();
        return servings.Count == 0 ? null : servings[^1].ToRecord();
    }

    public bool ReheatCurrentServing(float targetTemperature, int qualityLoss, int currentTick)
    {
        if (!TemperatureOwnership.ImmersiveChefsFeaturesActive)
        {
            return false;
        }

        EnsureServingCount();
        if (servings.Count == 0)
        {
            return false;
        }

        var index = servings.Count - 1;
        AdvanceServing(index);
        var record = servings[index].ToRecord();
        record.Reheat(targetTemperature, qualityLoss, currentTick);
        servings[index] = CulinaryServingData.From(record);
        return true;
    }

    public void AddContaminationToCurrent(ContaminationSources contamination)
    {
        EnsureServingCount();
        if (servings.Count == 0)
        {
            return;
        }

        var index = servings.Count - 1;
        var record = servings[index].ToRecord();
        record.AddContamination(contamination);
        servings[index] = CulinaryServingData.From(record);
    }

    internal void AddContaminationToServing(int index, ContaminationSources contamination)
    {
        EnsureServingCount();
        if (index < 0 || index >= servings.Count)
        {
            return;
        }

        var record = servings[index].ToRecord();
        record.AddContamination(contamination);
        servings[index] = CulinaryServingData.From(record);
    }

    public void EnsureServingCount()
    {
        while (servings.Count < parent.stackCount)
        {
            var ownsTemperature = TemperatureOwnership.ImmersiveChefsFeaturesActive;
            var ambient = ownsTemperature ? parent.AmbientTemperature : 21f;
            var tick = ownsTemperature ? Find.TickManager?.TicksGame ?? 0 : 0;
            servings.Add(CulinaryServingData.From(new CulinaryServingRecord(
                40, ambient, ContaminationSources.None, 0, tick)));
        }
    }

    public override void PostPostMake()
    {
        EnsureServingCount();
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        EnsureServingCount();
    }

    public override void PostSplitOff(Thing piece)
    {
        if (ReferenceEquals(piece, parent))
        {
            return;
        }

        EnsureServingCount();
        var target = (piece as ThingWithComps)?.GetComp<CompCulinaryState>();
        if (target is null)
        {
            return;
        }

        var count = Math.Min(piece.stackCount, servings.Count);
        var start = servings.Count - count;
        target.servings = servings.GetRange(start, count).Select(item => item.Copy()).ToList();
        servings.RemoveRange(start, count);
    }

    public override void PreAbsorbStack(Thing otherStack, int count)
    {
        var source = (otherStack as ThingWithComps)?.GetComp<CompCulinaryState>();
        if (source is null)
        {
            return;
        }

        source.EnsureServingCount();
        var transfer = Math.Min(count, source.servings.Count);
        var start = source.servings.Count - transfer;
        servings.AddRange(source.servings.GetRange(start, transfer).Select(item => item.Copy()));
        source.servings.RemoveRange(start, transfer);
    }

    public override string CompInspectStringExtra()
    {
        EnsureServingCount();
        if (servings.Count == 0)
        {
            return string.Empty;
        }

        var record = PeekCurrentServing()!;
        var lines = new List<string>();
        if (ImmersiveChefsMod.Settings.CulinaryQualityEnabled)
        {
            lines.Add($"Culinary quality: {CulinaryQualityCalculator.LabelFor(record.QualityScore)} ({record.QualityScore})");
        }

        if (TemperatureOwnership.ImmersiveChefsFeaturesActive &&
            ImmersiveChefsMod.Settings.MealTemperatureEnabled)
        {
            lines.Add($"Meal temperature: {ThermalCalculator.BandFor(record.TemperatureCelsius)} ({record.TemperatureCelsius:0.#} °C)");
        }

        return string.Join("\n", lines);
    }

    public override void PostIngested(Pawn ingester)
    {
        ConsumeOne();
    }

    public override void PostExposeData()
    {
        List<CulinaryServingData>? persisted = servings;
        Scribe_Collections.Look(ref persisted, "culinaryServings", LookMode.Deep);
        servings = persisted ?? new List<CulinaryServingData>();
    }

    private void AdvanceServing(int index)
    {
        if (!TemperatureOwnership.ImmersiveChefsFeaturesActive ||
            !ImmersiveChefsMod.Settings.MealTemperatureEnabled ||
            index < 0 ||
            index >= servings.Count)
        {
            return;
        }

        var record = servings[index].ToRecord();
        record.AdvanceTemperature(
            parent.AmbientTemperature,
            Find.TickManager?.TicksGame ?? record.LastThermalTick,
            ImmersiveChefsMod.Settings.ThermalHalfLifeHours);
        servings[index] = CulinaryServingData.From(record);
    }

    private sealed class CulinaryServingData : IExposable
    {
        private int schemaVersion = CulinaryServingRecord.CurrentSchemaVersion;
        private int qualityScore = 40;
        private float temperatureCelsius = 21f;
        private int contamination;
        private int microwaveReheatCount;
        private int lastThermalTick;
        private List<string> hiddenSourceDefNames = new();
        private int hiddenDietaryFlags;

        public void ExposeData()
        {
            Scribe_Values.Look(ref schemaVersion, "schemaVersion", CulinaryServingRecord.CurrentSchemaVersion);
            Scribe_Values.Look(ref qualityScore, "qualityScore", 40);
            Scribe_Values.Look(ref temperatureCelsius, "temperatureCelsius", 21f);
            Scribe_Values.Look(ref contamination, "contamination", 0);
            Scribe_Values.Look(ref microwaveReheatCount, "microwaveReheatCount", 0);
            Scribe_Values.Look(ref lastThermalTick, "lastThermalTick", 0);
            Scribe_Collections.Look(ref hiddenSourceDefNames, "hiddenSourceDefNames", LookMode.Value);
            hiddenSourceDefNames ??= new List<string>();
            Scribe_Values.Look(ref hiddenDietaryFlags, "hiddenDietaryFlags", 0);
        }

        public CulinaryServingRecord ToRecord() => new(
            qualityScore,
            temperatureCelsius,
            (ContaminationSources)contamination,
            microwaveReheatCount,
            lastThermalTick,
            hiddenSourceDefNames,
            (DietaryFlags)hiddenDietaryFlags);

        public CulinaryServingData Copy() => From(ToRecord());

        public static CulinaryServingData From(CulinaryServingRecord record)
        {
            var snapshot = record.Capture();
            return new CulinaryServingData
            {
                schemaVersion = snapshot.SchemaVersion,
                qualityScore = snapshot.QualityScore,
                temperatureCelsius = snapshot.TemperatureCelsius,
                contamination = (int)snapshot.Contamination,
                microwaveReheatCount = snapshot.MicrowaveReheatCount,
                lastThermalTick = snapshot.LastThermalTick,
                hiddenSourceDefNames = snapshot.HiddenSourceDefNames.ToList(),
                hiddenDietaryFlags = (int)snapshot.HiddenDietaryFlags
            };
        }
    }
}
