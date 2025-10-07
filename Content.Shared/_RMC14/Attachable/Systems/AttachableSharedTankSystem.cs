using Content.Shared._RMC14.Attachable.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared._RMC14.Weapons.Ranged.Flamer;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.Containers.ItemSlots;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed class AttachableSharedTankSystem : EntitySystem
{

    [Dependency] private readonly SharedRMCFlamerSystem _flamer = default!;
    [Dependency] private readonly AttachableHolderSystem _holder = default!;

    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SolutionTransferSystem _solutionTransfer = default!;

    [Dependency] private readonly SharedGunSystem _gun = default!;

    private EntityQuery<RMCIgniterComponent> _flamerIgniterQuery;
    public override void Initialize()
    {
        _flamerIgniterQuery = GetEntityQuery<RMCIgniterComponent>();

        SubscribeLocalEvent<RMCAttachableSharedTankComponent, TakeAmmoEvent>(OnTakeAmmo);
        
        SubscribeLocalEvent<RMCAttachableSharedTankComponent, AttachableAlteredEvent>(InitTankShare);

        SubscribeLocalEvent<RMCAttachableSharedTankComponent, GetAmmoCountEvent>(GetAmmoCount);

        SubscribeLocalEvent<RMCAttachableSharedTankComponent, AttemptShootEvent>(AttemptShoot);
    }
    public void ShootNozzle(Entity<RMCAttachableSharedTankComponent> nozzle,
        Entity<GunComponent> gun,
        EntityUid? user,
        EntityCoordinates fromCoordinates,
        EntityCoordinates toCoordinates)
    {
        if(!TryComp(nozzle.Comp.Holder, out RMCFlamerAmmoProviderComponent? fuelTank))
            return;

        Entity<RMCFlamerAmmoProviderComponent> wrapper = (nozzle.Comp.Holder, fuelTank);
        if (!_flamer.TryGetTankSolution(wrapper, out var solutionEnt)) // Flipped TryGetTankSolution from private to public fn
            return;
        var volume = solutionEnt.Value.Comp.Solution.Volume;
        if (volume <= nozzle.Comp.CostPer)
            return;

        ProtoId<ReagentPrototype>? reagent = null;
        if (solutionEnt.Value.Comp.Solution.TryFirstOrNull(out var firstReagent))
            reagent = firstReagent.Value.Reagent.Prototype;
        //the fireballs are supposed to use the reagent in the tank but.....
        solutionEnt.Value.Comp.Solution.RemoveSolution(nozzle.Comp.CostPer);
        _solution.UpdateChemicals(solutionEnt.Value);

        EntProtoId fireKind = "";
        switch (reagent){
            case "RMCNapalmUT":
                fireKind = "RMCBulletSentryFireProjectile";

                break;
            case "1": //Standin for the other fuel types someone should probably reagent this up though...
                fireKind = "RMCBulletFireVesgGreen";
                break;
            case "2":
                fireKind = "RMCBulletFireVesgBlue";
                break;
            default: //something funky is in the tank
                return; 
        }
        if(fireKind.Equals(""))
            return;
        var ball = Spawn(fireKind, fromCoordinates);
        var ballComp = EnsureComp<AmmoComponent>(ball);

        _gun.Shoot(nozzle, gun, ball, fromCoordinates, toCoordinates, out var userImpulse, user, false); // I don't want to reimpl projectile stuffs
    }
    private void OnTakeAmmo(Entity<RMCAttachableSharedTankComponent> ent, ref TakeAmmoEvent args)
    {
        if(!TryComp(ent.Comp.Holder, out RMCFlamerAmmoProviderComponent? fuelTank))
            return;
        args.Ammo.Add((ent, ent.Comp));
    }

    private void InitTankShare(Entity<RMCAttachableSharedTankComponent> ent, ref AttachableAlteredEvent args)
    {
        ent.Comp.Holder = args.Holder;
        if (! TryComp(args.Holder, out RMCIgniterComponent? igniter))
            return;
        ent.Comp.Enabled = igniter.Enabled;
        Dirty(ent, ent.Comp);

    }

    private void AttemptShoot(Entity<RMCAttachableSharedTankComponent> ent, ref AttemptShootEvent args)
    { // I know more stuff can be here for pretty text and everything...
        if (args.Cancelled)
            return;

        if (!ent.Comp.Enabled)
            args.Cancelled = true;
    }

    private void GetAmmoCount(Entity<RMCAttachableSharedTankComponent> ent, ref GetAmmoCountEvent args)
    {
        args.Count = 100;
        args.Capacity = 100;
        /*if(!TryComp(ent.Comp.Holder, out RMCFlamerAmmoProviderComponent? fuelTank))
            return;
        if(!TryComp(ent.Comp.Holder, out ItemSlotsComponent? guh))
            return;
        var tankEntity = guh.Slots["gun_magazine"].Item;
        //return;
        if (! (tankEntity != null))
            return;
        //args.Ammo.Add((ent, fuelTank));
        (EntityUid lhs, RMCFlamerAmmoProviderComponent rhs) tuple = (tankEntity.Value, fuelTank);
        Entity<RMCFlamerAmmoProviderComponent> wrapper = tuple;
        if (!_flamer.TryGetTankSolution(wrapper, out var solutionEnt)) // Flipped TryGetTankSolution from private to public fn
            return;
        var solution = solutionEnt.Value.Comp.Solution;
        args.Count = solution.Volume.Int();
        args.Capacity = solution.MaxVolume.Int();*/
        return;
    } 

}