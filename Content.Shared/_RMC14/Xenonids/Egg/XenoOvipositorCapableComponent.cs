using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Egg;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoEggSystem))]
public sealed partial class XenoOvipositorCapableComponent : Component
{
    [DataField, AutoNetworkedField]
    public string AttachedState = "normal";

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(30);

    [DataField, AutoNetworkedField]
    public EntProtoId Spawn = "XenoEgg";

    [DataField, AutoNetworkedField]
    public Vector2 Offset = new(-1, -1);

    [DataField, AutoNetworkedField]
    public EntProtoId[] ActionIds = ["ActionXenoLeader", "ActionXenoHeal"];

    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId, EntityUid> Actions = new();
}
