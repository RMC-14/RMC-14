using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Intel;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(IntelSystem))]
public sealed partial class IntelChemGrantPaperComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Grant;

    [DataField, AutoNetworkedField]
    public int MinGrant = 2;

    [DataField, AutoNetworkedField]
    public int MaxGrant = 4;
}
