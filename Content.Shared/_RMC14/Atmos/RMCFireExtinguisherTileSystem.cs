using Content.Shared.Atmos.Components;
using Robust.Shared.Physics.Events;

namespace Content.Shared._RMC14.Atmos;

public sealed class RMCFireExtinguisherTileSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCFireExtinguisherTileComponent, ResistFireAlertEvent>(OnAttemptExtinguish); // this doesnt work
        SubscribeLocalEvent<RMCFireExtinguisherTileComponent, StartCollideEvent>(OnEntityEnter);
        SubscribeLocalEvent<RMCFireExtinguisherTileComponent, EndCollideEvent>(OnEntityExit);

    }

    private void OnAttemptExtinguish(Entity<RMCFireExtinguisherTileComponent> comp, ref ResistFireAlertEvent args)
    {
        Logger.Debug("RMCFireExtinguisherTileSystem: OnAttemptExtinguish");
        Logger.Debug(args.User.ToString());
        foreach (EntityUid ent in comp.Comp.CollidingEntities)
        {
            if (args.User == ent)
            {
                if (!TryComp<FlammableComponent>(args.User, out var flammableComp))
                    return;
                flammableComp.FireStacks = 0;
            }
        }
    }

    private void OnEntityEnter(Entity<RMCFireExtinguisherTileComponent> comp, ref StartCollideEvent args)
    {
        Logger.Debug("ENTITY ENTERED");
        Logger.Debug(args.OtherEntity.ToString());
        comp.Comp.CollidingEntities.Add(args.OtherEntity); // this needs a check if it alredy exists
    }

    private void OnEntityExit(Entity<RMCFireExtinguisherTileComponent> comp, ref EndCollideEvent args)
    {
        Logger.Debug("ENTITY LEFT");
        Logger.Debug(args.OtherEntity.ToString());
        comp.Comp.CollidingEntities.Remove(args.OtherEntity);
    }

}
