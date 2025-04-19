using Content.Shared.Interaction;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Ladder;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedLadderSystem))]
public sealed partial class LadderComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? Id;

    [DataField, AutoNetworkedField]
    public EntityUid? Other;

    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public float Range = SharedInteractionSystem.InteractionRange + 0.1f;

    [DataField, AutoNetworkedField]
    public EntityUid? LastDoAfterEnt;

    [DataField, AutoNetworkedField]
    public ushort? LastDoAfterId;

    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan LastDoAfterTime;

    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Watching = new();
}
