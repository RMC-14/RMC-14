using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Buckle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCBuckleSystem))]
public sealed partial class ActiveBuckleClimbingComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Strap;
}
