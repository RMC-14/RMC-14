using Content.Shared.Atmos.Components;

namespace Content.Server._RMC14.Mobs.Animals;

public abstract partial class RMCAnimalSystem
{
    protected TimeSpan RandomTime(TimeSpan min, TimeSpan max)
    {
        if (max <= min)
            return min;

        return min + TimeSpan.FromSeconds(Random.NextDouble() * (max - min).TotalSeconds);
    }

    protected bool ValidLivingMob(EntityUid uid)
    {
        return !TerminatingOrDeleted(uid) &&
               MobQuery.HasComp(uid) &&
               MobState.IsAlive(uid);
    }

    protected bool IsOnFire(EntityUid uid)
    {
        return FlammableQuery.TryComp(uid, out var flammable) && flammable.OnFire;
    }
}
