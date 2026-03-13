using Content.Server._RMC14.NPC;
using Content.Server._RMC14.NPC.HTN;
using Content.Shared._RMC14.Dropship.Utility.Systems;
using Content.Shared._RMC14.SupplyDrop;
using Content.Shared.ParaDrop;
using Robust.Server.GameObjects;

namespace Content.Server._RMC14.Dropship.Utility;

public sealed class RMCOrbitalDeployerSystem : SharedRMCOrbitalDeployerSystem
{
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly RMCNPCSystem _rmcNpc = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SleepNPCComponent, ParaDropFinishedEvent>(OnParaDropFinished);
        SubscribeLocalEvent<SupplyDropPodComponent, ParaDropFinishedEvent>(OnParaDropFinished);
    }

    private void OnParaDropFinished(Entity<SleepNPCComponent> ent, ref ParaDropFinishedEvent args)
    {
        _rmcNpc.WakeNPC(ent);
        _physics.SetCanCollide(ent, true);
    }

    private void OnParaDropFinished(Entity<SupplyDropPodComponent> ent, ref ParaDropFinishedEvent args)
    {
        ent.Comp.Landed = true;
        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<SupplyDropPodComponent>();
        while (query.MoveNext(out var uid, out var dropPod))
        {
            if (!dropPod.Landed)
                continue;

            dropPod.OpenTimeRemaining -= TimeSpan.FromSeconds(frameTime);
            if (dropPod.OpenTimeRemaining > TimeSpan.Zero)
                continue;

            if (Container.TryGetContainer(uid, dropPod.DeploySlotId, out var container))
            {
                var deployedEntities = Container.EmptyContainer(container, true);
                foreach (var entity in deployedEntities)
                {
                    var ev = new ParaDropFinishedEvent();
                    RaiseLocalEvent(entity, ref ev);
                }
            }
            QueueDel(uid);
        }
    }
}
