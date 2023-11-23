using Content.Shared._CM14.Marines;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Containers;

namespace Content.Shared._CM14.Xenos.Devour;

public sealed class XenoDevourSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoComponent, CanDropTargetEvent>(OnXenoCanDropTarget);
        SubscribeLocalEvent<MarineComponent, CanDropDraggedEvent>(OnMarineCanDropDragged);
        SubscribeLocalEvent<MarineComponent, DragDropDraggedEvent>(OnMarineDragDropDragged);
        SubscribeLocalEvent<XenoComponent, XenoDevourDoAfterEvent>(OnXenoDevourDoAfter);
        SubscribeLocalEvent<XenoComponent, XenoRegurgitateActionEvent>(OnXenoRegurgitateAction);
    }

    private void OnXenoCanDropTarget(Entity<XenoComponent> xeno, ref CanDropTargetEvent args)
    {
        args.CanDrop |= args.User == xeno.Owner &&
                        HasComp<MarineComponent>(args.Dragged);

        args.Handled = true;
    }

    private void OnMarineCanDropDragged(Entity<MarineComponent> marine, ref CanDropDraggedEvent args)
    {
        args.CanDrop |= args.Target == args.User &&
                        HasComp<XenoComponent>(args.User) &&
                        HasComp<MarineComponent>(args.Target) &&
                        !_mobState.IsDead(args.Target);

        if (args.CanDrop)
            args.Handled = true;
    }

    private void OnMarineDragDropDragged(Entity<MarineComponent> marine, ref DragDropDraggedEvent args)
    {
        if (args.Handled || args.Target != args.User)
            return;

        if (!TryComp(args.User, out XenoComponent? xeno))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-cant-devour", ("target", args.Target)), marine, marine);
            return;
        }

        StartDevour((args.User, xeno), marine, xeno.DevourDelay);
        args.Handled = true;
    }

    private void OnXenoDevourDoAfter(Entity<XenoComponent> xeno, ref XenoDevourDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target)
            return;

        // TODO CM14 breaking out
        var container = _container.EnsureContainer<ContainerSlot>(xeno, xeno.Comp.DevourContainerId);
        if (!_container.Insert(target, container))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-cant-devour", ("target", target)), xeno, xeno);
        }
    }

    private void OnXenoRegurgitateAction(Entity<XenoComponent> xeno, ref XenoRegurgitateActionEvent args)
    {
        if (!_container.TryGetContainer(xeno, xeno.Comp.DevourContainerId, out var container) ||
            container.ContainedEntities.Count == 0)
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-none-devoured"), xeno, xeno);
            return;
        }

        _container.EmptyContainer(container);
        _audio.PlayPredicted(xeno.Comp.RegurgitateSound, xeno, xeno);
    }

    private void StartDevour(Entity<XenoComponent> xeno, Entity<MarineComponent> target, TimeSpan delay)
    {
        var doAfter = new DoAfterArgs(EntityManager, xeno, delay, new XenoDevourDoAfterEvent(), xeno, target)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true
        };

        _doAfter.TryStartDoAfter(doAfter);
    }
}
