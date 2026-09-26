using System;
using System.Collections.Generic;

namespace AbyssalEcologies.Core;

public enum ExpeditionBiome
{
    GlassKelpGarden,
    EmberTrench,
    GhostlightNursery
}

public readonly struct ExpeditionSectorCoordinate : IEquatable<ExpeditionSectorCoordinate>
{
    public ExpeditionSectorCoordinate(int x, int z) { X = x; Z = z; }
    public int X { get; }
    public int Z { get; }
    public bool Equals(ExpeditionSectorCoordinate other) => X == other.X && Z == other.Z;
    public override bool Equals(object? obj) => obj is ExpeditionSectorCoordinate other && Equals(other);
    public override int GetHashCode() => unchecked((X * 397) ^ Z);
    public override string ToString() => $"sector({X},{Z})";
}

public readonly struct ExpeditionChunkCoordinate : IEquatable<ExpeditionChunkCoordinate>
{
    public ExpeditionChunkCoordinate(int x, int z) { X = x; Z = z; }
    public int X { get; }
    public int Z { get; }
    public bool Equals(ExpeditionChunkCoordinate other) => X == other.X && Z == other.Z;
    public override bool Equals(object? obj) => obj is ExpeditionChunkCoordinate other && Equals(other);
    public override int GetHashCode() => unchecked((X * 397) ^ Z);
    public override string ToString() => $"chunk({X},{Z})";
}

public sealed class ExpeditionGenerationSettings
{
    public const int DefaultChunksPerSectorAxis = 8;
    public int ChunksPerSectorAxis { get; set; } = DefaultChunksPerSectorAxis;
    public float ChunkSize { get; set; } = 256f;
    public float BaseDepth { get; set; } = -280f;
    public float Relief { get; set; } = 70f;

    public void Validate()
    {
        if (ChunksPerSectorAxis < 2 || ChunksPerSectorAxis > 32) throw new InvalidOperationException("Chunks per sector axis must be between 2 and 32.");
        if (ChunkSize < 64f || ChunkSize > 1024f) throw new InvalidOperationException("Chunk size must be between 64 and 1024 metres.");
        if (Relief < 0f || Relief > 300f) throw new InvalidOperationException("Relief must be between 0 and 300 metres.");
    }
}

public sealed class ExpeditionChunkDescriptor
{
    public ExpeditionChunkDescriptor(ExpeditionChunkCoordinate coordinate, ExpeditionSectorCoordinate sector, ExpeditionBiome biome, int terrainSeed, int contentSeed, float northWest, float northEast, float southEast, float southWest)
    {
        Coordinate = coordinate; Sector = sector; Biome = biome; TerrainSeed = terrainSeed; ContentSeed = contentSeed;
        NorthWestHeight = northWest; NorthEastHeight = northEast; SouthEastHeight = southEast; SouthWestHeight = southWest;
    }

    public ExpeditionChunkCoordinate Coordinate { get; }
    public ExpeditionSectorCoordinate Sector { get; }
    public ExpeditionBiome Biome { get; }
    public int TerrainSeed { get; }
    public int ContentSeed { get; }
    public float NorthWestHeight { get; }
    public float NorthEastHeight { get; }
    public float SouthEastHeight { get; }
    public float SouthWestHeight { get; }
}

public sealed class ExpeditionSectorDescriptor
{
    public ExpeditionSectorDescriptor(int worldSeed, ExpeditionSectorCoordinate coordinate, IReadOnlyList<ExpeditionChunkDescriptor> chunks)
    {
        WorldSeed = worldSeed; Coordinate = coordinate; Chunks = chunks;
    }
    public int WorldSeed { get; }
    public ExpeditionSectorCoordinate Coordinate { get; }
    public IReadOnlyList<ExpeditionChunkDescriptor> Chunks { get; }
}

public sealed class ExpeditionGenerator
{
    public ExpeditionSectorDescriptor GenerateSector(int worldSeed, ExpeditionSectorCoordinate sector, ExpeditionGenerationSettings? settings = null)
    {
        settings ??= new ExpeditionGenerationSettings();
        settings.Validate();
        var chunks = new List<ExpeditionChunkDescriptor>(settings.ChunksPerSectorAxis * settings.ChunksPerSectorAxis);
        for (var localZ = 0; localZ < settings.ChunksPerSectorAxis; localZ++)
        for (var localX = 0; localX < settings.ChunksPerSectorAxis; localX++)
        {
            var coordinate = new ExpeditionChunkCoordinate(
                checked(sector.X * settings.ChunksPerSectorAxis + localX),
                checked(sector.Z * settings.ChunksPerSectorAxis + localZ));
            chunks.Add(GenerateChunk(worldSeed, coordinate, settings));
        }
        return new ExpeditionSectorDescriptor(worldSeed, sector, chunks);
    }

    public IReadOnlyList<ExpeditionChunkDescriptor> GenerateStreamingWindow(int worldSeed, ExpeditionChunkCoordinate center, int radius, ExpeditionGenerationSettings? settings = null)
    {
        if (radius < 0 || radius > 4) throw new ArgumentOutOfRangeException(nameof(radius), "Streaming radius must be between 0 and 4 chunks.");
        settings ??= new ExpeditionGenerationSettings();
        settings.Validate();
        var chunks = new List<ExpeditionChunkDescriptor>((radius * 2 + 1) * (radius * 2 + 1));
        for (var z = center.Z - radius; z <= center.Z + radius; z++)
        for (var x = center.X - radius; x <= center.X + radius; x++)
            chunks.Add(GenerateChunk(worldSeed, new ExpeditionChunkCoordinate(x, z), settings));
        return chunks;
    }

    public ExpeditionChunkDescriptor GenerateChunk(int worldSeed, ExpeditionChunkCoordinate coordinate, ExpeditionGenerationSettings? settings = null)
    {
        settings ??= new ExpeditionGenerationSettings();
        settings.Validate();
        var terrainSeed = Mix(worldSeed, coordinate.X, coordinate.Z, 0x54A13);
        var contentSeed = Mix(worldSeed, coordinate.X, coordinate.Z, 0xC017E);
        var biomeRoll = (uint)Mix(worldSeed, coordinate.X, coordinate.Z, 0xB10) % 3u;
        var sector = new ExpeditionSectorCoordinate(FloorDivide(coordinate.X, settings.ChunksPerSectorAxis), FloorDivide(coordinate.Z, settings.ChunksPerSectorAxis));
        return new ExpeditionChunkDescriptor(
            coordinate,
            sector,
            (ExpeditionBiome)biomeRoll,
            terrainSeed,
            contentSeed,
            SampleVertexHeight(worldSeed, coordinate.X, coordinate.Z, settings),
            SampleVertexHeight(worldSeed, coordinate.X + 1, coordinate.Z, settings),
            SampleVertexHeight(worldSeed, coordinate.X + 1, coordinate.Z + 1, settings),
            SampleVertexHeight(worldSeed, coordinate.X, coordinate.Z + 1, settings));
    }

    public float SampleVertexHeight(int worldSeed, int vertexX, int vertexZ, ExpeditionGenerationSettings? settings = null)
    {
        settings ??= new ExpeditionGenerationSettings();
        settings.Validate();
        var value = (uint)Mix(worldSeed, vertexX, vertexZ, 0x71E);
        var unit = (value >> 8) * (1f / 16777216f);
        return settings.BaseDepth + (unit - 0.5f) * settings.Relief;
    }

    private static int FloorDivide(int value, int divisor)
    {
        var quotient = value / divisor;
        var remainder = value % divisor;
        return remainder < 0 ? quotient - 1 : quotient;
    }

    private static int Mix(int worldSeed, int x, int z, int salt)
    {
        unchecked
        {
            uint hash = 2166136261u;
            hash = (hash ^ (uint)worldSeed) * 16777619u;
            hash = (hash ^ (uint)x) * 16777619u;
            hash = (hash ^ (uint)z) * 16777619u;
            hash = (hash ^ (uint)salt) * 16777619u;
            hash ^= hash >> 16;
            hash *= 0x7FEB352Du;
            hash ^= hash >> 15;
            hash *= 0x846CA68Bu;
            hash ^= hash >> 16;
            return (int)hash;
        }
    }
}
