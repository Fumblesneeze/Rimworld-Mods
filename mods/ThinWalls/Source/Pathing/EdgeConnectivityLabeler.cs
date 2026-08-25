using System;

namespace ThinWalls.Pathing;

public static class EdgeConnectivityLabeler
{
    public static int[] Build(
        int width,
        int height,
        Func<int, bool> walkable,
        Func<int, int, bool> connected)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        if (walkable == null)
        {
            throw new ArgumentNullException(nameof(walkable));
        }

        if (connected == null)
        {
            throw new ArgumentNullException(nameof(connected));
        }

        int cellCount = checked(width * height);
        var labels = new int[cellCount];
        var queue = new int[cellCount];
        int nextLabel = 0;
        for (int seed = 0; seed < cellCount; seed++)
        {
            if (labels[seed] != 0 || !walkable(seed))
            {
                continue;
            }

            nextLabel++;
            labels[seed] = nextLabel;
            int read = 0;
            int write = 0;
            queue[write++] = seed;
            while (read < write)
            {
                int current = queue[read++];
                int currentX = current % width;
                int currentZ = current / width;
                for (int deltaZ = -1; deltaZ <= 1; deltaZ++)
                {
                    for (int deltaX = -1; deltaX <= 1; deltaX++)
                    {
                        if (deltaX == 0 && deltaZ == 0)
                        {
                            continue;
                        }

                        int adjacentX = currentX + deltaX;
                        int adjacentZ = currentZ + deltaZ;
                        if (adjacentX < 0 || adjacentX >= width ||
                            adjacentZ < 0 || adjacentZ >= height)
                        {
                            continue;
                        }

                        int adjacent = adjacentZ * width + adjacentX;
                        if (labels[adjacent] != 0 || !walkable(adjacent) ||
                            !connected(current, adjacent))
                        {
                            continue;
                        }

                        labels[adjacent] = nextLabel;
                        queue[write++] = adjacent;
                    }
                }
            }
        }

        return labels;
    }
}
