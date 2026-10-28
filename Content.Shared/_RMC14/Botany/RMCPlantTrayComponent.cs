using Content.Shared.Chemistry.Components;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Botany;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, fieldDeltas: true)]
public sealed partial class RMCPlantTrayComponent : Component
{
    /// <summary>
    /// Game time for the next plant reagent update.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    /// <summary>
    /// Time between plant reagent consumption updates.
    /// </summary>
    [DataField]
    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(3.5);

    /// <summary>
    /// Time between plant growth updates.
    /// </summary>
    [DataField]
    public TimeSpan CycleDelay = TimeSpan.FromSeconds(15f);

    /// <summary>
    /// Game time when the tray last did a growth update.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LastCycle = TimeSpan.Zero;

    [DataField, AutoNetworkedField]
    public float WaterLevel = 100f;

    [DataField, AutoNetworkedField]
    public float NutritionLevel = 100f;

    [DataField, AutoNetworkedField]
    public float Toxins;

    [DataField, AutoNetworkedField]
    public float PestLevel;

    [DataField, AutoNetworkedField]
    public float WeedLevel;

    [DataField]
    public float WeedCoefficient = 1f;

    [DataField]
    public string SoilSolutionName = "soil";

    [ViewVariables]
    public Entity<SolutionComponent>? SoilSolution;

    [DataField]
    public SoundSpecifier? WateringSound;

    [DataField]
    public bool DrawWarnings;

    [DataField]
    public bool UpdateSpriteAfterUpdate;

    [DataField, AutoNetworkedField]
    public int MissingGas;

    [DataField, AutoNetworkedField]
    public bool ImproperHeat;

    [DataField, AutoNetworkedField]
    public bool ImproperPressure;

    [DataField]
    public bool ForceUpdate;

    [DataField]
    public string PlantSlotId = "plant_slot";

    public ContainerSlot PlantSlot = default!;
}
