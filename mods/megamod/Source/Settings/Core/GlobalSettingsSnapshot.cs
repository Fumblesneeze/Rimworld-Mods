using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Megamod.Settings;

public sealed class GlobalSettingsSnapshot
{
    public GlobalSettingsSnapshot(IDictionary<string, string> values)
    {
        Values = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(values));
    }

    public IReadOnlyDictionary<string, string> Values { get; }
}
