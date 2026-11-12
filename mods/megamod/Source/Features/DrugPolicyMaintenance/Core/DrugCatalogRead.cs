using System;
using System.Collections.Generic;
using System.Linq;

namespace Megamod.Features.DrugPolicyMaintenance.Core;

// Caller-owned ordered publication. Its identity is this object; its lifetime is supplied by
// the publishing boundary. Data alone does not establish a Game or an applied exclusion policy.
public sealed class DrugCatalogRead
{
    private readonly Func<bool> isCurrent;
    public DrugCatalogRead(IEnumerable<DrugCatalogEntry> entries, Func<bool> isCurrent)
    {
        this.isCurrent = isCurrent ?? throw new ArgumentNullException(nameof(isCurrent));
        Entries = Array.AsReadOnly((entries ?? throw new ArgumentNullException(nameof(entries))).ToArray());
    }

    public IReadOnlyList<DrugCatalogEntry> Entries { get; }
    public bool IsCurrent => isCurrent();
    public PresetEntriesRead ForPreset(Func<bool> isPresetCurrent)
    {
        if (isPresetCurrent == null) throw new ArgumentNullException(nameof(isPresetCurrent));
        return new PresetEntriesRead(Entries.Select(entry => entry.Identity), () => IsCurrent && isPresetCurrent());
    }
}
