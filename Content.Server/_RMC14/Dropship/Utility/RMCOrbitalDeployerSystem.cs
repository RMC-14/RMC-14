using Content.Server._RMC14.NPC;
using Content.Server._RMC14.NPC.HTN;
using Content.Shared._RMC14.Dropship.Utility.Systems;
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
    }

    private void OnParaDropFinished(Entity<SleepNPCComponent> ent, ref ParaDropFinishedEvent args)
    {
        _rmcNpc.WakeNPC(ent);
        _physics.SetCanCollide(ent, true);
    }
}
