using System;
using System.Collections.Generic;
using System.Linq;

namespace GuestBedGizmo.Compatibility.Hospitality;

internal static class HospitalityPackagePolicy
{
    internal const string PackageId = "orion.hospitality";
    internal const string IdeologyPackageId = "ludeon.rimworld.ideology";

    internal static bool ShouldActivate(IEnumerable<string> packageIds) =>
        packageIds != null &&
        packageIds.Contains(PackageId, StringComparer.OrdinalIgnoreCase) &&
        packageIds.Contains(IdeologyPackageId, StringComparer.OrdinalIgnoreCase);
}
