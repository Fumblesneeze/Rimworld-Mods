using System;
using System.Collections.Generic;
using System.Linq;

namespace Megamod.Features.DrugPolicyMaintenance.Core;

public static class DrugPolicyProviderPolicy
{
    public static string? UnavailableReason(IEnumerable<string> activePackageIds) =>
        activePackageIds.Any(id => string.Equals(id, "Mlie.DrugPolicyFix", StringComparison.OrdinalIgnoreCase))
            ? "Original provider Mlie.DrugPolicyFix is active; owned drug-policy maintenance is unavailable; native/provider behavior remains."
            : null;
}
