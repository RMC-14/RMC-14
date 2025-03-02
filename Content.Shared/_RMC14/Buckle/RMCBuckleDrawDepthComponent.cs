using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Buckle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCBuckleSystem))]
public sealed partial class RMCBuckleDrawDepthComponent : Component
{
    [DataField, AutoNetworkedField]
    public DrawDepth.DrawDepth? BuckleDepth;

    [DataField, AutoNetworkedField]
    public DrawDepth.DrawDepth UnbuckleDepth = DrawDepth.DrawDepth.Mobs;
}
