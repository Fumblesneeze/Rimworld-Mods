using System;
using System.Threading;
using RimWorld;
using Verse;

namespace Megamod.Features.DrugTolerance.Runtime;

public static class NativeDrugTolerancePersistence
{
    private static int pendingFailure;

    public static DrugTolerancePersistencePatch Create(Action<string> report) => new(typeof(DrugPolicyEntry), typeof(ScribeSaver), typeof(ScribeLoader),
        entry => NativeDrugToleranceState.Read((DrugPolicyEntry)entry),
        (entry, choice) => NativeDrugToleranceState.Write((DrugPolicyEntry)entry, choice),
        entry => Expose((DrugPolicyEntry)entry), message =>
        {
            Interlocked.Exchange(ref pendingFailure, 1);
            report(message);
        });

    // Native serialization can run on a loader thread. The ordinary root lifecycle pump presents
    // the once-per-failure notice only after returning to a usable native UI.
    public static void ProcessPendingFailure()
    {
        if (!UnityData.IsInMainThread || LongEventHandler.AnyEventNowOrWaiting || Find.WindowStack == null) return;
        if (Interlocked.Exchange(ref pendingFailure, 0) == 0) return;
        Find.WindowStack.Add(new Dialog_MessageBox("Megamod.DrugTolerance.PersistenceFailureBody".Translate(),
            title: "Megamod.DrugTolerance.PersistenceFailureTitle".Translate()));
    }

    private static void Expose(DrugPolicyEntry entry)
    {
        string? encoding = NativeDrugToleranceState.ReadEncoded(entry);
        Scribe_Values.Look(ref encoding, "megamodTolerance", defaultValue: null);
        if (Scribe.mode == LoadSaveMode.LoadingVars)
            NativeDrugToleranceState.WriteEncoded(entry, encoding);
    }
}
