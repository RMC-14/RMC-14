using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceNetwork;
using Content.Shared.Interaction;
using Content.Shared.Lock;
using Content.Shared.Access.Systems;
using Content.Shared._RMC14.Doors;

namespace Content.Server.DeviceLinking.Systems;

//This is code from the original SS14, modernized for RMC buttons.
public sealed class CMSignalSwitchSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CMSignalSwitchComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CMSignalSwitchComponent, ActivateInWorldEvent>(OnActivated);
    }

    private void OnInit(EntityUid uid, CMSignalSwitchComponent comp, ComponentInit args)
    {
        _deviceLink.EnsureSourcePorts(uid, comp.OnPort, comp.OffPort, comp.StatusPort);
    }

    private void OnActivated(EntityUid uid, CMSignalSwitchComponent comp, ref ActivateInWorldEvent args)
    {
        var user = args.User;
        Entity<CMSignalSwitchComponent> button = (uid, comp);

        if (args.Handled || !args.Complex)
            return;

        if (_lock.IsLocked(uid))
            return;

        if (!_accessReader.IsAllowed(user, button))
        {
            return;
        }


        comp.State = !comp.State;
        _deviceLink.InvokePort(uid, comp.State ? comp.OnPort : comp.OffPort);

        // only send status if it's a toggle switch and not a button
        if (comp.OnPort != comp.OffPort)
        {
            _deviceLink.SendSignal(uid, comp.StatusPort, comp.State);
        }

        args.Handled = true;
    }
}
