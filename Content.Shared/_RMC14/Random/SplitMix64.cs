namespace Content.Shared._RMC14.Random;

/// <summary>
/// Seed initializer PRNG (splitmix64).
/// </summary>
/// <remarks>http://prng.di.unimi.it/splitmix64.c</remarks>
public record struct SplitMix64
{
    /// <summary>
    /// Creates a new instance.
    /// </summary>
    public SplitMix64()
        : this(DateTime.UtcNow.Ticks)
    {
    }

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="seed">Seed value.</param>
    public SplitMix64(long seed)
    {
        x = (UInt64) seed;
    }


    private UInt64 x;

    /// <summary>
    /// Returns the next 64-bit pseudo-random number.
    /// </summary>
    public long Next()
    {
        UInt64 z = unchecked(x += 0x9e3779b97f4a7c15);
        z = unchecked((z ^ (z >> 30)) * 0xbf58476d1ce4e5b9);
        z = unchecked((z ^ (z >> 27)) * 0x94d049bb133111eb);
        return unchecked((Int64) (z ^ (z >> 31)));
    }
}
