using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Shuttles;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCOverrideShuttleSoundsComponent : Component
{
    [DataField, AutoNetworkedField]
    public SoundSpecifier? StartupSound;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? TravelSound;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? ArrivalSound;
}
