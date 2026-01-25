using Content.Server._RMC14.NPC.HTN;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.NPC;

namespace Content.Server._RMC14.NPC;

public sealed class RMCNPCSystem : SharedRMCNPCSystem
{
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DropshipLandedOnPlanetEvent>(OnDropshipLandedOnPlanet);

        SubscribeLocalEvent<SleepNPCComponent, MapInitEvent>(OnSleepNPCMapInit, after: [typeof(HTNSystem)]);
    }

    private void OnDropshipLandedOnPlanet(ref DropshipLandedOnPlanetEvent ev)
    {
        if (!TryComp(ev.Dropship, out TransformComponent? dropshipTransform))
            return;

        var wake = EntityQueryEnumerator<WakeNPCOnDropshipLandingComponent, TransformComponent>();
        while (wake.MoveNext(out var uid, out var npc, out var npcTransform))
        {
            if (npc.FirstOnly && npc.Attempted)
                continue;

            if (dropshipTransform.MapUid != npcTransform.MapUid)
                continue;

            npc.Attempted = true;
            if (!_transform.InRange(dropshipTransform.Coordinates, npcTransform.Coordinates, npc.Range))
                continue;

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
