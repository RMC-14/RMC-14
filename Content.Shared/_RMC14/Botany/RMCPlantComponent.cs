using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Botany;

/// <summary>
/// The living plant growing inside a <see cref="RMCPlantTrayComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, fieldDeltas: true)]
public sealed partial class RMCPlantComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Health;

    [DataField, AutoNetworkedField]
    public int Age;

    [DataField, AutoNetworkedField]
    public int SkipAging;

    [DataField, AutoNetworkedField]
    public bool Dead;

    /// <summary>
    /// True when the plant is ready to be harvested.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Harvest;

    [DataField, AutoNetworkedField]
    public bool Sampled;

    /// <summary>
    /// Multiplier for the number of entities produced at harvest.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float YieldMod;

    [DataField, AutoNetworkedField]
    public float MutationMod;

    [DataField, AutoNetworkedField]
    public float MutationLevel;

    /// <summary>
    /// Adjusts growth cycle speed
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MetabolismAdjust;

    [DataField, AutoNetworkedField]
    public float BlightProductionCounter;

    [DataField]
    public EntityUid? Tray;

    [DataField, AutoNetworkedField]
    public string Name = "";

    [DataField, AutoNetworkedField]
    public string Noun = "";

    [DataField, AutoNetworkedField]
    public string DisplayName = "";

    [DataField, AutoNetworkedField]
    public bool Mysterious;

    [DataField]
    public bool UpdateSpriteAfterUpdate;
}
