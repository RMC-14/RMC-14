using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Botany;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, fieldDeltas: true)]
public sealed partial class RMCPlantAtmosphericComponent : Component
{
    [DataField, AutoNetworkedField]
    public float MinHeat = 283f;

    [DataField, AutoNetworkedField]
    public float MaxHeat = 303f;

    [DataField, AutoNetworkedField]
    public float MinPressure = 81f;

    [DataField, AutoNetworkedField]
    public float MaxPressure = 121f;

    [DataField, AutoNetworkedField]
    public float AlterTemperature;

    [DataField, AutoNetworkedField]
    public float IdealLight;

    [DataField, AutoNetworkedField]
    public float LightTolerance;
}
