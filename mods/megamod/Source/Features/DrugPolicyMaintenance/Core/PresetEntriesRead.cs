using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Megamod.Features.DrugPolicyMaintenance;

// Caller-owned publication; this type does not discover Defs or establish their lifetime.
public sealed class PresetEntriesRead
{
    private readonly HashSet<object> identities;
    private readonly Func<bool> isCurrent;
    public PresetEntriesRead(IEnumerable<object> identities, Func<bool> isCurrent)
    {
        this.identities = new HashSet<object>(identities ?? throw new ArgumentNullException(nameof(identities)), IdentityComparer.Instance);
        this.isCurrent = isCurrent ?? throw new ArgumentNullException(nameof(isCurrent));
    }
    public bool IsCurrent => isCurrent();
    public bool Contains(object? identity) => identity != null && identities.Contains(identity);
    private sealed class IdentityComparer : IEqualityComparer<object>
    {
        public static readonly IdentityComparer Instance = new();
        public new bool Equals(object? first, object? second) => ReferenceEquals(first, second);
        public int GetHashCode(object value) => RuntimeHelpers.GetHashCode(value);
    }
}
