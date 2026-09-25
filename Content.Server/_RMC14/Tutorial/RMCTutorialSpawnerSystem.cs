using System.Linq;
using Content.Shared.NPC.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;

namespace Content.Server._RMC14.Tutorial;

public sealed partial class RMCTutorialSpawnerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCTutorialSpawnerComponent, StartCollideEvent>(OnCollide);
    }

    private void OnCollide(EntityUid uid, RMCTutorialSpawnerComponent component, ref StartCollideEvent args)
    {
        // Prevent repeat triggering.
        if (component.HasTriggered)
            return;

        var subject = args.OtherEntity;
        // Ensure entity has a faction.
        if (!TryComp<RMCTutorialDummyComponent>(uid, out var tutComp) ||
            !TryComp<NpcFactionMemberComponent>(subject, out var factionComp))
            return;

        // Ensures triggering entity is a member of the wanted faction
        if (!factionComp.Factions.Any(faction => tutComp.Factions.Contains(faction)))
            return;

        var currentPos = Transform(uid).Coordinates;
        var newPos = new EntityCoordinates(currentPos.EntityId,
            currentPos.X + component.SpawnOffsetX,
            currentPos.Y + component.SpawnOffsetY);

        for (int i = 0; i < component.SpawnCount; i++)
        {
            SpawnAtPosition(component.SpawnPrototype, newPos);
        }

        component.HasTriggered = true;
        Log.Info($"Done");
    }
}
