using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DragDrop;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Marines.Medical;

public abstract class SharedIVDripSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containers = default!;
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
        SubscribeLocalEvent<IVDripComponent, DragDropDraggedEvent>(OnIVDripDragDropDragged);

        // TODO CM14 check for BloodstreamComponent instead of MarineComponent
        SubscribeLocalEvent<MarineComponent, CanDropTargetEvent>(OnMarineCanDropTarget);

        SubscribeLocalEvent<BloodPackComponent, MapInitEvent>(OnBloodPackMapInitEvent);
        SubscribeLocalEvent<BloodPackComponent, SolutionChangedEvent>(OnBloodPackSolutionChanged);
    }

    private void OnIVDripEntInserted(Entity<IVDripComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        UpdateIVVisuals(ent);
    }

    private void OnIVDripEntRemoved(Entity<IVDripComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        UpdateIVVisuals(ent);
    }

    private void OnIVDripAfterHandleState(Entity<IVDripComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateIVVisuals(ent);
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

    private void OnBloodPackMapInitEvent(Entity<BloodPackComponent> ent, ref MapInitEvent args)
    {
        if (!_solutionContainer.TryGetSolution(ent, ent.Comp.Solution, out var bagSolution))
            return;

        UpdateBagVisuals(ent, bagSolution);
    }

    private void OnBloodPackSolutionChanged(Entity<BloodPackComponent> ent, ref SolutionChangedEvent args)
    {
        UpdateBagVisuals(ent, args.Solution);
    }

    private void UpdateBagVisuals(Entity<BloodPackComponent> bag, Solution solution)
    {
        if (_containers.TryGetContainingContainer(bag, out var container) &&
            TryComp(container.Owner, out IVDripComponent? iv))
        {
            iv.FillColor = solution.GetColor(_prototype);
            iv.FillPercentage = (int) (solution.Volume / solution.MaxVolume * 100);
            Dirty(container.Owner, iv);
            UpdateIVAppearance((container.Owner, iv));
        }

        UpdatePackAppearance(bag);
    }

    private void UpdateIVVisuals(Entity<IVDripComponent> iv)
    {
        if (_containers.TryGetContainer(iv, iv.Comp.Slot, out var container))
        {
            foreach (var entity in container.ContainedEntities)
            {
                if (TryComp(entity, out BloodPackComponent? pack) &&
                    _solutionContainer.TryGetSolution(entity, pack.Solution, out var solution))
                {
                    iv.Comp.FillColor = solution.GetColor(_prototype);
                    iv.Comp.FillPercentage = (int) (solution.Volume / solution.MaxVolume * 100);
                    Dirty(iv);
                    UpdateIVAppearance(iv);
                    return;
                }
            }

            iv.Comp.FillColor = Color.White;
            iv.Comp.FillPercentage = 0;
            UpdateIVAppearance(iv);
        }
    }

    protected virtual void UpdateIVAppearance(Entity<IVDripComponent> iv)
    {
    }

    protected virtual void UpdatePackAppearance(Entity<BloodPackComponent> pack)
    {
    }
}
