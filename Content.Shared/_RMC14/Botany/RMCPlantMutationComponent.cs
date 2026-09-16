using Content.Shared.Random;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared._RMC14.Botany;

public enum RMCPlantMutationTier
{
    Normal,
    HighlyMutable,
    Immutable,
}

/// <summary>
/// Mutation state for a <see cref="RMCPlantComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, fieldDeltas: true)]
public sealed partial class RMCPlantMutationComponent : Component
{
    /// <summary>
    /// Controls which mutations can fire on this plant.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, float> Slots = new();

    [DataField, AutoNetworkedField]
    public int GeneEditCount;

    [DataField, AutoNetworkedField]
    public List<RandomPlantMutation> Mutations = new();

    /// <summary>
    /// Plant prototypes this plant may mutate into when prompted to.
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>)), AutoNetworkedField]
    public List<string> MutationPrototypes = new();

    [DataField, AutoNetworkedField]
    public RMCPlantMutationTier MutationTier = RMCPlantMutationTier.Normal;
}
