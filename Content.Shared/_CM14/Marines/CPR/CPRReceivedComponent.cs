using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Marines.CPR;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CPRSystem))]
public sealed partial class CPRReceivedComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Last;
}
