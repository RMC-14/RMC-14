using Content.Shared.Interaction;
using Robust.Shared.Network;

namespace Content.Shared._CM14.Dropship;

public abstract class SharedDropshipSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DropshipNavigationComputerComponent, InteractHandEvent>(OnNavigationInteractHand);
        SubscribeLocalEvent<DropshipNavigationComputerComponent, ActivateInWorldEvent>(OnNavigationActivateInWorld);

        Subs.BuiEvents<DropshipNavigationComputerComponent>(DropshipNavigationUiKey.Key, subs =>
        {
            subs.Event<DropshipNavigationLaunchMsg>(OnDropshipNavigationLaunchMsg);
        });
    }

    private void OnNavigationInteractHand(Entity<DropshipNavigationComputerComponent> ent, ref InteractHandEvent args)
    {
        args.Handled = true;
        Interact(ent, args.User);
    }

    private void OnNavigationActivateInWorld(Entity<DropshipNavigationComputerComponent> ent, ref ActivateInWorldEvent args)
    {
        args.Handled = true;
        Interact(ent, args.User);
    }

    private void OnDropshipNavigationLaunchMsg(Entity<DropshipNavigationComputerComponent> ent, ref DropshipNavigationLaunchMsg args)
    {
        if (_net.IsClient)
            return;

        if (!TryGetEntity(args.Target, out var destination))
        {
            Log.Warning($"{args.Session.Name} tried to launch to invalid dropship destination {args.Target}");
            return;
        }

        if (!HasComp<DropshipDestinationComponent>(destination))
        {
            Log.Warning($"{args.Session.Name} tried to launch to invalid dropship destination {ToPrettyString(destination)}");
            return;
        }

        FlyTo(ent, destination.Value);
    }

    protected virtual void Interact(Entity<DropshipNavigationComputerComponent> ent, EntityUid user)
    {
    }

    protected virtual void FlyTo(Entity<DropshipNavigationComputerComponent> computer, EntityUid destination)
    {
    }
}
