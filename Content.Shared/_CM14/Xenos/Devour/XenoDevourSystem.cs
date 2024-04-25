using System.Diagnostics.CodeAnalysis;
using Content.Shared._CM14.Marines;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Shared._CM14.Xenos.Devour;

public sealed class XenoDevourSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoDevourComponent, CanDropTargetEvent>(OnXenoCanDropTarget);
        SubscribeLocalEvent<MarineComponent, CanDropDraggedEvent>(OnMarineCanDropDragged);
        SubscribeLocalEvent<MarineComponent, DragDropDraggedEvent>(OnMarineDragDropDragged);
        SubscribeLocalEvent<XenoDevourComponent, DoAfterAttemptEvent<XenoDevourDoAfterEvent>>(OnXenoDevourDoAfterAttempt);
        SubscribeLocalEvent<XenoDevourComponent, XenoDevourDoAfterEvent>(OnXenoDevourDoAfter);
        SubscribeLocalEvent<XenoDevourComponent, XenoRegurgitateActionEvent>(OnXenoRegurgitateAction);
    }

    private void OnXenoCanDropTarget(Entity<XenoDevourComponent> xeno, ref CanDropTargetEvent args)
    {
        if (CanDevour(xeno, args.Dragged, out _))
            args.CanDrop = true;

        args.Handled = true;
    }

    private void OnMarineCanDropDragged(Entity<MarineComponent> marine, ref CanDropDraggedEvent args)
    {
        if (CanDevour(args.User, marine, out _))
        {
            args.CanDrop = true;
            args.Handled = true;
        }
    }

    private void OnMarineDragDropDragged(Entity<MarineComponent> marine, ref DragDropDraggedEvent args)
    {
        if (args.Target != args.User)
            return;

        if (!CanDevour(args.User, marine, out var devour))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-cant-devour", ("target", args.Target)), marine, marine);
            return;
        }

        StartDevour((args.User, devour), marine, devour.DevourDelay);
        args.Handled = true;
    }

    private void OnXenoDevourDoAfterAttempt(Entity<XenoDevourComponent> ent, ref DoAfterAttemptEvent<XenoDevourDoAfterEvent> args)
    {
        if (args.DoAfter.Args.Target is not { } target ||
            !CanDevour(ent, target, out _))
        {
            args.Cancel();
        }
    }

    private void OnXenoDevourDoAfter(Entity<XenoDevourComponent> xeno, ref XenoDevourDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target)
            return;

        if (!CanDevour(xeno, target, out _))
            return;

        args.Handled = true;

        // TODO CM14 breaking out
        var container = _container.EnsureContainer<ContainerSlot>(xeno, xeno.Comp.DevourContainerId);
        if (!_container.Insert(target, container))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-cant-devour", ("target", target)), xeno, xeno);
        }
    }

    private void OnXenoRegurgitateAction(Entity<XenoDevourComponent> xeno, ref XenoRegurgitateActionEvent args)
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

    private bool CanDevour(EntityUid xeno, EntityUid victim, [NotNullWhen(true)] out XenoDevourComponent? devour)
    {
        devour = default;
        if (xeno == victim)
            return false;

        if (HasComp<XenoComponent>(victim))
            return false;

        if (_mobState.IsDead(victim) || !_standing.IsDown(victim))
            return false;

        if (_mobState.IsIncapacitated(xeno))
            return false;

        if (!HasComp<DevourableComponent>(victim))
            return false;

        return TryComp(xeno, out devour);
    }

    private void StartDevour(Entity<XenoDevourComponent> xeno, Entity<MarineComponent> target, TimeSpan delay)
    {
        var doAfter = new DoAfterArgs(EntityManager, xeno, delay, new XenoDevourDoAfterEvent(), xeno, target)
        {
            BreakOnMove = true,
            AttemptFrequency = AttemptFrequency.EveryTick
        };

        _doAfter.TryStartDoAfter(doAfter);
    }
}
