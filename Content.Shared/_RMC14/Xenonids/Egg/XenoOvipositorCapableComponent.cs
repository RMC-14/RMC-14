using System.Linq;
using System.Numerics;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Egg;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoEggSystem))]
public sealed partial class XenoOvipositorCapableComponent : Component, IComponentDebug
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
    public EntProtoId[] ActionIds =
    [
        "ActionXenoLeader", "ActionXenoHeal", "ActionXenoTransferPlasmaQueen", "ActionXenoExpandWeeds",
        "ActionXenoQueenEye",
    ];

    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId, EntityUid> Actions = new();

    [DataField, AutoNetworkedField]
    public string? Sprite;

    public string GetDebugString()
    {
        return $"""
            AttachedState: {AttachedState}
            Cooldown: {Cooldown.TotalSeconds}
            Spawn: {Spawn.Id}
            Offset: {Offset}
            ActionIds:
              {string.Join("\r\n  ", ActionIds.Order())}
            Sprite: {Sprite}
            """;
    }
}
