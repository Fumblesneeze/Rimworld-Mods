using System;
using System.Linq;

namespace Megamod.Features.DrugPolicyMaintenance.Runtime;

// Root owns the finalized main-thread boundary, publication and invalidation receipt.
public static class NativeDrugPresetBaseline
{
    public static PresetEntriesRead Capture(Func<bool> isCurrent) =>
        new(NativeDrugCatalog.Capture(_ => true).Select(entry => entry.Identity), isCurrent);
}
