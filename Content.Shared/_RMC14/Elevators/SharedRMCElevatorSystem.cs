using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared._RMC14.Elevators;

public abstract class SharedRMCElevatorSystem : EntitySystem
{
    [Dependency] protected readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly IGameTiming _timing = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCElevatorPanelComponent, AfterActivatableUIOpenEvent>(OnPanelOpen);

        SubscribeLocalEvent<RMCElevatorLinkingComponent, AfterInteractEvent>(OnTryLink);

        Subs.BuiEvents<RMCElevatorPanelComponent>(ElevatorPanelUiKey.Key,
            subs =>
            {
                subs.Event<ElevatorSendMsg>(OnElevatorSendMsg);
            });
    }

    private void OnElevatorSendMsg(Entity<RMCElevatorPanelComponent> ent, ref ElevatorSendMsg args)
    {
        var user = args.Actor;

        if (!TryGetEntity(args.Target, out var destination))
        {
            Log.Warning($"{ToPrettyString(user)} tried to launch to invalid elevator destination {args.Target}");
            return;
        }

        if (!TryComp<RMCElevatorDestinationComponent>(destination, out var dest))
        {
            Log.Warning(
                $"{ToPrettyString(args.Actor)} tried to launch to invalid elevator destination {ToPrettyString(destination)}");
            return;
        }

        if (!TryGetLinkedElevator(ent, out var elevator))
        {
            Log.Warning(
                $"{ToPrettyString(args.Actor)} couldn't find a valid elevator for {ToPrettyString(ent)}");
            return;
        }

        if (dest.ElevatorId != ent.Comp.ElevatorId)
            return;

        if (!Fly(elevator.Value, destination.Value, user) && _net.IsServer)
            _audio.PlayPvs(ent.Comp.FailSound, ent);
    }

    private void OnPanelOpen(Entity<RMCElevatorPanelComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        RefreshUI(ent, true);
        RefreshUI(ent, false);
    }

    protected virtual bool Fly(Entity<RMCElevatorComponent> ent, EntityUid destination, EntityUid? user)
    {
        return false;
    }

    protected virtual void RefreshUI(string elevatorId, bool destinationRefresh)
    {
    }

    protected virtual void RefreshUI(Entity<RMCElevatorPanelComponent> ent, bool destinationRefresh)
    {
    }

    protected bool TryGetLinkedElevator(Entity<RMCElevatorPanelComponent> ent, [NotNullWhen(true)] out Entity<RMCElevatorComponent>? elevator)
    {
        if (ent.Comp.LinkedElevator != null &&
            TryComp<RMCElevatorComponent>(ent.Comp.LinkedElevator, out var elev) &&
            elev.ElevatorId == ent.Comp.ElevatorId)
        {
            elevator = (ent.Comp.LinkedElevator.Value, elev);
            return true;
        }

        ent.Comp.LinkedElevator = null;
        Dirty(ent);

        var elevators = EntityQueryEnumerator<RMCElevatorComponent>();

        while (elevators.MoveNext(out var uid, out var comp))
        {
            if (comp.ElevatorId == ent.Comp.ElevatorId)
            {
                ent.Comp.LinkedElevator = uid;
                elevator = (uid, comp);
                Dirty(ent);
                return true;
            }
        }

        elevator = null;
        return false;
    }

    protected virtual void OnTryLink(Entity<RMCElevatorLinkingComponent> tool, ref AfterInteractEvent args)
    {
        if (args.Target == null)
            return;

        if (TryComp<RMCElevatorPanelComponent>(args.Target, out var panel))
        {
            args.Handled = true;

            var success = TryLink((args.Target.Value, panel));

            if (success)
                _popup.PopupClient(Loc.GetString("rmc-elevator-link-success"), args.User);
            else
                _popup.PopupClient(Loc.GetString("rmc-elevator-link-fail"), args.User, PopupType.SmallCaution);
        }

        if (TryComp<RMCElevatorDestinationDoorComponent>(args.Target, out var door))
        {
            args.Handled = true;

            var success = TryLink((args.Target.Value, door));

            if (success)
                _popup.PopupClient(Loc.GetString("rmc-elevator-link-success"), args.User);
            else
                _popup.PopupClient(Loc.GetString("rmc-elevator-link-fail"), args.User, PopupType.SmallCaution);
        }
    }

    protected bool TryLink(Entity<RMCElevatorPanelComponent> ent)
    {
        if (ent.Comp.LinkCode == String.Empty)
            return false;

        ent.Comp.LinkedDestination = GetDestination(ent.Comp.ElevatorId, ent.Comp.LinkCode);

        return ent.Comp.LinkedDestination != null;
    }

    protected bool TryLink(Entity<RMCElevatorDestinationDoorComponent> ent)
    {
        if (ent.Comp.LinkCode == String.Empty)
            return false;

        ent.Comp.LinkedDestination = GetDestination(ent.Comp.ElevatorId, ent.Comp.LinkCode);

        return ent.Comp.LinkedDestination != null;
    }

    private EntityUid? GetDestination(string elevatorId, string linkCode)
    {
        var dests = EntityQueryEnumerator<RMCElevatorDestinationComponent>();

        while (dests.MoveNext(out var uid, out var dest))
        {
            if (dest.ElevatorId != elevatorId)
                continue;

            if (dest.LinkCode != linkCode)
                continue;

            return uid;
        }

        return null;
    }
}
