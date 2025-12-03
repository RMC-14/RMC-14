using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Marines.Mutiny;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MutineerLeaderComponent : Component
{
    [DataField, AutoNetworkedField]
    public SpriteSpecifier Icon = new SpriteSpecifier.Rsi(new ResPath("_RMC14/Interface/cm_job_icons.rsi"), "hudmutineerleader");

    [DataField, AutoNetworkedField]
    public EntProtoId RecruitAction = "ActionMutineerRecruit";

    [DataField, AutoNetworkedField]
    public EntityUid? RecruitActionEntity;
}
