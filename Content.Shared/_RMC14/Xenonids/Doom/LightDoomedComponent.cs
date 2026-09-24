using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Doom;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LightDoomedComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(10);

    [DataField, AutoNetworkedField]
    public TimeSpan? EndsAt;

    [DataField, AutoNetworkedField]
    public bool WasEnabled;

    [DataField, AutoNetworkedField]
    public bool DoomActivated = false;
}
