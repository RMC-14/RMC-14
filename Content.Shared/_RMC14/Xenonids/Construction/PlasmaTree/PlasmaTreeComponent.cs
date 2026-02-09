using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Construction.PlasmaTree;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PlasmaTreeComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaAmount = 75;

    [DataField, AutoNetworkedField]
    public float PlasmaRange = 1.5F;

    [DataField, AutoNetworkedField]
    public TimeSpan PlasmaCooldown = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public TimeSpan NextPlasmaAt;

    [DataField]
    public DoAfterId? PlasmaDoAfter;
}