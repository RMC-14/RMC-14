using Content.Server._RMC14.Dropship;
using Content.Shared.Doors.Components;
using Content.Shared.ParaDrop;

namespace Content.Server._RMC14.ParaDrop;

public sealed partial class ParaDropSystem: SharedParaDropSystem
{
    [Dependency] private readonly DropshipSystem _dropship = default!;

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
            if (!TryComp(child, out DoorBoltComponent? bolt) ||
                !TryComp(child, out DoorComponent? door) ||
                door.Location != DoorLocation.Aft)
                continue;

            _dropship.UnlockDoor(child);
            _dropship.LockDoor(child);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var paraDroppingQuery = EntityQueryEnumerator<ParaDroppingComponent>();

        while (paraDroppingQuery.MoveNext(out var uid, out var paraDropping))
        {
            paraDropping.RemainingTime -= frameTime;
            Dirty(uid, paraDropping);
            if (paraDropping.RemainingTime <= 0)
                RemComp<ParaDroppingComponent>(uid);

            Blocker.UpdateCanMove(uid);
        }

        var skyFallingQuery = EntityQueryEnumerator<SkyFallingComponent>();
        while (skyFallingQuery.MoveNext(out var uid, out var skyFalling))
        {
            skyFalling.RemainingTime -= frameTime;
            if (skyFalling.RemainingTime <= 0)
                RemComp<SkyFallingComponent>(uid);
        }
    }
}
