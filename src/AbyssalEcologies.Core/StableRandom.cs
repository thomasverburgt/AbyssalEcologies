using System;

namespace AbyssalEcologies.Core;

/// <summary>A small PCG generator whose output remains stable across .NET and Unity versions.</summary>
internal sealed class StableRandom
{
    private ulong _state;
    private readonly ulong _increment;

    public StableRandom(int seed)
    {
        _state = 0UL;
        _increment = ((ulong)(uint)seed << 1) | 1UL;
        NextUInt();
        _state += 0x9E3779B97F4A7C15UL ^ (uint)seed;
        NextUInt();
    }

    public uint NextUInt()
    {
        var oldState = _state;
        _state = (oldState * 6364136223846793005UL) + _increment;
        var xorshifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
        var rotation = (int)(oldState >> 59);
        return (xorshifted >> rotation) | (xorshifted << ((-rotation) & 31));
    }

    public float NextFloat() => (NextUInt() >> 8) * (1f / 16777216f);
    public float Range(float minimum, float maximum) => minimum + ((maximum - minimum) * NextFloat());
    public int Range(int minimum, int maximumExclusive) => minimum + (int)(NextUInt() % (uint)(maximumExclusive - minimum));
}

