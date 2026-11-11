using System;
using System.Collections.Generic;

namespace Megamod.Features.MessagePlacement;

/// <summary>One GUI pass of observed native message surfaces, never a message collection/cache.</summary>
public sealed record MessageSurface(MessageBounds Bounds, Action<MessageBounds> Apply, Action Restore,
    Func<bool>? IsCurrent = null);

public static class MessagePlacementBatch
{
    public static bool TryApply(IReadOnlyList<MessageSurface> newestFirst, double width, double height,
        double? visibleBarBottom, MessageOffsets offsets, Func<bool>? canCommit = null)
    {
        var bounds = new MessageBounds[newestFirst.Count];
        for (int index = 0; index < bounds.Length; index++) bounds[index] = newestFirst[index].Bounds;
        if (!MessagePlacement.TryCalculate(bounds, width, height, visibleBarBottom, offsets, out var translation))
            return false;
        int attempted = -1;
        try
        {
            foreach (var surface in newestFirst)
                if (surface.IsCurrent != null && !surface.IsCurrent()) return false;
            if (canCommit != null && !canCommit()) return false;
            for (int index = 0; index < bounds.Length; index++)
            {
                attempted = index;
                newestFirst[index].Apply(bounds[index] with { X = bounds[index].X + translation.X, Y = bounds[index].Y + translation.Y });
            }
        }
        catch (Exception failure)
        {
            // Restore exact pre-pass native state, which can differ from unshifted measurement on input events.
            List<Exception>? cleanupFailures = null;
            for (int index = attempted; index >= 0; index--)
            {
                try { newestFirst[index].Restore(); }
                catch (Exception cleanup)
                {
                    cleanupFailures ??= new() { failure };
                    cleanupFailures.Add(cleanup);
                }
            }
            if (cleanupFailures != null) throw new AggregateException("Message surface restoration was incomplete.", cleanupFailures);
            return false;
        }
        return true;
    }
}
