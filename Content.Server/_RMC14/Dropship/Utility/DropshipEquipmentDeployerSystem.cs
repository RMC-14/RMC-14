using Content.Server.Shuttles.Events;
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

        TryGetOffset(ent, out var deployOffset, out var rotationOffset);
        TryDeploy(ent, true, deployOffset, rotationOffset);
    }
}
