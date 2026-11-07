using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Ghost;
using Content.Shared._RMC14.Roles.FindParasite;

namespace Content.Shared._RMC14.Mobs;

[RegisterComponent, NetworkedComponent, Access([typeof(SharedGhostSystem)])]
[AutoGenerateComponentState(true)]
public sealed partial class CMGhostComponent : Component
{
    [DataField]
    public EntProtoId ToggleMarineHud = "ActionToggleMarineHud";

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleMarineHudEntity;

    [DataField]
    public EntProtoId ToggleXenoHud = "ActionToggleXenoHud";

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleXenoHudEntity;

    [DataField]
    public EntProtoId FindParasite = "ActionFindParasite";

    [DataField, AutoNetworkedField]
    public EntityUid? FindParasiteEntity;
}


public sealed partial class ToggleMarineHudActionEvent : InstantActionEvent { }

public sealed partial class ToggleXenoHudActionEvent : InstantActionEvent { }
