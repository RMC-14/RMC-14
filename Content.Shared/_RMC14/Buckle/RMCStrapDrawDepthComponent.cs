using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Buckle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCBuckleSystem))]
public sealed partial class RMCStrapDrawDepthComponent : Component
{
    [DataField, AutoNetworkedField]
    public DrawDepth.DrawDepth UnstrappedDepth = DrawDepth.DrawDepth.Mobs;

    [DataField, AutoNetworkedField]
    public DrawDepth.DrawDepth StrappedDepth = DrawDepth.DrawDepth.Mobs;
}
