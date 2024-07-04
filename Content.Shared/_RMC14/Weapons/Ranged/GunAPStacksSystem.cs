using Content.Shared._RMC14.Weapons.Common;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared._RMC14.Weapons.Ranged;

public abstract class SharedPumpActionSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<PumpActionComponent, ExaminedEvent>(OnExamined, before: [typeof(SharedGunSystem)]);
        SubscribeLocalEvent<PumpActionComponent, AttemptShootEvent>(OnAttemptShoot);
    }

}
