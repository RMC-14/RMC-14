using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DragDrop;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Marines.Medical;

public abstract class SharedIVDripSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<IVDripComponent, EntInsertedIntoContainerMessage>(OnIVDripEntInserted);
        SubscribeLocalEvent<IVDripComponent, EntRemovedFromContainerMessage>(OnIVDripEntRemoved);
        SubscribeLocalEvent<IVDripComponent, AfterAutoHandleStateEvent>(OnIVDripAfterHandleState);
        SubscribeLocalEvent<IVDripComponent, EntityUnpausedEvent>(OnIVDripUnPaused);
        SubscribeLocalEvent<IVDripComponent, CanDragEvent>(OnIVDripCanDrag);
        SubscribeLocalEvent<IVDripComponent, CanDropDraggedEvent>(OnIVDripCanDropDragged);
        // TODO CM14 check for BloodstreamComponent instead of MarineComponent
        SubscribeLocalEvent<MarineComponent, CanDropTargetEvent>(OnMarineCanDropTarget);
        SubscribeLocalEvent<IVDripComponent, DragDropDraggedEvent>(OnIVDripDragDropDragged);
    }

    private void OnIVDripEntInserted(Entity<IVDripComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        UpdateAppearance(ent);
    }

    private void OnIVDripEntRemoved(Entity<IVDripComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        UpdateAppearance(ent);
    }

    private void OnIVDripAfterHandleState(Entity<IVDripComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateAppearance(ent);
    }

    private void OnIVDripUnPaused(Entity<IVDripComponent> ent, ref EntityUnpausedEvent args)
    {
        ent.Comp.TransferAt += args.PausedTime;
    }

    private void OnIVDripCanDrag(Entity<IVDripComponent> ent, ref CanDragEvent args)
    {
        args.Handled = true;
    }

    private void OnIVDripCanDropDragged(Entity<IVDripComponent> ent, ref CanDropDraggedEvent args)
    {
        // TODO CM14 check for BloodstreamComponent instead of MarineComponent
        if (HasComp<MarineComponent>(args.Target))
        {
            args.Handled = true;
            args.CanDrop = true;
        }
    }

    // TODO CM14 check for BloodstreamComponent instead of MarineComponent
    private void OnMarineCanDropTarget(Entity<MarineComponent> ent, ref CanDropTargetEvent args)
    {
        if (HasComp<IVDripComponent>(args.Dragged))
        {
            args.Handled = true;
            args.CanDrop = true;
        }
    }

    private void OnIVDripDragDropDragged(Entity<IVDripComponent> ent, ref DragDropDraggedEvent args)
    {
        if (args.Handled)
            return;

        ent.Comp.AttachedTo = args.Target;
        Dirty(ent);
    }

    protected virtual void UpdateAppearance(Entity<IVDripComponent> iv)
    {
    }

    protected void UpdateFill(Entity<IVDripComponent> iv)
    {
        if (_itemSlots.GetItemOrNull(iv, iv.Comp.Slot) is not { } bag ||
            !_solutionContainer.TryGetSolution(bag, iv.Comp.Solution, out var solution))
        {
            iv.Comp.FillPercentage = 0;
            iv.Comp.FillColor = Color.White;
            Dirty(iv);
            return;
        }

        iv.Comp.FillPercentage = (int) (solution.Volume / solution.MaxVolume * 100);
        iv.Comp.FillColor = solution.GetColor(_prototype);
        Dirty(iv);
    }
}
