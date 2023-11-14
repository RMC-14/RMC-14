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
        SubscribeLocalEvent<XenoComponent, XenoRegurgitateActionEvent>(OnXenoRegurgitate);
    }

    private void OnXenoCanDropTarget(Entity<XenoComponent> ent, ref CanDropTargetEvent args)
    {
        args.CanDrop |= args.User == ent.Owner &&
                        HasComp<MarineComponent>(args.Dragged);

        args.Handled = true;
    }

    private void OnMarineCanDropDragged(Entity<MarineComponent> ent, ref CanDropDraggedEvent args)
    {
        args.CanDrop |= args.Target == args.User &&
                        HasComp<XenoComponent>(args.User) &&
                        HasComp<MarineComponent>(args.Target) &&
                        !_mobState.IsDead(args.Target);

        if (args.CanDrop)
            args.Handled = true;
    }

    private void OnMarineDragDropDragged(Entity<MarineComponent> ent, ref DragDropDraggedEvent args)
    {
        if (args.Handled || args.Target != args.User)
            return;

        if (!TryComp(args.User, out XenoComponent? xeno))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-cant-devour", ("target", args.Target)), ent, ent);
            return;
        }

        StartDevour((args.User, xeno), ent, xeno.DevourDelay);
        args.Handled = true;
    }

    private void OnXenoDevourDoAfter(Entity<XenoComponent> ent, ref XenoDevourDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target)
            return;

        // TODO CM14 breaking out
        var container = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.DevourContainerId);
        if (!_container.Insert(target, container))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-cant-devour", ("target", target)), ent, ent);
        }
    }

    private void OnXenoRegurgitate(Entity<XenoComponent> ent, ref XenoRegurgitateActionEvent args)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.DevourContainerId, out var container) ||
            container.ContainedEntities.Count == 0)
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-none-devoured"), ent, ent);
            return;
        }

        _container.EmptyContainer(container);
        _audio.PlayPredicted(ent.Comp.RegurgitateSound, ent, ent);
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
