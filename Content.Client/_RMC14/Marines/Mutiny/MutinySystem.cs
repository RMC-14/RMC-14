using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Mutiny;

namespace Content.Client._RMC14.Marines.Mutiny;

public sealed class MutinySystem : SharedMutinySystem
{
    protected override void MutineerAdded(Entity<MutineerComponent> ent, ref ComponentAdd args)
    {
        if (!TryComp<MarineComponent>(ent, out var marine))
            return;

        Dirty(ent);
    }

    protected override void MutineerRemoved(Entity<MutineerComponent> ent, ref ComponentRemove args)
    {
        if (!TryComp<MarineComponent>(ent, out var marine))
            return;

        Dirty(ent);
    }
}
