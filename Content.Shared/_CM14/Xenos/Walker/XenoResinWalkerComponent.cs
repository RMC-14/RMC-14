using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Walker;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoResinWalkerSystem))]
public sealed partial class XenoResinWalkerComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Active;

    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaCost = 50;

    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaUpkeep = 15;

    [DataField, AutoNetworkedField]
    public TimeSpan NextPlasmaUse;

    [DataField, AutoNetworkedField]
    public TimeSpan PlasmaUseDelay = TimeSpan.FromSeconds(0.5);

    [DataField, AutoNetworkedField]
    public float SpeedMultiplier = 1.66f;
}
