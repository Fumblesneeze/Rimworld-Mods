using System;
using System.Collections.Generic;
using System.Linq;

namespace ImmersiveSignalFire.Signals;

public static class ParticipantSelectionPolicy
{
    public static IReadOnlyList<T> SelectCaller<T>(
        IReadOnlyList<T> selected,
        T? currentCaller,
        T requestedCaller,
        int maximumParticipants)
        where T : class
    {
        if (selected is null)
        {
            throw new ArgumentNullException(nameof(selected));
        }

        if (requestedCaller is null)
        {
            throw new ArgumentNullException(nameof(requestedCaller));
        }

        if (maximumParticipants <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumParticipants));
        }

        var result = selected.Distinct().ToList();
        if (result.Contains(requestedCaller))
        {
            return result;
        }

        if (result.Count >= maximumParticipants && currentCaller is not null)
        {
            result.Remove(currentCaller);
        }

        if (result.Count < maximumParticipants)
        {
            result.Add(requestedCaller);
        }

        return result;
    }
}
