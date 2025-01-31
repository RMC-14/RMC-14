using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Marines.Squads;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SquadSystem))]
public sealed partial class SquadLeaderComponent : Component
{
    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi Icon = new(new ResPath("_RMC14/Interface/cm_job_icons.rsi"), "hudsquad_leader_a");

    [DataField, AutoNetworkedField]
    public EntityUid? Headset;
}
