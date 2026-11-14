using System;

namespace Megamod.Features.DrugPolicyMaintenance.Core;

public enum ScheduledDrugReadResult { Allowed, Rejected, Unavailable }

public static class ScheduledDrugReadBoundary
{
    public static ScheduledDrugReadResult Evaluate(Func<ScheduledDrugReadResult> readAndEvaluate,
        Action<Exception> reportReadFailure)
    {
        ArgumentNullException.ThrowIfNull(readAndEvaluate);
        ArgumentNullException.ThrowIfNull(reportReadFailure);
        try { return readAndEvaluate(); }
        catch (Exception error)
        {
            try { reportReadFailure(error); }
            catch (Exception) { /* A notice failure cannot disable the other scheduled restriction. */ }
            return ScheduledDrugReadResult.Unavailable;
        }
    }
}
