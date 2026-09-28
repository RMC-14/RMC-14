using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Mentor.ImaginaryFriend;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ImaginaryFriendComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Imaginer;

    [DataField, AutoNetworkedField]
    public float MaxFriendDistance = 9; //TODO Restrict moving further away from the imaginer than this value

    [DataField, AutoNetworkedField]
    public bool Visible = true;

    [DataField]
    public EntProtoId ToggleVisibility = "ActionToggleVisibility";

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleVisibilityActionEntity;

    [DataField]
    public EntProtoId StopBeingFriends = "ActionStopBeingFriends";

    [DataField, AutoNetworkedField]
    public EntityUid? StopBeingFriendsActionEntity;

    [DataField, AutoNetworkedField]
    public float DefaultAlpha = 1f;

    [DataField, AutoNetworkedField]
    public float OwnAlphaWhileHidden = 0.15f;
}

[Serializable, NetSerializable]
public enum ImaginaryFriendVisuals
{
    Sprite,
    State,
}

public sealed partial class ImaginaryFriendToggleVisibilityActionEvent : InstantActionEvent;

public sealed partial class ImaginaryFriendStopBeingFriendsActionEvent : InstantActionEvent;

