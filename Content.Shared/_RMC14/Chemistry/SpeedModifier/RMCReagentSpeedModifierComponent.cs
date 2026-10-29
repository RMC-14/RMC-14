using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry.SpeedModifier;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCReagentSpeedModifierSystem))]
public sealed partial class RMCReagentSpeedModifierComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<string, float> Delays = new();

    [DataField, AutoNetworkedField]
    public Dictionary<string, TimeSpan> ExpiresAt = new();
}
