using Robust.Shared.GameStates;

namespace Content.Shared.CM14.Xenos.Evolution;

[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoEvolutionSystem))]
public sealed partial class XenoEvolveActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown;
}
