using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedFireGroupSystem))]
public sealed partial class RMCUserFireGroupComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<string, TimeSpan> LastFired = new();

    [DataField, AutoNetworkedField]
    public Dictionary<string, EntityUid> LastGun = new();
}
