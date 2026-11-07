using System;

namespace PathfindingLab.Core;

// Owned immutable publication. Arrays never escape as writable views and are never
// returned to a pool: readers can outlive a background replacement safely.
public sealed class PortalGraph
{
    internal sealed class Chunk(int[] components, int[] portals, int[][] crossings)
    {
        internal int Component(int local) => components[local];
        internal ReadOnlyMemory<int> Portals => portals;
        internal ReadOnlyMemory<int> Crossings(int cell)
        {
            int index = Array.IndexOf(portals, cell);
            return index < 0 ? ReadOnlyMemory<int>.Empty : crossings[index];
        }
    }
    private readonly Chunk[] chunks;
    private readonly int[] versions;
    internal GeometrySnapshot Geometry { get; }
    internal object Identity { get; }
    public int Width => Geometry.Width;
    public int Height => Geometry.Height;
    public long Version => Geometry.Version;
    public int Generation { get; }
    public int ChangedChunks { get; }
    internal PortalGraph(GeometrySnapshot geometry, object identity, Chunk[] ownedChunks,
        int[] ownedVersions, int generation, int changedChunks)
    {
        Geometry = geometry; Identity = identity; chunks = ownedChunks;
        versions = ownedVersions; Generation = generation; ChangedChunks = changedChunks;
    }
    internal Chunk[] CopyChunkTable() => (Chunk[])chunks.Clone();
    internal int ChunkVersion(int chunk) => versions[chunk];
    internal int Component(int cell) => chunks[ChunkIndex(cell)].Component(cell % Width % 32 + cell / Width % 32 * 32);
    internal ReadOnlyMemory<int> Portals(int chunk) => chunks[chunk].Portals;
    internal ReadOnlyMemory<int> Crossings(int cell) => chunks[ChunkIndex(cell)].Crossings(cell);
    private int ChunkIndex(int cell) => cell % Width / 32 + cell / Width / 32 * ((Width + 31) / 32);
}
