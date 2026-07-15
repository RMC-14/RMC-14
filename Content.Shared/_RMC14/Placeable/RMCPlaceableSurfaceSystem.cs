using Content.Shared.Interaction;
using Content.Shared.Placeable;
using Content.Shared.Tools.Components;

namespace Content.Shared._RMC14.Placeable;

public sealed class RMCPlaceableSurfaceSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ToolComponent, AfterInteractEvent>(OnToolAfterInteract);
    }

    private void OnToolAfterInteract(Entity<ToolComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target != null && HasComp<PlaceableSurfaceComponent>(args.Target))
            args.Handled = true;
    }
}
