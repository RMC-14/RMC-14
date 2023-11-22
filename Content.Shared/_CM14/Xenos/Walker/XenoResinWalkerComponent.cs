using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Walker;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoResinWalkerSystem))]
public sealed partial class XenoResinWalkerComponent : Component
{
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Active;

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 PlasmaCost = 50;

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 PlasmaPerSecond = 30;

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float SpeedMultiplier = 1.66f;
}
