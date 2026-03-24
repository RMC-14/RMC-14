using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Tools;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCDeviceBreakerComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan DoAfterTime = TimeSpan.Zero;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? UseSound;

    [DataField, AutoNetworkedField]
    public bool Repeat = true;
}
