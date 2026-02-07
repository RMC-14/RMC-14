using Content.Shared.Interaction;
using Content.Shared.Placeable;
using Content.Shared.Tools.Components;

namespace Content.Shared._RMC14.Placeable;

public sealed class RMCPlaceableSurfaceSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        // Tools should now stay in hand, decorations and anything not tools unaffected and able to be placed as per normal.
        SubscribeLocalEvent<ToolComponent, AfterInteractEvent>(OnToolAfterInteract);
    }

    private void OnToolAfterInteract(EntityUid uid, ToolComponent component, AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        // Tool stay in hand to complete action.
        if (args.Target != null && HasComp<PlaceableSurfaceComponent>(args.Target))
            args.Handled = true;
    }
}
