using Content.Server._RMC14.Decals;
using Content.Server.Spawners.Components;
using Content.Shared._RMC14.Chemistry;
using Content.Shared._RMC14.Medical.Wounds;
using Content.Shared.Coordinates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Medical.Wounds;

public sealed partial class RMCBloodSplatterSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly RMCDecalSystem _rmcDecal = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCBloodSplattererComponent, RMCWoundAddedEvent>(OnWoundAdded);
        SubscribeLocalEvent<RMCBloodSplattererComponent, RMCVomitEvent>(OnVomit);
    }

    public void OnWoundAdded(Entity<RMCBloodSplattererComponent> ent, ref RMCWoundAddedEvent args)
    {
        if (args.Wound.Type != WoundType.Brute)
            return;

        if (args.Wound.Damage < ent.Comp.MinimalTriggerDamage)
        {
            SpawnDecal(ent, ent.Comp.BloodMinorDecal);
            return;
        }

        SpawnDecal(ent, ent.Comp.BloodDecal);
    }

    public void OnVomit(Entity<RMCBloodSplattererComponent> ent, ref RMCVomitEvent args)
    {
        SpawnDecal(ent, ent.Comp.VomitDecal);
    }

    private void SpawnDecal(EntityUid ent, EntProtoId decalSpawner)
    {
        if (!_prototypes.TryIndex(decalSpawner, out var prototype) ||
            !prototype.TryGetComponent(out RandomDecalSpawnerComponent? spawner, _compFactory) ||
            _rmcDecal.GetDecalsInTile(ent, spawner.Decals) < spawner.MaxDecalsPerTile)
        {
            Spawn(decalSpawner, ent.ToCoordinates());
        }
    }
}
