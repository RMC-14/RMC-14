using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Marines.Mutiny;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MutineerLeaderComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Rule;

    [DataField, AutoNetworkedField]
    public bool Active;

    [DataField, AutoNetworkedField]
    public SpriteSpecifier Icon = new SpriteSpecifier.Rsi(new ResPath("_RMC14/Interface/job_icons/Misc/mutiny.rsi"), "hudmutineerleader");

    [DataField, AutoNetworkedField]
    public EntProtoId RecruitAction = "ActionMutineerRecruit";

    [DataField, AutoNetworkedField]
    public EntityUid? RecruitActionEntity;

    [DataField, AutoNetworkedField]
    public EntProtoId BeginAction = "ActionMutineerBegin";

    [DataField, AutoNetworkedField]
    public EntityUid? BeginActionEntity;
}
