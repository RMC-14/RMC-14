using System.Linq;
using Content.Shared._RMC14.Botany;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Kitchen.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager;

namespace Content.Server._RMC14.Botany;

/// <summary>
/// Spawns seed packets from live plants (sampling, harvestin, age revert) & applies a
/// packets captured state onto a freshly planted plant entity
/// </summary>
public sealed class RMCPlantSeedSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly ISerializationManager _serManager = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    private static readonly Type[] MutableComponentTypes =
    {
        typeof(RMCPlantGrowthComponent),
        typeof(RMCPlantChemicalsComponent),
        typeof(RMCPlantHarvestComponent),
        typeof(RMCPlantAtmosphericComponent),
        typeof(RMCConsumeExudeGasComponent),
        typeof(RMCPlantTraitsComponent),
        typeof(RMCPlantMutationComponent),
        typeof(RMCPlantMetabolismComponent),
        typeof(RMCPlantTraitLigneousComponent),
        typeof(RMCPlantTraitScreamComponent),
        typeof(RMCPlantTraitCarnivorousComponent),
        typeof(RMCPlantTraitParasiteComponent),
        typeof(RMCPlantTraitBioluminescentComponent),
        typeof(RMCPlantTraitFlowersComponent),
    };

    private static readonly Type[] MutableTraitComponentTypes =
        MutableComponentTypes.Where(t => typeof(RMCPlantTraitComponent).IsAssignableFrom(t)).ToArray();

    /// <summary>
    /// Reads the growth/harvest/chemical stats a seed packet would produce if planted, without
    /// actually planting it. Used by UIs
    /// </summary>
    public RMCSeedStats GetSeedStats(EntityUid packet)
    {
        var snapshot = GetOrCreateSeedSnapshot(packet);
        var growth = snapshot.OfType<RMCPlantGrowthComponent>().FirstOrDefault();
        var harvest = snapshot.OfType<RMCPlantHarvestComponent>().FirstOrDefault();
        var chemicals = snapshot.OfType<RMCPlantChemicalsComponent>().FirstOrDefault();

        return new RMCSeedStats(
            growth?.Endurance ?? 0f,
            growth?.Lifespan ?? 0f,
            growth?.Maturation ?? 0f,
            growth?.Production ?? 0f,
            harvest?.Yield ?? 0,
            harvest?.HarvestRepeat ?? HarvestType.NoRepeat,
            harvest?.Seedless ?? false,
            chemicals?.Potency ?? 0f,
            growth?.Viable ?? true);
    }

    public EntityUid SpawnSeedPacket(EntityUid source, EntityCoordinates coords, EntityUid? user, float? healthOverride)
    {
        var harvest = Comp<RMCPlantHarvestComponent>(source);

        string name;
        string noun;
        string? plantProtoId;

        if (TryComp(source, out RMCPlantComponent? plantComp))
        {
            name = plantComp.Name;
            noun = plantComp.Noun;
            plantProtoId = MetaData(source).EntityPrototype?.ID;
        }
        else if (TryComp(source, out RMCProduceComponent? produceComp))
        {
            name = produceComp.Name;
            noun = produceComp.Noun;
            plantProtoId = produceComp.PlantPrototype;
        }
        else
        {
            return EntityUid.Invalid;
        }

        var packet = Spawn(harvest.PacketPrototype, coords);
        var seedComp = EnsureComp<RMCPlantSeedComponent>(packet);
        seedComp.PlantPrototype = plantProtoId;
        seedComp.HealthOverride = healthOverride;
        Dirty(packet, seedComp);

        var mutation = EnsureComp<RMCPlantSeedMutationComponent>(packet);
        mutation.MutatedComponents = SnapshotMutableComponents(source);

        var val = Loc.GetString("botany-seed-packet-name", ("seedName", Loc.GetString(name)), ("seedNoun", Loc.GetString(noun)));
        _metaData.SetEntityName(packet, val);

        if (user != null)
            _hands.TryPickupAnyHand(user.Value, packet);

        return packet;
    }

    /// <summary>
    /// Clones the current value of every mutable component on <paramref name="plant"/> into a
    /// detached list
    /// </summary>
    public List<IComponent> SnapshotMutableComponents(EntityUid plant)
    {
        var snapshot = new List<IComponent>();
        foreach (var type in MutableComponentTypes)
        {
            if (EntityManager.TryGetComponent(plant, type, out var comp))
                snapshot.Add(CloneComponent(comp));
        }

        return snapshot;
    }

    /// <summary>
    /// Overwrites the matching components on <paramref name="target"/> with the values captured in
    /// <paramref name="snapshot"/>
    /// </summary>
    public void ApplyMutatedComponents(EntityUid target, List<IComponent> snapshot)
    {
        foreach (var comp in snapshot)
        {
            EntityManager.CopyComponent(default, target, comp);
        }

        foreach (var type in MutableTraitComponentTypes)
        {
            if (!snapshot.Any(c => c.GetType() == type))
                RemComp(target, type);
        }
    }

    private IComponent CloneComponent(IComponent source)
    {
        object? clone = _componentFactory.GetComponent(source.GetType());
        _serManager.CopyTo(source, ref clone, notNullableOverride: true);
        return (IComponent)clone!;
    }

    public bool CanHarvest(EntityUid plant, EntityUid? held = null)
    {
        return !HasComp<RMCPlantTraitLigneousComponent>(plant) || (held != null && HasComp<SharpComponent>(held.Value));
    }

    /// <summary>
    /// Returns the seed packets mutable component snapshot
    /// </summary>
    public List<IComponent> GetOrCreateSeedSnapshot(EntityUid packet)
    {
        var mutation = EnsureComp<RMCPlantSeedMutationComponent>(packet);
        if (mutation.MutatedComponents != null)
            return mutation.MutatedComponents;

        var seedComp = Comp<RMCPlantSeedComponent>(packet);
        if (seedComp.PlantPrototype == null)
        {
            mutation.MutatedComponents = new List<IComponent>();
            return mutation.MutatedComponents;
        }

        var temp = Spawn(seedComp.PlantPrototype.Value.Id, Transform(packet).Coordinates);
        mutation.MutatedComponents = SnapshotMutableComponents(temp);
        QueueDel(temp);

        return mutation.MutatedComponents;
    }
}
