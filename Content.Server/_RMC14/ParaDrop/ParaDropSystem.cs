using Content.Server._RMC14.Dropship;
using Content.Server.Doors.Systems;
using Content.Server.Shuttles.Components;
using Content.Shared.Doors.Components;
using Content.Shared.ParaDrop;

namespace Content.Server._RMC14.ParaDrop;

public sealed partial class ParaDropSystem: SharedParaDropSystem
{
    [Dependency] private readonly DropshipSystem _dropship = default!;
    [Dependency] private readonly DoorSystem _door = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActiveParaDropComponent, ComponentShutdown>(OnActiveParaDropShutdown);
    }

    private void OnActiveParaDropShutdown(Entity<ActiveParaDropComponent> ent, ref ComponentShutdown args)
    {
        var enumerator = Transform(ent).ChildEnumerator;
        while (enumerator.MoveNext(out var child))
        {
            if (!HasComp<DockingComponent>(child)) //TODO Only lock the back doors
                continue;

            if (!TryComp(child, out DoorBoltComponent? bolt))
                return;

            _door.TrySetBoltDown((child, bolt), false);
            _dropship.LockDoor(child);
        }
    }
}
