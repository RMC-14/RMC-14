using Content.Shared._RMC14.Botany;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Kitchen.Components;
using Content.Shared.Random;
using Robust.Shared.Containers;

namespace Content.Server._RMC14.Botany;

public sealed class RMCLogSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly RandomHelperSystem _randomHelper = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCLogComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<RMCLogComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !HasComp<SharpComponent>(args.Used))
            return;

        var (uid, component) = ent;

        // if in some container, try pick up, else just drop to world
        var inContainer = _container.IsEntityInContainer(uid);
        var pos = Transform(uid).Coordinates;

        for (var i = 0; i < component.SpawnCount; i++)
        {
            var plank = Spawn(component.SpawnedPrototype, pos);

            if (inContainer)
                _hands.PickupOrDrop(args.User, plank);
            else
            {
                var xform = Transform(plank);
                _container.AttachParentToContainerOrGrid((plank, xform));
                xform.LocalRotation = 0;
                _randomHelper.RandomOffset(plank, 0.25f);
            }
        }

        QueueDel(uid);
        args.Handled = true;
    }
}
