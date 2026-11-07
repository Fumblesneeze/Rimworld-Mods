using System;
using System.Collections.Generic;

namespace PathfindingLab.Core;

public sealed class WanderHeading { public int Direction { get; set; } }

// Directional steering, not a destination search. The caller supplies real edge legality.
public static class DirectionalWalk
{
    private static readonly int[] dx = { 1, 1, 0, -1, -1, -1, 0, 1 };
    private static readonly int[] dz = { 0, 1, 1, 1, 0, -1, -1, -1 };
    public static List<int> Build(int width, int height, int start, int steps, WanderHeading heading,
        Func<int, int, bool> canStep, Func<int, int> random)
    {
        if (width <= 0 || height <= 0 || start < 0 || start >= (long)width * height || steps < 3 || steps > 10)
            throw new ArgumentOutOfRangeException(nameof(steps));
        var cells = new List<int>(steps + 1) { start };
        for (int step = 0; step < steps; step++)
        {
            int from = cells[^1], x = from % width, z = from / width;
            // Usually straight; occasional +/-45-degree drift. On collision test every
            // alternate heading once, wide turns before reversing, without revisiting cells.
            int jitter = random(4);
            int preferred = (heading.Direction + (jitter == 1 ? 1 : jitter == 2 ? -1 : 0) + 8) % 8;
            int sign = random(2) == 0 ? 1 : -1;
            bool moved = false;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                int offset = attempt == 0 ? 0 : (attempt + 1) / 2 * (attempt % 2 == 1 ? sign : -sign);
                int direction = (preferred + offset + 8) % 8;
                int nx = x + dx[direction], nz = z + dz[direction];
                if ((uint)nx >= width || (uint)nz >= height) continue;
                int to = nz * width + nx;
                if (cells.Contains(to) || !canStep(from, to)) continue;
                cells.Add(to);
                heading.Direction = direction;
                moved = true;
                break;
            }
            if (!moved) { heading.Direction = (preferred + 4) % 8; break; }
        }
        return cells;
    }

    public static bool WithinCircle(int width, int cell, int root, float radius)
    {
        long x = cell % width - root % width, z = cell / width - root / width;
        return x * x + z * z <= (double)radius * radius;
    }
}
