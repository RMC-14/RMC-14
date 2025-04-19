using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medal;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedPlaytimeMedalSystem))]
public sealed partial class PlaytimeMedalHolderComponent : Component
{
    [DataField, AutoNetworkedField]
    public string ContainerId = "rmc_playtime_medal";

    [DataField, AutoNetworkedField]
    public EntityUid? Medal;
}
