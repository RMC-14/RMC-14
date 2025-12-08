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
using Content.Shared._RMC14.Weapons.Common;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed class AttachableSharedTankSystem : EntitySystem
{

    [Dependency] private readonly SharedRMCFlamerSystem _flamer = default!;
    [Dependency] private readonly AttachableHolderSystem _holder = default!;

    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

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

        SubscribeLocalEvent<RMCAttachableSharedTankComponent, UniqueActionEvent>(OnNozzleUniqueAction);

        SubscribeLocalEvent<RMCAttachableSharedTankComponent, EntInsertedIntoContainerMessage>(OnInsertedIntoContainer);

        SubscribeLocalEvent<RMCAttachableSharedTankComponent, EntRemovedFromContainerMessage>(OnRemovedFromContainer);
    }
    public void ShootNozzle(Entity<RMCAttachableSharedTankComponent> nozzle,
        Entity<GunComponent> gun,
        EntityUid? user,
        EntityCoordinates fromCoordinates,
        EntityCoordinates toCoordinates)
    {
        if(!TryComp(nozzle.Comp.Holder, out RMCFlamerAmmoProviderComponent? fuelTank))
            return;

        Entity<RMCFlamerAmmoProviderComponent> wrapper = (nozzle.Comp.Holder.GetValueOrDefault(), fuelTank);
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
            
            case "reagent-name-welding-fuel":
                fireKind = "RMCBulletSentryFireProjectile";
                break;
            
            case "RMCBGel": 
                fireKind = "RMCBulletFireVesgGreen";
                break;
            
            case "RMCNapalmX":
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
        switch (args.Alteration)
        {
            case AttachableAlteredType.Attached:
                ent.Comp.Holder = args.Holder;
                Dirty(ent, ent.Comp);
                break;
            case AttachableAlteredType.Detached:
                ent.Comp.Holder = null;
                Dirty(ent, ent.Comp);
                break;
        }

    }

    private void AttemptShoot(Entity<RMCAttachableSharedTankComponent> ent, ref AttemptShootEvent args)
    { // I know more stuff can be here for pretty text and everything...
        if(!TryComp(ent.Comp.Holder, out RMCIgniterComponent? flamerIgniter))
            return;
        if ( ! (ent.Comp.Holder != null))
            return;
        Entity<RMCIgniterComponent> wrapper = (ent.Comp.Holder.GetValueOrDefault(), flamerIgniter);
        if (args.Cancelled)
            return;

        if (!wrapper.Comp.Enabled)
            args.Cancelled = true;
    }

    private void GetAmmoCount(Entity<RMCAttachableSharedTankComponent> ent, ref GetAmmoCountEvent args)
    {
        if(!TryComp(ent.Comp.Holder, out RMCFlamerAmmoProviderComponent? fuelTank))
            return;
        if ( ! (ent.Comp.Holder != null))
            return;
        Entity<RMCFlamerAmmoProviderComponent> wrapper = (ent.Comp.Holder.GetValueOrDefault(), fuelTank);
        if (!_flamer.TryGetTankSolution(wrapper, out var solutionEnt)) // Flipped TryGetTankSolution from private to public fn
            return;
        var solution = solutionEnt.Value.Comp.Solution;
        args.Count = solution.Volume.Int();
        args.Capacity = solution.MaxVolume.Int();
        return;
    } 

    private void OnNozzleUniqueAction(Entity<RMCAttachableSharedTankComponent> ent, ref UniqueActionEvent args)
    {
        if ( ! (ent.Comp.Holder != null))
            return;
        if(!TryComp(ent.Comp.Holder, out RMCIgniterComponent? flamerIgniter))
            return;
        Entity<RMCIgniterComponent> wrapper = (ent.Comp.Holder.GetValueOrDefault(), flamerIgniter);
        RaiseLocalEvent<RMCIgniterComponent>(wrapper);
        //ent.Comp.Enabled = wrapper.Comp.Enabled;

        //Dirty(ent);
    }

    private void OnInsertedIntoContainer(Entity<RMCAttachableSharedTankComponent> ent, ref EntInsertedIntoContainerMessage args)
    {

        if ( ! (ent.Comp.Holder != null))
            return;
        if(!TryComp(ent.Comp.Holder, out RMCFlamerAmmoProviderComponent? flamerIgniter))
            return;
        Entity<RMCFlamerAmmoProviderComponent> wrapper = (ent.Comp.Holder.GetValueOrDefault(), flamerIgniter);
        RaiseLocalEvent<RMCFlamerAmmoProviderComponent>(wrapper);
        return;
    }

    private void OnRemovedFromContainer(Entity<RMCAttachableSharedTankComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if ( ! (ent.Comp.Holder != null))
            return;
        if(!TryComp(ent.Comp.Holder, out RMCFlamerAmmoProviderComponent? flamerIgniter))
            return;
        Entity<RMCFlamerAmmoProviderComponent> wrapper = (ent.Comp.Holder.GetValueOrDefault(), flamerIgniter);
        RaiseLocalEvent<RMCFlamerAmmoProviderComponent>(wrapper);
        return;
    }

}
