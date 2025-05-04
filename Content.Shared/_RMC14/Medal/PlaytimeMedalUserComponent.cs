using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medal;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedPlaytimeMedalSystem))]
public sealed partial class PlaytimeMedalUserComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Medal;
}
