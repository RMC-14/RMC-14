using Content.Server._RMC14.NPC.HTN;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.NPC;

namespace Content.Server._RMC14.NPC;

public sealed class RMCNPCSystem : SharedRMCNPCSystem
{
    [Dependency] private readonly NPCSystem _npc = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DropshipLandedOnPlanetEvent>(OnDropshipLandedOnPlanet);

        SubscribeLocalEvent<SleepNPCComponent, MapInitEvent>(OnSleepNPCMapInit, after: [typeof(HTNSystem)]);
    }

    private void OnDropshipLandedOnPlanet(ref DropshipLandedOnPlanetEvent ev)
    {
        var wake = EntityQueryEnumerator<WakeNPCOnDropshipLandingComponent>();
        while (wake.MoveNext(out var uid, out _))
        {
            WakeNPC(uid);
        }
    }

    private void OnSleepNPCMapInit(Entity<SleepNPCComponent> ent, ref MapInitEvent args)
    {
        SleepNPC(ent);
    }

    public override void SleepNPC(EntityUid id)
    {
        base.SleepNPC(id);
        _npc.SleepNPC(id);
    }

    public override void WakeNPC(EntityUid id)
    {
        base.WakeNPC(id);
        _npc.WakeNPC(id);
    }
}
