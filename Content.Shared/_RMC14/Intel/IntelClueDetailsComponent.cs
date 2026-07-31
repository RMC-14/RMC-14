using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Intel;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(IntelSystem))]
public sealed partial class IntelClueDetailsComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Label = string.Empty;

    [DataField, AutoNetworkedField]
    public LocId? ColorName;
}
