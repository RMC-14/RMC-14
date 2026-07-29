using Content.Shared._RMC14.Xenonids.DeployTraps;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Xenonids.AcidMine;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoAcidMineSystem), typeof(XenoDeployTrapsSystem))]
public sealed partial class XenoAcidMineComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Range = 13;

    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaCost = 40;

    [DataField, AutoNetworkedField]
    public bool Empowered;

    //1 for a 3x3 area.
    //Only really used for the ability indicator now.
    [DataField, AutoNetworkedField]
    public int AcidMineRadius = 1;

    [DataField, AutoNetworkedField]
    public EntProtoId BlastProto = "XenoAcidBlast";

    [DataField, AutoNetworkedField]
    public EntProtoId EmpoweredBlastProto = "XenoAcidBlastEmpowered";

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi ActionIcon = new(new ResPath("_RMC14/Actions/xeno_actions.rsi"), "acid_mine");

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi ActionIconEmpowered = new(new ResPath("_RMC14/Actions/xeno_actions.rsi"), "acid_mine_empowered");
}
