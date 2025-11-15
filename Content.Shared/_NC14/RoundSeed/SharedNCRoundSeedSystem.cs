using System;

namespace Content.Shared._NC14.RoundSeed;

public abstract class SharedNCRoundSeedSystem : EntitySystem
{
    public bool TryGetSeed(out int seed)
    {
        var query = EntityQueryEnumerator<NCRoundSeedComponent>();
        while (query.MoveNext(out var roundSeed))
        {
            seed = roundSeed.Seed;
            return true;
        }

        seed = default;
        return false;
    }

    public int? GetSeed()
    {
        return TryGetSeed(out var seed) ? seed : null;
    }

    public global::System.Random? CreateRandom()
    {
        return TryGetSeed(out var seed) ? new global::System.Random(seed) : null;
    }
}
