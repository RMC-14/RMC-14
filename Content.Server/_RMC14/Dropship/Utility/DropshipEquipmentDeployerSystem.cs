using System.Numerics;
using Content.Server.Shuttles.Events;
using Content.Shared._RMC14.Dropship.AttachmentPoint;
using Content.Shared._RMC14.Dropship.Utility.Components;
using Content.Shared._RMC14.Dropship.Utility.Systems;

namespace Content.Server._RMC14.Dropship.Utility;

public sealed class DropshipEquipmentDeployerSystem : SharedDropshipEquipmentDeployerSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DropshipEquipmentDeployerComponent, FTLUpdatedRelayedEvent<FTLStartedEvent>>(OnFTLStarted);
        SubscribeLocalEvent<DropshipEquipmentDeployerComponent, FTLUpdatedRelayedEvent<FTLCompletedEvent>>(OnFTLCompleted);
    }

    private void OnFTLStarted(Entity<DropshipEquipmentDeployerComponent> ent, ref FTLUpdatedRelayedEvent<FTLStartedEvent> args)
    {
        if (!ent.Comp.AutoUnDeploy)
            return;

        TryDeploy(ent, false);
        ent.Comp.IsDeployable = false;
        Dirty(ent);
    }

    private void OnFTLCompleted(Entity<DropshipEquipmentDeployerComponent> ent, ref FTLUpdatedRelayedEvent<FTLCompletedEvent> args)
    {
        ent.Comp.IsDeployable = true;
        Dirty(ent);

        if (!ent.Comp.AutoDeploy)
            return;

        var deployOffset = Vector2.Zero;
        var rotationOffset = 0f;

        if (TryComp(args.Relayer, out DropshipWeaponPointComponent? weaponPoint))
            TryGetOffset(ent, out deployOffset, out rotationOffset, weaponPoint.Location);

        TryDeploy(ent, true, deployOffset, rotationOffset);
    }
}
