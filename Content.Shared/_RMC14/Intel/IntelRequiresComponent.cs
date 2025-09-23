using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Intel;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(IntelSystem))]
public sealed partial class IntelRequiresComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<EntityUid> Requires = new();

    [DataField, AutoNetworkedField]
    public int RequiresCount = 1;
}
