using Content.Shared.DragDrop;

namespace Content.Shared._RMC14.Synth;

/// <summary>
/// Shared drag-drop checks for synthetic maintenance stations.
/// </summary>
public sealed class SharedRMCSyntheticMaintenanceStationSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<RMCSyntheticMaintenanceStationComponent, CanDropTargetEvent>(OnCanDropTarget);
    }

    private void OnCanDropTarget(Entity<RMCSyntheticMaintenanceStationComponent> ent, ref CanDropTargetEvent args)
    {
        if (ent.Comp.Occupied || !HasComp<SynthComponent>(args.Dragged))
            return;

        // The server still validates the insert; this only enables the client-side drop affordance for synths.
        args.Handled = true;
        args.CanDrop = true;
    }
}
