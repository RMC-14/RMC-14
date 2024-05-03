using Content.Shared.UserInterface;
using Robust.Shared.Network;

namespace Content.Shared._CM14.Dropship;

public abstract class SharedDropshipSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DropshipNavigationComputerComponent, ActivatableUIOpenAttemptEvent>(OnActivatableUIOpenAttempt);
        SubscribeLocalEvent<DropshipNavigationComputerComponent, AfterActivatableUIOpenEvent>(OnNavigationOpen);

        Subs.BuiEvents<DropshipNavigationComputerComponent>(DropshipNavigationUiKey.Key, subs =>
        {
            subs.Event<DropshipNavigationLaunchMsg>(OnDropshipNavigationLaunchMsg);
        });

        Subs.BuiEvents<DropshipNavigationComputerComponent>(DropshipHijackerUiKey.Key, subs =>
        {
            subs.Event<DropshipHijackerDestinationChosenBuiMsg>(OnHijackerDestinationChosenMsg);
        });
    }

    private void OnActivatableUIOpenAttempt(Entity<DropshipNavigationComputerComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (TryComp(ent, out TransformComponent? xform) &&
            TryComp(xform.ParentUid, out DropshipComponent? dropship) &&
            dropship.Crashed)
        {
            args.Cancel();
        }
    }

    private void OnNavigationOpen(Entity<DropshipNavigationComputerComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        RefreshUI(ent);
    }

    private void OnDropshipNavigationLaunchMsg(Entity<DropshipNavigationComputerComponent> ent, ref DropshipNavigationLaunchMsg args)
    {
        if (_net.IsClient)
            return;

        if (!TryGetEntity(args.Target, out var destination))
        {
            Log.Warning($"{ToPrettyString(args.Actor)} tried to launch to invalid dropship destination {args.Target}");
            return;
        }

        if (!HasComp<DropshipDestinationComponent>(destination))
        {
            Log.Warning($"{ToPrettyString(args.Actor)} tried to launch to invalid dropship destination {ToPrettyString(destination)}");
            return;
        }

        FlyTo(ent, destination.Value, args.Actor);
        _ui.CloseUi(ent.Owner, DropshipNavigationUiKey.Key, args.Actor);
    }

    private void OnHijackerDestinationChosenMsg(Entity<DropshipNavigationComputerComponent> ent, ref DropshipHijackerDestinationChosenBuiMsg args)
    {
        if (!TryGetEntity(args.Destination, out var destination))
        {
            Log.Warning($"{ToPrettyString(args.Actor)} tried to hijack to invalid destination");
            return;
        }

        if (!HasComp<DropshipHijackDestinationComponent>(destination))
        {
            Log.Warning($"{ToPrettyString(args.Actor)} tried to hijack to invalid destination {ToPrettyString(destination)}");
            return;
        }

        if (FlyTo(ent, destination.Value, args.Actor, true) &&
            TryComp(ent, out TransformComponent? xform) &&
            xform.ParentUid.Valid)
        {
            var dropship = EnsureComp<DropshipComponent>(xform.ParentUid);
            dropship.Crashed = true;
            Dirty(xform.ParentUid, dropship);
        }

        _ui.CloseUi(ent.Owner, DropshipHijackerUiKey.Key, args.Actor);
    }

    protected virtual bool FlyTo(Entity<DropshipNavigationComputerComponent> computer, EntityUid destination, EntityUid user, bool hijack = false)
    {
        return false;
    }

    protected virtual void RefreshUI(Entity<DropshipNavigationComputerComponent> computer)
    {
    }
}
