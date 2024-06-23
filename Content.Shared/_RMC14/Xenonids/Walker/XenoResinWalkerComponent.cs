using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Walker;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(XenoResinWalkerSystem))]
public sealed partial class XenoResinWalkerComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Active;

    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaCost = 50;

    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaUpkeep = 15;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextPlasmaUse;

    [DataField, AutoNetworkedField]
    public TimeSpan PlasmaUseDelay = TimeSpan.FromSeconds(0.5);

    [DataField, AutoNetworkedField]
    public float SpeedMultiplier = 1.66f;
}
