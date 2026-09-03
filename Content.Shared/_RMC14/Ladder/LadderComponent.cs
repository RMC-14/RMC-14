using Content.Shared.Interaction;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Ladder;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedLadderSystem))]
public sealed partial class LadderComponent : Component
{
    /// <summary>
    /// The "Group ID" string of this ladder. On mapload, all ladders with matching IDs will be
    /// linked together in order of their <see cref="Level"/>.
    /// </summary>
    /// <remarks>
    /// When mapping, this should be set using the ladder commands.
    /// </remarks>
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField, AutoNetworkedField]
    public string? GroupId;

    /// <summary>
    /// The ""floor level"" that the ladder is on.
    /// Moving from a lower to higher level means you're climbing upwards, and vice versa.
    /// </summary>
    /// <remarks>
    /// When mapping, this should be set using the ladder commands, or with the View Variables menu.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public int Level = 0;

    /// <summary>
    /// The ladder entity "above" this one, if any.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Above;

    /// <summary>
    /// The ladder entity "below" this one, if any.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Below;

    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public float Range = SharedInteractionSystem.InteractionRange + 0.1f;

    [DataField, AutoNetworkedField]
    public ushort? CurrentDoAfterId;

    [DataField, AutoNetworkedField]
    public EntityUid? CurrentDoAfterUser;

    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Watching = [];
}
