using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Botany;

/// <summary>
/// Nutrient/water consumption rate for a <see cref="RMCPlantComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, fieldDeltas: true)]
public sealed partial class RMCPlantMetabolismComponent : Component
{
    [DataField, AutoNetworkedField]
    public float NutrientConsumption = 0.25f;

    [DataField, AutoNetworkedField]
    public float WaterConsumption = 0.5f;
}
