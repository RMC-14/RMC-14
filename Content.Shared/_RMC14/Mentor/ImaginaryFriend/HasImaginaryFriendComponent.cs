using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Mentor.ImaginaryFriend;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HasImaginaryFriendComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Friends = new ();
}
