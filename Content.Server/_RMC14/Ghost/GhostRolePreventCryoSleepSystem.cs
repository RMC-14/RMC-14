using Content.Shared.Bed.Cryostorage;
using Content.Shared.Mind.Components;

namespace Content.Server._RMC14.Ghost;

public sealed partial class GhostRolePreventCryoSleepSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GhostRolePreventCryoSleepComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<GhostRolePreventCryoSleepComponent, MindAddedMessage>(OnTakeover);
    }

    public void OnStartup(Entity<GhostRolePreventCryoSleepComponent> ent, ref ComponentStartup args)
    {
        if (!RemComp<CanEnterCryostorageComponent>(ent))
            RemComp<GhostRolePreventCryoSleepComponent>(ent);
    }

    public void OnTakeover(Entity<GhostRolePreventCryoSleepComponent> ent, ref MindAddedMessage args)
    {
        AddComp<CanEnterCryostorageComponent>(ent);
        RemComp<GhostRolePreventCryoSleepComponent>(ent);
    }
}
