using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Wounds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedWoundsSystem))]
public sealed partial class EscharSettingsComponent : Component
{
    [DataField, AutoNetworkedField]
    public float BodyBurnThreshold = 30f;

    [DataField, AutoNetworkedField]
    public float HealingThreshold = 5f;

    [DataField, AutoNetworkedField]
    public float LegWalkSpeedModifier = 0.75f;

    [DataField, AutoNetworkedField]
    public float LegSprintSpeedModifier = 0.75f;
}
