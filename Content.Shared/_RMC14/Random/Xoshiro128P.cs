using System.Runtime.CompilerServices;

namespace Content.Shared._RMC14.Random;

// https://github.com/medo64/Medo.ScrambledLinear/blob/main/src/Xoshiro/Xoshiro128P.cs
/// <summary>
/// 32-bit generator intended for floating point numbers with 128-bit state (xoshiro128+).
/// </summary>
/// <remarks>http://prng.di.unimi.it/xoshiro128plus.c</remarks>
public record struct Xoshiro128P
{
    private UInt32 _s0;
    private UInt32 _s1;
    private UInt32 _s2;
    private UInt32 _s3;


    /// <summary>
    /// Creates a new instance.
    /// </summary>
    public Xoshiro128P() : this(DateTime.UtcNow.Ticks) { }

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="seed">Seed value.</param>
    public Xoshiro128P(long seed)
    {
        var sm64 = new SplitMix64(seed);

        _s0 = unchecked((UInt32)sm64.Next());
        _s1 = unchecked((UInt32)sm64.Next());
        _s2 = unchecked((UInt32)sm64.Next());
        _s3 = unchecked((UInt32)sm64.Next());
    }

    public Xoshiro128P(long s0, long s1)
    {
        var sm64 = new SplitMix64(s0);
        _s0 = unchecked((UInt32)sm64.Next());
        _s1 = unchecked((UInt32)sm64.Next());

        sm64 = new SplitMix64(s1);
        _s2 = unchecked((UInt32)sm64.Next());
        _s3 = unchecked((UInt32)sm64.Next());
    }

    /// <summary>
    /// Returns the next 32-bit pseudo-random number.
    /// </summary>
    public int Next()
    {
        UInt32 result = unchecked(_s0 + _s3);

        UInt32 t = _s1 << 9;

        _s2 ^= _s0;
        _s3 ^= _s1;
        _s1 ^= _s2;
        _s0 ^= _s3;

        _s2 ^= t;

        _s3 = RotateLeft(_s3, 11);

        return Math.Abs((int)result);
    }

    public float NextFloat()
    {
        return Next() * 4.6566128752458E-10f;
    }

    public float NextFloat(float min, float max)
    {
        return NextFloat() * (max - min) + min;
    }

    private static UInt32 RotateLeft(UInt32 x, int k)
    {
        return (x << k) | (x >> (32 - k));
    }

}