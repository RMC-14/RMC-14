using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Construction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoConstructionSystem))]
public sealed partial class QueenBuildingBoostComponent : Component
{
    [DataField, AutoNetworkedField]
    public float BuildSpeedMultiplier = 0.5f;

    [DataField, AutoNetworkedField]
    public float RemoteUpgradeRange = 50f;
}
