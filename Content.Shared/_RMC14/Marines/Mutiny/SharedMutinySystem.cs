using Content.Shared._RMC14.Marines.Squads;

namespace Content.Shared._RMC14.Marines.Mutiny;

public abstract class SharedMutinySystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<MutineerComponent, GetMarineIconEvent>(OnGetMarineIcon, after: [typeof(SquadSystem)]);

        SubscribeLocalEvent<MutineerComponent, ComponentAdd>(MutineerAdded);
        SubscribeLocalEvent<MutineerComponent, ComponentRemove>(MutineerRemoved);
    }

    private void OnGetMarineIcon(Entity<MutineerComponent> mutineer, ref GetMarineIconEvent args)
    {
        args.Icon = mutineer.Comp.Icon;
    }

    protected abstract void MutineerAdded(Entity<MutineerComponent> ent, ref ComponentAdd args);

    protected abstract void MutineerRemoved(Entity<MutineerComponent> ent, ref ComponentRemove args);
}
