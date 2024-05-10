using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Evolution;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoEvolutionSystem))]
public sealed partial class XenoEvolveActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown;

    [DataField, AutoNetworkedField]
    public TimeSpan CooldownAccumulated;
}
