using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Random;

public sealed partial class RMCPseudoRandomSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public Xoroshiro64S GetXoroshiro64S(EntityUid ent)
    {
        long tick = _timing.CurTick.Value;
        tick <<= 32;
        tick |= (uint) GetNetEntity(ent).Id;
        return new Xoroshiro64S(tick);
    }

    public float NextFloat(EntityUid ent)
    {
        return GetXoroshiro64S(ent).NextFloat();
    }

    public float NextFloat(ref Xoroshiro64S xoroshiro)
    {
        return xoroshiro.NextFloat();
    }

    public Angle NextAngle(EntityUid ent, Angle minValue, Angle maxValue)
    {
        return NextFloat(ent) * (maxValue - minValue) + minValue;
    }

    public Angle NextAngle(ref Xoroshiro64S xoroshiro, Angle minValue, Angle maxValue)
    {
        return NextFloat(ref xoroshiro) * (maxValue - minValue) + minValue;
    }
}
