using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Buckle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMBuckleSystem))]
public sealed partial class ActiveBuckleClimbingComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Strap;
}
