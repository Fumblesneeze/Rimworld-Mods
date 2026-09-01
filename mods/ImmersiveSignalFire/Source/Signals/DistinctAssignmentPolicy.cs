using System;
using System.Collections.Generic;
using System.Linq;

namespace ImmersiveSignalFire.Signals;

public static class DistinctAssignmentPolicy
{
    public static bool TryAssign<TActor, TCell>(
        IReadOnlyList<TActor> actors,
        Func<TActor, IReadOnlyList<TCell>> candidatesFor,
        out IReadOnlyList<TCell> assigned)
        where TActor : notnull
        where TCell : notnull
    {
        if (actors is null)
        {
            throw new ArgumentNullException(nameof(actors));
        }

        if (candidatesFor is null)
        {
            throw new ArgumentNullException(nameof(candidatesFor));
        }

        List<TCell>[] candidates = actors
            .Select(actor => candidatesFor(actor).Distinct().ToList())
            .ToArray();
        if (candidates.Any(candidateSet => candidateSet.Count == 0))
        {
            assigned = Array.Empty<TCell>();
            return false;
        }

        int[] searchOrder = Enumerable.Range(0, actors.Count)
            .OrderBy(index => candidates[index].Count)
            .ThenBy(index => index)
            .ToArray();
        var result = new TCell[actors.Count];
        var used = new HashSet<TCell>();
        if (!AssignNext(0))
        {
            assigned = Array.Empty<TCell>();
            return false;
        }

        assigned = result;
        return true;

        bool AssignNext(int searchIndex)
        {
            if (searchIndex == searchOrder.Length)
            {
                return true;
            }

            int actorIndex = searchOrder[searchIndex];
            foreach (TCell candidate in candidates[actorIndex])
            {
                if (!used.Add(candidate))
                {
                    continue;
                }

                result[actorIndex] = candidate;
                if (AssignNext(searchIndex + 1))
                {
                    return true;
                }

                used.Remove(candidate);
            }

            return false;
        }
    }
}
